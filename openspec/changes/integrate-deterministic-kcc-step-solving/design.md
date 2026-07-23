## Context

接入前存在两个不同位置的事实，但不存在两个运行时：

```text
当前目标分支
    current Motor
    old TryStep
    current Grounding / Ground Snap

隔离工作树
    completed DeterministicKccStepSolver
    StepCandidate / StepRejection
    Step Support Evaluator
```

隔离模块不能先进入目标分支。接入 change 的职责是把目标分支从旧链路一次性变成新链路，并清理所有旧语义。

## Goals

- 门禁前对当前闭环零影响。
- 门禁后只接入一个已经完成的 Step Solver，不重新复制算法。
- 配置、运行时、资产、identity、诊断和文档在同一次切换中一致。
- 切换后的仓库没有旧 `TryStep`、旧字段、旧诊断或运行时选择。
- 最新闭环若改变 KCC 接口，先收敛唯一接口，再接入。

## Final Architecture

```text
DeterministicKccWorldSolver
    |
    v
DeterministicKccMotor
    +-- DeterministicCapsuleQueries
    +-- DeterministicKccStepSolver
    |      +-- blocker-linked Step Up
    |      +-- atomic Step Down
    |      +-- Step Support Evaluator
    +-- Grounding
    +-- Ground Snap
    +-- Multi-plane collide-and-slide
```

`DeterministicKccWorldSolver`、每 Actor Motor、query 实例和 configuration 仍各自唯一。Step Solver 是 Motor 内部策略模块，不注册新的 WorldSolver，也不拥有 world state。

## Decisions

### 1. 接入由用户明确放行

当前其它闭环仍在运行时，本 change 不实施。只有用户明确说明可以接入，才读取最新基线、检查重叠并开始迁移。

业务收益：KCC 不会打断正在收口的角色链路，也不会让序列化资产和运行时代码提前失配。

代价：隔离模块完成后会暂时停留在工作树或提交中，不能单独合入。

### 2. 隔离实现先变基，不写兼容桥

接入前比较基线 commit 与最新目标分支，逐项核对 query、contact、ground report、configuration、Motor 和 asset。若接口已经变化，直接修改隔离模块适配最新唯一合同。

业务收益：最终仓库只有一套清楚接口，后续维护者不需要理解临时 adapter。

代价：闭环期间改动越大，接入时需要重新核对和调整更多模块代码。

### 3. 配置迁移和 Motor 切换同批发生

`MinimumStepForwardDistance` 在接入时同时从 runtime property、constructor、Unity serialized field、正式 asset、configuration hash 和 KCC identity 删除，唯一替换为 `MinimumStepDepth`。新 Step Solver 与新字段同批接入。

业务收益：任何可运行版本都不会出现新算法读取旧语义或旧算法读取新语义。

代价：旧资产、snapshot、replay 和 endpoint 明确失效，不提供兼容读取。

### 4. Motor 内部按一个固定顺序接入

最终顺序为：

```text
requested displacement
    -> stable ground projection
    -> continuous cast
    -> blocker-linked Step Up candidate
    -> continue remaining collide-and-slide
    -> Grounding with Step Support Evaluator
    -> Ground Snap within GroundSnapDistance
    -> Step Down within MaximumStepHeight
    -> Finalize
```

Step Up 失败继续使用原 contact set 做普通 slide。Ground Snap 成功不执行 Step Down；Snap 失败且满足资格才尝试 Step Down。两种 Step 成功都只提交完整 candidate。

业务收益：上台阶、贴地和下台阶各自只有一个明确职责，诊断可以对应真实求解阶段。

代价：Motor 的 movement loop、Grounding 和 post-movement 阶段需要在同次接入中一起修改，不能拆成独立上线步骤。

### 5. 旧链路在接入提交内直接删除

接入新调用后立即删除旧 `TryStep`、raise/full-forward/down 实现、remaining 清零和 Ground Snap 后按 Y 差标记 `SteppedDown`。不保留开关、旧方法、包装器或注释掉的代码。

业务收益：最终只有一个算法真相，不会因配置或执行环境走到不同路径。

代价：切换不能靠运行时回退；若验收发现问题，应继续修正新唯一实现。

### 6. Identity 表达破坏性算法变化

Motor semantic version、WorldSolver version、configuration hash 和 KCC identity 在代码与资产完成切换后统一升级。step diagnostics 输出 blocker、landing、actual height、consumed progress 和 rejection phase，但 diagnostics 不进入 snapshot/hash。

业务收益：旧算法状态不会被新算法误读，回滚和重放的失败是明确版本不兼容，不是静默漂移。

代价：需要重新生成使用旧 identity 的正式运行数据。

## Atomic Cutover Sequence

1. 获得用户接入放行。
2. 记录目标分支最新 commit 和隔离实现 commit。
3. 检查所有 KCC 重叠文件和合同变化。
4. 将隔离模块变基到最新目标分支并收敛唯一接口。
5. 迁移 runtime configuration、Unity serialized field 和正式 asset。
6. 在现有 Motor 装配唯一 Step Solver。
7. 接入 blocker-linked Step Up 和 remaining continuation。
8. 接入 Step Support Evaluator。
9. 将 Ground Snap 与 Step Down 分成固定顺序。
10. 删除全部旧 Step 路径和旧配置命名。
11. 升级 version、identity 和 diagnostics。
12. 更新 current spec 与 `project.md`。
13. 搜索确认没有双实现、旧字段、兼容读取或未使用替代模块。

这些步骤属于一个接入 change，不允许在步骤 5 到 12 的半迁移状态停止并合入。

## Failure Policy

- 用户未放行：不实施。
- 隔离模块未完成：不接入。
- 最新基线与模块合同冲突：停止接入，先修改模块使用最新唯一合同。
- 需要 runtime flag、adapter、兼容字段或第二配置才能继续：停止，不建立绕行路径。
- 接入后 Step candidate 失败：按最终 spec 的普通 slide 或 Airborne 语义处理，不回退旧算法。
- 不回退 Unity Physics、CharacterController、隐藏 ramp、旧 `TryStep` 或放大 Ground Snap。

## Final State

- 当前 Motor 只调用唯一 `DeterministicKccStepSolver`。
- Grounding 只使用唯一 Step Support Evaluator。
- Ground Snap 与 Step Down 是两个固定阶段。
- 只存在 `MinimumStepDepth`，不存在旧字段 reader。
- 旧 `TryStep` 和事后 `SteppedDown` 标记已删除。
- Solver/KCC identity 与新算法一致。
- 两个 OpenSpec change 在完整接入后一起具备归档条件。
