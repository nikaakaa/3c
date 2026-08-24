using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [AddComponentMenu("")]
    public sealed class ClipCurves : MonoBehaviour
    {
        [SerializeField] float m_GaitPhase;
        [SerializeField, Range(0f, 1f)] float m_FootIK;
        [SerializeField, Min(0f)] float m_LStepTime;
        [SerializeField, Min(0f)] float m_RStepTime;
        [SerializeField, Min(0f)] float m_LStepDist;
        [SerializeField, Min(0f)] float m_RStepDist;
        [SerializeField, Min(0f)] float m_LFootHeight;
        [SerializeField, Min(0f)] float m_RFootHeight;
        [SerializeField] float m_LToeHeight;
        [SerializeField] float m_RToeHeight;
        [SerializeField, Min(0f)] float m_LToeSpeed;
        [SerializeField, Min(0f)] float m_RToeSpeed;
        [SerializeField, Min(0f)] float m_LPosError;
        [SerializeField, Min(0f)] float m_RPosError;
        [SerializeField, Min(0f)] float m_LRotError;
        [SerializeField, Min(0f)] float m_RRotError;
        [SerializeField, Range(0f, 1f)] float m_LContact;
        [SerializeField, Range(0f, 1f)] float m_RContact;
        [SerializeField, Range(0f, 2f)] float m_LLockMode;
        [SerializeField, Range(0f, 2f)] float m_RLockMode;
        [SerializeField, Range(0f, 1f)] float m_LLockWeight;
        [SerializeField, Range(0f, 1f)] float m_RLockWeight;
        [SerializeField, Range(0f, 1f)] float m_LSupport;
        [SerializeField, Range(0f, 1f)] float m_RSupport;
    }
}
