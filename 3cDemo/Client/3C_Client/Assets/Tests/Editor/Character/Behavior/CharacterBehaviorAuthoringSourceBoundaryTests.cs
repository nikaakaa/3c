using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior;
using ThirdPersonCharacterBehavior.Authoring;
using ThirdPersonCharacterBehavior.Editor.ActionBranch;
using ThirdPersonCharacterBehavior.Editor.Graph;
using ThirdPersonCharacterConfig;
using UnityEditor;
using UnityEngine;

namespace Tests.Editor.Character.Behavior
{
    public sealed class CharacterBehaviorAuthoringSourceBoundaryTests
    {
        const string FormalDodgeActionPath = "Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset";
        const string FormalBehaviorAuthoringPath = "Assets/Configs/3C/Behavior/DefaultCharacterBehaviorAuthoring.asset";
        const string FormalBehaviorRuntimePath = "Assets/Configs/3C/Behavior/DefaultCharacterBehaviorRuntimeDefinition.asset";

        [Test]
        public void CompilerOutputsSourceRuntimeDefinitionOnly()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();

            CharacterBehaviorAuthoringCompilerResult result = CharacterBehaviorAuthoringCompiler.Compile(asset);

            Assert.True(result.Success, string.Join("\n", result.Errors));
            Assert.True(result.BehaviorTree.IsDefined);
            Assert.True(result.RuntimeDefinition.HasRequiredProductionOrder);
            Assert.AreEqual(CharacterBehaviorSourceKind.Locomotion, result.RuntimeDefinition.GetLeafAt(0));
            Assert.AreEqual(CharacterBehaviorSourceKind.CommittedAction, result.RuntimeDefinition.GetLeafAt(1));
            Assert.IsNull(typeof(CharacterBehaviorAuthoringCompilerResult).GetProperty("DodgeCommittedActionBranch"));
        }

        [Test]
        public void DefaultBehaviorSourceAuthoringCompilesToFormalRuntimeDefinition()
        {
            CharacterBehaviorAuthoringAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterBehaviorAuthoringAsset>(FormalBehaviorAuthoringPath);
            CharacterBehaviorRuntimeDefinitionSO runtimeAsset =
                AssetDatabase.LoadAssetAtPath<CharacterBehaviorRuntimeDefinitionSO>(FormalBehaviorRuntimePath);

            Assert.NotNull(asset, FormalBehaviorAuthoringPath);
            Assert.NotNull(runtimeAsset, FormalBehaviorRuntimePath);

            CharacterBehaviorAuthoringCompilerResult compile = CharacterBehaviorAuthoringCompiler.Compile(asset);
            CharacterBehaviorRuntimeDefinition runtimeDefinition = runtimeAsset.ToDefinition();

            Assert.True(compile.Success, string.Join("\n", compile.Errors));
            Assert.True(compile.RuntimeDefinition.HasRequiredProductionOrder);
            Assert.AreEqual(compile.RuntimeDefinition.RootId.Value, runtimeDefinition.RootId.Value);
            Assert.AreEqual(compile.RuntimeDefinition.LeafCount, runtimeDefinition.LeafCount);
            for (int i = 0; i < compile.RuntimeDefinition.LeafCount; i++)
                Assert.AreEqual(compile.RuntimeDefinition.GetLeafAt(i), runtimeDefinition.GetLeafAt(i));
        }

        [Test]
        public void CorinCharacterConfigUsesFormalCompiledBehaviorRuntimeDefinition()
        {
            CharacterBehaviorAuthoringAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterBehaviorAuthoringAsset>(FormalBehaviorAuthoringPath);
            CharacterBehaviorRuntimeDefinitionSO runtimeAsset =
                AssetDatabase.LoadAssetAtPath<CharacterBehaviorRuntimeDefinitionSO>(FormalBehaviorRuntimePath);
            CharacterConfigSO config =
                AssetDatabase.LoadAssetAtPath<CharacterConfigSO>("Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset");

            Assert.NotNull(asset, FormalBehaviorAuthoringPath);
            Assert.NotNull(runtimeAsset, FormalBehaviorRuntimePath);
            Assert.NotNull(config);
            Assert.AreSame(runtimeAsset, config.BehaviorRuntimeDefinition);

            CharacterBehaviorAuthoringCompilerResult compile = CharacterBehaviorAuthoringCompiler.Compile(asset);
            CharacterBehaviorRuntimeDefinition runtimeDefinition = config.BehaviorRuntimeDefinition.ToDefinition();

            Assert.True(compile.Success, string.Join("\n", compile.Errors));
            Assert.AreEqual(compile.RuntimeDefinition.RootId.Value, runtimeDefinition.RootId.Value);
            Assert.AreEqual(compile.RuntimeDefinition.LeafCount, runtimeDefinition.LeafCount);
            for (int i = 0; i < compile.RuntimeDefinition.LeafCount; i++)
                Assert.AreEqual(compile.RuntimeDefinition.GetLeafAt(i), runtimeDefinition.GetLeafAt(i));
        }

        [Test]
        public void DefaultBehaviorSourceAuthoringUsesFixedRootFanOutTopology()
        {
            CharacterBehaviorAuthoringAsset asset =
                AssetDatabase.LoadAssetAtPath<CharacterBehaviorAuthoringAsset>(FormalBehaviorAuthoringPath);

            Assert.NotNull(asset, FormalBehaviorAuthoringPath);
            Assert.AreEqual(4, asset.Nodes.Count);
            Assert.AreEqual(3, asset.Edges.Count);
            Assert.True(asset.Nodes.Any(node => node.StableId == "behavior.root" &&
                                                node.Kind == CharacterBehaviorAuthoringNodeKind.Root &&
                                                node.EditorPosition.x < asset.Nodes.Single(candidate => candidate.StableId == "behavior.parallel").EditorPosition.x));
            Assert.True(asset.Edges.Any(edge => edge.ParentNodeId == "behavior.root" &&
                                                edge.ChildNodeId == "behavior.parallel"));
            Assert.True(asset.Edges.Any(edge => edge.ParentNodeId == "behavior.parallel" &&
                                                edge.ChildNodeId == "source.locomotion"));
            Assert.True(asset.Edges.Any(edge => edge.ParentNodeId == "behavior.parallel" &&
                                                edge.ChildNodeId == "source.committed-action"));
        }

        [Test]
        public void RuntimeDefinitionAssetAcceptsBehaviorCompilerOutput()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorAuthoringCompilerResult compile = CharacterBehaviorAuthoringCompiler.Compile(asset);
            CharacterBehaviorRuntimeDefinitionSO runtimeAsset =
                ScriptableObject.CreateInstance<CharacterBehaviorRuntimeDefinitionSO>();
            try
            {
                runtimeAsset.SetDefinition(compile.RuntimeDefinition);

                CharacterBehaviorRuntimeDefinition definition = runtimeAsset.ToDefinition();
                Assert.AreEqual("behavior.root", definition.RootId.Value);
                Assert.True(definition.HasRequiredProductionOrder);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(runtimeAsset);
            }
        }

        [Test]
        public void CompilerResultDoesNotExposeActionTimelinePayload()
        {
            Type resultType = typeof(CharacterBehaviorAuthoringCompilerResult);
            PropertyInfo[] properties = resultType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            for (int i = 0; i < properties.Length; i++)
            {
                Assert.AreNotEqual(typeof(ActionTimelineDefinition), properties[i].PropertyType);
                Assert.AreNotEqual(typeof(CommittedActionBranchDefinition), properties[i].PropertyType);
            }

            Assert.IsNull(resultType.GetProperty("ActionTimeline"));
            Assert.IsNull(resultType.GetProperty("DirectionalTimeline"));
            Assert.IsNull(resultType.GetProperty("BackstepTimeline"));
        }

        [Test]
        public void GraphViewWriteOnlyPersistsSourceTopology()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();

            graphView.Populate(asset);
            graphView.WriteTo(asset);

            Assert.AreEqual(4, asset.Nodes.Count);
            Assert.AreEqual(3, asset.Edges.Count);
        }

        [Test]
        public void GraphViewEditsSourceTopologyWithoutRootCreateOrDelete()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();

            graphView.Populate(asset);

            Assert.True(graphView.TryGetNodeView("behavior.root", out CharacterBehaviorRefPortedNodeView root));
            Assert.AreEqual("Behavior Root", root.TitleText);
            Assert.False(root.CanDelete);
            Assert.False(graphView.DeleteNode("behavior.root"));
            Assert.True(graphView.AddNode("Parallel", new Vector2(700f, 0f)));
            Assert.That(graphView.SelectedNodeId, Does.StartWith("behavior.parallel."));
            graphView.WriteTo(asset);

            Assert.True(asset.Nodes.Any(node => node.StableId == "behavior.root"));
            Assert.True(asset.Nodes.Any(node => node.StableId == graphView.SelectedNodeId));
        }

        [Test]
        public void GraphViewDoubleClickOpenCallbackUsesStableNodeId()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
            string openedNodeId = string.Empty;

            graphView.NodeOpened += nodeId => openedNodeId = nodeId;
            graphView.Populate(asset);
            Assert.True(graphView.TryGetNodeView("source.committed-action", out CharacterBehaviorRefPortedNodeView node));

            node.HandleClickForTests(2);

            Assert.AreEqual("source.committed-action", openedNodeId);
            Assert.AreEqual("source.committed-action", graphView.SelectedNodeId);
        }

        [Test]
        public void GraphViewSingleClickSelectsWithoutOpenCallback()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
            string selectedNodeId = string.Empty;
            string openedNodeId = string.Empty;

            graphView.NodeSelected += nodeId => selectedNodeId = nodeId;
            graphView.NodeOpened += nodeId => openedNodeId = nodeId;
            graphView.Populate(asset);
            Assert.True(graphView.TryGetNodeView("source.committed-action", out CharacterBehaviorRefPortedNodeView node));

            node.HandleClickForTests(1);

            Assert.AreEqual("source.committed-action", selectedNodeId);
            Assert.AreEqual("source.committed-action", graphView.SelectedNodeId);
            Assert.AreEqual(string.Empty, openedNodeId);
        }

        [Test]
        public void GraphViewNodeOpenCallbackIgnoresUnknownNodeId()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();
            string openedNodeId = string.Empty;

            graphView.NodeOpened += nodeId => openedNodeId = nodeId;
            graphView.Populate(asset);
            graphView.OpenNodeForTests("source.missing");

            Assert.AreEqual(string.Empty, openedNodeId);
        }

        [Test]
        public void GraphViewWriteDoesNotModifyFormalActionDefinitionTimeline()
        {
            CharacterActionDefinitionSO actionDefinition =
                AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(FormalDodgeActionPath);
            Assert.NotNull(actionDefinition, FormalDodgeActionPath);
            string before = EditorJsonUtility.ToJson(actionDefinition);
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorRefPortedGraphView graphView = new CharacterBehaviorRefPortedGraphView();

            graphView.Populate(asset);
            graphView.WriteTo(asset);

            string after = EditorJsonUtility.ToJson(actionDefinition);
            Assert.AreEqual(before, after);
        }

        [Test]
        public void BranchSaveDoesNotModifyBehaviorSourceTopology()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            string before = EditorJsonUtility.ToJson(asset);
            CharacterActionDefinitionSO actionDefinition =
                UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(FormalDodgeActionPath));

            try
            {
                CommittedActionBranchSerializedAdapter adapter = new CommittedActionBranchSerializedAdapter(actionDefinition);

                Assert.True(adapter.Save(out CharacterActionCatalogValidationResult validation), validation.DescribeErrors());

                string after = EditorJsonUtility.ToJson(asset);
                Assert.AreEqual(before, after);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actionDefinition);
            }
        }

        [Test]
        public void AuthoringAssetPublicSchemaDoesNotExposeDodgeTimelineAsSourceData()
        {
            Type type = typeof(CharacterBehaviorAuthoringAsset);

            Assert.NotNull(type.GetProperty("Nodes"));
            Assert.NotNull(type.GetProperty("Edges"));
            Assert.IsNull(type.GetProperty("TimelineClipIds"));
            Assert.IsNull(type.GetProperty("DodgeCommittedActionBranch"));
            Assert.IsNull(type.GetProperty("LegacyTimelineClipIdCount"));
            Assert.IsNull(type.GetProperty("HasLegacyTimelineClipIds"));
            Assert.IsNull(type.GetProperty("HasLegacyDodgeCommittedActionBranch"));
            Assert.IsNull(type.GetMethod("SetTimelineClipStableIds"));
            Assert.IsNull(type.GetMethod("SetDodgeCommittedActionBranch"));
            Assert.IsNull(type.Assembly.GetType("ThirdPersonCharacterBehavior.Authoring.CharacterBehaviorTimelineClipStableId"));
        }

        [Test]
        public void CommittedActionSourceWithoutActionCatalogReportsFormalConfigurationError()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterBehaviorAuthoringCompilerResult compile = CharacterBehaviorAuthoringCompiler.Compile(asset);
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();

            CharacterActionCatalogValidationResult validation = config.ValidateActionCatalog();

            Assert.True(compile.Success, string.Join("\n", compile.Errors));
            Assert.AreEqual(CharacterBehaviorSourceKind.CommittedAction, compile.RuntimeDefinition.GetLeafAt(1));
            Assert.True(validation.HasErrors);
            CollectionAssert.Contains(validation.Errors, "Character config action catalog is missing.");
        }

        [Test]
        public void GraphAuthoringBoundaryDoesNotNameFullBodyAsSourceSlotOrRoot()
        {
            string source =
                ReadFile("Scripts/Character/Behavior/Authoring/CharacterBehaviorAuthoringAsset.cs") +
                ReadFile("Scripts/Character/Behavior/Authoring/CharacterBehaviorAuthoringCompiler.cs") +
                ReadFile("Editor/Character/Graph/CharacterBehaviorEditorWindow.cs") +
                ReadFile("Editor/Character/Graph/CharacterBehaviorRefPortedGraphView.cs");
            string forbiddenName = "Skill" + " Editor";

            Assert.That(source, Does.Contain("Root"));
            Assert.That(source, Does.Contain("LocomotionLeaf"));
            Assert.That(source, Does.Contain("CommittedActionLeaf"));
            Assert.That(source, Does.Contain("Open Committed Action Timeline"));
            Assert.That(source, Does.Contain("DefaultCharacterBehaviorAuthoring.asset"));
            Assert.That(source, Does.Contain("DefaultCharacterBehaviorRuntimeDefinition.asset"));
            Assert.That(source, Does.Contain("CharacterBehaviorAuthoringCompiler.Compile"));
            Assert.That(source, Does.Contain("SetDefinition"));
            Assert.That(source, Does.Not.Contain("FullBody"));
            Assert.That(source, Does.Not.Contain(forbiddenName));
        }

        static CharacterBehaviorAuthoringAsset CreateSourceGraphAsset()
        {
            CharacterBehaviorAuthoringAsset asset = ScriptableObject.CreateInstance<CharacterBehaviorAuthoringAsset>();
            asset.SetStableAssetId("behavior.test");
            asset.SetGraph(
                new[]
                {
                    new CharacterBehaviorAuthoringNode("behavior.root", CharacterBehaviorAuthoringNodeKind.Root, new Vector2(0, 0)),
                    new CharacterBehaviorAuthoringNode("behavior.parallel", CharacterBehaviorAuthoringNodeKind.Parallel, new Vector2(320, 0)),
                    new CharacterBehaviorAuthoringNode("source.locomotion", CharacterBehaviorAuthoringNodeKind.LocomotionLeaf, new Vector2(660, -120)),
                    new CharacterBehaviorAuthoringNode("source.committed-action", CharacterBehaviorAuthoringNodeKind.CommittedActionLeaf, new Vector2(660, 120))
                },
                new[]
                {
                    new CharacterBehaviorAuthoringEdge("behavior.root", "behavior.parallel", CharacterBehaviorAuthoringPortIds.Children, CharacterBehaviorAuthoringPortIds.Input),
                    new CharacterBehaviorAuthoringEdge("behavior.parallel", "source.locomotion", CharacterBehaviorAuthoringPortIds.Children, CharacterBehaviorAuthoringPortIds.Input),
                    new CharacterBehaviorAuthoringEdge("behavior.parallel", "source.committed-action", CharacterBehaviorAuthoringPortIds.Children, CharacterBehaviorAuthoringPortIds.Input)
                });
            return asset;
        }

        static string ReadFile(string relativeFromAssets)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, relativeFromAssets), Encoding.UTF8);
        }
    }
}
