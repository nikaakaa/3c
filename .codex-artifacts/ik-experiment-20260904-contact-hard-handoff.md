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

待固定 Trace 回放完成后补充实际 Samples、关键帧和完整诊断对比。若硬交接造成大跳变、穿透或腿部回归，保留样本并精确回退本轮代码，恢复半步候选。
