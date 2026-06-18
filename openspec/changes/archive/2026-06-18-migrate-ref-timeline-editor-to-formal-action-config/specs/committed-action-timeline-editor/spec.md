# committed-action-timeline-editor Specification

## ADDED Requirements

### Requirement: Timeline Editor 以正式 Action Definition 为数据源
Committed Action Timeline Editor MUST 以本项目正式 `CharacterActionDefinitionSO` 作为唯一默认编辑入口。编辑器 MAY 支持用户选择其它正式 action definition，但 MUST NOT 默认加载 `Behavior/Samples` authoring asset，也 MUST NOT 生成 sample-only runtime definition 作为正式 gameplay 输入。

#### Scenario: 默认打开正式 Dodge ActionDefinition
- **WHEN** 设计者打开 `Tools/3C/Committed Action Timeline Editor`
- **THEN** 编辑器 MUST 默认加载 `Assets/Configs/3C/Action/Corin/Actions/Dodge/CorinDodgeActionDefinition.asset`
- **AND** ObjectField MUST 限制为 `CharacterActionDefinitionSO`
- **AND** MUST NOT 默认加载 `CorinDodgeBehaviorAuthoring.asset`

#### Scenario: 保存写回正式 ActionDefinition
- **GIVEN** 设计者移动或修改一个 Dodge timeline clip
- **WHEN** 设计者保存
- **THEN** 修改 MUST 写回被选择的 `CharacterActionDefinitionSO`
- **AND** 保存后 `CharacterActionDefinitionSO.ToDefinition()` MUST 能生成对应 runtime definition

### Requirement: Ref Timeline UI 迁移到 Editor-only Adapter
系统 MUST 将 `Ref/wly970123` Taco timeline 的主要编辑器交互迁移到本项目 Editor-only assembly，并通过 adapter 读写本项目 timeline authoring 数据。迁移后的 UI MUST NOT 直接保存 Taco `Timeline`、`Track`、`Clip` runtime object。

#### Scenario: 迁移 timeline field 和 track/clip view
- **WHEN** 设计者查看 Committed Action Timeline Editor
- **THEN** 编辑器 MUST 提供 track hierarchy、time marker、locator、track view、clip view 和 inspector
- **AND** UI 资源 MAY 来自 Ref UXML / USS / 图标
- **AND** 数据 MUST 映射到本项目 `ActionTimelineTrackAuthoring` 和 `ActionTimelineClipAuthoring`

#### Scenario: Ref runtime 不进入正式 gameplay
- **WHEN** 检查正式 runtime assembly 或 `Assets/Scripts/Character`
- **THEN** runtime MUST NOT 引用 `TimelinePlayer`
- **AND** MUST NOT 引用 Taco `BaseTree`、`RunnableTree`、`RunnableNode`、`TreeRunner`
- **AND** MUST NOT 通过 Ref `PlayableGraph` 执行动作 timeline

### Requirement: Timeline Editor 支持正式 Track 与 Clip 编辑
Timeline Editor MUST 支持对正式 action timeline 的 track 和 clip 进行结构编辑。所有编辑 MUST 通过正式 adapter 写入 Unity serialization，并 MUST 接受正式 validator 校验。

#### Scenario: Track 编辑
- **WHEN** 设计者编辑 Directional 或 Backstep timeline
- **THEN** 编辑器 MUST 支持添加、删除、选择、重排 Animation、Motion、Hitbox、Cancel、Cue track
- **AND** track kind MUST 来自正式 `ActionTimelineTrackKind`
- **AND** 非法 track kind MUST 被拒绝或报告错误

#### Scenario: Clip 编辑
- **WHEN** 设计者编辑一个 track
- **THEN** 编辑器 MUST 支持添加、删除、移动、左右缩放和选择 clip
- **AND** clip kind MUST 来自正式 `ActionTimelineClipKind`
- **AND** AnimationKey、Motion、HitboxWindow、CancelWindow、Cue payload MUST 可编辑
- **AND** 非法 seconds / tick 区间或缺失 payload MUST 被 validator 报告

### Requirement: Dodge Selector 与 Directional / Backstep Timeline 可编辑
系统 MUST 让 Dodge selector 和两个 timeline 作为同一正式 Dodge action definition 的可编辑数据。Directional 与 Backstep 的选择规则 MUST 继续由 `CommittedActionBranchEvaluator` 解释，Timeline Editor MUST NOT 创建第二套 selector 语义。

#### Scenario: Directional timeline 可编辑并可编译
- **GIVEN** 正式 Dodge action definition 包含 Directional timeline
- **WHEN** 设计者修改 Directional 的 Animation、Motion、Window 或 Cue clip
- **THEN** 保存后的 definition MUST 通过 `CommittedActionBranchEvaluator` 选择 `timeline.dodge.directional`
- **AND** evaluator outcome MUST 反映修改后的 timeline payload

#### Scenario: Backstep timeline 可编辑并可编译
- **GIVEN** 正式 Dodge action definition 包含 Backstep timeline
- **WHEN** 设计者修改 Backstep 的 Animation、Motion、Window 或 Cue clip
- **THEN** 保存后的 definition MUST 通过 `CommittedActionBranchEvaluator` 选择 `timeline.dodge.backstep`
- **AND** evaluator outcome MUST 反映修改后的 timeline payload

### Requirement: Timeline Preview 使用正式 Evaluator
Timeline preview MUST 基于本项目正式 `CommittedActionBranchEvaluator` 和 `ActionTimelineEvaluator` 展示当前 local time / local tick 结果。Preview MAY 提供 Editor-only 视觉预览绑定，但 MUST NOT 改变正式 gameplay 的 motion executor、animation presenter、blackboard writer 或角色帧管线。

#### Scenario: 数据预览显示 runtime outcome
- **WHEN** 设计者拖动 preview locator 到某一帧
- **THEN** preview MUST 显示 selected node id
- **AND** MUST 显示当前 local tick animation key、motion spec、active window facts 和 cue requests
- **AND** 显示结果 MUST 与 runtime evaluator 对同一 definition 的输出一致

#### Scenario: 视觉预览不成为 gameplay runner
- **WHEN** 编辑器实现动画、motion 或 cue 视觉预览
- **THEN** 预览代码 MUST 位于 Editor-only assembly
- **AND** runtime MUST NOT 引用该 preview binding
- **AND** 缺失 preview binding 时 MUST 显示明确未绑定状态，不得使用 scene 查找或隐藏 fallback

### Requirement: Timeline Editor 不编辑角色帧权威边界
Timeline Editor MUST NOT 暴露 `CharacterFramePipeline` phase、motion executor、Animancer presenter、blackboard writer、input consume 或 output apply 的重排入口。Timeline 只能编辑 committed action 的 selector/timeline 数据。

#### Scenario: 编辑 timeline 不改变 frame pipeline
- **WHEN** 设计者在 Timeline Editor 中添加 Motion 或 Animation clip
- **THEN** clip MUST 只改变 action timeline authoring data
- **AND** 最终 motion 仍由正式 output applier 调用统一 motion executor
- **AND** 最终 animation 仍由正式 output applier 调用正式 animation presenter

### Requirement: Timeline Editor 可测试
系统 MUST 提供 EditMode 测试和静态边界测试，证明迁移后的 editor 真正读写正式配置、preview 使用正式 evaluator，且 runtime 边界没有引入 Ref runner。

#### Scenario: 自动测试覆盖迁移能力
- **WHEN** 运行 timeline editor adapter EditMode 测试
- **THEN** 测试 MUST 覆盖正式 Dodge asset 读取
- **AND** MUST 覆盖 track add/remove/reorder
- **AND** MUST 覆盖 clip add/move/resize/delete
- **AND** MUST 覆盖 payload 写回
- **AND** MUST 覆盖保存后编译 Directional / Backstep runtime definition
- **AND** MUST 覆盖非法 timeline 报错
- **AND** MUST 覆盖 preview adapter 与 runtime evaluator 一致

#### Scenario: 静态边界测试
- **WHEN** 运行 runtime 边界测试
- **THEN** 测试 MUST 确认 runtime 不引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph 或 Taco runner
- **AND** 测试 MUST 确认 editor 菜单、窗口标题和文档不把本阶段称为通用 Skill Editor
