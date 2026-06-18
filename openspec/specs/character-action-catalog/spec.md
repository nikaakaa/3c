# character-action-catalog Specification

## Purpose
定义角色 Action Catalog 的作者入口、纯 runtime model 转换、Action 请求解析驱动和后续动作扩展验证边界。
## Requirements
### Requirement: 角色 Action Catalog 作者入口
系统 MUST 提供 `CharacterActionCatalogSO` 或等价角色动作目录作为角色 Action module 的正式逻辑配置入口。该目录 MUST 以稳定 `ActionStateId` 管理角色可用动作定义，并 MUST 能被 `CharacterConfigSO` 作为命名子模块引用。该目录 MUST NOT 直接持有动作动画 Profile、AnimationClip、Animancer runtime 对象、Locomotion graph 或 Unity scene object。

#### Scenario: Catalog 列出角色动作
- **WHEN** 设计者检查 Corin Action Catalog
- **THEN** catalog MUST 能列出 `Action.Dodge`
- **AND** 每个 entry MUST 暴露稳定 action id
- **AND** 每个 entry MUST 能定位对应动作定义
- **AND** catalog MUST NOT 要求设计者在 `CharacterConfigSO` 上为每个动作新增平铺字段

#### Scenario: Catalog 不持有表现资产
- **WHEN** 自动校验 Action Catalog
- **THEN** catalog MUST NOT 直接引用 `ActionAnimationProfileSO`
- **AND** MUST NOT 直接引用 `AnimationClip`
- **AND** MUST NOT 直接引用 Animancer Transition 或 runtime object
- **AND** 动作动画表现 MUST 继续通过独立动作动画绑定配置解析

### Requirement: 动作定义转换为纯 runtime model
系统 MUST 提供 `CharacterActionDefinitionSO` 或等价动作定义 SO，并能将其转换为纯 runtime action definition。runtime definition MUST 只包含动作解析、请求准入和 committed action branch 评估需要的值类型数据、稳定 ID、request binding、优先级、抗性、source input、body claim policy binding、selector definition 和 compiled tick timeline definition。runtime definition MUST NOT 持有 Unity asset、scene object、controller、presenter、input runtime object、AnimationClip 或 Animancer runtime object。Directional / Backstep 旧 variant 字段 MAY 作为迁移输入或 authoring 辅助存在，但正式 runtime motion、animation key、duration ticks、window 和 cue MUST 来自 selected ActionTimeline seconds authoring 经固定 tick interval 量化后的 timeline definition。

#### Scenario: Dodge definition 输出纯数据
- **WHEN** 运行时从 `Action.Dodge` definition 构建 runtime definition
- **THEN** 结果 MUST 包含 `Action.Dodge` action id
- **AND** MUST 包含 `ActionRequestType.Dodge` 或等价 request type
- **AND** MUST 包含 `InputRequestKind.Dodge` 或等价来源输入
- **AND** MUST 包含请求准入所需 priority 和 resistance
- **AND** MUST 包含 Dodge selector、Directional timeline 和 Backstep timeline 的纯数据 runtime definition
- **AND** Directional / Backstep 的 runtime motion、animation key、duration ticks、window 和 cue MUST 来自对应 timeline clip payload 的 compiled tick 数据
- **AND** MUST NOT 包含 `ScriptableObject`、`InputAction`、Animancer runtime 或场景实例引用

#### Scenario: 旧 Variant 不作为 runtime motion 权威
- **GIVEN** 动作定义中仍存在旧 Directional 或 Backstep variant 字段
- **WHEN** runtime 构建 `Action.Dodge`
- **THEN** 这些字段 MAY 被迁移工具或 validator 用于诊断
- **AND** runtime MUST NOT 从这些字段补齐 motion spec、animation key、duration ticks、window 或 cue
- **AND** 缺失对应 timeline payload 时 MUST 报告配置错误

#### Scenario: 非法定义报告错误
- **GIVEN** 动作定义缺失 action id、request type、source input、priority、resistance、selector、Directional timeline 或 Backstep timeline
- **WHEN** 运行配置校验
- **THEN** 校验 MUST 报告错误
- **AND** runtime MUST NOT 使用隐藏默认值、旧 variant 字段、Resources 或 sample asset 补齐定义

#### Scenario: seconds authoring 编译为 tick runtime
- **GIVEN** 动作定义中的 timeline authoring 使用 seconds 表达 Motion clip 范围
- **AND** 调用方从 simulation tick settings 提供 fixed tick interval compile context
- **WHEN** action definition compiler 构建 runtime definition
- **THEN** runtime definition MUST 保存 deterministic tick 区间
- **AND** runtime evaluator MUST NOT 在采样时重新读取 authoring seconds 字段

### Requirement: Catalog 驱动 Action 请求解析
Action request resolver MUST 通过正式 Action Catalog 查询动作定义，再基于动作请求、当前状态上下文和动作定义输出 `CharacterResolvedAction` 或等价纯数据结果。Dodge、Attack、Jump 或后续 Skill 的请求绑定、优先级、抗性、selector 入口和 branch/timeline reference MUST 来自 catalog definition 或其正式子配置；Dodge 的 runtime motion、animation key、duration ticks、window 和 cue MUST 由 selected ActionTimeline 在 Action lifecycle / branch evaluator 阶段输出，不得来自 `CharacterConfigSO` 上的动作平铺字段或旧 variant fallback。

#### Scenario: Dodge 请求通过 catalog 解析
- **GIVEN** 输入缓冲中存在未过期 Dodge 输入
- **AND** Corin Action Catalog 包含 `Action.Dodge` definition
- **WHEN** Dodge resolver 处理该请求
- **THEN** resolver MUST 从 catalog definition 读取 request binding、priority、resistance、selector entry 和必要 action id
- **AND** MUST 输出 Directional 或 Backstep 的 resolved action context，供后续 selector / timeline 评估
- **AND** MUST NOT 读取 `CharacterConfigSO.DodgeAction` 作为正式配置来源
- **AND** MUST NOT 从旧 Directional / Backstep variant 字段读取 runtime motion 或 animation payload

#### Scenario: 缺失 catalog 不 fallback
- **GIVEN** `CharacterConfigSO` 缺失 Action Catalog
- **OR** Action Catalog 缺失 `Action.Dodge` definition
- **WHEN** 正式 gameplay 路径尝试解析 Dodge 请求
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从旧 `DodgeAction` 字段、Resources、全局单例、Behavior Graph 或代码默认值继续运行

### Requirement: Action Catalog 支持后续动作扩展
Action Catalog MUST 为后续 LightAttack、Jump、Skill 或 HitReact 提供同一类数据入口。新增动作 MAY 需要自己的 resolver strategy，但 MUST NOT 要求新增独立 MonoBehaviour gameplay 入口、第二 Character frame pipeline、第二 motion executor、第二 animation presenter 或 `CharacterConfigSO` 平铺动作字段。

#### Scenario: 后续 LightAttack 使用同一 catalog
- **WHEN** 后续实现 LightAttack
- **THEN** LightAttack definition MUST 作为 Action Catalog entry 或等价动作定义进入角色配置
- **AND** Attack provider MUST 只提交 `ActionRequestType.Attack` 或等价请求
- **AND** Attack resolver MUST 基于 catalog definition 和当前 action context 决定具体攻击段
- **AND** 实现 MUST NOT 新增 `PlayerAttackController` 作为正式 gameplay tick 入口

#### Scenario: 后续 Skill 不绕过角色帧管线
- **WHEN** 后续实现 Skill 动作
- **THEN** Skill definition MUST 通过 Action Catalog 或批准的等价 Action module 配置入口进入运行时
- **AND** Skill 输出 MUST 继续通过 Character frame pipeline、Action request/resolver、body claim、motion executor 和 animation presenter 主线执行
- **AND** Skill MUST NOT 通过独立技能控制器直接移动角色或播放 base layer 动画

### Requirement: Action Catalog 可测试
系统 MUST 提供自动测试和静态边界验证，证明 Action Catalog 是正式动作逻辑入口，且没有重新引入 Dodge 特例 fallback 或表现层耦合。

#### Scenario: 自动测试覆盖 catalog 配置
- **WHEN** 运行 Action Catalog EditMode 测试
- **THEN** 测试 MUST 覆盖 catalog 解析 `Action.Dodge`
- **AND** MUST 覆盖重复 action id 报错
- **AND** MUST 覆盖缺失 `Action.Dodge` definition 报错
- **AND** MUST 覆盖缺失 Action Catalog 不 fallback 到 `CharacterConfigSO.DodgeAction`

#### Scenario: 静态边界验证
- **WHEN** 检查新增 Action Catalog 源码
- **THEN** 静态搜索 MUST 能确认 catalog 和 runtime definition 不引用 Animancer runtime
- **AND** MUST 能确认 catalog 不引用 `ActionAnimationProfileSO`
- **AND** MUST 能确认正式 runtime 不通过 `CharacterConfigSO.DodgeAction` 补齐缺失 Dodge 配置

### Requirement: Action Definition 使用通用 Branch Authoring
`CharacterActionDefinitionSO` 或批准等价动作定义 MUST 使用通用 Committed Action branch authoring 作为正式 branch 配置来源。Action definition compiler MUST 将该 branch authoring 编译为 `CommittedActionBranchDefinition` 或批准等价 runtime model。Dodge 专用 branch authoring、旧 variant 字段、single timeline authoring 或代码默认值 MUST NOT 作为正式 runtime branch fallback。

#### Scenario: Catalog 编译通用 Branch
- **GIVEN** Action Catalog 包含一个带通用 branch authoring 的 `Action.Dodge` definition
- **WHEN** runtime 构建 action catalog definition
- **THEN** `Action.Dodge` runtime definition MUST 包含从通用 branch authoring 编译出的 `CommittedActionBranchDefinition`
- **AND** selector、condition、timeline 和 body claim MUST 来自该通用 branch authoring
- **AND** runtime MUST NOT 根据 action id 特判读取 Dodge 专用 branch 字段

#### Scenario: 缺失 Branch 不 Fallback
- **GIVEN** action definition 缺失通用 branch authoring 或 branch authoring 非法
- **WHEN** action definition compiler 或 validator 运行
- **THEN** 系统 MUST 报告配置错误
- **AND** MUST NOT 从旧 Directional / Backstep variant、single timeline authoring、Resources、sample asset 或代码默认值补齐 branch

### Requirement: Action Catalog 支持编辑器 Action 导航
`CharacterActionCatalogSO` or an approved equivalent formal catalog MUST provide enough editor-readable data for Character Behavior Editor to list available committed actions. Each listed entry MUST expose a stable action id and a `CharacterActionDefinitionSO` reference or equivalent formal action definition reference. Editor navigation MUST reuse this catalog data and MUST NOT introduce a separate editor-only action registry.

#### Scenario: Catalog Entry 可被编辑器列出
- **GIVEN** Corin `CharacterConfigSO` references a formal Action Catalog
- **AND** the catalog contains `Action.Dodge`
- **WHEN** Character Behavior Editor builds the `CommittedActionLeaf` navigation list
- **THEN** the list MUST include `Action.Dodge`
- **AND** the entry MUST be able to locate the corresponding `CharacterActionDefinitionSO`
- **AND** the editor MUST NOT read a separate Dodge-specific editor field

#### Scenario: 新 Action 注册后可见
- **GIVEN** a new action definition is added to the formal Action Catalog
- **WHEN** Character Behavior Editor rebuilds the `CommittedActionLeaf` navigation list
- **THEN** the new action MUST appear as a selectable entry
- **AND** selecting it MUST open that action definition in Committed Branch mode
- **AND** no additional Behavior Source schema migration is required

#### Scenario: Invalid Catalog Entry 阻止导航
- **GIVEN** the catalog contains an entry with a missing definition reference
- **OR** the catalog contains duplicate action ids
- **WHEN** Character Behavior Editor builds the navigation list
- **THEN** the invalid entry or duplicate id MUST be reported
- **AND** the editor MUST NOT silently remove the problem and continue with a fallback action

### Requirement: Catalog 导航保持运行时边界
Action Catalog editor navigation MUST not change runtime catalog compilation or action request resolution. Runtime gameplay MUST continue to consume compiled action definitions through the approved Action Catalog / Action resolver path, while the editor navigation only locates which formal `CharacterActionDefinitionSO` the designer wants to edit.

#### Scenario: Editor Navigation 不进入 Runtime Definition
- **WHEN** action definitions are compiled for runtime
- **THEN** editor navigation UI state, selected catalog row, picker search text and GraphView selection MUST NOT appear in runtime action definitions
- **AND** runtime action request resolution MUST still be driven by formal catalog data and action lifecycle

