# Change: 增加脚步地面路径与连续包络

## Why

当前链已经能按Landing Event得到Accepted Landing，但Ground Path仍把逐帧Animated Sole当作起点，导致同一脚步周期内重复改变查询输入。参考文章和GDC需要的是上一已提交落点到下一Accepted Landing之间的Ground Detection与Ground Envelope。

本change继续沿唯一Foot Placement事务完成下一层：上一已提交落点保持稳定，下一Landing Event继续接收逐表现帧的唯一正式预测，把上一落点与当前下一Accepted Landing之间的原始Capsule接触整理为Edge，先判断脚能否跨过全部垂直变化，再为可达路径生成连续的feet-only地面上侧包络。

现有实现已经完成Capsule接触、排序、Edge候选和上侧Convex Hull，但跳过了GDC 35页位于Hull之前的Reachability。结果是墙体或过高障碍也可能被发布为Accepted Ground Envelope。本change因此重新打开，补齐这一个缺失边界后才算完成。

## What Changes

- 统一为`上一已提交Accepted Landing -> 下一Landing Event逐帧唯一SphereCast得到的实时Accepted Landing -> 分段Capsule Cast -> Raw Ground Contacts`唯一链；死区内继续预测但复用当前路径，超过死区时更新落点并重建路径。
- 将每段Physics命中缓冲`SegmentHitCapacity`与整条路径接触页`ContactCapacity`分成两个独立的正式Profile容量，避免楼梯多接触使累计页错误溢出。
- 增加独立Ground Envelope Builder：把接触投影到当前Swing脚到Target Landing的纵向与Component Up组成的二维平面，按近到远、低到高稳定排序。
- 使用接触位置与法线定义相邻地面平面；合法交点形成台阶和坡面的边缘候选。
- 在正式Ground Path Profile中增加`MaximumReachableVerticalEdge`，它表达该角色当前步行动画允许脚跨过的最大离散垂直Edge，不复用Capsule查询高度、KCC Step高度或腿长。
- 在Convex Hull之前检查全部有序Edge的Bottom到Top垂直距离；超过限值时发布`UnreachableEdge`与首个Invalid Segment，不删除障碍点、不生成Accepted Envelope。
- 只有全部Edge可达时才对合法候选计算二维上侧Convex Hull，输出从Path Start到Target Landing的连续折线。
- 左右脚各自把Raw Contacts与Ground Envelope写入现有Committed/Pending双页，并随外层Foot Placement事务Seal或Discard。
- Scene Gizmo显示上一已提交Landing、下一Accepted Landing和最终Ground Envelope；不可达时只额外显示红色Invalid Segment，不绘制伪Envelope。
- Foot/Pelvis Goal继续保持零权重，Ground Envelope本轮不修改Pose。

## Impact

- Affected specs: `character-foot-placement-presentation`
- Affected code: Ground Path合同与双页、Ground Path Profile、Ground Envelope纯算法、Foot Placement事务、只读diagnostics、Scene Gizmo与CSV采样器。
- 不影响Gameplay State、Network、KCC权威结果、Pose Graph拓扑或FinalIK求解。
- 不恢复旧Predictive、Anchor、Pelvis、Foot Lock、第二Grounding、fallback或FBBIK后处理。

## Current Spec Comparison

- 现行spec禁止Ground Envelope、Edge、Hull与Reachability；本change开放完整的`Ground Detection -> Edge -> Reachability -> Hull -> Envelope` feet-only世界事实，Goal和Pose仍保持恒等。
- 现行diagnostics只允许Landing图形；本change改为显示稳定的LastLanding/NextSwingLanding与成功Seal的最终Ground Envelope。
- 当前spec中的Landing Prediction、唯一Goal事务、Gameplay/Network隔离和零权重FinalIK边界保持不变。
