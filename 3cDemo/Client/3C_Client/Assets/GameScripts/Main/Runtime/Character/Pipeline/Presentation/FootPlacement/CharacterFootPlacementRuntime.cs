using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootPlacementRuntime : IDisposable
    {
        readonly CharacterFootPlacementFrameEvaluator m_Evaluator;

        internal CharacterFootPlacementRuntime(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene,
            ICharacterFutureBodyTrajectorySource futureBodyTrajectorySource)
        {
            m_Evaluator = new CharacterFootPlacementFrameEvaluator(
                actorId,
                settings,
                rig,
                physicsScene,
                futureBodyTrajectorySource);
        }

        internal CharacterFootGroundingDiagnostics GroundingDiagnostics =>
            m_Evaluator.Diagnostics;

        internal CharacterPredictiveFootPlacementDiagnostics PredictionDiagnostics =>
            m_Evaluator.PredictionDiagnostics;

        internal bool HasPendingFrame => m_Evaluator.HasPendingFrame;

        internal CharacterFullBodyIkGoalSetHeader EvaluateFrame(
            in CharacterFootPlacementFrameInput frame,
            NativeSlice<CharacterFullBodyIkGoal> output,
            int goalWorkspaceOffset,
            int producerOperationIndex,
            int producerCallSiteIndex,
            int weightParameterIndex)
        {
            var owner = new CharacterFullBodyIkGoalSetHeader(
                frame.RenderFrame,
                frame.CompletionIdentity,
                m_Evaluator.RigId,
                m_Evaluator.RigRevision,
                producerOperationIndex,
                producerCallSiteIndex,
                goalWorkspaceOffset,
                3,
                CharacterFullBodyIkGoalSetAvailability.Ready);
            CharacterFootPlacementFrameResult result = m_Evaluator.EvaluateFrame(
                in frame,
                in owner,
                weightParameterIndex);
            result.WriteGoals(output);
            return result.GoalSet;
        }

        internal void SealFrame(ulong renderFrame, ulong completionIdentity) =>
            m_Evaluator.SealFrame(renderFrame, completionIdentity);

        internal void DiscardPendingFrame() =>
            m_Evaluator.DiscardPendingFrame();

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState) =>
            m_Evaluator.ApplyTuning(layout, block, resetOwnerState);

        internal void Reset(CharacterFootPlacementReset reset) =>
            m_Evaluator.Reset(reset);

        internal void RetargetBodyBranch(ulong resetSequence) =>
            m_Evaluator.RetargetBodyBranch(resetSequence);

        public void Dispose() => m_Evaluator.Dispose();
    }
}
