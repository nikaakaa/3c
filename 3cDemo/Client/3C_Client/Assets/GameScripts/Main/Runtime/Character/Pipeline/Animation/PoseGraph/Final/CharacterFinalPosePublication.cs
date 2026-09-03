using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections.LowLevel.Unsafe;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterFinalPoseCommittedDiagnosticsView
    {
        internal CharacterFinalPoseCommittedDiagnosticsView(
            CharacterFinalPosePublication.CommittedDiagnosticsPage page)
        {
            m_Page = page ?? throw new ArgumentNullException(nameof(page));
            m_Identity = page.Identity;
            if (!IsValid)
                throw new ArgumentException(
                    "Final Pose committed diagnostics are invalid.",
                    nameof(page));
        }

        readonly CharacterFinalPosePublication.CommittedDiagnosticsPage
            m_Page;
        readonly ulong m_Identity;
        internal bool IsValid =>
            m_Page != null &&
            m_Identity != 0 &&
            m_Page.Identity == m_Identity &&
            m_Page.Result.IsPublished &&
            m_Page.ProgramOutput.IsCompleted &&
            m_Page.ProgramOutput.Lineage == m_Page.Result.Lineage &&
            m_Page.PhysicalWrite.IsAvailable;
        internal CharacterFinalPosePublicationResult Result
        {
            get
            {
                RequireValid();
                return m_Page.Result;
            }
        }
        internal CharacterPoseProgramOutputResult ProgramOutput
        {
            get
            {
                RequireValid();
                return m_Page.ProgramOutput;
            }
        }
        internal AnimationPhysicalBoneWriteDiagnostics PhysicalWrite
        {
            get
            {
                RequireValid();
                return m_Page.PhysicalWrite;
            }
        }
        internal ComposedAnimationPoseFrame Frame
        {
            get
            {
                RequireValid();
                return m_Page.Frame;
            }
        }

        void RequireValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Final Pose committed diagnostics lease is stale.");
            }
        }
    }

    internal readonly struct CharacterFinalPosePublicationOutputBinding
    {
        internal CharacterFinalPosePublicationOutputBinding(
            CharacterFinalPosePublication owner,
            CharacterFinalPosePublicationFrameLease lease,
            in CharacterFinalPosePublicationLayoutHandle layout)
        {
            m_Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            m_Lease = lease;
            Layout = layout;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Final Pose Publication output binding is invalid.");
            }
        }

        readonly CharacterFinalPosePublication m_Owner;
        readonly CharacterFinalPosePublicationFrameLease m_Lease;
        internal CharacterFinalPosePublicationLayoutHandle Layout { get; }
        internal bool IsValid =>
            m_Owner != null &&
            m_Owner.OutputBindingIsValid(this);
        internal ulong CompletionIdentity =>
            m_Lease.Lineage.CompletionIdentity;
        internal bool HasOutput =>
            IsValid && m_Owner.ProgramOutputIsWritten(this);
        internal CharacterFinalPosePublication Owner => m_Owner;
        internal CharacterFinalPosePublicationFrameLease Lease => m_Lease;

        internal void WritePose(
            in AnimationPoseValueNativeReadBinding input,
            float outputWeight,
            ulong continuityIdentity) =>
            m_Owner.WriteProgramOutputPose(
                this,
                in input,
                outputWeight,
                continuityIdentity);

        internal void WriteInvalid(
            AnimationPoseNativeInvalidReason outputInvalidReason,
            ulong continuityIdentity) =>
            m_Owner.WriteInvalidProgramOutput(
                this,
                outputInvalidReason,
                continuityIdentity);

        internal CharacterPoseProgramOutputResult Complete(
            AnimationPoseNativeInvalidReason graphInvalidReason,
            int invalidOperationIndex) =>
            m_Owner.CompleteProgramOutput(
                this,
                graphInvalidReason,
                invalidOperationIndex);
    }

    internal sealed class CharacterFinalPosePublication
    {
        sealed class CharacterFinalPosePublicationPendingPage
        {
            internal CharacterFinalPosePublicationFrameLease Lease;
            internal int BufferPage = -1;
            internal CharacterFinalPosePublicationResult Result;
            internal CharacterPoseProgramOutputResult ProgramOutput;
            internal AnimationPhysicalBoneWriteDiagnostics PhysicalWrite;
            internal AnimationPresentationDiagnosticsInterest
                DiagnosticsInterest;
            internal bool CaptureFootIkDiagnostics;
            internal ComposedAnimationPoseFrame Frame;
            internal AnimationPoseAvailability OutputAvailability;
            internal AnimationPoseNativeInvalidReason OutputInvalidReason;
            internal float OutputWeight;
            internal ulong OutputContinuityIdentity;
            internal bool HasProgramOutput;
            internal bool HasValue;
            internal bool IsOpen => Lease.IsValid;

            internal void Begin(
                CharacterFinalPosePublicationFrameLease lease,
                AnimationPresentationDiagnosticsInterest diagnosticsInterest,
                bool captureFootIkDiagnostics)
            {
                if (IsOpen || !lease.IsValid)
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication Pending page is already open or its lease is invalid.");
                }
                Lease = lease;
                BufferPage = -1;
                Result = default;
                ProgramOutput = default;
                PhysicalWrite = default;
                DiagnosticsInterest = diagnosticsInterest;
                CaptureFootIkDiagnostics = captureFootIkDiagnostics;
                Frame = default;
                OutputAvailability = default;
                OutputInvalidReason = default;
                OutputWeight = 0f;
                OutputContinuityIdentity = 0;
                HasProgramOutput = false;
                HasValue = false;
            }

            internal void BeginWrite(
                CharacterFinalPosePublicationFrameLease lease,
                int bufferPage)
            {
                RequireLease(lease);
                if (HasValue || BufferPage >= 0 ||
                    bufferPage < 0 || bufferPage > 1)
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication Pending page cannot begin its write.");
                }
                BufferPage = bufferPage;
            }

            internal void SetProgramOutput(
                CharacterFinalPosePublicationFrameLease lease,
                AnimationPoseAvailability availability,
                AnimationPoseNativeInvalidReason invalidReason,
                float outputWeight,
                ulong continuityIdentity,
                in ComposedAnimationPoseFrame frame)
            {
                RequireLease(lease);
                if (HasProgramOutput || HasValue || BufferPage < 0 ||
                    frame.CompletionIdentity !=
                    lease.Lineage.CompletionIdentity ||
                    continuityIdentity == 0 ||
                    !float.IsFinite(outputWeight))
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication Program output is invalid.");
                }
                Frame = frame;
                OutputAvailability = availability;
                OutputInvalidReason = invalidReason;
                OutputWeight = outputWeight;
                OutputContinuityIdentity = continuityIdentity;
                HasProgramOutput = true;
            }

            internal void CompleteProgramOutput(
                CharacterFinalPosePublicationFrameLease lease,
                in CharacterPoseProgramOutputResult output)
            {
                RequireLease(lease);
                if (!HasProgramOutput || ProgramOutput.IsValid ||
                    !output.IsValid ||
                    !lease.Matches(output.Lineage) ||
                    output.Availability != OutputAvailability ||
                    output.OutputInvalidReason != OutputInvalidReason ||
                    output.OutputWeight != OutputWeight ||
                    output.ContinuityIdentity != OutputContinuityIdentity)
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication Program output result is invalid.");
                }
                ProgramOutput = output;
            }

            internal void Prepare(
                CharacterFinalPosePublicationFrameLease lease,
                in CharacterPoseProgramOutputResult output,
                in CharacterFinalPosePublicationResult result)
            {
                RequireLease(lease);
                if (HasValue || !HasProgramOutput ||
                    !ProgramOutput.IsValid ||
                    !output.IsValid ||
                    ProgramOutput.Lineage != output.Lineage ||
                    !ProgramOutput.Layout.Equals(output.Layout) ||
                    ProgramOutput.Availability != output.Availability ||
                    ProgramOutput.OutputInvalidReason !=
                    output.OutputInvalidReason ||
                    ProgramOutput.GraphInvalidReason !=
                    output.GraphInvalidReason ||
                    ProgramOutput.InvalidOperationIndex !=
                    output.InvalidOperationIndex ||
                    ProgramOutput.OutputWeight != output.OutputWeight ||
                    ProgramOutput.ContinuityIdentity !=
                    output.ContinuityIdentity ||
                    !result.IsValid ||
                    !lease.Matches(result.Lineage))
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication Pending result is invalid.");
                }
                Result = result;
                HasValue = true;
            }

            internal void RequirePrepared(
                CharacterFinalPosePublicationFrameLease lease)
            {
                RequireLease(lease);
                if (!HasValue || !HasProgramOutput ||
                    !ProgramOutput.IsValid || BufferPage < 0 ||
                    !Result.IsValid)
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication Pending page is incomplete.");
                }
            }

            internal void RequireReady(
                CharacterFinalPosePublicationFrameLease lease)
            {
                RequirePrepared(lease);
                if (Result.IsPublished &&
                    (!PhysicalWrite.IsAvailable ||
                     PhysicalWrite.CompletionIdentity !=
                     Result.Lineage.CompletionIdentity))
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication physical write is incomplete.");
                }
            }

            internal void RequireLease(
                CharacterFinalPosePublicationFrameLease lease)
            {
                if (!IsOpen || !lease.IsValid ||
                    Lease.Lineage != lease.Lineage)
                {
                    throw new InvalidOperationException(
                        "Final Pose Publication Pending lease is stale.");
                }
            }

            internal void Clear()
            {
                Lease = default;
                BufferPage = -1;
                Result = default;
                ProgramOutput = default;
                PhysicalWrite = default;
                DiagnosticsInterest =
                    AnimationPresentationDiagnosticsInterest.None;
                CaptureFootIkDiagnostics = false;
                Frame = default;
                OutputAvailability = default;
                OutputInvalidReason = default;
                OutputWeight = 0f;
                OutputContinuityIdentity = 0;
                HasProgramOutput = false;
                HasValue = false;
            }
        }

        internal sealed class CommittedDiagnosticsPage
        {
            internal ulong Identity;
            internal CharacterFinalPosePublicationResult Result;
            internal CharacterPoseProgramOutputResult ProgramOutput;
            internal AnimationPhysicalBoneWriteDiagnostics PhysicalWrite;
            internal ComposedAnimationPoseFrame Frame;
        }

        readonly string m_PoseGraphId;
        readonly string m_PosePlanHash;
        readonly string m_RigId;
        readonly string m_RigRevision;
        readonly CharacterFinalPosePublicationLayoutHandle m_Layout;
        readonly PoseNodeId[] m_PoseNodeIds;
        readonly float[] m_ParameterDefaults;
        readonly AnimationLocalBonePose[] m_DenseLocalPoses;
        readonly float[] m_PoseParameters;
        readonly byte[] m_PoseParameterAvailability;
        readonly AnimationPoseSourceContribution[] m_Contributions;
        readonly float[] m_DenseContributionWeights;
        readonly CharacterPoseBoneKind[] m_BoneKinds;
        readonly FinalAnimationPoseFramePageLease[] m_PageLeases;
        readonly CharacterFinalPosePhysicalWriter m_PhysicalWriter;
        readonly CharacterPoseSourceModule m_SourceModule;
        readonly CommittedDiagnosticsPage m_CommittedDiagnostics =
            new CommittedDiagnosticsPage();
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_ContributionCapacity;
        readonly int m_PhysicalBoneCount;
        readonly int m_VirtualBoneCount;
        readonly long m_DenseDoublePageResidentPayloadBytes;
        readonly CharacterFinalPosePublicationPendingPage m_Pending =
            new CharacterFinalPosePublicationPendingPage();
        int m_CommittedPage = -1;
        ulong m_LastCommittedCompletionIdentity;
        ulong m_NextDiagnosticsIdentity = 1;
        CharacterFinalPosePublicationResult m_CommittedResult;
        CharacterPoseProgramOutputResult m_CommittedProgramOutput;
        AnimationPhysicalBoneWriteDiagnostics m_CommittedPhysicalWrite;
        ComposedAnimationPoseFrame m_CommittedFrame;

        internal CharacterFinalPosePublication(
            CharacterPoseProgramImage program,
            CharacterAnimationRigPayload rig,
            CharacterAnimationRigBinding rigBinding,
            CharacterRootHierarchyBinding rootHierarchy,
            CharacterPoseSourceModule sourceModule)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            m_PhysicalWriter = new CharacterFinalPosePhysicalWriter(
                rigBinding,
                rig,
                rootHierarchy);
            program.RequireValid();
            rig.RequireValid();
            CharacterFinalPosePublicationLayoutHandle layout =
                program.FinalPosePublicationLayout;
            layout.RequireValid();
            m_Layout = layout;
            m_SourceModule = sourceModule ??
                throw new ArgumentNullException(nameof(sourceModule));
            if (!string.Equals(program.RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(program.RigRevision, rig.RigRevision, StringComparison.Ordinal) ||
                layout.BoneCount != rig.PoseBoneCount ||
                layout.ParameterCount != program.Parameters.Count ||
                layout.PoseValueCount != program.PoseValueCount ||
                layout.OutputOperationIndex != program.OutputOperationIndex)
            {
                throw new InvalidOperationException("Final Animation Pose Frame Rig layout is invalid.");
            }
            int contributionCapacity = layout.ContributionCapacity;
            m_PoseGraphId = program.PoseGraphId;
            m_PosePlanHash = program.PlanHash;
            m_RigId = program.RigId;
            m_RigRevision = program.RigRevision;
            m_PoseNodeIds = new PoseNodeId[program.PlayerCount];
            for (int i = 0; i < program.OperationHeaders.Count; i++)
            {
                CharacterPoseOperationHeader operation =
                    program.OperationHeaders[i];
                if (operation.Code != CharacterPoseOperationCode.SelectedPosePlayer &&
                    operation.Code != CharacterPoseOperationCode.BlendStack &&
                    operation.Code != CharacterPoseOperationCode.BlendSpacePlayer &&
                    operation.Code != CharacterPoseOperationCode.ClipPlayer &&
                    operation.Code != CharacterPoseOperationCode.AnimationSlot)
                    continue;
                int playerIndex = operation.Family switch
                {
                    CharacterPoseOperationFamily.Player =>
                        ((CharacterPosePlayerOperationPayload)
                            program.OperationPages.RequirePayload(operation))
                        .PlayerIndex,
                    CharacterPoseOperationFamily.Blend =>
                        ((CharacterPoseBlendOperationPayload)
                            program.OperationPages.RequirePayload(operation))
                        .PlayerIndex,
                    CharacterPoseOperationFamily.AnimationSlot =>
                        ((CharacterPoseAnimationSlotOperationPayload)
                            program.OperationPages.RequirePayload(operation))
                        .PlayerIndex,
                    _ => -1
                };
                if (playerIndex < 0 || playerIndex >= m_PoseNodeIds.Length ||
                    m_PoseNodeIds[playerIndex].IsValid)
                    throw new InvalidOperationException($"Composed Animation Pose Player #{playerIndex} is invalid.");
                m_PoseNodeIds[playerIndex] = operation.NodeId;
            }

            m_ParameterDefaults = new float[program.Parameters.Count];
            for (int i = 0; i < m_ParameterDefaults.Length; i++)
            {
                CharacterPresentationPoseParameterEntry parameter = program.Parameters[i];
                if (parameter == null || parameter.Index != i || !float.IsFinite(parameter.DefaultValue))
                    throw new InvalidOperationException($"Final Animation Pose Frame Parameter #{i} is invalid.");
                m_ParameterDefaults[i] = parameter.DefaultValue;
            }

            m_BoneCount = layout.BoneCount;
            m_PhysicalBoneCount = rig.PhysicalBoneCount;
            m_VirtualBoneCount = rig.VirtualBoneCount;
            m_BoneKinds = new CharacterPoseBoneKind[m_BoneCount];
            for (int i = 0; i < m_BoneKinds.Length; i++)
                m_BoneKinds[i] = rig.GetPoseBoneKind(i);
            m_ParameterCount = layout.ParameterCount;
            m_ContributionCapacity = contributionCapacity;
            m_DenseLocalPoses = new AnimationLocalBonePose[checked(2 * m_BoneCount)];
            m_PoseParameters = new float[checked(2 * m_ParameterCount)];
            m_PoseParameterAvailability = new byte[checked(2 * m_ParameterCount)];
            m_Contributions = new AnimationPoseSourceContribution[checked(2 * m_ContributionCapacity)];
            m_DenseContributionWeights = new float[checked(2 * m_ContributionCapacity * m_BoneCount)];
            m_DenseDoublePageResidentPayloadBytes = checked(
                PayloadBytes(m_DenseLocalPoses) +
                PayloadBytes(m_PoseParameters) +
                PayloadBytes(m_PoseParameterAvailability) +
                PayloadBytes(m_DenseContributionWeights));
            m_PageLeases = new[]
            {
                new FinalAnimationPoseFramePageLease(),
                new FinalAnimationPoseFramePageLease()
            };
        }

        internal long DenseDoublePageResidentPayloadBytes =>
            m_DenseDoublePageResidentPayloadBytes;

        internal CharacterFinalPosePublicationFrameLease BeginFrame(
            in CharacterPoseFrameLineage lineage,
            AnimationPresentationDiagnosticsInterest diagnosticsInterest,
            bool captureFootIkDiagnostics)
        {
            var lease =
                new CharacterFinalPosePublicationFrameLease(in lineage);
            m_Pending.Begin(
                lease,
                diagnosticsInterest,
                captureFootIkDiagnostics);
            return lease;
        }

        internal CharacterFinalPosePublicationOutputBinding BindProgramOutput(
            CharacterFinalPosePublicationFrameLease lease)
        {
            m_Pending.RequireLease(lease);
            int page = (m_CommittedPage + 1) & 1;
            m_Pending.BeginWrite(lease, page);
            m_PageLeases[page].BeginWrite(
                lease.Lineage.CompletionIdentity);
            return new CharacterFinalPosePublicationOutputBinding(
                this,
                lease,
                in m_Layout);
        }

        internal bool OutputBindingIsValid(
            in CharacterFinalPosePublicationOutputBinding binding) =>
            ReferenceEquals(binding.Owner, this) &&
            binding.Layout.Equals(m_Layout) &&
            m_Pending.IsOpen &&
            m_Pending.BufferPage >= 0 &&
            m_Pending.Lease.Lineage == binding.Lease.Lineage;

        internal bool ProgramOutputIsWritten(
            in CharacterFinalPosePublicationOutputBinding binding) =>
            OutputBindingIsValid(in binding) &&
            m_Pending.HasProgramOutput;

        internal void ValidateWriterBeforeEvaluate(
            in CharacterFinalPosePublicationOutputBinding binding)
        {
            RequireOutputBinding(in binding);
            m_PhysicalWriter.ValidateBindingsBeforeEvaluate(
                HasCommittedPhysicalPose,
                in m_CommittedFrame);
        }

        internal void WritePhysicalPose(
            CharacterFinalPosePublicationFrameLease lease)
        {
            m_Pending.RequirePrepared(lease);
            m_PhysicalWriter.Write(
                in m_Pending.ProgramOutput,
                in m_Pending.Frame,
                HasCommittedPhysicalPose,
                in m_CommittedFrame,
                m_Pending.CaptureFootIkDiagnostics);
            m_Pending.PhysicalWrite = m_PhysicalWriter.Diagnostics;
        }

        internal void WriteProgramOutputPose(
            in CharacterFinalPosePublicationOutputBinding output,
            in AnimationPoseValueNativeReadBinding input,
            float outputWeight,
            ulong continuityIdentity)
        {
            RequireOutputBinding(in output);
            if (m_Pending.HasProgramOutput ||
                input.CompletionIdentity != output.CompletionIdentity ||
                input.DensePoses.Length != m_BoneCount ||
                input.PoseParameters.Length != m_ParameterCount ||
                input.PoseParameterAvailability.Length != m_ParameterCount ||
                input.Contributions.Length != m_ContributionCapacity ||
                input.DenseContributionWeights.Length !=
                checked(m_ContributionCapacity * m_BoneCount) ||
                input.Availability[0] != AnimationPoseAvailability.Pose ||
                input.InvalidReason[0] !=
                AnimationPoseNativeInvalidReason.None ||
                !float.IsFinite(outputWeight) || outputWeight < 0f ||
                outputWeight > 1f || continuityIdentity == 0)
            {
                throw new ArgumentException(
                    "Final Pose Program output Pose input is invalid.");
            }
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                if (!input.DensePoses[bone].IsValid)
                {
                    throw new InvalidOperationException(
                        $"Final Pose Program output Bone #{bone} is invalid.");
                }
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                if (!float.IsFinite(input.PoseParameters[parameter]) ||
                    input.PoseParameterAvailability[parameter] > 1)
                {
                    throw new InvalidOperationException(
                        $"Final Pose Program output Parameter #{parameter} is invalid.");
                }
            }
            int contributionCount = input.ContributionCount[0];
            if (contributionCount <= 0 ||
                contributionCount > m_ContributionCapacity)
            {
                throw new InvalidOperationException(
                    "Final Pose Program output contribution count is invalid.");
            }
            for (int contribution = 0;
                 contribution < contributionCount;
                 contribution++)
            {
                ExpandContribution(
                    input.Contributions[contribution],
                    m_SourceModule);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float weight = input.DenseContributionWeights[
                        contribution * m_BoneCount + bone];
                    if (!IsWeight(weight))
                    {
                        throw new InvalidOperationException(
                            $"Final Pose Program output contribution #{contribution} Bone #{bone} weight is invalid.");
                    }
                }
            }
            byte hasFootFeatures = input.HasFootFeatures[0];
            AnimationFootFeatureSample left = hasFootFeatures == 1
                ? input.LeftFootFeatures[0]
                : default;
            AnimationFootFeatureSample right = hasFootFeatures == 1
                ? input.RightFootFeatures[0]
                : default;
            if (hasFootFeatures > 1 ||
                hasFootFeatures == 1 && (!left.IsValid || !right.IsValid))
            {
                throw new InvalidOperationException(
                    "Final Pose Program output Foot Features are invalid.");
            }

            int page = m_Pending.BufferPage;
            int poseOffset = checked(page * m_BoneCount);
            int parameterOffset = checked(page * m_ParameterCount);
            int contributionOffset = checked(page * m_ContributionCapacity);
            int denseWeightOffset = checked(contributionOffset * m_BoneCount);
            for (int bone = 0; bone < m_BoneCount; bone++)
                m_DenseLocalPoses[poseOffset + bone] = input.DensePoses[bone];
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                m_PoseParameters[parameterOffset + parameter] =
                    input.PoseParameters[parameter];
                m_PoseParameterAvailability[parameterOffset + parameter] =
                    input.PoseParameterAvailability[parameter];
            }
            for (int contribution = 0;
                 contribution < contributionCount;
                 contribution++)
            {
                m_Contributions[contributionOffset + contribution] =
                    ExpandContribution(
                        input.Contributions[contribution],
                        m_SourceModule);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    m_DenseContributionWeights[
                        denseWeightOffset +
                        contribution * m_BoneCount + bone] =
                        input.DenseContributionWeights[
                            contribution * m_BoneCount + bone];
                }
            }
            FinalAnimationPoseFramePageLease pageLease = m_PageLeases[page];
            var frame = new ComposedAnimationPoseFrame(
                m_PoseGraphId,
                m_PosePlanHash,
                input.CompletionIdentity,
                AnimationPoseAvailability.Pose,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                    m_DenseLocalPoses,
                    poseOffset,
                    m_BoneCount,
                    pageLease,
                    input.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_PoseParameters,
                    parameterOffset,
                    m_ParameterCount,
                    pageLease,
                    input.CompletionIdentity),
                new AnimationReadOnlyBuffer<byte>(
                    m_PoseParameterAvailability,
                    parameterOffset,
                    m_ParameterCount,
                    pageLease,
                    input.CompletionIdentity),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                    m_Contributions,
                    contributionOffset,
                    contributionCount,
                    pageLease,
                    input.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_DenseContributionWeights,
                    denseWeightOffset,
                    checked(contributionCount * m_BoneCount),
                    pageLease,
                    input.CompletionIdentity),
                new AnimationReadOnlyBuffer<CharacterPoseBoneKind>(
                    m_BoneKinds,
                    0,
                    m_BoneKinds.Length,
                    pageLease,
                    input.CompletionIdentity),
                m_PhysicalBoneCount,
                m_VirtualBoneCount,
                m_BoneCount,
                left,
                right,
                hasFootFeatures == 1,
                continuityIdentity,
                pageLease,
                input.CompletionIdentity);
            m_Pending.SetProgramOutput(
                output.Lease,
                AnimationPoseAvailability.Pose,
                AnimationPoseNativeInvalidReason.None,
                outputWeight,
                continuityIdentity,
                in frame);
        }

        internal void WriteInvalidProgramOutput(
            in CharacterFinalPosePublicationOutputBinding output,
            AnimationPoseNativeInvalidReason outputInvalidReason,
            ulong continuityIdentity)
        {
            RequireOutputBinding(in output);
            outputInvalidReason =
                AnimationPoseNativeInvalidReasonContract.NormalizeFailure(
                    outputInvalidReason);
            int page = m_Pending.BufferPage;
            int poseOffset = checked(page * m_BoneCount);
            int parameterOffset = checked(page * m_ParameterCount);
            int contributionOffset = checked(page * m_ContributionCapacity);
            int denseWeightOffset = checked(contributionOffset * m_BoneCount);
            Array.Copy(
                m_ParameterDefaults,
                0,
                m_PoseParameters,
                parameterOffset,
                m_ParameterCount);
            Array.Clear(
                m_PoseParameterAvailability,
                parameterOffset,
                m_ParameterCount);
            FinalAnimationPoseFramePageLease pageLease = m_PageLeases[page];
            var frame = new ComposedAnimationPoseFrame(
                m_PoseGraphId,
                m_PosePlanHash,
                output.CompletionIdentity,
                AnimationPoseAvailability.Invalid,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                    m_DenseLocalPoses,
                    poseOffset,
                    0,
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
                    0,
                    pageLease,
                    output.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_DenseContributionWeights,
                    denseWeightOffset,
                    0,
                    pageLease,
                    output.CompletionIdentity),
                new AnimationReadOnlyBuffer<CharacterPoseBoneKind>(
                    m_BoneKinds,
                    0,
                    m_BoneKinds.Length,
                    pageLease,
                    output.CompletionIdentity),
                m_PhysicalBoneCount,
                m_VirtualBoneCount,
                m_BoneCount,
                default,
                default,
                false,
                continuityIdentity,
                pageLease,
                output.CompletionIdentity);
            m_Pending.SetProgramOutput(
                output.Lease,
                AnimationPoseAvailability.Invalid,
                outputInvalidReason,
                0f,
                continuityIdentity,
                in frame);
        }

        internal CharacterPoseProgramOutputResult CompleteProgramOutput(
            in CharacterFinalPosePublicationOutputBinding output,
            AnimationPoseNativeInvalidReason graphInvalidReason,
            int invalidOperationIndex)
        {
            RequireOutputBinding(in output);
            if (!m_Pending.HasProgramOutput ||
                m_Pending.ProgramOutput.IsValid)
            {
                throw new InvalidOperationException(
                    "Final Pose Program output cannot complete.");
            }
            CharacterPoseFrameLineage lineage = output.Lease.Lineage;
            var result = new CharacterPoseProgramOutputResult(
                in lineage,
                in m_Layout,
                m_Pending.OutputAvailability,
                m_Pending.OutputInvalidReason,
                graphInvalidReason,
                invalidOperationIndex,
                m_Pending.OutputWeight,
                m_Pending.OutputContinuityIdentity);
            if (!result.IsValid)
            {
                throw new InvalidOperationException(
                    "Final Pose Program output result is inconsistent.");
            }
            m_Pending.CompleteProgramOutput(
                output.Lease,
                in result);
            return result;
        }

        void RequireOutputBinding(
            in CharacterFinalPosePublicationOutputBinding binding)
        {
            if (!OutputBindingIsValid(in binding))
            {
                throw new InvalidOperationException(
                    "Final Pose Publication output binding is stale.");
            }
        }

        bool HasCommittedPhysicalPose =>
            m_CommittedResult.IsPublished &&
            m_CommittedPhysicalWrite.IsAvailable &&
            m_CommittedPhysicalWrite.CompletionIdentity ==
            m_CommittedResult.Lineage.CompletionIdentity &&
            m_CommittedFrame.CompletionIdentity ==
            m_CommittedResult.Lineage.CompletionIdentity;

        void RequirePhysicalWrite(
            CharacterFinalPosePublicationFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            in CharacterPoseProgramResult programResult,
            in CharacterPoseConstraintResult constraintResult,
            in CharacterPoseProgramOutputResult output)
        {
            m_Pending.RequireLease(lease);
            if (!lineage.IsValid ||
                !lease.Matches(lineage) ||
                !programResult.IsValid ||
                !constraintResult.IsValid ||
                programResult.Lineage != lineage ||
                constraintResult.Lineage != lineage ||
                output.Lineage != lineage ||
                !output.Layout.Equals(m_Layout) ||
                programResult.Outcome != constraintResult.Outcome ||
                programResult.IsCompleted &&
                (!constraintResult.IsCompleted || !output.IsCompleted) ||
                programResult.OutputAvailability != output.Availability ||
                programResult.OutputInvalidReason !=
                    output.OutputInvalidReason ||
                programResult.GraphInvalidReason !=
                    output.GraphInvalidReason ||
                programResult.InvalidOperationIndex !=
                    output.InvalidOperationIndex ||
                !string.Equals(
                    lineage.RigId,
                    m_RigId,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    lineage.RigRevision,
                    m_RigRevision,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Final Pose physical write inputs are inconsistent.");
            }
        }

        internal CharacterFinalPosePublicationResult PreparePending(
            CharacterFinalPosePublicationFrameLease lease,
            in CharacterPoseFrameLineage lineage,
            in CharacterPoseProgramResult programResult,
            in CharacterPoseConstraintResult constraintResult,
            in CharacterPoseProgramOutputResult output)
        {
            RequirePhysicalWrite(
                lease,
                in lineage,
                in programResult,
                in constraintResult,
                in output);
            if (!lineage.IsValid ||
                !lease.Matches(lineage) ||
                output.Lineage != lineage)
            {
                throw new ArgumentException(
                    "Final Pose Publication completed lineage is invalid.",
                    nameof(lineage));
            }
            AnimationFinalPoseWriteOutcome writeOutcome =
                programResult.IsCompleted
                    ? AnimationFinalPoseWriteOutcome.Committed
                    : AnimationFinalPoseWriteOutcome.TypedInvalid;
            CharacterFinalPosePublicationResult result =
                CreatePublicationResult(
                    in lineage,
                    in output,
                    writeOutcome);
            ulong completionIdentity = output.Lineage.CompletionIdentity;
            if (completionIdentity <= m_LastCommittedCompletionIdentity)
                throw new InvalidOperationException("Final Animation Pose Frame completion identity did not advance.");
            if (!output.IsValid ||
                output.Availability == AnimationPoseAvailability.NoPose ||
                output.ContinuityIdentity == 0)
            {
                throw new InvalidOperationException("Final Animation Pose Graph output header is invalid.");
            }
            m_Pending.Prepare(
                lease,
                in output,
                in result);
            return result;
        }

        internal int ResolveContributions(
            in AnimationPoseValueNativeReadBinding binding,
            CharacterPoseSourceModule sourceModule,
            AnimationPoseSourceContribution[] destination)
        {
            if (sourceModule == null)
                throw new ArgumentNullException(nameof(sourceModule));
            int count = binding.ContributionCount[0];
            if (binding.CompletionIdentity == 0 ||
                binding.Contributions.Length != m_ContributionCapacity ||
                count <= 0 || count > m_ContributionCapacity ||
                destination == null || destination.Length < count)
            {
                throw new ArgumentException(
                    $"Pose Value contribution resolution input is invalid: completion={binding.CompletionIdentity}, " +
                    $"availability={(binding.Availability.Length == 0 ? default : binding.Availability[0])}, " +
                    $"reason={(binding.InvalidReason.Length == 0 ? default : binding.InvalidReason[0])}, " +
                    $"operation={(binding.PoseGraphInvalidOperationIndex.Length == 0 ? -1 : binding.PoseGraphInvalidOperationIndex[0])}, " +
                    $"contributionLength={binding.Contributions.Length}, count={count}, " +
                    $"capacity={m_ContributionCapacity}, destinationLength={destination?.Length ?? -1}.");
            }
            for (int i = 0; i < count; i++)
            {
                destination[i] = ExpandContribution(
                    binding.Contributions[i],
                    sourceModule);
            }
            return count;
        }

        internal ComposedAnimationPoseFrame
            CommitPending(
                CharacterFinalPosePublicationFrameLease lease)
        {
            m_Pending.RequireReady(lease);
            if (!m_Pending.Result.IsPublished)
            {
                throw new InvalidOperationException(
                    "Final Pose Publication Pending result cannot commit.");
            }
            m_CommittedPage = m_Pending.BufferPage;
            m_LastCommittedCompletionIdentity =
                m_Pending.Result.Lineage.CompletionIdentity;
            m_CommittedResult = m_Pending.Result;
            m_CommittedProgramOutput = m_Pending.ProgramOutput;
            m_CommittedPhysicalWrite = m_Pending.PhysicalWrite;
            ComposedAnimationPoseFrame result =
                m_Pending.Frame;
            m_CommittedFrame = result;
            if (m_Pending.DiagnosticsInterest !=
                    AnimationPresentationDiagnosticsInterest.None ||
                m_Pending.CaptureFootIkDiagnostics)
            {
                FreezeCommittedDiagnostics(in m_CommittedResult);
            }
            m_Pending.Clear();
            return result;
        }

        void FreezeCommittedDiagnostics(
            in CharacterFinalPosePublicationResult result)
        {
            if (!m_CommittedResult.IsPublished ||
                !m_CommittedProgramOutput.IsCompleted ||
                !result.IsPublished ||
                m_CommittedResult.Lineage != result.Lineage ||
                m_CommittedProgramOutput.Lineage != result.Lineage ||
                !m_CommittedPhysicalWrite.IsAvailable ||
                m_CommittedPhysicalWrite.CompletionIdentity !=
                result.Lineage.CompletionIdentity ||
                m_CommittedFrame.CompletionIdentity !=
                result.Lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Final Pose committed diagnostics request is invalid.");
            }
            CommittedDiagnosticsPage page = m_CommittedDiagnostics;
            page.Identity = 0;
            page.Result = result;
            page.ProgramOutput = m_CommittedProgramOutput;
            page.PhysicalWrite = m_CommittedPhysicalWrite;
            page.Frame = m_CommittedFrame;
            page.Identity = m_NextDiagnosticsIdentity++;
        }

        internal CharacterFinalPoseCommittedDiagnosticsView
            CaptureCommittedDiagnostics(
            in CharacterFinalPosePublicationResult result)
        {
            if (!m_CommittedResult.IsPublished ||
                !result.IsPublished ||
                m_CommittedResult.Lineage != result.Lineage ||
                m_CommittedDiagnostics.Result.Lineage != result.Lineage)
            {
                throw new InvalidOperationException(
                    "Final Pose committed diagnostics are unavailable.");
            }
            return new CharacterFinalPoseCommittedDiagnosticsView(
                m_CommittedDiagnostics);
        }

        internal ComposedAnimationPoseFrame RequireCommittedFrame(
            in CharacterFinalPosePublicationResult result)
        {
            if (!m_CommittedResult.IsPublished ||
                !result.IsPublished ||
                m_CommittedResult.Lineage != result.Lineage ||
                m_CommittedFrame.CompletionIdentity !=
                result.Lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Final Pose committed frame is unavailable.");
            }
            return m_CommittedFrame;
        }

        internal CharacterFootIkPhysicalCapture
            RequireCommittedFootIkPhysical(
            in CharacterFinalPosePublicationResult result)
        {
            if (!m_CommittedResult.IsPublished ||
                !result.IsPublished ||
                m_CommittedResult.Lineage != result.Lineage ||
                m_CommittedPhysicalWrite.CompletionIdentity !=
                result.Lineage.CompletionIdentity ||
                !m_CommittedPhysicalWrite.FootIkCapture.IsAvailable)
            {
                throw new InvalidOperationException(
                    "Final Pose committed Foot physical facts are unavailable.");
            }
            return m_CommittedPhysicalWrite.FootIkCapture;
        }

        internal static bool RequiresPhysicalDiagnostics(
            AnimationPresentationDiagnosticsInterest interest) =>
            (interest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture |
              AnimationPresentationDiagnosticsInterest.FinalPoseDetail |
              AnimationPresentationDiagnosticsInterest.PoseWatch)) != 0;

        internal void ValidatePendingSeal(
            CharacterFinalPosePublicationFrameLease lease)
        {
            m_Pending.RequireReady(lease);
            if (!m_Pending.Result.IsPublished)
            {
                throw new InvalidOperationException(
                    "Final Pose Publication Pending result cannot seal.");
            }
        }

        internal ComposedAnimationPoseFrame RequirePendingFrame(
            CharacterFinalPosePublicationFrameLease lease)
        {
            m_Pending.RequireReady(lease);
            return m_Pending.Frame;
        }

        internal void DiscardPending(
            CharacterFinalPosePublicationFrameLease lease)
        {
            m_Pending.RequireLease(lease);
            if (m_Pending.BufferPage >= 0)
                m_PageLeases[m_Pending.BufferPage].Invalidate();
            m_Pending.Clear();
        }

        internal void Invalidate()
        {
            for (int i = 0; i < m_PageLeases.Length; i++)
                m_PageLeases[i].Invalidate();
            m_CommittedPage = -1;
            m_LastCommittedCompletionIdentity = 0;
            m_CommittedResult = default;
            m_CommittedProgramOutput = default;
            m_CommittedPhysicalWrite = default;
            m_CommittedDiagnostics.Identity = 0;
            m_CommittedDiagnostics.Result = default;
            m_CommittedDiagnostics.ProgramOutput = default;
            m_CommittedDiagnostics.PhysicalWrite = default;
            m_CommittedDiagnostics.Frame = default;
            m_Pending.Clear();
            m_CommittedFrame = default;
        }

        AnimationPoseSourceContribution ExpandContribution(
            AnimationPrimitivePoseContribution primitive,
            CharacterPoseSourceModule sourceModule)
        {
            if (primitive.PhysicalPlayerIndex < 0 || primitive.PhysicalPlayerIndex >= m_PoseNodeIds.Length ||
                !IsContributionKind(primitive.Kind) ||
                primitive.ContributionContinuityIdentity == 0 ||
                !IsWeight(primitive.Weight) || !IsWeight(primitive.LeftFootWeight) ||
                !IsWeight(primitive.RightFootWeight))
            {
                throw new InvalidOperationException("Final Animation Pose Graph primitive contribution is invalid.");
            }

            PoseNodeId playerNodeId = m_PoseNodeIds[primitive.PhysicalPlayerIndex];
            AnimationPoseSourceId sourceId = default;
            if (primitive.Kind == AnimationPoseContributionKind.Live)
            {
                if (primitive.PhysicalSourceIndex < 0 || primitive.PhysicalSourceGeneration == 0 ||
                    primitive.SourceOwnerIndex < 0)
                    throw new InvalidOperationException("Final Animation Pose Graph Live contribution identity is invalid.");
                var physicalIdentity = new AnimationPhysicalSourceIdentity(
                    new AnimationPhysicalSourceIndex(primitive.PhysicalSourceIndex),
                    primitive.PhysicalSourceGeneration);
                sourceId = sourceModule.RequireSourceId(physicalIdentity);
                if (!sourceModule.RequirePoseNodeId(physicalIdentity).Equals(playerNodeId) ||
                    sourceModule.RequireSourceOwnerIndex(physicalIdentity) != primitive.SourceOwnerIndex)
                {
                    throw new InvalidOperationException("Final Animation Pose Graph Live contribution metadata does not match its physical identity.");
                }
            }
            else if (primitive.PhysicalSourceIndex != -1 || primitive.PhysicalSourceGeneration != 0 ||
                     primitive.SourceOwnerIndex != -1)
            {
                throw new InvalidOperationException("Final Animation Pose Graph captured contribution must not carry a Live source identity.");
            }

            return new AnimationPoseSourceContribution(
                playerNodeId,
                primitive.Kind,
                sourceId,
                primitive.SourceOwnerIndex,
                primitive.ContributionContinuityIdentity,
                primitive.Weight,
                primitive.LeftFootWeight,
                primitive.RightFootWeight);
        }

        static CharacterFinalPosePublicationResult CreatePublicationResult(
            in CharacterPoseFrameLineage lineage,
            in CharacterPoseProgramOutputResult output,
            AnimationFinalPoseWriteOutcome writeOutcome)
        {
            AnimationPresentationFrameOutcome outcome = writeOutcome switch
            {
                AnimationFinalPoseWriteOutcome.Committed =>
                    AnimationPresentationFrameOutcome.Committed,
                AnimationFinalPoseWriteOutcome.TypedInvalid =>
                    AnimationPresentationFrameOutcome.TypedInvalid,
                _ => throw new InvalidOperationException(
                    $"Unsupported final Pose publication outcome '{writeOutcome}'.")
            };
            return new CharacterFinalPosePublicationResult(
                in lineage,
                outcome,
                writeOutcome,
                output.Availability,
                output.OutputInvalidReason,
                output.GraphInvalidReason,
                output.InvalidOperationIndex,
                writeOutcome == AnimationFinalPoseWriteOutcome.Committed
                    ? lineage.CompletionIdentity
                    : 0);
        }

        static bool IsWeight(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
        static bool IsContributionKind(AnimationPoseContributionKind value) =>
            (int)value >= (int)AnimationPoseContributionKind.Live &&
            (int)value <= (int)AnimationPoseContributionKind.Stored;
        static long PayloadBytes<T>(T[] values) where T : unmanaged =>
            checked((long)UnsafeUtility.SizeOf<T>() * values.LongLength);
    }
}
