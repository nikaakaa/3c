# character-root-motion-curves Specification

## Purpose
定义 root motion 曲线资产和烘焙链路：从指定 `AnimationClip` 和采样 Prefab 生成 `RootMotionCurveAsset`，保存累计位移与 yaw 曲线，作为离线 authoring 数据供作者生成、检查和重烘焙。Timeline 运行时位移 MUST 通过内联 `MotionCurveClip`、独立 `MotionCurveTrack` 或等价正式 motion fact 轨道进入 MotionStage，不从 `RootMotionCurveAsset` 或 AnimationClip 字段自动采样，不恢复旧 BBB motion 配置或 footphase/body claim 数据源。
## Requirements

### Requirement: Root Motion 曲线资产表达动画派生位移
系统 MUST 使用独立 `RootMotionCurveAsset` 表达从 `AnimationClip` 派生出的 root motion 曲线。该资产 MUST 保存源动画、时长、采样率和显式有效的求值模式。`Unspecified`、缺失字段、默认零值和未知枚举值均为配置错误，MUST NOT 被解释为其它模式。完整本地位移模式 MUST 保存累计本地位置 XYZ 曲线和累计 yaw 曲线；前向距离模式 MUST 保存累计前向距离和累计 yaw，并在运行时按角色 forward 解释位移。该资产 MUST NOT 保存 footphase、动作窗口、body claim、locomotion state 或旧 BBB motion 配置。

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

#### Scenario: 读取无效模式资产
- **WHEN** 系统读取模式未指定、字段缺失或模式未知的曲线资产
- **THEN** 系统 MUST 报告该资产的明确配置错误
- **AND** 系统 MUST NOT 推断为 `FullLocalDelta` 或 `ForwardDistanceYaw`
- **AND** 该资产 MUST NOT 产生 sample、delta 或 motion contribution

### Requirement: 编辑器烘焙器只生成正式曲线资产
系统 MUST 提供正式编辑器工具，从指定 `AnimationClip` 和采样对象烘焙 root motion 曲线。工具 MUST 要求作者显式选择完整本地位移或前向距离模式；未选择或非法模式时 MUST 拒绝烘焙。工具 MUST 参考 Animator root motion 采样结果，但 MUST NOT 递归扫描 `PlayerSO`、旧 SO/config 或 `Ref` 中 BBB 数据并写回。

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

#### Scenario: 作者未选择有效模式
- **WHEN** 作者保持未指定模式或提供未知模式
- **THEN** 工具 MUST 中止烘焙并报告明确配置错误
- **AND** 工具 MUST NOT 创建、覆盖或自动修复目标资产

### Requirement: Root Motion 求值器从累计曲线计算本帧 delta
系统 MUST 提供运行时求值器，从有效的 `RootMotionCurveAsset` 按播放时间和显式求值模式计算 root motion delta。求值器 MUST 对累计曲线在 `previousTime` 和 `currentTime` 的差值计算本帧本地位移和 yaw 变化。完整本地位移模式 MUST 使用累计本地 XYZ 差值；前向距离模式 MUST 使用累计 forward distance 差值生成本地 forward 位移。无效模式 MUST 使 sample 与 delta 求值失败，MUST NOT 使用“非前向距离模式即完整本地位移”的分支。

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

#### Scenario: 无效模式进入求值器
- **WHEN** 求值器收到模式未指定、缺失或未知的曲线资产
- **THEN** 求值 MUST 失败并保留零输出
- **AND** 系统 MUST NOT 推断曲线模式、自动写回资产或改用其它 motion 来源

### Requirement: RootMotionCurveAsset 与 Timeline 内联位移必须保持单向边界
`RootMotionCurveAsset` 是离线烘焙、检查和重烘焙动画派生曲线的 authoring 数据；`MotionCurveClip` 是 Timeline Runtime 实际提交的 motion fact。`MotionCurveTrack` MUST 只采样自己的内联曲线，MUST NOT 自动读取、按名称查找或隐式引用 `RootMotionCurveAsset`。系统 MUST NOT 创建资产到 Timeline 的自动同步、双写或运行时导入路径。

#### Scenario: Timeline 采样内联 MotionCurveClip
- **WHEN** `TimelinePlaybackScheduler` 推进一个包含 `MotionCurveTrack` 的 Timeline
- **THEN** 轨道 MUST 从该 Timeline 内联 `MotionCurveClip` 采样位移与 yaw
- **AND** 轨道 MUST NOT 读取未被显式转写的 `RootMotionCurveAsset`
- **AND** 最终位移 MUST 继续通过正式 motion 管线提交

#### Scenario: 独立烘焙资产没有 Timeline 引用
- **WHEN** 项目中存在没有被任何 Timeline 显式转写的 `RootMotionCurveAsset`
- **THEN** 该资产 MUST NOT 自动影响角色 Runtime 位移
- **AND** 系统 MUST NOT 根据动画名称、目录或曲线内容建立隐式关联

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

### Requirement: Root Motion 通过角色 motion 管线应用
系统 MUST 将动画派生位移或手画位移作为正式 motion contribution 或 modifier 提交到角色 motion 管线，由正式 motion resolver 或 `CharacterMotionStage` 生成并应用最终移动。Timeline 轨道、采样器、动画表现层、Animator 或 Animancer adapter MUST NOT 直接修改角色 Transform 来应用 root motion 或 motion curve。

#### Scenario: Timeline 采样 MotionCurve
- **WHEN** `TimelinePlaybackScheduler` 推进 active Timeline 并采样 `MotionCurveTrack`
- **THEN** 系统 MUST 根据本帧时间区间计算 motion curve delta
- **AND** 系统 MUST 将 delta 转成正式 motion contribution
- **AND** 最终位移 MUST 通过角色 motion 管线应用

#### Scenario: 多个 motion 来源同帧存在
- **WHEN** 同一帧存在输入移动、Timeline motion curve、击退或其它 motion 来源
- **THEN** 系统 MUST 通过正式 motion 仲裁规则生成最终 `MotionIntent`
- **AND** 系统 MUST NOT 让任意来源绕过管线直接移动角色

### Requirement: 旧 BBB Root Motion 数据链路不得进入正式运行时
系统 MAY 参考 `Assets/Ref/BBB` 中的 root motion 采样算法，但正式实现 MUST 位于 `Assets/GameScripts/Main` 下，并使用项目命名空间和角色管线类型。系统 MUST NOT 从正式运行时代码引用 BBB 旧数据结构。

#### Scenario: 迁移 BBB 参考逻辑
- **WHEN** 实现烘焙工具时参考 BBB `RootMotionExtractor` 或 `WarpedMotionExtractor`
- **THEN** 实现 MUST 改名并放入正式模块
- **AND** 实现 MUST 删除 `PlayerSO` 扫描和旧数据写回
- **AND** 正式代码 MUST NOT 引用 `BBBNexus.MotionClipData` 或 `BBBNexus.WarpedMotionData`

### Requirement: 动画表现淡入淡出不得成为 Root Motion 路径

Animancer transition、state blending 与 FadeGroup MUST只影响 visual animation pose 和 layer/state weight，MUST不从动画混合结果推导 gameplay 位移，不提交 motion contribution，也 MUST不修改逻辑 Transform。动画派生位移仍 MUST由显式 MotionCurveTrack、MotionResolver 与 CharacterMotionStage 处理。

#### Scenario: 闪避 Fade 与 MotionCurve 同时运行

- **WHEN** 闪避 Timeline 的 MotionCurveTrack 提交逻辑 motion
- **AND** Animancer 对动画 state 执行 transition/fade
- **THEN** CharacterMotionStage MUST只应用正式 motion contribution
- **AND** 动画 fade MUST只改变 visual pose 权重
- **AND** 角色逻辑位移 MUST不被重复计算
