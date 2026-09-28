#if KK_DIAGNOSTIC_SAMPLING
using System;
using KK.GeneratedDiagnosticSampling;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public static partial class CharacterNativePresentationDiagnosticEvent
    {
        public const string EventId = "character-presentation-replication/post-commit";
        public const string TargetTypeIdentity = "character-runtime/1";
        public const string LineageTypeIdentity = "character-pose-native-lineage/1";
        internal static bool IsInterested(Guid runtimeId)
        {
            var target = new DiagnosticEventTargetKey(TargetTypeIdentity, runtimeId);
            bool interested = false;
            QueryPublishCommittedInterest(in target, ref interested);
            return interested;
        }
        static partial void QueryPublishCommittedInterest(in DiagnosticEventTargetKey target, ref bool interested);
        [DiagnosticEvent(EventId)]
        static partial void PublishCommitted(in DiagnosticEventTargetKey target, in DiagnosticLineageKey lineage,
            in CharacterNativePoseCaptureFrame animation, in CharacterNativeCameraCaptureFrame camera,
            in CharacterNativeBodyCaptureFrame facts, in CharacterNativeCommandCaptureFrame commands,
            in CharacterPoseRenderCaptureFrame render);
        internal static void Publish(Guid runtimeId, in CharacterPoseDiagnosticFrame frame,
            in CharacterNativePoseCaptureFrame animation, in CharacterNativeCameraCaptureFrame camera,
            in CharacterNativeBodyCaptureFrame facts, in CharacterNativeCommandCaptureFrame commands,
            in CharacterPoseRenderCaptureFrame render)
        {
            var target = new DiagnosticEventTargetKey(TargetTypeIdentity, runtimeId);
            var lineage = new DiagnosticLineageKey(LineageTypeIdentity, frame.PresentationFrame, frame.CompletionIdentity);
            PublishCommitted(in target, in lineage, in animation, in camera, in facts, in commands, in render);
        }
    }
}
#endif
