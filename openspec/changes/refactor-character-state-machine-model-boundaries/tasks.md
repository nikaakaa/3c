## 1. Context Lock
- [x] 1.1 读取 active `refactor-state-timeline-facts-authority` 文档。
- [x] 1.2 读取 active `refactor-transition-condition-evaluators` 文档。
- [x] 1.3 读取 active `refactor-state-action-motion-output` 文档。
- [x] 1.4 读取 `CharacterStateMachineTypes.cs` 当前全部类型。
- [x] 1.5 读取 `CharacterStateMachineDefinition.cs`。
- [x] 1.6 读取 runner/runtime frame 类型。
- [x] 1.7 读取 default state machine asset validation tests。
- [x] 1.8 对 `CharacterStateMachineTypes` 运行 GitNexus upstream impact analysis。
- [x] 1.9 对 `CharacterStateMachineDefinition` 运行 GitNexus upstream impact analysis。
- [x] 1.10 记录 HIGH/CRITICAL 风险并等待用户确认后再实施。

## 2. Boundary Tests
- [x] 2.1 静态测试：generic graph model 不引用 `Dodge`。
- [x] 2.2 静态测试：generic graph model 不引用 `TurnBack`。
- [x] 2.3 静态测试：generic graph model 不引用 `BasicMovementGait`。
- [x] 2.4 静态测试：generic graph model 不引用 `ActionMovementCommand`。
- [x] 2.5 静态测试：generic graph model 不引用 Unity scene object。
- [x] 2.6 行为测试：默认状态机节点数量迁移前后等价。
- [x] 2.7 行为测试：默认 transition 迁移前后等价。
- [x] 2.8 行为测试：snapshot/restore 迁移前后等价。
- [x] 2.9 行为测试：FullBodyStateView 派生结果迁移前后等价。

## 3. Generic Model
- [x] 3.1 创建 generic graph definition 类型。
- [x] 3.2 创建 generic node 类型。
- [x] 3.3 创建 generic transition 类型。
- [x] 3.4 创建 generic snapshot/restore 类型。
- [x] 3.5 将 path/id/parent/children 关系收敛到 generic 层。
- [x] 3.6 确认 generic 层不持有 character output 字段。

## 4. Character Metadata Model
- [x] 4.1 创建 character node metadata 类型。
- [x] 4.2 创建 capability module model。
- [x] 4.3 迁移 Locomotion phase metadata。
- [x] 4.4 迁移 Action state metadata。
- [x] 4.5 迁移 animation/timeline binding metadata。
- [x] 4.6 迁移 output module metadata。
- [x] 4.7 保持 FullBodyStateView 为派生 view。
- [x] 4.8 加 validator 检查模块组合合法性。

## 5. Runtime Integration
- [x] 5.1 创建 compatibility facade，使 runner 可消费新模型。
- [x] 5.2 迁移 runner node lookup。
- [x] 5.3 迁移 transition lookup。
- [x] 5.4 迁移 snapshot active path 派生。
- [x] 5.5 迁移 variant/payload 读取。
- [x] 5.6 保持 runner 不执行 motion/animation/input side effects。
- [x] 5.7 保持唯一 runner owner。

## 6. Asset And Test Migration
- [x] 6.1 更新 default state machine asset conversion 或 authoring path。
- [x] 6.2 更新 config validation tests。
- [x] 6.3 更新 behavior regression tests。
- [x] 6.4 更新 static boundary tests。
- [x] 6.5 删除或瘦身旧万能字段。

## 7. Validation
- [x] 7.1 运行相关 Unity EditMode 定向测试。
- [x] 7.2 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp.csproj --no-restore`。
- [x] 7.3 运行 `dotnet build 3cDemo\Client\3C_Client\Assembly-CSharp-Editor.csproj --no-restore`。
- [x] 7.4 运行 `openspec validate refactor-character-state-machine-model-boundaries --strict --no-interactive`。
- [x] 7.5 运行 GitNexus `detect-changes` 并记录影响范围。

## 8. Scope Gates
- [x] 8.1 搜索 generic graph model，确认没有 `Dodge`。
- [x] 8.2 搜索 generic graph model，确认没有 `TurnBack`。
- [x] 8.3 搜索 generic graph model，确认没有 `RunLatch`。
- [x] 8.4 搜索 generic graph model，确认没有 `BasicMovementGait`。
- [x] 8.5 搜索 generic graph model，确认没有 `ActionMovementCommand`。
- [x] 8.6 搜索 generic graph model，确认没有 animation binding concrete 类型。
- [x] 8.7 搜索 runner core，确认没有 motion executor 调用。
- [x] 8.8 搜索 runner core，确认没有 animation presenter 调用。
- [x] 8.9 搜索 runner core，确认没有 input consume 调用。
- [x] 8.10 搜索 runner core，确认没有 direct diagnostic submit。

## 9. Fine-Grained Completion Checks
- [x] 9.1 `StateGraphDefinition` 只包含 graph topology 和 transition edge 数据。
- [x] 9.2 `StateGraphNode` 保持 id/path/parent/children 语义稳定。
- [x] 9.3 `StateGraphTransition` 只保存 condition key/reference，不保存 evaluator implementation。
- [x] 9.4 `StateGraphSnapshot` 只保存 generic active identity/time/pending/variant。
- [x] 9.5 `CharacterStateNodeMetadata` 以 graph node id 关联业务 metadata。
- [x] 9.6 capability module validator 能发现缺失 required metadata。
- [x] 9.7 `FullBodyStateView` 只从 snapshot + metadata 派生。
- [x] 9.8 默认资产转换覆盖 Idle、MoveStart、MoveLoop、MoveStop、TurnBack、Dodge。
- [x] 9.9 旧万能字段删除或只在 compatibility facade 内部使用。
- [x] 9.10 compatibility facade 有明确移除条件，不成为新的长期模型层。
