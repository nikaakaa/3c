using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterFootPlacementRig : MonoBehaviour
    {
        [SerializeField] CharacterFootPlacementRigCalibration m_Calibration;
        [SerializeField] Transform m_VisualRoot;
        [SerializeField] Transform m_Pelvis;
        [SerializeField] Transform m_LeftHip;
        [SerializeField] Transform m_LeftKnee;
        [SerializeField] Transform m_LeftAnkle;
        [SerializeField] Transform m_LeftToe;
        [SerializeField] Transform m_RightHip;
        [SerializeField] Transform m_RightKnee;
        [SerializeField] Transform m_RightAnkle;
        [SerializeField] Transform m_RightToe;
        [SerializeField] Transform m_SelfColliderRoot;

        public CharacterFootPlacementRigCalibration Calibration => m_Calibration;
        public Transform VisualRoot => m_VisualRoot;
        public Transform Pelvis => m_Pelvis;
        public Transform SelfColliderRoot => m_SelfColliderRoot;

        public CharacterFootPlacementRigBinding BuildBinding()
        {
            return new CharacterFootPlacementRigBinding(
                m_Calibration,
                m_VisualRoot,
                m_Pelvis,
                m_LeftHip,
                m_LeftKnee,
                m_LeftAnkle,
                m_LeftToe,
                m_RightHip,
                m_RightKnee,
                m_RightAnkle,
                m_RightToe,
                m_SelfColliderRoot);
        }
    }

    public sealed class CharacterFootPlacementRigBinding
    {
        public CharacterFootPlacementRigBinding(
            CharacterFootPlacementRigCalibration calibration,
            Transform visualRoot,
            Transform pelvis,
            Transform leftHip,
            Transform leftKnee,
            Transform leftAnkle,
            Transform leftToe,
            Transform rightHip,
            Transform rightKnee,
            Transform rightAnkle,
            Transform rightToe,
            Transform selfColliderRoot)
        {
            Calibration = calibration;
            VisualRoot = visualRoot;
            Pelvis = pelvis;
            LeftHip = leftHip;
            LeftKnee = leftKnee;
            LeftAnkle = leftAnkle;
            LeftToe = leftToe;
            RightHip = rightHip;
            RightKnee = rightKnee;
            RightAnkle = rightAnkle;
            RightToe = rightToe;
            SelfColliderRoot = selfColliderRoot;
            RequireValid();
            CalibrationId = Calibration.CalibrationId;
            CalibrationRevision = Calibration.ContentRevision;
            CharacterFootPlacementFootCalibration left = Calibration.Left;
            CharacterFootPlacementFootCalibration right = Calibration.Right;
            LeftHeelSoleOffset = left.HeelSoleLocalOffset;
            LeftToeSoleOffset = left.ToeSoleLocalOffset;
            RightHeelSoleOffset = right.HeelSoleLocalOffset;
            RightToeSoleOffset = right.ToeSoleLocalOffset;
            LeftSemanticForwardAxis = left.SemanticForwardLocalAxis;
            LeftSemanticUpAxis = left.SemanticUpLocalAxis;
            RightSemanticForwardAxis = right.SemanticForwardLocalAxis;
            RightSemanticUpAxis = right.SemanticUpLocalAxis;
            LeftKneePoleLocalDirection = left.KneePoleVisualRootLocalDirection;
            RightKneePoleLocalDirection = right.KneePoleVisualRootLocalDirection;
            LeftLegLength = Vector3.Distance(LeftHip.position, LeftKnee.position) +
                            Vector3.Distance(LeftKnee.position, LeftAnkle.position);
            RightLegLength = Vector3.Distance(RightHip.position, RightKnee.position) +
                             Vector3.Distance(RightKnee.position, RightAnkle.position);
            if (!IsFinite(LeftLegLength) || !IsFinite(RightLegLength) ||
                LeftLegLength <= 0.0001f || RightLegLength <= 0.0001f)
                throw new InvalidOperationException("Foot Placement rig leg lengths are degenerate.");
        }

        public CharacterFootPlacementRigCalibration Calibration { get; }
        public CharacterFootPlacementRigCalibrationId CalibrationId { get; }
        public string CalibrationRevision { get; }
        public Transform VisualRoot { get; }
        public Transform Pelvis { get; }
        public Transform LeftHip { get; }
        public Transform LeftKnee { get; }
        public Transform LeftAnkle { get; }
        public Transform LeftToe { get; }
        public Transform RightHip { get; }
        public Transform RightKnee { get; }
        public Transform RightAnkle { get; }
        public Transform RightToe { get; }
        public Vector3 LeftHeelSoleOffset { get; }
        public Vector3 LeftToeSoleOffset { get; }
        public Vector3 RightHeelSoleOffset { get; }
        public Vector3 RightToeSoleOffset { get; }
        public Vector3 LeftSemanticForwardAxis { get; }
        public Vector3 LeftSemanticUpAxis { get; }
        public Vector3 RightSemanticForwardAxis { get; }
        public Vector3 RightSemanticUpAxis { get; }
        public Vector3 LeftKneePoleLocalDirection { get; }
        public Vector3 RightKneePoleLocalDirection { get; }
        public Transform SelfColliderRoot { get; }
        public float LeftLegLength { get; }
        public float RightLegLength { get; }
        public int CharacterLayer => SelfColliderRoot.gameObject.layer;

        public Vector3 ResolvePelvisParentLocalVerticalOffset(float componentVerticalOffset)
        {
            Transform parent = Pelvis.parent;
            if (!parent)
                throw new InvalidOperationException("Foot Placement pelvis requires a parent transform.");
            Vector3 offset = parent.InverseTransformVector(VisualRoot.up * componentVerticalOffset);
            if (!IsFinite(offset.x) || !IsFinite(offset.y) || !IsFinite(offset.z))
                throw new InvalidOperationException("Foot Placement pelvis component-space offset is not finite.");
            return offset;
        }

        public CharacterFootPlacementAnimatedPose CaptureAnimatedPose(ulong renderFrame)
        {
            if (renderFrame == 0)
                throw new ArgumentOutOfRangeException(nameof(renderFrame));
            CharacterFootPlacementAnimatedFootPose left = CaptureFoot(
                LeftHip,
                LeftKnee,
                LeftAnkle,
                LeftToe,
                LeftHeelSoleOffset,
                LeftToeSoleOffset,
                LeftSemanticForwardAxis,
                LeftSemanticUpAxis);
            CharacterFootPlacementAnimatedFootPose right = CaptureFoot(
                RightHip,
                RightKnee,
                RightAnkle,
                RightToe,
                RightHeelSoleOffset,
                RightToeSoleOffset,
                RightSemanticForwardAxis,
                RightSemanticUpAxis);
            var pose = new CharacterFootPlacementAnimatedPose(
                renderFrame,
                Pelvis.localPosition,
                left,
                right);
            RequireFinite(pose);
            return pose;
        }

        public bool IsSelfCollider(Collider collider)
        {
            return collider && collider.transform.IsChildOf(SelfColliderRoot);
        }

        public void RequireValid()
        {
            if (!Calibration)
                throw new InvalidOperationException("Foot Placement rig requires a Rig Calibration.");
            Calibration.RequireValid();
            Require(VisualRoot, nameof(VisualRoot));
            Require(Pelvis, nameof(Pelvis));
            Require(LeftHip, nameof(LeftHip));
            Require(LeftKnee, nameof(LeftKnee));
            Require(LeftAnkle, nameof(LeftAnkle));
            Require(LeftToe, nameof(LeftToe));
            Require(RightHip, nameof(RightHip));
            Require(RightKnee, nameof(RightKnee));
            Require(RightAnkle, nameof(RightAnkle));
            Require(RightToe, nameof(RightToe));
            Require(SelfColliderRoot, nameof(SelfColliderRoot));
            RequireDescendant(VisualRoot, SelfColliderRoot, nameof(SelfColliderRoot));
            RequireDescendant(Pelvis, VisualRoot, nameof(Pelvis));
            if (!Pelvis.parent)
                throw new InvalidOperationException("Foot Placement pelvis requires a parent transform.");
            RequireLeg(LeftHip, LeftKnee, LeftAnkle, LeftToe, VisualRoot, "Left");
            RequireLeg(RightHip, RightKnee, RightAnkle, RightToe, VisualRoot, "Right");
            if (LeftHip == RightHip || LeftKnee == RightKnee || LeftAnkle == RightAnkle || LeftToe == RightToe)
                throw new InvalidOperationException("Foot Placement rig left and right chains share a bone.");
            if (Pelvis.IsChildOf(LeftAnkle) || Pelvis.IsChildOf(RightAnkle))
                throw new InvalidOperationException("Foot Placement pelvis cannot be a foot descendant.");
            RequirePole(LeftHip, LeftAnkle, Calibration.Left.KneePoleVisualRootLocalDirection, "Left");
            RequirePole(RightHip, RightAnkle, Calibration.Right.KneePoleVisualRootLocalDirection, "Right");
        }

        static CharacterFootPlacementAnimatedFootPose CaptureFoot(
            Transform hip,
            Transform knee,
            Transform ankle,
            Transform toe,
            Vector3 heelSoleOffset,
            Vector3 toeSoleOffset,
            Vector3 forwardAxis,
            Vector3 upAxis)
        {
            Vector3 soleForward = ankle.TransformDirection(forwardAxis).normalized;
            Vector3 soleUp = ankle.TransformDirection(upAxis).normalized;
            return new CharacterFootPlacementAnimatedFootPose(
                hip.position,
                knee.position,
                ankle.position,
                ankle.rotation,
                toe.TransformPoint(toeSoleOffset),
                toe.rotation,
                ankle.TransformPoint(heelSoleOffset),
                soleForward,
                soleUp,
                Quaternion.LookRotation(soleForward, soleUp));
        }

        static void RequireLeg(
            Transform hip,
            Transform knee,
            Transform ankle,
            Transform toe,
            Transform visualRoot,
            string side)
        {
            RequireDescendant(hip, visualRoot, side + "Hip");
            RequireDescendant(knee, hip, side + "Knee");
            RequireDescendant(ankle, knee, side + "Ankle");
            RequireDescendant(toe, ankle, side + "Toe");
        }

        static void RequireDescendant(Transform value, Transform ancestor, string field)
        {
            if (value != ancestor && !value.IsChildOf(ancestor))
                throw new InvalidOperationException($"Foot Placement rig '{field}' is outside its required hierarchy.");
        }

        static void Require(Transform value, string field)
        {
            if (!value)
                throw new InvalidOperationException($"Foot Placement rig requires '{field}'.");
        }

        void RequirePole(Transform hip, Transform ankle, Vector3 poleLocal, string side)
        {
            Vector3 legLocal = VisualRoot.InverseTransformDirection(ankle.position - hip.position).normalized;
            if (legLocal.sqrMagnitude <= 0.0001f || Mathf.Abs(Vector3.Dot(legLocal, poleLocal)) >= 0.98f)
                throw new InvalidOperationException($"Foot Placement rig '{side}' knee pole is collinear with its leg chain.");
        }

        static void RequireFinite(Vector3 value, string field)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z))
                throw new InvalidOperationException($"Foot Placement rig '{field}' is not finite.");
        }

        static void RequireFinite(CharacterFootPlacementAnimatedPose pose)
        {
            RequireFinite(pose.PelvisLocalPosition, "AnimatedPelvis");
            RequireFinite(pose.Left.HipPosition, "AnimatedLeftHip");
            RequireFinite(pose.Left.KneePosition, "AnimatedLeftKnee");
            RequireFinite(pose.Left.AnklePosition, "AnimatedLeftAnkle");
            RequireFinite(pose.Left.ToePosition, "AnimatedLeftToe");
            RequireFinite(pose.Left.HeelPosition, "AnimatedLeftHeel");
            RequireFinite(pose.Right.HipPosition, "AnimatedRightHip");
            RequireFinite(pose.Right.KneePosition, "AnimatedRightKnee");
            RequireFinite(pose.Right.AnklePosition, "AnimatedRightAnkle");
            RequireFinite(pose.Right.ToePosition, "AnimatedRightToe");
            RequireFinite(pose.Right.HeelPosition, "AnimatedRightHeel");
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
