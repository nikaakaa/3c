# 参考资料

本目录保存原理、源码研究、原生消费者依据和可复用的失败经验。当前业务配置、执行路径与实施状态由现行规格、内容资产和活跃 change 拥有。

| 主题 | 资料 |
| --- | --- |
| 预测 Foot IK 原理 | [GDC 2016 Fitting the World](foot-placement/gdc2016-fitting-the-world.md) |
| Foot Placement 历史否决与连续性经验 | [实现经验](foot-placement/implementation-lessons.md) |
| UE 示例原文 | [Shadow 实现](foot-placement/predict-foot-ik-ue5-shadow.md)、[实验实现](foot-placement/predict-foot-ik-ue5-experiment.md)、[实现总结](foot-placement/predict-foot-ik-implementation-summary.md) |
| 相机震动信号与消费者 | [原生消费者依据](camera/corin-camera-shake-source-parity-20260928.md) |
| 攻击查询与连续扫掠 | [命中检测源码研究](combat/hit-detection-options-20260929.md) |
| 帧同步机制与采用边界 | [NKGMobaBasedOnET 参考评估](network/frame-sync-reference.md) |
| 玩家可观察的移动、闪避、转身和动作基准 | [Gameplay 行为基准](gameplay-behavior-baseline.md) |

UE 示例保留其原有局限，不能作为 3C 的直接运行方案；历史经验中的撤回方案保留编号和否决原因，不恢复为当前实现要求。
