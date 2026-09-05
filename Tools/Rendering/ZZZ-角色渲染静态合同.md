# ZZZ 可琳角色渲染恢复合同

统一数据入口见 [可琳渲染数据总览](CorinRenderData/README.md)。本文件保留恢复链说明，快照记录归属和精确数值以整理表及其原始来源为准。

## 已闭合的原始资源

当前恢复不再依赖 Unity 2019 预编译 Shader 资产。那条路径会让 Unity 2022 在绘制时崩溃，已经隔离在 `Tools/Rendering/Archive/Unity2019Precompiled/`。

原资源通过 ZZZ 当前磁盘块的 mhy1 解密链恢复：

- Body：`MAT_Corin_Body` + `miHoYo/Character/NapAvatarStandard`
- Face：`MAT_Corin_Face` + `miHoYo/Character/NapAvatarStandardFace`
- Eye：`MAT_Corin_Eye` + `miHoYo/Character/NapAvatarStandardEye`
- Hair：`MAT_Corin_Hair` + `miHoYo/Character/NapAvatarStandard`
- HairShadow：`MAT_HairShadow` + `miHoYo/Character/NapStencilShadowCaster`
- Weapon：`MAT_Corin_Weapon` + `miHoYo/Character/NapAvatarStandard`

17 张原纹理的 GPU mip 数据已经按原格式导出并在 Unity 生成 `Texture2D` 资产。覆盖 BC7、DXT1、RGB24、RGBA32、RGBAHalf，保留宽高、mip、颜色空间、过滤、各向异性和 Wrap。PNG 只作预览；`ZZZ导出/Raw/*.bytes` 是正式来源。第 17 张为引擎全局纹理 `CharacterOverlayTex`，来自 `globalgamemanagers.assets`，不是 mhy1 块中的可琳材质贴图槽。

MatCap 使用原 Legacy 规则生成一层、五 mip 的 `Texture2DArray`。可琳 Body 五个 MatCap 槽都引用 `Eff_MatCap_019`，四组五元素向量数组继续作为一套数据传给原 Shader。

## 原模型与子网格

可琳主模型在 `1291803240.blk`。ZZZ 将 Mesh 作为 `SeparateMesh_*` 独立存储：

- `3203346160.blk`：Body、Body02、Face、Weapon、HairShadow 及部分 LOD
- `3243101009.blk`：Hair 和 Face/Hair LOD

`Tools/Rendering/ZZZCorinModelExporter` 会把模型、Mesh 和六份材质放进同一 AnimeStudio 上下文，避免未解析材质把 Face 两个子网格合并。正式 LOD0 结果：

- Body：15908 顶点，1 SubMesh
- Body02：1717 顶点，1 SubMesh
- Face：2401 顶点，2 SubMesh，顺序为 Face、Eye
- Hair：2394 顶点，1 SubMesh
- HairShadow：581 顶点，1 SubMesh
- Weapon：4336 顶点，1 SubMesh

所有 LOD0 Mesh 都保留 Normal、Tangent、Color；按源数据保留 UV0～UV3。Unity 导入比例必须为 100。原 FBX 和独立 Prefab 位于：

- `Assets/AssetArt/Model/ZZZ/可琳/Original/Avatar_Female_Size01_Corin_Model_ZZZ_Original.fbx`
- `Assets/AssetArt/Model/ZZZ/可琳/Original/Corin_ZZZ_Original.prefab`

现有社区材质与旧可琳模型没有删除；原版资产独立保存。

## Shader 和绘制顺序

原 D3D11 DXBC 经过位值翻译、具名绑定和 D3DCompile 对账后生成 Unity 2022 Shader。当前项目调度顺序为：

1. Body/Hair/Weapon：`CharacterToonDeferred`
2. Face：`FaceToonDeferred`
3. 原 `CharacterOutlineDeferred` / `FaceOutlineDeferred`，写入同一组角色 GBuffer
4. HairShadow：`StencilShadowCaster`，读取角色 stencil 128 并写入 bit 4
5. Face：`FaceToonDeferredWithStencilShadow`，只重画 stencil 132 区域
6. 四路角色输出：GBuffer0、GBuffer1、GBuffer2、CameraNormal
7. 当前 Camera Depth 复制为单采样深度
8. `DeferredShadingForCharacter`
9. `CharacterPostProcess`
10. Eye：`CharacterOpaqueEye` 输出最终颜色，不再混入角色 GBuffer；该顺序在项目实际绘制中恢复了虹膜。原游戏的全部调度分支仍未逐项闭合。

2026-09-06 原描边已经通过同参数开关对照。精确地址和尚未闭合的 stencil 144/128 优先级见 [原描边接入与验收](ZZZ-原描边接入与验收-20260906.md)。描边显示不等于全套深度/模板规则已恢复。

`CorinRestoredRendererFeature` 已按这个顺序接入 HighFidelity、Balanced、Performant 和 RockyDesert 四套 URP RendererData。原角色 LUT 为 `CorinOriginalCharacterLut.asset`。

## 已取得的运行数据与边界

完整物理快照为 `D:/ZZZ_Dump/output/ZZZ_20260905_101643.raw`，大小 36,507,222,016 字节。当前 boot 的 CR3 为 `0x62DF6A000`。全部读取为离线物理内存和页表翻译，没有读取运行中进程。

已确认可琳 `NapRenderEntity=0x7005F028640`，有 7 个运行时 NapRenderer。`NapRenderEntityManager=0x70051B12800`，字段链为：

- `+0x50 entityGpuDataBuffer -> 0x7005EC6ECE0`
- `+0x60 prepareInput -> 0x70052075840`
- `_RenderedEntityCount=2`
- `prepareInput` 有两条 208 字节记录

`NapRenderEntityPrepareInputCustom` 的 208 字节布局已闭合：Position、FaceForward、FacePosition、灯索引、方向光尺寸、WorldToObject、CameraPosition、覆盖色和时间历史字段。两条准备记录都有 `directionalLightSize=50`，但对应实体均未取得；第一条有正常形式的方向与矩阵，不足以指定为可琳，第二条还包含 -1 标记。不能把这些准备记录与已识别的可琳对象混为同一条数据。

`CSharpNapRenderEntityGPUData` 为 128 字节：

- `0x00` 主光颜色
- `0x0C` 主光半径
- `0x10` 主光位置
- `0x1C` 云影
- `0x20` 主光权重
- `0x24` 阴影覆盖
- `0x28` 附加灯索引/角度
- `0x30` 附加灯衰减和朝向量
- `0x40` 穿线与历史角度
- `0x50` 原始灯位置
- `0x60` 皮肤环境光
- `0x70` 其它材质环境光

GPU Buffer 的原生对象存在且页面可读，但 D3D11 Buffer 内容没有 CPU 镜像，不能从物理快照把 GPU 输出字节冒充为已读。项目因此直接运行恢复后的原 `NapEntityPrepare.compute`：每个 `CorinRestoredRenderEntity` 发布当前 Position、FaceForward、FacePosition、WorldToObject 和实体索引，由同一 Compute 生成 128 字节 `_NapEntityGPUData`，主体/Face/Eye Shader 共同消费。

Unity 2022 的实际 GPU 读回已经完成。隔离可琳首条 128 字节结果中，主光颜色为 `(1,1,1)`、半径为 `50`、主光权重为 `1`、阴影覆盖为 `1`，两组环境光 RGB 都是 `(0.2,0.16,0.16)`；主光位置和朝向量均为 finite。项目实体还会逐帧把包围盒中心发布为 `_MiddlePointPosition`，并把头骨世界到局部矩阵的前三行发布给 Face Shader。

## 已否定路径与运行验收

把 `_RenderedEntityCount` 设为 0 不是有效降级。原像素程序随后会执行实体主光距离平方除以半径平方；零实体分支把半径留为 0，产生 Inf/NaN。实际隔离绘制的 GBuffer0、GBuffer1、GBuffer2、Normal 全黑，角色后处理按原逻辑将非法值清为黑色。

因此不能用常量环境光、社区 Shader 或单路 Forward Shader 代替实体 GPU 数据。当前唯一链是 `CorinRestoredRenderEntity -> NapEntityPrepare.compute -> _NapEntityGPUData -> 原四路 GBuffer -> 原 Deferred -> 原后处理`。

隔离相机已经确认原实例正常输出 Body、Face、Eye、Hair 和 Weapon 的颜色。此前纯黑的直接原因是恢复 Shader 遗漏了动态渲染状态属性：`_CharacterStencil=128` 未进入材质，且 Opaque GBuffer0 使用了未声明的 `_BlendSrcFactor/_BlendDstFactor`，实际退化为 `Blend Zero Zero`。当前主 Shader 明确使用 Opaque 的 `One/Zero`，材质重建后保留 stencil 128。

HairShadow 已按原顺序接入。开关 `Corin_HairShadow` 的同相机 A/B 中，差异只出现在脸部 `568,318-615,372`，94 个像素发生变化，最大通道差 7，确认它只产生发丝投影遮罩，没有覆盖主体颜色。

当前独立 `Corin_ZZZ_Original.prefab` 可以绘制。Eye、MatCap 重新加载、全局 Overlay 绑定均已取得项目实际画面；新版 9 参数 Initialize 与纹理引用也已核验，详见 [Overlay 漏绑诊断与修复状态](ZZZ-衣服发黑-Overlay漏绑诊断-20260905.md)。完整原版描边、全部光照生产条件和调度分支仍不能宣称闭合。把这套六 Renderer 与原模型接到正式 Gameplay 可琳骨架仍是独立迁移工作；旧 Face 子网格和 HairShadow 几何不完整，不能只替换旧模型的几张材质。

## 关键证据

- 模型依赖定位：`D:/ZZZ_Dump/output/corin_replication/20260905_corin_model_mesh_locator_v1/block-scan.json`
- 完整模型导出：`D:/ZZZ_Dump/output/corin_replication/20260905_corin_original_model_complete_v3/model-export.json`
- 材质与纹理：`D:/ZZZ_Dump/output/corin_replication/20260905_corin_material_complete_v1`
- 原纹理字节：`D:/ZZZ_Dump/output/corin_replication/20260905_corin_texture_raw_v1`
- 可琳运行实体：`D:/ZZZ_Dump/output/corin_replication/20260905_render_graph_v1/render-graph.json`
- Entity Manager：`D:/ZZZ_Dump/output/corin_replication/20260905_entity_manager_resident_v1/resident-objects.json`
- PrepareInput：`D:/ZZZ_Dump/output/corin_replication/20260905_entity_gpu_input_snapshot_v3/entity-gpu-input.json`
