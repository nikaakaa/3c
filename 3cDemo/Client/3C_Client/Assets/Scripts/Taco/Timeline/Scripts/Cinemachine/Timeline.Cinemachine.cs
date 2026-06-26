using System;
using UnityEngine;
using Cinemachine;

namespace Taco.Timeline
{
    [TrackGroup("Base"), ScriptGuid("ef54de86b5796c549ab1911aa430f9b5"), IconGuid("bef36b97b5ebdd24a90df9570eb6e05d"), Ordered(0), Color(14, 106, 201)]
    public class CinemachineImpluseTrack : Track
    {

#if UNITY_EDITOR

        public override Type ClipType => typeof(CinemachineImpluseClip);
#endif
    }

    [ScriptGuid("ef54de86b5796c549ab1911aa430f9b5"), Color(14, 106, 201)]
    public class CinemachineImpluseClip : SignalClip
    {
        [ShowInInspector(3), ShowIf("UseCameraImpulse")]
        public CinemachineImpulseDefinition.ImpulseTypes ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
        [ShowInInspector(3), ShowIf("UseCameraImpulse"), OnValueChanged("RepaintInspector")]
        public CinemachineImpulseDefinition.ImpulseShapes ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
        [ShowInInspector(3), ShowIf("ShowImpulseCurve")]
        public AnimationCurve ImpulseCurve;
        [ShowInInspector(3), ShowIf("UseCameraImpulse")]
        public float ImpulseDuration = 0.1f;
        [ShowInInspector(3), ShowIf("UseCameraImpulse")]
        public Vector3 ImpulseVelocity;

        CinemachineImpulseDefinition m_ImpulseDefinition;
        public override void Bind()
        {
            base.Bind();
            m_ImpulseDefinition = new CinemachineImpulseDefinition
            {
                m_ImpulseChannel = 1,
                m_ImpulseShape = ImpulseShape,
                m_CustomImpulseShape = ImpulseCurve,
                m_ImpulseDuration = ImpulseDuration,
                m_ImpulseType = ImpulseType,
                m_DissipationDistance = 100,
                m_DissipationRate = 0.25f,
                m_PropagationSpeed = 343
            };
        }
        public override void Unbind()
        {
            base.Unbind();
        }
        public override void OnEnable()
        {
            base.OnEnable();
            if (m_ImpulseDefinition != null)
                m_ImpulseDefinition.CreateEvent(Timeline.TimelinePlayer.transform.position, ImpulseVelocity);
        }

#if UNITY_EDITOR

        public CinemachineImpluseClip(Track track, int frame) : base(track, frame) { }

        bool ShowImpulseCurve()
        {
            return ImpulseShape == CinemachineImpulseDefinition.ImpulseShapes.Custom;
        }
#endif
    }
}