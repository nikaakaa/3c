# 2026-09-29 当前版本性能采集

状态：已完成一次当前版本 Span Capture，并完成热点初读。失败证据保留，不复用旧包数据冒充当前版本；本轮没有选择 A/B 基线，不给出优化收益结论。

## 场景与目标

- Windows x64、Unity 2022.3.62f2c1、IL2CPP Development、Span 模式。
- GameplayLabFixed，character.fixed-local，fixed-player 与 fixed-target 两角色。
- 固定输入：757f243033414fc7b123c97e2fcb0d70；180 预热 Tick，2536 采集 Tick。
- Scenario：performance.757f243033414fc7b123c97e2fcb0d70.fixed.r5，1920×1080，quality 2；配置 target_frame_rate=-1、v_sync_count=0。实际生效值待本次 Player 日志核对。
- 本轮目标一次正式 Capture，Smoke/Replay 是启动和回放检查，不计为性能采集。
- 新增指标改变探针身份，不与旧包进行绕过门禁的正式 A/B，不从单次运行宣称稳定优化收益。

## 构建与阻塞记录

首个作业 b09deda725b24b49b0ef54d8de6667f1 成功发布 Player 7ba79d4e1c0882bd51247afb，总计 631324 ms。Editor 日志的阶段计时：

| 阶段 | 毫秒 |
| --- | ---: |
| 资源来源指纹 | 1100 |
| 内置资源构建 | 64402 |
| Player 输入快照 | 1427 |
| Unity 构建 | 427447 |
| 输入核对 | 1251 |
| 产物整理 | 353 |
| 产物哈希 | 135317 |

这些是完整发布流程的耗时，不全是 Unity 编译。相同 Player 重复采集可复用，无须重复构建。GameAssembly.pdb 为 1349398528 字节，未将整个哈希阶段耗时归给单个文件。

该 Player 的 instrumentation-manifest.json 已确认新增的 9 个指标、12 个方法入口实际织入，包括 Fixed/Float32 角色评估、GE 推进、世界快照，及 EventGraph、Timeline 采样/提交、Camera、装备和 KCC 批求解。织入确认不等于已有调用样本。

Smoke 作业 22fa281359dc480eb4da9e81048b5cf6 失败。证据目录：`3cDemo/Client/3C_Client/Library/Performance/Gates/smoke/smoke.20260929-081951.662a18445d84447097efe5d6ed80ac6d/`。runtime-result.json 为 Faulted / waiting-for-runtime，0 表现帧、0 Tick；具体错误为 Camera Stretch Asset 'Corin_Attack_Branch_01_CamStretch_01' is incomplete。transport closed 只是 Player 初始化失败后的连接关闭。

源码 CameraStretchAsset 已要求 character-camera-stretch/v2，但 18 项正式资产仍为 v1，RuntimeCamFollowYPoints 仍是旧标量。经现有 CorinCameraResourcesAuthoring.PublishEffectSettings 发布，18 项资产只产生 schema v1→v2、点位 0→空名称列表的变化；源配置点位为 null，镜头数值未改。整份 CharacterCameraProjectionBuilder.Build 在目标 Editor 通过，输出 character-camera-projection/v9、18 项 Stretch。

作业 68346d0f23bb495f8f56cf59561f0677 在构建前拒绝未保存的迁移资产，elapsed_ms=0，未执行第二轮原生编译。随后仅定向保存本次迁移的 18 项资产，并在独立调用确认 dirty 为空。新作业 f94f76ee1acb456a8c1a6d3941a7ac0a 使用 clean_build_cache=false 继续正式增量构建。

本次未改相机运行算法、未回退其它窗口源码、未更改脚步预测、未放宽资产协议或采集门禁。相机 Delay/轨道等其它未完成能力不因这次 Stretch 迁移而变成已完成。

## 采集结果

增量作业 f94f76ee1acb456a8c1a6d3941a7ac0a 完成，耗时 294889 ms，发布 Player 338eb8d529a9ed5646a94249；新增 12 个方法仍全部织入。该 Player 的 Smoke（cf584954dead4345ba971c6c89b09002）发现另一启动错误：CinemachineCameraRigAdapter.PixelHeight 在 Brain.Awake 建立 OutputCamera 缓存之前被角色 OnEnable 调用。证据位于 Gates/smoke/smoke.20260929-083234.b3e89b14521647ecaab9f62dde3e300c/。适配器现改为在自身 Awake 从 Brain.ControlledObject 取得 Camera，之后直接读取 pixelHeight。

随后在当前 GameplayLabFixed 的 Editor Play 中集中复现并处理两个额外问题，未为每个错误分别构建 Player：

1. EventGraphDeltaNode 已实现 InputId/ValueType/BindInput，却未声明 EventGraphHostInputNodeMarker，导致编辑器图校验失败且统一绑定循环忽略 Delta 输入。补齐接口声明，未修改 Delta 输入值和计算。
2. AP65 的 Pose 端口缓存早于 GatherPorts/BindPorts 建立。图随后重建端口，缓存引用旧 ValueInput，待机子图输出 pose 为空。将 runtime.Initialize 放到端口生成和绑定之后，保持每帧使用缓存。属于之前只做静态检查未发现的优化回归。

16:49 的重建作业 9dbd6063692c407aa4be013e43ee6bb2 被 `GameplayLabFixed.unity` 未保存拦住。后续只按构建边界保存该场景，未保存其它 dirty 资源。

## 当前版本采集

新构建作业 b991798232a44632b0b65e0655640d1e 完成，总计 575760 ms，发布 Player 7eda33a33be80142a355f59d。构建身份为 Span、`instrumentation_identity=4c905aef83b1bbab53c7521d99dd3073f33dd05f7be18fbd022815eded27f6ba`、`build_inputs_hash=70b03db003bab1f45c301c6959c3b47cff810811c2fe4b32d3de1b5a76873c82`。阶段耗时：

| 阶段 | 毫秒 |
| --- | ---: |
| 资源输入记录 | 1177 |
| 内置资源构建 | 73781 |
| Player 输入快照 | 1554 |
| Unity 构建 | 375322 |
| 输入核对 | 1204 |
| 产物整理 | 319 |
| 产物哈希 | 122378 |

Smoke 07657a716ac241af84466dc2f32dd678 与 Replay ba07d820e4184eafabe7bbb534dfcf5b 均 Completed。Capture 5fc857d0ac134ba3a1598d265e014645 完成，正式目录是 `3cDemo/Client/3C_Client/Library/Performance/Captures/capture.20260929-134336.3c5acd66f8e340e78cb73390ef29c09e/`。Player 日志确认 `targetFrameRate=-1, vSyncCount=0`。

### 整体与预算

| 指标 | 结果 |
| --- | ---: |
| 表现帧率 | 201.205 FPS |
| 逻辑频率 | 60.044 Tick/s |
| 采集时长 | 42.236 s |
| 表现帧 / 采集 Tick | 8498 / 2536 |
| 丢弃 Tick | 0 |
| Main Thread P95 / P99 | 6.6303 / 7.6988 ms |
| Logic Tick P95 / P99 | 1.7947 / 3.7436 ms |
| Presentation P95 / P99 | 3.6272 / 4.0810 ms |
| GC 每帧均值 | 39,172.6 B |
| GC P95 / P99 | 124,207 / 199,744 B |
| GC 总量 | 332,888,717 B |
| 预算 | GC 失败；其余已评估指标通过 |

`metric-samples.csv` 显示 GC 分配高度集中：前 1% 帧贡献 27.92%，前 5% 贡献 41.59%，前 10% 贡献 57.07%，前 25% 贡献 90.51%。4 帧各约 16.5 MB，合计约 66 MB，约占总量 19.8%。

### 热点初读

WPR Summary 共 83,074 个 Exclusive 样本，其中 61,414 个未解析符号，占 73.94%。因此原生模块比例只能当作继续分析的入口，不能当作完整精确归因。模块聚合为：`UnityPlayer.dll` 27.57%、`GameAssembly.dll` 26.07%、内核调度相关 22.86%、`ntdll.dll` 11.74%、NVIDIA 用户态驱动 6.24%。当前导出的 CPU 热点是 All Threads 聚合；Context Switch 是等待证据，不与 CPU 样本相加。

已解析 Managed 样本中最高的是 `GC_end_stubborn_change`（1,819 样本，2.19%），随后是 `SHA256Managed_SHATransform`（0.69%）、`Quaternion.normalized`（0.68%）、`InterfaceFuncInvoker0<int>.Invoke`（0.52%）、Timeline PlaybackHandle 不等比较（0.51%）、`CharacterPoseConstraintMath.TryCreateComponent`（0.48%）、`AnimationPoseMath.Differentiate`（0.45%）、`AnimationLocalBonePose.IsValid`（0.34%）和 `FixedScalar.DivideScaled`（0.33%）。

Span 层的业务热点集中在表现动画：

| 入口 | 调用数 | 均值 | 估算总量 | P99 / Max |
| --- | ---: | ---: | ---: | ---: |
| Gameplay Presentation | 8498 | 2.9046 ms | 24.684 s | 4.0805 / 15.0098 ms |
| Presentation Animation | 16996 | 1.4508 ms | 24.649 s | 2.79 / 13.94 ms |
| PoseGraph Evaluate | 54321 | 0.2307 ms | 12.532 s | 0.9535 / 1.6562 ms |
| Logic Tick | 2536 | 1.3363 ms | 3.389 s | 3.7436 / 7.7911 ms |
| Simulation Transaction | 2536 | 1.3145 ms | 3.334 s | 3.7375 / 7.7844 ms |
| Foot Placement | 16996 | 0.1334 ms | 2.267 s | 0.7059 / 1.3455 ms |
| EventGraph | 16996 | 0.0781 ms | 1.327 s | 0.1855 / 0.6261 ms |
| Simulation Pipeline Evaluate | 2536 | 0.4963 ms | 1.259 s | 2.5943 / 6.8848 ms |
| Simulation Character Evaluate | 5072 | 0.2262 ms | 1.147 s | 0.6710 / 6.7458 ms |
| FullBodyIK | 16996 | 0.0650 ms | 1.105 s | 0.1575 / 0.4802 ms |
| PoseGraph Prepare | 54321 | 0.0183 ms | 0.994 s | 0.10 / 0.44 ms |

当前场景没有实际调用的入口保留为无样本：装备表现、FactProjection、Float32 角色评估、Float32 GE 推进、Float32 Ability Tick、Pipeline Restore 两个入口、Pipeline Other Pass、Float32 Pipeline Transaction 和 Float32 World Snapshot。它们不能解释为零成本。

下一步应先追两件事：一是 PoseGraph Evaluate/Foot Placement 的骨骼数学和重复校验；二是 4 个约 16.5 MB GC 帧对应的表现 Timeline/动作事件路径。需要用原始 ETL 或 Span 原始文件把大帧对齐到具体动作后再改代码。
