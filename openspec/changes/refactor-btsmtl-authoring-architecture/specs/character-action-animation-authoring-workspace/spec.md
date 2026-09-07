## MODIFIED Requirements

### Requirement: 有限Action动画必须提供统一作者工作面

工作区 MUST以精确Character Definition、SkillDefinition、ActionProfile、子图／Timeline调用点、有限animation producer、AnimationClip、Presentation binding和Slot consumer建立typed作者上下文。技能可以没有Timeline，也可以具有多个或嵌套Timeline；工作区 MUST显示真实结构，并在需要单个编辑目标时要求明确选择。MUST不按名称、目录或首个候选猜目标，不创建新播放器或镜像资源。

#### Scenario: 作者打开攻击技能

- **WHEN** 从ActionProfile或角色技能目录进入工作区
- **THEN** MUST显示关联技能、Tree／子图、Timeline和动画owner
- **AND** 若策略关联多个技能 MUST显式区分

#### Scenario: 技能没有Timeline

- **WHEN** 当前技能只包含Tree逻辑
- **THEN** MUST正常显示技能内容，不能视为缺失唯一Timeline的错误

#### Scenario: 技能具有多个Timeline

- **WHEN** 多个子图分别播放Timeline
- **THEN** MUST按调用点列出，并按明确选择打开对应编辑器

#### Scenario: 作者打开Attack动作动画

- **WHEN** 作者从Corin Attack ActionProfile打开Action Animation Workspace
- **THEN** Workspace MUST显示精确关联技能及该Action的Gameplay、Timeline Segment、direct Clip、Slot、Blend、Preview和Live关系
- **AND** 每项关系 MUST解析到唯一正式owner

#### Scenario: 缺少唯一Timeline

- **WHEN** 技能没有Timeline或存在多个候选Timeline
- **THEN** 技能工作区 MUST将这些情况作为合法内容结构显示
- **AND** 需要单一Timeline编辑目标时 MUST显式选择调用点；缺少选择可报定位结果
- **AND** MUST不按显示名、目录或首个候选猜Timeline


### Requirement: Workspace必须保持跨owner唯一写入口

Action admission策略 MUST由ActionProfile拥有；角色选择与替换流程由代码控制，技能内部退出与内容由SkillDefinition及技能图拥有；Animation Segment的Clip引用、Start/End、ClipIn、Weight与Ease MUST由有限Action Timeline拥有；AnimationClip骨骼内容与注册Curve MUST由原生AnimationClip拥有并通过Unity Animation Window编辑；Window、Motion、Warp和Cue MUST继续由Timeline拥有；producer identity、Rig与Analysis装配 MUST继续由Animation Presentation Profile拥有；Slot topology与Blend Policy MUST继续由Pose Graph拥有。Workspace mutation MUST写入对应正式owner，不得保存镜像字段或第二Undo。

#### Scenario: 修改攻击动画引用

- **WHEN** 作者在Workspace替换Animation Segment引用的Clip
- **THEN** mutation MUST写入正式Timeline Segment
- **AND** Workspace、ActionProfile与Pose Graph MUST不保存Clip副本

#### Scenario: 修改Clip表现曲线

- **WHEN** 作者从Workspace打开Foot Placement Weight
- **THEN** Workspace MUST打开精确AnimationClip和Preview Target
- **AND** MUST不在Timeline Segment或Profile创建Curve副本


## ADDED Requirements

### Requirement: 技能工作区必须保持精确页面与实例上下文

工作区 MUST分别保存角色／SkillDefinition／作者调用点和Actor／ActionInstance／运行调用generation绑定。重载后只恢复仍有效的稳定作者页面；运行刷新不得改变焦点、草稿、选择或滚动。找不到原实例或页面时必须显示失效位置，不能选择同模板的另一个实例。

#### Scenario: 两个释放使用同一技能

- **WHEN** 当前窗口绑定其中一个ActionInstance
- **THEN** 时间轴、局部变量和高亮 MUST只来自该实例

#### Scenario: 编译后恢复技能页面

- **WHEN** Editor重载后原子图仍存在
- **THEN** MUST恢复作者页面和画布位置，但不恢复旧运行对象或自动Build

#### Scenario: 字段编辑时运行刷新

- **WHEN** 作者正在输入允许修改的数值
- **THEN** 只读刷新 MUST保留未提交文本，且不产生额外Undo
