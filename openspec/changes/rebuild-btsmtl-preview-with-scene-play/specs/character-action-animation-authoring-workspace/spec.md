## REMOVED Requirements

### Requirement: Workspace Preview必须只运行表现链

**Reason**：表现专用 Action/Base Pose fixture 无法表达本次要求的完整场景运行。
**Migration**：动作试验进入统一独立场景 Play，经正式输入与 Action admission 执行，工作区观察同一次真实运行。

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

### Requirement: Workspace Live Debug必须只读取正式Trace

Workspace 的场景预览观察与外部 Live Debug MUST从匹配正式运行身份的诊断源显示精确 SkillDefinition/SkillProgram、Actor、ActionInstance、作者调用路径/运行 generation、Tree/技能局部状态、Action lifecycle、实际存在的 Timeline/动画 playback、AnimationSlot route、Blend/Stored/Inertialization 状态与 Final Pose 贡献；Tree-only 技能没有 Timeline sample 时 MUST正常显示其 Tree 和释放进度。来源 MUST区分登记的控制代码与技能 operation，不能伪造角色图节点。观察区域 MUST只读，不得重新执行 Graph、Timeline 或 Pose，也不得显示 Action Phase relation。运行可调的作者字段 MUST位于明确的作者区域，通过同一 Mutation 与精确 Actor 参数入口修改，不得编辑 Trace 或推断已生效。

#### Scenario: Action被Hit打断

- **WHEN** 正式 Runtime 发生 Attack 到 Hit 的 Action replacement
- **THEN** 工作区 MUST显示旧 Action terminal、替换 command、Slot route、混合策略与最终 Pose 贡献
- **AND** 所有观察 MUST来自同一正式运行事实

#### Scenario: Trace过期

- **WHEN** Trace 与已发布角色包、SkillProgram/调用来源或 Projection 身份不匹配
- **THEN** 工作区 MUST显示过期并停止错误关联
- **AND** MUST不自动 Build、按显示名重建关系或用编辑游标补算

### Requirement: Workspace必须保持Numeric Target与显式Build边界

Workspace 作者文档 MUST不保存 NumericProfile 或 Float32/Fixed runtime state。场景配置 MUST明确选择正式 Numeric Target，工作区 MUST只读显示该次 Session 的选择；相同 Presentation Contract 的 Float32 与 Fixed MUST映射到同一 producer、AnimationSlot 和 Pose Plan。窗口打开、selection、mutation、普通开始预览、Live Debug 和 asset import MUST不自动 Build、重分析或选择替代产物；明确构建操作 MUST使用精确 Character Definition 和所选 Target，消费其控制 binding/版本、SkillProgram 目录、共享子图和完整发布身份，不要求存在角色总控 RootTree。

#### Scenario: 查看Fixed Session动作

- **WHEN** 工作区连接匹配合同的 Fixed Session
- **THEN** 观察 MUST显示 Fixed Target identity 和 committed raw sample
- **AND** 表现 MUST复用对应的 target-neutral Projection，不切换 Float32

#### Scenario: 修改Timeline Clip

- **WHEN** 作者在 Edit Mode 完成 Timeline mutation
- **THEN** 正式 owner MUST进入原有 Undo 并显示产物状态
- **AND** 系统 MUST等待明确 Dry Run、Build 或构建并开始操作

## ADDED Requirements

### Requirement: 动作工作区必须通过场景真实角色试验动作

动作工作区 MUST消费主重构的 Character Definition、SkillDefinition、唯一 ActionProfile 引用和真实技能 Root/调用路径，并通过统一场景预览操作连接 Actor；Timeline、producer、Clip 和 Slot 只在技能实际拥有或引用时解析。试验 MUST经正式输入、C# 控制和唯一 Action 服务准入建立 ActionInstance，再观察该释放及其调用 generation；模板选择不等于准入结果。技能 Root、Tree-only、多 Timeline 和嵌套子图 MUST正常工作，不能用缺少唯一 Timeline 阻止技能试验。

#### Scenario: 移动中试验攻击

- **WHEN** 作者使角色正式移动并通过合法输入请求攻击
- **THEN** 工作区 MUST观察同一角色的 Gameplay Action、当前 Base Pose、Slot 与最终表现
- **AND** MUST不另外配置 Base Pose fixture 或直接播放攻击资源

#### Scenario: Tree-only技能执行完成

- **WHEN** 无 Timeline 的技能经正式准入执行并完成
- **THEN** 工作区 MUST显示该 ActionInstance 的 Root/局部执行与正式终态
- **AND** MUST不创建空 Timeline、动画 producer 或第二个 SkillInstance

#### Scenario: 相同技能实例失效

- **WHEN** 窗口绑定的释放已终止或场景 generation 改变
- **THEN** 窗口 MUST显示该实例终态或绑定失效
- **AND** MUST不自动用同 SkillDefinition 的另一次释放填充原窗口
