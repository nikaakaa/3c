# 角色运行与作者底层重构记录

更新：2026-09-29。

本轮完整范围由 [OpenSpec proposal](../openspec/changes/refactor-character-runtime-and-authoring-boundaries/proposal.md) 定义；[29 项实施清单](../openspec/changes/refactor-character-runtime-and-authoring-boundaries/tasks.md) 已按代码、直接消费者与必要检查逐项收口。

[设计与实施收口记录](../openspec/changes/refactor-character-runtime-and-authoring-boundaries/design.md) 文末列出各职责的输入输出、对应提交、引用核对、编译证据、保留理由及专项归属。前文的未提交状态和旧行号仅作排查历史参考。

已完成 SessionHistory、Foot Sampler、Pose 作者校验／保存／创建事务、TreeDesigner 完整退役、Timeline／Foot 文件归位、场景残留清理以及表现外围故障和异常释放收口。正式 Skill 仍走编译链，Pose 保持原生 FlowCanvas 执行。

当前架构合同由 [project.md](../openspec/project.md) 与现行 specs 共同拥有，五份相关规格已同步。Unity 编译无错误，现行 101 份 spec 严格校验通过；未运行 replay、端到端验收或新性能采集，未新增测试，未自动归档。

性能校准、完整 Foot 跨关系质量验证、表现 checkpoint 新能力与工作台产品专项不由此次结构完成替代；具体边界见实施收口记录。此前将阶段性结果误报为整体完成的口径不再使用。
