using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterKneeAngleResponsePolicy : byte
    {
        Disabled = 0,
        Forced = 1
    }

    public enum CharacterFullBodyIkSmoothing : byte
    {
        None = 0,
        Exponential = 1,
        Cubic = 2
    }

    [Serializable]
    public sealed class CharacterFullBodyIkLimbSettings
    {
        [SerializeField, Range(0f, 1f)] float m_Pin;
        [SerializeField, Range(0f, 1f)] float m_Pull = 1f;
        [SerializeField, Range(0f, 1f)] float m_Push;
        [SerializeField, Range(-1f, 1f)] float m_PushParent;
        [SerializeField, Range(0f, 1f)] float m_Reach = 0.1f;
        [SerializeField] CharacterFullBodyIkSmoothing m_ReachSmoothing = CharacterFullBodyIkSmoothing.Exponential;
        [SerializeField] CharacterFullBodyIkSmoothing m_PushSmoothing = CharacterFullBodyIkSmoothing.Exponential;
        [SerializeField, Range(0f, 1f)] float m_MappingWeight = 1f;
        [SerializeField, Range(0f, 1f)] float m_MaintainRotationWeight;
        [SerializeField, Range(0f, 1f)] float m_BendConstraintWeight;
        [SerializeField, Range(0f, 1f)] float m_BendClamp = 0.505f;

        public float Pin => m_Pin;
        public float Pull => m_Pull;
        public float Push => m_Push;
        public float PushParent => m_PushParent;
        public float Reach => m_Reach;
        public CharacterFullBodyIkSmoothing ReachSmoothing => m_ReachSmoothing;
        public CharacterFullBodyIkSmoothing PushSmoothing => m_PushSmoothing;
        public float MappingWeight => m_MappingWeight;
        public float MaintainRotationWeight => m_MaintainRotationWeight;
        public float BendConstraintWeight => m_BendConstraintWeight;
        public float BendClamp => m_BendClamp;

        internal void ApplyTuning(
            string fieldPath,
            CharacterPoseTuningValue value)
        {
            switch (fieldPath)
            {
                case "pin": m_Pin = RequireFloat(value, fieldPath); break;
                case "pull": m_Pull = RequireFloat(value, fieldPath); break;
                case "push": m_Push = RequireFloat(value, fieldPath); break;
                case "push-parent": m_PushParent = RequireFloat(value, fieldPath); break;
                case "reach": m_Reach = RequireFloat(value, fieldPath); break;
                case "reach-smoothing": m_ReachSmoothing = RequireSmoothing(value, fieldPath); break;
                case "push-smoothing": m_PushSmoothing = RequireSmoothing(value, fieldPath); break;
                case "mapping-weight": m_MappingWeight = RequireFloat(value, fieldPath); break;
                case "maintain-rotation-weight": m_MaintainRotationWeight = RequireFloat(value, fieldPath); break;
                case "bend-constraint-weight": m_BendConstraintWeight = RequireFloat(value, fieldPath); break;
                case "bend-clamp": m_BendClamp = RequireFloat(value, fieldPath); break;
                default:
                    throw new InvalidOperationException($"Full Body IK limb tuning field '{fieldPath}' is not declared.");
            }
        }

        public void RequireValid(string limb)
        {
            RequireRange(Pin, 0f, 1f, limb, nameof(Pin));
            RequireRange(Pull, 0f, 1f, limb, nameof(Pull));
            RequireRange(Push, 0f, 1f, limb, nameof(Push));
            RequireRange(PushParent, -1f, 1f, limb, nameof(PushParent));
            RequireRange(Reach, 0f, 1f, limb, nameof(Reach));
            RequireRange(MappingWeight, 0f, 1f, limb, nameof(MappingWeight));
            RequireRange(MaintainRotationWeight, 0f, 1f, limb, nameof(MaintainRotationWeight));
            RequireRange(BendConstraintWeight, 0f, 1f, limb, nameof(BendConstraintWeight));
            RequireRange(BendClamp, 0f, 1f, limb, nameof(BendClamp));
            if (!Enum.IsDefined(typeof(CharacterFullBodyIkSmoothing), ReachSmoothing) ||
                !Enum.IsDefined(typeof(CharacterFullBodyIkSmoothing), PushSmoothing))
                throw new InvalidOperationException($"Full Body IK Profile {limb} smoothing is invalid.");
        }

        static void RequireRange(float value, float minimum, float maximum, string limb, string field)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new InvalidOperationException($"Full Body IK Profile {limb} {field} is outside FinalIK's supported range [{minimum}, {maximum}].");
        }

        static float RequireFloat(CharacterPoseTuningValue value, string fieldPath)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"Full Body IK limb tuning field '{fieldPath}' requires a float.");
            return value.FloatValue;
        }

        static CharacterFullBodyIkSmoothing RequireSmoothing(
            CharacterPoseTuningValue value,
            string fieldPath)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Enum ||
                !Enum.IsDefined(typeof(CharacterFullBodyIkSmoothing), value.EnumValue))
                throw new InvalidOperationException($"Full Body IK limb tuning field '{fieldPath}' has an invalid smoothing value.");
            return (CharacterFullBodyIkSmoothing)value.EnumValue;
        }
    }

    [CreateAssetMenu(fileName = "CharacterFullBodyIkProfile", menuName = "3C/Character/Full Body IK Profile")]
    public sealed class CharacterFullBodyIkProfile : ScriptableObject
    {
        public const string SchemaVersion = "character-full-body-ik-profile/v2";
        public const string SolverBackendIdentity = "rootmotion.finalik.full-body-biped-ik/indexed-pose-backend";
        public const string AuditedVendorSourceRevision = "7cd67a8e9ca9e22b68e466f60bf27aa29ea653cf3edc619566b0ac6d41ee3cb1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_Revision = string.Empty;
        [SerializeField, Range(0, 10)] int m_Iterations = 4;
        [SerializeField] bool m_FabrikPass = true;
        [SerializeField, Range(0f, 1f)] float m_SpineStiffness = 0.5f;
        [SerializeField, Range(-1f, 1f)] float m_PullBodyVertical = 0.5f;
        [SerializeField, Range(-1f, 1f)] float m_PullBodyHorizontal;
        [SerializeField, Range(0f, 1f)] float m_NodeWeight = 1f;
        [SerializeField] CharacterFullBodyIkLimbSettings m_LeftArm = new CharacterFullBodyIkLimbSettings();
        [SerializeField] CharacterFullBodyIkLimbSettings m_RightArm = new CharacterFullBodyIkLimbSettings();
        [SerializeField] CharacterFullBodyIkLimbSettings m_LeftLeg = new CharacterFullBodyIkLimbSettings();
        [SerializeField] CharacterFullBodyIkLimbSettings m_RightLeg = new CharacterFullBodyIkLimbSettings();
        [SerializeField] CharacterKneeAngleResponsePolicy m_KneeAngleResponsePolicy;
        [SerializeField] float m_KneeAngleUpstairRate;
        [SerializeField] float m_KneeAngleDownstairRate;

        public string Schema => m_Schema ?? string.Empty;
        public string ProfileId => m_ProfileId ?? string.Empty;
        public string Revision => m_Revision ?? string.Empty;
        public int Iterations => m_Iterations;
        public bool FabrikPass => m_FabrikPass;
        public float SpineStiffness => m_SpineStiffness;
        public float PullBodyVertical => m_PullBodyVertical;
        public float PullBodyHorizontal => m_PullBodyHorizontal;
        public float NodeWeight => m_NodeWeight;
        public CharacterFullBodyIkLimbSettings LeftArm => m_LeftArm;
        public CharacterFullBodyIkLimbSettings RightArm => m_RightArm;
        public CharacterFullBodyIkLimbSettings LeftLeg => m_LeftLeg;
        public CharacterFullBodyIkLimbSettings RightLeg => m_RightLeg;
        public CharacterKneeAngleResponsePolicy KneeAngleResponsePolicy => m_KneeAngleResponsePolicy;
        public float KneeAngleUpstairRate => m_KneeAngleUpstairRate;
        public float KneeAngleDownstairRate => m_KneeAngleDownstairRate;

        internal void ApplyTuning(
            string fieldPath,
            CharacterPoseTuningValue value)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                throw new ArgumentException("Full Body IK tuning field is missing.", nameof(fieldPath));
            if (fieldPath.StartsWith("left-arm/", StringComparison.Ordinal))
                LeftArm.ApplyTuning(fieldPath.Substring("left-arm/".Length), value);
            else if (fieldPath.StartsWith("right-arm/", StringComparison.Ordinal))
                RightArm.ApplyTuning(fieldPath.Substring("right-arm/".Length), value);
            else if (fieldPath.StartsWith("left-leg/", StringComparison.Ordinal))
                LeftLeg.ApplyTuning(fieldPath.Substring("left-leg/".Length), value);
            else if (fieldPath.StartsWith("right-leg/", StringComparison.Ordinal))
                RightLeg.ApplyTuning(fieldPath.Substring("right-leg/".Length), value);
            else
                ApplyRootTuning(fieldPath, value);
            m_Schema = SchemaVersion;
            m_Revision = ComputeRevision();
            RequireValid();
        }

        public string ComputeRevision()
        {
            var parts = new List<string>(52)
            {
                SchemaVersion,
                ProfileId,
                Iterations.ToString(CultureInfo.InvariantCulture),
                FabrikPass ? "1" : "0",
                Format(SpineStiffness),
                Format(PullBodyVertical),
                Format(PullBodyHorizontal),
                Format(NodeWeight)
            };
            AppendLimb(parts, LeftArm);
            AppendLimb(parts, RightArm);
            AppendLimb(parts, LeftLeg);
            AppendLimb(parts, RightLeg);
            parts.Add(((byte)KneeAngleResponsePolicy).ToString(CultureInfo.InvariantCulture));
            parts.Add(Format(KneeAngleUpstairRate));
            parts.Add(Format(KneeAngleDownstairRate));
            return StableHash.Compute(parts.ToArray()).ToString();
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(ProfileId) ||
                !string.Equals(ProfileId, ProfileId.Trim(), StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(Revision) ||
                !string.Equals(Revision, Revision.Trim(), StringComparison.Ordinal) ||
                Iterations < 0 || Iterations > 10)
                throw new InvalidOperationException($"Full Body IK Profile '{name}' identity or iteration count is invalid.");
            RequireRange(SpineStiffness, 0f, 1f, nameof(SpineStiffness));
            RequireRange(PullBodyVertical, -1f, 1f, nameof(PullBodyVertical));
            RequireRange(PullBodyHorizontal, -1f, 1f, nameof(PullBodyHorizontal));
            RequireRange(NodeWeight, 0f, 1f, nameof(NodeWeight));
            if (LeftArm == null || RightArm == null || LeftLeg == null || RightLeg == null)
                throw new InvalidOperationException($"Full Body IK Profile '{name}' limb settings are incomplete.");
            LeftArm.RequireValid("Left Arm");
            RightArm.RequireValid("Right Arm");
            LeftLeg.RequireValid("Left Leg");
            RightLeg.RequireValid("Right Leg");
            if (!Enum.IsDefined(typeof(CharacterKneeAngleResponsePolicy), KneeAngleResponsePolicy) ||
                !float.IsFinite(KneeAngleUpstairRate) || KneeAngleUpstairRate < 0f ||
                !float.IsFinite(KneeAngleDownstairRate) || KneeAngleDownstairRate < 0f)
                throw new InvalidOperationException($"Full Body IK Profile '{name}' knee angle response is invalid.");
            if (!string.Equals(Revision, ComputeRevision(), StringComparison.Ordinal))
                throw new InvalidOperationException($"Full Body IK Profile '{name}' revision is stale.");
        }

        void OnValidate()
        {
            m_Schema = SchemaVersion;
            m_ProfileId = m_ProfileId?.Trim() ?? string.Empty;
            m_Revision = string.IsNullOrEmpty(m_ProfileId)
                ? string.Empty
                : ComputeRevision();
        }

        static void AppendLimb(List<string> parts, CharacterFullBodyIkLimbSettings value)
        {
            if (value == null)
            {
                parts.Add("null");
                return;
            }
            parts.Add(Format(value.Pin));
            parts.Add(Format(value.Pull));
            parts.Add(Format(value.Push));
            parts.Add(Format(value.PushParent));
            parts.Add(Format(value.Reach));
            parts.Add(((byte)value.ReachSmoothing).ToString(CultureInfo.InvariantCulture));
            parts.Add(((byte)value.PushSmoothing).ToString(CultureInfo.InvariantCulture));
            parts.Add(Format(value.MappingWeight));
            parts.Add(Format(value.MaintainRotationWeight));
            parts.Add(Format(value.BendConstraintWeight));
            parts.Add(Format(value.BendClamp));
        }

        static string Format(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        void ApplyRootTuning(string fieldPath, CharacterPoseTuningValue value)
        {
            switch (fieldPath)
            {
                case "iterations":
                    m_Iterations = RequireInteger(value, fieldPath);
                    break;
                case "fabrik-pass":
                    m_FabrikPass = RequireBoolean(value, fieldPath);
                    break;
                case "spine-stiffness":
                    m_SpineStiffness = RequireFloat(value, fieldPath);
                    break;
                case "pull-body-vertical":
                    m_PullBodyVertical = RequireFloat(value, fieldPath);
                    break;
                case "pull-body-horizontal":
                    m_PullBodyHorizontal = RequireFloat(value, fieldPath);
                    break;
                case "node-weight":
                    m_NodeWeight = RequireFloat(value, fieldPath);
                    break;
                default:
                    throw new InvalidOperationException($"Full Body IK tuning field '{fieldPath}' is not declared.");
            }
        }

        static float RequireFloat(CharacterPoseTuningValue value, string fieldPath)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Float)
                throw new InvalidOperationException($"Full Body IK tuning field '{fieldPath}' requires a float.");
            return value.FloatValue;
        }

        static int RequireInteger(CharacterPoseTuningValue value, string fieldPath)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Integer)
                throw new InvalidOperationException($"Full Body IK tuning field '{fieldPath}' requires an integer.");
            return value.IntegerValue;
        }

        static bool RequireBoolean(CharacterPoseTuningValue value, string fieldPath)
        {
            if (value.Kind != CharacterPoseTuningValueKind.Boolean)
                throw new InvalidOperationException($"Full Body IK tuning field '{fieldPath}' requires a boolean.");
            return value.BooleanValue;
        }

        static void RequireRange(float value, float minimum, float maximum, string field)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new InvalidOperationException($"Full Body IK Profile {field} is outside FinalIK's supported range [{minimum}, {maximum}].");
        }
    }
}
