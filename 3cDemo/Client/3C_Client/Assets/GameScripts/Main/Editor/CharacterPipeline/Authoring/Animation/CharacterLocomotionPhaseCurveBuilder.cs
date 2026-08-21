using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    static class CharacterLocomotionPhaseCurveBuilder
    {
        const float EventTimeTolerance = 0.0001f;

        readonly struct LandingEvent
        {
            public LandingEvent(float time, bool right)
            {
                Time = time;
                Right = right;
            }

            public float Time { get; }
            public bool Right { get; }
        }

        public static AnimationCurve Build(
            AnimationFootPhaseValidationDescriptor validation,
            float duration,
            float coverageStart,
            float coverageEnd,
            bool loop)
        {
            List<LandingEvent> events = CollectEvents(validation, duration);
            List<LandingEvent> anchors = SelectCoverageAnchors(
                PrepareCoverageEvents(events, coverageStart, coverageEnd, loop),
                coverageStart,
                coverageEnd);
            var phases = new float[anchors.Count];
            phases[0] = anchors[0].Right ? 0f : 0.5f;
            for (int i = 1; i < phases.Length; i++)
                phases[i] = phases[i - 1] + 0.5f;
            if (loop)
                NormalizeCyclicPhase(anchors, phases, coverageStart);

            float startPhase = EvaluatePhase(anchors, phases, coverageStart);
            float endPhase = EvaluatePhase(anchors, phases, coverageEnd);
            if (loop)
            {
                float span = endPhase - startPhase;
                if (span < 1f || Mathf.Abs(span - Mathf.Round(span)) > EventTimeTolerance)
                    throw new InvalidOperationException("Cyclic Locomotion Phase candidate does not close on a positive integer span.");
            }

            var keys = new List<Keyframe>
            {
                new Keyframe(coverageStart, startPhase)
            };
            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].Time > coverageStart + EventTimeTolerance &&
                    anchors[i].Time < coverageEnd - EventTimeTolerance)
                {
                    keys.Add(new Keyframe(anchors[i].Time, phases[i]));
                }
            }
            keys.Add(new Keyframe(coverageEnd, endPhase));
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

        static List<LandingEvent> CollectEvents(
            AnimationFootPhaseValidationDescriptor validation,
            float duration)
        {
            validation?.RequireValid();
            var result = new List<LandingEvent>();
            AddEvents(result, validation.Left, duration, false);
            AddEvents(result, validation.Right, duration, true);
            if (result.Count < 2)
                throw new InvalidOperationException("Locomotion Phase candidate requires at least two Landing onsets.");
            return result
                .OrderBy(value => value.Time)
                .ThenBy(value => value.Right ? 0 : 1)
                .ToList();
        }

        static void AddEvents(
            ICollection<LandingEvent> destination,
            AnimationFootPhaseValidationFootDescriptor descriptor,
            float duration,
            bool right)
        {
            for (int i = 0; i < descriptor.Samples.Count; i++)
            {
                if (descriptor.Samples[i].LandingOnset)
                {
                    destination.Add(new LandingEvent(
                        descriptor.Samples[i].NormalizedTime * duration,
                        right));
                }
            }
        }

        static List<LandingEvent> PrepareCoverageEvents(
            IReadOnlyList<LandingEvent> events,
            float start,
            float end,
            bool loop)
        {
            var filtered = new List<LandingEvent>();
            for (int i = 0; i < events.Count; i++)
            {
                if (i + 1 < events.Count &&
                    Mathf.Abs(events[i + 1].Time - events[i].Time) <= EventTimeTolerance)
                {
                    if (events[i + 1].Right == events[i].Right ||
                        Mathf.Abs(events[i].Time - start) > EventTimeTolerance)
                    {
                        throw new InvalidOperationException("Locomotion Phase candidate contains simultaneous Landing onsets inside its source.");
                    }
                    i++;
                    continue;
                }
                filtered.Add(events[i]);
            }
            if (!loop)
                return filtered;

            float duration = end - start;
            var expanded = new List<LandingEvent>(filtered.Count * 3);
            for (int cycle = -1; cycle <= 1; cycle++)
            {
                for (int i = 0; i < filtered.Count; i++)
                {
                    expanded.Add(new LandingEvent(
                        filtered[i].Time + cycle * duration,
                        filtered[i].Right));
                }
            }
            return expanded;
        }

        static List<LandingEvent> SelectCoverageAnchors(
            IReadOnlyList<LandingEvent> events,
            float start,
            float end)
        {
            if (events.Count < 2)
                throw new InvalidOperationException("Locomotion Phase coverage requires Landing onset anchors.");
            int first = -1;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Time <= start + EventTimeTolerance)
                    first = i;
                else
                    break;
            }
            if (first < 0)
                first = 0;

            var result = new List<LandingEvent> { events[first] };
            for (int i = first + 1; i < events.Count; i++)
            {
                LandingEvent candidate = events[i];
                LandingEvent previous = result[result.Count - 1];
                if (candidate.Right == previous.Right)
                {
                    if (candidate.Time <= end + EventTimeTolerance)
                        throw new InvalidOperationException("Locomotion Phase coverage contains repeated same-side Landing onsets.");
                    continue;
                }
                result.Add(candidate);
                if (candidate.Time >= end - EventTimeTolerance)
                    break;
            }
            if (result.Count < 2)
                throw new InvalidOperationException("Locomotion Phase coverage requires opposing Landing onset anchors.");
            return result;
        }

        static void NormalizeCyclicPhase(
            IReadOnlyList<LandingEvent> anchors,
            IList<float> phases,
            float start)
        {
            int firstInside = -1;
            for (int i = 0; i < anchors.Count; i++)
            {
                if (anchors[i].Time >= start - EventTimeTolerance)
                {
                    firstInside = i;
                    break;
                }
            }
            if (firstInside < 0)
                throw new InvalidOperationException("Cyclic Locomotion Phase candidate has no in-coverage Landing onset.");
            float desired = anchors[firstInside].Right ? 0f : 0.5f;
            float shift = phases[firstInside] - desired;
            for (int i = 0; i < phases.Count; i++)
                phases[i] -= shift;
        }

        static float EvaluatePhase(
            IReadOnlyList<LandingEvent> events,
            IReadOnlyList<float> phases,
            float time)
        {
            if (time <= events[0].Time)
                return Interpolate(events, phases, 0, time);
            if (time >= events[events.Count - 1].Time)
                return Interpolate(events, phases, events.Count - 2, time);
            for (int i = 0; i < events.Count - 1; i++)
            {
                if (time >= events[i].Time - EventTimeTolerance &&
                    time <= events[i + 1].Time + EventTimeTolerance)
                {
                    return Interpolate(events, phases, i, time);
                }
            }
            throw new InvalidOperationException("Locomotion Phase boundary is outside its Landing onset anchors.");
        }

        static float Interpolate(
            IReadOnlyList<LandingEvent> events,
            IReadOnlyList<float> phases,
            int index,
            float time)
        {
            float t = (time - events[index].Time) /
                      (events[index + 1].Time - events[index].Time);
            return Mathf.LerpUnclamped(phases[index], phases[index + 1], t);
        }
    }
}
