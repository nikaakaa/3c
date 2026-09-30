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
                ["test"] = nameof(CapturedContactToSwingReleasesExpiredTargetWithoutLosingValidSupport),
                ["status"] = "running",
                ["rows"] = new JArray()
            };
            GameObject goalRoot = null;
            try
            {
                JObject fixture = JObject.Parse(File.ReadAllText(
                    Path.Combine(EvidenceDirectory, "expired-contact-input.json"), Encoding.UTF8));
                report["capture"] = fixture["capture"];
                report["baselineCommit"] = fixture["baselineCommit"];
                report["runtimeCommit"] = Git("rev-parse HEAD");
                report["runtimeSourceBlob"] = Git("hash-object " + RuntimeSource);
                report["sourceSha256"] = fixture["sourceSha256"];
                var frames = (JArray)fixture["frames"];
                var columns = (JObject)fixture["columns"];
                Assert.That(frames.Count, Is.EqualTo(100));
                Assert.That(SceneManager.GetActiveScene().path,
                    Is.EqualTo("Assets/Scenes/GameplayLab/GameplayLabFixed.unity"));

                var profile = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementProfile>(
                    AssetRoot + "FootPlacement/CorinFootPlacementProfile.asset");
                var calibration = AssetDatabase.LoadAssetAtPath<CharacterFootPlacementRigCalibration>(
                    AssetRoot + "FootPlacement/CorinFootPlacementRigCalibration.asset");
                var definition = AssetDatabase.LoadAssetAtPath<CharacterAnimationRigDefinition>(
                    AssetRoot + "Rig/CorinAnimationRigDefinition.asset");
                var binding = Object.FindObjectsByType<CharacterAnimationRigBinding>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).First(
                    x => x.gameObject.scene == SceneManager.GetActiveScene());
                var rig = new CharacterFootPlacementPoseRig(calibration,
                    new CharacterAnimationRigPayload(definition), binding,
                    binding.GetComponentInParent<CharacterWorldAwarePresentationBinding>());
                var settings = profile.FootMotion.Build();
                report["profileRevision"] = profile.Revision;
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
                var first = Row.Main(frames[0], columns);
                CharacterFootLifecycleContext original = Seed(first);
                CharacterFootLifecycleContext current = original;
                int originalMisses = 0, repairedMisses = 0, matchedProbes = 0;
                int contactFrames = 0, approachFrames = 0, acceptedSwingFrames = 0;
                float maxUnchangedDifference = 0f;
                float maxOutputPenetration = 0f;
                var inputProbes = new CharacterFootSoleProbeBuffer();
                var targetProbes = new CharacterFootSoleProbeBuffer();
                var outputProbes = new CharacterFootSoleProbeBuffer();

                foreach (JToken captured in frames)
                {
                    Row r = Row.Main(captured, columns);
                    ulong sequence = r.U("foot/resolved/core/frame-sequence");
                    ulong completionId = r.U("foot/resolved/core/completion-identity");
                    string frameLabel = "采样帧 " + sequence;
                    var samples = Row.Table(captured, columns, "probes").ToArray();
                    var animated = Animated(r, samples, goalRoot.transform);
                    var contacts = animated.ResolveSoleContacts(animated.AnklePosition, animated.AnkleRotation);
                    var support = soleQuery.Query(sequence, completionId, 1UL, CharacterFootSide.Right,
                        Vector3.up, r.B("input/grounded"), in contacts, inputProbes);
                    for (int i = 0; i < samples.Length; i++)
                    {
                        var hit = inputProbes[i].Result;
                        Assert.That(hit.Accepted, Is.EqualTo(samples[i].B("accepted")),
                            frameLabel + " 脚底探针 " + i + "：当前地形与采样不一致");
                        if (hit.Accepted)
                        {
                            Assert.That(Vector3.Distance(hit.Point, samples[i].V("point")), Is.LessThan(0.0002f), frameLabel);
                            Assert.That(Vector3.Distance(hit.Normal, samples[i].V("normal")), Is.LessThan(0.0002f), frameLabel);
                        }
                        matchedProbes++;
                    }

                    AnimationFootMotionRuntimeSample step = Step(r);
                    Assert.That(step.Events.CurrentContact.Identity,
                        Is.EqualTo(r.U("formal-input/events/current-contact/identity")), frameLabel);
                    Assert.That(step.Events.NextLanding.Identity,
                        Is.EqualTo(r.U("formal-input/events/next-landing/identity")), frameLabel);
                    CharacterFootGroundPathResult path = PathInput(r, captured, columns, profile);
                    CharacterFootLandingPredictionResult prediction = Prediction(r);
                    var oldSnapshot = oldProject(in original, in step, in prediction, in settings);
                    var newSnapshot = CharacterFootLandingRuntime.ProjectAfterPrediction(
                        in current, in step, in prediction, in settings);
                    CharacterFootStateFrame oldFrame = Frame(r, in animated, in step, in path,
                        in support, in oldSnapshot, rig.RightLegLength, profile);
                    CharacterFootStateFrame newFrame = Frame(r, in animated, in step, in path,
                        in support, in newSnapshot, rig.RightLegLength, profile);
                    var oldEvaluation = new CharacterFootStateEvaluation(CharacterFootSide.Right,
                        in step, in prediction, in oldFrame, default, r.B("input/grounded"),
                        goalRoot.transform, in soleQuery, targetProbes, outputProbes);
                    var newEvaluation = new CharacterFootStateEvaluation(CharacterFootSide.Right,
                        in step, in prediction, in newFrame, default, r.B("input/grounded"),
                        goalRoot.transform, in soleQuery, targetProbes, outputProbes);

                    // 历史生产者配合同一个未改动的 Resolve，避免切换共享运行代码。
                    oldAdvance(ref original.Landing, in step, in prediction, in settings);
                    resolve(ref original, in oldEvaluation, out _, out var oldReceipt);
                    var oldOutput = oldReceipt.Complete(ref original, false, out _);
                    CharacterFootLifecycle.Evaluate(ref current, in newEvaluation, out var newReceipt);
                    var newOutput = newReceipt.Complete(ref current, false, out _);
                    float oldWeight = oldOutput.GoalTarget.PositionWeight;
                    float newWeight = newOutput.GoalTarget.PositionWeight;
                    float recordedWeight = r.F("foot/resolved/core/position-weight");
                    var finalContacts = animated.ResolveSoleContacts(
                        Vector3.LerpUnclamped(animated.AnklePosition,
                            goalRoot.transform.TransformPoint(newOutput.GoalTarget.ComponentPosition), newWeight),
                        Quaternion.Slerp(animated.AnkleRotation,
                            goalRoot.transform.rotation * newOutput.GoalTarget.ComponentRotation,
                            newOutput.GoalTarget.RotationWeight));
                    var finalSupport = soleQuery.Query(sequence, completionId, 1UL, CharacterFootSide.Right,
                        Vector3.up, r.B("input/grounded"), in finalContacts, outputProbes);
                    float penetration = finalSupport.TryResolveHeightConstraint(out float displacement, out _)
                        ? Mathf.Max(0f, displacement) : 0f;
                    maxOutputPenetration = Mathf.Max(maxOutputPenetration, penetration);
                    float unchangedDifference = Vector3.Distance(
                        oldOutput.GoalTarget.EffectiveSole, newOutput.GoalTarget.EffectiveSole);
                    ((JArray)report["rows"]).Add(new JObject
                    {
                        ["frame"] = sequence, ["dt"] = r.F("input/presentation-delta-seconds"),
                        ["contact"] = step.Contact, ["phase"] = step.Events.Phase.ToString(),
                        ["inputWeight"] = r.F("foot/foot-motion/lifecycle/formal-foot-placement-weight"),
                        ["currentEvent"] = step.Events.CurrentContact.Identity.ToString(),
                        ["nextEvent"] = step.Events.NextLanding.Identity.ToString(),
                        ["oldPrepared"] = oldFrame.PreparedPlantActive,
                        ["newPrepared"] = newFrame.PreparedPlantActive,
                        ["support"] = support.Available,
                        ["swingAccepted"] = newFrame.SwingMotion.Accepted,
                        ["recordedWeight"] = recordedWeight, ["oldWeight"] = oldWeight, ["newWeight"] = newWeight,
                        ["sourceSole"] = Vec((animated.HeelPosition + animated.ToePosition) * 0.5f),
                        ["oldSole"] = Vec(oldOutput.GoalTarget.EffectiveSole),
                        ["newSole"] = Vec(newOutput.GoalTarget.EffectiveSole),
                        ["newPenetration"] = penetration,
                        ["finalSupportAvailable"] = finalSupport.Available,
                        ["oldState"] = original.Discrete.State.ToString(),
                        ["newState"] = current.Discrete.State.ToString()
                    });

                    Assert.That(oldWeight, Is.EqualTo(recordedWeight), frameLabel + " 历史函数应重现原采样权重");
                    Assert.That(newWeight, Is.EqualTo(1f), frameLabel + " 有效目标应保持作者权重");
                    Assert.That(penetration, Is.LessThan(0.0002f), frameLabel + " 输出脚底采样点不得穿入已查询地面");
                    Assert.That(current.Contact.HasContact, Is.False, frameLabel + " 窗口不应进入未录制锚点的求解");
                    Assert.That(oldSnapshot.PlantTargetState,
                        Is.EqualTo((CharacterFootPlantTargetState)r.I("foot/plant-target-state")), frameLabel);
                    Assert.That(oldFrame.PreparedPlantActive,
                        Is.EqualTo(r.B("foot/approach-plant-target-prepared")), frameLabel);
                    if (sequence < 34713UL)
                    {
                        Assert.That(newSnapshot.PlantTargetState, Is.EqualTo(oldSnapshot.PlantTargetState), frameLabel);
                        maxUnchangedDifference = Mathf.Max(maxUnchangedDifference, unchangedDifference);
                        Assert.That(unchangedDifference, Is.LessThan(0.00001f), frameLabel + " 正常接触阶段不得改变");
                    }
                    if (step.Events.InApproachContactToLanding)
                    {
                        approachFrames++;
                        Assert.That(newFrame.PreparedPlantActive, Is.True, frameLabel);
                        Assert.That(newSnapshot.PlantTarget.LandingEventIdentity,
                            Is.EqualTo(step.Events.NextLanding.Identity), frameLabel);
                    }
                    if (sequence >= 34689UL && sequence <= 34712UL)
                    {
                        contactFrames++;
                        Assert.That(newFrame.PreparedPlantActive, Is.True, frameLabel + " 真实接触期间保留目标");
                    }
                    if (step.Contact == 0f && !step.Events.InApproachContactToLanding)
                    {
                        acceptedSwingFrames++;
                        Assert.That(newFrame.PreparedPlantActive, Is.False, frameLabel + " 已结束接触不能霸占目标");
                        Assert.That(newFrame.SwingMotion.Accepted, Is.True, frameLabel);
                    }
                    if (recordedWeight == 0f)
                    {
                        originalMisses++;
                        if (newWeight == 1f) repairedMisses++;
                    }
                }
                Assert.That(originalMisses, Is.EqualTo(27), "必须包含采样中完整的 27 个掉权重帧");
                Assert.That(repairedMisses, Is.EqualTo(originalMisses));
                Assert.That(contactFrames, Is.EqualTo(24));
                Assert.That(approachFrames, Is.EqualTo(20));
                Assert.That(acceptedSwingFrames, Is.EqualTo(53));
                report["metrics"] = JObject.FromObject(new
                {
                    matchedProbes, originalMisses, repairedMisses, contactFrames,
                    approachFrames, acceptedSwingFrames, maxUnchangedDifference, maxOutputPenetration
                });
                report["status"] = "passed";
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
                string htmlPath = Path.Combine(EvidenceDirectory, "expired-contact.html");
                string html = File.ReadAllText(htmlPath, Encoding.UTF8);
                const string startMarker = "<script id=\"experiment-data\" type=\"application/json\">";
                int start = html.IndexOf(startMarker, StringComparison.Ordinal) + startMarker.Length;
                int end = html.IndexOf("</script>", start, StringComparison.Ordinal);
                string data = JsonConvert.SerializeObject(report, Formatting.None,
                    new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeHtml });
                string pendingPath = htmlPath + ".pending";
                File.WriteAllText(pendingPath, html.Substring(0, start) + data + html.Substring(end), new UTF8Encoding(false));
                File.Replace(pendingPath, htmlPath, null);
                TestContext.WriteLine("语义与逐帧结果：" + htmlPath);
            }
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

        static CharacterFootPlacementAnimatedFootPose Animated(Row r, Row[] probes, Transform root)
        {
            root.SetPositionAndRotation(r.V("physical-body/pose-root-world-position"),
                r.Q("physical-body/pose-root-world-rotation"));
            Quaternion ankle = r.Q("foot/source-ankle-rotation");
            Quaternion local = r.Q("foot/resolved/core/source-sole-frame-local-rotation");
            Quaternion sole = ankle * local;
            var pose = new CharacterFootPlacementAnimatedFootPose(
                root.TransformPoint(r.V("leg/leg-pose/original-hip")),
                root.TransformPoint(r.V("leg/leg-pose/original-knee")),
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

        static CharacterFootGroundPathResult PathInput(Row r, JToken captured, JObject columns,
            CharacterFootPlacementProfile profile)
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
                in last, in next, r.U("foot/ground-path/authority-tick"), Vector3.up, profile.Revision);
            var settings = profile.GroundDetection.Build();
            Assert.That(CharacterFootGroundPathInputBuilder.TryBuild(in key, last.Point, next.Point,
                last.Normal, next.Normal, last.SurfaceIdentity, next.SurfaceIdentity, Vector3.up,
                in settings, out var input), Is.True);
            Assert.That(page.Contacts.SurfaceCoverage.Begin(input.Query, 1UL), Is.True);
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

        static CharacterFootStateFrame Frame(Row r, in CharacterFootPlacementAnimatedFootPose animated,
            in AnimationFootMotionRuntimeSample step, in CharacterFootGroundPathResult path,
            in CharacterFootCurrentSupportObservation support, in CharacterFootLandingSnapshot landing,
            float legLength, CharacterFootPlacementProfile profile)
        {
            float weight = r.F("foot/foot-motion/lifecycle/formal-foot-placement-weight");
            var swing = CharacterFootSwingMotionBuilder.Build(in animated, in step, weight,
                Vector3.up, in path, true, step.FootHeight, landing.NextSwingPredictionError);
            var request = new CharacterFootLockRequest(in step);
            bool prepared = landing.PlantTargetState == CharacterFootPlantTargetState.Tracking;
            return new CharacterFootStateFrame(r.U("foot/resolved/core/frame-sequence"),
                r.U("foot/resolved/core/completion-identity"),
                new FixedString64Bytes(r.S("foot/resolved/core/rig-id")),
                new FixedString64Bytes(r.S("foot/resolved/core/rig-revision")), CharacterFootSide.Right,
                in animated, animated.HipPosition, legLength, in swing, in path, false, default,
                prepared, prepared ? landing.PlantTarget : default, in support, in request,
                step.Support, request.EventIdentity, CharacterFootGoalOwnershipLossReason.None,
                weight, Vector3.up, r.F("input/presentation-delta-seconds"),
                new FixedString128Bytes(r.S("input/foot-step-observation/source-identity")),
                new FixedString128Bytes(profile.Revision), 1UL, profile.FootMotion.Build());
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
