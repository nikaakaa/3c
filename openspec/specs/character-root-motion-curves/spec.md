# character-root-motion-curves Specification

## Purpose
定义 root motion 曲线资产和烘焙链路：从指定 `AnimationClip` 和采样 Prefab 生成 `RootMotionCurveAsset`，保存累计位移与 yaw 曲线，作为离线 authoring 数据供作者生成、检查和重烘焙；该资产也承接 Timeline 存量运动曲线的无损迁移。Compiler MUST将 Timeline 正式引用的曲线降低为既有 Program constant 与 MotionCurve operation，Runtime MUST经 CharacterMotionRequest 和 WorldSolver 应用，不从 AnimationClip 自动采样，也不恢复旧 BBB motion 配置或 footphase/body claim 数据源。
## Requirements
### Requirement: Root Motion 曲线资产表达动画派生位移
系统 MUST 使用独立 `RootMotionCurveAsset` 表达从 `AnimationClip` 派生出的 root motion 曲线，也 MUST 作为 Timeline 存量运动曲线的唯一正式源。该资产 MUST 保存源动画、时长、采样率和显式有效的求值模式；迁移来源未知时 MUST 不伪造动画或采样率。`Unspecified`、缺失字段、默认零值和未知枚举值均为配置错误，MUST NOT 被解释为其它模式。完整本地位移模式 MUST 保存累计本地位置 XYZ 曲线和累计 yaw 曲线；前向距离模式 MUST 保存累计前向距离和累计 yaw，并在运行时按角色 forward 解释位移。该资产 MUST NOT 保存 Timeline 播放窗口、混合、footphase、动作窗口、body claim、locomotion state 或旧 BBB motion 配置。

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

### Requirement: 运动源与 Timeline 数据段必须保持唯一所有权

RootMotionCurveAsset MUST 唯一拥有累计运动曲线与源时间；MotionCurveClip MUST 仅保存正式类型化源引用、源区间和播放/混合配置，不保留内联 PositionX/Y/Z/Yaw。TimelineData MUST 只拥有轨道和片段，不隐式拥有外部源的删除权。Compiler MUST将源曲线降低为 portable Program constants，Runtime MUST只读取 compiled constants，MUST不同时读取 RootMotionCurveAsset 与另一份 inline runtime curve。系统 MUST不恢复角色总 Program、整包 Projection、旧 ControlMotion catalog 或换名新总包，不通过全角色 Build 采用曲线变化。

#### Scenario: 编译 Dodge 曲线

- **WHEN** Dodge Timeline 引用 RootMotionCurveAsset
- **THEN** Compiler MUST生成唯一 portable curve constant
- **AND** Kernel MUST不读取 Unity AnimationCurve asset

#### Scenario: 删除使用共享源的 Timeline

- **WHEN** 作者删除或重建一个 Timeline
- **THEN** MUST 仅处理其拥有的输出范围
- **AND** MUST 不删除其它 Timeline 或动作节点仍使用的外部源

### Requirement: MotionCurve 源区间必须使用统一时间映射

源曲线 MUST 使用明确的秒时间域，Clip MUST 按正式源区间和 Timeline 时间映射读取累计位置与 yaw；映射语义 MUST 由 Timeline 唯一拥有并被 MotionWarp、Semantic Compiler 与正式 Control/Motion 绑定复用。累计曲线 MUST 通过前后差值产生位移，源区间开始前的累计值 MUST 不成为额外位移。曲线有效窗口结束早于片段结束时 MUST 保留终值，后续 delta 为零且不改变 Clip 权重或生命周期。Runtime MUST不回读 Unity 资产。

#### Scenario: 使用非零起点的数据段

- **WHEN** Clip 从源曲线中间开始播放
- **THEN** 位移 MUST 为该段前后累计值差，不包含该段之前的累计偏移
- **AND** Warp 与编译结果 MUST 使用相同源区间映射

#### Scenario: 运动窗口先于片段结束

- **WHEN** 源曲线有效播放窗口结束但 Clip 尚未结束
- **THEN** 源采样 MUST 保持区间终值，后续源运动 delta 为零
- **AND** MUST 不擅自缩短 Clip 权重或占用生命周期

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

显式Player transition、source weight、Presentation retention和visual Timeline sample MUST不改变compiled MotionCurve contribution、WorldRequest或Character/World state。Gameplay位移权重只能来自Program authoring规则。

#### Scenario: 攻击动画淡出

- **WHEN** Attack animation 仍在 Outgoing fade
- **THEN** Presentation MAY继续采样 pose
- **AND** MUST不继续产生 Gameplay Root Motion

### Requirement: MotionCurve Clip控制曲线必须分离源曲线和局部曲线

Timeline中的MotionCurve Clip MUST只保存Weight、Ease In/Out、正式 RootMotionCurveAsset 引用、源区间和使用配置，并 MUST通过显式 typed 字段进入 Timeline Editor。Position X/Y/Z/Yaw MUST由 RootMotionCurveAsset 唯一拥有，不得作为 Timeline-local 可写 channel；源运动 MUST提供只读展示和真实 owner 导航。Weight和Ease MUST保持`[0,1]` bounded domain。Curve Editor MUST只调用正式 owner mutation；Compiler MUST继续把源曲线降低为既有 portable Program constant 与 MotionCurve operation，不得新增 Generic Curve Runtime、第二份 inline curve 或 Presentation motion路径。

#### Scenario: 在Timeline打开Position Z源

- **WHEN** 作者在 Timeline 查看 MotionCurve 的 Position Z
- **THEN** Editor MUST 导航到 RootMotionCurveAsset 真实 owner，以源秒时间和 meter 单位编辑
- **AND** MUST 不在 Timeline 写入 Position Z 曲线副本

#### Scenario: MotionCurve引用RootMotionCurveAsset

- **WHEN** MotionCurve作者数据来自正式RootMotionCurveAsset
- **THEN** RootMotionCurveAsset MUST继续是外部烘焙source
- **AND** Timeline Curve Catalog MUST不复制该资产全部曲线形成第二份authoring

#### Scenario: 源 Position 曲线超出权重范围

- **WHEN** 源 Position X key值大于1或小于0
- **THEN** RootMotionCurveAsset 编辑器 MUST按 unbounded meter domain 显示与编辑
- **AND** MUST不Clamp到`[0,1]`

### Requirement: 存量内嵌运动曲线必须一次无损迁入正式源

迁移 MUST 以 Attack/Dodge 等受影响正式 Clip 的实际内嵌曲线为输入，保存全部关键帧、value、切线、权重、WeightedMode、插值及 pre/post wrap，并将归一化时间等价转换为源秒时间。迁移 MUST 不抽点、重采样或以动画重烘焙替换现有内容；新源与引用保存完成前 MUST 不删除原数据，完成切换后 MUST 删除旧字段、旧读写/导出分支及一次性迁移入口，不保留双读或兼容配置。

#### Scenario: 迁移归一化时间曲线

- **WHEN** 旧 Clip 的运动曲线按归一化时间表示
- **THEN** MUST 等价转换关键帧时间和切线为源秒时间域，保留完整曲线形状与 Clip/Warp 身份绑定
- **AND** MUST 不强行合并片段结束与曲线结束
