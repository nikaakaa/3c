# ZZZ 原始 Shader 离线恢复

当前新增了 [原角色 LUT 的 Unity 绘制与保存验证](ZZZ-LUT恢复合同.md)。主体恢复仍未完成；不要把 LUT 或离线编译成功等同于角色还原完成。旧预编译导入失败链已移至 `Archive/Unity2019Precompiled/`，不再自动注册。

角色光照计算、MatCap Legacy 分支、当前模型的两材质到五 Renderer 映射，以及 2026-09-05 物理快照的精确缺口见 [角色渲染静态合同](ZZZ-角色渲染静态合同.md)。

`recover_zzz_shader.py` 负责从现有 ZZZ 导出记录恢复可审查的 D3D11 阶段源码和材质序列化字段。它不访问游戏进程、不调用 Unity、不修改场景或工程材质，不生成替代渲染效果。

## 输入、处理和输出

输入为原 Shader JSON、原始材质对象 `.dat`、用于交叉核对的材质 JSON，以及已有 HLSL 反编译 DLL。输出目录必须是新目录，不能覆盖旧结果。

处理顺序：

1. 解析 D3D11 的六个 LZ4 段，按段号、偏移、长度读取程序目录。
2. 读取程序自身版本、类型、全局和局部关键字，不用文件名猜程序变体。
3. 按 `m_BlobIndex` 找到 Pass、阶段、原参数名称表和常量布局。
4. 提取 DXBC，保存原始反汇编及已有反编译器的原文。原文只作失败对照，不再作为正式恢复源码。
5. `recover_dxbc_hlsl.py` 按十六进制指令常量、原始接口签名和逐条操作恢复源码。寄存器与结构化数据保留 `uint` 位值，只有浮点操作使用 `asfloat`，有符号操作使用 `asint`；不把位解释误写成数值转换。
6. 按原字节偏移恢复常量名称、矩阵列、数组及纹理名称。对原文、位值源码、具名源码分别 D3DCompile，保存诊断、DXBC、反汇编、指令来源表和原始输出签名。未支持的指令或绑定直接报错。
7. 单独读取 Unity 2019 ZZZ Material 的序列化前缀及属性表，对引用、属性名和 float32 数值逐项核对现有 JSON，保留关键字、队列、禁用 Pass、enabledPassMask 和原始偏移。

输出中的 `runtime_bound=false` 是明确边界。编译通过不等于 Unity 导入、绑定正确，更不等于原画面已复现。

## 2026-09-04 已完成的恢复

旧反编译对照记录为 `D:/ZZZ_Dump/output/corin_replication/20260904_named_shader_recovery_v9`。该记录不再是正式源码候选。

- Shader JSON SHA-256：`e5d5ea77a8536a15568e40ab2a4338683c82373ebee1f72416a68a0250cfc468`。
- D3D11 程序目录完整读取 4026 项；不是恢复了 4026 份源码。
- 10 个阶段的反编译原文和带名字源码均完成独立 D3DCompile，零编译错误。不是 Unity 编译验证。
- 旧 entry 24 有 X3578 输出未初始化警告；48、2700、2705 有 X4115 负数转无符号警告。后续已证明不是可忽略的格式问题：前者错误增加了输出 W；后者使材质位标记在编译后归零。
- 带名字源码和反编译原文重新编译后的指令块，只有 entry 16 完全相同。其它结果含声明次序、交换操作数、动态矩阵索引等差异；不能声明字节相等或已经证明全部数值等价。
- 原始 DXBC 和重新编译 DXBC 分开保存。此记录不证明反编译器已无损恢复原程序。

新位值恢复记录为 `20260904_typed_named_shader_v3`，上述 10 个阶段均零警告、零错误，输出签名逐项相同。描边 TEXCOORD5 保持原来的三维，材质位标记按原整数操作处理。它取代逐个修补旧反编译表达式的候选；旧输出目录仅作为不可覆盖的失败证据保留。

完整缺陷、计算程序及后级角色合成证据见 [恢复进度与接入边界](ZZZ-Shader恢复进度.md)。这些结果仍不是 Unity 实际绘制验收。

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

后续只读工作已完成这些入口及关键被调方法的定点提取，并补解旧快照中的参数表。最新的已闭合规则与仍有限制的部分见 [MatCap 恢复合同](ZZZ-MatCap恢复合同.md)，包括实际原图、四组数组排列、缓存层号覆盖、mip 复制及按材质/PropertyBlock 写入的边界。该文没有宣称当前游戏一定采用 Simplify 分支。

主 Pass 还依赖 `UnityNapCB`、`_NapEntityGPUData`、角色专用光照、级联/单角色阴影。后级 `DeferredShadingForCharacter` 的实际源程序已恢复，确认消费 GBuffer、深度、法线和角色 LUT；原生绘制调用的各 RT 绑定身份、格式和当前项目运行时生产者尚未全部闭合。不能把主 Pass 的 RGB 当成原游戏最终画面。

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

## MatCap 证据工具

- `inspect_zzz_native.py`：必须提供匹配的 PE SHA-256；按 `.pdata` 和 `UNW_FLAG_CHAININFO` 提取显式指定函数的全部关联片段，保存各片段地址与拼接偏移；不会将主序言的结束误当完整函数结束。不读取活体。
- `export_zzz_render_metadata.py`：复用原有 829 离线解析器，只导出显式指定的类型到新目录；可补取 MatCap 静态表和已验证 System.String 字面量，保存源文件哈希及每次读取的字节。不重写旧分析包。
- `export_zzz_container_nodes.ps1`：复用已验证 AnimeStudio mhy1 解码器，只提取指定资源节点，保留输入块、库和节点哈希；原始压缩 mip 数据与 PNG 预览分开。
- `capture_zzz_renderer_snapshot.py`：读取已有 829 离线快照，验证模块身份与对象名称 getter，保存 ShaderConfig 和 NapEntityPrepare 原始计算程序。不访问活体，不向 Unity 导入。
- `capture_zzz_shader_parameters.py`：按原 `PropertyToID` 哈希探测及 typed property sheet 布局读取指定阶段需要的全局参数，保存原始位值、查找过程与四路 RT 配置。区分已发布、未发布和线程局部数据；不把未发布字段填零。
- `recover_dxbc_hlsl.py`：独立恢复现有 DXBC 阶段，输出位值源码与编译对照。`--decompiled-dir` 仅用于已知计算程序旧入口缺失的失败对照；补入口不代表修正旧数值语义。
