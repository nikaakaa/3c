# Change: 新增 URP 故障后处理

## Why
项目已有径向模糊后处理链路后，需要继续补一个可调参的故障屏幕效果，用于受击、异常状态、电子干扰或技能反馈。

## What Changes
- 新增一个 URP Glitch 后处理能力。
- 通过 Volume 组件暴露强度、块大小、水平抖动、RGB 分离、扫描线强度和速度。
- 支持可选目标遮罩：只对指定 Rendering Layer 的 Renderer 区域及其扰动扩展区域应用 Glitch。
- 第一版 shader 内部生成伪随机噪声，不依赖外部噪声贴图。
- 通过现有 URP Renderer Feature 路径接入三档 Renderer Data。
- 新增 EditMode 测试覆盖参数钳制、激活判定和 Renderer 接入。

## Impact
- Affected specs: `urp-glitch-post-processing`
- Affected code: `Assets/Scripts/Rendering/Runtime/`, `Assets/Shader/PostProcessing/`, `Assets/Tests/Editor/Rendering/`
- Affected assets: `Assets/Settings/URP-HighFidelity-Renderer.asset`, `Assets/Settings/URP-Balanced-Renderer.asset`, `Assets/Settings/URP-Performant-Renderer.asset`, `Assets/Settings/SampleSceneProfile.asset`
