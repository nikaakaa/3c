using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.RootMotion
{
    public static class AnimationClipBoundaryAuthoringService
    {
        const float TimeTolerance = 0.0001f;
        const float TangentSample = 0.00001f;

        public static void RepartitionClips(AnimationClip main, AnimationClip end, float mainDuration)
        {
            if (!main || !end || main == end)
                throw new ArgumentException("Animation boundary requires two distinct clips.");

            float oldMainDuration = main.length;
            float oldEndDuration = end.length;
            ValidateBoundary(oldMainDuration, oldEndDuration, mainDuration);
            EditorCurveBinding[] mainBindings = AnimationUtility.GetCurveBindings(main);
            EditorCurveBinding[] endBindings = AnimationUtility.GetCurveBindings(end);
            if (mainBindings.Length != endBindings.Length ||
                AnimationUtility.GetObjectReferenceCurveBindings(main).Length != 0 ||
                AnimationUtility.GetObjectReferenceCurveBindings(end).Length != 0 ||
                AnimationUtility.GetAnimationEvents(main).Length != 0 ||
                AnimationUtility.GetAnimationEvents(end).Length != 0)
                throw new InvalidOperationException("Animation pair contains unsupported or unmatched curves.");

            var endKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < endBindings.Length; i++)
                endKeys.Add(BindingKey(endBindings[i]));

            var mainCurves = new AnimationCurve[mainBindings.Length];
            var endCurves = new AnimationCurve[mainBindings.Length];
            for (int i = 0; i < mainBindings.Length; i++)
            {
                EditorCurveBinding binding = mainBindings[i];
                if (!endKeys.Remove(BindingKey(binding)))
                    throw new InvalidOperationException($"Animation pair is missing {binding.path}/{binding.propertyName}.");
                CurvePair pair = Repartition(
                    AnimationUtility.GetEditorCurve(main, binding),
                    AnimationUtility.GetEditorCurve(end, binding),
                    oldMainDuration,
                    mainDuration,
                    false);
                mainCurves[i] = pair.Main;
                endCurves[i] = pair.End;
            }
            if (endKeys.Count != 0)
                throw new InvalidOperationException("Animation pair has unmatched End curves.");

            Undo.RecordObjects(new UnityEngine.Object[] { main, end }, "Repartition animation boundary");
            AnimationUtility.SetEditorCurves(main, mainBindings, mainCurves);
            AnimationUtility.SetEditorCurves(end, mainBindings, endCurves);
            main.EnsureQuaternionContinuity();
            end.EnsureQuaternionContinuity();
            SetDuration(main, mainDuration);
            SetDuration(end, oldMainDuration + oldEndDuration - mainDuration);
            EditorUtility.SetDirty(main);
            EditorUtility.SetDirty(end);
        }

        public static void RepartitionMotionCurves(
            RootMotionCurveAsset main,
            RootMotionCurveAsset end,
            float mainDuration)
        {
            if (!main || !end || main == end || !main.SourceClip || !end.SourceClip ||
                main.EvaluationMode != end.EvaluationMode)
                throw new ArgumentException("Root motion boundary requires a matching curve pair.");
            float oldMainDuration = main.Duration;
            float oldEndDuration = end.Duration;
            ValidateBoundary(oldMainDuration, oldEndDuration, mainDuration);
            float endDuration = oldMainDuration + oldEndDuration - mainDuration;
            CurvePair x = Repartition(main.LocalPositionX, end.LocalPositionX, oldMainDuration, mainDuration, true);
            CurvePair y = Repartition(main.LocalPositionY, end.LocalPositionY, oldMainDuration, mainDuration, true);
            CurvePair z = Repartition(main.LocalPositionZ, end.LocalPositionZ, oldMainDuration, mainDuration, true);
            CurvePair distance = Repartition(main.ForwardDistance, end.ForwardDistance, oldMainDuration, mainDuration, true);
            CurvePair yaw = Repartition(main.LocalYaw, end.LocalYaw, oldMainDuration, mainDuration, true);

            AnimationClip mainClip = main.SourceClip;
            AnimationClip endClip = end.SourceClip;
            float sampleRate = main.SampleRate;
            float endSampleRate = end.SampleRate;
            RootMotionCurveEvaluationMode mode = main.EvaluationMode;
            Undo.RecordObjects(new UnityEngine.Object[] { main, end }, "Repartition root motion boundary");
            main.SetBakedData(mainClip, mainDuration, sampleRate, mode,
                x.Main, y.Main, z.Main, distance.Main, yaw.Main,
                PositionAt(x.Main, y.Main, z.Main, mainDuration),
                distance.Main.Evaluate(mainDuration), yaw.Main.Evaluate(mainDuration));
            end.SetBakedData(endClip, endDuration, endSampleRate, mode,
                x.End, y.End, z.End, distance.End, yaw.End,
                PositionAt(x.End, y.End, z.End, endDuration),
                distance.End.Evaluate(endDuration), yaw.End.Evaluate(endDuration));
            EditorUtility.SetDirty(main);
            EditorUtility.SetDirty(end);
        }

        static CurvePair Repartition(
            AnimationCurve main,
            AnimationCurve end,
            float oldMainDuration,
            float mainDuration,
            bool cumulative)
        {
            if (main == null || end == null || main.length == 0 || end.length == 0)
                throw new InvalidOperationException("Animation boundary contains an empty curve.");

            float moved = oldMainDuration - mainDuration;
            float boundaryValue = main.Evaluate(mainDuration);
            float endOffset = cumulative ? main.Evaluate(oldMainDuration) - boundaryValue - end.Evaluate(0f) : 0f;
            Keyframe boundary = KeyAt(main, mainDuration);
            var firstKeys = new List<Keyframe>();
            var secondKeys = new List<Keyframe>();
            Keyframe[] mainKeys = main.keys;
            for (int i = 0; i < mainKeys.Length; i++)
            {
                Keyframe key = mainKeys[i];
                if (key.time < mainDuration - TimeTolerance)
                    firstKeys.Add(key);
                else if (key.time > mainDuration + TimeTolerance &&
                         key.time < oldMainDuration - TimeTolerance)
                    secondKeys.Add(Shift(key, -mainDuration, cumulative ? -boundaryValue : 0f));
            }
            firstKeys.Add(boundary);
            secondKeys.Insert(0, Shift(boundary, -mainDuration, cumulative ? -boundaryValue : 0f));

            Keyframe[] endKeys = end.keys;
            for (int i = 0; i < endKeys.Length; i++)
            {
                Keyframe key = endKeys[i];
                if (key.time >= -TimeTolerance)
                    secondKeys.Add(Shift(key, moved, endOffset));
            }

            return new CurvePair(
                CopyCurve(firstKeys, main),
                CopyCurve(secondKeys, end));
        }

        static Keyframe KeyAt(AnimationCurve curve, float time)
        {
            Keyframe[] keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (Mathf.Abs(keys[i].time - time) >= TimeTolerance)
                    continue;
                Keyframe existing = keys[i];
                existing.time = time;
                return existing;
            }
            float before = Mathf.Max(0f, time - TangentSample);
            float after = Mathf.Min(curve.keys[curve.length - 1].time, time + TangentSample);
            float slope = after > before
                ? (curve.Evaluate(after) - curve.Evaluate(before)) / (after - before)
                : 0f;
            return new Keyframe(time, curve.Evaluate(time), slope, slope);
        }

        static Keyframe Shift(Keyframe key, float timeOffset, float valueOffset)
        {
            key.time += timeOffset;
            key.value += valueOffset;
            return key;
        }

        static AnimationCurve CopyCurve(List<Keyframe> keys, AnimationCurve source) =>
            new AnimationCurve(keys.ToArray())
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };

        static Vector3 PositionAt(AnimationCurve x, AnimationCurve y, AnimationCurve z, float time) =>
            new Vector3(x.Evaluate(time), y.Evaluate(time), z.Evaluate(time));

        static string BindingKey(EditorCurveBinding binding) =>
            $"{binding.path}\0{binding.type.FullName}\0{binding.propertyName}";

        static void ValidateBoundary(float mainDuration, float endDuration, float targetDuration)
        {
            if (!float.IsFinite(targetDuration) || targetDuration <= 0f ||
                targetDuration >= mainDuration || endDuration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(targetDuration));
        }

        static void SetDuration(AnimationClip clip, float duration)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0f;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            if (Mathf.Abs(clip.length - duration) > TimeTolerance)
                throw new InvalidOperationException($"Animation clip '{clip.name}' has length {clip.length} instead of {duration}.");
        }

        readonly struct CurvePair
        {
            internal CurvePair(AnimationCurve main, AnimationCurve end)
            {
                Main = main;
                End = end;
            }

            internal AnimationCurve Main { get; }
            internal AnimationCurve End { get; }
        }
    }
}
