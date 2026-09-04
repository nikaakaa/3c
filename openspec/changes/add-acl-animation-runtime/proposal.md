## Why

ZZZ 的 Corin 动画使用不同版本的 ACL Transform、Scalar 和数据库数据；本轮全量文件审计发现，现有独立导出缺数据库头与完整绑定，展开动画也未完成 Scalar 解码。3C 需要在完整的输入和质量合同下建立项目所有的 ACL 运行链，并接入现有唯一 Pose Source Module。

本稿处于复核阶段。资源、时间和生命周期的确定问题见 [research.md](D:/Unity_Project_1/3C/openspec/changes/add-acl-animation-runtime/research.md)。用户尚未确定是否在本次补齐 Corin 表情；这会改变质量参考、标量消费者和发布范围，因此当前文件齐全不代表已可实施。

用户随后已授权实现窗口完成离线 Scalar 解码；8 个完整片段、53,020 个样本值已通过本轮离线数据复审。它们可以进入正式素材发布的下一阶段设计，尚未证明 Unity 面部变形或 3C Scalar 混合/发布；这不改变本文仍需收口的 Runtime 范围。

## What Changes

- 新增项目所有的 ACL 动画资源合同，分别保存 Transform、可选 Scalar、数据库头及各 tier 的 bulk；固定各块版本、合法空块声明、Rig/Binding/default 身份、正式时长、采样范围和内容哈希。
- 新增 Editor-only ACL 构建与打包步骤，从正式 `AnimationClip` 生成可运行 ACL 资源，并保留压缩设置、误差度量和构建身份；不把 ZZZ 私有 DLL 或 UnityPlayer 偏移作为项目依赖。
- 新增 native ACL 解码桥，使用预分配解码 Context 和 Pose writer，支持按时间采样单轨道或整套 Pose，禁止每帧分配和临时展开为大量 Unity 曲线。
- 将 ACL backend 接入现有唯一 `CharacterPoseSourceModule` 和唯一 Evaluate Barrier；ACL source 使用现有 PlayableGraph，不创建第二个 Graph、第二个 Player、第二个 Final Pose 或第二条 IK 链。
- 新增 ACL 资源异步准备、预取、流入、释放和 LRU 生命周期；所需质量数据完整并取得租约后才 `Ready`，可见期间禁止缓存回收改变质量。候选 target 为 `Pending` 时，已有合法 source 继续按原规则采样；不得伪造 target sample 或回退到其它 backend。
- 扩展 Projection 的 source resource binding，使每个 source 在编译期明确选择 ACL 或现有原生 Clip backend；运行时只消费 Projection 的 dense identity、格式和资源句柄，不查作者字符串或 AssetDatabase。
- 扩展只读诊断与性能事实，记录 ACL resource readiness、resident/streaming 状态、解码 Context、采样时间、解码耗时、压缩字节和错误身份；不把这些事实用于脚步、IK、State 或 Gameplay 决策。
- **BREAKING** 对 ACL-backed source 禁止继续走展开 `.anim` 播放路径，避免同一 source 存在两个可见播放器或两个 Pose 真相。
- 不修改 Foot Placement、Landing、FBBIK、BendHistory、Action 语义、Gameplay Timeline、IK 目标或最终 Pose writer；ACL 只负责动画源数据的存储、流入和 Pose 采样。
- 将“项目 ACL 相对正式素材的压缩误差”与“正式素材相对原始 ZZZ 的还原误差”分开报告；缺失表情或未证明原始解码时，不承诺原始动画完整还原。

## Capabilities

### New Capabilities

- `character-animation-acl-runtime`: 定义 ACL 资源构建、native 解码、资源流式生命周期、Projection 绑定、唯一 Pose Source Module 接入和只读诊断事实。

### Modified Capabilities

- `character-animation-selection-runtime`: 将 source 采样合同扩展到编译期确定的 NativeClip/ACL 资源，覆盖 Action、Direct Clip、Blend Space、Motion Matching 和 Preview，同时保留唯一 consumer、readiness、usage 和 release 所有权。

若用户选择本次补齐表情，还需声明对应标量混合与统一发布的 capability 变化；该范围尚未定稿。

## Impact

- Runtime：`Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation` 下的 source module、Projection source binding、Playable/Animation Job 接入和资源生命周期。
- Editor/Build：ACL payload builder、格式/误差校验、Projection 编译、native plugin 构建与平台产物清单。
- Native dependency：项目基线采用官方 ACL v2.1.0，commit `414689d5cff4286a7898487a46dc5e48005d38da`，项目 payload 使用该版本的正式格式；不把 ZZZ 版本 100 数据重标后送入官方解码器，不依赖游戏私有 DLL。
- Resource：新增 ACL transform/scalar/database 资源及其 hash、Rig、binding、版本和 stream manifest。
- 兼容边界：现有非 ACL source 仍由现有 backend 播放；ACL-backed source 不提供运行时 fallback。该变更不改变脚步修正数学，也不承诺 ACL 解码本身解决 Foot Placement 踏空。
