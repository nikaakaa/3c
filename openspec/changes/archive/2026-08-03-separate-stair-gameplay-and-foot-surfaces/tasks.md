# Tasks

## 1. 固定现有楼梯与表面所有权

- [x] 1.1 枚举Gameplay Lab共享环境中的全部连续楼梯根对象及其上行、顶平台、下行结构。
- [x] 1.2 枚举LowStairs全部可见踏面Collider与顶平台Collider。
- [x] 1.3 枚举HighStairs全部可见踏面Collider与顶平台Collider。
- [x] 1.4 枚举连续OverLimitStairs全部可见踏面Collider与顶平台Collider。
- [x] 1.5 枚举普通地面、墙体、坡面与障碍Collider的现有Layer。
- [x] 1.6 枚举全部`DeterministicCollisionSurfaceAuthoring`及其Collider子树所有权。
- [x] 1.7 枚举当前Collision Artifact中三条楼梯路线的SurfaceId与Primitive来源。
- [x] 1.8 枚举Corin Foot Placement Profile当前Ground LayerMask。
- [x] 1.9 枚举Foot Placement Support Query全部LayerMask读取点。
- [x] 1.10 枚举Gameplay Lab Builder对共享环境、World Authoring和Surface Authoring的全部写入点。
- [x] 1.11 固定当前Low、High与OverLimit楼梯上行、下行梯段的入口、出口、宽度、rise、run和总高度。
- [x] 1.12 确认Fixed KCC Step Motor不读取Unity Layer、Foot Surface或Foot Placement状态。

## 2. 建立楼梯作者绑定合同

- [x] 2.1 新建`StairTraversalSurfaceAuthoring`序列化组件。
- [x] 2.2 定义非空稳定Stair identity字段。
- [x] 2.3 定义唯一Traversal Ramp Collider引用。
- [x] 2.4 定义唯一Foot Placement Surface根引用。
- [x] 2.5 定义Lower Transition引用。
- [x] 2.6 定义Upper Transition引用。
- [x] 2.7 提供只读公开属性访问全部作者字段。
- [x] 2.8 拒绝空Stair identity。
- [x] 2.9 拒绝同一场景重复Stair identity。
- [x] 2.10 拒绝缺失Traversal Ramp引用。
- [x] 2.11 拒绝缺失Foot Surface根引用。
- [x] 2.12 拒绝缺失Lower或Upper Transition引用。
- [x] 2.13 拒绝跨Scene或不属于当前Prefab作者上下文的引用。
- [x] 2.14 保持组件不实现Runtime Update、LateUpdate或Collider生成。

## 3. 建立正式Layer角色

- [x] 3.1 在TagManager定义`CharacterTraversal`层。
- [x] 3.2 在TagManager定义`FootPlacementSurface`层。
- [x] 3.3 保持现有`Ground`层作为普通共享地面角色。
- [x] 3.4 定义代码级稳定Layer名称常量。
- [x] 3.5 建立Layer名称到index的Editor严格解析。
- [x] 3.6 缺失`CharacterTraversal`层时阻止楼梯作者校验。
- [x] 3.7 缺失`FootPlacementSurface`层时阻止楼梯作者校验。
- [x] 3.8 拒绝两个正式Layer解析到相同index。
- [x] 3.9 不增加Default Layer或Ground Layer回退。
- [x] 3.10 不在运行时按字符串查找Layer。

## 4. 建立楼梯结构与几何Validator

- [x] 4.1 定义结构化楼梯校验结果。
- [x] 4.2 定义Stair identity重复诊断。
- [x] 4.3 定义Ramp引用缺失诊断。
- [x] 4.4 定义Foot Surface引用缺失诊断。
- [x] 4.5 定义Transition引用缺失诊断。
- [x] 4.6 验证Ramp Collider已启用。
- [x] 4.7 验证Ramp Collider不是Trigger。
- [x] 4.8 验证Ramp GameObject没有Renderer。
- [x] 4.9 验证Ramp位于`CharacterTraversal`层。
- [x] 4.10 验证Ramp被唯一Deterministic Surface作者拥有。
- [x] 4.11 收集Foot Surface根下全部活动Collider。
- [x] 4.12 拒绝Foot Surface根没有合法Collider。
- [x] 4.13 验证全部Foot Surface Collider不是Trigger。
- [x] 4.14 验证全部Foot Surface Collider位于`FootPlacementSurface`层。
- [x] 4.15 验证全部Foot Surface Collider不属于Deterministic Surface作者子树。
- [x] 4.16 拒绝Ramp Collider同时出现在Foot Surface子树。
- [x] 4.17 定义Ramp端点与Transition的固定几何容差。
- [x] 4.18 计算Ramp上表面下端位置与高度误差。
- [x] 4.19 计算Ramp上表面上端位置与高度误差。
- [x] 4.20 验证Ramp宽度覆盖Foot Surface可行走宽度。
- [x] 4.21 验证Ramp前进方向与楼梯上行方向一致。
- [x] 4.22 验证Ramp入口与普通地面不存在空隙或双支持重叠。
- [x] 4.23 验证Ramp出口与顶平台不存在空隙或双支持重叠。
- [x] 4.24 让全部诊断包含Stair identity、对象路径、实测值与限制值。

## 5. 建立显式Ramp作者操作

- [x] 5.1 在楼梯作者Inspector提供显式`Create Traversal Ramp`命令。
- [x] 5.2 在楼梯作者Inspector提供显式`Update Traversal Ramp`命令。
- [x] 5.3 命令只在完整Transition和Foot Surface输入存在时启用。
- [x] 5.4 命令生成持久化BoxCollider而不是临时对象。
- [x] 5.5 命令将Ramp设置到`CharacterTraversal`层。
- [x] 5.6 命令确保Ramp对象没有Renderer。
- [x] 5.7 命令根据Lower/Upper Transition建立唯一坡面方向。
- [x] 5.8 命令根据Foot Surface宽度建立Ramp宽度。
- [x] 5.9 命令保存Undo并标记对应Scene或Prefab dirty。
- [x] 5.10 命令不执行Collision Bake。
- [x] 5.11 `OnInspectorGUI`只显示缓存诊断与显式命令。
- [x] 5.12 `OnValidate`不创建、移动或缩放Ramp。
- [x] 5.13 资源导入和场景打开不自动更新Ramp。
- [x] 5.14 删除任何临时Ramp或运行时生成路径。

## 6. 将楼梯Validator接入Collision Bake

- [x] 6.1 在Bake开始时枚举唯一World Authoring下的楼梯作者绑定。
- [x] 6.2 按Stair identity与稳定层级排序楼梯绑定。
- [x] 6.3 在收集Collider前运行楼梯结构Validator。
- [x] 6.4 任一楼梯非法时阻止完整Artifact生成。
- [x] 6.5 保持既有Artifact在校验失败时不被修改。
- [x] 6.6 验证每个Ramp会被现有Surface Authoring收集。
- [x] 6.7 验证每个Foot Surface不会被现有Surface Authoring收集。
- [x] 6.8 拒绝同一个Ramp被两个Surface Authoring重复拥有。
- [x] 6.9 保持Baker不按Layer补收Collider。
- [x] 6.10 保持Baker不临时禁用Foot Surface Collider。
- [x] 6.11 保持Baker不根据踏面生成替代Ramp。
- [x] 6.12 将楼梯校验错误接入现有结构化Bake失败输出。

## 7. 重构Gameplay Lab Surface作者层级

- [x] 7.1 删除共享环境根上覆盖全部Collider的宽泛Surface所有权。
- [x] 7.2 新建普通Gameplay Ground Surface作者根。
- [x] 7.3 将普通地面Collider迁入Gameplay Ground Surface作者根。
- [x] 7.4 将现有独立坡面Collider迁入明确Gameplay Surface作者根。
- [x] 7.5 将墙体与阻挡Collider迁入明确Gameplay Surface作者根。
- [x] 7.6 为三条连续楼梯路线新建统一Gameplay Ramp Surface作者根，并分别容纳上行、下行梯段。
- [x] 7.7 新建连续楼梯Foot Placement Surface根。
- [x] 7.8 保证每个Gameplay Collider只有一个Surface owner。
- [x] 7.9 保证每个Foot Surface Collider没有Surface owner。
- [x] 7.10 更新Gameplay Lab Builder只装配World Authoring而不恢复宽泛根Surface marker。
- [x] 7.11 删除迁移后的旧Surface marker和重复作者层级。

## 8. 迁移LowStairs连续楼梯

- [x] 8.1 为LowStairs上行、下行梯段分别添加唯一Stair identity。
- [x] 8.2 为LowStairs上行、下行梯段分别配置Lower Transition。
- [x] 8.3 为LowStairs上行、下行梯段分别配置Upper Transition。
- [x] 8.4 为LowStairs建立两条持久化Traversal Ramp。
- [x] 8.5 将LowStairs两条Ramp纳入Gameplay Ramp Surface作者根。
- [x] 8.6 将LowStairs两条Ramp设置为`CharacterTraversal`层。
- [x] 8.7 将LowStairs真实踏面Collider移入Foot Surface根。
- [x] 8.8 将LowStairs真实踏面Collider设置为`FootPlacementSurface`层。
- [x] 8.9 从Deterministic Surface作者子树移除LowStairs真实踏面Collider。
- [x] 8.10 保持LowStairs可见Renderer和踏面尺寸不变。
- [x] 8.11 收敛LowStairs两条Ramp与各自低端地面过渡。
- [x] 8.12 收敛LowStairs两条Ramp与共享顶平台过渡。
- [x] 8.13 删除LowStairs逐级Gameplay Collider所有权。

## 9. 迁移HighStairs连续楼梯

- [x] 9.1 为HighStairs上行、下行梯段分别添加唯一Stair identity。
- [x] 9.2 为HighStairs上行、下行梯段分别配置Lower Transition。
- [x] 9.3 为HighStairs上行、下行梯段分别配置Upper Transition。
- [x] 9.4 为HighStairs建立两条持久化Traversal Ramp。
- [x] 9.5 将HighStairs两条Ramp纳入Gameplay Ramp Surface作者根。
- [x] 9.6 将HighStairs两条Ramp设置为`CharacterTraversal`层。
- [x] 9.7 将HighStairs真实踏面Collider移入Foot Surface根。
- [x] 9.8 将HighStairs真实踏面Collider设置为`FootPlacementSurface`层。
- [x] 9.9 从Deterministic Surface作者子树移除HighStairs真实踏面Collider。
- [x] 9.10 保持HighStairs可见Renderer和踏面尺寸不变。
- [x] 9.11 收敛HighStairs两条Ramp与各自低端地面过渡。
- [x] 9.12 收敛HighStairs两条Ramp与共享顶平台过渡。
- [x] 9.13 删除HighStairs逐级Gameplay Collider所有权。

## 10. 迁移OverLimit连续楼梯

- [x] 10.1 为连续OverLimitStairs上行、下行梯段分别添加唯一Stair identity。
- [x] 10.2 为连续OverLimitStairs上行、下行梯段分别配置Lower Transition。
- [x] 10.3 为连续OverLimitStairs上行、下行梯段分别配置Upper Transition。
- [x] 10.4 为连续OverLimitStairs建立两条持久化Traversal Ramp。
- [x] 10.5 将OverLimit两条Ramp纳入Gameplay Ramp Surface作者根。
- [x] 10.6 将OverLimit两条Ramp设置为`CharacterTraversal`层。
- [x] 10.7 将OverLimit真实踏面Collider移入Foot Surface根。
- [x] 10.8 将OverLimit真实踏面Collider设置为`FootPlacementSurface`层。
- [x] 10.9 从Deterministic Surface作者子树移除OverLimit真实踏面Collider。
- [x] 10.10 保持OverLimit可见Renderer和踏面尺寸不变。
- [x] 10.11 收敛OverLimit两条Ramp的低端地面与共享顶平台过渡。
- [x] 10.12 删除OverLimit连续路线的逐级Gameplay拒绝语义。

## 11. 建立独立Step Capability Course

- [x] 11.1 新建稳定命名的Step Capability Course根。
- [x] 11.2 新建0.14m孤立Gameplay Step Collider。
- [x] 11.3 新建0.24m孤立Gameplay Step Collider。
- [x] 11.4 新建0.40m孤立Gameplay阻挡Collider。
- [x] 11.5 为三项障碍提供互不重叠的接近路线。
- [x] 11.6 将三项障碍纳入唯一Deterministic Surface作者所有权。
- [x] 11.7 保持三项障碍不引用`StairTraversalSurfaceAuthoring`。
- [x] 11.8 保持三项障碍不生成Gameplay Ramp。
- [x] 11.9 使用明确对象名表达Step准入或拒绝业务含义。
- [x] 11.10 删除旧连续楼梯承担Step能力课程的命名和文档口径。

## 12. 迁移Foot Placement查询配置

- [x] 12.1 将Corin Foot Placement Ground LayerMask加入`FootPlacementSurface`。
- [x] 12.2 保持Corin Foot Placement Ground LayerMask包含`Ground`。
- [x] 12.3 从Corin Foot Placement Ground LayerMask排除`CharacterTraversal`。
- [x] 12.4 扩展Profile校验拒绝Character Layer与CharacterTraversal Layer。
- [x] 12.5 保持Support Query只消费Profile正式Mask。
- [x] 12.6 保持heel与toe查询不增加对象名或Tag分支。
- [x] 12.7 保持Future Landing查询不读取Deterministic Collision Artifact。
- [x] 12.8 保持Locked Surface只来自合法Unity Foot Surface命中。
- [x] 12.9 保持Actor Movement Compensation不读取Ramp或KCC Step phase。
- [x] 12.10 更新Foot Placement诊断显示命中Layer与Surface角色。
- [x] 12.11 更新楼梯作者校验确认Profile Mask与Ramp排他。
- [x] 12.12 删除任何同时查询Ramp与踏面后做优先级选择的路径。

## 13. 重新生成唯一Collision Artifact

- [x] 13.1 保持Collision Artifact schema不因纯几何迁移无故升级。
- [x] 13.2 通过现有显式菜单读取迁移后的Gameplay Surface作者数据。
- [x] 13.3 让Artifact包含LowStairs上行与下行Traversal Ramp。
- [x] 13.4 让Artifact包含HighStairs上行与下行Traversal Ramp。
- [x] 13.5 让Artifact包含OverLimitStairs上行与下行Traversal Ramp。
- [x] 13.6 让Artifact排除三条连续楼梯真实踏面Collider。
- [x] 13.7 让Artifact包含0.14m孤立Step障碍。
- [x] 13.8 让Artifact包含0.24m孤立Step障碍。
- [x] 13.9 让Artifact包含0.40m孤立超限障碍。
- [x] 13.10 更新唯一CollisionWorldHash与资产canonical bytes。
- [x] 13.11 保持Local Fixed与Rollback Variant引用同一Collision Asset。
- [x] 13.12 不创建旧Artifact镜像或兼容reader。
- [x] 13.13 不自动重新发布Network Product。

## 14. 收口诊断、文档与旧路径

- [x] 14.1 增加楼梯作者只读结构诊断。
- [x] 14.2 显示Ramp Collider、Layer与Deterministic Surface owner。
- [x] 14.3 显示Foot Surface Collider数量、Layer与无Fixed owner状态。
- [x] 14.4 显示Ramp下端与Lower Transition误差。
- [x] 14.5 显示Ramp上端与Upper Transition误差。
- [x] 14.6 显示Ramp宽度覆盖与前进方向结果。
- [x] 14.7 保持诊断不修改场景、Profile或Artifact。
- [x] 14.8 更新current `character-stair-surface-authoring` spec。
- [x] 14.9 更新current `deterministic-kcc-world-solver` spec。
- [x] 14.10 更新current `character-foot-placement-presentation` spec。
- [x] 14.11 更新`openspec/project.md`楼梯Gameplay与Presentation当前口径。
- [x] 14.12 更新KCC implementation inventory中的Gameplay Lab表面与Artifact身份。
- [x] 14.13 删除“不含隐藏坡道”的旧当前口径。
- [x] 14.14 删除连续楼梯逐级Fixed重放作为正式表现验收的旧口径。
- [x] 14.15 搜索并删除旧楼梯Collider同时进入KCC与Foot Placement的作者路径。
- [x] 14.16 搜索确认不存在Ramp失败后回退真实台阶的配置或Runtime分支。
- [x] 14.17 搜索确认不存在Inspector、OnValidate、导入或Play入口自动Bake。
