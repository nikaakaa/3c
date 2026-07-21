using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal sealed class AnimationBlendStoredPoseState
    {
        readonly AnimationLocalBonePose[] m_Pose;
        readonly AnimationBlendBoneVelocity[] m_Velocity;
        readonly float[] m_Parameters;
        readonly float[] m_BoneOutputWeights;

        public AnimationBlendStoredPoseState(int boneCount, int parameterCount)
        {
            m_Pose = new AnimationLocalBonePose[boneCount];
            m_Velocity = new AnimationBlendBoneVelocity[boneCount];
            m_Parameters = new float[parameterCount];
            m_BoneOutputWeights = new float[boneCount];
        }

        public bool Active { get; private set; }
        public bool HasFootFeatures { get; private set; }
        public AnimationFootFeatureSample LeftFootFeatures { get; private set; }
        public AnimationFootFeatureSample RightFootFeatures { get; private set; }
        public float OutputWeight { get; private set; }
        public ulong ContributionContinuityIdentity { get; private set; }
        public AnimationReadOnlyBuffer<AnimationLocalBonePose> Pose => new AnimationReadOnlyBuffer<AnimationLocalBonePose>(m_Pose, 0, m_Pose.Length);
        public AnimationReadOnlyBuffer<AnimationBlendBoneVelocity> Velocity => new AnimationReadOnlyBuffer<AnimationBlendBoneVelocity>(m_Velocity, 0, m_Velocity.Length);
        public AnimationReadOnlyBuffer<float> Parameters => new AnimationReadOnlyBuffer<float>(m_Parameters, 0, m_Parameters.Length);

        public float GetBoneOutputWeight(int boneIndex)
        {
            if ((uint)boneIndex >= (uint)m_BoneOutputWeights.Length)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_BoneOutputWeights[boneIndex];
        }

        public void Capture(
            PoseSlotFrame frame,
            AnimationReadOnlyBuffer<AnimationBlendBoneVelocity> velocity,
            ulong contributionContinuityIdentity)
        {
            if (frame.Availability != PoseSlotFrameAvailability.Pose ||
                frame.DenseLocalPose.Count != m_Pose.Length || velocity.Count != m_Velocity.Length ||
                frame.PoseParameters.Count != m_Parameters.Length || contributionContinuityIdentity == 0)
                throw new InvalidOperationException("Stored Pose capture boundary is invalid.");
            for (int i = 0; i < m_Pose.Length; i++)
            {
                m_Pose[i] = frame.DenseLocalPose[i];
                m_Velocity[i] = velocity[i];
                m_BoneOutputWeights[i] = frame.GetBoneOutputWeight(i);
            }
            for (int i = 0; i < m_Parameters.Length; i++)
                m_Parameters[i] = frame.PoseParameters[i];
            OutputWeight = frame.OutputWeight;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            LeftFootFeatures = frame.LeftFootFeatures;
            RightFootFeatures = frame.RightFootFeatures;
            HasFootFeatures = frame.HasFootFeatures;
            Active = true;
        }

        public void Clear()
        {
            Active = false;
            HasFootFeatures = false;
            LeftFootFeatures = default;
            RightFootFeatures = default;
            OutputWeight = 0f;
            ContributionContinuityIdentity = 0;
            Array.Clear(m_BoneOutputWeights, 0, m_BoneOutputWeights.Length);
        }
    }

    internal sealed class AnimationBlendInertialState
    {
        readonly Vector3[] m_PositionResidual;
        readonly Vector3[] m_RotationResidual;
        readonly Vector3[] m_ScaleResidual;
        readonly Vector3[] m_LinearVelocityResidual;
        readonly Vector3[] m_AngularVelocityResidual;
        readonly Vector3[] m_ScaleVelocityResidual;
        readonly float[] m_ParameterResidual;
        readonly float[] m_BoneOutputWeights;

        public AnimationBlendInertialState(int boneCount, int parameterCount)
        {
            m_PositionResidual = new Vector3[boneCount];
            m_RotationResidual = new Vector3[boneCount];
            m_ScaleResidual = new Vector3[boneCount];
            m_LinearVelocityResidual = new Vector3[boneCount];
            m_AngularVelocityResidual = new Vector3[boneCount];
            m_ScaleVelocityResidual = new Vector3[boneCount];
            m_ParameterResidual = new float[parameterCount];
            m_BoneOutputWeights = new float[boneCount];
        }

        public bool Active { get; private set; }
        public AnimationPlaybackId TargetPlaybackId { get; private set; }
        public ulong ContributionContinuityIdentity { get; private set; }
        public float OutputWeight { get; private set; }
        public bool SourceHasFootFeatures { get; private set; }
        public AnimationFootFeatureSample LeftFootFeatures { get; private set; }
        public AnimationFootFeatureSample RightFootFeatures { get; private set; }

        public void Capture(
            PoseSlotFrame current,
            AnimationReadOnlyBuffer<AnimationBlendBoneVelocity> currentVelocity,
            AnimationBlendSourcePoseFrame target,
            CharacterAnimationScalePolicy scalePolicy,
            ulong contributionContinuityIdentity)
        {
            if (current.Availability != PoseSlotFrameAvailability.Pose ||
                current.DenseLocalPose.Count != m_PositionResidual.Length ||
                currentVelocity.Count != m_PositionResidual.Length ||
                target.DenseLocalPose.Count != m_PositionResidual.Length ||
                target.DenseVelocity.Count != m_PositionResidual.Length ||
                current.PoseParameters.Count != m_ParameterResidual.Length ||
                target.PoseParameters.Count != m_ParameterResidual.Length ||
                contributionContinuityIdentity == 0)
                throw new InvalidOperationException("Inertial Blend capture boundary is invalid.");
            for (int i = 0; i < m_PositionResidual.Length; i++)
            {
                AnimationLocalBonePose from = current.DenseLocalPose[i];
                AnimationLocalBonePose to = target.DenseLocalPose[i];
                AnimationBlendBoneVelocity fromVelocity = currentVelocity[i];
                AnimationBlendBoneVelocity toVelocity = target.DenseVelocity[i];
                Vector3 positionResidual = from.Position - to.Position;
                Vector3 rotationResidual = AnimationBlendPoseMath.QuaternionLog(from.Rotation * Quaternion.Inverse(to.Rotation));
                Vector3 linearVelocityResidual = fromVelocity.Linear - toVelocity.Linear;
                Vector3 angularVelocityResidual = fromVelocity.Angular - toVelocity.Angular;
                Vector3 scaleResidual = from.Scale - to.Scale;
                Vector3 scaleVelocityResidual = fromVelocity.Scale - toVelocity.Scale;
                if (!AnimationBlendPoseMath.IsFinite(positionResidual) ||
                    !AnimationBlendPoseMath.IsFinite(rotationResidual) ||
                    !AnimationBlendPoseMath.IsFinite(linearVelocityResidual) ||
                    !AnimationBlendPoseMath.IsFinite(angularVelocityResidual) ||
                    !AnimationBlendPoseMath.IsFinite(scaleResidual) ||
                    !AnimationBlendPoseMath.IsFinite(scaleVelocityResidual))
                {
                    throw new InvalidOperationException($"Inertial Blend Bone residual #{i} is non-finite.");
                }
            }
            for (int i = 0; i < m_ParameterResidual.Length; i++)
            {
                if (!float.IsFinite(current.PoseParameters[i] - target.PoseParameters[i]))
                    throw new InvalidOperationException($"Inertial Blend parameter residual #{i} is non-finite.");
            }
            for (int i = 0; i < m_PositionResidual.Length; i++)
            {
                AnimationLocalBonePose from = current.DenseLocalPose[i];
                AnimationLocalBonePose to = target.DenseLocalPose[i];
                AnimationBlendBoneVelocity fromVelocity = currentVelocity[i];
                AnimationBlendBoneVelocity toVelocity = target.DenseVelocity[i];
                m_PositionResidual[i] = from.Position - to.Position;
                m_RotationResidual[i] = AnimationBlendPoseMath.QuaternionLog(from.Rotation * Quaternion.Inverse(to.Rotation));
                m_LinearVelocityResidual[i] = fromVelocity.Linear - toVelocity.Linear;
                m_AngularVelocityResidual[i] = fromVelocity.Angular - toVelocity.Angular;
                m_BoneOutputWeights[i] = current.GetBoneOutputWeight(i);
                if (scalePolicy == CharacterAnimationScalePolicy.BlendLocalScale)
                {
                    m_ScaleResidual[i] = from.Scale - to.Scale;
                    m_ScaleVelocityResidual[i] = fromVelocity.Scale - toVelocity.Scale;
                }
                else
                {
                    m_ScaleResidual[i] = Vector3.zero;
                    m_ScaleVelocityResidual[i] = Vector3.zero;
                }
            }
            for (int i = 0; i < m_ParameterResidual.Length; i++)
                m_ParameterResidual[i] = current.PoseParameters[i] - target.PoseParameters[i];
            TargetPlaybackId = target.PlaybackId;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            OutputWeight = current.OutputWeight;
            SourceHasFootFeatures = current.HasFootFeatures;
            LeftFootFeatures = current.LeftFootFeatures;
            RightFootFeatures = current.RightFootFeatures;
            Active = true;
        }

        public AnimationLocalBonePose EvaluateBone(
            int boneIndex,
            AnimationLocalBonePose target,
            float normalizedTime,
            float duration,
            AnimationBlendCurvePayload curve,
            float startDerivative,
            float endDerivative)
        {
            float s = Mathf.Clamp01(normalizedTime);
            EvaluateEnvelope(curve, s, startDerivative, endDerivative, out float envelope, out _);
            float residualWeight = 1f - envelope;
            Vector3 positionResidual = residualWeight *
                                       (m_PositionResidual[boneIndex] + s * duration * m_LinearVelocityResidual[boneIndex]);
            Vector3 rotationResidual = residualWeight *
                                       (m_RotationResidual[boneIndex] + s * duration * m_AngularVelocityResidual[boneIndex]);
            Vector3 scaleResidual = residualWeight *
                                    (m_ScaleResidual[boneIndex] + s * duration * m_ScaleVelocityResidual[boneIndex]);
            return new AnimationLocalBonePose(
                target.Position + positionResidual,
                AnimationBlendPoseMath.QuaternionExp(rotationResidual) * target.Rotation,
                target.Scale + scaleResidual);
        }

        public AnimationBlendBoneVelocity EvaluateBoneVelocity(
            int boneIndex,
            AnimationBlendBoneVelocity target,
            float normalizedTime,
            float duration,
            AnimationBlendCurvePayload curve,
            float startDerivative,
            float endDerivative)
        {
            if (duration <= 0f)
                return target;
            float s = Mathf.Clamp01(normalizedTime);
            EvaluateEnvelope(curve, s, startDerivative, endDerivative, out float envelope, out float envelopeDerivative);
            float residualWeight = 1f - envelope;
            float derivativeScale = -envelopeDerivative / duration;
            Vector3 positionBase = m_PositionResidual[boneIndex] + s * duration * m_LinearVelocityResidual[boneIndex];
            Vector3 rotationBase = m_RotationResidual[boneIndex] + s * duration * m_AngularVelocityResidual[boneIndex];
            Vector3 scaleBase = m_ScaleResidual[boneIndex] + s * duration * m_ScaleVelocityResidual[boneIndex];
            return new AnimationBlendBoneVelocity(
                target.Linear + derivativeScale * positionBase + residualWeight * m_LinearVelocityResidual[boneIndex],
                target.Angular + derivativeScale * rotationBase + residualWeight * m_AngularVelocityResidual[boneIndex],
                target.Scale + derivativeScale * scaleBase + residualWeight * m_ScaleVelocityResidual[boneIndex]);
        }

        public float EvaluateParameter(
            int parameterIndex,
            float target,
            float normalizedTime,
            AnimationBlendCurvePayload curve,
            float startDerivative,
            float endDerivative)
        {
            EvaluateEnvelope(curve, Mathf.Clamp01(normalizedTime), startDerivative, endDerivative, out float envelope, out _);
            return target + m_ParameterResidual[parameterIndex] * (1f - envelope);
        }

        public static float EvaluateEnvelope(
            AnimationBlendCurvePayload curve,
            float normalizedTime,
            float startDerivative,
            float endDerivative)
        {
            EvaluateEnvelope(
                curve,
                Mathf.Clamp01(normalizedTime),
                startDerivative,
                endDerivative,
                out float envelope,
                out _);
            return envelope;
        }

        public float GetBoneOutputWeight(int boneIndex)
        {
            if ((uint)boneIndex >= (uint)m_BoneOutputWeights.Length)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_BoneOutputWeights[boneIndex];
        }

        static void EvaluateEnvelope(
            AnimationBlendCurvePayload curve,
            float normalizedTime,
            float startDerivative,
            float endDerivative,
            out float envelope,
            out float derivative)
        {
            float s = normalizedTime;
            float s2 = s * s;
            float s3 = s2 * s;
            float h10 = s3 - 2f * s2 + s;
            float h11 = s3 - s2;
            float h10Derivative = 3f * s2 - 4f * s + 1f;
            float h11Derivative = 3f * s2 - 2f * s;
            envelope = AnimationBlendCurveEvaluator.Evaluate(curve, s) -
                       startDerivative * h10 - endDerivative * h11;
            derivative = AnimationBlendCurveEvaluator.EvaluateDerivative(curve, s) -
                         startDerivative * h10Derivative - endDerivative * h11Derivative;
        }

        public void Clear()
        {
            Active = false;
            TargetPlaybackId = default;
            ContributionContinuityIdentity = 0;
            OutputWeight = 0f;
            Array.Clear(m_BoneOutputWeights, 0, m_BoneOutputWeights.Length);
            SourceHasFootFeatures = false;
            LeftFootFeatures = default;
            RightFootFeatures = default;
        }
    }

    internal sealed class AnimationSlotBlendPoseEvaluator
    {
        readonly CharacterAnimationRigPayload m_Rig;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_LeftFootBoneIndex;
        readonly int m_RightFootBoneIndex;
        readonly int m_ContributionCapacity;
        readonly AnimationBlendSourcePoseFrame[] m_EntrySources;
        readonly AnimationBlendCurvePayload[] m_EntryCurves;
        readonly AnimationBlendProfilePayload[] m_EntryProfiles;
        readonly int[] m_EntryContributionIndices;
        readonly float[] m_EntryBoneWeights;
        readonly float[] m_EntryScalarWeights;
        readonly float[] m_EntryMaximumWeights;
        readonly float[] m_StoredBoneWeights;
        readonly AnimationLocalBonePose[] m_OutputPose;
        readonly AnimationBlendBoneVelocity[] m_OutputVelocity;
        readonly float[] m_OutputParameters;
        readonly AnimationPoseSourceContribution[] m_OutputContributions;
        readonly float[] m_OutputDenseContributionWeights;
        readonly float[] m_BuildDenseContributionWeights;
        readonly AnimationPoseContributionKind[] m_BuildKinds;
        readonly AnimationPlaybackId[] m_BuildPlaybackIds;
        readonly ulong[] m_BuildContributionContinuityIdentities;
        readonly int[] m_BuildProducerIndices;
        readonly float[] m_BuildScalarWeights;
        readonly float[] m_BuildLeftFootWeights;
        readonly float[] m_BuildRightFootWeights;

        PoseSlotFrame m_CurrentFrame;
        int m_CurrentBufferIndex = -1;
        bool m_HasFrame;
        float m_StoredMaximumWeight;

        public AnimationSlotBlendPoseEvaluator(
            CharacterAnimationRigPayload rig,
            int parameterCount,
            int entryCapacity)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            rig.RequireValid();
            if (parameterCount < 0 || entryCapacity < 2)
                throw new ArgumentOutOfRangeException();
            m_Rig = rig;
            m_BoneCount = rig.Bones.Count;
            m_ParameterCount = parameterCount;
            m_LeftFootBoneIndex = rig.LeftFootBoneIndex;
            m_RightFootBoneIndex = rig.RightFootBoneIndex;
            m_ContributionCapacity = entryCapacity + 2;
            m_EntrySources = new AnimationBlendSourcePoseFrame[entryCapacity];
            m_EntryCurves = new AnimationBlendCurvePayload[entryCapacity];
            m_EntryProfiles = new AnimationBlendProfilePayload[entryCapacity];
            m_EntryContributionIndices = new int[entryCapacity];
            m_EntryBoneWeights = new float[entryCapacity * m_BoneCount];
            m_EntryScalarWeights = new float[entryCapacity];
            m_EntryMaximumWeights = new float[entryCapacity];
            m_StoredBoneWeights = new float[m_BoneCount];
            m_OutputPose = new AnimationLocalBonePose[m_BoneCount * 2];
            m_OutputVelocity = new AnimationBlendBoneVelocity[m_BoneCount * 2];
            m_OutputParameters = new float[m_ParameterCount * 2];
            m_OutputContributions = new AnimationPoseSourceContribution[m_ContributionCapacity * 2];
            m_OutputDenseContributionWeights = new float[m_ContributionCapacity * m_BoneCount * 2];
            m_BuildDenseContributionWeights = new float[m_ContributionCapacity * m_BoneCount];
            m_BuildKinds = new AnimationPoseContributionKind[m_ContributionCapacity];
            m_BuildPlaybackIds = new AnimationPlaybackId[m_ContributionCapacity];
            m_BuildContributionContinuityIdentities = new ulong[m_ContributionCapacity];
            m_BuildProducerIndices = new int[m_ContributionCapacity];
            m_BuildScalarWeights = new float[m_ContributionCapacity];
            m_BuildLeftFootWeights = new float[m_ContributionCapacity];
            m_BuildRightFootWeights = new float[m_ContributionCapacity];
            StoredPose = new AnimationBlendStoredPoseState(m_BoneCount, m_ParameterCount);
            Inertial = new AnimationBlendInertialState(m_BoneCount, m_ParameterCount);
        }

        public AnimationBlendStoredPoseState StoredPose { get; }
        public AnimationBlendInertialState Inertial { get; }
        public bool HasFrame => m_HasFrame;
        public PoseSlotFrame CurrentFrame => m_CurrentFrame;
        public float StoredMaximumWeight => m_StoredMaximumWeight;

        public float GetEntryMaximumWeight(int entryIndex)
        {
            if ((uint)entryIndex >= (uint)m_EntryMaximumWeights.Length)
                throw new ArgumentOutOfRangeException(nameof(entryIndex));
            return m_EntryMaximumWeights[entryIndex];
        }

        public AnimationReadOnlyBuffer<AnimationBlendBoneVelocity> GetCurrentVelocity()
        {
            if (!m_HasFrame || m_CurrentFrame.Availability != PoseSlotFrameAvailability.Pose)
                return default;
            return new AnimationReadOnlyBuffer<AnimationBlendBoneVelocity>(
                m_OutputVelocity,
                m_CurrentBufferIndex * m_BoneCount,
                m_BoneCount);
        }

        public void CaptureStoredPose(ulong contributionContinuityIdentity)
        {
            if (!m_HasFrame || m_CurrentFrame.Availability != PoseSlotFrameAvailability.Pose)
                throw new InvalidOperationException("Stored Pose requires a completed Pose frame.");
            StoredPose.Capture(m_CurrentFrame, GetCurrentVelocity(), contributionContinuityIdentity);
        }

        public void BeginInertial(
            AnimationBlendSourcePoseFrame target,
            ulong contributionContinuityIdentity)
        {
            if (!m_HasFrame || m_CurrentFrame.Availability != PoseSlotFrameAvailability.Pose)
                throw new InvalidOperationException("Inertial Blend requires a completed Pose frame.");
            Inertial.Capture(
                m_CurrentFrame,
                GetCurrentVelocity(),
                target,
                m_Rig.ScalePolicy,
                contributionContinuityIdentity);
            StoredPose.Clear();
        }

        public PoseSlotFrame Evaluate(
            PoseSlotId poseSlotId,
            PoseSlotOutputPolicy outputPolicy,
            AnimationBlendEntryState[] entries,
            int entryCount,
            AnimationBlendSourcePoseWorkspace sources,
            AnimationBlendCurveCatalogPayload curveCatalog,
            AnimationBlendProfileCatalogPayload profileCatalog,
            ulong completionIdentity,
            ulong continuityIdentity,
            float deltaSeconds,
            out AnimationBlendStackInvalidReason invalidReason)
        {
            if (sources.CompletionIdentity != completionIdentity)
                return InvalidFrame(poseSlotId, completionIdentity, continuityIdentity,
                    AnimationBlendStackInvalidReason.SourceFrameNotPrepared, out invalidReason);
            if (Inertial.Active)
                return EvaluateInertial(poseSlotId, entries, entryCount, sources, curveCatalog, profileCatalog,
                    completionIdentity, continuityIdentity, deltaSeconds, out invalidReason);
            return EvaluateCrossFade(poseSlotId, outputPolicy, entries, entryCount, sources, curveCatalog, profileCatalog,
                completionIdentity, continuityIdentity, deltaSeconds, out invalidReason);
        }

        public PoseSlotFrame PublishInvalid(
            PoseSlotId poseSlotId,
            ulong completionIdentity,
            ulong continuityIdentity,
            AnimationBlendStackInvalidReason reason,
            out AnimationBlendStackInvalidReason invalidReason) =>
            InvalidFrame(poseSlotId, completionIdentity, continuityIdentity, reason, out invalidReason);

        PoseSlotFrame EvaluateCrossFade(
            PoseSlotId poseSlotId,
            PoseSlotOutputPolicy outputPolicy,
            AnimationBlendEntryState[] entries,
            int entryCount,
            AnimationBlendSourcePoseWorkspace sources,
            AnimationBlendCurveCatalogPayload curveCatalog,
            AnimationBlendProfileCatalogPayload profileCatalog,
            ulong completionIdentity,
            ulong continuityIdentity,
            float deltaSeconds,
            out AnimationBlendStackInvalidReason invalidReason)
        {
            ResetBuildState(entryCount);
            int contributionCount = 0;
            int storedContributionIndex = -1;
            if (StoredPose.Active)
                storedContributionIndex = AddContribution(
                    AnimationPoseContributionKind.Stored,
                    default,
                    -1,
                    StoredPose.ContributionContinuityIdentity,
                    ref contributionCount);
            for (int i = 0; i < entryCount; i++)
            {
                AnimationBlendEntryState entry = entries[i];
                m_EntryCurves[i] = curveCatalog.Require(entry.CanonicalCurveIndex);
                m_EntryProfiles[i] = profileCatalog.Require(entry.BlendProfileIndex);
                if (entry.IsEmpty)
                {
                    m_EntryContributionIndices[i] = -1;
                    continue;
                }
                if (!sources.TryGet(entry.PlaybackId, out AnimationBlendSourcePoseFrame source) ||
                    source.ProgramProducerIndex != entry.ProgramProducerIndex)
                {
                    return InvalidFrame(poseSlotId, completionIdentity, continuityIdentity,
                        AnimationBlendStackInvalidReason.MissingLiveSource, out invalidReason);
                }
                m_EntrySources[i] = source;
                m_EntryContributionIndices[i] = AddContribution(
                    AnimationPoseContributionKind.Live,
                    entry.PlaybackId,
                    entry.ProgramProducerIndex,
                    entry.ContributionContinuityIdentity,
                    ref contributionCount);
            }

            float scalarResidual = 1f;
            for (int i = entryCount - 1; i >= 0; i--)
            {
                float alpha = entries[i].EvaluateOutputAlpha(m_EntryCurves[i], m_EntryProfiles[i]);
                float weight = scalarResidual * alpha;
                m_EntryScalarWeights[i] = weight;
                m_EntryMaximumWeights[i] = weight;
                scalarResidual *= 1f - alpha;
            }
            float storedScalarWeight = StoredPose.Active ? scalarResidual * StoredPose.OutputWeight : 0f;
            float outputWeight = storedScalarWeight;
            m_StoredMaximumWeight = storedScalarWeight;
            if (storedContributionIndex >= 0)
                m_BuildScalarWeights[storedContributionIndex] = storedScalarWeight;
            for (int i = 0; i < entryCount; i++)
            {
                int contributionIndex = m_EntryContributionIndices[i];
                if (contributionIndex >= 0)
                {
                    m_BuildScalarWeights[contributionIndex] += m_EntryScalarWeights[i];
                    outputWeight += m_EntryScalarWeights[i];
                }
            }

            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                float residual = 1f;
                for (int i = entryCount - 1; i >= 0; i--)
                {
                    float alpha = entries[i].EvaluateBoneAlpha(
                        boneIndex,
                        m_EntryCurves[i],
                        m_EntryProfiles[i]);
                    float weight = residual * alpha;
                    m_EntryBoneWeights[i * m_BoneCount + boneIndex] = weight;
                    m_EntryMaximumWeights[i] = Mathf.Max(m_EntryMaximumWeights[i], weight);
                    residual *= 1f - alpha;
                }
                float storedWeight = StoredPose.Active
                    ? residual * StoredPose.GetBoneOutputWeight(boneIndex)
                    : 0f;
                m_StoredBoneWeights[boneIndex] = storedWeight;
                m_StoredMaximumWeight = Mathf.Max(m_StoredMaximumWeight, storedWeight);
                if (storedContributionIndex >= 0)
                    SetDenseContributionWeight(storedContributionIndex, boneIndex, storedWeight);
                for (int i = 0; i < entryCount; i++)
                {
                    int contributionIndex = m_EntryContributionIndices[i];
                    if (contributionIndex >= 0)
                        AddDenseContributionWeight(contributionIndex, boneIndex,
                            m_EntryBoneWeights[i * m_BoneCount + boneIndex]);
                }
            }

            if (outputWeight <= 0f)
            {
                if (outputPolicy == PoseSlotOutputPolicy.RequireOutput)
                    return InvalidFrame(poseSlotId, completionIdentity, continuityIdentity,
                        AnimationBlendStackInvalidReason.MissingRequiredOutput, out invalidReason);
                return PublishNoPose(poseSlotId, completionIdentity, continuityIdentity, deltaSeconds, out invalidReason);
            }

            int nextBuffer = NextBufferIndex();
            int poseOffset = nextBuffer * m_BoneCount;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                Vector3 position = Vector3.zero;
                Vector3 scale = Vector3.zero;
                Vector3 linearVelocity = Vector3.zero;
                Vector3 angularVelocity = Vector3.zero;
                Vector3 scaleVelocity = Vector3.zero;
                Vector4 rotation = Vector4.zero;
                Quaternion rotationReference = sources.GetReferencePose(boneIndex).Rotation;
                bool hasRotationReference = false;
                float poseWeight = 0f;
                float storedWeight = m_StoredBoneWeights[boneIndex];
                if (storedWeight > 0f)
                {
                    AnimationLocalBonePose pose = StoredPose.Pose[boneIndex];
                    rotationReference = pose.Rotation;
                    hasRotationReference = true;
                    position += pose.Position * storedWeight;
                    scale += pose.Scale * storedWeight;
                    AnimationBlendBoneVelocity velocity = StoredPose.Velocity[boneIndex];
                    linearVelocity += velocity.Linear * storedWeight;
                    angularVelocity += velocity.Angular * storedWeight;
                    scaleVelocity += velocity.Scale * storedWeight;
                    rotation += AnimationBlendPoseMath.AlignAndScale(pose.Rotation, rotationReference, storedWeight);
                    poseWeight += storedWeight;
                }
                for (int i = 0; i < entryCount; i++)
                {
                    if (entries[i].IsEmpty)
                        continue;
                    float weight = m_EntryBoneWeights[i * m_BoneCount + boneIndex];
                    if (weight <= 0f)
                        continue;
                    AnimationLocalBonePose pose = m_EntrySources[i].DenseLocalPose[boneIndex];
                    if (!hasRotationReference)
                    {
                        rotationReference = pose.Rotation;
                        hasRotationReference = true;
                    }
                    position += pose.Position * weight;
                    scale += pose.Scale * weight;
                    AnimationBlendBoneVelocity velocity = m_EntrySources[i].DenseVelocity[boneIndex];
                    linearVelocity += velocity.Linear * weight;
                    angularVelocity += velocity.Angular * weight;
                    scaleVelocity += velocity.Scale * weight;
                    rotation += AnimationBlendPoseMath.AlignAndScale(pose.Rotation, rotationReference, weight);
                    poseWeight += weight;
                }
                m_OutputPose[poseOffset + boneIndex] = AnimationBlendPoseMath.BlendWeighted(
                    position, rotation, scale, poseWeight, sources.GetReferencePose(boneIndex));
                m_OutputVelocity[poseOffset + boneIndex] = poseWeight > 0f
                    ? new AnimationBlendBoneVelocity(
                        linearVelocity / poseWeight,
                        angularVelocity / poseWeight,
                        scaleVelocity / poseWeight)
                    : default;
            }

            int parameterOffset = nextBuffer * m_ParameterCount;
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                float value = StoredPose.Active
                    ? StoredPose.Parameters[parameterIndex] * storedScalarWeight
                    : 0f;
                for (int i = 0; i < entryCount; i++)
                {
                    if (!entries[i].IsEmpty)
                        value += m_EntrySources[i].PoseParameters[parameterIndex] * m_EntryScalarWeights[i];
                }
                m_OutputParameters[parameterOffset + parameterIndex] = value / outputWeight;
            }

            ResolveCrossFadeFootFeatures(entries, entryCount, storedContributionIndex,
                out AnimationFootFeatureSample leftFoot, out AnimationFootFeatureSample rightFoot,
                out bool hasFootFeatures);
            PoseSlotFrame frame = PublishPose(poseSlotId, completionIdentity, continuityIdentity,
                outputWeight,
                contributionCount,
                nextBuffer,
                leftFoot,
                rightFoot,
                hasFootFeatures,
                deltaSeconds,
                !CanDifferentiateOutput(deltaSeconds));
            invalidReason = AnimationBlendStackInvalidReason.None;
            return frame;
        }

        PoseSlotFrame EvaluateInertial(
            PoseSlotId poseSlotId,
            AnimationBlendEntryState[] entries,
            int entryCount,
            AnimationBlendSourcePoseWorkspace sources,
            AnimationBlendCurveCatalogPayload curveCatalog,
            AnimationBlendProfileCatalogPayload profileCatalog,
            ulong completionIdentity,
            ulong continuityIdentity,
            float deltaSeconds,
            out AnimationBlendStackInvalidReason invalidReason)
        {
            ResetBuildState(entryCount);
            if (entryCount != 1 || entries[0].IsEmpty ||
                !entries[0].PlaybackId.Equals(Inertial.TargetPlaybackId) ||
                !sources.TryGet(entries[0].PlaybackId, out AnimationBlendSourcePoseFrame target))
            {
                return InvalidFrame(poseSlotId, completionIdentity, continuityIdentity,
                    AnimationBlendStackInvalidReason.MissingLiveSource, out invalidReason);
            }
            int contributionCount = 0;
            int inertialIndex = AddContribution(
                AnimationPoseContributionKind.Inertial,
                default,
                -1,
                Inertial.ContributionContinuityIdentity,
                ref contributionCount);
            int liveIndex = AddContribution(AnimationPoseContributionKind.Live, target.PlaybackId,
                target.ProgramProducerIndex, entries[0].ContributionContinuityIdentity, ref contributionCount);
            AnimationBlendCurvePayload curve = curveCatalog.Require(entries[0].CanonicalCurveIndex);
            AnimationBlendProfilePayload blendProfile = profileCatalog.Require(entries[0].BlendProfileIndex);
            float curveStartDerivative = AnimationBlendCurveEvaluator.EvaluateDerivative(curve, 0f);
            float curveEndDerivative = AnimationBlendCurveEvaluator.EvaluateDerivative(curve, 1f);
            float outputEnvelope = AnimationBlendInertialState.EvaluateEnvelope(
                curve,
                entries[0].GetOutputNormalizedTime(blendProfile),
                curveStartDerivative,
                curveEndDerivative);
            m_BuildScalarWeights[inertialIndex] = (1f - outputEnvelope) * Inertial.OutputWeight;
            m_BuildScalarWeights[liveIndex] = outputEnvelope;
            float outputWeight = m_BuildScalarWeights[inertialIndex] + m_BuildScalarWeights[liveIndex];
            int nextBuffer = NextBufferIndex();
            int poseOffset = nextBuffer * m_BoneCount;
            int velocityOffset = nextBuffer * m_BoneCount;
            for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
            {
                float normalizedTime = entries[0].GetBoneNormalizedTime(boneIndex, blendProfile);
                float envelope = AnimationBlendInertialState.EvaluateEnvelope(
                    curve,
                    normalizedTime,
                    curveStartDerivative,
                    curveEndDerivative);
                SetDenseContributionWeight(
                    inertialIndex,
                    boneIndex,
                    (1f - envelope) * Inertial.GetBoneOutputWeight(boneIndex));
                SetDenseContributionWeight(liveIndex, boneIndex, envelope);
                float duration = entries[0].GetBoneDuration(boneIndex, blendProfile);
                m_OutputPose[poseOffset + boneIndex] = Inertial.EvaluateBone(
                    boneIndex,
                    target.DenseLocalPose[boneIndex],
                    normalizedTime,
                    duration,
                    curve,
                    curveStartDerivative,
                    curveEndDerivative);
                m_OutputVelocity[velocityOffset + boneIndex] = Inertial.EvaluateBoneVelocity(
                    boneIndex,
                    target.DenseVelocity[boneIndex],
                    normalizedTime,
                    duration,
                    curve,
                    curveStartDerivative,
                    curveEndDerivative);
                m_EntryMaximumWeights[0] = 1f;
            }
            int parameterOffset = nextBuffer * m_ParameterCount;
            for (int parameterIndex = 0; parameterIndex < m_ParameterCount; parameterIndex++)
            {
                m_OutputParameters[parameterOffset + parameterIndex] = Inertial.EvaluateParameter(
                    parameterIndex,
                    target.PoseParameters[parameterIndex],
                    entries[0].GetOutputNormalizedTime(blendProfile),
                    curve,
                    curveStartDerivative,
                    curveEndDerivative);
            }
            float leftEnvelope = AnimationBlendInertialState.EvaluateEnvelope(
                curve,
                entries[0].GetBoneNormalizedTime(m_LeftFootBoneIndex, blendProfile),
                curveStartDerivative,
                curveEndDerivative);
            float rightEnvelope = AnimationBlendInertialState.EvaluateEnvelope(
                curve,
                entries[0].GetBoneNormalizedTime(m_RightFootBoneIndex, blendProfile),
                curveStartDerivative,
                curveEndDerivative);
            m_BuildLeftFootWeights[inertialIndex] =
                (1f - leftEnvelope) * Inertial.GetBoneOutputWeight(m_LeftFootBoneIndex);
            m_BuildLeftFootWeights[liveIndex] = leftEnvelope;
            m_BuildRightFootWeights[inertialIndex] =
                (1f - rightEnvelope) * Inertial.GetBoneOutputWeight(m_RightFootBoneIndex);
            m_BuildRightFootWeights[liveIndex] = rightEnvelope;
            ResolveInertialFootFeatures(
                target,
                m_BuildLeftFootWeights[inertialIndex],
                m_BuildLeftFootWeights[liveIndex],
                m_BuildRightFootWeights[inertialIndex],
                m_BuildRightFootWeights[liveIndex],
                out AnimationFootFeatureSample leftFoot,
                out AnimationFootFeatureSample rightFoot,
                out bool hasFootFeatures);
            PoseSlotFrame frame = PublishPose(poseSlotId, completionIdentity, continuityIdentity,
                outputWeight, contributionCount, nextBuffer, leftFoot, rightFoot, hasFootFeatures, deltaSeconds, true);
            invalidReason = AnimationBlendStackInvalidReason.None;
            return frame;
        }

        void ResolveInertialFootFeatures(
            AnimationBlendSourcePoseFrame target,
            float inertialLeftWeight,
            float liveLeftWeight,
            float inertialRightWeight,
            float liveRightWeight,
            out AnimationFootFeatureSample left,
            out AnimationFootFeatureSample right,
            out bool hasFeatures)
        {
            var leftAccumulator = new AnimationFootFeatureBlendAccumulator();
            var rightAccumulator = new AnimationFootFeatureBlendAccumulator();
            bool leftValid = inertialLeftWeight + liveLeftWeight > 0f &&
                             (inertialLeftWeight <= 0f || Inertial.SourceHasFootFeatures) &&
                             (liveLeftWeight <= 0f || target.HasFootFeatures);
            bool rightValid = inertialRightWeight + liveRightWeight > 0f &&
                              (inertialRightWeight <= 0f || Inertial.SourceHasFootFeatures) &&
                              (liveRightWeight <= 0f || target.HasFootFeatures);
            if (leftValid)
            {
                if (inertialLeftWeight > 0f)
                    leftAccumulator.Add(Inertial.LeftFootFeatures, inertialLeftWeight);
                if (liveLeftWeight > 0f)
                    leftAccumulator.Add(target.LeftFootFeatures, liveLeftWeight, target.VisualTimeScale);
            }
            if (rightValid)
            {
                if (inertialRightWeight > 0f)
                    rightAccumulator.Add(Inertial.RightFootFeatures, inertialRightWeight);
                if (liveRightWeight > 0f)
                    rightAccumulator.Add(target.RightFootFeatures, liveRightWeight, target.VisualTimeScale);
            }
            hasFeatures = leftValid && rightValid;
            left = hasFeatures ? leftAccumulator.Resolve() : default;
            right = hasFeatures ? rightAccumulator.Resolve() : default;
        }

        void ResolveCrossFadeFootFeatures(
            AnimationBlendEntryState[] entries,
            int entryCount,
            int storedContributionIndex,
            out AnimationFootFeatureSample left,
            out AnimationFootFeatureSample right,
            out bool hasFeatures)
        {
            var leftAccumulator = new AnimationFootFeatureBlendAccumulator();
            var rightAccumulator = new AnimationFootFeatureBlendAccumulator();
            float leftWeight = 0f;
            float rightWeight = 0f;
            bool leftValid = true;
            bool rightValid = true;
            if (StoredPose.Active)
            {
                float storedLeft = m_StoredBoneWeights[m_LeftFootBoneIndex];
                float storedRight = m_StoredBoneWeights[m_RightFootBoneIndex];
                if (storedContributionIndex >= 0)
                {
                    m_BuildLeftFootWeights[storedContributionIndex] = storedLeft;
                    m_BuildRightFootWeights[storedContributionIndex] = storedRight;
                }
                if (storedLeft > 0f)
                {
                    leftWeight += storedLeft;
                    leftValid &= StoredPose.HasFootFeatures;
                    if (StoredPose.HasFootFeatures)
                        leftAccumulator.Add(StoredPose.LeftFootFeatures, storedLeft);
                }
                if (storedRight > 0f)
                {
                    rightWeight += storedRight;
                    rightValid &= StoredPose.HasFootFeatures;
                    if (StoredPose.HasFootFeatures)
                        rightAccumulator.Add(StoredPose.RightFootFeatures, storedRight);
                }
            }
            for (int i = 0; i < entryCount; i++)
            {
                if (entries[i].IsEmpty)
                    continue;
                int contributionIndex = m_EntryContributionIndices[i];
                float sourceLeft = m_EntryBoneWeights[i * m_BoneCount + m_LeftFootBoneIndex];
                float sourceRight = m_EntryBoneWeights[i * m_BoneCount + m_RightFootBoneIndex];
                m_BuildLeftFootWeights[contributionIndex] += sourceLeft;
                m_BuildRightFootWeights[contributionIndex] += sourceRight;
                AnimationBlendSourcePoseFrame source = m_EntrySources[i];
                if (sourceLeft > 0f)
                {
                    leftWeight += sourceLeft;
                    leftValid &= source.HasFootFeatures;
                    if (source.HasFootFeatures)
                        leftAccumulator.Add(source.LeftFootFeatures, sourceLeft, source.VisualTimeScale);
                }
                if (sourceRight > 0f)
                {
                    rightWeight += sourceRight;
                    rightValid &= source.HasFootFeatures;
                    if (source.HasFootFeatures)
                        rightAccumulator.Add(source.RightFootFeatures, sourceRight, source.VisualTimeScale);
                }
            }
            hasFeatures = leftWeight > 0f && rightWeight > 0f && leftValid && rightValid;
            left = hasFeatures ? leftAccumulator.Resolve() : default;
            right = hasFeatures ? rightAccumulator.Resolve() : default;
        }

        PoseSlotFrame PublishPose(
            PoseSlotId poseSlotId,
            ulong completionIdentity,
            ulong continuityIdentity,
            float outputWeight,
            int contributionCount,
            int bufferIndex,
            AnimationFootFeatureSample leftFoot,
            AnimationFootFeatureSample rightFoot,
            bool hasFootFeatures,
            float deltaSeconds,
            bool preserveOutputVelocity = false)
        {
            int poseOffset = bufferIndex * m_BoneCount;
            if (!preserveOutputVelocity)
                UpdateOutputVelocity(bufferIndex, deltaSeconds);
            int contributionOffset = bufferIndex * m_ContributionCapacity;
            int denseOffset = bufferIndex * m_ContributionCapacity * m_BoneCount;
            for (int i = 0; i < contributionCount; i++)
            {
                m_OutputContributions[contributionOffset + i] = new AnimationPoseSourceContribution(
                    poseSlotId,
                    m_BuildKinds[i],
                    m_BuildPlaybackIds[i],
                    m_BuildProducerIndices[i],
                    m_BuildContributionContinuityIdentities[i],
                    Mathf.Clamp01(m_BuildScalarWeights[i]),
                    Mathf.Clamp01(m_BuildLeftFootWeights[i]),
                    Mathf.Clamp01(m_BuildRightFootWeights[i]));
                for (int boneIndex = 0; boneIndex < m_BoneCount; boneIndex++)
                {
                    m_OutputDenseContributionWeights[denseOffset + i * m_BoneCount + boneIndex] =
                        Mathf.Clamp01(GetDenseContributionWeight(i, boneIndex));
                }
            }
            var frame = new PoseSlotFrame(
                poseSlotId,
                completionIdentity,
                PoseSlotFrameAvailability.Pose,
                Mathf.Clamp01(outputWeight),
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(m_OutputPose, poseOffset, m_BoneCount),
                new AnimationReadOnlyBuffer<float>(m_OutputParameters, bufferIndex * m_ParameterCount, m_ParameterCount),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(m_OutputContributions, contributionOffset, contributionCount),
                new AnimationReadOnlyBuffer<float>(m_OutputDenseContributionWeights, denseOffset, contributionCount * m_BoneCount),
                leftFoot,
                rightFoot,
                hasFootFeatures,
                continuityIdentity);
            m_CurrentBufferIndex = bufferIndex;
            m_CurrentFrame = frame;
            m_HasFrame = true;
            return frame;
        }

        PoseSlotFrame PublishNoPose(
            PoseSlotId poseSlotId,
            ulong completionIdentity,
            ulong continuityIdentity,
            float deltaSeconds,
            out AnimationBlendStackInvalidReason invalidReason)
        {
            int bufferIndex = NextBufferIndex();
            int parameterOffset = bufferIndex * m_ParameterCount;
            Array.Clear(m_OutputParameters, parameterOffset, m_ParameterCount);
            var frame = new PoseSlotFrame(
                poseSlotId,
                completionIdentity,
                PoseSlotFrameAvailability.NoPose,
                0f,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(m_OutputPose, bufferIndex * m_BoneCount, 0),
                new AnimationReadOnlyBuffer<float>(m_OutputParameters, parameterOffset, m_ParameterCount),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(m_OutputContributions, bufferIndex * m_ContributionCapacity, 0),
                new AnimationReadOnlyBuffer<float>(m_OutputDenseContributionWeights, bufferIndex * m_ContributionCapacity * m_BoneCount, 0),
                default,
                default,
                false,
                continuityIdentity);
            m_CurrentBufferIndex = bufferIndex;
            m_CurrentFrame = frame;
            m_HasFrame = true;
            invalidReason = AnimationBlendStackInvalidReason.None;
            return frame;
        }

        PoseSlotFrame InvalidFrame(
            PoseSlotId poseSlotId,
            ulong completionIdentity,
            ulong continuityIdentity,
            AnimationBlendStackInvalidReason reason,
            out AnimationBlendStackInvalidReason invalidReason)
        {
            int bufferIndex = NextBufferIndex();
            var frame = new PoseSlotFrame(
                poseSlotId,
                completionIdentity,
                PoseSlotFrameAvailability.Invalid,
                0f,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(m_OutputPose, bufferIndex * m_BoneCount, 0),
                new AnimationReadOnlyBuffer<float>(m_OutputParameters, bufferIndex * m_ParameterCount, 0),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(m_OutputContributions, bufferIndex * m_ContributionCapacity, 0),
                new AnimationReadOnlyBuffer<float>(m_OutputDenseContributionWeights, bufferIndex * m_ContributionCapacity * m_BoneCount, 0),
                default,
                default,
                false,
                continuityIdentity);
            m_CurrentBufferIndex = bufferIndex;
            m_CurrentFrame = frame;
            m_HasFrame = true;
            invalidReason = reason;
            return frame;
        }

        void UpdateOutputVelocity(int nextBufferIndex, float deltaSeconds)
        {
            int nextOffset = nextBufferIndex * m_BoneCount;
            if (!m_HasFrame || m_CurrentFrame.Availability != PoseSlotFrameAvailability.Pose || deltaSeconds <= 0f)
            {
                Array.Clear(m_OutputVelocity, nextOffset, m_BoneCount);
                return;
            }
            for (int i = 0; i < m_BoneCount; i++)
            {
                m_OutputVelocity[nextOffset + i] = AnimationBlendPoseMath.Differentiate(
                    m_CurrentFrame.DenseLocalPose[i],
                    m_OutputPose[nextOffset + i],
                    deltaSeconds);
            }
        }

        bool CanDifferentiateOutput(float deltaSeconds) =>
            m_HasFrame && m_CurrentFrame.Availability == PoseSlotFrameAvailability.Pose && deltaSeconds > 0f;

        int AddContribution(
            AnimationPoseContributionKind kind,
            AnimationPlaybackId playbackId,
            int producerIndex,
            ulong contributionContinuityIdentity,
            ref int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (m_BuildKinds[i] == kind && m_BuildPlaybackIds[i].Equals(playbackId) &&
                    m_BuildProducerIndices[i] == producerIndex &&
                    m_BuildContributionContinuityIdentities[i] == contributionContinuityIdentity)
                    return i;
            }
            if (count == m_ContributionCapacity)
                throw new InvalidOperationException("Animation Blend contribution capacity was exceeded.");
            int index = count++;
            m_BuildKinds[index] = kind;
            m_BuildPlaybackIds[index] = playbackId;
            m_BuildProducerIndices[index] = producerIndex;
            m_BuildContributionContinuityIdentities[index] = contributionContinuityIdentity;
            return index;
        }

        void ResetBuildState(int entryCount)
        {
            Array.Clear(m_EntrySources, 0, entryCount);
            Array.Clear(m_EntryCurves, 0, entryCount);
            Array.Clear(m_EntryProfiles, 0, entryCount);
            for (int i = 0; i < entryCount; i++)
                m_EntryContributionIndices[i] = -1;
            Array.Clear(m_EntryBoneWeights, 0, entryCount * m_BoneCount);
            Array.Clear(m_EntryScalarWeights, 0, entryCount);
            Array.Clear(m_EntryMaximumWeights, 0, entryCount);
            Array.Clear(m_StoredBoneWeights, 0, m_StoredBoneWeights.Length);
            Array.Clear(m_BuildKinds, 0, m_BuildKinds.Length);
            Array.Clear(m_BuildPlaybackIds, 0, m_BuildPlaybackIds.Length);
            Array.Clear(m_BuildContributionContinuityIdentities, 0, m_BuildContributionContinuityIdentities.Length);
            Array.Clear(m_BuildProducerIndices, 0, m_BuildProducerIndices.Length);
            Array.Clear(m_BuildScalarWeights, 0, m_BuildScalarWeights.Length);
            Array.Clear(m_BuildLeftFootWeights, 0, m_BuildLeftFootWeights.Length);
            Array.Clear(m_BuildRightFootWeights, 0, m_BuildRightFootWeights.Length);
            Array.Clear(m_BuildDenseContributionWeights, 0, m_BuildDenseContributionWeights.Length);
            m_StoredMaximumWeight = 0f;
        }

        void AddDenseContributionWeight(int contributionIndex, int boneIndex, float value)
        {
            int index = contributionIndex * m_BoneCount + boneIndex;
            m_BuildDenseContributionWeights[index] += value;
        }

        void SetDenseContributionWeight(int contributionIndex, int boneIndex, float value)
        {
            m_BuildDenseContributionWeights[contributionIndex * m_BoneCount + boneIndex] = value;
        }

        float GetDenseContributionWeight(int contributionIndex, int boneIndex) =>
            m_BuildDenseContributionWeights[contributionIndex * m_BoneCount + boneIndex];

        int NextBufferIndex() => m_CurrentBufferIndex == 0 ? 1 : 0;

        public void Reset()
        {
            StoredPose.Clear();
            Inertial.Clear();
            m_HasFrame = false;
            m_CurrentFrame = default;
            m_CurrentBufferIndex = -1;
            Array.Clear(m_OutputVelocity, 0, m_OutputVelocity.Length);
        }
    }
}
