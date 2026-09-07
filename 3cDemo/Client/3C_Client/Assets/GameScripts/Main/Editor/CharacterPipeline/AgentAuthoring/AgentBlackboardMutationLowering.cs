using System;
using ThirdPersonSimulation;
using TreeDesigner;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentMutationLoweringSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentBlackboardMutationLowering
    {
        internal static AgentMutation LowerEnsureBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string declarationAuthoringId = context.OptionalAuthoringId(operation.declarationAuthoringId, "declarationAuthoringId");
            string key = context.RequiredText(operation.blackboardKey, string.Empty, "blackboardKey", "ensure_blackboard_declaration 缺少 blackboardKey。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            object defaultValue = ReadBlackboardDefault(context, operation.blackboardDefaultValue, valueType);
            bool valid = TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope) &
                         TryParseEnum(context, operation.blackboardLifetime, "blackboardLifetime", out PipelineBlackboardVariableLifetime lifetime);
            ValidateBlackboardPayloads(context, operation.inputBinding, operation.factProjection);
            return context.IsValid && valid && valueType != null
                ? new AgentEnsureBlackboardDeclarationMutation(operation.id, context.Path, graph, declarationAuthoringId, key, valueType, defaultValue, scope, lifetime, operation.inputBinding, operation.factProjection, operation.categoryPath)
                : null;
        }

        internal static AgentMutation LowerDeleteBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string declaration = context.OptionalAuthoringId(context.RequiredText(operation.declarationAuthoringId, string.Empty, "declarationAuthoringId", "delete_blackboard_declaration 缺少 declaration identity。"), "declarationAuthoringId");
            return context.IsValid ? new AgentDeleteBlackboardDeclarationMutation(operation.id, context.Path, graph, declaration) : null;
        }

        internal static AgentMutation LowerSetBlackboardSchemaRevision(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            if (operation.blackboardSchemaRevision != PipelineBlackboardAuthoringSchema.CurrentRevision)
                context.Error("blackboardSchemaRevision", "blackboard_schema_revision_invalid", $"Blackboard schema revision 必须是 {PipelineBlackboardAuthoringSchema.CurrentRevision}。");
            return context.IsValid
                ? new AgentSetBlackboardSchemaRevisionMutation(operation.id, context.Path, graph, operation.blackboardSchemaRevision)
                : null;
        }

        internal static AgentMutation LowerMoveBlackboardDeclaration(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference sourceGraph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentGraphTargetReference targetGraph = context.RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphPlannedIdentity, "targetGraph");
            string declaration = context.OptionalAuthoringId(context.RequiredText(operation.declarationAuthoringId, string.Empty, "declarationAuthoringId", "move_blackboard_declaration 缺少 declaration identity。"), "declarationAuthoringId");
            string key = context.RequiredText(operation.blackboardKey, string.Empty, "blackboardKey", "move_blackboard_declaration 缺少 blackboardKey。");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            bool valid = TryParseEnum(context, operation.blackboardScope, "blackboardScope", out PipelineBlackboardVariableScope scope) &
                         TryParseEnum(context, operation.blackboardLifetime, "blackboardLifetime", out PipelineBlackboardVariableLifetime lifetime);
            ValidateBlackboardPayloads(context, operation.inputBinding, operation.factProjection);
            return context.IsValid && valid && valueType != null
                ? new AgentMoveBlackboardDeclarationMutation(operation.id, context.Path, sourceGraph, targetGraph, declaration, key, valueType, scope, lifetime, operation.inputBinding, operation.factProjection, operation.categoryPath)
                : null;
        }

        internal static AgentMutation LowerEnsureExposedPropertyNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.targetGraphAuthoringId, operation.targetGraphPlannedIdentity, "targetGraph");
            AgentAuthoringReference declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "declaration");
            Type valueType = ParseBlackboardValueType(context, operation.blackboardValueType);
            bool valid = TryParseEnum(context, operation.exposedPropertyMode, "exposedPropertyMode", out ExposedPropertyNodeType mode);
            object value = null;
            if (mode == ExposedPropertyNodeType.Set)
                value = ReadBlackboardDefault(context, operation.blackboardDefaultValue, valueType);
            else if (operation.blackboardDefaultValue != null && operation.blackboardDefaultValue.Type != Newtonsoft.Json.Linq.JTokenType.Null)
                context.Error("blackboardDefaultValue", "exposed_property_get_value_forbidden", "Get ExposedProperty 节点不能保存 value。");
            string displayName = First(operation.displayName, mode + " Blackboard");
            return context.IsValid && valid && valueType != null
                ? new AgentEnsureExposedPropertyNodeMutation(operation.id, context.Path, graph, operation.targetElementAuthoringId, declaration, mode, valueType, value, displayName, operation.position)
                : null;
        }
    }
}
