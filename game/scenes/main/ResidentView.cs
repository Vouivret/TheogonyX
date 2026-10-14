using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using TheogonyX.Core;

namespace TheogonyX.Scenes.Main;

/// <summary>
/// 住人と墓を地表の上に描く（docs/domain/agent-needs.md の「画面」）。住人は群衆の点より大きい 7 種類の絵（住人の ID で選ぶ）で、
/// 昼はその年のタスクに合わせて地域の中を歩き、夜は地域の建物のそば（建物が無ければその場）で横になる。
/// 歩き方は描画だけで、シミュレーションの結果に影響しない（乱数はコアの計算と別の <see cref="CrowdHash"/> を使う）。
/// 子供（大人の年齢より若い住人）は小さく描き、つがいは夜に同じ建物のそばで並んで寝る。
/// 住人をクリックすると、名前・パートナー・欲求・体力・SAN 値の欄を出す。
/// </summary>
public sealed class ResidentView
{
    private const int DollWidth = 10;
    private const int DollHeight = 14;
    private const float WalkSpeed = 8f; // 1 秒に進む px
    private const float RampageSpeedFactor = 3f;
    private const float PauseSeconds = 1.5f;
    private const float ArrivedDistance = 0.5f;

    private const int DollKinds = 7;
    private const float ChildScale = 0.7f;
    private const float CoupleGap = 3f; // 並んで寝るつがいの、建物のそばの点からのずれ（px）

    // 精神崩壊中の住人は、絵に色を掛けて見分ける（狂乱は赤く、自閉・絶望は灰色に）
    private static readonly Color RampageTint = new(1f, 0.45f, 0.45f);
    private static readonly Color CatatoniaTint = new(0.55f, 0.55f, 0.6f);
    private static readonly Color Outline = Color.FromHtml("#140c1c");
    private static readonly Color Stone = Color.FromHtml("#8595a1");

    private readonly TimeControl _timeControl;
    private readonly CivilizationRules _rules;
    private readonly NeedsRules _needs;
    private readonly AmbienceView _ambience;
    private readonly uint _seed;
    private readonly SubViewport _viewport;
    private readonly Node2D _layer;
    private readonly Vector2 _terrainOrigin;
    private readonly Dictionary<int, Walker> _walkers = [];
    private readonly Dictionary<int, Sprite2D> _graves = [];
    private readonly Texture2D[] _dolls;
    private readonly Texture2D _graveTexture;
    private readonly PanelContainer _infoPanel;
    private readonly Label _infoLabel;
    private int? _selectedId;

    /// <param name="owner">ノードを探すためのシーンの根。</param>
    public ResidentView(Node owner, TimeControl timeControl, CivilizationRules rules, NeedsRules needs, AmbienceView ambience, int seed)
    {
        _timeControl = timeControl;
        _rules = rules;
        _needs = needs;
        _ambience = ambience;
        _seed = unchecked((uint)seed);
        _viewport = owner.GetNode<SubViewport>("%WorldViewport");
        _terrainOrigin = owner.GetNode<TileMapLayer>("%Terrain").Position;
        // 予言者・建物と同じ Y ソートのレイヤーに置く（地下の表示では、このレイヤーごと隠れる）
        _layer = new Node2D { Name = "Residents", YSortEnabled = true };
        owner.GetNode<Sprite2D>("%Prophet").GetParent().AddChild(_layer);
        _dolls = [.. Enumerable.Range(1, DollKinds).Select(kind => GD.Load<Texture2D>($"res://assets/sprites/resident_{kind}.png"))];
        _graveTexture = CreateGrave();

        _infoLabel = new Label { Name = "ResidentInfo" };
        _infoPanel = new PanelContainer
        {
            Name = "ResidentPanel",
            Visible = false,
            Position = new Vector2(16, 60),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _infoPanel.AddChild(_infoLabel);
        owner.GetNode<Node>("Hud").AddChild(_infoPanel);
        Refresh();
    }

    /// <summary>地表を描き直した後に呼ぶ（年が進んだ・リセットした・読み込んだ）。住人と墓の一覧に合わせる。</summary>
    public void Refresh(TileType[,] _) => Refresh();

    /// <summary>毎フレーム呼ぶ。住人を歩かせる。星が崩壊していれば止める。</summary>
    public void Process(double delta)
    {
        if (_timeControl.World.IsDestroyed)
        {
            return;
        }
        var night = _ambience.IsNight;
        var buildings = _timeControl.Civilization?.Buildings ?? [];
        foreach (var walker in _walkers.Values)
        {
            walker.Update((float)delta, night, buildings, this);
        }
    }

    /// <summary>HUD が受け取らなかった入力。住人をクリックしたら欄を出して true を返す（カメラのドラッグに渡さない）。</summary>
    public bool HandleInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } button || !_layer.IsVisibleInTree())
        {
            return false;
        }
        var point = _viewport.GetCanvasTransform().AffineInverse() * button.Position;
        var hit = _walkers.Values
            .Where(walker =>
            {
                var size = new Vector2(DollWidth, DollHeight) * walker.Node.Scale;
                return new Rect2(walker.Node.Position - new Vector2(size.X / 2f, size.Y), size).HasPoint(point);
            })
            .OrderBy(walker => walker.Node.Position.DistanceSquaredTo(point))
            .FirstOrDefault();
        _selectedId = hit?.Resident.Id;
        ShowInfo();
        return hit is not null;
    }

    private void Refresh()
    {
        var residents = _timeControl.Residents;
        var people = residents?.People ?? [];
        var alive = people.Select(person => person.Id).ToHashSet();
        foreach (var id in _walkers.Keys.Where(id => !alive.Contains(id)).ToList())
        {
            _walkers[id].Node.QueueFree();
            _walkers.Remove(id);
        }
        foreach (var person in people)
        {
            if (!_walkers.TryGetValue(person.Id, out var walker))
            {
                var sprite = new Sprite2D
                {
                    Name = $"Resident{person.Id}",
                    Centered = false,
                    Offset = new Vector2(-DollWidth / 2, -DollHeight),
                    Position = RandomPointIn(person.Region, (uint)person.Id, 0),
                };
                _layer.AddChild(sprite);
                walker = new Walker(sprite, person);
                _walkers[person.Id] = walker;
            }
            walker.Resident = person;
            walker.Node.Texture = _dolls[person.Id % DollKinds];
            walker.Node.Scale = Vector2.One * (person.AgeIn(_timeControl.World.Year) < _needs.AdultAge ? ChildScale : 1f);
            walker.Node.Modulate = person.Breakdown switch
            {
                BreakdownKind.Rampage => RampageTint,
                BreakdownKind.Catatonia => CatatoniaTint,
                _ => Colors.White,
            };
        }

        var graves = residents?.Graves ?? [];
        var shownGraves = graves.Select(grave => grave.ResidentId).ToHashSet();
        foreach (var id in _graves.Keys.Where(id => !shownGraves.Contains(id)).ToList())
        {
            _graves[id].QueueFree();
            _graves.Remove(id);
        }
        foreach (var grave in graves.Where(grave => !_graves.ContainsKey(grave.ResidentId)))
        {
            var sprite = new Sprite2D
            {
                Name = $"Grave{grave.ResidentId}",
                Texture = _graveTexture,
                Centered = false,
                Offset = new Vector2(-_graveTexture.GetWidth() / 2, -_graveTexture.GetHeight()),
                Position = RandomPointIn(grave.Region, (uint)grave.ResidentId, uint.MaxValue),
            };
            _layer.AddChild(sprite);
            _graves[grave.ResidentId] = sprite;
        }

        if (_selectedId is { } selected && !alive.Contains(selected))
        {
            _selectedId = null;
        }
        ShowInfo();
    }

    /// <summary>住人の絵を出す・隠す（視点で切り替える）。隠すとき、情報の枠も閉じる。</summary>
    public void SetShown(bool shown)
    {
        _layer.Visible = shown;
        if (!shown)
        {
            _infoPanel.Visible = false;
        }
    }

    private void ShowInfo()
    {
        if (_selectedId is not { } id || !_walkers.TryGetValue(id, out var walker))
        {
            _infoPanel.Visible = false;
            return;
        }
        var person = walker.Resident;
        var culture = CultureInfo.CurrentCulture;
        var partner = person.PartnerId is { } partnerId && _walkers.TryGetValue(partnerId, out var other)
            ? string.Format(culture, Tr("RESIDENT_INFO_PARTNER"), ResidentText.NameOf(other.Resident.Name, other.Resident.Epithet))
            : Tr("RESIDENT_INFO_SINGLE");
        _infoLabel.Text = string.Join('\n',
            ResidentText.NameOf(person.Name, person.Epithet),
            partner,
            string.Format(culture, Tr("RESIDENT_INFO_LOWER_NEEDS"), person.Hunger, person.Shelter),
            string.Format(culture, Tr("RESIDENT_INFO_UPPER_NEEDS"), person.Social, person.Esteem, person.Actualization),
            string.Format(culture, Tr("RESIDENT_INFO_BODY"), person.Health, person.Sanity));
        _infoPanel.Visible = true;
    }

    // 地域の中の点（足元）。列は地域の幅、行は地表の高さの中から、ハッシュで選ぶ
    private Vector2 RandomPointIn(int region, uint index, uint step)
    {
        var size = WorldScreen.TileSize;
        var left = _rules.Regions.FirstColumn(region) * size;
        var width = _rules.Regions.ColumnsPerRegion * size;
        var x = left + (2 + (CrowdHash.Next01(_seed, index, step * 2) * (width - 4)));
        var y = DollHeight + (CrowdHash.Next01(_seed, index, (step * 2) + 1) * ((_rules.Rows * size) - DollHeight));
        return _terrainOrigin + new Vector2(Mathf.Floor(x), Mathf.Floor(y));
    }

    private Vector2 CenterOf(int region)
    {
        var size = WorldScreen.TileSize;
        var x = (_rules.Regions.FirstColumn(region) + (_rules.Regions.ColumnsPerRegion / 2f)) * size;
        return _terrainOrigin + new Vector2(x, _rules.Rows * size / 2f);
    }

    // 建物の足元の少し手前（建物の絵と重ならないよう下にずらす）
    private Vector2 BesideBuilding(Building building, uint index, uint step) =>
        BuildingView.FootPosition(_terrainOrigin, building.Column, building.Row)
        + new Vector2(Mathf.Floor((CrowdHash.Next01(_seed, index, step) - 0.5f) * 24f), 3f);

    private static string Tr(string key) => TranslationServer.Translate(key).ToString();

    private static Texture2D CreateGrave()
    {
        var image = Image.CreateEmpty(6, 8, false, Image.Format.Rgba8);
        image.FillRect(new Rect2I(0, 1, 6, 7), Outline);
        image.FillRect(new Rect2I(1, 0, 4, 1), Outline);
        image.FillRect(new Rect2I(1, 1, 4, 6), Stone);
        image.FillRect(new Rect2I(2, 2, 2, 1), Outline); // 十字の横棒
        image.FillRect(new Rect2I(2, 2, 1, 3), Outline);
        return ImageTexture.CreateFromImage(image);
    }

    // 1 人の住人の歩き方。昼は目的地へ歩いては立ち止まり、夜は建物のそばへ行って横になる
    private sealed class Walker(Sprite2D node, Resident resident)
    {
        private Vector2? _target;
        private float _pause;
        private uint _step;
        private bool _wasNight;

        public Sprite2D Node { get; } = node;

        public Resident Resident { get; set; } = resident;

        public void Update(float delta, bool night, IReadOnlyList<Building> buildings, ResidentView view)
        {
            if (night != _wasNight)
            {
                _wasNight = night;
                _target = null;
                _pause = 0f;
                Node.Rotation = 0f;
            }
            if (Resident.Breakdown == BreakdownKind.Catatonia && !night)
            {
                return; // 自閉・絶望の住人は昼もその場から動かない
            }
            if (_pause > 0f)
            {
                _pause -= delta;
                return;
            }

            _target ??= night ? NightSpot(buildings, view) : DaySpot(buildings, view);
            var speed = WalkSpeed * (Resident.Breakdown == BreakdownKind.Rampage ? RampageSpeedFactor : 1f);
            Node.Position = Node.Position.MoveToward(_target.Value, speed * delta);
            if (Node.Position.DistanceTo(_target.Value) > ArrivedDistance)
            {
                return;
            }
            if (night)
            {
                Node.Rotation = Mathf.Pi / 2f; // 横になる。夜が明けるまでこのまま
                _pause = float.MaxValue;
                return;
            }
            _target = null;
            _step++;
            _pause = PauseSeconds * (0.5f + CrowdHash.Next01(view._seed, (uint)Resident.Id, _step + 0x8000));
        }

        // その年のタスクの場所。祠・工房のタスクはその建物、集まるなら地域の真ん中、ほかは地域の中のどこか
        private Vector2 DaySpot(IReadOnlyList<Building> buildings, ResidentView view)
        {
            var id = (uint)Resident.Id;
            var type = Resident.Task switch
            {
                ResidentTask.Pray => BuildingType.Shrine,
                ResidentTask.Research => BuildingType.Workshop,
                _ => (BuildingType?)null,
            };
            var places = type is null ? [] : buildings.Where(building => building.Region == Resident.Region && building.Type == type).ToList();
            if (places.Count > 0)
            {
                return view.BesideBuilding(places[(int)(CrowdHash.Hash(view._seed, id, _step) % (uint)places.Count)], id, _step + 1);
            }
            if (Resident.Task == ResidentTask.Gather)
            {
                return view.CenterOf(Resident.Region) + new Vector2((CrowdHash.Next01(view._seed, id, _step) - 0.5f) * 16f, (CrowdHash.Next01(view._seed, id, _step + 1) - 0.5f) * 16f);
            }
            return view.RandomPointIn(Resident.Region, id, _step + 1);
        }

        // 地域の建物のうち近いもののそば。建物が無ければその場。
        // つがいは、ID の小さいほうで決めた同じ建物のそばに、左右に並んで寝る（建物が無ければ、ID の小さいほうの寝る点の左右）
        private Vector2 NightSpot(IReadOnlyList<Building> buildings, ResidentView view)
        {
            if (Resident.PartnerId is { } partnerId && view._walkers.TryGetValue(partnerId, out var partner))
            {
                var anchor = Resident.Id < partnerId ? this : partner;
                var side = new Vector2(Resident.Id < partnerId ? -CoupleGap : CoupleGap, 0f);
                var regionBuildings = buildings.Where(building => building.Region == Resident.Region).OrderBy(building => building.Id).ToList();
                var anchorId = (uint)anchor.Resident.Id;
                return regionBuildings.Count == 0
                    ? anchor.Node.Position + side
                    : view.BesideBuilding(regionBuildings[(int)(CrowdHash.Hash(view._seed, anchorId, 0x4000) % (uint)regionBuildings.Count)], anchorId, 0x4000) + side;
            }
            var here = Node.Position;
            var nearest = buildings
                .Where(building => building.Region == Resident.Region)
                .OrderBy(building => BuildingView.FootPosition(view._terrainOrigin, building.Column, building.Row).DistanceSquaredTo(here))
                .FirstOrDefault();
            return nearest is null ? here : view.BesideBuilding(nearest, (uint)Resident.Id, 0x4000);
        }
    }
}
