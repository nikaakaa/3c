using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static partial class CharacterFootIkDiagnosticFields
    {
        const string ResolvedContactGroup = "resolved-contact";

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-contact-available", 1, DiagnosticValueKind.Boolean, "none", Main, ResolvedContactGroup)]
        internal static bool ResolvedContactAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Contact.Available;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-contact-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedContactGroup)]
        internal static ulong ResolvedContactEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Contact.EventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-contact-point", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedContactGroup, AvailabilityFieldId = "character-foot-ik/main/resolved-contact-available")]
        internal static DiagnosticVector3 ResolvedContactPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Resolved(in view, in metadata).Contact.Point);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-contact-ownership", 1, DiagnosticValueKind.Float32, "unitless", Main, ResolvedContactGroup)]
        internal static float ResolvedContactOwnership(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Contact.Ownership;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-eligibility", 1, DiagnosticValueKind.Int32, "category", Main, ResolvedContactGroup)]
        internal static int ResolvedSupportEligibility(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => (int)Resolved(in view, in metadata).Support.Eligibility;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, ResolvedContactGroup)]
        internal static float ResolvedSupportWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Support.Weight;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-intent-weight", 1, DiagnosticValueKind.Float32, "unitless", Main, ResolvedContactGroup)]
        internal static float ResolvedSupportIntentWeight(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Support.Weight;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-horizontal-error", 1, DiagnosticValueKind.Float32, "metres", Main, ResolvedContactGroup)]
        internal static float ResolvedSupportHorizontalError(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Support.HorizontalError;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-support-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedContactGroup)]
        internal static ulong ResolvedSupportEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Support.EventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-pelvis-reach-available", 1, DiagnosticValueKind.Boolean, "none", Main, ResolvedContactGroup)]
        internal static bool ResolvedPelvisReachAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Reach.PelvisAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-pelvis-reach-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedContactGroup)]
        internal static ulong ResolvedPelvisReachEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Reach.PelvisEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-pelvis-reach-point", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedContactGroup, AvailabilityFieldId = "character-foot-ik/main/resolved-pelvis-reach-available")]
        internal static DiagnosticVector3 ResolvedPelvisReachPoint(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Resolved(in view, in metadata).Reach.PelvisPoint);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-landing-reach-available", 1, DiagnosticValueKind.Boolean, "none", Main, ResolvedContactGroup)]
        internal static bool ResolvedLandingReachAvailable(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Reach.LandingAvailable;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-landing-reach-event-identity", 1, DiagnosticValueKind.UInt64, "identity", Main, ResolvedContactGroup)]
        internal static ulong ResolvedLandingReachEventIdentity(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Reach.LandingEventIdentity;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-landing-reach-hip", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedContactGroup, AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
        internal static DiagnosticVector3 ResolvedLandingReachHip(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Resolved(in view, in metadata).Reach.LandingHip);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-landing-reach-target-ankle", 1, DiagnosticValueKind.Vector3, "metres", Main, ResolvedContactGroup, AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
        internal static DiagnosticVector3 ResolvedLandingReachTargetAnkle(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Vector3(Resolved(in view, in metadata).Reach.LandingTargetAnkle);

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-landing-reach-leg-length", 1, DiagnosticValueKind.Float32, "metres", Main, ResolvedContactGroup, AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
        internal static float ResolvedLandingReachLegLength(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Reach.LandingLegLength;

        [DiagnosticField(Capability, "character-foot-ik/main/resolved-landing-reach-minimum-compression-reserve", 1, DiagnosticValueKind.Float32, "metres", Main, ResolvedContactGroup, AvailabilityFieldId = "character-foot-ik/main/resolved-landing-reach-available")]
        internal static float ResolvedLandingReachMinimumCompressionReserve(in CharacterFootIkCommittedCaptureViewLease view, in CharacterFootIkCaptureMetadata metadata) => Resolved(in view, in metadata).Reach.LandingMinimumCompressionReserve;
    }
}
