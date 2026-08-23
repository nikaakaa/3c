using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    public static class CharacterFootLandingPredictionSampler
    {
        const int MaximumPendingFrameCount = 256;
        const double SamplingStartTimeoutSeconds = 30d;
        const string GameplayLabPlayerActorId = "gameplay-lab-player";
        const string StartMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Start";
        const string StopMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Stop and Save";
        const string Header =
            "FrameSequence,CompletionIdentity,TargetRuntimeInstanceId,TargetHostInstanceId,RootInstanceId,Side,State,RejectReason,StepSource," +
            "LandingEventIdentity,TrajectoryGeneration,LandingConfidence,TimeToLandingSeconds," +
            "RootLocalLandingX,RootLocalLandingY,RootLocalLandingZ," +
            "PresentationDeltaSeconds,PreviousBodyTick,CurrentBodyTick,BodySampleAlpha,BodySampleAgeSeconds," +
            "MotionTimelineAvailable,TimelineGeneration,TimelineAuthorityTick,TimelineTickRate," +
            "TimelineCurrentVelocityX,TimelineCurrentVelocityZ,TimelineContinuationVelocityX,TimelineContinuationVelocityZ," +
            "TimelineHasContinuation,TimelineBodyYawVelocityDegreesPerSecond,TimelineMaximumBodyYawVelocityDegreesPerSecond,CurrentSegmentRemainingSeconds," +
            "Grounded,HorizontalSpeed,LeftActionInstanceIdentity,LeftActionFootWeight,RightActionInstanceIdentity,RightActionFootWeight," +
            "PrimarySupportHasValue,PrimarySupportSide,PrimarySupportLandingEventIdentity,PrimarySupportRetained," +
            "LogicRootPositionX,LogicRootPositionY,LogicRootPositionZ,LogicRootRotationX,LogicRootRotationY,LogicRootRotationZ,LogicRootRotationW," +
            "VisualRootLocalPositionX,VisualRootLocalPositionY,VisualRootLocalPositionZ,VisualRootLocalRotationX,VisualRootLocalRotationY,VisualRootLocalRotationZ,VisualRootLocalRotationW," +
            "VisualRootWorldPositionX,VisualRootWorldPositionY,VisualRootWorldPositionZ,VisualRootWorldRotationX,VisualRootWorldRotationY,VisualRootWorldRotationZ,VisualRootWorldRotationW," +
            "PoseRootLocalPositionX,PoseRootLocalPositionY,PoseRootLocalPositionZ,PoseRootLocalRotationX,PoseRootLocalRotationY,PoseRootLocalRotationZ,PoseRootLocalRotationW," +
            "PoseRootWorldPositionX,PoseRootWorldPositionY,PoseRootWorldPositionZ,PoseRootWorldRotationX,PoseRootWorldRotationY,PoseRootWorldRotationZ,PoseRootWorldRotationW," +
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
            "QueryDirectionX,QueryDirectionY,QueryDirectionZ,QueryMaximumDistance,QueryRadius,QueryLayerMask,QueryMinimumGroundNormalDot,QueryPreferredSurfaceIdentity," +
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
            "FootMotionLandingPredictionError,FootMotionLandingConstraintWeight,FootMotionPlantConfidence," +
            "FootMotionCorrectedSoleX,FootMotionCorrectedSoleY,FootMotionCorrectedSoleZ," +
            "FootMotionCorrectedAnkleX,FootMotionCorrectedAnkleY,FootMotionCorrectedAnkleZ,FootMotionPositionWeight,FootMotionRotationWeight," +
            "FootMotionSupportLockState,FootMotionSupportHorizontalError," +
            "FootMotionSupportConstraintWeight,FootMotionSupportWeight," +
            "FootMotionSupportContactAnchorX,FootMotionSupportContactAnchorY,FootMotionSupportContactAnchorZ," +
            "GoalTransitionMode,GoalTransitionHasCommittedOutput,GoalTransitionHasPendingOutput," +
            "GoalTransitionCommittedSourceGroundPathIdentity,GoalTransitionPendingSourceGroundPathIdentity," +
            "RawGoalPositionCorrectionX,RawGoalPositionCorrectionY,RawGoalPositionCorrectionZ," +
            "RawGoalRotationCorrectionX,RawGoalRotationCorrectionY,RawGoalRotationCorrectionZ,RawGoalRotationCorrectionW," +
            "RawGoalPositionWeight,RawGoalRotationWeight," +
            "CommittedGoalTransitionPositionCorrectionX,CommittedGoalTransitionPositionCorrectionY,CommittedGoalTransitionPositionCorrectionZ," +
            "CommittedGoalTransitionRotationCorrectionX,CommittedGoalTransitionRotationCorrectionY,CommittedGoalTransitionRotationCorrectionZ,CommittedGoalTransitionRotationCorrectionW," +
            "CommittedGoalTransitionPositionWeight,CommittedGoalTransitionRotationWeight," +
            "PendingGoalTransitionPositionCorrectionX,PendingGoalTransitionPositionCorrectionY,PendingGoalTransitionPositionCorrectionZ," +
            "PendingGoalTransitionRotationCorrectionX,PendingGoalTransitionRotationCorrectionY,PendingGoalTransitionRotationCorrectionZ,PendingGoalTransitionRotationCorrectionW," +
            "PendingGoalTransitionPositionWeight,PendingGoalTransitionRotationWeight,GoalTransitionHalfLifeSeconds," +
            "FinalGoalPositionX,FinalGoalPositionY,FinalGoalPositionZ,FinalGoalRotationX,FinalGoalRotationY,FinalGoalRotationZ,FinalGoalRotationW,FinalGoalPositionWeight,FinalGoalRotationWeight,PelvisPositionWeight,PelvisRotationWeight," +
            "StrideState,StrideRejectReason,StrideSupportSide,StrideSwingSide,StrideProgress,StrideSlope," +
            "StrideStartX,StrideStartY,StrideStartZ,StrideEndX,StrideEndY,StrideEndZ," +
            "StrideSampledGroundX,StrideSampledGroundY,StrideSampledGroundZ," +
            "StridePoseRootPositionX,StridePoseRootPositionY,StridePoseRootPositionZ," +
            "StrideAnimatedPelvisX,StrideAnimatedPelvisY,StrideAnimatedPelvisZ," +
            "StrideAnimatedPelvisComponentPositionX,StrideAnimatedPelvisComponentPositionY,StrideAnimatedPelvisComponentPositionZ," +
            "StrideRawPelvisDeltaX,StrideRawPelvisDeltaY,StrideRawPelvisDeltaZ," +
            "StrideRootRelativeGroundTargetAlongUp,StrideSoleClearanceLiftAlongUp,StrideHadPreviousState,StrideSupportChanged," +
            "StridePreviousSlope,StrideSpringHandoffReason,StrideSpringVelocityReset," +
            "StridePreviousSpringTarget,StridePreviousSpringOutput,StridePreviousSpringVelocity,StrideSpringInput,StrideSpringInputVelocity,StrideSpringFrequency," +
            "StrideUnclampedSpringTarget,StrideSupportReachAvailable,StrideSupportLegCompressionReserve,StrideSupportReachUsableLegLength,StrideSupportReachMinimumAlongUp,StrideSupportReachMaximumAlongUp," +
            "StrideSupportReachTargetClamped,StrideSupportReachOutputClamped," +
            "StrideSpringTarget,StrideSpringOutput,StrideSpringVelocity," +
            "StridePelvisDeltaX,StridePelvisDeltaY,StridePelvisDeltaZ,StridePositionWeight," +
            "FinalPelvisGoalX,FinalPelvisGoalY,FinalPelvisGoalZ," +
            "FinalPhysicalPelvisComponentPositionX,FinalPhysicalPelvisComponentPositionY,FinalPhysicalPelvisComponentPositionZ,FinalPhysicalPelvisGoalResidual," +
            "FinalIkSolverAvailable,FinalIkSucceeded,FinalIkFrameSequence,FinalIkInputCompletionIdentity,FinalIkOutputCompletionIdentity," +
            "FinalIkBackendIdentity,FinalIkRigId,FinalIkRigRevision,FinalIkProfileId,FinalIkProfileRevision,FinalIkFailure,FinalIkAppliedGoalCount," +
            "FinalIkEffectorAvailable,FinalIkEffectorSlot,FinalIkTargetPositionX,FinalIkTargetPositionY,FinalIkTargetPositionZ," +
            "FinalIkSolvedPositionX,FinalIkSolvedPositionY,FinalIkSolvedPositionZ,FinalIkPositionResidual,FinalIkRotationResidualDegrees," +
            "FinalIkLegAvailable,FinalIkLegSlot,FinalIkLegBendWeight,FinalIkLegStabilizationWeight,FinalIkLegRetainedPreviousBendDirection," +
            "FinalIkLegOriginalHipX,FinalIkLegOriginalHipY,FinalIkLegOriginalHipZ," +
            "FinalIkLegOriginalKneeX,FinalIkLegOriginalKneeY,FinalIkLegOriginalKneeZ," +
            "FinalIkLegOriginalAnkleX,FinalIkLegOriginalAnkleY,FinalIkLegOriginalAnkleZ," +
            "FinalIkLegTargetAnkleX,FinalIkLegTargetAnkleY,FinalIkLegTargetAnkleZ," +
            "FinalIkLegSolvedHipX,FinalIkLegSolvedHipY,FinalIkLegSolvedHipZ," +
            "FinalIkLegSolvedKneeX,FinalIkLegSolvedKneeY,FinalIkLegSolvedKneeZ," +
            "FinalIkLegSolvedAnkleX,FinalIkLegSolvedAnkleY,FinalIkLegSolvedAnkleZ," +
            "FinalIkLegOriginalBendDegrees,FinalIkLegSolvedBendDegrees," +
            "FinalIkLegOriginalExtensionRatio,FinalIkLegTargetExtensionRatio,FinalIkLegSolvedExtensionRatio," +
            "FinalIkLegOriginalCompressionReserve,FinalIkLegTargetCompressionReserve,FinalIkLegSolvedCompressionReserve," +
            "FinalIkLegEffectiveBendDirectionX,FinalIkLegEffectiveBendDirectionY,FinalIkLegEffectiveBendDirectionZ," +
            "FinalIkLegAnimatedBendDirectionPreviousDot,FinalIkLegEffectiveBendDirectionPreviousDot," +
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
                CharacterFullBodyIkLimbDiagnostics limb,
                bool physicalWriteAvailable,
                ulong physicalWriteCompletionIdentity,
                Vector3 physicalAnkleComponentPosition,
                Vector3 physicalPelvisComponentPosition)
            {
                Solver = solver;
                Pelvis = pelvis;
                Effector = effector;
                Limb = limb;
                PhysicalWriteAvailable = physicalWriteAvailable;
                PhysicalWriteCompletionIdentity = physicalWriteCompletionIdentity;
                PhysicalAnkleComponentPosition = physicalAnkleComponentPosition;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
            }

            internal CharacterFullBodyIkSolverDiagnostics Solver { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Pelvis { get; }
            internal CharacterFullBodyIkEffectorDiagnostics Effector { get; }
            internal CharacterFullBodyIkLimbDiagnostics Limb { get; }
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

        readonly struct RootHierarchyCapture
        {
            internal RootHierarchyCapture(CharacterRootHierarchyBinding binding)
            {
                if (!binding)
                    throw new ArgumentNullException(nameof(binding));
                LogicRootPosition = binding.LogicRoot.position;
                LogicRootRotation = binding.LogicRoot.rotation;
                VisualRootLocalPosition = binding.VisualRoot.localPosition;
                VisualRootLocalRotation = binding.VisualRoot.localRotation;
                VisualRootWorldPosition = binding.VisualRoot.position;
                VisualRootWorldRotation = binding.VisualRoot.rotation;
                PoseRootLocalPosition = binding.PoseRoot.localPosition;
                PoseRootLocalRotation = binding.PoseRoot.localRotation;
                PoseRootWorldPosition = binding.PoseRoot.position;
                PoseRootWorldRotation = binding.PoseRoot.rotation;
            }

            internal Vector3 LogicRootPosition { get; }
            internal Quaternion LogicRootRotation { get; }
            internal Vector3 VisualRootLocalPosition { get; }
            internal Quaternion VisualRootLocalRotation { get; }
            internal Vector3 VisualRootWorldPosition { get; }
            internal Quaternion VisualRootWorldRotation { get; }
            internal Vector3 PoseRootLocalPosition { get; }
            internal Quaternion PoseRootLocalRotation { get; }
            internal Vector3 PoseRootWorldPosition { get; }
            internal Quaternion PoseRootWorldRotation { get; }
        }

        sealed class CapturedFrame
        {
            internal CapturedFrame(
                in CharacterFootLandingPredictionDiagnostics foot,
                FootIkCapture left,
                FootIkCapture right,
                Vector3 physicalPelvisComponentPosition,
                RootHierarchyCapture roots,
                Guid targetRuntimeInstanceId,
                int targetHostInstanceId)
            {
                Foot = foot;
                Left = left;
                Right = right;
                PhysicalPelvisComponentPosition = physicalPelvisComponentPosition;
                Roots = roots;
                TargetRuntimeInstanceId = targetRuntimeInstanceId;
                TargetHostInstanceId = targetHostInstanceId;
            }

            internal CharacterFootLandingPredictionDiagnostics Foot { get; }
            internal FootIkCapture Left { get; }
            internal FootIkCapture Right { get; }
            internal Vector3 PhysicalPelvisComponentPosition { get; }
            internal RootHierarchyCapture Roots { get; }
            internal Guid TargetRuntimeInstanceId { get; }
            internal int TargetHostInstanceId { get; }
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
        static bool s_StartPending;
        static double s_StartDeadline;
        static string s_LastStartFailure = string.Empty;
        static string s_StartWaitReason = string.Empty;
        static string s_LastSavedPath = string.Empty;
        static int s_DroppedPendingFrameCount;
        static int s_LastSavedFrameCount;
        static int s_TargetHostInstanceId;
        static int s_TargetRootInstanceId;
        static CharacterRootHierarchyBinding s_TargetRootHierarchy;

        public static bool IsCapturing => s_Capturing;
        public static bool IsStartPending => s_StartPending;
        public static string LastStartFailure => s_LastStartFailure;
        public static string LastSavedPath => s_LastSavedPath;
        public static int CapturedFrameCount => s_Frames.Count;
        public static int PendingFrameCount => s_PendingFrames.Count;
        public static int DroppedPendingFrameCount => s_DroppedPendingFrameCount;
        public static int LastSavedFrameCount => s_LastSavedFrameCount;

        static CharacterFootLandingPredictionSampler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            s_LastSavedPath = FindLatestSavedPath();
        }

        public static void StartSampling()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Foot Landing sampling can only start in Play Mode.");
            if (s_Capturing)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already active.");
            if (s_StartPending)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already waiting for the Gameplay Lab player.");
            s_Frames.Clear();
            s_PendingFrames.Clear();
            s_DroppedPendingFrameCount = 0;
            s_LastSavedFrameCount = 0;
            s_LastStartFailure = string.Empty;
            s_StartWaitReason = string.Empty;
            s_StartPending = true;
            s_StartDeadline = EditorApplication.timeSinceStartup + SamplingStartTimeoutSeconds;
            EditorApplication.update -= PollSamplingStart;
            EditorApplication.update += PollSamplingStart;
            PollSamplingStart();
        }

        static void PollSamplingStart()
        {
            if (!s_StartPending)
            {
                EditorApplication.update -= PollSamplingStart;
                return;
            }
            if (!EditorApplication.isPlaying)
            {
                FailSamplingStart("Gameplay Lab left Play Mode before the player host became available.");
                return;
            }
            try
            {
                if (TryCompleteSamplingStart())
                    return;
            }
            catch (Exception exception)
            {
                FailSamplingStart(exception.Message);
                Debug.LogException(exception);
                return;
            }
            if (EditorApplication.timeSinceStartup >= s_StartDeadline)
                FailSamplingStart(s_StartWaitReason);
        }

        static bool TryCompleteSamplingStart()
        {
            if (!TryBindGameplayLabPlayerTarget())
            {
                s_StartWaitReason = "Gameplay Lab player host did not become available before sampling timed out.";
                return false;
            }
            if (!HasGameplayLabPlayerRuntimeTarget())
            {
                s_StartWaitReason = "Gameplay Lab player Animation Presentation target did not become available before sampling timed out.";
                return false;
            }
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
            s_StartPending = false;
            s_StartDeadline = 0d;
            s_StartWaitReason = string.Empty;
            EditorApplication.update -= PollSamplingStart;
            Debug.Log("Foot Landing sampling started.");
            return true;
        }

        [MenuItem(StartMenu)]
        static void StartFromMenu() => StartSampling();

        [MenuItem(StartMenu, true)]
        static bool CanStart() =>
            EditorApplication.isPlaying && !s_Capturing && !s_StartPending;

        [MenuItem(StopMenu)]
        static void Stop() => StopAndSave();

        [MenuItem(StopMenu, true)]
        static bool CanStop() => s_Capturing || s_StartPending;

        public static void StopAndSaveSampling() => StopAndSave();

        static void Capture(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (!s_Capturing)
                return;
            if (diagnostics.RootInstanceId != s_TargetRootInstanceId)
                return;
            if (s_PendingFrames.Count >= MaximumPendingFrameCount)
            {
                s_PendingFrames.RemoveAt(0);
                s_DroppedPendingFrameCount++;
            }
            s_PendingFrames.Add(new PendingFrame(in diagnostics));
        }

        static void ProcessPendingFrames()
        {
            if (!s_Capturing)
                return;
            ConfigureTargets();
            if (s_PendingFrames.Count == 0)
                return;
            for (int pendingIndex = 0; pendingIndex < s_PendingFrames.Count;)
            {
                PendingFrame pending = s_PendingFrames[pendingIndex];
                CharacterFootLandingPredictionDiagnostics pendingDiagnostics = pending.Diagnostics;
                PendingFrameResolution resolution = TryCaptureCommittedIk(
                    in pendingDiagnostics,
                    out CapturedFrame captured);
                if (resolution == PendingFrameResolution.Waiting)
                {
                    pendingIndex++;
                    continue;
                }
                if (resolution == PendingFrameResolution.Captured)
                    s_Frames.Add(captured);
                else
                    s_DroppedPendingFrameCount++;
                s_PendingFrames.RemoveAt(pendingIndex);
            }
        }

        enum PendingFrameResolution : byte
        {
            Waiting,
            Captured,
            Stale
        }

        static PendingFrameResolution TryCaptureCommittedIk(
            in CharacterFootLandingPredictionDiagnostics pending,
            out CapturedFrame captured)
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
            {
                AnimationPresentationRuntimeTarget target = targets[targetIndex];
                if (target.HostInstanceId != s_TargetHostInstanceId)
                    continue;
                if (!target.TryGetDebugView(out AnimationPresentationDebugView debugView))
                    continue;
                AnimationFootPlacementRuntimeSnapshot placement = debugView.PosePlan.FootPlacement;
                if (!placement.IsAvailable ||
                    placement.LandingPrediction.RootInstanceId != pending.RootInstanceId)
                {
                    continue;
                }
                if (placement.LandingPrediction.FrameSequence > pending.FrameSequence)
                {
                    captured = default;
                    return PendingFrameResolution.Stale;
                }
                if (placement.LandingPrediction.FrameSequence != pending.FrameSequence ||
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
                        placement.LeftLeg,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.LeftPhysicalAnkleComponentPosition,
                        placement.PhysicalPelvisComponentPosition),
                    new FootIkCapture(
                        placement.Solver,
                        placement.Pelvis,
                        placement.RightFoot,
                        placement.RightLeg,
                        placement.PhysicalWriteAvailable,
                        placement.PhysicalWriteCompletionIdentity,
                        placement.RightPhysicalAnkleComponentPosition,
                        placement.PhysicalPelvisComponentPosition),
                    placement.PhysicalPelvisComponentPosition,
                    new RootHierarchyCapture(s_TargetRootHierarchy),
                    target.RuntimeInstanceId,
                    target.HostInstanceId);
                return PendingFrameResolution.Captured;
            }
            captured = default;
            return PendingFrameResolution.Waiting;
        }

        static void ConfigureTargets()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            bool configured = false;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].HostInstanceId != s_TargetHostInstanceId)
                    continue;
                ConfigureTarget(targets[i]);
                configured = true;
            }
            if (!configured)
                throw new InvalidOperationException(
                    "Gameplay Lab player Animation Presentation target is unavailable.");
        }

        static void ConfigureTarget(AnimationPresentationRuntimeTarget target)
        {
            if (!s_Capturing || target == null ||
                target.HostInstanceId != s_TargetHostInstanceId)
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
            AnimationFootPlacementRuntimeSnapshot footPlacement = debugView.PosePlan.FootPlacement;
            if (footPlacement.IsAvailable &&
                footPlacement.LandingPrediction.RootInstanceId != 0)
            {
                int rootInstanceId = footPlacement.LandingPrediction.RootInstanceId;
                if (s_TargetRootInstanceId != 0 && s_TargetRootInstanceId != rootInstanceId)
                {
                    throw new InvalidOperationException(
                        "Gameplay Lab player Animation Presentation root changed after sampling target binding.");
                }
                s_TargetRootInstanceId = rootInstanceId;
            }
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

        static bool TryBindGameplayLabPlayerTarget()
        {
            int selectedHostInstanceId = 0;
            int selectedRootInstanceId = 0;
            CharacterRootHierarchyBinding selectedRootHierarchy = null;
            CharacterPipelineHost[] hosts = UnityEngine.Object.FindObjectsByType<CharacterPipelineHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < hosts.Length; i++)
            {
                CharacterPipelineHost host = hosts[i];
                if (host == null || !host.VisualRoot ||
                    !string.Equals(host.ActorId, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                if (selectedHostInstanceId != 0)
                    throw new InvalidOperationException(
                        "Gameplay Lab contains multiple gameplay-lab-player hosts.");
                selectedHostInstanceId = host.GetInstanceID();
                selectedRootInstanceId = host.VisualRoot.GetInstanceID();
                selectedRootHierarchy = host.RootHierarchy;
            }
            FixedCharacterHost[] fixedHosts = UnityEngine.Object.FindObjectsByType<FixedCharacterHost>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < fixedHosts.Length; i++)
            {
                FixedCharacterHost host = fixedHosts[i];
                if (host == null || !host.RootHierarchy ||
                    !string.Equals(host.ActorId.Value, GameplayLabPlayerActorId, StringComparison.Ordinal))
                    continue;
                if (selectedHostInstanceId != 0)
                    throw new InvalidOperationException(
                        "Gameplay Lab contains multiple gameplay-lab-player hosts.");
                selectedHostInstanceId = host.GetInstanceID();
                selectedRootInstanceId = host.RootHierarchy.VisualRoot.GetInstanceID();
                selectedRootHierarchy = host.RootHierarchy;
            }
            if (selectedHostInstanceId == 0)
            {
                ResetTargetBinding();
                return false;
            }
            s_TargetHostInstanceId = selectedHostInstanceId;
            s_TargetRootInstanceId = selectedRootInstanceId;
            s_TargetRootHierarchy = selectedRootHierarchy;
            return true;
        }

        static bool HasGameplayLabPlayerRuntimeTarget()
        {
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].HostInstanceId == s_TargetHostInstanceId)
                    return true;
            }
            return false;
        }

        static void ResetTargetBinding()
        {
            s_TargetHostInstanceId = 0;
            s_TargetRootInstanceId = 0;
            s_TargetRootHierarchy = null;
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
            EditorApplication.update -= PollSamplingStart;
            CharacterFootLandingPredictionDebugRegistry.Published -= Capture;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered -= ConfigureTarget;
            AnimationPresentationRuntimeTargetRegistry.TargetUnregistered -= RemoveTarget;
            EditorApplication.update -= ProcessPendingFrames;
            RemoveTargetDiagnostics();
            s_Capturing = false;
            s_StartPending = false;
            s_StartDeadline = 0d;
            s_StartWaitReason = string.Empty;
            s_Frames.Clear();
            s_PendingFrames.Clear();
            ResetTargetBinding();
        }

        static string StopAndSave()
        {
            if (s_StartPending)
            {
                CancelSamplingStart();
                return s_LastSavedPath;
            }
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
                s_LastSavedFrameCount = s_Frames.Count;
                s_LastSavedPath = Save();
                Debug.Log(
                    $"Foot Landing sampling saved {s_LastSavedFrameCount} frames " +
                    $"with {s_DroppedPendingFrameCount} dropped pending frames to {s_LastSavedPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                s_Frames.Clear();
                s_PendingFrames.Clear();
                ResetTargetBinding();
            }
            return s_LastSavedPath;
        }

        static void FailSamplingStart(string message)
        {
            s_LastStartFailure = string.IsNullOrWhiteSpace(message)
                ? "Foot Landing sampling could not bind the Gameplay Lab player."
                : message;
            CancelSamplingStart();
            Debug.LogError(s_LastStartFailure);
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
            string directory = ResolveSaveDirectory();
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
                RootHierarchyCapture roots = captured.Roots;
                CharacterFootLandingPredictionFootDiagnostics leftFoot = frame.Left;
                CharacterFootLandingPredictionFootDiagnostics rightFoot = frame.Right;
                WriteRows(
                    writer, row, in frame, in leftFoot, in left, in roots,
                    captured.TargetRuntimeInstanceId, captured.TargetHostInstanceId);
                WriteRows(
                    writer, row, in frame, in rightFoot, in right, in roots,
                    captured.TargetRuntimeInstanceId, captured.TargetHostInstanceId);
            }
            return path;
        }

        static string FindLatestSavedPath()
        {
            string directory = ResolveSaveDirectory();
            if (!Directory.Exists(directory))
                return string.Empty;
            string latestPath = string.Empty;
            DateTime latestWriteTime = DateTime.MinValue;
            foreach (string path in Directory.EnumerateFiles(directory, "foot-landing-*.csv"))
            {
                DateTime writeTime = File.GetLastWriteTimeUtc(path);
                if (writeTime <= latestWriteTime)
                    continue;
                latestWriteTime = writeTime;
                latestPath = path;
            }
            return latestPath;
        }

        static string ResolveSaveDirectory() => Path.GetFullPath(Path.Combine(
            Application.dataPath,
            "..",
            "Temp",
            "FootLandingSamples"));

        static void WriteRows(
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in FootIkCapture ik,
            in RootHierarchyCapture roots,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId)
        {
            int rowCount = Math.Max(
                1,
                Math.Max(
                    foot.GroundPath.ContactCount,
                    foot.GroundPath.EnvelopeVertexCount));
            for (int contactIndex = 0; contactIndex < rowCount; contactIndex++)
                WriteRow(
                    writer, row, in frame, in foot, in ik,
                    in roots,
                    targetRuntimeInstanceId, targetHostInstanceId, contactIndex);
        }

        static void WriteRow(
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            in CharacterFootLandingPredictionFootDiagnostics foot,
            in FootIkCapture ik,
            in RootHierarchyCapture roots,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId,
            int groundContactIndex)
        {
            row.Clear();
            CharacterFootLandingPredictionInputDiagnostics input = frame.Input;
            CharacterFootPlacementQueryRequest query = foot.Query;
            Add(row, frame.FrameSequence);
            Add(row, frame.CompletionIdentity);
            Add(row, targetRuntimeInstanceId.ToString("N"));
            Add(row, targetHostInstanceId);
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
            Add(row, frame.PrimarySupport.HasValue);
            Add(row, frame.PrimarySupport.Side.ToString());
            Add(row, frame.PrimarySupport.LandingEventIdentity);
            Add(row, frame.PrimarySupport.Retained);
            Add(row, roots.LogicRootPosition);
            Add(row, roots.LogicRootRotation);
            Add(row, roots.VisualRootLocalPosition);
            Add(row, roots.VisualRootLocalRotation);
            Add(row, roots.VisualRootWorldPosition);
            Add(row, roots.VisualRootWorldRotation);
            Add(row, roots.PoseRootLocalPosition);
            Add(row, roots.PoseRootLocalRotation);
            Add(row, roots.PoseRootWorldPosition);
            Add(row, roots.PoseRootWorldRotation);
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
            Add(row, query.PreferredSurfaceIdentity);
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
            CharacterFullBodyIkGoal footGoal = foot.Goal;
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
            Add(row, motion.PlantConfidence);
            Add(row, motion.CorrectedSole);
            Add(row, motion.CorrectedAnkle);
            Add(row, motion.PositionWeight);
            Add(row, motion.RotationWeight);
            Add(row, motion.SupportLockState.ToString());
            Add(row, motion.SupportHorizontalError);
            Add(row, motion.SupportConstraintWeight);
            Add(row, motion.SupportWeight);
            Add(row, motion.SupportContactAnchor);
            CharacterFootGoalTransitionDiagnostics goalTransition = foot.GoalTransition;
            Add(row, goalTransition.Mode.ToString());
            Add(row, goalTransition.HasCommittedOutput);
            Add(row, goalTransition.HasPendingOutput);
            Add(row, goalTransition.CommittedSourceGroundPathIdentity);
            Add(row, goalTransition.PendingSourceGroundPathIdentity);
            Add(row, goalTransition.RawPositionCorrection);
            Add(row, goalTransition.RawRotationCorrection);
            Add(row, goalTransition.RawPositionWeight);
            Add(row, goalTransition.RawRotationWeight);
            Add(row, goalTransition.CommittedPositionCorrection);
            Add(row, goalTransition.CommittedRotationCorrection);
            Add(row, goalTransition.CommittedPositionWeight);
            Add(row, goalTransition.CommittedRotationWeight);
            Add(row, goalTransition.PendingPositionCorrection);
            Add(row, goalTransition.PendingRotationCorrection);
            Add(row, goalTransition.PendingPositionWeight);
            Add(row, goalTransition.PendingRotationWeight);
            Add(row, goalTransition.HalfLifeSeconds);
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
            Add(row, stride.SampledGround);
            Add(row, stride.PoseRootPosition);
            Add(row, stride.AnimatedPelvis);
            Add(row, stride.AnimatedPelvisComponentPosition);
            Add(row, stride.RawPelvisDelta);
            Add(row, stride.RootRelativeGroundTargetAlongUp);
            Add(row, stride.SoleClearanceLiftAlongUp);
            Add(row, stride.HadPreviousState);
            Add(row, stride.SupportChanged);
            Add(row, stride.PreviousSlope.ToString());
            Add(row, stride.SpringHandoffReason.ToString().Replace(", ", "|"));
            Add(row, stride.SpringVelocityReset);
            Add(row, stride.PreviousSpringTarget);
            Add(row, stride.PreviousSpringOutput);
            Add(row, stride.PreviousSpringVelocity);
            Add(row, stride.SpringInput);
            Add(row, stride.SpringInputVelocity);
            Add(row, stride.SpringFrequency);
            Add(row, stride.UnclampedSpringTarget);
            Add(row, stride.SupportReachAvailable);
            Add(row, stride.SupportLegCompressionReserve);
            Add(row, stride.SupportReachUsableLegLength);
            Add(row, stride.SupportReachMinimumAlongUp);
            Add(row, stride.SupportReachMaximumAlongUp);
            Add(row, stride.SupportReachTargetClamped);
            Add(row, stride.SupportReachOutputClamped);
            Add(row, stride.SpringTarget);
            Add(row, stride.SpringOutput);
            Add(row, stride.SpringVelocity);
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
            CharacterFullBodyIkLimbDiagnostics limb = ik.Limb;
            CharacterFullBodyIkLegPoseDiagnostics legPose = limb.LegPose;
            Add(row, legPose.IsAvailable);
            Add(row, limb.Limb.ToString());
            Add(row, limb.BendWeight);
            Add(row, legPose.StabilizationWeight);
            Add(row, legPose.RetainedPreviousBendDirection);
            Add(row, legPose.OriginalHip);
            Add(row, legPose.OriginalKnee);
            Add(row, legPose.OriginalAnkle);
            Add(row, legPose.TargetAnkle);
            Add(row, legPose.SolvedHip);
            Add(row, legPose.SolvedKnee);
            Add(row, legPose.SolvedAnkle);
            Add(row, legPose.OriginalBendDegrees);
            Add(row, legPose.SolvedBendDegrees);
            Add(row, legPose.OriginalExtensionRatio);
            Add(row, legPose.TargetExtensionRatio);
            Add(row, legPose.SolvedExtensionRatio);
            Add(row, legPose.OriginalCompressionReserve);
            Add(row, legPose.TargetCompressionReserve);
            Add(row, legPose.SolvedCompressionReserve);
            Add(row, legPose.EffectiveBendDirection);
            Add(row, legPose.AnimatedBendDirectionPreviousDot);
            Add(row, legPose.EffectiveBendDirectionPreviousDot);
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
                ik.PhysicalWriteAvailable && foot.Goal.PositionWeight > 0f
                    ? Vector3.Distance(
                        ik.PhysicalAnkleComponentPosition,
                        ResolveWeightedAnkleComponentPosition(
                            in goalTransition,
                            in footGoal))
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

        static Vector3 ResolveWeightedAnkleComponentPosition(
            in CharacterFootGoalTransitionDiagnostics goalTransition,
            in CharacterFullBodyIkGoal goal)
        {
            Vector3 originalComponentPosition = goalTransition.OriginalComponentPosition;
            return originalComponentPosition +
                   (goal.ComponentPosition - originalComponentPosition) *
                   goal.PositionWeight;
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
