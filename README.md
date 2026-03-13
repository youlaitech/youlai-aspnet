<div align="center">
  <img alt="logo" width="100" height="100" src="https://foruda.gitee.com/images/1733417239320800627/3c5290fe_716974.png">
  <h2>youlai-aspnet</h2>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-10-blueviolet.svg"/>
  <img alt="ASP.NET Core" src="https://img.shields.io/badge/ASP.NET Core-10-blue.svg"/>
  <a href="https://gitcode.com/youlai/youlai-aspnet" target="_blank">
    <img alt="GitCode star" src="https://gitcode.com/youlai/youlai-aspnet/star/badge.svg"/>
  </a>
  <a href="https://gitee.com/youlaiorg/youlai-aspnet" target="_blank">
    <img alt="Gitee star" src="https://gitee.com/youlaiorg/youlai-aspnet/badge/star.svg"/>
  </a>
  <a href="https://github.com/youlaitech/youlai-aspnet" target="_blank">
    <img alt="Github star" src="https://img.shields.io/github/stars/youlaitech/youlai-aspnet.svg?style=social&label=Stars"/>
  </a>
</div>

<p align="center">
  <a target="_blank" href="https://vue.youlai.tech/">🖥️ 在线预览</a>
  <span>&nbsp;|&nbsp;</span>
  <a target="_blank" href="https://www.youlai.tech/youlai-aspnet">📑 阅读文档</a>
  <span>&nbsp;|&nbsp;</span>
  <a target="_blank" href="https://www.youlai.tech">🌐 官网</a>
</p>

## 📢 项目简介

`youlai-aspnet` 是 `vue3-element-admin` 的 ASP.NET Core 10（.NET 10）后端实现，接口路径与返回结构完全对齐，可直接为前端提供后端服务。

- **🚀 最新技术栈**：.NET 10 + ASP.NET Core 10，LTS 版本支持至 2028 年 11 月。
- **🔐 安全认证**：JWT 无状态认证 + Redis 会话双模式。
- **🔑 权限管理**：RBAC 权限模型，菜单/按钮/接口统一治理。
- **🛠️ 模块能力**：用户、角色、菜单、部门、字典等核心模块开箱即用。

## 🌈 项目源码

| 项目类型 | Gitee | GitHub | GitCode |
| --- | --- | --- | --- |
| ✅ .NET 后端 | [youlai-aspnet](https://gitee.com/youlaiorg/youlai-aspnet) | [youlai-aspnet](https://github.com/youlaitech/youlai-aspnet) | [youlai-aspnet](https://gitcode.com/youlai/youlai-aspnet) |
| Vue3 管理端 | [vue3-element-admin](https://gitee.com/youlaiorg/vue3-element-admin) | [vue3-element-admin](https://github.com/youlaitech/vue3-element-admin) | [vue3-element-admin](https://gitcode.com/youlai/vue3-element-admin) |
| uni-app 移动端 | [vue-uniapp-template](https://gitee.com/youlaiorg/vue-uniapp-template) | [vue-uniapp-template](https://github.com/youlaitech/vue-uniapp-template) | [vue-uniapp-template](https://gitcode.com/youlai/vue-uniapp-template) |

## 📚 项目文档

| 文档名称 | 访问地址 |
| --- | --- |
| 项目介绍与使用指南 | [https://www.youlai.tech/youlai-aspnet](https://www.youlai.tech/youlai-aspnet) |

## 📁 项目目录

<details>
<summary>目录结构</summary>

```text
youlai-aspnet/
├─ src/                             # 源码目录
│  ├─ Youlai.Api/                   # Web API层（控制器、中间件、认证授权）
│  ├─ Youlai.Application/           # 应用层（服务接口定义、DTO、业务逻辑）
│  ├─ Youlai.Domain/                # 领域层（实体模型、领域对象、枚举）
│  └─ Youlai.Infrastructure/        # 基础设施层（EF Core、仓储、缓存）
├─ sql/                             # 数据库脚本
│  └─ mysql/                        # MySQL 脚本
├─ tests/                           # 测试项目
├─ youlai-aspnet.sln                # 解决方案文件
└─ global.json                      # .NET SDK版本锁定
```

</details>

## 🚀 快速启动

1. **基础环境**
   - .NET SDK 10（建议 10.0.100+）
   - MySQL 5.7+ / 8.x（必需）
   - Redis 7.x（必需）

2. **初始化数据库**

   执行 `sql/mysql/youlai_admin.sql` 脚本，完成库表与基础数据初始化。

3. **配置应用**

   `appsettings.json` 默认配置线上环境 `www.youlai.tech`，可直接启动。

   本地开发请修改 Database 和 Redis 连接地址为本地环境。

4. **启动后端**

   ```bash
   dotnet restore
   dotnet run --project src/Youlai.Api -c Release
   ```

   启动成功后访问 [http://localhost:8000/swagger](http://localhost:8000/swagger)。

## 🤝 前端整合

```bash
# 1. 获取前端项目
git clone https://gitee.com/youlaiorg/vue3-element-admin.git
cd vue3-element-admin

# 2. 安装依赖
pnpm install

# 3. 配置后端地址（.env.development）
VITE_APP_API_URL=http://localhost:8000

# 4. 启动前端
pnpm run dev
```

- **访问地址**：[http://localhost:3000](http://localhost:3000)
- **登录账号**：`admin` / `123456`

## 🚀 项目部署

**Windows 部署（IIS + Kestrel）**

1. 安装 IIS（控制面板 → 启用或关闭 Windows 功能 → 勾选 Internet Information Services）。
2. 安装 ASP.NET Core Hosting Bundle（官网下载安装后重启 IIS）。
3. 发布项目：

   ```bash
   dotnet publish src/Youlai.Api -c Release -r win-x64 --self-contained true -o ./publish
   ```

4. IIS 新建站点：物理路径指向 `publish`，端口设置为可用端口（如 8000），应用程序池选择“无托管代码”。

访问 `http://服务器IP:端口` 即可。

## 💖 技术交流

① 关注「有来技术」公众号，点击菜单 **交流群** 获取加群二维码（此举防止广告进群，感谢理解和支持）。

② 直接添加微信 **`haoxianrui`** 备注「前端/后端/全栈」。

![有来技术公众号](https://foruda.gitee.com/images/1737108820762592766/3390ed0d_716974.png)

**博客**：[CSDN](https://youlai.blog.csdn.net/) | [稀土掘金](https://juejin.cn/user/4187394044331261) | [博客园](https://www.cnblogs.com/haoxianrui) | [51CTO](https://blog.51cto.com/youlai) | [阿里云](https://developer.aliyun.com/profile/r6wxjk6qzasuy) | [腾讯云社区](https://cloud.tencent.com/developer/user/10500752) | [知乎](https://www.zhihu.com/people/haoxr)

**官网**：https://www.youlai.tech/

**代码仓库**：[Gitee](https://gitee.com/youlaiorg) | [AtomGit](https://atomgit.com/youlai) | [GitHub](https://github.com/youlaitech)
