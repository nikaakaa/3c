## ADDED Requirements

### Requirement: RootMotionCurveAsset 与 Timeline 内联位移必须保持单向边界

`RootMotionCurveAsset` 是离线烘焙、检查和重烘焙动画派生曲线的 authoring 数据；`MotionCurveClip` 是 Timeline Runtime 实际提交的 motion fact。`MotionCurveTrack` MUST 只采样自己的内联曲线，MUST NOT 自动读取、按名称查找或隐式引用 `RootMotionCurveAsset`。本能力不创建资产到 Timeline 的自动同步、双写或运行时导入路径。

#### Scenario: Timeline 采样内联 MotionCurveClip

- **WHEN** `TimelinePlaybackScheduler` 推进一个包含 `MotionCurveTrack` 的 Timeline
- **THEN** 轨道 MUST 从该 Timeline 内联 `MotionCurveClip` 采样位移与 yaw
- **AND** 轨道 MUST NOT 读取未被显式转换的 `RootMotionCurveAsset`
- **AND** 最终位移 MUST 继续通过正式 motion 管线提交

#### Scenario: 独立烘焙资产没有 Timeline 引用

- **WHEN** 项目中存在没有被任何 Timeline 显式转写的 `RootMotionCurveAsset`
- **THEN** 该资产 MUST NOT 自动影响角色 Runtime 位移
- **AND** 系统 MUST NOT 根据动画名称、目录或曲线内容建立隐式关联

## MODIFIED Requirements

### Requirement: Root Motion 曲线资产表达动画派生位移

系统 MUST 使用独立 `RootMotionCurveAsset` 表达从 `AnimationClip` 派生出的 root motion 曲线。该资产 MUST 保存源动画、时长、采样率和显式有效的求值模式。`Unspecified`、缺失字段、默认零值和未知枚举值均为配置错误，MUST NOT 被解释为其它模式。完整本地位移模式 MUST 保存累计本地位置 XYZ 曲线和累计 yaw 曲线；前向距离模式 MUST 保存累计前向距离和累计 yaw，并在求值时按角色 forward 解释位移。该资产 MUST NOT 保存 footphase、动作窗口、body claim、locomotion state 或旧 BBB motion 配置。

#### Scenario: 读取有效完整本地曲线资产

- **WHEN** 系统读取显式 `FullLocalDelta` 的曲线资产
- **THEN** 系统 MUST 使用累计本地 XYZ 与累计 yaw 表达该资产的运动语义
- **AND** 系统 MUST NOT 将它改解释为前向距离模式

#### Scenario: 读取无效模式资产

- **WHEN** 系统读取模式未指定、字段缺失或模式未知的曲线资产
- **THEN** 系统 MUST 报告该资产的明确配置错误
- **AND** 系统 MUST NOT 推断为 `FullLocalDelta` 或 `ForwardDistanceYaw`
- **AND** 该资产 MUST NOT 产生 sample、delta 或 motion contribution

### Requirement: 编辑器烘焙器只生成正式曲线资产

系统 MUST 提供正式编辑器工具，从指定 `AnimationClip` 和采样对象烘焙 root motion 曲线。工具 MUST 要求作者显式选择完整本地位移或前向距离模式；未选择或非法模式时 MUST 拒绝烘焙。工具 MUST 参考 Animator root motion 采样结果，但 MUST NOT 递归扫描 `PlayerSO`、旧 SO/config 或 `Ref` 中 BBB 数据并写回。

#### Scenario: 作者选择有效模式后烘焙

- **WHEN** 用户指定 `AnimationClip`、采样对象、输出位置和有效求值模式
- **THEN** 工具 MUST 生成或覆盖一个 `RootMotionCurveAsset`
- **AND** 资产 MUST 保存所选的有效显式模式
- **AND** Baker MUST NOT 因 UI 初始值或默认分支写入隐式模式

#### Scenario: 作者未选择有效模式

- **WHEN** 作者保持未指定模式或提供未知模式
- **THEN** 工具 MUST 中止烘焙并报告明确配置错误
- **AND** 工具 MUST NOT 创建、覆盖或自动修复目标资产

### Requirement: Root Motion 求值器从累计曲线计算本帧 delta

系统 MUST 提供运行时求值器，从有效的 `RootMotionCurveAsset` 按播放时间和显式求值模式计算 root motion delta。求值器 MUST 对累计曲线在 `previousTime` 和 `currentTime` 的差值计算本帧本地位移和 yaw 变化。完整本地位移模式 MUST 使用累计本地 XYZ 差值；前向距离模式 MUST 使用累计 forward distance 差值生成本地 forward 位移。无效模式 MUST 使 sample 与 delta 求值失败，MUST NOT 使用“非前向距离模式即完整本地位移”的分支。

#### Scenario: 完整本地位移正常前进播放

- **WHEN** 显式 `FullLocalDelta` 曲线播放时间从 `previousTime` 前进到 `currentTime`
- **THEN** 求值器 MUST 采样两个时间点的累计本地位置与累计 yaw
- **AND** 求值器 MUST 输出它们的本帧差值

#### Scenario: 前向距离正常前进播放

- **WHEN** 显式 `ForwardDistanceYaw` 曲线播放时间从 `previousTime` 前进到 `currentTime`
- **THEN** 求值器 MUST 采样两个时间点的累计 forward distance 与累计 yaw
- **AND** 求值器 MUST 输出 `Vector3.forward * deltaDistance` 和 delta yaw

#### Scenario: 无效模式进入求值器

- **WHEN** 求值器收到模式未指定、缺失或未知的曲线资产
- **THEN** 求值 MUST 失败并保留零输出
- **AND** 系统 MUST NOT 推断曲线模式、自动写回资产或改用其它 motion 来源

## REMOVED Requirements

### Requirement: 旧曲线资产必须保持完整本地位移语义

系统 MUST 让既有 `RootMotionCurveAsset` 在未显式重烘焙或迁移时保持完整本地位移语义。系统 MUST NOT 静默把旧累计 XYZ 曲线解释为前向距离模式。

#### Scenario: 读取旧曲线资产

- **WHEN** 旧曲线资产没有显式求值模式字段或字段为默认值
- **THEN** 运行时 MUST 按 `FullLocalDelta` 解释
- **AND** 系统 MUST NOT 自动改写资产内容或切换为 `ForwardDistanceYaw`
