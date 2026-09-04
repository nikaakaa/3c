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

模块边界、接口、数据归属和迁移步骤统一在本文件定义，由设计窗口定稿。实现窗口按确定方案执行，发现与现有正确行为的具体冲突时回报证据；不自行新增 owner、旁路或通用框架。模块拆分与行为修复在本次实现内完成。

### 1. authoring、资源和 Projection 身份分层

`AnimationClip` 继续拥有 authoring 时间曲线。现有 Presentation Profile 的资源绑定明确选择 backend 与压缩配置，Editor builder 从正式 Clip、Rig 和完整 binding 生成不可变资源。manifest 分别声明 Transform、可选 Scalar、Database Header、各 quality tier 的 Bulk，保存存在标记、字节范围、16 字节对齐、各块版本/hash、track-to-bone 或 scalar binding、reference/default 值、源身份、正式 start/stop/loop、采样网格、误差设置和构建身份。无 Scalar 或某 tier 长度为零必须显式声明；不能与必需文件丢失混为一谈。

Projection 编译为唯一 dense 资源表。Action、Direct Clip、Blend Space、MM 与 Preview 都从该表取得 backend 及资源身份；ACL 条目不保存 authoring AnimationClip 的运行时强引用。作者继续在现有 Profile/Source Binding 中编辑和显式 Build，不增加窗口自动构建、OnInspectorGUI 重操作或另一套资源配置。若该字段进入 Agent Document，schema、exporter、reconciler、mutation 与 validator 必须同步。

ACL runtime 不读取 AnimationClip 曲线、AssetDatabase 或作者字符串，也不从 Projection 重新推导绑定。NativeClip backend 继续消费其正式 Clip。项目只支持声明并有消费者的轨道；不能悄悄丢掉 Renderer、Animator、PPtr、离散值或未知脚本曲线后宣称完整。

选择项目自有 manifest 而不是直接保存一段无身份的 `.acl` 字节，是为了在 Rig、轨道布局或压缩版本错误时返回 `Invalid`，而不是把错误数据解释成看似合法的 Pose。选择从正式 Clip 构建而不是把现有展开 YAML 作为运行输入，是为了避免把巨大的展开数据继续带入内存；选择项目锁定版本而不是复制 ZZZ 数据格式，是为了不把不可验证的私有 ABI 变成项目依赖。

NativeClip/ACL 的选择由 Profile 的唯一资源绑定决定。一个逻辑 source 的 Clip catalog 必须使用同一个 backend；不同 backend 之间的过渡由原 Program 的不同 source 完成。若一个 catalog 混合声明，Build 定位该 catalog 并失败。删除 Direct Clip Binding 中与 Profile 重复的 backend/ACL 选择字段，编译时统一解析。

### 2. native 解码边界

项目固定官方 ACL v2.1.0 的 commit `414689d5cff4286a7898487a46dc5e48005d38da`，同时锁定其数值依赖、编译选项和项目 C ABI。项目 payload 使用该版本正式格式 10，不接受 ZZZ 私有 Scalar 100 或重写版本号。该版本数据库能力用于 Transform；Scalar 使用正式独立数据流。Managed 侧只持有 identity、opaque handle、预分配输出 binding 和 release token，不能依赖 C++ 对象布局或跨 DLL 释放内存。

C ABI 明确 struct size/version、固定宽度字段、calling convention、16 字节 aligned allocation、输出容量、错误码和唯一销毁入口；C++ 异常不越过边界。Context、scratch 和现有 source 页来自准备阶段及编译容量，帧内不进行托管或 native 堆分配。共享 payload/database 与每个活跃 source 的可变采样 Context 分开管理；同一个可变 Context 不被不同 Actor 或并行采样共同 seek。

ACL 解码在 Source preparation 阶段消费 Program 已计算的 effective time、generation 和 lineage；它不推进第二时钟，也不再次乘 play rate。结果只进入唯一 Source owner 的编译容量工作区及现有 capture 合同，不能增加独立播放器自持的 Pose buffer 或第二 Final Pose。所需 codec scratch 必须进入同一 Source 容量布局，不得绕过布局额外缓存可见 Pose。Barrier 内完成同一 source 的 Root/Scale policy、Virtual Bone、Velocity 和 completion；这些步骤与现有 capture 共用实现，不能仅写 Physical Pose 就宣告 source 完成。Graph Evaluate 内不进行 I/O 或分配。

该边界使加载、native 失败与 Frame 提交分开，也避免在动画 Job 中引入外部阻塞调用。采用官方 native 库保留其现成压缩/解码能力，代价是维护平台构建和 ABI；纯托管实现减少 native 部署，但需要独立承担格式和数值实现。当前沿用原稿的 native 路线。性能收益必须测量，不能由算法名称直接承诺。

ACL 只编码 PhysicalBoneCount 条实体骨骼 QVV。虚拟骨骼不生成压缩轨道，由原 AnimationSourcePoseCaptureJob 在混合后派生；最终 source 页仍覆盖完整 PoseBoneCount。

#### Native 文件和 ABI

`Tools/Native/ACL/` 拆为：

- `acl_runtime.h`：唯一导出 C ABI 和定宽 DTO。
- `acl_compression.cpp`：组压缩、官方 build_database/split_database_bulk_data、构建结果释放。
- `acl_resources.cpp`：整组 aligned payload/database/streamer 的准备、验证与生命周期。
- `acl_decoder.cpp`：独占 Decoder 的创建、绑定、采样、解绑和销毁。
- `acl_native_internal.h`：以上实现共享的私有资源/Decoder 类型与 RAII 内存，不导出 C++ 布局。

删除单文件草稿 `acl_runtime.cpp`，不保留两套导出实现。Native C ABI 固定为 2：

- `acl_project_get_abi` 返回 ABI、结构大小与格式能力。
- `acl_project_build_group` / `acl_project_build_result_release` 只供 Editor 构建。
- `acl_project_group_create` / `acl_project_group_release` 持有完整的只读共享数据。
- `acl_project_decoder_create` 创建不绑定资源的预分配 Decoder；`bind` 绑定 group/Clip；`sample` 消费 effective time 和输出 span；`unbind` 解除资源引用；`destroy` 销毁私有状态。

group 和 Decoder 必须是不同 handle。group 不含任何 source 的采样时间。Decoder bind/sample 不进行 native 堆分配；池容量由编译布局决定。每个 block 在读取内部 header 前验证提供长度，再验证版本、类型、内部长度/hash、metadata/default 与输出覆盖；Scalar 只接受正式 float1。

数据库质量集合在 group Ready 前形成；底层 streamer 只访问已加载并保留的数据。并行 Decoder 可以读同一组，任何 stream-out/group 释放必须等待全部引用完成。Managed 与 native 的分配/销毁入口配对，native 异常不越过 C ABI。

### 3. 与唯一 Pose Graph 的接入

在 `CharacterPoseSourceModule` 内新增 ACL backend 注册和 source binding 分支。ACL source 与现有 Animancer source 都实现同一份 `PresentationPoseSourceSample`/`AnimationPoseSourceBinding` 合同；Program 仍只提交 source demand、时间、过渡和权重，Source Module 仍只负责准备和发布 source sample。

ACL 的输出通过现有 source fan-in 和 `CharacterPoseProgramSourcePreparationRuntime` 的工作页进入同一张 Animancer `PlayableGraph`。需要图节点时，只增加同一 Graph 内的 `AnimationScriptPlayable`/Job，并复用现有 output-job 安装、更新、退休和移除流程；不创建第二张 Graph 或绕过 `CharacterPoseFrameCoordinator` 的 Evaluate Barrier。ACL-backed source 一旦被 Projection 标为 ACL，就不再同时驱动其展开 `.anim` source，避免两个可见播放器争夺同一 source identity。

过渡期间允许两个已编译 source 同时存在，但两者各自拥有独立 source identity 和 capture，Transition weight、clock、slot、blend、retirement 和最终 Pose 仍由原 owner 计算。ACL backend 不得写 Physical Transform、IK Goal 或 Final Publication。

ACL 每条活动 Clip 使用图内输入 Job 写 AnimationStream，再由同一张图中的 AnimationMixerPlayable 混合，最后进入原 capture。删除 ACL 草稿的逐骨骼 C# Lerp 混合。NativeClip 保留原 ManualMixer；两者共用一次归一化的 ClipSampleBatch。

#### Runtime 模块与合同

路径前缀为 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/`。

| 最终文件/类型 | 输入 | 输出 | 数据归属 |
| --- | --- | --- | --- |
| `Animation/Contracts/Sources/AnimationPoseSamplingContracts.cs` | 编译资源、Frame、source identity | backend 枚举、采样批次、准备结果、释放 token | 共用合同；从 Animancer 实现文件中移出这些类型 |
| `Animation/Contracts/Sources/IAnimationPoseSamplingBackend.cs` | 物理 source identity、归一化采样批次、capture binding | typed 准备结果与 capture playable | 仅统一 backend 边界，不含 AnimancerComponent 类型 |
| `Animation/Sampling/CharacterClipSampleBatch.cs` | 现有 ClipSamplePlan 集合 | 有效时间及一次归一化后的权重 | Source 的预分配批次，不推进时钟 |
| `Animation/Sampling/CharacterAnimationScalarMixer.cs` | 各 Clip 完整标量值/常量、相同归一化权重 | Source 属性页 | 纯 source 内加权；只清理 AnimatedProperty 目标，不覆盖 Control/Foot 参数 |
| `Animation/ACL/CharacterAclPoseSamplingBackend.cs` | Registry 给出的物理槽位、Frame 操作、资源只读入口 | Source 准备/采样结果 | 小型协调入口，不维护第二套 source 身份 |
| `Animation/ACL/CharacterAclSourcePool.cs` | 编译容量、物理槽位与 generation | 对应 `CharacterAclSourceInstance` | 固定槽位数组；不能自行找空槽、分配 source ID 或决定退休 |
| `Animation/ACL/CharacterAclSourceInstance.cs` | 独占 Decoder、共享资源 lease、采样批次 | 本次 codec scratch 和标量 Clip 页 | 只持有这一槽位的解码状态，不选动作，不保存最终 Pose |
| `Animation/ACL/CharacterAclSourceGraph.cs` | 现有 PlayableGraph、编译容量、codec 页 | Clip 输入 Job → Unity Mixer → 原 capture | 图节点的装配和资源释放；不控制时间，不直接发布可见对象 |
| `Animation/ACL/CharacterAclClipPoseJob.cs` | 一条 Clip 的实体骨骼解码页与 dense 绑定 | 图内 AnimationStream | 单纯写临时流；不是 authoring Writer 节点或 Final Writer |
| `Animation/ACL/CharacterAclFrameJournal.cs` | Source 发出的本帧操作和物理槽位 | 本 backend 待应用/丢弃的操作 | 有界 mutation 记录，不拥有独立 source registry、generation 分配器或根事务 |
| `Animation/ACL/CharacterAclNativeBridge.cs` | 固定宽度 C ABI 输入输出 | 原生错误码与 opaque handles | 只负责 interop；不接收 ScriptableObject，不加载资产、不验 Profile、不做缓存 |
| `Animation/ACL/CharacterAclDecoder.cs` | 已分配 Decoder 槽、只读组 lease、Clip index | 指定时间的实体骨骼/Scalar | 独占可变 seek 状态，绑定和采样不分配堆内存 |
| `Animation/Resources/CharacterAnimationResourceScope.cs` | 正式 loader、资源预算、Actor 注册闭包 | 共享资源只读入口、准备推进和收口 | 一个表现会话持有一个实例，不由每个 Actor 创建 |
| `Animation/Resources/CharacterAclResourceStore.cs` | 已注册内容身份、准备请求、lease 操作 | Pending/Ready/Invalid 和共享数据 lease | 唯一共享数据缓存、预算与 LRU；不存可变 Decoder |
| `Animation/Resources/CharacterAclResourcePreparation.cs` | 一组块地址和 manifest | 完整且校验过的 native group | 一个加载操作的有限状态与已加载块，不另建全局缓存 |
| `Unity/Resources/YooAssetCharacterAnimationAssetLoader.cs` | `IResourceModule`、明确包名和块地址 | 项目定义的加载 ticket / bytes | 唯一正式资源适配器，负责 YooAsset handle 释放 |

仅保留两类必要接口：既有 backend 多态边界，以及资源加载边界 `ICharacterAnimationAssetLoader`。纯采样、质量评价、时间布局等使用直接类型/纯函数，不为每个类再造接口。

`ICharacterAnimationAssetLoader` 放入 `Animation/Contracts/Resources/ICharacterAnimationAssetLoader.cs`；资源 address、ticket、readiness 和 lease 放同目录的 `CharacterAnimationResourceContracts.cs`。资源地址是 Build 生成的正式包地址，允许传给资源系统；它不是运行时骨骼/参数名称查找。

`CharacterPoseSourceScalarReadView` 及其 identity 放入 `Animation/Contracts/Sources/CharacterPoseSourceScalarContracts.cs`，只开放带有效 lease 的只读访问；数组、可写 slice 和 owner 内部索引不跨模块公开。

`CharacterAnimationSamplingBackendKind` 替换草稿 `CharacterAclAnimationBackendKind`。它是 NativeClip 与 ACL 的共用枚举，不应以 ACL 命名整个抽象。

Projection 没有 ACL 资源时，ACL 池、Decoder 和图节点容量为零，不加载 ACL native DLL；这属于显式 NativeClip 配置。声明 ACL 的资源缺 DLL 或数据仍按原规则失败，不切换 backend。

#### Source 登记与准备状态

在 `PhysicalPoseSourceRegistry` 原有 pending/committed 记录中加入 BackendKind 和编译 ResourceCatalogIndex；`AnimationPhysicalSourceIdentity` 仍只有原 index/generation。`CharacterPoseSourceCommittedIdentity` 和释放 token 携带必要 backend 信息。Registry 的原提交、丢弃、释放路径同时处理新字段，不在外层镜像一份字典。

Source Module 持有固定的 NativeClip/ACL backend 实例，按 Registry 记录选择调用；其原 Frame 生命周期统一通知两个 backend。删除 Router 整个类型。`AnimancerPoseSamplingBackend` 仅做共用合同迁移、接口接线和采样批次输入调整，保留原 ClipState/ManualMixer、Root/Scale、capture 等行为。

准备状态在执行动画选择前确定：scope 先推进准备；Source Module 按已编译 catalog 汇总为预分配的 `CharacterPoseSourceReadinessView`；Program 把这个只读 view 接入原有 source readiness 判断。Source 只回答资源是否完整，不决定状态、赢家或过渡。候选 target Pending 继续使用当前合法 source，Entry Pending 不发布 Final Pose，Invalid 保留稳定原因。之后仅对 Ready demand 物化物理 source 和 acquire lease；这样正常等待不必进入会抛异常的 Context 构造器，也不会每次 Discard 都重启同一资源加载。

Readiness view 携带当前 Source lease 与资源 generation；资源回收只在事务外推进，Frame 内归还 lease 仅排队，不使同一批 Actor 使用的快照中途失效。Prepare 与 Barrier 对照同一代数据，不能把缓存下一代内容换进正在采样的 source。

### 4. 异步准备、预取和释放

资源准备使用正式 IResourceModule/YooAsset，由表现会话在 Actor BeginFrame 前推进，进度不依赖 Frame Seal。共享缓存只拥有不可变数据和已准备的数据库，可变 Decoder 按 Actor/source 槽位独占。

#### 资源接口

`ICharacterAnimationAssetLoader` 只提供 `BeginLoad(address)`、`Poll(ticket)`、`Release(ticket)`；返回项目定义 ticket/status，不把 YooAsset AssetHandle 传给 Source。BeginLoad 和读取 bytes 只在准备阶段执行。

Source 使用资源入口的 `Request(resourceIndex)`、`GetReadiness(resourceIndex)`、`TryAcquire(resourceIndex, out lease)` 和 `Release(lease)`。这些操作只针对 Actor 注册时已经编译的闭包，Frame 内不创建新字典项，不启动文件 I/O，不算 hash，不分配 lease 对象。lease 是带 index/generation 的值，不是每次 new 一个 IDisposable 类。

`AdvancePreparation()` 只由 scope 的装配宿主在所有 Actor `BeginFrame` 之前调用，接收资源完成事件、校验、准备 native 数据并执行安全回收。加载事件只能更新准备操作，不能写 Program/Source/Final 的当前帧数据。即使所有 Actor 的表现帧都 Pending 或 Discard，准备仍继续推进。

正式 `CharacterAnimationResourceSettings` 只声明共享驻留预算；初始 128 MiB。实际包名和初始化选项由现有资源初始化配置提供，不在 Actor 或 Router 中硬编码预算/默认包。编译布局另行完整记录 Actor 的 Decoder、codec scratch、标量页和图节点容量，并进入已有容量诊断。

#### 会话装配与 Preview

- 在现有 `CharacterPoseWorkerPresentationSession` 增加 scope 的持有和公开只读入口；其 `PresentationFrame` 在第一个 target.BeginFrame 前推进资源准备，已有 worker 调度算法不改。
- Gameplay Lab 的正式 Bootstrap 在实例化 RuntimeRoot 前，按已声明设置初始化该 scope；Source、Decoder 和 Actor 内不得 `ModuleSystem.GetModule`。仅 Unity 装配/资源适配层取得已有 `IResourceModule`。
- `CharacterPresentationRuntimeFactory` 从已配置会话取得资源入口，显式传给 `CharacterAnimationPresentationRuntime`、CompositionFactory、Source Module。删除 Router 构造内部创建资源服务的代码。
- Preview 的现有 `CharacterPoseWorkerPreviewAdapter` 持有同一实现的独立 scope，在 Preview BeginFrame 前推进；资源依然使用正式 `IResourceModule.InitPackage` / `LoadAssetAsyncHandle<TextAsset>` 和已有 EditorSimulateMode 初始化流程。已初始化包必须匹配配置，不能切换全局包或改用 AssetDatabase fallback。
- 初始化准备中的 Preview 返回明确 Pending，初始化失败返回 Invalid；不要求以先播放游戏作为隐含前置。资源准备不进入 Inspector 重操作，不增加表情 Update/LateUpdate。
- `ThirdPersonClient.Runtime` 的资源适配层需要显式引用 `TEngine.Runtime` 和 `YooAsset`；依赖只允许出现在 `Unity/Resources` 与装配代码中，backend、Program、worker kernel 不引入这些类型。无需另建一套插件/模块注册框架。

Scope 关闭时先拒绝新请求并取消准备；已持有的 lease 仍可归还。现有 Actor/Source 清理完成后才释放其 Decoder 引用，最后一个引用及在途操作归零才实际销毁 group。迟到的加载完成回调在 Closing 状态只释放本次句柄，不再发布 Ready；Scope 不能为满足 Clear 而强行释放其它 Actor 仍使用的数据。

#### 生命周期顺序

| 阶段 | 必须发生的操作 |
| --- | --- |
| Actor 注册，事务外 | 注册完整编译资源闭包；分配 Decoder 池、codec 页、标量页和 ACL 图节点；提交闭包预取请求 |
| 准备推进 | 加载/校验整组数据，绑定正式数据库全部需要的 tier；所有块和身份完整后原子置 Ready |
| source demand | 未 Ready 则发布 Pending/Invalid outcome；不登记可采样 source，不创建备用 Clip，不因正常等待抛异常 |
| Ready 的 Prepare | 由 Registry 分配物理 identity；按该槽位绑定独占 Decoder 和共享 lease，写有界 journal；规范化 Clip 权重一次 |
| Evaluate Barrier | 解码到本次 codec 页，采样/混合标量；Clip Job 写图内流，Unity Mixer 混合，原 capture 完成 Root/Scale、虚拟骨骼、velocity、completion |
| Program 消费 | 经同一 Source completion 读取 typed 标量 view，复制到 Program 自己的 Player/Pose Value 页，再执行原混合规则 |
| Barrier 前 Discard | 撤销 pending 绑定与 journal、归还本帧 lease、清空可见性；Registry 撤销相同 identity；共享已加载数据可留在缓存 |
| Seal 后退休 | 消费原退休许可；确认所有相关工作完成；解绑本 source 的 Decoder、归还 lease、清空对应图输入；最后发布 release completion，随后才允许复用槽位 |
| Fault / Actor 销毁 | 沿现有 worker 完成/终止边界收口，先释放本 Actor Decoder，再释放资源关系；不使其它 Actor 的共享数据失效 |

资源 Store 以内容身份作键，不能使用 `UnityEngine.Object.GetInstanceID()`。每个 group entry 只含不可变数据、generation、准备状态、使用计数和 LRU 信息。只有无活跃 lease、无在途准备/解码引用的组可淘汰。预算不足返回明确结果，不能换低质量 tier 或同步等待。

预取请求不等于永久使用 lease；活动 Decoder、可见 source 和保留中的 source 必须持有稳定 lease。Actor 的预取闭包不能被当成第二份动作选择树。

### 5. 构建与质量合同

路径前缀为 `3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterSimulation/Compilation/Animation/`。Sources 下的能力供 NativeClip 和 ACL 共用；ACL 下只放 codec 专属能力。

| 最终文件/类型 | 输入 | 输出 | 唯一职责 |
| --- | --- | --- | --- |
| `Sources/CharacterAnimationBuildCatalogCompiler.cs` | 现有 Action、Direct Clip、Blend Space、MM 编译器产生的引用结果，Profile、Rig、参数布局 | 排序确定的 source/Clip 资源闭包、backend 和数据库分组 | 合并现有编译结果，不另写一套 Graph 遍历和动作选择 |
| `Sources/CharacterAnimationAuthoringReader.cs` | 一条正式 Clip、已解析参数布局、Rig 绑定、属性绑定、来源证据 | `CharacterAnimationAuthoringSource` | 一次解析准确路径、轨道存在性、reference/default 和单位；不采样整段、不压缩、不写文件 |
| `Sources/CharacterAnimationSourceSampler.cs` | AuthoringSource、SampleGrid | `CharacterAnimationSampleSet`，以及 NativeClip 标量曲线页 | 唯一重采样与样本布局；不访问 AssetDatabase，不解析 Profile，不发布资源 |
| `Sources/CharacterAnimationSamplingQualityEvaluator.cs` | AuthoringSource、SampleSet、统一验证时刻和误差设置 | 原曲线到重采样的质量结果 | NativeClip 属性页与 ACL 共用这一门禁；不依赖 native DLL，不解码压缩数据 |
| `Sources/CharacterAnimationBuildContracts.cs` | 构造参数 | 上述只读构建 DTO | 保存本次构建的数据，不维护全局缓存或业务状态 |
| `ACL/CharacterAclAnimationResourceBuilder.cs` | 已解析的构建闭包、正式压缩设置、构建上下文 | 完整 `CharacterAclAnimationBuildArtifactSet` 或构建错误 | 仅按顺序调用 reader、sampler、compiler、evaluator；不包含轨道遍历、误差循环或文件写入 |
| `ACL/CharacterAclAnimationCompiler.cs` | 同一组 SampleSet、压缩设置、native ABI | 压缩 Transform、独立 Scalar、数据库头、分 tier bulk | 调用唯一 C ABI，整理官方数据库构建结果；不读取作者对象，不判定质量是否合格 |
| `ACL/CharacterAclAnimationQualityEvaluator.cs` | AuthoringSource、SampleSet、最终压缩块、误差设置和来源证据 | 逐轨道与整组质量报告 | 比较原曲线、重采样、解码结果；不修改数据、阈值或作者配置 |
| `ACL/CharacterAclAnimationArtifactPublisher.cs` | 通过全部门禁的 ArtifactSet、正式构建发布上下文 | 带地址、hash、平台信息的资源产物 | 只负责暂存、导入和发布，不压缩、不决定 backend、不修改作者曲线 |
| `ACL/CharacterAclAnimationBuildContracts.cs` | 构造参数 | 压缩产物及质量报告 DTO | 保留 codec 产物合同，不夹带采样方法 |

显式属性导入单独放在既有动画 authoring 工具目录的 `CharacterAnimationPropertyImporter.cs`。它只按完整 CAB/PathId、目标 Clip 和属性映射写入声明的 BlendShape 曲线，输出来源、单位、时间和修改差异证据。骨骼、Foot/Phase 和已有人工属性冲突不得覆盖。导入不触发另一套运行时资源发布。

#### 正式构建入口

- 继续由现有 `CharacterSimulationProgramBuildService.Build` 所属的 Character Build 驱动。
- `CharacterPresentationProjectionCompiler.Compile` 使用现有 source 编译结果组成 BuildCatalog；资源编译结果通过 `CharacterPresentationProjectionCompileResult.AnimationArtifacts` 向现有 Build 发布阶段交付。
- Projection 编译只生成内存产物和诊断。ArtifactPublisher 的发布由正式 Build 的发布阶段执行；不能在 `CompileCore`、Inspector、validator 或只读检查里落盘。
- 扩展现有编译 request/result 的显式字段传递构建上下文，不使用静态临时字典、全局“当前 Build”、修改 Profile 后再读回等通信办法。
- 现有公开 Character Build 调用链中的发布方若位于生成源码或包内，应修改对应源码/生成源以接收 AnimationArtifacts，不新建替代 Orchestrator。实现只需完成这个接线；不能自行更改发布 owner。
- 删除草稿 `BuildSelected` 独立发布菜单。原 authoring 按钮和 Agent 命令调用正式 Character Build。资源重新编译不自动改变 Profile 的 backend 选择。
- 只有全部动画资源、Program 和 Projection 合格才激活新发布清单。失败保留上一版完整发布，清理本次暂存；运行时不挑旧块补新产物。

#### 时间布局

`CharacterAnimationSampleGrid` 放在 `Animation/Contracts/Sources/`，保存 RequestedSampleRate、ActualSampleRate、SampleCount、FormalStart、FormalStop、CompressedStart、CompressedStop 和 Looping。

本次 Runtime 时间继续使用正式 Unity AnimationClip 自身的零起点时间；原始 ZZZ take 的起止偏移留在导入证据，不能再加到 Program 已计算的 ClipTime 上。既有 Timeline ClipInTime、play rate、连续时间和循环计算保持其原 owner。

对正式时长 D 和请求频率 F，固定算法为：

`N = max(1, ceil(D × F)); SampleCount = N + 1; ActualSampleRate = N / D; SampleTime(i) = D × i / N`

Corin 的正式初始压缩配置请求 60 Hz，写入 Profile 引用的压缩设置，不在 Builder 中写死 30。该数值是本方案选择的质量/体积起点；超限资源报告具体轨道与时刻，不自动改变频率或误差阈值。

最后一点明确为 D。Transform、Scalar、NativeClip 标量页、native 压缩输入和质量评价使用同一 SampleGrid。Native 接收 ActualSampleRate；metadata 不再把 RequestedSampleRate 冒充实际频率。运行时只消费已确定的 effective time，不再乘速率或重复换算。

压缩覆盖时间按实际 ACL 输出记录并核对，不能用压缩样本数重新定义动作时长。关闭 ACL 自动删除循环末帧的优化，保留正式端点。

#### 数据库和质量门禁

数据库分组固定为同一 Rig revision、参数布局、正式压缩设置和 native 平台版本下的 ACL Clip 闭包。组内按稳定 Clip 资源身份排序；改动一条 Clip 会重建对应组，产物整组发布。组是共享驻留/LRU 的单位，不在运行中对同一可见组变更质量。

Corin 的 ACL Transform 使用官方数据库。采用已锁定 ACL 的数据库初始设置：medium 比例 0、low 比例 0.5、最大 chunk 1 MiB；这些值进入正式压缩设置和 hash。导出的实际空 tier 合法，存在的全部 tier/chunk 必须准备完整。Scalar 使用独立官方流，不传入 Transform database。

正式误差设置分别声明位置（米）、旋转（度）、缩放（无单位）和 Scalar（Graph 声明单位），沿用已写出的 0.01、0.5、0.01、0.00001 初始值。ACL 内部采用官方 QVV 误差度量，ShellDistance 明确为正值，初始采用该锁定版本的 3.0 米；禁止草稿中的 0。最终发布仍由项目逐项误差门禁决定，不能自动放宽任何阈值。

Evaluator 分别输出原 Clip 到 SampleSet、SampleSet 到 ACL、原 Clip 到 ACL 的误差。验证时间包含正式端点、循环边界、全部相关原始关键帧和采样网格；合并排序后每个相邻区间再取四等分检查点。报告记录实际覆盖策略，不把有限采样写成无限时间范围的数学证明。

原 Clip 到 SampleSet 的实现归通用 `CharacterAnimationSamplingQualityEvaluator`；ACL Evaluator 复用它的验证时刻和第一段结果，再增加解码比较。NativeClip 只编译属性页时也必须执行通用门禁，不能因未选择 ACL 而跳过表情曲线失真检查。

以时间为外层循环：每个时间解码一次全部 Transform/Scalar，再逐轨道比较，避免每个骨骼都重复解码整段骨架。旋转必须参与拒绝发布。ZZZ 还原证据是独立必需输入；Corin 没有证据时保持失败，不以 `requireZzzRestoration=false` 发布“完整还原”。

ArtifactSet 包含每 Clip 的 Transform/Scalar、每组数据库头和分 tier bulk、完整 manifest、质量报告、构建依赖与 native 平台/hash。`CompressedScalarTrackCount` 与 `PropertyBindingCount` 分开，零压缩 Scalar 但存在默认常量属性合法；`PhysicalBoneCount` 与包含虚拟骨骼的 `PoseBoneCount` 分开。

#### 构建数据合同

| 合同 | 必需字段 | 禁止承担的职责 |
| --- | --- | --- |
| `CharacterAnimationBuildInput` | Definition 身份、Profile、已装配 SourceRigBinding、Rig、唯一 ParameterLayout、现有 source 编译引用、来源证据索引、native 目标版本 | 不从静态变量找当前角色，不自己搜模型 |
| `CharacterAnimationBuildCatalog` | Clip GUID/LocalFileId、原 source 引用位置、dense ResourceIndex、BackendKind、GroupIndex、Rig/ParameterLayout identity | 不保存运行时 Player、时钟或选择结果 |
| `CharacterAnimationAuthoringSource` | Clip 内容身份、精确曲线 binding、实体骨骼 reference、属性 presence/default/unit、来源证明、正式播放范围 | 不携带压缩结果，不修改 Clip |
| `CharacterAnimationSampleSet` | SampleGrid、frame-major QVV（每实体骨骼 10 个 float）、Scalar 样本、压缩轨道到属性参数的映射、默认常量记录 | 不决定数据是否发布，不访问磁盘 |
| `CharacterAclAnimationBuildArtifactSet` | 全部组/Clip 块、清单、质量报告、平台/native 信息与 hash、输出地址规划 | 不保存运行时可变 Context，不修改 Profile backend |

`CharacterPresentationProjectionCompileRequest` 显式携带 AnimationBuildInput；CompileResult 显式携带 AnimationArtifacts。发布上下文决定暂存位置和激活时机；不能通过修改作者对象传递这些结果。若原 Build 接口需要新增参数，按上述合同贯穿传递，不增加另一个 Build 入口。

哈希覆盖规范化 manifest 身份、设置和所有 payload；诊断耗时、临时路径和构建时间戳不进入内容 hash。Runtime 不提高误差阈值、不删除轨道、不用 IK 补偿，Editor 质量参考不成为第二播放器。

### 6. 诊断和事实发布

ACL backend 在 Source frame 成功 Seal 后，按现有 diagnostics interest 发布只读事实：资源 identity、格式版本、压缩/驻留/流入字节、stream 状态、Context identity、effective time、采样轨道数、解码耗时、completion 和稳定错误身份。事实带同一 Frame、Projection、Rig、SourceGeneration 和 resource generation。

事实只能复制已提交结果；Pending Context、下一帧数据和调试采样不得混入。诊断关闭时跳过复制和事件，但不得跳过资源准备、解码、过渡、IK 或 Final Publication，也不得反向影响 LRU 或 source 选择。ACL 事实不扩展现有脚步诊断 schema，也不参与脚步、状态、IK、Goal 或 Gameplay 决策。

### 7. 平台和版本固定

首个实现固定当前 Unity 2022.3.62f2c1 与 Windows x64，覆盖 Editor/Mono 和正式 IL2CPP Player；固定 ACL/数值依赖 revision、编译器/浮点选项、C ABI、plugin hash、Importer 平台声明和 manifest schema。其它平台或版本在 Build/准备阶段拒绝。可复现范围首先是同一平台和 artifact 的数值结果；Frame completion 和 source generation 是生命周期身份，不应要求不同帧或不同 Actor 数值相同就复用同一身份，也不承诺跨平台 bit-exact。

最终版本固定为 Projection `v14`、Pose Program schema `v25`、Pose Runtime ABI `v28`、ACL resource manifest `v2`、Native ABI `2`。所有新容量、属性布局、time grid、资源分组和 identity 纳入 hash，旧产物重建，不解释草稿 ABI/manifest。

### 8. 正式表情素材与参数绑定

输入是明确 source identity 的解码曲线、目标 AnimationClip、Graph 参数声明和当前模型；输出是补齐属性曲线的正式 Clip，以及 Build 发布的 dense scalar/property binding。首批目标仍是 Corin 当前正式 source 闭包，不得为了复用已验证的 MainCity 样本而替换现有战斗/Locomotion source。8 份样本只证明解码能力；缺少正式目标对应数据时继续完成该目标的离线解码，不假借另一 Clip 的曲线。

Graph 继续唯一拥有 ParameterId、类型、单位、默认值和允许来源；参数新增明确的 Control/AnimatedProperty 用途。Presentation Profile 只拥有参数到 RendererBindingId、Mesh identity 与 BlendShape 的映射。显式导入命令可以通过现有 authoring mutation 建立/更新声明与 Clip 曲线，但不能由 selection、Inspector 或 Build 隐式修改作者数据。导入只更新声明的 BlendShape 曲线；骨骼、Foot/Phase 和已有人工修订保持原样，冲突输出到具体曲线供作者决策。

Actor binding 与现有 Rig binding 一起由正式 Factory 装配。运行对象显式提供 Renderer binding，Build 已固定对应 Mesh 内容 hash、BlendShape 名称身份和索引；装配校验实际 Mesh，不按名字搜索场景。单位以原始数据及模型形变记录为准，正式 Profile 映射不提供运行时缩放补偿开关。需要换单位时由一次明确导入转换完成并保存转换身份；不能猜测乘 100 或 clamp。

对于项目声明的某属性，源绑定清单明确表示该片段没有动画该属性时，Build 编译声明默认值的常量通道，采样时它也是当前合法值。原始声明有 Scalar 却未解码、文件丢失或绑定不明仍是错误，不能走该规则。这样全身动作接管时能得到该动作明确的完整属性结果，结束时通过原有过渡回到 base，避免表情残留。

14 条 Motion/Root 进入离线来源记录和用途分类，本次不作为 Renderer 属性、不导入新的 Gameplay 位移，也不覆盖现有 Simulation MotionCurve。项目正式标量运行集合只包含声明的 BlendShape；Foot/Phase 注册曲线仍走其现有编译/消费合同。

#### 参数布局与精确作者绑定

参数 dense index 来自现有 Pose 编译的唯一参数布局。把 `CharacterPoseFamilyPayloadBindingPass` 中已有的参数布局生成逻辑提取为 `CharacterPoseParameterLayoutCompiler`，原 Pose 编译和动画资源编译共用该结果，排序规则保持。删除各采样器各自 `OrderBy(Graph.Parameters)` 推导 index 的实现。

现有 source 内的 ClipBindingIndex 不因资源表排序而改变。SourceLocalClipIndex、Projection ResourceIndex、数据库 GroupClipIndex 分别保存并显式映射，不允许把三个索引空间当成同一个整数复用。分组重建后统一更新映射和 hash，旧 Projection 不得绑到新组。

Profile 的属性绑定固定为：

- `ParameterId`：Graph 参数身份；用途必须为 AnimatedProperty，类型为 Float。
- `RendererBindingId`：运行目标身份，解析现有 `CharacterAnimationRigBinding.RendererBindings`。
- `AnimationCurvePath`：Editor 读取正式 Clip 的精确相对路径，不参与 Runtime 查找。
- `ExpectedMesh`、`MeshContentHash`、`BlendShapeName`、`BlendShapeIndex`：目标身份与构建验证信息。

Graph 唯一声明单位和默认值；Profile 不再声明一份可编辑默认值。编译产物可以物化默认值，但必须携带参数布局 hash 并验证一致。

构建上下文必须显式提供已装配的 `CharacterAnimationRigBinding` 作者模型绑定，复用现有角色 Rig 模板/装配入口。Reader 用绑定中的实际 Transform 与 Animator 根生成精确骨骼路径，验证 RigId、RigRevision、dense BoneId 和 Mesh；不以 BoneId 显示名或字符串前缀猜路径，不全项目搜索“相似”模型。所需绑定作为构建输入传递，不进入 Runtime 资源表，也不新增第二份骨骼默认姿势。

Corin 的绑定来源固定复用 `CharacterRuntimeProfileRootHierarchyBuilder` 当前使用的正式 Corin 模板。在该 Editor 装配入口提取只读、带释放范围的模板读取 API，填充 AnimationBuildInput.SourceRigBinding；不调用会改 prefab 的 Synchronize。Reader 完成后只保留路径、内容身份与值，不能把临时 prefab Transform 带入压缩或 Runtime。

`RendererBindingId` 与 `AnimationCurvePath` 不必相等。曲线按 path、component type、property 三者精确匹配；作者模型中目标 Renderer、Mesh 和该路径必须一致。合法未动画的属性来自已确认的轨道存在性，编译为默认常量；声明存在却读不到、来源未解或多目标冲突均 Invalid。

Graph/Profile 的新增字段、作者路径和构建诊断同步到原 Agent Document schema、exporter、reconciler、mutation、validator；不单独创建 ACL 作者数据源。

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

#### Source 页和 typed 交接

codec scratch 按 `SourceCapacity × ClipCapacity × PhysicalBoneCount` 以及标量容量编译并由 Source 持有，不由共享资源 Store 或 Program 持有。scratch 只是解码输入工作区，不具备 Final Pose、跨帧播放或独立提交的语义。

ACL 的每 Clip Job 只填实体骨骼流；Root/Scale policy 在原 capture 中统一应用，虚拟骨骼也只派生一次。所有 ACL Clip 输入、source 内 Mixer、capture 和原 source fan-in 在同一 PlayableGraph；最终 output weight 保持零。

标量采样写 Source 自己的页。NativeClip 从编译曲线页取值，ACL 从解码 Scalar 与显式常量 binding 取值，之后都交给 `CharacterAnimationScalarMixer`。禁止在 Source 里写 Program 的参数页，禁止把共享整数组加隐含 offset 当作跨模块合同。

`CharacterPoseSourceScalarReadView` 携带 Frame、Completion、Projection、Rig、SourceGeneration 和参数布局身份。Program 在现有 Player/Slot job 内读取并复制；只有原 source capture 完成后，骨骼和标量才共同可读。不能先宣告表情 Ready、后等待骨骼，也不能静默沿用旧值。

### 10. 唯一最终发布扩展

现有 `CharacterPoseSourceModule` 已在装配时把 Animancer Graph 的最终 output weight 设为 0，图只负责采样/capture，不直接驱动可见骨骼。该行为继续保持，并覆盖所有受管理 BlendShape；不能因为新增属性把 output weight 改回 1，或通过额外 AnimationPlayableOutput 写 Renderer。

Program 在唯一 Output operation 把最终 Pose 与已解析属性通过 typed binding 交给同一个 `CharacterFinalPosePublication`。该 owner 内部增加属性 Pending/Committed 页、模型 binding 和写入记录；它们是骨骼之外的属性数据，不是第二 Final Pose 或第二 Publisher。现有骨骼 Writer 数学保持不变，最终 Apply 先整体预验证，再按固定顺序完成骨骼写入和 Renderer 形变权重写入，最后统一 Seal。数据生成、默认值解析、混合、容量、Mesh/索引和所有 lineage 检查都在首次可见写入之前完成。

根事务、Frame completion 与原四个 owner 不变。属性失效与骨骼失效一样阻断整帧最终发布；Barrier 前 Discard，Barrier 内或之后 Fault。底层 Unity 写入出现不可预期异常时报告 Fault，不通过读回旧骨骼/Renderer 做恢复后继续。Writer 开始之后不再做可能失败的业务计算、资源请求或第二次参数解析。

Preview 继续使用正式 Factory 和同一 Projection/Frame 事务；节点检查、编辑器查看或曲线诊断不调用 Renderer 写入。Reset 只通过既有 owner 的同 generation 重置及正式下一帧默认/采样结果恢复，不能保留另一个表情时钟或独立 LateUpdate writer。

保留 `CharacterFinalPosePropertyWriter`，但它只缓存已经装配的 Renderer/目标索引并执行写入。`FinalPublication` 负责先验证骨骼和属性的全部输入与目标，再按固定顺序调用两个具体写入器，最后同一 Seal。PropertyWriter.Write 内移除再次执行业务预验证，写入阶段不查找、不混合、不加载。零属性绑定是合法空工作，不额外要求存在表情参数。

### 11. 与现行spec的差异和实施闭包

本次修改的是 source 资源合同、动画属性参数声明和 Final Publication 的结果/目标范围；现有混合公式、Foot/IK/Goal、Simulation 与骨骼写入数学保持。差异已经在本 change 下的 selection-runtime、presentation-pose-graph 和 runtime-architecture 三份 delta 中明确，新增 scalar-presentation spec 约束同帧结果，不直接改 current specs 来伪装已实施。

旧稿“不修改 Final writer”过宽：保持的是骨骼算法，Publication 必须增加属性输入和 Renderer 写入，否则表情只有数值而没有消费者。旧稿“没有现有 capability 变化”也已移除。新增参数用途/属性布局、resource binding 和 publication identity 要统一提升 schema/ABI，并由明确 Character Build 发布新 Program、Projection、资源包与 native artifact；旧版本拒绝装配，不提供字段缺省迁移或运行时兼容。

`原始资料 → 离线导入 → 正式 Clip/Graph/Profile → Character Build → 资源服务准备 → Source typed 结果 → 原 Program/Pose Plan 混合 → 同一 Final Publication` 是唯一链路。发布验收必须同时覆盖资源闭包、属性数值、模型目标和最终可见结果；离线 8 份样本或编译通过都不能替代完整接入验收。

| 规范 | 本设计如何遵守 |
| --- | --- |
| current `character-pose-graph-runtime-architecture` 的四个 owner、typed 页与根事务 | 不增业务 owner；资源缓存独立于 Pose 事务；backend discriminator 跟随原 Registry；Program 只写自己的参数页 |
| 本 change 的 `character-animation-acl-runtime` | 完整官方数据块、固定质量、独占 Context、共享数据、异步准备、无 fallback 和完整生命周期均落实到明确类型 |
| 本 change 的 `character-animation-scalar-presentation` | 精确作者路径/运行绑定、默认常量、统一时间/权重、同一 Source completion 和同一最终发布 |
| 本 change 的 selection-runtime / presentation-pose-graph delta | NativeClip/ACL 统一编译资源入口；骨骼 Mask 不再乘属性；禁止第二逻辑 Player/Graph/Final Pose |

current spec 的最终发布仍描述骨骼范围，本 change 已有 delta 明确扩展属性；这是待实施差异，不能提前修改 current spec 声称已完成。本文新增的是实现细化，没有放松该差异，也没有引入新的玩法能力。

图内 Clip 输入 Job 只写未发布 AnimationStream，codec 页只是声明容量的 Source scratch；二者不构成 current spec 禁止的第二 Final Writer、第二最终 Pose 或可编排 Writer 节点。具体骨骼写入和 Foot/IK/Goal 数学继续保留。

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

| 决定 | 得到什么 | 接受的代价 |
| --- | --- | --- |
| ACL source 内骨骼混合使用 Unity Mixer | 与 NativeClip 共用引擎混合行为，维护者不需要再理解和校正一套四元数混合公式 | 多一些预创建的图节点，容量与耗时须纳入既有诊断 |
| 按 Rig/参数/压缩设置组成共享数据库组 | Corin 的闭包、固定质量和预取容易对账，多角色复用同一组数据 | 单 Clip 改动会重建并重新发布所在组，启动准备会占用整组内存 |
| 实际采样率由时长和完整区间数计算 | 保留原动作时长和末端，不需要在播放阶段补时间比例 | 每条 Clip 的实际频率可能略有不同，必须显式存储和参与 hash |
| 作者曲线路径与运行目标 ID 分开 | 换运行实例不需要按层级猜对象，构建能准确定位源曲线 | 作者模型层级变化时要更新映射并重建，Build 必须检查路径失效 |
| 按编译容量预分配 source 图节点、Decoder 与 scratch | 动作切换和同帧混合不临时创建这些对象，时间与释放边界清楚 | Actor 装配成本和预留内存更高，容量必须有可读统计 |
| 编译只返回产物，统一 Build 决定发布 | 作者配置、Program、Projection、ACL 资源以同一清单生效 | 编译 request/result 和正式发布阶段需要一次明确接线 |

以上取舍已由设计窗口作出，不再交给实现窗口选方案。实现遇到与现有已正确行为不可兼容的具体冲突时，向设计窗口报告证据；不得自行降级为简化播放器或临时实现。

## Migration Plan

原有 Corin 完整闭包、离线属性补齐和发布任务全部保留。当前草稿按以下清单迁移，保留已经修正的行为。

| 当前文件/实现 | 执行动作 |
| --- | --- |
| 935 行 `CharacterAclAnimationResourceBuilder.cs` | 保留协调入口；原菜单删除；读取移到 Reader、采样移到通用 Sampler、压缩移到 Compiler、EvaluateQuality 移到 Evaluator、BuildAsset/文件操作移到 Publisher；删除原嵌套重复 DTO |
| 新增 `CharacterAclAnimationBuildContracts.cs` | 保留 ACL 产物/报告；RawTransform/RawScalar 等通用数据移到 Sources/BuildContracts 并去掉 Acl 前缀；删除旧 Builder 同名声明 |
| 新增 `CharacterAclAnimationSourceSampler.cs` | 迁移为 `Sources/CharacterAnimationSourceSampler.cs`；路径/AssetDatabase 读取剥离到 Reader，删除 ACL 专用副本；Projection 的 BuildScalarCurvePage 共用此链 |
| 新增 `CharacterAclAnimationQualityEvaluator.cs` | 保留，改接 AuthoringSource/SampleSet/完整数据库产物；统一 Grid 与验证时间策略；不再重新读取 Profile 或推导参数索引 |
| `CharacterAclPoseSamplingBackend.cs` | 按 Decisions 第 3 节拆出 pool、instance、graph、clip job、journal；删除 SourceKey/找空槽/自增退休 generation、C# 骨骼 Lerp 与文件资源逻辑 |
| `CharacterAnimationPoseSamplingBackendRouter.cs` | 删除；backend discriminator 迁移到原 PhysicalPoseSourceRegistry 的记录，Source Module 直接选择 backend |
| 草稿 `CharacterAclAnimationResourceService.cs` / Lease | 删除；替换为 Scope/Store/Preparation 和值类型 lease；删除按 InstanceID 缓存和共享 Context 的实现 |
| `CharacterAclNativeBridge.cs` | 只留定型 interop；native DTO 移到 `CharacterAclNativeContracts.cs`；Context 包装迁为 `CharacterAclDecoder.cs`；删除接收 ScriptableObject 的 Create 路径 |
| `AnimancerPoseSamplingBackend.cs` 顶部共用合同 | 移到 Animation/Contracts/Sources；正文只做接口/规范化批次接线，不重写既有 NativeClip 行为 |
| `CharacterPresentationProjectionCompiler.BuildScalarCurvePage`、草稿中重复的曲线查找 | 删除重复实现，消费通用构建模块返回的结果 |
| `CharacterFinalPosePropertyWriter.cs` | 保留具体目标写入职责；整体预验证归 FinalPublication，禁止第二次业务判断 |
| 草稿原生 DLL、旧 resource/Projection 工件 | 完成新 ABI 后重新生成和替换；不保留加载旧草稿的运行分支 |

以下按实现依赖排序，原 tasks.md 的功能范围全部保留：

1. 一次迁移去重 Builder 与三个草稿文件，完成 Reader/Sampler/Contracts/Pipeline/Publisher 的职责切分；保留已修正的 actualSampleRate、AnimationCurvePath、参数偏移和权重行为。
2. 完成 Native ABI 2 的共享 group、独占 Decoder 与整组数据库编译；交付 native 构建与导出清单。
3. 完成 Scope/Store/Loader 与现有会话/Preview 装配，资源准备在 Frame 外推进；删除草稿共享 Context 服务。
4. 完成 Registry backend 字段、ACL pool/graph/journal 和删除 Router；接好两个 backend 的统一采样批次。
5. 接好 Source 标量页、原 Program 消费与唯一 FinalPublication；补齐全部调用点和 Agent Document。
6. 发布完整 Corin 闭包、质量与 native 清单，统一 schema/hash，交付最终代码依赖与资源清单。

每个可构建批次使用独立中文提交，附输入输出说明、改动文件、实际构建日志和已关闭缺陷。提交不得夹带交接时已有用户改动。源码引用/重复类型/owner/容量/释放边界与编译一起核对；不把“类已创建”算成功能完成。

不设机械的文件行数指标。协调类只应表现业务步骤；单类出现读取、算法、I/O、生命周期等多种变化原因，必须按本表处理。任何新增或合并本文列出的 owner、替换接口语义或另建流程，先给设计窗口具体冲突与证据，不由实现窗口自行决定。

本次不新增测试工程。执行原构建和诊断门禁，记录未取得的运行证据；用户按原约定做最终端到端验证。禁止 Unity batchmode；dotnet/MSBuild 使用项目要求的关闭构建服务器参数并立即清理。

部署回滚仍使用上一版完整 Projection、资源清单与 native artifact，不在 Runtime 选择备用数据。

## Open Questions

本次没有影响实施范围、owner 或输入输出合同的未决项。其它角色和 Windows x64 之外的平台属于后续扩展，需独立资源清单与 native artifact，不作为本次实施前置。
