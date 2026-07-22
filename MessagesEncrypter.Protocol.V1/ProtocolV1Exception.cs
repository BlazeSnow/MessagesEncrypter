namespace MessagesEncrypter.Protocol.V1;

public sealed class ProtocolV1Exception : Exception
{
    public ProtocolV1Exception(string resourceKey)
        : base(resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public ProtocolV1Exception(string resourceKey, Exception innerException)
        : base(resourceKey, innerException)
    {
        ResourceKey = resourceKey;
    }

    public string ResourceKey { get; }
}
