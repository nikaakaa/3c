using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    public sealed class CharacterAnimationEventGraphHost : IDisposable
    {
        readonly ActorId m_ActorId;
        readonly CharacterAnimationEventGraph m_Graph;
        readonly CharacterAnimationVariableContract m_VariableContract;
        readonly EventGraphHostContract m_HostContract;
        readonly NativeEventGraphRuntime m_Runtime;
        public const string HasCameraInput = "presentation.has-camera";
        public const string ControlStateInputPrefix = "presentation.control-state/";
        public const string AbilityActiveInputPrefix = "presentation.ability-active/";
        public const string TimelineActiveInputPrefix = "presentation.timeline-active/";
        public const string TimelineClipActiveInputPrefix = "presentation.timeline-clip-active/";

        readonly FactHostContext m_Context;

        ulong m_LastBodyDiscontinuityGeneration;
        CharacterAnimationVariableFrame m_LastFrame;
        bool m_Disposed;

        public CharacterAnimationEventGraphHost(
            CharacterAnimationEventGraph graph,
            ActorId actorId,
            bool hasCamera)
        {
            m_Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            if (!actorId.IsValid)
                throw new ArgumentException(
                    "Character animation event graph Actor identity is invalid.",
                    nameof(actorId));
            m_ActorId = actorId;
            m_HostContract = CreateContract(graph);
            m_Context = new FactHostContext(m_HostContract, hasCamera);
            m_Runtime = new NativeEventGraphRuntime(
                graph,
                m_HostContract);
            m_VariableContract = new CharacterAnimationVariableContract(m_Runtime.VariableContract);
        }

        public CharacterAnimationEventGraph Graph => m_Graph;
        public CharacterAnimationVariableContract VariableContract =>
            m_VariableContract;
        public EventGraphHostContract HostContract => m_HostContract;
        public bool IsFaulted => m_Runtime.IsFaulted;
        public EventGraphExecutionFailure LastFailure => m_Runtime.LastFailure;
        public ulong ResetGeneration => m_Runtime.ResetGeneration;
        public CharacterAnimationVariableFrame LastFrame => m_LastFrame;

        [ThirdPersonPerformance.Instrumentation.PerformanceProbe("presentation.event-graph")]
        internal CharacterAnimationVariableUpdateResult Update(
            in CharacterPresentationFactFrame factFrame,
            ICommittedCharacterControlState controlState,
            float animationDeltaSeconds,
            ulong renderFrame)
        {
            RequireAlive();
            if (!factFrame.IsValid ||
                factFrame.Identity.ActorId != m_ActorId ||
                renderFrame == 0 ||
                factFrame.Identity.RenderFrame != renderFrame)
            {
                throw new ArgumentException(
                    "Character animation event graph fact frame identity is invalid.",
                    nameof(factFrame));
            }
            if (!float.IsFinite(animationDeltaSeconds) ||
                animationDeltaSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(animationDeltaSeconds));
            }
            if (m_LastBodyDiscontinuityGeneration != 0 &&
                m_LastBodyDiscontinuityGeneration !=
                factFrame.BodyDiscontinuityGeneration)
            {
                m_Runtime.Reset();
            }
            m_LastBodyDiscontinuityGeneration =
                factFrame.BodyDiscontinuityGeneration;
            var identity = new EventGraphInvocationIdentity(
                CharacterAnimationEventGraph.ContractId,
                renderFrame);
            var invocation = new EventGraphInvocationContext(
                identity,
                CharacterAnimationEventGraph.UpdateEventId,
                animationDeltaSeconds);
            m_Context.Set(invocation, in factFrame, controlState);
            NativeEventGraphExecutionResult execution =
                m_Runtime.Execute(invocation, m_Context);
            if (!execution.Succeeded)
                return CharacterAnimationVariableUpdateResult.Failed(
                    execution.Failure);
            var frame = new CharacterAnimationVariableFrame(
                m_ActorId,
                renderFrame,
                factFrame.SimulationTick,
                factFrame.BodyDiscontinuityGeneration,
                execution.Frame,
                m_VariableContract);
            m_LastFrame = frame;
            return CharacterAnimationVariableUpdateResult.Success(frame);
        }

        public void Reset()
        {
            RequireAlive();
            m_Runtime.Reset();
            m_LastBodyDiscontinuityGeneration = 0;
            m_LastFrame = default;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Runtime.Dispose();
        }

        public static EventGraphHostContract CreateContract(
            CharacterAnimationEventGraph graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));
            return CreateHostContract(
                new CharacterAnimationVariableContract(
                    graph.BuildVariableContract()), graph);
        }

        internal static EventGraphHostContract CreateHostContract(
            CharacterAnimationVariableContract variableContract, CharacterAnimationEventGraph graph)
        {
            var inputs = new List<EventGraphInputDescriptor>
            {
                new EventGraphInputDescriptor(HasCameraInput, typeof(bool)),
                new EventGraphInputDescriptor(CharacterPresentationFactSchema.MovementMode.Value, typeof(string)),
                new EventGraphInputDescriptor(
                    EventGraphHostInputIds.DeltaSeconds,
                    typeof(float)),
                new EventGraphInputDescriptor(
                    CharacterPresentationFactSchema.Velocity.Value,
                    typeof(UnityEngine.Vector3)),
                new EventGraphInputDescriptor(
                    CharacterPresentationFactSchema.Rotation.Value,
                    typeof(UnityEngine.Quaternion)),
                new EventGraphInputDescriptor(
                    CharacterPresentationFactSchema.Grounded.Value,
                    typeof(bool)),
                new EventGraphInputDescriptor(
                    CharacterPresentationFactSchema.DesiredPlanarVelocity.Value,
                    typeof(UnityEngine.Vector2)),
                new EventGraphInputDescriptor(
                    CharacterPresentationFactSchema.DesiredFacing.Value,
                    typeof(UnityEngine.Vector2)),
                new EventGraphInputDescriptor(
                    CharacterPresentationFactSchema.HasMotion.Value,
                    typeof(bool)),
                new EventGraphInputDescriptor(
                    CharacterPresentationFactSchema.LocomotionPlanarBasis.Value,
                    typeof(UnityEngine.Vector2))
            };
            var declaredInputs = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graph.allNodes.Count; i++)
            {
                if (graph.allNodes[i] is not EventGraphHostInputNodeMarker input)
                    continue;
                string id = input.InputId;
                if (id.StartsWith(ControlStateInputPrefix, StringComparison.Ordinal) && declaredInputs.Add(id))
                    inputs.Add(new EventGraphInputDescriptor(id, typeof(string)));
                else if ((id.StartsWith(AbilityActiveInputPrefix, StringComparison.Ordinal) ||
                          id.StartsWith(TimelineActiveInputPrefix, StringComparison.Ordinal) ||
                          id.StartsWith(TimelineClipActiveInputPrefix, StringComparison.Ordinal)) && declaredInputs.Add(id))
                    inputs.Add(new EventGraphInputDescriptor(id, typeof(bool)));
            }
            var outputs = new List<EventGraphOutputDescriptor>();
            for (int i = 0; i < variableContract.Variables.Count; i++)
            {
                EventGraphVariableDescriptor variable =
                    variableContract.Variables[i];
                outputs.Add(new EventGraphOutputDescriptor(
                    variable.Reference.VariableId,
                    variable.ValueType));
            }
            return new EventGraphHostContract(
                CharacterAnimationEventGraph.ContractId,
                CharacterAnimationEventGraph.ContractRevision,
                CharacterAnimationEventGraph.UpdateEventId,
                inputs,
                outputs);
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterAnimationEventGraphHost));
        }

        sealed class FactHostContext : IEventGraphHostContext
        {
            readonly Dictionary<string, CharacterControlStateFieldId> m_ControlInputs = new Dictionary<string, CharacterControlStateFieldId>(StringComparer.Ordinal);
            readonly Dictionary<string, CharacterSkillId> m_AbilityInputs = new Dictionary<string, CharacterSkillId>(StringComparer.Ordinal);
            readonly Dictionary<string, (string Timeline, string Clip)> m_TimelineInputs = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
            readonly bool m_HasCamera;
            ICommittedCharacterControlState m_ControlState;
            CharacterPresentationFactFrame m_Fact;

            internal FactHostContext(EventGraphHostContract contract, bool hasCamera)
            {
                m_HasCamera = hasCamera;
                for (int i = 0; i < contract.Inputs.Count; i++)
                {
                    string id = contract.Inputs[i].InputId;
                    if (id.StartsWith(ControlStateInputPrefix, StringComparison.Ordinal))
                        m_ControlInputs.Add(id, new CharacterControlStateFieldId(id.Substring(ControlStateInputPrefix.Length)));
                    else if (id.StartsWith(AbilityActiveInputPrefix, StringComparison.Ordinal))
                        m_AbilityInputs.Add(id, new CharacterSkillId(id.Substring(AbilityActiveInputPrefix.Length)));
                    else if (id.StartsWith(TimelineActiveInputPrefix, StringComparison.Ordinal))
                        m_TimelineInputs.Add(id, (id.Substring(TimelineActiveInputPrefix.Length), string.Empty));
                    else if (id.StartsWith(TimelineClipActiveInputPrefix, StringComparison.Ordinal))
                    {
                        int separator = id.IndexOf('/', TimelineClipActiveInputPrefix.Length);
                        m_TimelineInputs.Add(id, (id.Substring(TimelineClipActiveInputPrefix.Length, separator - TimelineClipActiveInputPrefix.Length), id.Substring(separator + 1)));
                    }
                }
            }
            EventGraphInvocationContext m_Invocation;

            public EventGraphInvocationContext Invocation => m_Invocation;

            internal void Set(
                EventGraphInvocationContext invocation,
                in CharacterPresentationFactFrame fact, ICommittedCharacterControlState controlState)
            {
                m_ControlState = controlState;
                m_Invocation = invocation;
                m_Fact = fact;
            }

            bool IsTimelineActive(string timelineId, string clipId)
            {
                var timelines = m_ControlState.TimelineSnapshots;
                for (int i = 0; i < timelines.Count; i++)
                {
                    AbilityTimelineRuntimeSnapshot timeline = timelines[i];
                    if (timeline.TimelineId != timelineId ||
                        (timeline.State != AbilityTimelineSnapshotState.Running && timeline.State != AbilityTimelineSnapshotState.Stopping))
                        continue;
                    if (clipId.Length == 0)
                        return true;
                    for (int c = 0; c < timeline.ActiveClipIds.Count; c++)
                        if (timeline.ActiveClipIds[c] == clipId)
                            return true;
                }
                return false;
            }

            public bool TryRead(
                string inputId,
                EventGraphValueKind expectedKind,
                out EventGraphValue value)
            {
                if (inputId == HasCameraInput)
                    return Read(EventGraphValueKind.Bool, expectedKind, EventGraphValue.FromBool(m_HasCamera), out value);
                if (m_ControlInputs.TryGetValue(inputId, out CharacterControlStateFieldId field))
                {
                    CharacterControlRuntimeState state = m_ControlState.ControlState;
                    return Read(EventGraphValueKind.String, expectedKind,
                        EventGraphValue.FromString(state.Values[state.Schema.RequireIndex(field)].Identity), out value);
                }
                if (m_TimelineInputs.TryGetValue(inputId, out var timeline))
                    return Read(EventGraphValueKind.Bool, expectedKind,
                        EventGraphValue.FromBool(IsTimelineActive(timeline.Timeline, timeline.Clip)), out value);
                if (m_AbilityInputs.TryGetValue(inputId, out CharacterSkillId ability))
                    return Read(EventGraphValueKind.Bool, expectedKind,
                        EventGraphValue.FromBool(m_ControlState.IsAbilityActive(ability)), out value);
                if (string.Equals(
                        inputId,
                        EventGraphHostInputIds.DeltaSeconds,
                        StringComparison.Ordinal))
                {
                    if (expectedKind == EventGraphValueKind.Float32)
                    {
                        value = EventGraphValue.FromFloat32(
                            m_Invocation.DeltaSeconds);
                        return true;
                    }
                    value = default;
                    return false;
                }
                if (string.Equals(
                        inputId,
                        CharacterPresentationFactSchema.Velocity.Value,
                        StringComparison.Ordinal))
                    return Read(
                        EventGraphValueKind.Vector3,
                        expectedKind,
                        EventGraphValue.FromVector3(m_Fact.Velocity),
                        out value);
                if (string.Equals(inputId, CharacterPresentationFactSchema.MovementMode.Value, StringComparison.Ordinal))
                    return Read(EventGraphValueKind.String, expectedKind,
                        EventGraphValue.FromString(m_Fact.MovementModeId), out value);
                if (string.Equals(
                        inputId,
                        CharacterPresentationFactSchema.Rotation.Value,
                        StringComparison.Ordinal))
                    return Read(
                        EventGraphValueKind.Quaternion,
                        expectedKind,
                        EventGraphValue.FromQuaternion(m_Fact.Rotation),
                        out value);
                if (string.Equals(
                        inputId,
                        CharacterPresentationFactSchema.Grounded.Value,
                        StringComparison.Ordinal))
                    return Read(
                        EventGraphValueKind.Bool,
                        expectedKind,
                        EventGraphValue.FromBool(m_Fact.Grounded),
                        out value);
                if (string.Equals(
                        inputId,
                        CharacterPresentationFactSchema.DesiredPlanarVelocity.Value,
                        StringComparison.Ordinal))
                    return Read(
                        EventGraphValueKind.Vector2,
                        expectedKind,
                        EventGraphValue.FromVector2(m_Fact.DesiredPlanarVelocity),
                        out value);
                if (string.Equals(
                        inputId,
                        CharacterPresentationFactSchema.DesiredFacing.Value,
                        StringComparison.Ordinal))
                    return Read(
                        EventGraphValueKind.Vector2,
                        expectedKind,
                        EventGraphValue.FromVector2(m_Fact.DesiredFacing),
                        out value);
                if (string.Equals(
                        inputId,
                        CharacterPresentationFactSchema.HasMotion.Value,
                        StringComparison.Ordinal))
                    return Read(
                        EventGraphValueKind.Bool,
                        expectedKind,
                        EventGraphValue.FromBool(m_Fact.HasMotion),
                        out value);
                if (string.Equals(
                        inputId,
                        CharacterPresentationFactSchema.LocomotionPlanarBasis.Value,
                        StringComparison.Ordinal))
                    return Read(
                        EventGraphValueKind.Vector2,
                        expectedKind,
                        EventGraphValue.FromVector2(m_Fact.LocomotionPlanarBasis),
                        out value);
                value = default;
                return false;
            }

            static bool Read(
                EventGraphValueKind actualKind,
                EventGraphValueKind expectedKind,
                EventGraphValue actual,
                out EventGraphValue value)
            {
                value = actualKind == expectedKind ? actual : default;
                return actualKind == expectedKind;
            }
        }
    }
}
