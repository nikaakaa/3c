using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum CharacterAnimationBlendSpaceValidationCode : byte
    {
        MissingIdentity = 1,
        MissingRevision = 2,
        MissingRig = 3,
        InvalidMode = 4,
        InvalidAxis = 5,
        InvalidSample = 6,
        DuplicateSample = 7,
        DuplicatePosition = 8,
        DegenerateSpace = 9,
        InvalidPhaseReference = 10,
        InvalidMarkerTopology = 11,
        InvalidParameterPolicy = 12
    }

    public readonly struct CharacterAnimationBlendSpaceValidationIssue
    {
        public CharacterAnimationBlendSpaceValidationIssue(
            CharacterAnimationBlendSpaceValidationCode code,
            string path,
            string message)
        {
            Code = code;
            Path = path ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public CharacterAnimationBlendSpaceValidationCode Code { get; }
        public string Path { get; }
        public string Message { get; }
        public override string ToString() => $"{Code} [{Path}]: {Message}";
    }

    public sealed class CharacterAnimationBlendSpaceValidationReport
    {
        readonly List<CharacterAnimationBlendSpaceValidationIssue> m_Issues = new List<CharacterAnimationBlendSpaceValidationIssue>();
        public IReadOnlyList<CharacterAnimationBlendSpaceValidationIssue> Issues => m_Issues;
        public bool IsValid => m_Issues.Count == 0;
        internal void Add(CharacterAnimationBlendSpaceValidationCode code, string path, string message) =>
            m_Issues.Add(new CharacterAnimationBlendSpaceValidationIssue(code, path, message));
        public void CopyMessagesTo(List<string> errors)
        {
            if (errors == null)
                return;
            for (int i = 0; i < m_Issues.Count; i++)
                errors.Add(m_Issues[i].ToString());
        }
    }

    public static class CharacterAnimationBlendSpaceValidator
    {
        const float PositionEpsilon = 0.000001f;

        public static CharacterAnimationBlendSpaceValidationReport Validate(CharacterAnimationBlendSpaceAsset asset)
        {
            var report = new CharacterAnimationBlendSpaceValidationReport();
            if (!asset)
            {
                report.Add(CharacterAnimationBlendSpaceValidationCode.MissingIdentity, "asset", "Blend Space asset is missing.");
                return report;
            }
            string assetPath = asset.BlendSpaceId.IsValid ? asset.BlendSpaceId.Value : asset.name;
            if (!asset.BlendSpaceId.IsValid)
                report.Add(CharacterAnimationBlendSpaceValidationCode.MissingIdentity, assetPath, "BlendSpaceId is missing.");
            if (string.IsNullOrWhiteSpace(asset.ContentRevision))
                report.Add(CharacterAnimationBlendSpaceValidationCode.MissingRevision, assetPath, "Content revision is missing.");
            if (!asset.Rig || string.IsNullOrWhiteSpace(asset.Rig.Revision))
                report.Add(CharacterAnimationBlendSpaceValidationCode.MissingRig, assetPath, "Rig or Rig revision is missing.");
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpaceMode), asset.Mode))
                report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidMode, assetPath, "Blend Space mode is not registered.");
            ValidateAxis(asset.XAxis, $"{assetPath}/axis/x", report);
            if (asset.AxisCount == 2)
                ValidateAxis(asset.YAxis, $"{assetPath}/axis/y", report);
            else if (asset.YAxis != null)
                report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidAxis, $"{assetPath}/axis/y", "Linear1D cannot retain a Y axis.");

            var sampleIds = new HashSet<CharacterAnimationBlendSpaceSampleId>();
            int zeroCount = 0;
            for (int i = 0; i < asset.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = asset.Samples[i];
                string path = $"{assetPath}/samples/{i}";
                if (sample == null || !sample.SampleId.IsValid || !sample.Clip ||
                    !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceSampleRole), sample.Role) ||
                    !float.IsFinite(sample.Position.x) || !float.IsFinite(sample.Position.y) ||
                    sample.Position.x < asset.XAxis.Minimum || sample.Position.x > asset.XAxis.Maximum ||
                    asset.AxisCount == 2 && (sample.Position.y < asset.YAxis.Minimum || sample.Position.y > asset.YAxis.Maximum) ||
                    asset.AxisCount == 1 && sample.Position.y != 0f ||
                    sample.Role == CharacterAnimationBlendSpaceSampleRole.StationaryPose &&
                    (!float.IsFinite(sample.StationaryNormalizedTime) || sample.StationaryNormalizedTime < 0f || sample.StationaryNormalizedTime > 1f))
                {
                    report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidSample, path, "Sample identity, clip, position, role, or time is invalid.");
                    continue;
                }
                path = $"{assetPath}/samples/{sample.SampleId.Value}";
                if (!sampleIds.Add(sample.SampleId))
                    report.Add(CharacterAnimationBlendSpaceValidationCode.DuplicateSample, path, "SampleId is duplicated.");
                if (sample.Position.sqrMagnitude <= PositionEpsilon)
                    zeroCount++;
                for (int previous = 0; previous < i; previous++)
                {
                    CharacterAnimationBlendSpaceSample other = asset.Samples[previous];
                    if (other == null)
                        continue;
                    bool duplicate = asset.Mode == CharacterAnimationBlendSpaceMode.Linear1D
                        ? Math.Abs(other.Position.x - sample.Position.x) <= PositionEpsilon
                        : (other.Position - sample.Position).sqrMagnitude <= PositionEpsilon;
                    if (duplicate)
                        report.Add(CharacterAnimationBlendSpaceValidationCode.DuplicatePosition, path, $"Sample position duplicates '{other.SampleId}'.");
                }
                var sampleParameters = new HashSet<PoseParameterId>();
                for (int parameterIndex = 0; parameterIndex < sample.Parameters.Count; parameterIndex++)
                {
                    CharacterAnimationBlendSpaceSampleParameter parameter = sample.Parameters[parameterIndex];
                    if (parameter == null || !parameter.ParameterId.IsValid || !float.IsFinite(parameter.Value) ||
                        !sampleParameters.Add(parameter.ParameterId))
                        report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidParameterPolicy, $"{path}/parameters/{parameterIndex}", "Sample Parameter is invalid or duplicated.");
                }
            }
            if (asset.Samples.Count == 0)
                report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidSample, $"{assetPath}/samples", "Blend Space has no samples.");
            if (asset.Mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D && zeroCount > 1)
                report.Add(CharacterAnimationBlendSpaceValidationCode.DuplicatePosition, $"{assetPath}/samples", "Directional Blend Space has more than one zero-vector sample.");
            if (asset.Mode == CharacterAnimationBlendSpaceMode.FreeformCartesian2D && IsCartesianDegenerate(asset.Samples))
                report.Add(CharacterAnimationBlendSpaceValidationCode.DegenerateSpace, $"{assetPath}/samples", "Cartesian Blend Space samples do not define a two-dimensional region.");

            ValidatePhase(asset, assetPath, report);
            ValidatePolicies(asset, assetPath, report);
            if (report.IsValid)
            {
                try
                {
                    var positions = new CharacterAnimationBlendSpaceCompiledPosition[asset.Samples.Count];
                    for (int i = 0; i < positions.Length; i++)
                    {
                        CharacterAnimationBlendSpaceSample sample = asset.Samples[i];
                        positions[i] = new CharacterAnimationBlendSpaceCompiledPosition(sample.SampleId, sample.Position.x, sample.Position.y);
                    }
                    _ = new CharacterAnimationBlendSpaceSolverPlan(asset.Mode, positions);
                }
                catch (Exception exception)
                {
                    report.Add(CharacterAnimationBlendSpaceValidationCode.DegenerateSpace, $"{assetPath}/samples", exception.Message);
                }
            }
            return report;
        }

        static void ValidateAxis(CharacterAnimationBlendSpaceAxis axis, string path, CharacterAnimationBlendSpaceValidationReport report)
        {
            if (axis == null || !axis.ParameterId.IsValid || axis.ValueType != PoseParameterValueType.Float ||
                string.IsNullOrWhiteSpace(axis.Unit) || !float.IsFinite(axis.Minimum) || !float.IsFinite(axis.Maximum) || axis.Minimum >= axis.Maximum)
                report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidAxis, path, "Axis ParameterId, Float type, unit, or range is invalid.");
        }

        static void ValidatePhase(CharacterAnimationBlendSpaceAsset asset, string assetPath, CharacterAnimationBlendSpaceValidationReport report)
        {
            if (!Enum.IsDefined(typeof(CharacterAnimationBlendSpacePhasePolicy), asset.PhasePolicy))
            {
                report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidPhaseReference, assetPath, "Phase policy is invalid.");
                return;
            }
            if (asset.PhasePolicy == CharacterAnimationBlendSpacePhasePolicy.SharedNormalizedPhase)
            {
                if (asset.PhaseReferenceSampleId.IsValid)
                    report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidPhaseReference, assetPath, "SharedNormalizedPhase retains a reference sample.");
                return;
            }
            if (asset.PhasePolicy != CharacterAnimationBlendSpacePhasePolicy.LocomotionPhase)
            {
                report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidPhaseReference, assetPath, "Phase policy is outside the installed catalog.");
                return;
            }
            CharacterAnimationBlendSpaceSample reference = asset.FindSample(asset.PhaseReferenceSampleId);
            if (reference == null || reference.Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidPhaseReference, assetPath, "Phase Reference Sample is missing or stationary.");
            for (int i = 0; i < asset.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = asset.Samples[i];
                if (sample == null || sample.Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                    continue;
                string path = $"{assetPath}/samples/{sample.SampleId}";
                if (!sample.Clip || !sample.Clip.isLooping)
                    report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidPhaseReference, path, "Locomotion Phase Dynamic sample requires a looping AnimationClip.");
            }
        }

        static void ValidatePolicies(CharacterAnimationBlendSpaceAsset asset, string assetPath, CharacterAnimationBlendSpaceValidationReport report)
        {
            var ids = new HashSet<PoseParameterId>();
            for (int i = 0; i < asset.PoseParameterPolicies.Count; i++)
            {
                CharacterAnimationBlendSpacePoseParameterPolicy policy = asset.PoseParameterPolicies[i];
                if (policy == null || !policy.ParameterId.IsValid || !Enum.IsDefined(typeof(CharacterAnimationBlendSpaceParameterPolicy), policy.Policy) || !ids.Add(policy.ParameterId))
                    report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidParameterPolicy, $"{assetPath}/parameters/{i}", "Pose Parameter policy is invalid or duplicated.");
                if (policy == null || !policy.ParameterId.IsValid)
                    continue;
                int available = 0;
                for (int sampleIndex = 0; sampleIndex < asset.Samples.Count; sampleIndex++)
                {
                    CharacterAnimationBlendSpaceSample sample = asset.Samples[sampleIndex];
                    if (sample == null)
                        continue;
                    bool found = false;
                    for (int valueIndex = 0; valueIndex < sample.Parameters.Count; valueIndex++)
                        found |= sample.Parameters[valueIndex] != null && sample.Parameters[valueIndex].ParameterId.Equals(policy.ParameterId);
                    if (found)
                        available++;
                }
                if (policy.Policy == CharacterAnimationBlendSpaceParameterPolicy.RequireAllSamplesWeighted && available != asset.Samples.Count ||
                    policy.Policy == CharacterAnimationBlendSpaceParameterPolicy.WeightedAvailableSamples && available == 0 ||
                    policy.Policy == CharacterAnimationBlendSpaceParameterPolicy.Unavailable && available != 0)
                    report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidParameterPolicy, $"{assetPath}/parameters/{policy.ParameterId}", "Sample Parameter availability does not match its explicit policy.");
            }
            for (int sampleIndex = 0; sampleIndex < asset.Samples.Count; sampleIndex++)
            {
                CharacterAnimationBlendSpaceSample sample = asset.Samples[sampleIndex];
                if (sample == null)
                    continue;
                for (int valueIndex = 0; valueIndex < sample.Parameters.Count; valueIndex++)
                {
                    CharacterAnimationBlendSpaceSampleParameter value = sample.Parameters[valueIndex];
                    if (value != null && !ids.Contains(value.ParameterId))
                        report.Add(CharacterAnimationBlendSpaceValidationCode.InvalidParameterPolicy, $"{assetPath}/samples/{sample.SampleId}/parameters/{value.ParameterId}", "Sample Parameter has no asset policy.");
                }
            }
        }

        static bool IsCartesianDegenerate(IReadOnlyList<CharacterAnimationBlendSpaceSample> samples)
        {
            if (samples.Count < 3)
                return true;
            CharacterAnimationBlendSpaceSample origin = samples[0];
            if (origin == null)
                return true;
            for (int i = 1; i < samples.Count - 1; i++)
            {
                if (samples[i] == null)
                    continue;
                Vector2 a = samples[i].Position - origin.Position;
                for (int j = i + 1; j < samples.Count; j++)
                {
                    if (samples[j] == null)
                        continue;
                    Vector2 b = samples[j].Position - origin.Position;
                    if (Math.Abs(a.x * b.y - a.y * b.x) > PositionEpsilon)
                        return false;
                }
            }
            return true;
        }
    }
}
