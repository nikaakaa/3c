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

## Clip 起止与精确末端迁移

- Clip 删除 StartFrame／EndFrame 和派生浮点时间副本，保存 m_StartTimeRaw／m_EndTimeRaw；StartTime、EndTime、DurationTime 是 FixedScalar 秒。ConfigureTimeRange 是正式写入入口，创建、混合重叠、分割复制、Inspector、Slate、作者 API 与输出均使用同一秒制区间。
- Timeline／Track 总时长及逻辑 TreeClip 的终点对齐改为精确秒，避免编辑其它属性时将末端重新凑整到帧。剩余 MaxFrame 只服务尚未迁移的播放游标和编辑显示，后续继续删除运行依赖。
- 266 个 Clip 已通过 ApplyModify／ConfigureTimeRange 保存；事务内先恢复全部区间，再 Init 建立归属并 UpdateMix。最初在 Init 前 UpdateMix 遇到未绑定 Track，事务回滚；提前 Init 又因未恢复素材区间而失败，最终调整为上述正式顺序。未引入兼容字段或反射写入。
- 回读 266 个起点全部等于旧帧／60的最近偶数量化值。7 个 MotionCurve 末端补齐至素材精确时长，13 个逻辑 TreeClip 随 Timeline 末端对齐，共20项末端变化；最大增量204 raw，约47.5纳秒。18个 Timeline 内容校验 errors=[]。配置资产旧 StartFrame／EndFrame 字段为零。
- 索引只纳入266个Clip的起止字段替换及8项混合字段重算，保留其它资产改动。原生成代码147个创建调用换为decimal秒；已按迁移结果同步完整调用的精确末端。素材采样在原float接口边界转换。
- 角色运动绑定沿上一任务未提交的定点秒映射继续完成：原映射二进制与指纹变更一并收口，绑定构建器直接读取Clip秒值。播放游标、旧Motion catalog字段、共享表现采样、Marker表现执行及0GC仍有未完成项，不据此勾选完整任务。
- 最终编译重载于1789880567369完成，观测1789880609780为idle、非Play／编译／导入；重载后18个Timeline、266个Clip再次校验errors=[]，错误控制台0条。未新增测试，未声称完整运行或生成重建已验收。

## 点事件区间精度

- ActionCueTrack 与 CameraCueTrack 的 Sample 入参改为 FixedScalar 秒，跨点判断直接比较作者端点，不再将事件位置转成 float；0 秒起点仍只由 includeStartBoundary 明确启用。
- 逻辑与表现求值共用 TimelineRuntimeEvaluationSegment，删除重复的 PresentationSegment。区间保存定点秒，动画／运动曲线采样在原浮点接口边界转换；MotionWarp 区间相交比较保留定点精度。
- 当前 BuildSegments 仍从待迁移的逻辑整数帧／表现浮点帧游标换算秒，该剩余入口不作为最终方案；没有新增一份权威游标。原 List、闭包和结果对象分配仍待后续清理。
- Unity重载完成1789880893502，随后错误控制台0条。只读调用现有ActionCueTrack.Sample检查180个非零实际Cue：从触发点前1 raw跨至触发点均恰好返回该Cue一次，原地重复区间不返回该Cue，errors=[]。未创建测试文件、未修改资产；该证据仅覆盖点事件边界，不代表整条播放链验收。

## 新建 Clip 草稿秒制收口

- TimelineClipCreationRequest 删除 FrameRate／StartFrame／EndFrame／DefaultEndFrame，草稿只保存定点秒。弹窗输入秒，仅字段变化时转换；选择Animation或RootMotion素材时直接使用源秒数，不再先按显示帧率取整。
- 创建提交直接传递草稿秒值给正式 AddClip／ConfigureTimeRange；逻辑TreeClip按精确Timeline末端对齐。显示网格只在打开弹窗时将点击帧位置转换一次，不进入草稿存储。
- Add Clip菜单通过既有Clip contract按轨道域过滤类型，删除已不允许的Presentation TreeClip创建后分支；不恢复跨域覆盖或空表现图执行。
- 配置驱动的逻辑tick／素材帧／关闭吸附与显式重新对齐尚未完成，不能因创建弹窗改用秒就勾选8.3／8.6。
- 编译与域重载于1789881136894完成，观测1789881157099为idle、非Play／编译／导入，错误控制台0条。创建草稿及弹窗源码不再含StartFrame／EndFrame／FrameRate字段；未进行鼠标操作验收，未新增测试。

## 逻辑播放推进与快照秒制迁移

- TimelineRuntimeService 在既有Step／Advance入口计算目标秒数和带符号换算余数，TimelineRuntimeAdvanceRequest改为previous／target秒区间；TimelineRuntimePlayback不再从tick数积分作者帧，只消费区间、处理循环及内容生命周期。候选时间和余数仍随同一Commit／Discard接受。
- 逻辑游标、AdvanceResult、CommittedEvaluation及Ability／Timeline快照统一为FixedScalar，余数改名TimeCarry。Fixed／Float32角色快照编码写64位raw，格式版本7升8，不保留旧快照兼容读取。
- TimelineContentUnit保存精确Duration；逻辑边界、Marker、循环分段和Section查询直接比较秒。帧仅暂留在尚待迁移的表现推进及部分诊断／业务输出，未新增第二套内容。
- RestoreCommittedState先用目标游标与循环校验Tree关联，再安装状态；失败不提前修改当前游标与余数。成功恢复前清空旧的已退出／待退出列表，避免重复恢复累加。
- 暂停／倍率控制、表现driver自主积分删除、共享采样、持续分配清理仍未完成；0.4／0.7和第5节不提前勾选。
- 推进计算对整数分子先求商余数，再按最近偶数舍入，循环长度raw的奇偶进入tie判定；带符号余数范围为±tickRate/2。没有用每tick先量化再累加，也不经float算逻辑秒。CharacterTimelineHost.Update的非CoreDriven delta×60旧入口与Presentation driver仍待后续移除。
- 本批编译重载完成1789881757642，观测1789881770374为idle，错误控制台0条；18个Timeline正式内容发现的Duration与作者精确DurationTime逐项一致。尚无完整Step／恢复运行证据，不把编译和内容发现视为完整回滚验收。

## 表现秒区间与无分配循环分段

- 表现driver的游标、循环末端、Marker比较和Evaluate入参统一FixedScalar秒，删除CursorFrame和作者帧率参与表现推进的换算；内容精确Duration不再经CeilingIndex改变循环长度。
- 逻辑与表现两套BuildSegments／List删除，统一TimelineRuntimeEvaluationSegments值类型按索引计算首段、完整循环及末段；每次求值不再为分段分配List和数组。保留区间顺序和4096循环范围约束。
- driver仍在原AdvanceCursor中累加delta，该自主进度尚未移交共享动作采样，不能视为5.2完成。MarkerTraversal、结果列表、事件身份字符串等分配及表现提交／丢弃链仍待处理。
- 当前调用顺序仍是Timeline产生动作命令后Pose打开投影事务；后续必须同时处理动作命令来源与事务范围，不能仅把Present挪位置制造另一条计时链。
- 本批编译重载完成1789882074513，观测1789882098124为idle、非Play／编译／导入，错误控制台0条。源码已无表现CursorFrame与分段List；没有以此声称整体0GC或共享采样完成。

## 轨道域筛选与采样委托清理

- 删除ActionCue、Animation、MotionCurve和相机各采样接口的可选Func<Clip,bool>，以及Evaluate每次创建的logicClipFilter／presentationClipFilter捕获委托。
- 求值器在轨道循环直接筛选Track.ExecutionDomain与PersistentMuted；剩余跨轨道Clip边界读所在轨道域。删除每个Clip都线性遍历Content.Clips的HasProjection辅助函数，不再维护另一份可覆盖执行域。
- 正式内容在preparation校验后只读消费，轨道采样只负责本轨道的数据映射。此改动消除两侧求值的域过滤闭包分配与重复扫描；结果容器、Marker事件和生命周期仍有其它分配，不声称整体0GC完成。
- 编译重载完成1789882282243，观测1789882291050为idle，错误控制台0条。全Main源码已无clipFilter／logicClipFilter／presentationClipFilter引用，未新增测试；共享动作采样仍待接线。

## Marker准备结果复用

- TimelineContentMarker在内容发现阶段已记录GraphId／GraphRevision并按Time、MarkerId排序。逻辑Marker直接消费这份已验证的正式数据，删除运行时FindTrack、FirstOrDefault捕获谓词与图依赖重复扫描。
- 两域按cycle外层、已排序Marker内层遍历，天然保持原cycle→time→identity顺序；删除逻辑二次排序，以及表现MarkerTransition值、临时List和排序。跨圈仍保留上一圈末端与下一圈起点各自的循环身份。
- Presentation Marker候选仍在原driver中生成，其正式图执行与接受／丢弃边界尚未接通；本批仅减少遍历准备开销，不能当作表现Marker业务已完成。
- SourceTracks只被旧FindTrack消费，删除该运行查找后一并移除内容对象中的重复Track引用集合及构造参数；保留原SourceTimeline作为正式源，不新增影子图或备用查找。
- 编译重载完成1789882677095，错误控制台0条；正式内容发现覆盖18个Timeline，现有1个Marker的图身份／版本与timeline.tree依赖一致、排序检查errors=[]。当前实际内容只有1个Marker，该证据不覆盖多Marker跨循环端到端，未新增测试。

## 表现Marker错误执行路径清理

- 已确认Skill图只通过Simulation编译器生成operations，角色依赖安装也只读取Simulation SourceMap；原InvokePresentationMarkers借用仅在逻辑tick作用域有效的m_ActiveTreeClipInvoker，不能构成正式表现图执行。
- Simulation图编译入口明确拒绝Presentation Marker，诊断包含调用路径、Timeline和Marker身份，禁止将表现触发图混入Simulation operations。角色内容安装遇到Presentation Marker明确报告缺少表现图executor，不接受逻辑图资源代替该能力。
- 删除表现帧构造AbilityTreeClipInvocation并调用逻辑invoker的实现；若收到未安装executor的Marker事件，保留明确失败，不静默丢事件或调用Simulation逻辑。
- 这完成错误归属的拒绝和旧路径删除，不代表6.1–6.4已全部完成。正式表现节点能力、只读上下文、typed输出与帧候选事务仍需实现；第6节保持未完成。

## 非Simulation入口删除60帧时间量化
- CharacterTimelineHost.Update直接将调用者deltaTime转换为Q32.32秒，删除乘60、整数四舍五入和至少前进一帧。零delta保持零时间区间，负数和非有限输入明确拒绝。
- TimelineRuntimeService的秒输入Step在既有播放游标上生成前后时间请求，沿原TimelineRuntimeStepCoordinator完成求值、消费和提交；循环、Decision截停与停止继续使用同一执行链。Simulation入口仍通过正式tick率和余数换算秒，不受本次修改影响。
- 这只移除非Simulation入口的旧作者帧量化，不代表共享动作表现采样已经完成。当前Timeline先生成动画命令，Pose后打开CommittedFollow采样事务；下一步需在原命令/采样事务内拆开内容选择与时间求值，不能另设时钟服务。
- Unity脚本编译与域重载完成（1789883519377），错误日志为零；未新增测试。首次编译发现FixedScalar命名空间缺失，已补齐后重新编译。

## 区间采样保留正式秒精度
- Animation、Camera State/Response/Effect、MotionCurve的正式Sample输入改为FixedScalar秒；删除float时间重载，逻辑/表现求值器直接传入同一个精确区间。
- Clip起止、Hold边界、TreeClip Update和ScenePresentation激活判断直接比较Q32.32时间。先以秒制求差和截断，再转换为Unity素材/AnimationCurve需要的float，不先转换绝对时间后相减。
- MotionCurve删除Mathf.Approximately的时间相等判定，改为比较截断后的精确本地时间；MotionWarp源窗口采样沿用同一正式秒输入。
- Unity脚本编译和域重载完成（1789883931101），错误日志为零。只读调用现有共享Timeline资产的AnimationTrack.Sample：12个Clip在StartTime.Raw-1处均无对应贡献，其中2个边界若转float会与起点重合。该证据只覆盖共享资产动画起点，不代表所有素材输出或共享动作采样完成；未新增测试。
- 共享采样仍有明确未完成项：TimelineToActionCommandBridge目前从表现帧生成ActionCommittedRawSample，CommittedSequence借用render frame，动作projector之后才打开事务。必须改为正式逻辑提交事实驱动并调整原事务顺序，不能将这些伪committed样本当作共享时钟的完成依据。

## Ability推进候选暴露精确进度
- AbilityTimelineTickResult现在通过IAbilityTimelineAdvancePending返回AbilityTimelineProgress值类型。内容身份/版本、generation、logic tick、精确前后时间、循环、内容时长及完成状态来自实际Advance结果，不从渲染帧倒推。
- CharacterTimelinePendingAdvance在创建时冻结该值，继续由原Commit/Discard拥有接受权。Running候选必须具备有效进度；终态无推进时允许默认空进度。零时长内容沿用TimelineContentUnit既有合法合同。
- 该合同本身不新建计时器、Registry或图runtime，也没有把Presentation轨道改成Logic求值。
- 尚未完成：经原PresentationCommand输出提交/替换/撤销通道发布这些候选中的最终事实，让既有Action采样事务统一消费；目前TimelineToActionCommandBridge的旧渲染帧样本仍在，未声称已删除或共享时钟完成。不能直接订阅Host.CommittedEvaluation发布，因为rollback中间推进也会经过该回调。
- Unity编译与域重载完成（1789884594787），错误日志为零；保留已有其他文件改动，未新增测试。

## 精确进度进入既有表现命令传输合同
- Fixed/Float32 PresentationCommand及Unity CharacterPresentationCommand增加TimelineProgress正式种类和AbilityTimelineProgress typed字段，要求Action身份及header逻辑tick与进度一致。Unity转换直接保留该值，不将原始秒转float或写字符串payload。
- 回放输出差异比较逐字段比较进度，包含内容revision、generation、循环和完成状态，避免时间变化被误判为相同输出。
- 统一AbilityTimelineProgressCodec按int64保存Q32.32原始时间；ServerAuthoritative egress对该命令读写typed进度，相关schema由7升级8，不兼容读取旧schema。
- Unity全资源刷新、编译及域重载完成（1789885021027），错误日志为零。新增脚本需全资源刷新才会导入，已处理；未新增测试。
- 这批只打通命令表示/转换/比较/编码，不宣称生产端和消费端已接通。原TimelineToActionCommandBridge、复制策略和共享采样owner尚需后续一起迁移，当前不新增提前发布进度的Host回调。

## 原输出提交链驱动Timeline被动表现采样
- Fixed/Float32 TickTimeline在推进候选有效时产生typed TimelineProgress命令，归属在推进前捕获的Action实例；按原Presentation输出收集、提交、替换与撤销流程交付，不从Host逻辑回调旁路发布。编译器为Timeline节点声明正式timeline-progress Producer，复制策略将该事实作为可靠输出。
- CharacterPresentationDomainRuntime把进度交给已有CommittedFollowPresentationClockCoordinator。协调器在自身预分配表内按Action/调用源/Timeline保存接受事实并缓存每表现帧采样，未新增Registry服务或delta时钟。原输出键按Action和节点来源隔离；相同EventId的进度修改也会触发替换。
- TimelineRuntimePresentationDriver删除AdvanceCursor及deltaSeconds累加，接口改为接收精确采样位置；校验generation和内容revision。非CoreDriven播放直接消费既有Update提交的秒进度，也不再由表现帧独立推进。
- 协调器对完整循环秒坐标做nearest-even插值；超过最后样本的逻辑tick保持目标端点，避免alpha重置造成往复。旧generation、终态清理和回退位置有明确处理，修正位置只重采当前内容、不补发经过事件。表现帧LogicTick来自接受样本。
- Unity编译与域重载完成（1789886218793），错误日志为零。未新增测试，未做Play模式验收，未把已修改的Ability派生资产覆盖重建。
- 尚未完成：TimelineToActionCommandBridge仍将映射后的表现动画样本包装为ActionCommittedRawSample，需在原动作样本/命令合同中清理来源并避免保留态再次外推；表现帧Discard与Marker记账尚未统一事务。远端角色尚需正式clock binding与Timeline消费装配，不能以新命令的编解码可用宣称远端播放完成。新Producer需随正式Ability工厂重建派生产品。

## 按5.1–5.3分清逻辑样本与共享采样输出
- 保留ActionCommittedRawSample、ActionCommittedSampleHistory及其原插值合同。TimelineToActionCommandBridge删除构造伪committed raw sample的路径，改为ActionProjectedSample：显式携带来源逻辑tick、PresentationFrame和已经完成Clip映射的采样时间。
- 仍走同一个ActionPlaybackCommandInbox和ActionAnimationPlaybackLifecycleRegistry，新命令参与原Select/Sample/Complete/Release生命周期、候选复制、Commit/Discard与诊断。一个playback generation不能在逻辑raw和已投影来源之间切换；没有新增播放Registry或临时时钟。
- 既有CommittedFollow策略消费已投影结果时直接交给Player，不再送入history插值或保留态delta外推；逻辑raw样本仍走原History/Projector。ProjectedActionPresentationSample的事件字段改为SourceEventId，不把表现来源标为已提交逻辑事件。
- 本批不改locomotion、Body、素材phase或混合权重owner。5.1–5.3仍保持未完成：推进原因/正式控制、共享帧候选的接受与丢弃、7节停止修正事件事务尚未完全闭合，不能凭这次样本消费迁移勾选整体任务。
- Unity脚本编译及域重载完成（1789887235950），错误日志为零；静态核对Timeline桥已无ActionCommittedRawSample构造，逻辑命令publisher仍保留原正式raw入口。未新增测试。

## 第7节：Pose拒绝时统一丢弃Timeline表现候选
- Timeline driver将本帧游标、完成状态、Marker traversal记账保留为pending，只有CommitPresentationFrame才写入已接受状态。Marker记账改为随内容预分配的双数组；Discard后重试不消耗事件身份。
- 原Host拆分PresentationFramePrepared与PresentationFrameProduced：动画桥先提供Pose候选命令，相机桥只在Pose接受后收到Timeline输出；停止播放的退役与缓存释放也延迟至接受阶段。Camera.Present移到该阶段之后，Pose事实输入仍只读取原Body/Trajectory事实。
- 原动作时钟协调器用预分配baseline回退修正标记和帧内采样缓存。原动画桥按Inbox正式容量预分配快照，失败时恢复生产者集合与元数据；Inbox按本帧起始发布序号移除候选命令，保留之前已接受逻辑输入。
- CharacterPresentationDomainRuntime通过finally覆盖EventGraph失败、缺少必要运动事实、Pose资源未就绪、Pose准备/求值/验证失败等提前退出路径。没有新建事件系统、Registry或并列时钟；未改现有Inbox空读lease的其他未提交修复。
- Unity编译及域重载完成（1789887842882），错误日志为零。静态核对driver已接受游标与Marker计数只在Commit写入，动画/相机分别订阅准备/接受阶段。未新增测试。
- 第7节仍未整体完成：正式Stop/Cancel的确认交付与最终分支revision需要继续接入；Pose已接受之后下游发布异常的原子失败边界、正式Presentation Marker图executor及其typed输出还需完成。当前不能据此勾选7.1–7.4或宣称整个运行链0 GC；旧求值结果集合仍有分配。

## 第7节：正式停止进度与终态确认交付
- AbilityTimelineProgress 的 bool 完成标记替换为 Active / Completed / Stopped 正式状态。停止候选冻结原内容版本、generation、动作实例、逻辑 tick 和精确位置；Fixed / Float32 沿原 PresentationCommand 产生停止输出，使用原 Timeline operation 的 producer 和事件 header。传输状态改为 byte，ServerAuthoritative 对应 schema 升到 9，不兼容旧格式。
- 逻辑 Restore 清理旧 runtime 时仍走原停止事务，但不产生业务进度命令。删除表现 driver 在逻辑 CommitStop 回调里写永久停止状态，以及读取逻辑 Stopping / Stopped 拒绝表现的路径；核心表现只认原输出调和链交付的状态。
- 原输出适配器沿已有 confirmedTick 门槛延迟 Completed / Stopped；未确认停止期间保留上次已交付采样。到达确认门槛后即使没有新命令，也重新调和待交付终态；交付后清理已确认且无预测后继的 Timeline 命令历史。动画终态账本仍只登记原动画命令。
- 共享采样显式给出 Advance / Correction / Completed / Stopped；停止不进行区间跨点求值，完成样本到达末端后才结束。Host 在表现帧接受阶段释放采样、动画和相机生命周期；非核心本地播放保留其直接逻辑提交归属。
- 仍未完整解决：最终分支撤销后的表现退役与 generation 绑定、Marker 图 executor、后续播放倍率 / 暂停和所有运行热路径分配。因此第5、7节不提前整体勾选。
- Marker traversal identity 固定为本 playback generation 内的 cycle + 1，原预分配数组保存该 Marker 已交付的最大 traversal；回退修正后再次跨过同一循环的 Marker 不重复交付，下一正常循环仍可触发。数组候选随 Pose 帧 Commit / Discard 接受或恢复，不新增集合或第二事件系统。
- Unity 最终编译及域重载完成（1789889008875），实例 e852139597e42532 已恢复 idle、非 Play、非编译状态，错误日志为零。git diff --check 通过；未新增测试，未声称 Marker 图执行或整个第7节已经完成。

## 第7节：分支撤销按原生命周期收尾，Restore 保留表现状态
- 原 ActionAnimationPlaybackCommand 增加 Withdraw，原生命周期结束原因改名 ActionPlaybackEndReason，明确 BranchWithdrawn，与已确认 Complete / Release 分开。Inbox 对已消费命令的撤销、Timeline generation 替换和修正后失效 Clip 都使用该原因；沿原 Slot 保留与退役许可链释放资源，不新建播放器。
- 原共享采样表保留已撤销行直至表现帧接受，采样和释放均精确匹配 generation；撤销不再直接删行而让 Host 永远等不到结束输入。最终 replacement 在动作身份、来源、内容或 generation 改变时先撤销原行，新旧分支各自处理。
- 动画结束／撤销候选在 Pose 求值前进入原 Inbox，本帧 Pose Discard 时由原桥快照回退；相机请求退役、driver 与采样缓存清理仍在帧接受阶段。修正原因透传到 TimelineRuntimePresentationFrame，修正移走的 Clip 不伪装自然结束。
- TimelineRuntimeService.ApplyRestore 对同 handle、generation、执行身份及内容版本原位恢复已提交逻辑状态；Host 不再追加重复 ActivePlayback，不重置表现 driver。新实例恢复后推进 handle / generation 分配边界，避免后续创建碰撞。
- 删除快照中的 ActiveTreeClipAssociations 双份派生数据及其构造／比较链：图关联完全由固定内容、时间、cycle 和 TreeDecision 集合导出；旧 Ability 恢复曾传空关联而与动态构造结果冲突。直接 runtime schema 升 v6，不提供兼容读取。
- 尚未证明完整完成：撤销后同一 generation 再次成为最终分支时的重接入、prepared 图执行、所有热路径 0 GC 与倍率／暂停仍需继续处理；第7节暂不整体勾选。
- Unity 编译与域重载完成（1789890365305），实例 e852139597e42532 idle、非 Play，错误日志为零；git diff --check 通过。未新增测试。

## Restore 活动集合校验收尾
- 活动 Clip 校验改为与正式内容计算出的完整集合比较，拒绝遗漏、重复及多余项，删除每次恢复创建的 HashSet 与接口集合的 LINQ Contains 枚举。TreeDecision 待退出仍属于本步已提交活动集合，只有已退出列表决定排除，吻合下一步消费退出请求的顺序。
- Fixed / Float32 恢复遍历直接扫描已有快照判断 handle 是否保留，删除每个 actor 临时 HashSet；未增加第二份持久状态。
- 这是已有 Restore 修改的收尾，不能代表整个 Timeline 0 GC 完成。后续优先完成倍率／暂停、表现 Marker 执行和作者域／网格功能，不继续扩展无关恢复框架。
- 本批脚本构建日志显示成功；域重载期间 MCP 暂时返回 503，尚未取得重载后的控制台结果。未新增测试。

## 作者域编辑与 Slate 时间网格
- Track 配置提交改为先克隆当前内容并调用正式 TimelineContentDiscovery；轨道域、Clip 合同、Marker 角色及图闭包检查通过后，才用原 ApplyModify / Undo 一次提交名称、静音及域。失败信息带轨道身份，失败预检不改正式资产。表现图节点域能力仍依赖第6节完整接入，未把闭包通过等同于表现执行完成。
- 遵照用户要求使用 Slate UI：原嵌入工具栏增加“吸附”菜单，参考 Pipeline、关闭／逻辑 tick／动画素材帧和选中内容重新对齐均放在该菜单；外层窗口未增加第二排控件。
- 删除 OpenRequest 默认 60 Hz 逻辑频率；会话明确绑定 CharacterPipelineDefinition，实时读取其 SimulationTickRate。缺绑定时逻辑吸附不可选，改变网格不改既有作者时间。素材帧使用选中动画的正式 frameRate 和 Start / ClipIn 映射，采用 nearest-even 回到同一 Q32.32 秒。
- Slate 嵌入编辑器的拖动吸附交给原 Timeline binding，删除先按显示帧率截断再换算逻辑 tick 的路径；Clip 起止提交再次采用同一正式网格，Blend 时间不再固定量化到60作者帧，Clip 缩放删除最小一显示帧限制。
- 选中 Clip、Marker、Section 或整条轨道可显式重新对齐，经原 Apply mutation 一次 Undo；不支持素材的选中项明确失败，时长被压为零明确失败，不悄悄拉长。
- 尚未完全完成8.3：Marker / Section 拖动、ClipIn 及部分曲线的 Slate 接口仍使用整数显示帧；当前没有宣称整个编辑器已完成秒制和素材映射迁移。倍率／暂停、正式表现 Marker 执行仍在主目标内。
- Slate 原左上区域高度40像素内复用两行紧凑布局，避免轨道栏缩到230像素时吸附菜单被裁掉；未扩大外层窗口工具栏。
- 本批脚本构建成功；改为 Slate 原工具栏后的首轮域重载完成（1789891834225），错误日志为零。最后紧凑布局调整构建成功，域重载结果继续核对；未新增测试。
- 最终布局调整后的域重载已完成（1789892061410），Editor idle、错误日志为零。

## Slate Marker、Section、ClipIn 与片段操作秒制接口
- Marker 的 Slate binding 删除整数 Frame 草稿，改为 Q32.32 候选；创建与拖动直接传秒，绘制只在屏幕边界转 float，未修改位置时不损失原始精度。Section 新增、定位和拖动提交调用同一会话网格，删除提交时再按显示帧率取整。
- ClipIn 改为秒输入，左侧向右裁剪时增加对应素材偏移，删除整数帧往返。动画 Clip 新增、粘贴、切分和 MotionCurve 源区间裁剪也直接传秒；删除无调用方的 AddClipAt 整数帧入口。素材曲线在其正式源 float 边界转换，Timeline 作者位置仍为 Q32.32。
- 当前帧字段仅保留原时间尺显示／逐帧定位职责；曲线局部时间编辑的旧帧吸附、动态播放控制和正式 Presentation Marker 图执行仍未完成，未整体勾选0.6或8.3。
- 本次本机8080服务曾停止监听；按现有Unity MCP skill恢复正式HTTP服务，原Editor自行重连。未重启Editor，未添加临时Editor脚本。
- Unity 编译与域重载完成（1789893390596），Editor idle、错误日志为零，git diff --check通过；未新增测试。

## Slate 曲线局部时间使用正式网格
- 可编辑曲线的局部秒坐标经当前 Clip 起点映射到 Timeline 秒坐标后使用会话网格，再映射回局部时间；删除内部60作者帧吸附。关键帧位置未移动时保留原位置，调整值／切线不将其他已有关键帧批量重新吸附。
- 删除曲线时长最少一显示帧与0.0001秒的隐式拉长，删除将附近关键点当同一时刻的阈值；正式短区间按实际时长映射归一化素材曲线。素材浮点无法表达的内部切分明确失败，不将位置夹到别处。
- 以上仍属原 Slate 编辑器、原曲线描述与原 mutation 链；不增加第二预览或求值器。完整运行控制、Marker 表现图、最终分支重接入及全运行链0 GC继续保持未完成。
- Unity编译与域重载完成（1789893614019），Editor idle、错误日志为零，git diff --check通过；未新增测试。

## 动作 Timeline 正式倍率／暂停控制
- 原播放技能Timeline节点增加动作进度倍率与暂停值输入，默认正式值为1／false，可连接既有值图和黑板；OperationValuePortContracts同步声明两个输入，Fixed／Float32按同一端口合同逐逻辑步读取。没有修改Unity全局时间或SimulationTickRate，也不把动作倍率写进Clip素材倍率。
- AbilityTimelinePlaybackControl作为值类型沿原Tick／Advance请求传递。原播放管理者以rate.Raw代替固定OneRaw生成tick区间，保留对正式tick率的有符号余数与nearest-even舍入；暂停保持进度与余数，不积攒暂停时间。控制只随原Advance Commit生效，Discard不泄露，原Capture／Restore同时保存Rate和Paused。
- 暂停候选不遍历Clip生命周期、Logic Marker、ActionCue或Motion输出；未消费的初始边界和待退出请求保留至恢复。表现采样显式给出Paused，动画／Camera消费同一停住的位置，不重复乘倍率；表现起点事件单独随帧接受记账，起步暂停不会提前消耗首次经过资格。
- 进度命令同时携带控制，差异比较包含控制字段。Fixed／Float32状态格式升9、直接runtime快照升v7、权威进度传输相关schema升10；删除旧签名，不兼容读取旧格式。
- 已通过正式菜单 Tools/3C/Internal/Republish Corin Ability Data 调用既有Publisher，从当前正式作者资产重建Attack、DodgeBack、DodgeForward、RushAttack共8份Fixed／Float32派生资产；静态核对8份产物都含m_PlaybackRate、m_Paused及Timeline producer。作者资产原有其他改动保留，派生产物不混入代码提交。
- Unity编译及域重载完成（1789894226854），正式重建后错误日志为零，git diff --check通过；未新增测试。尚未完成：多来源hitstop的叠加与解除来源合同、非Skill调用方控制接入、最终分支重接入、正式Presentation Marker图执行和全运行链0 GC，不据此勾选整个第5节。

## Slate 轨道域修改的图能力检查
- 原节点能力声明增加 TimelineDomains；Gameplay 写入、Timeline 驱动、结束片段、循环、状态机及尚未绑定表现事实的读取节点不允许用于 Presentation。顺序／选择／并行结构、纯比较、Macro 端口、OnEnable 和现有四类 Camera 输出声明可用于表现域；这仅声明图内容资格，不代替运行上下文和资源准备。
- ITimelineTreeGraphAsset 闭包入口显式接收所属轨道域，Marker 内容发现把轨道／Marker 身份传入。闭包检查全部子图节点并返回精确图／节点位置，原 Slate Track Inspector 的候选预检直接消费该结果，失败不提交轨道字段；未新增窗口或第二套 Inspector。
- 当前仍保留缺少正式表现图执行器的准备／编译失败，未借用 Simulation invoker，也未把第6节或8.2整体勾选完成。

## Marker 编译调用身份区分执行域
- Marker occurrence 从其所属 Track 记录执行域；图编译沿用原 Skill compiler 和正式 SourceMap，Presentation Marker 的 OnEnable 入口声明 PresentationMarker 调用归属，图 identity／revision／调用路径仍来自原闭包。Logic Marker 保持既有 TimelineClip 调用表，Fixed／Float32 的逻辑 invoker 不会收集 PresentationMarker 入口。
- 将上一批图能力检查收敛到 BtsmtlSkillGraphClosure.ValidateTimelineGraph，Slate 内容发现与编译 occurrence 共用同一入口，不复制一份编译器节点白名单。通过能力检查的 Presentation 图可生成正式编译内容；运行准备仍明确拒绝尚未安装表现执行器的内容，没有临时 Simulation actor 或借用逻辑 Tick。第6节仍未完成。
- 暂停 Advance 与正常 Advance 共用已提交区间起点检查；暂停请求携带不同目标时间或余数时明确拒绝，防止在暂停分支静默丢掉错误区间。

## 表现图复用原值求值算法
- 从原 Float32ValueRuntime 抽出 Float32GraphValueRuntime：常量、比较、布尔运算、条件结果、Macro 参数准备／输出读取和递归输入租约统一保留一份实现。原 Simulation 值运行时继承共享部分，仅保留输入、角色事实、Gameplay、黑板和诊断访问；没有新建影子值图解释器，也没有创建私有 Simulation actor／frame 供表现侧使用。
- 共享层只接收不可变编译数据、既有布局和专用值缓冲，域读取与 Macro 参数存储通过明确实现接口提供。30类值操作的 case 主体静态对照一致；这不代替用户端到端验证。
- 值递归 HashSet 与输入缓冲按正式操作数／最大输入端口数预分配，由原 Float32 Ability Evaluate Pass 在准备时按 actor／ability 持有并传给原求值链。删除运行中扩充输入缓冲的路径，不把可变暂存放进共享编译产物，不每逻辑步重新分配整套缓冲。其余原 Simulation 输出／上下文及 Timeline 求值分配仍未全部消除。
- 本批是正式表现图执行所需共享部件迁移；表现图 target、Camera 输出候选及帧提交尚未接通，第6节继续保持未完成。
- Unity脚本编译成功、域重载完成（1789896636348）。编译期间两份源文件曾报告 SourceAssetDB 时间戳不一致，日志随后显示两份文件均重新导入并产生正式 artifact；未清除用户控制台。首次代码拆分的语法错误已修复，最终编译没有C#错误。

## Camera 图输出复用原请求构造
- Fixed／Float32 的四类 Camera 图节点改为共用 CameraProgramRequestFactory，继续构造原 PresentationCameraRequest 并交给原 producer／事件／生命周期输出链。共享层只依赖公共合同，数值常量读取分别留在原数值模块；表现图执行上下文可直接复用，无需复制第三套请求解释。
- Camera 字段进入现有 OperationNamedConstant 索引，两个读取器按编译布局读取并在明确类型不匹配时报错；删除这条输出热路径上拼接 /constant/ 字符串并扫描常量引用的操作。原合同允许为空的可选字符串继续表示未指定目标，必填字符串与数值缺失仍失败。
- 四类请求构造参数顺序／值与改前静态对照一致。尚未接入表现 Marker 的执行 target 和帧事务输出，不据此勾选第6节。
- Unity编译与域重载完成（1789897343874），Editor idle，控制台错误为零。未新增测试。

## 表现 Marker 图执行适配器
- 新增 Float32PresentationGraphRuntime，复用 OperationControlRuntime 与 Float32GraphValueRuntime 执行编译后的 OnEnable，不创建 Simulation actor、Ability execution frame，也不调用 Simulation Evaluate／Finalize。准备阶段遍历可达控制／值操作，拒绝 Gameplay 黑板、Loop、Timeline 驱动、状态机及未绑定的读取能力；Macro 仅可读写正式参数槽。
- 绑定同时核对父调用路径、Timeline 节点身份、Marker 身份、图 identity 和 revision。编译器对 PresentationMarker 的 callerId 保留实际 Timeline 节点身份，避免同一图多处调用只能靠图名字匹配；Logic Marker 的旧 hook 调用合同保持原语义。
- Camera 请求在准备时由上一批公共工厂构造，按可达流程预计算候选容量。Evaluate 重置自己的临时值／控制状态，并返回原 typed 请求的只读候选区间；失败清空候选计数。成功调用的新增路径不构造 List／字符串／命令对象，状态槽复位采用索引循环。
- 这批只落地正式图控制器的表现执行适配与候选输出接口；CharacterTimelineHost 尚未装配该适配器，Camera 帧候选接收／提交、只读表现事实节点及资源准备仍待完成。现有 Host 的缺执行器错误保持，第6节不能勾选完成。
- Unity编译与最终域重载完成（1789898726011），Editor idle，控制台错误为零；没有运行图端到端验证，没有新增测试。

## Camera 输出与 Timeline 事件记账进入表现帧候选
- 原 CharacterCameraDomainRuntime 增加 BeginFrame／CommitFrame／DiscardFrame。帧内 Publish／Retire 只修改原请求集合的候选副本并登记已接受请求的退休原因；接受时交给既有 Sequence／Effect 生命周期，丢弃时不触碰已接受状态。原帧流程统一调用这些边界，没有第二 Camera registry。
- TimelinePresentationEventBridge 改为消费原 Prepared 事件，Camera 请求容量失败等调用错误发生在 Pose 接受之前；桥内事件字典也保留预分配帧基线，Discard 恢复原事件记账。复用 alive／retired 容器，删除每帧新建 HashSet／退休列表。没有 Camera 输出的帧不再强求 Camera 调用上下文。
- 修复 RetireInactive 忽略 playback handle、导致不同 Timeline 相互退休 Camera 请求的问题；修正／分支撤销使用原 activation 交给 Camera 的 EventRevoked，正常结束保留自然收尾。Dispose 先清理事件，再标记已释放，修复原来 Reset 被直接跳过的问题。
- Camera Profile 新增正式 RequestCapacity，参与 Revision 和合法性检查；Corin 资产显式配置128。原请求集合、帧候选、事件基线和投影请求缓冲按配置预分配，容量不足明确失败；未配置 Camera domain 的能力容量为0，任何实际 Camera 输出仍明确报缺 domain。
- 正式 Marker 图的 Host 装配仍未接通；原 Camera key／EventId 字符串构造与部分运行分配、Camera最终Apply异常后的跨领域回滚仍未消除，不能据此宣称完整0 GC或整体第7节完成。
- Unity编译及最终域重载完成（1789899749435），Editor idle，控制台错误为零；未新增测试。

## Host 装配正式表现图并提交 Marker Camera 候选
- Fixed Character Host 在原 Definition 能力集加载阶段安装正式 Float32 编译产物中的 PresentationMarker 图；纯表现图不创建 Fixed／Float32 Simulation actor。依赖解析器只接受已安装的表现图，保留源图 revision 一致性检查，删除一律拒绝 Presentation Marker 的阻断。
- CharacterTimelineHost 在原 Prepared 事件中按父图调用路径、Timeline 节点、Marker、graphId／revision 选择唯一已准备执行器，代次不匹配、缺少或重复绑定明确失败。TimelinePresentationEventBridge 调用原图适配器，将 typed Camera 输出写入上一批的原 Camera 帧候选；事件去重仍随原 Timeline 帧接受，Discard 同时恢复 Camera 与桥内记账，不调用逻辑 invoker。
- Camera 的 Sequence／Effect 资源、模式、目标绑定在表现域初始化检查；缺 Camera domain 直接失败。图节点已有 producer 区分输出，Marker 原有 EventId／TraversalIndex 保持一次正常经过的身份。循环重触发替换同一 Marker／producer 的旧请求，修正回到 Marker 之前撤销请求，正常结束沿原 Camera 退休策略收尾。
- 原 Camera 事件字典改用值类型键和事件记录；Marker 键由 handle／generation／Marker／producer 组成，不拼接新的复合字符串，也不新增第二字典。旧 Clip Camera key 字符串及原 Marker EventId 哈希分配仍待清理。表现帧携带共享采样的秒数／cycle，校正清理不读取逻辑中间游标。
- 结束事件、动画／Camera bridge 清理、表现 driver 释放均显式携带 generation，旧代结束不能释放同 handle 的新代。Float32 编译产物需要与作者图一致；本批没有创建或修改 Presentation Marker 业务资产，尚未做端到端图执行验证。
- 仍未整体完成第6／7节：只读表现事实节点未开放，最终 Camera Apply 异常后的跨领域回滚、同代分支重新出现、完整0 GC仍待完成。
- Unity最终编译与域重载完成（1789916115983），原菜单 Tools/3C/Internal/Republish Corin Ability Data 执行后控制台错误为零；git diff --check通过。派生资产保留工作区中其他任务的修改，不混入本批代码提交。

## Presentation Marker 读取同帧角色表现事实
- 原 Character State 的向量／数值／朝向／布尔节点声明允许 Presentation；继续使用既有五个字段标识与编译端口，不添加作者字段表。表现执行器只接受 position、velocity、vertical-velocity、body-yaw、grounded，Gameplay 黑板及其余未绑定读取能力仍拒绝。
- 原表现域先构造本帧 CharacterPresentationFactFrame，再将同帧 TargetPosition、事实速度／朝向／接地适配成只读 Float32PresentationGraphFacts 传给 Timeline Host。Marker 条件读取的是当前表现事实，不创建 Simulation frame，不回写逻辑状态，也不生成第二套事实采样。
- Host 按 renderFrame 核对求值上下文；图执行器每次 Evaluate 接收显式快照并在 finally 清理，原帧 Discard 同时清空 Host 的快照。只读事实为值类型，转换与读取路径无新增集合／装箱。Logic Character State 节点继续由原逻辑执行器读取。
- 仍待完成：全 Timeline 求值与原事件身份分配清理、播放控制多来源叠加、最终分支重接入、下游最终 Apply 异常回滚及完整对账；本批没有新增测试。

## 接续目标与跨循环动画采样
- 本任务继续以同目录 tasks.md 为完成清单，design.md 与 specs/ 为实施合同；本记录只登记改动与证据，不替代清单，不提前勾选未闭合条目。
- 对应 5.3：Timeline 表现求值的动画贡献只采样共享进度的最终秒数和最终 cycle，删除沿所有经过循环收集动画贡献的路径，避免旧循环末尾片段混入当前姿态选择。Camera Cue 继续按经过区间采集，保持跨循环触发语义。
- 本批仅改动 TimelineRuntimePresentationEvaluator，未修改作者资产、既有播放控制及用户工作区其他文件。Unity 脚本构建成功；MCP 在域重载期间暂未重新连接，尚未取得重载后控制台结果。未新增测试。
- 下一步仍需完成表现结果缓冲的明确寿命与复用；多来源播放控制、同代分支恢复和完整运行热路径 0 GC 保持未完成。

## 表现求值复用候选与已接受缓冲
- 对应 0.7／5.3／7.3：原 PresentationPlaybackState 持有两份固定容量结果缓冲。Evaluate 将动画、Camera 与场景参数写入候选；Commit 交换两份缓冲，Discard 仅清空候选，Marker 遍历基线仍在 Commit 才更新。重复帧直接返回原 pending／cached frame。
- 删除每帧七份 List、表现 Operations 堆对象及结果 ReadOnlyCollection／List 副本。Operations 与 SampleView 为值类型；视图按缓冲版本检查，候选丢弃、旧已接受帧被下一次提交替换、playback 释放／换代后访问明确失败，不静默读取下一帧数据。当前桥接消费者按索引同步读取，不保留过期视图。
- 容量由正式轨道 Clip 数、表现 Marker 数及既有单次最多跨4096圈的限制推导；循环 Cue／Marker 保留4097段容量，非循环仅一段，超限明确失败。此实现用预分配内存换取运行时不扩容；循环事件较多时内存占用随内容数量及最大遍历段数增长。
- 首次表现状态创建仍会分配，Marker Identity／EventId、Camera key 及逻辑求值链仍有分配，不能据此声称完整0 GC。多来源控制、同代撤销后重接入和下游 Apply 事务保持未完成。
- Unity脚本构建通过，最终域重载完成（1789917653415），Editor idle，控制台错误为零；本批文件 git diff --check 通过，未新增测试。导入时曾出现 SourceAssetDB 文件时间戳错误，随后原文件成功重导入，最终控制台无错误。

## 撤销记录保留至正式确认边界并允许同代重接入
- 对应 7.2／7.3／7.4：FixedUnityPresentationOutputAdapter.CompleteCommit 将原 confirmedTick 传给原表现域／共享时钟。撤销进度尚在未确认区间时，采样显式携带 RetainForCorrection；确认越过来源进度 tick 后才允许最终释放，不新增墙钟超时或第二注册表。
- Host 在撤销帧接受后仅将既有播放绑定标为 PresentationWithdrawn。动画与 Camera 沿原 Withdrawn 事件退役；driver 清空结果视图，保留游标、初始边界资格和 MarkerLastTraversal。同一撤销重复帧不重复通知下游。丢弃撤销帧不改变 Host 标记或去重基线。
- 同 action／调用／Timeline／generation 的正式进度重新发布时，原时钟将其识别为 Correction，Host 原绑定仍在，因此动画和连续 Camera 可按正式位置重新采样；帧提交后清除撤销标记。已接受 Marker 不因重现而补发或伪造新 TraversalIndex，遵循 design.md 的修正规则。确认后无重现则释放原时钟槽、driver 与 Host 绑定。
- 本批恢复标记在 driver 接受后更新；原跨 Pose／Camera 最终 Apply 异常的整体回滚仍未闭合，Marker 真实输出修订的下游调和与完整0 GC仍需继续对账，不据此勾选整个第7节。多来源倍率组合的相乘／最小值业务规则已向用户提问，尚未写入实现。
- Unity脚本构建及最终域重载完成（1789918181297），Editor idle，控制台错误为零；本批文件 git diff --check通过，未新增测试，未执行端到端验收。

## 诊断读取本次接受结果并删除逐步中间副本
- 对应0.7／5.3／8.4：TimelineRuntimeCommittedEvaluation 直接携带原 Advance.ActiveClipIds，不另存第二份活动集合。活动 Track／Clip 诊断按正式轨道顺序读取该集合，每轨道最多发布一次 TrackActive；删除逐步 HashSet、ClipSamples.Select.ToArray 及额外 playback descriptor 快照和旧 TryFindClip 扫描入口。
- TimelineVisualTime 直接读取原表现帧 Time／Cycle，修正先前显示逻辑游标、与实际表现采样不一致的问题；不再为了两个字段分配含活动集合的播放描述。
- 诊断构造前复用原 ShouldPublish（同时覆盖 live／capture）过滤；TreeClip 事件名使用原枚举对应常量，删除枚举 ToString 分配。ActionCue 无订阅者时不构造无人消费的 committed EventId，但正式订阅者存在时仍按原业务链交付，未删除事件能力。
- 仍未解决：有消费者时的 EventId 哈希分配、Camera 字符串键、逻辑求值集合及最终跨领域事务；本批不代表整个0 GC完成。多来源倍率规则仍等待用户答复。
- Unity脚本构建和最终域重载完成（1789918476732），Editor idle，控制台错误为零；本批文件 git diff --check通过，未新增测试。

## Camera 查询使用正式片段身份并保留 Cue 循环来源
- 对应0.7／5.3／7.3：Camera State／Cue／Response 的运行采样补传原 TrackAuthoringId／ClipAuthoringId，未新增作者配置；与原 Resource 采样统一按 playback handle／generation／轨道／片段查询。删除 CreateKey、params数组、临时List、字符串拼接查询以及 legacy string 隐式转换。Marker 与 Clip 继续共用原 CameraEventKey 字典，保留明确身份字段。
- Cue Sample 传递原 evaluation segment.Cycle，Camera 键、事件身份与输出命令携带该循环编号；相同显示名不再合并不同片段，同帧跨多圈的同一 Cue 不再被一个 key 吞掉。Camera 请求仍进入原候选容量检查，超限在帧接受前失败。
- Cue／Resource 的正式请求ID直接使用 ClipAuthoringId，不再临时拼显示名。首次激活时 EventId 哈希、producer ID 与动画事件身份仍存在分配，连续 Camera 权重更新及最终 Apply 事务仍需继续对账，不将本批记成完整0 GC或完整Camera功能。
- Unity编译和最终域重载完成（1789918771001），Editor idle，控制台错误为零；本批文件 git diff --check通过，未新增测试。

## Camera 已知绑定错误前移至 Pose 提交之前
- 对应6.4／7.3：原 ICameraRigAdapter 增加明确的 ValidateBinding 合同，由 Cinemachine adapter 检查正式主相机、Manual Update Brain 和实际 Shot rig 引用；不通过 UI、不创建替代 rig。Apply 复用该合同，删除缺依赖时仅记录错误并静默返回的 CanApply 路径。
- 原 Camera domain 在候选请求已收集后调用 ValidateFrame，逐项复用 ValidateRequest 检查资源、目标 key 与 Shot 绑定。原表现域在 RunPoseFrame 前执行该检查；错误沿既有 finally 丢弃 Timeline／Camera／桥接候选，Pose 尚未提交。Marker 准备时的 ValidateRequest 也使用同一 Shot 检查。
- Camera SequenceInterruptPolicy／EffectKind 从 int 到 byte 枚举改为明确的合法值映射，删除 Enum.IsDefined 装箱与底层类型不匹配风险；Effect 预检不再通过 byte 强转把越界值截断成合法类型。
- 本批只关闭已知配置／资源错误晚于 Pose 提交的边界。Camera 求值器的可变状态、最终物理 Apply 与 Pose 的跨领域回滚仍未完成，不能据此宣称完整帧事务。下一步沿原求值 owner 的候选／丢弃边界继续实现。
- Unity编译和最终域重载完成（1789919348469），Editor idle，控制台错误为零；本批文件 git diff --check通过，未新增测试。

## Camera 求值状态随候选帧接受与丢弃
- 对应6.3／7.3：沿原 Camera owner 增加值类型状态备份，覆盖 FramePlanner 朝向、SequenceTransition 全部过渡／退休状态、WorldBasic 平滑历史和环境碰撞平滑历史。保留唯一求值算法，不实例化第二份相机运行时；Discard 恢复本帧开始状态。
- CameraEffectEvaluator 原状态池改为按 RequestCapacity 预分配对象与值类型备份。BeginFrame 保存请求、计时、退休、完成记账及贡献；Commit 清除备份，Discard 恢复。活动效果与尚在收尾的效果共同计入容量，超限明确失败；删除 Add 时 new CameraEffectRuntimeState。完成记账只保留仍在正式请求集合中的事件，避免已退休来源永久占用。
- 原表现链分开 PrepareRequests 与 PrepareFrame：Pose 前先校验资源、转换候选请求、处理退休并检查效果容量；Pose 写入后读取同帧骨骼目标并求 Camera 候选计划。相机请求准备失败或 Pose 拒绝时，原 finally 恢复所有 Camera 内部状态，不丢掉待处理的 reset reason。
- Camera 内部计划 Apply 成功后才接受 Camera 状态，随后接受 Timeline 与桥接记账；删除原“先接受 Timeline、最后才求值 Camera”的顺序。Camera 目标依赖本帧骨骼，未把它错误前移为读取上一帧 Transform。
- 仍未闭合跨领域物理事务：Pose 当前仍在 Camera 实际求值前提交；Camera Apply 部分写入失败，以及 Camera 接受后 Timeline 提交失败，需要接入原 Native Final Pose 发布边界共同处理。本批只完成 Camera 内部候选状态，不宣称完整外部回滚。首次身份分配、其他逻辑求值分配及多来源控制仍未完成。
- 编译途中另一份正在修改的 CharacterPoseNativeBlendStackHandler.cs 出现 CS8156；该文件随后由原有改动修正为局部变量传 in，本任务未修改它，已发起当前代码的重新编译。
- 当前代码重新编译成功，最终域重载完成（1789920360427），Editor idle，控制台错误为零；本批文件 git diff --check通过，未新增测试，未做异常注入或端到端验收。

## 按用户明确边界收回相机历史回滚
- 本节取代以上记录中把 Camera 求值状态恢复和跨 Pose／Camera 最终物理回滚列为后续必做项的判断；该扩展不符合业务范围，不再作为未完成事项。
- 删除 FramePlanner、SequenceTransition、WorldBasic 和环境碰撞求值器的历史备份，以及 CameraEffectEvaluator 的帧备份／恢复。相机保留当前连续状态，在正式请求接受后读取本帧目标并正常求值。
- 保留原候选请求 BeginFrame／CommitFrame／DiscardFrame 和 Pose 前资源／Rig 校验；未接受 Marker 不消耗交付资格，已接受事件撤销按原镜头退休规则收尾。没有增加 Pose 物理恢复代码。
- 保留效果状态对象池、容量约束与已结束事件清理，避免每次激活分配状态对象；不把这些内存改进解释为相机回滚能力。design.md、tasks.md 和 character-animation-pipeline delta 已同步明确该边界。
- 删除仅为候选回滚拆出的 PrepareRequests／EvaluatePrepared 接口，效果请求处理与求值统一回 Resolve。Unity 日志已报告脚本构建成功；共享工作区后续修改再次触发域重载，提交时尚未取得最终 idle／控制台结果。git diff --check 通过，未新增测试。

## 逻辑推进复用工作列表与排序委托
- 对应0.7：原 playback 按正式 Clip 数预分配活动片段、待退出、已退出及求值工作列表；Advance 复用活动列表、退出列表和边界列表，不再逐 tick 创建这些临时集合。结果仍保留原独立副本寿命，下游尚未迁移的结果消费者不会观察到下一次工作区写入。
- 边界容量由有效 Logic Clip 数、每片段最多两个边界及既有单次最多4096圈推导；循环内容用较大的准备期内存换取推进时不扩容。排序继续比较绝对位置、Enter／Exit 和作者身份，委托只在 playback 初始化创建一次。退出查询改为索引遍历，边界诊断改用固定文字。
- AdvanceResult、EvaluationResult 及其输出集合副本仍有分配，本批不表示完整0 GC。Unity编译与域重载完成（1789921437488），Editor idle，控制台错误为零；同时补齐上一批相机边界修正的重载后检查。git diff --check通过，未新增测试。

## 连续 Camera 片段更新当前请求而不重启效果
- 对应5.3／7.3：Camera State、Response、Resource 每帧将正式共享位置得到的权重与参数发布到同一请求；删除只在首次出现时发布的门槛。既有 CameraEventKey 继续索引同一 EventId／producer，连续更新不重新计算哈希或拼接身份。Camera Cue 保留经过触发规则。
- 原 Camera domain 的 Publish 对相同来源请求、EventId、generation、资源及效果类型直接替换请求参数，保留候选的 AcceptedIndex，不发送退休，不重置效果计时；身份或资源变化仍走原退休／激活链。丢弃帧继续丢弃请求候选和桥内记账，不恢复相机历史。
- 发现并修正资源枚举值错配：Timeline 的 Shot=4，Camera 的 Shake=4、Shot=5，原强转会把 Shot 解释为 Shake。改为四种资源的显式对应，同时删除 Enum.IsDefined 的装箱。
- Unity脚本构建成功，git diff --check通过；共享工作区其他脚本仍在导入，MCP暂未重新连接，本批提交时未取得重载后的最终控制台检查。未新增测试，权重曲线与Shot资源的实际画面仍待用户端到端验收。

## 逻辑求值结果改用候选与已提交缓冲
- 对应0.7：原 playback 持有两份按内容预分配的 EvaluationStorage。Evaluate 直接写入13种输出缓冲，EvaluationResult 改为带版本的值类型视图；删除逐 tick 的13个List、结果ReadOnlyCollection／List副本、结果堆对象，以及没有消费者的 LogicOperations 重复包装。
- Commit 交换候选与已提交缓冲并使旧提交视图失效；Discard 不覆盖旧提交，候选仍供既有 Discard 回调读取，到下次 Advance 开始才清空失效。暂停提交使用本 playback 的空缓冲，CompleteStop／Dispose 清空两份结果。TryGetCommitted 检查结果版本，释放／换代后不会返回旧播放的失效视图；直接持有过期视图的调用会明确失败。结果不是可无限持有的历史快照。
- 容量按各类正式 Clip／Marker 数、既有4096圈上限及每片段的Enter／Exit／Update上限计算；超限明确失败。循环内容会占用较大的初始化内存，此处不引入运行时扩容。场景参数求值补齐轨道静音和Logic域过滤，与其他逻辑轨道保持一致。
- ActionCue和TreeClip诊断消费者改为直接索引值类型视图，不通过IReadOnlyList装箱。AdvanceResult及边界副本、调用身份哈希等仍有分配，完整0 GC仍未完成；本批未新增测试。
- Unity脚本编译通过，最终域重载完成（1789922044951），Editor idle，控制台错误为零；git diff --check通过。同时补齐上一批连续Camera请求更新／Shot映射的重载后检查；未做运行内存采样，不宣称已实测全链路0 GC。

## 推进结果值类型与边界缓冲统一
- 对应0.7：AdvanceResult 改为值类型，活动片段和边界直接引用原候选／已提交结果缓冲，删除推进结果堆对象、两组ReadOnlyCollection／List副本以及上一批单独的活动／边界工作列表。边界原地排序使用初始化时创建的比较器；消费者按具体值类型视图索引，未增加接口装箱。
- Commit／Discard 用 playback 引用与递增推进序号校验候选，丢弃后在同一逻辑tick重算也获得新序号，旧结果不能接受或丢弃新候选。推进序号只用于本机事务身份，不作为逻辑时间或存档状态。暂停复制当前活动身份到空候选结果，仍不遍历内容。
- 本批编译先发现跨程序集可见性和三个原引用类型判断，已迁移为明确的IsValid合同。CharacterTimelinePendingAdvance包装和运行事件身份仍有分配，完整0 GC仍未完成。
- Host 的 Commit／Discard 原先先按handle删除pending再调用底层；现先核对字典中的pending对象就是传入对象，避免同handle旧请求移除新候选。此处仍使用原pending注册表，不增加并行状态表。
- Unity编译与最终域重载完成（1789922436006），Editor idle，控制台错误为零；git diff --check通过，未新增测试，未进行运行分配采样。

## 删除没有消费者的TreeClip与Logic Marker调用字符串
- 对应0.7：全Assets C#引用核对显示，两种请求的CallId只在构造时赋值，没有读取方。正式invoker使用原playback handle、Clip／Marker身份、cycle和Action实例身份。删除冗余CallId字段、参数及两处拼接函数，持续TreeClip Update不再为无人使用的字段逐tick生成字符串；没有引入身份缓存或另一套调用身份。
- Unity脚本构建已成功，域重载后检查待本轮后续补齐；git diff --check通过。其他实际使用的EventId哈希和pending包装分配仍需处理，未新增测试。

## 推进pending跨域传递改用值类型凭据
- 对应0.7／7.3：删除IAbilityTimelinePending／IAbilityTimelineAdvancePending接口；Simulation正式合同改为AbilityTimelineAdvancePending值类型，包含runtime handle、候选序号和进度。Fixed／Float32执行工作区、提交／丢弃、Unity adapter全链消费同一具体类型，不把struct装箱回接口。
- CharacterTimelinePendingAdvance改为值类型，实际AdvanceResult留在原Host pending注册表；跨域只传正式凭据，不向Simulation泄露BTSMTL结果。原每tick pending包装对象与接口引用分配已删除，终态返回无候选的默认凭据。
- 候选序号使用进程内原子递增值，跨Host、Reset和逻辑Restore不复用；它仅是本机事务身份，不参与确定性快照或网络进度。Commit／Discard必须命中原注册表的handle与序号才移除候选，旧凭据不能消耗新记录。停止pending仍按原合同，后续继续清理，未新增测试。
- Unity编译与最终域重载完成（1789922983366），Editor idle，控制台错误为零；git diff --check通过。上一批冗余调用字符串删除同时通过重载后检查；未新增测试，未进行运行分配采样。

## 停止pending值类型化并拒绝旧停止凭据
- 对应0.7／7.1／7.3：删除IAbilityTimelineStopPending，原停止合同使用具体AbilityTimelineStopPending值类型；Fixed／Float32工作区、状态切换、停止提交／丢弃及Unity adapter统一迁移。CharacterTimelinePendingStop成为保存在原注册表中的值类型，不再为每次停止创建接口包装对象。
- 推进和停止共用Host的进程内原子候选序号来源；停止接口仍保持独立的类型与入口。Host先核对handle与序号，旧停止请求不能删掉新停止候选，取消播放时提交原候选的正式凭据。停止进度允许沿原合同为空，不额外合成未确认终态。
- 两套CharacterEvaluation结果的Copy仅接收值类型，删除对struct逐项判null的无效循环；集合副本及运行快照的分配尚未在此批清理。未新增测试；多来源控制的跨动作作用范围仍待用户明确，未据角色级GameplayEffect存储擅自决定业务行为。
- 用户补充可由Timeline Clip退出回调解除控制。现有正式链正常退出走OnDisable、Timeline停止走OnDestroy；后续沿这两个出口做同一来源清理，不把角色级存储误当成必须跨动作生效。定时hitstop到期仍应使用逻辑tick，不能依赖被冻结的片段位置。
- Unity编译与最终域重载完成（1789923462392），Editor idle，控制台错误为零；git diff --check通过，未新增测试。

## 多来源控制的现有能力与缺口核对
- 现有GameplayEffect按逻辑tick推进持续时间；属性Multiplicative修饰符按乘积聚合，标签由活跃效果持有。Timeline播放节点已经读取倍率和暂停值输入，因此不应在Timeline另建倍率／暂停计时服务。Clip正常退出OnDisable和停止OnDestroy可调用同一来源清理。
- 尚缺作者图可用的运行时效果句柄链：Apply操作拿到GameplayEffectApplyResult后只返回Succeeded，作者节点只暴露Applied；Remove的Handle来自静态编译常量。相同效果的多个独立实例无法由各自Clip保存并动态移除。后续需沿正式UInt64值端口与原状态槽补齐Apply句柄输出、Remove句柄输入和回调清理，不能用EffectId／SourceActor批量移除冒充独立来源解除。
- 0.7另清除一处TreeClip／Marker图查询分配：直接按既有dependency identity的前缀及作者身份作序号字符串比较，返回闭包中的原身份，删除每次求值重新拼接tree:身份。图revision校验继续沿原闭包，不增加缓存。
- Unity编译及最终域重载完成（1789923865117），Editor idle，控制台错误为零；git diff --check通过，未新增测试。多来源效果句柄接入尚未实现，不据现有聚合能力勾选5.6。

## GameplayEffect运行时句柄进入正式值端口
- 对应5.6／7.4：施加节点新增UInt64效果句柄输出；正式编译为该Apply操作声明GameplayEffectAppliedHandle状态槽，沿原Control状态端口写入并随原快照链保存恢复。Fixed／Float32都在成功时保存实际句柄，失败保存0；Applied输出读取原操作成功状态，不重新执行施加。
- 移除节点的Handle改为UInt64值输入，删除原静态Handle作者字段、Inspector输入、authoring配置参数及具名编译常量。两套控制运行时从正式值输入读取句柄，移除操作仍调用原GameplayEffect.Remove；Removed输出读取操作状态。没有新增时间控制服务、效果表或动态／静态双路径。
- 作者在OnEnable施加效果并将句柄保存到合适生命周期的正式黑板变量，在OnDisable与OnDestroy共用移除逻辑。独立来源需要配置独立效果实例；原合并堆叠策略仍共享实例句柄，不能承诺独立移除一个共享实例里的来源。当前Configs资产未发现使用旧Remove节点的内容。
- 时序沿正式操作顺序：Timeline读取倍率／暂停输入前已施加的控制可用于本次推进；在该Timeline求值内部回调才施加的控制影响后续推进，不倒改已经求值的区间。效果到期仍由逻辑tick驱动，暂停动作位置不冻结到期。
- 尚需继续闭合作者调用与多来源业务配置、TreeClip退出记账丢弃边界，以及完整0 GC；本批接口接通不等于整个5.6完成。
- 脚本编译和最终域重载完成（1789924550476）；正式菜单Tools/3C/Internal/Republish Corin Ability Data执行后控制台错误为零。已核对当前8份Fixed／Float32能力产物没有GameplayEffectAppliedHandle状态槽，说明现有Corin内容没有覆盖新增Apply节点端口；本次仅证明代码和现有内容重建通过，不宣称新句柄用法已端到端验收。派生资产及编辑器随保存产生的其他变化留在工作区，不混入本批提交；未新增测试。

## TreeClip候选退出不再修改已提交活动登记
- 对应7.3／7.4：原服务在Consume阶段直接删除Exit片段，而Discard只撤销Enter，导致被丢弃的退出仍永久丢失OnDestroy清理对象。现将Enter／Exit活动登记统一推迟到Commit；Consume只执行正式图回调，Discard不再改已提交登记。图中逻辑写入仍由原Simulation候选／快照链处理。
- 同帧Update先检查本次请求序列里同Clip／cycle的最后一个Enter／Exit，再读取已提交活动登记，保持新进入可Update、同帧已退出不可Update的规则，不新增候选活动表或状态备份。停止只遍历已提交活动片段。
- ActiveTreeClip改为值类型，删除逐次实例对象及RemoveAll捕获闭包；运行中的空列表保留复用，正常完成或正式停止后释放注册。空活动列表停止无需invoker。Step Commit同时移除Marker消费计数，避免已结束播放残留记账。
- 本批关闭候选丢弃边界；完整逻辑Restore后的活动登记同步仍需继续核对，首次注册分配及循环TreeDecision长期活动实例容量也未据此宣称0 GC。未新增测试。
- Unity编译与最终域重载完成（1789925037599），Editor idle，控制台错误为零；git diff --check通过，未新增测试，未进行候选失败注入或运行分配采样。

## 活动TreeClip调用登记进入原Timeline快照链
- 对应7.2／7.4：AbilityTimelineRuntimeSnapshot新增已进入尚未退出的TreeClip调用记录，保存Clip身份、图身份和cycle。原Host捕获时从同一TreeClip服务读取登记，正式Snapshot持有只读副本；Restore先恢复原Timeline，再替换同handle的登记，包含恢复为空，且在复用已有ActivePlayback的提前返回之前完成。不会为恢复重放OnEnable，也不沿用未来分支的活动列表。
- 删除Host内重复的ActiveTreeClip数据类型，服务与Snapshot使用同一AbilityTimelineTreeClipState。停止继续按恢复后的记录调用OnDestroy；上一批Commit／Discard行为保持不变。Host释放时同时释放该服务引用，不保留影子注册表。
- Fixed／Float32快照编码版本同步从9升为10，codec及hash身份从/7升为/8；记录参与原状态序列化与哈希，不保留旧格式读取。解码数量按剩余载荷的最小记录尺寸约束，不引入任意活动调用上限。空记录共用空数组。
- 这是逻辑调用登记恢复，不是表现／相机历史回滚。现有Snapshot自身及非空集合复制仍有分配，完整快照与运行热路径0 GC仍需继续清理；本批未新增测试。
- Unity编译与最终域重载完成（1789925574542），Editor idle，控制台错误为零；git diff --check通过。未新增测试，未运行旧快照兼容或异常注入测试，旧格式按新版本明确拒绝。

## 动画生产者身份校验不再逐帧拼接字符串
- 对应0.7：ActionAnimationPlaybackCommand与播放快照的IsValid此前读取AnimationProducerId.ProgramProducerIdentity，每次校验都会构造producer:Timeline:Track字符串。现由AnimationProducerId直接按长度、固定前缀、分隔符和两段Ordinal字符串比较校验既有ProgramProducerId，保持原身份格式与相等语义，不增加缓存或第二身份表。生产者首次建立时仍使用原格式化入口。
- 本轮核对确认，轨道Sample的foreach遍历具体List，不是接口枚举器分配；PlaybackChanged无订阅者时也不会构造描述对象，未为这些误判改动已有链路。
- 剩余明确分配包括Timeline表现动画每帧EventId构造（数字ToString、params数组、StableHash字符串哈希链）、实际Marker身份，以及Snapshot对象／集合复制和播放建立路径。当前EventId基于字符串StableHash，不能仅靠此次校验清理宣称完整0 GC；未新增测试。
- Unity编译及最终域重载完成（1789926058403），Editor idle，控制台错误为零；git diff --check通过，未新增测试，未进行运行内存采样。

## 连续表现动画采样使用播放身份与表现帧号
- 对应0.7／5.3：删除ActionProjectedSample的EventId字段及TimelineToActionCommandBridge每帧sample哈希构造，采样携带原逻辑tick、表现帧号、已映射素材时间和权重。收件箱内ProjectedSample以AnimationPlaybackId（含generation）和PresentationFrame去重，仍走同一Publish、候选读取、Commit／Discard链，不新增队列或身份缓存。
- Select／Complete／Release／Withdraw及committed Sample继续使用正式EventId。ProjectedSample命令明确不携带事件身份；按EventId的Replace／Retire只处理正式事件，拒绝连续表现采样。表现帧丢弃继续由既有发布序号截断处理；相同播放、相同帧的重复待交付采样明确报错。
- 生命周期Registry保留最近真实事件身份，不让无事件身份的连续采样覆盖它；ProjectSample沿同一Registry读取该来源身份与已投影时间。采样仍不写committed history，不推进逻辑，不改变Camera历史边界。删除PublishSample已无用途的handle参数。
- 对照现行spec，稳定EventId要求仍适用于正式事件及Simulation输出；此次删除的是PresentationFrame内的连续求值记录所重复构造的事件，不改变Marker或分支事件调和合同。未新增测试；实际Marker／生命周期事件哈希、Snapshot副本及首次播放建立分配仍在剩余范围，未宣称整条热路径0 GC。
- Unity编译与最终域重载完成（1789926783028），Editor idle，控制台错误为零；未新增测试，未进行运行内存采样或帧失败注入。

## 效果句柄的Tree节点与黑板作者链补齐
- 对应5.6：进一步核对发现，先前Apply／Remove动态句柄只接入Skill Flow节点，Tree图的ApplyGameplayEffectNode没有Handle输出，RemoveGameplayEffectNode仍保留静态ulong字段。现统一为同名m_Handle UInt64输出／输入端口，删除Remove的静态字段、属性及ConfigureAuthoring参数，继续使用已存在的操作值端口和GameplayEffectAppliedHandle状态槽。
- 原EquipmentUInt64PropertyPort移为独立共享UInt64PropertyPort，装备节点与效果节点使用同一端口类型；当前Assets内未发现旧类型的序列化引用或Apply／Remove节点实例，不保留旧类兼容入口。
- 另发现正式黑板作者声明、读写节点类型及编译映射未接受ulong，先前“保存到黑板再退出解除”的描述因此不完整。现补齐声明校验、编辑器新增类型菜单、拖拽生成读写节点的类型映射、节点valueType作者合同和语义编译的UInt64映射。枚举在末尾追加UInt64，已有值不变；状态槽、字面量、运行读写和快照继续使用原UInt64合同，不转为浮点数。
- 作者可在进入回调将Apply返回句柄写入适当生命周期的UInt64黑板声明，退出／停止回调读取同一声明移除自己的独立实例。效果合并堆叠仍共用句柄，不能用此用法区分合并后的来源。hitstop触发动作、持续时间及倍率作用内容已询问用户；未擅自修改Corin战斗配置。
- 最终Unity编译及域重载完成（1789927240467），Editor idle、控制台零错误；新增端口文件首次脚本刷新未导入导致的CS0246已通过正式全资源刷新解决。未新增测试，也未配置新效果图做端到端运行，不能将本批编译成功等同于多来源手感验收。

## 停止候选身份与未使用副本清理
- 对应0.7／7.1：TimelineRuntimeStopRequest原先每次停止都复制活动Clip列表并包一层ReadOnlyCollection，当前所有停止消费者均未读取这份副本。删除字段与属性，TreeClip停止清理仍读取原服务已提交活动登记，不增加另一份清理来源。
- 底层TimelineRuntimeService原先仅按传入handle获取播放并检查HasPendingStop，没有核对传入request是否属于该播放及当前停止候选。现每次成功RequestStop分配播放内单调序号，请求保存该序号；Commit／Discard在变更状态和调用消费者前核对播放对象与待停止序号。已丢弃请求、其他播放请求、同handle重建前的旧对象请求均不能消费新候选。
- 序号是本地候选身份，不加入确定性内容或恢复快照；与既有Advance候选序号同样在运行对象存活期间单调递增。Host现有外层pending令牌保留各自调用边界，未增加注册表。
- 检查OnDestroy调用返回值：未配置可选回调时Invoker返回false，实际图执行Failure抛异常；因此未把可选回调缺失改成停止失败。异常仍进入原DiscardStop及Simulation候选丢弃链，不承诺任意外部副作用回滚。
- Unity编译与最终域重载完成（1789927482420），Editor idle、控制台零错误，git diff --check通过。未新增测试，未进行陈旧停止请求注入或运行内存采样，完整热路径0 GC仍未闭合。

## TreeClip回调查找不再构造组合字符串
- 对应0.7：Fixed／Float32的InvokeTreeClip此前每次查找都调用hook.ToString("G")并拼接ClipAuthoringId与分隔符。两条链改用同一字段组成的ValueTuple<string,int>作为原字典键，初始化和运行查询一致；编号直接来自AbilityTreeClipHook，字符串仍按Ordinal相等，不生成临时字符串或装箱枚举。
- 删除TreeClipKey格式化函数，不新增缓存或第二查找表。正式SourceMap继续保留原调用身份，初始化时解析Hook；缺OnEnable报错、缺可选钩子返回false及实际执行Failure抛异常的语义保持原样。首次创建字典仍有分配，本批仅关闭回调查找的逐次分配，不代表完整0 GC。
- 下一项待核对：分支撤销／修正使Marker相机输出退役后，同generation重现是否能够从原事件记账恢复请求，且不重复执行已交付Marker图；不能因为动画播放已恢复就推定Marker输出也恢复。
- Unity编译与最终域重载完成（1789927645501），Editor idle、控制台零错误，git diff --check通过；未新增测试，未进行运行分配采样。

## 同generation恢复时重接Marker持续相机请求
- 对应7.2／7.4：原桥在撤销时直接删除Marker输出，而原Marker游标保留去重，恢复后既没有旧请求也不会重执行图。现沿原桥接表增加Suspended状态，表改名m_Events以准确表示已发与暂时撤销的记录；不增加第二事件表。原帧baseline包含该状态，失败帧沿原Discard恢复。
- Sequence／Response／Target请求在尚可修正的撤销或修正回退时，先按原相机owner退役，再保留原typed输出及EventId。后续同handle／generation的采样位置到达该记录，重发同一Activate请求并解除Suspended，不调用Marker图、不分配新TraversalIndex。Camera Effect输出撤销后删除，不重播震屏等效果，不保存或恢复相机平滑、碰撞、混合与效果计时。
- Host内部PresentationPlaybackEndPrepared携带原RetainForCorrection，并在已经暂时撤销的播放最终确定释放时仍通知原消费者清理。动画桥因播放已移除而不重复发终态；相机桥释放Suspended记录且不重复退役已撤销请求。Reset与正常完成不保留记录。
- 保留记录仍受原requestCapacity硬上限约束，容量不足明确失败，不增长新历史缓存。此批只恢复原桥已保留的最近输出：同一Marker较新循环会覆盖较旧循环记录，跨循环且输出分支变化的修正仍需继续核对，不据此勾选完整7.2／7.4。
- Unity编译与最终域重载完成（1789927908641），Editor idle、控制台零错误；未新增测试，未进行撤销恢复、跨循环或帧失败注入的端到端运行。代码及当前编译证据不等于这些业务边界均已验收。
