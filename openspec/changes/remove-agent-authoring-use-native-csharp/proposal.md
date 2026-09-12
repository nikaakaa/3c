## Why

AI 已能编写 C# 并调用项目的 FlowCanvas、Timeline 和 Presentation 正式 API。继续维护 Agent JSON、Document、导出、对账、专属 Validator 和 MCP，使同一业务修改多经过一层协议；业务规则不应该依赖 Agent 层才能成立。

本变更激进删除 Agent authoring 框架，只提供“资产显式导出为干净 C#”和“显式执行 C# 创建并保存资产”两个作者MCP。人工编辑资产不会生成代码；只有调用导出工具时才输出当前完整结构。已有正式 API 的校验照常使用，Agent 内重复校验删除，缺失业务约束补到所属模块。

## What Changes

- **BREAKING**：删除 BTSMTL Agent JSON/v8 目录包、Snapshot、Codec、Store、Exporter、Reconciler、Agent Mutation/Session/Report/Validator、Agent Controller Window 及五个 `btsmtl.*` authoring MCP，不保留兼容读取、转发、别名或替代 Agent 框架。
- **BREAKING**：生成范围内的图和 Timeline 以 C# 创建代码作为可重建的内容来源，Unity authoring 对象是生成结果和人工编辑载体，不再要求把生成资产永久当作唯一内容来源。原始动画、Rig、素材和明确范围外的资源仍是外部输入，不随生成资产删除。
- 新增只读的完整图导出器：遍历正式图及其拥有的 FSM/Macro/Blackboard/Timeline/片段/曲线/布局，直接输出已有创建、配置和连接 API 的调用；不先导出 JSON，不读取或分析旧 C#。
- 人工修改后只有显式请求才再次导出当前完整结构，统一整理创建代码；不保留原代码的循环、手写组织或编辑操作历史，不做语法树定位、增量源码写回或自动同步。
- 作者功能只提供 `btsmtl.export_code`（资产到C#）与 `btsmtl.generate_assets`（C#到资产）两个MCP，薄桥分别调用正式导出和生成服务。前者明确写出源码但不改输入资产，后者明确执行已编译的正式创建入口并保存输出资产。
- 人工编辑、保存资产、拖动Timeline以及Unity源码编译均不自动触发这两个操作；不新增源码Undo、watcher、后台同步或自动生成状态机。
- 本任务负责 `BtsmtlSkillNodeAuthoringBinding.cs` 与 `TimelineAuthoringClipBinding.cs` 的JSON退役，消费已有正式业务定义；Timeline任务负责Slate UI调用接入，其他领域任务维护各自正式API和薄输出适配，不改写已正确规则。
- 校验归正式节点、Graph、Timeline、Presentation 和 Character 编译器。删除重复或协议专用校验，不新增包办所有业务的中央 Validator、通用 C# DSL、执行器或整包同步事务。
- 保留现有领域校验、Undo、owner 和保存链；生成代码显式记录业务稳定身份并重建引用，已有输出范围可被完整替换，不要求永久保留生成资产的物理对象身份。不为普通编辑重造全角色事务。
- Slate 继续作为 BTSMTL Timeline 的人工编辑界面；正式创建使用 `TimelineData.AddTrack / AddClip / AddSection`，不改为 Slate Cutscene 数据源。
- Character Build 仍独立显式执行；原有 Build 与其他非AgentAuthoring工具不属于本次两个作者MCP，也不被导出/生成工具自动调用。
- 清理现行规范中的 Agent 专属合同，把仍有效的业务限制留在对应正式规范；列出与其他 active change 的冲突，不覆盖其已完成的 FSM、端口、Pose 或 UI 改动。

## Capabilities

### New Capabilities

- `character-csharp-authoring`：两个显式作者MCP、正式图完整导出C#、代码执行重建同一结构；规定手动编辑不生成源码、输出覆盖、依赖顺序、外部资源、稳定身份和Agent完整退役边界。

### Modified Capabilities

- `agent-character-controller-synthesis`：移除 Agent 生成、专属校验与报告合同，业务边界归正式 authoring。
- `btsmtl-agent-authoring-document-sync`：移除整个目录包及同步协议。
- `btsmtl-agent-authoring-mcp-bridge`：移除 authoring bridge 合同；独立 Build 工具条款移到编译能力。
- `btsmtl-compiled-simulation-program`：诊断直接复用正式编译器，保留独立精确 Build 工具。
- `graph-authoring-domain-framework`：正式 metadata、类型化写入与校验供 C#、人工和图代码导出共用，删除 Document 对账责任，不增加第二字段模型。
- `btsmtl-graph-core`：稳定身份和 capability 不再列出退役 Agent 消费者。
- `character-presentation-pose-graph`：移除 Document exporter/sync 依赖，保留 Pose 定义、Mutation、布局和编译边界。
- `character-pose-plan-compilation`：节点定义不再向 Agent Document / Reconciler供给语义。
- `character-animation-presentation-authoring`：C# 直接使用正式 Curve 与 Presentation 写入入口。
- `character-animation-clip-authoring`：注册曲线继续使用唯一 catalog，移除 Agent Document 消费要求。
- `character-state-timeline-authoring-loop`：资产编辑改用正式 API，保留独立显式 Build。
- `btsmtl-timeline-editor-preview`：完整曲线修改直接经正式 API，不保留 Agent / Patch 修改合同。
- `character-motion-warp-authoring`：正式 Timeline / Compiler 负责唯一校验。
- `character-input-pipeline`：C# 与 Inspector 修改同一正式 timing class。
- `character-camera-pipeline`：C# 曲线编辑保留既有 Camera owner 边界。
- `character-state-interruption-authoring`：状态可见范围由正式 API / compiler / runtime 共用。
- `unity-simulation-assembly-ownership`：删除 Agent 工具的程序集归属，保持正式 Editor / Runtime 单向依赖。

## Impact

本任务唯一拥有两个作者工具、最小生成入口合同、通用代码输出/生成机制、领域薄扩展合同、旧Agent公共协议退役与本change的规范清理计划。新增输出器归正式Editor authoring模块，各领域提供从正式对象读取并输出其正式API调用的薄适配，不建立第二领域模型。

共享文件按design D0分工：Slate投影由Timeline任务改UI接入；EventGraph三个作者文件归事件图任务；Pose adapter/Mutation/输入消费归Pose任务；Skill Graph applier内有效FSM操作迁出归FSM任务；共享Capability与端口定义归数据统一任务。这些文件不得因位于相关目录或含Document/Agent名称而由本任务整目录删除。

旧协议删除门槛是作者调用者脱离Agent、正式编辑/生成/保存已可用。该门槛与运行时固定motor参数桥删除分开记录，本任务不承担动画变量运行闭环，也不等待该运行闭环来保留已无消费者的旧协议。

不改角色运行逻辑、插件内部资产序列化或 Semantic IR/Program/Projection 格式，不新增测试。实施需具备生成范围的删除重建与引用恢复能力，但本次文档更新不删除任何资产、不操作 Unity。完整角色迁移不由“一个图可导出”自动推定。旧评估仍仅为本提案索引，r2 取代原 r1 的“资产为唯一来源”设计。

现行规范仍要求 Document 与 Agent Validator；本提案提供对应删除/修改增量，尚不宣称现行系统已切换。原生 FSM、Slate UI、共同节点定义和 Agent attribute-driven 等 active change 的重叠条款与处理边界见 design；实施前须解决实际冲突，不自动撤销其他任务成果。
