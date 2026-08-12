using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    [Flags]
    public enum WorldCapability : ulong
    {
        None = 0,
        BodyMotion = 1UL << 0,
        Grounding = 1UL << 1,
        Collision = 1UL << 2,
        Reconstructible = 1UL << 3,
        Snapshotable = 1UL << 4,
        DeterministicReplay = 1UL << 5,
        AirborneVerticalMotion = 1UL << 6
    }

    public enum ActionTargetRequirement : byte
    {
        None = 0,
        SnapshotRequired = 1,
        OptionalSnapshot = 2
    }

    public enum ProgramInputValueKind : byte
    {
        Boolean = 1,
        Scalar = 2,
        Vector2 = 3,
        Vector3 = 4,
        Yaw = 5,
        ActionTargetSnapshot = 6
    }

    public enum ProgramMotionModifierKind : byte
    {
        MotionWarp = 1
    }

    public enum ProgramMotionModifierChannel : byte
    {
        Locomotion = 0,
        Action = 1,
        GameplayResult = 2
    }

    public enum LocomotionInputMotionExecutionMode : byte
    {
        Once = 0,
        Timed = 1,
        Continuous = 2
    }

    public enum LocomotionInputMotionDisplacementMode : byte
    {
        ConstantSpeed = 0,
        ActionMotionCurve = 1
    }

    public enum ProgramMotionSourceBlendMode : byte
    {
        Additive = 0,
        WeightedBlend = 1,
        Override = 2
    }

    public enum ProgramMotionWarpTranslationMode : byte
    {
        Disabled = 0,
        ScaleToTarget = 1,
        SkewToTarget = 2,
        LinearToTarget = 3
    }

    public enum ProgramMotionWarpTargetOffsetSpace : byte
    {
        TargetLocal = 0,
        ApproachDirection = 1,
        ActorStartLocal = 2,
        World = 3
    }

    public enum ProgramMotionWarpRotationMode : byte
    {
        Disabled = 0,
        FaceTarget = 1,
        MatchTargetYaw = 2
    }

    public enum ProgramMotionWarpRotationMethod : byte
    {
        ProgressCurve = 0,
        ConstantRate = 1,
        ScaleSourceYaw = 2
    }

    public enum ProgramMotionWarpLimitPolicy : byte
    {
        ApplyClamped = 0,
        PreserveSource = 1
    }

    public enum ProgramMotionWarpLimitResult : byte
    {
        Applied = 0,
        AppliedClamped = 1,
        PreservedByLimitPolicy = 2
    }

    public sealed class ProgramMotionModifierDescriptor
    {
        public const int MotionWarpStateSlotCount = 16;

        public ProgramMotionModifierDescriptor(
            int index,
            ProgramMotionModifierKind kind,
            ProgramMotionModifierChannel channel,
            OperationHandle operation,
            OperationHandle sourceMotionOperation,
            OperationHandle timelineOwnerOperation,
            string actionContextIdentity,
            int catalogEntryIndex,
            int stateSlotStart,
            int stateSlotCount,
            ProgramMotionWarpTranslationMode translationMode,
            ProgramMotionWarpTargetOffsetSpace targetOffsetSpace,
            ProgramMotionWarpRotationMode rotationMode,
            ProgramMotionWarpRotationMethod rotationMethod,
            int targetPlanarOffsetConstantIndex,
            int targetYawOffsetConstantIndex,
            int maximumPositionCorrectionConstantIndex,
            int maximumYawCorrectionConstantIndex,
            int maximumYawRateConstantIndex,
            ProgramMotionWarpLimitPolicy limitPolicy,
            int positionProgressCurveConstantIndex,
            int yawProgressCurveConstantIndex)
        {
            if (index < 0 || !operation.IsValid || !sourceMotionOperation.IsValid || !timelineOwnerOperation.IsValid ||
                catalogEntryIndex < 0 || stateSlotStart < 0 || stateSlotCount <= 0 ||
                targetPlanarOffsetConstantIndex < -1 || targetYawOffsetConstantIndex < -1 ||
                maximumPositionCorrectionConstantIndex < -1 || maximumYawCorrectionConstantIndex < -1 ||
                maximumYawRateConstantIndex < -1 || positionProgressCurveConstantIndex < -1 || yawProgressCurveConstantIndex < -1)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (!Enum.IsDefined(typeof(ProgramMotionModifierKind), kind) ||
                !Enum.IsDefined(typeof(ProgramMotionModifierChannel), channel) ||
                !Enum.IsDefined(typeof(ProgramMotionWarpTranslationMode), translationMode) ||
                !Enum.IsDefined(typeof(ProgramMotionWarpTargetOffsetSpace), targetOffsetSpace) ||
                !Enum.IsDefined(typeof(ProgramMotionWarpRotationMode), rotationMode) ||
                !Enum.IsDefined(typeof(ProgramMotionWarpRotationMethod), rotationMethod) ||
                !Enum.IsDefined(typeof(ProgramMotionWarpLimitPolicy), limitPolicy))
            {
                throw new ArgumentOutOfRangeException();
            }
            bool hasTranslation = translationMode != ProgramMotionWarpTranslationMode.Disabled;
            bool hasRotation = rotationMode != ProgramMotionWarpRotationMode.Disabled;
            bool usesPositionProgress = translationMode is ProgramMotionWarpTranslationMode.SkewToTarget or ProgramMotionWarpTranslationMode.LinearToTarget;
            bool usesYawProgress = hasRotation && rotationMethod == ProgramMotionWarpRotationMethod.ProgressCurve;
            bool usesYawRate = hasRotation && rotationMethod == ProgramMotionWarpRotationMethod.ConstantRate;
            if (kind != ProgramMotionModifierKind.MotionWarp || channel != ProgramMotionModifierChannel.Action ||
                stateSlotCount != MotionWarpStateSlotCount ||
                !hasTranslation && !hasRotation ||
                hasTranslation &&
                    (targetPlanarOffsetConstantIndex < 0 || maximumPositionCorrectionConstantIndex < 0) ||
                !hasTranslation &&
                    (targetPlanarOffsetConstantIndex >= 0 || maximumPositionCorrectionConstantIndex >= 0 || positionProgressCurveConstantIndex >= 0) ||
                usesPositionProgress != (positionProgressCurveConstantIndex >= 0) ||
                hasRotation &&
                    (targetYawOffsetConstantIndex < 0 || maximumYawCorrectionConstantIndex < 0) ||
                !hasRotation &&
                    (targetYawOffsetConstantIndex >= 0 || maximumYawCorrectionConstantIndex >= 0 || maximumYawRateConstantIndex >= 0 || yawProgressCurveConstantIndex >= 0) ||
                usesYawProgress != (yawProgressCurveConstantIndex >= 0) ||
                usesYawRate != (maximumYawRateConstantIndex >= 0))
            {
                throw new ArgumentException("Motion modifier descriptor is inconsistent.");
            }
            Index = index;
            Kind = kind;
            Channel = channel;
            Operation = operation;
            SourceMotionOperation = sourceMotionOperation;
            TimelineOwnerOperation = timelineOwnerOperation;
            ActionContextIdentity = SimulationIdentity.Require(actionContextIdentity, nameof(actionContextIdentity));
            CatalogEntryIndex = catalogEntryIndex;
            StateSlotStart = stateSlotStart;
            StateSlotCount = stateSlotCount;
            TranslationMode = translationMode;
            TargetOffsetSpace = targetOffsetSpace;
            RotationMode = rotationMode;
            RotationMethod = rotationMethod;
            TargetPlanarOffsetConstantIndex = targetPlanarOffsetConstantIndex;
            TargetYawOffsetConstantIndex = targetYawOffsetConstantIndex;
            MaximumPositionCorrectionConstantIndex = maximumPositionCorrectionConstantIndex;
            MaximumYawCorrectionConstantIndex = maximumYawCorrectionConstantIndex;
            MaximumYawRateConstantIndex = maximumYawRateConstantIndex;
            LimitPolicy = limitPolicy;
            PositionProgressCurveConstantIndex = positionProgressCurveConstantIndex;
            YawProgressCurveConstantIndex = yawProgressCurveConstantIndex;
        }

        public int Index { get; }
        public ProgramMotionModifierKind Kind { get; }
        public ProgramMotionModifierChannel Channel { get; }
        public OperationHandle Operation { get; }
        public OperationHandle SourceMotionOperation { get; }
        public OperationHandle TimelineOwnerOperation { get; }
        public string ActionContextIdentity { get; }
        public int CatalogEntryIndex { get; }
        public int StateSlotStart { get; }
        public int StateSlotCount { get; }
        public ProgramMotionWarpTranslationMode TranslationMode { get; }
        public ProgramMotionWarpTargetOffsetSpace TargetOffsetSpace { get; }
        public ProgramMotionWarpRotationMode RotationMode { get; }
        public ProgramMotionWarpRotationMethod RotationMethod { get; }
        public int TargetPlanarOffsetConstantIndex { get; }
        public int TargetYawOffsetConstantIndex { get; }
        public int MaximumPositionCorrectionConstantIndex { get; }
        public int MaximumYawCorrectionConstantIndex { get; }
        public int MaximumYawRateConstantIndex { get; }
        public ProgramMotionWarpLimitPolicy LimitPolicy { get; }
        public int PositionProgressCurveConstantIndex { get; }
        public int YawProgressCurveConstantIndex { get; }
    }

    public enum SimulationOperationCode : ushort
    {
        Root = 1,
        Loop = 2,
        Parallel = 3,
        Sequence = 4,
        Selector = 5,
        Succeed = 6,
        StateMachine = 20,
        State = 21,
        StateEnter = 22,
        StateAny = 23,
        StateExit = 24,
        StateOnEnter = 25,
        StateOnExit = 26,
        StateRootCompleted = 27,
        StateExitCause = 28,
        Timeline = 40,
        TimelineEnter = 41,
        TimelineAnimation = 42,
        TimelineMotionCurve = 43,
        TimelineTreeClip = 44,
        TimelineCue = 45,
        TimelineCameraState = 46,
        TimelineCameraCue = 47,
        TimelineCameraResponse = 48,
        TimelineMotionWarp = 49,
        BlackboardGet = 60,
        BlackboardSet = 61,
        InputBoolean = 70,
        InputScalar = 71,
        InputVector2 = 72,
        InputVector2Magnitude = 73,
        InputRequest = 74,
        MoveFacingAngle = 75,
        ActivateActionInstance = 80,
        ActionContextActive = 81,
        SubmitActionLifecycle = 82,
        ActionWindowActive = 83,
        CanActivateAction = 84,
        LocomotionInputMotion = 90,
        ConditionResult = 100,
        Compare = 101,
        And = 102,
        Or = 103,
        Not = 104,
        Constant = 105,
        GameplayEffectHasTag = 110,
        GameplayEffectMatchTags = 111,
        GameplayAttributeRead = 112,
        GameplayEffectApply = 113,
        GameplayEffectRemove = 114,
        CameraStateRequest = 120,
        CameraCue = 121,
        CameraResponse = 122,
        CameraTarget = 123,
        CameraBasisRead = 124,
        ReadEquipmentIdentity = 130,
        ReadEquipmentParameter = 131,
        RequestEquipmentChange = 132,
        BeginEquipmentChange = 133,
        CommitEquipmentChange = 134,
        CancelEquipmentChange = 135,
        EnterEquipmentFeatureHost = 136,
        ExitEquipmentFeatureHost = 137,
        ResolveEquipmentActionRoute = 138,
        AIReadSelfObservation = 200,
        AIEnumerateConfiguredCandidates = 201,
        AISelectNearestCandidate = 202,
        AIReadTargetDistance = 203,
        AIReadTargetDirection = 204,
        AIReadMemory = 205,
        AIWriteMemory = 206,
        AIWriteContinuousInput = 207,
        AIWriteActionTargetSnapshot = 208,
        AISubmitActionRequest = 209,
        AIReadSelectedTargetSnapshot = 210,
        AIWaitTicks = 211
    }

    public static class AIIntentOperationSet
    {
        public const string Id = "ai-intent-operations";
        public static readonly OperationSetVersion Version = new OperationSetVersion(Id + "/3");
        static readonly ReadOnlyCollection<SimulationOperationCode> s_Operations = Array.AsReadOnly(new[]
        {
            SimulationOperationCode.Root,
            SimulationOperationCode.Loop,
            SimulationOperationCode.Parallel,
            SimulationOperationCode.Sequence,
            SimulationOperationCode.Selector,
            SimulationOperationCode.Succeed,
            SimulationOperationCode.Compare,
            SimulationOperationCode.And,
            SimulationOperationCode.Or,
            SimulationOperationCode.Not,
            SimulationOperationCode.Constant,
            SimulationOperationCode.AIReadSelfObservation,
            SimulationOperationCode.AIEnumerateConfiguredCandidates,
            SimulationOperationCode.AISelectNearestCandidate,
            SimulationOperationCode.AIReadTargetDistance,
            SimulationOperationCode.AIReadTargetDirection,
            SimulationOperationCode.AIReadSelectedTargetSnapshot,
            SimulationOperationCode.AIReadMemory,
            SimulationOperationCode.AIWriteMemory,
            SimulationOperationCode.AIWriteContinuousInput,
            SimulationOperationCode.AIWriteActionTargetSnapshot,
            SimulationOperationCode.AISubmitActionRequest,
            SimulationOperationCode.AIWaitTicks
        });

        public static IReadOnlyList<SimulationOperationCode> Operations => s_Operations;

        public static void RequireVersion(OperationSetVersion version)
        {
            if (!version.Equals(Version))
                throw new InvalidOperationException($"AI operation set '{version.Value}' is unsupported; expected '{Version.Value}'.");
        }

        public static void RequireOperation(SimulationOperationCode code)
        {
            if (!s_Operations.Contains(code))
                throw new InvalidOperationException($"Operation code '{(ushort)code}' is not supported by '{Version.Value}'.");
        }
    }

    public static class CharacterGameplayOperationSet
    {
        public const string Id = "character-gameplay-operations";
        public static readonly OperationSetVersion Version = new OperationSetVersion(Id + "/12");

        static readonly ReadOnlyCollection<SimulationOperationCode> s_Operations =
            Array.AsReadOnly(new[]
            {
                SimulationOperationCode.Root,
                SimulationOperationCode.Loop,
                SimulationOperationCode.Parallel,
                SimulationOperationCode.Sequence,
                SimulationOperationCode.Selector,
                SimulationOperationCode.Succeed,
                SimulationOperationCode.StateMachine,
                SimulationOperationCode.State,
                SimulationOperationCode.StateEnter,
                SimulationOperationCode.StateAny,
                SimulationOperationCode.StateExit,
                SimulationOperationCode.StateOnEnter,
                SimulationOperationCode.StateOnExit,
                SimulationOperationCode.StateRootCompleted,
                SimulationOperationCode.StateExitCause,
                SimulationOperationCode.Timeline,
                SimulationOperationCode.TimelineEnter,
                SimulationOperationCode.TimelineAnimation,
                SimulationOperationCode.TimelineMotionCurve,
                SimulationOperationCode.TimelineTreeClip,
                SimulationOperationCode.TimelineCue,
                SimulationOperationCode.TimelineCameraState,
                SimulationOperationCode.TimelineCameraCue,
                SimulationOperationCode.TimelineCameraResponse,
                SimulationOperationCode.TimelineMotionWarp,
                SimulationOperationCode.BlackboardGet,
                SimulationOperationCode.BlackboardSet,
                SimulationOperationCode.InputBoolean,
                SimulationOperationCode.InputScalar,
                SimulationOperationCode.InputVector2,
                SimulationOperationCode.InputVector2Magnitude,
                SimulationOperationCode.InputRequest,
                SimulationOperationCode.MoveFacingAngle,
                SimulationOperationCode.ActivateActionInstance,
                SimulationOperationCode.ActionContextActive,
                SimulationOperationCode.SubmitActionLifecycle,
                SimulationOperationCode.ActionWindowActive,
                SimulationOperationCode.CanActivateAction,
                SimulationOperationCode.LocomotionInputMotion,
                SimulationOperationCode.ConditionResult,
                SimulationOperationCode.Compare,
                SimulationOperationCode.And,
                SimulationOperationCode.Or,
                SimulationOperationCode.Not,
                SimulationOperationCode.Constant,
                SimulationOperationCode.GameplayEffectHasTag,
                SimulationOperationCode.GameplayEffectMatchTags,
                SimulationOperationCode.GameplayAttributeRead,
                SimulationOperationCode.GameplayEffectApply,
                SimulationOperationCode.GameplayEffectRemove,
                SimulationOperationCode.CameraStateRequest,
                SimulationOperationCode.CameraCue,
                SimulationOperationCode.CameraResponse,
                SimulationOperationCode.CameraTarget,
                SimulationOperationCode.CameraBasisRead,
                SimulationOperationCode.ReadEquipmentIdentity,
                SimulationOperationCode.ReadEquipmentParameter,
                SimulationOperationCode.RequestEquipmentChange,
                SimulationOperationCode.BeginEquipmentChange,
                SimulationOperationCode.CommitEquipmentChange,
                SimulationOperationCode.CancelEquipmentChange,
                SimulationOperationCode.EnterEquipmentFeatureHost,
                SimulationOperationCode.ExitEquipmentFeatureHost,
                SimulationOperationCode.ResolveEquipmentActionRoute
            });

        public static IReadOnlyList<SimulationOperationCode> Operations => s_Operations;

        public static void RequireVersion(OperationSetVersion version)
        {
            if (!version.Equals(Version))
                throw new InvalidOperationException($"Operation set '{version.Value}' is not supported; expected '{Version.Value}'.");
        }

        public static void RequireOperation(SimulationOperationCode code)
        {
            if (!s_Operations.Contains(code))
                throw new InvalidOperationException($"Operation code '{(ushort)code}' is not supported by '{Version.Value}'.");
            CharacterGameplayValuePortContracts.Require(code);
        }

        public static void RequireCompleteBackend(
            OperationSetVersion version,
            IReadOnlyList<SimulationOperationCode> supportedOperations,
            string backendIdentity)
        {
            RequireVersion(version);
            string identity = SimulationIdentity.Require(backendIdentity, nameof(backendIdentity));
            if (supportedOperations == null || supportedOperations.Count != s_Operations.Count)
                throw new InvalidOperationException($"Kernel backend '{identity}' does not implement the complete '{Version.Value}' operation set.");
            for (int i = 0; i < s_Operations.Count; i++)
            {
                if (supportedOperations[i] != s_Operations[i])
                {
                    throw new InvalidOperationException(
                        $"Kernel backend '{identity}' operation contract diverges at index {i}: expected '{s_Operations[i]}', received '{supportedOperations[i]}'.");
                }
            }
        }
    }

    public static class CameraProgramOperationSchema
    {
        public const int PayloadVersion = 1;
        public static readonly AnimationChannelId ChannelId = new AnimationChannelId("Camera");
        public const string OutputPortId = "Submitted";
        public const string BasisValidPortId = "Valid";
        public const string BasisPlanarForwardPortId = "Planar Forward";
        public const string BasisPlanarRightPortId = "Planar Right";
        public const string BasisLookDirectionPortId = "Look Direction";
        public const string BasisAimPointPortId = "Aim Point";
        public const string BasisYawPortId = "Yaw";
        public const string BasisPitchPortId = "Pitch";
        public const string BasisValidInputId = "camera-basis.valid";
        public const string BasisPlanarForwardInputId = "camera-basis.planar-forward";
        public const string BasisPlanarRightInputId = "camera-basis.planar-right";
        public const string BasisLookDirectionInputId = "camera-basis.look-direction";
        public const string BasisAimPointInputId = "camera-basis.aim-point";
        public const string BasisYawInputId = "camera-basis.yaw";
        public const string BasisPitchInputId = "camera-basis.pitch";
        public const int TargetKeyMask = 1 << 0;
        public const int AnchorKeyMask = 1 << 1;
        public const int AimPointKeyMask = 1 << 2;
        public const int PreferredBoneKeyMask = 1 << 3;
        public const int AllTargetKeyMasks = TargetKeyMask | AnchorKeyMask | AimPointKeyMask | PreferredBoneKeyMask;

        public static bool IsCameraPresentationOperation(SimulationOperationCode code)
        {
            return code == SimulationOperationCode.CameraStateRequest ||
                   code == SimulationOperationCode.CameraCue ||
                   code == SimulationOperationCode.CameraResponse ||
                   code == SimulationOperationCode.CameraTarget;
        }

        public static bool IsCameraBasisOperation(SimulationOperationCode code) =>
            code == SimulationOperationCode.CameraBasisRead;

        public static bool IsCameraOperation(SimulationOperationCode code) =>
            IsCameraPresentationOperation(code) || IsCameraBasisOperation(code);

        public static bool IsCameraBasisInputId(string inputId)
        {
            return string.Equals(inputId, BasisValidInputId, StringComparison.Ordinal) ||
                   string.Equals(inputId, BasisPlanarForwardInputId, StringComparison.Ordinal) ||
                   string.Equals(inputId, BasisPlanarRightInputId, StringComparison.Ordinal) ||
                   string.Equals(inputId, BasisLookDirectionInputId, StringComparison.Ordinal) ||
                   string.Equals(inputId, BasisAimPointInputId, StringComparison.Ordinal) ||
                   string.Equals(inputId, BasisYawInputId, StringComparison.Ordinal) ||
                   string.Equals(inputId, BasisPitchInputId, StringComparison.Ordinal);
        }

        public static bool IsCameraBasisOutputPort(string portId)
        {
            return string.Equals(portId, BasisValidPortId, StringComparison.Ordinal) ||
                   string.Equals(portId, BasisPlanarForwardPortId, StringComparison.Ordinal) ||
                   string.Equals(portId, BasisPlanarRightPortId, StringComparison.Ordinal) ||
                   string.Equals(portId, BasisLookDirectionPortId, StringComparison.Ordinal) ||
                   string.Equals(portId, BasisAimPointPortId, StringComparison.Ordinal) ||
                   string.Equals(portId, BasisYawPortId, StringComparison.Ordinal) ||
                   string.Equals(portId, BasisPitchPortId, StringComparison.Ordinal);
        }

        public static void RequireCameraBasisOutputPort(string portId, string sourceIdentity)
        {
            if (!IsCameraBasisOutputPort(portId))
                throw new InvalidOperationException($"Camera basis source '{sourceIdentity}' contains unknown output port '{portId}'.");
        }

        public static void Validate(SemanticOperation operation, IReadOnlyList<SemanticLiteral> literals)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (literals == null)
                throw new ArgumentNullException(nameof(literals));
            if (!IsCameraOperation(operation.Code))
                return;
            if (operation.Integer0 != PayloadVersion)
                throw Invalid(operation, $"payload version '{operation.Integer0}' is unsupported; expected '{PayloadVersion}'");
            if (operation.Operands.Count != 0 || operation.Unsigned0 != 0 || operation.Number0 != 0d || operation.Text0.Length != 0)
                throw Invalid(operation, "contains fields outside the Camera payload schema");

            switch (operation.Code)
            {
                case SimulationOperationCode.CameraStateRequest:
                    RequireEnum(operation, operation.Integer1, 0, 4, "Mode");
                    RequireEnum(operation, checked((int)operation.Flags), 0, 2, "InterruptPolicy");
                    RequireInt32(operation, literals, "Priority");
                    RequireUnit(operation, literals, "Weight");
                    RequireNonNegative(operation, literals, "BlendInSeconds");
                    RequireNonNegative(operation, literals, "BlendOutSeconds");
                    RequireString(operation, literals, "TargetKey", false);
                    RequireString(operation, literals, "ActionContext", false);
                    RequireFieldCount(operation, 6);
                    break;
                case SimulationOperationCode.CameraCue:
                    RequireEnum(operation, operation.Integer1, 0, 4, "CueKind");
                    RequireFlags(operation, 0);
                    RequireString(operation, literals, "CueId", true);
                    RequireString(operation, literals, "CueType", true);
                    RequireNonNegative(operation, literals, "Intensity");
                    RequireNonNegative(operation, literals, "DurationSeconds");
                    RequireInt32(operation, literals, "Priority");
                    RequireString(operation, literals, "ActionContext", false);
                    RequireFieldCount(operation, 6);
                    break;
                case SimulationOperationCode.CameraResponse:
                    RequireEnum(operation, operation.Integer1, 0, 2, "LookResponse");
                    RequireFlags(operation, 0);
                    RequireUnit(operation, literals, "ManualOrbitWeight");
                    RequireUnit(operation, literals, "PitchResponseWeight");
                    RequireUnit(operation, literals, "YawResponseWeight");
                    RequireInt32(operation, literals, "Priority");
                    RequireUnit(operation, literals, "Weight");
                    RequireString(operation, literals, "ActionContext", false);
                    RequireFieldCount(operation, 6);
                    break;
                case SimulationOperationCode.CameraTarget:
                    RequireFlags(operation, 0);
                    if (operation.Integer1 <= 0 || (operation.Integer1 & ~AllTargetKeyMasks) != 0)
                        throw Invalid(operation, $"target key mask '{operation.Integer1}' is unknown or empty");
                    string targetKey = RequireString(operation, literals, "TargetKey", false);
                    string anchorKey = RequireString(operation, literals, "AnchorKey", false);
                    string aimPointKey = RequireString(operation, literals, "AimPointKey", false);
                    string preferredBoneKey = RequireString(operation, literals, "PreferredBoneKey", false);
                    int expectedMask = (targetKey.Length > 0 ? TargetKeyMask : 0) |
                                       (anchorKey.Length > 0 ? AnchorKeyMask : 0) |
                                       (aimPointKey.Length > 0 ? AimPointKeyMask : 0) |
                                       (preferredBoneKey.Length > 0 ? PreferredBoneKeyMask : 0);
                    if (operation.Integer1 != expectedMask)
                        throw Invalid(operation, $"target key mask '{operation.Integer1}' does not match configured target identities '{expectedMask}'");
                    RequireInt32(operation, literals, "Priority");
                    RequireUnit(operation, literals, "Weight");
                    RequireString(operation, literals, "ActionContext", false);
                    RequireFieldCount(operation, 7);
                    break;
                case SimulationOperationCode.CameraBasisRead:
                    RequireFlags(operation, 0);
                    if (operation.Integer1 != 0)
                        throw Invalid(operation, $"field Integer1 must be zero but is '{operation.Integer1}'");
                    RequireFieldCount(operation, 0);
                    break;
                default:
                    throw Invalid(operation, $"operation '{operation.Code}' is unsupported");
            }
        }

        static void RequireFieldCount(SemanticOperation operation, int expected)
        {
            if (operation.LiteralReferences.Count != expected)
                throw Invalid(operation, $"requires exactly {expected} payload fields but contains {operation.LiteralReferences.Count}");
        }

        static void RequireEnum(SemanticOperation operation, int value, int minimum, int maximum, string field)
        {
            if (value < minimum || value > maximum)
                throw Invalid(operation, $"field '{field}' contains unknown enum value '{value}'");
        }

        static void RequireFlags(SemanticOperation operation, uint expected)
        {
            if (operation.Flags != expected)
                throw Invalid(operation, $"flags '{operation.Flags}' are unsupported");
        }

        static void RequireInt32(SemanticOperation operation, IReadOnlyList<SemanticLiteral> literals, string field)
        {
            SemanticLiteral literal = RequireLiteral(operation, literals, field);
            if (literal.Kind != SemanticLiteralKind.Int32)
                throw Invalid(operation, $"field '{field}' must be Int32");
        }

        static string RequireString(
            SemanticOperation operation,
            IReadOnlyList<SemanticLiteral> literals,
            string field,
            bool requireValue)
        {
            SemanticLiteral literal = RequireLiteral(operation, literals, field);
            if (literal.Kind != SemanticLiteralKind.String)
                throw Invalid(operation, $"field '{field}' must be String");
            if (requireValue && string.IsNullOrWhiteSpace(literal.Text))
                throw Invalid(operation, $"field '{field}' is required");
            if (!string.Equals(literal.Text, literal.Text.Trim(), StringComparison.Ordinal))
                throw Invalid(operation, $"field '{field}' must not contain leading or trailing whitespace");
            return literal.Text;
        }

        static void RequireUnit(SemanticOperation operation, IReadOnlyList<SemanticLiteral> literals, string field)
        {
            double value = RequireNumber(operation, literals, field);
            if (value < 0d || value > 1d)
                throw Invalid(operation, $"field '{field}' must be in [0, 1]");
        }

        static void RequireNonNegative(SemanticOperation operation, IReadOnlyList<SemanticLiteral> literals, string field)
        {
            if (RequireNumber(operation, literals, field) < 0d)
                throw Invalid(operation, $"field '{field}' must be non-negative");
        }

        static double RequireNumber(SemanticOperation operation, IReadOnlyList<SemanticLiteral> literals, string field)
        {
            SemanticLiteral literal = RequireLiteral(operation, literals, field);
            if (literal.Kind != SemanticLiteralKind.Number)
                throw Invalid(operation, $"field '{field}' must be Number");
            return literal.X;
        }

        static SemanticLiteral RequireLiteral(
            SemanticOperation operation,
            IReadOnlyList<SemanticLiteral> literals,
            string field)
        {
            SemanticLiteral result = null;
            string suffix = "/constant/" + field;
            for (int i = 0; i < operation.LiteralReferences.Count; i++)
            {
                int index = operation.LiteralReferences[i];
                if (index < 0 || index >= literals.Count)
                    throw Invalid(operation, $"field '{field}' references literal '{index}' outside the table");
                SemanticLiteral literal = literals[index];
                if (!literal.Identity.EndsWith(suffix, StringComparison.Ordinal))
                    continue;
                if (result != null)
                    throw Invalid(operation, $"field '{field}' is duplicated");
                result = literal;
            }
            return result ?? throw Invalid(operation, $"field '{field}' is missing");
        }

        static InvalidOperationException Invalid(SemanticOperation operation, string detail)
        {
            return new InvalidOperationException(
                $"Camera operation '{operation.TemplateIdentity}' ({operation.Code}) {detail}.");
        }
    }

    public enum ProgramControlFlowKind : byte
    {
        Child = 1,
        Transition = 2,
        Enter = 3,
        Exit = 4,
        Interrupt = 5,
        Value = 6
    }

    public enum ProgramAbortPolicy : byte
    {
        None = 0,
        Self = 1,
        LowerPriority = 2,
        Both = 3
    }

    public sealed class ProgramControlFlowEdge
    {
        public ProgramControlFlowEdge(
            string identity,
            OperationHandle source,
            OperationHandle target,
            string sourcePort,
            string targetPort,
            ProgramControlFlowKind kind,
            int order,
            int priority,
            ProgramAbortPolicy abortPolicy,
            bool hasCondition,
            OperationHandle condition)
        {
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            if (!source.IsValid || !target.IsValid || hasCondition && !condition.IsValid)
                throw new ArgumentException("Control-flow edge contains an invalid operation handle.");
            Source = source;
            Target = target;
            SourcePort = sourcePort ?? string.Empty;
            TargetPort = targetPort ?? string.Empty;
            Kind = kind;
            Order = order;
            Priority = priority;
            AbortPolicy = abortPolicy;
            HasCondition = hasCondition;
            Condition = condition;
        }

        public string Identity { get; }
        public OperationHandle Source { get; }
        public OperationHandle Target { get; }
        public string SourcePort { get; }
        public string TargetPort { get; }
        public ProgramControlFlowKind Kind { get; }
        public int Order { get; }
        public int Priority { get; }
        public ProgramAbortPolicy AbortPolicy { get; }
        public bool HasCondition { get; }
        public OperationHandle Condition { get; }
    }

    public sealed class ProgramConstantInputBinding
    {
        public ProgramConstantInputBinding(
            OperationHandle targetOperation,
            string targetPort,
            int constantIndex,
            SemanticValueKind resolvedValueKind)
        {
            if (!targetOperation.IsValid)
                throw new ArgumentException("Target operation is invalid.", nameof(targetOperation));
            if (constantIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(constantIndex));
            if (!Enum.IsDefined(typeof(SemanticValueKind), resolvedValueKind))
                throw new ArgumentOutOfRangeException(nameof(resolvedValueKind));
            TargetOperation = targetOperation;
            TargetPort = SimulationIdentity.Require(targetPort, nameof(targetPort));
            ConstantIndex = constantIndex;
            ResolvedValueKind = resolvedValueKind;
        }

        public OperationHandle TargetOperation { get; }
        public string TargetPort { get; }
        public int ConstantIndex { get; }
        public SemanticValueKind ResolvedValueKind { get; }
    }

    public enum ProgramReferenceKind : byte
    {
        Operation = 1,
        Constant = 2,
        StateSlot = 3,
        Scope = 4,
        WorldRequest = 5,
        OutputChannel = 6,
        Producer = 7,
        CatalogEntry = 8,
        MotionSourceOperation = 9
    }

    public sealed class ProgramReference
    {
        public ProgramReference(string identity, OperationHandle sourceOperation, ProgramReferenceKind kind, int targetIndex, string externalIdentity)
        {
            if (targetIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(targetIndex));
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            SourceOperation = sourceOperation;
            Kind = kind;
            TargetIndex = targetIndex;
            ExternalIdentity = externalIdentity ?? string.Empty;
        }
        public string Identity { get; }
        public OperationHandle SourceOperation { get; }
        public bool HasSourceOperation => SourceOperation.IsValid;
        public ProgramReferenceKind Kind { get; }
        public int TargetIndex { get; }
        public string ExternalIdentity { get; }
    }

    public enum ProgramStateValueKind : byte
    {
        Boolean = 1,
        Int32 = 2,
        UInt64 = 3,
        Scalar = 4,
        Vector2 = 5,
        Vector3 = 6,
        Yaw = 7,
        Identity = 8,
        BlackboardOwnerToken = 9,
        BlackboardWriteStamp = 10,
        InputRequest = 20,
        ActionActivationRequest = 21,
        ActionInstance = 22,
        ActionInstanceReference = 23,
        ActionTargetSnapshot = 24,
        GameplayEffectAggregate = 25,
        EquipmentAggregate = 26
    }

    public enum ProgramStateOwnerKind : byte
    {
        Runtime = 1,
        Runnable = 2,
        StateMachine = 3,
        Timeline = 4,
        Blackboard = 5,
        Action = 6,
        GameplayEffect = 7,
        MotionModifier = 8,
        Random = 9,
        Fact = 10,
        Input = 11,
        Equipment = 12
    }

    public enum ProgramStateSemantic : ushort
    {
        RunnableLifecycle = 1,
        RunnableChildCursor = 2,
        RunnableStopBarrier = 3,
        RunnableActivationGeneration = 4,
        LocomotionMotionElapsedTicks = 5,
        StateMachineActive = 20,
        StateMachinePending = 21,
        StateMachineExiting = 22,
        StateMachineTransition = 23,
        StateMachineExecutionPath = 24,
        TimelinePlayback = 40,
        TimelineLoop = 41,
        TimelineTreeClipCycle = 42,
        TimelineRetentionIdentity = 43,
        TimelineLogicTime = 44,
        MotionWarpActive = 45,
        MotionWarpInitialized = 46,
        MotionWarpPlaybackGeneration = 47,
        MotionWarpActionInstance = 48,
        MotionWarpStartBodyPosition = 49,
        MotionWarpStartBodyYaw = 50,
        MotionWarpSourceWindowStartPosition = 51,
        MotionWarpSourceWindowStartYaw = 52,
        MotionWarpResolvedTargetPosition = 53,
        MotionWarpResolvedTargetYaw = 54,
        MotionWarpLimitResult = 55,
        MotionWarpPreviousWarpedPosition = 56,
        MotionWarpPreviousWarpedYaw = 57,
        MotionWarpLastPositionProgress = 58,
        MotionWarpLastYawProgress = 59,
        MotionWarpSourceOperation = 62,
        BlackboardValue = 60,
        BlackboardOwnerToken = 61,
        BlackboardLifetime = 63,
        BlackboardWriteStamp = 64,
        InputRequestBuffer = 70,
        ActionInstance = 80,
        ActionRequestBuffer = 81,
        ActionEventSequence = 84,
        GameplayEffectAggregate = 100,
        EquipmentAggregate = 110,
        EquipmentLocalState = 111,
        RandomState = 122,
        HandleAllocator = 123,
        FactSequence = 124,
        AIWaitElapsedTicks = 130
    }

    public sealed class ProgramStateSlot
    {
        public ProgramStateSlot(int index, string identity, ProgramStateValueKind valueKind, ProgramStateOwnerKind ownerKind, ProgramStateSemantic semantic, string ownerIdentity, int defaultConstantIndex)
            : this(index, identity, valueKind, ownerKind, semantic, ownerIdentity, null, defaultConstantIndex)
        {
        }

        public ProgramStateSlot(
            int index,
            string identity,
            ProgramStateValueKind valueKind,
            ProgramStateOwnerKind ownerKind,
            ProgramStateSemantic semantic,
            string ownerIdentity,
            string stateCodecIdentity,
            int defaultConstantIndex)
        {
            if (index < 0 || defaultConstantIndex < -1)
                throw new ArgumentOutOfRangeException();
            Index = index;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            ValueKind = valueKind;
            OwnerKind = ownerKind;
            Semantic = semantic;
            OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            DefaultConstantIndex = defaultConstantIndex;
            StateCodecIdentity = stateCodecIdentity == null
                ? ProgramStateSchema.CodecIdentity(valueKind)
                : SimulationIdentity.Require(stateCodecIdentity, nameof(stateCodecIdentity));
            ProgramStateSchema.RequireSlot(valueKind, ownerKind, semantic);
        }
        public int Index { get; }
        public string Identity { get; }
        public ProgramStateValueKind ValueKind { get; }
        public ProgramStateOwnerKind OwnerKind { get; }
        public ProgramStateSemantic Semantic { get; }
        public string OwnerIdentity { get; }
        public int DefaultConstantIndex { get; }
        public string StateCodecIdentity { get; }
    }

    public static class ProgramStateSchema
    {
        public static string CodecIdentity(ProgramStateValueKind kind)
        {
            return kind switch
            {
                ProgramStateValueKind.Boolean => "state.boolean/v1",
                ProgramStateValueKind.Int32 => "state.int32/v1",
                ProgramStateValueKind.UInt64 => "state.uint64/v1",
                ProgramStateValueKind.Scalar => "state.float32-scalar/v1",
                ProgramStateValueKind.Vector2 => "state.float32-vector2/v1",
                ProgramStateValueKind.Vector3 => "state.float32-vector3/v1",
                ProgramStateValueKind.Yaw => "state.float32-yaw/v1",
                ProgramStateValueKind.Identity => "state.identity/v1",
                ProgramStateValueKind.BlackboardOwnerToken => "state.blackboard-owner-token/v1",
                ProgramStateValueKind.BlackboardWriteStamp => "state.blackboard-write-stamp/v1",
                ProgramStateValueKind.InputRequest => "state.input-request/v1",
                ProgramStateValueKind.ActionActivationRequest => "state.action-activation-request/v1",
                ProgramStateValueKind.ActionInstance => "state.action-instance/v1",
                ProgramStateValueKind.ActionInstanceReference => "state.action-instance-reference/v1",
                ProgramStateValueKind.ActionTargetSnapshot => "state.action-target-snapshot/v1",
                ProgramStateValueKind.GameplayEffectAggregate => "state.gameplay-effect-aggregate/v1",
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        public static void RequireSlot(
            ProgramStateValueKind kind,
            ProgramStateOwnerKind owner,
            ProgramStateSemantic semantic)
        {
            bool valid = semantic switch
            {
                ProgramStateSemantic.RunnableLifecycle => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Runnable,
                ProgramStateSemantic.RunnableChildCursor => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Runnable,
                ProgramStateSemantic.RunnableStopBarrier => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Runnable,
                ProgramStateSemantic.RunnableActivationGeneration => kind == ProgramStateValueKind.UInt64 && owner == ProgramStateOwnerKind.Runnable,
                ProgramStateSemantic.LocomotionMotionElapsedTicks => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Runnable,
                ProgramStateSemantic.AIWaitElapsedTicks => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Runnable,
                ProgramStateSemantic.StateMachineActive => kind == ProgramStateValueKind.Identity && owner == ProgramStateOwnerKind.StateMachine,
                ProgramStateSemantic.StateMachinePending => kind == ProgramStateValueKind.Identity && owner == ProgramStateOwnerKind.StateMachine,
                ProgramStateSemantic.StateMachineExiting => kind == ProgramStateValueKind.Identity && owner == ProgramStateOwnerKind.StateMachine,
                ProgramStateSemantic.StateMachineTransition => kind == ProgramStateValueKind.Identity && owner == ProgramStateOwnerKind.StateMachine,
                ProgramStateSemantic.StateMachineExecutionPath => kind == ProgramStateValueKind.Identity && owner == ProgramStateOwnerKind.StateMachine,
                ProgramStateSemantic.TimelinePlayback => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Timeline,
                ProgramStateSemantic.TimelineLoop => kind == ProgramStateValueKind.Boolean && owner == ProgramStateOwnerKind.Timeline,
                ProgramStateSemantic.TimelineTreeClipCycle => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Timeline,
                ProgramStateSemantic.TimelineRetentionIdentity => kind == ProgramStateValueKind.ActionInstanceReference && owner == ProgramStateOwnerKind.Timeline,
                ProgramStateSemantic.TimelineLogicTime => kind == ProgramStateValueKind.Scalar && owner == ProgramStateOwnerKind.Timeline,
                ProgramStateSemantic.MotionWarpActive => kind == ProgramStateValueKind.Boolean && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpInitialized => kind == ProgramStateValueKind.Boolean && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpPlaybackGeneration => kind == ProgramStateValueKind.UInt64 && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpActionInstance => kind == ProgramStateValueKind.ActionInstanceReference && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpStartBodyPosition => kind == ProgramStateValueKind.Vector3 && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpStartBodyYaw => kind == ProgramStateValueKind.Yaw && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpSourceWindowStartPosition => kind == ProgramStateValueKind.Vector3 && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpSourceWindowStartYaw => kind == ProgramStateValueKind.Scalar && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpResolvedTargetPosition => kind == ProgramStateValueKind.Vector3 && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpResolvedTargetYaw => kind == ProgramStateValueKind.Yaw && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpLimitResult => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpPreviousWarpedPosition => kind == ProgramStateValueKind.Vector3 && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpPreviousWarpedYaw => kind == ProgramStateValueKind.Yaw && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpLastPositionProgress => kind == ProgramStateValueKind.Scalar && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpLastYawProgress => kind == ProgramStateValueKind.Scalar && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.MotionWarpSourceOperation => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.MotionModifier,
                ProgramStateSemantic.BlackboardValue => IsBlackboardValue(kind) && owner == ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardOwnerToken => kind == ProgramStateValueKind.BlackboardOwnerToken && owner == ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardLifetime => kind == ProgramStateValueKind.Int32 && owner == ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.BlackboardWriteStamp => kind == ProgramStateValueKind.BlackboardWriteStamp && owner == ProgramStateOwnerKind.Blackboard,
                ProgramStateSemantic.InputRequestBuffer => kind == ProgramStateValueKind.InputRequest && owner == ProgramStateOwnerKind.Input,
                ProgramStateSemantic.ActionInstance => kind == ProgramStateValueKind.ActionInstance && owner == ProgramStateOwnerKind.Action,
                ProgramStateSemantic.ActionRequestBuffer => kind == ProgramStateValueKind.ActionActivationRequest && owner == ProgramStateOwnerKind.Action,
                ProgramStateSemantic.ActionEventSequence => kind == ProgramStateValueKind.UInt64 && owner == ProgramStateOwnerKind.Action,
                ProgramStateSemantic.GameplayEffectAggregate => kind == ProgramStateValueKind.GameplayEffectAggregate && owner == ProgramStateOwnerKind.GameplayEffect,
                ProgramStateSemantic.RandomState => kind == ProgramStateValueKind.UInt64 && owner == ProgramStateOwnerKind.Random,
                ProgramStateSemantic.HandleAllocator => kind == ProgramStateValueKind.UInt64 && owner == ProgramStateOwnerKind.Runtime,
                ProgramStateSemantic.FactSequence => kind == ProgramStateValueKind.UInt64 && owner == ProgramStateOwnerKind.Fact,
                _ => false
            };
            if (!valid)
                throw new ArgumentException($"State semantic '{semantic}' cannot use kind '{kind}' with owner '{owner}'.");
        }

        static bool IsBlackboardValue(ProgramStateValueKind kind)
        {
            return kind == ProgramStateValueKind.Boolean ||
                   kind == ProgramStateValueKind.Int32 ||
                   kind == ProgramStateValueKind.UInt64 ||
                   kind == ProgramStateValueKind.Scalar ||
                   kind == ProgramStateValueKind.Vector2 ||
                   kind == ProgramStateValueKind.Vector3 ||
                   kind == ProgramStateValueKind.Yaw ||
                   kind == ProgramStateValueKind.Identity ||
                   kind == ProgramStateValueKind.ActionTargetSnapshot;
        }
    }

    public enum ProgramScopeKind : byte
    {
        Character = 1,
        Graph = 2,
        State = 3,
        ActionInstance = 4,
        Frame = 5
    }

    public readonly struct BlackboardOwnerToken : IEquatable<BlackboardOwnerToken>
    {
        public BlackboardOwnerToken(ProgramScopeKind scopeKind, int compiledOwnerIndex, ulong generation)
        {
            if (!Enum.IsDefined(typeof(ProgramScopeKind), scopeKind) || compiledOwnerIndex < 0 || generation == 0)
                throw new ArgumentException("Blackboard owner token is incomplete.");
            ScopeKind = scopeKind;
            CompiledOwnerIndex = compiledOwnerIndex;
            Generation = generation;
        }

        public ProgramScopeKind ScopeKind { get; }
        public int CompiledOwnerIndex { get; }
        public ulong Generation { get; }
        public bool IsValid => CompiledOwnerIndex >= 0 && Generation != 0 && Enum.IsDefined(typeof(ProgramScopeKind), ScopeKind);
        public bool Equals(BlackboardOwnerToken other) =>
            ScopeKind == other.ScopeKind &&
            CompiledOwnerIndex == other.CompiledOwnerIndex &&
            Generation == other.Generation;
        public override bool Equals(object obj) => obj is BlackboardOwnerToken other && Equals(other);
        public override int GetHashCode() => HashCode.Combine((int)ScopeKind, CompiledOwnerIndex, Generation);
        public static bool operator ==(BlackboardOwnerToken left, BlackboardOwnerToken right) => left.Equals(right);
        public static bool operator !=(BlackboardOwnerToken left, BlackboardOwnerToken right) => !left.Equals(right);
    }

    public readonly struct BlackboardWriteStamp : IEquatable<BlackboardWriteStamp>
    {
        public BlackboardWriteStamp(
            OperationHandle sourceOperation,
            ulong logicTick,
            ulong actionInstanceId,
            OperationHandle timelineOperation,
            OperationHandle clipOperation,
            int cycle)
        {
            if (!sourceOperation.IsValid || logicTick == 0 ||
                timelineOperation.IsValid != clipOperation.IsValid || cycle < 0)
            {
                throw new ArgumentException("Blackboard write stamp is incomplete.");
            }
            SourceOperation = sourceOperation;
            LogicTick = logicTick;
            ActionInstanceId = actionInstanceId;
            TimelineOperation = timelineOperation;
            ClipOperation = clipOperation;
            Cycle = cycle;
        }

        public OperationHandle SourceOperation { get; }
        public ulong LogicTick { get; }
        public ulong ActionInstanceId { get; }
        public OperationHandle TimelineOperation { get; }
        public OperationHandle ClipOperation { get; }
        public int Cycle { get; }
        public bool IsValid => SourceOperation.IsValid && LogicTick != 0;
        public bool Equals(BlackboardWriteStamp other) =>
            SourceOperation.Equals(other.SourceOperation) &&
            LogicTick == other.LogicTick &&
            ActionInstanceId == other.ActionInstanceId &&
            TimelineOperation.Equals(other.TimelineOperation) &&
            ClipOperation.Equals(other.ClipOperation) &&
            Cycle == other.Cycle;
        public override bool Equals(object obj) => obj is BlackboardWriteStamp other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(SourceOperation, LogicTick, ActionInstanceId, TimelineOperation, ClipOperation, Cycle);
    }

    public enum ProgramBlackboardLifetime : byte
    {
        Config = 0,
        Spawn = 1,
        StateEnterToExit = 2,
        ActionInstance = 3,
        Frame = 4,
        ManualClear = 5,
        GraphInstance = 6
    }

    public enum ProgramBlackboardFactProjectionKind : byte
    {
        ActionWindow = 0
    }
    public sealed class ProgramScopeLayout
    {
        readonly ReadOnlyCollection<int> m_StateSlots;

        public ProgramScopeLayout(
            int compiledOwnerIndex,
            string identity,
            ProgramScopeKind kind,
            string ownerIdentity,
            OperationHandle ownerOperation,
            IEnumerable<int> stateSlots)
        {
            if (compiledOwnerIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(compiledOwnerIndex));
            CompiledOwnerIndex = compiledOwnerIndex;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            Kind = kind;
            OwnerIdentity = SimulationIdentity.Require(ownerIdentity, nameof(ownerIdentity));
            OwnerOperation = ownerOperation;
            var values = stateSlots == null ? new List<int>() : new List<int>(stateSlots);
            values.Sort();
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] < 0 || i > 0 && values[i - 1] == values[i])
                    throw new ArgumentException("Program scope state slots must be non-negative and unique.", nameof(stateSlots));
            }
            m_StateSlots = values.AsReadOnly();
        }

        public int CompiledOwnerIndex { get; }
        public string Identity { get; }
        public ProgramScopeKind Kind { get; }
        public string OwnerIdentity { get; }
        public OperationHandle OwnerOperation { get; }
        public IReadOnlyList<int> StateSlots => m_StateSlots;
    }

    public sealed class ProgramWorldRequestLayout
    {
        public ProgramWorldRequestLayout(int index, string identity, WorldCapability requiredCapability)
        {
            if (index < 0 || requiredCapability == WorldCapability.None)
                throw new ArgumentOutOfRangeException();
            Index = index;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            RequiredCapability = requiredCapability;
        }

        public int Index { get; }
        public string Identity { get; }
        public WorldCapability RequiredCapability { get; }
    }

    public enum ProgramOutputChannelKind : byte
    {
        GameplayFact = 1,
        Presentation = 2,
        Trace = 3
    }

    public sealed class ProgramOutputChannelLayout
    {
        public ProgramOutputChannelLayout(int index, string identity, ProgramOutputChannelKind kind)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            Index = index;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            Kind = kind;
        }

        public int Index { get; }
        public string Identity { get; }
        public ProgramOutputChannelKind Kind { get; }
    }

    public enum ProgramSourceTargetKind : byte
    {
        Operation = 1,
        Constant = 2,
        StateSlot = 3,
        Reference = 4,
        Producer = 5,
        CatalogEntry = 6,
        BodyMotion = 7
    }

	public sealed class ProgramSourceMapEntry
	{
		public ProgramSourceMapEntry(
			ProgramSourceTargetKind targetKind,
			int targetIndex,
			string sourceType,
			string graphId,
			string nodeId,
			string portId,
			string edgeId,
			string declarationId,
			string timelineId,
			string trackId,
			string clipId,
			string displayPath,
			string contentHash)
		{
			if (targetIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(targetIndex));
			TargetKind = targetKind;
			TargetIndex = targetIndex;
			SourceType = SimulationIdentity.Require(sourceType, nameof(sourceType));
			GraphId = graphId ?? string.Empty;
			NodeId = nodeId ?? string.Empty;
			PortId = portId ?? string.Empty;
			EdgeId = edgeId ?? string.Empty;
			DeclarationId = declarationId ?? string.Empty;
			TimelineId = timelineId ?? string.Empty;
			TrackId = trackId ?? string.Empty;
			ClipId = clipId ?? string.Empty;
			DisplayPath = displayPath ?? string.Empty;
			ContentHash = contentHash ?? string.Empty;
			if (GraphId.Length == 0 && TimelineId.Length == 0 && targetKind != ProgramSourceTargetKind.BodyMotion)
				throw new ArgumentException("Source map entry requires a graph or timeline identity.");
		}

		public ProgramSourceTargetKind TargetKind { get; }
		public int TargetIndex { get; }
		public string SourceType { get; }
		public string GraphId { get; }
		public string NodeId { get; }
		public string PortId { get; }
		public string EdgeId { get; }
		public string DeclarationId { get; }
		public string TimelineId { get; }
		public string TrackId { get; }
		public string ClipId { get; }
		public string DisplayPath { get; }
		public string ContentHash { get; }
	}

    public sealed class ProgramProducer
    {
        public ProgramProducer(
            int index,
            string identity,
            AnimationChannelId animationChannelId,
            string sourceIdentity,
            ProgramOutputChannelKind channelKind)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!animationChannelId.IsValid)
                throw new ArgumentException("Program Producer Animation Channel identity is invalid.", nameof(animationChannelId));
            Index = index;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            AnimationChannelId = animationChannelId;
            SourceIdentity = SimulationIdentity.Require(sourceIdentity, nameof(sourceIdentity));
            ChannelKind = channelKind;
        }
        public int Index { get; }
        public string Identity { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public string SourceIdentity { get; }
        public ProgramOutputChannelKind ChannelKind { get; }
    }

    public enum ProgramCatalogEntryKind : byte
    {
        InputValue = 1,
        InputRequest = 2,
        BlackboardDeclaration = 3,
        Action = 4,
        Behavior = 5,
        GameplayTag = 6,
        Attribute = 7,
        GameplayEffect = 8,
        Timeline = 9,
        TimelineTrack = 10,
        TimelineClip = 11,
        MotionCurve = 12,
        CompositionRoot = 20,
        EquipmentSlot = 21,
        EquipmentRoute = 22,
        EquipmentFeature = 23,
        EquipmentFeatureParameter = 24,
        EquipmentFeatureLocalState = 25,
        EquipmentRouteImplementation = 26,
        EquipmentDefinition = 27,
        EquipmentParameterValue = 28,
        EquipmentInitialLoadout = 29,
        EquipmentVisualBinding = 30
    }

    public enum ProgramCatalogFieldKind : byte
    {
        Constant = 1,
        Identity = 2
    }

    public sealed class ProgramCatalogField
    {
        public ProgramCatalogField(string name, ProgramCatalogFieldKind kind, int constantIndex, string identity)
        {
            Name = SimulationIdentity.Require(name, nameof(name));
            Kind = kind;
            if (kind == ProgramCatalogFieldKind.Constant && constantIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(constantIndex));
            ConstantIndex = constantIndex;
            Identity = kind == ProgramCatalogFieldKind.Identity
                ? SimulationIdentity.Require(identity, nameof(identity))
                : string.Empty;
        }
        public string Name { get; }
        public ProgramCatalogFieldKind Kind { get; }
        public int ConstantIndex { get; }
        public string Identity { get; }
    }

    public sealed class ProgramCatalogEntry
    {
        readonly ReadOnlyCollection<ProgramCatalogField> m_Fields;

        public ProgramCatalogEntry(int index, ProgramCatalogEntryKind kind, string identity, int revision, IEnumerable<ProgramCatalogField> fields)
        {
            if (index < 0 || revision < 0)
                throw new ArgumentOutOfRangeException();
            Index = index;
            Kind = kind;
            Identity = SimulationIdentity.Require(identity, nameof(identity));
            Revision = revision;
            var copied = fields == null ? new List<ProgramCatalogField>() : new List<ProgramCatalogField>(fields);
            for (int i = 0; i < copied.Count; i++)
            {
                if (copied[i] == null)
                    throw new ArgumentException($"Catalog entry '{identity}' fields must be non-null and unique.", nameof(fields));
            }
            copied.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
            for (int i = 0; i < copied.Count; i++)
            {
                if (i > 0 && string.Equals(copied[i - 1].Name, copied[i].Name, StringComparison.Ordinal))
                    throw new ArgumentException($"Catalog entry '{identity}' fields must be non-null and unique.", nameof(fields));
            }
            m_Fields = copied.AsReadOnly();
        }
        public int Index { get; }
        public ProgramCatalogEntryKind Kind { get; }
        public string Identity { get; }
        public int Revision { get; }
        public IReadOnlyList<ProgramCatalogField> Fields => m_Fields;
    }

    public sealed class ProgramCapabilityManifest
    {
        readonly ReadOnlyCollection<string> m_GameplayCapabilities;

        public ProgramCapabilityManifest(IEnumerable<string> gameplayCapabilities, WorldCapability requiredWorldCapabilities)
        {
            var values = gameplayCapabilities == null ? new List<string>() : new List<string>(gameplayCapabilities);
            values.Sort(StringComparer.Ordinal);
            for (int i = 0; i < values.Count; i++)
            {
                values[i] = SimulationIdentity.Require(values[i], nameof(gameplayCapabilities));
                if (i > 0 && string.Equals(values[i - 1], values[i], StringComparison.Ordinal))
                    throw new ArgumentException($"Duplicate gameplay capability '{values[i]}'.", nameof(gameplayCapabilities));
            }
            m_GameplayCapabilities = values.AsReadOnly();
            RequiredWorldCapabilities = requiredWorldCapabilities;
        }
        public IReadOnlyList<string> GameplayCapabilities => m_GameplayCapabilities;
        public WorldCapability RequiredWorldCapabilities { get; }
        public bool HasGameplayCapability(string capability)
        {
            if (string.IsNullOrEmpty(capability))
                return false;
            for (int i = 0; i < m_GameplayCapabilities.Count; i++)
            {
                if (string.Equals(m_GameplayCapabilities[i], capability, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
                                                                                                                                                                                                                                                         
