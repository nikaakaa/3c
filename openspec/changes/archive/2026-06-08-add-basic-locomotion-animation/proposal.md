# Change: 接入基础移动动画外观层

## Why
当前最小第三人称 WASD 已经完成输入、相机相对方向、移动阶段和 `CharacterMotionDriver` 位移闭环，但角色移动时还没有按 `Idle / MoveStart / MoveLoop / MoveStop` 播放对应动画，演示观感和后续动作系统承接都不完整。

用户已确认拥有所需角色动画 Clip，因此本变更只新增一层项目自有的基础移动动画外观层，通过配置把动画资源接入当前 WASD 阶段，不直接使用 BBB 运行时主线。

## What Changes
- 新增 `basic-locomotion-animation` 能力，用于描述基础移动四阶段动画接入。
- 新增移动动画上下文，用纯数据表达当前移动阶段、输入强度、世界方向和当前速度。
- 新增基础移动动画配置模块，用 ScriptableObject 配置 `Idle / MoveStart / MoveLoop / MoveStop` 对应动画和淡入参数。
- 新增 Animancer 基础移动外观层，消费移动动画上下文并播放配置动画。
- 将当前 `BasicWASDMovementController` 作为组装入口，向动画外观层提交上下文，但不在 WASD 入口中散落 Animancer 细节。
- 保持位移权威仍然只进入 `CharacterMotionDriver`；动画第一版只负责表现，不驱动角色位移。
- 本轮快速开发不新增 Unity 测试文件；通过 OpenSpec 校验、静态搜索和 Unity 手动验证完成验证说明。

## Impact
- Affected specs: `basic-locomotion-animation`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/BasicWASDMovementController.cs`
  - `3cDemo/Client/3C_Client/Assets/Config/3C`
  - 当前用于 WASD 演示的角色 Prefab 或场景绑定
- Reference use:
  - 可参考 BBB 的动画外观层思路。
  - 不直接挂接、继承、调用或依赖 BBB 的 `BBBCharacterController`、`MotionDriver`、状态机、Prefab、SO 配置或命名空间。
- Non-goals:
  - 不实现 Root Motion、Root Motion 烘焙、Motion Warping 或动作位移。
  - 不实现跳跃、闪避、攻击、连招、受击、死亡或上半身动作。
  - 不迁移完整 BBB 角色聚合点。
  - 不新增第二套角色移动入口。
  - 不删除现有 log。
