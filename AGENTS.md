# 3C 项目 Agent 指令

根目录统一使用 `AGENTS.md` 作为 agent 指令文件，不再创建或维护 `AGENT.md`。`openspec/AGENTS.md` 是 OpenSpec 子目录的工具说明，按需读取，不和根目录入口合并。

## 项目目标

本项目目标是制作一个求职向、高技术含量、可演示、可讲解、可测试的 3C demo。重点不是堆功能数量，而是展示角色控制、相机、动画、运动驱动、状态机、配置化、工具链、测试和后续网络同步准备能力。

3C 的核心含义：

- Character：角色能力、状态机、动画分层、动作接管、IK、音频与表现事件。
- Camera：第三人称相机、锁敌、瞄准、构图、碰撞、模式切换。
- Control：输入意图、移动手感、Root Motion 策略、运动权威、可预测的数据流。

## 工作规则

- 所有生成代码尽量不要写注释；只有关键设计、复杂边界或容易误用的地方用少量注释解释。
- 修改代码使用系统文件工具，不通过 Unity MCP 写文件。
- 读取文档必须显式使用 UTF-8。PowerShell 使用 `Get-Content -Encoding UTF8`。
- 搜索文件和文本优先使用 `rg`。
- 永远不要运行 Unity batchmode。
- 不做未审批的绕路实现。凡是需要绕过当前系统、额外创建分裂路径、引入临时独立配置或临时 fallback 的实现，必须先停下来说明原因。
- 不要新增 fallback 配置；要配置就做正式配置。
- 严格注意抽象和实现分离，模块化编程，对扩展开放，对修改关闭。
- 任务颗粒度要非常细，完成一个小闭环再进入下一个。
- 每次重构后不要直接宣称“重构完了”；说明真正还需要做的事情，并询问用户的重构意图是否已经被满足。
- 不删除已有 log，除非用户明确要求。

## OpenSpec 工作流

- 涉及新能力、破坏性变更、架构调整、计划、proposal、spec 或含糊的大改动时，先读取 `openspec/AGENTS.md` 和 `openspec/project.md`。
- OpenSpec 的说明、proposal、design、tasks 和 spec 内容除固定格式关键字外使用中文。
- 创建或修改 OpenSpec change 时必须包含测试设计或自动化测试任务。
- 不把手动验证写入 OpenSpec 的 `tasks.md`。
- 交付时必须明确告诉用户怎么验证，包括自动化测试命令和必要的 Unity 内手动验证步骤。
- 用户说已经 archive，视为用户已经测试过，直接归档，不额外设置阻塞条件。
- 实现前把任务拆细，按 OpenSpec 任务顺序逐项完成。

## 参考目录

参考资料根目录是 `D:\Unity_Project_1\3C\Ref`。

`Ref` 目录只能作为学习和对照来源，不作为当前项目的运行时依赖。需要参考时先总结它的设计意图，再按当前项目架构落地。特别关注：

- `Ref/BBB-Nexus`：只作为 BBB 设计参考和可选代码来源。本项目会重新做自己的 3C 框架，后续代码逻辑不得依赖、继承、调用或挂接 BBB 的运行时类型、命名空间、Prefab、配置资产或系统主线。若 BBB 中有可复用代码，可以直接复制一份到当前项目对应模块内，复制后必须按当前项目命名、边界和抽象重新归属，视为本项目代码维护。
- `Ref/zzzdemo-source-code`、`Ref/HoMiyabi` 等：动作表现、连招、镜头、技能反馈。
- 其他第三人称控制器参考：只吸收思路，不引入未审批的独立控制路径。

## 第一阶段目标

第一步不是直接写移动代码，而是先做基础移动的最小可展示纵切：

- 输入读取到运行时意图。
- 相机相对方向计算。
- Idle / MoveStart / MoveLoop / MoveStop 状态闭环。
- Animancer 播放动画和过渡。
- 位移权威统一进入当前运动 executor / `CharacterMotionDriver` 或已审批的运动驱动层。
- Root Motion 策略明确：基础循环优先输入驱动；起步、急停、闪避、翻越等再考虑烘焙曲线或 Warp。
- 配套 EditMode 测试；手动端到端验证只在交付说明中给出建议，不写成 OpenSpec 阻塞任务。

## 架构原则

- 不另起一套未审批的角色控制器路径。
- 不绕过当前系统做临时方案；凡是需要绕过现有聚合点、状态机、动画外观层或运动驱动层的实现，必须停止并说明原因。
- 抽象和实现分离。输入、意图、状态、运动、动画播放、表现事件不要塞进同一个 MonoBehaviour。
- 配置优先。动画资源、速度参数、过渡参数、动作窗口、IK 曲线等应进入 ScriptableObject 或明确的数据模块。
- 动画播放通过 Animancer 外观层收敛，不让业务状态直接散落大量 Animancer 细节。
- 物理位移权威只能有一条主路径。基础移动、动作位移、Root Motion 采样、Warp 修正最终都应进入同一个运动驱动出口。

## 状态机库选择

- 当前角色业务主线使用项目自研统一分层状态机：`CharacterStateMachineDefinitionSO -> CharacterStateMachineRunner -> CharacterStateMachineFrame`。
- `FullBody/Locomotion/...` 与 `FullBody/Action/...` 必须归属同一棵状态树，不得恢复 Locomotion、Action 两套状态机再外层缝合的分裂路径。
- 状态机代码按 `Model / Config / Solver/Runtime|Timeline|Transition|Output|Validation` 归档，中心状态机配置资产放在 `Assets/Configs/3C/StateMachine/`。
- 实现状态机 runtime、timeline facts 或输出解析前先读 `docs/agents/character-hierarchical-state-runtime-guide.md`。
- `com.inspiaaa.unityhfsm` 仍在包依赖中，但当前只作为第三方库参考；未经新的 OpenSpec 审批，不得接入为正式角色状态机 engine。
- `docs/agents/unityhfsm-usage-guide.md` 只作为历史参考和 API 对照，不作为当前角色业务主线指南。

## Root Motion 策略

- 基础移动循环默认不使用完整 Root Motion。
- 起步、急停、转身可参考 BBB 的离线 Root Motion 烘焙方式：把动画轨迹采样成速度曲线、旋转曲线和脚相位，再由当前运动 executor / `CharacterMotionDriver` 执行。
- 闪避、翻滚、翻越、攀爬等强位移动作可使用 Warped Motion 或明确的动作位移数据。
- 完整 `Animator.applyRootMotion` 只适合短时全身接管动作，必须有进入和退出清理。
- 不在多个状态里随意直接调用 `CharacterController.Move(animator.deltaPosition)`，除非该路径已经被明确设计为统一运动驱动的一部分。

## 测试要求

- 实现代码必须配套 Unity Test Framework 的 EditMode 测试。
- 普通 Unity Editor 辅助代码不强制新增测试，例如菜单、Inspector、Bootstrapper、批处理修复脚本和一次性编辑器工具；但如果 Editor 代码包含复杂规则、资产写入、反射扫描、配置迁移或会影响运行时数据，仍应优先补 EditMode 测试。
- 窄改动写聚焦测试；影响状态机、运动驱动、动画外观层、输入意图或配置模块时扩大测试覆盖。
- 自动化测试验证纯逻辑、状态流转、配置解析、参数计算和边界条件。
- 测试执行优先通过 Unity MCP 跑定向 EditMode 测试。
- 如果 Unity MCP 不可用、连接失败或当前环境无法执行 Unity 测试，不要伪造测试结果；说明未执行，并提供手动验证步骤，或请用户在 Unity 内自行运行对应测试。
- 端到端体验由用户在 Unity 内手动验证；agent 可以提供清晰步骤，但这些步骤不是 OpenSpec 任务项，也不是归档阻塞项。

## 求职展示标准

每个完成的能力都要能回答三个问题：

- 展示出来好不好看、手感是否明确。
- 架构上为什么这样做，和普通 demo 有什么区别。
- 面试时能不能打开代码、测试、工具或配置讲清楚技术含量。

优先做能体现深度的能力：输入意图管线、状态分层、Animancer 外观层、运动驱动、Root Motion 烘焙、Motion Warping、IK、动作仲裁、相机模式、测试和可视化调试。

<!-- gitnexus:start -->
## GitNexus 代码智能

本项目已经被 GitNexus 索引为 **3c**，当前索引包含 43055 个符号、81481 条关系和 300 条执行流。理解代码、评估影响和安全导航时优先使用 GitNexus MCP 工具。

如果索引过期，在项目根目录运行 `node .gitnexus/run.cjs analyze`，它会自动选择可用 runner。如果还没有 `.gitnexus/run.cjs`，运行 `npx gitnexus analyze`；如果遇到 npm 11 崩溃，先执行 `npm i -g gitnexus`。

### 必须做

- 修改任何函数、类或方法前，必须先运行 `impact({target: "symbolName", direction: "upstream"})`，并向用户报告影响范围：直接调用方、受影响执行流和风险等级。
- 提交前必须运行 `detect_changes()`，确认改动只影响预期符号和执行流。做回归审查时使用 `detect_changes({scope: "compare", base_ref: "main"})`。
- 如果 impact 返回 HIGH 或 CRITICAL，继续编辑前必须先警告用户。
- 探索陌生代码时，优先使用 `query({query: "concept"})` 查找执行流，而不是只靠文本搜索。
- 需要理解某个符号的完整上下文时，使用 `context({name: "symbolName"})` 查看调用方、被调用方和参与的执行流。

### 禁止做

- 未运行 impact 分析前，不要编辑函数、类或方法。
- 不要忽略 HIGH 或 CRITICAL 风险。
- 不要用普通查找替换来重命名符号；重命名使用 GitNexus `rename`。
- 未运行 `detect_changes()` 前不要提交。

### 资源

| 资源 | 用途 |
| --- | --- |
| `gitnexus://repo/3c/context` | 代码库概览和索引新鲜度 |
| `gitnexus://repo/3c/clusters` | 全部功能区域 |
| `gitnexus://repo/3c/processes` | 全部执行流 |
| `gitnexus://repo/3c/process/{name}` | 单个执行流的逐步追踪 |

### CLI 参考

| 任务 | 读取技能文件 |
| --- | --- |
| 理解架构或“X 是怎么工作的” | `.claude/skills/gitnexus/gitnexus-exploring/SKILL.md` |
| 影响分析或“改 X 会破坏什么” | `.claude/skills/gitnexus/gitnexus-impact-analysis/SKILL.md` |
| 追踪 bug 或“为什么 X 会失败” | `.claude/skills/gitnexus/gitnexus-debugging/SKILL.md` |
| 重命名、抽取、拆分、重构 | `.claude/skills/gitnexus/gitnexus-refactoring/SKILL.md` |
| 工具、资源和 schema 参考 | `.claude/skills/gitnexus/gitnexus-guide/SKILL.md` |
| 索引、状态、清理、wiki CLI 命令 | `.claude/skills/gitnexus/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
