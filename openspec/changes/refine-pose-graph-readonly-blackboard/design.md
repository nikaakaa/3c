# Design: PoseGraph只读Blackboard与公共输入 r2

日期：2026-09-13。规划修订：r2。来源：用户广播`2026-09-13-authoring-r2-plan`，公共基线为[原生C# authoring r2](../remove-agent-authoring-use-native-csharp/design.md)。本轮仅改规划，不修改业务代码、资产或发布产物，不发送执行消息。

本目录由规划窗口`01a09594-1751-7512-b8c0-08b04185055b`维护。原Decision 26及25.x已经独立拆入本change；旧架构任务不重新并入。现有1.4、1.5、1.6完成状态保留，新r2行为以第2组未完成任务表达，历史代码与检查记录仍在execution.md，不把历史Document实现写成r2已经完成。

## 1. 业务目标与输入输出

作者在PoseGraph找到当前图可读取的变量、事实和公开入参，拖出稳定Get并用于混合或条件；Body内明确读取输入姿势的脚权重曲线。改变作者名称不破坏引用，代码重建图后仍绑定同一业务对象。

| 输入类别 | 唯一来源 | 本任务输出/消费 |
|---|---|---|
| 动画实例变量 | 原生EventGraph声明、更新及唯一Contract/Layout/Frame | 只读Blackboard、Get、条件与BlendSpace句柄，不复制声明和布局 |
| 角色表现事实 | 既有同帧Presentation Fact合同 | 保留Fact类型与权限，包括原Identity事实；拒绝Gameplay可变地址 |
| 子图公开入参 | Subgraph/Linked Pose的正式调用接口 | 按调用作用范围绑定，不复制为全局动画变量 |
| source-local曲线/动画属性 | 指定Pose的采样、混合和惯性响应 | 保留曲线列和最终属性消费者，不由EventGraph Set |
| 固定配置/资源 | 正式Pose类型化字段、节点与资源owner | 原生Inspector、正式配置API及公共C#输出薄适配 |

输出包括同一声明的只读界面、typed读取和消费签名、完整对象读取/配置能力与准确诊断。Pose继续拥有状态机、采样、曲线、混合、IK、原Compiler/Program、独立动画编译和最终写入；普通共享变量只读不代表Pose节点没有状态。

## 2. 公共所有权与具体调用迁移

| 所有者 | 本轮唯一负责的内容 |
|---|---|
| 事件图任务add-flowcanvas-event-graph | HostEventGraph及其原生操作API、变量声明/更新、EventGraphVariableReference、CharacterAnimationVariableContract/Layout/Frame和发布/租约语义 |
| 本任务 | CharacterPoseGraphAuthoringAdapter.cs、Pose类型化Mutation、CharacterAnimationInputContract、Get/条件/BlendSpace、运行与Preview输入消费文件和Pose正式读取/配置能力 |
| C# authoring任务 | export_code/generate_assets两个显式MCP、公共对象到代码输出、生成入口及Agent Mapper/DTO/协议退役；输出薄适配消费正式Pose能力 |

`CharacterPoseGraphAuthoringAdapter`已不再拥有或执行事件图内容 Mutation；Pose 只通过 Profile 的正式事件图引用消费唯一 Contract/Layout/Frame。事件图内容由 `HostEventGraph` 原生 API 和公共 C# authoring 入口负责，Pose Mutation 不携带 Agent DTO 或替代 Document 模型。

保留正式Pose类型化修改、Node Definition、Port Shape和领域校验。旧Agent Document、五生命周期、专属Validator、Exporter/Reconciler与反向导出不再成为创建、保存、导出或生成的前置条件。只存在旧Agent层的必要业务规则归回现有Pose修改或编译入口，不增加中央Validator、整包同步事务、反射私有字段或第二Pose模型。

## 3. 唯一变量合同与输入装配

共享变量引用使用同一图业务ID和原生Variable.ID；显示名不参与寻址。Float、Int32、Bool保持精确类型，Int32/Bool不通过float中转。Contract/Layout/Frame由事件图唯一生成，Pose输入合同只能持有其引用及消费者绑定，不能重新排列另一份全局变量列或复制初值作为运行来源。

CharacterAnimationInputContract保留正式Fact、Slot、World能力、子图调用入参和source-local曲线。共享实例变量部分引用事件图合同和唯一布局；Curve与Fact不能因为同名或同为Float被合并进事件图变量。当前root.Parameters的遗留用途按真实消费者迁移，不先清空再补默认motor值。

独立Pose编译只需要合法动画根、资源、Rig、共享变量合同和其它正式输入声明，不要求运行事件图、先编译SkillGraphs或创建场景角色。编译输出固定变量引用和类型匹配的消费句柄；角色装配再提供实际实例和同次发布的变量帧。

## 4. 同次发布、类型、作用范围与条件

Get、Transition Rule和BlendSpace必须消费同次成功发布的typed变量帧。该帧带动画实例/Actor、表现采样、Simulation sample tick、Reset代际及图/合同/layout版本；读取前在现有交接边界检查一致性。事件图消费者完成前不能重写输出，Pose/Worker只读取冻结typed值，不持有可变Blackboard Variable对象。

Source Pending不回退已成功更新的事件图变量或原生节点状态。下一次更新按正式表现时间继续，不能重放旧更新。Pose按自身Pending/Committed和Fault边界执行，输入发布成功不能作为最终姿势已提交、已显示或Writer成功的证据。观察明确区分原生更新、变量输入发布与最终Pose提交。

Transition Rule新增同次动画变量读取，同时保留同帧Fact、TimeInState、StatePoseRemainingTime、短路求值、priority、stable order和MaxTransitionsPerFrame。变量Get不是写入口；禁止读取Gameplay Blackboard mutable address、ActionInstance、Timeline operation、Unity Transform或World query。

Root、StatePose、Subgraph、Linked Pose只展示接口允许的输入；共享变量不在每张图重建声明，子图入参不变成全局可写变量。合法未使用声明可以标记/筛选，但不能因暂未连线删除。主要显示名称、类型、来源、范围和使用情况，内部身份留在详情诊断。

## 5. Body曲线与已有实现保护

Body内FootPlacement Weight读取指定输入Pose经过上游正式混合后的曲线值。有真实外部控制需求时使用明确公开输入，不按节点名称或固定字符串猜来源。曲线绑定和通用曲线读取可以是不同作者表达，但必须进入同一曲线来源与执行语义，不能各自采样。

保留现有Foot、Pelvis、FBBIK和曲线数学；脚权重只控制既有Goal可见权重，不释放Anchor、清零连续历史或改变Landing Reach准入。已有输入Pose曲线门禁、显示名修正和属性声明拆分不回退。本轮r2只改变协议和消费接口规划。

## 6. 完整C#导出、生成与业务身份

两个公共操作由C# authoring任务实现：`btsmtl.export_code`读取当前资产完整导出C#，`btsmtl.generate_assets`执行对应已编译代码，重建并保存明确生成范围。本任务提供Pose对象的正式读取与配置API，公共输出适配使用这些API，不先构造Agent JSON、Source Snapshot或第二份节点模型。

完整往返覆盖图角色、状态/规则、节点类型和字段、Get稳定变量引用、完整曲线键/切线/权重/WeightedMode/wrap、布局、固定/条件/动态端口、端口顺序、资源和跨对象引用。未知字段、资源或端口不能静默省略；输出完整性由输出服务检查，业务有效性仍由现有Pose领域规则检查。

图、节点、变量和端口的业务ID在重建后保持一致；物理Unity对象、GUID和local file ID可以变化。生成范围内引用必须使用本次创建的新对象，Get仍绑定同一图/变量业务引用；真正范围外资源通过明确参数或正式资源身份提供。Profile/Definition的明确根挂接必须恢复，不能只生成孤立图，也不能依赖旧生成子资产GUID找回范围内内容。

人工编辑/保存不自动写源码。重新生成不自动合并未导出的人工修改，不新增rebase、源码同步或增长日志。生成范围外资产不被扫描删除，现有Undo与保存机制保持。变更按既有依赖失效显示Stale；独立动画Compile和精确Character Build另行显式调用，export_code/generate_assets不自动Build。

## 7. 运行与Preview消费迁移

完整Pose/角色Preview使用同一CharacterAnimationEventGraphHost和唯一变量Contract/Layout/Frame，携带同一实例、采样、tick、Reset与版本身份，不创建固定motor桥或Preview私有变量更新器。普通Play观察仍只消费真实角色和已发布事实，不取得时钟或执行权。

单AnimationClip/BlendSpace等资源查看使用原正式资源调参合同，不能为缺少角色变量帧补默认motor值，也不强迫单资源工具构造完整角色/事件图。两类入口按业务目的区分；不得以保留CharacterPresentationProgramParameterFrame.FromDirect作为资源Preview长期兼容路径。

本任务已迁移CharacterSimulationPresentationRuntime、Pose帧协调、Program/StateSource/BlendSpace、条件/Get及AnimationPreviewEngine等全部旧消费签名；旧 `CharacterPresentationProgramParameterFrame` 及其旧生产方法已无业务代码引用并完成删除。执行记录保留精确迁移文件清单和剩余引用。

## 8. 执行顺序与完成定义

1. 消费事件图唯一公共变量类型和原生操作API，明确本任务拥有的Adapter、Mutation和输入签名。
2. 删除Pose对Agent Mapper/DTO的依赖，保留正式领域修改和校验；接通输入合同中的共享实例变量引用。
3. 迁移Get、条件、BlendSpace以及运行和完整Preview到同次typed帧，保留资源查看的原合同。
4. 补齐Pose对象完整读取/配置、稳定业务ID和新对象引用恢复，供公共代码输出/生成使用。
5. 迁移完消费签名后，由事件图任务删除旧生产类型；C# authoring任务退役Agent协议。三者通过正式文档和代码依赖对齐，不建立临时接口。
6. 在明确生成范围中清理废弃声明、端口与无引用重复子图，恢复根挂接；既有Build单独发布产物。

设计/证据条件：两实例隔离；精确类型和跨范围错误可定位；同次Get/条件/BlendSpace输入一致；Source Pending不回退事件状态；完整Preview与运行同合同；导出/删除重建后业务ID、曲线、布局、动态端口、资源和根挂接等价；旧Agent调用和固定motor消费签名消失。条件留在本设计与执行证据，不新增测试或手动验证tasks。

## 9. 现行规范与旧方案对接

- 现行Transition仅允许Fact/时间的条款在本change delta中补充typed动画变量帧；Gameplay mutable address禁令、时间范围和状态机选择规则保留。
- 旧committed parameter措辞细分为输入发布与Pose提交，不能让Source Pending反向改写事件图状态。
- 原生EventGraph执行已按用户广播确定，不再保留“待选择原生/编译两种模式”的规划阻塞。事件图具体API仍由其唯一维护者提供，不在本任务设计第二套合同。
- 旧`integrate-pose-flowcanvas-editor-preview`的公共输入与authoring协议条款由本轮对接，旧任务其它作者组织、资源、Action/Slot、IK和独立编译业务保持原归属。目录内未发现另一明确planner身份；本轮依据用户指定只修改相关规划条款，实施记录与原代码不改。
- 旧方案中Document v7升级、整包同步和反向导出delta退出待实施规范。Agent能力删除及共享目录的Document要求退役由C# authoring change唯一拥有；旧Pose方案不再次修改已退役Requirement或重新安装协议。
- 其它窗口尚在更新的公共条款按本广播及C# authoring r2解释；例如笼统“领域图都必须编译”不能重新施加到原生事件图。若相关文件仍残留旧句，记录其所有者和依赖，不修改其它规划窗口文件。

## 10. 本轮记录

本轮只改本目录规划及旧Pose方案的公共对接条款，不改业务代码/资产，不向任何实现、规划或协调窗口发送回执、进度或执行消息。已有实现证据仍留在execution.md；其中Document路径是当时事实，不作为r2后续执行入口。
