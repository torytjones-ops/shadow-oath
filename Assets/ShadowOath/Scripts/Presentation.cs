using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowOath
{
    public static class Palette
    {
        public static readonly Color Night = new Color(0.035f, 0.042f, 0.07f);
        public static readonly Color Ink = new Color(0.015f, 0.016f, 0.02f);
        public static readonly Color Paper = new Color(0.93f, 0.90f, 0.84f);
        public static readonly Color Moon = new Color(0.96f, 0.94f, 0.88f);
        public static readonly Color Crimson = new Color(0.72f, 0.16f, 0.14f);
        public static readonly Color Gold = new Color(0.86f, 0.66f, 0.34f);
        public static readonly Color Water = new Color(0.28f, 0.78f, 0.82f);
        public static readonly Color Wood = new Color(0.29f, 0.18f, 0.12f);
        public static readonly Color WoodDark = new Color(0.16f, 0.10f, 0.08f);
        public static readonly Color Stone = new Color(0.20f, 0.21f, 0.25f);
        public static readonly Color StoneLight = new Color(0.32f, 0.33f, 0.36f);
        public static readonly Color Pine = new Color(0.07f, 0.11f, 0.09f);
        public static readonly Color Indigo = new Color(0.10f, 0.12f, 0.22f);
        public static readonly Color Roof = new Color(0.12f, 0.08f, 0.10f);
        public static readonly Color Vermilion = new Color(0.75f, 0.22f, 0.16f);
        public static readonly Color Fog = new Color(0.045f, 0.05f, 0.085f);
    }

    public static class StageLook
    {
        static Shader _lit;
        static Shader _unlit;
        static Shader _outline;
        static Shader _particle;
        static Material _outlineMat;
        static Material _particleMat;
        static bool _ready;

        public static void Init()
        {
            if (_ready) return;
            _lit = Shader.Find("Universal Render Pipeline/Lit");
            _unlit = Shader.Find("ShadowOath/UnlitColor");
            _outline = Shader.Find("ShadowOath/InkOutline");
            _particle = Shader.Find("ShadowOath/InkParticle");
            if (_outline != null)
            {
                _outlineMat = new Material(_outline);
                _outlineMat.SetColor("_Color", Palette.Ink);
                _outlineMat.SetFloat("_Extrude", 0.02f);
            }
            if (_particle != null)
            {
                _particleMat = new Material(_particle);
                _particleMat.SetColor("_BaseColor", Color.white);
            }
            _ready = true;
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.LogWarning("Shadow Oath: no render pipeline is assigned yet. Use Tools → Shadow Oath → Configure Project, then press Play again.");
            }
        }

        public static Material Lit(Color color, float emission = 0f)
        {
            Init();
            var shader = _lit != null ? _lit : _unlit;
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.04f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (emission > 0f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return mat;
        }

        public static Material Ghost(Color color, float alpha)
        {
            Init();
            var shader = _unlit != null ? _unlit : _lit;
            var mat = new Material(shader);
            var c = color;
            c.a = alpha;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            return mat;
        }

        public static void AddOutline(GameObject source)
        {
            Init();
            if (_outlineMat == null) return;
            var filter = source.GetComponent<MeshFilter>();
            var sourceRenderer = source.GetComponent<MeshRenderer>();
            if (filter == null || sourceRenderer == null || filter.sharedMesh == null) return;
            var shell = new GameObject("Ink");
            shell.transform.SetParent(source.transform, false);
            var meshFilter = shell.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = filter.sharedMesh;
            var renderer = shell.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _outlineMat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public static void Splash(Vector3 position, Color color, int count, float speed = 5.5f, float size = 0.28f)
        {
            Init();
            var go = new GameObject("Ink Splash");
            if (GameSession.LevelRoot != null) go.transform.SetParent(GameSession.LevelRoot, true);
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.35f, size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            main.gravityModifier = 1.4f;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 40)) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;
            var colorOver = ps.colorOverLifetime;
            colorOver.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOver.color = gradient;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (_particleMat != null) renderer.material = _particleMat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            Object.Destroy(go, 1.4f);
        }

        public static void ApplyAtmosphere(float fogDensity)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.045f, 0.05f, 0.075f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Palette.Fog;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.reflectionIntensity = 0f;
        }
    }

    public sealed class Tintable : MonoBehaviour
    {
        public Renderer[] Renderers;
        readonly List<Color> _colors = new List<Color>();
        float _flash;

        public void Remember()
        {
            _colors.Clear();
            if (Renderers == null) return;
            foreach (var renderer in Renderers)
            {
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    _colors.Add(Color.white);
                    continue;
                }
                var mat = renderer.sharedMaterial;
                if (mat.HasProperty("_BaseColor")) _colors.Add(mat.GetColor("_BaseColor"));
                else if (mat.HasProperty("_Color")) _colors.Add(mat.GetColor("_Color"));
                else _colors.Add(Color.white);
            }
        }

        public void Flash(float amount = 1f) => _flash = Mathf.Max(_flash, amount);

        void LateUpdate()
        {
            if (Renderers == null) return;
            if (_flash > 0f) _flash = Mathf.MoveTowards(_flash, 0f, Time.unscaledDeltaTime * 4.5f);
            for (int i = 0; i < Renderers.Length; i++)
            {
                var renderer = Renderers[i];
                if (renderer == null) continue;
                Color baseColor = i < _colors.Count ? _colors[i] : Color.white;
                Color color = Color.Lerp(baseColor, Color.white, _flash);
                var mat = renderer.material;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            }
        }
    }

    public sealed class Sway : MonoBehaviour
    {
        public float Degrees = 10f;
        public float Speed = 2.4f;
        public float Phase;
        Quaternion _base;

        void Awake() => _base = transform.localRotation;

        void LateUpdate()
        {
            if (GameSession.Paused) return;
            float wave = Mathf.Sin(Time.time * Speed + Phase) * Degrees;
            transform.localRotation = _base * Quaternion.Euler(0f, 0f, wave);
        }
    }

    public enum FigureKind
    {
        Haru,
        Guard,
        Kenzo,
        Elder,
        Scout,
        Echo,
        Dummy
    }

    public sealed class Figure
    {
        public Transform Root;
        public Transform Sword;
        public Tintable Tint;

        public static Figure Build(Transform parent, FigureKind kind, Color robe, Color accent)
        {
            var rootGo = new GameObject(kind.ToString());
            rootGo.transform.SetParent(parent, false);
            var figure = new Figure { Root = rootGo.transform };
            var parts = new List<Renderer>();

            if (kind == FigureKind.Dummy)
            {
                parts.Add(Part(rootGo.transform, PrimitiveType.Cylinder, "Post", new Vector3(0f, 0.8f, 0f), new Vector3(0.28f, 0.8f, 0.28f), Palette.Wood));
                parts.Add(Part(rootGo.transform, PrimitiveType.Cylinder, "Arm", new Vector3(0f, 1.25f, 0f), new Vector3(0.7f, 0.06f, 0.06f), Palette.WoodDark));
                parts.Add(Part(rootGo.transform, PrimitiveType.Sphere, "Knot", new Vector3(0f, 1.55f, 0f), new Vector3(0.28f, 0.28f, 0.28f), Palette.Wood));
                figure.Tint = AttachTint(rootGo, parts);
                return figure;
            }

            float height = kind == FigureKind.Elder ? 0.86f : kind == FigureKind.Kenzo ? 1.06f : kind == FigureKind.Scout ? 0.96f : 1f;
            rootGo.transform.localScale = Vector3.one * height;

            Color skin = kind == FigureKind.Kenzo ? new Color(0.12f, 0.1f, 0.1f) : Palette.Paper;
            parts.Add(Part(rootGo.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 0.92f, 0f), new Vector3(0.46f, 0.46f, 0.34f), robe));
            parts.Add(Part(rootGo.transform, PrimitiveType.Sphere, "Head", new Vector3(0.02f, 1.52f, 0f), new Vector3(0.34f, 0.34f, 0.32f), skin));
            parts.Add(Part(rootGo.transform, PrimitiveType.Sphere, "Eye", new Vector3(0.16f, 1.55f, 0f), new Vector3(0.07f, 0.07f, 0.07f), Palette.Ink, false));

            if (kind == FigureKind.Kenzo)
            {
                parts.Add(Part(rootGo.transform, PrimitiveType.Cube, "Mask", new Vector3(0.16f, 1.5f, 0f), new Vector3(0.06f, 0.16f, 0.22f), Palette.Ink, false));
                parts.Add(Part(rootGo.transform, PrimitiveType.Cube, "Slit", new Vector3(0.2f, 1.52f, 0f), new Vector3(0.03f, 0.03f, 0.12f), Palette.Crimson, false));
            }
            else if (kind == FigureKind.Guard)
            {
                parts.Add(Part(rootGo.transform, PrimitiveType.Cube, "Helm", new Vector3(0.02f, 1.68f, 0f), new Vector3(0.36f, 0.1f, 0.34f), new Color(0.18f, 0.18f, 0.2f)));
                parts.Add(Part(rootGo.transform, PrimitiveType.Cube, "Pauldron", new Vector3(0f, 1.28f, 0.22f), new Vector3(0.22f, 0.12f, 0.18f), Palette.StoneLight, false));
                parts.Add(Part(rootGo.transform, PrimitiveType.Cube, "PauldronL", new Vector3(0f, 1.28f, -0.22f), new Vector3(0.22f, 0.12f, 0.18f), Palette.StoneLight, false));
            }
            else if (kind == FigureKind.Elder)
            {
                parts.Add(Part(rootGo.transform, PrimitiveType.Cylinder, "Staff", new Vector3(0.28f, 0.85f, 0f), new Vector3(0.05f, 0.85f, 0.05f), Palette.WoodDark, false));
            }
            else if (kind == FigureKind.Scout)
            {
                parts.Add(Part(rootGo.transform, PrimitiveType.Cube, "Satchel", new Vector3(-0.18f, 1.0f, 0f), new Vector3(0.12f, 0.18f, 0.16f), Palette.Wood, false));
            }
            else if (kind == FigureKind.Haru || kind == FigureKind.Echo)
            {
                parts.Add(Part(rootGo.transform, PrimitiveType.Cube, "Band", new Vector3(0.04f, 1.62f, 0f), new Vector3(0.36f, 0.06f, 0.34f), accent));
            }

            var scarf = Part(rootGo.transform, PrimitiveType.Cube, "Scarf", new Vector3(-0.16f, 1.32f, 0f), new Vector3(0.34f, 0.05f, 0.1f), accent, false);
            var sway = scarf.gameObject.AddComponent<Sway>();
            sway.Degrees = 14f;
            sway.Speed = 2.2f;
            sway.Phase = 0.4f;
            var scarf2 = Part(rootGo.transform, PrimitiveType.Cube, "ScarfTail", new Vector3(-0.34f, 1.18f, 0f), new Vector3(0.22f, 0.04f, 0.08f), accent, false);
            var sway2 = scarf2.gameObject.AddComponent<Sway>();
            sway2.Degrees = 18f;
            sway2.Speed = 2.8f;
            sway2.Phase = 1.3f;

            bool armed = kind == FigureKind.Haru || kind == FigureKind.Guard || kind == FigureKind.Kenzo || kind == FigureKind.Echo || kind == FigureKind.Scout;
            if (armed)
            {
                var pivot = new GameObject("SwordPivot").transform;
                pivot.SetParent(rootGo.transform, false);
                pivot.localPosition = new Vector3(0.22f, 1.12f, 0f);
                float blade = kind == FigureKind.Guard ? 0.95f : kind == FigureKind.Scout ? 0.55f : 0.78f;
                Part(pivot, PrimitiveType.Cube, "Blade", new Vector3(blade * 0.5f, 0f, 0f), new Vector3(blade, 0.045f, 0.02f), Palette.Paper);
                Part(pivot, PrimitiveType.Cube, "Hilt", new Vector3(-0.08f, 0f, 0f), new Vector3(0.16f, 0.05f, 0.05f), Palette.Ink, false);
                pivot.localEulerAngles = new Vector3(0f, 0f, -70f);
                figure.Sword = pivot;
            }

            var blob = GameObject.CreatePrimitive(PrimitiveType.Quad);
            blob.name = "Blob";
            Object.Destroy(blob.GetComponent<Collider>());
            blob.transform.SetParent(rootGo.transform, false);
            blob.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            blob.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            blob.transform.localScale = new Vector3(0.85f, 0.45f, 1f);
            blob.GetComponent<Renderer>().sharedMaterial = StageLook.Ghost(new Color(0f, 0f, 0f, 0.45f), 0.45f);

            if (kind == FigureKind.Echo)
            {
                foreach (var renderer in rootGo.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = StageLook.Ghost(new Color(0.45f, 0.55f, 0.95f, 0.55f), 0.55f);
                }
            }

            figure.Tint = AttachTint(rootGo, parts);
            return figure;
        }

        public void Face(int direction)
        {
            if (Root == null) return;
            Root.localRotation = direction >= 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
        }

        static Renderer Part(Transform parent, PrimitiveType type, string name, Vector3 localPos, Vector3 localScale, Color color, bool outline = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var collider = go.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = StageLook.Lit(color);
            if (outline) StageLook.AddOutline(go);
            return renderer;
        }

        static Tintable AttachTint(GameObject root, List<Renderer> parts)
        {
            var tint = root.AddComponent<Tintable>();
            tint.Renderers = parts.ToArray();
            tint.Remember();
            return tint;
        }
    }

    public sealed class SideCamera : MonoBehaviour
    {
        public static SideCamera Instance { get; private set; }

        public Transform Target;
        Camera _cam;
        Vector3 _velocity;
        float _shake;
        float _fov = 40f;
        float _fovVel;

        public static SideCamera Create()
        {
            var go = new GameObject("Side Camera");
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 220f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Palette.Night;
            camera.allowHDR = true;
            go.AddComponent<AudioListener>();
            var side = go.AddComponent<SideCamera>();
            side._cam = camera;
            go.transform.position = new Vector3(0f, 2.2f, -15.5f);
            go.transform.rotation = Quaternion.Euler(6.5f, 0f, 0f);
            Instance = side;

            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon";
            Object.Destroy(moon.GetComponent<Collider>());
            moon.transform.SetParent(go.transform, false);
            moon.transform.localPosition = new Vector3(7.2f, 6.4f, 28f);
            moon.transform.localScale = Vector3.one * 3.4f;
            moon.GetComponent<Renderer>().sharedMaterial = StageLook.Lit(Palette.Moon, 2.4f);
            return side;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Shake(float magnitude) => _shake = Mathf.Max(_shake, magnitude);

        public void SetZoom(float fov) => _fov = fov;

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt <= 0f) return;
            Vector3 focus = Target != null ? Target.position : Vector3.zero;
            float lead = 0f;
            if (Target != null)
            {
                var player = Target.GetComponent<PlayerActor>();
                if (player != null) lead = player.Face * 1.8f;
            }
            Vector3 desired = new Vector3(focus.x + lead, focus.y + 1.85f, -15.5f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, 0.14f, 70f, dt);
            transform.rotation = Quaternion.Euler(6.5f, 0f, 0f);
            _cam.fieldOfView = Mathf.SmoothDamp(_cam.fieldOfView, _fov, ref _fovVel, 0.2f, 40f, dt);
            if (_shake > 0.001f)
            {
                transform.position += (Vector3)Random.insideUnitCircle * _shake * 0.25f;
                _shake = Mathf.MoveTowards(_shake, 0f, dt * 5f);
            }
        }
    }

    public sealed class ParallaxStrip : MonoBehaviour
    {
        public float Factor = 0.65f;
        float _anchorX;

        void Awake() => _anchorX = transform.position.x;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var position = transform.position;
            position.x = _anchorX + cam.transform.position.x * Factor;
            transform.position = position;
        }
    }
}
