using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    [Serializable]
    public struct AnimationFootStepLandingEvent
    {
        [SerializeField] float m_NormalizedTime;
        [SerializeField] int m_Ordinal;
        [SerializeField] int m_CycleOffset;
        [SerializeField] float m_Distance;
        [SerializeField] Vector3 m_RootLocalLanding;

        public AnimationFootStepLandingEvent(
            float normalizedTime,
            int ordinal,
            int cycleOffset,
            float distance,
            Vector3 rootLocalLanding)
        {
            m_NormalizedTime = normalizedTime;
            m_Ordinal = ordinal;
            m_CycleOffset = cycleOffset;
            m_Distance = distance;
            m_RootLocalLanding = rootLocalLanding;
            RequireValid();
        }

        public float NormalizedTime => m_NormalizedTime;
        public int Ordinal => m_Ordinal;
        public int CycleOffset => m_CycleOffset;
        public float Distance => m_Distance;
        public Vector3 RootLocalLanding => m_RootLocalLanding;

        public void RequireValid()
        {
            if (!float.IsFinite(m_NormalizedTime) ||
                m_NormalizedTime < 0f ||
                m_NormalizedTime > 1f ||
                m_Ordinal <= 0 ||
                m_CycleOffset < 0 ||
                !float.IsFinite(m_Distance) ||
                m_Distance < 0f ||
                !Finite(m_RootLocalLanding))
            {
                throw new InvalidOperationException(
                    "Foot Step Landing Event is invalid.");
            }
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) &&
            float.IsFinite(value.y) &&
            float.IsFinite(value.z);
    }

    [Serializable]
    public sealed class AnimationFootStepLandingEventTable
    {
        [SerializeField] AnimationFootStepLandingEvent[] m_Events =
            Array.Empty<AnimationFootStepLandingEvent>();

        public AnimationFootStepLandingEventTable(
            AnimationFootStepLandingEvent[] events)
        {
            m_Events = events == null
                ? throw new ArgumentNullException(nameof(events))
                : (AnimationFootStepLandingEvent[])events.Clone();
            RequireValid();
        }

        public int Count => m_Events?.Length ?? 0;
        public AnimationFootStepLandingEvent EventAt(int index) => m_Events[index];

        public void RequireValid()
        {
            if (m_Events == null || m_Events.Length == 0)
                throw new InvalidOperationException(
                    "Foot Step Landing Event table is empty.");
            for (int i = 0; i < m_Events.Length; i++)
            {
                m_Events[i].RequireValid();
                if (i > 0 &&
                    m_Events[i].NormalizedTime <=
                    m_Events[i - 1].NormalizedTime)
                {
                    throw new InvalidOperationException(
                        "Foot Step Landing Event table is unordered.");
                }
            }
        }
    }
}
