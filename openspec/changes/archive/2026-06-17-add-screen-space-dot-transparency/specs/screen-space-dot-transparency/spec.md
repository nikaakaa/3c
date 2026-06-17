## ADDED Requirements
### Requirement: 屏幕空间点阵透明接入
系统 MUST 在正式角色材质路径中提供屏幕空间点阵透明能力，至少覆盖当前场景使用的 Haste Diffuse 风格角色 shader，并保留项目 Toon shader 支持。该能力 MUST 只影响使用该材质和启用参数的 renderer，MUST NOT 通过全屏后处理、额外相机、独立 Renderer Feature 或第二套角色渲染路径实现。

#### Scenario: 当前 Diffuse 风格角色材质路径接入
- **WHEN** 角色材质使用当前场景的 Haste Diffuse 风格角色 shader 且启用屏幕空间点阵透明
- **THEN** 点状透明 MUST 在该材质的渲染 pass 中完成
- **AND** 系统 MUST NOT 新增并行角色 shader 作为主路径

#### Scenario: Toon 材质路径保持支持
- **WHEN** 角色材质使用项目 Toon shader 且启用屏幕空间点阵透明
- **THEN** 点状透明 MUST 在该材质的渲染 pass 中完成
- **AND** 系统 MUST NOT 新增并行角色 shader 作为主路径

#### Scenario: 不使用后处理替代
- **WHEN** 项目启用屏幕空间点阵透明
- **THEN** 系统 MUST NOT 依赖 `OnRenderImage`、额外相机、全屏 Renderer Feature 或独立 CommandBuffer 管线裁剪角色
- **AND** 其他未启用该材质参数的物体 MUST 不受影响

### Requirement: 屏幕固定点阵语义
系统 MUST 使用当前相机屏幕像素坐标生成点阵 mask，使点阵锚定到屏幕网格，而不是模型 UV、物体空间、世界空间或角色骨骼。

#### Scenario: 角色移动时点阵不跟随表面
- **WHEN** 启用点阵透明的角色在屏幕中移动或播放动画
- **THEN** 点阵网格 MUST 保持在屏幕像素位置
- **AND** 点阵 MUST NOT 随角色 UV、骨骼或世界位置滑动

#### Scenario: 屏幕像素间距控制
- **WHEN** 用户调整点阵间距参数
- **THEN** 点与点之间的距离 MUST 以屏幕像素单位变化
- **AND** 模型缩放或 UV 密度 MUST NOT 改变点阵网格密度

#### Scenario: 屏幕偏移只改变网格锚点
- **WHEN** 用户调整屏幕像素偏移参数
- **THEN** 点阵 MUST 在屏幕平面内整体偏移
- **AND** 系统 MUST NOT 将偏移绑定到角色位置

### Requirement: Cutout 渲染和 pass 一致性
系统 MUST 使用 alpha clip / cutout 方式表达点状透明，并保持 Forward、Outline、相机 DepthOnly 和 DepthNormals 的点阵裁剪一致。系统 MUST NOT 将该能力实现为普通 alpha blend 透明队列。

#### Scenario: Forward 使用点阵 clip
- **WHEN** 点阵透明启用且点阵 mask 判定当前像素为透明
- **THEN** Forward pass MUST clip 当前像素
- **AND** 不透明点位 MUST 保留原材质光照、颜色和材质表现

#### Scenario: Outline 使用同一裁剪
- **WHEN** 启用点阵透明的角色显示 Toon 描边
- **THEN** Outline pass MUST 使用同一屏幕空间点阵 mask 裁剪
- **AND** 描边 MUST NOT 以完整角色轮廓残留在透明洞口外

#### Scenario: 相机深度使用同一裁剪
- **WHEN** URP 使用角色的 DepthOnly 或 DepthNormals pass 生成相机深度相关纹理
- **THEN** DepthOnly 和 DepthNormals pass MUST 使用同一屏幕空间点阵 mask
- **AND** 透明洞口 MUST NOT 写入与可见画面不一致的实心角色深度

#### Scenario: 阴影第一版保持实心
- **WHEN** 点阵透明启用且角色参与 ShadowCaster pass
- **THEN** ShadowCaster pass MUST 保持现有实心阴影语义
- **AND** 系统 MUST NOT 在第一版中生成屏幕空间点阵阴影

#### Scenario: 不切普通透明队列
- **WHEN** 点阵透明启用
- **THEN** 材质 MUST 保持不透明或 cutout 语义
- **AND** 系统 MUST NOT 要求角色材质切换为普通 Transparent alpha blend 队列

### Requirement: 点阵透明参数抽象
系统 MUST 使用正式配置和归一化运行时参数表达点阵透明状态。shader MUST 只消费已经归一化的点阵参数，运行时 MUST NOT 依赖隐藏常量、临时调试字段或 fallback 配置完成主要效果。

#### Scenario: 默认关闭
- **WHEN** 角色材质或 Profile 使用默认点阵透明参数
- **THEN** 点阵透明 MUST 不改变现有角色画面
- **AND** 默认材质导入后 MUST 保持兼容

#### Scenario: 参数安全范围
- **WHEN** 输入超出范围的覆盖强度、点间距、点半径、硬度或屏幕偏移
- **THEN** 运行时归一化参数 MUST 钳制到安全范围
- **AND** shader MUST 避免 NaN、负间距或不可控高频点阵

#### Scenario: 运行时写入不修改共享材质
- **WHEN** 运行时对某个角色启用点阵透明
- **THEN** 系统 MUST 使用 renderer 实例级参数写入
- **AND** 系统 MUST NOT 修改 shared material 导致其他角色被隐式影响

#### Scenario: 缺失正式配置不静默替代
- **WHEN** 点阵透明运行时入口缺少必需的正式配置
- **THEN** 系统 MUST 明确保持禁用或报告配置问题
- **AND** 系统 MUST NOT 静默创建临时 fallback 配置继续播放

### Requirement: 点阵透明可测试性
系统 MUST 提供自动测试覆盖点阵透明配置、shader 路径、pass 一致性、默认关闭、运行时写入边界和预览资产配置。

#### Scenario: 自动测试覆盖核心行为
- **WHEN** 运行 `ThirdPersonRendering.Tests.ScreenSpaceDotTransparencyTests`
- **THEN** 测试 MUST 覆盖默认关闭、参数钳制、配置缺失边界、Haste Diffuse 风格角色 shader 属性、Toon shader 属性、Forward/Outline/DepthOnly/DepthNormals 裁剪路径、ShadowCaster 排除、非 Transparent 队列和运行时实例级参数写入

#### Scenario: 自动测试覆盖预览配置
- **WHEN** 运行点阵透明相关 EditMode 测试
- **THEN** 测试 MUST 验证默认 Profile、预览材质和 Sandbox 预览入口使用正式资产引用
- **AND** 预览入口 MUST 默认关闭
