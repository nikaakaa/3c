---
name: 3c-fast-development-validation
description: 仅用于 3C 项目的并行开发，在多个任务或 Agent 并行修改、需要隔离 worktree 并协调验证时，通过 Center 和统一 CLI 管理改动、前后运行与证据。普通单任务开发、单独编译、回放或性能分析不触发此 skill。
---

# 3C 并行开发与验证工作流

## 适用范围

仅在用户要求并行开发，或当前任务已属于明确的多任务 / 多 Agent 并行开发安排时调用。单纯存在多个聊天窗口、多个 worktree，或任务涉及 3C、编译、回放、性能优化，都不构成调用条件。

普通单任务按项目已有入口完成开发和验证，不自动引入本 skill 的 Center 改动记录与多 worktree 流程。调用本 skill 本身不授权创建 worktree、启动其他 Agent 或启动构建与采样进程，仍遵守当前任务已有的授权和限制。

作者给出改动目标，AI 负责执行，Center 以文件夹树组织任务、诊断、原始采样、前后对比、版本和执行记录。主页展示业务结论和验证缺口，编译尝试不占据任务结果。唯一工具源码仓库为 `D:/Unity_Project_1/3C-Development-Center`。流程是 `改动记录 → 同条件基线 → 修改 → 验证 → 比较 → 有证据的业务结论`，底层始终使用原 RunHost。

## 接到开发任务

1. 使用当前任务已分配的独立 worktree；仅在用户明确要求时新建 worktree，避免与其他任务共同修改被测目录。主验收 Editor 保留。
2. 从 Git 公共目录的正式配置解析选中的 RunHost，见[命令参考](references/commands.md)。不要写死产物哈希或按目录时间选工具。
3. 用户或上下文已有 `change_id` 时先 `change-show` 并继续它。新改动用 `change-create` 保存目标和 worktree；只查询状态时不新建记录。记录编号保留在当前任务上下文。
4. 使用 `change-note` 记录有意义的进展或阻塞原因。它是 AI 上次说明，不是实时心跳。不要让作者手动填写 Host、请求或 RunId。

## 执行与关联

- 只需要验证编译时使用 `compile`；角色、输入、动作、动画或相机改动使用已有输入的 `character.replay`；脚部证据用 `character.diagnostic-replay`。
- 能复用已有同条件基线时，通过 `change-attach --phase before` 关联，不重复跑。否则修改之前用 `change-run --phase before` 获取基线。
- 修改后使用 `change-run --phase after`，让提交和改动关联一并完成。现有已结束运行可以 `change-attach`；after 必须属于记录的 worktree。
- 提交返回 `run_id` 后查询这个运行，不重复提交。运行期间不得继续修改被测 worktree 或它依赖的共享包。
- 两次 Editor 验证结束后执行 `change-compare`。普通校验、Player 或性能请求可以通过 `change-submit` 关联同一个改动；其 request 仍采用既有合同。
- 日常验证不先构建 Player，不创建 Git bundle、LFS 内容包、源码 ZIP 或 SDK ZIP。只有明确要性能数据时才进入 build → smoke → replay → capture → analyze；同机性能采样排队。

## 判断与交付

`Completed` 只表示执行结束。检查 `change-show` 的 conclusion、source_changed 和比较解释；源码或包在运行中变化的结果不能充当固定版本依据。不能用编译成功证明功能正确，也不能用 Body 轨迹一致证明脚部质量、相机表现或性能改善。

角色前后比较检查工作流、Unity、录制输入、时钟模式和运行期间版本稳定性，再检查输入序列和 Body 轨迹。脚部、相机或性能没有对应分析证据时，写入尚未验证项，不推断已通过。

用 `change-review --path review.json` 保存业务说明、已核对项、尚未验证项以及正式产物中的文件引用。必须引用当前修改后运行或当前比较；CLI 验证证据属于关联运行并记录哈希。新的前后运行会清除旧比较和旧说明，历史 Run 关联保留。说明属于 AI 的判断，机器比较结论单独展示。

交付时给作者：改了什么、实际验证范围、发现的问题、Center 改动名称和 change_id；提交与前后 RunId 用于追溯。执行和继续任务都使用同一 CLI，Center 不再承担手动调度。当前代码没有实跑就明确记录未实跑；历史采样和历史诊断不能代替当前候选。已有脚部报告由 Center 按 manifest 和 sampler 文件哈希匹配关联 Run 后只读展示。

Center 使用 Avalonia，按“任务 → 诊断与报告 → 业务 Capability → 运行版本 → 类别 → 单条诊断”展开；原始 CSV 可按字段、采样序号和对象筛选。每条诊断可从问题片段定位该 Run 的采样证据。“全部实跑与报告”“全部 Player 打包”“全部执行记录”保留历史和未关联任务的运行，不为显示而重复提交或补造改动。

界面首次打开时读取一次，之后由作者点击“刷新”更新；不启用定时轮询或自动重绘。切换阅读页会保留滚动和筛选状态，手动刷新才重新读取数据。当前没有 RunHost 完成事件订阅，AI 仍用正式 CLI 查询运行是否结束，不把界面的旧快照当作实时状态。

单条报告由采样框架的正式生成器写入 `Rules/<capability>/<category>/<rule-id>.md`，包含输入、Plan、Analyzer 和原 diagnosis 的哈希。已有诊断可使用 `generated-diagnostic-sampling/Tools/KK.GeneratedDiagnosticSampling/Reports` 的正式 CLI `--diagnosis <diagnosis.json>` 生成逐条阅读文件；不重采、不改写旧诊断与评分。新业务使用统一诊断格式和 Capability 身份，Center 不增加专用分析器。

对比页可选择已关联实跑 A/B，但历史组合不代表当前候选完成验证。只有正式输入与 Body 比较一致、两次运行版本稳定、分析 Plan/Analyzer/Schema/Sampler 一致时才显示对应证据的数值差。其他情况只并排显示原结果，不以问题片段数量或差值符号替代业务判断。AI 必须明确保存已核对项和未验证项；缺少实跑、领域诊断或业务结论时，不报告任务已经完成。

## 执行边界

每个 worktree 有独立 Unity 项目和 Library，同一 worktree 使用独占锁。编译直接执行 Unity batchmode；回放通过共享 Unity MCP 的 HTTP Bridge 和每个 worktree 的纯实例 hash 调用 `character.fixed_input_trace`，所有 CLI 请求显式传实例。普通验证允许不同 worktree 并行，不保证任意数量同时启动都能变快。

遇到 `WorkspaceDependenciesMissing` 时修正正式 `Packages/manifest.json` 依赖；不复制临时包、不增加 fallback。若选中 Host 不支持 change 命令，按正式发布与选择流程更新工具，不能另建一套记录脚本。

Host 2.1.1 起，源码识别仅额外排除 Unity 项目 Diagnostics/GeneratedFootSampling 内的未跟踪采样输出。已跟踪变更、录制输入和 Assets 下的生成源码仍参与身份。旧 Run 不重算 source_changed；如果旧标记由这些输出触发，保留标记并说明准确差异，不能改写历史证据或自动给出 Matched。
