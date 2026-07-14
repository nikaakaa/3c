# btsmtl-graph-core Specification

## ADDED Requirements

### Requirement: Graph 运行时初始化必须收敛到统一非虚入口

BaseGraph 的公开 InitTree 入口 MUST统一完成 root/nested route 校验、runtime identity、节点与边初始化、Blackboard 注册和派生完成钩子。重载之间 MUST不通过虚调用决定初始化顺序。派生 Graph MAY在初始化前校验正式上下文，并在核心初始化后解析自身节点引用。

#### Scenario: 初始化嵌套 State Graph

- **WHEN** StateNode 或 StateMachineNode 使用 parent runtime Graph 与 authoring route 初始化子 Graph
- **THEN** 统一入口 MUST先建立 parent/route
- **AND** OneRootTree 与 StateBehaviorSubTree 的派生节点引用 MUST在核心 maps 建立后解析

#### Scenario: 正式初始化 Timeline TreeClip

- **WHEN** TimelineRunningTree 通过 InitTimelineTree 收到完整 TimelineTreeClipRuntimeContext
- **THEN** 它 MUST在统一入口前保存并校验 context
- **AND** root 与 Timeline lifecycle 节点 MUST在核心初始化后解析

#### Scenario: 绕过正式 Timeline 初始化

- **WHEN** 调用普通 InitTree 初始化 TimelineRunningTree
- **THEN** 初始化前校验 MUST明确失败
- **AND** 系统 MUST不创建缺少 TreeClip runtime context 的半初始化 Graph
