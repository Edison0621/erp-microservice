using System.Security.Cryptography;
using System.Text;

namespace ErpSystem.BuildingBlocks.Common;

public static class GuidHelper
{
    /// <summary>
    /// Creates a deterministic GUID based on a string input using MD5.
    /// Useful for generating consistent IDs for aggregates based on natural keys.
    /// </summary>
    public static Guid CreateDeterministicGuid(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = MD5.HashData(inputBytes);
        return new Guid(hashBytes);
    }
}
