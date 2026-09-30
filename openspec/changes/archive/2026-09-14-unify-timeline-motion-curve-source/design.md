## Context

本变更承接已归档的 `minimize-csharp-authoring-reconstruction`。用户在完成讨论和文档更新后明确要求“让实现窗口做吧，设置goal”，现已授权绑定实现任务按本文完成正式曲线源迁移。

本次 `camera-preview-timeline-domain-runtime-r1` 协调仅更新规划，统一基线为 [领域运行设计](../2026-09-17-replace-character-program-with-domain-runtimes/design.md)。角色总 Program 与整包 Projection 退役，技能独立编译、控制直接 C#、Pose 使用原生 FlowCanvas Runtime；网络 Pipeline/Pass、Float32/Fixed 与独立资源处理保留。本次文档对齐不扩大既有实现授权，不下发实现消息，也不代表下述新接口已经落地。

初次规划的源码基线：RootMotionCurveAsset 已保存累计 XYZ/Yaw、时长、采样率和求值模式，烘焙器按秒写关键帧；MotionCurveClip 内嵌 XYZ/Yaw 并按归一化时间读取。MotionWarp 与旧编译入口直接读取嵌入曲线。BTSMTL.Timeline 已引用独立 RootMotion 程序集。此段是问题来源，不声明当前代码仍处于该状态，目标接续以 D4/D8 为准。

实现任务提供的样本统计是 Attack 约 659 KB、4274 个 Keyframe，其中约 4204 个来自 10 个 MotionCurveClip。该统计是讨论依据，本轮未重新测量，不作为实施完成或体积承诺。

## Goals / Non-Goals

目标是让正式素材唯一拥有运动数据，Timeline 与 C# 只表达数据段使用方式，保持 Attack/Dodge 运动语义，并分别接入独立技能内容与正式 Control/Motion 资源绑定。

不新建 Timeline 专属曲线类型、通用曲线运行时、资源自动扫描、JSON 中转、兼容字段或 fallback。不抽点、不丢曲线、不为迁移重新烘焙动画，不把 Runtime 改为读取 Unity 资产。不增加测试或验证任务。

## Decisions

### D1：复用现有 RootMotionCurveAsset

| 方案 | 业务收益 | 业务代价 |
| --- | --- | --- |
| 复用 RootMotionCurveAsset，本次采用 | 动作节点、Timeline 共同引用一种运动素材，曲线修改集中在真实 owner | 明确时间单位与求值模式，正式 API 需承接无损导入 |
| 定义 Timeline 专属曲线源，不采用 | 可直接沿用内嵌归一化曲线语义，初次承接更直接 | 同一运动形成两类素材，烘焙、编辑、引用与编译需分别维护 |

Timeline 区间、播放位置和混合是“怎样使用素材”，不是第二种素材业务。RootMotionCurveAsset 保持唯一数据载体；现有类型语义确实不能承接某条存量内容时报告具体冲突，不把 World/local 或求值模式悄悄改成可接受值。

### D2：源资产、Clip 和 Timeline 的所有权

| 对象 | 拥有内容 | 修改含义 |
| --- | --- | --- |
| RootMotionCurveAsset | 累计运动曲线、源时长、明确求值模式和可确认的来源信息 | 修改源运动会影响全部引用者 |
| MotionCurveClip | 类型化源引用、源区间、Timeline 位置、播放映射、通道、空间、优先级、混合、Weight/Ease | 修改一次使用，不修改共享源 |
| TimelineData | Track、Clip、局部顺序和 owner 关系 | 删除或重建 Timeline 不删除外部源资产 |
| MotionWarpClip | 同 owner 内具体 MotionCurveClip 的稳定引用及自己的 Warp 参数 | 同一源素材使用两次仍是两个不同 Warp 绑定目标 |

PositionX/Y/Z/Yaw 不再是 Clip 内嵌字段；不保留“有源读源、无源读旧字段”的双读。Weight/Ease 和 Warp progress 仍是各自 owner 的独立作者曲线，不一并外移。

### D3：时间映射与求值共用一个正式定义

RootMotionCurveAsset 继续以秒表示源时间。Clip 保存源起止秒数及自身播放配置；映射定义由 Timeline 正式领域能力唯一拥有，技能内容处理、Control/Motion 资源绑定与 Warp 使用同一含义，预览消费正式采用结果，不在导出器或消费者内写另一份采样公式。

旧曲线归一化时间 u 对应源秒数 t = u × D，D 为该 Clip 原有曲线有效时长 `(CurveEndFrame - StartFrame) / TimelineUtility.FrameRate`。等价迁移转换关键帧 time，有限切线按 1/D 缩放，保留 value、权重、WeightedMode、常量段及 pre/post wrap；不是重新采样。零时长或非法区间定位具体 Clip 并失败，不以默认时长补齐。

原 Clip 的运动结束与片段结束可能不同。源采样达到区间末尾后保持终值，后续运动 delta 为零；Clip 的权重或占用生命周期仍按原正式时长处理。不能将 CurveEndFrame 简单并入 EndFrame 改掉这个行为。参数使用现有正式数据段/播放合同，不同时持有多个互相独立的时长真相。

FullLocalDelta 使用 XYZ 差值；ForwardDistanceYaw 使用 forward distance 差值及 yaw，遵守现行正式求值模式，不按字段名称猜测模式。源区间裁切时计算前后累计值差，不把裁切前的累计偏移当成新增位移。循环/倒放仅沿现有明确支持的规则，不额外添加模式。

### D4：统一所有消费链

```text
RootMotionCurveAsset（正式源）
    → MotionCurveClip（引用、区间、播放配置）
        → Timeline 唯一时间映射与 typed 源配置
        → MotionWarp 源窗口读取
        → 独立技能内容入口 → 技能数值目标数据／执行
        → 正式 Control/Motion 资源绑定 → C# 控制／运动执行
            → 既有领域运动算法 → Pipeline/Pass → WorldSolver
正式采用的领域绑定／运行结果 → 预览只读观察
```

源码修改点与职责：

- `Timeline.MotionCurve.cs`：Clip 源引用和时间映射，正式作者采样使用源数据；删除嵌入运动字段和旧读取。
- `TimelineAuthoringClipBinding.cs`、TimelineData 正式创建入口及 builder：类型化设置源与区间，继续承担 owner、合法性和保存。
- 源 typed 配置与字段描述由本任务提供；Timeline UI 只消费这些字段，局部 Weight/Ease 就地编辑，Position/Yaw 导航到源 owner，不并行修改源或时间映射语义。
- `Timeline.MotionWarp.cs`：按绑定 Clip 的同一源区间读取累计位移/yaw，保留窗口、权重和空间规则。
- 技能侧由编译收窄任务拥有的独立技能内容入口消费；控制侧由正式 Control/Motion 资源绑定消费。本任务声明源内容、源区间修订及依赖结果，具体新消费绑定由编译收窄 owner 定义并接续。
- 源内容及区间修订应使实际依赖的技能数据或控制资源绑定需要更新；调用方只能采用与所需修订匹配的结果。仅路径相同不足以判定内容未变，不新建全角色 hash 或总资源目录。

“共同读取同一曲线源”指技能和控制共享同一作者输入。portable Gameplay 只读正式采用的领域运行数据或绑定，不回读 Unity 资产；技能数值降低与独立运动资源处理保留，但都不进入 authoring C# 或成为第二份作者源。旧 CharacterControlMotionCatalogEmitter 与角色总 Program 不再是迁移目标，不恢复 Character 全量 Build、Projection 总包或换名总包。不能为等待新接口保留嵌入字段或双读。

### D5：Attack/Dodge 一次无损迁移

迁移是明确的数据迁移，不是 export_code 的隐式副作用。使用正式资产创建/保存能力，把原内嵌完整曲线采用为独立的 RootMotionCurveAsset。它是有正式消费者的素材，不是把大数组藏到临时文件。

1. 对明确的 Attack/Dodge 及受字段删除影响的正式 Clip，建立旧 Clip → 正式源的迁移对应。已有资产只有在数据、求值模式和时间含义相同且共享意图明确时才复用；不凭同名动画合并。
2. 已知 source AnimationClip 和采样信息原样保留；未知时明确无可重烘焙来源，不虚构动画或采样率。通过正式导入职责承接存量数据，现有动画烘焙入口仍要求其完整输入。不要用 SetBakedData 的默认补零掩盖缺失曲线，正式拷贝须保留 wrap 等完整语义。
3. 将数据和引用作为一次明确迁移保存，保留 Clip 身份、MotionWarp 绑定及播放配置。源未保存或引用未写完时不能删除原数据；失败报告具体未完成范围。
4. 所有受影响消费者和正式数据完成切换后，删除嵌入字段、旧作者通道、旧导出/读取分支与一次性旧格式迁移工具。小步提交可追溯，不把兼容读取留作常驻实现。

需要独立调整某次使用的运动内容时显式创建独立源资产；不是在 Clip 内重新开四条覆盖数组。Timeline 的权重与淡入淡出照常局部编辑。

### D6：两工具与链式 builder

export_code 只输出一次类型化 RootMotionCurveAsset 引用及片段 builder 配置，不输出源关键帧。generate_assets 解析源、创建 Clip、设置数据段并保存 Timeline；不提取或重烘焙源，不把源加入隐式删除范围。缺失源直接诊断，不能恢复为嵌入曲线。显式曲线源迁移与普通生成是不同操作职责，但只有一条正式资产创建和运行链。

继续采用已确认的偏函数式链式 builder、作者非默认值/有意义覆盖、每节点一份位置及共享对象先创建后连接。下面只示意阅读形式，不新增命名合同：

已有正确的 builder 与生成入口保留，公共接口变更由其唯一 owner 接续；本任务只消费正式能力，不重建另一套入口。人工编辑、保存和资源修订不会自动导出 C#，两工具仍仅显式调用。

```csharp
timeline.Motion(attackMotion)
    .Range(0.2f, 0.8f)
    .At(0f)
    .Speed(1.2f);
```

### D7：作者界面定位真实 owner

Timeline 提供类型化源选择、源区间/播放配置和“打开源资产”导航。源运动曲线可只读展示，但不注册为 Timeline-local 可写 channel。正式源编辑入口负责整组曲线、Undo、保存和依赖失效；不在 OnInspectorGUI 做采样、迁移或重烘焙。

业务取舍：作者编辑权重仍在片段就地完成；修改共享运动需进入源 owner，避免一次局部拖动意外改了所有引用者。需要独立素材时明确创建，不能自动复制。

### D8：共享文件、同批资产与真实接口缺口

| 负责方 | 唯一职责 | 本任务如何接续 |
| --- | --- | --- |
| 本曲线源任务 | Timeline.MotionCurve.cs、Timeline.MotionWarp.cs、相关源配置/binding、同批 Corin Timeline 资产及生成源码迁移 | 统一写入一次，不分窗口分别重建同一资产 |
| 编译收窄任务 | 独立技能内容入口、Control/Motion 新消费绑定及数值目标接入 | 提交源内容/区间修订和依赖结果，消费 owner 发布的正式接口，不改旧总包作为过渡 |
| Timeline UI | typed 字段展示、局部作者操作和源 owner 导航 | 只消费本任务正式字段与映射，不自行增加源语义 |
| Camera 任务 | Timeline.Camera.cs、镜头资源与请求 API、精确源动作/事件到工程 Clip 和 ResourceId 的映射 | 映射具备后由本任务统一写入同批 Timeline 资产，Camera 不再另行重建覆盖 |
| 预览任务 | 正式采用状态与运行结果展示 | 不生成目录、源副本或临时可运行绑定，不展示未采用数据为生效 |

仍需由相应 owner 成文的接口是：独立技能入口接受源/区间依赖的具体合同；Control/Motion 资源绑定的 portable 载体与采用结果；源内容/区间修订与依赖结果的实际字段；Camera 的精确动作/事件、目标 Clip、ResourceId 映射。当前广播未提供这些具体名称和映射，本文不编造。缺口记录在此，不新增测试或汇报任务，不通过旁路、旧总包或 guessed ResourceId 填补。此协调仅规划，不为获取接口向其它窗口索取汇报。

## Risks / Trade-offs

- 改变共享源会影响技能和控制 → 声明真实修订及依赖结果，由各领域正式采用，不重新生成角色总包。
- 只换字段未换时间单位 → 迁移完整时间与插值语义，预览、Warp 和编译共用映射。
- 消费绑定尚未定型 → 在 D8 记录真实缺口，由唯一 owner 接续，portable 不回读 Unity 资产，也不留旧内嵌双读。
- Camera 与曲线迁移重建同批资产 → Camera 给出精确映射，本任务作为同批 Timeline 资产唯一写入者合并落地。
- 不明确来源却重新烘焙 → 存量曲线作为迁移输入，保留可确认来源；不以动画重烘焙替换旧结果。
- 与其它任务同字段冲突 → 保留现场交用户决定，不回退已正确内容。

## Migration Plan

先补正式源无损导入与 Clip 绑定/映射，再统一作者界面和消费者，然后迁移存量资产并删除嵌入路径，最后重新导出正式 C# 与同步当前规范。实现每步按清楚职责中文提交，只提交本步拥有的差异；不新增测试、验证或回放任务，不执行破坏性 Git 拆分。

## Implementation Binding

- planning_thread_id: `01a09634-fc59-7192-8cda-25fdd142b82d`
- implementation_thread_id: `01a09635-2024-74b2-98b4-28c1e17d548b`
- implementation_authorization: 用户明确要求“让实现窗口做吧，设置goal”。
- goal_objective: 按 unify-timeline-motion-curve-source 的 proposal、design、specs 和 tasks 完成正式外部运动曲线源统一；无损迁移 Attack/Dodge 及受影响数据，统一 Timeline、MotionWarp、编译和作者工具，删除旧嵌入路径，重新导出正式 C# 并同步文档，按职责中文小步提交。
- goal_budget: 用户未指定预算，不设置 token_budget。
- completion_boundary: 全部授权实现和正式数据迁移完成后才将 goal 标为 complete；不把仅写文档、只改导出器或仅编译成功当作完成。不新增用户未要求的测试或验收任务。
- communication: 只调度上述绑定实现任务；无须回执，不转发其它任务。实际业务冲突保留现场交用户决定。
- coordination_revision: `camera-preview-timeline-domain-runtime-r1`
- coordination_mode: `PLAN_ONLY`；本次不发送 IMPLEMENT_FROM_DOCUMENT 或 DOCUMENT_UPDATED，不改变已运行 goal 或扩大实现授权。
