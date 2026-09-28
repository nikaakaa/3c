# Rush 结束时 MotionWarp 找不到激活动作

## 报错与调用顺序

用户报错为 `motion_warp_invalid_state: Timeline MotionWarp Action Context 'ability:RushAttack' is not active.`，发生在 Fixed 的 MotionWarp 应用入口。

当前执行顺序是：Ability Tick 推进 Timeline → 技能根完成并更新 Action 生命周期 → 收集本 Tick 已产生的 Timeline 位移和 MotionWarp → ResolveMotion。动作已结束不代表这个 Tick 已采样的末段位移作废。

旧请求只携带 Context 名称，消费时使用 `FindActive(contextId)`。因此生命周期已进入终态的请求无法取到原动作；若同名技能重新开始，还存在读到新动作的风险。`FindEmptyInstanceIndex` 允许复用终态槽位，所以单纯改成读取当前终态槽也无法覆盖同 Tick 接招。

## 修改链路

- `CharacterTimelineHost.CopyPendingMotionWarps` 从当前播放记录传递动作实例 ID、PredictionKey、技能执行代次，和已有 Context、Ability 一同进入正式请求。
- Fixed/Float32 `ActionStateStore` 在进入技能执行时保留该 Tick 的动作快照，绑定正式执行代次时同步更新。快照随本次 Evaluation 清理，供该 Tick 已产生的输出结算；不改变正式动作生命周期或恢复已结束技能。
- `MotionWarpTarget` 从本次执行快照按实例 ID 查询，并核对 Context、PredictionKey、执行代次和 Ability。已结束动作的末段读取自己的目标快照；身份缺失或不匹配仍失败，不转用其它动作。
- 循环续接判定增加上述动作实例身份检查，防止把两个动作实例当作同一个 Clip 的前后圈。

没有修改技能资产、输入、动画、IK 或烘焙资源；不需要重新生成技能产物。

## 验证

Unity 编译、域重载完成，目标 Editor 为 Edit 模式且非编译状态。回读确认 Fixed/Float32 的新查询接口已加载；Console 错误数为 0。`git diff --check` 通过。

静态核对 Evaluation 生命周期：快照在 `BeginEvaluation` 清空；技能执行和代次绑定时保留；`ResolveMotion` 在 `Complete/EndEvaluation` 之前读取；正常完成和 Abort 路径均通过 `EndEvaluation` 清理。

未运行 Play/replay，未新增测试代码，未测托管分配。实际 Rush 结束、松开和接招的画面与端到端运行由用户手测，不能把编译成功当作运行验收。
