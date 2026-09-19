# 实施记录

## 工作位置

- Goal：按本 change 完整实施，尚未完成。
- 唯一实施分支与目录：`main`，`D:/Unity_Project_1/3C`。按用户明确要求，后续不创建或使用其它 worktree。
- 当前 Center change_id：`fa4c33f9eb594240994f011afd2996b6`，绑定主目录。
- 本次误用的 worktree 成果已通过 `802abd8c5` 全量迁回主线，并归并原提交历史；主目录原有未提交改动保持。
- 原 Center 记录 `096ec1cf6812476495c366ae33bb9236` 和其 Run 只作历史证据，不再用于继续实施或主线验收。

## 工作位置纠正

- 原分支 `codex/timeline-seconds-passive` 从 `cdb06993e` 建立，形成 `20a04cb79`、`bce79a177`、`ffbcc4eff` 三个提交，共 11 个文件。三个提交的全部增量与历史均已归入主线，不保留独立实现路径。
- 历史归并提交为 `43550907e`；原 worktree 目录与 `codex/timeline-seconds-passive` 分支已删除。
- 迁入时分别向工作区和索引应用本任务增量；作者适配器原有执行域导出修改仍为主目录原先的未提交内容，未被覆盖或混入本任务提交。
- 原 worktree 额外的 package lock 差异和自动生成的 Timelines.meta 来自冷导入；主目录已有正式包与目录身份，因此不迁入这两项自动变化。
- 撤销“必须等待 locomotion 任务另行提交后才能继续”的当前阻塞判断：其消费者迁移本来就在主目录。此前失败反映错误 worktree 的不完整基线，不代表主线缺少该批源码。后续基于主目录实际状态继续，不恢复已删除旧类型。
- 所有历史编译结果都不能证明迁回后的主线通过编译、运行或完整功能验收。任务仍按实际完成范围勾选。

## 已完成与进行中

- 0.1 已完成：核对 `Simulation/Core/Fixed/Numerics/SimulationNumerics.cs`，选择与现有数值合同一致的 Q32.32 秒、nearest-even 输入舍入和累计余数规则，已写回 design。未把数值决定当作字段／资产迁移完成。
- 7.1 局部修复：原 Presentation driver 在 CommitStop 后只写 StopCommitted，后续 TryPresent 未读取；现于采样前退出并移除表现状态，禁止已接受停止的旧实例继续产生 Marker。完整终态／修正接线仍未完成，任务不勾选。
- 现有运行链含逐帧 List／结果对象分配，尚未完成 0 GC 迁移，不声称性能条件已满足。

## 原 worktree 修改前编译（历史）

- Run：`76b1360448ec47ed9a59744706edfe9d`，正式 RunHost 2.1.3 的 compile 工作流。
- 结果：Faulted，Unity 在 Package Manager 解析阶段退出，未进入 C# 编译。
- 原错误：`Failed to resolve packages: The "path" argument must be of type string. Received undefined. No packages loaded.`
- 日志：`D:/Unity_Project_1/3C-Artifacts/3c-gameplay/Runs/76b1360448ec47ed9a59744706edfe9d/Logs/unity-editor.log`。
- 当时已核对该 worktree manifest 的所有直接 file 依赖均存在 package.json，Unity 版本与 ProjectVersion 一致；UPM 根因见下方历史定位。
- 该次失败不作为当前主线的编译结论。

## Marker 私有图重建候选

- 独立 Timeline 根现在通过 `BtsmtlSkillGraphClosure.Validate(TimelineAsset)` 收集 TreeClip / Marker 图，沿同一正式闭包处理节点、连线、Blackboard、嵌套内容与稳定身份。
- `EmitTimelineRoot` 复用原图注册、创建、配置、连接和清理阶段；Marker 私有图缺失闭包时明确失败，删除旧子资产路径代替重建的分支。
- `EnsureTimelineGraph` 复用编辑器的 `TimelineGraphAuthoring.EnsureSubAsset`，保留 TimelineBody / TimelineTrigger 角色，不另建生成器专用图创建规则。
- 任务 8.5 仍未勾选：候选尚未通过完整编译与真实导出／重建。

## 主线作者闭包与执行域收口

- 用户已明确由本任务统一 Timeline 的执行域口径；移除主目录待提交的 `ResolveExportExecutionDomain` 自动降为 Logic／默认域的规则。
- `BtsmtlSkillAuthoringClosure` 在验证引用图后，只将当前根及其同资产私有内容纳入重建集合。共享 Timeline／Macro／图仍是正式外部引用，不再对外部图生成节点配置、连接及清理语句；私有 Marker 图缺失闭包继续明确报错。
- Track 导出通过 `TimelineAuthoringTrackBinding.Export` 读取并校验正式域，catalog 由装配层传入；不由作者工具推断业务域。缺声明或不支持的域保留具体轨道身份并失败，不隐式修复资产。
- Clip 的显式域进入既有 `TimelineAuthoringPropertyContract`，读写都校验轨道与 Clip 能力；新增正式 `InheritExecutionDomain` 作者操作。重建先建立继承关系，再按作者属性恢复显式覆盖，不把继承强制改成显式域，也不把合法覆盖写成轨道域。
- 本批变化只影响作者链，未增加运行时热路径分配。没有生成测试，也未用这些修改宣称运行链已达到 0 GC。
- 主线 Unity 实例 `e852139597e42532` 完成本批脚本编译与重载；最终观测 `1789834940597` 为非 Play、非编译、非导入，重载完成时间 `1789834912685`，随后错误控制台返回 0 条。首次编译发现基础 Timeline 程序集不能反向依赖 Tree composition，已改成传入正式 catalog 并重新编译。
- 8.2、8.5 保持未勾选：前者仍缺编辑器原子域修改和完整图能力约束，后者仍缺真实导出／重建及复制闭包证据。此处编译通过不代表资产迁移或端到端通过。

## 原 worktree 编译环境与依赖定位（历史）

以下仅为迁回主线以前的失败记录，后续主线工作见上方及文末。

- Run `17776ed34b634547ae425be647693db6` 再次在相同 UPM 阶段失败。
- 已定位环境原因：工具进程缺少 `ALLUSERSPROFILE`；安装的 UPM 在 `getGlobalConfigRoot` 中直接用它拼接路径。按 Windows `CommonApplicationData` 补齐正式子进程环境后，再使用同一 RunHost；未改包源、复制缓存或新增编译入口。
- Run `b7382578158f46c09b90e501cdcca17a` 已完成包解析并进入 C# 编译，最终 Faulted。四处独立错误均为现行基线缺少 `CharacterClipPlayerClockSource`，位于 `AnimationClipPlayer.cs:365` 和 `AnimationPhaseProjectionPlan.cs:286/295/321`；后续编译阶段尚无通过证据。
- 当时定位到 `add-network-model-locomotion-presentation-policy` 的消费者迁移位于主目录未提交改动；该记录仅解释旧 worktree 失败原因。当前已按上方“工作位置纠正”归回主线，不再等待独立依赖交接。
- 冷导入删除了 7537 个缺少配对资产的已跟踪 `.meta`，已仅恢复本 worktree 这些自动删除的原文件，未动主目录。UPM 自动从 lock 移除两项本 worktree 不存在的 embedded FSR 包；该 lock 差异未计入 Timeline 提交。
- 该 Run 已结束，进程 53988 已退出；旧 worktree 不再承担实施或验证，后续状态以主目录为准。

## 主线停止请求事务

- `RequestStopTimelinePlayback` 原先在下游拒绝时只撤回 playback 的 pending 标记，没有对已经收到请求的 Marker／TreeClip／领域 sink 执行 Discard；下游抛异常时还会留下 pending 标记。
- 现在同一入口在拒绝或异常时同时撤回 playback 与下游候选，失败不返回可提交的请求。正式 Commit 后的终态安装路径保持不变。
- `CancelTimelinePlayback` 复用既有 Request／Commit／Discard 方法，删除重复停止实现；未增加播放 owner、备用入口或运行分配。
- 此修复只补齐停止请求的拒绝／异常边界；任务 7.1 仍需共享表现采样与终态交付整体接线，0 GC 仍需处理既有停止请求快照等分配，均不据此勾选完成。
- 主线 Unity 完成该次编译和重载：观测 `1789835251464` 为 idle、非 Play／编译／导入，重载完成时间 `1789835225426`，错误控制台 0 条；未新增或执行测试。

## 主线执行域能力校验

- `TimelineContractCatalog` 不再跳过继承轨道域的 Clip：显式和继承域均按轨道类型及 Clip 类型能力检查；缺少轨道正式域也会失败。`RequireClipPlacement` 使用相同能力条件，在创建入口拒绝不支持的组合。
- Track／Clip 的 `SupportsExecutionDomain` 不再把非法枚举经 Normalize 当作 Logic 接受。
- TreeClip 当前只有 Logic TimelineBody 执行能力；删除其 Presentation 和 DualProjection 许可及“表现 TimelineBody”旧校验分支。Presentation 明确要求 Marker／TimelineTrigger，DualProjection 明确报告缺少表现投影，不把 Logic 图再执行一次。TreeTrack 保留 Marker 所需的域声明能力。
- 主线资源处理期间未发刷新／编译请求；资源处理结束后 Unity 自动完成源码编译。两个修改文件时间为 00:35:18，对应正式程序集更新时间为 00:35:24／00:35:25。观测 `1789835870089` 显示编译和重载已结束，错误控制台 0 条；Editor 正进入 Play，未刷新或 build。
- 6.1、8.1、8.2 仍未全部完成：Marker 节点及资源的表现能力约束、原子 Domain 编辑和完整 preparation 接线仍需实施。此处没有把 Track Inspector 入口写成已完成。

## 主线动作投影与播放器写入分离

- 既有 `CommittedFollowPresentationClockCoordinator` 已拥有 Registry、History、Projector 及其 Begin／Commit／Discard 租约；继续使用这个 owner，不新增时钟或注册表。
- 协调器的 `ProjectSample` 现在返回现有 `ProjectedActionPresentationSample`，不接收或写入 AnimationClipPlayer；CommittedFollow policy 再将结果写入播放器。FreeRun 与 CommittedMovement 的合法独立策略保持原链路。
- 删除 CommittedFollow 缺少通道、活动事务、播放身份或 committed 样本时调用 FreeRun 的回退。缺失合同输入明确失败，经原 Pose 帧 catch／Discard 撤回候选，不制造未提交进度。该变化不增加成功热路径分配。
- 已查明尚待迁移的具体顺序：`CharacterPresentationDomainRuntime.Present` 先调用 TimelineHost.Present；`TimelineToActionCommandBridge` 从 PresentationFrameProduced 生成动画命令；`RunPoseFrame` 之后才打开协调器事务。这还不是 committed 动作位置驱动 Timeline 的目标顺序，必须调整输入与帧边界后才可声称共享采样完成。
- 当前投影仍按 channel 查询 playback，并使用源时长；尚未变成 Timeline playback／generation 的单次动作区间投影。5.1、5.2、5.4、5.5 均保持未完成，不能仅凭方法拆分勾选。
- 本批先退出 Play，再修改并请求脚本编译；主线实例重载完成时间 `1789836193744`，观测 `1789836215836` 为 idle、非 Play／编译／导入，错误控制台 0 条。未新增测试，未宣称实际动作播放已通过验收。

## 主线同帧投影复用

- `ActionPresentationSampleProjector` 现在在既有 pending cursor 的值类型存储中保留本帧请求与投影结果；同一 playback 在同一 mutation lease 内只计算一次，后续相同请求直接复用。原实现的 Retained／RetirementPermitted 尾段会因重复调用再次累加 delta，此路径已删除。
- 重复请求的样本窗口、sample tick、delta、源时长、末端和生命周期必须一致；不一致明确失败，防止不同消费者按调用顺序覆盖共享时间。比较使用值类型字段与 EventId 的类型化比较，不产生装箱或逐帧容器分配。
- Commit 仅保留已接受游标并清空本帧投影；Discard 不写 committed cursor；已登记移除候选的实例拒绝同帧再次投影。没有新增 Registry 或另一份时钟。
- 本项仍是动画播放实例内的复用，不是尚未接入的 Timeline playback／generation 共享动作区间；5.2、7.1、7.3 保持未完成。
- 主线实例在 `1789836682344` 完成重载，`1789836724344` 为 idle、非 Play／编译／导入。本次 Editor.log 同时记录 Attack1／Attack2 私有 TimelineBody 图的三处连线端口缺失或类型不一致，控制台另有缺失脚本错误；不把编译重载描述为内容或运行验收通过。

## 私有图端口初始化顺序

- 三处失败连线在实际资产中均为正式 `Root.Output -> BlackboardSet.Input`，对应节点类型确实声明了这两个 Flow 端口；没有修改资产连线或端口名称。
- `BtsmtlSkillGraphClosure.ValidateTopology` 原先在遍历一个节点时立即验证其出边，可能先于目标节点的 GatherPorts。现分为全图端口收集与拓扑校验两阶段，保留原端点类型、容量、身份及环检查。
- 主线完成编译重载后，直接调用正式闭包校验，三张原失败图均通过；再调用 Ability 正式作者闭包及 TimelineContentDiscovery，得到 54 张图、7 个私有 Timeline，Attack1–5、Attack5End、Attack5End2 均 valid。查询仅加载并校验当前资产，没有写资产或生成测试文件。
- 这证明当前作者闭包与内容发现通过，不代表私有图导出重建、Marker 表现执行或完整动作运行已完成；缺失脚本错误不在此修复范围。
