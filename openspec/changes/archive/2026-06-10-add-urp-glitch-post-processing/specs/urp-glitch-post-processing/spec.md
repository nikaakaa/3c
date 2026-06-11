## ADDED Requirements
### Requirement: URP Glitch Volume 参数
系统 SHALL 提供一个 URP VolumeComponent，用于配置 Glitch 后处理的强度、块大小、水平抖动、RGB 分离、扫描线强度和速度。

#### Scenario: 默认不激活
- **WHEN** Glitch Volume 使用默认参数
- **THEN** Glitch 后处理不应入队执行

#### Scenario: 强度激活
- **WHEN** Glitch 强度大于激活阈值
- **THEN** Glitch 后处理应被视为激活

### Requirement: URP Glitch Renderer 接入
系统 SHALL 通过 ScriptableRendererFeature 和 ScriptableRenderPass 接入 Glitch 后处理，并使用现有 URP Renderer Data，不新增相机脚本或并行渲染路径。

#### Scenario: 三档 Renderer 可用
- **WHEN** 项目使用 High Fidelity、Balanced 或 Performant 任一质量档
- **THEN** 对应 URP Renderer Data 都应包含 Glitch Renderer Feature

#### Scenario: 内置后处理之前执行
- **WHEN** Glitch Renderer Feature 入队
- **THEN** Glitch Render Pass 应在内置后处理之前执行，并写回当前相机颜色目标

### Requirement: 无外部噪声贴图依赖
系统 SHALL 在第一版 Glitch shader 内部生成故障噪声，不要求提供外部噪声贴图。

#### Scenario: 缺少噪声贴图仍可运行
- **WHEN** 项目中没有给 Glitch 提供噪声贴图资产
- **THEN** Glitch shader 仍能生成块状故障、水平抖动、扫描线和 RGB 分离效果

### Requirement: 目标遮罩 Glitch
系统 SHALL 支持可选的目标遮罩模式，通过运行时 Mask RT 限定 Glitch 只影响指定 Rendering Layer 的 Renderer 区域及其故障扰动扩展区域。

#### Scenario: 遮罩模式关闭
- **WHEN** Glitch 的目标遮罩模式关闭
- **THEN** Glitch 应保持全屏后处理行为

#### Scenario: 遮罩模式开启
- **WHEN** Glitch 的目标遮罩模式开启且目标 Rendering Layer 中存在可见 Renderer
- **THEN** Glitch 应只在目标物体遮罩区域和扰动后的遮罩扩展区域混合故障结果

#### Scenario: 无目标物体
- **WHEN** Glitch 的目标遮罩模式开启但目标 Rendering Layer 中没有可见 Renderer
- **THEN** 画面不应出现全屏 Glitch 污染

### Requirement: Glitch 参数安全范围
系统 SHALL 对 Glitch 参数进行范围限制，避免非法参数导致不可控采样或过高成本。

#### Scenario: 参数钳制
- **WHEN** 输入超出范围的 Glitch 参数
- **THEN** 运行时设置应被限制在定义的安全范围内

#### Scenario: 遮罩扩展钳制
- **WHEN** 输入超出范围的遮罩扩展参数
- **THEN** 运行时设置应被限制在定义的安全范围内

### Requirement: Glitch 可验证性
系统 SHALL 提供自动测试和手动验证步骤，确认 Glitch 参数、Renderer 接入和手动画面效果可验证。

#### Scenario: 自动测试
- **WHEN** 运行 `ThirdPersonRendering.Tests.GlitchTests`
- **THEN** 测试应覆盖默认激活、强度激活、参数钳制、遮罩配置和 Renderer 接入

#### Scenario: 手动验证
- **WHEN** 在 `Assets/Scenes/Sandbox.unity` 的 Volume Profile 中启用 Glitch 并提高强度
- **THEN** Game View 应出现故障抖动、扫描线或 RGB 分离效果

#### Scenario: 局部手动验证
- **WHEN** 将一个目标 Renderer 的 `Rendering Layer Mask` 勾选 `Glitch Target` 并启用 Glitch 目标遮罩模式
- **THEN** Game View 应只在该物体附近出现故障效果，背景不应全屏故障
