using System;
using System.Collections.Generic;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEngine;
using FixedSimulationInput = ThirdPersonSimulation.Fixed.SimulationInput;
using FixedSimulationInputRequest = ThirdPersonSimulation.Fixed.SimulationInputRequest;
using FixedSimulationInputValue = ThirdPersonSimulation.Fixed.SimulationInputValue;

namespace ThirdPersonCharacter.Pipeline.Simulation.Fixed
{
    internal sealed class FixedCharacterInputCatalog
    {
        const string InputValuePrefix = "input:value:";

        readonly FixedSimulationInputValue[] m_NeutralValues;
        readonly string[] m_ActionTargetInputIds;
        readonly HashSet<string> m_InputValueIds;
        readonly HashSet<string> m_ActionTargetInputIdSet;

        FixedCharacterInputCatalog(CharacterPipelineDefinition definition)
        {
            if (!definition)
                throw new ArgumentNullException(nameof(definition));
            GameplayAbilityProviderBinding providers = definition.BuildGameplayAbilityProviderBinding();
            if (!providers.TryGetEntry(GameplayAbilityProviderKind.Input, out GameplayAbilityProviderBindingEntry inputProvider))
                throw new InvalidOperationException("Fixed Character input catalog requires an Input provider.");

            var values = new List<FixedSimulationInputValue>();
            var actionTargetInputIds = new List<string>();
            m_InputValueIds = new HashSet<string>(StringComparer.Ordinal);
            m_ActionTargetInputIdSet = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < inputProvider.Members.Count; i++)
            {
                GameplayAbilityProviderMemberBinding member = inputProvider.Members[i];
                if (!member.MemberIdentity.StartsWith(InputValuePrefix, StringComparison.Ordinal))
                    continue;
                string inputId = member.MemberIdentity.Substring(InputValuePrefix.Length);
                if (string.IsNullOrEmpty(inputId) || !m_InputValueIds.Add(inputId))
                    throw new InvalidOperationException("Fixed Character input catalog contains duplicate or invalid input values.");
                values.Add(CreateNeutralValue(inputId, member.ValueKind));
                if (member.ValueKind == GameplayAbilityProviderValueKind.ActionTargetSnapshot)
                {
                    m_ActionTargetInputIdSet.Add(inputId);
                    actionTargetInputIds.Add(inputId);
                }
            }
            values.Sort((left, right) => string.CompareOrdinal(left.InputId, right.InputId));
            actionTargetInputIds.Sort(StringComparer.Ordinal);
            m_NeutralValues = values.ToArray();
            m_ActionTargetInputIds = actionTargetInputIds.ToArray();
        }

        public IReadOnlyList<FixedSimulationInputValue> NeutralValues => m_NeutralValues;
        public IReadOnlyList<string> ActionTargetInputIds => m_ActionTargetInputIds;

        public bool ContainsInputValue(string inputId) =>
            !string.IsNullOrEmpty(inputId) && m_InputValueIds.Contains(inputId);

        public bool ContainsActionTargetInput(string inputId) =>
            !string.IsNullOrEmpty(inputId) && m_ActionTargetInputIdSet.Contains(inputId);

        public static FixedCharacterInputCatalog Create(CharacterPipelineDefinition definition) =>
            new FixedCharacterInputCatalog(definition);

        static FixedSimulationInputValue CreateNeutralValue(
            string inputId,
            GameplayAbilityProviderValueKind valueKind) => valueKind switch
        {
            GameplayAbilityProviderValueKind.Boolean => FixedSimulationInputValue.FromBoolean(inputId, false),
            GameplayAbilityProviderValueKind.Number => FixedSimulationInputValue.FromScalar(inputId, FixedScalar.Zero),
            GameplayAbilityProviderValueKind.Vector2 => FixedSimulationInputValue.FromVector2(inputId, FixedVector2.Zero),
            GameplayAbilityProviderValueKind.Vector3 => FixedSimulationInputValue.FromVector3(inputId, FixedVector3.Zero),
            GameplayAbilityProviderValueKind.Yaw => FixedSimulationInputValue.FromYaw(inputId, FixedYaw.Zero),
            GameplayAbilityProviderValueKind.ActionTargetSnapshot => FixedSimulationInputValue.FromActionTargetSnapshot(
                inputId,
                ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot.None),
            _ => throw new InvalidOperationException($"Fixed Character input '{inputId}' has unsupported type '{valueKind}'.")
        };

    }

    public interface IUnityFixedCharacterControlSourceRuntime :
        IFixedCharacterControlSourceRuntime,
        IDisposable
    {
        void Activate();
        void Deactivate();
        void CaptureRenderFrame(ulong renderFrame);
        void EnqueueRequest(string requestId);
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
            InputCatalog = FixedCharacterInputCatalog.Create(Definition);
        }

        public FixedCharacterHost Owner { get; }
        public CharacterPipelineDefinition Definition { get; }
        public CharacterControlModuleContract ControlModule { get; }
        internal FixedCharacterInputCatalog InputCatalog { get; }
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

        internal NeutralFixedSimulationInputAdapter(FixedCharacterInputCatalog inputCatalog)
        {
            if (inputCatalog == null)
                throw new ArgumentNullException(nameof(inputCatalog));
            m_Values.AddRange(inputCatalog.NeutralValues);
            SourceIdentity = "neutral-character-inputs/fixed-q32-32";
        }

        public string SourceIdentity { get; }

        public void EnqueueRequest(string requestId) =>
            throw new InvalidOperationException($"Neutral character input source has no request binding '{requestId}'.");

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
            if (disposition < FixedCharacterControlSourceStateDisposition.Prepared ||
                disposition > FixedCharacterControlSourceStateDisposition.Restored)
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
                throw new ObjectDisposedException(nameof(NeutralFixedSimulationInputAdapter));
        }
    }
}
