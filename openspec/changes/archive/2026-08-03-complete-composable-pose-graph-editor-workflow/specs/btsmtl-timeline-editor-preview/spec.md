## ADDED Requirements

### Requirement: Source Time Authoring模块必须跨正式owner复用

Timeline Field的time ruler、marker、curve与analysis interaction、geometry和rendering MUST被抽象为不依赖Timeline数据类型的Source Time Authoring模块。Timeline AnimationTrack/Clip与Presentation Pose Source binding MUST分别通过typed owner adapter使用同一模块，并把Mutation提交给各自正式owner。模块 MUST不复制数据、不创建Locomotion Timeline、不提供任意自定义curve或SerializedProperty入口。

#### Scenario: Timeline与Pose Source编辑相同曲线类型

- **WHEN** 作者分别编辑Attack Clip和Run Pose Source的Foot Placement Weight
- **THEN** 两个页面 MUST共享key/tangent/selection/Undo交互实现
- **AND** 数据 MUST分别只写入Timeline Clip与Profile binding

#### Scenario: 提取模块后编辑Timeline marker

- **WHEN** 作者在原Timeline页面拖动Attack marker
- **THEN** Timeline AnimationTrack identity与Mutation语义 MUST保持不变
- **AND** Presentation Profile MUST不获得副本

## MODIFIED Requirements

### Requirement: 预览采样必须复用正式动画Selection与Pose Plan

Preview Controller MUST向AnimationPreviewRuntime提交当前时间、Preview Actor、Evaluation Mode和精确authoring context。Action Timeline Preview MUST复用正式Action selection、source capture、Marker映射与staged Pose Plan；Pose Graph Fact Preview MUST复用正式Fact、source demand/capture、空间化PurePose、world-aware Pose和final publication stage。两者 MUST共享同一Projection revision、一次PlayableGraph Evaluate与同一completion语义。精确Host world context完整时 MAY执行FootPlacement，缺失时 MUST在首个world-aware节点报告Unavailable，不得创建专用preview layer、第二Animator、假地面或临时Plan。

#### Scenario: 当前时间采样

- **WHEN** 作者把playhead定位到某帧并执行Sample
- **THEN** Preview MUST通过正式source backend采样当前有效source
- **AND** MUST不直接调用AnimationClip.SampleAnimation绕过Pose Plan

#### Scenario: 尝试预览Walk到Run

- **WHEN** 作者需要观察持续Locomotion State transition
- **THEN** MUST进入Pose Graph Fact Preview并修改Fact fixture
- **AND** Timeline Action Preview MUST不伪造BaseLocomotion selection

#### Scenario: 非连续seek

- **WHEN** 作者从较晚时间跳回较早时间
- **THEN** Preview session MUST按正式reset顺序清理Player、transition、FootPlacement和stage state
- **AND** MUST不保留旧world plan或solver结果

#### Scenario: Preview非连续拖动时间

- **WHEN** 作者反复拖动playhead形成非连续采样
- **THEN** Preview MUST按seek语义重建同一正式session状态
- **AND** MUST不把历史Pose当作fallback

### Requirement: Timeline Editor 必须按时间语义抽象作者内容

Timeline Editor MUST把时间尺、selection、marker、curve、analysis、clip/window几何和preview overlay拆为明确模块。纯time/marker/curve/analysis模块 MUST能被Pose Source Editor通过typed owner adapter复用；Timeline专属Track/Clip/window语义 MUST继续只存在于Timeline domain。任何模块 MUST只发出领域Mutation request，不得直接写SerializedObject、runtime结构或其它owner。

#### Scenario: 同一AnimationTrack展开全部作者内容

- **WHEN** 作者展开一个AnimationTrack
- **THEN** 固定子轨 MUST按marker、curve和clip时间语义组合显示
- **AND** Track仍是这些Action数据的唯一owner

#### Scenario: 编辑器提交一次拖动

- **WHEN** 作者拖动一个marker或curve key并释放
- **THEN** interaction模块 MUST生成一次typed mutation与一次Undo事务
- **AND** MUST不在repaint期间持续写资产

#### Scenario: Editor抽象进入Runtime

- **WHEN** Runtime assembly引用Timeline或Pose Source数据
- **THEN** MUST只看到正式authoring/runtime contract
- **AND** MUST不依赖Editor interaction、geometry或rendering类型

#### Scenario: Pose Source复用时间模块

- **WHEN** Profile打开持续Sequence source
- **THEN** Pose Source Editor MUST使用同一时间尺、marker、curve和analysis模块
- **AND** MUST不创建Timeline Track/Clip作为adapter

### Requirement: Timeline Field内部交互、几何与渲染必须分属明确模块

Timeline与Pose Source共用的时间字段 MUST把输入路由、selection/drag session、frame/value转换、row/clip/key/marker几何、rendering与领域Mutation adapter分开。模块间 MUST通过稳定view model与typed edit request通信，不得通过Timeline具体资产、EditorWindow静态状态或OnInspectorGUI重操作耦合。Timeline专属resize/window规则 MAY由Timeline adapter扩展，共用marker/curve行为 MUST只有一份实现。

#### Scenario: Resize一个Animation Clip

- **WHEN** Timeline作者拖动Clip边界
- **THEN** Timeline adapter MUST解释为Clip window mutation
- **AND** 共用Pose Source模块 MUST不获得Clip resize能力

#### Scenario: 点击右侧Inspector设置

- **WHEN** 作者精确输入selected key的时间和值
- **THEN** typed edit request MUST更新当前正式owner
- **AND** geometry与rendering模块 MUST只消费更新后的view model

#### Scenario: Authoring Preview切换Live Debug

- **WHEN** 页面切换到Live Debug
- **THEN** mutation adapter MUST禁用
- **AND** rendering MUST只显示匹配revision的正式snapshot

#### Scenario: 多个playback overlay

- **WHEN** Timeline Live Debug显示同一source的多个playback
- **THEN** overlay MUST按playback identity区分
- **AND** 共用Source Time模块 MUST不改变runtime membership
