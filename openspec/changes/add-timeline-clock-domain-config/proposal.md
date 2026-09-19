# Timeline 时钟域、表现采样与作者闭环

## Why

Timeline 已有固定作者帧、可配置 SimulationTick 换算、Logic / Presentation 执行域和同级 Marker，但当前 Timeline 表现游标与动作播放器分别计算进度，表现 Marker 的图执行与停止链路仍有缺口。旧方案把作者帧、逻辑 tick、表现采样混在一起，又将部分接口接通记为整体完成，需要统一合同和任务状态。

## What Changes

- 保留 Logic 按 SimulationTick 提交、Presentation 按表现帧更新的分工。内容位置继续使用固定 60 作者帧基准；秒是换算读数，不在本变更迁移秒存储或亚帧存储。
- tick 率来自正式 pipeline 配置，逻辑推进通过整数比例与余数换算，余数参与快照；删除可变全局帧率与无语义的 TimelineData.Scale。这部分已有实现，保留其成果。
- 对同一动作播放实例统一计算表现采样结果，动作动画、Timeline 表现 Marker、随该动作采样的 Camera 内容消费同一结果。Clip 通过自己的起点、ClipIn 与源映射得到源动画时间；locomotion、独立特效和混合过渡继续由原 owner 管理。
- 表现进度来源与修正方式由业务在正式装配处显式选择。比较 committed 跟随、表现 delta 自由推进、向目标进度有界追赶的业务取舍；不按“单机 / MMO / ACT”在播放器中硬编码，不擅自选择一个全项目默认模式。
- 现行有限 Action 的 committed sample 合同继续有效；locomotion 继续消费自己的 prepared binding。自由推进与追赶的适用范围不得覆盖这些既有约束，扩展有限 Action 的自由播放须另行修改对应现行 spec。
- **BREAKING**：将现有表现时钟策略中的进度计算与动画播放器写入分开，移除同一动作在 Timeline 与播放器内各自累加的路径；不另建一套同义时钟接口、播放注册表或生命周期。
- Marker 继续与 Clip 同级、跟随 Track 执行域，触发图只有 OnEnable。Logic Marker 走原 Advance / Commit；Presentation Marker 使用正式表现执行上下文，不借用只在 Logic Tick 内有效的技能 invoker，不调用 Simulation Evaluate / Finalize。
- 补齐停止、取消、分支修正、循环与重复采样的事件语义。终态生效后禁止旧播放再产生新 Marker；已生成表现的收尾归原业务 owner，修正采样不自动成为新的事件经过。
- 补齐现有 Track 的 Domain 编辑。通过原作者 mutation 同步校验 Track、Clip、Marker 图的域能力，失败保留原内容并指出不兼容项；Domain 不同时充当时钟策略开关。
- 统一拖动、数值输入与提交的作者帧量化，明确“60 作者帧”不等于“60 个逻辑 tick”；不以解除 UI 吸附伪装亚帧存储支持。

## Capabilities

### New Capabilities

- btsmtl-timeline-clock-domain：作者时间与运行调度的区分、确定性换算、业务时钟策略边界、共享表现采样、Marker 事件生命周期、Domain 作者入口及正式下游消费。

### Modified Capabilities

- btsmtl-timeline-direct-runtime：双域直读同一内容、每个播放实例的时间所有权、停止与表现收尾边界，保留状态本地 ActionCue 合同。
- btsmtl-runnable-timeline-node：移除 Scale 模型描述，限定 Logic TimelineBody 与表现安全 Marker 图的执行能力。
- character-animation-pipeline：同一动作的表现采样一致、同级 Marker、表现事务与 Gameplay 状态隔离。

## Impact

- 运行链涉及 TimelineRuntimePresentationDriver、CharacterTimelineHost、既有 Action 表现时钟策略、Action sample history / projector、Pose Player 消费与 Camera bridge。只复用正式 owner，不新增全局时间服务。
- 作者链涉及 Slate binding、Track Inspector、正式 Timeline mutation、域能力校验及既有 C# authoring 导出 / 重建。私有 Marker 图仍随正式 owner 闭包管理。
- 本轮只更新本 change 的 proposal、design、四份 delta spec 与 tasks，不修改代码，不归档，不把待实施合同写成已完成事实。

### 与现行 spec 的对账

| 现行合同 | 本次处理 |
|---|---|
| gameplay-tick-system：固定 SimulationTick，Presentation 不调用 Kernel Evaluate / Finalize | 保持；表现 Marker 图必须在此边界内执行 |
| btsmtl-timeline-editor-preview：作者帧不自动等于 Logic Tick | 删除旧“必须按 1/tickRate 吸附”的错误结论；按可存储作者帧编辑 |
| character-presentation-interpolation：有限 Action 基于 committed raw sample 投影 | 保持；本变更不能把该类 Action 无条件改成自由推进 |
| character-presentation-interpolation：locomotion plan / prepared binding 与 Body correction 独立 | 保持；不把所有 Pose Player 合并成一个动作时钟，不恢复单 Clip 的策略配置 |
| character-animation-pipeline：旧条款仍描述 Clip 下的 Presentation Marker | 通过本 change 的同名 MODIFIED requirement 改为 Track 下的同级 Marker |
| btsmtl-runnable-timeline-node：数据模型仍列出 scale | 通过本 change 的 MODIFIED requirement 删除已废弃字段描述 |

旧文档中的“独立表现时钟已否决”“普通表现一律自由播放”“不同域不得共享采样来源”不再作为决策。设计中分别说明调度、进度来源和作者单位；本变更尚未实现完成，差异与待办见 [design.md](design.md) 和 [tasks.md](tasks.md)。
