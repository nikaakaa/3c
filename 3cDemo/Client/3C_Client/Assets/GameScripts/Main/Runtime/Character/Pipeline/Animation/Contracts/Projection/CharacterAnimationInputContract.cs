using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.EventGraphs;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct CharacterAnimationInputSlot : IEquatable<CharacterAnimationInputSlot>
    {
        public CharacterAnimationInputSlot(
            AnimationSlotId slotId,
            AnimationSlotGroupId groupId,
            AnimationChannelId animationChannelId,
            AnimationSelectionAvailabilityPolicy selectionAvailability)
        {
            SlotId = slotId.IsValid
                ? slotId
                : throw new ArgumentException("Animation input Slot identity is invalid.", nameof(slotId));
            GroupId = groupId.IsValid
                ? groupId
                : throw new ArgumentException("Animation input Slot Group identity is invalid.", nameof(groupId));
            AnimationChannelId = animationChannelId.IsValid
                ? animationChannelId
                : throw new ArgumentException("Animation input Channel identity is invalid.", nameof(animationChannelId));
            if (!Enum.IsDefined(typeof(AnimationSelectionAvailabilityPolicy), selectionAvailability))
                throw new ArgumentOutOfRangeException(nameof(selectionAvailability));
            SelectionAvailability = selectionAvailability;
        }

        public AnimationSlotId SlotId { get; }
        public AnimationSlotGroupId GroupId { get; }
        public AnimationChannelId AnimationChannelId { get; }
        public AnimationSelectionAvailabilityPolicy SelectionAvailability { get; }

        public bool Equals(CharacterAnimationInputSlot other) =>
            SlotId == other.SlotId &&
            GroupId == other.GroupId &&
            AnimationChannelId == other.AnimationChannelId &&
            SelectionAvailability == other.SelectionAvailability;

        public override bool Equals(object obj) =>
            obj is CharacterAnimationInputSlot other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(SlotId, GroupId, AnimationChannelId, SelectionAvailability);
    }

    public sealed class CharacterAnimationInputContract
    {
        readonly CharacterPoseParameterDeclaration[] m_Parameters;
        readonly CharacterPresentationFactDeclaration[] m_Facts;
        readonly CharacterAnimationInputSlot[] m_Slots;
        readonly string[] m_WorldCapabilities;
        readonly string[] m_MovementModeStateIdentities;
        readonly CharacterAnimationVariableContract m_AnimationVariables;

        CharacterAnimationInputContract(
            CharacterAnimationPresentationProfile profile,
            CharacterPoseParameterDeclaration[] parameters,
            CharacterPresentationFactDeclaration[] facts,
            CharacterAnimationInputSlot[] slots,
            string[] worldCapabilities,
            string[] movementModeStateIdentities)
        {
            m_Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
            m_Slots = slots ?? throw new ArgumentNullException(nameof(slots));
            m_WorldCapabilities = worldCapabilities ??
                throw new ArgumentNullException(nameof(worldCapabilities));
            m_MovementModeStateIdentities = movementModeStateIdentities ??
                throw new ArgumentNullException(nameof(movementModeStateIdentities));
            if (profile != null && profile.EventGraph)
            {
                m_AnimationVariables = new CharacterAnimationVariableContract(
                    profile.EventGraph.BuildVariableContract());
            }
            var hashParts = new List<string>
            {
                "character-animation-input-contract/v2"
            };
            for (int i = 0; i < m_Parameters.Length; i++)
            {
                CharacterPoseParameterDeclaration parameter = m_Parameters[i];
                hashParts.Add(parameter.ParameterId.Value);
                hashParts.Add(((int)parameter.ValueType).ToString());
                hashParts.Add(((int)parameter.Usage).ToString());
                hashParts.Add(parameter.Unit);
                hashParts.Add(parameter.DefaultValue.ToString("R"));
            }
            if (m_AnimationVariables != null)
            {
                hashParts.Add("event-graph");
                hashParts.Add(m_AnimationVariables.GraphId);
                hashParts.Add(m_AnimationVariables.Revision);
                hashParts.Add(m_AnimationVariables.LayoutId);
                for (int i = 0; i < m_AnimationVariables.Variables.Count; i++)
                {
                    EventGraphVariableDescriptor variable =
                        m_AnimationVariables.Variables[i];
                    hashParts.Add(variable.Reference.VariableId);
                    hashParts.Add(variable.Name);
                    hashParts.Add(variable.ValueKind.ToString());
                }
            }
            for (int i = 0; i < m_Facts.Length; i++)
            {
                CharacterPresentationFactDeclaration fact = m_Facts[i];
                hashParts.Add(fact.FactId.Value);
                hashParts.Add(((int)fact.ValueKind).ToString());
            }
            for (int i = 0; i < m_Slots.Length; i++)
            {
                CharacterAnimationInputSlot slot = m_Slots[i];
                hashParts.Add(slot.SlotId.Value);
                hashParts.Add(slot.GroupId.Value);
                hashParts.Add(slot.AnimationChannelId.Value);
                hashParts.Add(((int)slot.SelectionAvailability).ToString());
            }
            hashParts.AddRange(m_WorldCapabilities);
            hashParts.AddRange(m_MovementModeStateIdentities);
            ContractHash = StableHash.Compute(hashParts.ToArray()).Value;
        }

        public IReadOnlyList<CharacterPoseParameterDeclaration> Parameters => m_Parameters;
        public IReadOnlyList<CharacterPresentationFactDeclaration> Facts => m_Facts;
        public IReadOnlyList<CharacterAnimationInputSlot> Slots => m_Slots;
        public IReadOnlyList<string> WorldCapabilities => m_WorldCapabilities;
        public IReadOnlyList<string> MovementModeStateIdentities => m_MovementModeStateIdentities;
        public CharacterAnimationVariableContract AnimationVariables => m_AnimationVariables;
        public string ContractHash { get; }

        public static CharacterAnimationInputContract Create(
            CharacterAnimationPresentationProfile profile)
        {
            if (!profile || !profile.PoseGraph || !profile.RigDefinition)
                throw new InvalidOperationException(
                    "Animation Input Contract requires one Pose Graph and Rig Definition.");
            if (profile.PoseGraph.Graph == null)
                throw new InvalidOperationException("Animation Input Contract requires a Pose Graph root.");
            if (!profile.EventGraph)
                throw new InvalidOperationException(
                    "Animation Input Contract requires a Character Animation Event Graph.");
            CharacterPoseParameterDeclaration[] parameters = BuildParameters(profile);
            CharacterAnimationVariableContract animationVariables =
                new CharacterAnimationVariableContract(
                    profile.EventGraph.BuildVariableContract());
            ValidateAnimationVariables(
                profile,
                parameters,
                animationVariables);
            var facts = new Dictionary<string, CharacterPresentationFactDeclaration>(StringComparer.Ordinal);
            var slots = new List<CharacterAnimationInputSlot>();
            var slotsByKey = new Dictionary<string, CharacterAnimationInputSlot>(StringComparer.Ordinal);
            var worldCapabilities = new HashSet<string>(StringComparer.Ordinal);
            var movementModeStateIdentities = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterPoseCanvasGraph graph in profile.PoseGraph.EnumerateGraphs())
            {
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node?.Payload is CharacterPoseStateMachineNodePayload stateMachine)
                    {
                        foreach (CharacterPoseStateTransition transition in stateMachine.StateMachine.Transitions)
                        {
                            if (transition?.Rule == null)
                                continue;
                            foreach (CharacterPoseTransitionRuleOperation operation in transition.Rule.Operations)
                            {
                                if (operation == null)
                                    continue;
                                if (operation.Kind == PoseTransitionRuleOperationKind.FactInput)
                                    AddFact(facts, operation.FactId);
                                if (operation.Kind == PoseTransitionRuleOperationKind.IdentityLiteral &&
                                    operation.IdentityLiteral.StartsWith(
                                        CharacterPresentationTrajectoryIntent.MovementModeStatePrefix,
                                        StringComparison.Ordinal))
                                {
                                    movementModeStateIdentities.Add(operation.IdentityLiteral);
                                }
                            }
                        }
                    }
                    if (node?.Payload is CharacterMotionMatchingPosePayload motionMatching &&
                        profile.FindPoseResourceBinding(motionMatching.BindingSlot)?.Resource
                            is CharacterMotionMatchingBinding motionMatchingBinding &&
                        motionMatchingBinding.Chooser != null)
                    {
                        foreach (CharacterMotionMatchingDatabaseChooserRule rule in
                                 motionMatchingBinding.Chooser.Rules)
                        {
                            foreach (CharacterMotionMatchingFactPredicate predicate in rule.Predicates)
                                AddFact(facts, predicate.FactId);
                        }
                    }
                    if (node?.Payload is CharacterAnimationSlotPosePayload slotPayload)
                    {
                        CharacterAnimationSlotDefinition slotDefinition =
                            profile.RigDefinition.RequireAnimationSlot(slotPayload.SlotId);
                        var slot = new CharacterAnimationInputSlot(
                            slotPayload.SlotId,
                            slotDefinition.GroupId,
                            slotPayload.AnimationChannelId,
                            slotPayload.SelectionAvailability);
                        string key = string.Concat(
                            slot.SlotId.Value,
                            "/",
                            slot.AnimationChannelId.Value);
                        if (slotsByKey.TryGetValue(key, out CharacterAnimationInputSlot existing) &&
                            !existing.Equals(slot))
                        {
                            throw new InvalidOperationException(
                                $"Animation Input Contract Slot '{key}' has conflicting declarations.");
                        }
                        if (slotsByKey.TryAdd(key, slot))
                            slots.Add(slot);
                        continue;
                    }
                    if (node?.Kind == CharacterPoseNodeKind.FootPlacement)
                        worldCapabilities.Add("world.foot-placement");
                    else if (node?.Kind == CharacterPoseNodeKind.MotionMatchingPose)
                        worldCapabilities.Add("world.motion-matching");
                }
            }
            slots.Sort((left, right) =>
            {
                int result = left.SlotId.CompareTo(right.SlotId);
                return result != 0
                    ? result
                    : left.AnimationChannelId.CompareTo(right.AnimationChannelId);
            });
            CharacterPresentationFactDeclaration[] orderedFacts = facts.Values
                .OrderBy(value => value.FactId.Value, StringComparer.Ordinal)
                .ToArray();
            return new CharacterAnimationInputContract(
                profile,
                parameters,
                orderedFacts,
                slots.ToArray(),
                worldCapabilities.OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                movementModeStateIdentities.OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        static void ValidateAnimationVariables(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterPoseParameterDeclaration> parameters,
            CharacterAnimationVariableContract animationVariables)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                CharacterPoseParameterDeclaration parameter = parameters[i];
                if (parameter.Usage != CharacterPoseParameterUsage.Control)
                    continue;
                if (!animationVariables.TryGet(
                        parameter.ParameterId.Value,
                        out EventGraphVariableDescriptor variable))
                {
                    throw new InvalidOperationException(
                        $"Animation Input Contract parameter '{parameter.ParameterId}' is not published by the Character Animation Event Graph.");
                }
                if (variable.ValueKind != EventGraphKind(parameter.ValueType))
                {
                    throw new InvalidOperationException(
                        $"Animation Input Contract parameter '{parameter.ParameterId}' type does not match the Event Graph variable.");
                }
            }
            foreach (CharacterPoseCanvasGraph graph in profile.PoseGraph.EnumerateGraphs())
            {
                foreach (CharacterPoseCanvasNode node in graph.Nodes)
                {
                    if (node?.Payload is not CharacterPoseStateMachineNodePayload stateMachine)
                        continue;
                    foreach (CharacterPoseStateTransition transition in stateMachine.StateMachine.Transitions)
                    {
                        if (transition?.Rule == null)
                            continue;
                        foreach (CharacterPoseTransitionRuleOperation operation in transition.Rule.Operations)
                        {
                            if (operation?.Kind != PoseTransitionRuleOperationKind.AnimationVariableInput)
                                continue;
                            if (IsPoseOwnedParameter(operation.ParameterId))
                            {
                                throw new InvalidOperationException(
                                    $"Pose Transition Rule animation variable '{operation.ParameterId}' is owned by Pose and cannot come from the Character Animation Event Graph.");
                            }
                            if (!operation.ParameterId.IsValid ||
                                !animationVariables.TryGet(
                                    operation.ParameterId.Value,
                                    out EventGraphVariableDescriptor _))
                            {
                                throw new InvalidOperationException(
                                    $"Pose Transition Rule animation variable '{operation?.ParameterId}' is not published by the Character Animation Event Graph.");
                            }
                        }
                    }
                }
            }
        }

        static EventGraphValueKind EventGraphKind(PoseParameterValueType valueType) =>
            valueType switch
            {
                PoseParameterValueType.Float => EventGraphValueKind.Float32,
                PoseParameterValueType.Int => EventGraphValueKind.Int32,
                PoseParameterValueType.Bool => EventGraphValueKind.Bool,
                _ => throw new InvalidOperationException(
                    $"Pose parameter type '{valueType}' is unsupported by the Character Animation Event Graph.")
            };

        static CharacterPoseParameterDeclaration[] BuildParameters(
            CharacterAnimationPresentationProfile profile)
        {
            var parameters = new List<CharacterPoseParameterDeclaration>();
            var ids = new HashSet<PoseParameterId>();
            var declarations =
                new Dictionary<PoseParameterId, CharacterPoseParameterDeclaration>();
            CharacterAnimationVariableContract animationVariables =
                new CharacterAnimationVariableContract(
                    profile.EventGraph.BuildVariableContract());
            foreach (EventGraphVariableDescriptor variable in animationVariables.Variables)
            {
                if (IsPoseOwnedParameter(
                        new PoseParameterId(variable.Reference.VariableId)))
                    continue;
                CharacterPoseParameterDeclaration declaration =
                    variable.ValueKind switch
                    {
                        EventGraphValueKind.Float32 => new CharacterPoseParameterDeclaration(
                            new PoseParameterId(variable.Reference.VariableId),
                            PoseParameterValueType.Float,
                            variable.InitialValue.Float32Value,
                            displayName: variable.Name),
                        EventGraphValueKind.Int32 => new CharacterPoseParameterDeclaration(
                            new PoseParameterId(variable.Reference.VariableId),
                            PoseParameterValueType.Int,
                            variable.InitialValue.Int32Value,
                            displayName: variable.Name),
                        EventGraphValueKind.Bool => new CharacterPoseParameterDeclaration(
                            new PoseParameterId(variable.Reference.VariableId),
                            PoseParameterValueType.Bool,
                            variable.InitialValue.BoolValue ? 1f : 0f,
                            displayName: variable.Name),
                        _ => null
                    };
                if (declaration == null)
                    continue;
                if (declarations.TryGetValue(
                        declaration.ParameterId,
                        out CharacterPoseParameterDeclaration existing))
                {
                    if (existing.ValueType != declaration.ValueType ||
                        existing.DefaultValue != declaration.DefaultValue)
                    {
                        throw new InvalidOperationException(
                            $"Animation Input Contract contains conflicting Event Graph variable '{declaration.ParameterId}'.");
                    }
                    continue;
                }
                declarations.Add(declaration.ParameterId, declaration);
            }
            foreach (CharacterPoseParameterDeclaration declaration in
                     profile.PoseGraph.EnumerateGraphs()
                         .Where(value => value != null)
                         .SelectMany(value => value.Parameters)
                         .Where(CharacterPoseParameterAccess.IsBlackboardInput)
                         .OrderBy(value => value.ParameterId))
            {
                if (!declarations.TryGetValue(
                        declaration.ParameterId,
                        out CharacterPoseParameterDeclaration existing))
                {
                    declarations.Add(declaration.ParameterId, declaration);
                    continue;
                }
                if (existing.ValueType != declaration.ValueType ||
                    !string.Equals(existing.Unit, declaration.Unit, StringComparison.Ordinal) ||
                    existing.DefaultValue != declaration.DefaultValue)
                {
                    throw new InvalidOperationException(
                        $"Animation Input Contract contains conflicting control declarations for '{declaration.ParameterId}'.");
                }
            }
            foreach (CharacterPoseParameterDeclaration declaration in
                     declarations.Values.OrderBy(value => value.ParameterId))
            {
                if (!ids.Add(declaration.ParameterId))
                    throw new InvalidOperationException(
                        $"Animation Input Contract contains duplicate control parameter '{declaration.ParameterId}'.");
                parameters.Add(declaration);
            }
            foreach (CharacterAnimationPropertyAuthoringBinding binding in profile.AnimationPropertyBindings
                         .Where(value => value != null)
                         .OrderBy(value => value.ParameterId))
            {
                if (!binding.ParameterId.IsValid || !ids.Add(binding.ParameterId))
                    throw new InvalidOperationException(
                        $"Animation Input Contract contains duplicate or invalid property parameter '{binding?.ParameterId}'.");
                parameters.Add(new CharacterPoseParameterDeclaration(
                    binding.ParameterId,
                    PoseParameterValueType.Float,
                    0f,
                    "percent",
                    CharacterPoseParameterUsage.AnimatedProperty));
            }
            if (ids.Add(AnimationPoseParameterIds.FootPlacementWeight))
            {
                parameters.Add(new CharacterPoseParameterDeclaration(
                    AnimationPoseParameterIds.FootPlacementWeight,
                    PoseParameterValueType.Float,
                    1f,
                    "percent",
                    CharacterPoseParameterUsage.AnimatedProperty));
            }
            return parameters.ToArray();
        }

        static bool IsPoseOwnedParameter(PoseParameterId parameterId) =>
            parameterId.Equals(AnimationPoseParameterIds.ActionWeight) ||
            parameterId.Equals(AnimationPoseParameterIds.FootPlacementWeight) ||
            parameterId.Value.StartsWith("animation.blendshape.", StringComparison.Ordinal);

        static void AddFact(
            Dictionary<string, CharacterPresentationFactDeclaration> facts,
            PresentationFactId factId)
        {
            PresentationFactValueKind valueKind =
                CharacterPresentationFactSchema.RequireValueKind(factId);
            string identity = factId.Value;
            if (facts.TryGetValue(identity, out CharacterPresentationFactDeclaration existing) &&
                existing.ValueKind != valueKind)
            {
                throw new InvalidOperationException(
                    $"Animation Input Contract Fact '{identity}' has conflicting value kinds.");
            }
            facts[identity] = new CharacterPresentationFactDeclaration(factId, valueKind);
        }

        public bool Matches(CharacterAnimationPresentationProfile profile) =>
            string.Equals(ContractHash, Create(profile).ContractHash, StringComparison.Ordinal);
    }
}
