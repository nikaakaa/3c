using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonGameplay.ScenePlay;
using ThirdPersonSimulation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdPersonCharacter.Pipeline
{
    public enum BtsmtlScenePlayContextDiagnosticCode : byte
    {
        None = 0,
        ContextIdMissing = 1,
        ScenePathMissing = 2,
        SessionHostMissing = 3,
        CompositionMissing = 4,
        ActorRosterMissing = 5,
        ActorMissing = 6,
        ActorIdMissing = 7,
        DuplicateActorId = 8,
        SessionHostMismatch = 9,
        DefinitionMissing = 10,
        ControlSourceMissing = 11,
        DuplicateContext = 14,
        ActorSceneMismatch = 15,
        RuntimeOwnerMissing = 16,
        RuntimeOwnerContractMissing = 17,
        RuntimeOwnerSceneMismatch = 18,
        RuntimeOwnerIdentityMissing = 19,
        RuntimeOwnerReleaseFailed = 20,
        SkillGraphCatalogMissing = 21,
        SkillGraphInvalid = 22,
        SkillGraphMissing = 23
    }

    public readonly struct BtsmtlScenePlayContextDiagnostic
    {
        public BtsmtlScenePlayContextDiagnostic(
            BtsmtlScenePlayContextDiagnosticCode code,
            string message,
            string actorId = "")
        {
            Code = code;
            Message = message ?? string.Empty;
            ActorId = actorId ?? string.Empty;
        }

        public BtsmtlScenePlayContextDiagnosticCode Code { get; }
        public string Message { get; }
        public string ActorId { get; }
        public bool IsValid => Code == BtsmtlScenePlayContextDiagnosticCode.None;
    }

    public readonly struct BtsmtlScenePlayActorDescriptor
    {
        public BtsmtlScenePlayActorDescriptor(
            CharacterPipelineHost host,
            CharacterPipelineDefinition definition,
            CharacterControlSource controlSource,
            IReadOnlyList<GameplayAbilityAdmissionProfile> admissionProfiles)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            ControlSource = controlSource ?? throw new ArgumentNullException(nameof(controlSource));
            ActorId = host.SimulationActorId;
            ControlModuleId = definition.ControlModuleId;
            ControlSourceIdentity = controlSource.SourceIdentity;
            AdmissionProfiles = admissionProfiles ?? Array.Empty<GameplayAbilityAdmissionProfile>();
        }

        public CharacterPipelineHost Host { get; }
        public CharacterPipelineDefinition Definition { get; }
        public CharacterControlSource ControlSource { get; }
        public ActorId ActorId { get; }
        public string ControlModuleId { get; }
        public string ControlSourceIdentity { get; }
        public IReadOnlyList<GameplayAbilityAdmissionProfile> AdmissionProfiles { get; }
    }

    public readonly struct BtsmtlScenePlayContextDescriptor
    {
        public BtsmtlScenePlayContextDescriptor(
            BtsmtlScenePlayContext context,
            SimulationSessionHost sessionHost,
            IReadOnlyList<BtsmtlScenePlayActorDescriptor> actors,
            IBtsmtlScenePlayRuntimeOwner runtimeOwner)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            SessionHost = sessionHost;
            Actors = actors ?? throw new ArgumentNullException(nameof(actors));
            RuntimeOwner = runtimeOwner;
            RuntimeOwnerStatus = runtimeOwner != null ? runtimeOwner.Status : default;
            ScenePath = context.ScenePath;
            ContextId = context.ContextId;
            if (sessionHost && sessionHost.Composition)
            {
                SessionId = sessionHost.Composition.SessionId;
                WorldId = sessionHost.Composition.WorldId;
                MapId = sessionHost.Composition.MapId;
                WorldRevision = sessionHost.Composition.WorldRevision;
                TickRate = sessionHost.Composition.TickRate;
            }
            else
            {
                SessionId = string.Empty;
                WorldId = string.Empty;
                MapId = string.Empty;
                WorldRevision = string.Empty;
                TickRate = 0;
            }
        }

        public BtsmtlScenePlayContext Context { get; }
        public SimulationSessionHost SessionHost { get; }
        public IReadOnlyList<BtsmtlScenePlayActorDescriptor> Actors { get; }
        public IBtsmtlScenePlayRuntimeOwner RuntimeOwner { get; }
        public BtsmtlScenePlayRuntimeOwnerStatus RuntimeOwnerStatus { get; }
        public string ScenePath { get; }
        public string ContextId { get; }
        public string SessionId { get; }
        public string WorldId { get; }
        public string MapId { get; }
        public string WorldRevision { get; }
        public int TickRate { get; }
        public bool HasCharacterRuntime => SessionHost && Actors.Count != 0;
        public bool HasContentRuntime => RuntimeOwner != null;
        public bool HasRuntimeOwner => HasCharacterRuntime || HasContentRuntime;
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BtsmtlScenePlayContext : MonoBehaviour
    {
        [SerializeField] string m_ContextId;
        [SerializeField] SimulationSessionHost m_SessionHost;
        [SerializeField] List<CharacterPipelineHost> m_ActorHosts = new List<CharacterPipelineHost>();
        [SerializeField] MonoBehaviour m_RuntimeOwner;

        public string ContextId => string.IsNullOrWhiteSpace(m_ContextId) ? string.Empty : m_ContextId.Trim();
        public string ScenePath => gameObject.scene.path ?? string.Empty;
        public SimulationSessionHost SessionHost => m_SessionHost;
        public MonoBehaviour DeclaredRuntimeOwner => m_RuntimeOwner;
        public IReadOnlyList<CharacterPipelineHost> ActorHosts =>
            m_ActorHosts != null ? m_ActorHosts : Array.Empty<CharacterPipelineHost>();

        public bool TryDescribe(
            out BtsmtlScenePlayContextDescriptor descriptor,
            out BtsmtlScenePlayContextDiagnostic diagnostic)
        {
            descriptor = default;
            diagnostic = ValidateConfiguration();
            if (!diagnostic.IsValid)
                return false;

            int actorCount = m_ActorHosts == null ? 0 : m_ActorHosts.Count;
            var actors = new List<BtsmtlScenePlayActorDescriptor>(actorCount);
            for (int i = 0; i < actorCount; i++)
            {
                CharacterPipelineHost host = m_ActorHosts[i];
                actors.Add(new BtsmtlScenePlayActorDescriptor(
                    host,
                    host.Definition,
                    host.ControlSource,
                    host.Definition.AdmissionProfiles));
            }
            descriptor = new BtsmtlScenePlayContextDescriptor(
                this,
                m_SessionHost,
                actors,
                m_RuntimeOwner as IBtsmtlScenePlayRuntimeOwner);
            return true;
        }

        public bool TryReleaseRuntime(out BtsmtlScenePlayContextDiagnostic diagnostic)
        {
            diagnostic = ValidateConfiguration();
            if (!diagnostic.IsValid)
                return false;
            var failures = new List<string>();
            if (m_RuntimeOwner)
            {
                try
                {
                    BtsmtlScenePlayRuntimeOwnerReleaseResult result =
                        ((IBtsmtlScenePlayRuntimeOwner)m_RuntimeOwner).Release();
                    if (!result.Succeeded)
                    {
                        failures.Add(
                            $"{result.FailureCode}: {result.FailureMessage}");
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(exception.Message);
                }
            }
            if (m_SessionHost)
            {
                try
                {
                    m_SessionHost.Quiesce();
                    m_SessionHost.ReleaseSessionRuntime();
                }
                catch (Exception exception)
                {
                    failures.Add(exception.Message);
                }
            }
            if (failures.Count == 0)
                return true;
            diagnostic = new BtsmtlScenePlayContextDiagnostic(
                BtsmtlScenePlayContextDiagnosticCode.RuntimeOwnerReleaseFailed,
                string.Join(" | ", failures));
            return false;
        }

#if UNITY_EDITOR
        public void SetAuthoring(
            string contextId,
            SimulationSessionHost sessionHost = null,
            IReadOnlyList<CharacterPipelineHost> actorHosts = null,
            MonoBehaviour runtimeOwner = null)
        {
            m_ContextId = string.IsNullOrWhiteSpace(contextId)
                ? throw new ArgumentException("Scene Play context id is required.", nameof(contextId))
                : contextId.Trim();
            m_SessionHost = sessionHost;
            m_ActorHosts = actorHosts == null
                ? new List<CharacterPipelineHost>()
                : new List<CharacterPipelineHost>(actorHosts);
            m_RuntimeOwner = runtimeOwner;
        }
#endif

        void OnEnable()
        {
            BtsmtlScenePlayContextRegistry.Register(this);
        }

        void OnDisable()
        {
            BtsmtlScenePlayContextRegistry.Unregister(this);
        }

        void OnValidate()
        {
            BtsmtlScenePlayContextRegistry.Unregister(this);
            BtsmtlScenePlayContextRegistry.Register(this);
        }

        BtsmtlScenePlayContextDiagnostic ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(ContextId))
                return Invalid(BtsmtlScenePlayContextDiagnosticCode.ContextIdMissing, "Scene Play context requires an explicit ContextId.");
            if (string.IsNullOrEmpty(ScenePath))
                return Invalid(BtsmtlScenePlayContextDiagnosticCode.ScenePathMissing, "Scene Play context must belong to a saved Scene.");
            if (BtsmtlScenePlayContextRegistry.IsConflicted(ScenePath, ContextId))
                return Invalid(BtsmtlScenePlayContextDiagnosticCode.DuplicateContext, $"Scene Play context '{ContextId}' is registered more than once in '{ScenePath}'.");
            bool hasSessionHost = m_SessionHost;
            bool hasActors = m_ActorHosts != null && m_ActorHosts.Count != 0;
            IBtsmtlScenePlayRuntimeOwner runtimeOwner =
                m_RuntimeOwner as IBtsmtlScenePlayRuntimeOwner;
            if (m_RuntimeOwner && runtimeOwner == null)
                return Invalid(
                    BtsmtlScenePlayContextDiagnosticCode.RuntimeOwnerContractMissing,
                    "Scene Play context Runtime Owner must implement the formal owner contract.");
            if (runtimeOwner != null)
            {
                if (m_RuntimeOwner.gameObject.scene != gameObject.scene)
                    return Invalid(
                        BtsmtlScenePlayContextDiagnosticCode.RuntimeOwnerSceneMismatch,
                        "Scene Play context Runtime Owner must belong to the same Scene.");
                if (string.IsNullOrWhiteSpace(runtimeOwner.Status.OwnerIdentity))
                    return Invalid(
                        BtsmtlScenePlayContextDiagnosticCode.RuntimeOwnerIdentityMissing,
                        "Scene Play context Runtime Owner requires an explicit identity.");
            }
            if (!hasSessionHost && !hasActors && runtimeOwner == null)
                return Invalid(
                    BtsmtlScenePlayContextDiagnosticCode.RuntimeOwnerMissing,
                    "Scene Play context requires a formal Character Session or Runtime Owner.");
            if (!hasSessionHost && !hasActors)
                return default;
            if (!hasSessionHost)
                return Invalid(BtsmtlScenePlayContextDiagnosticCode.SessionHostMissing, "Scene Play context Actor roster requires an explicit Session Host.");
            if (m_SessionHost.gameObject.scene != gameObject.scene)
                return Invalid(BtsmtlScenePlayContextDiagnosticCode.SessionHostMissing, "Scene Play context Session Host must belong to the same Scene.");
            if (!m_SessionHost.Composition)
                return Invalid(BtsmtlScenePlayContextDiagnosticCode.CompositionMissing, "Scene Play context Session Host requires an explicit Composition.");
            if (!hasActors)
                return Invalid(BtsmtlScenePlayContextDiagnosticCode.ActorRosterMissing, "Scene Play context requires an explicit Actor roster.");

            var actorIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_ActorHosts.Count; i++)
            {
                CharacterPipelineHost host = m_ActorHosts[i];
                if (!host)
                    return Invalid(BtsmtlScenePlayContextDiagnosticCode.ActorMissing, $"Scene Play Actor roster entry {i} is missing.");
                if (host.gameObject.scene != gameObject.scene)
                    return Invalid(BtsmtlScenePlayContextDiagnosticCode.ActorSceneMismatch, $"Scene Play Actor '{host.name}' does not belong to the context Scene.");
                if (!host.SimulationActorId.IsValid)
                    return Invalid(BtsmtlScenePlayContextDiagnosticCode.ActorIdMissing, "Scene Play Actor requires an explicit ActorId.");
                if (!actorIds.Add(host.SimulationActorId.Value))
                    return Invalid(BtsmtlScenePlayContextDiagnosticCode.DuplicateActorId, $"Scene Play ActorId '{host.SimulationActorId}' is registered more than once.", host.SimulationActorId.Value);
                if (host.SessionHost != m_SessionHost)
                    return Invalid(BtsmtlScenePlayContextDiagnosticCode.SessionHostMismatch, $"Scene Play Actor '{host.SimulationActorId}' does not use the context Session Host.", host.SimulationActorId.Value);
                if (!host.Definition)
                    return Invalid(BtsmtlScenePlayContextDiagnosticCode.DefinitionMissing, $"Scene Play Actor '{host.SimulationActorId}' has no Character Definition.", host.SimulationActorId.Value);
                if (!host.ControlSource)
                    return Invalid(BtsmtlScenePlayContextDiagnosticCode.ControlSourceMissing, $"Scene Play Actor '{host.SimulationActorId}' has no control source.", host.SimulationActorId.Value);
#if UNITY_EDITOR
                BtsmtlScenePlayContextDiagnostic abilityDiagnostic = ValidateAbilityGraphs(host.Definition, host.SimulationActorId.Value);
                if (!abilityDiagnostic.IsValid)
                    return abilityDiagnostic;
#endif
            }
            return default;
        }

#if UNITY_EDITOR
        BtsmtlScenePlayContextDiagnostic ValidateAbilityGraphs(
            CharacterPipelineDefinition definition,
            string actorId)
        {
            IReadOnlyList<BtsmtlSkillFlowGraph> graphs = definition.AbilityGraphs;
            if (graphs == null || graphs.Count == 0)
                return Invalid(
                    BtsmtlScenePlayContextDiagnosticCode.SkillGraphCatalogMissing,
                    $"Scene Play Actor '{actorId}' has granted Abilities but no formal Ability Graph catalog.",
                    actorId);
            var graphIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graphs.Count; i++)
            {
                BtsmtlSkillFlowGraph graph = graphs[i];
                if (graph == null || graph.Role != BtsmtlSkillFlowGraphRole.Skill ||
                    string.IsNullOrWhiteSpace(graph.AuthoringId) || !graphIds.Add(graph.AuthoringId))
                    return Invalid(
                        BtsmtlScenePlayContextDiagnosticCode.SkillGraphInvalid,
                        $"Scene Play Actor '{actorId}' has an invalid or duplicated Ability Graph.",
                        actorId);
            }
            for (int i = 0; i < definition.AbilityGrants.Count; i++)
            {
                AbilityGrant grant = definition.AbilityGrants[i];
                BtsmtlSkillFlowGraph graph = grant?.Ability?.AbilityGraph;
                if (grant == null || !grant.Ability || graph == null || !graphIds.Contains(graph.AuthoringId))
                    return Invalid(
                        BtsmtlScenePlayContextDiagnosticCode.SkillGraphMissing,
                        $"Scene Play Actor '{actorId}' contains an AbilityGrant without a matching Ability Graph.",
                        actorId);
            }
            return default;
        }
#endif

        BtsmtlScenePlayContextDiagnostic Invalid(
            BtsmtlScenePlayContextDiagnosticCode code,
            string message,
            string actorId = "") =>
            new BtsmtlScenePlayContextDiagnostic(code, message, actorId);
    }

    public static class BtsmtlScenePlayContextRegistry
    {
        static readonly Dictionary<string, List<BtsmtlScenePlayContext>> Contexts =
            new Dictionary<string, List<BtsmtlScenePlayContext>>(StringComparer.Ordinal);

        public static event Action Changed;

        public static bool TryGet(string scenePath, string contextId, out BtsmtlScenePlayContext context)
        {
            context = null;
            if (string.IsNullOrWhiteSpace(scenePath) || string.IsNullOrWhiteSpace(contextId))
                return false;
            if (!Contexts.TryGetValue(BuildKey(scenePath.Trim(), contextId.Trim()), out List<BtsmtlScenePlayContext> matches) ||
                matches.Count != 1)
                return false;
            context = matches[0];
            return context;
        }

        public static bool IsConflicted(string scenePath, string contextId)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || string.IsNullOrWhiteSpace(contextId))
                return false;
            return Contexts.TryGetValue(
                       BuildKey(scenePath.Trim(), contextId.Trim()),
                       out List<BtsmtlScenePlayContext> matches) &&
                   matches.Count > 1;
        }

        internal static void Register(BtsmtlScenePlayContext context)
        {
            if (!context || string.IsNullOrEmpty(context.ScenePath) || string.IsNullOrEmpty(context.ContextId))
                return;
            string key = BuildKey(context.ScenePath, context.ContextId);
            if (!Contexts.TryGetValue(key, out List<BtsmtlScenePlayContext> matches))
            {
                matches = new List<BtsmtlScenePlayContext>();
                Contexts.Add(key, matches);
            }
            if (matches.Contains(context))
                return;
            if (matches.Count != 0)
            {
                Debug.LogError(
                    $"Scene Play context '{context.ContextId}' is registered more than once in '{context.ScenePath}'.",
                    context);
            }
            matches.Add(context);
            Changed?.Invoke();
        }

        internal static void Unregister(BtsmtlScenePlayContext context)
        {
            if (!context)
                return;
            List<string> emptyKeys = null;
            bool removed = false;
            foreach (KeyValuePair<string, List<BtsmtlScenePlayContext>> pair in Contexts)
            {
                if (!pair.Value.Remove(context))
                    continue;
                removed = true;
                if (pair.Value.Count == 0)
                    (emptyKeys ??= new List<string>()).Add(pair.Key);
            }
            if (emptyKeys != null)
            {
                for (int i = 0; i < emptyKeys.Count; i++)
                    Contexts.Remove(emptyKeys[i]);
            }
            if (removed)
                Changed?.Invoke();
        }

        static string BuildKey(string scenePath, string contextId) => $"{scenePath}|{contextId}";
    }
}
