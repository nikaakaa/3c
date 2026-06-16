## 1. 规格确认
- [x] 1.1 阅读本变更的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 对照现有 `RadialBlur`、`Glitch`、`BlockImpactPostProcess` 的 Runtime、Shader 和测试结构。
- [x] 1.3 确认第一版范围只包含全屏模式和径向局部模式。
- [x] 1.4 确认不接入格挡、受击、闪避、动作状态机、动画事件、目标 Mask 或深度法线描边。

## 2. Runtime 参数层
- [x] 2.1 新建 `BlackWhiteFlashMode` 枚举，至少包含 `FullScreen` 和 `RadialImpact`。
- [x] 2.2 新建 Volume 参数组件 `BlackWhiteFlash`。
- [x] 2.3 为 Volume 参数添加强度、模式、阈值、对比度、白场增强、暗部压黑、反白程度、中心、半径和软边。
- [x] 2.4 新建 `BlackWhiteFlashSettings` 值结构。
- [x] 2.5 在 Settings 中定义所有参数的最小值、最大值、默认禁用值和激活阈值。
- [x] 2.6 在 Settings 中实现参数钳制和模式归一化。
- [x] 2.7 在 Settings 中提供 shader 参数打包方法。
- [x] 2.8 确认默认参数不会激活后处理。

## 3. Renderer 接入
- [x] 3.1 新建 `BlackWhiteFlashRendererFeature`。
- [x] 3.2 新建 `BlackWhiteFlashRenderPass`。
- [x] 3.3 Renderer Feature 从 Volume Stack 读取 `BlackWhiteFlash` 并转为 Settings。
- [x] 3.4 Renderer Feature 在默认关闭或强度为 0 时不入队 Render Pass。
- [x] 3.5 Render Pass 使用当前相机颜色作为输入并写回当前相机颜色目标。
- [x] 3.6 Render Pass 使用项目现有 `RTHandle`/`Blitter` 风格，避免新增并行渲染路径。
- [x] 3.7 Render Pass 默认执行点设为 `BeforeRenderingPostProcessing`。
- [x] 3.8 Renderer Feature 的 shader 路径使用 `Hidden/3C/PostProcessing/BlackWhiteFlash`。

## 4. Shader
- [x] 4.1 新建 `Assets/Shader/PostProcessing/BlackWhiteFlash/BlackWhiteFlash.shader`。
- [x] 4.2 shader 采样当前相机颜色。
- [x] 4.3 shader 将颜色转换为亮度值。
- [x] 4.4 shader 根据阈值和对比度生成高对比黑白结果。
- [x] 4.5 shader 支持白场增强和暗部压黑。
- [x] 4.6 shader 支持受控反白混合。
- [x] 4.7 shader 支持全屏模式。
- [x] 4.8 shader 支持径向局部模式，通过中心、半径和软边限制影响范围。
- [x] 4.9 shader 在强度为 0 时输出原始颜色。
- [x] 4.10 shader 不依赖外部噪声贴图、Mask RT、深度纹理或法线纹理。

## 5. 项目配置
- [x] 5.1 将 Black White Flash Renderer Feature 加入 High Fidelity URP Renderer Data。
- [x] 5.2 将 Black White Flash Renderer Feature 加入 Balanced URP Renderer Data。
- [x] 5.3 将 Black White Flash Renderer Feature 加入 Performant URP Renderer Data。
- [x] 5.4 在 Sandbox 使用的 Volume Profile 中添加默认关闭的 Black White Flash 参数。
- [x] 5.5 确认没有新增 fallback 配置或额外相机路径。

## 6. 自动测试
- [x] 6.1 新建 `ThirdPersonRendering.Tests.BlackWhiteFlashTests`。
- [x] 6.2 测试默认 Settings 不激活。
- [x] 6.3 测试强度大于阈值时激活。
- [x] 6.4 测试强度、阈值、对比度、白场增强、暗部压黑、反白程度、半径和软边被钳制。
- [x] 6.5 测试非法模式会归一化到有效模式。
- [x] 6.6 测试全屏模式 shader 参数正确打包。
- [x] 6.7 测试径向局部模式 shader 参数正确打包。
- [x] 6.8 测试 Renderer Feature 在默认关闭时不入队。
- [x] 6.9 测试 Renderer Feature 在有效强度时可入队。
- [x] 6.10 测试三档 URP Renderer Data 都配置 Black White Flash Renderer Feature。
- [x] 6.11 测试 Hidden shader 资产路径可加载。
- [x] 6.12 运行定向 EditMode 测试：`ThirdPersonRendering.Tests.BlackWhiteFlashTests`，20 passed。

## 7. 手动验证
- [x] 7.1 手动验证步骤已明确：打开 `Assets/Scenes/Sandbox.unity`。
- [x] 7.2 手动验证步骤已明确：在 Sandbox Volume Profile 中启用 Black White Flash。
- [x] 7.3 手动验证步骤已明确：将模式设为 `FullScreen`，提高强度，确认 Game View 出现全屏黑白/反白冲击效果。
- [x] 7.4 手动验证步骤已明确：调整阈值、对比度、白场增强、暗部压黑和反白程度，确认画面变化可观察。
- [x] 7.5 手动验证步骤已明确：将模式设为 `RadialImpact`，调整中心、半径和软边，确认效果只在屏幕指定区域混合。
- [x] 7.6 手动验证步骤已明确：将强度调回 0，确认 Game View 恢复原始彩色画面。
- [x] 7.7 手动验证步骤已明确：切换 High Fidelity、Balanced、Performant 质量档，确认三档都可启用同一能力。

## 8. 收尾
- [x] 8.1 运行 `openspec validate add-urp-black-white-flash-post-processing --strict --no-interactive`。
- [x] 8.2 记录定向 EditMode 测试结果：`ThirdPersonRendering.Tests.BlackWhiteFlashTests`，20 passed。
- [x] 8.3 记录 Sandbox 手动验证方式：由用户按第 7 节步骤在 Editor Game View 中做主观画面验收。
- [x] 8.4 确认所有任务完成后再将任务勾选为完成。

## 9. 曲线播放规格补充
- [x] 9.1 补充 `proposal.md`，明确黑白闪需要可调曲线播放层。
- [x] 9.2 补充 `design.md`，明确 Profile/Controller 与 Volume/RenderPass 的职责边界。
- [x] 9.3 补充 spec delta，新增黑白闪曲线播放需求和验证场景。
- [x] 9.4 运行 `openspec validate add-urp-black-white-flash-post-processing --strict --no-interactive`，确认规格仍有效。

## 10. 曲线 Profile
- [x] 10.1 新建 `BlackWhiteFlashProfile` ScriptableObject。
- [x] 10.2 暴露持续时间、模式、屏幕中心和强度倍率。
- [x] 10.3 暴露强度曲线、径向半径曲线和反白曲线。
- [x] 10.4 暴露阈值、对比度、白场增强、暗部压黑、基础半径、峰值半径和软边。
- [x] 10.5 在 Profile 中实现参数钳制和曲线缺省保护。
- [x] 10.6 在 Profile 中实现采样到 `BlackWhiteFlashSettings` 的方法。
- [x] 10.7 新建默认 `Assets/Settings/BlackWhiteFlashProfile.asset`。

## 11. 曲线 Controller
- [x] 11.1 新建 `BlackWhiteFlashController`。
- [x] 11.2 Controller 持有目标 Volume、默认 Profile、`playOnEnable` 和停止后恢复强度配置。
- [x] 11.3 Controller 支持 `PlayDefault`、按屏幕中心播放、停止和逐帧 Tick。
- [x] 11.4 Controller 播放时只写入已有 Black White Flash Volume 参数。
- [x] 11.5 Controller 停止时默认把强度恢复为 0。
- [x] 11.6 Controller 支持编辑器 Context Menu 预览。
- [x] 11.7 将 Controller 挂到 Sandbox 的 `Global Volume`，并引用默认 Profile。
- [x] 11.8 确认没有新增输入绑定、动作事件桥接、额外相机或 fallback 配置。

## 12. 曲线自动测试
- [x] 12.1 测试默认 Profile 在开始采样时激活黑白闪。
- [x] 12.2 测试默认 Profile 在结束采样时强度归零。
- [x] 12.3 测试 Profile 将曲线采样和参数钳制到安全范围。
- [x] 12.4 测试 Controller 播放时写入 Volume 参数。
- [x] 12.5 测试 Controller 播放结束后恢复强度为 0。
- [x] 12.6 测试默认 Profile 资产存在并引用正确脚本。
- [x] 12.7 测试 Sandbox 场景包含 Black White Flash Controller、默认 Profile 引用和目标 Volume 引用。
- [x] 12.8 运行定向 EditMode 测试：`ThirdPersonRendering.Tests.BlackWhiteFlashTests`，20 passed。

## 13. 曲线手动验证与收尾
- [x] 13.1 手动验证步骤已明确：打开 `Assets/Scenes/Sandbox.unity` 并选择 `Global Volume`。
- [x] 13.2 手动验证步骤已明确：在 Black White Flash Controller 上执行 `Play Default`。
- [x] 13.3 手动验证步骤已明确：调整默认 Profile 的强度曲线、半径曲线或反白曲线后再次播放。
- [x] 13.4 手动验证步骤已明确：播放结束后确认画面恢复彩色。
- [x] 13.5 运行 `openspec validate add-urp-black-white-flash-post-processing --strict --no-interactive`。
- [x] 13.6 记录定向 EditMode 测试结果：`ThirdPersonRendering.Tests.BlackWhiteFlashTests`，20 passed。
- [x] 13.7 确认所有新增任务完成后再将新增任务勾选为完成。
