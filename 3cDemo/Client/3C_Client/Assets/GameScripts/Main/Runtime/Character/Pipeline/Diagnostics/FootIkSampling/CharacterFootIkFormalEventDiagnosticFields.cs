using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string FormalEventsGroup = "formal-events";
        const string InputFormalEventsGroup = "input-formal-events";
        const string FormalCurrentAvailable =
            "character-foot-ik/main/formal-current-contact-event-available";
        const string FormalNextAvailable =
            "character-foot-ik/main/formal-next-landing-event-available";
        const string InputFormalCurrentAvailable =
            "character-foot-ik/main/input-formal-current-contact-event-available";
        const string InputFormalNextAvailable =
            "character-foot-ik/main/input-formal-next-landing-event-available";

        [DiagnosticField(Capability, "character-foot-ik/main/formal-event-phase", 1,
            DiagnosticValueKind.Int32, "category", Main, FormalEventsGroup)]
        internal static int FormalEventPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)FormalOutputEvents(in view, in metadata).Phase;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-event-approach-contact-to-landing-progress", 1,
            DiagnosticValueKind.Float32, "unitless", Main, FormalEventsGroup)]
        internal static float FormalEventApproachContactToLandingProgress(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputEvents(in view, in metadata).ApproachContactToLandingProgress;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-event-time-to-landing-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, FormalEventsGroup)]
        internal static float FormalEventTimeToLandingSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputEvents(in view, in metadata).TimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-in-approach-contact-to-landing", 1,
            DiagnosticValueKind.Boolean, "none", Main, FormalEventsGroup)]
        internal static bool FormalInApproachContactToLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputEvents(in view, in metadata).InApproachContactToLanding;

        [DiagnosticField(Capability, FormalCurrentAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, FormalEventsGroup)]
        internal static bool FormalCurrentContactEventAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputCurrent(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-current-contact-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, FormalEventsGroup)]
        internal static ulong FormalCurrentContactEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            AnimationFootMotionEventOccurrence value =
                FormalOutputCurrent(in view, in metadata);
            return value.IsBound ? value.Identity : 0;
        }

        [DiagnosticField(Capability, "character-foot-ik/main/formal-current-contact-event-ordinal", 1,
            DiagnosticValueKind.Int32, "count", Main, FormalEventsGroup)]
        internal static int FormalCurrentContactEventOrdinal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputCurrent(in view, in metadata).Ordinal;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-current-contact-event-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, FormalEventsGroup)]
        internal static int FormalCurrentContactEventCycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputCurrent(in view, in metadata).LandingCycle;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-current-contact-event-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalEventsGroup,
            AvailabilityFieldId = FormalCurrentAvailable)]
        internal static float FormalCurrentContactEventDistance(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputCurrent(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-current-contact-root-local-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, FormalEventsGroup,
            AvailabilityFieldId = FormalCurrentAvailable)]
        internal static DiagnosticVector3 FormalCurrentContactRootLocalLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(FormalOutputCurrent(in view, in metadata).RootLocalLanding);

        [DiagnosticField(Capability, FormalNextAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, FormalEventsGroup)]
        internal static bool FormalNextLandingEventAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputNext(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-next-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, FormalEventsGroup)]
        internal static ulong FormalNextLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            AnimationFootMotionEventOccurrence value =
                FormalOutputNext(in view, in metadata);
            return value.IsBound ? value.Identity : 0;
        }

        [DiagnosticField(Capability, "character-foot-ik/main/formal-next-landing-event-ordinal", 1,
            DiagnosticValueKind.Int32, "count", Main, FormalEventsGroup)]
        internal static int FormalNextLandingEventOrdinal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputNext(in view, in metadata).Ordinal;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-next-landing-event-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, FormalEventsGroup)]
        internal static int FormalNextLandingEventCycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputNext(in view, in metadata).LandingCycle;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-next-landing-event-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, FormalEventsGroup,
            AvailabilityFieldId = FormalNextAvailable)]
        internal static float FormalNextLandingEventDistance(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputNext(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/formal-next-root-local-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, FormalEventsGroup,
            AvailabilityFieldId = FormalNextAvailable)]
        internal static DiagnosticVector3 FormalNextRootLocalLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(FormalOutputNext(in view, in metadata).RootLocalLanding);

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-event-phase", 1,
            DiagnosticValueKind.Int32, "category", Main, InputFormalEventsGroup)]
        internal static int InputFormalEventPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            (int)FormalInputEvents(in view, in metadata).Phase;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-event-approach-contact-to-landing-progress", 1,
            DiagnosticValueKind.Float32, "unitless", Main, InputFormalEventsGroup)]
        internal static float InputFormalEventApproachContactToLandingProgress(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputEvents(in view, in metadata).ApproachContactToLandingProgress;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-event-time-to-landing-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, InputFormalEventsGroup)]
        internal static float InputFormalEventTimeToLandingSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputEvents(in view, in metadata).TimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-in-approach-contact-to-landing", 1,
            DiagnosticValueKind.Boolean, "none", Main, InputFormalEventsGroup)]
        internal static bool InputFormalInApproachContactToLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputEvents(in view, in metadata).InApproachContactToLanding;

        [DiagnosticField(Capability, InputFormalCurrentAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, InputFormalEventsGroup)]
        internal static bool InputFormalCurrentContactEventAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputCurrent(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-current-contact-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, InputFormalEventsGroup)]
        internal static ulong InputFormalCurrentContactEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            AnimationFootMotionEventOccurrence value =
                FormalInputCurrent(in view, in metadata);
            return value.IsBound ? value.Identity : 0;
        }

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-current-contact-event-ordinal", 1,
            DiagnosticValueKind.Int32, "count", Main, InputFormalEventsGroup)]
        internal static int InputFormalCurrentContactEventOrdinal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputCurrent(in view, in metadata).Ordinal;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-current-contact-event-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, InputFormalEventsGroup)]
        internal static int InputFormalCurrentContactEventCycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputCurrent(in view, in metadata).LandingCycle;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-current-contact-event-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, InputFormalEventsGroup,
            AvailabilityFieldId = InputFormalCurrentAvailable)]
        internal static float InputFormalCurrentContactEventDistance(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputCurrent(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-current-contact-root-local-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, InputFormalEventsGroup,
            AvailabilityFieldId = InputFormalCurrentAvailable)]
        internal static DiagnosticVector3 InputFormalCurrentContactRootLocalLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(FormalInputCurrent(in view, in metadata).RootLocalLanding);

        [DiagnosticField(Capability, InputFormalNextAvailable, 1,
            DiagnosticValueKind.Boolean, "none", Main, InputFormalEventsGroup)]
        internal static bool InputFormalNextLandingEventAvailable(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputNext(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-next-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, InputFormalEventsGroup)]
        internal static ulong InputFormalNextLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            AnimationFootMotionEventOccurrence value =
                FormalInputNext(in view, in metadata);
            return value.IsBound ? value.Identity : 0;
        }

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-next-landing-event-ordinal", 1,
            DiagnosticValueKind.Int32, "count", Main, InputFormalEventsGroup)]
        internal static int InputFormalNextLandingEventOrdinal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputNext(in view, in metadata).Ordinal;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-next-landing-event-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, InputFormalEventsGroup)]
        internal static int InputFormalNextLandingEventCycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputNext(in view, in metadata).LandingCycle;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-next-landing-event-distance", 1,
            DiagnosticValueKind.Float32, "metres", Main, InputFormalEventsGroup,
            AvailabilityFieldId = InputFormalNextAvailable)]
        internal static float InputFormalNextLandingEventDistance(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputNext(in view, in metadata).Distance;

        [DiagnosticField(Capability, "character-foot-ik/main/input-formal-next-root-local-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, InputFormalEventsGroup,
            AvailabilityFieldId = InputFormalNextAvailable)]
        internal static DiagnosticVector3 InputFormalNextRootLocalLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(FormalInputNext(in view, in metadata).RootLocalLanding);

        static AnimationFootMotionEventFrame FormalOutputEvents(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputSample(in view, in metadata).Events;

        static AnimationFootMotionEventOccurrence FormalOutputCurrent(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputEvents(in view, in metadata).CurrentContact;

        static AnimationFootMotionEventOccurrence FormalOutputNext(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalOutputEvents(in view, in metadata).NextLanding;

        static AnimationFootMotionEventFrame FormalInputEvents(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputSample(in view, in metadata).Events;

        static AnimationFootMotionEventOccurrence FormalInputCurrent(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputEvents(in view, in metadata).CurrentContact;

        static AnimationFootMotionEventOccurrence FormalInputNext(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            FormalInputEvents(in view, in metadata).NextLanding;
    }
}
