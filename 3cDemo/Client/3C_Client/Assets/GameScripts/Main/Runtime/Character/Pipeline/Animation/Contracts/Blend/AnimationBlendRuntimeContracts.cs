using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class AnimationBlendStackPolicyPayload
    {
        [SerializeField] int m_MaxActiveSourceEntries;
        [SerializeField] AnimationStoredPosePolicy m_StoredPosePolicy;
        [SerializeField] float m_MaxBlendInTimeToReplaceNewest;
        [SerializeField] float m_DepthBlendTimeMultiplier;

        public int MaxActiveSourceEntries => m_MaxActiveSourceEntries;
        public AnimationStoredPosePolicy StoredPosePolicy => m_StoredPosePolicy;
        public float MaxBlendInTimeToReplaceNewest => m_MaxBlendInTimeToReplaceNewest;
        public float DepthBlendTimeMultiplier => m_DepthBlendTimeMultiplier;

        public AnimationBlendStackPolicyPayload(CharacterAnimationBlendStackPolicy source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            source.RequireValid();
            m_MaxActiveSourceEntries = source.MaxActiveSourceEntries;
            m_StoredPosePolicy = source.StoredPosePolicy;
            m_MaxBlendInTimeToReplaceNewest = source.MaxBlendInTimeToReplaceNewest;
            m_DepthBlendTimeMultiplier = source.DepthBlendTimeMultiplier;
        }

        public void RequireValid()
        {
            if (MaxActiveSourceEntries < 2 ||
                !Enum.IsDefined(typeof(AnimationStoredPosePolicy), StoredPosePolicy) ||
                !float.IsFinite(MaxBlendInTimeToReplaceNewest) || MaxBlendInTimeToReplaceNewest < 0f ||
                !float.IsFinite(DepthBlendTimeMultiplier) || DepthBlendTimeMultiplier <= 0f)
            {
                throw new InvalidOperationException("Compiled Animation Blend Stack policy is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class AnimationBlendProfilePayload
    {
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_RigId = string.Empty;
        [SerializeField] string m_RigRevision = string.Empty;
        [SerializeField] float m_GlobalDurationMultiplier;
        [SerializeField] float[] m_DenseDurationMultipliers = Array.Empty<float>();

        public string ProfileId => m_ProfileId ?? string.Empty;
        public string RigId => m_RigId ?? string.Empty;
        public string RigRevision => m_RigRevision ?? string.Empty;
        public float GlobalDurationMultiplier => m_GlobalDurationMultiplier;
        public IReadOnlyList<float> DenseDurationMultipliers => m_DenseDurationMultipliers ?? Array.Empty<float>();

        public AnimationBlendProfilePayload(CharacterAnimationBlendProfile source, CharacterAnimationRigDefinition rig)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (!rig)
                throw new ArgumentNullException(nameof(rig));
            m_ProfileId = source.ProfileId;
            m_RigId = rig.RigId;
            m_RigRevision = rig.Revision;
            m_GlobalDurationMultiplier = source.GlobalDurationMultiplier;
            m_DenseDurationMultipliers = source.BuildDense(rig);
            RequireValid(rig.PoseBoneCount, rig.RigId, rig.Revision);
        }

        public void RequireValid(int boneCount, string rigId, string rigRevision)
        {
            if (boneCount <= 0 || string.IsNullOrEmpty(ProfileId) ||
                !string.Equals(RigId, rigId, StringComparison.Ordinal) ||
                !string.Equals(RigRevision, rigRevision, StringComparison.Ordinal) ||
                !float.IsFinite(GlobalDurationMultiplier) || GlobalDurationMultiplier <= 0f ||
                DenseDurationMultipliers.Count != boneCount)
            {
                throw new InvalidOperationException("Compiled Animation Blend Profile identity or Bone count is invalid.");
            }
            for (int i = 0; i < DenseDurationMultipliers.Count; i++)
            {
                if (!float.IsFinite(DenseDurationMultipliers[i]) || DenseDurationMultipliers[i] <= 0f)
                    throw new InvalidOperationException($"Compiled Animation Blend Profile multiplier #{i} is invalid.");
            }
        }
    }

    public static class AnimationBlendCanonicalPayload
    {
        public static string CurveKey(AnimationBlendCurvePayload curve)
        {
            if (curve == null)
                throw new ArgumentNullException(nameof(curve));
            curve.RequireValid();
            var builder = new StringBuilder();
            builder.Append("curve/v1|").Append(curve.Segments.Count.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < curve.Segments.Count; i++)
            {
                AnimationBlendCurveSegment segment = curve.Segments[i];
                AppendFloat(builder, segment.StartTime);
                AppendFloat(builder, segment.EndTime);
                AppendFloat(builder, segment.A);
                AppendFloat(builder, segment.B);
                AppendFloat(builder, segment.C);
                AppendFloat(builder, segment.D);
            }
            return builder.ToString();
        }

        public static string ProfileKey(AnimationBlendProfilePayload profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            var builder = new StringBuilder("profile/v2|");
            AppendString(builder, profile.ProfileId);
            AppendString(builder, profile.RigId);
            AppendString(builder, profile.RigRevision);
            AppendFloat(builder, profile.GlobalDurationMultiplier);
            builder.Append('|').Append(profile.DenseDurationMultipliers.Count.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < profile.DenseDurationMultipliers.Count; i++)
                AppendFloat(builder, profile.DenseDurationMultipliers[i]);
            return builder.ToString();
        }

        public static bool CurveEquals(AnimationBlendCurvePayload left, AnimationBlendCurvePayload right) =>
            left != null && right != null && string.Equals(CurveKey(left), CurveKey(right), StringComparison.Ordinal);

        public static bool ProfileEquals(AnimationBlendProfilePayload left, AnimationBlendProfilePayload right) =>
            left != null && right != null && string.Equals(ProfileKey(left), ProfileKey(right), StringComparison.Ordinal);

        static void AppendString(StringBuilder builder, string value)
        {
            string normalized = value ?? string.Empty;
            builder.Append('|').Append(normalized.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(normalized);
        }

        static void AppendFloat(StringBuilder builder, float value)
        {
            if (!float.IsFinite(value))
                throw new InvalidOperationException("Animation Blend canonical payload contains a non-finite scalar.");
            float normalized = value == 0f ? 0f : value;
            int bits = BitConverter.SingleToInt32Bits(normalized);
            builder.Append('|').Append(unchecked((uint)bits).ToString("x8", CultureInfo.InvariantCulture));
        }
    }

    [Serializable]
    public sealed class AnimationBlendCurveCatalogEntry
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_CanonicalHash = string.Empty;
        [SerializeField] AnimationBlendCurvePayload m_Curve;

        public AnimationBlendCurveCatalogEntry(int index, AnimationBlendCurvePayload curve)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_Index = index;
            m_Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            m_CanonicalHash = StableHash.Compute(AnimationBlendCanonicalPayload.CurveKey(curve)).ToString();
        }

        public int Index => m_Index;
        public string CanonicalHash => m_CanonicalHash ?? string.Empty;
        public AnimationBlendCurvePayload Curve => m_Curve;
    }

    [Serializable]
    public sealed class AnimationBlendCurveCatalogPayload
    {
        [SerializeField] AnimationBlendCurveCatalogEntry[] m_Entries = Array.Empty<AnimationBlendCurveCatalogEntry>();

        public AnimationBlendCurveCatalogPayload(AnimationBlendCurveCatalogEntry[] entries)
        {
            m_Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            RequireValid();
        }

        public IReadOnlyList<AnimationBlendCurveCatalogEntry> Entries => m_Entries ?? Array.Empty<AnimationBlendCurveCatalogEntry>();

        public AnimationBlendCurvePayload Require(int index)
        {
            if ((uint)index >= (uint)Entries.Count || Entries[index] == null || Entries[index].Index != index)
                throw new InvalidOperationException($"Animation Blend Curve catalog index '{index}' is invalid.");
            return Entries[index].Curve;
        }

        public void RequireValid()
        {
            if (Entries.Count == 0)
                throw new InvalidOperationException("Animation Blend Curve catalog is empty.");
            var hashes = new Dictionary<string, AnimationBlendCurveCatalogEntry>(StringComparer.Ordinal);
            for (int i = 0; i < Entries.Count; i++)
            {
                AnimationBlendCurveCatalogEntry entry = Entries[i];
                if (entry == null || entry.Index != i || entry.Curve == null)
                    throw new InvalidOperationException($"Animation Blend Curve catalog entry #{i} is invalid.");
                entry.Curve.RequireValid();
                string expectedHash = StableHash.Compute(AnimationBlendCanonicalPayload.CurveKey(entry.Curve)).ToString();
                if (!string.Equals(entry.CanonicalHash, expectedHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Animation Blend Curve catalog entry #{i} hash is inconsistent.");
                if (hashes.TryGetValue(entry.CanonicalHash, out AnimationBlendCurveCatalogEntry existing))
                {
                    if (!AnimationBlendCanonicalPayload.CurveEquals(existing.Curve, entry.Curve))
                        throw new InvalidOperationException($"Animation Blend Curve canonical hash collision occurs at entry #{i}.");
                    throw new InvalidOperationException($"Animation Blend Curve catalog duplicates entry #{i}.");
                }
                hashes.Add(entry.CanonicalHash, entry);
            }
        }
    }

    [Serializable]
    public sealed class AnimationBlendProfileCatalogEntry
    {
        [SerializeField] int m_Index;
        [SerializeField] string m_CanonicalHash = string.Empty;
        [SerializeField] AnimationBlendProfilePayload m_Profile;

        public AnimationBlendProfileCatalogEntry(int index, AnimationBlendProfilePayload profile)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            m_Index = index;
            m_Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            m_CanonicalHash = StableHash.Compute(AnimationBlendCanonicalPayload.ProfileKey(profile)).ToString();
        }

        public int Index => m_Index;
        public string CanonicalHash => m_CanonicalHash ?? string.Empty;
        public AnimationBlendProfilePayload Profile => m_Profile;
    }

    [Serializable]
    public sealed class AnimationBlendProfileCatalogPayload
    {
        [SerializeField] AnimationBlendProfileCatalogEntry[] m_Entries = Array.Empty<AnimationBlendProfileCatalogEntry>();

        public AnimationBlendProfileCatalogPayload(AnimationBlendProfileCatalogEntry[] entries)
        {
            m_Entries = entries ?? throw new ArgumentNullException(nameof(entries));
            if (Entries.Count == 0)
                throw new InvalidOperationException("Animation Blend Profile catalog is empty.");
        }

        public IReadOnlyList<AnimationBlendProfileCatalogEntry> Entries => m_Entries ?? Array.Empty<AnimationBlendProfileCatalogEntry>();

        public AnimationBlendProfilePayload Require(int index)
        {
            if ((uint)index >= (uint)Entries.Count || Entries[index] == null || Entries[index].Index != index)
                throw new InvalidOperationException($"Animation Blend Profile catalog index '{index}' is invalid.");
            return Entries[index].Profile;
        }

        public void RequireValid(int boneCount, string rigId, string rigRevision)
        {
            if (Entries.Count == 0)
                throw new InvalidOperationException("Animation Blend Profile catalog is empty.");
            var identities = new HashSet<string>(StringComparer.Ordinal);
            var hashes = new Dictionary<string, AnimationBlendProfileCatalogEntry>(StringComparer.Ordinal);
            for (int i = 0; i < Entries.Count; i++)
            {
                AnimationBlendProfileCatalogEntry entry = Entries[i];
                if (entry == null || entry.Index != i || entry.Profile == null)
                    throw new InvalidOperationException($"Animation Blend Profile catalog entry #{i} is invalid.");
                entry.Profile.RequireValid(boneCount, rigId, rigRevision);
                string expectedHash = StableHash.Compute(AnimationBlendCanonicalPayload.ProfileKey(entry.Profile)).ToString();
                if (!string.Equals(entry.CanonicalHash, expectedHash, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Animation Blend Profile catalog entry #{i} hash is inconsistent.");
                if (!identities.Add(entry.Profile.ProfileId))
                    throw new InvalidOperationException($"Animation Blend Profile catalog duplicates Profile identity '{entry.Profile.ProfileId}'.");
                if (hashes.TryGetValue(entry.CanonicalHash, out AnimationBlendProfileCatalogEntry existing))
                {
                    if (!AnimationBlendCanonicalPayload.ProfileEquals(existing.Profile, entry.Profile))
                        throw new InvalidOperationException($"Animation Blend Profile canonical hash collision occurs at entry #{i}.");
                    throw new InvalidOperationException($"Animation Blend Profile catalog duplicates entry #{i}.");
                }
                hashes.Add(entry.CanonicalHash, entry);
            }
        }
    }

    public enum AnimationBlendTransitionEndpointKind : byte
    {
        SourceOwner = 1,
        SourcePose = 2,
        NoPose = 3
    }

    public readonly struct AnimationBlendTransitionIdentity : IEquatable<AnimationBlendTransitionIdentity>
    {
        public AnimationBlendTransitionIdentity(
            PoseNodeId nodeId,
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind sourceEndpointKind,
            int targetOwnerIndex,
            AnimationBlendTransitionEndpointKind targetEndpointKind)
        {
            if (!nodeId.IsValid ||
                !IsValidEndpoint(sourceOwnerIndex, sourceEndpointKind) ||
                !IsValidEndpoint(targetOwnerIndex, targetEndpointKind))
            {
                throw new ArgumentException("Animation Blend transition identity is invalid.");
            }
            NodeId = nodeId;
            SourceOwnerIndex = sourceOwnerIndex;
            SourceEndpointKind = sourceEndpointKind;
            TargetOwnerIndex = targetOwnerIndex;
            TargetEndpointKind = targetEndpointKind;
        }

        public AnimationBlendTransitionIdentity(PoseNodeId nodeId, AnimationBlendTransitionPayload payload)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            this = new AnimationBlendTransitionIdentity(
                nodeId,
                payload.SourceOwnerIndex,
                payload.SourceEndpointKind,
                payload.TargetOwnerIndex,
                payload.TargetEndpointKind);
        }

        public PoseNodeId NodeId { get; }
        public int SourceOwnerIndex { get; }
        public AnimationBlendTransitionEndpointKind SourceEndpointKind { get; }
        public int TargetOwnerIndex { get; }
        public AnimationBlendTransitionEndpointKind TargetEndpointKind { get; }
        public bool IsValid => NodeId.IsValid &&
                               IsValidEndpoint(SourceOwnerIndex, SourceEndpointKind) &&
                               IsValidEndpoint(TargetOwnerIndex, TargetEndpointKind);

        public bool Equals(AnimationBlendTransitionIdentity other) =>
            NodeId.Equals(other.NodeId) &&
            SourceOwnerIndex == other.SourceOwnerIndex &&
            SourceEndpointKind == other.SourceEndpointKind &&
            TargetOwnerIndex == other.TargetOwnerIndex &&
            TargetEndpointKind == other.TargetEndpointKind;

        public override bool Equals(object obj) =>
            obj is AnimationBlendTransitionIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = NodeId.GetHashCode();
                hash = hash * 397 ^ SourceOwnerIndex;
                hash = hash * 397 ^ (int)SourceEndpointKind;
                hash = hash * 397 ^ TargetOwnerIndex;
                return hash * 397 ^ (int)TargetEndpointKind;
            }
        }

        public override string ToString() =>
            $"{NodeId}:{Describe(SourceOwnerIndex, SourceEndpointKind)}->{Describe(TargetOwnerIndex, TargetEndpointKind)}";

        public static bool operator ==(AnimationBlendTransitionIdentity left, AnimationBlendTransitionIdentity right) =>
            left.Equals(right);

        public static bool operator !=(AnimationBlendTransitionIdentity left, AnimationBlendTransitionIdentity right) =>
            !left.Equals(right);

        static bool IsValidEndpoint(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind) =>
            Enum.IsDefined(
                typeof(AnimationBlendTransitionEndpointKind),
                endpointKind) &&
            (endpointKind == AnimationBlendTransitionEndpointKind.SourceOwner
                ? sourceOwnerIndex >= 0
                : sourceOwnerIndex == -1);

        static string Describe(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind endpointKind) =>
            endpointKind == AnimationBlendTransitionEndpointKind.SourceOwner
                ? sourceOwnerIndex.ToString()
                : endpointKind.ToString();
    }

    [Serializable]
    public sealed class AnimationBlendTransitionPayload
    {
        [SerializeField] int m_SourceOwnerIndex = -1;
        [SerializeField] AnimationBlendTransitionEndpointKind m_SourceEndpointKind;
        [SerializeField] string m_SourceOwnerIdentity = string.Empty;
        [SerializeField] int m_TargetOwnerIndex = -1;
        [SerializeField] AnimationBlendTransitionEndpointKind m_TargetEndpointKind;
        [SerializeField] string m_TargetOwnerIdentity = string.Empty;
        [SerializeField] AnimationTransitionBlendLogic m_BlendLogic;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] int m_CurveIndex = -1;
        [SerializeField] int m_BlendProfileIndex = -1;

        public int SourceOwnerIndex => m_SourceOwnerIndex;
        public AnimationBlendTransitionEndpointKind SourceEndpointKind => m_SourceEndpointKind;
        public string SourceOwnerIdentity => m_SourceOwnerIdentity ?? string.Empty;
        public int TargetOwnerIndex => m_TargetOwnerIndex;
        public AnimationBlendTransitionEndpointKind TargetEndpointKind => m_TargetEndpointKind;
        public string TargetOwnerIdentity => m_TargetOwnerIdentity ?? string.Empty;
        public AnimationTransitionBlendLogic BlendLogic => m_BlendLogic;
        public float DurationSeconds => m_DurationSeconds;
        public int CurveIndex => m_CurveIndex;
        public int BlendProfileIndex => m_BlendProfileIndex;
        public AnimationBlendTransitionIdentity GetIdentity(PoseNodeId nodeId) =>
            new AnimationBlendTransitionIdentity(nodeId, this);

        public AnimationBlendTransitionPayload(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind sourceEndpointKind,
            string sourceOwnerIdentity,
            int targetOwnerIndex,
            AnimationBlendTransitionEndpointKind targetEndpointKind,
            string targetOwnerIdentity,
            AnimationTransitionBlendLogic blendLogic,
            float durationSeconds,
            int curveIndex,
            int blendProfileIndex)
        {
            m_SourceOwnerIndex = sourceOwnerIndex;
            m_SourceEndpointKind = sourceEndpointKind;
            m_SourceOwnerIdentity = sourceOwnerIdentity ?? string.Empty;
            m_TargetOwnerIndex = targetOwnerIndex;
            m_TargetEndpointKind = targetEndpointKind;
            m_TargetOwnerIdentity = targetOwnerIdentity ?? string.Empty;
            m_BlendLogic = blendLogic;
            m_DurationSeconds = durationSeconds;
            m_CurveIndex = curveIndex;
            m_BlendProfileIndex = blendProfileIndex;
        }

        public void RequireValid(int curveCount, int blendProfileCount)
        {
            if (!Enum.IsDefined(typeof(AnimationBlendTransitionEndpointKind), SourceEndpointKind) ||
                !Enum.IsDefined(typeof(AnimationBlendTransitionEndpointKind), TargetEndpointKind) ||
                (SourceEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner) !=
                (SourceOwnerIndex >= 0 && !string.IsNullOrEmpty(SourceOwnerIdentity)) ||
                (TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner) !=
                (TargetOwnerIndex >= 0 && !string.IsNullOrEmpty(TargetOwnerIdentity)) ||
                SourceEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner &&
                (SourceOwnerIndex != -1 || !string.IsNullOrEmpty(SourceOwnerIdentity)) ||
                TargetEndpointKind != AnimationBlendTransitionEndpointKind.SourceOwner &&
                (TargetOwnerIndex != -1 || !string.IsNullOrEmpty(TargetOwnerIdentity)) ||
                !Enum.IsDefined(typeof(AnimationTransitionBlendLogic), BlendLogic) ||
                !float.IsFinite(DurationSeconds) || DurationSeconds < 0f ||
                BlendLogic == AnimationTransitionBlendLogic.Inertialization && DurationSeconds <= 0f ||
                (uint)CurveIndex >= (uint)curveCount || (uint)BlendProfileIndex >= (uint)blendProfileCount)
            {
                throw new InvalidOperationException("Compiled Animation Blend transition is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class AnimationBlendNodePayload
    {
        [SerializeField] string m_NodeId = string.Empty;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_PolicyRevision = string.Empty;
        [SerializeField] AnimationBlendStackPolicyPayload m_StackPolicy;
        [SerializeField] AnimationBlendTransitionPayload[] m_Transitions = Array.Empty<AnimationBlendTransitionPayload>();
        [SerializeField] CompiledTransitionRoutingPlanPayload m_RoutingPlan;

        public PoseNodeId NodeId => string.IsNullOrWhiteSpace(m_NodeId) ? default : new PoseNodeId(m_NodeId);
        public string PolicyId => m_PolicyId ?? string.Empty;
        public string PolicyRevision => m_PolicyRevision ?? string.Empty;
        public AnimationBlendStackPolicyPayload StackPolicy => m_StackPolicy;
        public IReadOnlyList<AnimationBlendTransitionPayload> Transitions => m_Transitions ?? Array.Empty<AnimationBlendTransitionPayload>();
        public bool HasRoutingPlan => m_RoutingPlan != null;
        public string RoutingPlanId =>
            m_RoutingPlan?.PlanId ?? string.Empty;
        public string RoutingDefinitionRevision =>
            m_RoutingPlan?.DefinitionRevision ?? string.Empty;

        public AnimationBlendNodePayload(
            PoseNodeId nodeId,
            string policyId,
            string policyRevision,
            AnimationBlendStackPolicyPayload stackPolicy,
            AnimationBlendTransitionPayload[] transitions,
            CompiledTransitionRoutingPlanPayload routingPlan)
        {
            m_NodeId = nodeId.IsValid ? nodeId.Value : throw new ArgumentException("Blend Stack Node identity is invalid.", nameof(nodeId));
            m_PolicyId = PoseIdentity.Require(policyId, nameof(policyId));
            m_PolicyRevision = PoseIdentity.Require(policyRevision, nameof(policyRevision));
            m_StackPolicy = stackPolicy ?? throw new ArgumentNullException(nameof(stackPolicy));
            m_Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            m_RoutingPlan = routingPlan;
        }

        public CompiledTransitionRoutingPlan LoadRoutingPlan() =>
            m_RoutingPlan?.Load() ??
            throw new InvalidOperationException(
                $"Animation Blend Stack '{NodeId}' has no compiled Routing Plan.");

        public AnimationBlendTransitionPayload RequireTransition(
            int sourceOwnerIndex,
            AnimationBlendTransitionEndpointKind sourceEndpointKind,
            int targetOwnerIndex,
            AnimationBlendTransitionEndpointKind targetEndpointKind)
        {
            var identity = new AnimationBlendTransitionIdentity(
                NodeId,
                sourceOwnerIndex,
                sourceEndpointKind,
                targetOwnerIndex,
                targetEndpointKind);
            AnimationBlendTransitionPayload result = null;
            for (int i = 0; i < Transitions.Count; i++)
            {
                AnimationBlendTransitionPayload candidate = Transitions[i];
                if (candidate == null || candidate.GetIdentity(NodeId) != identity)
                {
                    continue;
                }
                if (result != null)
                    throw new InvalidOperationException($"Compiled Animation Blend Stack '{NodeId}' duplicates an exact transition.");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException($"Compiled Animation Blend Stack '{NodeId}' has no exact transition.");
        }
    }

    public readonly struct AnimationReadOnlyBuffer<T>
    {
        readonly T[] m_Buffer;
        readonly int m_Offset;
        readonly int m_Count;
        readonly IAnimationReadOnlyBufferLease m_Lease;
        readonly ulong m_LeaseIdentity;

        internal AnimationReadOnlyBuffer(T[] buffer, int offset, int count)
            : this(buffer, offset, count, null, 0)
        {
        }

        internal AnimationReadOnlyBuffer(
            T[] buffer,
            int offset,
            int count,
            IAnimationReadOnlyBufferLease lease,
            ulong leaseIdentity)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if ((uint)offset > (uint)buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if ((uint)count > (uint)(buffer.Length - offset))
                throw new ArgumentOutOfRangeException(nameof(count));
            if ((lease == null) != (leaseIdentity == 0))
                throw new ArgumentException("Animation read-only buffer lease identity is invalid.");
            lease?.RequireValid(leaseIdentity);
            m_Buffer = buffer;
            m_Offset = offset;
            m_Count = count;
            m_Lease = lease;
            m_LeaseIdentity = leaseIdentity;
        }

        public int Count
        {
            get
            {
                RequireLease();
                return m_Count;
            }
        }

        public T this[int index]
        {
            get
            {
                RequireLease();
                if ((uint)index >= (uint)m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return m_Buffer[m_Offset + index];
            }
        }

        void RequireLease()
        {
            m_Lease?.RequireValid(m_LeaseIdentity);
        }
    }

    public readonly struct AnimationLocalBonePose
    {
        public AnimationLocalBonePose(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (!IsFinite(position) || !IsFinite(rotation) || !IsFinite(scale) || Quaternion.Dot(rotation, rotation) <= 0f)
                throw new ArgumentException("Animation local Bone pose is invalid.");
            Position = position;
            Rotation = rotation.normalized;
            Scale = scale;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }
        public bool IsValid => IsFinite(Position) && IsFinite(Rotation) && IsFinite(Scale) && Quaternion.Dot(Rotation, Rotation) > 0f;

        static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        static bool IsFinite(Quaternion value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);
    }

    internal enum AnimationSourcePoseCaptureFailure : byte
    {
        None = 0,
        PhysicalPoseInvalid = 1,
        VirtualBoneDerivationInvalid = 2,
        PreviousPoseInvalid = 3
    }

    internal readonly struct AnimationPoseSourceCaptureBinding
    {
        internal AnimationPoseSourceCaptureBinding(
            AnimationPoseSourceId sourceId,
            int sourceIndex,
            ulong completionIdentity,
            NativeSlice<AnimationLocalBonePose> currentPose,
            NativeSlice<AnimationLocalBonePose> previousPose,
            NativeSlice<AnimationBlendBoneVelocity> velocity,
            NativeArray<byte> previousAvailable,
            NativeArray<byte> hasPrevious,
            NativeArray<ulong> completedAt,
            NativeArray<AnimationSourcePoseCaptureFailure> failure,
            float presentationDeltaSeconds)
        {
            if (!sourceId.IsValid || sourceIndex < 0 || completionIdentity == 0 ||
                currentPose.Length == 0 ||
                previousPose.Length != currentPose.Length ||
                velocity.Length != currentPose.Length ||
                !previousAvailable.IsCreated || !hasPrevious.IsCreated || !completedAt.IsCreated || !failure.IsCreated || hasPrevious.Length == 0 ||
                previousAvailable.Length != hasPrevious.Length || hasPrevious.Length != completedAt.Length || hasPrevious.Length != failure.Length ||
                sourceIndex >= hasPrevious.Length ||
                previousAvailable[sourceIndex] > 1 || hasPrevious[sourceIndex] > 1 ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
            {
                throw new ArgumentException("Animation pose source capture binding is invalid.");
            }
            SourceId = sourceId;
            SourceIndex = sourceIndex;
            CompletionIdentity = completionIdentity;
            CurrentPose = currentPose;
            PreviousPose = previousPose;
            Velocity = velocity;
            PreviousAvailable = previousAvailable;
            HasPrevious = hasPrevious;
            CompletedAt = completedAt;
            Failure = failure;
            PresentationDeltaSeconds = presentationDeltaSeconds;
        }

        internal AnimationPoseSourceId SourceId { get; }
        internal int SourceIndex { get; }
        internal ulong CompletionIdentity { get; }
        internal NativeSlice<AnimationLocalBonePose> CurrentPose { get; }
        internal NativeSlice<AnimationLocalBonePose> PreviousPose { get; }
        internal NativeSlice<AnimationBlendBoneVelocity> Velocity { get; }
        internal NativeArray<byte> PreviousAvailable { get; }
        internal NativeArray<byte> HasPrevious { get; }
        internal NativeArray<ulong> CompletedAt { get; }
        internal NativeArray<AnimationSourcePoseCaptureFailure> Failure { get; }
        internal float PresentationDeltaSeconds { get; }
    }

    public enum AnimationPoseAvailability : byte
    {
        Pose = 1,
        NoPose = 2,
        Invalid = 3
    }

    public enum AnimationPoseContributionKind : byte
    {
        Live = 1,
        Stored = 2
    }

    public readonly struct AnimationPoseSourceContribution
    {
        public AnimationPoseSourceContribution(
            PoseNodeId nodeId,
            AnimationPoseContributionKind kind,
            AnimationPoseSourceId sourceId,
            int sourceOwnerIndex,
            ulong contributionContinuityIdentity,
            float weight,
            float leftFootWeight,
            float rightFootWeight)
        {
            if (!nodeId.IsValid ||
                (int)kind < (int)AnimationPoseContributionKind.Live ||
                (int)kind > (int)AnimationPoseContributionKind.Stored ||
                kind == AnimationPoseContributionKind.Live && (!sourceId.IsValid || sourceOwnerIndex < 0) ||
                kind != AnimationPoseContributionKind.Live && (sourceId.IsValid || sourceOwnerIndex != -1) ||
                contributionContinuityIdentity == 0 ||
                !IsNormalized(weight) || !IsNormalized(leftFootWeight) || !IsNormalized(rightFootWeight))
            {
                throw new ArgumentException("Animation pose source contribution is invalid.");
            }
            NodeId = nodeId;
            Kind = kind;
            SourceId = sourceId;
            SourceOwnerIndex = sourceOwnerIndex;
            ContributionContinuityIdentity = contributionContinuityIdentity;
            Weight = weight;
            LeftFootWeight = leftFootWeight;
            RightFootWeight = rightFootWeight;
        }

        public PoseNodeId NodeId { get; }
        public AnimationPoseContributionKind Kind { get; }
        public AnimationPoseSourceId SourceId { get; }
        public int SourceOwnerIndex { get; }
        public ulong ContributionContinuityIdentity { get; }
        public float Weight { get; }
        public float LeftFootWeight { get; }
        public float RightFootWeight { get; }

        static bool IsNormalized(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
    }

    public readonly struct AnimationPoseValue
    {
        readonly AnimationReadOnlyBuffer<float> m_DenseContributionWeights;

        internal AnimationPoseValue(
            PoseNodeId nodeId,
            ulong completionIdentity,
            AnimationPoseAvailability availability,
            float outputWeight,
            AnimationReadOnlyBuffer<AnimationLocalBonePose> denseLocalPose,
            AnimationReadOnlyBuffer<float> poseParameters,
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution> contributions,
            AnimationReadOnlyBuffer<float> denseContributionWeights,
            AnimationFootFeatureSample leftFootFeatures,
            AnimationFootFeatureSample rightFootFeatures,
            bool hasFootFeatures,
            ulong continuityIdentity)
        {
            if (!nodeId.IsValid || completionIdentity == 0 || continuityIdentity == 0 ||
                (int)availability < (int)AnimationPoseAvailability.Pose ||
                (int)availability > (int)AnimationPoseAvailability.Invalid ||
                !float.IsFinite(outputWeight) || outputWeight < 0f || outputWeight > 1f ||
                denseContributionWeights.Count != contributions.Count * denseLocalPose.Count ||
                availability == AnimationPoseAvailability.Pose && denseLocalPose.Count == 0 ||
                availability == AnimationPoseAvailability.NoPose && (denseLocalPose.Count != 0 || outputWeight != 0f) ||
                hasFootFeatures && (!leftFootFeatures.IsValid || !rightFootFeatures.IsValid))
            {
                throw new ArgumentException("Animation Pose Value is invalid.");
            }
            for (int i = 0; i < denseLocalPose.Count; i++)
            {
                if (!denseLocalPose[i].IsValid)
                    throw new ArgumentException($"Animation Pose Value Bone pose #{i} is invalid.");
            }
            for (int i = 0; i < poseParameters.Count; i++)
            {
                if (!float.IsFinite(poseParameters[i]))
                    throw new ArgumentException($"Animation Pose Value parameter #{i} is invalid.");
            }
            for (int i = 0; i < denseContributionWeights.Count; i++)
            {
                if (!float.IsFinite(denseContributionWeights[i]) || denseContributionWeights[i] < 0f || denseContributionWeights[i] > 1f)
                    throw new ArgumentException($"Animation Pose Value contribution weight #{i} is invalid.");
            }
            NodeId = nodeId;
            CompletionIdentity = completionIdentity;
            Availability = availability;
            OutputWeight = outputWeight;
            DenseLocalPose = denseLocalPose;
            PoseParameters = poseParameters;
            Contributions = contributions;
            m_DenseContributionWeights = denseContributionWeights;
            LeftFootFeatures = leftFootFeatures;
            RightFootFeatures = rightFootFeatures;
            HasFootFeatures = hasFootFeatures;
            ContinuityIdentity = continuityIdentity;
        }

        public PoseNodeId NodeId { get; }
        public ulong CompletionIdentity { get; }
        public AnimationPoseAvailability Availability { get; }
        public float OutputWeight { get; }
        public AnimationReadOnlyBuffer<AnimationLocalBonePose> DenseLocalPose { get; }
        public AnimationReadOnlyBuffer<float> PoseParameters { get; }
        public AnimationReadOnlyBuffer<AnimationPoseSourceContribution> Contributions { get; }
        public AnimationFootFeatureSample LeftFootFeatures { get; }
        public AnimationFootFeatureSample RightFootFeatures { get; }
        public bool HasFootFeatures { get; }
        public ulong ContinuityIdentity { get; }

        public float GetContributionBoneWeight(int contributionIndex, int boneIndex)
        {
            if ((uint)contributionIndex >= (uint)Contributions.Count || (uint)boneIndex >= (uint)DenseLocalPose.Count)
                throw new ArgumentOutOfRangeException();
            return m_DenseContributionWeights[contributionIndex * DenseLocalPose.Count + boneIndex];
        }

        public float GetBoneOutputWeight(int boneIndex)
        {
            if ((uint)boneIndex >= (uint)DenseLocalPose.Count)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            float weight = 0f;
            for (int i = 0; i < Contributions.Count; i++)
                weight += m_DenseContributionWeights[i * DenseLocalPose.Count + boneIndex];
            return Mathf.Clamp01(weight);
        }
    }
}
