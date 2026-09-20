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

## 正式导出结果读取进展

- 直接调用正式 `BtsmtlAuthoringCodeExportService.Export`，不调用 FileWriter；代码文件仍由系统文件工具写入。一次调用由 execute_code 历史确认完成，耗时 45454.4ms，返回结果预览含 `Success:true`；30 秒连接等待结束导致完整文件集未取回，不能据此声称生成代码已经保存或重建通过。
- 已确认现有 Unity MCP 传输支持请求参数 `timeout_seconds`；仅增加 CLI timeout 不能改变服务端 30 秒命令期限。无需修改工具服务器或增加另一套导出服务。
- 后续读取期间主 Editor 多次进入 Play／程序集重载，调用连接断开。本轮没有落盘导出的 C# 文件，也没有执行 generate_assets；不重复执行状态未知的资产写入。
- `Attack1.cs`／`Root.cs` 新增的未提交删除移除了 `Enter_To_State_Rule` 条件。只读核对实际资产中转移 `2c359c8b-c840-4888-b8db-fe22fe16df63` 的 `m_Payload` 为空，确认转移已无条件；图 `5ee51b65f135f7809601ec732f478ef1` 仍作为孤立子资产存在。因此源码删除与实际转移一致，保留该改动，不按孤立图仍存在误判为冲突或恢复旧条件。
- 8.5 仍未完成；需要取得完整正式导出结果、保留上述新口径，再继续重建。

## Marker 私有图导出与重建收口

- 已取回 Corin Attack 完整正式导出结果，通过系统文件工具写入 10 个根专属 C# 文件；原有入口条件删除保持，新增两个文件按现有 End／End2 状态维护边界分组。
- 当前 Marker `6409c746-28e1-421f-86fc-1104df163dca` 的私有图 `3d20d1d4c26a496393bb52f724252f24` 通过 EnsureOwnedGraph 创建 TimelineTrigger，再创建其原有 OnEnable 节点并绑定 Marker。导出外部依赖不包含本 Ability 资产，未借旧私有图路径／localFileId 重建。原图只有 OnEnable，输出忠实保留原内容；节点、端口、连线、Blackboard 仍走统一完整图生成流程。
- `035608a36` 在原 export_code／generate_assets 的 schema 和参数白名单中接受传输层已有的 timeout_seconds，没有新增导出服务、后台运行器或项目 fallback 配置。
- 首次真实重建发现默认显示名被写成显式名称覆盖，导致 12 项依赖指纹变化；资产差异只有 8 个节点的 `_name`。`bd99d195a` 由原生节点公开只读覆盖值、正式 Flow／FSM 作者 API 保留 null 默认语义，导出器消费该合同，不反射读取字段或修改指纹算法来掩盖差异。
- 修复后再次运行正式 `btsmtl.generate_assets`，返回 saved=true、diagnostics=[]；CorinAttackGameplayAbilityDefinition.asset 与本次重建前逐字节一致，52 张图与 7 个 Timeline 共 59 项正式指纹完全一致。再次调用正式导出服务，10 个文件按统一换行比较与当前源码全部相同。
- 独立 Timeline 根复用相同完整图生成阶段，通过 EnsureTimelineGraph 创建正式 TimelineBody／TimelineTrigger；嵌套私有图继续使用同一 owner 工厂。8.5 按 C# owner 闭包与重建范围完成，不以此代表 Marker 表现执行或秒制字段迁移完成。
- 主线完成生成代码及作者 API 编译重载；没有新增测试代码，没有改写源资产内容。当前任务完成 30/56，余项继续保持未勾选。

## Track 执行域作者入口

- 原 TimelineInspector 的 Track 面板增加 Execution Domain，Marker 明确显示其域继承自 Track；没有新增窗口或第二份配置。
- 域变更在原 ApplyModify / Undo 事务内调用 ConfigureExecutionDomain，再走正式 ValidateContent。Track 与 Clip 的声明能力不匹配时，错误包含内容身份，整次修改回滚，不自动改写显式 Clip 域，也不退回 Logic。
- 本次只补作者入口及已有合同校验。Marker 图节点的表现能力检查仍由 6.1 / 8.2 完成，不把 Inspector 接入视为该能力已经实现。
- 秒制迁移仍未完成：当前 TimelineData 的正式存储仍是整数作者帧；FixedScalar 位于 ThirdPersonSimulation.Fixed，Timeline 仅直接引用 Core。后续必须同时处理公共时间合同与依赖方向，不能用秒属性包裹旧帧字段冒充迁移完成。
- 本次未新增测试；Unity 编译重载期间 MCP 连接不可用，尚未取得本次编译完成证据，任务勾选保持不变。
## 秒制公共数值依赖

- 将现有 FixedScalar 从 Fixed/Numerics/SimulationNumerics.cs 移入公共 Core/Numerics/FixedScalar.cs；保留同一类型全名与全部运算实现，旧定义删除，没有复制第二份算法或新增兼容转发。
- Q32.32 的 FractionalBits 由数值类型自身声明，FixedSimulationNumericProfile 引用该常量。Core 不再需要为了使用秒制定点值而反向依赖 Fixed 执行器；Timeline 已引用 Core，后续公共时间合同可以直接复用此类型。
- 提取前后逐字比较 FixedScalar 主体，除常量来源改为等值 32 外完全一致。正式 Portable Fixed 与 Float32 项目均编译通过，各为 0 警告、0 错误；构建使用禁用 build servers / node reuse / shared compilation 参数，结束后执行 build-server shutdown。没有新增测试。
- 本次改变程序集归属，不改变数值格式。已搜索源码与资产，没有发现绑定旧程序集的 FixedScalar 类型字符串或反射查找；Unity 完整程序集重载仍待确认。
- 这是 0.2 的依赖准备，不代表秒制作者字段、被动求值、资产迁移或 tick 吸附完成，任务勾选保持不变。
## 推进余数与游标原子提交

- 发现 Advance 在接受结果前直接修改 m_FrameCarry，而 Discard 不恢复余数；失败后重试因此可能跨过不同的内容边界，Capture 也可能读到尚未接受的时间状态。
- 将本次余数存入既有 TimelineRuntimeAdvanceResult，Advance 只计算候选，Commit 才与 CursorFrame / Cycle 一起写入。Discard 或求值抛错不消耗 committed 余数；原 Capture / Restore 继续读写 committed 字段，没有新增快照链路或运行对象。
- tickCount 与作者帧率的乘法提升到 long，避免 int 乘法在大推进区间溢出。本次只增加既有结果中的整数值字段，没有新增每帧分配。
- 已核对唯一结果构造点及 Capture / Restore 读写链，git diff --check 通过。主 Editor 的 get_editor_state 仍超时，未取得本次 Unity 编译或运行证据，没有新增测试。
- 这是 0.4 所需原子提交语义的修复。时间单位仍为旧作者帧，外部秒制播放管理者尚未接入，0.4 保持未完成。
## Marker 首批秒制迁移

- TimelineMarker 删除 m_Frame / Frame，唯一作者存储改为 m_TimeRaw（Q32.32 秒），通过 FixedScalar Time 读取。Configure / AddMarker、图复制、闭包、排序、指纹、Logic Marker 请求和 Presentation Marker 事件均改为秒；没有保留旧帧字段或兼容读取。
- Inspector 输入秒，仅在实际编辑后走原 mutation；Slate 暂保留现有显示帧网格，通过 TimelineTimeGrid 统一映射，未拖动 Marker 时不量化回网格。NearestIndex 使用最近偶数，CeilingIndex 识别已量化网格位置，避免 47/60 的 Q32.32 舍入误差把终点抬到显示第 48 帧。
- 正式 C# EnsureMarker 接收 decimal 秒，导出由 raw / 2^32 生成 decimal 字面量，再经公共 FixedScalar.FromDecimal 使用原最近偶数规则重建。FromDouble 复用同一转换实现，避免大时间值经 double 导出丢失 Q32.32 低位。
- 迁移前通过 Unity AssetDatabase 读取全部 TimelineAsset，找到唯一 Marker：Timeline 10f4cb90-8b9a-4944-b77c-14efc9a3124d、Track 746b8d82-5d44-4f5d-a816-a92dfbc7773a、Marker 6409c746-28e1-421f-86fc-1104df163dca，旧位置 47。通过正式 EnsureMarker / ApplyModify 将其迁为 47/60 秒，保存 raw=3364391049；内容校验通过，身份及图引用保留。
- CorinAttackGameplayAbilityDefinition.asset 本来就包含尚未提交的 Marker、FSM 和其它作者改动，本次在当前内容上迁移并保存，未覆盖它们，也不把整份既有未提交资产夹带进代码提交。
- 当前迁移仍分阶段：Clip、Section、播放游标与 Slate 通用接口尚为帧。Marker 消费入口将当前区间端点换算成秒再比较，不再将 Marker 秒数量化回运行帧。这不是最终外部秒制被动求值；0.2–0.7、共享采样和自适应网格仍保持未完成。
- 没有新增测试。Marker 源码已成功编译加载并完成正式作者 API 迁移；正式导出成功且 diagnostics=[]。控制台另有原生 FSM 调用绑定与 CorinPanelExpandTimeline 缺失类型错误，不据此声称全项目运行通过。- 最终 decimal 入口编译重载后，再从持久资产读回 raw=3364391049；正式 Timeline 导出成功、diagnostics=[]，字面量为 0.7833333334419876337051391602m，FromDecimal 重建 raw 完全一致；最近显示网格与覆盖终点都为第 47 帧。此次仅调用导出服务读取结果，没有创建 MarkerSecondsSnapshot 文件或其它临时作者入口。
## Section 秒制迁移

- TimelineSection 删除 m_Frame / Frame，唯一存储为 m_TimeRaw；Create、Configure、AddSection、EnsureSection 和正式 C# API 均改为秒。闭包中的 TimelineContentSection、排序、运行 Section 选择及 Restore 一致性判断也比较定点秒。
- Inspector 使用秒输入，单独改名称保留原始秒值；Slate 仍使用现有显示网格，但未拖动不回写浮点显示值，拖动经统一网格映射再提交，并走原 ConfigureSection 排序。Marker 仅改图引用时同样保留原始秒值。
- 删除未被项目或工具源码使用的旧 TimelineSectionDescriptor / TimelineSectionCatalog 整数帧声明，不留第二份 Section 时间合同。ActionCue 的 LocalFrame 暂仍为既有来源元数据，由秒差映射，不用于推进；第 0.5 节仍未完成。
- 迁移前通过正式资产读取记录 11 个 Timeline 的 11 个 Section，包含原第 75 帧 Attack_Normal_03_Explode。编译后通过 EnsureSection / ApplyModify 迁移并保存，全部内容校验通过；第 75 帧变为精确 1.25 秒，其余为 0 秒。
- 最终编译重载后逐项读回，11 项时间、身份、名称、NextSectionId 和 BranchId 全部与迁移记录一致。正式导出第 75 帧所在 Timeline 成功、diagnostics=[]，生成 EnsureSection(..., 1.25m, ...)。没有生成临时作者源码或新增测试。
- 已跟踪 CorinAttack 资产只暂存本次 3 个 Section 的字段迁移，其既有 FSM、Marker 等未提交变更保留；8 个原本未跟踪的 Rush Timeline 也已迁移保存，继续保留原工作区状态。
- Clip、播放游标、外部控制、共享采样、完整派生产物和可配置网格仍未完成，任务 0.2–0.7 等不提前勾选。
## ClipIn 素材起点秒制迁移

- Clip 删除 ClipInFrame 正式存储与 FrameToTime 中的派生副本，唯一存储改为 m_ClipInTimeRaw；ClipInTime 返回公共 FixedScalar，ConfigureClipIn 校验非负。动画采样在现有浮点素材接口边界 ToSingle，不再先量化到作者帧。
- 正式 EnsureClip / ConfigureClipSegment 的素材起点参数改为 decimal 秒；导出通过 raw / 2^32 生成 decimal 字面量。现有生成代码的 20 个完整调用该参数均为零，数值含义不变且均可编译，无需重写其它任务的生成文件。
- Inspector 支持 Clip In 秒输入，仅实际修改该字段才换算；Slate 草稿持有定点秒，ClipInFrame 仅留在既有表面接口作显示与拖动网格，读取／编辑其它字段不把未修改的素材起点回写为帧。动画拆分按秒增加素材起点。
- 迁移前读取 19 个 Timeline 的 271 个 Clip，ClipIn 原值均为零。通过原 ApplyModify / ConfigureClipIn 保存，再逐项核对 271 个身份与秒值。配置资产内旧 ClipInFrame 字段计数为零；暂存资产只包含 271 处本次字段替换，其余未提交改动保持原样。
- Unity 已完成编译重载，正式 API 迁移调用成功；共享 MovingTurn Timeline 正式导出成功、diagnostics=[]，完整 EnsureClip 调用使用 0m。未创建临时作者代码或新增测试。
- 期间本机 MCP 服务退出，8080 无监听；恢复原安装服务后同一 Unity 实例自动重连，未重启 Editor。控制台仍存在 FSM 调用绑定、Panel Timeline 缺失类型和 Timeline.meta GUID 诊断，不声明全项目运行通过。
- Clip 起止、混合区间、播放游标、共享采样和自适应网格仍待迁移，相关任务保持未完成。
## Ease 混合区间秒制迁移

- 删除四个 Self/Other Ease 整数帧字段及派生浮点副本，唯一存储改为 Q32.32 秒。作者 API 使用 decimal 秒，Inspector 输入秒；Slate 保留未编辑的精确值，拖动沿当前显示网格转换。动画、相机、运动曲线在既有浮点采样边界读取秒值。
- 迁移前读取 271 个 Clip，其中 10 项存在非零混合区间（6 帧）。通过原 ApplyModify / ConfigureEase / UpdateMix 迁移并保存；1084 个字段逐项匹配旧值除以 60 的最近偶数结果，持久化资产独立读回也全部一致，配置资产旧 Ease 帧字段计数为零。
- 首次重算发现非 Mixable TreeClip 被通用重叠算法生成混合区间；事务回滚。UpdateMix 现遵守原 IsMixable 能力声明，修正后全部字段一致，不为逻辑图片段产生混合。
- Unity 已编译重载并执行正式迁移。正式导出非零 Self Ease 的两个 Clip 成功、diagnostics=[]，输出 0.1000000000931322574615478516m。此前按更长小数字符串筛选未匹配是文本末位差异，不能当作内容丢失；本轮按 Clip 身份读取实际导出行确认。
- 资产暂存只包含 1084 处 Ease 字段替换，保留其余未提交内容；未新增测试，未创建临时作者源码。起止时间、播放控制、共享采样及配置网格仍未完成，任务不提前勾选。
## 接续实施：运行 Clip 时间合同

- 已按用户授权删除 CorinPanelExpandTimeline 及 meta（1464879ec），没有恢复旧内联图。删除前配置目录内 19 个 Timeline 正式内容校验中仅此资产失败；Assets 范围名称与 GUID 查询未发现消费者。后续由工厂重建。
- Inspector 属性变化重新进入原 ApplyConfiguration / ApplyModify / Undo 链（3661249df），不再只修改临时配置对象。
- TimelineContentClip 起止、TimelineRuntimeClipBoundary、TreeClipRequest、ClipSample 和 TraceOutput 的正式位置统一使用 FixedScalar 秒，删除这些合同的整数帧位置。边界比较与排序直接使用秒，循环和同位置 Exit 优先顺序保留。
- 本批仍从尚未迁移的作者 StartFrame / EndFrame 建立秒制内容；播放游标仍为整数帧。没有把这些剩余入口当作最终方案，任务 0.2–0.7 继续保持未完成。未新增测试，原有运行容器分配尚待清理，不声明 0 GC 已完成。
- 本批 Unity 编译和域重载完成，观测 1789875977302 为 idle、非 Play／编译／导入，错误控制台 0 条。通过正式内容发现只读加载剩余 18 个 Timeline、266 个 Clip，内容校验和秒制区间检查 errors=[]；这不是完整播放验收。

## 执行域收敛为 Track 唯一声明

- 根据用户在“查看 Timeline 当前状态”任务中的最新决定，只保留 Logic／Presentation，删除 DualProjection；Clip 与 Marker 继承所在 Track，内容类型只校验是否允许放入该域轨道。同一动作共享进度，逻辑结果沿已有提交链交给表现。
- 修改前通过正式对象读取 66 条轨道、266 个 Clip，130 个 Clip 有显式域，全部与轨道域一致，无需要保留的跨域覆盖。删除 Clip 序列化域及覆盖／继承操作、作者属性和生成代码中的覆盖表达；删除第三域枚举、能力与消费分支。
- Track 未声明合法域继续由正式校验拒绝；删除非法枚举或未绑定 Clip／Marker 默认为 Logic 的回退。域有效性使用直接枚举比较，避免此处 Enum.IsDefined 的装箱。
- Unity 编译重载后，18 个 Timeline 内容校验通过，再通过 AssetDatabase 正式重序列化清理旧 Clip 字段。配置资产 Clip 域字段剩余 0；索引仅删除 HEAD 中 256 个旧 Clip 域字段，其它原有资产改动继续保留在工作区。
- 当前设计和变更规格已同步两域规则；现行 character-animation-pipeline 规格原先仍要求第三域及 Clip 覆盖，已按用户决定修正。上方较早实施记录中的覆盖／双侧投影只作历史，不再是当前目标。
- 最终重载观测 1789876549152 为 idle，错误控制台 0 条；正式读取 18 个 Timeline、266 个 Clip 的作者属性，executionDomain 属性数为 0，运行枚举仅 Logic／Presentation，内容校验 errors=[]。完整共享采样和 Marker 表现执行尚未完成。
