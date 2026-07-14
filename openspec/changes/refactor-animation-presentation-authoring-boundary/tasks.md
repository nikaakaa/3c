## 1. 基线与迁移前置

- [x] 1.1 记录当前 Animation、Timeline、BTSMTL、Presentation、Editor、Agent 相关新增和修改文件。
- [x] 1.2 记录当前运行时异常与合法空 StateBehaviorSubTree 的 State.None 行为。
- [x] 1.3 记录 CharacterAnimationPresentationAdapter、ExecutionLineage、Topology、Driver、Registry、Arbitrator、LayerPlan 和 LayerRuntime 的引用链。
- [x] 1.4 记录 Corin RootTree、inline/shared StateMachine、Timeline producer 与 LayerId 的稳定资产身份。
- [x] 1.5 完整导出旧 transition duration、strategy 和 AnimationCurve keyframe 数据。
- [x] 1.6 核对项目内 Animancer TransitionLibraryAsset、ITransition、FadeMode、FadeGroup easing 和 manual update API。
- [x] 1.7 建立旧 transition 数据到 Animancer 原生 transition/easing 的逐项映射表。
- [x] 1.8 确认 Timeline 外部采样可由 Animancer 正式 API 表达；旧自定义 transition strategy、duration 与 curve 属于可删除中间数据，不作为迁移门禁。

## 2. 逻辑选择合同

- [x] 2.1 定义稳定的 AnimationPlaybackId 与 playback generation。
- [x] 2.2 定义逻辑侧 AnimationLayerSelection。
- [x] 2.3 从 AnimationLayerSelection 删除 Priority、Weight、Driver 和 Tree route。
- [x] 2.4 定义每个 LayerId 每次逻辑提交最多一个 selection 的不变量。
- [x] 2.5 定义 RequireOutput 与 AllowEmpty 的空选择语义。
- [x] 2.6 在 CharacterGraphContext 暴露逻辑 selection 提交边界。
- [x] 2.7 在 CharacterPipelineFrame 保存本次逻辑 selection 结果。
- [x] 2.8 对同层重复 selection 报告全部逻辑来源并拒绝批次。
- [x] 2.9 保留 StateMachine priority、Tree interruption 和 ActionOverride 的逻辑所有权。
- [x] 2.10 删除 AnimationContribution.Priority 的合同与消费。

## 3. BTSMTL 反向依赖清理

- [x] 3.1 删除 RunnableNode 为动画新增的 activation entered/executed/released 发布。
- [x] 3.2 删除 TreeControlFlowCommitted 的动画 sink 和 adapter 调用。
- [x] 3.3 恢复空 StateBehaviorSubTree 返回 State.None 的合法逻辑语义。
- [x] 3.4 删除 State.None 到 InvalidExecuted 的动画特化检查。
- [x] 3.5 保留 Runnable natural/graceful/force stop 逻辑协议。
- [x] 3.6 保留 StateMachineExecutionScope 与 Blackboard 状态作用域。
- [x] 3.7 保留 State running facts 和 ConditionRuleGraph 读取。
- [x] 3.8 删除 StateMachine transition 的动画 control-flow 投影。
- [x] 3.9 删除 Graph presentation leaf 和 nested animation owner 传播。
- [x] 3.10 确认 BTSMTL runtime 程序集不引用 Character/Pipeline/Animation 命名空间。

## 4. Corin 逻辑所有权闭环

- [x] 4.1 识别 Corin Locomotion、Action、Dodge 和 nested Attack 的 active playback。
- [x] 4.2 将 Locomotion 当前状态映射为 Base layer selection。
- [x] 4.3 将 ActionOverride 活跃动作映射为 Base layer selection。
- [x] 4.4 让 Dodge 选择覆盖 Locomotion，但不停止仍需推进的 Locomotion 逻辑时间。
- [x] 4.5 让 Attack1 与 Attack2 的 nested StateMachine 输出唯一 Base selection。
- [x] 4.6 让 Attack/Dodge 结束后由逻辑输入和 locomotion 状态选择 RunLoop、RunEnd 或其它正式 producer。
- [x] 4.7 删除 Corin 对 AnimationTrack.Priority 的覆盖依赖。
- [x] 4.8 删除 Corin Tree/State edge 上的 HandoffRole、Driver 和 presentation transition 字段。
- [x] 4.9 校验 Corin 每个逻辑提交的 Base selection 唯一。
- [x] 4.10 若现有图无法在不新增第二仲裁器的情况下表达唯一选择，停止并说明需要修改 authoring 的取舍。

## 5. Timeline 表现采样

- [x] 5.1 保留 TimelinePlaybackScheduler 的逻辑时间权威。
- [x] 5.2 从 AnimationTrack 删除跨 producer Priority。
- [x] 5.3 保留 LayerId、clip 时间、loop、ease 和 producer 内部 Weight。
- [x] 5.4 让表现帧只采样最终 selected playback。
- [x] 5.5 为即将淡出的 current playback 创建 PresentationRetention。
- [x] 5.6 让 retained outgoing 只执行 animation track 表现采样。
- [x] 5.7 禁止 retained outgoing 重新执行 TreeClip、Motion、root motion、window 或 sync facts。
- [x] 5.8 在 fade 完成后释放 PresentationRetention。
- [x] 5.9 在 pipeline deactivate 时立即释放全部 retention。
- [x] 5.10 保证 loop playback 在 logic tick 之间按 visual Timeline time 连续采样。
- [x] 5.11 保证旧 generation sample 不会匹配新的 playback selection。

## 6. Animation 播放生命周期

- [x] 6.1 定义 PendingFirstSample、Current、Outgoing 和 Retired 状态。
- [x] 6.2 建立每 LayerId 一个 AnimationPlaybackLifecycleState。
- [x] 6.3 按完整批次消费 selection、sample、complete 和 release。
- [x] 6.4 实现无 current 时首个 ready selection 进入 Current。
- [x] 6.5 实现 target 未采样时 PendingFirstSample 保持现有 Current。
- [x] 6.6 实现 target 首样本到达后的原子 Current 切换。
- [x] 6.7 实现 AllowEmpty layer 的正式淡出到空。
- [x] 6.8 实现 RequireOutput 首次无输出的明确错误。
- [x] 6.9 实现 selected target 未采样即 complete/release 的明确错误。
- [x] 6.10 实现 fade 完成后的 outgoing retire。
- [x] 6.11 实现 pipeline reset/deactivate 的立即清理。
- [x] 6.12 删除 Registry 的候选收集、priority 和 owner membership 仲裁语义。

## 7. Animancer 播放适配

- [x] 7.1 为单 clip producer 创建或复用 AnimancerState。
- [x] 7.2 为同一 producer 内多 clip sample 创建或复用 ManualMixerState。
- [x] 7.3 将 Timeline clip time 写入对应 Animancer child state。
- [x] 7.4 将 Timeline producer 内部 Weight 写入 ManualMixerState child。
- [x] 7.5 使用 stable producer key 复用 Animancer state。
- [x] 7.6 通过 TransitionLibrary.Play 或 AnimancerLayer.Play 启动目标状态。
- [x] 7.7 将正式 easing 交给 Animancer FadeGroup。
- [x] 7.8 让 Animancer 使用 presentation delta 推进 fade。
- [x] 7.9 删除项目自算 incoming/outgoing state weight。
- [x] 7.10 删除 LayerPlan、ActiveHandoff 和 custom crossfade 状态机。
- [x] 7.11 删除项目自有 Inertialization 执行路径。
- [x] 7.12 从 Animancer fade/state 完成信号驱动 lifecycle retire。
- [x] 7.13 处理 fade 重入时复用当前 Animancer 视觉图，不建立 handoff stack。

## 8. Pipeline 单向集成

- [x] 8.1 将跨 logic tick 队列改为只保存 selection、sample、complete 和 release。
- [x] 8.2 删除队列中的 topology、Driver、ready 和 handoff command。
- [x] 8.3 在 PresentationFrame 聚合多个 logic tick 的最终 per-layer selection。
- [x] 8.4 保留 playback generation 的 complete/release 顺序。
- [x] 8.5 在同一批次完成 Timeline sample 与 lifecycle commit。
- [x] 8.6 只在 lifecycle commit 成功后 acknowledge 队列。
- [x] 8.7 将 CharacterPresentationStage 接到 AnimancerPlaybackAdapter。
- [x] 8.8 删除 CharacterAnimationPresentationAdapter。
- [x] 8.9 删除 CharacterAnimationExecutionLineage。
- [x] 8.10 删除 AnimationTopologyRecord、DriverBindingIndex 和 causal graph。
- [x] 8.11 删除 Arbitration 目录及全部引用。
- [x] 8.12 确认最终运行链只有 Logic Selection -> Timeline Sample -> Playback Lifecycle -> Animancer。

## 9. Presentation authoring 与资产

- [x] 9.1 定义 CharacterAnimationPresentationDefinition 的 Layer catalog。
- [x] 9.2 为 Timeline animation producer 定义 stable presentation key。
- [x] 9.3 在 Definition 中引用唯一 Animancer TransitionLibraryAsset。
- [x] 9.4 定义 producer 到 Animancer transition key/source 的绑定。
- [x] 9.5 禁止 Definition 保存 Priority、Driver、Tree site 或 custom source-target transition table。
- [x] 9.6 将 Layer catalog、TransitionLibrary 与 producer binding 收敛到 CharacterPipelineDefinition Inspector。
- [x] 9.7 在 Definition Inspector 中按稳定 identity 列出正式 producer 与 LayerId，不复制逻辑 flow。
- [x] 9.8 从 Definition Inspector 定位并打开 Animancer TransitionLibrary 正式编辑入口。
- [x] 9.9 删除独立 Animation Presentation 窗口，保持 Graph 和 Timeline 为两个可同时打开的正式窗口。
- [x] 9.10 删除 Tree/StateMachine Inspector 的 animation transition 字段。
- [x] 9.11 删除旧 Presentation Driver Inspector 和 binding 编辑入口。
- [x] 9.12 先迁移 Corin Layer、producer key 和 TransitionLibrary 绑定。
- [x] 9.13 保存新资产后删除 m_AnimationTransitionDefinitions、m_HandoffRole、m_ExternalExitTransition 和旧 m_AnimationLayers 路径。
- [x] 9.14 删除一次性 migrator，不保留 FormerlySerializedAs、lazy migration 或双写。

## 10. Agent、Diagnostics 与文档收口

- [x] 10.1 从 Agent schema 删除 Presentation Driver、Tree animation site 和 animation priority。
- [x] 10.2 从 Agent Patch compiler 删除旧 Driver 与 HandoffRole operations。
- [x] 10.3 从 Agent Snapshot exporter 删除 Driver/Lineage/LayerPlan 投影。
- [x] 10.4 从 Agent Validator 删除通用 Presentation binding 校验。
- [x] 10.5 保留 Agent 对 logic Graph、StateMachine、Timeline 和 stable producer identity 的只读理解。
- [x] 10.6 将 Animation Trace 改为 selection、sample、Pending/Current/Outgoing/Retired 和 Animancer fade。
- [x] 10.7 从 Trace 删除 Driver、ExecutionLineage、CausalGraph、Arbitrator 和 LayerPlan。
- [x] 10.8 更新 openspec/project.md 的动画主链、模块边界和 Agent 口径。
- [x] 10.9 更新受影响 current specs，删除与本 change 冲突的旧要求。
- [x] 10.10 从 Agent Patch schema、identity binder、compiler dispatch 和 reference validator 删除 `configure_animation_layer`。
- [x] 10.11 删除 Agent Patch 的 animation layer payload 与专用 AvatarMask resolver。
- [x] 10.12 保留 Agent Presentation Snapshot 只读 Layer/producer identity，并确认没有写回入口。
- [x] 10.13 从 Layer authoring、resolved contract、binding index、Snapshot 和 Corin Definition 删除无效 apply flag。
- [x] 10.14 定义逻辑侧 producer 候选只携带 playback、layer 与 ActionInstance 归属。
- [x] 10.15 让逻辑选择器读取 ActionRuntime 当前唯一 ActionInstance 解析 Action/Locomotion 所有权。
- [x] 10.16 让 TimelinePlaybackScheduler 只收集 producer 并委托逻辑选择，不再按 `ActionContext.IsValid` 分桶。
- [x] 10.17 保留同一所有权域、同一 layer 多 producer 的明确冲突并拒绝提交。
- [x] 10.18 在 TimelineMotionCurveContribution 中分离 `HasDelta` 与 `ClaimsLowerChannels`。
- [x] 10.19 在 MotionContribution 中分离 `HasDelta` 与 `ClaimsLowerChannels`。
- [x] 10.20 让 MotionResolver 接受零 delta Override claim 并正式消费低层 channel。
- [x] 10.21 为 MotionCurveClip 增加显式 `CurveEndFrame`，分开曲线采样结束与 channel claim 结束。
- [x] 10.22 严格校验每个 MotionCurveClip 的 `StartFrame < CurveEndFrame <= EndFrame`。
- [x] 10.23 迁移 Corin 全部 MotionCurveClip 的 `CurveEndFrame`，不保留缺省兼容解释。
- [x] 10.24 让 Attack1/Attack2 保持原曲线终点 49/48，并将零位移 Action channel claim 延续到 recovery 结束帧 80。
- [x] 10.25 将 Decision TreeClip 求值从 target-time 包含判断改为 tick segment 相交判断。
- [x] 10.26 让 Loop Timeline 的 Decision TreeClip 依次求值尾段、中间 cycle 和头段。
- [x] 10.27 将 BaseGraph 初始化收敛为非虚公开入口、上下文校验钩子和派生完成钩子。
- [x] 10.28 迁移 OneRootTree、StateBehaviorSubTree 与 TimelineRunningTree 的派生初始化逻辑。
- [x] 10.29 确认 TimelineRunningTree 普通 InitTree 明确失败，正式 InitTimelineTree 可完成 root 与 lifecycle 解析。
- [x] 10.30 搜索并删除旧操作、旧 apply flag、旧 `HasMotion` contribution 过滤和 Scheduler 双桶残留。
- [x] 10.31 使用带 --disable-build-servers /nr:false /p:UseSharedCompilation=false 的静态构建命令编译受影响运行时程序集。
- [x] 10.32 使用同样参数编译受影响 Editor/Agent 程序集并修复引用断裂。
- [x] 10.33 构建结束后立即执行 dotnet build-server shutdown。
- [x] 10.34 运行 openspec validate refactor-animation-presentation-authoring-boundary --strict --no-interactive。
- [x] 10.35 确认全部任务真实完成后再将本清单标记为已完成。

## 11. 删除冗余 Presentation 窗口

- [x] 11.1 修改 proposal、design、delta spec、current spec 与 project 口径，删除第三个独立窗口要求。
- [x] 11.2 在 CharacterPipelineDefinition Inspector 中读取内联 Animation Presentation Definition。
- [x] 11.3 在 Definition Inspector 中编辑 Layer catalog。
- [x] 11.4 在 Definition Inspector 中编辑唯一 TransitionLibrary 引用。
- [x] 11.5 在 Definition Inspector 中按 RootTree 正式 producer identity 列出 binding。
- [x] 11.6 在 Definition Inspector 中通过 authoring service 写入或删除 producer binding。
- [x] 11.7 保留从 Definition Inspector 打开来源 Graph、Timeline 与 Animancer transition 资产的定位入口。
- [x] 11.8 删除独立 CharacterAnimationPresentationWindow、菜单、Definition 按钮与 meta。
- [x] 11.9 删除只服务旧窗口的 producer flow 投影和重复 RuntimeDebugSession UI。
- [x] 11.10 搜索代码与现行文档，确认不存在独立 Presentation 窗口和第二份配置入口。
- [x] 11.11 使用规定参数静态编译受影响 Editor 程序集并修复引用断裂。
- [x] 11.12 构建后立即执行 dotnet build-server shutdown。
- [x] 11.13 运行 openspec validate refactor-animation-presentation-authoring-boundary --strict --no-interactive。
- [x] 11.14 确认本节全部真实完成后再标记为已完成。

## 12. 审计回补与分裂路径清理

- [x] 12.1 确认 Corin、场景和代码没有使用旧 TreeValueNode 与 MuteTrackNode。
- [x] 12.2 删除 TreeValueNode、TreeValueNodeView、专用 UXML/USS 及其 Unity meta。
- [x] 12.3 删除 MuteTrackNode、Timeline RuntimeMute 与 RuntimeMuted 状态。
- [x] 12.4 删除 Timeline Track/Clip 的 Bind、Unbind、Rebind、Evaluate、SetTime 自主播放生命周期。
- [x] 12.5 修复 TimelineData.Init 对 Track 的重复初始化。
- [x] 12.6 删除已完成使命的 ConditionRuleGraph ownership 一次性 migrator 及其 Unity meta。
- [x] 12.7 清除 Corin VisualRoot Animator Controller，保证 Animancer 是唯一 Animator 播放输出源。
- [x] 12.8 将缺失 Timeline stop context 从 Shutdown fallback 改为明确失败。
- [x] 12.9 将缺失 Animation producer binding 的 Linear easing fallback 改为明确失败。
- [x] 12.10 让 CharacterPipeline 在 ActionProfile 或 Definition 配置非法时拒绝构造。
- [x] 12.11 让 ExposedPropertyNode 在 Pipeline Blackboard runtime 缺失或写入失败时明确失败，不读写 authoring 默认值。
- [x] 12.12 让 Agent compiler 拒绝非法 lifecycleType，不默认生成 Complete。
- [x] 12.13 建立 Graph、Node reference、ConditionRuleGraph、Timeline 与 TreeClip Graph 的统一 authoring topology 投影。
- [x] 12.14 让 Animation producer discovery 只消费统一 topology 投影，不自行递归 Graph。
- [x] 12.15 让 Agent graph index、transaction owner collector 与 Validator 消费统一 topology 投影并覆盖 TreeClip Graph。
- [x] 12.16 让 runtime diagnostics source map 消费统一 topology 投影并保持 Graph、Timeline、Track、Clip 父子关系。
- [x] 12.17 删除未被消费的 TimelineSources 投影和重复 Graph 遍历辅助类型，让 Snapshot Graph 引用只使用 topology 投影路径。
- [x] 12.18 修复 TimelinePreviewSession 首次 evaluation tick 为零的问题。
- [x] 12.19 将 Timeline preview queue、playback generation、lifecycle、adapter 和 snapshot 改为每个 session 独立持有，由 Host 统一持有预览 Graph clock，并拒绝两个 session 竞争同一物理目标。
- [x] 12.20 让 Timeline preview fade 使用真实 presentation delta，seek/reset 使用零 delta。
- [x] 12.21 禁止 Authoring Preview 在 Play Mode 与正式 CharacterPipeline 共享 Animancer 输出。
- [x] 12.22 删除无调用的 Presentation authoring mutation API 与其它本轮确认的死投影。
- [x] 12.23 更新 project.md、current diagnostics spec 与实际链路和 spec 数量一致。
- [x] 12.24 搜索确认旧 Timeline autonomous playback、TreeValue、迁移器、Animator Controller 第二输出和静默 fallback 均已消失。
- [x] 12.25 使用规定参数静态编译受影响 Runtime 与 Editor 程序集并修复引用断裂。
- [x] 12.26 构建后立即执行 dotnet build-server shutdown。
- [x] 12.27 运行 openspec validate refactor-animation-presentation-authoring-boundary --strict --no-interactive。
- [x] 12.28 确认本节全部真实完成后再标记为已完成。

## 13. 运行边界审计修复

- [x] 13.1 将 CharacterNetworkSendStage 收敛为单 logic tick 输出，不跨 catch-up tick 重复累积事实。
- [x] 13.2 在 CharacterGameplaySyncDriver 完成 outgoing 映射后立即清空本 tick NetworkSendStage 输出。
- [x] 13.3 让 MotionCorrection 与 gameplay intent、modifier 合并后只执行一次 CharacterController.Move。
- [x] 13.4 让 Full correction 以 authoritative target 替换最终 intent，而不是在原 intent 上重复叠加目标位移。
- [x] 13.5 让 MotionCorrectionApplicationResult 记录实际落点差值，并只在实际达到请求落点时标记 Applied 和提交 acknowledgement。
- [x] 13.6 让 retained outgoing 从最后一次真实 visual sample 继续推进，不跳到未渲染的 terminal logic time。
- [x] 13.7 在 Pipeline activate/deactivate 清理 Presentation 插值样本并将 visual root 恢复到当前 logic root 绑定姿态。
- [x] 13.8 在 Pipeline activate/deactivate 清理 Camera resolver、modifier、basis、pending cue、terminal action 与 look input。
- [x] 13.9 让多个 catch-up logic tick 的 CameraCue 与 terminal action 在同一 presentation frame 统一消费。
- [x] 13.10 保证相机提前返回时仍先移除 terminal action 所属的 active camera cue。
- [x] 13.11 在 Pipeline deactivate 清理 NetworkReceiveStage 缓存、ActionRuntime 执行态和 frame transient output。
- [x] 13.12 将 ActionRuntime 调试集合改为本 logic tick 待发布事件，并在统一 Trace 发布后立即清空。
- [x] 13.13 删除未消费的 ActionMotionSample 调试副本、ActiveStateId 与 GameplayWindows 死输出。
- [x] 13.14 将 terminal ActionInstance 的 Profile 仅保留到同 tick 网络策略读取完成，并在下一 logic tick 释放。
- [x] 13.15 让 AnimationPlaybackLifecycleSnapshot 从 Animancer adapter 读取 state key、sample time 与 sample presence。
- [x] 13.16 删除 Animation PresentationStage 的 PresentationCue 死输出，并确认 Snapshot、GameplayResult、StateEffect 与 PresentationSync cue 的网络消费归属 add-local-two-client-gameplay-network-closure。
- [x] 13.17 使用规定参数静态编译 Runtime 程序集并修复引用断裂。
- [x] 13.18 使用规定参数静态编译 Editor/Agent 程序集并修复引用断裂。
- [x] 13.19 构建后立即执行 dotnet build-server shutdown。
- [x] 13.20 运行 openspec validate refactor-animation-presentation-authoring-boundary --strict --no-interactive。
- [x] 13.21 搜索确认不存在旧 cursor 调试历史、重复 NetworkSend 帧缓存、死 Motion debug 副本或已删除输出字段。
- [x] 13.22 确认本节全部真实完成后再标记为已完成。
