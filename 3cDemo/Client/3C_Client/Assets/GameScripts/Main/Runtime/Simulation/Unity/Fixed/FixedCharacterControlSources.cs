using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using FixedCharacterSimulationInput = ThirdPersonSimulation.Fixed.CharacterSimulationInput;
using FixedCharacterSimulationProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
using FixedProgramConstant = ThirdPersonSimulation.Fixed.ProgramConstant;
using FixedSimulationInputRequest = ThirdPersonSimulation.Fixed.SimulationInputRequest;
using FixedSimulationInputValue = ThirdPersonSimulation.Fixed.SimulationInputValue;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    public interface IUnityFixedCharacterControlSourceRuntime :
        IFixedCharacterControlSourceRuntime,
        IDisposable
    {
        void Activate();
        void Deactivate();
        void CaptureRenderFrame(ulong renderFrame);
    }

    public readonly struct FixedCharacterControlSourceContext
    {
        public FixedCharacterControlSourceContext(
            FixedCharacterHost owner,
            FixedCharacterSimulationProgram program)
        {
            Owner = owner ? owner : throw new ArgumentNullException(nameof(owner));
            Program = program ?? throw new ArgumentNullException(nameof(program));
        }

        public FixedCharacterHost Owner { get; }
        public FixedCharacterSimulationProgram Program { get; }
    }

    public abstract class FixedCharacterControlSource : MonoBehaviour
    {
        public abstract string SourceIdentity { get; }
        public abstract IUnityFixedCharacterControlSourceRuntime Create(FixedCharacterControlSourceContext context);
    }

    public sealed class NeutralFixedCharacterSimulationInputAdapter : IUnityFixedCharacterControlSourceRuntime
    {
        const string InputPrefix = "input:value:";
        readonly List<FixedSimulationInputValue> m_Values = new List<FixedSimulationInputValue>();
        readonly ProgramId m_ProgramId;
        readonly ProgramHash m_ProgramHash;
        bool m_Active;
        bool m_Disposed;
        ulong m_RenderFrame;

        public NeutralFixedCharacterSimulationInputAdapter(FixedCharacterSimulationProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (program.Manifest.NumericProfile != FixedSimulationNumericProfile.Value)
                throw new ArgumentException("Neutral Fixed Character input requires a FixedQ32.32 Program.", nameof(program));
            m_ProgramId = program.Manifest.ProgramId;
            m_ProgramHash = program.ProgramHash;
            for (int i = 0; i < program.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry entry = program.CatalogEntries[i];
                if (entry.Kind != ProgramCatalogEntryKind.InputValue)
                    continue;
                if (!entry.Identity.StartsWith(InputPrefix, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Fixed Program input catalog identity '{entry.Identity}' is invalid.");
                string inputId = entry.Identity.Substring(InputPrefix.Length);
                m_Values.Add(CreateNeutralValue(program, entry, inputId));
            }
            m_Values.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            SourceIdentity = $"NeutralProgramInputs/FixedQ32.32/{program.ProgramHash}";
        }

        public string SourceIdentity { get; }
        public ProgramId CharacterProgramId => m_ProgramId;
        public ProgramHash CharacterProgramHash => m_ProgramHash;

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
                throw new InvalidOperationException("Neutral Fixed Character input requires an active, strictly increasing render frame.");
            m_RenderFrame = renderFrame;
        }

        public FixedCharacterSimulationInput BuildInput(FixedCharacterInputBuildContext context)
        {
            RequireAlive();
            if (!m_Active || m_RenderFrame == 0)
                throw new InvalidOperationException("Neutral Fixed Character input has no captured render frame.");
            return new FixedCharacterSimulationInput(
                FixedSimulationNumericProfile.Value,
                context.Source,
                SourceIdentity,
                context.InputSequence,
                m_Values,
                Array.Empty<FixedSimulationInputRequest>());
        }

        public byte[] CaptureState()
        {
            using var writer = new CanonicalWriter();
            writer.WriteUInt32(0x4e584655);
            writer.WriteInt32(1);
            writer.WriteString(SourceIdentity);
            return writer.ToArray();
        }

        public void RestoreState(byte[] state)
        {
            var reader = new CanonicalReader(state ?? throw new ArgumentNullException(nameof(state)));
            if (reader.ReadUInt32() != 0x4e584655 || reader.ReadInt32() != 1 ||
                !string.Equals(reader.ReadString(), SourceIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Neutral Fixed Character input state identity is invalid.");
            }
            reader.RequireComplete();
        }

        public void NotifyStateDisposition(FixedCharacterControlSourceStateDisposition disposition)
        {
            if (!Enum.IsDefined(typeof(FixedCharacterControlSourceStateDisposition), disposition))
                throw new ArgumentOutOfRangeException(nameof(disposition));
        }

        public FixedCharacterControlSourceDiagnosticsSnapshot CaptureDiagnostics() =>
            new FixedCharacterControlSourceDiagnosticsSnapshot(0, 0, 0);

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Deactivate();
            m_Disposed = true;
            m_Values.Clear();
        }

        static FixedSimulationInputValue CreateNeutralValue(
            FixedCharacterSimulationProgram program,
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
                throw new InvalidOperationException($"Fixed Program input '{inputId}' has no ValueType field.");
            FixedProgramConstant type = program.Constants[typeField.ConstantIndex];
            if (type.Kind != ThirdPersonSimulation.Fixed.ProgramConstantKind.Int32)
                throw new InvalidOperationException($"Fixed Program input '{inputId}' ValueType is not Int32.");
            var kind = (ProgramInputValueKind)type.Int32;
            return kind switch
            {
                ProgramInputValueKind.Boolean => FixedSimulationInputValue.FromBoolean(inputId, false),
                ProgramInputValueKind.Scalar => FixedSimulationInputValue.FromScalar(inputId, FixedScalar.Zero),
                ProgramInputValueKind.Vector2 => FixedSimulationInputValue.FromVector2(inputId, FixedVector2.Zero),
                ProgramInputValueKind.Vector3 => FixedSimulationInputValue.FromVector3(inputId, FixedVector3.Zero),
                ProgramInputValueKind.Yaw => FixedSimulationInputValue.FromYaw(inputId, FixedYaw.Zero),
                ProgramInputValueKind.ActionTargetSnapshot => FixedSimulationInputValue.FromActionTargetSnapshot(
                    inputId,
                    ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot.None),
                _ => throw new InvalidOperationException($"Fixed Program input '{inputId}' has unsupported kind '{kind}'.")
            };
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(NeutralFixedCharacterSimulationInputAdapter));
        }
    }
}
