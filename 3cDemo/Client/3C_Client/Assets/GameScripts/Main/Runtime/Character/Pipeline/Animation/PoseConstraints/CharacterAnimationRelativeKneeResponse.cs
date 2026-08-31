using System;
using RootMotion.FinalIK;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal struct CharacterKneeAngleResponseHistory
    {
        internal bool HasMotionReference;
        internal Vector3 RootPosition;
        internal float ForwardSpeed;
        internal float DownSpeed;
        internal float LeftExtraAngle;
        internal float RightExtraAngle;
    }

    internal readonly struct CharacterKneeAngleResponseFrame
    {
        internal CharacterKneeAngleResponseFrame(
            float deltaSeconds,
            Vector3 rootPosition,
            Quaternion rootRotation,
            Matrix4x4 componentToWorld)
        {
            if (!float.IsFinite(deltaSeconds) || deltaSeconds < 0f ||
                !float.IsFinite(rootPosition.x) || !float.IsFinite(rootPosition.y) ||
                !float.IsFinite(rootPosition.z) || !float.IsFinite(rootRotation.x) ||
                !float.IsFinite(rootRotation.y) || !float.IsFinite(rootRotation.z) ||
                !float.IsFinite(rootRotation.w))
                throw new ArgumentException("Knee angle response frame is invalid.");
            for (int i = 0; i < 16; i++)
                if (!float.IsFinite(componentToWorld[i]))
                    throw new ArgumentException("Knee angle response PoseRoot matrix is invalid.");
            DeltaSeconds = deltaSeconds;
            RootPosition = rootPosition;
            Forward = rootRotation * Vector3.forward;
            ComponentToWorld = componentToWorld;
        }

        internal float DeltaSeconds { get; }
        internal Vector3 RootPosition { get; }
        internal Vector3 Forward { get; }
        internal Matrix4x4 ComponentToWorld { get; }
    }

    public readonly struct CharacterKneeAngleResponseDiagnostics
    {
        internal CharacterKneeAngleResponseDiagnostics(
            bool historyAvailable,
            bool motionSampleAvailable,
            Vector3 rootPosition,
            Vector3 rootForward,
            Vector3 velocity,
            float previousForwardSpeed,
            float previousDownSpeed,
            float forwardSpeed,
            float downSpeed,
            float downStairWeight,
            float upstairRate,
            float downstairRate,
            float rate,
            float maxStep,
            float animationAngle,
            float inputAngle,
            float desiredExtra,
            float previousExtra,
            float currentExtra,
            float outputAngle,
            Vector3 outputHip,
            Vector3 outputKnee,
            Vector3 outputAnkle,
            Vector3 footDisplacement,
            float footRotationErrorDegrees)
        {
            Evaluated = true;
            HistoryAvailable = historyAvailable;
            MotionSampleAvailable = motionSampleAvailable;
            RootPosition = rootPosition;
            RootForward = rootForward;
            Velocity = velocity;
            PreviousForwardSpeed = previousForwardSpeed;
            PreviousDownSpeed = previousDownSpeed;
            ForwardSpeed = forwardSpeed;
            DownSpeed = downSpeed;
            DownStairWeight = downStairWeight;
            UpstairRate = upstairRate;
            DownstairRate = downstairRate;
            Rate = rate;
            MaximumStep = maxStep;
            AnimationAngle = animationAngle;
            InputAngle = inputAngle;
            DesiredExtraAngle = desiredExtra;
            PreviousExtraAngle = previousExtra;
            CurrentExtraAngle = currentExtra;
            OutputAngle = outputAngle;
            OutputHip = outputHip;
            OutputKnee = outputKnee;
            OutputAnkle = outputAnkle;
            FootDisplacement = footDisplacement;
            FootRotationErrorDegrees = footRotationErrorDegrees;
        }

        public bool Evaluated { get; }
        public bool HistoryAvailable { get; }
        public bool MotionSampleAvailable { get; }
        public Vector3 RootPosition { get; }
        public Vector3 RootForward { get; }
        public Vector3 Velocity { get; }
        public float PreviousForwardSpeed { get; }
        public float PreviousDownSpeed { get; }
        public float ForwardSpeed { get; }
        public float DownSpeed { get; }
        public float DownStairWeight { get; }
        public float UpstairRate { get; }
        public float DownstairRate { get; }
        public float Rate { get; }
        public float MaximumStep { get; }
        public float AnimationAngle { get; }
        public float InputAngle { get; }
        public float DesiredExtraAngle { get; }
        public float PreviousExtraAngle { get; }
        public float CurrentExtraAngle { get; }
        public float AppliedExtraAngle => CurrentExtraAngle - PreviousExtraAngle;
        public float CompensationAngle => CurrentExtraAngle - DesiredExtraAngle;
        public float OutputAngle { get; }
        public Vector3 OutputHip { get; }
        public Vector3 OutputKnee { get; }
        public Vector3 OutputAnkle { get; }
        public Vector3 FootDisplacement { get; }
        public float FootRotationErrorDegrees { get; }
    }

    internal sealed class CharacterAnimationRelativeKneeResponse
    {
        readonly struct Leg
        {
            internal Leg(
                CharacterFinalIkPoseBufferBackend backend,
                CharacterAnimationLegChainPayload descriptor)
            {
                Hip = new IndexedBoneHandle(descriptor.HipPhysicalBoneIndex);
                Knee = new IndexedBoneHandle(descriptor.KneePhysicalBoneIndex);
                Ankle = new IndexedBoneHandle(descriptor.AnklePhysicalBoneIndex);
                Vector3 hip = backend.GetReferenceComponentPosition(Hip);
                Vector3 knee = backend.GetReferenceComponentPosition(Knee);
                Vector3 ankle = backend.GetReferenceComponentPosition(Ankle);
                Vector3 normal = Vector3.Cross(knee - hip, ankle - knee);
                if (normal.sqrMagnitude <= 1e-12f)
                    throw new InvalidOperationException("Knee angle response requires a non-degenerate Rig reference bend.");
                normal.Normalize();
                HipAxis = Quaternion.Inverse(backend.GetReferenceComponentRotation(Hip)) * normal;
                KneeAxis = Quaternion.Inverse(backend.GetReferenceComponentRotation(Knee)) * normal;
            }

            internal IndexedBoneHandle Hip { get; }
            internal IndexedBoneHandle Knee { get; }
            internal IndexedBoneHandle Ankle { get; }
            internal Vector3 HipAxis { get; }
            internal Vector3 KneeAxis { get; }
        }

        readonly CharacterFinalIkPoseBufferBackend m_Backend;
        readonly CharacterFullBodyIkProfile m_Profile;
        readonly Leg m_Left;
        readonly Leg m_Right;

        internal CharacterAnimationRelativeKneeResponse(
            CharacterFinalIkPoseBufferBackend backend,
            CharacterAnimationRigPayload rig,
            CharacterFullBodyIkProfile profile)
        {
            m_Backend = backend;
            m_Profile = profile;
            m_Left = new Leg(backend, rig.LeftLeg);
            m_Right = new Leg(backend, rig.RightLeg);
        }

        internal bool Enabled => m_Profile.KneeAngleResponsePolicy == CharacterKneeAngleResponsePolicy.Forced;

        internal void CaptureAnimation(out float leftAngle, out float rightAngle)
        {
            leftAngle = ReadComponentAngle(in m_Left);
            rightAngle = ReadComponentAngle(in m_Right);
        }

        internal void Apply(
            in CharacterKneeAngleResponseFrame frame,
            float leftAnimationAngle,
            float rightAnimationAngle,
            ref CharacterKneeAngleResponseHistory history,
            bool recordDiagnostics,
            out CharacterKneeAngleResponseDiagnostics left,
            out CharacterKneeAngleResponseDiagnostics right)
        {
            bool hadHistory = history.HasMotionReference;
            bool motionAvailable = hadHistory && frame.DeltaSeconds > 0f;
            float previousForward = history.ForwardSpeed;
            float previousDown = history.DownSpeed;
            Vector3 velocity = Vector3.zero;
            if (motionAvailable)
            {
                velocity = (frame.RootPosition - history.RootPosition) / frame.DeltaSeconds;
                history.ForwardSpeed += (Vector3.Dot(velocity, frame.Forward) - history.ForwardSpeed) * 0.25f;
                history.DownSpeed += (Vector3.Dot(velocity, Vector3.down) - history.DownSpeed) * 0.25f;
            }
            if (!hadHistory || frame.DeltaSeconds > 0f)
            {
                history.RootPosition = frame.RootPosition;
                history.HasMotionReference = true;
            }
            float downWeight = history.ForwardSpeed == 0f
                ? (history.DownSpeed > 0f ? 1f : 0f)
                : Mathf.Clamp01(3f * history.DownSpeed / history.ForwardSpeed);
            float rate = Mathf.Lerp(m_Profile.KneeAngleUpstairRate, m_Profile.KneeAngleDownstairRate, downWeight);
            float maxStep = rate * frame.DeltaSeconds;
            left = ApplyLeg(
                in m_Left, in frame, leftAnimationAngle, ref history.LeftExtraAngle,
                hadHistory, motionAvailable, velocity, previousForward, previousDown,
                history.ForwardSpeed, history.DownSpeed, downWeight, rate, maxStep, recordDiagnostics);
            right = ApplyLeg(
                in m_Right, in frame, rightAnimationAngle, ref history.RightExtraAngle,
                hadHistory, motionAvailable, velocity, previousForward, previousDown,
                history.ForwardSpeed, history.DownSpeed, downWeight, rate, maxStep, recordDiagnostics);
        }

        CharacterKneeAngleResponseDiagnostics ApplyLeg(
            in Leg leg,
            in CharacterKneeAngleResponseFrame frame,
            float animationAngle,
            ref float history,
            bool hadHistory,
            bool motionAvailable,
            Vector3 velocity,
            float previousForward,
            float previousDown,
            float forward,
            float down,
            float downWeight,
            float rate,
            float maxStep,
            bool recordDiagnostics)
        {
            Vector3 inputAnkle = recordDiagnostics ? m_Backend.GetComponentPosition(leg.Ankle) : default;
            float inputAngle = ReadWorldAngle(in leg, frame.ComponentToWorld);
            float desired = inputAngle - animationAngle;
            float previous = history;
            float current = Mathf.MoveTowards(previous, desired, maxStep);
            float compensation = current - desired;
            Quaternion footRotation = m_Backend.GetComponentRotation(leg.Ankle);
            m_Backend.SetLocalRotation(leg.Hip,
                m_Backend.GetLocalRotation(leg.Hip) *
                Quaternion.AngleAxis(-0.5f * compensation * Mathf.Rad2Deg, leg.HipAxis));
            m_Backend.SetLocalRotation(leg.Knee,
                m_Backend.GetLocalRotation(leg.Knee) *
                Quaternion.AngleAxis(compensation * Mathf.Rad2Deg, leg.KneeAxis));
            m_Backend.SetComponentRotation(leg.Ankle, footRotation);
            history = current;
            if (!recordDiagnostics)
                return default;
            Vector3 outputHip = m_Backend.GetComponentPosition(leg.Hip);
            Vector3 outputKnee = m_Backend.GetComponentPosition(leg.Knee);
            Vector3 outputAnkle = m_Backend.GetComponentPosition(leg.Ankle);
            return new CharacterKneeAngleResponseDiagnostics(
                hadHistory, motionAvailable, frame.RootPosition, frame.Forward, velocity,
                previousForward, previousDown, forward, down, downWeight,
                m_Profile.KneeAngleUpstairRate, m_Profile.KneeAngleDownstairRate, rate, maxStep,
                animationAngle, inputAngle, desired, previous, current,
                ReadWorldAngle(in leg, frame.ComponentToWorld), outputHip, outputKnee, outputAnkle,
                frame.ComponentToWorld.MultiplyVector(outputAnkle - inputAnkle),
                Quaternion.Angle(footRotation, m_Backend.GetComponentRotation(leg.Ankle)));
        }

        float ReadComponentAngle(in Leg leg) => Angle(
            m_Backend.GetComponentPosition(leg.Hip),
            m_Backend.GetComponentPosition(leg.Knee),
            m_Backend.GetComponentPosition(leg.Ankle));

        float ReadWorldAngle(in Leg leg, Matrix4x4 matrix) => Angle(
            matrix.MultiplyPoint3x4(m_Backend.GetComponentPosition(leg.Hip)),
            matrix.MultiplyPoint3x4(m_Backend.GetComponentPosition(leg.Knee)),
            matrix.MultiplyPoint3x4(m_Backend.GetComponentPosition(leg.Ankle)));

        static float Angle(Vector3 hip, Vector3 knee, Vector3 ankle)
        {
            Vector3 upper = knee - hip;
            Vector3 lower = ankle - knee;
            float denominator = Mathf.Sqrt(upper.sqrMagnitude * lower.sqrMagnitude);
            if (denominator < 1e-15f)
                throw new InvalidOperationException("Knee angle response received a zero-length leg segment.");
            return Mathf.Acos(Mathf.Clamp(Vector3.Dot(upper, lower) / denominator, -1f, 1f));
        }
    }
}
