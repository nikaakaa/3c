## 0. 前置确认
- [ ] 0.1 用 `Get-Content -Encoding UTF8` 读取本 change 的 `proposal.md`。
- [ ] 0.2 用 `Get-Content -Encoding UTF8` 读取本 change 的 `design.md`。
- [ ] 0.3 用 `Get-Content -Encoding UTF8` 读取本 change 的全部 spec delta。
- [ ] 0.4 运行 `openspec list`，确认活跃变更是否触碰同一状态机文件。
- [ ] 0.5 运行 `openspec list --specs`，确认相关规格仍存在。
- [ ] 0.6 确认本次只实现攻击动作连段，不实现伤害、hitbox 或受击。
- [ ] 0.7 确认如需绕过 Character frame pipeline、统一状态机、输入缓冲或 motion executor，立即停止并回到 OpenSpec。

## 1. 现状盘点
- [ ] 1.1 搜索 `InputRequestKind.Attack` 的现有使用点。
- [ ] 1.2 搜索 `ActionRequestType.Attack` 的现有使用点。
- [ ] 1.3 搜索 `ActionStateIds`，确认当前只落地哪些 Action state。
- [ ] 1.4 搜索 `ActionAnimationKeys`，确认当前只落地哪些动作动画 key。
- [ ] 1.5 搜索 `BuildDodgeRequestFact`、`FullBodyActionRequestSubmissionProviders`、request submission 和 `ActionInterruptArbiter`，确认现有请求提交与仲裁形态。
- [ ] 1.6 搜索状态机默认配置创建逻辑，确认 Dodge 节点和 transition 的接入位置。
- [ ] 1.7 搜索动作动画 Presenter 和 Profile 校验逻辑，确认新增攻击 key 的绑定位置。
- [ ] 1.8 记录与活跃 OpenSpec change 的潜在冲突点。

## 2. 纯数据 ID 和模型
- [ ] 2.1 在 `ActionStateIds` 增加 `Action.Attack01`。
- [ ] 2.2 在 `ActionStateIds` 增加 `Action.Attack02`。
- [ ] 2.3 在 `ActionStateIds` 增加 `Action.Attack03`。
- [ ] 2.4 在 `ActionAnimationKeys` 增加 `Action.Attack.Light.01`。
- [ ] 2.5 在 `ActionAnimationKeys` 增加 `Action.Attack.Light.02`。
- [ ] 2.6 在 `ActionAnimationKeys` 增加 `Action.Attack.Light.03`。
- [ ] 2.7 定义轻攻击 stage 枚举或等价纯数据 stage id。
- [ ] 2.8 定义轻攻击 stage 配置数据：state id、animation key、duration。
- [ ] 2.9 定义轻攻击 stage 配置数据：priority、resistance。
- [ ] 2.10 定义轻攻击 stage 配置数据：combo window start/end。
- [ ] 2.11 定义轻攻击 stage 配置数据：可选位移距离和转向策略。
- [ ] 2.12 确保新增模型不引用 Unity 场景对象、Animancer runtime、InputAction 或 CharacterController。

## 3. 配置资产和校验
- [ ] 3.1 创建轻攻击连段配置 SO 或等价正式配置入口。
- [ ] 3.2 配置入口必须包含三段轻攻击 stage。
- [ ] 3.3 配置入口必须能定位每段动画 key。
- [ ] 3.4 配置入口必须能定位每段 duration。
- [ ] 3.5 配置入口必须能定位每段 priority。
- [ ] 3.6 配置入口必须能定位每段 resistance。
- [ ] 3.7 配置入口必须能定位 Attack01 到 Attack02 的 combo window。
- [ ] 3.8 配置入口必须能定位 Attack02 到 Attack03 的 combo window。
- [ ] 3.9 配置入口必须明确 Attack03 没有下一段。
- [ ] 3.10 校验缺失配置时报 error。
- [ ] 3.11 校验空 animation key 时报 error。
- [ ] 3.12 校验 duration 小于等于 0 时报 error。
- [ ] 3.13 校验 priority 或 resistance 为负时报 error。
- [ ] 3.14 校验 combo window end 早于 start 时报 error。
- [ ] 3.15 校验 combo window 超出 0 到 1 范围时报 error。
- [ ] 3.16 不新增代码级 fallback 手感配置。

## 4. 连段窗口事实
- [ ] 4.1 定义轻攻击连段窗口事实数据。
- [ ] 4.2 定义窗口事实字段：当前 stage。
- [ ] 4.3 定义窗口事实字段：是否允许进入下一段。
- [ ] 4.4 定义窗口事实字段：下一段 state id。
- [ ] 4.5 定义窗口事实字段：采样 normalized time。
- [ ] 4.6 实现纯逻辑 sampler，以 action state elapsed / stage duration 采样 normalized time。
- [ ] 4.7 sampler 在窗口开始前输出不可接段。
- [ ] 4.8 sampler 在窗口内输出可接段。
- [ ] 4.9 sampler 在窗口结束后输出不可接段。
- [ ] 4.10 sampler 在 Attack03 输出不可接段。
- [ ] 4.11 sampler 缺少有效 stage 配置时输出不可接段并给校验暴露错误来源。
- [ ] 4.12 sampler 不读取 Animancer、Animator、AnimationClip、TransitionAsset 或 Unity 时间单例。

## 5. Attack Request Submission 和仲裁
- [ ] 5.1 新增 Attack request submission 构建器或扩展现有 FullBody Action 请求提交构建器。
- [ ] 5.2 从 `InputRequestBuffer` 查询 `InputRequestKind.Attack`。
- [ ] 5.3 从 Locomotion owner 状态构建进入 `Action.Attack01` 的请求。
- [ ] 5.4 从 `Action.Attack01` 状态且窗口允许时构建进入 `Action.Attack02` 的请求。
- [ ] 5.5 从 `Action.Attack02` 状态且窗口允许时构建进入 `Action.Attack03` 的请求。
- [ ] 5.6 从 `Action.Attack03` 状态不构建下一段请求。
- [ ] 5.7 构建的请求使用 `ActionRequestType.Attack`。
- [ ] 5.8 构建的请求使用配置 priority。
- [ ] 5.9 构建的请求保留 origin step 和 expire step。
- [ ] 5.10 请求进入统一状态机事实前先进入统一 request submission 并调用 `ActionInterruptArbiter`。
- [ ] 5.11 accepted 后才生成 `CharacterInputRequestFact`。
- [ ] 5.12 rejected 时不生成状态机事实。
- [ ] 5.13 rejected 时不消费输入缓冲中的 Attack 请求。
- [ ] 5.14 accepted 时消费对应 Attack 请求。
- [ ] 5.15 保留或新增诊断 log，说明 Attack accepted/rejected、目标 state、priority 和拒绝原因。

## 6. 统一状态机接入
- [ ] 6.1 在默认状态树加入 `FullBody/Action/Attack01`。
- [ ] 6.2 在默认状态树加入 `FullBody/Action/Attack02`。
- [ ] 6.3 在默认状态树加入 `FullBody/Action/Attack03`。
- [ ] 6.4 增加 Locomotion 到 Attack01 的 transition。
- [ ] 6.5 增加 Attack01 到 Attack02 的 transition。
- [ ] 6.6 增加 Attack02 到 Attack03 的 transition。
- [ ] 6.7 增加 Attack01 完成后回 Locomotion 的 transition。
- [ ] 6.8 增加 Attack02 完成后回 Locomotion 的 transition。
- [ ] 6.9 增加 Attack03 完成后回 Locomotion 的 transition。
- [ ] 6.10 transition 只读取已仲裁接受的 input fact 和窗口 fact。
- [ ] 6.11 transition 不直接读取输入缓冲。
- [ ] 6.12 transition 不直接读取 `ActionInterruptPolicySetSO`。
- [ ] 6.13 transition 不直接判断动作请求 priority。
- [ ] 6.14 状态快照必须暴露当前 Attack action state 和 state time。
- [ ] 6.15 攻击结束后 `ActionRuntimeStateSnapshot` 或等价 facts 回到 `Action.None`。

## 7. 输出和表现
- [ ] 7.1 Attack01 输出 `Action.Attack.Light.01` 动画命令。
- [ ] 7.2 Attack02 输出 `Action.Attack.Light.02` 动画命令。
- [ ] 7.3 Attack03 输出 `Action.Attack.Light.03` 动画命令。
- [ ] 7.4 攻击 active 时 FullBody Action submission 获胜。
- [ ] 7.5 攻击 active 时 Character output applier 不应用 Locomotion 平面位移。
- [ ] 7.6 攻击 active 时 Character output applier 不应用 Locomotion base layer 动画。
- [ ] 7.7 攻击可选位移通过 `IActionMovementExecutor` 或等价统一运动出口提交。
- [ ] 7.8 攻击可选转向通过现有运动/朝向输出边界提交。
- [ ] 7.9 动画 Presenter 只消费 Character output applier 提交的最终动作动画命令。
- [ ] 7.10 动画 Presenter 不消费输入请求。
- [ ] 7.11 动画 Presenter 不调用 `ActionInterruptArbiter`。
- [ ] 7.12 动画 Presenter 不调用 `CharacterController.Move`。

## 8. 动作动画配置
- [ ] 8.1 扩展动作动画 Profile 或绑定入口，支持 `Action.Attack.Light.01`。
- [ ] 8.2 扩展动作动画 Profile 或绑定入口，支持 `Action.Attack.Light.02`。
- [ ] 8.3 扩展动作动画 Profile 或绑定入口，支持 `Action.Attack.Light.03`。
- [ ] 8.4 校验缺失 Attack01 动画引用时报 error。
- [ ] 8.5 校验缺失 Attack02 动画引用时报 error。
- [ ] 8.6 校验缺失 Attack03 动画引用时报 error。
- [ ] 8.7 确认动作逻辑不直接引用 `AnimationClip`。
- [ ] 8.8 确认动作逻辑不写死具体角色动画资源名。

## 9. 回滚和快照边界
- [ ] 9.1 确认 Attack pressed 在 `PredictionInputFrame` 回灌为 Attack request。
- [ ] 9.2 捕获当前 Attack state 到 FullBody restore state 或统一状态机 restore state。
- [ ] 9.3 恢复 Attack state 后下一 tick state time 正确延续。
- [ ] 9.4 恢复 Attack state 后 combo window fact 正确重采样。
- [ ] 9.5 恢复 Attack state 后已消费请求不重复消费。
- [ ] 9.6 同输入重放 Attack01 结果收敛。
- [ ] 9.7 同输入重放 Attack01 -> Attack02 结果收敛。
- [ ] 9.8 同输入重放 Attack01 -> Attack02 -> Attack03 结果收敛。
- [ ] 9.9 不修改 Fantasy proto。
- [ ] 9.10 不新增真实网络流程。

## 10. EditMode 自动测试
- [ ] 10.1 测试 Attack pressed 可从 Locomotion 进入 Attack01。
- [ ] 10.2 测试 Attack held 不重复生成新段请求。
- [ ] 10.3 测试 Attack01 窗口前按 Attack 会保留请求。
- [ ] 10.4 测试 Attack01 窗口内消费请求进入 Attack02。
- [ ] 10.5 测试 Attack01 窗口后不进入 Attack02。
- [ ] 10.6 测试 Attack02 窗口内消费请求进入 Attack03。
- [ ] 10.7 测试 Attack03 不进入第四段。
- [ ] 10.8 测试 Attack01 无下一段输入时回 Locomotion。
- [ ] 10.9 测试 Attack02 无下一段输入时回 Locomotion。
- [ ] 10.10 测试 Attack03 完成后回 Locomotion。
- [ ] 10.11 测试 rejected Attack 请求不被消费。
- [ ] 10.12 测试 accepted Attack 请求被消费。
- [ ] 10.13 测试攻击期间 Locomotion 不提交平面位移。
- [ ] 10.14 测试攻击期间 Locomotion 不提交 base layer 动画。
- [ ] 10.15 测试 Attack01/02/03 动画 key 输出正确。
- [ ] 10.16 测试配置校验覆盖缺失 stage。
- [ ] 10.17 测试配置校验覆盖非法 duration。
- [ ] 10.18 测试配置校验覆盖非法 combo window。
- [ ] 10.19 测试配置校验覆盖缺失动画 key。
- [ ] 10.20 测试 FullBody replay 对三段攻击状态收敛。

## 11. 静态边界验证
- [ ] 11.1 静态搜索确认新增攻击逻辑不引用 `BBBNexus`。
- [ ] 11.2 静态搜索确认攻击逻辑不直接调用 `CharacterController.Move`。
- [ ] 11.3 静态搜索确认攻击逻辑不直接调用 Animancer 或 Animator 播放 API。
- [ ] 11.4 静态搜索确认攻击窗口 sampler 不引用 Animancer。
- [ ] 11.5 静态搜索确认攻击窗口 sampler 不引用 `AnimationClip`。
- [ ] 11.6 静态搜索确认状态机 transition evaluator 不读取输入缓冲。
- [ ] 11.7 静态搜索确认没有新增 per-action MonoBehaviour controller 决定状态权威。

## 12. OpenSpec 和构建验证
- [ ] 12.1 运行 `openspec validate add-light-attack-combo-action --strict --no-interactive`。
- [ ] 12.2 修复所有 OpenSpec validation 问题。
- [ ] 12.3 运行定向 EditMode 测试。
- [ ] 12.4 运行相关现有 EditMode 测试，至少包含输入缓冲、Action 仲裁、统一状态机、FullBody replay。
