using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Editor.MotionMatching
{
    public readonly struct MotionMatchingSampleAddress
    {
        public MotionMatchingSampleAddress(int sampleIndex, int segmentIndex, int ordinal, int segmentSampleCount, float sampleTime)
        {
            SampleIndex = sampleIndex;
            SegmentIndex = segmentIndex;
            Ordinal = ordinal;
            SegmentSampleCount = segmentSampleCount;
            SampleTime = sampleTime;
        }

        public int SampleIndex { get; }
        public int SegmentIndex { get; }
        public int Ordinal { get; }
        public int SegmentSampleCount { get; }
        public float SampleTime { get; }
        public bool IsLastInSegment => Ordinal == SegmentSampleCount - 1;
    }

    public sealed class MotionMatchingSampleBuildRecord
    {
        public MotionMatchingSampleBuildRecord(
            MotionMatchingSampleAddress address,
            CharacterMotionMatchingSampleId sampleId,
            CharacterMotionMatchingSegmentId segmentId,
            CharacterMotionMatchingSearchDomainId searchDomainId,
            int clipBindingIndex,
            bool canInitialize,
            bool canJumpInto,
            bool entryExcluded,
            bool exitExcluded,
            bool terminal,
            MotionMatchingFootContactMask contactMask,
            Vector2 rootPlanarVelocity,
            float rootYawVelocityDegrees,
            Vector3 leftFootRootPosition,
            Vector3 rightFootRootPosition,
            AnimationFootFeatureSample leftFoot,
            AnimationFootFeatureSample rightFoot,
            float[] rawFeatures)
        {
            Address = address;
            SampleId = sampleId;
            SegmentId = segmentId;
            SearchDomainId = searchDomainId;
            ClipBindingIndex = clipBindingIndex;
            CanInitialize = canInitialize;
            CanJumpInto = canJumpInto;
            EntryExcluded = entryExcluded;
            ExitExcluded = exitExcluded;
            Terminal = terminal;
            ContactMask = contactMask;
            RootPlanarVelocity = rootPlanarVelocity;
            RootYawVelocityDegrees = rootYawVelocityDegrees;
            LeftFootRootPosition = leftFootRootPosition;
            RightFootRootPosition = rightFootRootPosition;
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
            RawFeatures = rawFeatures ?? throw new ArgumentNullException(nameof(rawFeatures));
            NextSampleIndex = -1;
        }

        public MotionMatchingSampleAddress Address { get; }
        public CharacterMotionMatchingSampleId SampleId { get; }
        public CharacterMotionMatchingSegmentId SegmentId { get; }
        public CharacterMotionMatchingSearchDomainId SearchDomainId { get; }
        public int ClipBindingIndex { get; }
        public bool CanInitialize { get; }
        public bool CanJumpInto { get; }
        public bool EntryExcluded { get; }
        public bool ExitExcluded { get; }
        public bool Terminal { get; }
        public MotionMatchingFootContactMask ContactMask { get; }
        public Vector2 RootPlanarVelocity { get; }
        public float RootYawVelocityDegrees { get; }
        public Vector3 LeftFootRootPosition { get; }
        public Vector3 RightFootRootPosition { get; }
        public AnimationFootFeatureSample LeftFoot { get; }
        public AnimationFootFeatureSample RightFoot { get; }
        public float[] RawFeatures { get; }
        public int NextSampleIndex { get; set; }

        public MotionMatchingSamplePayload CreatePayload() => new MotionMatchingSamplePayload(
            SampleId, SegmentId, SearchDomainId, ClipBindingIndex, Address.SampleTime,
            CanInitialize, CanJumpInto, EntryExcluded, ExitExcluded, Terminal, NextSampleIndex,
            ContactMask, RootPlanarVelocity, RootYawVelocityDegrees, LeftFootRootPosition,
            RightFootRootPosition, LeftFoot, RightFoot);
    }

    public sealed class MotionMatchingDatabaseSampler : IDisposable
    {
        readonly struct PoseSnapshot
        {
            public PoseSnapshot(Vector3 rootPosition, Quaternion rootRotation, Vector3[] bonePositions, Vector3 leftFoot, Vector3 rightFoot)
            {
                RootPosition = rootPosition;
                RootRotation = rootRotation;
                BonePositions = bonePositions;
                LeftFoot = leftFoot;
                RightFoot = rightFoot;
            }

            public Vector3 RootPosition { get; }
            public Quaternion RootRotation { get; }
            public Vector3[] BonePositions { get; }
            public Vector3 LeftFoot { get; }
            public Vector3 RightFoot { get; }
        }

        readonly MotionMatchingDatabaseBuildRequest m_Request;
        readonly GameObject m_Instance;
        readonly CharacterAnimationRigBinding m_Binding;
        readonly int[] m_FeatureBoneIndices;
        readonly int m_LeftFootIndex;
        readonly int m_RightFootIndex;
        readonly bool m_OwnsAnimationMode;
        readonly Dictionary<CharacterMotionMatchingSegmentId, int> m_SegmentIndices;
        bool m_Disposed;

        public MotionMatchingDatabaseSampler(MotionMatchingDatabaseBuildRequest request)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Instance = Object.Instantiate(request.SamplingRigPrefab);
            m_Instance.name = $"MotionMatchingSamplingRig:{request.Database.DatabaseId}";
            m_Instance.hideFlags = HideFlags.HideAndDontSave;
            m_Binding = m_Instance.GetComponentInChildren<CharacterAnimationRigBinding>(true);
            if (!m_Binding || !m_Binding.Animator || m_Binding.PhysicalBones.Count != request.Database.TargetRig.PhysicalBoneCount)
                throw new InvalidOperationException("Instantiated Motion Matching Sampling Rig does not match the preflight binding.");
            m_FeatureBoneIndices = new int[request.FeatureSchema.BoneCount];
            for (int i = 0; i < m_FeatureBoneIndices.Length; i++)
                m_FeatureBoneIndices[i] = request.Database.TargetRig.RequirePhysicalBoneIndex(new AnimationBoneId(request.FeatureSchema.GetBoneId(i)));
            m_LeftFootIndex = request.Database.TargetRig.RequirePhysicalBoneIndex(request.Database.TargetRig.LeftLeg.AnkleBoneId);
            m_RightFootIndex = request.Database.TargetRig.RequirePhysicalBoneIndex(request.Database.TargetRig.RightLeg.AnkleBoneId);
            m_SegmentIndices = new Dictionary<CharacterMotionMatchingSegmentId, int>();
            for (int i = 0; i < request.SegmentCount; i++)
                m_SegmentIndices.Add(request.GetSegment(i).SegmentId, i);
            m_OwnsAnimationMode = !AnimationMode.InAnimationMode();
            if (m_OwnsAnimationMode)
                AnimationMode.StartAnimationMode();
        }

        public static MotionMatchingSampleAddress[] CreateAddresses(MotionMatchingDatabaseBuildRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            var values = new List<MotionMatchingSampleAddress>(request.EstimatedSampleCount);
            for (int segmentIndex = 0; segmentIndex < request.SegmentCount; segmentIndex++)
            {
                MotionMatchingSegmentBuildInput segment = request.GetSegment(segmentIndex);
                int count = Mathf.Max(1, Mathf.CeilToInt((segment.EndTime - segment.StartTime) * request.Database.SampleRate));
                for (int ordinal = 0; ordinal < count; ordinal++)
                {
                    float time = segment.StartTime + ordinal / request.Database.SampleRate;
                    values.Add(new MotionMatchingSampleAddress(values.Count, segmentIndex, ordinal, count, time));
                }
            }
            if (values.Count > request.SearchPolicy.MaximumAdmittedSampleCount)
                throw new InvalidOperationException($"Database sample count {values.Count} exceeds Search Policy maximum {request.SearchPolicy.MaximumAdmittedSampleCount}.");
            return values.ToArray();
        }

        public MotionMatchingSampleBuildRecord Sample(MotionMatchingSampleAddress address)
        {
            RequireAlive();
            MotionMatchingSegmentBuildInput segment = m_Request.GetSegment(address.SegmentIndex);
            MotionMatchingResolvedClipBuildInput clip = m_Request.GetClip(segment.ClipBindingIndex);
            PoseSnapshot current = SamplePose(clip, address.SampleTime);
            float deltaTime = 1f / m_Request.Database.SampleRate;
            ResolveForward(address.SegmentIndex, address.SampleTime, deltaTime, out Vector3 nextPosition, out Quaternion nextRotation);
            Vector2 rootVelocity = new Vector2(nextPosition.x, nextPosition.z) / deltaTime;
            float rootYawVelocity = Vector3.SignedAngle(Vector3.forward, nextRotation * Vector3.forward, Vector3.up) / deltaTime;
            if (!IsFinite(rootVelocity) || !float.IsFinite(rootYawVelocity) || rootVelocity.magnitude > 100f || Mathf.Abs(rootYawVelocity) > 1440f)
                throw new InvalidOperationException($"Segment '{segment.SegmentId}' sample {address.SampleTime:R} contains a non-finite or discontinuous Motion Root.");

            float normalizedTime = clip.Clip.length <= 0f ? 0f : Mathf.Clamp01(address.SampleTime / clip.Clip.length);
            AnimationFootFeatureSample leftFoot = clip.FootArtifact.Features.Left.Sample(normalizedTime);
            AnimationFootFeatureSample rightFoot = clip.FootArtifact.Features.Right.Sample(normalizedTime);
            MotionMatchingFootContactMask contacts = MotionMatchingFootContactMask.None;
            if (leftFoot.PlantConfidence >= 0.5f)
                contacts |= MotionMatchingFootContactMask.Left;
            if (rightFoot.PlantConfidence >= 0.5f)
                contacts |= MotionMatchingFootContactMask.Right;

            float[] raw = new float[m_Request.FeatureSchema.DenseFeatureCount];
            FillRawFeatures(raw, address, clip, current, leftFoot, rightFoot);
            bool entryExcluded = address.SampleTime < segment.StartTime + segment.EntryExclusion;
            bool exitExcluded = address.SampleTime > segment.EndTime - segment.ExitExclusion;
            return new MotionMatchingSampleBuildRecord(
                address,
                new CharacterMotionMatchingSampleId(checked((uint)address.SampleIndex + 1u)),
                segment.SegmentId,
                m_Request.Database.SearchDomainId,
                segment.ClipBindingIndex,
                segment.CanInitialize && !entryExcluded && !exitExcluded,
                segment.CanJumpInto && !entryExcluded && !exitExcluded,
                entryExcluded,
                exitExcluded,
                address.IsLastInSegment && segment.Terminal,
                contacts,
                rootVelocity,
                rootYawVelocity,
                current.LeftFoot,
                current.RightFoot,
                leftFoot,
                rightFoot,
                raw);
        }

        void FillRawFeatures(
            float[] raw,
            MotionMatchingSampleAddress address,
            MotionMatchingResolvedClipBuildInput clip,
            PoseSnapshot current,
            AnimationFootFeatureSample leftFoot,
            AnimationFootFeatureSample rightFoot)
        {
            for (int rangeIndex = 0; rangeIndex < m_Request.FeatureSchema.FeatureRangeCount; rangeIndex++)
            {
                MotionMatchingFeatureRange range = m_Request.FeatureSchema.GetFeatureRange(rangeIndex);
                switch (range.Group)
                {
                    case MotionMatchingCostGroup.TrajectoryPosition:
                    case MotionMatchingCostGroup.TrajectoryFacing:
                    case MotionMatchingCostGroup.TrajectoryVelocity:
                        FillTrajectory(raw, range, address);
                        break;
                    case MotionMatchingCostGroup.PosePosition:
                    case MotionMatchingCostGroup.PoseVelocity:
                        FillPose(raw, range, clip, address.SampleTime, range.Group == MotionMatchingCostGroup.PoseVelocity);
                        break;
                    case MotionMatchingCostGroup.ContactSoft:
                        FillContact(raw, range, leftFoot, rightFoot);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported dense Motion Matching feature group '{range.Group}'.");
                }
            }
            for (int i = 0; i < raw.Length; i++)
            {
                if (!float.IsFinite(raw[i]))
                    throw new InvalidOperationException($"Motion Matching sample #{address.SampleIndex} feature #{i} is non-finite.");
            }
        }

        void FillTrajectory(float[] raw, MotionMatchingFeatureRange range, MotionMatchingSampleAddress address)
        {
            if (range.Count != m_Request.TrajectoryPolicy.PointCount * 2)
                throw new InvalidOperationException($"Dense feature range '{range.Group}' does not match Trajectory Policy points.");
            for (int i = 0; i < m_Request.TrajectoryPolicy.PointCount; i++)
            {
                float time = m_Request.TrajectoryPolicy.GetPoint(i).TimeOffset;
                ResolveForward(address.SegmentIndex, address.SampleTime, time, out Vector3 position, out Quaternion rotation);
                Vector3 facing = rotation * Vector3.forward;
                int cursor = range.Offset + i * 2;
                if (range.Group == MotionMatchingCostGroup.TrajectoryPosition)
                {
                    raw[cursor] = position.x;
                    raw[cursor + 1] = position.z;
                }
                else if (range.Group == MotionMatchingCostGroup.TrajectoryFacing)
                {
                    Vector2 planar = new Vector2(facing.x, facing.z).normalized;
                    raw[cursor] = planar.x;
                    raw[cursor + 1] = planar.y;
                }
                else
                {
                    raw[cursor] = time <= 0f ? 0f : position.x / time;
                    raw[cursor + 1] = time <= 0f ? 0f : position.z / time;
                }
            }
        }

        void FillPose(float[] raw, MotionMatchingFeatureRange range, MotionMatchingResolvedClipBuildInput clip, float sampleTime, bool velocity)
        {
            int expected = m_Request.FeatureSchema.HistoryHorizonCount * m_FeatureBoneIndices.Length * 3;
            if (range.Count != expected)
                throw new InvalidOperationException($"Dense feature range '{range.Group}' does not match Pose history layout.");
            float deltaTime = 1f / m_Request.Database.SampleRate;
            int cursor = range.Offset;
            for (int horizonIndex = 0; horizonIndex < m_Request.FeatureSchema.HistoryHorizonCount; horizonIndex++)
            {
                float time = Mathf.Clamp(sampleTime + m_Request.FeatureSchema.GetHistoryHorizon(horizonIndex), 0f, clip.Clip.length);
                PoseSnapshot pose = SamplePose(clip, time);
                PoseSnapshot previous = velocity ? SamplePose(clip, Mathf.Max(0f, time - deltaTime)) : default;
                float actualDelta = velocity ? Mathf.Max(deltaTime, time - Mathf.Max(0f, time - deltaTime)) : 1f;
                for (int boneIndex = 0; boneIndex < pose.BonePositions.Length; boneIndex++)
                {
                    Vector3 value = velocity
                        ? (pose.BonePositions[boneIndex] - previous.BonePositions[boneIndex]) / actualDelta
                        : pose.BonePositions[boneIndex];
                    raw[cursor++] = value.x;
                    raw[cursor++] = value.y;
                    raw[cursor++] = value.z;
                }
            }
        }

        static void FillContact(float[] raw, MotionMatchingFeatureRange range, AnimationFootFeatureSample left, AnimationFootFeatureSample right)
        {
            if (range.Count != 8)
                throw new InvalidOperationException("Dense Contact feature range must contain eight values.");
            raw[range.Offset] = left.PlantConfidence;
            raw[range.Offset + 1] = left.SoleHeight;
            raw[range.Offset + 2] = left.SoleLocalVelocity.magnitude;
            raw[range.Offset + 3] = left.PredictedStep.Confidence;
            raw[range.Offset + 4] = right.PlantConfidence;
            raw[range.Offset + 5] = right.SoleHeight;
            raw[range.Offset + 6] = right.SoleLocalVelocity.magnitude;
            raw[range.Offset + 7] = right.PredictedStep.Confidence;
        }

        PoseSnapshot SamplePose(MotionMatchingResolvedClipBuildInput clip, float time)
        {
            AnimationMode.BeginSampling();
            try
            {
                AnimationMode.SampleAnimationClip(m_Instance, clip.Clip, Mathf.Clamp(time, 0f, clip.Clip.length));
            }
            finally
            {
                AnimationMode.EndSampling();
            }
            int rootIndex = m_Request.Database.TargetRig.RequirePhysicalBoneIndex(clip.MotionRootBoneId);
            Transform root = m_Binding.PhysicalBones[rootIndex];
            if (!root)
                throw new InvalidOperationException($"Motion Root Bone '{clip.MotionRootBoneId}' is missing from Sampling Rig.");
            Vector3 rootPosition = m_Instance.transform.InverseTransformPoint(root.position);
            Quaternion rootRotation = Canonical(Quaternion.Inverse(m_Instance.transform.rotation) * root.rotation);
            var bones = new Vector3[m_FeatureBoneIndices.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = m_Binding.PhysicalBones[m_FeatureBoneIndices[i]];
                if (!bone)
                    throw new InvalidOperationException($"Feature Bone #{i} is missing from Sampling Rig.");
                bones[i] = root.InverseTransformPoint(bone.position);
            }
            Transform left = m_Binding.PhysicalBones[m_LeftFootIndex];
            Transform right = m_Binding.PhysicalBones[m_RightFootIndex];
            Vector3 leftPosition = root.InverseTransformPoint(left.position);
            Vector3 rightPosition = root.InverseTransformPoint(right.position);
            if (!IsFinite(rootPosition) || !IsFinite(rootRotation) || !IsFinite(leftPosition) || !IsFinite(rightPosition))
                throw new InvalidOperationException($"Clip '{clip.SourceClipId}' contains a non-finite sampled Rig pose.");
            return new PoseSnapshot(rootPosition, rootRotation, bones, leftPosition, rightPosition);
        }

        void ResolveForward(int segmentIndex, float sampleTime, float offset, out Vector3 relativePosition, out Quaternion relativeRotation)
        {
            if (!float.IsFinite(offset) || offset < 0f)
                throw new ArgumentOutOfRangeException(nameof(offset));
            relativePosition = Vector3.zero;
            relativeRotation = Quaternion.identity;
            float remaining = offset;
            int currentSegmentIndex = segmentIndex;
            float currentTime = sampleTime;
            int transitions = 0;
            while (remaining > 0f)
            {
                MotionMatchingSegmentBuildInput segment = m_Request.GetSegment(currentSegmentIndex);
                MotionMatchingResolvedClipBuildInput clip = m_Request.GetClip(segment.ClipBindingIndex);
                float available = Mathf.Max(0f, segment.EndTime - currentTime);
                float step = Mathf.Min(remaining, available);
                PoseSnapshot from = SamplePose(clip, currentTime);
                PoseSnapshot to = SamplePose(clip, currentTime + step);
                Vector3 localDelta = Quaternion.Inverse(from.RootRotation) * (to.RootPosition - from.RootPosition);
                relativePosition += relativeRotation * localDelta;
                relativeRotation = Canonical(relativeRotation * Quaternion.Inverse(from.RootRotation) * to.RootRotation);
                remaining -= step;
                currentTime += step;
                if (remaining <= 0f)
                    break;
                if (++transitions > m_Request.SegmentCount * 4 + 8)
                    throw new InvalidOperationException($"Motion Matching continuation traversal from Segment '{segment.SegmentId}' did not terminate.");
                if (segment.LoopMode == MotionMatchingSegmentLoopMode.Loop)
                {
                    currentTime = segment.StartTime;
                    continue;
                }
                if (segment.Terminal)
                    break;
                if (!m_SegmentIndices.TryGetValue(segment.ContinuationTarget, out currentSegmentIndex))
                    throw new InvalidOperationException($"Segment '{segment.SegmentId}' continuation target is missing.");
                currentTime = m_Request.GetSegment(currentSegmentIndex).StartTime;
            }
        }

        static Quaternion Canonical(Quaternion value)
        {
            value = value.normalized;
            if (value.w < 0f)
                value = new Quaternion(-value.x, -value.y, -value.z, -value.w);
            return value;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(MotionMatchingDatabaseSampler));
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_Instance)
                Object.DestroyImmediate(m_Instance);
            if (m_OwnsAnimationMode && AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w) &&
            value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0f;
    }
}
