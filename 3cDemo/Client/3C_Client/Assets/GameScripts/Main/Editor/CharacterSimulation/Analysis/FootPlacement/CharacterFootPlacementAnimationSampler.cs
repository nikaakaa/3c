using System;
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
using static ThirdPersonCharacter.Pipeline.Simulation.Editor.CharacterFootPlacementSampleValidation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal readonly struct CharacterFootPlacementFootSamples
    {
        internal readonly Vector3[] HeelPositions;
        internal readonly Vector3[] ToePositions;
        internal readonly Vector3[] AnklePositions;
        internal readonly Vector3[] KneePositions;
        internal readonly Vector3[] HipPositions;
        internal readonly Quaternion[] SoleRotations;
        internal readonly Quaternion[] AnkleRotations;

        internal CharacterFootPlacementFootSamples(int count)
        {
            HeelPositions = new Vector3[count];
            ToePositions = new Vector3[count];
            AnklePositions = new Vector3[count];
            KneePositions = new Vector3[count];
            HipPositions = new Vector3[count];
            SoleRotations = new Quaternion[count];
            AnkleRotations = new Quaternion[count];
        }
    }

    internal readonly struct CharacterFootPlacementClipSamples
    {
        internal readonly CharacterFootPlacementFootSamples Left;
        internal readonly CharacterFootPlacementFootSamples Right;
        internal readonly Vector3[] RootPositions;
        internal readonly Quaternion[] RootRotations;
        internal readonly float DurationSeconds;
        internal readonly float Step;

        internal CharacterFootPlacementClipSamples(float durationSeconds, int intervals)
        {
            int count = intervals + 1;
            Left = new CharacterFootPlacementFootSamples(count);
            Right = new CharacterFootPlacementFootSamples(count);
            RootPositions = new Vector3[count];
            RootRotations = new Quaternion[count];
            DurationSeconds = durationSeconds;
            Step = durationSeconds / intervals;
        }
    }

    internal sealed class CharacterFootPlacementAnimationSampler : IDisposable
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
        int m_MotionRootPhysicalBoneIndex = -1;

        internal float GroundReferenceHeight { get; private set; }
        internal CharacterFootPlacementRigGeometryReport CalibrationGeometryReport { get; private set; }
        internal float LeftLegLength => m_Binding.LeftLegLength;
        internal float RightLegLength => m_Binding.RightLegLength;

        internal CharacterFootPlacementAnimationSampler(
            GameObject rigPrefab,
            CharacterFootPlacementAnalysisSource source,
            bool calibrationAuthoring = false)
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
                m_MotionRootPhysicalBoneIndex = calibrationAuthoring
                    ? -1
                    : source.RigDefinition.RequirePhysicalBoneIndex(source.MotionRootBoneId);
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
                    if (behaviours[i] && behaviours[i] != m_Animator)
                        behaviours[i].enabled = false;
                }
                Collider[] colliders = m_Instance.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i])
                        UnityEngine.Object.DestroyImmediate(colliders[i]);
                }
                Rigidbody[] rigidbodies = m_Instance.GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < rigidbodies.Length; i++)
                {
                    if (rigidbodies[i])
                        UnityEngine.Object.DestroyImmediate(rigidbodies[i]);
                }
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

        void BeginClip(UnityEngine.AnimationClip clip)
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

        CharacterFootPlacementAnimatedPose Sample(
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

        Vector3 ToVisualRootLocal(Vector3 worldPosition) =>
            m_Binding.VisualRoot.InverseTransformPoint(worldPosition);

        Quaternion ToVisualRootLocal(Quaternion worldRotation) =>
            (Quaternion.Inverse(m_Binding.VisualRoot.rotation) * worldRotation).normalized;

        Vector3 MotionRootPosition =>
            m_Binding.VisualRoot.InverseTransformPoint(
                MotionRoot.position);

        Quaternion MotionRootRotation =>
            (Quaternion.Inverse(m_Binding.VisualRoot.rotation) *
             MotionRoot.rotation).normalized;

        Transform MotionRoot => m_MotionRootPhysicalBoneIndex >= 0
            ? m_Binding.Binding.PhysicalBones[m_MotionRootPhysicalBoneIndex]
            : throw new InvalidOperationException("Foot Analysis Motion Root is unavailable during calibration authoring.");

        Quaternion LeftHipRotation => ToVisualRootLocal(m_Binding.LeftHip.rotation);
        Quaternion LeftKneeRotation => ToVisualRootLocal(m_Binding.LeftKnee.rotation);
        Quaternion LeftToeRotation => ToVisualRootLocal(m_Binding.LeftToe.rotation);
        Quaternion RightHipRotation => ToVisualRootLocal(m_Binding.RightHip.rotation);
        Quaternion RightKneeRotation => ToVisualRootLocal(m_Binding.RightKnee.rotation);
        Quaternion RightToeRotation => ToVisualRootLocal(m_Binding.RightToe.rotation);

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
        internal CharacterFootPlacementClipSamples SampleClip(
            CharacterFootPlacementAnalysisSource source, UnityEngine.AnimationClip clip)
        {
            float sourceDuration = CharacterAnimationClipRegisteredCurveCatalog.ResolveSourceDurationSeconds(clip);
            BeginClip(clip);
            int intervals = Mathf.Max(2, Mathf.RoundToInt(sourceDuration * source.SampleRate));
            var samples = new CharacterFootPlacementClipSamples(sourceDuration, intervals);
            for (int i = 0; i < samples.RootPositions.Length; i++)
            {
                CharacterFootPlacementAnimatedPose pose = Sample(i * samples.Step, (ulong)i + 1UL);
                samples.Left.HeelPositions[i] = ToVisualRootLocal(pose.Left.HeelPosition);
                samples.Left.ToePositions[i] = ToVisualRootLocal(pose.Left.ToePosition);
                samples.Left.AnklePositions[i] = ToVisualRootLocal(pose.Left.AnklePosition);
                samples.Left.KneePositions[i] = ToVisualRootLocal(pose.Left.KneePosition);
                samples.Right.HeelPositions[i] = ToVisualRootLocal(pose.Right.HeelPosition);
                samples.Right.ToePositions[i] = ToVisualRootLocal(pose.Right.ToePosition);
                samples.Right.AnklePositions[i] = ToVisualRootLocal(pose.Right.AnklePosition);
                samples.Right.KneePositions[i] = ToVisualRootLocal(pose.Right.KneePosition);
                samples.Left.HipPositions[i] = ToVisualRootLocal(pose.Left.HipPosition);
                samples.Right.HipPositions[i] = ToVisualRootLocal(pose.Right.HipPosition);
                samples.Left.SoleRotations[i] = ToVisualRootLocal(pose.Left.SemanticRotation);
                samples.Right.SoleRotations[i] = ToVisualRootLocal(pose.Right.SemanticRotation);
                samples.Left.AnkleRotations[i] = ToVisualRootLocal(pose.Left.AnkleRotation);
                samples.Right.AnkleRotations[i] = ToVisualRootLocal(pose.Right.AnkleRotation);
                samples.RootPositions[i] = Vector3.zero;
                samples.RootRotations[i] = Quaternion.identity;
                RequireFinite(samples.Left.HeelPositions[i], "left heel position", i);
                RequireFinite(samples.Left.ToePositions[i], "left toe position", i);
                RequireFinite(samples.Left.AnklePositions[i], "left ankle position", i);
                RequireFinite(samples.Left.KneePositions[i], "left knee position", i);
                RequireFinite(samples.Right.HeelPositions[i], "right heel position", i);
                RequireFinite(samples.Right.ToePositions[i], "right toe position", i);
                RequireFinite(samples.Right.AnklePositions[i], "right ankle position", i);
                RequireFinite(samples.Right.KneePositions[i], "right knee position", i);
                RequireFinite(samples.Left.HipPositions[i], "left hip position", i);
                RequireFinite(samples.Right.HipPositions[i], "right hip position", i);
                RequireFinite(samples.Left.SoleRotations[i], "left Sole rotation", i);
                RequireFinite(samples.Right.SoleRotations[i], "right Sole rotation", i);
                RequireFinite(samples.Left.AnkleRotations[i], "left Ankle rotation", i);
                RequireFinite(samples.Right.AnkleRotations[i], "right Ankle rotation", i);
            }

            return samples;
        }

        internal CharacterFootMotionDataInput SampleMotion(
            CharacterFootPlacementAnalysisSource source,
            in CharacterFootMotionReference motionReference,
            float sourceDuration,
            Vector3[] targetLeftRootLocalSolePositions,
            Vector3[] targetRightRootLocalSolePositions,
            AnimationFootContactSchedule contactSchedule)
        {
            AnimationClip motionClip = motionReference.MotionReference;
            AnimationClip samplingClip = CreateMotionSamplingClip(motionClip);
            try
            {
                BeginClip(samplingClip);
                int intervals = Mathf.Max(2, Mathf.RoundToInt(sourceDuration * source.SampleRate));
                int sampleCount = intervals + 1;
                float step = sourceDuration / intervals;
                var rootPositions = new Vector3[sampleCount];
                var rootRotations = new Quaternion[sampleCount];
                CharacterFootMotionSampleInput left = CreateMotionFootInput(
                    sampleCount,
                    LeftLegLength,
                    targetLeftRootLocalSolePositions);
                CharacterFootMotionSampleInput right = CreateMotionFootInput(
                    sampleCount,
                    RightLegLength,
                    targetRightRootLocalSolePositions);
                for (int i = 0; i < sampleCount; i++)
                {
                    CharacterFootPlacementAnimatedPose pose = Sample(i * step, (ulong)i + 1UL);
                    rootPositions[i] = MotionRootPosition;
                    rootRotations[i] = MotionRootRotation;
                    CaptureMotionFoot(this, pose.Left, left, i, true);
                    CaptureMotionFoot(this, pose.Right, right, i, false);
                    RequireFinite(rootPositions[i], "motion reference root position", i);
                    RequireFinite(rootRotations[i], "motion reference root rotation", i);
                }
                return new CharacterFootMotionDataInput
                    {
                        SampleRate = source.SampleRate,
                        DurationSeconds = sourceDuration,
                        GroundReferenceHeight = GroundReferenceHeight,
                        Loop = motionClip.isLooping,
                        NoContactLoop = motionClip.isLooping &&
                            !contactSchedule.InferLandingEvents &&
                            contactSchedule.LeftLandingPhases.Count == 0 &&
                            contactSchedule.RightLandingPhases.Count == 0,
                        RootPositions = rootPositions,
                        RootRotations = rootRotations,
                        Thresholds = source.Thresholds,
                        ContactMotionPolicy = contactSchedule.MotionPolicy,
                        Left = left,
                        Right = right
                    };
            }
            finally
            {
                if (samplingClip != motionClip)
                    UnityEngine.Object.DestroyImmediate(samplingClip);
            }
        }

        static AnimationClip CreateMotionSamplingClip(AnimationClip source)
        {
            if (!source.isLooping)
                return source;
            AnimationClip clone = UnityEngine.Object.Instantiate(source);
            clone.name = source.name + " Motion Sampling";
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clone);
            settings.loopTime = false;
            AnimationUtility.SetAnimationClipSettings(clone, settings);
            clone.wrapMode = WrapMode.ClampForever;
            return clone;
        }

        static CharacterFootMotionSampleInput CreateMotionFootInput(
            int sampleCount,
            float legLength,
            Vector3[] targetRootLocalSolePositions)
        {
            if (targetRootLocalSolePositions == null ||
                targetRootLocalSolePositions.Length != sampleCount)
            {
                throw new InvalidOperationException(
                    "Foot Motion target Root-local sole samples do not match the Motion Reference.");
            }
            return new CharacterFootMotionSampleInput
            {
                RigLegLength = legLength,
                HipPositions = new Vector3[sampleCount],
                HipRotations = new Quaternion[sampleCount],
                KneePositions = new Vector3[sampleCount],
                KneeRotations = new Quaternion[sampleCount],
                AnklePositions = new Vector3[sampleCount],
                AnkleRotations = new Quaternion[sampleCount],
                HeelPositions = new Vector3[sampleCount],
                ToePositions = new Vector3[sampleCount],
                ToeRotations = new Quaternion[sampleCount],
                SolePositions = new Vector3[sampleCount],
                SoleRotations = new Quaternion[sampleCount],
                TargetRootLocalSolePositions =
                    (Vector3[])targetRootLocalSolePositions.Clone()
            };
        }

        static void CaptureMotionFoot(
            CharacterFootPlacementAnimationSampler samplingContext,
            CharacterFootPlacementAnimatedFootPose pose,
            CharacterFootMotionSampleInput destination,
            int index,
            bool left)
        {
            destination.HipPositions[index] = samplingContext.ToVisualRootLocal(pose.HipPosition);
            destination.KneePositions[index] = samplingContext.ToVisualRootLocal(pose.KneePosition);
            destination.AnklePositions[index] = samplingContext.ToVisualRootLocal(pose.AnklePosition);
            destination.HeelPositions[index] = samplingContext.ToVisualRootLocal(pose.HeelPosition);
            destination.ToePositions[index] = samplingContext.ToVisualRootLocal(pose.ToePosition);
            destination.SolePositions[index] =
                (destination.HeelPositions[index] + destination.ToePositions[index]) * 0.5f;
            destination.SoleRotations[index] = samplingContext.ToVisualRootLocal(pose.SemanticRotation);
            destination.AnkleRotations[index] = samplingContext.ToVisualRootLocal(pose.AnkleRotation);
            destination.HipRotations[index] = left
                ? samplingContext.LeftHipRotation
                : samplingContext.RightHipRotation;
            destination.KneeRotations[index] = left
                ? samplingContext.LeftKneeRotation
                : samplingContext.RightKneeRotation;
            destination.ToeRotations[index] = left
                ? samplingContext.LeftToeRotation
                : samplingContext.RightToeRotation;
        }

    }

    internal static class CharacterFootPlacementSampleValidation
    {
        internal static void RequireFinite(Vector3 value, string field, int sample)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new InvalidOperationException($"Foot Analysis {field} sample #{sample} is not finite.");
        }

        internal static void RequireFinite(Quaternion value, string field, int sample)
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
