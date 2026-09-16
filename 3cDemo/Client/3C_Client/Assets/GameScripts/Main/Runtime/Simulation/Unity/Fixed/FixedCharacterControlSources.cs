using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using FixedSimulationInput = ThirdPersonSimulation.Fixed.SimulationInput;
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
            CharacterPipelineDefinition definition,
            CharacterControlModuleContract controlModule)
        {
            Owner = owner ? owner : throw new ArgumentNullException(nameof(owner));
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
        }

        public FixedCharacterHost Owner { get; }
        public CharacterPipelineDefinition Definition { get; }
        public CharacterControlModuleContract ControlModule { get; }
    }

    public abstract class FixedCharacterControlSource : MonoBehaviour
    {
        public abstract string SourceIdentity { get; }
        public abstract IUnityFixedCharacterControlSourceRuntime Create(FixedCharacterControlSourceContext context);
    }

    public sealed class NeutralFixedSimulationInputAdapter : IUnityFixedCharacterControlSourceRuntime
    {
        readonly List<FixedSimulationInputValue> m_Values = new List<FixedSimulationInputValue>();
        bool m_Active;
        bool m_Disposed;
        ulong m_RenderFrame;

        public NeutralFixedSimulationInputAdapter(CharacterInputProfile profile)
        {
            if (profile)
            {
                for (int i = 0; i < profile.InputValues.Count; i++)
                    m_Values.Add(CreateNeutralValue(profile.InputValues[i]));
            }
            m_Values.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            SourceIdentity = "neutral-character-inputs/fixed-q32-32";
        }

        public string SourceIdentity { get; }

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

        public FixedSimulationInput BuildInput(FixedCharacterInputBuildContext context)
        {
            RequireAlive();
            if (!m_Active || m_RenderFrame == 0)
                throw new InvalidOperationException("Neutral Fixed Character input has no captured render frame.");
            return new FixedSimulationInput(
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

        static FixedSimulationInputValue CreateNeutralValue(CharacterInputValueDefinition definition)
        {
            if (definition == null)
                throw new InvalidOperationException("Character Input Profile contains a missing input value.");
            return definition.ValueType switch
            {
                CharacterInputValueType.Bool => FixedSimulationInputValue.FromBoolean(definition.InputValueId, false),
                CharacterInputValueType.Float => FixedSimulationInputValue.FromScalar(definition.InputValueId, FixedScalar.Zero),
                CharacterInputValueType.Vector2 => FixedSimulationInputValue.FromVector2(definition.InputValueId, FixedVector2.Zero),
                _ => throw new InvalidOperationException($"Fixed character input '{definition.InputValueId}' has unsupported type '{definition.ValueType}'.")
            };
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(NeutralFixedSimulationInputAdapter));
        }
    }
}
