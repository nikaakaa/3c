## 0. 范围确认
- [x] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [x] 0.2 确认 behavior submission contracts 已完成。
- [x] 0.3 确认本变更不替换默认 runtime entry。

## 1. Baseline Capture
- [x] 1.1 新增 Dodge baseline capture helper。
- [x] 1.2 捕获 accepted / rejected request 结果。
- [x] 1.3 捕获 body claim。
- [x] 1.4 捕获 motion spec。
- [x] 1.5 捕获 animation request。
- [x] 1.6 捕获 input consume candidate。
- [x] 1.7 捕获 Run latch 相关 frame output。
- [x] 1.8 捕获 source step、tick/frame 和 playback identity。

## 2. Submission Mapping
- [x] 2.1 新增旧路径输出到 request submission 的 mapping。
- [x] 2.2 新增旧路径输出到 output submission 的 mapping。
- [x] 2.3 新增 ActionBranchOutcome 到 window fact / cue submission 的 mapping。
- [x] 2.4 新增 diagnostics mapping。
- [x] 2.5 新增 shared comparison helper 或等价字段级对比 schema。
- [x] 2.6 增加缺失 mapped field 产生明确失败信息的测试。
- [x] 2.7 增加 mapping 不写 blackboard、不调用 applier 的测试。

## 3. Directional 金线
- [x] 3.1 构造有移动输入的 Dodge 场景。
- [x] 3.2 对比 accepted request。
- [x] 3.3 对比 Directional motion spec。
- [x] 3.4 对比 animation key 和 playback intent。
- [x] 3.5 对比 body claim。
- [x] 3.6 对比 Run latch candidate。

## 4. Backstep 金线
- [x] 4.1 构造无移动输入的 Dodge 场景。
- [x] 4.2 对比 accepted request。
- [x] 4.3 对比 Backstep motion spec。
- [x] 4.4 对比 animation key 和 playback intent。
- [x] 4.5 对比 body claim。
- [x] 4.6 确认不产生 Run latch candidate。

## 5. Rejected / Retry / Restore
- [x] 5.1 覆盖 rejected request 不消费 input。
- [x] 5.2 覆盖 rejected request 不产生 output submission。
- [x] 5.3 覆盖动作完成后再次触发。
- [x] 5.4 覆盖 animation-end 等待。
- [x] 5.5 覆盖 restore 后 frame timing 一致。

## 6. 边界验证
- [x] 6.1 增加静态测试确认 golden helper 不注册 production runtime host。
- [x] 6.2 增加静态测试确认 golden helper 不新增第二 motion executor / animation presenter。
- [x] 6.3 运行相关 Unity EditMode 测试。
- [x] 6.4 运行 `openspec validate add-dodge-behavior-submission-golden-line --strict --no-interactive`。
