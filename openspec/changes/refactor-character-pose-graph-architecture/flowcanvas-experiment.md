# PoseGraph FlowCanvas路线决策记录

2026-09-08最终决定：只复用FlowCanvas作者UI，保留现有Pose Compiler、Program Image、Native数据、Burst／Job及帧输出链。Unity进入Play后观察实际角色，不创建独立预览场景或运行实例。

正式作者与观察规划见[独立提案](../integrate-pose-flowcanvas-editor-preview/proposal.md)及其[任务清单](../integrate-pose-flowcanvas-editor-preview/tasks.md)。技能作者迁移由[技能提案](../integrate-native-fsm-skill-authoring/proposal.md)管理，通用观察归[观察收尾](../finish-skill-runtime-observation/proposal.md)；技能预览同样只是普通运行结果显示。

## 已撤回的路线

- 曾提出Clip A／B、Weight、Blend、Output的原生FlowCanvas runtime独立实验。
- 随后讨论过完整替换Pose运行时。
- 核对已有Native／Job、阶段与帧事务后，用户决定保留编译运行。上述实验与替换未完成，不再作为实施目标；旧23.x／24.x清单已移除，未将其勾选为完成。

## 已有工作不等于运行替换

- CanvasCore接入、节点拖动修正和共享作者合同分离已有代码；它们不证明FlowCanvas runtime已接入。
- Clip／Blend Space播放器初始化已分离对整份Program Image的不必要依赖，实际播放与采样逻辑保留。
- Document身份重复与内联路径空值伪差异已修复。两个空Clip节点经正式apply保存，返回applied、saved、Clean；按用户选择恢复清理前布局。
- 已执行一次Corin正式Float32 Build并发布Program／Projection；该结果不替代后续源码变更的验证。
- 编辑交互、Play观察和性能仍须按新提案逐项取得证据，不能从文档状态推断完成。

原Canvas接入及早期失败证据见[接入记录](canvas-integration.md)，历史Git提交保留方案演变，不保留第二条生产运行路径。
