## ADDED Requirements

### Requirement: Owner authority command 必须区分 canonical input 与 prediction result

ServerAuthoritative adapter MUST 从 CharacterInputFrame、accepted action request 和配置 identity 构造权威端可独立执行的 command。`ResolvedCharacterMotionFact` MAY 作为 prediction comparison metadata 进入模型 history 或 packet，但 MUST NOT 成为服务端唯一 displacement、yaw 或 pose 输入。

#### Scenario: Owner 本地预测完成

- **WHEN** LocalSolver 同 tick 产生 canonical input facts 和 resolved motion result
- **THEN** adapter MUST 将两者保存为不同语义字段
- **AND** authoritative backend MUST 只消费 canonical input/action fields
- **AND** correction comparison MAY 读取 prediction result

### Requirement: Remote Motion 必须以 Character external pose/input 语义进入 Pipeline

ServerAuthoritative MotionSnapshot MUST 先由模型 binding 缓冲和采样，再转换为 Character `ExternalPoseSample`、movement summary 和 `ExternalPresentationPose`。Character SyncDomain MUST NOT 保存 MotionSnapshot packet、server clock、interpolation delay 或 Fantasy message。

#### Scenario: 收到远端移动快照

- **WHEN** 模型收到较新的 MotionSnapshot
- **THEN** snapshot MUST 进入对应 SubjectActorId 的 model-owned buffer
- **AND** Character logic tick MUST 只接收 resolved external pose/input

### Requirement: Remote Action Replication 必须保持 Gameplay 身份

ActionReplication MUST 使用 ActionId、ActionInstanceId、SubjectActorId、phase 和 source tick 表达远端动作，不得携带 Timeline、Track、Clip、AnimationClip、producer 或 Animancer identity。Activation MUST 转换为 `ExternalActionActivation`；terminal phase MUST 转换为既有 `ActionLifecycleTransition`。

#### Scenario: Owner 动作被确认

- **WHEN** 服务端接受 Owner ActionActivation
- **THEN** Owner MUST 收到模型 ActionDecision
- **AND** 其它 Session MUST 收到以该 Actor 为 Subject 的 ActionReplication activation

#### Scenario: Owner 动作被拒绝

- **WHEN** 服务端拒绝 ActionActivation
- **THEN** Owner MUST 收到 Reject lifecycle
- **AND** 其它 Session MUST 不收到该动作 replication

### Requirement: Remote 派生事实不得反向同步

ExternalFacts + ExternalPose Character MAY 因远端 action/pose 运行同一 Graph、Timeline 和动画，但其派生 motion、lifecycle、window、cue、result 与 state facts MUST NOT 再进入 ServerAuthoritative outgoing。该抑制 MUST 发生在 binding 收发资格边界，不得通过删除本地 gameplay facts 实现。

#### Scenario: 远端 Attack 打开 HitWindow

- **WHEN** 远端 Character 的现有 Timeline 产生 HitWindow fact
- **THEN** fact MAY 供本地 gameplay 表现和 diagnostics 使用
- **AND** binding MUST 不把它发回服务端
