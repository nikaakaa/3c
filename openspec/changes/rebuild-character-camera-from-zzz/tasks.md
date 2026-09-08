# 相机移植执行要求与工作记录

当前执行批次：CAM-EXEC-20260906-01。协作决定：COMM-20260906-01。

本文件是本规划与原实现任务的共同执行入口，位于 D:/Unity_Project_1/camera-zzz/openspec/changes/rebuild-character-camera-from-zzz/tasks.md。继续原 worktree 和 codex/rebuild-character-camera-from-zzz 分支，已有实施授权持续有效，不重新派生任务、不等待再次 apply。下方 1—13 保留整个变更的任务编号；本节明确当前交付范围、责任和审查结果，局部交付不代表整个变更完成。

## 0. 当前执行要求

### 0.1 文档协作与写入分工（COMM-20260906-01）

- 跨任务决定自行读取 D:/Unity_Project_1/3C/docs/coordination-progress.md，尤其“协作方式：文档为准”、C-04、P-08、P-12/P-13；该文件由协调任务单独维护，本任务只读。已有决定不再从聊天片段重新拼接。
- 规划维护本节的范围、接口分工、下一批要求、审查和待协调事项。实现维护下方任务的真实完成状态和末尾“实现工作记录”；原始来源、函数/字段/事件映射继续补入现有 evidence/source-behavior.md，资源版本/身份继续补入 evidence/source-baseline.md，不新建重复方案或另一份进展文档。
- 实现连续推进已授权工作、小步中文提交，把提交、源码/资源身份、验证证据和未完成项写回工作记录。普通提交、编译、进度、已读、仍在等或无变化不发消息。
- 规划在必要交付点读取工作记录并完成实际审查，将 APPROVED 或 CHANGES_REQUIRED 及下一步收回本文件。需要开始/调整执行、处理真实阻塞或接收已审查交付时，才发一次短通知，只包含绝对文档路径、章节/事件编号和所需动作，不索取回执。
- 对同一问题，先在文档一次写全背景、精确代码/资源/API证据、业务影响、已尝试处理、需要决定的事项和已有方案的业务取舍，再经本规划交协调任务。TASK_READY、CROSS_TASK_QUESTION、COORDINATION_INVALIDATION 留作文档标签；详细内容不再反复发成长消息。
- 只使用现有规划/实现配对，跨任务由协调任务转交；实现不直接联系其它任务。没有新动作时等待，不定时轮询或催报。消息减少不改变实施授权。

### 0.2 业务目标与当前接口分工

输入是已有真实 ZZZ 镜头资源、明确的动作/Timeline 请求、同一表现帧的时间和可见目标；输出是本地镜头位置、旋转、FOV及活动镜头实际 CameraBasis。正式链路为：源数据 → 作者资产/真实请求引用 → 同一 Build 的 Program/Projection → 当前有效请求 → 本地求值/混合 → 最终 Pose 之后应用 rig。只存在一个相机表现帧和一个 ScenePlay 生命周期。

本轮 MR 接收顺序为 ACL → Timeline → 相机。第三项只限制接收顺序，相机独立范围持续开发。先交付来源及行为能够证明的默认/Zoom/Stretch与真实请求，不要求 Override、Shake、Shot、碰撞全部完成才给限定 TASK_READY；不支持的能力保持明确拒绝，不填临时实现。BTSMTL 属于单角色范围，不加入换人业务。

| 责任方 | 必须提供的输入输出 | 相机任务的动作 |
| --- | --- | --- |
| BTSMTL 主线 | Core 命令 kind、合法 Source/Header 入口、Graph Camera 提交、C-04 公共事务/端口/Fixed与Float适配 | 消费已审查完整提交，提供已有共享改动的精确差异，不独立再写一套公共适配 |
| Timeline | 领域回调/输出 DTO、两个 TimelineTarget、真实 Source/call/Action/producer、Header.Activation.Generation、cycle/时间、一次 clip 权重，以及开始/更新/Complete/Release/Force和Stopping退出 | 提供真实 ZZZ 调用映射供其接作者引用，消费正式合同，不复制旧 Timeline 或填零/假身份 |
| 相机 | 作者资源/Projection领域数据、正常本地请求消费、求值/混合/淡出/平滑、rig/Basis | 完成可独立域内工作及源证据，正式依赖到达即接入并重建配套产物 |
| Pose/ScenePlay | 同一帧最终骨骼发布、正式时间和会话 Start/Pause/Resume/Reset/Stop | 沿既有发布/时钟消费，不另造 Pose 根、Camera 更新循环、预览会话或 seek 执行器 |

C-04：上游可在现有通用仿真记录中记录相机请求，由上游比较并提供完整旧/新命令和当前有效的新增、更新、停止；整个输出事务成功后，才进入同一正式表现队列。不能逐步重播所有 Replay，也不能丢弃全部 Replay 导致消失来源不退出。相机删除专用回滚日志、快照恢复、Suspend/Resume/Confirm与网络确认等待，保留正常播放身份、去重、转场、淡出和连续平滑状态。纠正发生时从当前可见结果按已有规则更新或停止。

相机 Complete/Release/Force 及网络出口/远端排除，均按真实 Program/Projection producer 判断，不能只看 Camera kind。本地终止不等 confirmedTick；角色和动画的回滚/确认保留。不得为此新增网络字段、回退 /7、丢失 typed Source/Skill/状态身份，或用 SkillExecutionGeneration 替换逐次 Header.Activation.Generation。

公共 Projection：ACL 的 a9c593bc3 与相机的 83aff47e1 都曾使用 v14，但前者是资源/属性布局，后者是 Camera 布局，不能互读。协调文档现记录主线 ACL 接收源码选用 v15，但这不等于相机组合容器或产品已发布。只消费主线正式交付的唯一 ABI/矩阵，在完整保留 ACL 资源/属性及 Source/Pose/Foot/Rig/Tuning/Blend/MM/LinkedPose/Equipment 字段的容器上加入 Camera payload/RequireCameraPayload；组合布局变化由主线统一版本，Create、编译、保存、hash/identity、加载 expectation、消费者和原子发布一同更新并重建。禁止只改字符串放行或覆盖旧 Generated。

### 0.3 本批连续执行内容（CAM-EXEC-20260906-01）

| 编号 | 工作与交付物 | 业务完成条件 |
| --- | --- | --- |
| CAM-01 | 在 evidence/source-behavior.md 整理原动作/调用/事件 → 相机请求键 → 本项目作者资源的真实映射，逐项列源文件/身份、起始帧率/时刻、更新依据、持续条件、权重、目标和正常/取消/Force退出 | 只接有真实调用证据的请求；36条资源不等于36个触发点，不虚构调用。映射由规划审查后经协调交Timeline，作者引用与相机资源及重建产品一同进入第三项接收 |
| CAM-02 | 追查实际 Corin 默认键选择及 ELEVATION_ANGLE 到比率/角度/轨道的源转换；列明输入、单位和消费者 | 不将 Default_Normal 候选或中间轨道推算当作原 Corin 默认已证明。最终确实缺来源时，写全已查范围和缺口交协调决策，显式 Zoom/Stretch 继续 |
| CAM-03 | 对三条原生曲线核对 native infinity、导入 AnimationCurve 的实际公开 WrapMode、显式字段与 payload 的边界行为 | 数字同为2不是映射证明；以真实读取结果修正，不以字段数值一致宣称播放语义通过 |
| CAM-04 | 完成本地请求消费与有证据的 Zoom/Stretch语义；把请求索引/退出队列和帧输入装配按职责收束 | 保留 FramePlanner/EffectEvaluator/Transition 分工，不把新算法堆入 CharacterCameraPresentationRuntime，也不只用 partial 降行数 |
| CAM-05 | 消费 C-04、Timeline 和公共 Projection 的正式完整小步，沿既有入口接引用、构建、重读并运行已纳入范围 | 真实动作能触发、更新和退出镜头；旧数据/路径被正式替换，无第二 adapter/loader/Build，产物/版本与本次源码一致 |

同帧已有 CompletePresentation → CommitFinalPose → Camera.Present → rig/Basis 的顺序必须保留。默认 follow/aim 当前来自 body 可见 Pose + 初始 bind offset，显式 target binding 才逐帧读取 Transform；不要把调用顺序描述成所有默认锚点都已读取最终骨骼。需要骨骼目标的源语义，沿已发布的正式 Transform 输入接入。

当前能力范围不包含已拒绝发布的 Override/Shake/Shot/碰撞。补充这些能力须先闭合原消费者及可达依赖；保留其未完成任务，不用近似公式/任意默认资产填空，不以它们阻塞已能证明的限定范围。

### 0.4 当前审查与事实基线

审查编号：CAM-REVIEW-46460-20260906。结论：CHANGES_REQUIRED，未新增 TASK_READY。

代码/资产审查对象是 46460e17d3cbe6aaccdec5e45c94eae6461fa908。本次文档更新前 HEAD 为 fd786fcf06b23c7729e38d686fc6d1195e4b693c；e0ff5595 曾加入 Attack5 引用，fd786fcf 随后撤回未交付接线，两提交对 46460 的净文件差异为空，不能把中间提交当当前作者引用或已批准映射。

已查明的成果：
- 0ff8d2a69、5a80a4476、4743af15b、46460e17d 共84个资产/meta/Definition引用变更、1996新增行，包含3条公共曲线、默认Sequence/Profile、18 Zoom和18 Stretch。
- 按现有字段/枚举映射对原 Corin Zoom/Stretch JSON 核对810项标量、向量和曲线GUID引用，无差异；三份原始 AnimationCurveLibrary 的曲线时间/值/切线/weightedMode相符。这个结果不覆盖默认选择、枚举/时间语义、非加权切线权重或画面。
- CharacterPresentationProjectionCompiler 调用 CharacterCameraProjectionBuilder，RuntimeFactory 创建本地相机；最终 Pose 之后执行 Camera.Present，CinemachineCameraRigAdapter 应用位置/旋转/镜头参数并回读实际 Basis。现有 prefab/场景已有 rig/Brain 装配代码和引用，不是仅存独立算法。
- 相机私有回滚恢复/Suspend/Resume/Confirm已删。仍有问题的是公共交接，不能继续把已删除的相机日志当当前实现。

待修证据（代码基址为 D:/Unity_Project_1/camera-zzz/3cDemo/Client/3C_Client/Assets/GameScripts/Main）：
- 本地作者/产品：Corin Definition 的 Generated/CorinCharacterPipelineDefinition.PresentationProjection.asset 第1326420行为旧v13，无根级Camera/defaultSequence；36条效果仅进Profile注册表，当前作者动作引用未完成。
- 默认/单位：D:/ZZZ_Dump/output/corin_replication/replication-guide/analysis/基础镜头.md 第3行明确尚未证明Corin选用Default_Normal；第25行为0.60000002，而当前Profile默认角度为3.4336302、Sequence比率为0.5。CAM-02负责闭合选择及转换，不擅自改为另一组猜值。
- 曲线：CameraCurveAsset的显式边界字段是UnityEngine.WrapMode；CAM-03负责其与native infinity的实际对应，当前没有已观察到游戏循环错误的结论。
- 公共交接：Runtime/Simulation/Unity/Fixed/FixedUnityOutputAdapters.cs:63–96直接Publish相机，不入m_ByEvent，Replace/Retire仍查旧事件；DeterministicRollback/Pipeline/RollbackOutputCommitter.cs:353–376仍传旧ID；RollbackOutputDispositionPass.cs:72–81仍按Kind将所有Complete/Release延迟。完整公共修复归主线，相机不得恢复私有历史或空操作吞掉失败。
- Timeline：相机worktree的旧副本在TimelineControlRuntime.cs:607–655读取Timeline generation，ForceStop发Release，FixedTimelineTarget未闭合Force。协调文档另已确认提供方19af51506的Camera generation/sourceInstance仍0、terminal仍Camera+Weight0、Force未完成；其已审查的Skill身份、分层和一次权重成果保留。旧3beb含后来删除的standalone候选，不能整批回灌。
- 结构：审查时FramePlanner140行、EffectEvaluator242行、SequenceTransition266行；请求/接线类CharacterCameraPresentationRuntime676行。按CAM-04收束职责，不重写已经正确的算法。

已有正式验证均是Unity导入/脚本编译，不能当Definition产品重建或运行验收：

| 源提交 | Run | 结果 |
| --- | --- | --- |
| 0ff8d2a698303e9d7c8aa943105494b214e73beb | 3d9a8f3025124a3796138343fdf9c0c6 | compile成功，前后commit/identity相同 |
| 5a80a4476ffee5f101551ac9dcd5a86900d69ece | 52aadee6e4984592b0f171b5dd9e0a28 | compile成功，前后commit/identity相同 |
| 4743af15b6da50915bd86690ea0b85450a82edc3 | 79a22d8aa6ae40b6b0faf36db5e8231c | compile成功，前后commit/identity相同 |
| 46460e17d3cbe6aaccdec5e45c94eae6461fa908 | 9b19cc4108054ea292a3d7b2c8a40d25 | compile成功，前后commit/identity相同 |

证据入口为 D:/Unity_Project_1/3C-Artifacts/3c-gameplay/Runs/<Run>/editor-receipt.json、source-before.json、source-after.json。最后一项identity为bff56ea62a91057af40a1067492f8e8f1f71a061dc8ed72251274be3d32bafad。fd786的净树一致不自动产生一份新的正式receipt。

历史ec29841b24a2edee561551f9ad97519a29381a81仅通过已提交控制/Skill依赖、Source与逐次producer generation对齐审查，证据Run a29eb4a7232e4fe882fdb144c47821f4；不扩大到新资产/产物。更早867429的私有回滚范围已被用户新边界部分替代，不能充当C-04批准。

### 0.5 依赖、问题与交付记录规则

目前没有需要用户重复批准或全体任务完成才能继续的条件。主线C-04小闭包、统一Projection容器/正式矩阵，以及Timeline真实请求合同尚待正式交付；这些依赖不阻止CAM-01—04。

新增问题在本节一次写完整，包含事件编号、状态、精确提交/文件/API、输入输出、影响、已经查证或尝试的处理、需要谁决定什么、相关方案的业务取舍。已有协调结论以协调文档为准，不重复提出已决定的C-04分支。

限定交付只在本规划实际APPROVED后记录TASK_READY，列完整提交/依赖、源映射、资产/产物身份、正式编译/构建/已有运行证据和未覆盖范围。未支持的其它ZZZ能力继续记录为未完成，不阻塞已经闭合的限定交付，也不把有限就绪扩成整个相机完成。

### 0.6 实施与验证规则

- 正确代码、作者改动和未提交内容不回退、不覆盖；只消费完整且已审查的必要依赖，不按几个文件名裁剪或恢复已删除候选，不新增fallback/兼容路径。
- 不新增测试代码，不把用户手动验收写成任务。完成必要正式编译、Definition Build及已有验证后准确记录实际结果，编译成功、产品发布、运行成功、画面验证分别描述。
- 用户最新仓库规则已允许按明确项目路径通过正式CLI/executeMethod运行本机Unity batchmode并退出；保留主验收Editor，CI禁令不变。旧worktree说明中的全面禁令不代表当前授权。Unity MCP每次显式指定正确unity_instance，不能切换全局实例。
- 当前相机Unity项目为D:/Unity_Project_1/camera-zzz/3cDemo/Client/3C_Client，Development Center workspace为6078ad4b097948031fa94f77；仍须按实际实例/项目核对后调用，不能靠旧工具任务快照判断工作状态。
- dotnet build/msbuild使用 --disable-build-servers /nr:false /p:UseSharedCompilation=false，完成后立即dotnet build-server shutdown。工具从正式配置解析，不按最新文件时间猜运行版本。
- 规划/实现都只在本节既定分工内写文档；不写协调进展文件。已有工作继续，不因切换为文档协作而停工。

## 1. 闭合 ZZZ 来源与原行为

- [x] 1.1 锁定原游戏/组件版本、原资源身份和内容 hash，交付同版本 Camera 来源清单及重复/冲突资源报告。
- [ ] 1.2 解析角色 Camera Profile、球面/轨道组、普通目标/Boss 锁定、输入和碰撞配置，交付逐字段值、单位、空间及正式消费者对应表。
- [ ] 1.3 追通单点/双点/多点/实体取景与原 Sequence 组合函数，交付输入输出、分支和依赖函数清单，所有纳入类型都有来源证据。
- [ ] 1.4 追通位置/旋转阻尼、BlendFromCurrent、MoveByBlending、抢占和退出函数，交付阶段历史、起终点、重入/取消与时间推进规则。
- [ ] 1.5 追通 Override、Zoom、Stretch/回弹、Shake 的组合、tag/优先级/静音规则，交付特殊时间值、相对/绝对数值、枚举与原曲线消费者映射。
- [ ] 1.6 补齐公共 Camera Curve、Shot、CinePrefab、Timeline/Track 绑定等可达依赖，交付无未解析引用的资源闭包及原曲线插值数据。
- [ ] 1.7 追通 WorldBasicCameraData 到活动 Cinemachine 实例及最终回读，交付逐阶段 owner、调用顺序、写入点、组件和结果表，每项计算只有一个 owner。
- [ ] 1.8 按每个拟交付能力汇总来源对应表、依赖和精确未完成项，将 1.1—1.7 的证据写入现有 evidence；该能力闭合后可独立实现和交付，不等待其它未知能力，不用近似公式补齐缺口。

## 2. 建立正式相机作者资源

- [ ] 2.1 建立 CharacterCameraProfile 与 Definition 的显式引用、默认序列和目标槽位合同，交付真实资源模型及 owner/dependency 定义，Definition 不内联相机参数。
- [ ] 2.2 建立 CameraSequenceAsset 的有限算法与组合描述，交付覆盖来源清单的 typed 数据模型和合法输入输出/组合校验，未支持类型不能发布。
- [ ] 2.3 建立 Override、Zoom、Stretch、Shake 资源模型，交付与原字段表逐项对应的数值、时间、空间、优先级及释放合同，无万能 Custom 字段袋。
- [ ] 2.4 建立 Shot 与 Camera Curve 资源模型，交付精确绑定需求、镜头参数和完整曲线数学描述，共享资源保持单一 owner。
- [ ] 2.5 建立来源基线与正式资源的身份映射、依赖和作者差异记录，交付可定位原身份与当前编辑差异的资源清单，重新导入不会自动覆盖作者改动。

## 3. 统一作者能力与 Mutation

- [ ] 3.1 为全部 Camera 资源和 SkillProgram/技能局部 Graph/TreeClip/Timeline 能力注册字段、类型、单位、空间、时间域和端口，交付人工 UI、Compiler、Document 共用的唯一 capability 目录。
- [ ] 3.2 实现 Profile/资源的创建、修改、删除和强类型引用 Mutation，交付与现有事务服务接线的 handlers，创建对象和引用变更属于同一 Undo owner 集合。
- [ ] 3.3 实现原数据到正式作者目标的明确 Import 命令，交付依赖预检、逐项映射和同一事务计划；缺失依赖或来源冲突时报告失败且不发布半套资产。
- [ ] 3.4 注册 Timeline-local Weight/Ease 与资源曲线各自的 Channel/Mutation owner，交付无重复可写曲线的 Catalog 和完整曲线替换接口。

## 4. 接入 Semantic IR 与唯一 Projection Build

- [ ] 4.1 迁移 SkillProgram Root、技能局部 Graph、TreeClip/Timeline 的 Camera 请求 payload，分离 portable 命令语义与 Presentation 资源引用，交付带完整 source mapping 的新版本相机 operation 合同。
- [ ] 4.2 消费主线的 Core/Graph Camera/公共提交与 Timeline 的领域 DTO/两 Target 映射，按 0.2 分工交付同语义的正式请求结果，未知/旧版本 payload 明确拒绝，不双写共享实现。
- [ ] 4.3 实现 Camera Projection 编译模块，交付同一 CharacterPresentationProjection 内的 dense 资源/曲线/序列/目标/Shot 计划和唯一阶段顺序，不新增独立 loader。
- [ ] 4.4 补齐资源类型、来源闭包、数值/时间域、阶段和容量预检，交付能定位 Profile、资源、节点或 Clip 的正式 Compiler 诊断。
- [ ] 4.5 将 Camera dependency/revision 与生成字段纳入原子 Build，交付完整发布组及身份报告，纯效果参数修改只改变 Projection，时点/命令语义变化更新对应 Program 合同。

## 5. 接入本地相机帧与请求生命周期

- [ ] 5.1 建立 CameraFrameInput、CameraFramePlan、CameraRigResult 和窄接口，交付不依赖场景对象/作者资产的计算合同及明确平台 binding 合同。
- [ ] 5.2 扩展现有 Presentation Frame context 的相机时间和动作锚点输入，交付 scaled/unscaled/暂停/重置来源清单，核心不读取 Unity Time 或二次缩放。
- [ ] 5.3 消费上游事务成功后提供的完整新增/更新/停止命令；本地按 producer、Header.Activation.Generation、Action、cycle 和事件身份去重/退出，旧代际不清新实例；不建立相机回滚日志、快照恢复或网络 Confirm。
- [ ] 5.4 实现请求退休、效果退出尾段和 Runtime/Body reset 的原规则，交付覆盖 Cut/BlendOut/等待来源结束、快速重入与目标切换的正式状态转换及原因快照。
- [ ] 5.5 将目标采样接入同帧 visible Body、最终骨骼、明确实体/世界点/候选输入和本地物理场景，交付槽位解析与缺失原因结果，不建立第二份 Body/台阶历史。

## 6. 实现完整基础构图与序列

- [ ] 6.1 实现原单点、双点、多点及实体取景算法，交付来源对应表中每种已纳入算法的正式 evaluator 和输入输出诊断。
- [ ] 6.2 实现原球面/轨道、构图偏移、屏幕位置和角色默认序列，交付消费 Profile 的完整基础镜头计划，删除基础参数硬编码依赖。
- [ ] 6.3 实现原手动输入、俯仰/水平响应、输入接管、普通目标/Boss 锁定和目标转换，交付明确输入权重、目标与构图结果，不修改 Gameplay 目标事实。
- [ ] 6.4 实现原位置/旋转阻尼与上下运动镜头响应，交付真实连续历史与阶段结果，对应 Adapter 不再执行同类阻尼。
- [ ] 6.5 实现 BlendFromCurrent、MoveByBlending、状态仲裁和进入/退出/抢占恢复，交付实际位置/旋转/构图/FOV 混合结果和起终点诊断，不只发布 blendProgress。

## 7. 实现全部相机效果

- [ ] 7.1 实现 OverrideTrack 的原轨道/构图覆盖和进出曲线，交付同 tag/优先级/被压制状态的正式结果与来源映射。
- [ ] 7.2 实现 Zoom 的原 FOV 类型、延迟、进入、保持、退出和时间尺度规则，交付与来源字段对应的完整效果阶段和最终 FOV 贡献。
- [ ] 7.3 实现 Stretch 的半径、位置、倾斜/俯仰及回弹行为，交付完整空间贡献和恢复到当前有效构图的结果，不折算为 FOV Kick。
- [ ] 7.4 实现 Shake 的原方向、频率、噪声/随机、各轴幅度、衰减和叠加规则，交付正式效果结果与种子/事件来源，不扰动角色或跟随目标。
- [ ] 7.5 将所有效果接入已确认阶段顺序，交付同帧覆盖/累计及首次采样结果，短事件不因统一先减寿命而无条件丢失。

## 8. 完成 Shot、碰撞和 Cinemachine 输出

- [ ] 8.1 实现原 Shot 的资源解析、明确绑定、进入/退出和打断，交付由唯一 Runtime/Adapter 管理的承载生命周期，Shot 无自主更新。
- [ ] 8.2 按原职责实现相机碰撞及其显式世界端口/平台扩展，交付唯一碰撞阶段与结果，缺少场景能力时提供正式错误。
- [ ] 8.3 实现 CinemachineCameraRigAdapter 的计划应用、原组件阶段和唯一 Brain 推进，交付阶段 owner 与活动输出结果，移除重复阻尼/混合/效果计算。
- [ ] 8.4 从实际活动输出发布同帧 CameraBasisSnapshot 和正式重置结果，交付 Shot/blend 后一致的方向/yaw/pitch，输入不再读取未活动 FreeLook。
- [ ] 8.5 将完整相机装配接回 CharacterCameraPresentationRuntime 与 Factory，交付保持 Body/最终动画/Camera 顺序的唯一调用链，无相机 Actor 不分配相机能力。

## 9. 接入正式作者入口与 Timeline

- [ ] 9.1 在 SkillProgram Root、技能局部 Graph 与 TreeClip/Timeline 入口接入 Camera Navigator、资源 Details、引用导航与 source mapping，交付复用原 Shell/Canvas/selection 的领域 adapter，无独立 Workbench；C# Locomotion 控制拓扑不提供 Camera 图节点。
- [ ] 9.2 迁移正式 SkillProgram Camera producer 的 Sequence、效果、响应、目标和 basis 能力，交付菜单/字段/端口/编译/Document 一致的正式能力；producer 通过 SkillProgram 与新 SourceMap 衔接，消费已提交 PresentationCommand、ActionInstance 来源和 generation。
- [ ] 9.3 接入 Timeline 的 Sequence/Override/Zoom/Stretch/Shot 区间与 Shake 时点，交付明确资源、时序、同帧顺序和生命周期编辑，不创建原 Animator 事件播放器。
- [ ] 9.4 接入 Clip Curve Lane 与共享资源 Curve owner 导航，交付带单位/时间/值域的原交互与唯一 Mutation，不生成隐式曲线副本。
- [ ] 9.5 接入明确 Import/Build/统一 ScenePlay Preview 准备命令与 Stale/错误显示，交付可定位资源的轻量状态；Camera 不拥有 Preview session、fixture executor 或 seek controller，OnInspectorGUI、selection 和恢复调用链不含重操作。

## 10. 接入统一 ScenePlay Preview 与 Live Debug

- [ ] 10.1 对账 `rebuild-btsmtl-preview-with-scene-play` 的统一 Preview owner 接口，交付 Camera Runtime、Projection、Rig/目标/物理绑定、Reset 和只读诊断的接入边界；BTSMTL 实际签名未提交前不写桥接或占位接口。
- [ ] 10.2 由统一 ScenePlay owner 提供初始镜头、目标/输入轨迹、时间和随机种子等可复用 fixture，交付明确输入来源；Camera 不创建第二份 fixture executor，也不从当前场景偷取隐式历史。
- [ ] 10.3 由统一 ScenePlay owner 调度播放、暂停、循环与可取消的分步 seek 重建，Camera 只执行正式 Runtime 的 Reset/逐帧求值；Timeline 游标只定位作者内容或观察历史，seek 不直接修改 Simulation。
- [ ] 10.4 由统一 ScenePlay owner 管理 Camera/Shot 输出独占绑定、Stop/Dispose/重绑/domain reload 清理，交付占用/释放与缺少上下文的明确结果；Camera 不拥有第二个会话或角色执行链。
- [ ] 10.5 接入正式相机诊断 provider 与窗口本地 Follow/Pin，交付资源/producer/generation/时间/退出/碰撞/最终 basis 快照和作者导航，Live 不调用 Preview，Preview owner 不把 Live 伪装成 Authoring Preview。

## 11. 完成 Agent Document v5 目标对账

- [ ] 11.1 盘点当前已安装 Document v4 中可达的 Camera 语义、资源身份、引用和 owner，交付迁入最终 v5 Camera domain 的完整对账表；不为过渡建立 v4 Camera 分片。
- [ ] 11.2 在 BTSMTL v5 实际接口提交后，注册 Camera Profile/Sequence/Effect/Curve、Definition 引用、结构化引用语义和 capability/context revision，交付 v5 owner 统一装配的精确 manifest 闭包，AI domain 拒绝 Camera 可写分片。
- [ ] 11.3 在 v5 实际接口提交后，同步 v5 owner 的 Exporter、严格 Codec、Catalog、local identity 发现、Reconciler、typed Mutation、Validator、owner 锁定与 Undo/失败恢复；Camera domain 不拥有第二套 schema/codec/Mutation。
- [ ] 11.4 在 v5 实际接口提交后完成整包反向导出、稳定对象身份、package hash 和 Clean 状态发布，交付经重读校验的完整包，Camera apply 不触发 Build 或运行相机。
- [ ] 11.5 在 v5 实际接口提交后同步五个生命周期工具的机器诊断、现有窗口和 skill/current-contract；在接口未提交前只维护边界说明和失败诊断，不写 v5 占位代码或把未安装能力写成 current truth。

## 12. 迁移 Corin 内容与全部相机调用者

- [ ] 12.1 通过正式 Import/作者事务迁入 Corin 的 81 项 Shake、18 项 Zoom、18 项 Stretch、4 项 Override，交付逐资源身份、数值和消费者对应报告。
- [ ] 12.2 补齐 Corin Profile、Sequence、公共 Curve、Shot 与绑定依赖，交付完整可达作者配置，无默认替代或仅保留名字的资源。
- [ ] 12.3 将已确认属于单角色动作演出的 Corin 相机事件迁入正式技能局部 Graph/TreeClip/Timeline 请求链，逐项交付真实调用、帧率/时点/顺序、权重/目标和终止映射；由 Timeline 接作者引用，不按资源数量虚构触发；SwitchInAttack 只保留来源证据，待换人/跨角色归属设计确认后决定是否迁入。
- [ ] 12.4 迁移 Float32/Fixed 输入、Local/Fixed/Rollback Host、Control Source 和 Factory 的 Camera/basis 接口，交付无具体旧 Controller 依赖的调用清单，输入与网络原逻辑保持原样。
- [ ] 12.5 迁移 GameplayLab Builder、性能采集/回放的初始相机接口及全部明确 Scene/Prefab bindings，交付无旧 FreeLook axis 直写的引用清单及正式绑定校验结果。
- [ ] 12.6 完整切换唯一正式入口并删除旧 Controller、State/Modifier 空实现、Mode/FOV 映射、旧 Cue/字符串/序列化配置和过期 generated 合同；交付旧符号与资产引用搜索结果，无法映射的既有正确行为先报告冲突。

## 13. 完整发布与规范收口

- [ ] 13.1 通过精确 Definition 的既有 Build 发布请求的 Float32/Fixed 与唯一公共 Projection，交付通过原子发布和相机依赖校验的完整产物组；正式本机 Unity 调用遵守 0.6 的当前授权与实例规则。
- [ ] 13.2 汇总来源、资源、编译、运行消费者、编辑入口和诊断的完整性报告，所有已纳入项无未解析/无消费者/旧路径状态，未达到则保持变更未完成。
- [ ] 13.3 对照本 delta、current v4 spec、未来 v5 owner 边界、ScenePlay active change（若已安装）和其它 active change，同步 Camera 相关主规范及 project context，交付冲突对账结果；不覆盖 Pose、Body、PIK、Performance 已有正确变更，也不把 v5/ScenePlay 未安装接口写成 current truth。
- [ ] 13.4 整理本变更各小步中文提交、作者资源和生成产物身份，交付精确变更范围及用户可跳转的最终作者/运行入口；如使用 .NET 构建，命令带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 并立即执行 `dotnet build-server shutdown`。

## 实现工作记录

本区由实现任务维护。每个可审查批次记录：关联CAM编号/实际范围、完整提交与依赖、输入输出和旧路径清理、源证据章节、正式验证Run/receipt/前后身份、产物与实际运行范围、未完成项；只有结论或可执行动作发生变化才准备必要通知。原始源数据/事件映射仍写入已有evidence文档，本区只引用其精确章节。

### 2026-09-06 既有基线迁入

本条由规划根据已读取的Git、原始资源和正式receipt迁入，不冒充新的实现报告：fd786fcf06b23c7729e38d686fc6d1195e4b693c与46460e17d的净文件差异为空；4次资产提交的导入/编译证据和CAM-REVIEW-46460-20260906详见0.4。当前尚无已审查的完整请求映射或包含相机的新产品。实现从CAM-01—05继续，并在后续条目记录真实交付。
