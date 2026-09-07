# 可琳 MatCap 原始生产与消费合同

## 本轮结论

已闭合四组参数数组的源字段、分量顺序、层号覆盖、写入 owner，以及 Simplify 分支的主要纹理收集、检查、创建和复制规则。已找回可琳 Body 引用的原始 MatCap 图及全部压缩 mip 数据。

未改 Unity、场景、工程材质或 Runtime。此文不是“ZZZ Shader 已可用”的验收记录，也不证明当前游戏画质配置选择了哪条构建分支。

## 证据范围

- 磁盘 `GameAssembly.dll` 本轮重新计算 SHA-256：`4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4`，与 829 元数据记录一致。
- Native 方法从 PE `.pdata` 的真实函数起点到终点提取；原始字节、哈希、RVA、直接调用和 RIP 引用一起保存。
- P1 为已有 829 离线页摘录，不是新采样。补解了 NapMaterial、NapRenderer、相关 Unity API 的 MethodInfo/字段身份；MatCap 静态表和字符串保留读地址与原始字节。
- 不把旧快照 VA 用到当前进程；旧快照的属性 ID 5067～5070 只用于对账，不能写死到 Unity 工程。

主要证据目录均位于 `D:/ZZZ_Dump/output/corin_replication/`：

| 目录 | 内容 |
| --- | --- |
| `20260904_matcap_native_v1` | 五个 NapRenderEntity 方法完整字节及汇编 |
| `20260904_matcap_native_helpers_v1` | NapMaterial 实例刷新方法与材质读取边界 |
| `20260904_matcap_packing_v1` | NapMaterial 静态构造、浮点读取、独立材质刷新和 SetVectorArray |
| `20260904_matcap_metadata_v1` | 分支配置字段 `m_IsOptimizeMatcapArrayCreation` 的布局 |
| `20260904_matcap_metadata_v5` | 本轮最终相关类型、静态参数表、属性名和原始读记录 |
| `20260904_matcap_icalls_v1` | Material/Texture/Graphics 间接调用槽身份 |
| `20260904_matcap_log_identity_v1` | `System.Math.Log(double)` 到 `0xE4FF00` 的跳转字节 |
| `20260904_matcap_shader_ids_v1` | CSharpShaderIDs 的原参数名和静态布局 |
| `20260904_matcap_feature_gate_v1` | SupportFeature 到 ShaderConfig 的准入检查 |

## 原图身份及数据

Body `_MatCapTex` 至 `_MatCapTex5` 均为同一 PPtr：fileID 4、PathID `-807226201400800218`。

外部 CAB 为 `CAB-98d94369a3c1de46239832836d89337d`，实际位于资源块 `2735517596.blk` 中偏移 `3055714` 的 mhy1 容器。解析得到 Texture2D 名称 `Eff_MatCap_019`，PathID 精确匹配，不是按图案相似度选的替代图。

- 大小 256×256，BC7，9 级 mip。
- `Eff_MatCap_019.dat` 为 208 字节，SHA-256：`32977e9480e8a788c4782b686335e9b7baaf7629c7320650a8d90b26598e9fe0`，与 AssetMap 对象哈希一致。
- 原始 `.resS` 为 87408 字节，SHA-256：`13b642d957d16df13b715bbab67098eedba6dc1b7bf3ceb15044a64cf9df42e5`。
- 原对象字节 `0x33` 为 `m_StreamingMipmaps=0`。该偏移按现有 `Texture.cs → Texture2D.cs` 的 ZZZ Unity 2019 分支核对；不能误用 JSON 中未参与此版本读取的 `m_MipMap=false` 判断没有 mip。
- 原始过滤值 1、各向异性值 1。原 JSON 的 `m_MipCount=9` 与 `.resS` 中九级 BC7 数据总长度一致。

完整原始数据：`20260904_matcap_texture_nodes_v2/`。图片预览：`20260904_matcap_texture_image_v1/Texture2D/Eff_MatCap_019.png`。JSON、原始对象和 AssetMap 分别保留在 `20260904_matcap_texture_json_v1`、`20260904_matcap_texture_raw_v1`、`20260904_matcap_texture_identity_v1`。

PNG 只是预览；后续原 mip 复制必须使用 `.resS`，不能用重新生成的 PNG mip 代替并声称相同。

## 构建分支和顺序

`NapRenderEntity.RefreshMatCapData`：

1. `0x1C98D302–313`：先释放现有纹理数组关联。
2. `0x1C98D3A9`：读取管线资产 `+0x2CD` 的布尔值。
3. 字段元数据证实该值为 `UniversalRenderPipelineAsset.m_IsOptimizeMatcapArrayCreation`。
4. 真值跳至 `0x1C985930 CreateMatCapTextureArraySimplify`；假值跳至 `0x1C987400 CreateMatCapTextureArrayLegacy`。

本轮没有取得当前可琳绘制所用管线实例的该字段值，不能宣称当前游戏必用 Simplify。两条 native 分支均已提取，以下详细收集规则以 Simplify 为准；不把尚未逐条闭合的 Legacy 分组行为合并成同一算法。

### Simplify 收集与检查

- 遍历 `NapRenderEntity.rendererData` 中的 NapRenderer 和 NapMaterial。仍有 Shader 能力位 `0x2000` 等准入门，本轮保留位值，不猜枚举成员名。已核对 `SupportFeature(0x1E65E950)`：先调用 `GetShaderConfig(0x1E65E3C0)`，再检查返回配置中的位；不是只看材质 `_MatCap`，也不是只注册 Shader 名字就能通过。
- `0x1C985D07–53`：检查材质属性存在并读取 `_MatCap` 对应值，值不大于零则不进入收集。
- `0x1C985D59–DBC`：分配五个索引并全部初始化为 **100**。
- `0x1C985DD2–E21`：按实际 `usedMaterialIDCount` 读取各子材质的 MatCap 纹理；不存在的纹理不进入有效列表。
- `0x1C985EB9–EF5`：先在已收集纹理中查找；`0x1C985F26–F45` 在未找到时以当前列表长度作为新索引，然后加入纹理。
- 同一原纹理引用重复出现会复用索引。可琳五个槽位最终应指向同一有效层；不能在未核对整个实体的其它材质及收集顺序前强称这一层必然为 0。

`MatCapTextureCheckResult` 返回的是“存在问题”，不是“通过”：

- `0x1C98A75D–77B`：宽、高分别与 256 比较；不符则记录问题。
- 新加入的纹理若已有首张有效纹理，`0x1C98AA2E–65` 比较 TextureFormat；格式不一致记录问题。
- `0x1C98A82A–835` 查询 `Texture2D.streamingMipmaps`，启用时记录问题。
- 调用者 `0x1C985F1E–20` 在返回真时跳过该子材质，保留无效索引。

上述间接调用已经通过 Unity 方法 thunk 对上：`0x5407C08` 为宽、`0x5407C10` 为高、`0x54083F0` 为格式、`0x54084A0` 为 streamingMipmaps。后者不是 isReadable。

### 数组创建与复制

`0x1C986549–681` 使用首张纹理尺寸/格式和有效纹理数创建 Texture2DArray。mip 数为：

```text
m = min(sourceMipCount, max(trunc(log(min(width, height)) / log(2)), 4) - 3)
```

`0xE4FF00` 已通过 `System.Math.Log(double)` 的直接跳转核对，不再只是按算术形状猜 log。分母 `0x2833920` 的磁盘 double 为 `0.6931471805599453`。

对这张 256×256、9 mip 原图，`m=5`。`0x1C986662–67C` 的构造参数为首图 TextureFormat、计算的 mipCount 和 `linear=false`。随后设置过滤值 1，并复制首图各向异性值。

`0x1C986772–7F9` 的两个循环调用已确认的 `Graphics.CopyTexture_Slice`：源 element=0，源 mip=j，目标 element=去重后的层号，目标 mip=j，`j=0..m-1`。不是重新烘焙、缩放或采样近似纹理。

### 层号、参数与材质写回

- `0x1C986A46–59`：将生成的数组绑定到材质的目标纹理属性。
- `0x1C986B4C–BE3`：读取旧子材质参数，保留其 yzw，用新层号写 x。
- `0x1C986CFE–D1B`：同一个层号进入 `NapMaterial.cachedTexIDs[i]`。
- `0x1C986863`：调用 `NapMaterial.RefreshMatCapVectorArrayProperties`。
- `0x1C98686B`：结束此次 NapRenderer 属性操作。

CSharpShaderIDs 元数据另行证实：`0x4B48` 为 `_MatCap`，`0x4964` 为 `_MatCap2DArray`，`0x21090` 为五个贴图属性的 ID 数组 `_MatCapTexArray`，`0x21088` 为 `_MatCapReractParamsArray`。其中整数字段的取值位置与 native 直接读取位置按该版本静态区对应；引用数组从 `0x5360730` 指向的静态区取值。

## 四组数组的精确排列

`NapMaterial.RefreshMatCapVectorArrayProperties` 位于 `0x1C4C97B0`。使用真实材质和 `min(materialIDCount,5)` 的元素数；按静态表读取属性，不添加插值或其它混合。

| Shader 数组 | 元素 i 的来源 |
| --- | --- |
| `_RefractParamArray` | `_RefractParam` / `2` / `3` / `4` / `5` 的原四分量 |
| `_MatCapColorTintArray` | `_MatCapColorTint` / `2` / `3` / `4` / `5` 的原四分量 |
| `_MatCapTexID_MatCapColorBurst_MatCapAlphaBurst_MatCapUSpeed` | **缓存层号**、ColorBurst、AlphaBurst、USpeed |
| `_MatCapVSpeed_MatCapBlendMode_MatCapRefract_RefractDepth` | VSpeed、BlendMode、MatCapRefract、RefractDepth |

静态构造 `0x1C4CA280` 建立两个 vector 条目和两个 floatPack 条目。829 快照中的数组指针、各字段数组、目标属性名均与其写入顺序一致。

关键落点：

- `0x1C4C995E–99CE`：逐项读取四分量并调用 NapRenderer.SetVectorArray。
- `0x1C4C9B8D / 9BD5 / 9C1D / 9C6A`：按 x/y/z/w 写入四个标量。
- `0x1C4C9C71–CA2`：只在第一组 floatPack，用 `cachedTexIDs[i]` 转成 float 覆盖 x。
- `0x1C4C9CED`：发布该组数组。

无效层号 100 与 Shader 消费一致：恢复的 entry2705 源码只有在层号 `<50` 时才采样 MatCap。不能将 100 改成层 0；这会把无效输入变成错误的反射图。

可琳 Body 的五组源数据中，ColorBurst=1、AlphaBurst≈0.2、USpeed=0、VSpeed=0、BlendMode=1、MatCapRefract=0、RefractDepth=0.5。Tint 第一组蓝分量与后四组存在 float32 的末位差，必须保留原值，不能统一抹平。

`NapRenderer.SetVectorArray` 是 `0x1D2F5E50`，参数为 `(propertyID, Vector4[], materialIndex)`。它按 `propertyMode` 使用材质或 MaterialPropertyBlock，不是一个无条件全局 Shader.SetGlobalVectorArray。不能把不同角色的数组写成同一组全局值。

另有 `NapMaterial.RefreshMaterialMatCapVectorArray`（`0x1C4C8CA0`）直接处理独立 Material；本轮确认的实体构建调用的是带 `cachedTexIDs` 的实例方法，不混用这两个入口。

## 实施边界

已具备实现该 MatCap 输入模块的核心数据和顺序，不再需要猜四组数组的分量或寻找一张相似图。但还不能据此恢复原 Shader 的整条绘制：

1. 当前管线选择的构建分支、实际收集的全部材质、Shader 能力位和最终层号没有本轮逐调用运行记录。
2. `UnityNapCB`、实体 GPU 数据、角色光照和阴影输入仍需各自闭合。
3. 四路 render target 的格式和下游消费者仍未接入。
4. 源码反编译警告及数值等价、Unity 编译和实际绘制验证未完成。

因此本轮不新增猜测配置、不硬编码层 0、不把数组挂成全局数据、不再强行绘制旧预编译 Shader。完成的是可迁移的 MatCap 数据合同与原始资源恢复，不是最终画面验收。
