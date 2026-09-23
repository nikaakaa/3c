using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterActionAnimationSourcePlan
    {
        [SerializeField] AnimationClip m_AuthoringClipIdentity;
        [SerializeField] string m_ClipIdentity = string.Empty;
        [SerializeField] string m_FullDependencyHash = string.Empty;
        [SerializeField] CharacterAnimationSamplingBackendKind m_Backend;
        [SerializeField] float m_DurationSeconds;
        [SerializeField] bool m_Looping;
        [SerializeField] int m_ResourceCatalogIndex = -1;
        [SerializeField] int m_GroupClipIndex = -1;
        [SerializeField] AnimationFootStepObservationCurvePair m_FootStepObservation;

        internal CharacterActionAnimationSourcePlan(
            AnimationClip authoringClipIdentity,
            string clipIdentity,
            string fullDependencyHash,
            CharacterAnimationSamplingBackendKind backend,
            float durationSeconds,
            bool looping,
            int resourceCatalogIndex,
            int groupClipIndex,
            AnimationFootStepObservationCurvePair footStepObservation)
        {
            m_AuthoringClipIdentity = authoringClipIdentity;
            m_ClipIdentity = clipIdentity?.Trim() ?? string.Empty;
            m_FullDependencyHash = fullDependencyHash?.Trim() ?? string.Empty;
            m_Backend = backend;
            m_DurationSeconds = durationSeconds;
            m_Looping = looping;
            m_FootStepObservation = footStepObservation;
            m_ResourceCatalogIndex = backend == CharacterAnimationSamplingBackendKind.Acl
                ? resourceCatalogIndex
                : -1;
            m_GroupClipIndex = backend == CharacterAnimationSamplingBackendKind.Acl
                ? groupClipIndex
                : -1;
            RequireValid();
        }

        public AnimationClip AuthoringClipIdentity => m_AuthoringClipIdentity;
        public string ClipIdentity => m_ClipIdentity ?? string.Empty;
        public string FullDependencyHash => m_FullDependencyHash ?? string.Empty;
        public CharacterAnimationSamplingBackendKind Backend => m_Backend;
        public float DurationSeconds => m_DurationSeconds;
        public bool Looping => m_Looping;
        public int ResourceCatalogIndex => m_ResourceCatalogIndex;
        public int GroupClipIndex => m_GroupClipIndex;
        public AnimationFootStepObservationCurvePair FootStepObservation => m_FootStepObservation;

        public void RequireValid()
        {
            if (m_FootStepObservation == null)
                throw new InvalidOperationException("Action animation source has no compiled Foot Motion observation.");
            m_FootStepObservation.RequireValid();
            if (!AuthoringClipIdentity ||
                string.IsNullOrWhiteSpace(ClipIdentity) ||
                string.IsNullOrWhiteSpace(FullDependencyHash) ||
                (Backend != CharacterAnimationSamplingBackendKind.NativeClip && Backend != CharacterAnimationSamplingBackendKind.Acl) ||
                !float.IsFinite(DurationSeconds) || DurationSeconds <= 0f ||
                Backend == CharacterAnimationSamplingBackendKind.NativeClip &&
                (ResourceCatalogIndex != -1 || GroupClipIndex != -1) ||
                Backend == CharacterAnimationSamplingBackendKind.Acl &&
                (ResourceCatalogIndex < 0 || GroupClipIndex < 0))
            {
                throw new InvalidOperationException(
                    "Compiled Action animation source plan is invalid.");
            }
        }

        internal ClipSamplePlan CreateSample(
            in ActionProjectedSample sample)
        {
            if (!sample.IsValid ||
                sample.Time.SampleTime > DurationSeconds)
            {
                throw new InvalidOperationException(
                    $"Action animation source '{ClipIdentity}' received an invalid sample time.");
            }
            float normalizedTime = Mathf.Clamp01(
                sample.Time.SampleTime / DurationSeconds);
            return Backend == CharacterAnimationSamplingBackendKind.Acl
                ? new ClipSamplePlan(
                    0,
                    ResourceCatalogIndex,
                    GroupClipIndex,
                    DurationSeconds,
                    sample.Time.SampleTime,
                    sample.Time.ContinuousTime,
                    normalizedTime,
                    sample.ProducerWeight,
                    sample.Time.Loop)
                : new ClipSamplePlan(
                    0,
                    AuthoringClipIdentity,
                    sample.Time.SampleTime,
                    sample.Time.ContinuousTime,
                    normalizedTime,
                    sample.ProducerWeight,
                    sample.Time.Loop);
        }
    }
}
