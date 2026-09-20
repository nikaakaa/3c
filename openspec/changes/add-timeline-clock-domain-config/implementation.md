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
