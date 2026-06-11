## ADDED Requirements
### Requirement: 相机消费表现层输出
系统 MUST 让 Cinemachine Follow/LookAt 目标代理消费角色表现层输出，使相机跟随位置与角色可见位置来自同一条表现主路径，而不是直接消费 tick 阶梯化角色真实 Transform。

#### Scenario: 高刷新率跟随表现根
- **WHEN** 角色真实 Transform 由 60Hz simulation tick 推进
- **AND** 渲染帧率高于 simulation tick rate
- **THEN** `CameraFollowTarget` / `CameraAimTarget` MUST 使用表现层输出更新
- **AND** Cinemachine MUST NOT 直接追随未插值的角色真实 Transform

#### Scenario: 相机目标代理保持统一
- **WHEN** `Third Person Rail CM vcam`、FreeLook 或后续第三人称 vcam 需要 Follow/LookAt
- **THEN** 它们 MUST 继续使用相机主路径提供的目标代理或等价输出
- **AND** 它们 MUST NOT 各自维护绕过表现层输出的场景目标更新逻辑

#### Scenario: 缺少 tick 信息安全退化
- **WHEN** 表现层输出缺少 tick driver 或有效样本
- **THEN** 相机目标代理 MUST 安全退化为跟随当前真实锚点或当前表现根
- **AND** 相机 MUST 不因为插值数据缺失而跳到无效位置

#### Scenario: 相机碰撞仍在 Cinemachine 边界
- **WHEN** 相机消费表现层输出
- **THEN** `CameraArmCollisionConstraint` MUST 继续在 Cinemachine 管线边界内修正最终相机位置
- **AND** 表现层插值 MUST NOT 新增第二套相机碰撞或缩臂路径
