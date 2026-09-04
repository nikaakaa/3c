# ZZZ 原始 Shader 候选

`NapAvatarStandard.asset` 是从 ZZZ 的 SerializedFile 原样复制的 Unity Shader 序列化资产，SHA-256 与离线导出源一致。它保留了原始解析属性、SubShader、Pass 和编译程序数据，不是重新写的 `.shader` 源码。它也保留在 `D:\ZZZ_Dump\output\corin_replication\20260904_exact_shader_archive_v1`，工程内副本由注册插件处理，避免材质长期停留在 `Hidden/InternalErrorShader`。

`可琳/可琳tex/ZZZ导出/Materials/Serialized` 下的两个材质按原始 JSON 的 27 个纹理槽、277 个浮点和 151 个颜色生成，并把能确认身份的 Body/Hair 三张主贴图绑定到工程 GUID。未知的 MatCap、Ramp、Mask 等引用保持为空。

`Assets/Plugins/Editor/ZZZExternalShaderRegistration.dll` 在编辑器域加载和 Shader 导入后调用 `ShaderUtil.RegisterShader`，再通过 Unity 序列化对象把两份材质的 `m_Shader` 重新绑定到同一个 Shader。只重导入材质不会修复这个引用，因此重绑定是必要步骤；它不改材质参数，也不替换 Shader。

原始资产来自 Unity 2019.4 SerializedFile，项目是 Unity 2022.3；GameplayLab 当前直接引用它。当前验证结果是 Shader 无编译错误，Body/Hair 的材质 Shader 均为 `miHoYo/Character/NapAvatarStandard`，不再是 `Hidden/InternalErrorShader`。
