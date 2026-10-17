namespace TheogonyX.Core;

/// <summary>自動進行の速さ（docs/domain/time-control.md）。切り替えボタンで、この順に回す。1 倍の 1 年の秒数を基準に、2 倍・3 倍は 1 年の秒数を 2 分の 1・3 分の 1 にする。</summary>
public enum AutoPlaySpeed
{
    X1,
    X2,
    X3,
}
