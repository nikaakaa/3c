using System;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public interface ILocomotionInputMotionAuthoring
    {
        float MoveSpeed { get; }
        LocomotionInputMotionDisplacementMode DisplacementMode { get; }
        RootMotionCurveAsset ActionMotionCurve { get; }
        float TurnSpeedDegrees { get; }
        bool CameraRelative { get; }
        LocomotionInputMotionExecutionMode ExecutionMode { get; }
        float DurationSeconds { get; }
    }

    public interface ICharacterInputValueAuthoring
    {
        string InputId { get; }
    }

    public interface ICharacterActionRequestAuthoring
    {
        string RequestId { get; }
    }

    public interface IActionContextAuthoring
    {
        ActionContextSlot ActionContext { get; }
    }

    public interface IActionWindowAuthoring
    {
        string WindowType { get; }
    }

    public interface ICanActivateActionAuthoring
    {
        GameplayAbilityAdmissionProfile AdmissionProfile { get; }
        string TargetSnapshotDeclarationId { get; }
        string TargetSnapshotOwnerId { get; }
    }

    public interface IGameplayTagAuthoring
    {
        GameplayTagId Tag { get; }
    }

    public interface IGameplayTagQueryAuthoring
    {
        GameplayTagQuery Query { get; }
    }

    public interface IGameplayAttributeAuthoring
    {
        GameplayAttributeId Attribute { get; }
    }

    public interface IGameplayEffectApplicationAuthoring : IActionContextAuthoring
    {
        GameplayEffectDefinition Effect { get; }
        bool Predicted { get; }
    }

    public interface IGameplayEffectRemovalAuthoring
    {
        GameplayEffectRemoveSelector Selector { get; }
        GameplayEffectDefinition Effect { get; }
        GameplayTagQuery EffectTagQuery { get; }
    }

    public interface ICharacterBlackboardAuthoring
    {
        string DeclarationId { get; }
        string OwnerId { get; }
        System.Type ValueType { get; }
        bool Writes { get; }
    }

    public static class CharacterInputAuthoringRules
    {
        public static string RequireInputId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Input authoring requires a declared input identity.", nameof(value));
            return value.Trim();
        }
    }

    public static class CharacterActionAuthoringRules
    {
        public static void ValidateTargetSnapshot(string declarationId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(declarationId) != string.IsNullOrWhiteSpace(ownerId))
                throw new ArgumentException("Target snapshot declaration and owner must be specified together.");
        }

        public static string RequireWindowType(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Action window requires a declared window type.", nameof(value));
            return value.Trim();
        }
    }

    public static class GameplayAuthoringRules
    {
        public static GameplayTagId RequireTag(GameplayTagId value)
        {
            if (!value.IsValid)
                throw new ArgumentException("Gameplay Tag authoring requires a valid tag identity.", nameof(value));
            return value;
        }

        public static GameplayAttributeId RequireAttribute(GameplayAttributeId value)
        {
            if (!value.IsValid)
                throw new ArgumentException("Gameplay Attribute authoring requires a valid attribute identity.", nameof(value));
            return value;
        }

        public static GameplayTagQuery RequireTagQuery(GameplayTagQuery value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            return value;
        }

        public static GameplayEffectDefinition RequireEffect(GameplayEffectDefinition value)
        {
            if (!value)
                throw new ArgumentNullException(nameof(value));
            return value;
        }

        public static void ValidateEffectRemoval(
            GameplayEffectRemoveSelector selector,
            GameplayEffectDefinition effect,
            GameplayTagQuery query)
        {
            if (!Enum.IsDefined(typeof(GameplayEffectRemoveSelector), selector))
                throw new ArgumentOutOfRangeException(nameof(selector));
            if (selector == GameplayEffectRemoveSelector.EffectId && !effect)
                throw new ArgumentException("EffectId removal requires an effect definition.", nameof(effect));
            if (selector == GameplayEffectRemoveSelector.EffectTagQuery && query == null)
                throw new ArgumentNullException(nameof(query));
        }
    }

    public static class LocomotionInputMotionAuthoringRules
    {
        public const float DefaultMoveSpeed = 4f;
        public const float DefaultTurnSpeedDegrees = 720f;
        public const bool DefaultCameraRelative = true;
        public const LocomotionInputMotionDisplacementMode DefaultDisplacementMode =
            LocomotionInputMotionDisplacementMode.ConstantSpeed;
        public const LocomotionInputMotionExecutionMode DefaultExecutionMode =
            LocomotionInputMotionExecutionMode.Once;
        public const float DefaultDurationSeconds = 0f;
        public const string DefaultMoveSpeedText = "4";
        public const string DefaultDisplacementModeText = "ConstantSpeed";
        public const string DefaultTurnSpeedDegreesText = "720";
        public const string DefaultCameraRelativeText = "true";
        public const string DefaultExecutionModeText = "Once";
        public const string DefaultDurationSecondsText = "0";

        public static void Validate(ILocomotionInputMotionAuthoring node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            Validate(
                node.MoveSpeed,
                node.DisplacementMode,
                node.ActionMotionCurve,
                node.TurnSpeedDegrees,
                node.ExecutionMode,
                node.DurationSeconds);
        }

        public static void Validate(
            float moveSpeed,
            LocomotionInputMotionDisplacementMode displacementMode,
            RootMotionCurveAsset actionMotionCurve,
            float turnSpeedDegrees,
            LocomotionInputMotionExecutionMode executionMode,
            float durationSeconds)
        {
            if (!float.IsFinite(moveSpeed) || moveSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(moveSpeed));
            if (!Enum.IsDefined(typeof(LocomotionInputMotionDisplacementMode), displacementMode))
                throw new ArgumentOutOfRangeException(nameof(displacementMode));
            if (displacementMode == LocomotionInputMotionDisplacementMode.ConstantSpeed && actionMotionCurve)
                throw new ArgumentException("Constant Speed locomotion cannot declare an Action Motion Curve.", nameof(actionMotionCurve));
            if (displacementMode == LocomotionInputMotionDisplacementMode.ActionMotionCurve)
            {
                if (!actionMotionCurve)
                    throw new ArgumentNullException(nameof(actionMotionCurve));
                if (moveSpeed != 0f)
                    throw new ArgumentOutOfRangeException(nameof(moveSpeed));
                if (actionMotionCurve.EvaluationMode != RootMotionCurveEvaluationMode.FullLocalDelta)
                    throw new ArgumentException("Action Motion Curve locomotion requires FullLocalDelta evaluation.", nameof(actionMotionCurve));
                if (!float.IsFinite(actionMotionCurve.Duration) || actionMotionCurve.Duration <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(actionMotionCurve));
            }
            if (!float.IsFinite(turnSpeedDegrees) || turnSpeedDegrees <= 0f)
                throw new ArgumentOutOfRangeException(nameof(turnSpeedDegrees));
            if (!Enum.IsDefined(typeof(LocomotionInputMotionExecutionMode), executionMode))
                throw new ArgumentOutOfRangeException(nameof(executionMode));
            if (!float.IsFinite(durationSeconds) || durationSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (executionMode == LocomotionInputMotionExecutionMode.Timed && durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (executionMode != LocomotionInputMotionExecutionMode.Timed && durationSeconds != 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (displacementMode == LocomotionInputMotionDisplacementMode.ActionMotionCurve &&
                executionMode == LocomotionInputMotionExecutionMode.Timed &&
                durationSeconds > actionMotionCurve.Duration + 0.0001f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));
        }
    }
}
