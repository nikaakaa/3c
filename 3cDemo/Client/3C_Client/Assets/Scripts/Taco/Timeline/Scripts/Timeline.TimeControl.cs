using System;
using UnityEngine;

namespace Taco.Timeline
{
    [TrackGroup("Base"), ScriptGuid("f3ac1e9577031234db593b7ceec0ff8e"), IconGuid("901e0ed8e4489e846b3261cdaf5a9da0"), Ordered(0), Color(127, 253, 228)]
    public class TimeSpeedTrack : Track
    {

#if UNITY_EDITOR

        public override Type ClipType => typeof(TimeSpeedClip);
#endif
    }

    [ScriptGuid("f3ac1e9577031234db593b7ceec0ff8e"), Color(127, 253, 228)]
    public class TimeSpeedClip : Clip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public float TargetSpeed;


        double m_ChangedValue;
        public override void Bind()
        {
            base.Bind();
            m_ChangedValue = 0;
        }
        public override void Unbind()
        {
            base.Unbind();
            Timeline.TimelinePlayer.PlaySpeed -= m_ChangedValue;
        }

        public override void Evaluate(float deltaTime)
        {
            double targetSpeed = 0;
            TargetTime = Time += deltaTime;

            if (Time < StartTime)
            {
                targetSpeed = 0;

            }
            else if (StartTime <= Time && Time <= EndTime)
            {
                float selfTime = Time - StartTime;
                float remainTime = EndTime - Time;

                if (selfTime < EaseInTime)
                {
                    targetSpeed = selfTime / EaseInTime;
                }
                else if (remainTime < EaseOutTime)
                {
                    targetSpeed = remainTime / EaseOutTime;
                }
                else
                {
                    targetSpeed = 1;
                }
            }
            else if (Time > EndTime)
            {
                targetSpeed = 0;
            }

            targetSpeed *= TargetSpeed;
            targetSpeed = Math.Round(targetSpeed, 2);

            double deltaSpeed = targetSpeed - m_ChangedValue;
            Timeline.TimelinePlayer.PlaySpeed += deltaSpeed;
            m_ChangedValue += deltaSpeed;
        }

#if UNITY_EDITOR

        public override ClipCapabilities Capabilities => base.Capabilities | ClipCapabilities.Mixable | ClipCapabilities.Resizable;
        public TimeSpeedClip(Track track, int frame) : base(track, frame) { }
#endif
    }
}