## ADDED Requirements

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
系统 MUST 提供 `CharacterActionDefinitionSO` 或等价动作定义 SO，并能将其转换为纯 runtime action definition。runtime definition MUST 只包含动作解析需要的值类型数据、稳定 ID、request binding、优先级、抗性、动作运动 seed、动画 key seed、variant 和 timeline window 数据。runtime definition MUST NOT 持有 Unity asset、scene object、controller、presenter 或 input runtime object。

#### Scenario: Dodge definition 输出纯数据
- **WHEN** 运行时从 `Action.Dodge` definition 构建 runtime definition
- **THEN** 结果 MUST 包含 `Action.Dodge` action id
- **AND** MUST 包含 `ActionRequestType.Dodge` 或等价 request type
- **AND** MUST 包含 `InputRequestKind.Dodge` 或等价来源输入
- **AND** MUST 包含 Directional 与 Backstep variant 的 duration、distance、priority、resistance 和 rotateToDirection
- **AND** MUST NOT 包含 `ScriptableObject`、`InputAction`、Animancer runtime 或场景实例引用

#### Scenario: 非法定义报告错误
- **GIVEN** 动作定义缺失 action id、request type、source input、priority、resistance 或必要 variant 数据
- **WHEN** 运行配置校验
- **THEN** 校验 MUST 报告错误
- **AND** runtime MUST NOT 使用隐藏默认值补齐定义

### Requirement: Catalog 驱动 Action 请求解析
Action request resolver MUST 通过正式 Action Catalog 查询动作定义，再基于动作请求、当前状态上下文和动作定义输出 `CharacterResolvedAction` 或等价纯数据结果。Dodge、Attack、Jump 或后续 Skill 的配置数值 MUST 来自 catalog definition 或其正式子配置，不得来自 `CharacterConfigSO` 上的动作平铺字段。

#### Scenario: Dodge 请求通过 catalog 解析
- **GIVEN** 输入缓冲中存在未过期 Dodge 输入
- **AND** Corin Action Catalog 包含 `Action.Dodge` definition
- **WHEN** Dodge resolver 处理该请求
- **THEN** resolver MUST 从 catalog definition 读取 Dodge motion、priority、resistance 和 variant 配置
- **AND** MUST 输出 Directional 或 Backstep resolved action
- **AND** MUST NOT 读取 `CharacterConfigSO.DodgeAction` 作为正式配置来源

#### Scenario: 缺失 catalog 不 fallback
- **GIVEN** `CharacterConfigSO` 缺失 Action Catalog
- **OR** Action Catalog 缺失 `Action.Dodge` definition
- **WHEN** 正式 gameplay 路径尝试解析 Dodge 请求
- **THEN** 系统 MUST 报告配置错误或拒绝动作输出
- **AND** MUST NOT 从旧 `DodgeAction` 字段、Resources、全局单例或代码默认值继续运行

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

