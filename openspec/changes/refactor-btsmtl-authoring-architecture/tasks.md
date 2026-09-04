## 1. 实施基线与交接范围

- [ ] 1.1 核对实际工作区代码、未提交差异、作者资产、Program／Projection／Numeric Target、Rig、场景和输入trace，交付可重建的迁移基线记录及其他任务正确修改的保护清单。
- [ ] 1.2 按design迁移表盘点每个目标模块的输入输出、现有调用者和待删除实现，交付覆盖节点、Document、窗口、调参与外层Projection的引用清单；通用解释器及TrainingEnemy不进入删除或资产迁移清单。
- [ ] 1.3 核对与预览、Pose架构、相机、ACL和诊断变更共用文件的现有接口，交付本次作者职责与各外部change功能范围的边界表；不引入场景接入任务或预览完成依赖，有相反需求时保留冲突记录。
- [ ] 1.4 使用既有正式Replay／Proof和比较入口取得同版本、同输入的完整重复运行结果，记录输入、Body、表现时间和采样身份；只有无运行错误且证据完整的结果进入本次行为保持基线。

## 2. BTSMTL节点作者模块

- [ ] 2.1 在既有Framework内定义窄typed节点作者模块合同和显式装配，迁入共享Capability；交付单一登记／查找调用链，并通过登记完整性检查拒绝重复身份。
- [ ] 2.2 迁移控制流节点的声明、默认值、创建和配置，删除对应中央分支；用既有catalog导出与节点Validator核对kind、字段、role及端口投影保持。
- [ ] 2.3 迁移系统anchor、状态与条件节点作者规则，保留现有状态机／条件图数据与Mutation；通过正式Validator和引用清单确认没有第二状态／条件语义目录。
- [ ] 2.4 迁移输入与黑板绑定规则，保持精确Input／Request／Declaration身份和Get／Set模式；通过现有Port Shape与Document导出结果核对条件端口及边端点。
- [ ] 2.5 迁移Action激活、准入和生命周期节点作者配置，保持原Action／Context资源解析；交付同一typed Mutation调用链及对应catalog对账结果。
- [ ] 2.6 迁移Motion节点的模式、曲线引用和参数组合校验，删除中央Motion特例；用原合法／非法配置的现有Validator结果核对值域和错误code。
- [ ] 2.7 迁移AI感知、记忆、目标与Intent节点作者规则，保持AI domain过滤和受控Character合同；通过AI正式Validator与Document目录确认没有Character-only可写分片泄漏。
- [ ] 2.8 将UI、Clipboard、Document和preflight全部接到同一模块投影，保留Pose现有Node Definition；交付引用搜索结果，确认中央配置分支、重复字段／端口表和转发alias已删除。

## 3. Package解析与映射

- [ ] 3.1 将Graph／layout与Timeline／curves的Codec和Mapper从总类提取为对应分片模块，保持原strict parser；用既有合法package的canonical文件、字段及hash对账证明往返不变。
- [ ] 3.2 提取Presentation Profile、Pose Graph与PoseStateMachine分片映射，保持精确对象引用和稀疏layout；通过正式导出／解析核对有序数据、负localFileId和layout-only语义。
- [ ] 3.3 提取Linked Pose与注册AnimationClip Curve分片处理，保留readonly Interface与完整Curve字段；用现有package校验器确认文件闭包、只读边界和删除语义保持。
- [ ] 3.4 将合法local文件对的描述交给对应分片模块，保持Store唯一发现与manifest发布；通过现有严格解析入口核对完整配对与未知文件拒绝，不增加按目录放行。
- [ ] 3.5 收敛Package Mapper和Codec总入口并按真实类型整理文件名，迁移所有调用者及技能引用候选；交付canonical hash对账和旧文件／重复解析分支的零引用结果。

## 4. 整包对账与Mutation lowering

- [ ] 4.1 从现有Index／Resolver／planning symbol提取本次请求的只读引用上下文，明确跨模块依赖；交付数据流与引用检查，模块不持有窗口、Application Service或其他模块的可写状态。
- [ ] 4.2 提取Graph节点、属性边、流边和图引用对账，保留端口变化时的删边／配置／重连顺序；用现有dry-run计划和最终规范目标核对原相对顺序及身份。
- [ ] 4.3 提取状态机、状态行为图与条件规则对账，保持inline／shared ownership和local owner先后关系；通过正式preflight／Validator核对计划引用闭合。
- [ ] 4.4 提取黑板与Action对账，保持声明revision、默认值和request／profile规则；交付同目标下计划、机器诊断及最终作者投影的一致结果。
- [ ] 4.5 提取Timeline、Track、Clip、Motion／Warp及Curve对账，保留TreeClip与Action producer跨owner依赖；通过既有dry-run和Timeline Validator核对完整目标与删除顺序。
- [ ] 4.6 提取AI对账，保留纯schema normalization、Character产物过期与AI语义变化的不同准入；交付原错误／延迟编译状态保持的现有校验结果。
- [ ] 4.7 将Presentation对账继续分为Graph／状态机、Slot／Binding／Profile、Linked Pose和Clip Curve结果，统一回到原准备结果与事务组；通过整包preflight和reverse export核对跨模块引用及local身份替换。
- [ ] 4.8 按业务族迁移lowering实现并收敛Document Reconciler、Planner和Application Service调用链，删除旧中央实现；交付唯一hash锁定、完整计划、Undo／rollback／save／reverse export的引用检查和现有事务校验结果。

## 5. 外层Projection编译

- [ ] 5.1 提取Pose Source Catalog并明确唯一dense索引分配者，保持原稳定排序；用同输入编译产物核对source index、Slot／Binding解析和资源引用完全一致。
- [ ] 5.2 提取有限Action producer与Timeline call-site投影，保持channel、播放模式和来源身份；交付同Semantic IR输入下producer表与诊断一致的结果。
- [ ] 5.3 提取Source／Blend Space计划组装，保留Rig、Analysis和Phase编译调用顺序；用原canonical payload和既有Validator核对参数、曲线及脚步数据无变化。
- [ ] 5.4 提取Blend Curve／Profile目录及混合payload编译，保持索引、路由规则和数学；通过同输入产物比较确认未引入第二混合规则目录。
- [ ] 5.5 为当前已安装的相机／Cue／装备投影明确模块边界，复用已有Linked Pose／MM模块；交付正式依赖与payload对账，不实施相机或ACL的新业务。
- [ ] 5.6 收敛Projection总入口，使其只组织typed结果、调用现有Pose Compiler、构造Tuning Layout和完整校验；通过现有Build核对原生成顺序、ABI、机器错误与唯一原子发布入口。
- [ ] 5.7 对同一精确Definition和请求的Numeric Target比较重构前后Semantic IR、Program、Projection、revision与canonical内容，交付差异记录；未解释差异不得通过重新生成身份或放宽容差标记完成。

## 6. 共享工作区与页面模块

- [ ] 6.1 从现有Graph Shell提取唯一可组合区域与生命周期宿主，迁移重复区域装配；交付Tree／Pose／Action使用同一实现的引用链，Canvas、Details、Navigator与Undo仍沿现有组件。
- [ ] 6.2 提取文档定位、页面路由和窗口本地视图状态，保存精确owner、稳定页面／元素身份与布局；交付可重新解析的数据合同，不保存运行对象或作者数据副本。
- [ ] 6.3 迁移Tree导航、业务Details和runtime overlay出窗口，保留原图下钻与黑板交互；通过现有domain投影和菜单／打开入口清单确认没有第二Graph或selection集合。
- [ ] 6.4 迁移Pose窗口的Graph／状态机／规则页导航、创建命令和校验定位，保留独立Pose模型；交付各页面对共享宿主的适配及旧窗口业务分支删除结果。
- [ ] 6.5 提取Action关系解析与页面Presenter，保持Definition／Action／call-site／Timeline精确关系；交付重载定位和失效关系结果，不按名称、selection或目录猜目标。
- [ ] 6.6 实现编译期间暂停命令和重载后恢复页面／视图，删除无条件编译Close与创建时关闭通知；交付与delta场景对应的恢复处理、失败状态及相关旧分支零引用结果。
- [ ] 6.7 将只读运行值、作者内容、layout和选择刷新分别路由，移除整块Details／Bottom Dock逐帧Clear；交付保持字段草稿、选择、滚动与折叠的更新路径，外部owner变化仍走原冲突规则。
- [ ] 6.8 收敛Timeline窗口binding与订阅生命周期，删除重复时间定位订阅、Pause和Dispose；交付单次命令／通知的唯一调用链，保持Timeline交互、几何、曲线和本地selection所有权。

## 7. 既有调参与运行观察模块

本章只提取当前已有实现并保持其目标解析、字段资格、保存与采用规则；场景预览的统一Actor接入、多Actor规则和权限变化由独立预览change实施。

- [ ] 7.1 从Pose预览视口提取字段到owner／Tuning Layout的映射及正式作者值读写，继续消费原Capability与Mutation；交付相同字段资格、值域和Undo结果。
- [ ] 7.2 提取候选构造、精确Actor提交和generation管理，复用现有Candidate Compiler与Runtime协调器；交付窗口不再构造候选或读取Program私有表的引用检查。
- [ ] 7.3 提取既有作者保存、运行排队、已采用、需Build和拒绝结果的状态投影，保留NextFrame／NextActivation；交付迁移前后相同采用结果与失败处理的调用链，不新增场景运行资格。
- [ ] 7.4 迁移既有Undo／Redo和外部作者变化后的候选更新／失效处理，保持Document TreeDirty／Conflict及Play门禁；通过既有同步状态与调参报告核对无自动apply／rebase或产物写入。
- [ ] 7.5 提取Tree／Pose／Action／Timeline共有的只读事实解析，窗口保留各自Follow／Pin与interest；交付相同source map下的观察投影及单窗口解绑不清理其他窗口状态的生命周期实现。

## 8. 清理与合同同步

- [ ] 8.1 按design目录图完成Editor内部文件／类型职责整理，保留脚本meta、正式窗口类型和既有序列化身份；交付最终代码地图与旧文件名／转发alias清理结果。
- [ ] 8.2 检查节点模块、Document模块和Projection模块的引用方向，交付公共Framework无Character／AI／Agent DTO反向依赖、无新增循环程序集引用的结果。
- [ ] 8.3 检查正式作者资产、Document schema／文件集合、五MCP工具、Build和运行ABI保持；交付差异清单，发现必须改变的外部合同先更新明确范围，不能自动加兼容。
- [ ] 8.4 随最终目录更新 `btsmtl-agent-authoring` 及其当前合同的代码地图，交付每个被引用文件／类型均可解析的检查结果；不增加新的可写字段或生命周期工具。
- [ ] 8.5 按实际实施内容安装本change两份delta，并对账设计中列出的保持项和其他change已安装的共享接口；交付无相反重复实施要求的规范对账记录，不等待或实施外部场景预览能力。
- [ ] 8.6 对全部作者迁移单元执行旧Editor实现、旧菜单、旧窗口状态键及资源引用定向搜索，交付逐项删除或保留业务依据；通用解释器和旧完整角色播放器的整体去留不计入本次清理。

## 9. 集成证据与交付

- [ ] 9.1 完成现有Editor编译与程序集依赖检查；如使用dotnet／msbuild，命令带 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，结束后立即执行 `dotnet build-server shutdown`；Unity MCP每次显式传 `unity_instance`、CLI每次传 `--instance`，交付明确错误归属的构建记录，不运行Unity batchmode。
- [ ] 9.2 使用现有Character／AI Validator和Document生命周期对账完整输入到最终作者结果，交付strict parse、hash、计划、同一事务和reverse export的适用证据，不新增测试代码。
- [ ] 9.3 通过已有正式Replay／Proof比较固定基线和上一保留小步，交付输入／Body／时间／来源／Foot／Pelvis／Goal／Solved／Physical分层差异；运行错误、缺帧或缺少时间对齐条件均不得记为通过。
- [ ] 9.4 执行本change严格校验及限定改动的 `git diff --check`，交付proposal、两份delta、design和tasks与作者重构范围一致的结果；完成条件不包含独立预览change的实施进度。
- [ ] 9.5 为每个完整迁移单元形成详细中文小步提交，交付提交、文件跳转、删除清单和实际验证结果；不夹带其他任务修改，不以剩余类行数或文件数量声明完成。
