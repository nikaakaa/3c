## ADDED Requirements
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
