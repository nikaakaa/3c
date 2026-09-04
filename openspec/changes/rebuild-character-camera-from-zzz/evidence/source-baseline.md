# ZZZ 相机来源基线

记录时间：2026-09-04

这份文件只记录已核实的来源身份和冲突边界，不把字段存在当成运行行为已闭合。

## 构建指纹

| 项目 | 值 |
|---|---|
| 产品标识 | `miHoYo / ZenlessZoneZero` |
| ZZZ 数据根 | `D:\\Normal_Software\\HoYoPlay\\games\\ZenlessZoneZero Game\\ZenlessZoneZero_Data` |
| GameAssembly SHA-256 | `4cba5d52c5fbfd478d2a9ec217075f82216780d56ad1bd1e85e4f724dcce30b4` |
| `globalgamemanagers` SHA-256 | `b4d2f79f1f0e77a5e121af5ee34e9bb59c15146f0e6fd8772e28405ee6f9c2d9` |
| `globalgamemanagers.assets` SHA-256 | `a63a34140274f5833555048fca0becc86f08d46b8f3c8e6a70dc4f3d495ae375` |
| Unity 资源目标版本 | `2022.3.62f2c1` |
| 元数据会话 | `829` |
| 配置导出清单 | `D:\\ZZZ_Dump\\output\\corin_replication\\20260904_replication-manifest.json` |

`app.info` 只提供产品标识，没有语义版本号。本次用同一安装目录的二进制 hash、Unity 版本和元数据会话共同锁定来源构建。

## Corin 主相机资源

| 类型 | 原始资源身份 | 字节数 | SHA-256 | 解码数量 |
|---|---|---:|---|---:|
| Shake | `1387972831.blk_export/CAB-fd532b3a2bb364f0cbc2496590111630/CameraShakes_Avatar_Corin.dat` | 106492 | `6171124319a11f382c77321297da29909faa356a2d2bce8d3df65d28fc630b44` | 81 |
| Zoom | `1840230881.blk_export/CAB-09281bc15a3a1cffd8619639b815b8d3/CameraZooms_Avatar_Corin.dat` | 14084 | `c8398bf81f71cd6b3d7eb030d64737adb18696e831f20e866f02b64edc02e14d` | 18 |
| Stretch | `1840230881.blk_export/CAB-1da9ce6da334157fa1683bb6ae65876b/CameraStretchs_Avatar_Corin.dat` | 28704 | `845898847f03807f6ef99010ffa4c40371719001e11f39137f2645f1a0db3d66` | 18 |
| OverrideTrack | `2158315646.blk_export/CAB-6dfda1a50aff1e471ab17801ef09e64e/CameraOverrideTracks_Avatar_Corin.dat` | 7424 | `d6c5545753d6f02a1f59e80920e946a1cffa3cae007a6c8e6711c80d35ca62f0` | 4 |

`CameraShakes_Avatar_Corin.dat` 在 probe5 和 raw_v2 各有一份，两个文件均为 106492 字节且 hash 相同；这是同一资源的导出副本。

## 已确认的 Corin Shot 身份

原生 `CameraCutscenes` 表在五个 Block 副本中都包含以下两项，字段值一致：

| Shot key | cine prefab | Near/Far | duration | Enter/Exit |
|---|---|---:|---:|---:|
| `Avatar_Corin_SwitchIn_Attack_Ex_Start_Cam_01` | `Assets/NapResources/CameraAnim/Combat/Avatar_Female_Size01_Corin_Cam_SwitchIn_Attack_Ex_Start.prefab` | `0.01/6000` | `-1` | `0.5/1.5` |
| `QuestStart_Avatar_Corin_01` | `Assets/NapResources/CameraAnim/Combat/Avatar_Female_Size01_Corin_Cam_QuestStart.prefab` | `0.1/6000` | `-1` | `0.1/1.0` |

表项身份和值已经闭合；prefab 本体、Timeline 绑定及 compact-blend-words 的未命名字段仍未闭合，因此不生成只保留名称的当前工程 Shot 资产。

## 重复与冲突

| 依赖 | 数量 | 内容状态 | 处理 |
|---|---:|---|---|
| `CameraCutscenes.dat` | 5 | 5 个不同 hash；typetree 4 个 identity-only，1 个 source block not found | 按 Block 身份分别保留，不能按文件名合并；2733648653 保持错误 |
| `AvatarTrack.dat` | 175 | 52 个内容 hash | 同 hash 副本只做身份归组；不同 hash 不择一覆盖 |
| Corin 四类主配置 | 4 | 各自唯一 raw hash | 可作为同一构建的主资源闭包入口 |

`CameraCutscenes` 的五个 raw hash：

| Block | 字节数 | SHA-256 |
|---:|---:|---|
| 1223166135 | 128300 | `976f813407590ed92567d5045c4620f2560d830d04792677e2829dcd4c622a9d` |
| 1387972831 | 137504 | `4028585cca845c5d6b2a5bc54d44aae6d1c40f9a4792555c92ecf57433786e63` |
| 1840230881 | 150412 | `3e010c5d2e3f5477ca42ffb045276352fd52f01e60745ded882d9cc1d8fe3e05` |
| 2158315646 | 122552 | `e0adac1b9f7805d50a77f3dc0cbb1c8a67da7cd69c5eae4e1f2957a6457089e1` |
| 2733648653 | 150636 | `eb90f8d686f1a77955a9bd6f831e04dff6430a0e5d46fe4875f5a9c128ac1067` |

## 当前边界

1. `CameraCutscenes` 尚无完整字段级 Shot/曲线/绑定闭包。
2. `AvatarTrack` 尚未完成每个内容 hash 到角色/场景消费者的对账。
3. `WorldBasicCameraData` 到活动 Cinemachine 实例的每阶段写入、更新顺序和回读点仍需继续取证。
