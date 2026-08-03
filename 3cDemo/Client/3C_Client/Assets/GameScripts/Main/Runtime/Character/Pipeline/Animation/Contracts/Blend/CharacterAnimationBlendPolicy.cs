using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationStoredPosePolicy : byte
    {
        Disabled = 1,
        CompressOldest = 2
    }

    [Serializable]
    public sealed class CharacterAnimationBlendStackPolicy
    {
        [SerializeField, Min(2)] int m_MaxActiveSourceEntries = 4;
        [SerializeField] AnimationStoredPosePolicy m_StoredPosePolicy = AnimationStoredPosePolicy.CompressOldest;
        [SerializeField, Min(0f)] float m_MaxBlendInTimeToReplaceNewest = 0.05f;
        [SerializeField] float m_DepthBlendTimeMultiplier = 1f;

        public int MaxActiveSourceEntries => m_MaxActiveSourceEntries;
        public AnimationStoredPosePolicy StoredPosePolicy => m_StoredPosePolicy;
        public float MaxBlendInTimeToReplaceNewest => m_MaxBlendInTimeToReplaceNewest;
        public float DepthBlendTimeMultiplier => m_DepthBlendTimeMultiplier;

        public void Configure(
            int maxActiveSourceEntries,
            AnimationStoredPosePolicy storedPosePolicy,
            float maxBlendInTimeToReplaceNewest,
            float depthBlendTimeMultiplier)
        {
            m_MaxActiveSourceEntries = maxActiveSourceEntries;
            m_StoredPosePolicy = storedPosePolicy;
            m_MaxBlendInTimeToReplaceNewest = maxBlendInTimeToReplaceNewest;
            m_DepthBlendTimeMultiplier = depthBlendTimeMultiplier;
            RequireValid();
        }

        public void RequireValid()
        {
            if (MaxActiveSourceEntries < 2 ||
                !Enum.IsDefined(typeof(AnimationStoredPosePolicy), StoredPosePolicy) ||
                !float.IsFinite(MaxBlendInTimeToReplaceNewest) ||
                MaxBlendInTimeToReplaceNewest < 0f ||
                !float.IsFinite(DepthBlendTimeMultiplier) ||
                DepthBlendTimeMultiplier <= 0f)
            {
                throw new InvalidOperationException("Animation Blend Stack policy is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendTransitionRule
    {
        [SerializeField] AnimationTransitionBlendLogic m_BlendLogic =
            AnimationTransitionBlendLogic.StandardBlend;
        [SerializeField, Min(0f)] float m_DurationSeconds = 0.2f;
        [SerializeField] CharacterAnimationBlendMode m_BlendMode =
            CharacterAnimationBlendMode.EaseInOut;
        [SerializeField] CharacterAnimationBlendCurveAsset m_CustomBlendCurve;
        [SerializeField] CharacterAnimationBlendProfile m_BlendProfile;

        public AnimationTransitionBlendLogic BlendLogic => m_BlendLogic;
        public float DurationSeconds => m_DurationSeconds;
        public CharacterAnimationBlendMode BlendMode => m_BlendMode;
        public CharacterAnimationBlendCurveAsset CustomBlendCurve => m_CustomBlendCurve;
        public CharacterAnimationBlendProfile BlendProfile => m_BlendProfile;

        public void Configure(
            float durationSeconds,
            CharacterAnimationBlendMode blendMode,
            CharacterAnimationBlendCurveAsset customBlendCurve,
            CharacterAnimationBlendProfile blendProfile,
            AnimationTransitionBlendLogic blendLogic = AnimationTransitionBlendLogic.StandardBlend)
        {
            m_BlendLogic = blendLogic;
            m_DurationSeconds = durationSeconds;
            m_BlendMode = blendMode;
            m_CustomBlendCurve = customBlendCurve;
            m_BlendProfile = blendProfile
                ? blendProfile
                : throw new ArgumentNullException(nameof(blendProfile));
        }

        public AnimationBlendCurvePayload CompileCurve() =>
            CharacterAnimationBlendCurveCompiler.Compile(BlendMode, CustomBlendCurve);

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!Enum.IsDefined(typeof(AnimationTransitionBlendLogic), BlendLogic) ||
                !float.IsFinite(DurationSeconds) ||
                (BlendLogic == AnimationTransitionBlendLogic.Inertialization
                    ? DurationSeconds <= 0f
                    : DurationSeconds < 0f) ||
                !BlendProfile)
            {
                throw new InvalidOperationException("Animation Blend transition rule is incomplete.");
            }
            CharacterAnimationBlendCurveCompiler.RequireConfiguration(
                BlendMode,
                CustomBlendCurve);
            BlendProfile.BuildDense(rig);
        }
    }

    [Serializable]
    public sealed class CharacterAnimationBlendTransitionOverride
    {
        [SerializeField] string m_SourceOwnerIdentity = string.Empty;
        [SerializeField] AnimationBlendTransitionEndpointKind m_SourceEndpointKind;
        [SerializeField] string m_TargetOwnerIdentity = string.Empty;
        [SerializeField] AnimationBlendTransitionEndpointKind m_TargetEndpointKind;
        [SerializeField] CharacterAnimationBlendTransitionRule m_Rule =
            new CharacterAnimationBlendTransitionRule();

        public string SourceOwnerIdentity => m_SourceOwnerIdentity ?? string.Empty;
        public AnimationBlendTransitionEndpointKind SourceEndpointKind => m_SourceEndpointKind;
        public string TargetOwnerIdentity => m_TargetOwnerIdentity ?? string.Empty;
        public AnimationBlendTransitionEndpointKind TargetEndpointKind => m_TargetEndpointKind;
        public CharacterAnimationBlendTransitionRule Rule => m_Rule;
        public string CanonicalKey => string.Join("|",
            SourceEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner
                ? SourceOwnerIdentity
                : SourceEndpointKind.ToString(),
            TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner
                ? TargetOwnerIdentity
                : TargetEndpointKind.ToString());

        public void Configure(
            string sourceOwnerIdentity,
            AnimationBlendTransitionEndpointKind sourceEndpointKind,
            string targetOwnerIdentity,
            AnimationBlendTransitionEndpointKind targetEndpointKind,
            CharacterAnimationBlendTransitionRule rule)
        {
            m_SourceOwnerIdentity = sourceOwnerIdentity ?? string.Empty;
            m_SourceEndpointKind = sourceEndpointKind;
            m_TargetOwnerIdentity = targetOwnerIdentity ?? string.Empty;
            m_TargetEndpointKind = targetEndpointKind;
            m_Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!Enum.IsDefined(typeof(AnimationBlendTransitionEndpointKind), SourceEndpointKind) ||
                !Enum.IsDefined(typeof(AnimationBlendTransitionEndpointKind), TargetEndpointKind) ||
                (SourceEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner) !=
                !string.IsNullOrWhiteSpace(SourceOwnerIdentity) ||
                (TargetEndpointKind == AnimationBlendTransitionEndpointKind.SourceOwner) !=
                !string.IsNullOrWhiteSpace(TargetOwnerIdentity) ||
                Rule == null)
            {
                throw new InvalidOperationException(
                    "Animation Blend transition endpoint kinds and source owner identities are inconsistent.");
            }
            Rule.RequireValid(rig);
        }
    }

    [CreateAssetMenu(
        fileName = "CharacterAnimationBlendPolicy",
        menuName = "3C/Character/Animation Blend Policy")]
    public sealed class CharacterAnimationBlendPolicy : ScriptableObject
    {
        public const string SchemaVersion = "character-animation-blend-policy/v3";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_PolicyId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField] CharacterAnimationBlendStackPolicy m_StackPolicy =
            new CharacterAnimationBlendStackPolicy();
        [SerializeField] CharacterAnimationBlendTransitionRule m_DefaultTransition =
            new CharacterAnimationBlendTransitionRule();
        [SerializeField] CharacterAnimationBlendTransitionOverride[] m_Overrides =
            Array.Empty<CharacterAnimationBlendTransitionOverride>();

        public string Schema => m_Schema ?? string.Empty;
        public string PolicyId => m_PolicyId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public CharacterAnimationBlendStackPolicy StackPolicy => m_StackPolicy;
        public CharacterAnimationBlendTransitionRule DefaultTransition => m_DefaultTransition;
        public IReadOnlyList<CharacterAnimationBlendTransitionOverride> Overrides =>
            m_Overrides ?? Array.Empty<CharacterAnimationBlendTransitionOverride>();

        public void Configure(
            string policyId,
            string revision,
            CharacterAnimationBlendStackPolicy stackPolicy,
            CharacterAnimationBlendTransitionRule defaultTransition,
            CharacterAnimationBlendTransitionOverride[] overrides,
            CharacterAnimationRigDefinition rig)
        {
            m_Schema = SchemaVersion;
            m_PolicyId = PoseNodeId.Require(policyId, nameof(policyId));
            m_Revision = PoseNodeId.Require(revision, nameof(revision));
            m_StackPolicy = stackPolicy ?? throw new ArgumentNullException(nameof(stackPolicy));
            m_DefaultTransition = defaultTransition ??
                throw new ArgumentNullException(nameof(defaultTransition));
            m_Overrides = overrides ?? throw new ArgumentNullException(nameof(overrides));
            RequireValid(rig);
        }

        public void RequireValid(CharacterAnimationRigDefinition rig)
        {
            if (!rig ||
                !string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(PolicyId) ||
                string.IsNullOrWhiteSpace(Revision) ||
                StackPolicy == null ||
                DefaultTransition == null)
            {
                throw new InvalidOperationException(
                    $"Animation Blend Policy '{name}' is incomplete.");
            }
            rig.RequireValid();
            StackPolicy.RequireValid();
            DefaultTransition.RequireValid(rig);
            var keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Overrides.Count; i++)
            {
                CharacterAnimationBlendTransitionOverride transition = Overrides[i];
                if (transition == null)
                {
                    throw new InvalidOperationException(
                        $"Animation Blend Policy '{name}' override #{i} is missing.");
                }
                transition.RequireValid(rig);
                if (!keys.Add(transition.CanonicalKey))
                {
                    throw new InvalidOperationException(
                        $"Animation Blend Policy '{name}' duplicates override '{transition.CanonicalKey}'.");
                }
            }
        }
    }
}
