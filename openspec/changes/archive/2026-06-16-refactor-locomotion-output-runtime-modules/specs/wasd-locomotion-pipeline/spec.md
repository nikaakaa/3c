## ADDED Requirements
### Requirement: Locomotion Output Runtime 模块化
系统 MUST 将 Locomotion 输出副作用拆分为明确的 output runtime modules。`ILocomotionOutputRuntimePort` MUST 作为 FullBody 输出层访问基础移动 motion execution、locomotion animation presentation、runtime facts 写入和 output completion 的唯一入口。输出模块 MUST NOT 选择逻辑状态、创建状态机 runner 或重算 frame decision。

#### Scenario: Motion execution 只经 motion executor
- **WHEN** Locomotion output runtime 执行基础移动位移
- **THEN** 它 MUST 只通过 `IBasicLocomotionMotionExecutor` 或等价 motion executor 端口执行
- **AND** MUST NOT 直接调用 `CharacterController.Move`
- **AND** MUST NOT 直接写角色 `Transform.position`

#### Scenario: Animation presentation 只消费上下文
- **WHEN** Locomotion output runtime 提交基础移动动画
- **THEN** 它 MUST 构建并提交 `MovementAnimationContext` 或等价表现上下文
- **AND** animation presenter MUST NOT 决定当前逻辑状态
- **AND** output runtime MUST NOT 直接读取 Animancer runtime state 作为 transition 权威

#### Scenario: Runtime facts 写入顺序明确
- **WHEN** Locomotion output runtime 写入 action facts、animation facts 或 locomotion facts
- **THEN** facts MUST 使用当前 frame/result 的 source step
- **AND** facts writer MUST NOT 重新推进状态机
- **AND** rollback/replay MUST 能比较迁移前后的 facts 结果

#### Scenario: Complete tick 不成为第二主线
- **WHEN** Locomotion output runtime 完成本帧 output
- **THEN** 它 MAY 处理 camera resolve、rollback camera basis sync 和 run latch reset
- **AND** MUST NOT 读取输入并推进 gameplay
- **AND** MUST NOT 恢复 Locomotion direct tick 为正式路径

### Requirement: Locomotion Output Runtime 阶段必须分离
系统 MUST 将 Locomotion output runtime 分为 motion apply、animation presentation、runtime facts write 和 output completion 阶段。每个阶段 MUST 有独立 Module 或等价清晰职责，且 MUST 能被测试单独观察。

#### Scenario: Motion 和 animation 分离
- **WHEN** Locomotion output runtime 同一帧执行移动和动画
- **THEN** motion apply MUST 通过 motion output module 完成
- **AND** animation presentation MUST 通过 animation output module 完成
- **AND** 两者 MUST NOT 共享会改变逻辑状态的可变决策对象

#### Scenario: Facts writer 不做输出执行
- **WHEN** runtime facts writer 写入 blackboard facts
- **THEN** 它 MUST NOT 执行 movement
- **AND** MUST NOT present animation
- **AND** MUST 使用 upstream frame/result 提供的 source step

#### Scenario: Completion module 不读新输入
- **WHEN** output completion module 同步 camera basis 或 reset run latch
- **THEN** 它 MUST NOT 读取新输入
- **AND** MUST NOT 重新构建 Locomotion frame
- **AND** MUST NOT 触发 FullBody 状态机 transition

### Requirement: Locomotion Output Runtime 不得创建新执行出口
系统 MUST 保持现有 motion executor、animation presenter 和 unified character frame pipeline 为正式执行出口。Locomotion output moduleization MUST NOT 引入 fallback executor、parallel presenter 或直接 scene mutation path。

#### Scenario: 没有 fallback motion executor
- **WHEN** formal motion executor 缺失或未配置
- **THEN** production initialization MUST fail explicitly or block execution
- **AND** MUST NOT silently fall back to direct movement
- **AND** MUST NOT create hidden local executor

#### Scenario: 没有 parallel animation presenter
- **WHEN** locomotion animation output 需要表现
- **THEN** output runtime MUST use the configured presenter Interface
- **AND** MUST NOT create a second presenter path
- **AND** MUST NOT write Animancer state directly as gameplay authority
