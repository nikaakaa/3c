using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using BTSMTL.Timeline.Editor;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
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
    sealed class BlendSpaceSampleNodeView : Node
    {
        readonly Label m_Weight;

        internal BlendSpaceSampleNodeView(CharacterAnimationBlendSpaceSample sample, Vector2 canvasPosition)
        {
            Sample = sample ?? throw new ArgumentNullException(nameof(sample));
            title = sample.Clip ? sample.Clip.name : "Missing Clip";
            viewDataKey = sample.SampleId.Value;
            userData = sample;
            capabilities &= ~Capabilities.Collapsible;
            var identity = new Label(sample.SampleId.Value) { tooltip = sample.SampleId.Value };
            identity.style.maxWidth = 210f;
            identity.style.unityTextAlign = TextAnchor.MiddleLeft;
            extensionContainer.Add(identity);
            extensionContainer.Add(new Label($"({sample.Position.x:0.###}, {sample.Position.y:0.###}) · {sample.Role}"));
            m_Weight = new Label();
            m_Weight.style.unityFontStyleAndWeight = FontStyle.Bold;
            extensionContainer.Add(m_Weight);
            SetPosition(new Rect(canvasPosition, new Vector2(230f, 96f)));
            RefreshExpandedState();
        }

        internal CharacterAnimationBlendSpaceSample Sample { get; }

        internal void SetPreviewWeight(float weight)
        {
            bool active = weight > 0f;
            m_Weight.text = active ? $"Preview {weight:P1}" : string.Empty;
            style.borderLeftWidth = active ? 4f : 0f;
            style.borderLeftColor = active ? new Color(0.15f, 0.85f, 1f) : Color.clear;
        }
    }

    sealed class BlendSpaceCanvasOverlay : VisualElement
    {
        CharacterAnimationBlendSpaceAsset m_Asset;
        IReadOnlyDictionary<CharacterAnimationBlendSpaceSampleId, float> m_Weights;
        Vector2 m_Parameter;

        internal BlendSpaceCanvasOverlay()
        {
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0f;
            style.top = 0f;
            style.width = 1000f;
            style.height = 700f;
            generateVisualContent += Draw;
        }

        internal void SetData(
            CharacterAnimationBlendSpaceAsset asset,
            Vector2 parameter,
            IReadOnlyDictionary<CharacterAnimationBlendSpaceSampleId, float> weights)
        {
            m_Asset = asset;
            m_Parameter = parameter;
            m_Weights = weights;
            RebuildLabels();
            MarkDirtyRepaint();
        }

        void RebuildLabels()
        {
            Clear();
            if (!m_Asset || m_Asset.XAxis == null)
                return;
            AddLabel(
                $"{m_Asset.XAxis.ParameterId.Value} [{m_Asset.XAxis.Unit}]",
                new Vector2(
                    BlendSpaceGraphView.CanvasLeft + BlendSpaceGraphView.CanvasWidth * 0.5f - 90f,
                    BlendSpaceGraphView.CanvasTop + BlendSpaceGraphView.CanvasHeight + 28f),
                180f);
            for (int i = 0; i <= 4; i++)
            {
                float ratio = i * 0.25f;
                float value = Mathf.Lerp(m_Asset.XAxis.Minimum, m_Asset.XAxis.Maximum, ratio);
                AddLabel(
                    value.ToString("0.###"),
                    new Vector2(
                        BlendSpaceGraphView.CanvasLeft + BlendSpaceGraphView.CanvasWidth * ratio - 35f,
                        BlendSpaceGraphView.CanvasTop + BlendSpaceGraphView.CanvasHeight + 6f),
                    70f);
            }
            if (m_Asset.AxisCount != 2 || m_Asset.YAxis == null)
                return;
            AddLabel(
                $"{m_Asset.YAxis.ParameterId.Value} [{m_Asset.YAxis.Unit}]",
                new Vector2(4f, BlendSpaceGraphView.CanvasTop - 32f),
                210f);
            for (int i = 0; i <= 4; i++)
            {
                float ratio = i * 0.25f;
                float value = Mathf.Lerp(m_Asset.YAxis.Maximum, m_Asset.YAxis.Minimum, ratio);
                AddLabel(
                    value.ToString("0.###"),
                    new Vector2(
                        BlendSpaceGraphView.CanvasLeft - 72f,
                        BlendSpaceGraphView.CanvasTop + BlendSpaceGraphView.CanvasHeight * ratio - 9f),
                    64f);
            }
        }

        void AddLabel(string text, Vector2 position, float width)
        {
            var label = new Label(text ?? string.Empty) { pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.left = position.x;
            label.style.top = position.y;
            label.style.width = width;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = new Color(0.72f, 0.72f, 0.72f);
            Add(label);
        }

        void Draw(MeshGenerationContext context)
        {
            if (!m_Asset || m_Asset.XAxis == null)
                return;
            Painter2D painter = context.painter2D;
            painter.strokeColor = new Color(0.42f, 0.42f, 0.42f);
            painter.lineWidth = 1.5f;
            if (m_Asset.AxisCount == 1)
            {
                float y = BlendSpaceGraphView.CanvasTop + BlendSpaceGraphView.CanvasHeight * 0.5f;
                DrawLine(
                    painter,
                    new Vector2(BlendSpaceGraphView.CanvasLeft, y),
                    new Vector2(BlendSpaceGraphView.CanvasLeft + BlendSpaceGraphView.CanvasWidth, y));
                DrawXTicks(painter, y);
            }
            else
            {
                DrawBoundary(painter);
                DrawXTicks(painter, BlendSpaceGraphView.CanvasTop + BlendSpaceGraphView.CanvasHeight);
                DrawYTicks(painter);
                if (m_Asset.Mode == CharacterAnimationBlendSpaceMode.FreeformDirectional2D)
                    DrawDirectionalRings(painter);
            }
            DrawContributions(painter);
        }

        static void DrawLine(Painter2D painter, Vector2 from, Vector2 to)
        {
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }

        static void DrawBoundary(Painter2D painter)
        {
            Vector2 topLeft = new Vector2(BlendSpaceGraphView.CanvasLeft, BlendSpaceGraphView.CanvasTop);
            Vector2 topRight = topLeft + new Vector2(BlendSpaceGraphView.CanvasWidth, 0f);
            Vector2 bottomRight = topLeft + new Vector2(BlendSpaceGraphView.CanvasWidth, BlendSpaceGraphView.CanvasHeight);
            Vector2 bottomLeft = topLeft + new Vector2(0f, BlendSpaceGraphView.CanvasHeight);
            painter.BeginPath();
            painter.MoveTo(topLeft);
            painter.LineTo(topRight);
            painter.LineTo(bottomRight);
            painter.LineTo(bottomLeft);
            painter.LineTo(topLeft);
            painter.Stroke();
        }

        static void DrawXTicks(Painter2D painter, float y)
        {
            for (int i = 0; i <= 4; i++)
            {
                float x = BlendSpaceGraphView.CanvasLeft + BlendSpaceGraphView.CanvasWidth * i * 0.25f;
                DrawLine(painter, new Vector2(x, y - 5f), new Vector2(x, y + 5f));
            }
        }

        static void DrawYTicks(Painter2D painter)
        {
            for (int i = 0; i <= 4; i++)
            {
                float y = BlendSpaceGraphView.CanvasTop + BlendSpaceGraphView.CanvasHeight * i * 0.25f;
                DrawLine(
                    painter,
                    new Vector2(BlendSpaceGraphView.CanvasLeft - 5f, y),
                    new Vector2(BlendSpaceGraphView.CanvasLeft + 5f, y));
            }
        }

        void DrawDirectionalRings(Painter2D painter)
        {
            float radiusX = Mathf.Min(Mathf.Abs(m_Asset.XAxis.Minimum), Mathf.Abs(m_Asset.XAxis.Maximum));
            float radiusY = Mathf.Min(Mathf.Abs(m_Asset.YAxis.Minimum), Mathf.Abs(m_Asset.YAxis.Maximum));
            if (radiusX <= 0f || radiusY <= 0f)
                return;
            painter.strokeColor = new Color(0.3f, 0.42f, 0.48f);
            painter.lineWidth = 1f;
            for (int ring = 1; ring <= 3; ring++)
            {
                float ratio = ring / 3f;
                painter.BeginPath();
                for (int segment = 0; segment <= 48; segment++)
                {
                    float angle = segment / 48f * Mathf.PI * 2f;
                    Vector2 point = BlendSpaceGraphView.ToCanvas(
                        m_Asset,
                        new Vector2(Mathf.Cos(angle) * radiusX * ratio, Mathf.Sin(angle) * radiusY * ratio));
                    if (segment == 0)
                        painter.MoveTo(point);
                    else
                        painter.LineTo(point);
                }
                painter.Stroke();
            }
        }

        void DrawContributions(Painter2D painter)
        {
            if (m_Weights == null || m_Weights.Count == 0)
                return;
            Vector2 preview = BlendSpaceGraphView.ToCanvas(m_Asset, m_Parameter);
            for (int i = 0; i < m_Asset.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = m_Asset.Samples[i];
                if (sample == null || !m_Weights.TryGetValue(sample.SampleId, out float weight) || weight <= 0f)
                    continue;
                painter.strokeColor = new Color(0.15f, 0.85f, 1f, Mathf.Lerp(0.35f, 1f, weight));
                painter.lineWidth = 1f + weight * 5f;
                DrawLine(painter, preview, BlendSpaceGraphView.ToCanvas(m_Asset, sample.Position));
            }
        }
    }

    sealed class BlendSpaceGraphView : GraphView, IGraphAuthoringDomainView
    {
        internal const float CanvasLeft = 80f;
        internal const float CanvasTop = 80f;
        internal const float CanvasWidth = 760f;
        internal const float CanvasHeight = 500f;

        readonly CharacterAnimationBlendSpaceEditorWindow m_Window;
        readonly Dictionary<CharacterAnimationBlendSpaceSampleId, BlendSpaceSampleNodeView> m_Nodes =
            new Dictionary<CharacterAnimationBlendSpaceSampleId, BlendSpaceSampleNodeView>();
        readonly BlendSpaceCanvasOverlay m_CanvasOverlay;
        readonly VisualElement m_PreviewPoint;
        IGraphAuthoringDocument m_Document;
        IGraphAuthoringMutationAdapter m_Mutation;
        bool m_Rebuilding;
        bool m_DraggingPreview;

        internal BlendSpaceGraphView(CharacterAnimationBlendSpaceEditorWindow window)
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
            m_CanvasOverlay = new BlendSpaceCanvasOverlay();
            contentViewContainer.Add(m_CanvasOverlay);
            m_PreviewPoint = new VisualElement
            {
                name = "blend-space-preview-point",
                tooltip = "Compiled Projection preview parameter"
            };
            m_PreviewPoint.style.position = Position.Absolute;
            m_PreviewPoint.style.width = 16f;
            m_PreviewPoint.style.height = 16f;
            m_PreviewPoint.style.borderTopLeftRadius = 8f;
            m_PreviewPoint.style.borderTopRightRadius = 8f;
            m_PreviewPoint.style.borderBottomLeftRadius = 8f;
            m_PreviewPoint.style.borderBottomRightRadius = 8f;
            m_PreviewPoint.style.backgroundColor = Color.yellow;
            m_PreviewPoint.RegisterCallback<PointerDownEvent>(BeginPreviewDrag);
            m_PreviewPoint.RegisterCallback<PointerMoveEvent>(MovePreviewDrag);
            m_PreviewPoint.RegisterCallback<PointerUpEvent>(EndPreviewDrag);
            contentViewContainer.Add(m_PreviewPoint);
        }

        public void BindAdapters(
            IGraphAuthoringDocument document,
            IGraphAuthoringPortPolicy portPolicy,
            IGraphAuthoringMutationAdapter mutation)
        {
            m_Document = document;
            m_Mutation = mutation;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) => new List<Port>();

        internal void Populate(
            CharacterAnimationBlendSpaceAsset asset,
            Vector2 previewParameter,
            IReadOnlyDictionary<CharacterAnimationBlendSpaceSampleId, float> previewWeights)
        {
            CharacterAnimationBlendSpaceSampleId[] selectedIds = selection
                .OfType<BlendSpaceSampleNodeView>()
                .Select(view => view.Sample.SampleId)
                .ToArray();
            m_Rebuilding = true;
            DeleteElements(graphElements.ToList());
            m_Nodes.Clear();
            if (asset)
            {
                for (int i = 0; i < asset.Samples.Count; i++)
                {
                    CharacterAnimationBlendSpaceSample sample = asset.Samples[i];
                    if (sample == null || !sample.SampleId.IsValid)
                        continue;
                    var view = new BlendSpaceSampleNodeView(sample, ToCanvas(asset, sample.Position));
                    m_Nodes.Add(sample.SampleId, view);
                    AddElement(view);
                    view.SetPreviewWeight(previewWeights != null && previewWeights.TryGetValue(sample.SampleId, out float weight) ? weight : 0f);
                }
                SetPreviewPoint(asset, previewParameter);
            }
            m_CanvasOverlay.SetData(asset, previewParameter, previewWeights);
            m_CanvasOverlay.SendToBack();
            m_PreviewPoint.BringToFront();
            for (int i = 0; i < selectedIds.Length; i++)
            {
                if (m_Nodes.TryGetValue(selectedIds[i], out BlendSpaceSampleNodeView selected))
                    AddToSelection(selected);
            }
            m_Rebuilding = false;
        }

        internal Vector2 ToParameter(Vector2 canvasPosition) => FromCanvas(m_Window.Asset, canvasPosition);

        internal bool FocusSample(CharacterAnimationBlendSpaceSampleId sampleId)
        {
            if (!m_Nodes.TryGetValue(sampleId, out BlendSpaceSampleNodeView view))
                return false;
            ClearSelection();
            AddToSelection(view);
            FrameSelection();
            return true;
        }

        internal void SetPreviewPoint(CharacterAnimationBlendSpaceAsset asset, Vector2 parameter)
        {
            if (!asset)
            {
                m_PreviewPoint.style.display = DisplayStyle.None;
                return;
            }
            Vector2 point = ToCanvas(asset, parameter);
            m_PreviewPoint.style.left = point.x - 8f;
            m_PreviewPoint.style.top = point.y - 8f;
            m_PreviewPoint.style.display = DisplayStyle.Flex;
        }

        internal void SetPreviewEvaluation(
            CharacterAnimationBlendSpaceAsset asset,
            Vector2 parameter,
            IReadOnlyDictionary<CharacterAnimationBlendSpaceSampleId, float> previewWeights)
        {
            foreach (KeyValuePair<CharacterAnimationBlendSpaceSampleId, BlendSpaceSampleNodeView> pair in m_Nodes)
            {
                pair.Value.SetPreviewWeight(
                    previewWeights != null && previewWeights.TryGetValue(pair.Key, out float weight) ? weight : 0f);
            }
            SetPreviewPoint(asset, parameter);
            m_CanvasOverlay.SetData(asset, parameter, previewWeights);
            m_CanvasOverlay.SendToBack();
            m_PreviewPoint.BringToFront();
        }

        void BeginPreviewDrag(PointerDownEvent evt)
        {
            if (evt.button != 0 || !m_Window.Asset)
                return;
            m_DraggingPreview = true;
            m_PreviewPoint.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void MovePreviewDrag(PointerMoveEvent evt)
        {
            if (!m_DraggingPreview || !m_PreviewPoint.HasPointerCapture(evt.pointerId))
                return;
            Vector2 canvas = contentViewContainer.WorldToLocal(evt.position);
            m_Window.SetTransientPreviewParameter(FromCanvas(m_Window.Asset, canvas));
            evt.StopPropagation();
        }

        void EndPreviewDrag(PointerUpEvent evt)
        {
            if (!m_DraggingPreview || evt.button != 0)
                return;
            m_DraggingPreview = false;
            if (m_PreviewPoint.HasPointerCapture(evt.pointerId))
                m_PreviewPoint.ReleasePointer(evt.pointerId);
            m_Window.CommitTransientPreviewParameter();
            evt.StopPropagation();
        }

        internal static Vector2 ToCanvas(CharacterAnimationBlendSpaceAsset asset, Vector2 parameter)
        {
            if (!asset || asset.XAxis == null || !asset.XAxis.ParameterId.IsValid)
                return new Vector2(CanvasLeft, CanvasTop + CanvasHeight * 0.5f);
            float x = Mathf.InverseLerp(asset.XAxis.Minimum, asset.XAxis.Maximum, parameter.x);
            float y = asset.AxisCount == 1 || asset.YAxis == null
                ? 0.5f
                : 1f - Mathf.InverseLerp(asset.YAxis.Minimum, asset.YAxis.Maximum, parameter.y);
            return new Vector2(CanvasLeft + x * CanvasWidth, CanvasTop + y * CanvasHeight);
        }

        static Vector2 FromCanvas(CharacterAnimationBlendSpaceAsset asset, Vector2 canvasPosition)
        {
            if (!asset || asset.XAxis == null || !asset.XAxis.ParameterId.IsValid)
                return Vector2.zero;
            float x = Mathf.Clamp01((canvasPosition.x - CanvasLeft) / CanvasWidth);
            float valueX = Mathf.Lerp(asset.XAxis.Minimum, asset.XAxis.Maximum, x);
            if (asset.AxisCount == 1 || asset.YAxis == null)
                return new Vector2(valueX, 0f);
            float y = 1f - Mathf.Clamp01((canvasPosition.y - CanvasTop) / CanvasHeight);
            return new Vector2(valueX, Mathf.Lerp(asset.YAxis.Minimum, asset.YAxis.Maximum, y));
        }

        GraphViewChange ApplyChange(GraphViewChange change)
        {
            if (m_Rebuilding || m_Mutation == null)
                return change;
            return m_Mutation.ApplyGraphViewChange(m_Document, change);
        }
    }

    sealed class BlendSpaceDocumentAdapter : IGraphAuthoringDocument
    {
        readonly CharacterAnimationBlendSpaceEditorWindow m_Window;
        internal BlendSpaceDocumentAdapter(CharacterAnimationBlendSpaceEditorWindow window) { m_Window = window; }
        public string DomainId => "character-animation-blend-space";
        public string DocumentId => m_Window.Asset ? m_Window.Asset.BlendSpaceId.Value : string.Empty;
        public string DisplayName => m_Window.Asset ? m_Window.Asset.name : "Animation Blend Space";
        public string ContentRevision => m_Window.Asset ? m_Window.Asset.ContentRevision : string.Empty;
        public UnityEngine.Object SerializedOwner => m_Window.Asset;
    }

    sealed class BlendSpaceNodeCatalogAdapter : IGraphAuthoringNodeCatalog
    {
        public IReadOnlyList<GraphAuthoringNodeCatalogEntry> GetEntries(IGraphAuthoringDocument document) =>
            new[] { new GraphAuthoringNodeCatalogEntry("Samples/Sample", "sample") };
    }

    sealed class BlendSpacePortPolicyAdapter : IGraphAuthoringPortPolicy
    {
        public bool CanConnect(IGraphAuthoringDocument document, Port startPort, Port endPort) => false;
    }

    [Serializable]
    sealed class BlendSpaceClipboardPayload
    {
        public string[] sampleIds = Array.Empty<string>();
        public Vector2 center;
    }

    sealed class BlendSpaceMutationAdapter : IGraphAuthoringMutationAdapter
    {
        readonly CharacterAnimationBlendSpaceEditorWindow m_Window;
        internal BlendSpaceMutationAdapter(CharacterAnimationBlendSpaceEditorWindow window) { m_Window = window; }
        public bool ReadOnly => false;

        public void CreateNode(IGraphAuthoringDocument document, string typeId, Vector2 graphPosition)
        {
            if (!string.Equals(typeId, "sample", StringComparison.Ordinal))
                throw new InvalidOperationException($"Blend Space node type '{typeId}' is unknown.");
            m_Window.ApplyMutation(() => CharacterAnimationBlendSpaceAuthoringService.CreateSample(
                m_Window.Asset,
                m_Window.GraphView.ToParameter(graphPosition)));
        }

        public GraphViewChange ApplyGraphViewChange(IGraphAuthoringDocument document, GraphViewChange change)
        {
            if (change.elementsToRemove != null)
            {
                CharacterAnimationBlendSpaceSampleId[] ids = change.elementsToRemove
                    .OfType<BlendSpaceSampleNodeView>()
                    .Select(view => view.Sample.SampleId)
                    .Distinct()
                    .ToArray();
                if (ids.Length > 0)
                    m_Window.ApplyMutation(() => CharacterAnimationBlendSpaceAuthoringService.DeleteSamples(m_Window.Asset, ids));
            }
            if (change.movedElements != null)
            {
                Dictionary<CharacterAnimationBlendSpaceSampleId, Vector2> positions = change.movedElements
                    .OfType<BlendSpaceSampleNodeView>()
                    .ToDictionary(
                        view => view.Sample.SampleId,
                        view => m_Window.GraphView.ToParameter(view.GetPosition().position));
                if (positions.Count > 0)
                    m_Window.ApplyMutation(() => CharacterAnimationBlendSpaceAuthoringService.SetSamplePositions(m_Window.Asset, positions));
            }
            return change;
        }

        public string SerializeSelection(IGraphAuthoringDocument document, IEnumerable<GraphElement> elements)
        {
            BlendSpaceSampleNodeView[] nodes = elements?.OfType<BlendSpaceSampleNodeView>().ToArray() ?? Array.Empty<BlendSpaceSampleNodeView>();
            return JsonUtility.ToJson(new BlendSpaceClipboardPayload
            {
                sampleIds = nodes.Select(node => node.Sample.SampleId.Value).ToArray(),
                center = nodes.Length == 0 ? Vector2.zero : nodes.Aggregate(Vector2.zero, (value, node) => value + node.Sample.Position) / nodes.Length
            });
        }

        public bool CanPaste(IGraphAuthoringDocument document, string payload)
        {
            try
            {
                BlendSpaceClipboardPayload data = JsonUtility.FromJson<BlendSpaceClipboardPayload>(payload);
                return data?.sampleIds != null && data.sampleIds.Length > 0 && data.sampleIds.All(id =>
                    !string.IsNullOrWhiteSpace(id) && m_Window.Asset.FindSample(new CharacterAnimationBlendSpaceSampleId(id)) != null);
            }
            catch
            {
                return false;
            }
        }

        public void Paste(IGraphAuthoringDocument document, string operationName, string payload)
        {
            if (!CanPaste(document, payload))
                throw new InvalidOperationException("Blend Space clipboard payload is invalid for this asset.");
            BlendSpaceClipboardPayload data = JsonUtility.FromJson<BlendSpaceClipboardPayload>(payload);
            Vector2 target = m_Window.Asset.Preview.Parameter;
            Vector2 offset = target - data.center;
            m_Window.ApplyMutation(() =>
            {
                for (int i = 0; i < data.sampleIds.Length; i++)
                {
                    var id = new CharacterAnimationBlendSpaceSampleId(data.sampleIds[i]);
                    CharacterAnimationBlendSpaceSample source = m_Window.Asset.FindSample(id);
                    CharacterAnimationBlendSpaceAuthoringService.DuplicateSample(m_Window.Asset, id, source.Position + offset);
                }
            });
        }

        public void Reload(IGraphAuthoringDocument document) => m_Window.RefreshWorkspace();
    }

    sealed class BlendSpaceInspectorAdapter : IGraphAuthoringInspectorAdapter, IGraphAuthoringWorkspacePageAdapter
    {
        readonly CharacterAnimationBlendSpaceEditorWindow m_Window;
        readonly VisualElement m_Root = new VisualElement();
        readonly VisualElement m_Content = new ScrollView();
        readonly ToolbarToggle m_Authoring;
        readonly ToolbarToggle m_Live;
        readonly ToolbarToggle m_References;
        CharacterAnimationBlendSpaceSampleId[] m_Selection = Array.Empty<CharacterAnimationBlendSpaceSampleId>();
        string m_Page = "authoring";

        internal BlendSpaceInspectorAdapter(CharacterAnimationBlendSpaceEditorWindow window)
        {
            m_Window = window;
            var toolbar = new Toolbar();
            m_Authoring = CreatePageToggle(toolbar, "Authoring", "authoring");
            m_Live = CreatePageToggle(toolbar, "Live", "live");
            m_References = CreatePageToggle(toolbar, "References", "references");
            m_Root.Add(toolbar);
            m_Content.style.flexGrow = 1f;
            m_Root.Add(m_Content);
        }

        public VisualElement View => m_Root;
        public string ActivePageId => m_Page;
        public void Bind(IGraphAuthoringDocument document) => Rebuild();

        public void Inspect(IReadOnlyList<ISelectable> selection)
        {
            m_Selection = selection?.OfType<BlendSpaceSampleNodeView>()
                .Select(view => view.Sample.SampleId)
                .Distinct()
                .ToArray() ?? Array.Empty<CharacterAnimationBlendSpaceSampleId>();
            Rebuild();
        }

        public void Clear() => m_Content.Clear();

        public void RestorePage(string pageId)
        {
            m_Page = pageId == "live" || pageId == "references" ? pageId : "authoring";
            Rebuild();
        }

        ToolbarToggle CreatePageToggle(Toolbar toolbar, string label, string page)
        {
            var toggle = new ToolbarToggle { text = label };
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue)
                    return;
                m_Page = page;
                Rebuild();
            });
            toolbar.Add(toggle);
            return toggle;
        }

        void Rebuild()
        {
            m_Content.Clear();
            m_Authoring.SetValueWithoutNotify(m_Page == "authoring");
            m_Live.SetValueWithoutNotify(m_Page == "live");
            m_References.SetValueWithoutNotify(m_Page == "references");
            m_Window.SetAnimationDiagnosticsInterest(
                m_Page == "live");
            if (!m_Window.Asset)
                return;
            if (m_Page == "live")
                DrawLive();
            else if (m_Page == "references")
                DrawReferences();
            else if (m_Selection.Length == 1)
                DrawSample(m_Window.Asset.FindSample(m_Selection[0]));
            else if (m_Selection.Length > 1)
                DrawMultiple();
            else
                DrawAsset();
        }

        void DrawAsset()
        {
            CharacterAnimationBlendSpaceAsset asset = m_Window.Asset;
            AddReadOnly("Asset", asset.name);
            var rig = new ObjectField("Rig") { objectType = typeof(CharacterAnimationRigDefinition), value = asset.Rig };
            rig.RegisterValueChangedCallback(evt => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.SetRig(asset, evt.newValue as CharacterAnimationRigDefinition)));
            m_Content.Add(rig);
            var mode = new EnumField("Mode", asset.Mode);
            mode.RegisterValueChangedCallback(evt => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.SetMode(asset, (CharacterAnimationBlendSpaceMode)evt.newValue)));
            m_Content.Add(mode);
            DrawAxis(asset, 0, asset.XAxis, "X Axis");
            if (asset.AxisCount == 2)
                DrawAxis(asset, 1, asset.YAxis, "Y Axis");
            var phase = new EnumField("Phase", asset.PhasePolicy);
            m_Content.Add(phase);
            var reference = new TextField("Phase Reference SampleId") { value = asset.PhaseReferenceSampleId.Value };
            m_Content.Add(reference);
            var applyPhase = new Button(() => m_Window.ApplyMutation(() =>
            {
                CharacterAnimationBlendSpacePhasePolicy policy = (CharacterAnimationBlendSpacePhasePolicy)phase.value;
                CharacterAnimationBlendSpaceSampleId id = policy == CharacterAnimationBlendSpacePhasePolicy.LocomotionPhase ||
                                                          policy == CharacterAnimationBlendSpacePhasePolicy.LocomotionPhase
                    ? new CharacterAnimationBlendSpaceSampleId(reference.value)
                    : default;
                CharacterAnimationBlendSpaceAuthoringService.SetPhase(asset, policy, id);
            })) { text = "Apply Phase" };
            m_Content.Add(applyPhase);
            DrawPolicies(asset);
            DrawFootAnalysis(default);
        }

        void DrawAxis(CharacterAnimationBlendSpaceAsset asset, int index, CharacterAnimationBlendSpaceAxis axis, string title)
        {
            var foldout = new Foldout { text = title, value = true };
            var parameter = new TextField("ParameterId") { value = axis?.ParameterId.Value ?? string.Empty };
            var unit = new TextField("Unit") { value = axis?.Unit ?? string.Empty };
            var minimum = new FloatField("Minimum") { value = axis?.Minimum ?? 0f };
            var maximum = new FloatField("Maximum") { value = axis?.Maximum ?? 1f };
            foldout.Add(parameter);
            foldout.Add(unit);
            foldout.Add(minimum);
            foldout.Add(maximum);
            foldout.Add(new Button(() => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.SetAxis(
                    asset,
                    index,
                    new PoseParameterId(parameter.value),
                    unit.value,
                    minimum.value,
                    maximum.value))) { text = "Apply Axis" });
            m_Content.Add(foldout);
        }

        void DrawSample(CharacterAnimationBlendSpaceSample sample)
        {
            if (sample == null)
                return;
            AddReadOnly("SampleId", sample.SampleId.Value);
            var clip = new ObjectField("AnimationClip")
            {
                objectType = typeof(AnimationClip),
                value = sample.Clip,
                allowSceneObjects = false
            };
            clip.RegisterValueChangedCallback(evt => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.SetSampleClip(
                    m_Window.Asset,
                    sample.SampleId,
                    evt.newValue as AnimationClip)));
            m_Content.Add(clip);
            var pingClip = new Button(() => EditorGUIUtility.PingObject(sample.Clip)) { text = "Ping Clip" };
            pingClip.SetEnabled(sample.Clip);
            m_Content.Add(pingClip);
            var position = new Vector2Field("Position") { value = sample.Position };
            m_Content.Add(position);
            m_Content.Add(new Button(() => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.SetSamplePosition(m_Window.Asset, sample.SampleId, position.value))) { text = "Apply Position" });
            var role = new EnumField("Role", sample.Role);
            var time = new Slider("Stationary Time", 0f, 1f) { value = sample.StationaryNormalizedTime, showInputField = true };
            m_Content.Add(role);
            m_Content.Add(time);
            m_Content.Add(new Button(() => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.SetSampleRole(
                    m_Window.Asset,
                    sample.SampleId,
                    (CharacterAnimationBlendSpaceSampleRole)role.value,
                    time.value))) { text = "Apply Role" });
            DrawSampleParameters(sample);
            var commands = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            commands.Add(new Button(() => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.DuplicateSample(m_Window.Asset, sample.SampleId, sample.Position))) { text = "Duplicate" });
            commands.Add(new Button(() => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.DeleteSample(m_Window.Asset, sample.SampleId))) { text = "Delete" });
            m_Content.Add(commands);
        }

        void DrawFootAnalysis(CharacterAnimationBlendSpaceSampleId selectedSampleId)
        {
            var foldout = new Foldout { text = "Foot Analysis Artifacts", value = false };
            CharacterAnimationPresentationProfile profile = m_Window.Profile;
            if (!profile)
            {
                foldout.Add(new HelpBox("Unavailable: no exact Presentation Profile context.", HelpBoxMessageType.Warning));
                m_Content.Add(foldout);
                return;
            }
            if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.Disabled)
            {
                foldout.Add(new Label("Disabled by Presentation Profile"));
                m_Content.Add(foldout);
                return;
            }
            IReadOnlyList<CharacterFootAnalysisArtifactDiagnostic> diagnostics =
                CharacterProjectionFootAnalysisResolver.InspectBlendSpace(profile, m_Window.Asset);
            string selectedKey = selectedSampleId.IsValid
                ? AnimationFootAnalysisProjectionBuildData.BlendSpaceBindingKey(m_Window.Asset.BlendSpaceId, selectedSampleId)
                : string.Empty;
            for (int i = 0; i < diagnostics.Count; i++)
            {
                CharacterFootAnalysisArtifactDiagnostic diagnostic = diagnostics[i];
                if (selectedSampleId.IsValid && !string.Equals(diagnostic.BindingKey, selectedKey, StringComparison.Ordinal))
                    continue;
                HelpBoxMessageType type = diagnostic.Status == AnimationFootAnalysisArtifactStatus.Ready
                    ? HelpBoxMessageType.Info
                    : diagnostic.Status == AnimationFootAnalysisArtifactStatus.Corrupt
                        ? HelpBoxMessageType.Error
                        : HelpBoxMessageType.Warning;
                foldout.Add(new HelpBox(
                    $"{diagnostic.Status} · {diagnostic.BindingKey}\n{diagnostic.Message}",
                    type));
            }
            m_Content.Add(foldout);
        }

        void DrawMultiple()
        {
            AddReadOnly("Selected Samples", m_Selection.Length.ToString());
            var delta = new Vector2Field("Position Delta");
            m_Content.Add(delta);
            m_Content.Add(new Button(() => m_Window.ApplyMutation(() =>
            {
                var positions = new Dictionary<CharacterAnimationBlendSpaceSampleId, Vector2>();
                for (int i = 0; i < m_Selection.Length; i++)
                {
                    CharacterAnimationBlendSpaceSample sample = m_Window.Asset.FindSample(m_Selection[i]);
                    if (sample != null)
                        positions.Add(sample.SampleId, sample.Position + delta.value);
                }
                CharacterAnimationBlendSpaceAuthoringService.SetSamplePositions(m_Window.Asset, positions);
            })) { text = "Apply Delta" });
            m_Content.Add(new Button(() => m_Window.ApplyMutation(() =>
                CharacterAnimationBlendSpaceAuthoringService.DeleteSamples(m_Window.Asset, m_Selection))) { text = "Delete Selected" });
        }

        void DrawSampleParameters(CharacterAnimationBlendSpaceSample sample)
        {
            var foldout = new Foldout { text = "Pose Parameters", value = false };
            var ids = new List<TextField>();
            var values = new List<FloatField>();
            for (int i = 0; i < sample.Parameters.Count; i++)
            {
                int parameterIndex = i;
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                var id = new TextField { value = sample.Parameters[i].ParameterId.Value };
                id.style.flexGrow = 1f;
                var value = new FloatField { value = sample.Parameters[i].Value };
                value.style.width = 90f;
                ids.Add(id);
                values.Add(value);
                row.Add(id);
                row.Add(value);
                row.Add(new Button(() => m_Window.ApplyMutation(() =>
                    CharacterAnimationBlendSpaceAuthoringService.SetSampleParameters(
                        m_Window.Asset,
                        sample.SampleId,
                        sample.Parameters.Where((_, index) => index != parameterIndex).ToArray()))) { text = "−" });
                foldout.Add(row);
            }
            List<string> availablePolicies = m_Window.Asset.PoseParameterPolicies
                .Where(policy => policy != null && !sample.Parameters.Any(value => value.ParameterId.Equals(policy.ParameterId)))
                .Select(policy => policy.ParameterId.Value)
                .ToList();
            if (availablePolicies.Count > 0)
            {
                var addParameter = new PopupField<string>("Add Parameter", availablePolicies, 0);
                foldout.Add(addParameter);
                foldout.Add(new Button(() => m_Window.ApplyMutation(() =>
                {
                    CharacterAnimationBlendSpaceSampleParameter[] parameters = sample.Parameters
                        .Concat(new[] { new CharacterAnimationBlendSpaceSampleParameter(new PoseParameterId(addParameter.value), 0f) })
                        .ToArray();
                    CharacterAnimationBlendSpaceAuthoringService.SetSampleParameters(m_Window.Asset, sample.SampleId, parameters);
                })) { text = "Add Parameter" });
            }
            foldout.Add(new Button(() => m_Window.ApplyMutation(() =>
            {
                CharacterAnimationBlendSpaceSampleParameter[] parameters = ids
                    .Select((id, index) => new CharacterAnimationBlendSpaceSampleParameter(new PoseParameterId(id.value), values[index].value))
                    .ToArray();
                CharacterAnimationBlendSpaceAuthoringService.SetSampleParameters(m_Window.Asset, sample.SampleId, parameters);
            })) { text = "Apply Parameters" });
            m_Content.Add(foldout);
        }

        void DrawPolicies(CharacterAnimationBlendSpaceAsset asset)
        {
            var foldout = new Foldout { text = "Pose Parameter Policies", value = false };
            var ids = new List<TextField>();
            var policies = new List<EnumField>();
            for (int i = 0; i < asset.PoseParameterPolicies.Count; i++)
            {
                int policyIndex = i;
                var id = new TextField("ParameterId") { value = asset.PoseParameterPolicies[i].ParameterId.Value };
                var policy = new EnumField("Policy", asset.PoseParameterPolicies[i].Policy);
                ids.Add(id);
                policies.Add(policy);
                foldout.Add(id);
                foldout.Add(policy);
                foldout.Add(new Button(() => m_Window.ApplyMutation(() =>
                    CharacterAnimationBlendSpaceAuthoringService.ReplacePoseParameterPolicies(
                        asset,
                        asset.PoseParameterPolicies.Where((_, index) => index != policyIndex).ToArray()))) { text = "Remove Policy" });
            }
            var newId = new TextField("New ParameterId");
            var newPolicy = new EnumField("New Policy", CharacterAnimationBlendSpaceParameterPolicy.WeightedAvailableSamples);
            foldout.Add(newId);
            foldout.Add(newPolicy);
            foldout.Add(new Button(() => m_Window.ApplyMutation(() =>
            {
                CharacterAnimationBlendSpacePoseParameterPolicy[] values = asset.PoseParameterPolicies
                    .Concat(new[]
                    {
                        new CharacterAnimationBlendSpacePoseParameterPolicy(
                            new PoseParameterId(newId.value),
                            (CharacterAnimationBlendSpaceParameterPolicy)newPolicy.value)
                    })
                    .ToArray();
                CharacterAnimationBlendSpaceAuthoringService.ReplacePoseParameterPolicies(asset, values);
            })) { text = "Add Policy" });
            foldout.Add(new Button(() => m_Window.ApplyMutation(() =>
            {
                CharacterAnimationBlendSpacePoseParameterPolicy[] values = ids
                    .Select((id, index) => new CharacterAnimationBlendSpacePoseParameterPolicy(
                        new PoseParameterId(id.value),
                        (CharacterAnimationBlendSpaceParameterPolicy)policies[index].value))
                    .ToArray();
                CharacterAnimationBlendSpaceAuthoringService.ReplacePoseParameterPolicies(asset, values);
            })) { text = "Apply Policies" });
            m_Content.Add(foldout);
        }

        void DrawLive()
        {
            AddReadOnly("Source", "AnimationPresentationRuntimeSnapshot");
            if (!m_Window.TryGetRuntimeSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string status))
            {
                AddReadOnly("Status", status);
                return;
            }
            AddReadOnly("Status", "Ready");
            AddReadOnly("Projection Revision", m_Window.Projection ? m_Window.Projection.ProjectionRevision : "Unavailable");
            bool found = false;
            for (int playerIndex = 0; playerIndex < snapshot.BlendSpacePlayers.Count; playerIndex++)
            {
                AnimationBlendSpacePlayerRuntimeSnapshot player = snapshot.BlendSpacePlayers[playerIndex];
                if (!player.BlendSpaceId.Equals(m_Window.Asset.BlendSpaceId))
                    continue;
                found = true;
                AddReadOnly("NodeId", player.NodeId.Value);
                AddReadOnly("Source", player.SourceId.ToString());
                AddReadOnly("Parameter", $"raw ({player.RawX:0.###}, {player.RawY:0.###}) / processed ({player.X:0.###}, {player.Y:0.###})");
                AddReadOnly("Canonical Phase", $"{player.CanonicalPhase.NormalizedPhase:0.###} / cycle {player.CanonicalPhase.Cycle}");
                AddReadOnly("Pose Result", $"{player.PoseAvailability} / {player.InvalidReason}");
                AnimationReadOnlyBuffer<AnimationBlendSpaceSampleRuntimeSnapshot> samples = snapshot.GetBlendSpaceSamples(playerIndex);
                for (int sampleIndex = 0; sampleIndex < samples.Count; sampleIndex++)
                {
                    AnimationBlendSpaceSampleRuntimeSnapshot sample = samples[sampleIndex];
                    AddReadOnly(
                        $"Sample {sampleIndex + 1}",
                        $"{sample.SampleId} / {sample.Weight:P2} / {sample.ClipTime:0.000}s / feature {(sample.HasFootFeatures ? $"{sample.FootAnalysisSourceId}@{sample.FootAnalysisVersion}/{sample.FootArtifactContentHash}" : "Unavailable")}");
                }
            }
            if (!found)
                AddReadOnly("Runtime Values", "Unavailable: the attached frame has no matching BlendSpacePlayer source.");
            for (int i = 0; i < snapshot.Parameters.Count; i++)
            {
                AnimationPoseParameterSnapshot parameter = snapshot.Parameters[i];
                AddReadOnly(
                    $"Parameter {parameter.ParameterId}",
                    parameter.Available ? parameter.Value.ToString("0.###") : "Unavailable");
            }
        }

        void DrawReferences()
        {
            AddReadOnly("Authoring Source", $"CharacterAnimationBlendSpaceAsset {m_Window.Asset.BlendSpaceId}@{m_Window.Asset.ContentRevision}");
            AddObject("Definition", m_Window.Definition);
            AddObject("Presentation Profile", m_Window.Profile);
            AddObject("Pose Graph", m_Window.Profile ? m_Window.Profile.PoseGraph : null);
            AddObject("Rig", m_Window.Asset.Rig);
            AddObject("Projection", m_Window.Projection);
            AddReadOnly("Projection Revision", m_Window.Projection ? m_Window.Projection.ProjectionRevision : "Unavailable");
            for (int i = 0; i < m_Window.Asset.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = m_Window.Asset.Samples[i];
                if (sample == null)
                    continue;
                AddObject($"Sample {sample.SampleId}", sample.Clip);
            }
            if (m_Window.Profile)
            {
                IReadOnlyList<CharacterFootAnalysisArtifactDiagnostic> diagnostics =
                    CharacterProjectionFootAnalysisResolver.InspectBlendSpace(m_Window.Profile, m_Window.Asset);
                for (int i = 0; i < diagnostics.Count; i++)
                    AddReadOnly($"Artifact {i + 1}", $"{diagnostics[i].Status} · {diagnostics[i].BindingKey}");
            }
            if (m_Window.Definition && m_Window.Definition.SimulationProgram && m_Window.Projection)
            {
                try
                {
                    CharacterSimulationProgram program = m_Window.Definition.SimulationProgram.Load();
                    CharacterPresentationProjection projection = m_Window.Projection.Load(
                        Float32CharacterPresentationContractAdapter.Create(program));
                    for (int playerIndex = 0; playerIndex < projection.BlendSpacePlayers.Count; playerIndex++)
                    {
                        CharacterAnimationBlendSpacePlayerPlan player = projection.BlendSpacePlayers[playerIndex];
                        bool matches =
                            player.BlendSpacePlanIndex >= 0 &&
                            player.BlendSpacePlanIndex <
                            projection.BlendSpaces.Count &&
                            projection.BlendSpaces[
                                player.BlendSpacePlanIndex]
                            .BlendSpaceId.Equals(
                                m_Window.Asset.BlendSpaceId);
                        if (matches)
                            AddReadOnly($"Pose Graph Node {playerIndex + 1}", player.NodeId.Value);
                    }
                }
                catch (Exception exception)
                {
                    AddReadOnly("Compiled References", $"Unavailable · {exception.Message}");
                }
            }
        }

        void AddReadOnly(string label, string value)
        {
            var field = new TextField(label) { value = value ?? string.Empty, isReadOnly = true };
            m_Content.Add(field);
        }

        void AddObject(string label, UnityEngine.Object value)
        {
            var field = new ObjectField(label) { objectType = typeof(UnityEngine.Object), value = value };
            field.SetEnabled(false);
            m_Content.Add(field);
        }
    }

    sealed class BlendSpaceNavigatorAdapter : IGraphAuthoringWorkspaceRegionAdapter
    {
        readonly CharacterAnimationBlendSpaceEditorWindow m_Window;
        readonly ScrollView m_Root = new ScrollView();
        internal BlendSpaceNavigatorAdapter(CharacterAnimationBlendSpaceEditorWindow window) { m_Window = window; }
        public VisualElement View => m_Root;
        public void Bind(IGraphAuthoringDocument document) => Refresh();
        public void Clear() => m_Root.Clear();

        public void Refresh()
        {
            m_Root.Clear();
            if (!m_Window.Asset)
                return;
            m_Root.Add(new Label($"Asset · {m_Window.Asset.name}"));
            m_Root.Add(new Label($"Mode · {m_Window.Asset.Mode}"));
            m_Root.Add(new Label($"X · {m_Window.Asset.XAxis?.ParameterId}"));
            if (m_Window.Asset.AxisCount == 2)
                m_Root.Add(new Label($"Y · {m_Window.Asset.YAxis?.ParameterId}"));
            var samples = new Foldout { text = $"Samples ({m_Window.Asset.Samples.Count})", value = true };
            for (int i = 0; i < m_Window.Asset.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = m_Window.Asset.Samples[i];
                if (sample == null)
                    continue;
                CharacterAnimationBlendSpaceSampleId id = sample.SampleId;
                samples.Add(new Button(() => m_Window.GraphView.FocusSample(id))
                {
                    text = $"{sample.Clip?.name ?? "Missing Clip"} · {id}"
                });
            }
            m_Root.Add(samples);
            var compiled = new Foldout { text = "Compiled Plan", value = true };
            if (m_Window.TryResolveCompiledPlan(out CharacterAnimationBlendSpacePlan plan, out string status))
            {
                compiled.Add(new Label(plan.PlanIdentity));
                compiled.Add(new Label($"{plan.Mode} · {plan.Samples.Count} samples · {plan.PhasePolicy}"));
            }
            else
            {
                compiled.Add(new Label(status));
            }
            m_Root.Add(compiled);
        }
    }

    sealed class BlendSpaceBottomDockAdapter : IGraphAuthoringWorkspaceRegionAdapter, IGraphAuthoringWorkspacePageAdapter
    {
        readonly CharacterAnimationBlendSpaceEditorWindow m_Window;
        readonly VisualElement m_Root = new VisualElement();
        readonly VisualElement m_Content = new ScrollView();
        readonly ToolbarToggle m_Preview;
        readonly ToolbarToggle m_Diagnostics;
        readonly ToolbarToggle m_References;
        string m_Page = "preview";
        string m_Report = string.Empty;

        internal BlendSpaceBottomDockAdapter(CharacterAnimationBlendSpaceEditorWindow window)
        {
            m_Window = window;
            var toolbar = new Toolbar();
            m_Preview = AddToggle(toolbar, "Preview", "preview");
            m_Diagnostics = AddToggle(toolbar, "Diagnostics", "diagnostics");
            m_References = AddToggle(toolbar, "Pose Watch / References", "references");
            m_Root.Add(toolbar);
            m_Root.Add(m_Content);
        }

        public VisualElement View => m_Root;
        public string ActivePageId => m_Page;
        public void Bind(IGraphAuthoringDocument document) => Refresh();
        public void Clear() => m_Content.Clear();

        public void RestorePage(string pageId)
        {
            m_Page = pageId == "diagnostics" || pageId == "references" ? pageId : "preview";
            Refresh();
        }

        internal void Report(string message)
        {
            m_Report = message ?? string.Empty;
            m_Page = "diagnostics";
            Refresh();
        }

        public void Refresh()
        {
            m_Content.Clear();
            m_Preview.SetValueWithoutNotify(m_Page == "preview");
            m_Diagnostics.SetValueWithoutNotify(m_Page == "diagnostics");
            m_References.SetValueWithoutNotify(m_Page == "references");
            if (!m_Window.Asset)
                return;
            if (m_Page == "diagnostics")
                DrawDiagnostics();
            else if (m_Page == "references")
                DrawReferences();
            else
                DrawPreview();
        }

        ToolbarToggle AddToggle(Toolbar toolbar, string text, string page)
        {
            var toggle = new ToolbarToggle { text = text };
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (!evt.newValue)
                    return;
                m_Page = page;
                Refresh();
            });
            toolbar.Add(toggle);
            return toggle;
        }

        void DrawPreview()
        {
            CharacterAnimationBlendSpaceAsset asset = m_Window.Asset;
            var x = new Slider($"{asset.XAxis?.ParameterId} ({asset.XAxis?.Unit})", asset.XAxis?.Minimum ?? 0f, asset.XAxis?.Maximum ?? 1f)
            {
                value = m_Window.PreviewParameter.x,
                showInputField = true
            };
            m_Content.Add(x);
            Slider y = null;
            if (asset.AxisCount == 2)
            {
                y = new Slider($"{asset.YAxis?.ParameterId} ({asset.YAxis?.Unit})", asset.YAxis?.Minimum ?? 0f, asset.YAxis?.Maximum ?? 1f)
                {
                    value = m_Window.PreviewParameter.y,
                    showInputField = true
                };
                m_Content.Add(y);
            }
            var time = new Slider("Canonical Normalized Time", 0f, 1f)
            {
                value = m_Window.PreviewNormalizedTime,
                showInputField = true
            };
            m_Content.Add(time);
            Vector2 Parameter() => new Vector2(x.value, y?.value ?? 0f);
            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.Add(new Button(() => m_Window.ApplyPreview(Parameter(), time.value)) { text = "Evaluate Plan" });
            m_Content.Add(actions);
            m_Content.Add(new HelpBox(
                "This page evaluates the compiled Blend Space solver only. Character locomotion preview belongs to the Pose Graph Workspace and runs the formal PoseStateMachine and Pose Plan.",
                HelpBoxMessageType.Info));
            if (!m_Window.TryEvaluatePreview(out BlendSpacePreviewEvaluation evaluation, out string status))
            {
                m_Content.Add(new HelpBox(status, HelpBoxMessageType.Warning));
                return;
            }
            m_Content.Add(new Label($"Projection {m_Window.Projection.ProjectionRevision}"));
            m_Content.Add(new Label($"Phase {evaluation.Canonical.NormalizedPhase:0.000} · cycle {evaluation.Canonical.Cycle}"));
            for (int i = 0; i < evaluation.Weights.Count; i++)
            {
                CharacterAnimationBlendSpaceSampleId id = evaluation.Weights.GetSampleId(i);
                float weight = evaluation.Weights.GetWeight(i);
                CharacterAnimationBlendSpaceSampleTime sampleTime = evaluation.FindTime(id);
                m_Content.Add(new Label($"{id} · {weight:P2} · {sampleTime.ClipTime:0.000}s ({sampleTime.NormalizedTime:0.000})"));
            }
        }

        void DrawDiagnostics()
        {
            if (!string.IsNullOrEmpty(m_Report))
                m_Content.Add(new HelpBox(m_Report, HelpBoxMessageType.Info));
            CharacterAnimationBlendSpaceValidationReport report = CharacterAnimationBlendSpaceValidator.Validate(m_Window.Asset);
            m_Content.Add(new Label($"Authoring: {(report.IsValid ? "Valid" : $"Invalid ({report.Issues.Count})")}"));
            for (int i = 0; i < report.Issues.Count; i++)
                m_Content.Add(new HelpBox(report.Issues[i].ToString(), HelpBoxMessageType.Error));
            m_Content.Add(new Label($"Projection: {m_Window.ResolveBuildState()}"));
            if (!m_Window.TryResolveCompiledPlan(out _, out string status))
                m_Content.Add(new HelpBox(status, HelpBoxMessageType.Warning));
        }

        void DrawReferences()
        {
            m_Content.Add(new Label($"Asset · {m_Window.Asset.name}"));
            m_Content.Add(new Label($"Rig · {(m_Window.Asset.Rig ? m_Window.Asset.Rig.name : "Unavailable")}"));
            m_Content.Add(new Label($"Definition · {(m_Window.Definition ? m_Window.Definition.name : "Unavailable")}"));
            m_Content.Add(new Label($"Pose Graph · {(m_Window.Profile && m_Window.Profile.PoseGraph ? m_Window.Profile.PoseGraph.name : "Unavailable")}"));
            m_Content.Add(new Label("Pose Watch reads only a matching AnimationPresentationRuntimeSnapshot; no runtime snapshot is available in asset-only mode."));
        }
    }

    sealed class BlendSpaceDiagnosticsAdapter : IGraphAuthoringDiagnosticsAdapter
    {
        readonly CharacterAnimationBlendSpaceEditorWindow m_Window;
        Label m_Status;
        internal BlendSpaceDiagnosticsAdapter(CharacterAnimationBlendSpaceEditorWindow window) { m_Window = window; }

        public void Bind(IGraphAuthoringDocument document, GraphView graphView, VisualElement toolbar)
        {
            if (m_Status == null)
            {
                m_Status = new Label();
                toolbar.Add(m_Status);
            }
            Refresh();
        }

        public void Refresh()
        {
            if (m_Status != null)
                m_Status.text = $"Blend Space · {m_Window.ResolveBuildState()}";
        }

        public void Clear()
        {
            m_Status?.RemoveFromHierarchy();
            m_Status = null;
        }
    }

    readonly struct BlendSpacePreviewEvaluation
    {
        internal BlendSpacePreviewEvaluation(
            CharacterAnimationBlendSpaceWeightPage weights,
            CharacterAnimationBlendSpaceTimePage times,
            CharacterAnimationBlendSpaceCanonicalPhase canonical)
        {
            Weights = weights;
            Times = times;
            Canonical = canonical;
        }

        internal CharacterAnimationBlendSpaceWeightPage Weights { get; }
        internal CharacterAnimationBlendSpaceTimePage Times { get; }
        internal CharacterAnimationBlendSpaceCanonicalPhase Canonical { get; }

        internal CharacterAnimationBlendSpaceSampleTime FindTime(CharacterAnimationBlendSpaceSampleId sampleId)
        {
            for (int i = 0; i < Times.Count; i++)
            {
                CharacterAnimationBlendSpaceSampleTime value = Times.Get(i);
                if (value.SampleId.Equals(sampleId))
                    return value;
            }
            throw new InvalidOperationException($"Compiled Blend Space time page has no Sample '{sampleId}'.");
        }
    }

    public sealed class CharacterAnimationBlendSpaceEditorWindow : GraphAuthoringEditorShell
    {
        [SerializeField] CharacterAnimationBlendSpaceAsset m_Asset;
        [SerializeField] CharacterAnimationPresentationProfile m_Profile;
        [SerializeField] CharacterPresentationProjectionAsset m_Projection;
        [SerializeField] CharacterPipelineDefinition m_Definition;
        BlendSpaceGraphView m_GraphView;
        BlendSpaceMutationAdapter m_Mutation;
        BlendSpaceInspectorAdapter m_Inspector;
        BlendSpaceNavigatorAdapter m_Navigator;
        BlendSpaceBottomDockAdapter m_BottomDock;
        readonly Dictionary<CharacterAnimationBlendSpaceSampleId, float> m_PreviewWeights =
            new Dictionary<CharacterAnimationBlendSpaceSampleId, float>();
        Vector2 m_PreviewParameter;
        float m_PreviewNormalizedTime;
        bool m_Building;
        readonly Guid m_AnimationDiagnosticsOwnerId = Guid.NewGuid();
        AnimationPresentationRuntimeTarget m_AnimationDiagnosticsTarget;

        internal CharacterAnimationBlendSpaceAsset Asset => m_Asset;
        internal CharacterAnimationPresentationProfile Profile => m_Profile;
        internal CharacterPresentationProjectionAsset Projection => m_Projection;
        internal CharacterPipelineDefinition Definition => m_Definition;
        internal BlendSpaceGraphView GraphView => m_GraphView;
        internal Vector2 PreviewParameter => m_PreviewParameter;
        internal float PreviewNormalizedTime => m_PreviewNormalizedTime;
        internal string RuntimeStatus => TryGetRuntimeSnapshot(out _, out string status) ? "Ready" : status;

        public static CharacterAnimationBlendSpaceEditorWindow Open(
            CharacterAnimationBlendSpaceAsset asset,
            CharacterPipelineDefinition definition = null)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            CharacterAnimationBlendSpaceEditorWindow window = GetWindow<CharacterAnimationBlendSpaceEditorWindow>();
            window.titleContent = new GUIContent("Character Animation Authoring");
            window.SetDocument(asset, definition);
            window.Show();
            window.Focus();
            return window;
        }

        protected override GraphView CreateGraphAuthoringView()
        {
            m_GraphView = new BlendSpaceGraphView(this);
            return m_GraphView;
        }

        protected override VisualElement CreateGraphAuthoringInspectorView()
        {
            m_Mutation = new BlendSpaceMutationAdapter(this);
            m_Inspector = new BlendSpaceInspectorAdapter(this);
            return m_Inspector.View;
        }

        protected override GraphAuthoringDomainAdapters CreateGraphAuthoringAdapters()
        {
            m_Navigator = new BlendSpaceNavigatorAdapter(this);
            m_BottomDock = new BlendSpaceBottomDockAdapter(this);
            var commands = new[]
            {
                new GraphAuthoringToolbarCommandDescriptor("compile", "Compile", GraphAuthoringToolbarCommandKind.ExplicitOperation, CompileSemanticIr),
                new GraphAuthoringToolbarCommandDescriptor("build", "Build", GraphAuthoringToolbarCommandKind.ExplicitOperation, BuildDefinition)
            };
            return new GraphAuthoringDomainAdapters(
                new BlendSpaceDocumentAdapter(this),
                new BlendSpaceNodeCatalogAdapter(),
                new BlendSpacePortPolicyAdapter(),
                m_Mutation,
                m_Inspector,
                new BlendSpaceDiagnosticsAdapter(this),
                new GraphAuthoringWorkspaceDescriptor(
                    new GraphAuthoringWorkspaceRegionDescriptor("Navigator", true, 220f, 280f),
                    new GraphAuthoringWorkspaceRegionDescriptor("Details", true, 260f, 380f),
                    new GraphAuthoringWorkspaceRegionDescriptor("Preview / Diagnostics", true, 170f, 280f),
                    commands),
                m_Navigator,
                m_BottomDock);
        }

        protected override void OnGraphAuthoringShellCreated()
        {
            RuntimeDebugSession.Shared.Changed -=
                OnRuntimeDebugSessionChanged;
            RuntimeDebugSession.Shared.Changed +=
                OnRuntimeDebugSessionChanged;
            RenderGraphAuthoringNavigation(
                new[]
                {
                    new GraphAuthoringBreadcrumbEntry("Character Animation", "Character Animation Authoring Workspace"),
                    new GraphAuthoringBreadcrumbEntry(m_Asset ? m_Asset.name : "Blend Space", "Blend Space asset mode")
                },
                null);
            RefreshWorkspace();
        }

        protected override void OnDisable()
        {
            RuntimeDebugSession.Shared.Changed -=
                OnRuntimeDebugSessionChanged;
            SetAnimationDiagnosticsInterest(false);
            base.OnDisable();
        }

        internal void SetDocument(CharacterAnimationBlendSpaceAsset asset, CharacterPipelineDefinition definition)
        {
            m_Asset = asset;
            m_Definition = definition;
            if (m_Definition)
            {
                m_Profile = m_Definition.AnimationPresentationProfile;
                m_Projection = m_Definition.PresentationProjection;
            }
            else
            {
                ResolveExactContext(asset, out m_Definition, out m_Profile, out m_Projection);
            }
            CharacterAnimationBlendSpaceAuthoringService.Initialize(m_Asset);
            m_PreviewParameter = m_Asset.Preview.Parameter;
            m_PreviewNormalizedTime = m_Asset.Preview.NormalizedTime;
            RefreshWorkspace();
        }

        internal void ApplyMutation(Action operation)
        {
            try
            {
                operation?.Invoke();
                m_PreviewWeights.Clear();
                RefreshWorkspace();
            }
            catch (Exception exception)
            {
                m_BottomDock?.Report(exception.Message);
            }
        }

        internal void ApplyPreview(Vector2 parameter, float normalizedTime)
        {
            try
            {
                CharacterAnimationBlendSpaceAuthoringService.SetPreview(m_Asset, parameter, normalizedTime);
                m_PreviewParameter = parameter;
                m_PreviewNormalizedTime = normalizedTime;
                if (!TryEvaluatePreview(out _, out string status))
                    m_BottomDock?.Report(status);
                RefreshWorkspace();
            }
            catch (Exception exception)
            {
                m_BottomDock?.Report(exception.Message);
            }
        }

        internal void SetTransientPreviewParameter(Vector2 parameter)
        {
            m_PreviewParameter = m_Asset.AxisCount == 1 ? new Vector2(parameter.x, 0f) : parameter;
            UpdatePreviewWeights();
            m_GraphView?.SetPreviewEvaluation(m_Asset, m_PreviewParameter, m_PreviewWeights);
        }

        internal void SetTransientPreviewTime(float normalizedTime)
        {
            m_PreviewNormalizedTime = Mathf.Clamp01(normalizedTime);
            if (!TryEvaluatePreview(out _, out string status))
                m_BottomDock?.Report(status);
        }

        internal void CommitTransientPreviewParameter()
        {
            try
            {
                CharacterAnimationBlendSpaceAuthoringService.SetPreview(
                    m_Asset,
                    m_PreviewParameter,
                    m_PreviewNormalizedTime);
                RefreshWorkspace();
            }
            catch (Exception exception)
            {
                m_BottomDock?.Report(exception.Message);
            }
        }

        internal void RefreshWorkspace()
        {
            if (m_GraphView == null)
                return;
            UpdatePreviewWeights();
            m_GraphView.Populate(m_Asset, m_PreviewParameter, m_PreviewWeights);
            RebindGraphAuthoringDocument();
        }

        internal string ResolveBuildState()
        {
            if (m_Building)
                return "Building";
            CharacterAnimationBlendSpaceValidationReport report = CharacterAnimationBlendSpaceValidator.Validate(m_Asset);
            if (!report.IsValid)
                return $"Invalid ({report.Issues.Count})";
            if (!m_Definition || !m_Profile || !m_Projection || !m_Definition.SimulationProgram)
                return "Missing Definition Context";
            if (EditorUtility.IsDirty(m_Profile) || EditorUtility.IsDirty(m_Definition))
                return "Dirty";
            if (string.IsNullOrWhiteSpace(m_Projection.ProjectionRevision))
                return "Build Required";
            return "Published";
        }

        internal bool TryResolveCompiledPlan(out CharacterAnimationBlendSpacePlan plan, out string status)
        {
            plan = null;
            string buildState = ResolveBuildState();
            if (!string.Equals(buildState, "Published", StringComparison.Ordinal))
            {
                status = $"Compiled preview unavailable: workspace state is {buildState}. Use explicit Compile/Build.";
                return false;
            }
            try
            {
                CharacterSimulationProgram program = m_Definition.SimulationProgram.Load();
                CharacterPresentationSemanticContract contract = Float32CharacterPresentationContractAdapter.Create(program);
                CharacterPresentationProjection projection = m_Projection.Load(contract);
                for (int i = 0; i < projection.BlendSpaces.Count; i++)
                {
                    CharacterAnimationBlendSpacePlan candidate = projection.BlendSpaces[i];
                    if (!candidate.BlendSpaceId.Equals(m_Asset.BlendSpaceId))
                        continue;
                    if (!string.Equals(candidate.ContentRevision, m_Asset.ContentRevision, StringComparison.Ordinal))
                    {
                        status = $"Compiled preview unavailable: Projection revision contains {candidate.ContentRevision}, authoring is {m_Asset.ContentRevision}.";
                        return false;
                    }
                    candidate.RequireValid(false);
                    plan = candidate;
                    status = "Ready";
                    return true;
                }
                status = $"Compiled preview unavailable: Projection has no Blend Space '{m_Asset.BlendSpaceId}'.";
                return false;
            }
            catch (Exception exception)
            {
                status = $"Compiled preview unavailable: {exception.Message}";
                return false;
            }
        }

        internal bool TryEvaluatePreview(out BlendSpacePreviewEvaluation evaluation, out string status)
        {
            if (!TryResolveCompiledPlan(out CharacterAnimationBlendSpacePlan plan, out status))
            {
                evaluation = default;
                return false;
            }
            return TryEvaluatePreview(plan, out evaluation, out status);
        }

        bool TryEvaluatePreview(
            CharacterAnimationBlendSpacePlan plan,
            out BlendSpacePreviewEvaluation evaluation,
            out string status)
        {
            evaluation = default;
            var weights = new CharacterAnimationBlendSpaceWeightPage(plan.Samples.Count);
            if (!CharacterAnimationBlendSpaceWeightEvaluator.Evaluate(
                    plan.CreateSolverPlan(),
                    m_PreviewParameter.x,
                    m_PreviewParameter.y,
                    weights,
                    out CharacterAnimationBlendSpaceSolveFailure solveFailure))
            {
                status = $"Compiled weight evaluation failed: {solveFailure}.";
                return false;
            }
            var times = new CharacterAnimationBlendSpaceTimePage(plan.Samples.Count);
            double effectiveTime = m_PreviewNormalizedTime * plan.ClockDurationSeconds;
            CharacterSimulationProgram program = m_Definition.SimulationProgram.Load();
            CharacterPresentationSemanticContract contract = Float32CharacterPresentationContractAdapter.Create(program);
            CharacterPresentationProjection compiledProjection = m_Projection.Load(contract);
            if (!CharacterAnimationBlendSpacePhaseMapper.Map(
                    plan.CreatePhasePlan(compiledProjection.ClipPhasePlans),
                    effectiveTime,
                    0,
                    times,
                    out CharacterAnimationBlendSpaceCanonicalPhase canonical,
                    out CharacterAnimationBlendSpacePhaseFailure phaseFailure))
            {
                status = $"Compiled phase evaluation failed: {phaseFailure}.";
                return false;
            }
            evaluation = new BlendSpacePreviewEvaluation(weights, times, canonical);
            status = "Ready";
            return true;
        }

        internal bool TryGetRuntimeSnapshot(out AnimationPresentationRuntimeSnapshot snapshot, out string status)
        {
            snapshot = default;
            RuntimeDebugViewModel viewModel = RuntimeDebugSession.Shared.ViewModel;
            if (!viewModel.Attached || !AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget target))
            {
                status = "Unavailable: no attached Animation Presentation runtime target.";
                return false;
            }
            try
            {
                if (!target.TryGetDebugView(
                        out AnimationPresentationDebugView debugView))
                {
                    status = "Unavailable: runtime target has no completed frame snapshot.";
                    return false;
                }
                snapshot = debugView.PosePlan;
            }
            catch (InvalidOperationException)
            {
                status = "Stale: runtime target Projection revision changed.";
                return false;
            }
            if (!m_Projection ||
                !string.Equals(target.ProjectionRevision, m_Projection.ProjectionRevision, StringComparison.Ordinal) ||
                !string.Equals(snapshot.ProjectionRevision, m_Projection.ProjectionRevision, StringComparison.Ordinal))
            {
                snapshot = default;
                status = "Stale: runtime Projection revision does not match this workspace.";
                return false;
            }
            for (int i = 0; i < snapshot.BlendSpacePlayers.Count; i++)
            {
                AnimationBlendSpacePlayerRuntimeSnapshot player = snapshot.BlendSpacePlayers[i];
                if (!player.BlendSpaceId.Equals(m_Asset.BlendSpaceId))
                    continue;
                if (!string.Equals(player.ContentRevision, m_Asset.ContentRevision, StringComparison.Ordinal))
                {
                    snapshot = default;
                    status = "Stale: runtime Blend Space content revision does not match this asset.";
                    return false;
                }
                status = "Ready";
                return true;
            }
            status = "Unavailable: attached runtime frame has no matching BlendSpacePlayer.";
            return false;
        }

        internal void SetAnimationDiagnosticsInterest(bool enabled)
        {
            RuntimeDebugViewModel viewModel =
                RuntimeDebugSession.Shared.ViewModel;
            AnimationPresentationRuntimeTarget target =
                enabled && viewModel.Attached &&
                AnimationPresentationRuntimeTargetRegistry.TryGet(
                    viewModel.Target.CharacterRuntimeId,
                    out AnimationPresentationRuntimeTarget resolved)
                    ? resolved
                    : null;
            if (!ReferenceEquals(target, m_AnimationDiagnosticsTarget))
            {
                m_AnimationDiagnosticsTarget?.RemoveDiagnosticsInterest(
                    m_AnimationDiagnosticsOwnerId);
                m_AnimationDiagnosticsTarget = target;
            }
            m_AnimationDiagnosticsTarget?.SetDiagnosticsInterest(
                m_AnimationDiagnosticsOwnerId,
                AnimationPresentationDiagnosticsInterest.LiveState |
                AnimationPresentationDiagnosticsInterest.FinalPoseDetail);
        }

        void OnRuntimeDebugSessionChanged()
        {
            if (m_Inspector?.ActivePageId != "live")
                return;
            SetAnimationDiagnosticsInterest(true);
            m_Inspector.Bind(null);
        }

        void UpdatePreviewWeights()
        {
            m_PreviewWeights.Clear();
            if (!TryEvaluatePreview(out BlendSpacePreviewEvaluation evaluation, out _))
            {
                return;
            }
            for (int i = 0; i < evaluation.Weights.Count; i++)
                m_PreviewWeights[evaluation.Weights.GetSampleId(i)] = evaluation.Weights.GetWeight(i);
        }

        void CompileSemanticIr()
        {
            if (!m_Definition)
            {
                m_BottomDock?.Report("Compile unavailable: the Blend Space has no unique Character Definition context.");
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
                m_BottomDock?.Report("Build unavailable: the Blend Space has no unique Character Definition context.");
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

        void SetBuilding(bool value)
        {
            m_Building = value;
            GraphAuthoringAdapters?.Diagnostics.Refresh();
        }

        static void ResolveExactContext(
            CharacterAnimationBlendSpaceAsset asset,
            out CharacterPipelineDefinition definition,
            out CharacterAnimationPresentationProfile profile,
            out CharacterPresentationProjectionAsset projection)
        {
            definition = null;
            profile = null;
            projection = null;
            string[] guids = AssetDatabase.FindAssets("t:CharacterPipelineDefinition");
            var matches = new List<CharacterPipelineDefinition>();
            for (int i = 0; i < guids.Length; i++)
            {
                CharacterPipelineDefinition candidate = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(AssetDatabase.GUIDToAssetPath(guids[i]));
                CharacterAnimationPresentationProfile candidateProfile = candidate ? candidate.AnimationPresentationProfile : null;
                if (!candidateProfile)
                    continue;
                bool referenced =
                    candidateProfile.PoseSourceBindings.Any(binding =>
                        binding is CharacterBlendSpacePoseSourceBinding blendSpace &&
                        blendSpace.BlendSpace == asset);
                if (referenced)
                    matches.Add(candidate);
            }
            if (matches.Count != 1)
                return;
            definition = matches[0];
            profile = definition.AnimationPresentationProfile;
            projection = definition.PresentationProjection;
        }

        [UnityEditor.Callbacks.OnOpenAsset]
        static bool OnOpenAsset(int instanceId, int line)
        {
            CharacterAnimationBlendSpaceAsset asset = EditorUtility.InstanceIDToObject(instanceId) as CharacterAnimationBlendSpaceAsset;
            if (!asset)
                return false;
            Open(asset);
            return true;
        }
    }

    [CustomEditor(typeof(CharacterAnimationBlendSpaceAsset))]
    public sealed class CharacterAnimationBlendSpaceAssetEditor : UnityEditor.Editor
    {
        bool m_ShowDiagnostics;
        public override void OnInspectorGUI()
        {
            CharacterAnimationBlendSpaceAsset asset = (CharacterAnimationBlendSpaceAsset)target;
            EditorGUILayout.LabelField("Animation Blend Space", asset.name);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Rig", asset.Rig, typeof(CharacterAnimationRigDefinition), false);
            m_ShowDiagnostics = EditorGUILayout.Foldout(m_ShowDiagnostics, "Diagnostics", true);
            if (m_ShowDiagnostics)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Blend Space Id", asset.BlendSpaceId.Value);
                    EditorGUILayout.TextField("Content Revision", asset.ContentRevision);
                }
            }
            CharacterAnimationBlendSpaceValidationReport report = CharacterAnimationBlendSpaceValidator.Validate(asset);
            EditorGUILayout.HelpBox(report.IsValid ? "Authoring asset is valid." : $"Authoring asset has {report.Issues.Count} issue(s).", report.IsValid ? MessageType.Info : MessageType.Error);
            if (GUILayout.Button("Open Character Animation Authoring Workspace"))
                CharacterAnimationBlendSpaceEditorWindow.Open(asset);
        }
    }
}
