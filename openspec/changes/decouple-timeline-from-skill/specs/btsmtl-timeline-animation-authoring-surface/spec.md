## MODIFIED Requirements

### Requirement: Timeline Animation作者表面必须只拥有本地时间作者内容

Timeline Editor Core MUST只拥有时间几何、Track/Clip/注册本地曲线、外部输入声明、选择、交互和正式 Mutation/Undo 表面。具体片段字段 MUST由领域合同提供；生成分析、原生 AnimationClip 注册曲线、Character 配置、Projection 状态和 Runtime 对象不能成为时间核心依赖。无 Graph/Character 上下文时本地内容 MUST仍可完整编辑。

#### Scenario: 独立打开shared Timeline

- **WHEN** 作者从 Project 直接打开 shared Timeline 且没有 Graph/Character 上下文
- **THEN** MUST能编辑片段、TreeClip、本地曲线和内容需要的目标/参数声明
- **AND** MUST不显示 Sequence、Sync Marker 或素材曲线行

#### Scenario: 领域工具没有配置

- **WHEN** 当前内容没有适用领域工具
- **THEN** 主时间轴 MUST完整可用，不增加空工具行，也不反向搜索 Character/Profile

### Requirement: Timeline上下文必须区分作者、领域工具与Runtime Debug

本地作者上下文、领域工具输入和 Runtime Debug binding MUST保持独立合同。缺失只禁用依赖该合同的功能，不能从另一合同猜测补全。实际调用对象与播放状态 MUST不成为作者数据；诊断可以观察 Skill 或非 Skill 的正式播放，但必须显式选定来源与实例。旧 Marker topology 和 Sequence document context 不得恢复。

#### Scenario: 只有Runtime Debug binding

- **WHEN** Timeline 附着正式播放且没有 Character 作者上下文
- **THEN** MUST能够显示所选播放的只读状态
- **AND** Clip 导航目标和领域作者工具 MUST不从运行实例推断

#### Scenario: 只有本地作者上下文

- **WHEN** shared Timeline 没有运行绑定和领域工具输入
- **THEN** 片段与本地曲线 MUST可编辑，界面不得显示空分析或 Marker 区域

## ADDED Requirements

### Requirement: 作者必须只配置内容需要的目标和参数

作者表面 MUST按片段/树声明显示实际业务目标位置和 typed 参数；已配置资源与常量不得要求重复输入。执行接口、编译槽位、服务注册和内部状态 MUST由程序装配，不进入作者播放配置。缺失外部绑定或能力时 MUST定位到具体片段/节点并说明缺少的业务输入。

#### Scenario: 技能 Timeline 继承释放信息

- **WHEN** 作者在技能中编辑普通攻击 Timeline
- **THEN** MUST不要求额外手填当前角色、ActionInstance 或运行服务

#### Scenario: 独立 Timeline 声明表现目标

- **WHEN** 内容需要一个场景表现目标
- **THEN** 作者 MUST看到有业务名称和类型的目标声明
- **AND** 未选择实际运行对象不得阻止编辑本地时间内容

### Requirement: 片段创建和修改必须复用正式领域合同

菜单、拖放、粘贴、Details 和 Document MUST使用同一 Track/Clip 类型、字段、允许组合和重叠约束。变更 MUST进入现有正式 Mutation、Validator 与 Undo，不新增第二写入口。新增片段不应要求修改通用时间窗口的业务分支；重操作不得进入 Inspector 重绘、选中或窗口恢复。

#### Scenario: 粘贴不允许的片段

- **WHEN** 作者把不满足目标轨道合同的片段粘贴到轨道
- **THEN** MUST与构建/Document 相同地拒绝，原作者内容保持不变
