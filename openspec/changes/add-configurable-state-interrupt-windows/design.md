## Context
当前项目已有三块能力，但还没有统一在一个状态 timeline 模型里：

- `ActionInterruptArbiter` 已经能处理 priority、resistance、force 和 elapsed time window，但它主要面向 Action 请求，并且时间规则来自逻辑 elapsed time。
- `animation-phase-timeline-facts` 已经建立播放进度事实和未来 window fact 的边界，但还没有完整窗口表。
- `formalize-turnback-locomotion-state` 已经把 TurnBack 收敛为 Locomotion 状态，并预留了 start、lock、turn complete、exit 等字段，但这些字段仍是 TurnBack 局部语义。

这次要把三者合并成一个正式模型：状态节点持有 timeline policy，sampler 输出 window facts，仲裁入口和统一状态机只读 facts。

## Goals
- 用同一套数据表达 TurnBack、Dodge、Attack、HitReact 后续需要的动作窗口。
- 把 priority、resistance、force、min priority 和 timing window 从状态机条件和 MonoBehaviour 特判里收口。
- 让 TurnBack 能配置“只在 RunLoop 进入、转身窗口内锁输入、转完后恢复普通移动、视觉尾巴可以继续混合或退出”。
- baked motion profile 继续作为 runtime root motion 权威数据源，动画表现使用 inplace clip。
- 将自然退出窗口、打断/取消窗口和视觉 blend 拆成不同语义，避免把 `fadeDuration` 当成逻辑退出时间。
- 保留未来 timeline 编辑器边界，但第一版优先完成数据、采样、仲裁、校验和测试。

## Non-Goals
- 不实现完整可视化 timeline 编辑器。
- 不新增 hitbox、IK、VFX、SFX、camera 事件轨道。
- 不引入第二套 TurnBack 专用 runtime。
- 不让完整 Animator Root Motion 直接驱动角色根。
- 不修改 Humanoid 资源链路，第一版仍以 Generic/Sandbox 验证为主。
- 不把 clip、fade、speed、start time 或 TransitionAsset 作为 timeline policy 或状态 transition 条件。
- 不用隐式 transition 持续时间表达玩法恢复段；需要持续时间的玩法过程必须是显式状态或 timeline window。

## Decisions
- Decision: 新增 `StateTimelinePolicy` 或等价纯数据模型，作为状态窗口的唯一来源。
  - Reason: TurnBack、Dodge 和 Attack 都需要窗口，不应继续由各自 config 散落定义。
- Decision: 窗口采样输出 `StateTimelineWindowFacts`，状态机和仲裁器都消费 facts。
  - Reason: 播放进度来自动画外观层，但业务判断不能持有 Animancer 对象。
- Decision: 现有 `ActionInterruptArbiter` 可演进为状态请求仲裁核心，但状态机 transition 不直接做 priority/resistance 裁决。
  - Reason: 现有纯数据仲裁规则可复用，避免新建平行仲裁器。
- Decision: 逻辑 transition 条件满足后立即切换当前状态，视觉 crossfade 由动画外观层独立处理。
  - Reason: 工业上通常把玩法状态权威和动画混合权威拆开；否则 fade 改动会意外改变打断窗口和退出时机。
- Decision: window 必须有明确 kind 或等价语义，`natural-exit`、`dodge-cancel`、`attack-cancel`、`motion`、`input-lock` 不得互相代用。
  - Reason: 自然退出是动作自身收尾，取消/打断是外部请求准入；混用会让配置看似简单但运行时不可推理。
- Decision: TurnBack 第一版用正式配置，不做 fallback 配置。
  - Reason: 当前问题已经多次由隐式路径和临时默认值放大，缺配置应诊断失败。

## Runtime Flow
1. 输入和预输入缓冲产出请求事实，例如 TurnBack intent、Dodge request、Attack request。
2. 当前统一状态机快照和状态 timeline policy 形成当前 state context。
3. timeline sampler 根据 state elapsed、animation normalized time 或配置锚点输出 window facts。
4. 状态请求仲裁入口读取 request、current state resistance、policy min priority、window facts，输出 accepted/rejected。
5. 统一状态机只消费 accepted request fact 和普通纯数据条件。
6. transition 条件通过后，统一状态机立即更新当前逻辑状态；transition 本身不表达持续时间。
7. 当前状态输出运动命令和动画请求；TurnBack 的 baked motion 只在 motion window 内贡献位移/yaw。
8. 动画外观层按动画表现配置解析 alias、TransitionAsset、clip、fade、speed 和 start time，视觉 blend 不反向决定逻辑退出。

## Risks / Trade-offs
- Risk: 同时改动 TurnBack、Action 仲裁和状态机配置，范围容易膨胀。
  - Mitigation: 第一版只落地 TurnBack 和现有 Dodge 请求的兼容测试，Attack 只验证模型能表达，不实现连招窗口。
- Risk: “elapsed time window”和“animation normalized window”混用导致时序再次混乱。
  - Mitigation: policy 必须声明 window time domain，sampler 统一转换成 facts；仲裁器不直接读取 Animancer。
- Risk: baked motion profile 与视觉 inplace clip 对不齐。
  - Mitigation: 验证任务要求同时检查 profile id、采样累计 yaw/translation、视觉 alias 和 Sandbox 手感。
- Risk: 现有状态机配置中的 `fadeDuration` 被继续误读为退出时间或打断窗口。
  - Mitigation: 迁移时必须审计该字段来源；自动测试要证明修改视觉 fade 不改变状态 transition、window facts 或 baked motion 采样。

## Migration Plan
1. 先新增纯数据模型、校验和 sampler，不接入运行时。
2. 将现有 TurnBack timing 字段迁移为状态 timeline policy 配置。
3. 将 TurnBack 运动窗口改为读取 window facts 和 baked motion profile。
4. 将 Dodge/Action 现有 interrupt policy 适配到新的状态请求上下文，保持旧行为测试通过。
5. 审计逻辑状态机中的动画表现字段，将 fade、clip、speed、start time、TransitionAsset 权威收口到动画表现配置或迁移期只读绑定。
6. 最后收口配置资产和诊断日志。

## Open Questions
- TurnBack 默认窗口数值是否沿用当前 `turnback613.asset` 的有效转身段，还是重新指定设计值？
- 第一版是否需要提供只读 timeline 预览窗口，还是只做 Inspector 字段和校验？
- `DefaultCharacterStateMachine.asset` 里的 `fadeDuration` 是第一版直接迁移到 Animancer/动画表现配置，还是先保留为迁移期只读表现绑定并明确不参与逻辑？
