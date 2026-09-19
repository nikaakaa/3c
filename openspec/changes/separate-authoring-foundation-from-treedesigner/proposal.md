## Why

当前技能、FSM 和 Pose 已经使用 FlowCanvas／NodeCanvas 原生图，分域数据访问、局部变量作用域、节点字段与端口规则仍由项目自研；这些有效能力却与旧 BaseGraph、旧节点执行器和旧画布混放在 TreeDesigner 中。作者难以判断删除旧 UI 会损失什么，现行部分规范还要求恢复旧图底座，因此需要明确唯一数据来源、独立业务合同和编辑集成，并完整退出旧链路。

## What Changes

本次只抽离作者合同、迁移受影响消费者与合法内容，并删除对应旧链路。现有技能变量寻址、Provider、生命周期、Pose 原生执行和 Timeline 调度机制保持原领域所有权。现行黑板 fact projection 与 provenance 的完整业务约束保留，不因目录清理删除。

- 保留现有 FlowCanvas／NodeCanvas 图资产格式、原生 Port／Connection，以及 TimelineData 和 Slate 编辑入口；不创建第三套可写图、双向同步器或替代序列化格式。
- 抽出项目自有黑板声明、作用域／生命周期、输入绑定、事实投射，以及节点字段、逻辑端口、类型和连接约束。分域 Provider 的数据所有权与局部变量生命周期分别保留，不退回通用可写字典。
- 将业务规则、框架适配、UI 描述、运行状态明确分开。业务规则由原领域唯一声明，FlowCanvas Port 对象由适配生成，编译器与 C# authoring 读取同一语义；Skill 继续编译执行，Pose 继续使用已有原生运行实例。
- 保留仍在使用的分域黑板面板、专用参数编辑、资源选择、Details、Navigator、多资产 Undo、C# 输出／生成和只读运行观察；迁出旧目录中的有效编辑辅助，删除被替代的窗口和画布。
- **BREAKING**：退役 TreeDesigner 的旧窗口、GraphView、节点视图，以及无正式消费者的 BaseGraph／BaseTree／BaseNode／PropertyPort／BaseExposedProperty 执行链、旧编译重载和旧 Timeline 图调用入口。有合法内容的旧资产先通过对应正式作者操作迁入原生图，不能以删资产代替迁移。
- **BREAKING**：对本次抽离的自有类型清理 TreeDesigner 命名空间、旧程序集和序列化类型引用。保留业务 ID、参数、连线、变量默认值与所有权；不保留旧程序集空壳、兼容类型、运行时迁移器或 fallback。
- 用增量规范替换要求旧 BaseGraph／旧画布的条款，保留已有领域算法与作者能力；当前主目录中其他任务的正确修改不回退。

## Capabilities

### New Capabilities

无。使用现有领域规范表达本次拆分，不另建一套图系统规范。

### Modified Capabilities

- `btsmtl-graph-core`：以当前原生图资产作为结构来源，保留稳定身份与私有／共享所有权，退出旧 BaseGraph 结构和运行实例要求。
- `graph-authoring-domain-framework`：独立共享作者合同与端口规则，分离业务定义和 UI 描述，保持人工、C# 与编译消费一致。
- `graph-authoring-editor-shell`：以原生 GraphEditor 为画布入口，保留必要的自研编辑集成，退出旧 GraphView Shell。
- `btsmtl-tree-inspector-information-architecture`：保留数据分类、专用 Details、导航和观察能力，移除对 TreeWindow／TreeView 实现的绑定。
- `character-pipeline-blackboard`：明确分域 Provider、原生局部变量、自有作用域声明和运行槽位各自职责，取消 BaseExposedProperty 唯一作者表面的要求。
- `btsmtl-skill-authoring-model`：明确自研节点与逻辑端口定义、原生框架适配及实际运行执行之间的关系，移除失去消费者的旧节点适配。
- `btsmtl-timeline-direct-runtime`：播放调用只接收必要的内容和调用身份，不再要求整棵旧 BaseGraph；保留现有调度、TreeClip、Marker 和提交语义。
- `unity-simulation-assembly-ownership`：明确独立作者合同和编辑集成的单向依赖，以及本次类型迁名的精确资产迁移约束。

## Impact

主要涉及 TreeDesigner/Scripts 中混合的黑板与 Authoring 合同、TreeDesigner/Editor 中仍被原生编辑器使用的辅助模块，以及 Skill／Pose／Timeline／C# authoring 的消费者和程序集引用。实际目录与文件迁移清单、删除前提、序列化处理及现行规范冲突见 design.md。

用户继续使用现有原生图和项目专用编辑功能；本次不改变技能参数、战斗规则、运动仲裁、回放输入、Pose／Foot IK 算法、网络模型或资源打包。Motion 重复累计与正在进行的 Pose、Timeline 运行修复保持各自任务所有权。本变更在主目录规划，不新建 worktree；不新增测试或验证任务。

当前已存在的原生 FSM、Pose 编辑与只读输入变更提供迁移事实，但其未归档条款不是本次完成证据。本次只新增本目录 proposal、design、spec delta 与实施清单；不修改代码，不替其它任务归档，不覆盖已有主 spec 改动。
