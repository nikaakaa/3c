using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterSimulationProgramAsset))]
    public sealed class CharacterSimulationProgramAssetEditor : UnityEditor.Editor
    {
        string m_LoadedProgramHash;
        CharacterSimulationProgram m_Program;
        string m_LoadError;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty artifact = serializedObject.FindProperty("m_CanonicalArtifact");
            EditorGUILayout.LabelField("Compiled Simulation Program", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Numeric Target", serializedObject.FindProperty("m_NumericProfileId").stringValue);
            EditorGUILayout.LabelField("Target ABI", serializedObject.FindProperty("m_TargetAbiVersion").intValue.ToString());
            EditorGUILayout.LabelField("Compiler", serializedObject.FindProperty("m_CompilerVersion").stringValue);
            EditorGUILayout.LabelField("Operation Set", serializedObject.FindProperty("m_OperationSetVersion").stringValue);
            EditorGUILayout.LabelField("Artifact Size", EditorUtility.FormatBytes(artifact?.arraySize ?? 0));
            LoadProgram();
            if (!string.IsNullOrEmpty(m_LoadError))
            {
                EditorGUILayout.HelpBox(m_LoadError, MessageType.Error);
                return;
            }
            ProgramBodyMotionDescriptor bodyMotion = m_Program.BodyMotion;
            EditorGUILayout.LabelField("Body Motion", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Source", bodyMotion.SourceIdentity);
            EditorGUILayout.LabelField("Revision", bodyMotion.ContentRevision.ToString());
            EditorGUILayout.LabelField("Semantic Version", bodyMotion.SemanticVersion.ToString());
            EditorGUILayout.LabelField("Gravity Acceleration", bodyMotion.GravityAcceleration.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Maximum Fall Speed", bodyMotion.MaximumFallSpeed.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            EditorGUILayout.LabelField("Required World Capabilities", m_Program.Manifest.Capabilities.RequiredWorldCapabilities.ToString());
            EditorGUILayout.LabelField("Motion Modifiers", m_Program.MotionModifiers.Count.ToString());
            for (int i = 0; i < m_Program.MotionModifiers.Count; i++)
            {
                ProgramMotionModifierDescriptor descriptor = m_Program.MotionModifiers[i];
                EditorGUILayout.LabelField($"Modifier {descriptor.Index}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Kind / Channel", $"{descriptor.Kind} / {descriptor.Channel}");
                EditorGUILayout.LabelField("Operation / Source", $"{descriptor.Operation.Value} / {descriptor.SourceMotionOperation.Value}");
                EditorGUILayout.LabelField("Timeline Owner", descriptor.TimelineOwnerOperation.Value.ToString());
                EditorGUILayout.LabelField("Action Context", descriptor.ActionContextIdentity);
                EditorGUILayout.LabelField("Modes", $"{descriptor.TranslationMode} / {descriptor.TargetOffsetSpace} / {descriptor.RotationMode} / {descriptor.RotationMethod} / {descriptor.LimitPolicy}");
                EditorGUILayout.LabelField("State Range", $"{descriptor.StateSlotStart}..{descriptor.StateSlotStart + descriptor.StateSlotCount - 1} ({descriptor.StateSlotCount})");
            }
        }

        void LoadProgram()
        {
            CharacterSimulationProgramAsset asset = (CharacterSimulationProgramAsset)target;
            if (m_Program != null && string.Equals(m_LoadedProgramHash, asset.ProgramHash, System.StringComparison.Ordinal))
                return;
            m_LoadedProgramHash = asset.ProgramHash;
            m_Program = null;
            m_LoadError = string.Empty;
            try
            {
                m_Program = asset.Load();
            }
            catch (System.Exception exception)
            {
                m_LoadError = exception.Message;
            }
        }
    }

    [CustomEditor(typeof(CharacterPresentationProjectionAsset))]
    public sealed class CharacterPresentationProjectionAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty projection = serializedObject.FindProperty("m_Projection");
            EditorGUILayout.LabelField("Compiled Presentation Projection", EditorStyles.boldLabel);
            if (projection == null)
            {
                EditorGUILayout.HelpBox("Compiled projection is missing.", MessageType.Error);
                return;
            }

            SerializedProperty blendSlots = projection.FindPropertyRelative("m_BlendSlots");
            SerializedProperty curveCatalog = projection.FindPropertyRelative("m_BlendCurveCatalog")?.FindPropertyRelative("m_Entries");
            SerializedProperty profileCatalog = projection.FindPropertyRelative("m_BlendProfileCatalog")?.FindPropertyRelative("m_Entries");
            SerializedProperty producers = projection.FindPropertyRelative("m_Producers");
            SerializedProperty poseProgram = projection.FindPropertyRelative("m_PoseProgram");
            SerializedProperty rig = projection.FindPropertyRelative("m_Rig");
            int animationCount = 0;
            int cameraCount = 0;
            int cueCount = 0;
            for (int i = 0; i < producers.arraySize; i++)
            {
                int kind = producers.GetArrayElementAtIndex(i).FindPropertyRelative("m_Kind").enumValueIndex;
                if (kind == (int)CharacterPresentationProducerKind.Animation)
                    animationCount++;
                else if (kind == (int)CharacterPresentationProducerKind.Camera)
                    cameraCount++;
                else if (kind == (int)CharacterPresentationProducerKind.Cue)
                    cueCount++;
            }

            EditorGUILayout.LabelField("Semantic Contract", projection.FindPropertyRelative("m_ContractHash").stringValue);
            EditorGUILayout.LabelField("Projection Revision", projection.FindPropertyRelative("m_ProjectionRevision").stringValue);
            EditorGUILayout.LabelField("Pose Slots", (blendSlots?.arraySize ?? 0).ToString());
            EditorGUILayout.LabelField("Blend Curves", (curveCatalog?.arraySize ?? 0).ToString());
            EditorGUILayout.LabelField("Blend Profiles", (profileCatalog?.arraySize ?? 0).ToString());
            EditorGUILayout.LabelField("Animation Producers", animationCount.ToString());
            EditorGUILayout.LabelField("Camera Producers", cameraCount.ToString());
            EditorGUILayout.LabelField("Cue Producers", cueCount.ToString());

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pose Program", EditorStyles.boldLabel);
            if (poseProgram == null)
            {
                EditorGUILayout.HelpBox("Compiled Pose Program is missing.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("Schema / Runtime ABI", $"{poseProgram.FindPropertyRelative("m_SchemaVersion").stringValue} / {poseProgram.FindPropertyRelative("m_RuntimeAbi").stringValue}");
                EditorGUILayout.LabelField("Graph Identity", $"{poseProgram.FindPropertyRelative("m_PoseGraphId").stringValue} @ {poseProgram.FindPropertyRelative("m_ContentRevision").stringValue}");
                EditorGUILayout.LabelField("Program Hash", poseProgram.FindPropertyRelative("m_ProgramHash").stringValue);
                EditorGUILayout.LabelField("Operations / Output", $"{poseProgram.FindPropertyRelative("m_Operations").arraySize} / {poseProgram.FindPropertyRelative("m_OutputOperationIndex").intValue}");
                EditorGUILayout.LabelField("Parameters / Masks", $"{poseProgram.FindPropertyRelative("m_Parameters").arraySize} / {poseProgram.FindPropertyRelative("m_BoneMasks").arraySize}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rig", EditorStyles.boldLabel);
            if (rig == null)
            {
                EditorGUILayout.HelpBox("Compiled Rig payload is missing.", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("Identity", $"{rig.FindPropertyRelative("m_RigId").stringValue} @ {rig.FindPropertyRelative("m_RigRevision").stringValue}");
                EditorGUILayout.LabelField("Bones", rig.FindPropertyRelative("m_Bones").arraySize.ToString());
                EditorGUILayout.LabelField("Root / Left Foot / Right Foot", $"{rig.FindPropertyRelative("m_RootBoneIndex").intValue} / {rig.FindPropertyRelative("m_LeftFootBoneIndex").intValue} / {rig.FindPropertyRelative("m_RightFootBoneIndex").intValue}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Channel to Pose Slot", EditorStyles.boldLabel);
            for (int i = 0; i < (blendSlots?.arraySize ?? 0); i++)
            {
                SerializedProperty slot = blendSlots.GetArrayElementAtIndex(i);
                SerializedProperty policy = slot.FindPropertyRelative("m_OutputPolicy");
                EditorGUILayout.LabelField(
                    slot.FindPropertyRelative("m_AnimationChannelId").stringValue,
                    $"{slot.FindPropertyRelative("m_PoseSlotId").stringValue} / {policy.enumDisplayNames[policy.enumValueIndex]}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Marker Sync", EditorStyles.boldLabel);
            for (int i = 0; i < producers.arraySize; i++)
            {
                SerializedProperty producer = producers.GetArrayElementAtIndex(i);
                if (producer.FindPropertyRelative("m_Kind").enumValueIndex != (int)CharacterPresentationProducerKind.Animation)
                    continue;
                SerializedProperty markerSync = producer.FindPropertyRelative("m_Animation")?.FindPropertyRelative("m_MarkerSync");
                if (markerSync == null)
                    continue;
                string producerIdentity = producer.FindPropertyRelative("m_ProgramProducerIdentity").stringValue;
                string animationChannelId = producer.FindPropertyRelative("m_AnimationChannelId").stringValue;
                SerializedProperty mode = markerSync.FindPropertyRelative("m_Mode");
                SerializedProperty topology = markerSync.FindPropertyRelative("m_SequenceTopology");
                SerializedProperty role = markerSync.FindPropertyRelative("m_SyncRole");
                SerializedProperty markers = markerSync.FindPropertyRelative("m_Markers");
                SerializedProperty segments = markerSync.FindPropertyRelative("m_Segments");
                EditorGUILayout.LabelField(producerIdentity, EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Channel / Group", $"{animationChannelId} / {markerSync.FindPropertyRelative("m_CanonicalGroupId").stringValue}");
                EditorGUILayout.LabelField(
                    "Mode / Topology / Role",
                    $"{mode.enumDisplayNames[mode.enumValueIndex]} / {topology.enumDisplayNames[topology.enumValueIndex]} / {role.enumDisplayNames[role.enumValueIndex]}");
                EditorGUILayout.LabelField("Markers / Segments", $"{markers.arraySize} / {segments.arraySize}");
                EditorGUI.indentLevel--;
            }
        }
    }
}
