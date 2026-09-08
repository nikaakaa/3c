# BTSMTL 重构与多 worktree 闭环

更新日期：2026-09-08。主目录核对基准：`a5853d45c1e33a3445af0b32e219604cd8bd6b40`。

## 1. 先看结论

目标是：作者能修改角色控制参数和技能内容，正式构建后在真实角色上运行，出现问题时能定位到具体技能、实例和片段，修正后能用同一输入确认结果。

当前已经建立控制、技能执行、Timeline、Pose、资源和部分编辑工具的基础。主目录已接收多项分支成果，但运行边界、作者操作、版本发布和最终证据仍有缺口，尚未进入只做验收的阶段。

后续只围绕四个结果组织进度：

| 工作包 | 作者最终得到什么 | 当前状态 |
|---|---|---|
| A 角色运行 | 移动和技能按明确规则开始、推进、换段、停止，各实例互不串扰 | 主要骨架已有；技能合同、事件隔离、并发及恢复边界未闭合 |
| B 作者操作 | 能创建、连接、配置、保存和撤销技能内容，UI 与 AI 使用相同规则 | v5 和部分工作区已有；通用子图写入及完整操作链未闭合 |
| C 构建发布 | 控制规则、技能、表现和资源作为一致版本被构建、加载 | 已有正式构建；最新组合对账及规则代码发布未闭合 |
| D 观察与验收 | 能在正式场景观察具体实例，并用对应版本的证据解释结果 | 部分场景、诊断及历史回放已有；最新整链未闭合 |

```mermaid
flowchart LR
    B[作者配置与修改] --> C[正式构建与加载]
    C --> A[角色运行]
    A --> D[场景观察与回放对账]
    D --> B
```

工作包编号表达职责，不替用户决定功能价值或业务排期。依赖只限制具体接线，不要求一个大 change 全部完成后其它工作才能继续。

## 2. 本文与已有文档的关系

- 本文是闭环总览和交付索引；细项状态仍维护在本 change 的 [tasks.md](tasks.md) 和各实现 worktree 的原执行记录中，不复制第二套可勾选任务表。
- 业务目标与合同来自 [proposal.md](proposal.md)、[design.md](design.md) 及对应规格。发现实现与合同不一致时记录差异，不通过缩小任务或改宽规格制造完成状态。
- 跨任务决定仍读取现有[协调记录](../../../docs/coordination-progress.md)，本文不代写该记录，也不改变原规划／实现配对。
- 本次只读取源码、Git 关系、文档和已有证据，没有重新构建、apply、回放或实际操作编辑器。提交说明中的成功报告与原始运行结果分别标明。
- 基准是本次会话的核对快照，主目录仍有未提交代码、资产和诊断内容；不是冻结过的构建候选。后续提交不能自动继承本次结论。
- 既有接收范围与顺序为 ACL → Timeline → 相机；其中已接收部分继续在主目录收尾。预览、AI、姿态修正、PIK 不因本文自动成为本轮整支合并范围。

## 3. 进度只使用明确状态

每项交付分开说明三个事实：

| 维度 | 必须回答的问题 |
|---|---|
| 实现 | 具体功能是否已写入正式调用链？还有哪些未实现行为？ |
| 接收 | 哪个工作区持有成果？哪些精确改动已被主目录采用？ |
| 验证 | 哪个源码与产物版本、通过什么入口、获得什么结果？ |

未关闭原因使用“缺实现”“缺接入”“缺证据”描述，可同时存在。源码编译成功、整支合入、一次 no-op dry-run 和某段历史回放不能互相替代。

Git 祖先关系只说明提交历史是否包含。组合接收、重新提交或历史重写后，分支存在独有提交不等于功能没有接入；分支已被包含也不等于功能验收完成。提交数量、空框数量和目录数量均不换算为完成百分比。

## 4. 工作包 A：角色运行

输入是正式角色输入、控制参数、技能定义和当前实例状态；输出是新的角色状态、动作结果以及提交到运动、表现等领域的正式输出。

| 事项 | 已有结果 | 剩余工作与关闭条件 | 原任务 |
|---|---|---|---|
| 控制状态机 | Corin 移动控制迁入代码；最新 `eb503bc83` 进一步改为按 Actor 实例化模块，删除自研控制 Runtime | 核对新时序、模块状态与恢复合同；保持实例隔离；用新版本验证移动及 Turn，旧1492帧记录不能覆盖这次引擎切换 | 2.1、2.1a、2.1b、5.1、7.1 |
| 技能定义与调用 | Attack1–5收为Attack技能树；依赖、后续候选、调用帧和动态值端口已有实现 | 补齐技能入口参数的作者→编译→调用链；核定技能自身索引、外部绑定与跨组合复用要求；验证多个调用点的独立状态和按值输入／成功输出 | 2.2–2.4、3.3、3.5、4.2、4.3、5.2 |
| 换段事件 | 事件传递、旧段终结和SegmentGeneration已有代码 | 事件只属于实际转移的实例；前后状态使用正式typed身份；核对同帧顺序、重复输出及旧代际命令；两Target事件和恢复结果可对账 | 7.6、7.7 |
| 并发和停止 | 共享准入、Activation／Commit／Lifecycle、实例作用域及stop barrier已有 | 明确合法并发与容量；逐项确认单活跃实例查询的适用范围；父技能Complete／Cancel／Interrupt／Reject／Abort及teardown完整关闭子图和Timeline，重复停止不串实例 | 4.4、4.6、5.3、5.4 |
| 变量、窗口、装备 | 技能局部状态、窗口来源、Feature代码绑定及Route→Skill引用已有 | 清理旧输入镜像；核对作用域；窗口同Tick可读；有效装备的参数、Action Context和snapshot贯通；领域叶子没有运动、播放器或网络旁路 | 4.5、4.7、5.5、6.1–6.3 |
| 状态与网络恢复 | 两Target状态／codec及Authority、Rollback接线已有阶段提交 | 在最终确认的产品合同下覆盖控制、实例、调用、Timeline、等待及停止状态；恢复不重复Enter／Exit或请求；验证checkpoint、Full／Delta、hash及输出替换／抑制 | 5.6、7.2–7.5 |

当前可复核的实质缺口：

- [Float32OperationEvaluator.NotifyStateTransition](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Simulation/Core/Float32/Execution/Float32OperationEvaluator.cs)推进当前段代后遍历全部活跃Action发事件，payload仍是`prev:…;next:…`；Fixed存在对应实现。并发串扰是源码风险，本次没有运行复现。
- [CharacterPresentationRuntime.NotifyDomainEvent](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/CharacterPresentationRuntime.cs)向下传实例ID与EventId，尚不能据此认定完整事件合同已实现。
- 技能入口签名已有类型声明，但本次未找到完整创建和发射消费链；不能把子图已有输入端口等同于技能入口合同全部完成。

关闭本包需要正式调用链及相应证据。完整命中／伤害、弹道、新战斗consumer和完整2v2vE不在本主重构范围内。

## 5. 工作包 B：作者操作

输入是作者选择的精确Definition、技能、图、调用点和目标配置；输出是通过正式校验保存的作者资产，以及重新读取后一致的Document。

| 事项 | 已有结果 | 剩余工作与关闭条件 | 原任务 |
|---|---|---|---|
| 正式角色入口 | Definition已有控制与技能字段，工作区已有SkillId与Timeline route | 删除失去业务意义的角色RootTree创建／页面／发现／调度入口；保留仍被技能内容使用的合法存储和引用，不按类名或目录整块删除 | 3.1、4.1、9.4、12.1、12.4 |
| 共享作者规则 | Capability、Port Shape和部分typed Mutation已有 | 创建、配置、复制粘贴、动态端口及受影响连接使用唯一领域规则；UI／Agent／Compiler可见能力一致；删除重复字段表和中央特例 | 9.1、9.2、3.2 |
| 子图与签名 | SubTree有部分setter，编译和实例调用已有 | 精确owner／slot的inline/shared挂载、按稳定声明身份完整替换输入输出绑定；UI和Agent消费同一实现 | 9.2、9.3、10.3 |
| Document v5 | 唯一v5、控制／技能分片、五生命周期、no-op空计划已有 | 新建／修改／删除的完整计划；producer先于consumer；全部owner进入同一保存、Undo、失败恢复和reverse export；local替换成stable后重新读包一致 | 10.1–10.6 |
| 技能工作区 | 已有精确Skill、多Timeline候选和部分导航 | 技能签名／子图完整工作面；Tree-only合法；按调用路径导航资源和动画通道；重载、草稿、订阅释放；运行目标失效时不自动换到另一个实例 | 9.3、9.5、11.1–11.4 |

[AgentGraphReferenceMutationHandler](../../../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/AgentAuthoring/AgentGraphReferenceMutationHandler.cs)仍对通用SubTree挂载和动态参数替换返回`graph_reference_api_unavailable`／`graph_binding_api_unavailable`。已有单个setter不构成整条共享作者API交付。该缺口不靠反射写字段、直接改YAML或第二套Mutation解决。

作者闭环的结果是“能够完成修改并正确保存／恢复”，不是仅展示一个新面板。Inspector绘制、selection和窗口恢复不得触发重构建或其它重操作。

## 6. 工作包 C：构建与版本发布

输入是完整作者依赖、明确Target、规则代码版本及资源；输出是可重读验证、身份一致、被实际运行入口采用的产物组。

| 事项 | 已有结果 | 剩余工作与关闭条件 | 原任务 |
|---|---|---|---|
| 编译职责 | 控制、技能、Timeline、变量、装备等发射已拆出多个模块 | 核对UI／Emitter／Target支持集、source map、声明和依赖闭包；根入口只组织正式模块；删除重复业务分支 | 3.2、3.4、3.5、12.5 |
| 数值Target与产物 | 同一IR生成Float32／Fixed及Projection的路径已有，历史同组构建成功 | 最新布局、ABI、资源映射、wrapper和实际Launcher引用一致；旧版本／混版／缺模块／缺能力拒绝；store重读和发布失败边界有效 | 3.6、3.7、7.3、12.3 |
| 规则代码发布 | Control.Rules及主要运行装配已有 | 接入正式HybridCLR配置、依赖加载、裁剪／泛型生成和相关产品清单；版本与技能依赖一起锁定，不以静态程序集引用代替可更新规则发布 | 8.1–8.3 |
| 版本采用 | 既有Session和产物版本基础保留 | 新Session采用完整新组合，活动Session维持其已锁定版本；缺失实现拒绝，不能局部换子图／规则并沿用不兼容状态 | 8.4 |

`9461a642f`的正式Gameplay Lab重建成功记录属于该提交及当时资源。之后相机、状态机和编辑器仍有变化，不能直接作为当前组合的Build receipt。

Character Document apply只保存作者资产；需要产物时显式走精确Definition的正式Build。独立Timeline使用其精确内容根。不得恢复selection猜根、目录扫描自动构建或旧reader。

## 7. 工作包 D：观察与最终验收

输入是确定的源码／产物、录制输入、初始世界和表现时钟；输出是绑定这些身份的编译、运行、采样及比较结果。

| 事项 | 要关闭的问题 | 原任务或提供方 |
|---|---|---|
| 技能试验与观察 | 从作者选择到正式输入／准入结果，再到精确ActionInstance、调用代际和Timeline；显示拒绝、替换、失效原因，不按模板取首个实例 | 11.3–11.5；Scene Play |
| 作者调参与采用 | 保存成功、运行待采用、已采用、需Build、失败分别显示；Undo、暂停、共享Profile和多Actor只影响指定目标 | Scene Play；各领域参数合同 |
| 重复性与业务差异 | 同版本相同输入结果一致；跨重构比较输入、Body、动作阶段、窗口和领域输出，不用ABI hash变化直接判回归 | 1.4、13.2 |
| 完整采样窗口 | 关闭Foot的1492输入／1491保存差额、表现采样起止错位；不能通过截掉错误帧或忽略异常制造通过 | 13.2；诊断owner |
| 完整生命周期 | 覆盖开始、连段、取消、重复释放、合法并发、停止和所要求的恢复；逻辑结果、动画、相机及资源释放分别可追溯 | A、C及关联模块 |
| 可交付记录 | 精确源码／未提交差异、工具、产物、Run、输入、日志和结果可以对应；旧失败保持原身份，新结果另存 | 1.1、13.1、13.2、13.4 |

遵循已有“先完成重构，再统一Replay”安排，不为每次拆类重复完整回放。必要编译和明确错误修复随实现进行；最终统一回放必须采用收口后的实际版本。

使用现有正式CLI、Validator、Document生命周期、Replay／Proof及诊断流程，不新增测试代码或临时运行器。若执行.NET构建，带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`并立即执行`dotnet build-server shutdown`。Unity本机调用遵循当前根指令、精确项目和实例规则，保留主验收Editor。

## 8. 每个 worktree 现在负责什么

以下是2026-09-08会话核对结果。历史目录不等于待删除目录；未提交文件及LFS变更未经过清理授权或内容处置，本次一律保留。

| 目录／分支 | 核对HEAD | 当前位置与继续边界 |
|---|---|---|
| `D:/Unity_Project_1/3C`／`main` | `a5853d45c` | 当前集成主目录，承接A–D中的主重构和已接收模块；有未提交代码／资产，不是已冻结候选 |
| `D:/Unity_Project_1/3C-worktrees/btsmtl-authoring-architecture` | `3ab172a97` | 六个独有提交是三次修改及对应撤回，净差异为零，之前基线已在main；进度看主目录，避免重做旧任务；另有未跟踪meta |
| `C:/Users/Lenovo/.codex/worktrees/a323/3C`／`codex/posegraph-luna` | `2f3cc86e7` | 已提交历史包含于main，后续Pose工作已在main继续；目录仍有配置／资源修改 |
| `D:/Unity_Project_1/3C-worktrees/timeline-runtime` | `698eb44d6` | 主要业务由main的`0495e6425`组合接收；剩余逐Clip策略、Document独立域、观察和完整对账继续按原owner处理，不重复整支回灌 |
| `D:/Unity_Project_1/camera-zzz` | `82c851507` | 已提交历史包含于main，工作区盘点时干净；主目录继续接收后的生命周期／资源／观察收尾；整项相机能力未获完整验收 |
| `D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview` | `71544dcc7` | 有独立成果且尚未整体接收；继续精确技能试验、参数采用、最新提供接口及最终观察链；旧缺接口报告需对照最新main复核 |
| `D:/Unity_Project_1/3C-worktrees/behavior-designer-ai` | `feddf49c9` | 已有插件Host／Source／任务、结果观察、批量输入和冻结事实修复；未整体接收；继续正式内容、网络多Actor、旧AI退役及运行证明 |
| `D:/Unity_Project_1/pose-correction` | `fdad43a2e` | 节点、样本、作者／编译／运行接线已有；未整体接收；继续自身新版本产物发布、真实Actor权重／骨骼效果观察和最终审查；有文档修改 |
| `D:/Unity_Project_1/3C-parallel-test`／`codex/ik-runtime-validation` | `41007cbc8` | 实际承载ZZZ PIK迁移；仍欠完整输入、查询／支撑／脚／骨盆链、Solver映射、资源切换和最终骨骼验证，不能列为只差合并 |
| `D:/Unity_Project_1/3C-center` | `e9aae0ba2` | 旧Center工作区；正式源码已迁独立仓库，不是当前Center进度入口 |
| `D:/Unity_Project_1/3C-center-tools` | `e9aae0ba2` | 与上一目录同一提交、盘点时干净，不重复计算一份工具成果 |
| `D:/Unity_Project_1/3C-worktrees/foot-ground-surface` | `6cd7851a4` | 旧地面修复分支；盘点时有6326项暂存变更并涉及LFS属性，先辨明历史和文件转换范围，不作为几千项新功能接收 |
| `D:/Unity_Project_1/3C-worktrees/publish-main-lfs` | `c05060182` | 历史发布／LFS整理目录，存在`.git-rewrite/`；分叉历史不能当成功能待办 |
| `C:/Users/Lenovo/.codex/worktrees/a88d/3C` | `229d2a9d0` | 干净detached旧快照，已包含于main，没有独立新提交 |
| `C:/Users/Lenovo/.codex/worktrees/2205/3C` | 登记`3bf66c4ea` | Git登记为`locked initializing`，实际路径不存在；不是有效运行工作区 |

ACL当前没有独立登记worktree，`codex/acl`分支仍存在，主要成果已通过主目录接收／修正继续演进。不能仅凭该分支tip不在main祖先中认定未接入。

Center正式仓库为`D:/Unity_Project_1/3C-Development-Center`，本次读到`65132e0`；已有Avalonia界面、任务树和诊断浏览。游戏仓库的两个旧Center目录不能代替独立仓库状态，旧执行记录中的网络／性能剩余范围仍需由其owner按最新实现核定。

## 9. 关联成果怎样接回主链

| 提供方 | 主链消费什么 | 还需提交的完整范围 | 不扩大为 |
|---|---|---|---|
| Timeline | 共用时间执行、片段生命周期、精确调用来源及独立内容根 | 逐Clip表现策略、完整相机停止／尾段、独立Document域、shared跨包冲突／事务、来源观察及角色对账 | 重做已有双Target双面板样例 |
| Pose／ACL | 正式姿态Program、资源准备／租约及已提交结果 | 当前资源／Projection身份、调用代际、取消／Discard／释放、实际最终姿态；Pose编辑器与编译器自身剩余项由原owner收口 | 把全部算法重构塞进BTSMTL主任务 |
| Camera | 已提交的动作／Timeline请求到镜头结果 | 精确资源、source／实例／generation、进入更新停止、目标与最终Basis、作者／观察接入 | 用资源数量或分支合入代替真实动作调用和画面验收 |
| Scene Play | 场景生命周期、作者选择、正式输入、实例观察和参数采用 | 与当前Skill／Timeline／Pose／Camera合同对齐，保留无关模块诊断，删除完整角色旧播放器残留 | 新SkillPreviewRuntime、窗口播放器或第二套seek执行 |
| AI | 插件通过正式输入合同驱动角色 | 冻结输入、可恢复消费、请求到结果、产品权限／多Actor以及旧AI域退役 | 插件直接读写技能私有状态、执行技能／Timeline或冒充完整战斗 |
| Pose Correction／PIK | 各自领域的完整已验证能力 | 精确源版本、数据／参数、正式接入点、实际目标及骨骼结果；必要共享接口单独对账 | 自动进入本轮ACL→Timeline→相机整支接收范围 |

先由原owner交付真实接口和必要依赖，接收方核对当前main是否已有同义实现，再迁入具体调用者。同一接口已有正确实现时保留它；冲突具体列出源码、合同和业务影响，不能用兼容配置、临时桥或另一个执行器绕过。

共享接口没有交付只阻止对应接线。子图作者API不需要等待相机全部完成，事件实例隔离不需要等待整个Scene Play完成。

## 10. 本轮证据与限制

| 记录 | 已能说明 | 不能说明 |
|---|---|---|
| 9月7日控制阶段1492帧重复回放；tasks中的`513c8fa7f`、`20067cf12`阶段记录 | 当时控制迁移／资产清理版本的重复性有成功报告 | 后续技能树化、事件层和`eb503bc83`控制引擎切换已通过 |
| `93ad2a768`及`.codex-tmp/corin-document-noop-fixed-20260907.json` | 本次会话读取到v5、success=true、Clean、空plannedDiff，applied=false、saved=false | 新包创建、apply、删除与失败恢复全部通过 |
| `9461a642f`提交说明 | 当时Pose引用／资源注册修复后正式Gameplay Lab重建成功的报告 | 当前磁盘、最新规则／相机／编辑器组合均已构建验收 |
| `0495e6425`接收记录 | Timeline业务组合已进入main；保留主线分域实现 | Timeline整项、独立Document域和所有表现策略完成 |
| Timeline实现记录中的双Target产物和`timeline-panel-run-lifecycle.log` | 独立双面板、并发调用、停止及错误目标拒绝的阶段运行报告 | 最新角色、相机、技能树和整包事务全部通过 |
| Scene Play的`implementation-audit.md` | 指定源码的编译、场景构建和部分观察交付 | 当前main全部新接口已进入预览分支或整体已批准 |
| Pose Correction的`review.json`与实现记录 | 指定版本节点／作者接线、Document写回和局部编译证据 | 当前Projection正式发布及真实Actor效果已完成 |
| PIK的原函数对照与1044帧实验 | 指定子步骤的公式或阶段结果，有失败及撤回记录 | 原算法完整迁移或主要脚部质量已经改善 |
| `eb503bc83`提交说明 | 新控制模块按Actor工厂化及若干程序集增量编译成功报告 | 恢复合同仍成立、最新Build和Replay通过 |
| `a5853d45c`提交说明 | Pose节点字段编辑和粘贴／重复路由已有代码 | 该批次编译、交互验证和Build完成；提交本身明确仍待验证 |

原始失败不删除、不覆盖。恢复到旧行为也不计为新的功能收益。后续取得新结果时，保留原记录并写明替代了哪个版本的哪项结论。

## 11. 尚需明确的合同差异

| 差异 | 当前事实与业务取舍 | 处理边界 |
|---|---|---|
| 控制状态恢复 | `eb503bc83`说明按帧同步口径无快照恢复需求，模块持有`m_Machine`／`m_Initialized`；现有[控制规格](specs/character-control-runtime/spec.md)仍要求全部影响后续模拟的状态进入角色snapshot／hash，恢复不依赖旧对象游标。保留恢复能力可服务回滚／纠正但需要正式状态还原；明确移除相关产品恢复要求可减少实现负担，但必须同步产品能力和全部消费者 | 本文不判断其它任务是否已得到新的用户决定；需追溯决定与实际调用链，未对齐前不能关闭7.2／7.4／7.5，也不擅自回退新控制实现 |
| 连招归属 | design第2节仍有连段／取消由控制代码组织的旧描述，后续用户决定及5.2已经改为技能树内接续。资产作者可直接编辑连招，控制层保持单一请求，但树内条件与取消窗口需要完整维护 | 规范收口时删除相反旧要求，保留已经正确迁入技能树的成果 |
| 技能独立定义索引 | [技能规格](specs/btsmtl-skill-program-runtime/spec.md)要求技能自身索引和显式外部绑定；目前完整跨组合复用交付尚未证明。它支持复用同一技能定义，代价是编译链接和绑定合同更明确 | 保留未完成状态；不能用角色包里已有一个Skill入口索引自动判为满足，也不未经业务决定缩小要求 |
| 事件身份与发布范围 | 7.7要求实际ActionInstance及typed前后状态；当前字符串payload、全部活跃实例发布与之有差距 | 修正正式链或提交明确合同决策，不放宽断言吞掉错误 |
| AI／Timeline领域集合 | 主v5仍有AIController；AI替换要求退役游戏AI域，Timeline要求增加独立域 | 最终按已采用版本组合安装，保留技能／Timeline／Pose共享能力，不恢复旧reader或覆盖已交付增量 |
| TrainingEnemy范围 | 原主重构要求不顺手修复无效TrainingEnemy；后续已有明确退役提交 | 对账退役后的实际内容和各change旧条款，不重新恢复资产作为验收前提 |

任务文字的已知滞后：2.1／3.1仍有旧图控制描述；2.2／2.3低估已有依赖／调用帧；6.1／6.2的勾选与正文相反；11.1低估已有Skill／Timeline route页面。原owner应按代码、接收和证据分别更新，本文不批量勾选或重复实施。

## 12. 收尾交付与最终关闭

每一批交付直接写回对应原任务／实施记录，包含下列信息：

| 信息 | 内容 |
|---|---|
| 实际能力 | 作者或角色现在能完成什么，属于A／B／C／D哪一段 |
| 精确来源 | worktree、完整提交或工作区差异、必要依赖及原任务号 |
| 输入输出 | 正式输入、唯一处理模块、实际输出与消费者 |
| 接收范围 | 哪些变更已进入main、哪些仍留分支；旧职责和配置删了什么 |
| 结果证据 | 对应源码／产物、工具入口、Run或日志、结果及限制 |
| 剩余问题 | 缺实现、缺接入、缺证据分别列明；冲突指向真实合同，不只写“等主线” |

接收时先核对已存在的正确代码，再接完整业务单元；必要依赖按实际签名处理。每次形成独立中文提交，不整文件覆盖其它任务，不把分支接收等同于验收。本文不触发新任务、消息广播、分支清理或自动合并。

最终关闭依赖以下结果同时成立：

1. A、B、C中的实施缺口关闭，D获得对应最终版本的完整证据；合同差异有明确结果，不能把未决恢复范围藏进完成说明。
2. 角色控制、技能、Timeline、表现与资源只有正式唯一调用链；旧角色入口、重复实现和废弃数据完成定向清理，合法共享模块与独立诊断保留。
3. 当前selected Target、规则、Program、Projection、资源及实际运行引用一致；发布／加载／整包失败不会留下被当作成功的半份结果。
4. 原tasks逐条按真实完成条件收口，保留未覆盖范围；现行spec、相关delta、技能合同和项目说明完成组合对账及适用严格校验，独立change的未完功能不被冒领。
5. 构建、运行、事务和行为证据能够从交付记录直接找到；已有用户验收／归档指令按项目规则处理，不新增人工验收task。

## 13. 原始记录入口

- 主重构：[任务](tasks.md)、[设计](design.md)、[迁移基线](baseline.md)。
- Timeline：[实现清单](D:/Unity_Project_1/3C-worktrees/timeline-runtime/openspec/changes/decouple-timeline-from-skill/tasks.md)、[执行记录](D:/Unity_Project_1/3C-worktrees/timeline-runtime/openspec/changes/decouple-timeline-from-skill/implementation/baseline-inventory.md)。
- Scene Play：[实施审计](D:/Unity_Project_1/3C-worktrees/btsmtl-scene-play-preview/openspec/changes/rebuild-btsmtl-preview-with-scene-play/implementation-audit.md)。
- AI：[分支任务](D:/Unity_Project_1/3C-worktrees/behavior-designer-ai/openspec/changes/replace-btsmtl-ai-with-behavior-designer/tasks.md)。
- Pose：[当前主目录任务](../refactor-character-pose-graph-architecture/tasks.md)；相机：[原分支任务](D:/Unity_Project_1/camera-zzz/openspec/changes/rebuild-character-camera-from-zzz/tasks.md)。
- Pose Correction：[实施记录](D:/Unity_Project_1/pose-correction/openspec/changes/add-character-pose-correction/implementation.md)、[验证范围](D:/Unity_Project_1/pose-correction/openspec/changes/add-character-pose-correction/review.json)。
- PIK：[任务与实验入口](D:/Unity_Project_1/3C-parallel-test/openspec/changes/rebuild-character-foot-ik-from-zzz-pik/tasks.md)。
- Center：[独立仓库说明](D:/Unity_Project_1/3C-Development-Center/src/ThirdPersonDevelopment/README.md)。
