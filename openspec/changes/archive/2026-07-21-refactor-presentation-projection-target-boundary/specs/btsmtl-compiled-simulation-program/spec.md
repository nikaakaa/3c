## MODIFIED Requirements

### Requirement: Presentation Projection 必须与 Gameplay Numeric Target 分离

Compiler MUST从validated Gameplay Semantic IR artifact建立唯一target-neutral `CharacterPresentationSemanticContract`，并与同一authoring root的Presentation inventory及正式Animation Analysis artifacts生成`CharacterPresentationProjection`。Contract MUST规范保存ProgramId、Gameplay SourceRevision、SemanticHash、按index排序的producer contract与ContractHash。Projection MUST保存该ContractHash与独立ProjectionRevision，MUST不保存或接收Float32/Fixed ProgramHash、LayoutHash、NumericProfile、Target ABI、State codec或target-specific constant。Projection Compiler的正式输入 MUST不包含任何Numeric Target Program；Float32与Fixed Program只能在各自严格加载后通过Target Adapter生成同一Presentation contract。

Projection用于映射producer identity到AnimationClip、Animancer、Camera、Cue、Equipment Visual资源，以及由显式Presentation Analysis Source生成的每脚动画特征。Projection MAY保存Calibration/Analysis identity和压缩后的表现feature curve，但 MUST不保存Graph flow、State transition、Timeline Window、MotionCurve、GameplayEffect真值、Gameplay contact、Sampling Rig实例或Editor采样状态。生成特征 MUST不进入Semantic IR、Numeric Program、Character State、Snapshot、Gameplay Hash或Network协议。

#### Scenario: 客户端定位攻击动画

- **WHEN** 任一Numeric Target Program输出Attack producer command
- **THEN** Presentation MUST通过匹配Presentation contract的Projection定位Unity动画资源并采样对应Foot Analysis
- **AND** Projection MUST不决定Attack状态、Window或Gameplay命中

#### Scenario: 同一语义生成Float32与Fixed Program

- **WHEN** 同一validated Semantic IR生成Float32 Program与Fixed Program
- **THEN** 两个Target Adapter MUST生成相同Presentation ContractHash并加载同一Projection
- **AND** 两个Program MUST继续拥有各自不同的ProgramHash、LayoutHash、NumericProfile与ABI

#### Scenario: 纯表现分析变化

- **WHEN** AnimationClip内容、Analysis Source内容或Rig Calibration改变但Gameplay authoring语义不变
- **THEN** ProjectionRevision MUST改变
- **AND** Gameplay SourceRevision、Semantic operation、State layout、Numeric Program payload与各Target ProgramHash MUST保持不变

#### Scenario: Graph Camera producer编译

- **WHEN** Projection Compiler处理Graph来源的Camera producer
- **THEN** MUST从validated Semantic IR operation、reference、source map与numeric-neutral literal生成Camera binding
- **AND** MUST不先生成Float32或Fixed Program再反读target constant

### Requirement: Program 与 Projection 必须在同一 Build Transaction 中发布

Character Simulation Build MUST按`Frontend artifact -> Presentation contract -> resolve exact Animation Analysis artifacts -> independently compile Presentation Projection and requested Numeric Target Programs -> cross-artifact identity validation -> atomic publish`执行。ProjectionRevision MUST由Projection schema、Presentation ContractHash、Presentation authoring dependency与Analysis artifact identity/content hash规范计算，MUST不包含任一Target ProgramHash、NumericProfile或ABI。单clip artifact MAY在该事务之前独立生成，但Build MUST重新校验其完整identity和payload hash。Semantic IR cache、Projection、全部请求Target canonical artifact、Unity wrapper与generated reference MUST先完成stage和exact重读，再作为一个发布组提交；任一artifact、Target或Projection阶段失败 MUST恢复完整旧发布组，不得更新一半generated reference。

#### Scenario: Ready artifact被复用

- **WHEN** Build发现全部Analysis artifacts Ready且精确匹配
- **THEN** Build MAY跳过AnimationClip重新采样
- **AND** Projection、请求Target Program发布事务和最终contract校验 MUST仍完整执行

#### Scenario: Artifact损坏

- **WHEN** 任一Analysis artifact存在但codec或hash校验失败
- **THEN** Build MUST失败并定位对应stable clip binding
- **AND** MUST不使用旧Projection或默认feature继续发布

#### Scenario: Fixed-only产品构建

- **WHEN** Product Build显式只请求Fixed Numeric Target
- **THEN** Build MUST从同一Frontend artifact生成唯一Projection与Fixed Program并验证相同Presentation contract
- **AND** MUST不生成Float32 Program作为Projection编译的隐藏前置产物

#### Scenario: 作者修改角色配置

- **WHEN** 作者修改Character authoring、Timeline或Presentation依赖
- **THEN** Editor MUST把现有产物视为stale，并只在显式角色编译、显式Compile All Stale或Product Build请求中执行完整Build Transaction
- **AND** 域重载、资产导入与退出Play Mode MUST不自动扫描或编译Character Simulation产物
