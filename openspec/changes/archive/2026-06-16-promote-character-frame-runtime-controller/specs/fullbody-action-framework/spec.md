## ADDED Requirements
### Requirement: FullBody Action 作为兄弟 Submitter
FullBody Action framework MUST 在目标架构中作为 Character frame owner 下的 sibling submitter 存在。它 MUST 提交动作请求、动作状态事实、full-body occupancy claim、action motion candidate 和 action animation candidate。它 MUST NOT 作为正式 Unity tick 入口、Character runtime host owner 或 Locomotion 上级 owner。

#### Scenario: Dodge 通过 FullBody Action submitter 提交
- **GIVEN** 输入缓冲中存在有效 Dodge 请求
- **WHEN** Character frame pipeline 收集 FullBody Action submitter 输出
- **THEN** FullBody Action submitter MUST 提交 Dodge action request 或 resolved action candidate
- **AND** MUST 提交 full-body occupancy claim
- **AND** MUST NOT 直接执行 Dodge movement
- **AND** MUST NOT 直接播放 Dodge animation

#### Scenario: FullBody 不拥有 Locomotion
- **GIVEN** Locomotion submitter 已提交基础移动候选输出
- **AND** FullBody Action submitter 已提交 full-body occupancy claim
- **WHEN** CharacterFramePlan 压制 Locomotion 输出
- **THEN** 压制 MUST 来自角色级计划
- **AND** FullBody Action framework MUST NOT 写 Locomotion runtime 私有状态来表达压制
- **AND** FullBody Action framework MUST NOT 调用 Locomotion output runtime 直接执行压制

#### Scenario: Future Action 不新增入口
- **WHEN** 后续新增 Attack、Jump 或 HitReact
- **THEN** 新动作 MUST 通过 FullBody Action submitter、action provider/resolver 或等价 sibling submitter 接入
- **AND** MUST NOT 新增 `PlayerAttackController`、`PlayerJumpController` 或等价 MonoBehaviour 作为正式 gameplay tick 入口

### Requirement: PlayerFullBodyActionController 降级为兼容 Adapter
`PlayerFullBodyActionController` 或等价 FullBody MonoBehaviour MUST 从正式 frame owner 降级为兼容 adapter、FullBody Action 配置/诊断 view 或 runtime module host。它 MAY 保留旧 API 以便测试或迁移，但正式 Corin playable 主线 MUST 不从它的 `Update` 进入。

#### Scenario: FullBody Update 不再正式驱动
- **WHEN** 检查 Corin 正式 prefab/scene 的 gameplay driver
- **THEN** `PlayerFullBodyActionController.AutoUpdate` MUST NOT 是正式 gameplay driver
- **AND** `PlayerFullBodyActionController.Update` MUST NOT 推进正式主线
- **AND** 正式主线 MUST 由 `CharacterFrameRuntimeController` 推进

#### Scenario: FullBody controller 不创建正式 host
- **WHEN** 检查 FullBody controller 运行时代码
- **THEN** 它 MUST NOT 直接创建正式 `CharacterFrameRuntimeHost`
- **AND** MUST NOT 私有持有正式 submitter graph
- **AND** MAY 暴露兼容 view 或委托到角色级 controller

#### Scenario: Legacy integrated adapter 退出正式主线
- **WHEN** Corin 正式角色推进一帧
- **THEN** runtime submitter graph MUST NOT 只依赖 `FullBodyIntegratedFrameAdapter`
- **AND** `FullBodyIntegratedFrameAdapter` MAY 只作为 legacy compatibility path 或 characterization test helper
- **AND** 新功能 MUST NOT 继续扩展 integrated adapter
