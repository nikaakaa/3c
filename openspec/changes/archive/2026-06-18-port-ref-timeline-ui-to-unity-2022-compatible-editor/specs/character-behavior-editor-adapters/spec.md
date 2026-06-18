## ADDED Requirements

### Requirement: Ref Timeline UI 迁移必须兼容 Unity 2022
Committed Action Timeline Editor 的 Ref UI 迁移 MUST 以 Unity 2022.3 可导入、可重载、可测试为前置条件。迁移资源 MAY 参考 `Ref/wly970123` 的 UXML、USS 和图标，但 MUST NOT 直接复制 Ref `.meta`，MUST NOT 保留指向 Ref 项目路径的 `project://database/Assets/Addon/Taco` 样式引用，且 MUST 在绑定代码前确认 Unity Editor 能安全导入资源。

#### Scenario: UXML 和 USS 逐个兼容导入
- **WHEN** 迁移 Ref Timeline UI 资源
- **THEN** 每个 UXML / USS MUST 先转换为 Unity 2022 兼容格式
- **AND** Unity MUST 负责生成本项目 `.meta`
- **AND** 导入失败 MUST 阻止后续代码绑定

#### Scenario: 禁止复制 Ref meta 和项目路径引用
- **WHEN** 运行 editor resource 静态检查
- **THEN** 检查 MUST 发现并拒绝直接复制的 Ref `.meta` 风险
- **AND** MUST 拒绝 `project://database/Assets/Addon/Taco` 样式引用进入本项目迁移资源

### Requirement: Timeline Editor 必须通过 Editor Timeline Model 操作正式 ActionDefinition
Committed Action Timeline Editor MUST 通过 Editor-only timeline model 操作正式 `CharacterActionDefinitionSO`。UI MAY 展示 Field、Track、Clip 和 Inspector 组件，但 MUST 通过 model transaction 和 serialized adapter 写回 `DodgeCommittedActionBranchAuthoring`、`CommittedActionBranchTimelineAuthoring`、`ActionTimelineTrackAuthoring` 和 `ActionTimelineClipAuthoring`。

#### Scenario: Model 从正式 ActionDefinition 建立快照
- **GIVEN** 正式 Dodge `CharacterActionDefinitionSO` 包含 Directional 和 Backstep timeline
- **WHEN** 打开 Committed Action Timeline Editor
- **THEN** editor timeline model MUST 从该 action definition 建立 Directional / Backstep 快照
- **AND** 快照 MUST 包含 timeline duration、track、clip、payload、validation state 和 selection 所需身份

#### Scenario: Model transaction 写回正式 serialized data
- **WHEN** 设计者添加、删除、移动、缩放 track 或 clip
- **THEN** 修改 MUST 通过 model transaction 写回 Unity serialized data
- **AND** 保存后 `CharacterActionDefinitionSO.ToDefinition()` MUST 看到同一份修改
- **AND** Behavior Graph compiler MUST NOT 参与 timeline payload 写回

#### Scenario: Selection 不依赖易碎数组 index
- **WHEN** 设计者删除或重排 track / clip
- **THEN** editor selection MUST 通过 stable id 或批准的等价身份保持正确
- **AND** MUST NOT 因数组 index 变化选中错误 payload

### Requirement: Timeline Field Track Clip 交互必须按 Ref 组件落地
迁移后的 Committed Action Timeline Editor MUST 按 Ref Timeline 的组件职责提供 field、track、clip 和 inspector 交互。实现 MAY 重写类名和 adapter，但 MUST 保留本阶段要求的用户可见交互能力。

#### Scenario: Field View 提供时间轴交互
- **WHEN** 设计者编辑 Directional 或 Backstep timeline
- **THEN** Field View MUST 提供 seconds ruler、tick grid、locator click / drag、scroll、zoom、中键 pan、F 定位和 rectangle selector
- **AND** timeline position 到 seconds authoring / local tick preview 的映射 MUST 使用稳定 position map 或批准的等价结构

#### Scenario: Track View 提供轨道编辑
- **WHEN** 设计者编辑 timeline track
- **THEN** Track View MUST 支持 track selection、add、delete、reorder 和 empty track 展示
- **AND** track kind MUST 来自正式 `ActionTimelineTrackKind`
- **AND** 非法 track / clip kind 组合 MUST 被拒绝或报告 validator 错误

#### Scenario: Clip View 提供片段编辑
- **WHEN** 设计者编辑 timeline clip
- **THEN** Clip View MUST 支持 clip selection、多选、add、delete、move、left resize、right resize 和 invalid 视觉状态
- **AND** clip kind MUST 来自正式 `ActionTimelineClipKind`
- **AND** 运行时不支持的 ease-in / ease-out 语义 MUST NOT 作为假编辑能力展示

#### Scenario: Inspector 编辑正式 payload
- **WHEN** 设计者选中 Animation、Motion、Window 或 Cue clip
- **THEN** Inspector MUST 显示并编辑对应正式 payload 字段
- **AND** payload 修改 MUST 写回正式 action definition
- **AND** 缺失必填 payload MUST 被 validator 报告

### Requirement: Timeline UI 迁移必须保持 Gameplay 边界
Timeline UI 迁移 MUST 只发生在 Editor-only 边界。Preview MUST 优先使用正式 evaluator 的数据结果，MUST NOT 引入第二 motion executor、第二 animation presenter、第二 blackboard writer、第二角色控制入口或 Ref gameplay runner。

#### Scenario: Runtime 不引用 Ref Timeline Runner
- **WHEN** 检查正式 runtime 源码和 asmdef
- **THEN** runtime MUST NOT 引用 `TimelinePlayer`
- **AND** MUST NOT 引用 Taco `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`
- **AND** MUST NOT 使用 Ref `PlayableGraph` 执行动作 timeline

#### Scenario: Preview 使用正式 evaluator 数据
- **WHEN** 设计者拖动 preview locator 到某一帧
- **THEN** preview MUST 调用正式 `CommittedActionBranchEvaluator` 或批准的等价 evaluator
- **AND** MUST 显示 selected node id、animation key、motion spec、active window facts 和 cue requests
- **AND** 缺少 preview binding 时 MUST 显示明确未绑定状态，不得查找 scene object 或使用 fallback 配置

### Requirement: Timeline UI 迁移必须可测试
系统 MUST 提供自动测试和静态检查，证明 Unity 2022 兼容资源、editor timeline model、serialized writeback、preview evaluator 和 runtime 边界均符合本变更要求。

#### Scenario: 自动测试覆盖 UI 数据闭环
- **WHEN** 运行相关 EditMode 测试
- **THEN** 测试 MUST 覆盖 Directional / Backstep timeline 读取
- **AND** MUST 覆盖 track add / delete / reorder
- **AND** MUST 覆盖 clip add / delete / move / resize
- **AND** MUST 覆盖 payload inspector 写回
- **AND** MUST 覆盖 save 后重新读取与 `ToDefinition()` 编译结果

#### Scenario: 静态检查覆盖迁移边界
- **WHEN** 运行 timeline editor 静态边界测试
- **THEN** 测试 MUST 确认 runtime 不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph 或 Taco runner
- **AND** MUST 确认迁移资源不包含 Ref 项目路径引用
- **AND** MUST 确认菜单、窗口标题和文档不使用通用技能编辑器命名
