﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace AttackSurfaceAnalyzer.Utils
{
    public static class DirectoryWalker
    {
        public static IEnumerable<string> WalkDirectory(string root)
        {
            // Data structure to hold names of subfolders to be
            // examined for files.
            Stack<string> dirs = new Stack<string>();

            if (System.IO.Directory.Exists(root))
            {
                dirs.Push(root);
            }

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                if (Filter.IsFiltered(AsaHelpers.GetPlatformString(), "Scan", "File", "Path", currentDir))
                {
                    continue;
                }
                else
                {
                    yield return currentDir;

                    var fileInfo = new DirectoryInfo(currentDir);
                    // Skip symlinks to avoid loops
                    // Future improvement: log it as a symlink in the data
                    if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        Log.Verbose($"Skipping symlink at {currentDir}");
                        continue;
                    }
                }

                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch (Exception e) when (
                    e is ArgumentException ||
                    e is ArgumentNullException ||
                    e is PathTooLongException ||
                    e is IOException ||
                    e is DirectoryNotFoundException ||
                    e is UnauthorizedAccessException)
                {
                    Log.Verbose("Failed to get Directories for {0} {1}", currentDir, e.GetType().ToString());
                    continue;
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }

                catch (Exception e) when (
                    e is UnauthorizedAccessException ||
                    e is IOException ||
                    e is ArgumentException ||
                    e is ArgumentNullException ||
                    e is PathTooLongException ||
                    e is DirectoryNotFoundException)
                {
                    Log.Verbose("Failed to get files for {0} {1}", currentDir, e.GetType().ToString());
                    continue;
                }

                foreach (string file in files)
                {
                    if (Filter.IsFiltered(AsaHelpers.GetPlatformString(), "Scan", "File", "Path", file))
                    {
                        continue;
                    }

                    yield return file;
                    //FileInfo fileInfo = null;


                }

                // Push the subdirectories onto the stack for traversal.
                // This could also be done before handing the files.
                foreach (string dir in subDirs)
                {
                    DirectoryInfo fileInfo = null;
                    try
                    {
                        fileInfo = new DirectoryInfo(dir);

                        // Skip symlinks to avoid loops
                        // Future improvement: log it as a symlink in the data
                        if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                        {
                            continue;
                        }
                    }
                    catch (Exception e) when (
                        e is SecurityException
                        || e is ArgumentException
                        || e is ArgumentException
                        || e is PathTooLongException
                        || e is UnauthorizedAccessException
                        || e is IOException)
                    {
                        Log.Verbose("Failed to create DirectoryInfo from Directory at {0} {1}", dir, e.GetType().ToString());
                        continue;
                    }


                    if (fileInfo != null)
                    {
                        dirs.Push(dir);
                    }
                }
            }
        }
    }
}