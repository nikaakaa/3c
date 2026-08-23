# Tasks

## 1. 固定现有Body与楼梯边界

- [ ] 1.1 枚举Corin Direct、Rollback与Observed三个Body Presentation Profile及其Host引用。
- [ ] 1.2 枚举`CharacterBodyPresentationSettings`全部构造点。
- [ ] 1.3 枚举`CharacterVisualTrajectoryFollower`全部创建、Reset、Retarget、Evaluate与Clear调用点。
- [ ] 1.4 枚举`CharacterBodyTargetFrame`构造与采样路径。
- [ ] 1.5 枚举`CharacterBodyPresentationFrame`全部构造点与消费者。
- [ ] 1.6 枚举Predictive Foot Placement对正式Body Frame与`VisibleTranslationDelta`的读取点。
- [ ] 1.7 枚举默认Camera follow point对Body visible pose的读取点。
- [ ] 1.8 枚举Gameplay Lab现有Ramp楼梯、真实踏面和`StepCapabilityCourse`作者层级。
- [ ] 1.9 枚举共享环境全部`Ground` Deterministic Surface owner及稳定Surface identity。
- [ ] 1.10 确认普通`Ground` Collider进入Collision Baker不依赖`StairTraversalSurfaceAuthoring`。
- [ ] 1.11 确认Fixed KCC Step Motor不读取Unity Layer、楼梯组件或Presentation状态。
- [ ] 1.12 固定`LowStairs_Rise0.14_Run0.45`的单级rise、run、宽度与总高度作为离散对照输入。

## 2. 建立接地竖直响应配置

- [ ] 2.1 新增有限`CharacterGroundedVerticalResponseMode`。
- [ ] 2.2 定义`Direct`值。
- [ ] 2.3 定义`BoundedDiscontinuityCorrection`值。
- [ ] 2.4 在`CharacterBodyPresentationProfile`增加Response Mode字段。
- [ ] 2.5 增加Discontinuity Threshold字段。
- [ ] 2.6 增加Vertical Half-life字段。
- [ ] 2.7 增加Maximum Vertical Error字段。
- [ ] 2.8 增加Vertical Settle Distance字段。
- [ ] 2.9 将全部新字段传入`CharacterBodyPresentationSettings`。
- [ ] 2.10 为settings公开只读竖直响应属性。
- [ ] 2.11 拒绝未知或零Response Mode。
- [ ] 2.12 拒绝非有限或非正Threshold。
- [ ] 2.13 拒绝非有限或非正Half-life。
- [ ] 2.14 拒绝非有限或非正Maximum Error。
- [ ] 2.15 拒绝Settle Distance大于Maximum Error。
- [ ] 2.16 保持竖直响应Mode不由Trajectory Mode推断。
- [ ] 2.17 保持竖直响应Mode不由Source Mode或Camera capability推断。
- [ ] 2.18 更新Animation Preview中全部Body settings构造点。
- [ ] 2.19 将Corin Direct Body Profile显式迁移到有界竖直响应。
- [ ] 2.20 将Corin Rollback Body Profile显式迁移到有界竖直响应。
- [ ] 2.21 将Corin Observed Body Profile显式迁移到有界竖直响应。
- [ ] 2.22 为三个Profile显式保存0.05m突变阈值。
- [ ] 2.23 为三个Profile显式保存0.30m最大竖直误差。
- [ ] 2.24 为三个Profile显式保存正式Half-life与Settle Distance。

## 3. 实现独立接地竖直Follower

- [ ] 3.1 新建`CharacterGroundedVerticalTrajectoryFollower`。
- [ ] 3.2 定义有限`CharacterGroundedVerticalDiscontinuityKind`。
- [ ] 3.3 定义`None`、`Up`与`Down`值。
- [ ] 3.4 定义Follower输入结构。
- [ ] 3.5 让输入包含previous/current Tick identity。
- [ ] 3.6 让输入包含previous/current Body Y。
- [ ] 3.7 让输入包含GroundedBefore/After。
- [ ] 3.8 让输入包含最终基础visible Y与vertical velocity。
- [ ] 3.9 定义Follower输出结构。
- [ ] 3.10 让输出包含visible Y与vertical velocity。
- [ ] 3.11 让输出包含Kind、offset与offset velocity。
- [ ] 3.12 让输出包含active、clamped与settled。
- [ ] 3.13 只在两端Grounded时允许分类。
- [ ] 3.14 只在绝对Source Y差达到Threshold时分类。
- [ ] 3.15 以Source Y差符号生成Up或Down。
- [ ] 3.16 按previous/current Tick锁定当前interval identity。
- [ ] 3.17 保证同一interval只建立一次竖直修正。
- [ ] 3.18 在不连续interval使用current Body endpoint Y作为竖直target。
- [ ] 3.19 从当前最终visible Y建立新offset。
- [ ] 3.20 从当前最终visible vertical velocity建立有限接管速度。
- [ ] 3.21 使用临界阻尼按Half-life衰减offset。
- [ ] 3.22 按Maximum Error夹紧offset。
- [ ] 3.23 移除夹紧边界上继续向外的offset velocity。
- [ ] 3.24 在Settle Distance内归零offset与velocity。
- [ ] 3.25 在连续台阶到来时从当前visible状态重新定向同一状态。
- [ ] 3.26 保持普通Grounded连续interval直接使用基础target并只回收已有残差。
- [ ] 3.27 保持Airborne输入清除接地竖直修正。
- [ ] 3.28 保持Direct Mode不建立竖直修正。
- [ ] 3.29 拒绝非有限输入、offset、velocity或输出。
- [ ] 3.30 提供Reset与Clear生命周期。
- [ ] 3.31 保持Follower不引用KCC、Collision Artifact、Unity Collider、Foot Placement或Camera类型。

## 4. 接入Body Presentation唯一链路

- [ ] 4.1 让`CharacterBodyPresentationRuntime`唯一拥有竖直Follower。
- [ ] 4.2 扩展`CharacterBodyTargetFrame`保留previous Body position。
- [ ] 4.3 扩展`CharacterBodyTargetFrame`保留current Body position。
- [ ] 4.4 保持现有horizontal/yaw target采样不变。
- [ ] 4.5 在基础`CharacterVisualTrajectoryFollower`之后执行竖直Follower。
- [ ] 4.6 将竖直Follower结果合成最终`CharacterVisualTrajectoryResult`。
- [ ] 4.7 保持rotation、horizontal position和yaw velocity来自既有Follower。
- [ ] 4.8 让最终visible velocity Y来自连续target与竖直correction速度合成。
- [ ] 4.9 保持普通Ramp和小Y interval不启动竖直修正。
- [ ] 4.10 保持CommittedStream和SelectedStream复用同一竖直Follower。
- [ ] 4.11 在Initialization锚定竖直Follower。
- [ ] 4.12 在SelectedStream Reset清除并重新锚定竖直Follower。
- [ ] 4.13 在Committed Branch Replacement清除旧台阶offset。
- [ ] 4.14 保持branch replacement不被分类为台阶。
- [ ] 4.15 在Body Runtime Reset清除竖直Follower。
- [ ] 4.16 在Dispose清除竖直Follower。
- [ ] 4.17 保持竖直Follower不推进Animation time、Pose Plan或Timeline。
- [ ] 4.18 保持竖直结果不写回Body history、World state或Simulation output。

## 5. 扩展Body Frame与诊断

- [ ] 5.1 在`CharacterBodyPresentationFrame`增加竖直不连续Kind。
- [ ] 5.2 增加竖直correction offset。
- [ ] 5.3 增加竖直correction velocity。
- [ ] 5.4 增加竖直correction active。
- [ ] 5.5 增加竖直correction clamped。
- [ ] 5.6 增加竖直correction settled。
- [ ] 5.7 更新全部Body Frame构造点。
- [ ] 5.8 更新Animation Preview Body Frame适配器。
- [ ] 5.9 在Body Presentation Trace显示竖直Kind。
- [ ] 5.10 在Body Presentation Trace显示Source Y差。
- [ ] 5.11 在Body Presentation Trace显示竖直offset与velocity。
- [ ] 5.12 在Body Presentation Trace显示active、clamped与settled。
- [ ] 5.13 扩展Runtime diagnostics snapshot暴露竖直correction量。
- [ ] 5.14 保持诊断字段只读且不成为任何运行输入。

## 6. 保持Camera单路消费

- [ ] 6.1 保持默认Camera使用最终Body Frame visible position。
- [ ] 6.2 保持Camera不读取logic Body Y或KCC Step diagnostics。
- [ ] 6.3 保持Camera不创建第二份台阶竖直Follower。

## 7. 作者连续离散楼梯内容

- [ ] 7.1 在共享环境Prefab增加`DiscreteStairs_Rise0.14_Run0.45`稳定根对象。
- [ ] 7.2 按LowStairs固定单级rise建立持久化可见踏面。
- [ ] 7.3 按LowStairs固定单级run建立持久化可见踏面。
- [ ] 7.4 保持离散楼梯可行走宽度与LowStairs一致。
- [ ] 7.5 建立持久化阶梯形Collider代理。
- [ ] 7.6 让代理上表面与可见踏面逐级一致。
- [ ] 7.7 将全部离散阶梯Collider设置为`Ground`。
- [ ] 7.8 保持全部离散阶梯Collider启用且非Trigger。
- [ ] 7.9 为离散楼梯建立唯一明确的Deterministic Surface作者根。
- [ ] 7.10 为离散楼梯配置稳定Surface identity。
- [ ] 7.11 保证每个离散阶梯Collider恰好一个Deterministic owner。
- [ ] 7.12 保证离散楼梯不挂`StairTraversalSurfaceAuthoring`。
- [ ] 7.13 保证离散楼梯不存在`CharacterTraversal` Ramp。
- [ ] 7.14 保证离散楼梯不存在`FootPlacementSurface`重复Collider。
- [ ] 7.15 建立普通`Ground`入口平台。
- [ ] 7.16 建立普通`Ground`顶平台。
- [ ] 7.17 收敛首级Collider与入口平台边界。
- [ ] 7.18 收敛末级Collider与顶平台边界。
- [ ] 7.19 保持现有Low、High与OverLimit Ramp楼梯不变。
- [ ] 7.20 保持现有`StepCapabilityCourse`内容与所有权不变。
- [ ] 7.21 保持`GameplayLabAssetBuilder`不生成或修改离散楼梯。

## 8. 更新Collision Artifact

- [ ] 8.1 保持Collision Artifact schema不因新增普通Ground几何升级。
- [ ] 8.2 保持Collision Baker不增加离散楼梯特判。
- [ ] 8.3 保持Ramp楼梯validator只枚举显式注册绑定。
- [ ] 8.4 通过现有显式Unity菜单执行唯一Collision Bake。
- [ ] 8.5 让Artifact包含全部离散楼梯Ground primitives。
- [ ] 8.6 让Artifact继续包含现有六条Traversal Ramp。
- [ ] 8.7 让Artifact继续排除Ramp楼梯真实踏面Collider。
- [ ] 8.8 让Artifact继续包含Step Capability Course三个障碍。
- [ ] 8.9 更新CollisionWorldHash与资产canonical bytes。
- [ ] 8.10 保持Local Fixed与Rollback Variant引用同一Collision Asset。
- [ ] 8.11 不创建旧Artifact镜像或兼容reader。
- [ ] 8.12 不自动重新发布Network Product。

## 9. 收口文档与旧口径

- [ ] 9.1 更新current `character-stair-surface-authoring` spec。
- [ ] 9.2 更新current `deterministic-kcc-world-solver` spec中的Ramp绑定范围。
- [ ] 9.3 更新current `character-presentation-interpolation` spec。
- [ ] 9.5 更新current `character-camera-pipeline` spec。
- [ ] 9.6 更新`openspec/project.md`两种楼梯当前口径。
- [ ] 9.7 更新KCC implementation inventory中的离散楼梯内容与Artifact身份。
- [ ] 9.8 删除“全部连续楼梯必须注册Ramp作者组件”的宽泛口径。
- [ ] 9.9 保留现有Ramp楼梯双表面作者规则。
- [ ] 9.10 保留Step Capability Course独立能力边界口径。
- [ ] 9.11 搜索确认没有新增KCC、WorldSolveResult或Snapshot字段。
- [ ] 9.12 搜索确认没有新增Unity楼梯Layer。
- [ ] 9.13 搜索确认没有新增Runtime Ramp/Step切换或fallback。
- [ ] 9.14 搜索确认没有Inspector、OnValidate、导入或Play入口自动Bake。
