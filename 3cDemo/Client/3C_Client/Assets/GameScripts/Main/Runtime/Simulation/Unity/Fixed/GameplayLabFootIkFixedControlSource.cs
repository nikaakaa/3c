using System;
using System.Collections.Generic;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using FixedCommittedActorPose = ThirdPersonSimulation.CommittedActorPose<ThirdPersonSimulation.Fixed.FixedVector3, ThirdPersonSimulation.Fixed.FixedYaw>;
using FixedCharacterSimulationInput = ThirdPersonSimulation.Fixed.CharacterSimulationInput;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public enum GameplayLabFootIkRoutePhase : byte
    {
        AlignStart = 1,
        ApproachStart = 2,
        SettleStart = 3,
        StartToEnd = 4
    }

    public readonly struct GameplayLabFootIkRouteSnapshot
    {
        public GameplayLabFootIkRouteSnapshot(
            string runId,
            ActorId actorId,
            GameplayLabFootIkRoutePhase phase,
            GameplayLabFootIkInputScenario scenario,
            int traversalSegment,
            int lap,
            ulong renderFrame,
            ulong simulationTick,
            Vector3 start,
            Vector3 end,
            Vector3 actorPosition,
            Vector2 movement,
            float actorYawDegrees,
            float actualPlanarSpeed,
            int tickRate)
        {
            RunId = runId ?? string.Empty;
            ActorId = actorId;
            Phase = phase;
            Scenario = scenario;
            TraversalSegment = traversalSegment;
            Lap = lap;
            RenderFrame = renderFrame;
            SimulationTick = simulationTick;
            Start = start;
            End = end;
            ActorPosition = actorPosition;
            Movement = movement;
            ActorYawDegrees = actorYawDegrees;
            ActualPlanarSpeed = actualPlanarSpeed;
            TickRate = tickRate;
        }

        public string RunId { get; }
        public ActorId ActorId { get; }
        public GameplayLabFootIkRoutePhase Phase { get; }
        public GameplayLabFootIkInputScenario Scenario { get; }
        public int TraversalSegment { get; }
        public int Lap { get; }
        public ulong RenderFrame { get; }
        public ulong SimulationTick { get; }
        public Vector3 Start { get; }
        public Vector3 End { get; }
        public Vector3 ActorPosition { get; }
        public Vector2 Movement { get; }
        public float ActorYawDegrees { get; }
        public float ActualPlanarSpeed { get; }
        public int TickRate { get; }
        public string Direction
        {
            get
            {
                string scenario = GameplayLabFootIkRegressionCourse.ScenarioIdentity(Scenario);
                string phase = Phase switch
                {
                    GameplayLabFootIkRoutePhase.StartToEnd => "start-to-end",
                    GameplayLabFootIkRoutePhase.AlignStart => "align-start",
                    GameplayLabFootIkRoutePhase.ApproachStart => "approach-start",
                    GameplayLabFootIkRoutePhase.SettleStart => "settle-start",
                    _ => throw new InvalidOperationException("GameplayLab Foot IK route phase is invalid.")
                };
                return $"{scenario}-{phase}";
            }
        }
        public bool IsTraversal => Phase == GameplayLabFootIkRoutePhase.StartToEnd;
    }

    readonly struct GameplayLabFootIkResolvedInput
    {
        public GameplayLabFootIkResolvedInput(Vector2 value, bool cameraRelative)
        {
            Value = value;
            CameraRelative = cameraRelative;
        }

        public Vector2 Value { get; }
        public bool CameraRelative { get; }

        public static GameplayLabFootIkResolvedInput World(Vector2 value) =>
            new GameplayLabFootIkResolvedInput(value, false);

        public static GameplayLabFootIkResolvedInput Camera(Vector2 value) =>
            new GameplayLabFootIkResolvedInput(value, true);
    }

    public static class GameplayLabFootIkRouteRegistry
    {
        static readonly Dictionary<ActorId, GameplayLabFootIkRouteSnapshot> s_Snapshots =
            new Dictionary<ActorId, GameplayLabFootIkRouteSnapshot>();

        public static bool TryGet(ActorId actorId, out GameplayLabFootIkRouteSnapshot snapshot) =>
            s_Snapshots.TryGetValue(actorId, out snapshot);

        internal static void Publish(in GameplayLabFootIkRouteSnapshot snapshot)
        {
            s_Snapshots[snapshot.ActorId] = snapshot;
        }

        internal static void Remove(ActorId actorId)
        {
            s_Snapshots.Remove(actorId);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            s_Snapshots.Clear();
        }
    }

    [DisallowMultipleComponent]
    public sealed class GameplayLabFootIkFixedControlSource : FixedCharacterControlSource
    {
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] string m_ActionTargetInputValueId;
        [SerializeField] CharacterActionTargetInputProvider m_ActionTargetProvider;
        [SerializeField] string m_MoveInputValueId = "MoveAxis";
        [SerializeField, Min(0.05f)] float m_ArrivalRadius = 0.18f;
        [SerializeField, Min(0f)] float m_EndpointHoldSeconds = 0.75f;

        public override string SourceIdentity =>
            $"gameplay-lab-foot-ik/course-v7/{m_MoveInputValueId}/{(m_InputProfile ? m_InputProfile.name : "unconfigured")}";

        public override IUnityFixedCharacterControlSourceRuntime Create(FixedCharacterControlSourceContext context)
        {
            GameplayLabFootIkRegressionCourse.Resolve(gameObject.scene, out Vector3 start, out Vector3 end);
            CharacterInputProfile inputProfile = m_InputProfile ? m_InputProfile :
                throw new InvalidOperationException($"GameplayLab Foot IK Control Source '{name}' requires Corin's Character Input Profile.");
            ThirdPersonCameraController cameraRig = context.Owner.CameraRig ? context.Owner.CameraRig :
                throw new InvalidOperationException($"GameplayLab Foot IK Control Source '{name}' requires the Fixed Character Host Camera Rig.");
            return new GameplayLabFootIkInputSystemRuntime(
                context.Owner.ActorId,
                SourceIdentity,
                inputProfile,
                context.Program,
                cameraRig,
                context.Owner,
                string.IsNullOrWhiteSpace(m_ActionTargetInputValueId) ? string.Empty : m_ActionTargetInputValueId.Trim(),
                m_ActionTargetProvider,
                Require(m_MoveInputValueId, nameof(m_MoveInputValueId)),
                start,
                end,
                Mathf.Max(0.05f, m_ArrivalRadius),
                Mathf.Max(0f, m_EndpointHoldSeconds));
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            CharacterInputProfile inputProfile,
            string actionTargetInputValueId,
            CharacterActionTargetInputProvider actionTargetProvider,
            string moveInputValueId,
            float arrivalRadius,
            float endpointHoldSeconds)
        {
            m_InputProfile = inputProfile ? inputProfile : throw new ArgumentNullException(nameof(inputProfile));
            m_ActionTargetInputValueId = string.IsNullOrWhiteSpace(actionTargetInputValueId)
                ? string.Empty
                : actionTargetInputValueId.Trim();
            m_ActionTargetProvider = actionTargetProvider;
            m_MoveInputValueId = Require(moveInputValueId, nameof(moveInputValueId));
            m_ArrivalRadius = Mathf.Max(0.05f, arrivalRadius);
            m_EndpointHoldSeconds = Mathf.Max(0f, endpointHoldSeconds);
        }
#endif

        static string Require(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("GameplayLab Foot IK route identity is empty.", parameterName);
            return value.Trim();
        }
    }

    sealed class GameplayLabFootIkInputSystemRuntime :
        IUnityFixedCharacterControlSourceRuntime,
        ICharacterPresentationLookInput
    {
        readonly ActorId m_ActorId;
        readonly string m_RouteIdentity;
        readonly string m_MoveInputValueId;
        readonly Vector3 m_Start;
        readonly Vector3 m_End;
        readonly Vector3 m_StartAlignment;
        readonly Vector3 m_RouteDirection;
        readonly float m_ArrivalRadius;
        readonly float m_EndpointHoldSeconds;
        readonly int m_TickRate;
        readonly FixedCharacterHost m_Owner;
        readonly UnityFixedCharacterInputAdapter m_PlayerInput;
        readonly string m_RunId = Guid.NewGuid().ToString("N");
        GameplayLabFootIkRoutePhase m_Phase = GameplayLabFootIkRoutePhase.AlignStart;
        int m_TraversalSegment;
        int m_Lap = 1;
        int m_HoldTicksRemaining;
        int m_TraversalTicksRemaining;
        ulong m_RenderFrame;
        ulong m_SimulationTick;
        ulong m_LastRouteUpdateTick;
        ulong m_LastCommittedSimulationTick;
        ulong m_InputUpdateSequence;
        ulong m_SubmissionInputUpdateSequence;
        Vector3 m_LastCommittedPosition;
        Vector2 m_LastWorldMovement;
        Vector2 m_LastCameraMovement;
        Vector2 m_SubmittedCameraMovement;
        float m_LastCommittedYawDegrees;
        float m_LastActualPlanarSpeed;
        Gamepad m_Gamepad;
        bool m_HasSubmittedInput;
        bool m_Active;
        bool m_Disposed;

        internal GameplayLabFootIkInputSystemRuntime(
            ActorId actorId,
            string routeIdentity,
            CharacterInputProfile inputProfile,
            ThirdPersonSimulation.Fixed.CharacterSimulationProgram program,
            ThirdPersonCameraController cameraRig,
            FixedCharacterHost owner,
            string actionTargetInputValueId,
            CharacterActionTargetInputProvider actionTargetProvider,
            string moveInputValueId,
            Vector3 start,
            Vector3 end,
            float arrivalRadius,
            float endpointHoldSeconds)
        {
            m_ActorId = actorId.IsValid ? actorId : throw new ArgumentException("GameplayLab Foot IK Actor identity is invalid.", nameof(actorId));
            m_RouteIdentity = routeIdentity ?? throw new ArgumentNullException(nameof(routeIdentity));
            m_MoveInputValueId = moveInputValueId ?? throw new ArgumentNullException(nameof(moveInputValueId));
            m_Owner = owner ? owner : throw new ArgumentNullException(nameof(owner));
            m_PlayerInput = new UnityFixedCharacterInputAdapter(
                inputProfile,
                program,
                cameraRig,
                owner,
                actionTargetInputValueId,
                actionTargetProvider);
            m_Start = start;
            m_End = end;
            Vector3 route = Vector3.ProjectOnPlane(end - start, Vector3.up);
            float alignmentDistance = Mathf.Min(
                GameplayLabFootIkRegressionCourse.AlignmentDistance,
                route.magnitude * 0.5f);
            m_RouteDirection = route.normalized;
            m_StartAlignment = start - m_RouteDirection * alignmentDistance;
            m_ArrivalRadius = arrivalRadius;
            m_EndpointHoldSeconds = endpointHoldSeconds;
            m_TickRate = program.Manifest.TickRate;
            m_LastCommittedPosition = owner.VisualPosition;
        }

        public string SourceIdentity => m_PlayerInput.SourceIdentity;
        public ProgramId CharacterProgramId => m_PlayerInput.CharacterProgramId;
        public ProgramHash CharacterProgramHash => m_PlayerInput.CharacterProgramHash;

        public void Activate()
        {
            RequireAlive();
            if (m_Active)
                return;
            m_Gamepad = InputSystem.AddDevice<Gamepad>($"GameplayLabFootIk-{m_ActorId.Value}");
            m_Gamepad.MakeCurrent();
            ApplyGamepadState(Vector2.zero);
            try
            {
                m_PlayerInput.Activate();
                InputSystem.onAfterUpdate += OnAfterInputUpdate;
            }
            catch
            {
                InputSystem.RemoveDevice(m_Gamepad);
                m_Gamepad = null;
                throw;
            }
            m_Active = true;
            m_Phase = GameplayLabFootIkRoutePhase.AlignStart;
            m_TraversalSegment = 0;
            m_Lap = 1;
            m_TraversalTicksRemaining = 0;
            m_SubmittedCameraMovement = Vector2.zero;
            m_SubmissionInputUpdateSequence = m_InputUpdateSequence;
            m_HasSubmittedInput = true;
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;
            InputSystem.onAfterUpdate -= OnAfterInputUpdate;
            if (m_Gamepad != null && m_Gamepad.added)
                ApplyGamepadState(Vector2.zero);
            m_PlayerInput.Deactivate();
            if (m_Gamepad != null && m_Gamepad.added)
                InputSystem.RemoveDevice(m_Gamepad);
            m_Gamepad = null;
            m_Active = false;
            m_RenderFrame = 0;
            m_SimulationTick = 0;
            m_LastRouteUpdateTick = 0;
            m_LastWorldMovement = Vector2.zero;
            m_LastCameraMovement = Vector2.zero;
            m_LastCommittedYawDegrees = 0f;
            m_LastCommittedSimulationTick = 0;
            m_LastActualPlanarSpeed = 0f;
            m_InputUpdateSequence = 0;
            m_SubmissionInputUpdateSequence = 0;
            m_Phase = GameplayLabFootIkRoutePhase.AlignStart;
            m_TraversalSegment = 0;
            m_Lap = 1;
            m_HoldTicksRemaining = 0;
            m_TraversalTicksRemaining = 0;
            m_SubmittedCameraMovement = Vector2.zero;
            m_HasSubmittedInput = false;
            GameplayLabFootIkRouteRegistry.Remove(m_ActorId);
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Active || renderFrame == 0 || renderFrame <= m_RenderFrame)
                throw new InvalidOperationException("GameplayLab Foot IK input requires an active, strictly increasing render frame.");
            m_RenderFrame = renderFrame;
            CameraBasisSnapshot cameraBasis = m_Owner.CameraRig.BasisSnapshot;
            if (m_Gamepad == null || !m_Gamepad.added)
                throw new InvalidOperationException("GameplayLab Foot IK virtual gamepad is unavailable.");
            m_Gamepad.MakeCurrent();
            m_PlayerInput.CaptureRenderFrame(renderFrame);
            if (!m_PlayerInput.TryGetLatchedVector2(m_MoveInputValueId, out Vector2 value))
                throw new InvalidOperationException("GameplayLab Foot IK could not read Corin's formal Move Input Action.");
            if (m_HasSubmittedInput && m_InputUpdateSequence <= m_SubmissionInputUpdateSequence)
            {
                m_LastCameraMovement = value;
                m_LastWorldMovement = ToWorldRelative(value, cameraBasis);
                return;
            }
            if ((m_SubmittedCameraMovement - value).sqrMagnitude > 0.000001f)
                throw new InvalidOperationException(
                    $"GameplayLab Foot IK formal Move Input Action did not consume the previously submitted virtual gamepad state. Submitted={m_SubmittedCameraMovement}, Latched={value}.");
            m_LastCameraMovement = value;
            m_LastWorldMovement = ToWorldRelative(value, cameraBasis);
            ulong elapsedTickCount = m_SimulationTick > m_LastRouteUpdateTick
                ? m_SimulationTick - m_LastRouteUpdateTick
                : 0UL;
            int elapsedTicks = elapsedTickCount > int.MaxValue ? int.MaxValue : (int)elapsedTickCount;
            m_LastRouteUpdateTick = m_SimulationTick;
            GameplayLabFootIkResolvedInput resolved = ResolveMovement(m_LastCommittedPosition, elapsedTicks);
            Vector2 nextCameraMovement = resolved.CameraRelative
                ? resolved.Value
                : ToCameraRelative(resolved.Value, cameraBasis);
            ApplyGamepadState(nextCameraMovement);
            Vector2 deviceMovement = m_Gamepad.leftStick.ReadUnprocessedValue();
            if ((deviceMovement - nextCameraMovement).sqrMagnitude > 0.000001f)
            {
                throw new InvalidOperationException(
                    $"GameplayLab Foot IK virtual gamepad did not accept the next route input. Submitted={nextCameraMovement}, Device={deviceMovement}.");
            }
            m_SubmittedCameraMovement = nextCameraMovement;
            m_SubmissionInputUpdateSequence = m_InputUpdateSequence;
            m_HasSubmittedInput = true;
        }

        public FixedCharacterSimulationInput BuildInput(FixedCharacterInputBuildContext context)
        {
            RequireAlive();
            if (!m_Active || m_RenderFrame == 0 || context.ActorId != m_ActorId || context.CommittedObservation == null)
                throw new InvalidOperationException("GameplayLab Foot IK input received an incomplete build context.");
            FixedCommittedActorPose actor = context.CommittedObservation.GetRequiredActor(m_ActorId);
            Vector3 committedPosition = new Vector3(
                actor.Position.X.ToSingle(),
                actor.Position.Y.ToSingle(),
                actor.Position.Z.ToSingle());
            ulong simulationTick = context.SimulationTick.Value;
            if (m_LastCommittedSimulationTick > 0 && simulationTick > m_LastCommittedSimulationTick)
            {
                Vector2 planarDelta = new Vector2(
                    committedPosition.x - m_LastCommittedPosition.x,
                    committedPosition.z - m_LastCommittedPosition.z);
                m_LastActualPlanarSpeed = planarDelta.magnitude * m_TickRate /
                                          (simulationTick - m_LastCommittedSimulationTick);
            }
            else
            {
                m_LastActualPlanarSpeed = 0f;
            }
            m_LastCommittedPosition = committedPosition;
            m_LastCommittedYawDegrees = actor.Yaw.Degrees.ToSingle();
            m_SimulationTick = simulationTick;
            m_LastCommittedSimulationTick = simulationTick;
            GameplayLabFootIkRouteRegistry.Publish(new GameplayLabFootIkRouteSnapshot(
                m_RunId,
                m_ActorId,
                m_Phase,
                GameplayLabFootIkInputScenario.CameraRelativeTurns,
                m_TraversalSegment,
                m_Lap,
                m_RenderFrame,
                m_SimulationTick,
                m_Start,
                m_End,
                m_LastCommittedPosition,
                m_LastCameraMovement,
                m_LastCommittedYawDegrees,
                m_LastActualPlanarSpeed,
                m_TickRate));
            return m_PlayerInput.BuildInput(context);
        }

        public byte[] CaptureState()
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x524b4946);
            writer.WriteInt32(19);
            writer.WriteString(m_RouteIdentity);
            writer.WriteByte((byte)m_Phase);
            writer.WriteInt32(m_TraversalSegment);
            writer.WriteInt32(m_Lap);
            writer.WriteInt32(m_HoldTicksRemaining);
            writer.WriteInt32(m_TraversalTicksRemaining);
            writer.WriteDouble(m_LastCommittedPosition.x);
            writer.WriteDouble(m_LastCommittedPosition.y);
            writer.WriteDouble(m_LastCommittedPosition.z);
            writer.WriteDouble(m_LastCommittedYawDegrees);
            writer.WriteUInt64(m_LastCommittedSimulationTick);
            writer.WriteDouble(m_LastActualPlanarSpeed);
            writer.WriteUInt64(m_LastRouteUpdateTick);
            writer.WriteDouble(m_SubmittedCameraMovement.x);
            writer.WriteDouble(m_SubmittedCameraMovement.y);
            writer.WriteBytes(m_PlayerInput.CaptureState());
            return writer.ToArray();
        }

        public void RestoreState(byte[] state)
        {
            var reader = new CanonicalReader(state ?? throw new ArgumentNullException(nameof(state)));
            if (reader.ReadUInt32() != 0x524b4946 || reader.ReadInt32() != 19 ||
                !string.Equals(reader.ReadString(), m_RouteIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("GameplayLab Foot IK input state identity is invalid.");
            }
            var phase = (GameplayLabFootIkRoutePhase)reader.ReadByte();
            int traversalSegment = reader.ReadInt32();
            int lap = reader.ReadInt32();
            int holdTicks = reader.ReadInt32();
            int traversalTicks = reader.ReadInt32();
            var position = new Vector3(
                checked((float)reader.ReadDouble()),
                checked((float)reader.ReadDouble()),
                checked((float)reader.ReadDouble()));
            float yawDegrees = checked((float)reader.ReadDouble());
            ulong lastCommittedSimulationTick = reader.ReadUInt64();
            float actualPlanarSpeed = checked((float)reader.ReadDouble());
            ulong lastRouteUpdateTick = reader.ReadUInt64();
            var submittedCameraMovement = new Vector2(
                checked((float)reader.ReadDouble()),
                checked((float)reader.ReadDouble()));
            byte[] playerState = reader.ReadBytes();
            reader.RequireComplete();
            if (!Enum.IsDefined(typeof(GameplayLabFootIkRoutePhase), phase) ||
                traversalSegment < 0 || lap <= 0 || holdTicks < 0 || traversalTicks < 0 ||
                !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) ||
                !float.IsFinite(yawDegrees) || !float.IsFinite(actualPlanarSpeed) || actualPlanarSpeed < 0f ||
                !float.IsFinite(submittedCameraMovement.x) || !float.IsFinite(submittedCameraMovement.y) ||
                submittedCameraMovement.sqrMagnitude > 1.000001f)
                throw new InvalidOperationException("GameplayLab Foot IK input state is invalid.");
            m_PlayerInput.RestoreState(playerState);
            m_Phase = phase;
            m_TraversalSegment = traversalSegment;
            m_Lap = lap;
            m_HoldTicksRemaining = holdTicks;
            m_TraversalTicksRemaining = traversalTicks;
            m_LastCommittedPosition = position;
            m_LastCommittedYawDegrees = yawDegrees;
            m_SimulationTick = lastCommittedSimulationTick;
            m_LastCommittedSimulationTick = lastCommittedSimulationTick;
            m_LastActualPlanarSpeed = actualPlanarSpeed;
            m_LastRouteUpdateTick = lastRouteUpdateTick;
            m_SubmittedCameraMovement = submittedCameraMovement;
            if (m_Gamepad != null && m_Gamepad.added)
            {
                ApplyGamepadState(submittedCameraMovement);
                m_SubmissionInputUpdateSequence = m_InputUpdateSequence;
                m_HasSubmittedInput = true;
            }
        }

        public void NotifyStateDisposition(FixedCharacterControlSourceStateDisposition disposition)
        {
            if (!Enum.IsDefined(typeof(FixedCharacterControlSourceStateDisposition), disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition));
            m_PlayerInput.NotifyStateDisposition(disposition);
        }

        public FixedCharacterControlSourceDiagnosticsSnapshot CaptureDiagnostics() =>
            m_PlayerInput.CaptureDiagnostics();

        public bool TryGetLatchedVector2(string inputId, out Vector2 value) =>
            m_PlayerInput.TryGetLatchedVector2(inputId, out value);

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Deactivate();
            m_PlayerInput.Dispose();
            m_Disposed = true;
        }

        GameplayLabFootIkResolvedInput ResolveMovement(Vector3 position, int elapsedTicks)
        {
            Vector3 target;
            switch (m_Phase)
            {
                case GameplayLabFootIkRoutePhase.AlignStart:
                    if (ReachedAlongRoute(position, m_StartAlignment, -m_RouteDirection))
                    {
                        m_Phase = GameplayLabFootIkRoutePhase.ApproachStart;
                        target = m_Start;
                        break;
                    }
                    target = m_StartAlignment;
                    break;
                case GameplayLabFootIkRoutePhase.ApproachStart:
                    if (ReachedAlongRoute(position, m_Start, m_RouteDirection))
                    {
                        EnterHold(GameplayLabFootIkRoutePhase.SettleStart);
                        return GameplayLabFootIkResolvedInput.World(Vector2.zero);
                    }
                    target = m_Start;
                    break;
                case GameplayLabFootIkRoutePhase.SettleStart:
                    if (m_HoldTicksRemaining > elapsedTicks)
                    {
                        m_HoldTicksRemaining -= elapsedTicks;
                        return GameplayLabFootIkResolvedInput.World(Vector2.zero);
                    }
                    m_HoldTicksRemaining = 0;
                    m_Phase = GameplayLabFootIkRoutePhase.StartToEnd;
                    m_TraversalSegment = 0;
                    m_TraversalTicksRemaining = 0;
                    return ResolveCameraRelativeTurnMovement(position, 0);
                case GameplayLabFootIkRoutePhase.StartToEnd:
                    return ResolveCameraRelativeTurnMovement(position, elapsedTicks);
                default:
                    throw new InvalidOperationException("GameplayLab Foot IK route phase is invalid.");
            }
            Vector2 direction = new Vector2(target.x - position.x, target.z - position.z);
            return GameplayLabFootIkResolvedInput.World(
                direction.sqrMagnitude <= 0.000001f
                    ? Vector2.zero
                    : direction.normalized);
        }

        GameplayLabFootIkResolvedInput ResolveCameraRelativeTurnMovement(
            Vector3 position,
            int elapsedTicks)
        {
            Vector3 direction = Vector3.ProjectOnPlane(m_End - m_Start, Vector3.up).normalized;
            Vector3 first = Vector3.LerpUnclamped(
                m_Start,
                m_End,
                GameplayLabFootIkRegressionCourse.TurnStressFirstFraction);
            while (true)
            {
                switch (m_TraversalSegment)
                {
                    case 0:
                        if (!ReachedAlongRoute(position, first, direction))
                            return WorldDirection(position, first);
                        BeginTurnSegment(1);
                        elapsedTicks = 0;
                        break;
                    case 1:
                        if (RemainInTurnSegment(ref elapsedTicks))
                            return GameplayLabFootIkResolvedInput.Camera(Vector2.left);
                        BeginTurnSegment(2, 2f);
                        break;
                    case 2:
                        if (RemainInTurnSegment(ref elapsedTicks))
                            return GameplayLabFootIkResolvedInput.Camera(Vector2.right);
                        BeginTurnSegment(3);
                        break;
                    case 3:
                        if (RemainInTurnSegment(ref elapsedTicks))
                            return GameplayLabFootIkResolvedInput.Camera(Vector2.left);
                        m_TraversalSegment = 4;
                        break;
                    case 4:
                        if (!ReachedPosition(position, first))
                            return WorldDirection(position, first);
                        m_Lap++;
                        BeginTurnSegment(1);
                        elapsedTicks = 0;
                        break;
                    default:
                        throw new InvalidOperationException("GameplayLab Foot IK camera-relative turn segment is invalid.");
                }
            }
        }

        void BeginTurnSegment(int segment, float durationScale = 1f)
        {
            m_TraversalSegment = segment;
            m_TraversalTicksRemaining = Mathf.CeilToInt(
                GameplayLabFootIkRegressionCourse.TurnStressLegSeconds * durationScale * m_TickRate);
        }

        bool RemainInTurnSegment(ref int elapsedTicks)
        {
            if (m_TraversalTicksRemaining > elapsedTicks)
            {
                m_TraversalTicksRemaining -= elapsedTicks;
                elapsedTicks = 0;
                return true;
            }
            elapsedTicks = Mathf.Max(0, elapsedTicks - m_TraversalTicksRemaining);
            m_TraversalTicksRemaining = 0;
            return false;
        }

        static GameplayLabFootIkResolvedInput WorldDirection(Vector3 position, Vector3 target)
        {
            Vector2 direction = new Vector2(target.x - position.x, target.z - position.z);
            return GameplayLabFootIkResolvedInput.World(
                direction.sqrMagnitude <= 0.000001f ? Vector2.zero : direction.normalized);
        }

        bool ReachedAlongRoute(Vector3 position, Vector3 target, Vector3 direction)
        {
            Vector2 delta = new Vector2(target.x - position.x, target.z - position.z);
            if (delta.sqrMagnitude <= m_ArrivalRadius * m_ArrivalRadius)
                return true;
            return Vector3.Dot(Vector3.ProjectOnPlane(target - position, Vector3.up), direction) <= 0f;
        }

        bool ReachedPosition(Vector3 position, Vector3 target)
        {
            Vector2 delta = new Vector2(target.x - position.x, target.z - position.z);
            return delta.sqrMagnitude <= m_ArrivalRadius * m_ArrivalRadius;
        }

        void EnterHold(GameplayLabFootIkRoutePhase phase)
        {
            m_Phase = phase;
            m_HoldTicksRemaining = Mathf.CeilToInt(m_EndpointHoldSeconds * m_TickRate);
        }

        static Vector2 ToCameraRelative(Vector2 worldMovement, CameraBasisSnapshot basis)
        {
            if (worldMovement.sqrMagnitude <= 0.000001f)
                return Vector2.zero;
            if (!basis.Valid)
                throw new InvalidOperationException("GameplayLab Foot IK input requires a valid camera basis.");
            Vector3 forward = Vector3.ProjectOnPlane(basis.PlanarForward, Vector3.up);
            Vector3 right = Vector3.ProjectOnPlane(basis.PlanarRight, Vector3.up);
            if (forward.sqrMagnitude <= 0.000001f || right.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException("GameplayLab Foot IK input received a degenerate camera basis.");
            Vector3 world = new Vector3(worldMovement.x, 0f, worldMovement.y);
            Vector2 cameraRelative = new Vector2(
                Vector3.Dot(world, right.normalized),
                Vector3.Dot(world, forward.normalized));
            return cameraRelative.sqrMagnitude > 1f ? cameraRelative.normalized : cameraRelative;
        }

        static Vector2 ToWorldRelative(Vector2 cameraMovement, CameraBasisSnapshot basis)
        {
            if (cameraMovement.sqrMagnitude <= 0.000001f)
                return Vector2.zero;
            if (!basis.Valid)
                throw new InvalidOperationException("GameplayLab Foot IK input requires a valid camera basis.");
            Vector3 forward = Vector3.ProjectOnPlane(basis.PlanarForward, Vector3.up);
            Vector3 right = Vector3.ProjectOnPlane(basis.PlanarRight, Vector3.up);
            if (forward.sqrMagnitude <= 0.000001f || right.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException("GameplayLab Foot IK input received a degenerate camera basis.");
            Vector3 direction = right.normalized * cameraMovement.x + forward.normalized * cameraMovement.y;
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();
            return new Vector2(direction.x, direction.z);
        }

        void ApplyGamepadState(Vector2 movement)
        {
            InputState.Change(
                m_Gamepad,
                new GamepadState { leftStick = movement },
                InputState.currentUpdateType);
        }

        void OnAfterInputUpdate()
        {
            m_InputUpdateSequence++;
            if (m_InputUpdateSequence == 0)
                throw new OverflowException("GameplayLab Foot IK input update sequence overflowed.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(GameplayLabFootIkInputSystemRuntime));
        }
    }
}
