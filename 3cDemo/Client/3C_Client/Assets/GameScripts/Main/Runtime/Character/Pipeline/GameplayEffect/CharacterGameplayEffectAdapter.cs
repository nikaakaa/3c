using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using ThirdPersonGameplay.Attributes;
using ThirdPersonGameplay.Contracts;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline.GameplayEffect
{
    public sealed class CharacterGameplayEffectAdapter :
        IGameplayTagReader,
        IGameplayTagSourceSink,
        IGameplayAttributeReader,
        IDisposable
    {
        readonly GameplayEffectRuntimeDefinition m_Definition;
        readonly CharacterGameplayEffectInputMapper m_InputMapper = new CharacterGameplayEffectInputMapper();
        readonly CharacterGameplayEffectFactProjector m_FactProjector = new CharacterGameplayEffectFactProjector();
        readonly CharacterGameplayCueProjector m_CueProjector = new CharacterGameplayCueProjector();
        readonly CharacterGameplayEffectTraceProjector m_TraceProjector = new CharacterGameplayEffectTraceProjector();
        readonly string m_ActorId;
        GameplayEffectRuntime m_Runtime;
        bool m_Disposed;

        public CharacterGameplayEffectAdapter(string actorId, GameplayEffectRuntimeDefinition definition)
        {
            m_ActorId = string.IsNullOrWhiteSpace(actorId)
                ? throw new ArgumentException("Character actor id is required.", nameof(actorId))
                : actorId.Trim();
            m_Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            QueryPorts = new CharacterGameplayEffectQueryPorts(this, this);
            CommandPorts = new CharacterGameplayEffectCommandPorts(ApplySelf, RemoveSelf);
        }

        public string ActorId => m_ActorId;
        public CharacterGameplayEffectQueryPorts QueryPorts { get; }
        public CharacterGameplayEffectCommandPorts CommandPorts { get; }
        public IGameplayTagReader TagReader => this;
        public IGameplayTagSourceSink TagSourceSink => this;
        public IGameplayAttributeReader AttributeReader => this;

        public void Activate()
        {
            ThrowIfDisposed();
            if (m_Runtime == null)
                m_Runtime = new GameplayEffectRuntime(m_Definition);
        }

        public void BeginLogicTick(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            IReadOnlyList<GameplayEffectAuthorityInput> inputs = m_InputMapper.Map(frame);
            RequireRuntime().BeginLogicTick(
                new GameplayEffectTickContext(context.LocalLogicTick, context.FixedDeltaSeconds),
                inputs);
        }

        public void CommitFacts(CharacterPipelineOutput output, RuntimeDiagnosticsContext diagnostics)
        {
            GameplayEffectChangeSet changes = RequireRuntime().DrainChangeSet();
            m_FactProjector.Project(changes, output);
            m_CueProjector.Project(changes, output);
            m_TraceProjector.Project(changes, diagnostics);
        }

        public void Deactivate()
        {
            m_Runtime?.Dispose();
            m_Runtime = null;
        }

        public bool HasTag(GameplayTagId tagId) => RequireRuntime().HasTag(tagId);
        public bool Matches(GameplayTagQuery query) => RequireRuntime().Matches(query);
        public bool Matches(GameplayTagQuery query, IReadOnlyList<GameplayTagId> explicitTags) => RequireRuntime().Matches(query, explicitTags);
        public bool SetSourceTags(GameplayTagSourceHandle source, IReadOnlyList<GameplayTagId> tags) => RequireRuntime().SetSourceTags(source, tags);
        public bool RemoveSource(GameplayTagSourceHandle source) => RequireRuntime().RemoveSource(source);
        public bool TryGetValue(GameplayAttributeId attributeId, out GameplayAttributeValue value) => RequireRuntime().TryGetValue(attributeId, out value);
        public void Dispose()
        {
            if (m_Disposed)
                return;
            Deactivate();
            m_Disposed = true;
        }

        GameplayEffectRuntime RequireRuntime()
        {
            ThrowIfDisposed();
            return m_Runtime ?? throw new InvalidOperationException("Character Gameplay Effect runtime is inactive.");
        }

        GameplayEffectApplyResult ApplySelf(CharacterGameplayEffectSelfApplyRequest request)
        {
            var context = new GameplayEffectContext(
                m_ActorId,
                m_ActorId,
                request.ActionInstanceId,
                request.PredictionKey,
                request.GameplayResultId,
                request.SourceLogicTick,
                request.ApplicationMode);
            return RequireRuntime().Apply(new GameplayEffectApplyRequest(
                request.EffectId,
                context,
                request.SetByCallerValues,
                definitionRevision: request.DefinitionRevision));
        }

        GameplayEffectRemoveResult RemoveSelf(CharacterGameplayEffectSelfRemoveRequest request)
        {
            GameplayEffectRemoveRequest runtimeRequest;
            switch (request.Selector)
            {
                case GameplayEffectRemoveSelector.Handle:
                    runtimeRequest = GameplayEffectRemoveRequest.ByHandle(request.Handle);
                    break;
                case GameplayEffectRemoveSelector.EffectId:
                    runtimeRequest = GameplayEffectRemoveRequest.ByEffect(request.EffectId);
                    break;
                case GameplayEffectRemoveSelector.SourceActorId:
                    runtimeRequest = GameplayEffectRemoveRequest.BySource(m_ActorId);
                    break;
                case GameplayEffectRemoveSelector.EffectTagQuery:
                    runtimeRequest = GameplayEffectRemoveRequest.ByTags(request.EffectTagQuery);
                    break;
                default:
                    return new GameplayEffectRemoveResult(Array.Empty<GameplayEffectHandle>());
            }
            return RequireRuntime().Remove(runtimeRequest);
        }

        void ThrowIfDisposed()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(CharacterGameplayEffectAdapter));
        }
    }
}
