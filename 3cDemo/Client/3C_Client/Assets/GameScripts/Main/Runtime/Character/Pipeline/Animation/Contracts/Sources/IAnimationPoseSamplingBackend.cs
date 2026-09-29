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
        void BeginFrame(in CharacterPoseSourceFrameLease lease);
        void RequireOpenFrame(in CharacterPoseSourceFrameLease lease);
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
        void ValidateFrame(in CharacterPoseSourceFrameLease lease);
        void EnterEvaluateBarrier(in CharacterPoseSourceFrameLease lease);
        void ApplyFrame(in CharacterPoseSourceFrameLease lease);
        void ValidateAppliedFrame(in CharacterPoseSourceFrameLease lease);
        void FinalizeAppliedFrame(in CharacterPoseSourceFrameLease lease);
        void RollbackAppliedFrame(in CharacterPoseSourceFrameLease lease);
        void DiscardFrame(in CharacterPoseSourceFrameLease lease);
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
