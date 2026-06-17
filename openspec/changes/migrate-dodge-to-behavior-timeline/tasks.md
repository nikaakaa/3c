## 0. 范围确认
- [ ] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [ ] 0.2 确认 Dodge behavior submission golden line、behavior submission entry 和 action selection nodes 已完成。
- [ ] 0.3 确认本变更只迁移 Dodge，不新增其它技能。

## 1. Dodge Timeline 数据
- [ ] 1.1 定义 Directional Dodge timeline 数据。
- [ ] 1.2 定义 Backstep Dodge timeline 数据。
- [ ] 1.3 将 duration seconds 转换为权威 frame 配置。
- [ ] 1.4 将 motion clip、animation clip key 和必要 window fact 写入 timeline。
- [ ] 1.5 增加 Directional / Backstep timeline validation 测试。

## 2. Dodge Selector
- [ ] 2.1 定义 Dodge selector root。
- [ ] 2.2 定义 Directional condition。
- [ ] 2.3 定义 Backstep condition。
- [ ] 2.4 确认 selector 未命中时报错或空输出，不 fallback。
- [ ] 2.5 增加 Directional / Backstep selection 测试。

## 3. Resolver 迁移
- [ ] 3.1 分析当前 `DodgeCharacterActionRequestResolver` 的职责。
- [ ] 3.2 将方向和 facing 计算保留为 request context。
- [ ] 3.3 移除正式 resolver 中对旧 variant motion 参数的运行时权威依赖。
- [ ] 3.4 保持 input consume 和 interrupt request 语义。
- [ ] 3.5 增加 resolver 不读取旧 variant 作为 motion/animation 权威的测试。

## 4. 配置迁移
- [ ] 4.1 更新 `CharacterActionDefinitionSO` 或等价 Dodge action definition。
- [ ] 4.2 更新 Corin Dodge action catalog 配置。
- [ ] 4.3 标记旧 variant 字段为迁移输入或移除。
- [ ] 4.4 确认缺失 timeline 不使用旧 variant fallback。
- [ ] 4.5 明确 `DodgeActionBranchTimelineBuilder` 的最终状态：删除、迁移工具或测试 fixture helper。
- [ ] 4.6 增加配置校验测试。

## 5. 行为回归
- [ ] 5.1 覆盖 Directional Dodge accepted 行为。
- [ ] 5.2 覆盖 Backstep Dodge accepted 行为。
- [ ] 5.3 覆盖 Run latch 行为。
- [ ] 5.4 覆盖 animation-end 等待行为。
- [ ] 5.5 覆盖动作完成后再次触发。
- [ ] 5.6 覆盖 rollback restore 后 timeline frame 一致性。

## 6. 边界验证
- [ ] 6.1 增加静态测试确认 Dodge timeline 抽象层不引用 Dodge 专用类型。
- [ ] 6.2 增加静态测试确认 Dodge runtime 不新增第二 pipeline、第二 motion executor 或第二 animation presenter。
- [ ] 6.3 运行相关 Unity EditMode 测试。
- [ ] 6.4 运行 `openspec validate migrate-dodge-to-behavior-timeline --strict --no-interactive`。
