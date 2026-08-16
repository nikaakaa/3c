using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [InitializeOnLoad]
    static class GameplayLabFootIkEnduranceCapture
    {
        const int ColumnCount = 1225;
        const int BaseColumnCount = 1127;
        const int GlobalColumnCount = 71;
        const int LegColumnCount = 577;
        const int BeforeSequenceColumnCount = 339;
        const int ReplacedSequenceColumnCount = 118;
        const int SequenceColumnCount = 146;
        const int CausalityColumnCount = 21;
        const int MaximumChunkRows = 1800;
        const int ManifestWriteAttemptCount = 6;
        static readonly UTF8Encoding s_Utf8 = new UTF8Encoding(false);
        static readonly string[] s_Header = BuildHeader();
        static readonly Guid s_InterestOwner = new Guid("f8d2f588-5c47-4da4-a418-673794bbfb71");
        static readonly HashSet<AnimationPresentationRuntimeTarget> s_InterestedTargets =
            new HashSet<AnimationPresentationRuntimeTarget>();
        static CaptureRun s_Run;
        static bool s_Failed;
        static bool s_AcceptingFrames;
        static int s_LastLoggedTargetCount = -1;

        static GameplayLabFootIkEnduranceCapture()
        {
            s_AcceptingFrames = EditorApplication.isPlayingOrWillChangePlaymode;
            CharacterFootIkCompletedFrameStream.Published += OnFrame;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += EnsureDiagnosticsInterest;
            AnimationPresentationRuntimeTargetRegistry.TargetRegistered += TryAttachDiagnosticsInterest;
            AnimationPresentationRuntimeTargetRegistry.TargetUnregistered += RemoveDiagnosticsInterest;
            AssemblyReloadEvents.beforeAssemblyReload += ShutdownForReload;
        }

        static void OnFrame(CharacterFootIkCompletedFrameSnapshot frame)
        {
            if (!s_AcceptingFrames || s_Failed ||
                !GameplayLabFootIkRouteRegistry.TryGet(frame.ActorId, out GameplayLabFootIkRouteSnapshot route))
                return;
            try
            {
                if (s_Run == null || !string.Equals(s_Run.RunId, route.RunId, StringComparison.Ordinal))
                {
                    s_Run?.Dispose("replaced");
                    s_Run = new CaptureRun(route.RunId, s_Header);
                    Debug.Log($"GameplayLab Foot IK endurance capture started: {s_Run.DirectoryPath}");
                }
                s_Run.Write(frame, route);
            }
            catch (Exception exception)
            {
                s_Failed = true;
                CaptureRun failedRun = s_Run;
                s_Run = null;
                try
                {
                    failedRun?.Dispose("failed");
                }
                catch (Exception cleanupException)
                {
                    Debug.LogException(cleanupException);
                }
                Debug.LogException(exception);
            }
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                s_AcceptingFrames = true;
                return;
            }
            if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode)
                return;
            s_AcceptingFrames = false;
            s_Run?.Dispose("stopped");
            s_Run = null;
            s_Failed = false;
            ReleaseDiagnosticsInterest();
        }

        static void ShutdownForReload()
        {
            s_Run?.Dispose("assembly-reload");
            s_Run = null;
            ReleaseDiagnosticsInterest();
        }

        static void EnsureDiagnosticsInterest()
        {
            if (!EditorApplication.isPlaying || !EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            s_AcceptingFrames = true;
            IReadOnlyList<AnimationPresentationRuntimeTarget> targets =
                AnimationPresentationRuntimeTargetRegistry.Targets;
            if (targets.Count != s_LastLoggedTargetCount)
            {
                s_LastLoggedTargetCount = targets.Count;
                Debug.Log($"GameplayLab Foot IK diagnostics targets: {targets.Count}");
            }
            for (int i = 0; i < targets.Count; i++)
                TryAttachDiagnosticsInterest(targets[i]);
        }

        static void TryAttachDiagnosticsInterest(AnimationPresentationRuntimeTarget target)
        {
            if (target == null)
                return;
            if (!s_InterestedTargets.Contains(target))
            {
                UnityEngine.Object hostObject = EditorUtility.InstanceIDToObject(target.HostInstanceId);
                GameObject host = hostObject is Component component
                    ? component.gameObject
                    : hostObject as GameObject;
                if (!host || !host.GetComponent<GameplayLabFootIkFixedControlSource>())
                    return;
                target.SetDiagnosticsInterest(s_InterestOwner, AnimationPresentationDiagnosticsInterest.LiveState);
                s_InterestedTargets.Add(target);
                Debug.Log($"GameplayLab Foot IK diagnostics attached: {target.DisplayName}/{host.name}");
            }

        }

        static void RemoveDiagnosticsInterest(AnimationPresentationRuntimeTarget target)
        {
            if (target == null || !s_InterestedTargets.Remove(target))
                return;
            target.RemoveDiagnosticsInterest(s_InterestOwner);
        }

        static void ReleaseDiagnosticsInterest()
        {
            foreach (AnimationPresentationRuntimeTarget target in s_InterestedTargets)
                target.RemoveDiagnosticsInterest(s_InterestOwner);
            s_InterestedTargets.Clear();
        }

        static string[] BuildHeader()
        {
            var builder = new StringBuilder(32768);
            CharacterRuntimeDiagnosticsInspector.AppendFootIkHeader(builder);
            var values = new List<string>(builder.ToString().TrimEnd('\r', '\n').Split(','));
            if (values.Count != BaseColumnCount)
                throw new InvalidOperationException($"Foot IK base CSV header has {values.Count} columns instead of {BaseColumnCount}.");
            ReplaceSequenceHeader(values, GlobalColumnCount, "left");
            InsertCausalityHeader(
                values,
                GlobalColumnCount + BeforeSequenceColumnCount + SequenceColumnCount,
                "left");
            ReplaceSequenceHeader(values, GlobalColumnCount + LegColumnCount, "right");
            InsertCausalityHeader(
                values,
                GlobalColumnCount + LegColumnCount + BeforeSequenceColumnCount + SequenceColumnCount,
                "right");
            if (values.Count != ColumnCount)
                throw new InvalidOperationException($"Foot IK CSV header has {values.Count} columns instead of {ColumnCount}.");
            if (new HashSet<string>(values, StringComparer.Ordinal).Count != ColumnCount)
                throw new InvalidOperationException("Foot IK CSV header names must be unique.");
            return values.ToArray();
        }

        static void ReplaceSequenceHeader(List<string> values, int legOffset, string prefix)
        {
            int offset = legOffset + BeforeSequenceColumnCount;
            for (int i = 0; i < SequenceColumnCount - ReplacedSequenceColumnCount; i++)
                values.Insert(offset + ReplacedSequenceColumnCount, string.Empty);
            string[][] groups =
            {
                new[] { "count", "fraction_seq", "x_seq", "y_seq", "z_seq", "min_y", "max_y", "start_x", "start_y", "start_z", "end_x", "end_y", "end_z", "hash" },
                new[] { "count", "action_phase_seq", "ground_path_progress_seq" },
                new[] { "segment_count", "start_fraction_seq", "end_fraction_seq", "surface_seq", "normal_x_seq", "normal_y_seq", "normal_z_seq", "edge_start_x_seq", "edge_start_y_seq", "edge_start_z_seq", "edge_end_x_seq", "edge_end_y_seq", "edge_end_z_seq", "sole_height_pair_seq" },
                new[] { "count", "start_fraction_seq", "end_fraction_seq", "start_x_seq", "start_y_seq", "start_z_seq", "end_x_seq", "end_y_seq", "end_z_seq", "start_height_seq", "end_height_seq", "surface_seq", "root_y_pair_seq", "hip_y_pair_seq" },
                new[] { "count", "shape_purpose_seq", "origin_x_seq", "origin_y_seq", "origin_z_seq", "capsule_end_x_seq", "capsule_end_y_seq", "capsule_end_z_seq", "direction_x_seq", "direction_y_seq", "direction_z_seq", "maximum_distance_seq", "radius_seq", "layer_minimum_dot_seq" },
                new[] { "count", "query_index_seq", "surface_seq", "position_x_seq", "position_y_seq", "position_z_seq", "normal_x_seq", "normal_y_seq", "normal_z_seq", "reason_seq", "min_y", "max_y", "first_query_index", "last_query_index" },
                new[] { "count", "query_index_seq", "surface_seq", "reason_seq", "position_x_seq", "position_y_seq", "position_z_seq", "normal_x_seq", "normal_y_seq", "normal_z_seq", "min_y", "max_y", "first_query_index", "last_query_index" },
                new[] { "plan_sequence", "generated_frame", "landing_event_identity", "executable", "landing_valid", "landing_x", "landing_y", "landing_z", "current_path_x", "current_path_y", "current_path_z", "clearance_evaluated", "rewritten", "action_progress", "ground_path_progress", "virtual_ground_split_valid", "virtual_ground_split_fraction", "virtual_ground_split_landing_event_identity", "virtual_ground_split_x", "virtual_ground_split_y", "virtual_ground_split_z" },
                new[] { "route_phase", "route_direction", "route_lap", "route_actor_x", "route_actor_y", "route_actor_z", "route_actor_yaw", "input_x", "input_y", "actual_planar_speed", "simulation_tick", "tick_rate", "input_magnitude", "plan_hashes_seq", "current_planar_velocity_x", "current_planar_velocity_y", "current_planar_velocity_z", "continuation_planar_velocity_x", "continuation_planar_velocity_y", "continuation_planar_velocity_z", "current_segment_switch_delay_seconds", "has_continuation", "yaw_velocity_degrees_per_second", "maximum_yaw_velocity_degrees_per_second" },
                new[] { "count", "phase_seq", "x_seq", "y_seq", "z_seq", "min_y", "max_y", "start_x", "start_y", "start_z", "end_x", "end_y", "end_z", "hash" }
            };
            string[] names = { "ground_probe", "foot_rate", "ground_envelope", "clearance_path", "query_requests", "accepted_supports", "rejected_geometry", "landing", "plan_invariants", "animation_foot_route" };
            int write = offset;
            for (int group = 0; group < groups.Length; group++)
            {
                for (int field = 0; field < groups[group].Length; field++)
                    values[write++] = $"{prefix}_{names[group]}_{groups[group][field]}";
            }
            if (write != offset + SequenceColumnCount)
                throw new InvalidOperationException("Foot IK complete sequence header width is invalid.");
        }

        static void InsertCausalityHeader(List<string> values, int index, string prefix)
        {
            values.InsertRange(index, new[]
            {
                $"{prefix}_authored_animation_clearance",
                $"{prefix}_animation_clearance_continuity_offset",
                $"{prefix}_animation_clearance_continuity_contribution",
                $"{prefix}_reach_clearance",
                $"{prefix}_composite_animation_clearance",
                $"{prefix}_required_lift",
                $"{prefix}_applied_lift",
                $"{prefix}_baseline_goal_world_x",
                $"{prefix}_baseline_goal_world_y",
                $"{prefix}_baseline_goal_world_z",
                $"{prefix}_final_goal_world_x",
                $"{prefix}_final_goal_world_y",
                $"{prefix}_final_goal_world_z",
                $"{prefix}_virtual_ground_split_event_phase",
                $"{prefix}_virtual_ground_opposing_landing_x",
                $"{prefix}_virtual_ground_opposing_landing_y",
                $"{prefix}_virtual_ground_opposing_landing_z",
                $"{prefix}_virtual_ground_split_route_x",
                $"{prefix}_virtual_ground_split_route_y",
                $"{prefix}_virtual_ground_split_route_z",
                $"{prefix}_virtual_ground_split_planar_error"
            });
        }

        static string BuildRow(
            CharacterFootIkCompletedFrameSnapshot completed,
            in GameplayLabFootIkRouteSnapshot route,
            SequenceValueCache leftCache,
            SequenceValueCache rightCache)
        {
            RuntimeFootIkTraceSnapshot snapshot = completed.Trace;
            var values = new List<string>(ColumnCount)
            {
                Number(route.RenderFrame), Number(snapshot.FrameSequence), Number(snapshot.FrameSequence), Number(snapshot.ResetSequence),
                Number(snapshot.GroundingCompletionIdentity), Number(snapshot.ModifierCompletionIdentity), Number(snapshot.SolverCompletionIdentity), Bool(snapshot.HasPredictiveModifier),
                snapshot.SolverBackendIdentity, snapshot.SolverFailure, Bool(snapshot.NodeExecuted), Bool(snapshot.BodyGrounded), Number(snapshot.PlacementAlpha), Number(snapshot.PresentationDeltaSeconds), Number(snapshot.PoseRootVerticalDelta),
                Number(snapshot.PoseRootWorldPosition.x), Number(snapshot.PoseRootWorldPosition.y), Number(snapshot.PoseRootWorldPosition.z),
                Number(snapshot.PoseRootWorldRotation.x), Number(snapshot.PoseRootWorldRotation.y), Number(snapshot.PoseRootWorldRotation.z), Number(snapshot.PoseRootWorldRotation.w),
                snapshot.LyraSourceIdentity, snapshot.SpringIdentity, snapshot.RigId, snapshot.RigRevision, snapshot.ProfileId, snapshot.ProfileRevision,
                snapshot.PosePlanHash, snapshot.CalibrationId, snapshot.CalibrationRevision, Number(snapshot.PhysicsSceneIdentity), Number(snapshot.SelfFilterIdentity),
                Number(snapshot.PelvisLyraTargetOffset), Number(snapshot.PelvisResolvedTargetOffset), Number(snapshot.CurrentPelvisOffset), Number(snapshot.PelvisSpringVelocity),
                Number(snapshot.PreviousPelvisTarget), Bool(snapshot.PelvisSpringInitialized), Number(snapshot.PelvisPreSolveTranslation.x), Number(snapshot.PelvisPreSolveTranslation.y),
                Number(snapshot.PelvisPreSolveTranslation.z), Number(snapshot.PelvisGoalPositionWeight), snapshot.PelvisGoalApplication, snapshot.PelvisGoalSourceKind,
                Bool(snapshot.PelvisSupportAvailable), snapshot.PelvisSupportSide, Bool(snapshot.PelvisSupportSwitched), Number(snapshot.PelvisSupportPlanSequence),
                Number(snapshot.PelvisCurrentSupportTarget), Number(snapshot.PelvisSelectedSupportTarget),
                Bool(snapshot.LeftPelvisHasActionConstraint), snapshot.LeftPelvisConstraintMode, snapshot.LeftPelvisSupportPhase,
                snapshot.LeftPelvisBodyPivotMode, Bool(snapshot.LeftPelvisCandidate), Number(snapshot.LeftPelvisPlanSequence), Number(snapshot.LeftPelvisDisplacement),
                Bool(snapshot.RightPelvisHasActionConstraint), snapshot.RightPelvisConstraintMode, snapshot.RightPelvisSupportPhase,
                snapshot.RightPelvisBodyPivotMode, Bool(snapshot.RightPelvisCandidate), Number(snapshot.RightPelvisPlanSequence), Number(snapshot.RightPelvisDisplacement),
                Number(snapshot.BaselineProducerOperationIndex), Number(snapshot.BaselineProducerCallSiteIndex),
                Number(snapshot.BaselineGoalOffset), Number(snapshot.BaselineGoalCount), snapshot.BaselineRigId, snapshot.BaselineRigRevision
            };
            CharacterRuntimeDiagnosticsInspector.AppendFootIkLegValues(values, snapshot.Left);
            CharacterRuntimeDiagnosticsInspector.AppendFootIkLegValues(values, snapshot.Right);
            if (values.Count != BaseColumnCount)
                throw new InvalidOperationException($"Foot IK base CSV row has {values.Count} columns instead of {BaseColumnCount}.");
            CharacterPredictiveFootLegFrameSnapshot left = completed.HasPredictiveSnapshot
                ? completed.Predictive.Left
                : default;
            CharacterPredictiveFootLegFrameSnapshot right = completed.HasPredictiveSnapshot
                ? completed.Predictive.Right
                : default;
            ReplaceSequenceValues(values, GlobalColumnCount, left, route, leftCache);
            InsertCausalityValues(
                values,
                GlobalColumnCount + BeforeSequenceColumnCount + SequenceColumnCount,
                snapshot.Left,
                in left);
            ReplaceSequenceValues(values, GlobalColumnCount + LegColumnCount, right, route, rightCache);
            InsertCausalityValues(
                values,
                GlobalColumnCount + LegColumnCount + BeforeSequenceColumnCount + SequenceColumnCount,
                snapshot.Right,
                in right);
            if (values.Count != ColumnCount)
                throw new InvalidOperationException($"Foot IK CSV row has {values.Count} columns instead of {ColumnCount}.");
            var builder = new StringBuilder(65536);
            CharacterRuntimeDiagnosticsInspector.AppendCsvRow(builder, values.ToArray());
            return builder.ToString().TrimEnd('\r', '\n');
        }

        static void InsertCausalityValues(
            List<string> values,
            int index,
            RuntimeFootIkLegTraceSnapshot runtimeLeg,
            in CharacterPredictiveFootLegFrameSnapshot leg)
        {
            CharacterPredictiveFootPlanGeometrySnapshot plan = leg.Plan;
            values.InsertRange(index, new[]
            {
                Number(runtimeLeg.AuthoredAnimationClearance),
                Number(runtimeLeg.AnimationClearanceContinuityOffset),
                Number(runtimeLeg.AnimationClearanceContinuityContribution),
                Number(runtimeLeg.ReachClearance),
                Number(runtimeLeg.CompositeAnimationClearance),
                Number(leg.RequiredLift),
                Number(leg.AppliedLift),
                Number(leg.BaselineAnkle.x),
                Number(leg.BaselineAnkle.y),
                Number(leg.BaselineAnkle.z),
                Number(leg.FinalAnkle.x),
                Number(leg.FinalAnkle.y),
                Number(leg.FinalAnkle.z),
                Number(plan?.VirtualGroundSplitEventPhase ?? 0f),
                Number(plan?.VirtualGroundOpposingLanding.x ?? 0f),
                Number(plan?.VirtualGroundOpposingLanding.y ?? 0f),
                Number(plan?.VirtualGroundOpposingLanding.z ?? 0f),
                Number(plan?.VirtualGroundSplitRoutePoint.x ?? 0f),
                Number(plan?.VirtualGroundSplitRoutePoint.y ?? 0f),
                Number(plan?.VirtualGroundSplitRoutePoint.z ?? 0f),
                Number(plan?.VirtualGroundSplitPlanarError ?? 0f)
            });
        }

        static void ReplaceSequenceValues(
            List<string> values,
            int legOffset,
            in CharacterPredictiveFootLegFrameSnapshot leg,
            in GameplayLabFootIkRouteSnapshot route,
            SequenceValueCache cache)
        {
            List<string> replacement = BuildSequenceValues(leg, route, cache);
            if (replacement.Count != SequenceColumnCount)
                throw new InvalidOperationException($"Foot IK complete sequence has {replacement.Count} columns instead of {SequenceColumnCount}.");
            int offset = legOffset + BeforeSequenceColumnCount;
            for (int i = 0; i < SequenceColumnCount - ReplacedSequenceColumnCount; i++)
                values.Insert(offset + ReplacedSequenceColumnCount, string.Empty);
            for (int i = 0; i < replacement.Count; i++)
                values[offset + i] = replacement[i];
        }

        static List<string> BuildSequenceValues(
            in CharacterPredictiveFootLegFrameSnapshot leg,
            in GameplayLabFootIkRouteSnapshot route,
            SequenceValueCache cache)
        {
            CharacterPredictiveFootPlanGeometrySnapshot plan = leg.Plan;
            if (cache.TryGet(plan, out string[] cached))
            {
                var result = new List<string>(cached);
                UpdateDynamicSequenceValues(result, leg, route);
                return result;
            }
            IReadOnlyList<CharacterPredictiveFootRoutePointSnapshot> footRoute = plan?.GroundProbeRoute ?? Array.Empty<CharacterPredictiveFootRoutePointSnapshot>();
            IReadOnlyList<CharacterPredictiveFootRoutePointSnapshot> animationFootRoute = plan?.AnimationFootRoute ?? Array.Empty<CharacterPredictiveFootRoutePointSnapshot>();
            IReadOnlyList<CharacterPredictiveFootRatePointSnapshot> footRate = plan?.FootRate ?? Array.Empty<CharacterPredictiveFootRatePointSnapshot>();
            IReadOnlyList<CharacterPredictiveFootClearanceSegmentSnapshot> clearancePath = plan?.ClearancePath ?? Array.Empty<CharacterPredictiveFootClearanceSegmentSnapshot>();
            IReadOnlyList<CharacterPredictiveFootEnvelopeSegmentSnapshot> envelope = plan?.GroundEnvelope ?? Array.Empty<CharacterPredictiveFootEnvelopeSegmentSnapshot>();
            IReadOnlyList<CharacterPredictiveFootQueryRequestSnapshot> requests = plan?.QueryRequests ?? Array.Empty<CharacterPredictiveFootQueryRequestSnapshot>();
            IReadOnlyList<CharacterPredictiveFootQueryGeometrySnapshot> accepted = plan?.AcceptedSupports ?? Array.Empty<CharacterPredictiveFootQueryGeometrySnapshot>();
            IReadOnlyList<CharacterPredictiveFootQueryGeometrySnapshot> rejected = plan?.RejectedGeometry ?? Array.Empty<CharacterPredictiveFootQueryGeometrySnapshot>();
            var values = new List<string>(SequenceColumnCount);

            string routeFractions = Join(footRoute, value => Number(value.Fraction));
            string routeX = Join(footRoute, value => Number(value.Position.x));
            string routeY = Join(footRoute, value => Number(value.Position.y));
            string routeZ = Join(footRoute, value => Number(value.Position.z));
            string animationRoutePhases = Join(animationFootRoute, value => Number(value.Fraction));
            string animationRouteX = Join(animationFootRoute, value => Number(value.Position.x));
            string animationRouteY = Join(animationFootRoute, value => Number(value.Position.y));
            string animationRouteZ = Join(animationFootRoute, value => Number(value.Position.z));
            string animationRouteHash = Hash(
                animationRoutePhases,
                animationRouteX,
                animationRouteY,
                animationRouteZ);
            BoundsY(footRoute, value => value.Position.y, out string routeMinY, out string routeMaxY);
            CharacterPredictiveFootRoutePointSnapshot routeStart = footRoute.Count > 0 ? footRoute[0] : default;
            CharacterPredictiveFootRoutePointSnapshot routeEnd = footRoute.Count > 0 ? footRoute[footRoute.Count - 1] : default;
            values.AddRange(new[]
            {
                Number(footRoute.Count), routeFractions, routeX, routeY, routeZ, routeMinY, routeMaxY,
                Number(routeStart.Position.x), Number(routeStart.Position.y), Number(routeStart.Position.z),
                Number(routeEnd.Position.x), Number(routeEnd.Position.y), Number(routeEnd.Position.z),
                Hash(routeFractions, routeX, routeY, routeZ)
            });

            string footRatePhases = Join(footRate, value => Number(value.ActionPhase));
            string footRateProgress = Join(footRate, value => Number(value.GroundPathProgress));
            values.AddRange(new[]
            {
                Number(footRate.Count), footRatePhases, footRateProgress
            });

            string envelopeStartFraction = Join(envelope, value => Number(value.StartFraction));
            string envelopeEndFraction = Join(envelope, value => Number(value.EndFraction));
            string envelopeSurface = Join(envelope, value => Number(value.SurfaceIdentity));
            string envelopeNormalX = Join(envelope, value => Number(value.SurfaceNormal.x));
            string envelopeNormalY = Join(envelope, value => Number(value.SurfaceNormal.y));
            string envelopeNormalZ = Join(envelope, value => Number(value.SurfaceNormal.z));
            string envelopeStartX = Join(envelope, value => Number(value.EdgeStart.x));
            string envelopeStartY = Join(envelope, value => Number(value.EdgeStart.y));
            string envelopeStartZ = Join(envelope, value => Number(value.EdgeStart.z));
            string envelopeEndX = Join(envelope, value => Number(value.EdgeEnd.x));
            string envelopeEndY = Join(envelope, value => Number(value.EdgeEnd.y));
            string envelopeEndZ = Join(envelope, value => Number(value.EdgeEnd.z));
            string envelopeHeights = Join(envelope, value => Number(value.StartSoleHeight) + ":" + Number(value.EndSoleHeight));
            values.AddRange(new[]
            {
                Number(envelope.Count), envelopeStartFraction, envelopeEndFraction, envelopeSurface,
                envelopeNormalX, envelopeNormalY, envelopeNormalZ,
                envelopeStartX, envelopeStartY, envelopeStartZ,
                envelopeEndX, envelopeEndY, envelopeEndZ, envelopeHeights
            });

            string clearanceStartFraction = Join(clearancePath, value => Number(value.StartFraction));
            string clearanceEndFraction = Join(clearancePath, value => Number(value.EndFraction));
            string clearanceStartX = Join(clearancePath, value => Number(value.Start.x));
            string clearanceStartY = Join(clearancePath, value => Number(value.Start.y));
            string clearanceStartZ = Join(clearancePath, value => Number(value.Start.z));
            string clearanceEndX = Join(clearancePath, value => Number(value.End.x));
            string clearanceEndY = Join(clearancePath, value => Number(value.End.y));
            string clearanceEndZ = Join(clearancePath, value => Number(value.End.z));
            string clearanceStartHeight = Join(clearancePath, value => Number(value.StartHeight));
            string clearanceEndHeight = Join(clearancePath, value => Number(value.EndHeight));
            string clearanceSurface = Join(clearancePath, value => Number(value.SurfaceIdentity));
            string clearanceRootY = Join(clearancePath, value => Number(value.RootStart.y) + ":" + Number(value.RootEnd.y));
            string clearanceHipY = Join(clearancePath, value => Number(value.HipStart.y) + ":" + Number(value.HipEnd.y));
            values.AddRange(new[]
            {
                Number(clearancePath.Count), clearanceStartFraction, clearanceEndFraction,
                clearanceStartX, clearanceStartY, clearanceStartZ,
                clearanceEndX, clearanceEndY, clearanceEndZ,
                clearanceStartHeight, clearanceEndHeight, clearanceSurface, clearanceRootY, clearanceHipY
            });

            values.AddRange(new[]
            {
                Number(requests.Count), Join(requests, value => value.Shape + ":" + value.Purpose),
                Join(requests, value => Number(value.Origin.x)), Join(requests, value => Number(value.Origin.y)), Join(requests, value => Number(value.Origin.z)),
                Join(requests, value => Number(value.CapsuleEnd.x)), Join(requests, value => Number(value.CapsuleEnd.y)), Join(requests, value => Number(value.CapsuleEnd.z)),
                Join(requests, value => Number(value.Direction.x)), Join(requests, value => Number(value.Direction.y)), Join(requests, value => Number(value.Direction.z)),
                Join(requests, value => Number(value.MaximumDistance)), Join(requests, value => Number(value.Radius)),
                Join(requests, value => Number(value.LayerMask) + ":" + Number(value.MinimumGroundNormalDot))
            });

            BoundsY(accepted, value => value.Position.y, out string acceptedMinY, out string acceptedMaxY);
            values.AddRange(new[]
            {
                Number(accepted.Count), Join(accepted, value => Number(value.QueryIndex)), Join(accepted, value => Number(value.SurfaceIdentity)),
                Join(accepted, value => Number(value.Position.x)), Join(accepted, value => Number(value.Position.y)), Join(accepted, value => Number(value.Position.z)),
                Join(accepted, value => Number(value.Normal.x)), Join(accepted, value => Number(value.Normal.y)), Join(accepted, value => Number(value.Normal.z)),
                Join(accepted, value => value.RejectReason), acceptedMinY, acceptedMaxY,
                accepted.Count > 0 ? Number(accepted[0].QueryIndex) : string.Empty,
                accepted.Count > 0 ? Number(accepted[accepted.Count - 1].QueryIndex) : string.Empty
            });

            BoundsY(rejected, value => value.Position.y, out string rejectedMinY, out string rejectedMaxY);
            values.AddRange(new[]
            {
                Number(rejected.Count), Join(rejected, value => Number(value.QueryIndex)), Join(rejected, value => Number(value.SurfaceIdentity)),
                Join(rejected, value => value.RejectReason),
                Join(rejected, value => Number(value.Position.x)), Join(rejected, value => Number(value.Position.y)), Join(rejected, value => Number(value.Position.z)),
                Join(rejected, value => Number(value.Normal.x)), Join(rejected, value => Number(value.Normal.y)), Join(rejected, value => Number(value.Normal.z)),
                rejectedMinY, rejectedMaxY,
                rejected.Count > 0 ? Number(rejected[0].QueryIndex) : string.Empty,
                rejected.Count > 0 ? Number(rejected[rejected.Count - 1].QueryIndex) : string.Empty
            });

            values.AddRange(new[]
            {
                Number(plan?.PlanSequence ?? 0UL), Number(plan?.GeneratedFrame ?? 0UL), Number(plan?.LandingEventIdentity ?? 0UL),
                Bool(plan?.Executable ?? false), Bool(plan?.LandingValid ?? false),
                Number(plan?.Landing.x ?? 0f), Number(plan?.Landing.y ?? 0f), Number(plan?.Landing.z ?? 0f),
                Number(leg.CurrentPath.x), Number(leg.CurrentPath.y), Number(leg.CurrentPath.z),
                Bool(leg.ClearanceEvaluated), Bool(leg.Rewritten), Number(leg.ActionProgress),
                Number(leg.GroundPathProgress),
                Bool(plan?.VirtualGroundSplitValid ?? false), Number(plan?.VirtualGroundSplitFraction ?? 0f),
                Number(plan?.VirtualGroundSplitLandingEventIdentity ?? 0UL),
                Number(plan?.VirtualGroundSplit.x ?? 0f), Number(plan?.VirtualGroundSplit.y ?? 0f),
                Number(plan?.VirtualGroundSplit.z ?? 0f)
            });

            string planHashes = string.Join(";", new[]
            {
                Hash(routeFractions, routeX, routeY, routeZ),
                animationRouteHash,
                Hash(footRatePhases, footRateProgress),
                Hash(envelopeStartFraction, envelopeEndFraction, envelopeSurface, envelopeStartX, envelopeStartY, envelopeStartZ, envelopeEndX, envelopeEndY, envelopeEndZ, envelopeHeights),
                Hash(Join(requests, value => value.Shape), Join(requests, value => value.Purpose), Join(requests, value => Number(value.Origin.x)), Join(requests, value => Number(value.Origin.y)), Join(requests, value => Number(value.Origin.z))),
                Hash(Join(accepted, value => Number(value.QueryIndex)), Join(accepted, value => Number(value.SurfaceIdentity)), Join(accepted, value => Number(value.Position.y))),
                Hash(Join(rejected, value => Number(value.QueryIndex)), Join(rejected, value => value.RejectReason), Join(rejected, value => Number(value.Position.y))),
                Hash(clearanceStartFraction, clearanceEndFraction, clearanceStartX, clearanceStartY,
                    clearanceStartZ, clearanceEndX, clearanceEndY, clearanceEndZ,
                    clearanceStartHeight, clearanceEndHeight)
            });
            values.AddRange(new[]
            {
                route.Phase.ToString(), route.Direction, Number(route.Lap),
                Number(route.ActorPosition.x), Number(route.ActorPosition.y), Number(route.ActorPosition.z),
                Number(route.ActorYawDegrees), Number(route.Movement.x), Number(route.Movement.y),
                Number(route.ActualPlanarSpeed), Number(route.SimulationTick), Number(route.TickRate), Number(route.Movement.magnitude),
                planHashes,
                Number(plan?.CurrentPlanarVelocity.x ?? 0f), Number(plan?.CurrentPlanarVelocity.y ?? 0f),
                Number(plan?.CurrentPlanarVelocity.z ?? 0f), Number(plan?.ContinuationPlanarVelocity.x ?? 0f),
                Number(plan?.ContinuationPlanarVelocity.y ?? 0f), Number(plan?.ContinuationPlanarVelocity.z ?? 0f),
                Number(plan?.CurrentSegmentSwitchDelaySeconds ?? 0f), Bool(plan?.HasContinuation ?? false),
                Number(plan?.YawVelocityDegreesPerSecond ?? 0f),
                Number(plan?.MaximumYawVelocityDegreesPerSecond ?? 0f)
            });

            BoundsY(
                animationFootRoute,
                value => value.Position.y,
                out string animationRouteMinY,
                out string animationRouteMaxY);
            CharacterPredictiveFootRoutePointSnapshot animationRouteStart =
                animationFootRoute.Count > 0 ? animationFootRoute[0] : default;
            CharacterPredictiveFootRoutePointSnapshot animationRouteEnd =
                animationFootRoute.Count > 0 ? animationFootRoute[animationFootRoute.Count - 1] : default;
            values.AddRange(new[]
            {
                Number(animationFootRoute.Count),
                animationRoutePhases,
                animationRouteX,
                animationRouteY,
                animationRouteZ,
                animationRouteMinY,
                animationRouteMaxY,
                Number(animationRouteStart.Position.x),
                Number(animationRouteStart.Position.y),
                Number(animationRouteStart.Position.z),
                Number(animationRouteEnd.Position.x),
                Number(animationRouteEnd.Position.y),
                Number(animationRouteEnd.Position.z),
                animationRouteHash
            });
            cache.Store(plan, values);
            return values;
        }

        static void UpdateDynamicSequenceValues(
            List<string> values,
            in CharacterPredictiveFootLegFrameSnapshot leg,
            in GameplayLabFootIkRouteSnapshot route)
        {
            values[95] = Number(leg.CurrentPath.x);
            values[96] = Number(leg.CurrentPath.y);
            values[97] = Number(leg.CurrentPath.z);
            values[98] = Bool(leg.ClearanceEvaluated);
            values[99] = Bool(leg.Rewritten);
            values[100] = Number(leg.ActionProgress);
            values[101] = Number(leg.GroundPathProgress);
            values[108] = route.Phase.ToString();
            values[109] = route.Direction;
            values[110] = Number(route.Lap);
            values[111] = Number(route.ActorPosition.x);
            values[112] = Number(route.ActorPosition.y);
            values[113] = Number(route.ActorPosition.z);
            values[114] = Number(route.ActorYawDegrees);
            values[115] = Number(route.Movement.x);
            values[116] = Number(route.Movement.y);
            values[117] = Number(route.ActualPlanarSpeed);
            values[118] = Number(route.SimulationTick);
            values[119] = Number(route.TickRate);
            values[120] = Number(route.Movement.magnitude);
        }

        static string Join<T>(IReadOnlyList<T> values, Func<T, string> selector)
        {
            if (values == null || values.Count == 0)
                return string.Empty;
            var builder = new StringBuilder(values.Count * 12);
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                    builder.Append(';');
                string value = selector(values[i]) ?? string.Empty;
                builder.Append(value.Replace(";", "/"));
            }
            return builder.ToString();
        }

        static void BoundsY<T>(IReadOnlyList<T> values, Func<T, float> selector, out string minimum, out string maximum)
        {
            if (values == null || values.Count == 0)
            {
                minimum = string.Empty;
                maximum = string.Empty;
                return;
            }
            float min = selector(values[0]);
            float max = min;
            for (int i = 1; i < values.Count; i++)
            {
                float value = selector(values[i]);
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
            minimum = Number(min);
            maximum = Number(max);
        }

        static string Hash(params string[] values)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong hash = offset;
            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i] ?? string.Empty;
                for (int character = 0; character < value.Length; character++)
                {
                    hash ^= value[character];
                    hash *= prime;
                }
                hash ^= 0xff;
                hash *= prime;
            }
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }

        static string Number<T>(T value) where T : IFormattable =>
            value.ToString(null, CultureInfo.InvariantCulture);

        static string Bool(bool value) => value ? "true" : "false";

        sealed class CaptureRun : IDisposable
        {
            readonly string[] m_Header;
            readonly CaptureManifest m_Manifest;
            readonly string m_ManifestPath;
            readonly SequenceValueCache m_LeftSequenceCache = new SequenceValueCache();
            readonly SequenceValueCache m_RightSequenceCache = new SequenceValueCache();
            ChunkWriter m_Chunk;
            int m_ChunkSequence;
            bool m_Disposed;

            public CaptureRun(string runId, string[] header)
            {
                RunId = runId;
                m_Header = header;
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ??
                                     throw new InvalidOperationException("Unity project root could not be resolved.");
                DirectoryPath = Path.Combine(projectRoot, "FootIkEnduranceRuns", runId);
                if (Directory.Exists(DirectoryPath))
                    throw new InvalidOperationException($"Foot IK endurance run directory already exists: {DirectoryPath}");
                Directory.CreateDirectory(DirectoryPath);
                m_ManifestPath = Path.Combine(DirectoryPath, "manifest.json");
                m_Manifest = new CaptureManifest
                {
                    runId = runId,
                    schema = "foot-ik-1189-se2-piecewise-virtual-ground-v93",
                    columnCount = ColumnCount,
                    startedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    status = "running"
                };
                WriteManifest();
            }

            public string RunId { get; }
            public string DirectoryPath { get; }

            public void Write(
                CharacterFootIkCompletedFrameSnapshot frame,
                in GameplayLabFootIkRouteSnapshot route)
            {
                if (m_Disposed)
                    throw new ObjectDisposedException(nameof(CaptureRun));
                if (m_Chunk == null || m_Chunk.RowCount >= MaximumChunkRows ||
                    m_Chunk.Lap != route.Lap || !string.Equals(m_Chunk.Direction, route.Direction, StringComparison.Ordinal))
                {
                    CloseChunk();
                    m_Chunk = new ChunkWriter(
                        DirectoryPath,
                        ++m_ChunkSequence,
                        route,
                        m_Header);
                }
                long buildStarted = System.Diagnostics.Stopwatch.GetTimestamp();
                string row = BuildRow(frame, route, m_LeftSequenceCache, m_RightSequenceCache);
                double buildMilliseconds = ElapsedMilliseconds(buildStarted);
                long enqueueStarted = System.Diagnostics.Stopwatch.GetTimestamp();
                m_Chunk.Write(row, route);
                double enqueueMilliseconds = ElapsedMilliseconds(enqueueStarted);
                m_Manifest.rowBuildTotalMilliseconds += buildMilliseconds;
                m_Manifest.rowBuildMaximumMilliseconds = Math.Max(
                    m_Manifest.rowBuildMaximumMilliseconds,
                    buildMilliseconds);
                m_Manifest.rowEnqueueTotalMilliseconds += enqueueMilliseconds;
                m_Manifest.rowEnqueueMaximumMilliseconds = Math.Max(
                    m_Manifest.rowEnqueueMaximumMilliseconds,
                    enqueueMilliseconds);
            }

            public void Dispose() => Dispose("disposed");

            public void Dispose(string status)
            {
                if (m_Disposed)
                    return;
                m_Disposed = true;
                CloseChunk();
                m_Manifest.status = status;
                m_Manifest.endedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                WriteManifest();
                Debug.Log($"GameplayLab Foot IK endurance capture {status}: {DirectoryPath}");
            }

            void CloseChunk()
            {
                if (m_Chunk == null)
                    return;
                ChunkWriter chunk = m_Chunk;
                m_Chunk = null;
                CaptureChunk entry = chunk.Complete();
                m_Manifest.chunks.Add(entry);
                m_Manifest.totalRows += entry.rowCount;
                WriteManifest();
            }

            void WriteManifest()
            {
                if (m_Manifest.totalRows > 0)
                {
                    m_Manifest.rowBuildAverageMilliseconds =
                        m_Manifest.rowBuildTotalMilliseconds / m_Manifest.totalRows;
                    m_Manifest.rowEnqueueAverageMilliseconds =
                        m_Manifest.rowEnqueueTotalMilliseconds / m_Manifest.totalRows;
                }
                string temporary = m_ManifestPath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(m_Manifest, true), s_Utf8);
                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        if (File.Exists(m_ManifestPath))
                            File.Replace(temporary, m_ManifestPath, null);
                        else
                            File.Move(temporary, m_ManifestPath);
                        return;
                    }
                    catch (IOException) when (attempt + 1 < ManifestWriteAttemptCount)
                    {
                        Thread.Sleep((attempt + 1) * 10);
                    }
                }
            }

            static double ElapsedMilliseconds(long started) =>
                (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000d /
                System.Diagnostics.Stopwatch.Frequency;
        }

        sealed class SequenceValueCache
        {
            bool m_Initialized;
            CharacterPredictiveFootPlanGeometrySnapshot m_Plan;
            string[] m_Values;

            public bool TryGet(
                CharacterPredictiveFootPlanGeometrySnapshot plan,
                out string[] values)
            {
                if (m_Initialized &&
                    ReferenceEquals(plan, m_Plan))
                {
                    values = m_Values;
                    return true;
                }
                values = null;
                return false;
            }

            public void Store(
                CharacterPredictiveFootPlanGeometrySnapshot plan,
                List<string> values)
            {
                m_Initialized = true;
                m_Plan = plan;
                m_Values = values.ToArray();
            }
        }

        sealed class ChunkWriter : IDisposable
        {
            const int MaximumQueuedRows = 256;
            readonly string m_Directory;
            readonly int m_Sequence;
            readonly string m_PartialPath;
            readonly FileStream m_File;
            readonly GZipStream m_Gzip;
            readonly StreamWriter m_Writer;
            readonly DateTime m_StartedUtcValue;
            readonly string m_StartedUtc;
            readonly BlockingCollection<string> m_Rows =
                new BlockingCollection<string>(MaximumQueuedRows);
            readonly Thread m_Thread;
            Exception m_BackgroundFailure;
            ulong m_FirstSimulationTick;
            ulong m_LastSimulationTick;
            int m_TickRate;
            Vector3 m_FirstActorPosition;
            Vector3 m_LastActorPosition;
            Vector3 m_PreviousActorPosition;
            Vector3 m_RouteStart;
            Vector3 m_RouteEnd;
            float m_FirstActorYawDegrees;
            float m_LastActorYawDegrees;
            double m_HorizontalPathDistance;
            double m_MaximumCrossTrackDistance;
            double m_InputMagnitudeTotal;
            double m_MinimumInputMagnitude = double.PositiveInfinity;
            double m_MaximumInputMagnitude;
            double m_ActualPlanarSpeedTotal;
            double m_MinimumActualPlanarSpeed = double.PositiveInfinity;
            double m_MaximumActualPlanarSpeed;
            bool m_Completed;

            public ChunkWriter(
                string directory,
                int sequence,
                in GameplayLabFootIkRouteSnapshot route,
                string[] header)
            {
                m_Directory = directory;
                m_Sequence = sequence;
                Direction = Sanitize(route.Direction);
                Lap = route.Lap;
                FirstFrame = route.RenderFrame;
                LastFrame = route.RenderFrame;
                m_StartedUtcValue = DateTime.UtcNow;
                m_StartedUtc = m_StartedUtcValue.ToString("O", CultureInfo.InvariantCulture);
                m_PartialPath = Path.Combine(directory, $"chunk-{sequence:D6}-{Direction}-lap-{Lap:D5}.partial.csv.gz");
                m_File = new FileStream(m_PartialPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 131072, FileOptions.SequentialScan);
                m_Gzip = new GZipStream(m_File, System.IO.Compression.CompressionLevel.Fastest, true);
                m_Writer = new StreamWriter(m_Gzip, s_Utf8, 131072, true);
                var builder = new StringBuilder(32768);
                CharacterRuntimeDiagnosticsInspector.AppendCsvRow(builder, header);
                m_Writer.Write(builder.ToString());
                m_Thread = new Thread(WriteRows)
                {
                    IsBackground = true,
                    Name = $"FootIkCsv-{sequence:D6}"
                };
                m_Thread.Start();
            }

            public string Direction { get; }
            public int Lap { get; }
            public ulong FirstFrame { get; }
            public ulong LastFrame { get; private set; }
            public int RowCount { get; private set; }

            public void Write(string row, in GameplayLabFootIkRouteSnapshot route)
            {
                if (m_Completed)
                    throw new InvalidOperationException("Foot IK CSV chunk is already complete.");
                ThrowBackgroundFailure();
                while (!m_Rows.TryAdd(row, 50))
                    ThrowBackgroundFailure();
                UpdateRouteMetrics(route);
                LastFrame = route.RenderFrame;
                RowCount++;
            }

            public CaptureChunk Complete()
            {
                if (m_Completed)
                    throw new InvalidOperationException("Foot IK CSV chunk was completed twice.");
                m_Completed = true;
                m_Rows.CompleteAdding();
                m_Thread.Join();
                Exception backgroundFailure = m_BackgroundFailure;
                m_Writer.Dispose();
                m_Gzip.Dispose();
                m_File.Dispose();
                m_Rows.Dispose();
                if (backgroundFailure != null)
                    throw new IOException("Foot IK CSV background writer failed.", backgroundFailure);
                string fileName = $"chunk-{m_Sequence:D6}-{Direction}-lap-{Lap:D5}-frames-{FirstFrame}-{LastFrame}.csv.gz";
                string finalPath = Path.Combine(m_Directory, fileName);
                File.Move(m_PartialPath, finalPath);
                DateTime endedUtcValue = DateTime.UtcNow;
                double wallSeconds = Math.Max(0d, (endedUtcValue - m_StartedUtcValue).TotalSeconds);
                double simulationSeconds = m_TickRate > 0 && m_LastSimulationTick >= m_FirstSimulationTick
                    ? (m_LastSimulationTick - m_FirstSimulationTick) / (double)m_TickRate
                    : 0d;
                return new CaptureChunk
                {
                    file = fileName,
                    direction = Direction,
                    lap = Lap,
                    firstFrame = FirstFrame.ToString(CultureInfo.InvariantCulture),
                    lastFrame = LastFrame.ToString(CultureInfo.InvariantCulture),
                    firstSimulationTick = m_FirstSimulationTick.ToString(CultureInfo.InvariantCulture),
                    lastSimulationTick = m_LastSimulationTick.ToString(CultureInfo.InvariantCulture),
                    tickRate = m_TickRate,
                    rowCount = RowCount,
                    startedUtc = m_StartedUtc,
                    endedUtc = endedUtcValue.ToString("O", CultureInfo.InvariantCulture),
                    wallSeconds = wallSeconds,
                    simulationSeconds = simulationSeconds,
                    firstActorX = m_FirstActorPosition.x,
                    firstActorY = m_FirstActorPosition.y,
                    firstActorZ = m_FirstActorPosition.z,
                    lastActorX = m_LastActorPosition.x,
                    lastActorY = m_LastActorPosition.y,
                    lastActorZ = m_LastActorPosition.z,
                    firstActorYawDegrees = m_FirstActorYawDegrees,
                    lastActorYawDegrees = m_LastActorYawDegrees,
                    horizontalPathDistance = m_HorizontalPathDistance,
                    meanSimulationHorizontalSpeed = simulationSeconds > 0d
                        ? m_HorizontalPathDistance / simulationSeconds
                        : 0d,
                    meanWallHorizontalSpeed = wallSeconds > 0d
                        ? m_HorizontalPathDistance / wallSeconds
                        : 0d,
                    minimumInputMagnitude = double.IsPositiveInfinity(m_MinimumInputMagnitude)
                        ? 0d
                        : m_MinimumInputMagnitude,
                    maximumInputMagnitude = m_MaximumInputMagnitude,
                    averageInputMagnitude = RowCount > 0 ? m_InputMagnitudeTotal / RowCount : 0d,
                    minimumActualPlanarSpeed = double.IsPositiveInfinity(m_MinimumActualPlanarSpeed)
                        ? 0d
                        : m_MinimumActualPlanarSpeed,
                    maximumActualPlanarSpeed = m_MaximumActualPlanarSpeed,
                    averageActualPlanarSpeed = RowCount > 0 ? m_ActualPlanarSpeedTotal / RowCount : 0d,
                    maximumCrossTrackDistance = m_MaximumCrossTrackDistance,
                    compressedBytes = new FileInfo(finalPath).Length
                };
            }

            public void Dispose()
            {
                if (!m_Rows.IsAddingCompleted)
                    m_Rows.CompleteAdding();
                if (m_Thread.IsAlive)
                    m_Thread.Join();
                m_Writer.Dispose();
                m_Gzip.Dispose();
                m_File.Dispose();
                m_Rows.Dispose();
            }

            void WriteRows()
            {
                try
                {
                    int count = 0;
                    foreach (string row in m_Rows.GetConsumingEnumerable())
                    {
                        m_Writer.WriteLine(row);
                        count++;
                        if ((count & 127) == 0)
                            m_Writer.Flush();
                    }
                }
                catch (Exception exception)
                {
                    m_BackgroundFailure = exception;
                }
            }

            void ThrowBackgroundFailure()
            {
                if (m_BackgroundFailure != null)
                    throw new IOException("Foot IK CSV background writer failed.", m_BackgroundFailure);
            }

            void UpdateRouteMetrics(in GameplayLabFootIkRouteSnapshot route)
            {
                Vector3 position = route.ActorPosition;
                double magnitude = route.Movement.magnitude;
                if (RowCount == 0)
                {
                    m_FirstSimulationTick = route.SimulationTick;
                    m_FirstActorPosition = position;
                    m_PreviousActorPosition = position;
                    m_RouteStart = route.Start;
                    m_RouteEnd = route.End;
                    m_FirstActorYawDegrees = route.ActorYawDegrees;
                    m_TickRate = route.TickRate;
                }
                else
                {
                    Vector2 delta = new Vector2(
                        position.x - m_PreviousActorPosition.x,
                        position.z - m_PreviousActorPosition.z);
                    m_HorizontalPathDistance += delta.magnitude;
                }
                m_LastSimulationTick = route.SimulationTick;
                m_LastActorPosition = position;
                m_PreviousActorPosition = position;
                m_LastActorYawDegrees = route.ActorYawDegrees;
                m_InputMagnitudeTotal += magnitude;
                m_MinimumInputMagnitude = Math.Min(m_MinimumInputMagnitude, magnitude);
                m_MaximumInputMagnitude = Math.Max(m_MaximumInputMagnitude, magnitude);
                m_ActualPlanarSpeedTotal += route.ActualPlanarSpeed;
                m_MinimumActualPlanarSpeed = Math.Min(m_MinimumActualPlanarSpeed, route.ActualPlanarSpeed);
                m_MaximumActualPlanarSpeed = Math.Max(m_MaximumActualPlanarSpeed, route.ActualPlanarSpeed);
                m_MaximumCrossTrackDistance = Math.Max(
                    m_MaximumCrossTrackDistance,
                    CrossTrackDistance(position, m_RouteStart, m_RouteEnd));
            }

            static float CrossTrackDistance(Vector3 position, Vector3 start, Vector3 end)
            {
                Vector2 origin = new Vector2(start.x, start.z);
                Vector2 segment = new Vector2(end.x - start.x, end.z - start.z);
                Vector2 point = new Vector2(position.x, position.z);
                if (segment.sqrMagnitude <= 0.000001f)
                    return Vector2.Distance(point, origin);
                float t = Mathf.Clamp01(Vector2.Dot(point - origin, segment) / segment.sqrMagnitude);
                return Vector2.Distance(point, origin + segment * t);
            }

            static string Sanitize(string value)
            {
                var builder = new StringBuilder(value?.Length ?? 0);
                foreach (char character in value ?? string.Empty)
                    builder.Append(char.IsLetterOrDigit(character) || character == '-' ? character : '-');
                return builder.Length > 0 ? builder.ToString() : "unknown";
            }

        }

        [Serializable]
        sealed class CaptureManifest
        {
            public string runId;
            public string schema;
            public int columnCount;
            public string startedUtc;
            public string endedUtc;
            public string status;
            public int totalRows;
            public double rowBuildTotalMilliseconds;
            public double rowBuildAverageMilliseconds;
            public double rowBuildMaximumMilliseconds;
            public double rowEnqueueTotalMilliseconds;
            public double rowEnqueueAverageMilliseconds;
            public double rowEnqueueMaximumMilliseconds;
            public List<CaptureChunk> chunks = new List<CaptureChunk>();
        }

        [Serializable]
        sealed class CaptureChunk
        {
            public string file;
            public string direction;
            public int lap;
            public string firstFrame;
            public string lastFrame;
            public string firstSimulationTick;
            public string lastSimulationTick;
            public int tickRate;
            public int rowCount;
            public string startedUtc;
            public string endedUtc;
            public double wallSeconds;
            public double simulationSeconds;
            public float firstActorX;
            public float firstActorY;
            public float firstActorZ;
            public float lastActorX;
            public float lastActorY;
            public float lastActorZ;
            public float firstActorYawDegrees;
            public float lastActorYawDegrees;
            public double horizontalPathDistance;
            public double meanSimulationHorizontalSpeed;
            public double meanWallHorizontalSpeed;
            public double minimumInputMagnitude;
            public double maximumInputMagnitude;
            public double averageInputMagnitude;
            public double minimumActualPlanarSpeed;
            public double maximumActualPlanarSpeed;
            public double averageActualPlanarSpeed;
            public double maximumCrossTrackDistance;
            public long compressedBytes;
        }
    }
}
