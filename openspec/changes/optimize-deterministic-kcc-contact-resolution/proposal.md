# Change: 优化 Deterministic KCC 接触与 Step 热路径

## Why

Gameplay Lab 角色无碰撞时约 62 FPS，碰撞墙体后单个 `Session.LogicTick` 从约 4 ms 上升到 13～14.5 ms。坏帧中 `GameplayTickSystem` 在同一个渲染帧内补跑 4 个逻辑 Tick，主线程帧达到约 60～68 ms；Physics 只有约 0.28 ms，Render Thread 在等待主线程。

当前 `DeterministicKccMotor` 对同一最早 TOI 的全部 canonical contacts 分别执行完整 `EvaluateHitStability`。每个 contact 都会执行 inner/outer Raycast；非稳定 contact 还会进入 Standard/Extra Step 候选、Overlap、clearance Cast 与再次 Raycast。该调用组织偏离 Philippe KCC 对单个 closest sweep hit 执行一次完整 stability/step evaluation 的 movement policy，导致角色持续顶墙时重复支付失败 Step 查询成本。

`MaxCatchUpTicks` 只放大已经存在的单 Tick 超预算，不能作为 KCC 修复。当前 change 不修改 catch-up 策略、Step 能力、Collision Artifact 作者数据或 Unity Physics 链路。

## What Changes

- 为每轮最早 TOI contact set 定义唯一、稳定的 movement obstruction representative。
- 只对 representative 执行一次完整 Hit Stability 与 Standard-first/Extra Step Detection。
- 其它 canonical contacts 继续进入完整多平面 constraint、碰撞分类、最终 overlap validation，不再重复执行 Step 查询。
- 在 Step Detection 之前加入与当前正式 Commit 合同一致的确定性准入：previous stable ground、向上意图、effective obstruction 方向和可跨越高度范围；明显不能 Commit 的 contact 不进入完整 Step 查询。
- 保持 Ground Probe 的独立稳定性语义，不把 movement representative 优化变成第二 Grounding 路径。
- 优化 Raycast closest 结果、相关 Step query 的 candidate 复用和重复排序，同时保持 stable primitive/contact order、Fixed math、capacity failure 与 canonical result 不变。
- 为 KCC movement、stability、step standard/extra、query candidate 与 Ground Probe 增加可移植诊断计数，并在 Unity Profiler 边界提供细粒度采样；正常热路径不创建字符串或动态集合。
- 提升 Motor/Query policy identity，将 representative selection、Step admission 和 query execution strategy 纳入 KccId 或 WorldConfigurationHash。
- 保持单一 `DeterministicKccMotor`、单一 `DeterministicCapsuleQueries`、单一 WorldSolver 链路，不建立 fallback、兼容路径或旁路实现。

## Impact

- Runtime：`DeterministicKccMotor`、Stability、Capsule Queries、Query Summary/diagnostics、KCC identity。
- Unity adapter：只增加正式 Profiler 边界，不在 `OnInspectorGUI`、`OnValidate` 或自动资产选择路径执行重操作。
- Current spec：`openspec/specs/deterministic-kcc-world-solver/spec.md`。
- Active change coordination：`fix-deterministic-kcc-zero-progress-contact` 已锁定不修改 Step/Ground Probe 语义，本 change 只改变 contact evaluation 的调用边界；`close-deterministic-rollback-character-pipeline` 只消费更新后的 KccId，不修改 Motor；其它楼梯与表现 change 不得复制 KCC 或建立第二 query 链。

## Current Spec 对账

- Current `Grounding` requirement 要求每个 movement hit 先完成 inner/outer probe；这与 Philippe 参考实现每轮对 closest sweep hit 执行一次完整 `EvaluateHitStability` 不一致。本 change 将其改为：Ground Probe hit 继续完整评估；movement earliest TOI contact set 选择唯一 representative 完整评估，其余 contact 只进入基础稳定性、constraint 与碰撞分类。
- Current `Step Up` requirement 已锁定 Standard-first、Extra、候选 overlap、outer/inner ray、upward clearance、SurfaceId commit 与 remaining movement；这些阶段和结果不删除，只限制为每轮唯一 representative 执行一次，并把 Commit 的前置条件提前为 Detection admission。
- Current `KCC 必须使用确定数值与固定查询顺序` 和 `热路径必须有界且无隐式扩容` 允许在不改变 canonical output 的前提下移除无必要排序、复用候选工作集；新增的 representative/admission identity 必须进入 KccId，避免不同策略静默互连。
- Current `KCC 失败必须终止确定模拟而不回退` 保持不变。容量溢出、query non-convergence、penetration failure 与无法满足静态约束仍然失败，不以性能优化吞掉真实错误。

## Out of Scope

- 不降低或关闭 Step Height。
- 不把 KCC 替换为 Unity CharacterController、PhysX、Float32 solver 或 DotRecast。
- 不修改 `MaxCatchUpTicks` 作为本 change 的解决手段。
- 不修改 Collision Artifact 几何、场景 Collider、Foot Placement、动画、Presentation、Body Motion 或 Network Model。

## Verification

- Portable deterministic test MUST构造同一最早 TOI 返回多个 contact 的墙面 fixture，验证 representative 只执行一次完整 Step Detection，所有 contact 仍进入相同 constraint result，最终 BodyResult 与 canonical identity 稳定。
- Query test MUST验证 closest-only fast path 与原 canonical Raycast 结果在 hit identity、distance、normal 和 summary capacity 规则上相同。
- Unity Editor 中由用户在 GameplayLab 通过原有显式入口复现持续顶墙场景，确认坏帧不再出现连续 4 个 13 ms 级 `Session.LogicTick`；交付时说明具体操作路径和观察指标。

## Tradeoffs

- 只做 representative 选择：最接近 Philippe 语义，行为风险最低，但单个复杂 obstruction 仍可能触发完整 Step 查询。
- 增加高度/方向 admission：对高墙持续顶撞收益最大，但会改变部分边界 Step Detection 的诊断与准入，必须提升 KccId 并重新验证 StepCapabilityCourse。
- 做 query candidate 复用：可降低所有 KCC 查询成本，但会扩大 Query Kernel 修改面，需要验证 candidate buffer 容量、排序和跨查询污染边界。
- 调低 catch-up：能改善渲染帧尖峰，但会丢弃逻辑时间，可能影响动作输入和确定性回放；不作为本 change 的替代方案。

