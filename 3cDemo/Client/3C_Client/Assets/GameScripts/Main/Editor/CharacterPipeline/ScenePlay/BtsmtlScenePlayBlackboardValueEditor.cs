using System;
using System.Globalization;
using BTSMTL.Diagnostics;
using BTSMTL.Diagnostics.Editor;
using ThirdPersonCharacter.Pipeline.Simulation.Fixed;
using ThirdPersonSimulation;
using ThirdPersonSimulation.Fixed;
using UnityEditor;
using UnityEngine.UIElements;
using FixedValue = ThirdPersonSimulation.Fixed.AbilityStateValue;
using FixedTarget = ThirdPersonSimulation.Fixed.SimulationActionTargetSnapshot;

namespace ThirdPersonCharacter.Pipeline.Editor.ScenePlay
{
    sealed class BtsmtlScenePlayBlackboardValueEditor : VisualElement
    {
        readonly RuntimeDebugSession m_Session = RuntimeDebugSession.Shared;
        readonly BtsmtlScenePlayPreviewHost m_Host = BtsmtlScenePlayPreviewHost.Shared;
        readonly Label m_Name = new Label("选择变量后修改当前实例值");
        readonly Label m_Access = new Label();
        readonly Label m_Result = new Label();
        readonly VisualElement m_Fields = new VisualElement();
        readonly Toggle m_Boolean = new Toggle("值");
        readonly TextField m_Text = new TextField("值");
        readonly TextField[] m_Numbers = { new TextField("X"), new TextField("Y"), new TextField("Z"), new TextField("Yaw") };
        readonly FixedScalar[] m_Parsed = new FixedScalar[4];
        readonly Button m_Submit;
        FixedCharacterRegistration m_Registration;
        GameplayAbilityExecutionIdentity m_Ability;
        FixedBlackboardValueSnapshot m_Target;
        RuntimeContentRevision m_Revision;
        ulong m_PendingSequence;

        internal BtsmtlScenePlayBlackboardValueEditor()
        {
            name = "workbench-blackboard-value-editor";
            Add(m_Name);
            m_Access.style.whiteSpace = WhiteSpace.Normal;
            m_Result.style.whiteSpace = WhiteSpace.Normal;
            Add(m_Access);
            m_Fields.Add(m_Boolean);
            m_Fields.Add(m_Text);
            for (int i = 0; i < m_Numbers.Length; i++)
                m_Fields.Add(m_Numbers[i]);
            Add(m_Fields);
            m_Submit = new Button(Submit) { text = "提交修改" };
            Add(m_Submit);
            Add(m_Result);
            m_Fields.style.display = DisplayStyle.None;
            m_Submit.SetEnabled(false);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Session.Changed += UpdateAvailability;
                m_Host.Changed += OnPreviewChanged;
                if (m_PendingSequence != 0)
                    EditorApplication.update += PollResult;
                OnPreviewChanged();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Session.Changed -= UpdateAvailability;
                m_Host.Changed -= OnPreviewChanged;
                EditorApplication.update -= PollResult;
            });
        }

        internal void Select(RuntimeDebugEventView record)
        {
            if (m_PendingSequence != 0)
            {
                m_Result.text = "上一条修改尚待采用；播放或单步后再选择变量。";
                return;
            }
            m_Registration = null;
            m_Fields.style.display = DisplayStyle.None;
            m_Name.text = record.SourceName;
            m_Result.text = string.Empty;
            if (m_Session.AttachmentState != RuntimeDebugAttachmentState.Live || !m_Host.IsReady ||
                record.Event.RuntimeInstance.CharacterRuntimeId != m_Host.Actor.Registration.DiagnosticsContext.CharacterRuntimeId ||
                !record.Event.ContentRevision.Equals(m_Host.Actor.Registration.DiagnosticsContext.Revision))
            {
                m_Result.text = "只能修改当前隐藏 Preview 的实时变量；历史和其他运行目标只读。";
                UpdateAvailability();
                return;
            }
            FixedCharacterRegistration registration = m_Host.Actor.Registration;
            RuntimeTracePayload payload = record.Event.Payload;
            ulong action = payload.BlackboardScope == (int)ProgramScopeKind.Character ? 0 : payload.ActionInstanceId;
            if (!registration.TryGetBlackboardValue(new CharacterSkillId(payload.SkillId), action,
                    payload.BlackboardStateSlot, out FixedBlackboardValueSnapshot value) || !value.IsActive || value.IsReadOnly ||
                value.ScopeKind != ProgramScopeKind.Character && value.SkillGeneration != payload.SkillExecutionGeneration ||
                value.ScopeKind != ProgramScopeKind.Frame && value.Owner.Generation != payload.BlackboardOwnerGeneration)
            {
                m_Result.text = "该变量未激活、只读或作用域已换代，请重新选择当前变量。";
                UpdateAvailability();
                return;
            }
            m_Registration = registration;
            m_Ability = registration.CharacterBinding.AbilityInstallations.Require(value.Ability).Identity;
            m_Revision = record.Event.ContentRevision;
            m_Target = value;
            ShowValue(value.Value);
            UpdateAvailability();
        }

        bool IsCurrentTarget => m_Registration != null && m_Host.IsReady &&
            ReferenceEquals(m_Registration, m_Host.Actor.Registration) &&
            m_Revision.Equals(m_Registration.DiagnosticsContext.Revision);

        void OnPreviewChanged()
        {
            if (m_Registration != null && !IsCurrentTarget)
                ReleaseTarget();
            else
                UpdateAvailability();
        }

        void ReleaseTarget()
        {
            m_Registration = null;
            m_Fields.style.display = DisplayStyle.None;
            m_Result.text = "预览已结束或内容已变化，请重新选择变量；原请求结果不再可用。";
            EndPending();
        }

        void UpdateAvailability()
        {
            bool observingTarget = m_Registration != null &&
                m_Session.ViewModel.Target.CharacterRuntimeId == m_Registration.DiagnosticsContext.CharacterRuntimeId;
            bool editable = IsCurrentTarget && observingTarget &&
                m_Session.AttachmentState == RuntimeDebugAttachmentState.Live && m_PendingSequence == 0;
            if (m_Submit.enabledSelf != editable)
                m_Submit.SetEnabled(editable);
            if (m_Fields.enabledSelf != editable)
                m_Fields.SetEnabled(editable);
            m_Access.text = m_PendingSequence != 0 ? "等待下一次正式 Tick；暂停时可点击单步。"
                : m_Registration == null ? string.Empty
                : !IsCurrentTarget ? "预览或内容版本已变化，请重新选择变量。"
                : !observingTarget ? "当前观察的是其他运行目标，不能提交这份变量草稿。"
                : m_Session.AttachmentState != RuntimeDebugAttachmentState.Live ? "历史观察只读；返回实时后可以提交。"
                : "只修改当前运行实例，不保存为作者默认值。";
        }

        void ShowValue(FixedValue value)
        {
            ProgramStateValueKind kind = value.Kind;
            m_Fields.style.display = DisplayStyle.Flex;
            m_Boolean.style.display = kind == ProgramStateValueKind.Boolean ? DisplayStyle.Flex : DisplayStyle.None;
            m_Text.style.display = kind is ProgramStateValueKind.Identity or ProgramStateValueKind.Int32 or
                ProgramStateValueKind.UInt64 or ProgramStateValueKind.ActionTargetSnapshot ? DisplayStyle.Flex : DisplayStyle.None;
            int count = ComponentCount(kind);
            for (int i = 0; i < m_Numbers.Length; i++)
                m_Numbers[i].style.display = i < count ? DisplayStyle.Flex : DisplayStyle.None;
            m_Numbers[0].label = count == 1 ? kind == ProgramStateValueKind.Yaw ? "角度" : "值" : "X";
            m_Text.label = kind == ProgramStateValueKind.ActionTargetSnapshot ? "目标 ID" : "值";
            switch (kind)
            {
                case ProgramStateValueKind.Boolean: m_Boolean.SetValueWithoutNotify(value.Boolean); break;
                case ProgramStateValueKind.Int32: m_Text.SetValueWithoutNotify(value.Int32.ToString(CultureInfo.InvariantCulture)); break;
                case ProgramStateValueKind.UInt64: m_Text.SetValueWithoutNotify(value.UInt64.ToString(CultureInfo.InvariantCulture)); break;
                case ProgramStateValueKind.Identity: m_Text.SetValueWithoutNotify(value.Identity); break;
                case ProgramStateValueKind.Scalar: m_Parsed[0] = value.Scalar; break;
                case ProgramStateValueKind.Yaw: m_Parsed[0] = value.Yaw.Degrees; break;
                case ProgramStateValueKind.Vector2: m_Parsed[0] = value.Vector2.X; m_Parsed[1] = value.Vector2.Y; break;
                case ProgramStateValueKind.Vector3:
                    m_Parsed[0] = value.Vector3.X; m_Parsed[1] = value.Vector3.Y; m_Parsed[2] = value.Vector3.Z; break;
                case ProgramStateValueKind.ActionTargetSnapshot:
                    FixedTarget target = value.ActionTargetSnapshot;
                    m_Text.SetValueWithoutNotify(target.TargetId);
                    m_Parsed[0] = target.Position.X; m_Parsed[1] = target.Position.Y; m_Parsed[2] = target.Position.Z;
                    m_Parsed[3] = target.Yaw.Degrees;
                    break;
            }
            for (int i = 0; i < count; i++)
                m_Numbers[i].SetValueWithoutNotify(((decimal)m_Parsed[i].Raw / FixedScalar.OneRaw).ToString(CultureInfo.InvariantCulture));
        }

        static int ComponentCount(ProgramStateValueKind kind) => kind switch
        {
            ProgramStateValueKind.Scalar or ProgramStateValueKind.Yaw => 1,
            ProgramStateValueKind.Vector2 => 2,
            ProgramStateValueKind.Vector3 => 3,
            ProgramStateValueKind.ActionTargetSnapshot => 4,
            ProgramStateValueKind.Boolean or ProgramStateValueKind.Int32 or ProgramStateValueKind.UInt64 or ProgramStateValueKind.Identity => 0,
            _ => throw new InvalidOperationException($"黑板变量类型 '{kind}' 没有编辑合同。")
        };

        bool TryReadValue(out FixedValue value)
        {
            value = default;
            ProgramStateValueKind kind = m_Target.Value.Kind;
            for (int i = 0; i < ComponentCount(kind); i++)
            {
                if (!decimal.TryParse(m_Numbers[i].value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal number) ||
                    number < (decimal)long.MinValue / FixedScalar.OneRaw || number > (decimal)long.MaxValue / FixedScalar.OneRaw)
                {
                    m_Result.text = $"{m_Numbers[i].label} 需要输入 Fixed 范围内的有限数值。";
                    return false;
                }
                m_Parsed[i] = FixedScalar.FromDecimal(number);
            }
            switch (kind)
            {
                case ProgramStateValueKind.Boolean: value = FixedValue.FromBoolean(m_Boolean.value); return true;
                case ProgramStateValueKind.Int32:
                    if (int.TryParse(m_Text.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int signed))
                    { value = FixedValue.FromInt32(signed); return true; }
                    m_Result.text = "需要输入 Int32 范围内的整数。";
                    return false;
                case ProgramStateValueKind.UInt64:
                    if (ulong.TryParse(m_Text.value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong unsigned))
                    { value = FixedValue.FromUInt64(unsigned); return true; }
                    m_Result.text = "需要输入 UInt64 范围内的非负整数。";
                    return false;
                case ProgramStateValueKind.Identity: value = FixedValue.FromIdentity(m_Text.value); return true;
                case ProgramStateValueKind.Scalar: value = FixedValue.FromScalar(m_Parsed[0]); return true;
                case ProgramStateValueKind.Yaw: value = FixedValue.FromYaw(new FixedYaw(m_Parsed[0])); return true;
                case ProgramStateValueKind.Vector2: value = FixedValue.FromVector2(new FixedVector2(m_Parsed[0], m_Parsed[1])); return true;
                case ProgramStateValueKind.Vector3: value = FixedValue.FromVector3(new FixedVector3(m_Parsed[0], m_Parsed[1], m_Parsed[2])); return true;
                case ProgramStateValueKind.ActionTargetSnapshot:
                    value = FixedValue.FromActionTargetSnapshot(new FixedTarget(m_Text.value,
                        new FixedVector3(m_Parsed[0], m_Parsed[1], m_Parsed[2]), new FixedYaw(m_Parsed[3])));
                    return true;
                default: throw new InvalidOperationException($"黑板变量类型 '{kind}' 没有编辑合同。");
            }
        }

        void Submit()
        {
            UpdateAvailability();
            if (!m_Submit.enabledSelf || !TryReadValue(out FixedValue value))
                return;
            var command = new FixedBlackboardWriteCommand(m_Ability, m_Target.ActionInstanceId, m_Target.SkillGeneration,
                m_Target.StateSlot, m_Target.Owner, value);
            m_PendingSequence = m_Registration.CharacterRuntime.QueueBlackboardWrite(m_Registration.ActorId, command);
            m_Result.text = "修改已提交，尚未采用。";
            EditorApplication.update += PollResult;
            UpdateAvailability();
        }

        void PollResult()
        {
            if (!IsCurrentTarget)
            {
                ReleaseTarget();
                return;
            }
            if (!m_Registration.CharacterRuntime.TryTakeBlackboardWriteResult(m_Registration.ActorId,
                    m_Target.Ability, m_PendingSequence, out FixedBlackboardWriteResult result))
                return;
            m_Result.text = result.Status switch
            {
                FixedBlackboardWriteStatus.Applied => $"已于 Tick {result.Tick} 采用；后续节点仍按原规则读写该变量。",
                FixedBlackboardWriteStatus.InstanceEnded => "未采用：目标技能实例已结束。",
                FixedBlackboardWriteStatus.ScopeEnded => "未采用：目标作用域已结束。",
                FixedBlackboardWriteStatus.ScopeChanged => "未采用：作用域已换代，请重新选择变量。",
                _ => throw new InvalidOperationException($"未知黑板命令结果 '{result.Status}'。")
            };
            if (result.Status == FixedBlackboardWriteStatus.Applied)
                ShowValue(result.Command.Value);
            else
                m_Registration = null;
            EndPending();
        }

        void EndPending()
        {
            EditorApplication.update -= PollResult;
            m_PendingSequence = 0;
            UpdateAvailability();
        }
    }
}
