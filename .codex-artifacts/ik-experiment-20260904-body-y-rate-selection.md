# Foot IK 实验：按动画髋部世界 Y 运动方向选择响应速率

候选提交：`736f61c11`

输入：`43357ff3cd384e5cba75d2c31175b116`

## 假设

当前 `ApplyCorrectionResponse` 按 `DesiredResponse - PreviousResponse` 的正负选择 1.8/1.5。ZZZ 的已闭合证据是按对象世界 Y 运动方向选择两档速率：上移用 1.8，下移用 1.5；这层用于身体运动造成的脚部抖动和间隙收拢，不是最终脚世界速度上限。

## 唯一变量

在唯一 `CharacterFootInterpolationRuntime` 中保存上一帧动画髋部位置。只有髋部沿 `ComponentUp` 的帧间位移超过 `GeometryEpsilon` 时，响应速率改按髋部上移/下移选择 1.8/1.5；静止或微动仍按原响应量正负选择。接触 `ContactWorldResidual` 仍不积分标量，目标、查询、PlantWorldResidual、Transition、Pelvis 和 Profile 不变。

## 预期

身体上移时提高脚部修正追踪，降低 Releasing/Swing 的短时悬空和腿部抖动；身体下移时保留较慢收拢，避免下坡加速造成穿透。代价是髋部运动方向与脚部修正方向相反的帧可能改变左右脚响应节奏，需检查全包跳变、Plant 穿透、Bend/Extension、Pelvis 与左右对称性。

## 验证结果

正式回放完成 1044/1044 帧，采样目录：`Diagnostics/GeneratedFootSampling/20260903-201919-e93ef4847bad490a94b4a01512757cf8`。本轮 Analyzer 未生成新的 diagnoses report，使用新 CSV 与 184 基线对账。

GroundPath、Contact Edge、LandingCompleted、ReleaseCompleted、Plant verified、Physical/Pelvis 覆盖均与基线一致；Plant 正穿透仍为 27 行，最大值仍为 `0.083242m`。但左脚 2069 的 Plant penetration 从 `0.066610m` 增至 `0.080244m`，Plant output distance 从 `0.072821m` 增至 `0.085470m`；右脚 1916 仍保留旧 `PreviousResponseOutputPoint`，residual after decay Y 仍为 `0.349391m`、Plant output distance `0.569929m`，主问题没有触及。

响应速率分布发生非目标变化：左脚 1.5/1.8 为 `340/402`（基线 `301/441`），右脚 `346/403`（基线 `287/462`）；更多帧使用 1.5。动画髋部上移 506、下移 436、保持 101、方向切换 61，位置与基线一致。最终 Sole/Ankle 最大跳变基本不变，右脚物理 ankle `0.265049m` 对基线 `0.265114m` 仅浮点级差。

结论：失败。ZZZ 的对象世界 Y 速率判据不能直接替换本项目响应标量的方向判据；它扩大了慢速帧并增加了左脚接触穿透，却没有改变旧 Contact residual。保留采样和上述结论，精确回退本候选。
