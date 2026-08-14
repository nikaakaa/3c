using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootPlacementRuntime : IDisposable
    {
        readonly CharacterFootGroundingGoalSource m_FootPlacement;

        internal CharacterFootPlacementRuntime(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene,
            ICharacterFutureBodyTrajectorySource futureBodyTrajectorySource)
        {
            m_FootPlacement = new CharacterFootGroundingGoalSource(
                actorId,
                settings,
                rig,
                physicsScene,
                futureBodyTrajectorySource);
        }

        internal CharacterFootGroundingDiagnostics GroundingDiagnostics =>
            m_FootPlacement.Diagnostics;

        internal CharacterPredictiveFootPlacementDiagnostics PredictionDiagnostics =>
            m_FootPlacement.PredictionDiagnostics;

        internal CharacterFullBodyIkGoalSetHeader ProduceGrounding(
            in CharacterFootPlacementPlanningFrame frame,
            NativeSlice<CharacterFullBodyIkGoal> output,
            int goalWorkspaceOffset,
            int producerOperationIndex,
            int producerCallSiteIndex,
            int weightParameterIndex) =>
            m_FootPlacement.Produce(
                frame,
                output,
                goalWorkspaceOffset,
                producerOperationIndex,
                producerCallSiteIndex,
                weightParameterIndex);

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState) =>
            m_FootPlacement.ApplyTuning(layout, block, resetOwnerState);

        internal void Reset(CharacterFootPlacementReset reset) =>
            m_FootPlacement.Reset(reset);

        internal void RetargetBodyBranch(ulong resetSequence) =>
            m_FootPlacement.RetargetBodyBranch(resetSequence);

        public void Dispose() => m_FootPlacement.Dispose();
    }
}
