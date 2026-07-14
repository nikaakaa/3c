using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public enum AnimationBlendMode
    {
        Override,
        Additive
    }

    public enum AnimationLayerOutputPolicy
    {
        Unspecified,
        RequireOutput,
        AllowEmpty
    }

    [Serializable]
    public sealed class CharacterAnimationLayerDefinition
    {
        public const string BaseLayerId = "Base";

        [SerializeField] string m_Id = BaseLayerId;
        [SerializeField] int m_AnimancerLayerIndex;
        [SerializeField] AvatarMask m_AvatarMask;
        [SerializeField] AnimationBlendMode m_BlendMode;
        [SerializeField] AnimationLayerOutputPolicy m_OutputPolicy;

        public CharacterAnimationLayerDefinition() { }

        public CharacterAnimationLayerDefinition(
            string id,
            int animancerLayerIndex,
            AvatarMask avatarMask,
            AnimationBlendMode blendMode,
            AnimationLayerOutputPolicy outputPolicy)
        {
            Configure(id, animancerLayerIndex, avatarMask, blendMode, outputPolicy);
        }

        public string Id => m_Id;
        public int AnimancerLayerIndex => m_AnimancerLayerIndex;
        public AvatarMask AvatarMask => m_AvatarMask;
        public AnimationBlendMode BlendMode => m_BlendMode;
        public AnimationLayerOutputPolicy OutputPolicy => m_OutputPolicy;

        public void Configure(
            string id,
            int animancerLayerIndex,
            AvatarMask avatarMask,
            AnimationBlendMode blendMode,
            AnimationLayerOutputPolicy outputPolicy)
        {
            m_Id = id ?? string.Empty;
            m_AnimancerLayerIndex = animancerLayerIndex;
            m_AvatarMask = avatarMask;
            m_BlendMode = blendMode;
            m_OutputPolicy = outputPolicy;
        }

        public static CharacterAnimationLayerDefinition CreateBase()
        {
            return new CharacterAnimationLayerDefinition(
                BaseLayerId,
                0,
                null,
                AnimationBlendMode.Override,
                AnimationLayerOutputPolicy.Unspecified);
        }
    }

    public readonly struct ResolvedAnimationLayer
    {
        public ResolvedAnimationLayer(
            string id,
            int animancerLayerIndex,
            AvatarMask avatarMask,
            AnimationBlendMode blendMode,
            AnimationLayerOutputPolicy outputPolicy,
            int order)
        {
            Id = id ?? string.Empty;
            AnimancerLayerIndex = animancerLayerIndex;
            AvatarMask = avatarMask;
            BlendMode = blendMode;
            OutputPolicy = outputPolicy;
            Order = order;
        }

        public string Id { get; }
        public int AnimancerLayerIndex { get; }
        public AvatarMask AvatarMask { get; }
        public AnimationBlendMode BlendMode { get; }
        public AnimationLayerOutputPolicy OutputPolicy { get; }
        public int Order { get; }
    }
}
