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
- 最早的最终脚底差异出现在左脚序列 187，离散约束状态仍为 `Releasing`；这说明 SwingMotionBuilder 的有符号结果会沿 `ResolveSwingCorrection → ReleaseTarget` 进入 Releasing，不是只影响 Swing。
- 两个主要跳变帧的 Swing raw/filtered 高度字段均为 0，候选在这些帧没有实际改变高度目标，因此它不可能修复这两个交接点。
- Plant 有效目标行共 527 行；还原未截断的 `dot(PlantTargetPoint - responseOutputPoint, ComponentUp)` 后，输出低于目标的正侧行 `28 → 38`，输出高于目标的负侧行 `232 → 208`，两侧最大绝对高度差仍约 `14.15cm / 34.94cm`。原字段只保留正侧，不能单独代表完整间隙。
- 完整向量复核显示变化以 Up 为主但不是纯 Y：最终脚底 X/Z 最大变化约 `1.68cm / 3.02cm`，实际 ankle X/Z 最大变化约 `3.75cm / 7.02cm`；最终脚底三维最大变化约 `31.22cm`，实际 ankle 三维最大变化约 `26.45cm`。
- 腿部姿态复核：`solved-bend-degrees` 最大变化约 `90.8°`；两脚合计 Swing 相邻对中弯曲变化超过 `30°` 的数量 `2 → 14`；接近伸直（bend < `5°`）的帧左脚 `119 → 196`、右脚 `120 → 146`。`target/solved-extension-ratio` 最大变化约 `0.331 / 0.320`。
- 约束状态行从 `Landing 447 / Locked 80` 变为 `Landing 490 / Locked 37`，与候选新增 Plant 目标落后导致 Landing 完成资格减少相符。
- 正式分析器仍因当前 full Plan JSON 在 dataset 后存在尾逗号而在位置 175 解析失败；以上为原始 CSV 对账结果。

解释：有符号 Swing 修正确实让脚在部分 Swing 区间向低处目标移动，减少了一部分输出高于目标的间隙，但它同时沿共享 `SwingMotion` 合同影响了 Releasing、腿部伸展和弯曲方向；而两个主要跳变帧的 raw/filtered 高度本来就是 0，真正跳变仍发生在 Plant 世界目标交接。进入 Plant 时，旧的向下修正又被带入世界目标响应，导致输出低于已验证目标，Landing 完成资格减少，最终表现为穿透和弯曲/伸直加重。

视觉验收：用户需要在同一条回放中确认 Swing 下台阶是否更自然，以及新增的 Plant 穿透是否可接受。若不接受，执行 `git revert --no-commit ab259951c` 并提交回退；若接受，再单独实验 Swing→Plant 的高度历史交接，不能把两个变量合并。
