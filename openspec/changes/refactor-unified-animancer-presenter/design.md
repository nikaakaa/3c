## Context
当前 FullBody 状态权威已经集中到 `CharacterStateMachineRunner`，但表现层仍存在两套正式 Animancer runtime adapter：

- `BasicLocomotionAnimancerPresenter`：消费 `MovementAnimationContext`，解析 Locomotion alias，维护 Locomotion 播放进度和 TurnBack 相关 root motion policy。
- `ActionAnimationAnimancerPresenter`：消费 `CharacterStateAnimationRequest`，按 Action key 播放动作动画，维护 Action 播放进度和 clear 语义。

两者在同一个视觉根上各自持有 `AnimancerComponent`、当前 `AnimancerState`、当前 key、播放进度、restore 入口和 root motion policy。虽然它们没有形成第二状态机权威，但已经形成两个正式动画播放入口。后续攻击、受击、跳跃、翻滚加入后，如果沿用这种拆法，会继续按动作类型复制 Presenter。

## Goals
- 让当前角色 FullBody base layer 只有一个正式 Animancer 播放组件。
- 让 Locomotion 和 Action 都转换成同一种播放请求后进入统一 Presenter。
- 统一播放、clear、restore、当前动画名、normalized time、播放结束事实和 root motion policy 的持有位置。
- 保持 Locomotion 配置和 Action Profile 的数据归属分离。
- 保持状态机、movement executor、animation presenter 的抽象和实现分离。
- 保持现有 TurnBack、Dodge、RunEnd 和 rollback replay 语义可测试。

## Non-Goals
- 不改变统一状态机的 transition、owner 或 action arbitration 规则。
- 不把基础移动配置和动作动画 Profile 合并成一个大配置资产。
- 不新增并行 UpperBody、Additive、Facial、IK 或 AvatarMask layer。
- 不让 Animator root motion 或 Animancer callback 成为位移权威。
- 不新增 fallback 配置、Resources 查找或全局单例播放表。
- 不运行 Unity batchmode。

## Decisions
- Decision: 引入一个正式 FullBody base layer Animancer Presenter。
  - Reason: Present 的本质是把统一状态机和 Character frame output phase 的动画请求发给 Animancer，并把播放进度转成纯数据事实。这个职责不需要按 Locomotion/Action 拆成两个正式 runtime owner。
- Decision: Locomotion 和 Action 先在 submission/adapter 层转换为统一播放请求。
  - Reason: `phase + gait`、foot phase match、TurnBack start time、Action key 这些是请求构建语义，不应该让两个 Presenter 各自决定播放入口。
- Decision: 统一 Presenter 暴露一个聚合播放快照。
  - Reason: Runtime blackboard 仍可保存 Locomotion progress 和 Action progress 两类事实，但事实来源必须来自同一个 Presenter 的只读快照，而不是两个播放组件。
- Decision: 旧 Presenter 类型不能作为正式双播放路径保留。
  - Reason: facade 包住两个 Presenter 仍然是分裂路径。允许短期迁移桥，但静态测试必须证明正式 prefab/scene 不再同时挂载旧两个播放组件。
- Decision: 保留配置边界，不合并数据资产。
  - Reason: 统一的是 runtime 播放入口，不是把 Locomotion alias、motion profile、Action Profile 和状态机配置揉成一个资产。

## Risks / Trade-offs
- Risk: 现有测试大量直接创建 `BasicLocomotionAnimancerPresenter` 或 `ActionAnimationAnimancerPresenter`。
  - Mitigation: 先加统一 Presenter 行为测试，再逐组迁移测试 fixture；旧类型只作为兼容桥时必须明确标记且不进入正式装配。
- Risk: TurnBack / RunEnd 的 restore 行为被重构破坏。
  - Mitigation: 保留并迁移播放进度 restore 测试，覆盖 same alias resume、不重启、Action clear 后 Locomotion 继续播放。
- Risk: root motion policy 从两个组件迁到一个组件时覆盖顺序变化。
  - Mitigation: 新增测试覆盖 Locomotion TurnBack force、Action disable、Action 清理后 stale policy 清除。
- Risk: `PlayerLocomotionController` 现在直接持有 `BasicLocomotionAnimancerPresenter` 类型。
  - Mitigation: 先收窄为接口或统一 Presenter 引用，再迁移自动发现逻辑，最后更新 prefab。
- Risk: 活跃变更 `formalize-animation-playback-rollback-authority` 同时修改播放进度 restore。
  - Mitigation: 实施前先重读该变更；本变更不得覆盖其 restore 权威，统一 Presenter 必须兼容其 snapshot/restore 语义。

## Migration Plan
1. 增加静态测试，锁定正式 prefab/scene 不得同时挂 `BasicLocomotionAnimancerPresenter` 和 `ActionAnimationAnimancerPresenter`。
2. 定义统一播放请求和播放快照模型，覆盖 Locomotion 和 Action 所需字段。
3. 建立统一 Presenter 的最小行为测试。
4. 将 Character output applier 的动画提交改为统一 Presenter 一处入口。
5. 将 Locomotion adapter 的 presenter 字段、自动发现和播放进度恢复迁到统一接口。
6. 将 Action animation presenter 的播放、clear、restore 和 facts 迁到统一 Presenter。
7. 迁移 prefab/scene，只保留一个正式 Animancer Presenter 组件。
8. 删除或降级旧 Presenter 类型和测试引用，保留必要兼容桥时加静态边界测试。
9. 运行定向 EditMode 测试、dotnet build 和 OpenSpec 校验。

## Open Questions
- 无。默认目标是单一正式 FullBody base layer Animancer Presenter；如果实现阶段发现必须保留两个正式播放组件，应停止并重新审批。
