using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowOath
{
    public sealed class MenuHotspot : MonoBehaviour
    {
        public int Index;
        public Action Click;
    }

    public sealed class GameUi : MonoBehaviour
    {
        public static GameUi Instance { get; private set; }

        public bool ControlsOpen { get; private set; }
        public bool PauseOpen { get; private set; }
        public bool ConsumedMenu { get; private set; }
        public bool BlockingMenu =>
            _titleRoot != null && _titleRoot.gameObject.activeSelf ||
            _clanRoot != null && _clanRoot.gameObject.activeSelf ||
            PauseOpen || ControlsOpen ||
            _endRoot != null && _endRoot.gameObject.activeSelf ||
            _deadRoot != null && _deadRoot.gameObject.activeSelf;

        Font _font;
        Sprite _circle;
        GraphicRaycaster _raycaster;
        readonly List<RaycastResult> _hits = new List<RaycastResult>();

        RectTransform _titleRoot;
        Text _titlePrompt;
        RectTransform _titleMenu;
        RectTransform _clanRoot;
        readonly List<Image> _clanPlates = new List<Image>();
        int _clanIndex;
        RectTransform _pauseRoot;
        readonly List<MenuHotspot> _pauseItems = new List<MenuHotspot>();
        int _pauseIndex;
        RectTransform _controlsRoot;
        Action _controlsBack;
        RectTransform _endRoot;
        RectTransform _deadRoot;
        RectTransform _hudRoot;
        RectTransform _dialogueRoot;
        Text _dialogueSpeaker;
        Text _dialogueBody;
        Text _dialogueHint;
        Image _hp;
        Image _seiki;
        Image _focus;
        Text _hpValue;
        Text _objective;
        Text _prompt;
        Text _combo;
        Text _clanLabel;
        Text _hint;
        float _hintTimer;
        Image _fade;
        Image _hurt;
        bool _titleSplash = true;
        bool _openMenuNext;
        bool _dialogueOpen;
        float _dialogueLock;
        string[] _lines = Array.Empty<string>();
        int _line;
        Action _dialogueDone;

        public void Build()
        {
            Instance = this;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _circle = MakeCircle();

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _raycaster = canvasGo.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
            }

            _fade = FullImage(canvasGo.transform, new Color(0.01f, 0.012f, 0.02f, 0f), "Fade");
            _fade.transform.SetAsLastSibling();
            _hurt = FullImage(canvasGo.transform, new Color(0.6f, 0.05f, 0.04f, 0f), "Hurt");
            _hurt.raycastTarget = false;

            BuildHud(canvasGo.transform);
            BuildDialogue(canvasGo.transform);
            BuildTitle(canvasGo.transform);
            BuildClans(canvasGo.transform);
            BuildPause(canvasGo.transform);
            BuildControls(canvasGo.transform);
            BuildEnd(canvasGo.transform);
            BuildGameOver(canvasGo.transform);
            HideAllMenus();
            if (_fade != null) _fade.transform.SetAsLastSibling();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Tick(GameInputReader input, float dt)
        {
            ConsumedMenu = false;
            if (_hintTimer > 0f)
            {
                _hintTimer -= dt;
                if (_hint != null)
                {
                    var c = _hint.color;
                    c.a = Mathf.Clamp01(_hintTimer / 0.4f);
                    if (_hintTimer > 0.4f) c.a = 1f;
                    _hint.color = c;
                    if (_hintTimer <= 0f) _hint.text = "";
                }
            }
            if (_hurt != null && _hurt.color.a > 0f)
            {
                var c = _hurt.color;
                c.a = Mathf.MoveTowards(c.a, 0f, dt * 1.6f);
                _hurt.color = c;
            }

            RefreshPlay();
            if (input == null) return;

            if (_dialogueOpen)
            {
                TickDialogue(input, dt);
                return;
            }
            if (_openMenuNext)
            {
                _openMenuNext = false;
                ShowTitleButtons();
                return;
            }
            if (ControlsOpen)
            {
                TickControls(input);
                return;
            }
            if (_titleRoot != null && _titleRoot.gameObject.activeSelf)
            {
                TickTitle(input);
                return;
            }
            if (_clanRoot != null && _clanRoot.gameObject.activeSelf)
            {
                TickClans(input);
                return;
            }
            if (PauseOpen)
            {
                TickList(_pauseItems, ref _pauseIndex, input, false);
                return;
            }
            if (_endRoot != null && _endRoot.gameObject.activeSelf)
            {
                TickNamed("end", input);
                return;
            }
            if (_deadRoot != null && _deadRoot.gameObject.activeSelf)
            {
                TickNamed("dead", input);
                return;
            }
        }

        public void RefreshPlay()
        {
            if (_hudRoot == null || !_hudRoot.gameObject.activeSelf) return;
            var player = GameSession.Player;
            if (player == null || player.Vital == null) return;
            _hp.fillAmount = Mathf.Clamp01(player.Vital.Current / Mathf.Max(1f, player.Vital.Max));
            _seiki.fillAmount = Mathf.Clamp01(player.Seiki / player.SeikiMax);
            _focus.fillAmount = Mathf.Clamp01(player.Focus / player.FocusMax);
            _hpValue.text = Mathf.CeilToInt(player.Vital.Current) + " / " + Mathf.CeilToInt(player.Vital.Max);
            _clanLabel.text = GameSession.HasClan ? GameSession.Def.Name.ToUpperInvariant() : "";
            _objective.text = GameSession.Objective ?? "";
            _prompt.text = string.IsNullOrEmpty(player.Prompt) ? "" : player.Prompt + "   ·   Triangle / E";
            if (player.Prompt == "Silent takedown") _prompt.text = "Silent takedown   ·   Square / V / R3";
            if (player.Prompt == "Jump to kick off the wall") _prompt.text = "Jump to kick off the wall";
            _combo.text = GameSession.Combo > 1 ? GameSession.Combo + " hits" : "";
            if (player.InShadow && string.IsNullOrEmpty(player.Prompt))
                _prompt.text = "Hidden in shadow";
        }

        public void ShowHint(string text)
        {
            if (_hint == null) return;
            _hint.text = text;
            _hint.color = Palette.Paper;
            _hintTimer = 5.5f;
        }

        public void HurtFlash()
        {
            if (_hurt == null) return;
            _hurt.color = new Color(0.55f, 0.04f, 0.03f, 0.45f);
        }

        public void SetPlayHud(bool on)
        {
            if (_hudRoot != null) _hudRoot.gameObject.SetActive(on);
        }

        public void ShowTitle()
        {
            HideAllMenus();
            _titleSplash = true;
            _titleRoot.gameObject.SetActive(true);
            _titleMenu.gameObject.SetActive(false);
            _titlePrompt.gameObject.SetActive(true);
            _titlePrompt.text = "Press any button";
        }

        public void ShowClans()
        {
            HideAllMenus();
            _clanRoot.gameObject.SetActive(true);
            _clanIndex = GameSession.HasClan ? (int)GameSession.Clan : 0;
            PaintClans();
        }

        public void ShowPause()
        {
            PauseOpen = true;
            _pauseRoot.gameObject.SetActive(true);
            _pauseIndex = 0;
            PaintList(_pauseItems, _pauseIndex);
        }

        public void HidePause()
        {
            PauseOpen = false;
            if (_pauseRoot != null) _pauseRoot.gameObject.SetActive(false);
        }

        public void ShowControls(Action onBack)
        {
            ControlsOpen = true;
            _controlsBack = onBack;
            _controlsRoot.gameObject.SetActive(true);
        }

        public void ShowEnding()
        {
            HideAllMenus();
            SetPlayHud(false);
            _endRoot.gameObject.SetActive(true);
            PaintList(Collect("end"), 0);
        }

        public void ShowGameOver()
        {
            HideAllMenus();
            _deadRoot.gameObject.SetActive(true);
            PaintList(Collect("dead"), 0);
        }

        public void OpenDialogue(string speaker, string[] lines, Action done)
        {
            _dialogueOpen = true;
            _dialogueSpeaker.text = speaker;
            _lines = lines ?? Array.Empty<string>();
            _line = 0;
            _dialogueDone = done;
            _dialogueLock = 0.2f;
            _dialogueRoot.gameObject.SetActive(true);
            _dialogueBody.text = _lines.Length > 0 ? _lines[0] : "";
            _dialogueHint.text = "Cross / Enter";
        }

        public System.Collections.IEnumerator Fade(float target, float duration)
        {
            if (_fade == null) yield break;
            float start = _fade.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, target, Mathf.Clamp01(t / duration));
                var c = _fade.color;
                c.a = a;
                _fade.color = c;
                yield return null;
            }
            var final = _fade.color;
            final.a = target;
            _fade.color = final;
        }

        void TickDialogue(GameInputReader input, float dt)
        {
            _dialogueLock -= dt;
            if (_dialogueLock > 0f) return;
            if (!input.ConfirmPressed && !input.InteractPressed && !input.ClickPressed && !input.CancelPressed) return;
            ConsumedMenu = true;
            GameAudio.Instance?.Play("click", 0.45f);
            _line++;
            _dialogueLock = 0.12f;
            if (_line >= _lines.Length)
            {
                _dialogueOpen = false;
                _dialogueRoot.gameObject.SetActive(false);
                var done = _dialogueDone;
                _dialogueDone = null;
                done?.Invoke();
                return;
            }
            _dialogueBody.text = _lines[_line];
        }

        void TickTitle(GameInputReader input)
        {
            if (_titleSplash)
            {
                if (input.AnyButtonPressed())
                {
                    _openMenuNext = true;
                    GameAudio.Instance?.Play("confirm", 0.6f);
                }
                return;
            }
            TickList(Collect("title"), ref _titleIndex, input, false);
        }

        int _titleIndex;

        void ShowTitleButtons()
        {
            _titleSplash = false;
            _titlePrompt.gameObject.SetActive(false);
            _titleMenu.gameObject.SetActive(true);
            _titleIndex = 0;
            PaintList(Collect("title"), 0);
        }

        void TickClans(GameInputReader input)
        {
            if (input.NavX != 0)
            {
                _clanIndex = (_clanIndex + input.NavX + 3) % 3;
                PaintClans();
                GameAudio.Instance?.Play("click", 0.4f);
            }
            int hovered = HoveredIndex();
            if (hovered >= 0 && hovered < 3 && hovered != _clanIndex)
            {
                _clanIndex = hovered;
                PaintClans();
            }
            bool click = Clicked(out var spot) && spot != null;
            if (input.ConfirmPressed || click)
            {
                GameAudio.Instance?.Play("confirm", 0.7f);
                GameController.Instance?.ChooseClan((ClanId)_clanIndex);
            }
            else if (input.CancelPressed && !input.PausePressed)
            {
                GameAudio.Instance?.Play("back", 0.5f);
                GameController.Instance?.ShowTitle();
            }
            else if (input.PausePressed)
            {
                GameAudio.Instance?.Play("back", 0.5f);
                GameController.Instance?.ShowTitle();
            }
        }

        void TickControls(GameInputReader input)
        {
            if (input.CancelPressed || input.PausePressed || input.ConfirmPressed)
            {
                ConsumedMenu = true;
                ControlsOpen = false;
                _controlsRoot.gameObject.SetActive(false);
                GameAudio.Instance?.Play("back", 0.45f);
                var back = _controlsBack;
                _controlsBack = null;
                back?.Invoke();
            }
        }

        void TickNamed(string group, GameInputReader input)
        {
            var items = Collect(group);
            int index = 0;
            for (int i = 0; i < items.Count; i++)
                if (items[i].GetComponent<Image>() != null && items[i].GetComponent<Image>().color.a > 0.2f)
                    index = items[i].Index;
            // Use stored index on the root via hotspot Index of the highlighted plate.
            index = Highlighted(items);
            TickList(items, ref index, input, false);
        }

        int Highlighted(List<MenuHotspot> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var image = items[i].GetComponent<Image>();
                if (image != null && image.color.r > 0.2f) return i;
            }
            return 0;
        }

        void TickList(List<MenuHotspot> items, ref int index, GameInputReader input, bool horizontal)
        {
            if (items.Count == 0) return;
            int dir = horizontal ? input.NavX : input.NavY;
            if (!horizontal && input.NavY == 0) dir = 0;
            if (dir != 0)
            {
                index = (index - dir + items.Count) % items.Count;
                PaintList(items, index);
                GameAudio.Instance?.Play("click", 0.35f);
            }
            int hover = HoveredIndex();
            if (hover >= 0)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Index == hover)
                    {
                        index = i;
                        PaintList(items, index);
                    }
                }
            }
            if (Clicked(out var spot) && spot != null)
            {
                spot.Click?.Invoke();
                return;
            }
            if (input.ConfirmPressed)
                items[index].Click?.Invoke();
        }

        void PaintClans()
        {
            for (int i = 0; i < _clanPlates.Count; i++)
            {
                var clan = ClanCatalog.Get((ClanId)i);
                bool on = i == _clanIndex;
                _clanPlates[i].color = on
                    ? new Color(clan.Accent.r * 0.22f, clan.Accent.g * 0.22f, clan.Accent.b * 0.22f, 0.96f)
                    : new Color(0.07f, 0.08f, 0.11f, 0.92f);
                var outline = _clanPlates[i].GetComponent<Outline>();
                if (outline != null) outline.effectColor = on ? clan.Accent : new Color(1f, 1f, 1f, 0.08f);
            }
        }

        void PaintList(List<MenuHotspot> items, int index)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var image = items[i].GetComponent<Image>();
                if (image == null) continue;
                image.color = i == index
                    ? new Color(0.83f, 0.68f, 0.38f, 0.95f)
                    : new Color(0.1f, 0.11f, 0.15f, 0.92f);
                var texts = items[i].GetComponentsInChildren<Text>();
                if (texts.Length > 0)
                    texts[0].color = i == index ? new Color(0.08f, 0.07f, 0.05f) : Palette.Paper;
            }
        }

        int HoveredIndex()
        {
            if (!Raycast(out var spot) || spot == null) return -1;
            return spot.Index;
        }

        bool Clicked(out MenuHotspot spot)
        {
            spot = null;
            var input = GameInputReader.Instance;
            if (input == null || !input.ClickPressed) return false;
            if (!Raycast(out spot)) return false;
            return spot != null;
        }

        bool Raycast(out MenuHotspot spot)
        {
            spot = null;
            var input = GameInputReader.Instance;
            if (input == null || !input.PointerValid || EventSystem.current == null || _raycaster == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = input.Pointer };
            _hits.Clear();
            _raycaster.Raycast(data, _hits);
            if (_hits.Count == 0) return false;
            spot = _hits[0].gameObject.GetComponentInParent<MenuHotspot>();
            return true;
        }

        void HideAllMenus()
        {
            PauseOpen = false;
            ControlsOpen = false;
            _dialogueOpen = false;
            SetActive(_titleRoot, false);
            SetActive(_clanRoot, false);
            SetActive(_pauseRoot, false);
            SetActive(_controlsRoot, false);
            SetActive(_endRoot, false);
            SetActive(_deadRoot, false);
            SetActive(_dialogueRoot, false);
        }

        static void SetActive(Component c, bool on)
        {
            if (c != null) c.gameObject.SetActive(on);
        }

        void BuildHud(Transform parent)
        {
            var root = NewRect("HUD", parent);
            Stretch(root);
            _hudRoot = root;
            root.gameObject.SetActive(false);

            var panel = ImageRect("HpPanel", root, new Color(0.03f, 0.035f, 0.05f, 0.72f));
            Anchor(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -36f), new Vector2(420f, 168f));

            _clanLabel = Text(panel.transform, "KURAYAMI", 18, TextAnchor.UpperLeft, Palette.Gold);
            Anchor(_clanLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -34f), new Vector2(-18f, -8f));
            var name = Text(panel.transform, "HARU", 28, TextAnchor.UpperLeft, Palette.Paper);
            Anchor(name.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(18f, -68f), new Vector2(-18f, -32f));

            _hp = Bar(panel.transform, Palette.Crimson, new Vector2(18f, -96f), out _);
            _seiki = Bar(panel.transform, Palette.Water, new Vector2(18f, -122f), out var seikiLabel);
            _focus = Bar(panel.transform, Palette.Gold, new Vector2(18f, -148f), out var focusLabel);
            seikiLabel.text = "SEIKI";
            focusLabel.text = "FOCUS";
            var hpLabel = _hp.transform.parent.GetComponentInChildren<Text>();
            if (hpLabel != null) hpLabel.text = "VITAL";
            _hpValue = Text(panel.transform, "100 / 100", 14, TextAnchor.MiddleRight, Palette.Paper);
            Anchor(_hpValue.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-170f, -96f), new Vector2(-22f, -78f));

            _objective = Text(root, "", 24, TextAnchor.UpperCenter, Palette.Paper);
            Anchor(_objective.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-420f, -78f), new Vector2(420f, -28f));

            _prompt = Text(root, "", 22, TextAnchor.LowerCenter, Palette.Gold);
            Anchor(_prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-520f, 36f), new Vector2(520f, 78f));

            _hint = Text(root, "", 20, TextAnchor.LowerCenter, Palette.Paper);
            Anchor(_hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-620f, 84f), new Vector2(620f, 150f));

            _combo = Text(root, "", 26, TextAnchor.LowerLeft, Palette.Paper);
            Anchor(_combo.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(40f, 36f), new Vector2(320f, 80f));
        }

        Image Bar(Transform parent, Color color, Vector2 topLeft, out Text caption)
        {
            var row = NewRect("Bar", parent);
            Anchor(row, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(topLeft.x, topLeft.y - 16f), new Vector2(-18f, topLeft.y));
            caption = Text(row, "VITAL", 12, TextAnchor.MiddleLeft, new Color(0.75f, 0.72f, 0.66f));
            Anchor(caption.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(70f, 0f));
            var back = ImageRect("Back", row, new Color(0f, 0f, 0f, 0.55f));
            Anchor(back.rectTransform, new Vector2(0f, 0.15f), new Vector2(1f, 0.85f), new Vector2(78f, 0f), new Vector2(0f, 0f));
            var fill = ImageRect("Fill", back.transform, color);
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            return fill;
        }

        void BuildDialogue(Transform parent)
        {
            _dialogueRoot = NewRect("Dialogue", parent);
            Anchor(_dialogueRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-460f, 150f), new Vector2(460f, 360f));
            var plate = ImageRect("Plate", _dialogueRoot, new Color(0.035f, 0.04f, 0.055f, 0.94f));
            Stretch(plate.rectTransform);
            plate.gameObject.AddComponent<Outline>().effectColor = new Color(0.83f, 0.68f, 0.38f, 0.8f);
            _dialogueSpeaker = Text(plate.transform, "", 18, TextAnchor.UpperLeft, Palette.Gold);
            Anchor(_dialogueSpeaker.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -40f), new Vector2(-24f, -12f));
            _dialogueBody = Text(plate.transform, "", 24, TextAnchor.UpperLeft, Palette.Paper);
            Anchor(_dialogueBody.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(24f, 36f), new Vector2(-24f, -48f));
            _dialogueHint = Text(plate.transform, "", 14, TextAnchor.LowerRight, new Color(0.7f, 0.66f, 0.58f));
            Anchor(_dialogueHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 8f), new Vector2(-16f, 32f));
            _dialogueRoot.gameObject.SetActive(false);
        }

        void BuildTitle(Transform parent)
        {
            _titleRoot = NewRect("Title", parent);
            Stretch(_titleRoot);
            var bg = ImageRect("BG", _titleRoot, new Color(0.015f, 0.018f, 0.03f, 1f));
            Stretch(bg.rectTransform);
            var moon = ImageRect("Moon", _titleRoot, Palette.Moon);
            moon.sprite = _circle;
            Anchor(moon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-520f, -520f), new Vector2(-80f, -80f));
            var bar = ImageRect("Ink", _titleRoot, Palette.Ink);
            Anchor(bar.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(120f, 80f), new Vector2(132f, -80f));

            var title = Text(_titleRoot, "S H A D O W   O A T H", 78, TextAnchor.MiddleLeft, Palette.Paper);
            Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(180f, 40f), new Vector2(-160f, 170f));
            var sub = Text(_titleRoot, "Swear once. Cut once.", 28, TextAnchor.MiddleLeft, Palette.Gold);
            Anchor(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(186f, -20f), new Vector2(-160f, 48f));
            var line = Text(_titleRoot, "A moonlit oath. One rival. No thrones, no giants — only the blade you brought.", 20, TextAnchor.MiddleLeft, new Color(0.75f, 0.73f, 0.68f));
            Anchor(line.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(186f, -80f), new Vector2(-200f, -16f));

            _titlePrompt = Text(_titleRoot, "Press any button", 26, TextAnchor.LowerLeft, Palette.Paper);
            Anchor(_titlePrompt.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(186f, 70f), new Vector2(-80f, 120f));

            _titleMenu = NewRect("TitleMenu", _titleRoot);
            Anchor(_titleMenu, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(186f, 70f), new Vector2(560f, 280f));
            MakeMenuButton(_titleMenu, "Begin the oath", 0, "title", () => GameController.Instance?.OpenClans(), new Vector2(0f, 140f));
            MakeMenuButton(_titleMenu, "Controls", 1, "title", () => GameController.Instance?.OpenControlsFromTitle(), new Vector2(0f, 70f));
            MakeMenuButton(_titleMenu, "Quit", 2, "title", () => GameController.Instance?.QuitGame(), new Vector2(0f, 0f));
            _titleMenu.gameObject.SetActive(false);
        }

        void BuildClans(Transform parent)
        {
            _clanRoot = NewRect("Clans", parent);
            Stretch(_clanRoot);
            var bg = ImageRect("BG", _clanRoot, new Color(0.015f, 0.018f, 0.03f, 0.96f));
            Stretch(bg.rectTransform);
            var heading = Text(_clanRoot, "Choose a bloodline", 48, TextAnchor.UpperCenter, Palette.Paper);
            Anchor(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -110f), new Vector2(0f, -36f));
            var sub = Text(_clanRoot, "Three sparks survived Okumura. One will carry the oath.", 22, TextAnchor.UpperCenter, Palette.Gold);
            Anchor(sub.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -150f), new Vector2(0f, -112f));

            var row = NewRect("Row", _clanRoot);
            Anchor(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-690f, -280f), new Vector2(690f, 220f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 28f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 0; i < 3; i++)
            {
                var clan = ClanCatalog.Get((ClanId)i);
                var plate = ImageRect(clan.Name, row, new Color(0.07f, 0.08f, 0.11f, 0.94f));
                var le = plate.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 420f;
                var outline = plate.gameObject.AddComponent<Outline>();
                outline.effectDistance = new Vector2(2f, -2f);
                var spot = plate.gameObject.AddComponent<MenuHotspot>();
                int captured = i;
                spot.Index = captured;
                spot.Click = () =>
                {
                    _clanIndex = captured;
                    PaintClans();
                    GameAudio.Instance?.Play("confirm", 0.7f);
                    GameController.Instance?.ChooseClan((ClanId)captured);
                };
                var v = plate.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(26, 26, 26, 22);
                v.spacing = 8f;
                v.childAlignment = TextAnchor.UpperLeft;
                v.childControlWidth = true;
                v.childControlHeight = true;
                v.childForceExpandHeight = false;
                v.childForceExpandWidth = true;

                var stripe = ImageRect("Stripe", plate.transform, clan.Accent);
                stripe.gameObject.AddComponent<LayoutElement>().preferredHeight = 8f;
                Block(plate.transform, clan.Name, 36, Palette.Paper, 48f);
                Block(plate.transform, clan.Epithet, 18, clan.Accent, 28f);
                Block(plate.transform, "“" + clan.Creed + "”", 16, new Color(0.78f, 0.76f, 0.7f), 96f);
                Block(plate.transform, clan.Gift, 22, Palette.Gold, 36f);
                Block(plate.transform, clan.GiftDetail, 16, Palette.Paper, 110f);
                _clanPlates.Add(plate);
            }

            var foot = Text(_clanRoot, "Left stick or A/D to choose    ·    Cross or Enter to swear    ·    Circle or Esc to return", 18, TextAnchor.LowerCenter, new Color(0.7f, 0.68f, 0.6f));
            Anchor(foot.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 28f), new Vector2(0f, 64f));
            _clanRoot.gameObject.SetActive(false);
        }

        void BuildPause(Transform parent)
        {
            _pauseRoot = NewRect("Pause", parent);
            Stretch(_pauseRoot);
            var dim = ImageRect("Dim", _pauseRoot, new Color(0.01f, 0.012f, 0.02f, 0.72f));
            Stretch(dim.rectTransform);
            var card = ImageRect("Card", _pauseRoot, new Color(0.04f, 0.045f, 0.06f, 0.96f));
            Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-240f, -210f), new Vector2(240f, 210f));
            card.gameObject.AddComponent<Outline>().effectColor = Palette.Gold;
            var title = Text(card.transform, "Paused", 40, TextAnchor.UpperCenter, Palette.Paper);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), new Vector2(0f, -20f));
            AddPause("Resume", 0, () => GameController.Instance?.SetPaused(false), -10f);
            AddPause("Controls", 1, () => GameController.Instance?.OpenControlsFromPause(), -80f);
            AddPause("Return to title", 2, () => GameController.Instance?.ShowTitle(), -150f);
            _pauseRoot.gameObject.SetActive(false);
        }

        void AddPause(string label, int index, Action click, float y)
        {
            var button = MakeMenuButton(_pauseRoot, label, index, "pause", click, Vector2.zero);
            Anchor(button.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, y), new Vector2(180f, y + 54f));
            _pauseItems.Add(button.GetComponent<MenuHotspot>());
        }

        void BuildControls(Transform parent)
        {
            _controlsRoot = NewRect("Controls", parent);
            Stretch(_controlsRoot);
            var bg = ImageRect("BG", _controlsRoot, new Color(0.015f, 0.018f, 0.03f, 0.97f));
            Stretch(bg.rectTransform);
            var title = Text(_controlsRoot, "Controls", 42, TextAnchor.UpperCenter, Palette.Paper);
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -70f), new Vector2(0f, -18f));
            string body =
                "DualSense and other gamepads use the same layout. Keyboard and mouse stay live if the pad is asleep.\n\n" +
                "Move                          Left stick, D-pad          A D  or  arrows\n" +
                "Jump                          Cross                        Space\n" +
                "Dodge                         Circle                       Left Shift\n" +
                "Light combo                   Square                       J  or  left mouse\n" +
                "Heavy                         Triangle                     K  or  right mouse\n" +
                "Talk / depart                 Triangle near someone        E\n" +
                "Silent takedown               Square from behind, or R3   V   (or J from behind)\n" +
                "Throw a stone                 L2                           G  or  middle mouse\n" +
                "Water Oath                    R1                           R\n" +
                "Mirror Shade                  L1                           Q\n" +
                "Moon Focus (hold)             R2                           F\n" +
                "Pause                         Options                      Esc\n\n" +
                "Menus: stick or arrows, Cross or Enter to confirm, Circle or Esc to go back. The light bar, when the pad allows it, takes your clan color.\n" +
                "Plug a DualSense in with USB and close Steam if Steam has claimed the controller.";
            var text = Text(_controlsRoot, body, 20, TextAnchor.UpperLeft, Palette.Paper);
            Anchor(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-620f, -340f), new Vector2(620f, 300f));
            var foot = Text(_controlsRoot, "Cross, Enter, Circle, or Esc to return", 18, TextAnchor.LowerCenter, Palette.Gold);
            Anchor(foot.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 24f), new Vector2(0f, 60f));
            _controlsRoot.gameObject.SetActive(false);
        }

        void BuildEnd(Transform parent)
        {
            _endRoot = NewRect("Ending", parent);
            Stretch(_endRoot);
            var bg = ImageRect("BG", _endRoot, new Color(0.015f, 0.018f, 0.03f, 1f));
            Stretch(bg.rectTransform);
            var moon = ImageRect("Moon", _endRoot, Palette.Moon);
            moon.sprite = _circle;
            Anchor(moon.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-420f, -420f), new Vector2(-40f, -40f));
            var title = Text(_endRoot, "The oath is kept", 58, TextAnchor.MiddleCenter, Palette.Paper);
            Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(80f, 80f), new Vector2(-80f, 180f));
            var body = Text(_endRoot,
                "Kenzo falls on the cedar bridge. The moon does not applaud. It only stays.\nYamigura is a name you still get to say out loud.",
                24, TextAnchor.MiddleCenter, new Color(0.8f, 0.78f, 0.72f));
            Anchor(body.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(200f, -40f), new Vector2(-200f, 70f));
            var a = MakeMenuButton(_endRoot, "Return to Okumura", 0, "end", () => GameController.Instance?.ReturnToHamlet(), Vector2.zero);
            Anchor(a.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200f, -150f), new Vector2(200f, -90f));
            var b = MakeMenuButton(_endRoot, "Title", 1, "end", () => GameController.Instance?.ShowTitle(), Vector2.zero);
            Anchor(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-200f, -230f), new Vector2(200f, -170f));
            _endRoot.gameObject.SetActive(false);
        }

        void BuildGameOver(Transform parent)
        {
            _deadRoot = NewRect("GameOver", parent);
            Stretch(_deadRoot);
            var bg = ImageRect("BG", _deadRoot, new Color(0.03f, 0.01f, 0.012f, 0.82f));
            Stretch(bg.rectTransform);
            var title = Text(_deadRoot, "The oath breaks", 52, TextAnchor.MiddleCenter, Palette.Paper);
            Anchor(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 70f), new Vector2(0f, 160f));
            var body = Text(_deadRoot, "Unless you pick it up.", 24, TextAnchor.MiddleCenter, Palette.Gold);
            Anchor(body.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 10f), new Vector2(0f, 60f));
            var a = MakeMenuButton(_deadRoot, "Try again", 0, "dead", () => GameController.Instance?.Retry(), Vector2.zero);
            Anchor(a.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -90f), new Vector2(180f, -30f));
            var b = MakeMenuButton(_deadRoot, "Title", 1, "dead", () => GameController.Instance?.ShowTitle(), Vector2.zero);
            Anchor(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-180f, -170f), new Vector2(180f, -110f));
            _deadRoot.gameObject.SetActive(false);
        }

        Image MakeMenuButton(Transform parent, string label, int index, string group, Action click, Vector2 unused)
        {
            var image = ImageRect(label, parent, new Color(0.1f, 0.11f, 0.15f, 0.94f));
            var spot = image.gameObject.AddComponent<MenuHotspot>();
            spot.Index = index;
            spot.Click = () =>
            {
                GameAudio.Instance?.Play("confirm", 0.6f);
                click?.Invoke();
            };
            image.gameObject.name = group + ":" + index;
            var text = Text(image.transform, label, 24, TextAnchor.MiddleCenter, Palette.Paper);
            Stretch(text.rectTransform);
            return image;
        }

        List<MenuHotspot> Collect(string group)
        {
            var list = new List<MenuHotspot>();
            var spots = GetComponentsInChildren<MenuHotspot>(true);
            foreach (var spot in spots)
            {
                if (spot != null && spot.gameObject.name.StartsWith(group + ":", StringComparison.Ordinal))
                    list.Add(spot);
            }
            list.Sort((a, b) => a.Index.CompareTo(b.Index));
            return list;
        }

        Text Block(Transform parent, string value, int size, Color color, float height)
        {
            var text = Text(parent, value, size, TextAnchor.UpperLeft, color);
            var element = text.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.minHeight = height;
            return text;
        }

        Text Text(Transform parent, string value, int size, TextAnchor anchor, Color color)
        {
            var rect = NewRect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.75f);
            outline.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        Image ImageRect(string name, Transform parent, Color color)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        Image FullImage(Transform parent, Color color, string name)
        {
            var image = ImageRect(name, parent, color);
            Stretch(image.rectTransform);
            image.raycastTarget = false;
            return image;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        static Sprite MakeCircle()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float radius = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                    float a = Mathf.Clamp01(1f - Mathf.InverseLerp(0.9f, 1f, d));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
