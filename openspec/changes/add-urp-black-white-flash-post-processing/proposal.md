# Change: 新增 URP 黑白闪后处理

## Why
当前格挡和冲击表现已有暖色闪光、径向拖影、故障和边缘扫描等后处理能力，但缺少类似高速动作游戏中“瞬时黑白化、反白、强对比冲击帧”的统一表现。该能力需要接入现有 URP 后处理体系，先服务 Sandbox 预览和后续动作表现扩展。

## What Changes
- 新增 `Black White Flash` URP 后处理能力，通过全屏 Render Pass 采样当前相机颜色并输出高对比黑白/反白冲击效果。
- 使用 Volume 组件配置强度、模式、灰度阈值、对比度、白场增强、暗部压黑、反白程度、径向中心、半径和软边。
- 第一版支持全屏黑白闪和屏幕空间径向局部黑白闪；技术实现仍为全屏后处理，视觉影响范围由 shader mask 控制。
- 新增可调曲线播放层，通过 `BlackWhiteFlashProfile` 暴露持续时间、强度曲线、半径曲线、反白曲线和基础视觉参数。
- 新增 `BlackWhiteFlashController`，可在 Sandbox 中开箱播放默认黑白闪，并把曲线采样结果写入已有 Black White Flash Volume 参数。
- 通过当前 `ScriptableRendererFeature -> ScriptableRenderPass -> Hidden shader` 链路接入 URP Renderer Data，不新增相机脚本、`OnRenderImage`、额外相机叠加或并行渲染路径。
- 第一版不接入格挡、受击、闪避、动作状态机或动画事件；曲线播放只提供手动/预览入口，不实现目标 Rendering Layer Mask，不实现深度/法线描边。
- 提供 EditMode 测试覆盖参数钳制、默认关闭、激活判定、Renderer 接入和 shader 资产路径。
- 提供 Sandbox 手动验证步骤，确认全屏模式、径向局部模式、曲线播放和强度归零恢复原画面。

## Impact
- Affected specs: `urp-black-white-flash-post-processing`
- Affected code: `3cDemo/Client/3C_Client/Assets/Scripts/Rendering/Runtime`
- Affected shaders: `3cDemo/Client/3C_Client/Assets/Shader/PostProcessing/BlackWhiteFlash`
- Affected tests: `3cDemo/Client/3C_Client/Assets/Tests/Editor/Rendering`
- Affected settings: High Fidelity、Balanced、Performant 三档 URP Renderer Data，Sandbox Volume Profile
