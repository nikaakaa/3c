using ThirdPersonSimulation;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct BtsmtlSkillInvocationContext
    {
        BtsmtlSkillInvocationContext(OperationHandle owner, ProgramInvocationCallerKind callerKind,
            string callerId, string clipId, bool useEnable)
        {
            Owner = owner;
            CallerKind = callerKind;
            CallerId = callerId;
            ClipId = clipId;
            UseTimelineEnable = useEnable;
        }
        public OperationHandle Owner { get; }
        public ProgramInvocationCallerKind CallerKind { get; }
        public string CallerId { get; }
        public string ClipId { get; }
        public bool UseTimelineEnable { get; }

        public static BtsmtlSkillInvocationContext Call(string nodeId, OperationHandle owner = default) =>
            new(owner, ProgramInvocationCallerKind.Node, nodeId, string.Empty, false);
        public static BtsmtlSkillInvocationContext Condition(string edgeId, OperationHandle owner) =>
            new(owner, ProgramInvocationCallerKind.Edge, edgeId, string.Empty, false);
        public static BtsmtlSkillInvocationContext Marker(string nodeId, string markerId, TimelineExecutionDomain domain) =>
            new(default, domain == TimelineExecutionDomain.Presentation
                ? ProgramInvocationCallerKind.PresentationMarker : ProgramInvocationCallerKind.TimelineClip,
                nodeId, markerId, true);
        public static BtsmtlSkillInvocationContext TreeClip(string nodeId, string clipId, bool useEnable) =>
            new(default, ProgramInvocationCallerKind.TimelineClip, nodeId, clipId, useEnable);
    }
}
