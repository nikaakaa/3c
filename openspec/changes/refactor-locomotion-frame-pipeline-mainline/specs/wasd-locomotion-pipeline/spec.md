## ADDED Requirements
### Requirement: Locomotion Frame Pipeline 主干编排
系统 MUST 将 `PlayerLocomotionController` 中的一帧 Locomotion 主干编排抽到 `LocomotionFramePipeline` 或等价 Module。该 Module MUST 负责 prepare decision facts、统一状态机 tick 协作、motion facts 解析、`BasicLocomotionFrame` 构建、phase/gait memory 更新和 runtime facts 写入协作；`PlayerLocomotionController` MUST 只保留 Runtime Adapter、输入读取、运动执行、动画提交、相机和 snapshot/restore 入口。

#### Scenario: Controller 委托一帧主干
- **WHEN** FullBody pipeline 请求 Locomotion 本帧结果
- **THEN** `PlayerLocomotionController` MUST 通过 `LocomotionFramePipeline` 或等价 Module 执行 prepare/evaluate/build 主干
- **AND** controller MUST NOT 继续直接承载完整的 prepare facts、state tick 和 frame build 细节

#### Scenario: Pipeline 输出保持纯数据
- **WHEN** `LocomotionFramePipeline` 完成本帧编排
- **THEN** 它 MUST 输出 `LocomotionDecisionFrame`、`CharacterStateMachineFrame`、`BasicLocomotionFrame` 或等价纯数据结果
- **AND** 它 MUST NOT 执行 `CharacterController.Move`
- **AND** 它 MUST NOT 播放 Animancer
- **AND** 它 MUST NOT 写角色 Transform

#### Scenario: FullBody 调度顺序不变
- **WHEN** `FullBodyFramePipeline` 调用 Locomotion 子职责
- **THEN** Locomotion frame pipeline MUST 继续在现有 FullBody gameplay decision / build motion 顺序内执行
- **AND** MUST NOT 新增绕过 FullBody 主线的独立 gameplay tick 入口

### Requirement: Locomotion 主干拆分保持行为一致
系统 MUST 在抽出 `LocomotionFramePipeline` 后保持当前基础移动行为、TurnBack 行为和 FullBody Action owner 行为一致，并提供 characterization 测试证明关键输出未改变。

#### Scenario: 基础移动四阶段保持
- **WHEN** 玩家输入 WASD 并通过正式 FullBody 主线推进
- **THEN** Idle、MoveStart、MoveLoop、MoveStop 的进入和退出 MUST 与拆分前一致
- **AND** Run latch、last moving gait 和 MoveStop gait memory MUST 与拆分前一致

#### Scenario: TurnBack 保持
- **WHEN** RunLoop 中出现有效反向输入
- **THEN** 系统 MUST 继续通过统一状态机进入 TurnBack
- **AND** TurnBack motion policy、input lock、motion window 和诊断 event id MUST 与拆分前一致

#### Scenario: Action owner 保持
- **WHEN** FullBody Action active
- **THEN** Locomotion frame pipeline MAY 生成 facts
- **AND** MUST NOT 提交基础移动 motion executor 输出
- **AND** MUST NOT 提交 base layer animation presenter 输出
