# Design: Unity 2022 兼容的 Ref Timeline UI 迁移

## 现状
本项目当前已有 Committed Action Timeline Editor、正式 `CharacterActionDefinitionSO` 入口、serialized adapter、validator 和 evaluator preview 基础。但 UI 仍存在几个问题：

- `CommittedActionRefPortedTimelineView` 过大，field、track、clip、selection、preview、payload inspector 混在同一个视图层里。
- UI 缺少稳定的 editor timeline model，复杂交互依赖数组 index 和深层 serialized path 会越来越脆。
- Ref UXML / USS 来自不同 Unity 版本，直接复制 `.uxml` 和 `.meta` 到 Unity 2022 项目有 importer 风险。
- 当前 `migrate-ref-timeline-editor-to-formal-action-config` 已经覆盖正式数据接入目标，但 UI 迁移完成度不足，需要单独收敛。

## 六层边界
本变更只触及 Presentation Layer 和 Editor adapter：

- Source：不变，仍由 Behavior Graph 表达 LocomotionSource / CommittedActionSource。
- Action：不变，仍由 `CharacterActionDefinitionSO` 表达 `Action.Dodge`。
- Claim：不变，FullBody 只作为 committed action 的 claim。
- Slot：不变，BodyArbiter 负责 Claim -> Slot。
- Channel：不变，timeline 输出仍映射 Animation / Motion / Window / Cue。
- Presentation Layer：迁移 Timeline Editor UI，显示并编辑正式 action timeline authoring 数据。

## 决策
### Decision: UI 迁移以 Unity 2022 安全导入为前置条件
Ref 的 UXML / USS 只能作为内容来源。迁移时必须：

- 不复制 Ref `.meta`。
- 不手写 `.meta`。
- 将 UXML 中 Unity 2023 或项目 GUID 风格引用改成 Unity 2022 可导入形式。
- 逐个资源导入和验证，不能一次性把整套 Ref 资源塞入项目。
- 资源验证失败时停止实现，不继续绑定代码绕过 importer 问题。

### Decision: 建立 Editor Timeline Model
Timeline UI 不直接散落操作 `SerializedProperty`。迁移后形成三层：

1. `CharacterActionDefinitionSO` 和 Unity serialized data。
2. Editor-only timeline model，包含 variant、duration、track、clip、stable id、selection id、validation state。
3. Field / Track / Clip / Inspector UI 操作 model，并通过 adapter transaction 写回 serialized data。

如果发现现有 track / clip 缺少稳定 id，必须作为正式 authoring 字段补齐并测试；不能只在 view state 里伪造。

### Decision: 按 Ref 组件顺序迁移
迁移顺序固定为：

1. Window shell 和资源加载边界。
2. Timeline field：seconds ruler、tick grid、locator、scroll、zoom、pan。
3. Track handle / track view：选择、添加、删除、重排。
4. Clip view：选择、添加、移动、左右缩放、invalid 状态。
5. Inspector：按 Animation / Motion / Window / Cue 显示正式 payload 字段。
6. Preview data：调用正式 evaluator 显示当前 local time / local tick outcome。

这样可以每一步都在正式 action definition 上闭环验证。

### Decision: Preview 先做数据预览
本阶段 preview 只要求展示正式 evaluator 的数据结果：selected node、animation key、motion spec、active windows 和 cue requests。动画 / motion 视觉预览后续单独审批，避免引入 `PlayableGraph` 或第二套表现执行路径。

## 风险与处理
- 风险：Ref UXML 在 Unity 2022 导入时崩溃。
  - 处理：先做资源兼容检查，逐个导入，不复制 `.meta`。
- 风险：当前 view 已经引用不存在或不稳定的 UXML path。
  - 处理：实现前先清点当前资源和引用，删除或替换不稳定入口。
- 风险：UI 为了快速可用绕过正式 serialized adapter。
  - 处理：所有编辑必须通过 editor timeline model 和 adapter transaction 写回正式 action definition。
- 风险：多选、拖拽、缩放依赖数组 index，删除或重排后选中错误。
  - 处理：引入 stable id 和 selection id 测试。
- 风险：把 Ref runtime runner 当 preview 跑起来。
  - 处理：静态边界测试禁止 `TimelinePlayer`、Taco runner、`PlayableGraph` 进入 runtime。

## 手动验证建议
手动验证不写入 `tasks.md`，交付时需要提供以下 Unity Editor 验证步骤：

- 打开 `Tools/3C/Committed Action Timeline Editor`。
- 默认加载 `Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset`。
- 切换 Directional / Backstep。
- 添加、选择、移动、缩放 Animation / Motion / Window / Cue clip。
- 保存、Validate、关闭窗口、重新打开确认数据仍在。
- 拖动 preview locator，确认当前 local time / local tick outcome 与 evaluator 数据一致。
- 肉眼确认轨道、clip label、ruler、locator、inspector 不重叠且没有明显布局错乱。

## Open Questions
- 是否需要在本变更内补 track / clip stable id 字段，取决于当前正式 authoring 数据是否已经具备可持久化 id。
- Ref 的 ease-in / ease-out 视觉是否进入本阶段，取决于当前 action timeline runtime 是否已经支持对应语义；如果 runtime 不支持，本阶段只迁移基础 resize，不做假视觉能力。
