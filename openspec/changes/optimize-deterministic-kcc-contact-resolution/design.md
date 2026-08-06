# Design: Deterministic KCC 接触代表与查询预算

## 现状

Motor 一轮 movement 先通过 `Cast` 得到最早 TOI 和 canonical contact set，然后对 contact set 每一项调用 `EvaluateHitStability`。Stability 无条件完成 inner/outer Raycast；非稳定 contact 继续执行 Standard/Extra Step。Wall contact 通常没有合法 landing，却会完整遍历候选并重复查询。

Philippe reference 在 movement loop 中使用单个 closest sweep hit 执行一次完整 Stability/Step evaluation，随后使用同一 hit 的 stability 结果决定 step commit 或普通 obstruction projection。项目必须保留 canonical contact set 供多平面约束，但不能把 contact manifold 的完整处理要求扩大为每项都执行 movement policy。

## Decision 1: 分离 contact manifold 与 movement obstruction

每个最早 TOI contact set 仍按现有 PrimitiveId、FeatureId、normal 和 TOI 规则排序、去重与保留。新增无分配的 representative selection：

1. 过滤没有闭合当前 displacement 的 contact。
2. 计算 previous ground 参与的 effective obstruction normal。
3. 过滤 base stable contact 与明显非 obstruction contact。
4. 按 canonical contact identity 选择唯一 representative。

Representative 只用于完整 `EvaluateHitStability`、Step Detection 和 Step Commit。全部 contact 仍用于 `ClassifyContact`、`AddConstraintPlane`、remaining projection、zero-progress signature 和最终 validation。若没有 representative，直接走普通约束，不执行 Step query。

这样可以同时保持：

- 多面墙角不会丢失任意 active plane。
- Step Detection 不依赖 contact 容器遍历顺序。
- Step policy 与 Philippe 的单 closest hit 调用边界一致。

不采用“遇到第一个 contact 就直接返回”：canonical 第一个可能是稳定支持面，不能遮蔽同一 TOI 的真正墙面 obstruction。

## Decision 2: Detection admission 与 Commit 使用同一准入事实

当前 Commit 已要求 previous stable、无向上意图和垂直 obstruction，但 Detection 在更早阶段没有使用这些事实。新流程先构造正式 admission：

- previous state stable on ground；
- 当前 request 没有明确向上分量；
- displacement 对 effective obstruction 存在正闭合分量；
- obstruction 近似垂直；
- obstacle 的固定几何证据仍在 MaximumStepHeight 可处理范围内。

准入失败直接记录对应 rejection，不进入 Standard/Extra CastAll。几何高度不能仅使用 contact point Y；应使用 primitive/feature 的固定垂直范围或一次轻量 top-clearance probe，避免误拒绝 blocker 与 stepped surface 不同 Collider 的合法 Step。

Detection 通过后仍必须执行现有 CheckStepValidity 全流程；admission 只减少不可能分支，不取代 overlap、outer/inner ground、upward clearance、SurfaceId commit 和最终 validation。

## Decision 3: Query 优化保持 canonical output

Raycast closest-only fast path 只能改变内部收集方式，不得改变 closest identity、distance、normal、tie-break 或 summary。对同一候选范围的 Standard/Extra 子查询，可使用预分配 query context 保存稳定候选 ID，再按各 shape bounds 过滤；context 只存在当前 Move 调用，不进入 Snapshot、Hash 或跨 Tick cache。

不使用动态 List、LINQ、字符串、Unity Physics 或隐式容量扩张。候选容量不足、contact overflow 和 conservative advancement failure 仍按原 stage 失败。

## Decision 4: Catch-up 独立处理

本 change 不改 `GameplayTickSettings.DefaultMaxCatchUpTicks`。先把碰撞 Tick 降回 60 Hz 预算内，再根据实际帧预算决定是否需要新的正式 clock policy。降低 catch-up 可以减少单帧尖峰，但会牺牲逻辑时间连续性；它不能证明 KCC 已经修复。

## 预期调用链

```text
Cast earliest TOI
-> canonical contact set
-> representative admission
-> one EvaluateHitStability / Standard-first Step
-> all contacts constraint classification and projection
-> optional one Step commit
-> Ground Probe / final validation
```

## 失败边界

- 代表选择没有合法 obstruction：普通 multi-plane movement。
- Detection admission 失败：保留 rejection 诊断，普通 multi-plane movement。
- Step candidate 或 commit 任一正式阶段失败：不改变 safe position，继续普通约束。
- query capacity、conservative advancement、penetration recovery、actor/static reconstraint 或无法证明零进展收敛：保持 deterministic failure。
- 不建立第二 Motor、第二 Query Kernel、Step fallback 或运行时旁路。

