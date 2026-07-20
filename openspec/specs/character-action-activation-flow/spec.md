# character-action-activation-flow Specification

## Purpose
定义 Graph 如何建立 ActionInstance、显式传递 Action Context，并以唯一准入与 lifecycle 规则驱动 Timeline、窗口、Motion、Cue 和 GameplayResult。

## Requirements

### Requirement: Graph 必须通过 ActivateActionInstance operation 产生 ActionInstance

Graph MUST 通过 `ActivateActionInstanceNode` 发射正式 operation。激活成功后 MUST 创建 ActionInstance，并写入后续输出显式引用的 Action Context。Tree、SubTree、StateNode、TimelineNode 或节点 membership MUST NOT 成为动作身份。

#### Scenario: 从输入启动动作

- **WHEN** Graph 接受 LightAttack request 并激活 Attack profile
- **THEN** operation MUST 创建 ActionInstance 和 Action Context
- **AND** 普通 locomotion graph MUST NOT 被迫创建 ActionInstance

### Requirement: ActivateActionInstance 必须携带动作事务来源

operation MUST 携带 ActionProfile identity、source request、InputSequence、source logic tick、target snapshot 和 source operation identity。服务端 tick MAY 出现在模型 decision 中，但 MUST NOT 替代本地来源 tick。

#### Scenario: 调试动作来源

- **WHEN** 动作来自输入、AI 或资源条件
- **THEN** Debug MUST 能定位 source operation、logic tick 和可选 input request

### Requirement: Timeline 必须只是可选动作输出来源

Timeline MAY 接收显式 Action Context，使 TreeClip、Motion、Cue 与 GameplayResult 输出关联 ActionInstance。Timeline MUST NOT 创建 ActionInstance，也 MUST NOT 从 ambient active action 或资产 membership 猜归属。Network Model 只消费 Finalize 后的显式 GameplayFact。

#### Scenario: Timeline 攻击

- **WHEN** Attack ActionInstance 播放 inline Timeline
- **THEN** playback MUST 携带 Action Context
- **AND** projected window、motion、cue 与 result MAY 写同一 ActionInstanceId

### Requirement: 非 Timeline 动作必须能使用同一 ActionInstance

没有 Timeline 的动作 MAY 在持有 Action Context 时写 owner-local scope variable，并通过同一 projection stage 生成输出。系统 MUST NOT 恢复 `SubmitActionWindowSampleNode` 或 ambient action fallback。

#### Scenario: 持续格挡

- **WHEN** Guard Action 没有 Timeline
- **THEN** Graph MAY 写 projected Guard window
- **AND** 缺少 Action Context 时 projection MUST 失败

### Requirement: Action operation runtime 必须保持事务层职责

Action runtime MUST 只负责 catalog/profile 查询、准入、ActionInstance 创建和 lifecycle 状态流转。Graph tick、Timeline、Motion、Cue、命中与世界求解 MUST 由各自正式模块处理。

#### Scenario: 动作校正

- **WHEN** typed correction ingress 到达
- **THEN** Action operation MUST 只更新实例状态与原因
- **AND** world restore 与 visual recovery MUST 留在各自模块

### Requirement: 系统不得恢复结构身份式 ActionModule

系统 MUST NOT 恢复 ActionModule、ActionSubTree、ActionStateNode、ActionTree、AbilityTree 或 membership table。作者元素只表达 activation request 或 action output。

#### Scenario: 同一状态启动不同动作

- **WHEN** GuardState 可启动 Guard 或 ParryCounter
- **THEN** 分支 MUST 提交不同 activation request
- **AND** StateNode MUST NOT 静态绑定唯一 ActionProfile

### Requirement: 动作生命周期必须通过 typed transition 表达

Complete、Cancel、Interrupt、Reject、Correct 与 Abort MUST 通过 `ActionLifecycleTransition` 或等价 typed operation/fact 表达。外部 decision MUST 先转为 SimulationIngress；节点某 Tick 未执行 MUST NOT 隐式关闭 ActionInstance。

#### Scenario: 动作被取消

- **WHEN** StateMachine 决定替换 active Action
- **THEN** source MUST 显式提交 terminal transition
- **AND** target MUST 创建独立 ActionInstance

### Requirement: Action Context 必须是动作输出的显式输入

Timeline、Window、Motion、Cue、GameplayResult 和 lifecycle 节点只有显式接收有效 Action Context 时，才 MAY 产出带 ActionInstanceId 的输出。terminal 后旧 Context MUST 失效。

#### Scenario: 读取已结束 Context

- **WHEN** 对应 ActionInstance 已进入 terminal
- **THEN** 后续读取 MUST 失败
- **AND** MUST NOT 继续输出旧 ActionInstanceId

### Requirement: Action 必须使用统一 Gameplay Effect 状态

Action admission 和 lifecycle MUST 读取 CharacterSimulationState 中唯一 Gameplay Effect Tag/Attribute/Effect 状态。Action runtime MUST NOT 建私有 tag 集合、字符串 SetTag、effect tick 或 attribute store。

#### Scenario: Stun 阻止攻击

- **WHEN**统一 Tag Container 包含 Stunned
- **THEN** Attack admission MUST 拒绝
- **AND** Graph 与 admission MUST 读取同一状态

### Requirement: 动作准入查询与激活提交必须共享唯一规则

numeric-neutral admission evaluator MUST 同时服务 `CanActivateAction` 与 `ActivateActionInstance`，读取同一 catalog/profile、Gameplay Effect state、active ActionInstance、block query 和 cancel query。Float32 与 Fixed 只提供窄状态端口。纯查询 MUST NOT 消费 request、创建实例、提交 lifecycle 或跨 Tick缓存。

ActionInstance 成功创建时，profile granted tags MUST 以 `action:<ActionInstanceId>` source 写入唯一 Tag Container；Complete、Cancel、Interrupt、Abort 或 teardown MUST 精确撤销该 source。

#### Scenario: Transition 预览 Dodge

- **WHEN** active Attack 满足 Dodge cancel query 且没有 block 条件
- **THEN** `CanActivateAction` 与最终 activation MUST 得到相同准入结果
- **AND** preview MUST 不改变输入、实例或 Tag 状态

#### Scenario: Numeric Target 对等

- **WHEN** Float32 与 Fixed 对相同 Program/state 查询准入
- **THEN** MUST 得到相同 allowed/rejected 结果与原因

### Requirement: Target activation 不得隐式结束 Source Action

`ActivateActionInstance` MUST 只在没有 active source Action 时创建 target。replacement MUST 先经过 source stop barrier 和 OnExit，由显式 lifecycle operation 关闭 source，再激活 target。系统 MUST NOT 自动 Cancel、覆盖 Context 或吞掉重复 terminal。

#### Scenario: Source 尚未关闭

- **WHEN** target activation 时仍有 active source
- **THEN** operation MUST 返回 `SourceActionStillActive` 或等价 typed reason
- **AND** source、Context 和 Tag source MUST 保持不变

#### Scenario: Recovery 后启动 Dodge

- **WHEN** Attack 到 Dodge replacement 提交
- **THEN** Attack OnExit MUST 先 `Cancel(RecoveryCancel)`
- **AND** Dodge target MUST 随后创建独立 ActionInstance

### Requirement: ActionProfile 必须类型化声明目标快照要求

ActionProfile MUST使用`ActionTargetRequirement`明确声明`None`、`OptionalSnapshot`或`SnapshotRequired`，MUST不使用自由字符串TargetPolicy。Action catalog、Semantic IR和两个Numeric Target MUST保存同一typed值。未知值或缺失值 MUST在artifact发布前失败。配置MotionWarp的动作 MUST声明`OptionalSnapshot`或`SnapshotRequired`；声明`None`时 MUST在发布前失败。

`OptionalSnapshot` MUST表达正式业务策略：有候选目标时ActionInstance固定保存目标快照并允许MotionWarp；无候选目标时动作仍可激活，MotionWarp MUST保留源MotionCurve并输出typed原因。该语义 MUST不通过捕获异常、静默禁用或运行时fallback实现。

#### Scenario: 普通无目标闪避

- **WHEN** Dodge ActionProfile声明`None`
- **THEN** admission MAY在没有target snapshot时成功
- **AND** 该动作 MUST不包含需要目标的MotionWarp

#### Scenario: 目标攻击缺少快照

- **WHEN** ActionProfile声明`SnapshotRequired`
- **AND** candidate target snapshot为None
- **THEN** admission MUST返回`TargetSnapshotRequired`或等价typed原因
- **AND** MUST不创建ActionInstance或启动Timeline

#### Scenario: 可选目标攻击没有目标

- **WHEN** Attack ActionProfile声明`OptionalSnapshot`
- **AND** candidate target snapshot为None
- **THEN** admission MUST允许创建无目标快照的ActionInstance
- **AND** 对应MotionWarp MUST保留源MotionCurve

#### Scenario: 可选目标攻击获得目标

- **WHEN** Attack ActionProfile声明`OptionalSnapshot`
- **AND** candidate target snapshot有效
- **THEN** admission MUST允许动作激活
- **AND** ActionInstance MUST固定保存该快照供MotionWarp使用

### Requirement: 动作准入查询与提交必须读取同一目标候选

`CanActivateAction`与`ActivateActionInstance` MUST把同一显式Blackboard ActionTargetSnapshot或显式None传入唯一portable admission evaluator。纯查询与最终提交 MUST对`None`、`OptionalSnapshot`和`SnapshotRequired`得到相同结果；系统 MUST不允许查询忽略目标而提交阶段再失败，也 MUST不在激活后从Scene、Transform、Presentation或registry补查目标。

#### Scenario: Transition 查询通过后激活动作

- **WHEN** transition条件使用CanActivateAction检查target-required动作
- **AND** target snapshot在同一准入输入中有效
- **THEN** 最终ActivateActionInstance MUST使用同一候选快照
- **AND** 创建的ActionInstance MUST固定保存该快照

#### Scenario: Transition 查询通过后激活可选目标动作

- **WHEN** transition条件使用CanActivateAction检查`OptionalSnapshot`动作
- **THEN** 最终ActivateActionInstance MUST读取同一Blackboard declaration
- **AND** 查询与提交 MUST对目标存在或缺失得到相同语义

### Requirement: MotionWarp 必须消费 ActionInstance 的固定目标快照

MotionWarp MUST只读取显式Action Context对应ActionInstance在激活时保存的target snapshot。运行期间target实体移动、消失或Presentation更新 MUST不改变该ActionInstance的Warp目标。MotionWarp MUST不按TargetId查询Transform、scene registry、Network Model或其它ambient状态。

#### Scenario: 目标在动作期间移动

- **WHEN** ActionInstance已经捕获target snapshot
- **AND** 目标实体随后移动
- **THEN** 当前动作的MotionWarp MUST继续使用已捕获pose
- **AND** 新目标位置只 MAY由后续正式Action activation重新捕获
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               
