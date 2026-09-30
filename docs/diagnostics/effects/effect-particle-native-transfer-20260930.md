# 特效粒子原生 Transfer 证据

日期：2026-09-30。本文只记录原始资源解析和 `UnityPlayer.dll` 静态反汇编证据，不修改 3C 工程代码，不通过 Unity 写文件，也不宣称可琳 VFX 已经可接入。

## 结论

AssetRipper 与 UnityPy 使用标准 Unity 2019.4 的静态 `ParticleSystem` 布局，无法读取这批 ZZZ 资源。`D:/ZZZ_Dump/output/corin_replication/20260929_effect_candidate_ripper/assetripper.log` 中，AssetRipper 1.3.14 报出 `19,463` 个 `ParticleSystem` 读取错误和 `12,037` 个 `ParticleSystemRenderer` 读取错误。典型调用栈是 `ParticleSystem_2019_2_0_a9.ReadRelease`，错误参数为 `length`；`ParticleSystemRenderer` 日志还出现 `Read 240 but expected 308/312`。

这批资源不是“整个文件坏掉”，而是使用 Unity 原生 Shuriken optimize transfer 的条件序列化，且 `ParticleSystem` 根部带 ZZZ 扩展头。继续用标准 typetree 或 AssetRipper 的完整 `MinMaxCurve`/`MinMaxGradient` 会在空曲线和颜色字段处累计漂移。

## 样本

样本来自 `D:/ZZZ_Dump/output/corin_replication/20260929_effect_candidate_extract_v1/988944888/0425_CAB-d6652fef59830a19de7f25294d180258`，对象 class id 198，`pathId=-9184719907490745092`，对象字节数 `7576`。

UnityPy 的节点表把 `MinMaxCurve` 展开成完整 2018 布局，但直接读取在 `startDelay.maxCurve.m_Curve.size` 处越界。实际样本根部布局为：

1. `m_GameObject`，12 字节。
2. ZZZ 扩展头：`distanceCulling:int`、`cullingFromDistance:int`、`postEmissionScalar:float`、`tickFrequency:float`。
3. `lengthInSec`、`simulationSpeed`。
4. `frequencyMode`、`stopAction`、`cullingMode`、`ringBufferMode`。
5. `ringBufferLoopRange`、六个 bool、对齐。
6. `startDelay` 使用完整 2018 版 `MinMaxCurve`，即保留 `minScalar` 与两条曲线。
7. `moveWithTransform`、`moveWithCustomTransform`、`scalingMode`、`randomSeed`。
8. 后续模块。

按该顺序闭合的关键值是：`lengthInSec=0.1`、`simulationSpeed=1`、`scalingMode=0`、`randomSeed=6582`。`InitialModule` 从 `136` 到 `920`，起始生命为 state 0、scalar `0.22`、minScalar `5`，两条空曲线存在。

## MinMaxCurve

`UnityPlayer.dll` 中优化版 `MinMaxCurve::Transfer` 为 RVA `0x2077D0-0x207A86`。函数内字段名依次出现 `minMaxState`、`scalar`、`minScalar`、`maxCurve`、`minCurve`。

release/读取路径的结构化规则是：

1. `minMaxState` 为 UInt16，写入后按 4 字节对齐。
2. `scalar` 常写。
3. `minScalar` 常写。
4. state `1`/`2` 调用真实 `maxCurve`；其余 state 构造栈上默认曲线后仍调用 `AnimationCurve::Transfer`。
5. state `2` 调用真实 `minCurve`；其余 state 构造栈上默认曲线后仍调用 `AnimationCurve::Transfer`。

函数还在非读写场景触发字符串 `"[MinMaxCurve::Transfer]optimize transfer can only be used in reading or writing"`，可作为函数身份的辅助证据。`0x207E70` 是带 typetree 分支的 `MinMaxCurve` 读取入口，内部同样按 state 1/2 分派两条曲线。

之前把 state 0 简化成只读 `scalar` 是错误规则，会漏掉 `minScalar` 和默认曲线载荷。当前解析器已统一读取两条曲线，但样本批量在 `SizeModule.y/z` 仍暴露出一个未闭合矛盾：该处 UInt16 读到 state 0，`maxCurve` 是空曲线载荷，后续 `minCurve` 却出现 `size=2` 的真实键列表；`InitialModule` 的 state 0 两条曲线都是空曲线载荷。因此还不能把当前工具宣称成正式批量解码器。

## AnimationCurve 断点

`UnityPlayer.dll` 中 `AnimationCurve::Transfer` 为 RVA `0x180670-0x180868`，字段顺序是 `m_Curve`、`m_PreInfinity`、`m_PostInfinity`、`m_RotationOrder`。容器头由 `0xBD2080-0xBD20FD` 处理并读取 `size`；`0x181040-0x18121D` 是 `Keyframe::Transfer`，固定写出 `time`、`value`、`inSlope`、`outSlope`、`weightedMode`、`inWeight`、`outWeight`，有效载荷 `28` 字节。

`AnimationCurve::Transfer` 本体固定读取 `m_Curve.size`，非 `-1` 时读取键数组，随后读取三个 wrap 字段。该函数自身没有看到按 state 跳过 wrap 的分支。旧样本边界把 `-1` 解释成只消耗 4 字节，是在 `MinMaxGradient` 少读 24 字节后的错误补偿，不能再作为正式规则。

## ParticleSystemRenderer

`UnityPlayer.dll` 中 `ParticleSystemRenderer::Transfer` 为 RVA `0x1243F50-0x1244763`。它先走 `Renderer` 基类，再按下列 ZZZ 顺序补入扩展字段：

1. `m_RenderMode`、`m_SortMode`、`m_OrderType`；`m_OrderType` 为 UInt16 并按 4 字节对齐。
2. 标准 `m_MinParticleSize` 到 `m_AllowRoll`。
3. `m_UseOctagonShape`、`m_SkipAutoScalingOpt`、`m_OctagonExpand`、`m_AlphaThresholdParticles`。
4. `m_VertexStreams`。
5. `m_Mesh`、`m_Mesh1`、`m_Mesh2`、`m_Mesh3`、`m_OctagonMesh`。
6. `m_MaskInteraction`、`m_UseCustomBoundingBox`、`m_CustomBoundCenter`、`m_CustomBoundSize`。

`0xBD32D0` 是 serialized-version gate：它比较 TypeTree 的 serialized version 与参数并返回 `version <= threshold`。这批 `ParticleSystemRenderer` TypeTree 的 `m_Version=6`，因此 `8`、`9` gate 成立，`_LodLevel` 和 `_LodMesh` 都不写入；`2` gate 不成立，`_VertexStreamMask` 也不写入。`0x35BA70` 证明 Mesh 字段使用 `PPtr<Mesh>`。按这条 release 路径，样本 CAB 内 20 个 Renderer 全部闭合到 `296`；总长 `308` 的 6 个对象尾部为 `12` 字节，总长 `312` 的 14 个对象尾部为 `16` 字节。之前记录的 `standardEnd=308` 和“剩余 4 字节”是把 version 6 不写的 `_LodMesh` 读进标准段造成的错位。

按该链解析后，样本 20 个对象的 `m_Materials` 均为空。当前 CAB 也不包含 `Material`、`Texture2D`、`Shader` 或 `Mesh` 对象；`m_Mesh`/`m_Mesh1` 是 `fileId=0` 的 local PPtr，但 Mesh 目标不在当前序列化对象表内。`fileId=0` 表示当前 CAB 内部对象；positive `fileId=N` 才映射到 external `N-1`。渲染器输出保留完整 `fileId/pathId`，网格目标必须留给完整 bundle 集合后的组装阶段。

2026-09-30 复查发现上面的 Renderer 结论不能作为正式依赖来源：样本解析出的 Mesh `pathId` 含 `0x01010101` 一类非合法句柄，说明 `standardEnd=296` 前已经漂移。`Renderer::Transfer`（`0x7D1FA0-0x7D2B36`）在标准 2019 TypeTree 之外至少还有 `m_RayTraceProcedural`、`m_NeedHizCulling`、`m_HighShadingRate`、`m_RayTracingLayerMask`、`m_CullingDistance`。这些字段与 TypeTree 的对齐关系尚未闭合；在未闭合前，旧的 `renderer-corin-dependency-scan.json` 只能当失败诊断，不能用于材质/Mesh 组装。

渲染器证据输出是 `sample-renderers.json`，命令使用 `--object-type ParticleSystemRenderer`。`standardEnd` 全部为 `296`；`zzzExtension` 长度分布为 14 个 `16` 字节、6 个 `12` 字节。

## Corin Renderer 范围扫描

正式范围只取 `20260929_corin_effect_assets_decoded_v1/manifest.json` 登记的 `Eff_Corin` 对象，源文件限定在 8 个 mhy1 块内；重复 CAB 先用 `raw_module_manifest.json` 的对象字节数选出版本一致的源副本，再校验目标 raw bytes 和 external 表一致。`350316997_dbg` 不是扫描范围。此前递归 6,568 个 CAB 的 `renderer-dependency-scan.json` 是超范围临时证据，不能作为 Corin 结论。

范围扫描覆盖 `125` 个含 Renderer 的 CAB 和全部 `1,470` 个 `ParticleSystemRenderer`，零解析错误，全部 `standardEnd=296`。对象总长与未命名尾部分布是：`336/40` 字节 `397` 个，`340/44` 字节 `961` 个，`344/48` 字节 `101` 个，`352/56` 字节 `11` 个。尾部数值只确认出零前缀和末尾三个 float32 `1.0`；字段名和语义还未由原生符号闭合。

Renderer 依赖汇总如下：`m_Materials` 全部为空；Mesh 引用共 `4,687` 条，其中 `local` `2,923` 条且目标都不在当前 CAB，`invalidFileId` `1,686` 条不能作为正式依赖消费。`external` 共 `78` 条：`29` 条指向 `Library/unity default resources`，`49` 条指向 archive CAB。archive 引用对应 `15` 个唯一目标，其中 `6` 个已在 8 个源块内命中 `Mesh`，覆盖 `16` 条引用；其余 `9` 个目标、`33` 条引用仍未命中。因此当前只能登记依赖，不能宣称材质、贴图或 Mesh 资源已解出。范围输出是 `renderer-corin-dependency-scan.json`；注释过的 Transfer 关键分支保存在 `renderer-transfer-annotated.json`。

## AssetRipper Prefab 引用扫描

2026-09-30 对 AssetRipper 导出工程中的全部 `298` 份 `Eff_Corin*.prefab` 做了结构化 YAML 引用扫描，输出是 `corin-ripper-prefab-reference-scan.json`，生成器是 `Tools/Rendering/scan_zzz_corin_ripper_prefab_references.py`。扫描按根 `GameObject.m_Name` 对齐，覆盖 `129` 个 formal 根；`89` 个根有 `2` 份导出副本，`40` 个根有 `3` 份导出副本。副本只按导出文件名区分，不因同名合并成正式数据源。

AssetRipper 导出的对象总计：`3,549` 个 GameObject/Transform、`5,322` 个 MonoBehaviour、`1,185` 个 ParticleSystemRenderer、`74` 个 Animation 和 `56` 个 Light；没有导出任何 ParticleSystem、TrailRenderer、Mesh、Material、Texture2D 或 Shader 对象。`5,322` 条 `m_Script` 全部能解析到导出工程中的 `.cs.meta`；除此之外没有一条 GUID 引用能解析到材质、贴图或 Shader 资产。

占位 GUID `0000000deadbeef15deadf00d0000000` 共出现 `8,913` 次：`5,571` 次是 GameObject 组件槽位占位，`3,342` 次是资产引用占位。资产占位分布在 AnimationClip `154` 次、Renderer Mesh `2,406` 次、Renderer 静态合批根 `782` 次。已导出的 ParticleSystemRenderer 中 `m_Materials` 全部为空数组；`m_Mesh` 只有 `1,149` 次空引用、`36` 次占位，`m_Mesh1`/`m_Mesh3` 分别有 `1,185` 次占位。因此 AssetRipper 工程不能作为 Renderer 依赖、材质或贴图的正式来源。

用 `corin-prefab-hierarchy.json` 对账 Renderer 槽位：formal 范围需要 `1,470` 个 ParticleSystemRenderer。每个根的所有 AssetRipper 副本中，最好的导出合计只有 `918` 个；只有 `17/129` 个根能找到完整 Renderer 副本，`112` 个根的最好副本仍缺 Renderer。`291/298` 份副本的“已导出 Renderer + 组件占位”恰好等于 formal 层级的 ParticleSystem + Renderer 总数，另外 `7` 份副本的槽位总数不匹配，说明 AssetRipper 还会丢失或改变组件槽位，不能通过挑选副本修复成正式依赖来源。

## MinMaxGradient

`UnityPlayer.dll` 中优化版 `MinMaxGradient::Transfer` 为 RVA `0x1FB470-0x1FB6E8`。函数内字段名依次出现 `minMaxState`、`minColor`、`maxColor`、`Gradient`、`maxGradient`、`minGradient`。

release/读取路径的结构化规则是：

1. `minMaxState` 为 UInt16，写入后按 4 字节对齐。
2. `minColor` 是四个 float32，共 16 字节；`0x14D850-0x14D9B5` 内的字段名是 `r/g/b/a`。
3. `maxColor` 同样是四个 float32，共 16 字节。
4. `maxGradient` 调用 `Gradient::Transfer`，字段结束后按 4 字节对齐。
5. `minGradient` 调用 `Gradient::Transfer`，字段结束后按 4 字节对齐。

之前把两个颜色解释成 `Color32` 是错误规则，每条 `MinMaxGradient` 少读 24 字节。`Gradient::Transfer` 位于 RVA `0x181E80-0x1822EC`，固定写 8 组四个 float32（128 字节）、8 个 UInt16 `ctime`（16 字节）、8 个 UInt16 `atime`（16 字节）、一个 int mode 和两个 byte count，载荷合计 166 字节；函数内没有额外补齐。第一条和第二条 gradient 外层结束后分别按 4 字节对齐。样本首对象 `InitialModule.startColor` 从相对偏移 `700` 闭合到 `764`，随后 `startSize` 正好从 state 0、scalar `4.32`、minScalar `1.0` 开始。

## 当前样本边界

`EmissionModule::Transfer` 位于 `0x235760` 起的多段函数。除已记录的 ZZZ 扩展字段外，`ParticleSystemEmissionInstance` 最后还传输 `instanceData:TypelessData`：先读 `size:int`，再读 `size` 字节 `data`，最后按 4 字节对齐。对应辅助函数是 `0xBD3840-0xBD3932`。样本首对象 `m_EmissionInstanceCount=0`，因此该修正不改变这份样本的 Emission 边界，但全量扫描必须读取该字段。

`ExternalForcesModule` 在这份样本中使用短布局：`enabled` 对齐、`multiplier:float`、`influenceFilter:int`、`influenceMask:uint`，共 16 字节，不含 influence list。`ClampVelocityModule` 的 version 6 样本路径在 `dampen` 后只写 `drag.maxCurve`，再写 4 字节 `dragMinCurveSize`；这个 4 字节的精确语义还未由符号闭合，但其边界已参与 20 个样本的完整闭合验证。

这段早期 Clamp 判断已被下文 Version 6 Clamp 拖拽载荷一节修正；`drag.maxCurve` 和 `dragMinCurveSize` 不是正式线缆规则。

native 根函数在 `CustomDataModule` 后还有 `TextModule::Transfer`（`0x25F240-0x25F420`）。字段顺序是 `enabled` 对齐、`sceneCamera/canvas/font` 三个 `PPtr`、`fontSize:int`、`fontStyle:int`、`outlineEnable` 对齐、`outlineDistance:Vector3`、`emitWithWorldPosition` 对齐。该模块不在 UnityPy TypeTree 中，但正好解释所有样本尾部的 68 字节。

当前 version 6 样本协议已覆盖 23 个模块：从 `InitialModule` 到 `CustomDataModule`，最后是 `TextModule`。样本 20 个对象全部满足 `decodedEnd == byteSize`，模块边界无重叠；输出是 `systems-batch.json`。

`ExternalForcesModule` 在这份样本中不是标准 2019.2 的 `multiplierCurve + influenceList`。字节模式匹配短布局：`enabled` 对齐、`multiplier:float`、`influenceFilter:int`、`influenceMask:uint`，共 16 字节，不含 influence list。

`RotationBySpeedModule` 的原生 Transfer 已定位为 `0x22FD60-0x22FF79`，其入口辅助函数为 `0x236100-0x236158`。字段顺序确认是 `enabled`、`x`、`y`、`curve`、`separateAxes`、`range`；`MinMaxCurve` 调用 `0x2077D0`。样本 `enabled=false`，`x`/`y` 为 state 2，`curve` 为 state 0。`ColorBySpeedModule` 的原生 Transfer 是 `0x2D7D90-0x2D7EE4`，同样先调用 `enabled` 辅助，再处理 `gradient` 和 `range`。样本该模块 `enabled=true`。

当前解析工具是 `tools/Rendering/decode_zzz_particle_prefix.py`。样本批量 `--object-type ParticleSystem` 已通过；`py_compile` 和 `git diff --check` 也通过。旧的 `system-batch-failure.json` 只保留作历史失败证据。

## Version 6 Clamp 拖拽载荷

旧线缆 `ClampVelocityModule` 在 `dampen` 后不再使用当前 `MinMaxCurve` 布局。对已推进的 `740` 个对象重放 Clamp 前缀后，`drag` 到模块结束全部是 `88` 字节：首 `4` 字节是唯一明确命名的 `drag.scalar`，随后固定 `84` 字节是 version 6 legacy payload。740 个样本中 scalar 有 10 个唯一 float 值，主要是 `1.0`、`0.2`、`0.1`、`0.4`；payload 中多处固定为 `2`、`4` 和零，另有少量曲线相关变化，但当前证据还不足以命名完整内部字段。

当前 `UnityPlayer.dll` 的 `ClampVelocityModule::Transfer` 在 `0x253C70-0x253E97`。它在读取 `dampen` 后调用标准 `MinMaxCurve::Transfer`（`0x2077D0`），再尾调用 `0x25E110-0x25E186`。后者只做运行时曲线求值和钳制，不消费序列化流，因此不能把当前函数的 drag schema 直接套到 version 6 线缆。反汇编证据在 `minmax-curve-tail-25e110/`。

解析器已按证据改为读取 `drag.scalar` 和 `dragLegacyPayload[84]`，删除旧的 state/`minScalar`/`minCurveSize` 错误补偿。用该规则重跑 Corin manifest 全量：`125` 个 CAB、`1,472` 个 `ParticleSystem` 全部满足 `decodedEnd == byteSize`，输出是 `corin-particle-system-legacy-clamp-scan.json`。这条结论闭合的是序列化边界和 `drag.scalar`；84 字节 payload 的字段语义仍未闭合，所以不能宣称所有 drag 运行时语义都已还原。

## Version 6 写端缺口

2026-09-30 递归检查 `D:/ZZZ_Dump/output` 内 `6,568` 个 CAB，得到 `19,463` 个 `ParticleSystem`，TypeTree `serializedVersion` 全部是 `6`，没有更高版本样本可作对照。当前 `UnityPlayer.dll` 的根 `ParticleSystem::Transfer` 位于 `0x205620-0x205DAC`，模块调用顺序包含：Initial/Shape 复合入口 `0x2BC030`、Shape 入口 `0x1265000`、Emission `0x235760`、Size `0x25F110`、Rotation `0x254630`、Color `0x2356C0`、UV `0x25F430`、Velocity `0x2D8340`、InheritVelocity `0x2BBF60`、Force `0x235D60`、ExternalForces `0x2D7EF0`、ClampVelocity `0x253C70`、Noise `0x2BC440`。Emission 和 Size 之间的 `0x247200` 只做运行时钳制，不读写字节。

当前 `UnityPlayer.dll` 是比这批 version 6 资源更新的 Transfer 实现。`ExternalForcesModule` 的当前函数会写 `influenceList`，`ClampVelocityModule.drag` 也存在当前函数与旧线缆的形态差；因此不能把当前 native 字段表直接当作这批资源的唯一 schema。

2026-09-30 复核后，先前的 “SizeModule prefix” 判断要修正：正式 Corin 对象 `CAB-0166b409613d7c97b39040c3abecd2f5` / `-2448053914573643455` 在 Emission 尾部多出 `emissionLevelVeryHigh:float`。该对象的三档值是 `0.3/0.6/0.8`，`emissionLevelVeryHigh=1.0`，随后 `emitCallbackThreshold=0`。把这 4 字节归还 Emission 后，Size 的 `enabled=true` 和三条标准 `MinMaxCurve` 可以正常推进；`1460` 处的 `01 00` 不是额外 prefix，而是 `minMaxState`。

正式线缆的 `SubModule` 在标准字段后有 1 个对齐 bool 扩展；首个对象该值为 `false`，因此模块从 `5884` 闭合到 `5920`。随后 `LightsModule` 从 `5920` 开始，规则是：`enabled` 对齐、`ratio:float`、`light:PPtr<Light>`、`randomDistribution/color/range/intensity` 四个连续 bool 后一次对齐、`rangeCurve:MinMaxCurve`、`intensityCurve:MinMaxCurve`、`maxLights:int`。两条曲线都调用完整 `MinMaxCurve`；`0x236970` 的原生 Lights 函数两次调用 `0x2077D0`，第一条读取链和字段名支持该顺序。首个对象 Lights 从 `5920` 闭合到 `6036`，值为 `ratio=1.0`、light 为空、四个控制 bool 全 true、`maxLights=20`。

`TrailModule` 的边界已由首个对象闭合为 `6036..6904`，载荷使用标准 TypeTree 字段顺序和当前优化 `MinMaxCurve`/`MinMaxGradient` 规则；不需要为首个对象另造 Trail schema。首个 formal 对象最终满足 `decodedEnd=8088=byteSize`，`TextModule` 结束于 `8020..8088`。

此前全量扫描在 `740/1472` 停止，是因为旧解析把 Clamp drag 的 float 低 16 位读成非法 `minMaxState`。该错误已由 Version 6 Clamp 拖拽载荷一节修正；当前 formal 全量已经闭合到 `1472/1472`。当前机器和 `D:/ZZZ_Dump` 内仍未找到这批 version 6 资源对应的旧 `UnityPlayer.dll`，所以 84 字节 legacy payload 的内部字段名仍缺原生符号证据。

`ParticleSystemRenderer` 的旧 TypeTree 结论已被 formal 线缆取代。标准 2019.2 TypeTree 读到 `240` 只是错误边界；另一个样本在 `m_VertexStreams` 越界说明插入字段更早。按 1,470 个对象重放后，version 6 formal 顺序闭合为：标准 Renderer Base 读到 `m_SortingOrder`；随后 `m_NeedHizCulling` 对齐 bool、`m_HighShadingRate` 对齐 bool、`m_RayTracingLayerMask` 对齐 UInt16、`m_CullingDistance` float；接着 `m_OrderType` 对齐 UInt16、一个当前全量为 0 的 4 字节 ZZZ 字段、`m_RenderMode` 与 `m_SortMode` 两个对齐 UInt16；然后 8 个 float 是 `MinParticleSize` 到 `ShadowBias`，再读 `m_RenderAlignment`、`m_Pivot`、`m_Flip`。四个 vertex-stream bool 后直接读 `m_VertexStreams` 的 `size:int + data + align`，不再是 TypeTree 误解的巨大 vector。随后是 `m_Mesh`、`m_Mesh1`、`m_Mesh2`、`m_Mesh3`、`m_OctagonMesh` 五个 PPtr、`m_MaskInteraction`、`m_UseCustomBoundingBox`、`m_CustomBoundCenter`、一段全零且长度随对象的 ZZZ suffix，最后固定 `m_CustomBoundSize` 三个 float。

用该协议全量扫描：`125` 个 CAB、`1,470` 个 Renderer 全部满足 `decodedEnd == byteSize`；对象长度仍为 `336/340/344/352`。`m_CustomBoundSize` 全部是 `(1,1,1)`，`m_CustomBoundCenter` 全部是 `(0,0,0)`，中间 suffix 全部为零。`m_RenderMode` 分布是 `0/1/2/4/6`，`m_SortMode` 主要是 `0`，`m_OrderType` 有 `1,459` 个 `1` 和 `11` 个 `0`，`m_VertexStreams` 有 `1,192` 个 size1 和 `278` 个 size0。输出是 `corin-renderer-formal-scan.json`。

Renderer 依赖仍不能直接组装：`m_Materials` 全部为空；Mesh 引用共 `2,546` 条，其中 `1,481` 条 fileID 为 0 但目标不在当前 CAB，`996` 条 ZZZ 大 fileID 不能按 Unity external index 消费，69 条 archive external 去重后是 13 个 `(CAB, pathId)`。

2026-09-30 继续反查本机正式游戏块后，13 个 archive Mesh 目标已经全部命中。正式追踪输出 `corin-renderer-external-dependency-trace.json` 的结果是 `13/13`，生成器是 `Tools/Rendering/trace_zzz_corin_renderer_external_dependencies.py`；AssetMap 输入在 `D:/ZZZ_Dump/work/corin_cab_map_v1/asset_maps`。命中的 `Sphere001`、`Sphere002`、`Eff_Cone_01_Sp`、`Eff_Corin_Saw_03`、`Eff_Smoke_Foot_alpha_L/R_zy_001`、`Eff_Circle_LYX_003`、`Eff_Trail_Cone_tt_066`、`Eff_Sweep_zy_001`、`Eff_EjectCylinder_01/02` 和两份 `Eff_Trail_Cone_04` 已导出 OBJ 到 `D:/ZZZ_Dump/work/corin_render_dependencies_v1/Mesh`。Mesh 引用不再是组装缺口；材质和贴图仍未闭合。

Prefab 层级已从已解出的 GameObject/Transform JSON 组装成正式中间数据：`Tools/Rendering/CorinRenderData/corin-prefab-hierarchy.json`。生成器是 `Tools/Rendering/build_zzz_corin_prefab_hierarchy.py`，输入 `20260929_corin_effect_assets_decoded_v1/manifest.json`，输出覆盖 `129` 个 `Eff_Corin` 根、`1550` 个 GameObject/Transform 节点；组件引用按 manifest 类型标注为 `2307` 个 MonoBehaviour、`1472` 个 ParticleSystem、`1470` 个 ParticleSystemRenderer、`33` 个 Animation、`26` 个 Light、`6` 个 TrailRenderer。全部 Transform 父子链接闭合，`unresolvedLinkCount=0`。29 个 MonoScript 都是 `Unity.RenderPipelines.Universal.Runtime.dll` 的 `VfxVolumetricShadow`，Prefab 只表达自身层级，不携带 3C 角色或技能的父级挂接。层级可消费；ParticleSystem 与 Renderer 的序列化数值已经全量闭合，但 Clamp drag payload 内部语义、材质、贴图和运行时触发入口仍限制直接接入。

## Preload Material/Texture 导出

2026-09-30 的 Material/Texture 解包没有安装或运行 Unity 2019，也没有运行 Unity batchmode。正式路径是 `Tools/Rendering/ZZZEffectDependencyExporter` 通过 AnimeStudio 的 Mhy 解码器读取已提取 CAB；AnimeStudio 只在 `D:/ZZZ_Dump/tools_dl/AnimeStudio` 中作为解析库使用。为了保留当前 ZZZ Material 的完整结构化字段，`AnimeStudio/Classes/Material.cs` 把原先丢弃的 `m_ShaderKeywords`、lightmap flags、instancing 开关、render queue、string tag map、disabled passes 和 `m_EnabledPassMask` 保留为公开解析结果。

导出器读取 AssetBundle preload，对当前解析范围启用 AssetBundle、Material、Texture2D 和 Shader。`v6` 保留为失败证据：100 份 Material 的 raw bytes 已写出，但 77 份因 stripped Material 在 Unity 2021 前没有 `m_Ints` 集合而触发空集合错误，23 份因 Texture 的 `CAB-*.resS` 目标带数字前缀而找不到流。`v7` 资源解析已推进到报告序列化，但 Material float 中存在非有限值，默认 JSON 拒绝写出。这些目录都不覆盖。

候选提取集先闭合到 `v10`：`129` 个根、`6,481` 个加载 CAB、`100` 个 Material、`16` 张 Texture raw bytes。随后用全量游戏块索引反查候选外目标。生成器是 `Tools/Rendering/trace_zzz_effect_texture_global_targets.py`，它只把 `Texture2D + PathID` 精确相等作为候选证据；导出器仍按目标 `CAB + PathID` 实际解析命中后才会写出。索引覆盖 `zzz_index_v2` 的 `3,800` 个块和 `zzz_index_v3` 补充的 `6,427` 个块，合计 `10,227/10,227` 个正式游戏块；中断的 `job_0017` 原始截断 map 保留，另用 `zzz_index_retry.json` 重跑。

最终 `v34` 输出在 `D:/ZZZ_Dump/output/corin_replication/20260930_effect_material_texture_export_v34`，报告是 `effect-material-texture-export.json`，schema 为 `zzz-effect-material-texture-export/2`。结果为 `129` 个根、`1,249` 条 preload 引用、`419` 个唯一依赖、`208` 个 Material、`180` 张 Texture raw bytes、`15` 个 Material 依赖 Shader raw objects、`0` 个解析错误。Texture 使用 `m_StreamData` 提取目标 CAB；候选提取集内定位同名但带数字前缀的 `NNNN_CAB-*.resS`，补充源块走 AnimeStudio 已缓存的资源流。记录保留 stream offset/path、资源文件名、尺寸、format、mip/filter/wrap 元数据、字节数和 SHA-256。

Material 记录保留 source CAB/pathId、raw bytes SHA-256、Shader PPtr、int/float/color/texture 属性、shader keywords、render queue、disabled passes 和 enabled pass mask。当前版本没有 `m_Ints` 集合，所以结构化 `Ints` 为空集；这不是解析错误。原 Material typetree dump 依赖 typetree，在 stripped/optimized 资源上只能得到 0 字节文件，因此 v10 起删除这条无效输出，raw object bytes 和结构化 JSON 是正式数据源。

Material/Texture/Shader 引用已经在序列化依赖层闭合。v34 的 `EmptyTextureEnvCount/ResolvedTextureEnvCount/MissingTextureEnvCount` 是 `1,767/465/0`；也就是所有非空 texture 槽位都解析到真实 Texture2D 并写出 raw bytes。208 个 Material 指向 `15` 个唯一 Shader，全部按目标 `CAB + PathID` 解析成功并导出 compiled Shader object；其中主溶解粒子 shader 是 `miHoYo/Particles/Particles_Dissolve_CustomColor_Mask`，raw bytes 为 `51,831,664`。文件级复查确认 `208/208` 个 Material raw 文件、`180/180` 个 Texture bytes 文件存在且非空。AnimeStudio 输出的是 raw serialized Shader object，不是可直接拖入 Unity 的 `.shader` 源文件；组装/导入仍需后续转换。

## 剩余范围

`ParticleSystem` 和 `ParticleSystemRenderer` 序列化边界已全量闭合，Renderer 的 13 个 archive Mesh 目标也已命中并导出 OBJ，208 个 Material 的 raw bytes 和结构化属性已导出，465 条非空 Material 贴图引用全部闭合到 180 张 Texture raw bytes，15 个 Material Shader 也已导出 raw serialized objects。剩余不确定性集中在 Clamp drag 84 字节 legacy payload、Renderer 少量未命名 ZZZ 字段的业务语义、raw Shader 到 Unity `.shader`/compiled asset 的转换，以及 Material/Texture/Mesh 组装成 Unity 资产；3C 侧还缺少把 `Eff_Corin_*` 根接到角色挂点和 Timeline 的正式运行时入口。

## Unity 组装管线

2026-09-30 搭建了从 v34 解码数据到 Unity 资产的正式组装管线。raw serialized Shader 跨版本不能直接导入，采用属性名对齐的 proxy shader 方案先闭合导入路径。

15 个 proxy shader 源文件在 `Assets/Render/ZZZRestored/Generated/Effect/`，shader 声明名与 `miHoYo/Particles/*` 一致，`Shader.Find` 可按名命中。属性声明从 v34 报告全量提取：主 shader `Particles_Dissolve_CustomColor_Mask` 覆盖 417 个 Float、65 个 Color、7 个 Texture；其余 14 个 shader 按各自 Material 提取。inf 值替换为 `1e+38` 避免编译错误。shader 体内渲染逻辑当前为 URP Unlit 占位，后续逐个替换为正确实现。

`CorinEffectAssetImporter.Rebuild()` 读取 v34 报告，生成 180 个 Texture2D（`LoadRawTextureData` + 元数据）和 208 个 Material（shaderKeywords、renderQueue、float/color/texture 属性、disabled passes），输出到 `Assets/AssetArt/Effect/ZZZ/Corin/`。`CorinEffectMaterialSet` 按 `CAB:PathId` 建立查找。

`CorinEffectPrefabAssembler.Rebuild()` 读取 `corin-prefab-hierarchy.json` 构建 129 个 Prefab，按节点 `CAB:pathId` 匹配 `corin-particle-system-unity.json`（1472 个 ParticleSystem，覆盖 main/emission/shape/colorOverLifetime/sizeOverLifetime/rotationOverLifetime/velocityOverLifetime 七模块的 MinMaxCurve/MinMaxGradient）和 `corin-particle-renderer-unity.json`（1470 个 Renderer 属性：renderMode/sortMode/normalDirection/pivot/flip/allowRoll/lengthScale/velocityScale 等）。69 个 archive mesh 引用按 `corin-renderer-mesh-map.json` 绑定 12 个 OBJ 导入的 Mesh；718 个 Renderer 材质按 `corin-renderer-mesh-map.json` 查 MaterialSet 绑定。

在 Unity Editor 中按顺序执行两个 MenuItem 即可产出完整资源：`Tools > ZZZ > Restored > Rebuild Corin Effect Assets`，然后 `Tools > ZZZ > Restored > Rebuild Corin Effect Prefab Hierarchy`。

当前限制：proxy shader 渲染逻辑不是正确视觉（后续替换）；277 个 Renderer mesh 引用中的非 archive local 引用无法解析（已有数据）；Clamp drag 84 字节 legacy payload 语义仍未闭合。
