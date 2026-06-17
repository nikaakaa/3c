## 0. 范围确认
- [ ] 0.1 读取本变更 `proposal.md`、`design.md` 和 spec delta。
- [ ] 0.2 确认本变更只收束 submitter chain，不引入 behavior runner。
- [ ] 0.3 确认本变更不改变 Locomotion / Action 行为。

## 1. 链路盘点
- [ ] 1.1 列出 `CharacterFrameSubmitterGraph` 文件和引用点。
- [ ] 1.2 记录 request stage 调用顺序。
- [ ] 1.3 记录 output stage 调用顺序。
- [ ] 1.4 记录 Locomotion 写入 context 的字段。
- [ ] 1.5 记录 Action 读取 context 的字段。

## 2. 命名收束
- [ ] 2.1 将 `CharacterFrameSubmitterGraph` 重命名为 `CharacterFrameSubmitterChain` 或批准的等价名称。
- [ ] 2.2 更新构造和默认创建逻辑。
- [ ] 2.3 更新测试类名和断言文本。
- [ ] 2.4 增加旧 Graph 名称不作为正式扩展入口的静态测试。

## 3. Context Dependency 测试
- [ ] 3.1 增加 request stage 中 Locomotion 先于 Action 的测试。
- [ ] 3.2 增加 output stage 中 Locomotion 先填 state/locomotion frame 的测试。
- [ ] 3.3 增加 Action 缺少必要 context 时失败原因明确的测试。
- [ ] 3.4 增加 rename 后 frame plan 行为不变的测试。

## 4. 行为回归
- [ ] 4.1 运行基础 Locomotion 相关 EditMode 测试。
- [ ] 4.2 运行 Directional / Backstep Dodge 相关 EditMode 测试。
- [ ] 4.3 运行 CharacterFramePipeline / arbitration 相关 EditMode 测试。

## 5. 验证
- [ ] 5.1 运行静态边界测试。
- [ ] 5.2 运行 `openspec validate refactor-character-submitter-chain-boundary --strict --no-interactive`。
