using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseNativePublicationFrameLease
    {
        internal CharacterPoseNativePublicationFrameLease(
            in CharacterPoseNativeFrameLineage lineage)
        {
            if (!lineage.IsValid)
                throw new ArgumentException(
                    "Native Final Pose publication lineage is invalid.",
                    nameof(lineage));
            Lineage = lineage;
            m_IsValid = true;
        }

        readonly bool m_IsValid;
        internal CharacterPoseNativeFrameLineage Lineage { get; }
        internal bool IsValid => m_IsValid && Lineage.IsValid;
        internal bool Matches(in CharacterPoseNativeFrameLineage lineage) =>
            IsValid && Lineage == lineage;
    }

    internal sealed class CharacterFinalPoseNativePublication : IDisposable
    {
        sealed class PendingPage
        {
            internal CharacterPoseNativePublicationFrameLease Lease;
            internal int BufferPage = -1;
            internal CharacterPoseNativePoseReadBinding Output;
            internal ComposedAnimationPoseFrame Frame;
            internal AnimationPhysicalBoneWriteDiagnostics PhysicalWrite;
            internal bool HasOutput;
            internal bool HasFrame;
            internal bool IsOpen => Lease.IsValid;

            internal void Begin(
                CharacterPoseNativePublicationFrameLease lease,
                int bufferPage)
            {
                if (IsOpen || !lease.IsValid || bufferPage < 0 || bufferPage > 1)
                    throw new InvalidOperationException(
                        "Native Final Pose publication page is already open or invalid.");
                Lease = lease;
                BufferPage = bufferPage;
                Output = default;
                Frame = default;
                PhysicalWrite = default;
                HasOutput = false;
                HasFrame = false;
            }

            internal void SetOutput(
                CharacterPoseNativePublicationFrameLease lease,
                in CharacterPoseNativePoseReadBinding output,
                in ComposedAnimationPoseFrame frame)
            {
                RequireLease(lease);
                if (HasOutput || HasFrame || !output.IsValid)
                    throw new InvalidOperationException(
                        "Native Final Pose publication output is already set or invalid.");
                Output = output;
                Frame = frame;
                HasOutput = true;
                HasFrame = true;
            }

            internal void RequireFrame(
                CharacterPoseNativePublicationFrameLease lease)
            {
                RequireLease(lease);
                if (!HasOutput || !HasFrame || BufferPage < 0)
                    throw new InvalidOperationException(
                        "Native Final Pose publication page is incomplete.");
            }

            internal void RequireLease(
                CharacterPoseNativePublicationFrameLease lease)
            {
                if (!IsOpen || !lease.IsValid || Lease.Lineage != lease.Lineage)
                    throw new InvalidOperationException(
                        "Native Final Pose publication lease is stale.");
            }

            internal void Clear()
            {
                Lease = default;
                BufferPage = -1;
                Output = default;
                Frame = default;
                PhysicalWrite = default;
                HasOutput = false;
                HasFrame = false;
            }
        }

        readonly string m_PoseGraphId;
        readonly string m_GraphRevision;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly IReadOnlyList<PoseNodeId> m_PlayerNodeIds;
        readonly CharacterFinalPosePhysicalWriter m_PhysicalWriter;
        readonly CharacterFinalPosePropertyWriter m_PropertyWriter;
        readonly CharacterPoseBoneKind[] m_BoneKinds;
        readonly AnimationLocalBonePose[] m_DenseLocalPoses;
        readonly float[] m_PoseParameters;
        readonly byte[] m_PoseParameterAvailability;
        readonly AnimationPoseSourceContribution[] m_Contributions;
        readonly float[] m_DenseContributionWeights;
        readonly FinalAnimationPoseFramePageLease[] m_PageLeases =
        {
            new FinalAnimationPoseFramePageLease(),
            new FinalAnimationPoseFramePageLease()
        };
        readonly PendingPage m_Pending = new PendingPage();
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_ContributionCapacity;
        int m_CommittedPage = -1;
        bool m_HasCommitted;
        ComposedAnimationPoseFrame m_CommittedFrame;
        AnimationPhysicalBoneWriteDiagnostics m_CommittedPhysicalWrite;
        bool m_Disposed;

        internal CharacterFinalPoseNativePublication(
            in CharacterPoseNativePreparedBinding preparedBinding,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterPoseSourceModule sourceModule,
            IReadOnlyList<CharacterPresentationAnimationPropertyBinding> animationProperties,
            IReadOnlyList<PoseNodeId> playerNodeIds,
            int contributionCapacity)
        {
            if (!preparedBinding.IsValid)
                throw new ArgumentException(
                    "Native Final Pose publication binding is invalid.",
                    nameof(preparedBinding));
            if (!rigBinding || !rootHierarchy)
                throw new ArgumentException(
                    "Native Final Pose publication rig binding is incomplete.");
            if (sourceModule == null)
                throw new ArgumentNullException(nameof(sourceModule));
            if (animationProperties == null)
                throw new ArgumentNullException(nameof(animationProperties));
            if (playerNodeIds == null || playerNodeIds.Count == 0)
                throw new ArgumentException(
                    "Native Final Pose publication player identities are missing.",
                    nameof(playerNodeIds));
            if (contributionCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(contributionCapacity));
            if (!string.Equals(preparedBinding.RigId, rigBinding.RigId, StringComparison.Ordinal) ||
                !string.Equals(preparedBinding.RigRevision, rigBinding.RigRevision, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Native Final Pose publication rig identity is inconsistent.");
            }

            m_PoseGraphId = preparedBinding.GraphId.Value;
            m_GraphRevision = preparedBinding.GraphRevision;
            m_Rig = preparedBinding.Rig;
            m_SourceModule = sourceModule;
            m_PlayerNodeIds = new PoseNodeId[playerNodeIds.Count];
            for (int i = 0; i < m_PlayerNodeIds.Count; i++)
            {
                if (!playerNodeIds[i].IsValid)
                    throw new ArgumentException(
                        "Native Final Pose publication player identity is invalid.",
                        nameof(playerNodeIds));
                m_PlayerNodeIds[i] = playerNodeIds[i];
            }
            m_BoneCount = m_Rig.PoseBoneCount;
            m_ParameterCount = preparedBinding.InputContract.Parameters.Count;
            m_ContributionCapacity = contributionCapacity;
            if (m_BoneCount <= 0 || m_ParameterCount <= 0)
                throw new ArgumentException(
                    "Native Final Pose publication layout is empty.");
            m_BoneKinds = new CharacterPoseBoneKind[m_BoneCount];
            for (int i = 0; i < m_BoneKinds.Length; i++)
                m_BoneKinds[i] = m_Rig.GetPoseBoneKind(i);
            for (int i = 0; i < m_ParameterCount; i++)
            {
                CharacterPoseParameterDeclaration parameter =
                    preparedBinding.InputContract.Parameters[i];
                if (parameter == null || parameter.ParameterId.IsValid == false ||
                    !float.IsFinite(parameter.DefaultValue))
                {
                    throw new ArgumentException(
                        $"Native Final Pose publication parameter #{i} is invalid.");
                }
            }
            m_DenseLocalPoses = new AnimationLocalBonePose[checked(2 * m_BoneCount)];
            m_PoseParameters = new float[checked(2 * m_ParameterCount)];
            m_PoseParameterAvailability = new byte[checked(2 * m_ParameterCount)];
            m_Contributions = new AnimationPoseSourceContribution[
                checked(2 * m_ContributionCapacity)];
            m_DenseContributionWeights = new float[
                checked(2 * m_ContributionCapacity * m_BoneCount)];
            m_PhysicalWriter = new CharacterFinalPosePhysicalWriter(
                rigBinding,
                m_Rig,
                rootHierarchy);
            m_PropertyWriter = new CharacterFinalPosePropertyWriter(
                rigBinding,
                preparedBinding.InputContract,
                animationProperties);
        }

        internal void ValidateBindingsBeforeEvaluate()
        {
            RequireAlive();
            m_PhysicalWriter.ValidateBindingsBeforeEvaluate(
                m_HasCommitted,
                in m_CommittedFrame);
            m_PropertyWriter.ValidateBindingsBeforeEvaluate();
        }

        internal CharacterPoseNativePublicationFrameLease BeginFrame(
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (!lineage.IsValid || m_Pending.IsOpen)
                throw new ArgumentException(
                    "Native Final Pose publication frame cannot begin.",
                    nameof(lineage));
            var lease = new CharacterPoseNativePublicationFrameLease(in lineage);
            int page = m_CommittedPage < 0 ? 0 : 1 - m_CommittedPage;
            m_PageLeases[page].BeginWrite(lineage.CompletionIdentity);
            m_Pending.Begin(lease, page);
            return lease;
        }

        internal void PreparePending(
            CharacterPoseNativePublicationFrameLease lease,
            in CharacterPoseNativeEvaluationResult evaluation)
        {
            RequireAlive();
            m_Pending.RequireLease(lease);
            if (!evaluation.IsValid ||
                evaluation.Status != CharacterPoseNativeFrameStatus.Evaluated ||
                evaluation.Lineage != lease.Lineage ||
                !(evaluation.Output is CharacterPoseNativeLocalPoseValue local) ||
                !local.Native.IsValid)
            {
                throw new ArgumentException(
                    "Native Final Pose publication evaluation is invalid.",
                    nameof(evaluation));
            }
            CharacterPoseNativePoseReadBinding output = local.Native;
            if (output.CompletionIdentity != lease.Lineage.CompletionIdentity ||
                output.Space != CharacterPoseSpace.Local ||
                output.Availability[0] != AnimationPoseAvailability.Pose ||
                output.InvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                output.CompletedAt[0] != output.CompletionIdentity ||
                output.ContinuityIdentity[0] == 0 ||
                !float.IsFinite(output.OutputWeight[0]) ||
                output.OutputWeight[0] < 0f || output.OutputWeight[0] > 1f ||
                output.DenseLocalPoses.Length != m_BoneCount ||
                output.PoseParameters.Length != m_ParameterCount ||
                output.PoseParameterAvailability.Length != m_ParameterCount ||
                output.Contributions.Length > m_ContributionCapacity ||
                output.DenseContributionWeights.Length !=
                    output.Contributions.Length * m_BoneCount)
            {
                throw new InvalidOperationException(
                    "Native Final Pose publication output layout is invalid.");
            }
            int contributionCount = output.ContributionCount[0];
            if (contributionCount <= 0 || contributionCount > output.Contributions.Length)
                throw new InvalidOperationException(
                    "Native Final Pose publication contribution count is invalid.");
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!output.DenseLocalPoses[bone].IsValid)
                    throw new InvalidOperationException(
                        $"Native Final Pose publication Bone #{bone} is invalid.");
                m_DenseLocalPoses[m_Pending.BufferPage * m_BoneCount + bone] =
                    output.DenseLocalPoses[bone];
            }
            int parameterOffset = m_Pending.BufferPage * m_ParameterCount;
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = output.PoseParameters[parameter];
                byte available = output.PoseParameterAvailability[parameter];
                if (!float.IsFinite(value) || available > 1)
                    throw new InvalidOperationException(
                        $"Native Final Pose publication parameter #{parameter} is invalid.");
                m_PoseParameters[parameterOffset + parameter] = value;
                m_PoseParameterAvailability[parameterOffset + parameter] = available;
            }
            int contributionOffset = m_Pending.BufferPage * m_ContributionCapacity;
            int denseWeightOffset = contributionOffset * m_BoneCount;
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                m_Contributions[contributionOffset + contribution] =
                    CharacterFinalPoseContributionResolver.Resolve(
                        output.Contributions[contribution],
                        m_SourceModule,
                        m_PlayerNodeIds);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float weight = output.DenseContributionWeights[
                        contribution * m_BoneCount + bone];
                    if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                        throw new InvalidOperationException(
                            $"Native Final Pose publication contribution #{contribution} Bone #{bone} weight is invalid.");
                    m_DenseContributionWeights[
                        denseWeightOffset + contribution * m_BoneCount + bone] = weight;
                }
            }
            byte hasFootFeatures = output.HasFootFeatures[0];
            AnimationFootFeatureSample left = hasFootFeatures == 1
                ? output.LeftFootFeatures[0]
                : default;
            AnimationFootFeatureSample right = hasFootFeatures == 1
                ? output.RightFootFeatures[0]
                : default;
            if (hasFootFeatures > 1 ||
                hasFootFeatures == 1 && (!left.IsValid || !right.IsValid))
            {
                throw new InvalidOperationException(
                    "Native Final Pose publication foot features are invalid.");
            }
            FinalAnimationPoseFramePageLease pageLease =
                m_PageLeases[m_Pending.BufferPage];
            int poseOffset = m_Pending.BufferPage * m_BoneCount;
            var frame = new ComposedAnimationPoseFrame(
                m_PoseGraphId,
                m_GraphRevision,
                output.CompletionIdentity,
                AnimationPoseAvailability.Pose,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                    m_DenseLocalPoses,
                    poseOffset,
                    m_BoneCount,
                    pageLease,
                    output.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_PoseParameters,
                    parameterOffset,
                    m_ParameterCount,
                    pageLease,
                    output.CompletionIdentity),
                new AnimationReadOnlyBuffer<byte>(
                    m_PoseParameterAvailability,
                    parameterOffset,
                    m_ParameterCount,
                    pageLease,
                    output.CompletionIdentity),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                    m_Contributions,
                    contributionOffset,
                    contributionCount,
                    pageLease,
                    output.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_DenseContributionWeights,
                    denseWeightOffset,
                    contributionCount * m_BoneCount,
                    pageLease,
                    output.CompletionIdentity),
                new AnimationReadOnlyBuffer<CharacterPoseBoneKind>(
                    m_BoneKinds,
                    0,
                    m_BoneKinds.Length,
                    pageLease,
                    output.CompletionIdentity),
                m_Rig.PhysicalBoneCount,
                m_Rig.VirtualBoneCount,
                m_BoneCount,
                left,
                right,
                hasFootFeatures == 1,
                output.ContinuityIdentity[0],
                pageLease,
                output.CompletionIdentity);
            m_Pending.SetOutput(lease, in output, in frame);
        }

        internal void WritePhysicalPose(
            CharacterPoseNativePublicationFrameLease lease,
            bool captureFootIkDiagnostics)
        {
            RequireAlive();
            m_Pending.RequireFrame(lease);
            m_PropertyWriter.ValidateFrame(in m_Pending.Frame);
            m_PhysicalWriter.WriteNative(
                in m_Pending.Output,
                in m_Pending.Frame,
                m_HasCommitted,
                in m_CommittedFrame,
                captureFootIkDiagnostics);
            m_Pending.PhysicalWrite = m_PhysicalWriter.Diagnostics;
            if (!m_Pending.PhysicalWrite.IsAvailable ||
                m_Pending.PhysicalWrite.CompletionIdentity !=
                lease.Lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Native Final Pose publication physical write is incomplete.");
            }
            m_PropertyWriter.Write(in m_Pending.Frame);
        }

        internal CharacterPoseNativePublicationResult Commit(
            CharacterPoseNativePublicationFrameLease lease)
        {
            RequireAlive();
            m_Pending.RequireFrame(lease);
            if (!m_Pending.PhysicalWrite.IsAvailable ||
                m_Pending.PhysicalWrite.CompletionIdentity !=
                lease.Lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Native Final Pose publication cannot commit before physical write.");
            }
            m_CommittedPage = m_Pending.BufferPage;
            m_CommittedFrame = m_Pending.Frame;
            m_CommittedPhysicalWrite = m_Pending.PhysicalWrite;
            m_HasCommitted = true;
            CharacterPoseNativeFrameLineage lineage = lease.Lineage;
            m_Pending.Clear();
            return new CharacterPoseNativePublicationResult(
                in lineage,
                CharacterPoseNativeFrameStatus.Committed,
                CharacterPoseNativeFailureCode.None,
                lineage.CompletionIdentity,
                "Pose/FinalPublication",
                "Native final Pose was published.");
        }

        internal void Discard(
            CharacterPoseNativePublicationFrameLease lease)
        {
            RequireAlive();
            m_Pending.RequireLease(lease);
            if (m_Pending.BufferPage >= 0)
                m_PageLeases[m_Pending.BufferPage].Invalidate();
            m_Pending.Clear();
        }

        internal bool TryGetCommittedFrame(
            out ComposedAnimationPoseFrame frame)
        {
            if (!m_HasCommitted)
            {
                frame = default;
                return false;
            }
            frame = m_CommittedFrame;
            return true;
        }

        internal void ResetToDefaults()
        {
            RequireAlive();
            try
            {
                m_PropertyWriter.WriteDefaults();
            }
            finally
            {
                InvalidateState();
            }
        }

        internal void RestoreInitialAndInvalidate()
        {
            RequireAlive();
            try
            {
                m_PropertyWriter.RestoreInitial();
            }
            finally
            {
                InvalidateState();
            }
        }

        void InvalidateState()
        {
            for (int i = 0; i < m_PageLeases.Length; i++)
                m_PageLeases[i].Invalidate();
            m_CommittedPage = -1;
            m_CommittedFrame = default;
            m_CommittedPhysicalWrite = default;
            m_HasCommitted = false;
            m_Pending.Clear();
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterFinalPoseNativePublication));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            InvalidateState();
            m_Disposed = true;
        }
    }
}
