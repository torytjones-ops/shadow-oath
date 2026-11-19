using UnityEngine;

namespace ShadowOath
{
    public static class Levels
    {
        public static Transform BuildHub()
        {
            var root = NewRoot("Okumura");
            StageLook.ApplyAtmosphere(0.012f);
            Ground(root, 20f, 48f);
            InkLine(root, 20f, 40f);
            Backdrop(root, 20f);

            House(root, 8f, 3.4f, 1f);
            House(root, 18f, 3.8f, 0.9f);
            House(root, 27f, 3.2f, 1.1f);
            Pine(root, 2f, 4.5f, 1.3f);
            Pine(root, 23f, 5.5f, 1.6f);
            Torii(root, 38f, 0f, 1.15f);
            Lantern(root, new Vector3(11f, 0f, -0.4f));
            Lantern(root, new Vector3(24f, 0f, -0.4f));
            Lantern(root, new Vector3(34f, 0f, -0.4f));

            var elder = Npc(root, new Vector3(13.5f, 0f, 0f), FigureKind.Elder, "Senae",
                new Color(0.78f, 0.76f, 0.7f), Palette.Gold,
                new[]
                {
                    "You came back with ash on your sleeves, Haru. Yamigura is gone. Okumura is what the night left us.",
                    "Three bloodlines kept a spark. The one you swore will decide how the oath is kept.",
                    "Kenzo of Clan Akagiri waits on the cedar bridge past the moonlit gate. He led the raid. He is one man, not a monster. That is enough.",
                    "Cross the ward roofs. Their lantern watch owns the street now. Come back when it is done."
                });
            elder.Finished = () =>
            {
                GameSession.ElderSpoken = true;
                if (!GameSession.ScoutSpoken) GameSession.Objective = "Speak with Toma at the gate";
                else GameSession.Objective = "Depart through the torii";
            };

            var toma = Npc(root, new Vector3(32f, 0f, 0f), FigureKind.Scout, "Toma",
                new Color(0.25f, 0.28f, 0.24f), Palette.Gold,
                new[]
                {
                    "If you want the quiet road: shadow eats their eyes. A stone turns a head. From behind, one cut is enough.",
                    "The ravine wall will hold you if you commit. Run up it, then jump before your feet give out.",
                    "Water Oath on R1 or R. Mirror Shade on L1 or Q. Hold Moon Focus — R2 or F — when the steel crowds you.",
                    "The torii opens once the elder has spoken. I will watch the road home."
                });
            toma.Finished = () =>
            {
                GameSession.ScoutSpoken = true;
                if (!GameSession.ElderSpoken) GameSession.Objective = "Speak with Elder Senae";
                else GameSession.Objective = "Depart through the torii";
            };

            TrainingPost.Spawn(new Vector3(21.5f, 0f, 0f), root);
            WaystoneMark(root, new Vector3(38f, 3.6f, 0f), Palette.Gold);

            var gate = new GameObject("Depart");
            gate.transform.SetParent(root, false);
            gate.transform.position = new Vector3(38f, 1f, 0f);
            var box = gate.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.2f, 2.4f, 2f);
            Kinematic(gate);
            gate.AddComponent<DepartGate>();

            Wall(root, -2.5f, 4f);
            Wall(root, 44f, 4f);
            PlayerActor.Spawn(new Vector3(4.2f, 0.05f, 0f), root);
            GameSession.Objective = "Speak with Elder Senae";
            return root;
        }

        public static Transform BuildMission()
        {
            var root = NewRoot("Moonlit Gate");
            StageLook.ApplyAtmosphere(0.018f);
            Backdrop(root, 70f);

            GroundSpan(root, 0f, 50f);
            InkLine(root, 25f, 46f);
            ShadowPool(root, new Vector3(8f, 1.5f, 0f), new Vector3(16f, 3.4f, 5f));
            Lantern(root, new Vector3(22f, 0f, -0.3f));
            Crate(root, 18.2f);
            GuardActor.Spawn(new Vector3(25f, 0.05f, 0f), 16f, 29f, -1, root);
            ShadowPool(root, new Vector3(36f, 1.5f, 0f), new Vector3(14f, 3.4f, 5f));
            CheckpointAt(root, new Vector3(40f, 0.05f, 0f));
            House(root, 6f, 3.6f, 0.8f);
            House(root, 30f, 4f, 0.75f);

            Hint(root, new Vector3(3f, 1f, 0f), new Vector3(4f, 2f, 3f),
                "Move with A/D or the left stick. Jump with Space or Cross.");
            Hint(root, new Vector3(8f, 1f, 0f), new Vector3(5f, 2f, 3f),
                "This is shadow. The cone is a guard's eyes — they barely see you here.");
            Hint(root, new Vector3(15f, 1f, 0f), new Vector3(4f, 2f, 3f),
                "Throw a stone with G or L2. From behind a calm guard: Square, V, or R3.");

            // Ravine wall. Low street ends, the tall face is the only way up.
            var wall = Cube(root, "Run Wall", new Vector3(51.5f, 4f, 0f), new Vector3(1.4f, 8f, 4f), Palette.Stone);
            var mark = Cube(root, "Run Mark", new Vector3(50.72f, 4.2f, -1.55f), new Vector3(0.06f, 6.2f, 0.16f), Palette.Paper);
            Object.Destroy(mark.GetComponent<Collider>());
            PitAt(root, new Vector3(50.45f, -3.2f, 0f), new Vector3(0.5f, 4f, 5f));
            Cube(root, "Roof", new Vector3(72.3f, 5.5f, 0f), new Vector3(40.2f, 1f, 6f), Palette.WoodDark);
            InkLine(root, 72f, 36f, 6.02f);
            Lantern(root, new Vector3(64f, 6f, -0.3f));
            ShadowPool(root, new Vector3(58f, 7.4f, 0f), new Vector3(10f, 3f, 5f));
            GuardActor.Spawn(new Vector3(70f, 6.05f, 0f), 60f, 82f, 1, root);
            ShadowPool(root, new Vector3(84f, 7.4f, 0f), new Vector3(12f, 3f, 5f));
            BellAt(root, new Vector3(86f, 6f, 0f));
            CheckpointAt(root, new Vector3(56f, 6.05f, 0f));
            Hint(root, new Vector3(46f, 1f, 0f), new Vector3(5f, 2.4f, 3f),
                "Jump at the marked wall and hold toward it to run up. Jump again to kick onto the roof.");
            Hint(root, new Vector3(60f, 7.2f, 0f), new Vector3(5f, 2f, 3f),
                "Light combo: Square or J. Heavy: Triangle or K. Dodge: Circle or Shift.");

            GroundSpan(root, 90f, 156f);
            InkLine(root, 123f, 60f);
            ShadowPool(root, new Vector3(98f, 1.5f, 0f), new Vector3(14f, 3.4f, 5f));
            GuardActor.Spawn(new Vector3(114f, 0.05f, 0f), 106f, 122f, 1, root);
            Lantern(root, new Vector3(116f, 0f, -0.3f));
            BellAt(root, new Vector3(124f, 0f, 0f));
            GuardActor.Spawn(new Vector3(132f, 0.05f, 0f), 126f, 140f, -1, root);
            ShadowPool(root, new Vector3(142f, 1.5f, 0f), new Vector3(12f, 3.4f, 5f));
            CheckpointAt(root, new Vector3(94f, 0.05f, 0f));
            Torii(root, 150f, 0f, 1.25f);
            WaystoneMark(root, new Vector3(150f, 3.8f, 0f), Palette.Crimson);
            House(root, 108f, 3.5f, 0.85f);
            Pine(root, 136f, 4.2f, 1.4f);

            var duel = new GameObject("Duel Gate");
            duel.transform.SetParent(root, false);
            duel.transform.position = new Vector3(150f, 1.2f, 0f);
            var trigger = duel.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 3f, 3f);
            Kinematic(duel);
            duel.AddComponent<DuelGate>();
            Wall(root, -2f, 5f);
            Wall(root, 158f, 5f);

            Hint(root, new Vector3(96f, 1f, 0f), new Vector3(4f, 2f, 3f),
                "Water Oath: R or R1. Mirror Shade: Q or L1. Hold F or R2 for Moon Focus.");

            PlayerActor.Spawn(new Vector3(2.5f, 0.05f, 0f), root);
            GameSession.Objective = "Reach the cedar bridge";
            return root;
        }

        public static Transform BuildDuel()
        {
            var root = NewRoot("Cedar Bridge");
            StageLook.ApplyAtmosphere(0.01f);
            Backdrop(root, 14f);

            Cube(root, "Bridge", new Vector3(14f, 7.5f, 0f), new Vector3(30f, 1f, 5.2f), Palette.WoodDark);
            InkLine(root, 14f, 26f, 8.02f);
            Cube(root, "Rail L", new Vector3(14f, 8.55f, 2.15f), new Vector3(28f, 0.12f, 0.12f), Palette.Vermilion);
            Cube(root, "Rail R", new Vector3(14f, 8.55f, -2.15f), new Vector3(28f, 0.12f, 0.12f), Palette.Vermilion);
            for (int i = 0; i < 6; i++)
            {
                float x = 1.5f + i * 5f;
                Cube(root, "Post", new Vector3(x, 8.3f, 2.15f), new Vector3(0.16f, 0.9f, 0.16f), Palette.Wood);
                Cube(root, "Post", new Vector3(x, 8.3f, -2.15f), new Vector3(0.16f, 0.9f, 0.16f), Palette.Wood);
            }
            Lantern(root, new Vector3(6f, 8.05f, -1.4f));
            Lantern(root, new Vector3(22f, 8.05f, -1.4f));
            Wall(root, -0.4f, 8f, 9f);
            Wall(root, 28.4f, 8f, 9f);
            Pine(root, -4f, 6f, 2.2f);
            Pine(root, 32f, 6f, 2.4f);

            PlayerActor.Spawn(new Vector3(4f, 8.08f, 0f), root);
            if (GameSession.Player != null) GameSession.Player.Respawn = new Vector3(4f, 8.08f, 0f);
            RivalActor.Spawn(new Vector3(22f, 8.08f, 0f), root);
            GameSession.Objective = "Duel Kenzo of Clan Akagiri";
            return root;
        }

        static Transform NewRoot(string name)
        {
            var root = new GameObject(name).transform;
            GameSession.LevelRoot = root;
            return root;
        }

        static void Ground(Transform parent, float centerX, float width)
        {
            Cube(parent, "Ground", new Vector3(centerX, -0.5f, 0f), new Vector3(width, 1f, 7f), Palette.Stone);
            Cube(parent, "Back Street", new Vector3(centerX, -0.55f, 4.2f), new Vector3(width + 6f, 1f, 5f), new Color(0.12f, 0.13f, 0.16f));
        }

        static void GroundSpan(Transform parent, float min, float max)
        {
            float width = max - min;
            float center = (min + max) * 0.5f;
            Cube(parent, "Ground", new Vector3(center, -0.5f, 0f), new Vector3(width, 1f, 7f), Palette.Stone);
            Cube(parent, "Back Street", new Vector3(center, -0.55f, 4.2f), new Vector3(width, 1f, 5f), new Color(0.11f, 0.12f, 0.15f));
        }

        static void InkLine(Transform parent, float centerX, float width, float y = 0.03f)
        {
            Cube(parent, "Ink Line", new Vector3(centerX, y, -0.2f), new Vector3(width, 0.025f, 0.18f), Palette.Ink);
        }

        static void Wall(Transform parent, float x, float height, float yBase = 0f)
        {
            Cube(parent, "Stop", new Vector3(x, yBase + height * 0.5f, 0f), new Vector3(0.6f, height, 4f), Palette.Stone);
        }

        static void Crate(Transform parent, float x)
        {
            Cube(parent, "Crate", new Vector3(x, 0.7f, 0f), new Vector3(1.35f, 1.4f, 1.35f), Palette.Wood);
        }

        static void ShadowPool(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Shadow");
            go.layer = 12;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            go.AddComponent<ShadowVolume>();
            var tint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tint.name = "Shadow Tint";
            Object.Destroy(tint.GetComponent<Collider>());
            tint.layer = 12;
            tint.transform.SetParent(go.transform, false);
            tint.transform.localPosition = Vector3.zero;
            tint.transform.localScale = size;
            tint.GetComponent<Renderer>().sharedMaterial = StageLook.Ghost(new Color(0.02f, 0.03f, 0.08f, 0.28f), 0.28f);
        }

        static void CheckpointAt(Transform parent, Vector3 position)
        {
            var go = new GameObject("Checkpoint");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.2f, 2f, 2f);
            Kinematic(go);
            go.AddComponent<Checkpoint>();
        }

        static void PitAt(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Pit");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            Kinematic(go);
            go.AddComponent<Pit>();
        }

        static void Hint(Transform parent, Vector3 position, Vector3 size, string text)
        {
            var go = new GameObject("Hint");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            Kinematic(go);
            var hint = go.AddComponent<HintZone>();
            hint.Text = text;
        }

        static void BellAt(Transform parent, Vector3 ground)
        {
            var post = Cube(parent, "Bell Post", ground + new Vector3(0f, 1.3f, 0f), new Vector3(0.12f, 2.6f, 0.12f), Palette.Wood);
            var bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bell.name = "Bell";
            bell.layer = 15;
            bell.transform.SetParent(parent, false);
            bell.transform.position = ground + new Vector3(0.35f, 2.35f, 0f);
            bell.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
            bell.GetComponent<Renderer>().sharedMaterial = StageLook.Lit(Palette.Gold, 0.4f);
            bell.AddComponent<Bell>();
        }

        static void Lantern(Transform parent, Vector3 ground)
        {
            Cube(parent, "Lantern Post", ground + new Vector3(0f, 1.1f, 0f), new Vector3(0.1f, 2.2f, 0.1f), Palette.WoodDark);
            var globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(globe.GetComponent<Collider>());
            globe.name = "Lantern";
            globe.transform.SetParent(parent, false);
            globe.transform.position = ground + new Vector3(0f, 2.25f, 0f);
            globe.transform.localScale = Vector3.one * 0.34f;
            globe.GetComponent<Renderer>().sharedMaterial = StageLook.Lit(Palette.Gold, 1.8f);
            var lightGo = new GameObject("Lantern Light");
            lightGo.transform.SetParent(parent, false);
            lightGo.transform.position = ground + new Vector3(0f, 2.25f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.72f, 0.4f);
            light.intensity = 3.2f;
            light.range = 8.5f;
            light.shadows = LightShadows.None;
        }

        static void Torii(Transform parent, float x, float y, float scale)
        {
            float height = 3.3f * scale;
            float half = 1.35f * scale;
            Cube(parent, "Torii Post", new Vector3(x - half, y + height * 0.5f, 0f), new Vector3(0.22f * scale, height, 0.22f * scale), Palette.Vermilion);
            Cube(parent, "Torii Post", new Vector3(x + half, y + height * 0.5f, 0f), new Vector3(0.22f * scale, height, 0.22f * scale), Palette.Vermilion);
            Cube(parent, "Torii Beam", new Vector3(x, y + height * 0.92f, 0f), new Vector3(half * 2.5f, 0.18f * scale, 0.28f * scale), Palette.Vermilion);
            Cube(parent, "Torii Beam", new Vector3(x, y + height * 0.78f, 0f), new Vector3(half * 2.1f, 0.1f * scale, 0.18f * scale), Palette.Ink);
        }

        static void House(Transform parent, float x, float z, float scale)
        {
            Cube(parent, "House", new Vector3(x, 0.9f * scale, z), new Vector3(2.4f * scale, 1.8f * scale, 2.2f * scale), Palette.WoodDark);
            var roofL = Cube(parent, "Roof", new Vector3(x - 0.55f * scale, 2.05f * scale, z), new Vector3(1.5f * scale, 0.16f * scale, 2.5f * scale), Palette.Roof);
            roofL.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            var roofR = Cube(parent, "Roof", new Vector3(x + 0.55f * scale, 2.05f * scale, z), new Vector3(1.5f * scale, 0.16f * scale, 2.5f * scale), Palette.Roof);
            roofR.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        }

        static void Pine(Transform parent, float x, float z, float scale)
        {
            Cube(parent, "Trunk", new Vector3(x, 0.7f * scale, z), new Vector3(0.18f * scale, 1.4f * scale, 0.18f * scale), Palette.WoodDark);
            var a = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(a.GetComponent<Collider>());
            a.transform.SetParent(parent, false);
            a.transform.position = new Vector3(x, 1.7f * scale, z);
            a.transform.localScale = new Vector3(1.1f, 1.5f, 1.1f) * scale;
            a.GetComponent<Renderer>().sharedMaterial = StageLook.Lit(Palette.Pine);
            StageLook.AddOutline(a);
        }

        static void WaystoneMark(Transform parent, Vector3 position, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(go.GetComponent<Collider>());
            go.name = "Waystone";
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.28f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            go.GetComponent<Renderer>().sharedMaterial = StageLook.Lit(color, 1.5f);
            go.AddComponent<Waystone>();
        }

        static void Backdrop(Transform parent, float anchorX)
        {
            var far = new GameObject("Far Hills").transform;
            far.SetParent(parent, false);
            far.position = new Vector3(anchorX, 0f, 26f);
            var parallax = far.gameObject.AddComponent<ParallaxStrip>();
            parallax.Factor = 0.78f;
            for (int i = -8; i <= 8; i++)
            {
                float h = 3.5f + (i % 3) * 1.4f;
                Cube(far, "Hill", new Vector3(i * 8f, h * 0.5f, 0f), new Vector3(9f, h, 4f), new Color(0.06f, 0.07f, 0.11f));
            }
            var mid = new GameObject("Pines").transform;
            mid.SetParent(parent, false);
            mid.position = new Vector3(anchorX, 0f, 12f);
            var midParallax = mid.gameObject.AddComponent<ParallaxStrip>();
            midParallax.Factor = 0.45f;
            for (int i = -10; i <= 10; i++)
            {
                if (i % 2 == 0) Pine(mid, i * 4.5f, 0f, 1.8f + (i % 3) * 0.25f);
            }
        }

        static NpcActor Npc(Transform parent, Vector3 position, FigureKind kind, string name, Color robe, Color accent, string[] lines)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Figure.Build(go.transform, kind, robe, accent);
            var npc = go.AddComponent<NpcActor>();
            npc.DisplayName = name;
            npc.Lines = lines;
            return npc;
        }

        static void Kinematic(GameObject go)
        {
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        static GameObject Cube(Transform parent, string name, Vector3 center, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = StageLook.Lit(color);
            StageLook.AddOutline(go);
            return go;
        }
    }
}
