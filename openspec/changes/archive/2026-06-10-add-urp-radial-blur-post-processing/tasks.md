## 1. 实现
- [x] 1.1 确认 URP Renderer Data 资产路径和接入点：`Assets/Settings/URP-HighFidelity-Renderer.asset`、`Assets/Settings/URP-Balanced-Renderer.asset`、`Assets/Settings/URP-Performant-Renderer.asset`。
- [x] 1.2 新建 `Assets/Scripts/Rendering/Runtime/` 目录。
- [x] 1.3 新建径向模糊 Volume 参数组件。
- [x] 1.4 在参数组件中定义强度、中心点、半径、采样次数和激活判定。
- [x] 1.5 新建径向模糊 Renderer Feature。
- [x] 1.6 新建径向模糊 Render Pass。
- [x] 1.7 将渲染 pass 的执行时机放在内置后处理之后；原因是该效果作为最终镜头表现反馈，应作用在 Bloom、Vignette 和 Tonemapping 后。
- [x] 1.8 新建 `Assets/Shader/PostProcessing/` 目录。
- [x] 1.9 新建径向模糊全屏 shader。
- [x] 1.10 将 shader 参数名与 C# pass 参数绑定统一。
- [x] 1.11 将 Renderer Feature 接入 High Fidelity、Balanced、Performant 三档 URP Renderer Data，避免质量档切换后效果缺失。
- [x] 1.12 在当前 Volume Profile 中准备可手动启用和调参的径向模糊参数。
- [x] 1.13 将 Render Pass 写回方式调整为拷贝当前颜色后显式绑定相机颜色目标绘制全屏三角形，保持与 URP 14 Full Screen Pass 的执行方式一致。

## 2. 测试
- [x] 2.1 新建 `Assets/Tests/Editor/Rendering/` 测试目录。
- [x] 2.2 测试默认参数不激活径向模糊。
- [x] 2.3 测试强度大于阈值时激活径向模糊。
- [x] 2.4 测试强度、半径和采样次数被限制在安全范围。
- [x] 2.5 测试 Renderer Feature 缺少 shader 时不入队 pass。
- [x] 2.6 测试 Renderer Feature 有 shader 且 Volume 激活时会创建有效 pass 配置。
- [x] 2.7 测试三档 URP Renderer Data 都引用径向模糊 Renderer Feature 和 shader。
- [x] 2.8 运行定向 EditMode 测试并记录结果：`ThirdPersonRendering.Tests.RadialBlurTests`，9 passed；与 Glitch 测试同轮重跑共 21 passed。

## 3. 验证
- [x] 3.1 运行 `openspec validate add-urp-radial-blur-post-processing --strict --no-interactive`。
- [x] 3.2 在 Unity Editor 中打开测试场景：`Assets/Scenes/Sandbox.unity`。
- [x] 3.3 确认相机启用 URP 后处理。
- [x] 3.4 在 Volume Profile 中启用 Radial Blur。
- [x] 3.5 将强度调到明显值，确认截图输出发生变化；临时截图与 baseline 哈希不同，验证后已清理截图产物。
- [x] 3.6 确认 `SampleSceneProfile.asset` 中 Radial Blur 参数可通过 Volume Profile 写入。
- [x] 3.7 调整中心点，确认参数可写入 Volume Profile。
- [x] 3.8 调整采样次数，确认参数可写入 Volume Profile 且无编译错误。
