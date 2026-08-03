using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Animation.TransitionRouting;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public readonly struct PoseStateMachineId : IEquatable<PoseStateMachineId>, IComparable<PoseStateMachineId>
    {
        public PoseStateMachineId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseStateMachineId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseStateMachineId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseStateMachineId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct PoseStateEntryId : IEquatable<PoseStateEntryId>, IComparable<PoseStateEntryId>
    {
        public PoseStateEntryId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseStateEntryId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseStateEntryId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseStateEntryId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct PoseStateId : IEquatable<PoseStateId>, IComparable<PoseStateId>
    {
        public PoseStateId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseStateId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseStateId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseStateId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PoseStateId left, PoseStateId right) => left.Equals(right);
        public static bool operator !=(PoseStateId left, PoseStateId right) => !left.Equals(right);
    }

    public readonly struct PoseStateTransitionId : IEquatable<PoseStateTransitionId>, IComparable<PoseStateTransitionId>
    {
        public PoseStateTransitionId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseStateTransitionId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseStateTransitionId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseStateTransitionId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct PoseStateAliasId : IEquatable<PoseStateAliasId>, IComparable<PoseStateAliasId>
    {
        public PoseStateAliasId(string value) { Value = PoseIdentity.Require(value, nameof(value)); }
        public string Value { get; }
        public bool IsValid => !string.IsNullOrEmpty(Value);
        public int CompareTo(PoseStateAliasId other) => string.CompareOrdinal(Value, other.Value);
        public bool Equals(PoseStateAliasId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoseStateAliasId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value ?? string.Empty;
        public static bool operator ==(PoseStateAliasId left, PoseStateAliasId right) => left.Equals(right);
        public static bool operator !=(PoseStateAliasId left, PoseStateAliasId right) => !left.Equals(right);
    }

    public enum PoseStateTransitionSourceKind : byte
    {
        State = 1,
        Alias = 2
    }

    [Serializable]
    public sealed class CharacterPoseStateTransitionSource
    {
        [SerializeField] PoseStateTransitionSourceKind m_Kind;
        [SerializeField] string m_StateId = string.Empty;
        [SerializeField] string m_AliasId = string.Empty;

        public PoseStateTransitionSourceKind Kind => m_Kind;
        public PoseStateId StateId => string.IsNullOrWhiteSpace(m_StateId) ? default : new PoseStateId(m_StateId);
        public PoseStateAliasId AliasId => string.IsNullOrWhiteSpace(m_AliasId) ? default : new PoseStateAliasId(m_AliasId);

        public CharacterPoseStateTransitionSource() { }

        public static CharacterPoseStateTransitionSource FromState(PoseStateId stateId)
        {
            if (!stateId.IsValid)
                throw new ArgumentException("Pose State source identity is invalid.", nameof(stateId));
            return new CharacterPoseStateTransitionSource
            {
                m_Kind = PoseStateTransitionSourceKind.State,
                m_StateId = stateId.Value
            };
        }

        public static CharacterPoseStateTransitionSource FromAlias(PoseStateAliasId aliasId)
        {
            if (!aliasId.IsValid)
                throw new ArgumentException("Pose State Alias source identity is invalid.", nameof(aliasId));
            return new CharacterPoseStateTransitionSource
            {
                m_Kind = PoseStateTransitionSourceKind.Alias,
                m_AliasId = aliasId.Value
            };
        }

        internal bool IsValid =>
            Kind == PoseStateTransitionSourceKind.State && StateId.IsValid && !AliasId.IsValid ||
            Kind == PoseStateTransitionSourceKind.Alias && AliasId.IsValid && !StateId.IsValid;
    }

    [Serializable]
    public sealed class CharacterPoseStateEntry
    {
        [SerializeField] string m_EntryId = string.Empty;
        [SerializeField] string m_TargetStateId = string.Empty;

        public PoseStateEntryId EntryId => string.IsNullOrWhiteSpace(m_EntryId)
            ? default
            : new PoseStateEntryId(m_EntryId);
        public PoseStateId TargetStateId => string.IsNullOrWhiteSpace(m_TargetStateId)
            ? default
            : new PoseStateId(m_TargetStateId);

        public CharacterPoseStateEntry() { }

        public CharacterPoseStateEntry(PoseStateEntryId entryId, PoseStateId targetStateId)
        {
            if (!entryId.IsValid || !targetStateId.IsValid)
                throw new ArgumentException("Pose State Entry is invalid.");
            m_EntryId = entryId.Value;
            m_TargetStateId = targetStateId.Value;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateDefinition
    {
        [SerializeField] string m_StateId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] string m_PoseGraphId = string.Empty;
        [SerializeField] string m_OutputPoseNodeId = string.Empty;
        [SerializeField] bool m_AlwaysResetOnEntry = true;

        public PoseStateId StateId => string.IsNullOrWhiteSpace(m_StateId)
            ? default
            : new PoseStateId(m_StateId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public PoseGraphId PoseGraphId => string.IsNullOrWhiteSpace(m_PoseGraphId)
            ? default
            : new PoseGraphId(m_PoseGraphId);
        public PoseNodeId OutputPoseNodeId => string.IsNullOrWhiteSpace(m_OutputPoseNodeId)
            ? default
            : new PoseNodeId(m_OutputPoseNodeId);
        public bool AlwaysResetOnEntry => m_AlwaysResetOnEntry;

        public CharacterPoseStateDefinition() { }

        public CharacterPoseStateDefinition(
            PoseStateId stateId,
            string displayName,
            PoseGraphId poseGraphId,
            PoseNodeId outputPoseNodeId,
            bool alwaysResetOnEntry)
        {
            if (!stateId.IsValid || !poseGraphId.IsValid || !outputPoseNodeId.IsValid)
                throw new ArgumentException("Pose State definition is invalid.");
            m_StateId = stateId.Value;
            m_DisplayName = displayName ?? string.Empty;
            m_PoseGraphId = poseGraphId.Value;
            m_OutputPoseNodeId = outputPoseNodeId.Value;
            m_AlwaysResetOnEntry = alwaysResetOnEntry;
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateAlias
    {
        [SerializeField] string m_AliasId = string.Empty;
        [SerializeField] string m_DisplayName = string.Empty;
        [SerializeField] CharacterPoseStateTransitionSource[] m_Sources =
            Array.Empty<CharacterPoseStateTransitionSource>();

        public PoseStateAliasId AliasId => string.IsNullOrWhiteSpace(m_AliasId)
            ? default
            : new PoseStateAliasId(m_AliasId);
        public string DisplayName => m_DisplayName ?? string.Empty;
        public IReadOnlyList<CharacterPoseStateTransitionSource> Sources =>
            m_Sources ?? Array.Empty<CharacterPoseStateTransitionSource>();

        public CharacterPoseStateAlias() { }

        public CharacterPoseStateAlias(
            PoseStateAliasId aliasId,
            string displayName,
            CharacterPoseStateTransitionSource[] sources)
        {
            if (!aliasId.IsValid)
                throw new ArgumentException("Pose State Alias identity is invalid.", nameof(aliasId));
            m_AliasId = aliasId.Value;
            m_DisplayName = displayName ?? string.Empty;
            m_Sources = sources ?? Array.Empty<CharacterPoseStateTransitionSource>();
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateTransition
    {
        [SerializeField] string m_TransitionId = string.Empty;
        [SerializeField] CharacterPoseStateTransitionSource m_Source;
        [SerializeField] string m_TargetStateId = string.Empty;
        [SerializeField] int m_Priority;
        [SerializeField] CharacterPoseTransitionRuleGraph m_Rule;
        [SerializeField] AnimationTransitionBlendLogic m_BlendLogic = AnimationTransitionBlendLogic.StandardBlend;
        [SerializeField] float m_DurationSeconds = 0.1f;
        [SerializeField] CharacterAnimationBlendMode m_BlendMode = CharacterAnimationBlendMode.Linear;
        [SerializeField] CharacterAnimationBlendCurveAsset m_CustomBlendCurve;
        [SerializeField] CharacterAnimationBlendProfile m_BlendProfile;

        public PoseStateTransitionId TransitionId => string.IsNullOrWhiteSpace(m_TransitionId)
            ? default
            : new PoseStateTransitionId(m_TransitionId);
        public CharacterPoseStateTransitionSource Source => m_Source;
        public PoseStateId TargetStateId => string.IsNullOrWhiteSpace(m_TargetStateId)
            ? default
            : new PoseStateId(m_TargetStateId);
        public int Priority => m_Priority;
        public CharacterPoseTransitionRuleGraph Rule => m_Rule;
        public AnimationTransitionBlendLogic BlendLogic => m_BlendLogic;
        public float DurationSeconds => m_DurationSeconds;
        public CharacterAnimationBlendMode BlendMode => m_BlendMode;
        public CharacterAnimationBlendCurveAsset CustomBlendCurve => m_CustomBlendCurve;
        public CharacterAnimationBlendProfile BlendProfile => m_BlendProfile;

        public CharacterPoseStateTransition() { }

        public CharacterPoseStateTransition(
            PoseStateTransitionId transitionId,
            CharacterPoseStateTransitionSource source,
            PoseStateId targetStateId,
            int priority,
            CharacterPoseTransitionRuleGraph rule,
            AnimationTransitionBlendLogic blendLogic,
            float durationSeconds,
            CharacterAnimationBlendMode blendMode,
            CharacterAnimationBlendCurveAsset customBlendCurve,
            CharacterAnimationBlendProfile blendProfile)
        {
            if (!transitionId.IsValid || source == null || !targetStateId.IsValid || priority < 0 ||
                rule == null || !Enum.IsDefined(typeof(AnimationTransitionBlendLogic), blendLogic) ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f)
            {
                throw new ArgumentException("Pose State Transition is invalid.");
            }
            RequireBlendSettings(blendLogic, durationSeconds, blendMode, customBlendCurve, blendProfile);
            m_TransitionId = transitionId.Value;
            m_Source = source;
            m_TargetStateId = targetStateId.Value;
            m_Priority = priority;
            m_Rule = rule;
            m_BlendLogic = blendLogic;
            m_DurationSeconds = durationSeconds;
            m_BlendMode = blendMode;
            m_CustomBlendCurve = customBlendCurve;
            m_BlendProfile = blendProfile;
        }

        public static void RequireBlendSettings(
            AnimationTransitionBlendLogic blendLogic,
            float durationSeconds,
            CharacterAnimationBlendMode blendMode,
            CharacterAnimationBlendCurveAsset customBlendCurve,
            CharacterAnimationBlendProfile blendProfile)
        {
            if (!Enum.IsDefined(typeof(AnimationTransitionBlendLogic), blendLogic) ||
                !float.IsFinite(durationSeconds) || durationSeconds < 0f ||
                blendLogic == AnimationTransitionBlendLogic.Inertialization && durationSeconds <= 0f)
            {
                throw new InvalidOperationException("Pose State Transition blend duration is invalid.");
            }
            CharacterAnimationBlendCurveCompiler.RequireConfiguration(blendMode, customBlendCurve);
            bool hardCut = blendLogic == AnimationTransitionBlendLogic.StandardBlend && durationSeconds == 0f;
            if (!hardCut && !blendProfile)
                throw new InvalidOperationException("Pose State Transition requires a Blend Profile.");
            if (blendProfile &&
                (!string.Equals(blendProfile.Schema, CharacterAnimationBlendProfile.SchemaVersion, StringComparison.Ordinal) ||
                 string.IsNullOrWhiteSpace(blendProfile.ProfileId)))
            {
                throw new InvalidOperationException("Pose State Transition Blend Profile is invalid.");
            }
        }
    }

    [Serializable]
    public sealed class CharacterPoseStateMachineDefinition
    {
        [SerializeField] string m_StateMachineId = string.Empty;
        [SerializeField] string m_ContentRevision = string.Empty;
        [SerializeField] CharacterPoseStateEntry m_Entry;
        [SerializeField] CharacterPoseStateDefinition[] m_States = Array.Empty<CharacterPoseStateDefinition>();
        [SerializeField] CharacterPoseStateTransition[] m_Transitions = Array.Empty<CharacterPoseStateTransition>();
        [SerializeField] CharacterPoseStateAlias[] m_Aliases = Array.Empty<CharacterPoseStateAlias>();
        [SerializeField, Min(1)] int m_MaxTransitionsPerFrame = 1;

        public PoseStateMachineId StateMachineId => string.IsNullOrWhiteSpace(m_StateMachineId)
            ? default
            : new PoseStateMachineId(m_StateMachineId);
        public string ContentRevision => m_ContentRevision ?? string.Empty;
        public CharacterPoseStateEntry Entry => m_Entry;
        public IReadOnlyList<CharacterPoseStateDefinition> States =>
            m_States ?? Array.Empty<CharacterPoseStateDefinition>();
        public IReadOnlyList<CharacterPoseStateTransition> Transitions =>
            m_Transitions ?? Array.Empty<CharacterPoseStateTransition>();
        public IReadOnlyList<CharacterPoseStateAlias> Aliases =>
            m_Aliases ?? Array.Empty<CharacterPoseStateAlias>();
        public int MaxTransitionsPerFrame => m_MaxTransitionsPerFrame;

        public CharacterPoseStateMachineDefinition()
        {
            RegenerateIdentity();
        }

        public CharacterPoseStateMachineDefinition(
            PoseStateMachineId stateMachineId,
            string contentRevision,
            CharacterPoseStateEntry entry,
            CharacterPoseStateDefinition[] states,
            CharacterPoseStateTransition[] transitions,
            CharacterPoseStateAlias[] aliases,
            int maxTransitionsPerFrame)
        {
            if (!stateMachineId.IsValid ||
                string.IsNullOrWhiteSpace(contentRevision) ||
                entry == null ||
                maxTransitionsPerFrame <= 0)
            {
                throw new ArgumentException(
                    "Pose StateMachine authoring data is invalid.");
            }
            m_StateMachineId = stateMachineId.Value;
            m_ContentRevision = contentRevision.Trim();
            m_Entry = entry;
            m_States = states ?? Array.Empty<CharacterPoseStateDefinition>();
            m_Transitions =
                transitions ?? Array.Empty<CharacterPoseStateTransition>();
            m_Aliases = aliases ?? Array.Empty<CharacterPoseStateAlias>();
            m_MaxTransitionsPerFrame = maxTransitionsPerFrame;
        }

        public void Configure(
            CharacterPoseStateEntry entry,
            CharacterPoseStateDefinition[] states,
            CharacterPoseStateTransition[] transitions,
            CharacterPoseStateAlias[] aliases,
            int maxTransitionsPerFrame)
        {
            if (maxTransitionsPerFrame <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxTransitionsPerFrame));
            m_Entry = entry;
            m_States = states ?? Array.Empty<CharacterPoseStateDefinition>();
            m_Transitions = transitions ?? Array.Empty<CharacterPoseStateTransition>();
            m_Aliases = aliases ?? Array.Empty<CharacterPoseStateAlias>();
            m_MaxTransitionsPerFrame = maxTransitionsPerFrame;
            Touch();
        }

        public void RegenerateIdentity()
        {
            m_StateMachineId = Guid.NewGuid().ToString("N");
            Touch();
        }

        public void Touch() => m_ContentRevision = Guid.NewGuid().ToString("N");
    }

    public static class CharacterPoseStateMachineAuthoringValidator
    {
        public static void RequireValid(
            CharacterPoseStateMachineDefinition definition,
            Func<PoseGraphId, CharacterTypedPoseGraph> graphResolver)
        {
            if (definition == null || !definition.StateMachineId.IsValid ||
                string.IsNullOrWhiteSpace(definition.ContentRevision) ||
                definition.Entry == null || !definition.Entry.EntryId.IsValid ||
                !definition.Entry.TargetStateId.IsValid || definition.MaxTransitionsPerFrame <= 0 ||
                graphResolver == null)
            {
                throw new InvalidOperationException("Pose StateMachine authoring identity or Entry is invalid.");
            }

            var states = new HashSet<PoseStateId>();
            for (int i = 0; i < definition.States.Count; i++)
            {
                CharacterPoseStateDefinition state = definition.States[i] ??
                    throw new InvalidOperationException($"Pose StateMachine State #{i} is missing.");
                if (!state.StateId.IsValid || !state.PoseGraphId.IsValid || !states.Add(state.StateId))
                    throw new InvalidOperationException($"Pose StateMachine has a missing or duplicate State identity.");
                RequireStatePose(state, graphResolver(state.PoseGraphId));
            }
            if (!states.Contains(definition.Entry.TargetStateId))
                throw new InvalidOperationException("Pose StateMachine Entry target does not exist.");

            var aliases = new HashSet<PoseStateAliasId>();
            for (int i = 0; i < definition.Aliases.Count; i++)
            {
                CharacterPoseStateAlias alias = definition.Aliases[i] ??
                    throw new InvalidOperationException($"Pose StateMachine Alias #{i} is missing.");
                if (!alias.AliasId.IsValid || !aliases.Add(alias.AliasId) || alias.Sources.Count == 0)
                    throw new InvalidOperationException("Pose StateMachine has an invalid State Alias.");
            }
            for (int i = 0; i < definition.Aliases.Count; i++)
            {
                CharacterPoseStateAlias alias = definition.Aliases[i];
                for (int sourceIndex = 0; sourceIndex < alias.Sources.Count; sourceIndex++)
                {
                    CharacterPoseStateTransitionSource source = alias.Sources[sourceIndex];
                    RequireSource(source, states, aliases);
                    if (source.Kind == PoseStateTransitionSourceKind.Alias && source.AliasId == alias.AliasId)
                        throw new InvalidOperationException($"Pose State Alias '{alias.AliasId}' directly references itself.");
                }
            }

            var transitions = new HashSet<PoseStateTransitionId>();
            for (int i = 0; i < definition.Transitions.Count; i++)
            {
                CharacterPoseStateTransition transition = definition.Transitions[i] ??
                    throw new InvalidOperationException($"Pose StateMachine Transition #{i} is missing.");
                if (!transition.TransitionId.IsValid || !transitions.Add(transition.TransitionId) ||
                    transition.Priority < 0 || transition.Rule == null ||
                    !transition.Rule.GraphId.IsValid || !transition.Rule.OutputOperationId.IsValid ||
                    !states.Contains(transition.TargetStateId) ||
                    !Enum.IsDefined(typeof(AnimationTransitionBlendLogic), transition.BlendLogic) ||
                    !float.IsFinite(transition.DurationSeconds) || transition.DurationSeconds < 0f)
                {
                    throw new InvalidOperationException("Pose StateMachine has an invalid Transition.");
                }
                CharacterPoseStateTransition.RequireBlendSettings(
                    transition.BlendLogic,
                    transition.DurationSeconds,
                    transition.BlendMode,
                    transition.CustomBlendCurve,
                    transition.BlendProfile);
                RequireSource(transition.Source, states, aliases);
            }
        }

        static void RequireStatePose(
            CharacterPoseStateDefinition state,
            CharacterTypedPoseGraph graph)
        {
            if (graph == null || graph.GraphId != state.PoseGraphId ||
                string.IsNullOrWhiteSpace(graph.ContentRevision) || !state.OutputPoseNodeId.IsValid)
            {
                throw new InvalidOperationException(
                    $"Pose State '{state.StateId}' Pose Graph '{state.PoseGraphId}' is invalid.");
            }
            int outputCount = 0;
            bool outputMatched = false;
            var nodeIds = new HashSet<PoseNodeId>();
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i] ??
                    throw new InvalidOperationException($"Pose State '{state.StateId}' Node #{i} is missing.");
                if (!node.NodeId.IsValid || !nodeIds.Add(node.NodeId))
                    throw new InvalidOperationException($"Pose State '{state.StateId}' has duplicate Node identity.");
                if (node.Kind == CharacterPoseNodeKind.PoseStateMachine)
                    throw new InvalidOperationException($"Pose State '{state.StateId}' cannot nest a Pose StateMachine.");
                if (node.Kind != CharacterPoseNodeKind.OutputPose)
                    continue;
                outputCount++;
                outputMatched |= node.NodeId == state.OutputPoseNodeId;
            }
            if (outputCount != 1 || !outputMatched)
                throw new InvalidOperationException($"Pose State '{state.StateId}' requires one explicit Pose output.");
        }

        static void RequireSource(
            CharacterPoseStateTransitionSource source,
            HashSet<PoseStateId> states,
            HashSet<PoseStateAliasId> aliases)
        {
            if (source == null || !source.IsValid ||
                source.Kind == PoseStateTransitionSourceKind.State && !states.Contains(source.StateId) ||
                source.Kind == PoseStateTransitionSourceKind.Alias && !aliases.Contains(source.AliasId))
            {
                throw new InvalidOperationException("Pose State transition source is invalid.");
            }
        }
    }
}
