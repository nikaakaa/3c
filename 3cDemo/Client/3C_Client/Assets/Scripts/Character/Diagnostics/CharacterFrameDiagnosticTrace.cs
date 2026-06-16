using ThirdPersonDiagnostics;

namespace ThirdPersonDiagnostics
{
    public readonly struct CharacterFrameDiagnosticTrace
    {
        public CharacterFrameDiagnosticTrace(
            RuntimeDiagnosticLogCategory category,
            RuntimeDiagnosticLogLevel level,
            string eventId,
            string statePath,
            string previousStatePath,
            int step,
            int frame,
            string context,
            string channelKey = "")
        {
            Category = category;
            Level = level;
            EventId = eventId ?? string.Empty;
            StatePath = statePath ?? string.Empty;
            PreviousStatePath = previousStatePath ?? string.Empty;
            Step = step < 0 ? 0 : step;
            Frame = frame < 0 ? 0 : frame;
            Context = context ?? string.Empty;
            ChannelKey = channelKey ?? string.Empty;
        }

        public RuntimeDiagnosticLogCategory Category { get; }
        public RuntimeDiagnosticLogLevel Level { get; }
        public string EventId { get; }
        public string StatePath { get; }
        public string PreviousStatePath { get; }
        public int Step { get; }
        public int Frame { get; }
        public string Context { get; }
        public string ChannelKey { get; }
        public bool HasEvent => !string.IsNullOrEmpty(EventId);

        public RuntimeDiagnosticLogEvent ToEvent()
        {
            return new RuntimeDiagnosticLogEvent(
                Category,
                Level,
                EventId,
                StatePath,
                PreviousStatePath,
                Step,
                Frame,
                Context,
                ChannelKey);
        }
    }
}
