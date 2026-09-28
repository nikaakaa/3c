# 角色运行与作者底层重构计划

更新：2026-09-28。

状态：代码评估与后续实施建议，尚未实施。本文承接本次架构、大类和职责评估，不替代 `openspec/specs/` 的现行合同，也不修改已经归档的历史方案。本次文档更新不启动新的 OpenSpec workflow。

## 目标与范围

让一帧成败由一处决定，每份状态只有一个提交者，新增业务尽量在所属模块内完成。减少调用方必须了解的内部阶段，删除被替代的状态、转发、数据和入口。

这里的“干净”不按类数量、文件行数或接口数量衡量。拆成多个 partial、增加一层 Manager 转发、把同一状态复制给多个模块，都不算完成职责整理。

保留当前方向：技能使用编译后的执行数据；Pose 使用原生 FlowCanvas 图实例；Timeline 使用正式内容与播放管理；Source、Constraint、FinalPublication 分别拥有动画来源、约束历史与最终姿势输出。运行期继续遵守预分配与 0 GC 要求。

不在此计划中改变 Foot、IK、混合算法或角色配置，不恢复 Pose IR、独立 TreeRunner、旧作者窗口或第二条 Preview／Runtime 链。其他窗口正在修改的算法与资产不归入本计划。

## 当前事实与证据范围

- `Simulation.Core` 的程序集禁止引擎引用，玩法核心与 Unity 表现有实际隔离。
- 公共 `BTSMTL.Authoring` 不依赖 TreeDesigner，但仍使用 Unity 类型，不能描述成完全脱离 Unity 的通用库。
- 技能作者节点直接声明 FlowCanvas 端口，正式运行经过技能编译器；不能把整个技能系统描述成“TreeDesigner 端口外套 FlowCanvas UI”。
- TreeDesigner 的部分字段端口、黑板及 Timeline 接入仍有引用，迁移尚未完全结束。
- 旧 Pose 状态机编译计划、规则 IR、无消费者的 TreeRunner、InputActionAssetValueSource、ITypeAdapter 与 SerializeHelper 已删除；这不等于大类职责已经完成重构。
- 下述问题来自源码与调用链检查。Pose 失败场景未进行故障复现；没有据此推断实际性能收益或宣称已经满足全链路 0 GC。

## 建议实施顺序

先收拢 Pose 帧提交与失败处理，再整理 CharacterTimelineHost，随后按真实消费者迁移 TreeDesigner 剩余依赖。这是本次讨论的建议顺序，不代表三个阶段已经获准实施或已经交付。

Pose Workspace、会话恢复模块与动画分析器是独立候选，不强行作为前三项的前置。每次以一个完整职责为提交单位；不能为减少单次修改量而长期保留新旧双路径。

## 一、收拢 Pose 帧事务

### 当前问题

入口与证据：

- [CharacterPresentationDomainRuntime.RunPoseFrame](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/CharacterPresentationDomainRuntime.cs) 直接理解 Begin、PrepareEvaluation、Evaluate、Validate、Commit、Discard，并协调动作命令和表现时钟。
- [CharacterPoseNativeFrameCoordinator](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeFrameCoordinator.cs) 保存帧租约与阶段结果；GraphRuntime 也持有执行阶段及结果。两者分别涉及外层帧与图执行，不能仅凭字段相似判断冗余。
- [CharacterPoseNativeGraphRuntime.Commit](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeGraphRuntime.cs) 的最终发布重载捕获异常后返回 `Faulted`。
- [CharacterPoseNativeRoleRuntime.Commit](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/PoseGraph/CharacterPoseNativeRoleRuntime.cs) 接收该结果后，没有判断提交成功，继续调用约束 `CompleteFrame/SealFrame` 和来源 `CommitFrame`。

已确认的是失败结果没有阻断后续提交调用；实际哪些状态能推进、哪些调用会先报错，需要按失败位置继续追踪。不能把它直接描述成已经复现的视觉错误。

### 输入、输出与职责

输入是同一角色、同一表现帧的已提交事实、动画参数、动作命令和准备好的资源。输出是已经提交的最终姿势，或者明确的失败阶段与故障结果。

帧事务只拥有阶段顺序、结果判断和统一收尾。Graph 继续拥有图执行与子图状态；Source 继续拥有采样和资源生命周期；Constraint 继续拥有 Foot／IK 的 Pending 与 Committed 历史；FinalPublication 继续拥有最终结果页与骨骼写入。

### 实施内容

1. 沿正常提交和每个失败返回追踪状态变化，明确动画求值、物理写入、图提交、历史提升、动作确认与延迟释放的位置。
2. 修正发布失败后继续提交的问题，并明确失败清理使用哪个仍有效的租约；不能只补成功判断后留下未关闭的帧。
3. 动画求值之前的失败丢弃 Pending；动画求值之内或之后的失败进入现有 Actor 故障处理，不伪装成完整回滚。
4. 所有可能失败的内容准备与合法性检查前置；成功路径末尾只执行已准备好的状态提升、确认与释放，不在骨骼写入后新增可能失败的业务计算。
5. 在现有帧执行模块内收拢整体成败判断，逐项辨认并删除纯转发、重复整体帧状态和重复分支；保留图及子图确实需要的局部阶段状态。
6. 外层表现模块继续协调 Timeline、Camera、动作命令和表现时钟，减少对 Pose 内部失败类型和阶段细节的理解。

### 业务取舍与完成形态

收益是避免姿势发布失败后历史或来源继续推进，修改失败处理时也不必跨多个转发层。代价是必须保持求值与物理写入的不可逆顺序，不能简单把所有调用藏进一个万能 Update。

完成后，整体提交或丢弃由一个明确模块决定；每份可变状态仍只有一个拥有者；失败不会再走成功提交尾部；不可逆失败被明确报告。不能仅以新增一个 if、缩短文件或编译通过认定整个重构完成。

## 二、整理 CharacterTimelineHost

[CharacterTimelineHost](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/Lifecycle/CharacterTimelineHost.cs) 文件约 2484 行，Host 本身约 1970 行；同时处理内容替换、播放、MotionWarp 转换、快照恢复、表现图执行和诊断。

| 职责 | 输入 | 输出与状态归属 |
| --- | --- | --- |
| 内容准备与替换 | 作者 Timeline、资源与版本 | 可采用内容和版本；独立管理内容，不复制播放状态 |
| 播放生命周期 | 播放、推进、停止请求 | 唯一播放记录、待提交结果及动作归属 |
| 快照转换与恢复 | 正式播放状态或快照 | 快照或恢复操作；不维护第二份常驻播放状态 |
| 表现图执行 | 正式表现采样事实 | 待提交表现输出；不自主推进逻辑时钟 |
| 诊断投射 | 已提交播放结果 | 只读观察结果；不反向控制播放 |

Host 保留装配和业务调用顺序。提取完整职责后直接删除原方法和重复字段，不新增仅把同样参数转交回 Host 的包装，也不允许子模块任意读写 Host 全部状态。

业务收益是内容发布、表现输出、历史恢复可以分别理解和维护。代价是需要明确状态交接，尤其播放句柄、generation、动作身份和退休条件不能因拆类分裂。

## 三、迁移 TreeDesigner 剩余依赖

具体入口包括 [GameplayAbilitySemanticBlackboardEmitter](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterSimulation/Compilation/Semantic/GameplayAbilitySemanticBlackboardEmitter.cs) 对 BaseTree 与 Skill FlowGraph 两种输入的处理，以及 [Timeline Runtime 程序集](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Runtime/BTSMTL.Timeline.Runtime.asmdef) 的 TreeDesigner 引用。

输入是仍在使用的作者数据和序列化资产；输出是由正式业务模块拥有的唯一作者定义、编译输入与资源绑定。

实施时先定位正式入口与真实消费者，再确定类型属于公共定义、技能作者实现还是 Timeline 接入。代码、类型名、序列化身份、资产及程序集引用必须按同一职责一起迁移。没有消费者的直接删除，有消费者的完整迁移，不保留旧类型别名、转换兼容链或备用配置。

不能仅因文件夹名包含 TreeDesigner 就删除，也不能因一种旧节点暂时没有资产实例，就认定它不再是受支持的作者能力。通用端口、递归取值、动态端口和短路语义的保留范围要由正式作者与执行消费者确定。

收益是改一个变量或节点时只维护正式作者链；代价是涉及资产与编译输入迁移，比删除死文件范围更大。

## 其他大类候选与文件组织问题

行数为本次评估时近似值，不作为长期质量指标。

| 对象 | 观察 | 后续方向与取舍 |
| --- | --- | --- |
| [CharacterPoseGraphWorkspace](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.cs)，三个 partial 文件约 2304 行 | 页面、导航、选择、领域创建、保存、校验定位和调参集中 | 工作区负责页面与选择，领域操作回到现有 mutation 模块；保持唯一 Undo 与选择状态，不能靠新增窗口拆开 |
| [SimulationSessionHost](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Unity/SimulationSessionHost.cs)，约 1177 行 | Unity 生命周期中嵌入完整检查点恢复流程 | 可提取纯 C# 会话恢复事务；Host 保持唯一装配与暂停入口，不能新增会话拥有者 |
| [CharacterFootPlacementAnimationAnalyzer](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterSimulation/Analysis/FootPlacement/CharacterFootPlacementAnimationAnalyzer.cs)，约 2485 行 | Unity 采样环境与特征分析、曲线生成混合 | 明确采样输入与分析结果，保留唯一构建入口；避免为拆层复制大数组，本计划不修改算法 |
| [AnimationBlendStackRuntime](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation/BlendStack/AnimationBlendStackRuntime.cs)，约 2216 行 | 混合、打断历史与来源退休共同维护连续过渡 | 先看状态所有权，再提取职责；拆错会破坏打断连续性，不按长度硬拆 |
| [TimelineRuntimePreparation](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Runtime/TimelineRuntimePreparation.cs)，约 2529 行 | 同文件约 32 个类型，包含 Playback 与 Evaluator | 属于文件归属不清，可按已有类型职责归位，无需新增抽象 |
| [CharacterFootSwingMotionBuilder](../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation/FootPlacement/CharacterFootSwingMotionBuilder.cs)，约 2625 行 | 真正 Builder 约 450 行，前面大量合同与诊断类型 | 先区分合同、诊断与算法文件；搬文件不等于算法或性能优化 |

## 与现行规格对照

| 现行合同 | 对照结论 |
| --- | --- |
| [character-animation-pipeline](../openspec/specs/character-animation-pipeline/spec.md)：失败不发布局部状态、Animancer Evaluate 是不可逆门槛、Writer 后只执行预验证提交 | 发布 Faulted 后继续约束／来源提交，与该要求不一致。修正失败路径属于实现向现行合同对齐，不是放宽合同 |
| 同一规格：写明 FrameCoordinator 与根 GraphRuntime 保存阶段结果、RoleRuntime 单点驱动 Source、外层按阶段门消费 | 将纯转发合并或改变整体帧状态拥有者，会改变规格写明的具体结构。结构方案确定并实施时必须同步修改相关条款；本次只记录该差异，不提前宣称已迁移 |
| 同一规格：Source、Constraint、FinalPublication 唯一拥有资源、历史和输出；算法与数值顺序保持 | 本计划保留这些分工。不能借事务重构改 Foot／IK 算法、复制整份骨骼状态或引入第二写入器 |
| [btsmtl-timeline-direct-runtime](../openspec/specs/btsmtl-timeline-direct-runtime/spec.md)：Timeline 作者内容和正式播放语义 | Host 职责整理必须保持同一内容和播放事实，不新增 Timeline IR、自主时钟或备用运行路径 |
| [unity-simulation-assembly-ownership](../openspec/specs/unity-simulation-assembly-ownership/spec.md)：公共定义单向依赖、唯一序列化身份 | TreeDesigner 剩余迁移必须遵守类型迁移范围与资产身份规则，不能泛化已有例外或添加空壳程序集 |
| [graph-authoring-domain-framework](../openspec/specs/graph-authoring-domain-framework/spec.md)：UI 与 C# authoring 共用业务语义 | Workspace 整理只能复用正式领域修改入口，不能建立第二份字段、节点或保存协议 |

已归档的作者定义提取与旧窗口清理仍作为历史事实，不因本计划而重新打开。本文不把手动验收、回放或新增测试列成实施任务。实际实施前应重新读取当前工作区与调用链；遇到其他窗口已修改的同一职责或现行语义冲突，明确指出，不能覆盖。
