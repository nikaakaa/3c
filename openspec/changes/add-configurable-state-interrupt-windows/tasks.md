# 可配置状态打断窗口任务

## 1. 现状确认
- [ ] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [ ] 1.2 读取 `formalize-turnback-locomotion-state`，确认 TurnBack 现有状态输出和 baked motion 接入点。
- [ ] 1.3 读取 `refactor-fullbody-frame-pipeline`，确认状态 facts、动画 facts、运动输出的执行顺序。
- [ ] 1.4 读取 `ActionInterruptArbiter`、`ActionInterruptPolicy` 和相关测试，确认可复用规则。
- [ ] 1.5 读取 `BasicMovementMotionFacts.TurnBackMotionPolicy`，列出现有局部字段迁移目标。
- [ ] 1.6 读取 `turnback613.asset` 和 Corin Generic TurnBack 动画配置，确认当前正式资源路径。
- [ ] 1.7 确认第一版只改 Generic/Sandbox 验证链路，不修改 Humanoid。

## 2. 状态 Timeline Policy 数据模型
- [ ] 2.1 新增纯数据状态 timeline policy 模型。
- [ ] 2.2 policy 使用稳定 state id，不引用 MonoBehaviour、Animator、Animancer、AnimationClip、TransitionAsset 或 Transform。
- [ ] 2.3 policy 支持 normalized window。
- [ ] 2.4 policy 支持 seconds window。
- [ ] 2.5 policy 支持 motion window。
- [ ] 2.6 policy 支持 input lock window。
- [ ] 2.7 policy 支持 interrupt/cancel window。
- [ ] 2.8 policy 支持 exit window。
- [ ] 2.9 policy 支持 priority。
- [ ] 2.10 policy 支持 resistance。
- [ ] 2.11 policy 支持 min priority。
- [ ] 2.12 policy 支持 force 标记。
- [ ] 2.13 policy 支持 allowed request kind 或等价请求过滤。
- [ ] 2.14 policy 支持 note/label，用于诊断和未来编辑器展示。
- [ ] 2.15 增加模型默认构造测试。
- [ ] 2.16 增加模型无 Unity 对象静态边界测试。
- [ ] 2.17 policy 明确 window kind 或等价语义，区分 motion、input lock、natural exit、interrupt 和 cancel。
- [ ] 2.18 policy 不保存 fade、blend duration、clip、clip fallback、speed、start time、TransitionAsset 或 TransitionLibrary key。
- [ ] 2.19 增加 natural exit 不授权 Dodge/Attack 的模型测试。
- [ ] 2.20 增加修改视觉 fade 不改变 window facts 的模型测试。

## 3. Timeline Policy 配置和校验
- [ ] 3.1 新增正式 ScriptableObject 或等价配置入口保存 timeline policy。
- [ ] 3.2 配置入口可以挂到角色状态机配置或角色动作/移动配置的正式装配点。
- [ ] 3.3 配置入口不得作为游离 fallback 被运行时自动加载。
- [ ] 3.4 校验空 state id。
- [ ] 3.5 校验空 window id。
- [ ] 3.6 校验非法 normalized window。
- [ ] 3.7 校验非法 seconds window。
- [ ] 3.8 校验负 priority。
- [ ] 3.9 校验负 resistance。
- [ ] 3.10 校验重复窗口并报告 warning。
- [ ] 3.11 校验 TurnBack 必须有 motion window。
- [ ] 3.12 校验 TurnBack 必须有 exit window。
- [ ] 3.13 增加配置编译测试。
- [ ] 3.14 增加配置校验测试。
- [ ] 3.15 校验 interrupt/cancel window 必须携带 allowed request kind 或等价请求过滤。
- [ ] 3.16 校验 timeline policy 编译结果不暴露表现层字段。

## 4. Timeline Window Facts 采样
- [ ] 4.1 新增纯数据 window facts。
- [ ] 4.2 facts 表达当前 state id。
- [ ] 4.3 facts 表达当前 normalized time。
- [ ] 4.4 facts 表达当前 elapsed seconds。
- [ ] 4.5 facts 表达 motion window 是否 active。
- [ ] 4.6 facts 表达 input lock window 是否 active。
- [ ] 4.7 facts 表达 interrupt/cancel window 是否 active。
- [ ] 4.8 facts 表达 exit window 是否 active。
- [ ] 4.9 facts 表达当前窗口 priority/resistance 修正或等价值。
- [ ] 4.10 sampler 支持 normalized time domain。
- [ ] 4.11 sampler 支持 seconds time domain。
- [ ] 4.12 sampler 不读取 Animancer、Animator、AnimationClip、TransitionAsset、Unity 时间单例或 Transform。
- [ ] 4.13 播放进度无效时 sampler 不猜测 clip 长度。
- [ ] 4.14 增加 sampler 窗口边界测试。
- [ ] 4.15 增加 sampler time domain 测试。
- [ ] 4.16 增加 sampler 静态边界测试。

## 5. 状态请求仲裁接入
- [ ] 5.1 复用或扩展现有 `ActionInterruptArbiter` 纯数据模型，不创建 TurnBack 专用仲裁器。
- [ ] 5.2 请求模型支持 TurnBack、Dodge、Attack、HitReact 或等价 request kind。
- [ ] 5.3 context 能表达当前统一状态路径。
- [ ] 5.4 context 能表达当前 state elapsed seconds。
- [ ] 5.5 context 能表达当前 state normalized time 或对应 window facts。
- [ ] 5.6 context 能表达当前 state resistance。
- [ ] 5.7 仲裁器先检查 request 过期。
- [ ] 5.8 仲裁器检查 policy 匹配。
- [ ] 5.9 仲裁器检查 min priority。
- [ ] 5.10 仲裁器检查 resistance。
- [ ] 5.11 仲裁器检查 force。
- [ ] 5.12 仲裁器检查 window facts。
- [ ] 5.13 多请求时仍按 priority 和稳定顺序选择。
- [ ] 5.14 rejected 请求不生成状态机请求事实。
- [ ] 5.15 增加 TurnBack 请求窗口仲裁测试。
- [ ] 5.16 增加 Dodge 旧行为兼容测试。
- [ ] 5.17 增加多请求选择测试。

## 6. 统一状态机接入
- [ ] 6.1 状态机配置能引用状态 timeline policy。
- [ ] 6.2 transition evaluator 只读取 accepted request fact 和 timeline facts。
- [ ] 6.3 transition evaluator 不读取 policy SO。
- [ ] 6.4 transition evaluator 不直接计算 priority/resistance。
- [ ] 6.5 状态机 runner 不引用 Animancer 或 Animator。
- [ ] 6.6 状态机 runner 不引用 motion executor。
- [ ] 6.7 缺失必需 timeline policy 时输出配置诊断并停止相关状态更新。
- [ ] 6.8 增加状态机接入测试。
- [ ] 6.9 增加状态机边界静态测试。
- [ ] 6.10 transition 条件满足后立即切换逻辑状态，不保存视觉 blend 持续时间。
- [ ] 6.11 transition evaluator 不读取 fadeDuration、TransitionAsset、clip、speed 或 start time。
- [ ] 6.12 增加修改视觉 fade 不改变逻辑 transition 结果的测试。
- [ ] 6.13 增加需要玩法恢复段时必须由显式状态或 timeline window 表达的配置测试。

## 7. TurnBack 迁移
- [ ] 7.1 将 TurnBack 的 start/lock/turn complete/exit 字段迁移或映射到 state timeline policy。
- [ ] 7.2 TurnBack 只允许从 `FullBody/Locomotion/MoveLoop` 且 gait 为 Run 时触发。
- [ ] 7.3 TurnBack 进入请求经过状态请求仲裁入口。
- [ ] 7.4 TurnBack motion window 内抑制普通输入旋转。
- [ ] 7.5 TurnBack motion window 内抑制普通输入平面位移。
- [ ] 7.6 TurnBack yaw 来自 baked motion profile。
- [ ] 7.7 TurnBack translation 来自 baked motion profile。
- [ ] 7.8 TurnBack visual 使用 inplace 动画 alias。
- [ ] 7.9 TurnBack 视觉动画不要求裁剪成 turn-only clip。
- [ ] 7.10 TurnBack 到 exit window 后可按输入回 MoveLoop 或 Idle。
- [ ] 7.11 TurnBack 退出后不继续消费 TurnBack baked motion tail。
- [ ] 7.12 TurnBack 退出后普通输入位移和旋转立即恢复。
- [ ] 7.13 增加 TurnBack RunLoop 才能触发测试。
- [ ] 7.14 增加 TurnBack window lock 测试。
- [ ] 7.15 增加 TurnBack baked yaw/translation 测试。
- [ ] 7.16 增加 TurnBack exit window 测试。

## 8. 资产和编辑器边界
- [ ] 8.1 为 Corin Generic TurnBack 绑定正式 timeline policy 资产。
- [ ] 8.2 为 Corin Generic TurnBack 绑定 `Assets/Configs/3C/Animation/Locomotion/Corin/Bake/turnback613.asset` 或后续正式 profile。
- [ ] 8.3 确认 Animancer TurnBack transition 指向 inplace 动画。
- [ ] 8.4 配置校验能报告 TransitionAsset 不是 inplace 资源的风险。
- [ ] 8.5 配置校验能报告 baked profile 缺失。
- [ ] 8.6 第一版只提供 Inspector 字段和校验入口。
- [ ] 8.7 预留 timeline editor 接口，但不实现完整编辑器。
- [ ] 8.8 增加资产校验测试。
- [ ] 8.9 审计默认状态机 `fadeDuration` 字段，确认它不参与逻辑 transition、exit window 或 cancel window。
- [ ] 8.10 将 clip、fade、speed、start time 和 TransitionAsset 权威收口到 Animancer/动画表现配置或迁移期只读绑定。

## 9. 诊断日志
- [ ] 9.1 增加状态 timeline policy 编译日志。
- [ ] 9.2 增加 window facts 采样日志。
- [ ] 9.3 增加状态请求仲裁 accepted/rejected 日志。
- [ ] 9.4 TurnBack 日志输出当前 state id。
- [ ] 9.5 TurnBack 日志输出 normalized time。
- [ ] 9.6 TurnBack 日志输出 motion/input lock/interrupt/exit window active 状态。
- [ ] 9.7 TurnBack 日志输出 baked profile id。
- [ ] 9.8 TurnBack 日志输出本帧 baked yaw/translation delta。
- [ ] 9.9 日志继续受现有诊断系统开关控制。
- [ ] 9.10 不删除现有日志。

## 10. 自动验证
- [ ] 10.1 运行 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`。
- [ ] 10.2 运行 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`。
- [ ] 10.3 使用 Unity Test Runner 定向运行 State Timeline Policy EditMode 测试。
- [ ] 10.4 使用 Unity Test Runner 定向运行 ActionInterrupt 相关 EditMode 测试。
- [ ] 10.5 使用 Unity Test Runner 定向运行 UnifiedCharacterStateMachine 相关 EditMode 测试。
- [ ] 10.6 使用 Unity Test Runner 定向运行 BasicLocomotionAnimation/TurnBack 相关 EditMode 测试。
- [ ] 10.7 读取 Unity Console，确认相关 error 为 0。
- [ ] 10.8 运行 `openspec validate add-configurable-state-interrupt-windows --strict --no-interactive`。
- [ ] 10.9 不运行 Unity batchmode。

## 11. Sandbox 手动验证
- [ ] 11.1 打开 Sandbox 场景并使用 Generic 可琳。
- [ ] 11.2 启用 Locomotion、Animation、Action 或等价诊断日志。
- [ ] 11.3 按 W 进入 RunLoop 后切 S，确认进入 TurnBack。
- [ ] 11.4 Walk、MoveStart、MoveStop、Idle 前后切换不触发 TurnBack。
- [ ] 11.5 TurnBack motion window 内角色按 baked profile 完成转身和位移。
- [ ] 11.6 TurnBack motion window 内普通输入位移和旋转不叠加。
- [ ] 11.7 到 exit window 后持续按输入可快速回到普通 MoveLoop。
- [ ] 11.8 松开输入时到 exit window 后回 Idle。
- [ ] 11.9 A/D 横向切换不误触发前后 TurnBack。
- [ ] 11.10 搜索 `state-timeline|state-interrupt|turnback|baked-motion|animation-motion-executor` 复制关键日志验证。

## 12. 收尾
- [ ] 12.1 检查没有新增 TurnBack 专用仲裁器。
- [ ] 12.2 检查没有新增运行时 fallback 配置加载。
- [ ] 12.3 检查没有让 Animancer/Animator 直接决定状态切换。
- [ ] 12.4 检查没有让 Animator root motion 直接写角色根。
- [ ] 12.5 更新相关调试文档或 Path 文档。
- [ ] 12.6 全部任务真实完成后再将 checklist 标为 `- [x]`。
- [ ] 12.7 检查没有把 natural exit window、cancel/interrupt window 和 visual fade 混用。
