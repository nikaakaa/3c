## MODIFIED Requirements

### Requirement: Foot Placement必须通过统一状态机生成双脚修正

每只脚 MUST继续只经过唯一State Machine、Foot Context、Effective Correction/Velocity和Resolved Foot Result，但内部行为政策 MUST从已归档重构中的8fc等价解释升级为权威Contact Plan政策。

新Swing Event MUST从上一帧Final Sole捕获一次Swing Origin Sole。Swing MUST用Animated Sole在`SwingOriginSole -> NextLandingProposal`方向上的空间投影计算进度，并按同一进度采样Baseline与Ground Envelope：

```text
RawSwingCorrection =
    ComponentUp * max(0, EnvelopeHeight - BaselineHeight)

PathTargetCorrection =
    animation.foot-placement-weight * RawSwingCorrection
```

State Machine MUST保留动画脚XZ、下降轨迹、最高点与旋转；MUST不使用Phase Lerp、`Baseline - AnimatedSole`、未来Landing Height、实时Path硬地面下限或Current Trace重建Swing轨迹。同Event Path Target变化 MUST只替换Target并保留唯一Effective Correction/Velocity。

LandingStarted MUST冻结`Event/Path/Surface/Plane/Normal/SupportDomain`并捕获一次Landing Residual。SupportDomain MUST由Ground Path固定接触页中包含Next Landing的连续同Surface接触段生成有限平面胶囊域；沿路径范围来自接触段端点，横向半径使用本次查询半径，identity覆盖Path、Surface、Candidate端点与几何。当前Animated Sole入口投影和Landing期间每帧投影 MUST位于域内；不得使用无限平面、边界Clamp或第二查询补洞。

Landing MUST只按同Event单调LandingHeightProgress沿Component Up交接。PlantStarted当帧 MUST从Current Effective Sole投影到Frozen Patch的SupportDomain内生成Anchor；Anchor XZ MUST来自Current Effective Sole，不得来自Prediction Point。Progress不完整、Patch无效、投影离域或入口差异超容差 MUST消费Event并进入UnlockedSupport。

Locked MUST严格输出`Anchor - AnimatedSole`并使用非零Goal权重1；MUST删除Sliding水平削弱。正常Releasing MUST只按同Event ReleaseProgress衰减入口Residual；Grounded丢失、Anchor超距与不可达才使用Safety Release。

#### Scenario: 同Event Path Target变化

- **WHEN** 输入方向变化使同一Event产生新的合法Path Target
- **THEN** State Machine MUST保留Effective Correction与Velocity并只替换Target
- **AND** MUST不重新捕获起点、启动第二Lerp或直接设置脚高

#### Scenario: Plant投影越出SupportDomain

- **WHEN** Current Effective Sole沿Component Up能投影到Patch平面但不在SupportDomain内
- **THEN** 当前Event MUST进入UnlockedSupport且不得创建Anchor
- **AND** MUST不把无限平面、Prediction Point、边界Clamp或Current Trace作为替代

#### Scenario: 正常Release

- **WHEN** Projection为Locked Event发布ReleaseStarted与单调ReleaseProgress
- **THEN** State Machine MUST从入口Effective Correction连续回到动画脚
- **AND** MUST不读取原始Constraint下降、PlantConfidence或固定Duration生成进度

### Requirement: Pelvis必须在Landing阶段约束双腿可达

State Machine MUST在Resolved Foot中发布Landing Support Intent Weight与typed Pelvis Reach Reference。Pelvis MUST继续只消费Resolved Foot Pair，并从Landing开始同时约束上一支撑腿与Landing腿的可达区间；MUST不读取Foot State、Lock Response或Context。Rebasing Proposal不得成为Stride终点，Frozen Patch不得伪造Anchor。

#### Scenario: Landing腿尚未Locked

- **WHEN** 一只脚处于Landing且Support Intent非零
- **THEN** Resolved Foot MUST发布Frozen Patch、Support Intent与Landing腿可达区间
- **AND** Pelvis MUST不等待Locked帧才接入该腿

### Requirement: Foot Placement诊断必须发布新行为事实

Diagnostics MUST从现有根Bank深冻结Contact Plan、Swing Origin/Progress、Path Target/Tracking、SupportDomain、唯一Effective Correction/Velocity、状态Trigger、Anchor、Pelvis与Goal/Solved/Physical残差。旧Constraint/PlantConfidence Runtime Ownership、实时Path硬地面下限、Sliding和兼容字段 MUST删除。

#### Scenario: 捕获新Landing行为

- **WHEN** Foot Placement、FBBIK和Writer完成同一Bank提交
- **THEN** Diagnostics MUST发布同Frame/Completion/Rig的Contact Plan到Physical结果
- **AND** Diagnostics MUST不反向影响Foot状态或Goal
