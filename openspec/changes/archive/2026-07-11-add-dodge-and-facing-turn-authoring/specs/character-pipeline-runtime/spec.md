## MODIFIED Requirements

### Requirement: Graph 执行上下文来自 CharacterGraphContext

系统 MUST 使用 `CharacterGraphContext` 作为 BTSMTL RootTree 的 `BaseGraph.User`。该 context MUST 直接提供 Timeline 播放请求服务、InputAction value source、authority mode、network tick context、gameplay blackboard、tick 起点 actor pose snapshot 和 correction 输入入口，MUST NOT 依赖场景搜索或 fallback 补齐缺失引用。Actor pose snapshot MUST 在 BTSMTL 决策前由 pipeline 从显式注入的 actor Transform 捕获，MUST NOT 由条件节点临时读取场景对象。

#### Scenario: TimelineNode 获取 Timeline 播放请求入口

- **WHEN** `TimelineNode` 在角色 pipeline 中被 tick
- **THEN** `TimelineNode` MUST 通过 `BaseGraph.User` 获取 `ITimelinePlaybackService`
- **AND** service MUST 由 `CharacterGraphContext` 暴露给 Graph/BTSMTL

#### Scenario: InputAction ValueNode 读取输入

- **WHEN** InputAction ValueNode 被请求输出值
- **THEN** 节点 MUST 通过 `BaseGraph.User` 获取 `IInputActionValueSource`
- **AND** value source MUST 使用 graph context 当前帧输入来源读取 Button、Float 或 Vector2

#### Scenario: 捕获 tick 起点角色姿态

- **WHEN** CharacterPipeline 开始一个新的 logic tick
- **THEN** pipeline MUST 在 BTSMTL 执行前捕获 actor 的平面位置与朝向
- **AND** 同 tick 内所有 ConditionRuleGraph MUST 读取同一个只读 snapshot

#### Scenario: 缺失上下文引用

- **WHEN** graph context 缺少 Timeline 播放请求服务、输入资产或有效 actor pose snapshot
- **THEN** 对应节点 MUST 按现有 BTSMTL 节点规则报告缺失来源
- **AND** graph context MUST NOT 通过 `FindObjectOfType`、`Camera.main`、全局 singleton 或 GameObject 搜索补齐该引用

#### Scenario: Graph 读取网络上下文

- **WHEN** ConditionRuleGraph、状态行为或后续 gameplay 节点需要读取网络 tick、authority mode、confirmed event 或 correction 状态
- **THEN** 它们 MUST 通过 `CharacterGraphContext` 的正式接口读取
- **AND** 它们 MUST NOT 直接读取 transport、Fantasy Session 或服务端对象
