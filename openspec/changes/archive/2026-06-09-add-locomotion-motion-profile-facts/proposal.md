# Change: 增加基础移动烘焙运动 Profile Facts

## Why
`RunEnd` 已经可以通过 `OnAnimationEnd` 播完再回 `Idle`，但动画自身携带的急停位移还没有进入角色运动出口。当前无输入时运动命令速度为 0，角色胶囊停住而动画脚步继续刹车，会产生滑步。

需要把 BBB 的离线 Root Motion 烘焙思路转成当前项目自己的数据路径：动画层提供播放进度，烘焙 Profile 采样出本帧运动 facts，逻辑/运动层读取 facts 后仍通过统一运动执行端口移动角色。

## What Changes
- 新增基础移动烘焙运动 Profile 数据规划，用于保存 `phase + alias` 对应的累计本地位移曲线、偏航曲线、时长和校验信息。
- 新增 Motion Profile sampler 规划，把播放进度窗口采样为纯数据运动 facts。
- 规划 `PlayerLocomotionController / BasicLocomotionPipeline / MovementCommand / IBasicLocomotionMotionExecutor` 如何读取并消费这些 facts。
- 规划一个第一版编辑器烘焙工具，参考 BBB 的 `RootMotionExtractor` 采样算法，但不依赖 BBB 运行时。
- 修改基础移动动画位移边界：仍禁止动画外观层直接移动角色，但允许烘焙运动 facts 通过唯一运动执行端口生效。
- 第一版只落地 `MoveStop / RunEnd` 的急停位移，后续 `MoveStart`、转身、闪避、翻越和 Motion Warping 单独扩展。

## Impact
- Affected specs: `basic-locomotion-animation`, `unityhfsm-locomotion`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Config`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Model`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Solver`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Model`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Solver`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime`
  - `3cDemo/Client/3C_Client/Assets/Editor/Character/Animation`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`
- Reference only:
  - `Ref/BBB-Nexus/Editor/RootMotionExtractor.cs`
  - `Ref/BBB-Nexus/Character/ConfigData/CharacterDataDefinitions.cs`
  - `Ref/BBB-Nexus/Character/Core/Driver/MotionDriver.cs`

