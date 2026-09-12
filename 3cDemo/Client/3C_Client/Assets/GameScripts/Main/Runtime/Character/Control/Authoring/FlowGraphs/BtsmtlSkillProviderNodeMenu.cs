#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using NodeCanvas.Framework;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillProviderNodeMenu
    {
        public static void Show(FlowGraph graph)
        {
            var menu = new GenericMenu();
            Append(graph, menu, NextNodePosition(graph), null, null);
            menu.ShowAsContext();
        }

        public static void Append(
            FlowGraph graph,
            GenericMenu menu,
            Vector2 position,
            Port context,
            UnityEngine.Object instance)
        {
            if (instance != null)
                return;
            if (!TryResolveDefinition(graph, out CharacterPipelineDefinition definition, out string reason))
            {
                menu.AddDisabledItem(new GUIContent("BTSMTL/Provider/Unavailable · " + reason));
                return;
            }

            menu.AddSeparator("BTSMTL/Provider/");
            string controlOwner = $"control-module:{definition.ControlModuleId}";
            AppendConfigured(graph, menu, "BTSMTL/Provider/Character State/Get Facing Angle", RequireType("character-move-facing-angle"), position, context,
                node => ((BtsmtlSkillMoveFacingAngleFlowNode)node).Configure(controlOwner));
            AppendConfigured(graph, menu, "BTSMTL/Provider/Character State/Get Position", RequireType("character-state-vector3"), position, context,
                node => ((BtsmtlSkillCharacterStateVector3FlowNode)node).Configure(CharacterStateProviderFields.Position, controlOwner));
            AppendConfigured(graph, menu, "BTSMTL/Provider/Character State/Get Velocity", RequireType("character-state-vector3"), position, context,
                node => ((BtsmtlSkillCharacterStateVector3FlowNode)node).Configure(CharacterStateProviderFields.Velocity, controlOwner));
            AppendConfigured(graph, menu, "BTSMTL/Provider/Character State/Get Vertical Velocity", RequireType("character-state-scalar"), position, context,
                node => ((BtsmtlSkillCharacterStateScalarFlowNode)node).Configure(CharacterStateProviderFields.VerticalVelocity, controlOwner));
            AppendConfigured(graph, menu, "BTSMTL/Provider/Character State/Get Body Yaw", RequireType("character-state-yaw"), position, context,
                node => ((BtsmtlSkillCharacterStateYawFlowNode)node).Configure(CharacterStateProviderFields.BodyYaw, controlOwner));
            AppendConfigured(graph, menu, "BTSMTL/Provider/Character State/Get Grounded", RequireType("character-state-bool"), position, context,
                node => ((BtsmtlSkillCharacterStateBooleanFlowNode)node).Configure(CharacterStateProviderFields.Grounded, controlOwner));

            AppendInputNodes(graph, menu, position, context, definition.InputProfile);
            AppendGameplayNodes(graph, menu, position, context, definition.GameplayEffectProfile);
        }

        static void AppendInputNodes(
            FlowGraph graph,
            GenericMenu menu,
            Vector2 position,
            Port context,
            CharacterInputProfile inputProfile)
        {
            if (!inputProfile)
                return;
            string owner = AssetOwnerId(inputProfile);
            for (int i = 0; i < inputProfile.InputValues.Count; i++)
            {
                CharacterInputValueDefinition input = inputProfile.InputValues[i];
                if (input == null || string.IsNullOrWhiteSpace(input.InputValueId))
                    continue;
                string kind = input.ValueType switch
                {
                    CharacterInputValueType.Bool => "character-input-bool",
                    CharacterInputValueType.Float => "character-input-float",
                    CharacterInputValueType.Vector2 => "character-input-vector2",
                    _ => string.Empty
                };
                Type nodeType = string.IsNullOrEmpty(kind) ? null : RequireType(kind);
                if (nodeType == null)
                    continue;
                string inputId = input.InputValueId;
                AppendConfigured(graph, menu, "BTSMTL/Provider/Input / " + inputId, nodeType, position, context,
                    node => ((IBtsmtlSkillInputNode)node).SetInputId(inputId, owner));
            }

            for (int i = 0; i < inputProfile.ActionRequests.Count; i++)
            {
                CharacterActionRequestDefinition request = inputProfile.ActionRequests[i];
                if (request == null || string.IsNullOrWhiteSpace(request.RequestId))
                    continue;
                string requestId = request.RequestId;
                AppendConfigured(graph, menu, "BTSMTL/Provider/TargetData / " + requestId,
                    RequireType("character-action-request"), position, context,
                    node => ((IBtsmtlSkillInputNode)node).SetInputId(requestId, owner));
            }
        }

        static void AppendGameplayNodes(
            FlowGraph graph,
            GenericMenu menu,
            Vector2 position,
            Port context,
            CharacterGameplayEffectProfile gameplay)
        {
            if (!gameplay)
                return;
            string owner = AssetOwnerId(gameplay);
            for (int i = 0; i < gameplay.AttributeDefinitions.Count; i++)
            {
                GameplayAttributeDefinition attribute = gameplay.AttributeDefinitions[i];
                if (!attribute || !attribute.AttributeId.IsValid)
                    continue;
                GameplayAttributeId attributeId = attribute.AttributeId;
                string label = string.IsNullOrWhiteSpace(attribute.DisplayName) ? attribute.name : attribute.DisplayName;
                AppendConfigured(graph, menu, "BTSMTL/Provider/Ability Attribute/Get / " + label,
                    RequireType("gameplay-attribute-read"), position, context,
                    node => ((BtsmtlSkillGameplayAttributeFlowNode)node).Configure(attributeId, owner));
            }

            if (gameplay.TagCatalog)
            {
                for (int i = 0; i < gameplay.TagCatalog.Tags.Count; i++)
                {
                    GameplayTagDefinition tag = gameplay.TagCatalog.Tags[i];
                    if (tag == null || !tag.TagId.IsValid)
                        continue;
                    GameplayTagId tagId = tag.TagId;
                    string label = string.IsNullOrWhiteSpace(tag.DisplayName) ? tagId.Value : tag.DisplayName;
                    AppendConfigured(graph, menu, "BTSMTL/Provider/Gameplay Tag/Get / " + label,
                        RequireType("gameplay-tag-has"), position, context,
                        node => ((BtsmtlSkillGameplayTagFlowNode)node).Configure(tagId, owner));
                }
            }

            for (int i = 0; i < gameplay.EffectDefinitions.Count; i++)
            {
                GameplayEffectDefinition effect = gameplay.EffectDefinitions[i];
                if (!effect)
                    continue;
                GameplayEffectDefinition selected = effect;
                string label = string.IsNullOrWhiteSpace(effect.DisplayName) ? effect.name : effect.DisplayName;
                AppendConfigured(graph, menu, "BTSMTL/Provider/Gameplay Effect/Apply / " + label,
                    RequireType("gameplay-effect-apply"), position, context,
                    node => ((BtsmtlSkillApplyGameplayEffectFlowNode)node).Configure(selected, null, false, owner));
                AppendConfigured(graph, menu, "BTSMTL/Provider/Gameplay Effect/Remove / " + label,
                    RequireType("gameplay-effect-remove"), position, context,
                    node => ((BtsmtlSkillRemoveGameplayEffectFlowNode)node).Configure(
                        GameplayEffectRemoveSelector.EffectId, 0, selected, null, owner));
            }
        }

        static void AppendConfigured(
            FlowGraph graph,
            GenericMenu menu,
            string category,
            Type nodeType,
            Vector2 position,
            Port context,
            Action<FlowNode> configure) =>
            BtsmtlSkillFlowEditorMutation.AppendConfiguredCreationItem(
                graph, menu, category, nodeType, position, context, configure);

        static Type RequireType(string kind)
        {
            if (!BtsmtlSkillGraphAuthoringMetadata.TryResolveType(kind, out Type type))
                throw new InvalidOperationException($"技能节点kind无法解析：{kind}");
            return type;
        }

        static bool TryResolveDefinition(
            FlowGraph graph,
            out CharacterPipelineDefinition definition,
            out string reason)
        {
            var matches = new List<CharacterPipelineDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterPipelineDefinition"))
            {
                CharacterPipelineDefinition candidate = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (!candidate || candidate.SkillGraphs == null)
                    continue;
                bool found = candidate.SkillGraphs.Any(root =>
                {
                    if (!root)
                        return false;
                    try { return BtsmtlSkillGraphClosure.Validate(root, false).Contains(graph); }
                    catch (InvalidOperationException) { return false; }
                });
                if (found)
                    matches.Add(candidate);
            }
            if (matches.Count == 1)
            {
                definition = matches[0];
                reason = string.Empty;
                return true;
            }
            definition = null;
            reason = matches.Count == 0 ? "缺少精确Character Definition上下文" : "存在多个Character Definition上下文";
            return false;
        }

        static string AssetOwnerId(UnityEngine.Object asset)
        {
            if (!asset)
                return string.Empty;
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            return string.IsNullOrEmpty(guid) ? string.Empty : "asset:" + guid;
        }

        static Vector2 NextNodePosition(FlowGraph graph) =>
            new Vector2(80f + graph.allNodes.Count % 4 * 220f, 80f + graph.allNodes.Count / 4 * 120f);
    }
}
#endif
