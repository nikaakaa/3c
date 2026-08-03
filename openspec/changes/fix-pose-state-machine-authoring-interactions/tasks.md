# Tasks

## 1. 现状与合同对账

- [x] 1.1 固定`TreeRectangleSelector`当前`BaseTreeView`类型门禁和事件入口。
- [x] 1.2 固定`GraphAuthoringCanvasView`、`BaseTreeView`和Pose窗口的继承与装配关系。
- [x] 1.3 固定BTSMTL Graph、Pose Graph、Gameplay StateMachine与Pose StateMachine当前框选调用链。
- [x] 1.4 固定`CharacterPoseStateMachinePolicy.PersistsLayout=false`到State View移除`Movable`的调用链。
- [x] 1.5 固定Entry、State与Alias当前投影位置来源。
- [x] 1.6 固定共享StateMachine surface当前只提交State移动的缺口。
- [x] 1.7 固定Pose StateMachine人工Mutation、Document Reconciler和资产owner入口。
- [x] 1.8 固定Document v3当前Pose StateMachine单文件闭包。
- [x] 1.9 枚举全部正式Pose Graph资产和其中的Pose StateMachine identity。
- [x] 1.10 对账旧实施清单中框选、移动和StateMachine表面已完成声明。

## 2. 收口共享框选器

- [x] 2.1 把`TreeRectangleSelector`目标从`BaseTreeView`具体类型改为共享GraphView能力。
- [x] 2.2 把空白画布起点判断改为共享GraphView、content view和content container判断。
- [x] 2.3 保留GraphElement及其子元素不能启动框选的约束。
- [x] 2.4 保留IMGUI容器不能启动框选的约束。
- [x] 2.5 保留普通框选先清空selection的语义。
- [x] 2.6 保留Shift或Action键增选的语义。
- [x] 2.7 保留仅命中可见且具备`Selectable`能力元素的过滤。
- [x] 2.8 保留矩形视觉层、鼠标捕获和捕获丢失清理。
- [x] 2.9 删除框选器中剩余的`BaseTreeView`类型依赖。
- [x] 2.10 确认没有增加Unity内置`RectangleSelector`或Pose专用选择器。

## 3. 定义Pose StateMachine layout作者模型

- [x] 3.1 定义Pose StateMachine layout稳定owner identity。
- [x] 3.2 定义StateMachine layout element的稳定identity与二维有限坐标。
- [x] 3.3 定义根Pose资产中的StateMachine layout catalog。
- [x] 3.4 定义按`PoseStateMachineId`读取唯一layout的正式API。
- [x] 3.5 定义空显式layout的合法语义。
- [x] 3.6 定义Entry缺失显式位置时的确定性位置。
- [x] 3.7 定义State按稳定identity生成位置的确定性规则。
- [x] 3.8 定义Alias按稳定identity生成位置的确定性规则。
- [x] 3.9 定义显式位置覆盖生成位置的规则。
- [x] 3.10 拒绝重复layout element identity。
- [x] 3.11 拒绝layout引用未知Entry、State或Alias。
- [x] 3.12 拒绝NaN和Infinity坐标。
- [x] 3.13 确认Transition edge不保存layout元素。
- [x] 3.14 确认layout字段不进入Pose StateMachine运行合同。
- [x] 3.15 确认layout变化不修改StateMachine `ContentRevision`。

## 4. 完成共享StateMachine移动分类

- [x] 4.1 为共享Entry View暴露稳定移动identity。
- [x] 4.2 为共享State View保留稳定移动identity。
- [x] 4.3 为共享Alias View暴露稳定移动identity。
- [x] 4.4 从`GraphViewChange.movedElements`分类Entry移动。
- [x] 4.5 从`GraphViewChange.movedElements`分类State移动。
- [x] 4.6 从`GraphViewChange.movedElements`分类Alias移动。
- [x] 4.7 为每个移动元素生成唯一`MoveElement`请求。
- [x] 4.8 在不持久化layout的domain policy下继续拒绝移动写入。
- [x] 4.9 在Mutation只读时清空全部移动请求。
- [x] 4.10 保持State双击打开State Pose Graph的手势。
- [x] 4.11 保持Transition双击打开Transition Rule的手势。
- [x] 4.12 确认共享移动分类不读取Pose或BTSMTL业务类型。

## 5. 接入Pose StateMachine类型化layout Mutation

- [x] 5.1 定义Pose StateMachine layout移动Mutation。
- [x] 5.2 让`CharacterPoseStateMachineMutationAdapter`降低Entry移动。
- [x] 5.3 让`CharacterPoseStateMachineMutationAdapter`降低State移动。
- [x] 5.4 让`CharacterPoseStateMachineMutationAdapter`降低Alias移动。
- [x] 5.5 让layout Mutation只修改根Pose资产layout catalog。
- [x] 5.6 让单次多选拖动进入同一Undo事务。
- [x] 5.7 让新建State同时接收初始位置。
- [x] 5.8 让新建Alias同时接收初始位置。
- [x] 5.9 让删除State同时删除对应显式位置。
- [x] 5.10 让删除Alias同时删除对应显式位置。
- [x] 5.11 保持Entry不可删除但允许移动。
- [x] 5.12 把Pose StateMachine policy切换为正式持久化layout。
- [x] 5.13 让Pose StateMachine document从layout owner投影Entry位置。
- [x] 5.14 让Pose StateMachine document从layout owner投影State位置。
- [x] 5.15 让Pose StateMachine document从layout owner投影Alias位置。
- [x] 5.16 确认移动后只刷新authoring和dirty状态。
- [x] 5.17 确认移动不调用Projection Compile或Character Build。

## 6. 扩展Document v3 StateMachine layout分片

- [x] 6.1 定义`AgentPackagePoseStateMachineLayoutFile`。
- [x] 6.2 定义稀疏layout element JSON模型。
- [x] 6.3 固定`stateMachineId`字段和canonical字段顺序。
- [x] 6.4 固定layout element按稳定identity排序。
- [x] 6.5 把每个Pose StateMachine目录扩展为`state-machine.json + layout.json`文件对。
- [x] 6.6 更新Presentation package路径解析。
- [x] 6.7 更新manifest精确文件闭包生成。
- [x] 6.8 更新checkout canonical writer。
- [x] 6.9 更新strict parser拒绝未知字段。
- [x] 6.10 更新strict parser拒绝重复元素。
- [x] 6.11 更新strict parser拒绝未知元素identity。
- [x] 6.12 更新strict parser拒绝非有限坐标。
- [x] 6.13 允许`elements`为空或只覆盖部分合法元素。
- [x] 6.14 把layout semantic hash纳入editable hash。
- [x] 6.15 把layout文件路径纳入document hash。
- [x] 6.16 把layout变化纳入DocumentDirty与Conflict判断。
- [x] 6.17 让旧闭包缺少layout文件时返回明确重新checkout诊断。
- [x] 6.18 确认没有增加旧闭包reader、补文件fallback或v3双形状apply。

## 7. 贯通Exporter、Reconciler与Validator

- [x] 7.1 让Presentation Exporter为每个Pose StateMachine输出layout分片。
- [x] 7.2 让Exporter输出空显式layout而不是伪造已保存位置。
- [x] 7.3 让Presentation Package Codec关联同目录语义文件与layout文件。
- [x] 7.4 让Reconciler比较StateMachine layout目标与正式owner。
- [x] 7.5 让纯layout差异降低为Pose StateMachine layout Mutation。
- [x] 7.6 让结构新增与layout新增按owner依赖排序。
- [x] 7.7 让结构删除与layout清理按owner依赖排序。
- [x] 7.8 让Mutation preflight解析StateMachine、元素identity和layout owner。
- [x] 7.9 让Validator检查layout的StateMachine owner唯一性。
- [x] 7.10 让Validator检查显式元素引用合法性。
- [x] 7.11 让Validator检查坐标有限性。
- [x] 7.12 让Application Service把layout owner纳入同一资产级事务。
- [x] 7.13 让apply成功后的reverse export发布canonical layout文件。
- [x] 7.14 让apply失败同时回滚StateMachine语义与layout变化。
- [x] 7.15 确认layout apply不发布Program、Projection或Native Pose Program。

## 8. 收口画布与现有资产工作流

- [x] 8.1 保持中央Graph区域统一拉伸全部直接子画布。
- [x] 8.2 确认Root PoseGraph与StateMachine切换不创建第二套GraphView实现。
- [x] 8.3 确认Pose StateMachine打开时Entry、State、Alias使用正式layout投影。
- [x] 8.4 确认无显式layout的现有StateMachine使用唯一确定性排布。
- [x] 8.5 确认首次拖动只新增被移动元素的显式位置。
- [x] 8.6 通过显式checkout刷新Corin Document v3 StateMachine文件闭包。
- [x] 8.7 读取刷新后的Corin `state-machine.json`与`layout.json`确认owner identity一致。
- [x] 8.8 对Corin Document执行dry-run并确认无业务语义diff。
- [x] 8.9 仅在layout目标确有差异时通过同hash apply提交正式layout。
- [x] 8.10 apply后重新checkout确认Document回到Clean。
- [x] 8.11 不执行Character Build，因为纯layout不改变generated产品。

## 9. 删除错误路径与同步文档

- [x] 9.1 删除框选器中的`BaseTreeView`具体类型门禁。
- [x] 9.2 删除Pose StateMachine按数组序号直接构造最终位置的唯一来源。
- [x] 9.3 删除Pose policy固定禁止layout持久化的旧配置。
- [x] 9.4 确认没有EditorPrefs、window-local节点位置或Pose专用layout缓存。
- [x] 9.5 确认没有Pose专用框选Manipulator。
- [x] 9.6 确认没有selection、窗口打开、AssetDatabase refresh触发自动保存或Build。
- [x] 9.7 更新`btsmtl-agent-authoring`当前合同的Presentation目录结构。
- [x] 9.8 更新`btsmtl-agent-authoring`技能的Pose StateMachine editable路径。
- [x] 9.9 更新旧实施清单中框选、移动和layout承接状态。
- [x] 9.10 对账current specs与实现后的唯一交互链。

## 10. 静态收口

- [x] 10.1 搜索并确认`TreeRectangleSelector`不再依赖`BaseTreeView`。
- [x] 10.2 搜索并确认Pose StateMachine只有一个layout owner。
- [x] 10.3 搜索并确认人工UI与Document Reconciler使用同一种typed layout Mutation。
- [x] 10.4 搜索并确认Compiler与Runtime不读取StateMachine layout。
- [x] 10.5 搜索并确认没有新增自动Compile或自动Build调用。
- [x] 10.6 检查全部新增和修改C#文件的程序集所有权。
- [x] 10.7 检查全部新增Unity文件的`.meta`配对。
- [x] 10.8 执行OpenSpec严格校验并修复全部诊断。
