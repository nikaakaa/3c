## MODIFIED Requirements

### Requirement: 有限Action动画必须提供统一作者工作面

Character Editor MUST提供Action Animation Workspace，从精确Character Definition、ActionProfile、Action Context call site、有限Action Timeline、Sequence Segment、Animation Sequence、Presentation producer binding、AnimationSlot consumer与Runtime Debug binding建立typed session。Workspace MUST聚合关系并提供Open Timeline/Open Sequence导航，不得创建Sequence副本、Timeline副本、Slot配置或运行时播放器。

#### Scenario: 作者打开Attack动作动画

- **WHEN** 作者从Corin Attack ActionProfile打开Workspace
- **THEN** Workspace MUST显示Gameplay、Timeline、Sequence Segment、Sequence素材、Slot、Blend、Preview与Live关系
- **AND** 每项关系 MUST解析到唯一正式owner

#### Scenario: Segment缺少Sequence

- **WHEN** Action Timeline Segment没有精确Sequence引用
- **THEN** Workspace MUST显示typed authoring错误并定位Timeline/Segment
- **AND** MUST不按AnimationClip、名称或目录猜测Sequence

#### Scenario: 缺少唯一Timeline

- **WHEN** 当前Action没有有限Timeline或解析出多个候选Timeline
- **THEN** Workspace MUST显示typed authoring错误并定位Action call site
- **AND** MUST不按显示名、目录或generated Program猜测Timeline

### Requirement: Workspace必须保持跨owner唯一写入口

Action admission与退出 MUST由ActionProfile和Gameplay Graph拥有；Sequence Clip、Marker、素材Curve、Notify与Analysis Source MUST由Animation Sequence拥有；Segment范围、ClipIn、Weight/Ease、Section、Window、Motion、Warp与Cue MUST由Action Timeline拥有；producer binding MUST由Animation Presentation Profile拥有；Slot topology与Blend Policy MUST由Pose Graph拥有。Workspace mutation MUST调用对应正式owner API，不得保存镜像字段或第二Undo。

#### Scenario: 修改攻击素材Marker

- **WHEN** 作者从Workspace打开Attack Sequence并移动Marker
- **THEN** mutation MUST只写入Sequence
- **AND** Action Timeline、Profile与Pose Graph MUST不保存Marker副本

#### Scenario: 修改攻击片段范围

- **WHEN** 作者调整Action Timeline中的Sequence Segment范围
- **THEN** mutation MUST只写入Timeline Segment
- **AND** Sequence素材duration、Marker与Curve MUST保持不变

#### Scenario: 修改攻击素材资源

- **WHEN** 作者在Workspace为攻击选择另一份Animation Sequence
- **THEN** mutation MUST写入正式Sequence Segment或producer binding的Sequence引用owner
- **AND** Workspace、ActionProfile与Pose Graph MUST不保存资源副本

#### Scenario: 修改受击进入混合

- **WHEN** 作者从Workspace调整对应AnimationSlot transition policy
- **THEN** mutation MUST写入Pose Graph或正式Policy owner
- **AND** Timeline与Sequence MUST不保存Blend duration或Inertialization参数
