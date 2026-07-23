## 1. 等待接入门禁

- [ ] 1.1 获得用户“当前闭环完成，可以接入 KCC”的明确确认。
- [ ] 1.2 确认 `rebuild-deterministic-kcc-step-solving` 隔离实现已经完成。
- [ ] 1.3 确认隔离实现尚未单独合入当前目标分支。
- [ ] 1.4 确认两个 change 都没有提前 archive。

## 2. 核对最新基线

- [ ] 2.1 记录目标分支最新 commit。
- [ ] 2.2 记录隔离 Step Solver 的基线 commit 和完成 commit。
- [ ] 2.3 比较 KCC query 文件的并行修改。
- [ ] 2.4 比较 contact 和 ground report 文件的并行修改。
- [ ] 2.5 比较 configuration 和 identity 文件的并行修改。
- [ ] 2.6 比较 Motor 和 WorldSolver 文件的并行修改。
- [ ] 2.7 比较 Unity Solver Definition 和正式 KCC asset 的并行修改。
- [ ] 2.8 比较 current specs 与 `project.md` 的并行修改。
- [ ] 2.9 将隔离模块变基到最新唯一合同。
- [ ] 2.10 删除为旧基线保留的 adapter、兼容字段或重复模型。

## 3. 原子迁移 Step 配置

- [ ] 3.1 删除 `MinimumStepForwardDistance` runtime property。
- [ ] 3.2 删除旧 constructor 参数。
- [ ] 3.3 新增唯一 `MinimumStepDepth` runtime property。
- [ ] 3.4 新增唯一 constructor 参数。
- [ ] 3.5 将普通 movement progress 统一使用 `MinimumMovementDistance`。
- [ ] 3.6 将 Unity Solver Definition 字段改名为 `m_MinimumStepDepth`。
- [ ] 3.7 更新 Solver Definition 到 runtime configuration 的唯一映射。
- [ ] 3.8 更新 `CorinDeterministicKcc.asset` 的正式序列化字段和值。
- [ ] 3.9 将 `MinimumStepDepth` 纳入 configuration hash。
- [ ] 3.10 搜索并删除旧字段、旧参数和兼容读取。

## 4. 装配唯一 Step Solver

- [ ] 4.1 将隔离模块文件纳入现有 Deterministic KCC 程序集。
- [ ] 4.2 保持现有 asmdef，不新增第二程序集。
- [ ] 4.3 让每 Actor `DeterministicKccMotor` 持有或调用唯一 Step Solver。
- [ ] 4.4 复用 Motor 当前唯一 `DeterministicCapsuleQueries` 实例。
- [ ] 4.5 将 locked configuration 映射为唯一 Step policy。
- [ ] 4.6 确认 Step Solver 不注册 WorldSolver、descriptor、Composition 或 Session。

## 5. 替换 Step Up 链路

- [ ] 5.1 在 continuous cast 得到最早 TOI contact set 后建立 Step Up request。
- [ ] 5.2 将 safe position、remaining、previous support 和 canonical contacts传给 Step Solver。
- [ ] 5.3 Step Up rejection 时从原 safe position 与原 contact set 继续普通 slide。
- [ ] 5.4 Step Up success 时原子提交 accepted pose 和 stable report。
- [ ] 5.5 从 Motor remaining 中移除 candidate consumed planar displacement。
- [ ] 5.6 将未消费 remaining 交回同一 collide-and-slide iteration。
- [ ] 5.7 在新 support 上重建唯一 constraint plane。
- [ ] 5.8 保持后续墙面、第二级台阶和内角继续由同一 Motor 循环处理。
- [ ] 5.9 保持 Step Up 不写入或推导 `VerticalVelocity`。

## 6. 接入台阶鼻部 Grounding

- [ ] 6.1 在 edge/step feature 分支调用唯一 Step Support Evaluator。
- [ ] 6.2 将 current ground query 输入映射到 evaluator request。
- [ ] 6.3 outer 非稳定且 inner 稳定时采用 inner 顶部法线和 identity。
- [ ] 6.4 保留明确 ledge state。
- [ ] 6.5 使用 previous support 与 movement direction保持跨鼻部连续性。
- [ ] 6.6 角色朝空侧离开或 inner 证据失效时取消 stable support。
- [ ] 6.7 普通稳定 face 和双稳定 seam 保持现有快速路径。
- [ ] 6.8 删除顶部/立面共享边无条件 `UnsupportedEdge` 分支。

## 7. 拆分 Ground Snap 与 Step Down

- [ ] 7.1 保持 Ground Snap 只查询 `GroundSnapDistance`。
- [ ] 7.2 保持 Ground Snap 受 previous support 和非向上 request 约束。
- [ ] 7.3 删除 Ground Snap 成功后按位置 Y 差写入 `SteppedDown`。
- [ ] 7.4 Ground Snap 成功时直接使用 Snap 结果，不尝试 Step Down。
- [ ] 7.5 Ground Snap 失败后建立唯一 Step Down request。
- [ ] 7.6 将 movement position、request、previous support 和 Snap 结果传给 Step Solver。
- [ ] 7.7 Step Down success 时原子提交 landing pose、Below 和 stable report。
- [ ] 7.8 Step Down rejection 时保留常规 movement pose 并报告 Airborne。
- [ ] 7.9 保持 Step Down 不写入或推导 `VerticalVelocity`。

## 8. 删除旧台阶实现

- [ ] 8.1 删除 Motor 旧 `TryStep` 调用。
- [ ] 8.2 删除旧 `TryStep` 方法。
- [ ] 8.3 删除整体上抬 `MaximumStepHeight` 的旧候选路径。
- [ ] 8.4 删除旧完整 forward remaining 消费。
- [ ] 8.5 删除旧 remaining 直接清零。
- [ ] 8.6 删除 Ground Snap 事后伪造 `SteppedDown` 的路径。
- [ ] 8.7 删除重复 blocker、landing 或 support helper。
- [ ] 8.8 搜索并确认运行时只剩唯一 Step Solver。

## 9. 接入结构化诊断

- [ ] 9.1 将 Step phase 写入 Motor result diagnostics。
- [ ] 9.2 将唯一 rejection 写入 Motor result diagnostics。
- [ ] 9.3 写入 blocker primitive/feature。
- [ ] 9.4 写入 outer/inner/final landing identity。
- [ ] 9.5 写入 actual height。
- [ ] 9.6 写入 consumed planar progress。
- [ ] 9.7 保持 diagnostics 不进入 snapshot 或 hash。
- [ ] 9.8 保持 diagnostics 不改变 contact 排序和 Fixed 分支。

## 10. 升级身份

- [ ] 10.1 升级 `DeterministicKccConfiguration.MotorSemanticVersion`。
- [ ] 10.2 升级 `DeterministicKccWorldSolver.SolverVersion`。
- [ ] 10.3 升级 KCC identity schema。
- [ ] 10.4 将 `MinimumStepDepth` 和全部正式配置纳入 identity。
- [ ] 10.5 将新 Step 算法 semantic version 纳入 identity。
- [ ] 10.6 删除旧 Motor version、Solver version 和 identity 口径。
- [ ] 10.7 确认旧 snapshot、replay 和 endpoint identity 被明确拒绝。

## 11. 收口正式文档

- [ ] 11.1 更新 current `deterministic-kcc-world-solver` 的 blocker-linked Step Up requirement。
- [ ] 11.2 更新 current spec 的独立 Step Down requirement。
- [ ] 11.3 更新 current spec 的台阶鼻部 Grounding requirement。
- [ ] 11.4 更新 current spec 的 Ground Snap 单一职责。
- [ ] 11.5 更新 `openspec/project.md` 的 Fixed KCC 当前能力描述。
- [ ] 11.6 删除 current 文档中旧整段 raise/forward/down 和伪 Step Down 口径。

## 12. 确认唯一最终链路

- [ ] 12.1 搜索并确认没有旧 `TryStep`。
- [ ] 12.2 搜索并确认没有 `MinimumStepForwardDistance`。
- [ ] 12.3 搜索并确认没有 runtime Step 开关。
- [ ] 12.4 搜索并确认没有第二个 KCC、Solver descriptor 或配置资产。
- [ ] 12.5 搜索并确认没有兼容 reader、adapter 或 fallback。
- [ ] 12.6 确认隔离模块全部由当前唯一 Motor 链路使用。
- [ ] 12.7 运行 `openspec validate rebuild-deterministic-kcc-step-solving --strict --no-interactive`。
- [ ] 12.8 运行 `openspec validate integrate-deterministic-kcc-step-solving --strict --no-interactive`。
- [ ] 12.9 在用户完成端到端验收并明确 archive 后归档两个 change。
