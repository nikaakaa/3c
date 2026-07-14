# Tasks

## 1. 现状收口
- [x] 1.1 确认 `add-character-pipeline-runtime-entry` 的实现状态和 checklist 状态是否一致。
- [x] 1.2 读取当前 `CharacterInputStage` 实现。
- [x] 1.3 读取当前 `CharacterInputSnapshot` 实现。
- [x] 1.4 读取当前 `CharacterGraphContext` 输入读取实现。
- [x] 1.5 读取当前 `ClientCommand` 和 `NetworkOutput` 实现。
- [x] 1.6 确认新输入层仍位于 `Assets/GameScripts/Main/Runtime/Character/Pipeline/Input`。
- [x] 1.7 确认不继续扩展旧 `Assets/Scripts` 或旧 `Charactor` 路径。

## 2. CharacterInputProfile
- [x] 2.1 新增 `CharacterInputProfile` 正式资产类型。
- [x] 2.2 让 profile 引用正式 `InputActionAsset`。
- [x] 2.3 新增 continuous signal 定义结构。
- [x] 2.4 新增 action request 定义结构。
- [x] 2.5 为每个定义保存 semantic id。
- [x] 2.6 为每个定义保存来源 action identity。
- [x] 2.7 为 request 定义保存 buffer duration。
- [x] 2.8 为 request 定义保存 priority。
- [x] 2.9 增加 profile 配置解析错误报告。
- [x] 2.10 禁止 profile 按 action 显示名 fallback 查找。

## 3. CharacterInputFrame
- [x] 3.1 将当前 `CharacterInputSnapshot` 演进为 `CharacterInputFrame` 或等价正式结构。
- [x] 3.2 在 input frame 中保存 simulation tick。
- [x] 3.3 在 input frame 中保存 input sequence。
- [x] 3.4 在 input frame 中保存 authority mode。
- [x] 3.5 在 input frame 中保存 continuous command 集合。
- [x] 3.6 在 input frame 中保存本 tick 新产生的 request 集合。
- [x] 3.7 为 Vector2 command 提供写入接口。
- [x] 3.8 为 Float command 提供写入接口。
- [x] 3.9 为 Bool command 提供写入接口。
- [x] 3.10 为 command 提供按 semantic id 查询接口。

## 4. CharacterInputStage
- [x] 4.1 将 `CharacterInputStage` 构造输入从 raw `InputActionAsset` 调整为 `CharacterInputProfile`。
- [x] 4.2 让 InputStage 从 profile 的 source asset 启用输入。
- [x] 4.3 让 InputStage 在 `LocalPredicted` 模式采样本地 action。
- [x] 4.4 让 InputStage 在 `RemoteProxy` 模式不产生本地 request。
- [x] 4.5 让 InputStage 采样 Vector2 signal。
- [x] 4.6 让 InputStage 采样 Float signal。
- [x] 4.7 让 InputStage 采样 Bool signal。
- [x] 4.8 让 InputStage 检测 request action 的触发边沿。
- [x] 4.9 让 InputStage 将连续输入写入 input frame。
- [x] 4.10 让 InputStage 将离散输入写入 request buffer。
- [x] 4.11 保持输入读取只进入 frame/context，不直接驱动 Transform。

## 5. RequestBuffer
- [x] 5.1 新增 `CharacterInputRequest`。
- [x] 5.2 新增 `CharacterInputRequestBuffer`。
- [x] 5.3 request 保存 semantic request id。
- [x] 5.4 request 保存 created tick。
- [x] 5.5 request 保存 input sequence。
- [x] 5.6 request 保存 expire tick 或 buffer duration。
- [x] 5.7 request 保存 priority。
- [x] 5.8 request 保存 consumed 状态。
- [x] 5.9 buffer 支持写入 request。
- [x] 5.10 buffer 支持非消费查询 request。
- [x] 5.11 buffer 支持按 priority 查询候选 request。
- [x] 5.12 buffer 支持消费 request。
- [x] 5.13 buffer 清理过期 request。
- [x] 5.14 buffer 帧末不得清掉仍在 buffer 窗口内的 request。

## 6. InputHistory
- [x] 6.1 新增 `CharacterInputHistory`。
- [x] 6.2 history 支持设置正式容量。
- [x] 6.3 history 保存每 tick input frame。
- [x] 6.4 history 支持按 simulation tick 查询。
- [x] 6.5 history 支持按 input sequence 查询。
- [x] 6.6 correction 相关逻辑不得从当前 InputAction 反推历史输入。

## 7. GraphContext 输入入口
- [x] 7.1 让 `CharacterGraphContext` 暴露当前 input frame。
- [x] 7.2 让 graph context 暴露 request buffer。
- [x] 7.3 让 graph context 暴露 input history 查询入口。
- [x] 7.4 保留 graph context 对 `IInputActionValueSource` 的实现。
- [x] 7.5 让 raw InputAction 节点仍通过同一 input stage/source 读取。
- [x] 7.6 禁止 graph context 场景搜索输入来源。

## 8. NetworkOutput 和 ClientCommand
- [x] 8.1 调整 `ClientCommand`，使其来源于 `CharacterInputFrame`。
- [x] 8.2 在 `ClientCommand` 中保存 input sequence。
- [x] 8.3 在 `ClientCommand` 中保存 simulation tick。
- [x] 8.4 在 `ClientCommand` 中保存 continuous command 摘要。
- [x] 8.5 在 `ClientCommand` 中保存本 tick action request 列表。
- [x] 8.6 让 `NetworkOutput` 收集 input frame 生成的 command。
- [x] 8.7 保持 `NetworkSendStage` 只收集 command，不发送真实 transport。
- [x] 8.8 确认网络层不读取 raw InputAction 名称作为协议语义。

## 9. BTSMTL semantic 输入节点
- [x] 9.1 新增 semantic input signal 节点基类。
- [x] 9.2 新增 semantic Bool signal 节点。
- [x] 9.3 新增 semantic Float signal 节点。
- [x] 9.4 新增 semantic Vector2 signal 节点。
- [x] 9.5 节点保存 semantic id。
- [x] 9.6 节点从 graph context input frame 读取值。
- [x] 9.7 读取失败时输出类型默认值并报告缺失来源。

## 10. BTSMTL request 查询节点
- [x] 10.1 新增 request 查询节点。
- [x] 10.2 查询节点输出 BoolPropertyPort。
- [x] 10.3 查询节点从 graph context request buffer 读取。
- [x] 10.4 查询节点不得消费 request。
- [x] 10.5 确认查询节点可用于 TransitionRuleGraph。
- [x] 10.6 确认 request consume 节点不得用于 TransitionRuleGraph。

## 11. 编辑器拖拽入口
- [x] 11.1 为 `CharacterInputProfile` signal 定义接入拖拽创建。
- [x] 11.2 拖入 Vector2 signal 创建 Vector2 semantic input 节点。
- [x] 11.3 拖入 Float signal 创建 Float semantic input 节点。
- [x] 11.4 拖入 Bool signal 创建 Bool semantic input 节点。
- [x] 11.5 拖入 request 定义创建 request 查询节点。
- [x] 11.6 拖拽创建必须调用 `BaseTreeView.CreateNode()`。
- [x] 11.7 不支持的定义必须报告原因。
- [x] 11.8 不得创建 object fallback 节点。

## 12. 纯度和一致性
- [x] 12.1 确认 TransitionRuleGraph 中 request 查询无副作用。
- [x] 12.2 确认 request 消费只在状态行为、动作管线或正式 accept 点发生。
- [x] 12.3 确认 InputAction raw 节点未承担 request buffer 职责。
- [x] 12.4 确认没有新增 Input 专用 Graph。
- [x] 12.5 确认没有新增 Workbench 输入路径。
- [x] 12.6 确认没有新增 fallback 配置。
- [x] 12.7 确认没有恢复旧 BBB 输入控制器或旧 SO 数据源。

## 13. 工具校验
- [x] 13.1 运行 `openspec validate add-character-input-command-layer --strict --no-interactive`。
- [x] 13.2 运行 `openspec validate --all --strict --no-interactive`。
