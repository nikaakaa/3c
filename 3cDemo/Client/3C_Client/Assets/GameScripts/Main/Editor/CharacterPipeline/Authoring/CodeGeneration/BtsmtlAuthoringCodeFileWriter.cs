using System;
using System.IO;
using System.Text;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.CodeGeneration
{
    public static class BtsmtlAuthoringCodeFileWriter
    {
        public static void Write(BtsmtlAuthoringCodeExportResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (!result.Success)
                throw new InvalidOperationException("不能写入失败的C#导出结果。");
            string outputPath = Path.GetFullPath(result.OutputCodePath);
            string directory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrEmpty(directory))
                throw new InvalidOperationException("C#导出路径缺少目录。");
            Directory.CreateDirectory(directory);
            string temporaryPath = outputPath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, result.SourceCode, new UTF8Encoding(false));
                if (File.Exists(outputPath))
                    File.Replace(temporaryPath, outputPath, null);
                else
                    File.Move(temporaryPath, outputPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }
    }
}
