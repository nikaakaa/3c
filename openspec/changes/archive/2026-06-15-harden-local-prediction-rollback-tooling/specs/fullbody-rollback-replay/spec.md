## ADDED Requirements
### Requirement: FullBody 可回滚状态完整性审计
系统 MUST 审计并覆盖 FullBody replay 中所有会影响下一 tick 输出的纯数据状态。至少包括 FullBody active state、state time、pending transition、input buffer restore state、locomotion runtime state、runtime blackboard、animation facts、profile sampling window、motion root pose 和 camera basis override。

#### Scenario: 捕获影响下一 Tick 的状态
- **WHEN** tick N 的 `CharacterSimulationSnapshot` 被创建
- **THEN** 快照 MUST 包含或可确定性重建 tick N+1 推进所需的 FullBody、Locomotion、action、animation 和 motion facts
- **AND** 快照 MUST NOT 保存 Unity Object、Animator、Animancer state 或场景实例引用

#### Scenario: 恢复后下一 Tick 一致
- **GIVEN** tick N 的快照已恢复
- **AND** tick N+1 的输入帧相同
- **WHEN** replay 推进 tick N+1
- **THEN** action state、locomotion state、runtime blackboard、motion root pose 和 animation facts MUST 与原始运行在容差内一致

#### Scenario: 缺失状态可诊断
- **GIVEN** 某个影响下一 tick 的事实没有进入快照也不能由输入重建
- **WHEN** replay 出现 first mismatch
- **THEN** differences MUST 指向对应字段类别
- **AND** 工具 MUST NOT 只输出笼统的 snapshot mismatch

### Requirement: Profile 驱动动画状态可严格重放
系统 MUST 能用严格 synctest 验证 profile 驱动的 FullBody/Locomotion 动画状态。TurnBack EntryLocal 或等价 profile 驱动状态 MUST 通过正式 FullBody replay 主线恢复、重放和比较，不得在测试中直接绕过主线采样器。

#### Scenario: TurnBack EntryLocal 重放一致
- **GIVEN** TurnBack 使用 profile 采样产生位移和 yaw
- **AND** replay 从 TurnBack 中间 tick 恢复
- **WHEN** 严格 synctest 重放到 end tick
- **THEN** first mismatch MUST 为空
- **AND** end tick 的 position、yaw、state、runtime blackboard 和 animation facts MUST 一致

#### Scenario: Profile 采样窗口被恢复
- **GIVEN** profile 采样依赖 previous normalized time 和 current normalized time
- **WHEN** replay 从历史 tick 恢复
- **THEN** replay 使用的采样窗口 MUST 与原始运行一致
- **AND** 采样 delta MUST 不因表现层当前播放时间改变

#### Scenario: 测试不绕过正式采样路径
- **WHEN** 自动测试验证 profile 驱动状态
- **THEN** 测试 MUST 通过 `FullBodyRollbackSimulation` 或等价 FullBody replay adapter 推进
- **AND** MUST NOT 直接调用底层 pipeline 或直接写 motion root 来制造通过结果

### Requirement: 动画变体和混合事实确定性
系统 MUST 将影响 rollback 结果的动画变体、转身方向、左右脚起步选择、motion space 和混合权重视为确定性事实。它们 MUST 由 tick 输入和配置确定性推导，或进入纯数据快照进行 capture/restore。

#### Scenario: 变体选择可重建
- **GIVEN** tick N 的状态选择了某个动画 variant
- **WHEN** replay 从 tick N-1 恢复并使用同一输入推进到 tick N
- **THEN** replay MUST 选择同一 variant
- **AND** 如果选择不同，first mismatch differences MUST 标记 variant 或 animation facts

#### Scenario: 混合权重影响采样时进入验收
- **GIVEN** 某个动画混合权重会影响 profile delta、yaw 或动作事实
- **WHEN** 该状态进入 rollback 验收
- **THEN** 该权重 MUST 由确定性数据恢复或重建
- **AND** strict synctest MUST 能检测权重不同导致的 replay 分叉

#### Scenario: 表现层混合不参与确定性验收
- **GIVEN** 某个 Animancer/Animator blend 只影响视觉，不影响 simulation tick 的 position、yaw、action 或 blackboard facts
- **WHEN** strict synctest 运行
- **THEN** 该表现层 blend MAY 不进入 simulation snapshot
- **AND** 它 MUST NOT 反向驱动 rollback core

### Requirement: AnimatorDirect 不作为回滚验收基准
系统 MUST 将 Unity Animator runtime delta 视为非确定性表现/兼容来源，不能作为预测回滚严格验收的唯一基准。需要回滚的动画位移 MUST 通过 tick 驱动 profile、纯数据曲线或等价确定性 motion source 验收。

#### Scenario: 严格测试拒绝 AnimatorDirect 作为唯一来源
- **GIVEN** 某状态只依赖 `OnAnimatorMove` 的 runtime delta 推进
- **WHEN** 该状态被纳入预测回滚严格验收
- **THEN** 测试 MUST 标记该状态缺少确定性 motion source
- **AND** MUST 要求提供 profile/曲线/纯数据采样或明确排除该状态的回滚承诺

#### Scenario: AnimatorDirect 作为表现兼容保留
- **GIVEN** 某动画只能暂时通过 AnimatorDirect 播放
- **WHEN** 它不参与预测回滚验收
- **THEN** 系统 MAY 保留该模式作为正式配置的非回滚能力
- **AND** 文档 MUST 标明它不提供本地预测确定性保证
