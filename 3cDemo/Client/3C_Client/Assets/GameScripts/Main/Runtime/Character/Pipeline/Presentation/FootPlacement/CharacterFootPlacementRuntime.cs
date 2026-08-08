using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootPlacementRuntime : IDisposable
    {
        readonly CharacterFootGroundingGoalSource m_Grounding;
        readonly CharacterPredictiveFootPlacementModifier m_PredictiveModifier;

        internal CharacterFootPlacementRuntime(
            ActorId actorId,
            CharacterFootPlacementRuntimeSettings settings,
            CharacterFootPlacementPoseRig rig,
            PhysicsScene physicsScene,
            bool enablePredictiveModifier)
        {
            m_Grounding = new CharacterFootGroundingGoalSource(
                actorId,
                settings,
                rig,
                physicsScene);
            if (enablePredictiveModifier)
            {
                m_PredictiveModifier = new CharacterPredictiveFootPlacementModifier(
                    actorId,
                    rig,
                    settings,
                    physicsScene);
            }
        }

        internal CharacterFootGroundingDiagnostics GroundingDiagnostics =>
            m_Grounding.Diagnostics;

        internal CharacterPredictiveFootPlacementModifierDiagnostics ModifierDiagnostics =>
            m_PredictiveModifier?.Diagnostics ?? default;

        internal CharacterFullBodyIkGoalSetHeader ProduceGrounding(
            in CharacterFootPlacementPlanningFrame frame,
            NativeSlice<CharacterFullBodyIkGoal> output,
            int goalWorkspaceOffset,
            int producerOperationIndex,
            int producerCallSiteIndex,
            int weightParameterIndex) =>
            m_Grounding.Produce(
                frame,
                output,
                goalWorkspaceOffset,
                producerOperationIndex,
                producerCallSiteIndex,
                weightParameterIndex);

        internal CharacterFullBodyIkGoalSetHeader ProduceModifier(
            in CharacterFootPlacementPlanningFrame frame,
            in CharacterFullBodyIkGoalSetHeader baselineHeader,
            NativeSlice<CharacterFullBodyIkGoal> baselineGoals,
            NativeSlice<CharacterFullBodyIkGoal> output,
            int goalWorkspaceOffset,
            int producerOperationIndex,
            int producerCallSiteIndex) =>
            (m_PredictiveModifier ?? throw new InvalidOperationException(
                "Predictive Foot Placement Modifier is not compiled for this Pose Plan.")).Modify(
                frame,
                baselineHeader,
                baselineGoals,
                m_Grounding.Diagnostics,
                output,
                goalWorkspaceOffset,
                producerOperationIndex,
                producerCallSiteIndex);

        internal string ApplyTuning(
            CharacterPoseTuningLayout layout,
            CharacterPoseTuningParameterBlock block,
            bool resetOwnerState)
        {
            string error = m_Grounding.ApplyTuning(layout, block, resetOwnerState);
            if (!string.IsNullOrEmpty(error))
                return error;
            m_PredictiveModifier?.ApplyTuning(m_Grounding.Settings.PredictiveExtension);
            if (resetOwnerState)
                m_PredictiveModifier?.Reset();
            return string.Empty;
        }

        internal void Reset(CharacterFootPlacementReset reset)
        {
            m_Grounding.Reset(reset);
            m_PredictiveModifier?.Reset();
        }

        internal void RetargetBodyBranch(ulong resetSequence)
        {
            m_Grounding.RetargetBodyBranch(resetSequence);
            m_PredictiveModifier?.Reset();
        }

        public void Dispose()
        {
            m_PredictiveModifier?.Reset();
            m_Grounding.Dispose();
        }
    }
}
