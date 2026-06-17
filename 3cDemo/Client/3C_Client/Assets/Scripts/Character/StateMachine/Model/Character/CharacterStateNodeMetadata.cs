using System;
using System.Collections.Generic;
using ThirdPersonAction;
using ThirdPersonMovement;

namespace ThirdPersonCharacterStateMachine
{
    public readonly struct CharacterStateNodeMetadata
    {
        readonly CharacterStateTag[] tags;
        readonly CharacterStateCapabilityModule[] capabilities;

        CharacterStateNodeMetadata(
            StateGraphNodeId nodeId,
            CharacterStateTag[] tags,
            CharacterStateCapabilityModule[] capabilities,
            FullBodyOwner owner,
            ActionStateId actionState,
            BasicMovementPhase locomotionPhase,
            bool isActionCapabilityState,
            bool isLocomotionPlaybackState)
        {
            NodeId = nodeId;
            this.tags = tags ?? Array.Empty<CharacterStateTag>();
            this.capabilities = capabilities ?? Array.Empty<CharacterStateCapabilityModule>();
            Owner = owner;
            ActionState = actionState;
            LocomotionPhase = locomotionPhase;
            IsActionCapabilityState = isActionCapabilityState;
            IsLocomotionPlaybackState = isLocomotionPlaybackState;
        }

        public StateGraphNodeId NodeId { get; }
        public IReadOnlyList<CharacterStateTag> Tags => tags;
        public IReadOnlyList<CharacterStateCapabilityModule> Capabilities => capabilities;
        public FullBodyOwner Owner { get; }
        public ActionStateId ActionState { get; }
        public BasicMovementPhase LocomotionPhase { get; }
        public bool IsActionCapabilityState { get; }
        public bool IsLocomotionPlaybackState { get; }

        public bool HasTag(CharacterStateTag tag)
        {
            for (int i = 0; i < tags.Length; i++)
                if (tags[i] == tag)
                    return true;
            return false;
        }

        public bool HasCapability(CharacterStateModuleType moduleType)
        {
            return TryGetCapability(moduleType, out _);
        }

        public bool TryGetCapability(CharacterStateModuleType moduleType, out CharacterStateCapabilityModule module)
        {
            for (int i = 0; i < capabilities.Length; i++)
            {
                if (capabilities[i].ModuleType == moduleType)
                {
                    module = capabilities[i];
                    return true;
                }
            }

            module = default;
            return false;
        }

        public static CharacterStateNodeMetadata FromNode(CharacterStateNodeDefinition node)
        {
            if (node == null)
                return default;

            CharacterStateCapabilityModule[] modules = CharacterStateCapabilityModule.FromDefinitions(node.Modules);
            bool isAction = HasTag(node.Tags, CharacterStateTag.Action) || HasAnyCapability(
                modules,
                CharacterStateModuleType.ConfiguredActionMotion,
                CharacterStateModuleType.ActionAnimation,
                CharacterStateModuleType.InputConsume);
            bool isLocomotion = HasTag(node.Tags, CharacterStateTag.Locomotion) || HasAnyCapability(
                modules,
                CharacterStateModuleType.LocomotionPhase,
                CharacterStateModuleType.InputDrivenMotion,
                CharacterStateModuleType.LocomotionAnimationAlias,
                CharacterStateModuleType.TurnBackMotionPolicy);
            BasicMovementPhase phase = TryResolveLocomotionPhase(modules, out BasicMovementPhase resolvedPhase)
                ? resolvedPhase
                : BasicMovementPhase.Idle;
            ActionStateId actionState = isAction ? ResolveActionState(node.StateId.Value) : ActionStateIds.None;
            FullBodyOwner owner = isAction ? FullBodyOwner.Action(actionState) : FullBodyOwner.None;

            return new CharacterStateNodeMetadata(
                new StateGraphNodeId(node.StateId.Value),
                Copy(node.Tags),
                modules,
                owner,
                actionState,
                phase,
                isAction,
                isLocomotion);
        }

        static bool HasAnyCapability(
            CharacterStateCapabilityModule[] modules,
            params CharacterStateModuleType[] moduleTypes)
        {
            for (int i = 0; i < modules.Length; i++)
                for (int j = 0; j < moduleTypes.Length; j++)
                    if (modules[i].ModuleType == moduleTypes[j])
                        return true;
            return false;
        }

        static bool TryResolveLocomotionPhase(
            CharacterStateCapabilityModule[] modules,
            out BasicMovementPhase phase)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i].ModuleType == CharacterStateModuleType.LocomotionPhase)
                {
                    phase = modules[i].LocomotionPhase;
                    return true;
                }
            }

            phase = BasicMovementPhase.Idle;
            return false;
        }

        static bool HasTag(IReadOnlyList<CharacterStateTag> source, CharacterStateTag tag)
        {
            if (source == null)
                return false;

            for (int i = 0; i < source.Count; i++)
                if (source[i] == tag)
                    return true;
            return false;
        }

        static CharacterStateTag[] Copy(IReadOnlyList<CharacterStateTag> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<CharacterStateTag>();

            CharacterStateTag[] result = new CharacterStateTag[source.Count];
            for (int i = 0; i < source.Count; i++)
                result[i] = source[i];
            return result;
        }

        static ActionStateId ResolveActionState(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return ActionStateIds.None;

            if (path.StartsWith("Action.", StringComparison.Ordinal))
                return new ActionStateId(path);

            int index = path.LastIndexOf('/');
            string segment = index >= 0 && index < path.Length - 1 ? path.Substring(index + 1) : path;
            return string.IsNullOrWhiteSpace(segment) ? ActionStateIds.None : new ActionStateId("Action." + segment);
        }
    }
}
