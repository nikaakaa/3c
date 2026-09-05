# ZZZ 可琳角色渲染静态合同

## 当前可实施范围

原 `NapEntityPrepare` D3D11 计算程序已经从磁盘 ComputeShader 的 DXBC 恢复为具名源码，并由 D3DCompile 重新编译通过。Unity 已导入当前源码包装，Editor 日志没有该 ComputeShader 的编译错误。当前工程另有 Opsive GraphDesigner ECS 包缺少 Unity.Entities 的既有 C# 错误，因此不能把“ComputeShader 导入成功”写成整项目编译成功。

生成资产为 `Assets/Render/ZZZRestored/Generated/EntityLighting/NapEntityPrepare.compute`。它尚未绑定 RendererFeature 或角色，不会改变当前社区材质画面。

原 NapAvatarStandard 的十个已恢复阶段也已生成到 `Assets/Render/ZZZRestored/Generated/Character/`，组成 ShadowCaster、CharacterOutlineDeferred、CharacterToonDeferred、CharDepthOnly 四个原 Pass。Body 的 `_MATCAP_ON` 选择 entry65/2705，Hair 选择 entry60/2700。第一次导入暴露 ShaderLab 常量状态不能直接写数值的问题；Cull、ZTest、ZWrite、Blend、Stencil 常量已改成当前 Unity 的正式枚举名字，第二次导入没有 Shader 解析错误。

该 Shader 尚未绘制。当前项目编译同时存在 `Float32BlackboardRuntime.cs` 与 `FixedBlackboardRuntime.cs` 把 `CharacterSkillId` 传给 string 参数的两项既有错误，阻断新增运行时绑定模块和完整 CompileVariant 验证。本工作不修改这两处并行 Gameplay 代码。

## 计算链

磁盘 `NapEntityPrepare.json` 的 D3D11 变体直接发布三个 Kernel。`NapEntityPrepareDev` 和 `NapEntityPrepareDev2` 的恢复源码哈希完全一致；`NapEntityPrepareDev3` 是独立程序。

GameAssembly `0x1BC70740` 选择 Kernel。829 管线实例的 `UniversalRenderPipelineAsset.m_OptimizeBlendLightOb0103`（字段偏移 `0x2C8`）为 false，因此选择 Kernel 1，即 `NapEntityPrepareDev2`。默认 Kernel 与它相同，不需要猜二者差异。该实例的 `m_IsOptimizeMatcapArrayCreation`（`0x2CD`）同样为 false，MatCap 采用 Legacy 构建分支。

ComputeShader 原资源表证明输入和输出：

- `_LightDatasForChar`
- `_CharacterGIPositionBuffer`
- `_CharacterGIColorBuffer`
- `_CharacterGIIndexBuffer`
- `_RenderEntityPrepareInput`
- `_CharacterBlendLightIndices`
- `_MainLightShadowmapTexture`
- `_CloudShadow`
- 输出 `_NapEntityGPUData`

`NapLightData` 字段布局已经由 type5947 闭合。去掉托管对象头后每元素为 360 字节：世界位置、层、方向、颜色、WorldToLight、角色颜色、衰减、阴影索引、灯类型、toon 参数、混合权重、锁角度、优先级、版本、模式、实例 ID、粗糙度、两个遮挡矩阵、角色 Rim 缩放和胶囊位置。不是未知 360 字节块。

`NapRenderEntityPrepareInputCustom` 为 208 字节；`CSharpNapRenderEntityGPUData` 为 128 字节。主体 Vertex/Pixel 实际消费输出中的：

- `0x00..0x0F`：主光颜色与半径
- `0x10..0x1F`：主光位置和云影
- `0x20`：主光权重
- `0x1C`：云影值
- `0x34`：朝向因子
- `0x60..0x6F`：皮肤环境光
- `0x70..0x7F`：其它材质环境光

主 Shader 不要求把全部计算结果重新命名后才可接入，但完整 128 字节结构继续保留，避免产生第二条简化数据链。

## 快照配置与当前场景

新增全局参数读取把 `NapEntityPrepare` 的需求并入原属性表。829 快照直接读到：

- `_NapCharacterGIEnabled=0`
- `_AvatarMainLightPosition=(0,0,1,0)`
- `_AvatarMainLightColor=(0,0,0,0)`
- `_RenderedEntityCount=2`
- `_IsBlackCanvasOn=0`
- 六个 `_FrustumePlanes`
- `_CloudShadowMoveSpeed` 和 `_CloudShadowRotateMatrix`

灯数量、灯列表、剔除距离等值未发布在全局 PropertySheet；它们属于渲染命令准备数据，不能按缺失即零处理。

GameplayLab 当前只有一盏无阴影 Directional Light，没有 Point/Spot Light。因而可以先通过原程序的无 GI、无附加灯、无阴影正式分支建立基础绘制，不需要为当前场景制造假的灯或 GI 数据。以后增加附加灯时仍走同一个 360 字节输入和同一 ComputeShader，不建立兼容路径。

## MatCap Legacy

Legacy `0x1C987400` 仍按有效纹理集合创建 Texture2DArray，深度取集合数量；尺寸、格式、mip、filter、aniso 均从首张有效纹理取得。mip 数公式与 Simplify 相同。`0x1C989710–760` 和 `0x1C9897F0–840` 的循环均以源 element 0、源 mip j、目标 element 为集合索引、目标 mip j 调用 `Graphics.CopyTexture_Slice`。

可琳 Body 的五个 MatCap 槽均引用同一个 `Eff_MatCap_019`，所以该角色独立集合的有效结果是一层，五个缓存层号都指向同一层。它不授权把所有角色共用一个硬编码层号；层号仍由实体材质集合产生。

## 材质身份

原资源只找到两份角色材质：`MAT_Corin_Body` 与 `MAT_Corin_Hair`。Body 的 2048 图集本身包含身体、脸和武器；Hair 使用独立图集。当前工程为导入模型拆成五个 SkinnedMeshRenderer：

- `Corin_body`、`Corin_body_02` 当前用 Body 工程材质
- `Corin_face` 当前用独立 Face 工程材质
- `Corin_hair` 当前用 Hair 工程材质
- `Corin_Weapon` 当前用独立 Weapon 工程材质

恢复版应保持五个 Renderer，不合并网格。Body、body_02、Face、Weapon 采用原 Body 参数语义，各自绑定当前分片对应贴图；Hair 采用原 Hair 参数语义。这样适配当前网格/UV结构，同时只保留 Body/Hair 两套原参数所有者。尚未用原 FBX 子网格数据证明当前 Face/Weapon UV 与原 Body 图集可直接互换，因此不能直接把一张 Body 图集挂到五个 Renderer 上。

Hair 原来缺失的 `_OtherDataTex2` 已由 fileID 5 追到 PathID `-7419615475345295008`。其外部节点是 `CAB-da8486c3ca99c5fad588232519e4e64f`，完整扫描在 `2015138260.blk` 找到唯一同名资源节点；导出对象名为 `Corin_Hair_A`，512×512、BC7、10 mip，原 `.resS` 为 349,552 字节。工程 PNG SHA-256 为 `332061430bb030459e0ee574574a51bd33283622a6762a7aa5f143212c863797`。Hair 的四张静态输入贴图现已齐全。

## 新物理快照

输入文件 `D:/ZZZ_Dump/output/ZZZ_20260905_101643.raw` 为 36,507,222,016 字节。定点指针扫描没有访问运行中进程。

原生 Renderer 指针 `0x48E13263000` 有两处物理引用。其中 `PA 0x1AE430700` 满足托管 Renderer 包装布局：

- `+0x00 = 0x500021755E8`，已知 SkinnedMeshRenderer klass
- `+0x08 = 0`
- `+0x10 = 0x48E13263000`，原生对象指针

第二处 `PA 0x626F820B8` 的前 16 字节不是该 klass/monitor 布局，只能认作其它引用。两个 PathID 数值 `0x200000000` 与 `0x400000000` 在前 2GB 已各出现至少 40 次，单值不能证明材质槽身份。

原生对象地址属于虚拟地址。当前 dump 尚无本 boot 的 CR3，无法把 `0x48E13263000` 翻译到承载原生 Renderer 的物理页，因而还不能从该对象继续读取 sharedMaterials、Material、cooked PropertySheet、MaterialPropertyBlock 和最终 NapEntity 索引。

达到同帧可琳闭环还需以下任一最小补充：

1. 本 dump 的 CR3；或
2. 原生 Renderer `0x48E13263000` 所在物理页偏移；或
3. 已解析的 Renderer→sharedMaterials→Material→cooked PropertySheet 链，保留每个虚拟地址及对应物理来源。

取得其中一项后，下一步定点读取原生 Renderer、两份实际 Material、NapCB 的 `_PackedParams0/_PackedParams1/_EntityInfo`，再用实体索引读取对应的 128 字节 `_NapEntityGPUData`。不需要再次采整机内存，也不需要运行时注入。
