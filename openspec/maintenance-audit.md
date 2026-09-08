# OpenSpec 整理与现行合同审计

更新：2026-09-08。范围：主仓库全部活跃change及现行spec；源码抽查采用当前工作区，Git读取期间已推进到`cfe1d9065`。这是文档整理结果，不是新的功能proposal或第二套任务清单；实现状态仍写回各原tasks。

## 本轮结果

- 活跃change从23项减少到21项，归档2项；现行spec从96份增加到97份。
- 已撤销膝角实验按撤销状态归档，不安装delta。Blend Space按用户明确决定取消独立演示、保留通用能力归档；独立spec已人工同步并核对与最终delta一致，archive命令用skip-specs避免再次重放。
- 删除Blend Space三个重复／过时关联delta，保留current中的typed Binding、Foot Motion和编译／观察合同，原版本在Git历史中可查。
- 修复两项change严格校验中的五个Scenario继承问题，未修改运行代码、资产或测试。
- 不移动并行任务正在创建的三个FlowCanvas计划，不接管Pose文档修改；不把任务框数、分支包含关系或旧运行日志换算成完成比例。

## 检查深度与证据限制

全部97份现行spec完成目录、Purpose、Requirement、Scenario与关联delta检查及严格校验；下表给出每份入口。对Document、HFSM控制、Pose画布、Blend Space、Foot诊断、AI入口及MM记录作源码或已有证据抽查。没有逐条运行验证所有规范，没有重新构建Unity／Player，没有完整审查外部仓库；标为保留不代表已证明全部实现正确。

## change处置

| change | 处置 | 依据及继续边界 |
|---|---|---|
| [add-animation-relative-knee-response](changes/archive/2026-09-08-add-animation-relative-knee-response/proposal.md) | 已归档：已撤销 | 用户已否决；f2c3ec0候选撤回，192218恢复记录已完成。保留失败证据，不同步膝角补偿delta。 |
| [add-character-presentation-blend-space](changes/archive/2026-09-08-add-character-presentation-blend-space/proposal.md) | 已归档：保留能力 | 306项原完成记录保留；用户取消22.11–22.20独立演示，补齐独立能力spec，删除三个重复或过时delta。 |
| [add-acl-animation-runtime](changes/add-acl-animation-runtime/) | 保留：接入收尾 | 主线已组合接收资源与公共代码；tasks仍0/49，需按接收版本核对资源、属性、生命周期和最新角色产物，不能按空框重做或按合入归档。 |
| [add-character-pose-correction](changes/add-character-pose-correction/) | 保留：分支交付 | 样本／节点／作者代码有阶段成果；真实Actor效果和当前Projection发布未完整交付，不能删。 |
| [add-compile-time-performance-instrumentation](changes/add-compile-time-performance-instrumentation/) | 保留：探针迁移 | 22/29；尚含最终Owner探针、手写Marker清理与产物证明，核心织入器存在不等于全部迁移完成。 |
| [add-discrete-stair-presentation](changes/add-discrete-stair-presentation/) | 保留：独立能力 | 0/148；增加真实阶梯及模型竖直连续表现，与现有Ramp表面能力不同。本轮只修复四个Scenario标题继承，不实施楼梯。 |
| [add-gameplay-performance-capture-workflow](changes/add-gameplay-performance-capture-workflow/) | 保留：产品接入 | 29/34；剩余DiagnosticCapabilitySet贯通构建、握手与产物验证，不能只按历史Player成功归档。 |
| [add-generated-diagnostic-sampling-framework](changes/add-generated-diagnostic-sampling-framework/) | 保留：框架交付 | 30/39；尚有真实产品Disabled/Capture门禁及订阅／封存闭环，与外部包抽取分工不同。 |
| [add-schema-driven-diagnostic-analysis](changes/add-schema-driven-diagnostic-analysis/) | 保留：状态需复核 | 38/41；源码已有CharacterFootQualityScorePublisher，5.6可能滞后，但尚需完整结果和Disabled门禁证据。评分spec正确部分保留。 |
| [decouple-timeline-from-skill](changes/decouple-timeline-from-skill/) | 保留：主线组合 | 0/47不能代表未实现；0495e6425组合接收已有，独立域、逐Clip策略及完整来源生命周期仍需收尾。 |
| [extract-generated-diagnostic-sampling-package](changes/extract-generated-diagnostic-sampling-package/) | 保留：消费门禁 | 15/22；外部包拥有框架，3C仍需程序集、产物、采集入口与版本闭包验证。不是框架change的重复副本。 |
| [integrate-pose-flowcanvas-editor-preview](changes/integrate-pose-flowcanvas-editor-preview/) | 保留：并行编写 | 本轮正在生成作者与预览计划，不移动或覆盖未提交文档；与Runtime迁移、技能作者change分别维护。 |
| [migrate-posegraph-to-flowcanvas-runtime](changes/migrate-posegraph-to-flowcanvas-runtime/) | 保留：并行草案 | 核查时目录尚缺proposal/spec/tasks，当前严格校验失败；不能为消除红项删除正在创建的计划。 |
| [rebuild-btsmtl-preview-with-scene-play](changes/rebuild-btsmtl-preview-with-scene-play/) | 保留：重新对齐范围 | 既有独立分支有Scene Play成果；新的Pose编辑预览change与其有交集，先交接具体Pose任务，不删除技能、场景生命周期和正式观察部分。 |
| [rebuild-character-camera-from-zzz](changes/rebuild-character-camera-from-zzz/) | 保留：已接入部分 | 资源及主线调用已有，完整来源类型、生命周期及最新画面证据未关闭；1/67状态不能按实现量解释。 |
| [rebuild-character-foot-ik-from-zzz-pik](changes/rebuild-character-foot-ik-from-zzz-pik/) | 保留：新算法迁移 | 第9节明确与旧stabilize算法冲突；只保留一个目标算法方向，完整查询／支撑／脚／骨盆／Solver映射仍未交付。 |
| [refactor-btsmtl-authoring-architecture](changes/refactor-btsmtl-authoring-architecture/) | 保留：主线实施 | 控制、Skill、Timeline、Document已有代码，事件隔离、共享SubTree、恢复及发布仍有缺口。本轮补回已安装空路径Scenario，不能被旧delta覆盖掉。 |
| [refactor-btsmtl-flowcanvas-authoring](changes/refactor-btsmtl-flowcanvas-authoring/) | 保留：当前技能作者计划 | 由并行任务从旧总计划拆出并提交9c447aa4b；不自动取消BTSMTL主重构中的Runtime、Skill及事务工作。 |
| [refactor-character-pose-graph-architecture](changes/refactor-character-pose-graph-architecture/) | 保留：待逐项交接 | 128/195原快照包含Runtime已做部分和未完成作者／预览部分；新FlowCanvas计划不等于旧整体失效。并行修改范围不覆盖。 |
| [refactor-foot-ik-diagnostic-sampling](changes/refactor-foot-ik-diagnostic-sampling/) | 保留：领域产品验证 | 22/29；剩余Disabled/Capture真实产物与采样证明，保留领域适配职责，不与通用框架一起盲合并。 |
| [replace-btsmtl-ai-with-behavior-designer](changes/replace-btsmtl-ai-with-behavior-designer/) | 保留：旧AI尚在主线 | 主线仍有AIControllerDefinition及AICharacterControlSource；不能先删AI现行spec。分支成果要完成正式接入、多Actor及旧域退役。 |
| [stabilize-character-foot-path-and-landing](changes/stabilize-character-foot-path-and-landing/) | 保留历史成果，冲突任务待交接 | 106/131；PIK design第9节已点名替代6.18／6.24／6.31–6.35、7.5–7.7及10.9旧采样入口。不能并行执行相反算法，也不能丢掉已接受修复、失败记录及未交接公共任务。 |
| [unify-versioned-development-control-center](changes/unify-versioned-development-control-center/) | 保留：跨仓库对账 | Center实现已迁D:/Unity_Project_1/3C-Development-Center；本仓0/38不是全项目零实现。需按外部仓库与3C消费结果核对，保留网络、版本与产品范围。 |

## 现行spec重点问题

| 范围 | 核对事实 | 处置／未关闭问题 |
|---|---|---|
| Document v4／v5 | agent-ai-controller-synthesis、agent-character-controller-synthesis、btsmtl-agent-authoring-document-sync仍有v4要求；源码AgentAuthoringModels.Version为v5，主delta正在迁移。 | 当前合同滞后；先完成v5完整分片／owner／失败恢复与两domain组合对账，不全局替换版本字符串或恢复v4 reader。 |
| 控制与旧角色RootTree | Corin Definition已声明character.corin.control，控制使用按Actor实例化的UnityHFSM；project与部分current仍以角色RootTree组织描述。 | 控制代码入口与技能图分开；HFSM tick时钟绑定缺口见本次会话源码检查，不能把普通控制接入当库完整验收。 |
| Pose编辑器 | current character-presentation-pose-graph仍要求GraphAuthoringCanvasView；726315c59已删除旧Pose画布并切换CanvasCore。 | 由当前Pose作者／Runtime／预览三个范围明确交接；新FlowCanvas Runtime仍是计划，不能提前宣布旧Program已退役。 |
| MM状态 | project曾称可选MM合同未安装且引用不存在的active change；实际模块spec及2026-07-24归档存在。 | 本轮修正文档入口，仍不宣称Corin内容已接入。具体资产字段名称已演变，后续以正式Binding闭包核对，不能长期引用旧字段。 |
| Blend Space缺失spec | 原proposal宣称已同步，但独立能力目录缺失；typed Binding、Phase和分析依赖已分散存在于其它spec。 | 本轮建立独立能力合同，保留后来已修改的资源／Foot数据边界；用户取消独立演示，原change归档。 |
| Foot新旧算法 | stabilize的旧响应／骨盆方案与PIK design第9节明确相反。 | 不是删除整个Foot spec：唯一Goal／FBBIK／提交等正确边界要保留，算法要求在实际替换后同步；先按原任务交接冲突项。 |
| Foot评分与存储 | current scoring/storage已指向独立离线分析，源码已有QualityScorePublisher。 | 保留，不能因为旧Analyzer删除就删业务评分合同；schema-driven的5.6需用现有报告核对后更新勾选。 |
| AI与训练敌人 | 主线旧AI类型仍存在；character-targeted-motion-warp-demo要求训练敌人，主重构记录已有TrainingEnemy退役。 | AI类型存在与具体演示资产存在分别核对；不能把Behaviour Designer分支成果当旧AI已退役，也不能为满足旧spec恢复已删训练资产。 |
| 相机 | current已明确本地表现边界，active扩大原始ZZZ资源类型和生命周期。 | 保留原local-only／提交边界；有代码接线不证明所有镜头资源已在动作中播放。 |
| Center与诊断 | 三个外部框架／产品／领域change责任不同，部分实现已迁独立仓库。 | 本仓spec仍需表达3C消费合同，不能因源码搬出就删除；旧Center spec-audit里的batchmode禁令已被根AGENTS替代，不再作为当前阻塞。 |
| 旧delta覆盖新场景 | 离散楼梯四个场景仅改标题；主Document delta漏掉刚安装的空共享路径场景。 | 本轮恢复场景身份及新增场景，保留现有正确语义；两change strict均通过。 |
| 不存在的计划入口 | project的add-linked-pose-interface-runtime及历史character-pipeline-serial-execution.md引用无法作为当前活跃入口。 | 保留功能事实与Git历史，后续按实际owner整理；不存在的文件不能成为等待依赖。 |

## 全部现行spec索引

关联change表示它声明要修改该合同，不表示已实施。没有关联change的spec保持现有业务合同，不因名称旧或文件小而删除。每条只登记用途与影响面；上表标出的差异仍需对应owner完成内容对账。

| spec | 用途 | Requirement数 | 本仓活跃delta |
|---|---|---:|---|
| [agent-ai-controller-synthesis](specs/agent-ai-controller-synthesis/spec.md) | 定义Agent通过统一Document v4目录包、Mutation、Validator与固定生命周期工具读取、修改和验证BTSMTL AI Controller资产的正式合同。 | 3 | refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [agent-character-controller-synthesis](specs/agent-character-controller-synthesis/spec.md) | 定义Agent在编辑器内通过Document v4目录包、canonical Snapshot、Mutation Compiler、Validator与Report生成并修复正式BTSMTL角色控制器资产的唯一链路。 | 26 | refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [btsmtl-agent-authoring-document-sync](specs/btsmtl-agent-authoring-document-sync/spec.md) | 定义BTSMTL Agent Authoring Document v4目录包的分片、规范编码、Gameplay/Timeline/Presentation声明式Mutation、事务apply与反向发布合同。 | 24 | add-character-pose-correction、decouple-timeline-from-skill、rebuild-btsmtl-preview-with-scene-play、rebuild-character-camera-from-zzz、refactor-btsmtl-authoring-architecture、refactor-btsmtl-flowcanvas-authoring、replace-btsmtl-ai-with-behavior-designer |
| [btsmtl-agent-authoring-mcp-bridge](specs/btsmtl-agent-authoring-mcp-bridge/spec.md) | 定义现有Unity MCP到Agent authoring service的薄桥接，以及Document checkout、rebase、dry-run、事务apply、validate与精确Build的统一边界。 | 11 | decouple-timeline-from-skill、refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [btsmtl-ai-controller-authoring](specs/btsmtl-ai-controller-authoring/spec.md) | 定义 AI Controller 独立 Definition、Tree、Graph capability、Blackboard、Perception 与 Intent authoring 的唯一正式边界。 | 5 | refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [btsmtl-bt-edge-condition-decorators](specs/btsmtl-bt-edge-condition-decorators/spec.md) | 定义 BT Composite output edge 的 ConditionRuleGraph、AbortPolicy、抢占运行时和编辑器创作闭环，并删除 IfNode 条件分裂路径。 | 10 | 无 |
| [btsmtl-compiled-simulation-program](specs/btsmtl-compiled-simulation-program/spec.md) | 定义 Character authoring 经 validated Semantic IR artifact 和显式 Numeric Target 生成不可变 portable Simulation Program、ProgramCatalog 与 Presentation Projection 的正式编译和发布边界。 | 23 | decouple-timeline-from-skill、rebuild-character-camera-from-zzz、refactor-btsmtl-authoring-architecture、refactor-character-pose-graph-architecture |
| [btsmtl-componentized-node-authoring](specs/btsmtl-componentized-node-authoring/spec.md) | 定义 BTSMTL 节点组合创作主链路：`BaseNode` 通过 `NodeModule` 扩展创作能力，节点字段和模块字段通过同一字段访问器暴露，属性端口继续使用 BTSMTL 原生 `PropertyPort` / `PropertyEdge`，不恢复 Workbench 或并行端口协议。 | 7 | 无 |
| [btsmtl-gameplay-semantic-ir](specs/btsmtl-gameplay-semantic-ir/spec.md) | 定义 Character authoring 到 numeric-neutral Gameplay Semantic IR 的唯一 Frontend、canonical artifact、稳定身份和非运行时边界，使不同 Numeric Target 共享业务语义而不共享目标 ABI。 | 14 | decouple-timeline-from-skill、refactor-btsmtl-authoring-architecture |
| [btsmtl-graph-core](specs/btsmtl-graph-core/spec.md) | 定义 BTSMTL 图底座：`BaseGraph` 承载唯一图结构数据、编辑操作和运行上下文；`BaseTree : BaseGraph` 是普通 C# 图数据类型；`BaseTreeAsset` 作为 Unity 资产和编辑器入口持有一份 `BaseTree` 数据；节点、边、模块、端口和默认私有下钻 Graph 都内联在所属 owner 中，只有显式复用时才使用 shared asset；执行生命周期留在 `RunnableTree`、`StateMachineGraphRuntime`、`TimelineNode` 等上层 Module。 | 19 | decouple-timeline-from-skill、refactor-btsmtl-authoring-architecture、refactor-btsmtl-flowcanvas-authoring、replace-btsmtl-ai-with-behavior-designer |
| [btsmtl-graph-data-catalog-authoring](specs/btsmtl-graph-data-catalog-authoring/spec.md) | 定义 Tree Inspector 中唯一的 Graph Data Catalog：它从正式 InputProfile 与 Pipeline Blackboard declaration 即时投影可见数据、所有权和编辑能力，并承载 Blackboard fact projection 的作者入口。 | 10 | 无 |
| [btsmtl-input-action-node-authoring](specs/btsmtl-input-action-node-authoring/spec.md) | 定义 Unity Input System 的 raw InputAction ValueNode 链路：它服务通用 BTSMTL 调试、简单条件和非角色语义读取，绑定数据放在 `NodeModule`，运行时只从正式 input value source 读取值。角色 gameplay 输入主链路必须使用 `CharacterInputProfile -> input value/action request 信息节点`，本 spec 不新增输入专用 Graph、Workbench 路径、object fallback 或第二套角色输入配置。 | 5 | 无 |
| [btsmtl-node-interruption-lifecycle](specs/btsmtl-node-interruption-lifecycle/spec.md) | 定义 RunnableNode 的行为结果、运行阶段、自然完成、graceful stop 和 force stop 分层协议，以及 Composite、State 和 Timeline 的统一停止传播边界。 | 6 | 无 |
| [btsmtl-runnable-timeline-node](specs/btsmtl-runnable-timeline-node/spec.md) | 定义 Graph 驱动 Timeline 的正式节点链路：`TimelineNode : RunnableNode` 默认拥有 inline `TimelineData`，仅在作者显式选择复用时引用 shared `TimelineAsset`；运行上下文来自所属 `BaseGraph`，不新增 Timeline 状态节点、场景对象 fallback 或并行端口协议。 | 10 | decouple-timeline-from-skill、refactor-btsmtl-authoring-architecture |
| [btsmtl-runtime-diagnostics](specs/btsmtl-runtime-diagnostics/spec.md) | 定义 Authoring identity、Program source map、Simulation/Presentation Trace、RuntimeDebugSession 与编辑器视图之间的只读诊断链路。 | 11 | decouple-timeline-from-skill、refactor-btsmtl-authoring-architecture |
| [btsmtl-semantic-ir-inspection](specs/btsmtl-semantic-ir-inspection/spec.md) | 定义 Unity Editor 与普通 DotNet Reader 对 canonical Semantic IR artifact 的只读检查、身份校验和精确 Authoring SourceMap 导航能力。 | 4 | refactor-btsmtl-authoring-architecture |
| [btsmtl-sm-node-authoring](specs/btsmtl-sm-node-authoring/spec.md) | 定义 BTSMTL 状态机创作链路：普通行为图通过 `StateMachineNode` 进入 `StateMachineGraph`；创建 `StateMachineNode` 时默认自动拥有并绑定私有 inline `StateMachineGraph` 数据，用户不需要先手动创建或拖拽状态机资产；`StateMachineGraph` 只表达状态关系；`StateNode` 和 Transition edge 是状态机图内联数据；状态具体行为在 `StateNode` resolved `SubTree` 或 `StateBehaviorSubTree` 中编辑。 | 24 | refactor-btsmtl-authoring-architecture |
| [btsmtl-timeline-animation-authoring-surface](specs/btsmtl-timeline-animation-authoring-surface/spec.md) | 定义Timeline Editor本地作者表面、typed session、可选Topology/Runtime Debug能力和显式领域工具的唯一边界。 | 4 | decouple-timeline-from-skill |
| [btsmtl-timeline-editor-preview](specs/btsmtl-timeline-editor-preview/spec.md) | 定义BTSMTL有限Action Timeline编辑器预览的正式链路：`TimelinePreviewSession`通过typed Action adapter接入唯一`AnimationPreviewRuntime`，复用Action lifecycle、AnimationSlot、Transition Routing、source backend与Pose Plan；持续Locomotion由Pose Graph Fact Preview负责，不恢复旧`TimelinePlayer`、BaseLocomotion Timeline、共享Playback总管或独立PlayableGraph。 | 17 | rebuild-btsmtl-preview-with-scene-play、rebuild-character-camera-from-zzz |
| [btsmtl-tree-inspector-information-architecture](specs/btsmtl-tree-inspector-information-architecture/spec.md) | 定义Tree Workspace中左侧Data、右侧Details、运行时观察和内部模块的正式信息架构。 | 5 | 无 |
| [character-action-activation-flow](specs/character-action-activation-flow/spec.md) | 定义 Graph 如何建立 ActionInstance、显式传递 Action Context，并以唯一准入与 lifecycle 规则驱动 Timeline、窗口、Motion、Cue 和 GameplayResult。 | 16 | refactor-btsmtl-authoring-architecture |
| [character-action-animation-authoring-workspace](specs/character-action-animation-authoring-workspace/spec.md) | 定义有限 Action 动画的统一作者工作面，聚合 Action、Timeline、原生 AnimationClip、Slot、预览与调试关系，并将每次修改交给对应正式 Owner。 | 6 | rebuild-btsmtl-preview-with-scene-play、refactor-btsmtl-authoring-architecture |
| [character-action-authoring-closure](specs/character-action-authoring-closure/spec.md) | 定义动作 profile、Graph activation request、Timeline Decision TreeClip 时间事实、Blackboard fact projection 和 Runtime Debug 的作者闭环。 | 18 | refactor-btsmtl-authoring-architecture |
| [character-action-instance-runtime](specs/character-action-instance-runtime/spec.md) | 定义 compiled Action operation 与 `CharacterSimulationState` Action slots 的动作事务语义：动作身份通过 `ActivateActionInstance` operation、Action Context 和 lifecycle transition 表达，不通过节点身份、ActionModule、AbilityBody、ActionTree 或静态结构归属表达。 | 13 | refactor-btsmtl-authoring-architecture |
| [character-animation-blend-space](specs/character-animation-blend-space/spec.md) | 连续参数动画样本混合；通用能力保留，独立演示取消。 | 12 | 无 |
| [character-animation-blend-stack](specs/character-animation-blend-stack/spec.md) | 定义显式 Blend Stack 节点的独立状态、逐骨骼混合权重、容量压缩与 source 生命周期，保持中断连续性并避免隐式创建第二播放器。 | 10 | 无 |
| [character-animation-clip-authoring](specs/character-animation-clip-authoring/spec.md) | 定义原生AnimationClip作为动画素材、注册表现曲线、Unity Animation Window导航、Preview Target与Character Presentation装配之间的唯一作者合同，并固定其构建与运行时边界。 | 7 | 无 |
| [character-animation-foot-analysis-artifact](specs/character-animation-foot-analysis-artifact/spec.md) | 定义单AnimationClip脚分析的Editor-only规范产物、精确缓存身份、Definition消费与Runtime Projection边界。 | 14 | 无 |
| [character-animation-layer-runtime](specs/character-animation-layer-runtime/spec.md) | 定义Presentation Fact、PoseState source、有限Action playback、显式transition owner、source backend和最终Pose之间的唯一角色动画运行链。 | 13 | add-character-pose-correction、rebuild-btsmtl-preview-with-scene-play |
| [character-animation-pipeline](specs/character-animation-pipeline/spec.md) | 定义Gameplay Timeline、Presentation Fact、state-local Pose source、有限Action playback、唯一编译Pose Plan与预分配表现帧事务之间的角色动画输出链。 | 18 | add-schema-driven-diagnostic-analysis、rebuild-btsmtl-preview-with-scene-play、refactor-character-pose-graph-architecture、stabilize-character-foot-path-and-landing |
| [character-animation-presentation-authoring](specs/character-animation-presentation-authoring/spec.md) | 定义角色动画表现配置的唯一作者边界：CharacterPipelineDefinition只引用CharacterAnimationPresentationProfile，Profile唯一引用Pose Graph、state-local Pose source、有限Action producer、node-local Policy、Rig、FullBodyIK Profile与Foot Analysis，Profile Inspector和Pose Graph Workspace提供各自唯一入口，并由编译链生成CharacterPresentationProjection。 | 19 | add-character-pose-correction、refactor-btsmtl-authoring-architecture |
| [character-animation-selection-runtime](specs/character-animation-selection-runtime/spec.md) | 定义持续Pose source、有限Action playback、显式Player、source-local时间映射、连续性、释放和Preview之间的唯一表现边界。 | 9 | add-acl-animation-runtime、rebuild-btsmtl-preview-with-scene-play、refactor-character-pose-graph-architecture |
| [character-animation-transition-routing-module](specs/character-animation-transition-routing-module/spec.md) | 定义独立 Transition Routing 模块的精确规则编译、Standard Blend／Inertialization 决策和 capture／release 协议；模块不计算 Pose 或拥有调用方状态。 | 9 | 无 |
| [character-camera-pipeline](specs/character-camera-pipeline/spec.md) | 定义角色本地相机从 committed PresentationCommand、CameraSequenceRequest、CameraShakeRequest、响应策略和目标绑定到 CharacterSimulationPresentationRuntime、CameraFramePlan 与 ICameraRigAdapter 的唯一表现链路。 | 13 | add-discrete-stair-presentation、rebuild-character-camera-from-zzz |
| [character-equipment-feature-authoring](specs/character-equipment-feature-authoring/spec.md) | 定义角色装备 Slot、Route、Equipment、Feature、参数与组合根的唯一作者配置模型。 | 8 | refactor-btsmtl-authoring-architecture |
| [character-equipment-presentation](specs/character-equipment-presentation/spec.md) | 定义装备 VisualBinding、Projection payload 与 Unity 外观实例的单向表现链路。 | 6 | 无 |
| [character-equipment-runtime](specs/character-equipment-runtime/spec.md) | 定义装备 catalog、typed state、切换事务、Feature Host 与 Action、Tag、GameplayEffect 集成的唯一运行时语义。 | 11 | refactor-btsmtl-authoring-architecture |
| [character-foot-diagnostic-scoring](specs/character-foot-diagnostic-scoring/spec.md) | 规定 Foot 七维质量评分的去重、固定权重、接触分域、缺失证据和历史版本解释规则。分数仅作辅助比较，不代表视觉验收；实现由唯一离线 Foot Analysis 的当前 Plan 拥有，不绑定旧 Analyzer 或 Publisher。 | 4 | 无 |
| [character-foot-diagnostic-storage](specs/character-foot-diagnostic-storage/spec.md) | 规定 Foot 诊断报告的事实复用、紧凑明细、索引、身份与历史证据保存规则。旧工具已经删除，当前分析实现由独立 Schema-driven Analysis 承接；本规范不恢复旧 Reader 或承诺采样停止后自动分析。 | 4 | 无 |
| [character-foot-placement-presentation](specs/character-foot-placement-presentation/spec.md) | 定义Corin Landing Prediction、Ground Path、Foot Lifecycle、Support、Pelvis、Goal Contribution与唯一FinalIK FBBIK之间的正式表现边界，不固定Foot模块内部类名和聚合方式。 | 21 | add-schema-driven-diagnostic-analysis、rebuild-character-foot-ik-from-zzz-pik、refactor-character-pose-graph-architecture、refactor-foot-ik-diagnostic-sampling、stabilize-character-foot-path-and-landing |
| [character-gameplay-effect-authoring](specs/character-gameplay-effect-authoring/spec.md) | 规定 Character Gameplay Effect 的唯一配置入口、稳定身份、引用闭包、有限数值校验和不可变 Runtime Definition 构建，保证 authoring 失败时直接阻止角色运行时创建，不产生默认配置或兼容链路。 | 6 | 无 |
| [character-gameplay-effect-integration](specs/character-gameplay-effect-integration/spec.md) | 规定 Gameplay Effect Program/State/operation 接入 Character Simulation 的唯一装配、固定 Tick、Self 命令、只读查询、事实投影和跨角色边界，保证 Unity Host 只做编译与装配，不复制 GE 规则或网络模型职责。 | 9 | 无 |
| [character-gameplay-pipeline-closure](specs/character-gameplay-pipeline-closure/spec.md) | 定义角色 Gameplay 管线闭环：输入、compiled Graph/StateMachine/Timeline/Action/Effect operation、Character/World state、batch WorldSolver、Committer、Presentation 和 Runtime Debug 必须走同一条正式 Program/Session 主线，不恢复旧 SO/config、对象解释器、旧播放器或 demo 临时桥接。 | 12 | 无 |
| [character-input-node-authoring](specs/character-input-node-authoring/spec.md) | 定义角色输入语义进入 BTSMTL 图的 authoring 链路：`CharacterInputProfile` 中的 input value 和 action request 可以创建正式信息节点，Graph 读取 input value 和 request buffer，不依赖 InputAction 显示名、场景搜索或输入专用 Graph。 | 8 | 无 |
| [character-input-pipeline](specs/character-input-pipeline/spec.md) | 定义角色输入管线：`CharacterInputProfile` 将 Unity InputAction映射为 gameplay input value和 action request，`UnityCharacterSimulationInputAdapter`负责表现帧采样并生成 portable `CharacterSimulationInput`，Program state slots负责 request buffer与消费；预测历史只属于需要它的 Network Model SnapshotParticipant。 | 13 | replace-btsmtl-ai-with-behavior-designer |
| [character-motion-matching-presentation-module](specs/character-motion-matching-presentation-module/spec.md) | 定义 Motion Matching 表现模块对轨迹、意图、查询、选择、历史及生命周期的唯一所有权；仅在 Projection 配置合法 MM payload 时装配。 | 8 | rebuild-btsmtl-preview-with-scene-play |
| [character-motion-semantics](specs/character-motion-semantics/spec.md) | 定义角色运动的唯一执行语义：Target operation产生`SimulationMotionContribution`，Target Motion accumulator先解析channel并执行Program Motion Modifier，再生成`ResolvedGameplayMotion`；Target Body Motion Integrator执行Prepare后生成唯一`CharacterMotionRequest`，WorldSolver返回实际body result，Body Motion Finalize提交垂直动力状态，Program Finalize提交`CharacterBodySample`与Motion GameplayFact；Unity Transform只在Solver/Presentation边界对齐。 | 17 | 无 |
| [character-motion-simulation-boundary](specs/character-motion-simulation-boundary/spec.md) | 定义 Character Program 产生 portable motion request、Simulation Session 批量调用唯一 WorldSolver、World state 保存逻辑 body 真值以及 Network Model 装配独立运动后端的边界。 | 10 | 无 |
| [character-motion-warp-authoring](specs/character-motion-warp-authoring/spec.md) | 定义MotionWarp Timeline authoring、稳定源MotionCurve绑定、目标姿态、累计进度曲线与发布前拒绝规则，使Warp只作为compiled Program中的正式Motion Modifier存在。 | 8 | 无 |
| [character-pipeline-blackboard](specs/character-pipeline-blackboard/spec.md) | 定义角色 Pipeline Blackboard 的声明、类型、作用域、运行时读写、ConditionRuleGraph 读取和 GameplayFact 投影边界。 | 17 | refactor-btsmtl-authoring-architecture |
| [character-pipeline-definition-authoring](specs/character-pipeline-definition-authoring/spec.md) | 定义 CharacterPipelineDefinition 作为角色 authoring 配置装配根的纯引用边界、紧凑 Inspector，以及 Animation Presentation Profile 与 generated Program/Projection 的所有权和状态入口。 | 7 | 无 |
| [character-pipeline-runtime](specs/character-pipeline-runtime/spec.md) | 定义 CharacterPipelineHost的 Unity Actor registration/Presentation边界，以及 portable Program Runtime、compiled Session Pipeline、Execution Backend、WorldSolver、Committer和 Presentation组成的唯一角色运行链。 | 23 | 无 |
| [character-pose-graph-runtime-architecture](specs/character-pose-graph-runtime-architecture/spec.md) | 定义角色Pose Graph运行时的唯一模块归属、帧事务、状态寿命、在线调参、诊断与最终姿态发布边界，确保架构重构不改变既有动画和IK业务行为。 | 15 | add-acl-animation-runtime、add-schema-driven-diagnostic-analysis、rebuild-btsmtl-preview-with-scene-play、refactor-character-pose-graph-architecture |
| [character-pose-inertialization](specs/character-pose-inertialization/spec.md) | 定义Pose Graph局部分支惯性化的正式节点、typed discontinuity输入、Pose输出、策略覆盖、每骨骼残差、原子rebase、阶段边界与只读诊断，并禁止隐藏全局连续化路径。 | 9 | rebuild-btsmtl-preview-with-scene-play |
| [character-pose-plan-compilation](specs/character-pose-plan-compilation/spec.md) | 定义角色Pose Graph从作者数据到不可变Program Image的唯一编译合同，包括节点定义、不可变Pass链、类型化Operation ABI、调度、容量规划与稳定诊断来源。 | 13 | add-character-pose-correction、refactor-btsmtl-authoring-architecture、refactor-character-pose-graph-architecture |
| [character-presentation-interpolation](specs/character-presentation-interpolation/spec.md) | 定义逻辑运动学轨迹到表现根姿态的采样与有界纠偏边界，以及visual Timeline重采样、动画播放生命周期、显式Player时间连续性与Pose Plan独立连续推进的职责分离。 | 15 | add-discrete-stair-presentation、decouple-timeline-from-skill |
| [character-presentation-pose-graph](specs/character-presentation-pose-graph/spec.md) | 定义Character Presentation Pose Graph的正式数据模型、编译边界、作者工作区、Preview、Live Debug和Pose Watch。 | 19 | add-acl-animation-runtime、add-character-pose-correction、rebuild-btsmtl-preview-with-scene-play、refactor-btsmtl-authoring-architecture、refactor-character-pose-graph-architecture |
| [character-root-motion-curves](specs/character-root-motion-curves/spec.md) | 定义 root motion 曲线资产和烘焙链路：从指定 `AnimationClip` 和采样 Prefab 生成 `RootMotionCurveAsset`，保存累计位移与 yaw 曲线，作为离线 authoring 数据供作者生成、检查和重烘焙。Compiler MUST将 Timeline 正式引用的曲线降低为 Program constant 与 MotionCurve operation，Runtime MUST经 CharacterMotionRequest 和 WorldSolver 应用，不从 AnimationClip 自动采样，也不恢复旧 BBB motion 配置或 footphase/body claim 数据源。 | 9 | 无 |
| [character-simulation-kernel](specs/character-simulation-kernel/spec.md) | 定义 Numeric Target 专属 SimulationKernel 的 Evaluate/Finalize、Character/World state、Session 四阶段执行、Snapshot 和稳定 EventId 输出合同。 | 32 | decouple-timeline-from-skill、refactor-btsmtl-authoring-architecture |
| [character-stair-surface-authoring](specs/character-stair-surface-authoring/spec.md) | 定义连续楼梯Gameplay Traversal Ramp、可见Foot Placement踏面、过渡地面、显式作者操作与独立Step能力课程之间的唯一作者所有权和校验合同。 | 7 | add-discrete-stair-presentation |
| [character-state-interruption-authoring](specs/character-state-interruption-authoring/spec.md) | 定义State Transition与父Tree abort共用通用Runnable stop、source-exit、OnExit、Timeline cancel、Action lifecycle和Presentation Adapter的创作闭环。 | 6 | 无 |
| [character-state-timeline-authoring-loop](specs/character-state-timeline-authoring-loop/spec.md) | 定义Corin Gameplay StateMachine、有限Action Timeline与Presentation PoseState的唯一职责边界：BTSMTL只拥有Gameplay控制、Motion、Action、Window、Cue与有限Action时间，持续Locomotion姿态只由Presentation Fact、PoseStateMachine和state-local source选择。 | 14 | refactor-btsmtl-authoring-architecture |
| [character-targeted-motion-warp-demo](specs/character-targeted-motion-warp-demo/spec.md) | 定义Standalone Gameplay中Corin玩家、同Session训练敌人、显式目标输入与五段攻击MotionWarp的正式演示闭环，同时明确训练AI不冒充完整敌人AI、命中或伤害系统。 | 4 | refactor-character-pose-graph-architecture |
| [character-toon-rendering](specs/character-toon-rendering/spec.md) | 定义 Corin 在现有 URP 中使用 ZZZMiyabi Toon 的唯一材质接入、纹理与色表语义、面部朝向输入和场景主光边界；不把当前角色接入等同于完整复刻 ZZZ 原始渲染。 | 4 | 无 |
| [character-vertical-body-motion](specs/character-vertical-body-motion/spec.md) | 定义角色玩法 Motion 完成后、WorldSolver 前后的唯一垂直动力积分、状态提交、能力校验，以及网络与回滚闭包。 | 9 | 无 |
| [client-build-artifact-layout](specs/client-build-artifact-layout/spec.md) | 定义 Content、普通 Player 与 Network 构建分区，Network Product 在各自固定根下保存不可变 Candidate，并将运行配置与日志放入独立 RunLogs。 | 8 | unify-versioned-development-control-center |
| [deterministic-kcc-world-solver](specs/deterministic-kcc-world-solver/spec.md) | 定义 DeterministicRollback Fixed Target 使用的确定性胶囊角色世界求解合同，包括版本化静态碰撞世界、连续查询、稳定地面、坡面、台阶、墙面滑动、Actor 接触和原子批量提交。 | 24 | add-discrete-stair-presentation |
| [deterministic-rollback-network-model](specs/deterministic-rollback-network-model/spec.md) | 定义独立 DeterministicRollback 网络模型的 Fixed Program、canonical input、history、restore/replay、snapshot、hash、output disposition 和确定性能力锁定边界。 | 13 | refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [deterministic-rollback-relay-product](specs/deterministic-rollback-relay-product/spec.md) | 定义纯 .NET Dedicated Relay 的网络职责、Candidate 静态身份与 Run 配置、精确工具和产物闭包，以及 Relay、GM 和两个 Unity Client 的开发运行边界。 | 4 | replace-btsmtl-ai-with-behavior-designer、unify-versioned-development-control-center |
| [deterministic-rollback-two-client-demo](specs/deterministic-rollback-two-client-demo/spec.md) | 定义两个 Unity Client、纯 .NET Relay 与独立只读 GM 组成的 Rollback Demo，保持精确 Candidate／Run 身份、选择性输入时序和现有 Gameplay 边界。 | 4 | replace-btsmtl-ai-with-behavior-designer |
| [dotrecast-authoritative-server-backend](specs/dotrecast-authoritative-server-backend/spec.md) | 定义 Fantasy 进程内 DotRecast Authority Scene 如何复用 portable ServerAuthoritative Host、共享 DotRecast WorldSolver 与独立产品装配，形成三进程权威同步纵切。 | 12 | 无 |
| [dotrecast-navigation-world-solver](specs/dotrecast-navigation-world-solver/spec.md) | 定义 Unity 与普通 .NET 共享的 DotRecast 导航世界资产、查询、Actor 接触和 ResolveBatch 合同，使 Prediction 与 Authority 使用同一 portable 求解语义。 | 10 | 无 |
| [fantasy-unity-authoritative-session](specs/fantasy-unity-authoritative-session/spec.md) | 定义 Fantasy Gate/Room、外部 Unity Authority Worker 与 Unity Client 之间的控制面、UDP Gameplay 数据面、固定 Roster 和四进程权威 Session 生命周期。 | 22 | 无 |
| [float32-session-runtime-launcher](specs/float32-session-runtime-launcher/spec.md) | 定义 Float32 Pipeline Runtime Package、Prepared Source 与 Runtime Launcher 的唯一启动边界，使公共 Composer 无需识别具体 Network Model。 | 5 | 无 |
| [gameplay-ai-control-source](specs/gameplay-ai-control-source/spec.md) | 定义从已提交 Actor Observation 到 AI Intent，再到 CharacterSimulationInput 的唯一 Local AI 控制输入链路。 | 5 | replace-btsmtl-ai-with-behavior-designer |
| [gameplay-attribute-runtime](specs/gameplay-attribute-runtime/spec.md) | 定义 Gameplay Attribute 的稳定身份、有限数值、基础值与当前值、revision、modifier 聚合和 Gameplay Effect transaction 写入边界。 | 6 | 无 |
| [gameplay-behavior-policy-model](specs/gameplay-behavior-policy-model/spec.md) | 定义 Gameplay 行为身份目录：Action、通用 Stream/Event 与 Gameplay Effect 使用稳定 BehaviorId、BehaviorKind、Tag 和调试元数据进入同一 Program catalog；网络策略继续归具体 Network Model，不由 BehaviorProfile 执行或隐式推导。 | 6 | 无 |
| [gameplay-effect-runtime](specs/gameplay-effect-runtime/spec.md) | 规定独立 Gameplay Effect 通用运行时的定义、事务、生命周期、数值安全和 ChangeSet 边界，使角色等业务模块能在固定逻辑 Tick 中复用同一套 Tag、Attribute 与 Effect 规则，而不把 Character、网络模型或表现职责带入 GE。 | 13 | 无 |
| [gameplay-network-model-boundary](specs/gameplay-network-model-boundary/spec.md) | 定义唯一 SimulationSessionHost、GameplayNetworkModelDefinition、model-owned Session Source/Pipeline、Actor roster与具体 protocol/history/endpoint实现之间的插件边界。 | 10 | 无 |
| [gameplay-network-test-build-workflow](specs/gameplay-network-test-build-workflow/spec.md) | 定义三个 Network Test Product 共享的 Editor Build Workflow，以干净源码、schema v3、精确产物与工具闭包发布不可变 Candidate，并与 Run 实例创建分离。 | 4 | replace-btsmtl-ai-with-behavior-designer、unify-versioned-development-control-center |
| [gameplay-simulation-pipeline](specs/gameplay-simulation-pipeline/spec.md) | 定义 Simulation Pipeline 从显式 Definition 编译为不可变四阶段计划，并以唯一 Schedule、World Step、Egress 和原子 Commit 执行 Gameplay Session。 | 11 | refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [gameplay-simulation-session-composition](specs/gameplay-simulation-session-composition/spec.md) | 定义 Unity gameplay Session 的唯一装配 owner、不可变 Actor roster、正式 Tick 生命周期、失败关闭与资源销毁边界。 | 14 | rebuild-btsmtl-preview-with-scene-play、refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [gameplay-tag-runtime](specs/gameplay-tag-runtime/spec.md) | 定义 Gameplay Tag 的稳定身份、Tag Container、requirement query、Gameplay Effect 授予与移除以及 Action operation 只读查询边界。 | 6 | 无 |
| [gameplay-tick-system](specs/gameplay-tick-system/spec.md) | 定义 gameplay 层统一 tick 系统：`GameplayTickSystem` 区分 `LocalLogicTick`、`RenderFrame` 和模型输入中的 `ServerTick`，并分别通过 render input、logic 和 presentation target 接口调度正式 Session 与表现消费者。 | 8 | 无 |
| [graph-authoring-domain-framework](specs/graph-authoring-domain-framework/spec.md) | 定义BTSMTL Gameplay Graph、AI Graph与Character Presentation Pose Graph共享的唯一作者交互框架，同时保持各领域数据、Mutation、Validator、Compiler与Runtime语义隔离。 | 9 | rebuild-btsmtl-preview-with-scene-play、refactor-btsmtl-authoring-architecture、refactor-btsmtl-flowcanvas-authoring、refactor-character-pose-graph-architecture、replace-btsmtl-ai-with-behavior-designer |
| [graph-authoring-editor-shell](specs/graph-authoring-editor-shell/spec.md) | 定义Tree、AI与Character Pose Graph共享的唯一图作者工作区外壳、区域装配、编辑器状态和显式重操作边界。 | 9 | rebuild-btsmtl-preview-with-scene-play、refactor-btsmtl-authoring-architecture、refactor-btsmtl-flowcanvas-authoring、replace-btsmtl-ai-with-behavior-designer |
| [network-test-runtime-product-boundary](specs/network-test-runtime-product-boundary/spec.md) | 定义 Network Model 与可独立构建和运行的 Product 边界，以及 schema v3 Candidate 的 runtime artifacts、Tool Bundles、Session Plan 和产品隔离规则。 | 5 | unify-versioned-development-control-center |
| [network-test-session-orchestration](specs/network-test-session-orchestration/spec.md) | 定义 Network Test 从干净源码构建不可变 Candidate、携带精确工具版本、以显式 Slot 启动独立 Run 及仅管理本次进程的完整合同。 | 6 | unify-versioned-development-control-center |
| [repository-ci-foundation](specs/repository-ci-foundation/spec.md) | 定义仓库 GitHub CI 对候选提交执行并行 Repository Policy、OpenSpec 严格校验和 portable 单元测试的只读基础合同。 | 7 | add-gameplay-performance-capture-workflow、unify-versioned-development-control-center |
| [rollback-gm-console](specs/rollback-gm-console/spec.md) | 定义独立 GM 文本工具的服务端命令、权限与 Relay 只读查询边界；工具版本绑定 Candidate，连接配置和访问凭据绑定唯一 Run，Unity Player 不接收工具凭据。 | 9 | 无 |
| [server-authoritative-host-portability](specs/server-authoritative-host-portability/spec.md) | 定义 ServerAuthoritative Authority Pipeline、Source、Control Transport 和 Launch Request 的 host-neutral 边界，使 Unity 与普通 .NET Host 复用同一模型运行语义。 | 6 | 无 |
| [server-authoritative-host-product-boundary](specs/server-authoritative-host-product-boundary/spec.md) | 定义 Unity Authority 与 DotRecast Authority 各自独立的 Fantasy Server 产品入口、模块闭包、配置、路由适配、构建 manifest 和运行边界。 | 8 | 无 |
| [server-authoritative-hybrid-sync-model](specs/server-authoritative-hybrid-sync-model/spec.md) | 定义 ServerAuthoritativeHybrid Network Model 对预测、权威修正、远端复制、历史、端点和 Session 级资源的唯一所有权。 | 12 | replace-btsmtl-ai-with-behavior-designer |
| [server-authoritative-prediction-correction-pipeline](specs/server-authoritative-prediction-correction-pipeline/spec.md) | 定义 ServerAuthoritative 客户端 Prediction Pipeline 的输入、时钟、历史、Baseline 合并、Restore/Replay、Output Disposition 和 Remote Presentation 语义。 | 22 | refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |
| [tengine-hotupdate-foundation](specs/tengine-hotupdate-foundation/spec.md) | 定义 TEngine、YooAsset、HybridCLR 和资源端点作为客户端启动、热更和资源底座的接入边界；TEngine 负责基础设施和流程装配，不替代 gameplay tick、角色管线或网络同步语义。 | 10 | 无 |
| [unity-authoritative-two-client-demo](specs/unity-authoritative-two-client-demo/spec.md) | 定义 Fantasy Gate、Unity Authority Worker、Client A 与 Client B 四进程 Demo 的显式装配、启动、双角色同步和诊断边界。 | 8 | 无 |
| [unity-simulation-assembly-ownership](specs/unity-simulation-assembly-ownership/spec.md) | 定义公共 Unity Simulation、具体 Network Model、可选 WorldSolver、客户端 Runtime 与 Editor 之间的程序集所有权和单向依赖。 | 5 | decouple-timeline-from-skill、rebuild-btsmtl-preview-with-scene-play、refactor-btsmtl-authoring-architecture、replace-btsmtl-ai-with-behavior-designer |

## 验证记录

- 初次全量strict：119项，115通过、4失败；96份current spec全部通过。失败包括楼梯／主Document的场景继承，以及两项并行创建的Pose计划缺少规划内容。
- 本轮修改后全量strict：118项，117通过、1失败；97份current spec全部通过。剩余失败为并行创建中的`migrate-posegraph-to-flowcanvas-runtime`，核查时尚无proposal/spec/tasks；不删除目录或伪造任务来取得全绿。`integrate-pose-flowcanvas-editor-preview`随后单独strict通过。
- Blend Space独立spec与最终delta正文规范化后相等；其change strict通过后归档。归档日志的specsUpdated=false只表示CLI没有再次写spec，人工同步已在归档前完成。
- 原膝角实验的失败与恢复记录保留在archive内experiment.md；五个撤销实现任务没有勾选为成功。

## 原始入口

- [项目口径](project.md)
- [主线闭环](changes/refactor-btsmtl-authoring-architecture/closure.md)
- [BTSMTL原任务](changes/refactor-btsmtl-authoring-architecture/tasks.md)
- [PIK新旧算法交接](changes/rebuild-character-foot-ik-from-zzz-pik/design.md)
- [Center既有交接审计](changes/unify-versioned-development-control-center/spec-audit.md)
