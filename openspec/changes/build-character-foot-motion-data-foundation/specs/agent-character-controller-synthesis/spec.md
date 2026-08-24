## MODIFIED Requirements

### Requirement: Agent必须保持Generated Foot Analysis只读

Agent Character Document package MUST只把正式Graph、StateMachine、Timeline-local Curve、Presentation装配和原生AnimationClip注册Curve放入对应editable分片。`Gait Phase`、`Foot IK`与左右脚22条已Apply Foot Motion Data Curve MUST通过唯一Catalog以完整秒域Curve进入精确`editable/animation-clips/**/curves.json`，并携带完整Clip dependency baseline、Registered Curve Hash与只读`AnimationClipAnalysisInputHash`。

Raw Motion Samples、Heel/Sole内部证据、Landing/LiftOff Event topology、StepVector、Foot Path Baseline、候选生成原因与未Apply候选 MUST不进入editable分片或Mutation Plan。Agent MUST不运行Analyzer、不创建候选、不修改Library Artifact，也不得把Artifact候选当作正式Curve。

左右脚22条Foot Motion Curve MUST作为同一原子数据组替换。Document修改其中任一条时 MUST提供整个22条组的完整Curve和共同baseline；Reconciler MUST拒绝单Curve、单脚或旧property binding更新。

#### Scenario: Agent读取已Apply脚步数据

- **WHEN** Corin RunLoop已经Apply合法22条Foot Motion Curve
- **THEN** checkout MUST从原生AnimationClip Catalog输出完整曲线组
- **AND** MUST不从Artifact复制Raw Evidence或未Apply候选

#### Scenario: Agent只修改左脚Contact

- **WHEN** Document只提交`L Contact`而没有同baseline下完整22条Curve
- **THEN** Reconciler MUST在Mutation前拒绝该请求
- **AND** MUST不产生部分AnimationClip写入
