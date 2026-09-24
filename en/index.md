---
titleTemplate: 'MessagesEncrypter'
description: 'MessagesEncrypter is a local public-key message encryption tool for the Windows desktop'
layout: home

hero:
    name: "MessagesEncrypter"
    tagline: "MessagesEncrypter is a local public-key message encryption tool for the Windows desktop"
---

## How to Download

<DownloadLinks microsoft-store="9pkn38fmmgbb" />

## Screenshots

**Message Encryption**

![MessagesEncrypter encryption page screenshot](/asset/encryption.png)

**Message Decryption**

![MessagesEncrypter decryption page screenshot](/asset/decryption.png)

## How to Use

1. The recipient generates a key pair on the **My Private Keys** page and shares the public key with the sender through any channel;
2. The sender imports the public key on the **Recipient Public Keys** page, selects it on the **Encrypt** page, and enters the plaintext to generate an encrypted package;
3. The sender sends the encrypted package to the recipient through their own channel (chat apps, email, etc.);
4. The recipient selects their private key on the **Decrypt** page, pastes the encrypted package, and enters the private key password to restore the plaintext.

![MessagesEncrypter guide](/asset/guide.png)

The software does not rely on any server: public keys are exchanged by the users themselves, and encrypted packages travel through your own channels. See the [Quick Start](/en/guide) page for a step-by-step walkthrough.

## Security Design

- **Hybrid encryption**: every encryption generates a fresh AES-256-GCM session key to encrypt the message body, which is then wrapped with the recipient's RSA public key (OAEP-SHA256);
- **Key strength**: supports generating 2048, 3072, 4096, and 8192-bit RSA keys (4096 by default); keys below 2048 bits are rejected;
- **Private key protection**: private keys are always stored as encrypted PKCS#8 PEM locally — never in plaintext; passwords can optionally be saved to Windows Credential Manager;
- **Integrity check**: the key store is protected by an HMAC-SHA256 integrity signature, verified at every startup with an immediate warning on tampering;
- **Fail-safe**: a tampered ciphertext always fails verification — decryption never outputs partial content.

For algorithm and format details, see the [Message Format](/en/protocol-v1) page.

## Open Source

- GitHub repository: <https://github.com/BlazeSnow/MessagesEncrypter>

## Copyright

Copyright © 2026 BlazeSnow. All rights reserved.

The software is released under the [GNU Affero General Public License v3.0](https://www.gnu.org/licenses/agpl-3.0.html).
