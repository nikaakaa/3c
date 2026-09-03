# Foot IK 实验记录：Swing 有符号高度修正

候选提交：`ab259951c`（实验恢复Swing有符号高度修正）

前置回退：`dc0e817f2`（撤销延后一帧Plant残差实验）

输入：`43357ff3cd384e5cba75d2c31175b116`

基线采样包：`Diagnostics/GeneratedFootSampling/20260903-085423-ccccf95de07a4a55a4e10b17752df116`

候选采样包：`Diagnostics/GeneratedFootSampling/20260903-105608-7ddfd35c29894028b55ba9a1b4053999`

结果：数据未通过，暂待视觉验收。

改动范围只有 Swing 高度合同：

- `CharacterFootSwingMotionBuilder` 不再把 `formalTargetCorrection` 截为非负值。
- `CharacterFootInterpolationRuntime` 不再把 Swing 的 raw/filtered 高度修正截为非负值。
- 动画 XZ、Ground Envelope、PlantWorldResidual、目标选择和接触状态没有改动。

前后 CSV 按 `sample.sequence + sample.dimension` 对齐，均为 2088 行。

- Swing→Landing 关键交接没有改善：左脚代表帧 `2067→2069` 的最终脚底跳变仍为 `6.64cm`，右脚 `1914→1916` 仍为 `16.41cm`。
- 发生最终脚底数值变化的行：1141 行；变化集中在 Swing 的向下追踪和后续 Plant 交接。
- `plant-penetration-depth > 0` 行：`28 → 38`。
- 新增最坏穿透：左脚序列 1417，约 `11.54cm`；Plant 目标约 `3.0600m`，输出约 `2.9446m`。
- 右脚代表帧 1914→1916 的跳变、Plant 输出距离和基线相同，说明该改动没有触及主要交接跳变来源。
- 正式分析器仍因当前 full Plan JSON 在 dataset 后存在尾逗号而在位置 175 解析失败；以上为原始 CSV 对账结果。

解释：有符号 Swing 修正确实让脚在 Swing 期间向低处目标移动，但进入 Plant 时，旧的向下修正仍被带入世界目标响应，导致输出低于已验证目标。当前唯一 Plant 交接没有消费这份 Swing 高度历史，也没有把 Swing 输出与 Plant 目标重新建立安全的连续边界，因此“减少悬空”变成了“进入 Plant 时穿透”。

视觉验收：用户需要在同一条回放中确认 Swing 下台阶是否更自然，以及新增的 Plant 穿透是否可接受。若不接受，执行 `git revert --no-commit ab259951c` 并提交回退；若接受，再单独实验 Swing→Plant 的高度历史交接，不能把两个变量合并。
