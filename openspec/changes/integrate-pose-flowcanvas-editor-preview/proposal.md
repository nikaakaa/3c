## Why

PoseGraph 已有 Program Image、Native 数据、Burst／Job、帧事务及唯一输出实现，但编辑器仍维护专用端口和交互，预览也缺少与正式执行结果一致的原生显示接入。本提案只为 Pose 复用 FlowCanvas 作者 UI，并把 Editor 预览接到现有编译运行链，保留已经完成的动画运行重构。

作者使用流程：编辑图 → 显式Build → 点Unity的Play → 角色照常运行 → 图窗口显示该角色的节点、权重和Pose Watch。技能窗口同理，但观察的是某一次技能释放；技能实施仍归独立提案。

## What Changes

- **BREAKING**：Pose 的唯一作者图使用领域 FlowGraph／FlowNode／BinderConnection，复用原生端口、节点布局、连线、选择、复制、Undo 和子图导航；Compiler 直接遍历这份图，不转换成旧 Pose 作者图、不保留可写镜像。
- 保留 Pose Compiler、Program Image、Execution View、Native 页、Worker 调度、Source／Constraint／Final Publication 与根帧事务。FlowCanvas 的端口 getter、FlowScript、协程和 Graph Update 不承担角色动画执行。
- 从唯一 Capability 和 Port Shape 注册 Pose 的字段与端口，保持 Local／Component 空间、动态端口、Graph Role 及稳定身份约束；原生写入原语进入既有 typed Mutation 和真实资产事务。
- 增加 Pose Editor 运行观察：Unity 进入 Play 后，角色由现有游戏入口正常运行；窗口绑定明确的角色实例，通过版本化 Source Map 读取已完成的节点、端口、状态、权重与 Pose Watch 结果。这里的“预览”只指看到实际运行结果。
- 原生状态外观通过只读观测数据绘制，不为连线闪烁启动一份 FlowCanvas 图；悬停、重绘和下钻不得重新计算动画、等待 Job 或写骨骼。无采样与零贡献必须区分。
- 明确未播放、等待目标、观察中、版本不匹配和目标结束状态；停止 Play、目标销毁或重载时解绑。Unity 自身暂停时保留最后完成帧；不新增场景启动、窗口播放／暂停／单步、节点指令断点、seek 或隐藏播放器。
- 按精确 Corin Definition 迁移全部可达 Pose 图、状态／规则及资源引用；保持现行 Document v5 的业务字段和五生命周期，不因 UI 接入引入另一套协议或无业务需求的 Macro/schema 迁移。
- 删除被替代的 Pose 画布、端口绘制和编辑入口；不删除仍有正式消费者的 Native／编译实现，不迁移技能、AI、网络或修改 Foot／IK 算法。

## Capabilities

### New Capabilities

- `pose-flowcanvas-editor-preview`：Pose 原生作者 UI、Play Mode 下编译结果的只读显示、精确实例／版本绑定及生命周期。

### Modified Capabilities

- `character-presentation-pose-graph`：原生 FlowCanvas 作者入口与编译运行分离，窗口只观察实际运行角色的 Committed Result。
- `graph-authoring-domain-framework`：允许 Pose 在复用唯一语义合同的同时采用原生编辑和序列化基础，保留其它领域边界。
- `graph-authoring-editor-shell`：Pose 的画布与通用交互由 GraphEditor 拥有，领域区域同窗口组合。
- `btsmtl-agent-authoring-document-sync`：Presentation Reconciler 直接修改正式 FlowCanvas 作者 owner，保留 v5 整包事务和反向发布。

## Impact

- Pose 作者模型、Capability、Compiler 输入遍历、Agent Document、Mutation、资产迁移、窗口与运行观察适配。
- CanvasCore／FlowCanvas 必要的领域无关编辑及外部观测钩子；不复制框架源码，不开启其业务 runtime。
- 运行层只扩展必要的诊断来源及结果投影，不替换计算、Stage、Worker、Frame 或 Physical Writer。
- `refactor-btsmtl-flowcanvas-authoring` 保留技能侧；共享框架接口先核对并复用，不由本提案实施技能。该提案近期由 `unify-flowcanvas-authoring-and-compiled-debug` 更名，历史 Pose 文字不构成本提案的重复实施范围。
- 本提案不依赖 `rebuild-btsmtl-preview-with-scene-play` 创建场景或预览角色。任何既有游戏／场景入口正常运行的合法角色都可被观察；本提案不接管这些入口，也不提供独立预览生命周期。
- 本提案取代 Pose 原任务 23.x／24.x 的原生 runtime 方向，以及22.x中被原生 UI 替代的专用交互目标；不自动完成或归档旧任务。具体规范对账见 design.md。
