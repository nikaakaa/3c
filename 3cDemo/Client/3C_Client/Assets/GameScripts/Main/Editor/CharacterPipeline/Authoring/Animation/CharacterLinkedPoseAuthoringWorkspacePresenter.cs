using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    interface ICharacterLinkedPoseSelectorWorkspaceCapability
    {
        bool CanHandle(CharacterLinkedPoseSelectorBindingAsset selector);
        void Render(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseSelectorBindingAsset selector);
        bool TryAddCreationActions(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseGroupBinding group);
        IReadOnlyList<string> CollectDiagnostics(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseSelectorBindingAsset selector);
    }

    static class CharacterLinkedPoseSelectorWorkspaceCapabilities
    {
        static readonly ICharacterLinkedPoseSelectorWorkspaceCapability[] s_Capabilities =
        {
            new CharacterEquipmentLinkedPoseSelectorWorkspaceCapability()
        };

        public static bool TryRender(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseSelectorBindingAsset selector)
        {
            for (int i = 0; i < s_Capabilities.Length; i++)
            {
                if (!s_Capabilities[i].CanHandle(selector))
                    continue;
                s_Capabilities[i].Render(presenter, selector);
                return true;
            }
            return false;
        }

        public static bool TryAddCreationActions(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseGroupBinding group)
        {
            for (int i = 0; i < s_Capabilities.Length; i++)
            {
                ICharacterLinkedPoseSelectorWorkspaceCapability capability = s_Capabilities[i];
                if (capability == null)
                    continue;
                if (capability.TryAddCreationActions(presenter, group))
                    return true;
            }
            return false;
        }

        public static bool TryCollectDiagnostics(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseSelectorBindingAsset selector,
            List<string> diagnostics)
        {
            for (int i = 0; i < s_Capabilities.Length; i++)
            {
                ICharacterLinkedPoseSelectorWorkspaceCapability capability = s_Capabilities[i];
                if (capability == null || !capability.CanHandle(selector))
                    continue;
                diagnostics.AddRange(capability.CollectDiagnostics(presenter, selector));
                return true;
            }
            return false;
        }
    }

    sealed class CharacterEquipmentLinkedPoseSelectorWorkspaceCapability :
        ICharacterLinkedPoseSelectorWorkspaceCapability
    {
        public bool CanHandle(CharacterLinkedPoseSelectorBindingAsset selector) =>
            selector is CharacterEquipmentLinkedPoseSelectionBinding;

        public void Render(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseSelectorBindingAsset selector) =>
            presenter.RenderEquipmentSelector(
                (CharacterEquipmentLinkedPoseSelectionBinding)selector);

        public bool TryAddCreationActions(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseGroupBinding group)
        {
            presenter.AddEquipmentSelectorCreation(group);
            return true;
        }

        public IReadOnlyList<string> CollectDiagnostics(
            CharacterLinkedPoseAuthoringWorkspacePresenter presenter,
            CharacterLinkedPoseSelectorBindingAsset selector) =>
            presenter.CollectEquipmentSelectorDiagnostics(
                (CharacterEquipmentLinkedPoseSelectionBinding)selector);
    }

    internal sealed class CharacterLinkedPoseAuthoringWorkspacePresenter
    {
        readonly CharacterPresentationPoseGraphEditorWindow m_Window;
        VisualElement m_Root;
        string m_SelectionId = string.Empty;

        public CharacterLinkedPoseAuthoringWorkspacePresenter(
            CharacterPresentationPoseGraphEditorWindow window)
        {
            m_Window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public bool IsShowing => m_Root != null && m_Root.style.display == DisplayStyle.Flex;

        public void Bind(VisualElement root)
        {
            m_Root = root ?? throw new ArgumentNullException(nameof(root));
            m_Root.style.display = DisplayStyle.None;
            m_Root.style.flexGrow = 1f;
            m_Root.style.paddingLeft = 8f;
            m_Root.style.paddingRight = 8f;
            m_Root.style.paddingTop = 8f;
            m_Root.style.paddingBottom = 8f;
        }

        public void Hide()
        {
            m_SelectionId = string.Empty;
            if (m_Root != null)
                m_Root.style.display = DisplayStyle.None;
        }

        public void Show(string selectionId)
        {
            if (m_Root == null || !m_Window.ProfileContext || string.IsNullOrWhiteSpace(selectionId))
                return;
            m_SelectionId = selectionId;
            m_Root.style.display = DisplayStyle.Flex;
            m_Root.Clear();
            try
            {
                if (selectionId == "linked-root" || selectionId == "linked-empty")
                    RenderRoot();
                else if (selectionId.StartsWith("linked-interface:", StringComparison.Ordinal))
                    RenderInterface(FindInterface(selectionId.Substring("linked-interface:".Length)));
                else if (selectionId.StartsWith("linked-group:", StringComparison.Ordinal))
                    RenderGroup(FindGroup(selectionId.Substring("linked-group:".Length)));
                else if (selectionId.StartsWith("linked-selector:", StringComparison.Ordinal))
                    RenderSelector(FindSelector(selectionId.Substring("linked-selector:".Length)));
                else if (selectionId.StartsWith("linked-implementation:", StringComparison.Ordinal))
                    RenderImplementation(FindImplementation(selectionId.Substring("linked-implementation:".Length)));
                else if (selectionId.StartsWith("linked-entry:", StringComparison.Ordinal))
                    RenderEntry(FindEntry(selectionId));
                else if (selectionId.StartsWith("linked-call:", StringComparison.Ordinal))
                    RenderCall(FindCall(selectionId));
                else
                    RenderEmpty("Linked Pose", "选择 Group、Interface、selector、Implementation、Entry 或 root Call。");
            }
            catch (Exception exception)
            {
                RenderError(exception.Message);
            }
        }

        CharacterLinkedPoseInterfaceAsset FindInterface(string id) =>
            CharacterLinkedPoseAuthoringService.EnumerateInterfaces(m_Window.ProfileContext)
                .FirstOrDefault(value => value && value.InterfaceId.Value == id);

        void RenderRoot()
        {
            Header("Linked Pose", m_Window.ProfileContext.name, "Authoring Workspace");
            Summary("Groups", m_Window.ProfileContext.LinkedPoseGroups.Count.ToString());
            Summary("Implementations", m_Window.ProfileContext.LinkedPoseImplementations.Count.ToString());
            Summary("Selectors", m_Window.ProfileContext.LinkedPoseSelectors.Count.ToString());
            m_Window.TryGetPublishedPosePlan(out _, out string projectionStatus);
            Summary("Projection", projectionStatus);
            AddAction("Create Interface", () =>
            {
                try
                {
                    CharacterLinkedPoseInterfaceAsset linkedInterface =
                        CharacterLinkedPoseAuthoringService.CreateInterface(
                            m_Window.ProfileContext,
                            "Linked Pose Interface");
                    m_Window.ReloadLinkedPoseWorkspace();
                    m_Window.ShowLinkedPoseSelection("linked-interface:" + linkedInterface.InterfaceId.Value);
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
            AddAction("Create Missing Required Calls", () =>
            {
                try
                {
                    int count = CharacterLinkedPoseAuthoringService.CreateMissingRequiredCalls(
                        m_Window.ProfileContext,
                        m_Window.AssetContext);
                    m_Window.MarkLinkedPoseChanged();
                    AddHelp($"Created {count} root Call node(s). No edge was guessed or created.", HelpBoxMessageType.Info);
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly || m_Window.ProfileContext.LinkedPoseGroups.Count == 0);
            AddHelp("作者顺序：Interface 定义合同 → Group 绑定语义 → Implementation 创建每个 Entry Graph → selector 选择实现 → root Call 接入宿主。", HelpBoxMessageType.Info);
        }

        CharacterLinkedPoseGroupBinding FindGroup(string id) =>
            m_Window.ProfileContext.LinkedPoseGroups
                .FirstOrDefault(value => value != null && value.GroupId.Value == id);

        CharacterLinkedPoseSelectorBindingAsset FindSelector(string id) =>
            m_Window.ProfileContext.LinkedPoseSelectors
                .FirstOrDefault(value => value && value.SelectorId.Value == id);

        CharacterLinkedPoseImplementationAsset FindImplementation(string id) =>
            m_Window.ProfileContext.LinkedPoseImplementations
                .FirstOrDefault(value => value && value.ImplementationId.Value == id);

        CharacterLinkedPoseImplementationEntryBinding FindEntry(string selectionId)
        {
            string[] parts = selectionId.Substring("linked-entry:".Length).Split(':');
            if (parts.Length != 2)
                return null;
            CharacterLinkedPoseImplementationAsset implementation = FindImplementation(parts[0]);
            return implementation?.Entries.FirstOrDefault(value => value != null && value.EntryId.Value == parts[1]);
        }

        (CharacterTypedPoseNode Node, PoseGraphId GraphId) FindCall(string selectionId)
        {
            string[] parts = selectionId.Substring("linked-call:".Length).Split(':');
            if (parts.Length != 2 || !m_Window.AssetContext.TryGetGraph(new PoseGraphId(parts[0]), out CharacterTypedPoseGraph graph))
                throw new InvalidOperationException("Linked Pose Call graph no longer exists.");
            CharacterTypedPoseNode node = graph.Nodes.FirstOrDefault(value => value.NodeId.Value == parts[1]);
            if (node == null)
                throw new InvalidOperationException("Linked Pose Call node no longer exists.");
            return (node, graph.GraphId);
        }

        void RenderInterface(CharacterLinkedPoseInterfaceAsset linkedInterface)
        {
            if (!linkedInterface)
            {
                RenderError("Interface 不存在或已被删除。");
                return;
            }
            Header("Interface", linkedInterface.name, linkedInterface.IsStale ? "Stale" : "Ready");
            Summary("Entries", linkedInterface.Entries.Count.ToString());
            int groupCount = m_Window.ProfileContext.LinkedPoseGroups.Count(value => value != null && value.Interface == linkedInterface);
            int implementationCount = m_Window.ProfileContext.LinkedPoseImplementations.Count(value => value && value.Interface == linkedInterface);
            int callCount = m_Window.AssetContext.EnumerateGraphs()
                .SelectMany(value => value.Nodes)
                .Count(value => value?.Payload is CharacterLinkedPoseCallPayload call && call.InterfaceId == linkedInterface.InterfaceId);
            int edgeCount = m_Window.AssetContext.EnumerateGraphs()
                .Where(value => value != null)
                .SelectMany(value => value.Edges)
                .Count();
            Summary("Impact Closure", $"{groupCount} Group · {implementationCount} Implementation · {callCount} Call · {edgeCount} Graph Edge");
            AddAction("Add Local Pose Entry", () =>
            {
                try
                {
                    CharacterLinkedPoseInterfaceEntryDescriptor[] entries = linkedInterface.Entries
                        .Where(value => value != null)
                        .Concat(new[] { CreateLocalPoseEntry(linkedInterface) })
                        .ToArray();
                    ConfigureInterface(linkedInterface, entries);
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
            AddAction("Create Group from Interface", () =>
            {
                try
                {
                    CharacterLinkedPoseAuthoringService.CreateGroup(
                        m_Window.ProfileContext,
                        linkedInterface,
                        linkedInterface.name + " Group");
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
            AddAction("Create Implementation", () => CreateImplementation(linkedInterface, false), m_Window.IsLinkedPoseReadOnly);
            AddAction("Create Empty Implementation", () => CreateImplementation(linkedInterface, true), m_Window.IsLinkedPoseReadOnly);
            AddAction("Delete Interface", () =>
            {
                try
                {
                    var transaction = new CharacterPresentationMutationTransaction(
                        Guid.NewGuid().ToString("N"),
                        "Delete Linked Pose Interface");
                    transaction.Add(new RemoveLinkedPoseInterfaceMutation(
                        CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext),
                        linkedInterface));
                    new CharacterPresentationMutationService().Apply(
                        new CharacterPresentationProfileMutationOwner(
                            m_Window.ProfileContext,
                            CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext)),
                        transaction);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
            AddSection("Entries");
            foreach (CharacterLinkedPoseInterfaceEntryDescriptor entry in linkedInterface.Entries)
            {
                if (entry == null)
                {
                    AddHelp("Missing Entry", HelpBoxMessageType.Error);
                    continue;
                }
                var box = new VisualElement();
                box.AddToClassList("linked-pose-entry-box");
                box.Add(new Label(entry.EntryId.Value) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
                box.Add(new Label(entry.ExecutionDomain.ToString()));
                foreach (CharacterLinkedPoseInterfacePortDescriptor port in entry.Ports)
                    box.Add(new Label($"{port.Direction}  {port.Kind}  {port.PortId.Value}"));
                m_Root.Add(box);
            }
            AddDiagnostics(CollectInterfaceDiagnostics(linkedInterface));
            AddHelp("修改 Entry/Port 会重新计算 Interface revision，并把关联 Implementation、Call 和 Projection 标成 stale；边不会被静默删除。", HelpBoxMessageType.Info);
        }

        CharacterLinkedPoseInterfaceEntryDescriptor CreateLocalPoseEntry(CharacterLinkedPoseInterfaceAsset linkedInterface)
        {
            int index = 1;
            while (linkedInterface.Entries.Any(value => value != null && value.EntryId.Value == $"pose.{index}"))
                index++;
            return new CharacterLinkedPoseInterfaceEntryDescriptor(
                new LinkedPoseEntryId($"pose.{index}"),
                CharacterPoseExecutionDomain.PurePose,
                new[]
                {
                    new CharacterLinkedPoseInterfacePortDescriptor(
                        new PoseInterfacePortId($"input.pose.{index}"),
                        CharacterPosePortDirection.Input,
                        CharacterPosePortKind.LocalPose,
                        CharacterPoseSpace.Local,
                        true,
                        0),
                    new CharacterLinkedPoseInterfacePortDescriptor(
                        new PoseInterfacePortId($"output.pose.{index}"),
                        CharacterPosePortDirection.Output,
                        CharacterPosePortKind.LocalPose,
                        CharacterPoseSpace.Local,
                        true,
                        1)
                });
        }

        void ConfigureInterface(
            CharacterLinkedPoseInterfaceAsset linkedInterface,
            CharacterLinkedPoseInterfaceEntryDescriptor[] entries)
        {
            var transaction = new CharacterPresentationMutationTransaction(
                Guid.NewGuid().ToString("N"),
                "Configure Linked Pose Interface");
            string profileId = CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext);
            ulong revision = linkedInterface.Revision.IsValid ? linkedInterface.Revision.Value + 1UL : 1UL;
            transaction.Add(new ConfigureLinkedPoseInterfaceMutation(
                profileId,
                linkedInterface,
                linkedInterface.OwnerIdentity,
                linkedInterface.name,
                linkedInterface.InterfaceId,
                new LinkedPoseRevision(revision),
                entries));
            new CharacterPresentationMutationService().Apply(
                new CharacterPresentationProfileMutationOwner(m_Window.ProfileContext, profileId),
                transaction);
            Show("linked-interface:" + linkedInterface.InterfaceId.Value);
            m_Window.MarkLinkedPoseChanged();
        }

        void CreateImplementation(CharacterLinkedPoseInterfaceAsset linkedInterface, bool emptyTemplate)
        {
            try
            {
                CharacterLinkedPoseAuthoringService.CreateImplementation(
                    m_Window.ProfileContext,
                    linkedInterface,
                    linkedInterface.name + (emptyTemplate ? " Empty" : " Implementation"),
                    emptyTemplate);
                m_Window.ReloadLinkedPoseWorkspace();
            }
            catch (Exception exception) { RenderError(exception.Message); }
        }

        void RenderGroup(CharacterLinkedPoseGroupBinding group)
        {
            if (group == null)
            {
                RenderError("Group 不存在或已被删除。");
                return;
            }
            Header("Group", group.Interface ? group.Interface.name : "Missing Interface", "Contract Binding");
            Summary("Interface", group.Interface ? group.Interface.name : "Missing Interface");
            AddAction("Open Interface", () => Show("linked-interface:" + group.Interface.InterfaceId.Value), !group.Interface);
            AddAction("Create Implementation", () =>
            {
                try
                {
                    CharacterLinkedPoseAuthoringService.CreateImplementation(
                        m_Window.ProfileContext,
                        group.Interface,
                        group.GroupId.Value + " Implementation",
                        false);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly || !group.Interface);
            AddAction("Create Empty Implementation", () =>
            {
                try
                {
                    CharacterLinkedPoseAuthoringService.CreateImplementation(
                        m_Window.ProfileContext,
                        group.Interface,
                        group.GroupId.Value + " Empty",
                        true);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly || !group.Interface);
            AddSection("Selectors");
            foreach (CharacterLinkedPoseSelectorBindingAsset selector in m_Window.ProfileContext.LinkedPoseSelectors.Where(value => value && value.GroupId == group.GroupId))
                AddLink(selector.name, "linked-selector:" + selector.SelectorId.Value);
            if (!CharacterLinkedPoseSelectorWorkspaceCapabilities.TryAddCreationActions(this, group))
                AddHelp("Unavailable: 当前 Profile 没有已注册的 selector authoring capability。", HelpBoxMessageType.Warning);
            AddDiagnostics(CollectGroupDiagnostics(group));
            AddAction("Delete Group", () =>
            {
                try
                {
                    var transaction = new CharacterPresentationMutationTransaction(
                        Guid.NewGuid().ToString("N"),
                        "Delete Linked Pose Group");
                    string profileId = CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext);
                    transaction.Add(new RemoveLinkedPoseGroupMutation(profileId, group.GroupId));
                    new CharacterPresentationMutationService().Apply(
                        new CharacterPresentationProfileMutationOwner(m_Window.ProfileContext, profileId),
                        transaction);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
        }

        void RenderSelector(CharacterLinkedPoseSelectorBindingAsset selector)
        {
            if (selector == null)
            {
                RenderError("Selector 不存在或已被删除。");
                return;
            }
            Header("Selection", selector.name, "Candidate Closure");
            Summary("Group", FindGroup(selector.GroupId.Value)?.Interface?.name ?? "Missing Interface");
            if (!CharacterLinkedPoseSelectorWorkspaceCapabilities.TryRender(this, selector))
                AddHelp("Unavailable: 当前 selector 没有已注册的 authoring capability。", HelpBoxMessageType.Error);
            AddDiagnostics(CollectSelectorDiagnostics(selector));
            AddAction("Delete Selector", () =>
            {
                try
                {
                    string profileId = CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext);
                    var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Delete Linked Pose Selector");
                    transaction.Add(new RemoveLinkedPoseSelectorMutation(profileId, selector));
                    new CharacterPresentationMutationService().Apply(new CharacterPresentationProfileMutationOwner(m_Window.ProfileContext, profileId), transaction);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
        }

        internal void AddEquipmentSelectorCreation(
            CharacterLinkedPoseGroupBinding group)
        {
            CharacterEquipmentProfile equipmentProfile = m_Window.DefinitionContext?.EquipmentProfile;
            List<EquipmentSlotDefinition> slots = equipmentProfile?.Slots
                .Where(value => value != null)
                .ToList() ?? new List<EquipmentSlotDefinition>();
            List<CharacterLinkedPoseImplementationAsset> compatibleImplementations =
                m_Window.ProfileContext.LinkedPoseImplementations
                    .Where(value => value && value.Interface == group.Interface)
                    .ToList();
            var slotField = new DropdownField(
                "Equipment Slot",
                slots.Select(value => value.SlotId.Value).ToList(),
                slots.Count > 0 ? 0 : -1);
            var emptyField = new DropdownField(
                "Empty Implementation",
                compatibleImplementations.Select(value => value.name).ToList(),
                compatibleImplementations.Count > 0 ? 0 : -1);
            m_Root.Add(slotField);
            m_Root.Add(emptyField);
            AddAction("Create Equipment Selector", () =>
            {
                try
                {
                    if (slotField.index < 0 || slotField.index >= slots.Count ||
                        emptyField.index < 0 || emptyField.index >= compatibleImplementations.Count)
                        throw new InvalidOperationException("Equipment Slot 和同 Interface Empty Implementation 都不能为空。");
                    CharacterLinkedPoseAuthoringService.CreateEquipmentSelector(
                        m_Window.ProfileContext,
                        group,
                        slots[slotField.index].SlotId,
                        compatibleImplementations[emptyField.index].ImplementationId);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly || slots.Count == 0 || compatibleImplementations.Count == 0);
        }

        internal void RenderEquipmentSelector(
            CharacterEquipmentLinkedPoseSelectionBinding equipment)
        {
            Summary("Slot", equipment.SlotId.Value);
            Summary("Empty", equipment.EmptyImplementationId.Value);
            AddSection("Exact Mappings");
            foreach (CharacterEquipmentLinkedPoseMapping mapping in equipment.Mappings)
            {
                var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                row.Add(new Label($"{mapping.EquipmentId.Value} → {mapping.ImplementationId.Value}") { style = { flexGrow = 1f } });
                var remove = new Button(() =>
                {
                    try
                    {
                        string profileId = CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext);
                        var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Remove Equipment Linked Pose Mapping");
                        transaction.Add(new RemoveEquipmentLinkedPoseMappingMutation(profileId, equipment, mapping.EquipmentId));
                        new CharacterPresentationMutationService().Apply(new CharacterPresentationProfileMutationOwner(m_Window.ProfileContext, profileId), transaction);
                        Show("linked-selector:" + equipment.SelectorId.Value);
                    }
                    catch (Exception exception) { RenderError(exception.Message); }
                }) { text = "Remove" };
                remove.SetEnabled(!m_Window.IsLinkedPoseReadOnly);
                row.Add(remove);
                m_Root.Add(row);
            }
            CharacterEquipmentProfile equipmentProfile = m_Window.DefinitionContext?.EquipmentProfile;
            List<EquipmentDefinition> equipmentCatalog = equipmentProfile?.Equipment.Where(value => value != null).ToList() ?? new List<EquipmentDefinition>();
            CharacterLinkedPoseGroupBinding mappingGroup = FindGroup(equipment.GroupId.Value);
            List<CharacterLinkedPoseImplementationAsset> mappingImplementations = m_Window.ProfileContext.LinkedPoseImplementations
                .Where(value => value && mappingGroup?.Interface == value.Interface)
                .ToList();
            var equipmentField = new DropdownField(
                "Equipment",
                equipmentCatalog.Select(value => value.name).ToList(),
                equipmentCatalog.Count > 0 ? 0 : -1);
            var implementationField = new DropdownField(
                "Implementation",
                mappingImplementations.Select(value => value.name).ToList(),
                mappingImplementations.Count > 0 ? 0 : -1);
            m_Root.Add(equipmentField);
            m_Root.Add(implementationField);
            AddAction("Add Exact Mapping", () =>
            {
                try
                {
                    if (equipmentField.index < 0 || implementationField.index < 0)
                        throw new InvalidOperationException("Equipment catalog 和同 Interface Implementation 都不能为空。");
                    string profileId = CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext);
                    var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Set Equipment Linked Pose Mapping");
                    transaction.Add(new SetEquipmentLinkedPoseMappingMutation(
                        profileId,
                        equipment,
                        new CharacterEquipmentLinkedPoseMapping(
                            equipmentCatalog[equipmentField.index].EquipmentId,
                            mappingImplementations[implementationField.index].ImplementationId)));
                    new CharacterPresentationMutationService().Apply(new CharacterPresentationProfileMutationOwner(m_Window.ProfileContext, profileId), transaction);
                    Show("linked-selector:" + equipment.SelectorId.Value);
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly || equipmentCatalog.Count == 0 || mappingImplementations.Count == 0);
            AddSection("Candidate Closure");
            foreach (LinkedPoseImplementationId implementationId in equipment.CandidateImplementationIds)
                AddLink(implementationId.Value, "linked-implementation:" + implementationId.Value);
            AddHelp("Candidate Closure 是由 Empty 行和 Exact Mapping 派生的只读集合；跨 Interface、缺失 Implementation 和重复 Equipment 会在这里显示诊断。", HelpBoxMessageType.Info);
        }

        void RenderImplementation(CharacterLinkedPoseImplementationAsset implementation)
        {
            if (!implementation)
            {
                RenderError("Implementation 不存在或已被删除。");
                return;
            }
            Header("Implementation", implementation.name, implementation.IsStale ? "Stale" : "Ready");
            Summary("Interface", implementation.Interface ? implementation.Interface.name : "Missing Interface");
            Summary("Entries", $"{implementation.Entries.Count} / {implementation.Interface?.Entries.Count ?? 0}");
            AddAction("Open Interface", () => Show(implementation.Interface ? "linked-interface:" + implementation.Interface.InterfaceId.Value : string.Empty), !implementation.Interface);
            AddAction("Copy Implementation", () =>
            {
                try
                {
                    CharacterLinkedPoseAuthoringService.CreateImplementation(
                        m_Window.ProfileContext,
                        implementation.Interface,
                        implementation.name + " Copy",
                        false,
                        implementation);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly || !implementation.Interface);
            AddAction("Delete Implementation", () =>
            {
                try
                {
                    string profileId = CharacterLinkedPoseAuthoringService.RequireAssetOwnerId(m_Window.ProfileContext);
                    var transaction = new CharacterPresentationMutationTransaction(Guid.NewGuid().ToString("N"), "Delete Linked Pose Implementation");
                    transaction.Add(new RemoveLinkedPoseImplementationMutation(profileId, implementation));
                    new CharacterPresentationMutationService().Apply(new CharacterPresentationProfileMutationOwner(m_Window.ProfileContext, profileId), transaction);
                    m_Window.ReloadLinkedPoseWorkspace();
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
            AddSection("Required Entries");
            foreach (CharacterLinkedPoseInterfaceEntryDescriptor entry in implementation.Interface?.Entries ?? Array.Empty<CharacterLinkedPoseInterfaceEntryDescriptor>())
            {
                CharacterLinkedPoseImplementationEntryBinding binding = implementation.Entries.FirstOrDefault(value => value != null && value.EntryId == entry.EntryId);
                string prefix = binding == null ? "Missing · " : string.Empty;
                AddLink(prefix + entry.EntryId.Value, "linked-entry:" + implementation.ImplementationId.Value + ":" + entry.EntryId.Value);
            }
            AddDiagnostics(CollectImplementationDiagnostics(implementation));
        }

        void RenderEntry(CharacterLinkedPoseImplementationEntryBinding entry)
        {
            if (entry == null)
            {
                RenderError("Entry 不存在或已被删除。");
                return;
            }
            Header("Entry Graph", entry.EntryId.Value, entry.GraphOwner ? "Required Boundary" : "Missing");
            Summary("Graph Owner", entry.GraphOwner ? entry.GraphOwner.name : "Missing");
            AddAction("Open Entry Graph", () =>
            {
                if (entry.GraphOwner)
                    m_Window.FocusLinkedPoseEntry(entry.GraphOwner, entry.GraphId);
            }, !entry.GraphOwner);
            if (entry.GraphOwner && entry.GraphOwner.TryGetGraph(entry.GraphId, out CharacterTypedPoseGraph graph))
            {
                AddSection("Boundary");
                foreach (CharacterTypedPoseNode node in graph.Nodes)
                    AddLink(node.DisplayName, "linked-entry:" + m_Window.FindImplementationId(entry) + ":" + entry.EntryId.Value);
                AddHelp("Entry Graph 初始只拥有 Graph Input/Graph Output。", HelpBoxMessageType.Info);
            }
            AddDiagnostics(CollectEntryDiagnostics(entry));
        }

        void RenderCall((CharacterTypedPoseNode Node, PoseGraphId GraphId) call)
        {
            if (!(call.Node.Payload is CharacterLinkedPoseCallPayload payload))
            {
                RenderError("节点不是 Linked Pose Call。");
                return;
            }
            Header("Linked Pose Call", call.Node.DisplayName, "Root Only");
            CharacterLinkedPoseGroupBinding currentGroup = FindGroup(payload.GroupId.Value);
            var groups = m_Window.ProfileContext.LinkedPoseGroups.Where(value => value != null).ToList();
            if (groups.Count == 0)
            {
                AddHelp("当前 Profile 没有 Group。先创建 Interface，再创建 Group。", HelpBoxMessageType.Warning);
                return;
            }
            int groupIndex = Math.Max(0, groups.FindIndex(value => value.GroupId == payload.GroupId));
            var groupField = new DropdownField("Group", groups.Select(value => value.GroupId.Value).ToList(), groupIndex);
            var entryField = new DropdownField();
            void RefreshEntries(CharacterLinkedPoseGroupBinding group)
            {
                List<string> entries = group?.Interface?.Entries.Where(value => value != null).Select(value => value.EntryId.Value).ToList() ?? new List<string>();
                entryField.choices = entries;
                int selected = entries.IndexOf(payload.EntryId.Value);
                entryField.index = selected >= 0 ? selected : (entries.Count > 0 ? 0 : -1);
            }
            RefreshEntries(groups[groupIndex]);
            groupField.RegisterValueChangedCallback(evt =>
            {
                RefreshEntries(groups.Find(value => value.GroupId.Value == evt.newValue));
            });
            m_Root.Add(groupField);
            m_Root.Add(entryField);
            AddAction("Apply Call Binding", () =>
            {
                try
                {
                    CharacterLinkedPoseGroupBinding group = groups.First(value => value.GroupId.Value == groupField.value);
                    if (entryField.index < 0 || entryField.index >= group.Interface.Entries.Count)
                        throw new InvalidOperationException("所选 Group 没有可用 Entry。");
                    LinkedPoseEntryId entryId = group.Interface.Entries[entryField.index].EntryId;
                    CharacterLinkedPoseAuthoringService.RebindCall(
                        m_Window.ProfileContext,
                        m_Window.AssetContext,
                        call.GraphId,
                        call.Node.NodeId,
                        group,
                        entryId);
                    m_Window.MarkLinkedPoseChanged();
                    m_Window.FocusLinkedPoseCall(call.GraphId, call.Node.NodeId);
                }
                catch (Exception exception) { RenderError(exception.Message); }
            }, m_Window.IsLinkedPoseReadOnly);
            Summary("Derived Interface", currentGroup?.Interface ? currentGroup.Interface.InterfaceId.Value : "Missing");
            Summary("Ports", call.Node.DynamicPorts.Count.ToString());
            AddDiagnostics(CollectCallDiagnostics(call, currentGroup));
            AddHelp("重绑会先检查现有 edge 的端口身份、方向和类型；不兼容时整个 mutation 拒绝，不会静默删线。", HelpBoxMessageType.Info);
        }

        List<string> CollectInterfaceDiagnostics(
            CharacterLinkedPoseInterfaceAsset linkedInterface)
        {
            var diagnostics = new List<string>();
            if (linkedInterface.IsStale)
                diagnostics.Add("Interface revision/signature is stale for the current contract.");
            for (int entryIndex = 0; entryIndex < linkedInterface.Entries.Count; entryIndex++)
            {
                CharacterLinkedPoseInterfaceEntryDescriptor entry = linkedInterface.Entries[entryIndex];
                if (entry == null)
                {
                    diagnostics.Add($"Entry #{entryIndex} is missing.");
                    continue;
                }
                for (int portIndex = 0; portIndex < entry.Ports.Count; portIndex++)
                    if (entry.Ports[portIndex] == null)
                        diagnostics.Add($"Entry '{entry.EntryId}' port #{portIndex} is missing.");
            }
            foreach (CharacterLinkedPoseImplementationAsset implementation in m_Window.ProfileContext.LinkedPoseImplementations)
                if (implementation && implementation.Interface == linkedInterface && implementation.IsStale)
                    diagnostics.Add($"Implementation '{implementation.name}' is stale.");
            return diagnostics;
        }

        List<string> CollectGroupDiagnostics(CharacterLinkedPoseGroupBinding group)
        {
            var diagnostics = new List<string>();
            if (!group.Interface)
                diagnostics.Add("Group has no Interface contract.");
            if (!m_Window.ProfileContext.LinkedPoseSelectors.Any(value => value && value.GroupId == group.GroupId))
                diagnostics.Add("Group has no selector binding.");
            if (!m_Window.ProfileContext.LinkedPoseImplementations.Any(value => value && value.Interface == group.Interface))
                diagnostics.Add("Group has no compatible Implementation.");
            if (group.Interface)
            {
                foreach (CharacterLinkedPoseInterfaceEntryDescriptor entry in group.Interface.Entries.Where(value => value != null))
                {
                    int callCount = (m_Window.AssetContext.Graph?.Nodes ?? Array.Empty<CharacterTypedPoseNode>())
                        .Count(value => value?.Payload is CharacterLinkedPoseCallPayload payload &&
                                        payload.GroupId == group.GroupId &&
                                        payload.EntryId == entry.EntryId);
                    if (callCount != 1)
                        diagnostics.Add($"Entry '{entry.EntryId}' root Call coverage is {(callCount == 0 ? "missing" : "duplicate")} ({callCount}).");
                }
            }
            return diagnostics;
        }

        List<string> CollectSelectorDiagnostics(CharacterLinkedPoseSelectorBindingAsset selector)
        {
            var diagnostics = new List<string>();
            CharacterLinkedPoseGroupBinding group = FindGroup(selector.GroupId.Value);
            if (group == null || !group.Interface)
                diagnostics.Add("Selector Group or Interface is missing.");
            if (selector.CandidateImplementationIds.Count == 0)
                diagnostics.Add("Candidate Closure is empty.");
            foreach (LinkedPoseImplementationId implementationId in selector.CandidateImplementationIds)
            {
                CharacterLinkedPoseImplementationAsset implementation = m_Window.ProfileContext.LinkedPoseImplementations
                    .FirstOrDefault(value => value && value.ImplementationId == implementationId);
                if (!implementation)
                    diagnostics.Add($"Candidate '{implementationId}' is missing.");
                else if (group?.Interface != implementation.Interface)
                    diagnostics.Add($"Candidate '{implementation.name}' implements a different Interface.");
            }
            CharacterLinkedPoseSelectorWorkspaceCapabilities.TryCollectDiagnostics(this, selector, diagnostics);
            return diagnostics;
        }

        internal List<string> CollectEquipmentSelectorDiagnostics(
            CharacterEquipmentLinkedPoseSelectionBinding equipment)
        {
            var diagnostics = new List<string>();
            if (!equipment.EmptyImplementationId.IsValid)
                diagnostics.Add("Empty Implementation is missing.");
            if (!equipment.SlotId.IsValid)
                diagnostics.Add("Equipment Slot is missing.");
            var equipmentIds = new HashSet<EquipmentId>();
            foreach (CharacterEquipmentLinkedPoseMapping mapping in equipment.Mappings)
                if (mapping == null || !equipmentIds.Add(mapping.EquipmentId))
                    diagnostics.Add("Equipment mapping contains a missing or duplicate Equipment entry.");
            return diagnostics;
        }

        List<string> CollectImplementationDiagnostics(CharacterLinkedPoseImplementationAsset implementation)
        {
            var diagnostics = new List<string>();
            if (implementation.IsStale)
                diagnostics.Add("Implementation Interface signature is stale.");
            if (!implementation.Interface)
            {
                diagnostics.Add("Implementation Interface is missing.");
                return diagnostics;
            }
            foreach (CharacterLinkedPoseInterfaceEntryDescriptor entry in implementation.Interface.Entries.Where(value => value != null))
            {
                CharacterLinkedPoseImplementationEntryBinding binding = implementation.Entries
                    .FirstOrDefault(value => value != null && value.EntryId == entry.EntryId);
                if (binding == null)
                {
                    diagnostics.Add($"Required Entry '{entry.EntryId}' is missing.");
                    continue;
                }
                if (!binding.GraphOwner)
                    diagnostics.Add($"Entry '{entry.EntryId}' Graph owner is missing.");
                else if (!binding.GraphOwner.TryGetGraph(binding.GraphId, out CharacterTypedPoseGraph graph))
                    diagnostics.Add($"Entry '{entry.EntryId}' Graph is missing.");
                else
                {
                    try
                    {
                        CharacterLinkedPosePortProjection.RequireEntryGraphMatch(
                            graph,
                            implementation.Interface,
                            entry.EntryId);
                    }
                    catch (Exception exception)
                    {
                        diagnostics.Add($"Entry '{entry.EntryId}' boundary mismatch: {exception.Message}");
                    }
                }
            }
            return diagnostics;
        }

        List<string> CollectEntryDiagnostics(CharacterLinkedPoseImplementationEntryBinding entry)
        {
            var diagnostics = new List<string>();
            if (!entry.GraphOwner)
            {
                diagnostics.Add("Graph owner is missing.");
                return diagnostics;
            }
            if (!entry.GraphOwner.TryGetGraph(entry.GraphId, out CharacterTypedPoseGraph graph))
            {
                diagnostics.Add("Graph is missing.");
                return diagnostics;
            }
            CharacterLinkedPoseImplementationAsset implementation = m_Window.ProfileContext.LinkedPoseImplementations
                .FirstOrDefault(value => value && value.Entries.Contains(entry));
            if (implementation?.Interface == null)
                diagnostics.Add("Entry owner or Interface is missing.");
            else
            {
                try
                {
                    CharacterLinkedPosePortProjection.RequireEntryGraphMatch(
                        graph,
                        implementation.Interface,
                        entry.EntryId);
                }
                catch (Exception exception)
                {
                    diagnostics.Add($"Boundary port mismatch: {exception.Message}");
                }
            }
            return diagnostics;
        }

        List<string> CollectCallDiagnostics(
            (CharacterTypedPoseNode Node, PoseGraphId GraphId) call,
            CharacterLinkedPoseGroupBinding group)
        {
            var diagnostics = new List<string>();
            if (group == null || !group.Interface)
            {
                diagnostics.Add("Call Group or Interface is missing.");
                return diagnostics;
            }
            try
            {
                CharacterLinkedPosePortProjection.RequireCallMatch(call.Node, group.Interface);
            }
            catch (Exception exception)
            {
                diagnostics.Add($"Call typed ports are invalid: {exception.Message}");
            }
            foreach (CharacterPoseEdge edge in m_Window.AssetContext.Graph?.Edges ?? Array.Empty<CharacterPoseEdge>())
            {
                if (edge == null || edge.SourceNodeId != call.Node.NodeId && edge.TargetNodeId != call.Node.NodeId)
                    continue;
                if (!call.Node.DynamicPorts.Any(value => value != null &&
                        (value.PortId.Equals(edge.SourcePortId) || value.PortId.Equals(edge.TargetPortId))))
                    diagnostics.Add($"Edge '{edge.EdgeId}' references a Call port that no longer exists.");
            }
            return diagnostics;
        }

        void AddDiagnostics(IReadOnlyList<string> diagnostics)
        {
            AddSection("Diagnostics");
            if (diagnostics == null || diagnostics.Count == 0)
            {
                AddHelp("No blocking diagnostics.", HelpBoxMessageType.Info);
                return;
            }
            for (int i = 0; i < diagnostics.Count; i++)
                AddHelp(diagnostics[i], HelpBoxMessageType.Error);
        }

        void Header(string kind, string title, string status)
        {
            string workspaceStatus = m_Window.LinkedPoseWorkspaceStatus;
            string displayStatus = string.IsNullOrEmpty(workspaceStatus)
                ? status
                : $"{status} · {workspaceStatus}";
            m_Root.Add(new Label(kind) { style = { color = new StyleColor(new Color(0.45f, 0.65f, 1f)), unityFontStyleAndWeight = FontStyle.Bold } });
            m_Root.Add(new Label(title) { style = { fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold } });
            m_Root.Add(new HelpBox(displayStatus, displayStatus.Contains("Invalid") || displayStatus.Contains("Stale") ? HelpBoxMessageType.Warning : HelpBoxMessageType.Info));
        }

        void AddSection(string title) => m_Root.Add(new Label(title) { style = { marginTop = 10, unityFontStyleAndWeight = FontStyle.Bold } });

        void Summary(string label, string value)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 3 } };
            row.Add(new Label(label) { style = { width = 120, color = new StyleColor(Color.gray) } });
            row.Add(new Label(value ?? string.Empty));
            m_Root.Add(row);
        }

        void AddLink(string label, string selectionId)
        {
            m_Root.Add(new Button(() => Show(selectionId)) { text = label });
        }

        void AddAction(string label, Action action, bool disabled)
        {
            var button = new Button(action) { text = label };
            button.SetEnabled(!disabled);
            m_Root.Add(button);
        }

        void AddHelp(string message, HelpBoxMessageType type) => m_Root.Add(new HelpBox(message, type));

        void RenderEmpty(string title, string message)
        {
            Header(title, string.Empty, "Empty");
            AddHelp(message, HelpBoxMessageType.Info);
        }

        void RenderError(string message)
        {
            m_Root.Clear();
            Header("Linked Pose", "Authoring Error", "Invalid");
            AddHelp(message, HelpBoxMessageType.Error);
        }
    }
}
