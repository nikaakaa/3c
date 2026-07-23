# Change: 构建隔离的 Deterministic KCC 台阶求解模块

## Why

当前 `DeterministicKccMotor` 已声明支持 `WorldFeature.Step`，但现有 `TryStep` 没有绑定触发阻挡的 contact、primitive 或 feature，下台阶也只是 Ground Snap 成功后的诊断标签。完整问题需要重建 Step Up、Step Down 和台阶鼻部 support。

用户当前仍在收口其它角色管线，不能让 KCC 改动提前进入现用 `DeterministicKccMotor`、正式 Solver Definition、Rollback 配置资产或 Session Composition。若把算法开发和管线切换放在同一执行清单里，并行实施时很容易出现旧字段已删除、Motor 尚未切换，或新模块已经合入但仍无人使用的半接入状态。

本 change 只负责在隔离分支或独立工作树中完成可供最终切换的 `DeterministicKccStepSolver` 模块、候选数据模型和纯查询算法。它不修改当前闭环分支，不接入现用 Motor，不迁移正式资产，也不能单独合并或归档。最终接入由依赖本 change 的 `integrate-deterministic-kcc-step-solving` 一次性完成。

## What Changes

- 定义独立的 `DeterministicKccStepSolver` 输入、输出、候选、拒绝原因和只读诊断。
- Step Up 只消费当前 continuous cast 提供的 canonical contact set，并选择真实闭合侧面 blocker。
- 先以 outer/inner 向下探测证明关联顶部和 `MinimumStepDepth`，再计算真实 `actualStepHeight`。
- 取得真实高度后，按该高度验证向上净空、受请求幅度约束的前移、落地关联和最终无重叠。
- Step Up candidate 明确返回实际消费的平面位移和未消费 remaining，不直接操作 Motor 循环。
- 定义可由 Step Solver 与后续 Grounding 共用的台阶鼻部 inner/outer support evaluator。
- Step Down 作为独立候选求解，使用 `MaximumStepHeight`，不借用或扩大 Ground Snap。
- 所有候选在成功前只读；成功只返回完整 candidate，不修改 world state、Motor pose、support 或 snapshot。
- 并行阶段只形成隔离实现提交或补丁，不注册到 `DeterministicKccMotor`、`DeterministicKccWorldSolver`、Composition 或正式配置资产。

## Parallel Delivery Boundary

- 允许现在实施：Step Solver 数据模型、纯查询算法、support evaluator、候选结果和模块内部诊断。
- 允许读取但不允许修改：当前 Motor 调用顺序、query contract、contact/ground report 和 configuration 字段，用于锁定接口。
- 不允许现在实施：替换旧 `TryStep`、修改 Ground Snap 调用顺序、迁移 `MinimumStepForwardDistance`、修改正式 KCC asset、升级 Solver/KCC identity、更新当前管线声明。
- 实施位置必须是隔离分支或独立工作树；不得把本 change 的未接入模块单独合入当前闭环分支。
- 本 change 完成表示“隔离实现已具备接入条件”，不表示仓库当前 KCC 行为已经改变，不能单独 archive。
- 不增加 runtime flag、feature toggle、第二个 Solver descriptor、第二份 KCC 配置或 fallback。

## Non-Goals

- 不接入当前 Gameplay、Fixed WorldSolver、Rollback、Session 或 Composition 管线。
- 不修改当前 `DeterministicKccMotor` 的运行时行为。
- 不迁移 Unity Solver Definition 和 `CorinDeterministicKcc.asset`。
- 不实现跳跃、翻越、攀爬、Motion Warp、动作类型或输入判断。
- 不把场景对象标记为 Stair，也不要求隐藏 ramp collider。
- 不实现 moving platform、动态刚体台阶、可破坏几何或任意方向重力。
- 不增加旧/新 Step 开关、fallback、兼容字段或第二份配置。
- 不新增自动化测试；用户继续负责 Unity 端到端验收。

## Dependencies

- 依赖当前已安装的 Fixed Q32.32 capsule query、canonical contact、stable ground report、triangle adjacency 和 overlap validation contract。
- 不依赖当前动画、Motion Matching、Agent authoring active changes，也不修改其文件。
- 后续 `integrate-deterministic-kcc-step-solving` MUST消费本 change 的唯一模块并原子替换旧链路，不能复制算法。

## Current Spec Comparison

- current `deterministic-kcc-world-solver` 描述的是已安装运行时行为。本 change 不修改它，因为并行阶段不会改变当前运行时。
- 新增 `deterministic-kcc-step-solver-module` capability，规定未接入模块的纯输入输出、确定性候选和无副作用边界。
- blocker-linked Step Up、独立 Step Down、台阶鼻部 support 和 Ground Snap 职责变更全部留给接入 change 修改 current `deterministic-kcc-world-solver`。
- `project.md` 继续描述当前已安装链路；只有接入 change 完成后才更新现行口径。

## Impact

- 并行工作树：新增 Step Solver 模块、候选数据类型、support evaluator 和模块诊断类型。
- 当前闭环分支：无文件变化、无配置变化、无运行时变化。
- Unity authoring：无变化。
- Network 与 snapshot：无变化。
- 后续接入：由 `integrate-deterministic-kcc-step-solving` 负责 Motor 切换、配置迁移、旧实现删除、identity 升级和 current spec 更新。
