using System;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterPresentationCleanup
    {
        internal static void Record(ref Exception failure, Exception cleanup)
        {
            failure = failure == null ? cleanup : new AggregateException(failure, cleanup);
        }

        internal static void Dispose(IDisposable owner, ref Exception failure)
        {
            try
            {
                owner?.Dispose();
            }
            catch (Exception cleanup)
            {
                Record(ref failure, cleanup);
            }
        }
    }
}
