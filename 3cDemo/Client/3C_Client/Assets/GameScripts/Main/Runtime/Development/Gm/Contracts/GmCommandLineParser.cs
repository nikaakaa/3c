using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdPerson.Development.Gm
{
    public readonly struct GmParsedCommand
    {
        public GmParsedCommand(string commandId, string[] arguments)
        {
            CommandId = commandId;
            Arguments = arguments;
        }

        public string CommandId { get; }
        public string[] Arguments { get; }
    }

    public static class GmCommandLineParser
    {
        public const int MaximumLineLength = 2048;
        public const int MaximumArguments = 16;

        public static bool TryParse(string line, out GmParsedCommand command, out string error)
        {
            command = default;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(line))
                return Reject("请输入命令。", out error);
            if (line.Length > MaximumLineLength)
                return Reject($"命令不能超过 {MaximumLineLength} 个字符。", out error);
            var tokens = new List<string>();
            var token = new StringBuilder();
            char quote = '\0';
            bool started = false;
            for (int i = 0; i < line.Length; i++)
            {
                char value = line[i];
                if (value == '\r' || value == '\n' || value == '\0')
                    return Reject("一次只能提交一行命令。", out error);
                if (quote != '\0')
                {
                    if (value == quote)
                        quote = '\0';
                    else if (value == '\\' && i + 1 < line.Length &&
                             (line[i + 1] == quote || line[i + 1] == '\\'))
                        token.Append(line[++i]);
                    else
                        token.Append(value);
                    continue;
                }
                if (value == '"' || value == '\'')
                {
                    quote = value;
                    started = true;
                }
                else if (char.IsWhiteSpace(value))
                {
                    if (!started)
                        continue;
                    tokens.Add(token.ToString());
                    token.Clear();
                    started = false;
                    if (tokens.Count > MaximumArguments + 1)
                        return Reject($"参数不能超过 {MaximumArguments} 个。", out error);
                }
                else
                {
                    token.Append(value);
                    started = true;
                }
            }
            if (quote != '\0')
                return Reject("参数引号没有闭合。", out error);
            if (started)
                tokens.Add(token.ToString());
            if (tokens.Count == 0 || !GmCommandSyntax.IsValidCommandId(tokens[0]))
                return Reject("命令名须以小写字母开头，仅使用小写字母、数字、点、横线或下划线，最多 64 个字符。", out error);
            if (tokens.Count > MaximumArguments + 1)
                return Reject($"参数不能超过 {MaximumArguments} 个。", out error);
            string id = tokens[0];
            tokens.RemoveAt(0);
            command = new GmParsedCommand(id, tokens.ToArray());
            return true;
        }

        static bool Reject(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
