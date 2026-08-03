using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using TreeDesigner.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterPoseTransitionRuleDocument :
        IGraphAuthoringDocumentProjection
    {
        readonly CharacterPresentationPoseGraphAsset m_Asset;
        readonly CharacterPoseStateMachineDefinition m_Machine;
        readonly PoseStateTransitionId m_TransitionId;

        public CharacterPoseTransitionRuleDocument(
            CharacterPresentationPoseGraphAsset asset,
            CharacterPoseStateMachineDefinition machine,
            PoseStateTransitionId transitionId)
        {
            m_Asset = asset
                ? asset
                : throw new ArgumentNullException(nameof(asset));
            m_Machine = machine ??
                throw new ArgumentNullException(nameof(machine));
            m_TransitionId = transitionId.IsValid
                ? transitionId
                : throw new ArgumentException(
                    "Pose Transition identity is invalid.",
                    nameof(transitionId));
            _ = Transition;
        }

        public GraphAuthoringDomainId DomainId =>
            CharacterPoseGraphAuthoringCapabilities.Domain;
        public GraphAuthoringDocumentRoleId DocumentRoleId =>
            CharacterPoseGraphAuthoringCapabilities.TransitionRule;
        public string DocumentId => Rule.GraphId.Value;
        public string DisplayName =>
            CharacterPoseAuthoringDisplayNames.Transition(
                m_Machine,
                Transition);
        public string ContentRevision => Rule.ContentRevision;
        public UnityEngine.Object SerializedOwner => m_Asset;
        public IReadOnlyList<GraphAuthoringPageProjection> Pages =>
            new[]
            {
                new GraphAuthoringPageProjection(
                    new GraphAuthoringElementId(DocumentId),
                    DisplayName,
                    DocumentRoleId.Value)
            };
        public IReadOnlyList<GraphAuthoringNodeProjection> Nodes =>
            ProjectNodes();
        public IReadOnlyList<GraphAuthoringEdgeProjection> Edges =>
            ProjectEdges();

        internal CharacterPresentationPoseGraphAsset Asset => m_Asset;
        internal CharacterPoseStateMachineDefinition Machine =>
            m_Machine;
        internal PoseStateTransitionId TransitionId =>
            m_TransitionId;
        internal CharacterPoseStateTransition Transition =>
            m_Machine.Transitions.Single(value =>
                value.TransitionId.Equals(m_TransitionId));
        internal CharacterPoseTransitionRuleGraph Rule =>
            Transition.Rule;

        IReadOnlyList<GraphAuthoringNodeProjection> ProjectNodes()
        {
            IReadOnlyList<CharacterPoseTransitionRuleOperation>
                operations = Rule.Operations;
            var byId = operations.ToDictionary(
                value => value.OperationId);
            var depths = new Dictionary<
                PoseTransitionRuleOperationId,
                int>();
            var visiting = new HashSet<
                PoseTransitionRuleOperationId>();
            for (int i = 0; i < operations.Count; i++)
                ResolveDepth(
                    operations[i],
                    byId,
                    depths,
                    visiting);
            var rowByDepth = new Dictionary<int, int>();
            var result =
                new GraphAuthoringNodeProjection[operations.Count];
            for (int i = 0; i < operations.Count; i++)
            {
                CharacterPoseTransitionRuleOperation operation =
                    operations[i];
                int depth = depths[operation.OperationId];
                int row = rowByDepth.TryGetValue(
                    depth,
                    out int nextRow)
                    ? nextRow
                    : 0;
                rowByDepth[depth] = row + 1;
                result[i] = new GraphAuthoringNodeProjection(
                    new GraphAuthoringElementId(
                        operation.OperationId.Value),
                    CharacterPoseGraphAuthoringCapabilities.Get(
                        operation.Kind),
                    string.Empty,
                    new Vector2(
                        depth * 280f,
                        row * 150f),
                    status:
                    operation.OperationId ==
                    Rule.OutputOperationId
                        ? "Rule Output"
                        : string.Empty);
            }
            return result;
        }

        IReadOnlyList<GraphAuthoringEdgeProjection> ProjectEdges()
        {
            HashSet<PoseTransitionRuleOperationId> operationIds =
                Rule.Operations
                    .Select(value => value.OperationId)
                    .ToHashSet();
            var result =
                new List<GraphAuthoringEdgeProjection>();
            foreach (CharacterPoseTransitionRuleOperation operation in
                     Rule.Operations)
            {
                AddEdge(
                    result,
                    operationIds,
                    operation,
                    operation.InputA,
                    "input-a");
                AddEdge(
                    result,
                    operationIds,
                    operation,
                    operation.InputB,
                    "input-b");
            }
            return result;
        }

        static void AddEdge(
            ICollection<GraphAuthoringEdgeProjection> result,
            ISet<PoseTransitionRuleOperationId>
                operationIds,
            CharacterPoseTransitionRuleOperation target,
            PoseTransitionRuleOperationId sourceId,
            string targetPortId)
        {
            if (!sourceId.IsValid ||
                !operationIds.Contains(sourceId))
            {
                return;
            }
            result.Add(new GraphAuthoringEdgeProjection(
                EdgeId(target.OperationId, targetPortId),
                new GraphAuthoringElementId(sourceId.Value),
                new GraphAuthoringPortId("result"),
                new GraphAuthoringElementId(
                    target.OperationId.Value),
                new GraphAuthoringPortId(targetPortId)));
        }

        internal static GraphAuthoringElementId EdgeId(
            PoseTransitionRuleOperationId targetId,
            string targetPortId) =>
            new GraphAuthoringElementId(
                $"{targetId.Value}:{targetPortId}");

        static int ResolveDepth(
            CharacterPoseTransitionRuleOperation operation,
            IReadOnlyDictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> byId,
            IDictionary<PoseTransitionRuleOperationId, int>
                depths,
            ISet<PoseTransitionRuleOperationId> visiting)
        {
            if (depths.TryGetValue(
                    operation.OperationId,
                    out int existing))
            {
                return existing;
            }
            if (!visiting.Add(operation.OperationId))
                return 0;
            int depth = 0;
            depth = Math.Max(
                depth,
                ResolveInputDepth(
                    operation.InputA,
                    byId,
                    depths,
                    visiting));
            depth = Math.Max(
                depth,
                ResolveInputDepth(
                    operation.InputB,
                    byId,
                    depths,
                    visiting));
            visiting.Remove(operation.OperationId);
            depths[operation.OperationId] = depth;
            return depth;
        }

        static int ResolveInputDepth(
            PoseTransitionRuleOperationId inputId,
            IReadOnlyDictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> byId,
            IDictionary<PoseTransitionRuleOperationId, int>
                depths,
            ISet<PoseTransitionRuleOperationId> visiting)
        {
            return inputId.IsValid &&
                   byId.TryGetValue(
                       inputId,
                       out CharacterPoseTransitionRuleOperation input)
                ? ResolveDepth(
                      input,
                      byId,
                      depths,
                      visiting) + 1
                : 0;
        }
    }

    public sealed class CharacterPoseTransitionRuleMutationAdapter :
        IGraphAuthoringDomainMutation
    {
        readonly CharacterPresentationMutationService m_Service =
            new CharacterPresentationMutationService();

        public bool ReadOnly { get; set; }

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringMutationRequest request) =>
            Apply(document, new[] { request });

        public void Apply(
            IGraphAuthoringDocumentProjection document,
            IReadOnlyList<GraphAuthoringMutationRequest> requests)
        {
            if (ReadOnly)
                throw new InvalidOperationException(
                    "Pose Transition Rule document is read-only.");
            CharacterPoseTransitionRuleDocument ruleDocument =
                document as
                    CharacterPoseTransitionRuleDocument ??
                throw new ArgumentException(
                    "Pose Transition Rule mutation requires the Presentation adapter.",
                    nameof(document));
            CharacterPoseTransitionRuleGraph current =
                ruleDocument.Rule;
            var operations = current.Operations.ToDictionary(
                value => value.OperationId);
            var order = current.Operations
                .Select(value => value.OperationId)
                .ToList();
            PoseTransitionRuleOperationId outputId =
                current.OutputOperationId;
            foreach (GraphAuthoringMutationRequest request in
                     requests ??
                     throw new ArgumentNullException(nameof(requests)))
            {
                ApplyRequest(
                    request,
                    operations,
                    order,
                    ref outputId);
            }
            CharacterPoseTransitionRuleOperation[] nextOperations =
                order.Select(value => operations[value]).ToArray();
            var nextRule =
                new CharacterPoseTransitionRuleGraph(
                    current.GraphId,
                    Guid.NewGuid().ToString("N"),
                    nextOperations,
                    outputId);
            var transaction =
                new CharacterPresentationMutationTransaction(
                    Guid.NewGuid().ToString("N"),
                    "Edit Pose Transition Rule");
            transaction.Add(
                new SetPoseTransitionFieldMutation(
                    ruleDocument.Machine.StateMachineId.Value,
                    ruleDocument.TransitionId,
                    "rule",
                    nextRule));
            m_Service.Apply(
                new CharacterPoseGraphAssetMutationOwner(
                    ruleDocument.Asset),
                transaction);
        }

        static void ApplyRequest(
            GraphAuthoringMutationRequest request,
            IDictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> operations,
            IList<PoseTransitionRuleOperationId> order,
            ref PoseTransitionRuleOperationId outputId)
        {
            switch (request.Kind)
            {
                case GraphAuthoringMutationKind.CreateNode:
                {
                    CharacterPoseTransitionRuleOperation operation =
                        request.Value as
                            CharacterPoseTransitionRuleOperation ??
                        throw new InvalidOperationException(
                            "Create Transition Rule operation requires one complete typed payload.");
                    if (operations.ContainsKey(
                            operation.OperationId))
                    {
                        throw new InvalidOperationException(
                            $"Transition Rule operation '{operation.OperationId}' already exists.");
                    }
                    operations.Add(
                        operation.OperationId,
                        operation);
                    order.Add(operation.OperationId);
                    return;
                }
                case GraphAuthoringMutationKind.DeleteElement:
                {
                    var operationId =
                        new PoseTransitionRuleOperationId(
                            request.TargetId.Value);
                    if (operationId == outputId)
                    {
                        throw new InvalidOperationException(
                            "Select another Bool operation as the Rule Output before deleting the current output.");
                    }
                    if (!operations.Remove(operationId))
                    {
                        throw new InvalidOperationException(
                            $"Transition Rule operation '{operationId}' does not exist.");
                    }
                    order.Remove(operationId);
                    PoseTransitionRuleOperationId[] dependents =
                        order.Where(value =>
                                operations[value].InputA ==
                                operationId ||
                                operations[value].InputB ==
                                operationId)
                            .ToArray();
                    foreach (PoseTransitionRuleOperationId dependentId
                             in dependents)
                    {
                        CharacterPoseTransitionRuleOperation dependent =
                            operations[dependentId];
                        operations[dependentId] = WithInputs(
                            dependent,
                            dependent.InputA == operationId
                                ? default
                                : dependent.InputA,
                            dependent.InputB == operationId
                                ? default
                                : dependent.InputB);
                    }
                    return;
                }
                case GraphAuthoringMutationKind.ConnectPorts:
                {
                    RequirePort(
                        request.SourcePortId,
                        "result");
                    var sourceId =
                        new PoseTransitionRuleOperationId(
                            request.SourceNodeId.Value);
                    var targetId =
                        new PoseTransitionRuleOperationId(
                            request.TargetNodeId.Value);
                    CharacterPoseTransitionRuleOperation target =
                        Require(operations, targetId);
                    Require(operations, sourceId);
                    operations[targetId] =
                        request.TargetPortId.Value switch
                        {
                            "input-a" => WithInputs(
                                target,
                                sourceId,
                                target.InputB),
                            "input-b" => WithInputs(
                                target,
                                target.InputA,
                                sourceId),
                            _ => throw new InvalidOperationException(
                                $"Transition Rule target port '{request.TargetPortId}' is not an operation input.")
                        };
                    return;
                }
                case GraphAuthoringMutationKind.DisconnectEdge:
                {
                    foreach (PoseTransitionRuleOperationId operationId
                             in order)
                    {
                        CharacterPoseTransitionRuleOperation operation =
                            operations[operationId];
                        if (request.TargetId.Equals(
                                CharacterPoseTransitionRuleDocument
                                    .EdgeId(
                                        operationId,
                                        "input-a")))
                        {
                            operations[operationId] = WithInputs(
                                operation,
                                default,
                                operation.InputB);
                            return;
                        }
                        if (request.TargetId.Equals(
                                CharacterPoseTransitionRuleDocument
                                    .EdgeId(
                                        operationId,
                                        "input-b")))
                        {
                            operations[operationId] = WithInputs(
                                operation,
                                operation.InputA,
                                default);
                            return;
                        }
                    }
                    throw new InvalidOperationException(
                        $"Transition Rule edge '{request.TargetId}' does not exist.");
                }
                case GraphAuthoringMutationKind.SetField:
                {
                    var operationId =
                        new PoseTransitionRuleOperationId(
                            request.TargetId.Value);
                    operations[operationId] = SetField(
                        Require(operations, operationId),
                        request.FieldId.Value,
                        request.Value);
                    return;
                }
                case GraphAuthoringMutationKind.ExecuteCommand:
                {
                    if (!string.Equals(
                            request.CommandId.Value,
                            "set-rule-output",
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Transition Rule command '{request.CommandId}' is not supported.");
                    }
                    var operationId =
                        new PoseTransitionRuleOperationId(
                            request.TargetId.Value);
                    RuleValueSignature signature =
                        CharacterPoseTransitionRuleAuthoringSchema
                            .RequireSignature(
                                Require(
                                    operations,
                                    operationId));
                    if (signature.Kind !=
                        PoseTransitionRuleValueKind.Bool)
                    {
                        throw new InvalidOperationException(
                            $"Transition Rule output '{operationId}' must be Bool.");
                    }
                    outputId = operationId;
                    return;
                }
                default:
                    throw new InvalidOperationException(
                        $"Shared Graph command '{request.Kind}' is not valid for a Pose Transition Rule.");
            }
        }

        static CharacterPoseTransitionRuleOperation SetField(
            CharacterPoseTransitionRuleOperation operation,
            string fieldId,
            object value)
        {
            PresentationFactId factId = operation.FactId;
            bool boolLiteral = operation.BoolLiteral;
            float floatLiteral = operation.FloatLiteral;
            string enumTypeId = operation.EnumTypeId;
            int enumLiteral = operation.EnumLiteral;
            string identityLiteral = operation.IdentityLiteral;
            switch (fieldId)
            {
                case "fact-id":
                    factId = new PresentationFactId(
                        value?.ToString() ??
                        string.Empty);
                    _ = CharacterPoseTransitionRuleAuthoringSchema
                        .RequireFactSignature(factId);
                    break;
                case "bool-literal":
                    boolLiteral = Convert.ToBoolean(value);
                    break;
                case "float-literal":
                    floatLiteral = Convert.ToSingle(value);
                    if (!float.IsFinite(floatLiteral))
                    {
                        throw new InvalidOperationException(
                            "Transition Rule float literal must be finite.");
                    }
                    break;
                case "enum-literal":
                    enumTypeId =
                        PoseTransitionRuleEnumTypes
                            .CharacterPresentationMotionPhase;
                    enumLiteral = value is
                        CharacterPresentationMotionPhase typed
                        ? (int)typed
                        : (int)Enum.Parse<
                            CharacterPresentationMotionPhase>(
                            value?.ToString() ??
                            string.Empty,
                            false);
                    break;
                case "identity-literal":
                    identityLiteral = value?.ToString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(identityLiteral))
                    {
                        throw new InvalidOperationException(
                            "Transition Rule identity literal is missing.");
                    }
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Transition Rule operation '{operation.Kind}' does not declare field '{fieldId}'.");
            }
            return new CharacterPoseTransitionRuleOperation(
                operation.OperationId,
                operation.Kind,
                operation.InputA,
                operation.InputB,
                factId,
                boolLiteral,
                floatLiteral,
                enumTypeId,
                enumLiteral,
                identityLiteral);
        }

        static CharacterPoseTransitionRuleOperation WithInputs(
            CharacterPoseTransitionRuleOperation operation,
            PoseTransitionRuleOperationId inputA,
            PoseTransitionRuleOperationId inputB) =>
            new CharacterPoseTransitionRuleOperation(
                operation.OperationId,
                operation.Kind,
                inputA,
                inputB,
                operation.FactId,
                operation.BoolLiteral,
                operation.FloatLiteral,
                operation.EnumTypeId,
                operation.EnumLiteral,
                operation.IdentityLiteral);

        static CharacterPoseTransitionRuleOperation Require(
            IDictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> operations,
            PoseTransitionRuleOperationId operationId) =>
            operations.TryGetValue(
                operationId,
                out CharacterPoseTransitionRuleOperation operation)
                ? operation
                : throw new InvalidOperationException(
                    $"Transition Rule operation '{operationId}' does not exist.");

        static void RequirePort(
            GraphAuthoringPortId actual,
            string expected)
        {
            if (!string.Equals(
                    actual.Value,
                    expected,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Transition Rule source port must be '{expected}'.");
            }
        }
    }

    public sealed class CharacterPoseTransitionRuleConnectionPolicy :
        IGraphAuthoringConnectionPolicy
    {
        public bool CanConnect(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringNodeProjection sourceNode,
            GraphAuthoringPortId sourcePortId,
            GraphAuthoringNodeProjection targetNode,
            GraphAuthoringPortId targetPortId)
        {
            CharacterPoseTransitionRuleDocument ruleDocument =
                document as
                    CharacterPoseTransitionRuleDocument;
            if (ruleDocument == null ||
                sourceNode == null ||
                targetNode == null ||
                sourceNode.NodeId.Equals(targetNode.NodeId) ||
                !string.Equals(
                    sourcePortId.Value,
                    "result",
                    StringComparison.Ordinal) ||
                targetPortId.Value != "input-a" &&
                targetPortId.Value != "input-b")
            {
                return false;
            }
            CharacterPoseTransitionRuleOperation source =
                ruleDocument.Rule.Operations.Single(value =>
                    value.OperationId.Value ==
                    sourceNode.NodeId.Value);
            CharacterPoseTransitionRuleOperation target =
                ruleDocument.Rule.Operations.Single(value =>
                    value.OperationId.Value ==
                    targetNode.NodeId.Value);
            if (targetPortId.Value == "input-a" &&
                    target.InputA.IsValid ||
                targetPortId.Value == "input-b" &&
                    target.InputB.IsValid)
            {
                return false;
            }
            Dictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> byId =
                ruleDocument.Rule.Operations.ToDictionary(
                    value => value.OperationId);
            if (DependsOn(
                    source,
                    target.OperationId,
                    byId,
                    new HashSet<
                        PoseTransitionRuleOperationId>()))
            {
                return false;
            }
            RuleValueSignature sourceSignature;
            try
            {
                sourceSignature =
                    CharacterPoseTransitionRuleAuthoringSchema
                        .RequireSignature(source);
            }
            catch
            {
                return false;
            }
            return Accepts(
                target,
                targetPortId.Value,
                sourceSignature,
                byId);
        }

        static bool Accepts(
            CharacterPoseTransitionRuleOperation target,
            string targetPortId,
            RuleValueSignature source,
            IReadOnlyDictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> byId)
        {
            switch (target.Kind)
            {
                case PoseTransitionRuleOperationKind.Not:
                case PoseTransitionRuleOperationKind.And:
                case PoseTransitionRuleOperationKind.Or:
                    return source.Kind ==
                           PoseTransitionRuleValueKind.Bool;
                case PoseTransitionRuleOperationKind.Greater:
                case PoseTransitionRuleOperationKind.GreaterOrEqual:
                case PoseTransitionRuleOperationKind.Less:
                case PoseTransitionRuleOperationKind.LessOrEqual:
                    return source.Kind ==
                           PoseTransitionRuleValueKind.Float;
                case PoseTransitionRuleOperationKind.Equal:
                case PoseTransitionRuleOperationKind.NotEqual:
                {
                    PoseTransitionRuleOperationId otherId =
                        targetPortId == "input-a"
                            ? target.InputB
                            : target.InputA;
                    if (!otherId.IsValid ||
                        !byId.TryGetValue(
                            otherId,
                            out CharacterPoseTransitionRuleOperation
                                other))
                    {
                        return true;
                    }
                    try
                    {
                        return source.Equals(
                            CharacterPoseTransitionRuleAuthoringSchema
                                .RequireSignature(other));
                    }
                    catch
                    {
                        return false;
                    }
                }
                default:
                    return false;
            }
        }

        static bool DependsOn(
            CharacterPoseTransitionRuleOperation operation,
            PoseTransitionRuleOperationId targetId,
            IReadOnlyDictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> byId,
            ISet<PoseTransitionRuleOperationId> visited)
        {
            if (!visited.Add(operation.OperationId))
                return false;
            if (operation.InputA == targetId ||
                operation.InputB == targetId)
            {
                return true;
            }
            return DependsOn(
                       operation.InputA,
                       targetId,
                       byId,
                       visited) ||
                   DependsOn(
                       operation.InputB,
                       targetId,
                       byId,
                       visited);
        }

        static bool DependsOn(
            PoseTransitionRuleOperationId operationId,
            PoseTransitionRuleOperationId targetId,
            IReadOnlyDictionary<
                PoseTransitionRuleOperationId,
                CharacterPoseTransitionRuleOperation> byId,
            ISet<PoseTransitionRuleOperationId> visited) =>
            operationId.IsValid &&
            byId.TryGetValue(
                operationId,
                out CharacterPoseTransitionRuleOperation operation) &&
            DependsOn(
                operation,
                targetId,
                byId,
                visited);
    }

    public sealed class CharacterPoseTransitionRuleDetailsDataSource :
        IGraphAuthoringDetailsDataSource
    {
        public object ReadField(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringElementId elementId,
            GraphAuthoringFieldDescriptor field)
        {
            CharacterPoseTransitionRuleOperation operation =
                ((CharacterPoseTransitionRuleDocument)document)
                .Rule.Operations.Single(value =>
                    value.OperationId.Value == elementId.Value);
            return field.FieldId.Value switch
            {
                "fact-id" => operation.FactId.Value,
                "bool-literal" => operation.BoolLiteral,
                "float-literal" => operation.FloatLiteral,
                "enum-type-id" => operation.EnumTypeId,
                "enum-literal" =>
                    ((CharacterPresentationMotionPhase)
                        operation.EnumLiteral).ToString(),
                "identity-literal" => operation.IdentityLiteral,
                _ => throw new InvalidOperationException(
                    $"Transition Rule operation '{operation.Kind}' does not declare field '{field.FieldId}'.")
            };
        }

        public IReadOnlyList<GraphAuthoringReadOnlyDetail> GetLive(
            IGraphAuthoringDocumentProjection document,
            GraphAuthoringSelection selection) =>
            Array.Empty<GraphAuthoringReadOnlyDetail>();

        public IReadOnlyList<GraphAuthoringReadOnlyDetail>
            GetReferences(
                IGraphAuthoringDocumentProjection document,
                GraphAuthoringSelection selection)
        {
            var rule =
                (CharacterPoseTransitionRuleDocument)document;
            if (selection.Kind !=
                GraphAuthoringSelectionKind.Node)
            {
                return Array.Empty<
                    GraphAuthoringReadOnlyDetail>();
            }
            CharacterPoseTransitionRuleOperation operation =
                rule.Rule.Operations.Single(value =>
                    value.OperationId.Value ==
                    selection.ElementId.Value);
            return new[]
            {
                new GraphAuthoringReadOnlyDetail(
                    "Rule Graph",
                    rule.Rule.GraphId.Value),
                new GraphAuthoringReadOnlyDetail(
                    "Value Kind",
                    CharacterPoseTransitionRuleAuthoringSchema
                        .RequireSignature(operation)
                        .ToString()),
                new GraphAuthoringReadOnlyDetail(
                    "Rule Output",
                    operation.OperationId ==
                    rule.Rule.OutputOperationId
                        ? "Yes"
                        : "No")
            };
        }

        public IReadOnlyList<GraphAuthoringReadOnlyDetail>
            GetDiagnostics(
                IGraphAuthoringDocumentProjection document,
                GraphAuthoringSelection selection) =>
            Array.Empty<GraphAuthoringReadOnlyDetail>();
    }

    public static class CharacterPoseTransitionRuleOperationFactory
    {
        public static CharacterPoseTransitionRuleOperation Create(
            PoseTransitionRuleOperationKind kind)
        {
            var operationId =
                new PoseTransitionRuleOperationId(
                    Guid.NewGuid().ToString("N"));
            return kind switch
            {
                PoseTransitionRuleOperationKind.FactInput =>
                    new CharacterPoseTransitionRuleOperation(
                        operationId,
                        kind,
                        factId:
                        CharacterPresentationFactSchema.Grounded),
                PoseTransitionRuleOperationKind.BoolLiteral =>
                    new CharacterPoseTransitionRuleOperation(
                        operationId,
                        kind,
                        boolLiteral: false),
                PoseTransitionRuleOperationKind.FloatLiteral =>
                    new CharacterPoseTransitionRuleOperation(
                        operationId,
                        kind,
                        floatLiteral: 0f),
                PoseTransitionRuleOperationKind.EnumLiteral =>
                    new CharacterPoseTransitionRuleOperation(
                        operationId,
                        kind,
                        enumTypeId:
                        PoseTransitionRuleEnumTypes
                            .CharacterPresentationMotionPhase,
                        enumLiteral:
                        (int)CharacterPresentationMotionPhase
                            .GroundedStationary),
                PoseTransitionRuleOperationKind.IdentityLiteral =>
                    new CharacterPoseTransitionRuleOperation(
                        operationId,
                        kind,
                        identityLiteral:
                        CharacterPresentationTrajectoryIntent
                            .StationaryMovementModeId),
                _ => new CharacterPoseTransitionRuleOperation(
                    operationId,
                    kind)
            };
        }
    }

    readonly struct RuleValueSignature :
        IEquatable<RuleValueSignature>
    {
        public RuleValueSignature(
            PoseTransitionRuleValueKind kind,
            string enumTypeId = "")
        {
            Kind = kind;
            EnumTypeId = enumTypeId ?? string.Empty;
        }

        public PoseTransitionRuleValueKind Kind { get; }
        public string EnumTypeId { get; }

        public bool Equals(RuleValueSignature other) =>
            Kind == other.Kind &&
            string.Equals(
                EnumTypeId,
                other.EnumTypeId,
                StringComparison.Ordinal);
        public override bool Equals(object obj) =>
            obj is RuleValueSignature other &&
            Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^
                       StringComparer.Ordinal.GetHashCode(
                           EnumTypeId);
            }
        }
        public override string ToString() =>
            Kind == PoseTransitionRuleValueKind.Enum
                ? $"{Kind} ({EnumTypeId})"
                : Kind.ToString();
    }

    static class CharacterPoseTransitionRuleAuthoringSchema
    {
        public static RuleValueSignature RequireSignature(
            CharacterPoseTransitionRuleOperation operation)
        {
            switch (operation.Kind)
            {
                case PoseTransitionRuleOperationKind.FactInput:
                    return RequireFactSignature(operation.FactId);
                case PoseTransitionRuleOperationKind.BoolLiteral:
                case PoseTransitionRuleOperationKind.Not:
                case PoseTransitionRuleOperationKind.And:
                case PoseTransitionRuleOperationKind.Or:
                case PoseTransitionRuleOperationKind.Equal:
                case PoseTransitionRuleOperationKind.NotEqual:
                case PoseTransitionRuleOperationKind.Greater:
                case PoseTransitionRuleOperationKind.GreaterOrEqual:
                case PoseTransitionRuleOperationKind.Less:
                case PoseTransitionRuleOperationKind.LessOrEqual:
                    return new RuleValueSignature(
                        PoseTransitionRuleValueKind.Bool);
                case PoseTransitionRuleOperationKind.FloatLiteral:
                case PoseTransitionRuleOperationKind.TimeInState:
                case PoseTransitionRuleOperationKind
                    .StatePoseRemainingTime:
                    return new RuleValueSignature(
                        PoseTransitionRuleValueKind.Float);
                case PoseTransitionRuleOperationKind.EnumLiteral:
                    if (!PoseTransitionRuleEnumTypes.IsDefined(
                            operation.EnumTypeId,
                            operation.EnumLiteral))
                    {
                        throw new InvalidOperationException(
                            $"Transition Rule enum literal '{operation.OperationId}' is invalid.");
                    }
                    return new RuleValueSignature(
                        PoseTransitionRuleValueKind.Enum,
                        operation.EnumTypeId);
                case PoseTransitionRuleOperationKind.IdentityLiteral:
                    if (string.IsNullOrWhiteSpace(
                            operation.IdentityLiteral))
                    {
                        throw new InvalidOperationException(
                            $"Transition Rule identity literal '{operation.OperationId}' is missing.");
                    }
                    return new RuleValueSignature(
                        PoseTransitionRuleValueKind.Identity);
                default:
                    throw new InvalidOperationException(
                        $"Transition Rule operation '{operation.OperationId}' has no authoring signature.");
            }
        }

        public static RuleValueSignature RequireFactSignature(
            PresentationFactId factId)
        {
            PresentationFactValueKind kind =
                CharacterPresentationFactSchema.RequireValueKind(
                    factId);
            return kind switch
            {
                PresentationFactValueKind.Bool =>
                    new RuleValueSignature(
                        PoseTransitionRuleValueKind.Bool),
                PresentationFactValueKind.Float =>
                    new RuleValueSignature(
                        PoseTransitionRuleValueKind.Float),
                PresentationFactValueKind.Enum
                    when factId ==
                         CharacterPresentationFactSchema
                             .MotionPhase =>
                    new RuleValueSignature(
                        PoseTransitionRuleValueKind.Enum,
                        PoseTransitionRuleEnumTypes
                            .CharacterPresentationMotionPhase),
                PresentationFactValueKind.Identity =>
                    new RuleValueSignature(
                        PoseTransitionRuleValueKind.Identity),
                _ => throw new InvalidOperationException(
                    $"Presentation Fact '{factId}' is not a Bool, Float, Enum, or Identity Transition Rule input.")
            };
        }
    }
}
