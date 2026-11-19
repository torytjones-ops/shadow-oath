using System.Collections;
using UnityEngine;

namespace ShadowOath
{
    public sealed class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }
        public FlowState Flow { get; private set; } = FlowState.Title;

        GameUi _ui;
        SideCamera _cam;
        Transform _level;
        Light _moon;
        Light _fill;
        bool _transition;
        bool _dead;
        FlowState _retryFlow = FlowState.Mission;
        RivalActor _finisherTarget;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            StageLook.Init();
            gameObject.AddComponent<GameInputReader>();
            gameObject.AddComponent<GameAudio>();
            _ui = gameObject.AddComponent<GameUi>();
            _ui.Build();
            SetupLights();
            _cam = SideCamera.Create();
            DontDestroyOnLoad(_cam.gameObject);
            SweepStrays();
            PlatformServices.Initialize();
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = 60;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            ShowTitle();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                PlatformServices.Shutdown();
                Instance = null;
            }
        }

        void Update()
        {
            ApplyTime();
            var input = GameInputReader.Instance;
            if (_ui != null) _ui.Tick(input, Time.unscaledDeltaTime);
            bool playing = Flow == FlowState.Hub || Flow == FlowState.Mission || Flow == FlowState.Duel;
            if (!_transition && playing && !GameSession.InDialogue && !GameSession.Cinematic && input != null && _ui != null && !_ui.ControlsOpen && !_ui.ConsumedMenu)
            {
                if (input.PausePressed) SetPaused(!GameSession.Paused);
                else if (GameSession.Paused && _ui.PauseOpen && input.CancelPressed) SetPaused(false);
            }
            if (GameSession.Player != null && _cam != null) _cam.Target = GameSession.Player.transform;
        }

        void ApplyTime()
        {
            if (GameSession.Paused || Flow == FlowState.Title || Flow == FlowState.Clan || Flow == FlowState.Ending || Flow == FlowState.GameOver)
            {
                Time.timeScale = 0f;
                return;
            }
            if (GameSession.Cinematic)
            {
                Time.timeScale = 0.28f;
                Time.fixedDeltaTime = 0.02f * 0.28f;
                return;
            }
            bool focus = GameSession.AcceptsGameplay && GameSession.Player != null && GameSession.Player.FocusActive;
            float scale = focus ? 0.35f : 1f;
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * scale;
        }

        public void ShowTitle()
        {
            StopAllCoroutines();
            _transition = false;
            _dead = false;
            ClearLevel();
            GameSession.ResetRun();
            Flow = FlowState.Title;
            Time.timeScale = 0f;
            if (_cam != null) _cam.SetZoom(40f);
            _ui.SetPlayHud(false);
            _ui.ShowTitle();
            PlatformServices.Presence("Before the oath");
        }

        public void OpenClans()
        {
            Flow = FlowState.Clan;
            _ui.ShowClans();
        }

        public void ChooseClan(ClanId clan)
        {
            if (_transition) return;
            GameSession.ResetRun();
            GameSession.Clan = clan;
            GameSession.HasClan = true;
            GameInputReader.Instance?.PaintLightBar(GameSession.Def.Accent);
            PlatformServices.Unlock(Achievements.OathSworn);
            PlatformServices.Presence("Swore the " + GameSession.Def.Name + " oath");
            FadeTo(() => Build(FlowState.Hub));
        }

        public void EnterMission()
        {
            if (_transition || Flow != FlowState.Hub) return;
            if (!GameSession.ElderSpoken || !GameSession.ScoutSpoken) return;
            FadeTo(() => Build(FlowState.Mission));
        }

        public void EnterDuel()
        {
            if (_transition || Flow != FlowState.Mission) return;
            FadeTo(() => Build(FlowState.Duel), StartDuelIntro);
        }

        public void OnPlayerDied()
        {
            if (_dead || Flow == FlowState.GameOver || Flow == FlowState.Ending) return;
            _dead = true;
            _retryFlow = Flow;
            Flow = FlowState.GameOver;
            GameSession.FlowLocked = true;
            GameSession.Cinematic = false;
            _ui.ShowGameOver();
        }

        public void Retry()
        {
            if (_transition) return;
            var flow = _retryFlow;
            _dead = false;
            FadeTo(() => Build(flow));
        }

        public void ReturnToHamlet()
        {
            if (_transition) return;
            FadeTo(() =>
            {
                Build(FlowState.Hub);
                GameSession.ElderSpoken = true;
                GameSession.ScoutSpoken = true;
                GameSession.Objective = "The bridge is quiet. The torii will take you back.";
            });
        }

        public void BeginFinisher(RivalActor rival)
        {
            if (GameSession.Cinematic) return;
            _finisherTarget = rival;
            StartCoroutine(FinisherRoutine());
        }

        public void StartDialogue(string speaker, string[] lines, System.Action done)
        {
            GameSession.InDialogue = true;
            _ui.OpenDialogue(speaker, lines, () =>
            {
                GameSession.InDialogue = false;
                GameSession.InputGrace = 0.16f;
                done?.Invoke();
            });
        }

        public void SetPaused(bool paused)
        {
            if (Flow != FlowState.Hub && Flow != FlowState.Mission && Flow != FlowState.Duel) return;
            GameSession.Paused = paused;
            if (paused) _ui.ShowPause();
            else
            {
                _ui.HidePause();
                GameSession.InputGrace = 0.12f;
            }
        }

        public void OpenControlsFromTitle()
        {
            _ui.ShowControls(null);
        }

        public void OpenControlsFromPause()
        {
            _ui.ShowControls(null);
        }

        public void QuitGame()
        {
            PlatformServices.Shutdown();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void Build(FlowState flow)
        {
            _dead = false;
            GameSession.Paused = false;
            GameSession.InDialogue = false;
            GameSession.Cinematic = false;
            GameSession.FlowLocked = false;
            ClearLevel();
            Flow = flow;
            Transform level = null;
            switch (flow)
            {
                case FlowState.Hub:
                    level = Levels.BuildHub();
                    PlatformServices.Presence("In Okumura");
                    break;
                case FlowState.Mission:
                    level = Levels.BuildMission();
                    PlatformServices.Presence("Crossing the moonlit gate");
                    break;
                case FlowState.Duel:
                    level = Levels.BuildDuel();
                    PlatformServices.Presence("Duel on the cedar bridge");
                    PlatformServices.Unlock(Achievements.GateCrossed);
                    break;
            }
            _level = level;
            _ui.SetPlayHud(true);
            if (GameSession.Player != null && _cam != null)
            {
                _cam.Target = GameSession.Player.transform;
                _cam.transform.position = GameSession.Player.transform.position + new Vector3(0f, 2.1f, -15.5f);
            }
        }

        void StartDuelIntro()
        {
            StartDialogue("Kenzo", new[]
            {
                "Haru of Yamigura. I burned that gate, and I would burn it again.",
                "Show me the oath was more than a word."
            }, () =>
            {
                GameSession.Objective = "Defeat Kenzo";
            });
        }

        IEnumerator FinisherRoutine()
        {
            GameSession.Cinematic = true;
            GameSession.FlowLocked = true;
            if (_cam != null)
            {
                _cam.SetZoom(30f);
                _cam.Shake(1.1f);
            }
            Vector3 at = _finisherTarget != null ? _finisherTarget.transform.position : Vector3.zero;
            StageLook.Splash(at + Vector3.up * 1.2f, Palette.Ink, 36, 8f, 0.55f);
            StageLook.Splash(at + Vector3.up * 1.1f, Palette.Paper, 18, 5f, 0.3f);
            GameAudio.Instance?.Play("finisher", 0.95f);
            GameInputReader.Instance?.Rumble(0.7f, 0.9f, 0.35f);
            yield return new WaitForSecondsRealtime(0.45f);
            _finisherTarget?.Collapse();
            yield return new WaitForSecondsRealtime(1.35f);
            if (_cam != null) _cam.SetZoom(40f);
            GameSession.Cinematic = false;
            GameSession.FlowLocked = false;
            Time.timeScale = 1f;
            Flow = FlowState.Ending;
            PlatformServices.Unlock(Achievements.OathKept);
            PlatformServices.Presence("The oath is kept");
            GameAudio.Instance?.Play("victory", 0.8f);
            _ui.ShowEnding();
        }

        void FadeTo(System.Action middle, System.Action after = null)
        {
            if (_transition) return;
            _transition = true;
            GameSession.FlowLocked = true;
            StartCoroutine(FadeRoutine(middle, after));
        }

        IEnumerator FadeRoutine(System.Action middle, System.Action after)
        {
            yield return _ui.Fade(1f, 0.32f);
            middle?.Invoke();
            yield return null;
            if (GameSession.Player != null && _cam != null)
                _cam.transform.position = GameSession.Player.transform.position + new Vector3(1.5f, 2.1f, -15.5f);
            yield return _ui.Fade(0f, 0.38f);
            GameSession.FlowLocked = false;
            _transition = false;
            after?.Invoke();
        }

        void ClearLevel()
        {
            if (_level != null) Destroy(_level.gameObject);
            _level = null;
            GameSession.LevelRoot = null;
        }

        void SetupLights()
        {
            var moonGo = new GameObject("Moon Light");
            moonGo.transform.SetParent(transform, false);
            moonGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            _moon = moonGo.AddComponent<Light>();
            _moon.type = LightType.Directional;
            _moon.color = new Color(0.73f, 0.81f, 1f);
            _moon.intensity = 1.08f;
            _moon.shadows = LightShadows.Soft;
            _moon.shadowStrength = 0.8f;

            var fillGo = new GameObject("Fill Light");
            fillGo.transform.SetParent(transform, false);
            fillGo.transform.rotation = Quaternion.Euler(18f, 150f, 0f);
            _fill = fillGo.AddComponent<Light>();
            _fill.type = LightType.Directional;
            _fill.color = new Color(0.32f, 0.38f, 0.62f);
            _fill.intensity = 0.16f;
            _fill.shadows = LightShadows.None;
        }

        void SweepStrays()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var camera in cameras)
            {
                if (_cam != null && camera.gameObject == _cam.gameObject) continue;
                Destroy(camera.gameObject);
            }
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light == _moon || light == _fill) continue;
                if (light.transform.IsChildOf(transform)) continue;
                Destroy(light.gameObject);
            }
        }
    }

    static class ShadowOathBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameController>() != null) return;
            var go = new GameObject("Shadow Oath");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<GameController>();
        }
    }
}
