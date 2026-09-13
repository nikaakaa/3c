## MODIFIED Requirements

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

Timeline Editor MUST 通过显式 typed Curve Channel Catalog 显示和编辑 Timeline owner 正式拥有的 Continuous Curve。每个 descriptor MUST 声明稳定 ChannelId、owner 类型、显示名、颜色、time/value domain、单位、完整读取、正式 mutation 和领域 validator。Catalog MUST 覆盖 Animation Segment 的 Weight/Ease In/Out、MotionCurveClip 的 Weight/Ease In/Out、MotionWarpClip 的 Position/Yaw Progress、CameraSequenceClip/CameraResponseClip/CameraResourceClip 的 Weight/Ease In/Out。MotionCurve 的 PositionX/Y/Z/Yaw MUST 不再是 Timeline-local 可写通道；Editor MUST 提供类型化源选择、区间/播放配置及真实 RootMotionCurveAsset owner 导航。共享相机曲线仍 MUST 只读引用和 owner 导航；presentation.locomotion-phase、presentation.foot-placement-weight、AnimationClip 骨骼曲线与其它 Clip 注册 Curve MUST 由 Unity Animation Window 编辑，不进入 Timeline Catalog。

具有注册 Timeline channel 的 Track MUST 显示可折叠 CURVES 分组，每个 ChannelId 有独立 lane。Clip/Segment MUST 只在自己的 StartFrame..EndFrame 范围显示其 Timeline-local curve、key 和边界；重叠内容 MUST 不在作者层合并。Curve Lane MUST 按完整 AnimationCurve.Evaluate 绘制插值，显示原始 key、tangent handle、游标 time/value、value reference 和单位。

#### Scenario: 展开Animation Track曲线

- **WHEN** 作者展开包含 Animation Segment 的 CURVES 分组
- **THEN** Editor MUST 只显示 Weight、Ease In 与 Ease Out 三个 Timeline-local typed channel
- **AND** Foot Placement Weight 与 Locomotion Phase MUST 不出现在该分组

#### Scenario: 展开MotionCurve Track曲线

- **WHEN** 作者展开 MotionCurveClip 的 CURVES 分组
- **THEN** Editor MUST 提供 Weight 与 Ease In/Out 局部编辑，Position/Yaw 提供源引用和 owner 导航而非本地可写 lane
- **AND** 源 Position/Yaw 的展示 MUST 保持明确单位与 unbounded value 含义，不创建局部曲线副本

## ADDED Requirements

### Requirement: 修改共享运动曲线必须进入真实源 owner

RootMotionCurveAsset MUST 不进入 Timeline-local 可写 Curve Channel Catalog。源曲线修改 MUST 通过正式源资产编辑能力保存完整曲线、Undo 和依赖失效；Timeline 只能导航或只读展示源数据。修改 Clip 的 Weight/Ease、源区间或播放配置 MUST 不修改共享源。作者需要独立运动内容时 MUST 显式创建独立正式源，不自动复制素材或在 Clip 内恢复覆盖数组。Inspector 重绘 MUST 不触发烘焙、采样迁移或重操作。

#### Scenario: 修改单个片段淡入

- **WHEN** 作者修改引用共享源的一个 MotionCurveClip 的 Ease In
- **THEN** MUST 只修改该 Clip 的正式局部配置
- **AND** MUST 不修改源资产或其它引用者

#### Scenario: 修改共享源运动

- **WHEN** 作者通过真实源 owner 编辑运动曲线
- **THEN** MUST 保存源曲线并使既有依赖机制标记相关编译产物失效
- **AND** MUST 不把修改复制回各 Timeline 内嵌字段
