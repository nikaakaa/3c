## MODIFIED Requirements

### Requirement: Presentation Projection 必须与 Gameplay Numeric Target 分离

Compiler MUST从validated Gameplay Semantic IR artifact建立唯一target-neutral `CharacterPresentationSemanticContract`。Contract MUST规范保存ProgramId、Gameplay SourceRevision、SemanticHash、按index排序且只包含有限Gameplay-owned `AnimationChannelId`的producer contract与ContractHash。Projection Compiler MUST结合Presentation Profile、Pose Graph、PoseStateMachine、state-local pose source、node-local Blend/Inertialization Policy、Rig Definition、有限Action producer inventory和正式Animation Analysis artifacts生成唯一`CharacterPresentationProjection`。Projection MUST保存typed Presentation Fact布局、state-local source binding、Action channel binding、dense Rig、Transition source sync、Foot Analysis、独立ProjectionRevision以及内部唯一不可变`CharacterPoseProgramImage`。Program Image MUST完整保存PoseProgramImageHash、Pose Stage、Operation Header、Family Payload、typed Value/Workspace layout、Source Map与Capacity Manifest；旧compiled Pose Plan万能Operation payload、`CharacterPresentationPoseProgram`别名和Runtime第二语义Program容器 MUST删除。

Projection与Program Image MUST随同一ProjectionRevision原子发布。Runtime、Preview与Live Debug MUST直接读取Projection内部Program Image，不得现场编译、重排、补字段或构造第二Pose程序真相。Unity执行层 MAY让每个`CharacterPoseProgramRuntime`建立最多一份actor-local、只读、identity与PoseProgramImageHash精确匹配的Execution View，只逐值materialize不可序列化执行存储；该View MUST不拥有Frame状态、运行时Tuning或独立schema，并由对应Program Runtime唯一Dispose。Projection与Program Image MUST不保存或接收Float32/Fixed ProgramHash、LayoutHash、NumericProfile、Target ABI、State codec或target-specific constant。

Projection MUST把state-local PresentationPoseSource identity与有限Action producer identity分别映射到AnimationClip/Animancer source、Camera、Cue、Equipment Visual和其它表现资源。持续Locomotion MUST由committed Body/Intent构造Presentation Fact后进入PoseStateMachine，不得包装成Gameplay producer或AnimationChannel。有限Action channel MUST只进入ActionPlaybackInput并由唯一AnimationSlot消费。Projection、Pose Graph、BlendStack和Inertialization MUST不保存Gameplay Graph flow、Timeline Gameplay Window、MotionCurve、GameplayEffect真值、Gameplay contact或Editor采样状态。Pose Graph/Policy/Rig/Mask/Parameter变化只属于Presentation dependency；有限Action AnimationChannel或producer semantic变化属于Gameplay contract。

#### Scenario: 客户端定位Attack动画

- **WHEN** 任一Numeric Target Program为FullBodyAction channel输出Attack producer command
- **THEN** Presentation MUST通过匹配contract的Projection定位source并路由到FullBodyAction ActionPlaybackInput
- **AND** AnimationSlot MUST把该Action source插入同帧Locomotion基础Pose
- **AND** Pose Graph MUST不决定Attack状态、Window或Gameplay命中

#### Scenario: 客户端求值持续Locomotion

- **WHEN** 任一Numeric Target Program提交移动Body但没有有限Action command
- **THEN** Presentation MUST从committed Body/Intent构造Fact并执行Projection内部唯一Program Image的PoseStateMachine
- **AND** Program与Semantic Contract MUST不包含BaseLocomotion animation producer

#### Scenario: 同一语义生成Float32与Fixed Program

- **WHEN** 同一validated Semantic IR生成Float32与Fixed Program
- **THEN** 两个Target Adapter MUST生成相同AnimationChannel producer contract并加载同一Projection内部Program Image
- **AND** 两个Program MUST继续拥有各自不同ProgramHash、LayoutHash、NumericProfile与ABI

#### Scenario: 只修改Pose Graph Mask

- **WHEN** 作者修改FullBodyAction Bone Mask但Gameplay authoring不变
- **THEN** ProjectionRevision与内部Program Image identity MUST改变
- **AND** Gameplay SourceRevision、Semantic operation、ContractHash与各Target ProgramHash MUST保持不变

#### Scenario: Runtime尝试构造第二语义Program

- **WHEN** Runtime加载合法Projection后尝试重新编译、重排或补齐Program Image，或者为每个Actor构造可变Native Program
- **THEN** Runtime装配 MUST失败或该构造路径 MUST在迁移时删除
- **AND** Preview与正式Runtime如需Execution View MUST各自由自己的Program Runtime建立最多一份同identity只读物理视图，不得根据旧类型或schema选择不同Pose执行链
