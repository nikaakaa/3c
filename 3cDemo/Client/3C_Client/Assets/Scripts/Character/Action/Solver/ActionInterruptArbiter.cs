using System.Collections.Generic;
using ThirdPersonDiagnostics;

namespace ThirdPersonAction
{
    public static class ActionInterruptArbiter
    {
        public static ActionInterruptDecision Arbitrate(
            in ActionInterruptContext context,
            IReadOnlyList<ActionInterruptRequest> requests,
            IReadOnlyList<ActionInterruptPolicy> policies)
        {
            if (requests == null || requests.Count == 0)
            {
                LogDecision(in context, ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest), 0, policies != null ? policies.Count : 0);
                return ActionInterruptDecision.Reject(ActionInterruptRejectReason.NoRequest);
            }

            ActionInterruptRequest selected = default;
            bool hasSelected = false;
            ActionInterruptRejectReason rejectReason = ActionInterruptRejectReason.NoRequest;

            for (int i = 0; i < requests.Count; i++)
            {
                ActionInterruptRequest request = requests[i];
                ActionInterruptRejectReason requestRejectReason;
                if (!TryAcceptRequest(in context, request, policies, out requestRejectReason))
                {
                    rejectReason = MoreUseful(rejectReason, requestRejectReason);
                    continue;
                }

                if (!hasSelected || IsHigherPriority(request, selected))
                {
                    selected = request;
                    hasSelected = true;
                }
            }

            return hasSelected
                ? LogDecision(in context, ActionInterruptDecision.Accept(selected), requests.Count, policies != null ? policies.Count : 0)
                : LogDecision(in context, ActionInterruptDecision.Reject(rejectReason), requests.Count, policies != null ? policies.Count : 0);
        }

        static bool TryAcceptRequest(
            in ActionInterruptContext context,
            ActionInterruptRequest request,
            IReadOnlyList<ActionInterruptPolicy> policies,
            out ActionInterruptRejectReason rejectReason)
        {
            if (request.IsExpired(context.CurrentTick))
            {
                rejectReason = ActionInterruptRejectReason.Expired;
                LogRequestRejected(in context, request, rejectReason, -1, default);
                return false;
            }

            if (policies == null || policies.Count == 0)
            {
                rejectReason = ActionInterruptRejectReason.NoPolicy;
                LogRequestRejected(in context, request, rejectReason, -1, default);
                return false;
            }

            bool hasMatchingPolicy = false;
            rejectReason = ActionInterruptRejectReason.NoPolicy;

            for (int i = 0; i < policies.Count; i++)
            {
                ActionInterruptPolicy policy = policies[i];
                if (!policy.Matches(context, request))
                    continue;

                hasMatchingPolicy = true;

                if (!ActionInterruptPolicyValidator.IsPolicyValid(policy))
                {
                    rejectReason = MoreUseful(rejectReason, ActionInterruptRejectReason.InvalidPolicy);
                    LogRequestRejected(in context, request, ActionInterruptRejectReason.InvalidPolicy, i, policy);
                    continue;
                }

                if (request.Priority < policy.MinPriority)
                {
                    rejectReason = MoreUseful(rejectReason, ActionInterruptRejectReason.PriorityTooLow);
                    LogRequestRejected(in context, request, ActionInterruptRejectReason.PriorityTooLow, i, policy);
                    continue;
                }

                if (!policy.Force && request.Priority <= context.CurrentStateResistance)
                {
                    rejectReason = MoreUseful(rejectReason, ActionInterruptRejectReason.BlockedByResistance);
                    LogRequestRejected(in context, request, ActionInterruptRejectReason.BlockedByResistance, i, policy);
                    continue;
                }

                if (!IsTimingSatisfied(in context, policy))
                {
                    rejectReason = MoreUseful(rejectReason, ActionInterruptRejectReason.TimingNotSatisfied);
                    LogRequestRejected(in context, request, ActionInterruptRejectReason.TimingNotSatisfied, i, policy);
                    continue;
                }

                rejectReason = ActionInterruptRejectReason.None;
                LogRequestAccepted(in context, request, i, policy);
                return true;
            }

            if (!hasMatchingPolicy)
            {
                rejectReason = ActionInterruptRejectReason.NoPolicy;
                LogRequestRejected(in context, request, rejectReason, -1, default);
            }

            return false;
        }

        static bool IsTimingSatisfied(in ActionInterruptContext context, ActionInterruptPolicy policy)
        {
            switch (policy.TimingRule)
            {
                case ActionInterruptTimingRule.Always:
                    return true;
                case ActionInterruptTimingRule.AfterElapsedTime:
                    return context.CurrentStateElapsedSeconds >= policy.WindowStart;
                case ActionInterruptTimingRule.DuringElapsedTimeWindow:
                    return context.CurrentStateElapsedSeconds >= policy.WindowStart &&
                           context.CurrentStateElapsedSeconds <= policy.WindowEnd;
                default:
                    return false;
            }
        }

        static bool IsHigherPriority(ActionInterruptRequest candidate, ActionInterruptRequest current)
        {
            if (candidate.Priority != current.Priority)
                return candidate.Priority > current.Priority;

            if (candidate.SourceOrder != current.SourceOrder)
                return candidate.SourceOrder < current.SourceOrder;

            return false;
        }

        static ActionInterruptRejectReason MoreUseful(ActionInterruptRejectReason current, ActionInterruptRejectReason next)
        {
            return Rank(next) > Rank(current) ? next : current;
        }

        static int Rank(ActionInterruptRejectReason reason)
        {
            return reason switch
            {
                ActionInterruptRejectReason.InvalidPolicy => 7,
                ActionInterruptRejectReason.BlockedByResistance => 6,
                ActionInterruptRejectReason.PriorityTooLow => 5,
                ActionInterruptRejectReason.TimingNotSatisfied => 4,
                ActionInterruptRejectReason.NoPolicy => 3,
                ActionInterruptRejectReason.Expired => 2,
                ActionInterruptRejectReason.NoRequest => 1,
                _ => 0
            };
        }

        static ActionInterruptDecision LogDecision(
            in ActionInterruptContext context,
            ActionInterruptDecision decision,
            int requestCount,
            int policyCount)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Trace,
                decision.Accepted ? "interrupt-decision-accepted" : "interrupt-decision-rejected",
                decision.TargetState.Value,
                context.CurrentState.Value,
                context.CurrentTick,
                0,
                $"accepted={decision.Accepted} target={decision.TargetState.Value} reject={decision.RejectReason} requests={requestCount} policies={policyCount} elapsed={context.CurrentStateElapsedSeconds:F3} resistance={context.CurrentStateResistance}"));
            return decision;
        }

        static void LogRequestAccepted(
            in ActionInterruptContext context,
            ActionInterruptRequest request,
            int policyIndex,
            ActionInterruptPolicy policy)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Trace,
                "interrupt-request-accepted",
                request.TargetState.Value,
                context.CurrentState.Value,
                context.CurrentTick,
                0,
                $"request={request.RequestType} id={request.RequestId} origin={request.OriginTick} expire={request.ExpireTick} priority={request.Priority} sourceOrder={request.SourceOrder} policyIndex={policyIndex} policyFrom={policy.FromState.Value} policyTarget={policy.TargetState.Value} minPriority={policy.MinPriority} timing={policy.TimingRule} windowStart={policy.WindowStart:F3} windowEnd={policy.WindowEnd:F3} force={policy.Force} elapsed={context.CurrentStateElapsedSeconds:F3} resistance={context.CurrentStateResistance}"));
        }

        static void LogRequestRejected(
            in ActionInterruptContext context,
            ActionInterruptRequest request,
            ActionInterruptRejectReason reason,
            int policyIndex,
            ActionInterruptPolicy policy)
        {
            string policyContext = policyIndex >= 0
                ? $" policyIndex={policyIndex} policyFrom={policy.FromState.Value} policyTarget={policy.TargetState.Value} minPriority={policy.MinPriority} timing={policy.TimingRule} windowStart={policy.WindowStart:F3} windowEnd={policy.WindowEnd:F3} force={policy.Force}"
                : " policyIndex=none";
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Trace,
                "interrupt-request-rejected",
                request.TargetState.Value,
                context.CurrentState.Value,
                context.CurrentTick,
                0,
                $"request={request.RequestType} id={request.RequestId} origin={request.OriginTick} expire={request.ExpireTick} priority={request.Priority} sourceOrder={request.SourceOrder} reason={reason}{policyContext} elapsed={context.CurrentStateElapsedSeconds:F3} resistance={context.CurrentStateResistance}"));
        }
    }
}
