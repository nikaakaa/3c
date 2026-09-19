# Tasks

2026-09-19 状态修正：保留已完成的代码成果，撤销“1–8 勾完即 Timeline 已闭环”的结论。勾选只表示所述范围的实现；共享表现采样、表现图执行、停止与作者入口仍有待办。本清单不含测试或手动验证任务，本轮只改文档。

## 1. 作者帧基准与逻辑换算

- [x] 1.1 将 TimelineUtility.FrameRate 固定为 60 作者帧基准，删除可变全局帧率依赖
- [x] 1.2 编辑器会话、TimelineContentClosure 与内容派生换算使用明确的 FrameRate，不再共享可变全局配置
- [x] 1.3 将正式 tick 增量在 Timeline runtime 内按 tick 率 / 作者帧率进行整数比例换算，不再把一次逻辑 tick 等同于一个作者帧
- [x] 1.4 将换算余数纳入 TimelineRuntimePreparation 的 Capture / Restore，保存确定性逻辑播放私有状态
- [x] 1.5 GameplayAbilityAuthoringCompilationModel 读取 CharacterPipelineDefinition.SimulationTickRate，并沿用 session / protocol 的 tick 率一致性约束
- [x] 1.6 删除 TimelineData.m_Scale、Scale 属性及 TimelineContentClosure 指纹传递链

## 2. 已有 Pose 表现策略

- [x] 2.1 建立 FreeRun、CommittedMovement、CommittedFollow 策略，CommittedMovementPlaybackClock 保持为逻辑派生事实
- [x] 2.2 保留 ActionCommittedSampleHistory、ActionPresentationSampleProjector 与 ActionAnimationPlaybackLifecycleRegistry 的 committed Action 投影链
- [x] 2.3 CharacterPoseNativeClipPlayerHandler 通过 IActionPresentationClockPolicy.DriveClock 消费表现策略

以上只登记当前 Player 策略能力，不代表同一动作的 Timeline Marker / Camera 已消费同一时间结果。locomotion 装配服从现行 plan / prepared binding 合同，不把旧单 Clip ClockSource 配置作为继续实现目标。

## 3. 已有执行域与 Marker 作者模型

- [x] 3.1 Track / Clip 增加 Logic、Presentation、DualProjection，Runtime 按当前 Advance / Present 直接遍历同一正式内容
- [x] 3.2 建立 PresentationFrame evaluation 和表现游标入口；其独立累加实现由第 5 节迁移，不视为最终时钟合同
- [x] 3.3 Marker 成为 Track 持有的同级点实体，进入闭包与指纹，删除 Clip 子列表与 Pulse / Stateful 区间模型
- [x] 3.4 TimelineBody AssetTree 保留 Logic 执行路径，PresentationFrame 不重复执行该 Logic 图
- [x] 3.5 建立 PlaybackHandle / Generation / MarkerId / TraversalIndex 事件身份字段；停止与修正行为由第 7 节补齐
- [x] 3.6 建立 TimelineTrigger 图角色、仅 OnEnable 入口、MarkerTrees 编译闭包与 Logic Marker 的 Advance / Commit 调用链
- [x] 3.7 Slate 接入 Marker 创建、绘制、选中、拖动、图编辑与正式 Undo；修正拖动坐标重复扣除左栏导致落到第 0 帧的问题
- [x] 3.8 C# authoring 增加 EnsureMarker / PruneTimelineMarkers 及 Marker 字段导出；完整私有图重建边界由第 8 节补齐
- [x] 3.9 Camera State / Cue / Response / Resource 通过既有 Camera bridge 交给正式 domain，动画继续使用 ActionPlaybackCommandInbox

## 4. 已有编辑器与内容合同

- [x] 4.1 Timeline 顶栏拆为 TimelineEditorBindingState、TimelineEditorToolbarView 与 TimelineEditorWindow，保留 Slate 单一编辑入口
- [x] 4.2 AttackProperty 由 Ability / Attack 领域转换和消费，Timeline 不引入原始 dump 解析器或第二运行链
- [x] 4.3 ActionCue 只在 Logic commit 发布 CueType / CueId 与正式事件身份，不代行 Camera / VFX / Audio 领域逻辑
- [x] 4.4 同步 Corin AttackProperty 效果 key 与 uint 编号合同，payload 留在 GameplayEffect / Ability，保留旧 TreeDesigner Timeline UI 删除结果
- [x] 4.5 登记 Normal Attack End / Explode 内容边界，Branch / Rush 不并入现有五段 Timeline
- [x] 4.6 Attack3 在 frame=75 建 Attack_Normal_03_Explode Section，本地 frame=1 cue 映射为全局 frame=75
- [x] 4.7 Attack5 在 frame=47 建 Attack5EndBoundary 及 End / End_2 正式状态转移
- [x] 4.8 建立 CorinAttack5EndTimeline / CorinAttack5End2Timeline，End_2 携带 BranchId 与 15 个状态本地 cue
- [x] 4.9 删除 Attack5 frame=64 多余 _01_02 cue 并重建对应 Ability 定义
- [x] 4.10 StateId / LocalFrame / BranchId 进入 ActionCue sample、committed event 与稳定 EventId

## 5. 同一动作的共享表现采样

- [ ] 5.1 沿既有 Action 表现时钟策略拆出进度计算结果，输入正式 playback identity、committed controls / samples 和表现 delta，输出前后位置、循环经过、变化原因与事件资格
- [ ] 5.2 在既有播放 owner 中按动作实例 / generation 每表现帧计算一次采样，移除 Timeline driver 与动作 Player 对同一动作的独立累加，不增加第二 Registry 或同义时钟接口
- [ ] 5.3 将动作动画、同 playback 的 Timeline Marker 与 Camera 采样接入该结果，Clip 源采样继续经过起点 / ClipIn / 速率映射，locomotion 与混合过渡保持原 owner
- [ ] 5.4 在正式 composition / prepared binding 中接入策略与控制输入，保留有限 Action 的 committed sample 合同，不按游戏类型或 Network Model 在消费者内分支，不提供缺配置后的自由播 fallback
- [ ] 5.5 让当前合法策略统一接收暂停、速率与终态控制，缺少所需 samples / binding 时报告对应正式失败或合同规定的保持状态，不擅自外推

自由推进、跟随和有界追赶的业务 tradeoff 已列入 design.md。本次不增加尚无使用方与正式参数定义的追赶策略，也不把有限 Action 改为自由播。

## 6. Presentation Marker 的正式图执行

- [ ] 6.1 沿现有图 compiler / preparation 增加有效域能力约束，拒绝 Presentation 图中的 Gameplay 写入、TreeDecision、结束片段及不支持的节点 / 资源
- [ ] 6.2 在正式图服务边界绑定表现执行上下文，消费精确图 identity / revision、只读表现事实和 typed 输出能力，删除 CharacterTimelineHost.Present 对临时 m_ActiveTreeClipInvoker 的依赖
- [ ] 6.3 将 OnEnable 产生的表现候选与事件记账接入原表现帧接受 / 丢弃边界，不调用 Simulation Evaluate / Finalize，不新建影子图 runtime 或私有 Simulation context
- [ ] 6.4 缺少正式下游 domain 或图执行能力时在准备 / 调用边界明确失败，Camera / 动画继续走原领域输出，不以空实现或 payload 字符串宣称已消费

## 7. 停止、分支修正与事件生命周期

- [ ] 7.1 接通正式 Stop / Cancel 接受结果与 Presentation 状态，停止后禁止旧 playback / generation 推进并产生新 Marker，清理失效采样缓存
- [ ] 7.2 将最终 branch revision / reset 交给既有表现 owner，区分正常推进、Seek 与修正采样，不将逻辑 Restore 的中间状态写回表现游标
- [ ] 7.3 将事件去重与表现帧一起提交，重复采样或回退修正不制造新 TraversalIndex，正常循环可再次触发，Discard 不消耗尚未交付事件
- [ ] 7.4 按既有领域生命周期退役旧 Camera / 动画请求，保留已生成表现的正式收尾策略；预测分支撤销不合成 confirmed Complete / Release

## 8. Domain 与作者数据一致性

- [ ] 8.1 在现有 Track Inspector 增加 Domain 编辑、Marker 继承域显示及不兼容内容定位，复用原 Timeline mutation / Undo，一次失败不留下部分域变更
- [ ] 8.2 域修改同步处理 Track、显式 Clip 域与 Marker 图能力，闭包和编译使用一致声明；缺少 DualProjection 合法投影时明确失败，不把同一 Logic 图执行两次
- [ ] 8.3 统一 Slate 拖动反馈、数值输入、Snap 与 CommitSource 的整数作者帧量化，标明作者帧 / 秒换算，移除“Presentation 可保存亚帧”及“作者帧等于 tick”的误导
- [ ] 8.4 在现有时间观察入口显示所用 SimulationTickRate 与作者位置对应的实际逻辑生效 tick，不为观察新增预览时钟或第二求值器
- [ ] 8.5 补齐 Marker 私有图在正式 C# export_code / generate_assets 中的 owner 闭包，复制 / 重建保留图角色、节点内容与引用，不以旧资产路径 / localFileId 或空图代替完整重建
