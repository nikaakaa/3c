using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    internal static class CharacterFootLandingPredictionSampler
    {
        const string StartMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Start";
        const string StopMenu =
            "Tools/3C/Diagnostics/Foot Landing Sampling/Stop and Save";
        const string Header =
            "FrameSequence,CompletionIdentity,RootInstanceId,Side,State,RejectReason,StepSource," +
            "LandingEventIdentity,TrajectoryGeneration,LandingConfidence,TimeToLandingSeconds," +
            "RootLocalLandingX,RootLocalLandingY,RootLocalLandingZ," +
            "PresentationDeltaSeconds,PreviousBodyTick,CurrentBodyTick,BodySampleAlpha,BodySampleAgeSeconds," +
            "MotionTimelineAvailable,TimelineGeneration,TimelineAuthorityTick,TimelineTickRate," +
            "TimelineCurrentVelocityX,TimelineCurrentVelocityZ,TimelineContinuationVelocityX,TimelineContinuationVelocityZ," +
            "TimelineHasContinuation,TimelineBodyYawVelocityDegreesPerSecond,TimelineMaximumBodyYawVelocityDegreesPerSecond,CurrentSegmentRemainingSeconds," +
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
            "QueryDirectionX,QueryDirectionY,QueryDirectionZ,QueryMaximumDistance,QueryRadius,QueryLayerMask,QueryMinimumGroundNormalDot," +
            "Accepted,SurfaceIdentity,LandingPointX,LandingPointY,LandingPointZ," +
            "LandingNormalX,LandingNormalY,LandingNormalZ,QueryDistance";

        static readonly List<CharacterFootLandingPredictionDiagnostics> s_Frames =
            new List<CharacterFootLandingPredictionDiagnostics>(4096);

        static bool s_Capturing;

        static CharacterFootLandingPredictionSampler()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        [MenuItem(StartMenu)]
        static void Start()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException(
                    "Foot Landing sampling can only start in Play Mode.");
            if (s_Capturing)
                throw new InvalidOperationException(
                    "Foot Landing sampling is already active.");
            s_Frames.Clear();
            CharacterFootLandingPredictionDebugRegistry.Published += Capture;
            s_Capturing = true;
            Debug.Log("Foot Landing sampling started.");
        }

        [MenuItem(StartMenu, true)]
        static bool CanStart() => EditorApplication.isPlaying && !s_Capturing;

        [MenuItem(StopMenu)]
        static void Stop() => StopAndSave();

        [MenuItem(StopMenu, true)]
        static bool CanStop() => s_Capturing;

        static void Capture(in CharacterFootLandingPredictionDiagnostics diagnostics)
        {
            if (s_Capturing)
                s_Frames.Add(diagnostics);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                StopAndSave();
        }

        static void OnBeforeAssemblyReload() => StopAndSave();

        static void StopAndSave()
        {
            if (!s_Capturing)
                return;
            CharacterFootLandingPredictionDebugRegistry.Published -= Capture;
            s_Capturing = false;
            try
            {
                string path = Save();
                Debug.Log(
                    $"Foot Landing sampling saved {s_Frames.Count * 2} rows to {path}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                s_Frames.Clear();
            }
        }

        static string Save()
        {
            string directory = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                "Temp",
                "FootLandingSamples"));
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
                CharacterFootLandingPredictionDiagnostics frame = s_Frames[i];
                WriteRow(writer, row, in frame, frame.Left);
                WriteRow(writer, row, in frame, frame.Right);
            }
            return path;
        }

        static void WriteRow(
            StreamWriter writer,
            StringBuilder row,
            in CharacterFootLandingPredictionDiagnostics frame,
            CharacterFootLandingPredictionFootDiagnostics foot)
        {
            row.Clear();
            CharacterFootLandingPredictionInputDiagnostics input = frame.Input;
            CharacterFootPlacementQueryRequest query = foot.Query;
            Add(row, frame.FrameSequence);
            Add(row, frame.CompletionIdentity);
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
            Add(row, foot.Accepted);
            Add(row, foot.SurfaceIdentity);
            Add(row, foot.LandingPoint);
            Add(row, foot.LandingNormal);
            Add(row, foot.QueryDistance);
            writer.WriteLine(row);
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
