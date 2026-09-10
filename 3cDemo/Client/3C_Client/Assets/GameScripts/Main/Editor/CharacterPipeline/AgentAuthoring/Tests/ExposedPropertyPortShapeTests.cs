using TreeDesigner.Authoring;
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
