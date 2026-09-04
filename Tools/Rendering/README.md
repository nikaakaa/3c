# ZZZ 原始 Shader 离线恢复

`recover_zzz_shader.py` 负责从现有 ZZZ 导出记录恢复可审查的 D3D11 阶段源码和材质序列化字段。它不访问游戏进程、不调用 Unity、不修改场景或工程材质，不生成替代渲染效果。

## 输入、处理和输出

输入为原 Shader JSON、原始材质对象 `.dat`、用于交叉核对的材质 JSON，以及已有 HLSL 反编译 DLL。输出目录必须是新目录，不能覆盖旧结果。

处理顺序：

1. 解析 D3D11 的六个 LZ4 段，按段号、偏移、长度读取程序目录。
2. 读取程序自身版本、类型、全局和局部关键字，不用文件名猜程序变体。
3. 按 `m_BlobIndex` 找到 Pass、阶段、原参数名称表和常量布局。
4. 提取 DXBC，恢复 HLSL，并保存原始 DXBC 的反汇编。
5. 按原字节偏移恢复常量名称、整数位表达、矩阵列和数组访问；恢复纹理及 StructuredBuffer 名称。未解析的访问直接报错，不填零、不猜默认值。
6. 用本机 `d3dcompiler_47.dll` 分别编译反编译原文和带名字源码，保存全部警告、DXBC、反汇编和指令块比较结果。
7. 单独读取 Unity 2019 ZZZ Material 的序列化前缀及属性表，对引用、属性名和 float32 数值逐项核对现有 JSON，保留关键字、队列、禁用 Pass、enabledPassMask 和原始偏移。

输出中的 `runtime_bound=false` 是明确边界。编译通过不等于 Unity 导入、绑定正确，更不等于原画面已复现。

## 2026-09-04 已完成的恢复

最终记录位于 `D:/ZZZ_Dump/output/corin_replication/20260904_named_shader_recovery_v9`。

- Shader JSON SHA-256：`e5d5ea77a8536a15568e40ab2a4338683c82373ebee1f72416a68a0250cfc468`。
- D3D11 程序目录完整读取 4026 项；不是恢复了 4026 份源码。
- 10 个阶段的反编译原文和带名字源码均完成独立 D3DCompile，零编译错误。不是 Unity 编译验证。
- entry 24 有 X3578 未完全初始化输出警告；48、2700、2705 有反编译表达式负数转无符号的 X4115 警告。警告未压制，也未擅自更改原计算。
- 带名字源码和反编译原文重新编译后的指令块，只有 entry 16 完全相同。其它结果含声明次序、交换操作数、动态矩阵索引等差异；不能声明字节相等或已经证明全部数值等价。
- 原始 DXBC 和重新编译 DXBC 分开保存。此记录不证明反编译器已无损恢复原程序。

| Pass / 已恢复关键字条件 | Vertex entry | Pixel entry |
| --- | --- | --- |
| ShadowCaster，无关键字 | 0 | 16 |
| CharacterOutlineDeferred，无关键字 | 24 | 48 |
| CharacterToonDeferred，`_NAP_SHADER_QUALITY_HIGH` | 60 | 2700 |
| CharacterToonDeferred，上述质量 + `_MATCAP_ON` | 65 | 2705 |
| CharDepthOnly，无关键字 | 4020 | 4024 |

表中是已恢复条件，不是已采样确认的游戏当帧全局关键字。`_TOON_LIGHTS`、画质级别和效果变体仍须按实际使用条件选取。

## 已证实的旧材质导出缺项

AnimeStudio 的 `Classes/Material.cs` 将关键字、渲染队列、禁用 Pass 等读成局部变量，默认 JSON 不发布这些数据。直接从该 JSON 创建空关键字材质会丢信息。

| 原始序列化事实 | Body | Hair |
| --- | --- | --- |
| 关键字 | `_MATCAP_ON` | 空 |
| CustomRenderQueue | 2000 | 2000 |
| disabledShaderPasses | `CharacterOutlineFXDeferred` | `CharacterOutlineFXDeferred` |
| enabledPassMask 原值 | 1 | 1 |
| 读取到末尾 | 14508 / 14508 bytes | 14496 / 14496 bytes |

两份材质各 27 个纹理槽、277 个 float、151 个 color，均与既有 JSON 逐项对上。`enabledPassMask=1` 只记录原值，不据此猜测具体启用的 Pass。保留 instancing 后的原始三个字节，没有将未核实的布局强行命名为业务开关。

Body `_MatCapTex` 至 `_MatCapTex5` 指向同一个引用：fileID 4、PathID `-807226201400800218`，对应 Body CAB 外部引用 `CAB-98d94369a3c1de46239832836d89337d`。静态材质的 `_MatCap2DArray` 为空。

但 Body 的 `_MATCAP_ON` 阶段实际采样 `_MatCap2DArray`，并读取四组五元素向量数组：

- `_RefractParamArray`
- `_MatCapColorTintArray`
- `_MatCapTexID_MatCapColorBurst_MatCapAlphaBurst_MatCapUSpeed`
- `_MatCapVSpeed_MatCapBlendMode_MatCapRefract_RefractDepth`

因此将五张材质纹理直接绑定回槽位，不能代替上述运行时纹理数组及打包参数。

## 下一段必须接清的生产者

现有 `D:/ZZZ_Dump/PIK分析包/元数据/控制器与战斗/methods.csv` 已定位以下入口；地址为该份元数据记录的 RVA，未核对模块版本前不能直接用于其它磁盘版本或活体：

- `NapRenderEntity.CreateMatCapTextureArraySimplify`：`0x1C985930`。
- `NapRenderEntity.CreateMatCapTextureArrayLegacy`：`0x1C987400`。
- `NapRenderEntity.MatCapTextureCheckResult`：`0x1C98A710`。
- `NapRenderEntity.RefreshMatCapData`：`0x1C98D2E0`。
- `NapRenderEntity.RefreshMatCapVectorArrayProperties`：`0x1C98F570`。

这些是入口定位，不是对其内部算法已完成反汇编证明。不能仅凭名字复刻纹理去重、层索引和向量打包。

主 Pass 还依赖 `UnityNapCB`、`_NapEntityGPUData`、角色专用光照、级联/单角色阴影。`SV_Target0..3` 的后级消费和纹理格式尚未恢复。不能把最终颜色以外的三路丢掉后宣称完整还原。

在这些输入输出接清之前，不生成可挂载的运行时 Shader 包装、不恢复原始预编译 `.asset` 绘制，也不新增猜测光照、纹理数组或绕过现有渲染系统的临时 RendererFeature。

## 复跑

```powershell
& 'C:/ProgramData/miniconda3/python.exe' 'Tools/Rendering/recover_zzz_shader.py' `
  --shader-json '<原始Shader JSON绝对路径>' `
  --output '<新的离线输出目录>' `
  --decompiler '<已有AnimeStudio.HLSLDecompiler.dll绝对路径>' `
  --entries 0 16 24 48 60 65 2700 2705 4020 4024 `
  --material '<Body原始.dat>' '<Body原JSON>' `
  --material '<Hair原始.dat>' '<Hair原JSON>'
```

工具支持本次实际验证的布局；不宣称是通用 Unity Shader / Material 转换器。原始 Shader 的来源证据、工具和 DLL 哈希、各阶段编译诊断保存在 `recovery.json`；字段偏移见 `entry*.bindings.json`；材质完整字段见 `MAT_Corin_*.complete.json`。

常量偏移采用 [Microsoft HLSL packoffset 定义](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-variable-packoffset)；多目标输出采用 [Unity Shader semantics 中的 SV_TargetN 定义](https://docs.unity.cn/2022.2/Documentation/Manual/SL-ShaderSemantics.html)。这两项说明语法和绑定合同，不提供 ZZZ 的运行时参数值。
