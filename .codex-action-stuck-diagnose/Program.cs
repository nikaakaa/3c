using ThirdPersonSimulation;

const string ArtifactPath = @"D:\Unity_Project_1\3C\.codex-action-stuck-program.artifact";
byte[] bytes = File.ReadAllBytes(ArtifactPath);
CharacterSimulationProgramArtifactHeader header = CharacterSimulationProgramCodec.ReadArtifactHeader(bytes);
var expectation = new ProgramLoadExpectation(
    header.CompilerVersion,
    header.OperationSetVersion,
    header.SourceRevision,
    header.SemanticHash,
    header.NumericProfile);
CharacterSimulationProgram program = CharacterSimulationProgramCodec.ReadArtifact(bytes, expectation);
var runner = new Runner(program);

PrintMultiChildStateExits(program);
RunScenario("turn-dodge", tick => tick == 153 ? "Dodge" : null, 340);
RunScenario("turn-dodge-turn-attack", tick => tick == 153 ? "Dodge" : tick == 210 ? "Attack" : null, 380);
RunScenario("turn-dodge-turn-dodge", tick => tick == 153 || tick == 210 ? "Dodge" : null, 380);

void RunScenario(string name, Func<ulong, string> requestAt, ulong finalTick)
{
    Console.WriteLine($"=== {name} ===");
    runner.Reset();
    string previous = string.Empty;
    for (ulong tick = 1; tick <= finalTick; tick++)
    {
        Float32Vector2 move = tick < 150 ? V2(0f, 1f) : V2(0f, -1f);
        string request = requestAt(tick);
        SimulationActorTickResult result = runner.Tick(tick, move, request);
        foreach (SimulationTraceRecord trace in result.TraceRecords)
        {
            if (trace.Severity == SimulationTraceSeverity.Error ||
                trace.Detail.Contains("Failure", StringComparison.Ordinal) ||
                trace.Code.StartsWith("action_", StringComparison.Ordinal))
            {
                Console.WriteLine($"TRACE {tick}: op={trace.Header.Activation.Operation.Value} {trace.Boundary}/{trace.Code} {trace.Detail}");
            }
        }
        string state = runner.Describe(result.State);
        if (!string.Equals(previous, state, StringComparison.Ordinal) || request != null)
        {
            Console.WriteLine($"{tick}: request={request ?? "-"} {state}");
            previous = state;
        }
        if (runner.IsStuck(result.State))
        {
            Console.WriteLine($"STUCK at {tick}: {state}");
            break;
        }
    }
}

static Float32Vector2 V2(float x, float y) => new(
    Float32Scalar.FromSingle(x),
    Float32Scalar.FromSingle(y));

static void PrintSource(CharacterSimulationProgram value, int handle)
{
    ProgramSourceMapEntry source = value.SourceMap.FirstOrDefault(entry =>
        entry.TargetKind == ProgramSourceTargetKind.Operation && entry.TargetIndex == handle);
    Console.WriteLine(source == null
        ? $"operation {handle} {value.Operations[handle].Code}: source absent"
        : $"operation {handle} {value.Operations[handle].Code}: {source.DisplayPath} graph={source.GraphId} node={source.NodeId}");
}

static void PrintOperationSlots(CharacterSimulationProgram value, int handle)
{
    SimulationOperation operation = value.Operations[handle];
    Console.WriteLine($"operation {handle} {operation.Code} slots: " + string.Join(", ", operation.StateSlots.Select(index =>
        $"{index}:{value.StateSlots[index].Semantic}")));
}

static void PrintControlFlow(CharacterSimulationProgram value, int rootHandle, int maxDepth)
{
    ProgramExecutionLayout layout = ProgramExecutionLayout.GetOrCreate(value);
    var visited = new HashSet<int>();
    Print(rootHandle, 0);

    void Print(int handle, int depth)
    {
        if (depth > maxDepth || !visited.Add(handle))
            return;
        SimulationOperation operation = value.Operations[handle];
        Console.WriteLine($"FLOW {new string(' ', depth * 2)}{handle}:{operation.Code} operands=[{string.Join(',', operation.Operands)}] constants=[{string.Join(',', operation.ConstantReferences)}] int0={operation.Integer0} int1={operation.Integer1} flags={operation.Flags} text={operation.Text0}");
        foreach (ProgramControlFlowEdge edge in layout.IncomingValues(operation.Handle))
        {
            Console.WriteLine($"VALUE {new string(' ', depth * 2)}{edge.Source.Value}:{edge.SourcePort} --> {handle}:{edge.TargetPort}");
            Print(edge.Source.Value, depth + 1);
        }
        foreach (ProgramControlFlowKind kind in Enum.GetValues<ProgramControlFlowKind>())
        {
            foreach (ProgramControlFlowEdge edge in layout.Outgoing(operation.Handle, kind))
            {
                Console.WriteLine($"EDGE {new string(' ', depth * 2)}{handle} --{kind}/{edge.SourcePort}/{edge.TargetPort}--> {edge.Target.Value} condition={edge.Condition.Value}");
                Print(edge.Target.Value, depth + 1);
            }
        }
    }
}

static void PrintMultiChildStateExits(CharacterSimulationProgram value)
{
    ProgramExecutionLayout layout = ProgramExecutionLayout.GetOrCreate(value);
    foreach (SimulationOperation state in value.Operations.Where(operation => operation.Code == SimulationOperationCode.State))
    {
        foreach (ProgramControlFlowEdge exit in layout.Outgoing(state.Handle, ProgramControlFlowKind.Exit))
        {
            IReadOnlyList<ProgramControlFlowEdge> children = layout.Outgoing(exit.Target, ProgramControlFlowKind.Child);
            if (children.Count <= 1)
                continue;
            ProgramSourceMapEntry source = value.SourceMap.FirstOrDefault(entry =>
                entry.TargetKind == ProgramSourceTargetKind.Operation && entry.TargetIndex == state.Handle.Value);
            Console.WriteLine($"MULTI_EXIT state={state.Handle.Value} graph={source?.GraphId} node={source?.NodeId} exit={exit.Target.Value} children={string.Join(',', children.Select(child => child.Target.Value))}");
            foreach (ProgramControlFlowEdge child in children)
                PrintSource(value, child.Target.Value);
        }
    }
}

sealed class Runner
{
    readonly CharacterSimulationProgram m_Program;
    readonly ProgramExecutionLayout m_Layout;
    readonly SimulationKernel m_Kernel = SimulationKernel.CreateFloat32();
    readonly ActorId m_Actor = new("diagnose-actor");
    readonly SolverImplementationId m_Solver = new("diagnose-solver");
    CharacterSimulationState m_State;
    WorldBodyState m_Body;
    ulong m_InputSequence;
    ulong m_RequestSequence;

    public Runner(CharacterSimulationProgram program)
    {
        m_Program = program;
        m_Layout = ProgramExecutionLayout.GetOrCreate(program);
        Reset();
    }

    public void Reset()
    {
        m_State = CharacterSimulationState.CreateInitial(m_Program);
        m_Body = new WorldBodyState(
            m_Actor,
            Float32Vector3.Zero,
            Float32Yaw.Zero,
            Float32Vector3.Zero,
            true,
            default);
        m_InputSequence = 0;
        m_RequestSequence = 0;
    }

    public SimulationActorTickResult Tick(ulong tickValue, Float32Vector2 move, string requestId)
    {
        var tick = new SimulationTick(tickValue);
        m_InputSequence++;
        var values = new[]
        {
            SimulationInputValue.FromVector2("LookAxis", Float32Vector2.Zero),
            SimulationInputValue.FromVector2("MoveAxis", move),
            SimulationInputValue.FromBoolean(CameraProgramOperationSchema.BasisValidInputId, true),
            SimulationInputValue.FromVector3(CameraProgramOperationSchema.BasisPlanarForwardInputId, V3(0f, 0f, 1f)),
            SimulationInputValue.FromVector3(CameraProgramOperationSchema.BasisPlanarRightInputId, V3(1f, 0f, 0f)),
            SimulationInputValue.FromVector3(CameraProgramOperationSchema.BasisLookDirectionInputId, V3(0f, 0f, 1f)),
            SimulationInputValue.FromVector3(CameraProgramOperationSchema.BasisAimPointInputId, Float32Vector3.Zero),
            SimulationInputValue.FromYaw(CameraProgramOperationSchema.BasisYawInputId, Float32Yaw.Zero),
            SimulationInputValue.FromScalar(CameraProgramOperationSchema.BasisPitchInputId, Float32Scalar.Zero)
        };
        SimulationInputRequest[] requests;
        if (requestId == null)
        {
            requests = Array.Empty<SimulationInputRequest>();
        }
        else
        {
            m_RequestSequence++;
            int priority = string.Equals(requestId, "Dodge", StringComparison.Ordinal) ? 100 : 0;
            requests = new[]
            {
                new SimulationInputRequest(requestId, m_RequestSequence, tickValue, tickValue + 6, priority)
            };
        }
        var input = new CharacterSimulationInput(
            m_Program.Manifest.NumericProfile,
            new SimulationTickSourceIdentity(SimulationTickSourceKind.LocalLogic, "diagnose-clock", tickValue),
            "diagnose-input",
            m_InputSequence,
            values,
            requests);
        var pending = m_Kernel.Evaluate(new SimulationEvaluateRequest(
            m_Program,
            m_Layout,
            m_Actor,
            tick,
            input,
            Array.Empty<SimulationIngress>(),
            m_State,
            m_Body,
            true));
        CharacterMotionRequest motion = pending.WorldRequest.Motion;
        Float32Vector3 displacement = motion.HasMotion ? motion.Displacement : Float32Vector3.Zero;
        Float32Scalar yaw = motion.HasMotion ? motion.YawDegrees : Float32Scalar.Zero;
        var finalBody = new WorldBodyState(
            m_Actor,
            m_Body.Position + displacement,
            new Float32Yaw(m_Body.Yaw.Degrees + yaw),
            motion.HasMotion ? motion.RequestedVelocity : Float32Vector3.Zero,
            true,
            default);
        var world = new CharacterWorldSolveResult(
            m_Program.Manifest.NumericProfile,
            m_Actor,
            pending.WorldRequest.RequestId,
            tick,
            m_Solver,
            finalBody,
            displacement,
            yaw);
        SimulationActorTickResult result = m_Kernel.Finalize(new SimulationFinalizeRequest(pending, world, m_Solver));
        m_State = result.State;
        m_Body = result.BodySample.FinalBody;
        return result;
    }

    public string Describe(CharacterSimulationState state)
    {
        return $"Action={Read(state, 1)} Locomotion={Read(state, 2)} Dodge={Read(state, 32)}/{ReadLifecycle(state, 32)} AttackCombo={Read(state, 206)}/{ReadLifecycle(state, 206)}";
    }

    public bool IsStuck(CharacterSimulationState state)
    {
        return string.Equals(Read(state, 1), "8", StringComparison.Ordinal) &&
               string.Equals(Read(state, 2), "633", StringComparison.Ordinal);
    }

    string Read(CharacterSimulationState state, int machineHandle)
    {
        SimulationOperation operation = m_Program.Operations[machineHandle];
        int slot = m_Layout.FindOperationStateSlot(operation.Handle, ProgramStateSemantic.StateMachineActive);
        return slot < 0 ? "missing" : state.Get(slot, ProgramStateValueKind.Identity).Identity;
    }

    string ReadLifecycle(CharacterSimulationState state, int operationHandle)
    {
        SimulationOperation operation = m_Program.Operations[operationHandle];
        int slot = m_Layout.FindOperationStateSlot(operation.Handle, ProgramStateSemantic.RunnableLifecycle);
        return slot < 0
            ? "missing"
            : ((OperationRunnableStatus)state.Get(slot, ProgramStateValueKind.Int32).Int32).ToString();
    }

    static Float32Vector3 V3(float x, float y, float z) => new(
        Float32Scalar.FromSingle(x),
        Float32Scalar.FromSingle(y),
        Float32Scalar.FromSingle(z));
}
