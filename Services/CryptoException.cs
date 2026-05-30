using System;

namespace MessagesEncrypter.Services;

public sealed class CryptoException : Exception
{
    public CryptoException(string resourceKey)
        : base(resourceKey)
    {
        ResourceKey = resourceKey;
    }

    public CryptoException(string resourceKey, Exception innerException)
        : base(resourceKey, innerException)
    {
        ResourceKey = resourceKey;
    }

    public string ResourceKey { get; }
}
