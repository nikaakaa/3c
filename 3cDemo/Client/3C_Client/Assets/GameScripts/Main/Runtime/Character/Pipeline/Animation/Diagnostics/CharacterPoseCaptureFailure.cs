using System;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public static class CharacterPoseCaptureFailure
    {
        public static event Action<Guid, string, Exception> Reported;
        internal static void Report(Guid runtimeId, string eventId, Exception exception)
        {
            try
            {
                Reported?.Invoke(runtimeId, eventId, exception);
            }
            catch (Exception reportingFailure)
            {
                UnityEngine.Debug.LogException(new AggregateException(exception, reportingFailure));
            }
        }
    }
}
