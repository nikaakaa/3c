## 1. 收回共享边界并保护BTSMTL体验

- [ ] 1.1 将Pose Preview、动画输入与动画专属区域从通用`BaseTreeWindow`布局合同中移除，保持BTSMTL Tree与AI现有Data Catalog、Graph、Details和Live Debug行为
- [ ] 1.2 从现有BTSMTL作者实现中明确提取可复用的Graph interaction core，使PoseGraph复用Canvas、selection、clipboard、Undo、breadcrumb、创建菜单和StateMachine表面而不复制生命周期
- [ ] 1.3 提取不依赖`BaseNode`序列化模型的Node Visual Chrome，使Pose Node、Pose State、Alias与Entry消费`BaseNode.uxml/.uss`和`NodePortContainer.uxml/.uss`
- [ ] 1.4 修复全部BTSMTL UXML/USS正式资源路径并删除Unity默认GraphView节点视觉路径，缺少正式资源时明确失败而不回退

## 2. 唯一PoseGraph入口与精确上下文

- [x] 2.1 保留`CharacterAnimationPresentationProfile`到现有`CharacterPresentationPoseGraphEditorWindow.Open`的直接入口
- [x] 2.2 让Open请求完整绑定Definition、Profile、PoseGraph、Projection、Rig、Source Bindings与Preview Fixture identity，并在任一项失配时返回typed Unavailable
- [ ] 2.3 删除Action Animation Workspace中的PoseGraph主入口、验收路径与相关提示，使Action、Timeline或call site歧义不再阻断PoseGraph
- [x] 2.4 删除按Scene、名称、目录、上次选择或裸`CharacterPipelineHost` ObjectField补全工作上下文的路径
- [x] 2.5 在target结束、Play退出、Projection替换或domain reload后只按稳定authoring identity重建上下文，不恢复旧runtime对象

## 3. 已有实时调参基础

- [x] 3.1 为动画作者字段建立`Structural`、`TunableDefault`、`RuntimeInput`与`DerivedReadOnly`唯一策略
- [x] 3.2 为Tunable字段建立typed value、范围、有限值、`NextFrame | NextActivation`、状态语义和consumer identity
- [x] 3.3 由Character Build生成固定`CharacterPoseTuningLayout`、layout hash与默认`CharacterPoseTuningParameterBlock`
- [x] 3.4 实现Editor完整candidate编译和Program、Projection、Pose Plan、Rig、Layout identity校验
- [x] 3.5 实现每目标一个Pending candidate与一个Active block，并在PresentationFrame Prepare前原子交换
- [x] 3.6 发布Applied Candidate Revision、Applied Frame、Authoring Revision与typed拒绝原因

## 4. Preview Fixture与正式Pose Plan

- [x] 4.1 定义editor-only`CharacterAnimationPreviewFixture`及其精确Definition、Profile、Rig和角色Prefab上下文
- [x] 4.2 在隔离editor Scene与明确PhysicsScene中建立Fixture Session，并复用现有`AnimationPreviewRuntime`、Projection、Pose Plan与solver链
- [x] 4.3 缺少Preview Environment时让world-aware节点报告typed Unavailable，不创建假平面或场景搜索fallback
- [x] 4.4 在target结束、Play退出、Projection替换或domain reload时清除Editor candidate与Fixture runtime绑定
- [x] 4.5 建立与Corin当前Definition、Profile、Rig、角色Prefab和发布Projection精确匹配的正式Preview Fixture资产
- [x] 4.6 将Grounded、Movement Mode、Speed、Direction及现有必要typed输入接入Fixture，并通过正式Presentation Fact/Parameter输入驱动PoseStateMachine
- [x] 4.7 将Play、Pause、Step、Seek与Restart接入同一个Preview Session；Restart只重置Preview有限状态
- [x] 4.8 让Fixture最终角色画面只显示正式Pose Plan的FinalPublication结果，不存在直接Clip播放或shadow solver

## 5. PoseGraph可用工作区

- [x] 5.1 在现有`CharacterPresentationPoseGraphEditorWindow`内组合命令状态、角色画面、图导航、唯一Canvas、selection Details和Preview typed输入，不新建PoseGraph窗口
- [x] 5.2 删除窗口中的重复Preview、Live、Diagnostics Dock、`Graph / Preview / Split`模式、裸target对象框和全量参数表
- [x] 5.3 让现有Root Pose Graph打开后自动定位可见内容，并保持作者保存的节点位置与缩放
- [x] 5.4 让Root Graph、root-owned子图和PoseStateMachine使用同一page stack与breadcrumb导航
- [x] 5.5 让小窗口只调整或折叠区域，不切换第二工作模式、不丢失selection或runtime target
- [x] 5.6 使用业务术语显示Node、State、Transition与Source Slot，默认隐藏GUID、hash、dense index和workspace offset

## 6. BTSMTL式PoseGraph编辑体验

- [x] 6.1 让空白处右键菜单从Pose Capability创建节点，并支持按业务名搜索
- [x] 6.2 让typed端口拖到空白处只显示兼容节点，并在Mutation前复用唯一connection policy
- [x] 6.3 跑通节点移动、框选、连接、断开、删除、复制、粘贴与Undo/Redo，全部写回现有PoseGraph真实owner
- [x] 6.4 跑通PoseStateMachine下钻、State graph下钻、PoseSubgraph下钻和breadcrumb返回
- [x] 6.5 让Transition只作为StateMachine edge和当前selection Details存在，删除Navigator中的Transition全宽按钮与第二selection来源
- [ ] 6.6 让Node、Port、Edge、selection、错误和运行状态统一使用BTSMTL视觉资产与交互，不保留inline近似样式

## 7. 同图运行状态与selection调参

- [x] 7.1 让RuntimeDebugSession或Preview snapshot按当前PoseGraph、Projection revision和frame completion绑定同一作者图
- [x] 7.2 在同一Canvas高亮当前State、target State、Transition edge/progress和执行Pose节点；revision失配时立即清除旧高亮
- [x] 7.3 让Details只显示当前Node、State或Transition的Authoring、Runtime Input、Applied、References和错误，不显示完整Tuning Layout
- [x] 7.4 在Tunable字段同行显示作者值、当前目标Applied值与`Live Now | Next Activation | Build Required | Read Only`状态
- [x] 7.5 让Tunable编辑通过正式typed Mutation保存唯一owner并进入Undo，不新增Apply、Reset、Debug Profile或Override Asset
- [x] 7.6 让Undo/Redo重新生成完整candidate，并在无target、target替换或revision失配时清空旧Applied值
- [x] 7.7 让Foot Placement中不改变容量的Grounding/Predictive数值与Full Body IK数值消费Active block
- [x] 7.8 让Blend、Layered、Additive、Modify Bone默认Weight、Sequence Play Rate和Transition数值消费Active block
- [x] 7.9 完成固定容量BlendStack与Inertialization的`NextActivation`数值接入
- [x] 7.10 用唯一target选择器提供当前Preview Instance和RuntimeDebugSession精确匹配的Live Actor，多目标时要求显式选择
- [x] 7.11 保持Live Actor的Gameplay Fact和Runtime Input只读，只把Tunable candidate发送给当前选中的一个Actor

## 8. Compile、Build、错误反馈与清理

- [x] 8.1 将Validate与Compile接到PoseGraph唯一轻量入口，并把错误映射到Node、Port、State或Transition
- [x] 8.2 将Character Build接到Definition唯一正式发布入口，并显示Dirty、Invalid、Stale、Ready与Building状态
- [x] 8.3 保证窗口打开、selection、字段修改、Undo、target切换、Preview播放、asset import、refresh和domain reload均不自动Build
- [x] 8.4 删除Profile专用runtime update、ScriptableObject轮询、FinalIK组件调参、旧Bottom Dock和所有兼容开关
- [x] 8.5 删除PoseGraph UI对Action Animation、有限Timeline和call site解析结果的依赖
- [ ] 8.6 更新current specs与项目文档中的共享Graph内核、PoseGraph唯一入口、正式Preview、selection调参和显式Build口径
- [x] 8.7 执行`openspec validate refactor-pose-graph-preview-and-live-tuning --strict --no-interactive`并同步任务状态
