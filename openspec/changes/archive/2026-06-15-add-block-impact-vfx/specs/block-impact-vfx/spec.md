## ADDED Requirements
### Requirement: 开箱即用格挡冲击 Prefab
系统 MUST 提供一个开箱即用的 `BlockImpactVfx` Prefab，包含默认配置、材质、粒子、贴片层、生命周期控制组件和公开播放入口。Prefab MUST 能被拖入场景后手动触发，也 MUST 能被外部代码实例化后调用播放入口，不依赖动画事件、状态机或战斗判定。

#### Scenario: 拖入场景后可手动播放
- **WHEN** 用户将 `BlockImpactVfx` Prefab 放入场景并通过 Inspector 触发预览
- **THEN** Prefab MUST 在自身位置播放一次格挡冲击特效
- **AND** 播放不需要 AnimationClip Event、Timeline Signal、输入系统或真实格挡事件

#### Scenario: 代码实例化后可播放
- **WHEN** 外部代码实例化 `BlockImpactVfx` Prefab 并提交播放请求
- **THEN** Prefab MUST 根据请求中的命中点、方向、强度和持续时间播放一次特效
- **AND** Prefab MUST NOT 修改 FullBody owner、Locomotion phase、Action tracker、伤害结果或输入缓冲

### Requirement: 纯表现层播放请求
系统 MUST 使用纯表现层请求描述格挡冲击播放输入，包括世界命中点、攻击方向、屏幕中心、强度、持续时间、随机种子和表现层开关。请求 MUST NOT 携带 `GameObject`、`Transform`、`Collider`、Animancer 对象、状态机对象或场景实例引用作为必需输入。

#### Scenario: 请求驱动表现
- **WHEN** Prefab 收到有效播放请求
- **THEN** 系统 MUST 只消费该请求和配置资产驱动爆闪、火花、弧线、光带和屏幕冲击
- **AND** 系统 MUST NOT 反向查询动画事件或战斗状态

#### Scenario: 参数安全
- **WHEN** 请求输入非法强度、持续时间或零方向
- **THEN** 系统 MUST 将参数规范化到安全范围
- **AND** 系统 MUST 避免产生 NaN、无限生命周期或不可控粒子数量

### Requirement: 格挡冲击素材配置
系统 MUST 提供格挡冲击素材配置资产，正式引用爆闪、火花、弧线、圆环、横向光带、噪声或扭曲贴图。配置资产 MUST 使用项目内正式贴图引用，MUST NOT 在运行时代码中硬编码 `Assets/Art/Tex/绝区零贴图` 路径，也 MUST NOT 在缺失配置时静默改用 fallback 贴图。

#### Scenario: 配置引用已导入贴图
- **WHEN** 检查格挡冲击素材配置资产
- **THEN** 配置 MUST 引用 `Assets/Art/Tex/绝区零贴图` 中用于格挡冲击的正式贴图
- **AND** 爆闪、火花、弧线和光带所需贴图字段 MUST 不为空

#### Scenario: 缺失必需贴图暴露错误
- **WHEN** 格挡冲击配置缺少必需贴图
- **THEN** 配置校验 MUST 报告错误或使预览明确不可用
- **AND** 系统 MUST NOT 静默生成临时默认贴图继续运行

### Requirement: Prefab 分层表现
系统 MUST 在 `BlockImpactVfx` Prefab 中提供命中爆闪、方向性火花、能量弧线或冲击环、横向光带四类基础表现层。每一层 MUST 能由配置或播放请求启停，并 MUST 按生命周期淡出或停止。

#### Scenario: 命中爆闪
- **WHEN** 播放请求启用爆闪层
- **THEN** 命中点附近 MUST 生成短生命周期 Additive 爆闪
- **AND** 爆闪强度 MUST 能超过普通颜色范围以配合 Bloom

#### Scenario: 方向性火花
- **WHEN** 播放请求提供有效攻击方向并启用火花层
- **THEN** 火花粒子 MUST 主要沿攻击方向的反向或配置切线方向扩散
- **AND** 粒子数量、速度、寿命、颜色和拖尾 MUST 由配置控制

#### Scenario: 弧线和冲击环
- **WHEN** 播放请求启用弧线或冲击环层
- **THEN** 系统 MUST 在命中点附近生成短生命周期弧线、裂纹线或环形片
- **AND** 生命周期结束后 MUST 淡出，不残留可见对象

#### Scenario: 横向光带
- **WHEN** 播放请求启用光带层
- **THEN** 系统 MUST 能生成横向或屏幕对齐的 Additive 光带
- **AND** 光带 MUST 能通过强度和持续时间参数快速衰减

### Requirement: 格挡冲击 shader
系统 MUST 提供格挡冲击所需的 Additive 爆闪/光带 shader、火花/拖尾 shader 和能量弧线 shader。这些 shader MUST 支持贴图 alpha、HDR 强度、颜色、淡出和必要的 UV 控制，并 MUST 默认不写深度。

#### Scenario: 爆闪和光带 shader
- **WHEN** 爆闪或光带材质使用格挡冲击 Additive shader
- **THEN** shader MUST 支持主贴图、颜色、强度、透明度和 UV 缩放偏移
- **AND** shader MUST 使用透明 Additive 混合并关闭 ZWrite

#### Scenario: 火花 shader
- **WHEN** 火花 ParticleSystem 使用格挡冲击火花 shader
- **THEN** shader MUST 支持粒子顶点颜色、贴图 alpha、强度和拖尾渐隐

#### Scenario: 弧线 shader
- **WHEN** 弧线或冲击环使用格挡冲击弧线 shader
- **THEN** shader MUST 支持噪声滚动、dissolve、边缘强度和生命周期淡出

### Requirement: 生命周期和实例控制
系统 MUST 控制格挡冲击 Prefab 的播放生命周期，支持一次性播放、停止、重播和回收。系统 MUST 限制同一调度器下的活跃实例数量，避免连续触发时无限堆积场景对象。

#### Scenario: 一次性播放结束
- **WHEN** 一次格挡冲击播放达到配置持续时间
- **THEN** 爆闪、火花、弧线和光带 MUST 停止或淡出
- **AND** Prefab MUST 进入可回收或待机状态

#### Scenario: 连续播放受控
- **WHEN** 短时间内连续提交多个播放请求
- **THEN** 调度器 MUST 使用受控的实例数量或对象池策略
- **AND** 系统 MUST 避免无限生成场景对象

### Requirement: Sandbox 手动预览
系统 MUST 在 `Assets/Scenes/Sandbox.unity` 中提供默认关闭的格挡冲击预览入口，使用户可以在不接动画事件或真实战斗系统的情况下验证 Prefab 开箱即用效果。

#### Scenario: 默认关闭
- **WHEN** 打开 Sandbox 场景
- **THEN** 格挡冲击预览对象 MUST 默认不自动刷屏触发
- **AND** 默认场景画面 MUST 与未启用预览时一致

#### Scenario: 手动触发预览
- **WHEN** 用户启用预览对象并触发一次预览
- **THEN** Game View MUST 在配置的命中点显示格挡冲击 VFX
- **AND** 用户 MUST 能调整强度、方向和持续时间观察变化

### Requirement: 格挡冲击 Prefab 可验证性
系统 MUST 提供自动测试和手动验证步骤，确认播放请求、配置校验、shader 属性、Prefab 结构、生命周期和开箱即用效果可验证。

#### Scenario: 自动测试
- **WHEN** 运行 `ThirdPersonRendering.Tests.BlockImpactVfxTests`
- **THEN** 测试 MUST 覆盖请求默认值、参数钳制、素材配置必填字段、shader 关键属性、Prefab 层级、默认关闭的 Sandbox 预览入口、生命周期和运行时不依赖动画事件或战斗状态机对象

#### Scenario: 手动验证
- **WHEN** 用户在 Sandbox 中触发格挡冲击预览
- **THEN** 用户 MUST 能看到中心强爆闪、方向性火花、短弧线或冲击环以及快速消失的光带
- **AND** 用户将强度调为 0 后 MUST 能确认 Prefab 不再产生可见冲击效果
