using ThirdPersonCharacter.Animation.TransitionRouting;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.Animation.TransitionRouting
{
    public sealed class AnimationTransitionRoutingFixtureWindow : EditorWindow
    {
        readonly AnimationTransitionRoutingFixtureSession m_Session = new AnimationTransitionRoutingFixtureSession();
        AnimationTransitionRoutingFixtureAsset m_Asset;
        UnityEditor.Editor m_AssetEditor;
        Vector2 m_AuthoringScroll;
        Vector2 m_RuntimeScroll;

        [MenuItem("Tools/3C/Animation/Transition Routing Fixture")]
        static void Open()
        {
            GetWindow<AnimationTransitionRoutingFixtureWindow>("Transition Routing");
        }

        void OnDisable()
        {
            if (m_AssetEditor != null)
                DestroyImmediate(m_AssetEditor);
        }

        void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawAuthoring();
                DrawRuntime();
            }
        }

        void DrawHeader()
        {
            EditorGUI.BeginChangeCheck();
            var selected = (AnimationTransitionRoutingFixtureAsset)EditorGUILayout.ObjectField(
                "Fixture Definition",
                m_Asset,
                typeof(AnimationTransitionRoutingFixtureAsset),
                false);
            if (EditorGUI.EndChangeCheck())
                SetAsset(selected);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(m_Asset == null))
                {
                    if (GUILayout.Button("Compile", GUILayout.Width(90f)))
                        m_Session.Compile();
                }

                using (new EditorGUI.DisabledScope(!m_Session.HasCompiledPlan))
                {
                    if (GUILayout.Button("Reset Runtime", GUILayout.Width(110f)))
                        m_Session.ResetRuntime();
                    if (GUILayout.Button("Step Frame", GUILayout.Width(90f)))
                        m_Session.StepNext();
                    if (GUILayout.Button("Run Sequence", GUILayout.Width(110f)))
                        m_Session.RunSequence();
                    if (GUILayout.Button("Clear Timeline", GUILayout.Width(105f)))
                        m_Session.ClearTimeline();
                }
            }

            EditorGUILayout.HelpBox(
                "Compile and execution are explicit. Asset selection, field edits, domain reload and Play Mode changes do not compile or run this fixture.",
                MessageType.Info);
            EditorGUILayout.HelpBox("Pose Evaluation: Not Connected", MessageType.Warning);
        }

        void DrawAuthoring()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(430f)))
            {
                EditorGUILayout.LabelField("Fixture Definition", EditorStyles.boldLabel);
                m_AuthoringScroll = EditorGUILayout.BeginScrollView(m_AuthoringScroll);
                if (m_Asset == null)
                {
                    EditorGUILayout.HelpBox(
                        "Create a 3C/Animation/Transition Routing Fixture asset and assign it here.",
                        MessageType.Info);
                }
                else
                {
                    if (m_AssetEditor == null)
                        m_AssetEditor = UnityEditor.Editor.CreateEditor(m_Asset);
                    m_AssetEditor.OnInspectorGUI();
                    EditorGUILayout.HelpBox(
                        "Edited values remain authoring data until Compile is clicked again.",
                        MessageType.None);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawRuntime()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Compiled Plan And Runtime", EditorStyles.boldLabel);
                m_RuntimeScroll = EditorGUILayout.BeginScrollView(m_RuntimeScroll);
                DrawCompileResult();
                DrawRuleMatrix();
                DrawSnapshot();
                DrawFrameTimeline();
                DrawRuntimeEvents();
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawCompileResult()
        {
            TransitionRoutingCompileResult result = m_Session.CompileResult;
            if (result == null)
            {
                EditorGUILayout.HelpBox("No compiled plan.", MessageType.Info);
                return;
            }

            if (!result.Succeeded)
            {
                EditorGUILayout.LabelField("Compile Diagnostics", EditorStyles.boldLabel);
                for (int i = 0; i < result.Diagnostics.Count; i++)
                {
                    TransitionRoutingDiagnostic diagnostic = result.Diagnostics[i];
                    EditorGUILayout.HelpBox(diagnostic.ToString(), MessageType.Error);
                }
                return;
            }

            CompiledTransitionRoutingPlan plan = result.Plan;
            EditorGUILayout.LabelField("Plan", EditorStyles.boldLabel);
            DrawValue("Plan Id", plan.PlanId.ToString());
            DrawValue("Schema", plan.SchemaVersion.ToString());
            DrawValue("Definition Revision", plan.DefinitionRevision.ToString());
            DrawValue("Canonical Hash", plan.CanonicalHash.ToString());
            DrawValue("Endpoint Count", plan.Endpoints.Count.ToString());
            DrawValue("Rule Count", plan.Rules.Count.ToString());
        }

        void DrawRuleMatrix()
        {
            CompiledTransitionRoutingPlan plan = m_Session.Plan;
            if (plan == null)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Exact Rule Matrix", EditorStyles.boldLabel);
            for (int i = 0; i < plan.Rules.Count; i++)
            {
                AnimationTransitionRule rule = plan.Rules[i];
                string outcome = rule.IsHardCutOutcome ? "Hard Cut outcome" : rule.BlendLogic.ToString();
                EditorGUILayout.LabelField(
                    $"{rule.SourceEndpoint} -> {rule.TargetEndpoint}",
                    $"{rule.RuleId} | {outcome} | {rule.DurationSeconds:0.###}s");
            }
        }

        void DrawSnapshot()
        {
            if (m_Session.Workspace == null)
                return;

            TransitionRoutingRuntimeSnapshot snapshot = m_Session.Snapshot;
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Runtime Snapshot", EditorStyles.boldLabel);
            DrawValue("Next Sequence Index", m_Session.NextFrameIndex.ToString());
            DrawValue("Frame", snapshot.FrameId.ToString());
            DrawValue("Owner", snapshot.OwnerNodeId.ToString());
            DrawValue("Current Endpoint", snapshot.CurrentEndpoint.ToString());
            DrawValue("Requested Endpoint", snapshot.RequestedEndpoint.ToString());
            DrawValue("Selection Generation", snapshot.SelectionGeneration.ToString());
            DrawValue("Module Generation", snapshot.ModuleGeneration.ToString());
            DrawValue("Lifecycle", snapshot.Lifecycle.ToString());
            DrawValue("Active Rule", snapshot.ActiveRuleId.ToString());
            DrawValue("Has Request", snapshot.HasActiveRequest.ToString());
            if (snapshot.HasActiveRequest)
            {
                DrawValue("Request Event", snapshot.ActiveRequest.RequestEventId.ToString());
                DrawValue("Request Generation", snapshot.ActiveRequest.RequestGeneration.ToString());
            }
            DrawValue("Capture Completed", snapshot.CaptureCompleted.ToString());
            DrawValue("Release Completed", snapshot.ReleaseCompleted.ToString());
            DrawValue("Rebase Count", snapshot.RebaseCount.ToString());
            DrawValue("Reset Reason", snapshot.ResetReason.ToString());
            DrawValue("Reason", snapshot.ReasonCode == TransitionRoutingReasonCode.None
                ? string.Empty
                : $"{snapshot.ReasonCode}: {snapshot.Reason}");
        }

        void DrawFrameTimeline()
        {
            if (m_Session.Records.Count == 0)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Frame Timeline", EditorStyles.boldLabel);
            for (int i = 0; i < m_Session.Records.Count; i++)
            {
                AnimationTransitionRoutingFixtureFrameRecord record = m_Session.Records[i];
                TransitionRoutingFrameOutput output = record.Output;
                string flags =
                    $"{(output.CapturePermission ? " Capture" : string.Empty)}" +
                    $"{(output.ReleasePermission ? " Release" : string.Empty)}" +
                    $"{(output.RebaseRequired ? " Rebase" : string.Empty)}";
                EditorGUILayout.LabelField(
                    $"[{record.SequenceIndex}] Frame {record.Input.FrameId}",
                    $"{output.DecisionKind} / {output.Lifecycle}{flags}");
                if (output.ReasonCode != TransitionRoutingReasonCode.None)
                    EditorGUILayout.LabelField(string.Empty, $"{output.ReasonCode}: {output.Reason}", EditorStyles.miniLabel);
            }
        }

        void DrawRuntimeEvents()
        {
            if (m_Session.Workspace == null || m_Session.Workspace.EventCount == 0)
                return;

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Bounded Event Timeline", EditorStyles.boldLabel);
            TransitionRoutingEvent[] events = m_Session.Workspace.CopyEvents();
            for (int i = 0; i < events.Length; i++)
            {
                TransitionRoutingEvent item = events[i];
                EditorGUILayout.LabelField(
                    $"Frame {item.FrameId} | {item.Kind}",
                    $"{item.Lifecycle} | {item.Message}");
            }
        }

        void SetAsset(AnimationTransitionRoutingFixtureAsset asset)
        {
            if (m_AssetEditor != null)
            {
                DestroyImmediate(m_AssetEditor);
                m_AssetEditor = null;
            }

            m_Asset = asset;
            m_Session.SetAsset(asset);
        }

        static void DrawValue(string label, string value)
        {
            EditorGUILayout.LabelField(label, value ?? string.Empty);
        }
    }
}
