# camera-arm-collision-constraint Specification

## Purpose
TBD - created by archiving change add-camera-arm-collision-constraint. Update Purpose after archive.
## Requirements
### Requirement: 单一相机臂碰撞入口
系统 MUST 为第三人称相机提供唯一的正式缩臂碰撞入口，并 MUST 避免 `CinemachineCollider`、`3rd Person Follow` 内置碰撞和自定义约束同时修正相机位置。

#### Scenario: 关闭重复碰撞入口
- **WHEN** 第三人称相机启用自定义相机臂碰撞约束
- **THEN** 对应 vcam 的 `CinemachineCollider` 不参与正式缩臂
- **AND** `3rd Person Follow` 的 `CameraCollisionFilter` 不参与正式缩臂

#### Scenario: 保留 Cinemachine 姿态输出
- **WHEN** 自定义相机臂碰撞约束修正相机位置
- **THEN** Cinemachine vcam 仍负责 Follow、LookAt、blend 和基础 `3rd Person Follow` 姿态
- **AND** 约束模块只修正最终相机位置

### Requirement: 可插拔模块边界
系统 MUST 将相机臂碰撞约束实现为独立可插拔模块，并 MUST 通过 vcam 组件挂载、序列化配置或明确接口获取输入，避免把缩臂逻辑写入角色移动、输入、动画或主相机目标输出脚本。

#### Scenario: 移除模块后恢复基础相机
- **WHEN** 从 vcam 上移除或禁用相机臂碰撞约束模块
- **THEN** 项目 MUST 能回到基础 Cinemachine `3rd Person Follow` 行为
- **AND** 角色移动、输入、动画和 `ThirdPersonCameraController` 不需要同步改代码才能编译

#### Scenario: 输入依赖通过边界传入
- **WHEN** 相机臂碰撞约束需要锚点、pitch、碰撞层或距离参数
- **THEN** 这些输入 MUST 来自模块自身配置、vcam 状态或明确的相机接口
- **AND** 模块 MUST NOT 直接修改角色控制器、输入控制器或动画状态

### Requirement: 锚点到相机的球形检测
系统 MUST 从相机锚点到期望相机位置执行带半径的碰撞检测，并 MUST 根据命中结果缩短当前相机距离。

#### Scenario: 墙体命中缩臂
- **WHEN** 锚点到期望相机位置之间存在相机碰撞层墙体
- **THEN** 相机最终位置 MUST 位于墙体命中点外侧
- **AND** 当前相机距离 MUST 小于默认相机距离

#### Scenario: 无碰撞恢复
- **WHEN** 锚点到期望相机位置之间没有相机碰撞层物体
- **THEN** 相机当前距离 MUST 向 pitch 允许距离恢复

### Requirement: Pitch 驱动最大距离
系统 MUST 支持根据当前 pitch 限制相机当前最大允许距离，使向下看时可以提前缩短相机臂。

#### Scenario: 向下看缩短距离
- **WHEN** 当前 pitch 进入配置的向下视角区间
- **THEN** 相机最大允许距离 MUST 按配置曲线缩短
- **AND** 即使没有碰撞命中，相机也 MUST 不超过该 pitch 对应的最大允许距离

#### Scenario: 回到正常视角恢复距离
- **WHEN** 当前 pitch 回到正常视角区间且没有碰撞命中
- **THEN** 相机当前距离 MUST 平滑恢复到默认允许距离

### Requirement: 相机碰撞代理
系统 MUST 支持使用专用相机碰撞层或厚碰撞代理验证地面与墙体防穿透，并 MUST NOT 依赖单面 Plane 作为正式通过条件。

#### Scenario: 厚地面碰撞
- **WHEN** 相机向下压向厚地面碰撞代理
- **THEN** 相机 MUST 在接触前缩臂或停留在代理外侧
- **AND** 玩家不得看到地面另一侧作为正常结果

#### Scenario: 单面片不作为正式验收
- **WHEN** 场景只有单面 Plane 参与相机检测
- **THEN** 验证结果 MUST 标记为风险场景
- **AND** 正式验收 MUST 使用厚碰撞代理或专用相机碰撞几何

### Requirement: 可验证的调试输出
系统 MUST 暴露相机臂约束的只读调试数据，用于确认缩臂状态、当前距离和命中目标。

#### Scenario: 命中状态可见
- **WHEN** 相机臂碰撞约束命中相机碰撞层物体
- **THEN** 调试输出 MUST 显示当前处于命中状态
- **AND** 调试输出 MUST 显示当前距离和命中对象信息

#### Scenario: 恢复状态可见
- **WHEN** 相机臂碰撞约束未命中任何物体
- **THEN** 调试输出 MUST 显示当前处于恢复状态
- **AND** 调试输出 MUST 显示当前距离正在向允许距离恢复

