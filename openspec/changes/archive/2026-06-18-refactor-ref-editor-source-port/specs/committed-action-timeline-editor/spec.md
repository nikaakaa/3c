## ADDED Requirements
### Requirement: Timeline Editor 必须源码级替换为 Ref 组件结构
Committed Action Timeline Editor MUST 将当前半移植 / 自研混合的 timeline shell 替换为 Ref/Taco Timeline editor 的源码级等价组件结构。实现可以使用项目命名和项目 adapter，但 MUST 明确提供 Ref 等价的 field view、track view、track handle、clip view、drag manipulator、drag line manipulator、selection、locator、frame position map、move leader、apply move、resize clamp、rectangle selection、pan、zoom、focus 和 context menu 职责。旧的 root pointer mode 推断、局部 frame delta 拼接、card/list timeline 伪编辑面或临时 fallback UI MUST NOT 作为正式编辑路径保留。

#### Scenario: Clip 拖拽与伸缩由独立 manipulator 负责
- **WHEN** 设计者在 timeline 中拖动 clip 主体
- **THEN** move drag manipulator MUST 将开始、移动和结束事件委托给 field view 的 move leader / apply move 流程
- **AND** 该流程 MUST 通过正式 adapter 写回 selected TimelineNode 的 seconds authoring 数据
- **WHEN** 设计者拖动 clip 左右边缘
- **THEN** left resize 和 right resize MUST 使用独立 drag line manipulator 或批准等价结构
- **AND** resize MUST NOT 依赖 root clip pointer mode 猜测

#### Scenario: Field View 持有坐标和 selection 权威
- **WHEN** 设计者 pan、zoom、拖动 locator、框选或多选移动 clip
- **THEN** field view MUST 持有 frame / tick position map、scale、offset、selection、move leader 和 move validation
- **AND** clip view MUST NOT 保存第二套 timeline 权威数据
- **AND** 所有写回 MUST 进入正式 timeline serialized adapter

#### Scenario: 旧半移植交互路径被移除
- **WHEN** 检查 Timeline Editor 实现
- **THEN** 当前被替换的半自研交互 path MUST 删除或不可达
- **AND** MUST NOT 同时存在 Ref-equivalent manipulator path 与旧 root pointer delta path 两套可编辑路径
- **AND** MUST NOT 存在隐藏 fallback 配置来选择旧 timeline editor

### Requirement: Timeline Editor 保持项目数据与运行时边界
源码级移植后的 Timeline Editor MUST 只作为 Editor-only Presentation Layer。UI 内部可以使用 Ref 风格 frame / tick 位置映射，但正式 authoring 字段 MUST 继续使用 seconds，compiler MUST 继续执行 seconds authoring -> fixed tick compile -> runtime tick sampling。正式 runtime MUST NOT 保存或执行 Ref `Timeline`、`Track`、`Clip`、`TimelinePlayer`、PlayableGraph runner 或 Taco asset。

#### Scenario: Ref 数据模型被项目 adapter 替换
- **WHEN** timeline field、track 或 clip view 需要读取或修改数据
- **THEN** 它 MUST 通过项目 timeline editor snapshot、serialized adapter 或批准等价 adapter 访问 `CharacterActionDefinitionSO`
- **AND** MUST NOT 直接持有 Taco `Timeline`、`Track` 或 `Clip` 作为正式保存对象

#### Scenario: Runtime 边界保持干净
- **WHEN** 运行静态边界测试
- **THEN** runtime source MUST NOT 引用 UnityEditor、GraphView、TimelinePlayer、PlayableGraph、Taco `BaseTree`、`RunnableTree`、`RunnableNode` 或 Ref editor view
- **AND** preview MAY 使用 Editor-only visual sampling，但 MUST NOT 成为 gameplay runner
