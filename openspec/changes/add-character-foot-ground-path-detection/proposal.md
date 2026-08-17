# Change: 增加脚步地面路径与连续包络

## Why

当前链已经能得到同一只脚的Current/Next Accepted Landing，并在两点之间执行真实Capsule Ground Detection，但Scene里只有两点间中心直线。该线没有消费地面接触、法线、边缘或凸包，不能代表参考文章和GDC 33–36页中的Ground Envelope。

本change继续沿唯一Foot Placement事务完成下一层：把原始Capsule接触变成可达、连续的feet-only地面上侧包络，并让Debug只显示这份正式结果和两次落点。

## What Changes

- 保留`Current/Incoming Step -> 两次Landing -> 分段Capsule Cast -> Raw Ground Contacts`唯一链。
- 增加独立Ground Envelope Builder：把接触投影到脚步纵向与Component Up组成的二维平面，按近到远、低到高稳定排序。
- 使用接触位置与法线定义相邻地面平面；合法交点形成台阶和坡面的边缘候选。
- 使用现有`CastAbove/CastBelow`检查边缘与整条路径的竖直可达性，不可达时发布typed rejection，不输出替代路径。
- 对可达候选计算二维上侧Convex Hull，输出从Current Landing到Next Landing的连续折线。
- 左右脚各自把Raw Contacts与Ground Envelope写入现有Committed/Pending双页，并随外层Foot Placement事务Seal或Discard。
- Scene Gizmo保留绿色Current Landing与黄色Next Landing，以左右脚不同颜色绘制最终Ground Envelope粗折线；删除中心直线和其它遮挡图形。
- Foot/Pelvis Goal继续保持零权重，Ground Envelope本轮不修改Pose。

## Impact

- Affected specs: `character-foot-placement-presentation`
- Affected code: Ground Path合同与双页、Ground Envelope纯算法、Foot Placement事务、只读diagnostics、Scene Gizmo。
- 不影响Gameplay State、Network、KCC权威结果、Pose Graph拓扑或FinalIK求解。
- 不恢复旧Predictive、Anchor、Pelvis、Foot Lock、第二Grounding、fallback或FBBIK后处理。

## Current Spec Comparison

- 现行spec禁止Ground Envelope、Edge、Hull与Reachability；本change只开放这些feet-only世界事实，Goal和Pose仍保持恒等。
- 现行diagnostics只允许Landing图形；本change改为显示两次Accepted Landing与成功Seal的最终Ground Envelope。
- 当前spec中的Landing Prediction、唯一Goal事务、Gameplay/Network隔离和零权重FinalIK边界保持不变。
