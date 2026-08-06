using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.MotionMatching
{
    public readonly struct CharacterMotionMatchingBindingId : IEquatable<CharacterMotionMatchingBindingId>, IComparable<CharacterMotionMatchingBindingId>
    {
        public CharacterMotionMatchingBindingId(string value) => Value = MotionMatchingIdentity.Require(value, nameof(value));
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(CharacterMotionMatchingBindingId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(CharacterMotionMatchingBindingId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterMotionMatchingBindingId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(CharacterMotionMatchingBindingId left, CharacterMotionMatchingBindingId right) => left.Equals(right);
        public static bool operator !=(CharacterMotionMatchingBindingId left, CharacterMotionMatchingBindingId right) => !left.Equals(right);
    }

    [CreateAssetMenu(fileName = "CharacterMotionMatchingBinding", menuName = "3C/Character/Motion Matching/Binding")]
    public sealed class CharacterMotionMatchingBinding : ScriptableObject
    {
        public const string SchemaVersion = "character-motion-matching-binding/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_BindingId = string.Empty;
        [SerializeField] int m_Revision;
        [SerializeField] CharacterMotionMatchingProfile m_Profile;
        [SerializeField] CharacterMotionMatchingDatabaseChooser m_Chooser;

        public string Schema => m_Schema ?? string.Empty;
        public CharacterMotionMatchingBindingId BindingId => string.IsNullOrWhiteSpace(m_BindingId)
            ? default
            : new CharacterMotionMatchingBindingId(m_BindingId);
        public int Revision => m_Revision;
        public CharacterMotionMatchingProfile Profile => m_Profile;
        public CharacterMotionMatchingDatabaseChooser Chooser => m_Chooser;
        public CharacterMotionMatchingSearchDomainId SearchDomainId => m_Chooser ? m_Chooser.SearchDomainId : default;

        public void Configure(
            CharacterMotionMatchingBindingId bindingId,
            int revision,
            CharacterMotionMatchingProfile profile,
            CharacterMotionMatchingDatabaseChooser chooser,
            CharacterAnimationRigDefinition presentationRig)
        {
            if (!bindingId.IsValid || !profile || !chooser || !presentationRig)
                throw new ArgumentException("Motion Matching Binding is incomplete.");
            m_Schema = SchemaVersion;
            m_BindingId = bindingId.Value;
            m_Revision = revision;
            m_Profile = profile;
            m_Chooser = chooser;
            RequireValid(presentationRig);
        }

        public void RequireValid(CharacterAnimationRigDefinition presentationRig)
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || !BindingId.IsValid || !m_Profile || !m_Chooser)
                throw new InvalidOperationException($"Motion Matching Binding '{name}' has an invalid schema or identity.");
            MotionMatchingAuthoringValidation.RequireRevision(Revision, nameof(Revision));
            m_Profile.RequireRigClosure(presentationRig);
            m_Chooser.RequireValid(m_Profile, presentationRig);
            if (!m_Chooser.SearchDomainId.IsValid)
                throw new InvalidOperationException($"Motion Matching Binding '{name}' has no Search Domain.");
        }
    }
}
