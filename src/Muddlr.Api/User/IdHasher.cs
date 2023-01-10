using HashidsNet;

namespace Muddlr.Api;

internal class IdHasher
{
    private static readonly string HashSalt =
        Environment.GetEnvironmentVariable("muddlr__hashIdSalt") ?? "3eD>s0-93mfa;se";
    private static readonly Hashids InternalInstance = new(HashSalt);

    public static Hashids Instance => InternalInstance;
}