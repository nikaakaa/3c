using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string SelectedStepGroup = "selected-step";
        const string CurrentStepGroup = "current-step";
        const string IncomingStepGroup = "incoming-step";
        const string RootLandingGroup = "root-landing";

        [DiagnosticField(Capability, "character-foot-ik/main/selected-step-event-phase", 1,
            DiagnosticValueKind.Float32, "unitless", Main, SelectedStepGroup)]
        internal static float SelectedStepEventPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedStep(in view, in metadata).EventPhase;

        [DiagnosticField(Capability, "character-foot-ik/main/selected-step-approach-contact-to-landing-progress", 1,
            DiagnosticValueKind.Float32, "unitless", Main, SelectedStepGroup)]
        internal static float SelectedStepApproachContactToLandingProgress(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedStep(in view, in metadata).ApproachContactToLandingProgress;

        [DiagnosticField(Capability, "character-foot-ik/main/selected-step-landing-phase", 1,
            DiagnosticValueKind.Float32, "unitless", Main, SelectedStepGroup)]
        internal static float SelectedStepLandingPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedStep(in view, in metadata).LandingPhase;

        [DiagnosticField(Capability, "character-foot-ik/main/selected-step-at-or-after-approach-contact", 1,
            DiagnosticValueKind.Boolean, "none", Main, SelectedStepGroup)]
        internal static bool SelectedStepAtOrAfterApproachContact(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedStep(in view, in metadata).AtOrAfterApproachContact;

        [DiagnosticField(Capability, "character-foot-ik/main/selected-step-in-approach-contact-to-landing", 1,
            DiagnosticValueKind.Boolean, "none", Main, SelectedStepGroup)]
        internal static bool SelectedStepInApproachContactToLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            SelectedStep(in view, in metadata).InApproachContactToLanding;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-is-valid", 1,
            DiagnosticValueKind.Boolean, "none", Main, CurrentStepGroup)]
        internal static bool CurrentStepIsValid(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-is-authoritative", 1,
            DiagnosticValueKind.Boolean, "none", Main, CurrentStepGroup)]
        internal static bool CurrentStepIsAuthoritative(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).IsAuthoritative;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-has-consistent-landing-event-identity", 1,
            DiagnosticValueKind.Boolean, "none", Main, CurrentStepGroup)]
        internal static bool CurrentStepHasConsistentLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).HasConsistentLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-is-pre-swing", 1,
            DiagnosticValueKind.Boolean, "none", Main, CurrentStepGroup)]
        internal static bool CurrentStepIsPreSwing(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).IsPreSwing;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-is-swing", 1,
            DiagnosticValueKind.Boolean, "none", Main, CurrentStepGroup)]
        internal static bool CurrentStepIsSwing(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).IsSwing;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-event-ordinal", 1,
            DiagnosticValueKind.Int32, "count", Main, CurrentStepGroup)]
        internal static int CurrentStepEventOrdinal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).EventOrdinal;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-source-landing-cycle-offset", 1,
            DiagnosticValueKind.Int32, "count", Main, CurrentStepGroup)]
        internal static int CurrentStepSourceLandingCycleOffset(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).SourceLandingCycleOffset;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-source-sample-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, CurrentStepGroup)]
        internal static int CurrentStepSourceSampleCycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).SourceSampleCycle;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-contribution-continuity-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, CurrentStepGroup)]
        internal static ulong CurrentStepContributionContinuityIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).ContributionContinuityIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, CurrentStepGroup)]
        internal static ulong CurrentStepLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).LandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-time-to-landing-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, CurrentStepGroup)]
        internal static float CurrentStepTimeToLandingSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).TimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-event-phase", 1,
            DiagnosticValueKind.Float32, "unitless", Main, CurrentStepGroup)]
        internal static float CurrentStepEventPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).EventPhase;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-approach-contact-to-landing-progress", 1,
            DiagnosticValueKind.Float32, "unitless", Main, CurrentStepGroup)]
        internal static float CurrentStepApproachContactToLandingProgress(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).ApproachContactToLandingProgress;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-landing-phase", 1,
            DiagnosticValueKind.Float32, "unitless", Main, CurrentStepGroup)]
        internal static float CurrentStepLandingPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).LandingPhase;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-at-or-after-approach-contact", 1,
            DiagnosticValueKind.Boolean, "none", Main, CurrentStepGroup)]
        internal static bool CurrentStepAtOrAfterApproachContact(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).AtOrAfterApproachContact;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-in-approach-contact-to-landing", 1,
            DiagnosticValueKind.Boolean, "none", Main, CurrentStepGroup)]
        internal static bool CurrentStepInApproachContactToLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            CurrentStep(in view, in metadata).InApproachContactToLanding;

        [DiagnosticField(Capability, "character-foot-ik/main/current-step-root-local-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, CurrentStepGroup)]
        internal static DiagnosticVector3 CurrentStepRootLocalLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(CurrentStep(in view, in metadata).RootLocalLanding);

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-is-valid", 1,
            DiagnosticValueKind.Boolean, "none", Main, IncomingStepGroup)]
        internal static bool IncomingStepIsValid(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).IsValid;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-is-authoritative", 1,
            DiagnosticValueKind.Boolean, "none", Main, IncomingStepGroup)]
        internal static bool IncomingStepIsAuthoritative(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).IsAuthoritative;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-has-consistent-landing-event-identity", 1,
            DiagnosticValueKind.Boolean, "none", Main, IncomingStepGroup)]
        internal static bool IncomingStepHasConsistentLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).HasConsistentLandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-is-pre-swing", 1,
            DiagnosticValueKind.Boolean, "none", Main, IncomingStepGroup)]
        internal static bool IncomingStepIsPreSwing(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).IsPreSwing;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-is-swing", 1,
            DiagnosticValueKind.Boolean, "none", Main, IncomingStepGroup)]
        internal static bool IncomingStepIsSwing(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).IsSwing;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-event-ordinal", 1,
            DiagnosticValueKind.Int32, "count", Main, IncomingStepGroup)]
        internal static int IncomingStepEventOrdinal(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).EventOrdinal;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-source-landing-cycle-offset", 1,
            DiagnosticValueKind.Int32, "count", Main, IncomingStepGroup)]
        internal static int IncomingStepSourceLandingCycleOffset(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).SourceLandingCycleOffset;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-source-sample-cycle", 1,
            DiagnosticValueKind.Int32, "count", Main, IncomingStepGroup)]
        internal static int IncomingStepSourceSampleCycle(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).SourceSampleCycle;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-contribution-continuity-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IncomingStepGroup)]
        internal static ulong IncomingStepContributionContinuityIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).ContributionContinuityIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-landing-event-identity", 1,
            DiagnosticValueKind.UInt64, "identity", Main, IncomingStepGroup)]
        internal static ulong IncomingStepLandingEventIdentity(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).LandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-time-to-landing-seconds", 1,
            DiagnosticValueKind.Float32, "seconds", Main, IncomingStepGroup)]
        internal static float IncomingStepTimeToLandingSeconds(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).TimeToLandingSeconds;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-event-phase", 1,
            DiagnosticValueKind.Float32, "unitless", Main, IncomingStepGroup)]
        internal static float IncomingStepEventPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).EventPhase;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-approach-contact-to-landing-progress", 1,
            DiagnosticValueKind.Float32, "unitless", Main, IncomingStepGroup)]
        internal static float IncomingStepApproachContactToLandingProgress(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).ApproachContactToLandingProgress;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-landing-phase", 1,
            DiagnosticValueKind.Float32, "unitless", Main, IncomingStepGroup)]
        internal static float IncomingStepLandingPhase(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).LandingPhase;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-at-or-after-approach-contact", 1,
            DiagnosticValueKind.Boolean, "none", Main, IncomingStepGroup)]
        internal static bool IncomingStepAtOrAfterApproachContact(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).AtOrAfterApproachContact;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-in-approach-contact-to-landing", 1,
            DiagnosticValueKind.Boolean, "none", Main, IncomingStepGroup)]
        internal static bool IncomingStepInApproachContactToLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            IncomingStep(in view, in metadata).InApproachContactToLanding;

        [DiagnosticField(Capability, "character-foot-ik/main/incoming-step-root-local-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, IncomingStepGroup)]
        internal static DiagnosticVector3 IncomingStepRootLocalLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(IncomingStep(in view, in metadata).RootLocalLanding);

        [DiagnosticField(Capability, "character-foot-ik/main/root-local-landing", 1,
            DiagnosticValueKind.Vector3, "metres", Main, RootLandingGroup)]
        internal static DiagnosticVector3 RootLocalLanding(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Vector3(Foot(in view, in metadata).RootLocalLanding);

        static CharacterFootStepCandidateDiagnostics SelectedStep(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata)
        {
            CharacterFootStepCandidateSelectionDiagnostics selection =
                Foot(in view, in metadata).StepCandidateSelection;
            return selection.SelectedSource ==
                   CharacterFootLandingStepSource.FormalNextLanding
                ? selection.Current
                : default;
        }

        static CharacterFootStepCandidateDiagnostics CurrentStep(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).StepCandidateSelection.Current;

        static CharacterFootStepCandidateDiagnostics IncomingStep(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata metadata) =>
            Foot(in view, in metadata).StepCandidateSelection.Incoming;
    }
}
