# Timeline 秒制作者时间、时钟域与表现采样

## Why

Timeline 当前仍以固定 60 作者帧记录内容，只能换算不同 SimulationTickRate，内容精度不会随运行频率提高。用户已确定改为秒制作者时间，解除固定 60 帧存储限制。当前 Timeline 表现游标与动作播放器还分别计算进度，表现 Marker 的图执行与停止链路也有缺口；换时间单位本身不会解决这些运行问题。

## What Changes

- **BREAKING**：Timeline 的起点、时长、Marker、Section、ClipIn 和 Timeline 自有时间坐标统一用秒表达和保存，采用现有 Fixed 合同一致的 Q32.32 与 nearest-even 舍入。帧只作为编辑显示、素材来源或吸附单位，不保留帧与秒双写，不生成独立 tick 版作者资产；逻辑保留换算余数，不使用 float 反复累加。
- **BREAKING**：明确 Timeline 被动求值。原逻辑／表现播放管理者决定前后动作秒数、经过与推进原因，Timeline 接收结果遍历内容边界并产生候选，不自行读取时间源或累计另一份游标。逻辑调用仍在 SimulationTick 提交，表现调用仍在表现帧发生，tick 编号保留为模拟身份；播放状态和求值状态在同一正式事务与快照链中保持一致。
- 子弹时间、暂停与动作倍率由正式播放控制改变游标推进；不修改 Unity Time.timeScale、fixedDeltaTime 或为此改变逻辑 tick 率。动作倍率与 Clip 源采样倍率分开，共享动作采样只应用一次有效动作倍率，不把时间控制复制到消费者。
- 对同一动作播放实例统一计算表现采样结果，动作动画、Timeline 表现 Marker、随该动作采样的 Camera 内容消费同一结果。Clip 通过自己的起点、ClipIn 与源映射得到源动画时间；locomotion、独立特效和混合过渡继续由原 owner 管理。
- 表现进度来源与修正方式由业务在正式装配处显式选择。比较 committed 跟随、表现 delta 自由推进、向目标进度有界追赶的业务取舍；不按“单机 / MMO / ACT”在播放器中硬编码，不擅自选择一个全项目默认模式。
- 现行有限 Action 的 committed sample 合同继续有效；locomotion 继续消费自己的 prepared binding。自由推进与追赶的适用范围不得覆盖这些既有约束，扩展有限 Action 的自由播放须另行修改对应现行 spec。
- **BREAKING**：将现有表现时钟策略中的进度计算与动画播放器写入分开，移除同一动作在 Timeline 与播放器内各自累加的路径；不另建一套同义时钟接口、播放注册表或生命周期。
- Marker 继续与 Clip 同级、跟随 Track 执行域，触发图只有 OnEnable。Logic Marker 走原 Advance / Commit；Presentation Marker 使用正式表现执行上下文，不借用只在 Logic Tick 内有效的技能 invoker，不调用 Simulation Evaluate / Finalize。
- 补齐停止、取消、分支修正、循环与重复采样的事件语义。终态生效后禁止旧播放再产生新 Marker；已生成表现的收尾归原业务 owner，修正采样不自动成为新的事件经过。
- 补齐现有 Track 的 Domain 编辑。通过原作者 mutation 同步校验 Track、Clip、Marker 图的域能力，失败保留原内容并指出不兼容项；Domain 不同时充当时钟策略开关。
- 统一拖动、秒输入与正式保存。允许逻辑 tick 吸附自动读取当前绑定 pipeline 的 SimulationTickRate，也允许按素材帧或关闭吸附；网格不限制保存精度。配置变化只更新网格，显式重新对齐才修改选中内容并支持 Undo。迁移资产、闭包、指纹、作者 API、导出重建和运行消费者后删除旧帧存储路径。
- 沿既有播放管理者和运行实例实现上述边界；推进、共享采样、区间求值与事件输出热路径遵守 0 GC，不新增并列时钟、Registry 或临时事件系统。

## Capabilities

### New Capabilities

- btsmtl-timeline-clock-domain：作者时间与运行调度的区分、确定性换算、业务时钟策略边界、共享表现采样、Marker 事件生命周期、Domain 作者入口及正式下游消费。

### Modified Capabilities

- btsmtl-timeline-direct-runtime：双域直读同一内容、每个播放实例的时间所有权、停止与表现收尾边界，TreeClip 节点直接提交正式领域输出。
- btsmtl-runnable-timeline-node：移除 Scale 模型描述，限定 Logic TimelineBody 与表现安全 Marker 图的执行能力。
- character-animation-pipeline：同一动作的表现采样一致、同级 Marker、表现事务与 Gameplay 状态隔离。
- btsmtl-timeline-editor-preview：以秒保存作者时间，帧仅负责显示与吸附，Slate mutation 和 Undo 使用同一正式秒制模型。

## Impact

- 运行链涉及 TimelineRuntimePresentationDriver、CharacterTimelineHost、既有 Action 表现时钟策略、Action sample history / projector、Pose Player 消费与 Camera bridge。只复用正式 owner，不新增全局时间服务。
- 作者链涉及 TimelineData 及类型字段、资产和生成代码、内容闭包与指纹、Slate binding、Track Inspector、正式 Timeline mutation 及既有 C# authoring 导出 / 重建。私有 Marker 图仍随正式 owner 闭包管理。
- 本轮先同步本 change 的 proposal、design、delta spec、tasks 与现行 project/spec 边界，不修改代码和资产，不归档，不把待实施合同写成已完成事实。

### 与现行 spec 的对账

| 现行合同 | 本次处理 |
|---|---|
| gameplay-tick-system：固定 SimulationTick，允许正式 Scaled / Unscaled 时间来源，Presentation 不调用 Kernel Evaluate / Finalize | 保持；业务慢动作不写 Unity 全局时间，不暗改现行 Scaled 默认配置，表现 Marker 不借用逻辑 evaluator |
| btsmtl-timeline-editor-preview：要求整数作者帧和 StartFrame 保存，Slate 提交 frame/value mutation | 与秒制目标冲突；通过本 change 的 MODIFIED requirements 改为秒制保存与 mutation，帧显示不再限制保存精度 |
| character-presentation-interpolation：有限 Action 基于 committed raw sample 投影 | 保持；本变更不能把该类 Action 无条件改成自由推进 |
| character-presentation-interpolation：locomotion plan / prepared binding 与 Body correction 独立 | 保持；不把所有 Pose Player 合并成一个动作时钟，不恢复单 Clip 的策略配置 |
| character-animation-pipeline：旧条款仍描述 Clip 下的 Presentation Marker | 通过本 change 的同名 MODIFIED requirement 改为 Track 下的同级 Marker |
| btsmtl-runnable-timeline-node：数据模型仍列出 scale | 通过本 change 的 MODIFIED requirement 删除已废弃字段描述 |
| btsmtl-timeline-direct-runtime：Timeline 唯一时间 owner 管理帧/秒/Tick，ActionCue payload 包含 frame/cycle | 旧 owner 与旧 Timeline Cue 事件路径都与现行 TreeClip 节点边界冲突；delta 明确播放管理者决定进度，Timeline 管内容映射和求值状态，TreeClip 节点经正式 domain emitter 在同事务提交 |

旧文档中的“本轮保留固定 60 作者帧、不迁移秒存储”“独立表现时钟已否决”“普通表现一律自由播放”“不同域不得共享采样来源”不再作为决策。秒制及 Q32.32 数值合同已经确定；共享采样具体 owner 和表现图接入仍需沿代码落实，见 [design.md](design.md)。实现清单见 [tasks.md](tasks.md)。主 specs 已同步为 TreeClip 节点边界；本次 delta 尚未归档，不代表代码已采用目标合同。
