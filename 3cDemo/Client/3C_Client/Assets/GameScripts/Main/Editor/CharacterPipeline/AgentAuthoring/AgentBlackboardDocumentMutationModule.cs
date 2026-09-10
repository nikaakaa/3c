using System;
using System.Collections.Generic;
using System.Linq;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentDocumentMutationSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentBlackboardDocumentMutationModule
    {
        internal static void BuildCharacterBlackboardMutations(
            IReadOnlyList<AgentSnapshotBlackboardDeclaration> current,
            IReadOnlyList<AgentSnapshotBlackboardDeclaration> target,
            IReadOnlyList<AgentSnapshotGraph> targetGraphs,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            bool normalize)
        {
            var oldValues = Index(current, value => value.declarationId, "document.editable.blackboardDeclarations", report);
            var newValues = Index(target, value => value.declarationId, "document.editable.blackboardDeclarations", report);
            foreach (AgentSnapshotBlackboardDeclaration declaration in target ?? Array.Empty<AgentSnapshotBlackboardDeclaration>())
            {
                string path = $"document.editable.blackboardDeclarations[{Escape(declaration.declarationId)}]";
                if (normalize || IsLocal(declaration.declarationId) || !oldValues.TryGetValue(declaration.declarationId, out AgentSnapshotBlackboardDeclaration oldValue) || !Same(oldValue, declaration))
                {
                    Add(mutations, path, AgentMutationKind.EnsureBlackboardDeclaration, operation =>
                    {
                        if (IsLocal(declaration.declarationId))
                            operation.id = LocalIdentity(declaration.declarationId);
                        AgentSnapshotGraph graph = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                            .FirstOrDefault(value => string.Equals(value.graphAuthoringId, declaration.ownerId, StringComparison.Ordinal));
                        if (graph != null)
                            SetGraph(operation, graph);
                        else
                            SetGraph(operation, declaration.ownerId);
                        if (!IsLocal(declaration.declarationId))
                            operation.declarationAuthoringId = declaration.declarationId;
                        operation.blackboardKey = declaration.key;
                        operation.blackboardValueType = declaration.valueType;
                        operation.blackboardDefaultValue = declaration.defaultValue?.DeepClone();
                        operation.blackboardScope = declaration.scope;
                        operation.blackboardLifetime = declaration.lifetime;
                        operation.inputBinding = declaration.inputBinding;
                        operation.factProjection = declaration.factProjection;
                        operation.categoryPath = declaration.categoryPath;
                    });
                }
            }
            foreach (AgentSnapshotBlackboardDeclaration declaration in current ?? Array.Empty<AgentSnapshotBlackboardDeclaration>())
            {
                if (newValues.ContainsKey(declaration.declarationId))
                    continue;
                Add(mutations, $"document.editable.blackboardDeclarations[{Escape(declaration.declarationId)}]", AgentMutationKind.DeleteBlackboardDeclaration, operation =>
                {
                    SetGraph(operation, declaration.ownerId);
                    operation.declarationAuthoringId = declaration.declarationId;
                });
            }
        }

        internal static void BuildBlackboardSchemaRevisionMutation(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            bool required)
        {
            if (!required)
                return;
            Add(
                mutations,
                "document.editable.blackboardSchemaRevision",
                AgentMutationKind.SetBlackboardSchemaRevision,
                operation =>
                {
                    SetGraph(operation, string.Empty);
                    operation.blackboardSchemaRevision = target.blackboardSchemaRevision;
                });
        }
    }
}
