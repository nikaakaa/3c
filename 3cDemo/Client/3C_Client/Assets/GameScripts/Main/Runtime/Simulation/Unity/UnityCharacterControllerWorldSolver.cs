using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public sealed class UnityCharacterControllerWorldSolver : ICharacterWorldSolver
    {
        const float PositionTolerance = 0.0001f;
        const float RotationTolerance = 0.01f;
        static readonly SolverImplementationId Implementation = new SolverImplementationId("Unity.CharacterController.WorldSolver");
        readonly List<UnityCharacterControllerWorldBodyBinding> m_Bindings;
        readonly int m_TickRate;
        WorldSimulationState m_Current;
        bool m_Disposed;

        public UnityCharacterControllerWorldSolver(
            int tickRate,
            IEnumerable<UnityCharacterControllerWorldBodyBinding> bindings)
        {
            if (tickRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickRate));
            m_TickRate = tickRate;
            m_Bindings = bindings == null
                ? new List<UnityCharacterControllerWorldBodyBinding>()
                : new List<UnityCharacterControllerWorldBodyBinding>(bindings);
            if (m_Bindings.Count == 0)
                throw new ArgumentException("Unity World Solver requires explicit body bindings.", nameof(bindings));
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (!m_Bindings[i])
                    throw new ArgumentException("Unity World Solver contains a missing body binding.", nameof(bindings));
            }
            m_Bindings.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            var bindingIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                UnityCharacterControllerWorldBodyBinding binding = m_Bindings[i];
                binding.RequireValid();
                if (!bindingIds.Add(binding.BindingId) || i > 0 && m_Bindings[i - 1].ActorId == binding.ActorId)
                    throw new ArgumentException($"Unity World Solver contains duplicate binding or ActorId '{binding.ActorId}'.", nameof(bindings));
            }
            Descriptor = new CharacterWorldSolverDescriptor(
                Float32SimulationNumericProfile.Value,
                Implementation,
                "2",
                WorldCapability.BodyMotion | WorldCapability.Grounding | WorldCapability.Collision |
                WorldCapability.Reconstructible | WorldCapability.AirborneVerticalMotion,
                WorldFeature.Ground | WorldFeature.Slope | WorldFeature.Step | WorldFeature.WallSlide);
        }

        public CharacterWorldSolverDescriptor Descriptor { get; }

        public void RequireBodyBinding(ActorId actorId, string bindingId)
        {
            RequireAlive();
            string requiredBindingId = string.IsNullOrWhiteSpace(bindingId)
                ? throw new ArgumentException("World body binding identity is missing.", nameof(bindingId))
                : bindingId.Trim();
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                UnityCharacterControllerWorldBodyBinding binding = m_Bindings[i];
                if (binding.ActorId != actorId)
                    continue;
                if (!string.Equals(binding.BindingId, requiredBindingId, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Actor '{actorId}' expects World binding '{requiredBindingId}', but Solver owns '{binding.BindingId}'.");
                return;
            }
            throw new InvalidOperationException($"Unity World Solver has no body binding for Actor '{actorId}'.");
        }

        public WorldSimulationState Create(WorldRevision worldRevision, IReadOnlyList<WorldBodyState> orderedInitialBodies)
        {
            RequireAlive();
            if (string.IsNullOrEmpty(worldRevision.Value))
                throw new ArgumentException("World revision is missing.", nameof(worldRevision));
            WorldSimulationState state = CreateState(worldRevision, orderedInitialBodies);
            Reconstruct(state);
            return m_Current;
        }

        public void Reconstruct(WorldSimulationState state)
        {
            RequireAlive();
            ValidateState(state);
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                UnityCharacterControllerWorldBodyBinding binding = m_Bindings[i];
                WorldBodyState body = state.Bodies[i];
                CharacterController controller = binding.CharacterController;
                bool enabled = controller.enabled;
                controller.enabled = false;
                binding.LogicRoot.SetPositionAndRotation(
                    ToUnity(body.Position),
                    Quaternion.Euler(0f, body.Yaw.Degrees.ToSingle(), 0f));
                controller.enabled = enabled;
            }
            m_Current = CloneState(state);
        }

        public WorldSimulationState Capture(WorldRevision worldRevision)
        {
            RequireAlive();
            RequireCurrent();
            if (!worldRevision.Equals(m_Current.WorldRevision))
                throw new InvalidOperationException("Unity World Solver cannot capture another WorldRevision.");
            RequireSceneMatches(m_Current);
            return CloneState(m_Current);
        }

        public void Restore(WorldSimulationState state)
        {
            Reconstruct(state);
        }

        public WorldSolveBatchResult ResolveBatch(WorldSolveBatchRequest request, ISimulationDiagnosticsSink diagnostics)
        {
            RequireAlive();
            RequireCurrent();
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));
            if (!StateEquals(request.BeforeWorldState, m_Current))
                throw new InvalidOperationException("Unity World Solver request does not match its current explicit World state.");
            if (!Descriptor.Supports(request.RequiredCapabilities))
                throw new InvalidOperationException($"Unity World Solver is missing required capabilities '{request.RequiredCapabilities & ~Descriptor.Capabilities}'.");
            RequireSceneMatches(m_Current);

            try
            {
                Float32Scalar delta = Float32Scalar.One / Float32Scalar.FromInt64(m_TickRate);
                var bodies = new WorldBodyState[request.Requests.Count];
                var results = new CharacterWorldSolveResult[request.Requests.Count];
                for (int i = 0; i < request.Requests.Count; i++)
                {
                    CharacterWorldSolveRequest actorRequest = request.Requests[i];
                    UnityCharacterControllerWorldBodyBinding binding = m_Bindings[i];
                    if (binding.ActorId != actorRequest.ActorId || !BodyEquals(actorRequest.BeforeBody, m_Current.Bodies[i]))
                        throw new InvalidOperationException("Unity World Solver Actor request does not match its locked body binding.");
                    WorldBodyState before = actorRequest.BeforeBody;
                    Float32Vector3 requestedDisplacement = actorRequest.Motion.Space == WorldMotionSpace.ActorLocal
                        ? Float32Angle.RotatePlanar(actorRequest.Motion.Displacement, before.Yaw)
                        : actorRequest.Motion.Displacement;
                    Vector3 beforePosition = binding.LogicRoot.position;
                    CollisionFlags flags = CollisionFlags.None;
                    if (actorRequest.Motion.HasMotion)
                    {
                        flags = binding.CharacterController.Move(ToUnity(requestedDisplacement));
                        if (actorRequest.Motion.YawDegrees != Float32Scalar.Zero)
                        {
                            binding.LogicRoot.rotation = Quaternion.AngleAxis(
                                actorRequest.Motion.YawDegrees.ToSingle(),
                                Vector3.up) * binding.LogicRoot.rotation;
                        }
                    }
                    Vector3 appliedUnity = binding.LogicRoot.position - beforePosition;
                    Float32Vector3 applied = ToSimulation(appliedUnity, $"{binding.BindingId}/applied-displacement");
                    Float32Yaw finalYaw = new Float32Yaw(Float32ScalarBoundary.ConvertExternal(
                        NormalizeSignedYaw(binding.LogicRoot.eulerAngles.y),
                        $"{binding.BindingId}/yaw"));
                    Float32Scalar appliedYaw = Float32Angle.Delta(before.Yaw, finalYaw);
                    bodies[i] = CharacterBodyMotionRuntime.Finalize(
                        before,
                        actorRequest.BodyMotionPlan,
                        ToSimulation(binding.LogicRoot.position, $"{binding.BindingId}/position"),
                        finalYaw,
                        applied,
                        binding.CharacterController.isGrounded,
                        Convert(flags),
                        delta);
                    results[i] = new CharacterWorldSolveResult(
                        Descriptor.NumericProfile,
                        actorRequest.ActorId,
                        actorRequest.RequestId,
                        request.Tick,
                        Descriptor.ImplementationId,
                        bodies[i],
                        applied,
                        appliedYaw);
                }
                m_Current = new WorldSimulationState(
                    Descriptor.NumericProfile,
                    Descriptor.ImplementationId,
                    Descriptor.Version,
                    request.BeforeWorldState.WorldRevision,
                    WorldStatePersistenceMode.Reconstruct,
                    bodies,
                    Array.Empty<byte>());
                return new WorldSolveBatchResult(request, Descriptor.ImplementationId, Descriptor.Version, CloneState(m_Current), results);
            }
            catch
            {
                Reconstruct(request.BeforeWorldState);
                throw;
            }
        }

        WorldSimulationState CreateState(WorldRevision worldRevision, IReadOnlyList<WorldBodyState> bodies)
        {
            if (bodies == null || bodies.Count != m_Bindings.Count)
                throw new ArgumentException("Initial body roster does not match Unity World Solver bindings.", nameof(bodies));
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodies[i].ActorId != m_Bindings[i].ActorId)
                    throw new ArgumentException("Initial body order does not match Unity World Solver bindings.", nameof(bodies));
            }
            return new WorldSimulationState(
                Descriptor.NumericProfile,
                Descriptor.ImplementationId,
                Descriptor.Version,
                worldRevision,
                WorldStatePersistenceMode.Reconstruct,
                bodies,
                Array.Empty<byte>());
        }

        void ValidateState(WorldSimulationState state)
        {
            if (state == null ||
                !state.SolverId.Equals(Descriptor.ImplementationId) ||
                state.NumericProfile != Descriptor.NumericProfile ||
                !string.Equals(state.SolverVersion, Descriptor.Version, StringComparison.Ordinal) ||
                state.PersistenceMode != WorldStatePersistenceMode.Reconstruct ||
                state.SolverStatePayload.Length != 0 ||
                state.Bodies.Count != m_Bindings.Count)
                throw new InvalidOperationException("World state is incompatible with Unity CharacterController Solver.");
            for (int i = 0; i < state.Bodies.Count; i++)
            {
                if (state.Bodies[i].ActorId != m_Bindings[i].ActorId)
                    throw new InvalidOperationException("World state Actor order does not match Unity World Solver bindings.");
            }
        }

        void RequireSceneMatches(WorldSimulationState state)
        {
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                Transform root = m_Bindings[i].LogicRoot;
                WorldBodyState body = state.Bodies[i];
                if (Vector3.Distance(root.position, ToUnity(body.Position)) > PositionTolerance ||
                    Quaternion.Angle(root.rotation, Quaternion.Euler(0f, body.Yaw.Degrees.ToSingle(), 0f)) > RotationTolerance)
                    throw new InvalidOperationException($"Unity body '{m_Bindings[i].BindingId}' diverged from explicit World state.");
            }
        }

        static WorldSimulationState CloneState(WorldSimulationState state)
        {
            return new WorldSimulationState(
                state.NumericProfile,
                state.SolverId,
                state.SolverVersion,
                state.WorldRevision,
                state.PersistenceMode,
                state.Bodies,
                state.SolverStatePayload.ToArray());
        }

        static bool StateEquals(WorldSimulationState left, WorldSimulationState right)
        {
            if (left == null || right == null ||
                !left.SolverId.Equals(right.SolverId) ||
                left.NumericProfile != right.NumericProfile ||
                !string.Equals(left.SolverVersion, right.SolverVersion, StringComparison.Ordinal) ||
                !left.WorldRevision.Equals(right.WorldRevision) ||
                left.PersistenceMode != right.PersistenceMode ||
                left.Bodies.Count != right.Bodies.Count)
                return false;
            for (int i = 0; i < left.Bodies.Count; i++)
            {
                if (!BodyEquals(left.Bodies[i], right.Bodies[i]))
                    return false;
            }
            return true;
        }

        static bool BodyEquals(WorldBodyState left, WorldBodyState right)
        {
            return left.ActorId == right.ActorId &&
                   left.Position == right.Position &&
                   left.Yaw == right.Yaw &&
                   left.Velocity == right.Velocity &&
                   left.VerticalVelocity == right.VerticalVelocity &&
                   left.Grounded == right.Grounded &&
                   left.Collision == right.Collision;
        }

        static Vector3 ToUnity(Float32Vector3 value)
        {
            return new Vector3(value.X.ToSingle(), value.Y.ToSingle(), value.Z.ToSingle());
        }

        static Float32Vector3 ToSimulation(Vector3 value, string identity)
        {
            return new Float32Vector3(
                Float32ScalarBoundary.ConvertExternal(value.x, $"{identity}/x"),
                Float32ScalarBoundary.ConvertExternal(value.y, $"{identity}/y"),
                Float32ScalarBoundary.ConvertExternal(value.z, $"{identity}/z"));
        }

        static WorldCollisionSummary Convert(CollisionFlags flags)
        {
            WorldCollisionSummary result = WorldCollisionSummary.None;
            if ((flags & CollisionFlags.Sides) != 0)
                result |= WorldCollisionSummary.Sides;
            if ((flags & CollisionFlags.Above) != 0)
                result |= WorldCollisionSummary.Above;
            if ((flags & CollisionFlags.Below) != 0)
                result |= WorldCollisionSummary.Below;
            return result;
        }

        static float NormalizeSignedYaw(float yaw)
        {
            yaw %= 360f;
            if (yaw >= 180f)
                yaw -= 360f;
            if (yaw < -180f)
                yaw += 360f;
            return yaw;
        }

        void RequireCurrent()
        {
            if (m_Current == null)
                throw new InvalidOperationException("Unity World Solver has not been created or reconstructed.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(UnityCharacterControllerWorldSolver));
        }

        public void Dispose()
        {
            m_Disposed = true;
            m_Current = null;
            m_Bindings.Clear();
        }
    }
}
