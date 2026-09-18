# Timeline 时钟域与 tick 率配置化（2026-09-17 决策）

本专题登记 Timeline 双时间维度调研决策，属本 change 的 Timeline 直接 Runtime 规划范围的延续，不另开 change。proposal 级决策对账见 tasks.md 顶部与本文件。

## Why

当前 Timeline 时间体系把三处"60"隐式焊死，tick 率实际不可配置，逻辑判定域与表现采样域职责无正式边界：

1. `TimelineUtility.FrameRate` 是 static 可变字段（默认 60），运行时评估、编辑器会话、内容闭包全部读它；
2. `FixedAbilityOperationControlRuntime.TickTimeline` 写死 `deltaFrames = 1`，即"1 个逻辑 tick 恒等于 1 个 timeline 帧"，tick 率偏离 60 时播放速度与 cue/TreeClip 判定时刻全部错位；
3. `GameplayAbilityAuthoringCompilationModel.TickRate` 直接返回 const 60，ability 编译产物时长换算与 pipeline 配置脱钩。

项目需要同时承载 Local、Prediction、Server Authority、Rollback 与 Replay 等 Network Model：逻辑域必须是固定 tick 整数域并按所选模型执行；表现域保持独立连续推进，默认消费表现 delta，再由角色表现域按 Network Model 选择自由播放、平滑纠正、硬切或严格跟随逻辑采样。`CommittedMovementPlaybackClock` 是逻辑时钟派生的 locomotion 进度事实，不是第三个时间域。tick 率从 const 收敛为 pipeline 正式配置（`CharacterPipelineDefinition.m_SimulationTickRate` 已有序列化口子；现行 `gameplay-tick-system` spec 已要求 tick 率来自正式配置），Timeline 域接入该配置。默认 60:60 下行为与现状一致，资产零迁移。

## 调研证据

### ZZZ 本体 dump（手感与内容格式参考）

`D:\ZZZ_Dump\output\anbi_replication\replication-guide`（MoleMole.Config，Unity 2019.4）：

- 转场条件以整数帧判定：`FrameCount [模式 9] 18`——`FrameCount` 是喂给 Animator 的自定义 int 参数，即自维护的 60Hz 逻辑帧计数器；
- 时间区域（Zone）与事件（battle/effect/audio）全部整数帧字段，并存归一化起止 `[0.0, 0.3]` 供采样；
- 秒只出现在参数级（如 `ShakeTotalTime 0.6`），时间轴本体无秒字段。

结论：ZZZ 本体是"逻辑 60Hz 离散帧域 + Animator 按渲染 delta 连续推进"的双时钟，60fps 渲染下两钟重合。参考价值是判定粒度（60Hz）与内容格式（帧字段 + 归一化双轨），不是时钟架构——ZZZ 单机锁帧无回滚需求。

### HoMiyabi DemoClient（反例参考）

`Ref\HoMiyabi\DemoClient`：`KiraraActionSO : TimelineAsset`（Unity Timeline 帧存储）、ActionExtractor 导出 JSON 秒 + 60fps root motion 采样数组、`ActionCtrl` 按 `deltaTime` 可变帧率秒推进，hitstop 走 `Animator.speed`；全程无 fixed tick、无回滚。其"60fps 采样数组"本质是帧域离散化，但不保证整数判定确定性。本项目是帧同步，不采纳其运行时架构。

### UE 5.4 源码（表现层机制参考）

`C:\Program Files\Epic Games\UE_5.4\Engine\Source\Runtime\Engine`：

- `Private\Animation\AnimInstance.cpp:479` `UpdateAnimation(float DeltaSeconds)` → `UpdateMontage` → `Montage_Advance(DeltaSeconds)`：动画位置按渲染 deltaTime 本地连续推进，与网络 tick 无关；
- `Classes\GameFramework\CharacterMovementComponent.h:2252-2306`：`SmoothCorrection` / `SmoothClientPosition_Interpolate` 位置修正指数平滑，`ClientAdjustPosition` 修正位置不回退动画时钟。

结论：UE 表现层 = "离散事件 + 本地连续时钟"，动画永不卡在网络 tick 格子上。UE 无回滚需求，因此敢用与逻辑无关的私有动画时钟。

## 方案对比与决策

### 方案 A：事件驱动插值（已选，用户拍板）

逻辑域每 tick 产出 committed 动画贡献（clip 活跃集、`ClipTime`、权重），表现层 `ActionPresentationSampleProjector` 在相邻 committed 采样点之间按 `presentationSampleTick` 线性插值出连续动画时间。

- 动画时间是逻辑时间轴的连续函数：回滚重放同 tick 流必得相同动画时间，可精确复算；
- root motion、foot phase、motion warp 等"逻辑消费动画时间"的场景零分裂；
- 线性插值使连续量无格子感，累加器 +0/+1 交替的采样步差同样被插值抹平；
- 离散事件（clip 切换、cue、TreeClip）量化到 1 tick 粒度（60Hz 时 16.7ms），与 UE notify、ZZZ 帧事件同粒度。

### 方案 B：表现独立连续时钟（UE Montage 字面形态，已否决）

逻辑只广播"clip X 开始、速度 v"，表现层自按渲染 delta 累加动画时间。手感绝对连续，但动画时间成为表现层私有状态，回滚后只能事件级重对齐、不能精确复算，与逻辑域 motion curve/foot window 依赖动画时间形成双钟脑裂，误差随修正累积。UE 能承受是因为不做回滚；帧同步项目不可接受。

### "表现 clip 卡在 tick 格子上"疑虑澄清

不成立。表现层从不直接消费 tick 采样点——committed 贡献只是插值端点，插值输出连续。"落在格子上"的只有离散事件触发时刻，这是帧同步判定域的必然，也是 ZZZ/UE 共同形态。编辑器允许亚 tick 拖拽只会造成"显示位置与运行判定位置不一致"的所见非所得，因此吸附粒度定为 `1/tickRate`。

## 焊点修正设计

1. **`TimelineUtility.FrameRate` static 删除**。运行时换算率来自 pipeline tick 率配置，随 Timeline 准备上下文注入（Prepare/Content 闭包携带）；编辑器 Slate 会话保留自己的预览刻度（默认 60），两者不再共享可变全局。
2. **推进换算与累加器**。`TickTimeline` 不再写死 `deltaFrames = 1`；每 tick 推进的 timeline 帧数 = `tickRate : timelineFrameRate` 比率的整数累加（余数保留）。timeline 帧率默认与 tick 率一致（60:60 恒等推进，行为与现状零差异）。累加器余数是确定性整数状态，进入 `TimelineRuntimePreparation` 播放快照（Capture/Restore），保证回滚重放一致。
3. **编译层 tick 率**。`GameplayAbilityAuthoringCompilationModel.TickRate` 改读 `CharacterPipelineDefinition.SimulationTickRate`，编译产物携带；`SimulationSessionHost` 既有的 tick 率一致性校验与网络协议握手 TickRate 校验保持为最终防线。
4. **表现样本历史审计**。`ActionPresentationSampleProjector.Interpolate` 已有 `presentationSampleTick <= previous.LocalLogicTick` 不倒退保护；审计 `ActionCommittedSampleHistory` 在回滚重放产生 tick 倒退样本流时的替换/清理路径，保证修正瞬间表现平滑接管（视觉纠正遵循现行 `character-presentation-interpolation` 条款），缺口按其整批更新方式补齐。
5. **编辑器吸附**。`BtsmtlSlateTimelineBinding.SnapTime` 量化粒度从硬编码帧率改为会话 tick 步长（`1/tickRate`）；帧存储与 Slate 时间轴形态不变，60:60 时零行为差异。
6. **`TimelineData.m_Scale` 删除**。调研确认运行时评估从不消费（仅残留于 `TimelineContentClosure` 指纹传递），按激进清理原则删除字段、属性与闭包传递链。

## 与现行 spec / 其他文档关系

- `gameplay-tick-system`（tick 率配置化、表现插值 alpha）与本决策一致，不修改其文档，Timeline 是其下游消费者；
- `character-presentation-interpolation`（表现不回滚、连续状态保持、分支替换整批更新）是本 change 的表现侧约束；Network Model 级 locomotion plan 由独立的 `add-network-model-locomotion-presentation-policy` change 负责；
- `btsmtl-timeline-direct-runtime` 需要补充“直接内容遍历下的双域 evaluation”边界：Logic / Presentation 输出不是 Semantic operation 或预生成操作表；
- `btsmtl-runnable-timeline-node` 需要把既有 TimelineBody 图限定为 Logic TreeClip，并规定 Marker 为与 Clip 同级的点触发实体；
- `character-animation-pipeline` 需要记录表现 Marker 在 PresentationFrame 消费、不得写 Gameplay fact 的调用边界。

## 与归档条款的协调

归档后的 `btsmtl-timeline-editor-preview` 现行 spec 中"Timeline必须使用正式帧率统一编辑时间"条款声明"作者帧 MUST 不自动解释为 Runtime Logic Tick"。本 change 与其兼容：作者帧是编辑器作者域单位，吸附步长配置化（默认取 `1/tickRate`）不改变"帧经显式换算进运行时"的语义；"编辑器预览刻度 MAY 与运行时 tick 率独立配置"即该条款的延续。

## 表现时钟策略模式设计（2026-09-18 修订）

运行时只有两个时间域：逻辑时钟与表现时钟。`CommittedMovementPlaybackClock` 不是第三个时钟，而是逻辑时钟产生的 locomotion 进度事实，供表现层在需要时采样。

表现层时间驱动以策略对象插件化：`IActionPresentationClockPolicy` 的 `DriveClock(player, channelId, presentationSampleTick, factFrame, presentationDeltaSeconds)` 完整封装播放器每帧如何推进，播放器消费点只调用策略，不判断网络模型。

- `FreeRunPresentationClockPolicy`：`player.Advance(deltaSeconds, PlayRate)`，作为普通表现的默认模式；
- `CommittedMovementPresentationClockPolicy`：读取 `CharacterPresentationFactFrame` 中的逻辑 locomotion 进度，供明确要求逻辑跟随的表现域使用；
- `CommittedFollowPresentationClockPolicy`：组合 Registry、History 与 Projector，对带 channel 的 committed Action 采样做连续投影，供回放、观战或严格动作域使用；
- 消费点：`CharacterPoseNativeClipPlayerHandler.PrepareFrame` 只调用 `m_ClockPolicy.DriveClock(...)`；
- 装配边界：本 Timeline change 只在表现播放器层提供 FreeRun、CommittedMovement 与 CommittedFollow 策略；Network Model 级 locomotion 表现策略由独立 locomotion change 装配，避免把 Timeline 时钟改造和 locomotion 网络装配混成一条变更。

取舍：通用表现默认按表现时钟自由播放；业务需要严格重演时再装配逻辑跟随。这样不强迫普通表现承担确定性采样成本，也保留回放、回滚与关键动作接入严格逻辑采样的入口。

## Timeline 执行域拆分（2026-09-18 新增）

Timeline 不能把所有 Track / Clip 都绑定到同一个推进路径。运行时只有 Logic Tick 与 Presentation Frame 两个时间域；内容执行域为 `Logic`、`Presentation` 或 `DualProjection`：

```text
Logic
    SimulationTick 推进
    负责 Gameplay 判断、TreeClip 决策、Cue、Window、Action 状态
    可进入 Commit / Discard / Rollback

Presentation
    PresentationFrame 推进
    负责动画、特效、音效、相机和表现事件
    不生成 Gameplay fact，不修改 SimulationState

DualProjection
    同一作者内容同时提供 Logic 与 Presentation 投影
    不是第三个时钟，不把同一张 TimelineBody 图在两个时钟各执行一次
```

Track 合同持有默认执行域，Clip 只能在 Track 允许的范围内声明有效域；历史资产缺少该字段时固定按 `Logic` 解释。Clip 不保存 ModelId、Endpoint、Transport、Rollback 或具体时钟实现。`DualProjection` 只表示内容拥有两种输出，不意味着额外时钟或额外播放实例。

Timeline Runtime 仍直接读取正式只读 TimelineData。`Advance` 与 `Present` 分别在当前推进中遍历所需内容、形成 Logic Evaluation 或 Presentation Evaluation；两者是当前调用的结果分区，MUST NOT 被实现成预编译 Semantic operation、常驻操作表或第二份 Timeline 内容。

TreeClip 必须把图执行和表现触发拆成两个明确来源：

- Logic TreeClip：既有 `AssetTree` / TimelineBody 图只在逻辑 Tick 评估 Enter / Update / Exit，输出逻辑请求，进入既有 Commit / Discard 链；`TreeDecision` 退出也只作用于这一侧；
- Presentation TreeClip：由 PresentationFrame 游标推进的表现安全 TreeClip，只产出表现结果，不得绑定或执行 Logic TimelineBody 图，不得产生 Gameplay fact；
- Marker：与 Clip 同级的点触发实体——单帧、稳定 MarkerId、触发图仅暴露 OnEnable 回调；域归属决定由 SimulationTick（Advance / Commit）或 PresentationFrame 推进，不同域独立推进互不等待；
- DualProjection TreeClip：同一 Clip 同时持有 Logic `AssetTree` 与 Presentation Marker。两侧由同一 Clip identity 关联，但 AssetTree 只执行一次，永远不在 PresentationFrame 执行。

每个 Presentation Marker 事件的稳定身份由 `PlaybackHandle`、`Generation`、`MarkerId` 与 `TraversalIndex` 组成；`TraversalIndex` 是该 playback generation 下穿过 Marker 的循环/经过序号。Marker 是单帧点触发：同一次经过只交付一次，循环再次经过获得新的 `TraversalIndex`；playback 停止或 generation 变化后旧 generation 的 Marker 不再触发。触发只改变表现状态，不能写 Gameplay fact。

2026-09-19 用户定案：Marker 重构为与 Clip 同级的点触发实体，废除 `Pulse`/`Stateful` 区间模型与 clip 子列表方案；Marker 触发图仅 OnEnable 回调，域归属（Logic tick / Presentation frame）决定推进者，不同领域各自独立实现。

当前落地状态：Track 已直接持有 Marker 列表；闭包/指纹包含 Marker 与触发图；旧 Pulse / Stateful / Clip 子列表实现已删除；Presentation 事件已使用 `handle:generation:markerId:traversalIndex`；Slate 轨道已支持 Marker 创建、绘制、拖拽、选中与触发图编辑。Marker 触发图已进入 Ability Timeline 编译闭包：Marker 图单独建立 MarkerTrees，禁止 Disable / Destroy / 结束片段节点，SourceMap 只声明 OnEnable。Logic 域 Marker 在 Advance Evaluation 中形成独立请求，经 ExecutionConsumer 消费并参与 Commit / Discard；Presentation 域 Marker 继续由视觉游标产生稳定事件，CharacterTimelineHost 在广播 PresentationFrame 前执行 OnEnable。Presentation 输出的下游边界已经固定：Marker 的 `handle:generation:markerId:traversalIndex` 补齐为正式 `EventId`；表现动画继续经既有 `ActionPlaybackCommandInbox`；Timeline Camera 的 State / Cue / Response / Resource 采样在 `CharacterPresentationDomainRuntime` 装配的 Camera bridge 中生成激活与退役命令，交给既有 Camera domain 调和。激活身份来自 Ability Timeline invocation 的正式 SkillOperation source、invocation generation、Action instance 与当前 Logic tick，不用 playback handle 伪造 ActivationId。

特效和音效当前没有正式下游 domain，因此不得通过 `Vfx`、`Ui` 或 payload 字符串伪造命令，也不得新建第二套事件系统；这些领域以后必须以同样的 PresentationFrame 事件和稳定 EventId 接入正式 domain。现有逻辑 TreeClip runtime 不得被宣称为已经支持表现时钟 TreeClip；仅输出 Presentation Marker 事件也不等价于已完成触发图消费。

## Timeline 编辑器 MVC 与攻击帧消费边界（2026-09-19）

Timeline 顶栏已拆成正式 MVC 边界：`TimelineEditorBindingState` 只保存当前 Timeline binding 的只读投影；`TimelineEditorToolbarView` 只构造文档导航、Workspace 模式和运行状态三个按钮组；`TimelineEditorWindow` 继续作为 controller 拥有绑定、undo、Slate projection 和观察事件。不得把按钮回调、资产绑定或 runtime fact 混进 View。

Corin dump 中的 `AttackProperty` 不是 Timeline 的直接数据格式。主控负责把它转换成 Timeline Marker 或 Ability 打击帧；Timeline runtime 只消费这些正式 Marker / TreeClip / Ability 事件，Timeline editor 只提供这些内容的作者、跳转和只读观察。禁止在 Timeline 内新建 AttackProperty 解析器、私有时钟或第二运行链。

## Corin Attack Timeline ActionCue 合同（2026-09-19）

`ActionCueTrack` 的唯一 runtime 语义是发布 committed Logic domain event：事件名是作者配置的 `CueType`（Corin 攻击使用 `AttackProperty`），业务键是 `CueId`。`CharacterTimelineHost.ActionCueCommitted` 负责在 SimulationTick commit 后发布 `TimelineActionCueEvent`；payload 包含稳定 `EventId`、playback handle、generation、`LogicTick`、frame/cycle、execution identity、content revision、source/track/clip authoring id、`EventName` 和 `CueId`。Timeline 不解析 `AttackProperty`，不决定 Camera/VFX/Audio 命令，也不在 Track 内写领域状态。

消费边界：`AttackProperty` 由 Ability/Attack 领域订阅并解释；Camera 请求继续由 Camera/TreeClip 节点合同拥有；VFX/Audio 必须由各自正式 domain 订阅或显式失败，Timeline 不代发伪命令。没有领域订阅时，事件只能保持为已提交事实和 trace，不能宣称已消费。触发时钟只有 Logic SimulationTick；不得改由 PresentationFrame 重发。

2026-09-19 主控复验：`9fe8ea317` 已把 Corin AttackProperty 扩展到全量 `108` 个正式 GameplayEffect key，命中效果编号按 `uint` 收口；同 Trace 1121 帧逐帧 0 分歧。Timeline 侧确认：ActionCue 只发布稳定领域事件，`CueId` 保持原始 `Corin_Attack_*_AttackProperty_*` key；命中效果编号、碰撞形状和属性 payload 不进入 Timeline runtime。旧 TreeDesigner Timeline UI 保持删除，FlowCanvas 只承担 TreeClip / Marker 图可视化。

## Normal Attack End / Explode 复核（2026-09-19）

依据 `docs/zzz-corin-controller-map.md` 复核后，现有五段简化 Timeline 不升级为完整 Normal 状态机：Attack 1 / 2 / 4 可继续用线性 main + End 覆盖主起止和主段 cue；Attack 3 Explode 与 Attack 5 End / End_2 不可用当前 Timeline 表达。具名缺口是 `GAP-Normal3-ExplodeStateSegment`、`GAP-Normal3-ExplodeCueRemap`、`GAP-Normal5-EndBranchSelection`、`GAP-Normal5-End2TimelineBinding` 和 `GAP-NormalEndStateBoundary`。Branch / Rush 不混入现有 Attack Timeline。

`Attack_Normal_05_End_2` 不是缺 Clip：状态实际绑定 `Attack_Normal_05_B`，缺的是正式 Timeline 段、状态边界和分支选择。AttackProperty payload 继续全量留在 GameplayEffect Profile / Ability 执行域；Timeline 只发布原始 `CueId` 和播放身份。

## Attack3 Explode 状态分段收口（2026-09-19）

`Attack_Normal_03_Explode` 使用 `CorinAttack3Timeline` 内的 `TimelineSection` 承载，不改成主段普通全局 cue。Section 从 frame=75 开始，`Corin_Attack_Normal_03_AttackProperty_02` 的源本地 frame=1 映射到全局 frame=75。`TimelineActionCueSample` 和 committed `TimelineActionCueEvent` 现在携带 `StateId` 与 `LocalFrame`；ActionCue 采样按 cue 所属 Section 计算本地帧。这样消费端可以用状态身份调和，不能把 frame=75 解释成主段自己的第 75 帧。

本段只收口 Attack3 Explode cue。Attack5 frame=47 的 End / End_2 分支选择、End_2 正式 Timeline 绑定和 15 个本地 cue 仍保留在后续任务。

## Normal Attack 状态本地 cue 收口（进行中）

`docs/zzz-corin-normal-attack-cue-map.md` 已给出精确差量：Attack3 主段 20 个 cue 一致；Explode 缺 frame=1 的 `Corin_Attack_Normal_03_AttackProperty_02`。Attack5 主段源 15 个 cue，当前 16 个；多余 frame=64 的 `_01_02` 必须删除。`Attack_Normal_05_End` 没有 cue；`Attack_Normal_05_End_2` 缺 15 个状态本地 cue，帧号为 1、10、12、14、16、18、20、22、24、26、28、30、32、34、36。

正式收口不得把状态本地 cue 继续压平到当前五段全局轴：Attack3 Explode 需要 `Attack_Normal_03_Explode` 独立 Timeline / 状态分段；Attack5 需要在 frame=47 分离 End 与 End_2 分支，End_2 使用正式 Timeline 绑定并承载 15 个本地 cue。payload 仍只带 `CueId` 与播放身份，属性和碰撞语义留在 GameplayEffect / Ability 执行域。
