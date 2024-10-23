using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Extensions.FileSystemGlobbing;

namespace RepoToTxtDesktop
{
    // RepoAnalyzer.cs
    public class RepoAnalyzer
    {
        private readonly List<string> _binaryExtensions = new()
    {
        ".exe", ".dll", ".so", ".a", ".lib", ".dylib", ".o", ".obj",
        // ... (весь список из Python)
    };

        public class AnalysisResult
        {
            public string FolderName { get; set; }
            public string Instructions { get; set; }
            public string ReadmeContent { get; set; }
            public string FolderStructure { get; set; }
            public string FileContents { get; set; }
        }

        private List<string> ParseGitignore(string folderPath)
        {
            var gitignorePath = Path.Combine(folderPath, ".gitignore");
            var ignorePatterns = new List<string>();

            if (File.Exists(gitignorePath))
            {
                var lines = File.ReadAllLines(gitignorePath);
                ignorePatterns.AddRange(
                    lines.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                );
            }

            return ignorePatterns;
        }

        private string GetReadmeContent(string folderPath)
        {
            var readmePath = Path.Combine(folderPath, "README.md");
            return File.Exists(readmePath)
                ? File.ReadAllText(readmePath)
                : "README not found.";
        }

        public AnalysisResult GetFolderContents(string folderPath, List<FileTreeItem> selectedFiles)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException("The provided path is not a valid directory.");

            var folderName = Path.GetFileName(Path.GetFullPath(folderPath));
            var readmeContent = GetReadmeContent(folderPath);
            var ignorePatterns = ParseGitignore(folderPath);

            var folderStructure = $"Folder Structure: {folderName}\n";
            folderStructure += TraverseFolderIteratively(folderPath, ignorePatterns, selectedFiles);

            var fileContents = GetFileContentsIteratively(folderPath, ignorePatterns, selectedFiles);

            var instructions = GenerateInstructions(folderName);

            return new AnalysisResult
            {
                FolderName = folderName,
                Instructions = instructions,
                ReadmeContent = readmeContent,
                FolderStructure = folderStructure,
                FileContents = fileContents
            };
        }

        private string GenerateInstructions(string folderName)
        {
            return $@"Prompt: Analyze the {folderName} folder to understand its structure, purpose, and functionality. 
Follow these steps to study the codebase:

1. Read the README file to gain an overview of the project, its goals, and any setup instructions.

2. Examine the folder structure to understand how the files and directories are organized.

3. Identify the main entry point of the application (e.g., main.py, app.py, index.js) and start analyzing the code flow from there.

4. Study the dependencies and libraries used in the project to understand the external tools and frameworks being utilized.

5. Analyze the core functionality of the project by examining the key modules, classes, and functions.

6. Look for any configuration files (e.g., config.py, .env) to understand how the project is configured and what settings are available.

7. Investigate any tests or test directories to see how the project ensures code quality and handles different scenarios.

8. Review any documentation or inline comments to gather insights into the codebase and its intended behavior.

9. Identify any potential areas for improvement, optimization, or further exploration based on your analysis.

10. Provide a summary of your findings, including the project's purpose, key features, and any notable observations or recommendations.

Use the files and contents provided below to complete this analysis:

";
        }

        private string TraverseFolderIteratively(string folderPath, List<string> ignorePatterns, List<FileTreeItem> selectedFiles)
        {
            var structure = new StringBuilder();
            var dirsToVisit = new Stack<(string RelativePath, string FullPath)>();
            var dirsVisited = new HashSet<string>();

            dirsToVisit.Push(("", folderPath));

            while (dirsToVisit.Count > 0)
            {
                var (relativePath, currentPath) = dirsToVisit.Pop();
                dirsVisited.Add(currentPath);

                try
                {
                    var entries = Directory.EnumerateFileSystemEntries(currentPath);
                    foreach (var entry in entries)
                    {
                        var entryName = Path.GetFileName(entry);
                        var entryRelativePath = Path.Combine(relativePath, entryName);

                        if (ShouldIgnore(entryRelativePath, ignorePatterns))
                        {
                            structure.AppendLine($"{entryRelativePath} [Ignored]");
                            continue;
                        }

                        if (Directory.Exists(entry))
                        {
                            if (entryName == ".git")
                            {
                                structure.AppendLine($"{entryRelativePath}/ [Ignored .git folder]");
                                continue;
                            }

                            structure.AppendLine($"{entryRelativePath}/");
                            if (!dirsVisited.Contains(entry))
                            {
                                dirsToVisit.Push((entryRelativePath, entry));
                            }
                        }
                        else
                        {
                            var isSelected = selectedFiles.Any(f => f.FullPath == entry);
                            structure.AppendLine(isSelected ? entryRelativePath : $"{entryRelativePath} [Skipped]");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    structure.AppendLine($"{relativePath}/ [Permission Denied]");
                }
            }

            return structure.ToString();
        }

        private string GetFileContentsIteratively(string folderPath, List<string> ignorePatterns, List<FileTreeItem> selectedFiles)
        {
            var contents = new StringBuilder();
            var dirsToVisit = new Stack<(string RelativePath, string FullPath)>();
            var dirsVisited = new HashSet<string>();

            dirsToVisit.Push(("", folderPath));

            while (dirsToVisit.Count > 0)
            {
                var (relativePath, currentPath) = dirsToVisit.Pop();
                dirsVisited.Add(currentPath);

                try
                {
                    var entries = Directory.EnumerateFileSystemEntries(currentPath);
                    foreach (var entry in entries)
                    {
                        var entryName = Path.GetFileName(entry);
                        var entryRelativePath = Path.Combine(relativePath, entryName);

                        if (ShouldIgnore(entryRelativePath, ignorePatterns))
                        {
                            contents.AppendLine($"{entryRelativePath} [Ignored]\n");
                            continue;
                        }

                        if (Directory.Exists(entry))
                        {
                            if (entryName == ".git")
                            {
                                contents.AppendLine($"{entryRelativePath}/ [Ignored .git folder]\n");
                                continue;
                            }

                            if (!dirsVisited.Contains(entry))
                            {
                                dirsToVisit.Push((entryRelativePath, entry));
                            }
                        }
                        else
                        {
                            var isSelected = selectedFiles.Any(f => f.FullPath == entry);
                            if (!isSelected)
                                continue;

                            contents.AppendLine($"File: {entryRelativePath}");
                            var extension = Path.GetExtension(entry).ToLower();

                            if (_binaryExtensions.Contains(extension))
                            {
                                contents.AppendLine("Content: Skipped binary file\n");
                            }
                            else
                            {
                                try
                                {
                                    var fileContent = File.ReadAllText(entry, Encoding.UTF8);
                                    contents.AppendLine($"Content:\n{fileContent}\n");
                                }
                                catch (Exception ex)
                                {
                                    try
                                    {
                                        var fileContent = File.ReadAllText(entry, Encoding.GetEncoding("ISO-8859-1"));
                                        contents.AppendLine($"Content (Latin-1 Decoded):\n{fileContent}\n");
                                    }
                                    catch
                                    {
                                        contents.AppendLine($"Content: Skipped due to error ({ex.Message})\n");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    contents.AppendLine($"{relativePath}/ [Permission Denied]\n");
                }
            }

            return contents.ToString();
        }

        public void GenerateReport(string folderPath, List<FileTreeItem> selectedFiles)
        {
            var result = GetFolderContents(folderPath, selectedFiles);
            var outputFilename = $"{result.FolderName}_contents.txt";
            var outputPath = Path.Combine(folderPath, outputFilename);

            File.WriteAllText(outputPath,
                $"{result.Instructions}\n" +
                $"README:\n{result.ReadmeContent}\n\n" +
                $"{result.FolderStructure}\n\n" +
                result.FileContents,
                Encoding.UTF8);
        }


        private bool ShouldIgnore(string path, List<string> ignorePatterns)
            {
                var matcher = new Matcher();
                matcher.AddIncludePatterns(ignorePatterns);
                return matcher.Match(path.Replace("\\", "/")).HasMatches;
            }
        }
}

