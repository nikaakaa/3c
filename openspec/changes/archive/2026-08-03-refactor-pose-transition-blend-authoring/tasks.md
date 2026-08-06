# Tasks

## 1. 固定现状与迁移闭包

- [x] 1.1 枚举所有`CharacterPoseStateTransition`作者字段、构造入口与校验入口。
- [x] 1.2 枚举`blend-curve-id`与`blend-profile-id`在Capability、Details、Mutation、Document和Compiler中的全部引用。
- [x] 1.3 枚举Standard Blend从transition clock到Native pose weight的完整调用链。
- [x] 1.4 枚举StateMachine Inertialization从Routing request到Native rule的完整调用链。
- [x] 1.5 枚举`CharacterAnimationBlendCurve`在BlendStack、AnimationSlot与Inertialization Policy中的作者owner。
- [x] 1.6 枚举全部正式`CharacterAnimationBlendProfile`资产及其Rig identity。
- [x] 1.7 枚举全部正式Pose StateMachine Transition及当前Blend Logic、duration和字符串值。
- [x] 1.8 固定Corin Standard Blend当前实际线性行为。
- [x] 1.9 固定Corin Inertialization当前实际curve/profile来源与数学结果。
- [x] 1.10 确认active change未提供第二条Transition blend实现。

## 2. 建立统一Blend Mode与Curve Asset合同

- [x] 2.1 定义稳定`CharacterAnimationBlendMode`枚举。
- [x] 2.2 注册`Linear`模式的唯一canonical曲线生成规则。
- [x] 2.3 注册`EaseIn`模式的唯一canonical曲线生成规则。
- [x] 2.4 注册`EaseOut`模式的唯一canonical曲线生成规则。
- [x] 2.5 注册`EaseInOut`模式的唯一canonical曲线生成规则。
- [x] 2.6 定义`Custom`模式必须引用Curve Asset的约束。
- [x] 2.7 定义非Custom模式禁止保存Custom Curve引用的约束。
- [x] 2.8 新增`CharacterAnimationBlendCurveAsset`稳定schema、CurveId和revision。
- [x] 2.9 让Curve Asset只保存唯一作者曲线正文。
- [x] 2.10 定义Unity `AnimationCurve`到非加权canonical Hermite key的转换器。
- [x] 2.11 拒绝Curve Asset空曲线与少于两个key。
- [x] 2.12 拒绝非`(0,0)`到`(1,1)`端点。
- [x] 2.13 拒绝非有限time、value与tangent。
- [x] 2.14 拒绝非严格递增time与递减value。
- [x] 2.15 拒绝越过`[0,1]`值域或不满足单调Hermite约束的segment。
- [x] 2.16 让所有Blend Mode最终只输出现有`AnimationBlendCurvePayload`。
- [x] 2.17 把Curve Asset正文纳入稳定revision/hash验证。
- [x] 2.18 删除Policy可达链中的第二种inline curve作者格式。

## 3. 完成Custom Curve资产编辑器

- [x] 3.1 为Curve Asset增加专用Custom Editor。
- [x] 3.2 在Inspector中显示CurveId与revision。
- [x] 3.3 使用可视化CurveField显示关键帧和切线。
- [x] 3.4 固定CurveField时间视图为`[0,1]`。
- [x] 3.5 固定CurveField值视图为`[0,1]`。
- [x] 3.6 在提交前复制draft并执行正式曲线验证。
- [x] 3.7 让合法编辑进入单次Undo、dirty与资产保存状态。
- [x] 3.8 让非法编辑保留旧正式曲线并显示结构化诊断。
- [x] 3.9 显示canonical segment摘要而不在`OnInspectorGUI`执行Compile或Build。
- [x] 3.10 确认选中、打开和修改Curve Asset都不自动Build。

## 4. 重构Pose Transition作者模型

- [x] 4.1 用`BlendMode`替换`CharacterPoseStateTransition.m_BlendCurveId`。
- [x] 4.2 用强类型Custom Curve资产引用替换字符串curve identity。
- [x] 4.3 用强类型`CharacterAnimationBlendProfile`引用替换`m_BlendProfileId`。
- [x] 4.4 更新Transition构造函数表达完整blend settings。
- [x] 4.5 更新Transition clone与字段替换逻辑保持其它字段不变。
- [x] 4.6 让Standard Blend允许Duration为0表示Hard Cut。
- [x] 4.7 让Inertialization严格要求正Duration。
- [x] 4.8 让全部非Hard Cut Transition要求合法Blend Profile资产。
- [x] 4.9 让Custom严格要求合法Curve Asset。
- [x] 4.10 让非Custom拒绝Custom Curve资产。
- [x] 4.11 更新StateMachine `RequireValid`检查新合同。
- [x] 4.12 更新Transition creation默认值为显式Linear与正式Uniform Profile选择流程。
- [x] 4.13 删除Transition作者层`TransitionBlendCurveId`使用。
- [x] 4.14 删除Transition作者层`TransitionBlendProfileId`使用。

## 5. 收口共享StateMachine Details

- [x] 5.1 在Pose Transition Capability中注册`blend-mode`字段。
- [x] 5.2 把`custom-blend-curve`声明为强类型AssetReference。
- [x] 5.3 把`blend-profile`声明为强类型AssetReference。
- [x] 5.4 为Capability资产字段补充稳定目标类型合同。
- [x] 5.5 让共享StateMachine Details按目标类型限制ObjectField。
- [x] 5.6 让Custom Curve字段只在Blend Mode为Custom时显示。
- [x] 5.7 保持Gameplay StateMachine不显示任何Pose blend字段。
- [x] 5.8 让Curve/Profile ObjectField显示资产名而不是GUID或identity文本。
- [x] 5.9 让错误类型引用在Mutation前失败。
- [x] 5.10 让Blend Mode修改提交`SetTransitionField` typed request。
- [x] 5.11 让Custom Curve修改提交同一typed request。
- [x] 5.12 让Blend Profile修改提交同一typed request。
- [x] 5.13 让字段修改进入单一Undo和Presentation dirty owner。
- [x] 5.14 让语义修改更新StateMachine content revision并标记Projection stale。
- [x] 5.15 确认Details刷新不修改资产或触发Build。

## 6. 同步Presentation Mutation与Validator

- [x] 6.1 扩展`CharacterPoseTransitionPayload`投影Blend Mode。
- [x] 6.2 扩展payload投影强类型Curve Asset引用。
- [x] 6.3 扩展payload投影强类型Blend Profile引用。
- [x] 6.4 更新Transition field reader返回真实typed值。
- [x] 6.5 更新`CharacterPoseTransitionFieldMutation`处理`blend-mode`。
- [x] 6.6 更新field mutation处理`custom-blend-curve`。
- [x] 6.7 更新field mutation处理`blend-profile`。
- [x] 6.8 删除`blend-curve-id` mutation分支。
- [x] 6.9 删除`blend-profile-id` mutation分支。
- [x] 6.10 更新Create Transition Mutation要求完整新payload。
- [x] 6.11 在Presentation Validator中校验Curve Asset identity与revision。
- [x] 6.12 在Presentation Validator中校验Profile RigId/revision。
- [x] 6.13 在Presentation Validator中拒绝双owner与无owner惯性时间数学。
- [x] 6.14 保持UI与Document Reconciler调用同一种Mutation。

## 7. 替换Document v3 Transition JSON

- [x] 7.1 将Transition文档模型增加`blendMode`。
- [x] 7.2 将文档模型增加条件式`customBlendCurveAssetId`。
- [x] 7.3 将文档模型增加`blendProfileAssetId`。
- [x] 7.4 删除文档模型`blendCurveId`。
- [x] 7.5 删除文档模型旧`blendProfileId`字段名。
- [x] 7.6 更新Exporter输出新字段顺序和枚举文本。
- [x] 7.7 让Exporter只在Custom模式输出Curve Asset identity。
- [x] 7.8 更新strict parser拒绝未知Blend Mode。
- [x] 7.9 更新strict parser拒绝Custom缺Curve Asset identity。
- [x] 7.10 更新strict parser拒绝非Custom携带Curve Asset identity。
- [x] 7.11 更新strict parser拒绝旧curve/profile字段。
- [x] 7.12 把Curve/Profile正式资产加入只读Asset Catalog及dependency context。
- [x] 7.13 让Reconciler从Catalog解析Curve Asset强类型引用。
- [x] 7.14 让Reconciler从Catalog解析Blend Profile强类型引用。
- [x] 7.15 让缺失、重复或错误类型asset identity在dry-run失败。
- [x] 7.16 更新planned diff显示业务名称与数学模式而不是GUID。
- [x] 7.17 更新reverse export规范化新字段。
- [x] 7.18 更新Document editable hash与context hash覆盖新引用和资源revision。
- [x] 7.19 更新Document当前合同和Agent authoring技能说明。

## 8. 统一Slot、BlendStack与直接Player Policy曲线作者格式

- [x] 8.1 用`BlendMode + Custom Curve Asset`替换`CharacterAnimationBlendTransitionRule` inline curve。
- [x] 8.2 更新AnimationSlot default与exact override校验。
- [x] 8.3 更新显式BlendStack default与exact override校验。
- [x] 8.4 更新Blend Policy Inspector显示Mode和条件式Curve Asset。
- [x] 8.5 拆分Inertialization temporal rule与node response字段。
- [x] 8.6 保留直接Player endpoint pair的完整exact temporal policy。
- [x] 8.7 让StateMachine上游节点只保存Parameter/residual response设置。
- [x] 8.8 让Slot上游惯性请求使用Slot exact route时间数学。
- [x] 8.9 拒绝Inertialization node同时收到上游temporal settings和直接Player temporal policy。
- [x] 8.10 拒绝直接Player topology缺少exact temporal policy。
- [x] 8.11 删除旧Policy inline curve序列化字段与legacy carrier。
- [x] 8.12 更新Policy schema version与正式配置API。

## 9. 扩展Projection curve/profile catalog

- [x] 9.1 让catalog compiler收集全部Pose Transition Blend Mode。
- [x] 9.2 让catalog compiler收集全部Custom Curve Asset。
- [x] 9.3 让catalog compiler收集全部Transition Blend Profile资产。
- [x] 9.4 让Built-in模式生成稳定canonical curve key。
- [x] 9.5 让相同canonical曲线去重为同一catalog entry。
- [x] 9.6 让相同Profile identity只能解析同一canonical dense payload。
- [x] 9.7 让Curve Asset revision进入Projection dependency和hash。
- [x] 9.8 让Blend Profile revision进入Projection dependency和hash。
- [x] 9.9 删除从Transition arbitrary ID映射catalog index的路径。
- [x] 9.10 保持compiled Routing层只使用稳定curve/profile index identity。
- [x] 9.11 让非法曲线或Rig不匹配时Build定位Transition与资产路径。

## 10. 让Standard Blend消费curve与per-bone profile

- [x] 10.1 扩展`CharacterPoseStateTransitionDescriptor`保存compiled curve/profile index。
- [x] 10.2 删除descriptor中的作者字符串curve/profile identity。
- [x] 10.3 扩展PoseStateMachine Native Control提交elapsed与base duration。
- [x] 10.4 扩展Native Control提交curve index与profile index。
- [x] 10.5 在Native Program中为StateMachine Standard Blend绑定curve segment catalog。
- [x] 10.6 在Native Program中绑定dense Blend Profile catalog。
- [x] 10.7 实现每Pose Bone的profile duration计算。
- [x] 10.8 实现每Pose Bone的canonical curve target weight计算。
- [x] 10.9 让Physical与Virtual Bone使用同一dense profile规则。
- [x] 10.10 让Pose Parameter使用global canonical envelope。
- [x] 10.11 让左右Foot Feature使用对应foot Bone envelope。
- [x] 10.12 让source relevance持续到所有所需envelope完成。
- [x] 10.13 保持Duration为0的Standard Blend当帧Hard Cut。
- [x] 10.14 删除`elapsed / duration`统一weight作为正式Standard实现。
- [x] 10.15 更新Standard Blend runtime snapshot显示Mode、curve、profile与选定bone weight。

## 11. 让StateMachine Inertialization消费edge数学

- [x] 11.1 让Inertialization plan compiler按Transition解析curve index。
- [x] 11.2 让Inertialization plan compiler按Transition解析profile index。
- [x] 11.3 保持Transition duration作为该request base duration。
- [x] 11.4 从StateMachine topology删除下游Policy temporal default覆盖。
- [x] 11.5 保留Inertialization节点对history、residual与rebase的唯一所有权。
- [x] 11.6 保留node response对Pose Parameter filter的唯一所有权。
- [x] 11.7 让连续中断按新edge rule原子替换accumulator。
- [x] 11.8 让capture/release lifecycle继续使用唯一Transition Routing模块。
- [x] 11.9 更新Inertialization snapshot显示触发Transition与edge curve/profile。
- [x] 11.10 删除“Policy catalog payload替代edge payload”的编译分支。
- [x] 11.11 让StateMachine只在Native Inertialization进入Complete或无残差终态后提交release completion。

## 12. 迁移正式资产与生成产物

- [x] 12.1 创建项目正式Custom Curve资产目录与命名规则。
- [x] 12.2 将仍需Custom形状的现有Policy曲线转换为正式Curve Asset。
- [x] 12.3 将可由Built-in模式精确表达的现有Policy曲线改为对应Mode。
- [x] 12.4 更新现有Blend Policy资产到新schema。
- [x] 12.5 更新现有直接Player Inertialization Policy资产到新schema。
- [x] 12.6 更新Corin Inertialization response配置并删除重复temporal字段。
- [x] 12.7 显式checkout Corin唯一Document v3。
- [x] 12.8 在Transition JSON中迁移全部Blend Mode。
- [x] 12.9 在Transition JSON中迁移全部Blend Profile Asset identity。
- [x] 12.10 只为真实Custom edge写入Curve Asset identity。
- [x] 12.11 dry-run确认planned diff不触及Gameplay Timeline或Simulation语义。
- [x] 12.12 使用exact document hash apply并canonical reverse export。
- [x] 12.13 显式发布Corin Presentation Projection与Native Pose Program。
- [x] 12.14 重新checkout确认Document回到Clean。
- [x] 12.15 对账生成Projection中每条Transition的curve/profile index。
- [x] 12.16 删除旧字符串picker kind、旧资产字段与旧文档形状。

## 13. 文档与静态收口

- [x] 13.1 更新`openspec/project.md`中的Transition blend当前状态。
- [x] 13.2 更新相关current spec与本change任务状态。
- [x] 13.3 更新`btsmtl-agent-authoring`当前合同引用。
- [x] 13.4 搜索确认作者层不存在`blend-curve-id`。
- [x] 13.5 搜索确认作者层不存在`blend-profile-id`自由文本路径。
- [x] 13.6 搜索确认Standard Blend不再固定使用统一线性weight。
- [x] 13.7 搜索确认StateMachine Inertialization不再读取第二份temporal default。
- [x] 13.8 搜索确认StateMachine Blend Logic没有BlendStack或Custom枚举。
- [x] 13.9 搜索确认没有selection、asset import或field change自动Build入口。
- [x] 13.10 执行严格OpenSpec校验并修复全部错误。

## 14. 收口State进入生命周期

- [x] 14.1 为`CharacterPoseStateDefinition`增加必填`AlwaysResetOnEntry`作者字段。
- [x] 14.2 更新State构造、clone与validation完整传递进入策略。
- [x] 14.3 为compiled `CharacterPoseStateDescriptor`增加State级进入策略。
- [x] 14.4 让StateMachine首次准备target时只读取目标State进入策略。
- [x] 14.5 让StateMachine整体Reset重新进入Entry State时执行同一进入策略。
- [x] 14.6 删除`PoseStateTargetResetPolicy`枚举。
- [x] 14.7 删除Transition作者与compiled descriptor中的Target Reset字段。
- [x] 14.8 删除Transition runtime对Target Reset的读取。
- [x] 14.9 删除Sequence Player作者payload中的`ResetOnEntry`。
- [x] 14.10 删除Sequence Player compiled descriptor中的`ResetOnEntry`。
- [x] 14.11 让Sequence Player relevancy变化不再暗中重置clock。
- [x] 14.12 保留Player初始clock与State显式`ResetForStateEntry`执行边界。

## 15. 收口source-local同步所有权

- [x] 15.1 删除Transition作者模型中的`SourceSyncMode`。
- [x] 15.2 删除Transition创建、clone与validation中的Source Sync参数。
- [x] 15.3 删除Transition Capability与Details中的`Source Sync`字段。
- [x] 15.4 删除Transition typed Mutation中的`source-sync-mode`分支。
- [x] 15.5 让Projection从source State可达provider识别唯一可同步候选。
- [x] 15.6 让Projection从target State可达provider识别唯一可同步候选。
- [x] 15.7 让共同canonical Sync Group自动生成Marker Group Source Sync Plan。
- [x] 15.8 让没有共同Sync Group的Transition生成明确None计划。
- [x] 15.9 让单State多个可同步候选以结构化Build错误失败。
- [x] 15.10 保留共同group的Sync Role冲突严格失败。
- [x] 15.11 保留共同group的Marker topology不完整严格失败。
- [x] 15.12 扩展自动同步候选支持Sequence与Blend Space provider。

## 16. 同步State UI、Document与正式Mutation

- [x] 16.1 在Pose State Capability注册`always-reset-on-entry`布尔字段。
- [x] 16.2 在共享State Details显示`Always Reset on Entry`。
- [x] 16.3 保持Gameplay State Details不显示Pose进入策略。
- [x] 16.4 扩展State payload投影`AlwaysResetOnEntry`。
- [x] 16.5 扩展State typed field Mutation修改进入策略。
- [x] 16.6 让State进入策略修改更新StateMachine revision并标记Projection stale。
- [x] 16.7 从Sequence Player Capability删除`reset-on-entry`。
- [x] 16.8 从Sequence Player create/field Mutation删除`reset-on-entry`。
- [x] 16.9 在Document State模型增加必填`alwaysResetOnEntry`。
- [x] 16.10 从Document Transition模型删除`targetResetPolicy`。
- [x] 16.11 从Document Transition模型删除`sourceSyncMode`。
- [x] 16.12 更新strict parser拒绝两个旧Transition字段和Player旧Reset属性。
- [x] 16.13 更新Exporter将State进入策略写入State JSON。
- [x] 16.14 更新Exporter停止输出Transition Reset/Sync字段和Player Reset属性。
- [x] 16.15 更新Reconciler比较并提交State进入策略typed Mutation。
- [x] 16.16 更新Reconciler停止解析Transition Reset/Sync与Player Reset。
- [x] 16.17 更新Presentation Validator验证State进入策略唯一owner。
- [x] 16.18 更新Document当前合同与`btsmtl-agent-authoring`技能说明。

## 17. 迁移Corin并完成产物对账

- [x] 17.1 checkout Corin唯一Document并确认同步状态。
- [x] 17.2 为Corin全部Pose State写入显式`alwaysResetOnEntry=true`。
- [x] 17.3 从Corin全部Transition JSON删除`targetResetPolicy`。
- [x] 17.4 从Corin全部Transition JSON删除`sourceSyncMode`。
- [x] 17.5 从Corin全部Sequence Player节点删除`reset-on-entry`。
- [x] 17.6 dry-run确认只包含State/Transition/Player目标迁移。
- [x] 17.7 使用exact Document hash apply并完成canonical reverse export。
- [x] 17.8 显式发布Corin Float32 Presentation Projection与Native Pose Program。
- [x] 17.9 重新checkout确认Document为Clean。
- [x] 17.10 对账Projection State进入策略、Transition blend curve/profile与自动Source Sync Plan。
- [x] 17.11 搜索确认作者层不存在Target Reset、Transition Source Sync或Player Reset三份旧入口。
- [x] 17.12 修复相关正式Inspector读取过期Projection字段产生的异常。
- [x] 17.13 更新current spec与`openspec/project.md`当前状态。
- [x] 17.14 执行相称的非BatchMode编译检查。
- [x] 17.15 执行严格OpenSpec校验并修复全部错误。
- [x] 17.16 将Corin的Turn到Run Loop、Walk Loop与Idle三条出口改为0.30秒Linear Standard Blend并重新发布产品。
