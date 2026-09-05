## Why

用户希望把 AI 行为框架的维护交给已导入的 Behavior Designer，不再维护自研 AI 图、编译器、解释器、黑板和图调试。角色控制与技能已经明确分工，本变更把插件作为唯一 AI 行为来源，通过正式角色输入接入 Local、Unity Authority 和 Rollback，保留同一套角色、动作、运动与表现规则。

## What Changes

- **BREAKING**：使用 Opsive Behavior Designer 3.0.3 的编辑器、行为树、子树、变量和运行调试；删除 `AIControllerDefinition`、AI RootTree、AI Frontend、AI Semantic IR、AIIntentProgram、AIControllerState 及专用作者/运行链。项目只维护角色观察、输入与动作结果相关的游戏任务，不把插件图再编译为自研 AI Program。
- 插件相关类型只进入独立 Unity 接入模块。普通 GameObject Task 调用项目能力，树遍历复用插件 DOTS 实现；不因插件依赖 Entities 而迁移角色、KCC、技能或表现，不承诺未经采样的性能收益。
- **BREAKING**：在正式 Session Source 中提供批量输入准备与一次性输入生产。插件只为新的输入 Tick 推进，完整输入校验并冻结后才进入角色模拟；发送重试、角色重演和同 Tick 重试复用同一份输入，不重新执行 BT。
- **BREAKING**：将插件可变状态与角色模拟状态分责。已冻结输入及其生产序号属于不可倒退的 Source 输入事实；消费位置、Gameplay 状态与网络重演仍遵守各自事务。插件执行中途失败使生产者和会话明确失败，不用恢复旧 AI 内核、Neutral 输入或重跑树掩盖失败。
- Local Float32/Fixed 由本机生产 Bot 输入；ServerAuthoritative 由现有 Unity Authority Worker 生产；Rollback 由会话显式指定的既有 Unity Peer 生产 Bot 输入。客户端角色预测/回滚继续使用正式数值合同，BT 不进入 Fixed 世界 snapshot、状态 hash 或历史重演。
- **BREAKING**：网络名单分离连接身份、Actor 身份与输入所有权，允许一个真实 Peer 控制玩家 Actor 和多个 Bot。Relay 保持纯 .NET 路由与最终输入确认职责，不运行 AI、角色或世界；不为 Bot 创建假连接、假 Player 或新的 Unity Relay。
- 保持已发布 Bot 输入不可改写。AI 可以依据生产端当时已提交的预测世界作出决定；迟到输入纠正世界后，历史 Bot 操作仍然有效，后续新决策再读取新的已提交观察。本变更不提供 AI 历史重决策、存活 BT 无损迁移、生产端断线接管或任意存档恢复后继续决策。
- Unity Authority 的玩家输入与本地 Bot 输入进入同一权威 batch，敌人通过既有结果与远端表现通道同步。普通 .NET DotRecast Authority 保留现有正式用途，但本变更不为它安装插件 AI；不支持的装配在启动前明确拒绝。
- **BREAKING**：从 BTSMTL Agent Document、MCP、Capability、Mutation、Validator、窗口和技能说明中删除游戏 AI domain；保留主重构的技能/控制配置合同及其它正式非 AI domain，不新建插件 AI Document 或通用节点自动化平台。
- 迁移稳定 Corin 的本地 AI 样例，并在两个真人端点的正式网络产品中装配玩家角色 Bot 与中立 Actor 的最小移动/动作请求用例。复用同一角色资产，不补齐未完成的命中、伤害、队伍战斗规则或 TrainingEnemy 表现，不把该接入描述为完整 2v2vE 战斗交付。

## Capabilities

### New Capabilities

- `behavior-designer-ai-integration`：插件作者与版本绑定、批量运行、游戏任务边界、原生调试和正式内容发布。

### Modified Capabilities

- `btsmtl-ai-controller-authoring`：移除自研 AI Definition、图、窗口、黑板与 Intent 作者要求。
- `agent-ai-controller-synthesis`：移除 BTSMTL AIController domain、专用合成、校验与技能流程。
- `gameplay-ai-control-source`：改为插件输入生产者、冻结观察、不可改写输入事实与正式失败语义。
- `character-input-pipeline`：玩家与插件共用输入、请求时序、结果关联及输入生产/消费状态边界。
- `gameplay-simulation-session-composition`：锁定行为内容、输入所有权、观察能力和批量准备合同。
- `btsmtl-graph-core`：删除 AIControllerTree 的领域注册，保留技能及其它正式树能力。
- `graph-authoring-editor-shell`：删除自研 AI 窗口装配，保留共享编辑行为。
- `graph-authoring-domain-framework`：插件 AI 使用插件自身作者框架，不进入 BTSMTL Capability/Mutation。
- `btsmtl-agent-authoring-document-sync`：删除 AI 根与分片，保留唯一整包事务和其它领域能力。
- `btsmtl-agent-authoring-mcp-bridge`：移除 AIController 路由，保留既有五个生命周期工具。
- `agent-character-controller-synthesis`：移除 Character Document 合同中的 AI 正文要求。
- `deterministic-rollback-network-model`：一端多 Actor 输入归属、Bot 输入只生产一次及角色重演边界。
- `deterministic-rollback-two-client-demo`：两个真实 Peer 承载玩家与 Bot，保留同一 Fixed 角色模拟。
- `deterministic-rollback-relay-product`：发布并校验真实 Peer 与 Actor 所有权名单，保持 Relay-only 拓扑。
- `server-authoritative-hybrid-sync-model`：连接与 Actor 路由分离，权威本地 Bot 输入不依赖客户端连接。
- `server-authoritative-prediction-correction-pipeline`：合并玩家/Bot 输入，保持同一权威 batch 和正式结果同步。
- `unity-simulation-assembly-ownership`：隔离 Opsive/Entities 依赖，portable 模拟与网络合同不引用插件。
- `gameplay-network-test-build-workflow`：正式 Candidate 锁定 Bot 行为、输入所有权及所需资源闭包。

## Impact

- 主要实现范围：`Main/Runtime/Character/AI`、`Main/Editor/AI`、AI 专用 Semantic/Program/State、Unity 输入与 Session Source 接入、两个网络模型的名单/端点/输入装配，以及 Agent 作者链和正式样例资产。
- 依赖已导入的 `com.opsive.behaviordesigner`、`com.opsive.graphdesigner`、`com.opsive.shared` 和 Entities；新增代码沿现有发布与程序集边界进入 Unity 产品，不建立第二套资源或网络发布流程。
- 与 `refactor-btsmtl-authoring-architecture` 的“保留 AIIntentProgram/AIController domain”直接冲突，需按本提案删除目标对账；与 `decouple-timeline-from-skill`、场景预览 change 只合并非 AI 作者接口，不改动其 Timeline/TreeClip/预览所有权。详细差异、合并规则和删除范围见 `design.md`。
- 当前 AI 状态回滚、一 Peer 一 Actor、网络默认拒绝 AI 等规范需由本 change 的 delta 正式替换。当前技能、Action、KCC、Motion、Pose、IK、Camera 和既有无 Bot 网络行为不借此重写。
- 本次只创建提案、设计、delta specs 与实施任务，不修改 current specs、其它 active change、实现代码或 Unity 资产，不启动实施。后续复用正式编译、Validator 和既有回放/网络运行证据，不新增测试代码，不把手动验收写入任务。
