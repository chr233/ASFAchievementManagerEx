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

> 修改统计信息/成就具有一定风险, 修改有 VAC 保护的游戏的统计信息/成就可能会被封禁
>
> 使用本插件造成的任何后果与插件作者无关.
>
> 详见 [插件配置说明](#插件配置说明)

## 安装方式

### 初次安装 / 手动更新

1. 从 [GitHub Releases](https://github.com/chr233/ASFAchievementManagerEx/releases) 下载插件的最新版本
2. 解压后将 `ASFAchievementManagerEx.dll` 丢进 `ArchiSteamFarm` 目录下的 `plugins` 文件夹
3. 重新启动 `ArchiSteamFarm` , 使用命令 `AAM` 来检查插件是否正常工作

### 使用命令升级插件

> 可以使用插件自带的命令自带更新插件
> ASF 版本升级有可能出现不兼容情况, 如果发现插件无法加载请尝试更新 ASF

- `AAMVERSION` / `AAMV` 检查插件更新
- `AAMUPDATE` / `AAMU` 自动更新插件到最新版本 (需要手动重启 ASF)

### 更新日志

| ASFAchievementManagerEx 版本                                                      | 适配 ASF 版本 | 更新说明   |
| --------------------------------------------------------------------------------- | :-----------: | ---------- |
| [1.0.1.2](https://github.com/chr233/ASFAchievementManagerEx/releases/tag/1.0.1.2) |   5.4.10.3    | 第一个版本 |

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

| 配置项         | 类型 | 默认值  | 说明                                                                                                     |
| -------------- | ---- | ------- | -------------------------------------------------------------------------------------------------------- |
| `EULA`         | bool | `false` | 是否同意 [EULA](#EULA)\*                                                                                 |
| `Statistic`    | bool | `true`  | 是否允许发送统计数据, 仅用于统计插件用户数量, 不会发送任何其他信息                                       |
| `DisabledCmds` | list | `null`  | 可选配置, 在此列表中的命令将会被禁用\*\* , **不区分大小写**, 仅对 `ASFAchievementManagerEx` 中的命令生效 |

> 当 `EULA` 为 `false` 时, 所有命令将会被禁用
> 当 `EULA` 为 `true` 时, 视为同意 [EULA](#EULA), 此时可以使用所有命令
>
> \*\* `DisabledCmds` 配置说明: 该项配置**不区分大小写**, 仅对 `ASFEnhance` 中的命令有效
> 例如配置为 `["foo","BAR"]` , 则代表 `FOO` 和 `BAR` 命令将会被禁用
> 如果无需禁用任何命令, 请将此项配置为 `null` 或者 `[]`
> 当某条命令被禁用时, 仍然可以使用 `AAM.xxx` 的形式调用被禁用的命令, 例如 `AAM.ALIST`

## 插件指令说明

### 插件更新

| 命令                      | 缩写   | 权限            | 说明                                                           |
| ------------------------- | ------ | --------------- | -------------------------------------------------------------- |
| `ASFAchievementManagerEx` | `AAM`  | `FamilySharing` | 查看 ASFAchievementManagerEx 的版本                            |
| `AAMVERSION`              | `AAMV` | `Operator`      | 检查 ASFAchievementManagerEx 是否为最新版本                    |
| `AAMUPDATE`               | `AAMU` | `Owner`         | 自动更新 ASFAchievementManagerEx 到最新版本 (需要手动重启 ASF) |

### 核心功能

| 命令                                    | 缩写 | 权限       | 说明                                                             |
| --------------------------------------- | ---- | ---------- | ---------------------------------------------------------------- |
| `ALIST [Bots] <AppIds>`                 | -    | `Operator` | 获取指定机器人的成就列表                                         |
| `ASTATS [Bots] <AppIds>`                | -    | `Operator` | 获取指定机器人的统计项列表                                       |
| `AUNLOCK [Bots] AppId <AchievementIds>` | -    | `Master`   | 解锁指定游戏的成就, 部分成就只能由官方服务器设置, 客户端无法解锁 |
| `ASET [Bots] AppId <AchievementIds>`    | -    | `Master`   | 同 `AUNLOCK` (原版插件的指令)                                    |
| `ALOCK [Bots] AppId <AchievementIds>`   | -    | `Master`   | 锁住指定游戏的成就, 部分成就只能由官方服务器设置, 客户端无法解锁 |
| `ARESET [Bots] AppId <AchievementIds>`  | -    | `Master`   | 同 `ALOCK` (原版插件的指令)                                      |
| `AEDIT [Bots] AppId <KeyValues>`        | -    | `Master`   | 设置指定游戏的统计项, keyValue 语法为 Id=值, 具体参考示例        |

### 使用示例

- ALIST 获取成就列表

  ```text
  >> ALIST 410110
  << <7> App/410110 的成就列表:
  - 1 ✅ 第一步
  - 2 ❌ 你想听听我的故事吗？
  - 3 ❌ 你真的有兴趣吗？
  - 4 ✅ 后面会更刺激！
  ```

- ASTATS 获取统计项列表

  ```text
  >> ASTATS 410110
  << <7> App/410110 的统计数据列表:
    Id  [最小 当前 最大] 名称 | 🔒受保护🔼仅能增加⚠️最大修改量
  - 2   [- 0 99000] Killed enemies with a revolver
  - 3   [- 0 99999] Killed enemies with a gun
  - 4   [- 0 99999] Blowed up enemies
  - 5   [- 0 99999] Killed enemies with a knife
  ```

- AUNLOCK / ASET 解锁成就

  ```text
  >> AUNLOCK 410110 1,3,0,a
  << <7> 多行结果:
  警告信息:
  1-第一步: 无需修改该项成就
  0: 找不到此ID的成就
  a: 无效参数, 需要为整数

  执行结果:
  设置成就成功, 受影响成就 1 个
  ```

  > 插件会自动识别指定 `AchievementId` 是否已经解锁, 或是被保护, 可以一次解锁多个成就

- ALOCK / ARESET 锁住成就

  ```text
  >> ALOCK 410110 2,3,0,a
  << <7> 多行结果:
  警告信息:
  2-你想听听我的故事吗？: 无需修改该项成就
  0: 找不到此ID的成就
  a: 无效参数, 需要为整数

  执行结果:
  设置成就成功, 受影响成就 1 个
  ```

- AEDIT 修改统计项

  ```text
  >> AEDIT 410110 2=5,3=0,4=max,5=default,6=min,7=ss
  << <7> 多行结果:
  警告信息:
  3-Killed enemies with a gun [- 0 99999]: 无需更改的统计项
  5-Killed enemies with a knife [- 0 99999]: 无需更改的统计项
  6-Finished quests [- 0 1000]: 无法设置为最小值, 最小值为Null
  7-Killed animals [- 0 1000]: 无法设置为ss, 参数需要为整数

  执行结果:
  设置统计项成功, 受影响统计项 4 个
  ```

  > 语法说明 Id=值, 值可为:
  >
  > 整数: 例如 2=5, 设置 ID 为 2 的统计项为 5
  >
  > 最小值 min(i): 例如 2=i 或 2=min, 设置 ID 为 2 的统计项为最小值, 如果该项最小值为 null 会设置失败
  >
  > 最大值 man(a): 例如 2=a 或 2=max, 设置 ID 为 2 的统计项为最大值, 如果该项最大值为 null 会设置失败
  >
  > 默认值 default(d): 例如 2=d 或 2=default, 设置 ID 为 2 的统计项为默认值, 如果该项默认值为 null 会设置失败

  > 如果统计项具有保护属性, 则无法由客户端修改

  > 如果统计项具有只允许递增属性, 则无法设置为低于当前值的值

  > 如果统计项具有最大修改量属性, 则无法设置差值超过属性的值

---

[![Repobeats analytics image](https://repobeats.axiom.co/api/embed/ab47308c645a5405760e816bc3fa3d70fec8b558.svg "Repobeats analytics image")](https://github.com/chr233/ASFAchievementManagerEx/pulse)

---

[![Stargazers over time](https://starchart.cc/chr233/ASFAchievementManagerEx.svg)](https://github.com/chr233/ASFAchievementManagerEx/stargazers)

---
