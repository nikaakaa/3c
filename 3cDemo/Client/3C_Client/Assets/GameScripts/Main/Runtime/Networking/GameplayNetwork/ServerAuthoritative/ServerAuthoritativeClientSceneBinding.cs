using System;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class ServerAuthoritativeClientSceneBinding : MonoBehaviour
    {
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] CharacterPipelineHost m_CharacterHost;
        [SerializeField] SimulationSessionCompositionDefinition m_ClientAComposition;
        [SerializeField] SimulationSessionCompositionDefinition m_ClientBComposition;

        void Awake()
        {
            if (!m_SessionHost || !m_CharacterHost)
                throw new InvalidOperationException("Client Scene Binding requires explicit Session and Character Hosts.");
            try
            {
                ServerAuthoritativeProcessRole role = ServerAuthoritativeSceneLaunchSelection.TakeClientRole(
                    out string expectedPlayerId,
                    out string expectedActorId);
                SimulationSessionCompositionDefinition composition = role == ServerAuthoritativeProcessRole.ClientA
                    ? m_ClientAComposition
                    : m_ClientBComposition;
                if (!composition)
                    throw new InvalidOperationException($"Client Scene Binding requires an explicit {role} Composition.");
                if (composition.SessionSource is not ServerAuthoritativePredictionSessionSourceDefinition source)
                    throw new InvalidOperationException("Selected Client Composition does not use the Prediction Session Source.");
                ServerAuthoritativeProcessIdentity process = source.Launch.BuildProcessIdentity();
                if (process.Role != role)
                    throw new InvalidOperationException("Selected Client Composition launch role does not match Bootstrap role.");
                if (!string.IsNullOrEmpty(expectedPlayerId) &&
                    !string.Equals(process.PlayerId.Value, expectedPlayerId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Selected Client Composition PlayerId does not match the explicit launch identity.");
                }
                if (!string.IsNullOrEmpty(expectedActorId) &&
                    !string.Equals(process.ActorId.Value, expectedActorId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Selected Client Composition ActorId does not match the explicit launch identity.");
                }
                m_SessionHost.BindComposition(composition);
                m_CharacterHost.BindSessionActor(m_SessionHost, process.ActorId);
            }
            catch
            {
                m_SessionHost.enabled = false;
                m_CharacterHost.enabled = false;
                throw;
            }
        }
    }
}
