# ReleaseCompleted 后已验证 Contact 直接硬交接

## 假设

ZZZ 的接触纪元边界在记录层硬覆盖，平滑属于下游独立步骤。当前 3C 在 `ReleaseCompleted → Contact` 时把上一实际输出捕获为 `PlantWorldResidual` 并指数衰减，可能正是脚在低一级踏面上方平滑跨过的来源。本轮验证接触事件是否应直接采用已验证目标。

## 改动

- 仅在 `ReleaseCompleted` 后下一次进入已验证、已进入状态的 Plant 事件时，跳过旧可见输出到新目标的 `PlantWorldResidual` 捕获。
- 新 Contact 直接使用当前已验证目标；若中间先进入普通 Swing，不触发硬交接。
- 查询、目标、TargetHeight、其它 Contact 残差、响应、Transition、Pelvis、Solver 和 Profile 不变。

## 预期

若旧世界残差是主要拖延来源，右 1916 应直接落到新目标，Plant output 距离和后续尾巴消失；同时必须检查脚底/踝部首帧跳变、穿透、bend/extension 和左右脚覆盖。

## 回放

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260903-234222-5fc7c2ca3146462e8ea528c58802eeb2/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，编辑器回到 Edit；Console 只有既有 FinalIK serialization-depth 日志。
- 右 1916 的 captured/after-decay Plant residual 均为 0，Plant output 为 0；右 1918 仍为 0，直到 1926 才以负 residual 重新捕获，连续性已断裂。
- 右 1916 target/solved extension `1.03880012/0.99999`，solved bend `0.510910034°`；相比半步候选 `0.7801122/0.7801122/77.54771°`，腿被直接拉直。
- 右 1916 final sole/ankle/physical 首帧跳变 `0.837642/0.715679/0.713235m`，半步候选为 `0.273010/0.158563/0.158563m`。
- Plant positive `27 → 34`，新增 7 个穿透事件；最大穿透仍 0.083242m。Plant 最大 output 虽降至 `0.338173m`，只是硬清零造成的假收益。
- GroundPath 1796/292、Contact Edge 114、Landing/Release 24/53、Verified 527、Pelvis 753/2088、Physical 2088/2088 保持覆盖，但右全局最大 sole/ankle/physical 跳变达到 `0.837642/0.715679/0.713235m`。

结论：硬交接失败。它证实旧 Residual 不能在该边界直接清零；代价是连续性断裂、腿伸直、脚位大跳变和穿透增加。保留本样本和记录，精确回退 `89bf9a6cb`，恢复 `706d83fd1` 半步行为。
