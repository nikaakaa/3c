## 1. Swing Foot Motion合同

- [x] 1.1 定义每脚Foot Motion State、typed Reject Reason、可达Ground Envelope输入、输出与只读diagnostics。
- [x] 1.2 定义Original Sole、路径进度、Baseline Sample、Envelope Sample、垂直修正与最终Goal之间的明确合同。
- [x] 1.3 保持Pelvis、PreSwing、支撑脚和失败脚的Goal权重为零。

## 2. 纯计算模块

- [x] 2.1 新增不引用Physics、FinalIK或Editor类型的Swing Foot Motion Builder。
- [x] 2.2 从原生动画Sole计算LastLanding到NextSwingLanding的有限纵向进度。
- [x] 2.3 按纵向距离采样有序Ground Envelope与Landing基线。
- [x] 2.4 计算非负垂直地形增量并保持Ankle、Heel、Toe的原始水平位置与旋转。
- [x] 2.5 对上游`UnreachableEdge`、非法端点、乱序Envelope、退化路径、身份不一致和负增量发布typed rejection。

## 3. Foot Placement事务与Goal

- [x] 3.1 从Current authoritative Swing Step和同事件、全部Edge可达的Accepted Ground Path建立唯一Foot Motion输入。
- [x] 3.2 使用现有`animation.foot-placement-weight`作为上限，并按当前Swing phase连续生成位置Goal，旋转权重保持为零。
- [x] 3.3 保持支撑脚与Pelvis零权重，不修改FinalIK Goal ABI或增加第二solver。
- [x] 3.4 让Foot Motion结果、Goal和Ground Path随同一Pending Frame执行Seal或Discard。
- [x] 3.5 删除任何第二Reachability、旧Goal复用、默认Envelope、直线替代或失败补洞路径。

## 4. 可观察诊断

- [x] 4.1 把Original Sole/Ankle、progress、Baseline、Envelope Sample、Vertical Correction、Corrected Sole与Goal权重写入成功Seal后的只读摘要。
- [x] 4.2 扩展Scene Gizmo绘制Original Sole、Corrected Sole和实际垂直修正细线，并保留上游Invalid Segment显示，不重新计算Foot Motion或Reachability。
- [x] 4.3 扩展CSV采样器记录同一摘要和typed rejection，保持采样器只读。
- [x] 4.4 在唯一Final Pose Writer之后记录左右物理脚踝组件位置、写入完成identity及相对Goal残差，并随现有诊断事务输出到CSV。

## 5. 文档与校验

- [x] 5.1 对账Ground Path Reachability结果与GDC第11、31、33-36页，更新proposal、design与spec delta，保持Predictive Foot Motion和项目单一Goal链口径一致。
- [x] 5.2 明确区分Foot Motion、Goal、FinalIK Component Pose与final writer物理骨骼结果，并记录同帧watch与CSV物理脚踝字段的验证方法。
- [x] 5.3 记录GDC参考后续Foot Locking、Hips稳定、Foot Orientation和转向Pivot阶段，不在本change提前实现。
- [x] 5.4 刷新Unity脚本并检查Console，不运行独立Runtime或Editor编译。
- [x] 5.5 执行定向`git diff --check`。
- [x] 5.6 执行OpenSpec strict validate。
