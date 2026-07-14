## MODIFIED Requirements

### Requirement: ActionRuntime 必须是动作事务层而不是执行编排层
系统 MUST 让 `ActionRuntime` 只负责 activate、confirm、reject、cancel、end 等动作实例生命周期。`ActionRuntime` MUST NOT tick Graph、播放 Timeline、采样 Timeline、提交 Motion、播放 Cue 或裁决 gameplay result。

#### Scenario: 动作启动成功
- **WHEN** `ActionRuntime` 接受一个 start request
- **THEN** 它 MUST 只创建和记录 `ActionInstance`
- **AND** 后续执行流程 MUST 仍由 Graph/BTSMTL、TimelineStage、MotionStage、PresentationStage 和 GameplayResult resolver 完成

#### Scenario: 动作取消
- **WHEN** 当前 action instance 被取消
- **THEN** `ActionRuntime` MUST 更新实例 state
- **AND** Graph 或 Pipeline 后续 stage MUST 决定如何停止 Timeline、修正表现或输出 correction

### Requirement: Graph 必须通过运行时 action scope 关联动作输出
系统 MUST 通过运行时 action scope 将 Graph、Timeline、Motion、GameplayResult 和 Presentation 产出的动作输出关联到 `ActionInstance`。系统 MUST NOT 维护静态 node membership table 来记录哪些节点属于某个 action 或 ability。

#### Scenario: 进入 action scope
- **WHEN** Graph 提交 `ActionActivationRequest` 并得到 instance id
- **THEN** 后续由该流程提交的 Timeline request、window sample、motion sample、cue event 或 gameplay result MAY 关联该 instance id
- **AND** 关联 MUST 来自运行时上下文或显式参数，而不是静态节点归属表

#### Scenario: 离开 action scope
- **WHEN** Graph 提交 `ActionEndRequest` 或 action instance 被取消
- **THEN** 该 action scope MUST 关闭
- **AND** 后续普通 locomotion、gameplay result 或表现输出 MUST NOT 自动继承旧 instance id
