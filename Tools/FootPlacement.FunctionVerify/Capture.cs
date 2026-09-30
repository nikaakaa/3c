using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

internal static class Capture
{
    internal static CharacterFootLifecycleContext Seed(Row r)
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

    internal static CharacterFootLandingFact Fact(in CharacterFootGroundPathLanding landing)
    {
        var prediction = new CharacterFootLandingPredictionResult(CharacterFootSide.Right,
            CharacterFootLandingPredictionState.Accepted, 0, CharacterFootLandingStepSource.FormalNextLanding,
            landing.LandingEventIdentity, landing.TrajectoryGeneration, 1f, 0f, default, false,
            landing.FutureBodyTranslationSourceIdentity, default, default, default, default, default,
            new CharacterFootLandingSupport(landing.SurfaceIdentity, landing.Point, landing.Normal, 0f), default);
        return CharacterFootLandingFact.Create(landing.LandingEventIdentity, in prediction);
    }

    internal static AnimationFootMotionRuntimeSample Step(Row r)
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

    internal static AnimationFootMotionEventOccurrence Occurrence(Row r, string name)
    {
        string p = "formal-input/events/" + name + "/";
        // 绝对 clip 相位未采样；此调用链只消费已录制的事件身份与剩余时间。
        return new AnimationFootMotionEventOccurrence(r.I(p + "ordinal"), r.I(p + "landing-cycle"),
            0f, r.F(p + "distance"), r.V(p + "root-local-landing"));
    }

    internal static CharacterFootLandingPredictionResult Prediction(Row r)
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


    internal static CharacterFootPlacementAnimatedFootPose Animated(Row r)
    {
        Quaternion rootRotation = r.Q("physical-body/pose-root-world-rotation");
        Vector3 rootPosition = r.V("physical-body/pose-root-world-position");
        Quaternion ankle = r.Q("foot/source-ankle-rotation");
        Quaternion local = r.Q("foot/resolved/core/source-sole-frame-local-rotation");
        Quaternion sole = ankle * local;
        return new CharacterFootPlacementAnimatedFootPose(
            rootPosition + rootRotation * r.V("leg/leg-pose/original-hip"),
            rootPosition + rootRotation * r.V("leg/leg-pose/original-knee"),
            r.V("foot/source-ankle-position"), ankle, r.V("foot/source-toe-position"),
            Quaternion.identity, r.V("foot/source-heel-position"),
            sole * Vector3.forward, sole * Vector3.up, sole, local);
    }

    internal static CharacterFootCurrentSupportObservation Support(Row r)
    {
        const string p = "foot/current-support/";
        const string t = p + "target/";
        CharacterFootSupportTarget target = default;
        if (r.B(t + "available"))
            target = new CharacterFootSupportTarget(r.U(t + "frame-sequence"),
                r.U(t + "completion-identity"), CharacterFootSide.Right,
                r.V(t + "position"), r.V(t + "support-normal"), r.I(t + "surface-identity"),
                r.U(t + "world-revision"), (CharacterFootSupportTargetKind)r.I(t + "kind"),
                (CharacterFootSupportPositionSource)r.I(t + "position-source"),
                r.U(t + "position-frame-sequence"), r.U(t + "position-completion-identity"),
                r.U(t + "position-event-identity"), r.U(t + "position-path-identity"),
                (CharacterFootSupportNormalSource)r.I(t + "normal-source"),
                r.U(t + "normal-frame-sequence"), r.U(t + "normal-completion-identity"),
                r.U(t + "normal-event-identity"));
        return new CharacterFootCurrentSupportObservation(r.U(p + "frame-sequence"),
            r.U(p + "completion-identity"), CharacterFootSide.Right, r.U(p + "world-revision"),
            Vector3.up, default, null, (CharacterFootCurrentSupportRejectReason)r.I(p + "reject-reason"),
            r.I(p + "accepted-sample-count"), r.F(p + "required-displacement"),
            r.I(p + "selected-sample-index"),
            (CharacterFootCurrentSupportSelectionReason)r.I(p + "selection-reason"), in target);
    }

    internal static CharacterFootMotionSettings Settings(string path)
    {
        string yaml = File.ReadAllText(path, Encoding.UTF8);
        var authoring = new CharacterFootMotionAuthoringSettings();
        foreach (FieldInfo field in typeof(CharacterFootMotionAuthoringSettings)
            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var matches = Regex.Matches(yaml, "^    " + field.Name + @": (.+)\r?$", RegexOptions.Multiline);
            Check(matches.Count == 1, "profile field " + field.Name);
            string value = matches[0].Groups[1].Value.Trim();
            object parsed = field.FieldType == typeof(bool) ? value == "1" :
                field.FieldType.IsEnum ? Enum.ToObject(field.FieldType, int.Parse(value)) :
                Convert.ChangeType(value, field.FieldType, CultureInfo.InvariantCulture);
            field.SetValue(authoring, parsed);
        }
        return authoring.Build();
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }


    internal static CharacterFootGroundPathLanding PathLanding(Row r, string name, string source) =>
        new CharacterFootGroundPathLanding(r.U("foot/ground-path/" + name + "-event-identity"),
            r.U("foot/ground-path/trajectory-generation"), r.S("foot/ground-path/" + source),
            r.I("foot/ground-path/" + name + "-surface-identity"),
            r.V("foot/ground-path/" + name), r.V("foot/ground-path/" + name + "-normal"));

    internal static CharacterFootGroundPathResult PathInput(Row r, JToken captured, JObject columns)
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
        Check(page.Contacts.SurfaceCoverage.Begin(input.Query,
            r.U("foot/ground-path/surface-coverage/world-revision")), "surface input");
        foreach (Row surface in Row.Table(captured, columns, "surfaces"))
            Check(page.Contacts.SurfaceCoverage.TryAdd(surface.I("surface-identity"),
                surface.I("face-identity"), surface.V2("start"), surface.V2("end")), "captured path preparation");
        page.Contacts.SurfaceCoverage.Complete();
        foreach (Row contact in Row.Table(captured, columns, "contacts"))
        {
            var value = new CharacterFootGroundContact(contact.I("segment-index"), contact.I("surface-identity"),
                contact.U("candidate-identity"), contact.V("position"), contact.V("normal"), contact.F("query-distance"));
            Check(page.Contacts.TryAdd(in value), "captured path preparation");
        }
        foreach (Row vertex in Row.Table(captured, columns, "envelope"))
            Check(page.Envelope.TryPush(vertex.V("position")), "captured path preparation");
        page.SetAccepted(r.I("foot/ground-path/segment-count"), in input);
        return new CharacterFootGroundPathResult(page, true);
    }


    internal sealed class Row
    {
        readonly Dictionary<string, string> values;
        Row(JToken values, JToken columns) => this.values = columns.Select((x, i) =>
            new KeyValuePair<string, string>((string)x, (string)values[i])).ToDictionary(x => x.Key, x => x.Value);
        internal static Row ReadMain(JToken frame, JObject columns) => new Row(frame["main"], columns["main"]);
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
