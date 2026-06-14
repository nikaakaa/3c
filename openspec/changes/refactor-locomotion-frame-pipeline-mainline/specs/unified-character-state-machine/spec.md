## ADDED Requirements
### Requirement: Locomotion Frame Pipeline 不改变状态机权威
系统 MUST 保持 `PlayerFullBodyActionController` 作为统一角色状态机的唯一正式 runtime owner。`LocomotionFramePipeline` MAY 协作构建 `CharacterStateMachineContext` 并调用外部传入的 runner 推进一帧，但 MUST NOT 创建 runner、保存独立 active state path 作为权威、注册 tick driver 或绕过 FullBody phase order。

#### Scenario: Runner owner 不变
- **WHEN** 检查运行时代码
- **THEN** `LocomotionFramePipeline` MUST NOT 调用 `new CharacterStateMachineRunner`
- **AND** `PlayerFullBodyActionController` MUST 仍是正式 runner 创建点

#### Scenario: Pipeline 只协作状态机推进
- **WHEN** Locomotion frame pipeline 需要推进状态机
- **THEN** 它 MUST 使用外部传入的 runner 或 runner facade
- **AND** MUST NOT 持有第二套状态机实例
- **AND** MUST NOT 自行注册 simulation tick handler

#### Scenario: 状态输出只被正式 adapter 执行
- **WHEN** 统一状态机产出 Locomotion 运动或动画输出
- **THEN** `LocomotionFramePipeline` MUST 返回纯数据结果
- **AND** 正式 Runtime Adapter MUST 继续负责是否调用 motion executor 或 animation presenter
