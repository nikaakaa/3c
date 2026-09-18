# Timeline 时钟域与 tick 率配置化（2026-09-17 决策）

本专题登记 Timeline 双时间维度调研决策，属本 change 的 Timeline 直接 Runtime 规划范围的延续，不另开 change。proposal 级决策对账见 tasks.md 顶部与本文件。

## Why

当前 Timeline 时间体系把三处"60"隐式焊死，tick 率实际不可配置，逻辑判定域与表现采样域职责无正式边界：

1. `TimelineUtility.FrameRate` 是 static 可变字段（默认 60），运行时评估、编辑器会话、内容闭包全部读它；
2. `FixedAbilityOperationControlRuntime.TickTimeline` 写死 `deltaFrames = 1`，即"1 个逻辑 tick 恒等于 1 个 timeline 帧"，tick 率偏离 60 时播放速度与 cue/TreeClip 判定时刻全部错位；
3. `GameplayAbilityAuthoringCompilationModel.TickRate` 直接返回 const 60，ability 编译产物时长换算与 pipeline 配置脱钩。

项目是帧同步（确定性回滚）项目：逻辑域必须是固定 tick 整数域且可回滚；表现层确认不回滚，只消费 committed 事件流并连续插值。tick 率从 const 收敛为 pipeline 正式配置（`CharacterPipelineDefinition.m_SimulationTickRate` 已有序列化口子；现行 `gameplay-tick-system` spec 已要求 tick 率来自正式配置），Timeline 域接入该配置。默认 60:60 下行为与现状一致，资产零迁移。

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
- `character-presentation-interpolation`（表现不回滚、连续状态保持、分支替换整批更新）与本决策一致，Timeline 动画贡献遵守它；
- 本 change 既有 delta `btsmtl-timeline-direct-runtime` 的"Timeline 唯一时间 owner 负责帧/秒/Tick"措辞由本专题新增 capability `btsmtl-timeline-clock-domain` 细化为"tick 权威 + 秒读数"，无冲突；
- `btsmtl-runnable-timeline-node` 的播放隔离与停止语义不受影响。

## 与归档条款的协调

归档后的 `btsmtl-timeline-editor-preview` 现行 spec 中"Timeline必须使用正式帧率统一编辑时间"条款声明"作者帧 MUST 不自动解释为 Runtime Logic Tick"。本 change 与其兼容：作者帧是编辑器作者域单位，吸附步长配置化（默认取 `1/tickRate`）不改变"帧经显式换算进运行时"的语义；"编辑器预览刻度 MAY 与运行时 tick 率独立配置"即该条款的延续。

## 表现时钟策略模式设计（2026-09-17 定稿）

表现层时间驱动以策略对象插件化：新增 `IActionPresentationClockPolicy` 合同（`DriveClock(player, channelId, presentationSampleTick, factFrame, presentationDeltaSeconds)`），把"这一帧播放器时钟怎么走"完整封装在策略实现里，播放器消费点一行调度、零模式分支。

- `FreeRunPresentationClockPolicy`：`player.Advance(deltaSeconds, PlayRate)`——表现独立连续推进；
- `CommittedMovementPresentationClockPolicy`：从正式 `CharacterPresentationFactFrame.MovementPlaybackClock` 读取已提交 locomotion 时钟，调用 `SynchronizeMovementClock`，用于作者明确标记为 `CommittedMovement` 的 locomotion Clip；
- `CommittedFollowPresentationClockPolicy`：组合 `Registry`（Select/Sample/Complete/Release 生命周期、channel→playback 映射）+ `History`（committed 采样序列）+ `Projector`（窗口插值），`TryProject` 成功时 `SetRawClock(插值连续时间)`，窗口缺失时回到该 Clip 的自由推进语义；内部分支属于策略自身语义，消费点不可见；
- 消费点：`CharacterPoseNativeClipPlayerHandler.PrepareFrame` 的 `m_Player.Advance(...)` 替换为 `m_ClockPolicy.DriveClock(...)`；
- 装配开关：`CharacterPresentationDomainRuntimeFactory` 按 Clip 的 `CharacterClipPlayerClockSource` 选择 FreeRun 或 CommittedMovement；SimulatedActor 为 channel-bound Action 额外提供 CommittedFollow coordinator。

取舍：普通 locomotion 需要跟随已提交移动时钟，否则起步、循环和停止会脱离正式 movement playback；纯表现 Clip 继续使用 FreeRun；CommittedFollow 只为需要按 committed Action sample 重演的回放/观战链实例化。
