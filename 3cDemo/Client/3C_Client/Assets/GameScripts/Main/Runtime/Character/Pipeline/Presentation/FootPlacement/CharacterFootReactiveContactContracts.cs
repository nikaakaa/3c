using System;
using ThirdPersonCharacter.Pipeline.Animation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootGoalProposalSourceKind : byte
    {
        Predictive = 1,
        Reactive = 2
    }

    public enum CharacterFootReactiveContactRejectReason : byte
    {
        None = 0,
        InactivePhase = 1,
        WorldContextUnavailable = 2,
        InvalidRigGeometry = 3,
        InvalidRequest = 4,
        NoFootprintHit = 5,
        SelfColliderOnly = 6,
        InitialOverlapOnly = 7,
        InvalidSurfaceGeometry = 8,
        GroundAngleExceeded = 9,
        ContactOutsideAcquisitionRange = 10
    }

    public readonly struct CharacterFootGoalProposal
    {
        internal CharacterFootGoalProposal(
            CharacterFootGoalProposalSourceKind sourceKind,
            bool accepted,
            ulong frameSequence,
            ulong completionIdentity,
            string rigId,
            string rigRevision,
            CharacterFootSide side,
            ulong landingEventIdentity,
            ulong proposalRevision,
            int surfaceIdentity,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            Vector3 originalAnkle,
            Quaternion originalAnkleRotation,
            Vector3 originalSole,
            Vector3 targetAnkle,
            Quaternion targetAnkleRotation,
            Vector3 targetSole,
            bool hasRotationIntent,
            int sourceRejectReason)
        {
            SourceKind = sourceKind;
            Accepted = accepted;
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            Side = side;
            LandingEventIdentity = landingEventIdentity;
            ProposalRevision = proposalRevision;
            SurfaceIdentity = surfaceIdentity;
            SurfacePoint = surfacePoint;
            SurfaceNormal = surfaceNormal;
            OriginalAnkle = originalAnkle;
            OriginalAnkleRotation = originalAnkleRotation;
            OriginalSole = originalSole;
            TargetAnkle = targetAnkle;
            TargetAnkleRotation = targetAnkleRotation;
            TargetSole = targetSole;
            HasRotationIntent = hasRotationIntent;
            SourceRejectReason = sourceRejectReason;
        }

        public CharacterFootGoalProposalSourceKind SourceKind { get; }
        public bool Accepted { get; }
        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public CharacterFootSide Side { get; }
        public ulong LandingEventIdentity { get; }
        public ulong ProposalRevision { get; }
        public int SurfaceIdentity { get; }
        public Vector3 SurfacePoint { get; }
        public Vector3 SurfaceNormal { get; }
        public Vector3 OriginalAnkle { get; }
        public Quaternion OriginalAnkleRotation { get; }
        public Vector3 OriginalSole { get; }
        public Vector3 TargetAnkle { get; }
        public Quaternion TargetAnkleRotation { get; }
        public Vector3 TargetSole { get; }
        public bool HasRotationIntent { get; }
        public int SourceRejectReason { get; }
        public Vector3 PositionCorrection => TargetAnkle - OriginalAnkle;
        public Quaternion RotationCorrection =>
            (TargetAnkleRotation * Quaternion.Inverse(OriginalAnkleRotation)).normalized;
    }

    public readonly struct CharacterFootReactiveContactSettings
    {
        public CharacterFootReactiveContactSettings(
            float footHeight,
            float forwardBias,
            float footprintLength,
            float footprintWidth,
            float maximumIkCorrection,
            float correctionCastMargin,
            float maximumFootCorrection,
            float maximumBodyCorrection,
            int layerMask,
            QueryTriggerInteraction triggerInteraction,
            float maximumGroundDetectionAngleDegrees,
            float maximumFootAdaptationAngleDegrees,
            float normalRepairRadius,
            float maximumRepairSeparation,
            float maximumAcquisitionDistance,
            float contactPointDeadZone,
            float contactNormalDeadZoneDegrees)
        {
            FootHeight = footHeight;
            ForwardBias = forwardBias;
            FootprintLength = footprintLength;
            FootprintWidth = footprintWidth;
            MaximumIkCorrection = maximumIkCorrection;
            CorrectionCastMargin = correctionCastMargin;
            MaximumFootCorrection = maximumFootCorrection;
            MaximumBodyCorrection = maximumBodyCorrection;
            LayerMask = layerMask;
            TriggerInteraction = triggerInteraction;
            MaximumGroundDetectionAngleDegrees = maximumGroundDetectionAngleDegrees;
            MaximumFootAdaptationAngleDegrees = maximumFootAdaptationAngleDegrees;
            NormalRepairRadius = normalRepairRadius;
            MaximumRepairSeparation = maximumRepairSeparation;
            MaximumAcquisitionDistance = maximumAcquisitionDistance;
            ContactPointDeadZone = contactPointDeadZone;
            ContactNormalDeadZoneDegrees = contactNormalDeadZoneDegrees;
            RequireValid();
        }

        public float FootHeight { get; }
        public float ForwardBias { get; }
        public float FootprintLength { get; }
        public float FootprintWidth { get; }
        public float MaximumIkCorrection { get; }
        public float CorrectionCastMargin { get; }
        public float MaximumFootCorrection { get; }
        public float MaximumBodyCorrection { get; }
        public int LayerMask { get; }
        public QueryTriggerInteraction TriggerInteraction { get; }
        public float MaximumGroundDetectionAngleDegrees { get; }
        public float MaximumFootAdaptationAngleDegrees { get; }
        public float NormalRepairRadius { get; }
        public float MaximumRepairSeparation { get; }
        public float MaximumAcquisitionDistance { get; }
        public float ContactPointDeadZone { get; }
        public float ContactNormalDeadZoneDegrees { get; }

        internal void RequireValid()
        {
            if (!float.IsFinite(FootHeight) || FootHeight <= 0f ||
                !float.IsFinite(ForwardBias) ||
                !float.IsFinite(FootprintLength) || FootprintLength <= 0f ||
                !float.IsFinite(FootprintWidth) || FootprintWidth <= 0f ||
                !float.IsFinite(MaximumIkCorrection) || MaximumIkCorrection < 0f ||
                !float.IsFinite(CorrectionCastMargin) || CorrectionCastMargin < 0f ||
                !float.IsFinite(MaximumFootCorrection) || MaximumFootCorrection < 0f ||
                !float.IsFinite(MaximumBodyCorrection) || MaximumBodyCorrection < 0f ||
                LayerMask == 0 ||
                !float.IsFinite(MaximumGroundDetectionAngleDegrees) ||
                MaximumGroundDetectionAngleDegrees < 0f ||
                MaximumGroundDetectionAngleDegrees > 90f ||
                !float.IsFinite(MaximumFootAdaptationAngleDegrees) ||
                MaximumFootAdaptationAngleDegrees < 0f ||
                MaximumFootAdaptationAngleDegrees > MaximumGroundDetectionAngleDegrees ||
                !float.IsFinite(NormalRepairRadius) || NormalRepairRadius <= 0f ||
                !float.IsFinite(MaximumRepairSeparation) || MaximumRepairSeparation <= 0f ||
                !float.IsFinite(MaximumAcquisitionDistance) || MaximumAcquisitionDistance <= 0f ||
                !float.IsFinite(ContactPointDeadZone) || ContactPointDeadZone < 0f ||
                ContactPointDeadZone >= MaximumAcquisitionDistance ||
                !float.IsFinite(ContactNormalDeadZoneDegrees) ||
                ContactNormalDeadZoneDegrees < 0f || ContactNormalDeadZoneDegrees > 90f)
            {
                throw new ArgumentException("Reactive Foot Contact settings are invalid.");
            }
        }
    }

    public readonly struct CharacterFootReactiveContactRequest
    {
        public CharacterFootReactiveContactRequest(
            ulong frameSequence,
            ulong completionIdentity,
            string rigId,
            string rigRevision,
            CharacterFootSide side,
            ulong landingEventIdentity,
            bool activePhase,
            Vector3 poseRootPosition,
            Quaternion poseRootRotation,
            Vector3 componentUp,
            Vector3 originalAnklePosition,
            Quaternion originalAnkleRotation,
            Vector3 originalSolePosition)
        {
            FrameSequence = frameSequence;
            CompletionIdentity = completionIdentity;
            RigId = rigId ?? string.Empty;
            RigRevision = rigRevision ?? string.Empty;
            Side = side;
            LandingEventIdentity = landingEventIdentity;
            ActivePhase = activePhase;
            PoseRootPosition = poseRootPosition;
            PoseRootRotation = poseRootRotation;
            ComponentUp = componentUp;
            OriginalAnklePosition = originalAnklePosition;
            OriginalAnkleRotation = originalAnkleRotation;
            OriginalSolePosition = originalSolePosition;
        }

        public ulong FrameSequence { get; }
        public ulong CompletionIdentity { get; }
        public string RigId { get; }
        public string RigRevision { get; }
        public CharacterFootSide Side { get; }
        public ulong LandingEventIdentity { get; }
        public bool ActivePhase { get; }
        public Vector3 PoseRootPosition { get; }
        public Quaternion PoseRootRotation { get; }
        public Vector3 ComponentUp { get; }
        public Vector3 OriginalAnklePosition { get; }
        public Quaternion OriginalAnkleRotation { get; }
        public Vector3 OriginalSolePosition { get; }

        internal bool IsValid =>
            CompletionIdentity != 0 &&
            !string.IsNullOrWhiteSpace(RigId) &&
            !string.IsNullOrWhiteSpace(RigRevision) &&
            (Side == CharacterFootSide.Left || Side == CharacterFootSide.Right) &&
            IsFinite(PoseRootPosition) &&
            IsFinite(PoseRootRotation) &&
            IsFinite(ComponentUp) &&
            ComponentUp.sqrMagnitude > 0.000001f &&
            IsFinite(OriginalAnklePosition) &&
            IsFinite(OriginalAnkleRotation) &&
            IsFinite(OriginalSolePosition);

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) &&
            float.IsFinite(value.z) && float.IsFinite(value.w) &&
            value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w > 0.000001f;
    }

    public readonly struct CharacterFootReactiveContactProposal
    {
        internal CharacterFootReactiveContactProposal(
            CharacterFootReactiveContactRejectReason rejectReason,
            in CharacterFootGoalProposal goal,
            float correctionAlongUp,
            float clearanceAlongUp,
            Vector3 footprintOrigin,
            Vector3 footprintHalfExtents,
            Quaternion footprintRotation,
            float castDistance,
            float queryDistance,
            bool usedNormalRepair)
        {
            RejectReason = rejectReason;
            Goal = goal;
            CorrectionAlongUp = correctionAlongUp;
            ClearanceAlongUp = clearanceAlongUp;
            FootprintOrigin = footprintOrigin;
            FootprintHalfExtents = footprintHalfExtents;
            FootprintRotation = footprintRotation;
            CastDistance = castDistance;
            QueryDistance = queryDistance;
            UsedNormalRepair = usedNormalRepair;
        }

        public CharacterFootReactiveContactRejectReason RejectReason { get; }
        public bool Accepted => RejectReason == CharacterFootReactiveContactRejectReason.None && Goal.Accepted;
        public CharacterFootGoalProposal Goal { get; }
        public float CorrectionAlongUp { get; }
        public float ClearanceAlongUp { get; }
        public Vector3 FootprintOrigin { get; }
        public Vector3 FootprintHalfExtents { get; }
        public Quaternion FootprintRotation { get; }
        public float CastDistance { get; }
        public float QueryDistance { get; }
        public bool UsedNormalRepair { get; }
    }
}
