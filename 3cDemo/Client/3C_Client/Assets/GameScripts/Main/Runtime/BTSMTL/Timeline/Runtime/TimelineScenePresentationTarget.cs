using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace BTSMTL.Timeline.Runtime
{
    public sealed class TimelineScenePresentationTarget : ITimelineScenePresentationSink
    {
        readonly Dictionary<string, float> m_Scalars = new Dictionary<string, float>(StringComparer.Ordinal);
        readonly Dictionary<string, bool> m_Booleans = new Dictionary<string, bool>(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineScenePresentationSample> m_LastSamples =
            new Dictionary<string, TimelineScenePresentationSample>(StringComparer.Ordinal);
        readonly Dictionary<ulong, PendingRuntimeValues> m_PendingRuntimeValues =
            new Dictionary<ulong, PendingRuntimeValues>();
        readonly Dictionary<string, TimelineRuntimeScenePresentationSample> m_LastRuntimeSamples =
            new Dictionary<string, TimelineRuntimeScenePresentationSample>(StringComparer.Ordinal);

        public TimelineScenePresentationTarget(string targetIdentity)
        {
            TargetIdentity = string.IsNullOrWhiteSpace(targetIdentity)
                ? throw new ArgumentException("Timeline target identity is required.", nameof(targetIdentity))
                : targetIdentity.Trim();
        }

        public string TargetIdentity { get; }
        public IReadOnlyDictionary<string, float> Scalars => m_Scalars;
        public IReadOnlyDictionary<string, bool> Booleans => m_Booleans;
        public IReadOnlyDictionary<string, TimelineScenePresentationSample> LastSamples => m_LastSamples;
        public IReadOnlyDictionary<string, TimelineRuntimeScenePresentationSample> LastRuntimeSamples => m_LastRuntimeSamples;

        public void WriteScalar(TimelineScenePresentationSample sample)
        {
            Validate(sample);
            if (m_Booleans.ContainsKey(sample.ParameterId))
                throw new InvalidOperationException($"Scene presentation parameter '{sample.ParameterId}' is already bound as Boolean on target '{TargetIdentity}'.");
            m_Scalars[sample.ParameterId] = sample.Value;
            m_LastSamples[sample.ParameterId] = sample;
        }

        public void WriteBoolean(TimelineScenePresentationSample sample)
        {
            Validate(sample);
            if (m_Scalars.ContainsKey(sample.ParameterId))
                throw new InvalidOperationException($"Scene presentation parameter '{sample.ParameterId}' is already bound as Scalar on target '{TargetIdentity}'.");
            m_Booleans[sample.ParameterId] = sample.Value >= 0.5f;
            m_LastSamples[sample.ParameterId] = sample;
        }

        public bool Consume(TimelineRuntimeStepContext context)
        {
            PendingRuntimeValues pending = new PendingRuntimeValues();
            for (int index = 0; index < context.Advance.Evaluation.ScenePresentation.Count; index++)
            {
                TimelineRuntimeScenePresentationSample sample = context.Advance.Evaluation.ScenePresentation[index];
                if (!context.Playback.TryResolveTargetIdentity(sample.TargetBindingId, out string targetIdentity))
                    return false;
                if (!string.Equals(targetIdentity, TargetIdentity, StringComparison.Ordinal))
                    continue;
                if (sample.ValueKind == TimelineBindingValueKind.Scalar)
                {
                    if (pending.Booleans.ContainsKey(sample.ParameterBindingId) ||
                        m_Booleans.ContainsKey(sample.ParameterBindingId))
                        return false;
                    pending.Scalars[sample.ParameterBindingId] = sample.Value;
                    pending.ScalarSamples[sample.ParameterBindingId] = sample;
                }
                else if (sample.ValueKind == TimelineBindingValueKind.Boolean)
                {
                    if (pending.Scalars.ContainsKey(sample.ParameterBindingId) ||
                        m_Scalars.ContainsKey(sample.ParameterBindingId))
                        return false;
                    pending.Booleans[sample.ParameterBindingId] = sample.Value >= 0.5f;
                    pending.BooleanSamples[sample.ParameterBindingId] = sample;
                }
                else
                {
                    return false;
                }
            }
            m_PendingRuntimeValues[context.Playback.Handle.Value] = pending;
            return true;
        }

        public void Commit(TimelineRuntimeStepContext context)
        {
            if (!m_PendingRuntimeValues.TryGetValue(context.Playback.Handle.Value, out PendingRuntimeValues pending))
                return;
            foreach (KeyValuePair<string, float> value in pending.Scalars)
            {
                m_Scalars[value.Key] = value.Value;
                m_LastRuntimeSamples[value.Key] = pending.ScalarSamples[value.Key];
            }
            foreach (KeyValuePair<string, bool> value in pending.Booleans)
            {
                m_Booleans[value.Key] = value.Value;
                m_LastRuntimeSamples[value.Key] = pending.BooleanSamples[value.Key];
            }
            m_PendingRuntimeValues.Remove(context.Playback.Handle.Value);
        }

        public void Discard(TimelineRuntimeStepContext context)
        {
            m_PendingRuntimeValues.Remove(context.Playback.Handle.Value);
        }

        public bool ConsumeStop(TimelineRuntimeStopRequest request) => true;

        public void CommitStop(TimelineRuntimeStopRequest request)
        {
            m_PendingRuntimeValues.Remove(request.Handle.Value);
        }

        public void DiscardStop(TimelineRuntimeStopRequest request)
        {
            m_PendingRuntimeValues.Remove(request.Handle.Value);
        }

        void Validate(TimelineScenePresentationSample sample)
        {
            if (!string.Equals(sample.TargetIdentity, TargetIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException($"Scene presentation sample targets '{sample.TargetIdentity}', but receiver is '{TargetIdentity}'.");
        }

        sealed class PendingRuntimeValues
        {
            public readonly Dictionary<string, float> Scalars = new Dictionary<string, float>(StringComparer.Ordinal);
            public readonly Dictionary<string, bool> Booleans = new Dictionary<string, bool>(StringComparer.Ordinal);
            public readonly Dictionary<string, TimelineRuntimeScenePresentationSample> ScalarSamples =
                new Dictionary<string, TimelineRuntimeScenePresentationSample>(StringComparer.Ordinal);
            public readonly Dictionary<string, TimelineRuntimeScenePresentationSample> BooleanSamples =
                new Dictionary<string, TimelineRuntimeScenePresentationSample>(StringComparer.Ordinal);
        }
    }
}
