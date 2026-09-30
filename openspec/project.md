# Project Context

## Purpose

求职向第三人称 Gameplay 客户端 demo，重点为输入、控制、动画、相机、战斗窗口、受击反馈与调试。2v2vE 压力场景包含两名真人、两名同管线 Bot、中立怪；网络不是主展示方向。不做完整 PvPvE/MMO、匹配、账号、背包、大地图、多职业或反作弊产品，也不做纯网络框架或完整断线重连，除非用户明确改目标。见[产品范围](2v2ve-gameplay-client-demo.md)。

## Current Architecture

| Owner | 职责与输出 |
| --- | --- |
| CharacterPipelineDefinition | 只装配领域内容与 binding，不是整角色编译根 |
| Graph / Skill / Ability | Graph 编译只发布其 owner 的 artifact；技能管入口、授权、引用、实例，各 Ability 按数值目标独立发布执行数据并回写 Definition 引用 |
| Control | C# 模块执行 Locomotion |
| Timeline | 直接准备、调度 TimelineData，拥有内容身份、时间、求值边界 |
| Pose | 提交事实→原生 FlowCanvas 图与领域资源 binding→最终姿态 |
| Camera / Motion / Effect / Equipment | 各自准备、运行、报告采用结果；Profile、Rig 与正式内容各有资源语义 |
| SimulationSessionHost | 唯一装配根，组合各领域、Pipeline、Source、WorldSolver，拥有准备、roster 锁定、tick、销毁 |
| SimulationSessionHistory | 唯一持有检查点、历史分支、裁剪、恢复事务；Host 提供同一 runtime/roster/完成 Tick，在真实释放边界解除借用 |
| CharacterPipelineHost | 只做 Actor registration、binding、表现、诊断，不另建 Source、Solver、Pipeline 或 Preview runtime |

```text
输入/Session Source → SimulationSessionHost → Ingress → Schedule
→ Evaluate → World ResolveBatch → Finalize → Egress → atomic Commit → Presentation
```

领域持有自己的状态，同一 Step 延续到 Finalize 后一次 Commit，表现、网络、诊断只消费提交事实。Float32/Fixed 各有状态、codec、运行数据；Local/Rollback/ServerAuthoritative 由 Variant、Source、Pipeline、Network Model 显式装配，不由节点、UI 或 fallback 推断。

窗口与 C# 作者 API 共用业务定义，不把序列化字段/运行对象暴露为另一条作者链。配置、schema、枚举、值域由内容准备形成 binding/payload/resource；FBBIK、Pose、Blend 构造链不重复校验。

TreeClip 节点经所属 domain emitter 输出一次性 Gameplay/Camera/VFX/Audio；攻击碰撞与属性交 GameplayEffect/Ability，Timeline 不解释业务数值。输出带 Action Context、播放、图/节点、提交事务身份，不另包 Cue 或在表现帧重发 Logic。ActionCueTrack、ActionCueClip、ActionCueCommitted 链退役。Timeline 只用 Slate；FlowCanvas 编辑技能及 TreeClip/Marker 图，旧 TreeDesigner 作者包、节点、发现/菜单与自制 UI 整体退役。

```text
提交事实 → 原生 Pose 状态/源/Slot/姿态阶段
→ typed Goal Contributions → Goal Assembly → FBBIK → 最终姿态
```

外壳只调用 Pose 完整帧入口，内部单点准备、Animancer Evaluate Barrier、Pending 验证、提交；Foot、Assembler、FBBIK、final writer 每帧各至多一次，不另建 Grounding、Goal Set 或图外骨骼修正。运行只校验帧输入、血缘、缓冲形状、目标唯一性和求解事实。

Barrier 前失败丢弃 Pending；内/后及后续业务收尾失败使同一 Actor Faulted 并停止后续帧，不宣称物理回滚；纯诊断失败走诊断通道。见[原生 Pose](specs/character-pose-graph-runtime-architecture/spec.md)。

MM 通用能力不代表 Corin 已有完整正式 binding；MxM 仅供参考，不进 runtime。AI 唯一使用 Opsive Behavior Designer，经 Character Input、TargetData、Action Request 与只读结果接入。

## Conventions

Workbench 保留 Authoring、Preview、RuntimeDebug；Preview 在 CMC 式隔离隐藏 Scene 的 Edit Mode 自动准备正式 ScenePlay Session，不进 Unity Play。窗口共享唯一宿主/Renderer，复用 GameplayTickSystem，不另建 Session、播放器、时钟、世界查询或状态。可停靠 Preview 显示视口、实例黑板、执行时间线，FlowCanvas/Slate 保留作者面。详见[预览](specs/btsmtl-timeline-editor-preview/spec.md)。

RuntimeDebug 只观察，不推进 Timeline、改状态、抢占 GameView 或把焦点送入输入链。构建、重建、编译、发布须显式触发，不能因选中、开窗或运行自动执行；runtime 不读 AssetDatabase/作者编译实现。执行规则归根 AGENTS。

GameplayLab 只作 Editor/Development 展示；商业资源/认证端点、共享打包未闭环，不宣称可发布。网络测试、性能采集、普通构建各用唯一正式 workflow 发布精确产物。

## Document Ownership

本页与现行 specs 拥有当前合同；change 只拥有未收口增量，目录/勾选不代表交付。archive、实验、Replay、日志、Git 只记历史，已删 change 不作当前依赖。见[现行索引](maintenance-audit.md)。

## Cleanup Rules

不加 fallback、兼容镜像、桥接或双主线。BTSMTL 是 authoring 基座与参考，不要求照搬 runtime。旧 Workbench/Document 同步协议、整角色 CharacterSimulationProgram/ProgramCatalog/Projection、Pose Image、Timeline IR、runtime Graph/Timeline clone、分裂 locomotion/action/footphase/bodyclaim/Foot 源退役，迁入正式节点/模块/Timeline 或删。确认无消费者的旧数据、路径、命名、配置、编译器、缓存、wrapper 直接清理；Ref 迁入正式模块并改名，不作运行依赖。
