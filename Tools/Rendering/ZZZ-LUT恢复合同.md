# 原角色 HDR LUT：来源、接入与实际绘制

## 已完成的范围

已把原 `Hidden/Universal Render Pipeline/LutBuilderHdr` 的 `_TONEMAP_CUSTOM` 顶点与像素程序恢复为源码，在本项目 Unity 2022.3.62f2c1 / D3D11 编译、实际绘制，并回读成 1024×32 RGBAHalf LUT。不是只通过离线编译，也不是把一张白图当 LUT。

这只是角色调色输入完成一次 GPU 绘制验证；主体 Shader、角色光照缓冲及角色合成尚未完成接入。当前可琳仍使用社区 ZZZMiyabi 材质，没有提前切到缺输入的还原版。

## 来源身份

原 Shader 来自游戏 `globalgamemanagers.assets`，恢复记录为 `D:/ZZZ_Dump/output/corin_replication/20260904_lut_hdr_recovery_v2`。接入的是 entry3 Vertex / entry11 Fragment，其余 14 个阶段只做离线恢复和编译。

`capture_zzz_character_lut.py` 读取已有 829 离线快照，未读取当前游戏进程。完整原始读记录位于 `20260904_character_lut_capture_v2/character-lut.json`：

- CharacterColorGradingLutPass：GameAssembly `0x1EA0F930` → UnityPlayer `0xBE47F0` → `0x938470`。
- UnityPlayer `0x9386A9–9386AE` 通过 `0x9526F0 / 0x9524F0` 取槽 3 的缓存材质 ID `-13204`，与 EngineResources 中第三份 LUT 材质身份一致。
- `0x938D3C` 所在 HDR 分支启用 `_TONEMAP_CUSTOM`；读取的是该材质 cooked property sheet，不宣称是同次 Draw 的最终 GPU 上传。
- 25 个标量/向量参数直接来自该缓存。`_UserLut_Params` 未发布；项目配置明确禁用可选外部 UserLut，并绑定零参数，不把未发布值写成“游戏采样值为零”。
- 八个曲线槽实际指向两张原 RHalf 纹理。第一张 128 个 half 为 `i/128`，最后一项是 `0.9921875`，不是重新生成端点为 1 的曲线；第二张全为 `0.5`。像素 bytes、原格式、过滤及寻址值均保存。
- ImageData getter `0x5EF100 / 0x5EF110 / 0x17BB90` 分别验证数据、格式与长度；两张图均为 128×1、RHalf、256 字节。

这是**共享角色调色通道**的快照，不是已经确认的“可琳专属当帧光照”。工程中的 Corin 命名表示本次目标资产用途，不改变原数据归属。已有实体枚举识别到铃的材质，没有核实到可琳实体；不能拿其它角色的 NapCB/GPU 数据直接替代可琳。

## 当前代码链

1. `recover_dxbc_hlsl.py` 按原指令、位值和参数名字恢复阶段，补齐原 LUT 使用的 SM4 `sample` / `sample_l` 等语法，不改调色公式。
2. `emit_zzz_lut_assets.py` 将恢复结果生成到 `Assets/Render/ZZZRestored/Generated/CharacterLut`。原离线 cbuffer/packoffset 留在证据目录；Unity 接入使用具名 uniform 和 ShaderLab Properties，交由当前编译器建立绑定。
3. `CorinRestoredLutImporter.Bake` 编译两个阶段，读取原曲线 bytes、绑定参数与曲线，执行 `Graphics.Blit`，回读半精度纹理；非有限像素或整图无颜色变化直接失败，不发布黑色占位结果。
4. 原曲线、绑定材质和结果 LUT 均保存为正式资产。材质 Properties 保留参数和纹理引用，重载后不依赖刚好仍留在内存里的设置。
5. 每次验收写新的 `Diagnostics/Rendering/ZZZRestored/LutBakes/<时间>-<唯一ID>`，包含编译结果、回读 PNG 与 `lut-bake.json`，不覆盖旧验收。

菜单：`Tools/ZZZ/Restored/Bake Original Character LUT`。该入口不修改场景、角色材质或 Renderer 引用。

## 实际修正过的两类接入错误

- 直接把离线显式 cbuffer/packoffset 包进 Unity Shader 后，实际回读为常量色。保留原公式，改用 Unity 的具名绑定。
- 无 ShaderLab Properties 时，保存/复制材质会丢掉未序列化 uniform；实际 `_Lut_Params` 变成零，输出黑色。现补全 Properties、保存绑定，重载后再次实际绘制通过。

运行结果 `20260904-135926-255-f6654ccd`：1024×32，非有限像素 0，颜色变化为真，编译消息为空；首像素 `(0,0,0.0143432617,1)`，实际 `_Lut_Params=(32,1/2048,1/64,32/31)`。这证明本入口的编译、绑定、绘制和保存能工作，不证明整个角色最终画面与游戏逐像素一致。

## 仍需区分的接入适配

- 原内建 sampler 编码 85 当前映射为 Unity 可识别的 `linear_clamp_sampler`，与已读取曲线纹理的 Bilinear/Clamp 设置相符；尚未单独闭合原编码到 D3D sampler 描述符的解码，不能据此宣称字节级等价。
- 原 LUT 绘制无深度附件；包装用 ZTest Always / ZWrite Off。未把它写成原序列化渲染状态逐项相同。
- 原主体依赖逐角色光照数据和四路输出；LUT 成功不授权省略这些输入、替换角色合成，或重新启用旧预编译 Shader。

## 原生后端补核

已额外核对 `MonoRenderEntity.get_UseNativeBackend`（GameAssembly `0x1CB03B00`）读取 `0x5365A94`；829 快照字节为 0。`CreateNativeObject / SetupNative` 的完整指令保存在 `20260904_entity_native_setup_v1`。这排查了“只枚举托管实体就必定找全”的风险，但不构成当前可琳 Draw 记录，也不填补缺失的逐角色输入。
