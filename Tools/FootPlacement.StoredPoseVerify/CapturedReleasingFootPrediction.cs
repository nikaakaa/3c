/*
目的：把已校准的Action新事件接入同一Releasing双脚业务的真实预测、路径与骨盆反馈。
输入：2034完整输出种子、2035～2046双脚前态/姿势、Native正式FootMotion、实际PhysicsScene和录制KCC未来身体端口。
链路：正式ResolveBodyTrajectory → PredictFootPair → 实际查询 → PrepareGroundPath → Landing/Lifecycle → 双脚骨盆 → Goal。
边界：模块私有函数只投影实际消费的body/timeline字段；KCC预测输出为外部固定输入，未执行FBBIK。
说明：接续releasing-action-native.html，先检查历史重现，再比较来源修正。
*/
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    internal struct CapturedPredictionBody
    {
        internal Vector3 VisiblePosition, TargetVelocity;
        internal Quaternion VisibleRotation;
        internal ulong ResetSequence, CurrentTick;
    }
    internal struct CapturedPredictionTimeline
    {
        internal bool IsValid, HasContinuation;
        internal ulong Generation;
        internal SimulationTick AuthorityTick;
        internal float ContinuationVelocityX, ContinuationVelocityZ;
    }
    internal struct CapturedPredictionFrame { internal CapturedPredictionBody Body; }

    public sealed partial class CharacterFootCapturedContactTests
    {
        static JObject ReleasingFrameJson(in StoredGoalOutput value)
        {
            var prediction = value.Prediction;
            var observation = prediction.Observation;
            var transition = value.Motion.LifecycleTransition;
            var response = value.Motion.PathContinuity;
            return new JObject
            {
                ["pathState"] = value.PathState.ToString(), ["pathRejectReason"] = value.PathRejectReason.ToString(),
                ["pathInputIdentity"] = value.PathInputIdentity.ToString(), ["pathQueryExecuted"] = value.PathQueryExecuted,
                ["pathContactCount"] = value.PathContactCount, ["pathEnvelopeVertexCount"] = value.PathEnvelopeVertexCount,
                ["pathSegmentCount"] = value.PathSegmentCount, ["pathLastLanding"] = Vec(value.PathLastLanding), ["pathNextLanding"] = Vec(value.PathNextLanding),
                ["predictionRejectReason"] = prediction.RejectReason.ToString(), ["predictionTimeToLanding"] = prediction.TimeToLandingSeconds,
                ["predictionRawLanding"] = Vec(prediction.RawLandingCandidate), ["predictionLanding"] = Vec(prediction.LandingPoint),
                ["observationIdentity"] = observation.Identity.ToString(), ["observationCacheState"] = observation.CacheState.ToString(),
                ["observationQueryExecuted"] = observation.QueryExecutedThisFrame, ["observationQueryReason"] = observation.QueryReason.ToString(),
                ["observationSourceSampleIdentity"] = observation.SourceSampleIdentity.ToString(), ["observationSourceSampleCycle"] = observation.SourceSampleCycle,
                ["observationCanonicalLanding"] = Vec(observation.CanonicalRawLanding), ["observationCandidateLanding"] = Vec(observation.CandidateRawLanding),
                ["preTransitionReason"] = transition.PreTransition.Reason.ToString(), ["preTransitionSource"] = transition.PreTransition.SourceState.ToString(),
                ["preTransitionTarget"] = transition.PreTransition.TargetState.ToString(), ["preTransitionResetInterpolation"] = transition.PreTransition.ResetInterpolation,
                ["preTransitionAnchorCommand"] = transition.PreTransition.AnchorCommand.ToString(),
                ["postTransitionReason"] = transition.PostTransition.Reason.ToString(), ["postTransitionEvaluated"] = transition.PostTransitionEvaluated,
                ["postTransitionSource"] = transition.PostTransition.SourceState.ToString(), ["postTransitionTarget"] = transition.PostTransition.TargetState.ToString(),
                ["correctionResponseEvaluated"] = response.CorrectionResponseEvaluated, ["correctionResponseDomain"] = response.CorrectionResponseDomain.ToString(),
                ["correctionResponseDesired"] = response.CorrectionResponseDesired, ["correctionResponsePrevious"] = response.CorrectionResponsePrevious,
                ["correctionResponseCurrent"] = response.CorrectionResponseCurrent, ["correctionResponseAppliedDelta"] = response.CorrectionResponseAppliedDelta,
                ["correctionResponseSelectedSpeed"] = response.CorrectionResponseSelectedSpeed,
                ["previousResponseOutputPoint"] = Vec(response.PreviousResponseOutputPoint), ["desiredOutputPoint"] = Vec(response.DesiredOutputPoint),
                ["responseOutputPoint"] = Vec(response.ResponseOutputPoint), ["stateTargetCorrection"] = Vec(response.StateTargetCorrection),
                ["interpolationOutputCorrection"] = Vec(response.InterpolationOutputCorrection), ["safetyFloorOutputCorrection"] = Vec(response.SafetyFloorOutputCorrection),
                ["plantTargetHeightBefore"] = response.PlantTargetHeightBefore, ["plantTargetHeightTarget"] = response.PlantTargetHeightTarget,
                ["plantTargetHeightAfter"] = response.PlantTargetHeightAfter, ["plantTargetHeightUpdateReason"] = response.PlantTargetHeightUpdateReason.ToString(),
                ["plantResidualAfterDecay"] = Vec(response.PlantWorldResidualAfterDecay), ["plantResidualCaptureReason"] = response.PlantResidualCaptureReason.ToString()
            };
        }
        static JObject ReleasingGoalJson(CharacterFullBodyIkGoal goal) => new JObject {
            ["slot"]=(int)goal.Slot, ["position"]=Vec(goal.ComponentPosition),
            ["rotation"]=new JArray(goal.ComponentRotation.x,goal.ComponentRotation.y,goal.ComponentRotation.z,goal.ComponentRotation.w),
            ["positionWeight"]=goal.PositionWeight, ["rotationWeight"]=goal.RotationWeight,
            ["application"]=(int)goal.Application, ["sourceKind"]=(int)goal.SourceKind, ["metadataIndex"]=goal.DiagnosticMetadataIndex };
        sealed class ReleasingPredictionInput
        {
            internal readonly CapturedPredictionBody Body;
            internal readonly CapturedPredictionTimeline Timeline;
            internal readonly CharacterFutureBodyTranslation Forecast = new CharacterFutureBodyTranslation();
            internal readonly bool ForecastAvailable;
            internal readonly float SegmentRemaining;
            internal CharacterFutureBodyTranslationRequest LastRequest;
            internal bool Requested;
            internal ReleasingPredictionInput(JToken captured, JObject columns)
            {
                Row r = Row.Main(captured, columns);
                Body = new CapturedPredictionBody { VisiblePosition = r.V("input/visible-body-position"), VisibleRotation = r.Q("input/visible-body-rotation"),
                    TargetVelocity = r.V("input/target-body-velocity"), ResetSequence = r.U("input/body-reset-sequence"), CurrentTick = r.U("input/current-body-tick") };
                Timeline = new CapturedPredictionTimeline { IsValid = r.B("input/motion-timeline-available"), Generation = r.U("input/timeline-generation"),
                    AuthorityTick = new SimulationTick(r.U("input/timeline-authority-tick")), HasContinuation = r.B("input/timeline-has-continuation"),
                    ContinuationVelocityX = r.F("input/timeline-continuation-velocity-x"), ContinuationVelocityZ = r.F("input/timeline-continuation-velocity-z") };
                SegmentRemaining = r.F("input/current-segment-remaining-seconds");
                ForecastAvailable = r.B("input/body-trajectory/available");
                if (ForecastAvailable)
                {
                    var samples = Row.Table(captured, columns, "bodyTrajectory").OrderBy(x => x.I("sample-index")).Select(x =>
                    {
                        Vector3 p = x.V("relative-position"), v = x.V("velocity");
                        return new CharacterFutureBodyTranslationSample(x.F("elapsed-seconds"), p.x, p.y, p.z, v.x, v.y, v.z);
                    }).ToArray();
                    Forecast.Set(r.S("input/body-trajectory/source-identity"), samples);
                }
            }
        }

        sealed class RecordedFutureBodyPort : ICharacterFutureBodyTranslationSource
        {
            internal ReleasingPredictionInput Current;
            public string PredictionSourceIdentity { get; }
            internal RecordedFutureBodyPort(string identity) => PredictionSourceIdentity = identity;
            public bool TryPredict(in CharacterFutureBodyTranslationRequest request, CharacterFutureBodyTranslation output)
            {
                Current.LastRequest = request;
                Current.Requested = true;
                if (Current.ForecastAvailable) output.CopyFrom(Current.Forecast);
                return Current.ForecastAvailable;
            }
        }

        sealed class ReleasingPredictionWorkspace
        {
            readonly CapturedFootPredictionFunctions functions;
            readonly RecordedFutureBodyPort future;
            readonly CharacterFootPlacementBank bodyBank = new CharacterFootPlacementBank();
            readonly CharacterFootPredictionMotionState seedMotion;
            readonly ReleasingPredictionInput seedBody;
            readonly CharacterFootLandingObservationPage seedLeftObservation, seedRightObservation;
            readonly CharacterFootGroundPathPage seedLeftPath, seedRightPath;
            readonly CharacterFootLandingObservationPagePool leftObservationPool = new CharacterFootLandingObservationPagePool();
            readonly CharacterFootLandingObservationPagePool rightObservationPool = new CharacterFootLandingObservationPagePool();
            readonly CharacterFootGroundPathPagePool leftPathPool, rightPathPool;
            CharacterFootLandingObservationPage leftObservation, rightObservation;
            CharacterFootGroundPathPage leftPath, rightPath;
            internal readonly CharacterFootPelvisSpringState SpringSeed;
            internal readonly CharacterFootPrimarySupportState PrimarySeed;
            readonly ulong trajectoryTick, trajectoryReset, trajectoryGeneration, trajectoryAuthority, trajectoryRevision;
            readonly bool hadTrajectoryAttempt;
            readonly float requestedDuration;

            internal ReleasingPredictionWorkspace(JObject fixture, CharacterFootPlacementProfile profile, ICharacterFootPlacementWorldQuery world)
            {
                JObject columns = (JObject)fixture["columns"];
                var captured = fixture["seed"];
                Row r = Row.Main(captured, columns);
                Assert.That(r.I("stride/result/state"), Is.EqualTo((int)CharacterFootStrideState.Releasing));
                Assert.That(r.B("stride/result/response/evaluated"), Is.True);
                Assert.That(r.B("stride/result/response/completed"), Is.False);
                Assert.That(r.B("primary-support/has-value"), Is.False);
                SpringSeed = new CharacterFootPelvisSpringState { HasValue = true, SupportSide = default, SupportLandingEventIdentity = 0,
                    Slope = CharacterFootStrideSlope.Flat, TargetAlongUp = r.F("stride/result/response/target"),
                    OutputAlongUp = r.F("stride/result/response/output"), VelocityAlongUp = r.F("stride/result/response/velocity"), HasGoalWorldPosition = true,
                    GoalWorldPosition = r.V("stride/result/animated-pelvis") + Vector3.up * (r.F("stride/result/response/output") * r.F("stride/result/response/position-weight")) };
                PrimarySeed = default;
                var currentSupport = profile.CurrentSupportQuery.Build();
                var landing = profile.LandingPrediction.Build();
                var ground = profile.GroundDetection.Build();
                var motion = profile.FootMotion.Build();
                var settings = new CharacterFootPlacementModuleSettings(profile.ProfileId, profile.Revision, r.S("input/pose-plan-hash"), in currentSupport, in landing, in ground, in motion);
                future = new RecordedFutureBodyPort(r.S("input/prediction-motion-source-identity"));
                functions = new CapturedFootPredictionFunctions(settings, world, future, new ActorId(r.S("frame/actor-id")));
                seedMotion = new CharacterFootPredictionMotionState { HasValue = r.B("input/prediction-motion-available"),
                    StableCurrentVelocity = new Vector2(r.F("input/prediction-stable-current-velocity-x"), r.F("input/prediction-stable-current-velocity-z")),
                    StableContinuationVelocity = new Vector2(r.F("input/prediction-stable-continuation-velocity-x"), r.F("input/prediction-stable-continuation-velocity-z")),
                    TimelineGeneration = r.U("input/timeline-generation"), BodyResetSequence = r.U("input/body-reset-sequence"),
                    PredictionSourceIdentity = new FixedString128Bytes(r.S("input/prediction-motion-source-identity")), Revision = r.U("input/prediction-motion-revision") };
                seedBody = new ReleasingPredictionInput(captured, columns);
                seedLeftObservation = RestoreObservation(captured["paired"], columns, profile.Revision, CharacterFootSide.Left);
                seedRightObservation = RestoreObservation(captured, columns, profile.Revision, CharacterFootSide.Right);
                seedLeftPath = PathInput(Row.Main(captured["paired"], columns), captured["paired"], columns, CharacterFootSide.Left).Page;
                seedRightPath = PathInput(r, captured, columns, CharacterFootSide.Right).Page;
                leftPathPool = new CharacterFootGroundPathPagePool(ground.ContactCapacity);
                rightPathPool = new CharacterFootGroundPathPagePool(ground.ContactCapacity);
                trajectoryTick = r.U("input/body-trajectory/body-tick"); trajectoryReset = r.U("input/body-trajectory/reset-sequence");
                trajectoryGeneration = r.U("input/body-trajectory/timeline-generation"); trajectoryAuthority = r.U("input/body-trajectory/authority-tick");
                trajectoryRevision = r.U("input/body-trajectory/prediction-motion-revision"); requestedDuration = r.F("input/body-trajectory/requested-duration");
                hadTrajectoryAttempt = r.B("input/body-trajectory/has-attempt");
            }

            internal void Reset()
            {
                bodyBank.PredictionMotion = seedMotion;
                bodyBank.BodyTrajectory.CopyFrom(seedBody.Forecast);
                bodyBank.BodyTrajectoryTick = trajectoryTick; bodyBank.BodyTrajectoryResetSequence = trajectoryReset;
                bodyBank.BodyTrajectoryGeneration = trajectoryGeneration; bodyBank.BodyTrajectoryAuthorityTick = trajectoryAuthority;
                bodyBank.BodyTrajectoryPredictionMotionRevision = trajectoryRevision; bodyBank.BodyTrajectoryRequestedDuration = requestedDuration;
                bodyBank.HasBodyTrajectoryAttempt = hadTrajectoryAttempt;
                leftObservation = seedLeftObservation; rightObservation = seedRightObservation;
                leftPath = seedLeftPath; rightPath = seedRightPath;
            }

            internal void Prepare(StoredGoalInput l, StoredGoalInput r, ref CharacterFootLifecycleContext left, ref CharacterFootLifecycleContext right, bool corrected)
            {
                AnimationFootMotionRuntimeSample leftStep = corrected ? l.CurrentStep : l.HistoricalStep;
                AnimationFootMotionRuntimeSample rightStep = corrected ? r.CurrentStep : r.HistoricalStep;
                future.Current = r.PredictionInput;
                r.PredictionInput.Requested = false;
                var trajectory = functions.ResolveBodyTrajectory(bodyBank, in leftStep, in rightStep, in r.PredictionInput.Timeline,
                    r.PredictionInput.SegmentRemaining, r.Delta, in r.PredictionInput.Body);
                var frame = new CapturedPredictionFrame { Body = r.PredictionInput.Body };
                PrepareSide(l, in leftStep, in left, trajectory, in frame, leftObservationPool, ref leftObservation, leftPathPool, ref leftPath);
                PrepareSide(r, in rightStep, in right, trajectory, in frame, rightObservationPool, ref rightObservation, rightPathPool, ref rightPath);
                l.BodyTrajectoryUsed = r.BodyTrajectoryUsed = trajectory != null;
                r.PredictionMotionRevision = bodyBank.PredictionMotionResult.Revision;
            }

            void PrepareSide(StoredGoalInput input, in AnimationFootMotionRuntimeSample step, in CharacterFootLifecycleContext context,
                CharacterFutureBodyTranslation trajectory, in CapturedPredictionFrame frame, CharacterFootLandingObservationPagePool observationPool,
                ref CharacterFootLandingObservationPage observation, CharacterFootGroundPathPagePool pathPool, ref CharacterFootGroundPathPage path)
            {
                var before = CharacterFootLandingRuntime.ProjectBeforePrediction(in context, in step);
                var request = new CharacterFootLockRequest(in step);
                bool verification = CharacterFootTransitionResolver.RequiresPlantVerification(in context, in request);
                var prediction = functions.PredictFootPair(input.Side, in step, in input.Animated, input.PredictionInput.Timeline.IsValid,
                    input.PredictionInput.Timeline.Generation, trajectory, in frame, in before, verification, observationPool, observation, out observation);
                input.Prediction = prediction.Selected;
                var after = CharacterFootLandingRuntime.ProjectAfterPrediction(in context, in step, in input.Prediction, in input.Settings);
                input.Path = functions.PrepareGroundPath(input.Side, after.HasLastLanding, in after.LastLanding, after.HasNextSwingLanding,
                    in after.NextSwingLanding, frame.Body.VisibleRotation * Vector3.up, input.PredictionInput.Timeline.AuthorityTick.Value, pathPool, path, out path);
            }

            static CharacterFootLandingObservationPage RestoreObservation(JToken captured, JObject columns, string revision, CharacterFootSide side)
            {
                Row r = Row.Main(captured, columns);
                Assert.That(r.I("foot/observation/cache-state"), Is.GreaterThan(0));
                var key = new CharacterFootLandingObservationKey(side, (CharacterFootPlacementQueryPurpose)r.I("foot/observation/query-purpose"),
                    r.U("foot/landing-event-identity"), r.U("foot/observation/source-sample-identity"), r.I("foot/observation/source-sample-cycle"),
                    r.V("foot/observation/canonical-raw-landing"), r.V("foot/observation/canonical-component-up"), revision, r.U("foot/observation/world-revision"),
                    r.V("foot/query/heel-offset"), r.V("foot/query/toe-offset"));
                Assert.That(key.Identity, Is.EqualTo(r.U("foot/observation/identity")));
                var query = new CharacterFootPlacementQueryRequest((CharacterFootPlacementQueryShape)r.I("foot/query/shape"), (CharacterFootPlacementQueryPurpose)r.I("foot/query/purpose"),
                    r.I("foot/query/foot-index"), r.V("foot/query/origin"), r.V("foot/query/direction"), r.F("foot/query/maximum-distance"), r.F("foot/query/radius"),
                    r.I("foot/query/layer-mask"), r.F("foot/query/minimum-ground-normal-dot"), r.V("foot/query/heel-offset"), r.V("foot/query/toe-offset"));
                var support = new CharacterFootLandingSupport(r.I("foot/surface-identity"), r.V("foot/landing-point"), r.V("foot/landing-normal"), r.F("foot/query-distance"));
                var coverage = new CharacterFootSupportQueryDiagnostics(r.I("foot/query-selection/coverage/search-hit-count"), r.I("foot/query-selection/coverage/support-hit-count"),
                    r.I("foot/query-selection/coverage/outside-support-ray-count"), r.I("foot/query-selection/coverage/steep-surface-count"));
                var selected = new CharacterFootLandingQueryCandidateDiagnostics(r.I("foot/query-selection/selected/surface-identity"), r.V("foot/query-selection/selected/point"), r.F("foot/query-selection/selected/distance"));
                var selection = new CharacterFootLandingQuerySelectionDiagnostics((CharacterFootLandingQueryCandidateSelectionState)r.I("foot/query-selection/state"), r.I("foot/query-selection/valid-candidate-count"), selected, coverage);
                var result = new CharacterFootLandingQueryResult(CharacterFootLandingQueryRejectReason.None, support, selection);
                var page = new CharacterFootLandingObservationPage(); page.Set(in key, in query, in result); return page;
            }
        }
    }
}
