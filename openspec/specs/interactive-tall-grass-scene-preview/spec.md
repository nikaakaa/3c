# interactive-tall-grass-scene-preview Specification

## Purpose
定义高草丛预览场景的 shader、配置抽象、角色交互、Sandbox 展示、性能预期和验证边界，确保该表现只作为场景预览能力接入当前项目。
## Requirements
### Requirement: 高草丛预览接入
系统 MUST 提供一个可在 Sandbox 或独立预览场景中摆放的高草丛预览能力，并且 MUST NOT 新增角色控制器、独立相机路径或未审批的玩法逻辑入口。

#### Scenario: 预览对象可摆放
- **WHEN** 用户在场景中放置高草丛预览 prefab
- **THEN** 场景 MUST 出现一块由配置驱动的高草丛区域
- **AND** 高草丛 MUST 能在不接入动作状态机的情况下独立预览

#### Scenario: 默认不污染场景
- **WHEN** 打开默认 Sandbox 场景
- **THEN** 高草丛预览对象 MUST 默认关闭或位于明确的预览区域
- **AND** 默认画面 MUST NOT 被大面积草丛遮挡

### Requirement: 高草丛配置抽象
系统 MUST 使用配置对象表达高草丛的范围、密度、随机种子、高度、宽度、颜色、风摆和交互参数，使生成器和 shader 只消费归一化后的配置。

#### Scenario: 配置驱动生成
- **WHEN** 用户修改草丛范围、密度或随机种子
- **THEN** 生成器 MUST 根据配置生成可复现的草片分布
- **AND** 同一配置和随机种子 MUST 生成稳定结果

#### Scenario: 参数钳制
- **WHEN** 输入超出范围的密度、高度、宽度、风摆或交互参数
- **THEN** 配置 MUST 将参数钳制到安全范围内

### Requirement: 风格化高草 shader
系统 MUST 提供一个 URP 场景草 shader，用于渲染高草丛的色块、风摆、alpha clip 和交互压弯效果。

#### Scenario: 二次元风格预览
- **WHEN** 草材质使用二次元倾向的颜色和边缘参数
- **THEN** 草丛 MUST 呈现清晰色块、受控高光和可读轮廓

#### Scenario: 较自然风格预览
- **WHEN** 草材质使用较自然的颜色和柔和边缘参数
- **THEN** 草丛 MUST 保持风格化但不强制二次元描边

#### Scenario: Alpha clip 渲染
- **WHEN** 草片使用透明边缘纹理或程序形状
- **THEN** shader SHOULD 优先使用 alpha clip
- **AND** 草丛 SHOULD 避免依赖大面积半透明排序

### Requirement: 高草丛交互
系统 MUST 支持一个交互源基于 Transform 位置推开或压弯附近草片，使角色穿过草丛时有可见反馈。

#### Scenario: 角色进入草丛
- **WHEN** 交互源进入高草丛交互半径
- **THEN** 附近草片 MUST 朝远离交互源的方向弯曲或让开
- **AND** 弯曲强度 MUST 随距离衰减

#### Scenario: 角色离开草丛
- **WHEN** 交互源离开草丛区域
- **THEN** 草片 MUST 回到仅受风摆影响的状态

#### Scenario: 无交互源
- **WHEN** 高草丛没有绑定交互源
- **THEN** 草丛 MUST 仍能渲染和风摆
- **AND** 系统 MUST NOT 报错或停止生成

### Requirement: 高草丛可验证性
系统 MUST 提供自动测试和手动验证步骤，确认高草丛配置、生成、shader 引用、交互源和预览 prefab 可验证。

#### Scenario: 自动测试
- **WHEN** 运行高草丛 EditMode 测试
- **THEN** 测试 MUST 覆盖配置钳制、随机生成确定性、交互参数计算、无交互源安全行为和预览 prefab 结构

#### Scenario: 手动验证
- **WHEN** 用户打开预览场景或 Sandbox 中的高草丛预览对象
- **THEN** 用户 MUST 能观察草丛高度、密度、风摆和颜色风格
- **AND** 用户 MUST 能用角色或指定 Transform 穿过草丛观察交互压弯
