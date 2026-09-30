using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraInputSettings
    {
        [SerializeField] string m_MoveInputId;
        [SerializeField] Vector2 m_MaxSpeed;
        [SerializeField] Vector2 m_AccelerationTime;
        [SerializeField] Vector2 m_DecelerationTime;
        [SerializeField] Vector2 m_PointerInputScale;
        [SerializeField] Vector2 m_StickInputScale;
        [SerializeField] Vector2 m_PointerActivationThreshold;
        [SerializeField] Vector2 m_StickActivationThreshold;
        [SerializeField] float m_DragExitDuration;
        [SerializeField] Vector2 m_PointerAxisGain;
        [SerializeField] Vector2 m_StickAxisGain;
        [SerializeField] Vector2 m_AxisDirection = Vector2.one;
        [SerializeField] Vector2 m_ElevationRange = new Vector2(0f, 1f);
        [SerializeField] Vector2 m_PitchLimit = new Vector2(-70f, 70f);
        [SerializeField] float m_DefaultResponseWeight = 1f;
        [SerializeField] float m_PitchResponseWeight = 1f;
        [SerializeField] float m_YawResponseWeight = 1f;

        public string MoveInputId => m_MoveInputId;
        public Vector2 MaxSpeed => m_MaxSpeed;
        public Vector2 AccelerationTime => m_AccelerationTime;
        public Vector2 DecelerationTime => m_DecelerationTime;
        public Vector2 PointerInputScale => m_PointerInputScale;
        public Vector2 StickInputScale => m_StickInputScale;
        public Vector2 PointerActivationThreshold => m_PointerActivationThreshold;
        public Vector2 StickActivationThreshold => m_StickActivationThreshold;
        public float DragExitDuration => m_DragExitDuration;
        public Vector2 PointerAxisGain => m_PointerAxisGain;
        public Vector2 StickAxisGain => m_StickAxisGain;
        public Vector2 AxisDirection => m_AxisDirection;
        public Vector2 ElevationRange => m_ElevationRange;
        public Vector2 PitchLimit => m_PitchLimit;
        public float DefaultResponseWeight => m_DefaultResponseWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;

        public CameraInputSettings() { }

        internal CameraInputSettings(CameraInputSettings source)
        {
            ConfigureAxes(source.MaxSpeed, source.AccelerationTime, source.DecelerationTime,
                source.PointerInputScale, source.StickInputScale, source.PointerAxisGain,
                source.StickAxisGain, source.AxisDirection, source.ElevationRange);
            ConfigureDrag(source.PointerActivationThreshold, source.StickActivationThreshold, source.DragExitDuration);
            m_MoveInputId = source.MoveInputId;
            m_PitchLimit = source.PitchLimit;
            m_DefaultResponseWeight = source.DefaultResponseWeight;
            m_PitchResponseWeight = source.PitchResponseWeight;
            m_YawResponseWeight = source.YawResponseWeight;
        }

        public void ConfigureMovementInput(string inputId) => m_MoveInputId = inputId;

        public void ConfigureAxes(Vector2 maxSpeed, Vector2 accelerationTime, Vector2 decelerationTime,
            Vector2 pointerInputScale, Vector2 stickInputScale, Vector2 pointerAxisGain,
            Vector2 stickAxisGain, Vector2 axisDirection, Vector2 elevationRange)
        {
            m_MaxSpeed = maxSpeed;
            m_AccelerationTime = accelerationTime;
            m_DecelerationTime = decelerationTime;
            m_PointerInputScale = pointerInputScale;
            m_StickInputScale = stickInputScale;
            m_PointerAxisGain = pointerAxisGain;
            m_StickAxisGain = stickAxisGain;
            m_AxisDirection = axisDirection;
            m_ElevationRange = elevationRange;
        }

        public void ConfigureDrag(Vector2 pointerActivationThreshold, Vector2 stickActivationThreshold, float exitDuration)
        {
            m_PointerActivationThreshold = pointerActivationThreshold;
            m_StickActivationThreshold = stickActivationThreshold;
            m_DragExitDuration = exitDuration;
        }

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(MoveInputId) || !Positive(MaxSpeed) || !NonNegative(AccelerationTime) || !NonNegative(DecelerationTime) ||
                !NonNegative(PointerInputScale) || !NonNegative(StickInputScale) ||
                !NonNegative(PointerActivationThreshold) || !NonNegative(StickActivationThreshold) ||
                !float.IsFinite(DragExitDuration) || DragExitDuration < 0f ||
                !Positive(PointerAxisGain) || !Positive(StickAxisGain) ||
                Mathf.Abs(AxisDirection.x) != 1f || Mathf.Abs(AxisDirection.y) != 1f ||
                !Finite(ElevationRange) || ElevationRange.x >= ElevationRange.y ||
                !Finite(PitchLimit) || PitchLimit.x >= PitchLimit.y ||
                !float.IsFinite(DefaultResponseWeight) || !float.IsFinite(PitchResponseWeight) ||
                !float.IsFinite(YawResponseWeight) ||
                DefaultResponseWeight < 0f || DefaultResponseWeight > 1f ||
                PitchResponseWeight < 0f || PitchResponseWeight > 1f ||
                YawResponseWeight < 0f || YawResponseWeight > 1f)
                throw new InvalidOperationException($"{source} contains invalid Camera input settings.");
        }

        static bool Finite(Vector2 value) => float.IsFinite(value.x) && float.IsFinite(value.y);
        static bool Positive(Vector2 value) => Finite(value) && value.x > 0f && value.y > 0f;
        static bool NonNegative(Vector2 value) => Finite(value) && value.x >= 0f && value.y >= 0f;
    }
}
