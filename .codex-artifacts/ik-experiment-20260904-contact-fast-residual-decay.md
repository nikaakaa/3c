# 首次已验证 Contact 加快世界残差衰减

## 假设

半步交接已经把旧 Release 响应的首帧影响降低，但 `PlantWorldResidual` 首次仍按 `0.03s` 半衰期衰减。若“平滑跨过一级”主要由这条衰减曲线造成，首次已验证 Contact 将半衰期缩短一半，应该更快接近踏面；后续帧保持原曲线，避免全局改速率。

## 改动

- 仅在 `ReleaseCompleted` 后进入已验证、已进入状态的首个 Plant 事件中，将本次解析出的残差半衰期乘 `0.5`。
- 普通 Contact、同事件持续帧、Swing/Release 及其它换代仍使用现有 Profile 半衰期。
- 交接剥离量、目标、查询、Transition、Pelvis、Solver、穿透容差和其它 Profile 不变。

## 预期

右 1916 的 Plant output 与后续 residual 尾巴更快落向已验证踏面，目标 extension/bend 更快恢复；必须检查首帧和后续跳变、穿透、左右脚、Pelvis/Physical 全包覆盖。

## 回放

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260904-000028-6eb2d0501932437aa15191f6c5823c26/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，编辑器回到 Edit；Console 只有既有 FinalIK serialization-depth 日志。
- 右 1916 确认命中首个 Contact 半衰期 `0.015s`，基准为 `0.03s`；右 1918 恢复 `0.03s`，左 2069 未改变。
- 右 1916 captured residual Y 与半步候选相同 `0.5010123m`，但 after-decay `0.3408863 → 0.231937319m`，Plant output `0.5647549 → 0.384257972m`。
- 右 1916 target/solved extension `0.7845701/0.7845701`，solved bend `76.72625°`；final sole/ankle/physical 首帧跳变 `0.453434/0.333839/0.333839m`，半步候选为 `0.273010/0.158563/0.158563m`，ankle/physical 已超过 0.3m。
- GroundPath 1796/292，Contact Edge 114，Landing/Release 24/53，Plant positive 27，最大穿透 0.083242m，Bend 172.9645°，Extension 0.999992/1.19071，Pelvis 753/2088，Physical 2088/2088。

结论：半衰期减半虽降低 Plant output，但制造明显首帧脚位跳变，判为失败。保留本样本和记录，精确回退 `ec6082841`，恢复 `706d83fd1` 半步行为。
