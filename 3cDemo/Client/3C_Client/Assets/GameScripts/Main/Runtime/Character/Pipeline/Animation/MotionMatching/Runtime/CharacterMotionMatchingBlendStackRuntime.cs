using System;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    internal readonly struct CharacterMotionMatchingBlendFramePlan
    {
        readonly CharacterMotionMatchingBlendStackRuntime m_Owner;

        internal CharacterMotionMatchingBlendFramePlan(
            CharacterMotionMatchingBlendStackRuntime owner,
            in CharacterAnimationBlendStackFramePlan blend)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Blend = blend;
        }

        internal CharacterAnimationBlendStackFramePlan Blend { get; }
        internal bool IsValid => m_Owner != null && Blend.IsValid;
        internal int EntryCount => Blend.EntryCount;
        internal bool UsesStoredPose => Blend.UsesStoredPose;
        internal bool CapturesPreviousOutput => Blend.CapturesPreviousOutput;

        internal CharacterMotionMatchingEntrySourcePlan GetEntrySource(int entryIndex) =>
            m_Owner.RequireEntrySource(entryIndex);

        internal float GetEntryScalarWeight(int entryIndex) =>
            m_Owner.GetEntryScalarWeight(entryIndex);

        internal float GetEntryBoneWeight(int entryIndex, int boneIndex) =>
            m_Owner.GetEntryBoneWeight(entryIndex, boneIndex);

        internal float GetStoredBoneWeight(int boneIndex) =>
            m_Owner.GetStoredBoneWeight(boneIndex);

        internal float StoredOutputWeight => m_Owner.StoredOutputWeight;
    }

    internal sealed class CharacterMotionMatchingBlendStackRuntime
    {
        struct EntrySourceState
        {
            internal ulong Generation;
            internal CharacterMotionMatchingEntrySourcePlan Source;
            internal bool IsValid => Generation != 0 && Source.IsValid &&
                                     Source.Identity.SourceLineage.SelectionGeneration.Value == Generation;
        }

        readonly PoseNodeId m_NodeId;
        readonly CharacterMotionMatchingBlendPlanDescriptor m_Plan;
        readonly AnimationBlendCurveCatalogPayload m_Curves;
        readonly AnimationBlendProfileCatalogPayload m_Profiles;
        readonly CharacterAnimationBlendStackOwnerWorkspace m_Workspace;
        readonly EntrySourceState[] m_CommittedSources;
        readonly EntrySourceState[] m_PendingSources;

        ulong m_FrameIdentity;
        bool m_FrameOpen;

        internal CharacterMotionMatchingBlendStackRuntime(
            PoseNodeId nodeId,
            CharacterMotionMatchingBlendPlanDescriptor plan,
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles,
            CharacterAnimationRigPayload rig,
            int sourceWorkspaceCapacity)
        {
            if (!nodeId.IsValid || plan == null || curves == null || profiles == null || rig == null ||
                plan.StackPolicy == null || plan.StackPolicy.StoredPosePolicy != AnimationStoredPosePolicy.CompressOldest ||
                sourceWorkspaceCapacity <= plan.StackPolicy.MaxActiveSourceEntries)
            {
                throw new ArgumentException("Motion Matching Blend Stack runtime configuration is invalid.");
            }
            curves.Require(plan.CurveIndex).RequireValid();
            profiles.Require(plan.ProfileIndex).RequireValid(rig.PoseBoneCount, rig.RigId, rig.RigRevision);
            m_NodeId = nodeId;
            m_Plan = plan;
            m_Curves = curves;
            m_Profiles = profiles;
            m_Workspace = new CharacterAnimationBlendStackOwnerWorkspace(
                plan.StackPolicy.MaxActiveSourceEntries,
                rig.PoseBoneCount);
            m_CommittedSources = new EntrySourceState[sourceWorkspaceCapacity];
            m_PendingSources = new EntrySourceState[sourceWorkspaceCapacity];
        }

        internal void BeginFrame(ulong frameIdentity, float deltaSeconds)
        {
            if (m_FrameOpen)
                throw new InvalidOperationException($"Motion Matching Blend Stack '{m_NodeId}' frame is already open.");
            Array.Copy(m_CommittedSources, m_PendingSources, m_CommittedSources.Length);
            m_Workspace.BeginFrame(frameIdentity, deltaSeconds);
            m_FrameIdentity = frameIdentity;
            m_FrameOpen = true;
        }

        internal CharacterMotionMatchingBlendFramePlan Apply(
            in CharacterMotionMatchingEntrySourcePlan source,
            MotionMatchingSelectionDecisionKind selectionKind)
        {
            RequireOpenFrame();
            if (!source.IsValid || source.Identity.NodeId != m_NodeId ||
                !Enum.IsDefined(typeof(MotionMatchingSelectionDecisionKind), selectionKind) ||
                selectionKind == MotionMatchingSelectionDecisionKind.Invalid)
            {
                throw new ArgumentException("Motion Matching Blend Stack source is invalid.");
            }
            ulong generation = source.Identity.SourceLineage.SelectionGeneration.Value;
            int sourceIndex = selectionKind == MotionMatchingSelectionDecisionKind.Continue
                ? RequireSourceIndex(generation)
                : AllocateSourceIndex();
            m_PendingSources[sourceIndex] = new EntrySourceState
            {
                Generation = generation,
                Source = source
            };
            var request = new CharacterAnimationBlendStackPushRequest(
                generation,
                sourceIndex,
                m_Plan.JumpDurationSeconds,
                m_Plan.CurveIndex,
                m_Plan.ProfileIndex,
                selectionKind == MotionMatchingSelectionDecisionKind.Initialize);
            CharacterAnimationBlendStackPushKind push = m_Workspace.Push(
                in request,
                m_Plan.StackPolicy);
            if (selectionKind == MotionMatchingSelectionDecisionKind.Continue &&
                push != CharacterAnimationBlendStackPushKind.Continue ||
                selectionKind != MotionMatchingSelectionDecisionKind.Continue &&
                push == CharacterAnimationBlendStackPushKind.Continue)
            {
                throw new InvalidOperationException("Motion Matching selection and Blend Stack transition disagree.");
            }
            CharacterAnimationBlendStackFramePlan blend = m_Workspace.Evaluate(m_Curves, m_Profiles);
            return new CharacterMotionMatchingBlendFramePlan(this, in blend);
        }

        internal void CompleteFrame(
            ulong frameIdentity,
            float outputWeight,
            float[] denseBoneOutputWeights)
        {
            RequireFrame(frameIdentity);
            m_Workspace.CompleteFrame(frameIdentity, outputWeight, denseBoneOutputWeights);
            for (int i = 0; i < m_Workspace.RetirementCount; i++)
            {
                CharacterAnimationBlendStackRetirement retirement = m_Workspace.GetRetirement(i);
                EntrySourceState source = m_PendingSources[retirement.SourceWorkspaceIndex];
                if (!source.IsValid || source.Generation != retirement.EntryIdentity)
                    throw new InvalidOperationException("Motion Matching Blend Stack retirement targets a stale source.");
                m_PendingSources[retirement.SourceWorkspaceIndex] = default;
            }
            Array.Copy(m_PendingSources, m_CommittedSources, m_CommittedSources.Length);
            ClearFrame();
        }

        internal void DiscardFrame(ulong frameIdentity)
        {
            RequireFrame(frameIdentity);
            m_Workspace.DiscardFrame(frameIdentity);
            ClearFrame();
        }

        internal void Reset()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException($"Motion Matching Blend Stack '{m_NodeId}' cannot reset during a frame.");
            Array.Clear(m_CommittedSources, 0, m_CommittedSources.Length);
            m_Workspace.Reset();
        }

        internal void ResetFrame()
        {
            RequireOpenFrame();
            ulong frameIdentity = m_FrameIdentity;
            m_Workspace.DiscardFrame(frameIdentity);
            Array.Clear(m_CommittedSources, 0, m_CommittedSources.Length);
            Array.Clear(m_PendingSources, 0, m_PendingSources.Length);
            m_Workspace.Reset();
            m_Workspace.BeginFrame(frameIdentity, 0f);
        }

        internal CharacterMotionMatchingEntrySourcePlan RequireEntrySource(int entryIndex)
        {
            RequireOpenFrame();
            int sourceIndex = m_Workspace.GetSourceWorkspaceIndex(entryIndex);
            EntrySourceState source = m_PendingSources[sourceIndex];
            if (!source.IsValid || source.Generation != m_Workspace.GetEntryIdentity(entryIndex))
                throw new InvalidOperationException("Motion Matching Blend Stack entry source is stale.");
            return source.Source;
        }

        internal float GetEntryScalarWeight(int entryIndex)
        {
            RequireOpenFrame();
            return m_Workspace.GetEntryScalarWeight(entryIndex);
        }

        internal float GetEntryBoneWeight(int entryIndex, int boneIndex)
        {
            RequireOpenFrame();
            return m_Workspace.GetEntryBoneWeight(entryIndex, boneIndex);
        }

        internal float GetStoredBoneWeight(int boneIndex)
        {
            RequireOpenFrame();
            return m_Workspace.GetStoredBoneWeight(boneIndex);
        }

        internal float StoredOutputWeight
        {
            get
            {
                RequireOpenFrame();
                return m_Workspace.StoredOutputWeight;
            }
        }

        int RequireSourceIndex(ulong generation)
        {
            for (int i = 0; i < m_PendingSources.Length; i++)
            {
                if (m_PendingSources[i].IsValid && m_PendingSources[i].Generation == generation)
                    return i;
            }
            throw new InvalidOperationException("Motion Matching Continue has no live Blend Stack entry.");
        }

        int AllocateSourceIndex()
        {
            for (int i = 0; i < m_PendingSources.Length; i++)
            {
                if (!m_PendingSources[i].IsValid)
                    return i;
            }
            throw new InvalidOperationException("Motion Matching Blend Stack source workspace was exceeded.");
        }

        void RequireFrame(ulong frameIdentity)
        {
            RequireOpenFrame();
            if (frameIdentity == 0 || frameIdentity != m_FrameIdentity)
                throw new InvalidOperationException("Motion Matching Blend Stack frame identity is stale.");
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException($"Motion Matching Blend Stack '{m_NodeId}' has no open frame.");
        }

        void ClearFrame()
        {
            m_FrameIdentity = 0;
            m_FrameOpen = false;
        }
    }
}
