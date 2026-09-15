using System;
using System.Collections.Generic;
using ThirdPersonCamera;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public interface ISimulationSessionActorHost
    {
        ActorId SimulationActorId { get; }
        SimulationSessionHost SessionHost { get; }
    }

    public interface IUnityCharacterControlSourceRuntime : ICharacterControlSourceRuntime, IDisposable
    {
        void Activate();
        void Deactivate();
        void CaptureRenderFrame(ulong renderFrame);
    }

    public interface ICharacterActionTargetInputProvider
    {
        string ProviderIdentity { get; }
        bool TryGetTargetActorId(ISimulationSessionActorHost owner, out ActorId actorId);
    }

    public abstract class CharacterActionTargetInputProvider : MonoBehaviour, ICharacterActionTargetInputProvider
    {
        public abstract string ProviderIdentity { get; }
        public abstract bool TryGetTargetActorId(ISimulationSessionActorHost owner, out ActorId actorId);
    }

    public readonly struct CharacterControlSourceContext
    {
        public CharacterControlSourceContext(
            ISimulationSessionActorHost owner,
            ICameraBasisSnapshotProvider cameraBasis,
            CharacterPipelineDefinition definition,
            CharacterControlModuleContract controlModule)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            CameraBasis = cameraBasis ?? throw new ArgumentNullException(nameof(cameraBasis));
            Definition = definition ? definition : throw new ArgumentNullException(nameof(definition));
            ControlModule = controlModule ?? throw new ArgumentNullException(nameof(controlModule));
        }

        public ISimulationSessionActorHost Owner { get; }
        public ICameraBasisSnapshotProvider CameraBasis { get; }
        public CharacterPipelineDefinition Definition { get; }
        public CharacterControlModuleContract ControlModule { get; }
    }

    public abstract class CharacterControlSource : MonoBehaviour
    {
        public abstract string SourceIdentity { get; }
        public abstract IUnityCharacterControlSourceRuntime Create(CharacterControlSourceContext context);
    }

    public sealed class NeutralCharacterSimulationInputAdapter : IUnityCharacterControlSourceRuntime
    {
        readonly List<SimulationInputValue> m_Values = new List<SimulationInputValue>();
        bool m_Active;
        bool m_Disposed;
        ulong m_RenderFrame;

        public NeutralCharacterSimulationInputAdapter(CharacterInputProfile profile)
        {
            if (profile)
            {
                for (int i = 0; i < profile.InputValues.Count; i++)
                    m_Values.Add(CreateNeutralValue(profile.InputValues[i]));
            }
            m_Values.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            SourceIdentity = "neutral-character-inputs/float32";
        }

        public string SourceIdentity { get; }
        public SimulationNumericProfile NumericProfile => Float32SimulationNumericProfile.Value;
        public CharacterControlSourceCapability Capabilities => CharacterControlSourceCapability.None;

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
                SourceIdentity,
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

        static SimulationInputValue CreateNeutralValue(CharacterInputValueDefinition definition)
        {
            if (definition == null)
                throw new InvalidOperationException("Character Input Profile contains a missing input value.");
            return definition.ValueType switch
            {
                CharacterInputValueType.Bool => SimulationInputValue.FromBoolean(definition.InputValueId, false),
                CharacterInputValueType.Float => SimulationInputValue.FromScalar(definition.InputValueId, Float32Scalar.Zero),
                CharacterInputValueType.Vector2 => SimulationInputValue.FromVector2(definition.InputValueId, Float32Vector2.Zero),
                _ => throw new InvalidOperationException($"Character input '{definition.InputValueId}' has unsupported type '{definition.ValueType}'.")
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
