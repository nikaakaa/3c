using System;
using BTSMTL.EventGraphs;
using UnityEngine;
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

    internal readonly struct CharacterPoseDiagnosticVariableBindings
    {
        internal CharacterPoseDiagnosticVariableBindings(CharacterAnimationVariableContract contract) : this()
        {
            if (!contract.TryGet("animation.lean.rotation", out _))
                return;
            HasLean = true;
            Rotation = contract.Bind("animation.lean.rotation");
            Eligible = contract.Bind("animation.lean.eligible");
            HorizontalSpeed = contract.Bind("animation.horizontal-speed");
            TurnRate = contract.Bind("animation.lean.turn-rate");
            TargetAngle = contract.Bind("animation.lean.target-angle");
            Angle = contract.Bind("animation.lean.angle");
            MovementDirection = contract.Bind("animation.movement-direction");
        }

        internal bool HasLean { get; }
        internal EventGraphVariableBinding Rotation { get; }
        internal EventGraphVariableBinding Eligible { get; }
        internal EventGraphVariableBinding HorizontalSpeed { get; }
        internal EventGraphVariableBinding TurnRate { get; }
        internal EventGraphVariableBinding TargetAngle { get; }
        internal EventGraphVariableBinding Angle { get; }
        internal EventGraphVariableBinding MovementDirection { get; }
    }

    [DiagnosticGroup("timing")]
    public readonly struct CharacterPoseDiagnosticFrame
    {
        internal CharacterPoseDiagnosticFrame(in CharacterPoseNativeFrameLineage lineage,
            CharacterAnimationVariableFrame variables, in CharacterPoseDiagnosticVariableBindings bindings)
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
            LeanAvailable = false;
            LeanEligible = false;
            LeanHorizontalSpeed = LeanTurnRate = LeanTargetAngle = LeanAngle = 0f;
            LeanMovementX = LeanMovementZ = LeanRotationX = LeanRotationY = LeanRotationZ = LeanRotationW = 0f;
            if (bindings.HasLean && variables.TryRead(bindings.Rotation, out EventGraphValue leanRotation))
            {
                LeanAvailable = true;
                LeanEligible = variables.Require(bindings.Eligible).As<bool>();
                LeanHorizontalSpeed = variables.Require(bindings.HorizontalSpeed).As<float>();
                LeanTurnRate = variables.Require(bindings.TurnRate).As<float>();
                LeanTargetAngle = variables.Require(bindings.TargetAngle).As<float>();
                LeanAngle = variables.Require(bindings.Angle).As<float>();
                Vector2 direction = variables.Require(bindings.MovementDirection).Vector2Value;
                LeanMovementX = direction.x;
                LeanMovementZ = direction.y;
                Quaternion rotation = leanRotation.QuaternionValue;
                LeanRotationX = rotation.x;
                LeanRotationY = rotation.y;
                LeanRotationZ = rotation.z;
                LeanRotationW = rotation.w;
            }
        }

        [DiagnosticField, DiagnosticGroup("lean")] public bool LeanAvailable { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public bool LeanEligible { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanHorizontalSpeed { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanMovementX { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanMovementZ { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanTurnRate { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanTargetAngle { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanAngle { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanRotationX { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanRotationY { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanRotationZ { get; }
        [DiagnosticField, DiagnosticGroup("lean")] public float LeanRotationW { get; }

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
