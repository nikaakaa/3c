using System;
using ThirdPersonGameplay.Networking.ServerAuthoritative;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    [CustomEditor(typeof(ServerAuthoritativeHybridModelDefinition))]
    public sealed class ServerAuthoritativeHybridModelDefinitionEditor : UnityEditor.Editor
    {
        SerializedProperty m_Endpoint;
        SerializedProperty m_PredictionPipeline;
        SerializedProperty m_AuthorityPipeline;
        SerializedProperty m_SimulationTickRate;
        SerializedProperty m_CommandPacketRate;
        SerializedProperty m_SnapshotPacketRate;
        SerializedProperty m_CommandSlackTicks;
        SerializedProperty m_MaximumRemoteBodyExtrapolationTicks;
        SerializedProperty m_MaxGameplayDatagramBytes;
        SerializedProperty m_HistoryCapacity;
        SerializedProperty m_MaximumInputLeadTicks;
        SerializedProperty m_MaximumInputLagTicks;
        SerializedProperty m_MaximumReplayTicksPerOuterTick;
        SerializedProperty m_BodyPositionTolerance;
        SerializedProperty m_BodyYawToleranceDegrees;
        SerializedProperty m_HardRecoveryPolicy;
        SerializedProperty m_MissingInputPolicy;
        SerializedProperty m_ReliableGameplayFactKinds;
        SerializedProperty m_ReliableProducerIds;

        void OnEnable()
        {
            m_Endpoint = Find("m_Endpoint");
            m_PredictionPipeline = Find("m_PredictionPipeline");
            m_AuthorityPipeline = Find("m_AuthorityPipeline");
            m_SimulationTickRate = Find("m_SimulationTickRate");
            m_CommandPacketRate = Find("m_CommandPacketRate");
            m_SnapshotPacketRate = Find("m_SnapshotPacketRate");
            m_CommandSlackTicks = Find("m_CommandSlackTicks");
            m_MaximumRemoteBodyExtrapolationTicks = Find("m_MaximumRemoteBodyExtrapolationTicks");
            m_MaxGameplayDatagramBytes = Find("m_MaxGameplayDatagramBytes");
            m_HistoryCapacity = Find("m_HistoryCapacity");
            m_MaximumInputLeadTicks = Find("m_MaximumInputLeadTicks");
            m_MaximumInputLagTicks = Find("m_MaximumInputLagTicks");
            m_MaximumReplayTicksPerOuterTick = Find("m_MaximumReplayTicksPerOuterTick");
            m_BodyPositionTolerance = Find("m_BodyPositionTolerance");
            m_BodyYawToleranceDegrees = Find("m_BodyYawToleranceDegrees");
            m_HardRecoveryPolicy = Find("m_HardRecoveryPolicy");
            m_MissingInputPolicy = Find("m_MissingInputPolicy");
            m_ReliableGameplayFactKinds = Find("m_ReliableGameplayFactKinds");
            m_ReliableProducerIds = Find("m_ReliableProducerIds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawReferences();
            DrawTimingAndHistory();
            DrawCorrection();
            DrawReplicationCoverage();
            serializedObject.ApplyModifiedProperties();
            DrawIdentity(target as ServerAuthoritativeHybridModelDefinition);
        }

        void DrawReferences()
        {
            EditorGUILayout.LabelField("Source And Endpoint", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Endpoint, new GUIContent("Fantasy Endpoint"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Pipelines", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_PredictionPipeline, new GUIContent("Prediction Pipeline"));
            EditorGUILayout.PropertyField(m_AuthorityPipeline, new GUIContent("Authority Pipeline"));
        }

        void DrawTimingAndHistory()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Simulation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SimulationTickRate, new GUIContent("Tick Rate"));
            EditorGUILayout.PropertyField(m_HistoryCapacity, new GUIContent("History Capacity"));
            EditorGUILayout.PropertyField(m_MaximumInputLeadTicks, new GUIContent("Maximum Input Lead"));
            EditorGUILayout.PropertyField(m_MaximumInputLagTicks, new GUIContent("Maximum Input Lag"));
            EditorGUILayout.PropertyField(m_MaximumReplayTicksPerOuterTick, new GUIContent("Maximum Replay Per Tick"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Command", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_CommandPacketRate, new GUIContent("Packet Rate"));
            EditorGUILayout.PropertyField(m_CommandSlackTicks, new GUIContent("Target Slack Ticks"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Snapshot", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SnapshotPacketRate, new GUIContent("Packet Rate"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Interpolation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_MaximumRemoteBodyExtrapolationTicks, new GUIContent("Maximum Remote Body Extrapolation Ticks"));
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Budget", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_MaxGameplayDatagramBytes, new GUIContent("Max Datagram Bytes"));
        }

        void DrawCorrection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Correction And Missing Input", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_BodyPositionTolerance, new GUIContent("Body Position Tolerance"));
            EditorGUILayout.PropertyField(m_BodyYawToleranceDegrees, new GUIContent("Body Yaw Tolerance"));
            EditorGUILayout.PropertyField(m_HardRecoveryPolicy, new GUIContent("Hard Recovery"));
            EditorGUILayout.PropertyField(m_MissingInputPolicy, new GUIContent("Missing Input"));
        }

        void DrawReplicationCoverage()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Replication Coverage", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ReliableGameplayFactKinds, new GUIContent("Reliable Fact Kinds"));
            EditorGUILayout.PropertyField(m_ReliableProducerIds, new GUIContent("Program Producers"), true);
        }

        static void DrawIdentity(ServerAuthoritativeHybridModelDefinition definition)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Resolved Identity", EditorStyles.boldLabel);
            if (!definition)
                return;
            try
            {
                definition.RequireComplete();
                SimulationComponentIdentity model = definition.BuildModelIdentity();
                SimulationComponentIdentity endpoint = definition.Endpoint.BuildIdentity();
                SimulationPipelineDescriptor prediction = definition.PredictionPipeline.BuildPortableDescriptor();
                SimulationPipelineDescriptor authority = definition.AuthorityPipeline.BuildPortableDescriptor();
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Model", model.ToString());
                    EditorGUILayout.TextField("Protocol", $"{ServerAuthoritativeModelIdentity.ProtocolId}@{ServerAuthoritativeModelIdentity.ProtocolVersion}");
                    EditorGUILayout.TextField("Endpoint", endpoint.ToString());
                    EditorGUILayout.TextField("Prediction Pipeline", $"{prediction.PipelineId}@{prediction.Revision} | {prediction.DescriptorHash}");
                    EditorGUILayout.TextField("Authority Pipeline", $"{authority.PipelineId}@{authority.Revision} | {authority.DescriptorHash}");
                    EditorGUILayout.TextField("Policy Hash", definition.Policy.ConfigurationHash.ToString());
                    EditorGUILayout.TextField("Replication Policy", definition.ReplicationPolicy.ConfigurationHash.ToString());
                    EditorGUILayout.IntField("Covered Producers", definition.ReplicationPolicy.ReliableProducerIds.Count);
                }
                EditorGUILayout.HelpBox("ProgramHash, Solver identity and final PipelineHash values are resolved from the selected Session Composition during preparation.", MessageType.Info);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }
        }

        SerializedProperty Find(string name) => serializedObject.FindProperty(name) ??
            throw new InvalidOperationException($"ServerAuthoritative Model Inspector cannot find serialized field '{name}'.");
    }
}
