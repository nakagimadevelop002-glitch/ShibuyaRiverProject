# HerdSimアセット 徹底解析レポート

**作成日**: 2025-12-23
**対象アセット**: Unluck Software - HerdSim (Animal Rat)
**解析者**: Claude Code

---

## 📑 目次

1. [概要](#概要)
2. [アーキテクチャ設計](#アーキテクチャ設計)
3. [パフォーマンス最適化手法](#パフォーマンス最適化手法)
4. [AI行動ロジック](#ai行動ロジック)
5. [Raycast衝突回避システム](#raycast衝突回避システム)
6. [その他の優れたテクニック](#その他の優れたテクニック)
7. [学ぶべきテクニック Top 10](#学ぶべきテクニック-top-10)
8. [実戦投入時の注意点](#実戦投入時の注意点)
9. [プロジェクトへの応用例](#プロジェクトへの応用例)
10. [ボーナス：カメラ制御テクニック](#ボーナスカメラ制御テクニック)
11. [ボーナス：Unityエディタ拡張テクニック](#ボーナスunityエディタ拡張テクニック)

---

## 概要

HerdSimは、**NavMeshやRigidbodyを使わずに**動物の群れシミュレーションを実現する軽量アセットです。

### 主な特徴

- ✅ **NavMesh不要** - Raycastベースの独自パスファインディング
- ✅ **Rigidbody不要** - 物理演算コストゼロ
- ✅ **群れ形成** - 動的なリーダーシップシステム
- ✅ **地形追従** - 地面の法線から自動的に傾く
- ✅ **軽量動作** - 100体以上の同時処理が可能

### ファイル構成

```
Assets/Unluck Software/HerdSim/
├── Scripts/
│   ├── HerdSimCore.cs          (メインロジック - 892行)
│   ├── HerdSimController.cs    (エリア管理 - 16行)
│   ├── HerdSimScary.cs         (恐怖刺激 - 63行)
│   └── HerdSimDisabler.cs      (距離最適化 - 44行)
├── Animal Rat/
│   ├── Prefabs/
│   ├── Materials/
│   └── Models Generic/
└── Materials/
```

---

## アーキテクチャ設計

### クラス関係図

```
┌─────────────────────────┐
│ HerdSimController       │  (全体管理)
│ ・エリア定義            │
│ ・パーティクル管理      │
└──────────┬──────────────┘
           │
           │ 参照
           ▼
┌─────────────────────────┐
│ HerdSimCore             │  (メインAI)
│ ・移動/回転/衝突回避    │
│ ・群れ形成              │◄──┐
│ ・AI行動決定            │   │
│ ・アニメーション制御    │   │ 相互参照
└──────────┬──────────────┘   │ (群れ検索)
           │                   │
           │ アタッチ          │
           ▼                   │
┌─────────────────────────┐   │
│ HerdSimScary            │   │
│ ・恐怖刺激発生          ├───┘
└─────────────────────────┘
           │
           │ アタッチ
           ▼
┌─────────────────────────┐
│ HerdSimDisabler         │
│ ・距離ベース無効化      │
└─────────────────────────┘
```

### 🎯 学習ポイント：関心の分離

各クラスが明確な**単一責任**を持つ設計：

| クラス | 責任 |
|--------|------|
| `HerdSimController` | 全体エリア管理、パーティクル共有 |
| `HerdSimCore` | AI行動、移動、回転、衝突回避、群れ形成 |
| `HerdSimScary` | 恐怖刺激の発生、追跡行動 |
| `HerdSimDisabler` | 距離ベースのLOD（最適化） |

拡張機能（Scary, Disabler）は**独立したコンポーネント**として実装され、必要に応じてアタッチ可能。

---

## パフォーマンス最適化手法

HerdSimの最大の強みは**パフォーマンス最適化**にあります。100体以上の動物を同時に動かすための工夫が随所に見られます。

---

### 1. フレームスキップシステム ⭐⭐⭐

**場所**: `HerdSimCore.cs:627-637`

#### 仕組み

```csharp
public int _updateDivisor = 1;  // 2-4に設定すると、N回に1回だけUpdate実行
static int _updateNextSeed = 0;  // 全個体でUpdateタイミングをずらす
int _updateSeed = -1;
int _updateCounter;
float _newDelta;

void Update() {
    // フレームスキップ処理
    if (_updateDivisor > 1) {
        _updateCounter++;
        if (_updateCounter != _updateSeed) {
            _updateCounter = _updateCounter % _updateDivisor;
            return;  // スキップ
        }
        _updateCounter = _updateCounter % _updateDivisor;
        _newDelta = Time.deltaTime * _updateDivisor;  // スキップ分を補正
    } else {
        _newDelta = Time.deltaTime;
    }

    // 以降の処理は_newDeltaを使用
}
```

#### 初期化（Start内）

```csharp
if (_updateDivisor > 1) {
    int _updateSeedCap = _updateDivisor - 1;
    _updateNextSeed++;
    this._updateSeed = _updateNextSeed;
    _updateNextSeed = _updateNextSeed % _updateSeedCap;
}
```

#### 🎯 学習ポイント

1. **タイミング分散**
   - 全個体が同じフレームで処理されないよう、Seed値でタイミングを分散
   - 個体Aはフレーム0,3,6...、個体Bはフレーム1,4,7...、個体Cはフレーム2,5,8...

2. **時間補正**
   - `_newDelta = Time.deltaTime * _updateDivisor`でスキップ分を補正
   - 滑らかな動きを維持したまま処理頻度を削減

3. **効果**
   - 100体の動物 × フレームスキップ3 = **CPU負荷を約1/3に削減**

#### 応用例

```csharp
// 60FPSゲームで推奨設定
_updateDivisor = 3;  // 20FPS相当で更新（人間の目には十分滑らか）

// 30FPSゲームで推奨設定
_updateDivisor = 2;  // 15FPS相当で更新
```

---

### 2. 距離ベースのLOD（Level of Detail） ⭐⭐

**場所**: `HerdSimDisabler.cs`

#### 仕組み

```csharp
public int _distanceDisable = 1000;
public Transform _distanceFrom;
public bool _distanceFromMainCam;  // Trueならメインカメラからの距離

public void Start() {
    if (_distanceFromMainCam) {
        _distanceFrom = Camera.main.transform;
    }

    InvokeRepeating("CheckDisable", _checkDisableEverSeconds + Random.value * _checkDisableEverSeconds, _checkDisableEverSeconds);
    InvokeRepeating("CheckEnable", _checkEnableEverSeconds + Random.value * _checkEnableEverSeconds, _checkEnableEverSeconds);
}

public void CheckDisable() {
    if (_distanceFrom != null
        && transform.GetComponent<HerdSimCore>()._enabled
        && (transform.position - _distanceFrom.position).sqrMagnitude > _distanceDisable) {
        transform.GetComponent<HerdSimCore>().Disable(_disableModel, _disableCollider);
    }
}

public void CheckEnable() {
    if (_distanceFrom != null
        && !transform.GetComponent<HerdSimCore>()._enabled
        && (transform.position - _distanceFrom.position).sqrMagnitude < _distanceDisable) {
        transform.GetComponent<HerdSimCore>().Enable();
    }
}
```

#### 🎯 学習ポイント

1. **sqrMagnitude使用**
   - `Vector3.Distance()`ではなく`sqrMagnitude`を使用
   - 平方根計算を省略 → **約2倍高速**

2. **個別無効化**
   - `_disableModel` - モデル（Renderer）を非表示
   - `_disableCollider` - コライダーを無効化
   - 用途に応じて柔軟に設定可能

3. **InvokeRepeating活用**
   - 毎フレームチェック不要 → 10秒に1回（Disable）、1秒に1回（Enable）
   - `Random.value`で開始タイミングを分散 → 全個体が同時処理を回避

#### 性能比較

```csharp
// ❌ 遅い方法
float dist = Vector3.Distance(transform.position, _distanceFrom.position);
if (dist > 100f) { ... }

// ✅ 高速な方法
if ((transform.position - _distanceFrom.position).sqrMagnitude > 10000f) { ... }
// 100^2 = 10000
```

---

### 3. InvokeRepeating活用 ⭐⭐

**場所**: `HerdSimCore.cs:444-456`

#### 仕組み

```csharp
void Init() {
    if (_controller != null) {
        // パーティクルエフェクト発生（0.1秒間隔）
        InvokeRepeating("Effects", 1 + Random.value, .1f);
    }

    // 行動決定（1秒間隔）
    InvokeRepeating("Wander", 1 + Random.value, 1.0f);

    // 地面検知（_groundCheckInterval秒間隔、デフォルト0.1秒）
    InvokeRepeating("GroundCheck", (_groundCheckInterval * Random.value) + 1, _groundCheckInterval);

    // 群れリーダー検索（3秒間隔）
    InvokeRepeating("FindLeader", Random.value * 3, 3.0f);
}
```

#### 🎯 学習ポイント

1. **処理頻度の最適化**

| 処理 | 頻度 | 理由 |
|------|------|------|
| `Effects` | 0.1秒 | パーティクル発生は高頻度が必要 |
| `Wander` | 1秒 | 行動決定は毎フレーム不要 |
| `GroundCheck` | 0.1秒 | Raycastは重いので間引き |
| `FindLeader` | 3秒 | 群れ検索は低頻度で十分 |

2. **開始タイミング分散**
   - `Random.value`で開始タイミングをずらす
   - 全個体が同時にRaycast → CPU負荷スパイクを回避

#### 効果

```
毎フレームGroundCheck（60FPS）: 100体 × 60回/秒 = 6000 Raycast/秒
InvokeRepeating(0.1秒):        100体 × 10回/秒 = 1000 Raycast/秒

→ 約6倍の負荷削減
```

---

### 4. パーティクルシステムの一元管理 ⭐⭐

**場所**: `HerdSimCore.cs:294-302`, `HerdSimController.cs:9-10`

#### 仕組み

**HerdSimController.cs**
```csharp
public class HerdSimController : MonoBehaviour {
    public Vector3 _roamingArea;
    public ParticleSystem _runPS;    // 走行時のダストパーティクル（共有）
    public ParticleSystem _deadPS;   // 死亡時のパーティクル（共有）
}
```

**HerdSimCore.cs**
```csharp
public HerdSimController _controller;  // コントローラー参照

public void Effects() {
    // 走行中のダストエフェクト
    if ((_controller != null) && _mode == 2 && (_controller._runPS != null) && _speed > 1) {
        _controller._runPS.transform.position = this._thisTR.position;
        _controller._runPS.Emit(1);  // 1粒子だけ発生
    }

    // 死亡時のエフェクト
    if (_dead && (_controller != null) && (_controller._deadPS != null)) {
        _controller._deadPS.transform.position = _collider.transform.position;
        _controller._deadPS.Emit(1);
    }
}
```

#### 🎯 学習ポイント

1. **DrawCall削減**
   - 各動物が独自のParticleSystemを持たない
   - コントローラーが1つのParticleSystemを共有
   - **100体のDrawCallが1つに集約**

2. **Emit(1)の活用**
   - `ParticleSystem.Play()`ではなく`Emit(1)`
   - 必要な時だけ1粒子ずつ発生 → メモリ効率良好

#### DrawCall比較

```
個別ParticleSystem方式: 100体 × 2種類 = 200 DrawCall
共有ParticleSystem方式: 2種類のみ = 2 DrawCall

→ 100倍のDrawCall削減
```

---

### 5. Rigidbodyを使わない衝突システム ⭐⭐⭐

**場所**: `HerdSimCore.cs:582-608`

#### 仕組み

```csharp
public Transform _scanner;  // 回転するスキャナーオブジェクト
public float _pushDistance;
public float _pushForce = 5.0f;
bool _scan;

public void Pushy() {
    RaycastHit hit = new RaycastHit();
    float dx = 0.0f;
    Vector3 fwd = _scanner.forward;

    // Scannerを回転させてレーダースキャン
    if (_scan)  // 障害物なし → 高速回転
        _scanner.Rotate(new Vector3(0.0f, 1000 * _newDelta, 0.0f));
    else        // 障害物あり → 低速回転（精密スキャン）
        _scanner.Rotate(new Vector3(0.0f, 250 * _newDelta, 0.0f));

    // Raycast発射
    if (Physics.Raycast(_collider.transform.position, fwd, out hit, _pushDistance, _pushyLayerMask)) {
        Transform hitTransform = hit.transform;

        // 地面以外、または急な坂の場合は押し返す
        if (hitTransform.gameObject.layer != _groundIndex
            || (hitTransform.gameObject.layer == _groundIndex && Vector3.Angle(Vector3.up, hit.normal) > _maxGroundAngle)) {

            float dist = hit.distance;
            dx = (_pushDistance - dist) / _pushDistance;  // 近いほど強く押す

            // Rigidbodyなしで直接位置を操作
            if (gameObject.layer != _herdSimIndex) {
                _thisTR.position -= fwd * _newDelta * dx * _pushForce;
            } else if (dist < _pushDistance * .5f) {
                _thisTR.position -= fwd * _newDelta * (dx - .5f) * _pushForce;
            }

            _scan = false;
        } else {
            _scan = true;
        }
    } else {
        _scan = true;
    }
}
```

#### 🎯 学習ポイント

1. **Rigidbody不要**
   - 物理演算エンジンを一切使わない
   - `Transform.position`を直接操作
   - **物理演算コスト = ゼロ**

2. **回転するScanner**
   - 固定方向のRaycastではなく、回転するScannerで全方位検知
   - 障害物発見時は回転速度を落として精密スキャン
   - **1フレーム1方向のみチェック → 軽量**

3. **距離に応じた押し返し力**
   - `dx = (_pushDistance - dist) / _pushDistance`
   - 近いほど強く押す → 自然な反発

#### Rigidbody vs Transform直接操作

| 方式 | CPU負荷 | メモリ | 精度 | 設定難易度 |
|------|---------|--------|------|-----------|
| Rigidbody | 高い | 多い | 高い | 低い |
| Transform直接操作 | 低い | 少ない | 中程度 | 中程度 |

**100体の場合の影響**:
- Rigidbody方式: 物理演算が100体分実行される
- Transform方式: Raycast + 位置計算のみ（約10倍軽量）

---

## AI行動ロジック

HerdSimのAIは**確率ベース**と**状態管理**の組み合わせで、自然な動物の行動を再現しています。

---

### 1. ステートマシン

**場所**: `HerdSimCore.cs:73`

#### 状態定義

```csharp
public int _mode = 0;

// 0 = Idle（待機）
// 1 = Walk（歩行）
// 2 = Run（走行）
```

#### 状態遷移図

```
        ┌─────┐
        │Idle │
        │ 0   │
        └──┬──┘
           │
    確率的に遷移
           │
    ┌──────┴──────┐
    ▼             ▼
┌──────┐      ┌──────┐
│Walk  │      │Run   │
│  1   │◄────►│  2   │
└──────┘      └──────┘
    │             │
    └─────┬───────┘
          │
    Waypoint到達
          │
          ▼
        Idle
```

---

### 2. 行動決定ロジック ⭐⭐⭐

**場所**: `HerdSimCore.cs:388-441`

#### コード

```csharp
public void Wander() {
    Vector3 t = Vector3.zero;

    // リーダーの場合、リーダーエリアを拡大
    if (_leader == this)
        _leaderArea = Vector3.one * ((_leaderSize * _leaderAreaMultiplier) + 1);

    // エリアと中心位置の決定
    Vector3 _ra = Vector3.zero;  // Roaming Area
    Vector3 _pb = Vector3.zero;  // Position Base

    if ((_leader != null) && _leader != this) {
        // フォロワー → リーダーの周辺を徘徊
        _ra = _leader._leaderArea;
        _pb = _leader.transform.position;
    } else if (_controller == null) {
        // 単独個体 → 初期位置周辺を徘徊
        _ra = _roamingArea;
        _pb = _startPosition;
    } else {
        // コントローラー配下 → コントローラーのエリアを徘徊
        _ra = _controller._roamingArea;
        _pb = _controller.transform.position;
    }

    // ランダムなWaypoint生成
    t.x = Random.Range(-_ra.x, _ra.x) + _pb.x;
    t.z = Random.Range(-_ra.z, _ra.z) + _pb.z;

    // 食べ物がある場合は食べ物へ向かう
    if (_food != null) {
        t = _food.position;
        _mode = 2;  // 走る
    }
    // エリア外に出た場合
    else if (_thisTR.position.x < -_ra.x + _pb.x
          || _thisTR.position.x > _ra.x + _pb.x
          || _thisTR.position.z < -_ra.z + _pb.z
          || _thisTR.position.z > _ra.z + _pb.z) {

        if (Random.value < .1f) {
            _mode = 2;  // 10%の確率で走って戻る
        } else {
            _mode = 1;  // 90%の確率で歩いて戻る
        }
        _waypoint = t;
    }
    // フォロワー（リーダーがいる）の場合
    else if ((_leader != null) && _leader != this && Random.value < .75f) {
        _mode = 0;  // 75%の確率で停止
    }
    // Waypoint到達後の行動決定
    else if (_reachedWaypoint) {
        // -_idleProbablity ~ 1 の範囲でランダム
        // 負の値 → Idle、0 → Idle、1 → Walk、2 → Run
        _mode = Random.Range(-_idleProbablity, 2);

        // Walkの場合、_runChanceの確率でRunに変更（リーダーor単独個体のみ）
        if (_mode == 1 && Random.value < this._runChance && ((_leader == null) || _leader == this)) {
            _mode = 2;
        }
    }

    // 新しいWaypointを設定
    if (_reachedWaypoint && _mode > 0) {
        _waypoint = t;
        CancelInvoke("WalkTimeOut");
        Invoke("WalkTimeOut", 30.0f);  // 30秒以内に到達できなければリセット
        _reachedWaypoint = false;
    }

    _waypoint.y = _collider.transform.position.y;
    _lerpCounter = 0;
}
```

#### 🎯 学習ポイント

1. **確率ベースの行動決定**

```csharp
// アイドル確率の設定（Inspector）
public int _idleProbablity = 20;

// Random.Range(-20, 2)の結果:
// -20 ~ -1 → Idle (約91%の確率)
// 0        → Idle
// 1        → Walk (約4.5%の確率)
// 2        → Run  (約4.5%の確率)
```

2. **階層的行動決定**

```
エリア外? → 走って/歩いて戻る
  ↓ No
フォロワー? → 75%の確率で停止
  ↓ No
Waypoint到達? → 確率的にIdle/Walk/Run
```

3. **自然なばらつき**
   - `Random.value`（0.0～1.0）で有機的な動き
   - 機械的でない行動パターン

---

### 3. 群れ形成システム ⭐⭐⭐

**場所**: `HerdSimCore.cs:347-386`

#### コード

```csharp
public void FindLeader() {
    // 自分がリーダーで、フォロワーが1体以下の場合はリーダー解除
    if (_leader == this && _leaderSize <= 1) {
        _leader = null;
        _leaderSize = 0;
    }
    // フォロワーの場合
    else if (_leader != this) {
        // リーダーが死んだ場合はリーダー解除
        if ((_leader != null) && _leader._dead)
            _leader = null;

        _leaderSize = 0;

        // 半径_herdDistance内の同じtype動物を検索
        Collider[] hitColliders = Physics.OverlapSphere(_thisTR.position, _herdDistance, _herdLayerMask);
        HerdSimCore c = null;

        for (int i = 0; i < hitColliders.Length; i++) {
            if (hitColliders[i].transform.parent != null)
                c = hitColliders[i].transform.parent.GetComponent<HerdSimCore>();

            // 同じtype、かつ自分以外
            if ((c != null) && c != this && _type == c._type) {

                // ケース1: 両方リーダーなし → 自分がリーダーになる
                if ((_leader == null) && (c._leader == null)) {
                    _leader = this;
                    c._leader = this;
                    _leaderSize += 2;
                    break;
                }

                // ケース2: 相手がリーダーを持っている → リーダーを養子縁組
                if ((_leader == null) && (c._leader != null) && c._leader._leaderSize < c._leader._herdSize) {
                    _leader = c._leader;
                    _leader._leaderSize++;
                    break;
                }

                // ケース3: リーダーの乗り換え（より大きな群れへ）
                if ((_leader != null) && c._leader != _leader) {
                    if ((c._leader != null) && c._leader._leaderSize >= _leader._leaderSize && c._leader._leaderSize < c._leader._herdSize) {
                        _leader._leaderSize--;
                        c._leader._leaderSize++;
                        _leader = c._leader;
                        break;
                    }
                }
            }
        }
    }
}
```

#### 🎯 学習ポイント

1. **動的階層形成**

```
状況1: 個体A、個体B（両方リーダーなし）
→ 個体Aがリーダーになる
→ 個体Bは個体Aのフォロワーになる

状況2: 個体C（リーダーなし）が群れDに接近
→ 個体Cは群れDのリーダーのフォロワーになる

状況3: 群れE（小）が群れF（大）に接近
→ 群れE全体が群れFに吸収される
```

2. **群れサイズ管理**

```csharp
public int _leaderSize;        // 現在のフォロワー数
public int _maxHerdSize = 25;  // 最大フォロワー数
public int _minHerdSize = 10;  // 最小フォロワー数

// Start()でランダム化
_herdSize = Random.Range(this._minHerdSize, this._maxHerdSize);
```

3. **リーダーエリアの動的拡大**

```csharp
if (_leader == this)
    _leaderArea = Vector3.one * ((_leaderSize * _leaderAreaMultiplier) + 1);
```

フォロワーが増えるほど徘徊エリアが広がる → 自然な群れの広がり

---

### 4. 恐怖システム ⭐⭐

**場所**: `HerdSimCore.cs:553-572`, `HerdSimScary.cs`

#### HerdSimCore.cs

```csharp
public bool _scared;        // 恐怖状態フラグ
public Transform _scaredOf; // 恐怖対象

public void Scare(Transform t) {
    // 初回恐怖設定
    if (_scaredOf == null)
        _scaredOf = t;

    _mode = 2;  // 強制的に走る

    if (!_scared) {
        _scared = true;
        UnFlock();  // 群れから離脱
        Invoke("EndScare", 3.0f);  // 3秒後に恐怖解除
    } else {
        // より近い脅威に切り替え
        if (Vector3.Distance(_scaredOf.position, _thisTR.position) > Vector3.Distance(t.position, _thisTR.position)) {
            _scaredOf = t;
        }
    }
}

public void EndScare() {
    _scared = false;
    Wander();  // 新しいWaypointを設定
    _reachedWaypoint = true;
}
```

#### HerdSimScary.cs（恐怖刺激発生側）

```csharp
public int[] _scareType;  // 怖がらせる対象のtype配列
public float _scareRadius = 4.0f;  // 検知半径

public void BeScary() {
    // 半径内の動物を検索
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, _scareRadius, _herdLayerMask);
    HerdSimCore c = null;

    for (int i = 0; i < hitColliders.Length; i++) {
        Transform t = hitColliders[i].transform.parent;
        if (t != null)
            c = t.GetComponent<HerdSimCore>();

        if (c != null) {
            bool scare = false;

            // _scareType配列に含まれているかチェック
            for (int j = 0; j < _scareType.Length; j++) {
                if (c._type == _scareType[j])
                    scare = true;
            }

            if (scare) {
                c.Scare(this.transform);  // 恐怖を与える

                // 追跡モード（HerdSimCore持ちの場合）
                if ((_chase == null) && _canChase)
                    _chase = c;
            }
        }
    }

    // 追跡対象がいる場合、自分も走る
    if (_chase != null) {
        HerdSimCore p = GetComponent<HerdSimCore>();
        if (p != null) {
            p._waypoint = _chase.transform.position;
            p._mode = 2;
        }
    }
}
```

#### 🎯 学習ポイント

1. **感情状態の時限管理**
   - 恐怖状態は3秒間継続
   - `Invoke("EndScare", 3.0f)`で自動解除

2. **脅威の優先順位**
   - より近い脅威に自動的に切り替え
   - 複数の敵に囲まれても適切に対応

3. **群れからの離脱**
   - 恐怖状態になると`UnFlock()`で群れから離脱
   - リアルな逃走行動

4. **追跡システム**
   - HerdSimScaryを持つ動物（捕食者）が獲物を追跡
   - `_canChase`フラグで追跡のON/OFF切り替え

---

## Raycast衝突回避システム

HerdSimの核となる**NavMeshなしパスファインディング**の実装です。

---

### 1. 3方向Raycast ⭐⭐⭐

**場所**: `HerdSimCore.cs:745-850`

#### 仕組み

```
        前方
         ↑
         │
    ┌────┼────┐
    │    │    │
左前│    │    │右前
    │    │    │
    └────┼────┘
         │
      動物の位置
```

#### コード（簡略版）

```csharp
public float _avoidAngle = 0.35f;    // 左右Raycastの角度
public float _avoidDistance;         // Raycast距離
public float _avoidSpeed = 75.0f;    // 回避時の回転速度
public float _stopDistance;          // 停止距離
float _rotateCounterR;               // 右回転カウンター
float _rotateCounterL;               // 左回転カウンター

public bool Avoidance() {
    bool r = false;
    RaycastHit hit = new RaycastHit();
    float dx = 0.0f;
    Vector3 fwd = _model.transform.forward;
    Vector3 rgt = _model.transform.right;

    // Idle中は回避不要
    if (_mode == 0 && _speed < 0.21f) {
        return true;
    }

    // ========== 左前方Raycast ==========
    if (_mode > 0 && _rotateCounterR == 0 &&
        Physics.Raycast(_collider.transform.position,
                        fwd + (rgt * (_avoidAngle + _rotateCounterL)),
                        out hit, _avoidDistance, _pushyLayerMask)) {

        Transform hitTransform = hit.transform;

        // 地面以外、または急な坂の場合
        if (hitTransform.gameObject.layer != _groundIndex
            || (hitTransform.gameObject.layer == _groundIndex && Vector3.Angle(Vector3.up, hit.normal) > _maxGroundAngle)) {

            // 左回転カウンター増加
            _rotateCounterL += _newDelta;
            dx = (_avoidDistance - hit.distance) / _avoidDistance;

            // 左に回転
            Quaternion rot = _thisTR.rotation;
            rot.eulerAngles = new Vector3(rot.eulerAngles.x,
                                          rot.eulerAngles.y - _avoidSpeed * _newDelta * dx * _rotateCounterL * spd,
                                          rot.eulerAngles.z);
            _thisTR.rotation = rot;

            _avoidingLeft = true;
            _avoidingRight = false;

            // カウンター上限
            if (_rotateCounterL > 1.5f) {
                _rotateCounterL = 1.5f;
                _rotateCounterR = 0.0f;
                r = true;
            }
        }
    }
    // ========== 右前方Raycast ==========
    else if (_mode > 0 && _rotateCounterL == 0 &&
             Physics.Raycast(_collider.transform.position,
                             fwd + (rgt * -(_avoidAngle + _rotateCounterR)),
                             out hit, _avoidDistance, _pushyLayerMask)) {

        // 左前方と同様の処理（右回転）
        // ... (コード省略)
    }
    // ========== カウンターの減衰 ==========
    else {
        _rotateCounterL -= _newDelta;
        if (_rotateCounterL < 0) _rotateCounterL = 0.0f;
        _rotateCounterR -= _newDelta;
        if (_rotateCounterR < 0) _rotateCounterR = 0.0f;
    }

    // ========== 正面Raycast（緊急回避） ==========
    if (Physics.Raycast(_collider.transform.position,
                        fwd + (rgt * Random.Range(-.1f, .1f)),
                        out hit, _avoidDistance * .9f, _pushyLayerMask)) {

        Transform hitTransform = hit.transform;

        if (hitTransform.gameObject.layer != _groundIndex
            || (hitTransform.gameObject.layer == _groundIndex && Vector3.Angle(Vector3.up, hit.normal) > _maxGroundAngle)) {

            float dist = hit.distance;
            dx = (_avoidDistance - hit.distance) / _avoidDistance;

            // 左右カウンターの大きい方向に回転
            Quaternion rot = _thisTR.rotation;
            if (_rotateCounterL > _rotateCounterR) {
                rot.eulerAngles = new Vector3(rot.eulerAngles.x,
                                              rot.eulerAngles.y - _avoidSpeed * _newDelta * dx * _rotateCounterL,
                                              rot.eulerAngles.z);
            } else {
                rot.eulerAngles = new Vector3(rot.eulerAngles.x,
                                              rot.eulerAngles.y + _avoidSpeed * _newDelta * dx * _rotateCounterR,
                                              rot.eulerAngles.z);
            }
            transform.rotation = rot;

            // 非常に近い場合はバックする
            if (dist < _stopDistance * .5f) {
                _speed = -.2f;
                r = true;
            }

            // 停止距離内の場合は減速
            if (dist < _stopDistance && _speed > .2f) {
                _speed -= _newDelta * (1 - dx) * 25;
            }

            if (_speed < -.2f) {
                _speed = -.2f;
            }
        }
    }

    return r;
}
```

#### 🎯 学習ポイント

1. **多段階回避**

| 段階 | Raycast方向 | 処理 |
|------|------------|------|
| 1 | 左前方 | 左に障害物 → 左回転カウンター増加 → 左に回転 |
| 2 | 右前方 | 右に障害物 → 右回転カウンター増加 → 右に回転 |
| 3 | 正面 | 正面に障害物 → カウンター大きい方向に回転 + 減速/後退 |

2. **回転速度の累積**
   - `_rotateCounterL/R`で回転速度が徐々に増加
   - 障害物が近いほど、長く避けているほど強く回転
   - 滑らかな回避動作

3. **左右の排他制御**
   - `_rotateCounterR == 0`の時のみ左前方チェック
   - `_rotateCounterL == 0`の時のみ右前方チェック
   - 左右に振動しない安定した回避

4. **緊急回避**
   - 正面に障害物 → 後退（`_speed = -.2f`）
   - `_stopDistance`内で減速
   - リアルな行動

---

### 2. 地面検知システム ⭐⭐

**場所**: `HerdSimCore.cs:728-739`

#### コード

```csharp
public float _maxGroundAngle = 45.0f;  // 最大登坂角度
public float _maxFall = 3.0f;          // 最大落下距離
public float _fakeGravity = 5.0f;      // 疑似重力の強さ

public void GroundCheck() {
    RaycastHit hit = new RaycastHit();

    // 真下にRaycast発射
    if (Physics.Raycast(new Vector3(_thisTR.position.x, _collider.transform.position.y, _thisTR.position.z),
                        -_thisTR.up,
                        out hit,
                        _maxFall,
                        _groundLayerMask)) {

        _grounded = true;

        // 地面の法線からモデルの回転を計算
        _groundRot = Quaternion.FromToRotation(_model.transform.up, hit.normal) * _model.transform.rotation;

        _ground = hit.point;  // 地面の位置を記録
    } else {
        // 地面が見つからない（落下中）
        _grounded = false;
        _waypoint = _thisTR.position + (_thisTR.right * 5);  // 横に移動して地面を探す
        _speed = 0.0f;
    }
}
```

#### Update()での地面への移動

```csharp
void Update() {
    // ... (省略)

    // 疑似重力（地面に向かって移動）
    Vector3 gr = _thisTR.position;
    gr.y -= (_thisTR.position.y - _ground.y) * _newDelta * _fakeGravity;
    _thisTR.position = gr;

    // ... (省略)

    // モデルを地面に沿って回転
    _model.transform.rotation = Quaternion.Slerp(_model.transform.rotation, _groundRot, _newDelta * 5);
}
```

#### 🎯 学習ポイント

1. **地面の法線から回転計算**

```csharp
// 地面の法線（Normal）からモデルの回転を計算
_groundRot = Quaternion.FromToRotation(_model.transform.up, hit.normal) * _model.transform.rotation;
```

これにより、坂道や段差に自然に沿って歩く

2. **最大登坂角度**

```csharp
// Avoidance()内で地面角度をチェック
if (hitTransform.gameObject.layer == _groundIndex && Vector3.Angle(Vector3.up, hit.normal) > _maxGroundAngle) {
    // 急な坂 → 障害物として扱う
}
```

`_maxGroundAngle = 45.0f`以上の急な坂は登らない

3. **疑似重力**
   - Rigidbodyの重力を使わない
   - `gr.y -= (_thisTR.position.y - _ground.y) * _newDelta * _fakeGravity`
   - 滑らかに地面に吸着

---

### 3. LayerMask活用 ⭐⭐

**場所**: `HerdSimCore.cs:144-149`

#### コード

```csharp
public LayerMask _groundLayerMask = (LayerMask)(-1);   // 地面検知用
public LayerMask _pushyLayerMask = (LayerMask)(-1);    // 障害物検知用
public LayerMask _herdLayerMask = (LayerMask)(-1);     // 群れ検知用

int _groundIndex = 25;
int _herdSimIndex = 26;

void Start() {
    _groundIndex = LayerMask.NameToLayer(this._groundTag);
    _herdSimIndex = LayerMask.NameToLayer(this._herdSimLayerName);
}
```

#### 使用例

```csharp
// 地面検知Raycast
Physics.Raycast(..., _maxFall, _groundLayerMask)

// 障害物検知Raycast
Physics.Raycast(..., _avoidDistance, _pushyLayerMask)

// 群れ検索
Physics.OverlapSphere(_thisTR.position, _herdDistance, _herdLayerMask)
```

#### 🎯 学習ポイント

1. **用途別LayerMask**

| LayerMask | 用途 | 含まれるLayer |
|-----------|------|--------------|
| `_groundLayerMask` | 地面検知 | Layer 25（Ground） |
| `_pushyLayerMask` | 障害物検知 | Layer 0, 25, 26 |
| `_herdLayerMask` | 群れ検索 | Layer 26（HerdSim） |

2. **Raycast最適化**
   - 不要なオブジェクトとの衝突判定を完全排除
   - Layer設定だけで処理速度が劇的に向上

3. **設定例**

```
Layer 0:  Default（壁・障害物）
Layer 25: Ground（地面）
Layer 26: HerdSim（動物）

_groundLayerMask:  Layer 25のみ
_pushyLayerMask:   Layer 0, 25, 26
_herdLayerMask:    Layer 26のみ
```

---

## その他の優れたテクニック

---

### 1. アニメーション速度の動的調整 ⭐⭐

**場所**: `HerdSimCore.cs:471-550`

#### コード

```csharp
public void AnimationHandler() {
    if (!_dead) {
        // ========== Walk ==========
        if (_mode == 1) {
            if (currentAnimation != _animWalk) {
                if (!_animator) {
                    _animation.CrossFade(_animWalk, .5f);
                } else {
                    _animator.CrossFade(_animWalk, .5f);
                }
                currentAnimation = _animWalk;
            }

            // 実際の速度に応じてアニメーション速度を調整
            if (_speed > 0) {
                if (!_animator) {
                    _animation[_animWalk].speed = (_speed * _animWalkSpeed) + 0.051f;
                } else {
                    _animator.speed = (_speed * _animWalkSpeed) + 0.051f;
                }
            } else {
                if (!_animator) {
                    _animation[_animWalk].speed = .1f;
                } else {
                    _animator.speed = .1f;
                }
            }
            _idle = false;
        }
        // ========== Run ==========
        else if (_mode == 2) {
            if (currentAnimation != _animRun) {
                if (!_animator) {
                    _animation.CrossFade(_animRun, .5f);
                } else {
                    _animator.CrossFade(_animRun, .5f);
                }
                currentAnimation = _animRun;
            }

            if (_speed > _runSpeed * .35f) {
                if (!_animator) {
                    _animation[_animRun].speed = (_speed * _animRunSpeed) + 0.051f;
                } else {
                    _animator.speed = (_speed * _animRunSpeed) + 0.051f;
                }
            } else {
                // 速度が遅い場合はWalkアニメーションに切り替え
                if (!_animator) {
                    _animation.CrossFade(_animWalk, .5f);
                    _animation[_animWalk].speed = (_speed * _animWalkSpeed) + 0.051f;
                } else {
                    _animator.CrossFade(_animWalk, .5f);
                    _animator.speed = Mathf.Clamp(_speed * _animWalkSpeed, 0, 1000);
                }
            }
            _idle = false;
        }
        // ========== Idle/Sleep ==========
        else {
            if (currentAnimation != _animIdle) {
                if (!_animator) _animation.CrossFade(_animIdle, .5f);
                else _animator.CrossFade(_animIdle, .5f);
                currentAnimation = _animIdle;
            }

            // 一定時間Idleの場合、Sleepアニメーションに移行
            if (currentAnimation != _animSleep) {
                if (_idle && _sleepCounter > _idleToSleepSeconds) {
                    if (!_animator) _animation.CrossFade(_animSleep, .5f);
                    else _animator.CrossFade(_animSleep, .5f);
                }
                currentAnimation = _animSleep;
            }

            if (!_idle && _speed < .5f) {
                _sleepCounter = 0.0f;
                _idle = true;
            } else {
                _sleepCounter += _newDelta;
            }
        }
    }
}
```

#### 🎯 学習ポイント

1. **速度同期**

```csharp
animation.speed = (_speed * _animWalkSpeed) + 0.051f
```

- 実際の移動速度とアニメーション速度を連動
- `+ 0.051f`で停止時も僅かに動く → 不自然な完全停止を回避

2. **滑らない動き**
   - アニメーション速度 ∝ 実際の移動速度
   - 足が地面を滑らない

3. **Legacy Animation / Animator両対応**

```csharp
if (!_animator) {
    _animation.CrossFade(_animWalk, .5f);
} else {
    _animator.CrossFade(_animWalk, .5f);
}
```

---

### 2. Lean（傾き）アニメーション ⭐⭐

**場所**: `HerdSimCore.cs:237-258`

#### 初期化

```csharp
public bool _lean;  // 傾き機能ON/OFF
public AnimationClip _leanLeftAnimation;
public AnimationClip _leanRightAnimation;

AnimationState _leanLeft;
AnimationState _leanRight;

void LeanInit() {
    if (!_lean || _animator != null) return;

    _leanLeft = _animation[_leanLeftAnimation.name];
    _leanRight = _animation[_leanRightAnimation.name];

    // レイヤー10に配置
    _leanRight.layer = _leanLeft.layer = 10;

    // 加算アニメーションとして設定
    _leanRight.blendMode = _leanLeft.blendMode = AnimationBlendMode.Additive;

    _leanRight.enabled = true;
    _leanLeft.enabled = true;

    _leanLeft.weight = _leanRight.weight = 1.0f;

    // ループしない（静止ポーズとして使用）
    _leanRight.wrapMode = _leanLeft.wrapMode = WrapMode.ClampForever;
}
```

#### 実行時

```csharp
float _leanRightTime;
float _leanLeftTime;
bool _avoidingLeft;
bool _avoidingRight;

void Lean() {
    if (!_lean || _animator != null) return;

    float a = AngleAmount();  // -1.0 ~ 1.0 の曲がる角度

    // 左に傾ける条件
    if (_avoidingLeft || !_avoiding && _mode != 0 && a < 0.3) {
        _leanLeftTime = Mathf.Lerp(_leanLeftTime, (-a), _newDelta * 2f);
    } else {
        _leanLeftTime = Mathf.Lerp(_leanLeftTime, 0, _newDelta);
    }

    // 右に傾ける条件
    if (_avoidingRight || !_avoiding && _mode != 0 && a > 0.3) {
        _leanRightTime = Mathf.Lerp(_leanRightTime, (a), _newDelta * 2f);
    } else {
        _leanRightTime = Mathf.Lerp(_leanRightTime, 0, _newDelta);
    }

    // アニメーション再生位置を直接操作
    _leanLeft.normalizedTime = _leanLeftTime;
    _leanRight.normalizedTime = _leanRightTime;
}

float AngleAmount() {
    Vector3 dir = (_waypoint - transform.position).normalized;

    float direction = Vector3.Dot(dir, transform.right);  // -1.0 ~ 1.0
    float behind = Vector3.Dot(dir, transform.forward);

    // 後方の場合は-1 or 1にクランプ
    if (behind < 0) {
        if (direction < 0) direction = -1;
        if (direction > 0) direction = 1;
    }
    return direction;
}
```

#### 🎯 学習ポイント

1. **加算アニメーション**

```csharp
_leanRight.blendMode = AnimationBlendMode.Additive;
```

- 走りアニメーション + 傾きアニメーション
- 2つのアニメーションを重ねて再生

2. **normalizedTimeの直接操作**

```csharp
_leanLeft.normalizedTime = _leanLeftTime;
```

- アニメーションを「静的ポーズ」として使用
- 再生位置を直接指定 → 曲がる角度に応じた傾き

3. **リアルなバンク表現**
   - 左に曲がる → 左に傾く
   - 右に曲がる → 右に傾く
   - バイクのような自然な傾き

---

### 3. タイムアウトシステム ⭐⭐

**場所**: `HerdSimCore.cs:619-623`, `HerdSimCore.cs:434-437`

#### コード

```csharp
public void Wander() {
    // ... (省略)

    if (_reachedWaypoint && _mode > 0) {
        _waypoint = t;
        CancelInvoke("WalkTimeOut");
        Invoke("WalkTimeOut", 30.0f);  // 30秒後にタイムアウト
        _reachedWaypoint = false;
    }

    // ... (省略)
}

public void WalkTimeOut() {
    _reachedWaypoint = true;
    UnFlock();  // 群れから離脱
    Wander();   // 新しいWaypointを設定
}
```

#### 🎯 学習ポイント

1. **デッドロック回避**
   - Waypoint到達不可能な場合の救済措置
   - 30秒経過 → 新しいWaypointを再設定

2. **無限ループ防止**
   - 狭い場所で詰まった場合も自動回復
   - ゲームが止まらない

3. **群れからの離脱**
   - タイムアウト時は群れから離脱
   - 個別に新しい経路を探索

---

## 学ぶべきテクニック Top 10

HerdSimアセットから学べる、**実戦で即使える**テクニックのランキングです。

---

### 1位: フレームスキップ最適化 ⭐⭐⭐

**効果**: CPU負荷を1/2～1/4に削減

**手法**:
```csharp
public int _updateDivisor = 3;  // 3フレームに1回だけUpdate
static int _updateNextSeed = 0;
int _updateSeed;
float _newDelta;

void Update() {
    if (_updateDivisor > 1) {
        _updateCounter++;
        if (_updateCounter != _updateSeed) {
            _updateCounter = _updateCounter % _updateDivisor;
            return;
        }
        _newDelta = Time.deltaTime * _updateDivisor;
    }
    // 以降の処理は_newDeltaを使用
}
```

**応用先**:
- 大量の敵キャラ（100体以上）
- 背景NPC
- パーティクル風の群れシミュレーション

---

### 2位: Rigidbodyなし物理演算 ⭐⭐⭐

**効果**: 物理演算コストをゼロに

**手法**:
```csharp
// Raycastで障害物検知 + Transform直接操作
if (Physics.Raycast(_collider.transform.position, fwd, out hit, _pushDistance)) {
    _thisTR.position -= fwd * _newDelta * dx * _pushForce;
}
```

**応用先**:
- 鳥の群れ
- 魚の群れ
- 軽量な敵AI

---

### 3位: 3方向Raycast回避 ⭐⭐⭐

**効果**: 滑らかな障害物回避

**手法**:
```csharp
// 左前、右前、正面の3方向をチェック
// _rotateCounterL/Rで回転速度が徐々に増加
```

**応用先**:
- NavMeshなしのAI移動
- 車・バイクの自動運転
- ドローンの障害物回避

---

### 4位: 確率ベースAI ⭐⭐⭐

**効果**: 自然で有機的な動き

**手法**:
```csharp
_mode = Random.Range(-_idleProbablity, 2);
if (_mode == 1 && Random.value < _runChance) _mode = 2;
```

**応用先**:
- NPC行動
- 敵AI
- 動物シミュレーション

---

### 5位: InvokeRepeating活用 ⭐⭐

**効果**: 処理頻度の最適化

**手法**:
```csharp
InvokeRepeating("GroundCheck", Random.value + 1, 0.1f);
InvokeRepeating("FindLeader", Random.value * 3, 3.0f);
```

**応用先**:
- Raycast系の重い処理
- 状態更新
- 群れ検索

---

### 6位: パーティクル一元管理 ⭐⭐

**効果**: DrawCallを100個 → 1個に削減

**手法**:
```csharp
// コントローラーが共有ParticleSystemを管理
_controller._runPS.transform.position = this._thisTR.position;
_controller._runPS.Emit(1);
```

**応用先**:
- 足跡
- ダスト
- 血しぶき

---

### 7位: 距離ベースLOD ⭐⭐

**効果**: 遠距離の処理を完全停止

**手法**:
```csharp
if ((transform.position - _distanceFrom.position).sqrMagnitude > _distanceDisable) {
    transform.GetComponent<HerdSimCore>().Disable(_disableModel, _disableCollider);
}
```

**応用先**:
- オープンワールドゲーム
- 大規模戦闘
- MMO

---

### 8位: 動的階層システム ⭐⭐

**効果**: リアルな群れ形成

**手法**:
```csharp
// Physics.OverlapSphere + リーダーシップ管理
// 大きな群れに小さな群れが吸収される
```

**応用先**:
- 鳥の群れ
- 魚の群れ
- 群衆シミュレーション

---

### 9位: アニメーション速度同期 ⭐⭐

**効果**: 足が滑らない動き

**手法**:
```csharp
animation.speed = (_speed * _animWalkSpeed) + 0.051f;
```

**応用先**:
- キャラクター移動全般
- 車輪の回転
- 歩行アニメーション

---

### 10位: 加算アニメーション（Lean） ⭐⭐

**効果**: リアルな傾き表現

**手法**:
```csharp
_leanRight.blendMode = AnimationBlendMode.Additive;
_leanLeft.normalizedTime = _leanLeftTime;
```

**応用先**:
- バイク/車の傾き
- 走行時の体の傾き
- カメラのロール

---

## 実戦投入時の注意点

### ✅ 強み

1. **NavMesh不要**
   - 動的な地形変化に対応可能
   - メモリ使用量が少ない

2. **Rigidbody不要**
   - 物理演算コストがゼロ
   - 100体以上の同時処理が可能

3. **地形自動追従**
   - 坂道、段差に自然に沿って歩く
   - `_maxGroundAngle`で登坂制限可能

4. **軽量動作**
   - フレームスキップ + InvokeRepeating + 距離LODの三重最適化
   - モバイルでも動作可能

---

### ⚠️ 弱み・制約

1. **複雑な経路探索には不向き**
   - NavMeshの方が優秀（A*アルゴリズム使用）
   - 迷路や入り組んだ建物内では非効率

2. **デッドロック発生の可能性**
   - 30秒タイムアウトで救済されるが、一時的に詰まる
   - 狭い通路では渋滞が発生しやすい

3. **Layer設定が必須**
   - `_groundLayerMask`、`_pushyLayerMask`、`_herdLayerMask`を正しく設定しないと動作不良
   - Layer管理が複雑化する

4. **Raycast多用**
   - 個体数 × Raycast数 = 負荷
   - Layer設定を誤ると極端に重くなる

---

### 🔧 推奨設定

#### 少数（1～10体）
```csharp
_updateDivisor = 1;           // フレームスキップなし
_groundCheckInterval = 0.05f; // 地面検知：20FPS
```

#### 中規模（10～50体）
```csharp
_updateDivisor = 2;           // 2フレームに1回
_groundCheckInterval = 0.1f;  // 地面検知：10FPS
```

#### 大規模（50～100体以上）
```csharp
_updateDivisor = 3;           // 3フレームに1回
_groundCheckInterval = 0.2f;  // 地面検知：5FPS
_distanceDisable = 2500;      // 50m以上で無効化
```

---

## プロジェクトへの応用例

現在の「川のやつ」プロジェクトへの応用例です。

---

### 1. ネズミの逃走AI（実装済み）

**実装日**: 2025-12-05
**場所**: `HerdSimScary.cs`

**応用内容**:
- PlayerCubeに`HerdSimScary`コンポーネントをアタッチ
- `_scareRadius = 4.0f`でネズミ検知半径を調整可能に改良
- ネズミが半径内に入ると逃走開始

**追加改良案**:
```csharp
// フレームスキップ導入で大量のネズミを軽量化
_updateDivisor = 3;  // トンネル内に50匹のネズミを配置しても軽快
```

---

### 2. 蝙蝠の飛行制御（拡張案）

**現在の実装**: `BatMove.cs`（直線移動のみ）

**HerdSim応用案**:
```csharp
public class BatMoveAdvanced : MonoBehaviour {
    // HerdSimCoreの3方向Raycast回避を応用
    public float _avoidDistance = 2.0f;
    public float _avoidAngle = 0.35f;

    bool Avoidance() {
        // 左前、右前、正面の3方向Raycastで壁回避
        // HerdSimCore.cs:745-850 と同じロジック
    }

    void Update() {
        // 目標地点への移動 + 壁回避
        if (!Avoidance()) {
            transform.position += transform.forward * _speed * Time.deltaTime;
        }
    }
}
```

**効果**:
- 壁に衝突せず滑らかに飛行
- 複数の蝙蝠を配置してもフレームスキップで軽量

---

### 3. 水中の魚群シミュレーション（新規提案）

**概要**: トンネル内の水たまりに魚群を配置

**実装**:
```csharp
public class FishSwarm : HerdSimCore {
    // HerdSimCoreを継承してY軸移動を有効化

    void Start() {
        base.Start();
        // Y軸移動を許可
        _roamingArea = new Vector3(5, 3, 5);  // 幅5m、高さ3m、奥行5m
    }

    void Update() {
        base.Update();
        // 水面・水底の制限
        Vector3 pos = transform.position;
        pos.y = Mathf.Clamp(pos.y, waterBottom, waterSurface);
        transform.position = pos;
    }
}
```

**効果**:
- HerdSimの群れ形成をそのまま活用
- フレームスキップで100匹の魚も軽量動作

---

### 4. RouteFollowerへの応用

**提案**: RouteFollowerに障害物回避機能を追加

**実装**:
```csharp
public class RouteFollowerAdvanced : RouteFollower {
    public float _avoidDistance = 3.0f;

    void Update() {
        if (FadeManager.IsFading) return;

        // HerdSimの3方向Raycast回避を導入
        if (Avoidance()) {
            // 障害物あり → 回避行動
        } else {
            // 通常のRouteFollower処理
            base.MoveTowardsCurrentWaypoint();
        }
    }

    bool Avoidance() {
        // HerdSimCore.cs:745-850 のロジックを流用
    }
}
```

**効果**:
- WayPoint移動中に障害物を自動回避
- より自然な移動

---

## ボーナス：カメラ制御テクニック

HerdSimには**デモ用のカメラ制御スクリプト**も含まれています。

**場所**: `SmoothCameraOrbit.cs`

---

### 1. Lerpによる滑らかなカメラ移動 ⭐⭐

#### コード

```csharp
private float currentDistance;
private float desiredDistance;
private Quaternion currentRotation;
private Quaternion desiredRotation;
public float zoomDampening = 5.0f;

void LateUpdate() {
    // マウス左クリックでオービット
    if (Input.GetMouseButton(0)) {
        xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
        yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
        yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);

        // 目標回転を設定
        desiredRotation = Quaternion.Euler(yDeg, xDeg, 0);
        currentRotation = transform.rotation;

        // Lerpで滑らかに回転
        rotation = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * zoomDampening);
        transform.rotation = rotation;
    }

    // マウスホイールでズーム
    desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
    desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

    // Lerpで滑らかにズーム
    currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);

    // カメラ位置計算
    position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
    transform.position = position;
}
```

#### 🎯 学習ポイント

1. **現在値と目標値の分離**

```csharp
currentRotation  // 現在の回転
desiredRotation  // 目標の回転

currentDistance  // 現在の距離
desiredDistance  // 目標の距離
```

- 即座に変化させず、`Lerp`で補間 → 滑らかな動き

2. **Time.deltaTime * dampening**

```csharp
Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime * zoomDampening);
```

- `zoomDampening`が大きいほど速く到達
- フレームレート非依存

3. **ClampAngle関数**

```csharp
private static float ClampAngle(float angle, float min, float max) {
    if (angle < -360) angle += 360;
    if (angle > 360) angle -= 360;
    return Mathf.Clamp(angle, min, max);
}
```

- 360度を超える角度を正規化
- カメラの上下制限に使用

---

### 2. LateUpdate()の使用 ⭐⭐

```csharp
void LateUpdate() {
    // カメラ処理
}
```

#### 🎯 学習ポイント

**Update()実行順序**:
```
1. Update()        - キャラクター移動
2. LateUpdate()    - カメラ移動（キャラクター移動後）
3. レンダリング
```

**効果**:
- キャラクター移動後にカメラ更新 → 遅延・ジッターなし
- 追跡カメラには必須のテクニック

**応用例**:
```csharp
public class ThirdPersonCamera : MonoBehaviour {
    public Transform target;

    void LateUpdate() {
        // targetの移動が完了してからカメラ位置を更新
        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
```

---

### 3. Target自動生成 ⭐

```csharp
public Transform target;
public float distance = 5.0f;

public void Init() {
    // Targetが未設定の場合、自動生成
    if (!target) {
        GameObject go = new GameObject("Cam Target");
        go.transform.position = transform.position + (transform.forward * distance);
        target = go.transform;
    }
}
```

#### 🎯 学習ポイント

**防御的プログラミング**:
- Inspector設定忘れでもエラーにならない
- デフォルト動作を提供

**応用例**:
```csharp
public class FollowTarget : MonoBehaviour {
    public Transform target;

    void Start() {
        // targetが未設定ならPlayerを自動検索
        if (!target) {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }
}
```

---

## ボーナス：Unityエディタ拡張テクニック

HerdSimには**カスタムInspector**も実装されています。

**場所**: `HerdSimCoreEditor.cs`

---

### 1. CustomEditor属性 ⭐⭐⭐

```csharp
using UnityEditor;

[CustomEditor(typeof(HerdSimCore))]
[CanEditMultipleObjects]
public class HerdSimCoreEditor : Editor {
    public override void OnInspectorGUI() {
        // カスタムInspector描画
    }
}
```

#### 🎯 学習ポイント

1. **CustomEditor属性**
   - 特定のコンポーネント用にInspectorをカスタマイズ
   - デフォルトのInspectorを完全に置き換え

2. **CanEditMultipleObjects属性**
   - 複数選択時の一括編集に対応
   - SerializedPropertyと組み合わせて使用

**効果**:
- 使いやすいInspector
- パラメータ説明の表示
- 警告・エラー表示

---

### 2. SerializedProperty使用 ⭐⭐⭐

```csharp
public SerializedProperty updateDivisor;
public SerializedProperty walkSpeed;

public void OnEnable() {
    updateDivisor = serializedObject.FindProperty("_updateDivisor");
    walkSpeed = serializedObject.FindProperty("_walkSpeed");
}

public override void OnInspectorGUI() {
    serializedObject.Update();  // 開始

    EditorGUILayout.IntSlider(updateDivisor, 1, 9);
    EditorGUILayout.PropertyField(walkSpeed, new GUIContent("Walk Speed"));

    serializedObject.ApplyModifiedProperties();  // 終了
}
```

#### 🎯 学習ポイント

**SerializedProperty利用のメリット**:

| 機能 | SerializedProperty | 直接編集 |
|------|-------------------|----------|
| Undo/Redo対応 | ✅ 自動 | ❌ 手動実装必要 |
| マルチ編集対応 | ✅ 自動 | ❌ 手動実装必要 |
| Prefab Override表示 | ✅ 自動 | ❌ 不可 |
| パフォーマンス | ✅ 良好 | ❌ 遅い |

**正しい使い方**:
```csharp
// ✅ SerializedProperty（推奨）
EditorGUILayout.PropertyField(walkSpeed, new GUIContent("Walk Speed"));

// ❌ 直接編集（非推奨）
target_cs._walkSpeed = EditorGUILayout.FloatField("Walk Speed", target_cs._walkSpeed);
```

---

### 3. 折りたたみ可能なヘルプ ⭐⭐

```csharp
public bool showHelp;
public bool showHelpMovement;

public override void OnInspectorGUI() {
    EditorGUILayout.BeginHorizontal();
    EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);

    // ?ボタン
    GUI.color = helpColor;
    if (GUILayout.Button("?", buttonStyle)) {
        showHelpMovement = !showHelpMovement;
    }
    EditorGUILayout.EndHorizontal();

    EditorGUILayout.PropertyField(walkSpeed, new GUIContent("Walk Speed"));

    // ヘルプ表示
    if (showHelpMovement) {
        EditorGUILayout.LabelField("How fast this moves while walking", helpStyle);
    }
}
```

#### 🎯 学習ポイント

**ユーザビリティ向上**:
- 通常時：シンプルなInspector
- ?ボタンクリック：詳細説明表示
- 初心者に優しく、上級者には邪魔にならない

**GUIStyleカスタマイズ**:
```csharp
GUIStyle helpStyle = new GUIStyle(GUI.skin.label);
helpStyle.fontSize = 9;
helpStyle.normal.textColor = Color.yellow;

EditorGUILayout.LabelField("ヘルプテキスト", helpStyle);
```

---

### 4. Layer存在チェック & 警告表示 ⭐⭐

```csharp
bool warned = false;

// Layer存在チェック
if (LayerMask.NameToLayer(target_cs._groundTag) == -1) {
    EditorGUILayout.LabelField("Warning: No " + target_cs._groundTag + " layer found", boxStyle);
    warned = true;
}

if (LayerMask.NameToLayer(target_cs._herdSimLayerName) == -1) {
    EditorGUILayout.LabelField("Warning: No " + target_cs._herdSimLayerName + " layer found", boxStyle);
    warned = true;
}

if (warned) {
    EditorGUILayout.LabelField("Please create layers:\nLayer25: Ground\n & \nLayer26: HerdSim", boxStyle2);
}
```

#### 🎯 学習ポイント

**設定ミス防止**:
- Layer未作成時に警告表示
- 必要な設定手順を表示
- ゲーム実行前にエラーを発見

**LayerMask.NameToLayer()**:
```csharp
int layerIndex = LayerMask.NameToLayer("Ground");
if (layerIndex == -1) {
    Debug.LogWarning("Ground layer not found!");
}
```

---

### 5. 動的なGUI表示切り替え ⭐⭐

```csharp
// コントローラーが未設定の場合のみRoaming Area表示
if (target_cs._controller == null) {
    EditorGUILayout.PropertyField(roamingArea, new GUIContent("Roaming Area"));
    if (showHelpMovement) {
        EditorGUILayout.LabelField("The area this roams within", helpStyle);
    }
}
```

#### 🎯 学習ポイント

**文脈に応じたGUI**:
- 不要なパラメータを非表示
- Inspectorをシンプルに保つ
- ユーザーの混乱を防ぐ

**応用例**:
```csharp
// AIタイプに応じてパラメータ表示を切り替え
if (target_cs.aiType == AIType.Patrol) {
    EditorGUILayout.PropertyField(patrolPoints);
} else if (target_cs.aiType == AIType.Chase) {
    EditorGUILayout.PropertyField(chaseTarget);
}
```

---

### 6. ボタンでコンポーネント追加 ⭐

```csharp
GUI.color = helpColor;
if (GUILayout.Button("Add HerdSimDisabler Script")) {
    for (int i = 0; i < Selection.gameObjects.Length; i++) {
        HerdSimDisabler h = Selection.gameObjects[i].GetComponent<HerdSimDisabler>();
        if (h == null) {
            Selection.gameObjects[i].AddComponent<HerdSimDisabler>();
        }
    }
}
```

#### 🎯 学習ポイント

**ワンクリックセットアップ**:
- Inspector上のボタンでコンポーネント追加
- 複数選択対応（`Selection.gameObjects`）
- 既存チェックで重複防止

**応用例**:
```csharp
if (GUILayout.Button("Setup Rigidbody")) {
    Rigidbody rb = target_go.GetComponent<Rigidbody>();
    if (rb == null) rb = target_go.AddComponent<Rigidbody>();
    rb.useGravity = true;
    rb.mass = 1.0f;
}
```

---

## 参考資料

### 公式ドキュメント

- Unity Physics.Raycast: https://docs.unity3d.com/ScriptReference/Physics.Raycast.html
- Unity Animation: https://docs.unity3d.com/ScriptReference/Animation.html
- Unity LayerMask: https://docs.unity3d.com/ScriptReference/LayerMask.html

### 関連技術

- Boids（鳥の群れアルゴリズム）: https://en.wikipedia.org/wiki/Boids
- NavMesh: https://docs.unity3d.com/Manual/nav-NavigationSystem.html
- Animator Controller: https://docs.unity3d.com/Manual/class-AnimatorController.html

---

## まとめ

HerdSimアセットは、**パフォーマンス最適化**と**自然なAI行動**の両立を実現した優れた実装です。

### コアテクニック（即戦力）

特に以下の3点は、他のプロジェクトにも即座に応用可能：

1. **フレームスキップ最適化** - 大量のオブジェクト処理に必須
2. **Rigidbodyなし物理演算** - 軽量な群れシミュレーションの基本
3. **3方向Raycast回避** - NavMeshなしのパスファインディング

### ボーナステクニック（開発効率向上）

さらに、カメラ制御とエディタ拡張の実装例も含まれています：

4. **Lerpによる滑らかなカメラ移動** - 現在値と目標値の分離パターン
5. **LateUpdate()活用** - カメラジッター防止の基本
6. **CustomEditor + SerializedProperty** - 使いやすいInspector作成

これらのテクニックを活用することで、**軽量で自然な動物シミュレーション**と**快適な開発環境**をあなたのプロジェクトに実装できます。

---

**作成日**: 2025-12-23
**解析対象**: HerdSim (Unluck Software)
**解析者**: Claude Code
