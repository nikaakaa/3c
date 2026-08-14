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
using UnityEngine.SceneManagement;
using FixedCommittedActorPose = ThirdPersonSimulation.CommittedActorPose<ThirdPersonSimulation.Fixed.FixedVector3, ThirdPersonSimulation.Fixed.FixedYaw>;
using FixedCharacterSimulationInput = ThirdPersonSimulation.Fixed.CharacterSimulationInput;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public enum GameplayLabFootIkRoutePhase : byte
    {
        AlignStart = 1,
        ApproachStart = 2,
        HoldStart = 3,
        StartToEnd = 4,
        HoldEnd = 5,
        EndToStart = 6,
        ExitEnd = 7,
        ApproachEnd = 8,
        ExitStart = 9,
        SettleStart = 10,
        SettleEnd = 11,
        TurnLeft = 12,
        TurnRight = 13
    }

    public readonly struct GameplayLabFootIkRouteSnapshot
    {
        public GameplayLabFootIkRouteSnapshot(
            string runId,
            ActorId actorId,
            GameplayLabFootIkRoutePhase phase,
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
        public string Direction => Phase switch
        {
            GameplayLabFootIkRoutePhase.StartToEnd => "start-to-end",
            GameplayLabFootIkRoutePhase.EndToStart => "end-to-start",
            GameplayLabFootIkRoutePhase.HoldStart => "hold-start",
            GameplayLabFootIkRoutePhase.HoldEnd => "hold-end",
            GameplayLabFootIkRoutePhase.AlignStart => "align-start",
            GameplayLabFootIkRoutePhase.ApproachStart => "approach-start",
            GameplayLabFootIkRoutePhase.ExitEnd => "exit-end",
            GameplayLabFootIkRoutePhase.ApproachEnd => "approach-end",
            GameplayLabFootIkRoutePhase.ExitStart => "exit-start",
            GameplayLabFootIkRoutePhase.SettleStart => "settle-start",
            GameplayLabFootIkRoutePhase.SettleEnd => "settle-end",
            GameplayLabFootIkRoutePhase.TurnLeft => "turn-left",
            GameplayLabFootIkRoutePhase.TurnRight => "turn-right",
            _ => throw new InvalidOperationException("GameplayLab Foot IK route phase is invalid.")
        };
        public bool IsTraversal =>
            Phase == GameplayLabFootIkRoutePhase.StartToEnd ||
            Phase == GameplayLabFootIkRoutePhase.EndToStart;
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
        [SerializeField] string m_StartMarkerName = "teststart";
        [SerializeField] string m_EndMarkerName = "testend";
        [SerializeField] CharacterInputProfile m_InputProfile;
        [SerializeField] string m_ActionTargetInputValueId;
        [SerializeField] CharacterActionTargetInputProvider m_ActionTargetProvider;
        [SerializeField] string m_MoveInputValueId = "MoveAxis";
        [SerializeField, Min(0.05f)] float m_ArrivalRadius = 0.18f;
        [SerializeField, Min(0f)] float m_EndpointHoldSeconds = 0.75f;

        public override string SourceIdentity =>
            $"gameplay-lab-foot-ik/turn-v1/{m_StartMarkerName}/{m_EndMarkerName}/{m_MoveInputValueId}/{(m_InputProfile ? m_InputProfile.name : "unconfigured")}";

        public override IUnityFixedCharacterControlSourceRuntime Create(FixedCharacterControlSourceContext context)
        {
            Vector3 start = ResolveMarker(gameObject.scene, m_StartMarkerName);
            Vector3 end = ResolveMarker(gameObject.scene, m_EndMarkerName);
            if (Vector3.ProjectOnPlane(end - start, Vector3.up).sqrMagnitude <= 1f)
                throw new InvalidOperationException("GameplayLab Foot IK route endpoints are too close.");
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
            string startMarkerName,
            string endMarkerName,
            CharacterInputProfile inputProfile,
            string actionTargetInputValueId,
            CharacterActionTargetInputProvider actionTargetProvider,
            string moveInputValueId,
            float arrivalRadius,
            float endpointHoldSeconds)
        {
            m_StartMarkerName = Require(startMarkerName, nameof(startMarkerName));
            m_EndMarkerName = Require(endMarkerName, nameof(endMarkerName));
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

        static Vector3 ResolveMarker(Scene scene, string markerName)
        {
            string requiredName = Require(markerName, nameof(markerName));
            GameObject[] roots = scene.GetRootGameObjects();
            Transform found = null;
            for (int i = 0; i < roots.Length; i++)
                FindMarker(roots[i].transform, requiredName, ref found);
            if (!found)
                throw new InvalidOperationException($"GameplayLab Foot IK route marker '{requiredName}' was not found in scene '{scene.path}'.");
            return found.position;
        }

        static void FindMarker(Transform value, string markerName, ref Transform found)
        {
            if (string.Equals(value.name, markerName, StringComparison.Ordinal))
            {
                if (found)
                    throw new InvalidOperationException($"GameplayLab Foot IK route marker '{markerName}' is duplicated.");
                found = value;
            }
            for (int i = 0; i < value.childCount; i++)
                FindMarker(value.GetChild(i), markerName, ref found);
        }

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
        const float TurnVerificationSeconds = 1.2f;
        readonly ActorId m_ActorId;
        readonly string m_RouteIdentity;
        readonly string m_MoveInputValueId;
        readonly Vector3 m_Start;
        readonly Vector3 m_End;
        readonly Vector3 m_StartAlignment;
        readonly Vector3 m_EndAlignment;
        readonly Vector3 m_RouteDirection;
        readonly float m_ArrivalRadius;
        readonly float m_EndpointHoldSeconds;
        readonly int m_TickRate;
        readonly FixedCharacterHost m_Owner;
        readonly UnityFixedCharacterInputAdapter m_PlayerInput;
        readonly string m_RunId = Guid.NewGuid().ToString("N");
        GameplayLabFootIkRoutePhase m_Phase = GameplayLabFootIkRoutePhase.AlignStart;
        int m_Lap;
        int m_HoldTicksRemaining;
        ulong m_RenderFrame;
        ulong m_SimulationTick;
        ulong m_LastRouteUpdateTick;
        ulong m_LastCommittedSimulationTick;
        Vector3 m_LastCommittedPosition;
        Vector2 m_LastWorldMovement;
        Vector2 m_LastCameraMovement;
        float m_LastCommittedYawDegrees;
        float m_LastActualPlanarSpeed;
        Gamepad m_Gamepad;
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
            float alignmentDistance = Mathf.Min(8f, route.magnitude * 0.5f);
            m_RouteDirection = route.normalized;
            m_StartAlignment = start - m_RouteDirection * alignmentDistance;
            m_EndAlignment = end + m_RouteDirection * alignmentDistance;
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
            }
            catch
            {
                InputSystem.RemoveDevice(m_Gamepad);
                m_Gamepad = null;
                throw;
            }
            m_Active = true;
            m_Phase = GameplayLabFootIkRoutePhase.TurnLeft;
            m_HoldTicksRemaining = Mathf.CeilToInt(TurnVerificationSeconds * m_TickRate);
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;
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
            m_Phase = GameplayLabFootIkRoutePhase.AlignStart;
            m_Lap = 0;
            m_HoldTicksRemaining = 0;
            GameplayLabFootIkRouteRegistry.Remove(m_ActorId);
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Active || renderFrame == 0 || renderFrame <= m_RenderFrame)
                throw new InvalidOperationException("GameplayLab Foot IK input requires an active, strictly increasing render frame.");
            m_RenderFrame = renderFrame;
            ulong elapsedTickCount = m_SimulationTick > m_LastRouteUpdateTick
                ? m_SimulationTick - m_LastRouteUpdateTick
                : 0UL;
            int elapsedTicks = elapsedTickCount > int.MaxValue ? int.MaxValue : (int)elapsedTickCount;
            m_LastRouteUpdateTick = m_SimulationTick;
            m_LastWorldMovement = ResolveMovement(m_LastCommittedPosition, elapsedTicks);
            m_LastCameraMovement = ToCameraRelative(m_LastWorldMovement, m_Owner.CameraRig.BasisSnapshot);
            if (m_Gamepad == null || !m_Gamepad.added)
                throw new InvalidOperationException("GameplayLab Foot IK virtual gamepad is unavailable.");
            Vector2 submittedCameraMovement = m_LastCameraMovement;
            m_Gamepad.MakeCurrent();
            ApplyGamepadState(submittedCameraMovement);
            Vector2 deviceMovement = m_Gamepad.leftStick.ReadUnprocessedValue();
            if ((deviceMovement - submittedCameraMovement).sqrMagnitude > 0.000001f)
            {
                throw new InvalidOperationException(
                    $"GameplayLab Foot IK virtual gamepad did not accept the route input. Submitted={submittedCameraMovement}, Device={deviceMovement}.");
            }
            m_PlayerInput.CaptureRenderFrame(renderFrame);
            if (!m_PlayerInput.TryGetLatchedVector2(m_MoveInputValueId, out Vector2 value))
                throw new InvalidOperationException("GameplayLab Foot IK could not read Corin's formal Move Input Action.");
            if (submittedCameraMovement.sqrMagnitude > 0.000001f && value.sqrMagnitude <= 0.000001f)
                throw new InvalidOperationException("GameplayLab Foot IK formal Move Input Action ignored the virtual gamepad.");
            m_LastCameraMovement = value;
            m_LastWorldMovement = ToWorldRelative(value, m_Owner.CameraRig.BasisSnapshot);
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
                m_Lap,
                m_RenderFrame,
                m_SimulationTick,
                m_Start,
                m_End,
                m_LastCommittedPosition,
                m_LastWorldMovement,
                m_LastCommittedYawDegrees,
                m_LastActualPlanarSpeed,
                m_TickRate));
            return m_PlayerInput.BuildInput(context);
        }

        public byte[] CaptureState()
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x524b4946);
            writer.WriteInt32(12);
            writer.WriteString(m_RouteIdentity);
            writer.WriteByte((byte)m_Phase);
            writer.WriteInt32(m_Lap);
            writer.WriteInt32(m_HoldTicksRemaining);
            writer.WriteDouble(m_LastCommittedPosition.x);
            writer.WriteDouble(m_LastCommittedPosition.y);
            writer.WriteDouble(m_LastCommittedPosition.z);
            writer.WriteDouble(m_LastCommittedYawDegrees);
            writer.WriteUInt64(m_LastCommittedSimulationTick);
            writer.WriteDouble(m_LastActualPlanarSpeed);
            writer.WriteUInt64(m_LastRouteUpdateTick);
            writer.WriteBytes(m_PlayerInput.CaptureState());
            return writer.ToArray();
        }

        public void RestoreState(byte[] state)
        {
            var reader = new CanonicalReader(state ?? throw new ArgumentNullException(nameof(state)));
            if (reader.ReadUInt32() != 0x524b4946 || reader.ReadInt32() != 12 ||
                !string.Equals(reader.ReadString(), m_RouteIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("GameplayLab Foot IK input state identity is invalid.");
            }
            var phase = (GameplayLabFootIkRoutePhase)reader.ReadByte();
            int lap = reader.ReadInt32();
            int holdTicks = reader.ReadInt32();
            var position = new Vector3(
                checked((float)reader.ReadDouble()),
                checked((float)reader.ReadDouble()),
                checked((float)reader.ReadDouble()));
            float yawDegrees = checked((float)reader.ReadDouble());
            ulong lastCommittedSimulationTick = reader.ReadUInt64();
            float actualPlanarSpeed = checked((float)reader.ReadDouble());
            ulong lastRouteUpdateTick = reader.ReadUInt64();
            byte[] playerState = reader.ReadBytes();
            reader.RequireComplete();
            if (!Enum.IsDefined(typeof(GameplayLabFootIkRoutePhase), phase) || lap < 0 || holdTicks < 0 ||
                !float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z) ||
                !float.IsFinite(yawDegrees) || !float.IsFinite(actualPlanarSpeed) || actualPlanarSpeed < 0f)
                throw new InvalidOperationException("GameplayLab Foot IK input state is invalid.");
            m_PlayerInput.RestoreState(playerState);
            m_Phase = phase;
            m_Lap = lap;
            m_HoldTicksRemaining = holdTicks;
            m_LastCommittedPosition = position;
            m_LastCommittedYawDegrees = yawDegrees;
            m_SimulationTick = lastCommittedSimulationTick;
            m_LastCommittedSimulationTick = lastCommittedSimulationTick;
            m_LastActualPlanarSpeed = actualPlanarSpeed;
            m_LastRouteUpdateTick = lastRouteUpdateTick;
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

        Vector2 ResolveMovement(Vector3 position, int elapsedTicks)
        {
            Vector3 target;
            switch (m_Phase)
            {
                case GameplayLabFootIkRoutePhase.TurnLeft:
                    if (m_HoldTicksRemaining > elapsedTicks)
                    {
                        m_HoldTicksRemaining -= elapsedTicks;
                        return ResolveTurnMovement(-1f);
                    }
                    m_Phase = GameplayLabFootIkRoutePhase.TurnRight;
                    m_HoldTicksRemaining = Mathf.CeilToInt(TurnVerificationSeconds * m_TickRate);
                    return ResolveTurnMovement(1f);
                case GameplayLabFootIkRoutePhase.TurnRight:
                    if (m_HoldTicksRemaining > elapsedTicks)
                    {
                        m_HoldTicksRemaining -= elapsedTicks;
                        return ResolveTurnMovement(1f);
                    }
                    m_HoldTicksRemaining = 0;
                    m_Phase = GameplayLabFootIkRoutePhase.AlignStart;
                    target = m_StartAlignment;
                    break;
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
                        if (m_Lap == 0)
                            m_Lap = 1;
                        EnterHold(GameplayLabFootIkRoutePhase.SettleStart);
                        return Vector2.zero;
                    }
                    target = m_Start;
                    break;
                case GameplayLabFootIkRoutePhase.HoldStart:
                    if (m_HoldTicksRemaining > elapsedTicks)
                    {
                        m_HoldTicksRemaining -= elapsedTicks;
                        return Vector2.zero;
                    }
                    m_HoldTicksRemaining = 0;
                    m_Phase = GameplayLabFootIkRoutePhase.ApproachStart;
                    target = m_Start;
                    break;
                case GameplayLabFootIkRoutePhase.SettleStart:
                    if (m_HoldTicksRemaining > elapsedTicks)
                    {
                        m_HoldTicksRemaining -= elapsedTicks;
                        return Vector2.zero;
                    }
                    m_HoldTicksRemaining = 0;
                    m_Phase = GameplayLabFootIkRoutePhase.StartToEnd;
                    target = m_End;
                    break;
                case GameplayLabFootIkRoutePhase.StartToEnd:
                    if (ReachedAlongRoute(position, m_End, m_RouteDirection))
                    {
                        m_Phase = GameplayLabFootIkRoutePhase.ExitEnd;
                        target = m_EndAlignment;
                        break;
                    }
                    target = m_End;
                    break;
                case GameplayLabFootIkRoutePhase.ExitEnd:
                    if (ReachedAlongRoute(position, m_EndAlignment, m_RouteDirection))
                    {
                        EnterHold(GameplayLabFootIkRoutePhase.HoldEnd);
                        return Vector2.zero;
                    }
                    target = m_EndAlignment;
                    break;
                case GameplayLabFootIkRoutePhase.HoldEnd:
                    if (m_HoldTicksRemaining > elapsedTicks)
                    {
                        m_HoldTicksRemaining -= elapsedTicks;
                        return Vector2.zero;
                    }
                    m_HoldTicksRemaining = 0;
                    m_Phase = GameplayLabFootIkRoutePhase.ApproachEnd;
                    target = m_End;
                    break;
                case GameplayLabFootIkRoutePhase.ApproachEnd:
                    if (ReachedAlongRoute(position, m_End, -m_RouteDirection))
                    {
                        EnterHold(GameplayLabFootIkRoutePhase.SettleEnd);
                        return Vector2.zero;
                    }
                    target = m_End;
                    break;
                case GameplayLabFootIkRoutePhase.SettleEnd:
                    if (m_HoldTicksRemaining > elapsedTicks)
                    {
                        m_HoldTicksRemaining -= elapsedTicks;
                        return Vector2.zero;
                    }
                    m_HoldTicksRemaining = 0;
                    m_Phase = GameplayLabFootIkRoutePhase.EndToStart;
                    target = m_Start;
                    break;
                case GameplayLabFootIkRoutePhase.EndToStart:
                    if (ReachedAlongRoute(position, m_Start, -m_RouteDirection))
                    {
                        m_Phase = GameplayLabFootIkRoutePhase.ExitStart;
                        target = m_StartAlignment;
                        break;
                    }
                    target = m_Start;
                    break;
                case GameplayLabFootIkRoutePhase.ExitStart:
                    if (ReachedAlongRoute(position, m_StartAlignment, -m_RouteDirection))
                    {
                        m_Lap++;
                        EnterHold(GameplayLabFootIkRoutePhase.HoldStart);
                        return Vector2.zero;
                    }
                    target = m_StartAlignment;
                    break;
                default:
                    throw new InvalidOperationException("GameplayLab Foot IK route phase is invalid.");
            }
            Vector2 direction = new Vector2(target.x - position.x, target.z - position.z);
            return direction.sqrMagnitude <= 0.000001f
                ? Vector2.zero
                : direction.normalized;
        }

        Vector2 ResolveTurnMovement(float horizontal)
        {
            Vector2 cameraMovement = new Vector2(horizontal, 1f).normalized;
            return ToWorldRelative(cameraMovement, m_Owner.CameraRig.BasisSnapshot);
        }

        bool ReachedAlongRoute(Vector3 position, Vector3 target, Vector3 direction)
        {
            Vector2 delta = new Vector2(target.x - position.x, target.z - position.z);
            if (delta.sqrMagnitude <= m_ArrivalRadius * m_ArrivalRadius)
                return true;
            return Vector3.Dot(Vector3.ProjectOnPlane(target - position, Vector3.up), direction) <= 0f;
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

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(GameplayLabFootIkInputSystemRuntime));
        }
    }
}
