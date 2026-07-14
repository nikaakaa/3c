## 1. 固定编译边界与迁移清单

- [ ] 1.1 枚举 Corin RootTree 可达的 Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 motion curve source identity。
- [ ] 1.2 枚举正式 BTSMTL runtime node/module 类型，并按纯控制、纯数值、有状态、world query、presentation command 和禁止运行分类。
- [ ] 1.3 枚举 RunnableNode、StateMachine、Timeline、Blackboard、Action、GameplayEffect、Motion 和输入缓存中影响后续 Tick 的全部可变字段。
- [ ] 1.4 枚举 CharacterPipeline 当前 network receive/send、correction、presentation capture、camera capture 和 diagnostics 副作用入口。
- [ ] 1.5 建立旧 authoring source 到唯一 compiler emitter/runtime operation 的迁移表。
- [ ] 1.6 建立旧 runtime object state 到 SimulationState slot 的迁移表。
- [ ] 1.7 建立 Corin gameplay source 与 Presentation projection source 的单源投影表。
- [ ] 1.8 明确 Compiler、Program、Kernel、Driver、World Solver、Committer 和 model session 的程序集依赖方向。
- [ ] 1.9 确认共享 core 文件不引用 UnityEngine、BTSMTL authoring、Animancer、Character presentation、Networking packet 或 Diagnostics。
- [ ] 1.10 确认三个 Demo 的 Program、地图、输入 schema 和 Corin authoring source 完全共用。

## 2. 建立 portable simulation 基础合同

- [ ] 2.1 新增稳定 `SimulationProgramId`、`SimulationProgramRevision`、`SimulationActorId`、`SimulationTick` 和 `SimulationEventId`。
- [ ] 2.2 新增 portable `SimScalar`，定义范围、精度、舍入、溢出和序列化规则。
- [ ] 2.3 新增 portable `SimVector2`、`SimVector3` 和量化 yaw/rotation 合同。
- [ ] 2.4 新增 portable body pose、velocity、grounded 和 collision summary 合同。
- [ ] 2.5 新增 model-neutral `CharacterSimulationInput`、typed input value 和 action request edge。
- [ ] 2.6 新增 `SimulationPassKind`，区分 Forward、Prediction、Replay 和 Authoritative。
- [ ] 2.7 新增 gameplay fact、motion request、motion result 和 presentation command 输出容器。
- [ ] 2.8 新增 `SimulationProgramCapability` 与 manifest，覆盖 Portable、Snapshotable、Deterministic 和 required world capabilities。
- [ ] 2.9 新增 ProgramHash 计算输入合同，包含 compiler version、TickRate、program bytes 和 portable catalog revision。
- [ ] 2.10 将 portable core 放入 Unity asmdef 与 .NET server csproj 共用的唯一源码目录并收口依赖。

## 3. 建立 CharacterSimulationProgram 与状态布局

- [ ] 3.1 新增不可变 `CharacterSimulationProgram` 和只读 Program loader。
- [ ] 3.2 新增 operation code、constant table、control-flow table 和 reference table。
- [ ] 3.3 新增 state slot、scope slot、blackboard slot、timeline slot 和 actor state layout 描述。
- [ ] 3.4 新增 portable Action、Behavior 和 GameplayEffect runtime catalog 区段。
- [ ] 3.5 新增 Timeline gameplay segment、TreeClip decision/commit 和 motion sample 区段。
- [ ] 3.6 新增 Program input/output schema 与稳定 InputId/FactId/EventKind index。
- [ ] 3.7 新增 Program Source Map 区段并复用现有 authoring source identity。
- [ ] 3.8 新增 Program binary writer/reader，拒绝未知版本、重复 identity、断裂 index 和非 canonical 顺序。
- [ ] 3.9 新增 Program asset 的 Unity 只读 wrapper 和纯 .NET bytes loader。
- [ ] 3.10 新增 ProgramHash、source hash、TickRate 和 capability 的一致性校验入口。

## 4. 建立 CharacterSimulationProgramCompiler

- [ ] 4.1 新增以 `CharacterPipelineDefinition` 为唯一编译根的 Editor compiler。
- [ ] 4.2 新增 compiler registry，将 authoring node/module 类型解析为唯一 operation emitter。
- [ ] 4.3 递归解析 RootTree、inline/shared Graph、SubTree 和稳定 containment route。
- [ ] 4.4 递归解析 StateMachineGraph、StateNode body、ConditionRuleGraph 和 Transition edge。
- [ ] 4.5 递归解析 inline/shared Timeline、Track、Clip、TreeClip 和 TimelineRunningTree。
- [ ] 4.6 解析 Blackboard declaration owner、scope、lifetime、projection 和 visible reference。
- [ ] 4.7 解析 ActionProfile、GameplayBehaviorProfile 和 GameplayEffectProfile 为 portable catalog。
- [ ] 4.8 为每个 operation 分配稳定 handle、constant index、state slot 和 source map entry。
- [ ] 4.9 对 control flow、Transition priority、Graph references 和集合输出使用 canonical 稳定顺序。
- [ ] 4.10 将 authoring 数值按 SimScalar 格式量化并记录 source 值与 runtime 值。
- [ ] 4.11 将 Timeline gameplay 时间换算为固定 Tick segment。
- [ ] 4.12 将 gameplay motion curve 烘焙为固定 Tick portable sample。
- [ ] 4.13 从同一 Timeline source 生成客户端 Presentation projection，不复制 gameplay window/motion 数据。
- [ ] 4.14 对 `Time`、Unity Random、Unity Physics、InputAction、Camera、Transform 和任意 Unity Object runtime dependency 报告 source-mapped 编译错误。
- [ ] 4.15 对缺少 emitter、断裂引用、Program capability 不满足和不稳定 identity 明确失败，不建立解释执行 fallback。
- [ ] 4.16 让相同 source、compiler version 和 TickRate 产生字节稳定的 ProgramHash。

## 5. 编译通用 Tree 控制流

- [ ] 5.1 为 Root、Enter、State lifecycle root 和普通 Action operation 建立统一执行合同。
- [ ] 5.2 编译 Sequence、Selector 和固定 child order。
- [ ] 5.3 编译 Parallel 的 child 状态、完成策略和稳定停止顺序。
- [ ] 5.4 编译 Decorator、If、Loop、Repeat、For 和 child cursor state。
- [ ] 5.5 编译 And、Or、Not、Equal、Compare 和基础数值/向量 operation。
- [ ] 5.6 将 Wait Time 编译为 WaitTicks，并把 elapsed tick 存入 state slot。
- [ ] 5.7 将 Wait Frame 收口为 SimulationTick 计数，不读取 render frame。
- [ ] 5.8 为可用随机 operation 接入 SimulationState RNG，删除 Unity Random runtime 路径。
- [ ] 5.9 编译 graceful stop、force stop、pending stop 和 source/replacement barrier。
- [ ] 5.10 编译 runtime activation generation 和 stable parent/child execution identity。
- [ ] 5.11 删除通用 runtime 对 `BaseGraph.DeltaTime`、`Time.deltaTime` 和 authoring clone mutable state 的依赖。
- [ ] 5.12 删除正式 gameplay 路径中 RunnableNode 自执行虚方法入口。

## 6. 编译 StateMachine 与打断生命周期

- [ ] 6.1 编译 Enter、AnyState、State 和 Exit control nodes。
- [ ] 6.2 编译 Transition source、target、priority、flow order、interrupt mode 和 condition program。
- [ ] 6.3 将 active、exiting、pending state 和 pending transition 放入 state layout。
- [ ] 6.4 编译 outer-to-inner `StateMachineExecutionPath` 与 State scope generation。
- [ ] 6.5 编译 State OnEnter、Root、OnExit 和 graceful stop barrier。
- [ ] 6.6 编译父 Tree abort、Self/LowerPriority interruption 和 force stop 的统一生命周期。
- [ ] 6.7 编译 nested StateMachine 并保持完整 owner/scope path。
- [ ] 6.8 编译 state runtime facts 与 ConditionRuleGraph 查询。
- [ ] 6.9 保持 root completed 只作为事实，不成为 Transition 隐式条件。
- [ ] 6.10 删除 `StateMachineGraphRuntime` 对 runtime graph clone、Guid.NewGuid 和隐藏 dictionary state 的正式依赖。

## 7. 编译 Timeline、TreeClip 与 Blackboard

- [ ] 7.1 编译 TimelineNode request、handle、generation、Once/Loop 和 terminal 状态。
- [ ] 7.2 将 playback time、cycle、duration reached 和 active clip membership 放入 state layout。
- [ ] 7.3 编译 Decision TreeClip 的 segment traversal、loop boundary 和 Frame Blackboard write。
- [ ] 7.4 编译 RootTree 后 WindowFactProjection 的 provenance 和单次事实输出。
- [ ] 7.5 编译 Commit TreeClip Enter/Update/Exit/Destroy 和 stop lifecycle。
- [ ] 7.6 编译 motion curve contribution、CurveEndFrame、EndFrame、channel claim 和 consume lower semantics。
- [ ] 7.7 编译 Action Context 关联、window、cue、camera 和 gameplay result command。
- [ ] 7.8 将 animation track 只编译为 producer/playback identity 和 visual time需求，不把 AnimationClip 放入 Program。
- [ ] 7.9 将 Blackboard declaration 编译为稳定 declaration index 与 scope layout。
- [ ] 7.10 将 Character、Graph、State、ActionInstance 和 Frame owner generation 放入 SimulationState。
- [ ] 7.11 编译 Config 只读、scope cleanup、fact projection 和 write provenance。
- [ ] 7.12 删除 Timeline runtime clone、TimelineRunningTree authoring clone 和 Blackboard runtime dictionary 主路径。

## 8. 编译 Character 输入、Action 与 GameplayEffect

- [ ] 8.1 将 CharacterInputProfile 编译为 portable input schema，并保留 Unity Input adapter mapping。
- [ ] 8.2 将本地 InputAction value/request 转换为当前 Tick 的 CharacterSimulationInput。
- [ ] 8.3 将 camera-relative move 在输入适配层转换为量化 world direction 或 camera yaw input。
- [ ] 8.4 将 external model input 转换为同一 CharacterSimulationInput，不创建网络专用 Graph node。
- [ ] 8.5 将 request buffer、created tick、sequence、expiry、priority 和 consumed state 放入 SimulationState。
- [ ] 8.6 编译 input value、request query、consume 和 state exit cause nodes。
- [ ] 8.7 将 Action activation validation、instance allocation、prediction identity 和 lifecycle transition迁入 portable runtime。
- [ ] 8.8 将 Action active instance、phase、terminal/non-terminal state 和 source context 放入 SimulationState。
- [ ] 8.9 编译 Action scope 与 Timeline/Motion/Result/Presentation 输出关联。
- [ ] 8.10 将 GameplayTag、Attribute、ActiveEffect、stack、inhibition、prediction journal 和 revision 迁入 portable state。
- [ ] 8.11 将 GameplayEffect fixed Tick、transaction order、ChangeSet 和 command sink 迁入 portable runtime。
- [ ] 8.12 保持 GameplayEffect 不依赖 Character、BTSMTL authoring、Network Model、Presentation 或 Diagnostics。
- [ ] 8.13 删除 CharacterGraphContext 对 InputAction、Camera snapshot 和 Unity Object fact context 的 gameplay runtime 读取。

## 9. 建立 SimulationKernel 与 World Solver

- [ ] 9.1 新增 `CharacterSimulationState` 并按 Program state layout 初始化。
- [ ] 9.2 新增 `SimulationWorldState`，按稳定 ActorId 保存 actor/program/state。
- [ ] 9.3 新增 `CharacterSimulationKernel.Step`，只接收 Program、state、Tick input 和 world contract。
- [ ] 9.4 固定 Kernel 阶段为 input、Decision TreeClip、Graph/SM、window projection、Timeline commit、Action/Effect、Motion、facts/output。
- [ ] 9.5 新增 `ICharacterWorldSolver` 和 capability manifest。
- [ ] 9.6 将 MotionContribution、channel resolve、modifier 和 MotionIntent 迁入 portable Kernel。
- [ ] 9.7 将 world-constrained motion request/result 与 model correction、packet 和 presentation 完全分离。
- [ ] 9.8 将 Unity CharacterController adapter 改为 World Solver implementation 并只在 adapter 内转换 Unity 数值。
- [ ] 9.9 让 Unity solver result 量化回 portable body state 和 motion result。
- [ ] 9.10 按 ActorId 固定多 actor Step 和 world query 顺序。
- [ ] 9.11 将 Logic Pose 从 Unity Transform/Port 主权迁入 SimulationState，Unity scene object 只镜像 committed pose。
- [ ] 9.12 删除 Kernel 对 CharacterMotionAuthority、ExternalPose、NetworkReceiveStage 和 NetworkSendStage 的依赖。

## 10. 建立完整 Snapshot、Restore 与 Hash

- [ ] 10.1 新增 ProgramHash-bound `CharacterSimulationSnapshot`。
- [ ] 10.2 新增 `SimulationWorldSnapshot`，包含 world tick、actor states、solver state 和 command cursor。
- [ ] 10.3 捕获 Runnable、SM、Timeline、Blackboard、Action、Effect、Body、RNG、counter 和 handle allocator 状态。
- [ ] 10.4 恢复全部 state slot、scope generation、queue cursor 和 actor ordering。
- [ ] 10.5 为 snapshot binary 定义 canonical serialization 与容量边界。
- [ ] 10.6 为 gameplay state 定义 canonical state hash，排除 diagnostics、presentation 和 transport state。
- [ ] 10.7 对 ProgramHash、Actor catalog、TickRate 或 solver state schema 不匹配明确失败。
- [ ] 10.8 删除 GameplayEffect transaction snapshot 被误用为 world snapshot 的可能入口。
- [ ] 10.9 删除任何基于反射遍历 runtime object 的补漏 snapshot 路径。

## 11. 恢复唯一 Local 单机闭环

- [ ] 11.1 新增 `LocalSimulationDriver`，每个 LocalLogicTick 推进一次 SimulationWorldState。
- [ ] 11.2 让 CharacterPipelineHost 从 compiled Program 创建 actor state 并注册 Local Driver。
- [ ] 11.3 让 GameplayTickSystem 调度 Local Driver，而不是直接调度旧 CharacterPipeline monolith。
- [ ] 11.4 将 simulation facts 投影到现有 Character gameplay output 和 diagnostics。
- [ ] 11.5 新增 Presentation Committer，消费稳定 EventId、logic pose sample、Timeline visual state 和 animation selection。
- [ ] 11.6 让 Camera、Animation、Cue 和 VFX 只消费 committed/predicted presentation command。
- [ ] 11.7 保持 Timeline visual sample 与 Animancer fade 在 PresentationFrame 使用真实 render delta。
- [ ] 11.8 迁移 Corin RootTree、nested StateMachine、Timeline、TreeClip、Blackboard、Action 和 GameplayEffect 到 compiled Program。
- [ ] 11.9 迁移 Corin Unity solver、visual root、Animancer、camera 和 input adapter 绑定。
- [ ] 11.10 保持 Corin Idle/Walk/Run/MovingTurn/Dodge/Attack1/Attack2、连段和打断的唯一 authoring source。
- [ ] 11.11 切换正式运行入口后删除 Character 旧 BehaviorTreeRuntime、StateMachineGraphRuntime 和 Timeline runtime clone执行路径。
- [ ] 11.12 删除 `CharacterMotionAuthority`、ExternalPose 主分支和 Host 旧 authority 序列化字段。
- [ ] 11.13 删除 MotionStage 内 partial/full ServerAuthoritative correction 算法和 correction plan。
- [ ] 11.14 删除旧 CharacterPipeline network receive/send 内嵌调度和 presentation capture 副作用。
- [ ] 11.15 删除 runtime compile fallback、旧 Program 缺失 fallback 和一次性 migrator。

## 12. 建立模型与后端插件 authoring

- [ ] 12.1 扩展 `GameplayNetworkModelDefinition` capability contract，声明 required Program、Driver、Host 和 World Solver capability。
- [ ] 12.2 扩展 model session actor binding，使它接收 Program/state handle、input adapter 和 commit sink，而不是 concrete CharacterPipeline。
- [ ] 12.3 保持 GameplayNetworkSessionHost 只创建并锁定一个完整 model session。
- [ ] 12.4 保持 Endpoint 和 Transport 独立于 model Driver 与 World Solver。
- [ ] 12.5 为 server launch manifest 定义 ProgramHash、TickRate、model id、host id、solver id、map id 和 protocol revision。
- [ ] 12.6 在 CharacterPipelineDefinition Inspector 增加 Compile 状态、ProgramHash、TickRate、capability、量化摘要和 source-mapped errors。
- [ ] 12.7 在 Graph/Timeline Editor 增加只读 Program compatibility 与 compile diagnostics，不增加 network fields。
- [ ] 12.8 在 Network Model Inspector 显示 model、endpoint、transport、host、solver 和 Program capability 解析结果。
- [ ] 12.9 只枚举 runtime、definition、actor binding 和配置均完整的 model plugin。
- [ ] 12.10 对无效组合、缺失 Program、stale ProgramHash 和未安装 plugin 明确失败，不显示或创建 fallback。

## 13. 收口共享协议、Fantasy Endpoint 与双人 Room

- [ ] 13.1 将 ServerAuthoritative endpoint/wire 必需合同迁入模型专属共享 contracts assembly。
- [ ] 13.2 定义 Join、roster、clock、canonical simulation input、authoritative state、ack、action lifecycle 和 leave 协议。
- [ ] 13.3 定义 DeterministicRollback 的 input bundle、hash、snapshot recovery 和 acknowledgement 独立协议，不复用 correction packet。
- [ ] 13.4 更新 Outer proto 并生成 client/server C#，删除旧 FrameSync 协议、opcode 和 parser。
- [ ] 13.5 新增 Fantasy ServerAuthoritative EndpointDefinition/Endpoint 并让 endpoint 实例唯一拥有 Fantasy Session。
- [ ] 13.6 删除静态 Fantasy Session facade ownership 和字符串 placeholder。
- [ ] 13.7 新增服务端双人 Room、PlayerId/ActorId/TeamId/spawn 分配和固定 server clock。
- [ ] 13.8 新增 session-level Join/ActorJoined/ActorLeft/ClockUpdated/Disconnected event queue。
- [ ] 13.9 新增 model-owned roster host，按事件创建、绑定和释放 actor presentation。
- [ ] 13.10 让所有 command、snapshot、transaction queue 和 history 有界并区分可替换 stream 与可靠事实。

## 14. 实现 Unity 权威进程 Demo

- [ ] 14.1 新增 Unity authoritative server host，加载 canonical Program bytes 和 server launch manifest。
- [ ] 14.2 在服务端创建 SimulationWorldState、ServerAuthoritative Driver 和 Unity CharacterController solver。
- [ ] 14.3 让服务端只从 accepted canonical input/action state 推进 actor，不读取客户端 resolved displacement。
- [ ] 14.4 让 Owner client 使用同一 Program 本地预测并发送 canonical simulation input。
- [ ] 14.5 让 server snapshot 携带 authoritative tick、acked input sequence、actor body 和正式 action/effect facts。
- [ ] 14.6 将 Owner reconciliation 放入 ServerAuthoritative Driver history，不回到 MotionStage correction。
- [ ] 14.7 让 Remote actor 使用 model-owned snapshot buffer 和 Presentation Committer，不恢复 ExternalPose Pipeline mode。
- [ ] 14.8 让远端动作复制使用 ActionInstance/gameplay lifecycle，不传动画或 Timeline producer identity。
- [ ] 14.9 增加 Unity server 与两个客户端的正式 launch definition 和 Demo scene assets。
- [ ] 14.10 对 server process、ProgramHash、map、solver 或 endpoint 缺失明确 Faulted。

## 15. 实现纯 C# DotRecast 权威 Demo

- [ ] 15.1 将选定并固定版本的 DotRecast 依赖接入 .NET server 正式项目。
- [ ] 15.2 新增 DotRecast NavMesh build/import artifact 与 map identity。
- [ ] 15.3 新增 `DotRecastNavigationWorldSolver`，实现静态表面 MoveAlongSurface、height 和边界约束。
- [ ] 15.4 让 DotRecast solver 声明 Portable、StaticNavigationSurface 能力且不声明 DeterministicKCC。
- [ ] 15.5 将 DotRecast float result 量化回 portable body result。
- [ ] 15.6 新增 .NET authoritative server host，加载同一 Program bytes 和 ServerAuthoritative Driver。
- [ ] 15.7 复用同一 Join/roster/input/action/snapshot 协议和 client model session。
- [ ] 15.8 让移动、转向、闪避、MovingTurn 和攻击 motion curve 通过 DotRecast 表面约束执行。
- [ ] 15.9 增加 .NET server 与两个客户端的正式 launch definition 和 Demo assets。
- [ ] 15.10 删除参考目录运行时依赖、临时 navmesh parser 和 Unity server fallback。

## 16. 实现 Deterministic KCC 与 Rollback 模型

- [ ] 16.1 新增 deterministic static world geometry 和 stable collider identity 格式。
- [ ] 16.2 新增 deterministic capsule body、ground probe、sweep、slide、step/slope 范围和 yaw 求解。
- [ ] 16.3 新增 `DeterministicKccWorldSolver` 并声明 Portable、Deterministic、Snapshotable 和 CharacterCapsuleCollision。
- [ ] 16.4 将 solver mutable world/body state 纳入 SimulationWorldSnapshot。
- [ ] 16.5 新增 `DeterministicRollbackModelDefinition` 和完整 model session。
- [ ] 16.6 新增按 SimulationTick/ActorId 排序的 canonical input bundle。
- [ ] 16.7 新增客户端预测 input history、world snapshot ring 和 confirmed tick cursor。
- [ ] 16.8 新增服务端 accepted input history、canonical bundle broadcast 和 authoritative tick。
- [ ] 16.9 新增迟到输入检测、restore tick 选择和有界 replay。
- [ ] 16.10 重演时使用同一 SimulationKernel、Program 和 Deterministic KCC，不建立 replay 专用节点。
- [ ] 16.11 新增 state hash 周期、client/server hash report 和 mismatch identity。
- [ ] 16.12 新增首次加入、超出 history、Program 一致但 state hash 失配时的权威 snapshot recovery。
- [ ] 16.13 新增 presentation command ledger，按 EventId 去重、替换和撤销预测输出。
- [ ] 16.14 保持 Animancer/Camera/VFX 不参与 snapshot、replay 或 state hash。
- [ ] 16.15 增加 DeterministicRollback server 与两个客户端的正式 definition、manifest 和 Demo assets。
- [ ] 16.16 只有 runtime、协议、KCC、history、replay、commit 和 assets 全部存在后才在 Inspector 显示该模型。

## 17. 建立三方案比较与统一诊断

- [ ] 17.1 为三个 Demo 使用同一 Corin ProgramHash、input schema、地图 identity 和角色配置。
- [ ] 17.2 新增 model-neutral comparison metrics contract。
- [ ] 17.3 记录 RTT、sent/received bytes、queue depth 和 stale/duplicate command。
- [ ] 17.4 记录 Owner prediction error、correction count、correction distance 和 hard recovery。
- [ ] 17.5 记录 rollback count、restore tick、replayed ticks、history occupancy 和 state hash mismatch。
- [ ] 17.6 记录 World Solver id、capability、requested/applied motion 和 collision summary。
- [ ] 17.7 将 Program operation、SimulationTick、pass kind、ActorId 和 EventId 接入 structured Trace。
- [ ] 17.8 扩展 Debug Source Map overlay，使 replay instruction 仍映射到原 Graph/Timeline source。
- [ ] 17.9 新增统一 Demo HUD/Inspector，只读展示正式 metrics，不参与运行决策。
- [ ] 17.10 新增三个独立启动入口，不在运行中切换 model 或 solver。

## 18. 清理旧路径、更新文档并完成校验

- [ ] 18.1 删除已被本 change 吸收的 `add-local-two-client-gameplay-network-closure` active change 目录。
- [ ] 18.2 删除 Character 旧解释执行器、runtime authoring clone、旧 authority enum/字段和旧 correction path。
- [ ] 18.3 删除旧 ExternalPose、RemoteProxy、FrameSync、静态 Fantasy facade、重复 command/history/schema 和一次性 migrator。
- [ ] 18.4 使用 `rg` 确认 Graph、StateMachine、Timeline、Action 和 GameplayEffect 不引用 Network Model、Endpoint、Transport、Driver 或 solver implementation。
- [ ] 18.5 使用 `rg` 确认 portable core 不引用 UnityEngine、Animancer、InputSystem、Camera、Networking packet 或 Diagnostics。
- [ ] 18.6 使用 `rg` 确认正式 runtime 不再调用 `Time.deltaTime`、Unity Random 或 authoring clone 执行 gameplay。
- [ ] 18.7 使用 `rg` 确认不存在 interpreted fallback、Program fallback、solver fallback、compat parser 或双写入口。
- [ ] 18.8 更新 `openspec/project.md`，说明编译式 SimulationKernel、两种正式 Network Model、三个 Demo 组合和作品主线优先级。
- [ ] 18.9 更新所有受影响 current specs，删除旧 CharacterMotionAuthority、runtime clone、MotionStage correction 和“只存在一个模型”的过期口径。
- [ ] 18.10 更新代码组织文档，说明 shared core、compiler、program assets、drivers、solvers、server hosts 和 presentation adapters。
- [ ] 18.11 使用带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 的 dotnet/msbuild 编译 shared core 与 server projects。
- [ ] 18.12 使用相同参数编译 Unity Runtime 程序集。
- [ ] 18.13 使用相同参数编译 Unity Editor 程序集。
- [ ] 18.14 编译结束后立即执行 `dotnet build-server shutdown`。
- [ ] 18.15 运行 `openspec validate refactor-character-simulation-for-pluggable-network-models --strict --no-interactive` 并解决全部问题。
