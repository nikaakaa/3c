/*
目的：把同一混合业务的历史与候选 FootMotion 接入正式脚端生命周期和 Goal，复现并比较旋转跳变。
输入：双脚原采样、只在首帧恢复的完整脚前态、独立正式函数产出的候选 FootMotion；动画脚姿势保持同一原输入。
链路：正式 Stored/贡献选择输出 → LandingRuntime → Lifecycle → 实际脚底查询 → Complete → Goal。
边界：预测/地面路径和骨盆可达性是录制的边界输入，未重跑 PredictFootPair、Native Slot 或完整 FBBIK。
说明：docs/diagnostics/foot-placement/ik-tests/stored-foot-motion.html。此入口是 Unity 内函数实验，不能冒充正式 runner。
*/
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed partial class CharacterFootCapturedContactTests
    {
        public static JObject RunStoredGoalComparison(string inputPath, string sourceResultPath, string resultPath)
        {
            Assert.That(EditorApplication.isPlaying || EditorApplication.isCompiling, Is.False);
            const string scenePath = "Assets/Scenes/GameplayLab/GameplayLabFixed.unity";
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var report = new JObject
            {
                ["status"] = "running", ["stage"] = "prepare", ["utc"] = DateTime.UtcNow.ToString("o"),
                ["executionLayer"] = "Unity 内独立编译真实函数实验；不是正式 Test Runner",
                ["scope"] = "固定原混合动画脚姿势，预测/路径/骨盆可达性为录制边界输入；新 FootMotion 接入 Lifecycle 至实际 Goal 查询；未执行完整 FBBIK",
                ["editorCompilationFailed"] = EditorUtility.scriptCompilationFailed,
                ["fixtureSha256"] = Sha256(inputPath), ["sourceResultSha256"] = Sha256(sourceResultPath),
                ["runtimeCommit"] = Git("rev-parse HEAD")
            };
            GameObject root = null;
            try
            {
                JObject fixture = JObject.Parse(File.ReadAllText(inputPath, Encoding.UTF8));
                JObject sourceReport = JObject.Parse(File.ReadAllText(sourceResultPath, Encoding.UTF8));
                Assert.That((string)sourceReport["status"], Is.EqualTo("passed"));
                report["capture"] = fixture["capture"];
                var profile = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementProfile>(AssetRoot + "FootPlacement/CorinFootPlacementProfile.asset");
                var calibration = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementRigCalibration>(AssetRoot + "FootPlacement/CorinFootPlacementRigCalibration.asset");
                var definition = AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(AssetRoot + "Rig/CorinAnimationRigDefinition.asset");
                var worldBinding = Object.FindObjectsByType<CharacterWorldAwarePresentationBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(x => x.gameObject.scene == scene && x.SelfColliderRoot.name == "Gameplay Lab Fixed Player");
                var binding = Object.FindObjectsByType<CharacterAnimationRigBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Single(x => x.gameObject.scene == scene && x.Animator.transform.IsChildOf(worldBinding.PresentationRoot));
                var rig = new CharacterFootPlacementPoseRig(calibration, new CharacterAnimationRigPayload(definition), binding, worldBinding);
                CharacterFootSide side = (string)fixture["side"] == "left" ? CharacterFootSide.Left : CharacterFootSide.Right;
                var supportSettings = profile.CurrentSupportQuery.Build();
                var world = new CharacterFootPlacementWorldQueryBackend(scene.GetPhysicsScene(), rig, 16, 16, 16);
                var query = new CharacterFootSoleSupportQuery(world, in supportSettings);
                Physics.SyncTransforms();
                var columns = (JObject)fixture["columns"];
                var inputs = ((JArray)fixture["frames"]).Select((f, i) => new StoredGoalInput(f, columns,
                    sourceReport["rows"][i], profile, side == CharacterFootSide.Left ? rig.LeftLegLength : rig.RightLegLength, side)).ToArray();
                foreach (StoredGoalInput input in inputs)
                {
                    var contacts = input.Animated.ResolveSoleContacts(input.Animated.AnklePosition, input.Animated.AnkleRotation);
                    input.Support = query.Query(input.Sequence, input.Completion, world.WorldRevision, side,
                        Vector3.up, input.Grounded, in contacts, input.InputProbes);
                    Assert.That(input.InputProbes.Count, Is.EqualTo(23));
                    for (int i = 0; i < input.ExpectedProbes.Length; i++)
                    {
                        var hit = input.InputProbes[i].Result;
                        Assert.That(hit.Accepted, Is.EqualTo(input.ExpectedProbes[i].B("accepted")));
                        if (hit.Accepted) Assert.That(Vector3.Distance(hit.Point, input.ExpectedProbes[i].V("point")), Is.LessThan(.0002f));
                    }
                }
                CharacterFootLifecycleContext seed = CapturedPreState(fixture["frames"][0], columns);
                var targetProbes = new CharacterFootSoleProbeBuffer();
                var outputProbes = new CharacterFootSoleProbeBuffer();
                root = new GameObject("Stored foot Goal comparison") { hideFlags = HideFlags.HideAndDontSave };
                foreach (bool candidate in new[] { false, true })
                {
                    report["stage"] = candidate ? "current" : "historical";
                    var results = new StoredGoalOutput[inputs.Length];
                    RunStoredGoals(inputs, in seed, results, candidate, root.transform, in query, world.WorldRevision, targetProbes, outputProbes);
                    long start = GC.GetAllocatedBytesForCurrentThread();
                    RunStoredGoals(inputs, in seed, results, candidate, root.transform, in query, world.WorldRevision, targetProbes, outputProbes);
                    long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
                    var rows = new JArray();
                    report[candidate ? "current" : "historical"] = new JObject { ["frames"] = inputs.Length, ["allocatedBytes"] = allocated, ["rows"] = rows };
                    float maxError = 0f, maxRotationError = 0f, maxRotationStep = 0f, maxPenetration = 0f, maxExtraRotationStep = 0f;
                    int observedFinalFrames = 0;
                    Quaternion previousRotation = inputs[0].Animated.AnkleRotation;
                    for (int i = 0; i < results.Length; i++)
                    {
                        StoredGoalInput input = inputs[i];
                        StoredGoalOutput value = results[i];
                        Vector3 recordedSole = input.Recorded.V("foot/foot-motion/core/original-sole") + input.Recorded.V("foot/foot-motion/output-stages/final-effective-correction");
                        float error = Vector3.Distance(recordedSole, value.Output.Pose.EffectiveSole);
                        float rotationError = Quaternion.Angle(input.Recorded.Q("foot/resolved/core/effective-rotation"), value.Output.Pose.EffectiveRotation);
                        float rotationStep = i > 0 ? Quaternion.Angle(previousRotation, value.Output.Pose.EffectiveRotation) : 0f;
                        maxError = Mathf.Max(maxError, error);
                        maxRotationError = Mathf.Max(maxRotationError, rotationError);
                        maxRotationStep = Mathf.Max(maxRotationStep, rotationStep);
                        float originalRotationStep = i > 0 ? Quaternion.Angle(inputs[i - 1].Animated.AnkleRotation, input.Animated.AnkleRotation) : 0f;
                        maxExtraRotationStep = Mathf.Max(maxExtraRotationStep, rotationStep - originalRotationStep);
                        if (value.FinalSupport.Available)
                        {
                            observedFinalFrames++;
                            maxPenetration = Mathf.Max(maxPenetration, value.FinalSupport.RequiredDisplacement);
                        }
                        rows.Add(new JObject
                        {
                            ["frame"] = input.Sequence, ["dt"] = input.Delta, ["state"] = value.Motion.ConstraintState.ToString(),
                            ["contact"] = candidate ? input.CurrentStep.Contact : input.HistoricalStep.Contact,
                            ["responseSourceLineage"] = candidate ? input.CurrentLineage.ToString() : input.HistoricalLineage.ToString(),
                            ["authorWeight"] = input.Weight, ["positionWeight"] = value.Output.GoalTarget.PositionWeight,
                            ["rotationWeight"] = value.Output.GoalTarget.RotationWeight,
                            ["sole"] = Vec(value.Output.Pose.EffectiveSole), ["rotationStepDegrees"] = i > 0 ? new JValue(rotationStep) : JValue.CreateNull(),
                            ["rotationSpeedDegreesPerSecond"] = i > 0 ? new JValue(rotationStep / input.Delta) : JValue.CreateNull(),
                            ["originalRotationStepDegrees"] = i > 0 ? new JValue(originalRotationStep) : JValue.CreateNull(),
                            ["extraRotationStepDegrees"] = i > 0 ? new JValue(rotationStep - originalRotationStep) : JValue.CreateNull(),
                            ["rotation"] = new JArray(value.Output.Pose.EffectiveRotation.x, value.Output.Pose.EffectiveRotation.y, value.Output.Pose.EffectiveRotation.z, value.Output.Pose.EffectiveRotation.w),
                            ["recordedSoleError"] = error, ["recordedRotationErrorDegrees"] = rotationError,
                            ["finalSupportAvailable"] = value.FinalSupport.Available,
                            ["finalPenetration"] = value.FinalSupport.Available ? new JValue(value.FinalSupport.RequiredDisplacement) : JValue.CreateNull(),
                            ["finalAcceptedProbes"] = value.FinalSupport.AcceptedSampleCount,
                            ["hasContactAnchor"] = value.HasAnchor, ["contactAnchor"] = Vec(value.Anchor),
                            ["fixedHipReachRatio"] = Vector3.Distance(input.Animated.HipPosition, value.Output.Pose.EffectiveAnkle) / input.LegLength,
                            ["recordedLandingReachFeedback"] = input.RecordedLandingReach
                        });
                        Assert.That(value.Output.GoalTarget.PositionWeight, Is.EqualTo(input.Recorded.F("foot/resolved/core/position-weight")),
                            "原本无有效 Goal 的帧需保留零权重；有有效 Goal 的帧保留作者权重：" + input.Sequence);
                        Assert.That(value.GoalError, Is.LessThan(.0001f));
                        if (candidate && value.FinalSupport.Available)
                            Assert.That(value.FinalSupport.RequiredDisplacement, Is.LessThanOrEqualTo(.0002f), "有查询命中的候选最终脚掌不得新增正穿透");
                        if (candidate && value.Output.GoalTarget.PositionWeight > 0f)
                            Assert.That(value.FinalSupport.Available, Is.True, "实际输出 Goal 的候选帧必须取得最终脚掌净空证据");
                        previousRotation = value.Output.Pose.EffectiveRotation;
                    }
                    report[candidate ? "current" : "historical"] = new JObject
                    {
                        ["frames"] = inputs.Length, ["allocatedBytes"] = allocated, ["maximumRecordedSoleError"] = maxError,
                        ["maximumRecordedRotationErrorDegrees"] = maxRotationError, ["maximumRotationStepDegrees"] = maxRotationStep,
                        ["observedFinalFrames"] = observedFinalFrames,
                        ["maximumFinalPenetration"] = observedFinalFrames > 0 ? new JValue(maxPenetration) : JValue.CreateNull(),
                        ["maximumExtraRotationStepDegrees"] = maxExtraRotationStep, ["rows"] = rows
                    };
                    Assert.That(allocated, Is.Zero);
                    if (!candidate)
                    {
                        Assert.That(maxError, Is.LessThan(.0005f), "基线必须先重现原脚位");
                        Assert.That(maxRotationError, Is.LessThan(.1f), "基线必须先重现原有效旋转");
                    }
                }
                var historical = (JObject)report["historical"];
                var current = (JObject)report["current"];
                if (side == CharacterFootSide.Left)
                {
                    foreach (JObject row in current["rows"])
                        if ((ulong)row["frame"] >= 1178 && (ulong)row["frame"] <= 1180)
                        {
                            Assert.That((string)row["state"], Is.EqualTo("Swing"), "无接触 Stored 不得新增 Landing/锁脚");
                            Assert.That((bool)row["hasContactAnchor"], Is.False);
                        }
                    Assert.That((float)historical["maximumRotationStepDegrees"], Is.GreaterThan(130f), "历史基线须重现原错误旋转");
                    Assert.That((float)current["maximumRotationStepDegrees"], Is.LessThan((float)historical["maximumRotationStepDegrees"]), "候选完整窗口必须减少错误旋转，不能只移动异常帧");
                    Assert.That((float)current["maximumExtraRotationStepDegrees"], Is.LessThan((float)historical["maximumExtraRotationStepDegrees"]), "候选必须减少超出原动画的额外有效旋转");
                }
                else
                {
                    JObject beforeStored = (JObject)((JArray)current["rows"]).Single(x => (ulong)x["frame"] == 893);
                    var anchor = beforeStored["contactAnchor"];
                    foreach (JObject row in current["rows"])
                        if ((ulong)row["frame"] >= 894 && (ulong)row["frame"] <= 898)
                        {
                            Assert.That((bool)row["hasContactAnchor"], Is.True, "真实接触 Stored 必须保持接触锚点");
                            var position = row["contactAnchor"];
                            Assert.That(Vector3.Distance(new Vector3((float)anchor[0], (float)anchor[1], (float)anchor[2]),
                                new Vector3((float)position[0], (float)position[1], (float)position[2])), Is.LessThanOrEqualTo(.0002f), "Stored 不得因虚假换源移动真实锚点");
                        }
                    JObject released = (JObject)((JArray)current["rows"]).Single(x => (ulong)x["frame"] == 954);
                    Assert.That((string)released["state"], Is.Not.EqualTo("Locked"), "Live 接管后按实际输入释放");
                }
                report["status"] = "passed";
                report["stage"] = "complete";
            }
            catch (Exception error)
            {
                report["status"] = "failed";
                report["failure"] = error.ToString();
                throw;
            }
            finally
            {
                File.WriteAllText(resultPath, report.ToString(Formatting.None), new UTF8Encoding(false));
                Object.DestroyImmediate(root);
                SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
            return report;
        }

        struct StoredGoalOutput
        {
            internal CharacterResolvedFootResult Output;
            internal CharacterFootSwingMotionResult Motion;
            internal CharacterFootCurrentSupportObservation FinalSupport;
            internal float GoalError;
            internal bool HasAnchor;
            internal Vector3 Anchor;
        }

        static void RunStoredGoals(StoredGoalInput[] inputs, in CharacterFootLifecycleContext seed,
            StoredGoalOutput[] results, bool candidate, Transform root, in CharacterFootSoleSupportQuery query,
            ulong revision, CharacterFootSoleProbeBuffer targetProbes, CharacterFootSoleProbeBuffer outputProbes)
        {
            CharacterFootLifecycleContext context = seed;
            for (int i = 0; i < inputs.Length; i++)
            {
                StoredGoalInput input = inputs[i];
                root.SetPositionAndRotation(input.RootPosition, input.RootRotation);
                AnimationFootMotionRuntimeSample step = candidate ? input.CurrentStep : input.HistoricalStep;
                var snapshot = CharacterFootLandingRuntime.ProjectAfterPrediction(in context, in step, in input.Prediction, in input.Settings);
                var swing = CharacterFootSwingMotionBuilder.Build(in input.Animated, in step, input.Weight,
                    Vector3.up, in input.Path, true, step.FootHeight, snapshot.NextSwingPredictionError);
                var request = new CharacterFootLockRequest(in step);
                bool prepared = snapshot.PlantTargetState == CharacterFootPlantTargetState.Tracking;
                CharacterFootGroundPathLanding contactLanding = default;
                bool hasContact = request.RequestsLock && (snapshot.TryResolveVerifiedLanding(request.EventIdentity, out contactLanding) ||
                    CharacterFootLandingRuntime.TryResolveCurrentContactCandidate(in step, in input.Prediction, out contactLanding));
                var frame = new CharacterFootStateFrame(input.Sequence, input.Completion, input.RigId, input.RigRevision, input.Side,
                    in input.Animated, input.Animated.HipPosition, input.LegLength, in swing, in input.Path,
                    hasContact, in contactLanding, prepared, prepared ? snapshot.PlantTarget : default,
                    in input.Support, in request, step.Support, request.EventIdentity, CharacterFootGoalOwnershipLossReason.None,
                    input.Weight, Vector3.up, input.Delta, candidate ? input.CurrentLineage : input.HistoricalLineage,
                    input.ProfileRevision, revision, in input.Settings);
                var evaluation = new CharacterFootStateEvaluation(input.Side, in step, in input.Prediction, in frame,
                    default, input.Grounded, root, in query, targetProbes, outputProbes);
                CharacterFootLifecycle.Evaluate(ref context, in evaluation, out var receipt);
                CharacterResolvedFootResult output = receipt.Complete(ref context, input.RecordedLandingReach, out var motion);
                Vector3 goal = root.TransformPoint(output.GoalTarget.ComponentPosition);
                Quaternion rotation = root.rotation * output.GoalTarget.ComponentRotation;
                var finalContacts = input.Animated.ResolveSoleContacts(goal,
                    Quaternion.Slerp(input.Animated.AnkleRotation, rotation, output.GoalTarget.RotationWeight));
                var final = query.Query(input.Sequence, input.Completion, revision, input.Side, Vector3.up,
                    input.Grounded, in finalContacts, input.FinalProbes);
                results[i] = new StoredGoalOutput { Output = output, Motion = motion, FinalSupport = final,
                    GoalError = Vector3.Distance(goal, output.Pose.EffectiveAnkle), HasAnchor = context.Contact.HasContact, Anchor = context.Contact.Anchor };
            }
        }

        sealed class StoredGoalInput
        {
            internal StoredGoalInput(JToken captured, JObject columns, JToken sourceResult, CharacterFootPlacementProfile profile, float legLength, CharacterFootSide side)
            {
                Recorded = Row.Main(captured, columns);
                Row r = Recorded;
                Side = side;
                ExpectedProbes = Row.Table(captured, columns, "probes").OrderBy(x => x.I("sample-index")).ToArray();
                Animated = AnimatedPose(r, ExpectedProbes);
                HistoricalStep = StoredRecordedStep(r, side);
                CurrentStep = StoredProducedStep(sourceResult["currentSample"], side);
                RootPosition = r.V("physical-body/pose-root-world-position");
                RootRotation = r.Q("physical-body/pose-root-world-rotation");
                Path = PathInput(r, captured, columns, side);
                Prediction = StoredRecordedPrediction(r, side);
                Settings = profile.FootMotion.Build();
                Sequence = r.U("foot/resolved/core/frame-sequence");
                Completion = r.U("foot/resolved/core/completion-identity");
                Grounded = r.B("input/grounded");
                Weight = r.F("foot/foot-motion/lifecycle/formal-foot-placement-weight");
                Delta = r.F("input/presentation-delta-seconds");
                LegLength = legLength;
                RigId = new FixedString64Bytes(r.S("foot/resolved/core/rig-id"));
                RigRevision = new FixedString64Bytes(r.S("foot/resolved/core/rig-revision"));
                ProfileRevision = new FixedString128Bytes(profile.Revision);
                HistoricalLineage = new FixedString128Bytes(r.S("input/pose-plan-hash"));
                CurrentLineage = HistoricalLineage;
                RecordedLandingReach = r.B("foot/foot-motion/core/landing-reach-available");
            }
            internal readonly Row Recorded;
            internal readonly Row[] ExpectedProbes;
            internal readonly CharacterFootSide Side;
            internal readonly CharacterFootPlacementAnimatedFootPose Animated;
            internal readonly AnimationFootMotionRuntimeSample HistoricalStep, CurrentStep;
            internal readonly CharacterFootGroundPathResult Path;
            internal readonly CharacterFootLandingPredictionResult Prediction;
            internal readonly CharacterFootMotionSettings Settings;
            internal readonly Vector3 RootPosition;
            internal readonly Quaternion RootRotation;
            internal readonly ulong Sequence, Completion;
            internal readonly bool Grounded, RecordedLandingReach;
            internal readonly float Weight, Delta, LegLength;
            internal readonly FixedString64Bytes RigId, RigRevision;
            internal readonly FixedString128Bytes ProfileRevision, HistoricalLineage, CurrentLineage;
            internal readonly CharacterFootSoleProbeBuffer InputProbes = new CharacterFootSoleProbeBuffer();
            internal readonly CharacterFootSoleProbeBuffer FinalProbes = new CharacterFootSoleProbeBuffer();
            internal CharacterFootCurrentSupportObservation Support;
        }

        static AnimationFootMotionRuntimeSample StoredRecordedStep(Row r, CharacterFootSide side)
        {
            var events = new AnimationFootMotionEventFrame(StoredRecordedEvent(r, "current-contact", side), StoredRecordedEvent(r, "next-landing", side),
                (AnimationFootMotionEventPhase)r.I("formal-input/events/phase"), r.F("formal-input/events/time-to-landing-seconds"),
                r.F("formal-input/events/swing-progress"), r.F("formal-input/events/approach-contact-to-landing-progress"));
            return new AnimationFootMotionRuntimeSample(r.F("formal-input/foot-height"), r.F("formal-input/toe-height"), r.F("formal-input/toe-speed"),
                r.F("formal-input/position-error"), r.F("formal-input/rotation-error"), r.F("formal-input/contact"),
                (AnimationFootStepObservationLockMode)r.I("formal-input/lock-mode"), r.F("formal-input/lock-weight"), r.F("formal-input/support"), in events);
        }

        static AnimationFootMotionEventOccurrence StoredRecordedEvent(Row r, string name, CharacterFootSide side)
        {
            string p = "formal-input/events/" + name + "/";
            if (!r.B(p + "is-valid")) return default;
            return new AnimationFootMotionEventOccurrence(r.I(p + "ordinal"), r.I(p + "landing-cycle"), r.F(p + "normalized-time"),
                r.F(p + "distance"), r.V(p + "root-local-landing")).Bind(r.U(p + "source-sample-identity"), r.U(p + "contribution-continuity-identity"), side);
        }

        static AnimationFootMotionRuntimeSample StoredProducedStep(JToken value, CharacterFootSide side)
        {
            var events = new AnimationFootMotionEventFrame(StoredProducedEvent(value["current"], side), StoredProducedEvent(value["next"], side),
                (AnimationFootMotionEventPhase)(int)value["phase"], (float)value["timeToLanding"], (float)value["swingProgress"], (float)value["approachProgress"]);
            return new AnimationFootMotionRuntimeSample((float)value["footHeight"], (float)value["toeHeight"], (float)value["toeSpeed"],
                (float)value["positionError"], (float)value["rotationError"], (float)value["contact"],
                (AnimationFootStepObservationLockMode)(int)value["lockMode"], (float)value["lockWeight"], (float)value["support"], in events);
        }

        static AnimationFootMotionEventOccurrence StoredProducedEvent(JToken value, CharacterFootSide side)
        {
            if (value.Type == JTokenType.Null) return default;
            var point = value["point"];
            var result = new AnimationFootMotionEventOccurrence((int)value["ordinal"], (int)value["cycle"], (float)value["normalizedTime"],
                (float)value["distance"], new Vector3((float)point[0], (float)point[1], (float)point[2]))
                .Bind(ulong.Parse((string)value["sourceSampleIdentity"]), ulong.Parse((string)value["continuity"]), side);
            Assert.That(result.Identity.ToString(), Is.EqualTo((string)value["identity"]));
            return result;
        }

        static CharacterFootLandingPredictionResult StoredRecordedPrediction(Row r, CharacterFootSide side)
        {
            if (!r.B("foot/accepted"))
                return new CharacterFootLandingPredictionResult(side, (CharacterFootLandingPredictionState)r.I("foot/state"),
                    (CharacterFootLandingPredictionRejectReason)r.I("foot/reject-reason"), (CharacterFootLandingStepSource)r.I("foot/step-source"),
                    r.U("foot/landing-event-identity"), r.U("foot/trajectory-generation"), 0f, r.F("foot/time-to-landing-seconds"), default, false,
                    r.S("input/prediction-motion-source-identity"), default, r.V("foot/current-animated-sole"), default, default, default, default, default);
            bool futureAvailable = r.B("foot/future-body-translation-available");
            CharacterFutureBodyTranslationSample future = default;
            if (futureAvailable)
            {
                Vector3 translation = r.V("foot/future-body-relative-translation");
                Vector3 velocity = r.V("foot/future-body-translation-velocity");
                future = new CharacterFutureBodyTranslationSample(r.F("foot/time-to-landing-seconds"),
                    translation.x, translation.y, translation.z, velocity.x, velocity.y, velocity.z);
            }
            var support = new CharacterFootLandingSupport(r.I("foot/surface-identity"), r.V("foot/landing-point"),
                r.V("foot/landing-normal"), r.F("foot/query-distance"));
            return new CharacterFootLandingPredictionResult(side, (CharacterFootLandingPredictionState)r.I("foot/state"),
                (CharacterFootLandingPredictionRejectReason)r.I("foot/reject-reason"), (CharacterFootLandingStepSource)r.I("foot/step-source"),
                r.U("foot/landing-event-identity"), r.U("foot/trajectory-generation"), r.F("foot/landing-confidence"),
                r.F("foot/time-to-landing-seconds"), r.V("foot/root-local-landing"), futureAvailable,
                r.S("input/prediction-motion-source-identity"), in future, r.V("foot/current-animated-sole"),
                r.V("foot/raw-landing-candidate"), default, default, in support, default);
        }

        static CharacterFootLifecycleContext CapturedPreState(JToken captured, JObject columns)
        {
            Row r = Row.Main(captured, columns);
            const string prefix = "foot/pre-state/";
            return new CharacterFootLifecycleContext
            {
                PreviousOutputWeight = r.F(prefix + "previous-output-weight"),
                PreviousAnimatedSole = r.V(prefix + "previous-animated-sole"),
                Landing = new CharacterFootLandingContext
                {
                    LastLanding = CapturedLandingFact(r, "foot/pre-state/landing/last-landing"),
                    NextSwingLanding = CapturedLandingFact(r, "foot/pre-state/landing/next-swing-landing"),
                    PromotedLanding = CapturedLandingFact(r, "foot/pre-state/landing/promoted-landing"),
                    PlantTarget = CapturedLandingFact(r, "foot/pre-state/landing/plant-target"),
                    NextSwingReferencePoint = r.V("foot/pre-state/landing/next-swing-reference-point"),
                    NextSwingPredictionError = r.F("foot/pre-state/landing/next-swing-prediction-error"),
                    TrackedEventIdentity = r.U("foot/pre-state/landing/tracked-event-identity"),
                    NextTrackingState = (CharacterFootNextLandingTrackingState)r.I("foot/pre-state/landing/next-tracking-state"),
                    PlantTargetState = (CharacterFootPlantTargetState)r.I("foot/pre-state/landing/plant-target-state"),
                    PlantTargetUpdated = r.B("foot/pre-state/landing/plant-target-updated"),
                    PlantVerificationAttempted = r.B("foot/pre-state/landing/plant-verification-attempted"),
                    PlantVerificationUnavailable = r.B("foot/pre-state/landing/plant-verification-unavailable"),
                },
                Discrete = new CharacterFootDiscreteStateContext
                {
                    State = (CharacterFootConstraintState)r.I("foot/pre-state/discrete/state"),
                    LockResponse = (CharacterFootLockResponse)r.I("foot/pre-state/discrete/lock-response"),
                },
                Contact = new CharacterFootContactContext
                {
                    HasContact = r.B("foot/pre-state/contact/has-contact"),
                    EventIdentity = r.U("foot/pre-state/contact/event-identity"),
                    AcquiredFrameSequence = r.U("foot/pre-state/contact/acquired-frame-sequence"),
                    AcquiredCompletionIdentity = r.U("foot/pre-state/contact/acquired-completion-identity"),
                    WorldRevision = r.U("foot/pre-state/contact/world-revision"),
                    SurfaceIdentity = r.I("foot/pre-state/contact/surface-identity"),
                    Anchor = r.V("foot/pre-state/contact/anchor"),
                    Normal = r.V("foot/pre-state/contact/normal"),
                },
                ContactTransition = new CharacterFootContactTransitionContext
                {
                    HasPreviousRequest = r.B("foot/pre-state/contact-transition/has-previous-request"),
                    PreviousRequestedLock = r.B("foot/pre-state/contact-transition/previous-requested-lock"),
                    PreviousIsInZone = r.B("foot/pre-state/contact-transition/previous-is-in-zone"),
                    PreviousIsSliding = r.B("foot/pre-state/contact-transition/previous-is-sliding"),
                    PreviousEventIdentity = r.U("foot/pre-state/contact-transition/previous-event-identity"),
                    PreviousMode = (AnimationFootStepObservationLockMode)r.I("foot/pre-state/contact-transition/previous-mode"),
                    PreviousWeight = r.F("foot/pre-state/contact-transition/previous-weight"),
                    SecondsSinceEdge = r.F("foot/pre-state/contact-transition/seconds-since-edge"),
                    IsMoving = r.B("foot/pre-state/contact-transition/is-moving"),
                    IsLocking = r.B("foot/pre-state/contact-transition/is-locking"),
                    EnterGroundedZone = r.B("foot/pre-state/contact-transition/enter-grounded-zone"),
                    LeaveGroundedZone = r.B("foot/pre-state/contact-transition/leave-grounded-zone"),
                    IsBreakToGround = r.B("foot/pre-state/contact-transition/is-break-to-ground"),
                    EnterLockZone = r.B("foot/pre-state/contact-transition/enter-lock-zone"),
                    LeaveLockZone = r.B("foot/pre-state/contact-transition/leave-lock-zone"),
                    CurrentContactEdge = (CharacterFootContactEdge)r.I("foot/pre-state/contact-transition/current-contact-edge"),
                    CurrentEventIdentity = r.U("foot/pre-state/contact-transition/current-event-identity"),
                    Time = r.F("foot/pre-state/contact-transition/time"),
                    RemainTime = r.F("foot/pre-state/contact-transition/remain-time"),
                    LatestContactEventIdentity = r.U("foot/pre-state/contact-transition/latest-contact-event-identity"),
                    LatestReleasedContactEventIdentity = r.U("foot/pre-state/contact-transition/latest-released-contact-event-identity"),
                    CompletedLockWeightEventIdentity = r.U("foot/pre-state/contact-transition/completed-lock-weight-event-identity"),
                },
                Interpolation = new CharacterFootInterpolationState
                {
                    OutputWeightRebased = r.B("foot/pre-state/interpolation/output-weight-rebased"),
                    HasOutput = r.B("foot/pre-state/interpolation/has-output"),
                    HasSwingPath = r.B("foot/pre-state/interpolation/has-swing-path"),
                    SwingLandingEventIdentity = r.U("foot/pre-state/interpolation/swing-landing-event-identity"),
                    SwingGroundPathInputIdentity = r.U("foot/pre-state/interpolation/swing-ground-path-input-identity"),
                    SwingLandingPoint = r.V("foot/pre-state/interpolation/swing-landing-point"),
                    PreviousTargetCorrection = r.V("foot/pre-state/interpolation/previous-target-correction"),
                    PreviousSwingTargetCorrection = r.V("foot/pre-state/interpolation/previous-swing-target-correction"),
                    EffectiveCorrection = r.V("foot/pre-state/interpolation/effective-correction"),
                    SwingResidual = r.V("foot/pre-state/interpolation/swing-residual"),
                    HasTargetHeight = r.B("foot/pre-state/interpolation/has-target-height"),
                    TargetHeightEventIdentity = r.U("foot/pre-state/interpolation/target-height-event-identity"),
                    FilteredTargetHeightAlongUp = r.F("foot/pre-state/interpolation/filtered-target-height-along-up"),
                    TargetHeightRetargetActive = r.B("foot/pre-state/interpolation/target-height-retarget-active"),
                    Residual = r.V("foot/pre-state/interpolation/residual"),
                    Progress = r.F("foot/pre-state/interpolation/progress"),
                    StartResidual = r.F("foot/pre-state/interpolation/start-residual"),
                    Completed = r.B("foot/pre-state/interpolation/completed"),
                    Policy = (CharacterFootInterpolationPolicy)r.I("foot/pre-state/interpolation/policy"),
                    HasPlantTarget = r.B("foot/pre-state/interpolation/has-plant-target"),
                    PlantTargetEventIdentity = r.U("foot/pre-state/interpolation/plant-target-event-identity"),
                    PlantTargetKind = (CharacterFootPlantTargetKind)r.I("foot/pre-state/interpolation/plant-target-kind"),
                    PlantLockResponse = (CharacterFootLockResponse)r.I("foot/pre-state/interpolation/plant-lock-response"),
                    PlantTargetVerified = r.B("foot/pre-state/interpolation/plant-target-verified"),
                    PlantDirectFollow = r.B("foot/pre-state/interpolation/plant-direct-follow"),
                    PlantDesiredPoint = r.V("foot/pre-state/interpolation/plant-desired-point"),
                    PlantFilteredPoint = r.V("foot/pre-state/interpolation/plant-filtered-point"),
                    PreviousPlantSelectedWorldTarget = r.V("foot/pre-state/interpolation/previous-plant-selected-world-target"),
                    SelectedSupportTarget = CapturedSupportTarget(r, "foot/pre-state/interpolation/selected-support-target"),
                    HasPreviousResponseOutputPoint = r.B("foot/pre-state/interpolation/has-previous-response-output-point"),
                    PreviousResponseOutputPoint = r.V("foot/pre-state/interpolation/previous-response-output-point"),
                    PendingReleaseResponseRebase = r.B("foot/pre-state/interpolation/pending-release-response-rebase"),
                    PlantWorldResidual = r.V("foot/pre-state/interpolation/plant-world-residual"),
                    PlantWorldResidualTransitionActive = r.B("foot/pre-state/interpolation/plant-world-residual-transition-active"),
                    ResponseHistory = CapturedResponseHistory(r),
                    HasCorrectionResponseLineage = r.B("foot/pre-state/interpolation/has-correction-response-lineage"),
                    CorrectionResponseSourceLineage = CapturedResponseLineage(captured, columns, r, "source"),
                    CorrectionResponseProfileRevision = CapturedResponseLineage(captured, columns, r, "profile"),
                    CorrectionResponseWorldRevision = r.U("foot/pre-state/interpolation/correction-response-world-revision"),
                    PendingCorrectionResponseInitializationReason = (CharacterFootCorrectionResponseInitializationReason)r.I("foot/pre-state/interpolation/pending-correction-response-initialization-reason"),
                },
            };
        }

        static CharacterFootLandingFact CapturedLandingFact(Row r, string prefix)
        {
            if (!r.B(prefix + "/has-value")) return default;
            var landing = new CharacterFootGroundPathLanding(r.U(prefix + "/landing-event-identity"),
                r.U(prefix + "/trajectory-generation"), r.S(prefix + "/future-body-translation-source-identity"),
                r.I(prefix + "/surface-identity"), r.V(prefix + "/world-point"), r.V(prefix + "/world-normal"));
            return Fact(in landing);
        }

        static CharacterFootCorrectionResponseHistory CapturedResponseHistory(Row r)
        {
            const string prefix = "foot/pre-state/interpolation/response-history/";
            return r.B(prefix + "has-value")
                ? new CharacterFootCorrectionResponseHistory(r.F(prefix + "scalar"),
                    (CharacterFootCorrectionResponseDomain)r.I(prefix + "domain"), r.V(prefix + "applied-direction"))
                : default;
        }

        static FixedString128Bytes CapturedResponseLineage(JToken captured, JObject columns, Row r, string domain)
        {
            string countKey = domain == "source" ? "source-lineage-byte-count" : "profile-revision-byte-count";
            var bytes = new byte[r.I("foot/pre-state/interpolation/" + countKey)];
            foreach (Row value in Row.Table(captured, columns, "responseLineage"))
            {
                int index = value.I("byte-index");
                if (index < bytes.Length) bytes[index] = (byte)value.I(domain + "-utf8-byte");
            }
            return new FixedString128Bytes(Encoding.UTF8.GetString(bytes));
        }

        static CharacterFootSupportTarget CapturedSupportTarget(Row r, string prefix)
        {
            if (!r.B(prefix + "/available")) return default;
            return new CharacterFootSupportTarget(r.U(prefix + "/frame-sequence"), r.U(prefix + "/completion-identity"),
                (CharacterFootSide)r.I(prefix + "/side"), r.V(prefix + "/position"), r.V(prefix + "/support-normal"),
                r.I(prefix + "/surface-identity"), r.U(prefix + "/world-revision"),
                (CharacterFootSupportTargetKind)r.I(prefix + "/kind"),
                (CharacterFootSupportPositionSource)r.I(prefix + "/position-source"),
                r.U(prefix + "/position-frame-sequence"), r.U(prefix + "/position-completion-identity"),
                r.U(prefix + "/position-event-identity"), r.U(prefix + "/position-path-identity"),
                (CharacterFootSupportNormalSource)r.I(prefix + "/normal-source"),
                r.U(prefix + "/normal-frame-sequence"), r.U(prefix + "/normal-completion-identity"),
                r.U(prefix + "/normal-event-identity"));
        }

    }
}
