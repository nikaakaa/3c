# character-input-pipeline Delta

## ADDED Requirements

### Requirement: 动作 Request 必须由 Authoring 声明业务 Timing Class

`CharacterActionRequestDefinition` MUST为每个离散request保存稳定timing class，当前正式值为`Immediate`与`Offensive`。Timing class MUST表达request的业务类别，不得保存具体Network Model、Tick延迟或packet policy。CharacterInputProfile Inspector与Agent authoring MUST读写同一字段；缺失或非法值 MUST作为配置错误，MUST不按request id、InputAction显示名或字符串前缀推断类别。

#### Scenario: 作者配置攻击请求

- **WHEN** 作者把Corin Attack request标记为Offensive
- **THEN** CharacterInputProfile MUST保存该timing class
- **AND** Agent snapshot与Inspector MUST读取同一配置

#### Scenario: 请求没有合法 Timing Class

- **WHEN** CharacterInputProfile包含未定义的timing class值
- **THEN** 配置校验 MUST失败
- **AND** Runtime MUST不回退为Immediate

### Requirement: Network Model 必须独立解释 Request Timing Class

Input Adapter MUST先捕获带稳定request identity、capture sequence和timing class的动作事实；具体eligible Tick MUST由当前Session Source或Network Model timing policy决定。Standard Local与Preview MAY将全部类别映射为0 Tick；DeterministicRollback MAY为Offensive配置固定Tick延迟。BTSMTL、Program、Kernel和CharacterSimulationState MUST只消费已经eligible并写入`CharacterSimulationInput.Requests`的正式request，MUST不读取Network Model policy。

#### Scenario: 单机调试同一 Corin 输入配置

- **WHEN** Standard Local Session使用标记为Offensive的Attack request
- **THEN** Local input adapter MAY在当前Tick立即写入该request
- **AND** BTSMTL与Program MUST不需要Rollback专用节点

#### Scenario: Rollback 调度 Offensive Request

- **WHEN** DeterministicRollback policy把Offensive映射为2 Tick
- **THEN** Rollback Source MUST在capture tick记录request并在eligible tick写入正式Fixed input
- **AND** 远端收到的仍是普通portable input request

#### Scenario: 连续输入与延迟请求并存

- **WHEN** MoveAxis持续更新且一个Offensive request仍在等待eligible tick
- **THEN** MoveAxis MUST每Tick立即进入CharacterSimulationInput.Values
- **AND** pending request MUST不阻塞连续输入传播

### Requirement: 离散 Request 调度必须保持捕获顺序

需要选择性延迟的Model Source MUST以request capture sequence维护有界pending schedule。后捕获request MUST不越过尚未eligible的前序request；request到期后 MUST保留原始request id与sequence并进入正式input history。Pending schedule影响未来模拟时 MUST进入该Model Source的checkpoint/restore合同，不得藏在Unity UI状态或建立第二个Gameplay request buffer。

#### Scenario: Attack 后立即输入 Dodge

- **WHEN** Offensive Attack仍在等待eligible tick且之后捕获Dodge request
- **THEN** Dodge MUST不越过Attack写入更早SimulationTick
- **AND** 两个request MUST保留各自capture sequence

#### Scenario: Restore 到 Request 尚未 Eligible 的 Tick

- **WHEN** Rollback Source恢复到pending request尚未写入input frame的历史点
- **THEN** Source MUST从checkpoint恢复相同pending schedule
- **AND** MUST不重新读取InputAction生成重复request
