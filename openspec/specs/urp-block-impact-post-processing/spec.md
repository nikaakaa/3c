# urp-block-impact-post-processing Specification

## Purpose
定义 URP 格挡屏幕冲击后处理的接入方式、参数抽象、shader 行为、与现有后处理共存和验证边界。
## Requirements
### Requirement: URP 格挡屏幕冲击接入
系统 MUST 通过当前 URP Renderer Feature 链路接入格挡屏幕冲击后处理，并且 MUST NOT 使用 `OnRenderImage`、额外相机叠加、独立 CommandBuffer 管线或角色脚本作为主要渲染出口。

#### Scenario: Renderer Feature 接入当前 URP
- **WHEN** 项目启用格挡屏幕冲击后处理
- **THEN** 格挡屏幕冲击 MUST 作为 `ScriptableRendererFeature` 挂接到当前使用的 URP Renderer Data
- **AND** 渲染执行 MUST 通过 `ScriptableRenderPass` 完成
- **AND** 系统 MUST NOT 新增相机脚本或独立后处理路径

#### Scenario: 默认不改变画面
- **WHEN** 格挡屏幕冲击 Volume 使用默认参数或没有有效冲击脉冲
- **THEN** 格挡屏幕冲击 pass MUST NOT 入队执行
- **AND** 当前相机颜色结果 MUST NOT 被改写

### Requirement: 格挡屏幕冲击参数抽象
系统 MUST 使用 Volume 组件和运行时脉冲参数共同表达格挡屏幕冲击配置，使全局强度、屏幕中心、径向冲击强度、白闪强度、横向光带强度、色散强度、半径、采样次数和衰减由配置层控制，渲染实现只消费已经规范化的 settings。

#### Scenario: Volume 控制全局强度
- **WHEN** Volume Profile 中启用格挡屏幕冲击且全局强度大于激活阈值
- **THEN** RenderPass MUST 使用 Volume 输出的规范化 settings
- **AND** 参数 MUST 在进入 shader 前被限制在安全范围内

#### Scenario: Prefab 提交运行时脉冲
- **WHEN** `BlockImpactVfx` Prefab 播放一次格挡冲击并启用屏幕冲击
- **THEN** Prefab MUST 向屏幕冲击后处理提交屏幕空间命中中心和脉冲强度
- **AND** 脉冲 MUST 按配置持续时间衰减到 0

### Requirement: 格挡屏幕冲击 shader 行为
系统 MUST 使用全屏 shader 对当前相机颜色执行短白闪、径向采样拖影、横向 streak 增亮和轻微 RGB 色散。shader MUST 保持采样次数受控，MUST 在强度为 0 时输出原始颜色。

#### Scenario: 径向冲击
- **WHEN** 径向冲击强度大于 0
- **THEN** shader MUST 以屏幕中心为焦点沿 UV 径向方向采样当前颜色
- **AND** 越接近冲击中心或配置半径内的像素 MUST 获得更明显冲击响应

#### Scenario: 短白闪和横向光带
- **WHEN** 白闪或横向光带强度大于 0
- **THEN** shader MUST 在短时间内提升画面亮部或沿屏幕横向叠加强光带
- **AND** 效果 MUST 随脉冲衰减快速消失

#### Scenario: 色散受控
- **WHEN** 色散强度大于 0
- **THEN** shader MUST 对 RGB 通道使用受控偏移
- **AND** 色散偏移 MUST 被钳制，避免画面严重错位

### Requirement: 与现有 URP 后处理共存
系统 MUST 让格挡屏幕冲击与 Radial Blur、Local Heat Distortion、Glitch 和 Edge Scan 共存。Renderer Data 修改 MUST 保留已有 Renderer Feature，不得覆盖或删除其他后处理配置。

#### Scenario: 三档 Renderer 可用
- **WHEN** 项目使用 High Fidelity、Balanced 或 Performant 任一质量档
- **THEN** 对应 URP Renderer Data MUST 包含格挡屏幕冲击 Renderer Feature
- **AND** Renderer Feature MUST 默认不产生画面变化，直到 Volume 参数和运行时脉冲同时有效

#### Scenario: 不覆盖已有 Feature
- **WHEN** 实施本变更时 Renderer Data 已包含 Radial Blur、Local Heat Distortion、Glitch 或 Edge Scan
- **THEN** 本变更 MUST 保留这些已有 Feature
- **AND** 系统 MUST NOT 为格挡屏幕冲击创建第二套 Renderer Data

### Requirement: 格挡屏幕冲击可验证性
系统 MUST 提供自动测试和手动验证步骤，确认参数钳制、默认不激活、Renderer 接入、shader 关键路径、运行时脉冲和手动画面效果可验证。

#### Scenario: 自动测试
- **WHEN** 运行 `ThirdPersonRendering.Tests.BlockImpactPostProcessingTests`
- **THEN** 测试 MUST 覆盖默认 settings 不激活、正强度激活、参数钳制、Volume 配置、Renderer Feature 缺 shader 不渲染、三档 Renderer Data 引用、shader 关键属性和运行时脉冲衰减

#### Scenario: 手动验证
- **WHEN** 用户在 Sandbox 中启用格挡屏幕冲击并触发 `BlockImpactVfx` Prefab 预览
- **THEN** Game View MUST 出现短暂白闪、径向冲击、横向光带或轻微色散
- **AND** 用户将全局强度或脉冲强度调回 0 后画面 MUST 恢复无屏幕冲击
