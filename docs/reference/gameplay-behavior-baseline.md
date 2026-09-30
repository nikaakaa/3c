# Gameplay 行为基准

本页从 2026-07-23 基准中保留玩家可观察结果，剥离已退役实现名称。原提交为 `ccc103305027d5555db2f5ab64d3bb5f87d6b217`；完整旧实现和故障记录见[历史业务基准](../archive/records/architecture/character-pipeline-runtime-behavior-baseline-20260723.md)。本次整理未重新执行这些场景。

## 输入与移动

普通方向输入表达 Walk，起步、持续行走、停止和 Idle 连续衔接。Walk/Run 由正式 Gameplay 运动模式表达，不用表现速度阈值重新决定。Run 的既有业务含义包括有方向前闪避后保留冲势，不能因为重构改变成新的 Sprint 操作。

## 闪避与恢复

有方向的前闪避沿正式输入方向移动；无方向后闪避保持既有后撤规则。前闪避完成后，持续方向输入保留 Run 意图；终止状态按原业务消费该意图。闪避、恢复与动作接续由正式请求、准入和生命周期处理，Pose 消费已提交事实。

## TurnBack

转身的触发与分流保持正式 Control 规则。TurnBack 位移与 yaw 使用同一源时间，60Hz 下完整执行 28 ticks；表现不裁剪动作或写回 Body。RunLoop 淡入时匹配一次有限动作相位入口，此后自然推进；RunLoop 淡出到 TurnBack 保持连续时间。

对应合同见[控制运动入口](../../openspec/specs/character-control-motion-entry/spec.md)与[动画层运行](../../openspec/specs/character-animation-layer-runtime/spec.md)。

## 动作与动画

攻击、闪避及快速重入继续使用唯一动作请求、准入、Timeline、Action Slot 与表现混合。动画选择由真实 Gameplay/Action 事实驱动；新事实不能因旧混合尚未完成而丢失。有限动作保留自己的播放、完成和释放身份，连续来源保持合法 continuation。

Foot Placement、Goal Assembly、FullBodyIK 与最终姿势写入各有唯一 owner，脚部支撑和身体连续性从正式同帧输入建立。旧文档的“只验证 Landing”、Native Pose Program 和旧发布路径不再描述当前能力。

对应合同见[动作准入](../../openspec/specs/character-action-activation-flow/spec.md)、[动画管线](../../openspec/specs/character-animation-pipeline/spec.md)、[Foot Placement](../../openspec/specs/character-foot-placement-presentation/spec.md)与[惯性化](../../openspec/specs/character-pose-inertialization/spec.md)。

## 性能与证据

正常连续运行遵循 0 GC 原则；源码结构、初始化分配和正式 Player 采集分别评估。旧单次帧率、编辑器计数和局部函数零分配不代表当前整角色达到性能目标。比较必须使用工具声明的相同条件与有效身份，见[性能采集口径](../../Tools/ThirdPersonPerformanceCapture/README.md)。
