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
        bool m_ShowLinkedPose = true;

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

            SerializedProperty curveCatalog = projection.FindPropertyRelative("m_BlendCurveCatalog")?.FindPropertyRelative("m_Entries");
            SerializedProperty profileCatalog = projection.FindPropertyRelative("m_BlendProfileCatalog")?.FindPropertyRelative("m_Entries");
            SerializedProperty producers = projection.FindPropertyRelative("m_Producers");
            SerializedProperty poseProgram = projection.FindPropertyRelative("m_PosePlan");
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
            SerializedProperty poseOperations = poseProgram?.FindPropertyRelative("m_Operations");
            int playerCount = 0;
            int controlInputCount = 0;
            for (int i = 0; i < (poseOperations?.arraySize ?? 0); i++)
            {
                int code = poseOperations.GetArrayElementAtIndex(i).FindPropertyRelative("m_Code").enumValueIndex;
                if (code ==
                    (int)CharacterPoseOperationCode.ActionPlaybackInput)
                    controlInputCount++;
                if (code == (int)CharacterPoseOperationCode.SelectedPosePlayer ||
                    code == (int)CharacterPoseOperationCode.BlendStack ||
                    code == (int)CharacterPoseOperationCode.BlendSpacePlayer)
                    playerCount++;
            }
            EditorGUILayout.LabelField("Control Inputs / Players", $"{controlInputCount} / {playerCount}");
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
                EditorGUILayout.LabelField("Plan Hash", poseProgram.FindPropertyRelative("m_PlanHash").stringValue);
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
                EditorGUILayout.LabelField("Bones", rig.FindPropertyRelative("m_PhysicalBones").arraySize.ToString());
                EditorGUILayout.LabelField("Skeleton Root / Solver Root / Pelvis", $"{rig.FindPropertyRelative("m_RootPhysicalBoneIndex").intValue} / {rig.FindPropertyRelative("m_SolverRootPhysicalBoneIndex").intValue} / {rig.FindPropertyRelative("m_PelvisPhysicalBoneIndex").intValue}");
                SerializedProperty spine = rig.FindPropertyRelative("m_OrderedSpinePhysicalBoneIndices");
                EditorGUILayout.LabelField("Ordered Spine", spine == null ? "Missing" : spine.arraySize.ToString());
                DrawArmChain("Left Arm", rig.FindPropertyRelative("m_LeftArm"));
                DrawArmChain("Right Arm", rig.FindPropertyRelative("m_RightArm"));
                DrawLegChain("Left Leg", rig.FindPropertyRelative("m_LeftLeg"));
                DrawLegChain("Right Leg", rig.FindPropertyRelative("m_RightLeg"));
                EditorGUILayout.LabelField("Head", rig.FindPropertyRelative("m_HeadPhysicalBoneIndex").intValue.ToString());
            }

            DrawLinkedPoseProjection(projection);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Action Playback Inputs", EditorStyles.boldLabel);
            SerializedProperty actionInputs =
                poseProgram?.FindPropertyRelative("m_ActionPlaybackInputs");
            for (int i = 0; i < (actionInputs?.arraySize ?? 0); i++)
            {
                SerializedProperty input =
                    actionInputs.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField(
                    input.FindPropertyRelative("m_AnimationChannelId").stringValue,
                    $"{input.FindPropertyRelative("m_SlotId").stringValue} / " +
                    input.FindPropertyRelative("m_ProgramProducerId").stringValue);
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

        void DrawLinkedPoseProjection(SerializedProperty projection)
        {
            EditorGUILayout.Space();
            SerializedProperty linked = projection.FindPropertyRelative("m_LinkedPose");
            m_ShowLinkedPose = EditorGUILayout.Foldout(
                m_ShowLinkedPose,
                "Linked Pose Projection",
                true);
            if (!m_ShowLinkedPose)
                return;
            if (linked == null)
            {
                EditorGUILayout.HelpBox(
                    "This Projection ABI has no Linked Pose payload.",
                    MessageType.Warning);
                return;
            }

            SerializedProperty groups = linked.FindPropertyRelative("m_Groups");
            SerializedProperty interfaces = linked.FindPropertyRelative("m_Interfaces");
            SerializedProperty selectors = linked.FindPropertyRelative("m_Selectors");
            SerializedProperty equipmentSelectors = linked.FindPropertyRelative("m_EquipmentSelectors");
            SerializedProperty implementations = linked.FindPropertyRelative("m_Implementations");
            SerializedProperty calls = linked.FindPropertyRelative("m_Calls");
            string rigId = ReadString(linked, "m_RigId");
            string rigRevision = ReadString(linked, "m_RigRevision");
            string factContract = ReadString(linked, "m_FactContractIdentity");
            string executionContract = ReadString(linked, "m_ExecutionContract");
            if (string.IsNullOrWhiteSpace(rigId) &&
                (groups?.arraySize ?? 0) == 0 &&
                (implementations?.arraySize ?? 0) == 0)
            {
                EditorGUILayout.HelpBox(
                    "Linked Pose was not compiled into this Projection.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Rig", $"{rigId} @ {rigRevision}");
            EditorGUILayout.LabelField("Fact Contract", factContract);
            EditorGUILayout.LabelField("Execution Contract", executionContract);
            EditorGUILayout.LabelField(
                "Interfaces / Groups / Selectors",
                $"{interfaces?.arraySize ?? 0} / {groups?.arraySize ?? 0} / {selectors?.arraySize ?? 0}");
            EditorGUILayout.LabelField(
                "Implementations / Calls",
                $"{implementations?.arraySize ?? 0} / {calls?.arraySize ?? 0}");

            DrawLinkedInterfaces(interfaces);
            DrawLinkedGroups(groups);
            DrawLinkedSelectors(selectors);
            DrawEquipmentSelectors(equipmentSelectors);
            DrawLinkedImplementations(implementations);
            DrawLinkedCalls(calls);
            EditorGUILayout.HelpBox(
                "This read-only view reports the directory currently serialized in the Projection. Entry operation/stage ranges, candidate source closure, group maximum layout and live generation state are not present in the current payload and are therefore not inferred here.",
                MessageType.Info);
        }

        static void DrawLinkedInterfaces(SerializedProperty interfaces)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Interfaces ({interfaces?.arraySize ?? 0})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < (interfaces?.arraySize ?? 0); i++)
            {
                SerializedProperty value = interfaces.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(ReadString(value, "m_InterfaceId"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Revision", ReadUnsigned(value, "m_Revision"));
                EditorGUILayout.LabelField("Signature", ReadString(value, "m_SignatureHash"));
                EditorGUILayout.LabelField("Fact Contract", ReadString(value, "m_FactContractIdentity"));
                EditorGUILayout.LabelField("Execution Contract", ReadString(value, "m_ExecutionContract"));
                EditorGUILayout.LabelField("Entries", (value.FindPropertyRelative("m_Entries")?.arraySize ?? 0).ToString());
                EditorGUILayout.EndVertical();
            }
        }

        static void DrawLinkedGroups(SerializedProperty groups)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Groups ({groups?.arraySize ?? 0})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < (groups?.arraySize ?? 0); i++)
            {
                SerializedProperty value = groups.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(ReadString(value, "m_GroupId"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Interface", ReadString(value, "m_InterfaceId"));
                EditorGUILayout.LabelField("Signature", ReadString(value, "m_InterfaceSignature"));
                EditorGUILayout.LabelField("Selector", ReadString(value, "m_SelectorId"));
                EditorGUILayout.EndVertical();
            }
        }

        static void DrawLinkedSelectors(SerializedProperty selectors)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Selectors ({selectors?.arraySize ?? 0})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < (selectors?.arraySize ?? 0); i++)
            {
                SerializedProperty value = selectors.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(ReadString(value, "m_SelectorId"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Group / Interface", $"{ReadString(value, "m_GroupId")} / {ReadString(value, "m_InterfaceId")}");
                SerializedProperty candidates = value.FindPropertyRelative("m_CandidateImplementationIds");
                EditorGUILayout.LabelField("Candidate Closure", (candidates?.arraySize ?? 0).ToString());
                for (int candidateIndex = 0; candidateIndex < (candidates?.arraySize ?? 0); candidateIndex++)
                    EditorGUILayout.LabelField($"Candidate {candidateIndex + 1}", candidates.GetArrayElementAtIndex(candidateIndex).stringValue);
                EditorGUILayout.EndVertical();
            }
        }

        static void DrawEquipmentSelectors(SerializedProperty selectors)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Equipment Selectors ({selectors?.arraySize ?? 0})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < (selectors?.arraySize ?? 0); i++)
            {
                SerializedProperty value = selectors.GetArrayElementAtIndex(i);
                SerializedProperty core = value.FindPropertyRelative("m_Core");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(ReadString(core, "m_SelectorId"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Group", ReadString(core, "m_GroupId"));
                EditorGUILayout.LabelField("Equipment Slot", ReadString(value, "m_SlotId"));
                EditorGUILayout.LabelField("Empty Equipment", ReadString(value, "m_EmptyImplementationId"));
                SerializedProperty mappings = value.FindPropertyRelative("m_Mappings");
                for (int mappingIndex = 0; mappingIndex < (mappings?.arraySize ?? 0); mappingIndex++)
                {
                    SerializedProperty mapping = mappings.GetArrayElementAtIndex(mappingIndex);
                    EditorGUILayout.LabelField(
                        ReadString(mapping, "m_EquipmentId"),
                        ReadString(mapping, "m_ImplementationId"));
                }
                EditorGUILayout.EndVertical();
            }
        }

        static void DrawLinkedImplementations(SerializedProperty implementations)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Implementations ({implementations?.arraySize ?? 0})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < (implementations?.arraySize ?? 0); i++)
            {
                SerializedProperty value = implementations.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(ReadString(value, "m_ImplementationId"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Revision", ReadUnsigned(value, "m_Revision"));
                EditorGUILayout.LabelField("Interface / Signature", $"{ReadString(value, "m_InterfaceId")} / {ReadString(value, "m_InterfaceSignature")}");
                EditorGUILayout.LabelField("Content Hash", ReadString(value, "m_ContentHash"));
                EditorGUILayout.LabelField("Rig", $"{ReadString(value, "m_RigId")} @ {ReadString(value, "m_RigRevision")}");
                SerializedProperty entries = value.FindPropertyRelative("m_Entries");
                for (int entryIndex = 0; entryIndex < (entries?.arraySize ?? 0); entryIndex++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(entryIndex);
                    EditorGUILayout.LabelField(
                        ReadString(entry, "m_EntryId"),
                        $"{ReadString(entry, "m_GraphId")} @ {ReadString(entry, "m_GraphContentRevision")}");
                }
                EditorGUILayout.EndVertical();
            }
        }

        static void DrawLinkedCalls(SerializedProperty calls)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Root Calls ({calls?.arraySize ?? 0})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < (calls?.arraySize ?? 0); i++)
            {
                SerializedProperty value = calls.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(ReadString(value, "m_EntryId"), EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Group / Interface", $"{ReadString(value, "m_GroupId")} / {ReadString(value, "m_InterfaceId")}");
                EditorGUILayout.LabelField("Signature", ReadString(value, "m_InterfaceSignature"));
                EditorGUILayout.LabelField("Call Node", ReadString(value, "m_NodeId"));
                EditorGUILayout.EndVertical();
            }
        }

        static string ReadString(SerializedProperty owner, string name) =>
            owner?.FindPropertyRelative(name)?.stringValue ?? string.Empty;

        static string ReadUnsigned(SerializedProperty owner, string name)
        {
            SerializedProperty value = owner?.FindPropertyRelative(name);
            return value == null ? string.Empty : value.longValue.ToString();
        }

        static void DrawLegChain(string label, SerializedProperty leg)
        {
            if (leg == null)
            {
                EditorGUILayout.LabelField(label, "Missing");
                return;
            }
            EditorGUILayout.LabelField(
                label,
                $"{leg.FindPropertyRelative("m_HipPhysicalBoneIndex").intValue} → " +
                $"{leg.FindPropertyRelative("m_KneePhysicalBoneIndex").intValue} → " +
                $"{leg.FindPropertyRelative("m_AnklePhysicalBoneIndex").intValue} → " +
                leg.FindPropertyRelative("m_ToePhysicalBoneIndex").intValue);
        }

        static void DrawArmChain(string label, SerializedProperty arm)
        {
            if (arm == null)
            {
                EditorGUILayout.LabelField(label, "Missing");
                return;
            }
            EditorGUILayout.LabelField(
                label,
                $"{arm.FindPropertyRelative("m_ClaviclePhysicalBoneIndex").intValue} → " +
                $"{arm.FindPropertyRelative("m_UpperArmPhysicalBoneIndex").intValue} → " +
                $"{arm.FindPropertyRelative("m_ForearmPhysicalBoneIndex").intValue} → " +
                arm.FindPropertyRelative("m_HandPhysicalBoneIndex").intValue);
        }
    }
}
