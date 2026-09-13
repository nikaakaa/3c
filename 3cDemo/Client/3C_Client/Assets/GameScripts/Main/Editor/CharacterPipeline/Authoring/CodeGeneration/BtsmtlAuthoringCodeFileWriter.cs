using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlAuthoringCodeFileWriter
    {
        public static BtsmtlAuthoringCodeFileWriteResult Write(BtsmtlAuthoringCodeExportResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (!result.Success)
                throw new InvalidOperationException("不能写入失败的C#导出结果。");
            if (result.Files == null || result.Files.Count == 0)
                throw new InvalidOperationException("C#导出结果没有源码文件。");

            string entryPath = Path.GetFullPath(result.OutputCodePath);
            string outputDirectory = Path.GetDirectoryName(entryPath);
            if (string.IsNullOrEmpty(outputDirectory))
                throw new InvalidOperationException("C#导出路径缺少专属生成目录。");
            outputDirectory = Path.GetFullPath(outputDirectory);

            var expectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (result.Files.Count(file => file.IsEntryPoint) != 1)
                throw new InvalidOperationException("C#导出结果必须只有一个入口文件。");

            var createdFiles = new List<string>();
            var modifiedFiles = new List<string>();
            var unchangedFiles = new List<string>();
            var deletedFiles = new List<string>();
            string failedPath = string.Empty;
            try
            {
                foreach (BtsmtlAuthoringCodeSourceFile sourceFile in result.Files)
                {
                    string filePath = Path.GetFullPath(sourceFile.FilePath);
                    if (!IsInside(filePath, outputDirectory))
                        throw new InvalidOperationException($"生成文件超出明确输出目录：{filePath}");
                    if (!expectedPaths.Add(filePath))
                        throw new InvalidOperationException($"生成结果包含重复文件：{filePath}");
                    failedPath = filePath;
                    string directory = Path.GetDirectoryName(filePath);
                    if (string.IsNullOrEmpty(directory))
                        throw new InvalidOperationException($"生成文件缺少目录：{filePath}");
                    Directory.CreateDirectory(directory);
                    byte[] expected = Encoding.UTF8.GetBytes(sourceFile.SourceCode ?? string.Empty);
                    bool existed = File.Exists(filePath);
                    if (existed && SameContent(File.ReadAllBytes(filePath), expected))
                    {
                        unchangedFiles.Add(filePath);
                        continue;
                    }

                    WriteAtomically(filePath, expected);
                    if (existed)
                        modifiedFiles.Add(filePath);
                    else
                        createdFiles.Add(filePath);
                }

                foreach (string filePath in Directory.EnumerateFiles(outputDirectory, "*.cs", SearchOption.AllDirectories)
                             .Where(path => !expectedPaths.Contains(Path.GetFullPath(path)))
                             .ToArray())
                {
                    failedPath = filePath;
                    File.Delete(filePath);
                    string metaPath = filePath + ".meta";
                    if (File.Exists(metaPath))
                        File.Delete(metaPath);
                    deletedFiles.Add(filePath);
                }

                return new BtsmtlAuthoringCodeFileWriteResult(
                    true,
                    string.Empty,
                    string.Empty,
                    createdFiles,
                    modifiedFiles,
                    unchangedFiles,
                    deletedFiles);
            }
            catch (Exception error)
            {
                return new BtsmtlAuthoringCodeFileWriteResult(
                    false,
                    failedPath,
                    error.Message,
                    createdFiles,
                    modifiedFiles,
                    unchangedFiles,
                    deletedFiles);
            }
        }

        static void WriteAtomically(string filePath, byte[] content)
        {
            string temporaryPath = filePath + ".tmp";
            try
            {
                File.WriteAllBytes(temporaryPath, content);
                if (File.Exists(filePath))
                    File.Replace(temporaryPath, filePath, null);
                else
                    File.Move(temporaryPath, filePath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        static bool IsInside(string path, string directory)
        {
            string normalizedPath = Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedDirectory = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return normalizedPath.StartsWith(
                normalizedDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        }

        static bool SameContent(byte[] actual, byte[] expected)
        {
            if (actual.SequenceEqual(expected))
                return true;
            return actual.Length == expected.Length + 3 &&
                actual[0] == 0xef &&
                actual[1] == 0xbb &&
                actual[2] == 0xbf &&
                actual.Skip(3).SequenceEqual(expected);
        }
    }
}
