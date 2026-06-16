using System;
using System.Collections.Generic;
using ThirdPersonInput;
using ThirdPersonMovement;

namespace ThirdPersonCharacterStateMachine
{
    public readonly struct CharacterStateCapabilityModule
    {
        readonly CharacterStateVariantDefinition[] variants;
        readonly CharacterActionMovementDefinition[] actionMovements;
        readonly StateTimelineWindowDefinition[] timelineWindows;

        CharacterStateCapabilityModule(
            CharacterStateModuleType moduleType,
            BasicMovementPhase locomotionPhase,
            InputRequestKind requestKind,
            CharacterStatePlaybackFactSource playbackFactSource,
            CharacterStateAnimationBinding animation,
            CharacterStateVariantDefinition[] variants,
            CharacterActionMovementDefinition[] actionMovements,
            TurnBackMotionPolicy turnBackMotionPolicy,
            bool resetRunLatchOnEnter,
            bool setRunLatchOnComplete,
            StateTimelineWindowDefinition[] timelineWindows)
        {
            ModuleType = moduleType;
            LocomotionPhase = locomotionPhase;
            RequestKind = requestKind;
            PlaybackFactSource = playbackFactSource;
            Animation = animation;
            this.variants = variants ?? Array.Empty<CharacterStateVariantDefinition>();
            this.actionMovements = actionMovements ?? Array.Empty<CharacterActionMovementDefinition>();
            TurnBackMotionPolicy = turnBackMotionPolicy;
            ResetRunLatchOnEnter = resetRunLatchOnEnter;
            SetRunLatchOnComplete = setRunLatchOnComplete;
            this.timelineWindows = timelineWindows ?? Array.Empty<StateTimelineWindowDefinition>();
        }

        public CharacterStateModuleType ModuleType { get; }
        public BasicMovementPhase LocomotionPhase { get; }
        public InputRequestKind RequestKind { get; }
        public CharacterStatePlaybackFactSource PlaybackFactSource { get; }
        public CharacterStateAnimationBinding Animation { get; }
        public IReadOnlyList<CharacterStateVariantDefinition> Variants => variants;
        public IReadOnlyList<CharacterActionMovementDefinition> ActionMovements => actionMovements;
        public TurnBackMotionPolicy TurnBackMotionPolicy { get; }
        public bool HasTurnBackMotionPolicy => TurnBackMotionPolicy.IsEnabled;
        public bool ResetRunLatchOnEnter { get; }
        public bool SetRunLatchOnComplete { get; }
        public IReadOnlyList<StateTimelineWindowDefinition> TimelineWindows => timelineWindows;

        public static CharacterStateCapabilityModule FromDefinition(CharacterStateModuleDefinition module)
        {
            if (module == null)
                return default;

            return new CharacterStateCapabilityModule(
                module.ModuleType,
                module.LocomotionPhase,
                module.RequestKind,
                module.PlaybackFactSource,
                module.Animation,
                Copy(module.Variants),
                Copy(module.ActionMovements),
                module.HasTurnBackMotionPolicy ? module.TurnBackMotionPolicy : default,
                module.ResetRunLatchOnEnter,
                module.SetRunLatchOnComplete,
                Copy(module.TimelineWindows));
        }

        public static CharacterStateCapabilityModule[] FromDefinitions(IReadOnlyList<CharacterStateModuleDefinition> modules)
        {
            if (modules == null || modules.Count == 0)
                return Array.Empty<CharacterStateCapabilityModule>();

            CharacterStateCapabilityModule[] result = new CharacterStateCapabilityModule[modules.Count];
            for (int i = 0; i < modules.Count; i++)
                result[i] = FromDefinition(modules[i]);
            return result;
        }

        static CharacterStateVariantDefinition[] Copy(IReadOnlyList<CharacterStateVariantDefinition> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<CharacterStateVariantDefinition>();

            CharacterStateVariantDefinition[] result = new CharacterStateVariantDefinition[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];
            return result;
        }

        static CharacterActionMovementDefinition[] Copy(IReadOnlyList<CharacterActionMovementDefinition> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<CharacterActionMovementDefinition>();

            CharacterActionMovementDefinition[] result = new CharacterActionMovementDefinition[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];
            return result;
        }

        static StateTimelineWindowDefinition[] Copy(IReadOnlyList<StateTimelineWindowDefinition> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<StateTimelineWindowDefinition>();

            StateTimelineWindowDefinition[] result = new StateTimelineWindowDefinition[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];
            return result;
        }
    }
}
