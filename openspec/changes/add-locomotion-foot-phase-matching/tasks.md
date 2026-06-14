## 1. 准备和边界确认
- [x] 1.1 确认本变更基于 `add-locomotion-foot-phase-matching` proposal 已审批。
- [x] 1.2 读取 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.3 确认 active changes 中 TurnBack 进入、motion source、EntryLocal 相关文件没有未合并冲突。
- [x] 1.4 记录当前 `TurnBack -> RunLoop` 的手动复现步骤。
- [x] 1.5 确认第一版只覆盖 `TurnBack -> RunLoop`。

## 2. 脚相位纯数据模型
- [x] 2.1 新增 `LocomotionFootPhase` enum。
- [x] 2.2 新增 `LocomotionFootPhaseMarker` 纯数据结构。
- [x] 2.3 新增 `LocomotionFootPhaseSample` 纯数据结构。
- [x] 2.4 新增 `LocomotionFootPhaseMatchRequest` 纯数据结构。
- [x] 2.5 新增 `LocomotionFootPhaseMatchResult` 纯数据结构。
- [x] 2.6 为所有结构设置无效/default 状态。
- [x] 2.7 确认模型不引用 Animancer、AnimationClip、Transform、CharacterController、InputAction。

## 3. FootPhaseProfile 配置
- [x] 3.1 新增 `LocomotionFootPhaseProfileSO`。
- [x] 3.2 在 profile 中保存 phase、gait、alias key。
- [x] 3.3 在 profile 中保存 marker 列表。
- [x] 3.4 在 profile 中保存是否参与 phase matching 的显式开关。
- [x] 3.5 实现 marker normalized time 钳制或校验规则。
- [x] 3.6 实现 marker 顺序校验。
- [x] 3.7 实现重复 phase marker 校验。
- [x] 3.8 实现空 alias key 校验。
- [x] 3.9 实现没有 marker 但启用 matching 的错误。
- [x] 3.10 不把缺失 profile 当作可匹配配置。

## 4. Profile 绑定入口
- [x] 4.1 在 `RunLocomotionAnimationConfigSO` 增加 foot phase profile 绑定数组。
- [x] 4.2 绑定项包含 phase、gait、alias key、profile、enabled。
- [x] 4.3 增加按 phase/gait/alias 解析 foot phase profile 的方法。
- [x] 4.4 校验绑定 profile 与 phase/gait/alias 一致。
- [x] 4.5 校验 TurnBack matching 需要 TurnBack profile。
- [x] 4.6 校验 RunLoop matching 需要 RunLoop profile。
- [x] 4.7 不校验或覆盖 Animancer TransitionAsset 的 fade/speed/start time。

## 5. Foot phase sampler
- [x] 5.1 新增 `LocomotionFootPhaseSampler`。
- [x] 5.2 采样器输入只包含 profile 和 normalized time。
- [x] 5.3 采样器输出当前 marker 或最近有效相位。
- [x] 5.4 处理 normalized time 循环到 `[0,1)` 的 RunLoop 场景。
- [x] 5.5 处理非循环 TurnBack 的 clamp 场景。
- [x] 5.6 无 profile 时输出 invalid sample。
- [x] 5.7 profile 禁用 matching 时输出 invalid sample。
- [x] 5.8 无效 marker 时输出 invalid sample 并让校验覆盖原因。

## 6. Phase matching resolver
- [x] 6.1 新增 `LocomotionFootPhaseMatcher`。
- [x] 6.2 输入 `ExitFootPhase` 和目标 RunLoop profile。
- [x] 6.3 解析同脚支撑相位的目标 marker normalized time。
- [x] 6.4 多个候选 marker 时选择配置顺序中第一个或明确排序后的第一个。
- [x] 6.5 找不到同相位 marker 时返回 invalid result。
- [x] 6.6 invalid result 不修改 RunLoop 起播时间。
- [x] 6.7 输出诊断原因，便于日志和测试断言。

## 7. 黑板 facts 扩展
- [x] 7.1 扩展 `CharacterRuntimeAnimationFacts` 保存当前 locomotion foot phase sample。
- [x] 7.2 扩展 `CharacterRuntimeAnimationFacts` 保存 last locomotion exit foot phase sample。
- [x] 7.3 更新构造函数，保持现有调用点可迁移。
- [x] 7.4 更新 `Default`。
- [x] 7.5 更新黑板 snapshot。
- [x] 7.6 更新黑板 restore。
- [x] 7.7 更新 rollback snapshot comparer。
- [x] 7.8 更新 rollback/synctest log formatter。
- [x] 7.9 确认 Presenter 不直接写黑板。

## 8. 动画 facts adapter 接入
- [x] 8.1 找到当前写入 `CharacterRuntimeAnimationFacts` 的 adapter 调用点。
- [x] 8.2 在写入 animation facts 前采样当前 locomotion foot phase。
- [x] 8.3 当前播放 progress 无效时写入 invalid foot phase。
- [x] 8.4 phase/alias 不匹配 profile 时写入 invalid foot phase。
- [x] 8.5 TurnBack 可退出并将进入 RunLoop 时记录 exit foot phase。
- [x] 8.6 非 TurnBack 退出不污染 last exit foot phase。
- [x] 8.7 重播或 restore 后保持相位事实一致。

## 9. MovementAnimationContext 扩展
- [x] 9.1 为 `MovementAnimationContext` 增加可选 entry foot phase match result 或 start time override。
- [x] 9.2 保持旧构造函数可用。
- [x] 9.3 确保默认上下文不请求 phase matching。
- [x] 9.4 在上下文中保留纯数据，不携带 profile 或 Unity 对象。
- [x] 9.5 更新上下文构建调用点。

## 10. RunLoop 入场应用
- [x] 10.1 在构建 `MoveLoop + Run` 动画上下文时读取 last exit foot phase。
- [x] 10.2 只在上一状态为 TurnBack 或存在有效 TurnBack exit phase 时创建 match request。
- [x] 10.3 使用 RunLoop foot phase profile 解析 start normalized time。
- [x] 10.4 将匹配结果传给 Presenter。
- [x] 10.5 Presenter 只在新播放 RunLoop 时设置一次 `NormalizedTime`。
- [x] 10.6 相同 RunLoop 连续帧不重复设置 `NormalizedTime`。
- [x] 10.7 invalid match result 时保持正常播放并输出诊断。
- [x] 10.8 不修改 TurnBack 的 start normalized time 逻辑。

## 11. Corin 配置
- [x] 11.1 为当前 Sandbox 使用的 `Locomotion.Turn.Back` 创建 foot phase profile。
- [x] 11.2 为当前 Sandbox 使用的 `RunLoop` 创建 foot phase profile。
- [x] 11.3 将 profile 绑定到 `DefaultRunLocomotionAnimationConfig.asset`。
- [x] 11.4 初始 TurnBack exit marker 使用审批后的 normalized time。
- [x] 11.5 初始 RunLoop 左右脚 marker 使用审批后的 normalized time。
- [x] 11.6 配置缺失时通过校验报错，不新增自动 fallback。

## 12. 自动测试
- [x] 12.1 新增 sampler 测试：按 normalized time 采样 LeftPlant。
- [x] 12.2 新增 sampler 测试：按 normalized time 采样 RightPlant。
- [x] 12.3 新增 sampler 测试：RunLoop 循环 normalized time。
- [x] 12.4 新增 sampler 测试：禁用 profile 输出 invalid。
- [x] 12.5 新增 matcher 测试：RightPlant exit 匹配 RunLoop RightPlant marker。
- [x] 12.6 新增 matcher 测试：LeftPlant exit 匹配 RunLoop LeftPlant marker。
- [x] 12.7 新增 matcher 测试：目标 profile 缺少同相位 marker 时 invalid。
- [x] 12.8 新增黑板测试：foot phase facts 默认值。
- [x] 12.9 新增黑板测试：snapshot/restore 保留 foot phase facts。
- [x] 12.10 新增 Presenter 测试：RunLoop 新进入应用 start override。
- [x] 12.11 新增 Presenter 测试：RunLoop 连续帧不重复应用 start override。
- [x] 12.12 新增静态边界测试：sampler 不引用 Animancer/AnimationClip/Transform/CharacterController/InputAction。
- [x] 12.13 新增配置校验测试：缺 TurnBack profile 报错。
- [x] 12.14 新增配置校验测试：缺 RunLoop profile 报错。

## 13. 手动验证
> 待用户在 Unity Editor 中手动验证；自动化测试已通过，下面步骤用于最终体感和日志确认。
- [ ] 13.1 在 Unity Editor 打开 Sandbox 场景。
- [ ] 13.2 启用 Locomotion 与 Animation 诊断日志。
- [ ] 13.3 操作角色进入 RunLoop。
- [ ] 13.4 从前跑切反向输入触发 TurnBack。
- [ ] 13.5 观察 TurnBack 退出后 RunLoop 不出现明显左右脚交错糊脚。
- [ ] 13.6 查看日志包含 TurnBack exit foot phase。
- [ ] 13.7 查看日志包含 RunLoop phase matched start normalized time。
- [ ] 13.8 再测普通 Idle/MoveStart/MoveLoop/MoveStop 不受 phase matching override 影响。

## 14. 验证命令
- [x] 14.1 运行 `openspec validate add-locomotion-foot-phase-matching --strict --no-interactive`。
- [x] 14.2 运行 Unity EditMode 定向测试：`LocomotionFootPhase*`。
- [x] 14.3 运行 Unity EditMode 定向测试：`CharacterRuntimeBlackboard*`。
- [x] 14.4 运行 Unity EditMode 定向测试：`UnifiedCharacterStateMachine*` 中 TurnBack 相关用例。
- [x] 14.5 不运行 Unity batchmode。
