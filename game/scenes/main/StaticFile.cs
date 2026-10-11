namespace TheogonyX.Scenes.Main;

/// <summary>コンパニオンに配る静的ファイルの中身と Content-Type。</summary>
public sealed record StaticFile(byte[] Body, string ContentType);
