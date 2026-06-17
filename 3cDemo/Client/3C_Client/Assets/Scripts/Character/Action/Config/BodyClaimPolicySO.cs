using System;
using UnityEngine;

namespace ThirdPersonAction
{
    [CreateAssetMenu(fileName = "BodyClaimPolicy", menuName = "3C/Action/BodyClaimPolicy")]
    public sealed class BodyClaimPolicySO : ScriptableObject
    {
        [SerializeField] BodyClaimPolicyDefinition[] definitions =
        {
            new BodyClaimPolicyDefinition(
                "Action.Dodge",
                BodyOccupancyKind.FullBody,
                CharacterFrameOutputChannel.Motion | CharacterFrameOutputChannel.Animation)
        };

        public BodyClaimPolicy ToPolicy()
        {
            return new BodyClaimPolicy(definitions ?? Array.Empty<BodyClaimPolicyDefinition>());
        }
    }
}
