## ADDED Requirements

### Requirement: Foot Analysis必须发布单调Landing Height计划与唯一Plant事实

Projection Build MUST对全部接入Predictive Foot Placement的可达循环Locomotion Clip及Blend Space Dynamic Sample，验证每个Landing Event的Approach、Landing Height、Plant、Support与Release覆盖。

每个正式Event MUST具有：

```text
稳定Event identity
ApproachStarted分析事实
唯一LandingStarted
LandingStarted到PlantStarted之间从0单调到1的LandingHeightProgress
唯一PlantStarted
非零Support区间
唯一ReleaseStarted与单调ReleaseProgress
```

Build MUST从显式Marker或versioned分析算法中只选择一组规范Landing/Plant事实；显式Marker存在时不得并行发布推断Trigger。Constraint和PlantConfidence MAY作为Editor分析输入，但 MUST不原样发布为Runtime LandingHeightProgress、Plant Trigger、Ownership或Release Progress。

Build MUST在实际source coverage中验证Landing Height窗口晚于长Approach、结束不晚于Plant、进度单调完整，并结合腿长、脚高、垂直速度和Support事实拒绝明显不可达窗口。没有合法窗口 MUST阻止Projection发布；Runtime不得生成固定Duration或fallback曲线。

#### Scenario: 循环Run具有合法Landing计划

- **WHEN** 左脚Event具有晚期LandingStarted、单调LandingHeightProgress、唯一PlantStarted与Support区间
- **THEN** Projection MUST发布这些规范事实
- **AND** Runtime MUST只用LandingHeightProgress驱动垂直交接，只用PlantStarted提交Anchor

#### Scenario: PlantConfidence先升后降

- **WHEN** PlantConfidence在Plant前先达到局部高值、随后下降、再脉冲跨过接触阈值
- **THEN** Build MAY用完整曲线推断唯一Plant onset
- **AND** MUST不把PlantConfidence原值发布为LandingHeightProgress或Runtime Ownership

#### Scenario: Constraint从最高点开始上升

- **WHEN** Constraint覆盖从Approach Contact到Plant的长区间，直接投影未来Patch会产生不可达负高度目标
- **THEN** Build MUST不把该原始Constraint当LandingHeightProgress
- **AND** Runtime MUST不在ApproachStarted时冻结Patch或向下拖脚

#### Scenario: Landing计划不完整

- **WHEN** 实际source coverage中LandingHeightProgress没有从0单调覆盖到1或结束晚于PlantStarted
- **THEN** Build MUST拒绝Projection并报告Clip、脚侧、Event和缺失区间
- **AND** MUST不使用固定0.12秒、GoalTransition或Runtime平滑补偿

### Requirement: Foot Analysis必须保持Runtime地形与Anchor隔离

Projection中每脚计划 MUST携带Event、RootLocalLanding、ApproachStarted、LandingStarted、LandingHeightProgress、PlantStarted、Support和Release事实。Runtime Swing空间进度 MUST从同帧Animated Sole和世界Landing计算；Artifact MUST不发布世界Path、Ground Height、Contact Patch、Anchor或IK Goal。

#### Scenario: 同一Run运行在不同台阶

- **WHEN** 同一已验证Run分别在平地和楼梯运行
- **THEN** 两次运行 MUST消费相同动画时序计划
- **AND** Ground Envelope、Frozen Patch与Committed Anchor MUST只由当前Presentation、State Machine和World Query生成
