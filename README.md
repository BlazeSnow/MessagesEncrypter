# MessagesEncrypterWebsite

MessagesEncrypter 官方网站，基于 VitePress 构建，支持简体中文（根路径）与英文（`/en/`）双语。

线上地址：<https://messages.blazesnow.com/>

## 本地开发

```powershell
.\run.ps1     # 启动开发服务器（vitepress dev）
```

等价命令：

```bash
pnpm install
pnpm run docs:dev
```

## 构建与预览

```bash
pnpm run docs:build     # 构建到 .vitepress/dist
pnpm run docs:preview   # 本地预览构建产物
```

## 部署

站点部署在根路径 `messages.blazesnow.com`，`.vitepress/config/shared.ts` 中的 `base` 保持 `/`。构建配置见 [vercel.json](./vercel.json)，域名绑定在 Vercel 中管理。

## 发布

```powershell
.\tag.ps1   # 按 package.json 的 version 打 tag 并推送（tag 触发 Release 工作流）
```

## 目录结构

```
├── .github/workflows/   # CI：release.yml（tag 发布 Release）
├── .vitepress/
│   ├── config/          # 站点配置（shared / zh / en 双语言）
│   ├── custom/          # 自定义组件（DownloadLinks 微软商店徽章）
│   └── theme/           # 主题（品牌色 #0080FF）
├── en/                  # 英文页面
├── public/
│   ├── asset/           # 软件截图
│   ├── downloadlink/    # Microsoft Store 徽章（深浅色 × 中英文）
│   └── logo.ico         # 站点图标
├── index.md             # 中文首页
├── faq.md               # 中文常见问题
├── protocol.md          # 中文消息格式文档
└── changelog.md         # 软件更新日志
```

## 相关仓库

- 软件本体：<https://github.com/BlazeSnow/MessagesEncrypter>

## 许可证

本网站内容以 [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.html) 条款发布。
