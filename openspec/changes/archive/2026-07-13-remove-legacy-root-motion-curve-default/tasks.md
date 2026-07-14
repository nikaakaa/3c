## 1. 基线与边界确认

- [x] 1.1 盘点 `RootMotionCurveEvaluationMode` 的定义、字段初始化、Baker 转换和求值分支
- [x] 1.2 记录五个 `Corin/Pipeline/Motion/Curves` 资产当前的零值模式与累计曲线语义
- [x] 1.3 记录两个 `BakedAnimation` TurnBack 资产缺少模式字段的状态
- [x] 1.4 扫描七个资产的非 `.meta` 序列化引用并确认删除候选仍无消费者
- [x] 1.5 确认 `RootMotionCurveEvaluator` 没有生产运行时调用者
- [x] 1.6 确认 Corin RootTree 的正式位移来自内联 `MotionCurveClip`
- [x] 1.7 确认 `MotionCurveTrack` 不读取或引用 `RootMotionCurveAsset`

## 2. 现有资产收口

- [x] 2.1 将 `CorinAttack1RootMotionCurve` 显式迁移为 `FullLocalDelta`
- [x] 2.2 将 `CorinAttack2RootMotionCurve` 显式迁移为 `FullLocalDelta`
- [x] 2.3 将 `CorinDodgeBackRootMotionCurve` 显式迁移为 `FullLocalDelta`
- [x] 2.4 将 `CorinDodgeForwardRootMotionCurve` 显式迁移为 `FullLocalDelta`
- [x] 2.5 将 `CorinMovingTurnRootMotionCurve` 显式迁移为 `FullLocalDelta`
- [x] 2.6 删除无引用的 `Corin_TurnBack_WithWeaponRootmotion_RootMotionCurve` 资产及 meta
- [x] 2.7 删除无引用的 `Corin_TurnBack_WithWeaponRootmotion_ZeroZ_RootMotionCurve` 资产、meta 与空的 `BakedAnimation` 目录
- [x] 2.8 扫描剩余 `RootMotionCurveAsset`，确认每个资产都保存显式有效模式

## 3. 资产模式合同

- [x] 3.1 将枚举零值定义为 `Unspecified`
- [x] 3.2 为 `FullLocalDelta` 与 `ForwardDistanceYaw` 分配稳定的显式非零序列化值
- [x] 3.3 在 `RootMotionCurveAsset` 中提供复用的有效模式校验入口
- [x] 3.4 让资产校验能返回未指定或未知模式的明确原因
- [x] 3.5 让 `SetBakedData` 拒绝未指定模式
- [x] 3.6 让 `SetBakedData` 拒绝未知模式
- [x] 3.7 删除资产字段的 `FullLocalDelta` 隐式初始化

## 4. Baker 作者入口

- [x] 4.1 将 Baker 的模式初始值改为未指定
- [x] 4.2 让 Baker 在未选择有效模式时不可执行烘焙
- [x] 4.3 让 Baker 对未指定模式显示明确配置错误
- [x] 4.4 让 Baker 对未知模式显示明确配置错误
- [x] 4.5 将 Baker 的模式转换改为穷尽有效分支
- [x] 4.6 删除 Baker 中默认返回 `FullLocalDelta` 的转换路径
- [x] 4.7 确认 Baker 只向资产写入有效显式模式

## 5. 求值硬切

- [x] 5.1 让 `RootMotionCurveEvaluator` 在未指定模式下拒绝 sample 求值
- [x] 5.2 让 `RootMotionCurveEvaluator` 在未知模式下拒绝 sample 求值
- [x] 5.3 用显式模式分支计算 `FullLocalDelta` sample
- [x] 5.4 用显式模式分支计算 `ForwardDistanceYaw` sample
- [x] 5.5 删除“非 `ForwardDistanceYaw` 即 `FullLocalDelta`”分支
- [x] 5.6 确认无效模式不会产生 delta 或 motion contribution

## 6. 规格收口

- [x] 6.1 删除 current spec 的旧零值兼容 requirement
- [x] 6.2 写入 Root Motion 曲线显式模式合同
- [x] 6.3 写入 RootMotionCurveAsset 与 Timeline 内联 MotionCurveClip 的边界合同
- [x] 6.4 搜索并删除代码与规格中的旧默认模式表述
- [x] 6.5 确认不新增 legacy reader、自动升级、按名称查找、自动同步或运行时导入
- [x] 6.6 运行 `openspec validate remove-legacy-root-motion-curve-default --strict --no-interactive`
