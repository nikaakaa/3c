using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CharacterFootPlacementRig : MonoBehaviour
    {
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
        [SerializeField] Vector3 m_LeftHeelSoleOffset;
        [SerializeField] Vector3 m_LeftToeSoleOffset;
        [SerializeField] Vector3 m_RightHeelSoleOffset;
        [SerializeField] Vector3 m_RightToeSoleOffset;
        [SerializeField] Vector3 m_LeftFootForwardAxis = Vector3.forward;
        [SerializeField] Vector3 m_RightFootForwardAxis = Vector3.forward;
        [SerializeField] Transform m_SelfColliderRoot;

        public Transform VisualRoot => m_VisualRoot;
        public Transform Pelvis => m_Pelvis;
        public Transform SelfColliderRoot => m_SelfColliderRoot;

        public CharacterFootPlacementRigBinding BuildBinding()
        {
            return new CharacterFootPlacementRigBinding(
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
                m_LeftHeelSoleOffset,
                m_LeftToeSoleOffset,
                m_RightHeelSoleOffset,
                m_RightToeSoleOffset,
                m_LeftFootForwardAxis,
                m_RightFootForwardAxis,
                m_SelfColliderRoot);
        }
    }

    public sealed class CharacterFootPlacementRigBinding
    {
        public CharacterFootPlacementRigBinding(
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
            Vector3 leftHeelSoleOffset,
            Vector3 leftToeSoleOffset,
            Vector3 rightHeelSoleOffset,
            Vector3 rightToeSoleOffset,
            Vector3 leftFootForwardAxis,
            Vector3 rightFootForwardAxis,
            Transform selfColliderRoot)
        {
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
            LeftHeelSoleOffset = leftHeelSoleOffset;
            LeftToeSoleOffset = leftToeSoleOffset;
            RightHeelSoleOffset = rightHeelSoleOffset;
            RightToeSoleOffset = rightToeSoleOffset;
            LeftFootForwardAxis = leftFootForwardAxis;
            RightFootForwardAxis = rightFootForwardAxis;
            SelfColliderRoot = selfColliderRoot;
            RequireValid();
            LeftLegLength = Vector3.Distance(LeftHip.position, LeftKnee.position) +
                            Vector3.Distance(LeftKnee.position, LeftAnkle.position);
            RightLegLength = Vector3.Distance(RightHip.position, RightKnee.position) +
                             Vector3.Distance(RightKnee.position, RightAnkle.position);
            if (!IsFinite(LeftLegLength) || !IsFinite(RightLegLength) ||
                LeftLegLength <= 0.0001f || RightLegLength <= 0.0001f)
                throw new InvalidOperationException("Foot Placement rig leg lengths are degenerate.");
        }

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
        public Vector3 LeftFootForwardAxis { get; }
        public Vector3 RightFootForwardAxis { get; }
        public Transform SelfColliderRoot { get; }
        public float LeftLegLength { get; }
        public float RightLegLength { get; }
        public int CharacterLayer => SelfColliderRoot.gameObject.layer;

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
                LeftFootForwardAxis);
            CharacterFootPlacementAnimatedFootPose right = CaptureFoot(
                RightHip,
                RightKnee,
                RightAnkle,
                RightToe,
                RightHeelSoleOffset,
                RightToeSoleOffset,
                RightFootForwardAxis);
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
            RequireLeg(LeftHip, LeftKnee, LeftAnkle, LeftToe, VisualRoot, "Left");
            RequireLeg(RightHip, RightKnee, RightAnkle, RightToe, VisualRoot, "Right");
            if (LeftHip == RightHip || LeftKnee == RightKnee || LeftAnkle == RightAnkle || LeftToe == RightToe)
                throw new InvalidOperationException("Foot Placement rig left and right chains share a bone.");
            if (Pelvis.IsChildOf(LeftAnkle) || Pelvis.IsChildOf(RightAnkle))
                throw new InvalidOperationException("Foot Placement pelvis cannot be a foot descendant.");
            RequireFinite(LeftHeelSoleOffset, nameof(LeftHeelSoleOffset));
            RequireFinite(LeftToeSoleOffset, nameof(LeftToeSoleOffset));
            RequireFinite(RightHeelSoleOffset, nameof(RightHeelSoleOffset));
            RequireFinite(RightToeSoleOffset, nameof(RightToeSoleOffset));
            RequireAxis(LeftFootForwardAxis, nameof(LeftFootForwardAxis));
            RequireAxis(RightFootForwardAxis, nameof(RightFootForwardAxis));
        }

        static CharacterFootPlacementAnimatedFootPose CaptureFoot(
            Transform hip,
            Transform knee,
            Transform ankle,
            Transform toe,
            Vector3 heelSoleOffset,
            Vector3 toeSoleOffset,
            Vector3 forwardAxis)
        {
            return new CharacterFootPlacementAnimatedFootPose(
                hip.position,
                knee.position,
                ankle.position,
                ankle.rotation,
                toe.TransformPoint(toeSoleOffset),
                toe.rotation,
                ankle.TransformPoint(heelSoleOffset),
                ankle.TransformDirection(forwardAxis).normalized);
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

        static void RequireAxis(Vector3 value, string field)
        {
            RequireFinite(value, field);
            if (value.sqrMagnitude <= 0.0001f)
                throw new InvalidOperationException($"Foot Placement rig '{field}' is degenerate.");
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
