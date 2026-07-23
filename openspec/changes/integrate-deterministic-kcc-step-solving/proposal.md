# Change: 原子接入 Deterministic KCC 台阶求解

## Why

`rebuild-deterministic-kcc-step-solving` 允许在隔离工作树中并行完成新的 Step Solver，但刻意不修改当前 Motor、WorldSolver、配置资产或闭环管线。若缺少单独的接入 change，新模块可能以未使用代码进入仓库，或者旧 `TryStep`、新 Step Solver、旧字段和新字段形成半迁移状态。

本 change 只负责用户完成当前闭环并明确放行后的原子切换：把已经完成的唯一 Step Solver 接入现有 Fixed KCC，迁移正式配置，删除旧算法，升级 identity，并让 current spec 与真实运行时一致。它不再设计第二套楼梯算法。

## What Changes

- 以用户明确放行为接入门禁；门禁前不实施本 change。
- 将隔离 Step Solver 工作重放或变基到最新闭环基线，先核对 query、contact、ground report 和 configuration contract。
- 将 `MinimumStepForwardDistance` 破坏性迁移为唯一 `MinimumStepDepth`，同步修改 runtime、Unity Solver Definition、正式 KCC asset 和 configuration hash。
- 在现有每 Actor 唯一 `DeterministicKccMotor` 内装配唯一 Step Solver。
- 用 blocker-linked Step Up 替换旧 `TryStep`，并让未消费 remaining 返回同一 collide-and-slide 循环。
- 让 Grounding 调用唯一 Step Support Evaluator，补齐台阶鼻部 support。
- 保持 Ground Snap 只负责 `GroundSnapDistance` 内微小贴地，在其失败后接入独立 Step Down。
- 删除旧 `TryStep`、整段 remaining 清零、事后 `SteppedDown` 标记和旧配置命名。
- 升级 Motor/Solver semantic version、KCC identity 和结构化诊断。
- 更新 current `deterministic-kcc-world-solver` spec 与 `project.md`。

## Activation Gate

- 用户必须明确说明当前闭环已完成并允许接入 KCC。
- `rebuild-deterministic-kcc-step-solving` 的隔离实现必须已经完成，但尚未单独合入或 archive。
- 必须先检查最新基线是否修改了 KCC query、contact、ground report、configuration、Motor 或正式资产。
- 若接口冲突导致需要兼容 adapter、旧字段 reader、runtime flag 或第二条调用链，停止并重新调整唯一接口，不绕过。
- 接入必须作为一个完整提交序列进入目标分支；不得先合入未使用模块，也不得先删除旧链路。

## Non-Goals

- 不重新设计 Step Up、Step Down 或台阶鼻部算法。
- 不实现跳跃、翻越、攀爬、Motion Warp、动作输入或场景 Stair 标签。
- 不增加第二个 KCC、Solver descriptor、配置资产、运行时选择、fallback 或兼容读取。
- 不修改 Fixed Program ABI、VerticalVelocity、Actor contact 业务规则或 Presentation Foot Placement。
- 不新增自动化测试；用户继续负责 Unity 端到端验收。

## Dependencies

- 强依赖 `rebuild-deterministic-kcc-step-solving` 的唯一 Step Solver 模块、candidate contract、support evaluator 和 diagnostics。
- 强依赖用户对当前闭环完成并允许接入的明确确认。
- 依赖最新 current `deterministic-kcc-world-solver`、Fixed KCC runtime、Unity Solver Definition 和正式 `CorinDeterministicKcc.asset`。

## Current Spec Comparison

- current `deterministic-kcc-world-solver` 的 Step requirement 没有要求 blocker 关联、真实高度、踏面深度、remaining 保留和正式 Step Down。本 change 修改该 requirement。
- current Grounding requirement 对顶部/立面共享边缺少 inner/outer support 证据。本 change修改该 requirement。
- current Ground Snap requirement 限制在 `GroundSnapDistance`，但现有实现仍用位置差事后标记 Step Down。本 change保留 Snap 范围并删除伪 Step Down。
- `character-vertical-body-motion` 要求 KCC 不私有积分 `VerticalVelocity`。本 change保持该边界。
- `character-motion-simulation-boundary` 要求每 Step 只有一次 WorldSolver mutation。本 change只替换唯一 Motor 内部策略，不增加第二次 world solve。
- `project.md` 对当前 Fixed KCC 的 Step Down 能力存在过度描述；接入完成后按真实链路修正。

## Impact

- Runtime：现有 `DeterministicKccMotor`、Grounding、Step diagnostics、configuration、Solver/KCC identity。
- Unity authoring：Solver Definition 字段和正式 `CorinDeterministicKcc.asset` 单路迁移。
- Network：KCC identity 与 Solver version 改变，旧 snapshot/replay/endpoint 与新运行时不兼容；协议结构不变。
- 文档：current `deterministic-kcc-world-solver` spec 与 `project.md` 更新。
- 交付：两个 change 在同一原子接入结果中闭合，最终仓库只保留新 Step Solver。
