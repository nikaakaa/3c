/*
目的：用同一段真实采样验证旧接触目标释放后，摆腿目标能够接管。
输入：连续 100 帧事件、接触值、预测和路径查询记录；状态仅在首帧初始化。
调用：正式 LandingRuntime → SwingMotionBuilder → TransitionResolver/Runtime → StateTargetResolver。
预期：历史版复现目标丢失，当前版恢复目标且保持正常接触阶段。
边界：止于插值前目标选择，不验证新碰撞、最终权重、膝关节或整角色表现。
说明：../../docs/diagnostics/foot-placement/ik-tests/expired-contact.html。
*/
using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;
using Unity.Collections;
using static Capture;

static class Program
{
    static int Main(string[] args)
    {
        var report = new JObject { ["status"] = "running", ["rows"] = new JArray() };
        try
        {
            var fixture = JObject.Parse(File.ReadAllText(args[0], Encoding.UTF8));
            var columns = (JObject)fixture["columns"];
            var settings = Settings(args[1]);
            var inputs = ((JArray)fixture["frames"]).Select(f => new Input(f, columns)).ToArray();
            var outputs = new Output[inputs.Length];
            var seed = Seed(inputs[0].Recorded);
            seed.Discrete.State = (CharacterFootConstraintState)inputs[0].Recorded.I(
                "foot/foot-motion/output-stages/constraint-state-before");
            Run(inputs, outputs, seed, settings);
            long before = GC.GetAllocatedBytesForCurrentThread();
            Run(inputs, outputs, seed, settings);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            report["allocatedBytesInWarmedSequence"] = allocated;
            report["capture"] = fixture["capture"];
            for (int i = 0; i < inputs.Length; i++)
            {
                ref readonly Input input = ref inputs[i];
                ref readonly Output output = ref outputs[i];
                ((JArray)report["rows"]).Add(new JObject
                {
                    ["frame"] = input.Sequence, ["dt"] = input.Delta,
                    ["contact"] = input.Step.Contact,
                    ["approach"] = input.Step.Events.InApproachContactToLanding,
                    ["inputWeight"] = input.Weight,
                    ["prepared"] = output.Prepared,
                    ["plantEvent"] = output.PlantEvent.ToString(),
                    ["currentEvent"] = input.Step.Events.CurrentContact.Identity.ToString(),
                    ["nextEvent"] = input.Step.Events.NextLanding.Identity.ToString(),
                    ["swingAccepted"] = output.SwingAccepted,
                    ["support"] = input.Support.Available,
                    ["targetAvailable"] = output.Target.SupportTargetAvailable,
                    ["targetKind"] = output.Target.SupportTarget.Kind.ToString(),
                    ["targetEvent"] = output.Target.SupportTarget.PositionEventIdentity.ToString(),
                    ["targetPosition"] = Vector(output.Target.SupportTarget.Position),
                    ["correction"] = Vector(output.Target.Correction),
                    ["state"] = output.State.ToString(),
                    ["projectionMatchesAdvance"] = output.ProjectionMatchesAdvance,
                    ["hasAnchor"] = output.HasAnchor,
                    ["recordedPrepared"] = input.Recorded.B("foot/approach-plant-target-prepared"),
                    ["recordedTargetAvailable"] = input.Recorded.B("foot/foot-motion/selected-support-target/available"),
                    ["recordedWeight"] = input.Recorded.F("foot/resolved/core/position-weight"),
                    ["recordedState"] = ((CharacterFootConstraintState)input.Recorded.I("foot/foot-motion/core/constraint-state")).ToString()
                });
            }
            foreach (JObject row in report["rows"])
            {
                string label = "采样帧 " + row["frame"];
                Check((bool)row["projectionMatchesAdvance"], label + " 投影与状态推进不一致");
                Check(!(bool)row["hasAnchor"], label + " 超出了本实验无锚点范围");
                Check((string)row["state"] == (string)row["recordedState"], label + " 生命周期状态与原采样不一致");
            }
            Check(allocated == 0, "连续计算产生托管分配：" + allocated);
            report["status"] = "passed";
            Console.WriteLine("Executed " + inputs.Length + " captured frames; missing targets: " +
                outputs.Count(o => !o.Target.SupportTargetAvailable) + "; loop allocation: " + allocated);
            return 0;
        }
        catch (Exception error)
        {
            report["status"] = "failed";
            report["error"] = error.ToString();
            Console.Error.WriteLine(error);
            return 1;
        }
        finally
        {
            File.WriteAllText(args[2], report.ToString(Formatting.None), new UTF8Encoding(false));
        }
    }

    static void Run(Input[] inputs, Output[] outputs, CharacterFootLifecycleContext context,
        CharacterFootMotionSettings settings)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            ref readonly Input input = ref inputs[i];
            var snapshot = CharacterFootLandingRuntime.ProjectAfterPrediction(
                in context, in input.Step, in input.Prediction, in settings);
            var swing = CharacterFootSwingMotionBuilder.Build(in input.Animated, in input.Step,
                input.Weight, Vector3.up, in input.Path, true, input.Step.FootHeight,
                snapshot.NextSwingPredictionError);
            bool prepared = snapshot.PlantTargetState == CharacterFootPlantTargetState.Tracking;
            var frame = new CharacterFootStateFrame(input.Sequence, input.Completion,
                input.RigId, input.RigRevision, CharacterFootSide.Right,
                in input.Animated, input.Animated.HipPosition, 0f, in swing, in input.Path,
                input.HasContactLanding, in input.ContactLanding, prepared, snapshot.PlantTarget,
                in input.Support, in input.Lock, input.Step.Support, input.Lock.EventIdentity,
                CharacterFootGoalOwnershipLossReason.None, input.Weight, Vector3.up, input.Delta,
                input.SourceLineage, default, input.Support.WorldRevision, in settings);
            CharacterFootLandingRuntime.Evaluate(ref context.Landing,
                in input.Step, in input.Prediction, in settings);
            bool same = snapshot.PlantTargetState == context.Landing.PlantTargetState &&
                snapshot.PlantTarget.LandingEventIdentity == context.LandingSnapshot.PlantTarget.LandingEventIdentity;
            var transition = CharacterFootTransitionResolver.ResolvePreInterpolation(in context, in frame);
            CharacterFootLandingRuntime.CommitCurrentContactVerification(ref context.Landing,
                in input.Step, in input.Prediction, in transition);
            CharacterFootTransitionRuntime.Apply(ref context, in transition, in frame);
            var target = CharacterFootStateTargetResolver.Resolve(in context, in transition,
                input.Step.TimeToLandingSeconds, in frame);
            outputs[i] = new Output
            {
                Target = target, State = context.Discrete.State, Prepared = prepared,
                PlantEvent = snapshot.PlantTarget.LandingEventIdentity,
                SwingAccepted = swing.Accepted, HasAnchor = context.Contact.HasContact,
                ProjectionMatchesAdvance = same
            };
        }
    }

    static JArray Vector(Vector3 value) => new JArray(value.x, value.y, value.z);

    readonly struct Input
    {
        internal Input(JToken captured, JObject columns)
        {
            Recorded = Row.ReadMain(captured, columns);
            Step = Capture.Step(Recorded);
            Prediction = Capture.Prediction(Recorded);
            Animated = Capture.Animated(Recorded);
            Path = PathInput(Recorded, captured, columns);
            Support = Capture.Support(Recorded);
            Lock = new CharacterFootLockRequest(in Step);
            Sequence = Recorded.U("foot/resolved/core/frame-sequence");
            Completion = Recorded.U("foot/resolved/core/completion-identity");
            Delta = Recorded.F("input/presentation-delta-seconds");
            Weight = Recorded.F("foot/foot-motion/lifecycle/formal-foot-placement-weight");
            RigId = new FixedString64Bytes(Recorded.S("foot/resolved/core/rig-id"));
            RigRevision = new FixedString64Bytes(Recorded.S("foot/resolved/core/rig-revision"));
            SourceLineage = new FixedString128Bytes(Recorded.S("input/foot-step-observation/source-identity"));
            HasContactLanding = CharacterFootLandingRuntime.TryResolveCurrentContactCandidate(
                in Step, in Prediction, out ContactLanding);
            Check(Step.Events.CurrentContact.Identity == Recorded.U("formal-input/events/current-contact/identity"),
                "采样帧 " + Sequence + " 当前事件身份不一致");
            Check(Step.Events.NextLanding.Identity == Recorded.U("formal-input/events/next-landing/identity"),
                "采样帧 " + Sequence + " 下一事件身份不一致");
            Check(Support.Available == Recorded.B("foot/current-support/available"),
                "采样帧 " + Sequence + " 支撑记录不一致");
        }

        internal readonly Row Recorded;
        internal readonly AnimationFootMotionRuntimeSample Step;
        internal readonly CharacterFootLandingPredictionResult Prediction;
        internal readonly CharacterFootPlacementAnimatedFootPose Animated;
        internal readonly CharacterFootGroundPathResult Path;
        internal readonly CharacterFootCurrentSupportObservation Support;
        internal readonly CharacterFootLockRequest Lock;
        internal readonly CharacterFootGroundPathLanding ContactLanding;
        internal readonly bool HasContactLanding;
        internal readonly ulong Sequence, Completion;
        internal readonly float Delta, Weight;
        internal readonly FixedString64Bytes RigId, RigRevision;
        internal readonly FixedString128Bytes SourceLineage;
    }

    struct Output
    {
        internal CharacterFootStateTarget Target;
        internal CharacterFootConstraintState State;
        internal ulong PlantEvent;
        internal bool Prepared, SwingAccepted, HasAnchor, ProjectionMatchesAdvance;
    }
}
