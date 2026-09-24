# Quick Start

## 1. The message recipient generates a key pair

1. The message recipient goes to the **My Private Keys** page
2. Clicks **Generate Key**, enters and confirms the private key password
3. Once generated, the new key appears in the list with its alias, fingerprint, and key type

> The private key password is required to use the key — keep it safe. The app does not store the password (unless you check **Remember password**)

## 2. Exchange public keys

1. The message recipient copies (or exports) their public key on the **My Private Keys** page and sends it through any channel
2. The message sender clicks **Import Key** on the **Recipient Public Keys** page, pastes the public key text (or imports it from a file), and sets an alias

> Both parties are advised to verify that the key fingerprints match through a trusted channel (in person, a voice call, etc.) to confirm the public key has not been swapped

## 3. The message sender sends the message

1. The message sender goes to the **Encrypt** page and selects the message recipient's public key
2. Enters the plaintext and clicks **Encrypt** to generate the encrypted package
3. Sends the encrypted package to the message recipient through any channel

## 4. The message recipient decrypts the message

1. The message recipient goes to the **Decrypt** page and selects their own private key
2. Pastes the encrypted package, clicks **Decrypt**, enters the private key password, and gets the plaintext
