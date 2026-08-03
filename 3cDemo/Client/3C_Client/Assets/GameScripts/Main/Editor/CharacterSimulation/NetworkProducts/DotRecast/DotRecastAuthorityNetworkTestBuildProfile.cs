using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Networking.ServerAuthoritative;
using ThirdPersonSimulation;
using ThirdPersonSimulation.DotRecast;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [CreateAssetMenu(fileName = "DotRecastAuthorityNetworkTestBuildProfile", menuName = "3C/Network Tests/DotRecast Authority Build Profile")]
    public sealed class DotRecastAuthorityNetworkTestBuildProfile : ScriptableObject
    {
        [Serializable]
        public sealed class ActorAuthoring
        {
            [SerializeField] string m_PlayerId = string.Empty;
            [SerializeField] string m_ActorId = string.Empty;
            [SerializeField] ServerAuthoritativeProcessRole m_ClientRole;
            [SerializeField] string m_WorldBodyBindingId = string.Empty;
            [SerializeField] Vector3 m_InitialPosition;
            [SerializeField] float m_InitialYawDegrees;
            [SerializeField] bool m_InitialGrounded = true;
            [SerializeField] float m_ContactRadius;
            [SerializeField] float m_ContactHeight;
            [SerializeField] float m_ContactSkinWidth;

            public DotRecastAuthorityActorExportBinding Build(CharacterSimulationProgram program)
            {
                var actorId = new ActorId(Require(m_ActorId, nameof(m_ActorId)));
                var roster = new ServerAuthoritativeRosterEntry(
                    new ServerAuthoritativePlayerId(Require(m_PlayerId, nameof(m_PlayerId))),
                    actorId,
                    m_ClientRole);
                string bindingId = Require(m_WorldBodyBindingId, nameof(m_WorldBodyBindingId));
                var initialBody = new WorldBodyState(
                    actorId,
                    new Float32Vector3(
                        Float32ScalarBoundary.ConvertExternal(m_InitialPosition.x, $"{bindingId}/initial-position-x"),
                        Float32ScalarBoundary.ConvertExternal(m_InitialPosition.y, $"{bindingId}/initial-position-y"),
                        Float32ScalarBoundary.ConvertExternal(m_InitialPosition.z, $"{bindingId}/initial-position-z")),
                    new Float32Yaw(Float32ScalarBoundary.ConvertExternal(
                        Mathf.DeltaAngle(0f, m_InitialYawDegrees),
                        $"{bindingId}/initial-yaw")),
                    Float32Vector3.Zero,
                    Float32Scalar.Zero,
                    m_InitialGrounded,
                    WorldCollisionSummary.None);
                var outputRoute = new SimulationOutputRouteDescriptor(
                    $"server-authoritative-authority-output/{actorId.Value}",
                    "server-authoritative-authority-output",
                    1,
                    actorId,
                    StableHash.Compute(
                        "server-authoritative-authority-output/1",
                        actorId.Value,
                        program.ProgramHash.ToString(),
                        bindingId));
                var contactShape = new ActorContactShape(
                    Float32ScalarBoundary.ConvertExternal(m_ContactRadius, $"{bindingId}/contact-radius"),
                    Float32ScalarBoundary.ConvertExternal(m_ContactHeight, $"{bindingId}/contact-height"),
                    Float32ScalarBoundary.ConvertExternal(m_ContactSkinWidth, $"{bindingId}/contact-skin-width"));
                return new DotRecastAuthorityActorExportBinding(roster, bindingId, initialBody, contactShape, outputRoute);
            }
        }

        [SerializeField] CharacterPipelineDefinition m_CharacterDefinition;
        [SerializeField] ServerAuthoritativeAuthoritySessionSourceDefinition m_AuthoritySource;
        [SerializeField] SimulationExecutionBackendDefinition m_ExecutionBackend;
        [SerializeField] DotRecastWorldSolverDefinition m_WorldSolver;
        [SerializeField] ServerAuthoritativeFantasyEndpointDefinition m_Endpoint;
        [SerializeField] string m_HostId = string.Empty;
        [SerializeField] int m_FantasyProcessConfigId;
        [SerializeField] int m_AuthoritySceneConfigId;
        [SerializeField] string m_AuthoritySceneType = string.Empty;
        [SerializeField] string m_RoomId = string.Empty;
        [SerializeField] string m_DataHost = string.Empty;
        [SerializeField] int m_DataPort;
        [SerializeField] string m_SessionId = string.Empty;
        [SerializeField] string m_WorldId = string.Empty;
        [SerializeField] string m_SourceClockId = string.Empty;
        [SerializeField] List<ActorAuthoring> m_Actors = new List<ActorAuthoring>();

        public ServerAuthoritativeFantasyEndpointDefinition Endpoint => m_Endpoint
            ? m_Endpoint
            : throw new InvalidOperationException("DotRecast Authority build profile requires an Endpoint.");
        public int DataPort => m_DataPort is > 0 and <= 65535
            ? m_DataPort
            : throw new InvalidOperationException("DotRecast Authority build profile requires a valid data port.");

        public DotRecastAuthoritySceneManifestExportRequest BuildExportRequest(string serverPublishDirectory)
        {
            if (!m_CharacterDefinition || !m_AuthoritySource || !m_ExecutionBackend || !m_WorldSolver || !m_Endpoint)
                throw new InvalidOperationException("DotRecast Authority build profile requires all formal asset references.");
            CharacterSimulationProgram program = m_CharacterDefinition.SimulationProgram.Load();
            if (m_Actors == null || m_Actors.Count != 2)
                throw new InvalidOperationException("DotRecast Authority build profile requires exactly two Actor rows.");
            var actors = new DotRecastAuthorityActorExportBinding[m_Actors.Count];
            for (int i = 0; i < actors.Length; i++)
                actors[i] = m_Actors[i]?.Build(program) ?? throw new InvalidOperationException("DotRecast Authority build profile contains an empty Actor row.");
            return new DotRecastAuthoritySceneManifestExportRequest(
                serverPublishDirectory,
                m_CharacterDefinition,
                m_AuthoritySource,
                m_ExecutionBackend,
                m_WorldSolver,
                Require(m_HostId, nameof(m_HostId)),
                m_FantasyProcessConfigId,
                m_AuthoritySceneConfigId,
                Require(m_AuthoritySceneType, nameof(m_AuthoritySceneType)),
                Require(m_RoomId, nameof(m_RoomId)),
                Require(m_DataHost, nameof(m_DataHost)),
                DataPort,
                Require(m_SessionId, nameof(m_SessionId)),
                Require(m_WorldId, nameof(m_WorldId)),
                Require(m_SourceClockId, nameof(m_SourceClockId)),
                actors);
        }

        static string Require(string value, string parameter) => string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"DotRecast Authority build profile requires '{parameter}'.")
            : value.Trim();
    }
}
