## Context

本稿按“完整动画源接入现有播放链”的范围定稿：包含片段自带骨骼与 BlendShape，复用现有 Player、曲线混合和唯一 Final Publication；不增加独立眨眼、说话或表情状态机。数据依据见 [ZZZ 复核记录](D:/Unity_Project_1/3C/openspec/changes/add-acl-animation-runtime/research.md)。本文件定义待实施行为，不将规划完成等同于 Runtime 已验收。

离线前置已完成：8 个完整片段的 Scalar 结果已通过限定范围的数据复审。正式 3C AnimationClip 属性发布、当前 Mesh 对应关系和 Runtime 接入属于本稿实施任务；离线 fork 不替代下文规定的项目官方 ACL 版本和 C ABI。具体证据及未覆盖的格式边界见 research.md 第 12 节。

当前表现链已经有明确的唯一 owner：`CharacterPoseSourceModule` 管理物理 source，`CharacterPoseProgramRuntime` 管理状态、过渡、slot 和权重，`CharacterPoseFrameCoordinator` 在唯一的 Animancer `PlayableGraph` 上执行 Evaluate Barrier，后面继续进入 Goal Assembly、FBBIK 和 Final Publication。`CharacterPoseProgramSourcePreparationRuntime` 还会把现有 `AnimationScriptPlayable` 工作插入同一张图。

ZZZ 的已检查 UnityPlayer 含 ACL 家族解码、数据库接入和 LRU Sweep 相关代码。原始资源包含版本 8 的 Transform、版本 100 的 Scalar，以及与外置 bulk 分开的数据库头。旧 `20260904_acl_native_v1` 和旧展开 `.anim` 存在数据库头或 Scalar 缺口；新的离线工具已修复对应输入/解码路径，仍需按正式目标清单逐项发布。项目继续以正式 AnimationClip 为 authoring 真相，不把私有 ABI 或游戏偏移纳入 Runtime。

## Goals / Non-Goals

**Goals:**

- 生成带完整身份和质量合同的 Transform、Scalar、Database/Bulk ACL 资源。
- 以固定 C ABI 连接项目锁定版本的 ACL C++ 解码器，使用预分配 Context 和 Pose writer。
- 在异步准备、流入、使用、retirement、Seal 和 release completion 中保持现有 Source 生命周期语义。
- 让 ACL source 通过现有 `CharacterPoseSourceModule`、唯一 Pose Plan、唯一 Evaluate Barrier 和唯一 PlayableGraph 产生表现 Pose。
- 让 Projection 在编译期决定 source backend，并让 Runtime 只消费 dense identity、格式和资源句柄。
- 以只读事实记录资源状态、压缩体积和解码成本，且关闭诊断不改变正式结果。
- 让片段自带 BlendShape 与骨骼共用 effective time、readiness、参数混合与同帧最终发布。

**Non-Goals:**

- 不修改 UnityPlayer，不接入 ZZZ 私有 `UnityPlayer.dll`、`AnimeStudio.ACLNative2.dll` 或 Animage 私有 ABI。
- 不替换 Animator、Animancer、Pose Program、Transition、Goal、FBBIK 或现有骨骼 Writer 数学；允许唯一 Final Publication 的合同和内部写入流程增加 BlendShape。
- 不为 ACL source 建立第二张 PlayableGraph、第二个 Player、第二个 Pose buffer、第二个最终写入器或独立 Preview 链。
- 不在 ACL backend 中实现状态机、过渡、脚步锁、IK、Goal、Gameplay 或 Foot Placement 修正。
- 不以最终 Pose 低通、IK 或脚步校正掩盖 ACL 解码误差。
- 不在 ACL 资源未就绪时回退到展开 `.anim`、历史 Pose、默认 Idle 或其它播放器。
- 不增加独立表情 Player、时钟、状态机、眨眼/说话逻辑或隐藏的末端曲线覆盖。

## Decisions

具体模块、接口、所有权和对当前草稿的拆分迁移顺序见 [ACL 模块化实施设计](implementation-design.md)。该文件由设计窗口定稿，实现窗口按其落实；它保留本稿全部范围，并将共享资源/独占 Decoder、统一采样和唯一发布细化为可执行边界。

### 1. authoring、资源和 Projection 身份分层

`AnimationClip` 继续拥有 authoring 时间曲线。现有 Presentation Profile 的资源绑定明确选择 backend 与压缩配置，Editor builder 从正式 Clip、Rig 和完整 binding 生成不可变资源。manifest 分别声明 Transform、可选 Scalar、Database Header、各 quality tier 的 Bulk，保存存在标记、字节范围、16 字节对齐、各块版本/hash、track-to-bone 或 scalar binding、reference/default 值、源身份、正式 start/stop/loop、采样网格、误差设置和构建身份。无 Scalar 或某 tier 长度为零必须显式声明；不能与必需文件丢失混为一谈。

Projection 编译为唯一 dense 资源表。Action、Direct Clip、Blend Space、MM 与 Preview 都从该表取得 backend 及资源身份；ACL 条目不保存 authoring AnimationClip 的运行时强引用。作者继续在现有 Profile/Source Binding 中编辑和显式 Build，不增加窗口自动构建、OnInspectorGUI 重操作或另一套资源配置。若该字段进入 Agent Document，schema、exporter、reconciler、mutation 与 validator 必须同步。

ACL runtime 不读取 AnimationClip 曲线、AssetDatabase 或作者字符串，也不从 Projection 重新推导绑定。NativeClip backend 继续消费其正式 Clip。项目只支持声明并有消费者的轨道；不能悄悄丢掉 Renderer、Animator、PPtr、离散值或未知脚本曲线后宣称完整。

选择项目自有 manifest 而不是直接保存一段无身份的 `.acl` 字节，是为了在 Rig、轨道布局或压缩版本错误时返回 `Invalid`，而不是把错误数据解释成看似合法的 Pose。选择从正式 Clip 构建而不是把现有展开 YAML 作为运行输入，是为了避免把巨大的展开数据继续带入内存；选择项目锁定版本而不是复制 ZZZ 数据格式，是为了不把不可验证的私有 ABI 变成项目依赖。

### 2. native 解码边界

项目固定官方 ACL v2.1.0 的 commit `414689d5cff4286a7898487a46dc5e48005d38da`，同时锁定其数值依赖、编译选项和项目 C ABI。项目 payload 使用该版本正式格式 10，不接受 ZZZ 私有 Scalar 100 或重写版本号。该版本数据库能力用于 Transform；Scalar 使用正式独立数据流。Managed 侧只持有 identity、opaque handle、预分配输出 binding 和 release token，不能依赖 C++ 对象布局或跨 DLL 释放内存。

C ABI 明确 struct size/version、固定宽度字段、calling convention、16 字节 aligned allocation、输出容量、错误码和唯一销毁入口；C++ 异常不越过边界。Context、scratch 和现有 source 页来自准备阶段及编译容量，帧内不进行托管或 native 堆分配。共享 payload/database 与每个活跃 source 的可变采样 Context 分开管理；同一个可变 Context 不被不同 Actor 或并行采样共同 seek。

ACL 解码在 Source preparation 阶段消费 Program 已计算的 effective time、generation 和 lineage；它不推进第二时钟，也不再次乘 play rate。结果只进入唯一 Source owner 的编译容量工作区及现有 capture 合同，不能增加独立播放器自持的 Pose buffer 或第二 Final Pose。所需 codec scratch 必须进入同一 Source 容量布局，不得绕过布局额外缓存可见 Pose。Barrier 内完成同一 source 的 Root/Scale policy、Virtual Bone、Velocity 和 completion；这些步骤与现有 capture 共用实现，不能仅写 Physical Pose 就宣告 source 完成。Graph Evaluate 内不进行 I/O 或分配。

该边界使加载、native 失败与 Frame 提交分开，也避免在动画 Job 中引入外部阻塞调用。采用官方 native 库保留其现成压缩/解码能力，代价是维护平台构建和 ABI；纯托管实现减少 native 部署，但需要独立承担格式和数值实现。当前沿用原稿的 native 路线。性能收益必须测量，不能由算法名称直接承诺。

### 3. 与唯一 Pose Graph 的接入

在 `CharacterPoseSourceModule` 内新增 ACL backend 注册和 source binding 分支。ACL source 与现有 Animancer source 都实现同一份 `PresentationPoseSourceSample`/`AnimationPoseSourceBinding` 合同；Program 仍只提交 source demand、时间、过渡和权重，Source Module 仍只负责准备和发布 source sample。

ACL 的输出通过现有 source fan-in 和 `CharacterPoseProgramSourcePreparationRuntime` 的工作页进入同一张 Animancer `PlayableGraph`。需要图节点时，只增加同一 Graph 内的 `AnimationScriptPlayable`/Job，并复用现有 output-job 安装、更新、退休和移除流程；不创建第二张 Graph 或绕过 `CharacterPoseFrameCoordinator` 的 Evaluate Barrier。ACL-backed source 一旦被 Projection 标为 ACL，就不再同时驱动其展开 `.anim` source，避免两个可见播放器争夺同一 source identity。

过渡期间允许两个已编译 source 同时存在，但两者各自拥有独立 source identity 和 capture，Transition weight、clock、slot、blend、retirement 和最终 Pose 仍由原 owner 计算。ACL backend 不得写 Physical Transform、IK Goal 或 Final Publication。

### 4. 异步准备、预取和释放

在 Source Module 既有生命周期内增加资源准备服务，并通过正式 IResourceModule/YooAsset 资源包加载，不增加裸文件路径、独立下载器或另一套 Preview 加载流程。表现 composition 显式注入资源服务；资源异步进度不依赖 Frame Seal，避免首份 sample Pending 导致加载永远不能完成。状态严格为 `Pending`、`Ready` 或 `Invalid`：

1. Projection demand 到达后，在表现帧之外异步读取 payload 和 manifest。
2. 校验格式、哈希、Rig、绑定、容量和平台支持范围，并创建解码 Context。
3. 声明的全部质量 tier/chunk、绑定、默认值和 Context 到位，并取得租约后才 Ready；否则 Pending 或带稳定原因的 Invalid。
4. 编译器从完整可达 source 闭包生成预取身份和容量；正式资源配置声明驻留预算。首帧前预取角色所需集合，可见期间固定质量并 pin 资源。LRU 只回收无租约、无在途解码的数据；不能先按低质量解码，再随缓存命中提高质量。预算不足返回明确准备失败，不忽略预算或换播放器。
5. Program 发布匹配 retirement permission 且 Frame Seal 后，Source 才退休自己的 Context 和使用租约。共享 payload/database 必须等待所有 Actor 租约及在途请求结束才可回收；逻辑和物理 source slot 仍等待 release completion 后复用。
6. 取消或 Discard 只撤销本次需求和准备资源，不能误释放其它帧/Actor 已持有的数据。stream-out 必须等待解码结束，streamer 至少与对应 database context 同寿命；异步完成通过 generation 校验后在正式准备边界提升。

Evaluate 只消费 Ready 租约，不等待磁盘或网络。候选 target Pending 时，Program 按现有规则继续采样当前合法 source；Entry Pending 不发布结果，不能把旧 target sample 标成 Ready。预期 Pending 必须经 typed outcome 关闭本帧，不能通过现有“必须 Ready”的异常路径模拟正常加载。回滚部署上一版完整 Projection、资源包与 native artifact，不在 source 内选择备用数据。

### 5. 构建与质量合同

Editor builder 统一产生 manifest、payload、完整 binding 和质量报告。正式 Clip 的 start/stop/loop 与 ACL 的样本覆盖范围分别保存；首版保留端点并关闭隐式 loop 优化，沿用 Program 的 Finite/Cyclic 时间语义，不从压缩样本数反推角色播放时长。零时长或未支持 Clip 类型按现有准入拒绝，不能捏造时长；合法 constant/default 子轨道按声明的 Rig/reference 值解码。

质量报告分别保存原始素材来源可信度与项目重新采样/压缩误差。压缩验证使用正式 Clip 参考，覆盖关键时刻、采样点之间、末帧/循环边界、所有必须轨道、reference/default、Rig 和 binding；转换后的 scalar 数值域及离散性必须匹配其消费者。不得把损坏的原始导出、缺失表情或未解析的绑定作为“完整 ZZZ 参考”。哈希覆盖规范化 manifest 身份、设置及所有 payload；诊断耗时、临时路径和构建时间戳不进入内容 hash。

运行时只验证发布时声明的身份和解码确定性，不提高误差阈值、不删除轨道、不用 IK 或脚步逻辑做补偿。Quality reference 可以在 Editor/离线构建阶段使用，不能成为表现帧的第二播放器。

### 6. 诊断和事实发布

ACL backend 在 Source frame 成功 Seal 后，按现有 diagnostics interest 发布只读事实：资源 identity、格式版本、压缩/驻留/流入字节、stream 状态、Context identity、effective time、采样轨道数、解码耗时、completion 和稳定错误身份。事实带同一 Frame、Projection、Rig、SourceGeneration 和 resource generation。

事实只能复制已提交结果；Pending Context、下一帧数据和调试采样不得混入。诊断关闭时跳过复制和事件，但不得跳过资源准备、解码、过渡、IK 或 Final Publication，也不得反向影响 LRU 或 source 选择。ACL 事实不扩展现有脚步诊断 schema，也不参与脚步、状态、IK、Goal 或 Gameplay 决策。

### 7. 平台和版本固定

首个实现固定当前 Unity 2022.3.62f2c1 与 Windows x64，覆盖 Editor/Mono 和正式 IL2CPP Player；固定 ACL/数值依赖 revision、编译器/浮点选项、C ABI、plugin hash、Importer 平台声明和 manifest schema。其它平台或版本在 Build/准备阶段拒绝。可复现范围首先是同一平台和 artifact 的数值结果；Frame completion 和 source generation 是生命周期身份，不应要求不同帧或不同 Actor 数值相同就复用同一身份，也不承诺跨平台 bit-exact。

### 8. 正式表情素材与参数绑定

输入是明确 source identity 的解码曲线、目标 AnimationClip、Graph 参数声明和当前模型；输出是补齐属性曲线的正式 Clip，以及 Build 发布的 dense scalar/property binding。首批目标仍是 Corin 当前正式 source 闭包，不得为了复用已验证的 MainCity 样本而替换现有战斗/Locomotion source。8 份样本只证明解码能力；缺少正式目标对应数据时继续完成该目标的离线解码，不假借另一 Clip 的曲线。

Graph 继续唯一拥有 ParameterId、类型、单位、默认值和允许来源；参数新增明确的 Control/AnimatedProperty 用途。Presentation Profile 只拥有参数到 RendererBindingId、Mesh identity 与 BlendShape 的映射。显式导入命令可以通过现有 authoring mutation 建立/更新声明与 Clip 曲线，但不能由 selection、Inspector 或 Build 隐式修改作者数据。导入只更新声明的 BlendShape 曲线；骨骼、Foot/Phase 和已有人工修订保持原样，冲突输出到具体曲线供作者决策。

Actor binding 与现有 Rig binding 一起由正式 Factory 装配。运行对象显式提供 Renderer binding，Build 已固定对应 Mesh 内容 hash、BlendShape 名称身份和索引；装配校验实际 Mesh，不按名字搜索场景。单位以原始数据及模型形变记录为准，正式 Profile 映射不提供运行时缩放补偿开关。需要换单位时由一次明确导入转换完成并保存转换身份；不能猜测乘 100 或 clamp。

对于项目声明的某属性，源绑定清单明确表示该片段没有动画该属性时，Build 编译声明默认值的常量通道，采样时它也是当前合法值。原始声明有 Scalar 却未解码、文件丢失或绑定不明仍是错误，不能走该规则。这样全身动作接管时能得到该动作明确的完整属性结果，结束时通过原有过渡回到 base，避免表情残留。

14 条 Motion/Root 进入离线来源记录和用途分类，本次不作为 Renderer 属性、不导入新的 Gameplay 位移，也不覆盖现有 Simulation MotionCurve。项目正式标量运行集合只包含声明的 BlendShape；Foot/Phase 注册曲线仍走其现有编译/消费合同。

### 9. 两种backend共用一条属性运输与混合链

NativeClip 的骨骼仍由当前 Animancer/ManualMixer 采样；其 BlendShape 数值从正式 Clip 在 Build 时编译为不可变标量曲线页，Runtime 按同一 effective time 采样该页。ACL 条目则从官方 Scalar payload 得到对应值。两者都通过 Source Module 的同一 typed 属性结果交给 Program，由 Program 写入现有 Player/Pose Value 参数页；Source 不直接写 Program 页。不得同时再读 AnimationStream 中的同名属性作为第二结果源。

选择编译标量页的业务收益是表情可以在资源准备边界完整校验，并与已有控制曲线保持相同的明确时间、存在性和数值语义；代价是 NativeClip backend 需要该页及属性采样成本。另一个成立的实现是从每个 source 的 AnimationStream 用属性 handle 捕获再进入同一 typed 页，它可以复用 Unity 属性采样，但会把属性完成移到 Barrier 内并增加 handle/曲线默认行为的绑定负担。本次采用前者，不并存两个采样入口。

每个 prepared source 返回同 lineage 的骨骼采样计划与属性采样页。属性各 Clip 的采样时间、权重和归一化必须复用该 source 已生成的 ClipSamplePlan，不能重算 Blend Space 权重。属性值的 source-local 合成复用既有参数混合规则，typed handoff 后只有 Program 的参数页成为后续混合输入。资源缺失时完整 source Pending；骨骼和表情不分别宣布 Ready。

| 阶段 | 骨骼 | 片段自带属性 |
|---|---|---|
| Source-local Clip/Blend Space | 原 NativeClip 或 ACL 采样计划 | 同一 effective sample 与归一化 Clip 权重 |
| State Standard Blend | 原每骨骼 profile/过渡数学 | 原全局 transition weight 的参数插值 |
| BlendStack/Slot | 原 pose/贡献混合 | 原 scalar contribution 与参数规则 |
| Layered Bone Blend/Additive | 原 Mask 和叠加数学 | 原 Base 参数传播，不再乘骨骼 Mask |
| Parameter Resolve | 保留 Base 骨骼结果 | 原 Base/Overlay/Weighted/Max/Min 显式规则 |
| Inertialization/reset | 原历史和残差规则 | 复用已有参数响应与 generation/reset，不加 ACL 滤波 |

上述规则复用已有算法，不重新实现一套表情混合器。新增属性改变的是参数布局、来源和发布目标；所有数组、source/history/default/diagnostics 容量必须由新布局一次分配。不能把 BlendShape 数值误当 `animation.action-weight`、Foot Weight 或 Phase，也不能为图自动追加所谓 ALS 曲线修正节点。

### 10. 唯一最终发布扩展

现有 `CharacterPoseSourceModule` 已在装配时把 Animancer Graph 的最终 output weight 设为 0，图只负责采样/capture，不直接驱动可见骨骼。该行为继续保持，并覆盖所有受管理 BlendShape；不能因为新增属性把 output weight 改回 1，或通过额外 AnimationPlayableOutput 写 Renderer。

Program 在唯一 Output operation 把最终 Pose 与已解析属性通过 typed binding 交给同一个 `CharacterFinalPosePublication`。该 owner 内部增加属性 Pending/Committed 页、模型 binding 和写入记录；它们是骨骼之外的属性数据，不是第二 Final Pose 或第二 Publisher。现有骨骼 Writer 数学保持不变，最终 Apply 先整体预验证，再按固定顺序完成骨骼写入和 Renderer 形变权重写入，最后统一 Seal。数据生成、默认值解析、混合、容量、Mesh/索引和所有 lineage 检查都在首次可见写入之前完成。

根事务、Frame completion 与原四个 owner 不变。属性失效与骨骼失效一样阻断整帧最终发布；Barrier 前 Discard，Barrier 内或之后 Fault。底层 Unity 写入出现不可预期异常时报告 Fault，不通过读回旧骨骼/Renderer 做恢复后继续。Writer 开始之后不再做可能失败的业务计算、资源请求或第二次参数解析。

Preview 继续使用正式 Factory 和同一 Projection/Frame 事务；节点检查、编辑器查看或曲线诊断不调用 Renderer 写入。Reset 只通过既有 owner 的同 generation 重置及正式下一帧默认/采样结果恢复，不能保留另一个表情时钟或独立 LateUpdate writer。

### 11. 与现行spec的差异和实施闭包

本次修改的是 source 资源合同、动画属性参数声明和 Final Publication 的结果/目标范围；现有混合公式、Foot/IK/Goal、Simulation 与骨骼写入数学保持。差异已经在本 change 下的 selection-runtime、presentation-pose-graph 和 runtime-architecture 三份 delta 中明确，新增 scalar-presentation spec 约束同帧结果，不直接改 current specs 来伪装已实施。

旧稿“不修改 Final writer”过宽：保持的是骨骼算法，Publication 必须增加属性输入和 Renderer 写入，否则表情只有数值而没有消费者。旧稿“没有现有 capability 变化”也已移除。新增参数用途/属性布局、resource binding 和 publication identity 要统一提升 schema/ABI，并由明确 Character Build 发布新 Program、Projection、资源包与 native artifact；旧版本拒绝装配，不提供字段缺省迁移或运行时兼容。

`原始资料 → 离线导入 → 正式 Clip/Graph/Profile → Character Build → 资源服务准备 → Source typed 结果 → 原 Program/Pose Plan 混合 → 同一 Final Publication` 是唯一链路。发布验收必须同时覆盖资源闭包、属性数值、模型目标和最终可见结果；离线 8 份样本或编译通过都不能替代完整接入验收。

## Risks / Trade-offs

- [格式或 ABI 漂移] → manifest 固定版本、绑定、Rig、哈希和 native artifact；不接受未声明版本，也不复用 ZZZ 私有 ABI。
- [资源在战斗过渡时仍为 Pending] → 固定质量集合、闭包预取和在用租约；预算须覆盖同时可见 source，代价是峰值驻留提高，不能借降质掩盖不足。
- [native plugin 加载或平台发布失败] → 构建清单校验平台产物和哈希，缺失直接 Invalid；代价是每个平台需单独交付和验证。
- [解码 CPU 高于预期] → Context、scratch 和 Pose page 预分配，诊断记录解码耗时；代价是资源驻留和预取会占用一部分内存。
- [压缩误差影响细小动作] → 发布前按声明误差度量对规范参考采样，超限阻止发布；代价是资源体积可能高于最激进压缩设置。
- [过渡时两个 source 的缓存和释放交错] → 以 source identity、usage、retirement 和 Seal 严格配对，release completion 前不复用槽位；代价是短时峰值内存上升。
- [ACL 资源与展开 `.anim` 重复打包] → Projection 明确 backend，ACL-backed source 禁止同时驱动展开 Clip；代价是迁移期间需要维护清晰的资源清单。
- [把 ACL 误当作脚步修正方案] → source 层禁止访问 Foot/IK/Goal，诊断事实只读；脚步问题仍由原有链路单独验收。
- [形变值正确但模型映射或单位错误] → 在正式导入和 Actor 装配时固定目标 Mesh/属性/单位，缺映射明确失败；不能用缩放或 clamp 掩盖错误。
- [骨骼完成但表情失败] → 同一 source readiness 和整体发布预验证，代价是必需属性缺失也阻断该帧，而不会出现身体换动作、脸停在旧帧的半成品。

## Migration Plan

1. 固定 Corin 当前正式 source 闭包，为目标 Clip 补齐可追溯的 BlendShape authoring 曲线和模型绑定；不替换已经验证的骨骼/Foot/Phase 曲线。以一条现有正式 source 生成完整 manifest/payload 和质量记录。
2. 固定 ACL native revision，落地 C ABI、预分配 Context/Pose page 和资源准备/释放协调器；对格式、版本、哈希和确定性做构建门禁。
3. 在 `CharacterPoseSourceModule` 和 source preparation 中接入 ACL backend，复用现有 Graph、output-job 安装和 Evaluate Barrier；先让 Projection 只对明确标记的 source 使用 ACL，非标记 source 保持现有 backend。
4. 将属性接入既有参数布局、混合和唯一 Publication，编译显式 ACL binding；完成资源预取、骨骼/表情共同过渡、retirement、Seal 和 release completion 的接入验收。确认同一 source 无 `.anim` 备用播放，Graph 无直接可见属性输出。
5. 发布 ACL 只读诊断和性能事实，扩展到批准的 Corin 资源清单；逐步删除已迁移 source 的展开运行包，但保留 authoring Clip 作为可复现构建输入。
6. 回滚时部署上一版 Projection、资源清单和 native artifact；不在运行时增加 fallback，也不在同一 source identity 下混用两个播放器。

## Open Questions

本次没有影响实施范围、owner 或输入输出合同的未决项。其它角色和 Windows x64 之外的平台属于后续扩展，需独立资源清单与 native artifact，不作为本次实施前置。
