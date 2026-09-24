---
titleTemplate: 'MessagesEncrypter'
description: 'MessagesEncrypter 是一款面向 Windows 桌面端的本地公钥消息加密工具'
layout: home

hero:
    name: "MessagesEncrypter"
    tagline: "MessagesEncrypter 是一款面向 Windows 桌面端的本地公钥消息加密工具"
---

## 如何下载

<DownloadLinks microsoft-store="9pkn38fmmgbb" />

## 软件截图

**消息加密**

![MessagesEncrypter 消息加密页面截图](/asset/encryption.png)

**消息解密**

![MessagesEncrypter 消息解密页面截图](/asset/decryption.png)

## 使用说明

1. 接收方在「我的私钥」页生成密钥对，并将公钥通过任意渠道分享给发送方；
2. 发送方在「接收方公钥」页导入公钥，在「消息加密」页选中该公钥并输入明文，生成密文包；
3. 发送方将密文包通过自己的渠道（聊天软件、邮件等）发送给接收方；
4. 接收方在「消息解密」页选中自己的私钥，粘贴密文包并输入私钥密码，即可还原明文。

![MessagesEncrypter 使用说明](/asset/guide.png)

软件不依赖任何服务器：公钥由用户自行交换，密文包通过您自己的渠道传递。分步入门详见[快速开始](/guide)页面。

## 安全设计

- **混合加密**：每次加密都随机生成新的 AES-256-GCM 会话密钥加密消息正文，再用接收方 RSA 公钥（OAEP-SHA256）加密会话密钥；
- **密钥强度**：支持生成 2048、3072、4096、8192 位 RSA 密钥（默认 4096），拒绝接受低于 2048 位的密钥；
- **私钥保护**：私钥始终以加密 PKCS#8 PEM 存储在本地，绝不以明文落盘；密码可按需保存到 Windows 凭据管理器；
- **完整性校验**：密钥库配有 HMAC-SHA256 完整性签名，启动时自动校验，发现篡改立即告警；
- **失败安全**：密文被篡改时校验必然失败，解密不会输出任何部分内容。

更多算法与格式细节见[消息格式](/protocol-v1)页面。

## 开源

- GitHub 仓库：<https://github.com/BlazeSnow/MessagesEncrypter>

## 版权信息

Copyright © 2026 BlazeSnow. 保留所有权利。

软件以 [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.html) 条款发布。
