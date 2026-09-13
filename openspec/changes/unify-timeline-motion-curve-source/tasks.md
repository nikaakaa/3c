## 1. 正式源与片段合同

- [ ] 1.1 在现有 RootMotionCurveAsset 所属模块承接完整曲线无损导入与源编辑，保留关键帧、wrap、模式和可确认来源，不新建 Timeline 专属源。
- [ ] 1.2 MotionCurveClip 改为类型化源引用与正式数据段/播放配置，TimelineData、typed binding 和链式 builder 使用同一创建合同。
- [ ] 1.3 在正式领域统一源秒时间与 Timeline 时间映射，保留源区间差值、CurveEndFrame 结束后终值保持及原权重/生命周期。

## 2. 统一作者和编译消费

- [ ] 2.1 MotionCurve 正式作者采样与 MotionWarp 源窗口读取改用源和统一映射，保留 Warp 绑定、空间、窗口与权重规则。
- [ ] 2.2 Timeline semantic 与 ControlMotion catalog emitter 统一从正式源降低到既有 Numeric Program，接入源内容和区间的正式依赖失效。
- [ ] 2.3 Timeline 编辑器移除 Position/Yaw 的本地可写 channel，提供源选择、数据段配置与真实 owner 导航，Weight/Ease 继续本地编辑。

## 3. 存量迁移与旧路径删除

- [ ] 3.1 将 Attack/Dodge 及受字段删除影响的正式 Clip 内嵌曲线无损迁为正式源，保存源与引用，保留 Clip/Warp 身份、播放配置和现有曲线形状。
- [ ] 3.2 迁移归一化曲线的 time 和 tangent 为源秒时间，保留权重、插值和 wrap；不重烘焙动画、不抽点、不虚构来源。
- [ ] 3.3 切换全部受影响作者调用者后删除内嵌 XYZ/Yaw 字段、旧读写/导出分支和一次性旧格式迁移工具，不保留双读或兼容配置。

## 4. C# 与文档闭合

- [ ] 4.1 export_code 复用一次类型化 RootMotionCurveAsset 引用；generate_assets 仅创建数据段和保存绑定，删除源曲线字面量输出与隐式复制行为。
- [ ] 4.2 重新导出受影响的正式生成 C#，保留文件与 meta，继续链式 builder、最小作者配置和每节点一份位置。
- [ ] 4.3 实施完成后同步四份当前 spec 及 skill 的正式状态，移除本次替代的旧内联所有权说法。

用户已明确授权绑定实现任务执行上述工作并设置 goal，绑定与目标见 design 的 Implementation Binding。用户负责端到端验收，不新增测试、验证、回放或证据归集任务。每个职责清楚的改动及时中文小步提交，只提交本任务本步拥有的差异；不改写其它任务提交，不创建空提交。
