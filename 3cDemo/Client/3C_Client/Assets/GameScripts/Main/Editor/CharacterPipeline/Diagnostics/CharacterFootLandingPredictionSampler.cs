using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    public static class CharacterFootLandingPredictionSampler
    {
        const string StartMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Start";
        const string StopMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Stop and Save";
        const string Header =
            "FrameSequence,CompletionIdentity,RootInstanceId,Side,State,RejectReason,StepSource," +
            "LandingEventIdentity,TrajectoryGeneration,LandingConfidence,TimeToLandingSeconds," +
            "RootLocalLandingX,RootLocalLandingY,RootLocalLandingZ," +
            "PresentationDeltaSeconds,PreviousBodyTick,CurrentBodyTick,BodySampleAlpha,BodySampleAgeSeconds," +
            "MotionTimelineAvailable,TimelineGeneration,TimelineAuthorityTick,TimelineTickRate," +
            "TimelineCurrentVelocityX,TimelineCurrentVelocityZ,TimelineContinuationVelocityX,TimelineContinuationVelocityZ," +
            "TimelineHasContinuation,TimelineBodyYawVelocityDegreesPerSecond,TimelineMaximumBodyYawVelocityDegreesPerSecond,CurrentSegmentRemainingSeconds," +
            "Grounded,HorizontalSpeed,LeftActionInstanceIdentity,LeftActionFootWeight,RightActionInstanceIdentity,RightActionFootWeight," +
            "VisibleBodyPositionX,VisibleBodyPositionY,VisibleBodyPositionZ," +
            "VisibleBodyRotationX,VisibleBodyRotationY,VisibleBodyRotationZ,VisibleBodyRotationW," +
            "VisibleBodyVelocityX,VisibleBodyVelocityY,VisibleBodyVelocityZ,VisibleBodyYawVelocityDegreesPerSecond," +
            "TargetBodyPositionX,TargetBodyPositionY,TargetBodyPositionZ," +
            "TargetBodyRotationX,TargetBodyRotationY,TargetBodyRotationZ,TargetBodyRotationW," +
            "TargetBodyVelocityX,TargetBodyVelocityY,TargetBodyVelocityZ,TargetBodyYawVelocityDegreesPerSecond," +
            "BodyPositionError,BodyRotationError," +
            "CorrectionPositionErrorX,CorrectionPositionErrorY,CorrectionPositionErrorZ," +
            "CorrectionPositionVelocityX,CorrectionPositionVelocityY,CorrectionPositionVelocityZ," +
            "CorrectionYawVelocityDegreesPerSecond,CorrectionActive,CorrectionClamped,CorrectionSettled,BodyResetSequence," +
            "FutureBodyTranslationAvailable,FutureBodyRelativeTranslationX,FutureBodyRelativeTranslationY,FutureBodyRelativeTranslationZ," +
            "FutureBodyTranslationVelocityX,FutureBodyTranslationVelocityY,FutureBodyTranslationVelocityZ," +
            "CurrentAnimatedSoleX,CurrentAnimatedSoleY,CurrentAnimatedSoleZ," +
            "RawLandingCandidateX,RawLandingCandidateY,RawLandingCandidateZ," +
            "QueryShape,QueryPurpose,QueryFootIndex,QueryOriginX,QueryOriginY,QueryOriginZ," +
            "QueryDirectionX,QueryDirectionY,QueryDirectionZ,QueryMaximumDistance,QueryRadius,QueryLayerMask,QueryMinimumGroundNormalDot," +
            "Accepted,SurfaceIdentity,LandingPointX,LandingPointY,LandingPointZ," +
            "LandingNormalX,LandingNormalY,LandingNormalZ,QueryDistance," +
            "GroundPathState,GroundPathRejectReason,GroundPathInputIdentity,GroundPathQueryExecuted," +
            "GroundPathLastLandingEventIdentity,GroundPathNextSwingLandingEventIdentity,GroundPathTrajectoryGeneration,GroundPathAuthorityTick," +
            "GroundPathLastFutureBodyTranslationSourceIdentity,GroundPathNextSwingFutureBodyTranslationSourceIdentity," +
            "GroundPathLastLandingX,GroundPathLastLandingY,GroundPathLastLandingZ," +
            "GroundPathNextSwingLandingX,GroundPathNextSwingLandingY,GroundPathNextSwingLandingZ," +
            "GroundPathLastLandingNormalX,GroundPathLastLandingNormalY,GroundPathLastLandingNormalZ," +
            "GroundPathNextSwingLandingNormalX,GroundPathNextSwingLandingNormalY,GroundPathNextSwingLandingNormalZ," +
            "GroundPathLastLandingSurfaceIdentity,GroundPathNextSwingLandingSurfaceIdentity," +
            "GroundPathComponentUpX,GroundPathComponentUpY,GroundPathComponentUpZ," +
            "GroundPathAxisStartX,GroundPathAxisStartY,GroundPathAxisStartZ," +
            "GroundPathAxisEndX,GroundPathAxisEndY,GroundPathAxisEndZ," +
            "GroundPathRadius,GroundPathMaximumAxisSegmentLength,GroundPathDirectionX,GroundPathDirectionY,GroundPathDirectionZ," +
            "GroundPathMaximumDistance,GroundPathLayerMask,GroundPathSegmentHitCapacity,GroundPathContactCapacity,GroundPathSegmentCount,GroundPathContactCount," +
            "GroundPathEdgeCount,GroundPathHasInvalidSegment,GroundPathFirstInvalidSegmentIndex,GroundPathFirstInvalidSegmentIdentity," +
            "GroundPathFirstInvalidSegmentBottomX,GroundPathFirstInvalidSegmentBottomY,GroundPathFirstInvalidSegmentBottomZ," +
            "GroundPathFirstInvalidSegmentTopX,GroundPathFirstInvalidSegmentTopY,GroundPathFirstInvalidSegmentTopZ," +
            "GroundPathFirstInvalidSegmentVerticalDistance,GroundPathMaximumReachableVerticalEdge,GroundEnvelopeVertexCount," +
            "FootMotionState,FootMotionRejectReason,FootMotionLandingEventIdentity,FootMotionGroundPathInputIdentity," +
            "FootMotionDistance,FootMotionProgress," +
            "FootMotionOriginalSoleX,FootMotionOriginalSoleY,FootMotionOriginalSoleZ," +
            "FootMotionOriginalAnkleX,FootMotionOriginalAnkleY,FootMotionOriginalAnkleZ," +
            "FootMotionBaselineSampleX,FootMotionBaselineSampleY,FootMotionBaselineSampleZ," +
            "FootMotionEnvelopeSampleX,FootMotionEnvelopeSampleY,FootMotionEnvelopeSampleZ,FootMotionVerticalCorrection," +
            "FootMotionLandingPredictionError,FootMotionLandingConstraintWeight," +
            "FootMotionCorrectedSoleX,FootMotionCorrectedSoleY,FootMotionCorrectedSoleZ," +
            "FootMotionCorrectedAnkleX,FootMotionCorrectedAnkleY,FootMotionCorrectedAnkleZ,FootMotionPositionWeight,FootMotionRotationWeight," +
            "FootMotionSupportLockState,FootMotionSupportHorizontalError,FootMotionSupportUnlockRemainingSeconds," +
            "FootMotionSupportUnlockCorrectionX,FootMotionSupportUnlockCorrectionY,FootMotionSupportUnlockCorrectionZ," +
            "FinalGoalPositionX,FinalGoalPositionY,FinalGoalPositionZ,FinalGoalRotationX,FinalGoalRotationY,FinalGoalRotationZ,FinalGoalRotationW,FinalGoalPositionWeight,FinalGoalRotationWeight,PelvisPositionWeight,PelvisRotationWeight," +
            "StrideState,StrideRejectReason,StrideSupportSide,StrideSwingSide,StrideProgress,StrideSlope," +
            "StrideStartX,StrideStartY,StrideStartZ,StrideEndX,StrideEndY,StrideEndZ," +
            "StrideAnimatedPelvisX,StrideAnimatedPelvisY,StrideAnimatedPelvisZ," +
            "StrideAnimatedPelvisComponentPositionX,StrideAnimatedPelvisComponentPositionY,StrideAnimatedPelvisComponentPositionZ," +
            "StrideRawPelvisDeltaX,StrideRawPelvisDeltaY,StrideRawPelvisDeltaZ," +
            "StrideRawPelvisTargetAlongUp,StrideClearanceCorrectionAlongUp,StrideHadPreviousState,StrideSupportChanged," +
            "StridePreviousStrideStartX,StridePreviousStrideStartY,StridePreviousStrideStartZ,StrideRebaseAlongUp," +
            "StridePreviousRawPelvisTargetAlongUp,StrideRebasedPreviousRawPelvisTargetAlongUp," +
            "StridePreviousSpringOutput,StrideRebasedPreviousSpringOutput,StrideNecessaryDelta,StrideSpringInput," +
            "StrideSpringTarget,StrideSpringOutput,StrideSpringVelocity,StrideSpringDelta," +
            "StridePelvisDeltaX,StridePelvisDeltaY,StridePelvisDeltaZ,StridePositionWeight," +
            "FinalPelvisGoalX,FinalPelvisGoalY,FinalPelvisGoalZ," +
            "FinalPhysicalPelvisComponentPositionX,FinalPhysicalPelvisComponentPositionY,FinalPhysicalPelvisComponentPositionZ,FinalPhysicalPelvisGoalResidual," +
            "FinalIkSolverAvailable,FinalIkSucceeded,FinalIkFrameSequence,FinalIkInputCompletionIdentity,FinalIkOutputCompletionIdentity," +
            "FinalIkBackendIdentity,FinalIkRigId,FinalIkRigRevision,FinalIkProfileId,FinalIkProfileRevision,FinalIkFailure,FinalIkAppliedGoalCount," +
            "FinalIkEffectorAvailable,FinalIkEffectorSlot,FinalIkTargetPositionX,FinalIkTargetPositionY,FinalIkTargetPositionZ," +
            "FinalIkSolvedPositionX,FinalIkSolvedPositionY,FinalIkSolvedPositionZ,FinalIkPositionResidual,FinalIkRotationResidualDegrees," +
            "FinalIkPelvisAvailable,FinalIkPelvisTargetPositionX,FinalIkPelvisTargetPositionY,FinalIkPelvisTargetPositionZ," +
            "FinalIkPelvisSolvedPositionX,FinalIkPelvisSolvedPositionY,FinalIkPelvisSolvedPositionZ,FinalIkPelvisPositionResidual,FinalIkPelvisRotationResidualDegrees," +
            "FinalPhysicalWriteAvailable,FinalPhysicalWriteCompletionIdentity," +
            "FinalPhysicalAnkleComponentPositionX,FinalPhysicalAnkleComponentPositionY,FinalPhysicalAnkleComponentPositionZ,FinalPhysicalAnkleGoalResidual," +
            "GroundContactIndex,GroundContactSegmentIndex,GroundContactSurfaceIdentity,GroundContactCandidateIdentity," +
            "GroundContactPositionX,GroundContactPositionY,GroundContactPositionZ," +
            "GroundContactNormalX,GroundContactNormalY,GroundContactNormalZ,GroundContactQueryDistance," +
            "GroundEnvelopeVertexIndex,GroundEnvelopeVertexX,GroundEnvelopeVertexY,GroundEnvelopeVertexZ";

        readonly struct FootIkCapture
        {
            internal FootIkCapture(
                CharacterFullBodyIkSolverDiagnostics solver,
                CharacterFullBodyIkEffectorDiagnostics pelvis,
                CharacterFullBodyIkEffectorDiagnostics effector,
                bool physicalWriteAvailable,
                ulong physicalWriteCompletionIdentity,
                Vector3 physicalAnkleComponentPosition,
                Vector3 physicalPelvisComponentPosition)
            {
                Solver = solver;
                Pelvis = pelvis;
                Effector = effector;
                PhysicalWriteAvailable = physicalWriteAvailable;
                PhysicalWriteCompletionIdentity = physicalWriteCompletionIdentity;
                PhysicalAnkleComponentPosition = physicalAnkleComponentPosition;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
            }

            internal CharacterFullBodyIkSolverDiagnostics Solver { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Pelvis { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Effector { get; }
            internal bool SolverAvailable => Solver.IsCompleted;
            internal bool PelvisAvailable => Pelvis.IsAvailable;
            internal bool EffectorAvailable => Effector.IsAvailable;
            internal bool PhysicalWriteAvailable { get; }
            internal ulong PhysicalWriteCompletionIdentity { get; }
            internal Vector3 PhysicalAnkleComponentPosition { get; }
            internal Vector3 PhysicalPelvisComponentPosition { get; }
        }

        sealed class PendingFrame
        {
            internal PendingFrame(in CharacterFootLandingPredictionDiagnostics diagnostics)
            {
                Diagnostics = diagnostics;
            }

            internal CharacterFootLandingPredictionDiagnostics Diagnostics { get; }
        }

        sealed class CapturedFrame
        {
            internal CapturedFrame(
                in CharacterFootLandingPredictionDiagnostics foot,
                FootIkCapture left,
                FootIkCapture right,
                Vector3 physicalPelvisComponentPosition)
            {
                Foot = foot;
                Left = left;
                Right = right;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
            }

            internal CharacterFootLandingPredictionDiagnostics Foot { get; }
            internal FootIkCapture Left { get; }
            internal FootIkCapture Right { get; }
            internal Vector3 PhysicalPelvisComponentPosition { get; }
        }

        static readonly List<CapturedFrame> s_Frames =
            new List<CapturedFrame>(4096);
        static readonly List<PendingFrame> s_PendingFrames =
            new List<PendingFrame>(64);
        static readonly HashSet<Guid> s_ConfiguredTargets = new HashSet<Guid>();
        static readonly Dictionary<Guid, string> s_PoseWatchSignatures =
            new Dictionary<Guid, string>();
        static readonly Guid s_DiagnosticsOwnerId = Guid.NewGuid();

        static bool s_Capturing;
        static string s_LastSavedPath = string.Empty;

        public static bool IsCapturing => s_Capturing;
        public static string LastSavedPath => s_LastSavedPath;

        static CharacterFootLandingPredictionSampler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        public static void StartSampling()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Foot Landing sampling can only start in Play Mode.");
            if (s_Capturing)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already active.");
            s_Frames.Clear();
            s_PendingFrames.Clear();
            s_LastSavedPath = string.Empty;
            CharacterFootLandingPredictionDebugRegistry.Published += Capture;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered += ConfigureTarget;
            AnimationPresentationRuntimeTargetRegistry.TargetUnregistered += RemoveTarget;
            EditorApplication.update += ProcessPendingFrames;
            s_Capturing = true;
            try
            {
                ConfigureTargets();
            }
            catch
            {
                CancelSamplingStart();
                throw;
            }
            Debug.Log("Foot Landing sampling started.");
        }

        [MenuItem(StartMenu)]
        static void StartFromMenu() => StartSampling();

        [MenuItem(StartMenu, true)]
        static bool CanStart() => EditorApplication.isPlaying && !s_Capturing;

        [MenuItem(StopMenu)]
        static void Stop() => StopAndSave();

        [MenuItem(StopMenu, true)]
        static bool CanStop() => s_Capturing;

        public static void StopAndSaveSampling() => StopAndSave();

        static void Capture(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (s_Capturing)
                s_PendingFrames.Add(new PendingFrame(in diagnostics));
        }

        static void ProcessPendingFrames()
        {
            if (!s_Capturing || s_PendingFrames.Count == 0)
                return;
            ConfigureTargets();
            for (int pendingIndex = 0; pendingIndex < s_PendingFrames.Count;)
            {
                PendingFrame pending = s_PendingFrames[pendingIndex];
                CharacterFootLandingPredictionDiagnostics pendingDiagnostics = pending.Diagnostics;
                if (!TryCaptureCommittedIk(in pendingDiagnostics, out CapturedFrame captured))
                {
                    pendingIndex++;
                    continue;
                }
                s_Frames.Add(captured);
                s_PendingFrames.RemoveAt(pendingIndex);
            }
        }

        static bool TryCaptureCommittedIk(
            in CharacterFootLandingPredictionDiagnostics pending,
            out CapturedFrame captured)
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                AnimationPresentationRuntimeTarget target = targets[targetIndex];
                if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                    continue;
                AnimationFootPlacementRuntimeSnapshot placement = debugView.PosePlan.FootPlacement;
                if (!placement.IsAvailable ||
                    placement.LandingPrediction.RootInstanceId != pending.RootInstanceId ||
                    placement.LandingPrediction.FrameSequence != pending.FrameSequence ||
                    placement.LandingPrediction.CompletionIdentity != pending.CompletionIdentity)
                {
                    continue;
                }
                captured = new CapturedFrame(
                    in pending,
                    new FootIkCapture(
                        placement.Solver,
                        placement.Pelvis,
                        placement.LeftFoot,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.LeftPhysicalAnkleComponentPosition,
                        placement.PhysicalPelvisComponentPosition),
                    new FootIkCapture(
                        placement.Solver,
                        placement.Pelvis,
                        placement.RightFoot,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.RightPhysicalAnkleComponentPosition,
                        placement.PhysicalPelvisComponentPosition),
                    placement.PhysicalPelvisComponentPosition);
                return true;
            }
            captured = default;
            return false;
        }

        static void ConfigureTargets()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
                ConfigureTarget(targets[i]);
        }

        static void ConfigureTarget(AnimationPresentationRuntimeTarget target)
        {
            if (!s_Capturing || target == null)
                return;
            if (!s_ConfiguredTargets.Contains(target.RuntimeInstanceId))
            {
                target.SetDiagnosticsInterest(
                    s_DiagnosticsOwnerId,
                    AnimationPresentationDiagnosticsInterest.Capture |
                    AnimationPresentationDiagnosticsInterest.OperationDetail);
                s_ConfiguredTargets.Add(target.RuntimeInstanceId);
            }
            if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                return;
            IReadOnlyList<AnimationPoseWatchIdentity> watches = BuildPoseWatches(debugView.PosePlan);
            string signature = BuildPoseWatchSignature(watches);
            if (string.Equals(
                    s_PoseWatchSignatures.TryGetValue(target.RuntimeInstanceId, out string previous)
                        ? previous
                        : string.Empty,
                    signature,
                    StringComparison.Ordinal))
            {
                return;
            }
            s_PoseWatchSignatures[target.RuntimeInstanceId] = signature;
            target.SetPoseWatchInterests(s_DiagnosticsOwnerId, watches);
        }

        static void RemoveTarget(AnimationPresentationRuntimeTarget target)
        {
            if (target == null)
                return;
            s_ConfiguredTargets.Remove(target.RuntimeInstanceId);
            s_PoseWatchSignatures.Remove(target.RuntimeInstanceId);
        }

        static IReadOnlyList<AnimationPoseWatchIdentity> BuildPoseWatches(
            AnimationPresentationRuntimeSnapshot snapshot)
        {
            var result = new List<AnimationPoseWatchIdentity>(4);
            AnimationReadOnlyBuffer<AnimationPoseOperationSnapshot> operations = snapshot.Operations;
            for (int i = 0; i < operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = operations[i];
                if (operation.Code != CharacterPoseOperationCode.FootPlacement &&
                    operation.Code != CharacterPoseOperationCode.FullBodyIK)
                {
                    continue;
                }
                result.Add(new AnimationPoseWatchIdentity(
                    operation.GraphId,
                    snapshot.PoseGraphRevision,
                    operation.NodeId,
                    operation.CallSite));
            }
            return result;
        }

        static string BuildPoseWatchSignature(IReadOnlyList<AnimationPoseWatchIdentity> watches)
        {
            if (watches == null || watches.Count == 0)
                return string.Empty;
            var builder = new StringBuilder(256);
            for (int i = 0; i < watches.Count; i++)
            {
                if (builder.Length != 0)
                    builder.Append('|');
                builder.Append(watches[i]);
            }
            return builder.ToString();
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                StopAndSave();
        }

        static void OnBeforeAssemblyReload() => StopAndSave();

        static void CancelSamplingStart()
        {
            CharacterFootLandingPredictionDebugRegistry.Published -= Capture;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered -= ConfigureTarget;
            AnimationPresentationRuntimeTargetRegistry.TargetUnregistered -= RemoveTarget;
            EditorApplication.update -= ProcessPendingFrames;
            RemoveTargetDiagnostics();
            s_Capturing = false;
            s_Frames.Clear();
            s_PendingFrames.Clear();
        }

        static string StopAndSave()
        {
            if (!s_Capturing)
                return s_LastSavedPath;
            ProcessPendingFrames();
            CharacterFootLandingPredictionDebugRegistry.Published -= Capture;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered -= ConfigureTarget;
            AnimationPresentationRuntimeTargetRegistry.TargetUnregistered -= RemoveTarget;
            EditorApplication.update -= ProcessPendingFrames;
            RemoveTargetDiagnostics();
            s_Capturing = false;
            try
            {
                s_LastSavedPath = Save();
                Debug.Log(
                    $"Foot Landing sampling saved {s_Frames.Count} frames to {s_LastSavedPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                s_Frames.Clear();
                s_PendingFrames.Clear();
            }
            return s_LastSavedPath;
        }

        static void RemoveTargetDiagnostics()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                AnimationPresentationRuntimeTarget target = targets[i];
                if (!s_ConfiguredTargets.Contains(target.RuntimeInstanceId))
                    continue;
                target.RemovePoseWatchInterests(s_DiagnosticsOwnerId);
                target.RemoveDiagnosticsInterest(s_DiagnosticsOwnerId);
            }
            s_ConfiguredTargets.Clear();
            s_PoseWatchSignatures.Clear();
        }

        static string Save()
        {
            string directory = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                "FootLandingSamples"));
            Directory.CreateDirectory(directory);
            string path = Path.Combine(
                directory,
                $"foot-landing-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.csv");
            using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.WriteLine(Header);
            var row = new StringBuilder(2048);
            for (int i = 0; i < s_Frames.Count; i++)
            {
                CapturedFrame captured = s_Frames[i];
                CharacterFootLandingPredictionDiagnostics frame = captured.Foot;
                FootIkCapture left = captured.Left;
                FootIkCapture right = captured.Right;
                CharacterFootLandingPredictionFootDiagnostics leftFoot = frame.Left;
                CharacterFootLandingPredictionFootDiagnostics rightFoot = frame.Right;
                WriteRows(writer, row, in frame, in leftFoot, in left);
                WriteRows(writer, row, in frame, in rightFoot, in right);
            }
            return path;
        }

        static void WriteRows(
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in FootIkCapture ik)
        {
            int rowCount = Math.Max(
                1,
                Math.Max(
                    foot.GroundPath.ContactCount,
                    foot.GroundPath.EnvelopeVertexCount));
            for (int contactIndex = 0; contactIndex < rowCount; contactIndex++)
                WriteRow(writer, row, in frame, in foot, in ik, contactIndex);
        }

        static void WriteRow(
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in FootIkCapture ik,
            int groundContactIndex)
        {
            row.Clear();
            CharacterFootLandingPredictionInputDiagnostics input = frame.Input;
            CharacterFootPlacementQueryRequest query = foot.Query;
            Add(row, frame.FrameSequence);
            Add(row, frame.CompletionIdentity);
            Add(row, frame.RootInstanceId);
            Add(row, foot.Side.ToString());
            Add(row, foot.State.ToString());
            Add(row, foot.RejectReason.ToString());
            Add(row, foot.StepSource.ToString());
            Add(row, foot.LandingEventIdentity);
            Add(row, foot.TrajectoryGeneration);
            Add(row, foot.LandingConfidence);
            Add(row, foot.TimeToLandingSeconds);
            Add(row, foot.RootLocalLanding);
            Add(row, input.PresentationDeltaSeconds);
            Add(row, input.PreviousBodyTick);
            Add(row, input.CurrentBodyTick);
            Add(row, input.BodySampleAlpha);
            Add(row, input.BodySampleAgeSeconds);
            Add(row, input.MotionTimelineAvailable);
            Add(row, input.TimelineGeneration);
            Add(row, input.TimelineAuthorityTick);
            Add(row, input.TimelineTickRate);
            Add(row, input.TimelineCurrentVelocityX);
            Add(row, input.TimelineCurrentVelocityZ);
            Add(row, input.TimelineContinuationVelocityX);
            Add(row, input.TimelineContinuationVelocityZ);
            Add(row, input.TimelineHasContinuation);
            Add(row, input.TimelineBodyYawVelocityDegreesPerSecond);
            Add(row, input.TimelineMaximumBodyYawVelocityDegreesPerSecond);
            Add(row, input.CurrentSegmentRemainingSeconds);
            Add(row, input.Grounded);
            Add(row, input.HorizontalSpeed);
            Add(row, input.LeftActionInstanceIdentity);
            Add(row, input.LeftActionFootWeight);
            Add(row, input.RightActionInstanceIdentity);
            Add(row, input.RightActionFootWeight);
            Add(row, input.VisibleBodyPosition);
            Add(row, input.VisibleBodyRotation);
            Add(row, input.VisibleBodyVelocity);
            Add(row, input.VisibleBodyYawVelocityDegreesPerSecond);
            Add(row, input.TargetBodyPosition);
            Add(row, input.TargetBodyRotation);
            Add(row, input.TargetBodyVelocity);
            Add(row, input.TargetBodyYawVelocityDegreesPerSecond);
            Add(row, input.BodyPositionError);
            Add(row, input.BodyRotationError);
            Add(row, input.CorrectionPositionError);
            Add(row, input.CorrectionPositionVelocity);
            Add(row, input.CorrectionYawVelocityDegreesPerSecond);
            Add(row, input.CorrectionActive);
            Add(row, input.CorrectionClamped);
            Add(row, input.CorrectionSettled);
            Add(row, input.BodyResetSequence);
            Add(row, foot.FutureBodyTranslationAvailable);
            Add(row, foot.FutureBodyRelativeTranslation);
            Add(row, foot.FutureBodyTranslationVelocity);
            Add(row, foot.CurrentAnimatedSole);
            Add(row, foot.RawLandingCandidate);
            Add(row, query.Shape.ToString());
            Add(row, query.Purpose.ToString());
            Add(row, query.FootIndex);
            Add(row, query.Origin);
            Add(row, query.Direction);
            Add(row, query.MaximumDistance);
            Add(row, query.Radius);
            Add(row, query.LayerMask);
            Add(row, query.MinimumGroundNormalDot);
            Add(row, foot.Accepted);
            Add(row, foot.SurfaceIdentity);
            Add(row, foot.LandingPoint);
            Add(row, foot.LandingNormal);
            Add(row, foot.QueryDistance);
            CharacterFootGroundPathDiagnostics ground = foot.GroundPath;
            CharacterFootGroundPathQueryRequest groundQuery = ground.Query;
            Add(row, ground.State.ToString());
            Add(row, ground.RejectReason.ToString());
            Add(row, ground.InputIdentity);
            Add(row, ground.QueryExecuted);
            Add(row, ground.LastLandingEventIdentity);
            Add(row, ground.NextSwingLandingEventIdentity);
            Add(row, ground.TrajectoryGeneration);
            Add(row, ground.AuthorityTick);
            Add(row, ground.LastFutureBodyTranslationSourceIdentity);
            Add(row, ground.NextSwingFutureBodyTranslationSourceIdentity);
            Add(row, ground.LastLanding);
            Add(row, ground.NextSwingLanding);
            Add(row, ground.LastLandingNormal);
            Add(row, ground.NextSwingLandingNormal);
            Add(row, ground.LastLandingSurfaceIdentity);
            Add(row, ground.NextSwingLandingSurfaceIdentity);
            Add(row, ground.ComponentUp);
            Add(row, groundQuery.AxisStart);
            Add(row, groundQuery.AxisEnd);
            Add(row, groundQuery.Radius);
            Add(row, groundQuery.MaximumAxisSegmentLength);
            Add(row, groundQuery.Direction);
            Add(row, groundQuery.MaximumDistance);
            Add(row, groundQuery.LayerMask);
            Add(row, groundQuery.SegmentHitCapacity);
            Add(row, groundQuery.ContactCapacity);
            Add(row, ground.SegmentCount);
            Add(row, ground.ContactCount);
            Add(row, ground.EdgeCount);
            Add(row, ground.HasInvalidSegment);
            Add(row, ground.FirstInvalidSegmentIndex);
            Add(row, ground.FirstInvalidSegmentIdentity);
            Add(row, ground.FirstInvalidSegmentBottom);
            Add(row, ground.FirstInvalidSegmentTop);
            Add(row, ground.FirstInvalidSegmentVerticalDistance);
            Add(row, ground.MaximumReachableVerticalEdge);
            Add(row, ground.EnvelopeVertexCount);
            CharacterFootSwingMotionDiagnostics motion = foot.FootMotion;
            Add(row, motion.State.ToString());
            Add(row, motion.RejectReason.ToString());
            Add(row, motion.LandingEventIdentity);
            Add(row, motion.GroundPathInputIdentity);
            Add(row, motion.Distance);
            Add(row, motion.Progress);
            Add(row, motion.OriginalSole);
            Add(row, motion.OriginalAnkle);
            Add(row, motion.BaselineSample);
            Add(row, motion.EnvelopeSample);
            Add(row, motion.VerticalCorrection);
            Add(row, motion.LandingPredictionError);
            Add(row, motion.LandingConstraintWeight);
            Add(row, motion.CorrectedSole);
            Add(row, motion.CorrectedAnkle);
            Add(row, motion.PositionWeight);
            Add(row, motion.RotationWeight);
            Add(row, motion.SupportLockState.ToString());
            Add(row, motion.SupportHorizontalError);
            Add(row, motion.SupportUnlockRemainingSeconds);
            Add(row, motion.SupportUnlockCorrection);
            Add(row, foot.Goal.ComponentPosition);
            Add(row, foot.Goal.ComponentRotation);
            Add(row, foot.Goal.PositionWeight);
            Add(row, foot.Goal.RotationWeight);
            Add(row, frame.PelvisGoal.PositionWeight);
            Add(row, frame.PelvisGoal.RotationWeight);
            CharacterFootStrideHipsDiagnostics stride = frame.StrideHips;
            Add(row, stride.State.ToString());
            Add(row, stride.RejectReason.ToString());
            Add(row, stride.SupportSide.ToString());
            Add(row, stride.SwingSide.ToString());
            Add(row, stride.Progress);
            Add(row, stride.Slope.ToString());
            Add(row, stride.StrideStart);
            Add(row, stride.StrideEnd);
            Add(row, stride.AnimatedPelvis);
            Add(row, stride.AnimatedPelvisComponentPosition);
            Add(row, stride.RawPelvisDelta);
            Add(row, stride.RawPelvisTargetAlongUp);
            Add(row, stride.ClearanceCorrectionAlongUp);
            Add(row, stride.HadPreviousState);
            Add(row, stride.SupportChanged);
            Add(row, stride.PreviousStrideStart);
            Add(row, stride.RebaseAlongUp);
            Add(row, stride.PreviousRawPelvisTargetAlongUp);
            Add(row, stride.RebasedPreviousRawPelvisTargetAlongUp);
            Add(row, stride.PreviousSpringOutput);
            Add(row, stride.RebasedPreviousSpringOutput);
            Add(row, stride.NecessaryDelta);
            Add(row, stride.SpringInput);
            Add(row, stride.SpringTarget);
            Add(row, stride.SpringOutput);
            Add(row, stride.SpringVelocity);
            Add(row, stride.SpringDelta);
            Add(row, stride.PelvisDelta);
            Add(row, stride.PositionWeight);
            Add(row, frame.PelvisGoal.ComponentPosition);
            Add(row, ik.PhysicalPelvisComponentPosition);
            Vector3 expectedPhysicalPelvis = stride.AnimatedPelvisComponentPosition +
                frame.PelvisGoal.ComponentPosition * frame.PelvisGoal.PositionWeight;
            Add(
                row,
                ik.PhysicalWriteAvailable && frame.PelvisGoal.PositionWeight > 0f
                    ? Vector3.Distance(
                        ik.PhysicalPelvisComponentPosition,
                        expectedPhysicalPelvis)
                    : 0f);
            CharacterFullBodyIkSolverDiagnostics solver = ik.Solver;
            CharacterFullBodyIkEffectorDiagnostics effector = ik.Effector;
            Add(row, ik.SolverAvailable);
            Add(row, solver.Succeeded);
            Add(row, solver.FrameSequence);
            Add(row, solver.InputCompletionIdentity);
            Add(row, solver.OutputCompletionIdentity);
            Add(row, solver.BackendIdentity);
            Add(row, solver.RigId);
            Add(row, solver.RigRevision);
            Add(row, solver.ProfileId);
            Add(row, solver.ProfileRevision);
            Add(row, solver.Failure.ToString());
            Add(row, solver.AppliedGoalCount);
            Add(row, ik.EffectorAvailable);
            Add(row, effector.Slot.ToString());
            Add(row, effector.TargetComponentPosition);
            Add(row, effector.SolvedComponentPosition);
            Add(row, effector.PositionResidual);
            Add(row, effector.RotationResidualDegrees);
            CharacterFullBodyIkEffectorDiagnostics pelvis = ik.Pelvis;
            Add(row, ik.PelvisAvailable);
            Add(row, pelvis.TargetComponentPosition);
            Add(row, pelvis.SolvedComponentPosition);
            Add(row, pelvis.PositionResidual);
            Add(row, pelvis.RotationResidualDegrees);
            Add(row, ik.PhysicalWriteAvailable);
            Add(row, ik.PhysicalWriteCompletionIdentity);
            Add(row, ik.PhysicalAnkleComponentPosition);
            Add(
                row,
                ik.PhysicalWriteAvailable
                    ? Vector3.Distance(
                        ik.PhysicalAnkleComponentPosition,
                        foot.Goal.ComponentPosition)
                    : 0f);
            bool hasContact = groundContactIndex < ground.ContactCount;
            CharacterFootGroundContact contact = hasContact
                ? ground.ContactAt(groundContactIndex)
                : default;
            Add(row, hasContact ? groundContactIndex : -1);
            Add(row, hasContact ? contact.SegmentIndex : -1);
            Add(row, contact.SurfaceIdentity);
            Add(row, contact.CandidateIdentity);
            Add(row, contact.Position);
            Add(row, contact.Normal);
            Add(row, contact.QueryDistance);
            bool hasEnvelopeVertex = groundContactIndex < ground.EnvelopeVertexCount;
            CharacterFootGroundEnvelopeVertex envelopeVertex = hasEnvelopeVertex
                ? ground.EnvelopeVertexAt(groundContactIndex)
                : default;
            Add(row, hasEnvelopeVertex ? groundContactIndex : -1);
            Add(row, envelopeVertex.Position);
            writer.WriteLine(row);
        }

        static void Add(StringBuilder row, string value)
        {
            Separate(row);
            row.Append(value);
        }

        static void Add(StringBuilder row, bool value) => Add(row, value ? 1 : 0);

        static void Add(StringBuilder row, int value)
        {
            Separate(row);
            row.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        static void Add(StringBuilder row, ulong value)
        {
            Separate(row);
            row.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        static void Add(StringBuilder row, float value)
        {
            Separate(row);
            row.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void Add(StringBuilder row, Vector3 value)
        {
            Add(row, value.x);
            Add(row, value.y);
            Add(row, value.z);
        }

        static void Add(StringBuilder row, Quaternion value)
        {
            Add(row, value.x);
            Add(row, value.y);
            Add(row, value.z);
            Add(row, value.w);
        }

        static void Separate(StringBuilder row)
        {
            if (row.Length > 0)
                row.Append(',');
        }
    }
}
