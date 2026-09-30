# 修复下一步包络重新抬高当前接触脚

> 历史记录：保存当时实现、问题与证据；当前合同见[现行文档索引](../../../../openspec/maintenance-audit.md)，当前进展见[文档入口](../../../README.md)。文中的“当前”“待完成”和旧阻塞均属于该记录时点。

## 依据

c75d 的 E Walk 左脚 4452：当前支撑及插值目标约 0.180935m，末端却用下一步包络补高 0.126026m。3062 同样先被下一步包络顶高，3063 进入 Landing 后快速消解残差。

历史 680848fad 已阻止下一落点越权抬升当前脚，386073b62 已让目标选择和插值区分当前接触与下一落点。36715bdcc 为恢复真正摆腿的包络保护调整分支顺序时，没有让末端保护遵循这个选择结果，重新引入上述回归。

## 修改

CharacterFootLifecycle 将 interpolation.SupportTarget 传给 CharacterFootHardConstraintResolver。末端仅在 selectedTarget.Kind 为 SwingGround 且摆腿路径有效时，使用包络最低高度。目标已经是 CurrentSupport 时，不再因为下一步预测路径仍有效而重新抬脚。

PreparedPlant 保留原来只观察、不执行未来落点硬下界的语义。Landing/Locked 接触约束和实际输出脚尖/脚跟支撑保护保留；真正 SwingGround 仍使用包络，不恢复历史“全部以当前地面替代摆腿包络”的失败实验。

没有新增配置、物理查询或逐帧托管分配。既有测试调用仅补传新增的正式目标参数，没有新增或执行测试；不运行 replay。骨盆、插值参数、最终求解器及动画资产未改。

## 交付边界

本次只针对当前接触与下一步包络混用的回归。实际脚尖跨级后才补高、最终骨骼偏离检查位置穿透、停止后的蹲姿恢复仍是独立问题，不以此次编译通过宣称全部修复。

提交 f37f8b4df。Editor 构建0错误91警告，日志 tmp/foot-floor-owner-build.log，构建服务器已shutdown。主实例e852139597e42532域重载后自动恢复连接；确认Edit、不编译、不刷新，Resolve已加载selectedTarget参数，Console错误0条。无运行回放，交用户手测。
