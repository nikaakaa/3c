## MODIFIED Requirements

### Requirement: 动画帧必须按固定职责顺序执行

每个PresentationFrame MUST按固定顺序读取committed Body/Intent与Program parameter、构造Fact、求值PoseStateMachine、提交target provider demand、解析readiness、采样state-local source、消费有限Action frame、执行Transition Routing与AnimationSlot、执行Local Pose composition与Virtual Bone派生、显式转换到Component Pose、执行图中声明的前置Component Pose控制、让FootPlacement与PoseBone目标源发布typed Goal Contribution、由唯一Goal Assembler形成一个Goal Set、由唯一FullBodyIK求解Component Pose、执行图中声明的合法后置Component Pose控制、显式转回Local Pose，最后发布FinalAnimationPoseFrame。前置目标源与FullBodyIK MUST共享相同输入Pose，后置控制 MUST消费求解后的Pose。Action visual sampler MUST只生成有限Action sample；PoseState provider MUST只处理其state-local source。任一阶段 MUST不重新仲裁其它阶段的选择或写回Gameplay。

#### Scenario: 攻击期间角色速度归零

- **WHEN** FullBodyAction Slot仍有完整权重但Body速度已经归零
- **THEN** PoseStateMachine MUST继续更新到Stop或Idle目标
- **AND** Action结束时Slot MUST回到当时的当前Source Pose

#### Scenario: 没有配置姿态修正

- **WHEN** 图没有前置或后置修正节点
- **THEN** Runtime MUST执行现有图中阶段，不自动添加修正或默认中性节点

#### Scenario: 后置修正失败

- **WHEN** FBBIK已完成但必需后置修正产生Invalid
- **THEN** 现有帧事务 MUST阻止部分最终结果发布并执行对应失败语义
- **AND** MUST不绕过修正只发布FBBIK结果
