using BTSMTL.EventGraphs;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [CreateAssetMenu(
        fileName = "CharacterAnimationEventGraph",
        menuName = "3C/Character/Animation Event Graph")]
    public sealed class CharacterAnimationEventGraph : HostEventGraph
    {
        public const string ContractId = "character-animation-event-graph";
        public const string ContractRevision = "character-animation-event-graph/v2";
        public const string UpdateEventId = "animation.presentation.update";
    }
}
