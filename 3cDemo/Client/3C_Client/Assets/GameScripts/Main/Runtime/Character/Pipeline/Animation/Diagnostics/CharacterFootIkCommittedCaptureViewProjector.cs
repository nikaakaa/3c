using System;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal sealed class CharacterFootIkCommittedCaptureViewProjector
    {
        sealed class Page
        {
            internal readonly CharacterFootIkCommittedCaptureViewPage View =
                new CharacterFootIkCommittedCaptureViewPage();
            internal readonly FinalAnimationPoseFramePageLease Lease =
                new FinalAnimationPoseFramePageLease();
            internal ulong CompletionIdentity;

            internal void Clear()
            {
                Lease.Invalidate();
                View.Clear();
                CompletionIdentity = 0;
            }
        }

        readonly Page[] m_Pages = { new Page(), new Page() };
        int m_ActivePageIndex = -1;
        int m_PendingPageIndex = -1;

        internal bool HasPendingFrame => m_PendingPageIndex >= 0;

        internal void BeginFrame(
            in CharacterPoseFrameExecutionResult executionResult,
            in CharacterPoseActorCommittedDiagnosticsView actorDiagnostics,
            in CharacterPoseConstraintCommittedDiagnosticsView
                constraintDiagnostics,
            in CharacterFinalPoseCommittedDiagnosticsView
                publicationDiagnostics,
            bool includeBasicState)
        {
            if (m_PendingPageIndex >= 0)
            {
                throw new InvalidOperationException(
                    "Foot IK committed capture projector has an unpublished frame.");
            }
            if (!executionResult.IsPublished ||
                !actorDiagnostics.IsValid ||
                actorDiagnostics.Result.Lineage != executionResult.Lineage ||
                !constraintDiagnostics.IsValid ||
                constraintDiagnostics.Result.Lineage != executionResult.Lineage ||
                !publicationDiagnostics.IsValid ||
                publicationDiagnostics.Result.Lineage != executionResult.Lineage)
            {
                throw new ArgumentException(
                    "Foot IK committed capture projector inputs are inconsistent.");
            }
            int pageIndex = m_ActivePageIndex == 0 ? 1 : 0;
            Page page = m_Pages[pageIndex];
            page.Clear();
            page.CompletionIdentity =
                executionResult.Lineage.CompletionIdentity;
            page.View.Lineage = executionResult.Lineage;
            ComposedAnimationPoseFrame finalFrame =
                publicationDiagnostics.Frame;
            AnimationPhysicalBoneWriteDiagnostics physicalWrite =
                publicationDiagnostics.PhysicalWrite;
            CopyFootFeatures(
                page.View,
                in finalFrame);
            if (includeBasicState)
            {
                CopyFootPlacement(
                    page.View,
                    in constraintDiagnostics,
                    in physicalWrite);
                page.View.FootStepObservation =
                    ResolveFootStepObservation(
                        in finalFrame,
                        in actorDiagnostics);
            }
            m_PendingPageIndex = pageIndex;
        }

        internal CharacterFootIkCommittedCaptureViewLease Publish()
        {
            if (m_PendingPageIndex < 0)
            {
                throw new InvalidOperationException(
                    "Foot IK committed capture projector has no pending frame.");
            }
            Page page = m_Pages[m_PendingPageIndex];
            page.Lease.BeginWrite(page.CompletionIdentity);
            var result = new CharacterFootIkCommittedCaptureViewLease(
                page.View,
                page.Lease,
                page.CompletionIdentity);
            m_ActivePageIndex = m_PendingPageIndex;
            m_PendingPageIndex = -1;
            return result;
        }

        internal void DiscardPendingFrame()
        {
            if (m_PendingPageIndex < 0)
                return;
            m_Pages[m_PendingPageIndex].Clear();
            m_PendingPageIndex = -1;
        }

        internal void Invalidate()
        {
            for (int i = 0; i < m_Pages.Length; i++)
                m_Pages[i].Clear();
            m_ActivePageIndex = -1;
            m_PendingPageIndex = -1;
        }

        static void CopyFootFeatures(
            CharacterFootIkCommittedCaptureViewPage page,
            in ComposedAnimationPoseFrame finalFrame)
        {
            page.HasFootFeatures = finalFrame.HasFootFeatures;
            AnimationFootFeatureSample left = finalFrame.LeftFootFeatures;
            AnimationFootFeatureSample right = finalFrame.RightFootFeatures;
            page.LeftFootSteps = page.HasFootFeatures
                ? new AnimationBiomechanicalStepReadPage(
                    in left,
                    global::ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSide.Left)
                : default;
            page.RightFootSteps = page.HasFootFeatures
                ? new AnimationBiomechanicalStepReadPage(
                    in right,
                    global::ThirdPersonCharacter.Pipeline.Presentation.CharacterFootSide.Right)
                : default;
        }

        static void CopyFootPlacement(
            CharacterFootIkCommittedCaptureViewPage page,
            in CharacterPoseConstraintCommittedDiagnosticsView
                constraintDiagnostics,
            in AnimationPhysicalBoneWriteDiagnostics physicalWrite)
        {
            CharacterFootLandingPredictionDiagnostics footLandingPrediction =
                constraintDiagnostics.FootLandingPrediction;
            if (!footLandingPrediction.IsCompleted ||
                footLandingPrediction.CompletionIdentity !=
                page.Lineage.CompletionIdentity)
            {
                return;
            }
            CharacterFullBodyIkSolverDiagnostics solverDiagnostics = default;
            CharacterFullBodyIkEffectorDiagnostics pelvis = default;
            CharacterFullBodyIkEffectorDiagnostics leftFoot = default;
            CharacterFullBodyIkEffectorDiagnostics rightFoot = default;
            CharacterFullBodyIkLimbDiagnostics leftLeg = default;
            CharacterFullBodyIkLimbDiagnostics rightLeg = default;
            CharacterFullBodyIkSolverDiagnostics candidate =
                constraintDiagnostics.Solver;
            if (candidate.IsCompleted &&
                candidate.InputCompletionIdentity ==
                page.Lineage.CompletionIdentity &&
                candidate.FrameSequence ==
                footLandingPrediction.FrameSequence)
            {
                bool containsFoot = false;
                for (int effectorIndex = 0;
                     effectorIndex <
                     constraintDiagnostics.SolverEffectorCount;
                     effectorIndex++)
                {
                    CharacterFullBodyIkEffectorDiagnostics effector =
                        constraintDiagnostics.GetSolverEffector(
                            effectorIndex);
                    if (effector.Slot ==
                        CharacterFullBodyIkEffectorSlot.PelvisPreSolveTranslation)
                    {
                        pelvis = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot ==
                             CharacterFullBodyIkEffectorSlot.LeftFoot)
                    {
                        leftFoot = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot ==
                             CharacterFullBodyIkEffectorSlot.RightFoot)
                    {
                        rightFoot = effector;
                        containsFoot = true;
                    }
                }
                if (containsFoot)
                {
                    for (int limbIndex = 0;
                         limbIndex < constraintDiagnostics.SolverLimbCount;
                         limbIndex++)
                    {
                        CharacterFullBodyIkLimbDiagnostics limb =
                            constraintDiagnostics.GetSolverLimb(limbIndex);
                        if (limb.Limb ==
                            CharacterFullBodyIkLimbSlot.LeftLeg)
                        {
                            leftLeg = limb;
                        }
                        else if (limb.Limb ==
                                 CharacterFullBodyIkLimbSlot.RightLeg)
                        {
                            rightLeg = limb;
                        }
                    }
                    solverDiagnostics = candidate;
                }
            }
            page.LandingPrediction = footLandingPrediction;
            page.Solver = solverDiagnostics;
            page.Pelvis = pelvis;
            page.LeftFoot = leftFoot;
            page.RightFoot = rightFoot;
            page.LeftLeg = leftLeg;
            page.RightLeg = rightLeg;
            page.PhysicalWrite =
                physicalWrite.IsAvailable &&
                physicalWrite.CompletionIdentity ==
                page.Lineage.CompletionIdentity
                    ? physicalWrite
                    : default;
        }

        static AnimationFootStepObservationRuntimeSnapshot
            ResolveFootStepObservation(
                in ComposedAnimationPoseFrame finalFrame,
                in CharacterPoseActorCommittedDiagnosticsView
                    actorDiagnostics)
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
            return sourceId.IsValid
                ? actorDiagnostics.ResolveFootStepObservation(
                    sourceId,
                    sourceWeight)
                : default;
        }
    }
}
