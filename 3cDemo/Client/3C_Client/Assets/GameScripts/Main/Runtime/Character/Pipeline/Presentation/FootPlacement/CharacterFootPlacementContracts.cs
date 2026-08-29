using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public enum CharacterFootSide : byte
    {
        Left = 1,
        Right = 2
    }

    public readonly struct CharacterFootPlacementAnimatedFootPose
    {
        public CharacterFootPlacementAnimatedFootPose(
            Vector3 hipPosition,
            Vector3 kneePosition,
            Vector3 anklePosition,
            Quaternion ankleRotation,
            Vector3 toePosition,
            Quaternion toeRotation,
            Vector3 heelPosition,
            Vector3 soleForward,
            Vector3 soleUp,
            Vector3 soleRight,
            Quaternion semanticRotation,
            Quaternion soleFrameLocalRotation,
            float rearProbeExtension,
            float lateralProbeExtent,
            float toeProbeExtension)
        {
            HipPosition = hipPosition;
            KneePosition = kneePosition;
            AnklePosition = anklePosition;
            AnkleRotation = ankleRotation;
            ToePosition = toePosition;
            ToeRotation = toeRotation;
            HeelPosition = heelPosition;
            SoleForward = soleForward;
            SoleUp = soleUp;
            SoleRight = soleRight;
            SemanticRotation = semanticRotation;
            SoleFrameLocalRotation = soleFrameLocalRotation;
            RearProbeExtension = rearProbeExtension;
            LateralProbeExtent = lateralProbeExtent;
            ToeProbeExtension = toeProbeExtension;
        }

        public Vector3 HipPosition { get; }
        public Vector3 KneePosition { get; }
        public Vector3 AnklePosition { get; }
        public Quaternion AnkleRotation { get; }
        public Vector3 ToePosition { get; }
        public Quaternion ToeRotation { get; }
        public Vector3 HeelPosition { get; }
        public Vector3 SoleForward { get; }
        public Vector3 SoleUp { get; }
        public Vector3 SoleRight { get; }
        public Quaternion SemanticRotation { get; }
        public Quaternion SoleFrameLocalRotation { get; }
        public float RearProbeExtension { get; }
        public float LateralProbeExtent { get; }
        public float ToeProbeExtension { get; }

        internal CharacterFootPlacementSoleContactPose ResolveSoleContacts(
            Vector3 anklePosition,
            Quaternion ankleRotation) =>
            CharacterFootPlacementSoleContactPose.Resolve(
                AnklePosition,
                AnkleRotation,
                HeelPosition,
                ToePosition,
                anklePosition,
                ankleRotation);
    }

    public readonly struct CharacterFootPlacementSoleContactPose
    {
        internal CharacterFootPlacementSoleContactPose(
            Vector3 heelPosition,
            Vector3 toePosition)
        {
            HeelPosition = heelPosition;
            ToePosition = toePosition;
        }

        public Vector3 HeelPosition { get; }
        public Vector3 ToePosition { get; }

        public static CharacterFootPlacementSoleContactPose Resolve(
            Vector3 sourceAnklePosition,
            Quaternion sourceAnkleRotation,
            Vector3 sourceHeelPosition,
            Vector3 sourceToePosition,
            Vector3 finalAnklePosition,
            Quaternion finalAnkleRotation)
        {
            Quaternion rotationDelta =
                (finalAnkleRotation * Quaternion.Inverse(sourceAnkleRotation)).normalized;
            return new CharacterFootPlacementSoleContactPose(
                finalAnklePosition +
                rotationDelta * (sourceHeelPosition - sourceAnklePosition),
                finalAnklePosition +
                rotationDelta * (sourceToePosition - sourceAnklePosition));
        }
    }

    public readonly struct CharacterFootPlacementAnimatedPose
    {
        public CharacterFootPlacementAnimatedPose(
            ulong renderFrame,
            Vector3 pelvisLocalPosition,
            CharacterFootPlacementAnimatedFootPose left,
            CharacterFootPlacementAnimatedFootPose right)
        {
            RenderFrame = renderFrame;
            PelvisLocalPosition = pelvisLocalPosition;
            Left = left;
            Right = right;
        }

        public ulong RenderFrame { get; }
        public Vector3 PelvisLocalPosition { get; }
        public CharacterFootPlacementAnimatedFootPose Left { get; }
        public CharacterFootPlacementAnimatedFootPose Right { get; }
    }

}
