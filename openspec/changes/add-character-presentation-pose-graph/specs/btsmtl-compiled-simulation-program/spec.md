## MODIFIED Requirements

### Requirement: Presentation Projection 必须与 Gameplay Numeric Target 分离

Compiler MUST从validated Gameplay Semantic IR artifact建立唯一target-neutral `CharacterPresentationSemanticContract`。Contract MUST规范保存ProgramId、Gameplay SourceRevision、SemanticHash、按index排序且包含`AnimationChannelId`的producer contract与ContractHash。Projection Compiler MUST结合Presentation Profile、Pose Graph、Blend Library、Rig Definition、producer resource inventory和正式Animation Analysis artifacts生成唯一`CharacterPresentationProjection`。Projection MUST保存channel-to-PoseSlot binding、compiled slot stack payload、dense Rig、`CharacterPresentationPoseProgram`、Marker Sync、Foot Analysis与独立ProjectionRevision，MUST不保存或接收Float32/Fixed ProgramHash、LayoutHash、NumericProfile、Target ABI、State codec或target-specific constant。

Projection MUST只把producer identity映射到AnimationClip/Animancer source、Camera、Cue、Equipment Visual和其它表现资源，并把已经解析的channel输入交给Pose Slot/Graph。Projection、Pose Graph和Blend Stack MUST不保存Graph flow、State transition、Timeline Gameplay Window、MotionCurve、GameplayEffect真值、Gameplay contact或Editor采样状态。Pose Graph/Blend/Rig/Mask/Parameter变化只属于Presentation dependency；AnimationChannel或producer semantic变化属于Gameplay contract。

#### Scenario: 客户端定位Attack动画

- **WHEN** 任一Numeric Target Program为FullBodyAction channel输出Attack producer command
- **THEN** Presentation MUST通过匹配contract的Projection定位source并路由到FullBodyActionSlot
- **AND** Pose Graph MUST不决定Attack状态、Window或Gameplay命中

#### Scenario: 同一语义生成Float32与Fixed Program

- **WHEN** 同一validated Semantic IR生成Float32与Fixed Program
- **THEN** 两个Target Adapter MUST生成相同AnimationChannel producer contract并加载同一Projection/Pose Program
- **AND** 两个Program MUST继续拥有各自不同ProgramHash、LayoutHash、NumericProfile与ABI

#### Scenario: 只修改Pose Graph Mask

- **WHEN** 作者修改FullBodyAction Bone Mask但Gameplay authoring不变
- **THEN** ProjectionRevision MUST改变
- **AND** Gameplay SourceRevision、Semantic operation、ContractHash与各Target ProgramHash MUST保持不变
