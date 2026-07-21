using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterPoseGraphEvaluator : IDisposable
    {
        readonly CharacterPresentationPoseProgram m_Program;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_ValueCount;
        readonly int m_ContributionCapacity;
        readonly AnimationLocalBonePose[] m_Poses;
        readonly float[] m_Parameters;
        readonly AnimationPoseSourceContribution[] m_Contributions;
        readonly float[] m_ContributionBoneWeights;
        readonly int[] m_ContributionCounts;
        readonly PoseSlotFrameAvailability[] m_Availability;
        readonly float[] m_OutputWeights;
        readonly AnimationFootFeatureSample[] m_LeftFootFeatures;
        readonly AnimationFootFeatureSample[] m_RightFootFeatures;
        readonly bool[] m_HasFootFeatures;
        readonly ulong[] m_Continuity;
        bool m_Disposed;

        public CharacterPoseGraphEvaluator(
            CharacterPresentationPoseProgram program,
            CharacterAnimationRigPayload rig)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_Program.RequireValid();
            m_Rig.RequireValid();
            if (!string.Equals(m_Program.RigId, m_Rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(m_Program.RigRevision, m_Rig.RigRevision, StringComparison.Ordinal) ||
                m_Program.BoneCount != m_Rig.Bones.Count)
            {
                throw new InvalidOperationException("Pose Program and compiled Rig do not match.");
            }
            if (m_Program.ContributionWorkspaceCount % m_Program.PoseValueWorkspaceCount != 0)
                throw new InvalidOperationException("Pose Program contribution workspace layout is invalid.");

            m_BoneCount = m_Program.BoneCount;
            m_ParameterCount = m_Program.Parameters.Count;
            m_ValueCount = m_Program.PoseValueWorkspaceCount;
            m_ContributionCapacity = m_Program.ContributionWorkspaceCount / m_Program.PoseValueWorkspaceCount;
            if (m_ContributionCapacity <= 0)
                throw new InvalidOperationException("Pose Program contribution capacity is invalid.");
            m_Poses = new AnimationLocalBonePose[m_ValueCount * m_BoneCount];
            m_Parameters = new float[m_ValueCount * m_ParameterCount];
            m_Contributions = new AnimationPoseSourceContribution[m_ValueCount * m_ContributionCapacity];
            m_ContributionBoneWeights = new float[m_ValueCount * m_ContributionCapacity * m_BoneCount];
            m_ContributionCounts = new int[m_ValueCount];
            m_Availability = new PoseSlotFrameAvailability[m_ValueCount];
            m_OutputWeights = new float[m_ValueCount];
            m_LeftFootFeatures = new AnimationFootFeatureSample[m_ValueCount];
            m_RightFootFeatures = new AnimationFootFeatureSample[m_ValueCount];
            m_HasFootFeatures = new bool[m_ValueCount];
            m_Continuity = new ulong[m_ValueCount];
        }

        public FinalAnimationPoseFrame Evaluate(ulong completionIdentity, IReadOnlyList<PoseSlotFrame> slotFrames)
        {
            RequireAlive();
            if (completionIdentity == 0)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            if (slotFrames == null || slotFrames.Count != m_Program.Slots.Count)
                throw new ArgumentException("Pose Graph requires one frame for every compiled Pose Slot.", nameof(slotFrames));

            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                ResetValue(operation.OutputPoseValueIndex);
                switch (operation.Code)
                {
                    case CharacterPoseOperationCode.PoseSlotInput:
                        EvaluatePoseSlotInput(operation, slotFrames);
                        break;
                    case CharacterPoseOperationCode.LayeredBoneBlend:
                        EvaluateLayeredBoneBlend(operation);
                        break;
                    case CharacterPoseOperationCode.AdditivePose:
                        EvaluateAdditivePose(operation);
                        break;
                    case CharacterPoseOperationCode.PoseCurveResolve:
                        EvaluatePoseCurveResolve(operation);
                        break;
                    case CharacterPoseOperationCode.OutputPose:
                        EvaluateOutputPose(operation);
                        break;
                    default:
                        throw new InvalidOperationException($"Pose operation '{operation.Code}' is unsupported by '{CharacterPresentationPoseProgram.RuntimeAbi}'.");
                }
                RequireFiniteValue(operation.OutputPoseValueIndex, operation.NodeId);
            }

            CharacterPresentationPoseOperation output = m_Program.Operations[m_Program.OutputOperationIndex];
            int value = output.OutputPoseValueIndex;
            int poseCount = m_Availability[value] == PoseSlotFrameAvailability.Pose ? m_BoneCount : 0;
            int contributionCount = m_Availability[value] == PoseSlotFrameAvailability.Invalid ? 0 : m_ContributionCounts[value];
            return new FinalAnimationPoseFrame(
                m_Program.PoseGraphId,
                m_Program.ProgramHash,
                completionIdentity,
                m_Availability[value],
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(m_Poses, PoseOffset(value), poseCount),
                new AnimationReadOnlyBuffer<float>(m_Parameters, ParameterOffset(value), m_ParameterCount),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(m_Contributions, ContributionOffset(value), contributionCount),
                new AnimationReadOnlyBuffer<float>(m_ContributionBoneWeights, ContributionBoneOffset(value), contributionCount * m_BoneCount),
                m_LeftFootFeatures[value],
                m_RightFootFeatures[value],
                m_HasFootFeatures[value],
                m_Continuity[value]);
        }

        public void Dispose()
        {
            m_Disposed = true;
        }

        void EvaluatePoseSlotInput(CharacterPresentationPoseOperation operation, IReadOnlyList<PoseSlotFrame> slotFrames)
        {
            if ((uint)operation.PoseSlotIndex >= (uint)m_Program.Slots.Count)
                throw new InvalidOperationException($"PoseSlotInput '{operation.NodeId}' has an invalid compiled Slot index.");
            CharacterPresentationPoseSlotProgramEntry slot = m_Program.Slots[operation.PoseSlotIndex];
            PoseSlotFrame frame = slotFrames[operation.PoseSlotIndex];
            int output = operation.OutputPoseValueIndex;
            if (frame.PoseSlotId != slot.PoseSlotId)
                throw new InvalidOperationException($"PoseSlotInput '{operation.NodeId}' received Slot '{frame.PoseSlotId}' instead of '{slot.PoseSlotId}'.");
            if (frame.Availability == PoseSlotFrameAvailability.NoPose && slot.OutputPolicy == PoseSlotOutputPolicy.RequireOutput)
            {
                SetInvalid(output, frame.ContinuityIdentity);
                return;
            }
            if (frame.Availability == PoseSlotFrameAvailability.Invalid ||
                frame.Availability == PoseSlotFrameAvailability.Pose && frame.DenseLocalPose.Count != m_BoneCount ||
                frame.PoseParameters.Count != m_ParameterCount || frame.Contributions.Count > m_ContributionCapacity)
            {
                SetInvalid(output, frame.ContinuityIdentity);
                return;
            }

            m_Availability[output] = frame.Availability;
            m_OutputWeights[output] = frame.OutputWeight;
            m_Continuity[output] = RequireIdentity(frame.ContinuityIdentity);
            for (int i = 0; i < frame.DenseLocalPose.Count; i++)
                m_Poses[PoseOffset(output) + i] = frame.DenseLocalPose[i];
            for (int i = 0; i < m_ParameterCount; i++)
                m_Parameters[ParameterOffset(output) + i] = frame.PoseParameters[i];
            m_ContributionCounts[output] = frame.Contributions.Count;
            for (int i = 0; i < frame.Contributions.Count; i++)
            {
                m_Contributions[ContributionOffset(output) + i] = frame.Contributions[i];
                for (int bone = 0; bone < m_BoneCount; bone++)
                    SetContributionBoneWeight(output, i, bone, frame.GetContributionBoneWeight(i, bone));
            }
            m_LeftFootFeatures[output] = frame.LeftFootFeatures;
            m_RightFootFeatures[output] = frame.RightFootFeatures;
            m_HasFootFeatures[output] = frame.HasFootFeatures;
        }

        void EvaluateLayeredBoneBlend(CharacterPresentationPoseOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int overlayValue = operation.InputPoseValueIndexB;
            if (!RequireInputs(baseValue, overlayValue, output))
                return;
            if (m_Availability[overlayValue] == PoseSlotFrameAvailability.NoPose)
            {
                CopyValue(baseValue, output, operation.Index);
                return;
            }
            if (m_Availability[baseValue] == PoseSlotFrameAvailability.NoPose)
            {
                CopyValue(overlayValue, output, operation.Index);
                ScaleValue(output, operation, true);
                return;
            }

            CharacterPresentationDenseBoneMask mask = RequireMask(operation);
            m_Availability[output] = PoseSlotFrameAvailability.Pose;
            m_OutputWeights[output] = UnionWeight(
                m_OutputWeights[baseValue],
                m_OutputWeights[overlayValue] * operation.Weight);
            m_Continuity[output] = CombineContinuity(m_Continuity[baseValue], m_Continuity[overlayValue], operation.Index);
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float overlay = Mathf.Clamp01(GetBoneOutputWeight(overlayValue, bone) * mask.Weights[bone] * operation.Weight);
                m_Poses[PoseOffset(output) + bone] = BlendPose(
                    m_Poses[PoseOffset(baseValue) + bone],
                    m_Poses[PoseOffset(overlayValue) + bone],
                    overlay);
            }
            ResolveParameters(operation, baseValue, overlayValue, output);
            MergeContributions(baseValue, overlayValue, output, operation, false);
            ResolveFootFeatures(baseValue, overlayValue, output, operation, false);
        }

        void EvaluateAdditivePose(CharacterPresentationPoseOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int baseValue = operation.InputPoseValueIndexA;
            int additiveValue = operation.InputPoseValueIndexB;
            if (!RequireInputs(baseValue, additiveValue, output))
                return;
            if (m_Availability[additiveValue] == PoseSlotFrameAvailability.NoPose)
            {
                CopyValue(baseValue, output, operation.Index);
                return;
            }
            if (m_Availability[baseValue] != PoseSlotFrameAvailability.Pose)
            {
                SetInvalid(output, CombineContinuity(m_Continuity[baseValue], m_Continuity[additiveValue], operation.Index));
                return;
            }

            CharacterPresentationDenseBoneMask mask = RequireMask(operation);
            CharacterPresentationAdditiveReferenceDescriptor reference = RequireAdditiveReference(operation);
            m_Availability[output] = PoseSlotFrameAvailability.Pose;
            m_OutputWeights[output] = UnionWeight(
                m_OutputWeights[baseValue],
                m_OutputWeights[additiveValue] * operation.Weight);
            m_Continuity[output] = CombineContinuity(m_Continuity[baseValue], m_Continuity[additiveValue], operation.Index);
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float weight = Mathf.Clamp01(GetBoneOutputWeight(additiveValue, bone) * mask.Weights[bone] * operation.Weight);
                m_Poses[PoseOffset(output) + bone] = AddPose(
                    m_Poses[PoseOffset(baseValue) + bone],
                    m_Poses[PoseOffset(additiveValue) + bone],
                    reference,
                    bone,
                    weight);
            }
            ResolveParameters(operation, baseValue, additiveValue, output);
            MergeContributions(baseValue, additiveValue, output, operation, true);
            ResolveFootFeatures(baseValue, additiveValue, output, operation, true);
        }

        void EvaluatePoseCurveResolve(CharacterPresentationPoseOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int input = operation.InputPoseValueIndexA;
            if ((uint)input >= (uint)m_ValueCount)
            {
                SetInvalid(output, (ulong)operation.Index + 1);
                return;
            }
            CopyValue(input, output, operation.Index);
        }

        void EvaluateOutputPose(CharacterPresentationPoseOperation operation)
        {
            int output = operation.OutputPoseValueIndex;
            int input = operation.InputPoseValueIndexA;
            if ((uint)input >= (uint)m_ValueCount)
            {
                SetInvalid(output, (ulong)operation.Index + 1);
                return;
            }
            CopyValue(input, output, operation.Index);
        }

        bool RequireInputs(int inputA, int inputB, int output)
        {
            if ((uint)inputA >= (uint)m_ValueCount || (uint)inputB >= (uint)m_ValueCount)
            {
                SetInvalid(output, 1);
                return false;
            }
            if (m_Availability[inputA] == PoseSlotFrameAvailability.Invalid ||
                m_Availability[inputB] == PoseSlotFrameAvailability.Invalid)
            {
                SetInvalid(output, CombineContinuity(m_Continuity[inputA], m_Continuity[inputB], output));
                return false;
            }
            return true;
        }

        CharacterPresentationDenseBoneMask RequireMask(CharacterPresentationPoseOperation operation)
        {
            if ((uint)operation.BoneMaskIndex >= (uint)m_Program.BoneMasks.Count)
                throw new InvalidOperationException($"Pose operation '{operation.NodeId}' has no compiled Bone Mask.");
            return m_Program.BoneMasks[operation.BoneMaskIndex];
        }

        CharacterPresentationAdditiveReferenceDescriptor RequireAdditiveReference(CharacterPresentationPoseOperation operation)
        {
            if ((uint)operation.AdditiveReferenceIndex >= (uint)m_Program.AdditiveReferences.Count)
                throw new InvalidOperationException($"Pose operation '{operation.NodeId}' has no compiled Additive reference.");
            CharacterPresentationAdditiveReferenceDescriptor reference = m_Program.AdditiveReferences[operation.AdditiveReferenceIndex];
            if (reference.Positions.Count != m_BoneCount)
                throw new InvalidOperationException($"Pose operation '{operation.NodeId}' Additive reference has the wrong Bone count.");
            return reference;
        }

        void ResolveParameters(CharacterPresentationPoseOperation operation, int baseValue, int overlayValue, int output)
        {
            if (operation.ParameterPolicies.Count != m_ParameterCount)
                throw new InvalidOperationException($"Pose operation '{operation.NodeId}' has incomplete Pose Parameter policy.");
            float baseWeight = m_OutputWeights[baseValue];
            float overlayWeight = m_OutputWeights[overlayValue] * operation.Weight;
            for (int i = 0; i < m_ParameterCount; i++)
            {
                float baseParameter = m_Parameters[ParameterOffset(baseValue) + i];
                float overlayParameter = m_Parameters[ParameterOffset(overlayValue) + i];
                float value = operation.ParameterPolicies[i] switch
                {
                    PoseParameterResolvePolicy.Base => baseParameter,
                    PoseParameterResolvePolicy.Overlay => overlayWeight > 0f ? overlayParameter : baseParameter,
                    PoseParameterResolvePolicy.Weighted => Weighted(baseParameter, baseWeight, overlayParameter, overlayWeight),
                    PoseParameterResolvePolicy.Max => Mathf.Max(baseParameter, overlayParameter),
                    PoseParameterResolvePolicy.Min => Mathf.Min(baseParameter, overlayParameter),
                    _ => throw new InvalidOperationException($"Pose operation '{operation.NodeId}' Parameter policy #{i} is unsupported.")
                };
                m_Parameters[ParameterOffset(output) + i] = value;
            }
        }

        void MergeContributions(
            int baseValue,
            int overlayValue,
            int output,
            CharacterPresentationPoseOperation operation,
            bool additive)
        {
            CharacterPresentationDenseBoneMask mask = RequireMask(operation);
            for (int i = 0; i < m_ContributionCounts[baseValue]; i++)
                AddContribution(baseValue, i, output, operation, mask, false, additive);
            for (int i = 0; i < m_ContributionCounts[overlayValue]; i++)
                AddContribution(overlayValue, i, output, operation, mask, true, additive);
        }

        void AddContribution(
            int sourceValue,
            int sourceIndex,
            int output,
            CharacterPresentationPoseOperation operation,
            CharacterPresentationDenseBoneMask mask,
            bool overlay,
            bool additive)
        {
            AnimationPoseSourceContribution source = m_Contributions[ContributionOffset(sourceValue) + sourceIndex];
            int targetIndex = FindContribution(output, source);
            float scalarFactor;
            float leftFactor;
            float rightFactor;
            if (overlay)
            {
                scalarFactor = operation.Weight;
                leftFactor = mask.Weights[m_Rig.LeftFootBoneIndex] * operation.Weight;
                rightFactor = mask.Weights[m_Rig.RightFootBoneIndex] * operation.Weight;
            }
            else if (additive)
            {
                scalarFactor = 1f;
                leftFactor = 1f;
                rightFactor = 1f;
            }
            else
            {
                scalarFactor = 1f - m_OutputWeights[operation.InputPoseValueIndexB] * operation.Weight;
                leftFactor = 1f - GetBoneOutputWeight(operation.InputPoseValueIndexB, m_Rig.LeftFootBoneIndex) * mask.Weights[m_Rig.LeftFootBoneIndex] * operation.Weight;
                rightFactor = 1f - GetBoneOutputWeight(operation.InputPoseValueIndexB, m_Rig.RightFootBoneIndex) * mask.Weights[m_Rig.RightFootBoneIndex] * operation.Weight;
            }

            float scalarWeight = source.Weight * Mathf.Clamp01(scalarFactor);
            float leftWeight = source.LeftFootWeight * Mathf.Clamp01(leftFactor);
            float rightWeight = source.RightFootWeight * Mathf.Clamp01(rightFactor);
            if (targetIndex < 0)
            {
                targetIndex = m_ContributionCounts[output];
                if (targetIndex >= m_ContributionCapacity)
                    throw new InvalidOperationException("Pose Graph contribution workspace capacity is exhausted.");
                m_ContributionCounts[output]++;
                m_Contributions[ContributionOffset(output) + targetIndex] = new AnimationPoseSourceContribution(
                    source.PoseSlotId,
                    source.Kind,
                    source.PlaybackId,
                    source.ProgramProducerIndex,
                    source.ContributionContinuityIdentity,
                    scalarWeight,
                    leftWeight,
                    rightWeight);
            }
            else
            {
                AnimationPoseSourceContribution current = m_Contributions[ContributionOffset(output) + targetIndex];
                m_Contributions[ContributionOffset(output) + targetIndex] = new AnimationPoseSourceContribution(
                    current.PoseSlotId,
                    current.Kind,
                    current.PlaybackId,
                    current.ProgramProducerIndex,
                    current.ContributionContinuityIdentity,
                    Mathf.Clamp01(current.Weight + scalarWeight),
                    Mathf.Clamp01(current.LeftFootWeight + leftWeight),
                    Mathf.Clamp01(current.RightFootWeight + rightWeight));
            }

            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                float factor;
                if (overlay)
                    factor = mask.Weights[bone] * operation.Weight;
                else if (additive)
                    factor = 1f;
                else
                    factor = 1f - GetBoneOutputWeight(operation.InputPoseValueIndexB, bone) * mask.Weights[bone] * operation.Weight;
                float weight = GetContributionBoneWeight(sourceValue, sourceIndex, bone) * Mathf.Clamp01(factor);
                SetContributionBoneWeight(
                    output,
                    targetIndex,
                    bone,
                    Mathf.Clamp01(GetContributionBoneWeight(output, targetIndex, bone) + weight));
            }
        }

        int FindContribution(int value, AnimationPoseSourceContribution source)
        {
            for (int i = 0; i < m_ContributionCounts[value]; i++)
            {
                AnimationPoseSourceContribution candidate = m_Contributions[ContributionOffset(value) + i];
                if (candidate.PoseSlotId == source.PoseSlotId && candidate.Kind == source.Kind &&
                    candidate.PlaybackId.Equals(source.PlaybackId) && candidate.ProgramProducerIndex == source.ProgramProducerIndex &&
                    candidate.ContributionContinuityIdentity == source.ContributionContinuityIdentity)
                {
                    return i;
                }
            }
            return -1;
        }

        void ResolveFootFeatures(
            int baseValue,
            int overlayValue,
            int output,
            CharacterPresentationPoseOperation operation,
            bool additive)
        {
            bool hasBase = m_HasFootFeatures[baseValue];
            bool hasOverlay = m_HasFootFeatures[overlayValue];
            if (!hasBase && !hasOverlay)
                return;
            CharacterPresentationDenseBoneMask mask = RequireMask(operation);
            float left = GetBoneOutputWeight(overlayValue, m_Rig.LeftFootBoneIndex) * mask.Weights[m_Rig.LeftFootBoneIndex] * operation.Weight;
            float right = GetBoneOutputWeight(overlayValue, m_Rig.RightFootBoneIndex) * mask.Weights[m_Rig.RightFootBoneIndex] * operation.Weight;
            if (additive)
            {
                left = left / (1f + left);
                right = right / (1f + right);
            }
            m_LeftFootFeatures[output] = ResolveFeature(hasBase, m_LeftFootFeatures[baseValue], hasOverlay, m_LeftFootFeatures[overlayValue], left);
            m_RightFootFeatures[output] = ResolveFeature(hasBase, m_RightFootFeatures[baseValue], hasOverlay, m_RightFootFeatures[overlayValue], right);
            m_HasFootFeatures[output] = m_LeftFootFeatures[output].IsValid && m_RightFootFeatures[output].IsValid;
        }

        void ScaleValue(int value, CharacterPresentationPoseOperation operation, bool overlay)
        {
            if (!overlay || m_Availability[value] != PoseSlotFrameAvailability.Pose)
                return;
            CharacterPresentationDenseBoneMask mask = RequireMask(operation);
            m_OutputWeights[value] *= operation.Weight;
            for (int i = 0; i < m_ContributionCounts[value]; i++)
            {
                AnimationPoseSourceContribution source = m_Contributions[ContributionOffset(value) + i];
                m_Contributions[ContributionOffset(value) + i] = new AnimationPoseSourceContribution(
                    source.PoseSlotId,
                    source.Kind,
                    source.PlaybackId,
                    source.ProgramProducerIndex,
                    source.ContributionContinuityIdentity,
                    source.Weight * operation.Weight,
                    source.LeftFootWeight * mask.Weights[m_Rig.LeftFootBoneIndex] * operation.Weight,
                    source.RightFootWeight * mask.Weights[m_Rig.RightFootBoneIndex] * operation.Weight);
                for (int bone = 0; bone < m_BoneCount; bone++)
                    SetContributionBoneWeight(value, i, bone, GetContributionBoneWeight(value, i, bone) * mask.Weights[bone] * operation.Weight);
            }
        }

        void CopyValue(int source, int destination, int operationIndex)
        {
            m_Availability[destination] = m_Availability[source];
            m_OutputWeights[destination] = m_OutputWeights[source];
            m_Continuity[destination] = CombineContinuity(m_Continuity[source], (ulong)operationIndex + 1, operationIndex);
            for (int i = 0; i < m_BoneCount; i++)
                m_Poses[PoseOffset(destination) + i] = m_Poses[PoseOffset(source) + i];
            for (int i = 0; i < m_ParameterCount; i++)
                m_Parameters[ParameterOffset(destination) + i] = m_Parameters[ParameterOffset(source) + i];
            m_ContributionCounts[destination] = m_ContributionCounts[source];
            for (int i = 0; i < m_ContributionCounts[source]; i++)
            {
                m_Contributions[ContributionOffset(destination) + i] = m_Contributions[ContributionOffset(source) + i];
                for (int bone = 0; bone < m_BoneCount; bone++)
                    SetContributionBoneWeight(destination, i, bone, GetContributionBoneWeight(source, i, bone));
            }
            m_LeftFootFeatures[destination] = m_LeftFootFeatures[source];
            m_RightFootFeatures[destination] = m_RightFootFeatures[source];
            m_HasFootFeatures[destination] = m_HasFootFeatures[source];
        }

        void ResetValue(int value)
        {
            m_Availability[value] = PoseSlotFrameAvailability.Invalid;
            m_OutputWeights[value] = 0f;
            m_ContributionCounts[value] = 0;
            m_LeftFootFeatures[value] = default;
            m_RightFootFeatures[value] = default;
            m_HasFootFeatures[value] = false;
            m_Continuity[value] = 1;
            Array.Clear(m_ContributionBoneWeights, ContributionBoneOffset(value), m_ContributionCapacity * m_BoneCount);
            for (int i = 0; i < m_ParameterCount; i++)
                m_Parameters[ParameterOffset(value) + i] = m_Program.Parameters[i].DefaultValue;
        }

        void SetInvalid(int value, ulong continuity)
        {
            m_Availability[value] = PoseSlotFrameAvailability.Invalid;
            m_OutputWeights[value] = 0f;
            m_ContributionCounts[value] = 0;
            m_Continuity[value] = RequireIdentity(continuity);
        }

        void RequireFiniteValue(int value, PoseNodeId nodeId)
        {
            if (!Enum.IsDefined(typeof(PoseSlotFrameAvailability), m_Availability[value]) ||
                !float.IsFinite(m_OutputWeights[value]) || m_OutputWeights[value] < 0f || m_OutputWeights[value] > 1f ||
                m_Continuity[value] == 0)
            {
                throw new InvalidOperationException($"Pose Node '{nodeId}' produced invalid frame metadata.");
            }
            if (m_Availability[value] == PoseSlotFrameAvailability.Pose)
            {
                for (int i = 0; i < m_BoneCount; i++)
                {
                    if (!m_Poses[PoseOffset(value) + i].IsValid)
                        throw new InvalidOperationException($"Pose Node '{nodeId}' produced invalid Bone #{i}.");
                }
            }
            for (int i = 0; i < m_ParameterCount; i++)
            {
                if (!float.IsFinite(m_Parameters[ParameterOffset(value) + i]))
                    throw new InvalidOperationException($"Pose Node '{nodeId}' produced invalid Parameter #{i}.");
            }
        }

        float GetBoneOutputWeight(int value, int bone)
        {
            float weight = 0f;
            for (int i = 0; i < m_ContributionCounts[value]; i++)
                weight += GetContributionBoneWeight(value, i, bone);
            return Mathf.Clamp01(weight);
        }

        float GetContributionBoneWeight(int value, int contribution, int bone) =>
            m_ContributionBoneWeights[ContributionBoneOffset(value) + contribution * m_BoneCount + bone];

        void SetContributionBoneWeight(int value, int contribution, int bone, float weight) =>
            m_ContributionBoneWeights[ContributionBoneOffset(value) + contribution * m_BoneCount + bone] = weight;

        int PoseOffset(int value) => value * m_BoneCount;
        int ParameterOffset(int value) => value * m_ParameterCount;
        int ContributionOffset(int value) => value * m_ContributionCapacity;
        int ContributionBoneOffset(int value) => value * m_ContributionCapacity * m_BoneCount;

        static AnimationLocalBonePose BlendPose(AnimationLocalBonePose from, AnimationLocalBonePose to, float weight)
        {
            Quaternion target = to.Rotation;
            if (Quaternion.Dot(from.Rotation, target) < 0f)
                target = new Quaternion(-target.x, -target.y, -target.z, -target.w);
            return new AnimationLocalBonePose(
                Vector3.LerpUnclamped(from.Position, to.Position, weight),
                Quaternion.SlerpUnclamped(from.Rotation, target, weight),
                Vector3.LerpUnclamped(from.Scale, to.Scale, weight));
        }

        static AnimationLocalBonePose AddPose(
            AnimationLocalBonePose basePose,
            AnimationLocalBonePose additivePose,
            CharacterPresentationAdditiveReferenceDescriptor reference,
            int bone,
            float weight)
        {
            Quaternion delta = additivePose.Rotation * Quaternion.Inverse(reference.Rotations[bone]);
            if (delta.w < 0f)
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            Quaternion rotation = basePose.Rotation * Quaternion.SlerpUnclamped(Quaternion.identity, delta, weight);
            Vector3 scale = reference.ScalePolicy switch
            {
                AdditiveScalePolicy.Multiply => Vector3.Scale(
                    basePose.Scale,
                    Vector3.LerpUnclamped(Vector3.one, Divide(additivePose.Scale, reference.Scales[bone]), weight)),
                AdditiveScalePolicy.AddDelta => basePose.Scale + (additivePose.Scale - reference.Scales[bone]) * weight,
                AdditiveScalePolicy.Ignore => basePose.Scale,
                _ => throw new InvalidOperationException($"Additive scale policy '{reference.ScalePolicy}' is unsupported.")
            };
            return new AnimationLocalBonePose(
                basePose.Position + (additivePose.Position - reference.Positions[bone]) * weight,
                rotation,
                scale);
        }

        static Vector3 Divide(Vector3 value, Vector3 divisor)
        {
            if (Mathf.Abs(divisor.x) <= 0.000001f || Mathf.Abs(divisor.y) <= 0.000001f || Mathf.Abs(divisor.z) <= 0.000001f)
                throw new InvalidOperationException("Additive reference scale contains zero.");
            return new Vector3(value.x / divisor.x, value.y / divisor.y, value.z / divisor.z);
        }

        static AnimationFootFeatureSample ResolveFeature(
            bool hasBase,
            AnimationFootFeatureSample baseValue,
            bool hasOverlay,
            AnimationFootFeatureSample overlayValue,
            float weight)
        {
            if (!hasBase)
                return hasOverlay ? overlayValue : default;
            if (!hasOverlay)
                return baseValue;
            float t = Mathf.Clamp01(weight);
            return new AnimationFootFeatureSample(
                Vector3.LerpUnclamped(baseValue.SoleLocalVelocity, overlayValue.SoleLocalVelocity, t),
                Mathf.LerpUnclamped(baseValue.SoleHeight, overlayValue.SoleHeight, t),
                Mathf.LerpUnclamped(baseValue.PlantConfidence, overlayValue.PlantConfidence, t),
                Mathf.LerpUnclamped(baseValue.NextLandingConfidence, overlayValue.NextLandingConfidence, t),
                Mathf.LerpUnclamped(baseValue.NextLandingDelaySeconds, overlayValue.NextLandingDelaySeconds, t),
                Vector2.LerpUnclamped(baseValue.NextLandingLocalOffset, overlayValue.NextLandingLocalOffset, t));
        }

        static float Weighted(float a, float aWeight, float b, float bWeight)
        {
            float total = aWeight + bWeight;
            return total > 0f ? (a * aWeight + b * bWeight) / total : 0f;
        }

        static float UnionWeight(float a, float b) => Mathf.Clamp01(1f - (1f - Mathf.Clamp01(a)) * (1f - Mathf.Clamp01(b)));

        static ulong CombineContinuity(ulong a, ulong b, int operation)
        {
            unchecked
            {
                ulong value = 1469598103934665603UL;
                value = (value ^ RequireIdentity(a)) * 1099511628211UL;
                value = (value ^ RequireIdentity(b)) * 1099511628211UL;
                value = (value ^ (ulong)(operation + 1)) * 1099511628211UL;
                return RequireIdentity(value);
            }
        }

        static ulong RequireIdentity(ulong value) => value == 0 ? 1UL : value;

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterPoseGraphEvaluator));
        }
    }
}
