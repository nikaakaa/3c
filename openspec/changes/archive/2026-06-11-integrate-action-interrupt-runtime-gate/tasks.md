## 1. 现状确认
- [x] 1.1 记录当前 Dodge 请求从 `InputRequestBuffer` 到 `CharacterInputRequestFact` 的调用链。
- [x] 1.2 记录 `ActionInterruptPolicySetSO` 当前资产和 prefab/scene 引用情况。
- [x] 1.3 记录默认状态机资产中 `RequestPriorityAtLeast` 的使用位置。

## 2. FullBody Action 仲裁入口
- [x] 2.1 为 FullBody Action 控制器或等价请求门面增加策略集合输入边界。
- [x] 2.2 将 `DodgeActionRequest` 转换为 `ActionInterruptRequest`，保留 origin step、expire step、priority、source order 和 target state。
- [x] 2.3 从当前统一状态机快照或 Action 事实构建 `ActionInterruptContext`。
- [x] 2.4 调用 `ActionInterruptArbiter.Arbitrate` 得到 accepted/rejected decision。
- [x] 2.5 rejected decision 时不生成 `CharacterInputRequestFact`，不消费输入缓冲请求。
- [x] 2.6 accepted decision 时生成 `CharacterInputRequestFact` 并允许后续状态机消费请求。

## 3. 删除默认分裂路径
- [x] 3.1 从默认 `Dodge` 进入 transition 中移除 `RequestPriorityAtLeast` 条件。
- [x] 3.2 更新默认状态机资产，使 Dodge 入口只消费已被仲裁接受的 `HasInputRequest(Dodge)`。
- [x] 3.3 删除或降级 `RequestPriorityAtLeast` 作为默认 FullBody Action 准入手段。
- [x] 3.4 增加静态测试，确认默认状态机配置和默认定义不再使用 `RequestPriorityAtLeast` 作为 Dodge 入口条件。

## 4. 配置和校验
- [x] 4.1 创建或绑定默认 Dodge interrupt policy，覆盖 `Action.None` 或当前 Locomotion/空 Action 到 `Action.Dodge`。
- [x] 4.2 策略集合缺失或无匹配策略时输出可诊断 rejected 日志。
- [x] 4.3 配置校验能发现缺失 Dodge interrupt policy 的角色装配。
- [x] 4.4 确认策略集合不被 Locomotion controller、movement pipeline 或 animation presenter 读取。

## 5. 自动测试
- [x] 5.1 测试 accepted 仲裁后进入 `FullBody/Action/Dodge` 并消费输入请求。
- [x] 5.2 测试 priority 低于 policy min priority 时不进入 Dodge 且请求保留到过期。
- [x] 5.3 测试 resistance 阻挡时不进入 Dodge 且请求保留到过期。
- [x] 5.4 测试 force policy 可在满足最小优先级和时间规则时绕过 resistance。
- [x] 5.5 测试 timing window 未满足时不进入 Dodge。
- [x] 5.6 测试多个候选请求时仲裁器最高优先级和稳定顺序仍生效。
- [x] 5.7 测试状态机 runner、transition evaluator 和 animation presenter 不引用 `ActionInterruptArbiter`。
- [x] 5.8 测试 Action interrupt runtime gate 不引用 Animancer、Animator、CharacterController、Cinemachine、Input System 或 BBB 运行时。

## 6. 手动验证
- [x] 6.1 已给出 Unity Editor 手动验证步骤：打开可琳角色，确认 FullBody Action 控制器能定位 interrupt policy set。
- [x] 6.2 已给出 Play Mode 手动验证步骤：方向输入 + Shift，确认仲裁 accepted 后进入 Directional Dodge。
- [x] 6.3 已给出 Play Mode 手动验证步骤：无方向 + Shift，确认仲裁 accepted 后进入 Backstep Dodge。
- [x] 6.4 已给出 Play Mode 手动验证步骤：临时降低 Dodge 请求 priority 或提高当前 resistance，确认 Shift 不触发 Dodge 且日志显示 rejected 原因。
- [x] 6.5 已给出 Play Mode 手动验证步骤：确认 WASD 的 Idle、MoveStart、MoveLoop、MoveStop 不依赖策略集合且不回退。

## 7. 验证命令
- [x] 7.1 运行 `openspec validate integrate-action-interrupt-runtime-gate --strict --no-interactive`。
- [x] 7.2 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 7.3 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 7.4 运行定向 Unity EditMode 测试：`ActionInterruptArbiterTests`、`ActionInterruptPolicyDataTests`、`UnifiedCharacterStateMachineTests`。
