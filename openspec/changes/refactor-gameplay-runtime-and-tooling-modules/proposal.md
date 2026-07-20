# Change: 深化 Gameplay Runtime 与 Tooling 模块边界

## Why

当前角色模拟、ServerAuthoritative 双客户端链路、确定性 Rollback Target、BTSMTL Graph/Timeline 编辑器和三种 Network Test Product 已经形成可工作的正式主链，但实现仍有几处会阻碍后续继续扩展：

- Float32 与 Fixed Target 分别复制 Timeline、GameplayEffect、Pipeline Transaction 等业务控制算法；修复一次状态推进规则需要改两份实现，已经偏离“同一 Semantic Operation Set”。
- neutral Simulation Core 仍枚举 `UnityAuthorityWorker`、`DotRecastAuthorityScene` 等具体 Host Profile；新增 Authority backend 仍要修改 Core。
- Remote Presentation 直接追到最新 selected Body target，低频或不均匀网络帧下会形成视觉顿挫；同时 completed contact contract 又明确禁止另建 Body delay cursor，因此需要在同一选择流上增加纯视觉收敛。
- Tick 热路径仍重复创建 immutable roster、transaction collection、Timeline segment、GameplayEffect 临时集合和 Trace/Facts 容器，角色数与 replay 次数增加后会放大 GC 和抖动。
- Camera Graph 节点已经暴露 authoring 类型，但 Compiler emitter 未闭合，节点只能在编辑器里存在，无法稳定进入正式 Program。
- Fantasy Unity endpoint、BTSMTL Tree/Timeline 编辑器和三种 Network Test Build/Run 工具分别承担过多职责或复制相同流程，维护时必须跨越千行类或同步修改多个工具。

这些问题目前没有要求推倒 Simulation Session、Kernel、WorldSolver、Network Model 或 Editor 用户界面。需要做的是把已经存在的正式链路内部深化成清楚的 owner，并删除 Target、Host、工具之间的重复实现。

## What Changes

- 将 Timeline 控制、GameplayEffect 生命周期与堆叠、Pipeline Transaction 阶段顺序迁入 portable shared runtime；Float32/Fixed 只保留各自 Program/State/Numeric ABI、数值运算、曲线采样和 Target state access。
- 为 Program/Session 建立明确的 immutable execution services 与 actor-owned reusable workspace，移除正常 Tick 中重复索引、roster、segment 和临时集合构建；Snapshot/外部输出在越过事务边界前仍必须独立冻结。
- 从 neutral Simulation Core 移除具体 ServerAuthoritative Host Profile；由 Unity Authority Product 与 DotRecast Authority Product 分别拥有自己的 Host identity、manifest 和 launch lowering。
- 在 committed selected Body stream 上增加 presentation-only visual pose convergence/filter；Canonical contact、可靠事件 horizon 和 WorldSolver 继续使用原 selected Body，不增加远端 Body authority 时钟。
- 为现有 Camera Graph 节点补齐 versioned Program operation、Compiler emitter、Source Map 和 PresentationCommand 输出闭环；节点仍只提交强类型 Camera request，不直接控制 Cinemachine。
- 将 Fantasy Unity endpoint 内部分为 control session、datagram、checkpoint reconstruction、prediction evidence/metrics 模块，由唯一 connection coordinator 负责状态转换与释放；不新增第二 endpoint 或 transport 路径。
- 在不改变现有窗口、页签、序列化 identity、Undo/Redo 与 Live Debug 行为的前提下，拆分 BTSMTL Tree/Timeline Editor 内部的 view state、mutation、geometry、rendering、inspector 与 navigation 职责。
- 建立唯一 Editor-only Network Test Product Build Workflow；Unity Authority、DotRecast Authority 与 Deterministic Rollback 通过显式 adapter 提供产品差异，统一进程执行、原子目录替换、exact manifest 校验与 Build/Run 分离。
- 完成迁移后删除重复 Target runtime、Core 中的具体 Host Profile、旧工具私有 helper 和大类中已迁出的实现，不保留兼容层、fallback 或双路径。

## Non-Goals

- 不改变 Corin locomotion、attack combo、dodge、interruption、Timeline window 或动画 producer 的业务配置语义。
- 不合并 Float32 与 Fixed 的 Program/State/Numeric ABI，不把 fixed 约束泄漏到单机或 Unity Authority Target。
- 不替换 `SimulationKernel`、`ICharacterWorldSolver`、Session Composition、Runtime Launcher 或现有 Network Model。
- 不重写 Deterministic KCC、Unity CharacterController Solver、DotRecast Solver 或 Fantasy Gate Room 状态机。
- 不增加 remote Body delay buffer、第二 selected tick、客户端权威 pose、运行时 Network Model 切换或 shared build output。
- 不改变 Tree/Timeline Editor 的对外 UI 信息架构，不新增第二写入口。
- 不新增测试代码或把人工验证写入 tasks。

## Dependencies

- 当前 Action/Combo authoring 已进入current specs；本 change 只以其已安装Program operation和业务结果为基线，不接管或改写 combo 业务语义。
- Predicted Actor Contact、ServerAuthoritative Host Products 与 DeterministicRollback 已归档并进入current specs，本 change 直接以这些已安装 contract 为输入，不再保留待归档前置条件。
- 如果 portable shared runtime 无法在不改变现有 Program/State ABI 的前提下统一业务控制语义，或序列化资产不能安全重建，实施 MUST停止并说明 tradeoff，不得保留双实现。

## Current Spec Comparison

- `character-simulation-kernel` 已要求唯一 Semantic Operation Set 和 Program 级执行服务，但当前只共享 Runnable/Composite/StateMachine control；Timeline、GameplayEffect 与 Pipeline Transaction 仍重复，本 change 收紧该要求。
- `character-presentation-interpolation` 已要求 Remote Body 表现与 contact 使用同一 selected stream，并禁止独立 Body delay cursor；本 change 只在其上增加 visual pose convergence，不修改 authority 选择。
- `character-camera-pipeline` 已要求 BTSMTL Camera 节点提交强类型请求；当前 Compiler emitter 缺失，本 change 补齐 authoring 到 Program 的实现合同。
- `server-authoritative-host-portability` 已要求 Host-neutral Pipeline/Source/Composer，但没有禁止 Core 枚举具体 Host Profile；本 change 补上产品所有权。
- `fantasy-unity-authoritative-session` 已要求 Fantasy callback 只写 Source queue；本 change 进一步约束 endpoint 内部模块由一个 coordinator 统一拥有。
- Tree Inspector 与 Timeline Editor current specs 已定义 UI 和 Live Debug 语义；本 change 只增加内部模块所有权，不改变作者工作流。
- 当前 specs 没有三种 Network Test Product 共用 Build/Run 编排的能力，因此新增 `gameplay-network-test-build-workflow` capability。

## Impact

- 运行时：`Runtime/Simulation/Core`、Float32/Fixed Target、ServerAuthoritative identity、Remote Presentation、Camera operation/emitter、Fantasy Unity endpoint。
- 编辑器：BTSMTL Tree/Timeline Editor、Character Simulation compiler、Network Test Build/Run 菜单和 product adapters。
- 资产与产物：ProgramHash/LayoutHash、Host manifest、Network Test output manifest 可能因正式 schema/operation 变化而重建；不保留旧 hash 或旧 manifest reader。
- OpenSpec：修改七个现有 capability，新增一个 Network Test Build Workflow capability，并同步 `openspec/project.md` 的模块边界描述。
