using System.Collections.Generic;
using ThirdPersonGameplay.Tags;

namespace ThirdPersonGameplay.Contracts
{
    public enum GameplayBehaviorKind
    {
        Transaction,
        Stream,
        Effect,
        Event
    }

    public interface IGameplayBehaviorProfile
    {
        string BehaviorId { get; }
        GameplayBehaviorKind BehaviorKind { get; }
        string DisplayName { get; }
        string DebugCategory { get; }
        IReadOnlyList<GameplayTagId> Tags { get; }
    }
}
