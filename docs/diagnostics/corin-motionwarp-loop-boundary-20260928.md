# 可琳 E 行走循环边界 MotionWarp 报错

## 现场与原因

用户报错：`thirdperson.simulation.fixed-ability-evaluate` 失败，`motion_warp_ambiguous_modifier`，owner 为 `36`。

读取正式 Fixed 产物，`BranchAttack` 的 operation 36 为 Timeline，引用 `7bf8c586-06eb-00ff-1f88-f4030c3e41c9`，对应 `CorinBranchWalkTimeline`。该资产只有一个 MotionWarp，标识为 `40480466-e33e-0365-dff7-bf89e4256d23`。

用正式 `TimelineRuntimeEvaluationSegments` 和 `TimelineRuntimeMotionSampling.SampleWarp` 对循环边界执行只读计算，得到同一 Clip 的两条请求：

- cycle 0：1.0753333726897836 → 1.0833332538604736 秒。
- cycle 1：0 → 0.00800000037997961 秒。

旧 `ApplyDirect` 只允许一个 Timeline owner 对应一条请求，因此把正常的循环分段当作修改器争用。此前扩展到整段持续转向后暴露了这个限制。

相关位移链路还存在另一个边界缺口：MotionCurve 每段各输出一条 Override contribution，而仲裁只保留首个同优先级来源，因此会丢掉下一圈头段位移。仅移除异常会留下位移与转向不一致。

## 修改链路

1. `TimelineRuntimeMotionSampling` 在跨圈时按源 Clip 合并 Override 的顺序分段；各段先应用自己的 Gameplay weight，再以一条 contribution 参与原有仲裁。单段路径保持原采样逻辑。
2. `AbilityTimelineMotionWarpRuntime` 先验证全部候选，再顺序执行。仅同一来源、动作、播放激活、Clip，且相邻 cycle 的尾头边界才认作循环续接。同圈重复、不同 Clip、不同激活与乱序仍拒绝，并在错误中附上具体 cycle 和时间。
3. Fixed/Float32 的 `ResolvedMotionChannel` 累计本 Tick 已处理分段的位移与朝向。下一段以此前结果继续转向，扣除源位移时仍使用 Tick 开始朝向，避免重复旋转基准和丢失位移。

本轮没有改业务资产、IK、动画源或烘焙数据。

## 检查边界

Unity 编译及域重载完成，目标项目 Edit 模式，Console 错误数为 0。`git diff --check` 通过。

对 E 行走、E 原地持续攻击、强化 Rush 循环的实际 MotionCurve 执行只读计算：Fixed/Float32 × 跨 1 圈/3 圈，共 12 组。跨圈前分段的加权 XYZ 位移与 yaw 之和，和合并后一条 contribution 的对应值完全一致，2/4 条 Override 分段分别合并为 1 条。

调用实际续接判定：相邻循环尾头接受；同圈重复、逆序、不同播放激活、不同修改器均拒绝。

没有跑 Play/replay，没有新增测试代码。以上是编译和局部只读计算证据，尚未验证用户实际按键、画面与碰撞后的完整运行表现；未测托管分配。
