## 1. 规格和边界
- [x] 1.1 读取 `urp-glitch-post-processing` 和 `urp-radial-blur-post-processing` 规格，确认不新增独立渲染路径。
- [x] 1.2 确认 `LocalHeatDistortion` 只作为渲染预览能力，不接入动作状态机。
- [x] 1.3 确认候选模式命名：热浪折射、螺旋风压、脉冲冲击、纵向上升气流。
- [x] 1.4 确认该能力使用区域源表达一片空气范围，不复用 Glitch 的目标 Renderer 遮罩语义。

## 2. 参数抽象
- [x] 2.1 新增 `LocalHeatDistortionMode` 枚举。
- [x] 2.2 新增 `LocalHeatDistortionSettings` 纯数据结构。
- [x] 2.3 新增 `LocalHeatDistortionAreaSettings` 纯数据结构，用于表达区域中心、半径、椭圆比例、方向和软边。
- [x] 2.4 定义强度、半径、速度、噪声尺度、折射幅度、区域软边和粒子强度的安全范围。
- [x] 2.5 实现参数归一化和 `IsActive` 判定。
- [x] 2.6 保持默认参数不激活且不改变画面。

## 3. Volume 配置
- [x] 3.1 新增 `LocalHeatDistortion` VolumeComponent。
- [x] 3.2 暴露候选模式参数。
- [x] 3.3 暴露强度、速度、噪声尺度、折射幅度参数。
- [x] 3.4 暴露区域软边、全局粒子可见强度和预览调试开关。
- [x] 3.5 让 Volume 输出 `NormalizedSettings`，不在 RenderPass 中读取原始 Volume 参数。

## 4. 区域源和粒子预览
- [x] 4.1 新增 `LocalHeatDistortionAreaSource`，作为场景中可摆放的区域源组件。
- [x] 4.2 支持圆形/椭圆屏幕投影区域。
- [x] 4.3 支持圆柱风压载体区域。
- [x] 4.4 支持区域源覆盖候选模式，也支持使用 Volume 中的全局模式。
- [x] 4.5 支持区域源启停，不启用时不参与渲染。
- [x] 4.6 新增热浪折射预览 ParticleSystem prefab。
- [x] 4.7 新增螺旋风压预览 ParticleSystem prefab。
- [x] 4.8 新增脉冲冲击预览 ParticleSystem prefab。
- [x] 4.9 新增纵向上升气流预览 ParticleSystem prefab。
- [x] 4.10 粒子可见层可单独关闭，关闭后后处理折射仍可工作。

## 5. Renderer Feature 接入
- [x] 5.1 新增 `LocalHeatDistortionRendererFeature`。
- [x] 5.2 新增 `LocalHeatDistortionRenderPass`。
- [x] 5.3 新增区域 Mask RenderPass 或区域参数上传路径，不新增相机或 `OnRenderImage`。
- [x] 5.4 强度为 0、材质缺失或没有启用区域源时不入队。
- [x] 5.5 区域源不可见或不在相机视锥内时不污染全屏。
- [x] 5.6 将 Feature 挂到 High Fidelity、Balanced、Performant 三个 URP Renderer Data。

## 6. Shader 候选效果
- [x] 6.1 新增 `Hidden/3C/PostProcessing/LocalHeatDistortion` shader。
- [x] 6.2 实现热浪折射：低频噪声、轻微横向/纵向 UV 扰动。
- [x] 6.3 实现螺旋风压：围绕区域中心产生旋转扰动。
- [x] 6.4 实现脉冲冲击：从区域中心向外扩散的环形扰动。
- [x] 6.5 实现纵向上升气流：向上流动的条带和热空气偏移。
- [x] 6.6 所有模式都受同一套强度、半径、速度、软边和区域参数控制。
- [x] 6.7 缺少外部噪声贴图时仍可运行。

## 7. 示例配置
- [x] 7.1 在 Sandbox Volume Profile 添加默认关闭的 `Local Heat Distortion`。
- [x] 7.2 在 Sandbox 中提供一个默认关闭的 `LocalHeatDistortionPreviewArea`。
- [x] 7.3 预览区域包含范围载体和四套候选 ParticleSystem。
- [x] 7.4 保持默认场景打开后没有局部热力扭曲污染。

## 8. 自动测试
- [x] 8.1 新增 `LocalHeatDistortionTests`。
- [x] 8.2 测试默认 settings 不激活。
- [x] 8.3 测试正强度 settings 激活。
- [x] 8.4 测试非法参数会被钳制到安全范围。
- [x] 8.5 测试四种候选模式都能被归一化。
- [x] 8.6 测试区域源默认不参与渲染。
- [x] 8.7 测试启用区域源会生成有效区域参数。
- [x] 8.8 测试 Volume 默认不激活。
- [x] 8.9 测试 Volume 正强度激活。
- [x] 8.10 测试 Renderer Feature 在材质缺失时不渲染。
- [x] 8.11 测试三个质量档 Renderer Data 引用 Feature 和 shader。
- [x] 8.12 测试 Sandbox Volume Profile 包含默认关闭的配置。
- [x] 8.13 测试预览 prefab 包含范围载体和 ParticleSystem。
- [x] 8.14 测试 Sandbox 包含默认关闭的预览区域。
- [x] 8.15 测试区域设置会携带区域源深度。
- [x] 8.16 测试区域源会从相机投影结果生成有效深度参数。
- [x] 8.17 测试 RenderPass 请求相机深度输入。
- [x] 8.18 测试 shader 引用相机深度贴图并执行深度比较。

## 9. 手动验证
- [x] 9.1 打开 `Assets/Scenes/Sandbox.unity`。
- [x] 9.2 启用 `LocalHeatDistortionPreviewArea`。
- [x] 9.3 在 Volume Profile 中启用 `Local Heat Distortion`，将强度从 0 提高到 0.2。
- [x] 9.4 逐个切换热浪折射、螺旋风压、脉冲冲击、纵向上升气流，确认 Game View 画面有不同区域扭曲。
- [x] 9.5 分别开启对应 ParticleSystem，确认可见气流和后处理折射属于同一片区域。
- [x] 9.6 移动、缩放、旋转预览区域，确认扭曲区域随区域源变化。
- [x] 9.7 关闭粒子可见层，确认后处理折射仍可独立观察。
- [x] 9.8 将强度调回 0，确认画面恢复无扭曲。
- [x] 9.9 在区域源和相机之间放置墙体或不透明立方体，确认前景物体不被后方扭曲覆盖。
- [x] 9.10 将区域源移动到无遮挡位置，确认扭曲重新可见。
- [x] 9.11 记录用户选择的候选模式，作为后续动作事件接入变更输入。

## 10. 校验
- [x] 10.1 运行 `openspec validate add-local-heat-distortion-preview --strict --no-interactive`。
- [x] 10.2 使用 Unity Test Runner 运行 `ThirdPersonRendering.Tests.LocalHeatDistortionTests`。
- [x] 10.3 不运行 Unity batchmode。
