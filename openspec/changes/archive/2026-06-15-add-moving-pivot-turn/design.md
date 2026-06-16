# 逻辑状态 TurnBack 与 Root Motion 设计

## Context

参考工程把移动反向急转当作移动状态里的 `TurnRun/TurnBack`，不是 `TurnInPlace`。进入该状态后，动画关键前段不再执行普通输入旋转，并通过 Animator root motion 将动画位移交给 `CharacterController`。

当前工程的问题来自运动权威混杂：旧 `MovingPivotTurn` 用状态外 plan、配置 selector 和 baked profile 采样 yaw/local delta，同时普通输入旋转/位移仍可能参与。旧 `TurnInPlace` 又把无移动输入的原地转身和移动急转混在邻近概念里，导致排查时很难判断到底是谁在转角色。

## Decisions

- Decision: 先删除旧 `TurnInPlace` 和 `MovingPivotTurn` 运行系统。
  - Reason: 它们已经和目标方向相反，继续保留会让后续 root motion TurnBack 出现并行路径。
- Decision: 不再用 baked yaw/profile 作为 TurnBack 转身权威。
  - Reason: 当前过转/偏移很可能来自视觉动画、baked yaw 和代码旋转重复贡献。
- Decision: TurnBack 后续必须进入统一逻辑状态机。
  - Reason: 播放窗口、输入锁定、退出和回滚都应该有明确状态承载。
- Decision: root motion 不能直接绕过统一运动出口。
  - Reason: 项目已有 movement executor、回滚状态和诊断日志，动画 delta 应作为 command/facts 进入同一出口。
- Decision: 保留普通 animation-motion executor 日志。
  - Reason: 后续还需要诊断 root motion delta、输入旋转/位移是否被锁定。

## Migration Plan

1. 删除旧 TurnInPlace/MovingPivot 的代码入口、配置类型、selector、状态机节点、测试和配置资产。
2. 保证普通 locomotion/action/rollback 测试仍能通过。
3. 在后续任务中新增 `TurnBack` 或等价移动急转逻辑状态。
4. 在该状态中播放 `Locomotion.Turn.Back`，窗口内禁止普通输入位移和旋转。
5. 收集 Animator/Animancer root motion delta 并通过统一 motion executor 应用。

## Risks

- 删除旧配置字段后 prefab/asset 可能出现丢失序列化字段；这是预期迁移，后续 root motion 状态会重新绑定需要的 TurnBack 资源。
- 当前阶段只完成旧系统删除，Sandbox 不会立刻拥有正确 TurnBack 手感；这是为了先把错误路径清干净。
