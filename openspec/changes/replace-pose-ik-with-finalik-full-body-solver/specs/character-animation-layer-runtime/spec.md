## MODIFIED Requirements

### Requirement: 动画帧必须按固定职责顺序执行

每个PresentationFrame MUST按固定职责执行读取committed Body/Intent与Program parameter、构造Fact、求值PoseStateMachine、提交target provider demand、解析readiness、采样state-local source、消费有限Action frame、执行Transition Routing与AnimationSlot、执行Local Pose composition与Virtual Bone派生、显式转换到Component Pose、执行普通Component Pose控制、从同一Component Pose扇出Lyra Foot Plant等价FootGrounding Baseline Goals与PoseBoneIKGoals Hand Goals、可选用PredictiveFootPlacementModifier只改写Swing脚，再由唯一FullBodyIK一次求解Physical biped、显式转回Local Pose并发布FinalAnimationPoseFrame。Goal Source MAY在generated plan中有确定调度先后，但 MUST不改Pose或被解释为串行IK。Action visual sampler MUST只生成有限Action sample；PoseState provider MUST只处理其state-local source。Runtime MUST不串联TwoBoneIK、LegIK、FinalIK Grounding或图外FinalIK组件。

#### Scenario: 攻击期间角色速度归零

- **WHEN** FullBodyAction Slot仍有完整权重但Body速度已经归零
- **THEN** PoseStateMachine MUST继续更新到Stop或Idle目标
- **AND** Action结束时Slot MUST回到当时的当前Source Pose

#### Scenario: Foot与Hand Goals同时生效

- **WHEN** FootGrounding锁定左脚且武器Virtual Bone生成双手目标
- **THEN** 两个Goal Source MUST从同一Component Pose分支发布Goals并由一个FullBodyIK stage汇聚消费
- **AND** Runtime MUST不为任一limb执行第二solver
