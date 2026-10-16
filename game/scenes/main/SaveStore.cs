using System;
using System.Globalization;
using System.IO;
using Godot;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// セーブのファイルを読み書きする（docs/domain/save-data.md）。中身の形と検証はコアの <c>SaveDataSerializer</c> が受け持つ。
/// 書き込みは一時ファイルに書いてから置き換え、途中で終了しても前のセーブを壊さない。
/// </summary>
public sealed class SaveStore
{
    private const string FileName = "world.json";

    private readonly string _directory;
    private readonly TimeProvider _timeProvider;

    // 同じ理由の失敗が続くときは、成功するまで 1 回だけ記録する
    private string? _lastFailureReason;

    /// <param name="directory">置き場所（<c>user://</c> で始まる Godot のパス）。</param>
    public SaveStore(string directory, TimeProvider timeProvider)
    {
        _directory = ProjectSettings.GlobalizePath(directory);
        _timeProvider = timeProvider;
    }

    /// <summary>保存するか。セーブを読めなかった（権限など）起動では、上書きしないよう false にする。</summary>
    public bool IsEnabled { get; private set; } = true;

    private string FilePath => Path.Combine(_directory, FileName);

    /// <returns>セーブの中身。セーブが無ければ null。読めなかったら null を返し、この起動では保存しない。</returns>
    public string? Load()
    {
        try
        {
            return File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"event=save_load_failed reason=read_failed error={ex.GetType().Name}");
            IsEnabled = false;
            return null;
        }
    }

    /// <summary>壊れていたセーブを <c>world.broken-&lt;日時&gt;.json</c> に名前を変えて残す。できなければ、この起動では保存しない。</summary>
    public void KeepBroken()
    {
        var stamp = _timeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        try
        {
            File.Move(FilePath, Path.Combine(_directory, $"world.broken-{stamp}.json"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"event=save_load_failed reason=keep_broken_failed error={ex.GetType().Name}");
            IsEnabled = false;
        }
    }

    /// <summary>前の版のセーブを <c>world.old-&lt;日時&gt;.json</c> に名前を変えて残す。できなければ、この起動では保存しない。</summary>
    public void KeepOutdated()
    {
        var stamp = _timeProvider.GetUtcNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        try
        {
            File.Move(FilePath, Path.Combine(_directory, $"world.old-{stamp}.json"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            GD.PushError($"event=save_load_failed reason=keep_outdated_failed error={ex.GetType().Name}");
            IsEnabled = false;
        }
    }

    public void Save(string json)
    {
        if (!IsEnabled)
        {
            return;
        }

        var temporary = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(temporary, json);
            File.Move(temporary, FilePath, overwrite: true);
            _lastFailureReason = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var error = ex.GetType().Name;
            if (error != _lastFailureReason)
            {
                GD.PushWarning($"event=save_failed reason=write_failed error={error}");
                _lastFailureReason = error;
            }
        }
    }
}
