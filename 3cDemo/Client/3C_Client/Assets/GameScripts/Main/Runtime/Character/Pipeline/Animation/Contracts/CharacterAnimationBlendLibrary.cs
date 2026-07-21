using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationBlendTechnique : byte
    {
        CrossFade = 1,
        Inertial = 2
    }

    [Serializable]
    public sealed class CharacterAnimationBlendStackPolicy
    {
        [SerializeField, Min(2)] int m_MaxActiveSourceEntries = 4;
        [SerializeField, Min(0f)] float m_MaxBlendInTimeToReplaceNewest = 0.05f;
        [SerializeField] float m_DepthBlendTimeMultiplier = 1f;

        public int MaxActiveSourceEntries => m_MaxActiveSourceEntries;
        public float MaxBlendInTimeToReplaceNewest => m_MaxBlendInTimeToReplaceNewest;
        public float DepthBlendTimeMultiplier => m_DepthBlendTimeMultiplier;

        public CharacterAnimationBlendStackPolicy() { }

        public CharacterAnimationBlendStackPolicy(
            int maxActiveSourceEntries,
            float maxBlendInTimeToReplaceNewest,
            float depthBlendTimeMultiplier)
        {
            m_MaxActiveSourceEntries = maxActiveSourceEntries;
            m_MaxBlendInTimeToReplaceNewest = maxBlendInTimeToReplaceNewest;
            m_DepthBlendTimeMultiplier = depthBlendTimeMultiplier;
            RequireValid();
        }

        public void RequireValid()
        {
            if (MaxActiveSourceEntries < 2 ||
                !float.IsFinite(MaxBlendInTimeToReplaceNewest) || MaxBlendInTimeToReplaceNewest < 0f ||
                !float.IsFinite(DepthBlendTimeMultiplier) || DepthBlendTimeMultiplier <= 0f)
            {
                throw new InvalidOperationException("Animation Blend Stack policy is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendTransitionRule
    {
        [SerializeField] AnimationBlendTechnique m_Technique = AnimationBlendTechnique.CrossFade;
        [SerializeField, Min(0f)] float m_DurationSeconds = 0.2f;
        [SerializeField] CharacterAnimationBlendCurve m_Curve = new CharacterAnimationBlendCurve();
        [SerializeField] CharacterAnimationBlendProfile m_BlendProfile;

        public AnimationBlendTechnique Technique => m_Technique;
        public float DurationSeconds => m_DurationSeconds;
        public CharacterAnimationBlendCurve Curve => m_Curve;
        public CharacterAnimationBlendProfile BlendProfile => m_BlendProfile;

        public CharacterAnimationBlendTransitionRule() { }

        public CharacterAnimationBlendTransitionRule(
            AnimationBlendTechnique technique,
            float durationSeconds,
            CharacterAnimationBlendCurve curve,
            CharacterAnimationBlendProfile blendProfile)
        {
            m_Technique = technique;
            m_DurationSeconds = durationSeconds;
            m_Curve = curve ?? throw new ArgumentNullException(nameof(curve));
            m_BlendProfile = blendProfile ? blendProfile : throw new ArgumentNullException(nameof(blendProfile));
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!Enum.IsDefined(typeof(AnimationBlendTechnique), Technique) ||
                !float.IsFinite(DurationSeconds) || DurationSeconds < 0f || Curve == null || !BlendProfile)
            {
                throw new InvalidOperationException("Animation Blend transition rule is incomplete.");
            }
            Curve.RequireValid();
            BlendProfile.BuildDense(rig);
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendTransitionOverride
    {
        [SerializeField] string m_SourceProducerIdentity = string.Empty;
        [SerializeField] bool m_SourceEmpty;
        [SerializeField] string m_TargetProducerIdentity = string.Empty;
        [SerializeField] bool m_TargetEmpty;
        [SerializeField] CharacterAnimationBlendTransitionRule m_Rule = new CharacterAnimationBlendTransitionRule();

        public string SourceProducerIdentity => m_SourceProducerIdentity ?? string.Empty;
        public bool SourceEmpty => m_SourceEmpty;
        public string TargetProducerIdentity => m_TargetProducerIdentity ?? string.Empty;
        public bool TargetEmpty => m_TargetEmpty;
        public CharacterAnimationBlendTransitionRule Rule => m_Rule;
        public string CanonicalKey => string.Join("|",
            SourceEmpty ? "<empty>" : SourceProducerIdentity,
            TargetEmpty ? "<empty>" : TargetProducerIdentity);

        public CharacterAnimationBlendTransitionOverride() { }

        public CharacterAnimationBlendTransitionOverride(
            string sourceProducerIdentity,
            bool sourceEmpty,
            string targetProducerIdentity,
            bool targetEmpty,
            CharacterAnimationBlendTransitionRule rule)
        {
            m_SourceProducerIdentity = sourceProducerIdentity ?? string.Empty;
            m_SourceEmpty = sourceEmpty;
            m_TargetProducerIdentity = targetProducerIdentity ?? string.Empty;
            m_TargetEmpty = targetEmpty;
            m_Rule = rule ?? throw new ArgumentNullException(nameof(rule));
            RequireEndpoints();
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            RequireEndpoints();
            Rule?.RequireValid(rig);
            if (Rule == null)
                throw new InvalidOperationException("Animation Blend transition override has no rule.");
        }

        void RequireEndpoints()
        {
            if (SourceEmpty == !string.IsNullOrWhiteSpace(SourceProducerIdentity) ||
                TargetEmpty == !string.IsNullOrWhiteSpace(TargetProducerIdentity))
            {
                throw new InvalidOperationException("Animation Blend transition endpoints must each be Empty or one producer identity.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendSlotDefinition
    {
        [SerializeField] string m_PoseSlotId = string.Empty;
        [SerializeField] CharacterAnimationBlendStackPolicy m_StackPolicy = new CharacterAnimationBlendStackPolicy();
        [SerializeField] CharacterAnimationBlendTransitionRule m_DefaultTransition = new CharacterAnimationBlendTransitionRule();
        [SerializeField] CharacterAnimationBlendTransitionOverride[] m_Overrides = Array.Empty<CharacterAnimationBlendTransitionOverride>();

        public PoseSlotId PoseSlotId => string.IsNullOrWhiteSpace(m_PoseSlotId) ? default : new PoseSlotId(m_PoseSlotId);
        public CharacterAnimationBlendStackPolicy StackPolicy => m_StackPolicy;
        public CharacterAnimationBlendTransitionRule DefaultTransition => m_DefaultTransition;
        public IReadOnlyList<CharacterAnimationBlendTransitionOverride> Overrides => m_Overrides ?? Array.Empty<CharacterAnimationBlendTransitionOverride>();

        public CharacterAnimationBlendSlotDefinition() { }

        public CharacterAnimationBlendSlotDefinition(
            PoseSlotId poseSlotId,
            CharacterAnimationBlendStackPolicy stackPolicy,
            CharacterAnimationBlendTransitionRule defaultTransition,
            CharacterAnimationBlendTransitionOverride[] overrides)
        {
            if (!poseSlotId.IsValid)
                throw new ArgumentException("Pose Slot identity is invalid.", nameof(poseSlotId));
            m_PoseSlotId = poseSlotId.Value;
            m_StackPolicy = stackPolicy ?? throw new ArgumentNullException(nameof(stackPolicy));
            m_DefaultTransition = defaultTransition ?? throw new ArgumentNullException(nameof(defaultTransition));
            m_Overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!PoseSlotId.IsValid || StackPolicy == null || DefaultTransition == null)
                throw new InvalidOperationException("Animation Blend Slot definition is incomplete.");
            StackPolicy.RequireValid();
            DefaultTransition.RequireValid(rig);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride transition = Overrides[i];
                if (transition == null)
                    throw new InvalidOperationException($"Animation Blend Slot '{PoseSlotId}' override #{i} is missing.");
                transition.RequireValid(rig);
                if (!keys.Add(transition.CanonicalKey))
                    throw new InvalidOperationException($"Animation Blend Slot '{PoseSlotId}' duplicates override '{transition.CanonicalKey}'.");
            }
        }
    }

    [CreateAssetMenu(fileName = "CharacterAnimationBlendLibrary", menuName = "3C/Character/Animation Blend Library")]
    public sealed class CharacterAnimationBlendLibrary : ScriptableObject
    {
        public const string SchemaVersion = "character-animation-blend-library/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_LibraryId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterAnimationBlendSlotDefinition[] m_Slots = Array.Empty<CharacterAnimationBlendSlotDefinition>();

        public string Schema => m_Schema ?? string.Empty;
        public string LibraryId => m_LibraryId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public IReadOnlyList<CharacterAnimationBlendSlotDefinition> Slots => m_Slots ?? Array.Empty<CharacterAnimationBlendSlotDefinition>();

        public void Configure(string libraryId, string revision, CharacterAnimationBlendSlotDefinition[] slots)
        {
            m_Schema = SchemaVersion;
            m_LibraryId = PoseSlotId.Require(libraryId, nameof(libraryId));
            m_Revision = PoseSlotId.Require(revision, nameof(revision));
            m_Slots = slots ?? throw new ArgumentNullException(nameof(slots));
        }

        public CharacterAnimationBlendSlotDefinition RequireSlot(PoseSlotId poseSlotId)
        {
            CharacterAnimationBlendSlotDefinition result = null;
            for (int i = 0; i < Slots.Count; i++)
            {
                CharacterAnimationBlendSlotDefinition candidate = Slots[i];
                if (candidate == null || candidate.PoseSlotId != poseSlotId)
                    continue;
                if (result != null)
                    throw new InvalidOperationException($"Blend Library '{name}' duplicates Pose Slot '{poseSlotId}'.");
                result = candidate;
            }
            return result ?? throw new InvalidOperationException($"Blend Library '{name}' has no Pose Slot '{poseSlotId}'.");
        }

        public void RequireValid(CharacterPresentationPoseGraphAsset poseGraph, CharacterAnimationRigDefinition rig)
        {
            if (!poseGraph || !rig || !string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(LibraryId) || string.IsNullOrEmpty(Revision))
            {
                throw new InvalidOperationException($"Blend Library '{name}' is incomplete.");
            }
            rig.RequireValid();
            var slots = new HashSet<PoseSlotId>();
            for (int i = 0; i < Slots.Count; i++)
            {
                CharacterAnimationBlendSlotDefinition slot = Slots[i];
                if (slot == null || !slot.PoseSlotId.IsValid || !slots.Add(slot.PoseSlotId))
                    throw new InvalidOperationException($"Blend Library '{name}' Pose Slot #{i} is invalid or duplicated.");
                slot.RequireValid(rig);
            }
            if (slots.Count != poseGraph.Graph.PoseSlots.Count)
                throw new InvalidOperationException($"Blend Library '{name}' does not cover every Pose Slot exactly once.");
            for (int i = 0; i < poseGraph.Graph.PoseSlots.Count; i++)
            {
                if (!slots.Contains(poseGraph.Graph.PoseSlots[i].PoseSlotId))
                    throw new InvalidOperationException($"Blend Library '{name}' is missing Pose Slot '{poseGraph.Graph.PoseSlots[i].PoseSlotId}'.");
            }
        }
    }
}
