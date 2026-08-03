# Tasks

## 1. 现状冻结与冲突对账

- [x] 1.1 枚举`PresentationPoseSourceId`在authoring、Document、compiler、Projection和Runtime中的全部使用点。
- [x] 1.2 枚举Pose Player payload中的`m_SourceId`、`m_ProviderId`和对应constructor。
- [x] 1.3 枚举Profile内联`PresentationPoseSourceBinding[]`的全部读写入口。
- [x] 1.4 枚举Sequence、Blend Space与Motion Matching三类source的validator和compiler入口。
- [x] 1.5 枚举Document v3 profile、Pose Graph property和asset catalog中的source字段。
- [x] 1.6 枚举Details中IdentityReference退化为TextField的全部路径。
- [x] 1.7 枚举Popup、Navigator、breadcrumb和节点标题拼接或回退到identity的全部路径。
- [x] 1.8 枚举Profile、Rig、Blend Space、Motion Matching与Foot Analysis Inspector直接显示GUID、hash、revision或stable identity的位置。
- [x] 1.9 记录Corin全部Pose source、实际资源、source-local配置与consumer闭包。
- [x] 1.10 对账`refactor-animation-control-boundaries`中Source Id作者与runtime结论。
- [x] 1.11 对账`add-character-presentation-blend-space`中Source Id与资源owner结论。
- [x] 1.12 对账`add-character-motion-matching-pose-source`中provider/sample identity结论。

## 2. Source Slot作者模型

- [x] 2.1 定义`CharacterPresentationPoseSourceSlot`抽象子资产合同。
- [x] 2.2 定义Sequence Source Slot具体类型和可接受binding类型。
- [x] 2.3 定义Blend Space Source Slot具体类型和可接受binding类型。
- [x] 2.4 定义Motion Matching Source Slot具体类型和可接受binding类型。
- [x] 2.5 让Pose Graph资产唯一拥有Source Slot对象数组。
- [x] 2.6 校验每个Slot是当前Pose Graph文件内的合法子资产。
- [x] 2.7 校验Slot对象引用唯一且名字非空。
- [x] 2.8 校验不同Slot不因同名被合并或按名称重绑定。
- [x] 2.9 定义Source Slot创建typed Mutation。
- [x] 2.10 定义Source Slot重命名typed Mutation。
- [x] 2.11 定义Source Slot删除typed Mutation。
- [x] 2.12 让Slot创建、删除和Undo覆盖真实子资产生命周期。

## 3. Profile Binding子资产模型

- [x] 3.1 定义Profile-owned Pose source binding抽象子资产合同。
- [x] 3.2 定义Sequence binding的Clip、Rig、loop、rate、marker、curve和analysis字段。
- [x] 3.3 定义Blend Space binding的资产、Rig、参数和analysis字段。
- [x] 3.4 定义Motion Matching binding的Profile/provider、Rig、domain和analysis字段。
- [x] 3.5 让Profile数组只保存binding子资产对象引用。
- [x] 3.6 删除Profile内联`PresentationPoseSourceBinding[]`数据owner。
- [x] 3.7 校验binding属于当前Profile文件内的合法子资产。
- [x] 3.8 校验每个Source Slot在一个Profile中恰好拥有一个类型匹配binding。
- [x] 3.9 校验binding资源类型与Slot类型一致。
- [x] 3.10 校验binding Rig与Profile Rig一致。
- [x] 3.11 定义binding创建typed Mutation。
- [x] 3.12 定义binding资源和配置更新typed Mutation。
- [x] 3.13 定义binding删除typed Mutation。
- [x] 3.14 让binding创建、删除和Undo覆盖真实子资产生命周期。

## 4. Pose Player typed payload

- [x] 4.1 将SequencePlayer source字段改为Sequence Source Slot对象引用。
- [x] 4.2 将BlendSpacePlayer source字段改为Blend Space Source Slot对象引用。
- [x] 4.3 将SelectedPosePlayer source字段改为Motion Matching Source Slot对象引用。
- [x] 4.4 删除SelectedPosePlayer作者层Provider Id字符串。
- [x] 4.5 删除三类Player作者层Source Id字符串。
- [x] 4.6 更新Character Pose payload codec读取对象引用。
- [x] 4.7 更新Character Pose payload mutation写入对象引用。
- [x] 4.8 更新Capability Catalog声明精确Source Slot对象类型。
- [x] 4.9 更新clipboard codec保存和恢复对象引用。
- [x] 4.10 更新Pose Graph validator拒绝空Slot和类型不匹配Slot。
- [x] 4.11 更新Subgraph复制语义保持同一Slot对象引用。
- [x] 4.12 删除字符串Source Id的constructor、helper和mutation分支。

## 5. 通用Details与可读标签

- [x] 5.1 让AssetReference字段声明精确Unity对象类型。
- [x] 5.2 让通用ObjectField使用Capability声明的精确对象类型。
- [x] 5.3 禁止IdentityReference在缺少option source时退化为可编辑TextField。
- [x] 5.4 为缺少精确上下文的IdentityReference显示只读Unavailable状态。
- [x] 5.5 让IdentityReference Popup只显示业务DisplayName。
- [x] 5.6 删除Popup标签中的`DisplayName · identity`拼接。
- [x] 5.7 为Optional identity提供明确None选项而不伪造identity。
- [x] 5.8 为丢失引用显示Missing Reference而不直接显示原始identity。
- [x] 5.9 将原始identity只放入显式Diagnostics折叠区。
- [x] 5.10 为Pose parameter提供精确上下文的可读选项目录。
- [x] 5.11 为Gameplay Movement Mode提供精确上下文的可读选项目录。
- [x] 5.12 为Animation Channel与Slot提供精确上下文的可读选项目录。
- [x] 5.13 为Pose Subgraph提供Graph业务名选项目录。
- [x] 5.14 保持Rig bone选择显示骨骼业务名而不显示机器identity后缀。

## 6. Pose Source作者体验

- [x] 6.1 在SequencePlayer Details显示Source Slot对象选择器。
- [x] 6.2 在BlendSpacePlayer Details显示Source Slot对象选择器。
- [x] 6.3 在SelectedPosePlayer Details显示Source Slot对象选择器。
- [x] 6.4 在精确Definition/Profile上下文解析当前binding。
- [x] 6.5 在References显示实际AnimationClip、Blend Space或MM资源对象。
- [x] 6.6 在References显示Profile owner、Rig、duration、loop、marker和analysis状态。
- [x] 6.7 增加Ping Source资源命令。
- [x] 6.8 增加Open Source编辑器命令。
- [x] 6.9 增加Open Profile Owner命令。
- [x] 6.10 让节点副标题显示Source Slot名和实际资源名。
- [x] 6.11 让Navigator只显示State、Slot、binding与资源业务名。
- [x] 6.12 让breadcrumb只显示Graph、StateMachine、State和Rule业务名。
- [x] 6.13 删除Navigator和breadcrumb对GraphId、StateId、TransitionId与SourceId的显示回退。
- [x] 6.14 无精确Definition/Profile上下文时只显示Slot并禁用binding修改。
- [x] 6.15 保持selection和Inspector刷新不触发Compile、Build或Analysis。

## 7. Profile Inspector与动画资产Inspector

- [x] 7.1 用可读Source Slot列表替换Stable Source Id输入框。
- [x] 7.2 用binding子资产卡片替换内联Pose source binding绘制。
- [x] 7.3 支持显式创建Sequence binding子资产。
- [x] 7.4 支持显式创建Blend Space binding子资产。
- [x] 7.5 支持显式创建Motion Matching binding子资产。
- [x] 7.6 支持显式重命名binding业务名称。
- [x] 7.7 支持显式删除未使用binding。
- [x] 7.8 对仍有consumer的binding删除执行Mutation preflight拒绝。
- [x] 7.9 从Profile主视图删除Pose Graph、Rig和MM raw identity标签。
- [x] 7.10 从Pose source卡片删除Rig identity、Foot Analysis identity和Content Revision常驻字段。
- [x] 7.11 从Action producer卡片删除Program producer和Source Clip raw identity常驻字段。
- [x] 7.12 把必要机器identity移动到显式Diagnostics折叠区。
- [x] 7.13 保留Analysis Source ObjectField并删除其Source Identity常驻标签。
- [x] 7.14 盘点并收口其它动画作者Inspector的raw GUID/hash/revision常驻显示。
- [x] 7.15 保持generated产品Inspector作为只读Diagnostics，不提供作者mutation。

## 8. Projection编译与Runtime source index

- [x] 8.1 从Definition/Profile/Pose Graph闭包收集可达Source Slot对象。
- [x] 8.2 按精确对象引用解析唯一Profile binding。
- [x] 8.3 拒绝缺失、重复、跨Profile和类型不匹配binding。
- [x] 8.4 按稳定Unity对象身份确定性排序source表。
- [x] 8.5 为Projection生成连续dense source index。
- [x] 8.6 更新Projection source binding模型保存dense index和typed资源计划。
- [x] 8.7 更新Projection codec和revision hash覆盖Slot与binding内容。
- [x] 8.8 更新Projection source map保存可读业务名和Editor owner定位。
- [x] 8.9 更新Sequence provider使用dense source index。
- [x] 8.10 更新Blend Space provider使用dense source index。
- [x] 8.11 更新Motion Matching provider使用dense source index。
- [x] 8.12 更新`PresentationPoseSourceSample`使用dense source index。
- [x] 8.13 更新Selected、Sequence和Blend Space Player按index、node、generation与lease匹配sample。
- [x] 8.14 更新Physical Pose Source Registry使用Projection-local source index。
- [x] 8.15 更新Animancer source backend按typed Projection binding解析资源。
- [x] 8.16 更新Transition、BlendStack和Inertialization source usage不依赖Source Id字符串。
- [x] 8.17 更新Preview、Live Debug和Pose Watch显示source业务名。
- [x] 8.18 删除Runtime `PresentationPoseSourceId`查找和名称fallback。

## 9. Foot Analysis、Marker与资源依赖

- [x] 9.1 让Sequence binding子资产唯一保存source-local marker与weight curve。
- [x] 9.2 让Blend Space binding子资产精确引用Blend Space样本分析闭包。
- [x] 9.3 让MM binding子资产精确引用MM artifact与analysis闭包。
- [x] 9.4 让Foot Analysis resolver按binding对象和资源对象建立artifact key。
- [x] 9.5 让Projection Build按可达binding对象收集stable clip。
- [x] 9.6 更新artifact diagnostics使用资源名和owner名定位。
- [x] 9.7 将GUID、local file id和dependency hash限制在Editor构建身份与Diagnostics。
- [x] 9.8 删除按Source Id字符串查找artifact和marker binding的路径。

## 10. Document v3模型与codec

- [x] 10.1 扩展Document结构化资产引用保存`localFileId`。
- [x] 10.2 扩展asset catalog区分主资产和子资产对象。
- [x] 10.3 为Source Slot输出业务名、类型、owner和正式对象引用。
- [x] 10.4 为Profile binding输出业务名、类型、Slot引用和资源引用。
- [x] 10.5 更新Pose Player typed property使用Source Slot对象引用。
- [x] 10.6 为新建Source Slot定义`local:*` JSON语义。
- [x] 10.7 为新建Profile binding定义`local:*` JSON语义。
- [x] 10.8 更新strict parser拒绝未知引用字段和非法local file id。
- [x] 10.9 更新strict parser拒绝按显示名、路径或数组index单独绑定子资产。
- [x] 10.10 更新canonical writer按正式对象引用确定性排序。
- [x] 10.11 更新editable hash和document hash覆盖子资产引用。
- [x] 10.12 更新context hash覆盖可引用资源与owner变化。

## 11. Document Exporter、Reconciler与Mutation

- [x] 11.1 更新Presentation Exporter导出Source Slot对象。
- [x] 11.2 更新Presentation Exporter导出Profile binding子资产。
- [x] 11.3 更新Pose Graph Exporter导出Player到Slot的对象引用。
- [x] 11.4 更新Presentation Reconciler比较Source Slot对象集合。
- [x] 11.5 更新Presentation Reconciler比较binding子资产集合与内容。
- [x] 11.6 更新Presentation Reconciler解析已有子资产引用。
- [x] 11.7 更新Presentation Reconciler为`local:*`生成子资产创建Mutation。
- [x] 11.8 更新Mutation preflight锁定Pose Graph、Profile和全部新增/删除子资产owner。
- [x] 11.9 在一个Undo事务内创建子资产并写入数组与Player引用。
- [x] 11.10 在一个Undo事务内删除子资产并清理合法引用。
- [x] 11.11 让任一失败回滚子资产、数组、节点payload、dirty与保存状态。
- [x] 11.12 更新全域Validator验证对象owner、类型与闭包。
- [x] 11.13 更新reverse export把`local:*`替换为正式结构化对象引用。
- [x] 11.14 保持Document apply不触发Character Build或Projection Build。

## 12. Active change同步收口

- [x] 12.1 重写`refactor-animation-control-boundaries`中作者Source Id结论。
- [x] 12.2 重写`refactor-animation-control-boundaries`中runtime source sample身份结论。
- [x] 12.3 重写`add-character-presentation-blend-space`中BlendSpacePlayer Source Id合同。
- [x] 12.4 重写`add-character-presentation-blend-space`中Profile binding和compiler发现合同。
- [x] 12.5 重写`add-character-motion-matching-pose-source`中MM provider作者身份合同。
- [x] 12.6 重写`add-character-motion-matching-pose-source`中sample与runtime source身份合同。
- [x] 12.7 对账`fix-pose-state-machine-authoring-interactions`的Document文件闭包升级。
- [x] 12.8 更新`openspec/project.md`当前动画authoring与runtime source口径。
- [x] 12.9 更新`btsmtl-agent-authoring`技能的Presentation字段与子资产引用规则。
- [x] 12.10 更新`btsmtl-agent-authoring`当前合同代码地图。

## 13. Corin正式资产迁移

- [x] 13.1 在旧source字段删除前对精确Corin Definition执行一次Document checkout。
- [x] 13.2 从checkout事实确认Idle source资源与全部source-local配置。
- [x] 13.3 从checkout事实确认Walk Start和Walk Loop资源与全部source-local配置。
- [x] 13.4 从checkout事实确认Run Start、Run Loop和Run End资源与全部source-local配置。
- [x] 13.5 从checkout事实确认Moving Turn资源、clock binding与全部source-local配置。
- [x] 13.6 在新Document目标中创建对应Graph-owned Source Slot子资产。
- [x] 13.7 在新Document目标中创建对应Profile-owned binding子资产。
- [x] 13.8 将每个SequencePlayer引用改为精确Source Slot对象。
- [x] 13.9 保持全部State、Transition、Rule、Blend与clock业务语义不变。
- [x] 13.10 对新Document执行dry-run并检查完整旧删新建计划。
- [x] 13.11 用同一document hash执行一次资产级apply。
- [x] 13.12 apply后重新checkout并确认Document回到Clean。
- [x] 13.13 删除Corin资产中全部旧Source Id和内联binding序列化数据。
- [x] 13.14 通过明确Character Build命令重新发布Corin Projection和Native Pose Program。
- [x] 13.15 Build后重新checkout刷新generated context。
- [x] 13.16 执行BTSMTL正式validate确认authoring与Projection身份闭合。

## 14. 删除旧路径与静态收口

- [x] 14.1 删除`PresentationPoseSourceId`作者模型和Profile lookup API。
- [x] 14.2 删除Player字符串source/provider序列化字段。
- [x] 14.3 删除内联`PresentationPoseSourceBinding`作者类型。
- [x] 14.4 删除字符串source的Mutation、Exporter、Reconciler和codec分支。
- [x] 14.5 删除Details IdentityReference TextField fallback。
- [x] 14.6 删除Popup和Navigator显示identity的拼接逻辑。
- [x] 14.7 删除Profile Inspector的Stable Source Id输入和raw identity常驻标签。
- [x] 14.8 搜索并确认人工动画authoring UI没有可编辑GUID、hash、revision或stable identity字段。
- [x] 14.9 搜索并确认Source关系不存在按显示名、路径、数组index或字符串fallback查找。
- [x] 14.10 搜索并确认Sequence、Blend Space与MM只使用同一Source Slot/binding/dense index链。
- [x] 14.11 搜索并确认没有新增第二Presentation Mutation或Pose专用MCP action。
- [x] 14.12 搜索并确认没有selection、Inspector、窗口恢复或AssetDatabase refresh触发重操作。
- [x] 14.13 检查全部新增Unity代码和子资产类型的程序集所有权。
- [x] 14.14 检查全部新增代码文件的`.meta`配对。
- [x] 14.15 执行OpenSpec严格校验并修复全部诊断。
