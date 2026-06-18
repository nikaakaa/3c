## 0. 范围确认
- [x] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [x] 0.2 确认 submission contracts 已完成。
- [x] 0.3 确认 submitter chain boundary 已完成。
- [x] 0.4 确认 Dodge golden line 已完成并通过。

## 1. Production Definition 与 Runner
- [x] 1.1 定义最小 production behavior runtime definition / config。
- [x] 1.2 定义 root -> fixed ordered parallel -> Locomotion leaf / Committed Action leaf。
- [x] 1.3 缺失 root、leaf 或顺序时报告配置错误。
- [x] 1.4 新增 production behavior submission runner。
- [x] 1.5 支持固定 root。
- [x] 1.6 支持固定顺序 parallel。
- [x] 1.7 支持 Locomotion leaf。
- [x] 1.8 支持 Committed Action leaf。
- [x] 1.9 增加 runner 不支持 selector/condition 的明确测试。
- [x] 1.10 增加缺失 definition 不使用 fallback tree 的测试。

## 2. RequestPass
- [x] 2.1 实现 Locomotion leaf request pass。
- [x] 2.2 实现 Action leaf request pass。
- [x] 2.3 确保 Locomotion request context 先写入 frame context。
- [x] 2.4 确保 Action 消费 Locomotion request context。
- [x] 2.5 增加 RequestPass 顺序测试。

## 3. OutputPass
- [x] 3.1 实现 Locomotion leaf output pass。
- [x] 3.2 实现 Action leaf output pass。
- [x] 3.3 确保 Locomotion state frame / locomotion frame 先写入 context。
- [x] 3.4 确保 Action 消费 state frame / locomotion frame。
- [x] 3.5 增加 OutputPass 顺序测试。

## 4. Submission Composer
- [x] 4.1 新增 behavior submission composer。
- [x] 4.2 映射 Locomotion output submission。
- [x] 4.3 映射 Action output submission。
- [x] 4.4 映射 input consume candidate。
- [x] 4.5 映射 window fact / cue candidate。
- [x] 4.6 确认 composer 进入现有 BodyArbiter / CharacterFramePlan。
- [x] 4.7 增加 composer 不新增第二 arbiter 的测试。
- [x] 4.8 增加 unsupported / unconsumed submission 产生 diagnostic 的测试。

## 5. Default Entry 替换
- [x] 5.1 更新 `CharacterRuntimeCore` 默认 runtime host 创建逻辑。
- [x] 5.2 确认旧 submitter chain 不再是默认生产入口。
- [x] 5.3 若保留旧 chain，标注迁移用途和删除条件。
- [x] 5.4 增加 default entry 测试。

## 6. 端到端金线
- [x] 6.1 覆盖 Directional Dodge 从输入到 frame plan。
- [x] 6.2 覆盖 Backstep Dodge 从输入到 frame plan。
- [x] 6.3 覆盖 rejected Dodge。
- [x] 6.4 覆盖基础 Locomotion 无 Action。
- [x] 6.5 覆盖 restore 后继续输出一致。

## 7. 边界验证
- [x] 7.1 增加 wrappers 不直接调用 motion executor 的测试。
- [x] 7.2 增加 wrappers 不直接调用 animation presenter 的测试。
- [x] 7.3 增加 wrappers 不直接写 blackboard 的测试。
- [x] 7.4 运行相关 Unity EditMode 测试。
- [x] 7.5 运行 `openspec validate add-character-behavior-submission-entry --strict --no-interactive`。
