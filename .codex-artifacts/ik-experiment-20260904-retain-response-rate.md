# Foot IK 实验：下坡事件保留响应速率

## 假设

ZZZ 在台阶边缘会硬切换 `position_a/b` 和高度历史，但最终脚输出仍由独立的局部混合与高度速率推进。`e77a63084` 同时清掉 SwingResidual 并旁路 CorrectionResponse 速率，可能把约一级台阶的变化集中到单帧，导致腿部伸缩和裙摆抖动。

## 本轮唯一变量

保留 `lateDownwardLandingRevision` 对 SwingResidual 的清理；删除该事件对 CorrectionResponse 的直达旁路，让现有 `1.8/1.5 × dt` 响应速率继续生效。

## 预期

目标重基准仍能生效，但最终脚位以约 2–3 cm/帧推进，降低 Releasing/Landing 的弯曲突变和全身抖动。代价是目标可能延后数帧到达；需用同一 Trace 检查踩点距离、跨级残差和 Plant 距离。

## 验证状态

代码提交后等待 MCP 连接修复；未回放前不宣称有效。回放必须使用 Trace `43357ff3cd384e5cba75d2c31175b116`，并与 `e77a63084` 对比 GroundPath、目标高度、SwingResidual、响应历史、Releasing/Landing、Contact、Pelvis、最终脚底、物理 ankle、bend/extension、穿透和 Plant 输出距离。
