# Change: 分离并归档已完成的PoseGraph运行基础

## Why

原`refactor-character-pose-graph-architecture`同时保存已完成的运行基础和未完成的作者、编译质量、观察与资源边界工作，导致后续讨论容易把“运行基础完成”理解为“PoseGraph全部完成”。用户于2026-09-12明确要求拆出完成文档并归档，同时保留PoseGraph只读Blackboard范围划分。

## What Changes

- 从原任务清单完整移出第1、2、4、5、6、7、8组，共60条原已勾选记录；原编号和原文保存在completion-record.md。按用户随后要求，tasks.md只列其中53项实现或文档任务，7条验证/边界记录只作历史留存。
- 移出对应Decision 3、4、6、7、11、12的设计正文；原设计保留编号与归档链接，防止既有引用失效。
- 归档仅记录基线盘点、帧事务合同、Source、Program/实例状态、Program Runtime、Final Publication和在线调参已经完成的部分。
- 原change保留全部原未完成记录的真实状态：实现项留在tasks.md，验证及已撤销说明移至verification-history.md；增加只读输入、曲线作用范围、Body接口和EventGraph消费边界待办。
- 本次只有文档分离，不实施代码、不修改Unity资产、不重新执行运行验收，也不把历史未覆盖项改成通过。

## Capabilities

本次不新增或修改能力合同，`skip_specs: true`仅用于已经完成工作记录的归档。原change的delta specs仍可能包含未完成增量，全部留在原change；不把旧delta重新安装到当前spec。

## Impact

- 完成记录与设计：[completion-record.md](completion-record.md)、[tasks.md](tasks.md)、[design.md](design.md)。
- 剩余工作：原change任务（历史路径已退役，原引用：`../../refactor-character-pose-graph-architecture/tasks.md`；历史见 Git 提交 `f99572df9`）。
- 原始证据：execution.md（历史路径已退役，原引用：`../../refactor-character-pose-graph-architecture/execution.md`；历史见 Git 提交 `f99572df9`）、[行为基线（历史路径已退役，原引用：`../../refactor-character-pose-graph-architecture/behavior-baseline.md`；历史见 Git 提交 `f99572df9`）。证据包含过去的失败和后续修正，须按时间与范围阅读。
- 不涉及EventGraph规划窗口拥有的文档、其它change的实现、当前spec正文和任何运行产物。

## Completion Boundary

60项任务在分离前均已标记完成；本次按用户归档指令接收该完成范围。原任务3.9、9.x、10.x、11.x、13.x、14.x、19.x、21.x和24.x等剩余项并未因此完成。原Scene Play首帧等待、IK中间值对账和其它历史限制继续保留于原change，不能用本归档证明它们通过。
