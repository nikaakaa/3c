#if KK_DIAGNOSTIC_SAMPLING
using KK.GeneratedDiagnosticSampling;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public static partial class CharacterPresentationReplicationDiagnosticEvent
    {
        public const string EventId =
            "character-presentation-replication/post-commit";
        public const string TargetTypeIdentity = "character-runtime/1";
        public const string LineageTypeIdentity =
            "character-presentation-replication-lineage/1";

        [DiagnosticEvent(EventId)]
        static partial void PublishCommitted(
            in DiagnosticEventTargetKey target,
            in DiagnosticLineageKey lineage,
            in CharacterAnimationPresentationCaptureFrame animation,
            in CharacterCameraPresentationCaptureFrame camera,
            in CharacterPresentationCommandCaptureFacts commands);

        static partial void QueryPublishCommittedInterest(
            in DiagnosticEventTargetKey target,
            ref bool interested);

        internal static bool IsInterested(
            in DiagnosticEventTargetKey target)
        {
            bool interested = false;
            QueryPublishCommittedInterest(in target, ref interested);
            return interested;
        }

        internal static void Publish(
            in DiagnosticEventTargetKey target,
            in DiagnosticLineageKey lineage,
            in CharacterAnimationPresentationCaptureFrame animation,
            in CharacterCameraPresentationCaptureFrame camera,
            in CharacterPresentationCommandCaptureFacts commands) =>
            PublishCommitted(
                in target,
                in lineage,
                in animation,
                in camera,
                in commands);
    }
}
#endif
