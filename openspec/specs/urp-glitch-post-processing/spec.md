# urp-glitch-post-processing Specification

## Purpose
定义 URP Glitch 后处理的 Volume 参数、目标遮罩语义、Renderer Feature 接入和默认安全状态。
## Requirements
### Requirement: URP Glitch Volume 参数
系统 SHALL 提供一个 URP VolumeComponent，用于配置 Glitch 后处理的模式、强度、块大小、水平抖动、RGB 分离、扫描线强度、速度、斩击撕裂条带、横向拖影、亮部拉伸和撕裂方向。

#### Scenario: 默认不激活
- **WHEN** Glitch Volume 使用默认参数
- **THEN** Glitch 后处理不应入队执行

#### Scenario: 强度激活
- **WHEN** Glitch 强度大于激活阈值
- **THEN** Glitch 后处理应被视为激活

#### Scenario: 模式切换
- **WHEN** Glitch 模式设置为普通故障
- **THEN** shader MUST 保持现有块状故障、水平抖动、扫描线和 RGB 分离行为
- **AND** 斩击撕裂参数 MUST NOT 改变普通故障模式的默认输出

#### Scenario: 斩击撕裂参数
- **WHEN** Glitch 模式设置为斩击撕裂
- **THEN** RenderPass MUST 向 shader 提供斩击条带密度、横向拖影宽度、亮部拉伸强度、撕裂方向和模式混合强度

### Requirement: URP Glitch Renderer 接入
系统 SHALL 通过 ScriptableRendererFeature 和 ScriptableRenderPass 接入 Glitch 后处理，并使用现有 URP Renderer Data，不新增相机脚本或并行渲染路径。

#### Scenario: 三档 Renderer 可用
- **WHEN** 项目使用 High Fidelity、Balanced 或 Performant 任一质量档
- **THEN** 对应 URP Renderer Data 都应包含 Glitch Renderer Feature

#### Scenario: 内置后处理之前执行
- **WHEN** Glitch Renderer Feature 入队
- **THEN** Glitch Render Pass 应在内置后处理之前执行，并写回当前相机颜色目标

#### Scenario: 斩击撕裂沿用当前渲染链路
- **WHEN** Glitch 模式设置为斩击撕裂
- **THEN** 系统 MUST 仍通过现有 `GlitchRendererFeature` 和 `GlitchRenderPass` 渲染
- **AND** 系统 MUST NOT 新增独立 `SlashTear` 后处理 Renderer Feature

### Requirement: 无外部噪声贴图依赖
系统 SHALL 在第一版 Glitch shader 内部生成故障噪声和斩击撕裂条带噪声，不要求提供外部噪声贴图。

#### Scenario: 缺少噪声贴图仍可运行
- **WHEN** 项目中没有给 Glitch 提供噪声贴图资产
- **THEN** Glitch shader 仍能生成块状故障、水平抖动、扫描线和 RGB 分离效果

#### Scenario: 斩击撕裂缺少噪声贴图仍可运行
- **WHEN** Glitch 模式设置为斩击撕裂且没有外部噪声贴图
- **THEN** shader MUST 使用内部条带噪声生成局部横向撕裂和拖影

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

#### Scenario: 斩击撕裂局部预览
- **WHEN** Glitch 模式设置为斩击撕裂、目标遮罩开启且斩击预览载体位于 `Glitch Target` Rendering Layer
- **THEN** 斩击撕裂 MUST 只在预览载体遮罩范围及扰动扩展范围内出现
- **AND** 背景不应出现全屏斩击撕裂污染

### Requirement: Glitch 参数安全范围
系统 SHALL 对 Glitch 参数进行范围限制，避免非法参数导致不可控采样或过高成本。

#### Scenario: 参数钳制
- **WHEN** 输入超出范围的 Glitch 参数
- **THEN** 运行时设置应被限制在定义的安全范围内

#### Scenario: 遮罩扩展钳制
- **WHEN** 输入超出范围的遮罩扩展参数
- **THEN** 运行时设置应被限制在定义的安全范围内

#### Scenario: 斩击撕裂参数钳制
- **WHEN** 输入超出范围的斩击条带密度、横向拖影宽度、亮部拉伸强度、撕裂方向或模式混合强度
- **THEN** 运行时设置 MUST 将这些参数限制在定义的安全范围内

### Requirement: Glitch 可验证性
系统 SHALL 提供自动测试和手动验证步骤，确认 Glitch 参数、模式切换、Renderer 接入、目标遮罩和手动画面效果可验证。

#### Scenario: 自动测试
- **WHEN** 运行 `ThirdPersonRendering.Tests.GlitchTests`
- **THEN** 测试应覆盖默认激活、强度激活、参数钳制、模式归一化、斩击撕裂参数钳制、遮罩配置和 Renderer 接入

#### Scenario: 手动验证
- **WHEN** 在 `Assets/Scenes/Sandbox.unity` 的 Volume Profile 中启用 Glitch 并提高强度
- **THEN** Game View 应出现故障抖动、扫描线或 RGB 分离效果

#### Scenario: 局部手动验证
- **WHEN** 将一个目标 Renderer 的 `Rendering Layer Mask` 勾选 `Glitch Target` 并启用 Glitch 目标遮罩模式
- **THEN** Game View 应只在该物体附近出现故障效果，背景不应全屏故障

#### Scenario: 斩击撕裂手动验证
- **WHEN** 用户启用斩击撕裂预览载体、将 Glitch 模式切换为斩击撕裂并提高强度
- **THEN** Game View MUST 在预览载体范围内出现横向条带撕裂、横向拖影、扫描线和亮部拉伸
- **AND** 关闭预览载体或关闭目标遮罩后 MUST 能明确恢复到无局部斩击撕裂污染的画面

