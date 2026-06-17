## 0. 范围确认
- [ ] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [ ] 0.2 确认 submission contracts 已完成。
- [ ] 0.3 确认 submitter chain boundary 已完成。
- [ ] 0.4 确认 Dodge golden line 已完成并通过。

## 1. Production Runner
- [ ] 1.1 新增 production behavior submission runner。
- [ ] 1.2 支持固定 root。
- [ ] 1.3 支持固定顺序 parallel。
- [ ] 1.4 支持 Locomotion leaf。
- [ ] 1.5 支持 Committed Action leaf。
- [ ] 1.6 增加 runner 不支持 selector/condition 的明确测试。

## 2. RequestPass
- [ ] 2.1 实现 Locomotion leaf request pass。
- [ ] 2.2 实现 Action leaf request pass。
- [ ] 2.3 确保 Locomotion request context 先写入 frame context。
- [ ] 2.4 确保 Action 消费 Locomotion request context。
- [ ] 2.5 增加 RequestPass 顺序测试。

## 3. OutputPass
- [ ] 3.1 实现 Locomotion leaf output pass。
- [ ] 3.2 实现 Action leaf output pass。
- [ ] 3.3 确保 Locomotion state frame / locomotion frame 先写入 context。
- [ ] 3.4 确保 Action 消费 state frame / locomotion frame。
- [ ] 3.5 增加 OutputPass 顺序测试。

## 4. Submission Composer
- [ ] 4.1 新增 behavior submission composer。
- [ ] 4.2 映射 Locomotion output submission。
- [ ] 4.3 映射 Action output submission。
- [ ] 4.4 映射 input consume candidate。
- [ ] 4.5 映射 window fact / cue candidate。
- [ ] 4.6 确认 composer 进入现有 BodyArbiter / CharacterFramePlan。
- [ ] 4.7 增加 composer 不新增第二 arbiter 的测试。

## 5. Default Entry 替换
- [ ] 5.1 更新 `CharacterRuntimeCore` 默认 runtime host 创建逻辑。
- [ ] 5.2 确认旧 submitter chain 不再是默认生产入口。
- [ ] 5.3 若保留旧 chain，标注迁移用途和删除条件。
- [ ] 5.4 增加 default entry 测试。

## 6. 端到端金线
- [ ] 6.1 覆盖 Directional Dodge 从输入到 frame plan。
- [ ] 6.2 覆盖 Backstep Dodge 从输入到 frame plan。
- [ ] 6.3 覆盖 rejected Dodge。
- [ ] 6.4 覆盖基础 Locomotion 无 Action。
- [ ] 6.5 覆盖 restore 后继续输出一致。

## 7. 边界验证
- [ ] 7.1 增加 wrappers 不直接调用 motion executor 的测试。
- [ ] 7.2 增加 wrappers 不直接调用 animation presenter 的测试。
- [ ] 7.3 增加 wrappers 不直接写 blackboard 的测试。
- [ ] 7.4 运行相关 Unity EditMode 测试。
- [ ] 7.5 运行 `openspec validate add-character-behavior-submission-entry --strict --no-interactive`。
