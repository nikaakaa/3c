using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public interface IUnityCharacterSimulationInputAdapter : ISimulationInputAdapter, IDisposable
    {
        void Activate();
        void Deactivate();
        void CaptureRenderFrame(ulong renderFrame);
    }

    public readonly struct CharacterActionTargetInputSample
    {
        public CharacterActionTargetInputSample(ActorId targetId, Vector3 position, float yaw, ulong bodyTick)
        {
            if (!targetId.IsValid)
                throw new ArgumentException("Action target input sample identity is incomplete.");
            TargetId = targetId;
            Position = position;
            Yaw = yaw;
            BodyTick = bodyTick;
        }

        public ActorId TargetId { get; }
        public Vector3 Position { get; }
        public float Yaw { get; }
        public ulong BodyTick { get; }
        public bool IsValid => TargetId.IsValid;
    }

    public interface ICharacterActionTargetInputProvider
    {
        string ProviderIdentity { get; }
        bool TryCapture(CharacterPipelineHost owner, out CharacterActionTargetInputSample sample);
    }

    public abstract class CharacterActionTargetInputProvider : MonoBehaviour, ICharacterActionTargetInputProvider
    {
        public abstract string ProviderIdentity { get; }
        public abstract bool TryCapture(CharacterPipelineHost owner, out CharacterActionTargetInputSample sample);
    }

    public readonly struct CharacterControlSourceContext
    {
        public CharacterControlSourceContext(
            CharacterPipelineHost owner,
            CharacterPipelineDefinition definition,
            CharacterSimulationProgram program)
        {
            Owner = owner ? owner : throw new ArgumentNullException(nameof(owner));
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            Program = program ?? throw new ArgumentNullException(nameof(program));
        }

        public CharacterPipelineHost Owner { get; }
        public CharacterPipelineDefinition Definition { get; }
        public CharacterSimulationProgram Program { get; }
    }

    public abstract class CharacterControlSource : MonoBehaviour
    {
        public abstract string SourceIdentity { get; }
        public abstract IUnityCharacterSimulationInputAdapter Create(CharacterControlSourceContext context);
    }

    public sealed class NeutralCharacterSimulationInputAdapter : IUnityCharacterSimulationInputAdapter
    {
        const string InputPrefix = "input:value:";
        readonly List<SimulationInputValue> m_Values = new List<SimulationInputValue>();
        bool m_Active;
        bool m_Disposed;
        ulong m_RenderFrame;

        public NeutralCharacterSimulationInputAdapter(CharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (program.Manifest.NumericProfile != Float32SimulationNumericProfile.Value)
                throw new ArgumentException("Neutral Character input requires a Float32 Program.", nameof(program));
            for (int i = 0; i < program.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = program.CatalogEntries[i];
                if (entry.Kind != ProgramCatalogEntryKind.InputValue)
                    continue;
                if (!entry.Identity.StartsWith(InputPrefix, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Program input catalog identity '{entry.Identity}' is invalid.");
                string inputId = entry.Identity.Substring(InputPrefix.Length);
                m_Values.Add(CreateNeutralValue(program, entry, inputId));
            }
            m_Values.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            AdapterIdentity = $"NeutralProgramInputs/Float32/{program.ProgramHash}";
        }

        public string AdapterIdentity { get; }
        public SimulationNumericProfile NumericProfile => Float32SimulationNumericProfile.Value;

        public void Activate()
        {
            RequireAlive();
            m_Active = true;
        }

        public void Deactivate()
        {
            m_Active = false;
            m_RenderFrame = 0;
        }

        public void CaptureRenderFrame(ulong renderFrame)
        {
            RequireAlive();
            if (!m_Active || renderFrame == 0 || renderFrame <= m_RenderFrame)
                throw new InvalidOperationException("Neutral Character input requires an active, strictly increasing render frame.");
            m_RenderFrame = renderFrame;
        }

        public CharacterSimulationInput BuildInput(SimulationInputBuildContext context)
        {
            RequireAlive();
            if (!m_Active || m_RenderFrame == 0 || context.NumericProfile != NumericProfile)
                throw new InvalidOperationException("Neutral Character input received an incompatible build context.");
            return new CharacterSimulationInput(
                NumericProfile,
                context.Source,
                AdapterIdentity,
                context.InputSequence,
                m_Values,
                Array.Empty<SimulationInputRequest>());
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Deactivate();
            m_Disposed = true;
            m_Values.Clear();
        }

        static SimulationInputValue CreateNeutralValue(
            CharacterSimulationProgram program,
            ProgramCatalogEntry entry,
            string inputId)
        {
            ProgramCatalogField typeField = null;
            for (int i = 0; i < entry.Fields.Count; i++)
            {
                if (string.Equals(entry.Fields[i].Name, "ValueType", StringComparison.Ordinal))
                {
                    typeField = entry.Fields[i];
                    break;
                }
            }
            if (typeField == null || typeField.Kind != ProgramCatalogFieldKind.Constant)
                throw new InvalidOperationException($"Program input '{inputId}' has no ValueType field.");
            ProgramConstant type = program.Constants[typeField.ConstantIndex];
            if (type.Kind != ProgramConstantKind.Int32)
                throw new InvalidOperationException($"Program input '{inputId}' ValueType is not Int32.");
            var kind = (ProgramInputValueKind)type.Int32;
            return kind switch
            {
                ProgramInputValueKind.Boolean => SimulationInputValue.FromBoolean(inputId, false),
                ProgramInputValueKind.Scalar => SimulationInputValue.FromScalar(inputId, Float32Scalar.Zero),
                ProgramInputValueKind.Vector2 => SimulationInputValue.FromVector2(inputId, Float32Vector2.Zero),
                ProgramInputValueKind.Vector3 => SimulationInputValue.FromVector3(inputId, Float32Vector3.Zero),
                ProgramInputValueKind.Yaw => SimulationInputValue.FromYaw(inputId, Float32Yaw.Zero),
                ProgramInputValueKind.ActionTargetSnapshot => SimulationInputValue.FromActionTargetSnapshot(inputId, SimulationActionTargetSnapshot.None),
                _ => throw new InvalidOperationException($"Program input '{inputId}' has unsupported kind '{kind}'.")
            };
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(NeutralCharacterSimulationInputAdapter));
        }
    }
}

namespace ThirdPersonCharacter.Pipeline
{
    public enum CharacterPresentationRole : byte
    {
        LocalOwner = 1,
        SimulatedActor = 2
    }
}
