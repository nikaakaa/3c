# 可琳衣服发黑：Overlay 漏绑诊断

## 后续修复状态

2026-09-05 23:47 已取得修复后的实际画面：`Diagnostics/Rendering/ZZZRestored/20260905-original-verification/Corin_Original_Overlay_Bound.png`。同轮 audit `20260905-154740-6090375-rendering-audit.json` 确认 Editor 加载的 Initialize 参数数为 9，`profileTextureInputs._CharacterOverlayTex` 为原 256×256 纹理；一份角色实体、一个强度 1 的白色方向光。主体深色衣服不再被统一截为深蓝，原纹理与参数未被人为调亮。Runtime/Editor 程序集实际更新时间均为 2026-09-05 23:42:28。原编译阻塞不再是当前渲染差异的原因，但整个原版外观仍未宣称等价，剩余项见 [课件对账与剩余差异](ZZZ-课件对账与剩余差异-20260906.md)。

### 历史实施与编译阻塞

原纹理已从 `globalgamemanagers.assets` 定向导出并通过原有 importer 生成 Unity 资产；`CorinRestoredRenderProfile` 已增加必需的纹理引用，在角色绘制前统一发布 `_CharacterOverlayTex`，原材质开关保持不变。Runtime 使用 Unity 自身编译器与既有 Bee response files 单独编译通过，未将独立编译结果加载进 Editor。

完整 Unity 加载和实际绘制尚未通过。原阻塞为 `OperationStateMachineRuntime.cs.meta` 中 GUID `3e7a1c9b5d4f4280a6c2b8d1f5a9347` 只有 31 位；对应代码虽然在磁盘存在，AssetDatabase 返回 Unknown/空 GUID，Bee 的 `ThirdPersonSimulation.Core.rsp` 未包含该脚本，因而报 `IOperationStateMachineHost` 缺失。用户明确授权后，仅将 GUID 修正为未冲突的 `3e7a1c9b5d4f4280a6c2b8d1f5a93470`；重新导入后已成为 MonoScript，Bee 输入已收录，原错误消失，Simulation 代码未修改。

编译继续后剩余 4 个 Editor 错误：`CharacterSemanticEmitter.cs:215` 的 `BaseGraph → BaseTree` 类型不匹配，以及 `AgentAuthoringTargetMapper.cs:48/182/318` 的 `AgentSkillDocumentMapper` 未解析。后者脚本本身已在 Bee 输入中，不是同一 GUID/漏导入问题。未扩大本次授权修改它们；不能把磁盘上的新渲染源码当作 Editor 已加载版本。

### 原纹理数据

- 序列化对象：`D:/ZZZ_Dump/output/corin_replication/20260905_character_overlay_serialized_v1/Texture2D/CharacterOverlayTex.dat`。
- 原像素流：`globalgamemanagers.assets.resS`，偏移 6384804，长度 32768。
- 256×256，DXT1，单 mip，线性颜色空间，Bilinear、Repeat、Aniso=1。
- 原像素 SHA-256：`8296882a6cefda60cd2be8ec189d020cec29c33c99eac511a0b19b21d50e389c`。
- Unity `Textures/Original/CharacterOverlayTex.asset` 的 `_typelessdata` 与上述 32768 字节完全一致，差异 0；不是 PNG 重压缩或人工灰图。
- 纹理通过同一 `Raw/texture-raw-export.json → Rebuild Original Corin Materials → Textures/Original` 链生成，资源记录总数由 16 增为 17。
- 复跑工具 `export_zzz_serialized_texture_stream.py` 复用 `inspect_zzz_texture_serialization.parse_texture`，按正式流偏移提取并记录源文件、序列化对象及像素哈希。后者新增原格式 10=DXT1。

### 原版发布入口闭合

- `EngineResources.Initialize` 的 `GameAssembly+0x1E246195` 从 ForwardRendererData `+0x48` 读取原纹理，`0x1E246199` 存入静态表 `+0x211E0`。
- `ForwardRenderer.Setup`：`0x1C41D905` 读取属性 ID，`0x1C41D926` 取静态表 `+0x211E0` 的纹理，`0x1C41D92F` 通过槽 `0x5408D18` 调用发布函数。
- 829 快照中该属性 ID 为 2938；按已验证原名字表查找 `_CharacterOverlayTex` 也得到 2938。
- 同一调用槽被 `Shader.SetGlobalTextureImpl(int,Texture)` RVA `0x1E48FEC0` 与 `Shader.SetGlobalTexture(int,Texture)` RVA `0x1E490970` 的 thunk 使用；快照目标为 UnityPlayer `+0xC5BAA0`。
- 证据目录：`20260905_character_overlay_producer_v1`、`20260905_character_overlay_binding_v1`、`20260905_character_overlay_setter_v1`，均在 `D:/ZZZ_Dump/output/corin_replication/`，模块哈希通过工具核验。

这证明应补全局纹理绑定，而不是在每份材质新增另一套着色逻辑。以下保留修复前诊断与 A/B/A；关闭开关的图仍不是正式修复画面。

## 结论

已用主 Unity 实例的短窗口 A/B/A 对照确认：Body 原材质启用 `_UseOverlayTex=1`，但恢复接入没有发布全局 `_CharacterOverlayTex`，造成深色衣服在主体像素程序中被截黑；后级角色 LUT 将接近零的输入映成当前深蓝底色。不能继续把此现象归为单纯灯光不足、缺 GI 或原贴图本来这么黑。

本轮是诊断，不是正式修复。所有对照参数已恢复，没有把 Overlay 永久关闭，也没有调亮原色。正式修复需要补回原 `CharacterOverlayTex` 的像素资产和全局绑定，不使用常量灰纹理代替原资源。

## 直接代码链

- `Assets/AssetArt/Model/ZZZ/可琳/可琳tex/ZZZ导出/Materials/Original/Corin_Body_ZZZ_Original.mat`：`_UseOverlayTex=1`，`_OverlayTexScale=50`。
- Hair、Weapon 同样启用 Overlay，Scale 为 30；Face、Eye 的该开关为 0。本轮只切换 Body，不能据此声称 Hair/Weapon 已单独验收。
- `Assets/Render/ZZZRestored/Generated/Character/OriginalStage2705.hlsl:263–269`：

```text
overlay = Sample(_CharacterOverlayTex, UV0 * _OverlayTexScale)
source = MainTex.rgb * _Color.rgb
albedo = _UseOverlayTex > 0.5 ? max(source + overlay - 0.5, 0) : source
```

- 这发生在主体光照计算前。未提供该纹理时，实际结果与 overlay 采样为零的行为一致：小于 0.5 的深色输入被截为零，白围裙仍有剩余颜色。
- 主体普通变体 `OriginalStage2700.hlsl` 也读取该全局纹理。
- 全项目 C# 搜索没有 `_CharacterOverlayTex` 的发布者；它不在当前 ShaderLab Properties 中，也不是 importer 当前恢复的材质纹理槽。
- `CorinRestoredRenderProfile.ApplyGlobals` 发布 LUT、天气、角色光照等，但漏掉此全局纹理。

## 实际 A/B/A

目标实例 `e852139597e42532`，正式回读 projectRoot 为 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`。非 Play Mode，使用 `ZZZ_RenderCheck_Camera`，1181×669。没有修改 Shader 源程序、纹理、灯强和其它材质参数。

所有图片在 `3cDemo/Client/3C_Client/Diagnostics/Rendering/ZZZRestored/20260905-cloth-diagnosis/`：

1. `Corin_Cloth_Overlay_A1.png`：Body Overlay=1。
2. `Corin_Cloth_Overlay_B0.png`：仅 Body Overlay=0，诊断对照，不是交付版。
3. `Corin_Cloth_Overlay_A2.png`：恢复 Body Overlay=1。

坐标为图片左上原点，数值为 PNG 8-bit RGB，不冒称 HDR 中间缓冲值：

| 像素 x,y | A1 | B0 | A2 |
| --- | --- | --- | --- |
| 643,419 肩部 | 0,0,32 | 50,44,70 | 0,0,32 |
| 646,424 肩部 | 0,0,32 | 33,29,59 | 0,0,32 |
| 541,415 肩部 | 0,0,32 | 51,46,72 | 0,0,32 |
| 686,580 裙身 | 0,0,32 | 54,49,75 | 0,0,32 |
| 700,607 裙身 | 0,0,32 | 51,46,73 | 0,0,32 |
| 588,565 围裙 | 157,149,169 | 200,191,212 | 157,149,169 |
| 566,320 头发控制点 | 57,79,55 | 57,79,55 | 57,79,55 |

A1/A2 全图仅 2 个像素存在差异，最大通道差 1。恢复不是主观判断。

窗口前后只读 audit：

- `20260905-135144-0166081-rendering-audit.json`
- `20260905-135151-7657633-rendering-audit.json`

两次均只有一个 Directional Light，强度 1、白色、欧拉角 `(50,289.3,约0)`。较早阶段的灯光朝向曾从 330 变化到 289.3，因此早期 MainLight/Override 截图只作辅助证据，不混称与最终窗口完全固定输入。

图片 SHA-256：

```text
A1 a4da1b5cbfa8edde893cc316754040a4a3acfb84b80b0f5b5785b34ca2fc50db
B0 979a04c1c9e28e869817878ac3410de5dd4b83699263717735e5d9736e8367a7
A2 1cacef85f563945da938fd7f4d76fed18042fcfbd711caeaae8575bd86370bbe
```

## 后级调色的辅助排除

按当前 `CorinOriginalCharacterLut.asset` 原 RGBAHalf 字节与 `OriginalDeferredShading.hlsl` 的 log 坐标、切片、双线性采样公式复算：

- 线性 RGB=(0,0,0) → LUT RGB≈(0,0,0.014366)，sRGB 编码约为 `(0,0,32)`。
- 线性 RGB=(0.1,0.1,0.1) → LUT RGB≈(0.100661,0.099486,0.119889)，sRGB 编码约为 `(89,89,97)`。
- 临时令原材质 `_Override=0.1`、保留原 `_OverrideColor=(1,1,1,1)`，主体输出成为 `0.9*C+0.1`。五个黑布控制点均实际变为 `(89,89,97)`，与 C 接近零的预测吻合；之后 `_Override` 已恢复为 0。

这支持黑色在主体输出前已经产生，不是后级将正常灰布统一压成黑色。但不据此宣布所有 LUT、缓冲格式、光照参数均正确，也没有取得本次各 HDR RenderTarget 的直接读回。

## ZZZ 原资源身份

复用现有 829 离线快照和元数据，没有读取游戏进程，也没有扫描新 dump。

元数据：`D:/ZZZ_Dump/output/corin_replication/20260904_renderer_config_metadata_v1/types.json`。

- type27768 `UnityEngine.Rendering.Universal.ForwardRendererData.characterOverlayTex`，实例偏移 `+0x48`。
- type27105 `UnityEngine.Rendering.Universal.EngineResources.characterOverlayTexture`，静态偏移 `135648 / 0x211E0`。
- 829 static base=`0x70013fb0000`；ForwardRendererData=`0x70016eebc40`，通过运行类型定义指针核对为 type27768。
- 两个字段均指向同一 managed Texture2D=`0x70016f74720`。
- managed `+0x10` → native=`0x20ce13a8100`。
- native 虚表 `+0x58` getter=`0x7ff994fb1b90`，直接运行字节 `48 8B 41 38 C3`，读取 native `+0x38` 字符串。
- 字符串地址=`0x20ce138aa48`，内容=`CharacterOverlayTex`。

所以缺的不是需要自创的效果，而是已确认存在的原引擎纹理输入。本次尚未从磁盘导出它的像素：829 对象 native `+0x60` 的 CPU Image 指针为 0，不能直接套用已验证的 LUT 曲线 CPU Image 读取链。下一步可沿现有磁盘资源索引提取该确切资源；不将未取得的像素填成灰色。

## 执行与恢复边界

- 最初计划扩展既有 audit 读取各阶段 RT，但当前 Simulation 改动存在 `OperationControlRuntime.cs` 的 `IOperationExecutionLifecycleHost`、`IOperationStateMachineHost` 等编译错误，新入口没有加载。
- 该未运行的阶段读回扩展已撤除；保留原来已验证的 audit，不引入第二套 RendererFeature 或额外 Runtime 路径。
- 使用现有已加载材质参数入口完成上述 A/B/A，不绕过编译系统加载新 DLL。
- `_Override=0`、`_UseOverlayTex=1`、灯强=1 均已恢复；未保存场景，未覆盖已有截图，未改动其它任务的 Simulation 文件。
- 本轮确认直接缺陷，尚未实施补纹理的正式修复，不能把 B0 对照图称为 ZZZ 完整还原效果。
