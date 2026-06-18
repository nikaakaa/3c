using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonCharacterGraph
{
    public readonly struct CharacterGraphInput
    {
        public CharacterGraphInput(
            int sourceStep,
            float tickInterval,
            CharacterRuntimeBlackboardSnapshot blackboardSnapshot)
        {
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            TickInterval = tickInterval < 0f ? 0f : tickInterval;
            BlackboardSnapshot = blackboardSnapshot;
        }

        public int SourceStep { get; }
        public float TickInterval { get; }
        public CharacterRuntimeBlackboardSnapshot BlackboardSnapshot { get; }

        public static CharacterGraphInput Empty =>
            new CharacterGraphInput(0, 0f, CharacterRuntimeBlackboardSnapshot.Empty);
    }

    public readonly struct CharacterGraphNodeState
    {
        public CharacterGraphNodeState(
            CharacterExecutionNodeId nodeId,
            int lastEvaluatedStep)
        {
            NodeId = nodeId;
            LastEvaluatedStep = lastEvaluatedStep < 0 ? 0 : lastEvaluatedStep;
        }

        public CharacterExecutionNodeId NodeId { get; }
        public int LastEvaluatedStep { get; }
        public bool HasState => NodeId.IsValid;
    }

    public sealed class CharacterGraphState
    {
        readonly CharacterGraphNodeState[] nodeStates;

        public CharacterGraphState(CharacterGraphNodeState[] nodeStates)
        {
            this.nodeStates = nodeStates ?? Array.Empty<CharacterGraphNodeState>();
        }

        public IReadOnlyList<CharacterGraphNodeState> NodeStates => nodeStates ?? Array.Empty<CharacterGraphNodeState>();

        public static CharacterGraphState Empty =>
            new CharacterGraphState(Array.Empty<CharacterGraphNodeState>());
    }

    public readonly struct CueOutcome
    {
        readonly string[] cueIds;

        public CueOutcome(string[] cueIds, int sourceStep)
        {
            this.cueIds = cueIds ?? Array.Empty<string>();
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public IReadOnlyList<string> CueIds => cueIds ?? Array.Empty<string>();
        public int SourceStep { get; }
        public bool HasCue => CueIds.Count > 0;

        public static CueOutcome None(int sourceStep = 0)
        {
            return new CueOutcome(Array.Empty<string>(), sourceStep);
        }
    }

    public readonly struct CharacterGraphFrameResult
    {
        readonly string[] diagnostics;

        public CharacterGraphFrameResult(
            CharacterFrameCandidateOutput locomotionCandidate,
            CommittedActionBranchOutcome actionOutcome,
            CharacterFrameCandidateOutput upperBodyCandidate,
            CueOutcome cueOutcome,
            BodyOccupancyClaim occupancyClaim,
            string[] diagnostics,
            int sourceStep)
        {
            LocomotionCandidate = locomotionCandidate;
            ActionOutcome = actionOutcome;
            UpperBodyCandidate = upperBodyCandidate;
            CueOutcome = cueOutcome;
            OccupancyClaim = occupancyClaim;
            this.diagnostics = diagnostics ?? Array.Empty<string>();
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterFrameCandidateOutput LocomotionCandidate { get; }
        public CommittedActionBranchOutcome ActionOutcome { get; }
        public CharacterFrameCandidateOutput UpperBodyCandidate { get; }
        public CueOutcome CueOutcome { get; }
        public BodyOccupancyClaim OccupancyClaim { get; }
        public IReadOnlyList<string> Diagnostics => diagnostics ?? Array.Empty<string>();
        public int SourceStep { get; }
        public bool HasOutput =>
            LocomotionCandidate.HasAnyCandidate ||
            ActionOutcome.HasOutcome ||
            UpperBodyCandidate.HasAnyCandidate ||
            CueOutcome.HasCue ||
            OccupancyClaim.HasClaim;

        public CharacterFrameArbitrationInput ToArbitrationInput()
        {
            return new CharacterFrameArbitrationInput(
                OccupancyClaim,
                LocomotionCandidate,
                ActionOutcome.Candidate,
                UpperBodyCandidate,
                SourceStep);
        }

        public static CharacterGraphFrameResult Empty(int sourceStep = 0)
        {
            return new CharacterGraphFrameResult(
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.Locomotion, sourceStep),
                CommittedActionBranchOutcome.None(sourceStep),
                CharacterFrameCandidateOutput.None(CharacterBodyDomain.UpperBody, sourceStep),
                CueOutcome.None(sourceStep),
                BodyOccupancyClaim.None(sourceStep),
                Array.Empty<string>(),
                sourceStep);
        }
    }
}
