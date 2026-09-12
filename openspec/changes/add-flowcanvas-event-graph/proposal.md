## Why

项目已有 FlowCanvas 的事件、变量、计算、分支和执行能力。本提案以动画为第一个应用，提供作者可视化更新变量并交给 PoseGraph 的正式接入；以后关卡等业务复用宿主机制，不重建事件执行器。

r2 按用户单向广播 2026-09-13-authoring-r2-plan 更新作者入口：事件图沿正式直接配置 API 接入公共 C# 输出/生成能力，退出已决定退役的 Agent Document 链。原生运行和动画输入合同保持已确认方向。

## What Changes

- 直接复用原生 FlowScript、GraphEditor、事件、执行线、计算、分支、Get/Set、Blackboard 和 Macro；事件图仍由 FlowCanvas runtime 执行，不增加事件图 Compiler、IR 或备用执行器。
- 保持动画初始化/每帧更新、只读角色事实、实例隔离、错误/Reset，以及唯一 Contract/Layout/Frame 生产。Pose 的 Get、条件、BlendSpace、曲线和编译消费继续由 Pose 任务负责。
- **BREAKING**：取消事件图 PresentationDocument 分片、Document 升版、Reconciler/反向导出和整包同步事务要求。移除 EventGraphAuthoringDocument 及其必经 ApplyDocument 中转，不能只删除 Agent 目录或改名保留结构模型。
- 保留原生创建、变量声明、配置、连接、身份、局部规则和已有 Undo，形成原生 UI 与 C# 生成共用的直接 API；不新建中央 Validator，不复制业务规则。
- 为公共代码输出器提供事件图薄适配，完整读取变量、节点、配置、Macro、动态端口、连线、布局和引用，输出正式 API 调用；不另建事件图导出器或 EventGraph MCP。未支持的正式内容明确拒绝完整导出。
- 仅复用公共 btsmtl.export_code 和 btsmtl.generate_assets。人工编辑不自动导出，生成不自动合并未导出的修改，两者不自动 Build；C# 是明确生成范围的可重建内容来源。
- 生成代码保持图和 Variable.ID 等逻辑身份；内部引用使用本次生成对象，真正外部资源明确传入，Profile 根绑定显式恢复，不依赖旧生成资产 GUID 重建内部对象。
- **BREAKING**：保留原生 EventGraph 输入生产与编译 PoseGraph 求值的明确边界；固定 motor 桥只在全部 Pose、条件、BlendSpace、运行和 Preview 消费者迁移后删除，不能与 Agent 协议删除混为一步。

## Capabilities

### New Capabilities

- flowcanvas-event-graph：原生事件图、宿主合同、变量身份、直接作者 API，以及接入公共 C# 输出/生成的领域薄适配。
- character-animation-event-graph：动画事件、只读事实、精确变量输出、身份重建和同次 Pose 交接。

### Modified Capabilities

- graph-authoring-domain-framework：在公共 C# 作者基线上保留原生 EventGraph 执行域，现有编译领域与其运行描述继续分离。
- character-animation-pipeline：唯一动画变量生产入口，区分输入更新和 Pose 提交，保持现有最终姿势链。

本 change 撤下原 btsmtl-agent-authoring-document-sync 增量，不再扩展将退役的协议。通用输出/生成、两个 MCP 及 Agent 协议整体删除由 remove-agent-authoring-use-native-csharp 的 r2 拥有，本任务不重复声明该能力。

## Impact

- 本任务唯一维护 Runtime/BTSMTL/EventGraphs/EventGraphAuthoringDocument.cs、HostEventGraph.cs、HostEventGraphEditorMutation.cs 的直接 API 收口及事件图输出薄适配；完整路径和先后依赖见 design D7/D8。
- Pose 任务负责 CharacterPoseGraphAuthoringAdapter 等消费调用适配；C# authoring 任务负责 AgentAuthoringEventGraphDocumentMapper 等公共协议退役。各任务不同时覆盖同一文件。
- 动画根事件图引用、变量声明/更新/输出属于本任务；Pose 只读输入和曲线清理仍属于 [独立 Pose 任务](../refine-pose-graph-readonly-blackboard/proposal.md)。
- 不接管 Skill/FSM、关卡内容、Timeline/Montage、Foot/IK 算法或其它角色改造。不因生成源码需要正常 C# 编译而重开事件图 runtime 路线。
- r2 最初由 PLAN 广播形成，只修改规划。用户随后明确要求“让实现窗口做”，现授权原已绑定实现窗口按 r2 继续；不新建窗口，不扩大文件所有权和删除范围。派发状态见 design 的 Workflow Binding。
