# 启动行动

启动行动是通过命令行参数直接触发的操作, 适合在 CI/CD 管道或批处理脚本中使用

您无需进入交互模式, 直接在终端中传入参数即可执行

---

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

---

## 预设启动别名

ShulkerRDK 内置了一些常用的启动别名, 您可以直接使用:

| 启动命令 | 等价于 |
| :--- | :--- |
| `./srdk task <任务名>` | `./srdk c task <任务名>` |
| `./srdk build` | `./srdk c task build` |
| `./srdk publish` | `./srdk c task publish` |
| `./srdk run` | `./srdk c task run` |

> [!NOTE]
> 这些别名实际指向的是 `./shulker/tasks/` 目录下对应的 `.lvt` 脚本, 如果脚本不存在, 执行会失败
>
> 另外 `dev` 只内置在交互模式的指令别名里, 命令行启动需写成 `./srdk c dev` 或 `./srdk task dev` (或者自己加一条 `alias.startAction.^dev`)

---

## 自定义别名

您也可以通过环境变量来自定义别名:

```bash
# 自定义启动别名, 也可以进入交互模式设定
./srdk c env set alias.startAction.^mybuild$ "c task build"

# 之后就可以直接使用:
./srdk mybuild
```

---

## 其他说明

- **CI/CD 非交互启动**: 只要存在 `./shulker/proj.json` 且传入了启动参数, 即使本地没有 `./shulker/local/shulker.json`, 程序也会自动跳过首次引导并隐藏下载进度条直接执行
- **调试本地扩展 (`-epp`)**: 开发扩展时可以用 `./srdk -epp <扩展DLL路径>` 临时多加载一个本地 `.dll` 进入交互模式
