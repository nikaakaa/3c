#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    public static class CharacterFootIkCommitDiagnosticEvent
    {
        public const string EventId = "character-foot-ik/post-commit";
        public const string TargetTypeIdentity = "character-runtime/1";
        public const string LineageTypeIdentity =
            "character-foot-ik-lineage/1";
    }

    internal sealed partial class
        CharacterPoseCommittedDiagnosticsEventPublisher
    {
        partial void QueryFootDiagnosticEventInterest(
            ref bool interested)
        {
            var target = new DiagnosticEventTargetKey(
                CharacterFootIkCommitDiagnosticEvent.TargetTypeIdentity,
                m_RuntimeInstanceId);
            QueryPublishFootCommittedInterest(
                in target,
                ref interested);
        }

        partial void PublishFootDiagnosticEvent(
            in CharacterPoseFrameLineage frame,
            in CharacterPoseConstraintResult constraintResult,
            in CharacterFinalPosePublicationResult publicationResult,
            CharacterPoseProgramRuntime program,
            CharacterPoseConstraintRuntime constraints,
            CharacterFinalPosePublication publication)
        {
            constraints.RequireCommittedFootIkCapture(
                in constraintResult);
            ComposedAnimationPoseFrame finalFrame =
                publication.RequireCommittedFrame(
                    in publicationResult);
            CharacterFootIkPhysicalCapture physical =
                publication.RequireCommittedFootIkPhysical(
                    in publicationResult);
            CharacterFootLandingPredictionDiagnostics landing =
                constraints.CommittedFootLandingPrediction;
            if (!landing.IsCompleted ||
                landing.FrameSequence != frame.PresentationFrame ||
                landing.CompletionIdentity != frame.CompletionIdentity)
            {
                throw new System.InvalidOperationException(
                    "Committed Foot IK landing facts are inconsistent.");
            }
            CharacterFullBodyIkSolverDiagnostics solver = default;
            CharacterFullBodyIkEffectorDiagnostics pelvis = default;
            CharacterFullBodyIkEffectorDiagnostics leftEffector = default;
            CharacterFullBodyIkEffectorDiagnostics rightEffector = default;
            CharacterFullBodyIkLimbDiagnostics leftLeg = default;
            CharacterFullBodyIkLimbDiagnostics rightLeg = default;
            CharacterFullBodyIkSolverDiagnostics candidate =
                constraints.CommittedFullBodyIkSolver;
            if (candidate.IsCompleted &&
                candidate.InputCompletionIdentity == frame.CompletionIdentity &&
                candidate.FrameSequence == landing.FrameSequence)
            {
                bool containsFoot = false;
                for (int i = 0;
                     i < constraints.CommittedSolverEffectorCount;
                     i++)
                {
                    CharacterFullBodyIkEffectorDiagnostics effector =
                        constraints.GetCommittedSolverEffector(i);
                    if (effector.Slot ==
                        CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                    {
                        pelvis = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot ==
                             CharacterFullBodyIkEffectorSlot.LeftFoot)
                    {
                        leftEffector = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot ==
                             CharacterFullBodyIkEffectorSlot.RightFoot)
                    {
                        rightEffector = effector;
                        containsFoot = true;
                    }
                }
                if (containsFoot)
                {
                    for (int i = 0;
                         i < constraints.CommittedSolverLimbCount;
                         i++)
                    {
                        CharacterFullBodyIkLimbDiagnostics limb =
                            constraints.GetCommittedSolverLimb(i);
                        if (limb.Limb == CharacterFullBodyIkLimbSlot.LeftLeg)
                            leftLeg = limb;
                        else if (limb.Limb ==
                                 CharacterFullBodyIkLimbSlot.RightLeg)
                            rightLeg = limb;
                    }
                    solver = candidate;
                }
            }
            CharacterFootLandingPredictionFootDiagnostics leftFoot =
                landing.Left;
            CharacterFootLandingPredictionFootDiagnostics rightFoot =
                landing.Right;
            CharacterFootLandingPredictionInputDiagnostics input =
                landing.Input;
            CharacterFootStepObservationInputDiagnostics formalInput =
                input.FootStepObservation;
            AnimationFootMotionRuntimeSample leftFormalInput =
                formalInput.Left;
            AnimationFootMotionRuntimeSample rightFormalInput =
                formalInput.Right;
            ResolveCommittedFootMotion(
                in finalFrame,
                program,
                out AnimationFootMotionRuntimeSample leftFormalOutput,
                out AnimationFootMotionRuntimeSample rightFormalOutput);
            CharacterFullBodyIkGoal pelvisGoal = landing.PelvisGoal;
            CharacterFootPrimarySupportDiagnostics primarySupport =
                landing.PrimarySupport;
            CharacterFootStrideHipsDiagnostics stride = landing.StrideHips;
            CharacterPhysicalFootPose leftPhysical = physical.Left;
            CharacterPhysicalFootPose rightPhysical = physical.Right;
            CharacterPhysicalBodyPose physicalBody = physical.Body;
            var target = new DiagnosticEventTargetKey(
                CharacterFootIkCommitDiagnosticEvent.TargetTypeIdentity,
                m_RuntimeInstanceId);
            var lineage = new DiagnosticLineageKey(
                CharacterFootIkCommitDiagnosticEvent.LineageTypeIdentity,
                frame.PresentationFrame,
                frame.CompletionIdentity);
            PublishFootCommitted(
                in target,
                in lineage,
                in leftEffector,
                in leftFoot,
                in leftFormalInput,
                in leftFormalOutput,
                in leftLeg,
                in leftPhysical,
                in rightEffector,
                in rightFoot,
                in rightFormalInput,
                in rightFormalOutput,
                in rightLeg,
                in rightPhysical,
                in frame,
                in input,
                in pelvis,
                in pelvisGoal,
                in physicalBody,
                in primarySupport,
                in solver,
                in stride);
        }

        [DiagnosticEvent(CharacterFootIkCommitDiagnosticEvent.EventId)]
        static partial void PublishFootCommitted(
            in DiagnosticEventTargetKey target,
            in DiagnosticLineageKey lineage,
            in CharacterFullBodyIkEffectorDiagnostics leftEffector,
            in CharacterFootLandingPredictionFootDiagnostics leftFoot,
            in AnimationFootMotionRuntimeSample leftFormalInput,
            in AnimationFootMotionRuntimeSample leftFormalOutput,
            in CharacterFullBodyIkLimbDiagnostics leftLeg,
            in CharacterPhysicalFootPose leftPhysical,
            in CharacterFullBodyIkEffectorDiagnostics rightEffector,
            in CharacterFootLandingPredictionFootDiagnostics rightFoot,
            in AnimationFootMotionRuntimeSample rightFormalInput,
            in AnimationFootMotionRuntimeSample rightFormalOutput,
            in CharacterFullBodyIkLimbDiagnostics rightLeg,
            in CharacterPhysicalFootPose rightPhysical,
            in CharacterPoseFrameLineage frame,
            in CharacterFootLandingPredictionInputDiagnostics input,
            in CharacterFullBodyIkEffectorDiagnostics pelvis,
            in CharacterFullBodyIkGoal pelvisGoal,
            in CharacterPhysicalBodyPose physicalBody,
            in CharacterFootPrimarySupportDiagnostics primarySupport,
            in CharacterFullBodyIkSolverDiagnostics solver,
            in CharacterFootStrideHipsDiagnostics stride);

        static partial void QueryPublishFootCommittedInterest(
            in DiagnosticEventTargetKey target,
            ref bool interested);

        void ResolveCommittedFootMotion(
            in ComposedAnimationPoseFrame finalFrame,
            CharacterPoseProgramRuntime program,
            out AnimationFootMotionRuntimeSample left,
            out AnimationFootMotionRuntimeSample right)
        {
            AnimationPoseSourceId sourceId = default;
            float sourceWeight = -1f;
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution>
                contributions = finalFrame.Contributions;
            for (int i = 0; i < contributions.Count; i++)
            {
                AnimationPoseSourceContribution contribution =
                    contributions[i];
                if (contribution.Kind !=
                        AnimationPoseContributionKind.Live ||
                    contribution.Weight <= sourceWeight)
                {
                    continue;
                }
                sourceId = contribution.SourceId;
                sourceWeight = contribution.Weight;
            }
            if (!sourceId.IsValid)
            {
                left = default;
                right = default;
                return;
            }
            for (int i = 0;
                 i < program.PoseStateSources.ClipPlayers.Length;
                 i++)
            {
                AnimationClipPlayerRuntime player =
                    program.PoseStateSources.ClipPlayers[i];
                if (player.SourceId.Equals(sourceId))
                {
                    player.CreateFootMotionSamples(
                        sourceWeight,
                        out left,
                        out right);
                    return;
                }
            }
            left = default;
            right = default;
        }
    }
}
#endif
