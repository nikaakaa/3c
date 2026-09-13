using System;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [DisallowMultipleComponent]
    public sealed class ServerAuthoritativeAuthorityActorHost : MonoBehaviour
    {
        [SerializeField] CharacterPipelineDefinition m_CharacterDefinition;
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] string m_ActorId = string.Empty;
        [SerializeField] Float32WorldBodyBinding m_WorldBodyBinding;

        ServerAuthoritativeAuthorityActorRegistration m_Registration;

        public CharacterPipelineDefinition CharacterDefinition => m_CharacterDefinition;
        public SimulationSessionHost SessionHost => m_SessionHost;
        public string ActorId => string.IsNullOrWhiteSpace(m_ActorId) ? string.Empty : m_ActorId.Trim();
        public Float32WorldBodyBinding WorldBodyBinding => m_WorldBodyBinding;

        public bool EnsureRegistration()
        {
            if (m_Registration != null)
                return true;
            if (!m_CharacterDefinition || !m_SessionHost || string.IsNullOrEmpty(ActorId) || !m_WorldBodyBinding)
            {
                Debug.LogError("Authority Actor Host requires Character Definition, Session Host, ActorId, and World Body Binding.", this);
                return false;
            }
            RuntimeDiagnosticsTarget diagnosticsTarget = null;
            ServerAuthoritativeAuthorityActorRegistration registration = null;
            try
            {
                var actorId = new ActorId(ActorId);
                m_WorldBodyBinding.RequireValid();
                if (m_WorldBodyBinding.ActorId != actorId)
                    throw new InvalidOperationException("Authority ActorId does not match its World Body Binding.");
                CharacterSimulationProgramAsset programAsset = m_CharacterDefinition.SimulationProgram;
                CharacterSimulationProgram program = programAsset.Load();
                if (!m_SessionHost.Composition ||
                    program.Manifest.TickRate != m_SessionHost.Composition.TickRate ||
                    m_CharacterDefinition.SimulationTickRate != m_SessionHost.Composition.TickRate)
                {
                    throw new InvalidOperationException("Authority Program, Character Definition, and Composition TickRate must match.");
                }
                CharacterRuntimeDebugProgram debugProgram = CharacterRuntimeDebugProgramBuilder.Build(program);
                var diagnosticsContext = new RuntimeDiagnosticsContext(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    debugProgram.Revision,
                    debugProgram.SourceMap,
                    new RuntimeDiagnosticsStore());
                var diagnostics = new CharacterSimulationDiagnosticsAdapter(diagnosticsContext, program);
                diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
                WorldBodyState initialBody = m_WorldBodyBinding.InitialBody;
                registration = new ServerAuthoritativeAuthorityActorRegistration(
                    GetInstanceID(),
                    name,
                    actorId,
                    programAsset,
                    program,
                    m_WorldBodyBinding,
                    initialBody,
                    diagnostics,
                    diagnosticsTarget);
                diagnosticsTarget = null;
                m_SessionHost.RegisterActor(registration);
                m_Registration = registration;
                registration = null;
                return true;
            }
            catch (Exception exception)
            {
                registration?.Dispose();
                diagnosticsTarget?.Terminate();
                diagnosticsTarget?.Dispose();
                Debug.LogException(exception, this);
                return false;
            }
        }

        void Awake()
        {
            EnsureRegistration();
        }

        void OnEnable()
        {
            EnsureRegistration();
        }

        void OnDisable()
        {
            DisposeRegistration();
        }

        void OnDestroy()
        {
            DisposeRegistration();
        }

        void DisposeRegistration()
        {
            if (m_Registration == null)
                return;
            ServerAuthoritativeAuthorityActorRegistration registration = m_Registration;
            m_Registration = null;
            if (m_SessionHost)
            {
                m_SessionHost.Stop();
                m_SessionHost.ReleaseActor(registration);
            }
            else
            {
                registration.Dispose();
            }
        }
    }
}
