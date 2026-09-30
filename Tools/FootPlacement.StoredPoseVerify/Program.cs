/*
目的：用真实 Stored 捕获与 Live 交接连续窗口，对比正式贡献选择和接触时间语义。
输入：1174～1186 左脚无接触捕获、889～958 右脚真实接触捕获；保留双脚原样本和贡献顺序。
链路：历史/当前正式贡献选择 → 正式 Stored 样本捕获 → FootMotion frame；Goal 验证由同场景 Unity 入口接续。
边界：此独立入口不重建整份 Stored 骨骼，不执行 Physics、Goal 或 FBBIK；未记录身份的零权重 Action 不参与选择。
说明：docs/diagnostics/foot-placement/ik-tests/stored-foot-motion.html。
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using UnityEngine;

static class Program
{
    static int Main(string[] args)
    {
        var report = new JObject { ["status"] = "running", ["scope"] = "正式贡献选择与 Stored 时间语义；尚未执行 Goal、Physics 和 FBBIK", ["rows"] = new JArray() };
        try
        {
            var fixture = JObject.Parse(File.ReadAllText(args[0], Encoding.UTF8));
            report["capture"] = fixture["capture"];
            report["baselineCommit"] = fixture["baselineCommit"];
            var columns = (JObject)fixture["columns"];
            var names = new List<string>();
            var frames = ((JArray)fixture["frames"]).Select(x => new Input(x, columns, (JObject)fixture["sourceIds"], (string)fixture["side"], names)).ToArray();
            var baseline = new Output[frames.Length];
            var current = new Output[frames.Length];
            Run(frames, baseline, false);
            Run(frames, current, true);
            long start = GC.GetAllocatedBytesForCurrentThread();
            Run(frames, current, true);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            report["allocatedBytesInWarmedSequence"] = allocated;
            var rows = (JArray)report["rows"];
            for (int i = 0; i < frames.Length; i++)
            {
                Input input = frames[i];
                Output old = baseline[i], value = current[i];
                Check(old.Kind == AnimationPoseContributionKind.Live && Math.Abs(old.Weight - input.RecordedWeight) < .000001f,
                    "历史正式选择未重现源权重：" + input.Frame);
                Check(Math.Abs(old.Sample.Contact - input.RecordedStep.Contact) < .000001f &&
                    Math.Abs(old.Sample.LockWeight - input.RecordedStep.LockWeight) < .000001f,
                    "历史输入样本不一致：" + input.Frame);
                if (value.Kind == AnimationPoseContributionKind.Stored)
                {
                    Check(value.Sample.Contact == frames[0].RecordedStep.Contact,
                        "Stored 未保持所捕获姿态的接触：" + input.Frame);
                    Check(value.Sample.ToeSpeed == 0f && !value.Sample.HasPredictiveLanding,
                        "Stored 不应推进脚趾速度与未来落地：" + input.Frame);
                }
                Check(value.Weight > 0f, "FootMotion 必须属于实际参与姿态的贡献：" + input.Frame);
                rows.Add(new JObject
                {
                    ["frame"] = input.Frame, ["dt"] = input.Delta, ["omittedZeroActionCount"] = input.Omitted,
                    ["authorWeight"] = input.AuthorWeight,
                    ["historicalKind"] = old.Kind.ToString(), ["historicalWeight"] = old.Weight,
                    ["historicalContact"] = old.Sample.Contact, ["historicalLockWeight"] = old.Sample.LockWeight,
                    ["currentKind"] = value.Kind.ToString(), ["currentWeight"] = value.Weight,
                    ["currentContact"] = value.Sample.Contact, ["currentLockWeight"] = value.Sample.LockWeight,
                    ["currentToeSpeed"] = value.Sample.ToeSpeed, ["currentPredictiveLanding"] = value.Sample.HasPredictiveLanding,
                    ["currentSourceIdentity"] = names[value.SourceNameIndex], ["currentSample"] = SampleJson(value.Sample),
                    ["currentContactIdentity"] = value.Sample.Events.CurrentContact.Identity.ToString(),
                    ["capturedSourceSampleIdentity"] = value.SourceSampleIdentity.ToString()
                });
            }
            Check(allocated == 0, "预热后计算存在托管分配：" + allocated);
            report["status"] = "passed";
            report["verdict"] = "已执行正式函数的局部比较；不是 Goal、骨骼混合或 IK 修复通过。";
            Console.WriteLine("Executed " + frames.Length + " frames; allocation=" + allocated);
            return 0;
        }
        catch (Exception error)
        {
            report["status"] = "failed";
            report["failure"] = error.ToString();
            Console.Error.WriteLine(error);
            return 1;
        }
        finally { File.WriteAllText(args[1], report.ToString(Formatting.None), new UTF8Encoding(false)); }
    }

    static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }

    static JObject SampleJson(AnimationFootMotionRuntimeSample sample) => new JObject
    {
        ["footHeight"] = sample.FootHeight, ["toeHeight"] = sample.ToeHeight, ["toeSpeed"] = sample.ToeSpeed,
        ["positionError"] = sample.PositionError, ["rotationError"] = sample.RotationError,
        ["contact"] = sample.Contact, ["lockMode"] = (int)sample.LockMode, ["lockWeight"] = sample.LockWeight, ["support"] = sample.Support,
        ["phase"] = (int)sample.Events.Phase, ["timeToLanding"] = sample.Events.TimeToLandingSeconds,
        ["swingProgress"] = sample.Events.SwingProgress, ["approachProgress"] = sample.Events.ApproachContactToLandingProgress,
        ["current"] = EventJson(sample.Events.CurrentContact), ["next"] = EventJson(sample.Events.NextLanding)
    };

    static JToken EventJson(AnimationFootMotionEventOccurrence value) => value.IsValid ? new JObject
    {
        ["ordinal"] = value.Ordinal, ["cycle"] = value.LandingCycle, ["normalizedTime"] = value.NormalizedTime,
        ["distance"] = value.Distance, ["point"] = new JArray(value.RootLocalLanding.x, value.RootLocalLanding.y, value.RootLocalLanding.z),
        ["sourceSampleIdentity"] = value.SourceSampleIdentity.ToString(), ["continuity"] = value.ContributionContinuityIdentity.ToString(),
        ["identity"] = value.Identity.ToString()
    } : JValue.CreateNull();

    static void Run(Input[] inputs, Output[] outputs, bool candidate)
    {
        AnimationFootMotionSourceSample history = default;
        for (int i = 0; i < inputs.Length; i++)
        {
            Input input = inputs[i];
            AnimationPoseSourceContribution selected = candidate
                ? input.Contributions[CharacterPoseWorldContextAdapter.RequireFootMotionContribution(input.Contributions, input.Contributions.Length)]
                : HistoricalFootMotionSelector.Select(input.Contributions, input.Contributions.Length);
            AnimationFootMotionSourceSample sample = selected.Kind == AnimationPoseContributionKind.Stored
                ? history.CaptureStoredPose()
                : input.Live.BindContribution(selected.ContributionContinuityIdentity);
            history = sample;
            outputs[i] = new Output
            {
                Kind = selected.Kind, Weight = selected.Weight,
                Sample = input.Side == CharacterFootSide.Left ? sample.Left : sample.Right,
                SourceSampleIdentity = sample.SourceSampleIdentity, SourceNameIndex = sample.SourceNameIndex
            };
        }
    }

    struct Output
    {
        internal AnimationPoseContributionKind Kind;
        internal float Weight;
        internal AnimationFootMotionRuntimeSample Sample;
        internal ulong SourceSampleIdentity;
        internal int SourceNameIndex;
    }

    sealed class Input
    {
        internal Input(JToken captured, JObject columns, JObject identities, string side, List<string> names)
        {
            var row = new Row(captured["main"], columns["main"]);
            var paired = new Row(captured["pairedMain"], columns["main"]);
            Side = side == "left" ? CharacterFootSide.Left : CharacterFootSide.Right;
            Frame = (int)captured["frame"];
            Delta = row.F("input/presentation-delta-seconds");
            AuthorWeight = row.F("foot/foot-motion/lifecycle/formal-foot-placement-weight");
            RecordedWeight = row.F("input/foot-step-observation/source-weight");
            RecordedStep = Step(row, Side);
            AnimationFootMotionRuntimeSample left = Side == CharacterFootSide.Left ? RecordedStep : Step(paired, CharacterFootSide.Left);
            AnimationFootMotionRuntimeSample right = Side == CharacterFootSide.Right ? RecordedStep : Step(paired, CharacterFootSide.Right);
            ulong sampleIdentity = left.Events.CurrentContact.IsValid ? left.Events.CurrentContact.SourceSampleIdentity : left.Events.NextLanding.SourceSampleIdentity;
            string sourceName = row.S("input/foot-step-observation/source-identity");
            int sourceNameIndex = names.IndexOf(sourceName);
            if (sourceNameIndex < 0) { sourceNameIndex = names.Count; names.Add(sourceName); }
            Live = new AnimationFootMotionSourceSample(sourceNameIndex, sampleIdentity,
                row.I("input/foot-step-observation/clip-binding-index"), row.I("input/foot-step-observation/cycle"),
                row.F("input/foot-step-observation/normalized-time"), in left, in right);
            var contributions = new List<AnimationPoseSourceContribution>();
            foreach (JToken value in captured["sources"])
            {
                var source = new Row(value, columns["sources"]);
                var kind = (AnimationPoseContributionKind)source.I("kind");
                string key = source.S("node-id") + "|" + source.S("selection-generation") + "|" + source.S("action-instance-id");
                if (kind == AnimationPoseContributionKind.Live && source.F("weight") == 0f && identities[key] == null)
                {
                    Omitted++;
                    continue;
                }
                AnimationPoseSourceId id = kind == AnimationPoseContributionKind.Live ? SourceId((string)identities[key]) : default;
                contributions.Add(new AnimationPoseSourceContribution(new PoseNodeId(source.S("node-id")), kind,
                    id, kind == AnimationPoseContributionKind.Live ? contributions.Count : -1,
                    source.U("continuity"), source.F("weight"), source.F("left-foot-weight"), source.F("right-foot-weight")));
            }
            Contributions = contributions.ToArray();
        }

        internal readonly int Frame, Omitted;
        internal readonly float Delta, AuthorWeight, RecordedWeight;
        internal readonly CharacterFootSide Side;
        internal readonly AnimationPoseSourceContribution[] Contributions;
        internal readonly AnimationFootMotionSourceSample Live;
        internal readonly AnimationFootMotionRuntimeSample RecordedStep;
    }

    static AnimationPoseSourceId SourceId(string text)
    {
        string[] values = text.Split('/');
        int kindIndex = Array.FindIndex(values, x => x == "Timeline" || x == "Clip" || x == "MotionMatching" || x == "BlendSpace");
        var kind = (AnimationPoseSourceKind)Enum.Parse(typeof(AnimationPoseSourceKind), values[kindIndex]);
        var generation = new AnimationPoseSelectionGeneration(ulong.Parse(values[kindIndex + 1], CultureInfo.InvariantCulture));
        if (kind != AnimationPoseSourceKind.Timeline)
            return new AnimationPoseSourceId(new PresentationPoseSourceIndex(int.Parse(values[0], CultureInfo.InvariantCulture)), kind, generation);
        string[] track = values[1].Split('@');
        var producer = new AnimationProducerId(values[0], track[0]);
        var playback = new AnimationPlaybackId(producer, ulong.Parse(track[1], CultureInfo.InvariantCulture));
        ulong action = values.Length > kindIndex + 2 ? ulong.Parse(values[kindIndex + 2].Split(':')[1], CultureInfo.InvariantCulture) : 0;
        return new AnimationPoseSourceId(playback, kind, generation, action);
    }

    static AnimationFootMotionRuntimeSample Step(Row row, CharacterFootSide side)
    {
        var events = new AnimationFootMotionEventFrame(Occurrence(row, "current-contact", side), Occurrence(row, "next-landing", side),
            (AnimationFootMotionEventPhase)row.I("formal-input/events/phase"), row.F("formal-input/time-to-landing-seconds"),
            row.F("formal-input/events/swing-progress"), row.F("formal-input/events/approach-contact-to-landing-progress"));
        return new AnimationFootMotionRuntimeSample(row.F("formal-input/foot-height"), row.F("formal-input/toe-height"),
            row.F("formal-input/toe-speed"), row.F("formal-input/position-error"), row.F("formal-input/rotation-error"),
            row.F("formal-input/contact"), (AnimationFootStepObservationLockMode)row.I("formal-input/lock-mode"),
            row.F("formal-input/lock-weight"), row.F("formal-input/support"), in events);
    }

    static AnimationFootMotionEventOccurrence Occurrence(Row row, string name, CharacterFootSide side)
    {
        string prefix = "formal-input/events/" + name + "/";
        if (!row.B(prefix + "is-valid")) return default;
        var occurrence = new AnimationFootMotionEventOccurrence(row.I(prefix + "ordinal"), row.I(prefix + "landing-cycle"), row.F(prefix + "normalized-time"),
            row.F(prefix + "distance"), row.V(prefix + "root-local-landing"))
            .Bind(row.U(prefix + "source-sample-identity"), row.U(prefix + "contribution-continuity-identity"), side);
        Check(occurrence.Identity == row.U(prefix + "identity"), "事件身份必须重现原记录");
        return occurrence;
    }

    sealed class Row
    {
        readonly Dictionary<string, string> values;
        internal Row(JToken data, JToken columns) => values = columns.Select((x, i) => new KeyValuePair<string, string>((string)x, (string)data[i])).ToDictionary(x => x.Key, x => x.Value);
        internal string S(string key) => values[key];
        internal float F(string key) => float.Parse(S(key), CultureInfo.InvariantCulture);
        internal int I(string key) => int.Parse(S(key), CultureInfo.InvariantCulture);
        internal ulong U(string key) => ulong.Parse(S(key), CultureInfo.InvariantCulture);
        internal bool B(string key) => bool.Parse(S(key));
        internal Vector3 V(string key) => new Vector3(F(key + ".x"), F(key + ".y"), F(key + ".z"));
    }
}
