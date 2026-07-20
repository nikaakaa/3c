# character-root-motion-curves Specification

## Purpose
定义 root motion 曲线资产和烘焙链路：从指定 `AnimationClip` 和采样 Prefab 生成 `RootMotionCurveAsset`，保存累计位移与 yaw 曲线，作为离线 authoring 数据供作者生成、检查和重烘焙。Compiler MUST将 Timeline 正式引用的曲线降低为 Program constant 与 MotionCurve operation，Runtime MUST经 CharacterMotionRequest 和 WorldSolver 应用，不从 AnimationClip 自动采样，也不恢复旧 BBB motion 配置或 footphase/body claim 数据源。
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

RootMotionCurveAsset MUST继续作为动画派生累计曲线 authoring source；Compiler MUST将 Timeline 正式引用的曲线编译为 portable Program constants。Runtime MUST只读取 compiled constants，MUST不同时读取 RootMotionCurveAsset 与另一份 inline runtime curve。

#### Scenario: 编译 Dodge 曲线

- **WHEN** Dodge Timeline 引用 RootMotionCurveAsset
- **THEN** Compiler MUST生成唯一 portable curve constant
- **AND** Kernel MUST不读取 Unity AnimationCurve asset

### Requirement: Timeline 不得通过动画片段直接提交 Root Motion

Compiled Timeline MUST只通过正式 MotionCurve operation 产生 MotionContribution。AnimationClip、Animancer state、fade 与 sampled pose MUST不进入 WorldRequest 或 WorldSimulationState。

#### Scenario: Attack 动画包含 Root Transform

- **WHEN** Presentation 播放带 Root Transform 的动画片段
- **THEN** 逻辑位移 MUST仍只来自 compiled MotionCurve

### Requirement: Root Motion 通过角色 motion 管线应用

Root Motion curve delta MUST作为原始动画派生位移进入 Kernel Evaluate 的统一 contribution resolve。已解析channel MAY由Operation Set声明的正式Motion Modifier在WorldSolver前修正，再生成portable WorldRequest，并由Session WorldSolver batch产生actual body result。MotionCurve、Modifier与Timeline MUST不直接写Transform或调用CharacterController；AnimationClip、Animancer与Presentation MUST不成为Gameplay修正来源。

#### Scenario: Root Motion 被墙阻挡

- **WHEN** compiled curve 请求的位移穿过墙面
- **THEN** WorldSolver actual result MUST决定 WorldSimulationState

#### Scenario: 目标 Warp 修正动作曲线

- **WHEN** Action MotionCurve的resolved channel具有合法的compiled MotionWarp Modifier
- **THEN** Modifier MUST只修正该resolved channel后再构造唯一WorldRequest
- **AND** 原始MotionCurve constant与raw contribution MUST保持不变

### Requirement: 旧 BBB Root Motion 数据链路不得进入正式运行时
系统 MAY 参考 `Assets/Ref/BBB` 中的 root motion 采样算法，但正式实现 MUST 位于 `Assets/GameScripts/Main` 下，并使用项目命名空间和角色管线类型。系统 MUST NOT 从正式运行时代码引用 BBB 旧数据结构。

#### Scenario: 迁移 BBB 参考逻辑
- **WHEN** 实现烘焙工具时参考 BBB `RootMotionExtractor` 或 `WarpedMotionExtractor`
- **THEN** 实现 MUST 改名并放入正式模块
- **AND** 实现 MUST 删除 `PlayerSO` 扫描和旧数据写回
- **AND** 正式代码 MUST NOT 引用 `BBBNexus.MotionClipData` 或 `BBBNexus.WarpedMotionData`

### Requirement: 动画表现淡入淡出不得成为 Root Motion 路径

Animancer fade、animation state weight、Presentation retention 和 visual Timeline sample MUST不改变 compiled MotionCurve contribution、WorldRequest 或 Character/World state。Gameplay 位移权重只能来自 Program authoring 规则。

#### Scenario: 攻击动画淡出

- **WHEN** Attack animation 仍在 Outgoing fade
- **THEN** Presentation MAY继续采样 pose
- **AND** MUST不继续产生 Gameplay Root Motion

### Requirement: MotionCurve Clip控制曲线必须进入typed Curve Channel Catalog

Timeline中的MotionCurve Clip MUST继续唯一保存Weight、Position X/Y/Z、Yaw与Ease In/Out曲线，并 MUST通过显式registered ChannelId进入同一个Timeline Curve Editor。Position channel MUST声明meter单位与unbounded value domain，Yaw MUST声明degree单位与unbounded value domain，Weight和Ease MUST声明`[0,1]` bounded domain。Curve Editor MUST只调用MotionCurve Clip正式mutation API；Compiler MUST继续把这些曲线降低为既有portable Program constant与MotionCurve operation，不得新增Generic Curve Runtime、第二份inline curve或Presentation motion路径。

#### Scenario: 在Timeline编辑Position Z

- **WHEN** 作者展开MotionCurve Clip的Position Z channel并移动key
- **THEN** Curve Editor MUST按Clip-local time与meter value显示和提交完整curve
- **AND** Semantic Compiler MUST沿既有MotionCurve operation重新编译该curve
- **AND** Animation、Marker Sync与Presentation MUST不成为该位移的第二消费者

#### Scenario: MotionCurve引用RootMotionCurveAsset

- **WHEN** MotionCurve作者数据来自正式RootMotionCurveAsset
- **THEN** RootMotionCurveAsset MUST继续是外部烘焙source
- **AND** Timeline Curve Catalog MUST不复制该资产全部曲线形成第二份authoring

#### Scenario: Position curve超出权重范围

- **WHEN** Position X key值大于1或小于0
- **THEN** Curve Editor MUST按unbounded meter domain显示与编辑
- **AND** MUST不Clamp到`[0,1]`
