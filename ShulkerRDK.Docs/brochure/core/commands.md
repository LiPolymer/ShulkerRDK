# 指令

在交互模式中, 您可以输入指令来管理项目、查看信息或执行各类操作

进入交互模式后, 提示符会显示为:
```
>
```

直接输入您想要执行的指令即可, 如果您不确定有哪些可用指令, 可以先输入:
```
help c
```

这会列出当前所有可用的指令及其简要描述



## exit

输入 `exit` 即可退出 ShulkerRDK:
```
>exit
正在退出...
```



## env

用于管理项目环境变量, 支持查询、设置、删除和列出操作:

```
env get <变量名>           # 查询指定环境变量的值
env set <变量名> <变量值>   # 设置或修改环境变量
env remove <变量名>        # 删除环境变量
env list                  # 列出所有环境变量
```

**示例:**
```
>env set project.name "My Resource Pack"
已将项目环境变量[project.name]修改为[My Resource Pack]

>env get project.name
项目环境变量[project.name]>[My Resource Pack]

>env list
所有项目环境变量:
 - [project.src]>[./src/]
 - [project.output]>[./build/]
 - [project.name]>[My Resource Pack]

>env remove old.var
```

> [!TIP]
> 环境变量会被持久化保存到项目配置文件中, 下次启动时依然有效
>
> 在 Levitate 脚本中, 环境变量可以通过 `%变量名%` 语法或 `env get` 方法 引用



## clear

清屏指令, 清除终端当前显示的所有内容:
```
clear
```



## help

显示指令或别名的帮助信息:

```
help commands     (或 help c)    # 列出所有指令
help alias        (或 help a)    # 列出所有别名
```

## proj

项目管理指令, 可以查看和修改项目名称、资源根目录和输出目录:

```
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



## verm

项目版本管理指令, 支持语义化版本号的步进和直接设置:

```
verm show              # 显示当前版本号
verm smajor            # 主版本号 +1
verm sminor            # 次版本号 +1
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
项目版本更新为[1.1.1]

>verm set 2.0.0
已将项目版本修改为[2.0.0]
```



## netfile

管理网络链接文件。这项功能用于减小 Git 仓库体积 — 大文件不直接存储在仓库中, 而是存储为一个包含下载链接和 SHA1 校验的小文件:

```
netfile create <文件路径> <下载URL>       # 创建网络链接文件
netfile clean                           # 清理网络文件缓存
netfile restore [源目录] [输出目录]       # 将链接文件还原为真实文件
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
是否继续? [y/n]
y
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

文件的实际内容存储在 `./shulker/local/netFiles/` 目录下, 以 SHA1 哈希值命名

> [!NOTE]
> `netfile restore` 不传参数时, 默认从项目资源根目录 (`%project.src%`) 还原到自身



## ext

扩展管理器, 用于查看已加载的扩展信息:

```
ext list                    # 列出所有已加载扩展
ext lookup <扩展ID>          # 查看指定扩展详情
```

**示例:**
```
>ext list
已加载扩展列表...

>ext lookup shulker.core
扩展详情...
```



## task

管理 Levitate Task 脚本:

```
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
开始执行 build.lvt 脚本...
```



## reload

> [!WARNING]
> 重载可能会导致未知的异常, 不建议使用
>
> 如遇到问题请不要向开发者报告

```
reload
```
