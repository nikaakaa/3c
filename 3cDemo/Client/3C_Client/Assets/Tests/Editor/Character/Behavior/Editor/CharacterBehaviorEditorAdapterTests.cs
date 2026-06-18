using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using ThirdPersonAction;
using ThirdPersonCharacterBehavior.Authoring;
using ThirdPersonCharacterBehavior.Editor.ActionTimeline;
using ThirdPersonCharacterBehavior.Editor.Graph;
using ThirdPersonCharacterConfig;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tests.Editor.Character.Behavior.EditorAdapters
{
    public sealed class CharacterBehaviorEditorAdapterTests
    {
        const string FormalDodgeActionPath = "Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset";
        const string DeprecatedSamplePath = "Assets/Configs/3C/Behavior/Samples/CorinDodgeBehaviorAuthoring.asset";
        const string DeprecatedRuntimeOutputPath = "Assets/Configs/3C/Behavior/Samples/Compiled/CorinDodgeBehaviorRuntimeDefinition.asset";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void FormalDodgeActionDefinitionCompilesToSelectorWithEditableTimelines()
        {
            CharacterActionDefinitionSO asset = LoadFormalDodgeAction();
            ActionTimelineCompileContext compileContext = CompileContext();
            CharacterActionCatalogValidationResult validation = asset.Validate(in compileContext);

            Assert.False(validation.HasErrors, string.Join("\n", validation.Errors));
            CharacterActionDefinition definition = asset.ToDefinition(in compileContext);
            Assert.True(definition.TryGetCommittedActionBranch(out CommittedActionBranchDefinition branch));
            Assert.AreEqual(CommittedActionNodeKind.Root, branch.RootNode.Kind);
            Assert.AreEqual("branch.root.action.dodge", branch.RootNode.NodeId.Value);
            Assert.AreEqual("selector.dodge", branch.RootNode.ChildIds[0].Value);
            Assert.True(branch.DefaultBodyClaim.ClaimsFullBody);
            Assert.True(branch.TryGetNode(new CommittedActionNodeId("timeline.dodge.directional"), out CommittedActionNodeDefinition directional));
            Assert.True(branch.TryGetNode(new CommittedActionNodeId("timeline.dodge.backstep"), out CommittedActionNodeDefinition backstep));
            AssertTimelineHasEditableClipKinds(directional.TimelineNode.Timeline);
            AssertTimelineHasEditableClipKinds(backstep.TimelineNode.Timeline);
        }

        [Test]
        public void TimelineEditorDefaultsToFormalDodgeActionDefinition()
        {
            string source = ReadFile("Editor/Character/Action/Timeline/CommittedActionTimelineEditorWindow.cs");

            Assert.That(source, Does.Contain(FormalDodgeActionPath));
            Assert.That(source, Does.Contain("CharacterActionDefinitionSO"));
            Assert.That(source, Does.Not.Contain("CharacterBehaviorAuthoringAsset"));
            Assert.That(source, Does.Not.Contain("CorinDodgeBehaviorAuthoring"));
            Assert.That(source, Does.Not.Contain("CompileToRuntimeDefinitionAsset"));
        }

        [Test]
        public void TimelineViewClonesRefPortedUxmlAndPreviewsFormalDodgeData()
        {
            CharacterActionDefinitionSO asset = LoadFormalDodgeAction();
            SerializedObject serialized = new SerializedObject(asset);
            CommittedActionRefPortedTimelineView view = new CommittedActionRefPortedTimelineView();

            view.Populate(serialized);
            view.SetPreviewFrame(2);

            Assert.NotNull(view.Q("track-handle-container"));
            Assert.NotNull(view.Q("track-field"));
            Assert.NotNull(view.Q("time-locater"));
            Assert.NotNull(view.Q("clip-inspector"));
            Assert.True(view.MaxPreviewFrame >= 21);
            Label summary = view.Q<Label>("preview-summary");
            Assert.NotNull(summary);
            Assert.That(summary.text, Does.Contain("tick 2"));
            Assert.That(view.Query(className: "previewActive").ToList().Count, Is.GreaterThan(0));
        }

        [Test]
        public void GraphEditorNoLongerOpensDeprecatedSampleOrWritesFakeRuntimeDefinition()
        {
            string source = ReadFile("Editor/Character/Graph/CharacterBehaviorEditorWindow.cs");
            string compileUtilityPath = Path.Combine(Application.dataPath, "Editor/Character/Graph/CharacterBehaviorEditorCompileUtility.cs");

            Assert.That(source, Does.Not.Contain("Open Sample"));
            Assert.That(source, Does.Not.Contain("SamplePath"));
            Assert.That(source, Does.Not.Contain("CompileToRuntimeDefinitionAsset"));
            Assert.That(source, Does.Not.Contain("CorinDodgeBehaviorAuthoring"));
            Assert.False(File.Exists(compileUtilityPath));
        }

        [Test]
        public void DeprecatedSampleAssetsAreNotEditorEntryPoints()
        {
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Object>(DeprecatedSamplePath));
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<Object>(DeprecatedRuntimeOutputPath));
        }

        [Test]
        public void RuntimeBoundaryDoesNotReferenceEditorRefRunnerOrPlayableGraph()
        {
            string runtimeSource = ReadSource("Scripts/Character");

            Assert.That(runtimeSource, Does.Not.Contain("UnityEditor"));
            Assert.That(runtimeSource, Does.Not.Contain("GraphView"));
            Assert.That(runtimeSource, Does.Not.Contain("TreeRunner"));
            Assert.That(runtimeSource, Does.Not.Contain("RunnableTree"));
            Assert.That(runtimeSource, Does.Not.Contain("RunnableNode"));
            Assert.That(runtimeSource, Does.Not.Contain("TimelinePlayer"));
            Assert.That(runtimeSource, Does.Not.Contain("PlayableGraph"));
            Assert.That(runtimeSource, Does.Not.Contain("BaseTree"));
        }

        [Test]
        public void EditorAdaptersUseApprovedNamesAndStayEditorOnly()
        {
            string editorSource = ReadSource("Editor/Character");
            string forbiddenName = "Skill" + " Editor";

            Assert.That(editorSource, Does.Contain("Character Behavior Editor"));
            Assert.That(editorSource, Does.Contain("Committed Action Timeline Editor"));
            Assert.That(editorSource, Does.Not.Contain(forbiddenName));
            Assert.That(typeof(CharacterBehaviorEditorWindow).Assembly.FullName, Does.Contain("Editor"));
        }

        [Test]
        public void EditorAdaptersUseRefPortedGraphAndTimelineViewLayers()
        {
            string editorSource = ReadSource("Editor/Character");

            Assert.That(editorSource, Does.Contain("CharacterBehaviorRefPortedGraphView"));
            Assert.That(editorSource, Does.Contain("CharacterBehaviorRefPortedNodeView"));
            Assert.That(editorSource, Does.Contain("CharacterBehaviorRefPortedSearchWindow"));
            Assert.That(editorSource, Does.Contain("nodeCreationRequest"));
            Assert.That(editorSource, Does.Contain("GraphViewChange"));
            Assert.That(editorSource, Does.Contain("DrawConditionInspector"));
            Assert.That(editorSource, Does.Contain("DrawTimelineInspector"));
            Assert.That(editorSource, Does.Contain("OpenSelectedTimeline"));
            Assert.That(editorSource, Does.Contain("Open Independent Timeline Editor"));
            Assert.That(editorSource, Does.Contain("CommittedActionRefPortedTimelineView"));
            Assert.That(editorSource, Does.Contain("CommittedActionTimelineTrackHandle"));
            Assert.That(editorSource, Does.Contain("CommittedActionTimelineTrackView"));
            Assert.That(editorSource, Does.Contain("CommittedActionTimelineClipView"));
            Assert.That(editorSource, Does.Contain("BaseTree"));
            Assert.That(editorSource, Does.Contain("BaseNode"));
            Assert.That(editorSource, Does.Not.Contain("Ref Port/"));
            Assert.That(editorSource, Does.Not.Contain("Timeline Panel"));
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "Editor/Character/Graph/RefPortedResources/StyleSheet/CharacterBehaviorBaseTree.uss")));
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "Editor/Character/Action/Timeline/RefPortedResources/VisualTree/CommittedActionTimelineEditorWindow.uxml")));
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "Editor/Character/Action/Timeline/RefPortedResources/VisualTree/CommittedActionTimelineClipView.uxml")));
            Assert.That(File.Exists(Path.Combine(Application.dataPath, "Editor/Character/Action/Timeline/RefPortedResources/StyleSheet/CommittedActionTimelineClipView.uss")));
            Assert.That(typeof(CommittedActionRefPortedTimelineView).Assembly.FullName, Does.Contain("Editor"));
        }

        [Test]
        public void BehaviorSourceCommittedActionLeafOpenSwitchesToCommittedBranchMode()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterActionDefinitionSO action = CloneFormalDodgeAction();
            CharacterActionCatalogSO catalog = CreateCatalogAsset(action);
            CharacterConfigSO config = CreateCharacterConfig(catalog);
            CharacterBehaviorEditorWindow window = EditorWindow.GetWindow<CharacterBehaviorEditorWindow>();

            try
            {
                window.SetBehaviorAssetForTests(asset);
                window.SetCharacterConfigForTests(config);
                window.SetActionDefinitionForTests(null);

                bool opened = window.OpenBehaviorSourceNodeForTests("source.committed-action");

                Assert.True(opened);
                Assert.True(window.IsCommittedActionBranchModeForTests);
                Assert.AreSame(asset, window.CurrentBehaviorAssetForTests);
                Assert.AreSame(action, window.CurrentActionDefinitionForTests);
                Assert.AreEqual("branch.root.action.dodge", window.SelectedBranchNodeIdForTests);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(action);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void BehaviorSourceCommittedActionLeafOpenShowsCatalogPickerForMultipleActions()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterActionDefinitionSO dodge = CloneFormalDodgeAction();
            CharacterActionDefinitionSO lightAttack = CloneFormalDodgeAction("Action.LightAttack", "NavigationTestLightAttack");
            CharacterActionCatalogSO catalog = CreateCatalogAsset(dodge, lightAttack);
            CharacterConfigSO config = CreateCharacterConfig(catalog);
            CharacterBehaviorEditorWindow window = EditorWindow.GetWindow<CharacterBehaviorEditorWindow>();
            string sourceBefore = EditorJsonUtility.ToJson(asset);

            try
            {
                window.SetBehaviorAssetForTests(asset);
                window.SetCharacterConfigForTests(config);
                window.SetActionDefinitionForTests(null);

                bool opened = window.OpenBehaviorSourceNodeForTests("source.committed-action");

                Assert.False(opened);
                Assert.True(window.IsBehaviorSourceModeForTests);
                Assert.AreEqual(2, window.PendingActionNavigationEntryCountForTests);
                Assert.That(window.DiagnosticsTextForTests, Does.Contain("Select committed action"));

                Assert.True(window.OpenPendingCatalogActionForTests("Action.LightAttack"));
                Assert.True(window.IsCommittedActionBranchModeForTests);
                Assert.AreSame(lightAttack, window.CurrentActionDefinitionForTests);
                Assert.AreSame(asset, window.CurrentBehaviorAssetForTests);
                Assert.AreEqual(sourceBefore, EditorJsonUtility.ToJson(asset));
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(lightAttack);
                Object.DestroyImmediate(dodge);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void BehaviorSourceCommittedActionLeafOpenReportsDiagnosticWhenCatalogMissing()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
            CharacterBehaviorEditorWindow window = EditorWindow.GetWindow<CharacterBehaviorEditorWindow>();

            try
            {
                window.SetBehaviorAssetForTests(asset);
                window.SetCharacterConfigForTests(config);
                window.SetActionDefinitionForTests(null);

                bool opened = window.OpenBehaviorSourceNodeForTests("source.committed-action");

                Assert.False(opened);
                Assert.True(window.IsBehaviorSourceModeForTests);
                Assert.IsNull(window.CurrentActionDefinitionForTests);
                Assert.That(window.DiagnosticsTextForTests, Does.Contain("action catalog is missing"));
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void BehaviorSourceCommittedActionLeafOpenReportsDiagnosticWhenCatalogEntryMissingDefinition()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterActionCatalogSO catalog = CreateCatalogAsset(new CharacterActionDefinitionSO[] { null });
            CharacterConfigSO config = CreateCharacterConfig(catalog);
            CharacterBehaviorEditorWindow window = EditorWindow.GetWindow<CharacterBehaviorEditorWindow>();

            try
            {
                window.SetBehaviorAssetForTests(asset);
                window.SetCharacterConfigForTests(config);
                window.SetActionDefinitionForTests(null);

                bool opened = window.OpenBehaviorSourceNodeForTests("source.committed-action");

                Assert.False(opened);
                Assert.True(window.IsBehaviorSourceModeForTests);
                Assert.IsNull(window.CurrentActionDefinitionForTests);
                Assert.That(window.DiagnosticsTextForTests, Does.Contain("missing action definition"));
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void BehaviorSourceNonCommittedNodeOpenDoesNotSwitchModes()
        {
            CharacterBehaviorAuthoringAsset asset = CreateSourceGraphAsset();
            CharacterActionDefinitionSO action = CloneFormalDodgeAction();
            CharacterBehaviorEditorWindow window = EditorWindow.GetWindow<CharacterBehaviorEditorWindow>();

            try
            {
                window.SetBehaviorAssetForTests(asset);
                window.SetActionDefinitionForTests(action);

                bool opened = window.OpenBehaviorSourceNodeForTests("source.locomotion");

                Assert.False(opened);
                Assert.True(window.IsBehaviorSourceModeForTests);
                Assert.AreSame(asset, window.CurrentBehaviorAssetForTests);
            }
            finally
            {
                window.Close();
                Object.DestroyImmediate(action);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void BehaviorSourceBranchNavigationKeepsSingleWindowAndRuntimeBoundary()
        {
            string graphSource = ReadSource("Editor/Character/Graph");
            string runtimeSource = ReadSource("Scripts/Character");

            Assert.That(graphSource, Does.Contain("NodeOpened"));
            Assert.That(graphSource, Does.Contain("OpenBehaviorSourceNode"));
            Assert.That(graphSource, Does.Contain("CommittedActionLeafCatalogNavigationSnapshot"));
            Assert.That(graphSource, Does.Contain("CharacterConfigSO"));
            Assert.That(graphSource, Does.Contain("ActionCatalog"));
            Assert.That(graphSource, Does.Contain("source.committed-action"));
            Assert.That(graphSource, Does.Contain("Tools/3C/Character Behavior Editor"));
            Assert.That(graphSource, Does.Not.Contain("Tools/3C/Committed Action Branch Editor"));
            Assert.That(graphSource, Does.Not.Contain("class CommittedActionBranchEditorWindow"));
            Assert.That(graphSource, Does.Not.Contain("Timeline Panel"));
            Assert.That(graphSource, Does.Not.Contain("ResolveNavigationActionDefinition"));
            Assert.That(runtimeSource, Does.Not.Contain("NodeOpened"));
            Assert.That(runtimeSource, Does.Not.Contain("CharacterBehaviorEditorWindow"));
            Assert.That(runtimeSource, Does.Not.Contain("CommittedActionLeafCatalogNavigationSnapshot"));
        }

        [Test]
        public void CatalogNavigationSnapshotListsValidCatalogEntry()
        {
            CharacterActionDefinitionSO action = CloneFormalDodgeAction();
            CharacterActionCatalogSO catalog = CreateCatalogAsset(action);
            CharacterConfigSO config = CreateCharacterConfig(catalog);

            try
            {
                CommittedActionLeafCatalogNavigationSnapshot snapshot =
                    CharacterBehaviorEditorWindow.BuildCatalogNavigationSnapshotForTests(config);

                Assert.False(snapshot.HasErrors, snapshot.DescribeDiagnostics());
                Assert.AreEqual(1, snapshot.ValidEntries.Count);
                Assert.AreEqual(ActionStateIds.Dodge.Value, snapshot.ValidEntries[0].ActionId);
                Assert.AreSame(action, snapshot.ValidEntries[0].Definition);
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(action);
            }
        }

        [Test]
        public void CatalogNavigationSnapshotReportsDuplicateActionId()
        {
            CharacterActionDefinitionSO first = CloneFormalDodgeAction();
            CharacterActionDefinitionSO second = CloneFormalDodgeAction();
            CharacterActionCatalogSO catalog = CreateCatalogAsset(first, second);
            CharacterConfigSO config = CreateCharacterConfig(catalog);

            try
            {
                CommittedActionLeafCatalogNavigationSnapshot snapshot =
                    CharacterBehaviorEditorWindow.BuildCatalogNavigationSnapshotForTests(config);

                Assert.True(snapshot.HasErrors);
                Assert.That(snapshot.DescribeDiagnostics(), Does.Contain("duplicates action id 'Action.Dodge'"));
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(first);
            }
        }

        [Test]
        public void CatalogNavigationSnapshotReportsMissingDefinition()
        {
            CharacterActionCatalogSO catalog = CreateCatalogAsset(new CharacterActionDefinitionSO[] { null });
            CharacterConfigSO config = CreateCharacterConfig(catalog);

            try
            {
                CommittedActionLeafCatalogNavigationSnapshot snapshot =
                    CharacterBehaviorEditorWindow.BuildCatalogNavigationSnapshotForTests(config);

                Assert.True(snapshot.HasErrors);
                Assert.That(snapshot.DescribeDiagnostics(), Does.Contain("missing action definition"));
                Assert.AreEqual(0, snapshot.ValidEntries.Count);
            }
            finally
            {
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void LocomotionEditorLeafDoesNotExecuteMotionAnimationOrBlackboardWrites()
        {
            string source = ReadSource("Scripts/Character/Behavior/Authoring") +
                            ReadSource("Editor/Character/Graph");

            Assert.That(source, Does.Contain("LocomotionLeaf"));
            Assert.That(source, Does.Not.Contain("MotionExecutor"));
            Assert.That(source, Does.Not.Contain("AnimationPresenter"));
            Assert.That(source, Does.Not.Contain("Animancer"));
            Assert.That(source, Does.Not.Contain("RuntimeBlackboard"));
            Assert.That(source, Does.Not.Contain("WriteBlackboard"));
        }

        static CharacterActionDefinitionSO LoadFormalDodgeAction()
        {
            CharacterActionDefinitionSO asset = AssetDatabase.LoadAssetAtPath<CharacterActionDefinitionSO>(FormalDodgeActionPath);
            Assert.NotNull(asset, FormalDodgeActionPath);
            return asset;
        }

        static ActionTimelineCompileContext CompileContext()
        {
            return new ActionTimelineCompileContext(1f / 60f);
        }

        static void AssertTimelineHasEditableClipKinds(ActionTimelineDefinition timeline)
        {
            Assert.NotNull(timeline);
            Assert.True(timeline.Tracks.Any(track => track.Kind == ActionTimelineTrackKind.Animation &&
                                                     track.Clips.Any(clip => clip.Kind == ActionTimelineClipKind.AnimationKey)));
            Assert.True(timeline.Tracks.Any(track => track.Kind == ActionTimelineTrackKind.Motion &&
                                                     track.Clips.Any(clip => clip.Kind == ActionTimelineClipKind.Motion)));
            Assert.True(timeline.Tracks.Any(track => track.Kind == ActionTimelineTrackKind.Hitbox &&
                                                     track.Clips.Any(clip => clip.Kind == ActionTimelineClipKind.HitboxWindow)));
            Assert.True(timeline.Tracks.Any(track => track.Kind == ActionTimelineTrackKind.Cancel &&
                                                     track.Clips.Any(clip => clip.Kind == ActionTimelineClipKind.CancelWindow)));
            Assert.True(timeline.Tracks.Any(track => track.Kind == ActionTimelineTrackKind.Cue &&
                                                     track.Clips.Any(clip => clip.Kind == ActionTimelineClipKind.Cue)));
        }

        static CharacterActionDefinitionSO CloneFormalDodgeAction()
        {
            return CloneFormalDodgeAction(ActionStateIds.Dodge.Value, "BehaviorSourceNavigationTestDodgeAction");
        }

        static CharacterActionDefinitionSO CloneFormalDodgeAction(string actionId, string name)
        {
            CharacterActionDefinitionSO clone = Object.Instantiate(LoadFormalDodgeAction());
            clone.name = name;
            SetPrivateField(clone, "actionStateId", actionId);
            return clone;
        }

        static CharacterActionCatalogSO CreateCatalogAsset(params CharacterActionDefinitionSO[] definitions)
        {
            CharacterActionCatalogSO asset = ScriptableObject.CreateInstance<CharacterActionCatalogSO>();
            SetPrivateField(asset, "definitions", definitions);
            return asset;
        }

        static CharacterConfigSO CreateCharacterConfig(CharacterActionCatalogSO catalog)
        {
            CharacterConfigSO config = ScriptableObject.CreateInstance<CharacterConfigSO>();
            SetPrivateField(config, "actionCatalog", catalog);
            return config;
        }

        static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, PrivateInstance);
            Assert.NotNull(field, fieldName);
            field.SetValue(target, value);
        }

        static CharacterBehaviorAuthoringAsset CreateSourceGraphAsset()
        {
            CharacterBehaviorAuthoringAsset asset = ScriptableObject.CreateInstance<CharacterBehaviorAuthoringAsset>();
            asset.SetStableAssetId("behavior.navigation.test");
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

        static string ReadSource(string relativeFromAssets)
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, relativeFromAssets));
            return string.Join(
                "\n",
                Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                    .Select(path => File.ReadAllText(path, Encoding.UTF8)));
        }

        static string ReadFile(string relativeFromAssets)
        {
            return File.ReadAllText(Path.Combine(Application.dataPath, relativeFromAssets), Encoding.UTF8);
        }
    }
}
