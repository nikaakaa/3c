## MODIFIED Requirements

### Requirement: Camera Sequence 必须使用注册的有限算法组合

系统 MUST 使用 Profile 注册的默认 Sequence 与强类型 stage 计算构图。FramePlanner MUST 保留基础轨道与手动偏移的明确合成；SequenceTransition MUST 处理进入、退出、Cut、BlendIn、BlendOut，History MUST 按正式时间保持连续状态。字段必须具有明确单位、坐标空间、消费者和依赖，未知 stage、未闭合公式或无消费者字段 MUST 明确拒绝，不得以默认值或静默忽略发布。

#### Scenario: 默认镜头接收鼠标输入

- **WHEN** 默认轨道已提供基础角度且本帧收到有效 Look
- **THEN** 系统 MUST 按响应权叠加手动角度，不得被基础轨道覆盖
- **AND** 普通推进 MUST 保持已定义的连续历史，只有明确 Reset 才重建相应状态

#### Scenario: 作者配置无消费者字段

- **WHEN** 一个字段被填写但当前算法未实现其语义
- **THEN** 系统 MUST 定位字段并拒绝该配置，或在已确认迁移中删除该字段及全部配置入口
- **AND** MUST 不把拷贝进 Projection 当作字段已经生效

#### Scenario: 默认序列

- **WHEN** 本帧没有 active camera sequence request
- **THEN** resolver MUST 使用 Profile 的正式默认 Sequence
- **AND** 该默认状态 MUST NOT 需要 BTSMTL 每帧显式提交

#### Scenario: 技能特写覆盖瞄准

- **WHEN** 同一帧存在 Aim Sequence 和更高优先级 SkillCloseup Sequence
- **THEN** resolver MUST 选择或混合到 SkillCloseup Sequence
- **AND** debug MUST 能追踪获胜请求的 source id 或 action instance id

### Requirement: Camera Effect owner 必须按固定顺序裁决表现

系统 MUST 将 Override、Zoom、Stretch、Shake、Shot 和环境约束作为职责明确的求解阶段。各 effect owner MUST 拥有自身资源、时间、空间、组合和退出语义，CameraEffectEvaluator 只负责顺序及生命周期转发。所有影响镜头位姿的效果 MUST 在最终环境约束与 RigAdapter 应用之前完成。尚未闭合的资源或消费者 MUST 明确拒绝；补齐实现之前不得仅移除错误。

#### Scenario: 受击效果接近墙体

- **WHEN** 一个已支持效果改变镜头期望位置
- **THEN** 该结果 MUST 继续进入统一环境约束，再交给 RigAdapter
- **AND** 效果 MUST 不通过改变角色或 follow/aim Transform 伪造镜头位移

#### Scenario: 命中帧震屏

- **WHEN** Timeline 或 Graph 提交正式 CameraShakeRequest
- **THEN** 内部 Camera Runtime MUST 保留请求的生命周期、顺序和 debug 来源，并交由对应 owner 求值
- **AND** 未闭合的 Shake MUST 明确拒绝，不得改变角色或目标伪造效果

#### Scenario: FOV 效果与特写同帧存在

- **WHEN** FOV 效果与专用特写请求在同帧有效
- **THEN** 效果 owner MUST 按正式固定顺序和权重修正计划
- **AND** MUST 不由各效果分别写入 Cinemachine lens 争夺结果

### Requirement: Cinemachine 必须是 CameraRigAdapter 实现细节

系统 MUST 由 Character Presentation 内部的 Planner、Transition、History 与效果/环境约束阶段求解 CameraFramePlan，通过 ICameraRigAdapter 的 CinemachineCameraRigAdapter 实现应用位姿、镜头参数并回读 CameraRigResult/CameraBasisSnapshot。Cinemachine 组件 MUST 不重新裁决业务目标、手动输入、动作生命周期或叠加一套未声明的 orbit/damping。Brain 推进必须由正式相机输出时序唯一负责；业务节点和 Timeline MUST 不直接写 Camera 或虚拟相机状态。

#### Scenario: 默认序列输出到 Cinemachine

- **WHEN** Adapter 收到最终 CameraFramePlan
- **THEN** 它 MUST 应用该计划并推进明确绑定的 Brain，回读实际输出
- **AND** MUST 不重新运行独立 FreeLook 输入或另一套跟随平滑

#### Scenario: Shot 使用专用 virtual camera

- **WHEN** 已支持的 CameraFramePlan 声明需要专用 Shot 承载
- **THEN** Adapter MAY 按正式计划切换明确绑定的 virtual camera 承载
- **AND** priority、进入和退出生命周期 MUST 由 Presentation runtime 控制，不得另加业务裁决或重复混合

### Requirement: Camera debug 必须解释状态和输出

系统 MUST 从同一 Runtime 状态提供原始/消费 Look、响应权、基准角和手动偏移、限幅、目标与请求来源、generation/action/cycle、时间域、blend、Reset、效果贡献、环境修正前后和最终 RigResult，并保留 Projection 身份与作者来源导航。诊断 MUST 不另算另一套相机；记录/回放 MUST 使用正式相机初始状态和输入合同，不依赖旧 Controller 或隐式场景搜索。

#### Scenario: 排查 look 不响应

- **WHEN** 作者查看当前相机诊断
- **THEN** 诊断 MUST 能区分未采集、被响应抑制、已限幅、正在过渡或实际输出无效
- **AND** MUST 提供对应帧号与配置/请求来源

#### Scenario: 排查技能后镜头残留

- **WHEN** 技能 action instance 已结束
- **THEN** debug MUST 显示该 action-scoped camera request 是否已经清理或正在正式退出
- **AND** 当前镜头 MUST 能追踪到仍然有效的请求来源与退出原因

## ADDED Requirements

### Requirement: 相机环境约束必须独立于平台查询实现

相机环境约束 MUST 消费最终期望计划、显式物理场景、正式碰撞配置、近裁剪保护体、自身/层/触发器过滤、delta 与 Reset，并输出安全计划及原因。抽象求解 MUST 不直接查找 Unity 场景对象；Unity 查询实现只返回环境事实。系统 MUST 处理起点重叠、转角移动、缩回、恢复和无合法空间，不能以未约束位置作为静默补齐。

#### Scenario: 镜头离开遮挡物

- **WHEN** 环境重新允许更远的期望位置
- **THEN** 系统 MUST 按正式恢复规则推进距离，且最终保护体仍满足本帧约束
- **AND** MUST 不改写角色跟随目标来实现避障

### Requirement: 相机请求生命周期必须保留稳定身份和同权裁决

Sequence、Response、Target 和效果请求 MUST 使用明确的 source、generation、action instance、cycle 与 event 身份，按各自裁决域的稳定规则处理同权候选。自然结束、取消、事件撤销、Owner 销毁与业务目标失效 MUST 进入正式退出合同，不能靠字典插入顺序或通用 Weight=0 代替。相机不拥有独立网络回滚日志。

#### Scenario: 同权请求发生替换

- **WHEN** 两个请求 priority 与 weight 相同
- **THEN** resolver MUST 按声明的身份规则得出稳定结果并记录原因
- **AND** 旧请求退出 MUST 不误删新 generation 或新 cycle 的请求
