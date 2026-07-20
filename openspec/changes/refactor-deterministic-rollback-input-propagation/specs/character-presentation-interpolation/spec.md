# character-presentation-interpolation Delta

## ADDED Requirements

### Requirement: Rollback 远端表现必须优先消费 Relayed Explicit Input 产生的当前分支

DeterministicRollback Peer MUST在Simulation执行阶段优先使用目标Tick已经到达的远端Relayed Explicit input，并将所得Body与动画producer lifecycle作为predicted current branch提交给现有Presentation Runtime。Presentation MUST不直接消费网络input、canonical packet或远端Transform。Confirmed horizon MUST不作为远端固定render delay。Canonical provenance晋升未改变GameplayHash时 MUST不生成Body correction、animation replace/retire或visual follower重新定向。

#### Scenario: 远端移动输入在执行前到达

- **WHEN** Peer B在Tick T执行前收到Peer A的Tick T Relayed Explicit MoveAxis
- **THEN** Fixed Program与KCC MUST用该输入生成Peer A的Tick T Body/动画输出
- **AND** Presentation MUST显示该predicted current branch

#### Scenario: Canonical Bundle 内容相同

- **WHEN** 后续canonical bundle与已经表现的Relayed Explicit input具有相同GameplayHash
- **THEN** Body history与animation lifecycle MUST保持当前分支
- **AND** visual correction MUST不启动

#### Scenario: Explicit Input 真正迟到

- **WHEN** Relayed Explicit input改变了已经执行的Tick T GameplayHash
- **THEN** Rollback output adapter MUST在同一outer transaction提交Replay后的最终Body与动画净分支
- **AND** visual follower MAY从当前visible pose收敛剩余误差

### Requirement: Rollback 动画同步必须来自同一 Gameplay 输入模拟

Rollback网络协议 MUST不发送AnimationClip、Animator state、Animancer state、normalized time或visual pose。每个Peer MUST从同一Fixed Program输入与Action/Timeline状态生成稳定producer lifecycle；PresentationFrame再独立推进Animancer sample/fade时间。进攻request的选择性延迟 MUST作用于Gameplay request eligible tick，因此本地与远端从同一SimulationTick开始对应动作，而不是由表现层等待或瞬切补齐。

#### Scenario: 双 Peer 进入 Attack Producer

- **WHEN** Offensive Attack request在Tick T变为eligible并进入双方同一Gameplay input history
- **THEN** 两端Fixed Program MUST从Tick T生成相同producer lifecycle identity
- **AND** 各自Animancer MUST在本地表现帧连续采样该producer

#### Scenario: 连续移动驱动循环动画

- **WHEN** Relayed MoveAxis持续到达且Locomotion状态保持Run
- **THEN** 远端Run producer MUST由本地模拟持续拥有
- **AND** 网络协议 MUST不逐帧同步Run动画时间
