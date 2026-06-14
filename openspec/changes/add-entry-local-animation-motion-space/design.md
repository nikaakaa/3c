# EntryLocal 动画运动坐标空间设计

## Context

当前 profile 烘焙工具对 root 位移做的是累计曲线：

- `cumulativeLocalX/Z(t)` 保存动画从起点到时间 t 的平面偏移。
- runtime sampler 用 `current - previous` 得到本 tick delta。
- root yaw 同样作为累计 yaw 曲线，runtime 用差分得到本 tick yaw delta。

这条链路本身是对的，问题出在 runtime 的 delta space。烘焙工具用初始基准计算 local offset，`RunEnd` 这类小角度动画按当前 root local 解释通常看不出问题；但 TurnBack 一边旋转一边位移，executor 在每个 tick 用当前 root rotation 解释 local delta，等于把同一条 profile path 放进不断变化的坐标系里。

因此修正点不是把 profile 改成世界空间，也不是让 Animator runtime root motion 接管，而是把 profile translation 的解释基准显式化。

## Goals

- 给 sampled animation planar delta 增加正式坐标空间语义。
- `EntryLocal` 表示“相对状态/动作进入瞬间捕获的固定平面基准”。
- TurnBack 的 profile translation 使用 `EntryLocal`，避免当前 root yaw 改变 translation 解释方向。
- `EntryLocal` 基准必须是纯数据，可进入状态机 restore state、movement facts、movement command 和日志。
- yaw 和 translation 保持分离：yaw 继续按 profile delta 应用到 root，translation 使用声明的 planar basis 转成 world delta。
- 不新增绕过现有 motion executor 的路径。

## Non-Goals

- 不把 profile 曲线改成绝对世界曲线。
- 不让 profile 采样器依赖 Transform、Animator、AnimancerState 或 CharacterController。
- 不把 TurnBack locked input direction 当成唯一合法基准；基准来源必须在进入状态时清晰捕获。
- 不在本变更里定义 target-relative、hit-relative 或 enemy-relative 动作位移。

## Decisions

### Decision: 增加 `EntryLocal` 作为第三种 planar delta space

现有语义保留：

- `Local`：delta 按执行时当前 motion root 朝向解释，适合必须跟随当前朝向的小段或持续移动。
- `World`：delta 已经是世界空间，executor 不再旋转它。
- `EntryLocal`：delta 按进入状态/动作时捕获的固定平面 forward/right 解释，适合 authored root-motion profile。

`EntryLocal` 不等于世界空间。它仍然是资源 local 曲线，只是运行时把 local X/Z 映射到“动作进入瞬间”的世界平面基准上。

### Decision: TurnBack 的进入基准使用进入状态时的角色 facing

TurnBack profile 的 local Z 代表动画母带相对自身初始朝向的前后路径。TurnBack 运行时应捕获进入状态前角色实际 facing 作为 entry basis，而不是每 tick 的当前 root facing。

这样 profile 的 yaw 可以把角色转过去，profile 的 translation 仍沿进入时的 authored path 走，不会因为 root yaw 累积而倒着解释。

### Decision: 基准进入 rollback restore state

预测/回滚不能靠当前 Transform 临时推导 EntryLocal 基准。进入 TurnBack 或后续动作状态时，状态机必须捕获 entry basis，并在 snapshot/restore 后恢复；重放同一输入序列时，movement facts 必须拿到同一基准。

如果缺少有效 basis，系统必须输出诊断并按无贡献或配置错误处理，不得静默改用当前 root local。

### Decision: Profile sampler 仍保持纯数据

`AnimationMotionProfileSampler` 继续只输出 local delta 和 yaw delta，不负责空间转换。空间转换属于 movement facts/command 到 motion executor 的边界。

这能避免 sampler 读取场景对象，也让同一 profile delta 可被 `Local`、`World` 或 `EntryLocal` 的不同 policy 复用。

### Decision: 诊断必须暴露 basis

TurnBack 排查需要看到完整链路：

- sampled local delta。
- delta space。
- entry basis forward/right。
- resolved world delta。
- yaw before/input/animation after。
- actual root delta。

这些日志用于验证方向问题，不删除现有 TurnBack 诊断 channel。

## Risks / Trade-offs

- 风险：已有测试把 TurnBack 断言为 `Local`，实现后需要更新为 `EntryLocal`。
  - Mitigation: 只改 TurnBack/profile-authored 状态，保留 RunEnd 等现有 `Local` 行为。
- 风险：进入基准捕获时机错误会导致回滚重放不一致。
  - Mitigation: 增加状态机 restore 测试和本地回滚重放测试。
- 风险：TurnBack timeline 提前 exit 会让修正后的 profile 仍只跑半段。
  - Mitigation: 手动验证同时检查 motion window、exit window 和 animation normalized time；窗口调整走已有 timeline 配置，不新增路径。

## Migration Plan

1. 在 motion facts/command 中补齐 `EntryLocal` delta space 和 entry planar basis 数据。
2. 在统一状态机进入 TurnBack 时捕获 entry basis，并纳入 restore state。
3. TurnBack sampled profile translation 标记为 `EntryLocal`，并传递 entry basis。
4. motion executor 按 `EntryLocal` 使用固定 basis 解析 world delta，`Local`/`World` 保持原语义。
5. 更新测试锁定 TurnBack 使用 `EntryLocal`，executor 三种 space 行为明确。
6. 用日志和手动验证确认 TurnBack 不再倒向位移，并且没有 `OnAnimatorMove` pending 参与。

## Open Questions

- 后续 Attack/Dodge 是否需要同一字段直接复用，还是在 Action movement policy 中暴露同名语义；本变更只要求抽象可复用，不一次性迁移。
