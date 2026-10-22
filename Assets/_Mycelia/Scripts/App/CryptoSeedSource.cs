using System;
using System.Security.Cryptography;

namespace Mycelia.App
{
    /// <summary>A fresh 64-bit seed per level attempt from the system's cryptographic generator (ADR-0007).</summary>
    public sealed class CryptoSeedSource : ISeedSource
    {
        readonly RandomNumberGenerator generator = RandomNumberGenerator.Create();
        readonly byte[] buffer = new byte[sizeof(ulong)];

        public ulong Next()
        {
            generator.GetBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }
    }
}
