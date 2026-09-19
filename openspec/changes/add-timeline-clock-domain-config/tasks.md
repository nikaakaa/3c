# Tasks

本次更新：用户已确定秒制作者时间和 Timeline 被动求值。播放管理者负责进度与倍率，Timeline 接收区间和原因处理内容；编辑器 tick 吸附跟随正式配置。第 1–4 节仅记录已有实现，不代表旧时间所有权继续作为目标；停止、修正和变速随共享采样一起接入。本清单不含测试或手动验证任务，本轮只改文档。

## 0. 秒制作者时间与全链路迁移

- [ ] 0.1 根据现有数值类型、素材帧率和业务时间范围，明确唯一秒制表示、精度、舍入、边界比较与速率换算规则，补齐设计中的数值决策
- [ ] 0.2 将 Timeline 起点、时长、Marker、Section、ClipIn、循环边界和 Timeline 自有时间坐标迁移为秒，更新正式字段名与作者 API，删除整数作者帧双写和兼容读取
- [ ] 0.3 将旧资产和生成 authoring 代码按正式旧时间 / 素材映射一次性迁移，保留内容身份和引用；同步闭包、指纹及正式派生产品格式，不生成 tick 版作者内容
- [ ] 0.4 将精确动作进度、倍率、暂停与换算余数归入既有逻辑播放管理者，Timeline 接收前后秒数／经过／原因被动遍历；播放状态与求值状态在同一 Step 和 Capture / Restore 链提交恢复，保留循环、Decision 与边界截停规则
- [ ] 0.5 将正式事件位置和运行消费者迁移为秒，ActionCue 保留 LogicTick、cycle 和素材来源身份；原 LocalFrame 不再参与第二套时间推进
- [ ] 0.6 迁移 Slate、Inspector、Session、mutation / Undo 和 C# 导出重建的时间读写，帧只作显示与可选吸附，删除 StartFrame 等旧正式存储入口
- [ ] 0.7 将逻辑和表现 Timeline 调用入口统一为被动区间求值，删除自主 delta 累加；沿既有预分配存储传递推进与候选结果，运行热路径保持 0 GC

以上是目标实现，尚未执行。调整运行 tick 率不改作者秒数，不承诺不同 tick 率下碰撞与输入结果完全相同；短窗口的业务区间消费仍属于原战斗领域。

## 1. 已有作者帧基准与逻辑换算（由第 0 节迁移）

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

- [ ] 5.1 沿既有 Action 表现时钟策略拆出进度计算结果，输入正式 playback identity、committed controls / samples 和表现 delta，输出前后动作秒数、循环经过、变化原因与事件资格；同时接入第 7 节的停止和修正控制
- [ ] 5.2 沿现有调用链明确共享采样的具体持有对象和帧内调用顺序，按动作实例 / generation 每表现帧计算一次采样，移除 Timeline driver 与动作 Player 对同一动作的独立累加，不增加第二 Registry 或同义时钟接口
- [ ] 5.3 将动作动画、同 playback 的 Timeline Marker 与 Camera 采样接入该结果，Clip 源采样继续经过起点 / ClipIn / 速率映射，locomotion 与混合过渡保持原 owner
- [ ] 5.4 在正式 composition / prepared binding 中接入策略与控制输入，保留有限 Action 的 committed sample 合同，不按游戏类型或 Network Model 在消费者内分支，不提供缺配置后的自由播 fallback
- [ ] 5.5 让当前合法策略统一接收暂停、速率与终态控制，缺少所需 samples / binding 时报告对应正式失败或合同规定的保持状态，不擅自外推
- [ ] 5.6 在既有正式播放控制中明确子弹时间／hitstop 的作用范围、倍率叠加、生效 Step 和解除来源；控制参与确定性状态，禁止写 Unity 全局时间或修改 tick 率，跟随样本的表现不重复应用倍率
- [ ] 5.7 区分动作进度倍率和 Clip 源采样倍率：前者影响动作窗口与共享采样，后者只改变素材映射；不为 Timeline 另建时间控制服务

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
- [ ] 8.3 在秒制模型上统一拖动反馈、秒输入、逻辑 tick／素材帧／关闭吸附与 CommitSource；逻辑网格自动读取绑定 pipeline 的 SimulationTickRate，展示来源，缺绑定时不可用；配置变化不移动已有内容
- [ ] 8.4 在现有时间观察入口显示作者秒数、SimulationTickRate 与当前播放控制下的实际逻辑生效 tick，标明静态换算的速率 / 暂停前提，不新增预览时钟或第二求值器
- [ ] 8.5 补齐 Marker 私有图在正式 C# export_code / generate_assets 中的 owner 闭包，复制 / 重建保留图角色、节点内容与引用，不以旧资产路径 / localFileId 或空图代替完整重建
- [ ] 8.6 将“按当前网格重新对齐”作为显式作者操作接入原 mutation，仅处理选中范围并支持一次完整 Undo，统一网格到正式秒制精度的舍入规则
