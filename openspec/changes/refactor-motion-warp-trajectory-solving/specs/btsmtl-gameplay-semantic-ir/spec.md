## ADDED Requirements

### Requirement: MotionWarp轨迹solver必须保持Numeric-Neutral

Gameplay Semantic IR MUST以typed字段表达MotionWarp source reference、Translation Mode、Target Offset Space、Target Pose参数、Rotation Mode、Rotation Method、Limit Policy及条件curve/rate。IR MUST不保存Float32/Fixed累计pose、Unity Transform、Animator Bone或运行时target对象。Float32与Fixed Target MUST从同一validated descriptor降低各自Program与state schema，不得重新遍历Timeline或发明Target专用mode。

#### Scenario: 同一Corin IR降低两个Numeric Target

- **WHEN** Corin IR包含SkewToTarget、ApproachDirection与ProgressCurve rotation
- **THEN** Float32与Fixed Program MUST包含相同业务mode、offset空间、窗口和Limit Policy
- **AND** 两者 MAY使用各自数值常量、curve codec和state slot identity
- **AND** Fixed Target MUST不降级为旧总残差算法

#### Scenario: IR包含未消费字段

- **WHEN** Translation Mode或Rotation Method不消费某条curve或rate
- **THEN** Frontend MUST拒绝含糊配置或从canonical descriptor中排除该字段
- **AND** SemanticHash MUST不依赖Editor残留的未消费数据

