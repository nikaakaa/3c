using System;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal readonly struct CharacterPoseProgramCommittedDiagnosticsView
    {
        internal CharacterPoseProgramCommittedDiagnosticsView(
            CharacterPoseProgramFramePages.CommittedDiagnosticsPage page)
        {
            m_Page = page ?? throw new ArgumentNullException(nameof(page));
            m_Identity = page.Identity;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Pose Program committed diagnostics are invalid.",
                    nameof(page));
            }
        }

        readonly CharacterPoseProgramFramePages.CommittedDiagnosticsPage m_Page;
        readonly ulong m_Identity;
        internal bool IsValid =>
            m_Page != null &&
            m_Identity != 0 &&
            m_Page.Identity == m_Identity &&
            m_Page.Result.IsCompleted;
        internal CharacterPoseProgramResult Result
        {
            get
            {
                RequireValid();
                return m_Page.Result;
            }
        }
        internal AnimationPresentationDiagnosticsInterest Interest
        {
            get
            {
                RequireValid();
                return m_Page.Interest;
            }
        }
        internal int PlayerCount => RequireLayout().PlayerCount;
        internal int BoneCount => RequireLayout().BoneCount;
        internal int PoseValueCount => RequireLayout().PoseValueCount;
        internal int PoseValueContributionStride =>
            RequireLayout().PoseValueContributionStride;
        internal AnimationPoseNativeInvalidReason PoseGraphInvalidReason
        {
            get
            {
                RequireValid();
                return m_Page.PoseGraphInvalidReason;
            }
        }

        internal CharacterPoseOperationCompletion GetOperationCompletion(
            int operationIndex)
        {
            RequireValid();
            if ((uint)operationIndex >=
                (uint)m_Page.OperationCompletions.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(operationIndex));
            }
            return m_Page.OperationCompletions[operationIndex];
        }

        internal float GetStateMachineBoneWeight(
            int stateMachineIndex,
            int boneIndex)
        {
            RequireInterest(
                AnimationPresentationDiagnosticsInterest.LiveState |
                AnimationPresentationDiagnosticsInterest.Capture);
            if ((uint)stateMachineIndex >=
                    (uint)m_Page.StateMachineCount ||
                (uint)boneIndex >= (uint)m_Page.Layout.BoneCount)
            {
                throw new ArgumentOutOfRangeException();
            }
            return m_Page.StateMachineBoneWeights[
                stateMachineIndex * m_Page.Layout.BoneCount + boneIndex];
        }

        internal AnimationPlayerPoseNativeRange GetSlotRange(int slotIndex)
        {
            RequireInterest(
                AnimationPresentationDiagnosticsInterest.LiveState |
                AnimationPresentationDiagnosticsInterest.Capture);
            if ((uint)slotIndex >= (uint)m_Page.Layout.PlayerCount)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            return m_Page.SlotRanges[slotIndex];
        }

        internal int GetSlotContributionCount(int slotIndex)
        {
            GetSlotRange(slotIndex);
            return m_Page.SlotContributionCounts[slotIndex];
        }

        internal AnimationPrimitivePoseContribution GetSlotContribution(
            int flatContributionIndex)
        {
            RequireInterest(
                AnimationPresentationDiagnosticsInterest.LiveState |
                AnimationPresentationDiagnosticsInterest.Capture);
            if ((uint)flatContributionIndex >=
                (uint)m_Page.SlotContributions.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(flatContributionIndex));
            }
            return m_Page.SlotContributions[flatContributionIndex];
        }

        internal float GetSlotContributionBoneWeight(
            int flatContributionIndex,
            int boneIndex)
        {
            GetSlotContribution(flatContributionIndex);
            if ((uint)boneIndex >= (uint)m_Page.Layout.BoneCount)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_Page.SlotDenseContributionWeights[
                flatContributionIndex * m_Page.Layout.BoneCount +
                boneIndex];
        }

        internal AnimationPoseAvailability GetValueAvailability(int valueIndex)
        {
            RequireValueIndex(valueIndex);
            return m_Page.ValueAvailability[valueIndex];
        }

        internal AnimationPoseNativeInvalidReason GetValueInvalidReason(
            int valueIndex)
        {
            RequireValueIndex(valueIndex);
            return m_Page.ValueInvalidReasons[valueIndex];
        }

        internal float GetValueOutputWeight(int valueIndex)
        {
            RequireValueIndex(valueIndex);
            return m_Page.ValueOutputWeights[valueIndex];
        }

        internal ulong GetValueContinuityIdentity(int valueIndex)
        {
            RequireValueIndex(valueIndex);
            return m_Page.ValueContinuityIdentities[valueIndex];
        }

        internal int GetValueContributionCount(int valueIndex)
        {
            RequireValueIndex(valueIndex);
            return m_Page.ValueContributionCounts[valueIndex];
        }

        internal AnimationPrimitivePoseContribution GetValueContribution(
            int valueIndex,
            int contributionIndex)
        {
            RequireValueIndex(valueIndex);
            if ((uint)contributionIndex >=
                (uint)m_Page.Layout.PoseValueContributionStride)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(contributionIndex));
            }
            return m_Page.ValueContributions[
                valueIndex * m_Page.Layout.PoseValueContributionStride +
                contributionIndex];
        }

        internal float GetValueContributionBoneWeight(
            int valueIndex,
            int contributionIndex,
            int boneIndex)
        {
            RequireInterest(
                AnimationPresentationDiagnosticsInterest.Capture |
                AnimationPresentationDiagnosticsInterest.OperationDetail);
            GetValueContribution(valueIndex, contributionIndex);
            if ((uint)boneIndex >= (uint)m_Page.Layout.BoneCount)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            int flatContributionIndex =
                valueIndex * m_Page.Layout.PoseValueContributionStride +
                contributionIndex;
            return m_Page.ValueDenseContributionWeights[
                flatContributionIndex * m_Page.Layout.BoneCount + boneIndex];
        }

        internal AnimationLocalBonePose GetValuePose(
            int valueIndex,
            int boneIndex)
        {
            RequireInterest(AnimationPresentationDiagnosticsInterest.PoseWatch);
            RequireValueIndex(valueIndex);
            if ((uint)boneIndex >= (uint)m_Page.Layout.BoneCount)
                throw new ArgumentOutOfRangeException(nameof(boneIndex));
            return m_Page.ValueDenseLocalPoses[
                valueIndex * m_Page.Layout.BoneCount + boneIndex];
        }

        AnimationPoseNativeAggregateLayout RequireLayout()
        {
            RequireValid();
            return m_Page.Layout;
        }

        void RequireValueIndex(int valueIndex)
        {
            RequireInterest(
                AnimationPresentationDiagnosticsInterest.Capture |
                AnimationPresentationDiagnosticsInterest.OperationDetail |
                AnimationPresentationDiagnosticsInterest.PoseWatch);
            if ((uint)valueIndex >= (uint)m_Page.Layout.PoseValueCount)
                throw new ArgumentOutOfRangeException(nameof(valueIndex));
        }

        void RequireInterest(AnimationPresentationDiagnosticsInterest mask)
        {
            RequireValid();
            if ((m_Page.Interest & mask) == 0)
            {
                throw new InvalidOperationException(
                    "Pose Program committed diagnostics group was not frozen.");
            }
        }

        void RequireValid()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Pose Program committed diagnostics lease is stale.");
            }
        }
    }

    internal sealed class CharacterPoseProgramCommittedDiagnosticsProjector
    {
        readonly CharacterPoseProgramExecutionView m_ExecutionView;

        internal CharacterPoseProgramCommittedDiagnosticsProjector(
            CharacterPoseProgramExecutionView executionView)
        {
            m_ExecutionView = executionView ??
                throw new ArgumentNullException(nameof(executionView));
            m_ExecutionView.RequireValid();
        }

        internal CharacterPoseProgramCommittedDiagnosticsView Capture(
            CharacterPoseProgramFramePages framePages,
            in CharacterPoseProgramResult result,
            in CharacterPoseGraphNativeBinding frame,
            AnimationPresentationDiagnosticsInterest interest)
        {
            if (framePages == null ||
                framePages.HasOpenFrame ||
                !result.IsCompleted ||
                interest == AnimationPresentationDiagnosticsInterest.None ||
                result.Lineage.CompletionIdentity !=
                frame.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    "Pose Program committed diagnostics request is invalid.");
            }
            frame.RequireValid();
            CharacterPoseProgramFramePages.CommittedDiagnosticsPage page =
                framePages.CommittedDiagnostics;
            AnimationPoseNativeAggregateLayout frameLayout = frame.Layout;
            RequireDiagnosticsLayout(in frameLayout, in page.Layout);
            page.Identity = 0;
            page.Result = result;
            page.Interest = interest;
            page.PoseGraphInvalidReason = frame.PoseGraphInvalidReason[0];
            for (int i = 0; i < page.OperationCompletions.Length; i++)
                page.OperationCompletions[i] = frame.OperationCompletions[i];

            bool basic = RequiresBasicDiagnostics(interest);
            bool operationDetail = RequiresOperationDiagnostics(interest);
            bool poseWatch =
                (interest &
                 AnimationPresentationDiagnosticsInterest.PoseWatch) != 0;
            if (basic)
            {
                for (int i = 0; i < page.SlotRanges.Length; i++)
                {
                    page.SlotRanges[i] = frame.SlotRanges[i];
                    page.SlotContributionCounts[i] =
                        frame.SlotContributionCounts[i];
                }
                for (int i = 0; i < page.SlotContributions.Length; i++)
                    page.SlotContributions[i] = frame.SlotContributions[i];
                for (int i = 0;
                     i < page.SlotDenseContributionWeights.Length;
                     i++)
                {
                    page.SlotDenseContributionWeights[i] =
                        frame.SlotDenseContributionWeights[i];
                }
                for (int stateMachine = 0;
                     stateMachine < page.StateMachineCount;
                     stateMachine++)
                {
                    for (int bone = 0; bone < page.Layout.BoneCount; bone++)
                    {
                        page.StateMachineBoneWeights[
                            stateMachine * page.Layout.BoneCount + bone] =
                            GetStateMachineBoneWeight(
                                framePages,
                                stateMachine,
                                bone);
                    }
                }
            }
            if (operationDetail || poseWatch)
            {
                for (int i = 0; i < page.Layout.PoseValueCount; i++)
                {
                    page.ValueContributionCounts[i] =
                        frame.ValueContributionCounts[i];
                    page.ValueOutputWeights[i] = frame.ValueOutputWeights[i];
                    page.ValueAvailability[i] = frame.ValueAvailability[i];
                    page.ValueContinuityIdentities[i] =
                        frame.ValueContinuityIdentities[i];
                    page.ValueInvalidReasons[i] =
                        frame.ValueInvalidReasons[i];
                }
                for (int i = 0; i < page.ValueContributions.Length; i++)
                    page.ValueContributions[i] = frame.ValueContributions[i];
            }
            if (operationDetail)
            {
                for (int i = 0;
                     i < page.ValueDenseContributionWeights.Length;
                     i++)
                {
                    page.ValueDenseContributionWeights[i] =
                        frame.ValueDenseContributionWeights[i];
                }
            }
            if (poseWatch)
            {
                for (int i = 0; i < page.ValueDenseLocalPoses.Length; i++)
                    page.ValueDenseLocalPoses[i] = frame.ValueDenseLocalPoses[i];
            }
            framePages.PublishCommittedDiagnostics();
            return new CharacterPoseProgramCommittedDiagnosticsView(page);
        }

        float GetStateMachineBoneWeight(
            CharacterPoseProgramFramePages framePages,
            int stateMachineIndex,
            int boneIndex)
        {
            NativeArray<CharacterPoseStateMachineNativeControl> controls =
                framePages.StateMachineControls;
            if ((uint)stateMachineIndex >= (uint)controls.Length ||
                (uint)boneIndex >= (uint)m_ExecutionView.PoseBoneCount)
            {
                throw new ArgumentOutOfRangeException();
            }
            CharacterPoseStateMachineNativeControl control =
                controls[stateMachineIndex];
            if (control.BlendMode !=
                    CharacterPoseStateMachineBlendMode.Standard ||
                control.DurationSeconds <= 0f)
            {
                return 1f;
            }
            NativeArray<AnimationBlendCurveNativeEntry> curves =
                m_ExecutionView.BlendCurves;
            NativeArray<AnimationBlendCurveSegment> segments =
                m_ExecutionView.BlendCurveSegments;
            NativeArray<AnimationBlendProfileNativeEntry> profiles =
                m_ExecutionView.BlendProfiles;
            NativeArray<float> denseProfiles =
                m_ExecutionView.BlendDenseProfiles;
            if ((uint)control.CurveIndex >= (uint)curves.Length ||
                (uint)control.BlendProfileIndex >= (uint)profiles.Length)
            {
                throw new InvalidOperationException(
                    "Pose StateMachine diagnostic blend indices are invalid.");
            }
            AnimationBlendProfileNativeEntry profile =
                profiles[control.BlendProfileIndex];
            float duration = control.DurationSeconds *
                             profile.GlobalDurationMultiplier *
                             denseProfiles[profile.DenseOffset + boneIndex];
            if (duration <= 0f || control.ElapsedSeconds >= duration)
                return 1f;
            float time = Mathf.Clamp01(control.ElapsedSeconds / duration);
            AnimationBlendCurveNativeEntry curve = curves[control.CurveIndex];
            AnimationBlendCurveSegment segment =
                segments[curve.SegmentOffset + curve.SegmentCount - 1];
            for (int i = 0; i < curve.SegmentCount; i++)
            {
                AnimationBlendCurveSegment candidate =
                    segments[curve.SegmentOffset + i];
                if (time > candidate.EndTime)
                    continue;
                segment = candidate;
                break;
            }
            float normalized = (time - segment.StartTime) /
                               (segment.EndTime - segment.StartTime);
            return Mathf.Clamp01(
                ((segment.A * normalized + segment.B) * normalized +
                 segment.C) * normalized + segment.D);
        }

        static void RequireDiagnosticsLayout(
            in AnimationPoseNativeAggregateLayout frame,
            in AnimationPoseNativeAggregateLayout page)
        {
            if (frame.PlayerCount != page.PlayerCount ||
                frame.BoneCount != page.BoneCount ||
                frame.ParameterCount != page.ParameterCount ||
                frame.TotalPlayerContributionCapacity !=
                page.TotalPlayerContributionCapacity ||
                frame.PoseValueCount != page.PoseValueCount ||
                frame.PoseValueContributionStride !=
                page.PoseValueContributionStride ||
                frame.OperationCount != page.OperationCount ||
                frame.StageCount != page.StageCount)
            {
                throw new InvalidOperationException(
                    "Pose Program committed diagnostics layout is inconsistent.");
            }
        }

        static bool RequiresBasicDiagnostics(
            AnimationPresentationDiagnosticsInterest interest) =>
            (interest &
             (AnimationPresentationDiagnosticsInterest.LiveState |
              AnimationPresentationDiagnosticsInterest.Capture)) != 0;

        static bool RequiresOperationDiagnostics(
            AnimationPresentationDiagnosticsInterest interest) =>
            (interest &
             (AnimationPresentationDiagnosticsInterest.Capture |
              AnimationPresentationDiagnosticsInterest.OperationDetail)) != 0;
    }
}
