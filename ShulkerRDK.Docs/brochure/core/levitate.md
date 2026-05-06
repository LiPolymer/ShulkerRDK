# Levitate 方法

Levitate 方法是 ShulkerRDK 实现自动化和客制化的主要工具

它们以 DSL (指令式领域特定语言) 的形式写在 `.lvt` 脚本文件中, 通过定义一系列命令来指导 ShulkerRDK 自动完成较为复杂的任务



## 开始编写您的脚本

在 `./shulker/tasks/` 目录下创建一个 `.lvt` 文件, 即可开始编写

**基本语法:**
```
<方法名> [参数1] [参数2] [参数3] ...
```

- 每行一条指令
- 参数之间用空格分隔
- 包含空格的参数需要用双引号包裹
- 以 `#` 开头的行为注释, 将被跳过
- 空行将被跳过

> [!TIP]
> 您可能注意到带有空格的参数被双引号包裹
>
> 这和 ShulkerRDK 如何解析参数有关 — 通常参数以空格分割, 但双引号内包裹的内容会作为一个整体来解析



## 变量/表达式?

在脚本执行时, ShulkerRDK 会按以下顺序自动处理传入的指令:

1. **变量注入** — 使用 `^变量名^` 语法, 引用 `var` 定义的局部变量
2. **别名解析** — 自动匹配已注册的别名并进行替换
3. **环境变量注入** — 使用 `%变量名%` 语法, 引用环境变量
4. **表达式求值** — 使用 `{方法 参数}` 语法, 执行方法并用返回值替换
5. 最终执行

**执行示例:**
```lvt
# 这是一条注释
echo "Hello World!"

# 使用变量
var my_var "Hello"
echo ^my_var^

# 使用环境变量
echo "项目名: %project.name%"

# 使用表达式
echo "缓存路径: {path dir %project.cache%/test.txt}"
```



## 自动环境变量

每个 Levitate 脚本执行时, 以下环境变量会自动注入:

| 环境变量               | 说明      | 示例值                           |
| ------------------ | ------- | ----------------------------- |
| `%project.src%`    | 项目资源根目录 | `./src/`                      |
| `%project.name%`   | 项目名称    | `My Project`                  |
| `%project.output%` | 项目输出目录  | `./build/`                    |
| `%project.ver%`    | 项目版本号   | `1.0.0`                       |
| `%project.cache%`  | 项目缓存目录  | `./shulker/local/cache/build` |

此外, 您在项目配置中定义的自定义环境变量也会被注入



## 局部变量 — var

用于在脚本执行期间存储临时数据:

```
var <变量名> <变量值>
```

通过 `^变量名^` 语法引用:

```lvt
var greeting "Hello"
echo ^greeting^ World!
# 输出: Hello World!
```



## 环境变量 — env

在脚本中获取或设置环境变量, 可用于方法间的值传递:

```
env get <变量名>          # 获取环境变量值 (作为方法返回值)
env set <变量名> <值>      # 设置环境变量
```

**示例:**
```lvt
env set build.mode "release"
env set build.target "1.20.1"

# 获取环境变量
var mode {env get build.mode}
echo "构建模式: ^mode^"
```

> [!NOTE]
> `env get` 会返回环境变量的值, 可以被大括号表达式捕获, 当然您也可以使用 `%环境变量名%`
>
> 如果变量不存在, 会输出警告并返回空值
>
> 脚本中设置的环境变量仅在当前解释器实例中有效, 脚本结束后不会保留



## 别名

别名使用正则表达式模式进行匹配和替换, 可以在脚本中简化常用操作的输入

**内置 Levitate 别名:**

| 别名模式            | 替换为                                                                                  |
| --------------- | ------------------------------------------------------------------------------------ |
| `^makeCleanup$` | `delete "%project.cache%"`                                                           |
| `^makeCopy`     | `copy "%project.src%" "%project.cache%" true `                                       |
| `^makePkg$`     | `pkgr zip make "%project.cache%" "%project.output%%project.name%_%project.ver%.zip"` |

**使用示例:**
```lvt
makeCopy
# 等价于: copy "%project.src%" "%project.cache%" true

makeCleanup
# 等价于: delete "%project.cache%"
```



## echo

输出文本到终端:

```
echo [内容]
```

**示例:**
```lvt
echo "Hello World!"
echo 不带引号的文本
echo "当前版本: %project.ver%"
echo "构建路径: {path dir \"%project.output%\"}"
```



## input

显示提示文本并等待用户输入, 返回用户输入的内容:

```
input [提示文本]
```

**示例:**
```lvt
# 等待用户输入并将结果存入变量
var user_input {input "请输入您的名字: "}
echo "你好, ^user_input^!"
```



## run

从脚本中执行另一个 Levitate Task 脚本:

```
run <脚本路径>        # 执行已有脚本
run <脚本路径> new    # 使用新的解释器实例执行
```

**示例:**
```lvt
# 执行 ./shulker/tasks/build.lvt
run ./shulker/tasks/build.lvt

# 使用新解释器实例执行 (变量和环境不会互相影响)
run ./shulker/tasks/cleanup.lvt new
```



## import

导入扩展的 Levitate 方法和别名到当前解释器:

```
import <扩展ID>
```

**示例:**
```lvt
# 导入 ResourceMagick 扩展的方法
import shulker.resourcemagick

# 导入 ModrinthPSK 扩展的方法
import shulker.modrinth
```

导入后, 该扩展提供的所有 Levitate 方法和别名都可以在当前脚本中使用



## copy

复制文件或目录:

```
copy <源路径> [目标路径] [是否覆盖] [忽略正则]
```

| 参数       | 必填 | 默认值     | 说明                        |
| -------- | -- | ------- | ------------------------- |
| `<源路径>`  | 是  | -       | 要复制的文件或目录路径               |
| `[目标路径]` | 否  | 源目录的父目录 | 复制目标位置                    |
| `[是否覆盖]` | 否  | `true`  | `true` 覆盖已存在文件，`false` 跳过 |
| `[忽略正则]` | 否  | 无       | 匹配此正则的文件将被忽略              |

**示例:**
```lvt
# 复制整个目录
copy "%project.src%" "%project.cache%"

# 复制目录, 忽略 .gitkeep 文件
copy "%project.src%" "%project.cache%" true "\.gitkeep$"

# 复制单个文件
copy "./src/icon.png" "./build/icon.png"
```

> [!NOTE]
> `copy` 会自动创建不存在的目标目录



## delete

删除文件或目录:

```
delete <目标路径> [正则过滤]
```

| 参数       | 必填 | 说明                |
| -------- | -- | ----------------- |
| `<目标路径>` | 是  | 要删除的文件或目录路径       |
| `[正则过滤]` | 否  | 如果提供，则只删除匹配此正则的文件 |

**示例:**
```lvt
# 删除整个目录
delete "%project.cache%"

# 删除目录下所有 .tmp 文件
delete "%project.cache%" "\.tmp$"
```



## flat

平整目录 — 将目录树中所有文件复制到目标目录, 忽略子目录结构:

```
flat <源目录> <目标目录> [是否覆盖] [忽略正则]
```

| 参数       | 必填 | 默认值    | 说明               |
| -------- | -- | ------ | ---------------- |
| `<源目录>`  | 是  | -      | 源目录路径            |
| `<目标目录>` | 是  | -      | 目标目录路径           |
| `[是否覆盖]` | 否  | `true` | `true` 或 `false` |
| `[忽略正则]` | 否  | 无      | 匹配的文件将被忽略        |

**示例:**
```lvt
# 将 src 下所有文件（含子目录）平铺到 build
flat "%project.src%" "%project.output%" true

# 平铺但忽略 .meta 文件
flat "./src/" "./build/" true "\.meta$"
```

`copy` 与 `flat` 的区别: `copy` 保留目录结构, `flat` 将所有文件放在同一目录下



## sh

执行外部 Shell 命令:

```
sh <可执行程序> [参数...]
```

**示例:**
```lvt
# 执行 git commit
sh git commit -m "Auto build commit"

# 执行 Python 脚本
sh python ./scripts/process.py
```

Shell 命令的输出会实时打印到终端, 执行完成后会显示耗时

> [!WARNING]
> `sh` 命令直接调用系统进程, 注意不同操作系统的命令差异



## verm

版本管理 (Levitate 版本):

```
verm smajor            # 主版本号 +1
verm sminor            # 次版本号 +1
verm sfix              # 修订号 +1
verm set <版本号>       # 直接设置版本号
verm get <深度>         # 获取版本号中指定深度的部分
```

**示例:**
```lvt
verm sfix
# 1.0.0 → 1.0.1

verm set 2.1.0
# 版本号设置为 2.1.0

var major_ver {verm get 2}
echo "主版本号: ^major_ver^"
```

> [!NOTE]
> 执行 `verm` 修改版本号后, 会自动更新环境变量 `project.ver`



## netfile

还原网络链接文件:

```
netfile restore [源目录] [输出目录]
# 或简写:
netfile r [源目录] [输出目录]
```

**示例:**
```lvt
# 从项目资源根目录还原
netfile restore

# 从指定目录还原
netfile restore "./downloads/" "./src/"
```



## check

条件检查 — 如果第一个参数为 `true`, 则执行后续方法:

```
check <条件> <方法名> [方法参数...]
```

**示例:**
```lvt
# 仅当目录存在时才执行删除
var is_dir_result {path isdir "%project.cache%"}
check ^is_dir_result^ delete "%project.cache%"

# 直接使用嵌套表达式
check {path isdir "%project.cache%"} delete "%project.cache%"
```

> [!NOTE]
> `check` 仅在第一个参数严格等于 `"true"` 时才执行后续方法



## path

路径工具:

```
path remapper <基准路径> <目标路径> <目标根> [新扩展名]    # 路径重映射
path dir <文件路径>                                      # 获取文件所在目录
path isdir <路径>                                        # 判断是否为目录
```

**示例:**
```lvt
# 路径重映射
var new_path {path remapper "./src/" "./src/assets/icon.png" "./build/" "jpg"}
# new_path 值为: ./build/assets/icon.jpg

# 获取目录
var dir_path {path dir "./src/assets/icon.png"}
echo "目录: ^dir_path^"

# 判断是否为目录
var is_dir {path isdir "./src/assets"}
check ^is_dir^ echo "assets 是一个目录"
```



## list

列表操作 — 使用 `|` 分隔的字符串模拟列表:

```
list get <列表字符串> <索引>           # 获取元素
list set <列表字符串> <索引> <新值>    # 设置元素
list add <列表字符串> <新元素>         # 添加元素
```

**示例:**
```lvt
var my_list "apple|banana|cherry"

# 获取元素
var first {list get ^my_list^ 0}
echo "第一个: ^first^"

# 设置元素
var my_list {list set ^my_list^ 1 "blueberry"}

# 添加元素
var my_list {list add ^my_list^ "date"}
```



## regex

正则表达式操作:

```
regex <正则表达式> match <测试字符串>              # 匹配检查
regex <正则表达式> replace <测试字符串> <替换内容>  # 替换
```

**示例:**
```lvt
# 匹配检查
var has_digit {regex "\d+" match "abc123"}
echo "包含数字: ^has_digit^"

# 替换
var result {regex "\s+" replace "hello   world" "-"}
echo "^result^"
# 输出: hello-world
```



## not

布尔取反:

```
not <true/false>
```

**示例:**
```lvt
var is_dir {path isdir "./src"}
var is_not_dir {not ^is_dir^}
check ^is_not_dir^ echo "src 不是目录"
```



## ifr

文件内文本替换 (In-File Replacer):

```
ifr <查找文本> <替换文本> <文件路径>
```

**示例:**
```lvt
# 替换文件中的版本号
ifr "1.0.0" "2.0.0" "./src/pack.mcmeta"

# 替换文件中的占位符
ifr "{{VERSION}}" "%project.ver%" "./src/README.txt"
```

> [!NOTE]
> 此为简单字符串替换, 非正则替换
>
> 如需正则替换请使用 `regex replace`



## pkgr

打包/解包工具, 支持 zip 和 tar 格式:

```
pkgr zip make   <源目录> <输出路径>     # 创建 zip 压缩包
pkgr zip tear   <压缩文件> <输出目录>    # 解压 zip 文件
pkgr tar make   <源目录> <输出路径>     # 创建 tar 归档
pkgr tar tear   <压缩文件> <输出目录>    # 解压 tar 文件
```

**示例:**
```lvt
# 创建 zip 包
pkgr zip make "%project.cache%" "%project.output%%project.name%_%project.ver%.zip"

# 解压 zip 文件
pkgr zip tear "./downloads/mod.zip" "./mods/"
```

> [!NOTE]
> `pkgr zip make` 要求源目录存在, 否则会报错
