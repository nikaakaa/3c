using System;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootPlacementRigCalibrationDiagnosticSeverity : byte
    {
        Error = 0
    }

    public enum CharacterFootPlacementRigCalibrationDiagnosticCode : byte
    {
        None = 0,
        DegenerateSoleBaseline = 1,
        ContactGroundMismatch = 2,
        SoleForwardMismatch = 3,
        SoleUpMismatch = 4,
        FlatGroundCorrectionExceeded = 5,
        DegenerateBendPlane = 6,
        PreferredBendCollinear = 7,
        PreferredBendOpposesReference = 8,
        FeetGroundMismatch = 9,
        FeetForwardOpposed = 10,
        SoleHandednessMismatch = 11
    }

    public readonly struct CharacterFootPlacementRigCalibrationDiagnostic
    {
        public CharacterFootPlacementRigCalibrationDiagnostic(
            CharacterFootPlacementRigCalibrationDiagnosticCode code,
            CharacterFootSide side,
            string metric,
            float actual,
            float limit)
        {
            Severity = CharacterFootPlacementRigCalibrationDiagnosticSeverity.Error;
            CalibrationId = string.Empty;
            CalibrationRevision = string.Empty;
            Code = code;
            Side = side;
            Metric = metric ?? string.Empty;
            Actual = actual;
            Limit = limit;
        }

        CharacterFootPlacementRigCalibrationDiagnostic(
            CharacterFootPlacementRigCalibrationDiagnostic source,
            string calibrationId,
            string calibrationRevision)
        {
            Severity = source.Severity;
            CalibrationId = calibrationId ?? string.Empty;
            CalibrationRevision = calibrationRevision ?? string.Empty;
            Code = source.Code;
            Side = source.Side;
            Metric = source.Metric;
            Actual = source.Actual;
            Limit = source.Limit;
        }

        public CharacterFootPlacementRigCalibrationDiagnosticSeverity Severity { get; }
        public string CalibrationId { get; }
        public string CalibrationRevision { get; }
        public CharacterFootPlacementRigCalibrationDiagnosticCode Code { get; }
        public CharacterFootSide Side { get; }
        public string Metric { get; }
        public float Actual { get; }
        public float Limit { get; }

        public override string ToString()
        {
            string side = Side == CharacterFootSide.Left
                ? "Left"
                : Side == CharacterFootSide.Right ? "Right" : "Both";
            string identity = string.IsNullOrEmpty(CalibrationId)
                ? string.Empty
                : $"{CalibrationId}@{CalibrationRevision} ";
            return $"{identity}{side} {Severity}/{Code}: {Metric}={Actual:F5}, limit={Limit:F5}";
        }

        internal CharacterFootPlacementRigCalibrationDiagnostic WithIdentity(string calibrationId, string calibrationRevision) =>
            new CharacterFootPlacementRigCalibrationDiagnostic(this, calibrationId, calibrationRevision);
    }

    public readonly struct CharacterFootPlacementFootRigGeometry
    {
        public CharacterFootPlacementFootRigGeometry(
            Vector3 heelContact,
            Vector3 toeContact,
            Vector3 soleForward,
            Vector3 soleUp,
            Quaternion soleRotation,
            Vector3 referenceBendDirection,
            Vector3 preferredBendDirection,
            float legLength,
            float soleLength,
            float contactGroundError,
            float soleForwardErrorDegrees,
            float soleUpErrorDegrees,
            float flatGroundCorrectionDegrees,
            float bendAxisDot,
            float bendReferenceDot)
        {
            HeelContact = heelContact;
            ToeContact = toeContact;
            SoleForward = soleForward;
            SoleUp = soleUp;
            SoleRotation = soleRotation;
            ReferenceBendDirection = referenceBendDirection;
            PreferredBendDirection = preferredBendDirection;
            LegLength = legLength;
            SoleLength = soleLength;
            ContactGroundError = contactGroundError;
            SoleForwardErrorDegrees = soleForwardErrorDegrees;
            SoleUpErrorDegrees = soleUpErrorDegrees;
            FlatGroundCorrectionDegrees = flatGroundCorrectionDegrees;
            BendAxisDot = bendAxisDot;
            BendReferenceDot = bendReferenceDot;
        }

        public Vector3 HeelContact { get; }
        public Vector3 ToeContact { get; }
        public Vector3 SoleForward { get; }
        public Vector3 SoleUp { get; }
        public Quaternion SoleRotation { get; }
        public Vector3 ReferenceBendDirection { get; }
        public Vector3 PreferredBendDirection { get; }
        public float LegLength { get; }
        public float SoleLength { get; }
        public float ContactGroundError { get; }
        public float SoleForwardErrorDegrees { get; }
        public float SoleUpErrorDegrees { get; }
        public float FlatGroundCorrectionDegrees { get; }
        public float BendAxisDot { get; }
        public float BendReferenceDot { get; }
    }

    public sealed class CharacterFootPlacementRigGeometryReport
    {
        internal CharacterFootPlacementRigGeometryReport(
            CharacterFootPlacementFootRigGeometry left,
            CharacterFootPlacementFootRigGeometry right,
            float referenceGroundHeight,
            CharacterFootPlacementRigCalibrationDiagnostic[] diagnostics)
        {
            Left = left;
            Right = right;
            ReferenceGroundHeight = referenceGroundHeight;
            Diagnostics = diagnostics ?? Array.Empty<CharacterFootPlacementRigCalibrationDiagnostic>();
        }

        public CharacterFootPlacementFootRigGeometry Left { get; }
        public CharacterFootPlacementFootRigGeometry Right { get; }
        public float ReferenceGroundHeight { get; }
        public CharacterFootPlacementRigCalibrationDiagnostic[] Diagnostics { get; }
        public bool IsValid => Diagnostics.Length == 0;

        public string FormatDiagnostics()
        {
            if (IsValid)
                return "Rig Calibration geometry is valid.";
            var builder = new StringBuilder();
            for (int i = 0; i < Diagnostics.Length; i++)
            {
                if (i > 0)
                    builder.AppendLine();
                builder.Append(Diagnostics[i]);
            }
            return builder.ToString();
        }
    }

    public static class CharacterFootPlacementRigGeometryValidator
    {
        const float MinimumSoleLengthRatio = 0.03f;
        const float MaximumContactGroundErrorRatio = 0.04f;
        const float MaximumFeetGroundErrorRatio = 0.05f;
        const float MaximumSoleForwardErrorDegrees = 15f;
        const float MaximumSoleUpErrorDegrees = 15f;
        const float MaximumFlatGroundCorrectionDegrees = 20f;
        const float MaximumBendAxisDot = 0.95f;
        const float MinimumBendReferenceDot = 0.25f;
        const float MinimumFeetForwardDot = -0.25f;

        readonly struct FootGeometryInput
        {
            internal FootGeometryInput(
                Vector3 hip,
                Vector3 knee,
                Vector3 ankle,
                Vector3 heelContact,
                Vector3 toeContact,
                Quaternion soleRotation,
                Vector3 preferredBend,
                float legLength)
            {
                Hip = hip;
                Knee = knee;
                Ankle = ankle;
                HeelContact = heelContact;
                ToeContact = toeContact;
                SoleRotation = soleRotation;
                PreferredBend = preferredBend;
                LegLength = legLength;
            }

            internal Vector3 Hip { get; }
            internal Vector3 Knee { get; }
            internal Vector3 Ankle { get; }
            internal Vector3 HeelContact { get; }
            internal Vector3 ToeContact { get; }
            internal Quaternion SoleRotation { get; }
            internal Vector3 PreferredBend { get; }
            internal float LegLength { get; }
        }

        public static CharacterFootPlacementRigGeometryReport Evaluate(CharacterFootPlacementPoseRig rig)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            CharacterComponentBonePose[] referencePose =
                BuildReferenceComponentPose(rig.Rig);
            return Evaluate(
                rig.CalibrationId.Value,
                rig.CalibrationRevision,
                rig.VisualRoot.up,
                BuildReferenceFootInput(
                    rig,
                    referencePose,
                    rig.Rig.LeftLeg,
                    rig.Calibration.Left),
                BuildReferenceFootInput(
                    rig,
                    referencePose,
                    rig.Rig.RightLeg,
                    rig.Calibration.Right));
        }

        public static CharacterFootPlacementRigGeometryReport Evaluate(
            CharacterFootPlacementPoseRig rig,
            CharacterFootPlacementFootCalibration leftCalibration,
            CharacterFootPlacementFootCalibration rightCalibration)
        {
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            Require(rig.VisualRoot, nameof(rig.VisualRoot));
            Require(rig.LeftHip, nameof(rig.LeftHip));
            Require(rig.LeftKnee, nameof(rig.LeftKnee));
            Require(rig.LeftAnkle, nameof(rig.LeftAnkle));
            Require(rig.LeftToe, nameof(rig.LeftToe));
            Require(rig.RightHip, nameof(rig.RightHip));
            Require(rig.RightKnee, nameof(rig.RightKnee));
            Require(rig.RightAnkle, nameof(rig.RightAnkle));
            Require(rig.RightToe, nameof(rig.RightToe));
            return Evaluate(
                rig.Calibration.CalibrationId.Value,
                rig.Calibration.ContentRevision,
                rig.VisualRoot.up,
                BuildLiveFootInput(
                    rig.VisualRoot,
                    rig.LeftHip,
                    rig.LeftKnee,
                    rig.LeftAnkle,
                    rig.LeftToe,
                    leftCalibration),
                BuildLiveFootInput(
                    rig.VisualRoot,
                    rig.RightHip,
                    rig.RightKnee,
                    rig.RightAnkle,
                    rig.RightToe,
                    rightCalibration));
        }

        static CharacterFootPlacementRigGeometryReport Evaluate(
            string calibrationId,
            string calibrationRevision,
            Vector3 up,
            in FootGeometryInput leftInput,
            in FootGeometryInput rightInput)
        {
            CharacterFootPlacementFootRigGeometry left = EvaluateFoot(
                CharacterFootSide.Left,
                up,
                in leftInput,
                out CharacterFootPlacementRigCalibrationDiagnostic[] leftDiagnostics);
            CharacterFootPlacementFootRigGeometry right = EvaluateFoot(
                CharacterFootSide.Right,
                up,
                in rightInput,
                out CharacterFootPlacementRigCalibrationDiagnostic[] rightDiagnostics);

            var diagnostics = new CharacterFootPlacementRigCalibrationDiagnostic[
                leftDiagnostics.Length + rightDiagnostics.Length + 3];
            int count = 0;
            Copy(leftDiagnostics, diagnostics, ref count);
            Copy(rightDiagnostics, diagnostics, ref count);
            float maximumLegLength = Mathf.Max(left.LegLength, right.LegLength);
            float leftGround = (Vector3.Dot(left.HeelContact, up) +
                                Vector3.Dot(left.ToeContact, up)) * 0.5f;
            float rightGround = (Vector3.Dot(right.HeelContact, up) +
                                 Vector3.Dot(right.ToeContact, up)) * 0.5f;
            float feetGroundError = Mathf.Abs(leftGround - rightGround);
            float feetGroundLimit = maximumLegLength * MaximumFeetGroundErrorRatio;
            if (feetGroundError > feetGroundLimit)
            {
                diagnostics[count++] = new CharacterFootPlacementRigCalibrationDiagnostic(
                    CharacterFootPlacementRigCalibrationDiagnosticCode.FeetGroundMismatch,
                    0,
                    "reference-ground-error",
                    feetGroundError,
                    feetGroundLimit);
            }
            float feetForwardDot = Vector3.Dot(left.SoleForward, right.SoleForward);
            if (feetForwardDot < MinimumFeetForwardDot)
            {
                diagnostics[count++] = new CharacterFootPlacementRigCalibrationDiagnostic(
                    CharacterFootPlacementRigCalibrationDiagnosticCode.FeetForwardOpposed,
                    0,
                    "sole-forward-dot",
                    feetForwardDot,
                    MinimumFeetForwardDot);
            }
            float leftHandedness = Vector3.Dot(
                Vector3.Cross(left.SoleRotation * Vector3.right, left.SoleUp),
                left.SoleForward);
            float rightHandedness = Vector3.Dot(
                Vector3.Cross(right.SoleRotation * Vector3.right, right.SoleUp),
                right.SoleForward);
            if (leftHandedness <= 0f || rightHandedness <= 0f || leftHandedness * rightHandedness <= 0f)
            {
                diagnostics[count++] = new CharacterFootPlacementRigCalibrationDiagnostic(
                    CharacterFootPlacementRigCalibrationDiagnosticCode.SoleHandednessMismatch,
                    0,
                    "sole-frame-handedness-product",
                    leftHandedness * rightHandedness,
                    0f);
            }
            if (count != diagnostics.Length)
                Array.Resize(ref diagnostics, count);
            for (int i = 0; i < diagnostics.Length; i++)
                diagnostics[i] = diagnostics[i].WithIdentity(calibrationId, calibrationRevision);
            return new CharacterFootPlacementRigGeometryReport(
                left,
                right,
                (leftGround + rightGround) * 0.5f,
                diagnostics);
        }

        public static CharacterFootPlacementRigGeometryReport RequireValid(CharacterFootPlacementPoseRig rig)
        {
            CharacterFootPlacementRigGeometryReport report = Evaluate(rig);
            if (!report.IsValid)
            {
                throw new InvalidOperationException(
                    $"Foot Placement Rig Calibration '{rig.CalibrationId}@{rig.CalibrationRevision}' is geometrically invalid.\n{report.FormatDiagnostics()}");
            }
            return report;
        }

        static CharacterFootPlacementFootRigGeometry EvaluateFoot(
            CharacterFootSide side,
            Vector3 up,
            in FootGeometryInput input,
            out CharacterFootPlacementRigCalibrationDiagnostic[] diagnostics)
        {
            var values = new CharacterFootPlacementRigCalibrationDiagnostic[8];
            int count = 0;
            Vector3 heel = input.HeelContact;
            Vector3 toeContact = input.ToeContact;
            Quaternion soleRotation = input.SoleRotation;
            Vector3 soleForward = soleRotation * Vector3.forward;
            Vector3 soleUp = soleRotation * Vector3.up;
            Vector3 baseline = Vector3.ProjectOnPlane(toeContact - heel, up);
            float soleLength = baseline.magnitude;
            float minimumSoleLength = input.LegLength * MinimumSoleLengthRatio;
            Vector3 baselineForward = soleLength > 0.000001f ? baseline / soleLength : Vector3.zero;
            if (soleLength < minimumSoleLength)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.DegenerateSoleBaseline, side, "sole-length", soleLength, minimumSoleLength);
            float contactGroundError = Mathf.Abs(Vector3.Dot(toeContact - heel, up));
            float contactGroundLimit = input.LegLength * MaximumContactGroundErrorRatio;
            if (contactGroundError > contactGroundLimit)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.ContactGroundMismatch, side, "heel-toe-ground-error", contactGroundError, contactGroundLimit);
            float forwardError = soleLength > 0.000001f ? Vector3.Angle(soleForward, baselineForward) : 180f;
            if (forwardError > MaximumSoleForwardErrorDegrees)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.SoleForwardMismatch, side, "sole-forward-angle", forwardError, MaximumSoleForwardErrorDegrees);
            float upError = Vector3.Angle(soleUp, up);
            if (upError > MaximumSoleUpErrorDegrees)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.SoleUpMismatch, side, "sole-up-angle", upError, MaximumSoleUpErrorDegrees);
            Quaternion flatRotation = soleLength > 0.000001f
                ? Quaternion.LookRotation(baselineForward, up)
                : soleRotation;
            float flatCorrection = Quaternion.Angle(soleRotation, flatRotation);
            if (flatCorrection > MaximumFlatGroundCorrectionDegrees)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.FlatGroundCorrectionExceeded, side, "flat-ground-correction", flatCorrection, MaximumFlatGroundCorrectionDegrees);

            Vector3 legAxis = input.Ankle - input.Hip;
            Vector3 referenceBend = Vector3.zero;
            if (legAxis.sqrMagnitude > 0.000001f)
            {
                Vector3 projectedKnee = input.Hip +
                                        Vector3.Project(
                                            input.Knee - input.Hip,
                                            legAxis);
                referenceBend = input.Knee - projectedKnee;
            }
            if (referenceBend.sqrMagnitude <= 0.000001f)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.DegenerateBendPlane, side, "reference-bend-length", referenceBend.magnitude, 0.001f);
            else
                referenceBend.Normalize();
            Vector3 preferredBend = input.PreferredBend;
            float bendAxisDot = legAxis.sqrMagnitude > 0.000001f
                ? Mathf.Abs(Vector3.Dot(preferredBend, legAxis.normalized))
                : 1f;
            if (bendAxisDot >= MaximumBendAxisDot)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.PreferredBendCollinear, side, "preferred-bend-axis-dot", bendAxisDot, MaximumBendAxisDot);
            float bendReferenceDot = referenceBend.sqrMagnitude > 0f
                ? Vector3.Dot(preferredBend, referenceBend)
                : -1f;
            if (bendReferenceDot < MinimumBendReferenceDot)
                Add(values, ref count, CharacterFootPlacementRigCalibrationDiagnosticCode.PreferredBendOpposesReference, side, "preferred-reference-dot", bendReferenceDot, MinimumBendReferenceDot);
            Array.Resize(ref values, count);
            diagnostics = values;
            return new CharacterFootPlacementFootRigGeometry(
                heel,
                toeContact,
                soleForward,
                soleUp,
                soleRotation,
                referenceBend,
                preferredBend,
                input.LegLength,
                soleLength,
                contactGroundError,
                forwardError,
                upError,
                flatCorrection,
                bendAxisDot,
                bendReferenceDot);
        }

        static CharacterComponentBonePose[] BuildReferenceComponentPose(
            CharacterAnimationRigPayload rig)
        {
            var result = new CharacterComponentBonePose[rig.PhysicalBoneCount];
            for (int index = 0; index < result.Length; index++)
            {
                AnimationLocalBonePose local = rig.GetReferenceLocalPose(index);
                int parent = rig.PhysicalBones[index].ParentPhysicalIndex;
                if (parent < 0)
                {
                    result[index] = new CharacterComponentBonePose(
                        local.Position,
                        local.Rotation,
                        local.Scale);
                    continue;
                }
                if (!CharacterPoseConstraintMath.TryCreateComponent(
                        local,
                        result[parent],
                        out CharacterComponentBonePose component))
                {
                    throw new InvalidOperationException(
                        $"Compiled Animation Rig reference Bone #{index} cannot be converted to Component Pose.");
                }
                result[index] = component;
            }
            return result;
        }

        static FootGeometryInput BuildReferenceFootInput(
            CharacterFootPlacementPoseRig rig,
            CharacterComponentBonePose[] pose,
            CharacterAnimationLegChainPayload leg,
            CharacterFootPlacementFootCalibration calibration)
        {
            CharacterComponentBonePose hip = pose[leg.HipPhysicalBoneIndex];
            CharacterComponentBonePose knee = pose[leg.KneePhysicalBoneIndex];
            CharacterComponentBonePose ankle = pose[leg.AnklePhysicalBoneIndex];
            CharacterComponentBonePose toe = pose[leg.ToePhysicalBoneIndex];
            Vector3 hipPosition = rig.PoseRoot.TransformPoint(hip.Position);
            Vector3 kneePosition = rig.PoseRoot.TransformPoint(knee.Position);
            Vector3 anklePosition = rig.PoseRoot.TransformPoint(ankle.Position);
            return new FootGeometryInput(
                hipPosition,
                kneePosition,
                anklePosition,
                TransformPoint(
                    rig.PoseRoot,
                    in ankle,
                    calibration.HeelContactLocalOffset),
                TransformPoint(
                    rig.PoseRoot,
                    in toe,
                    calibration.ToeContactLocalOffset),
                rig.PoseRoot.rotation *
                ankle.Rotation *
                calibration.SoleFrameLocalRotation,
                rig.VisualRoot.TransformDirection(
                    calibration.PreferredBendVisualRootLocalDirection).normalized,
                Vector3.Distance(hipPosition, kneePosition) +
                Vector3.Distance(kneePosition, anklePosition));
        }

        static FootGeometryInput BuildLiveFootInput(
            Transform visualRoot,
            Transform hip,
            Transform knee,
            Transform ankle,
            Transform toe,
            CharacterFootPlacementFootCalibration calibration)
        {
            return new FootGeometryInput(
                hip.position,
                knee.position,
                ankle.position,
                ankle.TransformPoint(calibration.HeelContactLocalOffset),
                toe.TransformPoint(calibration.ToeContactLocalOffset),
                ankle.rotation * calibration.SoleFrameLocalRotation,
                visualRoot.TransformDirection(
                    calibration.PreferredBendVisualRootLocalDirection).normalized,
                Vector3.Distance(hip.position, knee.position) +
                Vector3.Distance(knee.position, ankle.position));
        }

        static Vector3 TransformPoint(
            Transform root,
            in CharacterComponentBonePose bone,
            Vector3 localOffset) =>
            root.TransformPoint(
                bone.Position +
                bone.Rotation * Vector3.Scale(
                    bone.Scale,
                    localOffset));

        static void Add(
            CharacterFootPlacementRigCalibrationDiagnostic[] values,
            ref int count,
            CharacterFootPlacementRigCalibrationDiagnosticCode code,
            CharacterFootSide side,
            string metric,
            float actual,
            float limit)
        {
            values[count++] = new CharacterFootPlacementRigCalibrationDiagnostic(code, side, metric, actual, limit);
        }

        static void Copy(
            CharacterFootPlacementRigCalibrationDiagnostic[] source,
            CharacterFootPlacementRigCalibrationDiagnostic[] destination,
            ref int count)
        {
            for (int i = 0; i < source.Length; i++)
                destination[count++] = source[i];
        }

        static void Require(Transform value, string field)
        {
            if (!value)
                throw new InvalidOperationException($"Foot Placement rig requires '{field}'.");
        }
    }
}
