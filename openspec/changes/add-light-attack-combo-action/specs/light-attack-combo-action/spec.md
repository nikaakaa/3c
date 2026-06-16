## ADDED Requirements
### Requirement: 三段轻攻击动作范围
系统 MUST 提供第一版三段轻攻击动作连段。该连段 MUST 只表达动作状态、输入消费、连段窗口、动作动画和可选动作位移，不得实现伤害判定、hitbox、hurtbox、命中停顿、击退、受击状态或死亡。

#### Scenario: 默认三段轻攻击
- **WHEN** 设计者检查第一版轻攻击配置
- **THEN** 系统 MUST 能表达 `Action.Attack01`
- **AND** 系统 MUST 能表达 `Action.Attack02`
- **AND** 系统 MUST 能表达 `Action.Attack03`
- **AND** 系统 MUST NOT 要求存在第四段攻击状态

#### Scenario: 伤害判定不进入本变更
- **WHEN** 实施三段轻攻击动作连段
- **THEN** 系统 MUST NOT 新增 hitbox 或 hurtbox 判定
- **AND** MUST NOT 新增伤害、命中停顿、击退或受击状态
- **AND** 后续伤害判定 MUST 另开 OpenSpec proposal

### Requirement: 轻攻击输入和仲裁
系统 MUST 从现有 `InputRequestBuffer` 的 `Attack` 请求构建轻攻击 action request submission，并在进入 `Attack01`、`Attack02` 或 `Attack03` 前经过统一 request submission 与 Action 仲裁。输入层 MUST 只记录请求，不得提前决定未来连段结果。

#### Scenario: Locomotion 中按 Attack 进入第一段
- **GIVEN** 当前 winning submission 为 Locomotion
- **AND** 输入缓冲中存在未过期 Attack 请求
- **AND** Action 仲裁接受进入 `Action.Attack01`
- **WHEN** Character frame pipeline 处理本帧
- **THEN** 系统 MUST 生成可被统一状态机消费的 Attack 请求事实
- **AND** 统一状态机 MUST 进入 `FullBody/Action/Attack01`
- **AND** 对应 Attack 请求 MUST 被消费

#### Scenario: 仲裁拒绝不消费请求
- **GIVEN** 输入缓冲中存在未过期 Attack 请求
- **AND** Action 仲裁拒绝进入目标攻击状态
- **WHEN** Character frame pipeline 处理本帧
- **THEN** 系统 MUST NOT 生成可被统一状态机消费的 Attack 请求事实
- **AND** 统一状态机 MUST NOT 因该 rejected 请求进入攻击状态
- **AND** 输入缓冲中的 Attack 请求 MUST 保留到过期或后续合法消费

#### Scenario: 输入层不决定连段
- **WHEN** 玩家在 step N 按下 Attack
- **THEN** 输入缓冲 MUST 只记录 Attack 请求的 origin step 和 expire step
- **AND** MUST NOT 记录该请求未来必定进入 `Attack01`、`Attack02` 或 `Attack03`

### Requirement: 轻攻击连段窗口
系统 MUST 提供最小轻攻击连段窗口事实，用于判断当前攻击段是否允许消费 Attack 请求进入下一段。窗口事实 MUST 是纯数据，不得读取 Animancer runtime、Animator、AnimationClip、TransitionAsset、Unity 时间单例或场景实例。

#### Scenario: Attack01 窗口内进入 Attack02
- **GIVEN** 当前状态为 `FullBody/Action/Attack01`
- **AND** 当前轻攻击窗口事实允许进入下一段
- **AND** 输入缓冲中存在未过期 Attack 请求
- **AND** Action 仲裁接受进入 `Action.Attack02`
- **WHEN** Character frame pipeline 处理本帧
- **THEN** 统一状态机 MUST 进入 `FullBody/Action/Attack02`
- **AND** 对应 Attack 请求 MUST 被消费

#### Scenario: Attack02 窗口内进入 Attack03
- **GIVEN** 当前状态为 `FullBody/Action/Attack02`
- **AND** 当前轻攻击窗口事实允许进入下一段
- **AND** 输入缓冲中存在未过期 Attack 请求
- **AND** Action 仲裁接受进入 `Action.Attack03`
- **WHEN** Character frame pipeline 处理本帧
- **THEN** 统一状态机 MUST 进入 `FullBody/Action/Attack03`
- **AND** 对应 Attack 请求 MUST 被消费

#### Scenario: 窗口外不进入下一段
- **GIVEN** 当前状态为 `FullBody/Action/Attack01` 或 `FullBody/Action/Attack02`
- **AND** 当前轻攻击窗口事实不允许进入下一段
- **AND** 输入缓冲中存在未过期 Attack 请求
- **WHEN** Character frame pipeline 处理本帧
- **THEN** 统一状态机 MUST NOT 进入下一段攻击
- **AND** 对应 Attack 请求 MUST NOT 因连段失败被消费

#### Scenario: Attack03 不进入第四段
- **GIVEN** 当前状态为 `FullBody/Action/Attack03`
- **AND** 输入缓冲中存在未过期 Attack 请求
- **WHEN** Character frame pipeline 处理本帧
- **THEN** 系统 MUST NOT 构建进入第四段攻击的请求
- **AND** 统一状态机 MUST NOT 进入未定义攻击状态

### Requirement: 轻攻击生命周期
系统 MUST 让每段轻攻击拥有明确的进入、计时、可接段窗口、结束和返回 Locomotion 规则。攻击生命周期 MUST 由统一状态机和 Character frame pipeline 表达，不得由独立攻击 MonoBehaviour 或动画回调决定。

#### Scenario: Attack01 无下一段时返回 Locomotion
- **GIVEN** 当前状态为 `FullBody/Action/Attack01`
- **AND** 当前段达到配置结束条件
- **AND** 没有合法进入 `Action.Attack02` 的 accepted 请求事实
- **WHEN** 统一状态机推进
- **THEN** 系统 MUST 退出攻击状态
- **AND** 当前 action state MUST 回到 `Action.None`
- **AND** winning submission MUST 回到 Locomotion

#### Scenario: Attack02 无下一段时返回 Locomotion
- **GIVEN** 当前状态为 `FullBody/Action/Attack02`
- **AND** 当前段达到配置结束条件
- **AND** 没有合法进入 `Action.Attack03` 的 accepted 请求事实
- **WHEN** 统一状态机推进
- **THEN** 系统 MUST 退出攻击状态
- **AND** 当前 action state MUST 回到 `Action.None`
- **AND** winning submission MUST 回到 Locomotion

#### Scenario: Attack03 结束后返回 Locomotion
- **GIVEN** 当前状态为 `FullBody/Action/Attack03`
- **AND** 当前段达到配置结束条件
- **WHEN** 统一状态机推进
- **THEN** 系统 MUST 退出攻击状态
- **AND** winning submission MUST 回到 Locomotion
- **AND** 当前 action state MUST 回到 `Action.None`

### Requirement: 轻攻击 FullBody 输出权威
系统 MUST 让轻攻击 active 期间成为当前 FullBody Action winning submission。攻击动作 MAY 输出动作动画命令、可选动作位移和可选转向，但 MUST 通过 Character output applier 提交到统一动画 Presenter 和统一 motion executor 执行。

#### Scenario: 攻击期间压制 Locomotion 输出
- **GIVEN** 当前状态为 `FullBody/Action/Attack01`、`FullBody/Action/Attack02` 或 `FullBody/Action/Attack03`
- **WHEN** Character output applier 处理本帧输出
- **THEN** 当前 winning submission MUST 为 FullBody Action
- **AND** Character output applier MUST NOT 应用 Locomotion 平面位移命令
- **AND** Character output applier MUST NOT 应用 Locomotion base layer 动画上下文

#### Scenario: 攻击动作位移走统一出口
- **GIVEN** 当前攻击段配置了动作位移
- **WHEN** 该段 active tick 产生位移
- **THEN** 系统 MUST 输出纯数据动作位移命令
- **AND** 该命令 MUST 通过 Character output applier 交给统一 motion executor 或等价运动出口
- **AND** 攻击逻辑 MUST NOT 直接调用 `CharacterController.Move`
- **AND** 攻击逻辑 MUST NOT 直接写入 Transform

#### Scenario: 攻击动画只由 Presenter 播放
- **WHEN** 当前攻击段需要播放动作动画
- **THEN** 攻击逻辑 MUST 输出稳定动作动画 key
- **AND** 动画 Presenter MUST 只消费 Character output applier 提交的最终动画请求并播放表现
- **AND** 攻击逻辑 MUST NOT 直接调用 Animancer 或 Animator 播放 API

#### Scenario: Look 不被攻击锁死
- **GIVEN** 当前轻攻击 active
- **WHEN** 玩家输入 Look
- **THEN** 项目侧相机入口 MUST 继续接收 Look 输入或等价相机意图
- **AND** 轻攻击逻辑 MUST NOT 直接读取或控制 Cinemachine 具体实例

### Requirement: 轻攻击配置闭环
系统 MUST 提供正式轻攻击配置入口，使设计者能追踪三段轻攻击的状态 ID、动画 key、duration、priority、resistance、连段窗口和可选位移/转向参数。缺失或非法配置 MUST 被校验报告，不得静默使用 fallback 手感配置。

#### Scenario: 三段配置完整
- **WHEN** 设计者检查轻攻击配置
- **THEN** 配置 MUST 包含 `Action.Attack01` 的 stage entry
- **AND** 配置 MUST 包含 `Action.Attack02` 的 stage entry
- **AND** 配置 MUST 包含 `Action.Attack03` 的 stage entry
- **AND** 每个 stage entry MUST 包含 duration、priority、resistance 和 animation key

#### Scenario: 连段窗口配置完整
- **WHEN** 设计者检查轻攻击配置
- **THEN** `Action.Attack01` MUST 能配置进入 `Action.Attack02` 的 combo window
- **AND** `Action.Attack02` MUST 能配置进入 `Action.Attack03` 的 combo window
- **AND** `Action.Attack03` MUST 显式表达没有下一段

#### Scenario: 缺失配置报错
- **GIVEN** 轻攻击配置缺失 stage、duration、animation key 或 combo window
- **WHEN** 运行配置校验
- **THEN** 校验结果 MUST 报告 error
- **AND** 运行时 MUST NOT 静默使用代码默认手感参数替代正式配置

### Requirement: 轻攻击边界
系统 MUST 保持轻攻击动作与伤害、表现事件、网络协议和 Root Motion 权威分离。任何需要突破当前边界的实现都 MUST 停止并补充 OpenSpec proposal。

#### Scenario: 不接伤害系统
- **WHEN** 实施轻攻击动作连段
- **THEN** 系统 MUST NOT 新增伤害计算
- **AND** MUST NOT 新增受击状态
- **AND** MUST NOT 新增命中确认逻辑

#### Scenario: 不接表现事件轨道
- **WHEN** 实施轻攻击动作连段
- **THEN** 系统 MUST NOT 新增 VFX、SFX、Camera event 或 IK 轨道
- **AND** MUST NOT 通过动画事件直接切换攻击状态

#### Scenario: Root Motion 权威需要另审
- **WHEN** 实现发现必须让 Root Motion 驱动攻击位移
- **THEN** 实现 MUST 停止
- **AND** MUST 新建或更新 OpenSpec proposal 说明运动权威边界变化

#### Scenario: 不修改网络协议
- **WHEN** 实施轻攻击动作连段
- **THEN** 系统 MUST NOT 修改 Fantasy 协议文件
- **AND** MUST NOT 新增真实网络发送接收流程

### Requirement: 轻攻击可测试和可验证
系统 MUST 为三段轻攻击动作连段提供自动测试、静态边界验证和 Play Mode 验证方式。验证 MUST 证明攻击接入现有 Character frame pipeline 且没有引入分裂路径。

#### Scenario: 自动测试覆盖连段主路径
- **WHEN** 运行轻攻击连段 EditMode 测试
- **THEN** 测试 MUST 覆盖从 Locomotion 进入 Attack01
- **AND** MUST 覆盖 Attack01 窗口内进入 Attack02
- **AND** MUST 覆盖 Attack02 窗口内进入 Attack03
- **AND** MUST 覆盖 Attack03 完成后回 Locomotion

#### Scenario: 自动测试覆盖拒绝和边界
- **WHEN** 运行轻攻击连段 EditMode 测试
- **THEN** 测试 MUST 覆盖窗口外不接下一段
- **AND** MUST 覆盖 rejected 请求不消费
- **AND** MUST 覆盖攻击期间 Locomotion 不叠加平面位移或 base layer 动画
- **AND** MUST 覆盖缺失正式配置时报错

#### Scenario: 静态边界验证
- **WHEN** 检查轻攻击新增源码
- **THEN** 静态搜索 MUST 能确认新增攻击逻辑不引用 `BBBNexus`
- **AND** MUST 能确认攻击逻辑不直接调用 `CharacterController.Move`
- **AND** MUST 能确认攻击逻辑不直接调用 Animancer 或 Animator 播放 API

#### Scenario: Play Mode 验证方式
- **WHEN** 用户在 Play Mode 中按一次 Attack
- **THEN** 角色 MUST 播放第一段轻攻击动作
- **WHEN** 用户在连段窗口内继续按 Attack
- **THEN** 角色 MUST 依次播放第二段和第三段轻攻击动作
- **AND** 第三段结束后 MUST 回到 Locomotion
- **AND** 攻击过程中 WASD MUST 不叠加基础移动平面位移
