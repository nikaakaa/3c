using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using BTSMTL.Timeline.Runtime;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonSimulation;
using UnityEngine;

namespace BTSMTL.Timeline.Unity
{
    [Serializable]
    public sealed class ScenePresentationTimelineTargetBinding
    {
        [SerializeField]
        string m_BindingId = string.Empty;

        [SerializeField]
        ScenePresentationPanelTarget m_Target;

        public string BindingId => m_BindingId ?? string.Empty;
        public ScenePresentationPanelTarget Target => m_Target;
    }

    [DisallowMultipleComponent]
    public sealed class ScenePresentationTimelineHost : MonoBehaviour
    {
        [SerializeField]
        TimelineSimulationProgramAsset m_ProgramAsset;

        [SerializeField]
        ScenePresentationTimelineTargetBinding[] m_TargetBindings = Array.Empty<ScenePresentationTimelineTargetBinding>();

        [SerializeField]
        string m_OwnerIdentity = "scene.presentation";

        [SerializeField]
        string m_CallIdentity = "scene.panel.expand";

        [SerializeField]
        ulong m_InstanceId = 1;

        Float32TimelinePlayback m_Timeline;

        public Float32TimelinePlaybackStatus Status => m_Timeline?.Status ?? Float32TimelinePlaybackStatus.Unprepared;
        public TimelinePlaybackObservation Observation => m_Timeline?.Observation ?? default;

        void OnEnable()
        {
            PrepareAndStart();
        }

        public void PrepareAndStart()
        {
            if (m_Timeline != null)
                return;
            if (!m_ProgramAsset)
                throw new InvalidOperationException($"Timeline host '{name}' has no Timeline Program asset.");
            if (m_InstanceId == 0)
                throw new InvalidOperationException($"Timeline host '{name}' requires a non-zero instance identity.");

            ScenePresentationTimelineTargetBinding[] targetBindings = m_TargetBindings ?? Array.Empty<ScenePresentationTimelineTargetBinding>();
            var targets = new List<Float32TimelineTargetBinding>(targetBindings.Length);
            var calls = new List<TimelineCallBinding>(targetBindings.Length);
            for (int i = 0; i < targetBindings.Length; i++)
            {
                ScenePresentationTimelineTargetBinding binding = targetBindings[i];
                if (binding == null || !binding.Target)
                    throw new InvalidOperationException($"Timeline host '{name}' has an incomplete target binding at index {i}.");
                targets.Add(new Float32TimelineTargetBinding(binding.BindingId, binding.Target));
                calls.Add(new TimelineCallBinding(binding.BindingId, TimelineBindingValue.Target(binding.Target.TargetIdentity)));
            }

            m_Timeline = new Float32TimelinePlayback(m_ProgramAsset.Load());
            m_Timeline.Prepare(
                new Float32TimelineBindingSet(targets),
                calls,
                new Float32TimelineCall(m_OwnerIdentity, m_CallIdentity, m_InstanceId));
            m_Timeline.Start();
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (m_Timeline?.Status == Float32TimelinePlaybackStatus.Running)
                m_Timeline.Advance(Float32Scalar.FromSingle(deltaSeconds));
        }

        void OnDisable()
        {
            m_Timeline?.Dispose();
            m_Timeline = null;
        }
    }
}
