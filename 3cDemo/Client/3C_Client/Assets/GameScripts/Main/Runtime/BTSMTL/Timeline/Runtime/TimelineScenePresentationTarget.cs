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

        public TimelineScenePresentationTarget(string targetIdentity)
        {
            TargetIdentity = TimelineRuntimeIdentityValidation.Require(targetIdentity, nameof(targetIdentity));
        }

        public string TargetIdentity { get; }
        public IReadOnlyDictionary<string, float> Scalars => m_Scalars;
        public IReadOnlyDictionary<string, bool> Booleans => m_Booleans;
        public IReadOnlyDictionary<string, TimelineScenePresentationSample> LastSamples => m_LastSamples;

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

        void Validate(TimelineScenePresentationSample sample)
        {
            if (!string.Equals(sample.TargetIdentity, TargetIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException($"Scene presentation sample targets '{sample.TargetIdentity}', but receiver is '{TargetIdentity}'.");
        }
    }
}
