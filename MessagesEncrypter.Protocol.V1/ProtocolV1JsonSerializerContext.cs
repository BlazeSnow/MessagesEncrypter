using System.Text.Json;
using System.Text.Json.Serialization;

namespace MessagesEncrypter.Protocol.V1;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(EncryptedMessagePackage))]
internal sealed partial class ProtocolV1JsonSerializerContext : JsonSerializerContext;
