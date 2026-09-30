using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using UnityEditor.UIElements;
using ThirdPersonSimulation;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    sealed class BtsmtlScenePlayBlackboardView : VisualElement
    {
        sealed class ValueRow : VisualElement
        {
            readonly Button m_Source;
            readonly Button m_Edit;
            readonly Label m_Value = new Label();
            readonly Label m_Position = new Label();
            RuntimeDebugEventView m_Record;

            internal ValueRow(Action<RuntimeDebugEventView> edit)
            {
                style.height = 76;
                m_Source = new Button(() => RuntimeDebugSourceNavigator.Open(m_Record, pin: true))
                {
                    style = { height = 22, unityTextAlign = UnityEngine.TextAnchor.MiddleLeft }
                };
                var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                m_Source.style.flexGrow = 1;
                header.Add(m_Source);
                m_Edit = new Button(() => edit(m_Record)) { text = "改值" };
                header.Add(m_Edit);
                Add(header);
                Add(m_Value);
                Add(m_Position);
            }

            internal void Bind(RuntimeDebugEventView record)
            {
                m_Record = record;
                m_Edit.SetEnabled(record.Event.Payload.BlackboardIsActive &&
                    record.Event.Payload.BlackboardLifetime != (int)ProgramBlackboardLifetime.Config);
                m_Source.text = record.SourceName;
                m_Source.tooltip = record.SourceName + "\n" + record.Event.Payload.Name;
                m_Value.text = record.Event.Payload.BlackboardIsActive
                    ? record.Event.Payload.Value.DisplayValue()
                    : "作用域未激活";
                string access = record.Event.Payload.BlackboardLifetime == (int)ProgramBlackboardLifetime.Config ? " · Config 只读" : string.Empty;
                m_Position.text = $"Tick {record.Event.Position} · 作用域代次 {record.Event.Payload.BlackboardOwnerGeneration}{access}";
            }
        }

        static readonly Comparison<RuntimeDebugEventView> s_ByVariable = (left, right) =>
        {
            RuntimeTracePayload a = left.Event.Payload;
            RuntimeTracePayload b = right.Event.Payload;
            int ability = string.CompareOrdinal(a.SkillId, b.SkillId);
            if (ability != 0)
                return ability;
            ulong actionA = a.BlackboardScope == (int)ProgramScopeKind.Character ? 0 : a.ActionInstanceId;
            ulong actionB = b.BlackboardScope == (int)ProgramScopeKind.Character ? 0 : b.ActionInstanceId;
            int action = actionA.CompareTo(actionB);
            return action != 0 ? action : a.BlackboardStateSlot.CompareTo(b.BlackboardStateSlot);
        };
        readonly RuntimeDebugSession m_Session = RuntimeDebugSession.Shared;
        readonly List<RuntimeDebugEventView> m_Events = new List<RuntimeDebugEventView>();
        readonly List<RuntimeDebugEventView> m_Values = new List<RuntimeDebugEventView>();
        readonly Dictionary<(string Ability, ulong Action, int Slot), RuntimeDebugEventView> m_ValuesBySlot = new();
        readonly List<RuntimeDebugEventView> m_Filtered = new List<RuntimeDebugEventView>();
        readonly List<RuntimeInstanceKey> m_Instances = new List<RuntimeInstanceKey> { default };
        readonly List<RuntimeInstanceKey> m_NextInstances = new List<RuntimeInstanceKey>();
        readonly HashSet<RuntimeInstanceKey> m_InstanceSet = new HashSet<RuntimeInstanceKey>();
        readonly PopupField<RuntimeInstanceKey> m_Instance;
        readonly Label m_Status = new Label();
        readonly ListView m_List;
        readonly BtsmtlScenePlayBlackboardValueEditor m_Editor = new BtsmtlScenePlayBlackboardValueEditor();
        RuntimeDebugViewModel m_View;
        long m_Revision = -1;
        RuntimeDebugAttachmentState m_State;
        int m_RowCount;

        internal BtsmtlScenePlayBlackboardView()
        {
            name = "workbench-blackboard";
            style.minWidth = 220;
            style.flexGrow = 1;
            Add(new Label("黑板"));
            m_Instance = new PopupField<RuntimeInstanceKey>(m_Instances, 0, FormatInstance, FormatInstance);
            m_Instance.RegisterValueChangedCallback(_ => ApplyFilter());
            Add(m_Instance);
            m_Status.style.whiteSpace = WhiteSpace.Normal;
            Add(m_Status);
            m_List = new ListView(m_Filtered, 76, () => new ValueRow(m_Editor.Select),
                (element, index) => ((ValueRow)element).Bind(m_Filtered[index]));
            m_List.selectionType = SelectionType.None;
            m_List.style.flexGrow = 1;
            Add(m_List);
            Add(m_Editor);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Session.EnsureLiveInterest(this, RuntimeTraceChannel.Blackboard);
                m_Session.Changed += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Session.Changed -= Refresh;
                m_Session.ReleaseLiveInterest(this);
            });
        }

        static string FormatInstance(RuntimeInstanceKey instance) => !instance.IsValid
            ? "全部已记录实例"
            : instance.Kind == RuntimeInstanceKind.Character
                ? "角色作用域"
                : $"{instance.StateId} · 释放 {instance.ActionInstanceId}";

        static RuntimeInstanceKey ObservationInstance(RuntimeDebugEventView value)
        {
            RuntimeTracePayload payload = value.Event.Payload;
            RuntimeInstanceKey instance = value.Event.RuntimeInstance;
            return payload.BlackboardScope == (int)ProgramScopeKind.Character || payload.ActionInstanceId == 0
                ? RuntimeInstanceKey.Character(instance.CharacterRuntimeId)
                : RuntimeInstanceKey.ActionInstance(instance.CharacterRuntimeId, instance.GraphRuntimeId,
                    payload.SkillId, payload.ActionInstanceId, payload.SkillExecutionGeneration);
        }

        void Refresh()
        {
            RuntimeDebugViewModel view = m_Session.ViewModel;
            bool replaced = !ReferenceEquals(m_View, view);
            bool stateChanged = m_State != m_Session.AttachmentState;
            if (!replaced && !stateChanged && m_Revision == view.Changes.Revision)
                return;
            m_View = view;
            m_Revision = view.Changes.Revision;
            m_State = m_Session.AttachmentState;
            if (!replaced && !view.Changes.AffectsSourceKind(RuntimeSourceElementKind.BlackboardDeclaration))
            {
                if (stateChanged)
                    ApplyFilter();
                return;
            }
            view.CopyCurrentEvents(RuntimeTraceChannel.Blackboard, m_Events);
            m_Values.Clear();
            m_ValuesBySlot.Clear();
            m_InstanceSet.Clear();
            for (int i = 0; i < m_Events.Count; i++)
            {
                RuntimeDebugEventView value = m_Events[i];
                if (value.Event.Kind is not RuntimeTraceEventKind.BlackboardSnapshot and not RuntimeTraceEventKind.BlackboardWritten)
                    continue;
                RuntimeTracePayload payload = value.Event.Payload;
                ulong action = payload.BlackboardScope == (int)ProgramScopeKind.Character ? 0 : payload.ActionInstanceId;
                var key = (payload.SkillId, action, payload.BlackboardStateSlot);
                if (!m_ValuesBySlot.TryGetValue(key, out RuntimeDebugEventView previous) || value.Event.Sequence > previous.Event.Sequence)
                    m_ValuesBySlot[key] = value;
            }
            foreach (RuntimeDebugEventView value in m_ValuesBySlot.Values)
                m_Values.Add(value);
            m_Values.Sort(s_ByVariable);
            for (int i = 0; i < m_Values.Count; i++)
                m_InstanceSet.Add(ObservationInstance(m_Values[i]));
            bool changed = m_Instances.Count != m_InstanceSet.Count + 1;
            for (int i = 1; !changed && i < m_Instances.Count; i++)
                changed = !m_InstanceSet.Contains(m_Instances[i]);
            if (changed)
            {
                m_NextInstances.Clear();
                m_NextInstances.Add(default);
                for (int i = 1; i < m_Instances.Count; i++)
                    if (m_InstanceSet.Contains(m_Instances[i]))
                        m_NextInstances.Add(m_Instances[i]);
                for (int i = 0; i < m_Values.Count; i++)
                    if (!m_NextInstances.Contains(ObservationInstance(m_Values[i])))
                        m_NextInstances.Add(ObservationInstance(m_Values[i]));
                RuntimeInstanceKey selected = m_Instance.value;
                m_Instances.Clear();
                m_Instances.AddRange(m_NextInstances);
                m_Instance.choices = m_Instances;
                m_Instance.SetValueWithoutNotify(m_InstanceSet.Contains(selected) ? selected : default);
            }
            ApplyFilter();
        }

        void ApplyFilter()
        {
            m_Filtered.Clear();
            RuntimeInstanceKey selected = m_Instance.value;
            for (int i = 0; i < m_Values.Count; i++)
                if (!selected.IsValid || ObservationInstance(m_Values[i]).Equals(selected))
                    m_Filtered.Add(m_Values[i]);
            m_Status.text = m_Filtered.Count == 0
                ? "尚无已提交的黑板快照。"
                : m_Session.AttachmentState == RuntimeDebugAttachmentState.Live
                    ? "显示已提交值；点击改值，在下一次播放或单步时采用。"
                    : "显示截至历史游标的黑板快照。";
            if (m_RowCount != m_Filtered.Count)
                m_List.Rebuild();
            else
                m_List.RefreshItems();
            m_RowCount = m_Filtered.Count;
        }
    }
}
