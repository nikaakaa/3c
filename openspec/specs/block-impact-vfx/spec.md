# block-impact-vfx Specification

## Purpose
定义格挡冲击 VFX 的轻量三层表现、后处理主路径、素材配置、Prefab 分层、生命周期控制和纯表现层播放请求边界。
## Requirements
### Requirement: 格挡冲击必须使用轻量三层表现
系统 MUST 将 `BlockImpactVfx` 表现收缩为中心 HDR 核心、屏幕空间横向强光和方向火花三类基础层。每一层 MUST 能通过正式 Profile 或播放请求启停，且 MUST 继续使用现有 `BlockImpactVfx` Prefab 和播放入口。

#### Scenario: 同一入口重构而非分裂路径
- **WHEN** 项目提供格挡冲击特效
- **THEN** 系统 MUST 使用现有 `BlockImpactVfx` Prefab、Profile、Controller 和 PostProcess 入口
- **AND** 系统 MUST NOT 新增并行的 `BlockImpactVfx2`、第二套主 Prefab 或第二套播放 API

#### Scenario: 三层播放
- **WHEN** 用户触发一次格挡冲击预览
- **THEN** 系统 MUST 能同时播放中心 HDR 核心、屏幕横向强光和方向火花
- **AND** 用户 MUST 能单独关闭任一层来观察剩余层级

### Requirement: 横向强光必须由屏幕空间后处理承担主路径
系统 MUST 使用 URP Renderer Feature/Render Pass 的屏幕空间 shader 生成格挡冲击的主横向强光。该横光 MUST 根据屏幕中心、强度、长度、厚度、软边和时间衰减计算，并 MUST 在强度为 0 或无脉冲时不改变画面。

#### Scenario: 横光不是世界矩形板子
- **WHEN** 只开启横向强光层并触发预览
- **THEN** Game View MUST 出现以冲击点屏幕位置为中心的水平延展亮光
- **AND** 场景中 MUST NOT 依赖一个可见边界的世界空间矩形贴片作为主横条效果

#### Scenario: 横光参与 Bloom
- **WHEN** Main Camera 开启 Post Processing 且 Bloom 可用
- **THEN** 横向强光 MUST 以 HDR additive 输出进入当前 URP 后处理链路
- **AND** 用户 MUST 能观察到中心附近被 Bloom 放大的亮度

### Requirement: 贴图只能作为 mask/noise/shape 输入
系统 MUST 将导入贴图用作中心 mask、火花 shape、trail alpha、横光 mask 或 noise 输入。系统 MUST NOT 将整张参考效果或可见方形贴图直接作为完整格挡冲击画面。

#### Scenario: 方形边界不可见
- **WHEN** 播放中心核、横光或火花层
- **THEN** 对应材质 MUST 使用贴图 alpha、软边或程序化 mask 隐藏贴图方形边界
- **AND** 用户在 Game View 中 MUST NOT 看到明显矩形底板

#### Scenario: 贴图颜色不驱动发光色
- **WHEN** 中心核或火花贴图本身带有绿色、青色或其他参考图颜色
- **THEN** 世界空间 additive shader MUST 将贴图 RGB 视为 shape mask 或亮度 mask
- **AND** 发光色相 MUST 由正式 Profile 的颜色参数控制

#### Scenario: 缺失贴图不静默 fallback
- **WHEN** Profile 缺少必需的 mask、noise、spark 或 trail 贴图
- **THEN** 配置校验 MUST 报告错误或让预览明确不可用
- **AND** 系统 MUST NOT 静默生成临时默认贴图继续播放

### Requirement: 格挡冲击 Profile 必须暴露核心调参
系统 MUST 通过正式 `BlockImpactVfxProfile` 暴露中心核、屏幕横光和火花的常用调参。调参 MUST 写入正式资产并被运行时读取，系统 MUST NOT 依赖隐藏常量或临时 debug 字段完成主要观感控制。

#### Scenario: 中心核和屏幕层可调
- **WHEN** 用户选中默认 `BlockImpactVfxProfile`
- **THEN** 用户 MUST 能调整中心颜色、HDR 强度、软边、缩放、持续时间、屏幕横光长度、厚度、软边和整体屏幕冲击强度
- **AND** 用户 MUST 能分别调整闪白、径向拖影、横向光带和色散权重

#### Scenario: 火花层可调
- **WHEN** 用户选中默认 `BlockImpactVfxProfile`
- **THEN** 用户 MUST 能调整火花颜色、数量、速度、寿命、喷射角、速度拉伸、长度拉伸、trail 寿命、trail 宽度、重力和速度衰减
- **AND** 运行时播放 MUST 读取这些 Profile 参数

### Requirement: 火花必须响应攻击方向
系统 MUST 使用命中点和攻击方向构建火花喷射方向。系统 MUST NOT 让火花均匀球形散开或固定朝一个世界方向播放。

#### Scenario: 攻击方向影响火花
- **WHEN** 用户修改播放请求中的攻击方向并触发预览
- **THEN** 火花喷射主方向 MUST 随攻击方向改变
- **AND** 火花 MUST 从命中点附近 burst

### Requirement: 火花必须使用方向性速度拉伸、trail 和轻量物理感
系统 MUST 使用 ParticleSystem 或等效粒子实现方向性火花。火花渲染 MUST 支持速度拉伸或等效线状表现，MUST 支持短 trail，且 MUST 使用粒子速度、重力或速度衰减制造轻量物理感。系统 MUST NOT 为每个火花创建 Rigidbody。

#### Scenario: 火花有速度线
- **WHEN** 启用火花层并触发预览
- **THEN** 火花 MUST 以短生命周期高速喷射
- **AND** 火花 MUST 呈现速度拉伸或短 trail，而不是静态点状贴图云
- **AND** 火花 MUST 有可配置的下坠或速度衰减

#### Scenario: 火花方向可验证
- **WHEN** 用户连续用两个不同攻击方向触发预览
- **THEN** 两次火花的主喷射方向 MUST 可见不同
- **AND** 粒子数量、速度、寿命、trail 和轻量物理参数 MUST 由正式 Profile 控制

### Requirement: 立体格挡特效必须可测试和可手动验证
系统 MUST 提供自动测试和手动验证步骤，覆盖横条主路径、方形边界、方向火花、轻量物理参数、Profile 配置、Prefab 结构和无动画事件依赖。

#### Scenario: 自动测试
- **WHEN** 运行格挡冲击相关 EditMode 测试
- **THEN** 测试 MUST 验证 `BlockImpactVfx` 仍是唯一正式入口
- **AND** 测试 MUST 验证火花使用速度拉伸或 trail 配置
- **AND** 测试 MUST 验证火花使用轻量物理参数而不是 Rigidbody
- **AND** 测试 MUST 验证横向强光参数存在于屏幕空间后处理路径
- **AND** 测试 MUST 验证 Controller 不依赖 AnimationClip Event、Timeline、输入系统或状态机对象

#### Scenario: 用户手动验证
- **WHEN** 用户在 Sandbox 中按中心核、screen streak、火花的顺序逐层开启并触发预览
- **THEN** 用户 MUST 能分别确认中心 Bloom、屏幕水平亮条和方向火花
- **AND** 用户 MUST 能通过关闭对应层确认每一层来自独立正式配置

### Requirement: 开箱即用格挡冲击 Prefab
系统 MUST 提供一个开箱即用的 `BlockImpactVfx` Prefab，包含默认配置、材质、粒子、贴片层、生命周期控制组件和公开播放入口。Prefab MUST 能被拖入场景后手动触发，也 MUST 能被外部代码实例化后调用播放入口，不依赖动画事件、状态机或战斗判定。

#### Scenario: 拖入场景后可手动播放
- **WHEN** 用户将 `BlockImpactVfx` Prefab 放入场景并通过 Inspector 触发预览
- **THEN** Prefab MUST 在自身位置播放一次格挡冲击特效
- **AND** 播放不需要 AnimationClip Event、Timeline Signal、输入系统或真实格挡事件

#### Scenario: 代码实例化后可播放
- **WHEN** 外部代码实例化 `BlockImpactVfx` Prefab 并提交播放请求
- **THEN** Prefab MUST 根据请求中的命中点、方向、强度和持续时间播放一次特效
- **AND** Prefab MUST NOT 修改 body claim、slot owner、Locomotion phase、Action tracker、伤害结果或输入缓冲

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
