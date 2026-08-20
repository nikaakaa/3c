using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    [DisallowMultipleComponent]
    public sealed class ServerAuthoritativeRemotePresentationSite : MonoBehaviour
    {
        [SerializeField] string m_BindingId = string.Empty;
        [SerializeField] CharacterPipelineDefinition m_CharacterDefinition;
        [SerializeField] GameObject m_CharacterTemplate;
        [SerializeField] Vector3 m_SpawnPosition;
        [SerializeField] Vector3 m_SpawnEulerAngles;
        [SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;

        ServerAuthoritativeRemotePresentationRegistration m_Registration;

        public string BindingId => string.IsNullOrWhiteSpace(m_BindingId)
            ? throw new InvalidOperationException($"Remote Presentation Site '{name}' requires an explicit BindingId.")
            : m_BindingId.Trim();

        internal ServerAuthoritativeRemotePresentationRegistration Claim(
            ActorId actorId,
            CharacterSimulationProgram ownerProgram,
            int tickRate,
            ISimulationDiagnosticsSink diagnostics)
        {
            if (m_Registration != null)
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' is already claimed.");
            if (!isActiveAndEnabled)
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' is not active.");
            if (!m_CharacterDefinition || !m_CharacterDefinition.SimulationProgram ||
                !m_CharacterDefinition.PresentationProjection)
            {
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' requires compiled Character assets.");
            }
            if (!m_CharacterTemplate)
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' requires an explicit Character Root template.");
            if (tickRate <= 0 || !m_BodyPresentationProfile)
            {
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' requires a formal Presentation Profile.");
            }

            CharacterSimulationProgram program = m_CharacterDefinition.SimulationProgram.Load();
            if (!program.ProgramHash.Equals(ownerProgram.ProgramHash) || !program.LayoutHash.Equals(ownerProgram.LayoutHash))
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' Program does not match the owner Session Program.");
            CharacterPresentationProjection projection = m_CharacterDefinition.PresentationProjection.Load(
                Float32CharacterPresentationContractAdapter.Create(program));
            GameObject characterObject = Instantiate(m_CharacterTemplate, m_SpawnPosition, Quaternion.Euler(m_SpawnEulerAngles));
            characterObject.name = $"{m_CharacterTemplate.name} [Remote {actorId.Value}]";
            CharacterRootHierarchyBinding rootHierarchy = characterObject.GetComponent<CharacterRootHierarchyBinding>();
            if (!rootHierarchy)
            {
                Destroy(characterObject);
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' template requires a Root Hierarchy Binding on LogicRoot.");
            }
            try
            {
                rootHierarchy.RequireValid();
            }
            catch
            {
                Destroy(characterObject);
                throw;
            }
            AnimancerComponent animancer = rootHierarchy.PoseRoot.GetComponent<AnimancerComponent>();
            CharacterAnimationRigBinding animationRigBinding =
                rootHierarchy.PoseRoot.GetComponent<CharacterAnimationRigBinding>();
            if (!animancer || !animancer.Animator || !animationRigBinding)
            {
                Destroy(characterObject);
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' PoseRoot requires Animancer, Animator, and Animation Rig Binding.");
            }
            animationRigBinding.RequireValid(projection.Rig);
            if (animancer.Animator.transform != rootHierarchy.PoseRoot)
            {
                Destroy(characterObject);
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' Animator must use PoseRoot.");
            }
            CharacterWorldAwarePresentationBinding worldAwarePresentation =
                rootHierarchy.VisualRoot.GetComponent<CharacterWorldAwarePresentationBinding>();
            if (!worldAwarePresentation)
            {
                Destroy(characterObject);
                throw new InvalidOperationException($"Remote Presentation Site '{BindingId}' VisualRoot requires a World-Aware Presentation Binding.");
            }
            worldAwarePresentation.RequireValid();
            PhysicsScene physicsScene = characterObject.scene.GetPhysicsScene();
            WorldBodyState initialBody = BuildInitialBody(actorId, rootHierarchy.LogicRoot);
            CharacterRuntimeDebugProgram debugProgram = CharacterRuntimeDebugProgramBuilder.Build(program);
            var diagnosticsContext = new RuntimeDiagnosticsContext(
                Guid.NewGuid(),
                Guid.NewGuid(),
                debugProgram.Revision,
                debugProgram.SourceMap,
                new RuntimeDiagnosticsStore());
            var diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
            ICharacterPresentationRuntime runtime = null;
            ServerAuthoritativeRemotePresentationRegistration registration = null;
            try
            {
                CharacterPresentationRuntimeBinding presentationBinding =
                    CharacterPresentationRuntimeFactory.CreateObservedActor(
                    Float32CharacterPresentationContractAdapter.Create(program),
                    tickRate,
                    projection,
                    actorId,
                    animancer,
                    animationRigBinding,
                    rootHierarchy,
                    CharacterPresentationBodyState.FromFloat32(initialBody),
                    m_BodyPresentationProfile,
                    worldAwarePresentation,
                    physicsScene,
                    null,
                    diagnosticsContext);
                runtime = presentationBinding.Runtime;
                registration = new ServerAuthoritativeRemotePresentationRegistration(
                    BindingId,
                    actorId,
                    tickRate,
                    diagnostics,
                    runtime,
                    diagnosticsTarget,
                    characterObject,
                    rootHierarchy,
                    Release);
                runtime = null;
                diagnosticsTarget = null;
                registration.Activate();
                m_Registration = registration;
                return registration;
            }
            catch
            {
                registration?.Dispose();
                runtime?.Dispose();
                diagnosticsTarget?.Terminate();
                diagnosticsTarget?.Dispose();
                if (registration == null && characterObject)
                    Destroy(characterObject);
                throw;
            }
        }

        void OnEnable()
        {
            ServerAuthoritativeRemotePresentationSiteRegistry.Register(this);
        }

        void OnDisable()
        {
            ServerAuthoritativeRemotePresentationSiteRegistry.Unregister(this);
            m_Registration?.Dispose();
        }

        void OnDestroy()
        {
            ServerAuthoritativeRemotePresentationSiteRegistry.Unregister(this);
            m_Registration?.Dispose();
        }

        void Release(ServerAuthoritativeRemotePresentationRegistration registration)
        {
            if (ReferenceEquals(m_Registration, registration))
                m_Registration = null;
        }

        static WorldBodyState BuildInitialBody(ActorId actorId, Transform logicRoot)
        {
            Vector3 position = logicRoot.position;
            return new WorldBodyState(
                actorId,
                new Float32Vector3(
                    Float32Scalar.FromSingle(position.x),
                    Float32Scalar.FromSingle(position.y),
                    Float32Scalar.FromSingle(position.z)),
                new Float32Yaw(Float32Scalar.FromSingle(logicRoot.eulerAngles.y)),
                Float32Vector3.Zero,
                Float32Scalar.Zero,
                false,
                WorldCollisionSummary.None);
        }
    }

    internal static class ServerAuthoritativeRemotePresentationSiteRegistry
    {
        static readonly Dictionary<string, ServerAuthoritativeRemotePresentationSite> s_Sites =
            new Dictionary<string, ServerAuthoritativeRemotePresentationSite>(StringComparer.Ordinal);

        public static void Register(ServerAuthoritativeRemotePresentationSite site)
        {
            if (!site)
                throw new ArgumentNullException(nameof(site));
            string bindingId = site.BindingId;
            if (s_Sites.TryGetValue(bindingId, out ServerAuthoritativeRemotePresentationSite current) && current != site)
                throw new InvalidOperationException($"Remote Presentation BindingId '{bindingId}' is registered more than once.");
            s_Sites[bindingId] = site;
        }

        public static void Unregister(ServerAuthoritativeRemotePresentationSite site)
        {
            if (!site)
                return;
            string bindingId = site.BindingId;
            if (s_Sites.TryGetValue(bindingId, out ServerAuthoritativeRemotePresentationSite current) && current == site)
                s_Sites.Remove(bindingId);
        }

        public static ServerAuthoritativeRemotePresentationRegistration Claim(
            string bindingId,
            ActorId actorId,
            CharacterSimulationProgram ownerProgram,
            int tickRate,
            ISimulationDiagnosticsSink diagnostics)
        {
            string identity = string.IsNullOrWhiteSpace(bindingId)
                ? throw new ArgumentException("Remote Presentation BindingId is required.", nameof(bindingId))
                : bindingId.Trim();
            if (!s_Sites.TryGetValue(identity, out ServerAuthoritativeRemotePresentationSite site) || !site)
                throw new InvalidOperationException($"Remote Presentation Site '{identity}' is not registered by the active Client Scene.");
            return site.Claim(actorId, ownerProgram, tickRate, diagnostics);
        }
    }

    internal sealed class ServerAuthoritativeRemotePresentationRegistration : IDisposable
    {
        readonly int m_TickRate;
        readonly ICharacterPresentationRuntime m_Runtime;
        readonly CharacterSimulationGameplayOutputBuffer m_Gameplay = new CharacterSimulationGameplayOutputBuffer();
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        readonly GameObject m_CharacterObject;
        readonly CharacterRootHierarchyBinding m_RootHierarchy;
        readonly ServerAuthoritativeRemotePresentationFrameTarget m_PresentationTarget;
        readonly Action<ServerAuthoritativeRemotePresentationRegistration> m_Release;
        readonly SortedDictionary<ulong, List<PresentationCommand>> m_Commands =
            new SortedDictionary<ulong, List<PresentationCommand>>();
        readonly SortedDictionary<ulong, List<ServerAuthoritativeReliableEvent>> m_Reliable =
            new SortedDictionary<ulong, List<ServerAuthoritativeReliableEvent>>();

        ulong m_LastReliableSequence;
        EventId m_LastReliableEventId;
        ulong m_LastDiagnosticsTick;
        ulong m_SelectedTick;
        bool m_Activated;
        bool m_Disposed;

        public ServerAuthoritativeRemotePresentationRegistration(
            string bindingId,
            ActorId actorId,
            int tickRate,
            ISimulationDiagnosticsSink diagnostics,
            ICharacterPresentationRuntime runtime,
            RuntimeDiagnosticsTarget diagnosticsTarget,
            GameObject characterObject,
            CharacterRootHierarchyBinding rootHierarchy,
            Action<ServerAuthoritativeRemotePresentationRegistration> release)
        {
            BindingId = string.IsNullOrWhiteSpace(bindingId)
                ? throw new ArgumentException("Remote Presentation BindingId is required.", nameof(bindingId))
                : bindingId.Trim();
            if (!actorId.IsValid || tickRate <= 0)
            {
                throw new ArgumentException("Remote Presentation registration configuration is invalid.");
            }
            ActorId = actorId;
            m_TickRate = tickRate;
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            m_CharacterObject = characterObject ? characterObject : throw new ArgumentNullException(nameof(characterObject));
            m_RootHierarchy = rootHierarchy ? rootHierarchy : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            m_Release = release ?? throw new ArgumentNullException(nameof(release));
            m_PresentationTarget = new ServerAuthoritativeRemotePresentationFrameTarget(this);
        }

        public string BindingId { get; }
        public ActorId ActorId { get; }
        public IReadOnlyList<CharacterGameplayOutputChange> CurrentGameplayChanges => m_Gameplay.CurrentTickChanges;
        public EventId LastReliableEventId => m_LastReliableEventId;

        public void Activate()
        {
            RequireAlive();
            if (m_Activated)
                return;
            RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
            try
            {
                if (!GameplayTickSystem.RegisterPresentationTarget(m_PresentationTarget))
                    throw new InvalidOperationException("GameplayTickSystem rejected the remote Presentation target.");
                m_Activated = true;
            }
            catch
            {
                RuntimeDiagnosticsTargetRegistry.Unregister(m_DiagnosticsTarget);
                throw;
            }
        }

        public void Commit(RemotePresentationBatch batch)
        {
            RequireAlive();
            if (!m_Activated || batch == null || batch.ActorId != ActorId)
                throw new InvalidOperationException("Remote Presentation batch does not match the active registration.");
            if (batch.ResetBodyStream && batch.BodySamples.Count == 0)
                throw new InvalidOperationException("Remote selected Body stream reset requires an explicit anchor interval.");
            m_Gameplay.BeginTick();
            CharacterPresentationBodyState finalBody = default;
            bool hasFinalBody = false;
            for (int i = 0; i < batch.BodySamples.Count; i++)
            {
                CharacterBodySample sample = batch.BodySamples[i];
                m_Runtime.CaptureBodyInterval(
                    CharacterPresentationBodyInterval.FromFloat32(
                        sample,
                        m_TickRate,
                        batch.ResetBodyStream && i == 0
                            ? CharacterPresentationBodyStreamUpdateKind.Reset
                            : CharacterPresentationBodyStreamUpdateKind.Append));
                finalBody = CharacterPresentationBodyState.FromFloat32(sample.FinalBody);
                hasFinalBody = true;
                m_SelectedTick = sample.Tick.Value;
            }
            for (int i = 0; i < batch.SampleCommands.Count; i++)
                Enqueue(m_Commands, batch.SampleCommands[i].Header.Tick.Value, batch.SampleCommands[i]);
            for (int i = 0; i < batch.ReliableEvents.Count; i++)
                Enqueue(m_Reliable, batch.ReliableEvents[i].Header.Tick.Value, batch.ReliableEvents[i]);
            if (hasFinalBody)
                m_RootHierarchy.ApplyLogicPose(finalBody.Position, finalBody.Rotation);
        }

        public void Present(GameplayPresentationFrameContext context)
        {
            RequireAlive();
            if (m_SelectedTick == 0)
                return;
            PublishDue(m_SelectedTick, m_SelectedTick);
            m_Runtime.Present(context);
            PublishPresentationHorizonDiagnostics();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_Activated)
            {
                GameplayTickSystem.UnregisterPresentationTarget(m_PresentationTarget);
                RuntimeDiagnosticsTargetRegistry.Unregister(m_DiagnosticsTarget);
                m_Activated = false;
            }
            m_DiagnosticsTarget.Terminate();
            m_DiagnosticsTarget.Dispose();
            m_Runtime.Dispose();
            m_Commands.Clear();
            m_Reliable.Clear();
            if (m_CharacterObject)
                UnityEngine.Object.Destroy(m_CharacterObject);
            m_Release(this);
        }

        void Publish(ServerAuthoritativeReliableEvent value)
        {
            SimulationEventHeader header = value.Header;
            if (header.ActorId != ActorId)
                throw new InvalidOperationException("Remote reliable event targets another Actor.");
            if (header.Sequence <= m_LastReliableSequence)
                return;
            m_LastReliableSequence = header.Sequence;
            m_LastReliableEventId = header.EventId;
            if (value.IsGameplay)
            {
                m_Gameplay.Publish(value.GameplayFact);
                return;
            }
            if (value.PresentationCommand.Kind == PresentationCommandKind.Camera)
                throw new InvalidOperationException("Remote replication cannot contain Camera commands.");
            m_Runtime.Publish(CharacterPresentationCommand.FromFloat32(value.PresentationCommand));
        }

        void PublishDue(ulong sampleTick, ulong reliableTick)
        {
            m_Gameplay.BeginTick();
            PublishDue(
                m_Commands,
                sampleTick,
                value => m_Runtime.Publish(CharacterPresentationCommand.FromFloat32(value)));
            PublishDue(m_Reliable, reliableTick, Publish);
        }

        static void PublishDue<T>(SortedDictionary<ulong, List<T>> queue, ulong authorityTick, Action<T> publish)
        {
            var due = new List<ulong>();
            foreach (KeyValuePair<ulong, List<T>> pair in queue)
            {
                if (pair.Key > authorityTick)
                    break;
                for (int i = 0; i < pair.Value.Count; i++)
                    publish(pair.Value[i]);
                due.Add(pair.Key);
            }
            for (int i = 0; i < due.Count; i++)
                queue.Remove(due[i]);
        }

        static void Enqueue<T>(SortedDictionary<ulong, List<T>> queue, ulong tick, T value)
        {
            if (tick == 0)
                throw new InvalidOperationException("Remote presentation output has no authority Tick.");
            if (!queue.TryGetValue(tick, out List<T> values))
            {
                values = new List<T>();
                queue.Add(tick, values);
            }
            values.Add(value);
        }

        void PublishPresentationHorizonDiagnostics()
        {
            if (!m_Diagnostics.IsEnabled || m_SelectedTick == 0 ||
                m_LastDiagnosticsTick != 0 && m_SelectedTick < m_LastDiagnosticsTick + (ulong)m_TickRate)
            {
                return;
            }
            m_LastDiagnosticsTick = m_SelectedTick;
            m_Diagnostics.PublishModel(new SimulationModelTraceRecord(
                SimulationModelTraceKind.OutputDisposition,
                "remote_presentation_horizon",
                $"selectedTick={m_SelectedTick};commands={Count(m_Commands)};reliable={Count(m_Reliable)}",
                ActorId,
                m_SelectedTick,
                m_SelectedTick,
                0,
                m_LastReliableSequence,
                Count(m_Commands) + Count(m_Reliable)));
        }

        static int Count<T>(SortedDictionary<ulong, List<T>> queue)
        {
            int count = 0;
            foreach (List<T> values in queue.Values)
                count = checked(count + values.Count);
            return count;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ServerAuthoritativeRemotePresentationRegistration));
        }
    }

    internal sealed class ServerAuthoritativeRemotePresentationFrameTarget : IGameplayPresentationFrameTarget
    {
        readonly ServerAuthoritativeRemotePresentationRegistration m_Registration;

        public ServerAuthoritativeRemotePresentationFrameTarget(
            ServerAuthoritativeRemotePresentationRegistration registration)
        {
            m_Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            m_Registration.Present(context);
        }
    }
}
