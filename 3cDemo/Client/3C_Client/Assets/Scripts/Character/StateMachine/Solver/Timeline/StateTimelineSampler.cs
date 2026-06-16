using System.Collections.Generic;
using ThirdPersonAction;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine
{
    public static class StateTimelineSampler
    {
        public static StateTimelineWindowFacts Sample(
            in StateTimelinePolicyDefinition policy,
            float normalizedTime,
            bool hasValidNormalizedTime,
            float elapsedSeconds,
            ActionRequestType requestType = ActionRequestType.None)
        {
            bool motion = false;
            bool inputLock = false;
            bool interrupt = false;
            bool exit = false;
            int priority = policy.Priority;
            int resistance = policy.Resistance;
            int minPriority = 0;
            bool force = false;
            List<string> activeIds = null;
            List<string> requestIds = null;
            List<string> activeFactIds = null;
            List<string> requestFactIds = null;

            for (int i = 0; i < policy.Windows.Count; i++)
            {
                StateTimelineWindowDefinition window = policy.Windows[i];
                if (!Contains(window, normalizedTime, hasValidNormalizedTime, elapsedSeconds))
                    continue;

                bool requestWindowAllowed = window.IsRequestWindow &&
                                            (requestType == ActionRequestType.None || window.AllowsRequest(requestType));
                if (window.IsRequestWindow && !requestWindowAllowed)
                    continue;

                activeIds ??= new List<string>();
                activeIds.Add(window.WindowId);
                if (window.FactId.IsValid)
                {
                    activeFactIds ??= new List<string>();
                    activeFactIds.Add(window.FactId.Value);
                }

                priority = Mathf.Max(priority, window.Priority);
                resistance = Mathf.Max(resistance, window.Resistance);

                if (requestWindowAllowed)
                {
                    requestIds ??= new List<string>();
                    requestIds.Add(window.WindowId);
                    if (window.FactId.IsValid)
                    {
                        requestFactIds ??= new List<string>();
                        requestFactIds.Add(window.FactId.Value);
                    }

                    minPriority = Mathf.Max(minPriority, window.MinPriority);
                    force = force || window.Force;
                }

                switch (window.Kind)
                {
                    case StateTimelineWindowKind.Motion:
                        motion = true;
                        break;
                    case StateTimelineWindowKind.InputLock:
                        inputLock = true;
                        break;
                    case StateTimelineWindowKind.Interrupt:
                    case StateTimelineWindowKind.Cancel:
                        interrupt = true;
                        break;
                    case StateTimelineWindowKind.Exit:
                        exit = true;
                        break;
                }
            }

            return new StateTimelineWindowFacts(
                policy.StateId,
                normalizedTime,
                hasValidNormalizedTime,
                elapsedSeconds,
                motion,
                inputLock,
                interrupt,
                exit,
                priority,
                resistance,
                minPriority,
                force,
                activeIds == null ? string.Empty : string.Join(",", activeIds),
                requestIds == null ? string.Empty : string.Join(",", requestIds),
                activeFactIds == null ? string.Empty : string.Join(",", activeFactIds),
                requestFactIds == null ? string.Empty : string.Join(",", requestFactIds));
        }

        public static StateTimelineWindowFacts None(CharacterStateId stateId)
        {
            return StateTimelineWindowFacts.None(stateId);
        }

        static bool Contains(
            StateTimelineWindowDefinition window,
            float normalizedTime,
            bool hasValidNormalizedTime,
            float elapsedSeconds)
        {
            switch (window.TimeDomain)
            {
                case StateTimelineTimeDomain.Normalized:
                    return hasValidNormalizedTime &&
                           normalizedTime + 0.0001f >= window.Start &&
                           normalizedTime <= window.End + 0.0001f;
                case StateTimelineTimeDomain.Seconds:
                    return elapsedSeconds + 0.0001f >= window.Start &&
                           elapsedSeconds <= window.End + 0.0001f;
                default:
                    return false;
            }
        }
    }
}
