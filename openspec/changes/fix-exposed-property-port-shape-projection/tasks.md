## 1. Capability 端口形状合同

- [x] 1.1 在 Graph Authoring Domain contract 中定义 typed discriminator、条件端口变体与纯 Node Port Shape Projector。
- [x] 1.2 规定固定端口、条件投影端口和作者可编辑动态端口的互斥关系与 identity 校验。
- [x] 1.3 将 BTSMTL flow-only configurator 替换为完整 Flow/Property shape projector，删除旧命名与旧入口。
- [x] 1.4 盘点所有会随配置改变 Flow/Property 方向或容量的 BTSMTL 节点，并统一接入正式 projector。

## 2. ExposedProperty 节点不变量

- [x] 2.1 让 `ExposedPropertyNode.SetNodeType` 原子维护 mode 与 `m_Value.Direction`。
- [x] 2.2 让 declaration binding 只维护值类型和 Blackboard reference，不再隐式修正 mode。
- [x] 2.3 删除 `ExposedPropertyNodeView`、Timeline TreeClip 和 Agent Mutation 调用点中重复的端口方向写入。
- [x] 2.4 为 mode、实际方向和期望方向不一致提供带节点 identity 的正式 Validator 诊断。

## 3. Canvas 与共享 Capability

- [x] 3.1 将 `exposed-property` 的 `m_Value` 从固定端口目录移出，登记 Get/Set 条件端口变体。
- [x] 3.2 让 Shared Graph document adapter 通过唯一 projector 生成 Flow 与 Property node projection。
- [x] 3.3 保留严格 shape binding，确保实际 PortView 与 capability projection 不一致时明确失败。
- [x] 3.4 删除基于默认构造 Get 节点推断全部 ExposedProperty 实例形状的代码。

## 4. Document 与对账闭环

- [x] 4.1 扩展 `context/node-catalog.json` 模型、strict codec 和 canonical writer，正式输出条件端口变体。
- [x] 4.2 让 Agent Package endpoint validation 按节点 typed properties 解析唯一目标端口形状。
- [x] 4.3 删除 Package Mapper 通过当前 Unity snapshot property port 放行未知 endpoint 的 fallback。
- [x] 4.4 让 Exporter、Reconciler、Mutation preflight 和 Validator 共用同一 Node Port Shape Projector。
- [x] 4.5 让 mode change plan 按“删不兼容边、配置节点、建目标边”形成同一 immutable plan 和事务。
- [x] 4.6 对未知 discriminator、零变体匹配、多变体匹配、错误方向和错误容量返回机器可读诊断。

## 5. 清理与合同同步

- [x] 5.1 删除旧 flow-only 动态配置字典、重复方向维护和 snapshot endpoint 兼容路径。
- [x] 5.2 更新 BTSMTL Agent Authoring 当前合同与技能说明，写明条件端口的唯一投影来源和 node catalog 格式。
- [x] 5.3 重新生成受影响 Document 的 service-owned context，保持 editable Graph 与 Unity asset identity 不变。
- [x] 5.4 对照 current specs 和最终实现，确认 Canvas、Document、Reconciler、Mutation、Validator 不存在第二套端口判断。

## 6. 自动化回归

- [x] 6.1 覆盖 `SetNodeType` 的 Get/Set 方向不变量与两种 Capability 投影形状。
- [x] 6.2 覆盖 node catalog 歧义条件拒绝，以及 mode change 缺少删边时的 preflight 失败。
- [x] 6.3 覆盖“删边、配置、建边”完整 Mutation 顺序通过，并接入独立 EditMode 测试程序集。
- [x] 6.4 在 Unity 完成脚本导入后运行 `ThirdPersonClient.AgentAuthoring.Tests` EditMode 测试程序集。
