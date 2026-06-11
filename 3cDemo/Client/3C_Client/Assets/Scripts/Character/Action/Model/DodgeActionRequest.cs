using System;
using UnityEngine;

namespace ThirdPersonAction
{
    [Serializable]
    public readonly struct DodgeActionRequest
    {
        public DodgeActionRequest(
            DodgeActionVariant variant,
            Vector3 worldDirection,
            int originStep,
            int expireStep,
            int priority,
            int sourceOrder,
            ActionStateId targetState)
        {
            Variant = variant;
            WorldDirection = NormalizePlanarOrZero(worldDirection);
            OriginStep = Mathf.Max(0, originStep);
            ExpireStep = Mathf.Max(OriginStep, expireStep);
            Priority = Mathf.Max(0, priority);
            SourceOrder = Mathf.Max(0, sourceOrder);
            TargetState = targetState.IsValid ? targetState : ActionStateIds.Dodge;
        }

        public DodgeActionVariant Variant { get; }
        public Vector3 WorldDirection { get; }
        public int OriginStep { get; }
        public int ExpireStep { get; }
        public int Priority { get; }
        public int SourceOrder { get; }
        public ActionStateId TargetState { get; }
        public bool HasValidDirection => WorldDirection.sqrMagnitude > 0.000001f;

        public ActionInterruptRequest ToInterruptRequest()
        {
            return new ActionInterruptRequest(
                requestId: OriginStep,
                requestType: ActionRequestType.Dodge,
                targetState: TargetState,
                priority: Priority,
                sourceOrder: SourceOrder,
                originTick: OriginStep,
                expireTick: ExpireStep);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
