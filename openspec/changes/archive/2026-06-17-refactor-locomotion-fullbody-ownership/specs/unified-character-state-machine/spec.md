## REMOVED Requirements

### Requirement: 统一层级逻辑状态机权威

**Reason**: 角色正式目标不再是一棵统一层级状态树，也不是 FullBody base layer 的单一 runner。

**Migration**: 使用领域状态 authority 和 Character frame pipeline 合成最终输出。

### Requirement: 通用 transition 配置

**Reason**: transition 配置不再要求 Locomotion、Dodge、Attack 等全部位于同一张跨领域状态图。

**Migration**: Locomotion transition 归 Locomotion module；Action 请求准入归 Action module；跨领域互斥归 Character frame plan。

### Requirement: 状态输出配置

**Reason**: 旧要求默认由统一状态机节点先决定所有输出。

**Migration**: 由领域 submitter 提交 candidate output，再由 Character frame pipeline 合成。

### Requirement: 删除分裂路径

**Reason**: 旧要求把删除分裂路径等同于只保留统一分层状态机，方向过窄。

**Migration**: 删除分裂路径的目标是保留唯一 Character frame pipeline，而不是保留唯一跨领域状态树。

### Requirement: 自研统一分层状态图运行时

**Reason**: 自研状态图可以保留为领域内 implementation，但不再定义为角色正式主线总状态机。

**Migration**: Locomotion 和复杂 Action MAY 复用自研状态图；外部 interface 使用领域 facts 和 frame submissions。

### Requirement: 状态机文档口径一致

**Reason**: 原文档口径要求继续声明自研统一分层状态机为正式主线。

**Migration**: 文档应声明 Character frame pipeline 是合成主线，状态图是可选领域实现。

### Requirement: TurnBack Locomotion 正式状态契约

**Reason**: TurnBack 仍可为 Locomotion 正式状态，但不应使用 FullBody path 或统一树作为契约。

**Migration**: 使用 `Locomotion.TurnBack` 和 Locomotion snapshot/facts。

### Requirement: 状态机通用模型与角色业务模型分层

**Reason**: 原要求仍围绕自研统一分层状态机建模。

**Migration**: 保留“抽象与业务分层”原则，但迁移到领域 module interface 和 Character frame pipeline。

## ADDED Requirements

### Requirement: 领域状态权威由角色帧管线协调

系统 MUST 允许 Locomotion、Action 和后续已审批领域各自维护领域状态 authority 或 action facts，但它们 MUST 通过同一个 `CharacterFramePipeline` 提交纯数据 facts、requests、candidate output 或 occupancy claim。最终 movement、animation、input consume 和 runtime facts 写入 MUST 由角色级 frame plan/output apply 决定。

#### Scenario: 多领域状态不形成多管线
- **GIVEN** Locomotion state 和 Action facts 同时存在
- **WHEN** 角色推进 tick N
- **THEN** 两者 MUST 通过同一个 Character frame pipeline 参与
- **AND** 系统 MUST NOT 为 Locomotion、Action 或 FullBody 分别创建独立 gameplay frame pipeline

#### Scenario: 领域之间通过 facts 交互
- **GIVEN** Action resolver 需要移动方向、gait 或 TurnBack facts
- **WHEN** 它解析动作请求
- **THEN** 它 MUST 读取 Locomotion 提交的纯数据 facts
- **AND** MUST NOT 直接调用 Locomotion transition API
- **AND** MUST NOT 读取 Locomotion controller 私有字段作为仲裁权威

### Requirement: 领域 ID 替代跨领域树路径

系统 MUST 使用稳定领域 ID 表达正式状态或 action 身份。Locomotion 状态 MUST 使用 `Locomotion.*`，Action 状态或 resolved action MUST 使用 `Action.*`。系统 MUST NOT 要求这些 ID 位于同一棵角色级层级树。

#### Scenario: 默认 Locomotion ID
- **WHEN** 设计者查看默认 Locomotion 配置
- **THEN** 配置 MUST 能显示 `Locomotion.Idle`
- **AND** MUST 能显示 `Locomotion.MoveStart`
- **AND** MUST 能显示 `Locomotion.MoveLoop`
- **AND** MUST 能显示 `Locomotion.MoveStop`
- **AND** MUST 能显示 `Locomotion.TurnBack`

#### Scenario: 默认 Action ID
- **WHEN** 设计者查看默认 Dodge 配置
- **THEN** 配置 MUST 能显示 `Action.Dodge`
- **AND** MUST NOT 要求显示 `FullBody/Action/Dodge`
- **AND** MUST NOT 要求 Dodge 成为 Locomotion 同树 sibling 才能运行

### Requirement: 状态图只作为领域实现

系统 MAY 继续使用项目自研状态图运行时表达单个领域内部的状态推进，但该运行时 MUST NOT 成为 Character frame pipeline 之外的第二输出权威，也 MUST NOT 被固定为所有 Action 的唯一生命周期实现。

#### Scenario: Locomotion 复用状态图
- **WHEN** Locomotion module 需要表达基础移动 phase
- **THEN** 它 MAY 使用自研状态图
- **AND** 该状态图 MUST 输出纯数据 snapshot/facts
- **AND** 该状态图 MUST NOT 直接执行 movement 或 animation

#### Scenario: Action 可使用非状态图实现
- **WHEN** Dodge 或等价动作需要表达持续时间、变体和结束条件
- **THEN** 它 MAY 使用 action instance 或 timeline
- **AND** MUST 仍通过 Action resolver、body claim 和 Character frame pipeline 参与输出

### Requirement: Snapshot 与诊断 view 分离

领域 snapshot 和 Action facts MUST 只表达各自领域恢复所需的纯数据事实。FullBody owner、旧 FullBody path 或兼容 owner view MUST NOT 作为 snapshot 核心职责。需要诊断时，系统 MAY 从领域 snapshot、metadata 和 `CharacterFramePlan` 派生只读 view。

#### Scenario: Snapshot 保持纯状态事实
- **WHEN** 捕获 Locomotion 或 Action snapshot
- **THEN** snapshot MUST 保存恢复所需的 active id、state time、variant、payload 或 pending transition
- **AND** MUST NOT 保存 FullBody owner 作为核心字段
- **AND** MUST NOT 保存 Unity scene object 或 animation runtime object

#### Scenario: View 不反向决定仲裁
- **WHEN** 诊断或旧测试读取 FullBody view
- **THEN** view MAY 显示当前 action、Locomotion phase、body claim 或 suppression 状态
- **AND** view MUST 从领域 facts 或 frame plan 派生
- **AND** view MUST NOT 写回 transition、body arbitration 或 output apply

### Requirement: 文档口径声明管线主线

系统 MUST 让项目文档、agent 指南和 OpenSpec 对角色运行时采用同一口径：Character frame pipeline 是唯一合成主线；Locomotion 与 Action 是领域 module；FullBody 是 body/channel claim、动画层语义或诊断 view；状态图/HFSM 是局部 implementation 选项。

#### Scenario: 文档不再推荐统一树
- **WHEN** agent 阅读项目状态机相关文档
- **THEN** 文档 MUST NOT 把 FullBody 分层状态树描述为正式目标
- **AND** MUST NOT 把 Locomotion 描述为 FullBody 子职责
- **AND** MUST 明确绕过 Character frame pipeline 的实现需要重新审批
