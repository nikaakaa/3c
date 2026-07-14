## ADDED Requirements

### Requirement: Root Motion 曲线资产表达动画派生位移
系统 MUST 使用独立 `RootMotionCurveAsset` 表达从 `AnimationClip` 派生出的 root motion 曲线。该资产 MUST 保存源动画、时长、采样率、累计本地位置 XYZ 曲线和累计 yaw 曲线。该资产 MUST NOT 保存 footphase、动作窗口、body claim、locomotion state 或旧 BBB motion 配置。

#### Scenario: 烘焙生成曲线资产
- **WHEN** 用户对一个 `AnimationClip` 执行 root motion 曲线烘焙
- **THEN** 系统 MUST 生成或覆盖一个 `RootMotionCurveAsset`
- **AND** 资产 MUST 记录源动画和采样参数
- **AND** 资产 MUST 包含累计本地位置 XYZ 和累计 yaw 曲线
- **AND** 资产 MUST NOT 生成旧 `MotionClipData`、`WarpedMotionData` 或其它旧配置节点

#### Scenario: 动画没有 root motion
- **WHEN** 动画没有有效 root motion 位移或旋转
- **THEN** 烘焙结果 MAY 包含零值曲线
- **AND** 系统 MUST NOT 自动查找其它配置作为 fallback

### Requirement: 编辑器烘焙器只生成正式曲线资产
系统 MUST 提供正式编辑器工具，从指定 `AnimationClip` 和采样 Prefab 烘焙 root motion 曲线。工具 MUST 参考 Animator root motion 采样结果，但 MUST NOT 递归扫描 `PlayerSO`、旧 SO/config 或 `Ref` 中 BBB 数据并写回。

#### Scenario: 使用采样 Prefab 烘焙
- **WHEN** 用户指定 `AnimationClip`、采样 Prefab 和输出位置
- **THEN** 工具 MUST 临时实例化采样 Prefab
- **AND** 工具 MUST 使用 Prefab 上的 `Animator` 采样目标 clip
- **AND** 工具 MUST 把采样得到的 root motion 写入 `RootMotionCurveAsset`
- **AND** 工具 MUST 销毁临时实例

#### Scenario: 缺少采样条件
- **WHEN** 采样 Prefab 缺少 `Animator` 或可用 controller
- **THEN** 工具 MUST 中止烘焙并报告错误
- **AND** 工具 MUST NOT 使用场景对象搜索、默认 Prefab 或自动生成兼容配置作为 fallback

### Requirement: Root Motion 求值器从累计曲线计算本帧 delta
系统 MUST 提供运行时求值器，从 `RootMotionCurveAsset` 按播放时间计算 root motion delta。求值器 MUST 使用累计曲线在 `previousTime` 和 `currentTime` 的差值计算本地位移和 yaw 变化。

#### Scenario: 正常前进播放
- **WHEN** 动画播放时间从 `previousTime` 前进到 `currentTime`
- **THEN** 求值器 MUST 采样两个时间点的累计本地位置
- **AND** 求值器 MUST 输出本地 delta position
- **AND** 求值器 MUST 采样两个时间点的累计 yaw
- **AND** 求值器 MUST 输出 delta yaw

#### Scenario: 单次播放越界
- **WHEN** 播放时间小于 0 或大于资产时长
- **THEN** 求值器 MUST 按单次播放规则 clamp 到合法范围
- **AND** 求值器 MUST NOT 自动切换 loop、倒放或其它隐式模式

### Requirement: Timeline 和 Action 显式引用 Root Motion 曲线
系统 MUST 让 Timeline 或 Action 创作数据显式引用 `RootMotionCurveAsset`。系统 MUST NOT 通过动画名称、目录、同名 asset 或旧配置扫描自动寻找 root motion 曲线。

#### Scenario: Timeline 动画片段带 root motion
- **WHEN** Timeline 动画片段配置了 `RootMotionCurveAsset`
- **THEN** Timeline 采样阶段 MUST 使用该显式引用计算 root motion
- **AND** 该片段的动画贡献和 root motion 贡献 MUST 使用同一播放时间来源

#### Scenario: Timeline 动画片段未配置 root motion
- **WHEN** Timeline 动画片段没有配置 `RootMotionCurveAsset`
- **THEN** 系统 MUST 视为该片段不提交 root motion
- **AND** 系统 MUST NOT 自动按命名约定或旧 SO/config 查找 root motion

### Requirement: Root Motion 通过角色 motion 管线应用
系统 MUST 将 root motion 采样结果提交到角色 motion 管线，由正式 motion resolver 或 `CharacterMotionStage` 生成并应用最终移动。Timeline 轨道、采样器、动画表现层、Animator 或 Animancer adapter MUST NOT 直接修改角色 Transform 来应用 root motion。

#### Scenario: Timeline 采样 root motion
- **WHEN** `TimelinePlaybackScheduler` 推进 active Timeline 并采样带 root motion 的动画片段
- **THEN** 系统 MUST 根据本帧 clip 时间区间计算 root motion delta
- **AND** 系统 MUST 将 delta 转成正式 motion 数据
- **AND** 最终位移 MUST 通过角色 motion 管线应用

#### Scenario: 多个 motion 来源同帧存在
- **WHEN** 同一帧存在输入移动、root motion、击退或其它 motion 来源
- **THEN** 系统 MUST 通过正式 motion 仲裁规则生成最终 `MotionIntent`
- **AND** 系统 MUST NOT 让任意来源绕过管线直接移动角色

### Requirement: 旧 BBB Root Motion 数据链路不得进入正式运行时
系统 MAY 参考 `Assets/Ref/BBB` 中的 root motion 采样算法，但正式实现 MUST 位于 `Assets/GameScripts/Main` 下，并使用项目命名空间和角色管线类型。系统 MUST NOT 从正式运行时代码引用 BBB 旧数据结构。

#### Scenario: 迁移 BBB 参考逻辑
- **WHEN** 实现烘焙工具时参考 BBB `RootMotionExtractor` 或 `WarpedMotionExtractor`
- **THEN** 实现 MUST 改名并放入正式模块
- **AND** 实现 MUST 删除 `PlayerSO` 扫描和旧数据写回
- **AND** 正式代码 MUST NOT 引用 `BBBNexus.MotionClipData` 或 `BBBNexus.WarpedMotionData`
