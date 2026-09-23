using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public static class CharacterPoseCaptureFailure
    {
        public static event Action<Guid, string, Exception> Reported;
        internal static void Report(Guid runtimeId, string eventId, Exception exception) =>
            Reported?.Invoke(runtimeId, eventId, exception);
    }
}
