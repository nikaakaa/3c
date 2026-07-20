using System;
using System.Collections.Generic;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.GameplayEffect;
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
    public sealed class HasGameplayTagNode : ValueNode
    {
        [SerializeField, ShowInPanel("Tag")]
        GameplayTagId m_Tag;

        [SerializeField, PropertyPort(PortDirection.Output, "Has Tag"), ReadOnly]
        BoolPropertyPort m_Result = new BoolPropertyPort();

        public GameplayTagId Tag => m_Tag;

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Match Gameplay Tag Query")]
    [NodePath("Base/Value/Gameplay Effect/Match Tag Query")]
    public sealed class MatchGameplayTagQueryNode : ValueNode
    {
        [SerializeField]
        GameplayTagQuery m_Query = new GameplayTagQuery();

        [SerializeField, PropertyPort(PortDirection.Output, "Matches"), ReadOnly]
        BoolPropertyPort m_Result = new BoolPropertyPort();

        public GameplayTagQuery Query => m_Query;

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Read Gameplay Attribute")]
    [NodePath("Base/Value/Gameplay Effect/Read Attribute")]
    public sealed class ReadGameplayAttributeNode : ValueNode
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

        protected override void OutputValue()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Apply Gameplay Effect")]
    [NodePath("Base/Action/Gameplay Effect/Apply")]
    public sealed class ApplyGameplayEffectNode : ActionNode
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

        protected override void DoAction()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }

    [Serializable]
    [NodeName("Remove Gameplay Effect")]
    [NodePath("Base/Action/Gameplay Effect/Remove")]
    public sealed class RemoveGameplayEffectNode : ActionNode
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

        protected override void DoAction()
        {
            throw new InvalidOperationException($"{GetType().Name} must execute through CharacterSimulationProgram.");
        }
    }
}
