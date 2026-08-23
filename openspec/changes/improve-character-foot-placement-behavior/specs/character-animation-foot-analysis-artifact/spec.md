## ADDED Requirements

### Requirement: Foot Analysis必须发布权威Foot Contact Plan

Projection Build MUST对全部接入Foot Placement的可达循环Locomotion Clip及Blend Space Dynamic Sample，验证每个Landing Event的Approach、Landing Height、Plant、Support与Release覆盖，并把同一Event的事实原子发布为`AnimationFootContactPlanSample`：

```text
LandingEventIdentity / PlanSourceIdentity
ApproachStarted
LandingStarted / LandingHeightProgress
PlantStarted
SupportWeight
ReleaseStarted / ReleaseProgress
```

Started字段 MUST在对应onset后保持为true直到该Event语义结束，不得成为可能因Presentation采样跳过而丢失的单帧脉冲。Analyzer algorithm version、规范onset和进度曲线 MUST进入Artifact identity与Projection revision。

Build MUST从显式Marker或versioned分析算法中只选择一组规范事实；显式Marker存在时不得并行发布推断Trigger。第一版计划 MUST保留现有规范LandingPhase作为Plant onset，把ReleasePhase到LiftOffPhase编译为显式Release计划，并从完整sole下降轨迹生成独立LandingStarted与单调Landing Height曲线。Constraint和PlantConfidence MAY作为Editor分析输入，但 MUST不原样发布为Runtime Progress、Trigger或Ownership。

Build MUST在实际source coverage中验证Landing Height窗口晚于长Approach、结束不晚于Plant、进度从0单调完整覆盖到1，并结合腿长、脚高、垂直速度和Support事实拒绝明显不可达窗口。没有合法窗口 MUST阻止Projection发布；Runtime不得生成固定Duration或fallback曲线。

Blend Space MUST从拥有当前Step、Landing Event和Route的同一authoritative source读取整组Plan。不同Event identity的Trigger或Progress MUST不按source weight平均。Compiler MUST验证全部可达Dynamic Sample与Locomotion同步关系具有一致事件顺序、完整窗口与authority切换coverage；Runtime State Context MAY只对同一Event Progress取单调最大值。

#### Scenario: 循环Run具有合法Contact Plan

- **WHEN** 左脚Event具有晚期LandingStarted、单调LandingHeightProgress、唯一PlantStarted、Support与Release区间
- **THEN** Projection MUST原子发布匹配Event和Plan Source的Contact Plan
- **AND** Runtime MUST只用该Plan驱动Landing、Plant和正常Release

#### Scenario: Blend Space权威Source切换

- **WHEN** 同一Landing Event期间最大贡献source从一个Dynamic Sample切换到另一个
- **THEN** Runtime MUST从当前Step/Event/Route的同一authoritative source读取整组Plan并保持同Event进度不回退
- **AND** MUST不平均两个Event的Plant Trigger或拼接不同Event的Landing区间

#### Scenario: Landing计划不完整

- **WHEN** 实际source coverage中LandingHeightProgress没有单调覆盖到1或结束晚于PlantStarted
- **THEN** Build MUST拒绝Projection并报告Clip、脚侧、Event和缺失区间
- **AND** MUST不使用固定Duration、GoalTransition或Runtime平滑补偿

### Requirement: Foot Contact Plan必须与Runtime地形隔离

Projection Plan MUST只携带动画Event、RootLocalLanding、Approach、Landing、Plant、Support与Release事实。Artifact MUST不发布世界Path、Ground Height、Contact Patch、SupportDomain、Anchor或IK Goal。

#### Scenario: 同一Run运行在不同台阶

- **WHEN** 同一已验证Run分别运行在平地和楼梯
- **THEN** 两次运行 MUST消费相同动画Contact Plan
- **AND** Ground Envelope、SupportDomain、Frozen Patch与Anchor MUST只由当前Presentation生成
