using System;
using System.Collections.Generic;
using BTSMTL.Authoring.Blackboard;

namespace TreeDesigner
{
    public interface IPipelineBlackboardRuntimeAccess
    {
        void RegisterPipelineBlackboardVariables(BaseGraph graph, IReadOnlyList<BaseExposedProperty> variables);
        void UnregisterPipelineBlackboardGraph(BaseGraph graph);
        bool TryResolvePipelineBlackboardDeclaration(PipelineBlackboardVariableReference reference, out BaseExposedProperty declaration);
        bool TryGetPipelineBlackboardValue(BaseGraph accessGraph, PipelineBlackboardVariableReference reference, Type expectedType, out object value);
        bool SetPipelineBlackboardValue(
            BaseGraph accessGraph,
            PipelineBlackboardVariableReference reference,
            object value,
            UnityEngine.Object factContext);
        void NotifyPipelineBlackboardStateEntered(StateMachineExecutionScope scope);
        void NotifyPipelineBlackboardStateExited(StateMachineExecutionScope scope);
    }
}
