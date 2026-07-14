## Context

当前运动主线是：

```text
Input / Graph / Timeline
  -> MotionContribution
  -> MotionResolver
  -> MotionIntent
  -> CharacterMotionStage
       -> CharacterController.Move
       -> CharacterController.transform / isGrounded
  -> MotionResult
  -> ResolvedCharacterMotionFact
```

这里混在一起的是三件不同的事：

1. **玩法意图**：输入、动作曲线、击退和 warp 最终想让角色怎么动；
2. **世界约束求解**：墙、地面、台阶和斜坡允许角色实际怎么动；
3. **逻辑位姿真值**：本 tick 最终位置、旋转和 grounded 是什么。

第一件事属于 Character gameplay pipeline。第二件事可能由 Unity `CharacterController`、未来纯 C# KCC 或另一套确定性模拟完成。第三件事必须保持唯一写入边界，供碰撞、判定、网络和表现插值读取。

## Terms

- **MotionIntent**：经过 contribution 仲裁、modifier 和本 tick correction 计划后，角色请求执行的 displacement、velocity 与 yaw。
- **Motion Executor**：接收当前逻辑体状态和 `MotionIntent`，结合具体世界/碰撞实现，返回实际执行结果。它不读取 Graph、Timeline、Action 或 Network Model。
- **Logic Pose Port**：读取当前逻辑位姿，并执行外部权威位姿或显式重定位的唯一端口。它不负责 visual root 插值。
- **Motion Execution Result**：执行器返回的 actual displacement、position、rotation、grounded 与碰撞摘要；`CharacterMotionStage` 据此生成既有 `MotionResult` 和 sync fact。
- **Authoritative Simulation Backend**：服务端独立从 canonical input/action state 推进运动语义并执行世界约束的实现，不等于接收客户端 resolved displacement 后做限幅。

## End-to-End Chain

### 当前 Unity LocalSolver

```text
MotionContribution
  -> MotionResolver
  -> MotionModifier
  -> correction plan
  -> MotionIntent
  -> ICharacterMotionExecutor
       -> UnityCharacterControllerMotionExecutor
            -> CharacterController.Move
  -> MotionExecutionResult
  -> CharacterMotionStage
       -> MotionResult
       -> ResolvedCharacterMotionFact
  -> logic sample history
  -> Presentation interpolation
```

### ExternalPose

```text
ExternalPoseSample
  -> CharacterMotionStage
  -> ICharacterLogicPosePort.ApplyExternalPose
  -> canonical logic pose
  -> MotionResult / logic sample
  -> Presentation interpolation
```

`ExternalPose` 不创建、不调用 motion executor，也不要求 `CharacterController`。

### 后续服务端权威方案

```text
canonical input + accepted action state
  -> server-owned motion intent generation
  -> chosen authoritative simulation backend
  -> canonical server pose
  -> snapshot / correction
```

客户端的 `ResolvedCharacterMotionFact` 可以用于预测对账和诊断，但不能作为服务端 canonical intent 或 canonical pose 的唯一来源。

## Decisions

### 1. 拆成 Motion Executor 与 Logic Pose Port 两个边界

`CharacterMotionStage` 需要“执行一个运动请求”和“应用外部已确认位姿”两种不同能力。把它们塞进一个大接口，会让 ExternalPose 也看见无意义的 `Step`，并让纯展示角色看起来可以执行碰撞模拟。

因此：

- `LocalSolver` 必须配置 logic pose port 和 motion executor；
- `ExternalPose` 只必须配置 logic pose port；
- `None` 不执行 gameplay motion，但仍可通过既有表现输入更新 visual root；
- executor 和 pose port 可以由同一个 Unity adapter component 实现，但 pipeline 只依赖两个窄合同。

### 2. Motion Executor 输入输出不包含 Unity 类型

合同使用项目自己的 pose、intent、execution context 与 result 数据，不暴露 `CharacterController`、`Transform`、`CollisionFlags` 或 Unity scene object。Unity adapter 在边界内完成 Unity 类型转换。

这让当前 client 和 Unity Dedicated Server 可以复用 Unity executor，也允许后续纯 C# KCC 实现同一业务语义。合同不承诺 bitwise determinism。

### 3. 现有 CharacterController 行为迁入正式 Unity Executor

本 change 不借机重写移动手感、斜坡、台阶或 grounded 算法。现有 `CharacterController.Move`、旋转应用和 grounded 读取按原有顺序迁入唯一 Unity executor，`CharacterMotionStage` 只消费结果。

迁移完成后删除 Stage、Pipeline 与 Host 的 concrete `CharacterController` 依赖。项目中允许 `UnityCharacterControllerMotionExecutor` 持有该组件，但不允许第二条 direct Move 路径。

### 4. Logic Pose Port 是逻辑位姿唯一读写入口

MotionStage 的当前 pose、ExternalPose 写入、完整 correction 重定位和 execution result 对账都通过 logic pose port。Presentation 仍只写 visual root，不通过该端口回写逻辑位姿。

端口缺失或返回与 execution result 不一致的状态属于实现/配置错误，系统不从 Host transform、Animancer transform 或 scene search 猜测替代对象。

### 5. Correction 保留在 CharacterMotionStage，不进入 Executor 策略

network correction 的 partial/full 选择、ack identity 和 application extent 属于现有 Character motion 语义。Stage 先生成本 tick correction plan：

- 可参与碰撞的 correction delta 合入最终 execution intent，再由 executor 执行；
- 需要显式重定位的正式结果通过 logic pose port 应用；
- executor 只报告实际执行，不认识 server tick、input sequence、ack 或 Network Model。

这样不会把当前 `ServerAuthoritativeHybrid` 的纠偏策略塞进 Unity/KCC 实现。

### 6. Root motion、warp 和动作曲线不选择后端

Timeline MotionCurve、输入移动、GameplayResult 与 MotionWarp 继续汇入 `MotionContribution -> MotionIntent`。Graph、Timeline、ActionProfile 和 `CharacterPipelineDefinition` 不保存 executor type、server backend、physics model 或 Network Model id。

后端选择只发生在运行时装配根：Unity Character Host 或未来 server simulation composition root。

### 7. 显式装配，不做自动适配

`CharacterPipelineHost` 显式引用 logic pose adapter 和可选 motion executor adapter，并按 `CharacterMotionAuthority` 校验合法组合。不会使用 `GetComponent<CharacterController>`、Host transform、组件名、prefab 搜索或默认 executor 作为 fallback。

Sandbox/Corin 资产先迁入正式 adapter，再删除旧字段。由于当前仓库只找到 Sandbox 中一个 `CharacterPipelineHost` 装配点，迁移目标必须精确落在该 scene 引用，不建立一次性 runtime migrator。

### 8. 三种实验方案分成两个层级

1. **Unity 服务端进程**：继续使用 `ServerAuthoritativeHybrid`，服务端运行 Character gameplay 语义和 Unity executor。
2. **纯 C# 服务端**：继续使用 `ServerAuthoritativeHybrid`，服务端运行可共享/重建的 gameplay 语义和纯 C# KCC executor；DotRecast 只可提供 navmesh/query/crowd，不代替 KCC 碰撞求解。
3. **确定性 KCC 帧同步或 rollback**：是第二个完整 Network Model，必须拥有定点数状态、确定性世界、输入历史、重演和副作用提交规则；它不实现当前 float executor 来伪装兼容。

本 change 只建立前两种方案都需要的 client/runtime 边界，并防止第三种方案被错误压成一个 executor enum。

### 9. 双客户端 change 必须纠正服务端权威定义

`add-local-two-client-gameplay-network-closure` 当前让 Owner 发送 applied displacement/yaw 和 predicted pose，服务端只做 envelope validation。这可以作为弱校验原型，但不能继续命名为独立服务端运动权威。

该 change 在 apply 前必须选择：

- 使用 Unity authoritative simulation，服务端接收 canonical input/action request 并独立推进；或
- 使用纯 C# KCC authoritative simulation，服务端同样从 canonical input/action state 独立推进。

如果业务决定只做客户端 resolved motion 限幅，则必须明确降级模型口径，不能同时声称服务端权威运动闭环。

## Tradeoffs

### 前置拆分 vs 直接在双客户端 change 中加分支

前置拆分会多一个 change，但能先保证单机主线只有一条运动执行路径，后续网络实现只选择正式 backend。直接在网络 change 中写 Unity/Fantasy/DotRecast 分支改动较快，却会把服务端技术选型渗入 Character Host、Graph 和 packet mapping，之后难以公平比较方案。

### 两个窄端口 vs 一个 CharacterBody 大接口

两个端口让 ExternalPose 不依赖碰撞执行器，也让位姿写入所有权清楚；代价是 Unity adapter 装配多一个显式引用。大接口字段少，但会让每个 backend 被迫实现不属于自己的能力，容易出现空方法和 mode switch。

### 保留 CharacterController adapter vs 立即换 KCC

保留 adapter 能保持当前动作、攻击曲线、转身和移动手感，先验证架构拆分；代价是当前 client 仍受 CharacterController 能力限制。立即换 KCC 会把架构迁移和手感/碰撞算法重写混在一起，无法判断回归来自哪里。

### 共享 float executor contract vs 强行统一确定性 backend

float executor contract 足够支持 Unity 与普通纯 C# 服务端对比，数据和诊断也容易接入；代价是不能保证跨平台 bitwise determinism。强行让确定性 KCC 实现同一 float 合同会在边界上反复量化，失去确定性模型的核心价值，因此不采用。

### 发送 canonical input vs 发送 resolved displacement

canonical input/action request 允许服务端独立求解、做真实碰撞和反作弊；代价是服务端必须拥有相应 gameplay/motion 语义。resolved displacement 接入快、覆盖现有 Timeline 曲线，但服务端只能检查结果范围，无法证明移动来自合法输入。后续权威闭环必须明确选择前者，后者只能作为非权威诊断数据。

## Risks / Migration

- 当前 correction 同时存在 delta move 与显式 pose application，迁移时必须保留唯一顺序和实际 application extent，不能在 executor 与 pose port 各应用一次。
- Unity `CharacterController.Move` 返回的碰撞/grounded 状态与 transform 写入时机必须按当前行为迁移，不能在抽象过程中改手感。
- scene 序列化迁移必须先新增正式 adapter 和引用，再删除旧 Host 字段；不创建兼容字段、自动查找或运行时 migrator。
- `ExternalPose` 角色必须确认完全不调用 LocalSolver executor，否则远端 snapshot 与本地碰撞会争夺逻辑位姿。
- 纯 C# 服务端仍需要后继 change 定义 canonical action/motion intent generation；本 change 不能被宣称已经完成服务器运动模拟。
- 确定性 KCC 需要单独 Network Model spec；如果后续发现必须修改 MotionIntent 的数值域，应新增模型专属转换边界，不污染当前 float client contract。
- 当前 `add-local-two-client-gameplay-network-closure` 的 proposal、design、tasks 和 deltas 与新口径冲突，apply 本 change 时必须同步重写，不能让两份已批准文档继续陈述相反的 authority 语义。
