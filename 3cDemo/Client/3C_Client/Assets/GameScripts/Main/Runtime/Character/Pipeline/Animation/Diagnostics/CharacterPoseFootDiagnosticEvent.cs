#if KK_DIAGNOSTIC_SAMPLING && KK_DIAGNOSTIC_FOOT
using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public static partial class CharacterPoseFootDiagnosticEvent
    {
        public const string EventId = "character-foot-ik/post-commit";
        public const string TargetTypeIdentity = "character-runtime/1";
        public const string LineageTypeIdentity = "character-pose-native-lineage/1";

        internal static bool IsInterested(Guid runtimeInstanceId)
        {
            var target = new DiagnosticEventTargetKey(TargetTypeIdentity, runtimeInstanceId);
            bool interested = false;
            QueryPublishFootCommittedInterest(in target, ref interested);
            return interested;
        }

        internal static void Publish(
            Guid runtimeInstanceId,
            in CharacterPoseDiagnosticFrame frame,
            CharacterPoseConstraintRuntime constraints,
            in CharacterFootIkPhysicalCapture physical,
            in AnimationFootMotionRuntimeSample leftFormalOutput,
            in AnimationFootMotionRuntimeSample rightFormalOutput)
        {
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
            CharacterFullBodyIkGoal pelvisGoal = landing.PelvisGoal;
            CharacterFootPrimarySupportDiagnostics primarySupport =
                landing.PrimarySupport;
            CharacterFootStrideHipsDiagnostics stride = landing.StrideHips;
            CharacterPhysicalFootPose leftPhysical = physical.Left;
            CharacterPhysicalFootPose rightPhysical = physical.Right;
            CharacterPhysicalBodyPose physicalBody = physical.Body;
            var target = new DiagnosticEventTargetKey(
                CharacterPoseFootDiagnosticEvent.TargetTypeIdentity,
                runtimeInstanceId);
            var lineage = new DiagnosticLineageKey(
                CharacterPoseFootDiagnosticEvent.LineageTypeIdentity,
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

        [DiagnosticEvent(CharacterPoseFootDiagnosticEvent.EventId)]
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
            in CharacterPoseDiagnosticFrame frame,
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

    }
}
#endif
