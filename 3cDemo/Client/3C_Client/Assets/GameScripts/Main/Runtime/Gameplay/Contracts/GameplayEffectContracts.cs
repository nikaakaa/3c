using System.Collections.Generic;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Contracts
{
    public interface IGameplayTagReader
    {
        bool HasTag(GameplayTagId tagId);
        bool Matches(GameplayTagQuery query);
        bool Matches(GameplayTagQuery query, IReadOnlyList<GameplayTagId> explicitTags);
    }

    public interface IGameplayTagSourceSink
    {
        bool SetSourceTags(GameplayTagSourceHandle source, IReadOnlyList<GameplayTagId> tags);
        bool RemoveSource(GameplayTagSourceHandle source);
    }

    public interface IGameplayAttributeReader
    {
        bool TryGetValue(GameplayAttributeId attributeId, out GameplayAttributeValue value);
    }

    public interface IGameplayEffectCommandSink
    {
        GameplayEffectCanApplyResult CanApply(GameplayEffectApplyRequest request);
        GameplayEffectApplyResult Apply(GameplayEffectApplyRequest request);
        GameplayEffectRemoveResult Remove(GameplayEffectRemoveRequest request);
    }

    public interface IGameplayEffectAuthorityInputSink
    {
        GameplayEffectReconcileResult Reconcile(GameplayEffectAuthorityInput input);
    }
}
