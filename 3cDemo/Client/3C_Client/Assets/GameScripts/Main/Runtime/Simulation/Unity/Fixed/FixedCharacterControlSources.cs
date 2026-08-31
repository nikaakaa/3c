using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using FixedCharacterSimulationInput = ThirdPersonSimulation.Fixed.CharacterSimulationInput;
using FixedCharacterSimulationProgram = ThirdPersonSimulation.Fixed.CharacterSimulationProgram;
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
        readonly List<FixedSimulationInputValue> m_Values;
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
            m_Values = FixedProgramNeutralInputValues.Create(program);
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

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(NeutralFixedCharacterSimulationInputAdapter));
        }
    }
}
