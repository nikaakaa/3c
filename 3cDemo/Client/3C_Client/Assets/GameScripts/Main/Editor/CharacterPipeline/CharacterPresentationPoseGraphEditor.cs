using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [Serializable]
    sealed class PoseGraphClipboardPayload
    {
        public CharacterPoseNodeDefinition[] nodes = Array.Empty<CharacterPoseNodeDefinition>();
        public CharacterPoseEdge[] edges = Array.Empty<CharacterPoseEdge>();
        public Vector2 center;
    }

    [Serializable]
    sealed class PoseGraphWatchViewState
    {
        public string graphId = string.Empty;
        public string graphRevision = string.Empty;
        public string nodeId = string.Empty;
        public string callSite = string.Empty;
        public Color color = Color.cyan;
        public bool visible = true;
        public string boneFilter = string.Empty;

        public AnimationPoseWatchIdentity ToIdentity() => new AnimationPoseWatchIdentity(
            graphId,
            graphRevision,
            new PoseNodeId(nodeId),
            callSite);

        public bool Matches(AnimationPoseWatchIdentity identity) =>
            string.Equals(graphId, identity.GraphId, StringComparison.Ordinal) &&
            string.Equals(graphRevision, identity.GraphRevision, StringComparison.Ordinal) &&
            string.Equals(nodeId, identity.NodeId.Value, StringComparison.Ordinal) &&
            string.Equals(callSite, identity.CallSite, StringComparison.Ordinal);
    }

    [Serializable]
    sealed class PosePreviewViewportState
    {
        public float yaw = 135f;
        public float pitch = 12f;
        public float distance = 4f;
        public bool showGrid = true;
        public bool showSkeleton = true;
        public bool showRootTrajectory = true;
        public bool showFootPlacement = true;
    }

    sealed class PosePreviewViewportElement : IMGUIContainer
    {
        const int RootTrajectoryCapacity = 256;
        readonly PosePreviewViewportState m_State;
        readonly Action m_RepaintWindow;
        readonly List<Vector3> m_RootTrajectory = new List<Vector3>(RootTrajectoryCapacity);
        CharacterPipelineHost m_Target;
        GameObject m_CameraObject;
        Camera m_Camera;

        public PosePreviewViewportElement(PosePreviewViewportState state, Action repaintWindow)
        {
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_RepaintWindow = repaintWindow;
            style.flexGrow = 1f;
            style.minHeight = 220f;
            onGUIHandler = Draw;
        }

        public void SetTarget(CharacterPipelineHost target)
        {
            if (ReferenceEquals(m_Target, target))
                return;
            m_Target = target;
            m_RootTrajectory.Clear();
            RecordRoot();
            MarkDirtyRepaint();
        }

        public void RecordRoot()
        {
            if (!m_Target || !m_Target.VisualRoot)
                return;
            Vector3 point = m_Target.VisualRoot.position;
            if (m_RootTrajectory.Count > 0 && (m_RootTrajectory[m_RootTrajectory.Count - 1] - point).sqrMagnitude < 0.000001f)
                return;
            if (m_RootTrajectory.Count == RootTrajectoryCapacity)
                m_RootTrajectory.RemoveAt(0);
            m_RootTrajectory.Add(point);
        }

        public void ClearTrajectory()
        {
            m_RootTrajectory.Clear();
            RecordRoot();
            MarkDirtyRepaint();
        }

        public void ReleaseCamera()
        {
            if (m_CameraObject)
                UnityEngine.Object.DestroyImmediate(m_CameraObject);
            m_CameraObject = null;
            m_Camera = null;
        }

        void Draw()
        {
            Rect rect = new Rect(0f, 0f, resolvedStyle.width, resolvedStyle.height);
            EditorGUI.DrawRect(rect, new Color(0.055f, 0.06f, 0.065f));
            HandleInput(rect);
            if (!m_Target || !m_Target.VisualRoot || rect.width <= 1f || rect.height <= 1f)
            {
                EditorGUI.LabelField(new Rect(12f, 12f, Math.Max(0f, rect.width - 24f), 22f), "Select a scene Preview Target.");
                return;
            }
            EnsureCamera();
            Bounds bounds = ResolveBounds(m_Target.VisualRoot);
            ConfigureCamera(bounds);
            if (Event.current.type == EventType.Repaint)
                Handles.DrawCamera(rect, m_Camera, DrawCameraMode.Textured);
            Handles.BeginGUI();
            if (m_State.showGrid)
                DrawGrid(rect, bounds);
            if (m_State.showRootTrajectory)
                DrawRootTrajectory(rect);
            if (m_State.showSkeleton)
                DrawSkeleton(rect);
            Handles.EndGUI();
        }

        void HandleInput(Rect rect)
        {
            Event current = Event.current;
            if (!rect.Contains(current.mousePosition))
                return;
            if (current.type == EventType.MouseDrag && (current.button == 0 || current.button == 1))
            {
                m_State.yaw += current.delta.x * 0.5f;
                m_State.pitch = Mathf.Clamp(m_State.pitch - current.delta.y * 0.5f, -80f, 80f);
                current.Use();
                m_RepaintWindow?.Invoke();
            }
            else if (current.type == EventType.ScrollWheel)
            {
                m_State.distance = Mathf.Clamp(m_State.distance * (1f + current.delta.y * 0.05f), 0.25f, 100f);
                current.Use();
                m_RepaintWindow?.Invoke();
            }
        }

        void EnsureCamera()
        {
            if (m_Camera)
                return;
            m_CameraObject = new GameObject("Pose Preview Viewport Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            m_Camera = m_CameraObject.AddComponent<Camera>();
            m_Camera.enabled = false;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = new Color(0.055f, 0.06f, 0.065f);
            m_Camera.fieldOfView = 30f;
        }

        void ConfigureCamera(Bounds bounds)
        {
            float fittedDistance = Math.Max(0.5f, bounds.extents.magnitude / Mathf.Tan(m_Camera.fieldOfView * 0.5f * Mathf.Deg2Rad));
            float distance = Math.Max(m_State.distance, fittedDistance);
            Quaternion rotation = Quaternion.Euler(m_State.pitch, m_State.yaw, 0f);
            m_Camera.transform.SetPositionAndRotation(bounds.center - rotation * Vector3.forward * distance, rotation);
            m_Camera.nearClipPlane = Math.Max(0.01f, distance - bounds.extents.magnitude * 2f);
            m_Camera.farClipPlane = Math.Max(m_Camera.nearClipPlane + 10f, distance + bounds.extents.magnitude * 4f);
        }

        void DrawGrid(Rect rect, Bounds bounds)
        {
            float radius = Math.Max(1f, bounds.extents.magnitude * 1.5f);
            float step = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(radius)) - 1f);
            if (step < 0.1f)
                step = 0.1f;
            int lineCount = Mathf.Clamp(Mathf.CeilToInt(radius / step), 2, 20);
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Handles.color = new Color(0.35f, 0.38f, 0.42f, 0.45f);
            for (int i = -lineCount; i <= lineCount; i++)
            {
                float offset = i * step;
                DrawWorldLine(rect, center + new Vector3(offset, 0f, -lineCount * step), center + new Vector3(offset, 0f, lineCount * step), 1f);
                DrawWorldLine(rect, center + new Vector3(-lineCount * step, 0f, offset), center + new Vector3(lineCount * step, 0f, offset), 1f);
            }
        }

        void DrawRootTrajectory(Rect rect)
        {
            if (m_RootTrajectory.Count < 2)
                return;
            Handles.color = new Color(1f, 0.65f, 0.15f, 0.95f);
            for (int i = 1; i < m_RootTrajectory.Count; i++)
                DrawWorldLine(rect, m_RootTrajectory[i - 1], m_RootTrajectory[i], 2f);
        }

        void DrawSkeleton(Rect rect)
        {
            CharacterAnimationRigBinding binding = m_Target.AnimationRigBinding;
            CharacterAnimationRigDefinition rig = m_Target.Definition && m_Target.Definition.AnimationPresentationProfile
                ? m_Target.Definition.AnimationPresentationProfile.RigDefinition
                : null;
            if (!binding || !rig || binding.Bones.Count != rig.Bones.Count)
                return;
            Handles.color = new Color(0.1f, 0.9f, 1f, 0.9f);
            for (int i = 0; i < binding.Bones.Count; i++)
            {
                Transform bone = binding.Bones[i];
                if (!bone)
                    continue;
                int parentIndex = rig.Bones[i].ParentIndex;
                if (parentIndex >= 0 && parentIndex < binding.Bones.Count && binding.Bones[parentIndex])
                    DrawWorldLine(rect, binding.Bones[parentIndex].position, bone.position, 2f);
                Vector2 point = WorldToGui(rect, bone.position, out bool visible);
                if (visible)
                    Handles.DrawSolidDisc(point, Vector3.forward, 2f);
            }
        }

        void DrawWorldLine(Rect rect, Vector3 start, Vector3 end, float width)
        {
            Vector2 startPoint = WorldToGui(rect, start, out bool startVisible);
            Vector2 endPoint = WorldToGui(rect, end, out bool endVisible);
            if (startVisible && endVisible)
                Handles.DrawAAPolyLine(width, startPoint, endPoint);
        }

        Vector2 WorldToGui(Rect rect, Vector3 world, out bool visible)
        {
            Vector3 viewport = m_Camera.WorldToViewportPoint(world);
            visible = viewport.z > 0f;
            return new Vector2(rect.x + viewport.x * rect.width, rect.y + (1f - viewport.y) * rect.height);
        }

        static Bounds ResolveBounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(root.position + Vector3.up, new Vector3(1f, 2f, 1f));
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }

    sealed class PoseWatchViewportPose
    {
        public Color Color;
        public Vector3[] Positions;
        public string BoneFilter;
    }

    sealed class PoseWatchViewportElement : IMGUIContainer
    {
        readonly CharacterAnimationRigDefinition m_Rig;
        readonly IReadOnlyList<PoseWatchViewportPose> m_Poses;

        public PoseWatchViewportElement(
            CharacterAnimationRigDefinition rig,
            IReadOnlyList<PoseWatchViewportPose> poses)
        {
            m_Rig = rig;
            m_Poses = poses;
            style.height = 220f;
            onGUIHandler = Draw;
        }

        void Draw()
        {
            Rect rect = new Rect(0f, 0f, resolvedStyle.width, resolvedStyle.height);
            EditorGUI.DrawRect(rect, new Color(0.08f, 0.08f, 0.08f));
            if (!m_Rig || m_Poses == null || m_Poses.Count == 0 || rect.width <= 1f || rect.height <= 1f)
                return;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int poseIndex = 0; poseIndex < m_Poses.Count; poseIndex++)
            {
                Vector3[] positions = m_Poses[poseIndex].Positions;
                for (int boneIndex = 0; boneIndex < positions.Length; boneIndex++)
                {
                    Vector2 point = Project(positions[boneIndex]);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }
            Vector2 size = max - min;
            if (size.x < 0.001f)
                size.x = 1f;
            if (size.y < 0.001f)
                size.y = 1f;
            float scale = Mathf.Min((rect.width - 32f) / size.x, (rect.height - 32f) / size.y);
            Vector2 offset = new Vector2(16f, rect.height - 16f);
            Handles.BeginGUI();
            for (int poseIndex = 0; poseIndex < m_Poses.Count; poseIndex++)
            {
                PoseWatchViewportPose pose = m_Poses[poseIndex];
                Handles.color = pose.Color;
                for (int boneIndex = 0; boneIndex < pose.Positions.Length && boneIndex < m_Rig.Bones.Count; boneIndex++)
                {
                    CharacterAnimationBoneDefinition bone = m_Rig.Bones[boneIndex];
                    if (!MatchesBoneFilter(bone, pose.BoneFilter))
                        continue;
                    Vector2 point = ToViewport(Project(pose.Positions[boneIndex]), min, scale, offset);
                    if (bone.ParentIndex >= 0 && bone.ParentIndex < pose.Positions.Length &&
                        MatchesBoneFilter(m_Rig.Bones[bone.ParentIndex], pose.BoneFilter))
                    {
                        Vector2 parent = ToViewport(Project(pose.Positions[bone.ParentIndex]), min, scale, offset);
                        Handles.DrawAAPolyLine(2f, parent, point);
                    }
                    Handles.DrawSolidDisc(point, Vector3.forward, 2.2f);
                }
            }
            Handles.EndGUI();
        }

        static Vector2 Project(Vector3 value) => new Vector2(value.x + value.z * 0.25f, value.y);

        static Vector2 ToViewport(Vector2 value, Vector2 min, float scale, Vector2 offset) =>
            new Vector2(offset.x + (value.x - min.x) * scale, offset.y - (value.y - min.y) * scale);

        static bool MatchesBoneFilter(CharacterAnimationBoneDefinition bone, string filter) =>
            bone != null && (string.IsNullOrWhiteSpace(filter) ||
                             bone.BoneId.Value.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    sealed class PoseGraphPosePortValue { }
    sealed class PoseGraphSelectionPortValue { }
    sealed class PoseGraphParameterPortValue { }

    static class PoseGraphDisplayNames
    {
        public static string Node(CharacterPoseNodeKind kind)
        {
            return kind switch
            {
                CharacterPoseNodeKind.LayeredBoneBlend => "Layered Blend Per Bone",
                CharacterPoseNodeKind.ProgramParameterInput => "Animation Parameter",
                CharacterPoseNodeKind.OutputPose => "Output Pose",
                CharacterPoseNodeKind.PoseSubgraph => "Pose Subgraph",
                CharacterPoseNodeKind.FootPlacement => "Foot Placement",
                CharacterPoseNodeKind.SelectedPosePlayer => "Selected Pose Player",
                CharacterPoseNodeKind.MotionMatchingSelectionInput => "Motion Matching Selection",
                CharacterPoseNodeKind.AnimationSelectionInput => "Animation Selection",
                CharacterPoseNodeKind.PoseParameterResolve => "Pose Parameter Resolve",
                CharacterPoseNodeKind.ModifyBone => "Modify Bone",
                CharacterPoseNodeKind.MarkerSync => "Marker Sync",
                CharacterPoseNodeKind.BlendStack => "Blend Stack",
                CharacterPoseNodeKind.BlendSpacePlayer => "Blend Space Player",
                CharacterPoseNodeKind.BlendPose => "Blend Pose",
                CharacterPoseNodeKind.AdditivePose => "Additive Pose",
                _ => kind.ToString()
            };
        }

        public static string Stage(CharacterPoseNodeKind kind)
        {
            return kind switch
            {
                CharacterPoseNodeKind.AnimationSelectionInput or
                CharacterPoseNodeKind.MotionMatchingSelectionInput or
                CharacterPoseNodeKind.ProgramParameterInput or
                CharacterPoseNodeKind.MarkerSync => "SELECTION",
                CharacterPoseNodeKind.FootPlacement => "WORLD-AWARE",
                CharacterPoseNodeKind.OutputPose => "OUTPUT",
                _ => "SOURCE / POSE"
            };
        }
    }

    sealed class PoseGraphNodeConfiguration
    {
        public ThirdPersonSimulation.AnimationChannelId AnimationChannelId;
        public string ProgramProducerId;
        public PoseParameterId ParameterId;
        public AnimationSelectionAvailabilityPolicy SelectionAvailability;
        public CharacterAnimationBlendSpaceInputRangePolicy BlendSpaceInputRangePolicy;
        public CharacterAnimationBlendPolicy BlendPolicy;
        public CharacterPoseInertializationPolicy InertializationPolicy;
        public CharacterAnimationBoneMaskAsset BoneMask;
        public float Weight;
        public CharacterPoseParameterPolicy[] ParameterPolicies;
        public string AdditiveReferencePoseId;
        public AdditiveReferenceSpace AdditiveReferenceSpace;
        public AdditiveScalePolicy AdditiveScalePolicy;
        public AnimationBoneId BoneId;
        public ModifyBoneReferenceSpace ModifyBoneReferenceSpace;
        public ModifyBoneOperationMask ModifyBoneOperations;
        public Vector3 ModifyPosition;
        public Vector3 ModifyRotationEuler;
        public Vector3 ModifyScale;
        public CharacterFootPlacementProfile FootPlacementProfile;
        public CharacterFootPlacementRigCalibration FootPlacementCalibration;

        public PoseGraphNodeConfiguration(CharacterPoseNodeDefinition node)
        {
            AnimationChannelId = node.AnimationChannelId;
            ProgramProducerId = node.ProgramProducerId;
            ParameterId = node.ParameterId;
            SelectionAvailability = node.SelectionAvailability;
            BlendSpaceInputRangePolicy = node.BlendSpaceInputRangePolicy;
            BlendPolicy = node.BlendPolicy;
            InertializationPolicy = node.InertializationPolicy;
            BoneMask = node.BoneMask;
            Weight = node.Weight;
            ParameterPolicies = node.ParameterPolicies.ToArray();
            AdditiveReferencePoseId = node.AdditiveReferencePoseId;
            AdditiveReferenceSpace = node.AdditiveReferenceSpace;
            AdditiveScalePolicy = node.AdditiveScalePolicy;
            BoneId = node.BoneId;
            ModifyBoneReferenceSpace = node.ModifyBoneReferenceSpace;
            ModifyBoneOperations = node.ModifyBoneOperations;
            ModifyPosition = node.ModifyPosition;
            ModifyRotationEuler = node.ModifyRotation.eulerAngles;
            ModifyScale = node.ModifyScale;
            FootPlacementProfile = node.FootPlacementProfile;
            FootPlacementCalibration = node.FootPlacementCalibration;
        }
    }

    sealed class PoseGraphNodeView : Node
    {
        readonly Dictionary<PosePortId, Port> m_Ports = new Dictionary<PosePortId, Port>();
        readonly Label m_Diagnostic = new Label();
        readonly Label m_Stage = new Label();
        bool m_DiagnosticsInitialized;
        bool m_LastExecuted;
        string m_LastDiagnosticText = string.Empty;

        public PoseGraphNodeView(
            CharacterPoseNodeDefinition node,
            Action<CharacterPoseNodeDefinition> openSubgraph,
            Action<CharacterPoseNodeDefinition, DropdownMenu> buildContextMenu,
            string summary)
        {
            Node = node;
            string formalName = PoseGraphDisplayNames.Node(node.Kind);
            title = string.IsNullOrWhiteSpace(node.DisplayName) ? formalName : node.DisplayName;
            tooltip = $"{formalName} / {node.Kind} / {node.NodeId}";
            viewDataKey = node.NodeId.Value;
            SetPosition(new Rect(node.Position, new Vector2(220f, 120f)));
            for (int i = 0; i < node.Ports.Count; i++)
            {
                CharacterPosePortDefinition definition = node.Ports[i];
                Direction direction = definition.Direction == CharacterPosePortDirection.Input ? Direction.Input : Direction.Output;
                Port.Capacity capacity = direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi;
                Type valueType = definition.Kind switch
                {
                    CharacterPosePortKind.Pose => typeof(PoseGraphPosePortValue),
                    CharacterPosePortKind.AnimationSelection => typeof(PoseGraphSelectionPortValue),
                    _ => typeof(PoseGraphParameterPortValue)
                };
                Port port = Port.Create<Edge>(Orientation.Horizontal, direction, capacity, valueType);
                port.portName = string.IsNullOrWhiteSpace(definition.Name) ? definition.PortId.Value : definition.Name;
                port.userData = definition;
                m_Ports.Add(definition.PortId, port);
                (direction == Direction.Input ? inputContainer : outputContainer).Add(port);
            }
            m_Diagnostic.style.whiteSpace = WhiteSpace.Normal;
            m_Diagnostic.style.color = new Color(1f, 0.55f, 0.35f);
            m_Diagnostic.style.display = DisplayStyle.None;
            m_Stage.text = PoseGraphDisplayNames.Stage(node.Kind);
            m_Stage.style.unityFontStyleAndWeight = FontStyle.Bold;
            m_Stage.style.fontSize = 9f;
            m_Stage.style.color = new Color(0.55f, 0.72f, 0.85f);
            extensionContainer.Add(m_Stage);
            if (!string.IsNullOrEmpty(summary))
            {
                var value = new Label(summary);
                value.style.whiteSpace = WhiteSpace.Normal;
                value.style.color = new Color(0.7f, 0.8f, 0.85f);
                extensionContainer.Add(value);
            }
            if (node.Kind == CharacterPoseNodeKind.MarkerSync)
            {
                var sync = new Label("SYNC GROUP");
                sync.style.color = new Color(0.55f, 0.75f, 0.55f);
                extensionContainer.Add(sync);
            }
            extensionContainer.Add(m_Diagnostic);
            if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
            {
                RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0 && evt.clickCount == 2)
                        openSubgraph(Node);
                });
            }
            this.AddManipulator(new ContextualMenuManipulator(evt => buildContextMenu?.Invoke(Node, evt.menu)));
            RefreshExpandedState();
            RefreshPorts();
        }

        public CharacterPoseNodeDefinition Node { get; }

        public bool TryGetPort(PosePortId portId, out Port port) => m_Ports.TryGetValue(portId, out port);

        public void SetDiagnostics(IReadOnlyList<string> messages, bool executed)
        {
            string text = messages == null || messages.Count == 0
                ? string.Empty
                : string.Join("\n", messages);
            if (m_DiagnosticsInitialized &&
                m_LastExecuted == executed &&
                string.Equals(m_LastDiagnosticText, text, StringComparison.Ordinal))
            {
                return;
            }
            m_DiagnosticsInitialized = true;
            m_LastExecuted = executed;
            m_LastDiagnosticText = text;
            EnableInClassList("pose-node-executed", executed);
            style.borderLeftColor = executed ? new Color(0.2f, 0.75f, 1f) : Color.clear;
            style.borderLeftWidth = executed ? 3f : 0f;
            if (text.Length == 0)
            {
                m_Diagnostic.text = string.Empty;
                m_Diagnostic.style.display = DisplayStyle.None;
                return;
            }
            m_Diagnostic.text = text;
            m_Diagnostic.style.display = DisplayStyle.Flex;
            expanded = true;
            RefreshExpandedState();
        }
    }

    sealed class PoseGraphView : GraphView, IGraphAuthoringDomainView
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly Dictionary<PoseNodeId, PoseGraphNodeView> m_Nodes = new Dictionary<PoseNodeId, PoseGraphNodeView>();
        IGraphAuthoringDocument m_Document;
        IGraphAuthoringPortPolicy m_PortPolicy;
        IGraphAuthoringMutationAdapter m_Mutation;
        bool m_Rebuilding;

        public PoseGraphView(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
            StyleSheet style = Resources.Load<StyleSheet>("StyleSheet/BaseTree");
            if (style)
                styleSheets.Add(style);
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            graphViewChanged = ApplyChange;
            RegisterCallback<MouseMoveEvent>(evt =>
            {
                LocalMousePosition = contentViewContainer.WorldToLocal(evt.originalMousePosition);
            });
        }

        public Vector2 LocalMousePosition { get; private set; }

        public void BindAdapters(
            IGraphAuthoringDocument document,
            IGraphAuthoringPortPolicy portPolicy,
            IGraphAuthoringMutationAdapter mutation)
        {
            m_Document = document;
            m_PortPolicy = portPolicy;
            m_Mutation = mutation;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return m_PortPolicy == null
                ? new List<Port>()
                : ports.ToList().Where(port => m_PortPolicy.CanConnect(m_Document, startPort, port)).ToList();
        }

        public void Populate(CharacterPoseGraphData graph)
        {
            m_Rebuilding = true;
            DeleteElements(graphElements.ToList());
            m_Nodes.Clear();
            if (graph != null)
            {
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    CharacterPoseNodeDefinition node = graph.Nodes[i];
                    if (node == null)
                        continue;
                    var view = new PoseGraphNodeView(
                        node,
                        m_Window.OpenSubgraph,
                        m_Window.BuildPoseNodeContextMenu,
                        m_Window.ResolveNodeSummary(node));
                    m_Nodes.Add(node.NodeId, view);
                    AddElement(view);
                }
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    CharacterPoseEdge edge = graph.Edges[i];
                    if (edge == null || !m_Nodes.TryGetValue(edge.SourceNodeId, out PoseGraphNodeView source) ||
                        !m_Nodes.TryGetValue(edge.TargetNodeId, out PoseGraphNodeView target) ||
                        !source.TryGetPort(edge.SourcePortId, out Port output) ||
                        !target.TryGetPort(edge.TargetPortId, out Port input))
                        continue;
                    Edge view = output.ConnectTo(input);
                    view.userData = edge;
                    AddElement(view);
                }
            }
            m_Rebuilding = false;
        }

        public void ApplyDiagnostics(
            CharacterPoseGraphValidationReport report,
            CharacterPoseGraphData graph,
            IReadOnlyDictionary<PoseNodeId, IReadOnlyList<string>> runtimeMessages = null)
        {
            foreach (KeyValuePair<PoseNodeId, PoseGraphNodeView> pair in m_Nodes)
            {
                var messages = report?.Issues
                    .Where(issue => string.Equals(issue.GraphId, graph?.GraphId, StringComparison.Ordinal) && issue.NodeId.Equals(pair.Key))
                    .Select(issue => issue.Message)
                    .ToList() ?? new List<string>();
                if (runtimeMessages != null && runtimeMessages.TryGetValue(pair.Key, out IReadOnlyList<string> live))
                    messages.AddRange(live);
                bool executed = runtimeMessages != null && runtimeMessages.ContainsKey(pair.Key);
                pair.Value.SetDiagnostics(messages, executed);
            }
        }

        public bool FocusNode(PoseNodeId nodeId)
        {
            if (!m_Nodes.TryGetValue(nodeId, out PoseGraphNodeView view))
                return false;
            ClearSelection();
            AddToSelection(view);
            FrameSelection();
            return true;
        }

        GraphViewChange ApplyChange(GraphViewChange change)
        {
            if (m_Rebuilding || m_Window.CurrentGraph == null)
                return change;
            return m_Mutation == null
                ? change
                : m_Mutation.ApplyGraphViewChange(m_Document, change);
        }
    }

    sealed class PoseGraphDocumentAdapter : IGraphAuthoringDocument
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphDocumentAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public string DomainId => "character-presentation-pose-graph";
        public string DocumentId => m_Window.CurrentGraph?.GraphId ?? string.Empty;
        public string DisplayName => m_Window.CurrentDisplayName;
        public string ContentRevision => m_Window.CurrentGraph?.ContentRevision ?? string.Empty;
        public UnityEngine.Object SerializedOwner => m_Window.CurrentOwner;
    }

    sealed class PoseGraphNodeCatalogAdapter : IGraphAuthoringNodeCatalog
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphNodeCatalogAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public IReadOnlyList<GraphAuthoringNodeCatalogEntry> GetEntries(IGraphAuthoringDocument document)
        {
            var kinds = new List<CharacterPoseNodeKind>
            {
                CharacterPoseNodeKind.BlendPose,
                CharacterPoseNodeKind.LayeredBoneBlend,
                CharacterPoseNodeKind.AdditivePose,
                CharacterPoseNodeKind.PoseParameterResolve,
                CharacterPoseNodeKind.Inertialization,
                CharacterPoseNodeKind.ModifyBone,
                CharacterPoseNodeKind.FootPlacement,
                CharacterPoseNodeKind.PoseSubgraph
            };
            if (!m_Window.IsSubgraphDocument)
            {
                kinds.InsertRange(0, new[]
                {
                    CharacterPoseNodeKind.AnimationSelectionInput,
                    CharacterPoseNodeKind.MotionMatchingSelectionInput,
                    CharacterPoseNodeKind.ProgramParameterInput,
                    CharacterPoseNodeKind.MarkerSync,
                    CharacterPoseNodeKind.SelectedPosePlayer,
                    CharacterPoseNodeKind.BlendSpacePlayer,
                    CharacterPoseNodeKind.BlendStack
                });
                kinds.Add(CharacterPoseNodeKind.OutputPose);
            }
            return kinds.Select(kind => new GraphAuthoringNodeCatalogEntry("Pose/" + PoseGraphDisplayNames.Node(kind), kind.ToString())).ToArray();
        }
    }

    sealed class PoseGraphPortPolicyAdapter : IGraphAuthoringPortPolicy
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphPortPolicyAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public bool CanConnect(IGraphAuthoringDocument document, Port startPort, Port endPort)
        {
            if (startPort == null || endPort == null || startPort.node == endPort.node || startPort.direction == endPort.direction)
                return false;
            CharacterPosePortDefinition start = startPort.userData as CharacterPosePortDefinition;
            CharacterPosePortDefinition end = endPort.userData as CharacterPosePortDefinition;
            if (start == null || end == null || start.Kind != end.Kind)
                return false;
            Port input = startPort.direction == Direction.Input ? startPort : endPort;
            if (input.connections.Any())
                return false;
            PoseGraphNodeView source = (startPort.direction == Direction.Output ? startPort.node : endPort.node) as PoseGraphNodeView;
            PoseGraphNodeView target = (startPort.direction == Direction.Input ? startPort.node : endPort.node) as PoseGraphNodeView;
            return source != null && target != null && !WouldCreateCycle(source.Node.NodeId, target.Node.NodeId);
        }

        bool WouldCreateCycle(PoseNodeId source, PoseNodeId target)
        {
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
                return true;
            var stack = new Stack<PoseNodeId>();
            var visited = new HashSet<PoseNodeId>();
            stack.Push(target);
            while (stack.Count > 0)
            {
                PoseNodeId current = stack.Pop();
                if (!visited.Add(current))
                    continue;
                if (current.Equals(source))
                    return true;
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    CharacterPoseEdge edge = graph.Edges[i];
                    if (edge != null && edge.SourceNodeId.Equals(current))
                        stack.Push(edge.TargetNodeId);
                }
            }
            return false;
        }
    }

    sealed class PoseGraphMutationAdapter : IGraphAuthoringMutationAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;

        public PoseGraphMutationAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public bool ReadOnly => false;

        public void CreateNode(IGraphAuthoringDocument document, string typeId, Vector2 graphPosition)
        {
            if (!Enum.TryParse(typeId, out CharacterPoseNodeKind kind) || !IsAllowed(kind))
                throw new InvalidOperationException($"Pose node type '{typeId}' is unknown.");
            CharacterPresentationPoseGraphAuthoringService.CreateNode(m_Window.CurrentOwner, m_Window.CurrentGraph, kind, graphPosition);
            m_Window.RefreshGraphView();
        }

        public GraphViewChange ApplyGraphViewChange(IGraphAuthoringDocument document, GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                for (int edgeIndex = 0; edgeIndex < change.edgesToCreate.Count; edgeIndex++)
                {
                    Edge edge = change.edgesToCreate[edgeIndex];
                    CharacterPoseNodeDefinition sourceNode = (edge.output.node as PoseGraphNodeView)?.Node;
                    CharacterPoseNodeDefinition targetNode = (edge.input.node as PoseGraphNodeView)?.Node;
                    CharacterPosePortDefinition sourcePort = edge.output.userData as CharacterPosePortDefinition;
                    CharacterPosePortDefinition targetPort = edge.input.userData as CharacterPosePortDefinition;
                    if (sourceNode == null || targetNode == null || sourcePort == null || targetPort == null)
                        continue;
                    edge.userData = CharacterPresentationPoseGraphAuthoringService.Connect(
                        m_Window.CurrentOwner,
                        m_Window.CurrentGraph,
                        sourceNode.NodeId,
                        sourcePort.PortId,
                        targetNode.NodeId,
                        targetPort.PortId);
                }
            }
            if (change.elementsToRemove != null)
            {
                PoseNodeId[] removedNodes = change.elementsToRemove.OfType<PoseGraphNodeView>()
                    .Select(view => view.Node.NodeId)
                    .ToArray();
                var nodeSet = new HashSet<PoseNodeId>(removedNodes);
                string[] removedEdges = change.elementsToRemove.OfType<Edge>()
                    .Select(edge => edge.userData as CharacterPoseEdge)
                    .Where(edge => edge != null && !nodeSet.Contains(edge.SourceNodeId) && !nodeSet.Contains(edge.TargetNodeId))
                    .Select(edge => edge.EdgeId)
                    .ToArray();
                CharacterPresentationPoseGraphAuthoringService.DeleteSelection(
                    m_Window.CurrentOwner,
                    m_Window.CurrentGraph,
                    removedNodes,
                    removedEdges);
            }
            if (change.movedElements != null)
            {
                Dictionary<PoseNodeId, Vector2> positions = change.movedElements
                    .OfType<PoseGraphNodeView>()
                    .ToDictionary(view => view.Node.NodeId, view => view.GetPosition().position);
                CharacterPresentationPoseGraphAuthoringService.MoveNodes(
                    m_Window.CurrentOwner,
                    m_Window.CurrentGraph,
                    positions);
            }
            m_Window.NotifyDocumentMutated();
            return change;
        }

        public string SerializeSelection(IGraphAuthoringDocument document, IEnumerable<GraphElement> elements)
        {
            PoseGraphNodeView[] views = elements?.OfType<PoseGraphNodeView>()
                .Where(view => view.Node.Kind != CharacterPoseNodeKind.GraphInput && view.Node.Kind != CharacterPoseNodeKind.GraphOutput)
                .ToArray() ?? Array.Empty<PoseGraphNodeView>();
            var ids = new HashSet<PoseNodeId>(views.Select(view => view.Node.NodeId));
            Vector2 center = views.Length == 0 ? Vector2.zero : views.Aggregate(Vector2.zero, (value, view) => value + view.Node.Position) / views.Length;
            return JsonUtility.ToJson(new PoseGraphClipboardPayload
            {
                nodes = views.Select(view => view.Node).ToArray(),
                edges = m_Window.CurrentGraph.Edges.Where(edge => edge != null && ids.Contains(edge.SourceNodeId) && ids.Contains(edge.TargetNodeId)).ToArray(),
                center = center
            });
        }

        public bool CanPaste(IGraphAuthoringDocument document, string payload)
        {
            try
            {
                PoseGraphClipboardPayload data = JsonUtility.FromJson<PoseGraphClipboardPayload>(payload);
                return data != null && data.nodes != null && data.nodes.Length > 0 &&
                       data.nodes.All(node => node != null && IsAllowed(node.Kind));
            }
            catch
            {
                return false;
            }
        }

        public void Paste(IGraphAuthoringDocument document, string operationName, string payload)
        {
            if (!CanPaste(document, payload))
                throw new InvalidOperationException("Pose Graph clipboard payload is not valid for the current document.");
            PoseGraphClipboardPayload data = JsonUtility.FromJson<PoseGraphClipboardPayload>(payload)
                ?? throw new InvalidOperationException("Pose Graph clipboard payload is invalid.");
            Vector2 offset = m_Window.GraphView.LocalMousePosition - data.center;
            CharacterPoseNodeDefinition[] nodes = CharacterPresentationPoseGraphAuthoringService.CloneNodesWithNewIdentities(
                data.nodes,
                offset,
                out Dictionary<PoseNodeId, PoseNodeId> nodeMap,
                out Dictionary<string, PosePortId> portMap);
            CharacterPoseEdge[] edges = CharacterPresentationPoseGraphAuthoringService.CloneInternalEdges(data.edges, nodeMap, portMap);
            CharacterPresentationPoseGraphAuthoringService.AppendClonedSelection(m_Window.CurrentOwner, m_Window.CurrentGraph, nodes, edges);
            m_Window.RefreshGraphView();
        }

        public void Reload(IGraphAuthoringDocument document)
        {
            m_Window.ReloadAfterUndo();
        }

        bool IsAllowed(CharacterPoseNodeKind kind)
        {
            if (m_Window.IsSubgraphDocument)
            {
                return kind == CharacterPoseNodeKind.LayeredBoneBlend ||
                       kind == CharacterPoseNodeKind.BlendPose ||
                       kind == CharacterPoseNodeKind.AdditivePose ||
                       kind == CharacterPoseNodeKind.PoseParameterResolve ||
                       kind == CharacterPoseNodeKind.Inertialization ||
                       kind == CharacterPoseNodeKind.ModifyBone ||
                       kind == CharacterPoseNodeKind.FootPlacement ||
                       kind == CharacterPoseNodeKind.PoseSubgraph;
            }
            return kind == CharacterPoseNodeKind.AnimationSelectionInput ||
                   kind == CharacterPoseNodeKind.MotionMatchingSelectionInput ||
                   kind == CharacterPoseNodeKind.ProgramParameterInput ||
                   kind == CharacterPoseNodeKind.MarkerSync ||
                   kind == CharacterPoseNodeKind.SelectedPosePlayer ||
                   kind == CharacterPoseNodeKind.BlendSpacePlayer ||
                   kind == CharacterPoseNodeKind.BlendStack ||
                   kind == CharacterPoseNodeKind.Inertialization ||
                   kind == CharacterPoseNodeKind.BlendPose ||
                   kind == CharacterPoseNodeKind.LayeredBoneBlend ||
                   kind == CharacterPoseNodeKind.AdditivePose ||
                   kind == CharacterPoseNodeKind.PoseParameterResolve ||
                   kind == CharacterPoseNodeKind.ModifyBone ||
                   kind == CharacterPoseNodeKind.FootPlacement ||
                   kind == CharacterPoseNodeKind.PoseSubgraph ||
                   kind == CharacterPoseNodeKind.OutputPose;
        }

        public void RenameNode(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, string displayName)
        {
            CharacterPresentationPoseGraphAuthoringService.RenameNode(m_Window.CurrentOwner, graph, node.NodeId, displayName);
            m_Window.RefreshGraphView();
        }

        public void ConfigureNode(
            CharacterPoseGraphData graph,
            CharacterPoseNodeDefinition node,
            PoseGraphNodeConfiguration configuration)
        {
            CharacterPresentationPoseGraphAuthoringService.ConfigureNode(
                m_Window.CurrentOwner,
                graph,
                node.NodeId,
                configuration.AnimationChannelId,
                configuration.ProgramProducerId,
                configuration.ParameterId,
                configuration.SelectionAvailability,
                configuration.BlendSpaceInputRangePolicy,
                configuration.BlendPolicy,
                configuration.InertializationPolicy,
                configuration.BoneMask,
                configuration.Weight,
                configuration.ParameterPolicies,
                configuration.AdditiveReferencePoseId,
                configuration.AdditiveReferenceSpace,
                configuration.AdditiveScalePolicy,
                configuration.BoneId,
                configuration.ModifyBoneReferenceSpace,
                configuration.ModifyBoneOperations,
                configuration.ModifyPosition,
                configuration.ModifyRotationEuler,
                configuration.ModifyScale,
                configuration.FootPlacementProfile,
                configuration.FootPlacementCalibration);
            m_Window.RefreshGraphView();
        }

        public void CreateInline(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node)
        {
            CharacterPresentationPoseGraphAuthoringService.CreateInline(m_Window.CurrentOwner, graph, node.NodeId);
            m_Window.RefreshGraphView();
        }

        public void ExtractShared(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, string assetPath)
        {
            CharacterPresentationPoseGraphAuthoringService.ExtractShared(m_Window.CurrentOwner, graph, node.NodeId, assetPath);
            m_Window.RefreshGraphView();
        }

        public void UseShared(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node, CharacterPresentationPoseGraphAsset asset)
        {
            CharacterPresentationPoseGraphAuthoringService.UseShared(m_Window.CurrentOwner, graph, node.NodeId, asset);
            m_Window.RefreshGraphView();
        }

        public void ClearShared(CharacterPoseGraphData graph, CharacterPoseNodeDefinition node)
        {
            CharacterPresentationPoseGraphAuthoringService.ClearShared(m_Window.CurrentOwner, graph, node.NodeId);
            m_Window.RefreshGraphView();
        }
    }

    sealed class PoseGraphNavigatorAdapter : IGraphAuthoringWorkspaceRegionAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly VisualElement m_Root = new VisualElement();
        readonly ToolbarSearchField m_Search = new ToolbarSearchField();
        readonly ScrollView m_Content = new ScrollView();

        public PoseGraphNavigatorAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window ?? throw new ArgumentNullException(nameof(window));
            m_Search.SetValueWithoutNotify(m_Window.NavigatorSearch);
            m_Search.RegisterValueChangedCallback(evt =>
            {
                m_Window.NavigatorSearch = evt.newValue ?? string.Empty;
                Rebuild();
            });
            m_Root.style.flexGrow = 1f;
            m_Content.style.flexGrow = 1f;
            m_Root.Add(m_Search);
            m_Root.Add(m_Content);
        }

        public VisualElement View => m_Root;

        public void Bind(IGraphAuthoringDocument document) => Rebuild();
        public void Refresh() => Rebuild();
        public void Clear() => m_Content.Clear();

        void Rebuild()
        {
            m_Content.Clear();
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
            {
                m_Content.Add(new HelpBox("Pose Graph document is unavailable.", HelpBoxMessageType.Error));
                return;
            }
            AddHeader("Graphs");
            AddEntry(m_Window.CurrentDisplayName, $"{graph.GraphId} @ {graph.ContentRevision}");
            AddGraphEntries(graph);

            AddHeader("Inputs");
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[i];
                if (node == null || !Matches(node.DisplayName, node.NodeId.Value, node.Kind.ToString()))
                    continue;
                if (node.Kind == CharacterPoseNodeKind.AnimationSelectionInput)
                    AddEntry(NodeLabel(node), $"Animation Channel {node.AnimationChannelId}");
                else if (node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput)
                    AddEntry(NodeLabel(node), $"Motion Matching {node.ProgramProducerId} / {node.AnimationChannelId}");
                else if (node.Kind == CharacterPoseNodeKind.ProgramParameterInput)
                    AddEntry(NodeLabel(node), $"Animation Parameter {node.ParameterId}");
            }

            AddHeader("References");
            AddAssetEntry("Presentation Profile", m_Window.ProfileContext);
            AddAssetEntry("Rig Definition", m_Window.RigDefinition);
            AddNodeReferences(graph);

            AddHeader("Reachable Producers");
            CharacterPipelineDefinition definition = m_Window.DefinitionContext;
            CharacterAnimationPresentationProfile profile = m_Window.ProfileContext;
            if (!definition || !profile)
            {
                m_Content.Add(new HelpBox(
                    "Unavailable: open this Pose Graph from an Animation Presentation Profile with one explicit Character Definition context.",
                    HelpBoxMessageType.Info));
                return;
            }
            try
            {
                IReadOnlyList<AnimationProducerAuthoringEntry> producers =
                    CharacterAnimationPresentationAuthoringService.DiscoverProducers(profile, definition);
                foreach (IGrouping<ThirdPersonSimulation.AnimationChannelId, AnimationProducerAuthoringEntry> group in
                         producers.GroupBy(value => value.AnimationChannelId).OrderBy(value => value.Key.Value, StringComparer.Ordinal))
                {
                    AddSubheader($"Animation Channel {group.Key}");
                    foreach (AnimationProducerAuthoringEntry producer in group.OrderBy(value => value.ProgramProducerIdentity, StringComparer.Ordinal))
                    {
                        if (!Matches(producer.DisplayName, producer.ProgramProducerIdentity, producer.AnimationChannelId.Value))
                            continue;
                        AnimationProducerPresentationBinding binding = profile.FindProducerBinding(producer.ProducerId);
                        string source = binding == null
                            ? "Binding Missing"
                            : binding.SourceKind.ToString();
                        var button = new Button(() => RuntimeDebugSourceNavigator.Open(
                            definition,
                            RuntimeSourceElementKey.Track(
                                producer.ProducerId.TimelineAuthoringId,
                                producer.ProducerId.TrackAuthoringId)))
                        {
                            text = producer.DisplayName,
                            tooltip = $"{producer.ProgramProducerIdentity}\n{source}\nOpen the exact Timeline Track owner."
                        };
                        m_Content.Add(button);
                        AddEntry(producer.ProgramProducerIdentity, $"{source} / {producer.SourceClips.Count} clips");
                    }
                }
            }
            catch (Exception exception)
            {
                m_Content.Add(new HelpBox(exception.Message, HelpBoxMessageType.Error));
            }
        }

        void AddGraphEntries(CharacterPoseGraphData graph)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[i];
                if (node == null || node.Kind != CharacterPoseNodeKind.PoseSubgraph || node.Subgraph == null || !node.Subgraph.IsExclusive)
                    continue;
                CharacterPoseGraphData child = node.Subgraph.HasInline ? node.Subgraph.Inline : node.Subgraph.Shared.Graph;
                if (Matches(NodeLabel(node), node.NodeId.Value, child.GraphId))
                    AddEntry(NodeLabel(node), $"Pose Subgraph {child.GraphId} @ {child.ContentRevision}");
            }
        }

        void AddNodeReferences(CharacterPoseGraphData graph)
        {
            var assets = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[i];
                if (node == null)
                    continue;
                AddAsset(node.BlendPolicy);
                AddAsset(node.InertializationPolicy);
                AddAsset(node.BoneMask);
                AddAsset(node.FootPlacementProfile);
                AddAsset(node.FootPlacementCalibration);
            }
            void AddAsset(UnityEngine.Object asset)
            {
                if (asset && assets.Add(asset))
                    AddAssetEntry(asset.GetType().Name, asset);
            }
        }

        void AddAssetEntry(string label, UnityEngine.Object asset)
        {
            if (!asset || !Matches(label, asset.name, AssetDatabase.GetAssetPath(asset)))
                return;
            var button = new Button(() =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            })
            {
                text = $"{label}: {asset.name}",
                tooltip = AssetDatabase.GetAssetPath(asset)
            };
            m_Content.Add(button);
        }

        void AddHeader(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 8f;
            m_Content.Add(label);
        }

        void AddSubheader(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginLeft = 4f;
            label.style.marginTop = 4f;
            m_Content.Add(label);
        }

        void AddEntry(string title, string detail)
        {
            if (!Matches(title, detail))
                return;
            var row = new VisualElement();
            row.style.marginLeft = 6f;
            row.Add(new Label(title));
            var metadata = new Label(detail);
            metadata.style.color = new Color(0.58f, 0.58f, 0.58f);
            row.Add(metadata);
            m_Content.Add(row);
        }

        bool Matches(params string[] values)
        {
            string query = m_Search.value?.Trim();
            if (string.IsNullOrEmpty(query))
                return true;
            for (int i = 0; i < values.Length; i++)
            {
                if (!string.IsNullOrEmpty(values[i]) && values[i].IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        static string NodeLabel(CharacterPoseNodeDefinition node) =>
            string.IsNullOrWhiteSpace(node.DisplayName) ? node.NodeId.Value : node.DisplayName;
    }

    sealed class PoseGraphBottomDockAdapter : IGraphAuthoringWorkspaceRegionAdapter, IGraphAuthoringWorkspacePageAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly VisualElement m_Root = new VisualElement();
        readonly VisualElement m_PreviewPage = new VisualElement();
        readonly ScrollView m_DiagnosticsPage = new ScrollView();
        readonly ScrollView m_SyncPage = new ScrollView();
        readonly ScrollView m_PoseWatchPage = new ScrollView();
        readonly ObjectField m_TargetField = new ObjectField("Preview Target");
        readonly PopupField<string> m_ProducerField = new PopupField<string>("Producer", new List<string> { "Unavailable" }, 0);
        readonly FloatField m_TimeField = new FloatField("Seek Time");
        readonly Label m_Status = new Label();
        readonly Label m_PreviewTargetSummary = new Label();
        readonly Label m_PreviewRevisionSummary = new Label();
        readonly Label m_PreviewPlaybackSummary = new Label();
        readonly Label m_PreviewWorldAwareSummary = new Label();
        readonly PosePreviewViewportElement m_PreviewViewport;
        readonly List<AnimationProducerAuthoringEntry> m_Producers = new List<AnimationProducerAuthoringEntry>();
        CharacterPipelineHost m_Target;
        Guid m_SessionId;
        bool m_Playing;
        bool m_Bound;
        ulong m_Tick;
        float m_Time;
        double m_LastUpdate;
        double m_NextReadOnlyRefresh;
        string m_ActivePageId = "preview";
        readonly Guid m_PoseWatchOwnerId = Guid.NewGuid();
        AnimationPresentationRuntimeTarget m_PoseWatchRuntimeTarget;
        string m_PoseWatchError = string.Empty;

        public PoseGraphBottomDockAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window ?? throw new ArgumentNullException(nameof(window));
            m_PreviewViewport = new PosePreviewViewportElement(m_Window.PreviewViewportState, m_Window.Repaint);
            m_TargetField.objectType = typeof(CharacterPipelineHost);
            m_TargetField.allowSceneObjects = true;
            m_TargetField.RegisterValueChangedCallback(evt => SetTarget(evt.newValue as CharacterPipelineHost));
            m_ProducerField.RegisterValueChangedCallback(_ => StopPreview("Producer changed. Press Play to start a new explicit session."));
            m_TimeField.isDelayed = true;
            m_TimeField.RegisterValueChangedCallback(evt => m_Time = Math.Max(0f, evt.newValue));
            var tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.Add(new Button(() => ShowPage("preview", m_PreviewPage)) { text = "Preview" });
            tabs.Add(new Button(() => ShowPage("diagnostics", m_DiagnosticsPage)) { text = "Diagnostics" });
            tabs.Add(new Button(() => ShowPage("sync", m_SyncPage)) { text = "Sync" });
            tabs.Add(new Button(() => ShowPage("pose-watch", m_PoseWatchPage)) { text = "Pose Watch" });
            var controls = new VisualElement();
            controls.style.flexDirection = FlexDirection.Row;
            controls.Add(new Button(Play) { text = "Play" });
            controls.Add(new Button(Pause) { text = "Pause" });
            controls.Add(new Button(Step) { text = "Step" });
            controls.Add(new Button(Seek) { text = "Seek" });
            controls.Add(new Button(Reset) { text = "Reset" });
            var viewportOptions = new VisualElement();
            viewportOptions.style.flexDirection = FlexDirection.Row;
            viewportOptions.Add(CreateViewportToggle("Grid", m_Window.PreviewViewportState.showGrid, value => m_Window.PreviewViewportState.showGrid = value));
            viewportOptions.Add(CreateViewportToggle("Skeleton", m_Window.PreviewViewportState.showSkeleton, value => m_Window.PreviewViewportState.showSkeleton = value));
            viewportOptions.Add(CreateViewportToggle("Root Trajectory", m_Window.PreviewViewportState.showRootTrajectory, value => m_Window.PreviewViewportState.showRootTrajectory = value));
            viewportOptions.Add(CreateViewportToggle("Foot Support / IK", m_Window.PreviewViewportState.showFootPlacement, value => m_Window.PreviewViewportState.showFootPlacement = value));
            m_Root.style.flexGrow = 1f;
            m_PreviewPage.style.flexGrow = 1f;
            m_DiagnosticsPage.style.flexGrow = 1f;
            m_SyncPage.style.flexGrow = 1f;
            m_PoseWatchPage.style.flexGrow = 1f;
            m_PreviewPage.Add(m_TargetField);
            m_PreviewPage.Add(m_ProducerField);
            m_PreviewPage.Add(m_TimeField);
            m_PreviewPage.Add(controls);
            m_PreviewPage.Add(viewportOptions);
            m_PreviewPage.Add(m_PreviewTargetSummary);
            m_PreviewPage.Add(m_PreviewRevisionSummary);
            m_PreviewPage.Add(m_PreviewPlaybackSummary);
            m_PreviewPage.Add(m_PreviewWorldAwareSummary);
            m_PreviewPage.Add(m_Status);
            m_PreviewPage.Add(m_PreviewViewport);
            m_Root.Add(tabs);
            m_Root.Add(m_PreviewPage);
            m_Root.Add(m_DiagnosticsPage);
            m_Root.Add(m_SyncPage);
            m_Root.Add(m_PoseWatchPage);
            ShowPage("preview", m_PreviewPage);
        }

        public VisualElement View => m_Root;
        public string ActivePageId => m_ActivePageId;

        public void RestorePage(string pageId)
        {
            switch (pageId)
            {
                case "diagnostics":
                    ShowPage("diagnostics", m_DiagnosticsPage);
                    break;
                case "sync":
                    ShowPage("sync", m_SyncPage);
                    break;
                case "pose-watch":
                    ShowPage("pose-watch", m_PoseWatchPage);
                    break;
                default:
                    ShowPage("preview", m_PreviewPage);
                    break;
            }
        }

        public void Bind(IGraphAuthoringDocument document)
        {
            RebuildProducers();
            if (!m_Bound)
            {
                EditorApplication.update += Update;
                m_Bound = true;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (!m_Playing)
                m_Status.text = ResolvePreviewState();
            DrawPreviewSummary();
            DrawDiagnostics();
            DrawSync();
            DrawPoseWatch();
        }

        public void Clear()
        {
            ReleasePoseWatchInterests();
            StopPreview(string.Empty);
            m_PreviewViewport.ReleaseCamera();
            if (m_Bound)
            {
                EditorApplication.update -= Update;
                m_Bound = false;
            }
        }

        public void Invalidate()
        {
            StopPreview("Stale: authoring changed. Run the explicit Build before Preview.");
        }

        public void SynchronizePoseWatchInterests()
        {
            var interests = new List<AnimationPoseWatchIdentity>();
            IReadOnlyList<PoseGraphWatchViewState> states = m_Window.PoseWatchStates;
            for (int i = 0; i < states.Count; i++)
            {
                try
                {
                    interests.Add(states[i].ToIdentity());
                }
                catch (ArgumentException)
                {
                    m_PoseWatchError = "Stale: one Pose Watch identity no longer matches the open document.";
                    continue;
                }
            }
            try
            {
                if (m_Target && m_SessionId != Guid.Empty)
                    m_Target.TrySetPreviewPoseWatchInterests(m_SessionId, m_PoseWatchOwnerId, interests);
                AnimationPresentationRuntimeTarget runtimeTarget = ResolveRuntimeTarget();
                if (!ReferenceEquals(runtimeTarget, m_PoseWatchRuntimeTarget))
                {
                    m_PoseWatchRuntimeTarget?.RemovePoseWatchInterests(m_PoseWatchOwnerId);
                    m_PoseWatchRuntimeTarget = runtimeTarget;
                }
                m_PoseWatchRuntimeTarget?.SetPoseWatchInterests(m_PoseWatchOwnerId, interests);
                m_PoseWatchError = string.Empty;
            }
            catch (Exception exception)
            {
                m_PoseWatchError = exception.Message;
            }
        }

        public void RefreshPoseWatchPanel() => DrawPoseWatch();
        public void FocusPoseWatch(PoseGraphWatchViewState state)
        {
            if (state == null)
                return;
            int index = -1;
            for (int i = 0; i < m_Window.PoseWatchStates.Count; i++)
            {
                if (!ReferenceEquals(m_Window.PoseWatchStates[i], state))
                    continue;
                index = i;
                break;
            }
            if (index < 0)
                return;
            ShowPage("pose-watch", m_PoseWatchPage);
            string elementName = $"pose-watch-entry-{index}";
            m_PoseWatchPage.schedule.Execute(() =>
            {
                VisualElement entry = m_PoseWatchPage.Q(elementName);
                if (entry != null)
                    m_PoseWatchPage.ScrollTo(entry);
            });
        }

        public void ReportPoseWatchError(string message)
        {
            m_PoseWatchError = message ?? string.Empty;
            DrawPoseWatch();
        }

        public void SetBuilding(bool building)
        {
            StopPreview(building ? "Building: explicit Character Definition build is running." : string.Empty);
            if (!building)
                Refresh();
        }

        public void Report(string message)
        {
            m_Status.text = message ?? string.Empty;
        }

        void RebuildProducers()
        {
            m_Producers.Clear();
            var choices = new List<string>();
            if (m_Window.DefinitionContext && m_Window.ProfileContext)
            {
                try
                {
                    m_Producers.AddRange(CharacterAnimationPresentationAuthoringService.DiscoverProducers(
                        m_Window.ProfileContext,
                        m_Window.DefinitionContext));
                    choices.AddRange(m_Producers.Select(value => value.DisplayName));
                }
                catch (Exception exception)
                {
                    m_Status.text = exception.Message;
                }
            }
            if (choices.Count == 0)
                choices.Add("Unavailable");
            m_ProducerField.choices = choices;
            m_ProducerField.index = 0;
        }

        void SetTarget(CharacterPipelineHost target)
        {
            if (ReferenceEquals(m_Target, target))
                return;
            if (m_Target)
                m_Target.RemovePreviewPoseWatchInterests(m_PoseWatchOwnerId);
            StopPreview(string.Empty);
            m_Target = target;
            m_PreviewViewport.SetTarget(target);
            Refresh();
        }

        void Play()
        {
            if (!TryGetContext(out CharacterPipelineHost target, out AnimationProducerAuthoringEntry producer, out string error))
            {
                m_Status.text = error;
                return;
            }
            if (m_SessionId == Guid.Empty)
                m_SessionId = Guid.NewGuid();
            m_Playing = true;
            m_LastUpdate = EditorApplication.timeSinceStartup;
            Evaluate(target, producer, 0f, false);
            SynchronizePoseWatchInterests();
        }

        void Pause()
        {
            m_Playing = false;
            m_Status.text = m_SessionId == Guid.Empty ? ResolvePreviewState() : $"Paused at {m_Time:0.###}s.";
        }

        void Step()
        {
            if (!TryGetContext(out CharacterPipelineHost target, out AnimationProducerAuthoringEntry producer, out string error))
            {
                m_Status.text = error;
                return;
            }
            if (m_SessionId == Guid.Empty)
                m_SessionId = Guid.NewGuid();
            m_Playing = false;
            Evaluate(target, producer, 1f / 60f, false);
        }

        void Seek()
        {
            if (!TryGetContext(out CharacterPipelineHost target, out AnimationProducerAuthoringEntry producer, out string error))
            {
                m_Status.text = error;
                return;
            }
            if (m_SessionId == Guid.Empty)
                m_SessionId = Guid.NewGuid();
            m_Playing = false;
            Evaluate(target, producer, 0f, true);
        }

        void Reset()
        {
            StopPreview(string.Empty);
            m_Time = 0f;
            m_TimeField.SetValueWithoutNotify(0f);
            m_PreviewViewport.ClearTrajectory();
            Refresh();
        }

        void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now >= m_NextReadOnlyRefresh)
            {
                m_NextReadOnlyRefresh = now + 0.1d;
                if (m_ActivePageId == "preview")
                {
                    DrawPreviewSummary();
                    m_PreviewViewport.MarkDirtyRepaint();
                }
                else if (m_ActivePageId == "sync")
                    DrawSync();
                else if (m_ActivePageId == "pose-watch")
                {
                    SynchronizePoseWatchInterests();
                    DrawPoseWatch();
                }
            }
            if (!m_Playing)
                return;
            float delta = Mathf.Clamp((float)(now - m_LastUpdate), 0f, 0.1f);
            m_LastUpdate = now;
            if (!TryGetContext(out CharacterPipelineHost target, out AnimationProducerAuthoringEntry producer, out string error))
            {
                StopPreview(error);
                return;
            }
            Evaluate(target, producer, delta, false);
        }

        void Evaluate(
            CharacterPipelineHost target,
            AnimationProducerAuthoringEntry producer,
            float delta,
            bool reset)
        {
            float previous = m_Time;
            if (!reset)
                m_Time += Math.Max(0f, delta);
            bool blendSpace = TryResolveBlendSpacePreview(
                producer,
                out CharacterAnimationBlendSpaceAsset blendSpaceAsset,
                out float blendSpaceDuration);
            float duration = blendSpace ? blendSpaceDuration : producer.Timeline.Timeline.Duration;
            if (duration > 0f)
                m_Time = Mathf.Repeat(m_Time, duration);
            m_TimeField.SetValueWithoutNotify(m_Time);
            if (blendSpace)
            {
                target.EvaluateBlendSpacePreview(
                    m_SessionId,
                    producer.Timeline.Timeline,
                    producer.Track.AuthoringId,
                    previous,
                    m_Time,
                    producer.ProgramProducerIdentity,
                    producer.DisplayName,
                    ++m_Tick,
                    Math.Max(0f, delta),
                    reset,
                    blendSpaceAsset.Preview.Parameter);
            }
            else
            {
                target.EvaluateTimelinePreview(
                    m_SessionId,
                    producer.Timeline.Timeline,
                    previous,
                    m_Time,
                    producer.ProgramProducerIdentity,
                    producer.DisplayName,
                    ++m_Tick,
                    Math.Max(0f, delta),
                    reset);
            }
            SynchronizePoseWatchInterests();
            m_PreviewViewport.RecordRoot();
            CharacterPosePlanStageSnapshot stages = target.PreviewPosePlanStages;
            m_Status.text = target.HasPreviewAnimationRuntimeSnapshot
                ? $"{(m_Playing ? "Playing" : "Paused")} {m_Time:0.###}s / Completion {target.PreviewAnimationRuntimeSnapshot.CompletionIdentity} / Pose {stages.ComposedAvailability} / Final {stages.Final.Status}"
                : $"Preview evaluated at {m_Time:0.###}s; runtime snapshot unavailable.";
            DrawPreviewSummary();
            m_PreviewViewport.MarkDirtyRepaint();
            if (m_ActivePageId == "diagnostics")
                DrawDiagnostics();
            else if (m_ActivePageId == "sync")
                DrawSync();
            else if (m_ActivePageId == "pose-watch")
                DrawPoseWatch();
        }

        void ShowPage(string pageId, VisualElement page)
        {
            m_ActivePageId = pageId;
            m_PreviewPage.style.display = ReferenceEquals(page, m_PreviewPage) ? DisplayStyle.Flex : DisplayStyle.None;
            m_DiagnosticsPage.style.display = ReferenceEquals(page, m_DiagnosticsPage) ? DisplayStyle.Flex : DisplayStyle.None;
            m_SyncPage.style.display = ReferenceEquals(page, m_SyncPage) ? DisplayStyle.Flex : DisplayStyle.None;
            m_PoseWatchPage.style.display = ReferenceEquals(page, m_PoseWatchPage) ? DisplayStyle.Flex : DisplayStyle.None;
            if (pageId == "preview")
            {
                DrawPreviewSummary();
                m_PreviewViewport.MarkDirtyRepaint();
            }
            else if (pageId == "diagnostics")
                DrawDiagnostics();
            else if (pageId == "sync")
                DrawSync();
            else if (pageId == "pose-watch")
            {
                SynchronizePoseWatchInterests();
                DrawPoseWatch();
            }
        }

        Toggle CreateViewportToggle(string label, bool value, Action<bool> setValue)
        {
            var toggle = new Toggle(label) { value = value };
            toggle.RegisterValueChangedCallback(evt =>
            {
                setValue(evt.newValue);
                DrawPreviewSummary();
                m_PreviewViewport.MarkDirtyRepaint();
            });
            return toggle;
        }

        void DrawPreviewSummary()
        {
            m_PreviewTargetSummary.text = m_Target && m_Target.VisualRoot
                ? $"Target: {m_Target.name} / Scene {m_Target.gameObject.scene.path} / VisualRoot {m_Target.VisualRoot.name}"
                : "Target: Unavailable";
            m_PreviewRevisionSummary.text = $"PoseGraph Revision: {m_Window.CurrentGraph?.ContentRevision ?? "Unavailable"} / Projection Revision: {(string.IsNullOrEmpty(m_Window.ProjectionRevision) ? "Unavailable" : m_Window.ProjectionRevision)}";
            m_PreviewPlaybackSummary.text = $"Time: {m_Time:0.###}s / Frame: {m_Tick} / Speed: 1x / State: {(m_Playing ? "Playing" : m_SessionId == Guid.Empty ? "Stopped" : "Paused")}";
            if (!m_Target || !m_Target.HasPreviewAnimationRuntimeSnapshot)
            {
                m_PreviewWorldAwareSummary.text = "World-Aware: Unavailable / Foot support and IK goals: Unavailable";
                return;
            }
            CharacterPosePlanPhaseSnapshot worldAware = m_Target.PreviewPosePlanStages.WorldAware;
            string foot = m_Window.PreviewViewportState.showFootPlacement
                ? "Foot support and IK goals: Unavailable in authoring Preview"
                : "Foot support and IK goals: Hidden";
            m_PreviewWorldAwareSummary.text = $"World-Aware: {worldAware.Status} / Reason: {worldAware.UnavailableReason} / {foot}";
        }

        void DrawPoseWatch()
        {
            m_PoseWatchPage.Clear();
            IReadOnlyList<PoseGraphWatchViewState> states = m_Window.PoseWatchStates;
            AddBottomReadOnly(m_PoseWatchPage, "Capacity", $"{states.Count}/{AnimationPoseWatchCapacity.PerWindow} window / {AnimationPoseWatchCapacity.PerTarget} target");
            if (!string.IsNullOrEmpty(m_PoseWatchError))
                m_PoseWatchPage.Add(new HelpBox(m_PoseWatchError, HelpBoxMessageType.Error));
            if (states.Count == 0)
            {
                m_PoseWatchPage.Add(new HelpBox("Right-click a node with a Pose output and choose Pose Watch.", HelpBoxMessageType.Info));
                return;
            }
            bool hasSnapshot = TryGetPoseWatchSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string snapshotStatus);
            var viewportPoses = new List<PoseWatchViewportPose>();
            if (!hasSnapshot)
                m_PoseWatchPage.Add(new HelpBox(snapshotStatus, snapshotStatus.StartsWith("Stale", StringComparison.Ordinal) ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info));
            for (int stateIndex = 0; stateIndex < states.Count; stateIndex++)
            {
                PoseGraphWatchViewState state = states[stateIndex];
                AnimationPoseWatchIdentity identity;
                try
                {
                    identity = state.ToIdentity();
                }
                catch (ArgumentException)
                {
                    AddBottomReadOnly(m_PoseWatchPage, state.nodeId, "Stale identity");
                    continue;
                }
                var card = new VisualElement();
                card.name = $"pose-watch-entry-{stateIndex}";
                card.style.marginTop = 4f;
                card.style.paddingBottom = 4f;
                card.style.borderBottomWidth = 1f;
                card.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);
                var header = new VisualElement();
                header.style.flexDirection = FlexDirection.Row;
                var visible = new Toggle { value = state.visible, tooltip = "Show this read-only Pose in the Preview viewport." };
                visible.RegisterValueChangedCallback(evt =>
                {
                    state.visible = evt.newValue;
                    m_Root.schedule.Execute(DrawPoseWatch);
                });
                var color = new ColorField { value = state.color };
                color.style.width = 70f;
                color.RegisterValueChangedCallback(evt =>
                {
                    state.color = evt.newValue;
                    m_Root.schedule.Execute(DrawPoseWatch);
                });
                header.Add(visible);
                header.Add(color);
                header.Add(new Label($"{identity.NodeId} / {(string.IsNullOrEmpty(identity.CallSite) ? "Root" : identity.CallSite)}"));
                card.Add(header);
                AnimationPoseWatchSnapshot watch = default;
                bool found = false;
                int foundWatchIndex = -1;
                if (hasSnapshot)
                {
                    for (int watchIndex = 0; watchIndex < snapshot.PoseWatches.Count; watchIndex++)
                    {
                        AnimationPoseWatchSnapshot candidate = snapshot.PoseWatches[watchIndex];
                        if (!candidate.Identity.Equals(identity))
                            continue;
                        watch = candidate;
                        found = true;
                        foundWatchIndex = watchIndex;
                        break;
                    }
                }
                AddBottomReadOnly(
                    card,
                    "Availability",
                    found
                        ? $"{watch.Availability} / completion {watch.CompletionIdentity} / weight {watch.OutputWeight:0.###} / contributions {watch.ContributionCount}"
                        : hasSnapshot ? "Not published for this frame" : snapshotStatus);
                var boneFilter = new TextField("Bone Filter") { value = state.boneFilter ?? string.Empty, isDelayed = true };
                boneFilter.RegisterValueChangedCallback(evt =>
                {
                    state.boneFilter = evt.newValue ?? string.Empty;
                    m_Root.schedule.Execute(DrawPoseWatch);
                });
                card.Add(boneFilter);
                var commands = new VisualElement();
                commands.style.flexDirection = FlexDirection.Row;
                commands.Add(new Button(() => m_Window.GraphView.FocusNode(identity.NodeId)) { text = "Locate Node" });
                commands.Add(new Button(() => m_Window.RemovePoseWatch(state)) { text = "Remove" });
                card.Add(commands);
                m_PoseWatchPage.Add(card);
                if (found && state.visible && watch.Availability == AnimationPoseWatchAvailability.Pose)
                {
                    viewportPoses.Add(new PoseWatchViewportPose
                    {
                        Color = state.color,
                        Positions = CopyPoseWatchWorldPositions(snapshot, foundWatchIndex, m_Window.RigDefinition),
                        BoneFilter = state.boneFilter ?? string.Empty
                    });
                }
            }
            if (viewportPoses.Count > 0)
                m_PoseWatchPage.Insert(1, new PoseWatchViewportElement(m_Window.RigDefinition, viewportPoses));
        }

        static Vector3[] CopyPoseWatchWorldPositions(
            AnimationPresentationRuntimeSnapshot snapshot,
            int watchIndex,
            CharacterAnimationRigDefinition rig)
        {
            if (!rig)
                return Array.Empty<Vector3>();
            AnimationReadOnlyBuffer<AnimationLocalBonePose> local = snapshot.GetPoseWatchLocalPoses(watchIndex);
            if (local.Count != rig.Bones.Count)
                throw new InvalidOperationException("Pose Watch Bone count does not match the formal Rig.");
            var world = new Matrix4x4[local.Count];
            var positions = new Vector3[local.Count];
            for (int boneIndex = 0; boneIndex < local.Count; boneIndex++)
            {
                AnimationLocalBonePose pose = local[boneIndex];
                Matrix4x4 matrix = Matrix4x4.TRS(pose.Position, pose.Rotation, pose.Scale);
                int parentIndex = rig.Bones[boneIndex].ParentIndex;
                world[boneIndex] = parentIndex >= 0 ? world[parentIndex] * matrix : matrix;
                positions[boneIndex] = world[boneIndex].GetColumn(3);
            }
            return positions;
        }

        bool TryGetPoseWatchSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string status)
        {
            if (m_Target && m_SessionId != Guid.Empty && m_Target.HasPreviewAnimationRuntimeSnapshot)
            {
                snapshot = m_Target.PreviewAnimationRuntimeSnapshot;
                if (string.Equals(snapshot.PoseGraphId, m_Window.ValidationRoot?.Graph?.GraphId, StringComparison.Ordinal) &&
                    string.Equals(snapshot.PoseGraphRevision, m_Window.ValidationRoot?.Graph?.ContentRevision, StringComparison.Ordinal) &&
                    string.Equals(snapshot.ProjectionRevision, m_Window.ProjectionRevision, StringComparison.Ordinal))
                {
                    status = "Preview";
                    return true;
                }
                snapshot = default;
                status = "Stale: Preview Pose Graph or Projection revision does not match this document.";
                return false;
            }
            return TryGetRuntimeSnapshot(out snapshot, out status);
        }

        AnimationPresentationRuntimeTarget ResolveRuntimeTarget()
        {
            RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
            return viewModel.Attached && AnimationPresentationRuntimeTargetRegistry.TryGet(
                viewModel.Target.CharacterRuntimeId,
                out AnimationPresentationRuntimeTarget target)
                ? target
                : null;
        }

        void ReleasePoseWatchInterests()
        {
            if (m_Target)
                m_Target.RemovePreviewPoseWatchInterests(m_PoseWatchOwnerId);
            m_PoseWatchRuntimeTarget?.RemovePoseWatchInterests(m_PoseWatchOwnerId);
            m_PoseWatchRuntimeTarget = null;
        }

        void DrawDiagnostics()
        {
            m_DiagnosticsPage.Clear();
            CharacterPresentationPoseGraphAsset graphAsset = m_Window.ValidationRoot;
            CharacterAnimationRigDefinition rig = m_Window.RigDefinition;
            if (!graphAsset || graphAsset.Graph == null || !rig)
            {
                m_DiagnosticsPage.Add(new HelpBox("Unavailable: root Pose Graph and Rig context are required.", HelpBoxMessageType.Info));
                return;
            }
            CharacterPoseGraphValidationReport report = CharacterPresentationPoseGraphValidator.Validate(graphAsset, rig);
            AddBottomReadOnly(m_DiagnosticsPage, "Authoring", report.IsValid ? "Valid" : $"Invalid / {report.Issues.Count} issue(s)");
            AddBottomReadOnly(
                m_DiagnosticsPage,
                "Build",
                m_Window.ResolveWorkspaceBuildState());
            for (int i = 0; i < report.Issues.Count; i++)
            {
                CharacterPoseGraphValidationIssue issue = report.Issues[i];
                var button = new Button(() =>
                {
                    if (issue.NodeId.IsValid)
                        m_Window.GraphView.FocusNode(issue.NodeId);
                })
                {
                    text = $"{issue.Code}: {issue.Message}",
                    tooltip = $"Graph {issue.GraphId} / Node {issue.NodeId} / Port {issue.PortId}"
                };
                button.SetEnabled(issue.NodeId.IsValid);
                m_DiagnosticsPage.Add(button);
            }
            if (!TryGetRuntimeSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string status))
            {
                m_DiagnosticsPage.Add(new HelpBox(status, status.StartsWith("Stale", StringComparison.Ordinal) ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info));
                return;
            }
            AddBottomReadOnly(m_DiagnosticsPage, "Runtime Completion", snapshot.CompletionIdentity.ToString());
            AddBottomReadOnly(m_DiagnosticsPage, "Final Availability", $"{snapshot.FinalAvailability} / {snapshot.FinalAppliedAt}");
            for (int i = 0; i < snapshot.Operations.Count; i++)
            {
                AnimationPoseOperationSnapshot operation = snapshot.Operations[i];
                if (operation.InvalidReason == AnimationPoseNativeInvalidReason.None)
                    continue;
                var button = new Button(() => m_Window.GraphView.FocusNode(operation.NodeId))
                {
                    text = $"#{operation.OperationIndex} {operation.Code}: {operation.InvalidReason}",
                    tooltip = $"{operation.GraphId} / {operation.NodeId} / {operation.CallSite}"
                };
                m_DiagnosticsPage.Add(button);
            }
        }

        void DrawSync()
        {
            m_SyncPage.Clear();
            if (!TryGetSelectedProducer(out AnimationProducerAuthoringEntry producer))
            {
                m_SyncPage.Add(new HelpBox("Unavailable: select one reachable Timeline producer.", HelpBoxMessageType.Info));
                return;
            }
            AddProducerNavigation(m_SyncPage, "Open Target Timeline", producer);
            AddBottomReadOnly(m_SyncPage, "Target Producer", producer.ProgramProducerIdentity);
            AddBottomReadOnly(m_SyncPage, "Duration", $"{producer.Timeline.Timeline.Duration:0.###}s");
            AddMarkerRuler(m_SyncPage, "Target", producer, m_Time);
            if (!m_Target || m_SessionId == Guid.Empty ||
                !m_Target.TryGetAnimationMarkerSyncPreviewState(m_SessionId, producer.ProducerId.TrackAuthoringId, out TimelineAnimationMarkerSyncPreviewState state))
            {
                m_SyncPage.Add(new HelpBox("Unavailable: start an explicit Preview session to read the formal MarkerSync playback snapshot.", HelpBoxMessageType.Info));
                return;
            }
            AddBottomReadOnly(m_SyncPage, "Source Producer", string.IsNullOrEmpty(state.SourceProducerId) ? "No current source" : state.SourceProducerId);
            AddBottomReadOnly(m_SyncPage, "Target Producer Runtime", state.TargetProducerId);
            AddBottomReadOnly(m_SyncPage, "Animation Channel", state.AnimationChannelId.ToString());
            AddBottomReadOnly(m_SyncPage, "Sync Group", string.IsNullOrEmpty(state.SyncGroupId) ? "None" : state.SyncGroupId);
            AddBottomReadOnly(m_SyncPage, "Time", $"raw {state.RawTime:0.###} / effective {state.EffectiveTime:0.###} / cycle {state.EffectiveCycle}");
            AddBottomReadOnly(m_SyncPage, "Marker Segment", $"{state.PreviousMarkerId} -> {state.NextMarkerId} / {state.Fraction:0.###}");
            AddBottomReadOnly(m_SyncPage, "Relation", $"occurrence {state.TargetOccurrenceIndex} / depth {state.RelationDepth} / {state.LifecyclePhase} / {state.Reason}");
            AnimationProducerAuthoringEntry source = m_Producers.FirstOrDefault(value => string.Equals(value.ProgramProducerIdentity, state.SourceProducerId, StringComparison.Ordinal));
            if (source != null)
            {
                AddProducerNavigation(m_SyncPage, "Open Source Timeline", source);
                AddMarkerRuler(m_SyncPage, "Source", source, (float)state.EffectiveTime);
            }
        }

        bool TryGetSelectedProducer(out AnimationProducerAuthoringEntry producer)
        {
            producer = m_ProducerField.index >= 0 && m_ProducerField.index < m_Producers.Count
                ? m_Producers[m_ProducerField.index]
                : null;
            return producer != null;
        }

        bool TryGetRuntimeSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string status)
        {
            snapshot = default;
            RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
            if (!viewModel.Attached || !AnimationPresentationRuntimeTargetRegistry.TryGet(viewModel.Target.CharacterRuntimeId, out AnimationPresentationRuntimeTarget target))
            {
                status = "Unavailable: no attached Animation Presentation runtime target.";
                return false;
            }
            try
            {
                if (!target.TryGetSnapshot(out snapshot))
                {
                    status = "Unavailable: runtime target has no completed frame snapshot.";
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                status = "Stale: runtime target Projection revision changed.";
                return false;
            }
            if (!string.Equals(target.ProjectionRevision, m_Window.ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(snapshot.ProjectionRevision, m_Window.ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(snapshot.PoseGraphId, m_Window.ValidationRoot?.Graph?.GraphId, StringComparison.Ordinal) ||
                !string.Equals(snapshot.PoseGraphRevision, m_Window.ValidationRoot?.Graph?.ContentRevision, StringComparison.Ordinal))
            {
                snapshot = default;
                status = "Stale: runtime Pose Graph or Projection revision does not match this document.";
                return false;
            }
            status = "Ready";
            return true;
        }

        void AddProducerNavigation(VisualElement parent, string label, AnimationProducerAuthoringEntry producer)
        {
            parent.Add(new Button(() => RuntimeDebugSourceNavigator.Open(
                m_Window.DefinitionContext,
                RuntimeSourceElementKey.Track(producer.ProducerId.TimelineAuthoringId, producer.ProducerId.TrackAuthoringId)))
            {
                text = label,
                tooltip = producer.DisplayName
            });
        }

        static void AddMarkerRuler(VisualElement parent, string title, AnimationProducerAuthoringEntry producer, float time)
        {
            var heading = new Label($"{title} Marker Ruler");
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(heading);
            var ruler = new VisualElement();
            ruler.style.height = 36f;
            ruler.style.position = Position.Relative;
            ruler.style.marginBottom = 4f;
            var line = new VisualElement();
            line.style.position = Position.Absolute;
            line.style.left = 0f;
            line.style.right = 0f;
            line.style.top = 22f;
            line.style.height = 1f;
            line.style.backgroundColor = new Color(0.45f, 0.45f, 0.45f);
            ruler.Add(line);
            float duration = producer.Timeline.Timeline.Duration;
            for (int i = 0; i < producer.Track.SyncMarkers.Count; i++)
            {
                AnimationSyncMarker marker = producer.Track.SyncMarkers[i];
                if (marker == null)
                    continue;
                float markerTime = marker.Frame / (float)TimelineUtility.FrameRate;
                AddRulerTick(ruler, duration, markerTime, marker.MarkerId, new Color(0.4f, 0.75f, 1f));
            }
            AddRulerTick(ruler, duration, time, $"{time:0.###}s", new Color(1f, 0.7f, 0.2f));
            parent.Add(ruler);
        }

        static void AddRulerTick(VisualElement ruler, float duration, float time, string label, Color color)
        {
            float fraction = duration > 0f ? Mathf.Clamp01(time / duration) : 0f;
            var tick = new VisualElement { tooltip = $"{label} @ {time:0.###}s" };
            tick.style.position = Position.Absolute;
            tick.style.left = Length.Percent(fraction * 100f);
            tick.style.top = 8f;
            tick.style.width = 2f;
            tick.style.height = 22f;
            tick.style.backgroundColor = color;
            ruler.Add(tick);
        }

        static void AddBottomReadOnly(VisualElement parent, string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            var name = new Label(label);
            name.style.minWidth = 150f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(name);
            row.Add(new Label(value ?? string.Empty));
            parent.Add(row);
        }

        bool TryGetContext(
            out CharacterPipelineHost target,
            out AnimationProducerAuthoringEntry producer,
            out string error)
        {
            target = m_Target;
            producer = null;
            if (!m_Window.DefinitionContext)
            {
                error = "Unavailable: open the Pose Graph with one explicit Character Definition context.";
                return false;
            }
            if (!target || target.Definition != m_Window.DefinitionContext)
            {
                error = "Unavailable: select a scene Preview Target using the same explicit Character Definition.";
                return false;
            }
            if (!target.CanPreviewTimeline)
            {
                error = $"Unavailable: {target.PreviewStatus}";
                return false;
            }
            if (!m_Window.DefinitionContext.SimulationProgram ||
                string.IsNullOrWhiteSpace(m_Window.ProjectionRevision))
            {
                error = "Unavailable: the selected Character Definition has no published Program and Projection.";
                return false;
            }
            if (EditorUtility.IsDirty(m_Window.CurrentOwner) ||
                EditorUtility.IsDirty(m_Window.ProfileContext) ||
                EditorUtility.IsDirty(m_Window.DefinitionContext) ||
                EditorUtility.IsDirty(m_Window.RigDefinition))
            {
                error = "Dirty: authoring changed. Run the explicit Build before Preview.";
                return false;
            }
            if (m_ProducerField.index < 0 || m_ProducerField.index >= m_Producers.Count)
            {
                error = "Unavailable: select one reachable Timeline producer.";
                return false;
            }
            producer = m_Producers[m_ProducerField.index];
            error = string.Empty;
            return true;
        }

        bool TryResolveBlendSpacePreview(
            AnimationProducerAuthoringEntry producer,
            out CharacterAnimationBlendSpaceAsset asset,
            out float duration)
        {
            asset = null;
            duration = 0f;
            CharacterAnimationPresentationProfile profile = m_Window.ProfileContext;
            AnimationProducerPresentationBinding binding = profile?.FindProducerBinding(producer.ProducerId);
            if (binding == null || binding.SourceKind != AnimationPoseSourceKind.BlendSpace)
                return false;
            asset = binding.BlendSpaceSource;
            if (!asset)
                throw new InvalidOperationException(
                    $"Blend Space producer '{producer.ProgramProducerIdentity}' has no source asset.");
            CharacterSimulationProgram program = m_Window.DefinitionContext.SimulationProgram.Load();
            CharacterPresentationProjection projection = m_Window.DefinitionContext.PresentationProjection.Load(
                Float32CharacterPresentationContractAdapter.Create(program));
            CharacterAnimationBlendSpaceId blendSpaceId = asset.BlendSpaceId;
            CharacterAnimationBlendSpacePlan plan = projection.BlendSpaces.SingleOrDefault(value =>
                value.BlendSpaceId.Equals(blendSpaceId));
            if (plan == null || !string.Equals(plan.ContentRevision, asset.ContentRevision, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Blend Space producer '{producer.ProgramProducerIdentity}' has no matching compiled Projection plan.");
            duration = plan.ClockDurationSeconds;
            return true;
        }

        void StopPreview(string status)
        {
            m_Playing = false;
            if (m_Target && m_SessionId != Guid.Empty)
            {
                m_Target.RemovePreviewPoseWatchInterests(m_PoseWatchOwnerId);
                m_Target.ClearTimelinePreview(m_SessionId);
            }
            m_SessionId = Guid.Empty;
            m_Tick = 0;
            m_PreviewViewport.ClearTrajectory();
            if (!string.IsNullOrEmpty(status))
                m_Status.text = status;
            DrawPreviewSummary();
        }

        string ResolvePreviewState()
        {
            return TryGetContext(out _, out _, out string error)
                ? "Preview Ready: press Play, Step, or Seek to create an explicit session."
                : error;
        }
    }

    sealed class PoseGraphInspectorAdapter : IGraphAuthoringInspectorAdapter, IGraphAuthoringWorkspacePageAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        readonly PoseGraphMutationAdapter m_Mutation;
        readonly VisualElement m_Root = new VisualElement();
        readonly ScrollView m_View = new ScrollView();
        readonly ScrollView m_LiveView = new ScrollView();
        readonly ScrollView m_ReferencesView = new ScrollView();
        CharacterPoseNodeDefinition m_SelectedNode;
        bool m_Polling;
        double m_NextPollAt;
        string m_ActivePageId = "authoring";

        public PoseGraphInspectorAdapter(CharacterPresentationPoseGraphEditorWindow window, PoseGraphMutationAdapter mutation)
        {
            m_Window = window;
            m_Mutation = mutation;
            var tabs = new VisualElement();
            tabs.style.flexDirection = FlexDirection.Row;
            tabs.Add(new Button(() => ShowPage("authoring", m_View)) { text = "Authoring" });
            tabs.Add(new Button(() => ShowPage("live", m_LiveView)) { text = "Live" });
            tabs.Add(new Button(() => ShowPage("references", m_ReferencesView)) { text = "References" });
            m_Root.style.flexGrow = 1f;
            m_View.style.flexGrow = 1f;
            m_LiveView.style.flexGrow = 1f;
            m_ReferencesView.style.flexGrow = 1f;
            m_Root.Add(tabs);
            m_Root.Add(m_View);
            m_Root.Add(m_LiveView);
            m_Root.Add(m_ReferencesView);
            ShowPage("authoring", m_View);
        }

        public VisualElement View => m_Root;
        public string ActivePageId => m_ActivePageId;

        public void RestorePage(string pageId)
        {
            switch (pageId)
            {
                case "live":
                    ShowPage("live", m_LiveView);
                    break;
                case "references":
                    ShowPage("references", m_ReferencesView);
                    break;
                default:
                    ShowPage("authoring", m_View);
                    break;
            }
        }

        public void Bind(IGraphAuthoringDocument document)
        {
            m_SelectedNode = null;
            Draw(null);
            if (!m_Polling)
            {
                EditorApplication.update += Poll;
                m_Polling = true;
            }
        }

        public void Inspect(IReadOnlyList<ISelectable> selection)
        {
            CharacterPoseNodeDefinition[] nodes = selection?.OfType<PoseGraphNodeView>()
                .Select(view => view.Node)
                .Take(2)
                .ToArray() ?? Array.Empty<CharacterPoseNodeDefinition>();
            CharacterPoseNodeDefinition node = nodes.Length == 1 ? nodes[0] : null;
            if (node == m_SelectedNode)
                return;
            m_SelectedNode = node;
            Draw(node);
        }

        public void Clear()
        {
            m_SelectedNode = null;
            m_View.Clear();
            m_LiveView.Clear();
            m_ReferencesView.Clear();
            if (m_Polling)
            {
                EditorApplication.update -= Poll;
                m_Polling = false;
            }
        }

        void Draw(CharacterPoseNodeDefinition node)
        {
            m_View.Clear();
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
            {
                m_View.Add(new HelpBox("Pose Graph document is unavailable.", HelpBoxMessageType.Error));
                m_LiveView.Clear();
                m_ReferencesView.Clear();
                return;
            }
            m_View.Add(new Label(m_Window.CurrentDisplayName) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            m_View.Add(new Label($"Graph {graph.GraphId}"));
            m_View.Add(new Label($"Revision {graph.ContentRevision}"));
            if (node == null)
            {
                m_View.Add(new HelpBox("Select one Pose node to edit its formal authoring fields.", HelpBoxMessageType.Info));
                DrawLive(null, graph);
                DrawReferences(null, graph);
                return;
            }
            m_View.Add(new Label($"{node.Kind} / {node.NodeId}") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
            var displayName = new TextField("Display Name") { value = node.DisplayName, isDelayed = true };
            displayName.RegisterValueChangedCallback(evt =>
            {
                m_Mutation.RenameNode(graph, node, evt.newValue);
            });
            m_View.Add(displayName);

            var configuration = new PoseGraphNodeConfiguration(node);
            if (node.Kind == CharacterPoseNodeKind.AnimationSelectionInput || node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput)
                DrawSelection(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.ProgramParameterInput)
                DrawParameter(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
                DrawBlendSpacePlayer(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.BlendStack)
                DrawBlendPolicy(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.Inertialization)
                DrawInertializationPolicy(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.LayeredBoneBlend || node.Kind == CharacterPoseNodeKind.AdditivePose)
                DrawMask(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.BlendPose || node.Kind == CharacterPoseNodeKind.LayeredBoneBlend ||
                node.Kind == CharacterPoseNodeKind.AdditivePose || node.Kind == CharacterPoseNodeKind.ModifyBone ||
                node.Kind == CharacterPoseNodeKind.FootPlacement)
                DrawWeight(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.AdditivePose)
                DrawAdditive(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.PoseParameterResolve)
                DrawPolicies(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.ModifyBone)
                DrawModifyBone(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.FootPlacement)
                DrawFootPlacement(node, graph, configuration);
            if (node.Kind == CharacterPoseNodeKind.PoseSubgraph)
                DrawSubgraph(node, graph);
            DrawLive(node, graph);
            DrawReferences(node, graph);
        }

        void ShowPage(string pageId, VisualElement page)
        {
            m_ActivePageId = pageId;
            m_View.style.display = ReferenceEquals(page, m_View) ? DisplayStyle.Flex : DisplayStyle.None;
            m_LiveView.style.display = ReferenceEquals(page, m_LiveView) ? DisplayStyle.Flex : DisplayStyle.None;
            m_ReferencesView.style.display = ReferenceEquals(page, m_ReferencesView) ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Poll()
        {
            if (!m_Window ||
                !string.Equals(m_ActivePageId, "live", StringComparison.Ordinal) ||
                EditorApplication.timeSinceStartup < m_NextPollAt)
                return;
            m_NextPollAt = EditorApplication.timeSinceStartup + 0.1d;
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
                return;
            DrawLive(m_SelectedNode, graph);
        }

        void DrawLive(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            m_LiveView.Clear();
            AddReadOnly(m_LiveView, "Graph", $"{graph.GraphId} @ {graph.ContentRevision}");
            AddReadOnly(m_LiveView, "Projection", string.IsNullOrEmpty(m_Window.ProjectionRevision) ? "Unavailable" : m_Window.ProjectionRevision);
            if (node == null)
            {
                m_LiveView.Add(new HelpBox("Select one Pose node to inspect its formal runtime operation.", HelpBoxMessageType.Info));
                return;
            }
            if (!TryGetSnapshot(graph, out AnimationPresentationRuntimeSnapshot snapshot, out string status))
            {
                m_LiveView.Add(new HelpBox(status, status.StartsWith("Stale", StringComparison.Ordinal) ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info));
                return;
            }
            AddReadOnly(m_LiveView, "Frame Completion", snapshot.CompletionIdentity.ToString());
            AddReadOnly(m_LiveView, "Final Availability", snapshot.FinalAvailability.ToString());
            AddReadOnly(m_LiveView, "Final Applied At", snapshot.FinalAppliedAt.ToString());
            int occurrenceCount = snapshot.GetOperationMatchCount(graph.GraphId, node.NodeId);
            if (occurrenceCount == 0)
            {
                m_LiveView.Add(new HelpBox("Unavailable: the selected node has no operation in the completed Pose Plan snapshot.", HelpBoxMessageType.Info));
                return;
            }
            for (int occurrence = 0; occurrence < occurrenceCount; occurrence++)
            {
                if (!snapshot.TryGetOperationTrace(graph.GraphId, node.NodeId, occurrence, out AnimationPoseOperationTrace trace))
                    continue;
                AnimationPoseOperationSnapshot operation = trace.Operation;
                AddSection(m_LiveView, occurrenceCount == 1 ? "Operation" : $"Operation {occurrence + 1}");
                AddReadOnly(m_LiveView, "Compiled Identity", $"#{operation.OperationIndex} {operation.Code}");
                AddReadOnly(m_LiveView, "Call Site", string.IsNullOrEmpty(operation.CallSite) ? "Root" : operation.CallSite);
                AddReadOnly(m_LiveView, "Availability", operation.Availability.ToString());
                AddReadOnly(m_LiveView, "Invalid Reason", operation.InvalidReason.ToString());
                AddReadOnly(m_LiveView, "Weight", operation.OutputWeight.ToString("0.###"));
                AddReadOnly(m_LiveView, "Continuity", operation.ContinuityIdentity.ToString());
                AddReadOnly(m_LiveView, "Completion", operation.CompletionIdentity.ToString());
                AnimationReadOnlyBuffer<AnimationPoseSourceContribution> contributions = trace.Contributions;
                for (int i = 0; i < contributions.Count; i++)
                {
                    AnimationPoseSourceContribution contribution = contributions[i];
                    string source = contribution.SourceId.IsValid ? contribution.SourceId.ToString() : contribution.Kind.ToString();
                    AddReadOnly(m_LiveView, $"Contribution {i + 1}", $"{source} / {contribution.Weight:0.###}");
                }
            }
            if (node.Kind == CharacterPoseNodeKind.MarkerSync)
                DrawMarkerSyncLive(node);
            if (node.Kind == CharacterPoseNodeKind.FootPlacement)
            {
                RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
                if (viewModel.Attached && AnimationPresentationRuntimeTargetRegistry.TryGet(
                        viewModel.Target.CharacterRuntimeId,
                        out AnimationPresentationRuntimeTarget target) &&
                    target.TryGetPosePlanStages(out CharacterPosePlanStageSnapshot stages))
                {
                    AddSection(m_LiveView, "World-Aware Stage");
                    AddReadOnly(m_LiveView, "Status", stages.WorldAware.Status.ToString());
                    AddReadOnly(m_LiveView, "Solver", stages.WorldAware.UnavailableReason == CharacterPosePlanPhaseUnavailableReason.None
                        ? "Completed"
                        : stages.WorldAware.UnavailableReason.ToString());
                    AddReadOnly(m_LiveView, "Completion", stages.WorldAware.CompletionIdentity.ToString());
                }
                else
                {
                    AddSection(m_LiveView, "World-Aware Stage");
                    AddReadOnly(m_LiveView, "Status", "Unavailable");
                }
            }
            if (node.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
            {
                for (int playerIndex = 0; playerIndex < snapshot.BlendSpacePlayers.Count; playerIndex++)
                {
                    AnimationBlendSpacePlayerRuntimeSnapshot player = snapshot.BlendSpacePlayers[playerIndex];
                    if (!player.NodeId.Equals(node.NodeId))
                        continue;
                    AddSection(m_LiveView, "Blend Space Player");
                    AddReadOnly(m_LiveView, "Asset", $"{player.BlendSpaceId}@{player.ContentRevision}");
                    AddReadOnly(m_LiveView, "Mode", player.Mode.ToString());
                    AddReadOnly(m_LiveView, "Source", player.SourceId.ToString());
                    AddReadOnly(m_LiveView, "Raw Parameter", $"({player.RawX:0.###}, {player.RawY:0.###})");
                    AddReadOnly(m_LiveView, "Processed Parameter", $"({player.X:0.###}, {player.Y:0.###})");
                    AddReadOnly(m_LiveView, "Canonical Phase", $"{player.CanonicalPhase.NormalizedPhase:0.###} / cycle {player.CanonicalPhase.Cycle}");
                    AddReadOnly(m_LiveView, "Pose Result", $"{player.PoseAvailability} / {player.InvalidReason}");
                    AnimationReadOnlyBuffer<AnimationBlendSpaceSampleRuntimeSnapshot> samples =
                        snapshot.GetBlendSpaceSamples(playerIndex);
                    for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
                    {
                        AnimationBlendSpaceSampleRuntimeSnapshot sample = samples[sampleIndex];
                        AddReadOnly(
                            m_LiveView,
                            $"Sample {sampleIndex + 1}",
                            $"{sample.SampleId} / weight {sample.Weight:0.###} / time {sample.ClipTime:0.###}s ({sample.NormalizedTime:0.###}) / feature {(sample.HasFootFeatures ? $"{sample.FootAnalysisSourceId}@{sample.FootAnalysisVersion}/{sample.FootArtifactContentHash}" : "Unavailable")}");
                    }
                }
            }
            for (int i = 0; i < snapshot.Lifecycle.Count; i++)
            {
                var lifecycle = snapshot.Lifecycle[i];
                if (!lifecycle.PoseNodeId.Equals(node.NodeId))
                    continue;
                AddSection(m_LiveView, "Player Lifecycle");
                AddReadOnly(m_LiveView, "Source", lifecycle.SourceId.IsValid ? lifecycle.SourceId.ToString() : "No Source");
                AddReadOnly(m_LiveView, "Phase", lifecycle.Phase.ToString());
                AddReadOnly(m_LiveView, "Sample Time", lifecycle.SampleTime.ToString("0.###"));
                AddReadOnly(m_LiveView, "Play Rate", lifecycle.VisualTimeScale.ToString("0.###"));
                AddReadOnly(m_LiveView, "Output Weight", lifecycle.OutputWeight.ToString("0.###"));
            }
            for (int i = 0; i < snapshot.Releases.Count; i++)
            {
                AnimationReleasedPoseSourceSnapshot release = snapshot.Releases[i];
                if (!release.PoseNodeId.Equals(node.NodeId))
                    continue;
                AddSection(m_LiveView, "Released Source");
                AddReadOnly(m_LiveView, "Source", release.SourceId.ToString());
                AddReadOnly(m_LiveView, "Completion", release.CompletionIdentity.ToString());
            }
            for (int i = 0; i < snapshot.Stacks.Count; i++)
            {
                AnimationBlendStackSnapshot stack = snapshot.Stacks[i];
                if (!stack.PoseNodeId.Equals(node.NodeId))
                    continue;
                AddSection(m_LiveView, "Blend Stack");
                AddReadOnly(m_LiveView, "Entries", stack.EntryCount.ToString());
                AddReadOnly(m_LiveView, "Stored Pose", stack.HasStoredPose ? "Ready" : stack.HasPendingStoredCapture ? "Pending Capture" : "None");
                AddReadOnly(m_LiveView, "Stored Weight", stack.StoredOutputWeight.ToString("0.###"));
                AddReadOnly(m_LiveView, "Stored Completion", $"Captured {stack.StoredCapturedAt} / Source History {stack.StoredSourceHistoryCompletedAt}");
                AddReadOnly(m_LiveView, "Output Weight", stack.OutputWeight.ToString("0.###"));
                AddReadOnly(m_LiveView, "Completion", stack.CompletionIdentity.ToString());
                for (int entryIndex = stack.EntryOffset; entryIndex < stack.EntryOffset + stack.EntryCount; entryIndex++)
                {
                    AnimationBlendStackEntrySnapshot entry = snapshot.Entries[entryIndex];
                    AddReadOnly(
                        m_LiveView,
                        $"Entry {entry.Order}",
                        $"{entry.EntryId} / Producer {entry.ProgramProducerIndex} / Weight {entry.OutputWeight:0.###} / Clock {entry.ElapsedSeconds:0.###}/{entry.DurationSeconds:0.###} / Active");
                }
            }
            for (int i = 0; i < snapshot.Inertializations.Count; i++)
            {
                PoseInertializationSnapshot inertialization = snapshot.Inertializations[i];
                if (!inertialization.NodeId.Equals(node.NodeId))
                    continue;
                AddSection(m_LiveView, "Inertialization");
                AddReadOnly(m_LiveView, "State", inertialization.State.ToString());
                AddReadOnly(m_LiveView, "Rule", inertialization.RuleIdentity);
                AddReadOnly(m_LiveView, "Clock", $"{inertialization.ElapsedSeconds:0.###} / {inertialization.DurationSeconds:0.###}");
                AddReadOnly(m_LiveView, "Reset", $"{inertialization.ResetReason} / {inertialization.ResetSequence}");
            }
        }

        void DrawMarkerSyncLive(CharacterPoseNodeDefinition node)
        {
            RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
            if (!viewModel.Attached || !AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget target))
            {
                AddSection(m_LiveView, "Marker Sync");
                AddReadOnly(m_LiveView, "Status", "Unavailable");
                return;
            }
            int relationCount = 0;
            IReadOnlyList<AnimationMarkerSyncRelationSnapshot> relations = target.MarkerSyncSnapshots;
            for (int i = 0; i < relations.Count; i++)
            {
                AnimationMarkerSyncRelationSnapshot relation = relations[i];
                if (!relation.MarkerNodeId.Equals(node.NodeId))
                    continue;
                relationCount++;
                AddSection(m_LiveView, relationCount == 1 ? "Marker Sync Relation" : $"Marker Sync Relation {relationCount}");
                AddReadOnly(m_LiveView, "Channel / Group", $"{relation.AnimationChannelId} / {relation.SyncGroupId}");
                AddReadOnly(m_LiveView, "Source / Target", $"{relation.Source} / {relation.Target}");
                AddReadOnly(m_LiveView, "Source Time", $"Raw {relation.SourceRawTime:0.###} / Effective {relation.SourceEffectiveTime:0.###}");
                AddReadOnly(m_LiveView, "Target Time", $"Raw {relation.TargetRawTime:0.###} / Effective {relation.TargetEffectiveTime:0.###} / Cycle {relation.TargetEffectiveCycle}");
                AddReadOnly(m_LiveView, "Marker Pair", $"{relation.PreviousMarkerId} -> {relation.NextMarkerId}");
                AddReadOnly(m_LiveView, "Fraction", relation.Fraction.ToString("0.###"));
                AddReadOnly(m_LiveView, "Occurrence / Depth", $"{relation.TargetOccurrenceIndex} / {relation.RelationDepth}");
                AddReadOnly(m_LiveView, "Reason / Lifecycle", $"{relation.Reason} / {relation.TargetLifecyclePhase}");
            }
            int playbackCount = 0;
            IReadOnlyList<AnimationMarkerSyncPlaybackSnapshot> playbacks = target.MarkerSyncPlaybackSnapshots;
            for (int i = 0; i < playbacks.Count; i++)
            {
                AnimationMarkerSyncPlaybackSnapshot playback = playbacks[i];
                if (!playback.MarkerNodeId.Equals(node.NodeId))
                    continue;
                playbackCount++;
                AddSection(m_LiveView, playbackCount == 1 ? "Marker Sync Playback" : $"Marker Sync Playback {playbackCount}");
                AddReadOnly(m_LiveView, "Playback", playback.PlaybackId.ToString());
                AddReadOnly(m_LiveView, "Channel / Group", $"{playback.AnimationChannelId} / {playback.SyncGroupId}");
                AddReadOnly(m_LiveView, "Time", $"Raw {playback.RawTime:0.###} / Effective {playback.EffectiveTime:0.###} / Cycle {playback.EffectiveCycle}");
                AddReadOnly(m_LiveView, "Marker Pair", $"{playback.PreviousMarkerId} -> {playback.NextMarkerId}");
                AddReadOnly(m_LiveView, "Fraction", playback.Fraction.ToString("0.###"));
                AddReadOnly(m_LiveView, "Mapping", $"Mapped {playback.Mapped} / Rebased {playback.Rebased}");
            }
            if (relationCount == 0 && playbackCount == 0)
            {
                AddSection(m_LiveView, "Marker Sync");
                AddReadOnly(m_LiveView, "Status", "Unavailable: no formal MarkerSync snapshot for this node in the completed frame.");
            }
        }

        void DrawReferences(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            m_ReferencesView.Clear();
            AddReadOnly(m_ReferencesView, "Graph", graph.GraphId);
            AddReadOnly(m_ReferencesView, "Graph Revision", graph.ContentRevision);
            AddAssetReference(m_ReferencesView, "Graph Owner", m_Window.CurrentOwner);
            AddAssetReference(m_ReferencesView, "Presentation Profile", m_Window.ProfileContext);
            AddAssetReference(m_ReferencesView, "Character Definition", m_Window.DefinitionContext);
            AddAssetReference(m_ReferencesView, "Rig Definition", m_Window.RigDefinition);
            if (node == null)
                return;
            AddSection(m_ReferencesView, "Selected Node");
            AddReadOnly(m_ReferencesView, "Node", node.NodeId.Value);
            AddReadOnly(m_ReferencesView, "Formal Kind", node.Kind.ToString());
            AddAssetReference(m_ReferencesView, "Blend Policy", node.BlendPolicy);
            AddAssetReference(m_ReferencesView, "Inertialization Policy", node.InertializationPolicy);
            AddAssetReference(m_ReferencesView, "Bone Mask", node.BoneMask);
            AddAssetReference(m_ReferencesView, "Foot Placement Profile", node.FootPlacementProfile);
            AddAssetReference(m_ReferencesView, "Foot Placement Calibration", node.FootPlacementCalibration);
            if (TryGetSnapshot(graph, out AnimationPresentationRuntimeSnapshot snapshot, out _))
            {
                int count = snapshot.GetOperationMatchCount(graph.GraphId, node.NodeId);
                for (int i = 0; i < count; i++)
                {
                    if (!snapshot.TryGetOperationTrace(graph.GraphId, node.NodeId, i, out AnimationPoseOperationTrace trace))
                        continue;
                    AddReadOnly(
                        m_ReferencesView,
                        $"Compiled Operation {i + 1}",
                        $"#{trace.Operation.OperationIndex} {trace.Operation.Code} / {(string.IsNullOrEmpty(trace.Operation.CallSite) ? "Root" : trace.Operation.CallSite)}");
                }
                if (node.Kind == CharacterPoseNodeKind.BlendSpacePlayer)
                {
                    for (int playerIndex = 0; playerIndex < snapshot.BlendSpacePlayers.Count; playerIndex++)
                    {
                        AnimationBlendSpacePlayerRuntimeSnapshot player = snapshot.BlendSpacePlayers[playerIndex];
                        if (!player.NodeId.Equals(node.NodeId))
                            continue;
                        AddReadOnly(m_ReferencesView, "Blend Space", $"{player.BlendSpaceId}@{player.ContentRevision}");
                        AddReadOnly(m_ReferencesView, "Producer Source", player.SourceId.ToString());
                        AddReadOnly(m_ReferencesView, "Projection", snapshot.ProjectionRevision);
                    }
                }
            }
            if ((node.Kind == CharacterPoseNodeKind.AnimationSelectionInput || node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput) &&
                m_Window.DefinitionContext && m_Window.ProfileContext)
            {
                try
                {
                    IReadOnlyList<AnimationProducerAuthoringEntry> producers =
                        CharacterAnimationPresentationAuthoringService.DiscoverProducers(m_Window.ProfileContext, m_Window.DefinitionContext);
                    for (int i = 0; i < producers.Count; i++)
                    {
                        AnimationProducerAuthoringEntry producer = producers[i];
                        if (!producer.AnimationChannelId.Equals(node.AnimationChannelId))
                            continue;
                        var button = new Button(() => RuntimeDebugSourceNavigator.Open(
                            m_Window.DefinitionContext,
                            RuntimeSourceElementKey.Track(
                                producer.ProducerId.TimelineAuthoringId,
                                producer.ProducerId.TrackAuthoringId)))
                        {
                            text = $"Open {producer.DisplayName}"
                        };
                        m_ReferencesView.Add(button);
                    }
                }
                catch (Exception exception)
                {
                    m_ReferencesView.Add(new HelpBox(exception.Message, HelpBoxMessageType.Error));
                }
            }
        }

        bool TryGetSnapshot(
            CharacterPoseGraphData graph,
            out AnimationPresentationRuntimeSnapshot snapshot,
            out string status)
        {
            snapshot = default;
            RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
            if (!viewModel.Attached)
            {
                status = "Unavailable: no attached runtime target.";
                return false;
            }
            if (!AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget target))
            {
                status = "Unavailable: target has no Animation Presentation runtime.";
                return false;
            }
            try
            {
                if (!target.TryGetSnapshot(out snapshot))
                {
                    status = "Unavailable: target has no published frame snapshot.";
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                status = "Stale: runtime target Projection revision changed.";
                return false;
            }
            if (!string.Equals(target.ProjectionRevision, m_Window.ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(snapshot.ProjectionRevision, m_Window.ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(snapshot.PoseGraphId, m_Window.ValidationRoot?.Graph?.GraphId, StringComparison.Ordinal) ||
                !string.Equals(snapshot.PoseGraphRevision, m_Window.ValidationRoot?.Graph?.ContentRevision, StringComparison.Ordinal))
            {
                snapshot = default;
                status = "Stale: Pose Graph or Projection revision does not match the attached runtime target.";
                return false;
            }
            status = "Ready";
            return true;
        }

        static void AddSection(VisualElement view, string title)
        {
            var label = new Label(title);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 8f;
            view.Add(label);
        }

        static void AddReadOnly(VisualElement view, string label, string value)
        {
            var field = new TextField(label) { value = value ?? string.Empty, isReadOnly = true };
            view.Add(field);
        }

        static void AddAssetReference(VisualElement view, string label, UnityEngine.Object asset)
        {
            if (!asset)
                return;
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            var field = new ObjectField(label) { objectType = asset.GetType(), value = asset };
            field.SetEnabled(false);
            field.style.flexGrow = 1f;
            row.Add(field);
            row.Add(new Button(() =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }) { text = "Locate" });
            view.Add(row);
        }

        void DrawSelection(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var channel = new TextField("Animation Channel") { value = node.AnimationChannelId.Value ?? string.Empty, isDelayed = true };
            channel.RegisterValueChangedCallback(evt =>
            {
                configuration.AnimationChannelId = string.IsNullOrWhiteSpace(evt.newValue)
                    ? default
                    : new ThirdPersonSimulation.AnimationChannelId(evt.newValue.Trim());
                Apply(node, graph, configuration);
            });
            m_View.Add(channel);
            if (node.Kind == CharacterPoseNodeKind.MotionMatchingSelectionInput)
            {
                var producer = new TextField("Program Producer") { value = node.ProgramProducerId, isDelayed = true };
                producer.RegisterValueChangedCallback(evt =>
                {
                    configuration.ProgramProducerId = evt.newValue?.Trim() ?? string.Empty;
                    Apply(node, graph, configuration);
                });
                m_View.Add(producer);
            }
            var availability = new EnumField("Availability", node.SelectionAvailability);
            availability.RegisterValueChangedCallback(evt =>
            {
                configuration.SelectionAvailability = (AnimationSelectionAvailabilityPolicy)evt.newValue;
                Apply(node, graph, configuration);
            });
            m_View.Add(availability);
        }

        void DrawParameter(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            List<string> parameters = graph.Parameters.Select(value => value.ParameterId.Value).ToList();
            if (parameters.Count == 0)
            {
                m_View.Add(new HelpBox("ProgramParameterInput requires a declared graph Parameter.", HelpBoxMessageType.Error));
                return;
            }
            int selected = Math.Max(0, parameters.IndexOf(node.ParameterId.Value));
            var field = new PopupField<string>("Parameter", parameters, selected);
            field.RegisterValueChangedCallback(evt =>
            {
                configuration.ParameterId = new PoseParameterId(evt.newValue);
                Apply(node, graph, configuration);
            });
            m_View.Add(field);
        }

        void DrawBlendSpacePlayer(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var availability = new EnumField("Availability", node.SelectionAvailability);
            availability.RegisterValueChangedCallback(evt =>
            {
                configuration.SelectionAvailability = (AnimationSelectionAvailabilityPolicy)evt.newValue;
                Apply(node, graph, configuration);
            });
            m_View.Add(availability);
            var range = new EnumField("Input Range", node.BlendSpaceInputRangePolicy);
            range.RegisterValueChangedCallback(evt =>
            {
                configuration.BlendSpaceInputRangePolicy = (CharacterAnimationBlendSpaceInputRangePolicy)evt.newValue;
                Apply(node, graph, configuration);
            });
            m_View.Add(range);
        }

        void DrawBlendPolicy(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var field = new ObjectField("Blend Policy") { objectType = typeof(CharacterAnimationBlendPolicy), value = node.BlendPolicy };
            field.RegisterValueChangedCallback(evt =>
            {
                configuration.BlendPolicy = evt.newValue as CharacterAnimationBlendPolicy;
                Apply(node, graph, configuration);
            });
            m_View.Add(field);
        }

        void DrawInertializationPolicy(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var field = new ObjectField("Inertialization Policy") { objectType = typeof(CharacterPoseInertializationPolicy), value = node.InertializationPolicy };
            field.RegisterValueChangedCallback(evt =>
            {
                configuration.InertializationPolicy = evt.newValue as CharacterPoseInertializationPolicy;
                Apply(node, graph, configuration);
            });
            m_View.Add(field);
        }

        void DrawMask(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var mask = new ObjectField("Bone Mask") { objectType = typeof(CharacterAnimationBoneMaskAsset), value = node.BoneMask };
            mask.RegisterValueChangedCallback(evt =>
            {
                configuration.BoneMask = evt.newValue as CharacterAnimationBoneMaskAsset;
                Apply(node, graph, configuration);
            });
            m_View.Add(mask);
        }

        void DrawWeight(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var weight = new FloatField("Weight") { value = node.Weight, isDelayed = true };
            weight.RegisterValueChangedCallback(evt =>
            {
                configuration.Weight = Mathf.Clamp01(evt.newValue);
                Apply(node, graph, configuration);
            });
            m_View.Add(weight);
        }

        void DrawAdditive(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            m_View.Add(new Label($"Reference {node.AdditiveReferencePoseId}"));
            var space = new EnumField("Reference Space", node.AdditiveReferenceSpace);
            space.RegisterValueChangedCallback(evt =>
            {
                configuration.AdditiveReferenceSpace = (AdditiveReferenceSpace)evt.newValue;
                Apply(node, graph, configuration);
            });
            m_View.Add(space);
            var scale = new EnumField("Scale Policy", node.AdditiveScalePolicy);
            scale.RegisterValueChangedCallback(evt =>
            {
                configuration.AdditiveScalePolicy = (AdditiveScalePolicy)evt.newValue;
                Apply(node, graph, configuration);
            });
            m_View.Add(scale);
        }

        void DrawPolicies(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var current = new Dictionary<PoseParameterId, PoseParameterResolvePolicy>();
            for (int policyIndex = 0; policyIndex < node.ParameterPolicies.Count; policyIndex++)
            {
                CharacterPoseParameterPolicy value = node.ParameterPolicies[policyIndex];
                if (value != null)
                    current.TryAdd(value.ParameterId, value.Policy);
            }
            for (int i = 0; i < graph.Parameters.Count; i++)
            {
                CharacterPoseParameterDeclaration parameter = graph.Parameters[i];
                if (!current.TryGetValue(parameter.ParameterId, out PoseParameterResolvePolicy policy))
                {
                    m_View.Add(new HelpBox($"Missing policy: {parameter.ParameterId}", HelpBoxMessageType.Error));
                    policy = PoseParameterResolvePolicy.Weighted;
                    current[parameter.ParameterId] = policy;
                }
                PoseParameterId parameterId = parameter.ParameterId;
                var field = new EnumField(parameterId.Value, policy);
                field.RegisterValueChangedCallback(evt =>
                {
                    current[parameterId] = (PoseParameterResolvePolicy)evt.newValue;
                    CharacterPoseParameterPolicy[] policies = graph.Parameters
                        .Select(declaration => new CharacterPoseParameterPolicy(declaration.ParameterId, current[declaration.ParameterId]))
                        .ToArray();
                    configuration.ParameterPolicies = policies;
                    Apply(node, graph, configuration);
                });
                m_View.Add(field);
            }
        }

        void DrawModifyBone(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var bone = new TextField("Bone Id") { value = node.BoneId.Value ?? string.Empty, isDelayed = true };
            bone.RegisterValueChangedCallback(evt =>
            {
                configuration.BoneId = string.IsNullOrWhiteSpace(evt.newValue) ? default : new AnimationBoneId(evt.newValue.Trim());
                Apply(node, graph, configuration);
            });
            m_View.Add(bone);
            AddEnum("Reference Space", node.ModifyBoneReferenceSpace, value => configuration.ModifyBoneReferenceSpace = (ModifyBoneReferenceSpace)value);
            AddEnum("Operations", node.ModifyBoneOperations, value => configuration.ModifyBoneOperations = (ModifyBoneOperationMask)value);
            AddVector("Position", node.ModifyPosition, value => configuration.ModifyPosition = value);
            AddVector("Rotation", node.ModifyRotation.eulerAngles, value => configuration.ModifyRotationEuler = value);
            AddVector("Scale", node.ModifyScale, value => configuration.ModifyScale = value);

            void AddEnum(string label, Enum value, Action<Enum> assign)
            {
                var field = new EnumField(label, value);
                field.RegisterValueChangedCallback(evt => { assign(evt.newValue); Apply(node, graph, configuration); });
                m_View.Add(field);
            }
            void AddVector(string label, Vector3 value, Action<Vector3> assign)
            {
                var field = new Vector3Field(label) { value = value };
                field.RegisterValueChangedCallback(evt => { assign(evt.newValue); Apply(node, graph, configuration); });
                m_View.Add(field);
            }
        }

        void DrawFootPlacement(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration)
        {
            var profile = new ObjectField("Foot Placement Profile") { objectType = typeof(CharacterFootPlacementProfile), value = node.FootPlacementProfile };
            profile.RegisterValueChangedCallback(evt =>
            {
                configuration.FootPlacementProfile = evt.newValue as CharacterFootPlacementProfile;
                Apply(node, graph, configuration);
            });
            m_View.Add(profile);
            var calibration = new ObjectField("Rig Calibration") { objectType = typeof(CharacterFootPlacementRigCalibration), value = node.FootPlacementCalibration };
            calibration.RegisterValueChangedCallback(evt =>
            {
                configuration.FootPlacementCalibration = evt.newValue as CharacterFootPlacementRigCalibration;
                Apply(node, graph, configuration);
            });
            m_View.Add(calibration);
        }

        void DrawSubgraph(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph)
        {
            CharacterPoseSubgraphReference reference = node.Subgraph;
            string state = reference == null ? "Missing" : reference.HasInline ? "Inline" : reference.HasShared ? "Shared Asset" : "Missing";
            m_View.Add(new Label("Ownership: " + state));
            Button open = new Button(() => m_Window.OpenSubgraph(node)) { text = "Open" };
            open.SetEnabled(reference != null && reference.IsExclusive);
            m_View.Add(open);
            if (reference == null || !reference.IsExclusive)
            {
                m_View.Add(new Button(() =>
                {
                    m_Mutation.CreateInline(graph, node);
                }) { text = "Create Inline" });
            }
            if (reference != null && reference.HasInline && !reference.HasShared)
            {
                m_View.Add(new Button(() =>
                {
                    string path = EditorUtility.SaveFilePanelInProject("Extract Shared Pose Subgraph", "SharedPoseSubgraph", "asset", "Choose the formal shared Pose Graph asset path.");
                    if (string.IsNullOrEmpty(path))
                        return;
                    m_Mutation.ExtractShared(graph, node, path);
                }) { text = "Extract Shared" });
            }
            var shared = new ObjectField("Shared Asset")
            {
                objectType = typeof(CharacterPresentationPoseGraphAsset),
                value = reference != null && reference.HasShared ? reference.Shared : null
            };
            shared.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue is CharacterPresentationPoseGraphAsset asset)
                    m_Mutation.UseShared(graph, node, asset);
                else if (reference != null && reference.HasShared)
                    m_Mutation.ClearShared(graph, node);
            });
            m_View.Add(shared);
        }

        void Apply(CharacterPoseNodeDefinition node, CharacterPoseGraphData graph, PoseGraphNodeConfiguration configuration) =>
            m_Mutation.ConfigureNode(graph, node, configuration);
    }

    sealed class PoseGraphDiagnosticsAdapter : IGraphAuthoringDiagnosticsAdapter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        Label m_Status;
        bool m_Polling;
        bool m_RuntimePollingEnabled;
        double m_NextPollAt;
        CharacterPoseGraphValidationReport m_AuthoringReport;
        string m_AuthoringStatus = string.Empty;

        public PoseGraphDiagnosticsAdapter(CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window;
        }

        public void Bind(IGraphAuthoringDocument document, GraphView graphView, VisualElement toolbar)
        {
            if (m_Status == null)
            {
                m_Status = new Label();
                m_Status.style.flexGrow = 1f;
                m_Status.style.unityTextAlign = TextAnchor.MiddleRight;
                toolbar.Add(m_Status);
            }
            if (!m_Polling)
            {
                EditorApplication.update += PollRuntimeSnapshot;
                m_Polling = true;
            }
            Refresh();
        }

        public void Refresh()
        {
            m_RuntimePollingEnabled = false;
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph == null)
                return;
            CharacterAnimationRigDefinition rig = m_Window.RigDefinition;
            if (!rig)
            {
                m_Status.text = "Diagnostics Unavailable: open from a Presentation Profile with an explicit Rig";
                m_Window.GraphView.ApplyDiagnostics(null, graph);
                return;
            }
            if (m_Window.ValidationRoot == null || m_Window.ValidationRoot.Graph.Nodes.Any(node =>
                    node != null && (node.Kind == CharacterPoseNodeKind.GraphInput || node.Kind == CharacterPoseNodeKind.GraphOutput)))
            {
                m_Status.text = "Diagnostics Unavailable: shared subgraph requires a parent call-site context";
                m_Window.GraphView.ApplyDiagnostics(null, graph);
                return;
            }
            CharacterPoseGraphValidationReport report = CharacterPresentationPoseGraphValidator.Validate(m_Window.ValidationRoot, rig);
            string projection = m_Window.ProjectionRevision.Length > 0
                ? $"Projection {m_Window.ProjectionRevision}"
                : "Projection Unavailable";
            string authoring = m_Window.ResolveWorkspaceBuildState();
            m_AuthoringReport = report;
            m_AuthoringStatus = $"{authoring} / {graph.ContentRevision} / {projection}";
            m_RuntimePollingEnabled = true;
            RefreshRuntimeSnapshot(graph);
        }

        void RefreshRuntimeSnapshot(CharacterPoseGraphData graph)
        {
            string liveStatus = ResolveRuntimeSnapshot(graph, out IReadOnlyDictionary<PoseNodeId, IReadOnlyList<string>> runtimeMessages);
            m_Status.text = $"{m_AuthoringStatus} / {liveStatus}";
            m_Window.GraphView.ApplyDiagnostics(m_AuthoringReport, graph, runtimeMessages);
        }

        public void Clear()
        {
            if (m_Polling)
            {
                EditorApplication.update -= PollRuntimeSnapshot;
                m_Polling = false;
            }
            if (m_Status != null)
                m_Status.RemoveFromHierarchy();
            m_Status = null;
            m_RuntimePollingEnabled = false;
            m_AuthoringReport = null;
            m_AuthoringStatus = string.Empty;
        }

        void PollRuntimeSnapshot()
        {
            if (!m_Window ||
                !m_RuntimePollingEnabled ||
                EditorApplication.timeSinceStartup < m_NextPollAt)
                return;
            m_NextPollAt = EditorApplication.timeSinceStartup + 0.1d;
            CharacterPoseGraphData graph = m_Window.CurrentGraph;
            if (graph != null)
                RefreshRuntimeSnapshot(graph);
        }

        string ResolveRuntimeSnapshot(
            CharacterPoseGraphData graph,
            out IReadOnlyDictionary<PoseNodeId, IReadOnlyList<string>> runtimeMessages)
        {
            runtimeMessages = null;
            RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
            if (!viewModel.Attached)
                return "Live Unavailable: no attached runtime target";
            if (!AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget target))
            {
                return "Live Unavailable: target has no Animation Presentation runtime";
            }
            AnimationPresentationRuntimeSnapshot snapshot;
            try
            {
                if (!target.TryGetSnapshot(out snapshot))
                    return "Live Unavailable: target has no published frame snapshot";
            }
            catch (InvalidOperationException)
            {
                return "Live Stale: runtime target ProjectionRevision changed";
            }
            if (!string.Equals(target.ProjectionRevision, m_Window.ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(snapshot.ProjectionRevision, m_Window.ProjectionRevision, StringComparison.Ordinal))
            {
                return "Live Stale: ProjectionRevision mismatch";
            }

            CharacterPoseGraphData root = m_Window.ValidationRoot?.Graph;
            if (root == null ||
                !string.Equals(snapshot.PoseGraphId, root.GraphId, StringComparison.Ordinal) ||
                !string.Equals(snapshot.PoseGraphRevision, root.ContentRevision, StringComparison.Ordinal))
            {
                return "Live Stale: Pose Graph identity mismatch";
            }

            var messages = new Dictionary<PoseNodeId, IReadOnlyList<string>>();
            for (int nodeIndex = 0; nodeIndex < graph.Nodes.Count; nodeIndex++)
            {
                CharacterPoseNodeDefinition node = graph.Nodes[nodeIndex];
                if (node == null)
                    continue;
                int occurrenceCount = snapshot.GetOperationMatchCount(graph.GraphId, node.NodeId);
                if (occurrenceCount == 0)
                    continue;
                var nodeMessages = new List<string>(occurrenceCount);
                for (int occurrence = 0; occurrence < occurrenceCount; occurrence++)
                {
                    if (!snapshot.TryGetOperationTrace(graph.GraphId, node.NodeId, occurrence, out AnimationPoseOperationTrace trace))
                        continue;
                    nodeMessages.Add(FormatRuntimeTrace(trace, occurrenceCount));
                }
                if (nodeMessages.Count > 0)
                    messages.Add(node.NodeId, nodeMessages);
            }
            runtimeMessages = messages;
            return $"Live / Completion {snapshot.CompletionIdentity} / Output {snapshot.FinalAvailability}";
        }

        static string FormatRuntimeTrace(AnimationPoseOperationTrace trace, int occurrenceCount)
        {
            AnimationPoseOperationSnapshot operation = trace.Operation;
            string callSite = occurrenceCount > 1 && !string.IsNullOrWhiteSpace(operation.CallSite)
                ? $" [{operation.CallSite}]"
                : string.Empty;
            string invalid = operation.Availability == AnimationPoseAvailability.Invalid
                ? $" / {operation.InvalidReason}"
                : string.Empty;
            AnimationReadOnlyBuffer<AnimationPoseSourceContribution> contributions = trace.Contributions;
            string contribution = contributions.Count == 0
                ? "none"
                : string.Join(", ", Enumerable.Range(0, contributions.Count).Select(index =>
                {
                    AnimationPoseSourceContribution value = contributions[index];
                    string source = value.SourceId.IsValid ? value.SourceId.ToString() : value.Kind.ToString();
                    return $"{value.NodeId}:{source}={value.Weight:0.###}";
                }));
            string completion = operation.Code == CharacterPoseOperationCode.OutputPose
                ? $" / OutputCompletion {operation.CompletionIdentity} / AppliedAt {trace.FinalAppliedAt}"
                : string.Empty;
            return $"Live{callSite}: {operation.Availability}{invalid} / Weight {operation.OutputWeight:0.###} / Contribution {contribution}{completion}";
        }
    }

    public sealed class CharacterPresentationPoseGraphEditorWindow : GraphAuthoringEditorShell
    {
        readonly struct Page
        {
            public Page(CharacterPresentationPoseGraphAsset owner, CharacterPoseGraphData graph, string displayName)
            {
                Owner = owner;
                Graph = graph;
                DisplayName = displayName;
            }

            public CharacterPresentationPoseGraphAsset Owner { get; }
            public CharacterPoseGraphData Graph { get; }
            public string DisplayName { get; }
        }

        [SerializeField] CharacterPresentationPoseGraphAsset m_Asset;
        [SerializeField] CharacterAnimationPresentationProfile m_Profile;
        [SerializeField] CharacterPresentationProjectionAsset m_Projection;
        [SerializeField] CharacterPipelineDefinition m_Definition;
        [SerializeField] string m_NavigatorSearch = string.Empty;
        [SerializeField] PosePreviewViewportState m_PreviewViewportState = new PosePreviewViewportState();
        [SerializeField] List<PoseGraphWatchViewState> m_PoseWatchStates = new List<PoseGraphWatchViewState>();
        readonly List<Page> m_Pages = new List<Page>();
        PoseGraphView m_GraphView;
        PoseGraphInspectorAdapter m_Inspector;
        PoseGraphMutationAdapter m_Mutation;
        PoseGraphBottomDockAdapter m_BottomDock;
        bool m_Building;

        internal PoseGraphView GraphView => m_GraphView;
        internal string NavigatorSearch
        {
            get => m_NavigatorSearch ?? string.Empty;
            set => m_NavigatorSearch = value ?? string.Empty;
        }
        public CharacterPoseGraphData CurrentGraph => m_Pages.Count > 0 ? m_Pages[m_Pages.Count - 1].Graph : m_Asset?.Graph;
        public CharacterPresentationPoseGraphAsset CurrentOwner => m_Pages.Count > 0 ? m_Pages[m_Pages.Count - 1].Owner : m_Asset;
        public string CurrentDisplayName => m_Pages.Count > 0 ? m_Pages[m_Pages.Count - 1].DisplayName : m_Asset ? m_Asset.name : "Pose Graph";
        public CharacterPresentationPoseGraphAsset ValidationRoot => m_Asset;
        public CharacterAnimationPresentationProfile ProfileContext => m_Profile;
        public CharacterPipelineDefinition DefinitionContext => m_Definition;
        public CharacterAnimationRigDefinition RigDefinition => m_Profile ? m_Profile.RigDefinition : null;
        public string ProjectionRevision => m_Projection ? m_Projection.ProjectionRevision : string.Empty;
        public bool IsSubgraphDocument => CurrentGraph != null && CurrentGraph.Nodes.Any(node =>
            node != null && (node.Kind == CharacterPoseNodeKind.GraphInput || node.Kind == CharacterPoseNodeKind.GraphOutput));
        internal IReadOnlyList<PoseGraphWatchViewState> PoseWatchStates => m_PoseWatchStates;
        internal PosePreviewViewportState PreviewViewportState => m_PreviewViewportState ??= new PosePreviewViewportState();

        internal string ResolveNodeSummary(CharacterPoseNodeDefinition node)
        {
            if (node == null || node.Kind != CharacterPoseNodeKind.BlendSpacePlayer)
                return string.Empty;
            return $"Input Range · {node.BlendSpaceInputRangePolicy}";
        }

        internal string ResolveWorkspaceBuildState()
        {
            if (m_Building)
                return "Building";
            if (!m_Definition || !m_Profile || !m_Asset || m_Asset.Graph == null || !RigDefinition)
                return "Invalid";
            CharacterPoseGraphValidationReport report = CharacterPresentationPoseGraphValidator.Validate(m_Asset, RigDefinition);
            if (!report.IsValid)
                return $"Invalid ({report.Issues.Count})";
            if (EditorUtility.IsDirty(m_Asset) || EditorUtility.IsDirty(m_Profile) ||
                EditorUtility.IsDirty(m_Definition) || EditorUtility.IsDirty(RigDefinition))
            {
                return "Dirty";
            }
            if (!m_Definition.SimulationProgram ||
                !m_Projection ||
                string.IsNullOrWhiteSpace(m_Projection.ProjectionRevision))
            {
                return "Build Required";
            }
            return "Published";
        }

        public static CharacterPresentationPoseGraphEditorWindow Open(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile = null,
            CharacterPresentationProjectionAsset projection = null,
            CharacterPipelineDefinition definition = null)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            CharacterPresentationPoseGraphEditorWindow window = GetWindow<CharacterPresentationPoseGraphEditorWindow>();
            window.titleContent = new GUIContent("Presentation Pose Graph");
            window.SetDocument(asset, profile, projection, definition);
            window.Show();
            window.Focus();
            return window;
        }

        protected override GraphView CreateGraphAuthoringView()
        {
            m_GraphView = new PoseGraphView(this);
            return m_GraphView;
        }

        protected override VisualElement CreateGraphAuthoringInspectorView()
        {
            m_Mutation = new PoseGraphMutationAdapter(this);
            m_Inspector = new PoseGraphInspectorAdapter(this, m_Mutation);
            return m_Inspector.View;
        }

        protected override GraphAuthoringDomainAdapters CreateGraphAuthoringAdapters()
        {
            m_BottomDock = new PoseGraphBottomDockAdapter(this);
            var commands = new[]
            {
                new GraphAuthoringToolbarCommandDescriptor(
                    "compile",
                    "Compile",
                    GraphAuthoringToolbarCommandKind.ExplicitOperation,
                    CompileSemanticIr),
                new GraphAuthoringToolbarCommandDescriptor(
                    "build",
                    "Build",
                    GraphAuthoringToolbarCommandKind.ExplicitOperation,
                    BuildDefinition)
            };
            return new GraphAuthoringDomainAdapters(
                new PoseGraphDocumentAdapter(this),
                new PoseGraphNodeCatalogAdapter(this),
                new PoseGraphPortPolicyAdapter(this),
                m_Mutation,
                m_Inspector,
                new PoseGraphDiagnosticsAdapter(this),
                new GraphAuthoringWorkspaceDescriptor(
                    new GraphAuthoringWorkspaceRegionDescriptor("Navigator", true, 220f, 280f),
                    new GraphAuthoringWorkspaceRegionDescriptor("Details", true, 240f, 360f),
                    new GraphAuthoringWorkspaceRegionDescriptor("Preview / Diagnostics", true, 160f, 260f),
                    commands),
                new PoseGraphNavigatorAdapter(this),
                m_BottomDock);
        }

        protected override void OnGraphAuthoringShellCreated()
        {
            BindGraphAuthoringNavigation(PopPage);
            if (m_Asset)
                ResetPages();
            RefreshGraphView();
        }

        public void SetDocument(
            CharacterPresentationPoseGraphAsset asset,
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationProjectionAsset projection,
            CharacterPipelineDefinition definition = null)
        {
            if (definition && (!profile || definition.AnimationPresentationProfile != profile))
                throw new InvalidOperationException("Pose Graph Definition context does not own the selected Animation Presentation Profile.");
            m_BottomDock?.Clear();
            if (m_Asset != asset)
                m_PoseWatchStates.Clear();
            m_Asset = asset;
            m_Profile = profile;
            m_Projection = projection;
            m_Definition = definition;
            ResetPages();
            RefreshGraphView();
        }

        public void OpenSubgraph(CharacterPoseNodeDefinition node)
        {
            if (node == null || node.Kind != CharacterPoseNodeKind.PoseSubgraph || node.Subgraph == null || !node.Subgraph.IsExclusive)
                return;
            CharacterPresentationPoseGraphAsset owner = node.Subgraph.HasShared ? node.Subgraph.Shared : CurrentOwner;
            CharacterPoseGraphData graph = node.Subgraph.HasInline ? node.Subgraph.Inline : node.Subgraph.Shared.Graph;
            string displayName = string.IsNullOrWhiteSpace(node.DisplayName) ? node.NodeId.Value : node.DisplayName;
            m_Pages.Add(new Page(owner, graph, displayName));
            RefreshGraphView();
        }

        public void RefreshGraphView()
        {
            if (m_GraphView == null)
                return;
            m_GraphView.Populate(CurrentGraph);
            RefreshNavigation();
            RebindGraphAuthoringDocument();
        }

        public void NotifyDocumentMutated()
        {
            m_BottomDock?.Invalidate();
            RebindGraphAuthoringDocument();
        }

        internal void BuildPoseNodeContextMenu(CharacterPoseNodeDefinition node, DropdownMenu menu)
        {
            if (node == null || menu == null)
                return;
            bool hasPoseOutput = node.Ports.Any(port =>
                port != null && port.Kind == CharacterPosePortKind.Pose &&
                port.Direction == CharacterPosePortDirection.Output);
            if (!hasPoseOutput)
            {
                menu.AppendAction("Pose Watch/Unavailable: node has no Pose output", null, DropdownMenuAction.Status.Disabled);
                return;
            }
            IReadOnlyList<AnimationPoseWatchIdentity> identities = ResolvePoseWatchIdentities(node);
            if (identities.Count == 0)
            {
                menu.AppendAction("Pose Watch/Unavailable: explicit Build is required", null, DropdownMenuAction.Status.Disabled);
                return;
            }
            for (int i = 0; i < identities.Count; i++)
            {
                AnimationPoseWatchIdentity identity = identities[i];
                string callSite = string.IsNullOrEmpty(identity.CallSite) ? "Root" : identity.CallSite;
                PoseGraphWatchViewState watched = m_PoseWatchStates.FirstOrDefault(state => state.Matches(identity));
                menu.AppendAction(
                    $"Pose Watch/{callSite}",
                    _ => TogglePoseWatch(identity),
                    _ => watched != null
                        ? DropdownMenuAction.Status.Checked
                        : DropdownMenuAction.Status.Normal);
                if (watched != null)
                {
                    menu.AppendAction(
                        $"Pose Watch/Focus {callSite}",
                        _ => m_BottomDock?.FocusPoseWatch(watched));
                }
            }
        }

        internal void TogglePoseWatch(AnimationPoseWatchIdentity identity)
        {
            int index = m_PoseWatchStates.FindIndex(state => state.Matches(identity));
            if (index >= 0)
                m_PoseWatchStates.RemoveAt(index);
            else
            {
                if (m_PoseWatchStates.Count >= AnimationPoseWatchCapacity.PerWindow)
                {
                    m_BottomDock?.ReportPoseWatchError($"Pose Watch window capacity exceeded: {AnimationPoseWatchCapacity.PerWindow}.");
                    return;
                }
                Color[] palette =
                {
                    Color.cyan,
                    new Color(1f, 0.55f, 0.2f),
                    new Color(0.45f, 1f, 0.45f),
                    new Color(1f, 0.35f, 0.7f),
                    Color.yellow,
                    new Color(0.65f, 0.45f, 1f),
                    new Color(0.3f, 0.8f, 1f),
                    new Color(1f, 0.8f, 0.4f)
                };
                m_PoseWatchStates.Add(new PoseGraphWatchViewState
                {
                    graphId = identity.GraphId,
                    graphRevision = identity.GraphRevision,
                    nodeId = identity.NodeId.Value,
                    callSite = identity.CallSite,
                    color = palette[m_PoseWatchStates.Count % palette.Length],
                    visible = true
                });
            }
            m_BottomDock?.SynchronizePoseWatchInterests();
            m_BottomDock?.RefreshPoseWatchPanel();
        }

        internal void RemovePoseWatch(PoseGraphWatchViewState state)
        {
            if (state == null || !m_PoseWatchStates.Remove(state))
                return;
            m_BottomDock?.SynchronizePoseWatchInterests();
            m_BottomDock?.RefreshPoseWatchPanel();
        }

        IReadOnlyList<AnimationPoseWatchIdentity> ResolvePoseWatchIdentities(CharacterPoseNodeDefinition node)
        {
            var result = new List<AnimationPoseWatchIdentity>();
            if (!m_Definition || !m_Definition.SimulationProgram || !m_Projection ||
                EditorUtility.IsDirty(m_Asset) ||
                EditorUtility.IsDirty(m_Profile) ||
                EditorUtility.IsDirty(m_Definition) ||
                string.IsNullOrWhiteSpace(m_Projection.ProjectionRevision))
                return result;
            CharacterSimulationProgram program = m_Definition.SimulationProgram.Load();
            CharacterPresentationSemanticContract contract = Float32CharacterPresentationContractAdapter.Create(program);
            CharacterPresentationProjection projection = m_Projection.Load(contract);
            CharacterPresentationPosePlan plan = projection.PosePlan;
            for (int i = 0; i < plan.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = plan.Operations[i];
                CharacterPresentationPoseSourceMapEntry source = plan.SourceMap[i];
                if (operation.OutputValueIndex < 0 ||
                    !string.Equals(source.GraphId, CurrentGraph.GraphId, StringComparison.Ordinal) ||
                    !source.NodeId.Equals(node.NodeId))
                    continue;
                result.Add(new AnimationPoseWatchIdentity(
                    source.GraphId,
                    ValidationRoot.Graph.ContentRevision,
                    source.NodeId,
                    source.CallSite));
            }
            return result
                .Distinct()
                .OrderBy(value => value.CallSite, StringComparer.Ordinal)
                .ToArray();
        }

        void CompileSemanticIr()
        {
            if (!m_Definition)
            {
                m_BottomDock?.Report("Compile unavailable: open the Pose Graph with one explicit Character Definition context.");
                return;
            }
            SetBuilding(true);
            string message;
            try
            {
                CharacterSemanticFrontendResult result = CharacterSimulationBuildOrchestrator.CompileSemanticIr(m_Definition, true);
                message = result.IsValid ? "Compile completed." : "Compile failed. Inspect the formal compile report.";
            }
            catch (Exception exception)
            {
                message = $"Compile failed: {exception.Message}";
            }
            finally
            {
                SetBuilding(false);
            }
            m_BottomDock?.Report(message);
        }

        void BuildDefinition()
        {
            if (!m_Definition)
            {
                m_BottomDock?.Report("Build unavailable: open the Pose Graph with one explicit Character Definition context.");
                return;
            }
            SetBuilding(true);
            string message;
            try
            {
                bool success = CharacterSimulationProgramBuildService.Build(m_Definition, true);
                message = success ? "Build completed and published." : "Build failed. Inspect the formal compile report.";
            }
            catch (Exception exception)
            {
                message = $"Build failed: {exception.Message}";
            }
            finally
            {
                SetBuilding(false);
            }
            m_BottomDock?.Report(message);
        }

        void SetBuilding(bool building)
        {
            m_Building = building;
            m_BottomDock?.SetBuilding(building);
        }

        public void ReloadAfterUndo()
        {
            ResetPages();
            if (m_GraphView == null)
                return;
            m_GraphView.Populate(CurrentGraph);
            RefreshNavigation();
        }

        void ResetPages()
        {
            m_Pages.Clear();
            if (m_Asset && m_Asset.Graph != null)
                m_Pages.Add(new Page(m_Asset, m_Asset.Graph, m_Asset.name));
        }

        void PopPage()
        {
            if (m_Pages.Count <= 1)
                return;
            m_Pages.RemoveAt(m_Pages.Count - 1);
            RefreshGraphView();
        }

        void RefreshNavigation()
        {
            GraphAuthoringBreadcrumbEntry[] entries = m_Pages
                .Select(page => new GraphAuthoringBreadcrumbEntry(page.DisplayName, "Pose Graph"))
                .ToArray();
            RenderGraphAuthoringNavigation(entries, PopTo);
        }

        void PopTo(int index)
        {
            if (index < 0 || index >= m_Pages.Count)
                return;
            m_Pages.RemoveRange(index + 1, m_Pages.Count - index - 1);
            RefreshGraphView();
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        static bool OnOpenAsset(int instanceId, int line)
        {
            CharacterPresentationPoseGraphAsset asset = EditorUtility.InstanceIDToObject(instanceId) as CharacterPresentationPoseGraphAsset;
            if (!asset)
                return false;
            Open(asset);
            return true;
        }
    }
}
