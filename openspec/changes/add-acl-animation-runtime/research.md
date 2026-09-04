# ZZZ ACL 资源与 3C 接入复核

日期：2026-09-04。前半部分记录 Sol 首轮只读复核的证据与缺口；第 12 节记录随后实现窗口完成的离线解码及 Sol 复审。Sol 未修改游戏、导出器、3C 实现或 Unity 资产，未启动 Unity 或游戏采样。

最新状态：8 个完整片段的 53,020 个 Scalar 样本已产出，可作为下一阶段离线数据源。旧稿中“尚未解码”“单轨道接口未实现”只描述首轮检查时的工具版本；当前覆盖与限制以第 12 节及 [解码复审记录](D:/Unity_Project_1/3C/openspec/changes/add-acl-animation-runtime/research/scalar-decoding-review.json) 为准。Unity 面部变形及 3C 标量发布仍未验收。

## 结论与证据范围

旧稿的唯一 Source、唯一 Graph、唯一 Pose Plan 和不修改 IK 的方向可以保留，但资源格式、质量参考、标量去向、时间映射和流入期间的一致性没有闭合。不能把旧稿的 OpenSpec 格式校验通过当作这些问题已经解决。

本轮覆盖了本地 Corin 导出批次全部 144 份 ACL 文件组、8 份含完整字段的原始 AnimationClip JSON、两代导出工具代码、实际 UnityPlayer 的相关静态指令，以及 3C Source/Capture/Projection/帧事务入口。没有证明 ZZZ 全部动画状态、完整运行时缓存策略或每条动画的最终游戏表现。逐文件计数、头信息及原始证据哈希见 [资源审计数据](D:/Unity_Project_1/3C/openspec/changes/add-acl-animation-runtime/research/acl-resource-audit.json)。

| 复核项 | 直接证据 | 能得出的结论 | 尚不能得出的结论 |
|---|---|---|---|
| 原生导出数量 | 批次 manifest 与磁盘逐文件检查 | 169 条记录，153 个不同名称；143 条标记 native-acl，1 条标记缺 database，25 条为 not-zzz-acl | 143 条均已完整解码或可直接用于 3C |
| 压缩头 | 144 组文件的头、长度、SHA-256 | Transform 全为版本 8、类型 12；Scalar 全为版本 100、类型 0；元数据哈希与长度均匹配 | 两种数据都属于官方同一版本，或只改版本号就能解码 |
| 数据库 | 原始 JSON、导出器、解码入口的四个指针 | 数据库头与外置 bulk 是两个不同输入；独立导出漏了数据库头 | 当前 database.acl 是可直接初始化的 compressed_database |
| 绑定 | 8 份原始 JSON | 常见动作是 205 个 Transform、41 条模型形变和 14 条 Motion/Root 分量 | 670 个 binding 就是 670 根骨骼 |
| 展开动画质量 | 展开 manifest、native 循环、当前 Run.anim | manifest 全部记录 ScalarTracksDecoded=false；native 循环只解码 Transform；Run.anim 的 m_FloatCurves 为空 | 展开的 .anim 已完整恢复表情与根运动 |
| 时间 | 原始 Clip 范围与压缩头 | 正式播放时长与压缩数据覆盖范围需要分开保存 | 可以用压缩样本数直接替换 Clip.length |
| 引擎接入 | 本机 UnityPlayer 的源码路径、指令和 PlayerLoop 字符串 | ACL 家族实现已经存在于该 UnityPlayer，且有数据库相关路径 | 已确定官方 ACL commit、全部 Animage ABI 或动态缓存策略 |
| 3C 接入 | Source Module、Catalog、Capture Job、Coordinator | 现有接口仍直接依赖 AnimationClip；扩展需要贯穿全部 source 使用点 | 增加一个 DLL 和一个枚举就已经接通 |

## 1. 原始数据实际由哪些部分组成

已检查的 AnimeStudio 将 ZZZClip 中的连续字节解释为：

```text
m_ClipData
  ├─ Transform compressed_tracks
  ├─ 对齐到 16 字节
  └─ Scalar compressed_tracks

m_databaseData  → 数据库头、索引和声明
m_DatabaseData  → StreamingInfo 指向的外置 bulk
genericBindings + Avatar/模型资料 → 轨道的真正目标
Clip start/stop/loop → 作者播放时间合同
```

`m_databaseData` 和 `m_DatabaseData` 仅大小写不同，但语义不同。`ACLExtensions.Process` 把 Transform、Scalar、小写数据库头、大写外置 bulk 分别传给 native。`CorinAclAssetExporter` 只导出大写字段，并将它命名为 `.database.acl`；`native-acl` 状态只检查该数组是否非空。

8 份完整原始 JSON 都另外保存了 88 字节的数据库头，其 tag 为 `0xAC11DB01`、版本为 100。例如 MainCity_Run_Loop 的外置 bulk 是 40,727 字节，数据库头是另一份数据。证据路径：

- [原生导出器](D:/ZZZ_Dump/kern_tools/CorinAclAssetExporter/Program.cs:69)
- [ZZZACLClip 字段与对齐拆分](D:/ZZZ_Dump/tools_dl/AnimeStudio/AnimeStudio/Classes/AnimationClip.cs:972)
- [外置流读取](D:/ZZZ_Dump/tools_dl/AnimeStudio/AnimeStudio/Classes/AnimationClip.cs:2078)
- [解码器参数组装](D:/ZZZ_Dump/tools_dl/AnimeStudio/AnimeStudio.Utility/ACL/ACLExtensions.cs:22)
- [MainCity_Run_Loop 原始数据](D:/ZZZ_Dump/output/corin_start_loop_clip_json_20260903/AnimationClip/Avatar_Female_Size01_Corin_Ani_MainCity_Run_Loop.json)

`Galgame_Facial_Think` 的外置 bulk 为零，只能证明该导出字段为空。需要结合原始数据库头的 inline 标记和各 tier 声明长度，才能判断是否缺少必需数据。现有标记不足以证明游戏原资源损坏。

正式项目合同必须区分“不存在标量轨道”“未绑定数据库”“该 tier 正式长度为零”和“声明必需的数据丢失”。前三种是可以声明的合法格式，最后一种才是缺失错误。

## 2. Transform、Scalar 和数据库版本不相同

全部 144 组头检查结果：

| 数据 | tag | 版本数值 | track type | 文件数 |
|---|---|---:|---:|---:|
| Transform | 0xAC11AC11 | 8 | 12（qvvf） | 144 |
| Scalar | 0xAC11AC11 | 100 | 0（float1） | 144 |
| 原始 JSON 内数据库头 | 0xAC11DB01 | 100 | 不适用 | 本轮检查 8 份 |

文件头版本 8 对应官方版本表中的 `v02_01_99`；本地 AnimeStudio fork 又定义 `vHoYo=100`，并实现专门的 scalar 解码上下文。数值相同只能用于识别格式分支，不能证明私有布局与官方布局完全等价。[官方版本表](https://github.com/nfrechette/acl/blob/v2.1.0/includes/acl/core/compressed_tracks_version.h)、[本地 fork 版本表](D:/ZZZ_Dump/tools_dl/AnimeStudio/AnimeStudio.ACLNative2/acl/core/compressed_tracks_version.h:71)。

项目沿用“正式 AnimationClip → 项目 ACL 构建产物”的方向时，应锁定自己的格式。已核实官方 `v2.1.0` tag 指向 `414689d5cff4286a7898487a46dc5e48005d38da`，其正式格式版本为 10；不能把 ZZZ 的版本 100 标量直接交给该解码器。是否另外建设 ZZZ 原始数据完整导入，需要独立明确输入与验收范围，不能藏在“已批准 ACL 输入”这句话里。

## 3. 55 条 Scalar 到底是什么

8 份原始 locomotion JSON 的绑定分布完全一致：

- 205 个骨骼 Transform，每个有 rotation、position、scale 三个 binding，共 615 个 binding。
- 41 个 `SkinnedMeshRenderer` binding，`customType=20`，用于 blend shape。已有模型绑定报告可以解析到眼睛、眉毛、嘴等名称。
- 14 个 `Animator` binding，`customType=8`，attribute 连续为 0–13。本地导出格式表将其解释为 `MotionT.xyz`、`MotionQ.xyzw`、`RootT.xyz`、`RootQ.xyzw`，是两组根变换。

因此：`615 + 41 + 14 = 670` 个 binding；`205 × 10 + 55 = 2105` 个标量分量。不能混用 bone count、binding count、track count 和 float count。

依据是 [MuscleHelper](D:/ZZZ_Dump/tools_dl/AnimeStudio/AnimeStudio.Utility/YAML/MuscleHelper.cs:5) 和 [绑定解释入口](D:/ZZZ_Dump/tools_dl/AnimeStudio/AnimeStudio.Utility/YAML/AnimationClipConverter.cs:366)。它们证明这些编号在已检查解析器中的含义；游戏如何消费 Motion/Root、与 Animage 的转换顺序以及其动态数值仍未完整验证。

3C 的表现注册曲线还有自己的职责：Phase、Foot Placement Weight、Foot Motion 等由正式 Clip 在 Build 时进入既有 Projection/Program。它们不能因为名字像 Scalar 就改由 ACL decoder 决定，也不能把原始 Motion/Root 分量直接接到 CharacterController，造成第二条位移来源。

## 4. 当前展开 .anim 不能当作完整原始质量基准

`20260903_animations_all_v2/export-manifest.json` 的 169 条记录均为 `ScalarTracksDecoded=false`。这个字段由导出器固定写入 false，属于导出能力声明，不是逐条解码数值的检测结果。

进一步读 native 源码可以看到：它创建 scalar context、预留 scalar 输出空间，但逐帧循环仅调用 `transform_context.seek/decompress_tracks`，没有调用 scalar 解码。它还保留了 scalar 初始化跳过安全检查、只支持特定 tier 组合、缺 bulk 时继续按无数据库解码等离线工具行为。这些都不能成为项目正式运行合同。

当前工作区的 [Run.anim](D:/Unity_Project_1/3C/3cDemo/Client/3C_Client/Assets/AssetArt/Animation/ZZZ/可琳/dump/Avatar_Female_Size01_Corin_Ani_Run.anim:161764) 的 `m_FloatCurves` 确实为空。其骨骼曲线可以是当前 3C 的 authoring 输入，但不能声称它覆盖原始 41 条表情和 14 条根变换。

进一步核对“能否解码”：本地 [decompression.hoyo.h](D:/ZZZ_Dump/tools_dl/AnimeStudio/AnimeStudio.ACLNative2/acl/decompression/impl/decompression.hoyo.h:472) 已有版本 100 的整组 Scalar 解码实现，包含 seek、数据库片段定位、数值重建和 `write_float1`；单轨道 `decompress_track_v0` 在第 755 行仍是未实现断言。因此存在可继续完成的批量解码路径，但不能把代码存在当作数值已经验证正确，也不能直接采用原稿“单轨道或整套均可用”的说法描述该离线工具。

MainCity_Run_Loop 的完整原始 JSON 实际包含 599 字节 Scalar（55 轨、44 个样本）、88 字节数据库头、40,727 字节 bulk 和 41 条 BlendShape 绑定。它可以作为首个离线解码验证输入；本轮仍未执行该解码器，尚未证明输出的表情数值、插值和数据库定位全部正确。

质量需要拆成两项：

1. **项目压缩误差**：项目 ACL 解码相对正式 AnimationClip 参考采样的误差，包含均匀重采样和压缩两段误差。
2. **原始还原误差**：正式 AnimationClip 相对原始 ZZZ 的差异。原始解码、绑定或曲线缺失时，此项必须标记未证明。

重新压缩第一项不能证明第二项，也不能补回缺失曲线。发布报告需要保存基准身份和覆盖范围，不能只写“误差达标”。

## 5. 播放时长、循环和采样次数必须拆开

Run 的正式 start/stop 为 0–0.5 秒，但 Transform 和 Scalar 头都记录 32 个样本、60 Hz，按 `(N-1)/rate` 得到约 0.516667 秒。MainCity_Run_Loop 的正式 stop 为约 0.7 秒，而头中为 44 个样本。全批次的差值范围约为 0 到 1/60 秒；不能用单一固定裁剪规律替代逐资源验证。

压缩头描述可采样数据范围，作者 Clip 描述播放范围。Project builder 应在正式范围内定义自己的采样网格、最后一帧、循环端点与速度映射，并将其纳入资源身份。Program 仍唯一计算 effective time；native decoder 不应再次累计时间或乘一次 play rate。

官方 ACL 的 wrap 会让末端向首帧插值，并影响 duration；它不适合非循环播放或具有非周期根位移的曲线。首个项目格式可以明确保留端点、关闭隐式 loop 优化，由既有 Program 的 Finite/Cyclic 语义决定时间。[ACL 循环说明](https://github.com/nfrechette/acl/blob/v2.1.0/docs/handling_looping_playback.md)。

## 6. Database 流入会影响质量

官方 ACL 数据库把部分关键帧放到不同 importance tier。缺少数据库仍可能解出较低质量姿态；流入更多数据可能改变同一时刻的解码值。因此旧稿“LRU 只影响驻留，缓存情况永远不影响结果”少了质量条件。

要维持原先的固定结果要求，正式资源必须声明所需 tier/chunk 集合；只有完整到位才 Ready，可见期间持有租约并冻结本帧集合，不能在解码中途因流入而提升质量。该集合不完整时保持 Pending；不能调用底层允许的低质量解码当成正常 Ready。[ACL 数据库与流入说明](https://github.com/nfrechette/acl/blob/v2.1.0/docs/database_support.md)。

项目层还必须区分三种寿命：共享不可变 payload、共享 database/context 与 streamer、每个活跃 source 的解码上下文。一个 Actor 退休某个 source，不代表其它 Actor 的共享数据库可以释放；LRU 只能回收没有租约和在途解码的资源。stream-out 必须在解码完成后执行。完整资源准备必须先于该 source 的首份 sample，不能依赖成功 Seal 才推进加载，否则首帧 Pending 会永久等待。

官方 v2.1.0 的 database 构建支持明确限定为 Transform；项目 Scalar 应使用该版本正式支持的独立 scalar 数据流，不能照搬 ZZZ 的 scalar database 扩展。[compression_settings.h](https://github.com/nfrechette/acl/blob/v2.1.0/includes/acl/compression/compression_settings.h)。

## 7. 默认轨道不是缺失资源

ACL 可以把一条子轨道存成 animated、constant 或 default。default 子轨道不保存样本，解码时依赖明确的 reference/default 值。旧稿笼统禁止“默认值补全”，会把合法的压缩方式一并禁止。

应禁止的是用未声明默认值掩盖错误；应允许的是资源合同声明的 reference pose、默认 scale、默认 scalar 和轨道掩码，并把这些数据的 hash 与 Rig/binding 一起验证。不能沿用上一帧页中残留的值，也不能把 default 子轨道误判为数据缺块。[默认子轨道说明](https://github.com/nfrechette/acl/blob/v2.1.0/docs/default_sub_track_handling.md)。

## 8. UnityPlayer 静态复核与旧结论修正

本轮实际读取 `D:/Normal_Software/HoYoPlay/games/ZenlessZoneZero Game/UnityPlayer.dll`，SHA-256 为 `66493677ce2739e188c41634230c3a35e5f584c4575650949d0a53b5785c6c44`。下述地址都是该文件的 RVA，仅供证据定位。

- 文件含 `AnimageSDK/ThirdParty/acl/...` 的 transform 解码源码路径以及 `PostLateUpdate/ACLDatabaseLRUSweep` 字符串。
- `0x1117470` 检查 16 字节对齐、`0xAC11AC11` 和版本 7–9；`0x1114C20/0x1114D50` 调用该入口、seek 和 Transform 解码。
- 本轮进一步找到 `0xCE4FB0/0xCE5040`，它们检查数据库上下文，并使用 `version - 7 <= 0x5D` 的边界，即 7–100，再分别转入 `0xCE1230/0xCE1460`。这证明之前的 7–9 路径不是该引擎的全部 ACL 接入。
- `0xCF3530` 同样检查到版本 100；当第一个 track type 非 scalar 时，按首块 size 对齐到 16 字节寻找后续数据，与原始资源的 Transform/Scalar 拼接结构相符。这里尚未证明全部 Scalar 算法或动态调用次数。
- 普通 PE import table 没有独立 acl/AnimeStudio DLL。它支持“所检查代码在 UnityPlayer 内”的判断，不能单独排除动态加载的其它模块。

旧会话提到的 AnimationClip `StreamIn/StreamOut`、LRU 参数接口，本轮未从磁盘 GameAssembly 的明文中重新定位；保留为历史线索，不把名称、参数默认值或真实调用顺序写成已验证项目要求。没有确定 ZZZ 官方 fork commit、LRU 默认预算、质量提升时机或跨线程完整协议。

## 9. 3C 的实际接入缺口

| 当前入口 | 当前行为 | 方案必须补足的内容 |
|---|---|---|
| CharacterPoseSourceCatalog | Action、Direct Clip、Blend Space、MM 都保存 AnimationClip binding | 同一 dense 资源表覆盖所有使用点；ACL 引用不能仍带运行时 Clip 强引用 |
| AnimationPoseSourceClipBinding | 构造时要求 AnimationClip 和正长度 | 用正式 backend 资源合同区分 authoring Clip 与运行数据，不能传空 Clip 绕过 |
| AnimancerPoseSamplingBackend | 物理 source、混合、capture 与 release 集中在现有实现 | 在统一 Source 生命周期内增加 backend 分工，不能复制第二套 registry |
| AnimationSourcePoseCaptureJob | 从 stream 读 Physical Pose，然后执行 Root/Scale policy、Virtual Bone、Velocity、completion | ACL 输入必须复用这些后续步骤和既有输出页，不能直接写 raw Pose 后跳过 |
| CharacterPoseProgramSourcePreparationRuntime | 安装与更新同一 Graph 的 Player/Stack 工作节点 | ACL 内部源节点只能接入现有拓扑，不能给 Program 新建第二个逻辑 Player |
| CharacterPoseFrameCoordinator | Prepare 完成后一次 Evaluate，再执行 Pose Plan | 预期 Pending 在 Barrier 前按 typed outcome 收口，不得抛异常代替正常等待 |
| CharacterFinalPosePublication | 唯一 Physical Bone writer | 若纳入表情，需明确唯一 Scalar 发布 owner 及事务；不能由 decoder 直接 SetBlendShapeWeight |
| CharacterPresentationProjectionCompiler | 从注册曲线构建 Phase/Foot 等数据 | 继续保持原曲线消费者和数值语义，不能改走另一条 ACL 参数采样链 |
| IResourceModule / YooAsset | 已有正式资源包与异步加载 | ACL 资源准备接入该资源体系；不添加裸路径、独立下载器或 Preview 私有加载器 |

Reference Pose、Root/Scale policy、Virtual Bone、速度历史和 completion 都是同一份动画结果的组成部分。“不创建第二 Pose buffer”应准确指不新增平行真相；现有由容量计划分配的源页、当前/历史页和正式 scratch 仍需保留。

## 10. 与 current specs 的对照

| 现行规范 | 旧稿的问题 | 修正方向 |
|---|---|---|
| character-animation-selection-runtime | 只笼统说新增 backend，没有覆盖完整 Action/Clip/Blend Space/MM/Preview 资源合同 | 明确资源统一解析及所有权，保留原逻辑 consumer |
| character-presentation-pose-graph | Pending 保持当前合法 source 被旧稿“不用历史 Pose”一句掩盖 | 当前 source 继续按本帧时间采样是正式行为；禁止的是伪造 target sample |
| character-animation-pipeline | 预期 Pending 本应使用 outcome；旧任务没有处理现有要求 Ready 的构造入口 | 在 Barrier 前传递 typed Pending；加载进度独立于帧提交 |
| character-pose-graph-runtime-architecture | Final Publication 与结果页只有一个 owner | ACL 复用 capture 的 Root/Scale/Virtual/Velocity/completion；表情纳入与否需明确 |
| character-pose-plan-compilation | Image Seal 后不可补字段、容量和 ABI | resource layout、source 容量及绑定 hash 都应在正式 Build 中完成 |
| character-foot-placement-presentation / project.md | Foot 与 Phase 曲线已有正式 authoring 和消费路径 | 本轮不改变 Foot、IK、Goal、Simulation root motion 数学 |

本轮不修改 current specs 来掩盖尚未实施的能力。后续应在本 change 下补充真正改变要求的 delta，并在 proposal 中声明；不能继续无条件写“本次不修改现有 capability”。

## 11. 尚需用户明确的范围

“迁移现有 3C 表现”和“补齐 Corin 原始表情”有不同交付内容：前者以当前正式素材为质量基准；后者还需要完整原始解码、表情名称/模型绑定、标量混合及统一发布。后者不能用目前 ScalarTracksDecoded=false 的素材作为完整参考。

用户已要求解释该区别，尚未确认表情是否纳入本次验收。此处保留决定依据，不替用户选择优先级。Motion/Root 数据无论如何保留明确来源边界，不让 ACL decoder 成为第二个 Gameplay 位移 owner。

## 12. 实现窗口离线解码结果复审

实现窗口在 AnimeStudio 仓库提交 `76d55f6` 与 `ae75ccc`，补充实际 Scalar 采样、整组与单轨道接口、数据库头/bulk 和 streamer 寿命、managed 对齐与任意时间入口。Sol 已核对提交范围、工作树状态、实际 DLL hash、全部 8 份源 JSON hash，以及绑定参考 manifest hash。

接受范围是 **8 个完整 Corin 片段的已解码离线数据源**，不扩大为全部 144 组资源、完整 ZZZ 私有格式、正式 3C 资产或游戏内表情还原通过。继续沿用“离线完整解码 → 正式 authoring 素材 → 项目 ACL 构建”的方向，不把 AnimeStudio fork 和版本 100 直接装入项目 Runtime。

| 片段 | 样本数 | 随时间变化的表情轨道数 |
|---|---:|---:|
| MainCity_Run_Loop | 44 | 0 |
| MainCity_Run_Start | 92 | 33 |
| MainCity_Walk_Loop | 68 | 0 |
| MainCity_Walk_Start | 112 | 21 |
| MainCity_Walk_End_02_L | 182 | 31 |
| MainCity_Walk_End_02_R | 182 | 20 |
| MainCity_Walk_End_L | 142 | 7 |
| MainCity_Walk_End_R | 142 | 39 |

每片段均为 41 条 BlendShape 加 14 条 Motion/Root，共 55 条 Scalar，合计 53,020 个样本值；数值全部有限，重复解码一致，绑定参考内不存在同 key 指向不同目标的冲突。正式 Clip stop 与压缩覆盖范围分别记录。该绑定证明针对原始模型身份，尚未验证当前 3C Mesh 的对应形变及索引。

首个 Run_Loop 与旧 `acldb_zzz.dll` 的 2,420 个 Scalar 值逐 bit 一致，但该片段的 41 条表情全部是 default/constant；这项对照不能单独证明其余片段的动态表情或每一种位宽。旧 DLL 是另一份离线实现，不是游戏内同帧真值。

复审修正了实现消息中的插值口径：报告实际请求 `0、0.5/60、1/60、1.5/60` 四个时刻，其中两个是半帧点，每片段做 110 次逐轨道中点比较。它们均通过，但不能称为“四个半帧点”，也没有覆盖整个片段的 segment 交界、末端或循环接缝任意时间采样。本批实际出现的位宽为 3–16，数据库均为 medium=0、low=1；24–31 位、raw32、双 tier、多 chunk 和单轨道接口的全部情况仍没有这批数据证明。

正式 DLL SHA-256：`d8afe819c8d139437d13cab14bbc339586f228f553cd9ec1763b989f28182c40`。报告及其 hash 已固化在 [scalar-decoding-review.json](D:/Unity_Project_1/3C/openspec/changes/add-acl-animation-runtime/research/scalar-decoding-review.json)。原始结果见 [批次报告](D:/Unity_Project_1/3C/.codex-artifacts/acl-scalar-decoding-20260904-complete/batch-summary.json) 和 [旧解码器对照](D:/Unity_Project_1/3C/.codex-artifacts/acl-scalar-decoding-20260904-complete/oracle-diff.json)。

进入 3C 正式表情发布前，仍需明确以下业务合同：

1. **素材与模型绑定**：把已解码曲线完整发布进正式 AnimationClip，核对当前 Mesh 的形变名称、顺序、reference/default 与权重单位。不能按名字猜索引、统一乘 100 或把超出 0–100 的值直接裁掉。
2. **表情混合归属**：动作自带表情可以随同一动作计划混合；需要眨眼、说话等独立覆盖时，应在同一计划中显式表达 mask、权重和优先级。两者是不同业务表现，不能让 decoder 自行选规则，也不能由“Playable 会混”替代正式合同。
3. **统一发布**：当前 Final Publication 主要拥有骨骼输出。表情要求扩展同一表现帧的标量结果与 Renderer 写入职责，同时保留唯一骨骼 Writer、completion、Seal/Discard 与 Fault 边界；不能让 native decoder 或 Preview 直接写 Renderer。
4. **根运动归属**：14 条 Motion/Root 保留素材证据与明确编译用途，不直接变成 Gameplay 位移，也不复制 Foot/IK/Goal 曲线的原有消费者。
5. **能力与验收范围**：正式 Scalar schema、Projection 绑定、默认值、混合数学、缺轨道行为和发布顺序需要设计/spec/tasks 同步。用户目前授权并完成的是离线数据解码，尚不能自动认定全部 Runtime 接入已获实施授权。
