using System;
using System.Collections.Generic;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonAction
{
    public readonly struct CommittedActionRequestFact
    {
        public CommittedActionRequestFact(
            InputRequestKind requestKind,
            bool held,
            bool released,
            int sourceTick)
        {
            RequestKind = requestKind;
            Held = held;
            Released = released;
            SourceTick = sourceTick < 0 ? 0 : sourceTick;
        }

        public InputRequestKind RequestKind { get; }
        public bool Held { get; }
        public bool Released { get; }
        public int SourceTick { get; }
        public bool IsDefined => Held || Released;

        public static CommittedActionRequestFact HeldFact(InputRequestKind requestKind, int sourceTick)
        {
            return new CommittedActionRequestFact(requestKind, true, false, sourceTick);
        }

        public static CommittedActionRequestFact ReleasedFact(InputRequestKind requestKind, int sourceTick)
        {
            return new CommittedActionRequestFact(requestKind, false, true, sourceTick);
        }
    }

    public readonly struct CommittedActionRequestFactSet
    {
        readonly CommittedActionRequestFact[] facts;

        public CommittedActionRequestFactSet(CommittedActionRequestFact[] facts)
        {
            this.facts = facts ?? Array.Empty<CommittedActionRequestFact>();
        }

        public IReadOnlyList<CommittedActionRequestFact> Facts => facts ?? Array.Empty<CommittedActionRequestFact>();

        public bool IsHeld(InputRequestKind requestKind, int sourceTick)
        {
            for (int i = 0; i < Facts.Count; i++)
            {
                CommittedActionRequestFact fact = Facts[i];
                if (fact.RequestKind == requestKind && fact.Released && fact.SourceTick == sourceTick)
                    return false;
            }

            for (int i = 0; i < Facts.Count; i++)
            {
                CommittedActionRequestFact fact = Facts[i];
                if (fact.RequestKind == requestKind && fact.Held)
                    return true;
            }

            return false;
        }

        public bool IsReleased(InputRequestKind requestKind, int sourceTick)
        {
            for (int i = 0; i < Facts.Count; i++)
            {
                CommittedActionRequestFact fact = Facts[i];
                if (fact.RequestKind == requestKind && fact.Released && fact.SourceTick == sourceTick)
                    return true;
            }

            return false;
        }

        public static CommittedActionRequestFactSet Empty =>
            new CommittedActionRequestFactSet(Array.Empty<CommittedActionRequestFact>());

        public static CommittedActionRequestFactSet FromRequestFact(
            CharacterInputRequestFact requestFact,
            int sourceTick)
        {
            return requestFact.HasRequest
                ? new CommittedActionRequestFactSet(new[]
                {
                    CommittedActionRequestFact.HeldFact(requestFact.RequestKind, sourceTick)
                })
                : Empty;
        }
    }

    public readonly struct CommittedActionBranchEvaluationContext
    {
        public CommittedActionBranchEvaluationContext(
            int sourceStep,
            CharacterResolvedAction activeAction,
            CharacterInputRequestFact requestFact,
            MovementInputIntent movementIntent,
            LocomotionDecisionFacts locomotionFacts)
            : this(
                sourceStep,
                activeAction,
                requestFact,
                movementIntent,
                locomotionFacts,
                CharacterRuntimeBlackboardSnapshot.Empty)
        {
        }

        public CommittedActionBranchEvaluationContext(
            int sourceStep,
            CharacterResolvedAction activeAction,
            CharacterInputRequestFact requestFact,
            MovementInputIntent movementIntent,
            LocomotionDecisionFacts locomotionFacts,
            CharacterRuntimeBlackboardSnapshot blackboardSnapshot)
            : this(
                sourceStep,
                activeAction,
                requestFact,
                movementIntent,
                locomotionFacts,
                blackboardSnapshot,
                default,
                0,
                0,
                CommittedActionRequestFactSet.FromRequestFact(requestFact, sourceStep),
                ActionFactSet.Empty,
                StateTimelineWindowFacts.None(default),
                1)
        {
        }

        public CommittedActionBranchEvaluationContext(
            int sourceStep,
            CharacterResolvedAction activeAction,
            CharacterInputRequestFact requestFact,
            MovementInputIntent movementIntent,
            LocomotionDecisionFacts locomotionFacts,
            CharacterRuntimeBlackboardSnapshot blackboardSnapshot,
            CommittedActionNodeId activeTimelineNodeId,
            int actionLocalTick,
            int runtimeTimelineDurationTicks,
            CommittedActionRequestFactSet requestFacts,
            ActionFactSet activeFacts,
            StateTimelineWindowFacts timelineFacts,
            int factResolverVersion)
        {
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            ActiveAction = activeAction;
            RequestFact = requestFact;
            MovementIntent = movementIntent;
            LocomotionFacts = locomotionFacts;
            BlackboardSnapshot = blackboardSnapshot;
            ActiveTimelineNodeId = activeTimelineNodeId;
            ActionLocalTick = actionLocalTick < 0 ? 0 : actionLocalTick;
            RuntimeTimelineDurationTicks = runtimeTimelineDurationTicks < 0 ? 0 : runtimeTimelineDurationTicks;
            RequestFacts = requestFacts;
            ActiveFacts = activeFacts;
            TimelineFacts = timelineFacts;
            FactResolverVersion = factResolverVersion <= 0 ? 1 : factResolverVersion;
        }

        public int SourceStep { get; }
        public CharacterResolvedAction ActiveAction { get; }
        public CharacterInputRequestFact RequestFact { get; }
        public MovementInputIntent MovementIntent { get; }
        public LocomotionDecisionFacts LocomotionFacts { get; }
        public CharacterRuntimeBlackboardSnapshot BlackboardSnapshot { get; }
        public CommittedActionNodeId ActiveTimelineNodeId { get; }
        public int ActionLocalTick { get; }
        public int RuntimeTimelineDurationTicks { get; }
        public CommittedActionRequestFactSet RequestFacts { get; }
        public ActionFactSet ActiveFacts { get; }
        public StateTimelineWindowFacts TimelineFacts { get; }
        public int FactResolverVersion { get; }
        public ActionStateId AcceptedActionId => ActiveAction.MotionSpec.HasSpec ? ActiveAction.MotionSpec.ActionState : ActionStateIds.None;
        public bool HasActiveTimeline => ActiveTimelineNodeId.IsValid;
        public CharacterStateVariant ActionVariant => ActiveAction.MotionSpec.Variant != CharacterStateVariant.None
            ? ActiveAction.MotionSpec.Variant
            : RequestFact.Variant;
        public bool HasMoveIntent => MovementIntent.HasMoveIntent || LocomotionFacts.HasMoveIntent || RequestFact.HasWorldDirection;

        public bool HasActiveFact(TimelineFactId factId)
        {
            return ActiveFacts.Contains(factId) ||
                   TimelineFacts.Contains(factId) ||
                   TimelineFacts.ContainsRequestFact(factId);
        }

        public CommittedActionBranchEvaluationContext WithActiveTimeline(
            CommittedActionNodeDefinition timelineNode,
            int localTick)
        {
            return new CommittedActionBranchEvaluationContext(
                SourceStep,
                ActiveAction,
                RequestFact,
                MovementIntent,
                LocomotionFacts,
                BlackboardSnapshot,
                timelineNode.NodeId,
                localTick,
                timelineNode.TimelineNode.Timeline != null ? timelineNode.TimelineNode.Timeline.DurationTicks : 0,
                RequestFacts,
                ActiveFacts,
                TimelineFacts,
                FactResolverVersion);
        }

        public static CommittedActionBranchEvaluationContext FromActiveAction(
            in CharacterResolvedAction activeAction,
            int sourceStep)
        {
            CharacterInputRequestFact request = activeAction.RequestFact;
            return new CommittedActionBranchEvaluationContext(
                sourceStep,
                activeAction,
                request,
                default,
                default);
        }

        public static CommittedActionBranchEvaluationContext Empty(int sourceStep = 0)
        {
            return new CommittedActionBranchEvaluationContext(
                sourceStep,
                default,
                CharacterInputRequestFact.None(InputRequestKind.Dodge),
                default,
                default);
        }
    }

    public readonly struct CommittedActionBranchEvaluationInput
    {
        public CommittedActionBranchEvaluationInput(
            CommittedActionBranchDefinition branch,
            int localTick,
            int sourceStep)
            : this(branch, localTick, sourceStep, CommittedActionBranchEvaluationContext.Empty(sourceStep))
        {
        }

        public CommittedActionBranchEvaluationInput(
            CommittedActionBranchDefinition branch,
            int localTick,
            int sourceStep,
            CommittedActionBranchEvaluationContext context)
        {
            Branch = branch;
            LocalTick = localTick < 0 ? 0 : localTick;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            Context = context;
        }

        public CommittedActionBranchDefinition Branch { get; }
        public int LocalTick { get; }
        public int SourceStep { get; }
        public CommittedActionBranchEvaluationContext Context { get; }
    }

    public static class CommittedActionBranchEvaluator
    {
        public static CommittedActionBranchOutcome Evaluate(in CommittedActionBranchEvaluationInput input)
        {
            CommittedActionBranchDefinition branch = input.Branch;
            if (!branch.CanEvaluate)
                return CommittedActionBranchOutcome.None(input.SourceStep);

            if (input.Context.ActiveTimelineNodeId.IsValid &&
                branch.TryGetNode(input.Context.ActiveTimelineNodeId, out CommittedActionNodeDefinition activeNode) &&
                activeNode.Kind == CommittedActionNodeKind.Timeline)
            {
                CommittedActionBranchEvaluationInput activeInput = new CommittedActionBranchEvaluationInput(
                    branch,
                    input.LocalTick,
                    input.SourceStep,
                    input.Context.WithActiveTimeline(activeNode, input.LocalTick));
                CommittedActionBranchOutcome transitioned = EvaluateTimelineChildren(in branch, in activeNode, in activeInput);
                if (transitioned.HasOutcome || transitioned.HasDiagnostic)
                    return transitioned;
                return EvaluateTimeline(in branch, in activeNode, in activeInput);
            }

            return EvaluateNode(in branch, branch.RootNode, in input);
        }

        static CommittedActionBranchOutcome EvaluateNode(
            in CommittedActionBranchDefinition branch,
            in CommittedActionNodeDefinition node,
            in CommittedActionBranchEvaluationInput input)
        {
            switch (node.Kind)
            {
                case CommittedActionNodeKind.Root:
                    return EvaluateRoot(in branch, in node, in input);
                case CommittedActionNodeKind.Timeline:
                    return EvaluateTimeline(in branch, in node, in input);
                case CommittedActionNodeKind.Condition:
                    return EvaluateConditionNode(in branch, in node, in input);
                case CommittedActionNodeKind.Selector:
                    return EvaluateSelector(in branch, in node, in input);
                default:
                    return CommittedActionBranchOutcome.DiagnosticOnly(
                        input.SourceStep,
                        $"committed-action-node-unsupported:{node.Kind}");
            }
        }

        static CommittedActionBranchOutcome EvaluateRoot(
            in CommittedActionBranchDefinition branch,
            in CommittedActionNodeDefinition node,
            in CommittedActionBranchEvaluationInput input)
        {
            if (node.ChildIds.Count != 1)
                return CommittedActionBranchOutcome.DiagnosticOnly(
                    input.SourceStep,
                    $"committed-action-root-child-invalid:{node.NodeId.Value}");

            CommittedActionNodeId childId = node.ChildIds[0];
            if (!branch.TryGetNode(childId, out CommittedActionNodeDefinition child))
                return CommittedActionBranchOutcome.DiagnosticOnly(
                    input.SourceStep,
                    $"committed-action-child-missing:{childId.Value}");

            return EvaluateNode(in branch, in child, in input);
        }

        static CommittedActionBranchOutcome EvaluateTimeline(
            in CommittedActionBranchDefinition branch,
            in CommittedActionNodeDefinition node,
            in CommittedActionBranchEvaluationInput input)
        {
            ActionTimelineOutcome timelineOutcome = ActionTimelineEvaluator.Evaluate(
                new ActionTimelineEvaluationInput(
                    node.TimelineNode.Timeline,
                    input.LocalTick,
                    input.SourceStep));
            CharacterFrameCandidateOutput candidate = CharacterFrameCandidateOutput.CommittedAction(
                timelineOutcome.HasMotion,
                timelineOutcome.HasAnimation,
                input.SourceStep);
            BodyOccupancyClaim claim = timelineOutcome.HasOutcome
                ? branch.DefaultBodyClaim
                : BodyOccupancyClaim.None(input.SourceStep);

            return new CommittedActionBranchOutcome(
                timelineOutcome,
                candidate,
                claim,
                input.SourceStep,
                node.NodeId,
                string.Empty);
        }

        static CommittedActionBranchOutcome EvaluateTimelineChildren(
            in CommittedActionBranchDefinition branch,
            in CommittedActionNodeDefinition node,
            in CommittedActionBranchEvaluationInput input)
        {
            for (int i = 0; i < node.ChildIds.Count; i++)
            {
                CommittedActionNodeId childId = node.ChildIds[i];
                if (!branch.TryGetNode(childId, out CommittedActionNodeDefinition child))
                    return CommittedActionBranchOutcome.DiagnosticOnly(
                        input.SourceStep,
                        $"committed-action-child-missing:{childId.Value}");
                if (child.Kind != CommittedActionNodeKind.Condition)
                    return CommittedActionBranchOutcome.DiagnosticOnly(
                        input.SourceStep,
                        $"committed-action-timeline-child-not-condition:{childId.Value}");
                CommittedActionBranchEvaluationContext context = input.Context;
                if (!CommittedActionConditionEvaluator.Evaluate(child.Condition, in context))
                    continue;

                CommittedActionBranchEvaluationInput transitionInput = new CommittedActionBranchEvaluationInput(
                    branch,
                    0,
                    input.SourceStep,
                    input.Context);
                return EvaluateNode(in branch, in child, in transitionInput);
            }

            return CommittedActionBranchOutcome.None(input.SourceStep);
        }

        static CommittedActionBranchOutcome EvaluateConditionNode(
            in CommittedActionBranchDefinition branch,
            in CommittedActionNodeDefinition node,
            in CommittedActionBranchEvaluationInput input)
        {
            CommittedActionBranchEvaluationContext context = input.Context;
            if (!CommittedActionConditionEvaluator.Evaluate(node.Condition, in context))
                return CommittedActionBranchOutcome.DiagnosticOnly(
                    input.SourceStep,
                    $"committed-action-condition-failed:{node.NodeId.Value}");

            if (node.ChildIds.Count == 0)
                return CommittedActionBranchOutcome.DiagnosticOnly(
                    input.SourceStep,
                    $"committed-action-condition-child-missing:{node.NodeId.Value}");

            CommittedActionNodeId childId = node.ChildIds[0];
            if (!branch.TryGetNode(childId, out CommittedActionNodeDefinition child))
                return CommittedActionBranchOutcome.DiagnosticOnly(
                    input.SourceStep,
                    $"committed-action-child-missing:{childId.Value}");

            return EvaluateNode(in branch, in child, in input);
        }

        static CommittedActionBranchOutcome EvaluateSelector(
            in CommittedActionBranchDefinition branch,
            in CommittedActionNodeDefinition node,
            in CommittedActionBranchEvaluationInput input)
        {
            for (int i = 0; i < node.ChildIds.Count; i++)
            {
                CommittedActionNodeId childId = node.ChildIds[i];
                if (!branch.TryGetNode(childId, out CommittedActionNodeDefinition child))
                    continue;

                CommittedActionBranchEvaluationContext context = input.Context;
                if (child.Kind == CommittedActionNodeKind.Condition &&
                    !CommittedActionConditionEvaluator.Evaluate(child.Condition, in context))
                {
                    continue;
                }

                CommittedActionBranchOutcome outcome = EvaluateNode(in branch, in child, in input);
                return outcome;
            }

            return CommittedActionBranchOutcome.DiagnosticOnly(
                input.SourceStep,
                $"committed-action-selector-no-match:{node.NodeId.Value}");
        }
    }

    public static class CommittedActionConditionEvaluator
    {
        public static bool Evaluate(
            CommittedActionConditionDefinition condition,
            in CommittedActionBranchEvaluationContext context)
        {
            switch (condition.Kind)
            {
                case CommittedActionConditionKind.Always:
                    return true;
                case CommittedActionConditionKind.RequestHeld:
                    return context.RequestFacts.IsHeld(condition.RequestKind, context.SourceStep);
                case CommittedActionConditionKind.RequestReleased:
                    return context.RequestFacts.IsReleased(condition.RequestKind, context.SourceStep);
                case CommittedActionConditionKind.RequiredFactActive:
                    return context.HasActiveFact(condition.RequiredFactId);
                case CommittedActionConditionKind.TimelineComplete:
                    return context.HasActiveTimeline &&
                           context.ActionLocalTick >= context.RuntimeTimelineDurationTicks;
                case CommittedActionConditionKind.HasMoveIntent:
                    return context.HasMoveIntent == condition.ExpectedBool;
                case CommittedActionConditionKind.ActionVariantEquals:
                    return context.ActionVariant == condition.ExpectedVariant;
                default:
                    return false;
            }
        }
    }
}
