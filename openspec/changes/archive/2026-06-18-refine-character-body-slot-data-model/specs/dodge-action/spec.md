## MODIFIED Requirements

### Requirement: Dodge 属于 Action domain
系统 MUST 将 Shift Dodge 视为 Action domain 中的全身动作。Dodge lifecycle MAY 使用 action instance、timeline 或局部 FSM/HFSM；对外 MUST 输出 `Action.Dodge`、action facts、FullBody claim、motion candidate 和 animation candidate。FullBody claim MUST 只表示 Dodge 请求全身占用；claim 被采纳后的结果 MUST 是 Action-side owner 接管 `BaseSlot` 并压制冲突的 `UpperBodySlot`，不得表示 `FullBody` behavior node、`FullBody` runtime source、slot owner 或 Locomotion 父树。

基础 Locomotion MUST 保持为 sibling movement module，不得被迁移到 Action 内部。

#### Scenario: Dodge accepted 后进入 Action domain
- **GIVEN** input intent 提供 Shift Dodge 请求
- **WHEN** Action domain 接受 Dodge
- **THEN** 系统 MUST 创建或推进 `Action.Dodge` 的 lifecycle
- **AND** Dodge 输出 MUST 以 Action submission 参与本帧仲裁

#### Scenario: Locomotion 独立保持
- **WHEN** Dodge 处于 active lifecycle
- **THEN** Locomotion source 仍 MAY 计算基础移动 intent、facts 或移动候选
- **AND** 身体仲裁 MUST 根据 Dodge 的 FullBody claim 决定 `BaseSlot` 是否被 Action 接管

#### Scenario: 模块化不等于分裂运行时
- **WHEN** Dodge 需要 Action lifecycle、位移、动画、窗口或 cue
- **THEN** 这些输出 MUST 通过现有 CharacterFramePipeline / CharacterFramePlan / output applier 链路汇合
- **AND** 系统 MUST NOT 新增第二角色控制入口、第二 motion executor、第二 animation presenter 或第二 blackboard writer

#### Scenario: 当前阶段不引入并行动作表现层
- **WHEN** 需要讨论 FullBody、UpperBody、Facial 或 Additive 输出
- **THEN** 当前 Dodge MUST 只交付基础全身动作接管
- **AND** UpperBody、Facial 或 Additive runtime source MUST 通过单独 change 批准后再实现

#### Scenario: Dodge 不需要 FullBody 节点
- **WHEN** authoring graph、runtime branch 或 compiler 表达 Dodge
- **THEN** Dodge MAY 位于 CommittedAction branch、selector 或 ActionTimeline
- **AND** graph MUST NOT 要求存在名为 `FullBody` 的 gameplay 节点才能编译 `Action.Dodge`
