## ADDED Requirements

### Requirement: Editor tuning block必须在PresentationFrame Prepare前原子应用

Editor测试期间，Animation Runtime MAY接收与当前Program、Projection、Pose Plan、Rig和Tuning Layout精确匹配的完整`CharacterPoseTuningParameterBlock` candidate。Runtime MUST在PresentationFrame读取Fact、source demand和任何Pose operation之前验证并交换Active block；交换 MUST发生在Animancer Evaluate Barrier之前，且所有本帧consumer MUST读取同一个candidate revision。该Editor写入口 MUST不进入Player、Gameplay、Network、Rollback或正式发布资源闭包。

#### Scenario: 帧开始前存在有效Pending block

- **WHEN** PresentationFrame开始且Pending block通过全部identity、range和组合约束校验
- **THEN** Runtime MUST一次交换完整Active page并记录Applied Frame
- **AND** 本帧source、blend、world-aware stage、FinalIK和FinalPublication MUST观察同一revision

#### Scenario: Candidate identity不匹配

- **WHEN** Pending block的Projection、Pose Plan、Rig或Layout identity与当前runtime不一致
- **THEN** Runtime MUST拒绝Pending并继续使用旧Active block
- **AND** MUST不重新编译、重建runtime或选择fallback block

#### Scenario: Barrier后收到candidate

- **WHEN** 当前帧已经越过Animancer Evaluate Barrier
- **THEN** candidate MUST等待下一PresentationFrame
- **AND** MUST不修改当前帧Physical Transform writer或FinalPublication输入
