# ASFAchievementManagerEx

> Forked from [Rudokhvist/ASF-Achievement-Manager](https://github.com/Rudokhvist/ASF-Achievement-Manager)

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/3d174e792fd4412bb6b34a77d67e5dea)](https://www.codacy.com/gh/chr233/ASFAchievementManagerEx/dashboard)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/chr233/ASFAchievementManagerEx/publish.yml?logo=github)
[![License](https://img.shields.io/github/license/chr233/ASFAchievementManagerEx?logo=apache)](https://github.com/chr233/ASFAchievementManagerEx/blob/master/license)

[![GitHub Release](https://img.shields.io/github/v/release/chr233/ASFAchievementManagerEx?logo=github)](https://github.com/chr233/ASFAchievementManagerEx/releases)
[![GitHub Release](https://img.shields.io/github/v/release/chr233/ASFAchievementManagerEx?include_prereleases&label=pre-release&logo=github)](https://github.com/chr233/ASFAchievementManagerEx/releases)
![GitHub last commit](https://img.shields.io/github/last-commit/chr233/ASFAchievementManagerEx?logo=github)

![GitHub Repo stars](https://img.shields.io/github/stars/chr233/ASFAchievementManagerEx?logo=github)
[![GitHub Download](https://img.shields.io/github/downloads/chr233/ASFAchievementManagerEx/total?logo=github)](https://img.shields.io/github/v/release/chr233/ASFAchievementManagerEx)

[![Bilibili](https://img.shields.io/badge/bilibili-Chr__-00A2D8.svg?logo=bilibili)](https://space.bilibili.com/5805394)
[![Steam](https://img.shields.io/badge/steam-Chr__-1B2838.svg?logo=steam)](https://steamcommunity.com/id/Chr_)

[![Steam](https://img.shields.io/badge/steam-donate-1B2838.svg?logo=steam)](https://steamcommunity.com/tradeoffer/new/?partner=221260487&token=xgqMgL-i)
[![爱发电](https://img.shields.io/badge/爱发电-chr__-ea4aaa.svg?logo=github-sponsors)](https://afdian.net/@chr233)

## EULA

> 请不要使用本插件来进行不受欢迎的行为, 包括但不限于: 刷好评, 发布广告 等.
>
> 详见 [插件配置说明](#插件配置说明)

## 安装方式

### 初次安装 / 手动更新

1. 从 [GitHub Releases](https://github.com/chr233/ASFAchievementManagerEx/releases) 下载插件的最新版本
2. 解压后将 `ASFAchievementManagerEx.dll` 丢进 `ArchiSteamFarm` 目录下的 `plugins` 文件夹
3. 重新启动 `ArchiSteamFarm` , 使用命令 `ASFE` 来检查插件是否正常工作

### 使用命令升级插件

> 可以使用插件自带的命令自带更新插件
> ASF 版本升级有可能出现不兼容情况, 如果发现插件无法加载请尝试更新 ASF

- `ASFEVERSION` / `AV` 检查插件更新
- `ASFEUPDATE` / `AU` 自动更新插件到最新版本 (需要手动重启 ASF)

### 更新日志

| ASFAchievementManagerEx 版本                                                      | 适配 ASF 版本 | 更新说明 |
| --------------------------------------------------------------------------------- | :-----------: | -------- |
| [1.0.0.0](https://github.com/chr233/ASFAchievementManagerEx/releases/tag/1.0.0.0) |    5.4.9.3    |          |

<details>
  <summary>历史版本</summary>

</details>

## 插件配置说明

> 本插件的配置不是必须的, 保持默认配置即可使用大部分功能

ASF.json

```json
{
  //ASF 配置
  "CurrentCulture": "...",
  "IPCPassword": "...",
  "...": "...",
  //ASFAchievementManagerEx 配置
  "ASFAchievementManagerEx": {
    "EULA": true,
    "Statistic": true,
    "DisabledCmds": ["foo", "bar"]
  }
}
```

| 配置项         | 类型 | 默认值 | 说明                                                                                                     |
| -------------- | ---- | ------ | -------------------------------------------------------------------------------------------------------- |
| `EULA`         | bool | `true` | 是否同意 [EULA](#EULA)\*                                                                                 |
| `Statistic`    | bool | `true` | 是否允许发送统计数据, 仅用于统计插件用户数量, 不会发送任何其他信息                                       |
| `DisabledCmds` | list | `null` | 可选配置, 在此列表中的命令将会被禁用\*\* , **不区分大小写**, 仅对 `ASFAchievementManagerEx` 中的命令生效 |

> \* 同意 [EULA](#EULA) 后, ASFAchievementManagerEx 将会开放全部命令, 作为交换, ASFAchievementManagerEx 会在执行 `GROUPLIST` 和 `CURATORLIST` 时自动关注作者的[鉴赏家](https://steamcommunity.com/groups/11012580/curation)和[组](https://steamcommunity.com/groups/11012580) (如果尚未关注的话)
>
> \* 禁用 [EULA](#EULA) 后, ASFAchievementManagerEx 将会限制使用 鉴赏家/群组/评测 等功能, 同时 ASFAchievementManagerEx 也不会主动关注[鉴赏家](https://steamcommunity.com/groups/11012580/curation)和[组](https://steamcommunity.com/groups/11012580)
>
> \*\* `DisabledCmds` 配置说明: 该项配置**不区分大小写**, 仅对 `ASFAchievementManagerEx` 中的命令有效
> 例如配置为 `["foo","BAR"]` , 则代表 `FOO` 和 `BAR` 命令将会被禁用
> 如果无需禁用任何命令, 请将此项配置为 `null` 或者 `[]`
> 当某条命令被禁用时, 仍然可以使用 `ASFE.xxx` 的形式调用被禁用的命令, 例如 `ASFE.EXPLORER`

## 插件指令说明

### 插件更新

| 命令                      | 缩写   | 权限            | 说明                                                           |
| ------------------------- | ------ | --------------- | -------------------------------------------------------------- |
| `ASFAchievementManagerEx` | `ASFE` | `FamilySharing` | 查看 ASFAchievementManagerEx 的版本                            |
| `ASFEVERSION`             | `AV`   | `Operator`      | 检查 ASFAchievementManagerEx 是否为最新版本                    |
| `ASFEUPDATE`              | `AU`   | `Owner`         | 自动更新 ASFAchievementManagerEx 到最新版本 (需要手动重启 ASF) |

## IPC 接口

> 使用该功能前需要同意 EULA, 详见 [插件配置说明](#插件配置说明)

| API | 方法 | 参数 | 说明 |
| --- | ---- | ---- | ---- |
| -   |      |      |      |

---

[![Repobeats analytics image](https://repobeats.axiom.co/api/embed/df6309642cc2a447195c816473e7e54e8ae849f9.svg "Repobeats analytics image")](https://github.com/chr233/ASFAchievementManagerEx/pulse)

---

[![Stargazers over time](https://starchart.cc/chr233/ASFAchievementManagerEx.svg)](https://github.com/chr233/ASFAchievementManagerEx/stargazers)

---
