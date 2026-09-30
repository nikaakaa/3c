using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonPerformance.Instrumentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct PresentationFactId : IEquatable<PresentationFactId>
    {
        public PresentationFactId(string value)
        {
            Value = string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Presentation Fact identity is missing.", nameof(value))
                : value.Trim();
        }

        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(PresentationFactId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is PresentationFactId other && Equals(other);

        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PresentationFactId left, PresentationFactId right) => left.Equals(right);
        public static bool operator !=(PresentationFactId left, PresentationFactId right) => !left.Equals(right);
    }

    public enum PresentationFactValueKind : byte
    {
        Bool = 1,
        Float = 2,
        Vector2 = 3,
        Enum = 4,
        UInt64 = 5,
        Identity = 6,
        Vector3 = 7,
        Quaternion = 8
    }

    public enum CharacterPresentationMotionPhase : byte
    {
        GroundedStationary = 1,
        GroundedMoving = 2,
        AirborneRising = 3,
        AirborneFalling = 4
    }

    public enum PresentationFactMissingReason : byte
    {
        None = 0,
        FrameInvalid = 1,
        FactIdInvalid = 2,
        FactNotDeclared = 3,
        ValueKindMismatch = 4
    }

    public readonly struct CharacterPresentationFactDeclaration
    {
        public CharacterPresentationFactDeclaration(
            PresentationFactId factId,
            PresentationFactValueKind valueKind)
        {
            if (!factId.IsValid)
                throw new ArgumentException("Presentation Fact declaration identity is invalid.", nameof(factId));
            if (!Enum.IsDefined(typeof(PresentationFactValueKind), valueKind))
                throw new ArgumentOutOfRangeException(nameof(valueKind));
            FactId = factId;
            ValueKind = valueKind;
        }

        public PresentationFactId FactId { get; }
        public PresentationFactValueKind ValueKind { get; }
    }

    public static class CharacterPresentationFactSchema
    {
        public const string Version = "character-presentation-fact/v6";

        public static readonly PresentationFactId Grounded = new PresentationFactId("presentation.grounded");
        public static readonly PresentationFactId Velocity = new PresentationFactId("presentation.velocity");
        public static readonly PresentationFactId Rotation = new PresentationFactId("presentation.rotation");
        public static readonly PresentationFactId DesiredPlanarVelocity = new PresentationFactId("presentation.desired-planar-velocity");
        public static readonly PresentationFactId DesiredFacing = new PresentationFactId("presentation.desired-facing");
        public static readonly PresentationFactId HasMotion = new PresentationFactId("presentation.has-motion");
        public static readonly PresentationFactId LocomotionPlanarBasis = new PresentationFactId("presentation.locomotion-planar-basis");
        public static readonly PresentationFactId MovementMode = new PresentationFactId("presentation.movement-mode");
        public static readonly PresentationFactId BodyDiscontinuityGeneration = new PresentationFactId("presentation.body-discontinuity-generation");

        static readonly CharacterPresentationFactDeclaration[] s_OrderedDeclarations =
        {
            new CharacterPresentationFactDeclaration(Grounded, PresentationFactValueKind.Bool),
            new CharacterPresentationFactDeclaration(Velocity, PresentationFactValueKind.Vector3),
            new CharacterPresentationFactDeclaration(Rotation, PresentationFactValueKind.Quaternion),
            new CharacterPresentationFactDeclaration(DesiredPlanarVelocity, PresentationFactValueKind.Vector2),
            new CharacterPresentationFactDeclaration(DesiredFacing, PresentationFactValueKind.Vector2),
            new CharacterPresentationFactDeclaration(HasMotion, PresentationFactValueKind.Bool),
            new CharacterPresentationFactDeclaration(LocomotionPlanarBasis, PresentationFactValueKind.Vector2),
            new CharacterPresentationFactDeclaration(MovementMode, PresentationFactValueKind.Identity),
            new CharacterPresentationFactDeclaration(BodyDiscontinuityGeneration, PresentationFactValueKind.UInt64)
        };

        public static IReadOnlyList<CharacterPresentationFactDeclaration> OrderedDeclarations =>
            s_OrderedDeclarations;

        public static PresentationFactValueKind RequireValueKind(PresentationFactId id)
        {
            if (!id.IsValid)
                throw new CharacterPresentationFactMissingException(id, PresentationFactMissingReason.FactIdInvalid);
            for (int i = 0; i < s_OrderedDeclarations.Length; i++)
            {
                CharacterPresentationFactDeclaration declaration = s_OrderedDeclarations[i];
                if (declaration.FactId == id)
                    return declaration.ValueKind;
            }
            throw new CharacterPresentationFactMissingException(id, PresentationFactMissingReason.FactNotDeclared);
        }
    }

    public readonly struct CharacterPresentationFactFrameIdentity : IEquatable<CharacterPresentationFactFrameIdentity>
    {
        public CharacterPresentationFactFrameIdentity(ActorId actorId, ulong renderFrame)
        {
            if (!actorId.IsValid || renderFrame == 0)
                throw new ArgumentException("Presentation Fact frame identity is incomplete.");
            ActorId = actorId;
            RenderFrame = renderFrame;
        }

        public ActorId ActorId { get; }
        public ulong RenderFrame { get; }
        public bool IsValid => ActorId.IsValid && RenderFrame != 0;

        public bool Equals(CharacterPresentationFactFrameIdentity other) =>
            ActorId == other.ActorId && RenderFrame == other.RenderFrame;

        public override bool Equals(object obj) =>
            obj is CharacterPresentationFactFrameIdentity other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (ActorId.GetHashCode() * 397) ^ RenderFrame.GetHashCode();
            }
        }
    }

    public readonly struct CharacterPresentationFactValue
    {
        CharacterPresentationFactValue(
            PresentationFactValueKind kind,
            bool boolValue,
            float floatValue,
            Vector2 vector2Value,
            Vector3 vector3Value,
            Quaternion quaternionValue,
            int enumValue,
            ulong uint64Value,
            string identityValue)
        {
            Kind = kind;
            BoolValue = boolValue;
            FloatValue = floatValue;
            Vector2Value = vector2Value;
            Vector3Value = vector3Value;
            QuaternionValue = quaternionValue;
            EnumValue = enumValue;
            UInt64Value = uint64Value;
            IdentityValue = identityValue ?? string.Empty;
        }

        public PresentationFactValueKind Kind { get; }
        public bool BoolValue { get; }
        public float FloatValue { get; }
        public Vector2 Vector2Value { get; }
        public Vector3 Vector3Value { get; }
        public Quaternion QuaternionValue { get; }
        public int EnumValue { get; }
        public ulong UInt64Value { get; }
        public string IdentityValue { get; }

        public static CharacterPresentationFactValue FromBool(bool value) =>
            new CharacterPresentationFactValue(PresentationFactValueKind.Bool, value, 0f, default, default, default, 0, 0, string.Empty);

        public static CharacterPresentationFactValue FromFloat(float value)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new CharacterPresentationFactValue(PresentationFactValueKind.Float, false, value, default, default, default, 0, 0, string.Empty);
        }

        public static CharacterPresentationFactValue FromVector2(Vector2 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new CharacterPresentationFactValue(PresentationFactValueKind.Vector2, false, 0f, value, default, default, 0, 0, string.Empty);
        }

        public static CharacterPresentationFactValue FromVector3(Vector3 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new CharacterPresentationFactValue(PresentationFactValueKind.Vector3, false, 0f, default, value, default, 0, 0, string.Empty);
        }

        public static CharacterPresentationFactValue FromQuaternion(Quaternion value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new CharacterPresentationFactValue(PresentationFactValueKind.Quaternion, false, 0f, default, default, value, 0, 0, string.Empty);
        }

        public static CharacterPresentationFactValue FromUInt64(ulong value) =>
            new CharacterPresentationFactValue(PresentationFactValueKind.UInt64, false, 0f, default, default, default, 0, value, string.Empty);

        public static CharacterPresentationFactValue FromIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Presentation Fact identity value is missing.", nameof(value));
            return new CharacterPresentationFactValue(
                PresentationFactValueKind.Identity,
                false,
                0f,
                default,
                default,
                default,
                0,
                0,
                value.Trim());
        }
    }

    public sealed class CharacterPresentationFactMissingException : InvalidOperationException
    {
        public CharacterPresentationFactMissingException(
            PresentationFactId factId,
            PresentationFactMissingReason reason)
            : base($"Presentation Fact '{factId}' is unavailable: {reason}.")
        {
            FactId = factId;
            Reason = reason;
        }

        public PresentationFactId FactId { get; }
        public PresentationFactMissingReason Reason { get; }
    }

    internal readonly struct CharacterPresentationFactFrame
    {

        public CharacterPresentationFactFrame(
            CharacterPresentationFactFrameIdentity identity,
            SimulationTick simulationTick,
            double presentationTime,
            bool grounded,
            Vector3 velocity,
            Quaternion rotation,
            Vector2 desiredFacing,
            bool hasMotion,
            Vector2 locomotionPlanarBasis,
            Vector2 desiredPlanarVelocity,
            string movementModeId,
            CommittedMovementPlaybackClock movementPlaybackClock,
            CommittedLocomotionPlanarMotionTimeline locomotionMotionTimeline,
            double movementPlaybackTime,
            ulong bodyDiscontinuityGeneration,
            CharacterLocomotionPresentationFactLineage locomotionFactLineage,
            ulong poseDiscontinuityIdentity)
        {
            if (!identity.IsValid || !simulationTick.IsValid ||
                !double.IsFinite(presentationTime) || presentationTime < 0d ||
                !IsFinite(velocity) || !IsFinite(rotation) ||
                !IsFinite(desiredFacing) ||
                !IsFinite(locomotionPlanarBasis) || locomotionPlanarBasis.sqrMagnitude > 1.0001f ||
                !IsFinite(desiredPlanarVelocity) ||
                string.IsNullOrWhiteSpace(movementModeId) ||
                movementPlaybackClock.IsValid && movementPlaybackClock.AuthorityTick.Value > simulationTick.Value ||
                movementPlaybackClock.IsValid != locomotionMotionTimeline.IsValid ||
                locomotionMotionTimeline.IsValid && !locomotionMotionTimeline.Matches(movementPlaybackClock) ||
                !double.IsFinite(movementPlaybackTime) || movementPlaybackTime < 0d ||
                bodyDiscontinuityGeneration == 0)
            {
                throw new ArgumentException("Presentation Fact frame is incomplete.");
            }
            Identity = identity;
            SimulationTick = simulationTick;
            PresentationTime = presentationTime;
            Grounded = grounded;
            Velocity = velocity;
            Rotation = rotation;
            DesiredFacing = desiredFacing;
            HasMotion = hasMotion;
            LocomotionPlanarBasis = locomotionPlanarBasis;
            DesiredPlanarVelocity = desiredPlanarVelocity;
            MovementModeId = movementModeId.Trim();
            MovementPlaybackClock = movementPlaybackClock;
            LocomotionMotionTimeline = locomotionMotionTimeline;
            MovementPlaybackTime = movementPlaybackTime;
            BodyDiscontinuityGeneration = bodyDiscontinuityGeneration;
            LocomotionFactLineage = locomotionFactLineage;
            PoseDiscontinuityIdentity = poseDiscontinuityIdentity;
        }

        public CharacterPresentationFactFrameIdentity Identity { get; }
        public SimulationTick SimulationTick { get; }
        public double PresentationTime { get; }
        public bool Grounded { get; }
        public Vector3 Velocity { get; }
        public Quaternion Rotation { get; }
        public Vector2 DesiredFacing { get; }
        public bool HasMotion { get; }
        public Vector2 LocomotionPlanarBasis { get; }
        public Vector2 DesiredPlanarVelocity { get; }
        public string MovementModeId { get; }
        public CommittedMovementPlaybackClock MovementPlaybackClock { get; }
        public readonly CommittedLocomotionPlanarMotionTimeline LocomotionMotionTimeline;
        public double MovementPlaybackTime { get; }
        public ulong BodyDiscontinuityGeneration { get; }
        public CharacterLocomotionPresentationFactLineage LocomotionFactLineage { get; }
        public ulong PoseDiscontinuityIdentity { get; }
        public bool IsValid => Identity.IsValid && SimulationTick.IsValid && BodyDiscontinuityGeneration != 0;

        public bool TryRead(
            PresentationFactId factId,
            out CharacterPresentationFactValue value,
            out PresentationFactMissingReason missingReason)
        {
            value = default;
            if (!IsValid)
            {
                missingReason = PresentationFactMissingReason.FrameInvalid;
                return false;
            }
            if (!factId.IsValid)
            {
                missingReason = PresentationFactMissingReason.FactIdInvalid;
                return false;
            }
            if (factId == CharacterPresentationFactSchema.Grounded)
                value = CharacterPresentationFactValue.FromBool(Grounded);
            else if (factId == CharacterPresentationFactSchema.Velocity)
                value = CharacterPresentationFactValue.FromVector3(Velocity);
            else if (factId == CharacterPresentationFactSchema.Rotation)
                value = CharacterPresentationFactValue.FromQuaternion(Rotation);
            else if (factId == CharacterPresentationFactSchema.DesiredPlanarVelocity)
                value = CharacterPresentationFactValue.FromVector2(DesiredPlanarVelocity);
            else if (factId == CharacterPresentationFactSchema.DesiredFacing)
                value = CharacterPresentationFactValue.FromVector2(DesiredFacing);
            else if (factId == CharacterPresentationFactSchema.HasMotion)
                value = CharacterPresentationFactValue.FromBool(HasMotion);
            else if (factId == CharacterPresentationFactSchema.LocomotionPlanarBasis)
                value = CharacterPresentationFactValue.FromVector2(LocomotionPlanarBasis);
            else if (factId == CharacterPresentationFactSchema.MovementMode)
                value = CharacterPresentationFactValue.FromIdentity(MovementModeId);
            else if (factId == CharacterPresentationFactSchema.BodyDiscontinuityGeneration)
                value = CharacterPresentationFactValue.FromUInt64(BodyDiscontinuityGeneration);
            else
            {
                missingReason = PresentationFactMissingReason.FactNotDeclared;
                return false;
            }
            missingReason = PresentationFactMissingReason.None;
            return true;
        }

        public CharacterPresentationFactValue Require(PresentationFactId factId)
        {
            if (TryRead(factId, out CharacterPresentationFactValue value, out PresentationFactMissingReason reason))
                return value;
            throw new CharacterPresentationFactMissingException(factId, reason);
        }

        static bool IsFinite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w);
    }

    internal sealed class CharacterPresentationFactProjector
    {
        readonly ActorId m_ActorId;
        readonly SortedDictionary<ulong, CharacterPresentationTrajectoryIntent> m_Intents =
            new SortedDictionary<ulong, CharacterPresentationTrajectoryIntent>();
        readonly List<ulong> m_TrimIntentTicks = new List<ulong>();

        double m_PresentationTime;
        ulong m_BodyBranchSequence;
        ulong m_BodyDiscontinuityGeneration;
        ulong m_LatestIntentTick;

        internal CharacterPresentationFactProjector(ActorId actorId)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Presentation Fact projector Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
        }

        internal void CaptureBodyBranch(
            ulong branchSequence,
            CharacterBodyPresentationResetReason reason)
        {
            if (branchSequence == 0 ||
                reason < CharacterBodyPresentationResetReason.Initialization ||
                reason > CharacterBodyPresentationResetReason.SelectedStreamReset)
            {
                throw new ArgumentException("Presentation Body branch identity is invalid.");
            }
            if (m_BodyBranchSequence != 0 && branchSequence < m_BodyBranchSequence)
                throw new InvalidOperationException("Presentation Body branch sequence regressed.");
            if (branchSequence == m_BodyBranchSequence)
                return;
            if (reason == CharacterBodyPresentationResetReason.CommittedBranchReplacement)
            {
                RetargetBodyBranch(branchSequence);
                return;
            }
            ResetBodyBranch(branchSequence);
        }

        internal void CaptureIntent(CharacterPresentationTrajectoryIntent intent)
        {
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Fact Intent targets another Actor.");
            if (intent.ResetSequence == 0)
                throw new InvalidOperationException("Presentation Fact Intent has no Body branch sequence.");
            if (intent.ResetSequence != m_BodyBranchSequence)
                throw new InvalidOperationException("Presentation Fact Intent does not match the current Body branch.");
            if (intent.CurrentTick.Value <= m_LatestIntentTick)
            {
                ReplaceBranchFrom(intent.CurrentTick.Value);
            }
            m_Intents.Add(intent.CurrentTick.Value, intent);
            m_LatestIntentTick = intent.CurrentTick.Value;
        }

        [PerformanceProbe("presentation.fact-projection")]
        internal CharacterPresentationFactFrame Project(
            ulong renderFrame,
            float presentationDeltaSeconds,
            in CharacterBodyPresentationFrame bodyFrame)
        {
            if (!bodyFrame.IsValid || renderFrame == 0 ||
                !float.IsFinite(presentationDeltaSeconds) || presentationDeltaSeconds < 0f)
            {
                throw new ArgumentException("Presentation Fact projection input is invalid.");
            }
            if (bodyFrame.ResetSequence == 0)
                throw new InvalidOperationException("Presentation Body has no discontinuity generation.");
            if (bodyFrame.ResetSequence != m_BodyBranchSequence ||
                m_BodyDiscontinuityGeneration == 0)
                throw new InvalidOperationException("Presentation Body branch was not captured before Fact projection.");
            double sampleTick = bodyFrame.PreviousTick +
                                (bodyFrame.CurrentTick - bodyFrame.PreviousTick) *
                                (double)bodyFrame.SampleAlpha;
            IntentSample intent = SampleIntent(sampleTick);
            Vector3 velocity = bodyFrame.VisibleVelocity;
            m_PresentationTime += presentationDeltaSeconds;
            var frame = new CharacterPresentationFactFrame(
                new CharacterPresentationFactFrameIdentity(m_ActorId, renderFrame),
                new SimulationTick(bodyFrame.CurrentTick),
                m_PresentationTime,
                bodyFrame.TargetGrounded,
                velocity,
                bodyFrame.VisibleRotation,
                intent.DesiredFacing,
                intent.HasMotion,
                intent.LocomotionPlanarBasis,
                intent.DesiredPlanarVelocity,
                intent.MovementModeId,
                intent.MovementPlaybackClock,
                intent.LocomotionMotionTimeline,
                intent.MovementPlaybackTime,
                m_BodyDiscontinuityGeneration,
                intent.LocomotionFactLineage,
                intent.PoseDiscontinuityIdentity);
            TrimIntents(bodyFrame.PreviousTick);
            return frame;
        }

        internal void Reset()
        {
            m_Intents.Clear();
            m_TrimIntentTicks.Clear();
            m_PresentationTime = 0d;
            m_BodyBranchSequence = 0;
            m_BodyDiscontinuityGeneration = 0;
            m_LatestIntentTick = 0;
        }

        IntentSample SampleIntent(double sampleTick)
        {
            if (m_Intents.Count == 0)
                throw new InvalidOperationException("Presentation Fact projection has no committed Intent.");
            bool hasPrevious = false;
            CharacterPresentationTrajectoryIntent previous = default;
            ulong previousTick = 0;
            foreach (KeyValuePair<ulong, CharacterPresentationTrajectoryIntent> pair in m_Intents)
            {
                if (pair.Key <= sampleTick)
                {
                    previous = pair.Value;
                    previousTick = pair.Key;
                    hasPrevious = true;
                    continue;
                }
                if (!hasPrevious)
                {
                    double intervalStart = pair.Value.PreviousTick.IsValid
                        ? pair.Value.PreviousTick.Value
                        : 0d;
                    if (sampleTick < intervalStart)
                        throw new InvalidOperationException("Presentation Fact projection cannot sample Intent before its first committed interval.");
                    return IntentSample.From(pair.Value);
                }
                float alpha = Mathf.Clamp01((float)((sampleTick - previousTick) / (pair.Key - previousTick)));
                return IntentSample.Lerp(previous, pair.Value, alpha);
            }
            if (!hasPrevious)
                throw new InvalidOperationException("Presentation Fact projection cannot sample committed Intent.");
            return IntentSample.From(previous);
        }

        void ResetBodyBranch(ulong branchSequence)
        {
            m_Intents.Clear();
            m_TrimIntentTicks.Clear();
            m_BodyBranchSequence = branchSequence;
            m_BodyDiscontinuityGeneration = branchSequence;
            m_LatestIntentTick = 0;
        }

        void RetargetBodyBranch(ulong branchSequence)
        {
            if (m_BodyDiscontinuityGeneration == 0)
                throw new InvalidOperationException("Presentation Body branch cannot retarget before initialization.");
            m_Intents.Clear();
            m_TrimIntentTicks.Clear();
            m_BodyBranchSequence = branchSequence;
            m_LatestIntentTick = 0;
        }

        void ReplaceBranchFrom(ulong firstReplacementTick)
        {
            m_TrimIntentTicks.Clear();
            foreach (ulong tick in m_Intents.Keys)
            {
                if (tick >= firstReplacementTick)
                    m_TrimIntentTicks.Add(tick);
            }
            for (int i = 0; i < m_TrimIntentTicks.Count; i++)
                m_Intents.Remove(m_TrimIntentTicks[i]);
            m_LatestIntentTick = 0;
            foreach (ulong tick in m_Intents.Keys)
                m_LatestIntentTick = tick;
            m_TrimIntentTicks.Clear();
        }

        void TrimIntents(ulong retainTick)
        {
            m_TrimIntentTicks.Clear();
            ulong lastBeforeRetain = 0;
            foreach (ulong tick in m_Intents.Keys)
            {
                if (tick >= retainTick)
                    break;
                if (lastBeforeRetain != 0)
                    m_TrimIntentTicks.Add(lastBeforeRetain);
                lastBeforeRetain = tick;
            }
            for (int i = 0; i < m_TrimIntentTicks.Count; i++)
                m_Intents.Remove(m_TrimIntentTicks[i]);
        }

        readonly struct IntentSample
        {
            IntentSample(
                Vector2 locomotionPlanarBasis,
                Vector2 desiredPlanarVelocity,
                Vector2 desiredFacing,
                bool hasMotion,
                string movementModeId,
                CommittedMovementPlaybackClock movementPlaybackClock,
                CommittedLocomotionPlanarMotionTimeline locomotionMotionTimeline,
                double movementPlaybackTime,
                CharacterLocomotionPresentationFactLineage locomotionFactLineage,
                ulong poseDiscontinuityIdentity)
            {
                if (string.IsNullOrWhiteSpace(movementModeId))
                    throw new ArgumentException("Presentation Intent sample is incomplete.", nameof(movementModeId));
                LocomotionPlanarBasis = locomotionPlanarBasis;
                DesiredPlanarVelocity = desiredPlanarVelocity;
                DesiredFacing = desiredFacing;
                HasMotion = hasMotion;
                MovementModeId = movementModeId;
                MovementPlaybackClock = movementPlaybackClock;
                LocomotionMotionTimeline = locomotionMotionTimeline;
                MovementPlaybackTime = movementPlaybackTime;
                LocomotionFactLineage = locomotionFactLineage;
                PoseDiscontinuityIdentity = poseDiscontinuityIdentity;
            }

            internal Vector2 LocomotionPlanarBasis { get; }
            internal Vector2 DesiredPlanarVelocity { get; }
            internal Vector2 DesiredFacing { get; }
            internal bool HasMotion { get; }
            internal string MovementModeId { get; }
            internal CommittedMovementPlaybackClock MovementPlaybackClock { get; }
            internal CommittedLocomotionPlanarMotionTimeline LocomotionMotionTimeline { get; }
            internal double MovementPlaybackTime { get; }
            internal CharacterLocomotionPresentationFactLineage LocomotionFactLineage { get; }
            internal ulong PoseDiscontinuityIdentity { get; }

            internal static IntentSample From(CharacterPresentationTrajectoryIntent intent) =>
                new IntentSample(
                    intent.LocomotionPlanarBasis,
                    intent.DesiredPlanarVelocity,
                    intent.DesiredFacing,
                    intent.HasMotion,
                    intent.MovementModeId,
                    intent.MovementPlaybackClock,
                    intent.LocomotionMotionTimeline,
                    intent.MovementPlaybackClock.ElapsedSeconds,
                    intent.LocomotionFactLineage,
                    intent.PoseDiscontinuityIdentity);

            internal static IntentSample Lerp(
                CharacterPresentationTrajectoryIntent previous,
                CharacterPresentationTrajectoryIntent current,
                float alpha)
            {
                float radians = Vector2.SignedAngle(previous.DesiredFacing, current.DesiredFacing) *
                    Mathf.Deg2Rad * alpha;
                float sin = Mathf.Sin(radians);
                float cos = Mathf.Cos(radians);
                Vector2 facing = new Vector2(
                    previous.DesiredFacing.x * cos - previous.DesiredFacing.y * sin,
                    previous.DesiredFacing.x * sin + previous.DesiredFacing.y * cos);
                bool useCurrentDiscrete = alpha >= 1f;
                CommittedMovementPlaybackClock previousClock = previous.MovementPlaybackClock;
                CommittedMovementPlaybackClock currentClock = current.MovementPlaybackClock;
                bool sameMovementClock = previousClock.IsValid &&
                                         currentClock.IsValid &&
                                         previousClock.Generation == currentClock.Generation &&
                                         string.Equals(
                                             previousClock.OwnerIdentity,
                                             currentClock.OwnerIdentity,
                                             StringComparison.Ordinal);
                if (sameMovementClock &&
                    (currentClock.AuthorityTick.Value <= previousClock.AuthorityTick.Value ||
                     currentClock.ContinuousTicks < previousClock.ContinuousTicks))
                {
                    throw new InvalidOperationException("Committed Movement playback clock is not monotonic within one identity.");
                }
                double previousMotionTime = previousClock.ElapsedSeconds;
                double currentMotionTime = currentClock.ElapsedSeconds;
                double movementPlaybackTime = sameMovementClock
                    ? previousMotionTime + (currentMotionTime - previousMotionTime) * alpha
                    : useCurrentDiscrete ? currentMotionTime : previousMotionTime;
                return new IntentSample(
                    Vector2.Lerp(previous.LocomotionPlanarBasis, current.LocomotionPlanarBasis, alpha),
                    Vector2.Lerp(previous.DesiredPlanarVelocity, current.DesiredPlanarVelocity, alpha),
                    facing.normalized,
                    useCurrentDiscrete ? current.HasMotion : previous.HasMotion,
                    useCurrentDiscrete ? current.MovementModeId : previous.MovementModeId,
                    useCurrentDiscrete ? currentClock : previousClock,
                    useCurrentDiscrete
                        ? current.LocomotionMotionTimeline
                        : previous.LocomotionMotionTimeline,
                    movementPlaybackTime,
                    useCurrentDiscrete
                        ? current.LocomotionFactLineage
                        : previous.LocomotionFactLineage,
                    useCurrentDiscrete
                        ? current.PoseDiscontinuityIdentity
                        : previous.PoseDiscontinuityIdentity);
            }
        }
    }
}
