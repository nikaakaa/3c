# 不进入 3C Locked 状态

## 假设

ZZZ 的脚部有 `LockFoot`/`FootLockInfo` 相关机制，但当前证据没有证明它等同于 3C 独立的 `Landing → Locked → Releasing` 离散状态。若 3C 的 Locked 状态或 FullAnchor/Sliding 分支是造成弯曲、伸直和裙摆抖动的来源，保留 Contact Anchor 与 Landing 目标、只禁止 `LandingCompleted` 进入 `Locked`，应能观察到承重脚的表现变化。

## 改动

- `ResolvePostInterpolation` 在 Landing 完成准入成立时不再产生 `LandingCompleted`，保持当前 Landing 状态和 Contact Anchor。
- Contact 查询、Verified Anchor、PlantWorldResidual、Release、Pelvis、Solver 和 Profile 不变。
- 这是单变量的“无 Locked 状态”对照，不删除其他锁相关代码，也不声称等价复刻 ZZZ。

## 预期

Landing 完成前后的脚位应保持基本连续；如果 Locked 的 FullAnchor/Sliding 是抖动来源，右 1916 后的 bend/extension、稳定承重和裙摆代理应改善。必须同时检查 Landing/Release 计数、持续 Contact、穿透、脚底/ankle、Pelvis 和 Physical 覆盖。

## 回放

固定 Trace `43357ff3cd384e5cba75d2c31175b116` 已完整消费 1044/1044 帧并封口：

- Samples：`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260904-010957-dfc311bf461d43a39c9a545ee0f7556c/character-foot-ik%2Ffull.csv`
- capturing/finalizing/analyzing 均为 false，编辑器回到 Edit；Replay Proof `matched:1044`，Console 无新增错误。
- `LandingCompleted` 从半步基线的 24 降为 0，`ReleaseCompleted` 保持 53；canonical Locked 状态为 0，Landing 状态覆盖 527 行。
- 右 1916/1918 与半步基线完全一致：Plant residual after-decay/output `0.3408863/0.5647549m`、`0.231937334/0.384257972m`，target/solved extension `0.7801122/0.7801122`、`0.7238196/0.723819435`，bend `77.54771°/87.36483°`。
- 左 2069 与半步基线一致：after/output `-0.0666096359/0.0728213042m`，sole/ankle/physical 跳变 `0.068029/0.074007/0.074006m`。
- 全局 GroundPath 1796/292、Contact Edge 114、Plant positive 27、最大穿透 0.08324221m、Plant output max 0.5647549m、Bend 172.9645°、Extension 0.999992/1.19071、Pelvis 753/2088、Physical 2088/2088，均与半步基线一致。
- 全局 final sole/ankle/physical 最大跳变也与半步一致；没有额外尾巴、穿透或左右脚变化。

结论：取消 3C `Landing → Locked` 只改变生命周期事件计数，不改变固定 Trace 的可见脚位或主异常，无法支持“Locked 是主因”。保留本样本和结论，精确回退本轮代码，恢复半步行为。
