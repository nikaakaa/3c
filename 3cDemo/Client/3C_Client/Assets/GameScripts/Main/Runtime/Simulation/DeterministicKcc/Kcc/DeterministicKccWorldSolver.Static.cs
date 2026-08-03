using System;
using System.Collections.Generic;
using System.Diagnostics;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicKcc
{
    public sealed partial class DeterministicKccWorldSolver
    {
        void ReapplyStaticWorldConstraints(int index)
        {
            ActorSolveCandidate candidate = m_Candidates[index];
            DeterministicKccMotorResult result;
            try
            {
                result = m_Motors[index].ReconstraintAfterMovement(
                    candidate.Position,
                    candidate.PreviousState,
                    candidate.Requested,
                    candidate.Ground.LastMovementIterationFoundAnyGround);
            }
            catch (DeterministicKccQueryException exception)
            {
                throw new DeterministicActorContactSolveException(
                    $"Actor '{candidate.Request.ActorId}' static reconstraint failed: {exception.Message}",
                    Array.Empty<DeterministicActorContactTrace>());
            }
            candidate.QuerySummary = candidate.QuerySummary.Add(result.QuerySummary);
            candidate.Position = result.Position;
            candidate.Ground = result.Ground;
            candidate.Collision |= result.Collision;
            RequireInsideWorld(candidate.Position);
            m_Candidates[index] = candidate;
        }

        void ValidateStaticWorldResult(int index)
        {
            ActorSolveCandidate candidate = m_Candidates[index];
            DeterministicKccQuerySummary summary = default;
            try
            {
                m_Motors[index].ValidatePose(candidate.Position, ref summary);
            }
            catch (DeterministicKccQueryException exception)
            {
                throw new DeterministicActorContactSolveException(
                    $"Actor '{candidate.Request.ActorId}' final static validation failed: {exception.Message}",
                    Array.Empty<DeterministicActorContactTrace>());
            }
            candidate.QuerySummary = candidate.QuerySummary.Add(summary);
            RequireInsideWorld(candidate.Position);
            m_Candidates[index] = candidate;
        }

        void ApplyActorContactResult(DeterministicActorContactBatchResult result)
        {
            if (result.Count != m_Candidates.Length)
                throw new InvalidOperationException("Deterministic Actor contact result roster is invalid.");
            for (int i = 0; i < m_Candidates.Length; i++)
            {
                ActorSolveCandidate candidate = m_Candidates[i];
                candidate.Position = result.PositionAt(i);
                if (result.HadContactAt(i))
                    candidate.Collision |= WorldCollisionSummary.Sides;
                m_Candidates[i] = candidate;
            }
        }

        void FillCandidatePositions()
        {
            for (int i = 0; i < m_Candidates.Length; i++)
                m_CandidatePositions[i] = m_Candidates[i].Position;
        }

        void AppendTraces(IReadOnlyList<DeterministicActorContactTrace> source)
        {
            if (source == null)
                return;
            if (m_ContactTraces.Count + source.Count > m_ContactTraces.Capacity)
            {
                throw new DeterministicActorContactSolveException(
                    $"Deterministic Actor contact trace capacity '{m_ContactTraces.Capacity}' was exceeded.",
                    Array.Empty<DeterministicActorContactTrace>());
            }
            for (int i = 0; i < source.Count; i++)
                m_ContactTraces.Add(source[i]);
        }

        WorldSimulationState CreateState(
            WorldRevision revision,
            IReadOnlyList<WorldBodyState> bodies,
            IReadOnlyList<DeterministicKccBodyState> states)
        {
            byte[] payload = DeterministicKccStateCodec.Write(
                m_CollisionWorld.ContentHash,
                m_Configuration.ConfigurationHash,
                states);
            return new WorldSimulationState(
                Descriptor.NumericProfile,
                Descriptor.ImplementationId,
                Descriptor.Version,
                revision,
                WorldStatePersistenceMode.Snapshot,
                bodies,
                payload);
        }

        void ValidateState(WorldSimulationState state)
        {
            if (state == null || state.NumericProfile != Descriptor.NumericProfile ||
                !state.SolverId.Equals(Descriptor.ImplementationId) ||
                !string.Equals(state.SolverVersion, Descriptor.Version, StringComparison.Ordinal) ||
                state.PersistenceMode != WorldStatePersistenceMode.Snapshot || state.Bodies.Count != m_Bindings.Length)
            {
                throw new InvalidOperationException("World state is incompatible with Deterministic KCC.");
            }
            ValidateBodies(state.Bodies);
        }

        void ValidateBodies(IReadOnlyList<WorldBodyState> bodies)
        {
            if (bodies == null || bodies.Count != m_Bindings.Length)
                throw new ArgumentException("World body roster does not match Deterministic KCC bindings.", nameof(bodies));
            for (int i = 0; i < bodies.Count; i++)
            {
                if (!bodies[i].ActorId.Equals(m_Bindings[i].ActorId))
                    throw new ArgumentException("World body order does not match Deterministic KCC bindings.", nameof(bodies));
                RequireInsideWorld(bodies[i].Position);
            }
        }

        void RequireKccRoster(IReadOnlyList<DeterministicKccBodyState> states)
        {
            if (states.Count != m_Bindings.Length)
                throw new InvalidOperationException("Deterministic KCC state roster count is stale.");
            for (int i = 0; i < states.Count; i++)
            {
                if (!states[i].ActorId.Equals(m_Bindings[i].ActorId))
                    throw new InvalidOperationException("Deterministic KCC state roster order is stale.");
            }
        }

        void RequireInsideWorld(FixedVector3 position)
        {
            DeterministicCollisionBounds bounds = m_CollisionWorld.Bounds;
            if (position.X < bounds.Minimum.X || position.X > bounds.Maximum.X ||
                position.Y < bounds.Minimum.Y || position.Y + m_Configuration.Height > bounds.Maximum.Y ||
                position.Z < bounds.Minimum.Z || position.Z > bounds.Maximum.Z)
            {
                throw new InvalidOperationException("Deterministic KCC body left the collision world bounds.");
            }
        }

    }
}
