## ADDED Requirements
### Requirement: 正式 Prefab Runtime Adapter 收敛
系统 MUST 让 Corin 正式 prefab 只挂载一个 gameplay runtime assembly adapter。该 adapter MUST 负责创建、持有或绑定 `CharacterRuntimeCore`，并将输入、运动、动画、facing、tick 和配置等 Unity-facing adapters 注入 core dependencies。`PlayerLocomotionController`、`FullBodyActionRuntime` 或等价迁移期 facade MUST NOT 作为正式 prefab 组件表达 Locomotion 或 Action owner。

#### Scenario: Prefab 只有一个 runtime assembly adapter
- **WHEN** 自动校验 `Assets/Prefabs/Character/可琳.prefab` 和 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **THEN** 每个 prefab MUST 只有一个正式 gameplay runtime assembly adapter
- **AND** 该 adapter MUST 绑定同一个 `CharacterRuntimeCore`
- **AND** prefab MUST NOT 同时挂载 `PlayerLocomotionController` 和 `FullBodyActionRuntime` 作为正式 gameplay runtime facade

#### Scenario: Unity-facing adapters 保留为窄 seam
- **WHEN** 自动校验 Corin 正式 prefab 的 MonoBehaviour 清单
- **THEN** prefab MAY 保留输入、输入缓冲、motion executor、Animancer presenter、facing/camera basis、presentation interpolation 和 tick registration adapter
- **AND** 这些 adapters MUST 只满足各自 Unity seam 的 Interface
- **AND** MUST NOT 自行持有正式 runtime state、runner、lifecycle 或 frame pipeline host

#### Scenario: 不通过减少 Mono 数量破坏 seam
- **WHEN** 实施 prefab 收敛
- **THEN** 系统 MUST NOT 把 Animancer runtime、CharacterController、Transform、InputAction 或 scene object 放入 pure C# core/module
- **AND** MUST NOT 为了合并 MonoBehaviour 而让 runtime assembly adapter 直接执行运动、播放动画或消费输入

### Requirement: 迁移期 Facade 从正式装配退场
`PlayerLocomotionController` 和 `FullBodyActionRuntime` MUST 从正式代码面删除，旧测试、旧 fixture、旧 assembler 和旧 debug rig MUST NOT 继续依赖这些迁移期 facade。Locomotion 与 Action runtime state MUST 只由 `CharacterRuntimeCore` 组合的 module 持有，并通过窄 Unity-facing adapters 装配。

#### Scenario: Locomotion facade 不在正式 prefab 上
- **WHEN** 自动校验 Corin 正式 prefab 和正式 scene override
- **THEN** `PlayerLocomotionController` MUST NOT 作为正式 gameplay 组件存在
- **AND** Locomotion runtime state MUST 仍由 `CharacterRuntimeCore` 组合的 `LocomotionRuntimeModule` 或批准等价 Module 持有
- **AND** 移动输入、facing 和 motion executor MUST 通过窄 Unity-facing adapters 注入

#### Scenario: Action facade 不在正式 prefab 上
- **WHEN** 自动校验 Corin 正式 prefab 和正式 scene override
- **THEN** `FullBodyActionRuntime` MUST NOT 作为正式 gameplay 组件存在
- **AND** Action runtime state MUST 仍由 `CharacterRuntimeCore` 组合的 `FullBodyActionRuntimeModule` 或批准等价 Module 持有
- **AND** Action request、lifecycle、claim、motion 和 animation 输出 MUST 继续通过角色帧管线推进

#### Scenario: 旧兼容代码不保留
- **WHEN** 自动静态测试扫描 production runtime、测试 fixture 和 prefab 装配
- **THEN** `PlayerLocomotionController` 和 `FullBodyActionRuntime` 类型 MUST 不存在
- **AND** 旧测试 MUST 改为直接使用 `CharacterFrameRuntimeController`、`CharacterRuntimeCore`、`LocomotionRuntimeModule` 或 `FullBodyActionRuntimeModule`
- **AND** 系统 MUST NOT 保留注册旧 tick、创建第二 core、创建第二 runner、创建第二 motion executor、创建第二 animation presenter 或第二 pipeline host 的兼容入口

### Requirement: Prefab 装配 Allowlist 验证
系统 MUST 提供自动测试验证 Corin 正式 prefab 和正式 scene runtime 装配。测试 MUST 以 allowlist 方式区分 runtime assembly adapter、Unity-facing adapter、迁移期 facade 和 debug tooling，并在出现第二出口、debug tooling 或旧 facade 时失败。

#### Scenario: Prefab 脚本清单可验证
- **WHEN** 运行 EditMode prefab binding 测试
- **THEN** 测试 MUST 解析两个 Corin prefab 的 MonoBehaviour 脚本清单
- **AND** MUST 确认脚本清单只包含批准的 runtime assembly adapter 和 Unity-facing adapters
- **AND** MUST 对未分类 runtime 脚本报错

#### Scenario: 唯一副作用出口
- **WHEN** 运行 EditMode prefab binding 测试
- **THEN** 每个正式 prefab MUST 只有一个 motion executor adapter
- **AND** MUST 只有一个 animation presenter adapter
- **AND** MUST 没有第二 pipeline runner、第二 state runner 或第二 runtime host owner

#### Scenario: Debug tooling 不挂正式角色
- **WHEN** 运行 EditMode prefab/scene boundary 测试
- **THEN** 正式角色 prefab 和正式 scene instance MUST NOT 挂载 rollback debug runner、history recorder、hidden replay adapter 或 synctest runner
- **AND** rollback debug rig MUST 继续通过显式 target 引用连接正式角色 runtime
