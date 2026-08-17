# Change: 增加脚步地面路径与连续包络

## Why

当前链已经能按Landing Event得到Accepted Landing，但Ground Path仍把逐帧Animated Sole当作起点，导致同一脚步周期内重复改变查询输入。参考文章和GDC需要的是上一已提交落点到下一Accepted Landing之间的Ground Detection与Ground Envelope。

本change继续沿唯一Foot Placement事务完成下一层：每个Landing Event只确定一次Accepted Landing，把上一已提交落点与下一Accepted Landing之间的原始Capsule接触变成连续的feet-only地面上侧包络。

## What Changes

- 统一为`上一已提交Accepted Landing -> 下一Landing Event的Accepted Landing -> 分段Capsule Cast -> Raw Ground Contacts`唯一链；同一Landing Event不重复SphereCast。
- 将每段Physics命中缓冲`SegmentHitCapacity`与整条路径接触页`ContactCapacity`分成两个独立的正式Profile容量，避免楼梯多接触使累计页错误溢出。
- 增加独立Ground Envelope Builder：把接触投影到当前Swing脚到Target Landing的纵向与Component Up组成的二维平面，按近到远、低到高稳定排序。
- 使用接触位置与法线定义相邻地面平面；合法交点形成台阶和坡面的边缘候选。
- 保留接触高差作为包络几何事实；`CastAbove/CastBelow`只定义Capsule查询范围，不把碰撞点按可达性删除或拒绝。
- 对全部合法候选计算二维上侧Convex Hull，输出从Path Start到Target Landing的连续折线。
- 左右脚各自把Raw Contacts与Ground Envelope写入现有Committed/Pending双页，并随外层Foot Placement事务Seal或Discard。
- Scene Gizmo显示上一已提交Landing、下一Accepted Landing和最终Ground Envelope；删除逐帧Animated Sole中心线和其它遮挡图形。
- Foot/Pelvis Goal继续保持零权重，Ground Envelope本轮不修改Pose。

## Impact

- Affected specs: `character-foot-placement-presentation`
- Affected code: Ground Path合同与双页、Ground Envelope纯算法、Foot Placement事务、只读diagnostics、Scene Gizmo。
- 不影响Gameplay State、Network、KCC权威结果、Pose Graph拓扑或FinalIK求解。
- 不恢复旧Predictive、Anchor、Pelvis、Foot Lock、第二Grounding、fallback或FBBIK后处理。

## Current Spec Comparison

- 现行spec禁止Ground Envelope、Edge、Hull与Reachability；本change只开放这些feet-only世界事实，Goal和Pose仍保持恒等。
- 现行diagnostics只允许Landing图形；本change改为显示稳定的Current/Next Landing与成功Seal的最终Ground Envelope。
- 当前spec中的Landing Prediction、唯一Goal事务、Gameplay/Network隔离和零权重FinalIK边界保持不变。
