## 0. 范围确认
- [ ] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [ ] 0.2 确认本变更只扩展 CommittedActionBranch 内部节点选择。
- [ ] 0.3 确认本变更不迁移 Dodge 配置权威。

## 1. Action Node 数据模型
- [ ] 1.1 扩展 `CommittedActionNodeKind`，新增 Selector 和 Condition 或批准的等价 kind。
- [ ] 1.2 新增 Action node child id 列表或等价树结构。
- [ ] 1.3 新增 Condition payload 纯数据模型。
- [ ] 1.4 保持 TimelineNode payload 兼容。
- [ ] 1.5 增加单 timeline 旧模型兼容测试。

## 2. Evaluation Input
- [ ] 2.1 新增 `CommittedActionBranchEvaluationContext` 或等价上下文。
- [ ] 2.2 暴露只读 request facts。
- [ ] 2.3 暴露只读 locomotion facts 或 movement intent。
- [ ] 2.4 暴露只读 blackboard snapshot。
- [ ] 2.5 增加 context 不持有 Unity object 的静态测试。

## 3. Condition Evaluator
- [ ] 3.1 新增 condition evaluator 接口或策略集合。
- [ ] 3.2 实现 Directional / Backstep 所需的最小条件。
- [ ] 3.3 确认 condition 不写 state、不消费 input、不写 blackboard。
- [ ] 3.4 增加 condition true / false 测试。

## 4. Selector Evaluator
- [ ] 4.1 扩展 `CommittedActionBranchEvaluator` 支持 selector。
- [ ] 4.2 实现稳定 child 顺序评估。
- [ ] 4.3 实现第一个通过 child 被选中。
- [ ] 4.4 确认未选中 timeline 不产生 outcome。
- [ ] 4.5 增加 selector 顺序和未选中输出测试。

## 5. Timeline Leaf 兼容
- [ ] 5.1 保持 timeline leaf 可直接作为 root。
- [ ] 5.2 保持 ActionTimeline frame evaluator 行为不变。
- [ ] 5.3 增加 selector 下 timeline 的 motion / animation / fact / cue 输出测试。

## 6. 验证
- [ ] 6.1 运行相关 Action Branch / Timeline EditMode 测试。
- [ ] 6.2 运行静态边界测试，确认 Action selection nodes 不引用 Unity runtime object 或黑板写入接口。
- [ ] 6.3 运行 `openspec validate add-committed-action-selection-nodes --strict --no-interactive`。
