using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Presentation;
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
        Identity = 6
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

    public static class CharacterPresentationFactSchema
    {
        public const string Version = "character-presentation-fact/v2";

        public static readonly PresentationFactId Grounded = new PresentationFactId("presentation.grounded");
        public static readonly PresentationFactId HorizontalSpeed = new PresentationFactId("presentation.horizontal-speed");
        public static readonly PresentationFactId HorizontalAcceleration = new PresentationFactId("presentation.horizontal-acceleration");
        public static readonly PresentationFactId VerticalSpeed = new PresentationFactId("presentation.vertical-speed");
        public static readonly PresentationFactId MovementDirection = new PresentationFactId("presentation.movement-direction");
        public static readonly PresentationFactId DesiredDirection = new PresentationFactId("presentation.desired-direction");
        public static readonly PresentationFactId FacingError = new PresentationFactId("presentation.facing-error");
        public static readonly PresentationFactId MotionPhase = new PresentationFactId("presentation.motion-phase");
        public static readonly PresentationFactId MovementMode = new PresentationFactId("presentation.movement-mode");
        public static readonly PresentationFactId BodyDiscontinuityGeneration = new PresentationFactId("presentation.body-discontinuity-generation");

        public static PresentationFactValueKind RequireValueKind(PresentationFactId id)
        {
            if (!id.IsValid)
                throw new CharacterPresentationFactMissingException(id, PresentationFactMissingReason.FactIdInvalid);
            if (id == Grounded)
                return PresentationFactValueKind.Bool;
            if (id == HorizontalSpeed || id == HorizontalAcceleration || id == VerticalSpeed || id == FacingError)
                return PresentationFactValueKind.Float;
            if (id == MovementDirection || id == DesiredDirection)
                return PresentationFactValueKind.Vector2;
            if (id == MotionPhase)
                return PresentationFactValueKind.Enum;
            if (id == MovementMode)
                return PresentationFactValueKind.Identity;
            if (id == BodyDiscontinuityGeneration)
                return PresentationFactValueKind.UInt64;
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
            int enumValue,
            ulong uint64Value,
            string identityValue)
        {
            Kind = kind;
            BoolValue = boolValue;
            FloatValue = floatValue;
            Vector2Value = vector2Value;
            EnumValue = enumValue;
            UInt64Value = uint64Value;
            IdentityValue = identityValue ?? string.Empty;
        }

        public PresentationFactValueKind Kind { get; }
        public bool BoolValue { get; }
        public float FloatValue { get; }
        public Vector2 Vector2Value { get; }
        public int EnumValue { get; }
        public ulong UInt64Value { get; }
        public string IdentityValue { get; }

        public static CharacterPresentationFactValue FromBool(bool value) =>
            new CharacterPresentationFactValue(PresentationFactValueKind.Bool, value, 0f, default, 0, 0, string.Empty);

        public static CharacterPresentationFactValue FromFloat(float value)
        {
            if (!float.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new CharacterPresentationFactValue(PresentationFactValueKind.Float, false, value, default, 0, 0, string.Empty);
        }

        public static CharacterPresentationFactValue FromVector2(Vector2 value)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
                throw new ArgumentOutOfRangeException(nameof(value));
            return new CharacterPresentationFactValue(PresentationFactValueKind.Vector2, false, 0f, value, 0, 0, string.Empty);
        }

        public static CharacterPresentationFactValue FromMotionPhase(
            CharacterPresentationMotionPhase value)
        {
            if ((byte)value < (byte)CharacterPresentationMotionPhase.GroundedStationary ||
                (byte)value > (byte)CharacterPresentationMotionPhase.AirborneFalling)
                throw new ArgumentOutOfRangeException(nameof(value));
            return new CharacterPresentationFactValue(
                PresentationFactValueKind.Enum,
                false,
                0f,
                default,
                (int)value,
                0,
                string.Empty);
        }

        public static CharacterPresentationFactValue FromUInt64(ulong value) =>
            new CharacterPresentationFactValue(PresentationFactValueKind.UInt64, false, 0f, default, 0, value, string.Empty);

        public static CharacterPresentationFactValue FromIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Presentation Fact identity value is missing.", nameof(value));
            return new CharacterPresentationFactValue(
                PresentationFactValueKind.Identity,
                false,
                0f,
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
            float horizontalSpeed,
            float horizontalAcceleration,
            float verticalSpeed,
            Vector2 movementDirection,
            Vector2 desiredDirection,
            float facingError,
            CharacterPresentationMotionPhase motionPhase,
            string movementModeId,
            ulong bodyDiscontinuityGeneration)
        {
            if (!identity.IsValid || !simulationTick.IsValid ||
                !double.IsFinite(presentationTime) || presentationTime < 0d ||
                !float.IsFinite(horizontalSpeed) || horizontalSpeed < 0f ||
                !float.IsFinite(horizontalAcceleration) || horizontalAcceleration < 0f ||
                !float.IsFinite(verticalSpeed) ||
                !IsFinite(movementDirection) || movementDirection.sqrMagnitude > 1.0001f ||
                !IsFinite(desiredDirection) || desiredDirection.sqrMagnitude > 1.0001f ||
                !float.IsFinite(facingError) ||
                (byte)motionPhase < (byte)CharacterPresentationMotionPhase.GroundedStationary ||
                (byte)motionPhase > (byte)CharacterPresentationMotionPhase.AirborneFalling ||
                string.IsNullOrWhiteSpace(movementModeId) ||
                bodyDiscontinuityGeneration == 0)
            {
                throw new ArgumentException("Presentation Fact frame is incomplete.");
            }
            Identity = identity;
            SimulationTick = simulationTick;
            PresentationTime = presentationTime;
            Grounded = grounded;
            HorizontalSpeed = horizontalSpeed;
            HorizontalAcceleration = horizontalAcceleration;
            VerticalSpeed = verticalSpeed;
            MovementDirection = movementDirection;
            DesiredDirection = desiredDirection;
            FacingError = facingError;
            MotionPhase = motionPhase;
            MovementModeId = movementModeId.Trim();
            BodyDiscontinuityGeneration = bodyDiscontinuityGeneration;
        }

        public CharacterPresentationFactFrameIdentity Identity { get; }
        public SimulationTick SimulationTick { get; }
        public double PresentationTime { get; }
        public bool Grounded { get; }
        public float HorizontalSpeed { get; }
        public float HorizontalAcceleration { get; }
        public float VerticalSpeed { get; }
        public Vector2 MovementDirection { get; }
        public Vector2 DesiredDirection { get; }
        public float FacingError { get; }
        public CharacterPresentationMotionPhase MotionPhase { get; }
        public string MovementModeId { get; }
        public ulong BodyDiscontinuityGeneration { get; }
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
            else if (factId == CharacterPresentationFactSchema.HorizontalSpeed)
                value = CharacterPresentationFactValue.FromFloat(HorizontalSpeed);
            else if (factId == CharacterPresentationFactSchema.HorizontalAcceleration)
                value = CharacterPresentationFactValue.FromFloat(HorizontalAcceleration);
            else if (factId == CharacterPresentationFactSchema.VerticalSpeed)
                value = CharacterPresentationFactValue.FromFloat(VerticalSpeed);
            else if (factId == CharacterPresentationFactSchema.MovementDirection)
                value = CharacterPresentationFactValue.FromVector2(MovementDirection);
            else if (factId == CharacterPresentationFactSchema.DesiredDirection)
                value = CharacterPresentationFactValue.FromVector2(DesiredDirection);
            else if (factId == CharacterPresentationFactSchema.FacingError)
                value = CharacterPresentationFactValue.FromFloat(FacingError);
            else if (factId == CharacterPresentationFactSchema.MotionPhase)
                value = CharacterPresentationFactValue.FromMotionPhase(MotionPhase);
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
    }

    internal sealed class CharacterPresentationFactProjector
    {
        readonly ActorId m_ActorId;
        readonly SortedDictionary<ulong, CharacterPresentationTrajectoryIntent> m_Intents =
            new SortedDictionary<ulong, CharacterPresentationTrajectoryIntent>();
        readonly List<ulong> m_TrimIntentTicks = new List<ulong>();

        CharacterPresentationFactFrame m_PreviousFrame;
        Vector2 m_PreviousPlanarVelocity;
        double m_PresentationTime;
        ulong m_ResetSequence;
        ulong m_LatestIntentTick;

        internal CharacterPresentationFactProjector(ActorId actorId)
        {
            if (!actorId.IsValid)
                throw new ArgumentException("Presentation Fact projector Actor identity is invalid.", nameof(actorId));
            m_ActorId = actorId;
        }

        internal void CaptureIntent(CharacterPresentationTrajectoryIntent intent)
        {
            if (intent.ActorId != m_ActorId)
                throw new InvalidOperationException("Presentation Fact Intent targets another Actor.");
            if (intent.ResetSequence == 0)
                throw new InvalidOperationException("Presentation Fact Intent has no Body discontinuity generation.");
            if (m_ResetSequence != 0 && intent.ResetSequence < m_ResetSequence)
                throw new InvalidOperationException("Presentation Fact Intent discontinuity generation regressed.");
            if (intent.ResetSequence != m_ResetSequence)
            {
                ResetBranch(intent.ResetSequence);
            }
            else if (intent.CurrentTick.Value <= m_LatestIntentTick)
            {
                throw new InvalidOperationException("Presentation Fact Intent Tick duplicated or regressed.");
            }
            m_Intents.Add(intent.CurrentTick.Value, intent);
            m_LatestIntentTick = intent.CurrentTick.Value;
        }

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
            if (bodyFrame.ResetSequence != m_ResetSequence)
            {
                if (bodyFrame.ResetSequence < m_ResetSequence)
                    throw new InvalidOperationException("Presentation Body discontinuity generation regressed.");
                ResetBranch(bodyFrame.ResetSequence);
            }
            double sampleTick = bodyFrame.PreviousTick +
                                (bodyFrame.CurrentTick - bodyFrame.PreviousTick) *
                                (double)bodyFrame.SampleAlpha;
            IntentSample intent = SampleIntent(sampleTick);
            Vector3 velocity = bodyFrame.VisibleVelocity;
            var planarVelocity = new Vector2(velocity.x, velocity.z);
            float speed = planarVelocity.magnitude;
            Vector2 movementDirection = speed > 0.0001f ? planarVelocity / speed : Vector2.zero;
            Vector2 desiredVelocity = intent.DesiredPlanarVelocity;
            Vector2 desiredDirection = desiredVelocity.sqrMagnitude > 0.00000001f
                ? desiredVelocity.normalized
                : Vector2.zero;
            Vector3 forward3 = bodyFrame.VisibleRotation * Vector3.forward;
            var facing = new Vector2(forward3.x, forward3.z).normalized;
            float facingError = Vector2.SignedAngle(facing, intent.DesiredFacing);
            float acceleration = m_PreviousFrame.IsValid && presentationDeltaSeconds > 0f
                ? (planarVelocity - m_PreviousPlanarVelocity).magnitude / presentationDeltaSeconds
                : 0f;
            m_PresentationTime += presentationDeltaSeconds;
            var frame = new CharacterPresentationFactFrame(
                new CharacterPresentationFactFrameIdentity(m_ActorId, renderFrame),
                new SimulationTick(bodyFrame.CurrentTick),
                m_PresentationTime,
                bodyFrame.TargetGrounded,
                speed,
                acceleration,
                velocity.y,
                movementDirection,
                desiredDirection,
                facingError,
                ResolveMotionPhase(bodyFrame.TargetGrounded, intent.HasMotion, speed, velocity.y),
                intent.MovementModeId,
                bodyFrame.ResetSequence);
            m_PreviousFrame = frame;
            m_PreviousPlanarVelocity = planarVelocity;
            TrimIntents(bodyFrame.PreviousTick);
            return frame;
        }

        internal void Reset()
        {
            m_Intents.Clear();
            m_TrimIntentTicks.Clear();
            m_PreviousFrame = default;
            m_PreviousPlanarVelocity = default;
            m_PresentationTime = 0d;
            m_ResetSequence = 0;
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

        void ResetBranch(ulong resetSequence)
        {
            m_Intents.Clear();
            m_TrimIntentTicks.Clear();
            m_PreviousFrame = default;
            m_PreviousPlanarVelocity = default;
            m_ResetSequence = resetSequence;
            m_LatestIntentTick = 0;
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

        static CharacterPresentationMotionPhase ResolveMotionPhase(
            bool grounded,
            bool hasMotion,
            float speed,
            float verticalSpeed)
        {
            if (!grounded)
            {
                return verticalSpeed > 0f
                    ? CharacterPresentationMotionPhase.AirborneRising
                    : CharacterPresentationMotionPhase.AirborneFalling;
            }
            return hasMotion || speed > 0.0001f
                ? CharacterPresentationMotionPhase.GroundedMoving
                : CharacterPresentationMotionPhase.GroundedStationary;
        }

        readonly struct IntentSample
        {
            IntentSample(
                Vector2 desiredPlanarVelocity,
                Vector2 desiredFacing,
                bool hasMotion,
                string movementModeId)
            {
                if (string.IsNullOrWhiteSpace(movementModeId))
                    throw new ArgumentException("Presentation Intent movement mode identity is missing.", nameof(movementModeId));
                DesiredPlanarVelocity = desiredPlanarVelocity;
                DesiredFacing = desiredFacing;
                HasMotion = hasMotion;
                MovementModeId = movementModeId;
            }

            internal Vector2 DesiredPlanarVelocity { get; }
            internal Vector2 DesiredFacing { get; }
            internal bool HasMotion { get; }
            internal string MovementModeId { get; }

            internal static IntentSample From(CharacterPresentationTrajectoryIntent intent) =>
                new IntentSample(
                    intent.DesiredPlanarVelocity,
                    intent.DesiredFacing,
                    intent.HasMotion,
                    intent.MovementModeId);

            internal static IntentSample Lerp(
                CharacterPresentationTrajectoryIntent previous,
                CharacterPresentationTrajectoryIntent current,
                float alpha)
            {
                Vector2 facing = Vector2.Lerp(previous.DesiredFacing, current.DesiredFacing, alpha);
                if (facing.sqrMagnitude <= 0.00000001f)
                    throw new InvalidOperationException("Presentation Fact desired facing interpolation is degenerate.");
                bool useCurrentDiscrete = alpha >= 1f;
                return new IntentSample(
                    Vector2.Lerp(previous.DesiredPlanarVelocity, current.DesiredPlanarVelocity, alpha),
                    facing.normalized,
                    useCurrentDiscrete ? current.HasMotion : previous.HasMotion,
                    useCurrentDiscrete ? current.MovementModeId : previous.MovementModeId);
            }
        }
    }
}
