## Context

角色逻辑以固定 tick 推进，表现层以渲染帧重采样 Timeline 动画。StateMachine transition 在逻辑上必须立即停止 source State，旧 State、Timeline、Action 和 motion fact 不能为了动画淡出继续运行。当前实现因此把 source contribution 暂存到 Registry，再由 `CharacterPresentationStage` 维护 blend session。

这条链路解决了“旧逻辑继续 tick”的问题，但没有解决“表现 Transition 本身也是一个需要身份、状态、重入和终止原因的运行时对象”。Registry 的 `Active/CompletedHeld/Outgoing/Retired` 同时描述 producer 生命周期和 Transition 生命周期，导致以下边界混合：

- target 首次执行和 target 首个 animation sample 被混为一件事；
- source owner release 和 source visual retirement 被混为一件事；
- 同 runtime 第二次切换会覆盖第一次 session，而不是从当前视觉输出接管；
- graph Exit、父级 abort 和 pipeline dispose 使用同一种 release 命令，表现策略不明确；
- Animancer adapter 只收到最终 clip plan，没有正式位置承载 pose-space inertialization。

Animancer 当前 package 已提供 `InsertOutputJob<T>` / `InsertOutputPlayable`，因此可以在正式 Animancer graph 内插入 Unity `AnimationScriptPlayable`，无需修改 package、直接控制 Animator 或建立第二条播放路径。

## Goals / Non-Goals

### Goals

- 让逻辑 transition 和动画 transition 成为两个明确协作但互不冒充的生命周期。
- 每次动画 transition 都有稳定 identity、显式策略、明确 target readiness、完成和 supersede 语义。
- 在攻击、闪避、移动等连续抢占场景中，从当前最终视觉输出连续接管。
- source 逻辑和 source playback 已停止后，仍能通过冻结 contribution 或 pose-space offset 完成表现收尾。
- 保持 Timeline window/cue/motion、黑板和同步事实只在逻辑 tick 中产生。
- 保持所有动画来源继续经过 Registry、LayerRuntime、PresentationStage 和 Animancer adapter。

### Non-Goals

- 不照搬 UE 源码或其资产格式。
- 不实现 Montage、State Alias、Sync Group、Motion Matching 或完整 UE AnimGraph。
- 不实现自定义 blend graph 或 author-facing per-bone inertialization profile。
- 不让 animation output job 应用 root motion、逻辑位移、motion warp 或网络 correction。
- 不为缺配置、缺 rig、缺 target contribution 提供自动 crossfade、Idle 或旧 pose fallback。

## Decisions

### 1. Transition edge 保存 Definition，runtime 保存 Instance

`AnimationTransitionDefinition` 内联属于 `StateMachineGraph` edge，包含：

- `Strategy`: `Immediate`、`ContributionCrossFade`、`Inertialization`；
- `Duration`；
- `Curve`；
- edge authoring identity。

每次命中 edge 创建新的 `AnimationTransitionInstanceId`。Definition 是创作配置，Instance 是运行状态，二者不能共用序列化对象。

业务取舍：配置放在边上符合“从哪个状态到哪个状态如何过渡”的创作心智，也允许同一 target 从不同 source 使用不同策略；代价是边数量多时需要批量迁移和摘要工具，而不是在 State 或全局 SO 上只配一次。

### 2. StateMachine 只提交 request，不拥有表现时钟

StateMachine runtime 在逻辑切换时提交：

- transition instance identity；
- runtime activation scope；
- source owner；
- target owner 或 Empty；
- transition definition；
- logical stop/release cause。

它随后立即完成 source State 的 OnExit、Action/Timeline/gameplay owner 关闭并激活 target State。它不等待 blend，也不推进 blend elapsed。

业务取舍：移动输入、攻击窗口和网络事实不会被动画时长拖住；代价是调试时必须同时观察“当前逻辑 State”和“当前动画 Transition”，二者不再被假装成同一状态。

### 3. 独立 CharacterAnimationTransitionRuntime

每个 StateMachine runtime activation scope 最多拥有一个 active animation transition instance。正式状态为：

1. `Requested`: 已收到逻辑切换 request。
2. `WaitingTarget`: target owner 尚未获得首次正式执行机会。
3. `Capturing`: 在表现帧批次中冻结 source contribution 或捕获最终 pose/velocity。
4. `Running`: 根据 presentation delta 推进策略。
5. `Completed`: 已到达 duration 或 Immediate 原子完成。
6. `Retired`: 已释放所有 transition-owned snapshot/native data。

`Superseded` 是从未完成状态进入 Retired 前必须记录的终止结果和原因，而不是可继续推进的第二条活跃分支。

业务取舍：一个 runtime 只有一个 active transition，能阻止高频输入形成无限 transition 栈和不可控延迟；代价是 supersede 必须重新捕获当前视觉结果，不能简单保留历史 session 逐层混合。

### 4. TargetReady 表示执行机会，不表示存在动画

target State 的 OnEnter 或 Root producer 首次正式 tick 后提交 `TargetReady`。同一表现帧批次内，PresentationStage 先合并 target sample，再让 Transition runtime 捕获和启动。

如果 target ready 后没有合法 animation contribution，最终 target 就是 Empty。系统必须按已配置策略向 Empty 过渡或立即清空，不能保留 source、播放 Idle 或让 adapter 自选 clip。

业务取舍：消除 transition tick 与 target 首帧分属不同 logic tick 造成的空帧，同时保留“状态本来就没有动画”的真实配置错误；代价是 producer 生命周期必须稳定提交 ready，不能再靠 contribution 是否出现来猜测状态是否激活。

### 5. 三种正式策略

#### Immediate

在同一 presentation batch 原子接受 target snapshot 并释放 source。`Duration` 必须为 0。

适用于硬切、ForceStop、deactivate、dispose 和明确要求无过渡的边。

#### ContributionCrossFade

在 Capturing 时冻结 source owner 的最后合法、已聚合 contribution snapshot。Running 时按 curve 计算 source/target 权重，再把二者放入同一个 LayerRuntime 批次；source State、Timeline 和 Action 不再 tick。

适用于两个来源都能由 clip/layer contribution 清晰表达的普通移动过渡。它保留可解释的 layer 权重，但 source snapshot 不是最终骨骼姿态，复杂多层覆盖下的重入质量有限。

#### Inertialization

在 Capturing 时读取当前 Animancer 最终输出 local pose 以及前一表现帧 pose，计算每个绑定骨骼的位置/旋转速度。target contribution 正常生成新 pose，output job 在 Animancer 层合成之后对新 pose 施加从 source visual pose 到 target pose 的衰减偏移。

适用于攻击、闪避、急停、转身等高频打断。它不保留 source player，不需要继续采样 source Timeline，但需要维护骨骼流绑定、pose history 和 native buffer。

三种策略共享同一 lifecycle 接口，不允许 adapter 根据 duration、clip 类型或缺失配置自行选择策略。

### 6. Inertialization 输出位置和数据边界

output job 通过 Animancer 正式 graph 插入，执行顺序为：

`Animation contributions -> LayerRuntime plans -> Animancer layers -> Inertialization output job -> 后续 IK/程序化姿态 -> Animator output`

首版自动绑定 Animator visual hierarchy 中可写入 animation stream 的骨骼，排除 Animator/visual root 的 root motion 通道。`ProcessRootMotion` 只透传上游结果，不计算惯性偏移。缺少有效 Animator、stream handle 或 native storage 时必须报告配置错误，不能自动改用 crossfade。

PresentationStage 即使以 `Evaluate(0)` 手动推进 Animancer，也必须向 job 显式写入真实 presentation delta。Pose history 是运行时表现状态，不是 debug snapshot，也不能被 Transition condition、黑板或网络同步读取。

业务取舍：在最终层合成后捕获，能覆盖 locomotion、action override 和 additive 的真实可见结果；代价是 inertialization 位于 IK 前，IK 仍可能改变最终接触姿态。把它放到 IK 后虽然更接近屏幕最终像素，但会把脚底/武器约束的修正速度也惯性化，业务上更容易拖尾，因此首版不采用。

### 7. 重入和 supersede

新 request 到达同一 runtime activation scope 时：

- 旧 instance 记录 `Superseded` 和替代者 identity；
- `ContributionCrossFade` 从当前 source/target 加权结果冻结新的 source snapshot；
- `Inertialization` 从当前已应用惯性修正的最终 pose 和 velocity 重新捕获；
- `Immediate` 直接以当前 target snapshot 原子替换；
- 旧 instance 的 snapshot/native data 在新 instance 完成 capture 后释放。

不同 StateMachine runtime activation scope 仍可并行，例如 Locomotion 与 Action 各自拥有 active transition；它们先在各自 scope 形成结果，再进入统一 layer priority-fill 仲裁。

业务取舍：重捕获当前视觉结果比保存完整 transition stack 更稳定、内存上界明确，也符合玩家只看到“此刻姿态”的业务事实；代价是不能回放被 supersede transition 的历史权重链。

### 8. 明确 stop 与 release 映射

- internal edge：source -> target，使用命中 edge 的 definition。
- edge to Exit：source -> Empty，使用 Exit edge 的 definition。
- parent graceful stop / replacement：source -> Empty，使用父级 replacement/stop context 明确传入的 definition。
- ForceStop、deactivate、dispose：source -> Empty，必须使用 `Immediate`。
- standalone owner complete：只改变 contribution membership；除非存在正式 transition/release request，否则不能自己创建 blend。

`ReleaseOwner` 这种未说明目标、策略和原因的命令必须删除。若父级 graceful stop 没有可追溯 definition，validator 或 runtime 必须明确报错并停止该配置闭环，不能隐式选择时长。

业务取舍：每次表现退出都可解释、可调、可复现；代价是父树边和 Exit 边需要承担完整配置，迁移时会暴露以前被默认行为掩盖的缺口。

### 9. Registry 与 LayerRuntime 边界

Registry 继续拥有：

- playback instance；
- contribution instance；
- runtime owner membership；
- `Active`、`CompletedHeld`、`Retired`；
- sample、complete、release 的幂等处理。

Registry 不再拥有：

- pending/active owner transition；
- transition elapsed、curve progress；
- `Outgoing` 作为 active transition session；
- target ready 门控；
- supersede 规则。

LayerRuntime 对同一 Override layer 按 priority 从高到低消耗 `0..1` 剩余权重；高优先级未占满时，低优先级正式贡献填充剩余权重。同优先级超出剩余权重时组内归一化。这个规则同时处理普通 contribution 和 Transition runtime 产出的正式加权 snapshot。

### 10. Authoring 与迁移

Inspector 使用枚举控件选择策略，并按策略显示合法字段：

- `Immediate`: duration 固定为 0，不显示 curve 编辑。
- `ContributionCrossFade`: duration 必须大于 0，显示 curve。
- `Inertialization`: duration 必须大于 0，显示 curve与 rig binding 摘要。

迁移是一次性破坏性重写：

- 旧 duration = 0 -> 显式 `Immediate`；
- 旧 duration > 0 -> 显式 `ContributionCrossFade`；
- Corin 指定攻击、闪避、急停/返回移动边再显式改为 `Inertialization`；
- 删除旧字段和反序列化兼容代码；
- 缺失或非法配置让 validator 报错。

首版不新增单独 profile SO，也不根据 clip 名称或目录猜策略。

## Rejected Alternatives

### 继续扩展 Registry handoff

会让 contribution membership、target readiness、blend strategy、pose capture 和 supersede 全部进入同一状态机，Registry 无法保持来源无关且难以单独验证 Transition。拒绝。

### 让旧 State 或旧 Timeline 在淡出期继续 tick

能获得连续 source clip time，但会重新产生 window、cue、motion、blackboard 或同步事实，并恢复第二套逻辑时钟。拒绝。

### 只冻结上一帧最终播放计划

实现简单，适合 contribution crossfade，但不能表示经过多层混合后的真实骨骼姿态和速度；高频重入仍可能跳变。保留为一种策略，不作为全部 Transition 的唯一实现。

### 只实现 Inertialization

所有边都强制 pose-space 处理会增加运行成本，且普通 locomotion clip 混合的 layer 权重更易解释。拒绝；三种策略并列存在并显式创作。

### 建立独立 Animator/PlayableGraph

会绕过现有 Registry、LayerRuntime、PresentationStage 和 Animancer adapter，形成分裂播放路径。拒绝。

## Risks / Trade-offs

- Quaternion 速度与衰减计算不稳定会造成骨骼翻转；实现必须使用最短弧和数值边界。
- 自动绑定全部 stream 骨骼会增加 native memory 与每帧 job 成本；首版以单角色完整绑定换取配置简单和动作完整性，后续性能证据充分时再设计 profile。
- target 为 Empty 时 inertialization 的目标参考姿态必须来自正式空 layer/Animator 基准，不能偷偷使用 Idle；资产配置错误会更显眼，但不会被掩盖。
- `Evaluate(0)` 与真实 presentation delta 分离，若 host 未提交 delta 必须报错；不能默认使用 `Time.deltaTime`，否则离线预览和 runtime 会产生两套时钟。
- active changes 与本 change 修改相同资产和 runtime，必须串行 apply；并行实施会让迁移结果不可验证。

## Migration Plan

1. 在两个依赖 change 完成后重新读取 current specs、runtime 和 Corin 资产，确认没有新增分裂路径。
2. 引入 Definition、Request、Instance、lifecycle 与 strategy 接口，但尚不接旧 Registry session。
3. 将 PresentationStage 批次改为 Transition runtime 权威，并接入 Immediate。
4. 迁移 contribution crossfade 到正式 strategy，删除 Stage session。
5. 插入 inertialization output job，接入 pose history、re-entry 和资源释放。
6. 迁移 internal、Exit、parent graceful、ForceStop/deactivate/dispose request。
7. 原子迁移全部 edge 资产和 Corin 指定策略。
8. 删除 Registry 旧 handoff/Outgoing/release 语义和旧序列化字段。
9. 更新 debug、validator、snapshot/export 与 current spec 对应实现。

## Open Questions Resolved By This Proposal

- `ExposedProperty` 是否驱动动画 transition？不驱动策略；它可以通过纯 ConditionRuleGraph 决定边是否通过，策略仍由 edge definition 决定。
- 打断是否只发生在 Timeline 节点？不是。逻辑抢占发生在 Tree/StateMachine 生命周期；Timeline 只是被 source owner 关闭的 producer，动画 Transition 独立收尾。
- 是否照搬 UE？采用 UE 式“明确 transition 生命周期 + 可插拔策略 + inertialization”的业务模型，但使用现有 BTSMTL、Registry、Animancer 和 Unity Animation Job 实现。
- 旧 source 是否继续播放？不继续。CrossFade 冻结 contribution snapshot，Inertialization 捕获最终 pose/velocity。
- 重入时是否叠加多个 transition？不叠加。新 instance supersede 旧 instance，并从当前最终视觉状态重新捕获。

