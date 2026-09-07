## 当前进度

### 控制层 2.1b 推进记录（2026-09-07 晚）

- 提交 `513c8fa7f`：`ThirdPersonCharacter.Control.Rules.asmdef` 显式引用补上 `UnityHFSM`（上次迁移漏项）；`movement-mode.state` 前缀提升为公共常量 `CharacterPresentationTrajectoryIntent.MovementModeStatePrefix`；新增 `CharacterMovementModeStateIdentities`，Projection 编译入口解析 `Definition.ControlModuleId` 对应控制合同，身份集合传入 `CharacterPoseTransitionRuleCompiler`，对带 `movement-mode.state` 前缀的 IdentityLiteral 做编译期校验（不命中构建失败并列出合法状态）。首战即逮住姿态图 `run-start-mode` 手抄 `RunStart` 错位。
- 提交 `c5aec070b`：控制合同七状态无 RunStart，删除姿态图 idle→start 规则的 RunStart 死分支（运行时行为不变），全量重建 Corin 产物。
- 提交 `8dca4cb82`：BuildAll 路径曾发布携带陈旧 SourceRevision 的 CorinFixedProgram，改走 `Tools/3C/Internal/Rebuild Gameplay Lab Assets` 全量重建修正；重建后 Projection/SimulationProgram 与已提交内容一致（构建确定性佐证）。
- 2.1b replay 回归：trace 基线 `f169da25` 重放两次，第一次建基线（semantic `38fc3af1…`，1492 帧），第二次对账 `matched:1492` 零偏差。proof 位于 `Temp/CharacterInputReplayProofs/v5/f169da25…/20260907-181036…` 与 `20260907-181515…`。2.1b 剩余：表现采样对照（motion 全链路含 Turn）。
- 提交 `2743a3589`：删除 TrainingEnemy 全链路（definition 既有无效、monster prefab、AI 目录、builder/菜单/Collector 死分支）；float32 变体靶子改为第二个 Corin（`NeutralCharacterControlSource`），launcher float32 校验从 Player+AI 改为 Player+Neutral；AI 重做分支合并后以正式变体回归。
- 提交 `d0274cb21`：ACL 输入侧缓存。`CharacterAclAnimationResource` 落 `BuildInputIdentity`，`Complete` 编码前计算 acl-group-build-input/v1 哈希并与已发布资源比对，命中即从已发布资源与 quality.json 重建 artifact 跳过整组采样编码；发布层沿用旧文件时补盖身份。验证：重建#3 全量编码并落身份，重建#4 `[ACL] Read/sample` 零增长、重建通过。

### 技能树化与事件层立项（2026-09-07 深夜）

- ZZZ参考包《技能与输入.md》补齐：技能=子状态机树（普攻12态/Branch18态/Rush·Counter·Evade14态）、连招为树内转移、长按／连按／帧窗口／事件条件清单、115类窗口zone、与当前实现的差距表；数据源transitions.json(844条)/states.json(105)/zones.json/actions(45)。
- 用户决策：连招接续归资产层（技能树内转移），控制层只发单一技能请求；不走"照抄ZZZ技能框架"或"一技能拆N图"路线——BTSMTL现有表达（SkillDefinition入口图+树内状态机+条件图）即为正式形态。
- 提交 `20067cf12`：Locomotion状态机整体移出RootTree（80820→75159行），MovingTurn源曲线Timeline安置到Action SM的None body；程序普查LocomotionInputMotion=0、StateMachine=0证明删除为纯资产清理；replay两遍matched:1492零偏差（proof 212224/212551）。
- 技能树化接线落地：控制合同Skills 7→3（Attack1-5合并为Attack），SelectAttackSkill连段硬编码删除，Attack请求直达；Definition入口指向Attack body图（3ae19d5e）；转移边与条件进程序（StateMachine=1、TransitionEdges=20、ops 512→622），正式重建通过（sync#6）。
- 技能树化回放在表现层失败：同实例段转移的新段Select因producer变化撞ActionAnimationPlaybackLifecycleRegistry所有权断言——确认语义变化缺少一等通道，立项7.7领域事件层（design第14节）：ActionSegmentChanged与presentation command同事务同信封发布，rollback随tick重放，表现层由事件驱动playback语义推进；用户指定先重构完再统一replay。

### Corin 集成接手记录（2026-09-07）

本批按用户新目标接入 BTSMTL、Timeline、PoseGraph、ACL、相机及相关姿态修正；最终验收对照 `D:/ZZZ_Dump/output/corin_replication/replication-guide/README.md`。下面历史总览不代表本批源码和产物已经验收，暂不批量更新任务勾选。Center 记录为 `9fa880d872184c41a3cc3c42f0bcc1c3`，工作目录为主目录。Timeline 本批接入提交 `0495e6425`，Skill 文件默认值修复提交 `93ad2a768`。

- 已提交 `375e78473`：Program 取消不再提前清空 Action、Slot、Workspace 的句柄，各 owner 执行自己的 Discard，修复异步 ACL 等待阶段的 stale 错误。
- 已提交 `9a55e32d1`：Gameplay Lab 在现有 ResourceModule 与 ProjectResourceInitializationAdapter 完成包、版本和 manifest 准备后创建 Session；显式引用现有 ProductStartupProfile。正式资源包启用原文件名地址规则，修复未初始化和 ACL 地址无效。用户已有场景布局、诊断引用和暂存修改没有纳入该提交。
- 已提交 `dd53eb275`：ClipSamplePlan 按后端比较来源，ACL 使用资源目录和组内 Clip 索引，原生使用 Unity Clip 身份；普通 Clip 与 Timeline Foot 消费者共用规则。
- 接入前新运行 Proof `20260907-015831-463-399e3fea8b7e40a8981a8032b66040d8.json` 与 `20260907-014235-098-9e8e0beb985e4e8382f99c2797e9343b.json` 同版本比较 1492 帧，matched=true、aggregate/逐帧差异均为 0，Console 无错误。两份文件位于 Unity 项目 `Temp/CharacterInputReplayProofs/v5/f169da25c67742aaafa0e9860ae4a230/`，仅证明输入与 Body 重复性，不证明所有动作、窗口或表现。
- Foot 诊断输入执行 1492 帧，实际保存 1491 帧，差额未关闭；采样 `Diagnostics/GeneratedFootSampling/20260906-174536-5f150a1c8b46457ca24c367aba0889ff/`。表现采样 `Diagnostics/GeneratedPresentationSampling/20260906-174602-b8f39caa0d9b41e9aa1860c9350400b6/` 共 1621 帧，晚于回放起点启动且含结束后帧，不作为完整回放窗口。报告长路径已修改为短目录，但正式 Host 在 staging 目录原子移动时报 AccessDenied，报告尚未发布。
- Timeline 业务接入取分支 `698eb44d` 的组合差异，修复其合并 main 时误回灌旧角色发射器的问题，按已提交 `421206da3` 接回共用内容发现、发射和独立 Frontend。保留主线已拆分的角色编译模块，CharacterSemanticTimelineEmitter 仅负责角色来源与 TreeClip 编译适配。Unity 和 Lab Editor 完整依赖源码构建均为 0 错误；新增独立播放大文件与共用发射器仍须继续按职责拆分。
- 正式 Corin 双 Target 构建 `1767cf3524b44704a29821d74be3f28d` 已结束，因 112 条动画过渡策略旧 producer 身份错误在 Projection 阶段失败，未发布当前组合产物。原始证据 `.codex-tmp/corin-timeline-products-1767cf35.json`。下一步完成正式作者策略迁移，不能恢复旧 producer 或放宽校验。
- 当前 v5 无改动 dry-run 的 7 条 Skill 写入已定位为 Codec 省略空字符串、读取返回 null 的差异；文件到快照边界修正后，同 documentHash `28f6c0b3ca9b49d67cb107065a6657877b5c0a68073c483d03ee4b475fa18143` 的 dry-run plannedDiff=0、Clean。证据 `.codex-tmp/corin-document-noop-fixed-20260907.json`；未执行 apply。Blend Policy 正文当前不在 Document 可编辑包，需要接入正式作者模块，不能直接改 YAML。
- 参考文档有 45 个重点动作页，当前 Corin Definition 只有 Attack1–5、DodgeBack、DodgeForward 七个技能入口；冲刺、反击、分支、长按及对应镜头不能用现有录制通过代替复刻交付。Center before 被主 Editor 占用拒绝且没有 RunId，主 Editor 始终保留。

更新于 2026-09-06（Asia/Shanghai）。本次任务记录核对基准为主目录 `D:/Unity_Project_1/3C` 的 `main` 提交 `cafb5ee4306b7668c7a4769ba25153c3c64ab99e`，已核对的 ACL 接收修正到 `466967dff02b92858b5401c50c802214473105c4`。本次分别记录清单关闭状态、代码交付与正式构建证据，不把旧生成组当作当前候选，也不把部分代码落地当作整项完成。

本文件是本 change 的当前执行进度；`baseline.md`保留实施起点的历史身份。勾选只表示该条完整要求已满足；未勾任务另外写明“已实现，待验证”“部分完成”“尚未完成”或“待验证”，不再用同一个空框掩盖不同阶段。静态审查、源码编译、产物发布和Replay分别记录，不互相替代。

按当前清单实际勾选统计：共66项，7项已勾选，59项未关闭。已勾选项是1.2、1.3、2.1及Agent任务10.1、10.2、10.5、10.6；后四项仍明确保留整链复核或最终对账条件。未关闭项包含已实现待验证、部分完成和未完成，不能据此计算实现完成率。下面各条的历史状态说明仍须随业务提交更新，不能仅修改总览数字。

### 本轮确认的进展与剩余工作

- 公共根、入口与缓存身份校验已有`31f111d7e`、`8f3294561`及`6bcf16aa2`交付；ACL接入已有`dc7e720bc`，`b2d0e0d09`补齐资源配置入口并将组合Projection升级为`v15`，`466967dff`修正Unity可选资源读取。254项ACL来源路径均存在；244项与原固定索引相同，10项为公共Build、受保护运行时、Projection版本和明确资源补丁的接收差异。这是来源范围核对，不是整个接收组合已通过运行验收。
- 实施报告本轮Runtime、Editor源码构建均为0错误，并按要求关闭build servers。正式Unity构建job `25a237be85b34263b2704289e0de245e`在主实例`e852139597e42532`返回19条`PresentationProjection`错误，原因均为当前Sampling Rig或Calibration Preview Pose的几何验证identity过期；尚未证明具体哪个输入变化。原始返回证据保存在`D:/Unity_Project_1/3C/.codex-tmp/bts-acl-main-build-25a237be.review.json`。
- 此前成功返回的Projection仍为`v13`、PosePlan `v24`、PoseRuntime `v27`，不能作为当前源码Projection `v15`、PosePlan `v25`、PoseRuntime `v28`的构建证据。Float与Fixed也尚未形成当前版本的同组发布；12.3保持未关闭，完整Replay仍未通过。
- 普通重构继续按原任务收口，包括装备入口迁移、规则热更与产品装配、作者规则和技能工作区、旧路径删除、中央职责迁移及最终结构审查。相机公共命令交接、公共诊断来源修复继续按已确认边界推进；Timeline内容和Agent Document仍由各自任务负责。脚部几何问题只阻塞相关产物构建，不能作为整项重构停止的条件。

### 已有代码交付与剩余边界

| 工作块 | 已有交付与审查 | 尚未完成 |
|---|---|---|
| C#控制合同与接线 | 显式State/Transition、typed状态和唯一模块目录；Corin规则经两Target进入同一Evaluate | 原输入/移动时序比较、所有旧作者入口清理及完整装备迁移 |
| Action与技能实例状态 | `6db20c5f2`共用Activation/Commit/Lifecycle、SlotMap和实例状态管理；`ce72111f2`修正窗口Fact/Trace来源；`1b80885e3`、`c26a15721`和`0a4bc76b3`已接入参数化子图调用frame及按值输入/成功输出边界 | 合法并发/容量、完整停止与恢复运行证据 |
| 编译器职责拆分 | `b419fcdb9 → 770ecfc51`迁出Blackboard声明/状态及领域绑定；`3e1355410`迁出技能目录；`51aed878f → be440e2f5`迁出控制合同发射；`adc29ea30`迁出Timeline/TreeClip编排；`705f01d82`迁出Action/Behavior目录；`bf1289be3`统一Asset/Node来源；`ae28b8998`迁出Equipment目录；`147556f2f`迁出Input目录；`0c416ac24 → a23eb2ae8`迁出Tag/Attribute目录与协调器；`9a036e8ad`迁出全局状态；`475cfa9d0 → 51b3a07c0`按领域拆分节点登记与目录绑定。各模块已参与Editor/Frontend构建，局部代码对照确认原语义保留 | UI能力／Emitter／Target支持集一致性报告；解释器本体的大类拆分与作者/发布职责 |
| 状态与网络身份 | `121ec4a49`、`6dcdfd82a`、`304d83880`等已迁移codec/source/skill/generation及恢复读取边界 | 当前完整控制/技能状态的checkpoint、Rollback及输出对账Proof |
| 当前Corin产物发布 | `00c47f3c`统一两Target与Projection的一次Publish，`31ac3821`提交过正式产物；后续控制、Timeline及Action目录拆分、公共 Projection ABI v15 后，旧产物身份不再是当前候选，12.3待正式重建 | 本组实际运行和Replay比较仍归13.2；不等同于全部产品发布完成 |
| 作者工具、Document、发布 | 规划边界已明确；Control.Rules程序集与部分产品装配已有代码 | 作者模块/工作区、唯一Document v5、旧schema删除、热更发布和全部产品装配仍有实质实施工作 |

### 历史构建与回放证据

本节保留先前迁移阶段的检查记录。当前候选以“本轮确认的进展与剩余工作”为准；旧版本构建、引用检查和曾经关闭的任务不能替代新版本的验收。

- Runtime源码编译由实施任务报告通过。当前按要求构建`ThirdPersonClient.Editor.csproj`及其新增语义模块，结果为0错误；输出中的警告均为现有依赖或既有字段警告。主审已确认各新Emitter、节点登记模块、目录绑定模块及共享BehaviorCatalogFields均进入实际Editor工程，正式Frontend的上一次有效产物仍为6ca8e09a7。Editor编译、构建调用与产品receipt的完整日志归位仍由13.1收口。
- 正式资产操作在项目`D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`的主Editor执行。首轮Gameplay Lab重建因旧端点字段抛异常，已由`c3b2e3ed0`修正。后续唯一重建请求虽CLI超时，日志后来明确出现`Shared Gameplay Lab synchronized`；不能继续记为“尚未执行完”，也不能据此直接判定产物一致。
- 18:17核对曾发现新Fixed与旧Float32语义不一致。有效修复代码在`00c47f3c`：同一`CharacterSimulationBuildOrchestrator`请求同时声明Float32和Fixed Target，沿原原子发布组生成Projection；后继`f56064f3`与其Git tree相同，不另算代码进展。正式产物于`31ac3821`提交，之前的混版状态已关闭。
- 控制模块迁出Builder的首步`51aed878f`将控制状态SourceMap误写为普通StateSlot；`be440e2f5`已恢复ControlState，Builder通过显式来源种类保留通用写入。主审对照迁出前代码确认目录字段、状态默认值、顺序及State/Transition来源身份保留，并在当前`Editor.log:44118`看到修正后的正式重建完成，调用栈为`GameplayLabAssetBuilder.cs:149`。实施报告Console为0错误。首轮错误来源类型生成的临时产物不作为交付；下列Program身份已恢复并由主审直接读取核对，根资产更新见`bb41cb4ca`。
- Timeline编排迁移`adc29ea30`保留同一Track/Clip注册表、EmissionSession及Builder；主审对照父提交确认TreeClip入口为Enter/order 0，OnEnable为Enter/order 1，OnDisable与OnDestroy分别为Exit/order 0和1，嵌套CompileGraph继续传递原stateScopeOwner。原中央Timeline与生命周期方法已删除；`bf1289be3`将Node/Asset来源收敛到`CharacterSemanticSourceFactory`。本轮正式重建完成日志为此前`Editor.log:45734`，调用栈为`GameplayLabAssetBuilder.cs:149`；此处只确认代码编排与构建身份，不作为Replay行为通过。
- Action/Behavior目录迁移`705f01d82`保留action/behavior稳定ID、版本3/1、Required/Block/Cancel下的All/Any/None字段、ActionRequestBuffer及ActionInstance状态归属和声明顺序。原CompileActions/CompileBehaviors及公共BehaviorFields已删除，Action/Behavior/GE实际共用CharacterSemanticBehaviorCatalogFields；`bf1289be3`之后Action、GE、目录Compiler共用唯一Asset来源工厂。
- `ae28b8998`迁出Equipment目录，`147556f2f`迁出Input目录，`0c416ac24 → a23eb2ae8`迁出Tag/Attribute目录，`9a036e8ad`迁出全局状态；这些模块仍写入同一个CatalogIndex与Builder，原中央分支已删除。`475cfa9d0`及`51b3a07c0`又把Root/State/Timeline、Input/Blackboard、Action、Camera、Gameplay、Equipment、Motion的节点登记按领域拆开，并把目录绑定拆成共享目录引用、Input、Equipment、Action、Gameplay模块。当前仅有源码构建证据，最新拆分后尚未重新正式发布产物。
- 先前有效两Program均为compiler/24、ProgramId `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`、SourceRevision `ba8dcda4ece0f958a5b78cd6f2ad1d2ee8b0ca0b2c5f1efb3a0a31701cb7c68b`、SemanticHash `fe5bbbd7c89aec250a7d0eabbd24ea7392bbd964a6b9bb3ce8e19e059428e969`。Float32 ProgramHash为`00ca45fd7f86f03516b30aef09edfaa4bbccb99291c9d9a2521bc28cf06328e9`，Fixed为`63f7ee3716847d7ebd3c1458fad3a96f16c5c9a1f65c24d6e1d111c7a23e1091`；Target ProgramHash不同是正常数值/布局差异。该身份是历史产物记录，不能替代本轮正式重建。
- 主审此前核对三个Variant：LocalFloat32引用Program GUID `5740a6cfbfb0fe542ad6a6cb66fe1a80`，LocalFixed及Rollback引用`91063668fd0eaa84d9b7d688aadbc90a`，三者均引用Projection GUID `f365735adcfd49c4e96070df6bcd3bc4`。实施当时报告同组Projection与Launcher Validate完成，代码审查确认原统一Publish检查路径；该历史结论曾关闭12.3，当前ABI变化后12.3已重新打开，13.2也未关闭。
- 本轮Corin Replay尚无完整通过证据。历史输入/Proof的精确路径见`baseline.md`，其中比较帧数为0的旧Proof不作为本轮回归通过。
- 下一份可交付验证结果应包含对应任务号、代码提交、正式入口/日志、实际产物身份及Replay运行/比较位置。遇到一次请求超时先查该请求最终结果，不重复发起有副作用的构建，不手改生成资产。

### 执行与沟通规则

- 实现依据本change的proposal、design、delta specs和任务要求自主推进，继续中文小步提交，并随业务提交更新本文件对应任务的状态、证据和剩余条件。已明确的职责迁移、重复代码清理和验证由实现直接完成。
- 常规进度写入本文件，不按类或提交向规划窗口发送完成消息，不等待逐项审查或同意。规划窗口按文档和代码查看进展，不用消息往返代替任务记录。此前逐小步汇报的安排停止使用；小步提交不等于小步汇报，也不要求每次拆类都重建完整资产作为继续开发的门槛。
- 只有文档存在影响实现的歧义或缺口、现有正确业务或其他owner接口与设计冲突、需要改变既定职责/合同或必须绕过现有系统时，才向规划窗口提出具体问题。问题一次说明相关任务、代码事实、无法自行确定的决策及业务取舍；其余明确且不受冲突影响的工作继续推进。常规代码错误和构建失败按已有链路修复，不自动转成规划审批。
- 整个change完成后统一提交任务结果、结构迁移/删除清单及构建/Replay证据，再进行整体审查。用户询问进展时直接读取当前任务记录和代码；除上述不确定事项及最终交付外，不主动发送例行进度消息。

只有条款全部满足才勾选，不能通过删掉验证要求或改小范围来取得完成状态。静态结构审查与构建、Replay行为证据继续分别保留。

## 1. 固定迁移基线与边界

- [ ] 1.1 记录当前工作区差异、有效Definition／Composition／Target／Program／Projection和既有输入trace，交付保护清单与可重建基线；明确TrainingEnemy等既有无效目标，不通过修补它们取得基线。

  当前状态：**部分完成**。基线文件已记录工作区、受保护资产、两Target及输入身份；迁移前完整源码/外部依赖和正式receipt证据仍未闭合。此缺口不重新作为已授权实施的开工门槛。
- [x] 1.2 沿角色RootTree、状态机、Action和装备Route列出“C# Locomotion State／Transition、技能请求规则、技能内容、删除”的业务映射，明确外层动作状态机直接拆除，交付每个正式入口及引用的迁移表。

  当前状态：**已完成**。源到目标的Locomotion、Transition、7个技能入口、Timeline/TreeClip/窗口映射见baseline.md；这是迁移映射完成，不是后续行为验证完成。
- [x] 1.3 对账设计中的33项capability及预览、Pose、装备后续change的共享接口，交付明确替换／保留项；同步接口描述不接管外部change功能。

  当前状态：**已完成**。baseline.md已交付33项capability的替换/保留与领域所有权矩阵；后续并行变更的最终组合安装仍由13.3对账。
- [ ] 1.4 用既有Replay／Proof与业务观察确定同版本重复性和跨实现比较字段，交付输入／Body／动作阶段／输出基线及来源映射规则；不得以不同ABI的StateHash直接判定回归。

  当前状态：**待验证**。已有trace f169da25c67742aaafa0e9860ae4a230及旧Proof 52d757e59fb841f9968feba2eab66461；该Proof的baseline_available=false、比较帧数为0，不是本轮回归通过。

## 2. 角色与技能基础合同

- [ ] 2.1 定义C#显式StateMachine／State／Transition及角色控制模块合同，覆盖Enter／Tick／Exit、来源／目标、纯条件、优先级和稳定顺序、输入／结果、参数、typed状态schema与代码版本；通过既有登记／组合校验交付唯一模块目录。

  当前状态：**部分完成（2026-09-07 收回已完成判定）**。cbcd7fa42、c2d9a203d及规则装配提交已提供自研显式State/Transition、只读条件端口、稳定选择顺序、typed控制状态、版本和唯一CharacterControlModuleCatalog，Runtime编译及正式Frontend构建已有成功证据。但Corin控制状态机（Idle/WalkStart/WalkLoop/WalkEnd/RunStart/RunLoop/RunEnd及迁移）当前仍寄宿在`CorinPlayableRootTree.asset`的图资产里、经Discovery从树发现后发射——即设计明确禁止的“重新生成成角色状态机图”形态；Turn控制状态缺失；姿态图迁移字面量因手抄身份与运行时fact错位（旧GUID）导致locomotion动画恒idle，已修复（c262837fb，采样证实状态机全链路恢复、replay 0帧偏差）。2026-09-07新增实施载体决定（design第2节）：控制状态机运行时骨架改用项目fork的UnityHFSM（嵌入包`Packages/UnityHFSM`，remote=nikaakaa/UnityHFSM），时序由仿真Tick注入。剩余工作：库改造（剥float秒/Unity时间、tick注入、typed观察包、删OnKey/OnMouse/协程/可视化）、现有自研合同映射到库骨架、Corin控制模块含Turn迁到库上、状态身份改为代码常量供姿态图编译期引用。

- [x] 2.1a 将fork的UnityHFSM改造成确定性控制骨架：计时全部改为注入的仿真Tick（整数），删除/隔离Unity时间与输入耦合成员（TransitionOnKey、TransitionOnMouse、CoState、ParallelStates、Visualization、Samples），迁移条件改为typed观察包谓词，双Target复用同一实现；交付改造清单与两端构建证据。

  当前状态：**已完成(库改造部分)**。fork提交a078f74:新增`TickClock`(拥有方每逻辑tick推进),`Timer`/`ITimer`改为整数`ElapsedTicks`,`TransitionAfter`/`TransitionAfterDynamic`的delay改为tick数;`StateMachine`持有clock并在AddState/AddTransition自动绑定,嵌套机器经`ITickClockHost.AdoptClock`共享父时钟。删除CoState/ParallelStates/TransitionOnKey/OnMouse/Visualization/Samples~/Tests,asmdef设`noEngineReferences:true`(纯C#程序集,Version 2.4.0),Editor编译0错误。typed观察包谓词即库原生`Func<T,bool>`条件,由2.1b的Corin模块提供;双Target同程序集消费证据随2.1b的replay回归交付。
- [ ] 2.1b 将Corin控制状态机从`CorinPlayableRootTree.asset`迁移到C#控制模块（UnityHFSM骨架）：七状态+Turn、迁移条件、每状态Timeline绑定与Locomotion参数（MoveSpeed/DurationSeconds等）进正式控制合同；状态身份输出为代码常量（`presentation.movement-mode.state/<Name>`），姿态图迁移字面量改为引用该常量并重建产物；交付replay回归（trace基线f169da25）与表现采样对照（motion状态机全链路含Turn）。
- [ ] 2.2 定义SkillDefinition、入口签名、ActionProfile引用、子图依赖及允许的后续候选，交付相同策略被多个技能引用时仍可精确选中技能的作者／校验结果。

  当前状态：**部分完成**。a47532948、ceb50eb9e已接入SkillDefinition、ActionProfile引用和精确SkillId/入口目录；参数化子图签名、依赖及允许的后续候选尚未完整交付。
- [ ] 2.3 明确ActionInstance与SkillExecutionState的唯一owner关系，交付Context、模板、实例、调用点和generation的typed地址及生命周期合同，删除第二生命周期候选设计。

  当前状态：**部分完成**。d90da602b、6db20c5f2、ce72111f2已使ActionInstance拥有技能局部状态，并修正实例/generation与窗口来源；独立子图调用frame和全部调用点地址仍缺，不能用一次技能实例frame代替它们。
- [ ] 2.4 定义角色运行包中的控制binding、SkillProgram目录、组合布局与显式容量，交付缺失模块、非法依赖、并发／容量不符的正式诊断。

  当前状态：**部分完成**。控制binding、技能目录、typed布局及共享执行aggregate已接入；显式并发容量、容量错误与完整非法依赖诊断仍未闭合。

## 3. 语义与Target编译

- [ ] 3.1 将角色组合Discovery改为读取控制合同和技能闭包，迁移原Character／Equipment graph roots；通过正式Frontend报告验证根目录唯一且旧角色root不再生成。

  当前状态：**部分完成**。ceb50eb9e已将Corin Discovery接到C#控制合同与技能记录；旧Character/Equipment作者入口和编译注册尚未全部删除，正式根目录唯一性报告未交付。2026-09-07核实：控制状态机本体仍在`CorinPlayableRootTree.asset`（七状态State Body、迁移规则图、4个LocomotionInputMotion节点），Definition仍引用`m_RootTreeAsset`；2.1b迁移完成后此处需删除树内控制authoring与树发现分支，根树瘦身为纯Skill入口（见4.1的RootTree调度清理）。
- [ ] 3.2 按设计12的职责迁移表，从CharacterSemanticEmitter迁出技能节点业务族及变量／装备／GE绑定发射，复用唯一操作目录与typed端口；CharacterSimulationProgramBuilder仅保留通用IR写入／索引／一致性约束。交付模块输入输出、实际调用链、中央分支删除清单及UI能力／Emitter／Target支持集一致结果；Timeline发射按已分配的owner接口接入。

  当前状态：**部分完成**。b419fcdb9→770ecfc51、3e1355410已迁出Blackboard声明/状态/作用域、领域绑定和技能目录；51aed878f→be440e2f5迁出控制合同；adc29ea30迁出Timeline编排并通过窄回调复用Graph拓扑。705f01d82进一步把Action/Behavior目录、标签条件及动作状态槽发射移入CharacterSemanticActionCatalogEmitter；`bf1289be3`统一Node/Asset来源工厂；`ae28b8998`、`147556f2f`、`0c416ac24 → a23eb2ae8`、`9a036e8ad`分别迁出Equipment、Input、Tag/Attribute及全局状态目录。`475cfa9d0 → 51b3a07c0`将节点登记和目录绑定继续按领域拆开，所有模块仍写入共享CatalogIndex与Builder，中央业务分支已删除。各模块进入实际Editor构建，局部对照确认语义保留；UI/Emitter/Target能力一致性报告、其余作者目录和最终发布证据尚未完整交付。
- [ ] 3.3 接入子图输入／输出签名、调用点及occurrence绑定，交付完整引用链与类型校验；子图递归被拒绝，显式Loop及跨技能候选分别验证。

  当前状态：**已实现，待验证**。`1b80885e3`接入Graph signature、occurrence CallFrame、IR/两Target codec及类型绑定；`c26a15721`建立SubGraph动态输入/输出值端口并将未连接输入转为正式默认常量，`0a4bc76b3`接通编译布局；递归拒绝沿既有Discovery的`graph_cycle`路径保留。显式Loop、跨技能候选及运行行为仍待Validator/Replay核对。
- [ ] 3.4 迁移Tree／Timeline／TreeClip／局部状态机发射和状态声明，保持Decision／Commit及停止顺序；通过正式IR Inspector和source map核对对应关系。

  当前状态：**部分完成**。既有Tree/Timeline/TreeClip/局部状态机继续经过同一IR，6dcdfd82a等已迁移精确实例来源；adc29ea30将Timeline编排与TreeClip生命周期发射从中央类迁出，`6f7216303`又将边、状态生命周期编排交给独立GraphFlow emitter，代码对照确认入口/启用/停用/销毁顺序、状态owner及EmissionSession.Complete顺序保留；`bdedba175`将状态机运行、状态切换和状态停止迁出OperationControlRuntime。局部状态机调用参数、状态声明及IR Inspector完整核对仍未完成；本步未改公共Timeline运行或另建发射注册表，公共部分继续按独立owner接口对齐。
- [ ] 3.5 将C#控制参数／state合同、GE／Equipment／Body Motion描述及技能目录纳入同一IR／角色运行包，交付canonical identity与依赖闭包结果。

  当前状态：**部分完成**。ceb50eb9e及后续目录发射提交已写入控制合同、参数/state、Body Motion和技能目录；51aed878f→be440e2f5将控制描述与来源发射集中到独立模块，`05a7471ae`、`d344670ac`又把控制配置解析和默认参数收进正式Contract/IR链。`705f01d82`之后Action、GE、Equipment、Input、Tag/Attribute和全局状态目录继续共用同一IR；Action与GE的Behavior字段来自同一实现，节点绑定也经共享目录引用模块进入同一Builder。当前正式重建的canonical SemanticHash保持fe5bbbd7c89aec250a7d0eabbd24ea7392bbd964a6b9bb3ce8e19e059428e969，Float32/Fixed身份已直接核对；最新拆分后的正式产物及GE/Equipment完整依赖闭包报告仍未交付。
- [ ] 3.6 分别完成Float32与Fixed降低、组合state layout、SkillProgram与codec升级，交付同语义双Target构建和旧ABI拒绝结果；列明两端保留差异的数值／存储／编码原因，控制与Action业务流程必须调用共享实现，不能复制后要求同步维护。

  当前状态：**部分完成**。两Target的技能状态、Graph CallFrame、动态Value端口和codec已有实现；`00c47f3c`、`31ac3821`完成同一IR的Float32/Fixed构建和产物身份核对，`1b80885e3`、`c26a15721`、`0a4bc76b3`继续保持两端同一调用合同。旧ABI拒绝及完整Target能力一致性结果尚未收口。
- [ ] 3.7 更新既有.csir／.csim store、wrapper和原子发布组，交付精确重读及混版拒绝结果；Projection只迁移技能producer来源，保持当前Pose实现。

  当前状态：**部分完成**。00c47f3c改为一次正式Publish同时生成两Target和Projection，31ac3821已提交本轮同组产物并核对实际引用；完整store重读、混版拒绝及发布失败边界的结果仍待整理，不能将有效组构建成功扩大为所有失败条件已验证。

## 4. 技能解释器与实例状态

- [ ] 4.1 将现有控制解释器的正式使用范围收至技能，从OperationControlRuntime按组合控制、局部状态机、执行范围生命周期迁出职责，保留唯一分派／状态／调度入口；迁移调用者并删除角色RootTree调度及重复启停分支，交付模块输入输出、实际调用链和删除证据，不以partial或转发壳替代拆分。

  当前状态：**部分完成**。C#控制/技能入口及实例作用域已迁移；`bdedba175`将局部状态机运行从OperationControlRuntime移入OperationStateMachineRuntime，`f56974a63`又将激活、停止、强制释放和执行范围完成移入OperationExecutionLifecycleRuntime，`603a5951e`进一步拆出组合节点执行，`cc1eb16f6`和`0a4bc76b3`接入SubGraph调用入口及调用帧。OperationControlRuntime保留组合遍历与唯一公开Cursor/分派入口。角色RootTree调度、重复启停分支及旧作者入口仍未清理。
- [ ] 4.2 接入ActionInstance拥有的节点、Timeline、局部状态机和等待状态，交付跨Tick字段清单及既有状态coverage校验结果。

  当前状态：**已实现，待验证**。d90da602b→6db20c5f2已接入ActionSkillExecutionFrame/Aggregate/Manager及两端state/layout/codec；跨Tick覆盖清单和现有coverage/运行证据尚未完整交付。
- [ ] 4.3 实现子图按值入参、声明返回值和中止不提交输出的调用frame，交付多个调用点复用同一子图时的独立地址与运行诊断。

  当前状态：**已实现，待验证**。`1b80885e3`建立调用点、输入/输出绑定、IR/Target codec与拓扑索引；`c26a15721`将SubGraph值端口按调用frame签名解析，并编译未连接输入默认值；`0a4bc76b3`在两Target的控制Tick前写入输入，成功后读取输出，失败/停止时以默认值隔离旧输出；`6b94d16f`在重复激活前重置输出状态。多个调用点通过occurrence route拥有独立state slot，完整运行诊断和中止/并发覆盖仍待验证。
- [ ] 4.4 接入合法并发释放、重复激活与调用generation，交付模板共享、实例隔离和容量失败的现有Runtime／Validator结果，不增加对象clone。

  当前状态：**部分完成**。6dcdfd82a、304d83880、6db20c5f2已加强重复激活与实例/generation隔离；`1b80885e3`及后续调用帧按occurrence保持状态地址隔离，重复激活时输出状态也会重置。当前仍有单active Action查找约束，合法并发、容量边界及完整运行结果尚未闭合。
- [ ] 4.5 迁移技能变量与Frame投影，角色控制字段通过只读事实暴露；交付已删除Character Blackboard输入镜像、无跨实例写入的引用及布局检查。

  当前状态：**部分完成**。技能局部slot已禁止缺实例时回落全局状态，ce72111f2修正Frame窗口发布来源；角色输入镜像清理、变量作用域完整迁移及全量布局检查未完成。
- [ ] 4.6 接通父级Complete／Cancel／Interrupt／Reject／Abort／teardown对全部子图与Timeline的停止，交付graceful进度、force释放和重复停止的生命周期事实。

  当前状态：**部分完成**。6db20c5f2已统一Action生命周期并将停止处理放入精确实例作用域；全部父/子图/Timeline的graceful、force、重复停止和teardown尚缺完整运行证明。
- [ ] 4.7 对技能内Motion、GE、Equipment与Presentation叶子收敛唯一请求／输出端口，交付无Transform、播放器、WorldSolver或网络旁路调用的定向检查。

  当前状态：**部分完成**。现有Motion/GE/Equipment请求端口继续复用，控制输出通过正式端口进入同一Step；所有领域叶子的迁移清单、依赖/旁路定向检查尚未完整交付。

## 5. C#角色控制迁移

- [ ] 5.1 将有效角色的Gameplay Locomotion迁入C#显式State／Transition，保留原输入、数值与同Tick转换顺序；通过既有业务观察比较核对控制状态、Body／Intent时序，不复制Presentation Pose State。

  当前状态：**已实现，待验证**。Corin显式Locomotion模块及两Target适配已有提交，控制代码仍在原Evaluate内执行；同输入数值、Body/Intent与同Tick转换顺序尚未完成回放比较。
- [ ] 5.2 控制层只承载输入消费与单一技能请求（Attack／Dodge请求直达技能树），允许State保持active时请求技能；连段与取消接续由技能树内转移边条件（资产层ConditionRule，含ComboAccept／Recovery窗口）驱动，不在控制代码中复刻连招；删除外层动作图调度与控制层连段选择逻辑，不新增角色总状态机或C#技能阶段镜像，交付输入到精确Skill请求的诊断链。

  当前状态：**已实现，待技能树化回放验证**。控制合同Skills收敛为Attack／DodgeBack／DodgeForward，SelectAttackSkill连段硬编码已删除，Attack入口指向连招树（Attack body图），段转移边与条件进程序（StateMachine=1、TransitionEdges=20），正式构建通过；技能树化回放在表现层ActionAnimationPlayback所有权断言处失败，由7.7事件层承接修复。
- [ ] 5.3 将FixedActionRuntime／Float32ActionRuntime中的准入、来源检查、replacement／stop barrier、输入消费、请求暂存、最终提交及生命周期转换收敛到一份共享业务实现，Target只保留必要状态／数值适配；复用唯一Required Tag、TargetRequirement及目标快照规则。交付两端调用链、重复分支删除清单，以及纯查询／最终提交、既有同Tick顺序和实例身份的验证结果。

  当前状态：**已实现，待验证**。6db20c5f2已共用Activation/Commit/Lifecycle及实例管理，ce72111f2补齐窗口来源发布；两端重复业务分支删除和小步静态审查已有证据。纯查询/最终提交、同Tick顺序与实例身份仍需本轮运行结果闭合。
- [ ] 5.4 接入显式replacement及source stop barrier，区分独立并发请求；交付来源、退出原因、停止进度和新实例建立顺序的事实。

  当前状态：**部分完成**。显式replacement与source stop barrier已有代码，停止scope已修正；独立并发请求、退出原因和新实例建立顺序尚无完整现有运行验证。
- [ ] 5.5 使当前Decision窗口在同Tick角色决策前可读，连段／取消窗口条件由技能树内转移边条件消费；通过既有Replay业务事件核对不额外延后一渲染帧。

  当前状态：**待验证**。Evaluate保持Decision窗口先于技能树内条件求值，ce72111f2保存窗口来源至最终发布；尚无当前候选的Replay事件比较，不能据代码顺序直接认定行为一致。
- [ ] 5.6 保持AIIntentProgram与CharacterSimulationInput边界，更新只读输入合同引用；交付AI不访问控制／技能私有状态的依赖与Validator结果，不修复TrainingEnemy资产。

  当前状态：**部分完成**。当前AI仍通过正式Character输入接入，未接管AI运行；AI插件替换由其独立任务负责，本项输入合同、私有状态访问和组合规范仍需最终对账。

## 6. 装备核心接入

- [x] 6.1 将Feature Persistent／Route角色图入口迁成代码binding与Skill引用，更新作者目录及IR；交付稳定Slot／Route／Feature／参数身份和旧Host opcode零引用结果。

  当前状态：**尚未完成**。Equipment Persistent/Route/Host作者和编译分支仍存在；当前只保留并修正其正式catalog绑定，没有完成向代码binding/Skill引用的整套迁移。
- [x] 6.2 保留装备Begin／Commit／Cancel事务、Tag／Effect贡献及Feature generation，将控制状态接入同一typed布局；交付既有事务和上下文合同的验证结果。

  当前状态：**部分完成**。既有Equipment事务、贡献和上下文保留，控制typed状态已新增；两者完整整合及现有事务验证未交付，不把“没有改坏旧逻辑”计为本项完成。
- [ ] 6.3 更新Action Equipment Context、参数查找和已有效装备数据的调用者，交付精确Skill绑定与snapshot覆盖；不补做装备样例或网络装备业务。

  当前状态：**尚未完成**。Skill与Action的基本身份已接入，但有效Equipment路由到精确Skill、参数和snapshot闭包尚未完整迁移；仍不补装备样例或网络装备业务。

## 7. Session、状态与网络恢复

- [ ] 7.1 在原Evaluate／WorldResolve／Finalize Step内接入控制模块和技能解释器，交付原四阶段、多Tick与Commit入口的调用图及Pipeline编译结果。

  当前状态：**已实现，待验证**。c2d9a203d及后续提交已在同一Evaluate依次执行C#控制和技能，再走原WorldResolve/Finalize；两者共用Actor/Tick transaction。完整Pipeline编译及当前候选运行证据尚待交付。
- [ ] 7.2 扩展两个Target的状态transaction、copy、codec和hash，覆盖控制State identity、必要进入Tick／转换进度／输入缓存、ActionInstance、子图frame、参数、Timeline及停止进度；恢复直接还原数据，不重放Enter／Exit或技能请求，交付完整状态schema与旧版本拒绝结果。

  当前状态：**部分完成**。121ec4a49、6dcdfd82a、304d83880、d90da602b、6db20c5f2已扩展两端codec/hash与控制/Action/技能frame身份；`1b80885e3`已加入Graph CallFrame codec/hash，`c26a15721`和`0a4bc76b3`接通动态端口与调用状态；完整状态覆盖及旧版本拒绝结果仍缺。
- [ ] 7.3 更新Composition、ProgramCatalog和模块装配校验，交付缺模块、混版、能力不足及不兼容Target在Active前失败的正式报告。

  当前状态：**部分完成**。模块目录、绑定版本和Program/State基础组合检查已落地；本轮产物混版已通过统一Publish修复。缺模块、混版、能力不足和Target不兼容的完整正式拒绝报告仍待交付。
- [ ] 7.4 更新ServerAuthoritative owner checkpoint、Full／Delta与Correction恢复，交付完整技能状态恢复及原Remote观察体边界的现有证明。

  当前状态：**已实现，待验证**。121ec4a49及来源身份提交已更新owner checkpoint、Full/Delta和恢复读取边界；尚无当前技能实例完整恢复和Remote边界的本轮Proof。
- [ ] 7.5 更新Fixed Rollback snapshot、history、分层hash与恢复投影，交付同输入重算中实例／调用状态一致的现有Proof；Relay继续只路由。

  当前状态：**已实现，待验证**。Fixed snapshot/codec/history身份路径已随新状态接线；本轮同输入重算与实例/调用状态一致性尚未获得Replay/Proof。
- [ ] 7.6 更新EventId来源、output disposition与state publish衔接，交付确认／替换／抑制及重复输出的既有诊断，保证代码来源不伪造Graph节点。

  当前状态：**部分完成**。代码/技能来源已使用typed Source，ActionWindow最终Fact/Trace发布不再晚读局部帧；确认/替换/抑制、重复输出与所有消费者还需完整运行对账。

- [ ] 7.7 按设计第14节接入逻辑到表现的领域事件层：定义 ActionSegmentChanged 事件契约（typed 实例／段／技能身份，复用 SimulationEventHeader 信封），Float32 与 Fixed 的技能状态机运行时在同一转移事务点发布；表现层 ActionAnimationPlaybackLifecycle 消费事件终结旧 playback 条目并推进新段 generation，所有权断言不再依赖命令形状推断技能组织形态；交付两 Target 同输入事件序列一致的对账与技能树化回放通过证明。

  当前状态：**部分完成（上半已交付）**。触发背景：技能连招迁入资产层后（Attack 单技能入口指向连招树），同实例段转移发出的新段 Select 因 producer 变化撞表现层所有权断言，确认语义变化缺少一等通道（详见 design 第14节）。上半：两 Target 契约 DomainEvent kind＋DomainPayload、NotifyStateTransition 转移点发布链（Fixed/Float32/AI/TimelinePlayback 全实现）、表现命令类型同步、delta spec 已建。下半a已交付：表现分发 DomainEvent 分流与四层透传、playback runtime 事件队列与 lease 内 flush、registry 按实例终结活跃条目（SegmentReplaced）与已终结条目 Select 重初始化、Fixed Unity 适配 DomainPayload 透传。下半b已交付：ActionInstanceState（Fixed/Float32）新增 SegmentGeneration 字段并进 state codec（允许 ABI 变化），NotifyStateTransition 转移事务点推进段代，TimelineTarget.EmitPresentation 将 playback generation 组合段代（段切换后 PlaybackId 天然分代，旧段迟到命令路由不到新生命）。剩余：①构建仍报 RunLoop_Inplace ACL 源孤立（Pose Canvas 迁移中间态：m_PoseSourceBindings 4 个负 fileID 断链、RunLoop binding 缺失，归 pose graph 重构 owner）；②Unity 编辑器脚本编译与磁盘内容不同步（需编辑器侧 Refresh/重启后重验）；③技能树化回放两遍 matched:1492。

## 8. 规则与数据发布

- [ ] 8.1 将共享合同、稳定解释器与可更新控制／技能规则按设计分程序集，交付单向依赖及portable规则不引用Unity／Fantasy对象的编译结果。

  当前状态：**部分完成**。ceb50eb9e、0cdb043dd已建立portable Control.Rules程序集及主要消费者引用；稳定解释器与可更新技能叶子规则的完整程序集/版本边界尚未全部交付。
- [ ] 8.2 将规则程序集接入现有HybridCLR构建、依赖、裁剪／泛型生成与启动加载，交付精确模块版本和现有资源发布闭包；不新装热更框架。

  当前状态：**尚未完成**。尚无本轮规则程序集进入HybridCLR构建、裁剪/泛型生成和启动加载的完整提交与验证证据。
- [ ] 8.3 更新Unity客户端／Authority与普通.NET Authority产品的规则模块和技能产物发布清单，交付相同语义版本、完整依赖及缺失模块拒绝结果；Relay产品不安装Gameplay执行。

  当前状态：**尚未完成**。已有部分Unity/Fixed/DotRecast规则装配；各正式产品的模块、技能产物和发布manifest依赖尚未统一交付。
- [ ] 8.4 接通新Session采用新代码／技能版本和活动Session版本锁定，交付正式manifest及加载状态报告，不增加对局中状态迁移、旧ABI读取或兼容开关。

  当前状态：**尚未完成**。既有Session版本锁定基础继续保留；新代码/技能发布后由新Session采用的完整加载与manifest结果尚未交付。

## 9. 技能作者模块与共享框架

- [ ] 9.1 将技能定义、Flow／局部状态机、Timeline／TreeClip、变量／参数和领域叶子的作者规则从中央类迁出，交付各模块输入输出、唯一Capability装配、实际消费调用链和中央字段／节点特例删除清单；UI、Document与Compiler复用相同局部语义，不把重复规则平移进helper。

  当前状态：**尚未完成**。编译发射器拆分不等于作者规则拆分；中央Capability、窗口及领域作者规则尚未完成本项迁移和重复特例删除。
- [ ] 9.2 将节点创建、配置、复制粘贴和端口变化统一接入现有Port Shape与typed Mutation，交付作者目录／Validator一致结果并删除重复字段表。

  当前状态：**尚未完成**。SubTree运行编译已复用既有PropertyPort/PropertyEdge身份并接入动态值合同；技能创建、配置、复制和作者侧动态端口的统一Mutation迁移及重复字段表清理尚未交付。

  共享作者写入接点（2026-09-06代码核对）：`SubTreeNode`只公开只读SubTree与端口集合，`NodeGraphReference`只投影引用信息；`SubTreeNodeView`仍在UI内遍历类型表、按名称拼输入/输出端口并直接增删。Agent创建Graph与配置引用需要消费本条的正式业务实现，不能另用反射、序列化字段名或私有端口类型表补齐。

  - 提供精确owner与slot的typed引用写入能力：按该slot实际允许的图类型和inline/shared模式设置或清除引用，正确维护serialized owner、graph与scope identity。复用已有模块setter并补齐SubTree节点缺失能力，不只增加只读描述或空接口。
  - 提供按稳定declaration/port identity完整替换输入与输出绑定的业务入口，覆盖新增、配置、删除、类型/方向校验、端口形状及受影响连接。UI与Agent复用同一参数类型能力和Port Shape规则，删除UI中的重复选择/构造逻辑，不以显示名或C#类型别名识别声明。
  - 共享业务实现不拥有Document、Undo、rollback、SaveAssets或Build生命周期。UI和Agent由各自既有事务调用相同实现；Agent继续负责整包顺序、符号、Mutation接线及事务。交付真实类型/成员、支持范围、实际UI调用链、必要提交和既有编译/Validator证据，再由Agent接入原dry-run/apply/reverse-export验证。
- [ ] 9.3 增加技能定义、签名与inline／shared子图编辑，交付从定义到Tree／Timeline／调用点的精确owner导航与原正式转换命令。

  当前状态：**尚未完成**。尚无技能签名、inline/shared子图工作面及精确owner导航的完整实施交付。
- [ ] 9.4 将角色入口改为代码控制binding／参数及技能目录，删除角色图创建菜单和无效页面；交付无假RootTree及无任意代码调用节点的能力清单。

  当前状态：**部分完成**。Definition已有控制模块与技能定义字段；旧角色图菜单/页面和相关能力仍未全部删除，不能据配置字段存在判定作者入口已完成。
- [ ] 9.5 保留AI／Pose共享画布及独立领域数据，实现窗口重载恢复、字段草稿保护和单次订阅释放；交付现有窗口状态／生命周期诊断，不在OnInspectorGUI执行重计算。

  当前状态：**尚未完成**。既有AI/Pose共享作者框架保留；窗口重载、草稿保护及订阅生命周期这一轮改进尚无实施/验证交付。

## 10. Document v5整包闭合

实施归属：任务 10.1–10.6 由规划窗口 `01a07206-ec83-74e3-866a-7ccb6a158217` 与唯一同目录实现窗口 `01a0720a-6105-72b1-bf2f-bbfeb6654773` 独立负责；原 BTSMTL 实现不再修改本节，最终集成与全链 Replay 仍由原 BTSMTL 实施负责。

### 执行与审查记录 AGENT-EXEC-20260906-01

协作按 COMM-20260906-01 改为文档为准。本节与 design 第 10、12 节是本任务的执行入口；实现将本范围提交、验证命令、原始证据路径、剩余问题写回本节，规划将审查结论与下一批要求写回本节。消息仅在需要调整执行、处理真实阻塞或提交完整交付时简短通知文档位置；普通编译、提交、收到、仍在等不发消息，不要求回执。对外关系仅由规划处理，实现仍只联系自己的规划，不改其它任务配对、模型、目录或共享代码所有权。

记录边界：此处只记录 AI 通过既有 CLI／MCP 执行的结果摘要、退出状态和证据路径，不新增用户手工操作、手工验收清单或测试任务。完整命令与原始 JSON 留在独立证据文件，本节不粘贴全量响应。用户端到端验收不写成 OpenSpec task；这一规则不禁止记录已经执行的自动化检查及其失败证据。

范围仍为 10.1–10.6 与 design 责任表中的 Agent 部分。输入是精确 Definition 的正式作者投影与完整 v5 目标；输出是唯一有序 typed Mutation 计划和同一资产事务结果。控制、Skill、Graph、StateMachine、Timeline、Blackboard、AI 与 Presentation 的领域规则各归内容模块，中央仅组织整包和跨分片依赖。保留已正确的 strict 解析、local 身份、只读 RootGraph、跨域拒绝与同一 Undo／保存／失败恢复／reverse export；不新增局部 apply、fallback、反射字段写入或替代运行器。

共享 SubTree／Graph 引用与参数绑定按已确认决定等待提供方完整交付：精确 owner／slot 的 inline／shared typed 写入、serialized owner 与 graph／scope 身份维护、按稳定 declaration／port identity 完整替换绑定、唯一 Port Shape，以及 UI 实际消费同一规则。未提交 SetSubTree 草稿和单个 setter 不算交付，不能复制或提前消费。Agent 继续独立的计划、预检、身份和事务修复，接口到达后由规划提供经审查的消费提交。

当前审查结论为 **CHANGES_REQUIRED**。已核对中央职责迁移、已有 StateMachine 新 State 创建修正、Graph／Node 分型写回、Preflight false 传播、已有 Shared setter 接回，以及排序的空 planned 引用过滤和邻接表预初始化。以上为源码检查结论；定向 `--no-dependencies` 编译不得称为完整依赖构建或 Unity 最新程序集已加载。

当前验证证据均位于仓库 `.codex-tmp/`，原文件保留，不覆盖旧结果：

- `agent-authoring-checkout-20260906.json`：实例 `e852139597e42532`，精确根 `Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`，恢复后 v5 Clean；documentHash 为 `6dd983e46105f46979a408a86616ea7edd62140f5520972bc340a38fcb175581`。
- `agent-authoring-dry-run-noop-20260906.json`：未改包却产生 7 条 `set_skill_definition`，目标为 Attack1–5、DodgeBack、DodgeForward。该往返差异尚未解释，禁止直接 apply。源码存在 `snapshot.skills = skills` 只能支持排查方向，不能证明运行实例采用了该代码，也不能排除字段值、null／空值或映射差异。
- `agent-authoring-dry-run-stable-20260906.json`：1 条节点改名加上述 7 条 Skill 写入，不能视作纯改名通过。
- `agent-authoring-dry-run-local-state-20260906.json`：root 声明、State、Transition 与 7 条 Skill 写入；无新 behaviorGraph 创建项，未覆盖新图内声明依赖后置 producer 的场景。
- `agent-authoring-unity-console-20260906.json`：force refresh 后 18 条共享 Fixed／Float32 Equipment `CS1061`，缺 `EntryOperation`／`PersistentEntry`；规划在 `d8d8481dae87a96e46111cd51f76c70b7a73ee91` 工作文件中核对旧调用仍存在。证据 SHA256 为 `6fd8ec9f4e5266ab1f826a27a8ecbaa82d6db5fb46c7068fb0ee98521d359ae1`。此为当前候选正式编译／装载依赖，已集中对接；Agent 不恢复废弃字段或修改 Equipment runtime。
- `agent-authoring-validate-20260906.json`：当前已加载版本 formal validate 失败，`presentation_projection_invalid`，compileFailure／semanticInvalid 各 1，消息指向 Foot geometry validation identity stale。保留此独立失败，不改 Calibration／Rig 来使 Agent 验证过关，不把历史 validate 成功覆盖当前结果。

下一批执行要求：先确认最新程序集的正式导入和加载，再获取无改动 dry-run 及两侧 Skill 字段对照；正常 no-op 不应出现未解释业务写入。字段对账必须列出实际 current／target 差异，旧程序集原因需要加载版本证据。新增 State 场景必须包含 local behaviorGraph 与归属于该图的声明，并以完整计划证明每个 producer 在 consumer 前，不能仅给 scenario 标签或计数。补齐缺失 producer、循环、Graph／Node 类型与删除依赖的正式诊断，按现有入口执行，不新增测试代码。

当前独立执行批次 AGENT-BATCH-20260906-02：共享编译／作者 API 未交付期间，完成 Skill exporter → 文件 DTO → reader → SemanticEquals 的逐字段读写对照，列出缺省值、null／空值、集合顺序和身份字段的实际处理；该源码对照不替代加载版本上的 7 条差异归因。补齐每种 Mutation 所有 planned 引用的 producer／consumer 覆盖记录，包括 StateBehavior 派生类型、Skill 的入口／子图／callsite、Timeline／声明／转换及删除依赖；从完整调用链确认所有新增类型均进入正式目录、handler 与 owner 收集。完成「原中央职责 → 内容模块 → 输入输出 → 正式调用者 → 已删除旧分支」地图，并对齐技能文档中允许文件族、正式类型／路径和仍不可用操作。将已正确修改及新源码／meta 按完整迁移单元形成中文小步提交，不混入共享文件或本节外其它所有者的改动。以上完成后，把完整 IMPLEMENTATION_REPORT、证据入口和剩余共享依赖写回本节，一次通知规划；不在每次编译后索要下一批或最终批准。

整包 apply、保存、反向导出及失败恢复已由原实现包授权，无需再次询问用户。执行前须确认精确实例／根、最新加载代码、可恢复基线与符合目标的完整 diff，使用最新 dry-run 的精确 hash；通过同一正式作者链恢复验证改动并检查资产与 package，不覆盖他人修改、不自动 Build 产品。7 条未解释 Skill 写入、旧程序集替代验证、额外故障注入、临时运行器和共享源码修改不在此执行范围内。

本地独立工作完成后，一次提交完整 IMPLEMENTATION_REPORT 到本节：模块输入输出和实际调用者、已删除旧实现、中文小步提交、精确命令与退出状态、最新候选原始计划／apply／恢复证据，以及仍未交付的共享接口。规划实际复核前不得宣称收口。共享依赖恢复前继续可独立工作；本节以外任务与协调文档由原所有者维护，不整文件覆盖或代提交其改动。

- [x] 10.1 定义v5控制配置与skill definition分片、精确允许文件族及local身份规则，交付schema与只读context／生成数据边界文档。

  当前状态：**已完成实现，待13.2整链复核**。`AgentDocumentControlConfiguration`、控制参数、Skill definition、Graph/Timeline完整分片和local canonical目录已落地；`SKILL.md`与current contract已同步v5，代码实现明确只读context与generated边界。
- [ ] 10.2 更新Exporter与strict Codec／Mapper，按控制配置、技能、Graph、Timeline及Presentation内容分责；AgentAuthoringPackageMapper及Package Codec只保留整包协调和跨分片引用。交付模块输入输出、中央字段分支删除清单、canonical往返、整包hash和未知／旧字段拒绝的现有校验结果，保持Presentation原owner。

  当前状态：**部分完成，审查未通过**。Exporter和Mapper已迁入内容模块，strict codec及规范路径／内容hash已有实现，Presentation保持原owner。最新候选的canonical往返证据仍不完整；已加载版本的无改动dry-run产生7条未解释Skill写入，不能将本项标为完成。继续AGENT-BATCH-20260906-02及最新程序集上的正式复验。
- [ ] 10.3 将AgentDocumentReconciler／Planner中的领域diff、依赖计划和Mutation lowering迁入对应内容模块，中央服务只协调完整有序计划与跨分片引用；新技能、子图、Timeline和控制binding仍共用同一事务。交付模块调用链、原中央业务分支删除清单及dry-run依赖／删除顺序报告，不新增分片apply入口。

  当前状态：**部分完成**。Skill diff/export、Control configuration diff/handler以及Graph/Timeline/Blackboard/StateMachine的文档模块与typed lowering已迁出并注册到同一MutationHandlers；创建阶段已先于声明、节点、边和Skill引用计划，StateMachine/State local Graph identity可通过正式setter回写。SubTree通用挂载与动态参数绑定仍等待共享authoring API，故不宣称本项闭合。
- [ ] 10.4 接入所有新owner的同一Undo、保存、失败恢复和reverse export，交付任一分片失败不发布半包的既有事务结果。

  当前状态：**部分完成，待整链验证**。控制、Skill、Graph与StateMachine owner继续进入同一Store staging、Undo、rollback与reverse export路径；SubTree挂载/动态绑定因共享API缺口明确拒绝，真实新包apply与失败回滚仍需在该API交付及Unity链恢复后复验，不能用源码静态检查替代。
- [x] 10.5 删除v4及更早reader／writer／manifest分支和角色RootTree正文入口，沿用两个domain与五生命周期工具；交付旧包明确拒绝、重新checkout生成v5的结果。

  当前状态：**已完成实现，待13.2整链复核**。AgentAuthoring下V4文件、V4类型名与正文入口已删除；strict reader对旧schema明确报unsupported，Character checkout只保留技能可达Graph/Timeline正文，RootTree路径仅在context保存。
- [x] 10.6 更新btsmtl-agent-authoring技能、MCP合同描述和实际代码地图，交付路径／字段可解析且与唯一v5实现一致的检查结果，不新增局部写工具。

  当前状态：**已完成实现，待最终对账**。技能说明、current contract、MCP五个独立工具描述和代码地图已同步；`status`只作为异步轮询动作，不是第六个工具，也未增加局部写入口。

## 11. 技能工作区与诊断

- [ ] 11.1 将Action Workspace统一到SkillDefinition、ActionProfile及调用点上下文，支持Tree-only和多个／嵌套Timeline；交付不猜唯一Timeline的typed页面状态。

  当前状态：**尚未完成**。Action Workspace尚未完成围绕SkillDefinition、调用点及多/嵌套Timeline的完整页面状态迁移。
- [ ] 11.2 打通技能到AnimationClip、producer、Profile和AnimationSlot的原owner导航，交付无镜像字段或第二动画资源配置的引用检查。

  当前状态：**尚未完成**。动画/Pose原owner保持；从新技能目录到资源、producer与Slot的正式导航尚未完整接通。
- [ ] 11.3 扩展source map和Trace区分C#模块／State／Transition来源、技能模板、ActionInstance、调用点和generation，交付控制转换与技能激活可分别追溯、同模板多实例隔离的诊断输出，不伪造角色图节点。

  当前状态：**部分完成**。运行来源、SkillId、实例/调用generation及ActionWindow来源已有改动；控制转换、技能激活与多实例在全部诊断入口上的精确展示尚未收口。
- [ ] 11.4 更新IR Inspector、Live Debug和窗口Follow／Pin绑定，交付控制合同及技能执行根可查看、过期目标不选其他实例的状态报告。

  当前状态：**部分完成**。新控制/技能记录已进入IR，现有Inspector基础可读取目录；Live Debug、Follow/Pin精确实例绑定与过期目标行为尚未完成本轮迁移。
- [ ] 11.5 向独立场景预览提供精确技能选择、正式请求及只读实例接口，交付双方接口对账；不创建场景、SkillPreviewRuntime或重复实现旧播放器删除。

  当前状态：**部分完成**。c2d9a203d等已提供正式控制/技能请求和读取合同；与独立预览当前候选的全部选择/观察接口及组合说明仍待交接对账，预览功能本身不计入本项实施。

## 12. 资产迁移与旧路径清理

- [ ] 12.1 通过现有正式作者事务转换全部选定有效Character根及其技能依赖，交付角色代码／技能映射、仍有效的稳定业务identity与完整引用报告；Graph／Node kind变化时创建新identity并替换引用，不能原地改kind，受保护无效资产继续明确报错。

  当前状态：**部分完成**。ceb50eb9e已迁移Corin Definition及技能配置；所有选定有效根的可达闭包、稳定引用与旧角色图资产清理尚未完整交付。
- [ ] 12.2 转换有效装备入口、输入／变量绑定及有限producer来源，交付新控制／技能目录可构建结果，保持既有Motion曲线、Warp及表现资源内容。

  当前状态：**部分完成**。Corin移动/动作来源已有接线并保留原Motion/Pose资源；有效装备入口及全部输入/变量/producer引用迁移尚未闭合。
- [ ] 12.3 显式构建并发布所选Target、技能目录和同组Projection，更新现有Launcher／Variant／Profile引用；交付exact artifact与产品引用一致报告。

  当前状态：**待正式重建**。旧6ca8e09a7产物是在公共 Projection ABI v15、ACL接收修正和当前脚部几何前置之前生成，不能继续作为当前交付；新正式构建需在脚部几何验证身份闭合后，由同一入口生成两Target与同组Projection并重新核对Variant/Launcher引用。该条的旧代码接线仍保留，运行行为仍由13.2验证。
- [ ] 12.4 删除已替代角色控制图入口、activation／Equipment Host编译注册、旧schema、菜单、字段、别名及废弃文件，交付定向零引用与仍保留AI／Pose／独立预览依赖的业务清单。

  当前状态：**部分完成**。已删除部分旧控制代码位置、重复Action流程及中央发射分支；旧角色图、Equipment Host注册、v4 schema、菜单和别名尚未全量清除。
- [ ] 12.5 按设计12逐项核对最终目录、类型和公开命名，交付原职责→正式模块→输入输出→调用者→已删除旧实现的代码地图；确认没有重复Action业务流程、中央领域特例、partial拆分、转发壳、万能Context、临时桥接、双运行入口或兼容配置，不能只以新增类数或行数降低收口。

  当前状态：**部分完成**。Agent Document Mapper、Reconciler与Mutation Planner已按Graph、Timeline、Blackboard、Action、Control、AI和Skill职责拆成真实模块，旧中央领域分支已删除；Graph创建阶段、local owner顺序、Skill callsite的Graph/Node分型解析已接入。SubTree共享作者写入API尚未交付，且完整目录/命名地图与新包整链证据仍待收口。

## 13. 集成证据与规范收口

- [ ] 13.1 运行现有portable／Editor／产品构建和依赖检查，交付实际构建结果；dotnet／msbuild使用禁用build server参数并立即shutdown，本机Unity CLI按明确项目路径退出且保留主验收Editor，CI禁令不变。

  当前状态：**部分完成**。按要求执行的`ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false`在补齐共享Action Workspace路由后最近一次完整构建成功（58个既有警告、0个错误）；Agent范围`--no-dependencies`定向构建同样为0警告/0错误，两个构建结束后均立即执行`dotnet build-server shutdown`。正式Rebuild、产品构建和Unity编译恢复后的完整receipt仍待交付。
- [ ] 13.2 使用已有Validator、Document生命周期与Replay／Proof覆盖完整新链，交付同版本重复性及跨实现输入／Body／动作阶段／输出比较；不编写新测试、不忽略缺帧或运行错误、不把ProgramHash变化当作行为通过或失败。

  当前状态：**部分完成，待整链复核**。明确实例`e852139597e42532`上的v5 Character checkout成功（root `c7a7c1e3f7e64d81b5a04a90cbeb8d4e`，`editableHash=07390110c2c6173f43f0f92f208cc558e428a84069c44a0ea434900ef0917930`，`contextHash=05a7444d544becb03c6e3431ef214d277b8d49060c8c24336f14e5e7349e2994`，`documentHash=b04cd5547d92a9b0709fc343a89fe03f2fe8f785463813416868f62c385e2f51`）与formal validate成功；dry-run尚受Unity实例未加载最新Reconciler及外部`generated-diagnostic-sampling`编译错误影响，Replay/Proof未开始。
- [ ] 13.3 安装本change的delta并同步当前项目口径、Purpose及关联接口说明，交付现行规范与预览／其它change不存在相反共享要求的对账；保留独立预览和受保护任务范围。

  当前状态：**尚未完成**。尚未安装本change全部delta和更新最终项目口径；AI/Timeline/预览/Pose等并行规范仍需按实际采用版本完成组合对账。
- [ ] 13.4 执行严格OpenSpec校验与限定改动diff检查，按完整迁移单元形成中文小步提交；分别交付设计12的结构迁移证据和构建／Replay行为证据，附文件跳转、删除清单及剩余明确错误。仍有重复业务流程或未迁出的中央职责时保持对应任务未完成，不以编译通过、类行数或文件数宣称整个重构完成。

  当前状态：**部分完成**。V4路径零引用、v5工具数量、strict文件族和定向构建结果已完成静态收口；本实现窗口已形成中文小步提交`84812507b`、`346e4024d`和`5538b80fc`，但共享工作区仍有Action Workspace编译错误、Unity最新assembly reload与dry-run复验缺口，保持整项未完成。
