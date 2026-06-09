## Context
现有 `SimpleHFSM.cs` 已经证明项目需要的不是平铺 FSM，而是能够表达组合状态和叶子状态的 HFSM。它的主要缺口在核心语义：当前状态没有统一路径表达，跨层转移没有 LCA 算法，组合状态没有历史模式，临时覆盖状态没有下推栈，transition 冲突和重入请求没有统一规则。

本设计只处理纯状态机框架，不处理角色业务。角色层后续通过 `TContext` 注入输入、移动、动画和运行时数据。

## Goals
- HFSM Core 必须和 Unity 业务对象解耦，除必要编译环境外不依赖 MonoBehaviour、Animancer 或 Cinemachine。
- 状态树必须拥有清晰、可调试、可恢复的当前路径。
- 跨层转移必须使用最近公共父节点退出/进入路径。
- transition 选择必须确定性，避免同一帧行为随注册容器顺序漂移。
- 下推栈和历史恢复必须是框架语义，而不是业务层手写缓存。
- 框架必须能报告非法结构和被拒绝的转移，方便后续角色状态调试。

## Non-Goals
- 不支持正交并行区域。
- 不实现可视化编辑器。
- 不把 SimpleHFSM 原地改成兼容所有旧 API 的大而全版本。
- 不把状态序列化为 Unity 资产；本阶段只提供运行时快照数据。

## Decisions
- Decision: 新框架放在 `HFSM.cs`，不覆盖 `SimpleHFSM.cs`。
  - Rationale: 旧文件保留为试验参考，新框架可以用更干净的 API，不被 demo 命名和 public 字段约束拖住。

- Decision: 运行时事实使用 `StatePath`，而不是只暴露 `CurrentSubState`。
  - Rationale: LCA 转移、history、push/pop、调试和保存恢复都依赖完整路径。

- Decision: 跨层转移默认允许，但必须 target 属于同一棵状态树。
  - Rationale: 纯 HFSM 框架需要支持跨层；合法性由构建校验和 LCA 算法保证。

- Decision: transition 查找从当前叶子向祖先逐层查找，再按 priority、作用域深度、注册顺序决策。
  - Rationale: 子状态局部规则优先，全局规则仍可挂在父层或根层。

- Decision: 切换请求在 Enter/Exit/transition action 执行期间进入队列。
  - Rationale: 避免重入造成递归切换和半退出状态。

- Decision: history 和 pushdown stack 都进入 Core，但正交并行区域不进入第一版。
  - Rationale: history/push/pop 是单树 HFSM 的基础能力；并行区域会显著增加调度、冲突和调试复杂度。

## Risks / Trade-offs
- 跨层转移和事件队列会让实现比现有 demo 复杂。
  - Mitigation: 任务拆成小步，先实现路径和 LCA，再做 history/push，再做事件队列。

- API 一次性铺太大可能影响后续调整。
  - Mitigation: 先提供最小必要类型和只读查询，避免把业务语义塞进 Core。

- 不新增测试文件会降低回归保护。
  - Mitigation: 本阶段至少通过编译和最小手动验证；后续如果进入稳定化，再补 EditMode 测试 proposal。

## Migration Plan
1. 保留现有 `SimpleHFSM.cs` 和 `HFSMTest.cs`。
2. 在 `HFSM.cs` 实现新 Core。
3. 编译确认新旧文件可共存。
4. 后续角色状态机接入时再决定是否迁移或删除旧 demo。

## Open Questions
- `TContext` 是否需要强制为 class，还是允许 struct？建议第一版允许任意类型，但文档提示引用类型更适合保存运行时服务。
- 事件 payload 是否使用 `object`，还是泛型事件类型？建议第一版使用小型 `HfsmEvent` 包装，payload 为 `object`，避免过早泛型爆炸。
