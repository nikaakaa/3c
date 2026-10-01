/*
目的：把同一混合业务的历史与候选 FootMotion 接入正式脚端生命周期和 Goal，复现并比较旋转跳变。
输入：双脚原采样、只在首帧恢复的完整脚前态、独立正式函数产出的候选 FootMotion；动画脚姿势保持同一原输入。
链路：正式 Stored/贡献选择输出 → LandingRuntime → Lifecycle → 实际脚底查询 → Complete → Goal。
边界：预测/地面路径和骨盆可达性是录制的边界输入，未重跑 PredictFootPair、Native Slot 或完整 FBBIK。
说明：docs/diagnostics/foot-placement/ik-tests/stored-foot-motion.html。此入口是 Unity 内函数实验，不能冒充正式 runner。
P1_FIXTURE使用同一2023种子和2024～2056双脚业务，实际重算预测、路径及骨盆；P1_CANDIDATE把当帧加权骨盆偏移交给Complete。
最终Goal实际查询保存heel/toe与整脚净空，完整记录后断言；说明与阻塞状态见releasing-action-native.html。
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
#if P1_FIXTURE
        static ulong s_P1Frame;
#endif
        public static JObject RunStoredGoalComparison(string inputPath, string sourceResultPath, string resultPath, string footVariant)
        {
            Assert.That(EditorApplication.isPlaying || EditorApplication.isCompiling, Is.False);
            bool boundary = footVariant == "boundary-current" || footVariant == "p1-current";
            bool releasing = footVariant == "releasing" || boundary;
            bool sourceComparison = footVariant == "sources" || footVariant == "releasing";
            bool bilateral = footVariant.StartsWith("pelvis-", StringComparison.Ordinal) || releasing;
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
            report["footVariant"] = footVariant;
            report["fixedHipScope"] = "使用录制骨盆之后的原Hip作固定条件指标；录制landingReach反馈保持不变，未重新计算骨盆或腿IK";
            if (bilateral)
            {
                report["scope"] = "固定原混合动画脚姿势、预测和地面路径；双脚正式Evaluate→PrimarySupport→Intent→PreparePelvis→ResolvePelvis→Complete→实际加权Goal查询；未执行完整FBBIK";
                report["fixedHipScope"] = "从录制骨盆后Hip减原骨盆加权偏移恢复计算前Hip，再加实际重新计算的骨盆偏移；未求解FBBIK";
            }
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
                    sourceReport["rows"][i], profile, side == CharacterFootSide.Left ? rig.LeftLegLength : rig.RightLegLength, side,
                    bilateral ? "currentRightSample" : "currentSample", bilateral)).ToArray();
                StoredGoalInput[] pairedInputs = bilateral ? ((JArray)fixture["frames"]).Select((f, i) => new StoredGoalInput(f["paired"], columns,
                    sourceReport["rows"][i], profile, rig.LeftLegLength, CharacterFootSide.Left, "currentLeftSample", true)).ToArray() : Array.Empty<StoredGoalInput>();
                foreach (StoredGoalInput input in inputs.Concat(pairedInputs))
                {
                    var contacts = input.Animated.ResolveSoleContacts(input.Animated.AnklePosition, input.Animated.AnkleRotation);
                    input.Support = query.Query(input.Sequence, input.Completion, world.WorldRevision, input.Side,
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
                CharacterFootLifecycleContext pairedSeed = bilateral ? CapturedPreState(fixture["frames"][0]["paired"], columns) : default;
                bool liveSwitch = (ulong)fixture["firstFrame"] == 2192;
                if (liveSwitch) Assert.That(inputs[0].Weight, Is.Zero, "真实作者关闭帧必须正式清空新旧历史，不能伪造候选旋转前态");
                var targetProbes = new CharacterFootSoleProbeBuffer();
                var outputProbes = new CharacterFootSoleProbeBuffer();
                root = new GameObject("Stored foot Goal comparison") { hideFlags = HideFlags.HideAndDontSave };
#if RELEASING_FIXTURE
                var predictionWorkspace = new ReleasingPredictionWorkspace(fixture, profile, world);
                report["scope"] = "已校准Native来源输出；真实前帧spring/primary/观测缓存种子 → 正式BodyTrajectory/PredictFootPair/实际查询/PrepareGroundPath → 双脚Lifecycle/Pelvis/Complete/Goal；KCC未来预测端口为录制边界，未执行FBBIK";
#endif
                foreach (bool candidate in sourceComparison ? new[] { false, true } : new[] { footVariant.EndsWith("current", StringComparison.Ordinal) })
                {
                    bool correctedSource = !sourceComparison || candidate;
                    report["stage"] = candidate ? "current" : "historical";
                    var results = new StoredGoalOutput[inputs.Length];
                    var pairedResults = new StoredGoalOutput[pairedInputs.Length];
                    var pelvisResults = new CharacterFootStrideHipsResult[bilateral ? inputs.Length : 0];
                    var pelvisGoals = new CharacterFullBodyIkGoal[bilateral ? inputs.Length : 0];
                    var pairedTargetProbes = new CharacterFootSoleProbeBuffer();
                    var pairedOutputProbes = new CharacterFootSoleProbeBuffer();
                    if (bilateral) RunBilateralGoals(inputs, pairedInputs, in seed, in pairedSeed, results, pairedResults, pelvisResults, pelvisGoals,
                        root.transform, in query, world.WorldRevision, targetProbes, outputProbes, pairedTargetProbes, pairedOutputProbes, correctedSource
#if RELEASING_FIXTURE
                        , predictionWorkspace
#endif
                        );
#if !P1_FIXTURE
                    else RunStoredGoals(inputs, in seed, results, correctedSource, root.transform, in query, world.WorldRevision, targetProbes, outputProbes);
#endif
                    long start = GC.GetAllocatedBytesForCurrentThread();
                    if (bilateral) RunBilateralGoals(inputs, pairedInputs, in seed, in pairedSeed, results, pairedResults, pelvisResults, pelvisGoals,
                        root.transform, in query, world.WorldRevision, targetProbes, outputProbes, pairedTargetProbes, pairedOutputProbes, correctedSource
#if RELEASING_FIXTURE
                        , predictionWorkspace
#endif
                        );
#if !P1_FIXTURE
                    else RunStoredGoals(inputs, in seed, results, correctedSource, root.transform, in query, world.WorldRevision, targetProbes, outputProbes);
#endif
                    long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
                    var rows = new JArray();
                    report[candidate ? "current" : "historical"] = new JObject { ["frames"] = inputs.Length, ["allocatedBytes"] = allocated, ["rows"] = rows };
                    float maxError = 0f, maxRotationError = 0f, maxRotationStep = 0f, maxPenetration = 0f, maxExtraRotationStep = 0f;
                    float maxPositionStep = 0f, maxHorizontalStep = 0f, maxExtraPositionStep = 0f, maxReach = 0f, overreachDuration = 0f;
                    float maxRelativeCorrectionStep = 0f;
                    int overreachFrames = 0;
                    int observedFinalFrames = 0;
                    Quaternion previousRotation = inputs[0].Animated.AnkleRotation;
                    Quaternion previousRelativeCorrection = Quaternion.identity;
                    for (int i = 0; i < results.Length; i++)
                    {
                        StoredGoalInput input = inputs[i];
                        StoredGoalOutput value = results[i];
                        Vector3 recordedSole = input.Recorded.V("foot/resolved/core/effective-sole");
                        float error = Vector3.Distance(recordedSole, value.Output.Pose.EffectiveSole);
                        float rotationError = Quaternion.Angle(input.Recorded.Q("foot/resolved/core/effective-rotation"), value.Output.Pose.EffectiveRotation);
                        float rotationStep = i > 0 ? Quaternion.Angle(previousRotation, value.Output.Pose.EffectiveRotation) : 0f;
                        maxError = Mathf.Max(maxError, error);
                        maxRotationError = Mathf.Max(maxRotationError, rotationError);
                        maxRotationStep = Mathf.Max(maxRotationStep, rotationStep);
                        float originalRotationStep = i > 0 ? Quaternion.Angle(inputs[i - 1].Animated.AnkleRotation, input.Animated.AnkleRotation) : 0f;
                        Quaternion relativeCorrection = (value.Output.Pose.EffectiveRotation * Quaternion.Inverse(input.Animated.AnkleRotation)).normalized;
                        float relativeCorrectionStep = i > 0 ? Quaternion.Angle(previousRelativeCorrection, relativeCorrection) : 0f;
                        maxRelativeCorrectionStep = Mathf.Max(maxRelativeCorrectionStep, relativeCorrectionStep);
                        maxExtraRotationStep = Mathf.Max(maxExtraRotationStep, rotationStep - originalRotationStep);
                        float positionStep = i > 0 ? Vector3.Distance(results[i - 1].Output.Pose.EffectiveSole, value.Output.Pose.EffectiveSole) : 0f;
                        float horizontalStep = i > 0 ? Vector3.ProjectOnPlane(value.Output.Pose.EffectiveSole - results[i - 1].Output.Pose.EffectiveSole, Vector3.up).magnitude : 0f;
                        float originalPositionStep = i > 0 ? Vector3.Distance(
                            CharacterFootConstraintMath.ResolveOriginalSole(inputs[i - 1].Animated),
                            CharacterFootConstraintMath.ResolveOriginalSole(input.Animated)) : 0f;
                        Vector3 hip = input.Animated.HipPosition;
                        if (bilateral) hip += pelvisResults[i].PelvisDelta * pelvisResults[i].PositionWeight;
                        float reach = Vector3.Distance(hip, value.Output.Pose.EffectiveAnkle) / input.LegLength;
                        maxPositionStep = Mathf.Max(maxPositionStep, positionStep);
                        maxHorizontalStep = Mathf.Max(maxHorizontalStep, horizontalStep);
                        maxExtraPositionStep = Mathf.Max(maxExtraPositionStep, positionStep - originalPositionStep);
                        maxReach = Mathf.Max(maxReach, reach);
                        if (reach > 1f) { overreachFrames++; overreachDuration += input.Delta; }
                        if (value.FinalSupport.Available)
                        {
                            observedFinalFrames++;
                            maxPenetration = Mathf.Max(maxPenetration, value.FinalSupport.RequiredDisplacement);
                        }
                        rows.Add(new JObject
                        {
                            ["frame"] = input.Sequence, ["dt"] = input.Delta, ["state"] = value.Motion.ConstraintState.ToString(),
                            ["contact"] = correctedSource ? input.CurrentStep.Contact : input.HistoricalStep.Contact,
                            ["lockWeight"] = correctedSource ? input.CurrentStep.LockWeight : input.HistoricalStep.LockWeight,
                            ["responseSourceLineage"] = correctedSource ? input.CurrentLineage.ToString() : input.HistoricalLineage.ToString(),
                            ["authorWeight"] = input.Weight, ["positionWeight"] = value.Output.GoalTarget.PositionWeight,
                            ["rotationWeight"] = value.Output.GoalTarget.RotationWeight,
                            ["sole"] = Vec(value.Output.Pose.EffectiveSole), ["rotationStepDegrees"] = i > 0 ? new JValue(rotationStep) : JValue.CreateNull(),
                            ["ankle"] = Vec(value.Output.Pose.EffectiveAnkle), ["fixedHip"] = Vec(hip),
                            ["originalHip"] = Vec(input.RootPosition + input.RootRotation * input.Recorded.V("leg/leg-pose/original-hip")),
                            ["originalKnee"] = Vec(input.RootPosition + input.RootRotation * input.Recorded.V("leg/leg-pose/original-knee")),
                            ["originalAnkle"] = Vec(input.RootPosition + input.RootRotation * input.Recorded.V("leg/leg-pose/original-ankle")),
                            ["sourceAnkle"] = Vec(input.Animated.AnklePosition),
                            ["rotationSpeedDegreesPerSecond"] = i > 0 ? new JValue(rotationStep / input.Delta) : JValue.CreateNull(),
                            ["originalRotationStepDegrees"] = i > 0 ? new JValue(originalRotationStep) : JValue.CreateNull(),
                            ["extraRotationStepDegrees"] = i > 0 ? new JValue(rotationStep - originalRotationStep) : JValue.CreateNull(),
                            ["animationRelativeCorrectionStepDegrees"] = i > 0 ? new JValue(relativeCorrectionStep) : JValue.CreateNull(),
                            ["animationRelativeCorrectionSpeedDegreesPerSecond"] = i > 0 ? new JValue(relativeCorrectionStep / input.Delta) : JValue.CreateNull(),
                            ["soleStepMeters"] = i > 0 ? new JValue(positionStep) : JValue.CreateNull(),
                            ["horizontalSoleStepMeters"] = i > 0 ? new JValue(horizontalStep) : JValue.CreateNull(),
                            ["soleSpeedMetersPerSecond"] = i > 0 ? new JValue(positionStep / input.Delta) : JValue.CreateNull(),
                            ["originalSoleStepMeters"] = i > 0 ? new JValue(originalPositionStep) : JValue.CreateNull(),
                            ["extraSoleStepMeters"] = i > 0 ? new JValue(positionStep - originalPositionStep) : JValue.CreateNull(),
                            ["rotation"] = new JArray(value.Output.Pose.EffectiveRotation.x, value.Output.Pose.EffectiveRotation.y, value.Output.Pose.EffectiveRotation.z, value.Output.Pose.EffectiveRotation.w),
                            ["recordedSoleError"] = error, ["recordedRotationErrorDegrees"] = rotationError,
                            ["finalSupportAvailable"] = value.FinalSupport.Available,
                            ["finalPenetration"] = value.FinalSupport.Available ? new JValue(value.FinalSupport.RequiredDisplacement) : JValue.CreateNull(),
                            ["finalAcceptedProbes"] = value.FinalSupport.AcceptedSampleCount,
                            ["hasContactAnchor"] = value.HasAnchor, ["contactAnchor"] = Vec(value.Anchor),
                            ["fixedHipReachRatio"] = reach,
                            ["recordedLandingReachFeedback"] = input.RecordedLandingReach
                        });
                        if (bilateral)
                        {
                            var pelvis = pelvisResults[i];
                            Vector3 local = pelvisGoals[i].ComponentPosition;
                            float weight = pelvisGoals[i].PositionWeight;
                            float pelvisError = Vector3.Distance(local, input.Recorded.V("pelvis-goal/component-position"));
                            rows[rows.Count - 1]["pelvisState"] = pelvis.State.ToString();
                            rows[rows.Count - 1]["pelvisDelta"] = Vec(local);
                            rows[rows.Count - 1]["pelvisWeight"] = weight;
                            rows[rows.Count - 1]["recordedPelvisError"] = pelvisError;
                            rows[rows.Count - 1]["recomputedLandingReachFeedback"] = pelvis.RightLandingReachAvailable;
                            rows[rows.Count - 1]["pairedSole"] = Vec(pairedResults[i].Output.Pose.EffectiveSole);
                            rows[rows.Count - 1]["pairedAnkle"] = Vec(pairedResults[i].Output.Pose.EffectiveAnkle);
                            rows[rows.Count - 1]["pairedHip"] = Vec(pairedInputs[i].Animated.HipPosition + pelvis.PelvisDelta * pelvis.PositionWeight);
                            rows[rows.Count - 1]["pairedOriginalHip"] = Vec(pairedInputs[i].RootPosition + pairedInputs[i].RootRotation * pairedInputs[i].Recorded.V("leg/leg-pose/original-hip"));
                            rows[rows.Count - 1]["pairedOriginalKnee"] = Vec(pairedInputs[i].RootPosition + pairedInputs[i].RootRotation * pairedInputs[i].Recorded.V("leg/leg-pose/original-knee"));
                            rows[rows.Count - 1]["pairedOriginalAnkle"] = Vec(pairedInputs[i].RootPosition + pairedInputs[i].RootRotation * pairedInputs[i].Recorded.V("leg/leg-pose/original-ankle"));
                            rows[rows.Count - 1]["pairedFinalPenetration"] = pairedResults[i].FinalSupport.Available ? new JValue(pairedResults[i].FinalSupport.RequiredDisplacement) : JValue.CreateNull();
                            rows[rows.Count - 1]["pairedFinalSupportAvailable"] = pairedResults[i].FinalSupport.Available;
                            rows[rows.Count - 1]["pairedState"] = pairedResults[i].Motion.ConstraintState.ToString();
                            rows[rows.Count - 1]["pairedHasContactAnchor"] = pairedResults[i].HasAnchor;
                            rows[rows.Count - 1]["pairedPositionWeight"] = pairedResults[i].Output.GoalTarget.PositionWeight;
                            rows[rows.Count - 1]["pairedRotation"] = new JArray(pairedResults[i].Output.Pose.EffectiveRotation.x, pairedResults[i].Output.Pose.EffectiveRotation.y,
                                pairedResults[i].Output.Pose.EffectiveRotation.z, pairedResults[i].Output.Pose.EffectiveRotation.w);
                            float leftRotationStep = i > 0 ? Quaternion.Angle(pairedResults[i - 1].Output.Pose.EffectiveRotation, pairedResults[i].Output.Pose.EffectiveRotation) : 0f;
                            float leftOriginalRotationStep = i > 0 ? Quaternion.Angle(pairedInputs[i - 1].Animated.AnkleRotation, pairedInputs[i].Animated.AnkleRotation) : 0f;
                            float leftRelativeStep = i > 0 ? Quaternion.Angle(
                                pairedResults[i - 1].Output.Pose.EffectiveRotation * Quaternion.Inverse(pairedInputs[i - 1].Animated.AnkleRotation),
                                pairedResults[i].Output.Pose.EffectiveRotation * Quaternion.Inverse(pairedInputs[i].Animated.AnkleRotation)) : 0f;
                            rows[rows.Count - 1]["pairedRotationStepDegrees"] = i > 0 ? new JValue(leftRotationStep) : JValue.CreateNull();
                            rows[rows.Count - 1]["pairedOriginalRotationStepDegrees"] = i > 0 ? new JValue(leftOriginalRotationStep) : JValue.CreateNull();
                            rows[rows.Count - 1]["pairedRelativeRotationStepDegrees"] = i > 0 ? new JValue(leftRelativeStep) : JValue.CreateNull();
                            rows[rows.Count - 1]["pairedSoleStepMeters"] = i > 0 ? new JValue(Vector3.Distance(pairedResults[i - 1].Output.Pose.EffectiveSole, pairedResults[i].Output.Pose.EffectiveSole)) : JValue.CreateNull();
                            rows[rows.Count - 1]["pairedReachRatio"] = Vector3.Distance(pairedInputs[i].Animated.HipPosition + pelvis.PelvisDelta * pelvis.PositionWeight,
                                pairedResults[i].Output.Pose.EffectiveAnkle) / pairedInputs[i].LegLength;
#if RELEASING_FIXTURE
                            rows[rows.Count - 1]["rightGoal"] = ReleasingGoalJson(CapturedFootModuleFunctions.EncodeFootGoal(in value.Output));
                            rows[rows.Count - 1]["leftGoal"] = ReleasingGoalJson(CapturedFootModuleFunctions.EncodeFootGoal(in pairedResults[i].Output));
                            rows[rows.Count - 1]["pelvisGoal"] = ReleasingGoalJson(pelvisGoals[i]);
                            rows[rows.Count - 1]["bodyTrajectoryUsed"] = value.BodyTrajectoryUsed;
                            rows[rows.Count - 1]["predictionMotionRevision"] = value.PredictionMotionRevision.ToString();
                            rows[rows.Count - 1]["predictionState"] = value.Prediction.State.ToString();
                            rows[rows.Count - 1]["predictionEventIdentity"] = value.Prediction.LandingEventIdentity.ToString();
                            rows[rows.Count - 1]["pairedPredictionState"] = pairedResults[i].Prediction.State.ToString();
                            rows[rows.Count - 1]["pairedPredictionEventIdentity"] = pairedResults[i].Prediction.LandingEventIdentity.ToString();
                            rows[rows.Count - 1]["groundPathAccepted"] = value.PathState == CharacterFootGroundPathState.Accepted;
                            rows[rows.Count - 1]["pairedGroundPathAccepted"] = pairedResults[i].PathState == CharacterFootGroundPathState.Accepted;
                            rows[rows.Count - 1]["rightFrameDiagnostics"] = ReleasingFrameJson(in value);
                            rows[rows.Count - 1]["leftFrameDiagnostics"] = ReleasingFrameJson(in pairedResults[i]);
                            Vector3 correction = value.Output.Pose.EffectiveSole - CharacterFootConstraintMath.ResolveOriginalSole(input.Animated);
                            Vector3 leftCorrection = pairedResults[i].Output.Pose.EffectiveSole - CharacterFootConstraintMath.ResolveOriginalSole(pairedInputs[i].Animated);
                            Vector3 previousCorrection = i > 0 ? results[i-1].Output.Pose.EffectiveSole - CharacterFootConstraintMath.ResolveOriginalSole(inputs[i-1].Animated) : correction;
                            Vector3 previousLeftCorrection = i > 0 ? pairedResults[i-1].Output.Pose.EffectiveSole - CharacterFootConstraintMath.ResolveOriginalSole(pairedInputs[i-1].Animated) : leftCorrection;
                            rows[rows.Count - 1]["rightCorrection"] = Vec(correction);
                            rows[rows.Count - 1]["leftCorrection"] = Vec(leftCorrection);
                            rows[rows.Count - 1]["rightCorrectionDelta"] = Vec(correction - previousCorrection);
                            rows[rows.Count - 1]["leftCorrectionDelta"] = Vec(leftCorrection - previousLeftCorrection);
                            rows[rows.Count - 1]["rightCorrectionSpeed"] = i > 0 ? new JValue((correction-previousCorrection).magnitude/input.Delta) : JValue.CreateNull();
                            rows[rows.Count - 1]["leftCorrectionSpeed"] = i > 0 ? new JValue((leftCorrection-previousLeftCorrection).magnitude/input.Delta) : JValue.CreateNull();
#if P1_FIXTURE
                            rows[rows.Count - 1]["rightContactClearance"] = P1ClearanceJson(in value);
                            rows[rows.Count - 1]["leftContactClearance"] = P1ClearanceJson(in pairedResults[i]);
#endif
#endif
#if !P1_FIXTURE
                            Assert.That(pairedResults[i].GoalError, Is.LessThan(.0001f));
                            Assert.That(pairedResults[i].GoalRotationError, Is.LessThan(.1f));
                            if (candidate && pairedResults[i].Output.GoalTarget.PositionWeight > 0f)
                            {
                                Assert.That(pairedResults[i].FinalSupport.Available, Is.True);
                                Assert.That(pairedResults[i].FinalSupport.RequiredDisplacement, Is.LessThanOrEqualTo(.0002f));
                            }
                            if (!candidate)
                            {
                                Assert.That(pelvisError, Is.LessThan(.00002f), "历史骨盆Goal必须先重现原录制");
                                Assert.That(weight, Is.EqualTo(input.Recorded.F("pelvis-goal/position-weight")));
                                Assert.That(Vector3.Distance(pairedResults[i].Output.Pose.EffectiveSole, pairedInputs[i].Recorded.V("foot/resolved/core/effective-sole")), Is.LessThan(.0005f));
                                Assert.That(pelvis.RightLandingReachAvailable, Is.EqualTo(input.RecordedLandingReach));
                                Assert.That(pelvis.LeftLandingReachAvailable, Is.EqualTo(pairedInputs[i].RecordedLandingReach));
                            }
#endif
                        }
#if ROTATION_CANDIDATE
                        rows[rows.Count - 1]["hasRotationCorrection"] = value.HasRotationCorrection;
                        rows[rows.Count - 1]["rotationCorrection"] = new JArray(value.RotationCorrection.x, value.RotationCorrection.y, value.RotationCorrection.z, value.RotationCorrection.w);
                        if (input.Recorded.F("foot/resolved/core/position-weight") == 0f)
                        {
                            Assert.That(value.HasRotationCorrection, Is.False, "真实无输出帧必须清空旋转修正历史");
                            Assert.That(value.Output.GoalTarget.RotationWeight, Is.Zero);
                        }
#endif
#if !P1_FIXTURE
                        Assert.That(value.Output.GoalTarget.PositionWeight, Is.EqualTo(input.Recorded.F("foot/resolved/core/position-weight")),
                            "原本无有效 Goal 的帧需保留零权重；有有效 Goal 的帧保留作者权重：" + input.Sequence);
                        Assert.That(value.GoalError, Is.LessThan(.0001f));
                        Assert.That(value.GoalRotationError, Is.LessThan(.1f), "最终查询必须重现正式作者加权旋转");
                        if (candidate && value.FinalSupport.Available)
                            Assert.That(value.FinalSupport.RequiredDisplacement, Is.LessThanOrEqualTo(.0002f), "有查询命中的候选最终脚掌不得新增正穿透");
                        if (candidate && value.Output.GoalTarget.PositionWeight > 0f)
                            Assert.That(value.FinalSupport.Available, Is.True, "实际输出 Goal 的候选帧必须取得最终脚掌净空证据");
#endif
                        previousRotation = value.Output.Pose.EffectiveRotation;
                        previousRelativeCorrection = relativeCorrection;
                    }
                    report[candidate ? "current" : "historical"] = new JObject
                    {
                        ["frames"] = inputs.Length, ["allocatedBytes"] = allocated, ["maximumRecordedSoleError"] = maxError,
                        ["maximumRecordedRotationErrorDegrees"] = maxRotationError, ["maximumRotationStepDegrees"] = maxRotationStep,
                        ["observedFinalFrames"] = observedFinalFrames,
                        ["maximumFinalPenetration"] = observedFinalFrames > 0 ? new JValue(maxPenetration) : JValue.CreateNull(),
                        ["maximumExtraRotationStepDegrees"] = maxExtraRotationStep, ["rows"] = rows
                    };
                    var summary = (JObject)report[candidate ? "current" : "historical"];
                    summary["maximumSoleStepMeters"] = maxPositionStep;
                    summary["maximumHorizontalSoleStepMeters"] = maxHorizontalStep;
                    summary["maximumExtraSoleStepMeters"] = maxExtraPositionStep;
                    summary["maximumFixedHipReachRatio"] = maxReach;
                    summary["fixedHipOverreachFrames"] = overreachFrames;
                    summary["fixedHipOverreachDurationSeconds"] = overreachDuration;
                    summary["maximumAnimationRelativeCorrectionStepDegrees"] = maxRelativeCorrectionStep;
#if P1_FIXTURE
                    for (int i = 0; i < results.Length; i++)
                    {
                        s_P1Frame = inputs[i].Sequence;
                        ValidateP1Output(in results[i], inputs[i]);
                        ValidateP1Output(in pairedResults[i], pairedInputs[i]);
                    }
#endif
                    Assert.That(allocated, Is.Zero);
                    if (!candidate && (sourceComparison || liveSwitch))
                    {
                        Assert.That(maxError, Is.LessThan(.0005f), "基线必须先重现原脚位");
                        Assert.That(maxRotationError, Is.LessThan(.1f), "基线必须先重现原有效旋转");
                    }
                }
                var historical = (JObject)report["historical"];
                var current = (JObject)report["current"];
                if (sourceComparison && !releasing && side == CharacterFootSide.Left)
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
                else if (sourceComparison && !releasing)
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
                if (!sourceComparison && side == CharacterFootSide.Left)
                    foreach (JObject row in report[footVariant == "rotation-current" ? "current" : "historical"]["rows"])
                        if ((ulong)row["frame"] >= 1178 && (ulong)row["frame"] <= 1180)
                        {
                            Assert.That((string)row["state"], Is.EqualTo("Swing"));
                            Assert.That((bool)row["hasContactAnchor"], Is.False);
                        }
                if (!sourceComparison && (ulong)fixture["firstFrame"] == 874)
                {
                    var rows = (JArray)report[footVariant == "rotation-current" ? "current" : "historical"]["rows"];
                    JObject anchorFrame = (JObject)rows.Single(x => (ulong)x["frame"] == 893);
                    var anchor = anchorFrame["contactAnchor"];
                    foreach (JObject row in rows)
                        if ((ulong)row["frame"] >= 894 && (ulong)row["frame"] <= 898)
                        {
                            Assert.That((bool)row["hasContactAnchor"], Is.True);
                            var point = row["contactAnchor"];
                            Assert.That(Vector3.Distance(new Vector3((float)anchor[0], (float)anchor[1], (float)anchor[2]),
                                new Vector3((float)point[0], (float)point[1], (float)point[2])), Is.LessThanOrEqualTo(.0002f));
                        }
                    Assert.That((bool)rows.Single(x => (ulong)x["frame"] == 954)["hasContactAnchor"], Is.False);
                }
                report["status"] = "passed";
                report["stage"] = "complete";
            }
            catch (Exception error)
            {
                report["status"] = "failed";
                report["failure"] = error.ToString();
#if P1_FIXTURE
                report["failureFrame"] = s_P1Frame;
#endif
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
            internal float GoalRotationError;
#if ROTATION_CANDIDATE
            internal bool HasRotationCorrection;
            internal Quaternion RotationCorrection;
#endif
            internal bool HasAnchor;
            internal Vector3 Anchor;
#if P1_FIXTURE
            internal bool HeelObserved, ToeObserved;
            internal float HeelClearance, ToeClearance, MaximumPointClearance;
            internal int ObservedPoints;
#endif
#if RELEASING_FIXTURE
            internal bool BodyTrajectoryUsed;
            internal ulong PredictionMotionRevision;
            internal CharacterFootLandingPredictionResult Prediction;
            internal CharacterFootGroundPathState PathState;
            internal CharacterFootGroundPathRejectReason PathRejectReason;
            internal ulong PathInputIdentity;
            internal bool PathQueryExecuted;
            internal int PathContactCount, PathEnvelopeVertexCount, PathSegmentCount;
            internal Vector3 PathLastLanding, PathNextLanding;
#endif
        }

#if !P1_FIXTURE
        static void RunStoredGoals(StoredGoalInput[] inputs, in CharacterFootLifecycleContext seed,
            StoredGoalOutput[] results, bool candidate, Transform root, in CharacterFootSoleSupportQuery query,
            ulong revision, CharacterFootSoleProbeBuffer targetProbes, CharacterFootSoleProbeBuffer outputProbes)
        {
            CharacterFootLifecycleContext context = seed;
            for (int i = 0; i < inputs.Length; i++)
            {
                StoredGoalInput input = inputs[i];
                root.SetPositionAndRotation(input.RootPosition, input.RootRotation);
                EvaluateCapturedFoot(ref context, input, candidate, root, in query, revision,
                    targetProbes, outputProbes, out var receipt, out _, out _);
                CharacterResolvedFootResult output = receipt.Complete(ref context, input.RecordedLandingReach, out var motion);
                results[i] = QueryCapturedGoal(input, in output, in motion, in context, root, in query, revision);
            }
        }
#endif

        static CharacterFootPlacementRequest EvaluateCapturedFoot(ref CharacterFootLifecycleContext context,
            StoredGoalInput input, bool candidate, Transform root, in CharacterFootSoleSupportQuery query,
            ulong revision, CharacterFootSoleProbeBuffer targetProbes, CharacterFootSoleProbeBuffer outputProbes,
            out CharacterFootLifecycle.Completion receipt, out AnimationFootMotionRuntimeSample step, out CharacterFootSwingMotionResult swing)
        {
                step = candidate ? input.CurrentStep : input.HistoricalStep;
                var snapshot = CharacterFootLandingRuntime.ProjectAfterPrediction(in context, in step, in input.Prediction, in input.Settings);
                swing = CharacterFootSwingMotionBuilder.Build(in input.Animated, in step, input.Weight,
                    Vector3.up, in input.Path, true, step.FootHeight, snapshot.NextSwingPredictionError);
                var request = new CharacterFootLockRequest(in step);
                bool prepared = snapshot.PlantTargetState == CharacterFootPlantTargetState.Tracking;
                CharacterFootGroundPathLanding contactLanding = default;
                bool hasContact = request.RequestsLock && (snapshot.TryResolveVerifiedLanding(request.EventIdentity, out contactLanding) ||
                    CharacterFootLandingRuntime.TryResolveCurrentContactCandidate(in step, in input.Prediction, out contactLanding));
                var frame = new CharacterFootStateFrame(input.Sequence, input.Completion, input.RigId, input.RigRevision, input.Side,
                    in input.Animated, input.Animated.HipPosition, input.LegLength, in swing, in input.Path,
                    hasContact, in contactLanding, prepared, prepared ? snapshot.PlantTarget : default,
                    in input.Support, in request, step.Support, request.EventIdentity,
                    CapturedFootModuleFunctions.ResolveFootGoalOwnershipLoss(input.Grounded, step.IsAuthoritative),
                    input.Weight, Vector3.up, input.Delta, candidate ? input.CurrentLineage : input.HistoricalLineage,
                    input.ProfileRevision, revision, in input.Settings);
                var nextLanding = snapshot.NextSwingLanding;
                var stride = new CharacterFootStrideRequest(in step, snapshot.HasNextSwingLanding, in nextLanding, input.Path.Accepted);
                var evaluation = new CharacterFootStateEvaluation(input.Side, in step, in input.Prediction, in frame,
                    in stride, input.Grounded, root, in query, targetProbes, outputProbes);
                return CharacterFootLifecycle.Evaluate(ref context, in evaluation, out receipt);
        }

        static StoredGoalOutput QueryCapturedGoal(StoredGoalInput input, in CharacterResolvedFootResult output,
            in CharacterFootSwingMotionResult motion, in CharacterFootLifecycleContext context, Transform root,
            in CharacterFootSoleSupportQuery query, ulong revision)
        {
                Vector3 goal = Vector3.Lerp(input.Animated.AnklePosition,
                    root.TransformPoint(output.GoalTarget.ComponentPosition), output.GoalTarget.PositionWeight);
                Quaternion rotation = root.rotation * output.GoalTarget.ComponentRotation;
                Quaternion effectiveRotation = Quaternion.Slerp(input.Animated.AnkleRotation, rotation, output.GoalTarget.RotationWeight);
                var finalContacts = input.Animated.ResolveSoleContacts(goal, effectiveRotation);
                var final = query.Query(input.Sequence, input.Completion, revision, input.Side, Vector3.up,
                    input.Grounded, in finalContacts, input.FinalProbes);
#if P1_FIXTURE
                var endpoints = new FixedList512Bytes<Vector3>();
                endpoints.Add(finalContacts.HeelPosition);
                endpoints.Add(finalContacts.ToePosition);
                var endpointContacts = new CharacterFootPlacementSoleContactPose(finalContacts.HeelPosition, finalContacts.ToePosition, in endpoints);
                query.Query(input.Sequence, input.Completion, revision, input.Side, Vector3.up,
                    input.Grounded, in endpointContacts, input.EndpointProbes);
                var heel = input.EndpointProbes[0];
                var toe = input.EndpointProbes[1];
                float maximumClearance = float.NegativeInfinity;
                int observedPoints = 0;
                for (int p = 0; p < input.FinalProbes.Count; p++)
                {
                    var probe = input.FinalProbes[p];
                    if (probe.Result.Accepted)
                    {
                        observedPoints++;
                        maximumClearance = Mathf.Max(maximumClearance, Vector3.Dot(probe.Position - probe.Result.Point, Vector3.up));
                    }
                }
#endif
                return new StoredGoalOutput { Output = output, Motion = motion, FinalSupport = final,
#if P1_FIXTURE
                    HeelObserved = heel.Result.Accepted, ToeObserved = toe.Result.Accepted,
                    HeelClearance = Vector3.Dot(heel.Position - heel.Result.Point, Vector3.up),
                    ToeClearance = Vector3.Dot(toe.Position - toe.Result.Point, Vector3.up),
                    MaximumPointClearance = maximumClearance, ObservedPoints = observedPoints,
#endif
#if RELEASING_FIXTURE
                    BodyTrajectoryUsed = input.BodyTrajectoryUsed, PredictionMotionRevision = input.PredictionMotionRevision,
                    Prediction = input.Prediction, PathState = input.Path.State, PathRejectReason = input.Path.RejectReason,
                    PathInputIdentity = input.Path.InputIdentity, PathQueryExecuted = input.Path.QueryExecutedThisFrame,
                    PathContactCount = input.Path.ContactCount, PathEnvelopeVertexCount = input.Path.EnvelopeVertexCount,
                    PathSegmentCount = input.Path.SegmentCount, PathLastLanding = input.Path.LastLanding, PathNextLanding = input.Path.NextSwingLanding,
#endif
                    GoalError = Vector3.Distance(goal, output.Pose.EffectiveAnkle),
                    GoalRotationError = Quaternion.Angle(effectiveRotation, output.Pose.EffectiveRotation),
#if ROTATION_CANDIDATE
                    HasRotationCorrection = context.Interpolation.HasRotationCorrection,
                    RotationCorrection = context.Interpolation.RotationCorrection,
#endif
                    HasAnchor = context.Contact.HasContact, Anchor = context.Contact.Anchor };
        }

        static void RunBilateralGoals(StoredGoalInput[] rightInputs, StoredGoalInput[] leftInputs,
            in CharacterFootLifecycleContext rightSeed, in CharacterFootLifecycleContext leftSeed,
            StoredGoalOutput[] rightResults, StoredGoalOutput[] leftResults, CharacterFootStrideHipsResult[] pelvisResults,
            CharacterFullBodyIkGoal[] pelvisGoals, Transform root, in CharacterFootSoleSupportQuery query, ulong revision,
            CharacterFootSoleProbeBuffer rightTargetProbes, CharacterFootSoleProbeBuffer rightOutputProbes,
            CharacterFootSoleProbeBuffer leftTargetProbes, CharacterFootSoleProbeBuffer leftOutputProbes, bool correctedSource
#if RELEASING_FIXTURE
            , ReleasingPredictionWorkspace predictionWorkspace
#endif
            )
        {
            CharacterFootLifecycleContext left = leftSeed, right = rightSeed;
            CharacterFootPrimarySupportState primary = default;
            CharacterFootPelvisSpringState spring = default;
#if RELEASING_FIXTURE
            predictionWorkspace.Reset();
            primary = predictionWorkspace.PrimarySeed;
            spring = predictionWorkspace.SpringSeed;
#endif
            for (int i = 0; i < rightInputs.Length; i++)
            {
                StoredGoalInput l = leftInputs[i], r = rightInputs[i];
#if P1_FIXTURE
                s_P1Frame = r.Sequence;
#endif
                root.SetPositionAndRotation(r.RootPosition, r.RootRotation);
#if RELEASING_FIXTURE
                predictionWorkspace.Prepare(l, r, ref left, ref right, correctedSource);
#endif
                var leftRequest = EvaluateCapturedFoot(ref left, l, correctedSource, root, in query, revision, leftTargetProbes, leftOutputProbes,
                    out var leftCompletion, out var leftStep, out var leftSwing);
                var rightRequest = EvaluateCapturedFoot(ref right, r, correctedSource, root, in query, revision, rightTargetProbes, rightOutputProbes,
                    out var rightCompletion, out var rightStep, out var rightSwing);
                bool selected = CharacterFootStrideHipsBuilder.TrySelectSwing(in leftStep, in rightStep, in leftSwing, in rightSwing, out var selectedSide);
                var requests = new CharacterFootPlacementRequestPair(in leftRequest, in rightRequest);
                var support = CharacterFootStrideHipsBuilder.ResolvePrimarySupport(in leftRequest, in rightRequest, ref primary);
                var intent = CharacterFootStrideHipsBuilder.ResolveIntent(in requests, in support, r.Grounded, selected, selectedSide, Vector3.up);
                var pose = new CharacterFootPlacementAnimatedPose(r.Sequence, r.AnimatedPelvisComponentPosition, l.Animated, r.Animated);
                var frame = new CharacterFootPelvisFrame(Vector3.up, r.RootPosition, r.AnimatedPelvis,
                    r.AnimatedPelvisComponentPosition, in pose,
                    leftRequest.GoalTarget.EffectiveSole, rightRequest.GoalTarget.EffectiveSole, l.LegLength, r.LegLength, r.Weight, r.Delta);
                var input = CharacterFootStrideHipsBuilder.PreparePelvis(in intent, in requests, in support, in frame);
                var pelvis = CharacterFootStrideHipsBuilder.ResolvePelvis(in input, in r.Settings, ref spring);
#if P1_CANDIDATE
                Vector3 weightedPelvisDelta = pelvis.PelvisDelta * pelvis.PositionWeight;
                var leftOutput = leftCompletion.Complete(ref left, pelvis.LeftLandingReachAvailable, weightedPelvisDelta, out var leftMotion);
                var rightOutput = rightCompletion.Complete(ref right, pelvis.RightLandingReachAvailable, weightedPelvisDelta, out var rightMotion);
#else
                var leftOutput = leftCompletion.Complete(ref left, pelvis.LeftLandingReachAvailable, out var leftMotion);
                var rightOutput = rightCompletion.Complete(ref right, pelvis.RightLandingReachAvailable, out var rightMotion);
#endif
                pelvisGoals[i] = CapturedFootModuleFunctions.CreatePelvisGoal(in pelvis, root);
                if (!pelvis.ProducesPelvisGoal) spring.Clear();
                leftResults[i] = QueryCapturedGoal(l, in leftOutput, in leftMotion, in left, root, in query, revision);
                rightResults[i] = QueryCapturedGoal(r, in rightOutput, in rightMotion, in right, root, in query, revision);
                pelvisResults[i] = pelvis;
            }
        }

        sealed class StoredGoalInput
        {
            internal StoredGoalInput(JToken captured, JObject columns, JToken sourceResult, CharacterFootPlacementProfile profile, float legLength, CharacterFootSide side,
                string sampleName = "currentSample", bool beforePelvis = false)
            {
                Recorded = Row.Main(captured, columns);
                Row r = Recorded;
                Side = side;
                ExpectedProbes = Row.Table(captured, columns, "probes").OrderBy(x => x.I("sample-index")).ToArray();
                Animated = AnimatedPose(r, ExpectedProbes);
                if (beforePelvis)
                {
                    Vector3 pelvisShift = r.Q("physical-body/pose-root-world-rotation") * r.V("pelvis-goal/component-position") * r.F("pelvis-goal/position-weight");
                    var pose = new CharacterFootPlacementAnimatedFootPose(Animated.HipPosition - pelvisShift,
                        Animated.KneePosition - pelvisShift, Animated.AnklePosition, Animated.AnkleRotation,
                        Animated.ToePosition, Animated.ToeRotation, Animated.HeelPosition, Animated.SoleForward,
                        Animated.SoleUp, Animated.SemanticRotation, Animated.SoleFrameLocalRotation);
                    pose.SoleSamples = Animated.SoleSamples;
                    Animated = pose;
                    if (r.B("foot/resolved/reach/landing-available"))
                        Assert.That(Vector3.Distance(Animated.HipPosition, r.V("foot/resolved/reach/landing-hip")), Is.LessThan(.00002f), "正式计算前Hip必须重现直接采样，不能用录制骨盆后的Hip");
                }
                HistoricalStep = StoredRecordedStep(r, side);
                CurrentStep = StoredProducedStep(sourceResult[sampleName], side);
                RootPosition = r.V("physical-body/pose-root-world-position");
                RootRotation = r.Q("physical-body/pose-root-world-rotation");
                AnimatedPelvis = r.V("stride/result/animated-pelvis");
                AnimatedPelvisComponentPosition = r.V("stride/result/animated-pelvis-component-position");
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
#if RELEASING_FIXTURE
                PredictionInput = new ReleasingPredictionInput(captured, columns);
#endif
            }
            internal readonly Row Recorded;
            internal readonly Row[] ExpectedProbes;
            internal readonly CharacterFootSide Side;
            internal readonly CharacterFootPlacementAnimatedFootPose Animated;
            internal readonly AnimationFootMotionRuntimeSample HistoricalStep, CurrentStep;
            internal CharacterFootGroundPathResult Path;
            internal CharacterFootLandingPredictionResult Prediction;
#if RELEASING_FIXTURE
            internal readonly ReleasingPredictionInput PredictionInput;
            internal bool BodyTrajectoryUsed;
            internal ulong PredictionMotionRevision;
#endif
            internal readonly CharacterFootMotionSettings Settings;
            internal readonly Vector3 RootPosition;
            internal readonly Vector3 AnimatedPelvis, AnimatedPelvisComponentPosition;
            internal readonly Quaternion RootRotation;
            internal readonly ulong Sequence, Completion;
            internal readonly bool Grounded, RecordedLandingReach;
            internal readonly float Weight, Delta, LegLength;
            internal readonly FixedString64Bytes RigId, RigRevision;
            internal readonly FixedString128Bytes ProfileRevision, HistoricalLineage, CurrentLineage;
            internal readonly CharacterFootSoleProbeBuffer InputProbes = new CharacterFootSoleProbeBuffer();
            internal readonly CharacterFootSoleProbeBuffer FinalProbes = new CharacterFootSoleProbeBuffer();
#if P1_FIXTURE
            internal readonly CharacterFootSoleProbeBuffer EndpointProbes = new CharacterFootSoleProbeBuffer();
#endif
            internal CharacterFootCurrentSupportObservation Support;
        }

#if P1_FIXTURE
        static void ValidateP1Output(in StoredGoalOutput value, StoredGoalInput input)
        {
            float ankle = value.Output.Pose.EffectiveAnkle.sqrMagnitude;
            float sole = value.Output.Pose.EffectiveSole.sqrMagnitude;
            Assert.That(float.IsNaN(ankle) || float.IsInfinity(ankle) || float.IsNaN(sole) || float.IsInfinity(sole), Is.False,
                "最终脚位必须有限，失败保存首帧，不以默认值替代");
            Assert.That(value.Output.GoalTarget.PositionWeight, Is.EqualTo(input.Recorded.F("foot/resolved/core/position-weight")));
            Assert.That(value.GoalError, Is.LessThan(.0001f));
            Assert.That(value.GoalRotationError, Is.LessThan(.1f));
            if (value.Output.GoalTarget.PositionWeight > 0f)
            {
                Assert.That(value.FinalSupport.Available, Is.True);
                Assert.That(value.FinalSupport.RequiredDisplacement, Is.LessThanOrEqualTo(.0002f));
                if (value.Motion.ConstraintState == CharacterFootConstraintState.Landing || value.Motion.ConstraintState == CharacterFootConstraintState.Locked)
                {
                    Assert.That(value.HeelObserved, Is.True);
                    Assert.That(value.ToeObserved, Is.True);
                }
            }
        }

        static JObject P1ClearanceJson(in StoredGoalOutput value) => new JObject
        {
            ["heelObserved"] = value.HeelObserved, ["toeObserved"] = value.ToeObserved,
            ["heelGap"] = value.HeelObserved ? new JValue(value.HeelClearance) : JValue.CreateNull(),
            ["toeGap"] = value.ToeObserved ? new JValue(value.ToeClearance) : JValue.CreateNull(),
            ["wholeFootMinimumGap"] = value.FinalSupport.Available ? new JValue(-value.FinalSupport.RequiredDisplacement) : JValue.CreateNull(),
            ["wholeFootMaximumGap"] = value.ObservedPoints > 0 ? new JValue(value.MaximumPointClearance) : JValue.CreateNull(),
            ["observedPoints"] = value.ObservedPoints, ["goalError"] = value.GoalError,
            ["goalRotationErrorDegrees"] = value.GoalRotationError
        };
#endif

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
