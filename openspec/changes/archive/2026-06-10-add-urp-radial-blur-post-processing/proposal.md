# Change: 新增 URP 径向模糊后处理

## Why
项目需要在继续推进角色逻辑前，先建立一条可展示、可调参、可测试的屏幕后处理能力，方便后续做冲刺、受击、蓄力、爆发等表现反馈。

## What Changes
- 新增一个 URP 径向模糊后处理能力。
- 通过 Volume 组件暴露强度、中心点、采样次数、半径和启停阈值。
- 通过 URP Renderer Feature 接入项目 URP Renderer，不绕过 URP 渲染链。
- 通过全屏 shader 实现径向采样模糊。
- 新增 EditMode 测试覆盖参数钳制、激活判定和渲染 pass 配置。

## Impact
- Affected specs: `urp-radial-blur-post-processing`
- Affected code: `Assets/Scripts/Rendering/Runtime/`, `Assets/Shader/PostProcessing/`, `Assets/Tests/Editor/Rendering/`
- Affected assets: `Assets/Settings/URP-HighFidelity-Renderer.asset`, `Assets/Settings/URP-Balanced-Renderer.asset`, `Assets/Settings/URP-Performant-Renderer.asset`, `Assets/Settings/SampleSceneProfile.asset`
