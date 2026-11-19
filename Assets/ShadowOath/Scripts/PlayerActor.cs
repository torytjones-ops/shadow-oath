using System.Collections.Generic;
using UnityEngine;

namespace ShadowOath
{
    public sealed class PlayerActor : MonoBehaviour
    {
        public Vital Vital;
        public Figure Body;
        public int Face = 1;
        public Vector3 Respawn;

        public bool InShadow { get; private set; }
        public bool Dead => Vital == null || !Vital.Alive;
        public bool InCombat => _combatTimer > 0f;
        public bool IsStriking { get; private set; }
        public bool FocusActive { get; private set; }
        public float Seiki { get; private set; }
        public float SeikiMax => 100f;
        public float Focus { get; private set; }
        public float FocusMax => 100f;
        public string Prompt { get; private set; }

        CharacterController _cc;
        float _vy = -2f;
        float _coyote;
        float _jumpBuffer;
        float _dodgeLeft;
        float _dodgeCd;
        int _dodgeDir = 1;
        float _wallTime;
        bool _wallSpent;
        float _seikiDelay;
        float _focusDelay;
        float _stoneCd;
        float _combatTimer;
        float _fallLock;
        float _knock;
        float _knockDir = 1f;
        bool _attacking;
        bool _queueLight;
        bool _didHit;
        bool _wavePending;
        Attack _atk;
        float _atkTime;
        readonly HashSet<int> _already = new HashSet<int>();

        struct Attack
        {
            public float Windup, Active, Recover, Damage, Reach;
            public bool Heavy, Takedown, Wave;
            public int Step;
        }

        public static PlayerActor Spawn(Vector3 position, Transform parent)
        {
            var go = new GameObject("Haru");
            go.layer = 8;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.32f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.28f;
            cc.slopeLimit = 55f;
            cc.skinWidth = 0.05f;
            var actor = go.AddComponent<PlayerActor>();
            actor._cc = cc;
            var clan = GameSession.Def;
            var figure = Figure.Build(go.transform, FigureKind.Haru, clan.Robe, clan.Accent);
            var vital = go.AddComponent<Vital>();
            vital.Max = 100f;
            vital.Current = 100f;
            vital.Team = 0;
            actor.Bind(vital, figure);
            GameSession.Player = actor;
            return actor;
        }

        public void Bind(Vital vital, Figure figure)
        {
            Vital = vital;
            Body = figure;
            Seiki = SeikiMax;
            Focus = FocusMax;
            Respawn = transform.position;
            vital.Damaged += OnDamaged;
            vital.Died += OnDied;
        }

        void Awake()
        {
            if (_cc == null) _cc = GetComponent<CharacterController>();
        }

        void OnDestroy()
        {
            if (GameSession.Player == this) GameSession.Player = null;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (GameSession.InputGrace > 0f) GameSession.InputGrace -= dt;
            RefreshShadow();
            Prompt = "";
            if (_combatTimer > 0f) _combatTimer -= dt;
            if (_fallLock > 0f) _fallLock -= dt;
            if (_stoneCd > 0f) _stoneCd -= dt;
            if (_dodgeCd > 0f) _dodgeCd -= dt;
            if (_seikiDelay > 0f) _seikiDelay -= dt;
            if (_focusDelay > 0f) _focusDelay -= dt;

            if (Dead)
            {
                FocusActive = false;
                Pose();
                return;
            }

            if (GameSession.Paused || GameSession.Cinematic)
            {
                FocusActive = false;
                Pose();
                return;
            }

            if (!GameSession.AcceptsGameplay)
            {
                FocusActive = false;
                FallAndStick(dt);
                Pose();
                return;
            }

            var input = GameInputReader.Instance;
            if (input == null) return;

            bool wantFocus = input.FocusHeld && Focus > 0.5f;
            if (wantFocus)
            {
                Focus = Mathf.Max(0f, Focus - GameSession.Def.FocusDrain * dt);
                FocusActive = Focus > 0.5f;
                _focusDelay = 0.45f;
            }
            else
            {
                FocusActive = false;
                if (_focusDelay <= 0f) Focus = Mathf.Min(FocusMax, Focus + 11f * dt);
            }

            if (_seikiDelay <= 0f) Seiki = Mathf.Min(SeikiMax, Seiki + GameSession.Def.SeikiRegen * dt);

            int dir = 0;
            if (input.Move.x > 0.22f) dir = 1;
            else if (input.Move.x < -0.22f) dir = -1;

            if (input.JumpPressed) _jumpBuffer = 0.13f;
            else _jumpBuffer -= dt;

            bool grounded = _cc.isGrounded;
            if (grounded)
            {
                _coyote = 0.13f;
                _wallTime = 0f;
                _wallSpent = false;
                if (_vy < 0f) _vy = -2.2f;
            }
            else _coyote -= dt;

            if (GameSession.InputGrace <= 0f)
                TryActions(input, dir);

            if (_dodgeLeft > 0f)
            {
                _dodgeLeft -= dt;
                _vy = 0f;
                Slide(_dodgeDir * GameSession.Def.DodgeSpeed, 0f, dt);
            }
            else
            {
                TickAttack(dt);
                Locomote(input, dir, grounded, dt);
            }

            if (transform.position.y < -8f) FallRespawn();
            ComposePrompt(dir);
            Pose();
        }

        void TryActions(GameInputReader input, int dir)
        {
            var takedown = FindTakedown();
            if (input.DodgePressed && _dodgeCd <= 0f && !(_attacking && _atk.Takedown))
            {
                BeginDodge(dir != 0 ? dir : Face);
                return;
            }
            if (_dodgeLeft > 0f || (_attacking && _atk.Takedown)) return;

            if (_attacking && input.LightPressed && _atk.Step > 0 && _atk.Step < 3)
                _queueLight = true;

            if (_attacking) return;

            if ((input.TakedownPressed || input.LightPressed) && takedown != null)
            {
                BeginTakedown(takedown);
                return;
            }

            var npc = NearNpc();
            var gate = NearGate();
            bool talkContext = npc != null || (gate != null && gate.Ready);
            if (input.InteractPressed || (input.HeavyPressed && talkContext))
            {
                if (npc != null)
                {
                    npc.Talk();
                    return;
                }
                if (gate != null && gate.Ready)
                {
                    GameController.Instance?.EnterMission();
                    return;
                }
            }

            if (input.LightPressed)
            {
                BeginLight(1);
                return;
            }
            if (input.HeavyPressed)
            {
                BeginHeavy();
                return;
            }
            if (input.WaterPressed)
            {
                TryWater();
                return;
            }
            if (input.ClonePressed)
            {
                TryClone();
                return;
            }
            if (input.DistractPressed) TryStone();
        }

        void Locomote(GameInputReader input, int dir, bool grounded, float dt)
        {
            bool lockFacing = _attacking && (_atk.Heavy || _atk.Takedown);
            if (!lockFacing && dir != 0) Face = dir;

            bool wall = !_wallSpent && !grounded && dir != 0 && ProbeWall(dir, out _);
            bool wallRun = wall && !_attacking;

            if (wallRun && _wallTime < 0.9f)
            {
                _wallTime += dt;
                _vy = 7.8f;
                Prompt = "Jump to kick off the wall";
                if (_jumpBuffer > 0f)
                {
                    Face = -dir;
                    _vy = 13.4f;
                    _jumpBuffer = 0f;
                    _coyote = 0f;
                    _wallSpent = true;
                    _wallTime = 1f;
                    Slide(-dir * 9.4f, _vy, dt);
                    GameAudio.Instance?.Play("jump", 0.55f, 1.15f);
                    return;
                }
                Slide(dir * 1.35f, _vy, dt);
                return;
            }

            if (_jumpBuffer > 0f && _coyote > 0f && !(_attacking && _atk.Takedown))
            {
                _vy = input.JumpHeld ? 11.6f : 11.6f;
                _jumpBuffer = 0f;
                _coyote = 0f;
                grounded = false;
                GameAudio.Instance?.Play("jump", 0.4f);
                if (!InShadow) GuardActor.Hear(transform.position, false, 4.2f);
            }
            else
            {
                float gravity = -34f;
                if (!input.JumpHeld && _vy > 0f) gravity *= 2.15f;
                _vy += gravity * dt;
                if (grounded && _vy < 0f) _vy = -2.2f;
            }

            float speed = (InShadow && GameSession.Def.ShadowAffinity) ? 9.15f : 7.35f;
            if (_attacking) speed *= _atk.Heavy ? 0.15f : 0.42f;
            float vx = dir * speed;
            if (_attacking && _atk.Takedown) vx = 0f;
            Slide(vx, _vy, dt);
        }

        void FallAndStick(float dt)
        {
            if (_cc.isGrounded && _vy < 0f) _vy = -2.2f;
            else _vy += -34f * dt;
            Slide(0f, _vy, dt);
        }

        void Slide(float vx, float vy, float dt)
        {
            _vy = vy;
            if (_knock > 0f)
            {
                vx += _knockDir * 8f;
                _knock -= dt;
            }
            _cc.Move(new Vector3(vx, vy, 0f) * dt);
            float zFix = -transform.position.z;
            if (Mathf.Abs(zFix) > 0.0001f) _cc.Move(new Vector3(0f, 0f, zFix));
            if (!_attacking || !_atk.Takedown) Separate();
        }

        void Separate()
        {
            for (int i = 0; i < Vital.All.Count; i++)
            {
                var other = Vital.All[i];
                if (other == null || other == Vital || other.Team == 0 || !other.Alive) continue;
                Vector3 delta = transform.position - other.transform.position;
                delta.y = 0f;
                delta.z = 0f;
                float distance = delta.magnitude;
                if (distance < 0.74f && distance > 0.001f)
                    _cc.Move(delta.normalized * (0.74f - distance));
            }
        }

        bool ProbeWall(int dir, out RaycastHit hit)
        {
            hit = default;
            Vector3 origin = transform.position + Vector3.up * 1.05f;
            if (!Physics.Raycast(origin, Vector3.right * dir, out hit, 0.62f, 1, QueryTriggerInteraction.Ignore))
                return false;
            if (hit.collider.bounds.size.y < 2.5f) return false;
            if (hit.normal.x * dir > -0.45f) return false;
            return true;
        }

        void BeginDodge(int dir)
        {
            _dodgeDir = dir >= 0 ? 1 : -1;
            Face = _dodgeDir;
            _dodgeLeft = GameSession.Def.DodgeDuration;
            _dodgeCd = 0.52f;
            if (Vital != null) Vital.InvulnerableTimer = Mathf.Max(Vital.InvulnerableTimer, GameSession.Def.DodgeInvuln);
            _attacking = false;
            _queueLight = false;
            _wavePending = false;
            IsStriking = false;
            GameAudio.Instance?.Play("swing", 0.28f, 1.35f);
            StageLook.Splash(transform.position + Vector3.up * 0.7f, GameSession.Def.Accent, 8, 3.2f, 0.12f);
        }

        void BeginLight(int step)
        {
            _attacking = true;
            _atkTime = 0f;
            _didHit = false;
            _wavePending = false;
            _queueLight = false;
            _already.Clear();
            if (step <= 1)
                _atk = new Attack { Windup = 0.05f, Active = 0.1f, Recover = 0.13f, Damage = 9f, Reach = 1.15f, Step = 1 };
            else if (step == 2)
                _atk = new Attack { Windup = 0.045f, Active = 0.1f, Recover = 0.12f, Damage = 12f, Reach = 1.28f, Step = 2 };
            else
                _atk = new Attack { Windup = 0.07f, Active = 0.13f, Recover = 0.24f, Damage = 18f, Reach = 1.5f, Step = 3 };
            GuardActor.Hear(transform.position, false, 4.5f);
        }

        void BeginHeavy()
        {
            _attacking = true;
            _atkTime = 0f;
            _didHit = false;
            _queueLight = false;
            _wavePending = false;
            _already.Clear();
            _atk = new Attack { Windup = 0.26f, Active = 0.12f, Recover = 0.34f, Damage = 26f, Reach = 1.65f, Heavy = true };
            GuardActor.Hear(transform.position, false, 6f);
        }

        void BeginTakedown(GuardActor guard)
        {
            _attacking = true;
            _atkTime = 0f;
            _didHit = false;
            _queueLight = false;
            _wavePending = false;
            _already.Clear();
            _atk = new Attack { Windup = 0.06f, Active = 0.12f, Recover = 0.28f, Damage = 999f, Reach = 1.2f, Takedown = true };
            _takedown = guard;
        }

        GuardActor _takedown;

        void TryWater()
        {
            float cost = GameSession.Def.WaterCost;
            if (Seiki < cost)
            {
                GameAudio.Instance?.Play("deny", 0.55f);
                return;
            }
            Seiki -= cost;
            _seikiDelay = 0.45f;
            _attacking = true;
            _atkTime = 0f;
            _didHit = true;
            _queueLight = false;
            _wavePending = true;
            _atk = new Attack { Windup = 0.12f, Active = 0.02f, Recover = 0.18f, Wave = true };
            _combatTimer = 2.2f;
        }

        void TryClone()
        {
            float cost = GameSession.Def.CloneCost;
            if (Seiki < cost || ShadeEcho.ActiveCount >= 1)
            {
                GameAudio.Instance?.Play("deny", 0.55f);
                return;
            }
            Seiki -= cost;
            _seikiDelay = 0.35f;
            ShadeEcho.Spawn(transform.position + Vector3.right * Face * 0.9f, Face);
            _combatTimer = 2f;
        }

        void TryStone()
        {
            if (_stoneCd > 0f) return;
            _stoneCd = 0.62f;
            ThrownStone.Spawn(transform.position + Vector3.up * 1.35f + Vector3.right * Face * 0.3f, Face);
        }

        void TickAttack(float dt)
        {
            if (!_attacking)
            {
                IsStriking = false;
                return;
            }
            _atkTime += dt;
            float activeEnd = _atk.Windup + _atk.Active;
            IsStriking = !_atk.Wave && !_atk.Takedown && _atkTime <= activeEnd;
            if (_wavePending && _atkTime >= _atk.Windup)
            {
                _wavePending = false;
                WaterWave.Spawn(transform.position + Vector3.up * 1.15f + Vector3.right * Face * 0.55f, Face, GameSession.Def.WaterDamage);
                _combatTimer = 2.5f;
            }
            if (!_didHit && _atkTime >= _atk.Windup && _atkTime <= activeEnd + 0.02f)
            {
                _didHit = true;
                if (_atk.Takedown) CommitTakedown();
                else if (!_atk.Wave) DoSwing();
            }
            if (_atkTime >= activeEnd + _atk.Recover)
            {
                _attacking = false;
                IsStriking = false;
                if (_queueLight && _atk.Step > 0 && _atk.Step < 3)
                {
                    int next = _atk.Step + 1;
                    _queueLight = false;
                    BeginLight(next);
                }
            }
        }

        void DoSwing()
        {
            Vector3 origin = transform.position + Vector3.up * 1.05f + Vector3.right * Face * (_atk.Reach * 0.62f);
            var hits = Physics.OverlapSphere(origin, 0.48f, (1 << 9) | (1 << 15), QueryTriggerInteraction.Collide);
            bool connected = false;
            foreach (var hit in hits)
            {
                var bell = hit.GetComponentInParent<Bell>();
                if (bell != null && _already.Add(bell.GetInstanceID()))
                {
                    bell.Ring(origin);
                    continue;
                }
                var vital = hit.GetComponentInParent<Vital>();
                if (vital == null || !_already.Add(vital.GetInstanceID())) continue;
                float dx = vital.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > _atk.Reach + 0.35f) continue;
                if (Mathf.Sign(dx == 0f ? Face : dx) != Face && Mathf.Abs(dx) > 0.4f) continue;
                if (!vital.Receive(new HitInfo
                {
                    Damage = _atk.Damage,
                    Team = 0,
                    FromX = transform.position.x,
                    Heavy = _atk.Heavy
                })) continue;
                connected = true;
                StageLook.Splash(vital.transform.position + Vector3.up * 1.15f, _atk.Heavy ? Palette.Ink : Palette.Paper, _atk.Heavy ? 24 : 12, _atk.Heavy ? 7f : 5f, _atk.Heavy ? 0.38f : 0.2f);
            }
            if (connected)
            {
                GameAudio.Instance?.PlayVaried("hit", _atk.Heavy ? 0.9f : 0.62f);
                GameSession.NoteHit();
                SideCamera.Instance?.Shake(_atk.Heavy ? 0.75f : 0.3f);
                GameInputReader.Instance?.Rumble(_atk.Heavy ? 0.4f : 0.12f, _atk.Heavy ? 0.75f : 0.4f, _atk.Heavy ? 0.12f : 0.06f);
                GuardActor.Hear(transform.position, true, _atk.Heavy ? 12f : 9f);
                _combatTimer = 2.6f;
            }
            else GameAudio.Instance?.PlayVaried("swing", _atk.Heavy ? 0.7f : 0.45f);
        }

        void CommitTakedown()
        {
            var guard = _takedown;
            _takedown = null;
            if (guard == null || !guard.CanBeTakenDown)
            {
                GameAudio.Instance?.PlayVaried("swing", 0.4f);
                return;
            }
            Vector3 pos = guard.transform.position - Vector3.right * guard.Face * 0.75f;
            pos.y = transform.position.y;
            pos.z = 0f;
            Warp(pos);
            Face = guard.Face;
            guard.VitalBody.Receive(new HitInfo
            {
                Damage = 999f,
                Team = 0,
                FromX = transform.position.x,
                Stealth = true
            });
            StageLook.Splash(guard.transform.position + Vector3.up * 1.2f, Palette.Ink, 30, 7.5f, 0.42f);
            StageLook.Splash(guard.transform.position + Vector3.up * 1.0f, Palette.Paper, 10, 3f, 0.16f);
            GameAudio.Instance?.Play("takedown", 0.95f);
            GameInputReader.Instance?.Rumble(0.25f, 0.8f, 0.2f);
            SideCamera.Instance?.Shake(0.6f);
            Focus = Mathf.Min(FocusMax, Focus + GameSession.Def.TakedownFocus);
            if (Vital != null) Vital.InvulnerableTimer = Mathf.Max(Vital.InvulnerableTimer, 0.4f);
            GameSession.NoteHit();
        }

        void OnDamaged(HitInfo hit)
        {
            if (Vital != null) Vital.InvulnerableTimer = Mathf.Max(Vital.InvulnerableTimer, 0.55f);
            _vy = 6f;
            _knock = 0.14f;
            _knockDir = transform.position.x >= hit.FromX ? 1f : -1f;
            _attacking = false;
            _queueLight = false;
            _wavePending = false;
            IsStriking = false;
            Body?.Tint?.Flash();
            GameAudio.Instance?.Play("hurt", 0.85f);
            StageLook.Splash(transform.position + Vector3.up * 1.1f, Palette.Crimson, 16, 4.5f, 0.24f);
            SideCamera.Instance?.Shake(0.6f);
            GameInputReader.Instance?.Rumble(0.55f, 0.25f, 0.14f);
            GameUi.Instance?.HurtFlash();
            _combatTimer = 3f;
        }

        void OnDied()
        {
            FocusActive = false;
            GameAudio.Instance?.Play("death", 0.9f);
            StageLook.Splash(transform.position + Vector3.up, Palette.Ink, 32, 6.5f, 0.5f);
            GameController.Instance?.OnPlayerDied();
        }

        public void FallRespawn()
        {
            if (_fallLock > 0f || Dead) return;
            _fallLock = 0.5f;
            Vital?.Receive(new HitInfo { Damage = 8f, Team = 1, FromX = transform.position.x });
            if (Dead) return;
            Warp(Respawn);
            _vy = 0f;
            _attacking = false;
        }

        public void Warp(Vector3 position)
        {
            position.z = 0f;
            _cc.enabled = false;
            transform.position = position;
            _cc.enabled = true;
        }

        void RefreshShadow()
        {
            var hits = Physics.OverlapCapsule(
                transform.position + Vector3.up * 0.25f,
                transform.position + Vector3.up * 1.5f,
                0.28f,
                1 << 12,
                QueryTriggerInteraction.Collide);
            InShadow = false;
            foreach (var hit in hits)
            {
                if (hit.GetComponent<ShadowVolume>() != null)
                {
                    InShadow = true;
                    break;
                }
            }
        }

        GuardActor FindTakedown()
        {
            GuardActor best = null;
            float bestDist = 1.55f;
            for (int i = 0; i < GuardActor.All.Count; i++)
            {
                var guard = GuardActor.All[i];
                if (guard == null || !guard.CanBeTakenDown) continue;
                if (Mathf.Abs(transform.position.y - guard.transform.position.y) > 1.25f) continue;
                float dx = transform.position.x - guard.transform.position.x;
                if (Mathf.Abs(dx) < 0.15f) continue;
                if (Mathf.Sign(dx) == guard.Face) continue;
                float adx = Mathf.Abs(dx);
                if (adx < bestDist)
                {
                    bestDist = adx;
                    best = guard;
                }
            }
            return best;
        }

        NpcActor NearNpc()
        {
            NpcActor best = null;
            float bestDist = 1.85f;
            for (int i = 0; i < NpcActor.All.Count; i++)
            {
                var npc = NpcActor.All[i];
                if (npc == null) continue;
                float dist = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.y),
                    new Vector2(npc.transform.position.x, npc.transform.position.y));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = npc;
                }
            }
            return best;
        }

        DepartGate NearGate()
        {
            DepartGate best = null;
            float bestDist = 2.1f;
            for (int i = 0; i < DepartGate.All.Count; i++)
            {
                var gate = DepartGate.All[i];
                if (gate == null) continue;
                float dist = Mathf.Abs(transform.position.x - gate.transform.position.x);
                if (dist < bestDist && Mathf.Abs(transform.position.y - gate.transform.position.y) < 2f)
                {
                    bestDist = dist;
                    best = gate;
                }
            }
            return best;
        }

        void ComposePrompt(int dir)
        {
            if (!string.IsNullOrEmpty(Prompt)) return;
            var takedown = FindTakedown();
            if (takedown != null)
            {
                Prompt = "Silent takedown";
                return;
            }
            var npc = NearNpc();
            if (npc != null)
            {
                Prompt = "Speak with " + npc.DisplayName;
                return;
            }
            var gate = NearGate();
            if (gate != null)
            {
                if (!GameSession.ElderSpoken) Prompt = "Elder Senae is waiting by the shrine";
                else if (!GameSession.ScoutSpoken) Prompt = "Speak with Toma before you leave";
                else Prompt = "Depart for the moonlit gate";
            }
        }

        void Pose()
        {
            Body?.Face(Face);
            if (Body == null || Body.Sword == null) return;
            float angle = -68f;
            if (_attacking)
            {
                float swing;
                if (_atkTime < _atk.Windup)
                    swing = -Mathf.Lerp(0f, _atk.Heavy ? 70f : 48f, _atkTime / Mathf.Max(0.01f, _atk.Windup));
                else
                    swing = Mathf.Lerp(-48f, _atk.Heavy ? 120f : 86f, Mathf.Clamp01((_atkTime - _atk.Windup) / 0.14f));
                angle = -68f + swing;
            }
            else if (_dodgeLeft > 0f) angle = -15f;
            Body.Sword.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }
}
