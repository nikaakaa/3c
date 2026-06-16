## ADDED Requirements
### Requirement: FullBody 输出运行时模块化
系统 MUST 将 FullBody frame 输出副作用拆分为明确的 runtime output modules。`CharacterFramePipeline` MUST 继续只依赖角色帧运行时端口；`FullBodyRuntimePortAdapter` MAY 作为生产包装存在，但 MUST NOT 长期把所有 output 操作直接转回 `PlayerFullBodyActionController` 的大操作面板。输出模块 MUST 只消费状态机 frame、frame output、action motion result 和必要的运行时端口，不得重新做请求仲裁、transition 选择或 timeline facts 采样。

#### Scenario: Pipeline 只调用端口
- **WHEN** `CharacterFramePipeline` 执行 BuildMotion、ExecuteMotion、PresentationBridge 和 WriteSnapshotAndEvents
- **THEN** 它 MUST 只调用 `ICharacterFrameRuntimePort` 或 `IFullBodyOutputRuntimePort`
- **AND** MUST NOT 接收或保存 `PlayerFullBodyActionController`
- **AND** MUST NOT 直接调用 motion executor、animation presenter 或 input buffer component

#### Scenario: Adapter 委托 output modules
- **WHEN** 生产路径通过 `FullBodyRuntimePortAdapter` 执行输出
- **THEN** adapter MUST 委托给 FullBody output runtime module 或等价子职责
- **AND** adapter MUST NOT 自己承载全部 motion、animation、facts、snapshot 和 diagnostics 逻辑
- **AND** `PlayerFullBodyActionController` MUST NOT 继续作为 output 操作面板扩张

#### Scenario: 输出模块不改变状态权威
- **WHEN** FullBody output module 执行本帧副作用
- **THEN** 它 MUST NOT 创建、重置或推进 `CharacterStateMachineRunner`
- **AND** MUST NOT 重新选择 active state
- **AND** MUST NOT 重新采样 current/projected/target timeline facts
- **AND** MUST NOT 重新计算 action motion result

#### Scenario: 输出顺序保持可测试
- **WHEN** 运行 FullBody output runtime EditMode 测试
- **THEN** input consume MUST 发生在 motion execution phase
- **AND** action/basic motion execution MUST 早于 animation presentation
- **AND** runtime facts 写入 MUST 晚于 motion result resolve
- **AND** state snapshot update MUST 晚于 motion 和 animation presentation

### Requirement: FullBody 输出模块职责边界
FullBody 输出模块 MUST 以明确 Module 组合承载 frame 输出副作用。每个 Module MUST 有稳定 Interface，且 Interface MUST 只暴露调用者完成该阶段所需的最小 frame/result/facts 数据。任何 Module 如果需要读取状态图 definition、重新采样 timeline 或重新判断 transition，系统 MUST 拒绝该拆分并保留为后续独立 proposal。

#### Scenario: Motion 输出模块只消费 resolved command
- **WHEN** FullBody motion output module 执行动作位移
- **THEN** 它 MUST 接收已经 resolve 的 motion command 或 action motion result
- **AND** MUST NOT 计算 action 每帧距离
- **AND** MUST NOT 判断 action 是否完成
- **AND** MUST NOT 直接调用 Unity movement primitive

#### Scenario: Animation 输出模块只消费 resolved presentation
- **WHEN** FullBody animation output module 执行动画表现
- **THEN** 它 MUST 接收已经 resolve 的 animation presentation request
- **AND** MUST NOT 选择 active state
- **AND** MUST NOT 判断 locomotion/action animation exit condition
- **AND** MUST NOT 直接改变 frame pipeline phase order

#### Scenario: Facts 和 snapshot 分离
- **WHEN** FullBody runtime 写入 action facts 和 state snapshot
- **THEN** facts writer MUST 只写运行时事实
- **AND** snapshot writer MUST 只提交 runner 输出的状态身份
- **AND** 两者 MUST 可分别通过 EditMode 测试验证
- **AND** 两者 MUST NOT 互相承担对方职责

### Requirement: FullBody 输出拆分不得产生分裂路径
系统 MUST 保持唯一角色帧管线、唯一 FullBody runner owner、唯一正式 motion executor 出口和唯一正式 animation presentation 出口。FullBody 输出模块化 MUST 是实现迁移，不得创建绕过现有管线的新提交路径。

#### Scenario: 没有第二条 frame pipeline
- **WHEN** FullBody output runtime 被生产代码调用
- **THEN** 调用 MUST 仍来自 `CharacterFramePipeline`
- **AND** MUST NOT 新增 parallel frame pipeline
- **AND** MUST NOT 让 action/locomotion controller 各自独立提交最终帧结果

#### Scenario: 没有第二条 motion 或 animation 出口
- **WHEN** action motion 或 animation presentation 需要执行
- **THEN** output modules MUST 通过现有正式 executor/presenter Interface
- **AND** MUST NOT 创建 fallback executor
- **AND** MUST NOT 直接绕过现有 output port
