using System.Collections.Generic;
using UnityEngine;

namespace ShadowOath
{
    public sealed class Vital : MonoBehaviour
    {
        public static readonly List<Vital> All = new List<Vital>();

        public float Max = 100f;
        public float Current = 100f;
        public int Team;
        public bool Alive => Current > 0f;
        public float InvulnerableTimer;
        public HitInfo LastHit;
        public event System.Action<HitInfo> Damaged;
        public event System.Action Died;

        void OnEnable() => All.Add(this);

        void OnDisable() => All.Remove(this);

        void Update()
        {
            if (GameSession.Paused) return;
            if (InvulnerableTimer > 0f)
                InvulnerableTimer -= Time.unscaledDeltaTime;
        }

        public bool Receive(HitInfo hit)
        {
            if (!Alive) return false;
            if (InvulnerableTimer > 0f) return false;
            if (hit.Team == Team) return false;
            LastHit = hit;
            Current = Mathf.Max(0f, Current - hit.Damage);
            Damaged?.Invoke(hit);
            if (Current <= 0f) Died?.Invoke();
            return true;
        }

        public static Vital Nearest(Vector3 position, int team, float maxDistance, Vital ignore = null)
        {
            Vital best = null;
            float bestDistance = maxDistance;
            for (int i = 0; i < All.Count; i++)
            {
                var vital = All[i];
                if (vital == null || vital == ignore || !vital.Alive || vital.Team != team) continue;
                float distance = Mathf.Abs(vital.transform.position.x - position.x);
                if (Mathf.Abs(vital.transform.position.y - position.y) > 2.2f) continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = vital;
                }
            }
            return best;
        }
    }

    public sealed class ShadowVolume : MonoBehaviour
    {
    }

    public sealed class Checkpoint : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerActor>();
            if (player != null) player.Respawn = transform.position;
        }
    }

    public sealed class Pit : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<PlayerActor>();
            if (player != null) player.FallRespawn();
        }
    }

    public sealed class HintZone : MonoBehaviour
    {
        public string Text;

        void Start()
        {
            Physics.SyncTransforms();
            var box = GetComponent<BoxCollider>();
            var player = GameSession.Player;
            if (box == null || player == null) return;
            if (box.bounds.Contains(player.transform.position) || box.bounds.Contains(player.transform.position + Vector3.up))
                Show(box);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PlayerActor>() == null) return;
            var box = GetComponent<BoxCollider>();
            if (box != null) Show(box);
        }

        void Show(Collider box)
        {
            if (box == null || !box.enabled) return;
            if (GameUi.Instance != null) GameUi.Instance.ShowHint(Text);
            box.enabled = false;
        }
    }

    public sealed class DepartGate : MonoBehaviour
    {
        public static readonly List<DepartGate> All = new List<DepartGate>();
        public bool Ready => GameSession.ElderSpoken && GameSession.ScoutSpoken;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
    }

    public sealed class DuelGate : MonoBehaviour
    {
        bool _used;

        void OnTriggerEnter(Collider other)
        {
            if (_used) return;
            if (other.GetComponent<PlayerActor>() == null) return;
            _used = true;
            if (GameController.Instance != null) GameController.Instance.EnterDuel();
        }
    }

    public sealed class Bell : MonoBehaviour
    {
        float _cool;

        void Update()
        {
            if (_cool > 0f) _cool -= Time.deltaTime;
        }

        public void Ring(Vector3 from)
        {
            if (_cool > 0f) return;
            _cool = 1.2f;
            GameAudio.Instance?.Play("bell", 0.7f);
            StageLook.Splash(transform.position + Vector3.up * 0.4f, Palette.Gold, 14, 3f, 0.18f);
            GuardActor.Hear(transform.position, false, 18f);
        }
    }

    public sealed class Waystone : MonoBehaviour
    {
        float _baseY;

        void Awake() => _baseY = transform.position.y;

        void Update()
        {
            if (GameSession.Paused) return;
            var position = transform.position;
            position.y = _baseY + Mathf.Sin(Time.time * 2f) * 0.15f;
            transform.position = position;
            transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }
    }

    public sealed class ThrownStone : MonoBehaviour
    {
        Vector3 _velocity;
        float _life = 1.6f;

        public static void Spawn(Vector3 position, int direction)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Stone";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.16f;
            go.GetComponent<Renderer>().sharedMaterial = StageLook.Lit(Palette.StoneLight);
            var stone = go.AddComponent<ThrownStone>();
            stone._velocity = new Vector3(direction * 11.5f, 6.2f, 0f);
            if (GameSession.LevelRoot != null) go.transform.SetParent(GameSession.LevelRoot, true);
            GameAudio.Instance?.PlayVaried("swing", 0.25f);
        }

        void Update()
        {
            if (GameSession.Paused) return;
            float dt = Time.unscaledDeltaTime;
            _velocity.y += -20f * dt;
            Vector3 next = transform.position + _velocity * dt;
            Vector3 delta = next - transform.position;
            float distance = delta.magnitude;
            if (distance > 0.001f && Physics.Raycast(transform.position, delta.normalized, out var hit, distance + 0.05f, ~((1 << 8) | (1 << 12)), QueryTriggerInteraction.Ignore))
            {
                var bell = hit.collider.GetComponentInParent<Bell>();
                if (bell != null) bell.Ring(hit.point);
                else
                {
                    GameAudio.Instance?.Play("stone", 0.65f);
                    StageLook.Splash(hit.point, Palette.Paper, 8, 2.5f, 0.12f);
                    GuardActor.Hear(hit.point, false, 11f);
                }
                Destroy(gameObject);
                return;
            }
            transform.position = next;
            _life -= dt;
            if (_life <= 0f || transform.position.y < -6f)
            {
                GuardActor.Hear(transform.position, false, 8f);
                GameAudio.Instance?.Play("stone", 0.4f);
                Destroy(gameObject);
            }
        }
    }

    public sealed class WaterWave : MonoBehaviour
    {
        public int Direction = 1;
        public float Damage = 28f;
        public int Team;
        float _life = 0.62f;
        readonly HashSet<int> _hit = new HashSet<int>();

        public static void Spawn(Vector3 position, int direction, float damage)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Still Current";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.position = position;
            go.transform.localScale = new Vector3(1.3f, 0.55f, 0.4f);
            go.GetComponent<Renderer>().sharedMaterial = StageLook.Ghost(new Color(0.55f, 0.95f, 1f, 0.75f), 0.75f);
            var wave = go.AddComponent<WaterWave>();
            wave.Direction = direction >= 0 ? 1 : -1;
            wave.Damage = damage;
            wave.Team = 0;
            if (GameSession.LevelRoot != null) go.transform.SetParent(GameSession.LevelRoot, true);
            GameAudio.Instance?.Play("water", 0.75f);
            StageLook.Splash(position, Palette.Water, 16, 4f, 0.22f);
        }

        void Update()
        {
            if (GameSession.Paused) return;
            float dt = Time.unscaledDeltaTime;
            transform.position += Vector3.right * Direction * 17f * dt;
            transform.localScale = new Vector3(1.5f + (0.62f - _life), 0.5f, 0.35f);
            Strike();
            _life -= dt;
            if (_life <= 0f) Destroy(gameObject);
        }

        void Strike()
        {
            var hits = Physics.OverlapSphere(transform.position, 0.7f, (1 << 9) | (1 << 15), QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                var bell = hit.GetComponentInParent<Bell>();
                if (bell != null)
                {
                    int id = bell.GetInstanceID();
                    if (_hit.Add(id)) bell.Ring(transform.position);
                    continue;
                }
                var vital = hit.GetComponentInParent<Vital>();
                if (vital == null || !_hit.Add(vital.GetInstanceID())) continue;
                if (vital.Receive(new HitInfo
                {
                    Damage = Damage,
                    Team = Team,
                    FromX = transform.position.x,
                    Heavy = true
                }))
                {
                    StageLook.Splash(vital.transform.position + Vector3.up, Palette.Paper, 18, 6f, 0.32f);
                    GameAudio.Instance?.PlayVaried("hit", 0.7f);
                    GameSession.NoteHit();
                }
            }
        }
    }

    public sealed class ShadeEcho : MonoBehaviour
    {
        public static int ActiveCount { get; private set; }

        Vital _vital;
        Figure _figure;
        int _face = 1;
        float _life = 4.2f;
        float _attack;
        float _hitTimer;
        bool _swinging;

        public static ShadeEcho Spawn(Vector3 position, int face)
        {
            var go = new GameObject("Mirror Shade");
            go.layer = 8;
            if (GameSession.LevelRoot != null) go.transform.SetParent(GameSession.LevelRoot, true);
            go.transform.position = position;
            var echo = go.AddComponent<ShadeEcho>();
            echo._face = face >= 0 ? 1 : -1;
            echo._figure = Figure.Build(go.transform, FigureKind.Echo, new Color(0.18f, 0.22f, 0.38f), Palette.Water);
            var vital = go.AddComponent<Vital>();
            vital.Max = 18f;
            vital.Current = 18f;
            vital.Team = 0;
            echo._vital = vital;
            vital.Died += echo.Pop;
            var body = go.AddComponent<CapsuleCollider>();
            body.height = 1.7f;
            body.radius = 0.32f;
            body.center = new Vector3(0f, 0.9f, 0f);
            body.isTrigger = true;
            ActiveCount++;
            GameAudio.Instance?.Play("clone", 0.8f);
            StageLook.Splash(position + Vector3.up, Palette.Indigo, 20, 4f, 0.25f);
            GuardActor.Hear(position, false, 14f);
            return echo;
        }

        void OnDestroy()
        {
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }

        void Update()
        {
            if (GameSession.Paused || GameSession.Cinematic) return;
            if (_vital != null && !_vital.Alive) return;
            float dt = Time.unscaledDeltaTime;
            _life -= dt;
            if (_life <= 0f)
            {
                Pop();
                return;
            }
            var target = Vital.Nearest(transform.position, 1, 16f);
            if (target != null)
            {
                float dx = target.transform.position.x - transform.position.x;
                _face = dx >= 0f ? 1 : -1;
                if (Mathf.Abs(dx) > 1.15f)
                    transform.position += Vector3.right * _face * 6.2f * dt;
                else
                {
                    _attack -= dt;
                    if (_attack <= 0f)
                    {
                        _attack = 0.7f;
                        _swinging = true;
                        _hitTimer = 0.08f;
                        GameAudio.Instance?.PlayVaried("swing", 0.35f);
                    }
                }
            }
            if (_swinging)
            {
                _hitTimer -= dt;
                if (_hitTimer <= 0f)
                {
                    _swinging = false;
                    var victim = Vital.Nearest(transform.position, 1, 1.7f);
                    if (victim != null && victim.Receive(new HitInfo
                    {
                        Damage = 8f,
                        Team = 0,
                        FromX = transform.position.x
                    }))
                    {
                        StageLook.Splash(victim.transform.position + Vector3.up, Palette.Paper, 10, 4f, 0.18f);
                        GameAudio.Instance?.PlayVaried("hit", 0.45f);
                        GuardActor.Hear(transform.position, true, 10f);
                    }
                }
            }
            _figure?.Face(_face);
            if (_figure != null && _figure.Sword != null && _swinging)
                _figure.Sword.localEulerAngles = new Vector3(0f, 0f, 30f);
            else if (_figure != null && _figure.Sword != null)
                _figure.Sword.localEulerAngles = new Vector3(0f, 0f, -70f);

            var position = transform.position;
            position.z = 0f;
            transform.position = position;
        }

        bool _gone;

        void Pop()
        {
            if (_gone) return;
            _gone = true;
            StageLook.Splash(transform.position + Vector3.up, new Color(0.7f, 0.8f, 1f), 22, 5f, 0.3f);
            Destroy(gameObject);
        }
    }
}
