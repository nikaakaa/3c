# ZZZ 可琳 Shader：源码恢复与接入边界

> 历史取证记录：保存当时现场与验证边界。当前工具与数据入口见[Rendering README](../../README.md)和[渲染数据总览](../../CorinRenderData/README.md)。

## 当前结论

原 Shader 不能直接作为 Unity 2022 预编译 `.asset` 绘制。三份既有崩溃日志均出现旧 GPU 程序类型不能识别，随后进入 `ShaderLab::Program::GetMatchingSubProgram` 崩溃。恢复源码并重新编译是必需步骤，但不是全部工作。

当前已新增原角色 LUT 的源码资产和烘焙入口，并经过 Unity 实际绘制、回读及保存验证；主体角色渲染尚未完成。可琳仍使用保留的社区 ZZZMiyabi 材质，未切换缺输入的恢复候选。

旧预编译 Shader、自动注册 DLL 及两份无人引用的实验材质已按哈希不变移至 `Tools/Rendering/Archive/Unity2019Precompiled/`，不再由 Unity 导入。社区版和原始失败证据均保留。最新进展见 [原角色 LUT 恢复合同](../../ZZZ-LUT恢复合同.md)；以下保留各轮证据边界，不把旧轮结论当当前验收状态。

## 已修正的恢复错误

### 位值被错误地作数值转换

3Dmigoto 输出将临时寄存器和 StructuredBuffer 字段统一声明为 float。实际 GPU 寄存器没有这种固定类型；同样的 32 位数据由具体指令决定如何解释。

原 NapEntityPrepare 的 `t5` 步长为 208 字节，其中 byte 28 / 44 是两个 int 灯光索引边界。原程序直接将位值用于 `ige` / `ilt`，再从步长 4 的 `t6` 读取整数灯光索引。旧源码将这些位值先当 float，再执行数值强转。整数 1 的位模式会成为极小浮点数，转回 int 得到 0。这不是调光照参数能修复的问题。

已有快照 kernel1 的对照：

| 转换指令 | 原始 DXBC | 旧源码只补入口后 | 新位值源码重编译 |
| --- | ---: | ---: | ---: |
| ftoi | 1 | 16 | 1 |
| ftou | 3 | 13 | 3 |
| itof | 1 | 16 | 1 |
| utof | 2 | 4 | 2 |

计数相等只证明这些多余转换已消除，不证明整个程序逐位等价。仍保留原始程序、每条指令对应的生成行、全部重新编译结果。

相同根因还影响主体材质的位标记和后处理的 NaN / Inf 检查。旧材质位标记重新编译后变成零；旧后处理把“检查浮点位模式”变成“先把颜色强转成整数”。现在统一从原 DXBC 恢复，不再逐处猜测补 cast。

位值恢复器自身也经过一轮否决：最初将所有源操作数负号都按浮点符号位处理，但原 `iadd`、`itof` 的负号必须按整数补码处理。该错误涉及主 Pass 的整数索引和角色合成的采样循环。现在明确分离两种语义，依据 [Microsoft iadd 定义](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/iadd--sm4---asm-) 重编译。`typed_named_shader_v1/v2`、`typed_deferred_character_v1` 不作为接入候选，保留作失败记录。

### 输出签名被扩宽

原描边 Vertex entry24 的 TEXCOORD5 是 xyz，原指令仅声明并写入 xyz。旧反编译器声明 float4，造成 W 未初始化警告。新恢复器直接使用原接口签名的 float3，没有填入一个猜测的 W。

### 原生函数曾只取到序言

UnityPlayer 存在 chained unwind。旧工具把 `.pdata` 的第一条范围当整函数：GBuffer Execute 的内部入口 `0x93B210` 只得到 36 字节，Deferred Execute `0x929A00` 只得到 46 字节。

按 [Microsoft x64 chained unwind 定义](https://learn.microsoft.com/en-us/cpp/build/exception-handling-x64?view=msvc-170#chained-unwind-info-structures) 聚合后，分别得到 1,555 和 19,065 字节，且每个片段都能完整反汇编。工具保存片段地址和二进制拼接偏移，不把中间间隙当代码。

## 已取得的原程序

统一根目录为 `D:/ZZZ_Dump/output/corin_replication`。

| 独立目录 | 内容 | 验证边界 |
| --- | --- | --- |
| `20260904_typed_named_shader_v3` | Shadow、Outline、主体普通 / MatCap、Depth 共 10 阶段 | D3DCompile 零警告零错误、输出签名与原程序相同；没有 Unity 绘制 |
| `20260904_renderer_snapshot_v1` | 829 快照里的 NapEntityPrepare 原始 3 kernel，0 / 1 相同 | 有对象、模块和内存来源；不是新采样 |
| `20260904_typed_compute_v1` | 两个不同的快照计算程序位值恢复及旧源码失败对照 | D3DCompile 零警告零错误；未绑定项目输入 |
| `20260904_compute_typetree_v1` | 磁盘 NapEntityPrepare 的原类型树与资源名 | 与快照字节码不同，不能混称同一程序 |
| `20260904_deferred_shader_json_v1` | 原 DeferredShading Shader JSON | block 273563795、CAB-43f05fb98dd40780c7f9725d3c8707ec |
| `20260904_typed_deferred_character_v2` | 角色合成的普通、Stylization、ShadowLine 三变体及角色后处理 | 4 个 Pixel 阶段，零警告零错误；未验证游戏当帧选择 |
| `20260904_gbuffer_unityplayer_v3` | 完整原生 GBuffer / Deferred Execute | 原生接口身份闭合，所有配置分支尚未映射到项目 |

旧快照的 NapEntityPrepare 与当前磁盘两份同名资源不逐字节相等。恢复工具保留各自哈希，未拿其中一份参数表冒充另一份的已验证绑定。没有新 Hook、注入、游戏进程读取或游戏重启。

## 后级角色合成的实际内容

829 快照中 ForwardRendererData `+0xE8` 的 ShaderResources 经类型定义验证为 type27772；其 `+0xB0` Shader 对象名称为 `Hidden/Universal Render Pipeline/DeferredShading`。

名称读取链由原 UnityPlayer 虚函数证明：`0x12A4C40` 调用虚表 `+0xC8`，目标 `0x12A4D10`。本快照该 Shader 的 `+0xC0` 为空，执行 getter 的 `object+0x58` 字符串分支。没有把不可用的 parsed form 当成现成程序缓存。

磁盘原 Shader 的 `DeferredShadingForCharacter` entry6 直接读取：

- `_GBuffer0.rgb`：主体颜色输入。
- `_GBuffer1.rgb`：按 `(5 × rgb)²` 解码的边缘光颜色；其 alpha 参与原颜色与角色 LUT 后颜色的混合。
- `_GBuffer2.xy/w`：运动及类型相关数据。这里保留原始用途范围，不给全部通道自造业务名。
- `_CameraNormalTexture`：按 `2 × rgb − 1` 解码法线。
- `_CameraDepthTexture`：用于原程序的多点深度比较与边缘光计算。
- `_InternalLut_Char`、`_Lut_Params_Char`、`_FXCC_LutToneParams`：角色专用调色及混色。

entry6 输出颜色和另一份运动相关结果；不是把 `_GBuffer0` 直接复制到屏幕。角色后处理 entry4664 还检查 NaN / Inf 并根据 `_ExposureParams` 处理曝光。

这是原程序本身的读写数学证据。当前尚未把原生所有 RT 绑定身份、实际格式、MSAA / 分辨率分支与上述 Shader 槽位逐个接完；不能仅凭名称相似就认定所有附件关系已闭合。

## 下一段接入所需的确切内容

1. 完成原生 RenderTarget 的创建、槽序、格式与后级纹理绑定对账，包括角色绘制实际走到的分支。GBuffer 内部入口 `UnityPlayer+0x93B210` 已恢复，主四路及可选七路附件数组已定位；不能将它们直接等同于某一帧 Corin 的附件。
2. 用单一正式运行时模块提供 UnityNapCB、角色光照输入、MatCap 数组、阴影和相机数据。已有原始公式不能替代这些每帧输入，也不能使用未命名默认值把它们填满。
3. 接入原角色合成与 LUT / 曝光生产者，再做 Unity 导入、编译、绘制和前后截图；不得重新启用已导致崩溃的旧序列化 Shader 绘制路径。

目前未完成以上接入，因此不交付“已可用”“与游戏一致”结论。零编译诊断不能代替最终画面，也不能保证原生崩溃问题已经由新源码路径经过实际绘制验证。

## 后续：四路缓冲与全局参数已读取

### 材质保留决定

用户明确要求两套都保存，完成后使用还原版。当前 Corin 的 Body / Face / Hair / Weapon 四个正式材质均引用 GUID `7296a46efec407646afe9bc4aa0eb31c`，即社区 ZZZMiyabi 的 `CelShaders/ZZZShader`。这与上轮使用的安全材质状态不同。

本次没有覆盖这些材质、社区 Shader 或 Face 朝向组件。之后应保留社区版资产，还原版另存；只有完成验证后才切换可琳的正式引用，不建立运行时 fallback。

### 缓冲的名字与配置格式

`20260904_shader_parameter_snapshot_v2/shader-parameters.json` 从原 `GlobalBufferManager.globalRTs`、`GlobalRTWrap` 和 `PropertyToID` 名字表交叉核对得到：

| 槽 | 名称 | 原 GraphicsFormat 数值 | 当前 Unity 2022 同值枚举 |
| --- | --- | ---: | --- |
| 0 | `_GBuffer0` | 48 | R16G16B16A16_SFloat |
| 1 | `_GBuffer1` | 4 | R8G8B8A8_SRGB |
| 2 | `_GBuffer2` | 75 | A2B10G10R10_UNormPack32 |
| 3 | `_CameraNormalTexture` | 75 | A2B10G10R10_UNormPack32 |

不是按字段名称猜槽序：四个 wrapper 的 `rtNameHolder` 分别为 672～675，并与原名字表查找结果逐项相同。类型定义也验证为 GlobalRTWrap/type6028。Unity 2022 枚举值通过本机 UnityEngine.CoreModule.dll 反射核对。

静态初始化入口 `GameAssembly+0x1E47D580` 同样验证后三路格式，第一路另有格式 74 的平台分支，不能把 48 宣称为全部平台的固定值。`UnityPlayer+0x93B34A–38B` 使用 `{0,1,2,3}` 创建四路目标；是否增加额外三路有独立条件。

快照中四个 wrapper 的实际 RT handle、上次创建宽高均为 -1。因此这里只证明配置和身份，**不证明快照发生时存在已分配的四张目标纹理，更不证明一次原子 Corin Draw 的绑定状态**。

### 参数读取不是猜默认值

原链如下：

- `Shader.PropertyToID`：GameAssembly `0x1E48CC20` → UnityPlayer `0xC58610` → `0x79FC40` → `0x919DF0` → `0xA440E0`。
- 名字表查询：`0xA45F50`。采用 FNV-1a 32 位，起点按原 mask，后续探测步长 8、16、24。槽地址为 `base + 3 × index`，不是错误地把 mask 当作槽数量。
- Vector 读取：`0xC5D360` → `0x79E260`；Matrix 读取：`0xC5D5F0` → `0x79E130`。
- typed property 查找：`0x16D990` / `0x16DC60`。按类型范围找到同一 PropertyID，再由 descriptor 低 20 位定位 payload。
- MatrixArray：`0xC5EA10` → `0x79E1F0` → `0x79B080`；数量 getter 最终到 `0x79B1D0`，数量为 `(descriptor >> 20) & 0x3FF`。已读取 `_MainLightWorldToShadow` 的实际五元素矩阵数组。

对主体 MatCap 的 Vertex/Pixel 与普通角色合成的 50 项全局需求，本次结果为：

- 44 项已发布，保存原字节、float 视图、uint 视图、名字查询地址和参数声明。
- 5 项是线程局部储存的内建相机参数：`_WorldSpaceCameraPos`、`_ZBufferParams`、`unity_MatrixV`、`unity_MatrixVP`、`unity_WorldToCamera`。
- `_MotionBlurMask` 在快照里未发布，没有用零值冒充已采到。

已发布示例：`_PostFrontTint=(1,0.97647065,0.87450987,1)`，`_CharacterAmbient=(0.2,0.16,0.16,1)`，`_CharacterMatCapEnable=1`，`_is_apply_lut_character_on=1`。该快照中 `_is_main_light_shadows_on=0`、`_RimGlowIntensityForChara=0`；这只是该缓存的事实，不是可以全局写死的游戏规则。

该读取仍不具备同次 Draw 的原子性。尤其 `_GlobalTimeParamsB`、相机与场景控制值不能直接当常量永久写入项目。两个数值视图也不等同于已证明上传至 GPU 的类型转换：最终仍须按原声明和 setter/upload 合同处理。

### 当前仍须接入的具体对象

1. 当前相机的矩阵 / 位置 / 深度参数，以及每帧时间输入。
2. UnityNapCB 与 NapEntityGPUData 的实体输入及生命周期，不用别的角色或陈旧快照冒充当前可琳。
3. 已证实的 MatCap 数组构造、角色 LUT 的原生成过程、对应纹理绑定。
4. 按原 Stencil、Blend、Depth 状态执行角色绘制与合成，再验证实际画面。

这次新增的是可重跑的数据读取工具和证据，没有提前交付一个缺输入但可误挂的 Shader 包装。
