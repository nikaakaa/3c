using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonGameplay.Networking;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid
{
    [DisallowMultipleComponent]
    public sealed class CharacterServerAuthoritativeBinding : MonoBehaviour, IGameplayTickHook
    {
        [SerializeField] GameplayNetworkSessionHost m_SessionHost;
        [SerializeField] CharacterPipelineHost m_CharacterHost;
        [SerializeField] ServerAuthoritativeCharacterSyncProfile m_SyncProfile;

        CharacterServerAuthoritativeAdapter m_Adapter;
        ServerAuthoritativeHybridSession m_Session;
        bool m_BindingRegistered;
        bool m_TickHookRegistered;

        public CharacterPipeline Pipeline => m_CharacterHost ? m_CharacterHost.Pipeline : null;
        public IGameplayTickTarget Target => Pipeline;
        public string SubjectActorId => m_CharacterHost ? m_CharacterHost.ActorId : string.Empty;
        public ServerAuthoritativeHybridSession Session => m_Session;
        public ServerAuthoritativeDebug Debug => m_Session?.Debug;

        void Reset()
        {
            m_CharacterHost = GetComponent<CharacterPipelineHost>();
        }

        void OnEnable()
        {
            InitializeBinding();
        }

        void OnDisable()
        {
            ShutdownBinding();
        }

        void OnDestroy()
        {
            ShutdownBinding();
        }

        public void BeforeLogicTick(GameplayLogicTickContext context)
        {
            RequireActiveBinding();
            m_Session.Pump(context.LocalLogicTick);
            m_Adapter.DrainIncoming(m_Session, SubjectActorId, Pipeline.NetworkReceiveStage);
        }

        public void AfterLogicTick(GameplayLogicTickContext context)
        {
            RequireActiveBinding();
            m_Adapter.CollectOutgoing(
                SubjectActorId,
                context.LocalLogicTick,
                Pipeline.NetworkSendStage,
                Pipeline.ActionRuntime,
                m_Session);
            Pipeline.NetworkSendStage.Clear();
            m_Session.FlushOutgoing();
        }

        void InitializeBinding()
        {
            if (!m_SessionHost)
                throw new InvalidOperationException("CharacterServerAuthoritativeBinding requires a GameplayNetworkSessionHost.");
            if (!m_CharacterHost || !m_CharacterHost.EnsurePipeline())
                throw new InvalidOperationException("CharacterServerAuthoritativeBinding requires an initialized CharacterPipelineHost.");
            if (string.IsNullOrWhiteSpace(SubjectActorId))
                throw new InvalidOperationException("CharacterServerAuthoritativeBinding requires SubjectActorId.");
            if (!m_SyncProfile)
                throw new InvalidOperationException("CharacterServerAuthoritativeBinding requires a ServerAuthoritative Character Sync Profile.");
            if (m_SyncProfile.CharacterDefinition != m_CharacterHost.Definition)
            {
                throw new InvalidOperationException(
                    "CharacterServerAuthoritativeBinding Sync Profile targets a different CharacterPipelineDefinition.");
            }

            var errors = new List<string>();
            if (!m_SyncProfile.CollectConfigurationErrors(errors))
                throw new InvalidOperationException(string.Join("\n", errors));

            m_Session = m_SessionHost.RequireSession<ServerAuthoritativeHybridSession>();
            m_Session.RegisterBinding(SubjectActorId);
            m_BindingRegistered = true;
            m_Adapter = new CharacterServerAuthoritativeAdapter(m_SyncProfile);
            m_TickHookRegistered = GameplayTickSystem.RegisterTickHook(this);
            if (!m_TickHookRegistered)
            {
                ShutdownBinding();
                throw new InvalidOperationException("CharacterServerAuthoritativeBinding could not register its gameplay tick hook.");
            }
        }

        void ShutdownBinding()
        {
            if (m_TickHookRegistered)
                GameplayTickSystem.UnregisterTickHook(this);
            m_TickHookRegistered = false;

            if (m_BindingRegistered)
                m_Session?.UnregisterBinding(SubjectActorId);
            m_BindingRegistered = false;

            Pipeline?.NetworkReceiveStage.Clear();
            Pipeline?.NetworkSendStage.Clear();
            m_Adapter = null;
            m_Session = null;
        }

        void RequireActiveBinding()
        {
            if (!m_BindingRegistered || !m_TickHookRegistered || m_Session == null || m_Adapter == null || Pipeline == null)
                throw new InvalidOperationException("CharacterServerAuthoritativeBinding is not initialized.");
        }
    }
}
