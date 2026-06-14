# urp-local-heat-distortion-post-processing Specification

## Purpose
定义 URP 区域型热力扭曲预览能力，包括 Volume 参数、区域源、多候选扭曲模式、遮挡规则、粒子可见层和验证边界。
## Requirements
### Requirement: URP 区域型热力扭曲接入
系统 MUST 通过当前 URP Renderer Feature 链路接入区域型热力扭曲后处理，并且 MUST NOT 使用 `OnRenderImage`、额外相机叠加、独立 CommandBuffer 管线或角色脚本作为主要渲染出口。

#### Scenario: Renderer Feature 接入当前 URP
- **WHEN** 项目启用区域型热力扭曲
- **THEN** 区域型热力扭曲 MUST 作为 `ScriptableRendererFeature` 挂接到当前使用的 URP Renderer Data
- **AND** 渲染执行 MUST 通过 `ScriptableRenderPass` 完成
- **AND** 系统 MUST NOT 新增相机脚本或独立后处理路径

#### Scenario: 默认不改变画面
- **WHEN** 区域型热力扭曲 Volume 使用默认参数或场景中没有启用的区域源
- **THEN** 区域型热力扭曲 pass MUST NOT 入队执行
- **AND** 当前相机颜色结果 MUST NOT 被改写

### Requirement: 区域型热力扭曲参数抽象
系统 MUST 使用 Volume 组件表达区域型热力扭曲的全局配置，使候选模式、强度、速度、噪声尺度、折射幅度、区域软边和粒子可见强度由配置层控制，渲染实现只消费已经归一化的 settings。

#### Scenario: Volume 控制区域型热力扭曲
- **WHEN** Volume Profile 中启用区域型热力扭曲且强度大于激活阈值
- **THEN** RenderPass MUST 使用 Volume 输出的归一化 settings
- **AND** 参数 MUST 在进入 shader 前被限制在安全范围内

#### Scenario: 参数钳制
- **WHEN** 输入超出范围的强度、速度、噪声尺度、折射幅度、区域软边或粒子可见强度
- **THEN** settings MUST 将这些参数钳制到定义的安全范围内

### Requirement: 热力扭曲区域源
系统 MUST 通过场景中的区域源表达一片空气扰动范围，使热力扭曲区别于 Glitch 的目标物体遮罩。

#### Scenario: 区域源定义范围
- **WHEN** 场景中启用了 `LocalHeatDistortionAreaSource`
- **THEN** 系统 MUST 使用区域源的位置、旋转、缩放、半径和形状生成局部折射区域
- **AND** 扭曲 MUST 被限制在区域源表达的一片空气范围内

#### Scenario: 区域源不可见或关闭
- **WHEN** 区域源被禁用、超出相机视锥或没有有效半径
- **THEN** 系统 MUST NOT 因该区域源产生全屏扭曲污染

#### Scenario: 不复用 Glitch 目标遮罩
- **WHEN** 项目同时存在 Glitch 目标物体和热力扭曲区域源
- **THEN** 热力扭曲 MUST 由区域源决定范围
- **AND** 系统 MUST NOT 将任意目标 Renderer 当作热力扭曲区域

### Requirement: 多候选扭曲模式
系统 MUST 在同一套区域型热力扭曲能力中提供多种候选扭曲模式，供用户在预览阶段切换选择。

#### Scenario: 热浪折射
- **WHEN** 候选模式设置为热浪折射且强度大于 0
- **THEN** shader MUST 使用流动噪声产生轻微局部 UV 折射

#### Scenario: 螺旋风压
- **WHEN** 候选模式设置为螺旋风压且强度大于 0
- **THEN** shader MUST 围绕局部区域产生旋转方向的 UV 扰动

#### Scenario: 脉冲冲击
- **WHEN** 候选模式设置为脉冲冲击且强度大于 0
- **THEN** shader MUST 产生从局部中心向外扩散的环形 UV 扰动

#### Scenario: 纵向上升气流
- **WHEN** 候选模式设置为纵向上升气流且强度大于 0
- **THEN** shader MUST 产生向上流动的局部条带式 UV 扰动

### Requirement: 区域范围限定
系统 MUST 支持区域遮罩或区域参数限定扭曲范围，使折射只发生在区域源表达的一片空气范围中。

#### Scenario: 区域参数路径
- **WHEN** 区域源可以被表达为屏幕空间圆形或椭圆区域
- **THEN** RenderPass MAY 直接上传区域中心、半径、椭圆比例、方向和软边参数
- **AND** shader MUST 在区域外保持原始画面

#### Scenario: 区域 Mask 路径
- **WHEN** 区域源需要圆柱风压载体或更复杂区域
- **THEN** Renderer Feature MAY 绘制区域 Mask RT
- **AND** Mask RT MUST 只来自热力扭曲区域载体，而不是任意目标物体 Renderer

#### Scenario: 无区域源
- **WHEN** Volume 激活但场景中没有启用的热力扭曲区域源
- **THEN** 画面 MUST NOT 出现全屏热力扭曲污染

### Requirement: 不透明场景遮挡
系统 MUST 支持基于相机深度贴图的遮挡裁剪，使区域型热力扭曲不会穿过写入深度的不透明墙体、地形或场景物体覆盖前景。

#### Scenario: 区域源被不透明物体遮挡
- **WHEN** `LocalHeatDistortionAreaSource` 位于相机可见不透明物体后方
- **THEN** shader MUST 使用相机深度和区域源深度比较裁掉被前景遮挡的扭曲区域
- **AND** 前景不透明物体 MUST NOT 被后方热力扭曲覆盖

#### Scenario: 区域源未被遮挡
- **WHEN** `LocalHeatDistortionAreaSource` 位于相机前方且没有更近的不透明深度遮挡
- **THEN** shader MUST 保持原有区域型扭曲效果

#### Scenario: 不新增粒子折射路径
- **WHEN** 系统实现热力扭曲遮挡
- **THEN** 遮挡支持 MUST 沿用当前 `VolumeComponent -> Settings -> ScriptableRendererFeature -> ScriptableRenderPass -> Hidden shader` 链路
- **AND** 系统 MUST NOT 为本修复新增独立粒子折射 shader 作为主要渲染出口

### Requirement: 粒子可见层
系统 MUST 支持可选 ParticleSystem 作为区域型热力扭曲的可见层，用于展示热浪边缘、尘雾、风线和冲击线，但折射计算 MUST NOT 依赖粒子才能工作。

#### Scenario: 粒子开启
- **WHEN** 用户启用区域源的 ParticleSystem 可见层
- **THEN** Game View MUST 在同一片区域中看到粒子气流提示和后处理折射

#### Scenario: 粒子关闭
- **WHEN** 用户关闭区域源的 ParticleSystem 可见层
- **THEN** 后处理折射 MUST 仍可独立工作
- **AND** 用户 MUST 能单独观察 shader 扭曲强度

### Requirement: 无外部噪声贴图依赖
系统 MUST 在第一版区域型热力扭曲 shader 内部生成噪声和流动扰动，不要求提供外部噪声贴图资产。

#### Scenario: 缺少噪声贴图仍可运行
- **WHEN** 项目中没有给区域型热力扭曲提供噪声贴图资产
- **THEN** shader MUST 仍能生成热浪折射、螺旋风压、脉冲冲击和纵向上升气流候选效果

### Requirement: 区域型热力扭曲可验证性
系统 MUST 提供自动测试和手动验证步骤，确认区域型热力扭曲参数、区域源、粒子可见层、候选模式、Renderer 接入和手动画面效果可验证。

#### Scenario: 自动测试
- **WHEN** 运行 `ThirdPersonRendering.Tests.LocalHeatDistortionTests`
- **THEN** 测试 MUST 覆盖默认激活、强度激活、参数钳制、区域源参数、区域源深度参数、候选模式归一化、Renderer 接入、粒子预览 prefab 和示例场景配置

#### Scenario: 手动验证
- **WHEN** 在 `Assets/Scenes/Sandbox.unity` 的 Volume Profile 中启用 `Local Heat Distortion` 并提高强度
- **THEN** Game View MUST 能通过切换热浪折射、螺旋风压、脉冲冲击和纵向上升气流观察到不同区域扭曲效果

#### Scenario: 区域和粒子手动验证
- **WHEN** 用户移动、缩放、旋转 `LocalHeatDistortionAreaSource` 并启用对应 ParticleSystem
- **THEN** Game View MUST 只在该区域中出现热力扭曲和粒子气流提示
- **AND** 背景 MUST NOT 全屏扭曲

#### Scenario: 遮挡手动验证
- **WHEN** 用户把 `LocalHeatDistortionAreaSource` 放到墙体或其他不透明物体后方
- **THEN** Game View MUST NOT 在前景墙体上显示后方热力扭曲
- **AND** 用户把区域源移动到无遮挡位置后 MUST 能重新看到区域扭曲
