# MessagesEncrypter

## 一、程序介绍

本程序是 Windows 桌面端公钥加密工具，帮助用户使用接收方公钥加密消息，并使用自己的私钥解密收到的密文包。

程序支持管理多个接收方公钥和多个个人私钥。私钥密码保存到 Windows 凭据管理器，密钥存档保存在应用本地数据目录。

消息加密使用 RSA-4096 + OAEP-SHA256 加密 AES-256-GCM 会话密钥，再使用 AES-GCM 加密消息正文。

## 二、如何下载

即将上线 Microsoft Store。

## 三、软件截图

![main](./images/main.png)

## 四、使用说明

### 发送消息

1. 在“接收方公钥”中导入对方提供的公钥。
2. 在“消息加密”中选择接收方，输入要发送的消息。
3. 点击“加密”，复制生成的密文包。
4. 通过你信任的渠道把密文包发送给对方。

### 接收消息

1. 在“设置”中编辑并保存私钥密码。
2. 在“我的私钥”中生成或选择自己的加密私钥，并把对应公钥交给发送方。
3. 收到密文包后，在“消息解密”中选择自己的私钥并粘贴密文。
4. 点击“解密”，认证通过后查看明文。

## 五、开发者信息

<https://github.com/BlazeSnow>

项目仓库：<https://github.com/BlazeSnow/MessagesEncrypter>

项目官网：<https://www.blazesnow.com/messages/>

反馈邮箱：<messages@blazesnow.com>

## 六、版权信息

Copyright © 2026 BlazeSnow. 保留所有权利。

以GNU Affero General Public License v3.0的条款发布。

## 七、更新日志

更新日志见：<https://github.com/BlazeSnow/MessagesEncrypter/blob/main/CHANGELOG.md>
