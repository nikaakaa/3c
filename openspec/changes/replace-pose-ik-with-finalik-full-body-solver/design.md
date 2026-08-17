# Design

## 1. 保留边界

```text
Original Component Pose
+ Atomic Biomechanical Step Facts
+ Committed Body Trajectory
+ World Query
-> CharacterFootPlacementRuntime single-frame transaction
-> Pelvis / Left Foot / Right Foot Final Goal Set
-> one FinalIK FBBIK
```

保留模块：Pose输入合同、Foot Feature、Rig/Calibration、Heel/Toe/Sole重建、Surface、WorldQuery、Lyra Current Grounding基础与FinalIK。上述模块不得拥有预测Plan生命周期。

## 2. 已删除边界

旧Predictive Planner、Plan、Query、Ground Envelope、WorldProjection、Revision、Event Successor、Output Continuity、LandingHandoff、Stance/Anchor/Pelvis、Profile/Tuning、Gizmo、CSV Capture和自动Foot IK控制入口全部删除。重做不得复制旧类型或恢复兼容路径。

## 3. 重做顺序

### 3.1 平地动画事实

先在无IK地形修正下证明：同一权威Action Phase采样的Animation Foot Route与Native Sole XZ、旋转和Landing端点一致。可视化必须画完整路线和当前相位点；实际脚不在线上时禁止进入Ground Query。

### 3.2 确定的身体轨迹

Body trajectory只来自Simulation committed位置、朝向、线速度和角速度。in-place动画不提供世界Root位移。一次Plan冻结一个trajectory generation与动作时钟。

首个可运行阶段只发布`Landing Prediction`，不生成Path、Hull、Foot Motion、Lock或Pelvis：

```text
Current/Incoming Biomechanical Step
+ committed Body trajectory
-> RootLocalLanding世界投影
-> 向下SphereCast
-> Accepted Landing或typed Rejection
```

该阶段仍从唯一FootPlacement事务输出三个零权重Goal，保证FinalIK不改变原动画姿势。零权重Goal只表示该阶段尚未拥有Foot Motion，不得被描述为预测IK效果或响应式兜底。

### 3.3 转向Revision

有效输入/轨迹方向改变必须新建Revision：

```text
new committed trajectory
-> new landing position/facing
-> new landing cast
-> new capsule path samples
-> new edge planes
-> new reachability filtering
-> new upper ground envelope
```

禁止把旧Foot Route、旧命中点、旧Surface法线或旧凸包按Root yaw旋转。旧Plan保持不可变，直到新Plan查询成功后只做一次Goal交接。

### 3.4 Ground Path与Foot Motion

GDC顺序固定为：采集位置/法线、排序、Edge Plane、Reachability、删除不可达、上侧Hull。Ground Envelope只是feet-only下界；最终Foot Motion等于环境下界加动画相对Foot Path的Clearance，不能让Envelope接管动画XZ。

### 3.5 Landing、Lock与Pelvis

Landing原子提交Sole Pose、重心化Support、Surface identity/local anchor和Plan/Event identity。Locked、Sliding、Unlocked来自同一动画数据并由世界约束验证。Pelvis只消费同源Support Leg/Body Path，不从左右脚高度临时猜测。

## 4. 失败策略

任一Step、Trajectory、Route、Query、Hull、Landing或Goal事实无效时发布typed failure。不得使用第二Grounding、第二查询、Original/Current Grounding中途fallback、固定高度、第二Pelvis或FBBIK后处理。

## 5. 诊断顺序

```text
Artifact reconstruction
-> Phase-aligned Animation Foot Route vs Native Sole
-> Committed Body Trajectory
-> Query requests/hits/rejections
-> Edge/Reachability/Hull
-> Foot Motion
-> Landing/Lock
-> Pelvis/Reach
-> Final Goal
-> FBBIK result/residual
```

Landing Prediction阶段的Gizmo固定为：白色Native Sole、青色Raw Landing、黄色实际SphereCast、绿色Accepted Landing、红色Rejected位置，不显示文字、不绘制伪Path。
