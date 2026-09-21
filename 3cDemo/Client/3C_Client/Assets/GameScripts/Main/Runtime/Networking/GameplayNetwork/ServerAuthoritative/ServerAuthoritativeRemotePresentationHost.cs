using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.Tick;
using ThirdPersonSimulation;
using ThirdPersonSimulation.ServerAuthoritative;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonGameplay.Networking.ServerAuthoritative
{
    internal interface IServerAuthoritativeRemotePresentationTarget : IDisposable
    {
        ActorId ActorId { get; }
        void Commit(RemotePresentationBatch batch);
    }

    [DisallowMultipleComponent]
    public sealed class ServerAuthoritativeRemotePresentationHost : MonoBehaviour
    {
        [SerializeField] string m_BindingId = string.Empty;
        [SerializeField] CharacterPipelineDefinition m_CharacterDefinition;
        [SerializeField] GameObject m_CharacterTemplate;
        [SerializeField] Vector3 m_SpawnPosition;
        [SerializeField] Vector3 m_SpawnEulerAngles;
        [SerializeField] CharacterBodyPresentationProfile m_BodyPresentationProfile;

        ServerAuthoritativeRemotePresentationTarget m_Target;

        public string BindingId => string.IsNullOrWhiteSpace(m_BindingId)
            ? throw new InvalidOperationException($"Remote Presentation Host '{name}' requires an explicit BindingId.")
            : m_BindingId.Trim();

        internal ServerAuthoritativeRemotePresentationTarget Claim(
            ActorId actorId,
            Float32CharacterRuntime characterRuntime,
            int tickRate,
            ISimulationDiagnosticsSink diagnostics)
        {
            if (m_Target != null)
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' is already claimed.");
            if (!isActiveAndEnabled)
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' is not active.");
            CharacterAnimationPresentationProfile profile = m_CharacterDefinition
                ? m_CharacterDefinition.AnimationPresentationProfile
                : null;
            if (!profile)
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' requires an explicit Animation Presentation Profile.");
            if (!m_CharacterTemplate)
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' requires an explicit Character Root template.");
            if (tickRate <= 0 || !m_BodyPresentationProfile)
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' requires a formal Presentation Profile.");
            if (characterRuntime == null || diagnostics == null || !actorId.IsValid)
                throw new ArgumentException("Remote Presentation Host claim identity is incomplete.");

            CharacterAnimationRigPayload animationRig = new CharacterAnimationRigPayload(profile.RigDefinition);
            GameObject characterObject = Instantiate(m_CharacterTemplate, m_SpawnPosition, Quaternion.Euler(m_SpawnEulerAngles));
            characterObject.name = $"{m_CharacterTemplate.name} [Remote {actorId.Value}]";
            CharacterRootHierarchyBinding rootHierarchy = characterObject.GetComponent<CharacterRootHierarchyBinding>();
            if (!rootHierarchy)
            {
                Destroy(characterObject);
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' template requires a Root Hierarchy Binding on LogicRoot.");
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
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' PoseRoot requires Animancer, Animator, and Animation Rig Binding.");
            }
            animationRigBinding.RequireValid(animationRig);
            if (animancer.Animator.transform != rootHierarchy.PoseRoot)
            {
                Destroy(characterObject);
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' Animator must use PoseRoot.");
            }
            CharacterWorldAwarePresentationBinding worldAwarePresentation =
                rootHierarchy.VisualRoot.GetComponent<CharacterWorldAwarePresentationBinding>();
            if (!worldAwarePresentation)
            {
                Destroy(characterObject);
                throw new InvalidOperationException($"Remote Presentation Host '{BindingId}' VisualRoot requires a World-Aware Presentation Binding.");
            }
            worldAwarePresentation.RequireValid();
            PhysicsScene physicsScene = characterObject.scene.GetPhysicsScene();
            WorldBodyState initialBody = BuildInitialBody(actorId, rootHierarchy.LogicRoot);
            RuntimeContentRevision diagnosticsRevision = new RuntimeContentRevision(
                $"float32-character-runtime/{actorId.Value}",
                profile.PoseGraph.Graph.ContentRevision,
                characterRuntime.GameplayContentHash.ToString());
            var debugSourceMap = new DebugSourceMap(diagnosticsRevision);
            var diagnosticsContext = new RuntimeDiagnosticsContext(
                Guid.NewGuid(),
                Guid.NewGuid(),
                diagnosticsRevision,
                debugSourceMap,
                new RuntimeDiagnosticsStore());
            var diagnosticsTarget = new RuntimeDiagnosticsTarget(name, GetInstanceID(), diagnosticsContext);
            ICharacterPresentationDomainRuntime runtime = null;
            ServerAuthoritativeRemotePresentationTarget target = null;
            try
            {
                runtime = CharacterPresentationDomainRuntimeFactory.Create(
                    tickRate,
                    profile,
                    animationRig,
                    actorId,
                    animancer,
                    animationRigBinding,
                    rootHierarchy,
                    CharacterPresentationBodyState.FromFloat32(initialBody),
                    CharacterPresentationRole.SimulatedActor,
                    CharacterLocomotionPresentationPreparation.RequireBinding(
                        new CharacterLocomotionPresentationPreparationRequest(
                            new CharacterLocomotionPresentationPlan(
                                string.Concat(
                                    "server-authority-remote/",
                                    BindingId,
                                    "/",
                                    actorId.Value,
                                    "/locomotion"),
                                CharacterLocomotionClockMode.FreeRun,
                                CharacterLocomotionBodySource.SelectedStream),
                            m_BodyPresentationProfile,
                            CharacterLocomotionPresentationSourceCapability.SelectedBodyStream)),
                    worldAwarePresentation,
                    physicsScene,
                    null,
                    null,
                    null,
                    null,
                    null,
                    string.Empty,
                    null,
                    null,
                    null,
                    null,
                    diagnosticsContext,
                    true);
                target = new ServerAuthoritativeRemotePresentationTarget(
                    BindingId,
                    actorId,
                    tickRate,
                    characterRuntime,
                    diagnostics,
                    runtime,
                    diagnosticsTarget,
                    characterObject,
                    rootHierarchy,
                    Release);
                runtime = null;
                diagnosticsTarget = null;
                target.Activate();
                m_Target = target;
                return target;
            }
            catch
            {
                target?.Dispose();
                runtime?.Dispose();
                diagnosticsTarget?.Terminate();
                diagnosticsTarget?.Dispose();
                if (target == null && characterObject)
                    Destroy(characterObject);
                throw;
            }
        }

        void OnEnable()
        {
            ServerAuthoritativeRemotePresentationHostRegistry.Register(this);
        }

        void OnDisable()
        {
            ServerAuthoritativeRemotePresentationHostRegistry.Unregister(this);
            m_Target?.Dispose();
        }

        void OnDestroy()
        {
            ServerAuthoritativeRemotePresentationHostRegistry.Unregister(this);
            m_Target?.Dispose();
        }

        void Release(ServerAuthoritativeRemotePresentationTarget target)
        {
            if (ReferenceEquals(m_Target, target))
                m_Target = null;
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

    internal static class ServerAuthoritativeRemotePresentationHostRegistry
    {
        static readonly Dictionary<string, ServerAuthoritativeRemotePresentationHost> s_Hosts =
            new Dictionary<string, ServerAuthoritativeRemotePresentationHost>(StringComparer.Ordinal);

        public static void Register(ServerAuthoritativeRemotePresentationHost host)
        {
            if (!host)
                throw new ArgumentNullException(nameof(host));
            string bindingId = host.BindingId;
            if (s_Hosts.TryGetValue(bindingId, out ServerAuthoritativeRemotePresentationHost current) && current != host)
                throw new InvalidOperationException($"Remote Presentation BindingId '{bindingId}' is registered more than once.");
            s_Hosts[bindingId] = host;
        }

        public static void Unregister(ServerAuthoritativeRemotePresentationHost host)
        {
            if (!host)
                return;
            string bindingId = host.BindingId;
            if (s_Hosts.TryGetValue(bindingId, out ServerAuthoritativeRemotePresentationHost current) && current == host)
                s_Hosts.Remove(bindingId);
        }

        public static IServerAuthoritativeRemotePresentationTarget Claim(
            string bindingId,
            ActorId actorId,
            Float32CharacterRuntime characterRuntime,
            int tickRate,
            ISimulationDiagnosticsSink diagnostics)
        {
            string identity = string.IsNullOrWhiteSpace(bindingId)
                ? throw new ArgumentException("Remote Presentation BindingId is required.", nameof(bindingId))
                : bindingId.Trim();
            if (!s_Hosts.TryGetValue(identity, out ServerAuthoritativeRemotePresentationHost host) || !host)
                throw new InvalidOperationException($"Remote Presentation Host '{identity}' is not registered by the active Client Scene.");
            return host.Claim(actorId, characterRuntime, tickRate, diagnostics);
        }
    }

    internal sealed class ServerAuthoritativeRemotePresentationTarget : IServerAuthoritativeRemotePresentationTarget
    {
        readonly int m_TickRate;
        readonly Float32CharacterRuntime m_CharacterRuntime;
        readonly ICharacterPresentationDomainRuntime m_Runtime;
        readonly SimulationGameplayOutputBuffer m_Gameplay = new SimulationGameplayOutputBuffer();
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly RuntimeDiagnosticsTarget m_DiagnosticsTarget;
        readonly GameObject m_CharacterObject;
        readonly CharacterRootHierarchyBinding m_RootHierarchy;
        readonly ServerAuthoritativeRemotePresentationFrameTarget m_PresentationTarget;
        readonly Action<ServerAuthoritativeRemotePresentationTarget> m_Release;
        readonly TickQueue<PresentationCommand> m_Commands = new TickQueue<PresentationCommand>();
        readonly TickQueue<ServerAuthoritativeReliableEvent> m_Reliable = new TickQueue<ServerAuthoritativeReliableEvent>();
        readonly List<CharacterPresentationBodyInterval> m_BodyIntervals = new List<CharacterPresentationBodyInterval>();

        ulong m_LastReliableSequence;
        EventId m_LastReliableEventId;
        ulong m_LastDiagnosticsTick;
        ulong m_SelectedTick;
        bool m_Activated;
        bool m_Disposed;

        public ServerAuthoritativeRemotePresentationTarget(
            string bindingId,
            ActorId actorId,
            int tickRate,
            Float32CharacterRuntime characterRuntime,
            ISimulationDiagnosticsSink diagnostics,
            ICharacterPresentationDomainRuntime runtime,
            RuntimeDiagnosticsTarget diagnosticsTarget,
            GameObject characterObject,
            CharacterRootHierarchyBinding rootHierarchy,
            Action<ServerAuthoritativeRemotePresentationTarget> release)
        {
            BindingId = string.IsNullOrWhiteSpace(bindingId)
                ? throw new ArgumentException("Remote Presentation BindingId is required.", nameof(bindingId))
                : bindingId.Trim();
            if (!actorId.IsValid || tickRate <= 0)
                throw new ArgumentException("Remote Presentation target configuration is invalid.");
            ActorId = actorId;
            m_TickRate = tickRate;
            m_CharacterRuntime = characterRuntime ?? throw new ArgumentNullException(nameof(characterRuntime));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            m_DiagnosticsTarget = diagnosticsTarget ?? throw new ArgumentNullException(nameof(diagnosticsTarget));
            m_CharacterObject = characterObject ? characterObject : throw new ArgumentNullException(nameof(characterObject));
            m_RootHierarchy = rootHierarchy ? rootHierarchy : throw new ArgumentNullException(nameof(rootHierarchy));
            m_RootHierarchy.RequireValid();
            m_Release = release ?? throw new ArgumentNullException(nameof(release));
            m_PresentationTarget = new ServerAuthoritativeRemotePresentationFrameTarget(this, m_Runtime);
        }

        public string BindingId { get; }
        public ActorId ActorId { get; }
        public IReadOnlyList<SimulationGameplayOutputChange> CurrentGameplayChanges => m_Gameplay.CurrentTickChanges;
        public EventId LastReliableEventId => m_LastReliableEventId;

        public void Activate()
        {
            RequireAlive();
            if (m_Activated)
                return;
            RuntimeDiagnosticsTargetRegistry.Register(m_DiagnosticsTarget);
            try
            {
                m_PresentationTarget.Activate();
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
                throw new InvalidOperationException("Remote Presentation batch does not match the active target.");
            if (batch.ResetBodyStream && batch.BodySamples.Count == 0)
                throw new InvalidOperationException("Remote selected Body stream reset requires an explicit anchor interval.");
            m_Gameplay.BeginTick();
            CharacterPresentationBodyState finalBody = default;
            bool hasFinalBody = false;
            m_BodyIntervals.Clear();
            for (int i = 0; i < batch.BodySamples.Count; i++)
            {
                CharacterBodySample sample = batch.BodySamples[i];
                m_BodyIntervals.Add(CharacterPresentationBodyInterval.FromFloat32(
                    sample,
                    m_TickRate,
                    batch.ResetBodyStream && i == 0
                        ? CharacterPresentationBodyStreamUpdateKind.Reset
                        : CharacterPresentationBodyStreamUpdateKind.Append));
                finalBody = CharacterPresentationBodyState.FromFloat32(sample.FinalBody);
                hasFinalBody = true;
                m_SelectedTick = sample.Tick.Value;
            }
            if (m_BodyIntervals.Count != 0)
                m_Runtime.CaptureBodyStream(m_BodyIntervals);
            m_BodyIntervals.Clear();
            for (int i = 0; i < batch.SampleCommands.Count; i++)
                Enqueue(m_Commands, batch.SampleCommands[i].Header.Tick.Value, batch.SampleCommands[i]);
            for (int i = 0; i < batch.ReliableEvents.Count; i++)
                Enqueue(m_Reliable, batch.ReliableEvents[i].Header.Tick.Value, batch.ReliableEvents[i]);
            if (hasFinalBody)
                m_RootHierarchy.ApplyLogicPose(finalBody.Position, finalBody.Rotation);
        }

        internal bool PreparePresentationFrame(GameplayPresentationFrameContext context)
        {
            RequireAlive();
            if (m_SelectedTick == 0)
                return false;
            PublishDue(m_SelectedTick, m_SelectedTick);
            return true;
        }

        internal void CompletePresentationFrame(GameplayPresentationFrameContext context)
        {
            PublishPresentationHorizonDiagnostics();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_Activated)
            {
                m_PresentationTarget.Deactivate();
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
            if (value.PresentationCommand.Kind == PresentationCommandKind.Camera ||
                ServerAuthoritativeReplicationPolicy.IsCameraProducer(
                    m_CharacterRuntime,
                    value.PresentationCommand.ProducerId))
            {
                throw new InvalidOperationException("Remote replication cannot contain Camera producer commands.");
            }
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

        static void PublishDue<T>(TickQueue<T> queue, ulong authorityTick, Action<T> publish)
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

        static void Enqueue<T>(TickQueue<T> queue, ulong tick, T value) => queue.Enqueue(tick, value);

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

        static int Count<T>(TickQueue<T> queue) => queue.Count;

        sealed class TickQueue<T>
        {
            readonly SortedDictionary<ulong, List<T>> m_Entries = new SortedDictionary<ulong, List<T>>();
            readonly Stack<List<T>> m_FreeValues = new Stack<List<T>>();
            readonly List<ulong> m_DueTicks = new List<ulong>();

            public int Count
            {
                get
                {
                    int count = 0;
                    foreach (List<T> values in m_Entries.Values)
                        count = checked(count + values.Count);
                    return count;
                }
            }

            public void Enqueue(ulong tick, T value)
            {
                if (tick == 0)
                    throw new InvalidOperationException("Remote presentation output has no authority Tick.");
                if (!m_Entries.TryGetValue(tick, out List<T> values))
                {
                    values = m_FreeValues.Count == 0 ? new List<T>() : m_FreeValues.Pop();
                    m_Entries.Add(tick, values);
                }
                values.Add(value);
            }

            public void PublishDue(ulong authorityTick, Action<T> publish)
            {
                m_DueTicks.Clear();
                foreach (KeyValuePair<ulong, List<T>> pair in m_Entries)
                {
                    if (pair.Key > authorityTick)
                        break;
                    for (int i = 0; i < pair.Value.Count; i++)
                        publish(pair.Value[i]);
                    m_DueTicks.Add(pair.Key);
                }
                for (int i = 0; i < m_DueTicks.Count; i++)
                {
                    m_Entries.Remove(m_DueTicks[i], out List<T> values);
                    values.Clear();
                    m_FreeValues.Push(values);
                }
            }

            public void Clear()
            {
                foreach (List<T> values in m_Entries.Values)
                {
                    values.Clear();
                    m_FreeValues.Push(values);
                }
                m_Entries.Clear();
                m_DueTicks.Clear();
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(ServerAuthoritativeRemotePresentationTarget));
        }
    }

    internal sealed class ServerAuthoritativeRemotePresentationFrameTarget : IGameplayPresentationFrameTarget
    {
        readonly ServerAuthoritativeRemotePresentationTarget m_Target;

        public ServerAuthoritativeRemotePresentationFrameTarget(
            ServerAuthoritativeRemotePresentationTarget target,
            ICharacterPresentationDomainRuntime runtime)
        {
            m_Target = target ?? throw new ArgumentNullException(nameof(target));
            m_Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        }

        readonly ICharacterPresentationDomainRuntime m_Runtime;

        public void PresentationFrame(GameplayPresentationFrameContext context)
        {
            if (!m_Target.PreparePresentationFrame(context))
                return;
            m_Runtime.PresentationFrame(context);
            m_Target.CompletePresentationFrame(context);
        }

        internal void Activate()
        {
            if (!GameplayTickSystem.RegisterPresentationTarget(this))
                throw new InvalidOperationException("Gameplay Tick System is not initialized.");
        }

        internal void Deactivate() => GameplayTickSystem.UnregisterPresentationTarget(this);
    }
}
