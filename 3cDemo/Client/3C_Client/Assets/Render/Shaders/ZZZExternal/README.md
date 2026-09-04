# ZZZ 原始 Shader 候选

`NapAvatarStandard.asset` 是从 ZZZ 的 SerializedFile 原样复制的 Unity Shader 序列化资产，SHA-256 与离线导出源一致。它保留了原始解析属性、SubShader、Pass 和编译程序数据，不是重新写的 `.shader` 源码。它也保留在 `D:\ZZZ_Dump\output\corin_replication\20260904_exact_shader_archive_v1`，工程内副本由注册插件处理，避免材质长期停留在 `Hidden/InternalErrorShader`。

`可琳/可琳tex/ZZZ导出/Materials/Serialized` 下的两个材质按原始 JSON 的 27 个纹理槽、277 个浮点和 151 个颜色生成，并把能确认身份的 Body/Hair 三张主贴图绑定到工程 GUID。未知的 MatCap、Ramp、Mask 等引用保持为空。

`Assets/Plugins/Editor/ZZZExternalShaderRegistration.dll` 在编辑器域加载和 Shader 导入后调用 `ShaderUtil.RegisterShader`，再通过 Unity 序列化对象把两份材质的 `m_Shader` 重新绑定到同一个 Shader。只重导入材质不会修复这个引用，因此重绑定是必要步骤；它不改材质参数，也不替换 Shader。

原始资产来自 Unity 2019.4 SerializedFile，项目是 Unity 2022.3。此前仅验证了材质引用能解析为 `miHoYo/Character/NapAvatarStandard` 和 Shader 错误查询为空，不能据此判定 GPU 程序可运行。GameplayLab 已撤回原始预编译材质挂载，Body/Hair 恢复原工程材质；这只是恢复编辑器可用性，不代表原始 Shader 还原完成。

2026-09-04 的三次 Scene View 崩溃均报告 `GpuProgram creation error: shader program type is unrecognised`，随后在 `ShaderLab::Program::GetMatchingSubProgram` 发生 `SIGSEGV`。当前预编译程序不能直接作为本项目可用 Shader 发布。`ZZZToonOutlineRendererFeature` 新增的 `CharacterToonDeferred` 强制绘制入口已撤销；原有描边逻辑保留。撤销绘制只阻断这条触发路径，不等于原始材质已恢复可见，也不证明材质预览或其它 Pass 安全。

直接证据：`C:/Users/Lenovo/AppData/Local/Temp/Unity/Editor/Crashes/Crash_2026-09-04_071334389/Editor.log` 第 8305、8353、8355 行；同型崩溃另见 `Crash_2026-09-04_064647303` 和 `Crash_2026-09-04_070816660`。后续应先核对 GPU 程序类型、程序块格式和当前 Unity 的导入合同，再恢复实际绘制；缺贴图、C# 编译成功和注册成功均不能代替这项验证。

GameplayLab 的 `Corin_body_02` 实际曾引用 URP 默认 `Lit.mat`，现与 `Corin_body` 一样使用已有工程 `Corin_Body_D.mat`。通过正式编辑器接口读取确认后，已保存场景并将 Scene View 对准可琳完成一次截图，角色头发和衣服可见，没有触发上述崩溃。验证图为 `Diagnostics/Rendering/corin-safe-materials-20260904.png`。这只验证撤下原始预编译材质后的场景绘制，不验证原始 Shader 的任一 Pass，也不代表效果与 ZZZ 一致。
