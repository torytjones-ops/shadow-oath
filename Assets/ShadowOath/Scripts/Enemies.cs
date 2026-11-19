using UnityEngine;

namespace ShadowOath
{
    public sealed class NpcActor : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<NpcActor> All = new System.Collections.Generic.List<NpcActor>();

        public string DisplayName = "Villager";
        public string[] Lines = System.Array.Empty<string>();
        public bool Spoken;
        public System.Action Finished;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void Talk()
        {
            if (GameSession.InDialogue || Lines == null || Lines.Length == 0) return;
            GameController.Instance?.StartDialogue(DisplayName, Lines, () =>
            {
                Spoken = true;
                Finished?.Invoke();
            });
        }
    }

    public sealed class TrainingPost : MonoBehaviour
    {
        Vital _vital;
        float _reset;
        Quaternion _base;

        public static TrainingPost Spawn(Vector3 position, Transform parent)
        {
            var go = new GameObject("Training Post");
            go.layer = 9;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            Figure.Build(go.transform, FigureKind.Dummy, Palette.Wood, Palette.Gold);
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 1.7f;
            col.radius = 0.34f;
            col.center = new Vector3(0f, 0.9f, 0f);
            var vital = go.AddComponent<Vital>();
            vital.Max = 30f;
            vital.Current = 30f;
            vital.Team = 1;
            var post = go.AddComponent<TrainingPost>();
            post._vital = vital;
            post._base = go.transform.rotation;
            vital.Damaged += post.OnHit;
            vital.Died += post.OnBreak;
            return post;
        }

        void OnHit(HitInfo hit)
        {
            transform.rotation = _base * Quaternion.Euler(0f, 0f, hit.FromX > transform.position.x ? 12f : -12f);
            GetComponentInChildren<Tintable>()?.Flash(0.6f);
        }

        void OnBreak()
        {
            _reset = 1.4f;
            StageLook.Splash(transform.position + Vector3.up, Palette.Wood, 18, 4f, 0.25f);
            GameAudio.Instance?.Play("hit", 0.5f);
        }

        void Update()
        {
            if (_reset > 0f)
            {
                _reset -= Time.unscaledDeltaTime;
                if (_reset <= 0f && _vital != null)
                {
                    _vital.Current = _vital.Max;
                    transform.rotation = _base;
                }
            }
            else if (_vital != null && _vital.Alive)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _base, Time.deltaTime * 8f);
            }
        }
    }

    public sealed class GuardActor : MonoBehaviour
    {
        public enum Mode { Patrol, Suspicious, Investigate, Alert, Attack, Dead }

        public static readonly System.Collections.Generic.List<GuardActor> All = new System.Collections.Generic.List<GuardActor>();

        public Mode State = Mode.Patrol;
        public int Face = 1;
        public Vital VitalBody;
        public float Left = -10f;
        public float Right = 10f;
        public bool CanBeTakenDown =>
            VitalBody != null && VitalBody.Alive &&
            State != Mode.Alert && State != Mode.Attack && State != Mode.Dead &&
            _alert < 0.72f;

        CharacterController _cc;
        Figure _figure;
        Transform _cone;
        Material _coneMat;
        float _vy = -2f;
        float _alert;
        float _wait;
        float _lost = 5f;
        float _attackTime;
        bool _struck;
        bool _heavy;
        Vector3 _interest;
        float _knock;
        float _knockDir;

        public static GuardActor Spawn(Vector3 position, float left, float right, int face, Transform parent)
        {
            var go = new GameObject("Ward Guard");
            go.layer = 9;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.34f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.25f;
            var guard = go.AddComponent<GuardActor>();
            guard._cc = cc;
            guard.Left = left;
            guard.Right = right;
            guard.Face = face >= 0 ? 1 : -1;
            guard._figure = Figure.Build(go.transform, FigureKind.Guard, new Color(0.18f, 0.17f, 0.2f), Palette.Crimson);
            var vital = go.AddComponent<Vital>();
            vital.Max = 42f;
            vital.Current = 42f;
            vital.Team = 1;
            guard.VitalBody = vital;
            vital.Damaged += guard.OnDamaged;
            vital.Died += guard.OnDied;
            guard.BuildCone();
            return guard;
        }

        public static void Hear(Vector3 position, bool alertNow, float radius)
        {
            for (int i = 0; i < All.Count; i++)
            {
                var guard = All[i];
                if (guard == null || guard.State == Mode.Dead) continue;
                if (Mathf.Abs(guard.transform.position.y - position.y) > 3.2f) continue;
                if (Mathf.Abs(guard.transform.position.x - position.x) > radius) continue;
                if (alertNow) guard.BecomeAlert(position);
                else guard.Investigate(position);
            }
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void BuildCone()
        {
            var go = new GameObject("Vision");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = WedgeMesh();
            var renderer = go.AddComponent<MeshRenderer>();
            _coneMat = StageLook.Ghost(new Color(1f, 0.25f, 0.18f, 0.16f), 0.16f);
            renderer.sharedMaterial = _coneMat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _cone = go.transform;
        }

        static Mesh WedgeMesh()
        {
            const int seg = 10;
            float half = 50f * Mathf.Deg2Rad;
            var verts = new Vector3[seg + 2];
            var tris = new int[seg * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i <= seg; i++)
            {
                float angle = Mathf.Lerp(-half, half, i / (float)seg);
                verts[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            }
            for (int i = 0; i < seg; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }
            var mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        void Update()
        {
            if (VitalBody == null || State == Mode.Dead) return;
            if (GameSession.Frozen)
            {
                Pose();
                return;
            }
            float dt = Time.deltaTime;
            if (_knock > 0f) _knock -= dt;
            Think(dt);
            Pose();
            PaintCone();
        }

        void Think(float dt)
        {
            bool seesPlayer = LookForPlayer(dt);
            LookForEcho();
            if (!seesPlayer && State != Mode.Attack)
            {
                _alert = Mathf.Max(0f, _alert - 0.28f * dt);
                _lost += dt;
            }

            switch (State)
            {
                case Mode.Patrol:
                    Patrol(dt);
                    if (_alert >= 0.18f) State = Mode.Suspicious;
                    break;
                case Mode.Suspicious:
                    _wait -= dt;
                    if (_alert >= 1f) BecomeAlert(_interest);
                    else if (_alert >= 0.42f) Investigate(_interest);
                    else if (_alert <= 0.02f) State = Mode.Patrol;
                    break;
                case Mode.Investigate:
                    if (_alert >= 1f) { BecomeAlert(_interest); break; }
                    StepToward(_interest.x, 2.6f, dt);
                    if (Mathf.Abs(transform.position.x - _interest.x) < 0.45f)
                    {
                        _wait -= dt;
                        if (_wait <= 0f)
                        {
                            if (_alert < 0.2f) State = Mode.Patrol;
                            else _wait = 1.2f;
                        }
                    }
                    break;
                case Mode.Alert:
                    Chase(dt);
                    break;
                case Mode.Attack:
                    Attack(dt);
                    break;
            }
        }

        bool LookForPlayer(float dt)
        {
            var player = GameSession.Player;
            if (player == null || player.Dead) return false;
            float range = 8.4f;
            if (player.InShadow) range = GameSession.HasClan && GameSession.Def.ShadowAffinity ? 1.15f : 2.25f;
            if (!Sees(player.transform.position + Vector3.up, range)) return false;
            float rate = player.InShadow ? 0.45f : 1.05f;
            _alert = Mathf.Min(1f, _alert + rate * dt);
            _interest = player.transform.position;
            _lost = 0f;
            if (_alert >= 1f && State != Mode.Attack && State != Mode.Dead) BecomeAlert(player.transform.position);
            return true;
        }

        void LookForEcho()
        {
            var echoes = FindObjectsByType<ShadeEcho>(FindObjectsSortMode.None);
            foreach (var echo in echoes)
            {
                if (echo == null) continue;
                if (!Sees(echo.transform.position + Vector3.up, 8f)) continue;
                _interest = echo.transform.position;
                if (State == Mode.Patrol || State == Mode.Suspicious) Investigate(echo.transform.position);
            }
        }

        bool Sees(Vector3 point, float range)
        {
            Vector3 eye = transform.position + Vector3.up * 1.45f;
            Vector3 to = point - eye;
            to.z = 0f;
            float distance = to.magnitude;
            if (distance > range || distance < 0.05f) return false;
            if (Vector3.Angle(Vector3.right * Face, to) > 50f) return false;
            if (Physics.Raycast(eye, to.normalized, distance, 1, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }

        void Patrol(float dt)
        {
            float goal = Face > 0 ? Right : Left;
            if (Mathf.Abs(transform.position.x - goal) < 0.3f)
            {
                _wait -= dt;
                if (_wait <= 0f)
                {
                    Face = -Face;
                    _wait = Random.Range(0.6f, 1.3f);
                }
                Gravity(dt);
                _cc.Move(new Vector3(_knockDir * KnockSpeed(), _vy, 0f) * dt);
                LockZ();
                return;
            }
            StepToward(goal, 1.75f, dt);
        }

        void Investigate(Vector3 point)
        {
            if (State == Mode.Alert || State == Mode.Attack || State == Mode.Dead) return;
            _interest = point;
            _interest.x = Mathf.Clamp(_interest.x, Left - 2f, Right + 2f);
            State = Mode.Investigate;
            _wait = 1.7f;
            _alert = Mathf.Max(_alert, 0.35f);
        }

        public void BecomeAlert(Vector3 point)
        {
            if (State == Mode.Dead) return;
            bool fresh = State != Mode.Alert && State != Mode.Attack;
            State = Mode.Alert;
            _alert = 1f;
            _interest = point;
            _lost = 0f;
            if (fresh)
            {
                GameAudio.Instance?.Play("alert", 0.55f);
                _figure?.Tint?.Flash(0.8f);
            }
        }

        void Chase(float dt)
        {
            var player = GameSession.Player;
            float targetX = Mathf.Clamp(_interest.x, Left - 6f, Right + 6f);
            bool close = player != null && !player.Dead &&
                         Mathf.Abs(player.transform.position.x - transform.position.x) < 1.45f &&
                         Mathf.Abs(player.transform.position.y - transform.position.y) < 1.3f &&
                         _lost < 0.35f;
            if (close)
            {
                Face = player.transform.position.x >= transform.position.x ? 1 : -1;
                State = Mode.Attack;
                _attackTime = 0f;
                _struck = false;
                _heavy = Random.value < 0.28f;
                return;
            }
            if (Mathf.Abs(targetX - transform.position.x) > 0.4f) StepToward(targetX, 5.3f, dt);
            else GravityOnly(dt);
            if (_lost > 4.2f)
            {
                _alert = 0.4f;
                Investigate(_interest);
            }
        }

        void Attack(float dt)
        {
            _attackTime += dt;
            float windup = _heavy ? 0.52f : 0.36f;
            if (_attackTime < windup)
            {
                _figure?.Tint?.Flash(0.45f);
                GravityOnly(dt);
                return;
            }
            if (!_struck)
            {
                _struck = true;
                float reach = _heavy ? 1.85f : 1.6f;
                var hits = Physics.OverlapSphere(transform.position + Vector3.up, reach, 1 << 8, QueryTriggerInteraction.Collide);
                foreach (var hit in hits)
                {
                    var vital = hit.GetComponentInParent<Vital>();
                    if (vital == null || !vital.Alive || vital.Team != 0) continue;
                    float dx = vital.transform.position.x - transform.position.x;
                    if (Mathf.Abs(dx) > reach) continue;
                    if (Mathf.Sign(dx == 0f ? Face : dx) != Face && Mathf.Abs(dx) > 0.35f) continue;
                    if (vital.Receive(new HitInfo
                    {
                        Damage = _heavy ? 18f : 11f,
                        Team = 1,
                        FromX = transform.position.x,
                        Heavy = _heavy
                    }))
                    {
                        StageLook.Splash(vital.transform.position + Vector3.up, Palette.Crimson, 12, 4f, 0.2f);
                    }
                }
                GameAudio.Instance?.PlayVaried("swing", 0.5f);
            }
            if (_attackTime > windup + (_heavy ? 0.55f : 0.42f))
            {
                State = Mode.Alert;
                _attackTime = 0f;
            }
            GravityOnly(dt);
        }

        void StepToward(float x, float speed, float dt)
        {
            int dir = x >= transform.position.x ? 1 : -1;
            Face = dir;
            Gravity(dt);
            float vx = dir * speed + _knockDir * KnockSpeed();
            _cc.Move(new Vector3(vx, _vy, 0f) * dt);
            LockZ();
        }

        void GravityOnly(float dt)
        {
            Gravity(dt);
            _cc.Move(new Vector3(_knockDir * KnockSpeed(), _vy, 0f) * dt);
            LockZ();
        }

        float KnockSpeed() => _knock > 0f ? 6f : 0f;

        void Gravity(float dt)
        {
            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            else _vy += -32f * dt;
        }

        void LockZ()
        {
            float z = -transform.position.z;
            if (Mathf.Abs(z) > 0.0001f) _cc.Move(new Vector3(0f, 0f, z));
        }

        void OnDamaged(HitInfo hit)
        {
            _figure?.Tint?.Flash();
            _knock = 0.12f;
            _knockDir = transform.position.x >= hit.FromX ? 1f : -1f;
            if (!hit.Stealth) BecomeAlert(new Vector3(hit.FromX, transform.position.y, 0f));
            if (hit.Heavy && State == Mode.Attack && !_struck)
            {
                State = Mode.Alert;
                _attackTime = 0f;
            }
        }

        void OnDied()
        {
            State = Mode.Dead;
            if (_cc != null) _cc.enabled = false;
            if (_cone != null) _cone.gameObject.SetActive(false);
            if (_figure != null && _figure.Root != null)
                _figure.Root.localRotation = Quaternion.Euler(0f, Face > 0 ? 0f : 180f, 80f);
            if (!VitalBody.LastHit.Stealth)
                Hear(transform.position, true, 8f);
            StageLook.Splash(transform.position + Vector3.up * 0.4f, Palette.Ink, 16, 3f, 0.3f);
        }

        void Pose()
        {
            _figure?.Face(Face);
            if (_figure == null || _figure.Sword == null) return;
            float angle = -80f;
            if (State == Mode.Attack)
            {
                float windup = _heavy ? 0.52f : 0.36f;
                if (_attackTime < windup) angle = Mathf.Lerp(-80f, -140f, _attackTime / windup);
                else angle = Mathf.Lerp(-140f, 20f, Mathf.Clamp01((_attackTime - windup) / 0.12f));
            }
            _figure.Sword.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        void PaintCone()
        {
            if (_cone == null || _coneMat == null) return;
            var player = GameSession.Player;
            float range = 8.4f;
            if (player != null && player.InShadow)
                range = GameSession.HasClan && GameSession.Def.ShadowAffinity ? 1.15f : 2.25f;
            float sign = Face >= 0 ? 1f : -1f;
            _cone.localScale = new Vector3(range * sign, range, 1f);
            Color color = new Color(1f, 0.22f, 0.16f, 0.13f);
            if (State == Mode.Suspicious || State == Mode.Investigate) color = new Color(1f, 0.62f, 0.2f, 0.16f);
            if (State == Mode.Alert || State == Mode.Attack) color = new Color(1f, 0.12f, 0.08f, 0.22f);
            if (_coneMat.HasProperty("_BaseColor")) _coneMat.SetColor("_BaseColor", color);
        }
    }

    public sealed class RivalActor : MonoBehaviour
    {
        public Vital VitalBody;
        public int Face = -1;

        enum Phase { Approach, Windup, Strike, Recover, Dodge }

        CharacterController _cc;
        Figure _figure;
        Phase _phase = Phase.Approach;
        float _timer;
        float _vy;
        float _think = 0.4f;
        bool _heavy;
        bool _struck;
        float _knock;
        float _knockDir;
        bool _finisherSent;

        public static RivalActor Spawn(Vector3 position, Transform parent)
        {
            var go = new GameObject("Kenzo");
            go.layer = 9;
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.radius = 0.34f;
            cc.center = new Vector3(0f, 0.92f, 0f);
            var rival = go.AddComponent<RivalActor>();
            rival._cc = cc;
            rival._figure = Figure.Build(go.transform, FigureKind.Kenzo, new Color(0.12f, 0.07f, 0.08f), Palette.Crimson);
            var vital = go.AddComponent<Vital>();
            vital.Max = 176f;
            vital.Current = 176f;
            vital.Team = 1;
            rival.VitalBody = vital;
            vital.Damaged += rival.OnDamaged;
            vital.Died += rival.OnDied;
            return rival;
        }

        void Update()
        {
            if (VitalBody == null || !VitalBody.Alive) return;
            if (GameSession.Frozen && !GameSession.Cinematic)
            {
                Pose();
                return;
            }
            if (GameSession.Cinematic)
            {
                Pose();
                return;
            }
            float dt = Time.deltaTime;
            if (_knock > 0f) _knock -= dt;
            var player = GameSession.Player;
            if (player == null || player.Dead)
            {
                Gravity(dt);
                Pose();
                return;
            }
            Face = player.transform.position.x >= transform.position.x ? 1 : -1;
            bool phaseTwo = VitalBody.Current < VitalBody.Max * 0.45f;
            float dx = player.transform.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);

            switch (_phase)
            {
                case Phase.Approach:
                    _think -= dt;
                    if (player.IsStriking && dist < 2.15f && Random.value < (phaseTwo ? 0.35f : 0.18f))
                    {
                        _phase = Phase.Dodge;
                        _timer = 0.22f;
                        break;
                    }
                    if (dist > 2.55f) Step(dx > 0f ? 1 : -1, phaseTwo ? 5.4f : 4.3f, dt);
                    else if (dist < 1.25f) Step(dx > 0f ? -1 : 1, 3.2f, dt);
                    else
                    {
                        Gravity(dt);
                        Nudge(dt);
                        if (_think <= 0f)
                        {
                            _heavy = Random.value < (phaseTwo ? 0.4f : 0.25f);
                            _phase = Phase.Windup;
                            _timer = _heavy ? 0.48f : (phaseTwo ? 0.24f : 0.38f);
                            _struck = false;
                            _think = Random.Range(phaseTwo ? 0.15f : 0.28f, phaseTwo ? 0.45f : 0.7f);
                        }
                    }
                    if (phaseTwo && dist > 3.4f && dist < 7f && Random.value < dt * 0.35f)
                    {
                        _phase = Phase.Windup;
                        _timer = 0.22f;
                        _heavy = true;
                        _struck = false;
                    }
                    break;
                case Phase.Windup:
                    _timer -= dt;
                    _figure?.Tint?.Flash(0.35f);
                    Gravity(dt);
                    Nudge(dt);
                    if (_timer <= 0f)
                    {
                        _phase = Phase.Strike;
                        _timer = 0f;
                    }
                    break;
                case Phase.Strike:
                    if (!_struck)
                    {
                        _struck = true;
                        float reach = _heavy ? 1.9f : 1.55f;
                        if (dist < reach && Mathf.Sign(dx == 0 ? Face : dx) == Face &&
                            Mathf.Abs(player.transform.position.y - transform.position.y) < 1.5f)
                        {
                            player.Vital.Receive(new HitInfo
                            {
                                Damage = _heavy ? 20f : 12f,
                                Team = 1,
                                FromX = transform.position.x,
                                Heavy = _heavy
                            });
                            StageLook.Splash(player.transform.position + Vector3.up * 1.1f, Palette.Crimson, _heavy ? 18 : 10, 5f, 0.24f);
                        }
                        GameAudio.Instance?.Play(_heavy ? "heavy" : "swing", 0.7f);
                        if (_heavy) Step(Face, 7.5f, dt);
                    }
                    _timer += dt;
                    if (_struck && _timer > (_heavy ? 0.18f : 0.1f))
                    {
                        _phase = Phase.Recover;
                        _timer = _heavy ? 0.48f : 0.32f;
                    }
                    Gravity(dt);
                    break;
                case Phase.Recover:
                    _timer -= dt;
                    Gravity(dt);
                    Nudge(dt);
                    if (_timer <= 0f) _phase = Phase.Approach;
                    break;
                case Phase.Dodge:
                    _timer -= dt;
                    Step(-Face, 8.5f, dt);
                    if (_timer <= 0f) _phase = Phase.Approach;
                    break;
            }

            var p = transform.position;
            if (p.x < 1.6f || p.x > 26.4f)
            {
                p.x = Mathf.Clamp(p.x, 1.6f, 26.4f);
                transform.position = p;
            }
            Pose();
        }

        void Step(int dir, float speed, float dt)
        {
            Gravity(dt);
            float vx = dir * speed + (_knock > 0f ? _knockDir * 5f : 0f);
            _cc.Move(new Vector3(vx, _vy, 0f) * dt);
            LockZ();
        }

        void Nudge(float dt)
        {
            if (_knock > 0f) _cc.Move(new Vector3(_knockDir * 4f, 0f, 0f) * dt);
            LockZ();
        }

        void Gravity(float dt)
        {
            if (_cc.isGrounded && _vy < 0f) _vy = -2f;
            else _vy += -32f * dt;
        }

        void LockZ()
        {
            float z = -transform.position.z;
            if (Mathf.Abs(z) > 0.0001f) _cc.Move(new Vector3(0f, 0f, z));
        }

        void OnDamaged(HitInfo hit)
        {
            _figure?.Tint?.Flash();
            _knock = 0.1f;
            _knockDir = transform.position.x >= hit.FromX ? 1f : -1f;
            if (hit.Heavy && _phase == Phase.Windup)
            {
                _phase = Phase.Recover;
                _timer = 0.45f;
            }
        }

        void OnDied()
        {
            if (_finisherSent) return;
            _finisherSent = true;
            if (_cc != null) _cc.enabled = false;
            GameController.Instance?.BeginFinisher(this);
        }

        public void Collapse()
        {
            if (_figure != null && _figure.Root != null)
                _figure.Root.localRotation = Quaternion.Euler(0f, Face > 0 ? 0f : 180f, 78f);
        }

        void Pose()
        {
            _figure?.Face(Face);
            if (_figure == null || _figure.Sword == null) return;
            float angle = -75f;
            if (_phase == Phase.Windup) angle = -130f;
            else if (_phase == Phase.Strike) angle = 25f;
            else if (_phase == Phase.Dodge) angle = -30f;
            _figure.Sword.localEulerAngles = new Vector3(0f, 0f, angle);
        }
    }
}
