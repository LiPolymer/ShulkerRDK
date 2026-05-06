# 启动行动

启动行动是通过命令行参数直接触发的操作, 适合在 CI/CD 管道或批处理脚本中使用

您无需进入交互模式, 直接在终端中传入参数即可执行

***

## c

通过启动参数执行指令:

```bash
./srdk c <指令> [指令参数...]
```

**示例:**
```bash
# 执行帮助
./srdk c help c

# 查看项目信息
./srdk c proj i

# 设置环境变量
./srdk c env set build.mode release

# 执行 task 任务
./srdk c task build
```

***

## 预设启动别名

ShulkerRDK 内置了一些常用的启动别名, 您可以直接使用:

| 启动命令             | 等价于                     |
| ---------------- | ----------------------- |
| `./srdk build`   | `./srdk c task build`   |
| `./srdk publish` | `./srdk c task publish` |
| `./srdk run`     | `./srdk c task run`     |
| `./srdk dev`     | `./srdk c task dev`     |

> [!NOTE]
> 这些别名实际指向的是 `./shulker/tasks/` 目录下对应的 `.lvt` 脚本
>
> 如果脚本不存在, 执行会失败

***

## 自定义别名

您也可以通过环境变量来自定义别名:

```bash
# 自定义指令别名, 也可以进入交互模式设定
./srdk c env set alias.startAction.mybuild "task build "

# 之后就可以直接使用:
./srdk mybuild
```
