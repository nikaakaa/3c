# 3C 工作协调进展

维护人：工作协调窗口。最近更新：2026-09-06 11:56:12 +08:00（北京时间，以事件更新）。
本文件是推进记录，由协调窗口单独维护；不替代业务 spec，不由功能任务顺手修改或并入功能 MR。

## 协作方式：文档为准

决定编号：COMM-20260906-01。用户明确要求所有规划任务及其实现任务改为主要通过文档协作，消息只作极少量的读文档执行通知。本节替代以前按每次回报转发、长消息下达和索取回执的约定。

- 协调窗口将跨任务决定直接写在本文件对应问题/决定中，写清责任方、需要的动作、交付条件及证据入口。规划任务只读本文件，继续由协调窗口单独维护。
- 协调结论本身也以本文档为准，由责任规划自行读取并落实；通知里不重述整套决定。提报协调前，应在自己的文档一次写全背景、具体问题和证据、影响、已尝试处理及需要协调决定的事项，已有可行方案则写清业务取舍；不要先报一句卡住再靠多轮消息补全。
- 各规划任务把本任务的完整执行要求、接口选择、范围、剩余项和审查结论写进现有执行文档；向实现任务提供该文档的绝对路径及具体章节/决定编号。详细要求不再主要放在消息里，不要求实现任务从聊天记录拼接方案。
- 实现任务按执行文档连续工作、小步中文提交，将提交和验证证据写回现有的工作记录。沿用现有文档及写入分工，不另建重复方案，不抢写协调文档；规划任务负责把审查结果和下一批执行要求收回自己的执行文档。
- 同一问题的连续补充先合并成完整、可执行的一批文档变更。只有接收方确实需要开始/调整执行、处理无法自行解决的阻塞或接收已审查交付时，才发送一次简短通知，内容仅为“请读某绝对路径的某节，执行某项”或“请读某节处理阻塞/接收交付”。
- 普通提交、编译、进度、收到、已读、仍在等、无变化都不发消息；不要求通知回执，也不再发送回执的回执。改了文档不等于每次都要通知，局部小步合并到必要的审查/交付点再通知。
- TASK_READY、CROSS_TASK_QUESTION、COORDINATION_INVALIDATION等作为文档中的状态/事件标签保留；详细证据、影响和待决定项留在文档，必要时仅发路径和事件编号。通知必须指出哪个动作需要推进，不能用改标签继续发长报文。
- 只有确实特殊、现有文档信息不足以作决定的情况，才补充一次针对性的沟通；仍把问题一次说全，最后的结论回写文档。不把普通接口问题、重复状态或每次代码更新当例外。
- 已授权的事情继续自主执行，减少消息不等于增加确认或停工。无关任务继续已有工作；没有新动作就等待，不定时轮询、催报或广播。
- 各规划在开始下一批执行、完成一轮审查或准备交付等自然工作节点，读取本文件中与自己有关的最新问题/决定，再更新自己的执行文档；同一批读取通知之后新增的普通协调内容集中在文档读取，不为每次落笔另发消息。
- 所有沟通仍走既有规划/实现配对；跨任务通过协调窗口。各规划任务把本规则落实到自己的执行文档，再仅向自己的实现任务发一次读取通知；无需向协调窗口回复收到。
- 本次由用户明确要求，协调窗口向八个已登记规划任务各发一次切换通知；之后不为同一规则反复通知。Agent作者工具仍仅参与已登记的共享SubTree API范围，不扩大任务或MR。

## 本轮目标与授权

- 对现有、已经开发了一部分的 worktree 收尾和接入，保留已经做对的成果，继续原规划/实现配对。
- BTSMTL 继续现有重构，按职责清理超大类、重复逻辑和废弃路径，公共数据、编译、运行、表现的输入输出和依赖要清楚。
- MR 范围及顺序：ACL 接入 → Timeline → 相机。BTSMTL 主线提供三项必需的公共改动。其它任务不整支纳入本轮 MR。
- 已整理的 ZZZ 数据随对应功能进入正式作者数据、构建产物和运行链，替换相应旧数据及写死的业务实现；替换到位即清旧，不保留第二条路径。
- 用户已要求开始发消息，并明确授权协调窗口对推进中的问题作出决定，避免反复停滞；小步提交，提交信息用中文。
- 本轮方向取代此前“暂不推进 MR”的限制，仅针对上述三项；审查、依赖和实际构建证据仍须满足。
- 不新建任务或 worktree，不改现有任务模型、分支、目录。协调窗口不改实现代码、不替代各规划任务的代码审查。

## 接入设计

### 构建与数据

1. 以已有真实 ZZZ 数据和现有作者资产为输入，保留真实资产身份、内容引用和绑定；缺失数据明确报错，不编造占位业务。
2. Character 和独立 Timeline 使用显式的根描述，共用内容表达、Semantic IR、codec/envelope、目标编译和原子发布。
3. Character 根继续要求真实 BodyMotion；Timeline 根描述真实 Timeline 内容、入口和绑定需求，不伪造 CharacterDefinition、BodyMotion 或 ActionInstance。
4. 根身份贯穿 Manifest、header、正文、canonical hash、加载 expectation 和缓存键。升级唯一正式版本，错误根、旧版本、头正文不符明确拒绝。
5. Float32/Fixed 的必要降低、Program 构造/codec/loader 和 Character 调用者随公共合同一起迁入，保留 Source、producer、Skill、GraphCallFrame 与 typed 状态。
6. 运行时只加载已构建并重读验证的不可变产物；Scene Host 不现场调用 Builder/Lowerer，不再保留私有 Timeline codec 或第二套 store。
7. ACL 资源与实际引用它们的 Program/Projection、目标版本和资源映射一起核对，按同一正式发布组交付；旧的一次成功不覆盖新的代码、数据或依赖。

### Timeline 共同内容的具体接管

- 公共根提交31f111d7e0d811082c70730ac87364bcc9387011及入口提交ab7e727d8aa05514abdeb3104c78562eab5b5d90已产生，但主线规划review为CHANGES_REQUIRED，尚无TASK_READY。正确根header/payload/expectation与Character BodyMotion严格性保留。
- Timeline任务接管ab中TimelineSemanticContentDiscovery.cs、TimelineSemanticFrontendCompiler.cs、TimelineSemanticRootEmitter.cs及meta，并统一既有CharacterSemanticTimelineEmitter的内容部分和Registry中的Timeline来源适配。
- Character与独立根仅装配各自真实来源、入口、绑定/能力后调用同一内容发现/发射；catalog/session/Track/Clip/segment遍历不能重复。相同资产的多个调用仍有各自Source/activation/状态/绑定。
- TreeClip不能整类拒绝；按真实闭包与具体能力/绑定验证。Character图节点调用保持必要Graph/Node身份检查，独立根/C#调用使用真实根/调用来源，不靠全局允许空字符串或伪造身份。
- ab还改了Build/MCP/Target/报告和正式Timeline Program资产载体，这些通用部分仍归BTSMTL，不随三个领域文件转走。主线继续ACL A–D适配和公共根/缓存/正式导入工作。
- 31/ab是明确的开发接管材料，不是整提交合入批准。Timeline先交共同内容及Character委托接入的可消费小步；最后资源映射/发布/装载仍须闭合，不等于全部独立功能已完成。
- 正确版本更正：31f为IR14；Float Artifact17/Program19/Layout10；Fixed也为Artifact17/Program19/Layout10。先前传播的Fixed Artifact18撤回。Fixed头新增RootDescriptor却未升级Artifact版本，已交主线修正，后续只认实际审查交付矩阵。
- 临时补生成csproj的编译仅为开发检查，不代替正式Unity导入、程序集/meta与Character构建receipt。

### 根与共同内容的关联合同

- 31/ab与Timeline独立Invocation曾把同名ContentIdentity用作不同含义；现已按主线8f32945619e303b0d84465d6eeb5043de42abfab的实际校验统一，不增加影子根/codec。
- RootIdentity为精确顶层根资产GUID（32位小写hex）；Character是Definition，独立Timeline是TimelineAsset，不放运行调用ID。
- EntryIdentity为Program唯一稳定逻辑入口：Character为control:<ControlModuleId>，Timeline为timeline:<顶层AuthoringId>，对应选中的顶层ContentUnit.Identity，不用诊断route或调用前缀。
- RootDescriptor.ContentIdentity为64位小写hex的canonical内容指纹。Timeline比较既有ContentHash，不能与ContentUnit.Identity的稳定字符串比较；两者分别负责版本验证与稳定内容引用。
- 沿已有SimulationProgramRootValidation.RequireEntryReference：保留名program:root-operation每Program一次，SourceOperation无效、Kind=Operation、Target指向Root操作、ExternalIdentity等于根EntryIdentity。内容调用不得声明或前缀化保留名。
- 主线提供Builder既有不可变m_Root的只读入口及现行校验的最小完整开发依赖；Timeline按该唯一根装配/比对，不复制三份漂移字段、不提前Build偷读根、不用临时类型补齐。
- 8f已有Guid/hash/入口引用校验；读取时主线HEAD742cb300785fc113311714db3ae98999f152879a仍未见Builder公开Root。限定开发入口可先交付，正式Fixed/产物/Unity证据不自动通过。

### 相机交接决定 C-04

相机在本轮接入范围内，开发与主线/Timeline并行。第三项仅限制MR接收顺序，不构成开始开发的等待条件。

当前相机实际审查：46460e17d3cbe6aaccdec5e45c94eae6461fa908，git状态前后干净，CHANGES_REQUIRED。已有公共曲线、Corin默认Profile/首组效果、18 Zoom与18 Stretch共84个资产/meta/引用提交；810项字段/GUID和3条曲线关键帧核对相符，但不等于默认选择/时间/枚举语义或画面已经批准。

- 已有真实代码接线：Definition.CameraProfile→Projection compiler/Camera builder→本地CameraRuntime→FinalPose之后Present→Cinemachine rig ManualUpdate与CameraBasis回读；不是只存独立算法。
- 四次正式导入/脚本编译均通过，最新Run为9b19cc4108054ea292a3d7b2c8a40d25；只证明compile，不证明Definition产品重建或运行。
- 当前生成Projection仍v13且无Camera payload/defaultSequence，动作/Timeline资产未发现36资源的真实请求引用，所以镜头尚未形成完整可播放交付。
- Default_Normal是否为Corin实例实际选择未证实；源0.60000002与Profile3.4336302/Sequence比率0.5的单位换算、native infinity2与Unity公开WrapMode2的语义仍需真实验证，不宣称已经观察到循环故障。
- Override/Shake/Shot仍不支持，碰撞关闭且无正式开启能力；不把已支持小范围包装成全部ZZZ能力。请求索引/退出队列/帧装配继续拆清，保留已分出的FramePlanner/EffectEvaluator/Transition。
- 相机提供真实ZZZ调用/事件→资源/类型/时序/权重/目标/终止映射；Timeline写正式动作引用，不为36资源虚构36个调用点。新增引用随相机第三项的资源/产品一起接收，公共请求合同可先交。
- Timeline在19af51506确认：已有身份/分层/一次权重APPR保留，但相机generation/sourceInstance仍0、terminal仍Camera+Weight0、Force未闭合；当前确无完整可消费请求包，不只是消费者副本旧。历史3beb含已删除standalone候选，不能整批回灌。
- 精确写入分工：主线提供Core命令kind、合法Source/Header入口、Graph Camera提交、C-04公共事务/适配；Timeline统一领域回调/DTO和两TimelineTarget投递，按真实Header generation发Complete/Release/Force并处理Stopping/退出；相机负责本地消费与数据。已经正确的重叠小步按精确提交接收，不双写。
- 主线唯一新Projection版本和完整公共端口仍须正式交付。默认follow/aim目前是body可见Pose+bind offset，显式目标逐帧读Transform，不扩大成所有默认锚点读取最终骨骼。
状态：按用户本轮“遇到问题帮我决策，不要停滞”的授权，协调窗口选择原方案 A；不再等待逐项选择。

- 上游继续使用既有通用仿真输出记录，允许其中记录相机请求；由上游提供完整的旧/新命令和当前有效的停止、更新、新增请求。
- 只有整个输出事务成功后，才向既有正式表现队列提交本次有效变化；不得把每个 Replay 步骤重新播放给相机，也不能只丢弃 Replay 而漏掉已消失的来源。
- 相机本身删除专用回滚日志、快照恢复、Suspend/Resume/Confirm 和网络确认等待；保留正常镜头混合、淡出、平滑历史、真实播放身份和本地去重。
- 修复只给旧 eventId、下游已没有对应记录的 Replace/Retire 交接；停止请求必须包含实际来源/代际等所需信息。禁止恢复相机私有历史或空操作吞掉失败。
- Complete/Release 等必须按真实 Program/Projection producer 判断相机归属；本地相机结束不等 confirmedTick。角色、动画的回滚与确认逻辑保留。
- 网络出口和远端也按真实 producer 排除相机请求，不能只判断 Camera kind；不新增线协议字段，不退回旧协议或丢失真实 Source/Skill/状态身份。
- Timeline 提供真实 Source、调用/Action、producer、Header.Activation.Generation、cycle/时间和一次权重结果；正常停止、取消和强制清理均闭合。
- 同一正式表现帧先完成 Pose 最终发布，再执行 Camera.Present/rig 应用；沿用现有时钟和 ScenePlay 生命周期。

### 校正分支几何验证决定

- 仅处理 pose-correction 分支发布 Corin Projection 的现有前置，不新增本轮 MR、不让其它任务等待校正整体完成。
- 已决定有条件采用现有正式校准发布：先列实际 Unity identity 差异，证明 Rig/足底/Clip 的采样几何未变，再通过精确 character.foot_rig_calibration publish 重发。
- 正式工具会先配置并保存 Calibration，服务还可能同步绑定 revision、保存 Sampling Rig prefab；这不是保证只改一个 hash 的操作，失败也可能已写入前半部分。
- 允许同一几何下的必要绑定版本/验证元数据同步，单独中文提交并列全变更文件；不允许调整足底参数、骨骼结构、物理映射或算法来掩盖失效。
- 无法证明同输入或发现实际输入/合同变化时，给出具体差异和必要输入提交，由协调窗口确定后续交付范围；不默认等待未知 owner。
- 前置发布通过后沿原 Definition Build 重建 Projection；本任务自身 CHANGES_REQUIRED 独立继续，不能因前置成功整体报 TASK_READY。

### ACL 第一项 MR 的版本基准

状态：来源与职责已查清，组合尚未经过完整编译/审查，不能消费。审计快照：ACL eb3393ae225b775634970df555bf3a00674559e3；main 47f3e84fa256c7be4fedfca4393693544a69e4bf。主线随后提交 fca07f8adb84c36a02c6b330ec7be978583e34b8 仅改 Agent 状态机对账，相关 Core/CharacterSimulation/Build 与 47 相同。

| 项目 | ACL 已提交快照 | main 已提交快照 |
| --- | --- | --- |
| Float32 codec | Artifact15 / ProgramFormat17 / Layout10 | Artifact16 / ProgramFormat18 / Layout10 |
| Fixed codec | Artifact16 / ProgramFormat17 / Layout10 | Artifact17 / ProgramFormat18 / Layout10 |
| Projection ABI | v14（ACL 资源/属性布局） | v13 |
| Pose schema / runtime | v25 / v28 | v24 / v27 |

- main 的正式 Pose 根、四类 lease、同一 lineage 和最终发布已存在，无需等待 PoseGraph 再提供一个根，也不依赖其整个 Canvas change。
- ACL v25/v28 来自 590658b11，ACL 的 Projection v14 来自 a9c593bc3。相机 83aff47e1 也写 v14，但字段布局不同：ACL 增资源/属性和编译索引，相机增 Camera payload，不能互认。
- 已决定：主线在现有 Character/Pose 根上接收 ACL 的必要 A–D 增量，保留 main Format18 的子图调用帧、类型签名、Source/Skill/State 合同。根描述/Format19现已在31f提交，但review仍为CHANGES_REQUIRED，且Fixed头版本有碰撞；只有完整修正、正式导入/构建和审查通过后才能消费，不为赶工改回版本号或提前宣布就绪。
- 公共 Projection 由主线按最终布局分配新的唯一 ABI。读写、Create、校验、hash/expectation 和消费者一起更新，旧/混合格式明确拒绝；相机随后扩展 ACL 后的公共容器时，再统一对应组合版本。不添加兼容解释，不手改产物版本号。
- ACL 规划已对固定 Git 对象的 A–D 代码作实际链路审查，以 5f8b1015 限定 scope TASK_READY 交付；主线已获得该材料，可推进独立 Character 公共段与接收适配，组合仍须重新编译/review。PoseGraph 只复核具体 Pose 公共差异，不存在必须先等它生成的根提供包。
- 保护所有未提交差异，不代交、不回退；同一路径必要的固定提交部分仍需接入。交付索引列6个固定保护引用，包含CorinPoseGraph资产；此前观察到的GameplayLabBootstrap等其它脏差异也仍受保护，不能把列表当作覆盖其它文件的许可。
- c243d58f4 的 Corin Program/Projection（Program compiler22、Projection Pose203）不是新组合的配套产品。基准确定后由 ACL 正式重建；144 AnimationLibrary 压缩、质量与发布独立继续，不等待产品基准，门限不变。

本轮受限的 ACL 共享接入范围（固定源码已 APPROVED_FOR_RECEIVER_ADAPTATION；接收组合和产物尚未批准）：

| 范围 | 必须闭合的接口/调用者 | 来源锚点 |
| --- | --- | --- |
| A Source/资源 | typed readiness/scalar，CompletionIdentity 与 demand 对齐，编译资源目录/index、资源生命周期，保留后续 lease/recycle/MM 修正与最终模块拆分 | 500fb6fea、590658b11 及 eb3393 的最终已提交形态 |
| B 会话/root 调用者 | 唯一 AnimationResourceScope；BeginPresentation bool/AwaitingSample 清 Pending；调用者据返回值设置 Pending；Factory/Targets/Loader 同一 scope，先释放 Worker 再释放资源 | ed147fdb1 及最终修正 |
| C 既有最终发布 | ComposedAnimationPoseFrame 的参数/可用性双页，原 publication lease 下 ValidateFrame→骨骼/属性 writer；Reset 默认值、退出恢复初始值；不另造总帧/总事务 | 500fb6fea、44e39627 |
| D 编译与序列化 | Param Usage、Profile/资源/Clip/Blend/MM绑定、Projection Create/校验/revision、可选ScalarPage；原meta/目录meta/asmdef与同组Native插件/C ABI/资源manifest | a9c593bc3、590658b11、c326843e4 及最终修正 |

来源锚点不是 cherry-pick 白名单。取提供方最终已提交模块形态，核对完整依赖，不重建早期大类。保留原骨骼写入/Constraint 正确算法及 Source/Foot/Rig/Tuning/Blend/MM/LinkedPose/Equipment 全部字段。主线现有 Performance 正式源码直接保留，不复制 ACL 脏文件的 Performance 接线。

### 已接收的 ACL 共享代码交付

- 限定范围：A–D 源码接收适配；来源规划实际审查状态 APPROVED_FOR_RECEIVER_ADAPTATION，不能扩大到144最终产物、接收组合、IL2CPP或游戏端到端。
- 固定提交：5f8b1015eea1bd7bfea05ffd704456c41f4aec81。254个路径与先前被审查的eb3393对象逐个blob一致。
- 索引：[acl-shared-source-delivery.json](C:/Users/Lenovo/.codex/worktrees/dae9/3C/.codex-tmp/acl-shared-source-delivery.json)，SHA256为19914b1477c9b8912a8bab2236420e1efd4fe87630830f5f0e8787c4f67dd0b8。代码从固定Git对象消费，索引不是覆盖开发工作区的脚本。
- 协调窗口已核验：254个唯一路径（197新增、57修改、无删除条目）的源/审查对象blob与基线动作均一致，3项作者输入存在，6个固定保护引用匹配，2项旧Generated产品不在源码接收列表中。
- Native DLL Git blob为0925990c97549ad178e1c1438b3b82862cf17290，实际对象大小332288；来源规划报告SHA256为53e48c0f34fd67f6db14e90e6e83f49b7141ae8dda47fa324a8f9ada0815d789，同组ACL2.1.0/RTM2.2.0、ABI2/payload10/struct1。协调窗口未另行运行该二进制或复算其SHA。
- 作者输入为CorinFootPlacementRigCalibration、CorinPresentationPoseGraph、CorinAnimationPresentationProfile。接收方按目的地现有Rig/Canvas/Profile核对，不覆盖正确作者改动，不复制几何验证hash跳过正式检查。
- 已将明确索引/对象/范围交给主线推进适配。SourceModule、FinalPublication的新增接线按主线现有职责结构承接，不复活早期大类；已有拆分的backend/owner/scalar mixer/property writer保留。
- Build04与Build03是历史阶段证据，不能冒充纯Git或主线组合编译。两条精度问题后续已解决并完成144限定产物交付，详见下节；原失败日志保持原状态。

### ACL 144库与可选资源读取的最新交付

- A–D限定源码接收现恢复为5f8b1015来源加bb20fb38413305b1ecb6122c69b7e3279cdcafe9修正。仅CharacterAclAnimationResource两处可选Unity Object读取改为Unity bool判空；其它共享路径/格式/C ABI/压缩/混合不变。
- 已核验两组冷加载runtimeReady/inventory、Transform/Scalar读取、原完整group validator、quality JSON往返，以及导入索引后3次原子写入均通过；该局部失效关闭，不再等一份重复A–D大交付。
- Corin144 AnimationLibrary本地代码/源包/产物由来源规划APPROVED。代码bb20、首次稳定产物a8c5fe1b1611af09d36756abf00560c1e0e52c05、Library索引句柄修复7c2c69bb2029739fe0398b4f52f31a89748bed2c必须匹配。
- 144/144质量通过，150二进制共35101982 bytes；142条60Hz、2条15360Hz。两组地址acl-f723ad06654421585b964d30be0aa1bb8df72f07f4cc23500df00aedcbedb9b4和acl-07aeaca8588b6c92f492d24d0f7b0a919e92933ed0767a5607da8f538c5e6386，GUID分别0494948447b89984d916455077dab76e、4caa18dc8c99491439540b36b020ce1d。
- 源anim 4956573602 bytes清为0，保留ZIP261873058 bytes，SHA256 b2c81b4b990e3ae217a9a55f69a6f9efe33a9d13222a04471fe8f61795e0bc38。原模型physical/pose212，旧19 Gameplay201/203与84文件保持；库就绪不等于144已自动匹配现行Gameplay rig/图，不伪造原本142条没有的float曲线/表情。
- 从ZIP还原后150payload和两份quality与首次发布逐字节同；第二轮最后cleanup索引失败仍记失败。定向修复后复验，不重复无变化全量压缩冒充新成功Run。310资源/索引文件恢复首次稳定SHA。
- 最终Center compile d6a0b4b6761c497789f3d4a7135178e4 succeeded=true/source_changed=false，前后649e4bd2abf57f398df9eb4e6d79394ac1eb424c0ff43095d31d1529204eb103。属于保留其它dirty的开发工作区，非纯Git或主线接收组合；IL2CPP和游戏端到端未验。
- 证据：源worktree .codex-tmp/corin-library-final-review.json、corin-library-rebuild-review.json、corin-publication-probe-fixed.json；正式BuildInput.publication.json和Center editor-result。主线已收到，当前等待其实际接收组合/新ABI和配套Program/Projection重建。

### MR 与主线接收

- BTSMTL重构方案规划是本轮主线公共提供方和接收方，由其原实现任务执行必要代码整合和提交；协调窗口提供范围、依赖与顺序。
- 各来源规划任务先完成自身审查，给出范围明确的 TASK_READY、稳定提交及必要依赖；编译成功不等于整体运行验收或 MR 已完成。
- 只取经审查的完整依赖闭包，核对主线祖先或等价补丁。不得按几个文件名裁剪提交，也不得把整个分支的无关改动一起带入。
- 主线公共改动可先交付以解除开发阻塞；三个 MR 按 ACL、Timeline、相机顺序接收。各 worktree 的已有实现继续并行收尾。
- 合入时核对目标主线实际版本，必要的生成产物按正式入口重建；准确记录 MR 标识、接收提交和审查结果。

## 任务卡

| 规划任务 | 本轮具体责任 | 已知已有成果 | 下一次有效事件 |
| --- | --- | --- | --- |
| BTSMTL重构方案规划 | 清理现有 BTSMTL 职责；提供共用根/IR/codec/Builder/两 Target；正式交付所需 Build 源码与原子发布；实现相机公共输出交接；按顺序接收三个 MR | 已有 Skill、子图/调用帧等提交；已给出公共根合同，但尚无完整交付 | 公共依赖 TASK_READY；锁处理的定向回复；实际阻塞或本轮接收阶段结果 |
| ACL 动画运行链方案窗口 | 保持bb20修正与144库正式交付，等待实际接收组合后重建Program/Projection并准备第一MR | A–D及144库两份限定TASK_READY均已接收；150二进制、质量144/144、冷加载/索引复验与最终compile完成，非main组合 | 主线基准到达后的配套产品TASK_READY，或会推翻既有范围的新证据 |
| Timeline职责与独立使用（规划窗口） | 保留有效内容工作，接管主线ab的三个领域文件并让Character/独立根共用内容入口；统一TreeClip与来源校验，继续正式产物消费及生命周期收尾 | 原私有接法已被否决；新接管的公共候选也未通过整体review | 共同内容/Character委托接入的限定TASK_READY，或具体公共合同缺口 |
| ZZZ相机核心移植（方案窗口） | 并行完成真实ZZZ请求映射、默认/曲线语义、本地消费者；配合Timeline动作引用和主线C-04/新Projection | 46460e17d数据/代码链已有、4次compile通过，但当前真实接入review为CHANGES_REQUIRED | 已支持且语义/调用/产品闭合的限定TASK_READY或具体公共缺口；不等所有ZZZ能力 |
| 预览_规划 | 保持唯一 ScenePlay Start/Pause/Resume/Reset/Stop 和原正式运行入口；提供本轮确实需要的已有接入契约 | 已有预览迁移工作；最近环境故障不能算运行通过 | 仅真实依赖阻塞、已审查的必要接口交付或旧结论失效 |
| PoseGraph 编译与运行架构方案讨论（Sol） | 保护已有 Pose/Canvas 成果，对实际共享边界作来源对账和必要定向复核；原根已在main，不另造ACL前置根 | 已完成ACL固定对象来源/签名对账，非组合APPROVED；Canvas迁移批准不覆盖ACL产物 | 仅具体公共差异复核、必要依赖或实际冲突 |
| ZZZ BoneAdjust 与 3C PoseGraph 方案审查（Sol） | 继续原任务范围；校正结果通过既有 Pose 页面与最终发布接入，相关 ZZZ 数据不得另建执行根 | 已有校正 kernel/采样与作者工作，局部完成不等于整个任务可合入 | 仅本轮必需的共享交付或实际冲突 |
| BTSMTL Agent作者工具（规划窗口） | 消费主线正式SubTree/Graph引用与Port Shape API；负责唯一Document、整包计划/符号/Mutation/事务，不反射补私有字段 | 已有独立规划/实现配对和Agent工作；共享挂载/绑定API尚未正式交付 | 收到正式API后自身消费验证；真实共享缺口/限定TASK_READY/失效事件 |

后三项不是本轮新增 MR，也不是默认全员完成后才能推进的条件。现有授权工作继续，只让实际必需的接口形成依赖。

新增窄范围协调成员：BTSMTL Agent作者工具（规划窗口）。主线提供其既有任务身份后，协调窗口核对同项目标准规划标题、历史正式IMPLEMENTATION_LINK及规划/实现职责，按用户自主协调授权建立一次COORDINATION_LINK。仅加入本共享作者API与直接依赖关系，不创建任务、不改变原配对/模型/目录、不联系实现，不新增MR。

## 已确认事实与问题

| 编号 | 状态 | 事实/影响 | 负责方与推进方式 |
| --- | --- | --- | --- |
| P-01 共用 Timeline 根 | 已提交，review仍CHANGES_REQUIRED | 31f公共根和ab入口已提交；IR14/Program19存在，但领域重复、资源/发布、源/入口/缓存校验与正式导入证据尚未闭合。正确根与Character BodyMotion约束保留 | 主线继续Character公共/ACL接收交付，Timeline接共同内容；不以提交存在或临时csproj编译替代TASK_READY |
| P-02 Build 正式来源 | 源码入库缺口已关闭；组合验证仍待交付 | d8f239431fc64d40c8f7d174dec2765f9f883530 已将六份 Build 源码及六份 meta 纳入 main；主线规划核对12个blob均同历史a70，并加入精确源目录忽略例外；47/fca Git tree已实际包含 | 不重复导入或回退其它既有.gitignore规则；继续完成共用根/ACL接收组合的真实构建与review，不把源码入库等同整体TASK_READY |
| P-03 ACL 资源发布 | 144本地代码/源包/压缩产物已限定TASK_READY | 144质量全过，150二进制35101982 bytes；bb20冷加载/复用和7c2索引修复已定向验证，稳定产物a8；原第二轮最后索引失败历史保留 | 主线可接已批准库与修正，不再等待重复压缩；尚需接收组合、实际rig/资源关系和Program/Projection重建，非MR/E2E完成 |
| P-04 相机公共请求交接 | 已定责，完整请求包尚未交付 | Timeline19af仍有0身份、Weight0终止、Force缺项；相机464消费者旧公共端口仍按旧eventId及Kind处理。已批准身份/分层成果保持 | 主线提供Core kind/合法Header、Graph提交和通用事务；Timeline接领域DTO/两Target及完整生命周期；相机接本地消费，独立小步推进 |
| P-05 主目录 Git 锁 | 新锁已报告，主线按本项核实；原凌晨事件仍关闭 | Timeline规划11:32报告主.git/index.lock再次存在，0字节、创建/修改10:23:13；仅见3个fsmonitor，无add/commit进程，尚未确认持有者。此为来源观察，协调窗口未删除锁；timeline-runtime自身无锁 | 仅主线核实精确锁持有/写入状态，不能仅凭0字节或未见add/commit认定遗留；确认无活跃写入且为遗留后，只处理该index.lock并在既有文档记录。证据不足保留并写明实际阻塞；不改index/回退改动，不扩成Timeline停工原因，不处理另一个worktree历史锁 |
| P-06 校正分支 Foot 几何验证 | 有条件正式重发已下达；实际差异待源任务取证 | pose-correction 7deae2369 的 Definition Build 在 Projection 阶段报几何验证身份过期，尚无证据说明哪一字段变化或主线有 bug。该分支 Correction 自身仍为 CHANGES_REQUIRED | 校正任务读取真实 Unity 输入/报告；几何相同则通过现有精确发布工具重发、独立提交并重建 Projection。实际几何变化则给协调窗口定交付范围；不扩大三项 MR |
| P-07 ACL 公共产物版本 | 接收源码已提交，正式产品被主线Foot验证阻挡 | 主线b2d0e0d0/466967df报告254来源路径齐、244原blob同、10处接收适配，Projection v15及bb20 blob已纳入；尚无接收组合TASK_READY | 主线完成本实例Foot前置与实际接收矩阵/产品，再通知ACL重建配套Program/Projection；不拿旧v13/Pose24/27 receipt顶替 |
| P-08 Projection 同名 ABI 冲突 | 主线接收源码已选v15，组合产品未验 | ACL/相机旧v14布局不同的事实保持；主线已报告ACL接收版Projection v15，未等同已发布Camera组合 | 相机后续保留ACL完整字段扩Camera，主线统一组合版本/identity/校验并正式生成，不用旧v14互读 |
| P-09 Timeline内容重复 | 已交Timeline接管统一 | ab的RootEmitter与CharacterSemanticTimelineEmitter重复catalog/session/Track/Clip/segment流，Character未委托共同入口，TreeClip整类被拒绝；Registry还全局放宽Graph/Node为空 | Timeline统一三个领域文件、Character内容适配和来源校验；主线保留通用根/Build/正式Program资产职责，ACL适配继续 |
| P-10 Fixed产物头版本 | 已交主线修正，未交付 | 31f新增RootDescriptor头字段但Fixed Artifact仍17，与旧头格式共用版本；正确当前数值是Artifact17/Program19/Layout10，先前Artifact18报告已撤回并通知Timeline | 主线修正版本/头读写与正式旧包拒绝证据，再给稳定矩阵；其它不依赖此项的工作继续 |
| P-11 Fixed状态槽诊断 | 根因已改正为查询目标类型错误，修复/回放待验 | 同一旧包1092槽对应StateSlot1085+ControlState7；11..17来源完整。Adapter却全部查StateSlot，按Control owner选ControlState后静态缺项0 | 主线只修正确语义目标选择及相关消费者；撤销补来源方向，不重复映射/降级/按槽号/兜底双查。原trace仍0帧，未宣告复验通过 |
| P-12 ACL可选Unity资源冷加载 | 局部失效已关闭，bb20必须消费 | 旧?.bytes/??对Unity反序列化空TextAsset抛异常；bb20仅改两处Unity bool读取，两组完整复用及3次索引重写已通过 | A–D其它范围保留，主线不可继续用5f旧两行；Library7c2与原失败历史分别记录 |
| P-13 相机真实作者/产品接线 | CHANGES_REQUIRED，分工已落实 | 18Zoom+18Stretch数据和runtime/rig代码链已有；Generated仍v13、真实动作引用缺失，默认源/角度及曲线枚举语义未证实 | 相机给真实请求映射并修域内语义，Timeline接正式动作引用/请求合同，主线给C-04和新Projection/产品；第三只是MR顺序 |
| P-14 主线ACL后的Foot几何前置 | 已授权本实例有条件正式重发，实际差异待报告 | 主实例e852、job25a237be正式Float32产品构建19条Projection错误，SamplingRig/Calibration Preview Pose identity stale，无新产品；不能用校正P-06冒充主线已验 | 主线先读本实例stored/current及几何，若同输入直接精确正式publish并Build；实际几何变化给具体差异由协调窗口决定，其它任务继续 |
| P-15 根/内容身份关联 | 合同已定，Builder只读根小步待交付 | ab把ContentIdentity写ContentHash，Timeline曾与ContentUnit.Identity稳定ID比较；主线现行8f已明确64位hash及入口引用关系，Builder未公开只读root | 主线提供实际只读根/校验及开发依赖；Timeline分别校验资产、入口/内容ID与指纹，保留program:root-operation唯一声明，不复活前缀/兼容Reader |
| P-16 SubTree共享作者写入 | 主线原9.1–9.3负责，Agent已接入窄范围沟通 | 需要typed owner/slot、inline/shared引用、serialized owner/graph/scope、稳定declaration/port绑定和统一Port Shape；工作区出现SetSubTree草稿但UI仍有旧TargetTypeMap/名字规则，非正式交付 | 主线交完整业务API并迁入UI删重复；Agent保留Document/计划/符号/Mutation/事务及Undo/保存/Build组织，等正式签名后接绑定，独立工作继续 |
| P-17 主线 Equipment 消费者编译 | 主线收尾消费者迁移；正式恢复待审查 | Agent报告主实例force refresh后18条CS1061，两Target的EquipmentRuntime仍引用已删除EntryOperation/PersistentEntry；证据JSON及SHA已核验，未宣告新源码装载 | BTSMTL沿已定Equipment输入迁移两端消费者，不恢复废弃字段/stub/兼容，完成正式导入编译并把稳定交付记入既有文档；Agent继续独立字段对账，生命周期验证待实际编译条件恢复 |

本次 P-11 的最终证据口径：

- 原Run ef59fbe07ae54f94814fe5ec22f2d533，input trace43357ff3cd384e5cba75d2c31175b116（1044帧），运行0帧。源提交1cf080fa63fb06aa3a76376c53e87acdd5eeacdd，identity042e051b50370cf7110fac3abdfafde8ae149a057c0d206ed57b3d9cf3201e9f。
- Run目录D:/Unity_Project_1/3C-Artifacts/3c-gameplay/Runs/ef59fbe07ae54f94814fe5ec22f2d533/；只有status失败记录，无receipt/result；Logs/unity-editor.log:629/640为首错，四份status/log/asset/bin的SHA已由协调窗口核验。
- 实际资产为timeline-runtime项目的Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset，GUID91063668fd0eaa84d9b7d688aadbc90a；固定1cf对象保留相同字节。对应Library/CharacterSimulation/Fixed/c7a7c1e3f7e64d81b5a04a90cbeb8d4e.fixed-program为2584769 bytes，SHA740ffc1dd1b5656d7e97aa7cea7a38fdd6d9ad67f1d426857aa7b287c968fbc4。
- 旧包格式Artifact16/Program17/Layout10、compiler24/operations12/fixed-q32.32/TargetABI7/60Hz。ProgramHash77dceaa6d70e2d0d3fee1303338fabad12c0272e6c2f9d62138b6214f7e1aaaf，LayoutHashecaa6a86758fd957ab3919f1d3ab2708af00a57312dd318cc4feaa37dfd754cd；不是31的新根格式。
- 1092槽、3620来源中StateSlot1085条另有ControlState7条；11..17分别active-state、entered-tick、last-transition、transition-progress、motion-elapsed-ticks、directional-dodge-run-intent、dodge-forward-completion-instance，owner均character.corin.control。
- Timeline最初仅筛(StateSlot,index)而推断“原来源缺7条”，协调窗口曾转发，现已明确撤回。主线完整Reader证明7条都在ControlState，旧Builder/DebugBuilder映射正确；按owner选择正确类型覆盖缺项0。证据bts-timeline-control-source-review.json，SHA8ca97456d7aaf7ad3690e341649dd7d86c94b90f114c7720097a2b40dee9aad3，已核验。
- 主线修诊断及相关消费者的目标类型选择，不能补重复SourceMap或假Graph/Node，不按物理11..17硬编码，也不先查失败再fallback到另一种。修后原正确实例复验实际帧数；当前只有字节/静态链定位，没有Replay通过。
证据边界：以上提交与版本是事件发生时的观察值，不固定为永远最新。没有新的规划审查证据，不把旧 TASK_READY 扩大到新提交或新产物。

本次 P-14 的主线发布边界：

- 接收代码b2d0e0d093cafd1d4d573b5f4136736d270f8fe9、466967dff02b92858b5401c50c802214473105c4；正式character.build_float32_products job25a237be85b34263b2704289e0de245e失败，无新产品。
- 主项目D:/Unity_Project_1/3C/3cDemo/Client/3C_Client、实例e852139597e42532。证据D:/Unity_Project_1/3C/.codex-tmp/bts-acl-main-build-25a237be.review.json，SHA b8d1d5985618ee51abec1bfc767eb1a04a177788605c4003c5a2d6417873e1dd已核验。
- 与P-06相似的Rig GUID/calibration b4不能证明本实例几何已核验。主线自己读取stored/current与报告，同输入只需依赖/绑定/验证身份刷新时，直接用已有精确foot_rig_calibration publish；保存前后实际Calibration/prefab变化及几何依据。
- 实际骨骼/物理映射/足底/采样几何改变时，不按“刷新hash”处理，不调参数遮盖失败，交协调窗口按具体差异定范围；C-04/Control诊断/其它重构继续。旧v13/Pose24/27 receipt不得顶替ACL组合。

共享作者API边界：主线业务操作只管模型规则、真实引用身份与端口/参数一致性；不接管Agent的Document/Undo/保存/Build。UI与Agent必须使用同一业务实现，不能由Agent使用FindFieldAccessor/私有类型别名表建立第二套规则。已有SetSubTree工作草稿不得当正式入口；提供方须交真实成员、实现、UI使用、必要提交与编译/Validator证据。成员间共享问题通过协调窗口，不直接联系其它规划/实现。

## 小步提交与质量条件

P-17决定（agent-lifecycle-current-equipment-compile-block）：这是共享编译的直接依赖，由既有主线提供方收尾Equipment消费者，Agent不接管共享源码，不新增MR范围。若已在迁移，合入正在进行的同一工作，不另起一条修复路径。来源报告HEAD d8d8481dae87a96e46111cd51f76c70b7a73ee91；精确项目D:/Unity_Project_1/3C/3cDemo/Client/3C_Client，实例e852139597e42532。报错文件为Assets/GameScripts/Main/Runtime/Simulation/Core/Fixed/Execution/FixedEquipmentRuntime.cs和Float32/Execution/Float32EquipmentRuntime.cs。证据D:/Unity_Project_1/3C/.codex-tmp/agent-authoring-unity-console-20260906.json，SHA256 6FD8EC9F4E5266AB1F826A27A8ECBAA82D6DB5FB46C7068FB0EE98521D359AE1，已核验18条记录。旧已加载程序集能应答工具不证明候选源码已装载；Agent no-op产生7条set_skill_definition为自身对账问题，继续自己处理，不转嫁共享API。交付条件是来源规划审查后的稳定消费者提交及本项目正式导入/编译证据，再由Agent完成真实Mutation/回滚验证。此项合入本轮文档读取，不为接到一份报告立即再发一条长消息。

- 每步围绕一个可解释的职责或可消费合同提交，中文标题和具体正文，保留源码/元数据/必要生成产物的完整关系。
- 不把正确代码回退；实际交叉修改由协调窗口确定唯一提供方和接入范围。不得复制未提交工作区充当正式依赖。
- 不靠拆文件掩饰职责混杂。规划审查应说明输入、处理、输出、依赖及旧路径删除情况，并给出关键调用链。
- 不新增测试代码；完成项目要求的必要编译、正式构建/已有验证，真实记录未做的运行和用户端到端验收。
- dotnet build/msbuild 使用 --disable-build-servers /nr:false /p:UseSharedCompilation=false，结束后关闭 build-server。
- Unity 调用显式指定正确实例/项目路径，保留主验收 Editor；不为过关删 Library、禁用依赖或搭临时兼容路径。

## 事件推进规则

任务只通过原规划窗口向协调窗口汇报，不建立其它规划/实现任务之间的直接通信。

1. 来源规划把 APPROVED/TASK_READY 的具体范围、稳定提交、依赖、实际证据和剩余验收写进自己的执行/审查文档；需要被消费时仅通知该文档绝对路径和事件编号。
2. 真实跨任务阻塞在同一文档写 CROSS_TASK_QUESTION，集中写清证据、受影响输入输出和需要的决定；需要协调动作时仅发文档定位，不重复催报。
3. 定向问题的回复、已有决定或就绪状态的失效也写文档；仅在需要对方调整行动时发一次读取该事件的通知。
4. 有效事件到达后，协调窗口读取对应文档、核对影响、把职责/接口/顺序决定写回本文件；仅通知需要改变行动的责任方读取具体决定。
5. 公共交付及当前依赖均就绪后才推进当前MR；先将接收范围与动作记档，再向实际消费者发一次文档通知，当前MR接收确认后才推进下一项。
6. 不要求“收到”回执、日报、普通编译/提交进度，不做定时轮询、心跳或全员等待。没有可行动事件时保持等待。
7. 用户已授权协调窗口处理推进问题；决定写明原因、范围、承担方和交付条件，不为普通接口/依赖选择反复向用户索要确认。
8. 普通提交、编译和重复状态只入文档，不逐条确认或转发；同一问题集中处理，已有分工不重发。仅按COMM-20260906-01发送必要的读文档通知；本次用户明确要求的一次性通知后，不再重复广播规则。
9. 面向用户只汇总重要成果和重大变化；用户询问时直接回答，不把每次后台协调改写成一条用户通知。

## 消息投递

本轮消息标识：night-20260906-existing-worktrees-v1。

| 目标 | 状态 |
| --- | --- |
| BTSMTL重构方案规划 | 已发送，工具确认成功 |
| ACL 动画运行链方案窗口 | 已发送，工具确认成功 |
| Timeline职责与独立使用（规划窗口） | 已发送，工具确认成功 |
| ZZZ相机核心移植（方案窗口） | 已发送，工具确认成功 |
| 预览_规划 | 已发送，工具确认成功 |
| PoseGraph 编译与运行架构方案讨论（Sol） | 已发送，工具确认成功 |
| ZZZ BoneAdjust 与 3C PoseGraph 方案审查（Sol） | 已发送，工具确认成功 |
| BTSMTL Agent作者工具（规划窗口） | 本共享作者API窄范围链接与边界转达成功；不属于最初七项广播 |

## 事件记录

- E-001：用户确定范围是现有 worktree 收尾，MR 顺序 ACL → Timeline → 相机，并要求使用已有 ZZZ 数据替换对应旧实现。
- E-002：主线提供共用根合同与 Build 来源缺口的定向回复；尚无完整公共交付，正确的 Skill/子图/调用帧成果保留。
- E-003：ACL 修复共享包 .meta 并在原任务中重试；只关闭该元数据故障，未宣告 144 构建完成。
- E-004：Timeline 报告主目录 index.lock；交主线写入方处理，不影响各 worktree 继续工作。
- E-005：用户要求维护进展文档、完整下发、小步提交、事件推进，并授权协调窗口直接决策；C-04 选择既有通用输出纠偏，P-02 本轮必要 Build 发布交 BTSMTL。

- E-006：2026-09-06 01:33:44 +08:00，7 个规划任务全部投递成功。前四项下达完整实施范围，后三项通知本轮边界；不要求收到回执。C-04 相机交接、必要 Build 发布归属和主索引锁核实均随对应任务下发。

- E-007：主线定向回复 main-index-lock-owner：01:34:53 精确路径已无锁，关闭本次历史锁阻塞；不认领删除者、不改他人文件，也不扩大为未来无竞争或公共交付完成。已向 Timeline 解除该项等待。

- E-008：收到 pose-correction-foot-geometry-publication-20260906。协调窗口核对了正式 validator、发布服务及工具的写入顺序，已下达有条件的同输入正式重发决定；明确可能写入 Calibration/绑定 prefab，禁止手填身份或混入几何调整。仅解除该分支前置，不改变本轮 MR 队列。

- E-009：ACL 报告已审查的 Program/Projection/Pose 公共版本分叉。协调窗口核对提交身份、代码位置与六个未提交路径，决定由 BTSMTL 组织接收组合、PoseGraph 核对必要公共来源；各发送一次 acl-public-artifact-baseline-20260906 定向请求，并通知 ACL 继续独立的 144 资源工作。未指定尚不存在的稳定提交。

- E-010：PoseGraph 返回固定对象来源审计：ACL v25/v28与v14均来自ACL；main已有原Pose根和最终发布，属性发布在ACL已有实现。发现相机另一套不同布局的v14。协调窗口下达 coordination-acl-main18-shared-increments-20260906，明确main原根接ACL最终A–D形态、新公共ABI、排除未提交差异而非整条路径，已通知主线/ACL/相机。
- E-011：主线返回已提交格式矩阵、d8的12个Build blob与忽略例外、Format19未提交草稿及下一份独立Character交付。协调窗口关闭Build源码缺失项，纠正其仍等待Pose新包的旧前置；ACL共享代码可先独立审查交付，不等待144全部产物。当前仍无组合TASK_READY。

- E-012：收到 ACL共享接入代码 A–D 的限定 TASK_READY（5f8b1015）。协调窗口核验索引SHA、254个源/审查对象blob及动作、作者输入/保护引用/排除项和Native对象大小，通过后立即交主线推进接收适配；未扩大为纯Git编译、144产品、接收组合或MR完成。ACL继续两条精度问题与正式发布。

- E-013：主线报告31f/ab审查未通过，发现Timeline内容双发射与职责越界。协调窗口核对固定对象，补充发现Registry全局允许空Graph/Node，已下达timeline-single-content-emitter-handoff-ab7e727d8给Timeline和主线，明确三个领域文件及Character内容适配的唯一所有权，通用根/Build/资产载体仍归主线。
- E-014：主线更正Fixed格式并报告新增头未升Artifact。协调窗口直接核对31f，两Target均Artifact17/Program19；已向Timeline撤回错误的Fixed18说法。主线继续修正/旧包拒绝验证，不改变内容接管或CHANGES_REQUIRED。

- E-015：Timeline报告Fixed启动时slot11缺调试来源，1044帧输入未开始回放。协调窗口核对注册与DebugMap构造链，交主线负责公共生产/消费映射定位修复，同时仅向Timeline补取该Run实际程序/槽/映射与绝对证据路径。若根因落在具体Timeline发射点再按最窄范围交回；共同内容接管与此前APPROVED保持。

- E-016：用户追问镜头接入。协调窗口明确相机仍在本轮且并行开发；核对Git发现已新增Corin曲线、默认镜头、Zoom/Stretch资源提交至46460e17d，未把旧任务快照当当前审查。已再次向相机明确接入范围并作一次定向核对，主线同步保持C-04接口并行提供；不自动扩大为TASK_READY或合入完成。

- E-017：ACL报告5f交付中两个可选Unity TextAsset读取在冷加载失效，bb20两行修正；协调窗口核对仅一共享文件变化并立即通知主线替换旧读法，其它A–D保留。
- E-018：相机464实际review CHANGES_REQUIRED返回，证实资源/代码链已有但旧Generated、动作引用、默认/曲线语义未闭合；协调窗口落实相机请求映射、Timeline作者/请求合同、主线C-04/Projection三方接线，明确并行推进。
- E-019：ACL以同一冷加载/完整validator/索引三轮重写证据证明bb20修复，局部失效关闭，恢复包含bb20的A–D限定接收。
- E-020：ACL144库代码/ZIP/压缩产品限定APPROVED到达，协调窗口核对复验与最终Center证据后交主线；不重复全量压缩、不把第二轮原失败改写成成功、不扩大为main组合。
- E-021：Timeline提供原故障资产/bin/Reader数据，协调窗口核对四份SHA并转主线。当时仅按StateSlot类型筛选而推断七Control来源缺失，该推断随后由E-024撤回。
- E-022：主线报告b2/466接收源码与Projection v15，但job25a237be在本实例Foot几何身份失败。协调窗口授权基于本实例实际同几何证据的精确正式重发，P-06不作为主线已验证依据，其它工作继续。
- E-023：Timeline19af请求合同审查确认现有身份/分层APPR有效，但真实Camera generation/sourceInstance、normal/cancel/force未闭合；已明确主线Core kind/Header/Graph/事务、Timeline领域DTO/两Target、相机本地消费的单一写入范围。
- E-024：主线完整原bin证明ControlState7条完整，消费者查错目标kind。协调窗口核验更正证据，向两方撤回补来源方向，仅主线修语义目标选择；未声称原trace复验成功。

- E-025：Timeline报告根ContentIdentity与稳定内容ID混用。协调窗口核对31/ab及8f现行校验，确定资产GUID、稳定入口/内容ID、64位内容指纹的唯一关联；两方已收到只读Builder根与保留根引用的最小交付决定，Inline TreeClip owner问题仍留Timeline。
- E-026：主线转来既有Agent规划的SHARED_AUTHORING_API_GAP，原9.1–9.3已负责实现。协调窗口核实正式规划/配对后按用户授权仅将该API沟通纳入组，建立链接并转达主线业务API/UI共享与Agent Document/事务边界，不新增任务或MR、不以反射绕过未交付接口。

- E-027：Timeline报告10:23创建的主目录新锁，原01:34关闭结论不适用于这次锁；只交主线精确核实，Timeline无本地锁而继续。其4d61b82fc两Target精确Corin发射通过的来源进展仅记档：Projection仍被本分支Foot geometry identity stale阻断，未发布新Program，独立Root正式调用链未完成，不作为TASK_READY转发。
- E-028：用户指出消息频繁；协调窗口收紧消息门槛，停止逐条回报确认、重复分工和普通进展转发。同一问题聚合，必要行动仅联系责任方，用户仅接收重要结果；不为宣布降噪向全组再发一轮消息。
- E-029：用户进一步明确要求通知各规划任务，协调决定直接写文档由任务自行读取，规划到实现也以文档为主、消息极少；提报协调时应一次把问题说全，仅特殊情况补充沟通。已形成COMM-20260906-01并取代旧长报文规则；本次按用户明确要求向各已登记规划任务一次性通知，后续只保留必要的执行/交付/阻塞文档定位通知，发送结果记下方。

- E-030：收到Agent完整共享编译阻塞报告，核对原JSON及SHA后将P-17归属、正式迁移要求和交付条件直接记档；沿主线既有迁移处理，Agent独立对账继续。合入刚发出的本轮文档读取，不回复收到、不再转发长报文。

COMM-20260906-01通知状态：八个已登记规划任务各一次通知，8/8工具投递成功；均要求读取本节、落实到既有执行文档并少量通知自己的实现任务，无需回复收到。主线同一条通知附P-05定位，未另发锁问题长报文。仅确认投递，落实情况留待必要交付/审查事件，不催回执。

当前阶段：任务按既有分工推进，只处理会改变下一步行动的事件。未宣告任何本轮 MR 已完成。
