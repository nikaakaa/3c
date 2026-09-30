/*
目的：验证已释放接触交还目标控制权，并沿同一连续输入检查真实地形查询、插值与最终单脚 Goal。
输入：采样 B 右脚 34668～34767 共 100 帧、每帧 23 个原脚底探针、实际帧间隔、事件、路径及 Corin 正式配置。
链路：录制输入 → LandingRuntime → SwingMotionBuilder → Lifecycle（插值、真实 PhysicsScene 查询、输出约束）→ GoalTarget。
预期：旧版重现 27 帧目标缺失与掉权重；当前版保持作者权重 1，前 45 帧输出一致，24 帧真实接触继续保留目标。
边界：先逐点确认原地形一致，再连续推进两版冷启动上下文；不逐帧回填期望状态，不执行骨盆、腿 IK 或整角色回放。
净空只检查新姿态上正式向下查询命中的脚底点；无命中不能证明无碰撞。缺失的窗口前插值历史不用于要求旧空间轨迹重现。
解析、断言和 JSON 写出在计算循环外；预热后分别测量两版连续计算分配，失败也保存已完成帧和执行阶段。
说明：docs/diagnostics/foot-placement/ik-tests/expired-contact.html。
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterFootCapturedContactTests
    {
        const string AssetRoot = "Assets/Configs/Character/Corin/Pipeline/Presentation/";
        const string RuntimeSource = "3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootLandingRuntime.cs";
        const BindingFlags StaticMethods = BindingFlags.Static | BindingFlags.NonPublic;
        static readonly string Repository = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.."));
        static readonly string EvidenceDirectory = Path.Combine(Repository, "docs/diagnostics/foot-placement/ik-tests");

        delegate CharacterFootLandingSnapshot ProjectLanding(
            in CharacterFootLifecycleContext context, in AnimationFootMotionRuntimeSample sample,
            in CharacterFootLandingPredictionResult prediction, in CharacterFootMotionSettings settings);

        delegate void AdvanceLanding(
            ref CharacterFootLandingContext context, in AnimationFootMotionRuntimeSample sample,
            in CharacterFootLandingPredictionResult prediction, in CharacterFootMotionSettings settings);

        delegate CharacterFootPlacementRequest ResolveLifecycle(
            ref CharacterFootLifecycleContext context, in CharacterFootStateEvaluation evaluation,
            out CharacterFootSwingMotionResult motion, out CharacterFootLifecycle.Completion completion);

        [Test]
        public void CapturedContactToSwingReleasesExpiredTargetWithoutLosingValidSupport()
        {
            var report = new JObject
            {
                ["test"] = GetType().FullName + "." + nameof(CapturedContactToSwingReleasesExpiredTargetWithoutLosingValidSupport),
                ["status"] = "running", ["stage"] = "preparation", ["rows"] = new JArray(),
                ["utc"] = DateTime.UtcNow.ToString("o"),
                ["boundary"] = "单脚 Lifecycle 至 GoalTarget 与正式向下脚底查询；不执行骨盆、腿 IK、完整预测器或整角色回放。"
            };
            GameObject goalRoot = null;
            CapturedFrame[] inputs = Array.Empty<CapturedFrame>();
            FrameResult[] oldResults = Array.Empty<FrameResult>();
            FrameResult[] newResults = Array.Empty<FrameResult>();
            try
            {
                string fixturePath = Path.Combine(EvidenceDirectory, "expired-contact-input.json");
                JObject fixture = JObject.Parse(File.ReadAllText(fixturePath, Encoding.UTF8));
                report["capture"] = fixture["capture"];
                report["baselineCommit"] = fixture["baselineCommit"];
                report["runtimeCommit"] = Git("rev-parse HEAD");
                report["sourceSha256"] = fixture["sourceSha256"];
                report["fixtureSha256"] = Sha256(fixturePath);
                report["testSourceBlob"] = Git("hash-object 3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Analysis/FootPlacement/CharacterFootCapturedContactTests.cs");
                report["sourceBlobs"] = new JObject(Directory.GetFiles(Path.Combine(Repository,
                    Path.GetDirectoryName(RuntimeSource)), "*.cs").OrderBy(x => x).Select(x =>
                    new JProperty(Path.GetFileNameWithoutExtension(x), Git("hash-object \"" + x + "\""))));
                var frames = (JArray)fixture["frames"];
                var columns = (JObject)fixture["columns"];
                report["input"] = new JObject
                {
                    ["columns"] = new JObject
                    {
                        ["main"] = columns["main"], ["probes"] = columns["probes"], ["envelope"] = columns["envelope"]
                    },
                    ["frames"] = new JArray(frames.Select(frame => new JObject
                    {
                        ["main"] = frame["main"], ["probes"] = frame["probes"], ["envelope"] = frame["envelope"]
                    }))
                };
                Assert.That(frames.Count, Is.EqualTo(100));
                Assert.That(SceneManager.GetActiveScene().path,
                    Is.EqualTo("Assets/Scenes/GameplayLab/GameplayLabFixed.unity"));
                Assert.That(EditorApplication.isPlaying, Is.False);
                Assert.That(EditorApplication.isCompiling, Is.False);
                Assert.That(EditorUtility.scriptCompilationFailed, Is.False, "当前源码未成功加载，不能执行旧程序集替代本次验证");

                var profile = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementProfile>(
                    AssetRoot + "FootPlacement/CorinFootPlacementProfile.asset");
                var calibration = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementRigCalibration>(
                    AssetRoot + "FootPlacement/CorinFootPlacementRigCalibration.asset");
                var definition = AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(
                    AssetRoot + "Rig/CorinAnimationRigDefinition.asset");
                var worldBinding = Object.FindObjectsByType<CharacterWorldAwarePresentationBinding>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Single(
                    x => x.gameObject.scene == SceneManager.GetActiveScene() &&
                         x.SelfColliderRoot.name == "Gameplay Lab Fixed Player");
                var binding = Object.FindObjectsByType<CharacterAnimationRigBinding>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Single(
                    x => x.gameObject.scene == SceneManager.GetActiveScene() &&
                         x.Animator.transform.IsChildOf(worldBinding.PresentationRoot));
                var rig = new CharacterFootPlacementPoseRig(calibration,
                    new CharacterAnimationRigPayload(definition), binding, worldBinding);
                var settings = profile.FootMotion.Build();
                report["profileRevision"] = profile.Revision;
                report["assets"] = new JObject(new[]
                {
                    AssetDatabase.GetAssetPath(profile), AssetDatabase.GetAssetPath(calibration),
                    AssetDatabase.GetAssetPath(definition), SceneManager.GetActiveScene().path
                }.Select(x => new JProperty(x, Sha256(Path.Combine(Application.dataPath, "..", x)))));
                report["assemblies"] = new JObject(new[]
                {
                    typeof(CharacterFootLifecycle).Assembly, typeof(AnimationFootMotionRuntimeSample).Assembly,
                    typeof(CharacterFutureBodyTranslationSample).Assembly, typeof(FixedString128Bytes).Assembly
                }.Distinct().Select(x => new JProperty(x.GetName().Name, Sha256(x.Location))));
                var supportSettings = profile.CurrentSupportQuery.Build();
                var world = new CharacterFootPlacementWorldQueryBackend(
                    binding.gameObject.scene.GetPhysicsScene(), rig, 16, 16, 16);
                var soleQuery = new CharacterFootSoleSupportQuery(world, in supportSettings);
                Physics.SyncTransforms();
                goalRoot = new GameObject("Captured IK function experiment")
                    { hideFlags = HideFlags.HideAndDontSave };

                Type historical = CompileHistoricalLanding((string)fixture["baselineCommit"]);
                var oldProject = (ProjectLanding)historical.GetMethod(
                    "ProjectAfterPrediction", StaticMethods).CreateDelegate(typeof(ProjectLanding));
                var oldAdvance = (AdvanceLanding)historical.GetMethod(
                    "Evaluate", StaticMethods).CreateDelegate(typeof(AdvanceLanding));
                var resolve = (ResolveLifecycle)typeof(CharacterFootLifecycle).GetMethod(
                    "Resolve", StaticMethods).CreateDelegate(typeof(ResolveLifecycle));
                inputs = new CapturedFrame[frames.Count];
                oldResults = new FrameResult[frames.Count];
                newResults = new FrameResult[frames.Count];
                for (int i = 0; i < inputs.Length; i++)
                    inputs[i] = new CapturedFrame(frames[i], columns, profile, rig.RightLegLength);
                var seed = Seed(inputs[0].Recorded);
                var oldTargetProbes = new CharacterFootSoleProbeBuffer();
                var oldOutputProbes = new CharacterFootSoleProbeBuffer();
                var newTargetProbes = new CharacterFootSoleProbeBuffer();
                var newOutputProbes = new CharacterFootSoleProbeBuffer();

                report["stage"] = "original-physics-query";
                for (int i = 0; i < inputs.Length; i++)
                {
                    CapturedFrame input = inputs[i];
                    var contacts = input.Animated.ResolveSoleContacts(input.Animated.AnklePosition, input.Animated.AnkleRotation);
                    input.Support = soleQuery.Query(input.Sequence, input.CompletionId, world.WorldRevision,
                        CharacterFootSide.Right, Vector3.up, input.Grounded, in contacts, input.InputProbes);
                    input.Queried = true;
                }
                int matchedProbes = CheckCapturedQueries(inputs);
                report["matchedProbes"] = matchedProbes;

                report["stage"] = "warmup";
                RunSequence(inputs, in seed, oldResults, goalRoot.transform, in soleQuery, world.WorldRevision,
                    oldProject, oldAdvance, resolve, oldTargetProbes, oldOutputProbes);
                RunSequence(inputs, in seed, newResults, goalRoot.transform, in soleQuery, world.WorldRevision,
                    null, null, resolve, newTargetProbes, newOutputProbes);
                Array.Clear(oldResults, 0, oldResults.Length);
                Array.Clear(newResults, 0, newResults.Length);
                report["stage"] = "historical-lifecycle";
                long allocationStart = GC.GetAllocatedBytesForCurrentThread();
                RunSequence(inputs, in seed, oldResults, goalRoot.transform, in soleQuery, world.WorldRevision,
                    oldProject, oldAdvance, resolve, oldTargetProbes, oldOutputProbes);
                long oldAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
                report["stage"] = "current-lifecycle";
                allocationStart = GC.GetAllocatedBytesForCurrentThread();
                RunSequence(inputs, in seed, newResults, goalRoot.transform, in soleQuery, world.WorldRevision,
                    null, null, resolve, newTargetProbes, newOutputProbes);
                long newAllocated = GC.GetAllocatedBytesForCurrentThread() - allocationStart;
                report["allocatedBytesInWarmedSequence"] = new JObject { ["old"] = oldAllocated, ["current"] = newAllocated };
                report["stage"] = "scenario-assertions";
                CheckScenario(inputs, oldResults, newResults, report);
                Assert.That(oldAllocated, Is.Zero, "历史版预热后的连续计算应为 0GC");
                Assert.That(newAllocated, Is.Zero, "当前版预热后的连续计算应为 0GC");
                report["status"] = "passed";
                report["stage"] = "completed";
            }
            catch (Exception error)
            {
                report["status"] = "failed";
                report["error"] = error.ToString();
                throw;
            }
            finally
            {
                Object.DestroyImmediate(goalRoot);
                report["queriedInputFrames"] = inputs.Count(x => x != null && x.Queried);
                report["historicalCompletedFrames"] = oldResults.Count(x => x.Completed);
                report["currentCompletedFrames"] = newResults.Count(x => x.Completed);
                report["rows"] = BuildRows(inputs, oldResults, newResults);
                string data = JsonConvert.SerializeObject(report, Formatting.None,
                    new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
                File.WriteAllText(Path.Combine(EvidenceDirectory, "expired-contact-unity.json"), data + "\n", new UTF8Encoding(false));
                string htmlPath = Path.Combine(EvidenceDirectory, "expired-contact.html");
                string html = File.ReadAllText(htmlPath, Encoding.UTF8);
                const string startMarker = "<script id=\"experiment-data\" type=\"application/json\">";
                int start = html.IndexOf(startMarker, StringComparison.Ordinal) + startMarker.Length;
                int end = html.IndexOf("</script>", start, StringComparison.Ordinal);
                string pendingPath = htmlPath + ".pending";
                File.WriteAllText(pendingPath, html.Substring(0, start) + data + html.Substring(end), new UTF8Encoding(false));
                File.Replace(pendingPath, htmlPath, null);
                TestContext.WriteLine("语义与逐帧结果：" + htmlPath);
            }
        }

        static int CheckCapturedQueries(CapturedFrame[] inputs)
        {
            int matched = 0;
            foreach (CapturedFrame input in inputs)
            {
                string label = "采样帧 " + input.Sequence;
                Assert.That(input.InputProbes.Count, Is.EqualTo(23), label);
                for (int i = 0; i < input.ExpectedProbes.Length; i++)
                {
                    var hit = input.InputProbes[i].Result;
                    Row sample = input.ExpectedProbes[i];
                    Assert.That(hit.Accepted, Is.EqualTo(sample.B("accepted")), label + " 原脚底探针 " + i);
                    if (hit.Accepted)
                    {
                        Assert.That(Vector3.Distance(hit.Point, sample.V("point")), Is.LessThan(0.0002f), label);
                        Assert.That(Vector3.Distance(hit.Normal, sample.V("normal")), Is.LessThan(0.0002f), label);
                    }
                    matched++;
                }
                Assert.That(input.Support.Available, Is.EqualTo(input.Recorded.B("foot/current-support/available")), label);
            }
            return matched;
        }

        static void RunSequence(CapturedFrame[] inputs, in CharacterFootLifecycleContext seed,
            FrameResult[] results, Transform root, in CharacterFootSoleSupportQuery query, ulong worldRevision,
            ProjectLanding historicalProject, AdvanceLanding historicalAdvance, ResolveLifecycle resolve,
            CharacterFootSoleProbeBuffer targetProbes, CharacterFootSoleProbeBuffer outputProbes)
        {
            CharacterFootLifecycleContext context = seed;
            for (int i = 0; i < inputs.Length; i++)
            {
                CapturedFrame input = inputs[i];
                root.SetPositionAndRotation(input.RootPosition, input.RootRotation);
                var snapshot = historicalProject == null
                    ? CharacterFootLandingRuntime.ProjectAfterPrediction(in context, in input.Step, in input.Prediction, in input.Settings)
                    : historicalProject(in context, in input.Step, in input.Prediction, in input.Settings);
                var frame = Frame(input, in input.Support, in snapshot, worldRevision);
                var evaluation = new CharacterFootStateEvaluation(CharacterFootSide.Right,
                    in input.Step, in input.Prediction, in frame, default, input.Grounded,
                    root, in query, targetProbes, outputProbes);
                CharacterFootLifecycle.Completion receipt;
                if (historicalAdvance == null)
                    CharacterFootLifecycle.Evaluate(ref context, in evaluation, out receipt);
                else
                {
                    historicalAdvance(ref context.Landing, in input.Step, in input.Prediction, in input.Settings);
                    resolve(ref context, in evaluation, out _, out receipt);
                }
                var output = receipt.Complete(ref context, false, out _);
                float weight = output.GoalTarget.PositionWeight;
                var contacts = input.Animated.ResolveSoleContacts(
                    Vector3.LerpUnclamped(input.Animated.AnklePosition, root.TransformPoint(output.GoalTarget.ComponentPosition), weight),
                    Quaternion.Slerp(input.Animated.AnkleRotation, root.rotation * output.GoalTarget.ComponentRotation,
                        output.GoalTarget.RotationWeight));
                var finalSupport = query.Query(input.Sequence, input.CompletionId, worldRevision, CharacterFootSide.Right,
                    Vector3.up, input.Grounded, in contacts, input.FinalProbes);
                results[i] = new FrameResult
                {
                    Completed = true, Prepared = frame.PreparedPlantActive, Plant = snapshot.PlantTargetState,
                    PlantEvent = snapshot.PlantTarget.LandingEventIdentity, SwingAccepted = frame.SwingMotion.Accepted,
                    Output = output, State = context.Discrete.State, HasAnchor = context.Contact.HasContact,
                    FinalSupportAvailable = finalSupport.Available, FinalAcceptedSamples = finalSupport.AcceptedSampleCount,
                    FinalRejectReason = finalSupport.RejectReason,
                    Penetration = finalSupport.TryResolveHeightConstraint(out float displacement, out _) ? Mathf.Max(0f, displacement) : 0f
                };
            }
        }

        static void CheckScenario(CapturedFrame[] inputs, FrameResult[] oldResults, FrameResult[] newResults, JObject report)
        {
            int originalMisses = 0, repairedMisses = 0, contactFrames = 0, approachFrames = 0, acceptedSwingFrames = 0;
            float maxUnchangedDifference = 0f, maxOutputPenetration = 0f;
            int finalQueryNoHits = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                CapturedFrame input = inputs[i];
                Row r = input.Recorded;
                FrameResult old = oldResults[i], current = newResults[i];
                string label = "采样帧 " + input.Sequence;
                float oldWeight = old.Output.GoalTarget.PositionWeight, newWeight = current.Output.GoalTarget.PositionWeight;
                Assert.That(oldWeight, Is.EqualTo(input.RecordedWeight), label + " 历史版必须重现原采样权重");
                Assert.That(newWeight, Is.EqualTo(1f), label + " 有效目标应保持作者权重");
                Assert.That(current.Penetration, Is.LessThan(0.0002f), label + " 新输出命中的脚底采样点不得穿入地面");
                Assert.That(current.FinalRejectReason, Is.EqualTo(CharacterFootCurrentSupportRejectReason.None)
                    .Or.EqualTo(CharacterFootCurrentSupportRejectReason.NoSupport), label + " 容量不足等查询失败不能记为无穿透");
                Assert.That(old.HasAnchor || current.HasAnchor, Is.False, label + " 不能进入未录制锚点的求解");
                Assert.That(old.Plant, Is.EqualTo((CharacterFootPlantTargetState)r.I("foot/plant-target-state")), label);
                Assert.That(old.Prepared, Is.EqualTo(r.B("foot/approach-plant-target-prepared")), label);
                maxOutputPenetration = Mathf.Max(maxOutputPenetration, current.Penetration);
                if (!current.FinalSupportAvailable) finalQueryNoHits++;
                if (input.Sequence < 34713UL)
                {
                    Assert.That(current.Plant, Is.EqualTo(old.Plant), label);
                    float difference = Vector3.Distance(old.Output.GoalTarget.EffectiveSole, current.Output.GoalTarget.EffectiveSole);
                    maxUnchangedDifference = Mathf.Max(maxUnchangedDifference, difference);
                    Assert.That(difference, Is.LessThan(0.00001f), label + " 正常阶段两版输出不得改变");
                }
                if (input.Step.Events.InApproachContactToLanding)
                {
                    approachFrames++;
                    Assert.That(current.Prepared, Is.True, label);
                    Assert.That(current.PlantEvent, Is.EqualTo(input.Step.Events.NextLanding.Identity), label);
                }
                if (input.Sequence >= 34689UL && input.Sequence <= 34712UL)
                {
                    contactFrames++;
                    Assert.That(current.Prepared, Is.True, label + " 真实接触期间保留目标");
                }
                if (input.Step.Contact == 0f && !input.Step.Events.InApproachContactToLanding)
                {
                    acceptedSwingFrames++;
                    Assert.That(current.Prepared, Is.False, label + " 已释放接触不能继续占位");
                    Assert.That(current.SwingAccepted, Is.True, label);
                }
                if (input.RecordedWeight == 0f)
                {
                    originalMisses++;
                    if (newWeight == 1f) repairedMisses++;
                }
            }
            report["metrics"] = JObject.FromObject(new
            {
                originalMisses, repairedMisses, contactFrames, approachFrames, acceptedSwingFrames,
                maxUnchangedDifference, maxOutputPenetration, finalQueryNoHits
            });
            Assert.That(originalMisses, Is.EqualTo(27));
            Assert.That(repairedMisses, Is.EqualTo(originalMisses));
            Assert.That(contactFrames, Is.EqualTo(24));
            Assert.That(approachFrames, Is.EqualTo(20));
            Assert.That(acceptedSwingFrames, Is.EqualTo(53));
        }

        static JArray BuildRows(CapturedFrame[] inputs, FrameResult[] oldResults, FrameResult[] newResults)
        {
            var rows = new JArray();
            for (int i = 0; i < inputs.Length; i++)
            {
                if (!oldResults[i].Completed || !newResults[i].Completed) continue;
                CapturedFrame input = inputs[i];
                FrameResult old = oldResults[i], current = newResults[i];
                rows.Add(new JObject
                {
                    ["frame"] = input.Sequence, ["dt"] = input.DeltaSeconds,
                    ["contact"] = input.Step.Contact, ["phase"] = input.Step.Events.Phase.ToString(),
                    ["inputWeight"] = input.Weight, ["currentEvent"] = input.Step.Events.CurrentContact.Identity.ToString(),
                    ["nextEvent"] = input.Step.Events.NextLanding.Identity.ToString(),
                    ["oldPrepared"] = old.Prepared, ["newPrepared"] = current.Prepared,
                    ["support"] = input.Support.Available, ["swingAccepted"] = current.SwingAccepted,
                    ["recordedWeight"] = input.RecordedWeight,
                    ["oldWeight"] = old.Output.GoalTarget.PositionWeight, ["newWeight"] = current.Output.GoalTarget.PositionWeight,
                    ["sourceSole"] = Vec((input.Animated.HeelPosition + input.Animated.ToePosition) * 0.5f),
                    ["oldSole"] = Vec(old.Output.GoalTarget.EffectiveSole), ["newSole"] = Vec(current.Output.GoalTarget.EffectiveSole),
                    ["newPenetration"] = current.FinalSupportAvailable ? (JToken)current.Penetration : JValue.CreateNull(),
                    ["finalSupportAvailable"] = current.FinalSupportAvailable,
                    ["finalAcceptedSamples"] = current.FinalAcceptedSamples,
                    ["finalRejectReason"] = current.FinalRejectReason.ToString(),
                    ["oldState"] = old.State.ToString(), ["newState"] = current.State.ToString()
                });
            }
            return rows;
        }

        sealed class CapturedFrame
        {
            internal CapturedFrame(JToken captured, JObject columns, CharacterFootPlacementProfile profile, float legLength)
            {
                Recorded = Row.Main(captured, columns);
                Row r = Recorded;
                ExpectedProbes = Row.Table(captured, columns, "probes").OrderBy(x => x.I("sample-index")).ToArray();
                Assert.That(ExpectedProbes.Length, Is.EqualTo(23));
                for (int i = 0; i < ExpectedProbes.Length; i++) Assert.That(ExpectedProbes[i].I("sample-index"), Is.EqualTo(i));
                Sequence = r.U("foot/resolved/core/frame-sequence");
                CompletionId = r.U("foot/resolved/core/completion-identity");
                RootPosition = r.V("physical-body/pose-root-world-position");
                RootRotation = r.Q("physical-body/pose-root-world-rotation");
                Animated = AnimatedPose(r, ExpectedProbes);
                Step = CharacterFootCapturedContactTests.Step(r);
                Assert.That(Step.Events.CurrentContact.Identity, Is.EqualTo(r.U("formal-input/events/current-contact/identity")));
                Assert.That(Step.Events.NextLanding.Identity, Is.EqualTo(r.U("formal-input/events/next-landing/identity")));
                Path = PathInput(r, captured, columns);
                Prediction = CharacterFootCapturedContactTests.Prediction(r);
                Settings = profile.FootMotion.Build();
                Grounded = r.B("input/grounded");
                Weight = r.F("foot/foot-motion/lifecycle/formal-foot-placement-weight");
                Assert.That(Weight, Is.EqualTo(1f));
                DeltaSeconds = r.F("input/presentation-delta-seconds");
                RecordedWeight = r.F("foot/resolved/core/position-weight");
                RigId = new FixedString64Bytes(r.S("foot/resolved/core/rig-id"));
                RigRevision = new FixedString64Bytes(r.S("foot/resolved/core/rig-revision"));
                SourceLineage = new FixedString128Bytes(r.S("input/foot-step-observation/source-identity"));
                ProfileRevision = new FixedString128Bytes(profile.Revision);
                LegLength = legLength;
            }

            internal readonly Row Recorded;
            internal readonly Row[] ExpectedProbes;
            internal readonly ulong Sequence, CompletionId;
            internal readonly Vector3 RootPosition;
            internal readonly Quaternion RootRotation;
            internal readonly CharacterFootPlacementAnimatedFootPose Animated;
            internal readonly AnimationFootMotionRuntimeSample Step;
            internal readonly CharacterFootGroundPathResult Path;
            internal readonly CharacterFootLandingPredictionResult Prediction;
            internal readonly CharacterFootMotionSettings Settings;
            internal readonly bool Grounded;
            internal readonly float Weight, DeltaSeconds, RecordedWeight, LegLength;
            internal readonly FixedString64Bytes RigId, RigRevision;
            internal readonly FixedString128Bytes SourceLineage, ProfileRevision;
            internal readonly CharacterFootSoleProbeBuffer InputProbes = new CharacterFootSoleProbeBuffer();
            internal readonly CharacterFootSoleProbeBuffer FinalProbes = new CharacterFootSoleProbeBuffer();
            internal CharacterFootCurrentSupportObservation Support;
            internal bool Queried;
        }

        struct FrameResult
        {
            internal bool Completed, Prepared, SwingAccepted, HasAnchor, FinalSupportAvailable;
            internal CharacterFootPlantTargetState Plant;
            internal ulong PlantEvent;
            internal CharacterResolvedFootResult Output;
            internal CharacterFootConstraintState State;
            internal CharacterFootCurrentSupportRejectReason FinalRejectReason;
            internal int FinalAcceptedSamples;
            internal float Penetration;
        }

        static string Sha256(string path)
        {
            using var algorithm = System.Security.Cryptography.SHA256.Create();
            using var stream = File.OpenRead(path);
            return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        static CharacterFootLifecycleContext Seed(Row r)
        {
            var context = new CharacterFootLifecycleContext();
            var last = PathLanding(r, "last-landing", "last-future-body-translation-source-identity");
            var next = PathLanding(r, "next-swing-landing", "next-swing-future-body-translation-source-identity");
            context.Landing.LastLanding = Fact(in last);
            context.Landing.NextSwingLanding = Fact(in next);
            context.Landing.NextSwingReferencePoint = next.Point;
            context.Landing.TrackedEventIdentity = next.LandingEventIdentity;
            context.Landing.NextTrackingState = CharacterFootNextLandingTrackingState.Tracking;
            context.Landing.VerifyPlantTarget(in context.Landing.LastLanding);
            context.ContactTransition.LatestReleasedContactEventIdentity =
                r.U("foot/foot-motion/lifecycle/previous-latest-released-contact-event-identity");
            return context;
        }

        static CharacterFootLandingFact Fact(in CharacterFootGroundPathLanding landing)
        {
            var prediction = new CharacterFootLandingPredictionResult(CharacterFootSide.Right,
                CharacterFootLandingPredictionState.Accepted, 0, CharacterFootLandingStepSource.FormalNextLanding,
                landing.LandingEventIdentity, landing.TrajectoryGeneration, 1f, 0f, default, false,
                landing.FutureBodyTranslationSourceIdentity, default, default, default, default, default,
                new CharacterFootLandingSupport(landing.SurfaceIdentity, landing.Point, landing.Normal, 0f), default);
            return CharacterFootLandingFact.Create(landing.LandingEventIdentity, in prediction);
        }

        static AnimationFootMotionRuntimeSample Step(Row r)
        {
            var events = new AnimationFootMotionEventFrame(
                Occurrence(r, "current-contact"), Occurrence(r, "next-landing"),
                (AnimationFootMotionEventPhase)r.I("formal-input/events/phase"),
                r.F("formal-input/time-to-landing-seconds"), r.F("foot/foot-motion/core/progress"),
                r.F("formal-input/approach-contact-to-landing-progress")).Bind(
                    r.U("formal-input/source-sample-identity"),
                    r.U("formal-input/contribution-continuity-identity"), CharacterFootSide.Right);
            return new AnimationFootMotionRuntimeSample(r.F("formal-input/foot-height"),
                r.F("formal-input/toe-height"), r.F("formal-input/toe-speed"),
                r.F("formal-input/position-error"), r.F("formal-input/rotation-error"),
                r.F("formal-input/contact"), (AnimationFootStepObservationLockMode)r.I("formal-input/lock-mode"),
                r.F("formal-input/lock-weight"), r.F("formal-input/support"), in events);
        }

        static AnimationFootMotionEventOccurrence Occurrence(Row r, string name)
        {
            string p = "formal-input/events/" + name + "/";
            // 绝对 clip 相位未采样；此调用链只消费已录制的事件身份与剩余时间。
            return new AnimationFootMotionEventOccurrence(r.I(p + "ordinal"), r.I(p + "landing-cycle"),
                0f, r.F(p + "distance"), r.V(p + "root-local-landing"));
        }

        static CharacterFootLandingPredictionResult Prediction(Row r)
        {
            bool accepted = r.B("foot/accepted");
            CharacterFootLandingSupport support = accepted
                ? new CharacterFootLandingSupport(r.I("foot/surface-identity"), r.V("foot/landing-point"),
                    r.V("foot/landing-normal"), r.F("foot/query-distance"))
                : default;
            Vector3 translation = r.V("foot/future-body-relative-translation");
            Vector3 velocity = r.V("foot/future-body-translation-velocity");
            var future = new CharacterFutureBodyTranslationSample(r.F("foot/time-to-landing-seconds"),
                translation.x, translation.y, translation.z, velocity.x, velocity.y, velocity.z);
            return new CharacterFootLandingPredictionResult(CharacterFootSide.Right,
                (CharacterFootLandingPredictionState)r.I("foot/state"),
                (CharacterFootLandingPredictionRejectReason)r.I("foot/reject-reason"),
                (CharacterFootLandingStepSource)r.I("foot/step-source"),
                r.U("foot/landing-event-identity"), r.U("foot/trajectory-generation"),
                r.F("foot/landing-confidence"), r.F("foot/time-to-landing-seconds"),
                r.V("foot/root-local-landing"), r.B("foot/future-body-translation-available"),
                r.S("input/prediction-motion-source-identity"), in future,
                r.V("foot/current-animated-sole"), r.V("foot/raw-landing-candidate"), default, default,
                in support, default);
        }

        static CharacterFootPlacementAnimatedFootPose AnimatedPose(Row r, Row[] probes)
        {
            Vector3 rootPosition = r.V("physical-body/pose-root-world-position");
            Quaternion rootRotation = r.Q("physical-body/pose-root-world-rotation");
            Quaternion ankle = r.Q("foot/source-ankle-rotation");
            Quaternion local = r.Q("foot/resolved/core/source-sole-frame-local-rotation");
            Quaternion sole = ankle * local;
            var pose = new CharacterFootPlacementAnimatedFootPose(
                rootPosition + rootRotation * r.V("leg/leg-pose/original-hip"),
                rootPosition + rootRotation * r.V("leg/leg-pose/original-knee"),
                r.V("foot/source-ankle-position"), ankle, r.V("foot/source-toe-position"),
                Quaternion.identity, r.V("foot/source-heel-position"),
                sole * Vector3.forward, sole * Vector3.up, sole, local);
            var soleSamples = new FixedList512Bytes<Vector3>();
            foreach (Row probe in probes) soleSamples.Add(probe.V("probe-position"));
            pose.SoleSamples = soleSamples;
            return pose;
        }

        static CharacterFootGroundPathLanding PathLanding(Row r, string name, string source) =>
            new CharacterFootGroundPathLanding(r.U("foot/ground-path/" + name + "-event-identity"),
                r.U("foot/ground-path/trajectory-generation"), r.S("foot/ground-path/" + source),
                r.I("foot/ground-path/" + name + "-surface-identity"),
                r.V("foot/ground-path/" + name), r.V("foot/ground-path/" + name + "-normal"));

        static CharacterFootGroundPathResult PathInput(Row r, JToken captured, JObject columns)
        {
            var page = new CharacterFootGroundPathPage(64);
            if (r.I("foot/ground-path/state") == (int)CharacterFootGroundPathState.Rejected)
            {
                page.SetRejected((CharacterFootGroundPathRejectReason)r.I("foot/ground-path/reject-reason"),
                    r.B("foot/ground-path/query-executed-this-frame"),
                    r.I("foot/ground-path/segment-count"), default, default);
                return new CharacterFootGroundPathResult(page, true);
            }
            var last = PathLanding(r, "last-landing", "last-future-body-translation-source-identity");
            var next = PathLanding(r, "next-swing-landing", "next-swing-future-body-translation-source-identity");
            var key = CharacterFootGroundPathInputBuilder.BuildKey(CharacterFootSide.Right,
                in last, in next, r.U("foot/ground-path/authority-tick"), Vector3.up, string.Empty);
            const string q = "foot/ground-path/query/";
            var query = new CharacterFootGroundPathQueryRequest(CharacterFootSide.Right,
                r.V(q + "axis-start"), r.V(q + "axis-end"), r.F(q + "radius"),
                r.F(q + "maximum-axis-segment-length"), r.V(q + "direction"),
                r.F(q + "maximum-distance"), r.I(q + "layer-mask"),
                r.I(q + "segment-hit-capacity"), r.I(q + "contact-capacity"));
            var input = new CharacterFootGroundPathInput(r.U("foot/ground-path/input-identity"),
                in key, last.Point, next.Point, last.Normal, next.Normal,
                last.SurfaceIdentity, next.SurfaceIdentity, r.V("foot/ground-path/component-up"),
                r.F("foot/ground-path/maximum-reachable-vertical-edge"), in query);
            Assert.That(page.Contacts.SurfaceCoverage.Begin(input.Query,
                r.U("foot/ground-path/surface-coverage/world-revision")), Is.True);
            foreach (Row surface in Row.Table(captured, columns, "surfaces"))
                Assert.That(page.Contacts.SurfaceCoverage.TryAdd(surface.I("surface-identity"),
                    surface.I("face-identity"), surface.V2("start"), surface.V2("end")), Is.True);
            page.Contacts.SurfaceCoverage.Complete();
            foreach (Row contact in Row.Table(captured, columns, "contacts"))
            {
                var value = new CharacterFootGroundContact(contact.I("segment-index"), contact.I("surface-identity"),
                    contact.U("candidate-identity"), contact.V("position"), contact.V("normal"), contact.F("query-distance"));
                Assert.That(page.Contacts.TryAdd(in value), Is.True);
            }
            foreach (Row vertex in Row.Table(captured, columns, "envelope"))
                Assert.That(page.Envelope.TryPush(vertex.V("position")), Is.True);
            page.SetAccepted(r.I("foot/ground-path/segment-count"), in input);
            return new CharacterFootGroundPathResult(page, true);
        }

        static CharacterFootStateFrame Frame(CapturedFrame input,
            in CharacterFootCurrentSupportObservation support, in CharacterFootLandingSnapshot landing, ulong worldRevision)
        {
            var swing = CharacterFootSwingMotionBuilder.Build(in input.Animated, in input.Step, input.Weight,
                Vector3.up, in input.Path, true, input.Step.FootHeight, landing.NextSwingPredictionError);
            var request = new CharacterFootLockRequest(in input.Step);
            bool prepared = landing.PlantTargetState == CharacterFootPlantTargetState.Tracking;
            CharacterFootGroundPathLanding contactLanding = default;
            bool hasContactLanding = request.RequestsLock &&
                (landing.TryResolveVerifiedLanding(request.EventIdentity, out contactLanding) ||
                 CharacterFootLandingRuntime.TryResolveCurrentContactCandidate(in input.Step, in input.Prediction, out contactLanding));
            return new CharacterFootStateFrame(input.Sequence, input.CompletionId,
                input.RigId, input.RigRevision, CharacterFootSide.Right,
                in input.Animated, input.Animated.HipPosition, input.LegLength, in swing, in input.Path,
                hasContactLanding, in contactLanding, prepared, prepared ? landing.PlantTarget : default,
                in support, in request, input.Step.Support, request.EventIdentity,
                CharacterFootGoalOwnershipLossReason.None, input.Weight, Vector3.up, input.DeltaSeconds,
                input.SourceLineage, input.ProfileRevision, worldRevision, in input.Settings);
        }

        static Type CompileHistoricalLanding(string commit)
        {
            string source = Git("show " + commit + ":" + RuntimeSource);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/FootContactBaseline"));
            Directory.CreateDirectory(directory);
            string sourcePath = Path.Combine(directory, "CharacterFootLandingRuntime.cs");
            string assemblyPath = Path.Combine(directory, "ThirdPersonClient.Editor.dll");
            string responsePath = Path.Combine(directory, "compile.rsp");
            File.WriteAllText(sourcePath, source, new UTF8Encoding(false));
            var references = new[]
            {
                typeof(object).Assembly.Location,
                typeof(Vector3).Assembly.Location,
                typeof(CharacterFootLifecycleContext).Assembly.Location,
                typeof(AnimationFootMotionRuntimeSample).Assembly.Location,
                typeof(CharacterFutureBodyTranslationSample).Assembly.Location,
                typeof(FixedString128Bytes).Assembly.Location,
                Assembly.Load("netstandard").Location
            };
            var arguments = new List<string>
            {
                "-nostdlib+", "-langversion:latest", "-target:library",
                "-out:\"" + assemblyPath + "\"", "\"" + sourcePath + "\""
            };
            arguments.AddRange(references.Distinct().Select(x => "-r:\"" + x + "\""));
            File.WriteAllLines(responsePath, arguments, new UTF8Encoding(false));
            string contents = EditorApplication.applicationContentsPath;
            Run(Path.Combine(contents, "NetCoreRuntime/dotnet.exe"),
                "\"" + Path.Combine(contents, "DotNetSdkRoslyn/csc.dll") + "\" /noconfig \"@" + responsePath + "\"");
            return Assembly.Load(File.ReadAllBytes(assemblyPath)).GetType(
                "ThirdPersonCharacter.Pipeline.Presentation.CharacterFootLandingRuntime", true);
        }

        static string Git(string arguments) => Run("git", arguments);

        static string Run(string executable, string arguments)
        {
            using var process = Process.Start(new ProcessStartInfo(executable, arguments)
            {
                WorkingDirectory = Repository, UseShellExecute = false,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            });
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.That(process.ExitCode, Is.Zero, output + error);
            return output.TrimEnd();
        }

        static JArray Vec(Vector3 value) => new JArray(value.x, value.y, value.z);

        sealed class Row
        {
            readonly Dictionary<string, string> values;
            Row(JToken values, JToken columns) => this.values = columns.Select((x, i) =>
                new KeyValuePair<string, string>((string)x, (string)values[i])).ToDictionary(x => x.Key, x => x.Value);
            internal static Row Main(JToken frame, JObject columns) => new Row(frame["main"], columns["main"]);
            internal static IEnumerable<Row> Table(JToken frame, JObject columns, string name) =>
                frame[name].Select(x => new Row(x, columns[name]));
            internal string S(string key) => values[key];
            internal float F(string key) => float.Parse(S(key), CultureInfo.InvariantCulture);
            internal int I(string key) => int.Parse(S(key), CultureInfo.InvariantCulture);
            internal ulong U(string key) => ulong.Parse(S(key), CultureInfo.InvariantCulture);
            internal bool B(string key) => bool.Parse(S(key));
            internal Vector3 V(string key) => new Vector3(F(key + ".x"), F(key + ".y"), F(key + ".z"));
            internal Vector2 V2(string key) => new Vector2(F(key + ".x"), F(key + ".y"));
            internal Quaternion Q(string key) => new Quaternion(F(key + ".x"), F(key + ".y"), F(key + ".z"), F(key + ".w"));
        }
    }
}
