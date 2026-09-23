using System;
using KK.GeneratedDiagnosticSampling;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    public readonly struct CharacterPoseDiagnosticTarget
    {
        public CharacterPoseDiagnosticTarget(
            Guid runtimeInstanceId,
            string actorId,
            ulong poseInstanceId,
            string graphRevision,
            string resourceRevision)
        {
            if (runtimeInstanceId == Guid.Empty || string.IsNullOrWhiteSpace(actorId) ||
                poseInstanceId == 0 || string.IsNullOrWhiteSpace(graphRevision) ||
                string.IsNullOrWhiteSpace(resourceRevision))
                throw new ArgumentException("Pose diagnostic target is incomplete.");
            RuntimeInstanceId = runtimeInstanceId;
            ActorId = actorId;
            PoseInstanceId = poseInstanceId;
            GraphRevision = graphRevision;
            ResourceRevision = resourceRevision;
        }

        public Guid RuntimeInstanceId { get; }
        public string ActorId { get; }
        public ulong PoseInstanceId { get; }
        public string GraphRevision { get; }
        public string ResourceRevision { get; }
        public bool IsValid => RuntimeInstanceId != Guid.Empty && PoseInstanceId != 0;
    }

    [DiagnosticGroup("timing")]
    public readonly struct CharacterPoseDiagnosticFrame
    {
        internal CharacterPoseDiagnosticFrame(in CharacterPoseNativeFrameLineage lineage)
        {
            ActorId = lineage.ActorId.Value;
            FrameIdentity = lineage.FrameIdentity;
            CompletionIdentity = lineage.CompletionIdentity;
            PresentationFrame = lineage.PresentationFrame;
            BodyTick = lineage.BodyTick;
            GraphId = lineage.GraphId.Value;
            GraphRevision = lineage.GraphRevision;
            RigId = lineage.RigId;
            RigRevision = lineage.RigRevision;
            InputContractHash = lineage.InputContractHash;
            InstanceId = lineage.InstanceId;
            ResetGeneration = lineage.ResetGeneration;
        }

        [DiagnosticField] public string ActorId { get; }
        [DiagnosticField] public ulong FrameIdentity { get; }
        [DiagnosticField] public ulong CompletionIdentity { get; }
        [DiagnosticField] public ulong PresentationFrame { get; }
        [DiagnosticField] public ulong BodyTick { get; }
        [DiagnosticField] public string GraphId { get; }
        [DiagnosticField] public string GraphRevision { get; }
        [DiagnosticField] public string RigId { get; }
        [DiagnosticField] public string RigRevision { get; }
        [DiagnosticField] public string InputContractHash { get; }
        [DiagnosticField] public ulong InstanceId { get; }
        [DiagnosticField] public ulong ResetGeneration { get; }
    }
}
