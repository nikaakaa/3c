/*
目的：用同一段 Releasing 跨阶输入，比较水平冻结引入前后的真实函数，复现停脚、腿长冲突与恢复前移。
输入：12570 帧的已完成状态初始化一次，连续执行左脚 12571～12585；保留作者权重、旋转、时间间隔与路径。
链路：正式目标选择 → 插值 → 脚底查询 → 输出约束 → 历史回写 → 单脚 Goal；历史源码来自 413e931de。
检查：原查询的场景一致性、当前轨迹重现、约束前后净空、位移、固定采样髋姿态下的腿长占比和计算分配。
边界：不执行骨盆与腿求解；23 点向下查询不是鞋网格连续扫掠。4107/4127/4131 仅作原采样历史对照。
说明：docs/diagnostics/foot-placement/ik-tests/release-clearance.html。测试通过只表示复现与比较成立，不表示修复通过。
*/
using System;
using System.Collections.Generic;
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
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed partial class CharacterFootCapturedContactTests
    {
        const string ReleaseTestSource = "3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Analysis/FootPlacement/CharacterFootCapturedReleaseTests.cs";
        const string ContactTestSource = "3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Analysis/FootPlacement/CharacterFootCapturedContactTests.cs";
        static readonly string[] ReleaseSources =
        {
            "CharacterFootLifecycle.cs", "CharacterFootInterpolationRuntime.cs", "CharacterFootHardConstraintResolver.cs",
            "CharacterFootLandingRuntime.cs"
        };

        [Test]
        public void CapturedReleaseComparesHistoricalClearanceAndContinuousOutput()
        {
            const string scenePath = "Assets/Scenes/GameplayLab/GameplayLabFixed.unity";
            Scene previousScene = SceneManager.GetActiveScene();
            Scene fixtureScene = SceneManager.GetSceneByPath(scenePath);
            bool openedFixture = !fixtureScene.isLoaded;
            if (openedFixture)
                fixtureScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(fixtureScene);
            var report = new JObject
            {
                ["status"] = "running", ["stage"] = "compile", ["utc"] = DateTime.UtcNow.ToString("o"),
                ["test"] = GetType().FullName + "." + nameof(CapturedReleaseComparesHistoricalClearanceAndContinuousOutput),
                ["scope"] = "连续单脚 Lifecycle 至 Goal，固定采样髋姿态，不执行骨盆、腿 IK 或整角色回放"
            };
            try
            {
                string inputPath = Path.Combine(EvidenceDirectory, "release-clearance-input.json");
                JObject fixture = JObject.Parse(File.ReadAllText(inputPath, Encoding.UTF8));
                report["capture"] = fixture["capture"];
                report["fixtureSha256"] = Sha256(inputPath);
                report["sourceSha256"] = fixture["sourceSha256"];
                report["runtimeCommit"] = Git("rev-parse HEAD");
                report["historicalRegressionCoverage"] = fixture["historicalRegressionCoverage"];
                report["historicalRegressionRecords"] = fixture["historicalRegressionRecords"];
                report["testSourceBlobs"] = new JObject
                {
                    ["release"] = Git("hash-object " + ReleaseTestSource),
                    ["capture"] = Git("hash-object " + ContactTestSource)
                };
                foreach (string variant in new[] { "historical", "current" })
                {
                    string commit = variant == "historical" ? (string)fixture["baselineCommit"] : null;
                    Type compiled = CompileReleaseVariant(variant, commit, out JObject sourceBlobs);
                    report["stage"] = variant;
                    var result = (JObject)compiled.GetMethod(nameof(RunReleaseVariant), BindingFlags.Public | BindingFlags.Static)
                        .Invoke(null, new object[] { inputPath });
                    result["sourceBlobs"] = sourceBlobs;
                    result["commit"] = commit ?? (string)report["runtimeCommit"];
                    report[variant] = result;
                }
                report["stage"] = "comparison";
                var current = (JObject)report["current"];
                var historical = (JObject)report["historical"];
                Assert.That((int)current["frames"], Is.EqualTo(15));
                Assert.That((int)historical["frames"], Is.EqualTo(15));
                Assert.That((float)current["maximumRecordedSoleError"], Is.LessThan(0.0005f), "当前源码必须重现原轨迹，才能解释冻结影响");
                Assert.That((float)current["maximumRecordedExtensionError"], Is.LessThan(0.001f));
                Assert.That((int)current["heldFrames"], Is.EqualTo(3));
                Assert.That((int)historical["heldFrames"], Is.Zero);
                Assert.That((float)historical["maximumClamp"], Is.GreaterThan(0.05f));
                Assert.That((float)current["maximumClamp"], Is.Zero);
                Assert.That((float)historical["maximumExtension"], Is.LessThan(1f));
                Assert.That((float)current["maximumExtension"], Is.GreaterThan(1f));
                Assert.That((float)current["resumeHorizontalStep"], Is.GreaterThan(0.20f));
                Assert.That((float)current["maximumFinalPenetration"], Is.LessThan(0.0005f));
                Assert.That((float)historical["maximumFinalPenetration"], Is.LessThan(0.0005f));
                Assert.That((long)current["allocatedBytes"], Is.Zero);
                Assert.That((long)historical["allocatedBytes"], Is.Zero);
                JObject safeHeldFrame = (JObject)((JArray)current["rows"]).Single(x => (ulong)x["frame"] == 12574);
                Assert.That((bool)safeHeldFrame["held"], Is.True);
                Assert.That((float)safeHeldFrame["rawPenetration"], Is.LessThanOrEqualTo(0f),
                    "此帧候选脚掌已有向下查询净空，当前规则却仍然冻结");
                report["status"] = "passed";
                report["verdict"] = "当前版消除了本窗口旧版的末端硬抬，但造成三帧停脚、腿长冲突和恢复跳步；两版都不能作为完整修复。";
            }
            catch (Exception error)
            {
                report["status"] = "failed";
                report["failure"] = error.ToString();
                throw;
            }
            finally
            {
                string json = report.ToString(Formatting.None);
                File.WriteAllText(Path.Combine(EvidenceDirectory, "release-clearance-result.json"), json, new UTF8Encoding(false));
                string htmlPath = Path.Combine(EvidenceDirectory, "release-clearance.html");
                string html = File.ReadAllText(htmlPath, Encoding.UTF8);
                html = System.Text.RegularExpressions.Regex.Replace(html,
                    "(<script id=\"experiment-data\" type=\"application/json\">)[\\s\\S]*?(</script>)",
                    match => match.Groups[1].Value + json.Replace("</", "<\\/") + match.Groups[2].Value);
                File.WriteAllText(htmlPath, html, new UTF8Encoding(false));
                SceneManager.SetActiveScene(previousScene);
                if (openedFixture)
                    EditorSceneManager.CloseScene(fixtureScene, true);
            }
        }

        static Type CompileReleaseVariant(string variant, string commit, out JObject blobs)
        {
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/FootReleaseComparison", variant));
            Directory.CreateDirectory(directory);
            string runtimeDirectory = Path.GetDirectoryName(RuntimeSource);
            var sources = new List<string> { Path.Combine(Repository, ContactTestSource), Path.Combine(Repository, ReleaseTestSource) };
            blobs = new JObject();
            foreach (string name in ReleaseSources)
            {
                string relative = (runtimeDirectory + "/" + name).Replace('\\', '/');
                string source = commit == null ? File.ReadAllText(Path.Combine(Repository, relative), Encoding.UTF8)
                    : Git("show " + commit + ":" + relative);
                string path = Path.Combine(directory, name);
                File.WriteAllText(path, source, new UTF8Encoding(false));
                sources.Add(path);
                blobs[name] = commit == null ? Git("hash-object " + relative) : Git("rev-parse " + commit + ":" + relative);
            }
            string assembly = Path.Combine(directory, "ThirdPersonClient.Editor.dll");
            var arguments = new List<string> { "-nostdlib+", "-langversion:latest", "-target:library", "-utf8output", "-nowarn:0436", "-out:\"" + assembly + "\"" };
            arguments.AddRange(sources.Select(x => "\"" + x + "\""));
            var references = new[]
            {
                typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(System.Diagnostics.Process).Assembly,
                typeof(Vector3).Assembly, typeof(Physics).Assembly, typeof(Animator).Assembly, typeof(AssetDatabase).Assembly,
                typeof(CharacterFootLifecycleContext).Assembly, typeof(AnimationFootMotionRuntimeSample).Assembly,
                typeof(CharacterFutureBodyTranslationSample).Assembly, typeof(FixedString128Bytes).Assembly,
                typeof(JObject).Assembly, typeof(TestAttribute).Assembly, Assembly.Load("netstandard")
            };
            arguments.AddRange(references.Select(x => x.Location).Distinct().Select(x => "-r:\"" + x + "\""));
            string response = Path.Combine(directory, "compile.rsp");
            File.WriteAllLines(response, arguments, new UTF8Encoding(false));
            string contents = EditorApplication.applicationContentsPath;
            Run(Path.Combine(contents, "NetCoreRuntime/dotnet.exe"),
                "\"" + Path.Combine(contents, "DotNetSdkRoslyn/csc.dll") + "\" /noconfig \"@" + response + "\"");
            return Assembly.Load(File.ReadAllBytes(assembly)).GetType(typeof(CharacterFootCapturedContactTests).FullName, true);
        }

        public static JObject RunReleaseVariant(string inputPath)
        {
            Assert.That(EditorApplication.isPlaying || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed, Is.False);
            var fixture = JObject.Parse(File.ReadAllText(inputPath, Encoding.UTF8));
            var profile = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementProfile>(AssetRoot + "FootPlacement/CorinFootPlacementProfile.asset");
            var calibration = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementRigCalibration>(AssetRoot + "FootPlacement/CorinFootPlacementRigCalibration.asset");
            var definition = AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(AssetRoot + "Rig/CorinAnimationRigDefinition.asset");
            var worldBinding = Object.FindObjectsByType<CharacterWorldAwarePresentationBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(x => x.gameObject.scene == SceneManager.GetActiveScene() && x.SelfColliderRoot.name == "Gameplay Lab Fixed Player");
            var binding = Object.FindObjectsByType<CharacterAnimationRigBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(x => x.gameObject.scene == SceneManager.GetActiveScene() && x.Animator.transform.IsChildOf(worldBinding.PresentationRoot));
            var rig = new CharacterFootPlacementPoseRig(calibration, new CharacterAnimationRigPayload(definition), binding, worldBinding);
            var supportSettings = profile.CurrentSupportQuery.Build();
            var world = new CharacterFootPlacementWorldQueryBackend(binding.gameObject.scene.GetPhysicsScene(), rig, 16, 16, 16);
            var query = new CharacterFootSoleSupportQuery(world, in supportSettings);
            Physics.SyncTransforms();
            var inputs = ((JArray)fixture["frames"]).Select(x => new CapturedFrame(x, (JObject)fixture["columns"], profile,
                rig.LeftLegLength, CharacterFootSide.Left)).ToArray();
            foreach (CapturedFrame input in inputs)
            {
                var contacts = input.Animated.ResolveSoleContacts(input.Animated.AnklePosition, input.Animated.AnkleRotation);
                input.Support = query.Query(input.Sequence, input.CompletionId, world.WorldRevision, input.Side,
                    Vector3.up, input.Grounded, in contacts, input.InputProbes);
            }
            int matched = CheckCapturedQueries(inputs);
            CharacterFootLifecycleContext seed = ReleaseSeed(inputs[0], world.WorldRevision);
            var results = new ReleaseResult[inputs.Length - 1];
            var targetProbes = new CharacterFootSoleProbeBuffer();
            var outputProbes = new CharacterFootSoleProbeBuffer();
            var rawProbes = new CharacterFootSoleProbeBuffer();
            var root = new GameObject("Captured release comparison") { hideFlags = HideFlags.HideAndDontSave };
            long allocated;
            try
            {
                RunReleaseSequence(inputs, in seed, results, root.transform, in query, world.WorldRevision, targetProbes, outputProbes, rawProbes);
                long start = GC.GetAllocatedBytesForCurrentThread();
                RunReleaseSequence(inputs, in seed, results, root.transform, in query, world.WorldRevision, targetProbes, outputProbes, rawProbes);
                allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            }
            finally { Object.DestroyImmediate(root); }
            var rows = new JArray();
            float maxSoleError = 0f, maxExtensionError = 0f, maxExtension = 0f, maxPenetration = 0f, maxClamp = 0f, resumeStep = 0f;
            int held = 0;
            Vector3 previous = seed.Interpolation.PreviousResponseOutputPoint;
            for (int i = 0; i < results.Length; i++)
            {
                CapturedFrame input = inputs[i + 1];
                Row r = input.Recorded;
                ReleaseResult value = results[i];
                Vector3 recordedSole = r.V("foot/foot-motion/core/original-sole") + r.V("foot/foot-motion/output-stages/final-effective-correction");
                Vector3 hip = r.V("leg/leg-pose/original-hip");
                float legLength = Vector3.Distance(hip, r.V("leg/leg-pose/original-knee")) +
                    Vector3.Distance(r.V("leg/leg-pose/original-knee"), r.V("leg/leg-pose/original-ankle"));
                float extension = Vector3.Distance(input.RootPosition + input.RootRotation * hip, value.Output.Pose.EffectiveAnkle) / legLength;
                Vector3 rawDelta = value.Motion.PathContinuity.InterpolationOutputCorrection - value.Motion.PathContinuity.FinalEffectiveCorrection;
                float rawExtension = Vector3.Distance(input.RootPosition + input.RootRotation * hip,
                    value.Output.Pose.EffectiveAnkle + rawDelta) / legLength;
                float horizontalStep = Vector3.ProjectOnPlane(value.Output.Pose.EffectiveSole - previous, Vector3.up).magnitude;
                float soleError = Vector3.Distance(recordedSole, value.Output.Pose.EffectiveSole);
                float extensionError = Mathf.Abs(extension - r.F("leg/leg-pose/target-extension-ratio"));
                bool hold = Vector3.ProjectOnPlane(value.Motion.PathContinuity.InterpolationOutputCorrection - value.Motion.PathContinuity.FinalEffectiveCorrection, Vector3.up).magnitude > 0.0001f;
                if (hold) held++;
                if (input.Sequence == 12575) resumeStep = horizontalStep;
                maxSoleError = Mathf.Max(maxSoleError, soleError);
                maxExtensionError = Mathf.Max(maxExtensionError, extensionError);
                maxExtension = Mathf.Max(maxExtension, extension);
                maxPenetration = Mathf.Max(maxPenetration, value.FinalSupport.RequiredDisplacement);
                maxClamp = Mathf.Max(maxClamp, value.Motion.PathContinuity.SafetyFloorClampMeters);
                Assert.That(value.Output.GoalTarget.PositionWeight, Is.EqualTo(input.Weight));
                Assert.That(value.GoalPositionError, Is.LessThan(0.0001f));
                Assert.That(value.FinalSupport.Available, Is.True, "最终净空无命中不能算通过");
                Assert.That(value.RawSupport.Available, Is.True, "约束前净空无命中不能算通过");
                rows.Add(new JObject
                {
                    ["frame"] = input.Sequence, ["dt"] = input.DeltaSeconds, ["state"] = value.Motion.ConstraintState.ToString(),
                    ["contact"] = input.Step.Contact, ["lockWeight"] = input.Step.LockWeight, ["weight"] = value.Output.GoalTarget.PositionWeight,
                    ["held"] = hold, ["sole"] = Vec(value.Output.Pose.EffectiveSole), ["ankle"] = Vec(value.Output.Pose.EffectiveAnkle),
                    ["hip"] = Vec(input.RootPosition + input.RootRotation * hip), ["animatedSole"] = Vec(CharacterFootConstraintMath.ResolveOriginalSole(input.Animated)),
                    ["rawSole"] = Vec(value.Output.Pose.EffectiveSole + value.Motion.PathContinuity.InterpolationOutputCorrection - value.Motion.PathContinuity.FinalEffectiveCorrection),
                    ["recordedSole"] = Vec(recordedSole), ["recordedExtension"] = r.F("leg/leg-pose/target-extension-ratio"),
                    ["recordedState"] = r.I("foot/foot-motion/core/constraint-state"), ["extension"] = extension, ["rawExtension"] = rawExtension,
                    ["legLength"] = legLength, ["horizontalStep"] = horizontalStep, ["horizontalSpeed"] = horizontalStep / input.DeltaSeconds,
                    ["rawPenetration"] = value.RawSupport.RequiredDisplacement, ["finalPenetration"] = value.FinalSupport.RequiredDisplacement,
                    ["clamp"] = value.Motion.PathContinuity.SafetyFloorClampMeters, ["recordedSoleError"] = soleError,
                    ["rawAcceptedProbes"] = value.RawSupport.AcceptedSampleCount, ["finalAcceptedProbes"] = value.FinalSupport.AcceptedSampleCount
                });
                previous = value.Output.Pose.EffectiveSole;
            }
            return new JObject
            {
                ["frames"] = results.Length, ["matchedOriginalProbes"] = matched, ["allocatedBytes"] = allocated,
                ["maximumRecordedSoleError"] = maxSoleError, ["maximumRecordedExtensionError"] = maxExtensionError,
                ["maximumExtension"] = maxExtension, ["maximumFinalPenetration"] = maxPenetration, ["maximumClamp"] = maxClamp,
                ["heldFrames"] = held, ["resumeHorizontalStep"] = resumeStep, ["rows"] = rows,
                ["profileSha256"] = Sha256(Path.Combine(Application.dataPath, "../", AssetDatabase.GetAssetPath(profile))),
                ["sceneSha256"] = Sha256(Path.Combine(Application.dataPath, "../", SceneManager.GetActiveScene().path)),
                ["runtimeAssemblySha256"] = Sha256(typeof(CharacterFootLifecycleContext).Assembly.Location)
            };
        }

        static CharacterFootLifecycleContext ReleaseSeed(CapturedFrame input, ulong worldRevision)
        {
            Row r = input.Recorded;
            CharacterFootLifecycleContext context = new CharacterFootLifecycleContext();
            context.Landing.LastLanding = CharacterFootLandingFact.Create(
                input.Step.Events.CurrentContact.Identity, in input.Prediction);
            context.Landing.VerifyPlantTarget(in context.Landing.LastLanding);
            context.Landing.TrackedEventIdentity = r.U("foot/next-landing-tracking-event-identity");
            context.Landing.NextTrackingState = (CharacterFootNextLandingTrackingState)r.I("foot/next-landing-tracking-state");
            const string anchor = "foot/foot-motion/lifecycle/current-contact-anchor-";
            context.PreviousOutputWeight = input.Weight;
            context.PreviousAnimatedSole = CharacterFootConstraintMath.ResolveOriginalSole(input.Animated);
            context.Discrete.State = (CharacterFootConstraintState)r.I("foot/foot-motion/core/constraint-state");
            context.Contact = new CharacterFootContactContext
            {
                HasContact = r.B(anchor + "available"), EventIdentity = r.U(anchor + "event-identity"),
                AcquiredFrameSequence = r.U(anchor + "acquired-frame-sequence"), AcquiredCompletionIdentity = r.U(anchor + "acquired-completion-identity"),
                WorldRevision = worldRevision, SurfaceIdentity = r.I(anchor + "surface-identity"), Anchor = r.V(anchor + "point"), Normal = r.V(anchor + "normal")
            };
            var request = new CharacterFootLockRequest(in input.Step);
            context.ContactTransition = new CharacterFootContactTransitionContext
            {
                HasPreviousRequest = true, PreviousRequestedLock = request.RequestsLock, PreviousIsInZone = request.IsInZone,
                PreviousIsSliding = request.IsSliding, PreviousEventIdentity = request.EventIdentity, PreviousMode = request.Mode,
                PreviousWeight = request.Weight, IsMoving = false, IsLocking = request.LockNow,
                LatestContactEventIdentity = r.U("foot/foot-motion/lifecycle/current-latest-contact-event-identity"),
                LatestReleasedContactEventIdentity = r.U("foot/foot-motion/lifecycle/current-latest-released-contact-event-identity"),
                CompletedLockWeightEventIdentity = r.U("foot/foot-motion/lifecycle/current-completed-lock-weight-event-identity")
            };
            Vector3 correction = r.V("foot/foot-motion/output-stages/final-effective-correction");
            context.Interpolation = new CharacterFootInterpolationState
            {
                HasOutput = true, EffectiveCorrection = correction, HasPreviousResponseOutputPoint = true,
                PreviousResponseOutputPoint = context.PreviousAnimatedSole + correction,
                ResponseHistory = new CharacterFootCorrectionResponseHistory(r.F("foot/foot-motion/response/correction-response-current"),
                    (CharacterFootCorrectionResponseDomain)r.I("foot/foot-motion/response/correction-response-domain"),
                    r.V("foot/foot-motion/response/correction-response-direction")),
                HasCorrectionResponseLineage = true, CorrectionResponseSourceLineage = input.SourceLineage,
                CorrectionResponseProfileRevision = input.ProfileRevision, CorrectionResponseWorldRevision = worldRevision
            };
            return context;
        }

        struct ReleaseResult
        {
            internal CharacterResolvedFootResult Output;
            internal CharacterFootSwingMotionResult Motion;
            internal CharacterFootCurrentSupportObservation RawSupport, FinalSupport;
            internal float GoalPositionError;
        }

        static void RunReleaseSequence(CapturedFrame[] inputs, in CharacterFootLifecycleContext seed, ReleaseResult[] results,
            Transform root, in CharacterFootSoleSupportQuery query, ulong revision, CharacterFootSoleProbeBuffer targetProbes,
            CharacterFootSoleProbeBuffer outputProbes, CharacterFootSoleProbeBuffer rawProbes)
        {
            CharacterFootLifecycleContext context = seed;
            for (int i = 1; i < inputs.Length; i++)
            {
                CapturedFrame input = inputs[i];
                root.SetPositionAndRotation(input.RootPosition, input.RootRotation);
                var snapshot = CharacterFootLandingRuntime.ProjectAfterPrediction(in context, in input.Step, in input.Prediction, in input.Settings);
                var frame = Frame(input, in input.Support, in snapshot, revision);
                var evaluation = new CharacterFootStateEvaluation(input.Side, in input.Step, in input.Prediction, in frame,
                    default, input.Grounded, root, in query, targetProbes, outputProbes);
                CharacterFootLifecycle.Evaluate(ref context, in evaluation, out var receipt);
                var output = receipt.Complete(ref context, false, out var motion);
                Vector3 delta = motion.PathContinuity.InterpolationOutputCorrection - motion.PathContinuity.FinalEffectiveCorrection;
                var rawContacts = input.Animated.ResolveSoleContacts(output.Pose.EffectiveAnkle + delta, output.Pose.EffectiveRotation);
                var rawSupport = query.Query(input.Sequence, input.CompletionId, revision, input.Side, Vector3.up,
                    input.Grounded, in rawContacts, rawProbes);
                Vector3 goalAnkle = root.TransformPoint(output.GoalTarget.ComponentPosition);
                Quaternion goalRotation = root.rotation * output.GoalTarget.ComponentRotation;
                var finalContacts = input.Animated.ResolveSoleContacts(goalAnkle, Quaternion.Slerp(input.Animated.AnkleRotation,
                    goalRotation, output.GoalTarget.RotationWeight));
                var finalSupport = query.Query(input.Sequence, input.CompletionId, revision, input.Side, Vector3.up,
                    input.Grounded, in finalContacts, input.FinalProbes);
                results[i - 1] = new ReleaseResult
                {
                    Output = output, Motion = motion, RawSupport = rawSupport, FinalSupport = finalSupport,
                    GoalPositionError = Vector3.Distance(goalAnkle, output.Pose.EffectiveAnkle)
                };
            }
        }
    }
}
