# 技能作者迁移盘点

## 2026-09-09 公开能力补充对账

本节为源码对账，不表示 Corin 资产已经迁移。读取 `BtsmtlGraphAuthoringCapabilities` 构造注册：48 项显式 Register 加 1 项 ExposedProperty，共 49 项；其中 12 项 AI 专用能力不在本 change 内。其余 37 项按下表处理，含资产里没有出现的能力。

| 原公开 kind | 原生技能对应与处理 |
|---|---|
| `@root`、`@enter`、`@exit`、`@any`、`@onEnter`、`@onExit`、`@result` | 对应原生系统锚点，保留页面角色限制，不能在普通创建菜单单独新增 |
| `@timelineEnter` | 根据原 Hook 拆为 `@timelineEnable`、`@timelineDisable`、`@timelineDestroy`；继续分别编译对应生命周期入口 |
| `state-machine`、`state`、`sequence`、`selector`、`parallel`、`loop`、`succeed` | 均进入原生目录及原操作发射；组合步骤使用 stable slot Flow 输出，不能把原生 Flip Flop 当作顺序等待 |
| `timeline` | 原生 Timeline 节点引用正式 TimelineAsset，TreeClip 进入原生 TimelineBody 编译 |
| `activate-action-instance` | 从技能作者目录移除；旧 `CharacterSemanticEmitter` 在技能深度下已经跳过该操作，运行继续由 ActionSkillExecutionRuntime 激活，迁移重定向边而不重新激活动作 |
| `submit-action-lifecycle`、`action-context-active`、`action-window-active`、`can-activate-action` | 对应原生业务节点及原 Action 操作；窗口查询仍核对当前调用及执行阶段 |
| `character-action-request`、`character-input-bool`、`character-input-float`、`character-input-vector2`、`character-input-vector2-magnitude`、`character-move-facing-angle` | 全部保留，读取稳定 Input identity 或正式操作输入 |
| `pipeline-blackboard-bool`、`pipeline-blackboard-float`、`exposed-property` | 保留声明 ID 和 owner ID；ExposedProperty 的 Get／Set 是同一 kind 的两种访问模式，按声明类型投影端口 |
| `state-root-completed`、`state-exit-cause` | 从编译状态及具体状态机 owner 读取结果，不依赖作者图 runtime |
| `locomotion-input-motion` | 原生 Locomotion 节点通过共享只读参数接口进入已有 Motion emitter，不另建位移执行器 |
| `and`、`or`、`not` | 复用原生 AND／OR／NOT；原生 `a/b/value` 显式映射到 Program 的输入 ID |
| `compare` | 显式展开 float 和 int 的相等、不等、小于、小于等于、大于等于、大于共 12 个原生比较种类；保留比较操作 variant 与类型约束 |

AI 排除项完整名单：`ai-read-self`、`ai-enumerate-candidates`、`ai-select-nearest-candidate`、`ai-read-target-distance`、`ai-read-target-direction`、`ai-read-target-snapshot`、`ai-read-memory`、`ai-write-memory`、`ai-write-continuous-input`、`ai-write-action-target`、`ai-submit-action-request`、`ai-wait-ticks`。这些节点继续属于 AI 域，不通过技能目录暴露。

本批新增原生 Macro 调用、输入与输出锚点，统一注册在 `BtsmtlSkillNodeCatalog`。技能原生类型共 53 个：旧技能能力去掉二次激活，Timeline Hook 拆分，比较展开，Get／Set 分开类型，再加入 Macro 三种类型。没有因 Corin 未使用而漏掉 Loop、Parallel、NOT 或 Locomotion。

固定端口以节点 `RegisterPorts` 声明为准，Document 读取同一原型声明，编译用 `CharacterGameplayValuePortContracts` 校验输入／输出数量、ID 和值类型。组合步骤、Macro 参数及黑板访问按显式文档数据投影动态端口；Flow 输出和值输入为单连接，Flow 输入和值输出为多连接。动态端口代码进一步合并、全部人工编辑的 typed Mutation 接入及正式构建报告仍归任务 2.2.3、2.3 和 4，不由本盘点代替。

目录导出不发布原型构造器随机生成的示例步骤 ID，避免重复 checkout 因临时 slot identity 产生不同 context hash。Macro 参数预检复用 `BtsmtlSkillMacroInterface`，同侧重名、重复 ID、多个执行入口和控制输出在写资产前拒绝。

以下为 2026-09-08 的资产基线和当时调用链记录。

2026-09-08静态读取精确Corin Definition及其引用资产，没有编译、Build或Unity资产写入。此表用于确定迁移输入，不作为运行验收。

## 精确入口与资产闭包

| 技能 | 当前入口identity | 根文件中可达图数据 | 可达节点 | 内联Timeline数据 |
|---|---|---:|---:|---:|
| Attack | 3ae19d5e-dd52-4f44-a80d-e32d2474e7ec | 68 | 419 | 4 |
| DodgeBack | ee909991-5be0-4961-838a-a2854baca30d | 9 | 61 | 1 |
| DodgeForward | b2328afd-40a8-467d-91f8-784c461f6137 | 9 | 61 | 1 |

读取源为`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`与其当前`Graphs/CorinPlayableRootTree.asset`。以上按技能入口的managed-reference闭包统计，不包含角色根，不意味着迁移后仍保留RootTree入口。角色实例仍是运行观察入口，Definition仅定位当前资产数据。

Attack还引用`Graphs/SharedTimelines/CorinAttack1Timeline.asset`，GUID为`be588770448d17444828b566954e2709`。该共享资源含4份图数据、20个节点：RootNode 4、TimelineEnterNode 12、ExposedPropertyNode 4；没有进一步共享图／Timeline引用。不能只迁根文件而漏掉该资源。

## 当前资产实际节点类型与去向

| 类型／分组 | Attack | DodgeBack | DodgeForward | 迁移处理 |
|---|---:|---:|---:|---|
| RootNode | 21 | 3 | 3 | 编译入口锚点；不提供Character Root Tree作者入口 |
| SequenceNode | 1 | 1 | 1 | 独立步骤Flow端口；保留等待完成的Sequence语义 |
| SelectorNode | 5 | 1 | 1 | 独立候选Flow端口；保留失败后选择语义 |
| SucceedNode | 5 | 1 | 1 | 保留技能完成结果 |
| StateMachineNode／StateNode | 1／5 | 0 | 0 | 技能局部状态和规则页面，不迁回C#角色总状态机 |
| StateMachine Enter／Any／Exit | 各1 | 0 | 0 | 结构锚点，领域规则控制创建和删除 |
| StateOnEnter／StateOnExit | 各6 | 各1 | 各1 | 保留进入和退出执行关系 |
| StateRootCompletedNode | 5 | 0 | 0 | 从本次技能调用状态读取 |
| StateExitCauseInfoNode | 25 | 5 | 5 | 保留退出原因条件 |
| TimelineNode | 5 | 1 | 1 | 调用原Timeline内容，保留owner、完成和中断顺序 |
| TimelineEnterNode | 45 | 6 | 6 | 包含TreeClip图入口，不以普通Update替代 |
| ActivateActionInstanceNode | 5 | 1 | 1 | 现技能编译明确跳过这些节点；迁移不得恢复二次激活，按正式边重定向语义清理 |
| SubmitActionLifecycleTransitionNode | 20 | 4 | 4 | 继续提交当前ActionInstance生命周期 |
| ActionContextActiveInfoNode | 20 | 4 | 4 | 读取当前执行上下文 |
| ActionWindowActiveInfoNode | 28 | 3 | 3 | 读取正式动作窗口，不另建预览窗口状态 |
| CanActivateActionInfoNode | 18 | 2 | 2 | 保留准入判断，不能自行发起第二次激活 |
| CharacterActionRequestInfoNode | 18 | 2 | 2 | 读取正式请求输入 |
| CharacterInputVector2MagnitudeInfoNode | 10 | 1 | 1 | 稳定输入identity到数值输出 |
| PipelineBlackboardFloatInfoNode | 10 | 1 | 1 | 保留声明identity和作用域，不能按显示名查找 |
| ExposedPropertyNode | 16 | 3 | 3 | 按声明类型注册原生值端口，保留默认值与作用域 |
| CompareNode | 10 | 1 | 1 | 保留比较方式和两个输入的数值类型约束 |
| AndNode／OrNode | 71／14 | 10／3 | 10／3 | 可复用原生外观与端口，语义进入已有操作合同 |
| ConditionRuleResultNode | 46 | 6 | 6 | 条件图返回锚点，不能调用行为或Timeline |

未在上述资产出现但当前能力目录存在的Loop、Parallel、Not、Bool／Float／Vector2输入、MoveFacingAngle、其他合法黑板类型和LocomotionInputMotion仍须迁移其公开能力，不能以Corin没有使用为由删掉。AI专用节点不迁移；Camera、Equipment、GE等额外发射器须按真实技能可达引用核对，不因在全局注册表存在就开放进技能菜单。

## 唯一端口和字段来源

- 作者kind、字段可写性、role、固定／条件／动态端口：`BtsmtlGraphAuthoringCapabilities`及共享Port Shape；不在本文复制另一份可维护schema。
- 编译值合同：`OperationValuePortContracts.cs`。输入读取使用`m_Output`；Compare使用`m_InputValue1/m_InputValue2/m_Result`；And／Or使用`m_Input1/m_Input2/m_Output`；条件返回使用`m_Result`。这些是当前编译端口身份，不应因显示文案变化而丢失映射。
- 系统Root、状态入口／出口、Timeline入口和条件返回均有保留anchor；Macro接口和组合步骤slot是不同来源，不混用数组index作为稳定身份。
- 新组合节点遵守原生一输出一连线，每步骤独立slot，原技能执行顺序由slot顺序发射；原生Sequence类实际是Flip Flop，不直接映射为技能Sequence。

## 编译和运行调用者

`CharacterSkillCompilationDiscovery`发现技能及调用关系；`CharacterAuthoringGraphOccurrence`、GraphReference及TreeClip记录仍依赖旧图对象；`CharacterSemanticEmitter`编译图和边；Core／Input／Action／Motion注册模块读取业务字段；`CharacterSemanticSkillProgramEmitter`发布技能目录；`CharacterSimulationOperationEmitter`写入唯一Program Builder。下一阶段应替换作者读取与遍历，不能把旧Node实例装进新FlowNode。

运行继续由ActionSkillExecutionRuntime及现有Session／Numeric Program拥有，观测由RuntimeDiagnosticsTargetRegistry、RuntimeDebugSession与RuntimeDebugViewBinding提供。本表未声明已完成原生作者模型、Macro编译或资产迁移。
