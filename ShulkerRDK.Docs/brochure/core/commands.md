# 指令

在交互模式中, 您可以输入指令来管理项目、查看信息或执行各类操作

进入交互模式后, 提示符会显示为:
```
>
```

直接输入您想要执行的指令即可, 如果您不确定有哪些可用指令, 可以先输入:
```lvt
help c
```

这会列出当前所有可用的指令及其简要描述

---

## exit

输入 `exit` 即可退出 ShulkerRDK:
```
>exit
正在退出...
```

---

## env

用于管理项目环境变量, 支持查询、设置、删除和列出操作:

```lvt
env get <变量名>           # 查询指定环境变量的值
env set <变量名> <变量值>   # 设置或修改环境变量
env remove <变量名>        # 删除环境变量
env list                  # 列出所有环境变量
```

**示例:**
```
>env set my.var "Hello"
[my.var]定义为[Hello]

>env get my.var
项目环境变量[my.var]>[Hello]

>env list
所有项目环境变量:
 - [my.var]>[Hello]

>env remove my.var
```

> [!TIP]
> 环境变量会被持久化保存到 `./shulker/proj.json` 中, 下次启动时依然有效
>
> 在 Levitate 脚本中, 环境变量可以通过 `%变量名%` 语法或 `env get` 方法引用

---

## clear

清屏指令, 清除终端当前显示的所有内容:
```lvt
clear
```

---

## help

显示指令或别名的帮助信息:

```lvt
help commands     (或 help c)    # 列出所有指令
help alias        (或 help a)    # 列出所有别名
```

---

## proj

项目管理指令, 可以查看和修改项目名称、资源根目录和输出目录:

```lvt
proj info           (或 proj i)      # 显示当前项目信息
proj chname <新名称>                  # 修改项目名称
proj chroot <新路径>                  # 修改项目资源根目录
proj chout  <新路径>                  # 修改项目输出目录
```

**示例:**
```
>proj i
My Project@1.0.0
项目资源根[./src/]
项目输出目录[./build/]

>proj chname "新项目名称"
已将项目名修改为[新项目名称]

>proj chroot ./assets/
已将项目资源根修改为[./assets/]

>proj chout ./dist/
已将项目输出目录修改为[./dist/]
```

---

## verm

项目版本管理指令, 支持常规版本号、带后缀的预发布版本号以及 Shulker 版本格式的步进与直接设置:

```lvt
verm show              # 显示当前版本号
verm smajor            # 主版本号 +1 (较低位段归零)
verm sminor            # 次版本号 +1 (较低位段归零)
verm sfix              # 修订号 +1
verm set <版本号>       # 直接设置版本号
```

**示例:**
```
>verm show
当前版本号[1.0.0]

>verm sfix
项目版本更新为[1.0.1]

>verm sminor
项目版本更新为[1.1.0]

>verm smajor
项目版本更新为[2.0.0]

>verm set 2.0.0
已将项目版本修改为[2.0.0]
```

::: details **关于 Shulker 版本格式与预发布后缀**

- **Shulker 版本格式 (`R/B/A/D<主版本>.<两位次版本>[fix<N>]`)**, 例如 `B0.20`、`R1.00`:
  - `sfix`: `B0.20` -> `B0.20fix1` -> `B0.20fix2`
  - `sminor`: `B0.20fix1` -> `B0.21` (`00`–`99` 之后进入 `a0`–`zz` 进位, 并去掉 `fix`)
  - `smajor`: `B0.21fix2` -> `B1.00`
- **预发布后缀**: 比如 `1.2.3-alpha` 执行 `sfix` 会变成 `1.2.4-alpha`, 执行 `sminor` 会变成 `1.3.0-alpha`

:::

---

## netfile

管理网络链接文件。这项功能用于减小 Git 仓库体积 — 大文件不直接存储在仓库中, 而是存储为一个包含下载链接和 SHA1 校验的小文件:

```lvt
netfile create <文件路径> <下载URL>       # 创建网络链接文件
netfile clean                           # 清理网络文件缓存
netfile restore                         # 原地还原 %project.src% 下的 .nfm 并删除原链接文件
netfile restore <源目录> <输出目录>       # 还原到指定目录 (保留原 .nfm 文件)
```

**示例:**
```
>netfile create ./src/assets/big_texture.png "https://example.com/big_texture.png"
正在获取文件[https://example.com/big_texture.png]
正在分析
正在存入
完成!

>netfile clean
正在清理缓存文件...
完成!

>netfile restore
警告 此操作将转化链接文件为真实文件,不可撤销
是否继续? [y/n] y
正在复原[./src/]
完成!
```

**网络链接文件格式:**

创建后会生成 `.nfm` 格式的 JSON 文件:
```json
{
  "Sha1": "abc123def456...",
  "Link": "https://example.com/big_texture.png"
}
```

文件实体缓存在 `./shulker/local/netFiles/` 目录下, 以 SHA1 哈希值命名

---

## ext

扩展管理器, 用于查看已加载的扩展信息:

```lvt
ext list                    # 列出所有已加载扩展
ext lookup <扩展ID>          # 查看指定扩展详情
```

**示例:**
```
>ext list
已安装的扩展[3]
 - ShulkerRDK@B0.20#shulker.core
 - ModrinthPSK@B0.20#shulker.modrinth

>ext lookup shulker.core
```

---

## task

管理 Levitate Task 脚本:

```lvt
task list           # 列出所有可用的任务脚本
task <任务名>        # 执行指定任务
```

任务脚本文件位于 `./shulker/tasks/` 目录下, 扩展名为 `.lvt`

**示例:**
```
>task list
所有可使用的LevitateTask
 - build [25lines]
 - publish [18lines]
 - dev [12lines]

>task build
```

---

## reload

> [!WARNING]
> 重载可能会导致未知的异常, 不建议使用
>
> 如遇到问题请不要向开发者报告

```lvt
reload
```

---

## 指令别名

交互模式内置了几个常用短指令别名:

| 输入 | 等价于 |
| :--- | :--- |
| `build` | `task build` |
| `publish` | `task publish` |
| `run` | `task run` |
| `dev` | `task dev` |

也可以通过 `env set alias.command.<正则> "<替换内容>"` 自定义自己的指令别名 (重启后生效):
```lvt
env set alias.command.^b$ "task build"
```
