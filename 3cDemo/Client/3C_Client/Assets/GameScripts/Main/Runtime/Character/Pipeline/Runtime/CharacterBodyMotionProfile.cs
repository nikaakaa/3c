using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    [CreateAssetMenu(fileName = "CharacterBodyMotionProfile", menuName = "3C/Character/Body Motion Profile")]
    public sealed class CharacterBodyMotionProfile : ScriptableObject
    {
        public const int SemanticVersion = 1;

        [SerializeField] float m_GravityAcceleration = -25f;
        [SerializeField] float m_MaximumFallSpeed = 40f;

        public float GravityAcceleration => m_GravityAcceleration;
        public float MaximumFallSpeed => m_MaximumFallSpeed;

        public bool CollectConfigurationErrors(List<string> errors)
        {
            bool valid = true;
            if (float.IsNaN(m_GravityAcceleration) || float.IsInfinity(m_GravityAcceleration) || m_GravityAcceleration >= 0f)
            {
                errors?.Add($"{name}: Gravity Acceleration must be a finite negative value.");
                valid = false;
            }
            if (float.IsNaN(m_MaximumFallSpeed) || float.IsInfinity(m_MaximumFallSpeed) || m_MaximumFallSpeed <= 0f)
            {
                errors?.Add($"{name}: Maximum Fall Speed must be a finite positive value.");
                valid = false;
            }
            return valid;
        }
    }
}
