# 脚部局部复算工具证据

本目录按完整场景保存验证。接触交接包含独立函数实验与尚未完成的 Unity 下游验证；Releasing 跨阶已有正式 EditMode 历史对照。每个场景维护自己的 HTML，避免局部通过被误读为整条链路通过。输入和结果统一从[脚部入口](../README.md)查找。

| 文件 | 用途 |
| --- | --- |
| [接触输入](expired-contact-input.json) | 从指定脚部采样中保存的帧、查询与来源哈希 |
| [接触复算页](expired-contact.html) | 原采样播放、目标交接语义与实际重算结果；测试未运行时仍可播放原采样 |
| [Unity 下游证据](expired-contact-unity.json) | 正式 runner 空跑、装配编译、输入来源复核与未执行层级 |
| [函数实验结果](expired-contact-functions.json) | 真实历史/当前源码身份、100 帧函数输出、完整断言与失败原因 |
| [Releasing 连续对照](release-clearance.html) | 同输入播放历史版、当前版与原采样，解释硬抬、停脚与腿长冲突 |
| [Releasing 输入](release-clearance-input.json) | 12570 初始化、12571～12585 输入及七份原 CSV 哈希 |
| [Releasing 数值结果](release-clearance-result.json) | 四份实际历史/当前源码、逐帧输出、净空、腿长占比与计算分配 |
| [正式 runner 结果](release-clearance-runner.json) / [NUnit XML](release-clearance-test-results.xml) | EditMode 1 个用例、1 个通过；装配失败记录见 [原始失败](release-clearance-attempts.json) |

独立运行（从仓库根目录；需要 .NET 8 或以上 SDK、当前项目的真实 Library 程序集）：

```powershell
pwsh -File Tools/FootPlacement.FunctionVerify/run.ps1 -UnityEditorData "C:/Program Files/Unity/Hub/Editor/2022.3.62f2c1/Editor/Data"
```

入口在 [run.ps1](../../../../Tools/FootPlacement.FunctionVerify/run.ps1)，调用 [Program.cs](../../../../Tools/FootPlacement.FunctionVerify/Program.cs) 链接的正式 C# 函数。历史 LandingRuntime 取自输入记录的基线提交，其他被测函数两版共用相同源码。编译身份使用已有 friend assembly 名称，仅用于访问内部正式类型，不覆盖 Unity 程序集。构建及历史文件写入 Client/Temp。

此次 100 帧结果：旧版重现 27 帧目标缺失，当前为 0；前 45 帧目标类型和位置保持一致；20 帧 Approach、24 帧实际接触和 53 帧无接触摆腿检查通过。两版预热后的连续计算均为 0 字节托管分配。结果的确切源码与输入哈希见 JSON。没有执行新的地面查询、插值后最终权重、碰撞或骨骼 IK，因此不能宣称脚不穿透或 Landing / Releasing 伸直已修复。

输入新增的 CurrentSupport 字段来自同一原 CSV，提取前核对原 SHA-256；不是由探针记录重新猜出的支撑目标。独立入口保留原路径输入身份，消费录制查询结果。未记录的 clip normalizedTime 及未消费的腿长、配置版本标识不参与本段计算；原事件身份逐帧核对。上下文首帧初始化一次，此后由正式函数推进。

此处是测试固定的输入与输出位置，测试代码已同步使用本目录。运行入口为 Unity EditMode 的 `CharacterFootCapturedContactTests.CapturedContactToSwingReleasesExpiredTargetWithoutLosingValidSupport`；场景需为 `GameplayLabFixed`。局部复算只建立对应输入及调用范围内的事实，实际回放、画面与完整角色验收仍由正式运行证据说明。整体调查见[台阶连续性解释器](../ik-stair-continuity-explainer-20260930.html)。

## 本轮接触交接下游状态

2026-09-30 正式 EditMode job `17df3c45ac9a47bc97efed6516468aea` 返回 summary `Passed`，但 total=0：目标用例没有执行。当时 Editor 编译失败；刷新前为 Timeline 5 个接口缺失 CS0535，脚本刷新后为新 Diagnostics 类型未导入的 2 个 CS0246。完整导入及域重载后编译失败状态已解除，实际测试类型由 `Analysis/ThirdPersonCharacter.Content.Editor.asmref` 归入 `ThirdPersonCharacter.Content.Editor` 并已加载，不能用只查 `ThirdPersonClient.Editor` 的结果判断全局没有测试类型。本轮续发请求明确收好该段，不继续重跑或修改无关 Timeline。

测试补齐文件头业务说明，保留录制地面路径身份与查询输入，按正式模块装配 ContactLanding；解析、断言、结果写出移到计算循环外，预分配结果并添加两版预热分配检查。查询失败与无命中分别记录，无命中的净空写 null，不用 0 伪装安全。修正后的测试用 Unity 自带 Roslyn 引用真实程序集独立编译成功；没有再次执行正式用例，不称为行为验证。装配同时按 WorldAware.PresentationRoot 的实际引用选择 Player rig；WorldAware 不在 Animator 的父级组件链上，不能用 GetComponentInParent 获取。

本轮逐项核对五份原 CSV 哈希、100 帧所有保留字段、2300 条探针、322 个包络点、4942 个接触与5511个覆盖面，按 sample.sequence 和 /right 配对，字符串与原记录完全一致。这是输入来源核对，**不是**2300次新 Physics 查询。原函数证据的输入、配置和六个源码 blob 均未变化，继续复用原 27→0 帧缺少目标结论。新的 PhysicsScene 一致性、Lifecycle 插值、最终权重与输出脚底查询仍为 0 帧，见同一 HTML 和 Unity JSON。

## Releasing 历史连续对照（本轮完成）

测试为 `ThirdPersonCharacter.Pipeline.Editor.CharacterFootCapturedContactTests.CapturedReleaseComparesHistoricalClearanceAndContinuousOutput`，代码在 `CharacterFootCapturedReleaseTests.cs`，共享原采样解析器。由 Unity Test Runner 的 EditMode 或正式 `run_tests` 按完整名称显式启动。测试装配 GameplayLabFixed，结束后恢复原活动场景，只关闭自身加载的场景。

历史源取自 `413e931de7724578c8a15e1b00ab16fb8b000e9d`，包含 Lifecycle、InterpolationRuntime、HardConstraintResolver、LandingRuntime；当前版编译相同四个正式文件。两版共用同一组真实运行程序集，具体身份见结果 JSON。临时历史源码与程序集只保存在 Client/Temp，不覆盖 Unity 的 Library。路径、预测和作者参数作为固定上游输入；接触状态、残差、插值、查询、约束、历史回写与 Goal 由正式函数连续推进。

| 同一窗口指标 | 冻结前历史版 | 当前版 |
| --- | --- | --- |
| 最大末端硬抬 | 6.456 cm（12572） | 0 |
| 水平冻结帧数 | 0 | 3（12572～12574） |
| 最大目标距离 / 腿长 | 95.925% | 115.625% |
| 12572 水平步长 | 29.829 cm | 0 |
| 12575 水平步长 | 3.380 cm | 20.162 cm |
| 退出 Releasing | 12579 | 12581 |
| 预热后计算循环分配 | 0 B | 0 B |

当前版的脚位逐帧重现原采样，最大位置误差为 0，腿长占比最大差约 1.79e-6；两版各匹配 368 个原始脚底探针。七份原 CSV 哈希与所有保留字符串逐项核对，包含 368 个原脚探针、276 个目标探针、368 个输出探针、62 个包络点、472 个接触和 356 个覆盖面。上述来源核对与实际重新执行的 Physics 查询是不同证据。

结论：当前规则消除了这段旧版硬抬，但引入了停脚、不可达目标与恢复位移。旧版也存在 29.829 cm 的单帧水平移动，不能因为腿长合格就说整体平滑。12574 当前冻结前的插值脚掌重查已有向下净空，固定髋下的伸展比约 96.810%，冻结后却为 115.625%；这进一步定位到目标位置高度门对当前推进的过度限制。直接回退冻结仍会恢复 12572 的 6.456 cm 硬抬。

正式 job `173de80382fb454ab49e56cef446612f` 为 1/1 Passed。通过只表示历史比较和缺陷复现成立，不表示运行时修复通过。本轮没有修改运行时代码。腿长计算固定使用记录的髋姿态，没有执行骨盆或腿 IK；23 点向下查询不是鞋网格连续扫掠。4107-left、4127-right、4131-left 的旧硬抬只保留原记录，没有在本轮连续重跑，仍是后续修复必须覆盖的历史回归条件。

## 被否决的净空放行候选

在 `840bbd6b9` 验证基线上试验：原高度门准备冻结时，按插值脚掌重新查询；有净空则提前前移，仍有穿入量时继续暂停，并复用未改变姿态的查询。运行时与对应验收断言的完整试验改动保存在 [补丁](release-clearance-rejected-candidate.patch)，数值、源文件身份及失败断言见 [候选结果](release-clearance-rejected-candidate.json)。补丁可以在该基线上复核，未保留为生产执行路径。

| 指标 | 现行冻结版 | 被否决候选 |
| --- | --- | --- |
| 冻结帧数 | 3 | 2 |
| 最大目标距离 / 腿长 | 115.625% | 101.498% |
| 实际恢复帧 | 12575 | 12574 |
| 实际恢复水平步长 | 20.162 cm | 26.435 cm |
| 固定看 12575 的水平步长 | 20.162 cm | 9.153 cm |
| 整个窗口最大水平步长 | 21.467 cm | 26.435 cm |
| 末端硬抬 / 计算循环分配 | 0 / 0 B | 0 / 0 B |

只比较固定 12575 会错误地判为改善；按实际解除冻结事件检查，允许上限 0.201716438 m，实际为 0.264349431 m，NUnit 断言失败。该候选由 Unity 内直接执行编译后的真实 C# 测试方法完成函数实验，不是正式 runner 的新通过结果。前述正式 1/1 Passed 仍只属于历史缺陷复现。失败候选已撤销，运行时与正式测试源码恢复到 `840bbd6b9`；同一 HTML 增加红色失败轨迹，保留三个版本的区别。

后续验收必须按实际事件对齐，并检查完整窗口的极值；不能把问题提前一帧当作消除，也不能仅按腿长指标判定整体改善。

## Stored 混合输入对照（2026-10-01）

同一动画混合业务包含两种连续条件：左脚 1174～1186 在无接触姿态被捕获后交给新的 Live；右脚 889～958 在真实接触姿态被捕获后继续接触并交还 Live。13 / 70 帧已调用历史正式选择函数、当前正式选择函数和 `CaptureStoredPose`，预热后的计算均为 0 B。历史源固定为 `2c757a422`，候选生产身份为 `18e1f2a4f`，实际程序集哈希见 [来源身份](stored-foot-motion-provenance.json)。

左脚 1178 的历史选择为零权重 Idle，Contact=1；候选选择有实际权重的 Stored，保留捕获时 Contact=0。真实接触段保持捕获的 Contact / LockWeight，停止未来落地预测和脚趾速度，新的 Live 占主权后接回其曲线。作者 FootPlacementWeight 未被改写。连续播放、贡献权重与接管过程见 [Stored 场景页](stored-foot-motion.html)。

这批通过只属于来源选择与 Stored 时间语义，没有执行 Native Slot 历史捕获 Job、Goal、Physics 或 FBBIK。当前 Editor 被其它任务的程序集引用错误阻塞，正式 runner 未执行。完整 Stored 骨骼姿势仍缺失，不用默认姿势替代；Goal 接续结果将保存在同一场景页。没有 SourceId 记录且为零权重的 Action 条目不参与本段选择，零权重 Idle 和所有正权重贡献保留；不能把测试准备位置当作物理 source 注册验证。

复现入口：`powershell -File Tools/FootPlacement.StoredPoseVerify/run.ps1`。入口使用当前项目实际编译的 Animation 程序集；历史方法从 Git 正式源码提取，只把实例贡献数组变为显式参数，分支算法不改写。固定输入保存双脚原字符串、原采样哈希和查询记录，结果见 [左脚函数结果](stored-no-contact-left-functions.json) 与 [右脚函数结果](stored-contact-right-functions.json)。

## 后续场景范围

按用户指出的 Landing / Releasing 伸直组织两个完整场景，不按私有函数或字段拆测试；完成一段后统一维护其 HTML 与提交。

| 场景 | 已有证据与需要回答的问题 | 完整检查与历史限制 |
| --- | --- | --- |
| Releasing 修复及历史回归（待实施） | 本轮已完成 12571～12585 连续历史对照。下一次候选需修正多余冻结，同时覆盖旧版跨边硬抬，不能把本轮缺陷复现通过当作修复通过。 | 保留作者参数，比较净空、恢复位移与可达性；补跑 4107 / 4127 / 4131 旧硬抬窗口。不能只删除冻结，也不能用旧位置的命中证明新位置无穿透。 |
| Landing 获得接触到释放 | 围绕 33765→33766 扩到完整接触过程；脚底约动 1.2 mm、身体水平约动 16.4 cm。先核对 Body 提交时序和坐标，再连续推进锚点、滑动/释放、骨盆与腿求解，找首次不可达来自哪一层。 | 正常着地保持支撑；检查接触稳定、离地/穿透、腿长余量、状态退出和权重。不能求解后平滑膝角却同时固定脚点；历史提前释放/骨盆硬约束带来的离地、掉权重与末端补高须纳入回归。 |

独立函数先执行几何与显式状态；候选改变脚掌姿态后，新的碰撞查询必须进入正式 Unity 环境。两段都先重现旧结果，再比较候选，不能把一次同帧几何复算当作连续修复通过。旋转导致脚掌跨边作为完整场景的输入和检查项；作者手调权重的生效语义保持明确，不靠自动改权重掩盖冲突。若当前脚约束、身体运动和净空不存在共同可行解，先列清必须调整的业务行为及代价，再决定方案。
