## Context
当前项目已经有清晰的基础移动输入边界：`IBasicLocomotionInputSource` 输出 `BasicLocomotionInputSnapshot`，`PlayerLocomotionController` 每帧读取 Move/Look，再交给 `BasicLocomotionPipeline`、运动执行端口和动画外观层。这个边界适合移动，但不适合直接承载攻击、闪避、跳跃、交互等离散输入。

预输入和输入缓冲要解决的是手感与动作衔接：玩家在状态还不能消费时提前按下动作键，系统在短时间窗口内保留请求，等状态或仲裁规则允许时立刻消费。这里的关键不是网络，而是本地玩法边界：输入层不能直接决定动作结果，必须让状态机或动作仲裁层消费。

本变更只落“本地预输入/输入缓冲层”。完整 Simulation Tick、预测回滚、Fantasy 输入同步和服务器权威模拟应当后续单独规划。

## Goals
- 建立离散按钮事实模型。
- 建立本地输入请求缓冲。
- 支持 Attack、Dodge、Jump、Interact 等请求种类的第一版枚举。
- 支持短窗口预输入，请求在窗口内等待玩法层消费。
- 支持请求过期和同次模拟重复消费保护。
- 保持输入缓冲为纯 C# 数据层，不持有 Unity 场景对象。
- 保持当前 Locomotion Move/Look 主线不变。
- 用 EditMode 测试证明输入缓冲可生成、可过期、可消费且结果确定。

## Non-Goals
- 不建立完整 tick 系统。
- 不接入真实网络包格式。
- 不做服务器输入等待队列。
- 不做预测回滚状态快照。
- 不实现攻击/闪避/跳跃状态或取消窗口业务。
- 不改变 `BasicLocomotionPipeline` 的 Move/Look 语义。
- 不新增全局输入服务或第二套 `PlayerController`。

## Decisions

### Decision: 本地缓冲窗口用离散 step 表达
第一版输入缓冲窗口使用 `currentStep`、`originStep`、`expireStep` 或等价整数表达。运行时可以先由本地帧序号或轻量输入 step source 提供，测试中使用 fake step 推进。

Rationale: 预输入窗口需要稳定、可测试。完整 Simulation Tick 还没审批，因此本变更只要求一个局部离散 step，不把全项目时间系统拖进来。

### Decision: 输入缓冲只保存请求，不保存动作结果
输入缓冲保存 `kind`、来源 step、过期 step、是否在本次模拟已消费等数据。它不保存“成功进入攻击 B”这类动作结果。

Rationale: 这能让未来预测回滚从相同输入事实重新计算消费结果，也避免当前阶段把动作业务塞进输入层。

### Decision: Locomotion 不消费动作请求
`PlayerLocomotionController` 继续只消费 Move/Look。Attack、Dodge、Jump、Interact 请求等待未来 ActionArbiter/HFSM 消费。

Rationale: Locomotion 和 Action 是不同消费域。把离散动作请求塞进 Locomotion controller 会让当前入口继续膨胀成大控制器。

### Decision: 第一版不做真实 ActionArbiter
本变更可以提供 fake consumer 或简单测试 policy 来验证“窗口内可消费、过期不可消费、消费后不重复”。真实动作仲裁器、取消窗口和连招状态后续单独 OpenSpec。

Rationale: 当前目标是并行做输入层，不能因为输入缓冲而提前实现完整动作系统。

## Proposed Shape

```text
Unity Input System button adapter
  -> InputButtonState
  -> InputRequestBuffer
  -> future ActionArbiter / HFSM consumer

PlayerLocomotionController
  -> still consumes Move/Look only
```

## Boundaries

### Input Button Adapter
可以读取 `InputActionReference`，但只能生成按钮事实。不得直接调用状态机、动作控制器、运动执行器或动画外观层。

### Input Request Buffer
只保存请求 kind、origin step、expire step、来源按钮和本次模拟中的消费状态。它不得持有 Unity 对象引用。

### Consumer
未来 ActionArbiter/HFSM 读取请求并决定消费。消费成功可以返回本次模拟结果，但不得反写输入事实为永久动作结果。

### Network Preparation
本变更只保证缓冲层不污染未来网络语义，不新增协议文件。若实现发现必须修改 Fantasy proto、服务端代码或预测回滚驱动，必须停止并新建单独 OpenSpec。

## Risks / Trade-offs
- 风险：过早设计完整输入服务会拖慢当前 3C demo。
  - 缓解：第一版只做按钮事实和请求缓冲，不接完整动作系统。
- 风险：当前活跃 UnityHFSM 迁移也在调整输入端口。
  - 缓解：本变更依赖其边界，不重复创建 `PlayerLocomotionController` 或新的 Locomotion 输入端口。
- 风险：消费语义没有真实攻击系统验证。
  - 缓解：用 fake consumer 测试过期、消费和确定性；真实动作层后续单独提案。
- 风险：局部 step 被误认为完整网络 tick。
  - 缓解：文档和命名中明确第一版只是本地缓冲 step，不是 SimulationClock。

## Stop Conditions
- 需要新增第二套角色控制器入口。
- 需要让输入缓冲直接调用状态切换、Animancer、`CharacterController.Move` 或 Cinemachine。
- 需要修改 Fantasy 协议、服务端模拟或预测回滚驱动。
- 需要实现完整 ActionArbiter、攻击连招或取消窗口业务。
- 需要绕过当前 Locomotion pipeline 或 UnityHFSM proposal 中的输入端口边界。
