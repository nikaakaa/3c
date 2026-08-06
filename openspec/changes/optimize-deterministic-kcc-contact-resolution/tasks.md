# Tasks: 优化 Deterministic KCC 接触与 Step 热路径

## 1. 对账与基线

- [ ] 1.1 固定当前 wall contact 坏帧的 LogicTick、Kcc query summary 与 Profiler 采样基线。
- [ ] 1.2 对照 Philippe reference 记录 movement closest hit、stability、step detection、commit 和普通 slide 的调用顺序。
- [ ] 1.3 记录当前 canonical contact set 中 representative、stable support、wall obstruction 和同 TOI 多 contact 的实际组合。
- [ ] 1.4 明确当前 `deterministic-kcc-world-solver`、`fix-deterministic-kcc-zero-progress-contact` 与本 change 的职责边界。

## 2. Representative selection

- [ ] 2.1 定义无分配 movement obstruction representative workspace。
- [ ] 2.2 按 Fixed displacement、effective obstruction normal 与 canonical contact identity 实现唯一选择。
- [ ] 2.3 保持所有 contact 进入 constraint plane、collision summary 和 output contact。
- [ ] 2.4 删除对非 representative contact 的完整 `EvaluateHitStability` 与 `TryDetectStep` 调用。
- [ ] 2.5 保持 representative selection 不读取 Unity 对象、无序集合或 Presentation 状态。

## 3. Step admission

- [ ] 3.1 将 previous stable、upward intent、closing direction 与 vertical obstruction 准入集中为唯一正式判断。
- [ ] 3.2 为 obstacle height envelope 选择固定 primitive/feature 或轻量 top-clearance 证据。
- [ ] 3.3 在 Standard/Extra CastAll 前拒绝无法 Commit 的 contact，并保留确定性 rejection。
- [ ] 3.4 保持 CheckStepValidity、SurfaceId、overlap、clearance、remaining 和 Ground Probe 合同。
- [ ] 3.5 删除重复的 Detection/Commit 前置判断，避免两套准入真相。

## 4. Query Kernel

- [ ] 4.1 为 closest-only Raycast 增加不依赖全量排序的 stable selection。
- [ ] 4.2 为当前 Move 内的相关 Step queries 建立预分配 candidate context。
- [ ] 4.3 保证 context 按 query bounds 过滤，禁止跨 Tick 保存或跨 Actor 共享。
- [ ] 4.4 保持 candidate/contact order、tie-break、capacity failure 与 QuerySummary 结果。
- [ ] 4.5 检查热路径不存在 LINQ、动态集合、字符串和隐式扩容。

## 5. Diagnostics 与身份

- [ ] 5.1 为 Move、representative、admission、Standard、Extra、validity、Ground Probe 和 Query 增加结构化计数。
- [ ] 5.2 在 Unity adapter 的正式边界增加 Profiler marker，不在 Inspector 回调执行采样重操作。
- [ ] 5.3 将 representative/admission/query strategy version 纳入 KccId 或 WorldConfigurationHash。
- [ ] 5.4 保持 Snapshot/StateHash 不包含 query workspace、contact manifold 和 diagnostics。

## 6. 回归验证

- [ ] 6.1 增加 portable 多 contact 同 TOI fixture，验证只执行一次完整 Step Detection 且全部约束仍生效。
- [ ] 6.2 增加高墙持续顶撞 fixture，验证 Step admission 直接拒绝并保持普通 slide/blocked result。
- [ ] 6.3 增加合法 0.14m、0.24m、0.40m StepCapabilityCourse fixture，验证 Standard/Extra 与 SurfaceId commit。
- [ ] 6.4 增加 Raycast closest fast path 与 canonical 全排序结果的一致性验证。
- [ ] 6.5 验证两端相同输入、CollisionWorldHash 与 KccId 下 BodyResult、QuerySummary identity 和 StateHash 一致。

## 7. 文档与校验

- [ ] 7.1 更新 current deterministic KCC spec delta 中 movement representative、admission 与 query contract。
- [ ] 7.2 对比并指出 current spec 与 Philippe closest-hit 调用顺序的矛盾。
- [ ] 7.3 对账 `openspec/project.md` 中 Fixed KCC 当前身份与本 change 的策略版本。
- [ ] 7.4 执行 `openspec validate optimize-deterministic-kcc-contact-resolution --strict --no-interactive`。

