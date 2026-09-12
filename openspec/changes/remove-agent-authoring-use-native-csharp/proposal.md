## Why

AI 已能编写 C# 并调用项目的 FlowCanvas、Timeline 和 Presentation 正式 API。继续维护 Agent JSON、Document、导出、对账、专属 Validator 和 MCP，使同一业务修改多经过一层协议；业务规则不应该依赖 Agent 层才能成立。

本变更直接删除 Agent authoring 框架。已有正式 API 的校验照常使用，Agent 内重复校验删除；只有证明确实缺少的业务约束，才补到所属业务模块，不把 Agent 框架换名搬走。

## What Changes

- **BREAKING**：删除 BTSMTL Agent JSON/v8 目录包、Snapshot、Codec、Store、Exporter、Reconciler、Agent Mutation/Session/Report/Validator、Agent Controller Window 及五个 `btsmtl.*` authoring MCP，不保留兼容读取、转发、别名或替代 Agent 框架。
- AI 直接编写 Editor C#，调用正式 Graph / FSM / Macro / Timeline / Profile / Pose / Clip Curve API，结果保存为现有 Unity authoring 资产。人工编辑器继续可写；C# 不成为要求资产持续匹配的全量声明来源。
- 将 authoring binding 中 `JObject / JToken` 改为正式类型和资源引用；人工编辑同步调用同一入口，删除隐藏在 SkillDocument / PresentationDocument 或 Slate UI 中的 JSON 适配。
- 校验归正式节点、Graph、Timeline、Presentation 和 Character 编译器。删除重复或协议专用校验，不新增包办所有业务的中央 Validator、通用 C# DSL、执行器或整包同步事务。
- 保留现有领域 Undo、稳定身份、owner 和保存链。确有跨 owner 的单次业务编辑才组合其事务；不为普通 AddNode 重造全角色事务。
- Slate 继续作为 BTSMTL Timeline 的人工编辑界面；正式创建使用 `TimelineData.AddTrack / AddClip / AddSection`，不改为 Slate Cutscene 数据源。
- Character Build 仍独立显式执行；保留现有 Build MCP 与其他非 AgentAuthoring 工具，不新增 AI 自动执行协议。
- 清理现行规范中的 Agent 专属合同，把仍有效的业务限制留在对应正式规范；列出与其他 active change 的冲突，不覆盖其已完成的 FSM、端口、Pose 或 UI 改动。

## Capabilities

### New Capabilities

- `character-csharp-authoring`：直接 C# 与人工编辑共享正式业务 API，规定来源、校验归属、身份、保存、删除和 Agent 完整退役边界。

### Modified Capabilities

- `agent-character-controller-synthesis`：移除 Agent 生成、专属校验与报告合同，业务边界归正式 authoring。
- `btsmtl-agent-authoring-document-sync`：移除整个目录包及同步协议。
- `btsmtl-agent-authoring-mcp-bridge`：移除 authoring bridge 合同；独立 Build 工具条款移到编译能力。
- `btsmtl-compiled-simulation-program`：诊断直接复用正式编译器，保留独立精确 Build 工具。
- `graph-authoring-domain-framework`：正式 metadata、类型化写入与校验供 C# 和人工共用，删除 Document 对账责任。
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

主要涉及 `Editor/CharacterPipeline/AgentAuthoring`、`Authoring/SkillDocument`、`Authoring/PresentationDocument`、Skill 节点 binding、Timeline Clip binding、Slate 投影新增界面与 Definition 导航。正式业务代码按实际依赖去重或补齐，不整包搬迁。

不改角色运行逻辑、插件内部资产序列化、Semantic IR、Program/Projection 格式，不重建现有资产，不新增测试，不在提案阶段操作 Unity。原 r1 评估文档退为本提案索引，不再维护“搬迁 Agent 总事务”方案。

现行规范仍要求 Document 与 Agent Validator；本提案提供对应删除/修改增量，尚不宣称现行系统已切换。原生 FSM、Slate UI、共同节点定义和 Agent attribute-driven 等 active change 的重叠条款与处理边界见 design；实施前须解决实际冲突，不自动撤销其他任务成果。
