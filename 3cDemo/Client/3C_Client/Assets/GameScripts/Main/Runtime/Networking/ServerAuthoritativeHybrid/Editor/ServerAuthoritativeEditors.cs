using System.Collections.Generic;
using ThirdPersonGameplay.Networking;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid.Editor
{
    [CustomEditor(typeof(ServerAuthoritativeCharacterSyncProfile))]
    public sealed class ServerAuthoritativeCharacterSyncProfileEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();

        SerializedProperty m_CharacterDefinition;
        SerializedProperty m_BehaviorPolicies;
        SerializedProperty m_ActionPolicies;
        SerializedProperty m_FactBindings;

        void OnEnable()
        {
            m_CharacterDefinition = serializedObject.FindProperty("m_CharacterDefinition");
            m_BehaviorPolicies = serializedObject.FindProperty("m_BehaviorPolicies");
            m_ActionPolicies = serializedObject.FindProperty("m_ActionPolicies");
            m_FactBindings = serializedObject.FindProperty("m_FactBindings");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHeader("Character Contract");
            EditorGUILayout.PropertyField(m_CharacterDefinition);
            DrawHeader("Behavior Policies");
            EditorGUILayout.PropertyField(m_BehaviorPolicies, true);
            DrawHeader("Action Policies");
            EditorGUILayout.PropertyField(m_ActionPolicies, true);
            DrawHeader("Fact Bindings");
            EditorGUILayout.PropertyField(m_FactBindings, true);
            serializedObject.ApplyModifiedProperties();

            ServerAuthoritativeCharacterSyncProfile profile = target as ServerAuthoritativeCharacterSyncProfile;
            if (!profile)
                return;

            DrawEffectivePolicies(profile);
            DrawConfiguration(profile);
        }

        static void DrawEffectivePolicies(ServerAuthoritativeCharacterSyncProfile profile)
        {
            DrawHeader("Effective Packet Mapping");
            var behaviorResolver = new ServerAuthoritativeBehaviorPolicyResolver(profile);
            for (int i = 0; i < profile.FactBindings.Count; i++)
            {
                ServerAuthoritativeFactBinding binding = profile.FactBindings[i];
                if (binding != null)
                    DrawResolution(binding.FactKind.ToString(), behaviorResolver.ResolveFact(binding.FactKind));
            }

            for (int i = 0; i < profile.BehaviorPolicies.Count; i++)
            {
                ServerAuthoritativeBehaviorPolicy policy = profile.BehaviorPolicies[i];
                if (policy == null)
                    continue;
                if (policy.TargetDomain == ServerAuthoritativeDomain.GameplayResult)
                {
                    DrawResolution(
                        $"{policy.BehaviorId} Event",
                        behaviorResolver.ResolveEvent(policy.BehaviorId, ServerAuthoritativePacketKind.GameplayResult));
                }
                else if (policy.TargetDomain == ServerAuthoritativeDomain.Presentation)
                {
                    DrawResolution(
                        $"{policy.BehaviorId} Event",
                        behaviorResolver.ResolveEvent(policy.BehaviorId, ServerAuthoritativePacketKind.GameplayCue));
                }
                else if (policy.TargetDomain == ServerAuthoritativeDomain.GameplayEffect)
                {
                    DrawResolution(
                        $"{policy.BehaviorId} Lifecycle",
                        behaviorResolver.ResolveGameplayEffect(policy.BehaviorId, ServerAuthoritativePacketKind.GameplayEffectLifecycle));
                    DrawResolution(
                        $"{policy.BehaviorId} Attribute",
                        behaviorResolver.ResolveGameplayEffect(policy.BehaviorId, ServerAuthoritativePacketKind.GameplayAttributeValue));
                }
            }

            var transactionResolver = new ServerAuthoritativeTransactionPolicyResolver(profile);
            for (int i = 0; i < profile.ActionPolicies.Count; i++)
            {
                ServerAuthoritativeActionPolicy action = profile.ActionPolicies[i];
                if (action == null)
                    continue;

                DrawResolution($"{action.ActionId} Activation", transactionResolver.ResolveActivation(action.ActionId));
                DrawResolution(
                    $"{action.ActionId} Lifecycle",
                    transactionResolver.ResolveLifecycle(action.ActionId, ThirdPersonCharacter.ActionSystem.ActionLifecycleTransitionType.Complete));
                for (int j = 0; j < action.WindowPolicies.Count; j++)
                {
                    ServerAuthoritativeWindowPolicy window = action.WindowPolicies[j];
                    if (window != null)
                        DrawResolution($"{action.ActionId} Window {window.WindowType}", transactionResolver.ResolveWindow(action.ActionId, window.WindowType));
                }
                for (int j = 0; j < action.MotionPolicies.Count; j++)
                {
                    ServerAuthoritativeMotionPolicy motion = action.MotionPolicies[j];
                    if (motion != null)
                        DrawResolution($"{action.ActionId} Motion {motion.SourceType}", transactionResolver.ResolveMotion(action.ActionId, motion.SourceType));
                }
                for (int j = 0; j < action.CuePolicies.Count; j++)
                {
                    ServerAuthoritativeCuePolicy cue = action.CuePolicies[j];
                    if (cue != null)
                        DrawResolution($"{action.ActionId} Cue {cue.CueType}", transactionResolver.ResolveCue(action.ActionId, cue.CueType));
                }
                DrawResolution($"{action.ActionId} Result", transactionResolver.ResolveGameplayResult(action.ActionId));
            }
        }

        void DrawConfiguration(ServerAuthoritativeCharacterSyncProfile profile)
        {
            DrawHeader("Configuration");
            m_Errors.Clear();
            if (profile.CollectConfigurationErrors(m_Errors))
            {
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
                return;
            }

            for (int i = 0; i < m_Errors.Count; i++)
                EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
        }

        static void DrawResolution(string label, ServerAuthoritativePolicyResolution resolution)
        {
            MessageType messageType = !resolution.IsConfigured
                ? MessageType.Error
                : resolution.ShouldSend ? MessageType.Info : MessageType.Warning;
            string state = !resolution.IsConfigured ? "Missing" : resolution.ShouldSend ? "Send" : "Filtered";
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, state, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Domain", resolution.Domain.ToString());
            EditorGUILayout.LabelField("Packet", resolution.PacketKind.ToString());
            EditorGUILayout.LabelField("Policy Id", string.IsNullOrEmpty(resolution.PolicyId) ? "-" : resolution.PolicyId);
            if (!string.IsNullOrEmpty(resolution.Summary))
                EditorGUILayout.HelpBox(resolution.Summary, messageType);
            if (!string.IsNullOrEmpty(resolution.Reason))
                EditorGUILayout.HelpBox(resolution.Reason, messageType);
            EditorGUILayout.EndVertical();
        }

        static void DrawHeader(string label)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }
    }

    [CustomEditor(typeof(ServerAuthoritativeHybridModelDefinition))]
    public sealed class ServerAuthoritativeHybridModelDefinitionEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ServerAuthoritativeHybridModelDefinition definition = target as ServerAuthoritativeHybridModelDefinition;
            if (!definition)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Resolved Model", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Model Id", definition.ModelId);
            EditorGUILayout.LabelField(
                "Endpoint",
                definition.EndpointDefinition ? definition.EndpointDefinition.EndpointId : "Disconnected");
            m_Errors.Clear();
            if (definition.CollectConfigurationErrors(m_Errors))
            {
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
                return;
            }

            for (int i = 0; i < m_Errors.Count; i++)
                EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
        }
    }

    [CustomEditor(typeof(LocalServerAuthoritativeEndpointDefinition))]
    public sealed class LocalServerAuthoritativeEndpointDefinitionEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            LocalServerAuthoritativeEndpointDefinition definition = target as LocalServerAuthoritativeEndpointDefinition;
            if (!definition)
                return;

            m_Errors.Clear();
            if (definition.CollectConfigurationErrors(m_Errors))
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
            else
            {
                for (int i = 0; i < m_Errors.Count; i++)
                    EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
            }
        }
    }

    [CustomEditor(typeof(GameplayNetworkSessionHost))]
    public sealed class GameplayNetworkSessionHostEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GameplayNetworkSessionHost host = target as GameplayNetworkSessionHost;
            if (!host)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Session Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Model Id", string.IsNullOrEmpty(host.ModelId) ? "Inactive" : host.ModelId);
            EditorGUILayout.LabelField("Bindings", host.BindingCount.ToString());
            for (int i = 0; i < host.ConfigurationErrors.Count; i++)
                EditorGUILayout.HelpBox(host.ConfigurationErrors[i], MessageType.Error);

            if (host.Session is not ServerAuthoritativeHybridSession session)
                return;

            EditorGUILayout.LabelField("Endpoint", string.IsNullOrEmpty(session.EndpointId) ? "Disconnected" : session.EndpointId);
            EditorGUILayout.LabelField("Outgoing Queue", session.PendingOutgoingCount.ToString());
            EditorGUILayout.LabelField("Incoming Queue", session.PendingIncomingCount.ToString());
            EditorGUILayout.LabelField("History", session.History.Records.Count.ToString());
            foreach (string subjectActorId in session.BindingSubjectActorIds)
                EditorGUILayout.LabelField("Subject Actor", subjectActorId);

            IReadOnlyList<ServerAuthoritativePolicyDecisionDebugRecord> decisions = session.Debug.PolicyDecisions;
            int first = Mathf.Max(0, decisions.Count - 8);
            for (int i = first; i < decisions.Count; i++)
            {
                ServerAuthoritativePolicyDecisionDebugRecord decision = decisions[i];
                EditorGUILayout.LabelField(
                    decision.FactKind,
                    $"{decision.PacketKind} {(decision.ShouldSend ? "Send" : decision.Reason)}");
            }
        }
    }
}
