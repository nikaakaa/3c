using System;
using UnityEngine;

namespace ThirdPersonAction
{
    [Serializable]
    public struct BodyClaimPolicyDefinition
    {
        [SerializeField] string actionStateId;
        [SerializeField] BodyOccupancyKind kind;
        [SerializeField] CharacterFrameOutputChannel channels;

        public BodyClaimPolicyDefinition(
            string actionStateId,
            BodyOccupancyKind kind,
            CharacterFrameOutputChannel channels)
        {
            this.actionStateId = actionStateId ?? string.Empty;
            this.kind = kind;
            this.channels = channels;
        }

        public ActionStateId ActionState => new ActionStateId(actionStateId);
        public BodyOccupancyKind Kind => kind;
        public CharacterFrameOutputChannel Channels => channels;
        public bool HasDefinition => ActionState.IsValid && Kind != BodyOccupancyKind.None;

        public bool Matches(ActionStateId actionState)
        {
            return HasDefinition && ActionState.Matches(actionState);
        }

        public BodyOccupancyClaim ToClaim(int sourceStep)
        {
            return new BodyOccupancyClaim(
                Kind == BodyOccupancyKind.UpperBody ? CharacterBodyDomain.UpperBody : CharacterBodyDomain.CommittedAction,
                Kind,
                channels,
                sourceStep);
        }
    }

    public readonly struct BodyClaimPolicy
    {
        readonly BodyClaimPolicyDefinition[] definitions;

        public BodyClaimPolicy(BodyClaimPolicyDefinition[] definitions)
        {
            this.definitions = definitions ?? Array.Empty<BodyClaimPolicyDefinition>();
        }

        public bool HasPolicy => definitions != null && definitions.Length > 0;

        public bool TryResolveClaim(
            ActionStateId actionState,
            int sourceStep,
            out BodyOccupancyClaim claim)
        {
            if (!actionState.IsValid || actionState == ActionStateIds.None || definitions == null)
            {
                claim = BodyOccupancyClaim.None(sourceStep);
                return false;
            }

            for (int i = 0; i < definitions.Length; i++)
            {
                BodyClaimPolicyDefinition definition = definitions[i];
                if (!definition.Matches(actionState))
                    continue;

                claim = definition.ToClaim(sourceStep);
                return claim.HasClaim;
            }

            claim = BodyOccupancyClaim.None(sourceStep);
            return false;
        }

        public static BodyClaimPolicy Empty => new BodyClaimPolicy(Array.Empty<BodyClaimPolicyDefinition>());
    }
}
