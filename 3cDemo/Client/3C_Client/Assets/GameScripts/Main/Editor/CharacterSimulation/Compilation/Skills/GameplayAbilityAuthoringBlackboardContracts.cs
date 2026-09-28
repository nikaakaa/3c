using BTSMTL.Authoring.Blackboard;
using System;
using FlowCanvas;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class GameplayAbilityBlackboardDeclarationSnapshot
    {
        public GameplayAbilityBlackboardDeclarationSnapshot(string identity, string key, Type valueType,
            PipelineBlackboardVariableScope scope, PipelineBlackboardVariableLifetime lifetime, string category,
            object defaultValue, PipelineBlackboardInputBinding inputBinding, PipelineBlackboardFactProjection factProjection)
        {
            DeclarationId = identity;
            BlackboardKey = key;
            ValueType = valueType;
            BlackboardScope = scope;
            BlackboardLifetime = lifetime;
            BlackboardCategoryPath = category;
            DefaultValue = defaultValue;
            InputBinding = inputBinding;
            FactProjection = factProjection;
        }

        public string DeclarationId { get; }
        public string BlackboardKey { get; }
        public Type ValueType { get; }
        public PipelineBlackboardVariableScope BlackboardScope { get; }
        public PipelineBlackboardVariableLifetime BlackboardLifetime { get; }
        public string BlackboardCategoryPath { get; }
        public object DefaultValue { get; }
        public PipelineBlackboardInputBinding InputBinding { get; }
        public string InputValueId => InputBinding?.InputValueId ?? string.Empty;
        public PipelineBlackboardFactProjection FactProjection { get; }
    }

    public sealed class GameplayAbilityAuthoringBlackboardDeclaration
    {
        internal GameplayAbilityAuthoringBlackboardDeclaration(FlowGraph graph, BtsmtlSkillBlackboardDeclaration declaration, string route, string contentHash)
        {
            Variable variable = BtsmtlSkillBlackboardDeclarations.RequireVariable(graph, declaration.VariableId);
            GraphId = ((IBtsmtlSkillFlowGraph)graph).AuthoringId;
            SourceType = variable.GetType().FullName;
            ContentHash = contentHash;
            AuthoringDeclaration = variable;
            Declaration = new GameplayAbilityBlackboardDeclarationSnapshot(variable.ID, variable.name, variable.varType,
                declaration.Scope, declaration.Lifetime, declaration.Category, ((ISerializedVariableValue)variable).serializedValue,
                declaration.InputBinding, declaration.FactProjection);
            Route = route;
        }

        public string GraphId { get; }
        public string SourceType { get; }
        public string ContentHash { get; }
        public object AuthoringDeclaration { get; }
        public GameplayAbilityBlackboardDeclarationSnapshot Declaration { get; }
        public string Route { get; }
    }
}
