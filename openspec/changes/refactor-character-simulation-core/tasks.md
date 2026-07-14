## 1. 冻结核心边界与迁移清单

- [ ] 1.1 盘点 CharacterPipelineDefinition 引用的 RootTree、nested Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 motion curve。
- [ ] 1.2 盘点 Corin 正式资产可达 Node/Module/Edge/Track/Clip 类型及它们读写的 runtime 状态。
- [ ] 1.3 盘点 RunnableTree、StateMachine runtime、Timeline scheduler、Blackboard、ActionRuntime 和 GameplayEffectRuntime 中会影响后续 Tick 的隐藏字段。
- [ ] 1.4 盘点 gameplay runtime 对 UnityEngine、Time、Random、Input System、Camera、Transform 和 Animator/Animancer 的直接依赖。
- [ ] 1.5 盘点 CharacterMotionAuthority、ExternalPose、MotionStage correction、NetworkSendStage 和 NetworkReceiveStage 的全部使用点。
- [ ] 1.6 盘点 ServerAuthoritativeHybrid Character binding 对旧 CharacterPipeline stage 的编译期和运行时依赖。
- [ ] 1.7 形成 source type -> emitter -> operation -> state slot -> output channel 的唯一迁移表。
- [ ] 1.8 锁定 Program、State、Input、Output、Driver、Solver、Projection 和 Committer 的 assembly ownership，不建立循环引用。

## 2. 建立 portable 模拟数据合同

- [ ] 2.1 建立稳定 ProgramId、ProgramRevision、ProgramHash、ActorId、SimulationTick、OperationHandle 和 EventId。
- [ ] 2.2 建立 SimScalar、SimVector2、SimVector3、量化 yaw/rotation 和明确 Tick duration 合同。
- [ ] 2.3 建立 CharacterSimulationInput 的连续值、离散 request、sequence 和 source tick 布局。
- [ ] 2.4 建立 portable BodyState、MotionRequest、WorldQuery、WorldSolverResult 和 collision summary。
- [ ] 2.5 建立 typed gameplay facts、presentation commands 和 SimulationOutput 不可变容器。
- [ ] 2.6 建立 Program CapabilityManifest，声明 portable、snapshotable、deterministic-compatible 和所需 World capabilities。
- [ ] 2.7 将 portable 合同放入不引用 UnityEngine 的正式 assembly/source set。
- [ ] 2.8 清理 portable 合同中的 Unity Vector、Quaternion、AnimationCurve、Object 和 GameObject 引用。

## 3. 建立 CharacterSimulationProgram 与 State Layout

- [ ] 3.1 建立 Program manifest、compiler version、TickRate、source revision 和稳定 serialization header。
- [ ] 3.2 建立 operation table、constant table、control-flow table 和 graph ownership table。
- [ ] 3.3 建立 StateMachine transition table、scope table 和 nested execution path table。
- [ ] 3.4 建立 Timeline segment、TreeClip、loop、motion sample 和 producer identity table。
- [ ] 3.5 建立 Blackboard declaration、type、scope、lifetime、owner 和 address layout。
- [ ] 3.6 建立 Action、Behavior、GameplayEffect、Tag 和 Attribute portable catalog。
- [ ] 3.7 建立 Runnable、StateMachine、Timeline、Blackboard、Action、Effect、Body、RNG 和 sequence state slot 描述。
- [ ] 3.8 建立 SimulationState 初始化、深拷贝、释放和 layout-bound access API。
- [ ] 3.9 建立 CharacterSimulationProgram canonical bytes 与稳定 ProgramHash 计算。
- [ ] 3.10 对 duplicate identity、不稳定排序、state slot 重叠和 stale revision 明确失败。

## 4. 建立 CharacterSimulationProgram Compiler

- [ ] 4.1 建立以 CharacterPipelineDefinition 为唯一根的 Compiler 入口和 compile report。
- [ ] 4.2 建立 inline/shared Graph、SubTree、StateBehaviorSubTree 和 GraphReference 的稳定遍历。
- [ ] 4.3 建立 Node/Module emitter registry，每个可执行 authoring type 只能对应一个 emitter。
- [ ] 4.4 编译 Root/Composite/Decorator/Action/Value 节点的 control-flow 和 data-flow operation。
- [ ] 4.5 编译 PropertyPort/PropertyEdge、ConditionRuleGraph 和稳定 PortId 连接。
- [ ] 4.6 编译 StateMachineGraph、StateNode、TransitionEdge、interrupt metadata 和 nested execution path。
- [ ] 4.7 编译 TimelineNode、TimelineData、Track、Clip、TreeClip、loop 和 playback mode。
- [ ] 4.8 编译 Blackboard declaration/reference、scope owner、fact projection 和 config constant。
- [ ] 4.9 编译 Action/Behavior/GameplayEffect catalog 引用与稳定 BehaviorId。
- [ ] 4.10 编译 motion curve 为 Tick-indexed portable sample，不在 Kernel 评估 Unity AnimationCurve。
- [ ] 4.11 从同一 source 生成 CharacterPresentationProjection 的 producer/resource binding。
- [ ] 4.12 生成 operation/state/source 的 Debug Source Map 与 capability manifest。
- [ ] 4.13 对缺失 emitter、断裂引用、循环 ownership、非 portable 调用和 capability 不满足明确失败。
- [ ] 4.14 建立 Program Asset 写入和 stale artifact 判定，不增加 runtime compile fallback。

## 5. 迁移 Tree 控制流与 Runnable 生命周期

- [ ] 5.1 建立 Kernel operation dispatch 和每 Tick 稳定 execution budget。
- [ ] 5.2 迁移 Runnable enter/update/complete/fail/stop/release 生命周期到 state slot。
- [ ] 5.3 迁移 Root、Sequence、Selector、Parallel 和装饰节点的 child cursor/结果语义。
- [ ] 5.4 迁移 child activation、parent-child identity、generation 和 cause sequence。
- [ ] 5.5 迁移 Self/LowerPriority 中断选择、source exit barrier 和 ForceStop 传播。
- [ ] 5.6 迁移 Value/Bool/Int/算术/And/Or/Not 等纯运算 operation。
- [ ] 5.7 迁移 Wait/Time 节点为 SimulationTick 计时，删除 Time.deltaTime 依赖。
- [ ] 5.8 迁移 Random 节点为 SimulationState RNG，删除 Unity Random 依赖。
- [ ] 5.9 迁移 lifecycle/interrupt/result structured Trace，保留 authoring source 反查。
- [ ] 5.10 删除角色主线 RunnableNode 私有运行状态和 authoring clone 执行依赖。

## 6. 迁移 StateMachine 与打断生命周期

- [ ] 6.1 迁移 StateMachine enter、active、pending、exiting 和 complete 状态。
- [ ] 6.2 迁移 Transition ConditionRuleGraph 求值、priority、stable edge order 和 decision fact。
- [ ] 6.3 迁移 StateBehaviorSubTree OnEnter/Root/OnExit 与 graceful stop barrier。
- [ ] 6.4 迁移 nested StateMachineExecutionPath 与 outer-to-inner scope owner。
- [ ] 6.5 迁移 StateEnterToExit Blackboard frame 创建、访问和精确释放。
- [ ] 6.6 迁移 upper Tree interruption 对嵌套 SM/State body 的 release 链。
- [ ] 6.7 保持 StateMachine 只发布逻辑事实，不生成动画 owner/ready/topology。
- [ ] 6.8 删除 StateMachineGraphRuntime 的隐藏可变状态和 runtime graph clone。

## 7. 迁移 Timeline、TreeClip 与 Blackboard

- [ ] 7.1 迁移 TimelineNode request、playback、loop、complete、stop 和 release 状态。
- [ ] 7.2 迁移 Timeline logic time 为 SimulationTick/Tick fraction 数据，不读取 Unity 帧时间。
- [ ] 7.3 迁移 Decision TreeClip 在 RootTree 前无状态求值和 Frame Blackboard 写入。
- [ ] 7.4 迁移 Commit TreeClip Enter/Update/Exit/Destroy 和 stop 生命周期。
- [ ] 7.5 迁移 loop 跨边界尾段/中间 cycle/头段采样顺序。
- [ ] 7.6 迁移 Timeline motion contribution、CurveEndFrame、EndFrame 和 channel claim 语义。
- [ ] 7.7 迁移 TreeClip ActionWindow projection、Action Context 和 stable WindowId/Digest。
- [ ] 7.8 迁移 Blackboard Character/Graph/State/ActionInstance/Frame owner bucket 寻址。
- [ ] 7.9 迁移 Config 只读、Spawn/ManualClear、GraphInstance、StateEnterToExit、ActionInstance 和 Frame lifetime。
- [ ] 7.10 迁移 Blackboard write provenance 与 fact projection，不创建第二份 window 数据源。
- [ ] 7.11 删除 TimelineRunningTree runtime clone、Timeline 自主播放路径和旧 Blackboard runtime dictionary。

## 8. 迁移输入、Action 与 GameplayEffect

- [ ] 8.1 将 InputAction 采样与 Camera-relative 方向换算留在 Unity Input Adapter。
- [ ] 8.2 将连续输入和离散 request 写入 CharacterSimulationInput，保留 InputId 与 sequence。
- [ ] 8.3 将 Graph input operation 改为只读取当前 portable input，不读取 Camera/InputAction。
- [ ] 8.4 迁移 Action request buffer、ActionInstanceId、activation、accept/reject/cancel/complete 状态。
- [ ] 8.5 迁移 Attack1/Attack2 combo request、window gate、interrupt 和 terminal lifecycle。
- [ ] 8.6 迁移 GameplayEffect Tag、Attribute、ActiveEffect、PredictionJournal 和 ChangeSet 状态。
- [ ] 8.7 保持 GE 窄合同与 Character adapter 分层，Kernel operation 不引用 Presentation/Network/Diagnostics。
- [ ] 8.8 将 Action/Effect/Cue/Attribute 结果投影为 typed SimulationOutput。
- [ ] 8.9 删除 Action/GE 中影响未来 Tick 但未进入 SimulationState 的字段。

## 9. 建立 SimulationKernel、Local Driver 与 Unity Solver

- [ ] 9.1 建立 SimulationKernel.Step 的明确输入、状态更新和输出顺序。
- [ ] 9.2 建立稳定 Actor/operation 排序和单 Tick 事实序列。
- [ ] 9.3 建立 motion contribution resolve、modifier、portable request 和 World Solver 调用顺序。
- [ ] 9.4 建立 ICharacterWorldSolver 的 capability、state capture、request 和 result 合同。
- [ ] 9.5 实现 UnityCharacterControllerWorldSolver adapter，保持 CharacterController.Move 唯一 concrete 调用点。
- [ ] 9.6 将 Unity solver float 结果量化为 portable BodyState/MotionResult。
- [ ] 9.7 建立 ISimulationDriver composition contract 和 actor binding contract。
- [ ] 9.8 实现 LocalSimulationDriver 的固定 Tick、单次 Step 和立即 commit。
- [ ] 9.9 将 CharacterPipelineHost 收口为 Program、State、InputAdapter、Driver、Solver、Projection 和 Committer 装配器。
- [ ] 9.10 删除 CharacterMotionAuthority、LocalSolver/ExternalPose/None 总控分支和 Transform fallback。
- [ ] 9.11 将现有 ServerAuthoritativeHybrid Character binding 迁到最终 Driver/input/output 合同。
- [ ] 9.12 将 model correction/history 从 MotionStage 迁回 ServerAuthoritative model，保持现有 LocalLoopback/disconnected 能力不扩张。
- [ ] 9.13 删除 Character NetworkSendStage/ReceiveStage 与 model Driver 的双写路径。

## 10. 建立 State Capture、Restore、Hash 与 Diagnostics

- [ ] 10.1 建立 SimulationState canonical serialization 与反序列化。
- [ ] 10.2 建立按 State Layout 完整 capture/restore 的原子替换。
- [ ] 10.3 将 World Solver state/capability 纳入 snapshot header，不把 Unity object 写入 snapshot。
- [ ] 10.4 建立 ProgramHash、layout hash、ActorId 和 Tick 的 restore 匹配校验。
- [ ] 10.5 建立 stable state hash，排除 diagnostics、Presentation resource 和 Unity object。
- [ ] 10.6 将 operation enter/exit/result、SM transition、Timeline sample、Blackboard write、Motion、Action 和 Effect 发布到现有 structured Trace。
- [ ] 10.7 将 ProgramHash、state slot、snapshot identity 和 solver result 加入 Debug Source Map/Trace。
- [ ] 10.8 保持 RuntimeDebugSession/Live/Capture 只读，不让 Editor 持有 SimulationState mutable view。

## 11. 迁移 Presentation 与副作用提交

- [ ] 11.1 建立 CharacterPresentationProjection 与 Program producer identity 的一对一绑定。
- [ ] 11.2 建立 SimulationOutput presentation command 的稳定 EventId 生成。
- [ ] 11.3 建立 SimulationCommitter 对 animation selection/sample/release 的唯一提交入口。
- [ ] 11.4 保持 Animancer 对 state/fade/layer 的执行权威，Kernel 不维护 CrossFade/Inertialization。
- [ ] 11.5 保持 Timeline logic time、visual sample time 和 Animancer presentation delta 的分层。
- [ ] 11.6 保持 logic pose 与 visual root 分离，Unity Solver 只更新逻辑 body。
- [ ] 11.7 将 Camera、Cue、VFX、UI 命令收口到 Committer adapter，不在 Kernel 直接触发。
- [ ] 11.8 删除从 Tree 结构、runtime clone 或 MotionDebug 反向推断表现结果的路径。

## 12. 迁移 Corin 资产并恢复单机闭环

- [ ] 12.1 编译 Corin RootTree 与 Locomotion/Action nested StateMachine 全部可达 operation。
- [ ] 12.2 迁移 Idle、WalkStart、WalkLoop、WalkEnd、RunStart、RunLoop、RunEnd 和 MovingTurn 状态数据。
- [ ] 12.3 迁移 Shift 单次闪避、闪避结束输入进 Run 和无输入进 End 语义。
- [ ] 12.4 迁移 None/Attack1/Attack2 nested Action StateMachine、combo request 和终止生命周期。
- [ ] 12.5 迁移 Attack/Dodge Timeline、TreeClip Window、motion curve、cue 和 interrupt 条件。
- [ ] 12.6 迁移 Corin Blackboard declaration/scope/value 和 transition ConditionRuleGraph 引用。
- [ ] 12.7 迁移 Corin Action/Behavior/GameplayEffect/Attribute 绑定与 portable catalog。
- [ ] 12.8 生成 Corin CharacterSimulationProgramAsset 与 CharacterPresentationProjection。
- [ ] 12.9 将 Sandbox Host 改为显式装配 Local Driver、Unity Solver、Input Adapter 和 Committer。
- [ ] 12.10 删除 Corin 资产中的旧 runtime clone/config、authority mode、废弃 network stage 和双数据源。
- [ ] 12.11 删除一次性 asset migrator，保留最终序列化数据。

## 13. Editor、文档、清理与校验

- [ ] 13.1 在 CharacterPipelineDefinition Inspector 显示 Program source revision、compile status、ProgramHash、capabilities 和精确错误。
- [ ] 13.2 将 Agent snapshot/validator 改为只读取正式 authoring 和 compile report，不生成第二份 Program schema。
- [ ] 13.3 删除角色主线 old interpreter、runtime clone、runtime authoring reflection 和废弃 type。
- [ ] 13.4 删除 runtime compile fallback、stale Program fallback、Transform fallback、默认 Solver 和一次性 parser。
- [ ] 13.5 使用 `rg` 确认不存在 CharacterMotionAuthority、Character NetworkSend/Receive 双写、MotionStage model correction 和 Corin runtime clone 执行入口。
- [ ] 13.6 更新 `openspec/project.md` 的 Character gameplay、BTSMTL runtime、Motion、Presentation、Diagnostics 和 Network adapter 口径。
- [ ] 13.7 更新本 change 影响的 current specs，删除 object interpreter、runtime clone、CharacterMotionAuthority 和公共 correction stage 旧真相。
- [ ] 13.8 以 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 portable core 与 Unity runtime 相关工程。
- [ ] 13.9 以 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 Editor/Agent 相关工程。
- [ ] 13.10 编译结束后立即执行 `dotnet build-server shutdown`。
- [ ] 13.11 运行 `openspec validate refactor-character-simulation-core --strict --no-interactive`。
