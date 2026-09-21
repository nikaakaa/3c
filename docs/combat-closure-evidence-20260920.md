# 战斗闭环运行证据

## 当前状态

尚未闭环。本文不是完成通知；早期交接约定保留为历史，其他任务是否开展整理不代表这里的运行验收已经通过。

## 2026-09-22 运动采样与测试资产清理

- 后续重载恢复后 Console 0 error；DodgeBack 正式 generate_assets 返回 saved=true / diagnostics=[]。测试轨道、Marker、孤立子图的三个身份已从资产消失。此前失败的清理曾导致私有子资产重建，Definition 第 7 个 Timeline 引用失效；已通过 SetControlMotionTimelines 正式配置 API 精确重绑 DodgeBack，重新发布后变更仅落在 DodgeBack 两份运行数据和该 Definition 引用。已重新请求同一 1492 帧回放，尚待完成证据。

- `d2fe3fd79` 将实际 MovingTurn 控制运动接入准备好的 Fixed 曲线。实际链为控制模块 SourceCurve 请求 → ControlMotionBindings → FixedCharacterControlMotionRuntime；并不执行独立 MovingTurn Timeline 的 TreeClip，因此独立 Prepare 的缺树结果不是该转身链的运行阻塞。真实绑定 28 帧增量之和与整段采样 raw 值一致，yaw 为 180 度；预热后 4096 次 EvaluateDelta 分配为 0，仅覆盖该采样函数。
- 相同录制曾推进到 131 个 issued ticks，运动诊断读取已解绑的 Action，报 state slot 574 requires a bound Action instance。`49893d97e` 让运动贡献携带提交时的 SourceGeneration，经运动选择传至诊断，删除帧末再次读取 invocation 的旧路径；Unity 编译通过。尚无完整回放结果。
- 后续回放遇到 DodgeBack 新增测试 Marker 的表现图未发布。用户明确同意删除该测试内容；正式生成已移除测试轨道与 Marker，但发现孤立子图残留。正在修复正式子资产清理：生成完成后扫描孤立子资产，普通编辑仅释放本次断开的引用，避免清理尚未绑定的新建内容。该清理尚待再次生成与发布核对。
- 清理修复静态 Editor 构建成功，92 warnings / 0 errors，已关闭构建服务器。随后 Unity 刷新遇到并行改动的 CS0117：Float32PresentationGraphRuntime 仍使用已删除的 CameraResponseRequest / CameraTargetRequest；未修改这些在途文件，停止生成与发布。孤立子图清理和运行回放不能计为通过。

## 2026-09-21 继续动作闭环

- 当前任务已设置 active goal，沿主目录继续，不创建 worktree、不新增测试。用户确认另一个任务正在小步处理 GC，允许本任务继续；不得覆盖其在途修改。
- 当前正式运动链已存在：Timeline evaluation → CharacterTimelineHost.CopyPendingTimelineMotion → Fixed/Float32CharacterEvaluationRuntime → ResolveMotion。旧记录中的“尚未接入”已过时。
- `587557d34` 修复多 Timeline 收集：Copy 每次清空接收列表，因此必须每条读取后立即合并；MotionWarp 在帧开始清空各技能结果，再逐条读取并分发。Fixed/Float32 同步。补齐 Fixed Unity 对 RootMotion 程序集的正式引用，Unity 刷新重载后 Console 0 error。
- 复用 Center 改动 `dd37173262704b34bb1aa04056de01a7`。RunHost 的 character.replay before 请求返回 WorkspaceEditorInUse，未产生 RunId；保留主验收 Editor，没有创建替代执行器。随后使用项目已有 character.fixed_input_trace 正式入口采集现场，不把它表述成 Center A/B 通过。
- 第一轮 1492 帧 replay_start 使用 Trace `f169da25c67742aaafa0e9860ae4a230`，工具正确绑定 fixed-player，但角色注册报 Fixed Gameplay Ability payload version is unsupported，自动退出 Play，未进入动作回放。
- `3969e56a9` 通过正式 Republish Corin Ability Data 菜单重建四组 Fixed/Float32 共八份资产。正式 FixedData.Load 读取四份成功：Attack SourceMap=2063、DodgeBack=195、DodgeForward=195、RushAttack=1057。只覆盖加载，不代表动作正常。
- 第二轮相同 trace 越过 payload 加载，角色注册时报 AbilityTimelineMotionWarpCatalog 类型初始化失败：Empty 把字符串 empty 传给要求 64 位十六进制的 StableHash。已改为正式 SimulationIdentity.Hash；目录输入先校验 Timeline/Clip 身份，再由目录分配 Operation，避免要求输入已有尚未分配的 Operation；查询键改为字符串二元组，删除逐次 string.Concat。
- 上述 MotionWarp 目录修复尚未载入运行复核。当前刷新遇到并行 GC 改动编译错误：EventIdBuilder 的 stackalloc span 传递 CS8352/CS8350，以及 RuntimeExecutionTimeline 的数组 Count、AddCount、列表/数组和 unmappedEventCount 残留。未改动这些文件。Editor 最近读取为非 Play、非编译；回放没有完成 Proof。
- 下一步：待当前程序集可编译后，先在 Edit 模式复核空目录及真实目录初始化，再用同 trace 继续角色注册和动作推进；继续完成 Fixed 曲线采样和 MotionWarp 数值合同。当前普通曲线仍在 float 采样后转 FixedScalar，MotionWarp 合同仍携带 float 源位置/进度，不能宣称完整定点或整链路 0 GC。

代码入口：TimelineControlContracts.cs 的 AbilityTimelineMotionWarpCatalog；CharacterTimelineHost.cs 的 CopyPendingTimelineMotion/CopyPendingMotionWarps；Timeline.MotionCurve.cs 的采样；TimelineRuntimePreparation.cs 的分段求值；Fixed/Float32CharacterEvaluationRuntime.cs 的贡献合成。下一阶段应复用正式曲线数值实现并在内容准备时构建数据，不在逐帧读取时转换曲线或增加旁路。

### 普通位移定点采样迁移

- `ac49eb93a` 将普通 MotionCurve 的逻辑采样从作者轨道移至 TimelineRuntimeMotionSampling。准备阶段构建数值曲线，Fixed 使用现有 FixedGameplayAbilityCurve，Float32 使用复制的 AnimationCurve；TimelineRuntimePreparationResult 持有数据，播放实例复用。推进按现有跨循环分段传入定点时间；位移、Yaw、权重以 FixedScalar 传给 Host，删除 Fixed 逻辑先 float 采样再转定点的普通位移路径。MotionWarp 尚未迁移。
- 未新增测试。执行 `dotnet build BTSMTL.Timeline.Runtime.csproj --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false -v:q`，结束后已执行 `dotnet build-server shutdown`。模块编译报告一个错误：TimelineRuntimePreparation.cs 的 Marker EventId 仍调用并行迁移已删除的单参数构造。新采样模块无编译诊断，但整体编译未通过，不能视为运行验证。
- 在继续时发现 CopyPendingMotionWarps 已被另一任务改动：增加 sourceStart/sourceEnd/sourcePrevious/sourceCurrent 局部采样，窗口起止改用 warpClip。该函数也必须做定点迁移，已向用户询问归属，等待明确答复前不覆盖这一段。此前用户只确认可以与 GC 任务并行，并未指定这个实际重叠函数的归属。
- 下一步先处理重叠归属，再完成 MotionWarp 的源位置、进度和目标限制数值合同；复查曲线边界、分段循环与零分配。全部 MotionWarp 修复、普通采样和目录初始化仍需在当前程序集加载后取得正式运行证据。

### Corin 运行继续推进

- 用户明确验收目标为 Corin 完整动作闭环，Timeline 是依赖环节，不是收窄后的终点。Goal 已由用户更新为“3C 动作与 corin 正式运行闭环”。
- `BTSMTL.Timeline.Runtime.csproj` 在并行 EventId 迁移补齐后编译通过，0 error；依要求关闭构建服务器。Unity 已重载当前模块。
- 空 MotionWarp 目录初始化通过，但下一次真实回放发现实际目录仍用 SourceContentHasher 的 16 位 FNV 哈希。`aa4c1e5bf` 改用公开 StableHash.Compute，并统一空输入身份。Edit 下正式 CharacterTimelineAbilityRuntime 构造使用 Corin 的18条Timeline，成功建立5个MotionWarp，schema/content均64位，Console 0 error。
- 18条Timeline的实际定点运动采样准备均成功。13个逻辑MotionCurve片段（普攻1～5的Main/End、前后闪避、MovingTurn180）各预热128次，再通过预编译委托调用正式TimelineRuntimeMotionCurve.TrySample 4096次；GC.GetAllocatedBytesForCurrentThread均为0，输出具有非零位移或Yaw。反射、委托构建、曲线准备、结果收集在计量区间外。只证明普通定点曲线采样，不覆盖整帧、MotionWarp或世界位移。
- 后续回放暴露来源表读取 MotionWarp.Name 时 Clip.Track 未绑定。`90aad0204` 在正式 CharacterTimelineDebugSourceMapFiller 入口调用 Timeline.Init；18份未初始化克隆通过来源表填充，得到148条记录，Console 0 error。
- 再次相同1492帧回放成功进入运行，工具已发出131个Tick后，技能启动触发“Timeline Clip has no owning Track”：正式Prepare先校验未初始化克隆。`f2584ab41` 将准备快照的Clone/Init放到校验之前，后续发现、树合同和依赖准备读取同一快照。
- 使用正式角色依赖解析器，安装四份Fixed技能SourceMap及正式Float32表现程序，然后对未初始化克隆调用正式Prepare：17条战斗Timeline均ready=true/errors=[]。CorinMovingTurnRootMotionTimeline仍失败，缺少tree:bd35fa643bea4339ad9a9a7c7d85aeab，预期revision=2e81e05179a66b590d68e7859cf52bd3cb026920605024a03de89f84ada088b9；需核对控制树是否还有另一正式装配来源，不能无条件注册假句柄。
- f2584ab41之后的1492帧回放已请求并报告开始，但随后并行SLATE改动在CutsceneEditor.cs 815/816行引用已删除的IEmbeddedTimelineBinding.CurrentFrame，编译错误触发PlayModeErrorAutoExit；现为Edit/Idle，无Proof。不能作为完成的动作运行或稳定版本证据。该文件仍在其他任务修改，本任务未覆盖。
- MotionWarp采样函数重叠归属的用户问题仍未收到明确选择；继续保留其修改。下一步需要完成MotionWarp数值链、处理实际转身依赖，并在可稳定运行的当前版本继续攻击/闪避/Rush及收尾、表现和1492帧证据。

## 2026-09-20 后续核对与提交

- 用户确定先完成 Timeline 时间迁移，再继续技能位移接入。任务 `01a0bcde-cf76-7162-9fcd-da6283a04b7a` 最近读取仍为 active，未取得依赖完成结论。
- 回调消费和执行器生命周期修复已由 `68cd6346e` 提交。下文“尚未提交”描述的是当时进度，不能作为现在的 Git 状态。
- `7f12c9b27` 已提供 AbilityTimelineProgress：前后定点秒、循环次数和完成标志。后续位移采样应使用当前正式合同，不再按下文旧方案从前后帧另推时间。
- `cdd41e740` 修复相机旧轮次撤销误删当前请求。Edit 局部探针直接调用正式 Publish/Retire：两轮替换后数量为 1，撤销旧轮次后保持 1，撤销当前轮次后为 0；修改前为 1/0/0。此证据仅覆盖请求轮次隔离，不证明开场鼠标输入正常。
- `5c02c1b58` 提交 Attack1Main/End 曲线目录迁移，六文件内容及 GUID 不变，Unity AssetDatabase 确认两个 GUID 在新目录可加载。`fbef01294` 删除脚本已经退役且无消费者的旧预览夹具。
- 工作区整理提交：`4c82681fb` 渲染持久 ID 映射；`8204397e0` Fixed 正式输入目录；`343bcd81a` Fixed Pass 端口声明；`73f1cd263` Pose 时钟与相机接口；`5a7c15b3b` 脚部素材、校准、Pose 资源映射和生命周期配套；`6a910a155` Rush Section 秒制序列化。这些整理提交没有新增端到端运行证据。
- 正式 RunHost 编译曾返回 WorkspaceEditorInUse，无 RunId。局部相机探针是在现有 Editor 重载后执行，不应描述成正式批处理编译或整段 Replay 通过。Center 记录为 `2d848daee8a343e49f9b7fceaae265e9`。
- 动作窗口候选 BeginFrame/EndFrame 均清空；控制层窗口查询接口未找到业务调用。ClearActionInstanceScopes 当前为空，资源释放必须结合技能执行帧移除核对，不能凭方法名声称完整释放。
- `491ba4935` 收口 GameplayAbilityGraphInvocationLayout 与编译来源、Fixed/Float32 代次读取配套；查询键改为三个字符串字段组成的值类型，Hook 使用固定名称，去掉原 Hook.ToString 和 string.Concat。此处仅静态核对消除显式查询字符串分配，尚无修改后编译、Profiler 或动作回放证据，不能宣称整条链路 0 GC。
- 仍未完成：Timeline 定点位移进入 MotionAccumulator 和世界求解；攻击完成、取消、打断后的运行复核；开场相机输入定位；动画时钟与速度检查；1492 帧整段回放。

以下章节保留原时间点的诊断过程，其中旧状态不覆盖上述最新核对。

### 22:22 左右的现有资产读取

Unity MCP 已恢复连接，目标实例为 e852139597e42532，GameplayLabFixed 在 Edit 模式且无编译，Console 返回零条错误。通过正式 FixedGameplayAbilityDataAsset.Load，传入 Corin Definition.BuildGameplayAbilityProviderBinding，四份现有 Fixed 技能数据均读取成功：Attack SourceMap=2063，DodgeBack=195，DodgeForward=195，RushAttack=1057。这覆盖既有二进制完整性和 Provider 合同检查，不覆盖 SourceRevision 与当前作者资产一致性、运行调用布局查询、动作推进或位移。没有重新发布资产、刷新、启动 Play 或长回放。

### TreeClip 调用布局与局部分配复核

现有 Editor 已执行 `491ba4935` 的布局代码。用四份实际 FixedData 的 SourceMap、Operations 和 Topology 构造正式 GameplayAbilityGraphInvocationLayout，逐一检查 TimelineClip 入口下的 Operation：GenerationSlot 必须等于对应入口在编译拓扑中的 RunnableActivationGeneration 槽，ParentGenerationSlot 必须可解析。Attack 421 组、DodgeBack 40 组、DodgeForward 40 组、RushAttack 288 组，合计 789 组均通过。

每组先预热 10 次，再各重复 GenerationSlot/ParentGenerationSlot 100 次，以 GC.GetAllocatedBytesForCurrentThread 统计查询循环，四份数据各为 0 字节。数据加载、布局构造、调用对象构造和反射探针本身均在计量区间之外；结论仅是这两个查询在上述数据下无托管分配，不覆盖 Timeline 推进、技能执行、Float32 或整帧。没有新增测试文件或启动 Play。此结果更新上文该提交仅有静态检查的历史状态。

## 已完成的修复与资源生成

- `72cb32c0a`：Fixed 回放准备接口明确区分录制角色与接收角色。编辑器入口绑定 `fixed-player`；检查点入口使用 Body 的角色；性能采集明确使用录制角色。没有修改原始录制或另造回放链路。
- 1492 帧录制：`f169da25c67742aaafa0e9860ae4a230`，原角色 `gameplay-lab-player`。用户确认包含攻击、前后冲。尚未用修改后的绑定完成回放验收。
- Corin 正式 Foot Motion authoring 已生成七个战斗动画及脚部曲线。Editor 日志确认 `analyzed 7 AnimationClips and applied 7`。
- Pose Domain Resource Set 已重新编译。Editor 日志确认 `compiled 7 source plans`。动作速度和运行表现尚待核对。

## 普通 Play 探针

2026-09-20，在 GameplayLabFixed 普通 Play 中，Session 状态为 Active，回放模块状态为 Idle，控制源为 UnityFixedCharacterInputAdapter。

通过现有 `FixedCharacterHost.EnqueueAbilityInputRequest("Attack")` 提交攻击后，读取当前世界中 `fixed-player` 的 ActionInstances：

| 字段 | 值 |
| --- | --- |
| ActionId / SkillId | Attack / Attack |
| InstanceId / InputSequence | 1 / 1 |
| StartTick | 2521 |
| LastTransitionTick | 2521 |
| SkillEntryOperation | 1 |
| SkillExecutionGeneration | 1 |
| SegmentGeneration | 0 |
| State / LastTransition | Ended / Complete |
| Reason | TimelineCompleted |

玩家位置在该探针前后均为 `(40.000, 0.010, -3.000)`；Console 没有错误。这证明请求已进入并创建实例，但记录显示同 Tick 结束，需要继续查明。该探针没有覆盖物理按键采集，不能据此宣称手动输入正常。

暂停逐帧复查得到第二次证据：在 Tick 2413 排入 Attack，前进一个 Editor 帧后逻辑 Tick 为 2415；Attack 的 SourceTick、StartTick、LastTransitionTick 均为 2414，Consumed=true，ExpireTick=2426，Reason=TimelineCompleted。运行时 Attack 数据哈希为 `1d7f5319be3220e4cb3f8d7499fd9b33b2b7aec2cb251ec9384775a6c7cd9839`，与磁盘编译产物一致。尚未定位到导致完成的具体节点，不能把怀疑写成根因。

期间多次发生非本任务发起的 Play 退出及程序集重载，探针随后看到 Registration=null、Session=Uninitialized。已向用户询问是否手动停止或其他任务操作同一个 Editor；尚未收到回复。不得把连接恢复、空错误日志或 Play 请求接受当作运行状态保持的证据。

只读解码当前 Attack.FixedData 确认：Root 1 经 Child 边连接 StateMachine 0；Entry 2 连接 State 11；State 11 的 Root 119 经 Child 边连接 Timeline 121；Timeline 身份为 `10f4cb90-8b9a-4944-b77c-14efc9a3124d`。退出条件 235 由 StateRootCompleted 234 驱动。因此不能将问题直接归因于缺少图连线。

## 待完成

- 查明攻击实例同 Tick 完成的运行原因，确认前后冲执行及位移。
- 查明普通 Play 前段相机无法操作的原因，覆盖从启动开始的输入。
- 确认战斗动画、动作时钟及移动速度匹配。
- 集中修复后优先以已有 1492 帧录制核对；输入哈希一致不能替代动作和表现证据。
- 整理本任务尚未提交的资源映射、IK 生命周期和动画资源变更，保留其他任务修改；删除迁移后确认无引用的旧资源。

## 节点执行记录后的定位与修复

临时在 OperationControlRuntime 中加入预分配整数数组记录，只在显式启用时记节点进入/返回/边条件，不改执行语义。定位完成后该诊断已删除，git diff 确认 OperationControlRuntime 不再有本任务改动。

首次记录：Root 1 → StateMachine 0 → Entry 2 到 State 11 的条件结果 false，随后返回 Failure(3)。真正原因是入口连接了只有空 ConditionResult 的条件图（AuthoringId `5ee51b65f135f7809601ec732f478ef1`），未进入 Attack1；TimelineCompleted 是 ExecutionCompleted 结束规则覆盖失败结果所造成的误导。

已修改 Attack authoring 的 Root.cs / Attack1.cs：入口不绑定条件图，删除其生成代码。通过正式 `btsmtl.generate_assets` 保存成功，并执行正式 `Tools/3C/Internal/Republish Corin Ability Data`。生成时出现两条 NativeFormatImporter inconsistent result，仍需复查；旧空条件子资产也需确认清理，不能只删除源码。

入口修正后的执行记录确认进入 State 11、Root 119、Timeline 121，随后报 Control 状态端口无权访问 TimelinePlayback。已在 Fixed/Float32 正式 ControlPolicy 中加入 TimelinePlayback，并修正失败必须 Abort、不应用成功结束规则。提交 `731228f7f`。

再次短探针已越过状态访问检查，当前异常为 `Ability Timeline '10f4cb90-8b9a-4944-b77c-14efc9a3124d' failed to start for Action 'Attack'`。已在 CharacterTimelineHost.RequestAbilityTimelinePlayback 增加底层 Service.LastFailure 的异常信息，正在编译，尚未拿到具体拒绝原因。下一步读取该原因；不要重跑长录制。

随后在 Edit 模式通过相同正式 TimelineRuntimePreparation.Prepare、CharacterTimelineDomainBindingResolver、CharacterTimelineDependencyResolver 检查 Attack1，得到确定拒绝原因：CharacterTimelineDependencyResolver.TryResolve 是始终返回 false 的未完成实现。两条 `motion-curve` 依赖和五条 `tree` 依赖全部 unresolved。下一步应组合已编译技能图与位移曲线的依赖服务，不可将解析器改为无条件返回有效句柄。该检查没有启动 Play 或回放。

源码入口修正提交为 `a2bdb1c54`。之后另一任务正式导出更新了整个 CorinAttackGameplayAbilityAuthoringCode 目录，新增 Attack5_End / Attack5_End2 局部文件；必须保留最新导出，不覆盖回旧 Parts 编号。

依赖连接提交 `886070333`：CharacterTimelineHost 以技能 SourceMap 注册图版本，以实际有效曲线源注册曲线版本，按身份、类型、数值目标和内容哈希解析。一次发布前后原子检查确认 marker 图哈希稳定且编译哈希一致。随后 Attack1 正式 Prepare 返回 ready=true、errors=[]。

运行中 Attack1 已能启动但持续 Running，位置未变、动画采样时间仍为 0。查到分阶段 Advance/CommitAdvance/DiscardAdvance 未调用已存在的 TimelineRuntimeExecutionConsumer；只有一步式 Step 调用。已在正式 Service 分阶段 API 补齐 Consume/Commit/Discard，并由 AdvanceResult 保留原 AdvanceRequest，不额外分配上下文。

补齐后短探针进入 TreeClip Update，明确报 ObjectDisposedException(FixedSkillExecutionState)，堆栈指向 CharacterTimelineTreeClipService.Consume 使用旧 clip.Invoker。已删除跨 Tick 保存的 invoker，Update 使用 Host 当前帧 invoker；OnDestroy 从 CommitStop 移至 ConsumeStop，CommitStop 只移除记录，DiscardStop 保留记录。Fixed/Float32 的 Timeline Stop 同步压入/弹出当前 invoker。此轮正在编译，尚未运行复查，尚未提交这些回调修改。

仍需实现/核对：Timeline 运动结果进入角色 MotionAccumulator 的正式消费，目前 IAbilityTimelineTickResult 仅返回状态与 pending，CharacterTimelineHost 没有运动接收者；Fixed 的数值路径不得靠无说明的 Float 位移转换冒充已完成。TimelineRuntimeEvaluator 当前 MotionCurveTrack.Sample 产出 Unity Vector3/float，需要连同数值责任一起检查。

## 最近一次运行检查的阻塞

2026-09-20 01:50（本地时间）左右，生命周期修复已编译完并请求普通 Play。随后实例恢复注册为 `cb7bb3e0...`，但 execute_code、read_console、get_editor_state 均未取得响应，manage_editor stop 也在 30 秒后超时。官方 CLI instance list 仍列出 `3C_Client@e852139597e42532`；Unity 进程仍在。Editor.log 写入停在 01:48:44 的重载恢复注册处。不能确认攻击请求是否已执行，不能重发攻击冒充第一次，也未强杀或重启 Editor。恢复可读状态前不编辑脚本或刷新编译。

位移接入已核对可复用的数值实现为 `ThirdPersonSimulation.Fixed.FixedGameplayAbilityCurve`。应在角色准备时将引用的曲线键转换为定点曲线，按 Timeline Advance 的前后帧/循环区间求增量，再通过正式 Fixed 运动贡献入口提交；不要每帧创建曲线、数组或跨帧保留旧 invocation。该模块尚未实现。

## 后续 0 GC 交接线索

后续任务已计划按角色复用 Fixed/Float32 技能 invocation、workspace 和服务。当前能直接定位的分配点包括 FixedAbilityInvocationRuntime.Complete 的列表复制，以及 FixedCharacterInputTraceModule 的逐帧请求数组、SimulationInput 和诊断字符串。它们具有不同寿命，应在确认闭环后按正式所有者与消费边界处理；当前没有为整理工作新增接口或旁路。
