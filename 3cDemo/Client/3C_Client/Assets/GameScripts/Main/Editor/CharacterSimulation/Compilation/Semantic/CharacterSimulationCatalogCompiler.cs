using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSimulationCatalogIndex
    {
        public HashSet<string> InputValues { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> InputRequests { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Actions { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Behaviors { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> GameplayTags { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Attributes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> GameplayEffects { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentSlots { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentRoutes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentFeatures { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentItems { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentParameters { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class CharacterSimulationCatalogCompiler
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationCatalogIndex m_Index = new CharacterSimulationCatalogIndex();
        readonly CharacterSemanticInputCatalogEmitter m_InputCatalog;
        readonly CharacterSemanticTagAttributeCatalogEmitter m_TagAttributes;
        readonly CharacterSemanticActionCatalogEmitter m_ActionCatalog;
        readonly CharacterSemanticGameplayEffectCatalogEmitter m_GameplayEffects;
        readonly CharacterSemanticEquipmentCatalogEmitter m_EquipmentCatalog;

        public CharacterSimulationCatalogCompiler(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report)
        {
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_InputCatalog = new CharacterSemanticInputCatalogEmitter(model, builder, report, m_Index);
            m_TagAttributes = new CharacterSemanticTagAttributeCatalogEmitter(model, builder, report, m_Index);
            m_ActionCatalog = new CharacterSemanticActionCatalogEmitter(model, builder, report, m_Index);
            m_GameplayEffects = new CharacterSemanticGameplayEffectCatalogEmitter(model, builder, report, m_Index);
            m_EquipmentCatalog = new CharacterSemanticEquipmentCatalogEmitter(model, builder, report, m_Index);
        }

        public CharacterSimulationCatalogIndex Compile()
        {
            m_InputCatalog.Emit();
            m_TagAttributes.Emit();
            m_ActionCatalog.Emit();
            m_GameplayEffects.Emit();
            m_EquipmentCatalog.Emit();
            DeclareGlobalState();
            return m_Index;
        }

        void DeclareGlobalState()
        {
            CharacterSimulationSourceLocation source = DefinitionSource;
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Action, ProgramStateSemantic.ActionEventSequence, "action:event-sequence");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.GameplayEffectAggregate, ProgramStateOwnerKind.GameplayEffect, ProgramStateSemantic.GameplayEffectAggregate, "gameplay-effect:aggregate");
            if (m_Model.Definition.EquipmentCapabilityEnabled)
                m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.EquipmentAggregate, ProgramStateOwnerKind.Equipment, ProgramStateSemantic.EquipmentAggregate, "equipment:aggregate");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Random, ProgramStateSemantic.RandomState, "runtime:rng");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Runtime, ProgramStateSemantic.HandleAllocator, "runtime:handle-allocator");
            m_Builder.DeclareStandaloneStateSlot(source, ProgramStateValueKind.UInt64, ProgramStateOwnerKind.Fact, ProgramStateSemantic.FactSequence, "runtime:fact-sequence");
            m_Builder.RequireGameplayCapability("RunnableTree");
            m_Builder.RequireGameplayCapability("StateMachine");
            m_Builder.RequireGameplayCapability("Timeline");
            m_Builder.RequireGameplayCapability("PipelineBlackboard");
            m_Builder.RequireGameplayCapability("Action");
            m_Builder.RequireGameplayCapability("GameplayEffect");
            m_Builder.RequireWorldRequest("CharacterBodyMotion", WorldCapability.BodyMotion | WorldCapability.Grounding | WorldCapability.Collision);
        }

        CharacterSimulationSourceLocation DefinitionSource => AssetSource(m_Model.Definition, $"definition:{m_Model.Definition.name}");

        CharacterSimulationSourceLocation AssetSource(UnityEngine.Object asset, string identity)
        {
            return CharacterSemanticSourceFactory.Asset(m_Model, asset, identity);
        }

        static ProgramCatalogField[] Fields(params ProgramCatalogField[] fields) => Fields((IEnumerable<ProgramCatalogField>)fields);
        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) => fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
