# ZZZ 可琳 Shader：源码恢复与接入边界

## 当前结论

原 Shader 不能直接作为 Unity 2022 预编译 `.asset` 绘制。三份既有崩溃日志均出现旧 GPU 程序类型不能识别，随后进入 `ShaderLab::Program::GetMatchingSubProgram` 崩溃。恢复源码并重新编译是必需步骤，但不是全部工作。

本次只修改离线恢复工具，未修改场景、运行时代码、渲染管线或材质引用。场景仍使用之前恢复的安全项目材质，不能称为 ZZZ 原效果。

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
