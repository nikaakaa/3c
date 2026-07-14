## 1. 现状与迁移边界

- [x] 1.1 记录旧 `Timeline.Evaluate -> TreeClip.Evaluate -> TimelineRunningTree.UpdateTree` 链路。
- [x] 1.2 记录当前 Scheduler 显式轨道采样中缺少 TreeTrack 的回归点。
- [x] 1.3 对照 current spec 确认 TreeClip 保留要求仍有效。
- [x] 1.4 确认项目当前不存在需要兼容迁移的 TreeClip 业务资产。
- [x] 1.5 列出 `fix-corin-action-lifecycle-and-dodge-interruption` 中与 DodgeMoveToRun 重叠的任务和 spec delta。
- [x] 1.6 固定 apply 顺序为通用 runtime、authoring、Corin 原子迁移、旧路径删除。

## 2. Inline TreeClip 数据模型

- [x] 2.1 验证 Timeline managed-reference 能安全序列化 inline TimelineRunningTree。
- [x] 2.2 验证 inline tree 的节点、边、PropertyPort 和 ExposedProperty 保存后身份稳定。
- [x] 2.3 验证 Timeline asset clone 后 inline tree runtime 数据彼此隔离。
- [x] 2.4 若任一 inline 序列化前置条件不成立，停止 apply 并记录缺口。
- [x] 2.5 定义 TreeClip inline/shared 唯一引用模型。
- [x] 2.6 删除旧 `TreeAsset + TreeInstance` 双字段模型。
- [x] 2.7 删除旧 TreeProperty 隐式 override 路径。
- [x] 2.8 创建 TreeClip 时自动生成 inline TimelineRunningTree。
- [x] 2.9 删除 TreeClip owner 时同步删除 inline graph data。
- [x] 2.10 实现显式 Extract Shared，并清理 inline 真数据。
- [x] 2.11 实现 shared 引用切换，禁止 inline/shared 双真相。

## 3. TreeClip 阶段合同

- [x] 3.1 定义 TreeClip `Decision` 和 `Commit` 执行阶段。
- [x] 3.2 将新建 TreeClip 的正式默认阶段配置为 Commit 并在 UI 显示。
- [x] 3.3 定义 Decision 节点能力合同。
- [x] 3.4 定义 Commit 节点能力合同。
- [x] 3.5 禁止 Decision Tree 返回跨 Tick Running 状态。
- [x] 3.6 禁止 Decision Tree 提交 Action、Motion、Cue、Camera、Result 或场景副作用。
- [x] 3.7 定义 Decision Tree 每 Tick reset 和单次求值语义。
- [x] 3.8 定义 Commit Tree Enter、Update、Exit 和 Destroy 语义。

## 4. Tree runtime 上下文

- [x] 4.1 定义 TimelineTreeClipRuntimeContext。
- [x] 4.2 在 context 中保存 clip、timeline time、clip time、cycle 和 playback identity。
- [x] 4.3 在 context 中保存 animation owner 和可选 Action Context。
- [x] 4.4 将 TimelineRunningTree 的 Clip 绑定从 BaseGraph.User 中拆出。
- [x] 4.5 让 TimelineRunningTree 的 Graph User 接收正式 CharacterGraphContext。
- [x] 4.6 更新 TimelineTimeNode 从独立 Clip context 读取时间。
- [x] 4.7 清理 `InitTree(TreeClip)` 旧调用和 TreeClip context fallback。
- [x] 4.8 保证 runtime dispose 后清空 Clip context 和 Graph User。

## 5. Scheduler Tree runtime owner

- [x] 5.1 在 ActiveTimeline 中建立 Tree runtime 集合。
- [x] 5.2 为 playback、track、clip 和 cycle 生成稳定 Tree runtime identity。
- [x] 5.3 从 runtime Timeline clone 解析 TreeTrack 和 TreeClip。
- [x] 5.4 在 playback 启动时初始化所需 Tree runtime template。
- [x] 5.5 在 playback 完成后释放全部 Tree runtime。
- [x] 5.6 在 Timeline cancel 时传播正式 stop cause。
- [x] 5.7 在 pipeline deactivate 时 ForceStop 全部 Tree runtime。
- [x] 5.8 在 scheduler dispose 时释放全部 Tree runtime。
- [x] 5.9 禁止 TreeTrack 接入调用旧 Timeline.Bind/Evaluate/Unbind。

## 6. Decision 阶段执行

- [x] 6.1 在 PrepareDecisionFacts 计算 Decision TreeClip 目标时间范围。
- [x] 6.2 处理 Once Timeline 中 Decision clip 的进入、保持和离开。
- [x] 6.3 处理 Loop Timeline 尾段与头段的 Decision clip 求值。
- [x] 6.4 防止同一 playback/clip/cycle 在同 Tick重复执行 Decision。
- [x] 6.5 在 Decision 执行前重置节点状态。
- [x] 6.6 将 Decision Graph delta time 设置为 logic fixed delta。
- [x] 6.7 让 Decision 写入在 RootTree tick 前可见。
- [x] 6.8 保证 source Timeline 同 Tick被取消后 OnExit 仍能读取本 Tick Decision 值。
- [x] 6.9 保证 Decision 输出不进入 Commit 二次提交。

## 7. Commit 阶段执行

- [x] 7.1 在 Commit 中创建进入范围的 Commit Tree runtime。
- [x] 7.2 每个 retained playback 每 Tick只推进一次 Commit Tree。
- [x] 7.3 自然离开 clip 范围时执行 graceful exit。
- [x] 7.4 Timeline cancel 时停止 Commit Tree 且不提交取消后的输出。
- [x] 7.5 Timeline complete 时完成 Commit Tree stop 和 Destroy。
- [x] 7.6 处理单 Tick跨过完整 Commit clip 的 enter/exit 顺序。
- [x] 7.7 处理 loop boundary 上 Commit clip 的 stop/restart identity。
- [x] 7.8 保证 PresentationFrame 不 Tick TreeClip。
- [x] 7.9 让自然离开范围的 Commit Tree 进入 stopping runtime 集合。
- [x] 7.10 让 Once Timeline 等待自然 stopping runtime 完成后再写回 Succeeded。
- [x] 7.11 让 State exit、Tree abort、reset 和 deactivate 对 Commit Tree 使用对应 cause 的 ForceStop。
- [x] 7.12 对永不完成的自然 stop 报告配置问题，不注入超时成功 fallback。

## 8. Pipeline Blackboard 接入

- [x] 8.1 让 inline/shared TimelineRunningTree 注册 ExposedProperty blackboard declaration。
- [x] 8.2 校验 Decision 输出变量必须是 Frame scope。
- [x] 8.3 校验 Decision 输出变量必须是 Frame lifetime。
- [x] 8.4 校验同 key 的跨 Graph declaration 类型一致。
- [x] 8.5 校验同 key 的 scope、lifetime、authority 和 sync policy 一致。
- [x] 8.6 让 ExposedProperty Set 通过 IPipelineBlackboardRuntimeAccess 写 runtime value。
- [x] 8.7 让 ConditionRuleGraph 继续通过纯 PipelineBlackboard ValueNode 读取结果。
- [x] 8.8 禁止 Decision Blackboard 写入自动产生 SyncFact。
- [x] 8.9 在 Runtime Debug 中显示 Decision TreeClip 写入来源。

## 9. Timeline Editor 与校验

- [x] 9.1 在 TreeClip Inspector 显示 Decision/Commit 分段控件。
- [x] 9.2 在 TreeClip Inspector 显示 Inline/Shared/Missing ownership。
- [x] 9.3 为 inline TreeClip 提供 Open 和双击下钻。
- [x] 9.4 下钻时保持 Timeline authoring context 和返回栈。
- [x] 9.5 为 TreeClip 实现 Extract Shared 入口。
- [x] 9.6 在 Timeline clip 标题显示阶段和 Tree 名称。
- [x] 9.7 在 Inspector 显示 Decision Blackboard 输出摘要。
- [x] 9.8 Validator 报告 Decision Graph 非纯节点。
- [x] 9.9 Validator 报告 Decision Graph Running 节点。
- [x] 9.10 Validator 报告 Blackboard declaration 缺失或冲突。
- [x] 9.11 Validator 报告 inline/shared 双真相。
- [x] 9.12 缺少正式 preview context 时显示不可执行状态，不创建 fallback。
- [x] 9.13 提供 Timeline Editor 与资产迁移共用的 TreeClip authoring service。
- [x] 9.14 让 Corin 迁移通过正式 TreeClip authoring service 创建 inline Graph。

## 10. Corin Dodge 原子迁移

- [x] 10.1 在 Corin RootTree 声明 `CanDodgeMoveCancel` Bool 变量。
- [x] 10.2 配置变量为 Frame scope 和 Frame lifetime。
- [x] 10.3 配置变量 authority 和 sync policy 为本地决策语义。
- [x] 10.4 在 DodgeForward Timeline 创建 inline Decision TreeClip。
- [x] 10.5 将 DodgeForward Decision clip 范围配置为原恢复段。
- [x] 10.6 在 DodgeForward Decision graph 写入 `CanDodgeMoveCancel=true`。
- [x] 10.7 在 DodgeBack Timeline 创建等价 inline Decision TreeClip。
- [x] 10.8 将 DodgeBack Decision clip 范围配置为原恢复段。
- [x] 10.9 在 DodgeBack Decision graph 写入同一 Blackboard key。
- [x] 10.10 将两个 Dodge 完成边条件迁移为 Completed OR BlackboardAndMove。
- [x] 10.11 将两个 Dodge OnExit Cancel 分支迁移为 BlackboardAndMove。
- [x] 10.12 保持 Dodge Complete、Abort 和 IsDodging 回收语义不变。
- [x] 10.13 删除两个 Dodge Timeline 的 `Cancel/DodgeMoveToRun` ActionWindow clip。
- [x] 10.14 删除 Dodge ActionProfile 的 Cancel window policy。
- [x] 10.15 删除 RootTree 中 DodgeMoveToRun ActionWindow reader。
- [x] 10.16 删除旧 window digest、WindowId 和对应 authoring 摘要。
- [x] 10.17 保持 Dodge IFrame ActionWindow 不变。
- [x] 10.18 保持 Attack1/Attack2 连招 CancelWindow 不变。

## 11. 清理与校验

- [x] 11.1 搜索并删除 TreeClip 旧直接 Evaluate runtime 入口。
- [x] 11.2 搜索并删除旧 TreeInstance authoring/runtime 字段引用。
- [x] 11.3 搜索并确认没有一次性 TimelineRunningTree 业务 asset。
- [x] 11.4 搜索并确认 DodgeMoveToRun 不再作为 ActionWindow 或 policy 存在。
- [x] 11.5 运行 Agent snapshot/export 并更新 TreeClip 与 Blackboard 摘要。
- [x] 11.6 运行 Agent graph validator 并清理全部 error。
- [x] 11.7 触发 Unity 正常脚本与资产导入并清理新增 console error。
- [x] 11.8 使用 `--disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译相关项目。
- [x] 11.9 立即运行 `dotnet build-server shutdown`。
- [x] 11.10 对照 proposal、design 和 spec delta 确认任务状态真实。
- [x] 11.11 将全部已完成任务更新为 `[x]`。
- [x] 11.12 运行 `openspec validate restore-timeline-treeclip-pipeline-runtime --strict --no-interactive`。
