using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Presentation.Animancer
{
    internal interface IAnimationPoseSamplingBackend : IDisposable
    {
        int SourceCapacity { get; }
        int ClipCapacity { get; }
        bool HasOpenFrame { get; }
        void BeginFrame(CharacterPoseSourceFrameLease lease);
        void RequireOpenFrame(CharacterPoseSourceFrameLease lease);
        AnimationPoseSourcePrepareResult PrepareOrUpdate(
            in AnimationPoseSampleRequest request,
            AnimationPhysicalSourceIdentity physicalIdentity,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId playerNodeId);
        AnimationPoseSourcePrepareResult PrepareOrUpdate(
            AnimationPoseSourceId sourceId,
            AnimationPhysicalSourceIdentity physicalIdentity,
            AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            PoseNodeId playerNodeId);
        AnimationPoseSourceReleaseToken StageRelease(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId,
            AnimationPhysicalSourceIdentity physicalIdentity);
        void ValidateFrame(CharacterPoseSourceFrameLease lease);
        void EnterEvaluateBarrier(CharacterPoseSourceFrameLease lease);
        void ApplyFrame(CharacterPoseSourceFrameLease lease);
        void ValidateAppliedFrame(CharacterPoseSourceFrameLease lease);
        void FinalizeAppliedFrame(CharacterPoseSourceFrameLease lease);
        void RollbackAppliedFrame(CharacterPoseSourceFrameLease lease);
        void DiscardFrame(CharacterPoseSourceFrameLease lease);
        void Release(in AnimationPoseSourceReleaseToken token);
        bool ContainsCommitted(AnimationPoseSourceId sourceId, PoseNodeId playerNodeId);
        ClipSamplePlan RequireDominantClipSample(
            AnimationPoseSourceId sourceId,
            PoseNodeId playerNodeId,
            ulong completionIdentity);
        void ExecuteDeferredReleases();
        void Clear();
    }
}
