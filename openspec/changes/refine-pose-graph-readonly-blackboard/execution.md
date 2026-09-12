# PoseGraph只读Blackboard实施记录

## 2026-09-12 当前小步

已完成：

- `216bccd99`：FlowCanvas原生Pose Blackboard只投影外部Control输入，AnimatedProperty不再进入作者变量列表。
- `f8d6e78d4`：动画输入合同从根图Control声明和Profile属性绑定分别构建参数布局；属性导入器不再把BlendShape声明复制到每张PoseGraph。
- `779e9273f`、`1208a06f8`、`e260de9e2`：统一作者显示名、输入类别/作用范围投影、Document参数的Usage/displayName导出解析，以及Graph参数声明校验。
- `fb89142d4`、`9d916f0a9`、`8818c7d1c`、`9fdbc321b`：FootPlacement默认从输入Pose的内部曲线参数列读取；只有直接连接公开Pose输入Get时才允许外部覆盖，旧Body GraphInput透传会在编译时明确失败。

代码输入输出边界：

```text
Graph Control declaration -> read-only Blackboard/Get
Profile BlendShape binding -> animation input curve declaration -> source scalar page
Input Pose source-local Foot curve -> FootPlacement internal weight read
```

检查记录：

- `ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；编译后已执行`dotnet build-server shutdown`。
- `ThirdPersonClient.Editor.csproj`被工作区已有的`TimelineClipCreationPopup.cs`未定义`TimelineBindingValueKind`、`TimelineBindingAccess`和`TimelineBindingLifetime`阻塞；未修改该交叉文件，随后已执行`dotnet build-server shutdown`。
- 未运行Unity、Character Build、Play或资产导入；未修改用户未提交的PoseGraph/Profile/Scene资产。

仍未完成：

- EventGraph正式变量Contract/Layout/Frame尚未交付，本change没有创建第二变量更新器。
- 当前Corin Pose资产仍需要通过正式Document/Mutation删除旧的Action/Foot声明、根图Get、Body透传端口和确认无引用的重复子图。
- Subgraph/Linked Pose跨图可访问范围、完整曲线依赖编译收口、全部Document/Exporter/Reconciler/Validator同步和最终现行spec更新仍待继续。

## 2026-09-13 当前小步

已完成：

- `d82a7afbb`、`3e788506a`：为Pose子图接口增加内部Foot曲线端口门禁，并修正签名校验调用位置；内部曲线不得通过GraphInput、GraphOutput或Subgraph Call公开透传。
- `32f2cbba1`：原生Pose Blackboard主菜单不再显示内部Owner identity，只显示作者需要的输入信息。
- `9a448cfb8`、`4a8625e95`、`43cfcb34c`：纯十六进制稳定身份不进入作者主显示；Root、Animation Layer、Control Rig、Transition Rule及参数/骨骼下拉统一使用语义名称。
- `67e688fad`：FootPlacement和普通参数节点在输入合同缺项时返回带图/节点范围的明确编译诊断。
- `6034ba2a0`、`3367b6495`：拓扑校验接收同一动画输入合同，允许合法输入Pose曲线参与Resolve策略校验，并同步编辑器 Validate 与 Document Apply 的合同来源。
- `f612f8449`：节点和动态端口缺少显式作者名时使用语义显示名兜底，连接与稳定identity不变。
- `9427ba641`、`b0672667d`：Blackboard重命名同步到复用的原生变量对象；State与Subgraph页面标题过滤不透明GUID名称。
- `8b2d611d4`：拓扑校验要求跨图复用的同一外部参数保持类型、单位、默认值和Usage一致，避免形成第二份输入定义。

正式 Document 流程：

- 已通过 Unity MCP CLI 以实例 `e852139597e42532` 调用 `btsmtl.checkout_document`，目标为 `Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- checkout 在 SkillDefinition 完整性检查阶段失败，报告缺少原生状态机资产以及 Blackboard/ActionTargetSnapshot declaration；没有进入 Pose Document 写入或资产 Apply，也没有生成 `AgentAuthoring` 半成品目录。
- 随后以同一实例调用只读 `btsmtl.validate`，在相同 SkillDefinition 入口返回相同错误；因此当前还没有可用于 Pose 资产迁移的有效 Document hash 或 Mutation plan。

当前约束：

- 上述代码提交未修改当前用户未提交的PoseGraph、Profile、Definition或Scene资产。
- 当前工作区的Character Document package尚未checkout，不能绕过Document/Mutation直接编辑PoseGraph YAML；旧的Action/Foot声明、根图Get、Body透传端口和孤立重复子图仍需在正式Document/Mutation流程中删除。
- `CharacterPoseGraphProjectionValidator` 的子图签名门禁已进入代码，但尚未对当前资产执行Unity Validate/Build；编辑器交叉编译仍受工作区既有Timeline改动影响。
