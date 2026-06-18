using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior.Editor.ActionBranch;
using ThirdPersonCharacterBehavior.Editor.ActionTimeline;
using ThirdPersonCharacterBehavior.Editor.Graph;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Tests.Editor.Character.Action.Branch
{
    public sealed class CommittedActionBranchEditorAdapterTests
    {
        const string DodgeAssetPath = "Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset";

        [Test]
        public void BranchAdapterCapturesFormalDodgeNodesAndSelectedTimeline()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter branchAdapter = new CommittedActionBranchSerializedAdapter(clone);
                CommittedActionBranchEditorSnapshot snapshot = branchAdapter.Capture();

                Assert.AreEqual("action.dodge", snapshot.BranchId);
                Assert.AreEqual("branch.root.action.dodge", snapshot.RootNodeId);
                Assert.That(snapshot.Nodes.Count, Is.EqualTo(6));
                Assert.That(snapshot.Nodes.Count(node => node.Kind == CommittedActionNodeKind.Root), Is.EqualTo(1));
                Assert.That(snapshot.Nodes.Count(node => node.Kind == CommittedActionNodeKind.Selector), Is.EqualTo(1));
                Assert.That(snapshot.Nodes.Count(node => node.Kind == CommittedActionNodeKind.Timeline), Is.EqualTo(2));
                Assert.True(branchAdapter.TryGetTimelineNodeId(CommittedActionTimelineVariant.Directional, out string directionalNodeId));
                Assert.AreEqual("timeline.dodge.directional", directionalNodeId);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchAdapterMarksRootAsProtectedSnapshot()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);
                CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
                CommittedActionBranchNodeEditorSnapshot root =
                    snapshot.Nodes.Single(node => node.NodeId == snapshot.RootNodeId);
                CharacterBehaviorRefPortedGraphSnapshot graphSnapshot =
                    new CommittedActionBranchRefPortedGraphAdapter(adapter).Capture();
                CharacterBehaviorRefPortedGraphNodeSnapshot graphRoot =
                    graphSnapshot.Nodes.Single(node => node.StableId == snapshot.RootNodeId);

                Assert.True(root.IsRoot);
                Assert.True(root.IsProtected);
                Assert.False(root.CanDelete);
                Assert.True(graphRoot.IsRoot);
                Assert.False(graphRoot.CanDelete);
                Assert.False(graphRoot.CanCopy);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchAdapterExplicitlyInitializesMinimalBranchTemplate()
        {
            CharacterActionDefinitionSO clone = CloneDodgeActionWithEmptyBranch();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);

                Assert.True(adapter.InitializeMinimalBranchTemplate(out string diagnostic), diagnostic);

                CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
                Assert.AreEqual("action.dodge", snapshot.BranchId);
                Assert.AreEqual("branch.root.action.dodge", snapshot.RootNodeId);
                Assert.AreEqual(2, snapshot.Nodes.Count);
                Assert.True(snapshot.Nodes.Single(node => node.NodeId == snapshot.RootNodeId).IsRoot);
                Assert.True(snapshot.Nodes.Any(node => node.NodeId == "timeline.action.dodge.main" && node.IsTimeline));
                CommittedActionTimelineSerializedAdapter timelineAdapter =
                    new CommittedActionTimelineSerializedAdapter(clone, "timeline.action.dodge.main");
                Assert.AreEqual(2, TrackCount(timelineAdapter, CommittedActionTimelineVariant.Generic));
                Assert.False(clone.Validate(CompileContext()).HasErrors);
                CharacterActionDefinition definition = clone.ToDefinition(CompileContext());
                Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
                Assert.AreEqual(CommittedActionNodeKind.Root, branch.RootNode.Kind);
                Assert.AreEqual("branch.root.action.dodge", branch.RootNode.NodeId.Value);
                Assert.AreEqual("timeline.action.dodge.main", branch.RootNode.ChildIds[0].Value);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchAdapterExplicitlyInitializesDodgeBranchTemplate()
        {
            CharacterActionDefinitionSO clone = CloneDodgeActionWithEmptyBranch();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);

                Assert.True(adapter.InitializeDodgeBranchTemplate(out string diagnostic), diagnostic);

                CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
                Assert.AreEqual("action.dodge", snapshot.BranchId);
                Assert.AreEqual("branch.root.action.dodge", snapshot.RootNodeId);
                Assert.That(snapshot.Nodes.Count(node => node.Kind == CommittedActionNodeKind.Root), Is.EqualTo(1));
                Assert.That(snapshot.Nodes.Count(node => node.Kind == CommittedActionNodeKind.Selector), Is.EqualTo(1));
                Assert.That(snapshot.Nodes.Count(node => node.Kind == CommittedActionNodeKind.Condition), Is.EqualTo(2));
                Assert.That(snapshot.Nodes.Count(node => node.Kind == CommittedActionNodeKind.Timeline), Is.EqualTo(2));
                Assert.True(snapshot.Nodes.Any(node => node.NodeId == "condition.dodge.directional" &&
                                                       node.ExpectedVariant == CharacterStateVariant.Directional));
                Assert.True(snapshot.Nodes.Any(node => node.NodeId == "condition.dodge.backstep" &&
                                                       node.ExpectedVariant == CharacterStateVariant.Backstep));
                CommittedActionTimelineSerializedAdapter directional =
                    new CommittedActionTimelineSerializedAdapter(clone, "timeline.dodge.directional");
                CommittedActionTimelineSerializedAdapter backstep =
                    new CommittedActionTimelineSerializedAdapter(clone, "timeline.dodge.backstep");
                Assert.AreEqual(2, TrackCount(directional, CommittedActionTimelineVariant.Directional));
                Assert.AreEqual(2, TrackCount(backstep, CommittedActionTimelineVariant.Backstep));
                Assert.False(clone.Validate(CompileContext()).HasErrors);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchAdapterRejectsRootDeletion()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);

                Assert.False(adapter.RemoveNode("branch.root.action.dodge", out string diagnostic));
                Assert.That(diagnostic, Does.Contain("root-node-protected:branch.root.action.dodge"));
                CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
                Assert.AreEqual("branch.root.action.dodge", snapshot.RootNodeId);
                Assert.True(snapshot.Nodes.Any(node => node.NodeId == "branch.root.action.dodge"));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchAdapterDeletesOrdinaryNodeAndKeepsRootContract()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);

                Assert.True(adapter.RemoveNode("condition.dodge.backstep", out string diagnostic), diagnostic);

                CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
                Assert.AreEqual("branch.root.action.dodge", snapshot.RootNodeId);
                Assert.True(snapshot.Nodes.Any(node => node.NodeId == "branch.root.action.dodge" && node.IsRoot));
                Assert.False(snapshot.Nodes.Any(node => node.NodeId == "condition.dodge.backstep"));
                Assert.True(adapter.TryGetNodeProperty("selector.dodge", out SerializedProperty selector, out string selectorDiagnostic), selectorDiagnostic);
                Assert.That(Children(selector), Does.Not.Contain("condition.dodge.backstep"));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void SelectedTimelineAdapterWritesBackOnlySelectedTimelineNode()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                SerializedObject serialized = new SerializedObject(clone);
                CommittedActionTimelineSerializedAdapter directionalAdapter =
                    new CommittedActionTimelineSerializedAdapter(clone, serialized, "timeline.dodge.directional");
                CommittedActionTimelineSerializedAdapter backstepAdapter =
                    new CommittedActionTimelineSerializedAdapter(clone, serialized, "timeline.dodge.backstep");
                int originalBackstepTrackCount = TrackCount(backstepAdapter, CommittedActionTimelineVariant.Backstep);

                Assert.True(directionalAdapter.AddTrack(
                    CommittedActionTimelineVariant.Directional,
                    ActionTimelineTrackKind.Cue,
                    out string addDiagnostic), addDiagnostic);

                Assert.AreEqual(originalBackstepTrackCount, TrackCount(backstepAdapter, CommittedActionTimelineVariant.Backstep));
                ActionTimelineCompileContext compileContext = CompileContext();
                CharacterActionDefinition definition = clone.ToDefinition(in compileContext);
                Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
                Assert.True(branch.TryGetNode(new CommittedActionNodeId("timeline.dodge.directional"), out CommittedActionNodeDefinition directional));
                Assert.True(branch.TryGetNode(new CommittedActionNodeId("timeline.dodge.backstep"), out CommittedActionNodeDefinition backstep));
                Assert.That(directional.TimelineNode.Timeline.Tracks.Count, Is.GreaterThan(backstep.TimelineNode.Timeline.Tracks.Count));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchAdapterEditsTopologyAndCompilesAfterSave()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);

                Assert.True(adapter.AddNode(CommittedActionNodeKind.Selector, "selector.editor", out string addDiagnostic), addDiagnostic);
                Assert.True(adapter.AppendChild("selector.editor", "timeline.dodge.directional", out string appendADiagnostic), appendADiagnostic);
                Assert.True(adapter.AppendChild("selector.editor", "timeline.dodge.backstep", out string appendBDiagnostic), appendBDiagnostic);
                Assert.True(adapter.ReorderChild("selector.editor", 1, 0, out string reorderDiagnostic), reorderDiagnostic);
                Assert.True(adapter.RemoveChild("selector.editor", "timeline.dodge.directional", out string removeDiagnostic), removeDiagnostic);
                Assert.True(adapter.RenameNode("selector.editor", "selector.editor.renamed", out string renameDiagnostic), renameDiagnostic);
                Assert.True(adapter.RemoveChild("branch.root.action.dodge", "selector.dodge", out string removeRootChildDiagnostic), removeRootChildDiagnostic);
                Assert.True(adapter.AppendChild("branch.root.action.dodge", "selector.editor.renamed", out string appendRootChildDiagnostic), appendRootChildDiagnostic);
                Assert.True(adapter.SetNodePosition("selector.editor.renamed", new Vector2(44f, 55f), out string positionDiagnostic), positionDiagnostic);
                Assert.True(adapter.Save(out CharacterActionCatalogValidationResult validation), validation.DescribeErrors());

                CommittedActionBranchSerializedAdapter reloaded = new CommittedActionBranchSerializedAdapter(clone);
                CommittedActionBranchEditorSnapshot snapshot = reloaded.Capture();
                ActionTimelineCompileContext compileContext = CompileContext();
                CharacterActionDefinition definition = clone.ToDefinition(in compileContext);

                Assert.AreEqual("branch.root.action.dodge", snapshot.RootNodeId);
                Assert.True(reloaded.TryGetNodeProperty("branch.root.action.dodge", out SerializedProperty root, out string rootDiagnostic), rootDiagnostic);
                Assert.AreEqual(1, root.FindPropertyRelative("childNodeIds").arraySize);
                Assert.AreEqual("selector.editor.renamed", root.FindPropertyRelative("childNodeIds").GetArrayElementAtIndex(0).stringValue);
                Assert.True(reloaded.TryGetNodeProperty("selector.editor.renamed", out SerializedProperty renamed, out string renamedDiagnostic), renamedDiagnostic);
                Assert.AreEqual(44f, renamed.FindPropertyRelative("editorPosition").vector2Value.x, 0.0001f);
                Assert.AreEqual(55f, renamed.FindPropertyRelative("editorPosition").vector2Value.y, 0.0001f);
                Assert.AreEqual(1, renamed.FindPropertyRelative("childNodeIds").arraySize);
                Assert.AreEqual("timeline.dodge.backstep", renamed.FindPropertyRelative("childNodeIds").GetArrayElementAtIndex(0).stringValue);
                Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
                Assert.AreEqual("branch.root.action.dodge", branch.RootNode.NodeId.Value);
                Assert.AreEqual("selector.editor.renamed", branch.RootNode.ChildIds[0].Value);
                Assert.True(branch.TryGetNode(new CommittedActionNodeId("selector.editor.renamed"), out CommittedActionNodeDefinition selector));
                Assert.AreEqual("timeline.dodge.backstep", selector.ChildIds[0].Value);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchAdapterWritesConditionPayloads()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);

                Assert.True(adapter.SetConditionKind("condition.dodge.directional", CommittedActionConditionKind.RequestHeld, out string kindDiagnostic), kindDiagnostic);
                Assert.True(adapter.SetConditionRequestKind("condition.dodge.directional", InputRequestKind.Dodge, out string requestDiagnostic), requestDiagnostic);
                Assert.True(adapter.SetConditionKind("condition.dodge.backstep", CommittedActionConditionKind.RequiredFactActive, out string factKindDiagnostic), factKindDiagnostic);
                Assert.True(adapter.SetConditionRequiredFactId("condition.dodge.backstep", TimelineFactIds.CancelableToDodge.Value, out string factDiagnostic), factDiagnostic);
                Assert.True(adapter.SetConditionExpectedVariant("condition.dodge.backstep", CharacterStateVariant.Backstep, out string variantDiagnostic), variantDiagnostic);

                CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
                CommittedActionBranchNodeEditorSnapshot directional =
                    snapshot.Nodes.Single(node => node.NodeId == "condition.dodge.directional");
                CommittedActionBranchNodeEditorSnapshot backstep =
                    snapshot.Nodes.Single(node => node.NodeId == "condition.dodge.backstep");

                Assert.AreEqual(CommittedActionConditionKind.RequestHeld, directional.ConditionKind);
                Assert.AreEqual(InputRequestKind.Dodge, directional.RequestKind);
                Assert.AreEqual(CommittedActionConditionKind.RequiredFactActive, backstep.ConditionKind);
                Assert.AreEqual(TimelineFactIds.CancelableToDodge.Value, backstep.RequiredFactId);
                Assert.AreEqual(CharacterStateVariant.Backstep, backstep.ExpectedVariant);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphViewPopulatesFromAdapterSnapshot()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);
                CommittedActionBranchEditorSnapshot snapshot = adapter.Capture();
                int expectedEdges = snapshot.Nodes.Sum(node => node.ChildNodeIds.Count);
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                CommittedActionBranchRefPortedGraphAdapter graphAdapter = new CommittedActionBranchRefPortedGraphAdapter(adapter);

                graphView.Populate(graphAdapter, "timeline.dodge.directional");

                Assert.AreEqual(snapshot.Nodes.Count, graphView.NodeViewCount);
                Assert.AreEqual(expectedEdges, graphView.EdgeViewCount);
                Assert.AreEqual("timeline.dodge.directional", graphView.SelectedNodeId);
                Assert.True(graphView.TryGetNodeView("branch.root.action.dodge", out CharacterBehaviorRefPortedNodeView root));
                Assert.AreEqual("Branch Root", root.TitleText);
                Assert.False(root.CanDelete);
                Assert.True(graphView.TryGetNodeView("selector.dodge", out CharacterBehaviorRefPortedNodeView selector));
                Assert.AreEqual("Selector", selector.TitleText);
                Assert.True(selector.CanDelete);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphViewWritesEdgesAndPositionsThroughAdapter()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                graphView.Populate(new CommittedActionBranchRefPortedGraphAdapter(adapter), "selector.dodge");

                Assert.True(graphView.DisconnectNodes("selector.dodge", "condition.dodge.directional"));
                Assert.True(adapter.TryGetNodeProperty("selector.dodge", out SerializedProperty selector, out string selectorDiagnostic), selectorDiagnostic);
                Assert.That(Children(selector), Does.Not.Contain("condition.dodge.directional"));

                Assert.True(graphView.ConnectNodes("selector.dodge", "condition.dodge.directional"));
                Assert.True(graphView.MoveNode("condition.dodge.directional", new Vector2(111f, 222f)));

                Assert.True(adapter.TryGetNodeProperty("selector.dodge", out selector, out selectorDiagnostic), selectorDiagnostic);
                Assert.That(Children(selector), Does.Contain("condition.dodge.directional"));
                Assert.True(adapter.TryGetNodeProperty("condition.dodge.directional", out SerializedProperty moved, out string movedDiagnostic), movedDiagnostic);
                Assert.AreEqual(111f, moved.FindPropertyRelative("editorPosition").vector2Value.x, 0.0001f);
                Assert.AreEqual(222f, moved.FindPropertyRelative("editorPosition").vector2Value.y, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphViewRectangleSelectionUsesNodeContentCoordinates()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                graphView.Populate(new CommittedActionBranchRefPortedGraphAdapter(adapter), string.Empty);

                int selected = graphView.SelectNodesInContentRect(new Rect(960f, -150f, 160f, 100f));

                Assert.AreEqual(1, selected);
                Assert.AreEqual("timeline.dodge.directional", graphView.SelectedNodeId);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphViewAddsAndDeletesNodesThroughAdapter()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                graphView.Populate(new CommittedActionBranchRefPortedGraphAdapter(adapter), string.Empty);
                int nodeCount = graphView.NodeViewCount;

                Assert.True(graphView.AddNode("Condition", new Vector2(300f, 400f)));
                string addedNodeId = graphView.SelectedNodeId;

                Assert.That(addedNodeId, Does.StartWith("condition."));
                Assert.AreEqual(nodeCount + 1, graphView.NodeViewCount);
                Assert.True(adapter.TryGetNodeProperty(addedNodeId, out SerializedProperty added, out string addedDiagnostic), addedDiagnostic);
                Assert.AreEqual(300f, added.FindPropertyRelative("editorPosition").vector2Value.x, 0.0001f);
                Assert.AreEqual(400f, added.FindPropertyRelative("editorPosition").vector2Value.y, 0.0001f);

                Assert.True(adapter.AppendChild("selector.dodge", addedNodeId, out string appendDiagnostic), appendDiagnostic);
                Assert.True(graphView.DeleteNode(addedNodeId));

                Assert.False(adapter.TryGetNodeProperty(addedNodeId, out _, out _));
                Assert.True(adapter.TryGetNodeProperty("selector.dodge", out SerializedProperty selector, out string selectorDiagnostic), selectorDiagnostic);
                Assert.That(Children(selector), Does.Not.Contain(addedNodeId));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphViewRejectsProtectedRootDeletion()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                graphView.Populate(new CommittedActionBranchRefPortedGraphAdapter(adapter), "branch.root.action.dodge");

                Assert.False(graphView.DeleteNode("branch.root.action.dodge"));
                Assert.True(adapter.TryGetNodeProperty("branch.root.action.dodge", out _, out _));
                Assert.True(graphView.TryGetNodeView("branch.root.action.dodge", out CharacterBehaviorRefPortedNodeView root));
                Assert.False(root.CanDelete);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphCreateOptionsDoNotExposeRoot()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchRefPortedGraphAdapter adapter =
                    new CommittedActionBranchRefPortedGraphAdapter(new CommittedActionBranchSerializedAdapter(clone));

                Assert.That(adapter.CreateOptions.Select(option => option.Id), Is.EquivalentTo(new[] { "Selector", "Condition", "Timeline" }));
                Assert.False(adapter.CreateOptions.Any(option => option.Id.Contains("Root") || option.Path.Contains("Root")));
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphSearchWindowListsOnlyFormalCreateOptions()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            CharacterBehaviorRefPortedSearchWindow searchWindow =
                ScriptableObject.CreateInstance<CharacterBehaviorRefPortedSearchWindow>();
            try
            {
                CommittedActionBranchRefPortedGraphAdapter adapter =
                    new CommittedActionBranchRefPortedGraphAdapter(new CommittedActionBranchSerializedAdapter(clone));
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                searchWindow.Init(graphView, adapter);

                string[] labels = searchWindow
                    .CreateSearchTree(new SearchWindowContext(Vector2.zero))
                    .Select(entry => entry.content.text)
                    .ToArray();

                CollectionAssert.Contains(labels, "Committed Action");
                CollectionAssert.Contains(labels, "Selector");
                CollectionAssert.Contains(labels, "Condition");
                CollectionAssert.Contains(labels, "Timeline");
                CollectionAssert.DoesNotContain(labels, "Root");
            }
            finally
            {
                Object.DestroyImmediate(searchWindow);
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphViewSelectionKeepsTimelineAdapterOnSelectedNode()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                SerializedObject serialized = new SerializedObject(clone);
                CommittedActionBranchSerializedAdapter branchAdapter =
                    new CommittedActionBranchSerializedAdapter(clone, serialized);
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                graphView.Populate(new CommittedActionBranchRefPortedGraphAdapter(branchAdapter), "timeline.dodge.backstep");

                CommittedActionTimelineSerializedAdapter timelineAdapter =
                    new CommittedActionTimelineSerializedAdapter(clone, serialized, graphView.SelectedNodeId);

                Assert.True(timelineAdapter.TryGetTimelineProperty(
                    CommittedActionTimelineVariant.Backstep,
                    out SerializedProperty timeline,
                    out string diagnostic), diagnostic);
                Assert.AreEqual("timeline.dodge.backstep", timeline.FindPropertyRelative("timelineNodeId").stringValue);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void BranchGraphViewChangesCompileThroughFormalDefinition()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(clone);
                CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
                graphView.Populate(new CommittedActionBranchRefPortedGraphAdapter(adapter), "selector.dodge");

                Assert.True(graphView.DisconnectNodes("selector.dodge", "condition.dodge.backstep"));
                Assert.True(adapter.Save(out CharacterActionCatalogValidationResult validation), validation.DescribeErrors());

                ActionTimelineCompileContext compileContext = CompileContext();
                CharacterActionDefinition definition = clone.ToDefinition(in compileContext);

                Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
                Assert.AreEqual("branch.root.action.dodge", branch.RootNode.NodeId.Value);
                Assert.AreEqual(1, branch.RootNode.ChildIds.Count);
                Assert.AreEqual("selector.dodge", branch.RootNode.ChildIds[0].Value);
                Assert.True(branch.TryGetNode(new CommittedActionNodeId("selector.dodge"), out CommittedActionNodeDefinition selector));
                Assert.AreEqual(1, selector.ChildIds.Count);
                Assert.AreEqual("condition.dodge.directional", selector.ChildIds[0].Value);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }

        [Test]
        public void MatrixAdapterWritesBackFormalPolicyRows()
        {
            ActionInterruptPolicySetSO asset = ScriptableObject.CreateInstance<ActionInterruptPolicySetSO>();
            try
            {
                ActionTransitionPolicyMatrixSerializedAdapter adapter =
                    new ActionTransitionPolicyMatrixSerializedAdapter(asset);

                Assert.True(adapter.AddRow(MatrixRow("Action.Block", "Action.GuardCounter", ActionRequestType.Attack, "window.counter.open", 40), out string addADiagnostic), addADiagnostic);
                Assert.True(adapter.AddRow(MatrixRow("Action.Attack01", "Action.Dodge", ActionRequestType.Dodge, TimelineFactIds.CancelableToDodge.Value, 30), out string addBDiagnostic), addBDiagnostic);
                Assert.True(adapter.SetFromActionId(0, "Action.BlockEdited", out string fromDiagnostic), fromDiagnostic);
                Assert.True(adapter.SetToActionId(0, "Action.CounterEdited", out string toDiagnostic), toDiagnostic);
                Assert.True(adapter.SetRequestType(0, ActionRequestType.Custom, out string requestDiagnostic), requestDiagnostic);
                Assert.True(adapter.SetRequiredFactId(0, "window.counter.edited", out string factDiagnostic), factDiagnostic);
                Assert.True(adapter.SetMinPriority(0, 70, out string priorityDiagnostic), priorityDiagnostic);
                Assert.True(adapter.SetForce(0, true, out string forceDiagnostic), forceDiagnostic);
                Assert.True(adapter.SetResistanceRule(0, ActionTransitionResistanceRule.UseCurrentState, out string resistanceDiagnostic), resistanceDiagnostic);
                Assert.True(adapter.MoveRow(1, 0, out string moveDiagnostic), moveDiagnostic);
                Assert.True(adapter.RemoveRow(1, out string removeDiagnostic), removeDiagnostic);
                Assert.True(adapter.Save(FactContext(TimelineFactIds.CancelableToDodge.Value, "window.counter.edited"), out ActionInterruptPolicyValidationResult validation), validation.DescribeErrors());

                ActionTransitionPolicyMatrixEditorSnapshot snapshot = adapter.Capture();
                var policies = asset.CompilePolicies();

                Assert.AreEqual(1, snapshot.Rows.Count);
                Assert.AreEqual("Action.Attack01", snapshot.Rows[0].Row.FromActionId);
                Assert.AreEqual("Action.Dodge", snapshot.Rows[0].Row.ToActionId);
                Assert.AreEqual(ActionRequestType.Dodge, snapshot.Rows[0].Row.RequestType);
                Assert.AreEqual(TimelineFactIds.CancelableToDodge.Value, snapshot.Rows[0].Row.RequiredFactId);
                Assert.AreEqual(DodgeActionId, policies[0].TargetState);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void BranchEditorSourceKeepsToolBoundaries()
        {
            string branchEditorSource = ReadSource("Editor/Character/Action/Branch");
            string graphEditorSource = ReadSource("Editor/Character/Graph");
            string timelineEditorSource = ReadSource("Editor/Character/Action/Timeline");
            string editorSource = branchEditorSource + graphEditorSource + timelineEditorSource;

            Assert.That(branchEditorSource, Does.Contain("CommittedActionBranchRefPortedGraphAdapter"));
            Assert.That(branchEditorSource, Does.Not.Contain("class CommittedActionBranchEditorWindow : EditorWindow"));
            Assert.That(branchEditorSource, Does.Not.Contain("Tools/3C/Committed Action Branch Editor"));
            Assert.That(branchEditorSource, Does.Not.Contain("OpenCommittedActionBranch"));
            Assert.That(branchEditorSource, Does.Not.Contain("CommittedActionBranchGraphView"));
            Assert.That(branchEditorSource, Does.Not.Contain("CommittedActionRefPortedTimelineView"));
            Assert.That(branchEditorSource, Does.Not.Contain("ScrollView branchGraph"));
            Assert.That(branchEditorSource, Does.Not.Contain("ScrollView nodeList"));
            Assert.That(branchEditorSource, Does.Not.Contain("Box card"));
            Assert.That(graphEditorSource, Does.Contain("Tools/3C/Character Behavior Editor"));
            Assert.That(graphEditorSource, Does.Contain("Initialize Branch"));
            Assert.That(graphEditorSource, Does.Contain("Initialize Dodge Template"));
            Assert.That(graphEditorSource, Does.Contain("DrawBranchInspector"));
            Assert.That(graphEditorSource, Does.Contain("Open Independent Timeline Editor"));
            Assert.That(graphEditorSource, Does.Not.Contain("Timeline Panel"));
            Assert.That(graphEditorSource, Does.Not.Contain("CommittedActionBranchEditorWindow"));
            Assert.That(editorSource, Does.Not.Contain("project://database/Assets/Addon/Taco"));
            Assert.That(editorSource, Does.Not.Contain("RunnableTree"));
            Assert.That(editorSource, Does.Not.Contain("RunnableNode"));
            Assert.That(editorSource, Does.Not.Contain("TreeRunner"));
            Assert.That(editorSource, Does.Not.Contain("TimelinePlayer"));
            Assert.That(graphEditorSource, Does.Contain("source topology"));
            Assert.That(graphEditorSource, Does.Contain("Committed Branch"));
            Assert.That(graphEditorSource, Does.Contain("CommittedActionBranchRefPortedGraphAdapter"));
            Assert.That(timelineEditorSource, Does.Contain("selectedTimelineNodeId"));
            Assert.That(editorSource, Does.Not.Contain("Skill" + " Editor"));
        }

        [Test]
        public void RuntimeSourceDoesNotReferenceBranchEditorViewTypes()
        {
            string runtimeSource = ReadSource("Scripts/Character");

            Assert.That(runtimeSource, Does.Not.Contain("UnityEditor"));
            Assert.That(runtimeSource, Does.Not.Contain("GraphView"));
            Assert.That(runtimeSource, Does.Not.Contain("CommittedActionBranchGraphView"));
            Assert.That(runtimeSource, Does.Not.Contain("CommittedActionBranchEditorWindow"));
            Assert.That(runtimeSource, Does.Not.Contain("CommittedActionBranchNodeView"));
            Assert.That(runtimeSource, Does.Not.Contain("project://database/Assets/Addon/Taco"));
        }

        static CharacterActionDefinitionSO CloneDodgeAction()
        {
            CharacterActionDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(DodgeAssetPath);
            Assert.NotNull(asset, DodgeAssetPath);
            CharacterActionDefinitionSO clone = Object.Instantiate(asset);
            clone.name = "DodgeBranchEditorAdapterTest";
            return clone;
        }

        static CharacterActionDefinitionSO CloneDodgeActionWithEmptyBranch()
        {
            CharacterActionDefinitionSO clone = CloneDodgeAction();
            SerializedObject serialized = new SerializedObject(clone);
            SerializedProperty branch = serialized.FindProperty("committedActionBranch");
            branch.FindPropertyRelative("schemaVersion").intValue = 0;
            branch.FindPropertyRelative("required").boolValue = true;
            branch.FindPropertyRelative("branchId").stringValue = string.Empty;
            branch.FindPropertyRelative("rootNodeId").stringValue = string.Empty;
            branch.FindPropertyRelative("defaultBodyKind").enumValueIndex = 0;
            branch.FindPropertyRelative("defaultChannels").intValue = 0;
            branch.FindPropertyRelative("nodes").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return clone;
        }

        static int TrackCount(
            CommittedActionTimelineSerializedAdapter adapter,
            CommittedActionTimelineVariant variant)
        {
            Assert.True(adapter.TryGetTimelineProperty(variant, out SerializedProperty timeline, out string diagnostic), diagnostic);
            return timeline.FindPropertyRelative("tracks").arraySize;
        }

        static string[] Children(SerializedProperty node)
        {
            SerializedProperty children = node.FindPropertyRelative("childNodeIds");
            string[] result = new string[children.arraySize];
            for (int i = 0; i < children.arraySize; i++)
                result[i] = children.GetArrayElementAtIndex(i).stringValue;
            return result;
        }

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
        }

        static ActionStateId DodgeActionId => new ActionStateId(ActionStateIds.Dodge.Value);

        static ActionTransitionPolicyRowDefinition MatrixRow(
            string from,
            string to,
            ActionRequestType requestType,
            string requiredFactId,
            int minPriority)
        {
            return new ActionTransitionPolicyRowDefinition(
                from,
                to,
                requestType,
                requiredFactId,
                minPriority);
        }

        static ActionFactCompileContext FactContext(params string[] factIds)
        {
            ActionFactDeclaration[] declarations = new ActionFactDeclaration[factIds.Length];
            for (int i = 0; i < factIds.Length; i++)
            {
                declarations[i] = new ActionFactDeclaration(
                    new TimelineFactId(factIds[i]),
                    ActionFactSourceKind.TimelineWindow,
                    true);
            }

            return new ActionFactCompileContext(declarations);
        }

        static string ReadSource(string relativeFromAssets)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, relativeFromAssets));
            return string.Join(
                "\n",
                Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Select(path => File.ReadAllText(path, Encoding.UTF8)));
        }
    }
}
