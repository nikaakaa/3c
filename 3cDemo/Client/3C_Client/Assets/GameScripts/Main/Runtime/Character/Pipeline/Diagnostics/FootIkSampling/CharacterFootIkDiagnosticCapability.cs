using System;
using KK.GeneratedDiagnosticSampling;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling
{
    internal static class CharacterFootIkDiagnosticIdentity
    {
        internal const string CapabilityId = "character-foot-ik";
        internal const int CapabilityRevision = 1;
        internal const string LineageTypeIdentity =
            "character-foot-ik-lineage/1";
    }

    [DiagnosticLifecycleEvent(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        "character-foot-ik/capture-started",
        DiagnosticLifecycleEventKind.CaptureStarted)]
    internal readonly struct CharacterFootIkCaptureStartedEvent
    {
        internal CharacterFootIkCaptureStartedEvent(
            DiagnosticCaptureStartRequest request)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
        }

        [DiagnosticLifecyclePayload(DiagnosticLifecyclePayloadKind.StartRequest)]
        internal DiagnosticCaptureStartRequest Request { get; }
    }

    [DiagnosticLifecycleEvent(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        "character-foot-ik/committed-sample",
        DiagnosticLifecycleEventKind.CommittedSample)]
    internal readonly struct CharacterFootIkCommittedSampleEvent
    {
        internal CharacterFootIkCommittedSampleEvent(
            in CharacterFootIkCommittedCaptureViewLease view,
            in CharacterFootIkCaptureMetadata left,
            in CharacterFootIkCaptureMetadata right)
        {
            if (left.Side != CharacterFootSide.Left ||
                right.Side != CharacterFootSide.Right)
            {
                throw new ArgumentException(
                    "Foot IK committed sample dimensions are invalid.");
            }
            View = view;
            Lineage = CharacterFootIkDiagnosticCapability.CreateLineage(in view);
            Left = left;
            Right = right;
        }

        [DiagnosticLifecyclePayload(DiagnosticLifecyclePayloadKind.CommittedView)]
        internal CharacterFootIkCommittedCaptureViewLease View { get; }

        [DiagnosticLifecyclePayload(DiagnosticLifecyclePayloadKind.Lineage)]
        internal DiagnosticLineageKey Lineage { get; }

        [DiagnosticLifecyclePayload(
            DiagnosticLifecyclePayloadKind.SampleDimension,
            "character-foot-ik/left")]
        internal CharacterFootIkCaptureMetadata Left { get; }

        [DiagnosticLifecyclePayload(
            DiagnosticLifecyclePayloadKind.SampleDimension,
            "character-foot-ik/right")]
        internal CharacterFootIkCaptureMetadata Right { get; }
    }

    [DiagnosticLifecycleEvent(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        "character-foot-ik/capture-stopped",
        DiagnosticLifecycleEventKind.CaptureStopped)]
    internal readonly struct CharacterFootIkCaptureStoppedEvent
    {
        internal CharacterFootIkCaptureStoppedEvent(
            in DiagnosticCaptureStopOutcome outcome)
        {
            Outcome = outcome;
        }

        [DiagnosticLifecyclePayload(DiagnosticLifecyclePayloadKind.StopOutcome)]
        internal DiagnosticCaptureStopOutcome Outcome { get; }
    }

    [DiagnosticCapability(
        CharacterFootIkDiagnosticIdentity.CapabilityId,
        CharacterFootIkDiagnosticIdentity.CapabilityRevision,
        typeof(CharacterFootIkCommittedCaptureViewLease),
        typeof(CharacterFootIkCaptureMetadata))]
    internal static class CharacterFootIkDiagnosticCapability
    {
        internal static DiagnosticLineageKey CreateLineage(
            in CharacterFootIkCommittedCaptureViewLease view)
        {
            CharacterPoseFrameLineage lineage = view.Lineage;
            if (!lineage.IsValid)
            {
                throw new ArgumentException(
                    "Foot IK diagnostic lineage is invalid.",
                    nameof(view));
            }
            var diagnosticLineage = new DiagnosticLineageKey(
                CharacterFootIkDiagnosticIdentity.LineageTypeIdentity,
                lineage.FrameIdentity,
                lineage.CompletionIdentity);
            return diagnosticLineage;
        }
    }

    internal readonly struct CharacterFootIkCaptureMetadata
    {
        internal CharacterFootIkCaptureMetadata(
            Guid sampleIdentity,
            DateTime startedUtc,
            in AnimationPresentationProgramIdentity program,
            Guid targetRuntimeInstanceId,
            int targetHostInstanceId,
            CharacterFootSide side,
            in CharacterFootIkCommittedCaptureViewLease view)
        {
            if (sampleIdentity == Guid.Empty ||
                startedUtc.Kind != DateTimeKind.Utc ||
                !program.IsValid ||
                targetRuntimeInstanceId == Guid.Empty ||
                targetHostInstanceId == 0 ||
                !view.IsAvailable ||
                (side != CharacterFootSide.Left &&
                 side != CharacterFootSide.Right))
            {
                throw new ArgumentException(
                    "Foot IK capture metadata is invalid.");
            }
            SampleIdentity = sampleIdentity.ToString("N");
            StartedUtcTicks = startedUtc.Ticks;
            ProgramIdentity =
                $"{program.ProjectionRevision}|{program.PosePlanHash}";
            TargetRuntimeInstanceId =
                targetRuntimeInstanceId.ToString("N");
            TargetHostInstanceId = targetHostInstanceId;
            Side = side;
            MotionCoreDerived =
                CharacterFootIkMotionCoreDerivedFacts.Resolve(in view, side);
        }

        internal string SampleIdentity { get; }
        internal long StartedUtcTicks { get; }
        internal string ProgramIdentity { get; }
        internal string TargetRuntimeInstanceId { get; }
        internal int TargetHostInstanceId { get; }
        internal CharacterFootSide Side { get; }
        internal CharacterFootIkMotionCoreDerivedFacts MotionCoreDerived { get; }
    }
}
