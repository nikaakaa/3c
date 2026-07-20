# character-action-authoring-closure Specification Delta

## MODIFIED Requirements

### Requirement: Full-body Action 必须通过 pipeline blackboard 公布 locomotion ownership

Corin outer Attack 与 Dodge MUST作为唯一 full-body action group，通过 root-owned pipeline blackboard `HasActionLocomotionOwnership` 公布 locomotion ownership，并通过 `ResumeLocomotionThroughRunEnd` 公布无输入释放策略。Outer group OnEnter MUST在 nested leaf 激活前写入 ownership；所有 outer source-exit 的 OnExit MUST写入 ownership=false。Attack1、Attack2、Attack3、Attack4、Attack5、RushAttack、DodgeForward 与 DodgeBack leaf MUST继续唯一拥有 ActionProfile、Action Context、Timeline 和 terminal lifecycle。Locomotion MUST只读取 ownership 与恢复策略，不得复制 Action request、ActionProfile、Timeline、motion curve、window 或 leaf direction/segment。

#### Scenario: Attack 激活后让渡 locomotion ownership

- **WHEN** outer Attack 成功进入并准备运行 Attack Combo StateMachine
- **THEN** Attack OnEnter MUST写入 `HasActionLocomotionOwnership=true`
- **AND** MUST写入 `ResumeLocomotionThroughRunEnd=false`
- **AND** Locomotion StateMachine MUST能读取这些值进入 ActionOverride

#### Scenario: Dodge 激活后让渡 locomotion ownership

- **WHEN** outer Dodge 成功进入并准备运行 Dodge Direction StateMachine
- **THEN** Dodge OnEnter MUST写入 `HasActionLocomotionOwnership=true`
- **AND** MUST写入 `ResumeLocomotionThroughRunEnd=true`
- **AND** Locomotion StateMachine MUST能读取这些值进入 ActionOverride

#### Scenario: Full-body Action 正常完成或被打断

- **WHEN** outer Attack 或 Dodge 正常完成、被 State transition 抢占或被上层 tree graceful stop
- **THEN** outer source OnExit MUST写入 `HasActionLocomotionOwnership=false`
- **AND** leaf Action lifecycle MUST根据自身离开原因提交唯一 Complete、Cancel、Interrupt 或 Abort
- **AND** Locomotion MUST能按当前输入和最近一次恢复策略收回 ownership

#### Scenario: 单一动作真相

- **WHEN** Locomotion 处理 full-body Action 活跃期间的 ownership
- **THEN** Locomotion MUST NOT创建第二个 Attack/Dodge action state或引用其 Timeline
- **AND** Action request MUST继续只由 nested leaf 激活接受点消费
- **AND** Window fact MUST继续由 Decision TreeClip scope variable projection 产生
- **AND** 项目 MUST不保留 `IsDodging` declaration、读写节点或兼容镜像

#### Scenario: Combo leaf 前进或循环不释放 outer ownership

- **WHEN** Attack1、Attack2、Attack3、Attack4 通过 Cancel window 进入下一段，或 Attack5 进入 Attack1
- **THEN** source leaf MUST完成自己的 Action terminal lifecycle
- **AND** outer Attack MUST保持 `HasActionLocomotionOwnership=true`
- **AND** Locomotion MUST保持 ActionOverride直到 outer Attack 真正退出

#### Scenario: Dodge 后摇进入 RushAttack 不产生 ownership 空洞

- **WHEN** DodgeRecoveryCancel active 且 Attack request 使外层 Dodge 进入 Attack
- **THEN** Dodge leaf MUST提交唯一 Cancel terminal lifecycle
- **AND** outer Dodge 与 Attack MUST在同一 Tick 完成 ownership 交接
- **AND** RushAttack target activation MUST成为 Attack request 的唯一消费点
