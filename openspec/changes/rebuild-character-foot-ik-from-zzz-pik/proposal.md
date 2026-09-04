## Why

现有脚部 IK 只吸收了 ZZZ 的部分字段与响应公式，同时保留了 3C 自己的接触五态、世界残差、预测路径和骨盆弹簧；这些组合不能作为 ZZZ 行为复刻的完成结果。按用户 2026-09-04 的要求，本 change 改为完整复刻 AMLegIK 的输入生产、普通／预测地面处理、脚目标、骨盆目标与控制参数语义，原始算法由同构建指令、真实元数据和已有采样共同约束。

## What Changes

- **BREAKING** 用真实 `AnimatorZoneFootPrint → OnFootPlant → PredictState` 规则接管每脚区域输入、滑动标记、下一边界、`remainTime`、预测脚点与事件生命周期；删除用 `Contact > 0`、clip 剩余时长或原 Landing 完成条件替代原输入的解释。
- **BREAKING** 完整迁移 `Prepare / ApplyPlayerMotion / IsInPIKState / PreprocessPredictionIK / PredictFoot / PreprocessAnimPos / CrossCheck / LockFoot` 的计算与状态责任。默认参数、当前状态参数、实例运动量、每脚状态和一次性命令分开保存。
- **BREAKING** 完整迁移 `OrdinaryIkHitGround / HitGround / HitGroundSimpleImpl / HitGroundImpl / PredictIkHitGround`。原来的 KCC 未来平移、5 厘米／1 度查询门、最近 Surface 选择、Capsule Ground Path 和 Convex Hull 不再作为 Foot 的目标算法。
- **BREAKING** 按真名使用 `currentFootprint / nextFootprint / currentFootnormal / nextFootnormal` 及两份高度历史。纠正旧文档“空中换代、抬脚缩放”的极性：已核对的重写入口位于 `!isMoving` 分支，进入区域时距离阈值乘 3；删除 3C 跨接触 `PlantWorldResidual` 与五态响应主线。
- 将 `pIkWeight` 明确定义为普通／预测支撑结果的混合权重；它与动画 Pose 过渡、每脚 `enablePIK`、全局 `IKWeight`、`pelvisIkWeight` 分别建模，不增加第二次动画 Pose 混合。
- **BREAKING** 完整迁移 `DoCalculateTarget / CalculateFootTarget / SetFootControlParam / FinalizeFootIk`：包括 Owner 坐标、两种方向限制、实例升降选速率、原高度区间、历史回归、位置／旋转及附加标量。不得以当前 3C 几何或限速代替尚未翻译的原函数。
- **BREAKING** 完整迁移两种原生骨盆模式、候选选择、上下响应、PD、一次性 `disableDamping` 和骨盆权重。替换 3C Primary Support 驱动的骨盆目标与弹簧；不恢复已否决的 3C Reach 硬夹紧和末端夹脚。
- 保留项目唯一 Pose Graph、Constraint 根 Bank、Goal Assembler、Solver operation 和 Final Publication 边界。原代码的脚控制参数写入在此阶段只形成 Pending 目标；原生 `InScale` 的消费者语义须核对后接入，不能直接冒充 FBBIK 权重。
- Corin 使用已绑定可琳 Rig 的实例参数作为明确的复刻基准，完整实现原有开关分支，同时保留基准快照中预测／脚锁关闭的值。TrainingEnemy 资产和验收仍不在范围内；共享模块不为它保留旧算法。
- 对原函数覆盖、输入区段、查询结果、目标、历史、控制参数和实际骨骼分别对账。大函数的离线翻译、Walk／Run 区段恢复和原生求解配置缺页均进入必做任务，不用“typed 适配”免除复刻要求。

## Capabilities

### New Capabilities

无。所有新增合同属于现有 Foot Placement 能力；不创建独立 PIK 框架。

### Modified Capabilities

- `character-foot-placement-presentation`：以完整 AMLegIK 行为替换现有 Foot 预测、接触、历史、地面目标与骨盆算法；明确区域输入、普通／预测混合、完整配置、原始输出语义、证据覆盖和一次根事务发布。

## Impact

- 运行时集中修改 `Presentation/FootPlacement`；Pose Graph／Source 只补正式的区域、状态标签、过渡与时钟输入，Query adapter 只执行原算法要求的查询，Constraint／Goal／Solver／Final Publication 继续各自只有一个 Owner。
- Corin 的 Foot Motion 作者输入、Profile、Rig Calibration、Projection 与 Float32／Fixed Program 需要成套迁移并显式重建。完整区段缺失时不得从旧自动分析曲线补齐并宣称原版复刻。
- 最终比较包含 AMLegIK 控制目标和项目实际骨骼。已确认 ZZZ 当前可琳使用原生 `NodeTaskFBIK`，不能仅凭项目使用 FinalIK 就宣称最终姿态逐骨等价；差异必须定位到目标、控制参数编码、求解配置或实际 solver。
- 本 change 接管 `stabilize-character-foot-path-and-landing` 的冲突行为任务。保留／替换／撤销清单写在 design，实施时同步清理旧 change 和 current specs，不并行实施相反要求。
- 本次只更新现有四份规划文档。遵循 `openspec-update-change` 的文件范围，尚未改写的 project／旧 change 冲突逐项登记；不修改运行代码、不运行 Unity、不新增测试。
