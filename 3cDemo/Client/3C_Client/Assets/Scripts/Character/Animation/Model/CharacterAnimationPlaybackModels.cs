using ThirdPersonAction;
using ThirdPersonCharacterStateMachine;
using ThirdPersonMovement;

namespace ThirdPersonAnimation
{
    public enum CharacterAnimationPlaybackDomain
    {
        None = 0,
        Locomotion = 1,
        Action = 2
    }

    public readonly struct CharacterAnimationPlaybackRequest
    {
        CharacterAnimationPlaybackRequest(
            CharacterAnimationPlaybackDomain domain,
            string stableKey,
            string timelineBindingKey,
            int sourceStep,
            BasicMovementPhase locomotionPhase,
            BasicMovementGait locomotionGait,
            bool hasEntryStartNormalizedTimeOverride,
            float entryStartNormalizedTime,
            ActionAnimationKey actionKey,
            ActionAnimationPlaybackIntent actionPlaybackIntent)
        {
            Domain = domain;
            StableKey = stableKey ?? string.Empty;
            TimelineBindingKey = timelineBindingKey ?? string.Empty;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
            LocomotionPhase = locomotionPhase;
            LocomotionGait = locomotionGait;
            HasEntryStartNormalizedTimeOverride = hasEntryStartNormalizedTimeOverride;
            EntryStartNormalizedTime = entryStartNormalizedTime < 0f ? 0f : entryStartNormalizedTime;
            ActionKey = actionKey;
            ActionPlaybackIntent = actionPlaybackIntent;
        }

        public CharacterAnimationPlaybackDomain Domain { get; }
        public string StableKey { get; }
        public string TimelineBindingKey { get; }
        public int SourceStep { get; }
        public BasicMovementPhase LocomotionPhase { get; }
        public BasicMovementGait LocomotionGait { get; }
        public bool HasEntryStartNormalizedTimeOverride { get; }
        public float EntryStartNormalizedTime { get; }
        public ActionAnimationKey ActionKey { get; }
        public ActionAnimationPlaybackIntent ActionPlaybackIntent { get; }

        public static CharacterAnimationPlaybackRequest FromLocomotion(
            in MovementAnimationContext context,
            string aliasKey,
            int sourceStep = 0)
        {
            return new CharacterAnimationPlaybackRequest(
                CharacterAnimationPlaybackDomain.Locomotion,
                aliasKey,
                string.Empty,
                sourceStep,
                context.Phase,
                context.Gait,
                context.HasEntryStartNormalizedTimeOverride,
                context.HasEntryStartNormalizedTimeOverride
                    ? context.EntryFootPhaseMatchResult.StartNormalizedTime
                    : 0f,
                default,
                ActionAnimationPlaybackIntent.Invalid);
        }

        public static CharacterAnimationPlaybackRequest FromAction(in CharacterStateAnimationRequest request)
        {
            return new CharacterAnimationPlaybackRequest(
                CharacterAnimationPlaybackDomain.Action,
                request.Key.Value,
                request.TimelineBindingKey,
                request.SourceStep,
                default,
                default,
                false,
                0f,
                request.Key,
                request.ActionPlaybackIntent);
        }
    }

    public readonly struct CharacterAnimationPlaybackSnapshot
    {
        public CharacterAnimationPlaybackSnapshot(
            CharacterAnimationPlaybackDomain activeDomain,
            string activeStableKey,
            AnimationPhasePlaybackProgress locomotionProgress,
            string locomotionAnimationName,
            ActionAnimationPlaybackProgress actionProgress,
            string actionAnimationName,
            int sourceStep)
        {
            ActiveDomain = activeDomain;
            ActiveStableKey = activeStableKey ?? string.Empty;
            LocomotionProgress = locomotionProgress;
            LocomotionAnimationName = locomotionAnimationName ?? string.Empty;
            ActionProgress = actionProgress;
            ActionAnimationName = actionAnimationName ?? string.Empty;
            SourceStep = sourceStep < 0 ? 0 : sourceStep;
        }

        public CharacterAnimationPlaybackDomain ActiveDomain { get; }
        public string ActiveStableKey { get; }
        public AnimationPhasePlaybackProgress LocomotionProgress { get; }
        public string LocomotionAnimationName { get; }
        public ActionAnimationPlaybackProgress ActionProgress { get; }
        public string ActionAnimationName { get; }
        public int SourceStep { get; }

        public static CharacterAnimationPlaybackSnapshot Empty(BasicMovementPhase phase)
        {
            return new CharacterAnimationPlaybackSnapshot(
                CharacterAnimationPlaybackDomain.None,
                string.Empty,
                AnimationPhasePlaybackProgress.Invalid(phase),
                string.Empty,
                ActionAnimationPlaybackProgress.Invalid,
                string.Empty,
                0);
        }
    }
}
