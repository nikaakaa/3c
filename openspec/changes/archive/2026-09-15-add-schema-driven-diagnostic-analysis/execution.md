# 表现复制采样执行记录

本文件由诊断采样任务窗口维护，记录 `add-schema-driven-diagnostic-analysis` 范围内对表现复制采样能力的补列交付。

## 交付记录（SAMPLING-20260907-01）

请求来源：`D:/Unity_Project_1/3C/docs/handoff-presentation-sampling-gap-20260907.md`（主线接入窗口提报，接收方为本窗口）。

### 交付内容

交接缺口共两项，均已交付：

1. **fact identity 值列**（另一实现窗口在本窗口工作期间先行提交，本窗口确认采纳，未重复实现）：
   - `61ce9ee6f`：`CharacterPresentationFactCaptureFrame`（含 `movement-mode`、移动播放时钟与 locomotion timeline 观测列）、复制事件新增 `facts` 参数、发布点保留本帧 fact 帧。
   - `4df9ad182`：capability 登记 `facts` FactRoot，修复生成器事件绑定。

2. **迁移规则评估输入快照**（本窗口交付，提交 `ac20ee980`）：
   - `CharacterPoseStateMachineRuntime`：`SelectTransition` / `SelectPredictiveTarget` 每次规则求值后记录逐操作快照行，含 state machine、transition、是否 prospective 路径、规则结果、operation code、值类型与 bool/float/enum/identity 四种值。预分配容量 = 2 × 最大出边数 × 最大规则操作数，`PrepareFrame` 清零，业务求解结果与 Commit 时机不变。
   - `AnimationPresentationRuntimeSnapshot` / `AnimationPresentationRuntimeSnapshotPublisher`：行类型 `PoseTransitionRuleEvaluationSnapshot` 与 `StateMachineRuleEvaluations` 缓冲沿状态机快照链路（状态机运行时 → 投影页 → View → 发布页 → 运行时快照）透传，页面构建仍由既有兴趣门控（LiveState|Capture）。
   - capture 侧 `animation` 根新增 `transition-rule-evaluations` 表（`animation-transition-rules` 组，容量 256），Full 采样器 IncludeAll 自动纳入；Core 采样器组闭包保持不变（与 fact 列同口径，`full` 为诊断口径）。

### 证据与边界

- 主实例 Editor（项目 `D:/Unity_Project_1/3C/3cDemo/Client/3C_Client`）正式导入编译通过，控制台零错误；生成器在编译期接受新表声明（表形状、Key、组闭包、packet 容量错误会使编译失败）。
- 未做实机复采：按交接文档分工，"同一份 1621 帧 trace 复采闭环定位" 属主线并行工作。复采时 `full.state-machines.csv` 之外将新增 `full.transition-rule-evaluations.csv` 与 `full.facts` 列，`ReadFact` 行的 `identity-value` 即规则实际读到的 `presentation.movement-mode` 值，可与姿态图字面量逐帧比对区分缺口 A/B。
- 观测边界：规则评估行只记录"产出已记录结果的那次求值窗口"内本帧全部候选拟合评估（含 predictive 路径），不跨帧保留。

### 与主线修复的关系

主线在交付期间已提交 `c262837fb`（迁移字面量对齐现行控制状态名）。观测列与本修复相互独立：列用于此后所有规则失配问题的定位，不依赖也不回退该修复。
