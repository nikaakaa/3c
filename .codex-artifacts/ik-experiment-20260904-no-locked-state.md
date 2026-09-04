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

待固定 Trace 回放完成后补充实际 Samples 与逐字段对比。若 Landing 永不收口、事件链重排或脚位无收益，保留样本并精确回退本轮代码。
