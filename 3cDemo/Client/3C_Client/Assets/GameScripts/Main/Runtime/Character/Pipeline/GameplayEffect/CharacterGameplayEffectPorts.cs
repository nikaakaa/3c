using System;
using System.Collections.Generic;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonCharacter.Pipeline.GameplayEffect
{
    public sealed class CharacterGameplayEffectQueryPorts
    {
        public CharacterGameplayEffectQueryPorts(
            IGameplayTagReader tagReader,
            IGameplayAttributeReader attributeReader)
        {
            TagReader = tagReader ?? throw new ArgumentNullException(nameof(tagReader));
            AttributeReader = attributeReader ?? throw new ArgumentNullException(nameof(attributeReader));
        }

        public IGameplayTagReader TagReader { get; }
        public IGameplayAttributeReader AttributeReader { get; }
    }

    public readonly struct CharacterGameplayEffectSelfApplyRequest
    {
        public CharacterGameplayEffectSelfApplyRequest(
            GameplayEffectId effectId,
            uint definitionRevision,
            ulong actionInstanceId,
            ulong predictionKey,
            ulong gameplayResultId,
            ulong sourceLogicTick,
            GameplayEffectApplicationMode applicationMode,
            IReadOnlyList<GameplaySetByCallerValue> setByCallerValues)
        {
            EffectId = effectId;
            DefinitionRevision = definitionRevision;
            ActionInstanceId = actionInstanceId;
            PredictionKey = predictionKey;
            GameplayResultId = gameplayResultId;
            SourceLogicTick = sourceLogicTick;
            ApplicationMode = applicationMode;
            SetByCallerValues = setByCallerValues ?? Array.Empty<GameplaySetByCallerValue>();
        }

        public GameplayEffectId EffectId { get; }
        public uint DefinitionRevision { get; }
        public ulong ActionInstanceId { get; }
        public ulong PredictionKey { get; }
        public ulong GameplayResultId { get; }
        public ulong SourceLogicTick { get; }
        public GameplayEffectApplicationMode ApplicationMode { get; }
        public IReadOnlyList<GameplaySetByCallerValue> SetByCallerValues { get; }
    }

    public readonly struct CharacterGameplayEffectSelfRemoveRequest
    {
        public CharacterGameplayEffectSelfRemoveRequest(
            GameplayEffectRemoveSelector selector,
            GameplayEffectHandle handle = default,
            GameplayEffectId effectId = default,
            GameplayTagQuery effectTagQuery = null)
        {
            Selector = selector;
            Handle = handle;
            EffectId = effectId;
            EffectTagQuery = effectTagQuery;
        }

        public GameplayEffectRemoveSelector Selector { get; }
        public GameplayEffectHandle Handle { get; }
        public GameplayEffectId EffectId { get; }
        public GameplayTagQuery EffectTagQuery { get; }
    }

    public sealed class CharacterGameplayEffectCommandPorts
    {
        readonly Func<CharacterGameplayEffectSelfApplyRequest, GameplayEffectApplyResult> m_ApplySelf;
        readonly Func<CharacterGameplayEffectSelfRemoveRequest, GameplayEffectRemoveResult> m_RemoveSelf;

        internal CharacterGameplayEffectCommandPorts(
            Func<CharacterGameplayEffectSelfApplyRequest, GameplayEffectApplyResult> applySelf,
            Func<CharacterGameplayEffectSelfRemoveRequest, GameplayEffectRemoveResult> removeSelf)
        {
            m_ApplySelf = applySelf ?? throw new ArgumentNullException(nameof(applySelf));
            m_RemoveSelf = removeSelf ?? throw new ArgumentNullException(nameof(removeSelf));
        }

        public GameplayEffectApplyResult ApplySelf(CharacterGameplayEffectSelfApplyRequest request) => m_ApplySelf(request);
        public GameplayEffectRemoveResult RemoveSelf(CharacterGameplayEffectSelfRemoveRequest request) => m_RemoveSelf(request);
    }
}
