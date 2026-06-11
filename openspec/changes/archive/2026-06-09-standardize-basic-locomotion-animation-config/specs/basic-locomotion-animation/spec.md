## ADDED Requirements
### Requirement: 角色基础移动动画表归属
系统 MUST 将当前角色的基础移动动画表归属到角色 prefab 上的基础移动动画外观层，而不是要求每个场景实例重复维护 `Idle / MoveStart / MoveLoop / MoveStop` 动画 Clip 配置。

#### Scenario: 角色 prefab 持有动画表
- **WHEN** 设计者配置当前演示角色的基础移动动画
- **THEN** 角色 prefab 上的 `BasicLocomotionAnimancerPresenter` MUST 引用对应的 `BasicLocomotionAnimationConfigSO`
- **AND** `BasicLocomotionAnimationConfigSO` MUST 继续提供 `Idle / MoveStart / MoveLoop / MoveStop` 四阶段动画配置

#### Scenario: 场景不重复维护动画 Clip
- **WHEN** 同一角色 prefab 被放入不同演示场景
- **THEN** 场景实例 MUST NOT 需要分别维护一套基础移动动画 Clip 引用才能播放移动动画
- **AND** 场景仍 MAY 维护输入、相机和移动参数等场景装配引用

### Requirement: WASD 自动发现动画外观层
系统 MUST 允许当前 WASD 运行时组装入口在未显式绑定 `locomotionPresenter` 时，从当前角色对象层级内发现现有 `BasicLocomotionAnimancerPresenter` 并提交移动动画上下文。

#### Scenario: 同对象发现 Presenter
- **WHEN** `BasicWASDMovementController` 的 `locomotionPresenter` 未绑定
- **AND** 同一 GameObject 上存在 `BasicLocomotionAnimancerPresenter`
- **THEN** WASD 入口 MUST 使用该 Presenter 接收 `MovementAnimationContext`

#### Scenario: 子对象发现 Presenter
- **WHEN** `BasicWASDMovementController` 的 `locomotionPresenter` 未绑定
- **AND** 同一 GameObject 上不存在 Presenter
- **AND** 当前角色子层级内存在 `BasicLocomotionAnimancerPresenter`
- **THEN** WASD 入口 MUST 使用该子层级 Presenter 接收 `MovementAnimationContext`

#### Scenario: 禁止跨角色全局查找
- **WHEN** `BasicWASDMovementController` 自动发现动画外观层
- **THEN** 自动发现 MUST 限制在当前角色对象层级内
- **AND** MUST NOT 使用全场景查找连接其他角色的 Presenter

#### Scenario: 不隐式创建配置路径
- **WHEN** 当前角色对象层级内没有 `BasicLocomotionAnimancerPresenter`
- **THEN** WASD 位移 MUST 仍可按现有逻辑运行
- **AND** WASD 入口 MUST NOT 自动创建 Presenter、AnimancerComponent 或动画配置资产
- **AND** WASD 入口 MUST NOT 通过 `Resources.Load` 或全局单例隐式加载动画表

### Requirement: 基础移动动画配置不改变位移权威
系统 MUST 在统一基础移动动画配置归属后继续保持基础 WASD 位移权威在 `CharacterMotionDriver`，动画外观层只负责表现。

#### Scenario: 统一配置后仍走运动驱动
- **WHEN** 角色通过 prefab 上的基础移动动画表播放移动动画
- **THEN** 角色位移 MUST 仍由 `CharacterMotionDriver` 执行
- **AND** `BasicLocomotionAnimancerPresenter` MUST NOT 调用 `CharacterController.Move`
- **AND** `BasicLocomotionAnimancerPresenter` MUST NOT 写入角色 `transform.position`

#### Scenario: Root Motion 仍需单独审批
- **WHEN** 实现统一配置归属时发现必须让 Root Motion 驱动基础移动
- **THEN** 实现 MUST 停止
- **AND** MUST 另建或更新 OpenSpec proposal 说明位移权威边界变化
