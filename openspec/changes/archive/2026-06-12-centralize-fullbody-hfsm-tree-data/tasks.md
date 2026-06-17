> 已被 `refactor-unified-character-state-machine` 接管：本变更不再作为后续实现基线，中心 HFSM 树数据产物需要按统一状态机删除、回滚或归并。

## 1. 现状复核
- [x] 1.1 读取 `add-fullbody-hfsm-state-tree` 的 proposal、design、tasks 和 spec delta。
- [x] 1.2 确认 `FullBodyHfsmStateTreeBuilder.Create()` 当前硬编码 Root、Locomotion、Action 和 Dodge。
- [x] 1.3 确认 `FullBodyHfsmStateTreeDriver.ResolveOwner` 当前对 Action.Dodge 的固定假设。
- [x] 1.4 确认 `FullBodyHfsmStateIds` 当前负责路径拼接。
- [x] 1.5 确认 `FullBodyActionSetSO` 仍是动作逻辑配置入口，不是状态树或动画配置闭环入口。
- [x] 1.6 确认 Locomotion 状态图配置不并入本树资产。

## 2. 数据模型
- [x] 2.1 新增 `FullBodyHfsmNodeKind`。
- [x] 2.2 新增内嵌 serializable `FullBodyHfsmNodeDefinition`。
- [x] 2.3 为节点定义 `nodeId` 字段。
- [x] 2.4 为节点定义 `pathSegment` 字段。
- [x] 2.5 为节点定义 `kind` 字段。
- [x] 2.6 为节点定义可选 `BasicMovementPhase` 绑定字段。
- [x] 2.7 为节点定义可选 `ActionStateId` 字符串绑定字段。
- [x] 2.8 为节点定义内嵌 `children` 列表。
- [x] 2.9 新增 `FullBodyHfsmTreeDefinitionSO`。
- [x] 2.10 为树资产提供只读 Root 节点访问。
- [x] 2.11 为测试提供构造当前默认树的最小方法或 fixture。

## 3. 默认资产
- [x] 3.1 在 `Assets/Configs/3C/Statemachine/FullBody` 下创建默认 FullBody HFSM 树资产。
- [x] 3.2 默认资产 Root 节点为 `FullBody`。
- [x] 3.3 默认资产 Root 下包含 `Locomotion` composite 节点。
- [x] 3.4 默认资产 Root 下包含 `Action` composite 节点。
- [x] 3.5 `Locomotion` 下包含 `Idle` phase 节点。
- [x] 3.6 `Locomotion` 下包含 `MoveStart` phase 节点。
- [x] 3.7 `Locomotion` 下包含 `MoveLoop` phase 节点。
- [x] 3.8 `Locomotion` 下包含 `MoveStop` phase 节点。
- [x] 3.9 `Action` 下包含绑定 `Action.Dodge` 的 `Dodge` 节点。
- [x] 3.10 默认资产不引用 Dodge motion config。
- [x] 3.11 默认资产不引用 Action animation profile。
- [x] 3.12 默认资产不引用 Action interrupt policy set。

## 4. 校验器
- [x] 4.1 新增树校验结果模型。
- [x] 4.2 校验 Root 存在。
- [x] 4.3 校验 Root 唯一。
- [x] 4.4 校验 node id 非空。
- [x] 4.5 校验 node id 不重复。
- [x] 4.6 校验 path segment 非空。
- [x] 4.7 校验同级 path segment 不重复。
- [x] 4.8 校验完整路径不重复。
- [x] 4.9 校验 `LocomotionPhase` 节点必须绑定 phase。
- [x] 4.10 校验 `BasicMovementPhase` 绑定不重复。
- [x] 4.11 校验默认树包含 Idle。
- [x] 4.12 校验默认树包含 MoveStart。
- [x] 4.13 校验默认树包含 MoveLoop。
- [x] 4.14 校验默认树包含 MoveStop。
- [x] 4.15 校验 `Action` 节点必须绑定有效 action state id。
- [x] 4.16 校验 Action state id 绑定不重复。
- [x] 4.17 校验默认树包含 `Action.Dodge`。
- [x] 4.18 校验 `Action.Dodge` 位于 `/FullBody/Action` 分支。
- [x] 4.19 校验 Composite 和 Root 节点不得绑定 phase/action。

## 5. 编译器和路径解析
- [x] 5.1 新增 compiled tree 模型。
- [x] 5.2 新增 compiled node 模型。
- [x] 5.3 编译时递归计算每个节点完整路径。
- [x] 5.4 编译时建立 node id 查询表。
- [x] 5.5 编译时建立完整路径查询表。
- [x] 5.6 编译时建立 phase 到节点查询表。
- [x] 5.7 编译时建立 action state 到节点查询表。
- [x] 5.8 编译失败时返回校验错误，不创建部分可用运行时树。
- [x] 5.9 提供 `TryResolveLocomotionPhase` 或等价查询。
- [x] 5.10 提供 `TryResolveActionState` 或等价查询。
- [x] 5.11 提供只读枚举所有节点的接口。

## 6. Builder 迁移
- [x] 6.1 为 `FullBodyHfsmStateTreeBuilder` 增加接收 compiled tree 的入口。
- [x] 6.2 builder 从 compiled Root 创建 root state。
- [x] 6.3 builder 从 compiled composite 节点创建子 state machine。
- [x] 6.4 builder 从 compiled Locomotion phase 节点创建 Locomotion 子 state。
- [x] 6.5 builder 从 compiled Action 节点创建 Action 子 state。
- [x] 6.6 builder 不再硬编码 `BasicMovementPhase.Idle/MoveStart/MoveLoop/MoveStop` 列表。
- [x] 6.7 builder 不再硬编码 `Dodge` 子状态。
- [x] 6.8 builder 保持现有 Action active 和 Locomotion 回退 transition 语义。
- [x] 6.9 builder 不直接读取 Input System、Animancer、CharacterController 或 Cinemachine。

## 7. Driver 和 snapshot 迁移
- [x] 7.1 driver 保存 compiled tree 引用。
- [x] 7.2 `AlignLocomotionPhase` 使用 phase 查询表定位节点名。
- [x] 7.3 active path 从 compiled node path 获取。
- [x] 7.4 pending transition path 从 compiled node path 获取。
- [x] 7.5 owner 推导读取 active compiled node kind。
- [x] 7.6 Locomotion owner 推导读取 phase 绑定。
- [x] 7.7 Action owner 推导读取 action state 绑定。
- [x] 7.8 移除 `if Action branch then Dodge` 的长期固定假设。
- [x] 7.9 保持 `FullBodyStateSnapshot` 不引用 UnityHFSM 内部 state 对象。

## 8. Runtime 和 prefab 接入
- [x] 8.1 `PlayerFullBodyActionController` 增加序列化树资产引用。
- [x] 8.2 初始化时校验树资产。
- [x] 8.3 初始化时编译树资产。
- [x] 8.4 初始化时用 compiled tree 创建 HFSM driver。
- [x] 8.5 树资产缺失时报告明确错误。
- [x] 8.6 树资产非法时报告明确错误。
- [x] 8.7 当前主角色 prefab 绑定默认 FullBody HFSM 树资产。
- [x] 8.8 确认没有新增第二个 FullBody coordinator 或 per-action controller。

## 9. 只读编辑器预览
- [x] 9.1 新增只读 Inspector 或 EditorWindow。
- [x] 9.2 预览显示节点层级。
- [x] 9.3 预览显示每个节点完整路径。
- [x] 9.4 预览显示节点 kind。
- [x] 9.5 预览显示 phase/action 绑定。
- [x] 9.6 预览显示校验错误。
- [x] 9.7 预览显示校验 warning。
- [x] 9.8 预览不提供拖拽改树。
- [x] 9.9 预览不提供图形连线。
- [x] 9.10 预览不提供动作 timeline 写入。

## 10. 自动测试
- [x] 10.1 测试默认树包含 Root。
- [x] 10.2 测试默认树包含 Locomotion 分支。
- [x] 10.3 测试默认树包含 Action 分支。
- [x] 10.4 测试默认树包含 Idle phase 节点。
- [x] 10.5 测试默认树包含 MoveStart phase 节点。
- [x] 10.6 测试默认树包含 MoveLoop phase 节点。
- [x] 10.7 测试默认树包含 MoveStop phase 节点。
- [x] 10.8 测试默认树包含 `Action.Dodge` 节点。
- [x] 10.9 测试重复 node id 报错。
- [x] 10.10 测试重复完整路径报错。
- [x] 10.11 测试重复 phase 绑定报错。
- [x] 10.12 测试重复 action state 绑定报错。
- [x] 10.13 测试 Composite 绑定 phase/action 报错。
- [x] 10.14 测试 `/FullBody/Locomotion/MoveLoop` 路径解析。
- [x] 10.15 测试 `/FullBody/Action/Dodge` 路径解析。
- [x] 10.16 测试 builder 从 compiled tree 创建 HFSM。
- [x] 10.17 测试 Locomotion active path 来自 compiled node。
- [x] 10.18 测试 Action.Dodge active path 来自 compiled node。
- [x] 10.19 测试 Action.Dodge owner 来自 compiled action 绑定。
- [x] 10.20 测试非法树不会静默创建运行时 HFSM。
- [x] 10.21 静态测试树定义资产代码不引用 Animancer 播放 API。
- [x] 10.22 静态测试树定义资产代码不调用 `CharacterController.Move`。
- [x] 10.23 静态测试树定义资产代码不引用 `BBBNexus`。

## 11. 文档和验证记录
- [x] 11.1 更新 `docs/agents/character-animation-state-roadmap.md`，记录 FullBody HFSM 树数据归属。
- [x] 11.2 记录树资产只负责拓扑和绑定，不接管 Dodge/Locomotion 业务配置。
- [x] 11.3 记录后续 Roll/Jump/Attack 节点必须通过中心树资产接入。
- [x] 11.4 运行 `openspec validate centralize-fullbody-hfsm-tree-data --strict --no-interactive` 并记录结果。
- [x] 11.5 运行定向 EditMode 测试并记录结果。
- [x] 11.6 记录静态边界检查结果。

验证记录：
- `openspec validate centralize-fullbody-hfsm-tree-data --strict --no-interactive`：passed。
- `dotnet build .\Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly`：passed。
- `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly`：passed，剩余 6 个既有参考代码 warning。
- Unity EditMode 定向测试 `ThirdPersonAction.Tests.FullBodyHfsmTreeDataTests`、`ThirdPersonAction.Tests.FullBodyActionFrameworkTests`：47/47 passed。
- 静态边界检查：树定义/编译器源码不引用 Animancer 播放 API、不调用 `CharacterController.Move`、不引用 `BBBNexus`。
- Path 文档检查：本仓库无 `DG_Entity/docs/Path` 对应文档，本次为 no-op；已更新本仓库 `docs/agents/character-animation-state-roadmap.md`。

## 12. 用户手动验证
- [x] 12.1 用户在 Unity Editor 中选择默认 FullBody HFSM 树资产。
- [x] 12.2 用户确认只读预览显示 `/FullBody/Locomotion/Idle`。
- [x] 12.3 用户确认只读预览显示 `/FullBody/Locomotion/MoveStart`。
- [x] 12.4 用户确认只读预览显示 `/FullBody/Locomotion/MoveLoop`。
- [x] 12.5 用户确认只读预览显示 `/FullBody/Locomotion/MoveStop`。
- [x] 12.6 用户确认只读预览显示 `/FullBody/Action/Dodge`。
- [x] 12.7 用户进入 Play Mode 后确认普通 WASD 状态路径仍随 Locomotion phase 变化。
- [x] 12.8 用户进入 Play Mode 后确认按 Shift 时状态路径仍显示 `/FullBody/Action/Dodge`。
- [x] 12.9 用户确认 Dodge active 时基础移动不叠加平面位移或 base layer 动画。
