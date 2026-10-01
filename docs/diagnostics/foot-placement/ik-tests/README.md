# 脚部局部复算工具证据

## 统一离散采样边界A/B（2026-10-01，候选被拒绝）

直接基线为`fbed4b1923b90d7126a50bab512b3a5e9b2a824b`已保留来源修正版，候选仅叠加`AnimationFootStepObservationCurves.cs`与`AnimationFootStepLandingEvents.cs`。原输入2023种子，Native实际预滚2023后连续2024～2056；Foot同时恢复2023真实Spring/primary/观测缓存。正式两采样文件完整编译，初始化时把发布曲线与事件数据复制到本地正式契约，暖机循环实际执行各自`Sample/Resolve`，没有借用Editor已加载的采样实现。Native Job、Workspace、Action Slot、预测、Lifecycle、骨盆与Goal均固定基线；其它工作区四个Animation修改不参与本轮源码。详见[完整结果](releasing-boundary-result.json)、[基线Native](releasing-boundary-baseline-native.json)/[Goal](releasing-boundary-baseline-goal.json)、[候选Native](releasing-boundary-candidate-native.json)/[Goal](releasing-boundary-candidate-goal.json)。

| 完整33帧业务指标 | 保留基线 | 两文件候选 |
| --- | --- | --- |
| 右最大目标伸展比 | 117.964232% | 112.936139% |
| 右超长帧 / 持续时间 | 8 / 0.13333334s | 8 / 0.13333334s |
| 2031目标伸展比 | 100.6192% | 103.173792% |
| 2032目标伸展比 | 105.127513% | 108.003044% |
| 右最大额外修正向量单步 | 0.301888393m | 0.302270128m |
| 右额外修正累计变化 | 1.459505558m | 1.403930138m |
| 左超长帧 / 最大额外修正单步 | 0 / 0.072392123m | 相同 |
| 双脚实际最终净空查询 / 最大正穿透 | 各33/33 / 0m | 相同 |
| Native含实际采样 / Foot暖机分配 | 0B / 0B | 0B / 0B |

2033的Phase为Swing时，LockMode从Sliding修为Unlocked，右脚提前一帧按正式规则进入Releasing；2030的Locked也修为Sliding。连续曲线、作者预算、实际Goal权重与Native踝位逐值一致。但2029～2032以及2041的Landing目标余量变差；2031目标距离多约1.8cm、2032多约2.0cm，已有超长进一步恶化。最大额外修正单步也超出原0.2mm比较余量，因此裁决`rejected-quality-regression`；峰值改善和阶段执行Passed不能覆盖早段回归。两版没有进入Locked的有效观测帧，Locked锚点漂移记null，不以0冒充覆盖。最大已观测脚底净空左约0.409596m、右约0.378882m，两版相同；这不是整窗口悬脚已经消除。

原0.240472mm>0.2mm的FBBIK历史校准失败保留，不拿未校准膝角作为本轮收益，也未重跑IK或Replay。提前Release造成8～10cm悬脚、解除冻结产生26.435cm追赶的反证仍保留。生产候选由主窗口撤回，只恢复这两文件；本窗口不改生产或其它在途文件。

封存身份：

| ZIP | SHA256 |
| --- | --- |
| [生产两文件](releasing-boundary-production-snapshot.zip) | `1e4972b129e681c6c7128312c51c8bc0d092b7fd7f5bfb82853ec0a0d23b738d` |
| [实际构建输入](releasing-boundary-build-inputs.zip) | `db58b3ef4135ca07d3bdf2ca53444244f26f62327620d89d2daae2fe7b1c1825` |
| [实际执行程序集](releasing-boundary-executed-assemblies.zip) | `38294057414da2fb242ec76b83b56163dcf114d0ac1f60eac746422b6f8c7f5d` |

唯一一次目标Editor检查取得本项目Edit/空闲状态后完成独立编译和真实函数A/B；两版作业均结束，未持有`DisallowAutoRefresh`，各次编译后build-server已关闭。原Temp引用清单已不存在，本轮从这一次检查取得真实已加载程序集位置，不使用备用DLL。初次Native链未包含2023预滚，已修正同一装配后按完整种子重算，最终身份绑定有预滚版本。所有候选读取来自已封存文件，撤回后报告重生成只读取封存结果，不读取工作区候选。原HTML完整33帧滑块、转换表、早段回归、每源几何与三版旧证据的DOM检查通过；浏览器视觉验收未完成。

## 实测准备前的执行交接（2026-10-01）

所有本窗口Unity调用已结束；未调用或持有`AssetDatabase.DisallowAutoRefresh`，没有待释放持有，不调用未配对的`AllowAutoRefresh`。独立编译后build-server已关闭。用户要求先实测保留的改善版，因此停止新增Unity调用、Assets写入和测试运行，主窗口接手刷新；保存结果和文档不启动新实验。

同一完整释放业务的两个排除实验已经实际结束：[逐帧独立求解器](releasing-business-ik-diagnostic-cold.json)、[Editor实际加载求解器](releasing-business-ik-diagnostic-loaded.json)。两者历史最大关节点误差均0.240472364mm，计算均0B；实际加载solver MVID为`cd6cec60-06dd-47f0-9dd6-6ad8029c56d7`，RootMotion为`9ddc76af-1111-4780-8a7c-82ad28fa1e26`。不支持跨帧求解状态或冻结实现绑定差异是原因，不改0.2mm容差，不猜runtime tuning，不修改生产solver。完整原组件页尚未取得，剩余输入差异仍未定位。

已完成2023种子加33帧的每源几何观测。正式ACL源姿势和实际Native混合输出经正式`CharacterFootPlacementPoseRig.CaptureFoot`代码及heel/toe标定得到组件脚底中点，原始坐标、源名称、每脚实际权重、作者FootHeight、原世界PoseRoot与RootBonePolicy单列保留，见[原始几何](releasing-source-geometry-raw.json)和[基准算术](releasing-source-geometry.json)。本轮只观测`B_i=sourceSoleY−FootHeight_i`及`H=finalSoleY−Σ(w_i B_i)/Σw_i`；ExcludeSourceRoot/PreserveReferenceScale，单位组件m，不视为已知地面，不复活脚高加权候选。右H在2037～2043依次约0.286918、0.244292、0.137402、0.021570、−0.077843、−0.070440、−0.024484m。

本轮[封存源码与实际程序集](releasing-observation-source-snapshot.zip)包含三种实际执行身份，完整生成、成员哈希复核后原子替换。几何首次编译暴露using NativeArray写入限制，修正为可写别名后实际执行；manifest明确记载准备期与最终编译输入的哈希更正。用户停止后只保存已经得到的证据。原场景HTML新增观测区已生成，但未重新运行交互检查；旧33帧/三版区域的先前DOM检查不冒充新增区已验收。

## 完整全身 Action 释放窗口（2026-10-01，FBBIK校准未通过）

同一业务扩为2023真实种子、2024～2056连续33帧，保留进入Landing、Releasing和退出Swing。完整结果在[原场景页](releasing-action-native.html)：[Native](releasing-business-native.json)、[双脚预测/骨盆/Goal](releasing-business-goal.json)、[实际FBBIK](releasing-business-ik.json)、[向量汇总](releasing-business-summary.json)、[源码与执行程序集快照](releasing-business-source-snapshot.zip)。原2035～2046的12帧结果及输入单独冻结，没有改写为33帧执行证据。

正式发布ACL提供201物理骨，Native Job提交实际状态，完整Action Slot混合后将真实FootMotion交给正式BodyTrajectory/PredictFootPair和PhysicsScene查询，再连续执行双脚Lifecycle、骨盆Spring、Complete、三个Goal以及实际FBBIK。Native固定`d6d6ab9f9`、Foot和IK固定`69a36d339`，KCC未来端口仍消费原采样。真实2023种子恢复Spring、primary、观测和地面路径；仅预滚实际姿势和Goal初始化IK方向，没有伪造四个历史方向。历史previous-dot/Revision元数据未还原，完整GoalAssembler及Unity/Burst调度未执行。正式Runner未执行。

| 全33帧实际指标 | 历史来源 | 来源修正 |
| --- | --- | --- |
| 右脚最大额外修正向量步长 | 0.434414m（2041） | 0.301888m（2041） |
| 右脚额外修正总变化 | 1.708409m | 1.459506m |
| 右脚最大额外修正速度 | 26.06484m/s | 18.11330m/s |
| 右脚目标超伸帧 | 10 | 8 |
| 左脚目标超伸帧 | 0 | 0 |
| 双脚已观测最终Goal查询 / 最大正穿透 | 各33/33帧 / 0m | 各33/33帧 / 0m |
| Native / Foot / IK暖机计算分配 | 0B / 0B / 0B | 0B / 0B / 0B |

额外修正按`Δ(Goal脚底−原动画脚底)`向量计算。历史右脚2031～2037已有连续7帧近直腿；完整实际求解右脚小于1°为10→8帧，但历史2033六关节点最大误差0.240472mm超过既定0.2mm，**IK比较未校准，整体未通过**。保留[初次失败](releasing-business-ik-attempt-native-pose-calibration.json)、[录制六位置诊断](releasing-business-ik-diagnostic-recorded-leg.json)和[录制三个Goal诊断](releasing-business-ik-diagnostic-recorded-goals.json)；后两种仍失败，不能据此回退到录制输入冒充正式结果。现在先保存两版实际求解，再统一判校准失败，避免首次断言丢掉候选数值。

首次路径JSON在序列结束后读取复用池页，使33帧Accepted全部误写false。实际计算使用当帧路径；已改为当帧复制状态、拒绝原因、身份、接触/包络计数、预测与观测必要标量，格式化仍在计算段外。重跑Goal值不变，历史2030～2041正确为Accepted；2041候选`NewEventContactAcquired`，2042`ContactOutOfSlideRange`，响应域分别切入ContactWorldResidual、AnimationRelativeScalar；历史同样变化晚一帧。HTML播放33帧、双腿投影、转换/响应表和失败标签已通过Node DOM检查；浏览器视觉验收未完成。

### 第三版连续FootHeight混合被拒绝

第三版在实际来源选择后调用正式`AnimationFootMotionSourceSample.BlendFootHeights`，Native历史捕获也消费正式脚高混合；Foot与IK仍冻结`69a36d339`。来源由[Native结果](releasing-height-native.json)交给[预测/双脚Goal](releasing-height-goal.json)，再进[实际FBBIK](releasing-height-ik.json)。2031～2036原本Landing超伸完全不变，2037～2044右脚目标伸展比增大；2038从0.9778428增至0.989226162、2040从0.835339增至0.8508378。完整超伸仍8帧，最大伸展1.17964232不变；右脚最大额外修正步长0.301888393→0.302998703m，累计变化1.459505558→1.447711612m。依据已校准Goal可达余量劣化裁决`rejected-height-blend`，不依赖未校准膝角；Foot阶段Passed仅证明比较执行与既有断言成立。三段暖机均0B、双脚最终已观测正穿透仍0，但这些不能覆盖业务劣化。完整骨骼组件页和接触/事件等非高度字段逐值与来源修正版相同，见[拒绝汇总](releasing-height-summary.json)。原窗口已撤回三份生产候选并独立编译，不由测试窗口恢复。

候选[源码及实际Native程序集快照](releasing-height-source-snapshot.zip)现在SHA为`ae1b1ec9a0ada07d3f338b1a2861057607f2c5d47607d7e30e7d5138a5860b6b`。二次生成归档与生产撤回并行，脚本错误截断原ZIP；保留编译输入及程序集，从冻结Git文本和已执行声明恢复三个production成员，逐项匹配原manifest SHA后完整生成并原子替换。原ZIP的`53c43e...`仅是撤回前原窗口已复核的历史身份，不冒充现文件。后续指标生成只读归档，不再读取已撤回生产源码；整包写入改为临时完整文件完成后替换。

已用正式“已归一化”`AnimationLocalBonePose`构造还原组件页四元数，并确认还原位值；同业务重跑历史2033仍0.240472mm，重复归一化未解释误差，见[保持四元数失败](releasing-business-ik-attempt-preserved-quaternion.json)及[第三版同样失败](releasing-height-ik-attempt-preserved-quaternion.json)。新[Foot/IK源码及实际程序集](releasing-preserved-quaternion-source-snapshot.zip)与旧失败程序集分开保存。

生产撤回后已从归档源码完整重现第三版：Native/Goal通过、实际IK输出保留并以校准失败退出1，所有暖机段0B。实际命令、MVID和SHA见[归档重现](releasing-height-reproduction.json)。复现入口不写生产文件：

```powershell
pwsh -File Tools/FootPlacement.StoredPoseVerify/run_releasing_business.ps1 -UnityInstance e852139597e42532 -ResultDirectory 3cDemo/Client/3C_Client/Temp/FootReleasingBusiness/check -HeightCandidate
```

不传`-HeightCandidate`使用冻结来源修正版。此入口是Unity内函数实验，末尾IK校准失败返回非零；没有正式Runner、Replay或自动输入。HTML同一33帧新增三版高度、Goal伸展、向量修正与实际膝角曲线，播放/选帧联动已检查，浏览器视觉验收未完成。

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

来源函数通过与 Goal 通过分别保存，不能混作同一执行层级。完整 Stored 骨骼姿势仍缺失，不用默认姿势替代；没有执行 Native Slot 历史捕获 Job 或 FBBIK。没有 SourceId 记录且为零权重的 Action 条目不参与本段选择，零权重 Idle 和所有正权重贡献保留；不能把测试准备位置当作物理 source 注册验证。

复现入口：`powershell -File Tools/FootPlacement.StoredPoseVerify/run.ps1`。入口使用当前项目实际编译的 Animation 程序集；历史方法从 Git 正式源码提取，只把实例贡献数组变为显式参数，分支算法不改写。固定输入保存双脚原字符串、原采样哈希和查询记录，结果见 [左脚函数结果](stored-no-contact-left-functions.json) 与 [右脚函数结果](stored-contact-right-functions.json)。

同一 83 帧已在目标 Editor 执行 `LandingRuntime → Lifecycle → 23点实际支撑查询 → Complete → Goal` 的连续函数实验。上下文只在首帧按完整采样前态恢复一次，后续由正式函数推进。脚端四份源码均冻结在 `18e1f2a4f`，分别接历史与候选 FootMotion。原动画脚姿势、预测、地面路径和骨盆可达性反馈为固定边界输入；`input/pose-plan-hash` 是正式 response lineage，不能拿 clip SourceId 代替。源码和真实 Unity 程序集身份、三次失败装配及正式入口复现身份见 [原生身份](stored-native-goal-provenance.json)。

| 连续 Goal 窗口指标 | 历史来源 | 候选来源 |
| --- | --- | --- |
| 左脚最大旋转步长 | 137.9343° | 27.5375° |
| 左脚最大额外旋转步长 | 137.5741° | 0.000143° |
| 左脚最终净空已观测帧 | 12 / 13 | 12 / 13 |
| 右脚最大额外旋转步长 | 1.33253° | 0.051918° |
| 右脚最终净空已观测帧 | 70 / 70 | 70 / 70 |
| 右脚固定髋最大伸展比 | 100.331926% | 100.331926% |
| 右脚固定髋超伸帧 / 时间 | 4 / 0.066667 s | 5 / 0.083333 s |
| 暖机 Goal 计算分配 | 两种条件均 0 B | 两种条件均 0 B |

历史 Goal 逐帧重现原脚位和旋转，误差均为0。候选 1178～1180 为 Swing 且无接触锚点；右脚 894～898 保留 893 的真实锚点，位置误差不超过0.2mm，954 按 Live 实际输入退出锁定。候选已观测帧最大正穿透约7.45e-9m。1174 原本没有有效 Goal，位置权重0、最终查询无命中，净空写 null；其余帧实际位置权重1。本轮输出权重仅覆盖0/1，部分作者权重须在后续连续场景验证正式加权位置。

函数实验结果：[左脚 Goal](stored-no-contact-left-goal.json)、[右脚 Goal](stored-contact-right-goal.json)。复现用 `pwsh -File Tools/FootPlacement.StoredPoseVerify/run_native_goal.ps1 -UnityInstance e852139597e42532 -ResultDirectory <独立结果目录>`；入口先确认项目与非Play/非编译，再用 Unity 自带 Roslyn 编译真实源码并在实际 PhysicsScene 执行，恢复活动场景、删除自身临时对象并仅关闭自身加载的场景。2026-10-01 完整导入后仍有其它任务的 Float32Host 编译错误，旋转新状态尚未加载，正式 Runner 未执行。HTML 已检查83帧切换、播放与数值曲线；浏览器视觉验收未完成。23点查询不是鞋网格连续扫掠；固定髋超伸仍增加，不能称为腿 IK 或膝盖伸直已修复。

## Live 连续旋转候选（2026-10-01，固定条件有业务回归）

本轮已保存 2192～2214 共23帧的完整函数对照，历史 `552f13083`，候选 `15894ee7b`。2192 是录制的作者零权重，真实执行后清空新旋转历史；2193～2201恢复0～1预算，2197第一次接触，2204～2206锁权重回0，2207再次接触。部分预算下的最终 ankle 与 rotation 均按正式 Goal 权重加权后重新查询，并与正式 EffectiveAnkle/EffectiveRotation核对。没有置零后直接从2200假装拥有真实候选历史。

初次基线使用诊断的 `final-effective-correction` 作为最终位移，暴露作者部分权重时该字段尚未加权。入口已统一消费正式 `resolved/effective-sole`；历史版23帧位置和有效旋转误差均0。类型绑定则同时编译11份实际生产源码，包括新状态契约和相关消费者，引用真实Unity程序集；没有只复制Lifecycle继续接旧state，也没有改写算法绕过绑定。

| 23帧完整窗口指标 | 历史 | 旋转候选 |
| --- | --- | --- |
| 最大有效旋转步长 | 37.06785° | 25.12180° |
| 最大动画相对修正步长 | 31.18009° | 9.07650° |
| 2207有效旋转步长 | 34.32866° | 12.23456° |
| 最大脚掌步长 | 0.163714m | 0.163714m |
| 23点实际查询覆盖 | 23/23帧 | 23/23帧 |
| 最大正穿透 / 暖机分配 | 0m / 0B | 0m / 0B |
| 固定录制骨盆后髋的最大伸展比 | 98.38907%（2210） | 99.81759%（2211） |
| 固定髋超伸时间 | 0s | 0s |

单版函数实验均完成，但跨版本业务断言为**失败**：候选没有保住逐帧固定髋弯曲余量，不能用未超1判通过。原动画2211膝弯曲约25.828°，同骨段长度下固定髋的候选Goal所需弯曲角更小；该几何量没有求解FBBIK。23帧尾部仍Releasing并有锚点，稳定/退出阶段与真实双脚骨盆反馈尚待继续，不能截掉后段宣称修复完成。

旋转候选的Stored回归另保存：左1174～1186无接触捕获仍为Swing且无锚点；右侧从真实无Goal的874延长到958，正式清空旋转历史后跨过真实接触、Stored捕获与954交还Live。两段各13/85帧、两版暖机均0B，候选查询无新增正穿透。右侧同帧固定髋弯曲余量也有回归；已有100.331926%的最大伸展比与5帧超伸均保留，未称为伸直修复。原83帧18e来源修正证据保持独立。

23帧及三窗固定条件证据冻结在[完整跨版本断言](rotation-business-comparison.json)、[装配失败](rotation-attempts.json)、[来源函数身份](rotation-source-provenance.json)。原生复现：`run_native_goal.ps1 -Mode rotation -UnityInstance e852139597e42532 -ResultDirectory <独立目录>`，然后对同一目录调用 `compare_rotation_reports.py --results <目录>`。同一HTML现已接续下述58帧真实双脚结果，原冻结JSON不覆盖。正式Runner仍未执行，浏览器视觉验收未完成。

## 双脚及真实骨盆连续对照（58帧，两个旋转候选均否决）

窗口扩为2192～2249，补齐另一脚原始查询、路径和完整前态。正式输入Hip用 `root + rotation * (recordedOriginalHip - recordedPelvisComponentDelta * recordedPelvisWeight)` 恢复，录制landingHip可用时在输入边界核对20um。两脚连续Evaluate后按正式PrimarySupport、Intent、PreparePelvis、ResolvePelvis计算骨盆，再把实际双边可达性反馈交给Complete；原生私有所有权与骨盆Goal发布方法从固定提交提取原文，只改变访问级别，没有改写算法。

历史 `552f13083` 重现58帧骨盆Goal、两脚脚位和左右可达性反馈；暖机0B。冻结候选 `15894ee7b`、修订候选 `8c0e878f8` 同样在真实PhysicsScene运行，各58帧、0B，两脚无新增正穿透，但完整业务比较均失败。

| 完整双脚窗口 | 历史552 | 失败158 | 失败8c0 |
| --- | --- | --- | --- |
| 右脚2207有效旋转步长 | 34.32866° | 12.23456° | 13.09939° |
| 右脚最大相对旋转修正步长 | 31.18009° | 9.07650° | 9.93661° |
| 右脚2211所需几何膝角（原25.82786°） | 20.85927° | 6.92972° | 24.52240° |
| 右脚膝角修正变化率峰值 | 892.35195°/s（2197） | 920.26624°/s（2198） | 920.26624°/s（2198） |
| 左脚最大伸展比 | 100.306225%（2228） | 100.6635%（2229） | 100.776231%（2229） |
| 左脚超伸帧 / 时间 | 1 / 0.016667s | 3 / 0.050000s | 3 / 0.050000s |

修订8c0虽改善右脚2211，却仍损失2200、2230的同帧余量；左脚2229由历史98.107916%增至100.776231%，不能只用右脚峰值或未超1的帧判通过。几何膝角修正速率用 `abs(delta(requiredBend-originalBend))/dt`，另存所需角速度减原角速度的正差，避免两个定义混用。原骨段/原膝角用同一录制post三点，实际新Hip独立由新骨盆得到；不可达角为null，不将preKnee、旧骨盆后的ankle与新Hip混算。

完整退出也纳入对照：历史和8c0在2245清空右脚锚点，2246～2249为UnlockedSupport；158还有状态退出回归。2224的178.67°有效旋转来自原源踝约178.6725°和pose root约176.03°大转向，不标为IK新增。候选是固定提交及实际SHA，当前生产已由 `48827a93e` 精确恢复552的六份旋转/采样文件，18e来源修正保留。

见[同一业务HTML](live-rotation-response.html)、[双脚跨版本断言](pelvis-rotation-business-comparison.json)、[输入与装配失败身份](pelvis-rotation-provenance.json)。统一复现用 `run_native_goal.ps1 -Mode pelvis -CandidateCommits @('15894ee7b','8c0e878f8') -UnityInstance e852139597e42532 -ResultDirectory <独立目录>`，再运行 `compare_pelvis_reports.py --results <目录> --output <结果JSON>`；业务失败返回非零。首次双脚装配曾错误地把左侧查询标为Right，已改为input.Side并保留失败，没有放宽生产RequireValid。

未执行Native Slot历史Job、完整FBBIK或Replay。膝角为真实骨段和Goal距离下的几何需求，不是已求解骨骼。DOM检查覆盖左右脚58帧、三版、null曲线断点与失败标签；浏览器视觉验收未完成。

## Releasing 的 Native / 全身 Action 连续混合（12帧，脚端待接续）

2035～2046 已执行真实发布ACL资源（Run组10、Action组1）解码201根物理骨，正式推导2根虚拟骨，连续执行 Native Slot `EvaluateFrame` 的Clear/Blend/History/Publish，再执行历史2c与当前正式Action Slot完整方法。全身alpha由录制base衰减边界恢复；上游BlendStack计划器没有重跑。两段clip的scalar track均为0，未使用的Native参数明确不可用；不据此声称参数系统已验证。

历史版重现原踝：最大位置误差9.942548e-7m、旋转误差0；历史/当前203根骨的局部位置、旋转和缩放差均0。旧Action贡献和dense weight会再次乘alpha，当前贡献与实际骨骼alpha一致。2041旧Run占比0.468768，旧Action贡献0.282207429；当前Action贡献0.531232，提前选到真实Action曲线：脚高0.0232933685m、Contact0.8675726、Sliding、LockWeight0。该曲线由正式资源在原时间0.1s采样并正式绑定事件5517395441386351926，未复制旧Run样本。

暖机连续完整计算0B。首次d5版本实际在Mono提交大值类型时失败，`Passing an argument of size 10200`证据保留；Burst函数指针请求仍走native-to-managed回调而失败，没有掩盖Mono。来源窗口正式修改Job/Workspace/Runtime为预分配Native元素直接引用读写，并提交d6d6ab9f9；本次执行绑定其工作区SHA，且核对Git规范化内容相符。实际Stored/History/Scratch大小分别10200/10192/9800字节，旧Editor加载类型为9816/9808/9800；旧Editor的MVID仍cd6cec60，因此新类型与消费者统一从正式源码编译，不能拿旧加载程序集冒充当前源码。

见[该业务HTML](releasing-action-native.html)、[实际12帧结果](releasing-action-native.json)、[源/绑定SHA与提交关联](releasing-action-native-provenance.json)、[原Mono失败](releasing-action-native-attempt-mono-state.json)。复现使用 `pwsh -File Tools/FootPlacement.StoredPoseVerify/run_native_releasing.ps1 -UnityInstance e852139597e42532 -ResultDirectory <独立目录> -NativeCommit d6d6ab9f9`；入口执行完整正式函数，失败返回非零并保留结果。

本段没有Stored捕获，没有验证FootFeatures混合、物理source注册、Foot、Physics、FBBIK或Unity/Burst调度。原录制2041膝角由前帧约66.77°到约0.509°、目标伸展比105.835%，仍只是原采样异常，不是当前新脚端输出。2035的骨盆已有历史，后续不能用default春状态补齐；新Action事件进入后预测与路径必须正式重查。正式Runner未执行。HTML的12帧、交接来源、场景坐标、播放与滑块已做DOM检查，浏览器视觉验收未完成。

## 后续场景范围

按用户指出的 Landing / Releasing 伸直组织两个完整场景，不按私有函数或字段拆测试；完成一段后统一维护其 HTML 与提交。

| 场景 | 已有证据与需要回答的问题 | 完整检查与历史限制 |
| --- | --- | --- |
| Releasing 修复及历史回归（待实施） | 本轮已完成 12571～12585 连续历史对照。下一次候选需修正多余冻结，同时覆盖旧版跨边硬抬，不能把本轮缺陷复现通过当作修复通过。 | 保留作者参数，比较净空、恢复位移与可达性；补跑 4107 / 4127 / 4131 旧硬抬窗口。不能只删除冻结，也不能用旧位置的命中证明新位置无穿透。 |
| Landing 获得接触到释放 | 围绕 33765→33766 扩到完整接触过程；脚底约动 1.2 mm、身体水平约动 16.4 cm。先核对 Body 提交时序和坐标，再连续推进锚点、滑动/释放、骨盆与腿求解，找首次不可达来自哪一层。 | 正常着地保持支撑；检查接触稳定、离地/穿透、腿长余量、状态退出和权重。不能求解后平滑膝角却同时固定脚点；历史提前释放/骨盆硬约束带来的离地、掉权重与末端补高须纳入回归。 |

独立函数先执行几何与显式状态；候选改变脚掌姿态后，新的碰撞查询必须进入正式 Unity 环境。两段都先重现旧结果，再比较候选，不能把一次同帧几何复算当作连续修复通过。旋转导致脚掌跨边作为完整场景的输入和检查项；作者手调权重的生效语义保持明确，不靠自动改权重掩盖冲突。若当前脚约束、身体运动和净空不存在共同可行解，先列清必须调整的业务行为及代价，再决定方案。
