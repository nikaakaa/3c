# Design: Timeline 循环播放与状态切换动画混合

## 当前问题

当前执行树是生命周期驱动的。`RunnableNode` 非 `Running` 时会重新 `OnStart`，普通 `LoopNode` 会在子节点返回 `Success` 后再次 tick 子节点。对普通行为节点这表示“重复执行”，但对 `TimelineNode` 表示“结束旧播放请求，再提交新播放请求”。

因此 `LoopNode -> TimelineNode` 会带来三个表现问题：

- Timeline request handle 和 source lifecycle 每轮变化，动画层很难把它识别成同一个持续动画源。
- duration 边界可能出现完成帧、重启帧或空贡献帧，循环 clip 看起来抖或卡。
- 表现层插值只看到 clip time 从末尾跳到开头，简单 lerp 会走反方向。

顶层 `Runtime Loop` 不是同一个问题。它负责让 RootTree 主流程每 tick 保持运行；它没有直接重启单个动画 Timeline 的局部播放请求。这个 change 保留顶层主流程 loop，移除状态 body 里的动画 loop decorator 用法。

## Timeline 播放模式

`TimelineNode` 新增正式播放模式：

- `Once`：默认模式，保持现有语义。Timeline request 播放到 duration 后进入 `Succeeded`，`TimelineNode` 返回 `Success`。
- `Loop`：循环模式。Timeline request 播放到 duration 后不进入 `Succeeded`，而是在 scheduler 内回绕时间并继续保持 `Running`。节点被 stop/reset 或状态离开时取消 request。

播放模式属于 `TimelineNode` 的 authoring 数据，并进入播放请求。`TimelineNode` 不自行推进时间、不直接 evaluate Timeline、不创建旧播放器。

`Loop` 模式要求 Timeline duration 大于 0。duration 非法时应作为配置错误失败暴露，不能隐式改成 `Once` 或用默认时长兜底。

## 回绕采样

`TimelinePlaybackScheduler` 的 active record 需要区分连续播放时间和当前 Timeline 相位。实现可以保存 unwrapped time，也可以保存 cycle index + local time；业务合同是相同的：

- request handle 在循环期间保持稳定。
- source id / source name 在循环期间保持稳定。
- 每个 tick 的采样区间必须能表达是否跨过 duration 边界。
- 跨边界时，轨道采样应按 `[previousLocal, duration]` 和 `[0, currentLocal]` 两段执行。

动画轨道输出的贡献必须携带循环上下文，例如 clip duration、loop flag、cycle index 或连续 clip time。表现层用这些信息把末尾到开头的采样解释为向前回绕，而不是反向插值。

非动画事实也必须使用同一条 scheduler 采样路径。Window、cue、motion、foot phase 等轨道如果出现在 loop Timeline 中，就表示作者希望它们每轮重复。采样器必须避免边界丢采样和同一边界重复发样；它不能伪造 Action Context，也不能绕过 strict/presentation/sync 分层。

## 同 Timeline clip 混合与跨 State 混合

BTSMTL Timeline clip 的 `StartFrame`、`EndFrame`、`Duration`、`ClipInFrame`、`SelfEaseIn/Out`、`OtherEaseIn/Out`、`EaseIn/Out` 和相关 curve 字段继续保留。它们表达同一条 Timeline 内部多个 clip 的重叠、入出权重和 clip local time，不负责不同 `StateNode` 之间的切换。

跨 State 混合放在 `StateMachineGraph` Transition edge 上。edge 至少需要一个 animation blend duration，可扩展为 curve/profile。默认 duration 为 0 时表示即时切换；非 0 表示发生切换后表现层保留 outgoing 播放计划并按 edge 元数据混合到 incoming 播放计划。

## 状态切换的双 pose

这里的“双 pose”只存在于表现层，不是两套逻辑状态。

状态机 runtime 切换时仍只 tick 当前 active state。旧状态离开后不继续 tick 它的行为图，也不继续采样旧 Timeline 的 gameplay facts。表现层保留旧状态上一帧已经生成的动画播放计划作为 outgoing pose；新 active state 通过正式 Timeline/动画贡献链路生成 incoming pose。`CharacterPresentationStage` 在 render frame 中按 blend duration 和 curve 合成两组计划，再交给动画层运行时和 Animancer adapter 应用。

这个选择的业务取舍是：动画视觉能平滑过渡，同时 gameplay window、cue、motion、ActionInstance 和网络输出仍只有一个当前逻辑来源，不会因为动画混合而重复结算。

## 为什么不采用其它方案

### 继续用 LoopNode 包 TimelineNode

优点是不用改节点数据；缺点是它表达的是“重复执行子行为”，不是“同一个 Timeline 连续循环”。它会重启 request lifecycle，直接造成当前循环衔接问题。

### 让 Animancer CrossFade 决定转换

优点是短期能看到淡入淡出；缺点是 Animancer 会变成隐藏状态机，transition 进度和权重不再来自 Graph/Timeline 合同。之后调试、网络同步、动画层快照和 Timeline 预览都会出现第二套真相。

### 切换期间继续 tick 旧状态

优点是旧状态能继续产出 pose；缺点是旧 Timeline 会继续产出 motion、window、cue 或 action facts，容易重复位移、重复攻击窗口或重复表现事件。这个方案不符合当前管线“单 active state 事实来源”的业务口径。

### 给 loop Timeline 做超长 duration

优点是看起来不需要回绕；缺点是它只是把问题推迟，无法表达 cycle，不能正确处理边界事实，也不利于作者按动画真实长度调 clip。

## Corin 配置口径

Corin RootTree 顶层继续保留 `Runtime Loop`、输入/运动入口、`Locomotion StateMachine` 和 `Action StateMachine`。

Locomotion 的 `Idle`、`WalkLoop`、`RunLoop` 状态 body 中，`TimelineNode` 配置为 `Loop` 播放模式，不再用普通 `LoopNode` 包住 Timeline。`WalkStart`、`WalkEnd`、`RunStart`、`RunEnd`、`MovingTurn` 仍按一次性 Timeline 播放，并由 `StateRootCompleted` 或其它 condition rule 决定何时离开。

Transition edge 上的 blend 元数据用于配置例如 `WalkStart -> WalkLoop`、`WalkLoop -> RunLoop`、`RunLoop -> RunEnd`、`Idle -> Attack1` 等切换的视觉淡入淡出。具体数值可以先用正式字段默认值表达，后续由作者在 Inspector 中调。
