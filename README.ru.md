# ASFAchievementManagerEx

> Сделан и улучшен на основе [Rudokhvist/ASF-Achievement-Manager](https://github.com/Rudokhvist/ASF-Achievement-Manager)

[![Codacy Badge](https://app.codacy.com/project/badge/Grade/bb9315f60f9742dd94651c5d09fe1310)](https://www.codacy.com/gh/chr233/ASFAchievementManagerEx/dashboard)
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

## ЛИЦЕНЗИОННОЕ СОГЛАШЕНИЕ

> Изменение статистики/достижений является рискованным, изменение статистики/достижений в играх, защищенных VAC, может быть споводовать VAC бан.
>
> Автор плагина не несет ответственности за любые последствия, вызванные использованием данного плагина.
>
> Подробности смотрите в разделе [конфигурация плагина](#конфигурация-плагина)

## Установка

### Первая установка / Обновление в ручном режиме

1. Загрузите плагин через [GitHub Releases](https://github.com/chr233/ASFAchievementManagerEx/releases) страницу
2. Распакуйте файл `ASFAchievementManagerEx.dll` и скопируйте его в папку `plugins` в директории `ArchiSteamFarm`
3. Учитывая риски, связанные с модификацией достижений, необходимо принять `ЛИЦЕНЗИОННОЕ СОГЛАШЕНИЕ` все команды могут быть доступны использованы после [конфигурации плагина](#конфигурация-плагина)
4. Перезапустите `ArchiSteamFarm` и используйте команду `AAM` для проверки работоспособности плагина

### Команды для обновления

> Для обновления плагина можно использовать собственную команду плагина.
> Обновление версии ASF может быть несовместимым, если вы обнаружили, что плагин не может быть загружен, попробуйте обновить ASF.

- `AAMVERSION` / `AAMV` проверить последнюю версию ASFAchievementManagerEx
- `AAMUPDATE` / `AAMU` автоматическое обновление ASFAchievementManagerEx (возможно, потребуется обновить ASF вручную)

###  Журнал изменений

| Версия ASFAchievementManagerEx                                                    | Зависит от ASF| Описание       | 
| --------------------------------------------------------------------------------- | :-----------: | -------------- |
| [1.0.1.2](https://github.com/chr233/ASFAchievementManagerEx/releases/tag/1.0.1.2) |   5.4.10.3    | Первая версия  |

<details>
  <summary>История изменений</summary>

</details>

## Конфигурация плагина

> Настройка этого плагина не требуется, большинство функций доступно в настройках по умолчанию

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

| Конфигурация   | Тип  | По умолчанию | Описание                                                                                                                                                                       |
| -------------- | ---- | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `EULA`         | bool | `false`      | | Если согласны с [лицензионным соглашением](#лицензионное-соглашение)\*                                                                                                       |
| `Statistic`    | bool | `true`       | Разрешить отправку данных для статистики. Она используется для подсчета количества пользователей, при этом никакой другой информации отправляться не будет                     |
| `DisabledCmds` | list | `null`       | Команды в списке будет отключены , **`DisabledCmds` нечувствительна к командам ASF**, данная конфигурация влияет только на команды `ASFAchievementManagerEx`                                |
> \* Если Вы не согласны с [лицензионным соглашением](#лицензионное-соглашение), то ASFAchievementManagerEx ограничит команды
>
> \* Если Вы согласны с [лицензионным соглашением](#лицензионное-соглашение), то в ASFAchievementManagerEx будут доступны все команды
>
> \*\* Описание `DisabledCmds`: каждый элемент в этой конфигурации является **нечувствительным к командам ASF**, и это влияет только на команды `ASFAchievementManagerEx`.
> Например, если настроить `["foo", "BAR"]`, то это означает, что `FOO` и `BAR` будут отключены,
> если вы не хотите отключать какие-либо команды, настройте их как `null` или `[]`.
> Если некоторые команды отключены, то все равно можно вызвать команду в виде `AAM.xxx`, например `AAM.ALIST`.

## Использование Команд

### Команды Обновления

| Команда                   | Сокращение | Доступ          | Описание                                                                               |
| ------------------------- | ------     | --------------- | -------------------------------------------------------------------------------------- |
| `ASFAchievementManagerEx` | `AAM`      | `FamilySharing` | Получить версию ASFAchievementManagerEx                                                |
| `AAMVERSION`              | `AAMV`     | `Operator`      | Проверить последнюю версию ASFAchievementManagerEx                                     |
| `AAMUPDATE`               | `AAMU`     | `Owner`         | Обновить ASFAchievementManagerEx до последней версии (необходим ручной перезапуск ASF) |

### Основные Функции

| Команда                                 | 缩写 | Доступ       | Описание                                                             |
| --------------------------------------- | ---- | ---------- | ---------------------------------------------------------------- |
| `ALIST [Bots] <AppIds>`                 | -    | `Operator` | 获取指定机器人的成就列表                                         |
| `ASTATS [Bots] <AppIds>`                | -    | `Operator` | 获取指定机器人的统计项列表                                       |
| `AUNLOCK [Bots] AppId <AchievementIds>` | -    | `Master`   | 解锁指定游戏的成就, 部分成就只能由官方服务器设置, 客户端无法解锁 |
| `ASET [Bots] AppId <AchievementIds>`    | -    | `Master`   | 同 `AUNLOCK` (原版插件的指令)                                    |
| `ALOCK [Bots] AppId <AchievementIds>`   | -    | `Master`   | 锁住指定游戏的成就, 部分成就只能由官方服务器设置, 客户端无法解锁 |
| `ARESET [Bots] AppId <AchievementIds>`  | -    | `Master`   | 同 `ALOCK` (原版插件的指令)                                      |
| `AEDIT [Bots] AppId <KeyValues>`        | -    | `Master`   | 设置指定游戏的统计项, keyValue 语法为 Id=值, 具体参考示例        |

### Пример использования

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
