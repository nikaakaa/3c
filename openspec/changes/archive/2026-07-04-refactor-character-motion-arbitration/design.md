# Design: 角色 Motion 有限仲裁模型

## 目标

这次不是重写一套运动系统，而是把当前已有链路补完整：

```text
Graph / StateMachine / Timeline / Action / Network
  -> MotionContribution 或 MotionModifier 数据
  -> MotionResolver 仲裁出 gameplay MotionIntent
  -> MotionModifier 修正 intent
  -> NetworkCorrection phase 做权威纠偏
  -> CharacterController.Move
  -> MotionResult / Debug / SyncFacts
```

核心原则是：上游只产出事实和意图，最终 Transform 仍然只由 `CharacterMotionStage` 应用。

## 概念

### MotionChannel

第一阶段使用有限 channel，不开放动态注册：

- `Locomotion`：输入移动、基础朝向移动、后续 locomotion graph 输出。
- `Action`：攻击、闪避、技能、Timeline root motion。
- `GameplayResult`：受击、击退、拉拽、硬直强制位移、环境 gameplay 结果。
- `Correction`：网络权威纠偏，只在 correction phase 应用。

业务含义是从“玩家主动控制”逐步走向“动作事务”和“权威结果”。`GameplayResult` 高于 `Action`，因为受击/击退应该能打断或覆盖攻击位移。`Correction` 最后处理，因为它代表服务端真值或同步误差收敛。

### MotionBlendMode

第一阶段只保留有限模式：

- `Additive`：与同层或低层结果相加，例如轻微平台移动、细小补偿。
- `WeightedBlend`：同类来源按 weight 混合，例如多个局部来源共同影响方向。
- `Override`：同层按 priority 选择赢家，并可消费低层 channel，例如闪避、攻击 root motion、击退。

不做任意公式，是因为这个 demo 的重点是动作业务和网络可解释性，不是让作者在编辑器里写一套隐式脚本语言。

### ConsumeLowerChannels

高层 contribution 可以声明是否消费低层结果：

- 闪避 root motion 可以消费 locomotion。
- 普通攻击可根据动作配置决定保留少量 locomotion 或覆盖 locomotion。
- 击退通常消费 action 和 locomotion。
- correction phase 不靠 consume 标记，它按 correction policy 最后处理。

这个字段解决的是动作游戏里非常常见的问题：玩家输入还在，但某些动作窗口期间位移权应该转交给动作本身或受击结果。

## Resolve 顺序

第一阶段固定顺序：

1. 收集并规范化 contribution。
2. 处理 `Locomotion` channel，得到基础移动。
3. 处理 `Action` channel，按 blend/override 规则覆盖或混合基础移动。
4. 处理 `GameplayResult` channel，处理受击、击退和强制 gameplay 位移。
5. 执行 Move 前 modifier，例如 `MotionWarp`。
6. 执行 `Correction` phase，按 smooth/force 策略收敛到权威状态。
7. 调用 `CharacterController.Move` 并写入 `MotionResult`。

`MotionWarp` 放在 correction 前，是因为它属于动作表现和玩法意图的修正；网络 correction 放在它之后，是因为权威纠偏不能再被攻击吸附二次扭曲。

## 与当前代码的映射

### 输入移动

当前 `SetMotionIntentFromInputNode` 直接写 `StrictGameplay.MotionIntent`。重构后它应提交 `MotionContribution`：

- channel：`Locomotion`
- blend mode：`Override` 或 `WeightedBlend`
- priority：默认 0
- source type：`Input`
- source id：输入值 id 或节点 id

`MotionIntent` 只作为 resolver 输出，不再是输入节点的主写入目标。

### Timeline root motion

当前 `TimelinePlaybackScheduler.SubmitRootMotion` 已经提交 `MotionContribution.LocalRootMotion`。重构后该 contribution 需要补上：

- channel：`Action`
- blend mode：默认 `Override`
- priority：来自 track/clip 或 action profile 解析结果
- source type：`RootMotion`
- 可选 action instance id/input sequence/debug source

Timeline 仍只负责采样曲线，不直接应用 Transform。

### MotionWarp

当前 `MotionWarpModifier` 的定位是正确的：它是 Move 前 modifier，不是普通 motion contribution。重构只需要让它在固定顺序中明确处于 gameplay intent 之后、network correction 之前。

### 受击和强制位移

后续 combat/gameplay result 不应直接改 Transform，也不应通过旧 body claim 或 locomotion SO。它们应提交 `GameplayResult` channel contribution：

- 轻推可以 `Additive`。
- 击飞、击退、硬直位移可以 `Override` 并消费低层 channel。
- source id 必须能追踪到 gameplay result event 或 server event。

### 网络 correction

当前 `CharacterMotionStage.ApplyNetworkCorrections` 在 resolver 前硬设 Transform。重构后 correction 进入正式 correction phase：

- smooth correction：按上限、时间常量或剩余误差逐帧修正。
- force correction：明确覆盖最终位置和朝向，并记录 debug。
- correction acknowledgement 仍由 network output 发出，但不能绕过 MotionStage 顺序。

## 类型安全和编辑器关系

这不是新的反射系统。节点字段和模块字段仍然走 BTSMTL 当前 `NodeFieldAccessor` / `PropertyPort` 链路。motion 仲裁只定义 runtime 数据语义：

- port 决定节点之间如何连值。
- contribution 决定运行时 motion 来源如何被 MotionStage 仲裁。
- action profile 决定动作网络策略，不把完整策略复制到每个 node/clip。

所以不需要替换 Taco/BTSMTL 的 port 系统，也不需要引入并行 Workbench 数据结构。

## Debug 口径

为求职展示，debug 不能只显示最终位置。它至少应能解释：

- 本帧有哪些 contribution。
- 每个 contribution 的 channel、blend mode、priority、weight、source type。
- 哪个来源覆盖了低层移动。
- MotionWarp 改了多少 displacement/yaw。
- Correction 是 smooth 还是 force，纠偏量是多少。

这能把“手感好”变成可展示的工程能力，而不是只靠肉眼感觉。
