# Project Context

## Purpose

本项目是求职向 Gameplay 客户端程序 demo。重点是第三人称动作客户端的输入响应、角色控制、动画表现、镜头、战斗窗口、受击反馈和调试可视化，以及在网络模型压力下仍能审查的玩法模拟边界。

业务压力场景固定为 2v2vE 动作战斗技术演示：两名真人玩家、两名使用同一角色管线的 Bot，以及不属于双方队伍的中立怪。它不是完整 PvPvE、MMO、匹配、账号、背包、大地图、多职业或反作弊产品。正式产品范围见 [2v2ve-gameplay-client-demo.md](2v2ve-gameplay-client-demo.md)。

## Current Architecture

### Authoring And Content

- `CharacterPipelineDefinition` 是角色配置装配根，只引用各领域的正式内容与 binding；它不是整角色编译根。
- Skill/Ability 提供入口、授权、引用和运行实例。每个 Ability 的执行数据由正式发布链按数值目标独立产出并回写 Definition 引用，不再拼成角色总包。
- Graph 编译只发布明确 graph owner 的 artifact。Character Definition、Ability、Control、Timeline、Pose、Camera、Motion、Effect 与 Equipment 不会被合并为整角色 `CharacterSimulationProgram`、`ProgramCatalog` 或 `Projection`。
- Locomotion 由 C# 控制模块执行；Timeline 直接准备和调度正式 `TimelineData`；Pose 直接运行原生 FlowCanvas Graph 与领域资源 binding；Camera、Motion、Effect、Equipment 各自准备、运行并报告采用结果。
- Animation Profile、Rig、Pose 资源、Foot Placement 配置、Camera Profile 和 Equipment Profile 各自拥有资源语义。作者窗口和 C# authoring API 必须走同一业务定义，不暴露 Unity 序列化字段或运行时对象作为另一条作者链。
- 配置、枚举和值域由 authoring/content preparation 统一校验并写入正式 binding、payload 或 compiled resource；运行实例消费已经准备好的内容，不在 FBBIK、Pose、Blend 等正式运行构造链重复检查同一份配置。

### Simulation

```text
输入 / Session Source
-> SimulationSessionHost
-> Ingress
-> Schedule
-> Evaluate
-> World ResolveBatch
-> Finalize
-> Egress
-> atomic Commit
-> Presentation
```

- `SimulationSessionHost` 是唯一运行装配根，显式组合 Control、Ability、Timeline、Effect、Equipment、Presentation、Pipeline、Session Source 与 WorldSolver，并拥有准备、roster 锁定、tick 生命周期和销毁顺序。
- `CharacterPipelineHost` 只负责 Actor registration、领域 binding、Presentation 和 diagnostics 接口；它不创建第二套 Source、Solver、Pipeline 或 Preview runtime。
- 每个领域只保存自己的正式状态。角色和世界状态只在同一 Step 的事务中从 Evaluate 延续到 Finalize，再执行一次 Commit；表现、网络和诊断只消费已提交事实。
- Float32 与 Fixed 分别拥有必要的数值状态、codec 和运行数据。Local、DeterministicRollback 和 ServerAuthoritative 的差异由各自 Variant、Session Source、Pipeline 与 Network Model 装配，不能由节点、作者 UI 或隐式 fallback 推断。

### Timeline TreeClip Node Boundary

- Timeline 不拥有 `ActionCueTrack`、`ActionCueClip` 或 `ActionCueCommitted` 事件链。一次性 Gameplay、Camera、VFX 和 Audio 行为都在对应 TreeClip 内由正式节点表达，并经节点所属的正式 domain emitter 输出。
- Corin 的攻击碰撞和攻击属性由 TreeClip 内的 Gameplay 节点提交给 GameplayEffect / Ability 执行域；Timeline 只拥有 TreeClip 的内容身份、时间和求值边界，不解析命中效果、碰撞形状或属性数值。
- TreeClip 节点输出必须携带正式 Action Context、播放身份、图／节点身份和提交事务身份。Timeline 不把节点输出重新包装成另一套 Cue 事件，也不在 PresentationFrame 重发 Logic 输出。
- Timeline 的旧 TreeDesigner 自制 UI 已删除；唯一 Timeline 编辑面是嵌入 Slate，FlowCanvas 只负责 TreeClip / Marker 触发图等正式图的可视化与作者入口。

### Presentation

- 表现只从 committed Body、Action、Timeline、Effect 与领域事实开始。动画帧保持唯一 `Prepare -> Validate -> Animancer Evaluate Barrier -> Seal` 事务；Barrier 前失败只丢弃 Pending，Barrier 后失败使该 Actor 的动画 runtime 进入 Faulted。
- Pose 的正式链是 `Presentation Fact -> PoseStateMachine -> state-local source -> AnimationSlot -> Pose stages -> typed Goal Contributions -> Goal Assembly -> FullBodyIK -> FinalAnimationPoseFrame`。每帧最多一次 Foot Placement 事务、一次 Goal Assembly、一次 FBBIK 与一次 final writer。
- FBBIK、Pose Graph、Blend Stack 的资源 schema、枚举和值域在 authoring/content preparation 通过后才进入正式运行；运行期只保留帧输入、事务血缘、缓冲形状、目标唯一性和求解结果等运行事实校验。
- Foot Placement、Goal Assembly、FullBodyIK 和 final writer 各有唯一 owner；不得增加第二个 Grounding、Goal Set、FBBIK、骨骼写入或图外修正路径。
- Motion Matching 的通用能力可以存在，但 Corin 尚未拥有完整的正式 MM 内容 binding；类型或工具存在不等于角色已经接入。第三方 MxM 仅作内容与实现参考，不能进入正式 runtime。
- AI 已从 BTSMTL 自研链路退役，Opsive Behavior Designer 是唯一 AI 作者与执行插件。它只通过 Character Input、TargetData、Action Request 和只读结果合同接入玩法。

### Product And Build Boundary

- `GameplayLab` 是 Editor/Development 技术展示，不是商业客户端启动、认证或资源交付链。
- 商业启动代码尚未完成唯一资源端点、认证端点和共享资源打包规则，因此不能描述为可发布产品闭环。
- Network Test Product、Performance Capture 与普通产品构建各自通过唯一正式 workflow 发布精确产物；运行、构建、采样和分析不能复制成另一套控制面。

## Document Ownership

- `openspec/specs/` 与本文件共同表达当前能力合同；细节以对应 spec 为准。
- 未归档 change 只记录尚未收口的增量，不能因目录存在或任务勾选而宣称能力已交付。
- `openspec/changes/archive/`、协调记录、实验、Replay、构建日志和 Git 历史只保留当时事实，不是当前实现入口。
- 现行目录和入口见 [maintenance-audit.md](maintenance-audit.md)。已删除的 change 目录不能再作为链接目标或等待依赖。

## Conventions

- 不做 fallback 配置、兼容镜像、临时桥接或双主线。
- Build、资源重建、编译和发布都是显式重操作；不得由选中资产、运行时或窗口打开自动触发。
- 运行时不读取 AssetDatabase 或作者编译实现；Preview 不创建第二个 Session、播放器、时钟、世界查询或状态真相。
- 文档读取使用 UTF-8；默认不新增测试。用户负责 Unity 端到端验收，不把手动验证写入 OpenSpec task。

## Cleanup Rules

- 不恢复旧 Workbench、旧 Document/同步协议、整角色 Program/Projection、Pose Image、Timeline IR、runtime Graph/Timeline clone 或旧 Foot 多数据源。
- 旧数据、路径、命名、编译器、缓存和 wrapper 确认没有消费者后直接删除，不保留兼容层。
- `Ref` 中的代码只能迁入正式模块后改名归属，不能成为运行时依赖。
