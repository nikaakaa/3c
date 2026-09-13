## MODIFIED Requirements

### Requirement: Timeline 导出必须表达 Clip 数据段和独立曲线

Timeline 输出 MUST 表达正式 Clip/数据段引用、源起止区间、时间轴位置及速度、混合、循环或其它正式作者覆盖，不复制逐 tick 数据、烘焙/分析缓存或运行编译结果。MotionCurveClip MUST 引用现有 RootMotionCurveAsset；同一源 MUST 只声明一次类型化外部引用，链式 builder 仅配置区间和播放方式，MUST 不将源 PositionX/Y/Z/Yaw 作为独立内嵌曲线输出。Timeline 自有 Weight/Ease、MotionWarp progress 等独立作者曲线 MUST 保留完整关键帧、切线、权重、插值和正式时间域，不重采样或有损抽点。export_code MUST 不隐式迁移或提取源资产；generate_assets MUST 只解析源、创建并保存片段及绑定，不烘焙或复制源、不将其加入隐式删除范围。源缺失 MUST 明确失败，不回退到旧嵌入数据。

#### Scenario: Clip只使用源数据的一段

- **WHEN** Timeline Clip 指定源资源、源区间和时间轴位置
- **THEN** 生成代码 MUST 通过正式 Clip 创建 API 恢复该数据段
- **AND** MUST 不生成每帧或每 tick 的时间映射数组

#### Scenario: 独立作者曲线包含非默认关键帧

- **WHEN** Clip 拥有独立作者曲线或覆盖曲线
- **THEN** 生成代码 MUST 保留其关键帧、插值/切线/权重和时间域
- **AND** MUST 不把曲线替换成默认曲线或密集采样结果，也不得把源运动曲线复制为局部覆盖

#### Scenario: 多个 MotionCurveClip 使用同一源

- **WHEN** 多个 MotionCurveClip 使用同一 RootMotionCurveAsset 的不同区间
- **THEN** 源码 MUST 复用一次类型化源声明，并分别表达片段配置
- **AND** generate_assets MUST 不创建多个曲线源副本
