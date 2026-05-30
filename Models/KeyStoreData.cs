using System.Collections.Generic;

namespace MessagesEncrypter.Models;

public sealed class KeyStoreData
{
    public List<KeyEntry> RecipientKeys { get; set; } = [];

    public List<KeyEntry> PrivateKeys { get; set; } = [];
}
