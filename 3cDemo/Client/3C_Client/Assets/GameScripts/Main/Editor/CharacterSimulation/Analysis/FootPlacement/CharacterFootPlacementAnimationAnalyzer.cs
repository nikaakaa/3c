using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Pipeline.Presentation;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class CharacterFootPlacementAnimationAnalyzer
    {
        sealed class SampledFoot
        {
            public Vector3[] HeelPositions;
            public Vector3[] SolePositions;
            public Vector3[] ToePositions;
            public Vector3[] AnklePositions;
            public Vector3[] KneePositions;
            public Vector3[] HipPositions;
            public Quaternion[] SoleRotations;
            public Quaternion[] AnkleRotations;
            public float RigLegLength;
            public Vector3[] Velocities;
            public float[] Heights;
            public float[] PlantConfidence;
            public float[] LandingConfidence;
            public float[] LandingDelay;
            public float[] EventPhase;
            public float[] ReleasePhase;
            public float[] LiftOffPhase;
            public float[] ApproachContactPhase;
            public float[] ActionStepDurationSeconds;
            public float[] EventOrdinal;
            public float[] SourceLandingCycleOffset;
            public float[] OpposingLandingDelaySeconds;
            public float[] OpposingEventOrdinal;
            public float[] OpposingLandingCycleOffset;
            public Vector3[] OpposingRootLocalLanding;
            public Quaternion[] OpposingRootLocalLandingRotation;
            public Vector3[][] RootLocalFootRoute;
            public Vector3[][] RootLocalHeelRoute;
            public Vector3[][] RootLocalToeRoute;
            public Vector3[][] RootLocalAnkleRoute;
            public Vector3[][] RootLocalKneeRoute;
            public Vector3[][] RootLocalHipRoute;
            public Quaternion[][] RootLocalSoleRotationRoute;
            public Quaternion[][] RootLocalAnkleRotationRoute;
            public Vector3[][] AuthoredFootPlanarRoute;
            public float[][] AnimationClearanceHeight;
            public float[][] ConstraintWeight;
            public float[][] SupportWeight;
            public float[][] SupportLegLength;
            public float[][] SupportLegCompressionReserve;
            public Vector3[][] SupportKneeBendPlane;
            public Vector3[][] SupportFootPivotPosition;
            public float[][] SupportFootPivotWeight;
        }

        sealed class SamplingContext : IDisposable
        {
            Scene m_PreviewScene;
            GameObject m_Instance;
            CharacterFootPlacementPoseRig m_Binding;
            Animator m_Animator;
            Transform[] m_Transforms;
            Vector3[] m_LocalPositions;
            Quaternion[] m_LocalRotations;
            Vector3[] m_LocalScales;
            PlayableGraph m_PlayableGraph;
            AnimationPlayableOutput m_PlayableOutput;
            AnimationClipPlayable m_ClipPlayable;
            NativeArray<AnimationLocalBonePose> m_ComponentPoses;

            public float GroundReferenceHeight { get; private set; }
            public CharacterFootPlacementRigGeometryReport CalibrationGeometryReport { get; private set; }
            public float LeftLegLength => m_Binding.LeftLegLength;
            public float RightLegLength => m_Binding.RightLegLength;

            public SamplingContext(GameObject rigPrefab, CharacterFootPlacementAnalysisSource source)
                : this(rigPrefab, source, false)
            {
            }

            public SamplingContext(
                GameObject rigPrefab,
                CharacterFootPlacementAnalysisSource source,
                bool calibrationAuthoring)
            {
                try
                {
                    m_PreviewScene = EditorSceneManager.NewPreviewScene();
                    m_Instance = PrefabUtility.InstantiatePrefab(rigPrefab, m_PreviewScene) as GameObject;
                    if (!m_Instance)
                        throw new InvalidOperationException("Sampling Rig Prefab could not be instantiated");
                    m_Instance.hideFlags = HideFlags.HideAndDontSave;
                    m_Instance.SetActive(true);
                    CharacterAnimationRigBinding[] rigBindings = m_Instance.GetComponentsInChildren<CharacterAnimationRigBinding>(true);
                    CharacterWorldAwarePresentationBinding[] worldBindings = m_Instance.GetComponentsInChildren<CharacterWorldAwarePresentationBinding>(true);
                    Animator[] animators = m_Instance.GetComponentsInChildren<Animator>(true);
                    if (rigBindings.Length != 1 || worldBindings.Length != 1 || animators.Length != 1)
                        throw new InvalidOperationException(
                            $"Sampling Rig requires exactly one Animation Rig Binding, World-Aware Binding and Animator; found {rigBindings.Length}/{worldBindings.Length}/{animators.Length}");
                    CharacterAnimationRigPayload rig = new CharacterAnimationRigPayload(source.RigDefinition);
                    rigBindings[0].RequireValid(rig);
                    m_Binding = calibrationAuthoring
                        ? CharacterFootPlacementPoseRig.CreateCalibrationAuthoringRig(
                            source.RigCalibration,
                            source.RigDefinition,
                            rigBindings[0],
                            worldBindings[0])
                        : new CharacterFootPlacementPoseRig(
                            source.RigCalibration,
                            rig,
                            rigBindings[0],
                            worldBindings[0]);
                    if (!calibrationAuthoring)
                        m_Binding.RequireValid();
                    m_ComponentPoses = new NativeArray<AnimationLocalBonePose>(
                        rig.PoseBoneCount,
                        Allocator.Persistent,
                        NativeArrayOptions.ClearMemory);
                    m_Animator = animators[0];
                    if (rigBindings[0].Animator != m_Animator)
                        throw new InvalidOperationException("Sampling Rig Animation Rig Binding and Animator do not match exactly");
                    m_Animator.enabled = true;
                    Behaviour[] behaviours = m_Instance.GetComponentsInChildren<Behaviour>(true);
                    for (int i = 0; i < behaviours.Length; i++)
                    {
                        if (behaviours[i] != m_Animator)
                            behaviours[i].enabled = false;
                    }
                    Collider[] colliders = m_Instance.GetComponentsInChildren<Collider>(true);
                    for (int i = 0; i < colliders.Length; i++)
                        UnityEngine.Object.DestroyImmediate(colliders[i]);
                    Rigidbody[] rigidbodies = m_Instance.GetComponentsInChildren<Rigidbody>(true);
                    for (int i = 0; i < rigidbodies.Length; i++)
                        UnityEngine.Object.DestroyImmediate(rigidbodies[i]);
                    m_Animator.applyRootMotion = false;
                    m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    m_PlayableGraph = PlayableGraph.Create("Foot Analysis Sampling");
                    m_PlayableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                    m_PlayableOutput = AnimationPlayableOutput.Create(
                        m_PlayableGraph,
                        "Foot Analysis Pose",
                        m_Animator);
                    m_PlayableGraph.Play();
                    m_Transforms = m_Instance.GetComponentsInChildren<Transform>(true);
                    m_LocalPositions = new Vector3[m_Transforms.Length];
                    m_LocalRotations = new Quaternion[m_Transforms.Length];
                    m_LocalScales = new Vector3[m_Transforms.Length];
                    for (int i = 0; i < m_Transforms.Length; i++)
                    {
                        m_LocalPositions[i] = m_Transforms[i].localPosition;
                        m_LocalRotations[i] = m_Transforms[i].localRotation;
                        m_LocalScales[i] = m_Transforms[i].localScale;
                    }
                    BeginClip(source.CalibrationPreviewClip);
                    _ = Sample(source.CalibrationPreviewTimeSeconds, 1UL);
                    CalibrationGeometryReport =
                        CharacterFootPlacementRigGeometryValidator.Evaluate(
                            m_Binding,
                            source.RigCalibration.Left,
                            source.RigCalibration.Right);
                    if (!CalibrationGeometryReport.IsValid)
                    {
                        throw new InvalidOperationException(
                            $"Foot Placement Calibration Preview Pose is geometrically invalid.\n{CalibrationGeometryReport.FormatDiagnostics()}");
                    }
                    GroundReferenceHeight = CalibrationGeometryReport.ReferenceGroundHeight;
                    if (!float.IsFinite(GroundReferenceHeight))
                        throw new InvalidOperationException("Sampling Rig ground reference is not finite");
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            public void BeginClip(UnityEngine.AnimationClip clip)
            {
                if (clip.humanMotion && (!m_Animator.avatar || !m_Animator.avatar.isHuman))
                    throw new InvalidOperationException("Humanoid AnimationClip requires the Sampling Rig's exact Humanoid Avatar");
                for (int i = 0; i < m_Transforms.Length; i++)
                {
                    m_Transforms[i].localPosition = m_LocalPositions[i];
                    m_Transforms[i].localRotation = m_LocalRotations[i];
                    m_Transforms[i].localScale = m_LocalScales[i];
                }
                if (m_ClipPlayable.IsValid())
                {
                    m_PlayableOutput.SetSourcePlayable(Playable.Null);
                    m_PlayableGraph.DestroyPlayable(m_ClipPlayable);
                }
                m_ClipPlayable = AnimationClipPlayable.Create(m_PlayableGraph, clip);
                m_ClipPlayable.SetApplyFootIK(false);
                m_ClipPlayable.SetApplyPlayableIK(false);
                m_PlayableOutput.SetSourcePlayable(m_ClipPlayable);
            }

            public CharacterFootPlacementAnimatedPose Sample(
                float sampleTime,
                ulong sequence)
            {
                m_ClipPlayable.SetTime(sampleTime);
                m_PlayableGraph.Evaluate(0f);
                CaptureComponentPoses();
                return m_Binding.CaptureAnimatedPose(
                    sequence,
                    new NativeSlice<AnimationLocalBonePose>(m_ComponentPoses));
            }

            void CaptureComponentPoses()
            {
                Transform poseRoot = m_Binding.PoseRoot;
                Vector3 rootScale = poseRoot.lossyScale;
                for (int i = 0; i < m_Binding.Rig.PhysicalBoneCount; i++)
                {
                    Transform bone = m_Binding.Binding.PhysicalBones[i];
                    Vector3 boneScale = bone.lossyScale;
                    m_ComponentPoses[i] = new AnimationLocalBonePose(
                        poseRoot.InverseTransformPoint(bone.position),
                        Quaternion.Inverse(poseRoot.rotation) * bone.rotation,
                        new Vector3(
                            boneScale.x / rootScale.x,
                            boneScale.y / rootScale.y,
                            boneScale.z / rootScale.z));
                }
            }

            public Vector3 ToVisualRootLocal(Vector3 worldPosition) =>
                m_Binding.VisualRoot.InverseTransformPoint(worldPosition);

            public Quaternion ToVisualRootLocal(Quaternion worldRotation) =>
                (Quaternion.Inverse(m_Binding.VisualRoot.rotation) * worldRotation).normalized;

            public void Dispose()
            {
                if (m_ComponentPoses.IsCreated)
                    m_ComponentPoses.Dispose();
                if (m_PlayableGraph.IsValid())
                {
                    m_PlayableGraph.Destroy();
                    m_PlayableGraph = default;
                }
                if (m_Instance)
                {
                    UnityEngine.Object.DestroyImmediate(m_Instance);
                    m_Instance = null;
                }
                if (m_PreviewScene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(m_PreviewScene);
                    m_PreviewScene = default;
                }
            }
        }

        public static AnimationFootAnalysisBuildResult Analyze(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source,
            AnimationFootContactSchedule contactSchedule)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!source)
                throw new ArgumentNullException(nameof(source));
            if (contactSchedule == null)
                throw new ArgumentNullException(nameof(contactSchedule));
            source.RequireValid();
            string clipPath = AssetDatabase.GetAssetPath(clip);
            string clipGuid = string.IsNullOrEmpty(clipPath) ? string.Empty : AssetDatabase.AssetPathToGUID(clipPath);
            if (!CharacterFootPlacementAnalysisSource.IsAssetGuid(clipGuid))
                throw new InvalidOperationException("Foot Analysis requires a persisted AnimationClip asset.");
            string rigPath = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (!rigPrefab)
                throw new InvalidOperationException(
                    $"Foot Analysis Clip '{clipGuid}' Source '{source.AnalysisSourceId}' Sampling Rig '{source.SamplingRigAssetGuid}' does not resolve to a Prefab.");
            try
            {
                using var samplingContext = new SamplingContext(rigPrefab, source);
                return AnalyzeClip(samplingContext, source, clip, contactSchedule);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Foot Analysis Clip '{clipGuid}' Source '{source.AnalysisSourceId}' Rig '{source.SamplingRigAssetGuid}' Calibration '{source.RigCalibration.CalibrationId}@{source.RigCalibration.ContentRevision}' failed during sampling: {exception.Message}",
                    exception);
            }
        }

        public static CharacterFootPlacementRigGeometryReport EvaluateCalibrationGeometry(
            CharacterFootPlacementAnalysisSource source)
        {
            if (!source)
                throw new ArgumentNullException(nameof(source));
            source.RequireCalibrationAuthoringInput();
            string rigPath = AssetDatabase.GUIDToAssetPath(source.SamplingRigAssetGuid);
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rigPath);
            if (!rigPrefab)
                throw new InvalidOperationException(
                    $"Foot Placement Sampling Rig '{source.SamplingRigAssetGuid}' does not resolve to a Prefab.");
            using var samplingContext = new SamplingContext(rigPrefab, source, true);
            return samplingContext.CalibrationGeometryReport;
        }

        static AnimationFootAnalysisBuildResult AnalyzeClip(
            SamplingContext samplingContext,
            CharacterFootPlacementAnalysisSource source,
            UnityEngine.AnimationClip clip,
            AnimationFootContactSchedule contactSchedule)
        {
            float sourceDuration =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(clip);
            samplingContext.BeginClip(clip);
            int intervals = Mathf.Max(2, Mathf.RoundToInt(sourceDuration * source.SampleRate));
            int sampleCount = intervals + 1;
            float step = sourceDuration / intervals;
            var leftHeelPositions = new Vector3[sampleCount];
            var leftToePositions = new Vector3[sampleCount];
            var leftAnklePositions = new Vector3[sampleCount];
            var leftKneePositions = new Vector3[sampleCount];
            var rightHeelPositions = new Vector3[sampleCount];
            var rightToePositions = new Vector3[sampleCount];
            var rightAnklePositions = new Vector3[sampleCount];
            var rightKneePositions = new Vector3[sampleCount];
            var leftHipPositions = new Vector3[sampleCount];
            var rightHipPositions = new Vector3[sampleCount];
            var leftSoleRotations = new Quaternion[sampleCount];
            var rightSoleRotations = new Quaternion[sampleCount];
            var leftAnkleRotations = new Quaternion[sampleCount];
            var rightAnkleRotations = new Quaternion[sampleCount];
            var rootPositions = new Vector3[sampleCount];
            var rootRotations = new Quaternion[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                CharacterFootPlacementAnimatedPose pose = samplingContext.Sample(i * step, (ulong)i + 1UL);
                leftHeelPositions[i] = samplingContext.ToVisualRootLocal(pose.Left.HeelPosition);
                leftToePositions[i] = samplingContext.ToVisualRootLocal(pose.Left.ToePosition);
                leftAnklePositions[i] = samplingContext.ToVisualRootLocal(pose.Left.AnklePosition);
                leftKneePositions[i] = samplingContext.ToVisualRootLocal(pose.Left.KneePosition);
                rightHeelPositions[i] = samplingContext.ToVisualRootLocal(pose.Right.HeelPosition);
                rightToePositions[i] = samplingContext.ToVisualRootLocal(pose.Right.ToePosition);
                rightAnklePositions[i] = samplingContext.ToVisualRootLocal(pose.Right.AnklePosition);
                rightKneePositions[i] = samplingContext.ToVisualRootLocal(pose.Right.KneePosition);
                leftHipPositions[i] = samplingContext.ToVisualRootLocal(pose.Left.HipPosition);
                rightHipPositions[i] = samplingContext.ToVisualRootLocal(pose.Right.HipPosition);
                leftSoleRotations[i] = samplingContext.ToVisualRootLocal(pose.Left.SemanticRotation);
                rightSoleRotations[i] = samplingContext.ToVisualRootLocal(pose.Right.SemanticRotation);
                leftAnkleRotations[i] = samplingContext.ToVisualRootLocal(pose.Left.AnkleRotation);
                rightAnkleRotations[i] = samplingContext.ToVisualRootLocal(pose.Right.AnkleRotation);
                rootPositions[i] = Vector3.zero;
                rootRotations[i] = Quaternion.identity;
                RequireFinite(leftHeelPositions[i], "left heel position", i);
                RequireFinite(leftToePositions[i], "left toe position", i);
                RequireFinite(leftAnklePositions[i], "left ankle position", i);
                RequireFinite(leftKneePositions[i], "left knee position", i);
                RequireFinite(rightHeelPositions[i], "right heel position", i);
                RequireFinite(rightToePositions[i], "right toe position", i);
                RequireFinite(rightAnklePositions[i], "right ankle position", i);
                RequireFinite(rightKneePositions[i], "right knee position", i);
                RequireFinite(leftHipPositions[i], "left hip position", i);
                RequireFinite(rightHipPositions[i], "right hip position", i);
                RequireFinite(leftSoleRotations[i], "left Sole rotation", i);
                RequireFinite(rightSoleRotations[i], "right Sole rotation", i);
                RequireFinite(leftAnkleRotations[i], "left Ankle rotation", i);
                RequireFinite(rightAnkleRotations[i], "right Ankle rotation", i);
                RequireFinite(rootPositions[i], "animation root position", i);
                RequireFinite(rootRotations[i], "animation root rotation", i);
            }

            SampledFoot left = AnalyzeFoot(
                leftHeelPositions,
                leftToePositions,
                leftAnklePositions,
                leftKneePositions,
                leftHipPositions,
                leftSoleRotations,
                leftAnkleRotations,
                samplingContext.LeftLegLength,
                rootPositions,
                rootRotations);
            SampledFoot right = AnalyzeFoot(
                rightHeelPositions,
                rightToePositions,
                rightAnklePositions,
                rightKneePositions,
                rightHipPositions,
                rightSoleRotations,
                rightAnkleRotations,
                samplingContext.RightLegLength,
                rootPositions,
                rootRotations);
            BuildContactFeatures(
                left,
                samplingContext.GroundReferenceHeight,
                clip.isLooping,
                step,
                source.Thresholds);
            BuildContactFeatures(
                right,
                samplingContext.GroundReferenceHeight,
                clip.isLooping,
                step,
                source.Thresholds);
            List<int> leftLandingSamples = ResolveLandingSamples(
                left,
                clip.isLooping,
                step,
                source.Thresholds,
                contactSchedule.InferLandingEvents,
                contactSchedule.LeftLandingPhases);
            List<int> rightLandingSamples = ResolveLandingSamples(
                right,
                clip.isLooping,
                step,
                source.Thresholds,
                contactSchedule.InferLandingEvents,
                contactSchedule.RightLandingPhases);
            BuildLandingFeatures(
                left,
                right,
                rootPositions,
                rootRotations,
                leftLandingSamples,
                rightLandingSamples,
                clip.isLooping,
                step,
                source.Thresholds,
                contactSchedule.InferLandingEvents);
            BuildLandingFeatures(
                right,
                left,
                rootPositions,
                rootRotations,
                rightLandingSamples,
                leftLandingSamples,
                clip.isLooping,
                step,
                source.Thresholds,
                contactSchedule.InferLandingEvents);
            BuildPairedLandingFeatures(
                left,
                right,
                rootPositions,
                rootRotations,
                leftLandingSamples,
                rightLandingSamples,
                clip.isLooping,
                step);
            BuildPairedLandingFeatures(
                right,
                left,
                rootPositions,
                rootRotations,
                rightLandingSamples,
                leftLandingSamples,
                clip.isLooping,
                step);
            if (!contactSchedule.InferLandingEvents &&
                (clip.isLooping ||
                 leftLandingSamples.Count > 0 && rightLandingSamples.Count > 0))
            {
                ValidateAuthoredLandingPair(
                    leftLandingSamples,
                    rightLandingSamples,
                    left.PlantConfidence.Length - 1,
                    clip.isLooping);
            }
            AnimationFootFeaturePair features = new AnimationFootFeaturePair(
                BuildCurveSet(left, source.Reduction, clip.isLooping, step),
                BuildCurveSet(right, source.Reduction, clip.isLooping, step));
            ValidateFlatReconstruction(
                features.Left,
                left,
                source.Reduction,
                clip.isLooping,
                step,
                "Left");
            ValidateFlatReconstruction(
                features.Right,
                right,
                source.Reduction,
                clip.isLooping,
                step,
                "Right");
            return new AnimationFootAnalysisBuildResult(
                features,
                new AnimationFootPhaseValidationDescriptor(
                    source.SampleRate,
                    sourceDuration,
                    BuildPhaseValidationFoot(
                        left,
                        samplingContext.GroundReferenceHeight,
                        leftLandingSamples),
                    BuildPhaseValidationFoot(
                        right,
                        samplingContext.GroundReferenceHeight,
                        rightLandingSamples)));
        }

        static void ValidateFlatReconstruction(
            AnimationFootFeatureCurveSet curves,
            SampledFoot source,
            CharacterFootPlacementCurveReductionSettings reduction,
            bool loop,
            float sampleStep,
            string side)
        {
            ValidatePredictionReconstruction(
                curves.PredictedStep,
                source,
                reduction,
                side);
            ValidatePredictionReconstruction(
                curves.IncomingPredictedStep,
                BuildIncomingPrediction(source, loop, sampleStep),
                reduction,
                side + " Incoming");
        }

        static void ValidatePredictionReconstruction(
            AnimationPredictedFootStepCurveSet curves,
            SampledFoot source,
            CharacterFootPlacementCurveReductionSettings reduction,
            string side)
        {
            int sourceCount = source.EventOrdinal.Length;
            float positionTolerance = reduction.LandingOffsetTolerance + 0.00001f;
            float heightTolerance = reduction.HeightTolerance + 0.00001f;
            float weightTolerance = reduction.ConfidenceTolerance + 0.00001f;
            float delayTolerance = reduction.LandingDelayTolerance + 0.00001f;
            const float rotationToleranceDegrees = 0.5f;
            for (int sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
            {
                float time = sourceIndex / (float)(sourceCount - 1);
                AnimationPredictedFootStepSample step = curves.Sample(time);
                curves.BiomechanicalStep.Sample(
                    time,
                    out _,
                    out _,
                    out FixedList4096Bytes<AnimationFootBiomechanicalRouteSample> biomechanicalRoute);
                RequireReconstruction(step.Confidence, source.LandingConfidence[sourceIndex], weightTolerance, side, sourceIndex, -1, "Confidence");
                RequireReconstruction(step.TimeToLandingSeconds, source.LandingDelay[sourceIndex], delayTolerance, side, sourceIndex, -1, "TimeToLanding");
                RequireReconstruction(step.EventPhase, source.EventPhase[sourceIndex], weightTolerance, side, sourceIndex, -1, "EventPhase");
                RequireReconstruction(step.ReleasePhase, source.ReleasePhase[sourceIndex], weightTolerance, side, sourceIndex, -1, "ReleasePhase");
                RequireReconstruction(step.LiftOffPhase, source.LiftOffPhase[sourceIndex], weightTolerance, side, sourceIndex, -1, "LiftOffPhase");
                RequireReconstruction(step.ApproachContactPhase, source.ApproachContactPhase[sourceIndex], weightTolerance, side, sourceIndex, -1, "ApproachContactPhase");
                RequireReconstruction(step.ActionStepClock.DurationSeconds, source.ActionStepDurationSeconds[sourceIndex], delayTolerance, side, sourceIndex, -1, "StepDuration");
                RequireReconstruction(step.EventOrdinal, source.EventOrdinal[sourceIndex], 0.00001f, side, sourceIndex, -1, "EventOrdinal");
                RequireReconstruction(step.SourceLandingCycleOffset, source.SourceLandingCycleOffset[sourceIndex], 0.00001f, side, sourceIndex, -1, "SourceLandingCycleOffset");
                RequireReconstruction(step.OpposingLandingDelaySeconds, source.OpposingLandingDelaySeconds[sourceIndex], delayTolerance, side, sourceIndex, -1, "OpposingLandingDelay");
                RequireReconstruction(step.OpposingEventOrdinal, source.OpposingEventOrdinal[sourceIndex], 0.00001f, side, sourceIndex, -1, "OpposingEventOrdinal");
                RequireReconstruction(step.OpposingLandingCycleOffset, source.OpposingLandingCycleOffset[sourceIndex], 0.00001f, side, sourceIndex, -1, "OpposingCycleOffset");
                RequireReconstruction(step.OpposingRootLocalLanding, source.OpposingRootLocalLanding[sourceIndex], positionTolerance, side, sourceIndex, -1, "OpposingLanding");
                RequireReconstruction(
                    step.LandingPhase,
                    source.EventOrdinal[sourceIndex] > 0f ? 1f : 0f,
                    weightTolerance,
                    side,
                    sourceIndex,
                    -1,
                    "LandingPhase");
                RequireReconstruction(
                    step.OpposingRootLocalSoleRotation,
                    source.OpposingRootLocalLandingRotation[sourceIndex],
                    rotationToleranceDegrees,
                    side,
                    sourceIndex,
                    -1,
                    "OpposingSoleRotation");
                for (int routeIndex = 0; routeIndex < AnimationPredictedFootStepCurveSet.RouteSampleCount; routeIndex++)
                {
                    AnimationFootBiomechanicalRouteSample actual = biomechanicalRoute[routeIndex];
                    RequireReconstruction(step.Route.RootLocalFoot[routeIndex], source.RootLocalFootRoute[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "Sole");
                    RequireReconstruction(step.Route.RootLocalAnkle[routeIndex], source.RootLocalAnkleRoute[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "Ankle");
                    RequireReconstruction(step.Route.RootLocalHip[routeIndex], source.RootLocalHipRoute[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "Hip");
                    RequireReconstruction(step.Route.AuthoredFootPlanar[routeIndex], source.AuthoredFootPlanarRoute[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "PlanarRoute");
                    RequireReconstruction(step.Route.AnimationClearance[routeIndex], source.AnimationClearanceHeight[routeIndex][sourceIndex], heightTolerance, side, sourceIndex, routeIndex, "Clearance");
                    RequireReconstruction(actual.RootLocalHeelPosition, source.RootLocalHeelRoute[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "Heel");
                    RequireReconstruction(actual.RootLocalToePosition, source.RootLocalToeRoute[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "Toe");
                    RequireReconstruction(actual.RootLocalKneePosition, source.RootLocalKneeRoute[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "Knee");
                    RequireReconstruction(actual.RootLocalSoleRotation, source.RootLocalSoleRotationRoute[routeIndex][sourceIndex], rotationToleranceDegrees, side, sourceIndex, routeIndex, "SoleRotation");
                    RequireReconstruction(actual.RootLocalAnkleRotation, source.RootLocalAnkleRotationRoute[routeIndex][sourceIndex], rotationToleranceDegrees, side, sourceIndex, routeIndex, "AnkleRotation");
                    RequireReconstruction(actual.ConstraintWeight, source.ConstraintWeight[routeIndex][sourceIndex], weightTolerance, side, sourceIndex, routeIndex, "ConstraintWeight");
                    RequireReconstruction(actual.SupportWeight, source.SupportWeight[routeIndex][sourceIndex], weightTolerance, side, sourceIndex, routeIndex, "SupportWeight");
                    RequireReconstruction(actual.SupportLegLength, source.SupportLegLength[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "SupportLegLength");
                    RequireReconstruction(actual.SupportLegCompressionReserve, source.SupportLegCompressionReserve[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "CompressionReserve");
                    RequireReconstruction(actual.SupportKneeBendPlane, source.SupportKneeBendPlane[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "KneeBendPlane");
                    RequireReconstruction(actual.SupportFootPivotPosition, source.SupportFootPivotPosition[routeIndex][sourceIndex], positionTolerance, side, sourceIndex, routeIndex, "SupportPivot");
                    RequireReconstruction(actual.SupportFootPivotWeight, source.SupportFootPivotWeight[routeIndex][sourceIndex], weightTolerance, side, sourceIndex, routeIndex, "SupportPivotWeight");
                }
            }
        }

        static void RequireReconstruction(
            Vector3 actual,
            Vector3 expected,
            float tolerance,
            string side,
            int sourceIndex,
            int routeIndex,
            string field)
        {
            float error = Vector3.Distance(actual, expected);
            if (!float.IsFinite(error) || error > tolerance)
                throw ReconstructionFailure(side, sourceIndex, routeIndex, field, error, tolerance);
        }

        static void RequireReconstruction(
            Quaternion actual,
            Quaternion expected,
            float tolerance,
            string side,
            int sourceIndex,
            int routeIndex,
            string field)
        {
            float error = Quaternion.Angle(actual, expected);
            if (!float.IsFinite(error) || error > tolerance)
                throw ReconstructionFailure(side, sourceIndex, routeIndex, field, error, tolerance);
        }

        static void RequireReconstruction(
            float actual,
            float expected,
            float tolerance,
            string side,
            int sourceIndex,
            int routeIndex,
            string field)
        {
            float error = Mathf.Abs(actual - expected);
            if (!float.IsFinite(error) || error > tolerance)
                throw ReconstructionFailure(side, sourceIndex, routeIndex, field, error, tolerance);
        }

        static InvalidOperationException ReconstructionFailure(
            string side,
            int sourceIndex,
            int routeIndex,
            string field,
            float error,
            float tolerance) =>
            new InvalidOperationException(
                $"{side} Foot Analysis flat reconstruction failed for {field} at source {sourceIndex}, route {routeIndex}: {error} > {tolerance}.");

        static AnimationFootPhaseValidationFootDescriptor BuildPhaseValidationFoot(
            SampledFoot foot,
            float groundReferenceHeight,
            IReadOnlyCollection<int> landingSamples)
        {
            int last = foot.SolePositions.Length - 1;
            var onset = new HashSet<int>(landingSamples ?? Array.Empty<int>());
            var samples = new AnimationFootPhaseValidationSample[last + 1];
            for (int i = 0; i <= last; i++)
            {
                Vector3 position = foot.SolePositions[i];
                samples[i] = new AnimationFootPhaseValidationSample(
                    i / (float)last,
                    new Vector2(position.x, position.z),
                    foot.Heights[i] - groundReferenceHeight,
                    foot.Velocities[i],
                    foot.PlantConfidence[i],
                    onset.Contains(i));
            }
            return new AnimationFootPhaseValidationFootDescriptor(samples);
        }

        static SampledFoot AnalyzeFoot(
            Vector3[] heelPositions,
            Vector3[] toePositions,
            Vector3[] anklePositions,
            Vector3[] kneePositions,
            Vector3[] hipPositions,
            Quaternion[] soleRotations,
            Quaternion[] ankleRotations,
            float rigLegLength,
            Vector3[] rootPositions,
            Quaternion[] rootRotations)
        {
            if (heelPositions == null || toePositions == null || hipPositions == null ||
                soleRotations == null || ankleRotations == null ||
                rootPositions == null || rootRotations == null ||
                heelPositions.Length != toePositions.Length || heelPositions.Length != rootPositions.Length ||
                heelPositions.Length != hipPositions.Length || heelPositions.Length != rootRotations.Length ||
                heelPositions.Length != soleRotations.Length || heelPositions.Length != ankleRotations.Length ||
                !float.IsFinite(rigLegLength) || rigLegLength <= 0f)
            {
                throw new ArgumentException("Foot Analysis foot/root sample counts do not match.");
            }
            int last = heelPositions.Length - 1;
            var positions = new Vector3[heelPositions.Length];
            var result = new SampledFoot
            {
                HeelPositions = heelPositions,
                SolePositions = positions,
                ToePositions = toePositions,
                AnklePositions = anklePositions,
                KneePositions = kneePositions,
                HipPositions = hipPositions,
                SoleRotations = soleRotations,
                AnkleRotations = ankleRotations,
                RigLegLength = rigLegLength,
                Velocities = new Vector3[positions.Length],
                Heights = new float[positions.Length],
                PlantConfidence = new float[positions.Length],
                LandingConfidence = new float[positions.Length],
                LandingDelay = new float[positions.Length],
                EventPhase = new float[positions.Length],
                ReleasePhase = new float[positions.Length],
                LiftOffPhase = new float[positions.Length],
                ApproachContactPhase = new float[positions.Length],
                ActionStepDurationSeconds = new float[positions.Length],
                EventOrdinal = new float[positions.Length],
                SourceLandingCycleOffset = new float[positions.Length],
                OpposingLandingDelaySeconds = new float[positions.Length],
                OpposingEventOrdinal = new float[positions.Length],
                OpposingLandingCycleOffset = new float[positions.Length],
                OpposingRootLocalLanding = new Vector3[positions.Length],
                OpposingRootLocalLandingRotation = CreateIdentityRotations(positions.Length),
                RootLocalFootRoute = CreateVectorRoute(positions.Length),
                RootLocalHeelRoute = CreateVectorRoute(positions.Length),
                RootLocalToeRoute = CreateVectorRoute(positions.Length),
                RootLocalAnkleRoute = CreateVectorRoute(positions.Length),
                RootLocalKneeRoute = CreateVectorRoute(positions.Length),
                RootLocalHipRoute = CreateVectorRoute(positions.Length),
                RootLocalSoleRotationRoute = CreateQuaternionRoute(positions.Length),
                RootLocalAnkleRotationRoute = CreateQuaternionRoute(positions.Length),
                AuthoredFootPlanarRoute = CreateVectorRoute(positions.Length),
                AnimationClearanceHeight = CreateScalarRoute(positions.Length),
                ConstraintWeight = CreateScalarRoute(positions.Length),
                SupportWeight = CreateScalarRoute(positions.Length),
                SupportLegLength = CreateScalarRoute(positions.Length),
                SupportLegCompressionReserve = CreateScalarRoute(positions.Length),
                SupportKneeBendPlane = CreateVectorRoute(positions.Length),
                SupportFootPivotPosition = CreateVectorRoute(positions.Length),
                SupportFootPivotWeight = CreateScalarRoute(positions.Length)
            };
            for (int i = 0; i <= last; i++)
            {
                positions[i] = (heelPositions[i] + toePositions[i]) * 0.5f;
                result.Heights[i] = Mathf.Min(heelPositions[i].y, toePositions[i].y);
            }
            return result;
        }

        static void BuildContactFeatures(
            SampledFoot foot,
            float groundReferenceHeight,
            bool loop,
            float step,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            foot.Velocities = BuildVelocities(
                foot.SolePositions,
                loop,
                step,
                "root-local sole velocity");
            Vector3[] heelVelocities = BuildVelocities(
                foot.HeelPositions,
                loop,
                step,
                "root-local heel velocity");
            Vector3[] toeVelocities = BuildVelocities(
                foot.ToePositions,
                loop,
                step,
                "root-local toe velocity");
            var heelContactConfidence = new float[foot.HeelPositions.Length];
            var toeContactConfidence = new float[foot.ToePositions.Length];
            var toeHeights = new float[foot.ToePositions.Length];
            for (int i = 0; i < foot.ToePositions.Length; i++)
            {
                toeHeights[i] = foot.ToePositions[i].y;
                heelContactConfidence[i] = EvaluateContactConfidence(
                    foot.HeelPositions[i].y,
                    Mathf.Abs(heelVelocities[i].y),
                    groundReferenceHeight,
                    thresholds);
                toeContactConfidence[i] = EvaluateContactConfidence(
                    toeHeights[i],
                    Mathf.Abs(toeVelocities[i].y),
                    groundReferenceHeight,
                    thresholds);
                foot.Heights[i] = Mathf.Min(foot.HeelPositions[i].y, foot.ToePositions[i].y);
            }
            float[] poseConfidence = BuildIkPoseConfidence(
                foot.HeelPositions,
                foot.ToePositions,
                foot.AnklePositions,
                foot.KneePositions,
                foot.HipPositions,
                groundReferenceHeight,
                thresholds);
            BuildPlantConfidence(
                foot.PlantConfidence,
                heelContactConfidence,
                toeContactConfidence,
                poseConfidence,
                loop,
                step,
                thresholds);
        }

        static Vector3[] BuildVelocities(
            Vector3[] positions,
            bool loop,
            float step,
            string field)
        {
            int last = positions.Length - 1;
            var velocities = new Vector3[positions.Length];
            for (int i = 0; i <= last; i++)
            {
                if (loop && (i == 0 || i == last))
                    velocities[i] = (positions[1] - positions[last - 1]) / (2f * step);
                else if (i == 0)
                    velocities[i] = (positions[1] - positions[0]) / step;
                else if (i == last)
                    velocities[i] = (positions[last] - positions[last - 1]) / step;
                else
                    velocities[i] = (positions[i + 1] - positions[i - 1]) / (2f * step);
                RequireFinite(velocities[i], field, i);
            }
            return velocities;
        }

        static void BuildPlantConfidence(
            float[] confidence,
            float[] heelContactConfidence,
            float[] toeContactConfidence,
            float[] poseConfidence,
            bool loop,
            float step,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            if (heelContactConfidence.Length != confidence.Length ||
                toeContactConfidence.Length != confidence.Length ||
                poseConfidence.Length != confidence.Length)
            {
                throw new ArgumentException("Foot Analysis plant feature sample counts do not match.");
            }
            int intervals = confidence.Length - 1;
            if (!loop)
            {
                bool planted = false;
                for (int i = 0; i <= intervals; i++)
                    confidence[i] = EvaluatePlantSample(
                        ref planted,
                        heelContactConfidence[i],
                        toeContactConfidence[i],
                        poseConfidence[i]);
                StabilizePlantConfidence(confidence, false, step, thresholds.MinimumLandingSegmentSeconds);
                return;
            }

            int releaseSample = -1;
            bool hasEnterEvidence = false;
            for (int i = 0; i < intervals; i++)
            {
                float value = EvaluateCombinedContactConfidence(
                    heelContactConfidence[i],
                    toeContactConfidence[i],
                    poseConfidence[i]);
                hasEnterEvidence |= value >= 0.5f;
                if (value <= 0f)
                {
                    releaseSample = i;
                }
            }

            bool loopPlanted = releaseSample < 0 && hasEnterEvidence;
            int start = releaseSample < 0 ? 0 : (releaseSample + 1) % intervals;
            for (int offset = 0; offset < intervals; offset++)
            {
                int i = (start + offset) % intervals;
                confidence[i] = EvaluatePlantSample(
                    ref loopPlanted,
                    heelContactConfidence[i],
                    toeContactConfidence[i],
                    poseConfidence[i]);
            }
            confidence[intervals] = confidence[0];
            StabilizePlantConfidence(confidence, true, step, thresholds.MinimumLandingSegmentSeconds);
        }

        static float EvaluatePlantSample(
            ref bool planted,
            float heelContactConfidence,
            float toeContactConfidence,
            float poseConfidence)
        {
            float value = EvaluateCombinedContactConfidence(
                heelContactConfidence,
                toeContactConfidence,
                poseConfidence);
            bool enter = value >= 0.5f;
            bool exit = value <= 0f;
            if (!planted && enter)
                planted = true;
            else if (planted && exit)
                planted = false;
            return planted ? Mathf.Max(0.5f, value) : Mathf.Min(0.499f, value);
        }

        static float EvaluateCombinedContactConfidence(
            float heelContactConfidence,
            float toeContactConfidence,
            float poseConfidence) =>
            Mathf.Min(
                Mathf.Max(heelContactConfidence, toeContactConfidence),
                poseConfidence);

        static float[] BuildIkPoseConfidence(
            Vector3[] heelPositions,
            Vector3[] toePositions,
            Vector3[] anklePositions,
            Vector3[] kneePositions,
            Vector3[] hipPositions,
            float groundReferenceHeight,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            int count = heelPositions.Length;
            if (toePositions.Length != count || anklePositions.Length != count ||
                kneePositions.Length != count || hipPositions.Length != count)
            {
                throw new ArgumentException("Foot Analysis IK pose sample counts do not match.");
            }
            var result = new float[count];
            for (int i = 0; i < count; i++)
            {
                float minimumContactHeight = Mathf.Min(heelPositions[i].y, toePositions[i].y);
                float translationError = Mathf.Max(0f, minimumContactHeight - groundReferenceHeight);
                float rotationError = Mathf.Abs(heelPositions[i].y - toePositions[i].y);
                Vector3 targetAnkle = anklePositions[i] + Vector3.up *
                    (groundReferenceHeight - minimumContactHeight);
                float legLength = Vector3.Distance(hipPositions[i], kneePositions[i]) +
                                  Vector3.Distance(kneePositions[i], anklePositions[i]);
                float reachError = Mathf.Max(
                    0f,
                    Vector3.Distance(hipPositions[i], targetAnkle) - legLength);
                float translationConfidence = Mathf.InverseLerp(
                    thresholds.PlantExitHeight,
                    thresholds.PlantEnterHeight,
                    translationError);
                float rotationConfidence = Mathf.InverseLerp(
                    thresholds.PlantExitHeight,
                    thresholds.PlantEnterHeight,
                    rotationError);
                float reachConfidence = Mathf.InverseLerp(
                    thresholds.PlantExitHeight,
                    thresholds.PlantEnterHeight,
                    reachError);
                float supportConfidence = Mathf.Min(
                    translationConfidence,
                    reachConfidence);
                float orientationCeiling = Mathf.Lerp(
                    AnimationFootConstraintFacts.GroundedMinimumConfidence,
                    1f,
                    rotationConfidence);
                result[i] = Mathf.Clamp01(Mathf.Min(
                    supportConfidence,
                    orientationCeiling));
            }
            return result;
        }

        static float EvaluateContactConfidence(
            float height,
            float speed,
            float groundReferenceHeight,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            float clearance = Mathf.Max(0f, height - groundReferenceHeight);
            float speedFactor = Mathf.InverseLerp(
                thresholds.PlantExitContactSpeed,
                thresholds.PlantEnterContactSpeed,
                speed);
            float heightFactor = Mathf.InverseLerp(
                thresholds.PlantExitHeight,
                thresholds.PlantEnterHeight,
                clearance);
            return Mathf.Clamp01(Mathf.Min(speedFactor, heightFactor));
        }

        static void StabilizePlantConfidence(
            float[] confidence,
            bool loop,
            float step,
            float minimumSegmentSeconds)
        {
            int count = loop ? confidence.Length - 1 : confidence.Length;
            int minimumSamples = Mathf.Max(1, Mathf.CeilToInt(minimumSegmentSeconds / step));
            if (count <= 1 || minimumSamples <= 1)
                return;
            var states = new bool[count];
            for (int i = 0; i < count; i++)
                states[i] = confidence[i] >= 0.5f;
            for (int pass = 0; pass < count; pass++)
            {
                bool changed = loop
                    ? CollapseShortestCircularRun(states, minimumSamples)
                    : CollapseShortestInteriorRun(states, minimumSamples);
                if (!changed)
                    break;
            }
            for (int i = 0; i < count; i++)
            {
                confidence[i] = states[i]
                    ? Mathf.Max(0.5f, confidence[i])
                    : Mathf.Min(0.499f, confidence[i]);
            }
            if (loop)
                confidence[confidence.Length - 1] = confidence[0];
        }

        static bool CollapseShortestInteriorRun(bool[] states, int minimumSamples)
        {
            int bestStart = -1;
            int bestLength = int.MaxValue;
            int start = 0;
            while (start < states.Length)
            {
                int end = start + 1;
                while (end < states.Length && states[end] == states[start])
                    end++;
                int length = end - start;
                if (start > 0 && end < states.Length && length < minimumSamples && length < bestLength)
                {
                    bestStart = start;
                    bestLength = length;
                }
                start = end;
            }
            if (bestStart < 0)
                return false;
            bool replacement = states[bestStart - 1];
            for (int i = 0; i < bestLength; i++)
                states[bestStart + i] = replacement;
            return true;
        }

        static bool CollapseShortestCircularRun(bool[] states, int minimumSamples)
        {
            int boundary = -1;
            for (int i = 0; i < states.Length; i++)
            {
                int previous = (i + states.Length - 1) % states.Length;
                if (states[i] != states[previous])
                {
                    boundary = i;
                    break;
                }
            }
            if (boundary < 0)
                return false;
            int bestOffset = -1;
            int bestLength = int.MaxValue;
            int offset = 0;
            while (offset < states.Length)
            {
                bool value = states[(boundary + offset) % states.Length];
                int length = 1;
                while (offset + length < states.Length &&
                       states[(boundary + offset + length) % states.Length] == value)
                {
                    length++;
                }
                if (length < minimumSamples && length < bestLength)
                {
                    bestOffset = offset;
                    bestLength = length;
                }
                offset += length;
            }
            if (bestOffset < 0)
                return false;
            bool replacement = states[(boundary + bestOffset + states.Length - 1) % states.Length];
            for (int i = 0; i < bestLength; i++)
                states[(boundary + bestOffset + i) % states.Length] = replacement;
            return true;
        }

        static List<int> ResolveLandingSamples(
            SampledFoot foot,
            bool loop,
            float step,
            CharacterFootPlacementAnalysisThresholds thresholds,
            bool inferLandingEvents,
            IReadOnlyList<float> authoredLandingPhases)
        {
            int intervals = foot.PlantConfidence.Length - 1;
            int minimumSamples = Mathf.Max(1, Mathf.CeilToInt(thresholds.MinimumLandingSegmentSeconds / step));
            var starts = new List<int>();
            if (inferLandingEvents)
            {
                for (int i = 0; i < intervals; i++)
                {
                    bool current = foot.PlantConfidence[i] >= 0.5f;
                    bool previous = i > 0
                        ? foot.PlantConfidence[i - 1] >= 0.5f
                        : loop && foot.PlantConfidence[intervals - 1] >= 0.5f;
                    if (!current || previous)
                        continue;
                    int count = CountPlantedSamples(foot.PlantConfidence, i, intervals, loop);
                    if (count >= minimumSamples)
                        starts.Add(i);
                }
                if (!loop && foot.PlantConfidence[intervals] >= 0.5f &&
                    foot.PlantConfidence[intervals - 1] < 0.5f)
                {
                    starts.Add(intervals);
                }
            }
            else
            {
                for (int i = 0; i < authoredLandingPhases.Count; i++)
                {
                    int sample = Mathf.Clamp(
                        Mathf.RoundToInt(authoredLandingPhases[i] * intervals),
                        0,
                        loop ? intervals - 1 : intervals);
                    if (starts.Count == 0 || starts[starts.Count - 1] != sample)
                        starts.Add(sample);
                }
            }
            return starts;
        }

        static void BuildLandingFeatures(
            SampledFoot foot,
            SampledFoot opposingFoot,
            Vector3[] authoredRootPositions,
            Quaternion[] authoredRootRotations,
            IReadOnlyList<int> ownLandings,
            IReadOnlyList<int> opposingLandings,
            bool loop,
            float step,
            CharacterFootPlacementAnalysisThresholds thresholds,
            bool inferLandingEvents)
        {
            int intervals = foot.PlantConfidence.Length - 1;
            if (opposingFoot == null ||
                opposingFoot.SolePositions.Length != foot.SolePositions.Length)
            {
                throw new ArgumentException("Foot Analysis opposing sole samples do not match.");
            }
            var rootLocalSole = new Vector3[foot.SolePositions.Length];
            var rootLocalHeel = new Vector3[foot.HeelPositions.Length];
            var rootLocalToe = new Vector3[foot.ToePositions.Length];
            var opposingRootLocalSole = new Vector3[opposingFoot.SolePositions.Length];
            var rootLocalAnkle = new Vector3[foot.AnklePositions.Length];
            var rootLocalKnee = new Vector3[foot.KneePositions.Length];
            var rootLocalHip = new Vector3[foot.HipPositions.Length];
            var rootLocalSoleRotation = new Quaternion[foot.SoleRotations.Length];
            var rootLocalAnkleRotation = new Quaternion[foot.AnkleRotations.Length];
            for (int i = 0; i < rootLocalSole.Length; i++)
            {
                Quaternion rootInverse = Quaternion.Inverse(authoredRootRotations[i]);
                rootLocalSole[i] = rootInverse *
                                   (foot.SolePositions[i] - authoredRootPositions[i]);
                rootLocalHeel[i] = rootInverse *
                                   (foot.HeelPositions[i] - authoredRootPositions[i]);
                rootLocalToe[i] = rootInverse *
                                  (foot.ToePositions[i] - authoredRootPositions[i]);
                opposingRootLocalSole[i] = rootInverse *
                                           (opposingFoot.SolePositions[i] - authoredRootPositions[i]);
                rootLocalAnkle[i] = rootInverse *
                                    (foot.AnklePositions[i] - authoredRootPositions[i]);
                rootLocalKnee[i] = rootInverse *
                                   (foot.KneePositions[i] - authoredRootPositions[i]);
                rootLocalHip[i] = rootInverse *
                                  (foot.HipPositions[i] - authoredRootPositions[i]);
                rootLocalSoleRotation[i] = (rootInverse * foot.SoleRotations[i]).normalized;
                rootLocalAnkleRotation[i] = (rootInverse * foot.AnkleRotations[i]).normalized;
            }
            PreserveQuaternionContinuity(rootLocalSoleRotation);
            PreserveQuaternionContinuity(rootLocalAnkleRotation);

            for (int i = 0; i <= intervals; i++)
            {
                int sample = i;
                int next = -1;
                int eventIndex = -1;
                for (int startIndex = 0; startIndex < ownLandings.Count; startIndex++)
                {
                    if (ownLandings[startIndex] >= sample)
                    {
                        next = ownLandings[startIndex];
                        eventIndex = startIndex;
                        break;
                    }
                }
                if (next < 0 && loop && ownLandings.Count > 0)
                {
                    next = ownLandings[0] + intervals;
                    eventIndex = 0;
                }
                if (next < 0)
                    continue;
                float delay = (next - sample) * step;
                if (delay < 0f || delay > thresholds.MaximumLandingSearchSeconds)
                    continue;
                int landingSample = loop ? next % intervals : Mathf.Clamp(next, 0, intervals);
                foot.LandingConfidence[i] = inferLandingEvents
                    ? foot.PlantConfidence[landingSample]
                    : 1f;
                foot.LandingDelay[i] = delay;
                foot.EventOrdinal[i] = eventIndex + 1;
                foot.SourceLandingCycleOffset[i] = loop ? next / intervals : 0;

                int previous = ResolvePreviousLanding(ownLandings, eventIndex, next, loop, intervals);
                int opposing = ResolveOpposingLanding(
                    opposingLandings,
                    previous,
                    next,
                    loop,
                    intervals);
                if (previous == 0 && eventIndex == 0 && !loop &&
                    opposing > previous && opposing < next)
                {
                    previous = opposing - (next - opposing);
                }
                int liftOff = ResolveLiftOff(foot.PlantConfidence, previous, next, intervals, loop);
                int release = ResolveRelease(foot.PlantConfidence, previous, liftOff, intervals, loop);
                int approachContact = ResolveApproachContact(
                    previous,
                    opposing,
                    liftOff,
                    next,
                    rootLocalSole,
                    opposingRootLocalSole,
                    loop,
                    intervals);
                float eventLength = Mathf.Max(1f, next - previous);
                foot.EventPhase[i] = Mathf.InverseLerp(previous, next, sample);
                foot.ReleasePhase[i] = Mathf.InverseLerp(previous, next, release);
                foot.LiftOffPhase[i] = Mathf.InverseLerp(previous, next, liftOff);
                foot.ApproachContactPhase[i] = Mathf.InverseLerp(previous, next, approachContact);
                foot.ActionStepDurationSeconds[i] = eventLength * step;
                var authoredSoleHeights = new float[AnimationPredictedFootStepCurveSet.RouteSampleCount];

                for (int routeIndex = 0;
                     routeIndex < AnimationPredictedFootStepCurveSet.RouteSampleCount;
                     routeIndex++)
                {
                    float routePhase = routeIndex /
                        (AnimationPredictedFootStepCurveSet.RouteSampleCount - 1f);
                    float routeSample = Mathf.Lerp(previous, next, routePhase);
                    Vector3 rootLocalFoot = SampleRootLocalRoute(
                        rootLocalSole,
                        routeSample,
                        loop,
                        intervals);
                    foot.RootLocalFootRoute[routeIndex][i] = rootLocalFoot;
                    foot.RootLocalHeelRoute[routeIndex][i] = SampleRootLocalRoute(
                        rootLocalHeel,
                        routeSample,
                        loop,
                        intervals);
                    foot.RootLocalToeRoute[routeIndex][i] = SampleRootLocalRoute(
                        rootLocalToe,
                        routeSample,
                        loop,
                        intervals);
                    foot.RootLocalAnkleRoute[routeIndex][i] = SampleRootLocalRoute(
                        rootLocalAnkle,
                        routeSample,
                        loop,
                        intervals);
                    foot.RootLocalKneeRoute[routeIndex][i] = SampleRootLocalRoute(
                        rootLocalKnee,
                        routeSample,
                        loop,
                        intervals);
                    foot.RootLocalHipRoute[routeIndex][i] = SampleRootLocalRoute(
                        rootLocalHip,
                        routeSample,
                        loop,
                        intervals);
                    foot.RootLocalSoleRotationRoute[routeIndex][i] = SampleRootLocalRotation(
                        rootLocalSoleRotation,
                        routeSample,
                        loop,
                        intervals);
                    foot.RootLocalAnkleRotationRoute[routeIndex][i] = SampleRootLocalRotation(
                        rootLocalAnkleRotation,
                        routeSample,
                        loop,
                        intervals);
                    foot.AuthoredFootPlanarRoute[routeIndex][i] = new Vector3(
                        rootLocalFoot.x,
                        0f,
                        rootLocalFoot.z);
                    authoredSoleHeights[routeIndex] = rootLocalFoot.y;
                    float constraintWeight = ResolveConstraintWeight(
                        routePhase,
                        foot.ReleasePhase[i],
                        foot.LiftOffPhase[i],
                        foot.ApproachContactPhase[i]);
                    float supportWeight = ResolveSupportWeight(
                        routePhase,
                        foot.ReleasePhase[i],
                        foot.LiftOffPhase[i],
                        foot.ApproachContactPhase[i]);
                    Vector3 routeHip = foot.RootLocalHipRoute[routeIndex][i];
                    Vector3 routeKnee = foot.RootLocalKneeRoute[routeIndex][i];
                    Vector3 routeAnkle = foot.RootLocalAnkleRoute[routeIndex][i];
                    float supportLegLength = Vector3.Distance(routeHip, routeAnkle);
                    foot.ConstraintWeight[routeIndex][i] = constraintWeight;
                    foot.SupportWeight[routeIndex][i] = supportWeight;
                    foot.SupportLegLength[routeIndex][i] = supportLegLength;
                    foot.SupportLegCompressionReserve[routeIndex][i] = Mathf.Max(
                        0f,
                        foot.RigLegLength - supportLegLength);
                    foot.SupportKneeBendPlane[routeIndex][i] = ResolveKneeBendPlane(
                        routeHip,
                        routeKnee,
                        routeAnkle);
                    foot.SupportFootPivotPosition[routeIndex][i] = rootLocalFoot;
                    foot.SupportFootPivotWeight[routeIndex][i] = supportWeight;

                }

                for (int routeIndex = 0;
                     routeIndex < AnimationPredictedFootStepCurveSet.RouteSampleCount;
                     routeIndex++)
                {
                    float routePhase = routeIndex /
                        (AnimationPredictedFootStepCurveSet.RouteSampleCount - 1f);
                    float footPathHeight = EvaluateVirtualGroundHeight(
                        routePhase,
                        previous,
                        opposing,
                        next,
                        rootLocalSole,
                        opposingRootLocalSole,
                        loop,
                        intervals);
                    foot.AnimationClearanceHeight[routeIndex][i] =
                        Mathf.Max(0f, authoredSoleHeights[routeIndex] - footPathHeight);
                }
            }
        }

        static float EvaluateVirtualGroundHeight(
            float routePhase,
            int previous,
            int opposing,
            int next,
            Vector3[] rootLocalSole,
            Vector3[] opposingRootLocalSole,
            bool loop,
            int intervals)
        {
            float startHeight = SampleRootLocalRoute(
                rootLocalSole,
                previous,
                loop,
                intervals).y;
            float endHeight = SampleRootLocalRoute(
                rootLocalSole,
                next,
                loop,
                intervals).y;
            if (opposing <= previous || opposing >= next)
                return Mathf.Lerp(startHeight, endHeight, Mathf.Clamp01(routePhase));
            float splitPhase = Mathf.InverseLerp(previous, next, opposing);
            float splitHeight = SampleRootLocalRoute(
                opposingRootLocalSole,
                opposing,
                loop,
                intervals).y;
            return routePhase <= splitPhase
                ? Mathf.Lerp(
                    startHeight,
                    splitHeight,
                    Mathf.InverseLerp(0f, splitPhase, routePhase))
                : Mathf.Lerp(
                    splitHeight,
                    endHeight,
                    Mathf.InverseLerp(splitPhase, 1f, routePhase));
        }

        static int ResolveApproachContact(
            int previous,
            int opposing,
            int liftOff,
            int next,
            Vector3[] rootLocalSole,
            Vector3[] opposingRootLocalSole,
            bool loop,
            int intervals)
        {
            if (liftOff >= next)
                return next;
            int approachContact = liftOff;
            float maximumClearance = float.NegativeInfinity;
            for (int sample = liftOff; sample < next; sample++)
            {
                float routePhase = Mathf.InverseLerp(previous, next, sample);
                float footPathHeight = EvaluateVirtualGroundHeight(
                    routePhase,
                    previous,
                    opposing,
                    next,
                    rootLocalSole,
                    opposingRootLocalSole,
                    loop,
                    intervals);
                float soleHeight = SampleRootLocalRoute(
                    rootLocalSole,
                    sample,
                    loop,
                    intervals).y;
                float clearance = Mathf.Max(0f, soleHeight - footPathHeight);
                if (clearance <= maximumClearance)
                    continue;
                maximumClearance = clearance;
                approachContact = sample;
            }
            return approachContact;
        }

        static void BuildPairedLandingFeatures(
            SampledFoot foot,
            SampledFoot opposingFoot,
            IReadOnlyList<Vector3> rootPositions,
            IReadOnlyList<Quaternion> rootRotations,
            IReadOnlyList<int> ownLandings,
            IReadOnlyList<int> opposingLandings,
            bool loop,
            float step)
        {
            int intervals = foot.PlantConfidence.Length - 1;
            if (opposingFoot == null || rootPositions == null || rootRotations == null ||
                opposingFoot.SolePositions.Length != intervals + 1 ||
                rootPositions.Count != intervals + 1 || rootRotations.Count != intervals + 1)
            {
                throw new ArgumentException("Foot Analysis opposing landing samples do not match.");
            }
            if (ownLandings.Count == 0 || opposingLandings.Count == 0 || intervals <= 0)
                return;
            for (int sample = 0; sample <= intervals; sample++)
            {
                if (foot.EventOrdinal[sample] <= 0f || foot.LandingDelay[sample] <= 0.000001f)
                    continue;
                int ownLanding = sample + Mathf.RoundToInt(foot.LandingDelay[sample] / step);
                int opposingLanding = -1;
                int opposingOrdinal = -1;
                for (int i = 0; i < opposingLandings.Count; i++)
                {
                    if (opposingLandings[i] <= sample)
                        continue;
                    opposingLanding = opposingLandings[i];
                    opposingOrdinal = i;
                    break;
                }
                if (opposingLanding < 0 && loop)
                {
                    opposingLanding = opposingLandings[0] + intervals;
                    opposingOrdinal = 0;
                }
                if (opposingLanding <= sample || opposingLanding >= ownLanding)
                    continue;
                int ownCycle = loop ? ownLanding / intervals : 0;
                int opposingCycle = loop ? opposingLanding / intervals : 0;
                foot.OpposingLandingDelaySeconds[sample] = (opposingLanding - sample) * step;
                foot.OpposingEventOrdinal[sample] = opposingOrdinal + 1;
                foot.OpposingLandingCycleOffset[sample] = opposingCycle - ownCycle;
                int landingSample = loop ? opposingLanding % intervals : opposingLanding;
                foot.OpposingRootLocalLanding[sample] =
                    Quaternion.Inverse(rootRotations[landingSample]) *
                    (opposingFoot.SolePositions[landingSample] - rootPositions[landingSample]);
                foot.OpposingRootLocalLandingRotation[sample] = (
                    Quaternion.Inverse(rootRotations[landingSample]) *
                    opposingFoot.SoleRotations[landingSample]).normalized;
            }
        }

        static void ValidateAuthoredLandingPair(
            IReadOnlyList<int> left,
            IReadOnlyList<int> right,
            int intervals,
            bool cyclic)
        {
            if (left.Count == 0 || right.Count == 0 || intervals <= 0)
                throw new InvalidOperationException("Foot Analysis gait requires authored landing events for both feet.");
            var events = new List<(int Sample, bool Left)>(left.Count + right.Count);
            for (int i = 0; i < left.Count; i++)
                events.Add((left[i], true));
            for (int i = 0; i < right.Count; i++)
                events.Add((right[i], false));
            events.Sort((first, second) =>
            {
                int order = first.Sample.CompareTo(second.Sample);
                return order != 0 ? order : first.Left.CompareTo(second.Left);
            });
            for (int i = 1; i < events.Count; i++)
            {
                if (events[i].Sample == events[i - 1].Sample ||
                    events[i].Left == events[i - 1].Left)
                {
                    throw new InvalidOperationException("Foot Analysis gait landing events do not alternate left and right.");
                }
            }
            if (cyclic && events[0].Left == events[events.Count - 1].Left)
                throw new InvalidOperationException("Foot Analysis cyclic gait does not preserve the left/right phase pair across the loop boundary.");
        }

        static Vector3[][] CreateVectorRoute(int sampleCount)
        {
            var route = new Vector3[AnimationPredictedFootStepCurveSet.RouteSampleCount][];
            for (int i = 0; i < route.Length; i++)
                route[i] = new Vector3[sampleCount];
            return route;
        }

        static Quaternion[][] CreateQuaternionRoute(int sampleCount)
        {
            var route = new Quaternion[AnimationPredictedFootStepCurveSet.RouteSampleCount][];
            for (int i = 0; i < route.Length; i++)
                route[i] = CreateIdentityRotations(sampleCount);
            return route;
        }

        static Quaternion[] CreateIdentityRotations(int sampleCount)
        {
            var result = new Quaternion[sampleCount];
            for (int i = 0; i < result.Length; i++)
                result[i] = Quaternion.identity;
            return result;
        }

        static float[][] CreateScalarRoute(int sampleCount)
        {
            var route = new float[AnimationPredictedFootStepCurveSet.RouteSampleCount][];
            for (int i = 0; i < route.Length; i++)
                route[i] = new float[sampleCount];
            return route;
        }

        static int CountPlantedSamples(
            float[] plantConfidence,
            int start,
            int intervals,
            bool loop)
        {
            int count = 0;
            while (count < intervals)
            {
                int index = start + count;
                if (loop)
                    index %= intervals;
                else if (index >= intervals)
                    break;
                if (plantConfidence[index] < 0.5f)
                    break;
                count++;
            }
            return count;
        }

        static Vector3 SampleRootLocalRoute(
            Vector3[] rootLocalSole,
            float sample,
            bool loop,
            int intervals)
        {
            if (!loop)
            {
                float clamped = Mathf.Clamp(sample, 0f, intervals);
                int first = Mathf.FloorToInt(clamped);
                int second = Mathf.Min(intervals, first + 1);
                return Vector3.Lerp(rootLocalSole[first], rootLocalSole[second], clamped - first);
            }
            float wrapped = sample % intervals;
            if (wrapped < 0f)
                wrapped += intervals;
            int start = Mathf.FloorToInt(wrapped);
            int end = (start + 1) % intervals;
            return Vector3.Lerp(rootLocalSole[start], rootLocalSole[end], wrapped - start);
        }

        static Quaternion SampleRootLocalRotation(
            Quaternion[] rotations,
            float sample,
            bool loop,
            int intervals)
        {
            if (!loop)
            {
                float clamped = Mathf.Clamp(sample, 0f, intervals);
                int first = Mathf.FloorToInt(clamped);
                int second = Mathf.Min(intervals, first + 1);
                return Quaternion.Slerp(rotations[first], rotations[second], clamped - first).normalized;
            }
            float wrapped = sample % intervals;
            if (wrapped < 0f)
                wrapped += intervals;
            int start = Mathf.FloorToInt(wrapped);
            int end = (start + 1) % intervals;
            return Quaternion.Slerp(rotations[start], rotations[end], wrapped - start).normalized;
        }

        static void PreserveQuaternionContinuity(Quaternion[] rotations)
        {
            for (int i = 1; i < rotations.Length; i++)
            {
                if (Quaternion.Dot(rotations[i - 1], rotations[i]) >= 0f)
                    continue;
                Quaternion value = rotations[i];
                rotations[i] = new Quaternion(-value.x, -value.y, -value.z, -value.w);
            }
        }

        static float ResolveConstraintWeight(
            float phase,
            float releasePhase,
            float liftOffPhase,
            float approachContactPhase)
        {
            if (phase < releasePhase)
                return 1f;
            if (phase < liftOffPhase)
                return 1f - Mathf.InverseLerp(releasePhase, liftOffPhase, phase);
            if (phase < approachContactPhase)
                return 0f;
            return Mathf.InverseLerp(approachContactPhase, 1f, phase);
        }

        static float ResolveSupportWeight(
            float phase,
            float releasePhase,
            float liftOffPhase,
            float approachContactPhase)
        {
            if (phase < releasePhase)
                return 1f;
            if (phase < liftOffPhase)
                return 1f - Mathf.InverseLerp(releasePhase, liftOffPhase, phase);
            if (phase < approachContactPhase)
                return 0f;
            return Mathf.InverseLerp(approachContactPhase, 1f, phase);
        }

        static Vector3 ResolveKneeBendPlane(
            Vector3 hip,
            Vector3 knee,
            Vector3 ankle)
        {
            Vector3 leg = ankle - hip;
            float denominator = leg.sqrMagnitude;
            if (denominator <= 0.00000001f)
                throw new InvalidOperationException("Foot Analysis support leg has zero length.");
            Vector3 projected = hip + leg * Mathf.Clamp01(Vector3.Dot(knee - hip, leg) / denominator);
            Vector3 bend = knee - projected;
            return bend.sqrMagnitude > 0.00000001f ? bend.normalized : Vector3.zero;
        }

        static float SampleScalarRoute(
            float[] values,
            float sample,
            bool loop,
            int intervals)
        {
            if (!loop)
            {
                float clamped = Mathf.Clamp(sample, 0f, intervals);
                int first = Mathf.FloorToInt(clamped);
                int second = Mathf.Min(intervals, first + 1);
                return Mathf.Lerp(values[first], values[second], clamped - first);
            }
            float wrapped = sample % intervals;
            if (wrapped < 0f)
                wrapped += intervals;
            int start = Mathf.FloorToInt(wrapped);
            int end = (start + 1) % intervals;
            return Mathf.Lerp(values[start], values[end], wrapped - start);
        }

        static int ResolvePreviousLanding(
            IReadOnlyList<int> starts,
            int eventIndex,
            int next,
            bool loop,
            int intervals)
        {
            if (eventIndex > 0)
                return starts[eventIndex - 1] + (loop && next >= intervals ? intervals : 0);
            if (loop && starts.Count > 0)
                return starts[starts.Count - 1] + (next >= intervals ? 0 : -intervals);
            return 0;
        }

        static int ResolveOpposingLanding(
            IReadOnlyList<int> landings,
            int previous,
            int next,
            bool loop,
            int intervals)
        {
            for (int cycle = loop ? -1 : 0; cycle <= (loop ? 1 : 0); cycle++)
            {
                int offset = cycle * intervals;
                for (int i = 0; i < landings.Count; i++)
                {
                    int candidate = landings[i] + offset;
                    if (candidate > previous && candidate < next)
                        return candidate;
                }
            }
            return -1;
        }

        static int ResolveRelease(
            float[] plantConfidence,
            int previous,
            int liftOff,
            int intervals,
            bool loop)
        {
            int first = loop ? previous + 1 : Mathf.Max(0, previous + 1);
            for (int sample = first; sample < liftOff; sample++)
            {
                int index = loop
                    ? ((sample % intervals) + intervals) % intervals
                    : Mathf.Clamp(sample, 0, intervals);
                if (plantConfidence[index] < AnimationFootConstraintFacts.LockedMinimumConfidence)
                    return sample;
            }
            return liftOff;
        }

        static int ResolveLiftOff(
            float[] plantConfidence,
            int previous,
            int next,
            int intervals,
            bool loop)
        {
            int first = loop ? previous + 1 : Mathf.Max(0, previous + 1);
            for (int sample = first; sample < next; sample++)
            {
                int index = loop
                    ? ((sample % intervals) + intervals) % intervals
                    : Mathf.Clamp(sample, 0, intervals);
                if (plantConfidence[index] < 0.5f)
                    return sample;
            }
            return next;
        }

        static AnimationFootFeatureCurveSet BuildCurveSet(
            SampledFoot foot,
            CharacterFootPlacementCurveReductionSettings reduction,
            bool loop,
            float sampleStep)
        {
            int count = foot.Velocities.Length;
            float[] x = new float[count];
            float[] y = new float[count];
            float[] z = new float[count];
            for (int i = 0; i < count; i++)
            {
                x[i] = foot.Velocities[i].x;
                y[i] = foot.Velocities[i].y;
                z[i] = foot.Velocities[i].z;
            }
            return new AnimationFootFeatureCurveSet(
                Reduce(x, reduction.VelocityTolerance),
                Reduce(y, reduction.VelocityTolerance),
                Reduce(z, reduction.VelocityTolerance),
                Reduce(foot.Heights, reduction.HeightTolerance),
                Reduce(foot.PlantConfidence, reduction.ConfidenceTolerance),
                BuildPredictedStepCurveSet(foot, reduction),
                BuildPredictedStepCurveSet(
                    BuildIncomingPrediction(foot, loop, sampleStep),
                    reduction));
        }

        static SampledFoot BuildIncomingPrediction(
            SampledFoot source,
            bool loop,
            float sampleStep)
        {
            if (!float.IsFinite(sampleStep) || sampleStep <= 0f)
                throw new ArgumentOutOfRangeException(nameof(sampleStep));
            int count = source.EventOrdinal.Length;
            int intervals = count - 1;
            var result = CreatePredictionFoot(count);
            for (int sample = 0; sample < count; sample++)
            {
                if (source.EventOrdinal[sample] <= 0f)
                    continue;
                int currentLanding = sample + Mathf.Max(
                    0,
                    Mathf.RoundToInt(source.LandingDelay[sample] / sampleStep));
                int lastCandidate = loop
                    ? currentLanding + intervals
                    : intervals;
                for (int candidate = currentLanding + 1; candidate <= lastCandidate; candidate++)
                {
                    int candidateIndex = loop ? candidate % intervals : candidate;
                    if (source.EventOrdinal[candidateIndex] <= 0f ||
                        source.LandingConfidence[candidateIndex] <= 0f ||
                        source.EventPhase[candidateIndex] >= source.LiftOffPhase[candidateIndex] ||
                        source.LandingDelay[candidateIndex] <= sampleStep)
                    {
                        continue;
                    }
                    int candidateLanding = candidate + Mathf.Max(
                        0,
                        Mathf.RoundToInt(source.LandingDelay[candidateIndex] / sampleStep));
                    if (candidateLanding <= currentLanding)
                        continue;
                    CopyPredictionSample(
                        source,
                        candidateIndex,
                        result,
                        sample,
                        (candidate - sample) * sampleStep,
                        loop ? candidateLanding / intervals : 0);
                    break;
                }
            }
            return result;
        }

        static SampledFoot CreatePredictionFoot(int sampleCount) => new SampledFoot
        {
            LandingConfidence = new float[sampleCount],
            LandingDelay = new float[sampleCount],
            EventPhase = new float[sampleCount],
            ReleasePhase = new float[sampleCount],
            LiftOffPhase = new float[sampleCount],
            ApproachContactPhase = new float[sampleCount],
            ActionStepDurationSeconds = new float[sampleCount],
            EventOrdinal = new float[sampleCount],
            SourceLandingCycleOffset = new float[sampleCount],
            OpposingLandingDelaySeconds = new float[sampleCount],
            OpposingEventOrdinal = new float[sampleCount],
            OpposingLandingCycleOffset = new float[sampleCount],
            OpposingRootLocalLanding = new Vector3[sampleCount],
            OpposingRootLocalLandingRotation = CreateIdentityRotations(sampleCount),
            RootLocalFootRoute = CreateVectorRoute(sampleCount),
            RootLocalHeelRoute = CreateVectorRoute(sampleCount),
            RootLocalToeRoute = CreateVectorRoute(sampleCount),
            RootLocalAnkleRoute = CreateVectorRoute(sampleCount),
            RootLocalKneeRoute = CreateVectorRoute(sampleCount),
            RootLocalHipRoute = CreateVectorRoute(sampleCount),
            RootLocalSoleRotationRoute = CreateQuaternionRoute(sampleCount),
            RootLocalAnkleRotationRoute = CreateQuaternionRoute(sampleCount),
            AuthoredFootPlanarRoute = CreateVectorRoute(sampleCount),
            AnimationClearanceHeight = CreateScalarRoute(sampleCount),
            ConstraintWeight = CreateScalarRoute(sampleCount),
            SupportWeight = CreateScalarRoute(sampleCount),
            SupportLegLength = CreateScalarRoute(sampleCount),
            SupportLegCompressionReserve = CreateScalarRoute(sampleCount),
            SupportKneeBendPlane = CreateVectorRoute(sampleCount),
            SupportFootPivotPosition = CreateVectorRoute(sampleCount),
            SupportFootPivotWeight = CreateScalarRoute(sampleCount)
        };

        static void CopyPredictionSample(
            SampledFoot source,
            int sourceIndex,
            SampledFoot destination,
            int destinationIndex,
            float scheduleDelay,
            int sourceLandingCycleOffset)
        {
            destination.LandingConfidence[destinationIndex] = source.LandingConfidence[sourceIndex];
            destination.LandingDelay[destinationIndex] = source.LandingDelay[sourceIndex] + scheduleDelay;
            destination.EventPhase[destinationIndex] = source.EventPhase[sourceIndex];
            destination.ReleasePhase[destinationIndex] = source.ReleasePhase[sourceIndex];
            destination.LiftOffPhase[destinationIndex] = source.LiftOffPhase[sourceIndex];
            destination.ApproachContactPhase[destinationIndex] = source.ApproachContactPhase[sourceIndex];
            destination.ActionStepDurationSeconds[destinationIndex] = source.ActionStepDurationSeconds[sourceIndex];
            destination.EventOrdinal[destinationIndex] = source.EventOrdinal[sourceIndex];
            destination.SourceLandingCycleOffset[destinationIndex] = sourceLandingCycleOffset;
            destination.OpposingLandingDelaySeconds[destinationIndex] =
                source.OpposingEventOrdinal[sourceIndex] > 0f
                    ? source.OpposingLandingDelaySeconds[sourceIndex] + scheduleDelay
                    : 0f;
            destination.OpposingEventOrdinal[destinationIndex] = source.OpposingEventOrdinal[sourceIndex];
            destination.OpposingLandingCycleOffset[destinationIndex] = source.OpposingLandingCycleOffset[sourceIndex];
            destination.OpposingRootLocalLanding[destinationIndex] = source.OpposingRootLocalLanding[sourceIndex];
            destination.OpposingRootLocalLandingRotation[destinationIndex] = source.OpposingRootLocalLandingRotation[sourceIndex];
            for (int routeIndex = 0; routeIndex < AnimationPredictedFootStepCurveSet.RouteSampleCount; routeIndex++)
            {
                destination.RootLocalFootRoute[routeIndex][destinationIndex] = source.RootLocalFootRoute[routeIndex][sourceIndex];
                destination.RootLocalHeelRoute[routeIndex][destinationIndex] = source.RootLocalHeelRoute[routeIndex][sourceIndex];
                destination.RootLocalToeRoute[routeIndex][destinationIndex] = source.RootLocalToeRoute[routeIndex][sourceIndex];
                destination.RootLocalAnkleRoute[routeIndex][destinationIndex] = source.RootLocalAnkleRoute[routeIndex][sourceIndex];
                destination.RootLocalKneeRoute[routeIndex][destinationIndex] = source.RootLocalKneeRoute[routeIndex][sourceIndex];
                destination.RootLocalHipRoute[routeIndex][destinationIndex] = source.RootLocalHipRoute[routeIndex][sourceIndex];
                destination.RootLocalSoleRotationRoute[routeIndex][destinationIndex] = source.RootLocalSoleRotationRoute[routeIndex][sourceIndex];
                destination.RootLocalAnkleRotationRoute[routeIndex][destinationIndex] = source.RootLocalAnkleRotationRoute[routeIndex][sourceIndex];
                destination.AuthoredFootPlanarRoute[routeIndex][destinationIndex] = source.AuthoredFootPlanarRoute[routeIndex][sourceIndex];
                destination.AnimationClearanceHeight[routeIndex][destinationIndex] = source.AnimationClearanceHeight[routeIndex][sourceIndex];
                destination.ConstraintWeight[routeIndex][destinationIndex] = source.ConstraintWeight[routeIndex][sourceIndex];
                destination.SupportWeight[routeIndex][destinationIndex] = source.SupportWeight[routeIndex][sourceIndex];
                destination.SupportLegLength[routeIndex][destinationIndex] = source.SupportLegLength[routeIndex][sourceIndex];
                destination.SupportLegCompressionReserve[routeIndex][destinationIndex] = source.SupportLegCompressionReserve[routeIndex][sourceIndex];
                destination.SupportKneeBendPlane[routeIndex][destinationIndex] = source.SupportKneeBendPlane[routeIndex][sourceIndex];
                destination.SupportFootPivotPosition[routeIndex][destinationIndex] = source.SupportFootPivotPosition[routeIndex][sourceIndex];
                destination.SupportFootPivotWeight[routeIndex][destinationIndex] = source.SupportFootPivotWeight[routeIndex][sourceIndex];
            }
        }

        static AnimationPredictedFootStepCurveSet BuildPredictedStepCurveSet(
            SampledFoot foot,
            CharacterFootPlacementCurveReductionSettings reduction)
        {
            int count = foot.EventOrdinal.Length;
            float[] opposingLandingX = new float[count];
            float[] opposingLandingY = new float[count];
            float[] opposingLandingZ = new float[count];
            float[] opposingLandingRotationX = new float[count];
            float[] opposingLandingRotationY = new float[count];
            float[] opposingLandingRotationZ = new float[count];
            float[] opposingLandingRotationW = new float[count];
            float[] landingPhase = new float[count];
            for (int i = 0; i < count; i++)
            {
                opposingLandingX[i] = foot.OpposingRootLocalLanding[i].x;
                opposingLandingY[i] = foot.OpposingRootLocalLanding[i].y;
                opposingLandingZ[i] = foot.OpposingRootLocalLanding[i].z;
                opposingLandingRotationX[i] = foot.OpposingRootLocalLandingRotation[i].x;
                opposingLandingRotationY[i] = foot.OpposingRootLocalLandingRotation[i].y;
                opposingLandingRotationZ[i] = foot.OpposingRootLocalLandingRotation[i].z;
                opposingLandingRotationW[i] = foot.OpposingRootLocalLandingRotation[i].w;
                landingPhase[i] = foot.EventOrdinal[i] > 0f ? 1f : 0f;
            }
            bool[] eventBoundaries = ResolveEventBoundaries(
                foot.EventPhase,
                foot.EventOrdinal,
                foot.SourceLandingCycleOffset);
            AnimationCurve[] routeX = BuildRouteCurves(foot.RootLocalFootRoute, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] routeY = BuildRouteCurves(foot.RootLocalFootRoute, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] routeZ = BuildRouteCurves(foot.RootLocalFootRoute, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] ankleRouteX = BuildRouteCurves(foot.RootLocalAnkleRoute, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] ankleRouteY = BuildRouteCurves(foot.RootLocalAnkleRoute, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] ankleRouteZ = BuildRouteCurves(foot.RootLocalAnkleRoute, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] hipRouteX = BuildRouteCurves(foot.RootLocalHipRoute, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] hipRouteY = BuildRouteCurves(foot.RootLocalHipRoute, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] hipRouteZ = BuildRouteCurves(foot.RootLocalHipRoute, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] authoredFootPlanarX = BuildRouteCurves(foot.AuthoredFootPlanarRoute, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] authoredFootPlanarZ = BuildRouteCurves(foot.AuthoredFootPlanarRoute, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] animationClearanceHeight = BuildRouteCurves(foot.AnimationClearanceHeight, reduction.HeightTolerance, eventBoundaries);
            AnimationCurve[] heelRouteX = BuildRouteCurves(foot.RootLocalHeelRoute, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] heelRouteY = BuildRouteCurves(foot.RootLocalHeelRoute, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] heelRouteZ = BuildRouteCurves(foot.RootLocalHeelRoute, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] toeRouteX = BuildRouteCurves(foot.RootLocalToeRoute, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] toeRouteY = BuildRouteCurves(foot.RootLocalToeRoute, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] toeRouteZ = BuildRouteCurves(foot.RootLocalToeRoute, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] kneeRouteX = BuildRouteCurves(foot.RootLocalKneeRoute, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] kneeRouteY = BuildRouteCurves(foot.RootLocalKneeRoute, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] kneeRouteZ = BuildRouteCurves(foot.RootLocalKneeRoute, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[][] soleRotation = BuildRouteCurves(foot.RootLocalSoleRotationRoute, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[][] ankleRotation = BuildRouteCurves(foot.RootLocalAnkleRotationRoute, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] constraintWeight = BuildRouteCurves(foot.ConstraintWeight, reduction.ConfidenceTolerance, eventBoundaries);
            AnimationCurve[] supportWeight = BuildRouteCurves(foot.SupportWeight, reduction.ConfidenceTolerance, eventBoundaries);
            AnimationCurve[] supportLegLength = BuildRouteCurves(foot.SupportLegLength, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] supportLegCompressionReserve = BuildRouteCurves(foot.SupportLegCompressionReserve, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] kneeBendPlaneX = BuildRouteCurves(foot.SupportKneeBendPlane, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] kneeBendPlaneY = BuildRouteCurves(foot.SupportKneeBendPlane, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] kneeBendPlaneZ = BuildRouteCurves(foot.SupportKneeBendPlane, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] pivotPositionX = BuildRouteCurves(foot.SupportFootPivotPosition, 0, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] pivotPositionY = BuildRouteCurves(foot.SupportFootPivotPosition, 1, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] pivotPositionZ = BuildRouteCurves(foot.SupportFootPivotPosition, 2, reduction.LandingOffsetTolerance, eventBoundaries);
            AnimationCurve[] pivotWeight = BuildRouteCurves(foot.SupportFootPivotWeight, reduction.ConfidenceTolerance, eventBoundaries);
            var biomechanical = new AnimationFootBiomechanicalStepCurveSet(
                ReduceEventScoped(landingPhase, 0f, eventBoundaries),
                ReduceStep(opposingLandingRotationX),
                ReduceStep(opposingLandingRotationY),
                ReduceStep(opposingLandingRotationZ),
                ReduceStep(opposingLandingRotationW),
                new[]
                {
                    heelRouteX, heelRouteY, heelRouteZ,
                    toeRouteX, toeRouteY, toeRouteZ,
                    kneeRouteX, kneeRouteY, kneeRouteZ,
                    soleRotation[0], soleRotation[1], soleRotation[2], soleRotation[3],
                    ankleRotation[0], ankleRotation[1], ankleRotation[2], ankleRotation[3],
                    kneeBendPlaneX, kneeBendPlaneY, kneeBendPlaneZ,
                    pivotPositionX, pivotPositionY, pivotPositionZ
                },
                constraintWeight,
                supportWeight,
                supportLegLength,
                supportLegCompressionReserve,
                pivotWeight);
            return new AnimationPredictedFootStepCurveSet(
                ReduceEventScoped(foot.LandingConfidence, reduction.ConfidenceTolerance, eventBoundaries),
                ReduceEventScoped(foot.LandingDelay, reduction.LandingDelayTolerance, eventBoundaries),
                ReduceEventScoped(foot.EventPhase, reduction.ConfidenceTolerance, eventBoundaries),
                ReduceEventScoped(foot.ReleasePhase, reduction.ConfidenceTolerance, eventBoundaries),
                ReduceEventScoped(foot.LiftOffPhase, reduction.ConfidenceTolerance, eventBoundaries),
                ReduceEventScoped(foot.ApproachContactPhase, reduction.ConfidenceTolerance, eventBoundaries),
                ReduceEventScoped(foot.ActionStepDurationSeconds, reduction.LandingDelayTolerance, eventBoundaries),
                ReduceEventScoped(foot.EventOrdinal, 0f, eventBoundaries),
                ReduceDiscrete(foot.SourceLandingCycleOffset),
                ReduceEventScoped(foot.OpposingLandingDelaySeconds, reduction.LandingDelayTolerance, eventBoundaries),
                ReduceDiscrete(foot.OpposingEventOrdinal),
                ReduceDiscrete(foot.OpposingLandingCycleOffset),
                ReduceStep(opposingLandingX),
                ReduceStep(opposingLandingY),
                ReduceStep(opposingLandingZ),
                routeX,
                routeY,
                routeZ,
                ankleRouteX,
                ankleRouteY,
                ankleRouteZ,
                hipRouteX,
                hipRouteY,
                hipRouteZ,
                authoredFootPlanarX,
                authoredFootPlanarZ,
                animationClearanceHeight,
                biomechanical);
        }

        static AnimationCurve[] BuildRouteCurves(
            Vector3[][] route,
            int axis,
            float tolerance,
            bool[] eventBoundaries)
        {
            var curves = new AnimationCurve[route.Length];
            for (int routeIndex = 0; routeIndex < route.Length; routeIndex++)
            {
                Vector3[] samples = route[routeIndex];
                var values = new float[samples.Length];
                for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
                {
                    values[sampleIndex] = axis == 0
                        ? samples[sampleIndex].x
                        : axis == 1
                            ? samples[sampleIndex].y
                            : samples[sampleIndex].z;
                }
                curves[routeIndex] = ReduceEventScoped(values, tolerance, eventBoundaries);
            }
            return curves;
        }

        static AnimationCurve[] BuildRouteCurves(
            float[][] route,
            float tolerance,
            bool[] eventBoundaries)
        {
            var curves = new AnimationCurve[route.Length];
            for (int routeIndex = 0; routeIndex < route.Length; routeIndex++)
                curves[routeIndex] = ReduceEventScoped(route[routeIndex], tolerance, eventBoundaries);
            return curves;
        }

        static AnimationCurve[][] BuildRouteCurves(
            Quaternion[][] route,
            float tolerance,
            bool[] eventBoundaries)
        {
            var result = new AnimationCurve[4][];
            for (int axis = 0; axis < result.Length; axis++)
            {
                var curves = new AnimationCurve[route.Length];
                for (int routeIndex = 0; routeIndex < route.Length; routeIndex++)
                {
                    Quaternion[] samples = route[routeIndex];
                    var values = new float[samples.Length];
                    for (int sampleIndex = 0; sampleIndex < samples.Length; sampleIndex++)
                    {
                        Quaternion value = samples[sampleIndex];
                        values[sampleIndex] = axis == 0
                            ? value.x
                            : axis == 1
                                ? value.y
                                : axis == 2
                                    ? value.z
                                    : value.w;
                    }
                    curves[routeIndex] = ReduceEventScoped(values, tolerance, eventBoundaries);
                }
                result[axis] = curves;
            }
            return result;
        }

        static bool[] ResolveEventBoundaries(
            float[] eventPhase,
            float[] eventOrdinal,
            float[] sourceLandingCycleOffset)
        {
            if (eventPhase == null || eventOrdinal == null || sourceLandingCycleOffset == null ||
                eventPhase.Length != eventOrdinal.Length ||
                eventPhase.Length != sourceLandingCycleOffset.Length)
                throw new InvalidOperationException("Foot Analysis event boundary input is invalid.");
            var result = new bool[eventPhase.Length];
            for (int i = 1; i < result.Length; i++)
            {
                result[i] = eventPhase[i] + 0.0001f < eventPhase[i - 1] ||
                            Mathf.RoundToInt(eventOrdinal[i]) != Mathf.RoundToInt(eventOrdinal[i - 1]) ||
                            Mathf.RoundToInt(sourceLandingCycleOffset[i]) !=
                            Mathf.RoundToInt(sourceLandingCycleOffset[i - 1]);
            }
            return result;
        }

        static AnimationCurve Reduce(float[] values, float tolerance)
        {
            if (values == null || values.Length < 2)
                throw new InvalidOperationException("Foot Analysis curve requires at least two samples.");
            var keep = new bool[values.Length];
            keep[0] = true;
            keep[values.Length - 1] = true;
            Reduce(values, 0, values.Length - 1, tolerance, keep);
            var keys = new List<Keyframe>();
            float denominator = values.Length - 1f;
            for (int i = 0; i < values.Length; i++)
            {
                if (keep[i])
                    keys.Add(new Keyframe(i / denominator, values[i]));
            }
            var curve = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        static AnimationCurve ReduceEventScoped(
            float[] values,
            float tolerance,
            bool[] eventBoundaries)
        {
            if (values == null || values.Length < 2 ||
                eventBoundaries == null || eventBoundaries.Length != values.Length)
            {
                throw new InvalidOperationException("Foot Analysis event curve input is invalid.");
            }
            var keep = new bool[values.Length];
            keep[0] = true;
            keep[values.Length - 1] = true;
            int segmentStart = 0;
            for (int i = 1; i < values.Length; i++)
            {
                if (!eventBoundaries[i])
                    continue;
                keep[i - 1] = true;
                keep[i] = true;
                Reduce(values, segmentStart, i - 1, tolerance, keep);
                segmentStart = i;
            }
            Reduce(values, segmentStart, values.Length - 1, tolerance, keep);
            var keys = new List<Keyframe>();
            var sourceIndices = new List<int>();
            float denominator = values.Length - 1f;
            for (int i = 0; i < values.Length; i++)
            {
                if (!keep[i])
                    continue;
                keys.Add(new Keyframe(i / denominator, values[i]));
                sourceIndices.Add(i);
            }
            var curve = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            for (int i = 1; i < sourceIndices.Count; i++)
            {
                int sourceIndex = sourceIndices[i];
                if (!eventBoundaries[sourceIndex])
                    continue;
                AnimationUtility.SetKeyRightTangentMode(curve, i - 1, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }
            return curve;
        }

        static AnimationCurve ReduceDiscrete(float[] values)
        {
            if (values == null || values.Length < 2)
                throw new InvalidOperationException("Foot Analysis discrete curve requires at least two samples.");
            var keys = new List<Keyframe>();
            float denominator = values.Length - 1f;
            keys.Add(new Keyframe(0f, values[0]));
            for (int i = 1; i < values.Length; i++)
            {
                if (Mathf.RoundToInt(values[i]) == Mathf.RoundToInt(values[i - 1]))
                    continue;
                float previousTime = (i - 1) / denominator;
                if (keys[keys.Count - 1].time < previousTime)
                    keys.Add(new Keyframe(previousTime, values[i - 1]));
                keys.Add(new Keyframe(i / denominator, values[i]));
            }
            if (keys[keys.Count - 1].time < 1f)
                keys.Add(new Keyframe(1f, values[values.Length - 1]));
            var curve = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }
            return curve;
        }

        static AnimationCurve ReduceStep(float[] values)
        {
            if (values == null || values.Length < 2)
                throw new InvalidOperationException("Foot Analysis step curve requires at least two samples.");
            var keys = new List<Keyframe>();
            float denominator = values.Length - 1f;
            keys.Add(new Keyframe(0f, values[0]));
            for (int i = 1; i < values.Length; i++)
            {
                if (Mathf.Abs(values[i] - values[i - 1]) <= 0.000001f)
                    continue;
                float previousTime = (i - 1) / denominator;
                if (keys[keys.Count - 1].time < previousTime)
                    keys.Add(new Keyframe(previousTime, values[i - 1]));
                keys.Add(new Keyframe(i / denominator, values[i]));
            }
            if (keys[keys.Count - 1].time < 1f)
                keys.Add(new Keyframe(1f, values[values.Length - 1]));
            var curve = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }
            return curve;
        }

        static void Reduce(float[] values, int first, int last, float tolerance, bool[] keep)
        {
            if (last <= first + 1)
                return;
            float start = values[first];
            float end = values[last];
            float maximumError = tolerance;
            int maximumIndex = -1;
            float span = last - first;
            for (int i = first + 1; i < last; i++)
            {
                float expected = Mathf.LerpUnclamped(start, end, (i - first) / span);
                float error = Mathf.Abs(values[i] - expected);
                if (error > maximumError)
                {
                    maximumError = error;
                    maximumIndex = i;
                }
            }
            if (maximumIndex < 0)
                return;
            keep[maximumIndex] = true;
            Reduce(values, first, maximumIndex, tolerance, keep);
            Reduce(values, maximumIndex, last, tolerance, keep);
        }

        static void RequireFinite(Vector3 value, string field, int sample)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new InvalidOperationException($"Foot Analysis {field} sample #{sample} is not finite.");
        }

        static void RequireFinite(Quaternion value, string field, int sample)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || !float.IsFinite(value.w) ||
                Quaternion.Dot(value, value) <= 0.000001f)
            {
                throw new InvalidOperationException($"Foot Analysis {field} sample #{sample} is not finite.");
            }
        }
    }
}
