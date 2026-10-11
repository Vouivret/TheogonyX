namespace TheogonyX.Core;

/// <summary>予言者の夢の状態（docs/domain/dream-tampering.md）。</summary>
public enum ProphetDreamState
{
    /// <summary>夢なし（神託を下した後）。</summary>
    None,

    /// <summary>夢があり、まだ改ざんしていない。</summary>
    Untampered,

    /// <summary>夢があり、改ざんした。</summary>
    Tampered,
}
