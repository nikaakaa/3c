using System;
using System.Collections.Generic;
using System.Globalization;

namespace ThirdPersonSimulation
{
    internal readonly struct SimulationBlackboardSlotGroup
    {
        public SimulationBlackboardSlotGroup(
            int value,
            int ownerToken,
            int lifetime,
            int writeStamp,
            ProgramScopeLayout scope,
            int compiledOwnerIndex,
            ProgramBlackboardLifetime lifetimeKind)
        {
            Value = value;
            OwnerToken = ownerToken;
            Lifetime = lifetime;
            WriteStamp = writeStamp;
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            CompiledOwnerIndex = compiledOwnerIndex;
            LifetimeKind = lifetimeKind;
        }

        public int Value { get; }
        public int OwnerToken { get; }
        public int Lifetime { get; }
        public int WriteStamp { get; }
        public ProgramScopeLayout Scope { get; }
        public int CompiledOwnerIndex { get; }
        public ProgramBlackboardLifetime LifetimeKind { get; }
    }

    internal readonly struct SimulationTimelineBlackboardContext
    {
        public SimulationTimelineBlackboardContext(
            OperationHandle timeline,
            OperationHandle clip,
            int cycle,
            Float32ActionInstanceState action)
        {
            Timeline = timeline;
            Clip = clip;
            Cycle = cycle;
            Action = action;
        }

        public OperationHandle Timeline { get; }
        public OperationHandle Clip { get; }
        public int Cycle { get; }
        public Float32ActionInstanceState Action { get; }
        public bool IsValid => Timeline.IsValid && Clip.IsValid;
    }

    internal readonly struct SimulationActionWindowProjectionCandidate
    {
        public SimulationActionWindowProjectionCandidate(
            OperationHandle source,
            ActorId actorId,
            ulong logicTick,
            string declarationId,
            string actionId,
            ulong actionInstanceId,
            string windowId,
            string windowType,
            ulong digest)
        {
            Source = source;
            ActorId = actorId;
            LogicTick = logicTick;
            DeclarationId = declarationId;
            ActionId = actionId;
            ActionInstanceId = actionInstanceId;
            WindowId = windowId;
            WindowType = windowType;
            Digest = digest;
        }

        public OperationHandle Source { get; }
        public ActorId ActorId { get; }
        public ulong LogicTick { get; }
        public string DeclarationId { get; }
        public string ActionId { get; }
        public ulong ActionInstanceId { get; }
        public string WindowId { get; }
        public string WindowType { get; }
        public ulong Digest { get; }
    }

    internal sealed class Float32BlackboardRuntime : Float32OperationModule, IFloat32BlackboardPort
    {
        readonly Float32StatePort m_State;
        readonly Float32EvaluationFrame m_Frame;
        readonly IFloat32ActionContextReader m_Actions;
        readonly Float32FactSink m_Facts;
        readonly Float32TraceSink m_Trace;
        readonly List<SimulationActionWindowProjectionCandidate> m_ActionWindowProjections;
        readonly HashSet<string> m_ActionWindowProjectionKeys;
        readonly Stack<SimulationTimelineBlackboardContext> m_TimelineBlackboardContexts;

        public Float32BlackboardRuntime(
            Float32ProgramAccess access,
            Float32StatePort state,
            Float32EvaluationFrame frame,
            IFloat32ActionContextReader actions,
            Float32FactSink facts,
            Float32TraceSink trace,
            Float32EvaluationWorkspace workspace)
            : base(access)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Frame = frame ?? throw new ArgumentNullException(nameof(frame));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Facts = facts ?? throw new ArgumentNullException(nameof(facts));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
            if (workspace == null)
                throw new ArgumentNullException(nameof(workspace));
            m_ActionWindowProjections = workspace.ActionWindowProjections;
            m_ActionWindowProjectionKeys = workspace.ActionWindowProjectionKeys;
            m_TimelineBlackboardContexts = workspace.TimelineBlackboardContexts;
        }

        public void BeginFrame()
        {
            m_ActionWindowProjections.Clear();
            m_ActionWindowProjectionKeys.Clear();
        }

        public void EndFrame()
        {
            FlushBlackboardProjections();
            m_ActionWindowProjections.Clear();
            m_ActionWindowProjectionKeys.Clear();
        }

        public void ActivateOperationScopes<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            ulong generation)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            if (generation == 0)
                throw new InvalidOperationException($"Operation scope '{SourcePath(operation)}' has no activation generation.");
        }

        public void CompleteOperationScopes(SimulationOperation operation)
        {
        }

        public void ClearStateScopes(SimulationOperation state)
        {
        }

        public void ClearActionInstanceScopes(ulong actionInstanceId)
        {
            if (actionInstanceId == 0)
                return;
        }

        public void ProjectInputDerived(InputDerivedStateBinding binding, SimulationInputValue value)
        {
            if (!string.Equals(binding.InputId, value.InputId, StringComparison.Ordinal) ||
                (byte)binding.InputKind != (byte)value.Kind)
                throw new InvalidOperationException($"InputDerived binding '{binding.InputId}/{binding.InputKind}' received '{value.InputId}/{value.Kind}'.");
            SimulationBlackboardSlotGroup group = RequireBlackboardGroup(binding.StateAddress.SlotIndex);
            if (group.Scope.Kind != ProgramScopeKind.Character || group.LifetimeKind != ProgramBlackboardLifetime.Spawn)
                throw new InvalidOperationException($"InputDerived Blackboard '{binding.InputId}' must use Character/Spawn ownership.");
            var owner = new BlackboardOwnerToken(ProgramScopeKind.Character, group.CompiledOwnerIndex, 1);
            if (m_State.Get(group.OwnerToken).BlackboardOwnerToken != owner)
                MaterializeGroup(group, owner);
            m_State.Set(group.Value, ToStateValue(value));
            m_State.Set(group.WriteStamp, CharacterStateValue.FromBlackboardWriteStamp(new BlackboardWriteStamp(
                m_Layout.RootOperation,
                m_Frame.Tick.Value,
                0,
                OperationHandle.Invalid,
                OperationHandle.Invalid,
                0)));
        }

        static CharacterStateValue ToStateValue(SimulationInputValue value)
        {
            return value.Kind switch
            {
                SimulationInputValueKind.Boolean => CharacterStateValue.FromBoolean(value.Boolean),
                SimulationInputValueKind.Scalar => CharacterStateValue.FromScalar(value.Scalar),
                SimulationInputValueKind.Vector2 => CharacterStateValue.FromVector2(value.Vector2),
                SimulationInputValueKind.Vector3 => CharacterStateValue.FromVector3(value.Vector3),
                SimulationInputValueKind.Yaw => CharacterStateValue.FromYaw(value.Yaw),
                SimulationInputValueKind.ActionTargetSnapshot => CharacterStateValue.FromActionTargetSnapshot(value.ActionTargetSnapshot),
                _ => throw new InvalidOperationException($"InputDerived value kind '{value.Kind}' is unsupported.")
            };
        }

        public CharacterStateValue Read<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            int valueSlot)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            SimulationBlackboardSlotGroup group = RequireBlackboardGroup(valueSlot);
            BlackboardOwnerToken expected = ResolveBlackboardOwnerToken(cursor, operation, group, false, out _);
            if (m_State.Get(group.OwnerToken).BlackboardOwnerToken != expected)
                return DefaultValue(group);
            return m_State.Get(valueSlot);
        }

        public void Write<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            int valueSlot,
            CharacterStateValue value)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            SimulationBlackboardSlotGroup group = RequireBlackboardGroup(valueSlot);
            ProgramScopeLayout scope = group.Scope;
            ProgramBlackboardLifetime lifetime = group.LifetimeKind;
            if (lifetime == ProgramBlackboardLifetime.Config)
                throw new InvalidOperationException($"Blackboard config '{m_Program.StateSlots[valueSlot].OwnerIdentity}' is read-only.");
            BlackboardOwnerToken expected = ResolveBlackboardOwnerToken(cursor, operation, group, true, out Float32ActionInstanceState action);
            BlackboardOwnerToken current = m_State.Get(group.OwnerToken).BlackboardOwnerToken;
            if (current != expected)
            {
                if (scope.Kind == ProgramScopeKind.ActionInstance && current.IsValid && m_Actions.ContainsInstance(current.Generation))
                {
                    throw new InvalidOperationException(
                        $"ActionInstance Blackboard scope '{scope.Identity}' cannot bind active instances '{current.Generation}' and '{expected.Generation}' to one state address.");
                }
                MaterializeGroup(group, expected);
            }
            m_State.Set(valueSlot, value);
            m_State.Set(group.WriteStamp, CharacterStateValue.FromBlackboardWriteStamp(BuildBlackboardWriteStamp(operation, action)));
            ProjectBlackboardWrite(operation, valueSlot, value, action);
        }

        BlackboardOwnerToken ResolveBlackboardOwnerToken<TTarget>(
            OperationControlCursor<TTarget> cursor,
            SimulationOperation operation,
            SimulationBlackboardSlotGroup group,
            bool writing,
            out Float32ActionInstanceState action)
            where TTarget : struct, IOperationControlTarget<TTarget>
        {
            action = default;
            ProgramScopeLayout scope = group.Scope;
            ulong expectedGeneration;
            switch (scope.Kind)
            {
                case ProgramScopeKind.Character:
                    expectedGeneration = 1;
                    break;
                case ProgramScopeKind.Graph:
                    if (group.LifetimeKind == ProgramBlackboardLifetime.Config)
                    {
                        expectedGeneration = 1;
                    }
                    else
                    {
                        if (!cursor.IsActive(scope.OwnerOperation))
                            throw new InvalidOperationException($"Blackboard access '{SourcePath(operation)}' is outside Graph scope '{scope.Identity}'.");
                        expectedGeneration = cursor.ReadGeneration(scope.OwnerOperation);
                    }
                    break;
                case ProgramScopeKind.State:
                    if (!cursor.IsCurrentStateExecution(scope.OwnerOperation))
                        throw new InvalidOperationException($"Blackboard access '{SourcePath(operation)}' is outside State scope '{scope.Identity}'.");
                    expectedGeneration = cursor.ReadGeneration(scope.OwnerOperation);
                    break;
                case ProgramScopeKind.ActionInstance:
                    action = ResolveBlackboardActionContext(operation, writing);
                    if (!action.IsActive)
                        throw new InvalidOperationException($"Blackboard access '{SourcePath(operation)}' requires an explicit active Action Context.");
                    expectedGeneration = action.InstanceId;
                    break;
                case ProgramScopeKind.Frame:
                    expectedGeneration = m_Frame.Tick.Value;
                    if (writing && BlackboardRequiresActionWindowProjection(operation))
                        action = ResolveBlackboardActionContext(operation, true);
                    break;
                default:
                    throw new InvalidOperationException($"Blackboard scope '{scope.Kind}' is unsupported.");
            }
            return new BlackboardOwnerToken(scope.Kind, group.CompiledOwnerIndex, expectedGeneration);
        }

        Float32ActionInstanceState ResolveBlackboardActionContext(SimulationOperation operation, bool writing)
        {
            if (m_TimelineBlackboardContexts.Count > 0)
            {
                SimulationTimelineBlackboardContext timeline = m_TimelineBlackboardContexts.Peek();
                string explicitContext = GetStringConstant(operation, OperationNamedConstant.FactContext, string.Empty);
                if (!string.IsNullOrEmpty(explicitContext) &&
                    !string.Equals(explicitContext, timeline.Action.ContextId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"TreeClip Blackboard write '{SourcePath(operation)}' Action Context does not match its Timeline playback context.");
                }
                return m_Actions.RequireActive(timeline.Action);
            }

            string contextId = GetStringConstant(operation, OperationNamedConstant.FactContext, string.Empty);
            if (!string.IsNullOrEmpty(contextId))
            {
                int slot = m_Actions.FindActive(contextId, out Float32ActionInstanceState explicitAction);
                return slot >= 0 ? explicitAction : default;
            }

            if (writing && BlackboardRequiresActionWindowProjection(operation))
                return default;
            return m_Actions.FindOnlyActive();
        }

        BlackboardWriteStamp BuildBlackboardWriteStamp(SimulationOperation operation, Float32ActionInstanceState action)
        {
            if (m_TimelineBlackboardContexts.Count == 0)
            {
                return new BlackboardWriteStamp(
                    operation.Handle,
                    m_Frame.Tick.Value,
                    action.IsActive ? action.InstanceId : 0,
                    OperationHandle.Invalid,
                    OperationHandle.Invalid,
                    0);
            }
            SimulationTimelineBlackboardContext context = m_TimelineBlackboardContexts.Peek();
            return new BlackboardWriteStamp(
                operation.Handle,
                m_Frame.Tick.Value,
                action.IsActive ? action.InstanceId : 0,
                context.Timeline,
                context.Clip,
                context.Cycle);
        }

        void ProjectBlackboardWrite(
            SimulationOperation operation,
            int valueSlot,
            CharacterStateValue value,
            Float32ActionInstanceState action)
        {
            ProgramCatalogEntry declaration = RequireBlackboardDeclaration(operation);
            ProgramBlackboardFactProjectionKind projection =
                (ProgramBlackboardFactProjectionKind)CatalogInt32(declaration, ProgramCatalogFieldId.Projection);
            if (projection == ProgramBlackboardFactProjectionKind.None)
                return;
            if (projection != ProgramBlackboardFactProjectionKind.ActionWindow ||
                value.Kind != ProgramStateValueKind.Boolean)
            {
                throw new InvalidOperationException($"Blackboard projection '{declaration.Identity}' is not a valid Bool ActionWindow projection.");
            }
            if (!value.Boolean)
                return;
            SimulationBlackboardSlotGroup group = RequireBlackboardGroup(valueSlot);
            if (group.Scope.Kind != ProgramScopeKind.Frame || group.LifetimeKind != ProgramBlackboardLifetime.Frame)
                throw new InvalidOperationException($"ActionWindow projection '{declaration.Identity}' is not Frame/Frame.");
            if (!action.IsActive)
                throw new InvalidOperationException($"ActionWindow projection '{declaration.Identity}' has no explicit active Action Context.");
            BlackboardWriteStamp stamp = m_State.Get(group.WriteStamp).BlackboardWriteStamp;
            if (!stamp.IsValid || !stamp.SourceOperation.Equals(operation.Handle) || stamp.LogicTick != m_Frame.Tick.Value ||
                stamp.ActionInstanceId != action.InstanceId)
            {
                throw new InvalidOperationException($"ActionWindow projection '{declaration.Identity}' has no current Blackboard write stamp.");
            }

            string key = $"{declaration.Identity}/{action.InstanceId.ToString(CultureInfo.InvariantCulture)}";
            if (!m_ActionWindowProjectionKeys.Add(key))
                return;
            m_ActionWindowProjections.Add(new SimulationActionWindowProjectionCandidate(
                operation.Handle,
                m_Frame.ActorId,
                m_Frame.Tick.Value,
                declaration.Identity,
                action.ActionId,
                action.InstanceId,
                CatalogString(declaration, ProgramCatalogFieldId.ActionWindowId),
                CatalogString(declaration, ProgramCatalogFieldId.ActionWindowType),
                CatalogUInt64(declaration, ProgramCatalogFieldId.ActionWindowDigest)));
        }

        public bool IsActionWindowActive(SimulationOperation operation)
        {
            string windowType = operation.Text0;
            if (string.IsNullOrWhiteSpace(windowType))
                throw new InvalidOperationException($"ActionWindow query '{SourcePath(operation)}' has no WindowType.");
            Float32ActionInstanceState active = m_Actions.FindOnlyActive();
            if (!active.IsActive)
            {
                if (m_Trace.Enabled)
                    m_Trace.Add(operation, "action_window_inactive", SimulationTraceSeverity.Detail, $"{windowType}:no_active_action");
                return false;
            }
            for (int i = 0; i < m_ActionWindowProjections.Count; i++)
            {
                SimulationActionWindowProjectionCandidate candidate = m_ActionWindowProjections[i];
                if (candidate.ActorId == m_Frame.ActorId &&
                    candidate.LogicTick == m_Frame.Tick.Value &&
                    candidate.ActionInstanceId == active.InstanceId &&
                    string.Equals(candidate.WindowType, windowType, StringComparison.Ordinal))
                {
                    if (m_Trace.Enabled)
                        m_Trace.Add(operation, "action_window_active", SimulationTraceSeverity.Detail, $"{windowType}:{candidate.WindowId}:{candidate.Digest}");
                    return true;
                }
            }
            if (m_Trace.Enabled)
                m_Trace.Add(operation, "action_window_inactive", SimulationTraceSeverity.Detail, $"{windowType}:action={active.ActionId}:instance={active.InstanceId}:tick={m_Frame.Tick.Value}");
            return false;
        }

        bool BlackboardRequiresActionWindowProjection(SimulationOperation operation)
        {
            ProgramCatalogEntry declaration = RequireBlackboardDeclaration(operation);
            return CatalogInt32(declaration, ProgramCatalogFieldId.Projection) == (int)ProgramBlackboardFactProjectionKind.ActionWindow;
        }

        ProgramCatalogEntry RequireBlackboardDeclaration(SimulationOperation operation)
        {
            return RequireCatalog(operation, ProgramCatalogEntryKind.BlackboardDeclaration);
        }

        void FlushBlackboardProjections()
        {
            for (int i = 0; i < m_ActionWindowProjections.Count; i++)
            {
                SimulationActionWindowProjectionCandidate candidate = m_ActionWindowProjections[i];
                SimulationOperation source = m_Program.Operations[candidate.Source.Value];
                SimulationEventHeader header = m_Facts.Next(source);
                var window = new ActionWindowFact(
                    candidate.ActionInstanceId,
                    candidate.ActionId,
                    candidate.WindowId,
                    candidate.WindowType,
                    m_Frame.Tick.Value,
                    m_Frame.Tick.Value,
                    candidate.Digest);
                m_Facts.Add(new GameplayFact(header, window));
                if (m_Trace.Enabled)
                    m_Trace.Add(source, "blackboard_action_window_projected", SimulationTraceSeverity.Information, candidate.DeclarationId);
            }
        }

        public IDisposable PushTimelineContext(
            SimulationOperation timeline,
            SimulationOperation clip,
            int cycle,
            Float32ActionInstanceState action)
        {
            m_TimelineBlackboardContexts.Push(new SimulationTimelineBlackboardContext(
                timeline.Handle,
                clip.Handle,
                cycle,
                action));
            return new TimelineBlackboardScope(this);
        }

        CharacterStateValue DefaultValue(SimulationBlackboardSlotGroup group)
        {
            ProgramStateSlot value = m_Program.StateSlots[group.Value];
            return CharacterStateValue.FromConstant(m_Program.Constants[value.DefaultConstantIndex], value.ValueKind);
        }

        void MaterializeGroup(SimulationBlackboardSlotGroup group, BlackboardOwnerToken ownerToken)
        {
            m_State.Set(group.Value, DefaultValue(group));
            m_State.Set(group.OwnerToken, CharacterStateValue.FromBlackboardOwnerToken(ownerToken));
            m_State.Set(group.WriteStamp, CharacterStateValue.FromBlackboardWriteStamp(default));
        }

        SimulationBlackboardSlotGroup RequireBlackboardGroup(int valueSlot)
        {
            ProgramStateSlot value = m_Program.StateSlots[valueSlot];
            if (value.Semantic != ProgramStateSemantic.BlackboardValue)
                throw new InvalidOperationException($"State slot '{valueSlot}' is not a Blackboard value.");
            return Access.Services.RequireBlackboardGroup(value.OwnerIdentity);
        }

        sealed class TimelineBlackboardScope : IDisposable
        {
            Float32BlackboardRuntime m_Owner;

            public TimelineBlackboardScope(Float32BlackboardRuntime owner)
            {
                m_Owner = owner;
            }

            public void Dispose()
            {
                if (m_Owner == null)
                    return;
                m_Owner.m_TimelineBlackboardContexts.Pop();
                m_Owner = null;
            }
        }
    }
}
