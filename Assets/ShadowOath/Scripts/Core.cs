using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ShadowOath
{
    public enum ClanId
    {
        Kurayami = 0,
        Shiranami = 1,
        Shiramine = 2
    }

    public enum FlowState
    {
        Title,
        Clan,
        Hub,
        Mission,
        Duel,
        Ending,
        GameOver
    }

    public sealed class ClanDef
    {
        public ClanId Id;
        public string Name;
        public string Epithet;
        public string Creed;
        public string Gift;
        public string GiftDetail;
        public Color Accent;
        public Color Robe;
        public bool ShadowAffinity;
        public float WaterCost;
        public float WaterDamage;
        public float CloneCost;
        public float SeikiRegen;
        public float FocusDrain;
        public float DodgeSpeed;
        public float DodgeDuration;
        public float DodgeInvuln;
        public float TakedownFocus;
    }

    public static class ClanCatalog
    {
        static readonly ClanDef[] All =
        {
            new ClanDef
            {
                Id = ClanId.Kurayami,
                Name = "Kurayami",
                Epithet = "Black Night",
                Creed = "We were the dark between lanterns. The raid took the roofs, not the silence.",
                Gift = "Umbral Step",
                GiftDetail = "In shadow you move faster, and guards almost lose you. A silent takedown returns Moon Focus.",
                Accent = new Color(0.62f, 0.68f, 0.95f),
                Robe = new Color(0.08f, 0.09f, 0.14f),
                ShadowAffinity = true,
                WaterCost = 36f,
                WaterDamage = 28f,
                CloneCost = 42f,
                SeikiRegen = 7f,
                FocusDrain = 36f,
                DodgeSpeed = 13.5f,
                DodgeDuration = 0.16f,
                DodgeInvuln = 0.18f,
                TakedownFocus = 32f
            },
            new ClanDef
            {
                Id = ClanId.Shiranami,
                Name = "Shiranami",
                Epithet = "White Wave",
                Creed = "Steel is a river. It finds the gap, then it does not stop.",
                Gift = "Tideborn",
                GiftDetail = "Water Oath costs less Seiki and cuts deeper. Seiki returns faster between exchanges.",
                Accent = new Color(0.35f, 0.82f, 0.84f),
                Robe = new Color(0.16f, 0.22f, 0.26f),
                ShadowAffinity = false,
                WaterCost = 22f,
                WaterDamage = 40f,
                CloneCost = 32f,
                SeikiRegen = 13f,
                FocusDrain = 36f,
                DodgeSpeed = 13.5f,
                DodgeDuration = 0.16f,
                DodgeInvuln = 0.18f,
                TakedownFocus = 0f
            },
            new ClanDef
            {
                Id = ClanId.Shiramine,
                Name = "Shiramine",
                Epithet = "White Peak",
                Creed = "Snow does not hurry. Neither does the blade that ends a feud.",
                Gift = "Still Blade",
                GiftDetail = "Moon Focus drains slowly. Your dodge carries farther and forgives a late start.",
                Accent = new Color(0.90f, 0.93f, 0.96f),
                Robe = new Color(0.22f, 0.24f, 0.28f),
                ShadowAffinity = false,
                WaterCost = 36f,
                WaterDamage = 28f,
                CloneCost = 42f,
                SeikiRegen = 7f,
                FocusDrain = 20f,
                DodgeSpeed = 16.5f,
                DodgeDuration = 0.20f,
                DodgeInvuln = 0.27f,
                TakedownFocus = 0f
            }
        };

        public static ClanDef Get(ClanId id) => All[(int)id];
        public static IReadOnlyList<ClanDef> List => All;
    }

    public struct HitInfo
    {
        public float Damage;
        public int Team;
        public float FromX;
        public bool Heavy;
        public bool Stealth;
    }

    public static class GameSession
    {
        public static ClanId Clan = ClanId.Kurayami;
        public static bool HasClan;
        public static bool ElderSpoken;
        public static bool ScoutSpoken;
        public static bool Paused;
        public static bool InDialogue;
        public static bool FlowLocked;
        public static bool Cinematic;
        public static string Objective = "";
        public static PlayerActor Player;
        public static Transform LevelRoot;
        public static int Combo;
        public static float ComboTimer;
        public static float InputGrace;

        public static ClanDef Def => ClanCatalog.Get(Clan);

        public static bool Frozen => Paused || InDialogue || FlowLocked || Cinematic;

        public static bool AcceptsGameplay =>
            HasClan && Player != null && !Player.Dead && !Frozen;

        public static void ResetRun()
        {
            HasClan = false;
            ElderSpoken = false;
            ScoutSpoken = false;
            Paused = false;
            InDialogue = false;
            FlowLocked = false;
            Cinematic = false;
            Objective = "";
            Combo = 0;
            ComboTimer = 0f;
            InputGrace = 0f;
            Player = null;
            LevelRoot = null;
        }

        public static void NoteHit()
        {
            if (ComboTimer > 0f) Combo++;
            else Combo = 1;
            ComboTimer = 1.7f;
        }

        public static void TickCombo(float unscaledDt)
        {
            if (ComboTimer > 0f)
            {
                ComboTimer -= unscaledDt;
                if (ComboTimer <= 0f) Combo = 0;
            }
        }
    }

    [DefaultExecutionOrder(-300)]
    public sealed class GameInputReader : MonoBehaviour
    {
        public static GameInputReader Instance { get; private set; }

        public Vector2 Move { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool LightPressed { get; private set; }
        public bool HeavyPressed { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool WaterPressed { get; private set; }
        public bool ClonePressed { get; private set; }
        public bool FocusHeld { get; private set; }
        public bool DistractPressed { get; private set; }
        public bool TakedownPressed { get; private set; }
        public bool PausePressed { get; private set; }
        public bool ConfirmPressed { get; private set; }
        public bool CancelPressed { get; private set; }
        public bool ClickPressed { get; private set; }
        public int NavX { get; private set; }
        public int NavY { get; private set; }
        public Vector2 Pointer { get; private set; }
        public bool PointerValid { get; private set; }

        InputAction _moveKeys;
        InputAction _movePad;
        InputAction _jump;
        InputAction _dodge;
        InputAction _light;
        InputAction _lightMouse;
        InputAction _heavy;
        InputAction _heavyMouse;
        InputAction _interact;
        InputAction _water;
        InputAction _clone;
        InputAction _focus;
        InputAction _distract;
        InputAction _takedown;
        InputAction _pause;
        InputAction _confirm;
        InputAction _cancel;

        float _prevX;
        float _prevY;
        float _holdX;
        float _holdY;
        bool _rumbling;

        void Awake()
        {
            Instance = this;
            BuildActions();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DisposeActions();
            StopRumble();
        }

        void Update()
        {
            Move = ReadMove();
            JumpHeld = _jump.IsPressed();
            JumpPressed = _jump.WasPressedThisFrame();
            DodgePressed = _dodge.WasPressedThisFrame();
            LightPressed = _light.WasPressedThisFrame() || _lightMouse.WasPressedThisFrame();
            HeavyPressed = _heavy.WasPressedThisFrame() || _heavyMouse.WasPressedThisFrame();
            InteractPressed = _interact.WasPressedThisFrame();
            WaterPressed = _water.WasPressedThisFrame();
            ClonePressed = _clone.WasPressedThisFrame();
            FocusHeld = _focus.IsPressed();
            DistractPressed = _distract.WasPressedThisFrame();
            TakedownPressed = _takedown.WasPressedThisFrame();
            PausePressed = _pause.WasPressedThisFrame();
            ConfirmPressed = _confirm.WasPressedThisFrame() || _jump.WasPressedThisFrame();
            CancelPressed = _cancel.WasPressedThisFrame() || _pause.WasPressedThisFrame();
            ClickPressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            ReadPointer();
            ReadNav();
        }

        public bool AnyButtonPressed()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
                return true;
            var pad = Gamepad.current;
            if (pad == null) return false;
            foreach (var control in pad.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame) return true;
            }
            return false;
        }

        public void Rumble(float low, float high, float seconds)
        {
            var pad = Gamepad.current;
            if (pad == null) return;
            pad.SetMotorSpeeds(low, high);
            if (!_rumbling) StartCoroutine(EndRumble(seconds));
        }

        System.Collections.IEnumerator EndRumble(float seconds)
        {
            _rumbling = true;
            yield return new WaitForSecondsRealtime(seconds);
            StopRumble();
            _rumbling = false;
        }

        public void PaintLightBar(Color color)
        {
            var pad = Gamepad.current;
            if (pad == null) return;
            var method = pad.GetType().GetMethod("SetLightBarColor", new[] { typeof(Color) });
            if (method != null) method.Invoke(pad, new object[] { color });
        }

        void StopRumble()
        {
            var pad = Gamepad.current;
            if (pad != null) pad.SetMotorSpeeds(0f, 0f);
        }

        Vector2 ReadMove()
        {
            Vector2 keys = _moveKeys.ReadValue<Vector2>();
            Vector2 pad = _movePad.ReadValue<Vector2>();
            if (Gamepad.current != null)
            {
                Vector2 dpad = Gamepad.current.dpad.ReadValue();
                if (dpad.sqrMagnitude > pad.sqrMagnitude) pad = dpad;
            }
            return pad.sqrMagnitude >= keys.sqrMagnitude ? pad : keys;
        }

        void ReadPointer()
        {
            if (Mouse.current == null)
            {
                PointerValid = false;
                return;
            }
            Pointer = Mouse.current.position.ReadValue();
            PointerValid = true;
        }

        void ReadNav()
        {
            int navX = 0;
            int navY = 0;
            float x = Move.x;
            float y = Move.y;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame) navX = -1;
                else if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame) navX = 1;
                if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) navY = 1;
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) navY = -1;
            }
            EdgeAxis(x, ref _prevX, ref _holdX, 1, ref navX);
            EdgeAxis(y, ref _prevY, ref _holdY, 1, ref navY);
            NavX = navX;
            NavY = navY;
        }

        static void EdgeAxis(float value, ref float prev, ref float hold, int sign, ref int nav)
        {
            const float dead = 0.55f;
            if (value > dead)
            {
                if (prev <= dead) nav = sign;
                else
                {
                    hold += Time.unscaledDeltaTime;
                    if (hold > 0.38f) { nav = sign; hold = 0.22f; }
                }
            }
            else if (value < -dead)
            {
                if (prev >= -dead) nav = -sign;
                else
                {
                    hold += Time.unscaledDeltaTime;
                    if (hold > 0.38f) { nav = -sign; hold = 0.22f; }
                }
            }
            else hold = 0f;
            prev = value;
        }

        void BuildActions()
        {
            _moveKeys = new InputAction("MoveKeys", InputActionType.Value, expectedControlType: "Vector2");
            _moveKeys.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            _moveKeys.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _movePad = new InputAction("MovePad", InputActionType.Value, expectedControlType: "Vector2");
            _movePad.AddBinding("<Gamepad>/leftStick").WithProcessor("stickDeadzone(min=0.18,max=0.95)");

            _jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            _dodge = Button("Dodge", "<Keyboard>/leftShift", "<Gamepad>/buttonEast");
            _light = Button("Light", "<Keyboard>/j", "<Gamepad>/buttonWest");
            _lightMouse = Button("LightMouse", "<Mouse>/leftButton");
            _heavy = Button("Heavy", "<Keyboard>/k", "<Gamepad>/buttonNorth");
            _heavyMouse = Button("HeavyMouse", "<Mouse>/rightButton");
            _interact = Button("Interact", "<Keyboard>/e");
            _water = Button("Water", "<Keyboard>/r", "<Gamepad>/rightShoulder");
            _clone = Button("Clone", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            _focus = Button("Focus", "<Keyboard>/f", "<Gamepad>/rightTrigger");
            _distract = Button("Distract", "<Keyboard>/g", "<Mouse>/middleButton", "<Gamepad>/leftTrigger");
            _takedown = Button("Takedown", "<Keyboard>/v", "<Gamepad>/rightStickPress");
            _pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            _confirm = Button("Confirm", "<Keyboard>/enter", "<Keyboard>/numpadEnter");
            _cancel = Button("Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");

            EnableAll();
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var binding in bindings) action.AddBinding(binding);
            return action;
        }

        void EnableAll()
        {
            foreach (var action in Actions()) action.Enable();
        }

        void DisposeActions()
        {
            foreach (var action in Actions())
            {
                if (action == null) continue;
                action.Disable();
                action.Dispose();
            }
        }

        IEnumerable<InputAction> Actions()
        {
            yield return _moveKeys;
            yield return _movePad;
            yield return _jump;
            yield return _dodge;
            yield return _light;
            yield return _lightMouse;
            yield return _heavy;
            yield return _heavyMouse;
            yield return _interact;
            yield return _water;
            yield return _clone;
            yield return _focus;
            yield return _distract;
            yield return _takedown;
            yield return _pause;
            yield return _confirm;
            yield return _cancel;
        }
    }

    public static class PlatformServices
    {
        public static IPlatformService Active { get; private set; } = new NullPlatformService();

        public static void Initialize()
        {
#if STEAMWORKS_NET
            Active = new SteamworksPlatformService();
#else
            Active = new NullPlatformService();
#endif
            Active.Initialize();
        }

        public static void Shutdown()
        {
            Active?.Shutdown();
        }

        public static void Presence(string status) => Active?.SetPresence(status);

        public static void Unlock(string achievementId) => Active?.Unlock(achievementId);
    }

    public interface IPlatformService
    {
        string BackendName { get; }
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
        void SetPresence(string status);
        void Unlock(string achievementId);
    }

    public sealed class NullPlatformService : IPlatformService
    {
        public string BackendName => "Local";
        public bool IsInitialized { get; private set; }

        public void Initialize() => IsInitialized = true;

        public void Shutdown() => IsInitialized = false;

        public void SetPresence(string status)
        {
            if (!string.IsNullOrEmpty(status))
                Debug.Log("[Shadow Oath] Presence: " + status);
        }

        public void Unlock(string achievementId)
        {
            if (!string.IsNullOrEmpty(achievementId))
                Debug.Log("[Shadow Oath] Achievement queued for a future Steam build: " + achievementId);
        }
    }

#if STEAMWORKS_NET
    /// <summary>
    /// Compiled only when the STEAMWORKS_NET scripting define is set and Steamworks.NET is imported.
    /// See README for the setup steps. App ID is read from steam_appid.txt by the Steam client.
    /// </summary>
    public sealed class SteamworksPlatformService : IPlatformService
    {
        public string BackendName => "Steam";
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            try
            {
                if (!Steamworks.SteamAPI.Init())
                {
                    Debug.LogWarning("[Shadow Oath] SteamAPI.Init failed. Running without Steam features.");
                    return;
                }
                IsInitialized = true;
                Steamworks.SteamUserStats.RequestCurrentStats();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Shadow Oath] Steam init error: " + exception.Message);
            }
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            Steamworks.SteamAPI.Shutdown();
            IsInitialized = false;
        }

        public void SetPresence(string status)
        {
            if (!IsInitialized) return;
            Steamworks.SteamFriends.SetRichPresence("status", status ?? "");
        }

        public void Unlock(string achievementId)
        {
            if (!IsInitialized || string.IsNullOrEmpty(achievementId)) return;
            Steamworks.SteamUserStats.SetAchievement(achievementId);
            Steamworks.SteamUserStats.StoreStats();
        }
    }
#endif

    public static class Achievements
    {
        public const string OathSworn = "oath_sworn";
        public const string GateCrossed = "gate_crossed";
        public const string OathKept = "oath_kept";
    }

    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        AudioSource[] _voices;
        AudioSource _bed;
        AudioSource _pulse;
        int _next;
        float _combat;

        const int Rate = 44100;

        void Awake()
        {
            Instance = this;
            BuildClips();
            _voices = new AudioSource[8];
            for (int i = 0; i < _voices.Length; i++)
            {
                _voices[i] = gameObject.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
                _voices[i].spatialBlend = 0f;
            }
            _bed = gameObject.AddComponent<AudioSource>();
            _bed.loop = true;
            _bed.playOnAwake = false;
            _bed.clip = _clips["bed"];
            _bed.volume = 0.28f;
            _bed.Play();
            _pulse = gameObject.AddComponent<AudioSource>();
            _pulse.loop = true;
            _pulse.playOnAwake = false;
            _pulse.clip = _clips["pulse"];
            _pulse.volume = 0f;
            _pulse.Play();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            bool combat = false;
            var player = GameSession.Player;
            if (player != null && !GameSession.Paused)
            {
                if (GameController.Instance != null)
                {
                    var flow = GameController.Instance.Flow;
                    combat = flow == FlowState.Duel || player.InCombat;
                }
            }
            float target = combat ? 0.22f : 0f;
            _combat = Mathf.MoveTowards(_combat, target, Time.unscaledDeltaTime * 0.35f);
            _pulse.volume = _combat;
            _bed.volume = GameSession.Paused ? 0.08f : 0.28f;
        }

        public void Play(string id, float volume = 0.8f, float pitch = 1f)
        {
            if (!_clips.TryGetValue(id, out var clip) || clip == null) return;
            var voice = _voices[_next];
            _next = (_next + 1) % _voices.Length;
            voice.pitch = pitch;
            voice.PlayOneShot(clip, volume);
        }

        public void PlayVaried(string id, float volume = 0.8f)
        {
            Play(id, volume, UnityEngine.Random.Range(0.94f, 1.06f));
        }

        void BuildClips()
        {
            _clips["click"] = Tone("click", 0.06f, t => Blip(t, 880f, 0.06f));
            _clips["confirm"] = Tone("confirm", 0.22f, t => Blip(t, 523f, 0.12f) + Blip(t - 0.08f, 784f, 0.14f));
            _clips["back"] = Tone("back", 0.12f, t => Blip(t, 392f, 0.12f));
            _clips["deny"] = Tone("deny", 0.14f, t => Blip(t, 180f, 0.14f) * 0.8f);
            _clips["swing"] = Tone("swing", 0.14f, t => Noise(t) * Env(t, 0.14f, 10f) * 0.45f + Blip(t, 210f, 0.1f) * 0.3f);
            _clips["heavy"] = Tone("heavy", 0.28f, t => Noise(t) * Env(t, 0.28f, 6f) * 0.5f + Blip(t, 110f, 0.22f) * 0.55f);
            _clips["hit"] = Tone("hit", 0.16f, t => (Noise(t) * 0.55f + Mathf.Sin(2f * Mathf.PI * 140f * t)) * Env(t, 0.16f, 14f));
            _clips["hurt"] = Tone("hurt", 0.22f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(320f, 90f, Mathf.Clamp01(t / 0.22f)) * t) * Env(t, 0.22f, 8f));
            _clips["takedown"] = Tone("takedown", 0.4f, t => Blip(t, 90f, 0.28f) * 0.9f + Blip(t, 640f, 0.08f) * 0.3f);
            _clips["water"] = Tone("water", 0.45f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(280f, 860f, t / 0.45f) * t) * Env(t, 0.45f, 3.5f));
            _clips["clone"] = Tone("clone", 0.35f, t => (Mathf.Sin(2f * Mathf.PI * 520f * t) + Mathf.Sin(2f * Mathf.PI * 780f * t) * 0.5f) * Env(t, 0.35f, 5f) * 0.6f);
            _clips["stone"] = Tone("stone", 0.12f, t => Noise(t) * Env(t, 0.12f, 18f) * 0.7f + Blip(t, 360f, 0.08f) * 0.3f);
            _clips["bell"] = Tone("bell", 0.7f, t => (Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 990f * t) * 0.25f) * Env(t, 0.7f, 3f));
            _clips["alert"] = Tone("alert", 0.28f, t => Blip(t, 740f, 0.1f) + Blip(t - 0.12f, 620f, 0.12f));
            _clips["jump"] = Tone("jump", 0.1f, t => Blip(t, 420f, 0.1f) * 0.4f);
            _clips["death"] = Tone("death", 0.7f, t => Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 50f, t / 0.7f) * t) * Env(t, 0.7f, 3f));
            _clips["victory"] = Tone("victory", 1.4f, t =>
                Blip(t, 220f, 0.3f) + Blip(t - 0.18f, 261f, 0.3f) + Blip(t - 0.36f, 329f, 0.35f) + Blip(t - 0.62f, 440f, 0.6f));
            _clips["finisher"] = Tone("finisher", 0.8f, t => (Noise(t) * 0.35f + Mathf.Sin(2f * Mathf.PI * 70f * t)) * Env(t, 0.8f, 3f));
            _clips["bed"] = Loop("bed", 6f, t =>
            {
                float a = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.18f;
                float e = Mathf.Sin(2f * Mathf.PI * 164.81f * t) * 0.08f;
                float d = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.04f;
                float lfo = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 0.18f * t);
                return (a + e + d) * lfo;
            });
            _clips["pulse"] = Loop("pulse", 1.6f, t =>
            {
                float beat = t % 0.4f;
                return Noise(t) * Env(beat, 0.08f, 40f) * 0.55f;
            });
        }

        static float Blip(float t, float freq, float dur)
        {
            if (t < 0f || t > dur) return 0f;
            return Mathf.Sin(2f * Mathf.PI * freq * t) * Env(t, dur, 6f);
        }

        static float Env(float t, float dur, float sharpness)
        {
            if (t < 0f || t > dur) return 0f;
            return Mathf.Exp(-sharpness * t / Mathf.Max(0.0001f, dur));
        }

        static float Noise(float t)
        {
            return Mathf.PerlinNoise(t * 80f, 0.2f) * 2f - 1f;
        }

        static AudioClip Tone(string name, float duration, Func<float, float> sample)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(Rate * duration));
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Clamp(sample(t), -1f, 1f);
            }
            var clip = AudioClip.Create(name, count, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Loop(string name, float duration, Func<float, float> sample)
        {
            var clip = Tone(name, duration, sample);
            return clip;
        }
    }
}
