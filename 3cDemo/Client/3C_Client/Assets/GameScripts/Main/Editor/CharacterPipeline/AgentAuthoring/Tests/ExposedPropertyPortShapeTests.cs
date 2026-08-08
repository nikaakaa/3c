using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThirdPersonCharacter.Pipeline.Input;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.Tests
{
    public sealed class ExposedPropertyPortShapeTests
    {
        const string CorinRootTreePath =
            "Assets/Configs/Character/Corin/Pipeline/Graphs/CorinPlayableRootTree.asset";
        const string AttackStateBodyGraphId =
            "3ae19d5e-dd52-4f44-a80d-e32d2474e7ec";

        [Test]
        public void SetNodeTypeMaintainsValueDirection()
        {
            var node = new ExposedPropertyNode();
            node.BeforeInit();

            node.SetNodeType(ExposedPropertyNodeType.Set);
            Assert.That(node.Value.Direction, Is.EqualTo(PortDirection.Input));

            node.SetNodeType(ExposedPropertyNodeType.Get);
            Assert.That(node.Value.Direction, Is.EqualTo(PortDirection.Output));
        }

        [TestCase(ExposedPropertyNodeType.Get, GraphAuthoringPortDirection.Output, GraphAuthoringPortCapacity.Multiple, 1)]
        [TestCase(ExposedPropertyNodeType.Set, GraphAuthoringPortDirection.Input, GraphAuthoringPortCapacity.Single, 2)]
        public void CatalogProjectsModeSpecificShape(
            ExposedPropertyNodeType mode,
            GraphAuthoringPortDirection valueDirection,
            GraphAuthoringPortCapacity valueCapacity,
            int portCount)
        {
            var catalog = new BtsmtlGraphAuthoringCapabilities();
            var node = new AgentSnapshotNode
            {
                typeName = typeof(ExposedPropertyNode).FullName,
                exposedProperty = new AgentSnapshotExposedProperty
                {
                    mode = mode.ToString()
                }
            };

            Assert.That(catalog.TryProjectSnapshotPortShape(
                node,
                out GraphAuthoringCapabilityDescriptor capability,
                out IReadOnlyList<GraphAuthoringDynamicPortProjection> projected,
                out GraphAuthoringPortShapeException error), Is.True, error?.Message);
            Assert.That(capability.FixedPorts.Any(value =>
                value.PortId.Equals(BtsmtlSharedGraphPort.Property("m_Value"))), Is.False);
            Assert.That(projected.Count, Is.EqualTo(portCount));
            GraphAuthoringDynamicPortProjection valuePort = projected.Single(value =>
                value.PortId.Equals(BtsmtlSharedGraphPort.Property("m_Value")));
            Assert.That(valuePort.Direction, Is.EqualTo(valueDirection));
            Assert.That(valuePort.Capacity, Is.EqualTo(valueCapacity));
            Assert.That(projected.Any(value =>
                    value.PortId.Equals(BtsmtlSharedGraphPort.Flow(ExposedPropertyNode.FlowInputPortName))),
                Is.EqualTo(mode == ExposedPropertyNodeType.Set));
        }

        [Test]
        public void NodeCatalogRejectsAmbiguousVariantCondition()
        {
            BtsmtlStateMachineAuthoringCapabilities.EnsureRegistered();
            var capabilities = new BtsmtlGraphAuthoringCapabilities();
            var catalog = new AgentPackageNodeCatalogFile
            {
                kinds = capabilities.ExportNodeKinds(AgentAuthoringSchema.CharacterControllerDomain).ToList()
            };
            AgentPackageNodeKindDescriptor exposed = catalog.kinds.Single(value =>
                string.Equals(value.kind, "exposed-property", StringComparison.Ordinal));
            exposed.portVariants.Add(AgentAuthoringDocumentCodec.Clone(exposed.portVariants[0]));
            var report = new AgentCompileReport();

            Assert.That(AgentPackageNodeCatalogValidator.Validate(catalog, report), Is.False);
            Assert.That(report.messages.Any(value =>
                string.Equals(value.code, "node_catalog_port_variant_identity_invalid", StringComparison.Ordinal) ||
                string.Equals(value.code, "node_catalog_port_variant_ambiguous", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void MutationPreflightRejectsShapeChangeWithoutEdgeDeletion()
        {
            AgentGraphSnapshot snapshot = CreateGetSnapshot();
            AgentMutationPlan plan = CreateSetPlan(includeDelete: false, includeLink: false);
            var report = new AgentCompileReport();

            Assert.That(AgentMutationPortShapePreflight.Validate(snapshot, plan, report), Is.False);
            Assert.That(report.messages.Any(value =>
                string.Equals(value.code, "port_shape_edge_delete_missing", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void MutationPreflightAcceptsDeleteConfigureLinkOrder()
        {
            AgentGraphSnapshot snapshot = CreateGetSnapshot();
            AgentMutationPlan plan = CreateSetPlan(includeDelete: true, includeLink: true);
            var report = new AgentCompileReport();

            Assert.That(AgentMutationPortShapePreflight.Validate(snapshot, plan, report), Is.True);
            Assert.That(report.HasErrors(), Is.False);
        }

        [Test]
        public void CorinAttackStateBodyOpensThroughRootTreeNavigation()
        {
            BaseTreeAsset asset = AssetDatabase.LoadAssetAtPath<BaseTreeAsset>(
                CorinRootTreePath);
            Assert.That(asset, Is.Not.Null);
            IReadOnlyList<NodeGraphReference> route = FindGraphRoute(
                asset.Tree,
                AttackStateBodyGraphId);
            Assert.That(route, Is.Not.Null);
            Assert.That(route.Count, Is.GreaterThan(0));
            BaseTree attackStateBody = route[route.Count - 1].Tree;
            Assert.That(attackStateBody.name, Is.EqualTo("Attack State Body"));

            ExposedPropertyNode setter = attackStateBody.Nodes
                .OfType<ExposedPropertyNode>()
                .Single();
            Assert.That(setter.NodeType, Is.EqualTo(ExposedPropertyNodeType.Set));
            Assert.That(setter.Value.Direction, Is.EqualTo(PortDirection.Input));

            BaseTreeWindow window = null;
            try
            {
                Assert.DoesNotThrow(() =>
                {
                    window = EditorWindow.CreateWindow<BaseTreeWindow>();
                    window.ReplaceNavigationRoot(asset);
                    window.Show();
                    foreach (NodeGraphReference reference in route)
                        window.PushReferencedTree(reference.OwnerNode, reference);
                });
                Assert.That(window.Tree, Is.SameAs(attackStateBody));
                Assert.That(window.TreeView.NodeViews.Count,
                    Is.EqualTo(attackStateBody.Nodes.Count));

                BaseNodeView setterView = window.TreeView.FindNodeView(setter);
                Assert.That(setterView, Is.Not.Null);
                Assert.That(setterView.InputPorts.ContainsKey(
                    ExposedPropertyNode.FlowInputPortName), Is.True);
                Assert.That(setterView.InputPropertyPorts.ContainsKey("m_Value"),
                    Is.True);
                Assert.That(setterView.OutputPropertyPorts.ContainsKey("m_Value"),
                    Is.False);
            }
            finally
            {
                if (window)
                    window.Close();
            }
        }

        static IReadOnlyList<NodeGraphReference> FindGraphRoute(
            BaseTree root,
            string graphAuthoringId)
        {
            var route = new List<NodeGraphReference>();
            var visited = new HashSet<BaseTree>();
            return TryFindGraphRoute(
                root,
                graphAuthoringId,
                route,
                visited)
                ? route
                : null;
        }

        static bool TryFindGraphRoute(
            BaseTree graph,
            string graphAuthoringId,
            List<NodeGraphReference> route,
            HashSet<BaseTree> visited)
        {
            if (graph == null || !visited.Add(graph))
                return false;
            if (string.Equals(
                    graph.GraphAuthoringId,
                    graphAuthoringId,
                    StringComparison.Ordinal))
                return true;
            foreach (BaseNode node in graph.Nodes.Where(value => value != null))
            {
                foreach (NodeGraphReference reference in node.GetGraphReferences())
                {
                    if (reference.Tree == null)
                        continue;
                    route.Add(reference);
                    if (TryFindGraphRoute(
                            reference.Tree,
                            graphAuthoringId,
                            route,
                            visited))
                        return true;
                    route.RemoveAt(route.Count - 1);
                }
            }
            return false;
        }

        static AgentGraphSnapshot CreateGetSnapshot()
        {
            return new AgentGraphSnapshot
            {
                graphs = new List<AgentSnapshotGraph>
                {
                    new AgentSnapshotGraph
                    {
                        graphAuthoringId = "graph",
                        nodes = new List<AgentSnapshotNode>
                        {
                            new AgentSnapshotNode
                            {
                                elementAuthoringId = "setter",
                                typeName = typeof(ExposedPropertyNode).FullName,
                                exposedProperty = new AgentSnapshotExposedProperty
                                {
                                    mode = ExposedPropertyNodeType.Get.ToString()
                                }
                            },
                            new AgentSnapshotNode
                            {
                                elementAuthoringId = "value",
                                typeName = typeof(PipelineBlackboardBoolInfoNode).FullName
                            }
                        },
                        propertyEdges = new List<AgentSnapshotPropertyEdge>
                        {
                            new AgentSnapshotPropertyEdge
                            {
                                elementAuthoringId = "old-edge",
                                startElementAuthoringId = "setter",
                                startPortId = "m_Value",
                                endElementAuthoringId = "value",
                                endPortId = "m_Value"
                            }
                        }
                    }
                }
            };
        }

        static AgentMutationPlan CreateSetPlan(bool includeDelete, bool includeLink)
        {
            var graph = new AgentGraphTargetReference(new AgentAuthoringReference("graph", default));
            var commands = new List<AgentMutation>();
            if (includeDelete)
                commands.Add(new AgentDeletePropertyEdgeMutation("delete", "test.delete", graph, "old-edge"));
            commands.Add(new AgentEnsureExposedPropertyNodeMutation(
                "configure",
                "test.configure",
                graph,
                "setter",
                new AgentAuthoringReference("declaration", default),
                ExposedPropertyNodeType.Set,
                typeof(bool),
                false,
                "Set Blackboard",
                Vector2.zero));
            if (includeLink)
            {
                commands.Add(new AgentLinkPropertyMutation(
                    "link",
                    "test.link",
                    graph,
                    new AgentElementTargetReference(new AgentAuthoringReference("value", default)),
                    new AgentElementTargetReference(new AgentAuthoringReference("setter", default)),
                    "m_Value",
                    "m_Value",
                    "new-edge"));
            }
            return new AgentMutationPlan(
                commands,
                AgentAuthoringSchema.CharacterControllerDomain,
                "root",
                "revision");
        }
    }
}
