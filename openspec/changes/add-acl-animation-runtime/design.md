## Context

本稿正在根据 [ZZZ 复核记录](D:/Unity_Project_1/3C/openspec/changes/add-acl-animation-runtime/research.md) 修订。用户尚未确定本次是否补齐表情，相关消费者和发布合同未定；该范围明确前不进入 apply。以下先收口与该决定无关的资源、格式、时间和生命周期问题。

离线前置状态已更新：实现窗口补齐了 ZZZ Scalar 解码，8 个完整片段的结果已通过限定范围的离线数据复审。正式 3C AnimationClip、Mesh 对应关系、表情混合与统一发布仍未实施；该离线 fork 不替代下文规定的项目官方 ACL 版本和 C ABI。具体数值证据及尚未覆盖的边界见 research.md 第 12 节。

当前表现链已经有明确的唯一 owner：`CharacterPoseSourceModule` 管理物理 source，`CharacterPoseProgramRuntime` 管理状态、过渡、slot 和权重，`CharacterPoseFrameCoordinator` 在唯一的 Animancer `PlayableGraph` 上执行 Evaluate Barrier，后面继续进入 Goal Assembly、FBBIK 和 Final Publication。`CharacterPoseProgramSourcePreparationRuntime` 还会把现有 `AnimationScriptPlayable` 工作插入同一张图。

ZZZ 的已检查 UnityPlayer 含 ACL 家族解码、数据库接入和 LRU Sweep 相关代码。原始资源包含版本 8 的 Transform、版本 100 的 Scalar，以及与外置 bulk 分开的数据库头。当前独立导出漏了数据库头和完整绑定，展开 `.anim` 未完成 Scalar 解码；它们都不能直接证明原始动画已完整还原。项目继续采用正式 AnimationClip 作为 authoring 输入，构建自己的 ACL 资源；不把私有 ABI 或游戏偏移纳入项目依赖。

## Goals / Non-Goals

**Goals:**

- 生成带完整身份和质量合同的 Transform、Scalar、Database/Bulk ACL 资源。
- 以固定 C ABI 连接项目锁定版本的 ACL C++ 解码器，使用预分配 Context 和 Pose writer。
- 在异步准备、流入、使用、retirement、Seal 和 release completion 中保持现有 Source 生命周期语义。
- 让 ACL source 通过现有 `CharacterPoseSourceModule`、唯一 Pose Plan、唯一 Evaluate Barrier 和唯一 PlayableGraph 产生表现 Pose。
- 让 Projection 在编译期决定 source backend，并让 Runtime 只消费 dense identity、格式和资源句柄。
- 以只读事实记录资源状态、压缩体积和解码成本，且关闭诊断不改变正式结果。

**Non-Goals:**

- 不修改 UnityPlayer，不接入 ZZZ 私有 `UnityPlayer.dll`、`AnimeStudio.ACLNative2.dll` 或 Animage 私有 ABI。
- 不替换 Animator、Animancer、Pose Program、Transition、Goal、FBBIK 或 Final writer。
- 不为 ACL source 建立第二张 PlayableGraph、第二个 Player、第二个 Pose buffer、第二个最终写入器或独立 Preview 链。
- 不在 ACL backend 中实现状态机、过渡、脚步锁、IK、Goal、Gameplay 或 Foot Placement 修正。
- 不以最终 Pose 低通、IK 或脚步校正掩盖 ACL 解码误差。
- 不在 ACL 资源未就绪时回退到展开 `.anim`、历史 Pose、默认 Idle 或其它播放器。

## Decisions

### 1. authoring、资源和 Projection 身份分层

`AnimationClip` 继续拥有 authoring 时间曲线。现有 Presentation Profile 的资源绑定明确选择 backend 与压缩配置，Editor builder 从正式 Clip、Rig 和完整 binding 生成不可变资源。manifest 分别声明 Transform、可选 Scalar、Database Header、各 quality tier 的 Bulk，保存存在标记、字节范围、16 字节对齐、各块版本/hash、track-to-bone 或 scalar binding、reference/default 值、源身份、正式 start/stop/loop、采样网格、误差设置和构建身份。无 Scalar 或某 tier 长度为零必须显式声明；不能与必需文件丢失混为一谈。

Projection 编译为唯一 dense 资源表。Action、Direct Clip、Blend Space、MM 与 Preview 都从该表取得 backend 及资源身份；ACL 条目不保存 authoring AnimationClip 的运行时强引用。作者继续在现有 Profile/Source Binding 中编辑和显式 Build，不增加窗口自动构建、OnInspectorGUI 重操作或另一套资源配置。若该字段进入 Agent Document，schema、exporter、reconciler、mutation 与 validator 必须同步。

ACL runtime 不读取 AnimationClip 曲线、AssetDatabase 或作者字符串，也不从 Projection 重新推导绑定。NativeClip backend 继续消费其正式 Clip。项目只支持声明并有消费者的轨道；不能悄悄丢掉 Renderer、Animator、PPtr、离散值或未知脚本曲线后宣称完整。

选择项目自有 manifest 而不是直接保存一段无身份的 `.acl` 字节，是为了在 Rig、轨道布局或压缩版本错误时返回 `Invalid`，而不是把错误数据解释成看似合法的 Pose。选择从正式 Clip 构建而不是把现有展开 YAML 作为运行输入，是为了避免把巨大的展开数据继续带入内存；选择项目锁定版本而不是复制 ZZZ 数据格式，是为了不把不可验证的私有 ABI 变成项目依赖。

### 2. native 解码边界

项目固定官方 ACL v2.1.0 的 commit `414689d5cff4286a7898487a46dc5e48005d38da`，同时锁定其数值依赖、编译选项和项目 C ABI。项目 payload 使用该版本正式格式 10，不接受 ZZZ 私有 Scalar 100 或重写版本号。该版本数据库能力用于 Transform；Scalar 使用正式独立数据流。Managed 侧只持有 identity、opaque handle、预分配输出 binding 和 release token，不能依赖 C++ 对象布局或跨 DLL 释放内存。

C ABI 明确 struct size/version、固定宽度字段、calling convention、16 字节 aligned allocation、输出容量、错误码和唯一销毁入口；C++ 异常不越过边界。Context、scratch 和现有 source 页来自准备阶段及编译容量，帧内不进行托管或 native 堆分配。共享 payload/database 与每个活跃 source 的可变采样 Context 分开管理；同一个可变 Context 不被不同 Actor 或并行采样共同 seek。

ACL 解码在 Source preparation 阶段消费 Program 已计算的 effective time、generation 和 lineage；它不推进第二时钟，也不再次乘 play rate。结果只进入现有 capture 合同的预分配 source 页，不能增加平行 Pose 真相。Barrier 内完成同一 source 的 Root/Scale policy、Virtual Bone、Velocity 和 completion；这些步骤必须与现有 capture 共用实现，不能仅写 Physical Pose 就宣告整个 source 完成。不得在 Graph Evaluate 中进行 I/O 或分配。

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

## Risks / Trade-offs

- [格式或 ABI 漂移] → manifest 固定版本、绑定、Rig、哈希和 native artifact；不接受未声明版本，也不复用 ZZZ 私有 ABI。
- [资源在战斗过渡时仍为 Pending] → 固定质量集合、闭包预取和在用租约；预算须覆盖同时可见 source，代价是峰值驻留提高，不能借降质掩盖不足。
- [native plugin 加载或平台发布失败] → 构建清单校验平台产物和哈希，缺失直接 Invalid；代价是每个平台需单独交付和验证。
- [解码 CPU 高于预期] → Context、scratch 和 Pose page 预分配，诊断记录解码耗时；代价是资源驻留和预取会占用一部分内存。
- [压缩误差影响细小动作] → 发布前按声明误差度量对规范参考采样，超限阻止发布；代价是资源体积可能高于最激进压缩设置。
- [过渡时两个 source 的缓存和释放交错] → 以 source identity、usage、retirement 和 Seal 严格配对，release completion 前不复用槽位；代价是短时峰值内存上升。
- [ACL 资源与展开 `.anim` 重复打包] → Projection 明确 backend，ACL-backed source 禁止同时驱动展开 Clip；代价是迁移期间需要维护清晰的资源清单。
- [把 ACL 误当作脚步修正方案] → source 层禁止访问 Foot/IK/Goal，诊断事实只读；脚步问题仍由原有链路单独验收。

## Migration Plan

1. 先为一条正式 Corin `AnimationClip` 生成 ACL manifest/payload，完成离线身份、绑定、采样和误差校验；此阶段不改变现有 Projection。
2. 固定 ACL native revision，落地 C ABI、预分配 Context/Pose page 和资源准备/释放协调器；对格式、版本、哈希和确定性做构建门禁。
3. 在 `CharacterPoseSourceModule` 和 source preparation 中接入 ACL backend，复用现有 Graph、output-job 安装和 Evaluate Barrier；先让 Projection 只对明确标记的 source 使用 ACL，非标记 source 保持现有 backend。
4. 为目标资源编译显式 ACL binding，执行资源预取、过渡、retirement、Seal 和 release completion 的端到端验收；确认同一 source 不再同时驱动展开 `.anim`。
5. 发布 ACL 只读诊断和性能事实，扩展到批准的 Corin 资源清单；逐步删除已迁移 source 的展开运行包，但保留 authoring Clip 作为可复现构建输入。
6. 回滚时部署上一版 Projection、资源清单和 native artifact；不在运行时增加 fallback，也不在同一 source identity 下混用两个播放器。

## Open Questions

- 在表情范围确定后，首批按 Corin 已批准的 source 清单发布；其它角色何时迁移只影响资源清单和发布批次。
- Windows x64 之外的平台何时加入；需以各平台独立 native artifact 和确定性验收为准，不改变 `Pending/Ready/Invalid` 语义。
