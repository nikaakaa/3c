using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public readonly struct CharacterFinalIkGroundingSettings
    {
        public CharacterFinalIkGroundingSettings(
            Grounding.Quality quality,
            int groundLayerMask,
            float maximumStep,
            float heightOffset,
            float footHeightSpeed,
            float footRadius,
            float velocityPrediction,
            float footRotationWeight,
            float footRotationSpeed,
            float maximumFootRotationAngle,
            bool rotateSolver,
            float rootCastRadius,
            bool overstepFallsDown)
        {
            Quality = quality;
            GroundLayerMask = groundLayerMask;
            MaximumStep = maximumStep;
            HeightOffset = heightOffset;
            FootHeightSpeed = footHeightSpeed;
            FootRadius = footRadius;
            VelocityPrediction = velocityPrediction;
            FootRotationWeight = footRotationWeight;
            FootRotationSpeed = footRotationSpeed;
            MaximumFootRotationAngle = maximumFootRotationAngle;
            RotateSolver = rotateSolver;
            RootCastRadius = rootCastRadius;
            OverstepFallsDown = overstepFallsDown;
            RequireValid();
        }

        public Grounding.Quality Quality { get; }
        public int GroundLayerMask { get; }
        public float MaximumStep { get; }
        public float HeightOffset { get; }
        public float FootHeightSpeed { get; }
        public float FootRadius { get; }
        public float VelocityPrediction { get; }
        public float FootRotationWeight { get; }
        public float FootRotationSpeed { get; }
        public float MaximumFootRotationAngle { get; }
        public bool RotateSolver { get; }
        public float RootCastRadius { get; }
        public bool OverstepFallsDown { get; }

        public void RequireValid()
        {
            if (!Enum.IsDefined(typeof(Grounding.Quality), Quality) || GroundLayerMask == 0)
                throw new InvalidOperationException("FinalIK Grounding identity or LayerMask is invalid.");
            RequirePositive(MaximumStep, nameof(MaximumStep));
            RequireFinite(HeightOffset, nameof(HeightOffset));
            RequirePositive(FootHeightSpeed, nameof(FootHeightSpeed));
            RequirePositive(FootRadius, nameof(FootRadius));
            RequireNonNegative(VelocityPrediction, nameof(VelocityPrediction));
            RequireRange(FootRotationWeight, 0f, 1f, nameof(FootRotationWeight));
            RequirePositive(FootRotationSpeed, nameof(FootRotationSpeed));
            RequireRange(MaximumFootRotationAngle, 0f, 90f, nameof(MaximumFootRotationAngle));
            RequirePositive(RootCastRadius, nameof(RootCastRadius));
        }

        static void RequireFinite(float value, string field)
        {
            if (!float.IsFinite(value))
                throw new InvalidOperationException($"FinalIK Grounding {field} is not finite.");
        }

        static void RequirePositive(float value, string field)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"FinalIK Grounding {field} must be finite and positive.");
        }

        static void RequireNonNegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"FinalIK Grounding {field} must be finite and non-negative.");
        }

        static void RequireRange(float value, float minimum, float maximum, string field)
        {
            if (!float.IsFinite(value) || value < minimum || value > maximum)
                throw new InvalidOperationException($"FinalIK Grounding {field} is outside [{minimum}, {maximum}].");
        }
    }

    public readonly struct CharacterFinalIkGroundingFootResult
    {
        public CharacterFinalIkGroundingFootResult(
            Vector3 componentPosition,
            Quaternion componentRotation,
            float verticalOffset,
            bool grounded,
            Vector3 velocity,
            GroundingQueryHit heelHit,
            GroundingQueryHit toeHit,
            GroundingQueryHit footCenterHit,
            GroundingQueryHit currentGroundingHit)
        {
            ComponentPosition = componentPosition;
            ComponentRotation = componentRotation;
            VerticalOffset = verticalOffset;
            Grounded = grounded;
            Velocity = velocity;
            HeelHit = heelHit;
            ToeHit = toeHit;
            FootCenterHit = footCenterHit;
            CurrentGroundingHit = currentGroundingHit;
        }

        public Vector3 ComponentPosition { get; }
        public Quaternion ComponentRotation { get; }
        public float VerticalOffset { get; }
        public bool Grounded { get; }
        public Vector3 Velocity { get; }
        public GroundingQueryHit HeelHit { get; }
        public GroundingQueryHit ToeHit { get; }
        public GroundingQueryHit FootCenterHit { get; }
        public GroundingQueryHit CurrentGroundingHit { get; }
    }

    public readonly struct CharacterFinalIkGroundingResult
    {
        public CharacterFinalIkGroundingResult(
            CharacterFinalIkGroundingFootResult leftFoot,
            CharacterFinalIkGroundingFootResult rightFoot,
            GroundingQueryHit rootHit,
            bool grounded)
        {
            LeftFoot = leftFoot;
            RightFoot = rightFoot;
            RootHit = rootHit;
            Grounded = grounded;
        }

        public CharacterFinalIkGroundingFootResult LeftFoot { get; }
        public CharacterFinalIkGroundingFootResult RightFoot { get; }
        public GroundingQueryHit RootHit { get; }
        public bool Grounded { get; }
    }

    public sealed class CharacterFinalIkGroundingAdapter
    {
        public const string BackendIdentity = "rootmotion.finalik.grounding/explicit-pose-world-query-secondary-plant/v4";
        public const string AuditedVendorSourceRevision = "76a0fcb8e0472d6ba5d0f2059c154a92e5ed3fa374e30ceccb507e972771d5b1";
        readonly Grounding m_Grounding = new Grounding();
        CharacterFinalIkGroundingSettings m_Settings;

        public CharacterFinalIkGroundingAdapter(CharacterFinalIkGroundingSettings settings)
        {
            settings.RequireValid();
            m_Settings = settings;
            ApplySettings();
            m_Grounding.Initiate(2);
        }

        public CharacterFinalIkGroundingResult Evaluate(
            in GroundingFrameInput frame,
            IGroundingWorldQueryBackend worldQueryBackend,
            in GroundingComponentTransform poseRoot)
        {
            if (frame.FootCount != 2 || frame.LayerMask != m_Settings.GroundLayerMask)
                throw new ArgumentException("FinalIK Grounding frame does not match its formal settings.", nameof(frame));
            m_Grounding.Update(in frame, worldQueryBackend);
            return new CharacterFinalIkGroundingResult(
                BuildFootResult(m_Grounding.legs[0], frame.LeftFoot, poseRoot),
                BuildFootResult(m_Grounding.legs[1], frame.RightFoot, poseRoot),
                m_Grounding.rootQueryHit,
                m_Grounding.isGrounded);
        }

        public void Reset() => m_Grounding.Reset();

        internal void ApplyTuning(CharacterFinalIkGroundingSettings settings)
        {
            settings.RequireValid();
            m_Settings = settings;
            ApplySettings();
        }

        void ApplySettings()
        {
            m_Grounding.quality = m_Settings.Quality;
            m_Grounding.layers = m_Settings.GroundLayerMask;
            m_Grounding.maxStep = m_Settings.MaximumStep;
            m_Grounding.heightOffset = m_Settings.HeightOffset;
            m_Grounding.footSpeed = m_Settings.FootHeightSpeed;
            m_Grounding.footRadius = m_Settings.FootRadius;
            m_Grounding.prediction = m_Settings.VelocityPrediction;
            m_Grounding.footRotationWeight = m_Settings.FootRotationWeight;
            m_Grounding.footRotationSpeed = m_Settings.FootRotationSpeed;
            m_Grounding.maxFootRotationAngle = m_Settings.MaximumFootRotationAngle;
            m_Grounding.rotateSolver = m_Settings.RotateSolver;
            m_Grounding.pelvisSpeed = 1f;
            m_Grounding.pelvisDamper = 0f;
            m_Grounding.lowerPelvisWeight = 0f;
            m_Grounding.liftPelvisWeight = 0f;
            m_Grounding.rootSphereCastRadius = m_Settings.RootCastRadius;
            m_Grounding.overstepFallsDown = m_Settings.OverstepFallsDown;
            m_Grounding.secondaryPlantQuery = true;
        }

        static CharacterFinalIkGroundingFootResult BuildFootResult(
            Grounding.Leg leg,
            GroundingFootInput foot,
            in GroundingComponentTransform poseRoot)
        {
            GroundingQueryHit currentGroundingHit = leg.currentQueryHit;
            if (!currentGroundingHit.HasHit)
            {
                return new CharacterFinalIkGroundingFootResult(
                    ToComponentPoint(poseRoot, foot.Ankle.Position),
                    (Quaternion.Inverse(poseRoot.Rotation) * foot.Ankle.Rotation).normalized,
                    0f,
                    false,
                    leg.velocity,
                    leg.heelQueryHit,
                    leg.toeQueryHit,
                    leg.capsuleQueryHit,
                    currentGroundingHit);
            }
            Quaternion worldRotation = (leg.rotationOffset * foot.Ankle.Rotation).normalized;
            return new CharacterFinalIkGroundingFootResult(
                ToComponentPoint(poseRoot, leg.IKPosition),
                (Quaternion.Inverse(poseRoot.Rotation) * worldRotation).normalized,
                leg.IKOffset,
                leg.isGrounded,
                leg.velocity,
                leg.heelQueryHit,
                leg.toeQueryHit,
                leg.capsuleQueryHit,
                currentGroundingHit);
        }

        static Vector3 ToComponentPoint(in GroundingComponentTransform root, Vector3 point) =>
            Quaternion.Inverse(root.Rotation) * (point - root.Position);

    }
}
