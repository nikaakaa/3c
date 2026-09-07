using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using CoreTimelinePlaybackStatus = ThirdPersonSimulation.TimelinePlaybackStatus;
using FixedProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using FixedProgramConstant = ThirdPersonSimulation.Fixed.ProgramConstant;
using FixedProgramCurve = ThirdPersonSimulation.Fixed.ProgramCurve;
using FixedProgramCurveCodec = ThirdPersonSimulation.Fixed.ProgramCurveCodec;

namespace BTSMTL.Timeline.Runtime
{
    public enum FixedTimelinePlaybackStatus : byte
    {
        Unprepared = 0,
        Prepared = 1,
        Running = 2,
        Stopping = 3,
        Completed = 4,
        Stopped = 5,
        Failed = 6,
        Disposed = 7
    }

    public readonly struct FixedTimelineCall
    {
        public FixedTimelineCall(string ownerIdentity, string callIdentity, ulong instanceId)
        {
            Identity = new TimelineExecutionIdentity(ownerIdentity, callIdentity, instanceId);
        }

        public TimelineExecutionIdentity Identity { get; }
    }

    public readonly struct FixedTimelineTargetBinding
    {
        public FixedTimelineTargetBinding(string bindingId, ITimelineScenePresentationSink target)
        {
            BindingId = TimelineRuntimeIdentityValidation.Require(bindingId, nameof(bindingId));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            if (string.IsNullOrWhiteSpace(Target.TargetIdentity))
                throw new ArgumentException("Timeline target identity is required.", nameof(target));
        }

        public string BindingId { get; }
        public ITimelineScenePresentationSink Target { get; }
    }

    public sealed class FixedTimelineBindingSet
    {
        readonly FixedTimelineTargetBinding[] m_Targets;
        readonly List<ActiveBinding> m_Active = new List<ActiveBinding>();

        public FixedTimelineBindingSet(IEnumerable<FixedTimelineTargetBinding> targets)
        {
            m_Targets = (targets ?? Array.Empty<FixedTimelineTargetBinding>()).ToArray();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Targets.Length; i++)
            {
                if (!ids.Add(m_Targets[i].BindingId))
                    throw new ArgumentException($"Timeline target binding '{m_Targets[i].BindingId}' is duplicated.", nameof(targets));
            }
        }

        public IReadOnlyList<FixedTimelineTargetBinding> Targets => m_Targets;

        internal FixedTimelineBindingPlan Acquire(
            FixedTimelineProgramPlan program,
            IEnumerable<TimelineCallBinding> callBindings,
            TimelineExecutionIdentity identity)
        {
            var values = new Dictionary<string, TimelineBindingValue>(StringComparer.Ordinal);
            foreach (TimelineCallBinding binding in callBindings ?? Array.Empty<TimelineCallBinding>())
            {
                if (!program.TryGetBinding(binding.BindingId, out FixedTimelineRuntimeBinding declaration))
                    throw new InvalidOperationException($"Timeline call binding '{binding.BindingId}' is not declared by the compiled Program.");
                if (declaration.Access != TimelineBindingAccess.Input || declaration.Lifetime != TimelineBindingLifetime.Call)
                    throw new InvalidOperationException($"Timeline binding '{binding.BindingId}' is not a call input.");
                if (!binding.Value.Matches(declaration.ToDeclaration()))
                    throw new InvalidOperationException($"Timeline call binding '{binding.BindingId}' has value kind '{binding.Value.ValueKind}', expected '{declaration.ValueKind}'.");
                if (!values.TryAdd(binding.BindingId, binding.Value))
                    throw new InvalidOperationException($"Timeline call binding '{binding.BindingId}' is assigned more than once.");
            }

            foreach (FixedTimelineRuntimeBinding declaration in program.Bindings)
            {
                if (declaration.Access == TimelineBindingAccess.Input && !values.ContainsKey(declaration.BindingId))
                    throw new InvalidOperationException($"Timeline call binding '{declaration.BindingId}' is required.");
            }

            var clips = new FixedTimelineBoundClip[program.Program.Operations.Count];
            var keys = new List<string>();
            for (int i = 0; i < program.SceneClips.Count; i++)
            {
                FixedTimelineSceneClip clip = program.SceneClips[i];
                if (!values.TryGetValue(clip.TargetBindingId, out TimelineBindingValue targetValue) ||
                    targetValue.ValueKind != TimelineBindingValueKind.Target)
                    throw new InvalidOperationException($"Timeline clip '{clip.Operation}' target binding '{clip.TargetBindingId}' is not provided.");
                FixedTimelineTargetBinding target = FindTarget(clip.TargetBindingId);
                if (!string.Equals(target.Target.TargetIdentity, targetValue.TargetIdentity, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Timeline binding '{clip.TargetBindingId}' resolved target '{targetValue.TargetIdentity}', but supplied target is '{target.Target.TargetIdentity}'.");
                if (!program.TryGetBinding(clip.ParameterBindingId, out FixedTimelineRuntimeBinding parameter) ||
                    parameter.Access != TimelineBindingAccess.Write ||
                    parameter.Lifetime != TimelineBindingLifetime.Tick)
                    throw new InvalidOperationException($"Timeline clip '{clip.Operation}' parameter binding '{clip.ParameterBindingId}' is not a writable tick binding.");
                if (!string.IsNullOrEmpty(parameter.TargetBindingId) &&
                    !string.Equals(parameter.TargetBindingId, clip.TargetBindingId, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Timeline clip '{clip.Operation}' parameter binding '{clip.ParameterBindingId}' targets '{parameter.TargetBindingId}', not '{clip.TargetBindingId}'.");
                string key = target.Target.TargetIdentity + "\u001f" + parameter.ParameterId;
                if (keys.Contains(key, StringComparer.Ordinal))
                    throw new InvalidOperationException($"Timeline call writes target '{target.Target.TargetIdentity}' parameter '{parameter.ParameterId}' more than once.");
                for (int activeIndex = 0; activeIndex < m_Active.Count; activeIndex++)
                {
                    if (string.Equals(m_Active[activeIndex].Key, key, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline target '{target.Target.TargetIdentity}' parameter '{parameter.ParameterId}' is already occupied by call '{m_Active[activeIndex].Identity.CallIdentity}'.");
                }
                keys.Add(key);
                clips[clip.Operation.Value] = new FixedTimelineBoundClip(
                    target.Target,
                    parameter.ParameterId,
                    parameter.ValueKind == TimelineBindingValueKind.Boolean);
            }
            for (int i = 0; i < keys.Count; i++)
                m_Active.Add(new ActiveBinding(keys[i], identity));
            return new FixedTimelineBindingPlan(this, clips, keys, identity);
        }

        internal void Release(FixedTimelineBindingPlan plan)
        {
            if (plan == null)
                return;
            for (int keyIndex = 0; keyIndex < plan.Keys.Count; keyIndex++)
            {
                for (int activeIndex = m_Active.Count - 1; activeIndex >= 0; activeIndex--)
                {
                    ActiveBinding active = m_Active[activeIndex];
                    if (string.Equals(active.Key, plan.Keys[keyIndex], StringComparison.Ordinal) &&
                        active.Identity == plan.Identity)
                    {
                        m_Active.RemoveAt(activeIndex);
                        break;
                    }
                }
            }
        }

        FixedTimelineTargetBinding FindTarget(string bindingId)
        {
            for (int i = 0; i < m_Targets.Length; i++)
                if (string.Equals(m_Targets[i].BindingId, bindingId, StringComparison.Ordinal))
                    return m_Targets[i];
            throw new InvalidOperationException($"Timeline target binding '{bindingId}' is not supplied.");
        }

        sealed class ActiveBinding
        {
            public ActiveBinding(string key, TimelineExecutionIdentity identity)
            {
                Key = key;
                Identity = identity;
            }

            public string Key { get; }
            public TimelineExecutionIdentity Identity { get; }
        }
    }

    public sealed class FixedTimelinePlayback : IDisposable
    {
        readonly FixedTimelineProgramPlan m_Program;
        readonly FixedTimelineState m_State;
        readonly FixedTimelineExecutionTarget m_Target;
        readonly FixedTimelineExecutionStatePort m_StatePort;
        readonly TimelineControlRuntime<FixedTimelineOperationTarget, FixedScalar> m_Timeline;
        readonly OperationControlRuntime<FixedTimelineOperationTarget> m_Control;
        FixedTimelineBindingSet m_Bindings;
        FixedTimelineBindingPlan m_BindingPlan;
        FixedTimelinePlaybackStatus m_Status = FixedTimelinePlaybackStatus.Unprepared;
        bool m_Disposed;

        public FixedTimelinePlayback(
            FixedProgram program,
            ITimelineStandaloneTraceSink trace = null)
        {
            m_Program = FixedTimelineProgramPlan.Create(program);
            m_State = new FixedTimelineState(m_Program.Program);
            m_Target = new FixedTimelineExecutionTarget(m_Program, m_State, trace);
            m_StatePort = new FixedTimelineExecutionStatePort(m_Program.Topology, m_State);
            m_Timeline = new TimelineControlRuntime<FixedTimelineOperationTarget, FixedScalar>(
                m_StatePort,
                m_Target,
                new NestedExecutionWorkspaceBuffer<TimelineSegment<FixedScalar>>());
            m_Target.Attach(m_Timeline);
            m_Control = new OperationControlRuntime<FixedTimelineOperationTarget>(
                m_Program.Topology,
                new FixedTimelineOperationTarget(m_Target),
                checked(Math.Max(256, m_Program.Program.Operations.Count * 128)));
        }

        public FixedProgram Program => m_Program.Program;
        public FixedTimelinePlaybackStatus Status => m_Disposed ? FixedTimelinePlaybackStatus.Disposed : m_Status;
        public TimelineExecutionIdentity ExecutionIdentity => m_Target.ExecutionIdentity;
        public TimelinePlaybackObservation Observation => new TimelinePlaybackObservation(
            m_Program.Program.Manifest.Root.Kind,
            m_Program.Program.Manifest.Root.RootIdentity,
            m_Program.Program.Manifest.Root.EntryIdentity,
            m_Program.Program.Manifest.Root.ContentIdentity,
            m_Program.Program.Manifest.SourceRevision.Value,
            m_Program.Program.ProgramHash.ToString(),
            m_Program.Program.LayoutHash.ToString(),
            ToObservationState(Status),
            m_Target.ExecutionIdentity,
            m_Program.Bindings.Select(value => value.BindingId));

        public void Prepare(
            FixedTimelineBindingSet bindings,
            IEnumerable<TimelineCallBinding> callBindings,
            FixedTimelineCall call)
        {
            RequireNotDisposed();
            if (m_Status != FixedTimelinePlaybackStatus.Unprepared)
                throw new InvalidOperationException("Timeline can only be prepared once.");
            if (!call.Identity.IsValid)
                throw new ArgumentException("Timeline call identity is invalid.", nameof(call));
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            m_BindingPlan = m_Bindings.Acquire(m_Program, callBindings, call.Identity);
            m_Target.Bind(call.Identity, m_BindingPlan);
            m_Status = FixedTimelinePlaybackStatus.Prepared;
        }

        public void Start()
        {
            RequireNotDisposed();
            if (m_Status != FixedTimelinePlaybackStatus.Prepared)
                throw new InvalidOperationException("Timeline must be prepared before it starts.");
            m_Status = FixedTimelinePlaybackStatus.Running;
        }

        public void Advance(FixedScalar delta)
        {
            RequireNotDisposed();
            if (m_Status == FixedTimelinePlaybackStatus.Prepared)
                throw new InvalidOperationException("Timeline must be started before it advances.");
            if (m_Status != FixedTimelinePlaybackStatus.Running && m_Status != FixedTimelinePlaybackStatus.Stopping)
                return;

            m_Target.SetTickDelta(delta);
            m_Target.BeginFrame();
            try
            {
                m_Control.BeginEvaluation();
                if (m_Status == FixedTimelinePlaybackStatus.Stopping || m_Control.IsStopping(m_Program.RootOperation))
                {
                    OperationStopStatus stop = m_Control.ContinueStop(m_Program.RootOperation);
                    if (stop == OperationStopStatus.Failed)
                    {
                        m_Target.DiscardFrame();
                        Fail();
                    }
                    else if (stop == OperationStopStatus.Completed)
                    {
                        m_Target.CommitFrame();
                        StopAndRelease();
                    }
                    else
                        m_Target.CommitFrame();
                    return;
                }
                m_Timeline.PrepareDecisionTimelines(m_Control.Cursor);
                OperationExecutionResult result = m_Control.Cursor.Tick(m_Program.RootOperation);
                switch (result)
                {
                    case OperationExecutionResult.Running:
                        m_Target.CommitFrame();
                        m_Status = FixedTimelinePlaybackStatus.Running;
                        break;
                    case OperationExecutionResult.Success:
                        m_Target.CommitFrame();
                        m_Status = FixedTimelinePlaybackStatus.Completed;
                        ReleaseBindings();
                        break;
                    default:
                        m_Target.DiscardFrame();
                        Fail();
                        break;
                }
            }
            catch
            {
                m_Target.DiscardFrame();
                Fail();
                throw;
            }
        }

        public void Stop()
        {
            RequireNotDisposed();
            if (m_Status == FixedTimelinePlaybackStatus.Prepared)
            {
                StopAndRelease();
                return;
            }
            if (m_Status != FixedTimelinePlaybackStatus.Running && m_Status != FixedTimelinePlaybackStatus.Stopping)
                return;
            m_Target.BeginFrame();
            try
            {
                m_Control.BeginEvaluation();
                OperationStopStatus stop = m_Control.RequestStop(
                    m_Program.RootOperation,
                    OperationStopContext.Shutdown(m_Program.RootOperation));
                if (stop == OperationStopStatus.Failed)
                {
                    m_Target.DiscardFrame();
                    Fail();
                }
                else if (stop == OperationStopStatus.Completed)
                {
                    m_Target.CommitFrame();
                    StopAndRelease();
                }
                else
                {
                    m_Target.CommitFrame();
                    m_Status = FixedTimelinePlaybackStatus.Stopping;
                }
            }
            catch
            {
                m_Target.DiscardFrame();
                Fail();
                throw;
            }
        }

        public void ForceStop()
        {
            RequireNotDisposed();
            if (m_Status != FixedTimelinePlaybackStatus.Prepared &&
                m_Status != FixedTimelinePlaybackStatus.Running &&
                m_Status != FixedTimelinePlaybackStatus.Stopping)
                return;
            m_Target.BeginFrame();
            try
            {
                m_Control.BeginEvaluation();
                m_Control.ForceStop(m_Program.RootOperation, OperationStopContext.Shutdown(m_Program.RootOperation));
                m_Target.CommitFrame();
                StopAndRelease();
            }
            catch
            {
                m_Target.DiscardFrame();
                Fail();
                throw;
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            if (m_Status == FixedTimelinePlaybackStatus.Prepared ||
                m_Status == FixedTimelinePlaybackStatus.Running ||
                m_Status == FixedTimelinePlaybackStatus.Stopping)
                ForceStop();
            else
                ReleaseBindings();
            m_Disposed = true;
            m_Status = FixedTimelinePlaybackStatus.Disposed;
        }

        void Fail()
        {
            m_Status = FixedTimelinePlaybackStatus.Failed;
            ReleaseBindings();
        }

        void StopAndRelease()
        {
            m_Status = FixedTimelinePlaybackStatus.Stopped;
            ReleaseBindings();
        }

        void ReleaseBindings()
        {
            if (m_Bindings == null || m_BindingPlan == null)
                return;
            m_Bindings.Release(m_BindingPlan);
            m_BindingPlan = null;
            m_Bindings = null;
            m_Target.ClearBindings();
        }

        void RequireNotDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(FixedTimelinePlayback));
        }

        static TimelineRuntimePlaybackState ToObservationState(FixedTimelinePlaybackStatus status)
        {
            return (TimelineRuntimePlaybackState)(byte)status;
        }
    }

    sealed class FixedTimelineProgramPlan
    {
        readonly Dictionary<string, FixedTimelineRuntimeBinding> m_Bindings;
        readonly Dictionary<int, FixedTimelineSceneClip> m_SceneClipsByOperation;

        FixedTimelineProgramPlan(
            FixedProgram program,
            OperationExecutionTopology topology,
            OperationHandle rootOperation,
            OperationHandle timelineOperation,
            IEnumerable<FixedTimelineRuntimeBinding> bindings,
            IEnumerable<FixedTimelineSceneClip> sceneClips)
        {
            Program = program;
            Topology = topology;
            RootOperation = rootOperation;
            TimelineOperation = timelineOperation;
            m_Bindings = (bindings ?? Array.Empty<FixedTimelineRuntimeBinding>()).ToDictionary(value => value.BindingId, StringComparer.Ordinal);
            SceneClips = (sceneClips ?? Array.Empty<FixedTimelineSceneClip>()).OrderBy(value => value.Operation.Value).ToArray();
            m_SceneClipsByOperation = SceneClips.ToDictionary(value => value.Operation.Value);
        }

        public FixedProgram Program { get; }
        public OperationExecutionTopology Topology { get; }
        public OperationHandle RootOperation { get; }
        public OperationHandle TimelineOperation { get; }
        public IReadOnlyList<FixedTimelineRuntimeBinding> Bindings => m_Bindings.Values.OrderBy(value => value.BindingId, StringComparer.Ordinal).ToArray();
        public IReadOnlyList<FixedTimelineSceneClip> SceneClips { get; }

        public bool TryGetBinding(string id, out FixedTimelineRuntimeBinding binding) => m_Bindings.TryGetValue(id ?? string.Empty, out binding);
        public bool TryGetSceneClip(OperationHandle operation, out FixedTimelineSceneClip clip) => m_SceneClipsByOperation.TryGetValue(operation.Value, out clip);

        public static FixedTimelineProgramPlan Create(FixedProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (!program.Manifest.Root.IsTimeline)
                throw new InvalidOperationException("Fixed Timeline playback requires a Timeline Program root.");
            OperationExecutionTopology topology = CreateTopology(program);
            OperationHandle rootOperation = FindRootOperation(program);
            IReadOnlyList<ProgramControlFlowEdge> rootChildren = topology.Outgoing(rootOperation, ProgramControlFlowKind.Child);
            if (rootChildren.Count != 1 || topology.Operation(rootChildren[0].Target).Code != SimulationOperationCode.Timeline)
                throw new InvalidDataException("Timeline Program root must contain exactly one Timeline operation.");
            OperationHandle timelineOperation = rootChildren[0].Target;

            var bindings = new List<FixedTimelineRuntimeBinding>();
            for (int i = 0; i < program.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = program.CatalogEntries[i];
                if (entry.Kind != ProgramCatalogEntryKind.TimelineBinding)
                    continue;
                bindings.Add(FixedTimelineRuntimeBinding.Read(entry, program));
            }
            var clips = new List<FixedTimelineSceneClip>();
            for (int i = 0; i < program.Operations.Count; i++)
            {
                ThirdPersonSimulation.Fixed.SimulationOperation operation = program.Operations[i];
                if (operation.Code != SimulationOperationCode.TimelineScenePresentationParameter)
                    continue;
                ProgramCatalogEntry entry = FindCatalog(program, operation, ProgramCatalogEntryKind.TimelineClip);
                clips.Add(new FixedTimelineSceneClip(
                    operation.Handle,
                    Identity(entry, "TargetBinding"),
                    Identity(entry, "ParameterBinding")));
            }
            return new FixedTimelineProgramPlan(program, topology, rootOperation, timelineOperation, bindings, clips);
        }

        static OperationExecutionTopology CreateTopology(FixedProgram program)
        {
            var operations = new OperationExecutionDescriptor[program.Operations.Count];
            for (int i = 0; i < operations.Length; i++)
            {
                ThirdPersonSimulation.Fixed.SimulationOperation operation = program.Operations[i];
                operations[i] = new OperationExecutionDescriptor(
                    operation.Handle,
                    operation.Code,
                    operation.Integer0,
                    operation.Integer1,
                    operation.Unsigned0,
                    operation.Text0,
                    operation.Flags,
                    operation.StateSlots);
            }
            return new OperationExecutionTopology(
                operations,
                program.ControlFlow,
                program.References,
                program.StateSlots,
                program.SourceMap,
                FindRootOperation(program));
        }

        static OperationHandle FindRootOperation(FixedProgram program)
        {
            ProgramReference[] roots = program.References
                .Where(value => value.Kind == ProgramReferenceKind.Operation && !value.HasSourceOperation)
                .ToArray();
            if (roots.Length != 1)
                throw new InvalidDataException($"Timeline Program must declare exactly one root operation reference, found {roots.Length}.");
            OperationHandle root = new OperationHandle(roots[0].TargetIndex);
            if (!root.IsValid || program.Operations[root.Value].Code != SimulationOperationCode.Root)
                throw new InvalidDataException("Timeline Program root reference does not target a Root operation.");
            return root;
        }

        static ProgramCatalogEntry FindCatalog(
            FixedProgram program,
            ThirdPersonSimulation.Fixed.SimulationOperation operation,
            ProgramCatalogEntryKind kind)
        {
            for (int i = 0; i < program.References.Count; i++)
            {
                ProgramReference reference = program.References[i];
                if (!reference.HasSourceOperation ||
                    !reference.SourceOperation.Equals(operation.Handle) ||
                    reference.Kind != ProgramReferenceKind.CatalogEntry ||
                    program.CatalogEntries[reference.TargetIndex].Kind != kind)
                    continue;
                return program.CatalogEntries[reference.TargetIndex];
            }
            throw new InvalidDataException($"Fixed Timeline operation '{operation.Handle}' has no '{kind}' catalog entry.");
        }

        static string Identity(ProgramCatalogEntry entry, string name)
        {
            ProgramCatalogField field = entry.Fields.SingleOrDefault(value => string.Equals(value.Name, name, StringComparison.Ordinal));
            if (field == null || field.Kind != ProgramCatalogFieldKind.Identity)
                throw new InvalidDataException($"Timeline catalog '{entry.Identity}' has no identity field '{name}'.");
            return field.Identity;
        }
    }

    sealed class FixedTimelineRuntimeBinding
    {
        public FixedTimelineRuntimeBinding(
            string bindingId,
            string domain,
            string parameterId,
            TimelineBindingValueKind valueKind,
            TimelineBindingAccess access,
            TimelineBindingLifetime lifetime,
            string targetBindingId)
        {
            BindingId = TimelineRuntimeIdentityValidation.Require(bindingId, nameof(bindingId));
            Domain = TimelineRuntimeIdentityValidation.Require(domain, nameof(domain));
            ParameterId = parameterId ?? string.Empty;
            TargetBindingId = targetBindingId ?? string.Empty;
            ValueKind = valueKind;
            Access = access;
            Lifetime = lifetime;
        }

        public string BindingId { get; }
        public string Domain { get; }
        public string ParameterId { get; }
        public string TargetBindingId { get; }
        public TimelineBindingValueKind ValueKind { get; }
        public TimelineBindingAccess Access { get; }
        public TimelineBindingLifetime Lifetime { get; }

        public TimelineBindingDeclaration ToDeclaration() =>
            new TimelineBindingDeclaration(BindingId, Domain, ParameterId, ValueKind, Access, Lifetime);

        public static FixedTimelineRuntimeBinding Read(
            ProgramCatalogEntry entry,
            FixedProgram program)
        {
            return new FixedTimelineRuntimeBinding(
                ReadBindingId(entry),
                StringValue(entry, program, "Domain"),
                StringValue(entry, program, "ParameterId"),
                (TimelineBindingValueKind)IntValue(entry, program, "ValueKind"),
                (TimelineBindingAccess)IntValue(entry, program, "Access"),
                (TimelineBindingLifetime)IntValue(entry, program, "Lifetime"),
                OptionalIdentity(entry, "TargetBinding"));
        }

        static string ReadBindingId(ProgramCatalogEntry entry)
        {
            const string marker = "/binding:";
            int index = entry.Identity.LastIndexOf(marker, StringComparison.Ordinal);
            return index < 0 ? throw new InvalidDataException($"Timeline binding catalog '{entry.Identity}' is malformed.") : entry.Identity.Substring(index + marker.Length);
        }

        static string StringValue(ProgramCatalogEntry entry, FixedProgram program, string name)
        {
            FixedProgramConstant constant = Constant(entry, program, name);
            if (constant.Kind != ThirdPersonSimulation.Fixed.ProgramConstantKind.String)
                throw new InvalidDataException($"Timeline binding '{entry.Identity}' field '{name}' is not a string.");
            return constant.Text;
        }

        static string OptionalIdentity(ProgramCatalogEntry entry, string name)
        {
            ProgramCatalogField field = entry.Fields.SingleOrDefault(value => string.Equals(value.Name, name, StringComparison.Ordinal));
            return field == null
                ? string.Empty
                : field.Kind == ProgramCatalogFieldKind.Identity
                    ? field.Identity
                    : throw new InvalidDataException($"Timeline binding '{entry.Identity}' field '{name}' is not an identity.");
        }

        static int IntValue(ProgramCatalogEntry entry, FixedProgram program, string name)
        {
            FixedProgramConstant constant = Constant(entry, program, name);
            if (constant.Kind != ThirdPersonSimulation.Fixed.ProgramConstantKind.Int32)
                throw new InvalidDataException($"Timeline binding '{entry.Identity}' field '{name}' is not an Int32.");
            return constant.Int32;
        }

        static FixedProgramConstant Constant(ProgramCatalogEntry entry, FixedProgram program, string name)
        {
            ProgramCatalogField field = entry.Fields.SingleOrDefault(value => string.Equals(value.Name, name, StringComparison.Ordinal));
            if (field == null || field.Kind != ProgramCatalogFieldKind.Constant || field.ConstantIndex < 0 || field.ConstantIndex >= program.Constants.Count)
                throw new InvalidDataException($"Timeline binding '{entry.Identity}' has no constant field '{name}'.");
            return program.Constants[field.ConstantIndex];
        }
    }

    sealed class FixedTimelineSceneClip
    {
        public FixedTimelineSceneClip(OperationHandle operation, string targetBindingId, string parameterBindingId)
        {
            Operation = operation;
            TargetBindingId = TimelineRuntimeIdentityValidation.Require(targetBindingId, nameof(targetBindingId));
            ParameterBindingId = TimelineRuntimeIdentityValidation.Require(parameterBindingId, nameof(parameterBindingId));
        }

        public OperationHandle Operation { get; }
        public string TargetBindingId { get; }
        public string ParameterBindingId { get; }
    }

    sealed class FixedTimelineBindingPlan
    {
        public FixedTimelineBindingPlan(
            FixedTimelineBindingSet owner,
            FixedTimelineBoundClip[] clips,
            IReadOnlyList<string> keys,
            TimelineExecutionIdentity identity)
        {
            Owner = owner;
            Clips = clips;
            Keys = keys.ToArray();
            Identity = identity;
        }

        public FixedTimelineBindingSet Owner { get; }
        public FixedTimelineBoundClip[] Clips { get; }
        public IReadOnlyList<string> Keys { get; }
        public TimelineExecutionIdentity Identity { get; }
    }

    sealed class FixedTimelineBoundClip
    {
        public FixedTimelineBoundClip(
            ITimelineScenePresentationSink sink,
            string parameterId,
            bool boolean)
        {
            Sink = sink;
            ParameterId = parameterId;
            Boolean = boolean;
        }

        public ITimelineScenePresentationSink Sink { get; }
        public string ParameterId { get; }
        public bool Boolean { get; }
    }

    sealed class FixedTimelineState
    {
        readonly FixedProgram m_Program;
        readonly int[] m_Int32;
        readonly ulong[] m_UInt64;
        readonly bool[] m_Boolean;
        readonly FixedScalar[] m_Scalar;
        readonly string[] m_Identity;
        readonly TimelineActionContextIdentity[] m_RetainedActionContext;

        public FixedTimelineState(FixedProgram program)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            int count = program.StateSlots.Count;
            m_Int32 = new int[count];
            m_UInt64 = new ulong[count];
            m_Boolean = new bool[count];
            m_Scalar = new FixedScalar[count];
            m_Identity = new string[count];
            m_RetainedActionContext = new TimelineActionContextIdentity[program.Operations.Count];
            for (int i = 0; i < count; i++)
                ResetSlot(i);
        }

        public int ReadInt32(int slot) => m_Int32[slot];
        public void WriteInt32(int slot, int value) => m_Int32[slot] = value;
        public ulong ReadUInt64(int slot) => m_UInt64[slot];
        public void WriteUInt64(int slot, ulong value) => m_UInt64[slot] = value;
        public bool ReadBoolean(int slot) => m_Boolean[slot];
        public void WriteBoolean(int slot, bool value) => m_Boolean[slot] = value;
        public FixedScalar ReadScalar(int slot) => m_Scalar[slot];
        public void WriteScalar(int slot, FixedScalar value) => m_Scalar[slot] = value;
        public string ReadIdentity(int slot) => m_Identity[slot] ?? string.Empty;
        public void WriteIdentity(int slot, string value) => m_Identity[slot] = value ?? string.Empty;
        public TimelineActionContextIdentity ReadRetainedActionContext(OperationHandle operation) => m_RetainedActionContext[operation.Value];
        public void WriteRetainedActionContext(OperationHandle operation, TimelineActionContextIdentity identity) => m_RetainedActionContext[operation.Value] = identity;

        public void ResetOperationState(OperationExecutionDescriptor operation)
        {
            for (int i = 0; i < operation.StateSlots.Count; i++)
            {
                int slot = operation.StateSlots[i];
                if (m_Program.StateSlots[slot].Semantic == ProgramStateSemantic.RunnableActivationGeneration)
                    continue;
                ResetSlot(slot);
            }
            m_RetainedActionContext[operation.Handle.Value] = default;
        }

        void ResetSlot(int slot)
        {
            ProgramStateSlot declaration = m_Program.StateSlots[slot];
            FixedProgramConstant constant = declaration.DefaultConstantIndex >= 0
                ? m_Program.Constants[declaration.DefaultConstantIndex]
                : null;
            switch (declaration.ValueKind)
            {
                case ProgramStateValueKind.Int32:
                    m_Int32[slot] = constant?.Int32 ?? 0;
                    break;
                case ProgramStateValueKind.UInt64:
                    m_UInt64[slot] = constant?.UInt64 ?? 0UL;
                    break;
                case ProgramStateValueKind.Boolean:
                    m_Boolean[slot] = constant?.Boolean ?? false;
                    break;
                case ProgramStateValueKind.Scalar:
                    m_Scalar[slot] = constant?.Scalar ?? FixedScalar.Zero;
                    break;
                case ProgramStateValueKind.Identity:
                    m_Identity[slot] = constant?.Text ?? string.Empty;
                    break;
            }
        }
    }

    sealed class FixedTimelineExecutionStatePort : ITimelineControlStatePort
    {
        readonly OperationExecutionTopology m_Topology;
        readonly FixedTimelineState m_State;

        public FixedTimelineExecutionStatePort(OperationExecutionTopology topology, FixedTimelineState state)
        {
            m_Topology = topology ?? throw new ArgumentNullException(nameof(topology));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
        }

        public CoreTimelinePlaybackStatus ReadPlayback(OperationHandle operation) =>
            (CoreTimelinePlaybackStatus)m_State.ReadInt32(Require(operation, ProgramStateSemantic.TimelinePlayback));

        public bool TryReadPlayback(OperationHandle operation, out CoreTimelinePlaybackStatus status)
        {
            int slot = Find(operation, ProgramStateSemantic.TimelinePlayback);
            status = slot < 0 ? CoreTimelinePlaybackStatus.Dormant : (CoreTimelinePlaybackStatus)m_State.ReadInt32(slot);
            return slot >= 0;
        }

        public void WritePlayback(OperationHandle operation, CoreTimelinePlaybackStatus status) =>
            m_State.WriteInt32(Require(operation, ProgramStateSemantic.TimelinePlayback), (int)status);

        public TimelineTreeClipStatus ReadTreeClipStatus(OperationHandle operation) =>
            (TimelineTreeClipStatus)m_State.ReadInt32(Require(operation, ProgramStateSemantic.TimelinePlayback));

        public bool TryReadTreeClipStatus(OperationHandle operation, out TimelineTreeClipStatus status)
        {
            int slot = Find(operation, ProgramStateSemantic.TimelinePlayback);
            status = slot < 0 ? TimelineTreeClipStatus.Dormant : (TimelineTreeClipStatus)m_State.ReadInt32(slot);
            return slot >= 0;
        }

        public void WriteTreeClipStatus(OperationHandle operation, TimelineTreeClipStatus status) =>
            m_State.WriteInt32(Require(operation, ProgramStateSemantic.TimelinePlayback), (int)status);

        public bool ReadLoop(OperationHandle operation) => m_State.ReadBoolean(Require(operation, ProgramStateSemantic.TimelineLoop));
        public void WriteLoop(OperationHandle operation, bool loop) => m_State.WriteBoolean(Require(operation, ProgramStateSemantic.TimelineLoop), loop);
        public int ReadCycle(OperationHandle operation) => m_State.ReadInt32(Require(operation, ProgramStateSemantic.TimelineTreeClipCycle));

        public bool TryReadCycle(OperationHandle operation, out int cycle)
        {
            int slot = Find(operation, ProgramStateSemantic.TimelineTreeClipCycle);
            cycle = slot < 0 ? 0 : m_State.ReadInt32(slot);
            return slot >= 0;
        }

        public void WriteCycle(OperationHandle operation, int cycle)
        {
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            m_State.WriteInt32(Require(operation, ProgramStateSemantic.TimelineTreeClipCycle), cycle);
        }

        public TimelineActionContextIdentity ReadRetainedActionContext(OperationHandle operation) => m_State.ReadRetainedActionContext(operation);
        public void WriteRetainedActionContext(OperationHandle operation, TimelineActionContextIdentity identity) => m_State.WriteRetainedActionContext(operation, identity);
        int Find(OperationHandle operation, ProgramStateSemantic semantic) => m_Topology.FindOperationStateSlot(operation, semantic);
        int Require(OperationHandle operation, ProgramStateSemantic semantic) => m_Topology.RequireOperationStateSlot(operation, semantic);
    }

    sealed class FixedTimelineExecutionTarget : ITimelineTargetLeaf<FixedScalar>
    {
        readonly FixedTimelineProgramPlan m_Program;
        readonly FixedTimelineState m_State;
        readonly ITimelineStandaloneTraceSink m_Trace;
        readonly Dictionary<(int Operation, string Port), (OperationHandle Source, string Port)> m_ValueSources =
            new Dictionary<(int Operation, string Port), (OperationHandle Source, string Port)>();
        readonly Dictionary<(int Operation, string Port), int> m_ConstantInputs =
            new Dictionary<(int Operation, string Port), int>();
        readonly HashSet<(int Operation, string Port)> m_ValueStack = new HashSet<(int Operation, string Port)>();
        readonly List<TimelineScenePresentationSample> m_PendingSceneSamples = new List<TimelineScenePresentationSample>();
        FixedTimelineBindingPlan m_Bindings;
        TimelineExecutionIdentity m_Identity;
        TimelineActionContextIdentity m_ActionContext;
        FixedScalar m_TickDelta;
        TimelineControlRuntime<FixedTimelineOperationTarget, FixedScalar> m_Timeline;

        public FixedTimelineExecutionTarget(
            FixedTimelineProgramPlan program,
            FixedTimelineState state,
            ITimelineStandaloneTraceSink trace)
        {
            m_Program = program ?? throw new ArgumentNullException(nameof(program));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Trace = trace;
            for (int i = 0; i < program.Program.ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge edge = program.Program.ControlFlow[i];
                if (edge.Kind == ProgramControlFlowKind.Value)
                    m_ValueSources[(edge.Target.Value, edge.TargetPort)] = (edge.Source, edge.SourcePort);
            }
            for (int i = 0; i < program.Program.ConstantInputBindings.Count; i++)
            {
                ProgramConstantInputBinding binding = program.Program.ConstantInputBindings[i];
                m_ConstantInputs[(binding.TargetOperation.Value, binding.TargetPort)] = binding.ConstantIndex;
            }
        }

        public TimelineExecutionIdentity ExecutionIdentity => m_Identity;
        public bool DiagnosticsEnabled => m_Trace != null && m_Trace.Enabled;
        public FixedScalar Zero => FixedScalar.Zero;
        public FixedScalar One => FixedScalar.One;
        public FixedScalar TickDelta => m_TickDelta;
        public FixedScalar Epsilon => FixedScalar.FromRatio(1, 1000000);
        public int TimelineOperationCount => m_Program.Topology.TimelineOperationCount;
        public int Compare(FixedScalar left, FixedScalar right) => left.CompareTo(right);
        public FixedScalar FromInt32(int value) => FixedScalar.FromInt64(value);
        public FixedScalar Add(FixedScalar left, FixedScalar right) => left + right;
        public FixedScalar Subtract(FixedScalar left, FixedScalar right) => left - right;
        public FixedScalar Multiply(FixedScalar left, FixedScalar right) => left * right;
        public FixedScalar Divide(FixedScalar left, FixedScalar right) => left / right;
        public FixedScalar Min(FixedScalar left, FixedScalar right) => FixedScalar.Min(left, right);
        public FixedScalar Max(FixedScalar left, FixedScalar right) => FixedScalar.Max(left, right);
        public FixedScalar Clamp(FixedScalar value, FixedScalar minimum, FixedScalar maximum) => FixedScalar.Clamp(value, minimum, maximum);
        public string Format(FixedScalar value) => value.ToString();
        public OperationExecutionDescriptor TimelineOperationAt(int index) => m_Program.Topology.TimelineOperationAt(index);
        public OperationHandle TimelineOwner(OperationHandle child) => m_Program.Topology.TimelineOwner(child);
        public OperationExecutionDescriptor Operation(OperationHandle operation) => m_Program.Topology.Operation(operation);
        public IReadOnlyList<ProgramControlFlowEdge> Edges(OperationHandle source, ProgramControlFlowKind kind) => m_Program.Topology.Outgoing(source, kind);
        public string SourcePath(OperationHandle operation) => $"{m_Program.Program.Manifest.ProgramId.Value}/{operation.Value}/{Operation(operation).Code}";
        public bool IsLoop(OperationHandle operation) => Operation(operation).Integer0 == 1;

        public bool IsTrackMuted(OperationHandle operation)
        {
            ProgramCatalogEntry clip = RequireCatalog(operation, ProgramCatalogEntryKind.TimelineClip);
            string trackIdentity = RequireIdentity(clip, "Track");
            ProgramCatalogEntry track = m_Program.Program.CatalogEntries.FirstOrDefault(value => value.Kind == ProgramCatalogEntryKind.TimelineTrack && value.Identity == trackIdentity);
            return track != null && RequireConstant(track, "Muted").Boolean;
        }

        public FixedScalar TimelineDuration(OperationHandle operation)
        {
            ProgramCatalogEntry timeline = RequireCatalog(operation, ProgramCatalogEntryKind.Timeline);
            int maxFrame = RequireConstant(timeline, "MaxFrame").Int32;
            int frameRate = RequireConstant(timeline, "FrameRate").Int32;
            if (maxFrame < 0 || frameRate <= 0)
                throw new InvalidDataException($"Timeline catalog '{timeline.Identity}' has invalid frame range.");
            return FixedScalar.FromInt64(maxFrame) / FixedScalar.FromInt64(frameRate);
        }

        public FixedScalar ClipTime(OperationHandle operation, TimelineClipTimePoint point)
        {
            ProgramCatalogEntry clip = RequireCatalog(operation, ProgramCatalogEntryKind.TimelineClip);
            string field = point switch
            {
                TimelineClipTimePoint.Start => "StartFrame",
                TimelineClipTimePoint.End => "EndFrame",
                TimelineClipTimePoint.CurveEnd => "CurveEndFrame",
                TimelineClipTimePoint.EaseIn => "EaseInFrame",
                TimelineClipTimePoint.EaseOut => "EaseOutFrame",
                _ => throw new ArgumentOutOfRangeException(nameof(point))
            };
            int frame = RequireConstant(clip, field).Int32;
            ProgramCatalogEntry track = m_Program.Program.CatalogEntries.First(value => value.Kind == ProgramCatalogEntryKind.TimelineTrack && value.Identity == RequireIdentity(clip, "Track"));
            ProgramCatalogEntry timeline = m_Program.Program.CatalogEntries.First(value => value.Kind == ProgramCatalogEntryKind.Timeline && value.Identity == RequireIdentity(track, "Timeline"));
            int frameRate = RequireConstant(timeline, "FrameRate").Int32;
            if (frameRate <= 0)
                throw new InvalidDataException($"Timeline '{timeline.Identity}' has invalid FrameRate '{frameRate}'.");
            return FixedScalar.FromInt64(frame) / FixedScalar.FromInt64(frameRate);
        }

        public ProgramControlFlowEdge TreeClipEdge(OperationHandle operation, TimelineTreeClipEdgeKind kind)
        {
            ProgramControlFlowKind flow = kind == TimelineTreeClipEdgeKind.Root || kind == TimelineTreeClipEdgeKind.Enable
                ? ProgramControlFlowKind.Enter
                : ProgramControlFlowKind.Exit;
            string port = kind switch
            {
                TimelineTreeClipEdgeKind.Root => "TreeClip",
                TimelineTreeClipEdgeKind.Enable => "OnEnable",
                TimelineTreeClipEdgeKind.Disable => "OnDisable",
                TimelineTreeClipEdgeKind.Destroy => "OnDestroy",
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
            ProgramControlFlowEdge result = m_Program.Topology.Outgoing(operation, flow)
                .SingleOrDefault(value => string.Equals(value.SourcePort, port, StringComparison.Ordinal));
            return result ?? throw new InvalidOperationException($"Timeline TreeClip '{operation}' has no '{port}' lifecycle edge.");
        }

        public FixedScalar ReadLogicTime(OperationHandle operation) => m_State.ReadScalar(m_Program.Topology.RequireOperationStateSlot(operation, ProgramStateSemantic.TimelineLogicTime));
        public void WriteLogicTime(OperationHandle operation, FixedScalar value) => m_State.WriteScalar(m_Program.Topology.RequireOperationStateSlot(operation, ProgramStateSemantic.TimelineLogicTime), value);
        public ulong ReadActivationGeneration(OperationHandle operation) => m_State.ReadUInt64(m_Program.Topology.RequireOperationStateSlot(operation, ProgramStateSemantic.RunnableActivationGeneration));
        public void SetTickDelta(FixedScalar value)
        {
            if (value < Zero)
                throw new ArgumentOutOfRangeException(nameof(value));
            m_TickDelta = value;
        }

        public void BeginFrame() => m_PendingSceneSamples.Clear();

        public void CommitFrame()
        {
            for (int i = 0; i < m_PendingSceneSamples.Count; i++)
            {
                TimelineScenePresentationSample sample = m_PendingSceneSamples[i];
                FixedTimelineBoundClip binding = m_Bindings.Clips[sample.Operation.Value];
                if (binding.Boolean)
                    binding.Sink.WriteBoolean(sample);
                else
                    binding.Sink.WriteScalar(sample);
            }
            m_PendingSceneSamples.Clear();
        }

        public void DiscardFrame() => m_PendingSceneSamples.Clear();
        public IReadOnlyList<OperationHandle> AnimationProducerRepresentatives(OperationHandle timeline) => Array.Empty<OperationHandle>();

        public bool TryCaptureActionContext(OperationHandle operation, out TimelineActionContextIdentity identity)
        {
            identity = m_ActionContext;
            return identity.IsValid;
        }

        public bool IsActionContextCurrent(OperationHandle operation, TimelineActionContextIdentity identity) => identity.IsValid && identity.Equals(m_ActionContext);
        public IDisposable PushTimelineContext(OperationHandle timeline, OperationHandle clip, int cycle, TimelineActionContextIdentity identity) => NoopDisposable.Instance;

        public void ResetTreeClipState(OperationHandle operation)
        {
            m_State.WriteInt32(m_Program.Topology.RequireOperationStateSlot(operation, ProgramStateSemantic.TimelinePlayback), (int)TimelineTreeClipStatus.Dormant);
            m_State.WriteInt32(m_Program.Topology.RequireOperationStateSlot(operation, ProgramStateSemantic.TimelineTreeClipCycle), 0);
            m_State.WriteScalar(m_Program.Topology.RequireOperationStateSlot(operation, ProgramStateSemantic.TimelineLogicTime), FixedScalar.Zero);
        }

        public FixedScalar ClipScalar(OperationHandle operation, TimelineClipScalarValue value) => RequireConstant(RequireCatalog(operation, ProgramCatalogEntryKind.TimelineClip), "Intensity").Scalar;

        public FixedScalar SampleCurve(OperationHandle operation, TimelineCurveChannel channel, FixedScalar time, FixedScalar fallback)
        {
            string field = channel switch
            {
                TimelineCurveChannel.Weight => "WeightCurve",
                TimelineCurveChannel.EaseIn => "EaseInCurve",
                TimelineCurveChannel.EaseOut => "EaseOutCurve",
                TimelineCurveChannel.PositionX => "PositionX",
                TimelineCurveChannel.PositionY => "PositionY",
                TimelineCurveChannel.PositionZ => "PositionZ",
                TimelineCurveChannel.Yaw => "Yaw",
                _ => throw new ArgumentOutOfRangeException(nameof(channel))
            };
            ProgramCatalogField fieldValue = RequireCatalog(operation, ProgramCatalogEntryKind.TimelineClip).Fields.SingleOrDefault(value => string.Equals(value.Name, field, StringComparison.Ordinal));
            if (fieldValue == null)
                return fallback;
            FixedProgramCurve curve = FixedProgramCurveCodec.Read(RequireConstant(RequireCatalog(operation, ProgramCatalogEntryKind.TimelineClip), field).Bytes.ToArray());
            return curve.Evaluate(Clamp(time, Zero, One), fallback);
        }

        public void SampleMotionCurve(OperationHandle timeline, OperationHandle operation, TimelineSegment<FixedScalar> segment) =>
            throw new InvalidOperationException($"Timeline motion curve '{SourcePath(operation)}' is not supported by standalone playback.");

        public void SampleMotionWarp(OperationHandle operation, TimelineSegment<FixedScalar> segment, TimelineActionContextIdentity actionContext) =>
            throw new InvalidOperationException($"Timeline motion warp '{SourcePath(operation)}' is not supported by standalone playback.");

        public void SampleTimelineClip(OperationHandle timeline, OperationHandle operation, TimelineSegment<FixedScalar> segment)
        {
            if (Operation(operation).Code != SimulationOperationCode.TimelineScenePresentationParameter)
                throw new InvalidOperationException($"Timeline operation '{operation}' is unsupported by standalone playback.");
            if (m_Bindings == null || operation.Value < 0 || operation.Value >= m_Bindings.Clips.Length || m_Bindings.Clips[operation.Value] == null)
                throw new InvalidOperationException($"Timeline scene clip '{operation}' has no prepared binding.");
            FixedScalar start = ClipTime(operation, TimelineClipTimePoint.Start);
            FixedScalar end = ClipTime(operation, TimelineClipTimePoint.End);
            if (segment.Current < start || segment.Previous >= end)
                return;
            FixedScalar duration = Max(Epsilon, Subtract(end, start));
            FixedScalar sampleTime = Clamp(segment.Current, start, end);
            FixedScalar normalized = Clamp(Divide(Subtract(sampleTime, start), duration), Zero, One);
            WriteScene(operation, SampleSceneCurve(operation, normalized), normalized, ReadActivationGeneration(timeline), segment.Cycle);
        }

        public void CompleteTimeline(OperationHandle timeline, FixedScalar time)
        {
            for (int i = 0; i < m_Program.SceneClips.Count; i++)
                WriteScene(m_Program.SceneClips[i].Operation, SampleSceneCurve(m_Program.SceneClips[i].Operation, One), One, ReadActivationGeneration(timeline), 0);
        }

        public void ReleaseTimeline(OperationHandle timeline)
        {
            for (int i = 0; i < m_Program.SceneClips.Count; i++)
                WriteScene(m_Program.SceneClips[i].Operation, Zero, Zero, ReadActivationGeneration(timeline), 0);
        }

        public void EmitPresentation(TimelinePresentationOutput<FixedScalar> output) => throw new InvalidOperationException("Timeline standalone playback has no installed presentation producer.");
        public void EmitCue(TimelineCueOutput<FixedScalar> output) => throw new InvalidOperationException("Timeline standalone playback has no installed Cue producer.");
        public void Attach(TimelineControlRuntime<FixedTimelineOperationTarget, FixedScalar> timeline) => m_Timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));

        public void Bind(TimelineExecutionIdentity identity, FixedTimelineBindingPlan bindings)
        {
            m_Identity = identity;
            m_ActionContext = new TimelineActionContextIdentity(
                identity.OwnerIdentity,
                identity.CallIdentity,
                identity.InstanceId,
                identity.InstanceId);
            m_Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        }

        public void ClearBindings()
        {
            m_Bindings = null;
            m_Identity = default;
            m_ActionContext = default;
        }

        public int ReadInt32(int slot) => m_State.ReadInt32(slot);
        public void WriteInt32(int slot, int value) => m_State.WriteInt32(slot, value);
        public ulong ReadUInt64(int slot) => m_State.ReadUInt64(slot);
        public void WriteUInt64(int slot, ulong value) => m_State.WriteUInt64(slot, value);
        public string ReadIdentity(int slot) => m_State.ReadIdentity(slot);
        public void WriteIdentity(int slot, string value) => m_State.WriteIdentity(slot, value);
        public void PrepareActivation(OperationExecutionDescriptor operation) { }
        public void PrepareSubGraph(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation) { }
        public void ActivateScopes(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, ulong generation) { }
        public void CompleteScopes(OperationExecutionDescriptor operation) { }
        public void ClearStateScope(OperationExecutionDescriptor state) { }
        public void ResetOperationState(OperationExecutionDescriptor operation) => m_State.ResetOperationState(operation);
        public void NotifyStateLifecycle(OperationExecutionDescriptor machine, OperationHandle state, OperationStateLifecyclePhase phase) { }
        public void NotifyStateTransition(OperationExecutionDescriptor machine, OperationHandle exitingState, OperationHandle targetState) { }
        public OperationStopStatus ContinueLeafStop(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => operation.Code == SimulationOperationCode.Timeline ? m_Timeline.ContinueTimelineStop(cursor, operation.Handle, context) : OperationStopStatus.Completed;

        public void ForceStopLeaf(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context)
        {
            if (operation.Code == SimulationOperationCode.Timeline)
                m_Timeline.ForceStopTimeline(cursor, operation.Handle, context);
        }

        public OperationExecutionResult ExecuteLeaf(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation)
        {
            if (operation.Code == SimulationOperationCode.Timeline)
                return m_Timeline.TickTimeline(cursor, operation.Handle);
            if (operation.Code == SimulationOperationCode.ConditionResult ||
                operation.Code == SimulationOperationCode.Compare ||
                operation.Code == SimulationOperationCode.And ||
                operation.Code == SimulationOperationCode.Or ||
                operation.Code == SimulationOperationCode.Not ||
                operation.Code == SimulationOperationCode.Constant)
                return EvaluateValue(cursor, operation).IsTruthy ? OperationExecutionResult.Success : OperationExecutionResult.Failure;
            if (operation.Code == SimulationOperationCode.TimelineScenePresentationParameter)
                return OperationExecutionResult.Success;
            throw new InvalidOperationException($"Timeline standalone playback does not support operation '{operation.Code}'.");
        }

        public void EmitTrace(OperationExecutionDescriptor operation, string code, OperationControlTraceSeverity severity, string detail)
        {
            if (DiagnosticsEnabled)
                m_Trace.Add(new TimelineTraceOutput(operation.Handle, code, severity == OperationControlTraceSeverity.Error ? TimelineTraceSeverity.Error : TimelineTraceSeverity.Detail, detail));
        }

        public void EmitTrace(TimelineTraceOutput output) => m_Trace?.Add(output);

        public bool EvaluateCondition(OperationControlCursor<FixedTimelineOperationTarget> cursor, ProgramControlFlowEdge edge)
        {
            if (edge == null || !edge.HasCondition)
                return true;
            return cursor.Tick(edge.Condition) == OperationExecutionResult.Success;
        }

        void WriteScene(OperationHandle operation, FixedScalar value, FixedScalar normalized, ulong generation, int cycle)
        {
            FixedTimelineBoundClip binding = m_Bindings.Clips[operation.Value];
            var sample = new TimelineScenePresentationSample(
                operation,
                binding.Sink.TargetIdentity,
                binding.ParameterId,
                value.ToSingle(),
                normalized.ToSingle(),
                m_Identity,
                generation == 0 ? 1UL : generation,
                cycle);
            m_PendingSceneSamples.Add(sample);
        }

        FixedTimelineValue EvaluateValue(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, string outputPort = "m_Output")
        {
            var key = (operation.Handle.Value, outputPort ?? string.Empty);
            if (!m_ValueStack.Add(key))
                throw new InvalidOperationException($"Timeline value operation '{operation.Handle}' is recursive.");
            try
            {
                switch (operation.Code)
                {
                    case SimulationOperationCode.ConditionResult:
                        return FixedTimelineValue.BooleanValue(ReadInput(cursor, operation, "m_Result").ToBoolean());
                    case SimulationOperationCode.Compare:
                    {
                        FixedScalar left = ReadInput(cursor, operation, "m_InputValue1").ToScalar();
                        FixedScalar right = ReadInput(cursor, operation, "m_InputValue2").ToScalar();
                        bool result = operation.Integer0 switch
                        {
                            0 => left == right,
                            1 => left != right,
                            2 => left < right,
                            3 => left <= right,
                            4 => left >= right,
                            5 => left > right,
                            _ => false
                        };
                        return FixedTimelineValue.BooleanValue(result);
                    }
                    case SimulationOperationCode.And:
                        return FixedTimelineValue.BooleanValue(ReadInput(cursor, operation, "m_Input1").ToBoolean() && ReadInput(cursor, operation, "m_Input2").ToBoolean());
                    case SimulationOperationCode.Or:
                        return FixedTimelineValue.BooleanValue(ReadInput(cursor, operation, "m_Input1").ToBoolean() || ReadInput(cursor, operation, "m_Input2").ToBoolean());
                    case SimulationOperationCode.Not:
                        return FixedTimelineValue.BooleanValue(!ReadInput(cursor, operation, "m_Input").ToBoolean());
                    case SimulationOperationCode.Constant:
                        IReadOnlyList<int> constantReferences = m_Program.Program.Operations[operation.Handle.Value].ConstantReferences;
                        return constantReferences.Count == 0
                            ? FixedTimelineValue.BooleanValue(false)
                            : FixedTimelineValue.FromConstant(m_Program.Program.Constants[constantReferences[0]]);
                    default:
                        throw new InvalidOperationException($"Timeline standalone value operation '{operation.Code}' is unsupported.");
                }
            }
            finally
            {
                m_ValueStack.Remove(key);
            }
        }

        FixedTimelineValue ReadInput(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, string port)
        {
            if (m_ValueSources.TryGetValue((operation.Handle.Value, port), out var source))
                return EvaluateValue(cursor, m_Program.Topology.Operation(source.Source), source.Port);
            if (m_ConstantInputs.TryGetValue((operation.Handle.Value, port), out int constant))
                return FixedTimelineValue.FromConstant(m_Program.Program.Constants[constant]);
            return FixedTimelineValue.BooleanValue(false);
        }

        FixedScalar SampleSceneCurve(OperationHandle operation, FixedScalar normalized)
        {
            FixedProgramCurve curve = FixedProgramCurveCodec.Read(RequireConstant(RequireCatalog(operation, ProgramCatalogEntryKind.TimelineClip), "ValueCurve").Bytes.ToArray());
            return curve.Evaluate(Clamp(normalized, Zero, One), Zero);
        }

        ProgramCatalogEntry RequireCatalog(OperationHandle operation, ProgramCatalogEntryKind kind)
        {
            ProgramCatalogEntry entry = m_Program.Program.References
                .Where(value => value.HasSourceOperation && value.SourceOperation.Equals(operation) && value.Kind == ProgramReferenceKind.CatalogEntry)
                .Select(value => m_Program.Program.CatalogEntries[value.TargetIndex])
                .SingleOrDefault(value => value.Kind == kind);
            return entry ?? throw new InvalidOperationException($"Operation '{operation}' has no '{kind}' catalog entry.");
        }

        FixedProgramConstant RequireConstant(ProgramCatalogEntry entry, string name)
        {
            ProgramCatalogField field = entry.Fields.SingleOrDefault(value => string.Equals(value.Name, name, StringComparison.Ordinal));
            if (field == null || field.Kind != ProgramCatalogFieldKind.Constant || field.ConstantIndex < 0 || field.ConstantIndex >= m_Program.Program.Constants.Count)
                throw new InvalidDataException($"Timeline catalog '{entry.Identity}' has no constant field '{name}'.");
            return m_Program.Program.Constants[field.ConstantIndex];
        }

        static string RequireIdentity(ProgramCatalogEntry entry, string name)
        {
            ProgramCatalogField field = entry.Fields.SingleOrDefault(value => string.Equals(value.Name, name, StringComparison.Ordinal));
            if (field == null || field.Kind != ProgramCatalogFieldKind.Identity)
                throw new InvalidDataException($"Timeline catalog '{entry.Identity}' has no identity field '{name}'.");
            return field.Identity;
        }

        sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new NoopDisposable();
            public void Dispose() { }
        }
    }

    readonly struct FixedTimelineOperationTarget : IOperationControlTarget<FixedTimelineOperationTarget>
    {
        readonly FixedTimelineExecutionTarget m_Target;

        public FixedTimelineOperationTarget(FixedTimelineExecutionTarget target)
        {
            m_Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public bool DiagnosticsEnabled => m_Target.DiagnosticsEnabled;
        public int ReadInt32(int slotIndex) => m_Target.ReadInt32(slotIndex);
        public void WriteInt32(int slotIndex, int value) => m_Target.WriteInt32(slotIndex, value);
        public ulong ReadUInt64(int slotIndex) => m_Target.ReadUInt64(slotIndex);
        public void WriteUInt64(int slotIndex, ulong value) => m_Target.WriteUInt64(slotIndex, value);
        public string ReadIdentity(int slotIndex) => m_Target.ReadIdentity(slotIndex);
        public void WriteIdentity(int slotIndex, string value) => m_Target.WriteIdentity(slotIndex, value);
        public bool EvaluateCondition(OperationControlCursor<FixedTimelineOperationTarget> cursor, ProgramControlFlowEdge edge) => m_Target.EvaluateCondition(cursor, edge);
        public OperationExecutionResult ExecuteLeaf(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation) => m_Target.ExecuteLeaf(cursor, operation);
        public void PrepareActivation(OperationExecutionDescriptor operation) => m_Target.PrepareActivation(operation);
        public void PrepareSubGraph(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation) => m_Target.PrepareSubGraph(cursor, operation);
        public void ActivateScopes(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, ulong generation) => m_Target.ActivateScopes(cursor, operation, generation);
        public void CompleteScopes(OperationExecutionDescriptor operation) => m_Target.CompleteScopes(operation);
        public void ClearStateScope(OperationExecutionDescriptor state) => m_Target.ClearStateScope(state);
        public void ResetOperationState(OperationExecutionDescriptor operation) => m_Target.ResetOperationState(operation);
        public OperationStopStatus ContinueLeafStop(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => m_Target.ContinueLeafStop(cursor, operation, context);
        public void ForceStopLeaf(OperationControlCursor<FixedTimelineOperationTarget> cursor, OperationExecutionDescriptor operation, OperationStopContext context) => m_Target.ForceStopLeaf(cursor, operation, context);
        public void EmitTrace(OperationExecutionDescriptor operation, string code, OperationControlTraceSeverity severity, string detail) => m_Target.EmitTrace(operation, code, severity, detail);
        public void NotifyStateLifecycle(OperationExecutionDescriptor machine, OperationHandle state, OperationStateLifecyclePhase phase) => m_Target.NotifyStateLifecycle(machine, state, phase);
        public void NotifyStateTransition(OperationExecutionDescriptor machine, OperationHandle exitingState, OperationHandle targetState) => m_Target.NotifyStateTransition(machine, exitingState, targetState);
    }

    readonly struct FixedTimelineValue
    {
        enum ValueKind : byte
        {
            Boolean,
            Int32,
            UInt64,
            Scalar,
            Identity
        }

        FixedTimelineValue(ValueKind kind, bool boolean, int int32, ulong uint64, FixedScalar scalar, string identity)
        {
            Kind = kind;
            Boolean = boolean;
            Int32 = int32;
            UInt64 = uint64;
            Scalar = scalar;
            Identity = identity ?? string.Empty;
        }

        ValueKind Kind { get; }
        bool Boolean { get; }
        int Int32 { get; }
        ulong UInt64 { get; }
        FixedScalar Scalar { get; }
        string Identity { get; }
        public bool IsTruthy => ToBoolean();

        public static FixedTimelineValue BooleanValue(bool value) => new FixedTimelineValue(ValueKind.Boolean, value, 0, 0, default, null);

        public static FixedTimelineValue FromConstant(FixedProgramConstant constant)
        {
            return constant.Kind switch
            {
                ThirdPersonSimulation.Fixed.ProgramConstantKind.Boolean => BooleanValue(constant.Boolean),
                ThirdPersonSimulation.Fixed.ProgramConstantKind.Int32 => new FixedTimelineValue(ValueKind.Int32, false, constant.Int32, 0, default, null),
                ThirdPersonSimulation.Fixed.ProgramConstantKind.UInt64 => new FixedTimelineValue(ValueKind.UInt64, false, 0, constant.UInt64, default, null),
                ThirdPersonSimulation.Fixed.ProgramConstantKind.Scalar => new FixedTimelineValue(ValueKind.Scalar, false, 0, 0, constant.Scalar, null),
                ThirdPersonSimulation.Fixed.ProgramConstantKind.String => new FixedTimelineValue(ValueKind.Identity, false, 0, 0, default, constant.Text),
                _ => throw new InvalidOperationException($"Timeline value constant '{constant.Identity}' has unsupported kind '{constant.Kind}'.")
            };
        }

        public bool ToBoolean() => Kind switch
        {
            ValueKind.Boolean => Boolean,
            ValueKind.Int32 => Int32 != 0,
            ValueKind.UInt64 => UInt64 != 0,
            ValueKind.Scalar => Scalar != FixedScalar.Zero,
            ValueKind.Identity => !string.IsNullOrEmpty(Identity),
            _ => false
        };

        public FixedScalar ToScalar() => Kind switch
        {
            ValueKind.Boolean => Boolean ? FixedScalar.One : FixedScalar.Zero,
            ValueKind.Int32 => FixedScalar.FromInt64(Int32),
            ValueKind.UInt64 when UInt64 <= long.MaxValue => FixedScalar.FromInt64((long)UInt64),
            ValueKind.Scalar => Scalar,
            _ => throw new InvalidOperationException($"Timeline value kind '{Kind}' is not numeric.")
        };
    }
}
