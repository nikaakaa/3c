using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSemanticIrInspectorWindow : EditorWindow
    {
        static readonly List<string> s_Sections = new List<string>
        {
            "Operations",
            "Literals",
            "Value Inputs",
            "References",
            "Control Flow",
            "State Slots",
            "Scopes",
            "World Requests",
            "Output Channels",
            "Catalog Entries",
            "Producers",
            "Source Map"
        };

        [SerializeField] string m_DefinitionGuid = string.Empty;
        [SerializeField] string m_LockedSemanticHash = string.Empty;

        readonly List<InspectorRow> m_AllRows = new List<InspectorRow>();
        readonly List<InspectorRow> m_FilteredRows = new List<InspectorRow>();
        readonly Dictionary<string, List<ProgramSourceMapEntry>> m_Sources = new Dictionary<string, List<ProgramSourceMapEntry>>(StringComparer.Ordinal);
        CharacterPipelineDefinition m_Definition;
        CharacterSemanticIrCacheResult m_Cache;
        CharacterGameplaySemanticIr m_SemanticIr;
        PopupField<string> m_Section;
        ToolbarSearchField m_Search;
        ListView m_List;
        ScrollView m_Details;
        Label m_NavigationStatus;

        [MenuItem("Assets/3C/Inspect Character Semantic IR", false, 2100)]
        static void InspectSelected()
        {
            Open(Selection.activeObject as CharacterPipelineDefinition);
        }

        [MenuItem("Assets/3C/Inspect Character Semantic IR", true)]
        static bool ValidateInspectSelected()
        {
            return Selection.activeObject is CharacterPipelineDefinition;
        }

        public static CharacterSemanticIrInspectorWindow Open(CharacterPipelineDefinition definition)
        {
            if (!definition)
                return null;
            string path = AssetDatabase.GetAssetPath(definition);
            string guid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("CharacterPipelineDefinition must be a persisted asset with a GUID.");

            CharacterSemanticIrInspectorWindow window = GetWindow<CharacterSemanticIrInspectorWindow>();
            window.titleContent = new GUIContent("Character Semantic IR");
            window.minSize = new Vector2(820f, 520f);
            window.m_DefinitionGuid = guid;
            window.m_LockedSemanticHash = string.Empty;
            window.Reload(true);
            window.Show();
            window.Focus();
            return window;
        }

        public void CreateGUI()
        {
            Reload(false);
        }

        void Reload(bool adoptIdentity)
        {
            m_Definition = LoadDefinition();
            m_Cache = InspectCurrentCache(m_DefinitionGuid, m_Definition);
            if (m_Cache.IsCurrent &&
                !adoptIdentity &&
                !string.IsNullOrEmpty(m_LockedSemanticHash) &&
                !string.Equals(m_LockedSemanticHash, m_Cache.Header.SemanticHash.ToString(), StringComparison.Ordinal))
            {
                m_Cache = new CharacterSemanticIrCacheResult(
                    CharacterSemanticIrCacheStatus.Stale,
                    m_Cache.Path,
                    m_Cache.Header,
                    null,
                    $"Artifact identity changed from {m_LockedSemanticHash} to {m_Cache.Header.SemanticHash}. Reload explicitly to inspect the new artifact.");
            }

            if (m_Cache.IsCurrent)
            {
                m_LockedSemanticHash = m_Cache.Header.SemanticHash.ToString();
                m_SemanticIr = m_Cache.Artifact.SemanticIr;
                IndexSources();
            }
            else
            {
                m_SemanticIr = null;
                m_Sources.Clear();
            }
            BuildUi();
        }

        CharacterPipelineDefinition LoadDefinition()
        {
            if (string.IsNullOrEmpty(m_DefinitionGuid))
                return null;
            string path = AssetDatabase.GUIDToAssetPath(m_DefinitionGuid);
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(path);
        }

        static CharacterSemanticIrCacheResult InspectCurrentCache(string definitionGuid, CharacterPipelineDefinition definition)
        {
            if (!definition)
            {
                string path = string.IsNullOrEmpty(definitionGuid) ? string.Empty : CharacterSemanticIrArtifactStore.GetPath(definitionGuid);
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Invalid, path, null, null, "Definition asset is unavailable.");
            }

            CharacterSemanticIrCacheResult cache = CharacterSemanticIrArtifactStore.Inspect(definitionGuid);
            if (!cache.IsCurrent)
                return cache;
            try
            {
                CharacterGameplaySemanticIrArtifactHeader header = cache.Header;
                ProgramId programId = CharacterSemanticFrontendCompiler.ComputeProgramId(definition);
                ProgramRevision sourceRevision = CharacterSemanticFrontendCompiler.ComputeSourceRevision(definition);
                bool current = header.ProgramId.Equals(programId) &&
                               string.Equals(header.CompilerVersion, CharacterSemanticFrontendCompiler.CompilerVersion, StringComparison.Ordinal) &&
                               header.OperationSetVersion.Equals(CharacterSemanticFrontendCompiler.OperationSetVersion) &&
                               header.TickRate == definition.SimulationTickRate &&
                               header.SourceRevision.Equals(sourceRevision);
                if (current)
                    return cache;
                return new CharacterSemanticIrCacheResult(
                    CharacterSemanticIrCacheStatus.Stale,
                    cache.Path,
                    header,
                    null,
                    "Semantic IR cache identity does not match the current Definition authoring source.");
            }
            catch (Exception exception)
            {
                return new CharacterSemanticIrCacheResult(CharacterSemanticIrCacheStatus.Invalid, cache.Path, cache.Header, null, exception.Message);
            }
        }

        void BuildUi()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.flexGrow = 1f;

            Toolbar toolbar = new Toolbar();
            toolbar.Add(new Label(m_Definition ? m_Definition.name : "Missing Definition"));
            m_Section = new PopupField<string>(s_Sections, 0);
            m_Section.style.width = 170f;
            m_Section.RegisterValueChangedCallback(_ => RebuildRows());
            toolbar.Add(m_Section);
            m_Search = new ToolbarSearchField();
            m_Search.style.flexGrow = 1f;
            m_Search.RegisterValueChangedCallback(_ => ApplyFilter());
            toolbar.Add(m_Search);
            Button reload = new Button(() => Reload(true)) { text = "Reload" };
            toolbar.Add(reload);
            rootVisualElement.Add(toolbar);

            HelpBoxMessageType statusType = m_Cache != null && m_Cache.IsCurrent
                ? HelpBoxMessageType.Info
                : HelpBoxMessageType.Error;
            string status = m_Cache == null
                ? "Semantic IR cache was not inspected."
                : $"Cache: {m_Cache.Status}  Path: {m_Cache.Path}{(string.IsNullOrEmpty(m_Cache.Message) ? string.Empty : $"\n{m_Cache.Message}")}";
            rootVisualElement.Add(new HelpBox(status, statusType));

            if (m_SemanticIr == null)
            {
                Button compile = new Button(CompileSemanticIr) { text = "Compile Semantic IR" };
                compile.SetEnabled(m_Definition);
                compile.style.alignSelf = Align.FlexStart;
                compile.style.marginLeft = 8f;
                compile.style.marginTop = 8f;
                rootVisualElement.Add(compile);
                return;
            }

            rootVisualElement.Add(BuildManifest());
            VisualElement body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            rootVisualElement.Add(body);

            m_List = new ListView
            {
                itemsSource = m_FilteredRows,
                fixedItemHeight = 22f,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                makeItem = () => new Label(),
                bindItem = (element, index) =>
                {
                    Label label = (Label)element;
                    label.text = index >= 0 && index < m_FilteredRows.Count ? m_FilteredRows[index].Summary : string.Empty;
                    label.tooltip = label.text;
                }
            };
            m_List.style.flexGrow = 1f;
            m_List.style.minWidth = 360f;
            m_List.selectionChanged += selection => ShowDetails(selection.Cast<InspectorRow>().FirstOrDefault());
            body.Add(m_List);

            m_Details = new ScrollView();
            m_Details.style.width = 430f;
            m_Details.style.minWidth = 320f;
            m_Details.style.borderLeftWidth = 1f;
            m_Details.style.borderLeftColor = new Color(0.25f, 0.25f, 0.25f);
            body.Add(m_Details);
            RebuildRows();
        }

        VisualElement BuildManifest()
        {
            CharacterGameplaySemanticIrManifest manifest = m_SemanticIr.Manifest;
            Foldout foldout = new Foldout { text = "Manifest and Table Counts", value = true };
            foldout.style.marginLeft = 6f;
            foldout.style.marginRight = 6f;
            AddField(foldout, "ProgramId", manifest.ProgramId.Value);
            AddField(foldout, "Compiler", manifest.CompilerVersion);
            AddField(foldout, "Operation Set", manifest.OperationSetVersion.ToString());
            AddField(foldout, "Tick Rate", manifest.TickRate.ToString(CultureInfo.InvariantCulture));
            AddField(foldout, "Source Revision", manifest.SourceRevision.Value);
            AddField(foldout, "Semantic Hash", m_SemanticIr.SemanticHash.ToString());
            AddField(foldout, "Capabilities", string.Join(", ", manifest.Capabilities.GameplayCapabilities));
            AddField(foldout, "World Capabilities", manifest.Capabilities.RequiredWorldCapabilities.ToString());
            AddField(foldout, "Body Motion Source", m_SemanticIr.BodyMotion.SourceIdentity);
            AddField(foldout, "Body Motion Revision", m_SemanticIr.BodyMotion.ContentRevision.ToString());
            AddField(foldout, "Body Motion Version", m_SemanticIr.BodyMotion.SemanticVersion.ToString(CultureInfo.InvariantCulture));
            AddField(foldout, "Gravity Acceleration", m_SemanticIr.BodyMotion.GravityAcceleration.ToString("R", CultureInfo.InvariantCulture));
            AddField(foldout, "Maximum Fall Speed", m_SemanticIr.BodyMotion.MaximumFallSpeed.ToString("R", CultureInfo.InvariantCulture));
            AddField(
                foldout,
                "Counts",
                $"Operations {m_SemanticIr.Operations.Count} | Literals {m_SemanticIr.Literals.Count} | ValueInputs {CountValueInputs()} | ControlFlow {m_SemanticIr.ControlFlow.Count} | References {m_SemanticIr.References.Count} | StateSlots {m_SemanticIr.StateDeclarations.Count} | Scopes {m_SemanticIr.Scopes.Count} | WorldRequests {m_SemanticIr.WorldRequests.Count} | Outputs {m_SemanticIr.OutputChannels.Count} | Catalog {m_SemanticIr.CatalogEntries.Count} | Producers {m_SemanticIr.Producers.Count} | SourceMap {m_SemanticIr.SourceMap.Count}");
            return foldout;
        }

        static void AddField(VisualElement parent, string name, string value)
        {
            Label label = new Label($"{name}: {value}");
            label.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(label);
        }

        void CompileSemanticIr()
        {
            if (!m_Definition)
                return;
            CharacterSemanticFrontendResult result = CharacterSimulationBuildOrchestrator.CompileSemanticIr(m_Definition, true);
            if (!result.IsValid)
            {
                string message = string.Join("\n", result.Report.Messages.Select(i => i.ToString()));
                m_Cache = new CharacterSemanticIrCacheResult(
                    CharacterSemanticIrCacheStatus.Invalid,
                    CharacterSemanticIrArtifactStore.GetPath(m_DefinitionGuid),
                    null,
                    null,
                    message);
                BuildUi();
                return;
            }
            m_LockedSemanticHash = result.Artifact.Header.SemanticHash.ToString();
            Reload(true);
        }

        void IndexSources()
        {
            m_Sources.Clear();
            for (int i = 0; i < m_SemanticIr.SourceMap.Count; i++)
            {
                ProgramSourceMapEntry source = m_SemanticIr.SourceMap[i];
                string key = TargetKey(source.TargetKind, source.TargetIndex);
                if (!m_Sources.TryGetValue(key, out List<ProgramSourceMapEntry> values))
                {
                    values = new List<ProgramSourceMapEntry>();
                    m_Sources.Add(key, values);
                }
                values.Add(source);
            }
        }

        void RebuildRows()
        {
            if (m_SemanticIr == null || m_Section == null)
                return;
            m_AllRows.Clear();
            switch (m_Section.value)
            {
                case "Operations": BuildOperations(); break;
                case "Literals": BuildLiterals(); break;
                case "Value Inputs": BuildValueInputs(); break;
                case "References": BuildReferences(); break;
                case "Control Flow": BuildControlFlow(); break;
                case "State Slots": BuildStateSlots(); break;
                case "Scopes": BuildScopes(); break;
                case "World Requests": BuildWorldRequests(); break;
                case "Output Channels": BuildOutputChannels(); break;
                case "Catalog Entries": BuildCatalogEntries(); break;
                case "Producers": BuildProducers(); break;
                case "Source Map": BuildSourceMap(); break;
            }
            ApplyFilter();
        }

        void ApplyFilter()
        {
            m_FilteredRows.Clear();
            string search = m_Search?.value?.Trim() ?? string.Empty;
            for (int i = 0; i < m_AllRows.Count; i++)
            {
                InspectorRow row = m_AllRows[i];
                if (search.Length == 0 || row.Search.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                    m_FilteredRows.Add(row);
            }
            m_List?.Rebuild();
            m_Details?.Clear();
        }

        void BuildOperations()
        {
            for (int i = 0; i < m_SemanticIr.Operations.Count; i++)
            {
                SemanticOperation value = m_SemanticIr.Operations[i];
                string details =
                    $"Handle: {value.Handle.Value}\nCode: {value.Code}\nTemplate: {value.TemplateIdentity}\nOperands: {Join(value.Operands)}\nLiterals: {Join(value.LiteralReferences)}\nState Slots: {Join(value.StateSlots)}\nInteger0: {value.Integer0}\nInteger1: {value.Integer1}\nUnsigned0: {value.Unsigned0}\nNumber0: {Format(value.Number0)}\nNumber Source: {value.Number0SourceIdentity}\nText0: {value.Text0}\nFlags: {value.Flags}";
                if (value.Code == SimulationOperationCode.TimelineMotionWarp)
                {
                    IEnumerable<ProgramReference> references = m_SemanticIr.References.Where(reference => reference.SourceOperation.Equals(value.Handle));
                    details += $"\nMotionWarp Source: {string.Join(", ", references.Where(reference => reference.Kind == ProgramReferenceKind.MotionSourceOperation).Select(reference => $"{reference.TargetIndex}:{reference.ExternalIdentity}"))}";
                }
                AddTargetRow($"{value.Handle.Value:D4}  {value.Code}  {value.TemplateIdentity}", details, ProgramSourceTargetKind.Operation, value.Handle.Value);
            }
        }

        void BuildReferences()
        {
            for (int i = 0; i < m_SemanticIr.References.Count; i++)
            {
                ProgramReference value = m_SemanticIr.References[i];
                string source = value.HasSourceOperation ? value.SourceOperation.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;
                string details = $"Identity: {value.Identity}\nSource Operation: {source}\nKind: {value.Kind}\nTarget Index: {value.TargetIndex}\nExternal Identity: {value.ExternalIdentity}";
                AddRow($"{value.Kind}  {source} -> {value.TargetIndex}  {value.ExternalIdentity}", details, null);
            }
        }

        void BuildLiterals()
        {
            for (int i = 0; i < m_SemanticIr.Literals.Count; i++)
            {
                SemanticLiteral value = m_SemanticIr.Literals[i];
                string details = $"Index: {value.Index}\nIdentity: {value.Identity}\nKind: {value.Kind}\nPrecision: {value.Precision}\nValue: {FormatLiteral(value)}";
                AddTargetRow($"{value.Index:D4}  {value.Kind}  {value.Identity}", details, ProgramSourceTargetKind.Constant, value.Index);
            }
        }

        void BuildValueInputs()
        {
            for (int i = 0; i < m_SemanticIr.ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge value = m_SemanticIr.ControlFlow[i];
                if (value.Kind != ProgramControlFlowKind.Value)
                    continue;
                SemanticValueKind kind = m_SemanticIr.ResolveLinkedValueKind(value);
                string details =
                    $"Target Operation: {value.Target.Value}\nTarget Port: {value.TargetPort}\nResolved Value Kind: {kind}\nSource Operation: {value.Source.Value}\nSource Port: {value.SourcePort}\nEdge Identity: {value.Identity}";
                AddTargetRow($"{value.Target.Value:D4}  {value.TargetPort}  {kind}  <-  {value.Source.Value:D4}.{value.SourcePort}", details, ProgramSourceTargetKind.Operation, value.Source.Value);
            }
            for (int i = 0; i < m_SemanticIr.ConstantInputBindings.Count; i++)
            {
                SemanticConstantInputBinding value = m_SemanticIr.ConstantInputBindings[i];
                SemanticLiteral constant = m_SemanticIr.Literals[value.ConstantIndex];
                string details =
                    $"Target Operation: {value.TargetOperation.Value}\nTarget Port: {value.TargetPort}\nResolved Value Kind: {value.ResolvedValueKind}\nConstant Index: {value.ConstantIndex}\nConstant Source Identity: {constant.Identity}";
                AddTargetRow($"{value.TargetOperation.Value:D4}  {value.TargetPort}  {value.ResolvedValueKind}  <-  {constant.Identity}", details, ProgramSourceTargetKind.Constant, value.ConstantIndex);
            }
        }

        int CountValueInputs()
        {
            int count = m_SemanticIr.ConstantInputBindings.Count;
            for (int i = 0; i < m_SemanticIr.ControlFlow.Count; i++)
            {
                if (m_SemanticIr.ControlFlow[i].Kind == ProgramControlFlowKind.Value)
                    count++;
            }
            return count;
        }

        void BuildControlFlow()
        {
            for (int i = 0; i < m_SemanticIr.ControlFlow.Count; i++)
            {
                ProgramControlFlowEdge value = m_SemanticIr.ControlFlow[i];
                string details =
                    $"Identity: {value.Identity}\nKind: {value.Kind}\nSource: {value.Source.Value}\nTarget: {value.Target.Value}\nSource Port: {value.SourcePort}\nTarget Port: {value.TargetPort}\nOrder: {value.Order}\nPriority: {value.Priority}\nAbort: {value.AbortPolicy}\nHas Condition: {value.HasCondition}\nCondition: {(value.HasCondition ? value.Condition.Value.ToString(CultureInfo.InvariantCulture) : string.Empty)}";
                AddRow($"{value.Kind}  {value.Source.Value} -> {value.Target.Value}  {value.Identity}", details, null);
            }
        }

        void BuildStateSlots()
        {
            for (int i = 0; i < m_SemanticIr.StateDeclarations.Count; i++)
            {
                ProgramStateSlot value = m_SemanticIr.StateDeclarations[i];
                string details =
                    $"Index: {value.Index}\nIdentity: {value.Identity}\nValue Kind: {value.ValueKind}\nOwner Kind: {value.OwnerKind}\nSemantic: {value.Semantic}\nOwner Identity: {value.OwnerIdentity}\nDefault Literal: {value.DefaultConstantIndex}";
                AddTargetRow($"{value.Index:D4}  {value.Semantic}  {value.Identity}", details, ProgramSourceTargetKind.StateSlot, value.Index);
            }
        }

        void BuildScopes()
        {
            for (int i = 0; i < m_SemanticIr.Scopes.Count; i++)
            {
                ProgramScopeLayout value = m_SemanticIr.Scopes[i];
                string details = $"Compiled Owner Index: {value.CompiledOwnerIndex}\nIdentity: {value.Identity}\nKind: {value.Kind}\nOwner Identity: {value.OwnerIdentity}\nOwner Operation: {(value.OwnerOperation.IsValid ? value.OwnerOperation.Value.ToString(CultureInfo.InvariantCulture) : string.Empty)}\nState Slots: {Join(value.StateSlots)}";
                AddRow($"{value.CompiledOwnerIndex:D4}  {value.Kind}  {value.Identity}", details, null);
            }
        }

        void BuildWorldRequests()
        {
            for (int i = 0; i < m_SemanticIr.WorldRequests.Count; i++)
            {
                ProgramWorldRequestLayout value = m_SemanticIr.WorldRequests[i];
                AddRow($"{value.Index:D4}  {value.RequiredCapability}  {value.Identity}", $"Index: {value.Index}\nIdentity: {value.Identity}\nRequired Capability: {value.RequiredCapability}", null);
            }
        }

        void BuildOutputChannels()
        {
            for (int i = 0; i < m_SemanticIr.OutputChannels.Count; i++)
            {
                ProgramOutputChannelLayout value = m_SemanticIr.OutputChannels[i];
                AddRow($"{value.Index:D4}  {value.Kind}  {value.Identity}", $"Index: {value.Index}\nIdentity: {value.Identity}\nKind: {value.Kind}", null);
            }
        }

        void BuildCatalogEntries()
        {
            for (int i = 0; i < m_SemanticIr.CatalogEntries.Count; i++)
            {
                ProgramCatalogEntry value = m_SemanticIr.CatalogEntries[i];
                string fields = string.Join("\n", value.Fields.Select(field => $"  {field.Name}: {field.Kind} {(field.Kind == ProgramCatalogFieldKind.Constant ? field.ConstantIndex.ToString(CultureInfo.InvariantCulture) : field.Identity)}"));
                string details = $"Index: {value.Index}\nKind: {value.Kind}\nIdentity: {value.Identity}\nRevision: {value.Revision}\nFields:\n{fields}";
                AddTargetRow($"{value.Index:D4}  {value.Kind}  {value.Identity}", details, ProgramSourceTargetKind.CatalogEntry, value.Index);
            }
        }

        void BuildProducers()
        {
            for (int i = 0; i < m_SemanticIr.Producers.Count; i++)
            {
                ProgramProducer value = m_SemanticIr.Producers[i];
                string details = $"Index: {value.Index}\nIdentity: {value.Identity}\nLayer: {value.LayerId}\nSource Identity: {value.SourceIdentity}\nChannel: {value.ChannelKind}";
                AddTargetRow($"{value.Index:D4}  {value.LayerId}  {value.Identity}", details, ProgramSourceTargetKind.Producer, value.Index);
            }
        }

        void BuildSourceMap()
        {
            for (int i = 0; i < m_SemanticIr.SourceMap.Count; i++)
            {
                ProgramSourceMapEntry value = m_SemanticIr.SourceMap[i];
                string details =
                    $"Target: {value.TargetKind}:{value.TargetIndex}\nSource Type: {value.SourceType}\nGraphId: {value.GraphId}\nNodeId: {value.NodeId}\nEdgeId: {value.EdgeId}\nDeclarationId: {value.DeclarationId}\nTimelineId: {value.TimelineId}\nTrackId: {value.TrackId}\nClipId: {value.ClipId}\nDisplay Path: {value.DisplayPath}";
                AddRow($"{value.TargetKind}:{value.TargetIndex}  {value.SourceType}  {value.DisplayPath}", details, value);
            }
        }

        void AddTargetRow(string summary, string details, ProgramSourceTargetKind kind, int index)
        {
            m_Sources.TryGetValue(TargetKey(kind, index), out List<ProgramSourceMapEntry> sources);
            ProgramSourceMapEntry source = sources != null && sources.Count == 1 ? sources[0] : null;
            string sourceDetail = sources == null
                ? "\nSource Maps: 0"
                : $"\nSource Maps: {sources.Count}\n{string.Join("\n", sources.Select(i => $"  {i.SourceType} {i.DisplayPath}"))}";
            AddRow(summary, details + sourceDetail, source);
        }

        void AddRow(string summary, string details, ProgramSourceMapEntry source)
        {
            m_AllRows.Add(new InspectorRow(summary, details, source));
        }

        void ShowDetails(InspectorRow row)
        {
            m_Details.Clear();
            if (row == null)
                return;
            TextField text = new TextField { multiline = true, isReadOnly = true, value = row.Details };
            text.style.whiteSpace = WhiteSpace.Normal;
            text.style.flexGrow = 1f;
            m_Details.Add(text);
            m_NavigationStatus = new Label();
            m_NavigationStatus.style.whiteSpace = WhiteSpace.Normal;
            m_Details.Add(m_NavigationStatus);
            if (row.Source == null)
            {
                m_NavigationStatus.text = "Authoring source: unresolved or not unique.";
                return;
            }
            Button navigate = new Button(() => Navigate(row.Source)) { text = "Open Authoring Source" };
            m_Details.Add(navigate);
        }

        void Navigate(ProgramSourceMapEntry source)
        {
            RuntimeSourceElementKey key = ResolveSource(source);
            bool opened = key.IsValid && RuntimeDebugSourceNavigator.Open(m_Definition, key);
            m_NavigationStatus.text = opened
                ? "Authoring source opened."
                : "Authoring source is unresolved. No display-name or index fallback was used.";
        }

        static RuntimeSourceElementKey ResolveSource(ProgramSourceMapEntry source)
        {
            if (!string.IsNullOrEmpty(source.DeclarationId))
                return RuntimeSourceElementKey.Declaration(source.GraphId, source.DeclarationId);
            if (!string.IsNullOrEmpty(source.NodeId))
                return RuntimeSourceElementKey.Node(source.GraphId, source.NodeId);
            if (!string.IsNullOrEmpty(source.EdgeId))
                return RuntimeSourceElementKey.Edge(source.GraphId, source.EdgeId);
            if (!string.IsNullOrEmpty(source.ClipId))
                return RuntimeSourceElementKey.Clip(source.TimelineId, source.TrackId, source.ClipId, string.Equals(source.SourceType, typeof(TreeClip).FullName, StringComparison.Ordinal));
            if (!string.IsNullOrEmpty(source.TrackId))
                return RuntimeSourceElementKey.Track(source.TimelineId, source.TrackId);
            if (!string.IsNullOrEmpty(source.TimelineId))
                return RuntimeSourceElementKey.Timeline(source.TimelineId);
            if (!string.IsNullOrEmpty(source.GraphId))
                return RuntimeSourceElementKey.Graph(source.GraphId);
            return default;
        }

        static string TargetKey(ProgramSourceTargetKind kind, int index) => $"{(int)kind}:{index}";
        static string Join(IReadOnlyList<int> values) => string.Join(",", values);
        static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        static string FormatLiteral(SemanticLiteral value)
        {
            return value.Kind switch
            {
                SemanticLiteralKind.Boolean => value.Boolean ? "true" : "false",
                SemanticLiteralKind.Int32 => value.Int32.ToString(CultureInfo.InvariantCulture),
                SemanticLiteralKind.UInt64 => value.UInt64.ToString(CultureInfo.InvariantCulture),
                SemanticLiteralKind.Number => Format(value.X),
                SemanticLiteralKind.Vector2 => $"({Format(value.X)}, {Format(value.Y)})",
                SemanticLiteralKind.Vector3 => $"({Format(value.X)}, {Format(value.Y)}, {Format(value.Z)})",
                SemanticLiteralKind.Yaw => Format(value.X),
                SemanticLiteralKind.String => value.Text,
                SemanticLiteralKind.Document => FormatDocument(value.Document),
                _ => string.Empty
            };
        }

        static string FormatDocument(SemanticDataDocument document)
        {
            if (document == null)
                return string.Empty;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < document.Tokens.Count; i++)
            {
                SemanticDataToken token = document.Tokens[i];
                if (i > 0)
                    builder.Append(" | ");
                builder.Append(token.Kind).Append(':');
                builder.Append(token.Kind switch
                {
                    SemanticDataTokenKind.Boolean => token.Boolean ? "true" : "false",
                    SemanticDataTokenKind.Int32 => token.Int32.ToString(CultureInfo.InvariantCulture),
                    SemanticDataTokenKind.UInt32 => token.UInt32.ToString(CultureInfo.InvariantCulture),
                    SemanticDataTokenKind.UInt64 => token.UInt64.ToString(CultureInfo.InvariantCulture),
                    SemanticDataTokenKind.String => token.Text,
                    SemanticDataTokenKind.Number => Format(token.Number),
                    SemanticDataTokenKind.Bytes => $"bytes[{token.Bytes.Length}]",
                    _ => string.Empty
                });
                if (!string.IsNullOrEmpty(token.SourceIdentity))
                    builder.Append('@').Append(token.SourceIdentity);
            }
            return builder.ToString();
        }

        sealed class InspectorRow
        {
            public InspectorRow(string summary, string details, ProgramSourceMapEntry source)
            {
                Summary = summary ?? string.Empty;
                Details = details ?? string.Empty;
                Source = source;
                Search = $"{Summary}\n{Details}";
            }

            public string Summary { get; }
            public string Details { get; }
            public string Search { get; }
            public ProgramSourceMapEntry Source { get; }
        }
    }
}
