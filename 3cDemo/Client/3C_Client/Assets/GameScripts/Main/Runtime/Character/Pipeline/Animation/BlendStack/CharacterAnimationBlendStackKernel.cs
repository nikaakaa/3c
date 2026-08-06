using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.BlendStack
{
    internal enum CharacterAnimationBlendStackPushKind : byte
    {
        Continue = 1,
        Jump = 2,
        JumpWithStoredCapture = 3
    }

    internal readonly struct CharacterAnimationBlendStackPushRequest
    {
        internal CharacterAnimationBlendStackPushRequest(
            ulong entryIdentity,
            int sourceWorkspaceIndex,
            float durationSeconds,
            int curveIndex,
            int profileIndex,
            bool hardCut)
        {
            if (entryIdentity == 0 || sourceWorkspaceIndex < 0 ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f ||
                curveIndex < 0 || profileIndex < 0)
            {
                throw new ArgumentException("Animation Blend Stack push request is invalid.");
            }
            EntryIdentity = entryIdentity;
            SourceWorkspaceIndex = sourceWorkspaceIndex;
            DurationSeconds = hardCut ? 0f : durationSeconds;
            CurveIndex = curveIndex;
            ProfileIndex = profileIndex;
        }

        internal ulong EntryIdentity { get; }
        internal int SourceWorkspaceIndex { get; }
        internal float DurationSeconds { get; }
        internal int CurveIndex { get; }
        internal int ProfileIndex { get; }
    }

    internal readonly struct CharacterAnimationBlendStackRetirement
    {
        internal CharacterAnimationBlendStackRetirement(
            ulong entryIdentity,
            int sourceWorkspaceIndex)
        {
            if (entryIdentity == 0 || sourceWorkspaceIndex < 0)
                throw new ArgumentException("Animation Blend Stack retirement is invalid.");
            EntryIdentity = entryIdentity;
            SourceWorkspaceIndex = sourceWorkspaceIndex;
        }

        internal ulong EntryIdentity { get; }
        internal int SourceWorkspaceIndex { get; }
        internal bool IsValid => EntryIdentity != 0 && SourceWorkspaceIndex >= 0;
    }

    internal readonly struct CharacterAnimationBlendStackFramePlan
    {
        internal CharacterAnimationBlendStackFramePlan(
            ulong frameIdentity,
            int entryCount,
            bool usesStoredPose,
            bool capturesPreviousOutput,
            float outputWeight)
        {
            if (frameIdentity == 0 || entryCount <= 0 ||
                !float.IsFinite(outputWeight) || outputWeight <= 0f || outputWeight > 1f)
            {
                throw new ArgumentException("Animation Blend Stack frame plan is invalid.");
            }
            FrameIdentity = frameIdentity;
            EntryCount = entryCount;
            UsesStoredPose = usesStoredPose;
            CapturesPreviousOutput = capturesPreviousOutput;
            OutputWeight = outputWeight;
        }

        internal ulong FrameIdentity { get; }
        internal int EntryCount { get; }
        internal bool UsesStoredPose { get; }
        internal bool CapturesPreviousOutput { get; }
        internal float OutputWeight { get; }
        internal bool IsValid => FrameIdentity != 0 && EntryCount > 0 &&
                                 float.IsFinite(OutputWeight) && OutputWeight > 0f && OutputWeight <= 1f;
    }

    internal struct CharacterAnimationBlendStackKernelEntry
    {
        internal CharacterAnimationBlendStackKernelEntry(
            in CharacterAnimationBlendStackPushRequest request)
        {
            EntryIdentity = request.EntryIdentity;
            SourceWorkspaceIndex = request.SourceWorkspaceIndex;
            DurationSeconds = request.DurationSeconds;
            CurveIndex = request.CurveIndex;
            ProfileIndex = request.ProfileIndex;
            ElapsedSeconds = 0f;
            DepthDurationScale = 1f;
            PushDepth = 0;
        }

        internal ulong EntryIdentity;
        internal int SourceWorkspaceIndex;
        internal float DurationSeconds;
        internal int CurveIndex;
        internal int ProfileIndex;
        internal float ElapsedSeconds;
        internal float DepthDurationScale;
        internal int PushDepth;

        internal void Advance(float deltaSeconds)
        {
            float elapsed = ElapsedSeconds + deltaSeconds;
            if (!float.IsFinite(elapsed))
                throw new InvalidOperationException("Animation Blend Stack clock overflowed.");
            ElapsedSeconds = elapsed;
        }

        internal void IncreaseDepth(float multiplier)
        {
            if (PushDepth == int.MaxValue)
                throw new InvalidOperationException("Animation Blend Stack push depth overflowed.");
            ElapsedSeconds *= multiplier;
            DepthDurationScale *= multiplier;
            if (!float.IsFinite(ElapsedSeconds) || !float.IsFinite(DepthDurationScale) ||
                DepthDurationScale <= 0f)
            {
                throw new InvalidOperationException("Animation Blend Stack depth clock is invalid.");
            }
            PushDepth++;
        }

        internal float OutputAlpha(
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles)
        {
            AnimationBlendProfilePayload profile = profiles.Require(ProfileIndex);
            float duration = DurationSeconds * profile.GlobalDurationMultiplier * DepthDurationScale;
            float normalized = duration <= 0f ? 1f : Mathf.Clamp01(ElapsedSeconds / duration);
            return AnimationBlendCurveEvaluator.Evaluate(curves.Require(CurveIndex), normalized);
        }

        internal float BoneAlpha(
            int boneIndex,
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles)
        {
            AnimationBlendProfilePayload profile = profiles.Require(ProfileIndex);
            float duration = DurationSeconds * profile.DenseDurationMultipliers[boneIndex] * DepthDurationScale;
            float normalized = duration <= 0f ? 1f : Mathf.Clamp01(ElapsedSeconds / duration);
            return AnimationBlendCurveEvaluator.Evaluate(curves.Require(CurveIndex), normalized);
        }
    }

    internal sealed class CharacterAnimationBlendStackOwnerWorkspace
    {
        readonly CharacterAnimationBlendStackKernelEntry[] m_CommittedEntries;
        readonly CharacterAnimationBlendStackKernelEntry[] m_PendingEntries;
        readonly float[] m_EntryScalarWeights;
        readonly float[] m_EntryBoneWeights;
        readonly float[] m_EntryMaximumWeights;
        readonly float[] m_CommittedStoredBoneWeights;
        readonly float[] m_PendingStoredBoneWeights;
        readonly float[] m_CommittedLastBoneWeights;
        readonly float[] m_PendingLastBoneWeights;
        readonly CharacterAnimationBlendStackRetirement[] m_Retirements;

        int m_CommittedEntryCount;
        int m_PendingEntryCount;
        int m_RetirementCount;
        ulong m_CommittedLastEntryIdentity;
        ulong m_PendingLastEntryIdentity;
        ulong m_CommittedCompletionIdentity;
        ulong m_PendingFrameIdentity;
        float m_CommittedStoredOutputWeight;
        float m_PendingStoredOutputWeight;
        float m_CommittedLastOutputWeight;
        float m_PendingLastOutputWeight;
        bool m_CommittedHasStoredPose;
        bool m_PendingHasStoredPose;
        bool m_CommittedHasCompletedOutput;
        bool m_PendingCapturesPreviousOutput;
        bool m_FrameOpen;

        internal CharacterAnimationBlendStackOwnerWorkspace(
            int entryCapacity,
            int boneCount)
        {
            if (entryCapacity < 2 || boneCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(entryCapacity));
            m_CommittedEntries = new CharacterAnimationBlendStackKernelEntry[entryCapacity];
            m_PendingEntries = new CharacterAnimationBlendStackKernelEntry[entryCapacity];
            m_EntryScalarWeights = new float[entryCapacity];
            m_EntryBoneWeights = new float[checked(entryCapacity * boneCount)];
            m_EntryMaximumWeights = new float[entryCapacity];
            m_CommittedStoredBoneWeights = new float[boneCount];
            m_PendingStoredBoneWeights = new float[boneCount];
            m_CommittedLastBoneWeights = new float[boneCount];
            m_PendingLastBoneWeights = new float[boneCount];
            m_Retirements = new CharacterAnimationBlendStackRetirement[entryCapacity];
        }

        internal int EntryCapacity => m_CommittedEntries.Length;
        internal int BoneCount => m_CommittedStoredBoneWeights.Length;
        internal int EntryCount => m_FrameOpen ? m_PendingEntryCount : m_CommittedEntryCount;
        internal int RetirementCount => m_RetirementCount;
        internal bool CapturesPreviousOutput => m_FrameOpen && m_PendingCapturesPreviousOutput;

        internal ulong GetEntryIdentity(int index) => ReadEntry(index).EntryIdentity;
        internal int GetSourceWorkspaceIndex(int index) => ReadEntry(index).SourceWorkspaceIndex;
        internal float GetEntryScalarWeight(int index)
        {
            RequireEntryIndex(index);
            return m_EntryScalarWeights[index];
        }

        internal float GetEntryBoneWeight(int index, int boneIndex)
        {
            RequireEntryIndex(index);
            if ((uint)boneIndex >= (uint)BoneCount)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_EntryBoneWeights[index * BoneCount + boneIndex];
        }

        internal float GetStoredBoneWeight(int boneIndex)
        {
            if (!m_FrameOpen || (uint)boneIndex >= (uint)BoneCount)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_PendingStoredBoneWeights[boneIndex];
        }

        internal float StoredOutputWeight
        {
            get
            {
                RequireOpenFrame();
                return m_PendingStoredOutputWeight;
            }
        }

        internal CharacterAnimationBlendStackRetirement GetRetirement(int index)
        {
            if ((uint)index >= (uint)m_RetirementCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return m_Retirements[index];
        }

        internal void BeginFrame(ulong frameIdentity, float deltaSeconds)
        {
            if (m_FrameOpen || frameIdentity == 0 ||
                !float.IsFinite(deltaSeconds) || deltaSeconds < 0f ||
                m_CommittedCompletionIdentity != 0 && frameIdentity <= m_CommittedCompletionIdentity)
            {
                throw new InvalidOperationException("Animation Blend Stack frame identity or delta is invalid.");
            }
            Array.Copy(m_CommittedEntries, m_PendingEntries, m_CommittedEntryCount);
            Array.Clear(m_PendingEntries, m_CommittedEntryCount, EntryCapacity - m_CommittedEntryCount);
            Array.Copy(m_CommittedStoredBoneWeights, m_PendingStoredBoneWeights, BoneCount);
            Array.Clear(m_EntryScalarWeights, 0, m_EntryScalarWeights.Length);
            Array.Clear(m_EntryBoneWeights, 0, m_EntryBoneWeights.Length);
            Array.Clear(m_EntryMaximumWeights, 0, m_EntryMaximumWeights.Length);
            Array.Clear(m_Retirements, 0, m_Retirements.Length);
            m_PendingEntryCount = m_CommittedEntryCount;
            m_PendingLastEntryIdentity = m_CommittedLastEntryIdentity;
            m_PendingStoredOutputWeight = m_CommittedStoredOutputWeight;
            m_PendingHasStoredPose = m_CommittedHasStoredPose;
            m_PendingCapturesPreviousOutput = false;
            m_RetirementCount = 0;
            m_PendingFrameIdentity = frameIdentity;
            for (int i = 0; i < m_PendingEntryCount; i++)
                m_PendingEntries[i].Advance(deltaSeconds);
            m_FrameOpen = true;
        }

        internal CharacterAnimationBlendStackPushKind Push(
            in CharacterAnimationBlendStackPushRequest request,
            AnimationBlendStackPolicyPayload policy)
        {
            RequireOpenFrame();
            policy?.RequireValid();
            if (policy == null || policy.MaxActiveSourceEntries != EntryCapacity)
                throw new InvalidOperationException("Animation Blend Stack policy and owner workspace differ.");
            if (m_PendingEntryCount > 0 &&
                m_PendingEntries[m_PendingEntryCount - 1].EntryIdentity == request.EntryIdentity)
            {
                CharacterAnimationBlendStackKernelEntry current = m_PendingEntries[m_PendingEntryCount - 1];
                if (current.SourceWorkspaceIndex != request.SourceWorkspaceIndex ||
                    current.CurveIndex != request.CurveIndex ||
                    current.ProfileIndex != request.ProfileIndex)
                {
                    throw new InvalidOperationException("Animation Blend Stack Continue changed the current entry identity.");
                }
                return CharacterAnimationBlendStackPushKind.Continue;
            }
            if (request.EntryIdentity <= m_PendingLastEntryIdentity)
                throw new InvalidOperationException("Animation Blend Stack Jump identity is not strictly increasing.");

            bool replaceHistory = m_PendingEntryCount == EntryCapacity ||
                                  m_PendingEntryCount > 0 &&
                                  m_PendingEntries[m_PendingEntryCount - 1].ElapsedSeconds <=
                                  policy.MaxBlendInTimeToReplaceNewest;
            bool capture = replaceHistory && m_CommittedHasCompletedOutput;
            if (replaceHistory && !capture)
                throw new InvalidOperationException("Animation Blend Stack cannot replace live history before one output frame completes.");
            if (replaceHistory)
            {
                for (int i = 0; i < m_PendingEntryCount; i++)
                    AddRetirement(m_PendingEntries[i]);
                Array.Clear(m_PendingEntries, 0, m_PendingEntryCount);
                m_PendingEntryCount = 0;
                m_PendingHasStoredPose = true;
                m_PendingCapturesPreviousOutput = true;
                m_PendingStoredOutputWeight = m_CommittedLastOutputWeight;
                Array.Copy(m_CommittedLastBoneWeights, m_PendingStoredBoneWeights, BoneCount);
            }
            else
            {
                for (int i = 0; i < m_PendingEntryCount; i++)
                {
                    CharacterAnimationBlendStackKernelEntry entry = m_PendingEntries[i];
                    entry.IncreaseDepth(policy.DepthBlendTimeMultiplier);
                    m_PendingEntries[i] = entry;
                }
            }
            m_PendingEntries[m_PendingEntryCount++] = new CharacterAnimationBlendStackKernelEntry(in request);
            m_PendingLastEntryIdentity = request.EntryIdentity;
            return capture
                ? CharacterAnimationBlendStackPushKind.JumpWithStoredCapture
                : CharacterAnimationBlendStackPushKind.Jump;
        }

        internal CharacterAnimationBlendStackFramePlan Evaluate(
            AnimationBlendCurveCatalogPayload curves,
            AnimationBlendProfileCatalogPayload profiles)
        {
            RequireOpenFrame();
            if (curves == null || profiles == null || m_PendingEntryCount == 0)
                throw new InvalidOperationException("Animation Blend Stack has no evaluable entries or catalogs.");
            float scalarResidual = 1f;
            for (int i = m_PendingEntryCount - 1; i >= 0; i--)
            {
                float alpha = RequireNormalized(m_PendingEntries[i].OutputAlpha(curves, profiles));
                float weight = scalarResidual * alpha;
                m_EntryScalarWeights[i] = weight;
                m_EntryMaximumWeights[i] = weight;
                scalarResidual *= 1f - alpha;
            }
            float storedScalarWeight = m_PendingHasStoredPose
                ? scalarResidual * m_PendingStoredOutputWeight
                : 0f;
            float outputWeight = storedScalarWeight;
            for (int i = 0; i < m_PendingEntryCount; i++)
                outputWeight += m_EntryScalarWeights[i];
            outputWeight = RequireNormalized(outputWeight);

            bool hasDenseOutput = false;
            for (int boneIndex = 0; boneIndex < BoneCount; boneIndex++)
            {
                float residual = 1f;
                float boneOutputWeight = 0f;
                for (int i = m_PendingEntryCount - 1; i >= 0; i--)
                {
                    float alpha = RequireNormalized(m_PendingEntries[i].BoneAlpha(boneIndex, curves, profiles));
                    float weight = residual * alpha;
                    m_EntryBoneWeights[i * BoneCount + boneIndex] = weight;
                    m_EntryMaximumWeights[i] = Mathf.Max(m_EntryMaximumWeights[i], weight);
                    boneOutputWeight += weight;
                    residual *= 1f - alpha;
                }
                if (m_PendingHasStoredPose)
                    boneOutputWeight += residual * m_PendingStoredBoneWeights[boneIndex];
                boneOutputWeight = RequireNormalized(boneOutputWeight);
                hasDenseOutput |= boneOutputWeight > 0f;
            }
            if (outputWeight <= 0f && !hasDenseOutput)
                throw new InvalidOperationException("Animation Blend Stack produced no required Pose contribution.");
            return new CharacterAnimationBlendStackFramePlan(
                m_PendingFrameIdentity,
                m_PendingEntryCount,
                m_PendingHasStoredPose,
                m_PendingCapturesPreviousOutput,
                outputWeight);
        }

        internal void CompleteFrame(
            ulong frameIdentity,
            float outputWeight,
            float[] denseBoneOutputWeights)
        {
            RequireOpenFrame();
            if (frameIdentity != m_PendingFrameIdentity ||
                !float.IsFinite(outputWeight) || outputWeight <= 0f || outputWeight > 1f ||
                denseBoneOutputWeights == null || denseBoneOutputWeights.Length != BoneCount)
            {
                throw new InvalidOperationException("Animation Blend Stack completion is invalid.");
            }
            for (int i = 0; i < BoneCount; i++)
                m_PendingLastBoneWeights[i] = RequireNormalized(denseBoneOutputWeights[i]);
            m_PendingLastOutputWeight = outputWeight;
            RetireZeroWeightEntries();
            CommitPending(frameIdentity);
        }

        internal void DiscardFrame(ulong frameIdentity)
        {
            RequireOpenFrame();
            if (frameIdentity != m_PendingFrameIdentity)
                throw new InvalidOperationException("Animation Blend Stack discard identity is stale.");
            m_FrameOpen = false;
            m_PendingFrameIdentity = 0;
            m_RetirementCount = 0;
        }

        internal void Reset()
        {
            if (m_FrameOpen)
                throw new InvalidOperationException("Animation Blend Stack cannot reset during a frame.");
            Array.Clear(m_CommittedEntries, 0, m_CommittedEntries.Length);
            Array.Clear(m_CommittedStoredBoneWeights, 0, BoneCount);
            Array.Clear(m_CommittedLastBoneWeights, 0, BoneCount);
            m_CommittedEntryCount = 0;
            m_CommittedLastEntryIdentity = 0;
            m_CommittedCompletionIdentity = 0;
            m_CommittedStoredOutputWeight = 0f;
            m_CommittedLastOutputWeight = 0f;
            m_CommittedHasStoredPose = false;
            m_CommittedHasCompletedOutput = false;
        }

        void RetireZeroWeightEntries()
        {
            if (m_PendingEntryCount <= 1)
                return;
            int write = 0;
            for (int i = 0; i < m_PendingEntryCount; i++)
            {
                CharacterAnimationBlendStackKernelEntry entry = m_PendingEntries[i];
                if (i == m_PendingEntryCount - 1 || m_EntryMaximumWeights[i] > 0f)
                    m_PendingEntries[write++] = entry;
                else
                    AddRetirement(entry);
            }
            Array.Clear(m_PendingEntries, write, m_PendingEntryCount - write);
            m_PendingEntryCount = write;
        }

        void CommitPending(ulong frameIdentity)
        {
            Array.Copy(m_PendingEntries, m_CommittedEntries, m_PendingEntryCount);
            Array.Clear(m_CommittedEntries, m_PendingEntryCount, EntryCapacity - m_PendingEntryCount);
            Array.Copy(m_PendingStoredBoneWeights, m_CommittedStoredBoneWeights, BoneCount);
            Array.Copy(m_PendingLastBoneWeights, m_CommittedLastBoneWeights, BoneCount);
            m_CommittedEntryCount = m_PendingEntryCount;
            m_CommittedLastEntryIdentity = m_PendingLastEntryIdentity;
            m_CommittedCompletionIdentity = frameIdentity;
            m_CommittedStoredOutputWeight = m_PendingStoredOutputWeight;
            m_CommittedLastOutputWeight = m_PendingLastOutputWeight;
            m_CommittedHasStoredPose = m_PendingHasStoredPose;
            m_CommittedHasCompletedOutput = true;
            m_FrameOpen = false;
            m_PendingFrameIdentity = 0;
        }

        void AddRetirement(in CharacterAnimationBlendStackKernelEntry entry)
        {
            for (int i = 0; i < m_RetirementCount; i++)
            {
                if (m_Retirements[i].EntryIdentity == entry.EntryIdentity)
                    return;
            }
            if (m_RetirementCount == m_Retirements.Length)
                throw new InvalidOperationException("Animation Blend Stack retirement workspace was exceeded.");
            m_Retirements[m_RetirementCount++] = new CharacterAnimationBlendStackRetirement(
                entry.EntryIdentity,
                entry.SourceWorkspaceIndex);
        }

        CharacterAnimationBlendStackKernelEntry ReadEntry(int index)
        {
            RequireEntryIndex(index);
            return m_FrameOpen ? m_PendingEntries[index] : m_CommittedEntries[index];
        }

        void RequireEntryIndex(int index)
        {
            if ((uint)index >= (uint)EntryCount)
                throw new ArgumentOutOfRangeException(nameof(index));
        }

        void RequireOpenFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException("Animation Blend Stack has no open frame.");
        }

        static float RequireNormalized(float value)
        {
            if (!float.IsFinite(value) || value < -0.0001f || value > 1.0001f)
                throw new InvalidOperationException("Animation Blend Stack weight is outside [0, 1].");
            return Mathf.Clamp01(value);
        }
    }
}
