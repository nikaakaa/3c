## 0. 范围确认
- [x] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [x] 0.2 确认本变更不接生产 pipeline。
- [x] 0.3 确认本变更不包装 Locomotion / Action。

## 1. Pass 合同
- [x] 1.1 新增 behavior pass enum 或等价类型。
- [x] 1.2 定义 RequestPass 可提交 payload。
- [x] 1.3 定义 OutputPass 可提交 payload。
- [x] 1.4 增加 pass 边界测试。

## 2. Source 与节点身份
- [x] 2.1 新增 behavior source id 或 node id。
- [x] 2.2 新增 source step 规范。
- [x] 2.3 新增 source ordering 规范。
- [x] 2.4 增加 id 默认值和排序测试。

## 3. Typed Submission Payload
- [x] 3.1 新增 request submission payload。
- [x] 3.2 新增 output submission payload。
- [x] 3.3 新增 cue submission payload。
- [x] 3.4 新增 diagnostic submission payload。
- [x] 3.5 新增 state write submission payload。
- [x] 3.6 定义每类 submission 的允许 consumer / owner。
- [x] 3.7 增加各 payload 默认值测试。
- [x] 3.8 增加非法 consumer 或未消费 submission 的 diagnostic 测试。

## 4. Submission Set
- [x] 4.1 新增 submission set 聚合模型。
- [x] 4.2 支持按 pass 查询 submission。
- [x] 4.3 支持按 source 查询 submission。
- [x] 4.4 支持空集合。
- [x] 4.5 增加聚合顺序测试。

## 5. 状态所有权
- [x] 5.1 新增状态所有权表或等价模型。
- [x] 5.2 覆盖 behavior node private state owner。
- [x] 5.3 覆盖 Locomotion runtime state owner。
- [x] 5.4 覆盖 Action lifecycle state owner。
- [x] 5.5 覆盖 animation playback state owner。
- [x] 5.6 覆盖 confirmed blackboard facts owner。
- [x] 5.7 覆盖 rollback restore state owner。
- [x] 5.8 增加状态所有权测试。

## 6. Fake Runner
- [x] 6.1 新增 fake leaf evaluator。
- [x] 6.2 新增 fake runner 或测试专用 collector。
- [x] 6.3 验证多个 fake leaf 同帧稳定收集。
- [x] 6.4 验证 fake runner 不注册生产入口。

## 7. 边界验证
- [x] 7.1 增加静态测试确认合同层不引用 Unity runtime object。
- [x] 7.2 增加静态测试确认合同层不引用 Editor / GraphView。
- [x] 7.3 增加静态测试确认合同层不调用 blackboard writer、motion executor、animation presenter 或 output applier。
- [x] 7.4 运行相关 Unity EditMode 测试。
- [x] 7.5 运行 `openspec validate add-character-behavior-submission-contracts --strict --no-interactive`。
