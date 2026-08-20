## MODIFIED Requirements

### Requirement: 人工编辑与Document Apply必须复用同一类型化Mutation

窗口交互与Agent Authoring Document Reconciler MUST分别把用户操作或目标状态差异降低为同一领域类型化Mutation，再由同一Validator、transaction、dirty owner和Undo边界应用。系统 MUST不允许Document直接写Unity YAML、SerializedObject path、AnimationClip序列化文本或构造第二套Pose/Clip资产写服务。

#### Scenario: UI与Document修改同一Transition

- **WHEN** 人工UI或Document v4修改Pose transition blend policy
- **THEN** 两条入口 MUST生成同一种Presentation Mutation
- **AND** 最终资产约束、诊断和revision变化 MUST一致

#### Scenario: Animation Window入口与Document修改同一Clip Curve

- **WHEN** 两条入口替换同一注册Curve
- **THEN** 两条入口 MUST调用同一Clip Curve validator与Mutation语义
- **AND** MUST进入各自单一Undo事务并产生相同canonical结果
