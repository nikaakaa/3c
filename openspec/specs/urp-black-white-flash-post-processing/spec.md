# urp-black-white-flash-post-processing Specification

## Purpose
TBD - created by archiving change add-urp-black-white-flash-post-processing. Update Purpose after archive.
## Requirements
### Requirement: URP 黑白闪 Volume 参数
系统 MUST 提供一个 URP VolumeComponent，用于配置黑白闪后处理的强度、模式、灰度阈值、对比度、白场增强、暗部压黑、反白程度、径向中心、径向半径和径向软边。

#### Scenario: 默认不激活
- **WHEN** Black White Flash Volume 使用默认参数
- **THEN** 黑白闪后处理 MUST NOT 入队执行
- **AND** 当前相机颜色结果 MUST NOT 被改写

#### Scenario: 强度激活
- **WHEN** Black White Flash Volume 已启用且强度大于激活阈值
- **THEN** 黑白闪后处理 MUST 被视为激活
- **AND** Renderer Feature MAY 入队对应 Render Pass

#### Scenario: 径向参数由 Volume 控制
- **WHEN** Black White Flash 模式设置为径向局部模式
- **THEN** Render Pass MUST 使用 Volume 中的径向中心、径向半径和径向软边参数
- **AND** 参数 MUST 在进入 shader 前被限制在安全范围内

### Requirement: URP 黑白闪 Renderer 接入
系统 MUST 通过当前 URP Renderer Feature 链路接入黑白闪后处理，并且 MUST NOT 使用 `OnRenderImage`、额外相机叠加、独立 CommandBuffer 管线或角色脚本作为主要渲染出口。

#### Scenario: Renderer Feature 接入当前 URP
- **WHEN** 项目启用黑白闪后处理
- **THEN** 黑白闪 MUST 作为 `ScriptableRendererFeature` 挂接到当前使用的 URP Renderer Data
- **AND** 渲染执行 MUST 通过 `ScriptableRenderPass` 完成
- **AND** 系统 MUST NOT 新增相机脚本或独立后处理路径

#### Scenario: 三档 Renderer 可用
- **WHEN** 项目使用 High Fidelity、Balanced 或 Performant 任一质量档
- **THEN** 对应 URP Renderer Data MUST 包含 Black White Flash Renderer Feature

#### Scenario: 内置后处理之前执行
- **WHEN** Black White Flash Renderer Feature 入队
- **THEN** Black White Flash Render Pass MUST 在内置后处理之前执行
- **AND** Render Pass MUST 写回当前相机颜色目标

### Requirement: 黑白闪 shader 行为
系统 MUST 使用全屏 shader 采样当前相机颜色，根据亮度、阈值和对比度生成高对比黑白结果，并按强度混合回当前画面。

#### Scenario: 全屏黑白闪
- **WHEN** 模式设置为全屏且强度大于 0
- **THEN** shader MUST 在整个屏幕范围内混合黑白/反白结果
- **AND** 原始彩色画面 MUST 按强度逐步过渡到黑白闪结果

#### Scenario: 径向局部黑白闪
- **WHEN** 模式设置为径向局部且强度大于 0
- **THEN** shader MUST 以屏幕空间中心、半径和软边生成径向 mask
- **AND** 径向 mask 外的画面 MUST 保持接近原始颜色

#### Scenario: 强度为零恢复原画面
- **WHEN** 黑白闪强度为 0
- **THEN** shader MUST 输出原始相机颜色

#### Scenario: 无外部贴图依赖
- **WHEN** 项目没有提供额外噪声、遮罩、深度或法线贴图给黑白闪
- **THEN** shader MUST 仍能生成全屏黑白闪和径向局部黑白闪效果

### Requirement: 黑白闪参数安全范围
系统 MUST 对黑白闪参数进行范围限制，避免非法参数导致不可控颜色输出、无效径向范围或不稳定渲染行为。

#### Scenario: 色调参数钳制
- **WHEN** 输入超出范围的强度、阈值、对比度、白场增强、暗部压黑或反白程度
- **THEN** Settings MUST 将这些参数钳制到定义的安全范围内

#### Scenario: 径向参数钳制
- **WHEN** 输入超出范围的径向中心、径向半径或径向软边
- **THEN** Settings MUST 将这些参数钳制到定义的安全范围内

#### Scenario: 模式归一化
- **WHEN** 输入非法黑白闪模式
- **THEN** Settings MUST 将模式归一化到受支持的有效模式

### Requirement: 黑白闪曲线播放
系统 MUST 提供一个可调曲线播放入口，用于把一次黑白闪的持续时间、强度曲线、半径曲线、反白曲线和基础视觉参数采样为已有 Black White Flash Volume 参数。

#### Scenario: 默认曲线可播放
- **WHEN** Sandbox 中的 Black White Flash Controller 使用默认 Profile 播放
- **THEN** Controller MUST 在播放开始时把黑白闪强度写入大于 0 的值
- **AND** 播放结束后 MUST 将强度恢复为 0

#### Scenario: 参数由 Profile 暴露
- **WHEN** 用户选择默认 Black White Flash Profile
- **THEN** Profile MUST 暴露持续时间、模式、屏幕中心、强度倍率、强度曲线、半径曲线、反白曲线、阈值、对比度、白场增强、暗部压黑、基础半径、峰值半径和软边
- **AND** Profile MUST 将采样结果限制在 Settings 定义的安全范围内

#### Scenario: Controller 写入已有 Volume
- **WHEN** Controller 播放曲线
- **THEN** Controller MUST 写入同一个 Black White Flash VolumeComponent
- **AND** Controller MUST NOT 新增独立后处理路径、额外相机、UI 输入绑定或动作事件桥接

#### Scenario: Sandbox 开箱可调
- **WHEN** 打开 `Assets/Scenes/Sandbox.unity`
- **THEN** `Global Volume` MUST 包含 Black White Flash Controller
- **AND** Controller MUST 引用默认 Black White Flash Profile 和 Sandbox 使用的 Volume

### Requirement: 黑白闪可验证性
系统 MUST 提供自动测试和手动验证步骤，确认黑白闪参数、Renderer 接入、shader 资产和手动画面效果可验证。

#### Scenario: 自动测试
- **WHEN** 运行 `ThirdPersonRendering.Tests.BlackWhiteFlashTests`
- **THEN** 测试 MUST 覆盖默认不激活、强度激活、参数钳制、模式归一化、Renderer 接入、shader 资产路径、曲线采样和 Sandbox Controller 配置

#### Scenario: 手动全屏验证
- **WHEN** 在 `Assets/Scenes/Sandbox.unity` 的 Volume Profile 中启用 Black White Flash 并选择全屏模式
- **THEN** Game View MUST 出现全屏黑白/反白冲击效果
- **AND** 将强度调回 0 后 MUST 恢复原始彩色画面

#### Scenario: 手动径向验证
- **WHEN** 在 `Assets/Scenes/Sandbox.unity` 的 Volume Profile 中选择径向局部模式并调整中心、半径和软边
- **THEN** Game View MUST 只在指定屏幕区域出现黑白/反白冲击效果
- **AND** 区域外画面 MUST 保持接近原始颜色

#### Scenario: 手动曲线播放验证
- **WHEN** 在 Sandbox 的 Black White Flash Controller 上执行默认播放
- **THEN** Game View MUST 出现一次随曲线衰减的黑白闪
- **AND** 用户调整默认 Profile 的曲线或参数后，再次播放 MUST 观察到对应变化

