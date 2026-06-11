## ADDED Requirements

### Requirement: 基础移动 OnAnimationEnd 退出策略
系统 MUST 允许基础移动 phase config 使用 `OnAnimationEnd` 退出策略，使 `MoveStop / RunEnd` 能通过动画播放结束事实退出，而不是要求设计者手动维护与动画长度一致的 exit duration。

#### Scenario: MoveStop 使用动画结束退出
- **GIVEN** `MoveStop` phase config 的 alias key 为 `RunEnd`
- **AND** `MoveStop` phase config 的退出策略为 `OnAnimationEnd`
- **WHEN** `RunEnd` 播放尚未结束
- **THEN** 动画事实层 MUST 产出 `CanExit=false`
- **WHEN** `RunEnd` 播放结束
- **THEN** 动画事实层 MUST 产出 `CanExit=true`

#### Scenario: OnAnimationEnd 不重复配置播放参数
- **WHEN** 设计者将 `MoveStop` 配置为 `OnAnimationEnd`
- **THEN** 项目侧 Run 基础移动配置 MUST NOT 要求填写 clip length
- **AND** MUST NOT 要求填写 Animancer fade duration
- **AND** MUST NOT 要求填写 Animancer speed
- **AND** MUST NOT 要求填写 normalized start time

#### Scenario: AfterDuration 兼容保留
- **WHEN** `MoveStart` 或其它 phase config 继续使用 `AfterDuration`
- **THEN** 系统 MUST 继续使用 phaseTime 和 exit duration 产出 `CanExit`
- **AND** 不需要对应 phase 存在有效 Animancer 播放进度

### Requirement: Animancer 基础移动播放进度边界
系统 MUST 允许 Animancer 基础移动外观层暴露当前基础移动播放进度快照，但外观层 MUST 只负责播放和只读进度，不负责判断 `CanExit`、不负责打断仲裁、不负责状态机切换。

#### Scenario: Presenter 暴露只读进度
- **WHEN** Animancer 外观层正在播放基础移动 alias
- **THEN** 外观层 MUST 能提供当前 phase、alias key、normalized time 和是否已结束的只读快照
- **AND** 该快照 MUST 不携带 Animancer runtime 对象给逻辑层

#### Scenario: Presenter 不决定退出事实
- **WHEN** Animancer 外观层提供播放进度快照
- **THEN** 外观层 MUST NOT 计算 `CanExit`
- **AND** MUST NOT 读取 Locomotion 状态图条件
- **AND** MUST NOT 读取动作打断仲裁器

#### Scenario: Presenter 不通过 OnEnd 切状态
- **WHEN** 基础移动动画自然播放到结束
- **THEN** Animancer 外观层 MUST NOT 通过 `OnEnd` 或等价回调直接切换 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** MUST NOT 调用 Locomotion 状态机切换 API

### Requirement: RunEnd 播完退出手动验证
系统 MUST 提供清晰的验证路径，证明 `RunEnd` 在无输入时能播完再回 `Idle`，并且在中途重新输入时能立即回到移动阶段。

#### Scenario: 无输入播完回 Idle
- **GIVEN** 当前基础移动配置将 `MoveStop` 设置为 `OnAnimationEnd`
- **AND** 当前角色已经进入 `MoveLoop`
- **WHEN** 玩家松开移动输入并保持无输入
- **THEN** 系统 MUST 播放 `RunEnd`
- **AND** MUST 在 `RunEnd` 播放结束后切换到 `Idle`

#### Scenario: 中途输入立即打断 RunEnd
- **GIVEN** 当前基础移动配置将 `MoveStop` 设置为 `OnAnimationEnd`
- **AND** 当前角色正在 `MoveStop` 播放 `RunEnd`
- **WHEN** 玩家在 `RunEnd` 播完前重新输入移动
- **THEN** 系统 MUST 立即切换到 `MoveStart` 或等价起步阶段
- **AND** MUST NOT 等待 `RunEnd` 播放结束
