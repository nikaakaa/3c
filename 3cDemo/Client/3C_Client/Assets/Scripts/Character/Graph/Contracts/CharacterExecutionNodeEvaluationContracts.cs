using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterGraph
{
    public interface ICharacterExecutionNodeEvaluator
    {
        CharacterExecutionNodeEvaluationResult Evaluate(in CharacterExecutionNodeEvaluationInput input);
    }

    public readonly struct CharacterExecutionNodeEvaluationInput
    {
        public CharacterExecutionNodeEvaluationInput(
            CharacterExecutionNodeDefinition node,
            CharacterGraphInput graphInput,
            CharacterGraphState graphState)
        {
            Node = node;
            GraphInput = graphInput;
            GraphState = graphState ?? CharacterGraphState.Empty;
        }

        public CharacterExecutionNodeDefinition Node { get; }
        public CharacterGraphInput GraphInput { get; }
        public CharacterGraphState GraphState { get; }
        public int SourceStep => GraphInput.SourceStep;
    }

    public readonly struct CharacterExecutionNodeStateWrite
    {
        public CharacterExecutionNodeStateWrite(
            CharacterExecutionNodeId ownerNodeId,
            CharacterGraphNodeState state)
        {
            OwnerNodeId = ownerNodeId;
            State = state;
        }

        public CharacterExecutionNodeId OwnerNodeId { get; }
        public CharacterGraphNodeState State { get; }
        public bool IsOwnedByNode => OwnerNodeId.IsValid && State.NodeId == OwnerNodeId;
    }

    public readonly struct CharacterExecutionNodeEvaluationResult
    {
        readonly CharacterExecutionNodeStateWrite[] stateWrites;
        readonly string[] diagnostics;

        public CharacterExecutionNodeEvaluationResult(
            CharacterGraphFrameResult frameResult,
            CharacterExecutionNodeStateWrite[] stateWrites,
            string[] diagnostics,
            int sourceStep)
        {
            FrameResult = frameResult;
            this.stateWrites = stateWrites ?? Array.Empty<CharacterExecutionNodeStateWrite>();
            this.diagnostics = diagnostics ?? Array.Empty<string>();
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterGraphFrameResult FrameResult { get; }
        public IReadOnlyList<CharacterExecutionNodeStateWrite> StateWrites => stateWrites ?? Array.Empty<CharacterExecutionNodeStateWrite>();
        public IReadOnlyList<string> Diagnostics => diagnostics ?? Array.Empty<string>();
        public int SourceStep { get; }
        public bool HasOutput => FrameResult.HasOutput;
        public bool HasForeignStateWrites
        {
            get
            {
                for (int i = 0; i < StateWrites.Count; i++)
                {
                    if (!StateWrites[i].IsOwnedByNode)
                        return true;
                }

                return false;
            }
        }

        public static CharacterExecutionNodeEvaluationResult Empty(int sourceStep = 0)
        {
            return new CharacterExecutionNodeEvaluationResult(
                CharacterGraphFrameResult.Empty(sourceStep),
                Array.Empty<CharacterExecutionNodeStateWrite>(),
                Array.Empty<string>(),
                sourceStep);
        }
    }
}
