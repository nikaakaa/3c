using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentMutationLoweringSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentStateBehaviorMutationLowering
    {
        internal static AgentMutation LowerEnsureActionExitLifecycle(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference source = context.OptionalElement(operation.sourceElementAuthoringId, operation.sourcePlannedIdentity, "sourceElement");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            AgentAssetReference actionContext = ReadActionContext(operation);
            List<AgentConditionGroupMutation> cancelConditionGroups = context.RequiredConditionGroups(operation.cancelConditionGroups, operation, "cancelConditionGroups");
            string cancelReason = context.RequiredText(operation.reason, string.Empty, "reason", "ensure_action_exit_lifecycle 必须显式提供 cancel reason。");
            string interruptReason = context.RequiredText(operation.interruptReason, string.Empty, "interruptReason", "ensure_action_exit_lifecycle 必须显式提供 interrupt reason。");
            string abortReason = context.RequiredText(operation.abortReason, string.Empty, "abortReason", "ensure_action_exit_lifecycle 必须显式提供 abort reason。");
            string completeReason = context.RequiredText(operation.completeReason, string.Empty, "completeReason", "ensure_action_exit_lifecycle 必须显式提供 complete reason。");
            return context.IsValid
                ? new AgentEnsureActionExitLifecycleMutation(
                    operation.id,
                    context.Path,
                    target,
                    source,
                    existing,
                    actionContext,
                    cancelReason,
                    interruptReason,
                    abortReason,
                    completeReason,
                    cancelConditionGroups,
                    operation.position)
                : null;
        }

        internal static AgentMutation LowerDeleteStateBehaviorNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference element = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            return context.IsValid
                ? new AgentDeleteStateBehaviorNodeMutation(operation.id, context.Path, target, element)
                : null;
        }

        internal static AgentMutation LowerEnsureStateBehaviorNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string nodeType = First(operation.nodeType, "SequenceNode");
            string displayName = First(operation.displayName, nodeType);
            LoopNode.StopType loopStopType = LoopNode.StopType.None;
            CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
            if (!string.IsNullOrEmpty(operation.loopStopType))
                TryParseEnum(context, operation.loopStopType, "loopStopType", out loopStopType);
            if (!string.IsNullOrEmpty(operation.compareType))
                TryParseEnum(context, operation.compareType, "compareType", out compareType);
            if (string.Equals(nodeType, typeof(LocomotionInputMotionNode).FullName, StringComparison.Ordinal))
            {
                if (!Enum.TryParse(operation.displacementMode, false, out LocomotionInputMotionDisplacementMode displacementMode) ||
                    !Enum.IsDefined(typeof(LocomotionInputMotionDisplacementMode), displacementMode))
                {
                    context.Error("displacementMode", "displacement_mode_invalid", $"locomotion-input-motion 的 displacementMode 无效：{operation.displacementMode}");
                    displacementMode = LocomotionInputMotionDisplacementMode.ConstantSpeed;
                }
                if (float.IsNaN(operation.moveSpeed) || float.IsInfinity(operation.moveSpeed) || operation.moveSpeed < 0f)
                    context.Error("moveSpeed", "move_speed_invalid", "locomotion-input-motion 的 moveSpeed 必须大于等于0且为有限值。");
                bool hasActionMotionCurve = !string.IsNullOrEmpty(operation.actionMotionCurveAssetPath) || !string.IsNullOrEmpty(operation.actionMotionCurveAssetGuid);
                if (displacementMode == LocomotionInputMotionDisplacementMode.ActionMotionCurve && operation.moveSpeed != 0f)
                    context.Error("moveSpeed", "curve_move_speed_invalid", "ActionMotionCurve locomotion 的 moveSpeed 必须为0。");
                if (displacementMode == LocomotionInputMotionDisplacementMode.ActionMotionCurve && !hasActionMotionCurve)
                    context.Error("actionMotionCurve", "action_motion_curve_required", "ActionMotionCurve locomotion 必须声明正式曲线资产。");
                if (displacementMode == LocomotionInputMotionDisplacementMode.ConstantSpeed && hasActionMotionCurve)
                    context.Error("actionMotionCurve", "constant_speed_curve_forbidden", "ConstantSpeed locomotion 不能声明Action Motion Curve。");
                if (float.IsNaN(operation.turnSpeedDegrees) || float.IsInfinity(operation.turnSpeedDegrees) || operation.turnSpeedDegrees <= 0f)
                    context.Error("turnSpeedDegrees", "turn_speed_invalid", "locomotion-input-motion 的 turnSpeedDegrees 必须大于0且为有限值。");
                if (!Enum.TryParse(operation.executionMode, false, out LocomotionInputMotionExecutionMode executionMode) ||
                    !Enum.IsDefined(typeof(LocomotionInputMotionExecutionMode), executionMode))
                {
                    context.Error("executionMode", "execution_mode_invalid", $"locomotion-input-motion 的 executionMode 无效：{operation.executionMode}");
                    executionMode = LocomotionInputMotionExecutionMode.Once;
                }
                if (float.IsNaN(operation.durationSeconds) || float.IsInfinity(operation.durationSeconds) || operation.durationSeconds < 0f)
                    context.Error("durationSeconds", "duration_invalid", "locomotion-input-motion 的 durationSeconds 必须大于等于0且为有限值。");
                else if (executionMode == LocomotionInputMotionExecutionMode.Timed && operation.durationSeconds <= 0f)
                    context.Error("durationSeconds", "timed_duration_invalid", "Timed locomotion-input-motion 的 durationSeconds 必须大于0。");
                else if (executionMode != LocomotionInputMotionExecutionMode.Timed && operation.durationSeconds != 0f)
                    context.Error("durationSeconds", "unused_duration_invalid", "非Timed locomotion-input-motion 的 durationSeconds 必须为0。");
            }
            Enum.TryParse(operation.displacementMode, false, out LocomotionInputMotionDisplacementMode resolvedDisplacementMode);
            Enum.TryParse(operation.executionMode, false, out LocomotionInputMotionExecutionMode resolvedExecutionMode);
            var actionMotionCurve = new AgentAssetReference(
                operation.actionMotionCurve,
                operation.actionMotionCurveAssetPath,
                operation.actionMotionCurveAssetGuid);
            return context.IsValid
                ? new AgentEnsureStateBehaviorNodeMutation(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    nodeType,
                    displayName,
                    operation.lifecycleSlot,
                    loopStopType,
                    compareType,
                    operation.moveSpeed,
                    resolvedDisplacementMode,
                    actionMotionCurve,
                    operation.turnSpeedDegrees,
                    operation.cameraRelative,
                    resolvedExecutionMode,
                    operation.durationSeconds,
                    operation.position)
                : null;
        }

        internal static AgentMutation LowerEnsureActionActivation(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string actionProfile = context.RequiredText(operation.actionProfile, string.Empty, "actionProfile", "ensure_action_activation 缺少 ActionProfile 引用。");
            string sourceRequest = First(operation.sourceInputRequestId, First(operation.inputId, operation.request));
            return context.IsValid
                ? new AgentEnsureActionActivationMutation(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    First(operation.displayName, $"Activate {actionProfile}"),
                    First(operation.lifecycleSlot, "OnEnter"),
                    new AgentAssetReference(actionProfile, string.Empty, string.Empty),
                    ReadActionContext(operation),
                    sourceRequest,
                    operation.consumeSourceInputRequest,
                    operation.targetKey,
                    operation.targetSnapshotBlackboardKey,
                    operation.position)
                : null;
        }

        internal static AgentMutation LowerEnsureActionLifecycleTransition(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentStateBehaviorTargetReference target = context.RequiredStateBehaviorTarget(operation);
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            if (!Enum.TryParse(operation.lifecycleType, true, out ActionLifecycleTransitionType transitionType) ||
                !Enum.IsDefined(typeof(ActionLifecycleTransitionType), transitionType))
            {
                context.Error("lifecycleType", "lifecycle_type_invalid", $"未知的 lifecycleType：{operation.lifecycleType}");
                transitionType = default;
            }
            return context.IsValid
                ? new AgentEnsureActionLifecycleTransitionMutation(
                    operation.id,
                    context.Path,
                    target,
                    existing,
                    First(operation.displayName, $"Lifecycle {operation.lifecycleType}"),
                    First(operation.lifecycleSlot, "OnExit"),
                    transitionType,
                    operation.reason,
                    ReadActionContext(operation),
                    operation.position)
                : null;
        }
    }
}

