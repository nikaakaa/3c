using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public enum StateTimelineWindowKind
    {
        Custom = 0,
        Motion = 1,
        InputLock = 2,
        Interrupt = 3,
        Exit = 4,
        Cancel = 5
    }

    public enum StateTimelineTimeDomain
    {
        Normalized = 0,
        Seconds = 1
    }

    [Serializable]
    public readonly struct TimelineFactId : IEquatable<TimelineFactId>
    {
        readonly string value;

        public TimelineFactId(string value)
        {
            this.value = Normalize(value);
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(TimelineFactId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is TimelineFactId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(TimelineFactId left, TimelineFactId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimelineFactId left, TimelineFactId right)
        {
            return !left.Equals(right);
        }

        public static string Normalize(string raw)
        {
            return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim();
        }
    }

    public static class TimelineFactIds
    {
        public static readonly TimelineFactId InputLocked = new TimelineFactId("InputLocked");
        public static readonly TimelineFactId MotionActive = new TimelineFactId("MotionActive");
        public static readonly TimelineFactId MotionLocked = new TimelineFactId("MotionLocked");
        public static readonly TimelineFactId NaturalExitReady = new TimelineFactId("NaturalExitReady");
        public static readonly TimelineFactId CancelableToDodge = new TimelineFactId("CancelableToDodge");
        public static readonly TimelineFactId ComboInputOpen = new TimelineFactId("ComboInputOpen");
        public static readonly TimelineFactId TurnBackEnterOpen = new TimelineFactId("TurnBackEnterOpen");
    }

    [Serializable]
    public struct StateTimelineWindowDefinition
    {
        [SerializeField] string windowId;
        [SerializeField] string factId;
        [SerializeField] StateTimelineWindowKind kind;
        [SerializeField] StateTimelineTimeDomain timeDomain;
        [SerializeField] float start;
        [SerializeField] float end;
        [SerializeField, Min(0)] int priority;
        [SerializeField, Min(0)] int resistance;
        [SerializeField, Min(0)] int minPriority;
        [SerializeField] bool force;
        [SerializeField] ActionRequestType requestType;
        [SerializeField] string note;

        public StateTimelineWindowDefinition(
            string windowId,
            StateTimelineWindowKind kind,
            StateTimelineTimeDomain timeDomain,
            float start,
            float end,
            int priority = 0,
            int resistance = 0,
            int minPriority = 0,
            bool force = false,
            ActionRequestType requestType = ActionRequestType.None,
            string note = "",
            string factId = "")
        {
            this.windowId = (windowId ?? string.Empty).Trim();
            this.factId = TimelineFactId.Normalize(factId);
            this.kind = kind;
            this.timeDomain = timeDomain;
            this.start = start;
            this.end = end;
            this.priority = Mathf.Max(0, priority);
            this.resistance = Mathf.Max(0, resistance);
            this.minPriority = Mathf.Max(0, minPriority);
            this.force = force;
            this.requestType = requestType;
            this.note = note ?? string.Empty;
        }

        public string WindowId => windowId ?? string.Empty;
        public TimelineFactId FactId => new TimelineFactId(factId);
        public StateTimelineWindowKind Kind => kind;
        public StateTimelineTimeDomain TimeDomain => timeDomain;
        public float Start => start;
        public float End => end;
        public int Priority => Mathf.Max(0, priority);
        public int Resistance => Mathf.Max(0, resistance);
        public int MinPriority => Mathf.Max(0, minPriority);
        public bool Force => force;
        public ActionRequestType RequestType => requestType;
        public string Note => note ?? string.Empty;

        public bool AllowsRequest(ActionRequestType candidate)
        {
            return candidate == ActionRequestType.None ||
                   requestType == candidate;
        }

        public bool IsRequestWindow => kind == StateTimelineWindowKind.Interrupt || kind == StateTimelineWindowKind.Cancel;
    }

    [Serializable]
    public struct StateTimelinePolicyDefinition
    {
        [SerializeField] string stateId;
        [SerializeField, Min(0)] int priority;
        [SerializeField, Min(0)] int resistance;
        [SerializeField] StateTimelineWindowDefinition[] windows;
        [SerializeField] string note;

        public StateTimelinePolicyDefinition(
            string stateId,
            int priority,
            int resistance,
            StateTimelineWindowDefinition[] windows,
            string note = "")
        {
            this.stateId = CharacterStateId.Normalize(stateId);
            this.priority = Mathf.Max(0, priority);
            this.resistance = Mathf.Max(0, resistance);
            this.windows = windows ?? Array.Empty<StateTimelineWindowDefinition>();
            this.note = note ?? string.Empty;
        }

        public CharacterStateId StateId => new CharacterStateId(stateId);
        public int Priority => Mathf.Max(0, priority);
        public int Resistance => Mathf.Max(0, resistance);
        public IReadOnlyList<StateTimelineWindowDefinition> Windows => windows ?? Array.Empty<StateTimelineWindowDefinition>();
        public string Note => note ?? string.Empty;
    }

    public readonly struct StateTimelineWindowFacts
    {
        public StateTimelineWindowFacts(
            CharacterStateId stateId,
            float normalizedTime,
            bool hasValidNormalizedTime,
            float elapsedSeconds,
            bool motionWindowActive,
            bool inputLockWindowActive,
            bool interruptWindowActive,
            bool exitWindowActive,
            int priority,
            int resistance,
            int minPriority,
            bool force,
            string activeWindowIds)
            : this(
                stateId,
                normalizedTime,
                hasValidNormalizedTime,
                elapsedSeconds,
                motionWindowActive,
                inputLockWindowActive,
                interruptWindowActive,
                exitWindowActive,
                priority,
                resistance,
                minPriority,
                force,
                activeWindowIds,
                string.Empty)
        {
        }

        public StateTimelineWindowFacts(
            CharacterStateId stateId,
            float normalizedTime,
            bool hasValidNormalizedTime,
            float elapsedSeconds,
            bool motionWindowActive,
            bool inputLockWindowActive,
            bool interruptWindowActive,
            bool exitWindowActive,
            int priority,
            int resistance,
            int minPriority,
            bool force,
            string activeWindowIds,
            string requestWindowIds,
            string activeFactIds = "",
            string requestFactIds = "")
        {
            StateId = stateId;
            NormalizedTime = Mathf.Max(0f, normalizedTime);
            HasValidNormalizedTime = hasValidNormalizedTime;
            ElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            MotionWindowActive = motionWindowActive;
            InputLockWindowActive = inputLockWindowActive;
            InterruptWindowActive = interruptWindowActive;
            ExitWindowActive = exitWindowActive;
            Priority = Mathf.Max(0, priority);
            Resistance = Mathf.Max(0, resistance);
            MinPriority = Mathf.Max(0, minPriority);
            Force = force;
            ActiveWindowIds = activeWindowIds ?? string.Empty;
            RequestWindowIds = requestWindowIds ?? string.Empty;
            ActiveFactIds = activeFactIds ?? string.Empty;
            RequestFactIds = requestFactIds ?? string.Empty;
        }

        public CharacterStateId StateId { get; }
        public float NormalizedTime { get; }
        public bool HasValidNormalizedTime { get; }
        public float ElapsedSeconds { get; }
        public bool MotionWindowActive { get; }
        public bool InputLockWindowActive { get; }
        public bool InterruptWindowActive { get; }
        public bool ExitWindowActive { get; }
        public int Priority { get; }
        public int Resistance { get; }
        public int MinPriority { get; }
        public bool Force { get; }
        public string ActiveWindowIds { get; }
        public string RequestWindowIds { get; }
        public string ActiveFactIds { get; }
        public string RequestFactIds { get; }
        public bool HasActiveWindow => MotionWindowActive || InputLockWindowActive || InterruptWindowActive || ExitWindowActive || !string.IsNullOrEmpty(ActiveWindowIds) || !string.IsNullOrEmpty(ActiveFactIds);
        public bool HasRequestWindow => !string.IsNullOrEmpty(RequestWindowIds) || !string.IsNullOrEmpty(RequestFactIds);

        public bool Contains(TimelineFactId factId)
        {
            return ContainsId(ActiveFactIds, factId.Value);
        }

        public bool ContainsRequestFact(TimelineFactId factId)
        {
            return ContainsId(RequestFactIds, factId.Value);
        }

        public IEnumerable<TimelineFactId> EnumerateActiveFacts()
        {
            if (string.IsNullOrWhiteSpace(ActiveFactIds))
                yield break;

            string[] ids = ActiveFactIds.Split(',');
            for (int i = 0; i < ids.Length; i++)
            {
                TimelineFactId factId = new TimelineFactId(ids[i]);
                if (factId.IsValid)
                    yield return factId;
            }
        }

        public static StateTimelineWindowFacts None(CharacterStateId stateId)
        {
            return new StateTimelineWindowFacts(stateId, 0f, false, 0f, false, false, false, false, 0, 0, 0, false, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        static bool ContainsId(string ids, string required)
        {
            if (string.IsNullOrWhiteSpace(ids) || string.IsNullOrWhiteSpace(required))
                return false;

            string[] split = ids.Split(',');
            for (int i = 0; i < split.Length; i++)
            {
                if (string.Equals(split[i].Trim(), required, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    public sealed class StateTimelinePolicyValidationResult
    {
        readonly List<string> errors = new List<string>();
        readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Errors => errors;
        public IReadOnlyList<string> Warnings => warnings;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                errors.Add(message);
        }

        public void AddWarning(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                warnings.Add(message);
        }

        public string DescribeErrors()
        {
            return string.Join(Environment.NewLine, errors);
        }
    }

    public enum CharacterStateTag
    {
        FullBody,
        Locomotion,
        Action,
        Dodge,
        Movement
    }

    public enum CharacterStateVariant
    {
        None,
        Directional,
        Backstep
    }

    public enum CharacterStateTransitionConditionKind
    {
        HasMoveIntent = 0,
        NoMoveIntent = 1,
        StateCanExit = 2,
        HasInputRequest = 3,
        StateElapsedAtLeast = 4,
        CurrentStateHasTag = 6,
        MoveTurnBackRequested = 7,
        LocomotionAnimationCanExit = 8,
        ActionCanExit = 9,
        LocomotionPreemptionPending = 10
    }

    public enum CharacterStateModuleType
    {
        LocomotionPhase = 0,
        InputDrivenMotion = 1,
        ConfiguredActionMotion = 2,
        ActionAnimation = 3,
        LocomotionAnimationAlias = 4,
        TurnBackMotionPolicy = 5,
        InputConsume = 6,
        RunLatch = 7,
        TimelineWindow = 8
    }

    public enum CharacterStatePlaybackFactSource
    {
        None = 0,
        Locomotion = 1,
        Action = 2
    }

    [Serializable]
    public struct CharacterActionMovementDefinition
    {
        [SerializeField] CharacterStateVariant variant;
        [SerializeField, Min(0f)] float duration;
        [SerializeField, Min(0f)] float distance;
        [SerializeField] bool rotateToDirection;
        [SerializeField] bool setRunLatchOnComplete;

        public CharacterActionMovementDefinition(
            CharacterStateVariant variant,
            float duration,
            float distance,
            bool rotateToDirection,
            bool setRunLatchOnComplete)
        {
            this.variant = variant;
            this.duration = Mathf.Max(0f, duration);
            this.distance = Mathf.Max(0f, distance);
            this.rotateToDirection = rotateToDirection;
            this.setRunLatchOnComplete = setRunLatchOnComplete;
        }

        public CharacterStateVariant Variant => variant;
        public float Duration => Mathf.Max(0f, duration);
        public float Distance => Mathf.Max(0f, distance);
        public bool RotateToDirection => rotateToDirection;
        public bool SetRunLatchOnComplete => setRunLatchOnComplete;
        public bool IsValid => variant != CharacterStateVariant.None || Duration > 0f || Distance > 0f || setRunLatchOnComplete;
    }

    [Serializable]
    public readonly struct CharacterStateId : IEquatable<CharacterStateId>
    {
        readonly string value;

        public CharacterStateId(string value)
        {
            this.value = Normalize(value);
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CharacterStateId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterStateId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static bool operator ==(CharacterStateId left, CharacterStateId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CharacterStateId left, CharacterStateId right)
        {
            return !left.Equals(right);
        }

        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            string[] parts = raw.Trim().Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("/", parts);
        }
    }

    public static class CharacterStateIds
    {
        public static readonly CharacterStateId FullBody = new CharacterStateId("FullBody");
        public static readonly CharacterStateId Locomotion = new CharacterStateId("Locomotion");
        public static readonly CharacterStateId Idle = new CharacterStateId("Locomotion.Idle");
        public static readonly CharacterStateId MoveStart = new CharacterStateId("Locomotion.MoveStart");
        public static readonly CharacterStateId MoveLoop = new CharacterStateId("Locomotion.MoveLoop");
        public static readonly CharacterStateId MoveStop = new CharacterStateId("Locomotion.MoveStop");
        public static readonly CharacterStateId TurnBack = new CharacterStateId("Locomotion.TurnBack");
        public static readonly CharacterStateId Action = new CharacterStateId("Action");
        public static readonly CharacterStateId Dodge = new CharacterStateId("Action.Dodge");
    }

    [Serializable]
    public struct CharacterStateAnimationBinding
    {
        [SerializeField] string animationKey;
        [SerializeField] string timelineBindingKey;
        [SerializeField] string debugName;

        public CharacterStateAnimationBinding(
            string animationKey,
            string timelineBindingKey,
            string debugName)
        {
            this.animationKey = (animationKey ?? string.Empty).Trim();
            this.timelineBindingKey = (timelineBindingKey ?? string.Empty).Trim();
            this.debugName = debugName ?? string.Empty;
        }

        public ActionAnimationKey Key => new ActionAnimationKey(animationKey);
        public string KeyValue => animationKey ?? string.Empty;
        public string TimelineBindingKey => string.IsNullOrWhiteSpace(timelineBindingKey) ? KeyValue : timelineBindingKey;
        public string DebugName => debugName ?? string.Empty;
        public bool HasKey => Key.IsValid;

        public static CharacterStateAnimationBinding FromKey(string key, string debugName)
        {
            return new CharacterStateAnimationBinding(key, key, debugName);
        }

        public static CharacterStateAnimationBinding FromLibraryKey(string key, string debugName)
        {
            return FromKey(key, debugName);
        }
    }

    [Serializable]
    public struct CharacterStateVariantDefinition
    {
        [SerializeField] CharacterStateVariant variant;
        [SerializeField] CharacterStateAnimationBinding animation;

        public CharacterStateVariantDefinition(CharacterStateVariant variant, CharacterStateAnimationBinding animation)
        {
            this.variant = variant;
            this.animation = animation;
        }

        public CharacterStateVariant Variant => variant;
        public CharacterStateAnimationBinding Animation => animation;
    }

    [Serializable]
    public sealed class CharacterStateModuleDefinition
    {
        [SerializeField] CharacterStateModuleType moduleType;
        [SerializeField] BasicMovementPhase locomotionPhase;
        [SerializeField] InputRequestKind requestKind;
        [SerializeField] CharacterStatePlaybackFactSource playbackFactSource;
        [SerializeField] CharacterStateAnimationBinding animation;
        [SerializeField] CharacterStateVariantDefinition[] variants = Array.Empty<CharacterStateVariantDefinition>();
        [SerializeField] CharacterActionMovementDefinition[] actionMovements = Array.Empty<CharacterActionMovementDefinition>();
        [SerializeField] TurnBackMotionPolicy turnBackMotionPolicy;
        [SerializeField] bool resetRunLatchOnEnter;
        [SerializeField] bool setRunLatchOnComplete;
        [SerializeField] StateTimelineWindowDefinition[] timelineWindows = Array.Empty<StateTimelineWindowDefinition>();

        public CharacterStateModuleDefinition()
        {
        }

        CharacterStateModuleDefinition(CharacterStateModuleType moduleType)
        {
            this.moduleType = moduleType;
        }

        public CharacterStateModuleType ModuleType => moduleType;
        public BasicMovementPhase LocomotionPhase => locomotionPhase;
        public InputRequestKind RequestKind => requestKind;
        public CharacterStatePlaybackFactSource PlaybackFactSource => playbackFactSource;
        public CharacterStateAnimationBinding Animation => animation;
        public IReadOnlyList<CharacterStateVariantDefinition> Variants => variants ?? Array.Empty<CharacterStateVariantDefinition>();
        public IReadOnlyList<CharacterActionMovementDefinition> ActionMovements => actionMovements ?? Array.Empty<CharacterActionMovementDefinition>();
        public TurnBackMotionPolicy TurnBackMotionPolicy =>
            turnBackMotionPolicy.IsEnabled ? turnBackMotionPolicy : ThirdPersonMovement.TurnBackMotionPolicy.Default;
        public bool HasTurnBackMotionPolicy => turnBackMotionPolicy.IsEnabled;
        public bool ResetRunLatchOnEnter => resetRunLatchOnEnter;
        public bool SetRunLatchOnComplete => setRunLatchOnComplete;
        public IReadOnlyList<StateTimelineWindowDefinition> TimelineWindows => timelineWindows ?? Array.Empty<StateTimelineWindowDefinition>();

        public bool TryResolveVariant(CharacterStateVariant variant, out CharacterStateVariantDefinition definition)
        {
            CharacterStateVariantDefinition[] source = variants;
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i].Variant == variant)
                    {
                        definition = source[i];
                        return true;
                    }
                }
            }

            definition = default;
            return false;
        }

        public bool TryResolveActionMovement(CharacterStateVariant variant, out CharacterActionMovementDefinition movement)
        {
            CharacterActionMovementDefinition[] source = actionMovements;
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i].Variant == variant && source[i].IsValid)
                    {
                        movement = source[i];
                        return true;
                    }
                }

                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i].Variant == CharacterStateVariant.None && source[i].IsValid)
                    {
                        movement = source[i];
                        return true;
                    }
                }
            }

            movement = default;
            return false;
        }

        public static CharacterStateModuleDefinition LocomotionPhaseModule(BasicMovementPhase phase)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.LocomotionPhase)
            {
                locomotionPhase = phase
            };
        }

        public static CharacterStateModuleDefinition InputDrivenMotion()
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.InputDrivenMotion);
        }

        public static CharacterStateModuleDefinition ConfiguredActionMotion(params CharacterActionMovementDefinition[] movements)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.ConfiguredActionMotion)
            {
                actionMovements = movements ?? Array.Empty<CharacterActionMovementDefinition>()
            };
        }

        public static CharacterStateModuleDefinition ActionAnimation(
            CharacterStateAnimationBinding animation,
            CharacterStateVariantDefinition[] variants = null)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.ActionAnimation)
            {
                playbackFactSource = CharacterStatePlaybackFactSource.Action,
                animation = animation,
                variants = variants ?? Array.Empty<CharacterStateVariantDefinition>()
            };
        }

        public static CharacterStateModuleDefinition LocomotionAnimationAlias(CharacterStateAnimationBinding animation)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.LocomotionAnimationAlias)
            {
                playbackFactSource = CharacterStatePlaybackFactSource.Locomotion,
                animation = animation
            };
        }

        public static CharacterStateModuleDefinition TurnBackMotionPolicyModule(TurnBackMotionPolicy policy)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.TurnBackMotionPolicy)
            {
                turnBackMotionPolicy = policy.IsEnabled ? policy : ThirdPersonMovement.TurnBackMotionPolicy.Default
            };
        }

        public static CharacterStateModuleDefinition InputConsume(InputRequestKind kind)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.InputConsume)
            {
                requestKind = kind
            };
        }

        public static CharacterStateModuleDefinition RunLatch(bool resetOnEnter, bool setOnComplete)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.RunLatch)
            {
                resetRunLatchOnEnter = resetOnEnter,
                setRunLatchOnComplete = setOnComplete
            };
        }

        public static CharacterStateModuleDefinition TimelineWindow(params StateTimelineWindowDefinition[] windows)
        {
            return new CharacterStateModuleDefinition(CharacterStateModuleType.TimelineWindow)
            {
                timelineWindows = windows ?? Array.Empty<StateTimelineWindowDefinition>()
            };
        }
    }

    [Serializable]
    public sealed class CharacterStateNodeDefinition
    {
        [SerializeField] string stateId;
        [SerializeField] string parentStateId;
        [SerializeField] string pathSegment;
        [SerializeField] CharacterStateTag[] tags = Array.Empty<CharacterStateTag>();
        [SerializeField] CharacterStateModuleDefinition[] modules = Array.Empty<CharacterStateModuleDefinition>();

        public CharacterStateNodeDefinition()
        {
        }

        public CharacterStateNodeDefinition(
            CharacterStateId stateId,
            CharacterStateId parentStateId,
            string pathSegment,
            CharacterStateTag[] tags,
            CharacterStateModuleDefinition[] modules)
        {
            this.stateId = stateId.Value;
            this.parentStateId = parentStateId.Value;
            this.pathSegment = pathSegment ?? string.Empty;
            this.tags = tags ?? Array.Empty<CharacterStateTag>();
            this.modules = modules ?? Array.Empty<CharacterStateModuleDefinition>();
        }

        public CharacterStateId StateId => new CharacterStateId(stateId);
        public CharacterStateId ParentStateId => new CharacterStateId(parentStateId);
        public string PathSegment => pathSegment ?? string.Empty;
        public IReadOnlyList<CharacterStateTag> Tags => tags ?? Array.Empty<CharacterStateTag>();
        public IReadOnlyList<CharacterStateModuleDefinition> Modules => modules ?? Array.Empty<CharacterStateModuleDefinition>();

        public bool HasTag(CharacterStateTag tag)
        {
            CharacterStateTag[] source = tags;
            if (source == null)
                return false;

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == tag)
                    return true;
            }

            return false;
        }

        public bool TryResolveVariant(CharacterStateVariant variant, out CharacterStateVariantDefinition definition)
        {
            if (TryGetModule(CharacterStateModuleType.ActionAnimation, out CharacterStateModuleDefinition actionAnimation) &&
                actionAnimation.TryResolveVariant(variant, out definition))
            {
                return true;
            }

            definition = default;
            return false;
        }

        public bool HasModule(CharacterStateModuleType moduleType)
        {
            return TryGetModule(moduleType, out _);
        }

        public bool TryGetModule(CharacterStateModuleType moduleType, out CharacterStateModuleDefinition module)
        {
            CharacterStateModuleDefinition[] source = modules;
            if (source != null)
            {
                for (int i = 0; i < source.Length; i++)
                {
                    if (source[i] != null && source[i].ModuleType == moduleType)
                    {
                        module = source[i];
                        return true;
                    }
                }
            }

            module = null;
            return false;
        }

        public bool TryResolveActionMovement(CharacterStateVariant variant, out CharacterActionMovementDefinition movement)
        {
            if (TryGetModule(CharacterStateModuleType.ConfiguredActionMotion, out CharacterStateModuleDefinition module))
                return module.TryResolveActionMovement(variant, out movement);

            movement = default;
            return false;
        }

        public bool TryResolveAnimationBinding(
            CharacterStateVariant variant,
            out CharacterStateAnimationBinding binding,
            out CharacterStatePlaybackFactSource playbackFactSource)
        {
            if (TryGetModule(CharacterStateModuleType.ActionAnimation, out CharacterStateModuleDefinition actionAnimation))
            {
                binding = actionAnimation.Animation;
                if (variant != CharacterStateVariant.None && actionAnimation.TryResolveVariant(variant, out CharacterStateVariantDefinition variantDefinition))
                    binding = variantDefinition.Animation;

                playbackFactSource = actionAnimation.PlaybackFactSource;
                return binding.HasKey;
            }

            if (TryGetModule(CharacterStateModuleType.LocomotionAnimationAlias, out CharacterStateModuleDefinition locomotionAnimation))
            {
                binding = locomotionAnimation.Animation;
                playbackFactSource = locomotionAnimation.PlaybackFactSource;
                return binding.HasKey;
            }

            binding = default;
            playbackFactSource = CharacterStatePlaybackFactSource.None;
            return false;
        }

        public bool TryGetTurnBackMotionPolicy(out TurnBackMotionPolicy policy)
        {
            if (TryGetModule(CharacterStateModuleType.TurnBackMotionPolicy, out CharacterStateModuleDefinition module) &&
                module.HasTurnBackMotionPolicy)
            {
                policy = module.TurnBackMotionPolicy;
                return true;
            }

            policy = default;
            return false;
        }

        public bool TryGetInputConsumeKind(out InputRequestKind kind)
        {
            if (TryGetModule(CharacterStateModuleType.InputConsume, out CharacterStateModuleDefinition module))
            {
                kind = module.RequestKind;
                return true;
            }

            kind = default;
            return false;
        }

        public bool ResetRunLatchOnEnterFromModules =>
            TryGetModule(CharacterStateModuleType.RunLatch, out CharacterStateModuleDefinition module) && module.ResetRunLatchOnEnter;

        public bool IsActionCapabilityState =>
            HasModule(CharacterStateModuleType.ConfiguredActionMotion) ||
            HasModule(CharacterStateModuleType.ActionAnimation) ||
            HasModule(CharacterStateModuleType.InputConsume);

        public bool IsLocomotionPlaybackState =>
            HasModule(CharacterStateModuleType.LocomotionPhase) ||
            HasModule(CharacterStateModuleType.InputDrivenMotion) ||
            HasModule(CharacterStateModuleType.LocomotionAnimationAlias) ||
            HasModule(CharacterStateModuleType.TurnBackMotionPolicy);

        public bool HasOutputModule =>
            HasModule(CharacterStateModuleType.InputDrivenMotion) ||
            HasModule(CharacterStateModuleType.ConfiguredActionMotion) ||
            HasModule(CharacterStateModuleType.LocomotionAnimationAlias) ||
            HasModule(CharacterStateModuleType.TurnBackMotionPolicy) ||
            HasModule(CharacterStateModuleType.InputConsume) ||
            HasModule(CharacterStateModuleType.RunLatch);

        static BasicMovementPhase ResolveLocomotionPhase(CharacterStateId id)
        {
            if (id == CharacterStateIds.MoveStart)
                return BasicMovementPhase.MoveStart;
            if (id == CharacterStateIds.MoveLoop)
                return BasicMovementPhase.MoveLoop;
            if (id == CharacterStateIds.MoveStop)
                return BasicMovementPhase.MoveStop;
            if (id == CharacterStateIds.TurnBack)
                return BasicMovementPhase.TurnBack;
            return BasicMovementPhase.Idle;
        }
    }

    [Serializable]
    public struct CharacterStateTransitionCondition
    {
        [SerializeField] CharacterStateTransitionConditionKind kind;
        [SerializeField] InputRequestKind requestKind;
        [SerializeField, Min(0f)] float minSeconds;
        [SerializeField, Min(0)] int minPriority;
        [SerializeField] CharacterStateTag tag;

        public CharacterStateTransitionCondition(
            CharacterStateTransitionConditionKind kind,
            InputRequestKind requestKind = InputRequestKind.Dodge,
            float minSeconds = 0f,
            int minPriority = 0,
            CharacterStateTag tag = CharacterStateTag.FullBody)
        {
            this.kind = kind;
            this.requestKind = requestKind;
            this.minSeconds = Mathf.Max(0f, minSeconds);
            this.minPriority = Mathf.Max(0, minPriority);
            this.tag = tag;
        }

        public CharacterStateTransitionConditionKind Kind => kind;
        public InputRequestKind RequestKind => requestKind;
        public float MinSeconds => Mathf.Max(0f, minSeconds);
        public int MinPriority => Mathf.Max(0, minPriority);
        public CharacterStateTag Tag => tag;

        public static CharacterStateTransitionCondition HasMoveIntent()
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.HasMoveIntent);
        }

        public static CharacterStateTransitionCondition NoMoveIntent()
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.NoMoveIntent);
        }

        public static CharacterStateTransitionCondition StateCanExit()
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.StateCanExit);
        }

        public static CharacterStateTransitionCondition HasInputRequest(InputRequestKind requestKind)
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.HasInputRequest, requestKind);
        }

        public static CharacterStateTransitionCondition StateElapsedAtLeast(float seconds)
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.StateElapsedAtLeast, minSeconds: seconds);
        }

        public static CharacterStateTransitionCondition CurrentStateHasTag(CharacterStateTag tag)
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.CurrentStateHasTag, tag: tag);
        }

        public static CharacterStateTransitionCondition MoveTurnBackRequested(float minAngle)
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.MoveTurnBackRequested, minSeconds: minAngle);
        }

        public static CharacterStateTransitionCondition LocomotionAnimationCanExit()
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.LocomotionAnimationCanExit);
        }

        public static CharacterStateTransitionCondition ActionCanExit()
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.ActionCanExit);
        }

        public static CharacterStateTransitionCondition LocomotionPreemptionPending()
        {
            return new CharacterStateTransitionCondition(CharacterStateTransitionConditionKind.LocomotionPreemptionPending);
        }
    }

    [Serializable]
    public sealed class CharacterStateTransitionDefinition
    {
        [SerializeField] string fromStateId;
        [SerializeField] string toStateId;
        [SerializeField] int priority;
        [SerializeField] CharacterStateTransitionCondition[] conditions = Array.Empty<CharacterStateTransitionCondition>();

        public CharacterStateTransitionDefinition()
        {
        }

        public CharacterStateTransitionDefinition(
            string fromStateId,
            CharacterStateId toStateId,
            int priority,
            params CharacterStateTransitionCondition[] conditions)
        {
            this.fromStateId = CharacterStateId.Normalize(fromStateId);
            this.toStateId = toStateId.Value;
            this.priority = priority;
            this.conditions = conditions ?? Array.Empty<CharacterStateTransitionCondition>();
        }

        public string FromStateId => CharacterStateId.Normalize(fromStateId);
        public CharacterStateId ToStateId => new CharacterStateId(toStateId);
        public int Priority => priority;
        public IReadOnlyList<CharacterStateTransitionCondition> Conditions => conditions ?? Array.Empty<CharacterStateTransitionCondition>();
        public string TransitionPath => $"{FromStateId}->{ToStateId.Value}";

        public bool MatchesSource(CharacterStateId currentState)
        {
            string source = FromStateId;
            if (source == "*")
                return true;

            if (source.EndsWith("/*", StringComparison.Ordinal))
            {
                string prefix = source.Substring(0, source.Length - 1);
                return currentState.Value.StartsWith(prefix, StringComparison.Ordinal);
            }

            if (source.EndsWith(".*", StringComparison.Ordinal))
            {
                string prefix = source.Substring(0, source.Length - 1);
                return currentState.Value.StartsWith(prefix, StringComparison.Ordinal);
            }

            return string.Equals(source, currentState.Value, StringComparison.Ordinal);
        }
    }

    public readonly struct CharacterInputRequestFact
    {
        public CharacterInputRequestFact(
            bool hasRequest,
            InputRequestKind requestKind,
            int originStep,
            int expireStep,
            int priority,
            CharacterStateVariant variant,
            Vector3 worldDirection)
        {
            HasRequest = hasRequest;
            RequestKind = requestKind;
            OriginStep = Mathf.Max(0, originStep);
            ExpireStep = Mathf.Max(OriginStep, expireStep);
            Priority = Mathf.Max(0, priority);
            Variant = variant;
            WorldDirection = NormalizePlanarOrZero(worldDirection);
        }

        public bool HasRequest { get; }
        public InputRequestKind RequestKind { get; }
        public int OriginStep { get; }
        public int ExpireStep { get; }
        public int Priority { get; }
        public CharacterStateVariant Variant { get; }
        public Vector3 WorldDirection { get; }
        public bool HasWorldDirection => WorldDirection.sqrMagnitude > 0.000001f;

        public static CharacterInputRequestFact None(InputRequestKind kind)
        {
            return new CharacterInputRequestFact(false, kind, 0, 0, 0, CharacterStateVariant.None, Vector3.zero);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
