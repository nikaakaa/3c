# Change: 增加脚步地面路径检测

## Why

当前Foot Placement已经能稳定得到每只脚的Accepted Landing，但只有终点，无法知道脚从当前完成位置走向该落点时中间经过哪些地面。GDC 2016《Fitting the World》的下一步不是直接做凸包，而是先沿脚步路径执行Capsule Cast，取得位置和法线；排序、Edge、Reachability和Convex Hull都依赖这组原始候选。

旧`replace-pose-ik-with-finalik-full-body-solver`同时包含已删除实现、历史修复和远期目标，继续追加会重新混合旧Predictive、Grounding、Anchor、Pelvis与新Landing链。本change从提交`6a3e1d0`的删除边界、`a59bd0d`的Landing最小闭环和`dc0b941`的真实Body Translation实现重建，只推进下一层地面检测。

## What Changes

- 在唯一`CharacterFootPlacementRuntime`内部增加每脚不可变Ground Path Revision，输入固定为上一完成帧实际Sole、同帧Accepted Landing、Landing Event identity和正式运动权威身份。
- Ground Path Revision变化时重新执行唯一Capsule Ground Detection请求，收集固定容量的位置、法线、Surface identity和查询距离；同一Revision不得逐Render帧重复查询。
- 把查询语义合同与Unity Physics适配器分开。Runtime只认识Ground Path查询请求和结果，不直接认识`RaycastHit`、`Collider`枚举或调试绘制。
- Unity适配器把完整Capsule轴切成连续短段并逐段执行真实Capsule Cast，使同一MeshCollider上的不同地形也能产生候选；这属于唯一请求的固定实现，不是射线替代或fallback。
- Scene诊断绘制完整Capsule包络、原始候选位置和法线；短胶囊首尾连续且并集等于该包络，Gizmo不重复绘制内部接缝、不重建查询、不画矩形鞋底、不把凸包画成查询形状。
- Foot Placement仍发布Pelvis、LeftFoot、RightFoot三个零权重Goal，Ground Path结果只进入成功Seal后的诊断，不改变动画Pose。
- Profile新增唯一正式Ground Detection配置并升级schema；不保留Landing-only旧schema reader、默认值补全或兼容配置。

## Impact

- Affected specs: `character-foot-placement-presentation`
- Affected code: Foot Placement运行时状态与合同、World Query抽象与Unity适配器、Profile/Projection发布、Scene Gizmo和诊断采样。
- 不影响Gameplay State、Network、KCC结果、Pose Graph拓扑或FinalIK求解。
- 不恢复旧Predictive Planner、旧Ground Envelope、第二Grounding、第二Pelvis、Anchor、Foot Lock、Reachability、Hull或Goal交接。

## Current Spec Comparison

- 现行`当前Landing阶段必须保持Pose恒等`明确禁止Capsule Path；本change修改该条，只开放Ground Path Revision与原始Capsule Ground Detection，仍禁止Edge、Reachability、Hull、Foot Motion、Anchor和Pelvis。
- 现行`Foot Placement诊断必须只显示当前事实`只允许Landing图形；本change修改为同时允许成功Seal的真实Ground Path查询与原始候选。
- 现行Landing Prediction、唯一Goal事务、Gameplay/Network隔离和FinalIK零权重边界保持不变。
- `character-animation-foot-analysis-artifact`已经把Kinematics、Step Header和Biomechanical Route分成独立读取合同；本change不扩大Landing对完整Route Page的依赖，也不把大Foot Feature重新按值传递。
