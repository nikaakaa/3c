# btsmtl-agent-authoring-mcp-bridge Specification

## Purpose
定义现有 Unity MCP 到 Agent authoring service 的薄桥接，以及 snapshot、dry-run、事务应用、验证、回滚和保存的统一边界。
## Requirements
### Requirement: Agent authoring 必须通过现有 Unity MCP 暴露单一桥接工具

系统 MUST 在现有 `unityMCP` 连接中自动注册 `manage_btsmtl_agent_authoring` editor-only 自定义工具。工具 MUST 使用当前 MCP package 的正式自定义工具发现和命令分发机制，MUST NOT 启动第二个 MCP server、终端常驻进程或 Unity batchmode。

#### Scenario: Unity Editor 完成 domain reload

- **WHEN** 当前项目的 Editor assembly 和 Unity MCP package 成功加载
- **THEN** `mcpforunity://custom-tools` MUST 能发现 `manage_btsmtl_agent_authoring`
- **AND** Codex MUST 能通过当前 Unity MCP 实例调用该工具

#### Scenario: Agent 调用 bridge

- **WHEN** Codex 调用 `manage_btsmtl_agent_authoring`
- **THEN** 请求 MUST 由当前 Unity Editor 会话处理
- **AND** bridge MUST NOT 创建额外进程或 transport

### Requirement: MCP bridge 必须提供完整且受限的 authoring action 集合

`manage_btsmtl_agent_authoring` MUST只提供`export_snapshot`、`dry_run_patch`、`apply_patch`和`validate`四个action。所有action MUST要求明确的`domain`与`root_asset_path`；`dry_run_patch`和`apply_patch`还 MUST要求`patch_json`。未知domain、未知action、domain/root类型不匹配或缺失参数 MUST在修改资产前返回结构化错误。

#### Scenario: 导出当前 Agent Snapshot

- **WHEN** Codex 以合法 definition 路径调用 `export_snapshot`
- **THEN** bridge MUST 返回 `AgentGraphSnapshotExporter` 生成的紧凑只读 snapshot
- **AND** bridge MUST NOT 修改或保存任何资产

#### Scenario: 预检 Patch

- **WHEN** Codex 以合法 definition 路径和 Patch JSON 调用 `dry_run_patch`
- **THEN** bridge MUST 返回 `AgentPatchCompiler` 生成的 planned diff、messages 和 metrics
- **AND** bridge MUST NOT dirty 或保存资产

#### Scenario: 验证当前 Graph

- **WHEN** Codex 以合法 definition 路径调用 `validate`
- **THEN** bridge MUST 返回 `AgentGraphValidator` 的机器可读 report
- **AND** bridge MUST NOT修改 graph

#### Scenario: 调用未知 action

- **WHEN** MCP 请求携带四个正式 action 之外的值
- **THEN** bridge MUST 返回 unsupported action 错误
- **AND** bridge MUST NOT改用菜单、反射命令或默认 action

### Requirement: MCP 和 EditorWindow 必须共用唯一 Patch application service

系统 MUST 使用 `AgentPatchAuthoringService` 或等价 editor-only service 统一编排 Patch 解析、snapshot、dry-run、apply、validator、Undo 和保存。MCP handler 与 `AgentCharacterControllerSynthesisWindow` MUST 调用同一 service。MCP handler、窗口和菜单 MUST NOT 各自复制一套 Patch 应用生命周期。

#### Scenario: 窗口执行 Patch dry-run

- **WHEN** 作者在 Agent Controller 窗口请求 dry-run
- **THEN** 窗口 MUST 调用统一 application service
- **AND** 返回语义 MUST 与 MCP `dry_run_patch` 一致

#### Scenario: MCP 执行 Patch apply

- **WHEN** Codex 调用 `apply_patch`
- **THEN** MCP handler MUST 把请求交给统一 application service
- **AND** handler MUST NOT 直接调用 BTSMTL 结构编辑 API

### Requirement: Apply 必须执行预检和资产级事务

`apply_patch` MUST 先基于调用时的当前资产执行无副作用 dry-run。预检成功后，系统 MUST 对 definition、RootTree 和全部可达 inline/shared graph serialized owner 建立单一 Undo 事务，再调用 `AgentPatchCompiler` apply 和 `AgentGraphValidator`。只有 compiler 与 validator 全部成功时才可保存；任一错误或异常 MUST 回滚本次全部修改。

#### Scenario: Dry-run 发现引用错误

- **WHEN** Patch 引用了当前 definition 中不存在的 ActionProfile、Timeline、input request 或 graph target
- **THEN** `apply_patch` MUST 返回 dry-run report
- **AND** 系统 MUST NOT 建立部分 graph 修改或保存资产

#### Scenario: Apply 后验证失败

- **WHEN** compiler 完成修改但 validator 报告 graph 语义错误
- **THEN** application service MUST 回滚当前 Undo group 覆盖的全部 owner
- **AND** bridge MUST 返回失败 report
- **AND** 系统 MUST NOT 保存半成品资产

#### Scenario: Apply 完整成功

- **WHEN** dry-run、compiler apply 和 validator 全部成功
- **THEN** application service MUST 折叠本次 Undo group
- **AND** 系统 MUST 调用正式资产保存入口
- **AND** response MUST 明确报告 applied 与 saved 状态

#### Scenario: 无法覆盖事务 owner

- **WHEN** 系统无法可靠枚举或注册某个可达 graph 的 serialized owner
- **THEN** `apply_patch` MUST 在修改前失败
- **AND** 系统 MUST NOT 退化为只保护 RootTree 或无回滚 apply

### Requirement: Bridge 必须复用正式 Agent compiler 与 BTSMTL authoring API

MCP bridge MUST 复用 `AgentGraphSnapshotExporter`、`AgentPatchCompiler`、`AgentGraphValidator` 和 `AgentCompileReport`。所有 graph 修改 MUST 继续由 compiler 通过 `BaseGraph.CreateNode`、`BaseGraph.Link`、`BaseGraph.LinkProperty` 和正式节点配置入口执行。Bridge MUST NOT 直接写 Unity YAML、节点集合、边集合、GUID 映射或建立第二套 graph 数据。

#### Scenario: Patch 请求创建状态和 Transition

- **WHEN** MCP bridge 接收包含状态和 Transition operation 的 Patch
- **THEN** bridge MUST 将 Patch 交给 `AgentPatchCompiler`
- **AND** compiler MUST 继续受 emitter 白名单和 graph 类型规则约束

#### Scenario: Patch 请求未知节点或操作

- **WHEN** Patch 包含 compiler 不支持的 operation、节点或端口
- **THEN** bridge MUST 返回 compiler report 中的明确错误
- **AND** bridge MUST NOT 创建 placeholder、执行动态代码或直接写序列化字段

### Requirement: Definition 目标必须由调用上下文显式提供

MCP/Editor请求 MUST通过`domain`与`root_asset_path`显式选择`CharacterPipelineDefinition`或`AIControllerDefinition`。路径 MUST是`Assets/`下能精确解析为对应domain根类型的项目资产。`AgentPatchIR` MUST保存Snapshot提供的root identity与source revision，但 MUST NOT保存或解释项目资产路径。系统 MUST NOT通过目录扫描、同名匹配、场景对象、剪贴板或旧配置寻找目标资产。

#### Scenario: Definition 路径合法

- **WHEN** 请求给出能精确加载为 `CharacterPipelineDefinition` 的 `Assets/...` 路径
- **THEN** service MUST 以该 definition 及其正式引用链作为 snapshot、resolver 和 compiler 上下文
- **AND** Patch MUST 只作用于该上下文

#### Scenario: Definition 路径缺失或类型错误

- **WHEN** 请求缺少路径、路径不在 `Assets/` 下、资产不存在或类型不是 `CharacterPipelineDefinition`
- **THEN** bridge MUST 在解析或应用 Patch 前返回错误
- **AND** bridge MUST NOT 搜索替代 definition

### Requirement: 临时剪贴板和快捷键入口必须删除

系统 MUST 删除 `Apply Agent Patch From Clipboard` 菜单、快捷键和剪贴板读取逻辑。系统 MUST NOT 保留隐藏菜单、兼容快捷键、Patch inbox 或文件监视器作为 MCP 不可用时的 fallback。人工 authoring 继续使用现有 Agent Controller 窗口，自动 authoring 使用正式 MCP bridge。

#### Scenario: 用户查看 Unity Shortcuts

- **WHEN** Editor assembly 与 Unity MCP package 完成 domain reload
- **THEN** Unity Shortcut Manager MUST NOT 再显示 Agent Patch clipboard 命令
- **AND** 该命令 MUST NOT 与 Unity 内置快捷键产生冲突

#### Scenario: MCP 当前不可用

- **WHEN** Unity MCP 未连接或自定义工具加载失败
- **THEN** 系统 MUST 明确暴露连接或编译问题
- **AND** 系统 MUST NOT 自动改用剪贴板、菜单或临时 Patch 文件

### Requirement: Bridge 必须拒绝不安全的 Editor 状态

Bridge MUST 在 Unity 正在编译、更新 AssetDatabase、处于 Play Mode 或正在切换 Play Mode 时拒绝执行。系统 MUST 返回明确状态错误，MUST NOT 排队重试、延迟执行或启动额外进程等待。

#### Scenario: Unity 正在编译

- **WHEN** Codex 在 Editor domain reload 或脚本编译期间调用任一 action
- **THEN** bridge MUST 返回 editor busy 错误
- **AND** bridge MUST NOT 读取或修改半加载的 graph 数据

#### Scenario: Unity 正在 Play Mode

- **WHEN** Codex 在 Play Mode 中调用 `apply_patch`
- **THEN** bridge MUST 拒绝 authoring 写入
- **AND** runtime working copy 与 authoring asset MUST NOT 因该请求发生交叠修改

### Requirement: MCP 返回必须保留机器可读诊断

Bridge response MUST 保留 action、definition 路径、success、applied、saved，以及 snapshot 或 `AgentCompileReport`。Compile report MUST 保留 message path、code、severity、message、suggestion、planned/applied diff 和 metrics。Bridge MUST NOT 只返回 Console 日志字符串。

#### Scenario: Patch 编译失败

- **WHEN** compiler 拒绝一条 Patch operation
- **THEN** MCP response MUST 包含对应 operation path、错误 code、原因和建议修复
- **AND** Codex MUST 能直接使用该 response 生成下一轮 Patch

#### Scenario: Patch 应用成功

- **WHEN** `apply_patch` 完成并保存
- **THEN** MCP response MUST 包含最终 applied diff 和 validation 结果
- **AND** response MUST 明确 `applied=true` 与 `saved=true`

### Requirement: MCP bridge 必须透传同一 v15 Character 与 AI 事务

BTSMTL Agent MCP bridge MUST接受并返回`agent-character-controller-synthesis.v15` Snapshot、Patch与Validation结果，并通过显式domain discriminator透传CharacterController或AIController generic事务。CharacterController事务继续携带Timeline、MotionWarp、Marker与registered Curve typed operation；AIController事务只携带AI Definition、Graph、Blackboard、Configured Candidate、Observation、Memory与Intent typed operation。Bridge MUST只调用正式Agent Snapshot、lowerer、dry-run、apply和validator入口，不得新增AI专用action、SerializedProperty、YAML、反射、任意字段写入或旧v14转换工具。

#### Scenario: 通过bridge配置循环组

- **WHEN** 调用方通过MCP bridge提交合法v14 Patch配置WalkLoop与RunLoop的Cyclic Marker Group与CanBeLeader角色
- **THEN** bridge MUST先返回正式dry-run command plan与validation结果
- **AND** apply MUST由同一typed plan执行
- **AND** bridge MUST返回更新后的stable identities与group摘要

#### Scenario: 通过bridge配置有限序列

- **WHEN** v15 Patch为Finite AnimationTrack提交frame 0到DurationFrame的marker序列与同步角色
- **THEN** bridge MUST保留重复MarkerId occurrence的独立AuthoringId
- **AND** MUST返回call site Once与directed pair coverage结果

#### Scenario: bridge收到非法marker事务

- **WHEN** Patch包含重复AuthoringId、非法frame、Once/Loop冲突或group pair缺口
- **THEN** bridge MUST返回正式Agent validation code、path与相关identity
- **AND** MUST不绕过validator直接写Unity资产

#### Scenario: 通过bridge修改Curve Channel

- **WHEN** 调用方通过generic Patch提交registered ChannelId与完整AnimationCurve payload
- **THEN** bridge MUST原样透传owner identity、domain、wrap mode和完整Keyframe字段
- **AND** lowerer与handler MUST调用同一Catalog descriptor和owner MutationAdapter
- **AND** bridge MUST不按字段名寻找AnimationCurve

#### Scenario: MCP提交AI Controller Patch

- **WHEN** 调用方通过MCP bridge提交合法v15 AI Controller Patch
- **THEN** Bridge MUST把同一请求交给AgentPatchAuthoringService
- **AND** MUST返回typed plan、事务与Validator产生的机器可读报告

#### Scenario: bridge收到旧schema请求

- **WHEN** 调用方提交v14或更早的Snapshot或Patch
- **THEN** bridge MUST返回unsupported schema错误
- **AND** MUST不转换为v15或调用旧reader
