## MODIFIED Requirements
### Requirement: PlayerFullBodyActionController 退役
系统 MUST 删除 `PlayerFullBodyActionController` 或等价 FullBody 主入口 MonoBehaviour。FullBody Action framework MUST 作为 `CharacterFrameRuntimeController` 下的 sibling submitter、action runtime、state facts provider 和 output runtime dependencies 参与角色帧管线。FullBody Action framework MUST NOT 保留独立 Unity Tick、兼容 controller 转发入口或 FullBody 主调度入口。

#### Scenario: 正式主线没有 FullBody controller
- **WHEN** 检查 Corin 正式 prefab/scene 的 gameplay driver
- **THEN** 正式主线 MUST 由 `CharacterFrameRuntimeController` 推进
- **AND** prefab/scene MUST NOT 挂载 `PlayerFullBodyActionController`
- **AND** 运行时代码 MUST NOT 依赖该 controller 的 `Update`、`Tick`、`RuntimePort` 或 `OutputRuntime`

#### Scenario: Action 请求由模块提交
- **WHEN** FullBody Action 处理 Dodge、Attack、Jump 或等价请求
- **THEN** 它 MUST 通过 action request provider/resolver、FullBody Action runtime 或等价 sibling submitter 提交候选
- **AND** MUST 进入统一请求/打断仲裁
- **AND** MUST NOT 通过 FullBody 主入口方法硬编码具体动作分支

#### Scenario: Locomotion controller 作为 adapter
- **WHEN** Character frame pipeline 需要 Locomotion 输入、相机 facts、运动执行或动画提交
- **THEN** `PlayerLocomotionController` MAY 作为外围 adapter 提供这些能力
- **AND** 它 MUST NOT 成为 winning submission 选择或状态切换的第二权威
- **AND** FullBody Action runtime MUST NOT 通过 controller 私有字段压制 Locomotion 输出

## ADDED Requirements
### Requirement: FullBody Action Submitter 不拥有 Locomotion 构建
FullBody Action submitter MUST 只拥有 action 请求、action occupancy、action motion、action animation 和 action facts 的构建。它 MUST NOT 构建 Locomotion motion、Locomotion animation、Locomotion facing、camera facts 或 Locomotion output fallback。

#### Scenario: Action submitter 不压制 Locomotion 私有输出
- **GIVEN** Locomotion submitter 已提交 tick N 的 Locomotion motion candidate
- **AND** FullBody Action submitter 已提交 Dodge action candidate
- **WHEN** Character frame pipeline 仲裁 tick N
- **THEN** FullBody Action submitter MAY 提交 full-body occupancy claim 影响仲裁
- **AND** MUST NOT 直接改写 Locomotion submitter 的候选内容
- **AND** MUST NOT 调用 Locomotion controller 私有状态构建 action 输出

### Requirement: Integrated FullBody Adapter 退场
`FullBodyIntegratedFrameAdapter` 或等价旧集成 adapter MUST 从正式生产图退场。若短期保留该类型用于迁移测试，它 MUST 被标记为非正式兼容测试资产，且 MUST NOT 被 prefab、scene、runtime controller、submitter graph 或 rollback replay 正式路径引用。

#### Scenario: 生产图没有 Integrated Adapter
- **WHEN** 检查正式 runtime 装配、Corin prefab/scene 和 rollback replay 路径
- **THEN** 它们 MUST NOT 引用 `FullBodyIntegratedFrameAdapter` 作为 frame source、submitter、runtime port 或 output adapter
- **AND** 新的 frame submitter MUST 由明确 Locomotion 与 FullBody Action sibling submitter 组合
- **AND** MUST NOT 新增等价的 FullBody integrated adapter 替代类

### Requirement: FullBody Action Runtime 职责归属
FullBody Action runtime MUST 承载动作请求配置、请求准入所需 facts、当前 action resistance、resolved action facts 和 full-body occupancy claim。它 MUST NOT 承载 Unity 生命周期、角色级 Tick、状态机 runner 创建、motion executor 调用或 animation presenter 调用。

#### Scenario: Runtime 只提供动作事实和候选
- **GIVEN** 输入缓冲中存在有效 Dodge 请求
- **WHEN** FullBody Action runtime 参与 `GameplayDecision` 和 `BuildMotion`
- **THEN** 它 MUST 提交 action request 或 resolved action facts
- **AND** MUST 提交 full-body occupancy claim、action motion candidate 和 action animation candidate
- **AND** MUST NOT 直接执行 Dodge movement 或播放 Dodge animation

#### Scenario: 新动作不新增 Player controller
- **WHEN** 后续新增 Attack、Jump 或 HitReact
- **THEN** 新动作 MUST 通过 FullBody Action runtime、provider/resolver 或 sibling submitter 接入
- **AND** MUST NOT 新增 `PlayerAttackController`、`PlayerJumpController` 或等价 MonoBehaviour 作为正式 gameplay tick 入口
- **AND** MUST NOT 重新引入 `PlayerFullBodyActionController`
