# Timeline直接内容运行：职责与公共合同

## 1. 接收范围与授权状态

2026-09-14依据[协调审阅PARALLEL-20260914-DOMAIN-01](../../../docs/coordination-progress.md)（2801861c1）与[主方案D9—D13](../replace-character-program-with-domain-runtimes/design.md)，在既有Timeline规划中登记新增Runtime职责。既有规划任务为01a095ac-88a4-7bd3-abbf-197b9058c1ca，实现任务仍为timeline（01a089db-81e3-7a73-ae52-82ef95b744d4），不新建任务。

本次只授权更新规划、任务归属与规范增量，不启动Runtime实现，不向实现任务发送执行消息。此前Slate UI已授权工作及正确提交保留，不因新增范围重新回退或重做。Runtime唯一执行清单放本change的tasks第12节；本文只有合同与分工，主方案1.5—1.7、3.8、8.2中的Timeline域内部分由该节承接，不复制第二套勾选清单。

| 工作线 | 接收内容 | 不接管内容 |
|---|---|---|
| 原Slate UI线 | 轨道/Clip/曲线/手势、编辑Session/Undo、typed字段、源导航、截图缺陷 | 不能用Slate Cutscene Runtime执行正式内容 |
| 新Timeline Runtime线 | 只读内容与portable表示、实例准备/创建、推进、循环/Section、窗口/取消、TreeClip调用、播放私有状态、Timeline专属发射退出 | 不编译技能图，不实现角色总Step/快照，不改Motion/Warp/Camera领域算法 |

## 2. 运行方案：一份内容、一个调度模块

TimelineData是唯一可写作者模型。运行时直接消费其正式轨道、Clip类型、区间、参数、顺序、稳定身份和资源引用的只读内容；不将轨道/Clip展开成Semantic operations，也不在加载时再生成操作表。技能图的“调用Timeline”仍可保留技能调用操作，但只携带内容identity/revision与入参。

普通.NET使用同字段语义的一对一portable序列化表示：Unity对象转换成稳定资源/技能引用；数据不包含Unity对象、Slate类型、Editor对象、IR、控制流、状态槽或可变播放状态。它不是另一份可编辑资产，也不是把旧ProgramPlan改个名字。内容解析/校验与资源绑定可以建立派生索引，但不能产生第二种Timeline执行语言。

技能与真实非Skill调用方使用同一Runtime，仅调用身份、目标和能力输入不同；不恢复旧TimelinePlayer/TimelineRunningTree自主播放，不建立预览专用播放器。Float32/Fixed共享边界遍历和Clip生命周期含义，只在数值与资源准备处做分型绑定，不各自复制运动映射或Clip逻辑。

## 3. 输入输出合同

以下名称描述公共操作，不声称当前已存在相同C#签名。接口分型，不用任意object字典或全角色万能Context。

| 操作 | 输入 | 输出与副作用限制 |
|---|---|---|
| Prepare | RequestId、Timeline identity/精确revision、只读内容、NumericTarget、外部资源/成员合同与版本、唯一运动映射、TreeClip执行服务需求 | Pending/Ready/Missing/Invalid/Failed、精确来源/字段/原因码；仅Ready带PreparedBinding。只解析/准备，不推进或发布Gameplay |
| CreatePlayback | Ready绑定、调用方身份、ActionInstance或真实非Skill调用身份、播放实例ID/generation、目标/typed入参、Once/Loop与正式区间 | 独立播放实例、实际内容/资源版本与绑定身份。不得将独立Timeline准备伪装成Ability Prepare，不为非Skill造空技能或假Actor |
| Advance | 播放实例、正式Step/阶段身份、调用方提供的时间增量/区间、同一步typed输入与TreeClip服务 | 待提交cursor/loop/活动Clip变化、窗口与Motion等领域贡献、TreeClip调用结果/待提交状态、表现请求和trace候选；不调用CommitFrame提前发布 |
| Commit | 本次Advance结果与同一Step接受凭据，且调用方已满足角色/world提交条件 | 只安装Timeline本实例的候选状态，向正式调用方交付可发布结果；角色/World和表现发布仍由各自唯一owner执行 |
| Discard | 同一Step候选或失败/取消原因 | 丢弃本次私有候选、TreeClip服务候选及输出，保持上次committed播放状态；不得遗漏已暂存的窗口/序号/调用状态 |
| Stop | 精确播放身份、正式自然结束/停止/强停/ActionContextEnded等原因与调用方阶段 | 关闭该实例活动窗口/TreeClip并产生相应终止候选；由同一调用方提交/丢弃协议接受，不在Stop中绕过角色事务发布Gameplay |
| Capture/Restore | 分型已提交播放状态；恢复时携带内容revision、NumericTarget、schema、实例身份及重绑所需资源/TreeClip服务 | Timeline私有快照或经校验的恢复候选；角色核心组合总快照并决定最终安装，Timeline不自行覆盖角色/World或推进表现图 |

Ready与实际创建/安装是不同事实。Timeline owner确认实际Playback内容版本、资源版本与generation后报告实际绑定；角色主实现汇集而不重新计算版本，预览只读取。内容改变不悄悄替换活动播放；重建/重新准备按调用方和Session正式规则执行，不生成假ProgramEpoch。

## 4. Step事务与TreeClip的具体分工

```text
角色/非Skill正式调用方开启本次Step
  → Timeline.Advance：边界遍历、Decision与候选输出
  → 主实现TreeClip技能服务：执行子调用并保留该Step候选
  → 角色核心消费Motion/窗口等候选，完成WorldResolve及提交判断
  → 接受：Timeline.Commit + 核心提交角色/World并发布正式结果
  → 失败：Timeline.Discard + 核心丢弃本Step候选
```

此图是责任顺序，不新增第二个Step调度器。具体Decision/Commit阶段沿既有领域顺序与主实现统一Step协议接入；Timeline Commit函数不能再执行第二遍技能逻辑。

Timeline决定某个TreeClip何时Enter/Update/Exit/Destroy、属于哪个循环/调用身份以及取消如何传播。TreeClip引用已独立编译的技能执行入口；技能代码准备、provider解析与实际图执行服务由主实现提供。接口应传稳定ClipId、父播放identity、循环/调用generation、阶段与typed入参，返回状态/结果及该服务的候选或正式调用引用。

TreeClip Decision结果可以作为同Step内部输入，不允许提前发布角色事实。子调用的技能私有状态由技能服务唯一拥有，Timeline快照保存关联身份与自己的生命周期位置；需要恢复技能帧/局部变量时由核心对应快照分区恢复，不能两边各存一份可写技能状态。请求接受不等于已执行/已提交，停止完成也不能只清一个UI状态。

非Skill调用方同样必须提供正式提交生命周期和所需执行能力。没有角色Gameplay能力的调用不能接收要求角色窗口/战斗服务的Clip，Prepare应准确拒绝；不能通过空服务“执行成功”。

## 5. 时间、循环、停止与资源

- 作者帧、秒、Logic Tick、ClipIn、播放速率、Section/loop和前后采样区间由唯一Timeline时间owner处理，保留已有算法顺序。单Tick跨多个Clip/循环边界，按原稳定顺序完整处理尾段、整循环和头段；相同边界不重复触发或漏掉退出。
- RootMotionCurveAsset与正式源区间/映射已完成迁移，直接消费现行源API。源区间结束后保持累计终值、后续delta为零；Clip自身权重/占用仍按自身边界处理，不将两个结束时间混同。
- 同一源被两次使用仍有不同Clip/Warp调用身份。Motion/Warp运算与Camera效果解释交给各自原owner；Runtime只调度这些typed输入和输出，不抄采样或求解公式。
- Stop只作用于指定播放generation；Action context失效后不能再产出旧窗口、运动、Cue或结果。退出/取消清理属于同一生命周期，有限动画淡出尾部归原Slot/ActionPlayback，不为等待视觉结束继续开放Gameplay窗口。
- 直接内容运行不自动增加“任意未知时长Clip/开放Track”新语义。若现有正式合同尚未提供这种生命周期，继续明确为独立缺口；不能填巨大EndFrame、画无限条或借用循环冒充该功能。

## 6. 播放私有状态与分型恢复

每次播放独立保存已提交cursor与上次采样边界、循环/Section位置、活动Clip及生命周期阶段、已开窗口、TreeClip调用关联、停止原因/终态、实例generation与内容版本，以及实际会影响后续Tick的私有数据。共享内容不能存这些字段。

Float32/Fixed快照保持对应数值精度与schema，不经float中转Fixed状态；只捕获已提交状态，Pending输出、Unity对象、缓存索引和GUI游标不入快照。角色核心负责角色/Timeline/技能服务/World各分区的完整候选校验和原子安装；Timeline只提供自己的Capture及恢复候选，不新增第二套角色codec或恢复协调器。

恢复先检查目标数值类型、内容/资源revision、schema、实例/服务关联；不兼容明确失败，不静默加载旧Program或从当前作者资产猜测旧内容。恢复不得重发已提交窗口/事件或重新推进Pose。恢复索引从同一精确内容重建，属于派生缓存而不是另一份状态真相。

## 7. 唯一文件Owner

路径相对Assets/GameScripts/Main；新类型按现有模块组织，本文不要求新建第二个Runtime目录。

| 文件/符号 | 唯一写入者 | 交接规则 |
|---|---|---|
| Runtime/BTSMTL/Timeline/Runtime下直接内容/portable内容/播放私有状态、Float32TimelinePlayback.cs、FixedTimelinePlayback.cs | 本任务Timeline Runtime线 | 迁移现有算法，去掉ProgramPlan/operation前提；不新增兼容模式 |
| Runtime/Simulation/Core/Execution/TimelineControlRuntime.cs及Timeline专属发射适配 | 本任务Timeline Runtime线 | 只处理Timeline职责；涉及通用Evaluator/Workspace/Program codec的改动给主实现集成，不占整个Simulation目录 |
| BtsmtlSkillTimelineCompiler.cs与共享技能编译/调用入口、TreeClip技能服务 | 原主实现01a09a5f-8d64-7c11-a461-7889623b7459 | Timeline提供内容及调用合同，主实现删除共同入口的内部发射并接技能调用，不能双方各删半个文件 |
| CharacterPipelineHost、领域Factory/Instance、Step、角色状态/codec、网络checkpoint/manifest、共享artifact及Character Build删除 | 原主实现 | Timeline只提供接口、私有状态与准确调用需求，不代改共享Host |
| Timeline.MotionCurve.cs、Timeline.MotionWarp.cs、RootMotionCurveAsset及源配置/映射 | 原运动源owner | 已归档迁移不重开，复用现行API；具体字段变化由原owner写入 |
| Timeline.Camera.cs、CameraBuilder/payload、Prepare/Adopt/Reset与相机算法 | Camera原owner | Timeline只调度正式片段，不能顺手删除Camera emitter中的业务资源处理 |
| Slate源码、编辑Session/UI适配 | 本任务原UI线 | 与Runtime同任务但不同职责，不修改采样公式或提供Slate运行fallback |
| ScenePlay协调器/观察生命周期；公共C#输出生成与Corin资产迁移 | 分别为预览原owner、C# authoring原owner | 消费同一正式接口，不由Timeline重复构建场景、生成资产或运行实例 |

TimelineSemanticEmitterRegistry等文件若混合Camera/Motion或共享编译职责，实施前按符号划定现有唯一写入者：本任务只迁出Timeline专属发射，跨域部分由原owner接续。不能因为“发射器”名称就整文件删除。发生实际同文件冲突保留现场交用户决定，不覆盖并行改动。

## 8. 现行规范替代与依赖

| 现行要求 | 处理 |
|---|---|
| btsmtl-runnable-timeline-node将Timeline生命周期/TreeClip编为operation并保存在Character state slots | 由新直接Runtime合同替代内部发射、播放状态与执行方式；保留调用节点不自行tick作者资产、inline/shared、隔离、完成/取消等业务含义。主方案负责涉及技能节点/共享运行入口的delta，本任务只定义Runtime实现合同 |
| btsmtl-gameplay-semantic-ir要求Timeline/MotionWarp发射为IR；btsmtl-compiled-simulation-program要求全部Timeline状态槽及Program/Projection挂接 | 主方案D9及其全局delta删除旧执行载体。本Runtime不等待旧Program构建；不能将只读portable内容重命名成Program继续解释operations |
| 原Slate设计只负责编辑、不承担播放 | UI本身仍不执行；本任务新增独立Runtime职责与tasks第12节，不把原UI goal完成状态当作Runtime已完成 |
| 已归档unify-timeline-motion-curve-source设计路径 | 只作历史追溯；读取现行character-root-motion-curves与相关Timeline规范/正式API，不再按旧“编进总Program”措辞开发 |
| 原预览联动要求纯Timeline有真实调用方但准备接口未明确 | 增补独立Timeline Prepare/CreatePlayback及实际版本报告，不借用Ability Prepare或造空Skill；由原预览owner接操作 |

依赖未完成时准确记录缺哪个typed接口或共享入口，不补旧Program适配器/空执行服务。D11曾交付的独立Ability前端和两个目标store保留事实，但它们仍返回旧容器的部分不能当作最终新技能服务。D12审查属于主实现整改，Timeline不复制Q1—Q4待办。

主方案只保留职责指针与公共集成项，本任务tasks第12节是Runtime域内唯一勾选入口。后续是否启动由用户另行授权；此次不派工、不运行Unity/Build、不新增测试或验证任务。编译/刷新若后续实施需要，按协调指定主实现组织，编译期间暂停源码写入，不新建锁或验证服务。
