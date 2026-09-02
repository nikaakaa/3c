using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal static class CharacterPoseProgramImageSealPass
    {
        internal static CharacterPoseProgramImage Run(
            CharacterPoseCompilationRequest request,
            CharacterPoseFamilyPayloadBinding binding,
            CharacterPoseStageSchedule schedule,
            CharacterPoseWorkspacePlan workspace)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (binding == null)
                throw new ArgumentNullException(nameof(binding));
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            CharacterPoseBoundFamilyPayloads payloads = binding.Payloads;
            CharacterPoseBoundProgramLayout layout = binding.Layout;
            for (int rangeIndex = 0;
                 rangeIndex < schedule.FragmentRanges.Count;
                 rangeIndex++)
            {
                CharacterPoseLinkedFragmentStageRange range =
                    schedule.FragmentRanges[rangeIndex];
                payloads.LinkedPoseFragments[range.FragmentIndex]
                    .BindStageRange(
                        range.StageStart,
                        range.StageCount);
            }
            CharacterPresentationPoseStage[] stages =
                schedule.Stages.ToArray();
            CharacterTypedPoseGraph graph = request.Asset.Graph;
            CharacterAnimationRigDefinition rig = request.Rig;
            string hash = ComputeHash(
                graph,
                rig,
                binding,
                stages,
                workspace.PoseValueCapacity,
                workspace.ParameterValueCapacity,
                workspace.ContributionCapacity,
                workspace.FrameCacheCapacity);
            return new CharacterPoseProgramImage(
                graph.GraphId.Value,
                graph.ContentRevision,
                hash,
                rig,
                payloads.Parameters,
                payloads.BlendNodes,
                Array.Empty<CharacterPresentationInertializationDescriptor>(),
                payloads.BoneMasks,
                payloads.AdditiveReferences,
                payloads.ModifyBones,
                payloads.RootOrientationWarps,
                payloads.PoseBoneIkGoalSources,
                payloads.FootPlacements,
                payloads.FullBodyIks,
                payloads.FullBodyIkGoalContributionInputValueIndices,
                payloads.ClipPlayers,
                payloads.StateMachines,
                payloads.AnimationSlots,
                payloads.ActionPlaybackInputs,
                payloads.LinkedPoseFragments,
                payloads.LinkedPoseCalls,
                binding.Operations,
                binding.SourceMap,
                stages,
                layout.PoseValueCount,
                workspace.PoseValueCapacity,
                layout.FullBodyIkGoalContributionValueCount,
                layout.FullBodyIkGoalSetValueCount,
                layout.FullBodyIkGoalContributionGoalWorkspaceCount,
                workspace.ParameterValueCapacity,
                workspace.ContributionCapacity /
                workspace.PoseValueCapacity,
                workspace.FrameCacheCapacity,
                layout.OutputOperationIndex);
        }

        static string ComputeHash(
            CharacterTypedPoseGraph graph,
            CharacterAnimationRigDefinition rig,
            CharacterPoseFamilyPayloadBinding binding,
            IReadOnlyList<CharacterPresentationPoseStage> stages,
            int poseWorkspace,
            int parameterWorkspace,
            int contributionWorkspace,
            int frameCache)
        {
            CharacterPoseBoundFamilyPayloads payloads = binding.Payloads;
            CharacterPoseBoundProgramLayout layout = binding.Layout;
            var values = new List<string>
            {
                CharacterPoseProgramImage.SchemaVersion,
                CharacterPoseProgramImage.RuntimeAbi,
                graph.GraphId.Value,
                graph.ContentRevision,
                rig.RigId,
                rig.Revision
            };
            values.AddRange(binding.GraphDependencies.Select(
                value => "graph:" + value));
            for (int i = 0; i < payloads.Parameters.Length; i++)
            {
                CharacterPresentationPoseParameterEntry parameter =
                    payloads.Parameters[i];
                values.Add(FormattableString.Invariant(
                    $"parameter:{parameter.Index}:{parameter.ParameterId}:{(int)parameter.ValueType}:{parameter.Unit}:{parameter.DefaultValue:R}"));
            }
            for (int i = 0; i < payloads.BlendNodes.Length; i++)
            {
                AnimationBlendNodePayload blend = payloads.BlendNodes[i];
                values.Add(
                    $"blend:{blend.NodeId}:{blend.PolicyId}:{blend.PolicyRevision}:{blend.RoutingPlanId}:{blend.RoutingDefinitionRevision}:{blend.Transitions.Count}");
                for (int transitionIndex = 0;
                     transitionIndex < blend.Transitions.Count;
                     transitionIndex++)
                {
                    AnimationBlendTransitionPayload transition =
                        blend.Transitions[transitionIndex];
                    values.Add(FormattableString.Invariant(
                        $"blend-transition:{i}:{transitionIndex}:{transition.SourceOwnerIndex}:{(int)transition.SourceEndpointKind}:{transition.SourceOwnerIdentity}:{transition.TargetOwnerIndex}:{(int)transition.TargetEndpointKind}:{transition.TargetOwnerIdentity}:{(int)transition.BlendLogic}:{transition.DurationSeconds:R}:{transition.CurveIndex}:{transition.BlendProfileIndex}"));
                }
            }
            for (int i = 0; i < rig.VirtualBoneCount; i++)
            {
                CharacterAnimationVirtualBoneDefinition bone =
                    rig.VirtualBones[i];
                values.Add(
                    $"virtual-bone:{i}:{bone.VirtualBoneId}:{bone.DisplayName}:{bone.SourcePhysicalBoneId}:{bone.TargetPhysicalBoneId}");
            }
            for (int i = 0;
                 i < payloads.PoseBoneIkGoalSources.Length;
                 i++)
            {
                CharacterPresentationPoseBoneIkGoalsDescriptor descriptor =
                    payloads.PoseBoneIkGoalSources[i];
                values.Add(
                    $"pose-bone-ik-goals:{i}:{descriptor.NodeId}:{descriptor.ContributionGoalWorkspaceOffset}:{descriptor.GoalCount}");
                for (int bindingIndex = 0;
                     bindingIndex < descriptor.Bindings.Count;
                     bindingIndex++)
                {
                    CharacterPresentationPoseBoneIkGoalBindingDescriptor goal =
                        descriptor.Bindings[bindingIndex];
                    values.Add(FormattableString.Invariant(
                        $"pose-bone-ik-goal:{i}:{bindingIndex}:{(int)goal.EffectorSlot}:{goal.TargetPoseBoneIndex}:{goal.PositionOffset.x:R}:{goal.PositionOffset.y:R}:{goal.PositionOffset.z:R}:{goal.RotationOffset.x:R}:{goal.RotationOffset.y:R}:{goal.RotationOffset.z:R}:{goal.RotationOffset.w:R}:{goal.PositionWeight:R}:{goal.RotationWeight:R}"));
                }
            }
            for (int i = 0; i < payloads.FootPlacements.Length; i++)
            {
                CharacterPresentationFootPlacementDescriptor descriptor =
                    payloads.FootPlacements[i];
                values.Add(
                    $"foot-placement:{i}:{descriptor.NodeId}:{descriptor.Profile.ProfileId}:{descriptor.Profile.Revision}:{descriptor.CalibrationId}:{descriptor.CalibrationRevision}:{descriptor.ContributionGoalWorkspaceOffset}");
            }
            for (int i = 0; i < payloads.FullBodyIks.Length; i++)
            {
                CharacterPresentationFullBodyIkDescriptor descriptor =
                    payloads.FullBodyIks[i];
                values.Add(
                    $"full-body-ik:{i}:{descriptor.NodeId}:{descriptor.ProfileId}:{descriptor.ProfileRevision}:{descriptor.BackendIdentity}:{descriptor.BackendSourceRevision}");
            }
            for (int i = 0;
                 i < payloads
                     .FullBodyIkGoalContributionInputValueIndices.Length;
                 i++)
            {
                values.Add(
                    $"full-body-ik-goal-contribution-input:{i}:{payloads.FullBodyIkGoalContributionInputValueIndices[i]}");
            }
            for (int i = 0; i < payloads.ClipPlayers.Length; i++)
            {
                CharacterPresentationClipPlayerDescriptor descriptor =
                    payloads.ClipPlayers[i];
                values.Add(FormattableString.Invariant(
                    $"clip-player:{descriptor.Index}:{descriptor.NodeId}:{descriptor.PresentationPoseSourceIndex.Value}:{descriptor.PlayRate:R}:{descriptor.InitialTime:R}:{(int)descriptor.ClockSource}:{descriptor.PlayerIndex}"));
            }
            for (int i = 0;
                 i < payloads.RootOrientationWarps.Length;
                 i++)
            {
                CharacterPresentationRootOrientationWarpDescriptor descriptor =
                    payloads.RootOrientationWarps[i];
                Keyframe[] keys = descriptor.YawCurve.keys;
                values.Add(FormattableString.Invariant(
                    $"root-orientation-warp:{descriptor.Index}:{descriptor.NodeId}:{descriptor.ClipPlayerIndex}:{descriptor.RootPhysicalBoneIndex}:{descriptor.Duration:R}:{descriptor.TotalYaw:R}:{keys.Length}"));
                for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
                {
                    Keyframe key = keys[keyIndex];
                    values.Add(FormattableString.Invariant(
                        $"root-orientation-warp-key:{i}:{keyIndex}:{key.time:R}:{key.value:R}:{key.inTangent:R}:{key.outTangent:R}:{key.inWeight:R}:{key.outWeight:R}:{(int)key.weightedMode}"));
                }
            }
            for (int i = 0; i < payloads.StateMachines.Length; i++)
            {
                CharacterPoseStateMachineDescriptor descriptor =
                    payloads.StateMachines[i];
                values.Add(
                    $"state-machine:{descriptor.Index}:{descriptor.NodeId}:{descriptor.StateMachineId}:" +
                    $"{descriptor.ContentRevision}:{descriptor.EntryStateIndex}:{descriptor.MaxTransitionsPerFrame}:" +
                    $"{descriptor.StateWorkspaceCount}:{descriptor.TransitionWorkspaceCount}:" +
                    $"{descriptor.RoutingPlanId}:{descriptor.RoutingDefinitionRevision}");
                for (int stateIndex = 0;
                     stateIndex < descriptor.States.Count;
                     stateIndex++)
                {
                    CharacterPoseStateDescriptor poseState =
                        descriptor.States[stateIndex];
                    values.Add(FormattableString.Invariant(
                        $"state:{i}:{poseState.Index}:{poseState.StateId}:{poseState.AlwaysResetOnEntry}"));
                }
                for (int transitionIndex = 0;
                     transitionIndex < descriptor.Transitions.Count;
                     transitionIndex++)
                {
                    CharacterPoseStateTransitionDescriptor transition =
                        descriptor.Transitions[transitionIndex];
                    values.Add(FormattableString.Invariant(
                        $"state-transition:{i}:{transition.Index}:{transition.TransitionId}:{transition.SourceStateIndex}:{transition.TargetStateIndex}:{transition.Priority}:{transition.Rule.GraphId}:{transition.Rule.ContentRevision}:{(int)transition.BlendLogic}:{transition.DurationSeconds:R}:{transition.CompletionDurationSeconds:R}:{(int)transition.BlendMode}:{transition.CurveIndex}:{transition.BlendProfileIndex}:{transition.RoutingRuleId}:{(int)transition.SourceSync.Mode}:{transition.SourceSync.RelationId}"));
                }
            }
            for (int i = 0; i < payloads.AnimationSlots.Length; i++)
            {
                CharacterAnimationSlotDescriptor descriptor =
                    payloads.AnimationSlots[i];
                values.Add(
                    $"animation-slot:{descriptor.Index}:{descriptor.NodeId}:{descriptor.SlotId}:{descriptor.AnimationChannelId}:" +
                    $"{descriptor.RoutingOwnerId}:{descriptor.RoutingPlanId}:{descriptor.RoutingDefinitionRevision}:" +
                    $"{descriptor.ActionPlayer.PlayerNodeId}:{descriptor.ActionPlayer.ActionPlaybackOperationIndex}:" +
                    $"{descriptor.ActionPlayer.PlayerIndex}:{descriptor.BlendStackWorkspace.BlendNodeIndex}:" +
                    $"{descriptor.BlendStackWorkspace.Capacity}:{descriptor.SourceUsage.SourcePoseValueIndex}");
                for (int routeIndex = 0;
                     routeIndex < descriptor.RequestRoutes.Count;
                     routeIndex++)
                {
                    CharacterAnimationSlotRequestRouteDescriptor route =
                        descriptor.RequestRoutes[routeIndex];
                    values.Add(FormattableString.Invariant(
                        $"animation-slot-route:{i}:{routeIndex}:{route.RuleId}:{route.SourceEndpointId}:{route.TargetEndpointId}:{(int)route.BlendLogic}:{route.DurationSeconds:R}:{route.CurveIndex}:{route.BlendProfileIndex}:{route.RequiresTargetFirstSample}:{route.RequiresCaptureCompletion}"));
                }
            }
            values.Add(
                $"inertial-count:{layout.InertializationCount}");
            for (int i = 0;
                 i < payloads.LinkedPoseFragments.Length;
                 i++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    payloads.LinkedPoseFragments[i];
                values.Add(
                    $"linked-fragment:{fragment.Index}:{fragment.GroupId}:{fragment.InterfaceId}:{fragment.InterfaceSignature}:{fragment.ImplementationId}:{fragment.ImplementationRevision}:{fragment.EntryId}:{fragment.GraphId}:{fragment.GraphRevision}:{fragment.OperationStart}:{fragment.OperationCount}:{fragment.PoseValueStart}:{fragment.PoseValueCount}:{fragment.GoalSetValueStart}:{fragment.GoalSetValueCount}:{fragment.PlayerStart}:{fragment.PlayerCount}:{fragment.StateMachineStart}:{fragment.StateMachineCount}:{fragment.InertializationStart}:{fragment.InertializationCount}:{fragment.RootOrientationWarpStart}:{fragment.RootOrientationWarpCount}:{fragment.MotionMatchingProviderStart}:{fragment.MotionMatchingProviderCount}:{fragment.StageStart}:{fragment.StageCount}:{string.Join(",", fragment.SourceIndices)}");
                for (int bindingIndex = 0;
                     bindingIndex < fragment.Inputs.Count;
                     bindingIndex++)
                {
                    CharacterLinkedPosePortValueBinding input =
                        fragment.Inputs[bindingIndex];
                    values.Add(
                        $"linked-fragment-input:{i}:{bindingIndex}:{input.PortId}:{(int)input.Kind}:{input.ValueIndex}");
                }
                for (int bindingIndex = 0;
                     bindingIndex < fragment.Outputs.Count;
                     bindingIndex++)
                {
                    CharacterLinkedPosePortValueBinding output =
                        fragment.Outputs[bindingIndex];
                    values.Add(
                        $"linked-fragment-output:{i}:{bindingIndex}:{output.PortId}:{(int)output.Kind}:{output.ValueIndex}");
                }
            }
            for (int i = 0; i < payloads.LinkedPoseCalls.Length; i++)
            {
                CharacterLinkedPoseCallPlanDescriptor call =
                    payloads.LinkedPoseCalls[i];
                values.Add(
                    $"linked-call:{call.Index}:{call.NodeId}:{call.GroupId}:{call.InterfaceId}:{call.InterfaceSignature}:{call.EntryId}:{(int)call.ExecutionDomain}:{string.Join(",", call.FragmentIndices)}");
            }
            for (int i = 0; i < binding.Operations.Length; i++)
            {
                CharacterPresentationPoseOperation operation =
                    binding.Operations[i];
                values.Add(FormattableString.Invariant(
                    $"operation:{operation.Index}:{(int)operation.ExecutionDomain}:{(int)operation.InputPoseSpace}:{(int)operation.OutputPoseSpace}:{(int)operation.Code}:{operation.NodeId}:{operation.AnimationChannelId}:{(int)operation.SelectionAvailability}:{operation.OutputValueIndex}:{operation.InputValueIndexA}:{operation.InputValueIndexB}:{operation.OutputFullBodyIkGoalContributionValueIndex}:{operation.OutputFullBodyIkGoalSetValueIndex}:{operation.InputFullBodyIkGoalSetValueIndex}:{operation.FullBodyIkGoalContributionInputStart}:{operation.FullBodyIkGoalContributionInputCount}:{operation.ControlInputOperationIndex}:{operation.ParameterIndex}:{operation.ParameterIndexB}:{operation.PlayerIndex}:{operation.BlendNodeIndex}:{operation.InertializationIndex}:{operation.BoneMaskIndex}:{operation.AdditiveReferenceIndex}:{operation.ModifyBoneIndex}:{operation.RootOrientationWarpIndex}:{operation.PoseBoneIkGoalsIndex}:{operation.FootPlacementIndex}:{operation.FullBodyIkIndex}:{operation.ClipPlayerIndex}:{operation.StateMachineIndex}:{operation.AnimationSlotIndex}:{operation.LinkedPoseCallIndex}:{operation.LinkedPoseFragmentIndex}:{operation.Weight:R}"));
            }
            for (int i = 0; i < stages.Count; i++)
            {
                CharacterPresentationPoseStage stage = stages[i];
                values.Add(
                    $"stage:{stage.Index}:{(int)stage.ExecutionDomain}:{(int)stage.InputPoseSpace}:{(int)stage.OutputPoseSpace}:{stage.OperationStart}:{stage.OperationCount}:{stage.NativeOperationStart}:{stage.NativeOperationCount}:{stage.PoseWorkspaceStart}:{stage.PoseWorkspaceCount}:{stage.CompletionIndex}:{stage.DiagnosticIndex}");
            }
            values.Add(FormattableString.Invariant(
                $"workspace:{poseWorkspace}:{layout.FullBodyIkGoalContributionValueCount}:{layout.FullBodyIkGoalSetValueCount}:{layout.FullBodyIkGoalContributionGoalWorkspaceCount}:{parameterWorkspace}:{contributionWorkspace}:{frameCache}:{layout.OutputOperationIndex}"));
            return StableHash.Compute(values.ToArray()).ToString();
        }
    }
}
