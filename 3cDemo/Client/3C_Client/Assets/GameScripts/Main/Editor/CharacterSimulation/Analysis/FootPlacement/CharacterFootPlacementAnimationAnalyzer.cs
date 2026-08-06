using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
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
            public Vector3[] SolePositions;
            public Vector3[] Velocities;
            public float[] Heights;
            public float[] PlantConfidence;
            public float[] LandingConfidence;
            public float[] LandingDelay;
            public Vector2[] LandingOffset;
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

        public static AnimationFootFeaturePair Analyze(
            UnityEngine.AnimationClip clip,
            CharacterFootPlacementAnalysisSource source)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (!source)
                throw new ArgumentNullException(nameof(source));
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
                return AnalyzeClip(samplingContext, source, clip);
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

        static AnimationFootFeaturePair AnalyzeClip(
            SamplingContext samplingContext,
            CharacterFootPlacementAnalysisSource source,
            UnityEngine.AnimationClip clip)
        {
            if (!float.IsFinite(clip.length) || clip.length <= 0f)
                throw new InvalidOperationException("AnimationClip duration is not finite and positive");
            samplingContext.BeginClip(clip);
            int intervals = Mathf.Max(2, Mathf.CeilToInt(clip.length * source.SampleRate));
            int sampleCount = intervals + 1;
            float step = clip.length / intervals;
            var leftHeelPositions = new Vector3[sampleCount];
            var leftToePositions = new Vector3[sampleCount];
            var rightHeelPositions = new Vector3[sampleCount];
            var rightToePositions = new Vector3[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                CharacterFootPlacementAnimatedPose pose = samplingContext.Sample(i * step, (ulong)i + 1UL);
                leftHeelPositions[i] = samplingContext.ToVisualRootLocal(pose.Left.HeelPosition);
                leftToePositions[i] = samplingContext.ToVisualRootLocal(pose.Left.ToePosition);
                rightHeelPositions[i] = samplingContext.ToVisualRootLocal(pose.Right.HeelPosition);
                rightToePositions[i] = samplingContext.ToVisualRootLocal(pose.Right.ToePosition);
                RequireFinite(leftHeelPositions[i], "left heel position", i);
                RequireFinite(leftToePositions[i], "left toe position", i);
                RequireFinite(rightHeelPositions[i], "right heel position", i);
                RequireFinite(rightToePositions[i], "right toe position", i);
            }

            SampledFoot left = AnalyzeFoot(
                leftHeelPositions,
                leftToePositions,
                samplingContext.GroundReferenceHeight,
                clip.isLooping,
                step,
                source);
            SampledFoot right = AnalyzeFoot(
                rightHeelPositions,
                rightToePositions,
                samplingContext.GroundReferenceHeight,
                clip.isLooping,
                step,
                source);
            AnimationFootFeaturePair features = new AnimationFootFeaturePair(
                BuildCurveSet(left, source.Reduction),
                BuildCurveSet(right, source.Reduction));
            return features;
        }

        static SampledFoot AnalyzeFoot(
            Vector3[] heelPositions,
            Vector3[] toePositions,
            float groundReferenceHeight,
            bool loop,
            float step,
            CharacterFootPlacementAnalysisSource source)
        {
            if (heelPositions == null || toePositions == null || heelPositions.Length != toePositions.Length)
                throw new ArgumentException("Foot Analysis heel/toe sample counts do not match.");
            int last = heelPositions.Length - 1;
            var positions = new Vector3[heelPositions.Length];
            var result = new SampledFoot
            {
                SolePositions = positions,
                Velocities = new Vector3[positions.Length],
                Heights = new float[positions.Length],
                PlantConfidence = new float[positions.Length],
                LandingConfidence = new float[positions.Length],
                LandingDelay = new float[positions.Length],
                LandingOffset = new Vector2[positions.Length]
            };
            for (int i = 0; i <= last; i++)
            {
                positions[i] = (heelPositions[i] + toePositions[i]) * 0.5f;
                result.Heights[i] = Mathf.Min(heelPositions[i].y, toePositions[i].y);
            }
            for (int i = 0; i <= last; i++)
            {
                if (loop && (i == 0 || i == last))
                    result.Velocities[i] = (positions[1] - positions[last - 1]) / (2f * step);
                else if (i == 0)
                    result.Velocities[i] = (positions[1] - positions[0]) / step;
                else if (i == last)
                    result.Velocities[i] = (positions[last] - positions[last - 1]) / step;
                else
                    result.Velocities[i] = (positions[i + 1] - positions[i - 1]) / (2f * step);
                RequireFinite(result.Velocities[i], "sole velocity", i);
            }

            CharacterFootPlacementAnalysisThresholds thresholds = source.Thresholds;
            float[] contactVerticalSpeeds = BuildContactVerticalSpeeds(result.Heights, loop, step);
            BuildPlantConfidence(
                result.PlantConfidence,
                result.Heights,
                contactVerticalSpeeds,
                groundReferenceHeight,
                loop,
                thresholds);
            BuildLandingFeatures(result, loop, step, thresholds);
            return result;
        }

        static float[] BuildContactVerticalSpeeds(float[] heights, bool loop, float step)
        {
            int last = heights.Length - 1;
            var speeds = new float[heights.Length];
            for (int i = 0; i <= last; i++)
            {
                float velocity;
                if (loop && (i == 0 || i == last))
                    velocity = (heights[1] - heights[last - 1]) / (2f * step);
                else if (i == 0)
                    velocity = (heights[1] - heights[0]) / step;
                else if (i == last)
                    velocity = (heights[last] - heights[last - 1]) / step;
                else
                    velocity = (heights[i + 1] - heights[i - 1]) / (2f * step);
                speeds[i] = Mathf.Abs(velocity);
                if (!float.IsFinite(speeds[i]))
                    throw new InvalidOperationException($"Foot Analysis contact vertical speed sample #{i} is not finite.");
            }
            return speeds;
        }

        static void BuildPlantConfidence(
            float[] confidence,
            float[] heights,
            float[] contactVerticalSpeeds,
            float groundReferenceHeight,
            bool loop,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            int intervals = confidence.Length - 1;
            if (!loop)
            {
                bool planted = false;
                for (int i = 0; i <= intervals; i++)
                    confidence[i] = EvaluatePlantSample(
                        ref planted,
                        heights[i],
                        contactVerticalSpeeds[i],
                        groundReferenceHeight,
                        thresholds);
                return;
            }

            int releaseSample = -1;
            bool hasEnterEvidence = false;
            for (int i = 0; i < intervals; i++)
            {
                float clearance = Mathf.Max(0f, heights[i] - groundReferenceHeight);
                float verticalSpeed = contactVerticalSpeeds[i];
                hasEnterEvidence |= verticalSpeed <= thresholds.PlantEnterVerticalSpeed &&
                                    clearance <= thresholds.PlantEnterHeight;
                if (verticalSpeed >= thresholds.PlantExitVerticalSpeed || clearance >= thresholds.PlantExitHeight)
                    releaseSample = i;
            }

            bool loopPlanted = releaseSample < 0 && hasEnterEvidence;
            int start = releaseSample < 0 ? 0 : (releaseSample + 1) % intervals;
            for (int offset = 0; offset < intervals; offset++)
            {
                int i = (start + offset) % intervals;
                confidence[i] = EvaluatePlantSample(
                    ref loopPlanted,
                    heights[i],
                    contactVerticalSpeeds[i],
                    groundReferenceHeight,
                    thresholds);
            }
            confidence[intervals] = confidence[0];
        }

        static float EvaluatePlantSample(
            ref bool planted,
            float height,
            float verticalSpeed,
            float groundReferenceHeight,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            float clearance = Mathf.Max(0f, height - groundReferenceHeight);
            if (!planted && verticalSpeed <= thresholds.PlantEnterVerticalSpeed && clearance <= thresholds.PlantEnterHeight)
                planted = true;
            else if (planted && (verticalSpeed >= thresholds.PlantExitVerticalSpeed || clearance >= thresholds.PlantExitHeight))
                planted = false;
            float speedFactor = Mathf.InverseLerp(
                thresholds.PlantExitVerticalSpeed,
                thresholds.PlantEnterVerticalSpeed,
                verticalSpeed);
            float heightFactor = Mathf.InverseLerp(thresholds.PlantExitHeight, thresholds.PlantEnterHeight, clearance);
            float value = Mathf.Clamp01(Mathf.Min(speedFactor, heightFactor));
            return planted ? Mathf.Max(0.5f, value) : Mathf.Min(0.499f, value);
        }

        static void BuildLandingFeatures(
            SampledFoot foot,
            bool loop,
            float step,
            CharacterFootPlacementAnalysisThresholds thresholds)
        {
            int intervals = foot.PlantConfidence.Length - 1;
            int minimumSamples = Mathf.Max(1, Mathf.CeilToInt(thresholds.MinimumLandingSegmentSeconds / step));
            var starts = new List<int>();
            bool allPlanted = true;
            for (int i = 0; i < intervals; i++)
            {
                bool current = foot.PlantConfidence[i] >= 0.5f;
                allPlanted &= current;
                bool previous = i > 0
                    ? foot.PlantConfidence[i - 1] >= 0.5f
                    : loop && foot.PlantConfidence[intervals - 1] >= 0.5f;
                if (!current || previous)
                    continue;
                int count = 0;
                while (count < intervals && foot.PlantConfidence[(i + count) % intervals] >= 0.5f)
                    count++;
                if (count >= minimumSamples)
                    starts.Add(i);
            }
            if (allPlanted)
                starts.Add(0);

            for (int i = 0; i <= intervals; i++)
            {
                int sample = i == intervals && loop ? 0 : i;
                int next = -1;
                for (int startIndex = 0; startIndex < starts.Count; startIndex++)
                {
                    if (starts[startIndex] >= sample)
                    {
                        next = starts[startIndex];
                        break;
                    }
                }
                if (next < 0 && loop && starts.Count > 0)
                    next = starts[0] + intervals;
                if (next < 0)
                    continue;
                float delay = (next - sample) * step;
                if (delay < 0f || delay > thresholds.MaximumLandingSearchSeconds)
                    continue;
                int landingSample = next % intervals;
                foot.LandingConfidence[i] = foot.PlantConfidence[landingSample];
                foot.LandingDelay[i] = delay;
                foot.LandingOffset[i] = new Vector2(
                    foot.SolePositions[landingSample].x,
                    foot.SolePositions[landingSample].z);
            }
        }

        static AnimationFootFeatureCurveSet BuildCurveSet(
            SampledFoot foot,
            CharacterFootPlacementCurveReductionSettings reduction)
        {
            int count = foot.SolePositions.Length;
            float[] x = new float[count];
            float[] y = new float[count];
            float[] z = new float[count];
            float[] offsetX = new float[count];
            float[] offsetZ = new float[count];
            for (int i = 0; i < count; i++)
            {
                x[i] = foot.Velocities[i].x;
                y[i] = foot.Velocities[i].y;
                z[i] = foot.Velocities[i].z;
                offsetX[i] = foot.LandingOffset[i].x;
                offsetZ[i] = foot.LandingOffset[i].y;
            }
            return new AnimationFootFeatureCurveSet(
                Reduce(x, reduction.VelocityTolerance),
                Reduce(y, reduction.VelocityTolerance),
                Reduce(z, reduction.VelocityTolerance),
                Reduce(foot.Heights, reduction.HeightTolerance),
                Reduce(foot.PlantConfidence, reduction.ConfidenceTolerance),
                Reduce(foot.LandingConfidence, reduction.ConfidenceTolerance),
                Reduce(foot.LandingDelay, reduction.LandingDelayTolerance),
                Reduce(offsetX, reduction.LandingOffsetTolerance),
                Reduce(offsetZ, reduction.LandingOffsetTolerance));
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
    }
}
