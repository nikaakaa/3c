using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticInputCatalogEmitter
    {
        readonly CharacterAuthoringCompilationModel m_Model;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSimulationCatalogIndex m_Index;

        public CharacterSemanticInputCatalogEmitter(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSimulationCatalogIndex index)
        {
            m_Model = model ?? throw new ArgumentNullException(nameof(model));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Index = index ?? throw new ArgumentNullException(nameof(index));
        }

        public void Emit()
        {
            CharacterInputProfile profile = m_Model.InputProfile;
            if (!profile)
            {
                m_Report.Error("input_profile_missing", DefinitionSource.Identity, "Character Input Profile is missing.");
                return;
            }

            foreach (CharacterInputValueDefinition value in m_Model.InputValues)
            {
                if (value == null || string.IsNullOrEmpty(value.InputValueId))
                    continue;
                m_Index.InputValues.Add(value.InputValueId);
                CharacterSimulationSourceLocation source = AssetSource(profile, $"input:value:{value.InputValueId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.InputValue,
                    $"input:value:{value.InputValueId}",
                    2,
                    Fields(m_Builder.ConstantField(source, "ValueType", MapInputValueKind(value.ValueType))),
                    source);
            }

            foreach (CharacterAuthoringBlackboardDeclaration item in m_Model.Declarations.Values)
            {
                BaseExposedProperty declaration = item.Declaration;
                if (declaration.InputBinding == null)
                    continue;
                ProgramInputValueKind kind = MapInputValueKind(declaration.ValueType);
                if (!m_Index.InputValues.Add(declaration.InputValueId))
                {
                    m_Report.Error(
                        "input_value_identity_duplicate",
                        item.Route,
                        $"Blackboard Input Binding '{declaration.BlackboardKey}' duplicates input value '{declaration.InputValueId}'.");
                    continue;
                }
                CharacterSimulationSourceLocation source = AssetSource(
                    m_Model.Definition,
                    $"input:value:{declaration.InputValueId}/declaration:{declaration.DeclarationId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.InputValue,
                    $"input:value:{declaration.InputValueId}",
                    2,
                    Fields(m_Builder.ConstantField(source, "ValueType", kind)),
                    source);
            }

            if (m_Model.Roots.Any(root => ContainsCameraBasisRead(root.Occurrence)))
            {
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisValidInputId, ProgramInputValueKind.Boolean);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisPlanarForwardInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisPlanarRightInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisLookDirectionInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisAimPointInputId, ProgramInputValueKind.Vector3);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisYawInputId, ProgramInputValueKind.Yaw);
                DeclareRuntimeInput(CameraProgramOperationSchema.BasisPitchInputId, ProgramInputValueKind.Scalar);
            }

            foreach (CharacterActionRequestDefinition request in m_Model.InputRequests)
            {
                if (request == null || string.IsNullOrEmpty(request.RequestId))
                    continue;
                m_Index.InputRequests.Add(request.RequestId);
                CharacterSimulationSourceLocation source = AssetSource(profile, $"input:request:{request.RequestId}");
                m_Builder.DeclareCatalogEntry(
                    ProgramCatalogEntryKind.InputRequest,
                    $"input:request:{request.RequestId}",
                    2,
                    Fields(
                        m_Builder.ConstantField(source, "BufferSeconds", request.BufferSeconds),
                        m_Builder.ConstantField(source, "Priority", request.Priority),
                        m_Builder.ConstantField(source, "TimingClass", request.TimingClass)),
                    source);
                m_Builder.DeclareStandaloneStateSlot(
                    source,
                    ProgramStateValueKind.InputRequest,
                    ProgramStateOwnerKind.Input,
                    ProgramStateSemantic.InputRequestBuffer,
                    $"input:request:{request.RequestId}");
            }
        }

        void DeclareRuntimeInput(string inputId, ProgramInputValueKind kind)
        {
            if (!m_Index.InputValues.Add(inputId))
                throw new InvalidOperationException($"Runtime input value '{inputId}' conflicts with an authored input declaration.");
            CharacterSimulationSourceLocation source = DefinitionSource;
            m_Builder.DeclareCatalogEntry(
                ProgramCatalogEntryKind.InputValue,
                $"input:value:{inputId}",
                2,
                Fields(m_Builder.ConstantField(source, "ValueType", kind)),
                source);
        }

        static bool ContainsCameraBasisRead(CharacterAuthoringGraphOccurrence occurrence)
        {
            for (int i = 0; i < occurrence.Nodes.Count; i++)
            {
                if (occurrence.Nodes[i] is ReadCameraBasisNode)
                    return true;
            }
            for (int i = 0; i < occurrence.GraphReferences.Count; i++)
            {
                if (ContainsCameraBasisRead(occurrence.GraphReferences[i].Child))
                    return true;
            }
            for (int i = 0; i < occurrence.Edges.Count; i++)
            {
                CharacterAuthoringGraphOccurrence condition = occurrence.Edges[i].ConditionGraph;
                if (condition != null && ContainsCameraBasisRead(condition))
                    return true;
            }
            return false;
        }

        static ProgramInputValueKind MapInputValueKind(CharacterInputValueType type)
        {
            return type switch
            {
                CharacterInputValueType.Bool => ProgramInputValueKind.Boolean,
                CharacterInputValueType.Float => ProgramInputValueKind.Scalar,
                CharacterInputValueType.Vector2 => ProgramInputValueKind.Vector2,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        static ProgramInputValueKind MapInputValueKind(Type type)
        {
            if (type == typeof(ActionTargetSnapshot))
                return ProgramInputValueKind.ActionTargetSnapshot;
            throw new InvalidOperationException($"Blackboard Input Binding type '{type?.FullName}' has no portable input kind.");
        }

        CharacterSimulationSourceLocation DefinitionSource => AssetSource(m_Model.Definition, $"definition:{m_Model.Definition.name}");

        CharacterSimulationSourceLocation AssetSource(UnityEngine.Object asset, string identity) =>
            CharacterSemanticSourceFactory.Asset(m_Model, asset, identity);

        static ProgramCatalogField[] Fields(params ProgramCatalogField[] fields) => Fields((IEnumerable<ProgramCatalogField>)fields);
        static ProgramCatalogField[] Fields(IEnumerable<ProgramCatalogField> fields) =>
            fields?.Where(value => value != null).ToArray() ?? Array.Empty<ProgramCatalogField>();
    }
}
