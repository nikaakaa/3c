using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public sealed class CharacterAnimationSourceResourceBinding
    {
        [SerializeField] AnimationClip m_AuthoringClip;
        [SerializeField] CharacterAnimationSamplingBackendKind m_Backend = CharacterAnimationSamplingBackendKind.NativeClip;

        public AnimationClip AuthoringClip => m_AuthoringClip;
        public CharacterAnimationSamplingBackendKind Backend => m_Backend;

        public void ConfigureNativeClip(AnimationClip clip)
        {
            m_AuthoringClip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            m_Backend = CharacterAnimationSamplingBackendKind.NativeClip;
        }

        public void ConfigureAcl(AnimationClip clip)
        {
            m_AuthoringClip = clip ? clip : throw new ArgumentNullException(nameof(clip));
            m_Backend = CharacterAnimationSamplingBackendKind.Acl;
        }

        public void RequireValid()
        {
            if (!m_AuthoringClip || !float.IsFinite(m_AuthoringClip.length) || m_AuthoringClip.length <= 0f ||
                !Enum.IsDefined(typeof(CharacterAnimationSamplingBackendKind), Backend))
                throw new InvalidOperationException("Animation source resource binding is invalid.");
        }
    }
}
