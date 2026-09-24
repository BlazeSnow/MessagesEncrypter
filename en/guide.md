# Quick Start

## System Requirements

- Windows 10 version 17763 or later
- x86, x64, or ARM64 architecture
- Distributed through the Microsoft Store; installing requires signing in with a Microsoft account

## Install

<DownloadLinks microsoft-store="9pkn38fmmgbb" />

## Your First Encrypted Message in Three Steps

Encrypted messaging involves two parties: the recipient holds the private key, and the sender holds the recipient's public key. In this example, Xiaohong is the recipient and Xiaoming is the sender.

### Step 1: The Recipient Generates a Key Pair

1. Xiaohong opens the app, goes to the **My Private Keys** page, and clicks **Generate Key**;
2. She sets a key alias, chooses the RSA key size (4096 by default), and enters and confirms the private key password;
3. Once generated, the new key appears in the list with its alias, fingerprint, and key type (e.g. RSA4096).

The private key password is required to use the key — keep it safe. The app does not store the password unless you check **Remember password**.

### Step 2: Exchange Public Keys

1. Xiaohong copies (or exports) her public key on the **My Private Keys** page and sends it to Xiaoming through any channel;
2. Xiaoming clicks **Import Key** on the **Recipient Public Keys** page, pastes the public key text (or imports it from a file), and sets an alias.

Both parties should verify the key fingerprints through a trusted channel (in person, a voice call, etc.) to confirm the public key has not been swapped.

### Step 3: Encrypt and Decrypt

Xiaoming (the sender):

1. Goes to the **Encrypt** page and selects Xiaohong's public key;
2. Enters the plaintext and clicks **Encrypt** to generate the encrypted package;
3. Clicks **Copy ciphertext** and sends the package to Xiaohong through any channel.

Xiaohong (the recipient):

1. Goes to the **Decrypt** page and selects her own private key;
2. Pastes the encrypted package, clicks **Decrypt**, and enters the private key password;
3. Reads the plaintext.

## Next Steps

- Check **Remember password** when decrypting — the password is saved to Windows Credential Manager so you won't need to enter it again;
- You can manage multiple recipient public keys and multiple private keys, useful for maintaining encrypted relationships with different contacts or identities;
- See the [User Guide](/en/usage) for detailed feature documentation;
- See the [FAQ](/en/faq) if you run into problems.
