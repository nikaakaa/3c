## 1. 实现
- [x] 1.1 确认沿用 URP Volume + Renderer Feature + Render Pass 路径。
- [x] 1.2 新建 Glitch Volume 参数组件。
- [x] 1.3 在参数组件中定义强度、块大小、水平抖动、RGB 分离、扫描线强度、速度和激活判定。
- [x] 1.4 新建 Glitch 参数值结构。
- [x] 1.5 新建 Glitch Renderer Feature。
- [x] 1.6 新建 Glitch Render Pass。
- [x] 1.7 将 Render Pass 执行时机放在内置后处理之前。
- [x] 1.8 新建 Glitch 全屏 shader。
- [x] 1.9 在 shader 内部生成伪随机块噪声，不依赖外部噪声贴图。
- [x] 1.10 将 shader 参数名与 C# pass 参数绑定统一。
- [x] 1.11 将 Renderer Feature 接入 High Fidelity、Balanced、Performant 三档 URP Renderer Data。
- [x] 1.12 在当前 Volume Profile 中准备可手动启用和调参的 Glitch 参数。
- [x] 1.13 新增 `Glitch Target` Rendering Layer 名称。
- [x] 1.14 新增 Glitch 目标遮罩参数：启用遮罩、遮罩影响、遮罩扩展。
- [x] 1.15 新增 Glitch Mask Render Pass，只将目标 Rendering Layer 渲染到 Mask RT。
- [x] 1.16 将 Mask RT 传入 Glitch 全屏 shader。
- [x] 1.17 在 shader 中使用原始 mask 和扰动 mask 控制局部 Glitch 混合。
- [x] 1.18 将局部 Glitch 筛选从 GameObject Layer 改为 Rendering Layer。
- [x] 1.19 移除 `GlitchTarget` 普通 GameObject Layer 配置。
- [x] 1.20 将 Glitch 主 pass 改为 `BeforeRenderingPostProcessing` 并通过 `Blitter.BlitCameraTexture` 写回相机颜色目标。
- [x] 1.21 将 Mask pass 改为只绑定单采样 Mask RT，不依赖相机 depth target。

## 2. 测试
- [x] 2.1 新建 Glitch EditMode 测试。
- [x] 2.2 测试默认参数不激活 Glitch。
- [x] 2.3 测试强度大于阈值时激活 Glitch。
- [x] 2.4 测试参数被限制在安全范围。
- [x] 2.5 测试 Renderer Feature 缺少 shader 时不创建有效 pass。
- [x] 2.6 测试 Renderer Feature 有 shader 时会创建有效 pass 配置。
- [x] 2.7 测试三档 URP Renderer Data 都引用 Glitch Renderer Feature 和 shader。
- [x] 2.8 运行定向 EditMode 测试并记录结果：`ThirdPersonRendering.Tests.GlitchTests`，9 passed。
- [x] 2.9 测试默认遮罩模式关闭。
- [x] 2.10 测试遮罩参数被限制在安全范围。
- [x] 2.11 测试三档 URP Renderer Data 都配置 `Glitch Target` Rendering Layer Mask。
- [x] 2.12 重新运行定向 EditMode 测试并记录结果：`ThirdPersonRendering.Tests.GlitchTests`，11 passed。
- [x] 2.13 重新运行定向 EditMode 测试并记录结果：`ThirdPersonRendering.Tests.GlitchTests`，12 passed。

## 3. 验证
- [x] 3.1 运行 `openspec validate add-urp-glitch-post-processing --strict --no-interactive`。
- [x] 3.2 在 Unity Editor 中刷新项目并确认无编译错误。
- [x] 3.3 确认 Glitch 已接入三档 URP Renderer Data。
- [x] 3.4 在 Volume Profile 中添加默认关闭的 Glitch 参数。
- [x] 3.5 提供手动验证步骤：将强度、水平抖动、RGB 分离调到明显值，确认画面出现故障抖动和色散。
- [x] 3.6 提供手动关闭步骤：将 Glitch active 关闭或强度调回 0，确认效果关闭。
- [x] 3.7 运行 `openspec validate add-urp-glitch-post-processing --strict --no-interactive`。
- [x] 3.8 在 Unity Editor 中刷新项目并确认无编译错误。
- [x] 3.9 运行定向 EditMode 测试并确认通过。
- [x] 3.10 提供局部 Glitch 手动验证步骤：将目标 Renderer 的 `Rendering Layer Mask` 勾选 `Glitch Target` 并启用 `Use Target Mask`。
- [x] 3.11 运行 `openspec validate add-urp-glitch-post-processing --strict --no-interactive`。
