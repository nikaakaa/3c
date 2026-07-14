using System;
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

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Result.Value = TryGetGraphContext(out CharacterGraphContext context) &&
                             context.GameplayEffectQueries.TagReader.HasTag(m_Tag);
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            return Owner != null && Owner.TryGetUser(out context) && context != null;
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

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Result.Value = TryGetGraphContext(out CharacterGraphContext context) &&
                             context.GameplayEffectQueries.TagReader.Matches(m_Query);
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            return Owner != null && Owner.TryGetUser(out context) && context != null;
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

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Valid.Value = false;
            m_BaseValue.Value = 0f;
            m_CurrentValue.Value = 0f;
            if (!TryGetGraphContext(out CharacterGraphContext context) ||
                !context.GameplayEffectQueries.AttributeReader.TryGetValue(m_Attribute, out GameplayAttributeValue value))
                return;
            m_Valid.Value = true;
            m_BaseValue.Value = value.BaseValue;
            m_CurrentValue.Value = value.CurrentValue;
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            return Owner != null && Owner.TryGetUser(out context) && context != null;
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

        public override State ReturnState => m_Applied.Value ? State.Success : State.Failure;

        protected override void DoAction()
        {
            m_Applied.Value = false;
            if (!m_Effect || !TryGetGraphContext(out CharacterGraphContext context))
                return;
            ulong actionInstanceId = 0;
            ulong predictionKey = 0;
            if (context.TryGetActionContextHandle(m_ActionContext, out ActionInstanceHandle handle))
            {
                actionInstanceId = handle.ActionInstanceId;
                predictionKey = handle.PredictionKey;
            }
            if (m_Predicted && (actionInstanceId == 0 || predictionKey == 0))
                return;
            GameplayEffectApplyResult result = context.GameplayEffectCommands.ApplySelf(
                new CharacterGameplayEffectSelfApplyRequest(
                m_Effect.EffectId,
                m_Effect.DefinitionRevision,
                actionInstanceId,
                predictionKey,
                0,
                context.LocalLogicTick,
                m_Predicted ? GameplayEffectApplicationMode.Predicted : GameplayEffectApplicationMode.Confirmed,
                m_SetByCallerValues));
            m_Applied.Value = result.Succeeded;
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            return Owner != null && Owner.TryGetUser(out context) && context != null;
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

        public override State ReturnState => m_Removed.Value ? State.Success : State.Failure;

        protected override void DoAction()
        {
            m_Removed.Value = false;
            if (!TryGetGraphContext(out CharacterGraphContext context))
                return;
            CharacterGameplayEffectSelfRemoveRequest request;
            switch (m_Selector)
            {
                case GameplayEffectRemoveSelector.Handle:
                    request = new CharacterGameplayEffectSelfRemoveRequest(m_Selector, handle: new GameplayEffectHandle(m_Handle));
                    break;
                case GameplayEffectRemoveSelector.EffectId:
                    if (!m_Effect)
                        return;
                    request = new CharacterGameplayEffectSelfRemoveRequest(m_Selector, effectId: m_Effect.EffectId);
                    break;
                case GameplayEffectRemoveSelector.SourceActorId:
                    request = new CharacterGameplayEffectSelfRemoveRequest(m_Selector);
                    break;
                case GameplayEffectRemoveSelector.EffectTagQuery:
                    request = new CharacterGameplayEffectSelfRemoveRequest(m_Selector, effectTagQuery: m_EffectTagQuery);
                    break;
                default:
                    return;
            }
            m_Removed.Value = context.GameplayEffectCommands.RemoveSelf(request).RemovedAny;
        }

        bool TryGetGraphContext(out CharacterGraphContext context)
        {
            context = null;
            return Owner != null && Owner.TryGetUser(out context) && context != null;
        }
    }
}
