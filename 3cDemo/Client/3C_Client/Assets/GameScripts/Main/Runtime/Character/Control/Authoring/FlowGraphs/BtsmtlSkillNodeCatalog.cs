#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas.Macros;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillNodeCatalog
    {
        public static IReadOnlyList<Type> All { get; } = Array.AsReadOnly(new[]
        {
            typeof(BtsmtlSkillSequenceFlowNode),
            typeof(BtsmtlSkillSelectorFlowNode),
            typeof(BtsmtlSkillLoopFlowNode),
            typeof(BtsmtlSkillParallelFlowNode),
            typeof(BtsmtlSkillSucceedFlowNode),
            typeof(BtsmtlSkillStateMachineFlowNode),
            typeof(BtsmtlSkillStateFlowNode),
            typeof(BtsmtlSkillStateRootCompletedFlowNode),
            typeof(BtsmtlSkillStateExitCauseFlowNode),
            typeof(BtsmtlSkillTimelineFlowNode),
            typeof(BtsmtlSkillBooleanInputFlowNode),
            typeof(BtsmtlSkillScalarInputFlowNode),
            typeof(BtsmtlSkillVector2InputFlowNode),
            typeof(BtsmtlSkillInputMagnitudeFlowNode),
            typeof(BtsmtlSkillActionRequestFlowNode),
            typeof(BtsmtlSkillActionContextActiveFlowNode),
            typeof(BtsmtlSkillActionWindowActiveFlowNode),
            typeof(BtsmtlSkillCanActivateActionFlowNode),
            typeof(BtsmtlSkillSubmitActionLifecycleFlowNode),
            typeof(BtsmtlSkillMoveFacingAngleFlowNode),
            typeof(BtsmtlSkillBlackboardBooleanFlowNode),
            typeof(BtsmtlSkillBlackboardScalarFlowNode),
            typeof(BtsmtlSkillBlackboardGetFlowNode),
            typeof(BtsmtlSkillBlackboardSetFlowNode),
            typeof(BtsmtlSkillLocomotionFlowNode),
            typeof(BtsmtlSkillRootFlowNode),
            typeof(BtsmtlSkillStateOnEnterFlowNode),
            typeof(BtsmtlSkillStateOnExitFlowNode),
            typeof(BtsmtlSkillStateEnterFlowNode),
            typeof(BtsmtlSkillStateAnyFlowNode),
            typeof(BtsmtlSkillStateExitFlowNode),
            typeof(BtsmtlSkillConditionResultFlowNode),
            typeof(BtsmtlSkillTimelineEnableFlowNode),
            typeof(BtsmtlSkillTimelineDisableFlowNode),
            typeof(BtsmtlSkillTimelineDestroyFlowNode),
            typeof(MacroNodeWrapper),
            typeof(MacroInputNode),
            typeof(MacroOutputNode),
            typeof(BtsmtlSkillMacroInputNode),
            typeof(BtsmtlSkillMacroOutputNode)
        }.Concat(BtsmtlSkillNativeNodeCatalog.All.Select(value => value.NodeType)).ToArray());

        static readonly HashSet<Type> s_Types = new(All);
        public static bool Contains(Type type) => type != null && s_Types.Contains(type);
    }
}
#endif
