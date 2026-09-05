using System;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedCharacterControlOutputPort : ICharacterControlOutputPort
    {
        readonly FixedProgramAccess m_Access;
        readonly ProgramCatalogEntry m_ControlModule;
        readonly FixedInputRuntime m_Input;
        readonly FixedLocomotionRuntime m_Locomotion;
        readonly FixedActionRuntime m_Actions;
        readonly FixedTraceSink m_Trace;

        public FixedCharacterControlOutputPort(
            FixedProgramAccess access,
            ProgramCatalogEntry controlModule,
            FixedInputRuntime input,
            FixedLocomotionRuntime locomotion,
            FixedActionRuntime actions,
            FixedTraceSink trace)
        {
            m_Access = access ?? throw new ArgumentNullException(nameof(access));
            m_ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
            m_Input = input ?? throw new ArgumentNullException(nameof(input));
            m_Locomotion = locomotion ?? throw new ArgumentNullException(nameof(locomotion));
            m_Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            m_Trace = trace ?? throw new ArgumentNullException(nameof(trace));
        }

        public void SubmitMotion(CharacterControlMotionRequest request)
        {
            CharacterControlMotionDescriptor descriptor = RequireMotion(request.Binding);
            m_Locomotion.SubmitControl(m_Input, request, descriptor);
        }

        public bool SubmitSkill(CharacterControlSkillRequest request) => m_Actions.ActivateFromControl(request);

        public void SubmitSkillStop(CharacterControlSkillStopRequest request) => m_Actions.StopFromControl(request);

        public void Trace(SimulationExecutionSource source, string code, string detail) =>
            m_Trace.Add(source, code, SimulationTraceSeverity.Information, detail);

        CharacterControlMotionDescriptor RequireMotion(string binding)
        {
            string prefix = $"Motion:{binding}:";
            string input = RequireIdentity(prefix + "Input");
            return new CharacterControlMotionDescriptor(
                binding,
                new SimulationInputValueId(input),
                RequireConstant(prefix + "MoveSpeed", ProgramConstantKind.Scalar).Scalar.ToDouble(),
                RequireConstant(prefix + "TurnSpeedDegrees", ProgramConstantKind.Scalar).Scalar.ToDouble(),
                (CharacterControlMotionExecutionMode)RequireConstant(prefix + "ExecutionMode", ProgramConstantKind.Int32).Int32,
                RequireConstant(prefix + "DurationSeconds", ProgramConstantKind.Scalar).Scalar.ToDouble(),
                FindIdentity(prefix + "SourceMotion"),
                (CharacterControlMotionDisplacementMode)RequireConstant(prefix + "DisplacementMode", ProgramConstantKind.Int32).Int32,
                (CharacterControlMotionSpace)RequireConstant(prefix + "Space", ProgramConstantKind.Int32).Int32,
                RequireConstant(prefix + "Priority", ProgramConstantKind.Int32).Int32,
                RequireConstant(prefix + "ConsumeLowerChannels", ProgramConstantKind.Boolean).Boolean);
        }

        string FindIdentity(string name)
        {
            for (int i = 0; i < m_ControlModule.Fields.Count; i++)
            {
                ProgramCatalogField field = m_ControlModule.Fields[i];
                if (string.Equals(field.Name, name, StringComparison.Ordinal))
                {
                    if (field.Kind != ProgramCatalogFieldKind.Identity)
                        throw new InvalidOperationException($"Control module field '{name}' is not an identity.");
                    return field.Identity;
                }
            }
            return string.Empty;
        }

        string RequireIdentity(string name)
        {
            string value = FindIdentity(name);
            if (string.IsNullOrEmpty(value))
                throw new InvalidOperationException($"Control module '{m_ControlModule.Identity}' has no '{name}' field.");
            return value;
        }

        ProgramConstant RequireConstant(string name, ProgramConstantKind kind)
        {
            for (int i = 0; i < m_ControlModule.Fields.Count; i++)
            {
                ProgramCatalogField field = m_ControlModule.Fields[i];
                if (!string.Equals(field.Name, name, StringComparison.Ordinal))
                    continue;
                if (field.Kind != ProgramCatalogFieldKind.Constant || field.ConstantIndex < 0 ||
                    field.ConstantIndex >= m_Access.Program.Constants.Count)
                    throw new InvalidOperationException($"Control module field '{name}' is not a valid constant.");
                ProgramConstant value = m_Access.Program.Constants[field.ConstantIndex];
                if (value.Kind != kind)
                    throw new InvalidOperationException($"Control module field '{name}' has kind '{value.Kind}', expected '{kind}'.");
                return value;
            }
            throw new InvalidOperationException($"Control module '{m_ControlModule.Identity}' has no '{name}' field.");
        }
    }
}
