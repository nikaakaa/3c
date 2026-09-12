using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Graph
{
    [Serializable]
    [NodeName("Has Gameplay Tag")]
    [NodePath("Base/Value/Gameplay Effect/Has Tag")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class HasGameplayTagNode : ValueNode, IGameplayTagAuthoring
    {
        [SerializeField, ShowInPanel("Tag")]
        GameplayTagId m_Tag;

        [SerializeField, PropertyPort(PortDirection.Output, "Has Tag"), ReadOnly]
        BoolPropertyPort m_Result = new BoolPropertyPort();

        public GameplayTagId Tag => m_Tag;

#if UNITY_EDITOR
        public void ConfigureAuthoring(GameplayTagId tag)
        {
            m_Tag = GameplayAuthoringRules.RequireTag(tag);
            OnNodeChangedCallback();
        }
#endif

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Match Gameplay Tag Query")]
    [NodePath("Base/Value/Gameplay Effect/Match Tag Query")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class MatchGameplayTagQueryNode : ValueNode, IGameplayTagQueryAuthoring
    {
        [SerializeField]
        GameplayTagQuery m_Query = new GameplayTagQuery();

        [SerializeField, PropertyPort(PortDirection.Output, "Matches"), ReadOnly]
        BoolPropertyPort m_Result = new BoolPropertyPort();

        public GameplayTagQuery Query => m_Query;

#if UNITY_EDITOR
        public void ConfigureAuthoring(GameplayTagQuery query)
        {
            m_Query = GameplayAuthoringRules.RequireTagQuery(query);
            OnNodeChangedCallback();
        }
#endif

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Read Gameplay Attribute")]
    [NodePath("Base/Value/Gameplay Effect/Read Attribute")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ReadGameplayAttributeNode : ValueNode, IGameplayAttributeAuthoring
    {
        [SerializeField, ShowInPanel("Attribute")]
        GameplayAttributeId m_Attribute;

        [SerializeField, PropertyPort(PortDirection.Output, "Valid"), ReadOnly]
        BoolPropertyPort m_Valid = new BoolPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Base Value"), ReadOnly]
        FloatPropertyPort m_BaseValue = new FloatPropertyPort();

        [SerializeField, PropertyPort(PortDirection.Output, "Current Value"), ReadOnly]
        FloatPropertyPort m_CurrentValue = new FloatPropertyPort();

        public GameplayAttributeId Attribute => m_Attribute;

#if UNITY_EDITOR
        public void ConfigureAuthoring(GameplayAttributeId attribute)
        {
            m_Attribute = GameplayAuthoringRules.RequireAttribute(attribute);
            OnNodeChangedCallback();
        }
#endif

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Apply Gameplay Effect")]
    [NodePath("Base/Action/Gameplay Effect/Apply")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class ApplyGameplayEffectNode : ActionNode, IGameplayEffectApplicationAuthoring
    {
        [SerializeField, ShowInPanel("Effect")]
        GameplayEffectDefinition m_Effect;

        [SerializeField, ShowInPanel("Action Context")]
        ActionContextSlot m_ActionContext;

        [SerializeField, ShowInPanel("Predicted")]
        bool m_Predicted;

        [SerializeField, ShowInPanel("Set By Caller")]
        GameplaySetByCallerValue[] m_SetByCallerValues = Array.Empty<GameplaySetByCallerValue>();

        [SerializeField, PropertyPort(PortDirection.Output, "Applied"), ReadOnly]
        BoolPropertyPort m_Applied = new BoolPropertyPort();

        public GameplayEffectDefinition Effect => m_Effect;
        public ActionContextSlot ActionContext => m_ActionContext;
        public bool Predicted => m_Predicted;
        public IReadOnlyList<GameplaySetByCallerValue> SetByCallerValues => m_SetByCallerValues ?? Array.Empty<GameplaySetByCallerValue>();

        public override State ReturnState => m_Applied.Value ? State.Success : State.Failure;

#if UNITY_EDITOR
        public void ConfigureAuthoring(
            GameplayEffectDefinition effect,
            ActionContextSlot actionContext,
            bool predicted)
        {
            m_Effect = GameplayAuthoringRules.RequireEffect(effect);
            m_ActionContext = actionContext;
            m_Predicted = predicted;
            OnNodeChangedCallback();
        }
#endif

        protected override void DoAction()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Remove Gameplay Effect")]
    [NodePath("Base/Action/Gameplay Effect/Remove")]
    [NodeAuthoringCapability(NodeAuthoringCapability.CharacterExecution)]
    public sealed class RemoveGameplayEffectNode : ActionNode, IGameplayEffectRemovalAuthoring
    {
        [SerializeField, ShowInPanel("Selector")]
        GameplayEffectRemoveSelector m_Selector = GameplayEffectRemoveSelector.EffectId;

        [SerializeField, ShowInPanel("Handle")]
        ulong m_Handle;

        [SerializeField, ShowInPanel("Effect")]
        GameplayEffectDefinition m_Effect;

        [SerializeField]
        GameplayTagQuery m_EffectTagQuery = new GameplayTagQuery();

        [SerializeField, PropertyPort(PortDirection.Output, "Removed"), ReadOnly]
        BoolPropertyPort m_Removed = new BoolPropertyPort();

        public GameplayEffectRemoveSelector Selector => m_Selector;
        public ulong Handle => m_Handle;
        public GameplayEffectDefinition Effect => m_Effect;
        public GameplayTagQuery EffectTagQuery => m_EffectTagQuery;

        public override State ReturnState => m_Removed.Value ? State.Success : State.Failure;

#if UNITY_EDITOR
        public void ConfigureAuthoring(
            GameplayEffectRemoveSelector selector,
            ulong handle,
            GameplayEffectDefinition effect,
            GameplayTagQuery effectTagQuery)
        {
            GameplayAuthoringRules.ValidateEffectRemoval(selector, effect, effectTagQuery);
            m_Selector = selector;
            m_Handle = handle;
            m_Effect = effect;
            m_EffectTagQuery = effectTagQuery ?? new GameplayTagQuery();
            OnNodeChangedCallback();
        }
#endif

        protected override void DoAction()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }
}
