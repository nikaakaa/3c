using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonCharacterBehavior
{
    public enum CharacterBehaviorEvaluationPass
    {
        None = 0,
        RequestPass = 1,
        OutputPass = 2
    }

    public enum CharacterBehaviorSubmissionKind
    {
        None = 0,
        Request = 1,
        Output = 2,
        Cue = 3,
        Diagnostic = 4,
        StateWrite = 5,
        MotionChannel = 6,
        AnimationChannel = 7,
        WindowFactsChannel = 8,
        Claim = 9
    }

    public enum CharacterBehaviorSourceKind
    {
        None = 0,
        Root = 1,
        Parallel = 2,
        Locomotion = 3,
        CommittedAction = 4,
        UpperBody = 5,
        Cue = 6,
        TestFake = 100
    }

    [Flags]
    public enum CharacterBehaviorSubmissionConsumer
    {
        None = 0,
        RequestArbiter = 1 << 0,
        ActionRequestContext = 1 << 1,
        BehaviorSubmissionComposer = 1 << 2,
        FramePlanInput = 1 << 3,
        CueQueue = 1 << 4,
        Diagnostics = 1 << 5,
        StateOwner = 1 << 6,
        FrameContextWriter = 1 << 7
    }

    public enum CharacterBehaviorStateOwner
    {
        None = 0,
        BehaviorRuntime = 1,
        LocomotionRuntime = 2,
        ActionLifecycleRuntime = 3,
        AnimationPresenter = 4,
        CharacterRuntimeBlackboard = 5,
        RuntimeCaptureRestore = 6,
        EditorOnlyAsset = 7,
        FrameContext = 8
    }

    public readonly struct CharacterBehaviorSourceId : IEquatable<CharacterBehaviorSourceId>, IComparable<CharacterBehaviorSourceId>
    {
        readonly string value;

        public CharacterBehaviorSourceId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public int CompareTo(CharacterBehaviorSourceId other)
        {
            return StringComparer.Ordinal.Compare(Value, other.Value);
        }

        public bool Equals(CharacterBehaviorSourceId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterBehaviorSourceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(CharacterBehaviorSourceId left, CharacterBehaviorSourceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterBehaviorSourceId left, CharacterBehaviorSourceId right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct CharacterBehaviorSubmissionSource : IComparable<CharacterBehaviorSubmissionSource>
    {
        public CharacterBehaviorSubmissionSource(
            CharacterBehaviorSourceId nodeId,
            CharacterBehaviorSourceKind sourceKind,
            CharacterBehaviorEvaluationPass pass,
            int sourceStep,
            int sourceOrder)
        {
            NodeId = nodeId;
            SourceKind = nodeId.IsValid ? sourceKind : CharacterBehaviorSourceKind.None;
            Pass = pass;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            SourceOrder = sourceOrder < 0 ? 0 : sourceOrder;
        }

        public CharacterBehaviorSourceId NodeId { get; }
        public CharacterBehaviorSourceKind SourceKind { get; }
        public CharacterBehaviorEvaluationPass Pass { get; }
        public int SourceStep { get; }
        public int SourceOrder { get; }
        public bool IsValid => NodeId.IsValid && SourceKind != CharacterBehaviorSourceKind.None;

        public int CompareTo(CharacterBehaviorSubmissionSource other)
        {
            int step = SourceStep.CompareTo(other.SourceStep);
            if (step != 0)
                return step;

            int order = SourceOrder.CompareTo(other.SourceOrder);
            return order != 0 ? order : NodeId.CompareTo(other.NodeId);
        }

        public static CharacterBehaviorSubmissionSource Create(
            string nodeId,
            CharacterBehaviorSourceKind sourceKind,
            CharacterBehaviorEvaluationPass pass,
            int sourceStep,
            int sourceOrder)
        {
            return new CharacterBehaviorSubmissionSource(
                new CharacterBehaviorSourceId(nodeId),
                sourceKind,
                pass,
                sourceStep,
                sourceOrder);
        }
    }

    public readonly struct BehaviorRequestSubmission
    {
        public BehaviorRequestSubmission(
            CharacterBehaviorSubmissionSource source,
            CharacterFrameRequestSubmission frameRequest,
            ActionInterruptDecision decision,
            CharacterResolvedAction resolvedAction,
            string diagnostic)
        {
            Source = source;
            FrameRequest = frameRequest;
            Decision = decision;
            ResolvedAction = resolvedAction;
            Diagnostic = diagnostic ?? string.Empty;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public CharacterFrameRequestSubmission FrameRequest { get; }
        public ActionInterruptDecision Decision { get; }
        public CharacterResolvedAction ResolvedAction { get; }
        public string Diagnostic { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasRequest => FrameRequest.HasRequest || ResolvedAction.HasResolvedAction || !string.IsNullOrWhiteSpace(Diagnostic);

        public static BehaviorRequestSubmission None(CharacterBehaviorSubmissionSource source)
        {
            return new BehaviorRequestSubmission(
                source,
                default,
                ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest),
                default,
                string.Empty);
        }
    }

    public readonly struct BehaviorOutputSubmission
    {
        public BehaviorOutputSubmission(
            CharacterBehaviorSubmissionSource source,
            LocomotionDecisionFrame locomotionDecision,
            LocomotionStateDecisionFrame stateDecision,
            BasicLocomotionFrame locomotionFrame,
            CharacterStateMachineFrame stateFrame,
            ActionMotionResolveResult actionMotionResult,
            CharacterFrameActionOutputSubmission actionOutput,
            CharacterFrameArbitrationInput arbitrationInput,
            LocomotionPreemptionFact locomotionPreemption,
            bool required)
        {
            Source = source;
            LocomotionDecision = locomotionDecision;
            StateDecision = stateDecision;
            LocomotionFrame = locomotionFrame;
            StateFrame = stateFrame;
            ActionMotionResult = actionMotionResult;
            ActionOutput = actionOutput;
            ArbitrationInput = arbitrationInput;
            LocomotionPreemption = locomotionPreemption;
            Required = required;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public LocomotionDecisionFrame LocomotionDecision { get; }
        public LocomotionStateDecisionFrame StateDecision { get; }
        public BasicLocomotionFrame LocomotionFrame { get; }
        public CharacterStateMachineFrame StateFrame { get; }
        public ActionMotionResolveResult ActionMotionResult { get; }
        public CharacterFrameActionOutputSubmission ActionOutput { get; }
        public CharacterFrameArbitrationInput ArbitrationInput { get; }
        public LocomotionPreemptionFact LocomotionPreemption { get; }
        public bool Required { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasOutput =>
            StateDecision.HasStateFrame ||
            StateFrame.Snapshot.ActiveState.IsValid ||
            ActionMotionResult.HasSpec ||
            ActionOutput.HasAnimationRequest ||
            ActionOutput.HasCommittedActionBranchOutcome ||
            ArbitrationInput.HasInput ||
            LocomotionPreemption.HasPreemption;

        public static BehaviorOutputSubmission None(CharacterBehaviorSubmissionSource source)
        {
            return new BehaviorOutputSubmission(
                source,
                default,
                default,
                default,
                default,
                ActionMotionResolveResult.None(source.SourceStep),
                CharacterFrameActionOutputSubmission.None(source.SourceStep),
                CharacterFrameArbitrationInput.None(source.SourceStep),
                LocomotionPreemptionFact.None,
                false);
        }
    }

    public readonly struct BehaviorCueSubmission
    {
        public BehaviorCueSubmission(
            CharacterBehaviorSubmissionSource source,
            string cueId,
            int sourceTick)
        {
            Source = source;
            CueId = (cueId ?? string.Empty).Trim();
            SourceTick = sourceTick < 0 ? 0 : sourceTick;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public string CueId { get; }
        public int SourceTick { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasCue => !string.IsNullOrWhiteSpace(CueId);
    }

    public readonly struct BehaviorMotionChannelSubmission
    {
        public BehaviorMotionChannelSubmission(
            CharacterBehaviorSubmissionSource source,
            ActionMotionSpec motionSpec)
        {
            Source = source;
            MotionSpec = motionSpec;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public ActionMotionSpec MotionSpec { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasMotion => MotionSpec.HasSpec;
    }

    public readonly struct BehaviorAnimationChannelSubmission
    {
        public BehaviorAnimationChannelSubmission(
            CharacterBehaviorSubmissionSource source,
            ActionAnimationKey animationKey)
        {
            Source = source;
            AnimationKey = animationKey;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public ActionAnimationKey AnimationKey { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasAnimation => AnimationKey.IsValid;
    }

    public readonly struct BehaviorWindowFactsChannelSubmission
    {
        readonly string[] factIds;

        public BehaviorWindowFactsChannelSubmission(
            CharacterBehaviorSubmissionSource source,
            string[] factIds)
        {
            Source = source;
            this.factIds = factIds ?? Array.Empty<string>();
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public IReadOnlyList<string> FactIds => factIds ?? Array.Empty<string>();
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasFacts => FactIds.Count > 0;
    }

    public readonly struct BehaviorClaimSubmission
    {
        public BehaviorClaimSubmission(
            CharacterBehaviorSubmissionSource source,
            BodyOccupancyClaim claim)
        {
            Source = source;
            Claim = claim;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public BodyOccupancyClaim Claim { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasClaim => Claim.HasClaim;
    }

    public readonly struct BehaviorDiagnosticSubmission
    {
        public BehaviorDiagnosticSubmission(
            CharacterBehaviorSubmissionSource source,
            string code,
            string message,
            bool error)
        {
            Source = source;
            Code = (code ?? string.Empty).Trim();
            Message = message ?? string.Empty;
            Error = error;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public string Code { get; }
        public string Message { get; }
        public bool Error { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasDiagnostic => !string.IsNullOrWhiteSpace(Code) || !string.IsNullOrWhiteSpace(Message);
    }

    public readonly struct BehaviorStateWriteSubmission
    {
        public BehaviorStateWriteSubmission(
            CharacterBehaviorSubmissionSource source,
            CharacterBehaviorStateOwner owner,
            CharacterBehaviorSourceId ownerNodeId,
            string stateKey,
            string value)
        {
            Source = source;
            Owner = owner;
            OwnerNodeId = ownerNodeId;
            StateKey = (stateKey ?? string.Empty).Trim();
            Value = value ?? string.Empty;
        }

        public CharacterBehaviorSubmissionSource Source { get; }
        public CharacterBehaviorStateOwner Owner { get; }
        public CharacterBehaviorSourceId OwnerNodeId { get; }
        public string StateKey { get; }
        public string Value { get; }
        public CharacterBehaviorEvaluationPass Pass => Source.Pass;
        public bool HasWrite => Owner != CharacterBehaviorStateOwner.None && !string.IsNullOrWhiteSpace(StateKey);
        public bool IsOwnedBySourceNode =>
            Owner != CharacterBehaviorStateOwner.BehaviorRuntime ||
            !OwnerNodeId.IsValid ||
            OwnerNodeId == Source.NodeId;
    }

    public readonly struct CharacterBehaviorStateOwnershipRule
    {
        public CharacterBehaviorStateOwnershipRule(
            string stateKind,
            CharacterBehaviorStateOwner owner,
            CharacterBehaviorSubmissionConsumer communicationBoundary)
        {
            StateKind = (stateKind ?? string.Empty).Trim();
            Owner = owner;
            CommunicationBoundary = communicationBoundary;
        }

        public string StateKind { get; }
        public CharacterBehaviorStateOwner Owner { get; }
        public CharacterBehaviorSubmissionConsumer CommunicationBoundary { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(StateKind) && Owner != CharacterBehaviorStateOwner.None;
    }

    public static class CharacterBehaviorStateOwnership
    {
        static readonly CharacterBehaviorStateOwnershipRule[] rules =
        {
            new CharacterBehaviorStateOwnershipRule("Behavior node private state", CharacterBehaviorStateOwner.BehaviorRuntime, CharacterBehaviorSubmissionConsumer.StateOwner),
            new CharacterBehaviorStateOwnershipRule("Locomotion runtime state", CharacterBehaviorStateOwner.LocomotionRuntime, CharacterBehaviorSubmissionConsumer.FrameContextWriter),
            new CharacterBehaviorStateOwnershipRule("Action active action and state time", CharacterBehaviorStateOwner.ActionLifecycleRuntime, CharacterBehaviorSubmissionConsumer.FrameContextWriter),
            new CharacterBehaviorStateOwnershipRule("Animation playback state", CharacterBehaviorStateOwner.AnimationPresenter, CharacterBehaviorSubmissionConsumer.FramePlanInput),
            new CharacterBehaviorStateOwnershipRule("Confirmed blackboard facts", CharacterBehaviorStateOwner.CharacterRuntimeBlackboard, CharacterBehaviorSubmissionConsumer.FramePlanInput),
            new CharacterBehaviorStateOwnershipRule("Rollback restore state", CharacterBehaviorStateOwner.RuntimeCaptureRestore, CharacterBehaviorSubmissionConsumer.FrameContextWriter),
            new CharacterBehaviorStateOwnershipRule("Editor graph state", CharacterBehaviorStateOwner.EditorOnlyAsset, CharacterBehaviorSubmissionConsumer.None)
        };

        public static IReadOnlyList<CharacterBehaviorStateOwnershipRule> Rules => rules;

        public static bool TryGetOwner(string stateKind, out CharacterBehaviorStateOwner owner)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                if (string.Equals(rules[i].StateKind, stateKind, StringComparison.Ordinal))
                {
                    owner = rules[i].Owner;
                    return true;
                }
            }

            owner = CharacterBehaviorStateOwner.None;
            return false;
        }
    }
}
