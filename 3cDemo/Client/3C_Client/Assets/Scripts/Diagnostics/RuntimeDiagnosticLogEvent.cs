namespace ThirdPersonDiagnostics
{
    public readonly struct RuntimeDiagnosticLogEvent
    {
        public RuntimeDiagnosticLogEvent(
            RuntimeDiagnosticLogCategory category,
            RuntimeDiagnosticLogLevel level,
            string message,
            string statePath = "",
            string previousStatePath = "",
            int step = 0,
            int frame = 0,
            string context = "",
            string channelKey = "")
        {
            Category = category;
            Level = level;
            Message = message ?? string.Empty;
            ChannelKey = NormalizeChannelKey(category, Message, channelKey);
            StatePath = statePath ?? string.Empty;
            PreviousStatePath = previousStatePath ?? string.Empty;
            Step = step < 0 ? 0 : step;
            Frame = frame < 0 ? 0 : frame;
            Context = context ?? string.Empty;
        }

        public RuntimeDiagnosticLogCategory Category { get; }
        public RuntimeDiagnosticLogLevel Level { get; }
        public string Message { get; }
        public string ChannelKey { get; }
        public string StatePath { get; }
        public string PreviousStatePath { get; }
        public int Step { get; }
        public int Frame { get; }
        public string Context { get; }
        public bool HasChannelKey => !string.IsNullOrEmpty(ChannelKey);
        public bool HasStatePath => !string.IsNullOrEmpty(StatePath);
        public bool HasPreviousStatePath => !string.IsNullOrEmpty(PreviousStatePath);
        public bool HasContext => !string.IsNullOrEmpty(Context);

        public static string BuildDefaultChannelKey(RuntimeDiagnosticLogCategory category, string message)
        {
            string safeMessage = string.IsNullOrWhiteSpace(message) ? "unnamed" : message.Trim();
            return category + "." + safeMessage;
        }

        static string NormalizeChannelKey(RuntimeDiagnosticLogCategory category, string message, string channelKey)
        {
            if (!string.IsNullOrWhiteSpace(channelKey))
                return channelKey.Trim();

            return BuildDefaultChannelKey(category, message);
        }
    }
}
