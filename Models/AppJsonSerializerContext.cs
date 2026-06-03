using System.Text.Json;
using System.Text.Json.Serialization;

namespace MessagesEncrypter.Models;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(EncryptedMessagePackage))]
[JsonSerializable(typeof(KeyEntry))]
[JsonSerializable(typeof(KeyStoreData))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
