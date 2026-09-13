## Context

本变更承接已归档的 `minimize-csharp-authoring-reconstruction`。用户在完成讨论和文档更新后明确要求“让实现窗口做吧，设置goal”，现已授权绑定实现任务按本文完成正式曲线源迁移。

当前读取到的结构：RootMotionCurveAsset 已保存累计 XYZ/Yaw、时长、采样率和求值模式，现有烘焙器按秒写关键帧；MotionCurveClip 内嵌 XYZ/Yaw 并按归一化时间读取。MotionWarp、TimelineMotionEmitterRegistration 和 CharacterControlMotionCatalogEmitter 均直接读取嵌入曲线。BTSMTL.Timeline 已引用独立 RootMotion 程序集。

实现任务提供的样本统计是 Attack 约 659 KB、4274 个 Keyframe，其中约 4204 个来自 10 个 MotionCurveClip。该统计是讨论依据，本轮未重新测量，不作为实施完成或体积承诺。

## Goals / Non-Goals

目标是让正式素材唯一拥有运动数据，Timeline 与 C# 只表达数据段使用方式，并保持 Attack/Dodge 的运动内容与原有 compiled 运行链。

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

RootMotionCurveAsset 继续以秒表示源时间。Clip 保存源起止秒数及自身播放配置；映射定义由 Timeline 正式领域能力拥有，预览、Warp authoring 和两个编译入口消费同一含义，不在导出器内写另一份采样公式。

旧曲线归一化时间 u 对应源秒数 t = u × D，D 为该 Clip 原有曲线有效时长 `(CurveEndFrame - StartFrame) / TimelineUtility.FrameRate`。等价迁移转换关键帧 time，有限切线按 1/D 缩放，保留 value、权重、WeightedMode、常量段及 pre/post wrap；不是重新采样。零时长或非法区间定位具体 Clip 并失败，不以默认时长补齐。

原 Clip 的运动结束与片段结束可能不同。源采样达到区间末尾后保持终值，后续运动 delta 为零；Clip 的权重或占用生命周期仍按原正式时长处理。不能将 CurveEndFrame 简单并入 EndFrame 改掉这个行为。参数使用现有正式数据段/播放合同，不同时持有多个互相独立的时长真相。

FullLocalDelta 使用 XYZ 差值；ForwardDistanceYaw 使用 forward distance 差值及 yaw，遵守现行正式求值模式，不按字段名称猜测模式。源区间裁切时计算前后累计值差，不把裁切前的累计偏移当成新增位移。循环/倒放仅沿现有明确支持的规则，不额外添加模式。

### D4：统一所有消费链

```text
RootMotionCurveAsset（正式源）
    → MotionCurveClip（引用、区间、播放配置）
        → Timeline 作者采样／预览
        → MotionWarp 源窗口读取
        → Timeline semantic / ControlMotion catalog
            → 既有 Numeric Program → Gameplay Runtime → WorldSolver
```

源码修改点与职责：

- `Timeline.MotionCurve.cs`：Clip 源引用和时间映射，正式作者采样使用源数据；删除嵌入运动字段和旧读取。
- `TimelineAuthoringClipBinding.cs`、TimelineData 正式创建入口及 builder：类型化设置源与区间，继续承担 owner、合法性和保存。
- `Timeline.CurveAuthoring.cs` 及编辑投影：只编辑真正 Timeline-local 曲线；Position/Yaw 展示源引用及导航，不经 Timeline mutation 改源。
- `Timeline.MotionWarp.cs`：按绑定 Clip 的同一源区间读取累计位移/yaw，保留窗口、权重和空间规则。
- `TimelineMotionEmitterRegistration.cs`、`CharacterControlMotionCatalogEmitter.cs`：从正式源和同一映射降低运动数据；已有 Motion 节点等源消费者沿同一求值模式，不恢复内嵌副本。
- 正式编译依赖与 stale/hash：包括源内容及区间配置，源修改应使相关编译产物失效。仅按引用路径计算身份不足以发现源曲线变化。

“共同读取同一曲线源”指 authoring/编译的唯一输入。Gameplay Runtime 继续只读 compiled constants/operation；编译器允许生成既有数值目标所需的数据，这些数据不进入 authoring C#，也不成为第二份可编辑素材。

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

```csharp
timeline.Motion(attackMotion)
    .Range(0.2f, 0.8f)
    .At(0f)
    .Speed(1.2f);
```

### D7：作者界面定位真实 owner

Timeline 提供类型化源选择、源区间/播放配置和“打开源资产”导航。源运动曲线可只读展示，但不注册为 Timeline-local 可写 channel。正式源编辑入口负责整组曲线、Undo、保存和依赖失效；不在 OnInspectorGUI 做采样、迁移或重烘焙。

业务取舍：作者编辑权重仍在片段就地完成；修改共享运动需进入源 owner，避免一次局部拖动意外改了所有引用者。需要独立素材时明确创建，不能自动复制。

## Risks / Trade-offs

- 改变共享源会影响多个片段 → 保持真实源 owner 与显式独立资源创建，更新既有编译依赖失效。
- 只换字段未换时间单位 → 迁移完整时间与插值语义，预览、Warp 和编译共用映射。
- 只改 Timeline emitter 漏掉 ControlMotion → 同步两条正式编译入口与所有直接读取者，Runtime 保持 Program 路径。
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
