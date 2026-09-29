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
            in AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            in PoseNodeId playerNodeId);
        AnimationPoseSourcePrepareResult PrepareOrUpdate(
            in AnimationPoseSourceId sourceId,
            AnimationPhysicalSourceIdentity physicalIdentity,
            in AnimationReadOnlyBuffer<ClipSamplePlan> clips,
            in AnimationReadOnlyBuffer<AnimationPoseSourceClipBinding> clipCatalog,
            in AnimationPoseSourceCaptureBinding capture,
            in PoseNodeId playerNodeId);
        AnimationPoseSourceReleaseToken StageRelease(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId playerNodeId,
            AnimationPhysicalSourceIdentity physicalIdentity);
        void ValidateFrame(in CharacterPoseSourceFrameLease lease);
        void EnterEvaluateBarrier(in CharacterPoseSourceFrameLease lease);
        void ApplyFrame(in CharacterPoseSourceFrameLease lease);
        void ValidateAppliedFrame(in CharacterPoseSourceFrameLease lease);
        void FinalizeAppliedFrame(in CharacterPoseSourceFrameLease lease);
        void RollbackAppliedFrame(in CharacterPoseSourceFrameLease lease);
        void DiscardFrame(in CharacterPoseSourceFrameLease lease);
        void Release(in AnimationPoseSourceReleaseToken token);
        bool ContainsCommitted(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId playerNodeId);
        ClipSamplePlan RequireDominantClipSample(
            in AnimationPoseSourceId sourceId,
            in PoseNodeId playerNodeId,
            ulong completionIdentity);
        void ExecuteDeferredReleases();
        void Clear();
    }
}
