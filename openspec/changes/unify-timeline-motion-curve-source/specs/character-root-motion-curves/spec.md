## MODIFIED Requirements

### Requirement: Root Motion 曲线资产表达动画派生位移

系统 MUST 使用独立 RootMotionCurveAsset 作为运动累计曲线的正式源，不新建 Timeline 专属源类型。动画烘焙资产 MUST 保存源动画、时长、采样率和显式有效求值模式；存量嵌入曲线迁移 MUST 保存完整内容、明确源时间及有效求值模式，保留可确认来源信息，来源未知时 MUST 不伪造动画或采样率、不得声称可直接重烘焙。Unspecified、缺失或未知求值模式 MUST 报告错误，不能默认转换。FullLocalDelta MUST 表达累计本地 XYZ 与 yaw；ForwardDistanceYaw MUST 表达累计前向距离与 yaw，按角色 forward 解释位移。资产 MUST 不保存 Timeline 播放窗口、混合、footphase、body claim、locomotion state 或旧 BBB motion 配置。

#### Scenario: 烘焙生成完整本地曲线资产

- **WHEN** 用户以完整本地位移模式对 AnimationClip 执行烘焙
- **THEN** MUST 创建或覆盖 RootMotionCurveAsset，记录源动画、采样参数和 FullLocalDelta 模式
- **AND** MUST 保存累计本地 XYZ/yaw，不生成旧 MotionClipData 或 WarpedMotionData

#### Scenario: 烘焙生成前向距离曲线资产

- **WHEN** 用户以前向距离模式对 AnimationClip 执行烘焙
- **THEN** MUST 创建或覆盖 RootMotionCurveAsset，记录源动画、采样参数和 ForwardDistanceYaw 模式
- **AND** MUST 保存累计 forward distance/yaw，不使用动画横向漂移作为最终角色侧向位移

#### Scenario: 动画没有 root motion

- **WHEN** 动画没有有效运动
- **THEN** 烘焙结果可以为零曲线
- **AND** MUST 不查找其它配置作为 fallback

#### Scenario: 读取无效模式资产

- **WHEN** 资产模式未指定、字段缺失或模式未知
- **THEN** MUST 报告该资产配置错误，不推断求值模式
- **AND** MUST 不产生 sample、delta 或 motion contribution

### Requirement: MotionCurve Clip控制曲线必须进入typed Curve Channel Catalog

MotionCurveClip MUST 只拥有 Weight 与 Ease In/Out 等片段局部曲线，以及正式源引用和使用配置；PositionX/Y/Z/Yaw MUST 由 RootMotionCurveAsset 唯一拥有。Timeline UI MUST 消费正式 typed 字段，只为局部曲线提供注册 channel 编辑；源运动曲线 MUST 提供只读展示及真实 owner 导航，不创建片段副本。Position MUST 保持 meter/unbounded domain，Yaw MUST 保持 degree/unbounded domain，Weight/Ease MUST 保持 [0,1] bounded domain。源语义、时间映射和 mutation MUST 来自正式 owner，不由 UI 另行定义；技能独立内容入口与 Control/Motion 资源绑定 MUST 消费同一正式源，不创建 Generic Curve Runtime 或角色总包。

#### Scenario: 在Timeline编辑Position Z

- **WHEN** 作者在 Timeline 请求编辑源 Position Z
- **THEN** Editor MUST 导航到真实源 owner，以源正式时间域和 meter 单位编辑
- **AND** MUST 发布源内容修订及依赖变化供技能或控制正式消费绑定处理，不在 Timeline-local channel 写入副本
- **AND** Animation 采样与 Presentation MUST 不成为 Gameplay 位移的第二来源

#### Scenario: MotionCurve引用RootMotionCurveAsset

- **WHEN** MotionCurve 作者数据来自正式 RootMotionCurveAsset
- **THEN** RootMotionCurveAsset MUST 继续是唯一外部源
- **AND** Timeline Curve Catalog MUST 不复制该资产全部曲线形成第二份 authoring

#### Scenario: Position curve超出权重范围

- **WHEN** 源 Position X key 值大于 1 或小于 0
- **THEN** 正式源编辑与 Timeline 只读展示 MUST 使用 unbounded meter domain
- **AND** MUST 不 Clamp 到 [0,1]

## REMOVED Requirements

### Requirement: RootMotionCurveAsset 与 Timeline 内联位移必须保持单向边界

**Reason**: 旧名称仍暗示内联数据存在，本次统一为源资产唯一所有权。

**Migration**: 使用“运动源与 Timeline 数据段必须保持唯一所有权”要求，迁移旧嵌入曲线后删除其读写路径；技能使用独立技能内容，控制使用正式 Control/Motion 资源绑定，portable Gameplay 不回读 Unity 资产。

## ADDED Requirements

### Requirement: 运动源与 Timeline 数据段必须保持唯一所有权

RootMotionCurveAsset MUST 唯一拥有累计运动曲线与源时间；MotionCurveClip MUST 仅保存正式类型化源引用、源区间和播放/混合配置，不保留内联 PositionX/Y/Z/Yaw。TimelineData MUST 只拥有轨道和片段，不隐式拥有外部源的删除权。技能侧 MUST 通过独立技能内容入口消费，控制侧 MUST 通过正式 Control/Motion 资源绑定消费，可共享同一作者源而不复制第二份。portable Gameplay MUST 只读取正式采用的领域运行数据与绑定，不回读 Unity 资产。系统 MUST 不恢复角色总 Program、整包 Projection、旧 ControlMotion catalog 或换名新总包，不通过全角色 Build 采用曲线变化。

#### Scenario: 编译 Dodge 曲线

- **WHEN** Dodge Timeline 引用 RootMotionCurveAsset
- **THEN** 独立技能内容处理 MUST 消费该源及其片段映射，输出所需的正式技能数值数据
- **AND** Kernel MUST 不读取 Unity AnimationCurve asset

#### Scenario: 删除使用共享源的 Timeline

- **WHEN** 作者删除或重建一个 Timeline
- **THEN** MUST 仅处理其拥有的输出范围
- **AND** MUST 不删除其它 Timeline 或动作节点仍使用的外部源

### Requirement: MotionCurve 源区间必须使用统一时间映射

源曲线 MUST 使用明确的秒时间域，Clip MUST 按正式源区间和播放配置映射 Timeline 时间，映射语义 MUST 由 Timeline 唯一拥有。MotionWarp、独立技能内容处理与 Control/Motion 资源绑定 MUST 消费同一语义及源求值模式。累计曲线 MUST 通过前后差值产生位移，源区间开始前的累计值 MUST 不成为额外位移。曲线有效窗口结束早于片段结束时 MUST 保留原有终值保持和片段权重/生命周期。系统 MUST 声明源内容/区间修订与依赖结果，由对应领域正式采用匹配的消费绑定，不得只按资产路径判断内容未变。预览 MUST 只展示正式采用状态与运行结果，不通过旧角色总包或现场读取资产生成替代运行结果。

#### Scenario: 使用非零起点的数据段

- **WHEN** Clip 从源曲线中间开始播放
- **THEN** Warp、技能处理和控制资源绑定 MUST 使用相同区间映射，预览 MUST 观察正式采用结果
- **AND** 位移 MUST 为该段前后累计值差，不包含该段之前的累计偏移

#### Scenario: 运动窗口先于片段结束

- **WHEN** 源曲线有效播放窗口结束但 Clip 尚未结束
- **THEN** 源采样 MUST 保持区间终值，后续源运动 delta 为零
- **AND** MUST 不擅自缩短 Clip 权重或占用生命周期

#### Scenario: 同一运动源同时被技能与控制使用

- **WHEN** 源内容或区间修订影响技能和控制的使用
- **THEN** MUST 声明各自真实依赖结果，分别通过独立技能入口和 Control/Motion 正式绑定采用
- **AND** MUST 不复制作者源、合成角色总包或将未采用结果标为预览已生效

### Requirement: 存量内嵌运动曲线必须一次无损迁入正式源

迁移 MUST 以 Attack/Dodge 等受影响正式 Clip 的实际嵌入曲线为输入，保存全部关键帧、value、切线、权重、WeightedMode、插值及 pre/post wrap，并等价转换时间域。迁移 MUST 不抽点、重采样或以动画重烘焙替换现有内容。仅在内容、时间/求值语义与共享意图明确一致时才复用已有源。新源与引用保存完成前 MUST 不删除原数据；完成切换后 MUST 删除旧嵌入字段、旧读取/编辑/导出分支及一次性旧格式工具，不保留双读或兼容配置。

#### Scenario: 迁移归一化时间曲线

- **WHEN** 旧 Clip 的运动曲线按归一化时间表示
- **THEN** MUST 等价转换关键帧时间和切线为源秒时间域，保留完整曲线形状与 Clip/Warp 身份绑定
- **AND** MUST 不只改关键帧时间或强行合并片段结束与曲线结束

#### Scenario: 迁移源保存失败

- **WHEN** 正式源或 Clip 引用尚未成功保存
- **THEN** MUST 报告具体未完成范围并保留原数据
- **AND** MUST 不宣称迁移完成或删除仍被使用的旧字段数据
