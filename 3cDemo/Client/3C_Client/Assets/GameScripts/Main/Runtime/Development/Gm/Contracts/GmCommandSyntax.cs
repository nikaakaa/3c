using System;

namespace ThirdPerson.Development.Gm
{
    public static class GmCommandSyntax
    {
        public const int MaximumCommandIdLength = 64;

        public static bool IsValidCommandId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaximumCommandIdLength || id[0] < 'a' || id[0] > 'z')
                return false;
            for (int i = 1; i < id.Length; i++)
            {
                char value = id[i];
                if (!(value >= 'a' && value <= 'z') && !(value >= '0' && value <= '9') &&
                    value != '.' && value != '-' && value != '_')
                    return false;
            }
            return true;
        }

        public static bool IsValidRequest(GmCommandRequest request)
        {
            if (!Guid.TryParseExact(request.requestId, "N", out _) || !IsValidCommandId(request.commandId) ||
                request.arguments == null || request.arguments.Length > GmCommandLineParser.MaximumArguments)
                return false;
            int length = request.commandId.Length;
            foreach (string argument in request.arguments)
            {
                if (argument == null || argument.Length > GmCommandLineParser.MaximumLineLength)
                    return false;
                for (int i = 0; i < argument.Length; i++)
                {
                    if (char.IsControl(argument[i]))
                        return false;
                }
                length += argument.Length + 1;
            }
            return length <= GmCommandLineParser.MaximumLineLength;
        }
    }
}
