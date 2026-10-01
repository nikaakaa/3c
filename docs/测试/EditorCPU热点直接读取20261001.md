# Editor CPU 热点直接读取

本轮按用户纠正，编辑器允许必要的 GC，优先处理大的计算热点；Player 运行期 0 GC 目标继续保留。没有构建 Player、切换 Play 或保存场景，没有新增或修改测试代码。角色哈希修正由目标 Editor 的现有脚本导入和编译流程加载。

目标实例为 `e852139597e42532`，返回项目路径为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets`，Profiler 连接标识为 `Editor`，Deep Profile 关闭。先读取并保存停止录制后仍保留的 18778–19077 共 300 帧。读取过程中目标 Editor 自行进入、退出 Play；每次返回的状态单独记录，未将读取时的 attachment 或 Graph 状态追认为全部历史帧的状态。

原始帧：[18778–19077](../../.performance-build/reports/20261001-editor-retained-18778-19077.raw)。全部主线程统计：[分析 JSON](../../.performance-build/reports/20261001-editor-retained-18778-19077-analysis.json)。300 帧中有 299 帧包含角色 Update/LateUpdate；第 18864 帧为 MCP 请求本身，耗时 1723.71 ms。再排除含恢复 JIT 的 18865 帧和关闭 Profiler 窗口的 19077 帧后，297 个常规运行帧的中位耗时为 16.89 ms、P95 为 34.98 ms，22 帧超过 33.33 ms。这是同一记录中不同输入、窗口状态的分布，不是修改前后 A/B。

第 18812 帧主线程为 41.66 ms，Update 阶段为 16.82 ms、LateUpdate 阶段为 14.58 ms、GameView.Paint 为 5.30 ms。`MainBehaviour.Update` 本体为 16.76 ms，但该记录没有内部 CPU 标记，暂不能将其归因到哈希、技能、Timeline 或某个具体函数。LateUpdate 中的 Pose 阶段可以继续拆分：

| 阶段 | 两次调用合计 |
| --- | ---: |
| CharacterPose.EvaluateNodes | 7.96 ms |
| CharacterPose.FootPlacement | 3.33 ms |
| CharacterPose.FullBodyIK | 2.06 ms |
| CharacterPose.PrepareSources | 2.84 ms |
| CharacterPose.SampleFootFeatures | 1.63 ms |
| CharacterPose.EvaluateAnimation | 1.06 ms |
| CharacterPose.ComponentToLocal | 0.68 ms |
| CharacterPose.LocalToComponent | 0.41 ms |
| CharacterPose.Commit | 0.81 ms |
| CharacterPose.ValidatePending | 0.096 ms |

FootPlacement、FullBodyIK 和空间转换是 EvaluateNodes 的子项；SampleFootFeatures 是 PrepareSources 的子项，不能和父阶段重复相加。两次 FootPlacement 一共执行 276 次 RaycastNonAlloc、8 次 SphereCastNonAlloc，原生检测合计约 0.386 ms，FootPlacement 的主要耗时仍在托管计算。[该帧的 Pose 调用树](../../.performance-build/reports/20261001-editor-frame-18812-pose-tree.json)保存了具体子调用和次数。

历史帧没有记录分配调用栈，GetSampleCallstack 返回空列表。后续短时开启调用栈后读取的 35709–36008 帧没有角色 Update/LateUpdate，读取时 `isPlaying=false`、RuntimeDebug 为 Detached、Canvas 没有具体 Graph；它不能用于运行卡顿或桥接开启后的性能结论。[原始帧](../../.performance-build/reports/20261001-editor-current-35709-36008.raw)及[统计](../../.performance-build/reports/20261001-editor-current-35709-36008-analysis.json)保留这一失败采样。采集结束已关闭本轮开启的两个 Profiler 录制开关、CPU area 和分配调用栈。

随后目标 Editor 重新进入 Play，直接采集 `GameplayLabFixed` 的 3349–3648 共 300 帧，Deep Profile 和分配调用栈关闭。读取时仍在 Play，RuntimeDebug 为 Detached，Canvas 没有具体 Graph。CPU 主线程耗时中位数为 17.27 ms、P95 为 26.22 ms；LateUpdate 平均 9.87 ms，FootPlacement 平均 2.70 ms、其中独占 2.56 ms，FullBodyIK 平均 1.34 ms，PrepareSources 平均 1.77 ms、其中 SampleFootFeatures 为 0.88 ms，GameView.Paint 平均 3.97 ms。MainBehaviour.Update 平均 1.28 ms、单次最大 8.37 ms。当前可细分的持续计算热点首先是 FootPlacement，其次是 FullBodyIK 和来源准备；输入不同，不能把旧、新采集的数值差异解释为优化收益。

第 3640 帧主线程达到 132.44 ms，其中 Application.Message 为 100.87 ms；该帧包含 GameView 的多个 KeyDown/Character 事件，原因仍需进一步定位，不能直接归因为 GC 或角色算法。原始帧为 [当前 Play CPU](../../.performance-build/reports/20261001-editor-current-3349-3648.raw)，完整统计为 [当前 Play 分析](../../.performance-build/reports/20261001-editor-current-3349-3648-analysis.json)。再通过 FrameDataView.frameStartTimeMs 读取 297 个相邻帧间隔，中位数为 17.275 ms、P95 为 26.224 ms，记录见 [帧开始间隔](../../.performance-build/reports/20261001-editor-current-3349-3648-frame-intervals.json)。这段输入和窗口状态未复现持续低于 30 FPS，仍有超过 100 ms 的异常帧；它不能用于证明 Graph/Timeline 开启后的表现。

源码核对还发现角色哈希 writer 在整个 roster 开始时仅清空一次：第二个角色会连同第一个角色的完整编码一起求哈希，结果与独立状态快照的角色哈希不一致。对于编码长度相同的 N 个角色，哈希扫描量由 N 份编码变成 1+2+…+N 份。当前 Fixed / Float32 的 ComputeHash(state, writer) 在生成单个角色哈希的入口清空 writer，Finalize 删除批次级清空；复用同一缓冲区，不新建 writer。修改前确认目标 Editor 非 Play、非编译；修改后目标 Editor 已编译并加载两条 Reset → WriteString → WriteCanonical → ComputeHash 调用链，scriptCompilationFailed=false。没有新增测试代码，没有进行运行同输入 A/B，实际耗时收益未验证。

FootPlacement、FullBodyIK 和来源准备的算法尚未修改，不能声称 FPS 已改善。下一步处理这些大的计算热点，并细分 MainBehaviour.Update 的真实运行调用及 Application.Message 的异常帧；这些改动应由对应 Editor CPU 阶段实测确认。完整 Player 0 GC 也未证明。

后续在原调用边界加入 CPU 子标记。FootPlacement 拆为 CurrentSupport、LandingPrediction、GroundPath、BodyTrajectory、Lifecycle、Completion、SoleSupport，统一前缀为 `CharacterPose.FootPlacement.`。FullBodyIK 拆为 BindPose、ApplyGoals、Solve、Diagnostics，统一前缀为 `CharacterPose.FullBodyIK.`；其中 Diagnostics 只包围实际开启诊断后的数据生成。`MainBehaviour.Update → GameplayTickBootstrap.FrameUpdate` 拆为 `GameplayTick.FrameUpdate`、`GameplayTick.Hotkeys`、`GameplayTick.Input`、`GameplayTick.Logic`，Logic 按实际逻辑 Tick 调用计数，用于区分单 Tick 计算和一帧推进多个 Tick。标记直接读取 Editor Profiler，沿已有静态 ProfilerMarker 模式实现，没有新增逐帧日志、闭包、结果缓存或执行路径，没有改变支撑查询、状态推进、曲线采样与 IK 算法。

本次核对同一目标实例，项目路径正确、非 Play、非编译、scriptCompilationFailed=false，保留帧仍是 3351–3650，两个录制开关均关闭。该帧集早于新增子标记，没有重读旧帧制造新结论，没有擅自进入 Play、构建 Player 或新增测试代码。本次持有和释放一次 AssetDatabase 自动刷新禁用，计数已配对；向目标 Editor 请求一次脚本编译后，确认编译完成、scriptCompilationFailed=false、Console 错误查询为空，六个类型的十五个静态 ProfilerMarker 字段均已加载。新增标记的实际运行帧、各子阶段排序和性能改善仍未验证；需要下一轮实际 Play 采样，尤其是具体 Graph/Timeline 活跃及自动来源切换时的帧。
