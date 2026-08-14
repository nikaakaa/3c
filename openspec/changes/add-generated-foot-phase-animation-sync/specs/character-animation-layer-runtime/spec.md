## MODIFIED Requirements

### Requirement: Marker同步必须编入对应source-local计划

MarkerGroup binding MUST提供canonical SyncGroupId、topology、SyncRole、Time Mapping、marker occurrence和duration。PoseState relation MUST以StateMachine、Transition generation和两侧Player operation identity为key；Action relation MUST以Slot、AnimationPlaybackId和source usage为key。Runtime MUST在source采样前定位leader有向Marker pair与linear segment fraction，再按编译计划明确执行`MarkerSegmentFraction`或`GeneratedFootPhase`，生成follower fraction与effective sample，并在leader仍有相位覆盖的共同可见期间每帧持续求值。有限leader到达最后marker coverage时 MUST只提交一次终点映射，后续共同可见帧 MUST让follower从该continuation anchor按自己的raw delta连续推进，不得每帧把follower重新压回同一终点。

`GeneratedFootPhase` MUST只查Projection中的精确occurrence warp plan，不得读取Foot Analysis artifact、当前骨骼、最终混合Pose、FootGrounding或IK结果。Pose Graph MUST不序列化MarkerSync节点，Runtime MUST不按State名、clip名、Action名、priority或weight推导relation或Time Mapping。

Sequence Player使用Movement clock时 MUST在source获得relevance时锁定精确`OwnerIdentity + Generation`。相同identity内clock MUST单调；identity变化 MUST显式重基线。transition保留的outgoing source MUST继续使用自己锁定的identity，不得因当前Movement winner改变而改绑。Marker relation MAY改变effective time，但 MUST不改变该clock identity或raw time。

#### Scenario: Walk切换Run

- **WHEN** Pose Transition两侧State的唯一同步候选source同组并共同选择GeneratedFootPhase
- **THEN** Source Sync Plan MUST持续把Walk leader fraction查表映射为Run follower fraction
- **AND** Gameplay movement与Transition start MUST不等待marker边界

#### Scenario: Action使用通用Marker比例

- **WHEN** Slot relation两侧共同选择MarkerSegmentFraction
- **THEN** Runtime MUST明确使用leader fraction作为follower fraction
- **AND** MUST不要求Foot Analysis artifact或自动改用GeneratedFootPhase

#### Scenario: GeneratedFootPhase计划损坏

- **WHEN** relation选择到不存在的occurrence plan或warp knot identity不匹配
- **THEN** Runtime MUST报告稳定typed invalid并阻止正式动画帧发布
- **AND** MUST不退回linear fraction、normalized time、Animancer自动同步或上一帧effective time

#### Scenario: 两侧source没有共同MarkerGroup

- **WHEN** Compiler无法从两侧binding找到共同canonical MarkerGroup
- **THEN** 两侧source MUST使用各自raw time
- **AND** compiled plan MUST为None且不得保留Time Mapping或warp引用

#### Scenario: transition期间Movement owner改变

- **WHEN** incoming source由新的Movement producer驱动且outgoing source仍被transition保留
- **THEN** incoming Player MUST锁定新owner与generation，outgoing Player MUST保持自己的已锁定identity
- **AND** Runtime MUST不把同一Movement通道当前winner的clock广播覆盖全部相关Player

#### Scenario: MovingTurn终点切入Run

- **WHEN** finite MovingTurn作为leader在最后marker coverage建立到Run的同步关系
- **THEN** Runtime MUST先把Run映射到匹配的终点脚相位并建立continuation anchor
- **AND** 后续混合帧 MUST让Run正常推进，不得因MovingTurn Pose仍被保留而冻结Run sample
