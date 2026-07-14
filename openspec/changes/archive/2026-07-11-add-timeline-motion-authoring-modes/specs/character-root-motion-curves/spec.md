# character-root-motion-curves Specification Delta

## MODIFIED Requirements

### Requirement: Root Motion 曲线资产表达动画派生位移
系统 MUST 使用独立 `RootMotionCurveAsset` 表达从 `AnimationClip` 派生出的 root motion 曲线。该资产 MUST 保存源动画、时长、采样率和明确的求值模式。完整本地位移模式 MUST 保存累计本地位置 XYZ 曲线和累计 yaw 曲线；前向距离模式 MUST 保存累计前向距离和累计 yaw，并在运行时按角色 forward 解释位移。该资产 MUST NOT 保存 footphase、动作窗口、body claim、locomotion state 或旧 BBB motion 配置。

#### Scenario: 烘焙生成完整本地曲线资产
- **WHEN** 用户以完整本地位移模式对一个 `AnimationClip` 执行 root motion 曲线烘焙
- **THEN** 系统 MUST 生成或覆盖一个 `RootMotionCurveAsset`
- **AND** 资产 MUST 记录源动画、采样参数和 `FullLocalDelta` 求值模式
- **AND** 资产 MUST 包含累计本地位置 XYZ 和累计 yaw 曲线
- **AND** 资产 MUST NOT 生成旧 `MotionClipData`、`WarpedMotionData` 或其它旧配置节点

#### Scenario: 烘焙生成前向距离曲线资产
- **WHEN** 用户以前向距离模式对一个 `AnimationClip` 执行 root motion 曲线烘焙
- **THEN** 系统 MUST 生成或覆盖一个 `RootMotionCurveAsset`
- **AND** 资产 MUST 记录源动画、采样参数和 `ForwardDistanceYaw` 求值模式
- **AND** 资产 MUST 包含累计前向距离和累计 yaw 曲线
- **AND** 资产 MUST NOT 使用动画横向漂移作为最终角色侧向位移

#### Scenario: 动画没有 root motion
- **WHEN** 动画没有有效 root motion 位移或旋转
- **THEN** 烘焙结果 MAY 包含零值曲线
- **AND** 系统 MUST NOT 自动查找其它配置作为 fallback

### Requirement: 编辑器烘焙器只生成正式曲线资产
系统 MUST 提供正式编辑器工具，从指定 `AnimationClip` 和采样对象烘焙 root motion 曲线。工具 MUST 允许作者显式选择完整本地位移或前向距离模式。工具 MUST 参考 Animator root motion 采样结果，但 MUST NOT 递归扫描 `PlayerSO`、旧 SO/config 或 `Ref` 中 BBB 数据并写回。

#### Scenario: 使用采样对象烘焙完整本地位移
- **WHEN** 用户指定 `AnimationClip`、采样对象、输出位置和完整本地位移模式
- **THEN** 工具 MUST 临时实例化采样对象
- **AND** 工具 MUST 使用对象上的 `Animator` 采样目标 clip
- **AND** 工具 MUST 把采样得到的累计 local XYZ 和 yaw 写入 `RootMotionCurveAsset`
- **AND** 工具 MUST 销毁临时实例

#### Scenario: 使用采样对象烘焙前向距离
- **WHEN** 用户指定 `AnimationClip`、采样对象、输出位置和前向距离模式
- **THEN** 工具 MUST 临时实例化采样对象
- **AND** 工具 MUST 使用对象上的 `Animator` 采样目标 clip
- **AND** 工具 MUST 把采样得到的平面位移距离累计为 forward distance
- **AND** 工具 MUST 把采样得到的 yaw 写入 `RootMotionCurveAsset`
- **AND** 工具 MUST 销毁临时实例

#### Scenario: 缺少采样条件
- **WHEN** 采样对象缺少 `Animator` 或可用 controller
- **THEN** 工具 MUST 中止烘焙并报告错误
- **AND** 工具 MUST NOT 使用场景对象搜索、默认对象或自动生成兼容配置作为 fallback

### Requirement: Root Motion 求值器从累计曲线计算本帧 delta
系统 MUST 提供运行时求值器，从 `RootMotionCurveAsset` 按播放时间和求值模式计算 root motion delta。求值器 MUST 对累计曲线在 `previousTime` 和 `currentTime` 的差值计算本帧本地位移和 yaw 变化。完整本地位移模式 MUST 使用累计本地 XYZ 差值；前向距离模式 MUST 使用累计 forward distance 差值生成本地 forward 位移。

#### Scenario: 完整本地位移正常前进播放
- **WHEN** `FullLocalDelta` 曲线播放时间从 `previousTime` 前进到 `currentTime`
- **THEN** 求值器 MUST 采样两个时间点的累计本地位置
- **AND** 求值器 MUST 输出本地 delta position
- **AND** 求值器 MUST 采样两个时间点的累计 yaw
- **AND** 求值器 MUST 输出 delta yaw

#### Scenario: 前向距离正常前进播放
- **WHEN** `ForwardDistanceYaw` 曲线播放时间从 `previousTime` 前进到 `currentTime`
- **THEN** 求值器 MUST 采样两个时间点的累计 forward distance
- **AND** 求值器 MUST 输出 `Vector3.forward * deltaDistance` 作为本地 delta position
- **AND** 求值器 MUST 采样两个时间点的累计 yaw
- **AND** 求值器 MUST 输出 delta yaw

#### Scenario: 单次播放越界
- **WHEN** 播放时间小于 0 或大于资产时长
- **THEN** 求值器 MUST 按单次播放规则 clamp 到合法范围
- **AND** 求值器 MUST NOT 自动切换 loop、倒放或其它隐式模式

### Requirement: Timeline 不得通过动画片段直接提交 Root Motion
系统 MUST NOT 使用 `Timeline.AnimationClip.RootMotionCurve` 或等价字段作为运行时 motion contribution 入口。Timeline 中的 `AnimationTrack` MUST 只提交动画表现贡献。动画派生的位移曲线若要影响角色运动，MUST 被显式配置为 `MotionCurveTrack` 或等价正式 motion fact 轨道，并继续通过 `MotionResolver` 和 `CharacterMotionStage` 应用。

#### Scenario: 动画片段播放
- **WHEN** Timeline 的 `AnimationTrack` 采样到动画片段
- **THEN** 系统 MUST 只提交动画表现贡献
- **AND** 系统 MUST NOT 从该动画片段读取 root motion 曲线并提交 motion contribution

#### Scenario: 烘焙曲线用于动作位移
- **WHEN** 作者需要使用从动画派生的位移曲线驱动动作
- **THEN** 作者 MUST 通过显式 `MotionCurveTrack` 或正式 motion fact 轨道配置该位移
- **AND** 系统 MUST NOT 通过动画名称、目录、同名 asset、AnimationClip 字段或旧 SO/config 自动查找并应用曲线

## ADDED Requirements

### Requirement: 旧曲线资产必须保持完整本地位移语义
系统 MUST 让既有 `RootMotionCurveAsset` 在未显式重烘焙或迁移时保持完整本地位移语义。系统 MUST NOT 静默把旧累计 XYZ 曲线解释为前向距离模式。

#### Scenario: 读取旧曲线资产
- **WHEN** 旧曲线资产没有显式求值模式字段或字段为默认值
- **THEN** 运行时 MUST 按 `FullLocalDelta` 解释
- **AND** 系统 MUST NOT 自动改写资产内容或切换为 `ForwardDistanceYaw`

#### Scenario: 作者选择前向距离模式
- **WHEN** 作者需要普通攻击前踏或 locomotion 稳定位移
- **THEN** 作者 MUST 通过正式 baker 或明确资产配置生成 `ForwardDistanceYaw` 曲线
- **AND** Timeline MUST 通过显式 `MotionCurveTrack` 或等价正式 motion fact 轨道表达运行时位移
