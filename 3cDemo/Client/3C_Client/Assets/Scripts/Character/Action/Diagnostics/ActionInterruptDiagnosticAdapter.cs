using System;
using ThirdPersonDiagnostics;

namespace ThirdPersonAction
{
    public sealed class ActionInterruptDiagnosticAdapter
    {
        readonly ICharacterDiagnosticSink sink;

        public ActionInterruptDiagnosticAdapter(ICharacterDiagnosticSink sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public ActionInterruptDecision LogDecision(
            in ActionInterruptContext context,
            ActionInterruptDecision decision,
            int requestCount,
            int policyCount)
        {
            Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Trace,
                decision.Accepted ? "interrupt-decision-accepted" : "interrupt-decision-rejected",
                decision.TargetState.Value,
                context.CurrentState.Value,
                context.CurrentTick,
                0,
                $"accepted={decision.Accepted} target={decision.TargetState.Value} reject={decision.RejectReason} requests={requestCount} policies={policyCount} elapsed={context.CurrentStateElapsedSeconds:F3} resistance={context.CurrentStateResistance} timelineWindows={context.TimelineFacts.ActiveWindowIds} requestWindows={context.TimelineFacts.RequestWindowIds} timelineFacts={context.TimelineFacts.ActiveFactIds} requestFacts={context.TimelineFacts.RequestFactIds} timelineResistance={context.TimelineFacts.Resistance}"));
            return decision;
        }

        public void LogRequestAccepted(
            in ActionInterruptContext context,
            ActionInterruptRequest request,
            int policyIndex,
            ActionInterruptPolicy policy)
        {
            Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Trace,
                "interrupt-request-accepted",
                request.TargetState.Value,
                context.CurrentState.Value,
                context.CurrentTick,
                0,
                $"request={request.RequestType} id={request.RequestId} origin={request.OriginTick} expire={request.ExpireTick} priority={request.Priority} sourceOrder={request.SourceOrder} policyIndex={policyIndex} policyFrom={policy.FromState.Value} policyTarget={policy.TargetState.Value} policyRequestType={policy.RequestType} minPriority={policy.MinPriority} timing={policy.TimingRule} windowStart={policy.WindowStart:F3} windowEnd={policy.WindowEnd:F3} windowId={policy.WindowId} requiredFactId={policy.RequiredFactId.Value} force={policy.Force} elapsed={context.CurrentStateElapsedSeconds:F3} resistance={context.CurrentStateResistance} timelineWindows={context.TimelineFacts.ActiveWindowIds} requestWindows={context.TimelineFacts.RequestWindowIds} timelineFacts={context.TimelineFacts.ActiveFactIds} requestFacts={context.TimelineFacts.RequestFactIds} timelineMinPriority={context.TimelineFacts.MinPriority} timelineResistance={context.TimelineFacts.Resistance}"));
        }

        public void LogRequestRejected(
            in ActionInterruptContext context,
            ActionInterruptRequest request,
            ActionInterruptRejectReason reason,
            int policyIndex,
            ActionInterruptPolicy policy)
        {
            string policyContext = policyIndex >= 0
                ? $" policyIndex={policyIndex} policyFrom={policy.FromState.Value} policyTarget={policy.TargetState.Value} policyRequestType={policy.RequestType} minPriority={policy.MinPriority} timing={policy.TimingRule} windowStart={policy.WindowStart:F3} windowEnd={policy.WindowEnd:F3} windowId={policy.WindowId} requiredFactId={policy.RequiredFactId.Value} force={policy.Force}"
                : " policyIndex=none";
            Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Trace,
                "interrupt-request-rejected",
                request.TargetState.Value,
                context.CurrentState.Value,
                context.CurrentTick,
                0,
                $"request={request.RequestType} id={request.RequestId} origin={request.OriginTick} expire={request.ExpireTick} priority={request.Priority} sourceOrder={request.SourceOrder} reason={reason}{policyContext} elapsed={context.CurrentStateElapsedSeconds:F3} resistance={context.CurrentStateResistance} timelineWindows={context.TimelineFacts.ActiveWindowIds} requestWindows={context.TimelineFacts.RequestWindowIds} timelineFacts={context.TimelineFacts.ActiveFactIds} requestFacts={context.TimelineFacts.RequestFactIds} timelineMinPriority={context.TimelineFacts.MinPriority} timelineResistance={context.TimelineFacts.Resistance}"));
        }

        void Submit(RuntimeDiagnosticLogEvent diagnosticEvent)
        {
            sink.Submit(in diagnosticEvent);
        }
    }
}
