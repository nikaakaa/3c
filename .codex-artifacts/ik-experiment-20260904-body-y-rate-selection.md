# Foot IK 实验：按动画髋部世界 Y 运动方向选择响应速率

候选提交：待提交

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

当前 `ApplyCorrectionResponse` 按 `DesiredResponse - PreviousResponse` 的正负选择 1.8/1.5。ZZZ 的已闭合证据是按对象世界 Y 运动方向选择两档速率：上移用 1.8，下移用 1.5；这层用于身体运动造成的脚部抖动和间隙收拢，不是最终脚世界速度上限。

## 唯一变量

在唯一 `CharacterFootInterpolationRuntime` 中保存上一帧动画髋部位置。只有髋部沿 `ComponentUp` 的帧间位移超过 `GeometryEpsilon` 时，响应速率改按髋部上移/下移选择 1.8/1.5；静止或微动仍按原响应量正负选择。接触 `ContactWorldResidual` 仍不积分标量，目标、查询、PlantWorldResidual、Transition、Pelvis 和 Profile 不变。

## 预期

身体上移时提高脚部修正追踪，降低 Releasing/Swing 的短时悬空和腿部抖动；身体下移时保留较慢收拢，避免下坡加速造成穿透。代价是髋部运动方向与脚部修正方向相反的帧可能改变左右脚响应节奏，需检查全包跳变、Plant 穿透、Bend/Extension、Pelvis 与左右对称性。

## 验证状态

提交后用同一固定 Trace 正式回放并逐帧对账；不做视觉 Pass。若失败，保留采样和结论，精确回退 Runtime 候选并保留本记录。
