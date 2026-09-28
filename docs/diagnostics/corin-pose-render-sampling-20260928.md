# 可琳骨骼写入与渲染对照采样

## 用途与入口

排查行走时偶发双脚快速摆动：区分正式姿态已经变化，还是写入 Transform 后被其它逻辑改动。沿用当前 Gameplay Lab 手动采样入口，简版和完整版均包含此项；不改 IK、同步组或动画资源。

## 同一帧的三个时点

1. 表现提交完成后：保存角色根节点与腿部 Transform。
2. 绑定的实际输出相机进入 URP `beginCameraRendering`：保存渲染前 Transform。
3. 同一相机进入 `endCameraRendering`：保存渲染后 Transform，并通过原表现诊断事件提交整帧。

只观察装配时绑定的 Cinemachine Brain 输出相机，不使用 Scene View、预览相机或任意其它相机。每个表现帧最多提交一次；同一 Unity 帧多次渲染该相机不会重复计数。

未走到对应渲染回调时，`before-render-available` / `after-render-available` 为 false。下一表现帧开始、角色重置、销毁、手动停止或重载前保存时，会先提交尚未发送的数据，避免读取已经失效的姿态缓冲。没有渲染的帧保留原表现记录，但不伪造渲染证据。

## 输出

保存在原 `Diagnostics/GeneratedPresentationSampling/<采样目录>` 下，新增 `character-presentation-replication%2Ffull.render-bones.csv`；简版对应 `core.render-bones.csv`。

仍用 `sample.lineage.high`（表现帧）和 `sample.lineage.low`（姿态完成标识）连接 sources、state-machines 和原足部采样。主表新增 `render/unity-frame`、输出相机 Instance ID、三个阶段时间戳和两个渲染可用标记。

每帧 13 个节点：LogicRoot、VisualRoot、PoseRoot、动画根骨、骨盆，以及左右髋、膝、踝、脚趾。每个节点包含三个阶段的局部位置、局部旋转、局部缩放、世界位置、世界旋转，以及相对写入后快照的局部位置、旋转、缩放差异。渲染证据缺失时，对应字段有正式 availability 标记。

如何读：局部姿态改变说明节点自身在写入后被改动；局部姿态不变而世界姿态改变时，沿根节点和父骨骼查整体变换。前后完全一致只能排除这几个采样时点之间保留下来的覆盖，不能证明整个动画视觉正常，也无法捕获两个采样点之间发生后又恢复的瞬时修改。

## 代码与开销

`CharacterPoseRenderCaptureRuntime` 负责生命周期、相机回调与复用缓冲；`CharacterPoseRenderCapture` 定义正式生成采样的数据合同；表现域工厂负责装配。沿用原 sampler、manifest、CSV 生成器，没有另写文件输出链或 MonoBehaviour。

数组和回调在装配时创建，逐帧循环不创建集合、不格式化字符串。只有表现采样订阅开启时才读取 Transform；未采样时回调直接返回。未做运行时 GC 实测。

## 已检查与边界

Unity 已编译包含新增文件的 Runtime / Editor 程序集，首次检查 Console 为 0 错误。编译后的简版、完整版 schema 均回读到 capability revision 4、7 个 render 主表字段，以及容量 13、24 个字段的 render-bones 表。

尚未运行 Play 或 replay，也没有生成真实的渲染对照录制。该改动交付采样能力，不宣称修复了行走拉扯。
