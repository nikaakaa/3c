# Change: 为 Glitch 新增斩击撕裂模式

## Why
用户希望实现参考图中类似刀光冲击的横向撕裂、拖影和扫描线模糊效果。现有项目已经有 URP Glitch 后处理、目标遮罩 Mask RT 和扫描线/RGB 分离能力，因此该效果应作为 Glitch 的动作表现模式扩展，而不是新增一条独立后处理管线或复用热力扭曲。

## What Changes
- 扩展现有 Glitch 能力，新增 `SlashTear` 斩击撕裂模式，用于攻击瞬间的局部横向撕裂和拖影预览。
- 新增 Glitch 模式参数，使普通故障效果和斩击撕裂效果通过同一个 Volume/Settings/RendererFeature/RenderPass/shader 链路切换。
- 新增斩击撕裂参数：条带密度、横向拖影宽度、亮部拉伸强度、撕裂方向和模式混合强度。
- 沿用现有目标遮罩 Glitch：第一版通过 `Glitch Target` Rendering Layer 的目标 Renderer 限定局部效果，不新增独立 mask 管线。
- 提供一个最小预览载体，用于在 Sandbox 中摆放斩击区域或刀光占位物，方便手动观察局部撕裂效果。
- 提供 EditMode 测试覆盖模式归一化、参数钳制、shader 关键路径、Renderer 接入、Volume Profile 配置和预览载体。

## Impact
- Affected specs: `urp-glitch-post-processing`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime`
- Affected shaders: `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing/Glitch`
- Affected prefabs/effects: `3cDemo/Client/3C_Client/Assets/Prefabs/Rendering`
- Affected tests: `3cDemo/Client/3C_Client/Assets/Tests/Editor/Rendering`
- Affected settings: Sandbox Volume Profile、URP Renderer Data 中已有 Glitch Feature 配置
