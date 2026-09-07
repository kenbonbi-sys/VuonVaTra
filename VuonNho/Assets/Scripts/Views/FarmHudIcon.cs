using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VuonNho.Views
{
    /// <summary>
    /// Small, coloured illustrations for the farm HUD. All artwork is built from
    /// UI vertices, so it stays crisp at any canvas scale and needs no icon font.
    /// Coordinates use a square, 100-unit drawing space with Y pointing up.
    /// </summary>
    [AddComponentMenu("UI/Farm HUD Icon")]
    public sealed class FarmHudIcon : MaskableGraphic
    {
        public enum Kind
        {
            Avatar, Coin, Banknotes, Crate, Leaf, Heart, Book, House,
            Clipboard, Tools, Workers, Settings, Reset, Play, Plus,
            Factory, Sprout, Star
        }

        public Kind Icon;

        private static readonly Color Cream = Hex(0xFFF6DA);
        private static readonly Color Paper = Hex(0xF8EBCD);
        private static readonly Color Brown = Hex(0x896849);
        private static readonly Color DarkBrown = Hex(0x574738);
        private static readonly Color Jade = Hex(0x559475);
        private static readonly Color DarkJade = Hex(0x3F7560);
        private static readonly Color Mint = Hex(0x85B49A);
        private static readonly Color Gold = Hex(0xF5BB39);
        private static readonly Color DarkGold = Hex(0xD89B2E);
        private static readonly Color Teal = Hex(0x78A9A6);
        private static readonly Color Coral = Hex(0xD97554);
        private static readonly Color Shadow = new Color(0.28f, 0.30f, 0.20f, 0.14f);

        private VertexHelper mesh;
        private Rect drawingRect;
        private float drawingScale;
        private Vector2 drawingOffset;
        private Vector2 partOffset;
        private float partScale = 1f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            mesh = vh;
            drawingRect = GetPixelAdjustedRect();
            drawingScale = Mathf.Min(drawingRect.width, drawingRect.height) / 100f;
            drawingOffset = drawingRect.center - Vector2.one * drawingScale * 50f;
            partOffset = Vector2.zero;
            partScale = 1f;

            switch (Icon)
            {
                case Kind.Avatar: DrawAvatar(); break;
                case Kind.Coin: DrawCoin(); break;
                case Kind.Banknotes: DrawBanknotes(); break;
                case Kind.Crate: DrawCrate(); break;
                case Kind.Leaf: DrawLeaf(); break;
                case Kind.Heart: DrawHeart(); break;
                case Kind.Book: DrawBook(); break;
                case Kind.House: DrawHouse(); break;
                case Kind.Clipboard: DrawClipboard(); break;
                case Kind.Tools: DrawTools(); break;
                case Kind.Workers: DrawWorkers(); break;
                case Kind.Settings: DrawSettings(); break;
                case Kind.Reset: DrawReset(); break;
                case Kind.Play: DrawPlay(); break;
                case Kind.Plus: DrawPlus(); break;
                case Kind.Factory: DrawFactory(); break;
                case Kind.Sprout: DrawSprout(); break;
                case Kind.Star: DrawStar(); break;
            }
        }

        private void DrawAvatar()
        {
            Ellipse(50, 49, 45, 45, Hex(0xD7DAB5));
            Ellipse(50, 51, 41, 41, Cream);
            Ellipse(50, 49, 35, 36, Hex(0xE6DEC0));
            DrawPerson(0, 0, 1f, true);
        }

        // The same little farmer is also used for the workers button.
        private void DrawPerson(float x, float y, float scale, bool darkHair)
        {
            Vector2 previousOffset = partOffset;
            float previousScale = partScale;
            partOffset = previousOffset + new Vector2(x, y) * previousScale;
            partScale *= scale;

            Ellipse(51, 20, 25, 7, Shadow);
            Poly(DarkJade, 25, 17, 26, 30, 35, 39, 65, 39, 75, 30, 76, 17, 62, 11, 38, 11);
            Poly(Teal, 27, 18, 30, 31, 40, 38, 61, 38, 70, 29, 72, 18, 58, 13, 40, 13);
            Poly(Hex(0x548A89), 60, 37, 70, 29, 72, 18, 58, 13, 56, 29);
            Box(44, 35, 12, 13, Hex(0xDAA274));
            Poly(Cream, 39, 37, 46, 31, 50, 37, 54, 31, 62, 37, 55, 41, 44, 41);
            Line(50, 30, 50, 17, 2.2f, Hex(0x3F7677));
            Ellipse(55, 24, 1.1f, 1.1f, Paper);
            Ellipse(32, 56, 5, 7, Hex(0xDCA879));
            Ellipse(68, 56, 5, 7, Hex(0xDCA879));
            Ellipse(50, 59, 20, 25, Hex(0xE6B48A));
            Ellipse(46, 61, 16, 21, Hex(0xF2C79D));
            Poly(Hex(0xE6B48A), 61, 68, 67, 60, 65, 47, 56, 38, 55, 43, 61, 51);
            if (darkHair)
            {
                Ellipse(47, 78, 19, 10, DarkBrown);
                Poly(DarkBrown, 30, 52, 26, 66, 30, 79, 44, 87, 61, 84, 71, 73, 70, 55, 65, 57, 62, 71, 55, 74, 42, 65, 34, 67, 34, 54);
                Poly(Hex(0x745746), 29, 72, 34, 82, 48, 87, 61, 82, 65, 76, 53, 80, 43, 73, 33, 69);
            }
            else
            {
                Poly(DarkJade, 27, 69, 31, 82, 59, 85, 72, 75, 71, 68);
                Poly(Teal, 30, 73, 34, 82, 58, 84, 69, 76, 61, 71);
                Poly(Mint, 26, 71, 60, 74, 76, 67, 43, 65);
            }
            Ellipse(41, 57, 2, 2.6f, DarkBrown);
            Ellipse(58, 57, 2, 2.6f, DarkBrown);
            Ellipse(41.5f, 58, 0.6f, 0.7f, Cream);
            Ellipse(58.5f, 58, 0.6f, 0.7f, Cream);
            Line(48, 50, 51, 49, 1.8f, Hex(0xD8A073));
            Arc(50, 49, 7, 5, 212, 326, 1.7f, Hex(0xA66651));
            partOffset = previousOffset;
            partScale = previousScale;
        }

        private void DrawCoin()
        {
            Ellipse(52, 17, 30, 7, Shadow);
            Ellipse(53, 49, 34, 38, Hex(0xC48A2C));
            Ellipse(48, 53, 34, 38, Gold);
            Ellipse(48, 53, 28, 32, Hex(0xFFE083));
            Ellipse(48, 53, 24, 28, Hex(0xEEAC28));
            Arc(48, 53, 30, 34, 44, 152, 3.5f, Hex(0xFFEFAE));
            // A hand-shaped, embossed currency mark instead of a font glyph.
            Line(49, 75, 49, 32, 4.6f, DarkGold);
            Arc(49, 62, 9, 8, 50, 258, 5.6f, DarkGold);
            Arc(49, 46, 9, 8, 230, 438, 5.6f, DarkGold);
            Line(47, 77, 47, 34, 3.8f, Hex(0xFFE48A));
            Arc(47, 64, 9, 8, 50, 258, 4.4f, Hex(0xFFE48A));
            Arc(47, 48, 9, 8, 230, 438, 4.4f, Hex(0xFFE48A));
        }

        private void DrawBanknotes()
        {
            Ellipse(51, 24, 35, 8, Shadow);
            Poly(Hex(0x467747), 8, 44, 61, 74, 93, 52, 93, 43, 39, 15, 8, 35);
            Poly(Hex(0x7CAC55), 9, 42, 62, 71, 91, 51, 38, 22);
            Poly(Hex(0x4E853F), 10, 50, 62, 80, 94, 59, 94, 51, 41, 22, 10, 43);
            Poly(Hex(0x9DCF69), 10, 51, 62, 81, 94, 60, 41, 30);
            Poly(Hex(0x76AE51), 20, 51, 62, 75, 83, 60, 41, 36);
            Poly(Hex(0x8BC75C), 29, 51, 63, 70, 77, 60, 41, 41);
            Ellipse(52, 56, 12, 10, Hex(0x639849), -29);
            Poly(Hex(0xB5DE7C), 47, 51, 48, 59, 58, 63, 56, 54);
            Line(45, 48, 59, 64, 1.8f, Hex(0xB5DE7C));
            Line(25, 48, 33, 42, 2.6f, Hex(0xC4E69A));
            Line(70, 68, 78, 63, 2.6f, Hex(0xC4E69A));
            Line(17, 37, 37, 24, 1.5f, Hex(0xB0CE85));
        }

        private void DrawCrate()
        {
            Ellipse(51, 16, 30, 7, Shadow);
            Poly(Hex(0xD49533), 18, 29, 18, 69, 52, 81, 85, 64, 85, 25, 53, 12);
            Poly(Hex(0xF0AF3E), 18, 69, 52, 54, 52, 13, 18, 29);
            Poly(Hex(0xD5912C), 52, 54, 85, 65, 85, 25, 52, 13);
            Poly(Hex(0xFFD56A), 18, 69, 50, 83, 85, 65, 52, 52);
            Poly(Hex(0xFFE598), 32, 75, 43, 79, 77, 62, 65, 57);
            Poly(Hex(0xF7CA60), 65, 57, 77, 62, 77, 43, 65, 38);
            Poly(Hex(0xFFD979), 22, 64, 48, 53, 48, 48, 22, 59);
            Poly(Hex(0xEFB64A), 58, 31, 70, 36, 70, 26, 58, 21);
            Line(23, 33, 34, 28, 2, Hex(0xCD8B31));
        }

        private void DrawLeaf()
        {
            Ellipse(52, 19, 25, 6, Shadow);
            Poly(DarkJade, 21, 28, 16, 49, 34, 73, 64, 85, 80, 61, 77, 31, 57, 18, 36, 19);
            Poly(Jade, 19, 48, 35, 72, 63, 83, 55, 52, 36, 21, 23, 29);
            Poly(Hex(0x79AC64), 35, 72, 63, 83, 55, 52, 21, 48);
            Poly(Hex(0x5B9757), 55, 52, 78, 60, 75, 33, 56, 20, 36, 21);
            Line(32, 25, 63, 76, 2.7f, Hex(0x96BA79));
            Line(48, 48, 33, 57, 1.8f, Hex(0x96BA79));
            Line(51, 51, 69, 47, 1.8f, Hex(0x96BA79));
        }

        private void DrawHeart()
        {
            Ellipse(50, 17, 27, 6, Shadow);
            HeartShape(2, -3, Hex(0xB45C48));
            HeartShape(0, 0, Hex(0xE57E67));
            Ellipse(33, 65, 12, 11, Hex(0xED9580));
            RoundBox(43, 36, 14, 34, 3, Cream);
            RoundBox(33, 46, 34, 14, 3, Cream);
        }

        private void HeartShape(float x, float y, Color fill)
        {
            Ellipse(34 + x, 63 + y, 21, 20, fill);
            Ellipse(66 + x, 63 + y, 21, 20, fill);
            Poly(fill, 14 + x, 58 + y, 85 + x, 58 + y, 80 + x, 45 + y,
                66 + x, 28 + y, 50 + x, 17 + y, 34 + x, 28 + y, 20 + x, 45 + y);
        }

        private void DrawBook()
        {
            Ellipse(50, 13, 28, 5, Shadow);
            Poly(Hex(0x96633C), 15, 17, 31, 84, 81, 80, 82, 73, 67, 12);
            Poly(Paper, 20, 17, 34, 78, 79, 75, 65, 16);
            Poly(Hex(0xD9C89E), 21, 17, 65, 14, 68, 23, 23, 26);
            Poly(Hex(0xAA713F), 13, 24, 28, 88, 80, 84, 65, 20);
            Poly(Hex(0xC58B50), 20, 29, 33, 83, 76, 79, 63, 24);
            Line(28, 84, 15, 25, 3.5f, Hex(0x80573B));
            Poly(Hex(0xDFA65F), 33, 74, 67, 71, 58, 34, 24, 37);
            Poly(Hex(0xBC8148), 36, 68, 62, 66, 56, 41, 30, 43);
            Line(40, 60, 57, 59, 2.2f, Paper);
            Line(37, 54, 55, 53, 2.2f, Paper);
            Line(35, 48, 50, 47, 2.2f, Paper);
        }

        private void DrawHouse()
        {
            Ellipse(51, 13, 37, 7, Shadow);
            Poly(Hex(0xB8A17A), 17, 16, 22, 12, 79, 12, 85, 18, 77, 23, 24, 23);
            Poly(Hex(0xE4D3AC), 24, 19, 24, 57, 50, 81, 76, 56, 76, 19);
            Poly(Cream, 26, 20, 26, 57, 48, 78, 48, 20);
            Poly(Hex(0xC4AD84), 64, 71, 64, 89, 76, 89, 76, 59);
            Box(64, 87, 14, 5, Brown);
            Poly(Hex(0xA8543D), 9, 53, 46, 88, 85, 56, 83, 48, 49, 74, 16, 45);
            Poly(Coral, 10, 59, 45, 91, 86, 61, 76, 51, 47, 75, 20, 50);
            Poly(Hex(0xE38B64), 10, 59, 45, 91, 48, 83, 20, 50);
            Box(42, 20, 16, 31, Brown);
            Box(45, 21, 10, 28, Hex(0x9C8056));
            Ellipse(53, 35, 1.4f, 1.4f, Gold);
            Box(62, 36, 11, 14, Paper);
            Box(64, 38, 7, 10, Teal);
            Box(28, 36, 10, 14, Paper);
            Box(30, 38, 6, 10, Teal);
            Box(38, 15, 24, 5, Hex(0xD9C7A0));
        }

        private void DrawClipboard()
        {
            Ellipse(51, 13, 29, 6, Shadow);
            RoundBox(18, 13, 65, 75, 5, Hex(0x917553));
            RoundBox(18, 17, 61, 73, 5, Hex(0xB59A70));
            RoundBox(24, 22, 48, 60, 2, Paper);
            Box(28, 23, 42, 56, Cream);
            RoundBox(36, 78, 28, 13, 4, Hex(0x769A9A));
            RoundBox(40, 84, 20, 10, 5, Hex(0x8CACAA));
            Ellipse(50, 89, 3, 2.4f, Paper);
            for (int i = 0; i < 3; i++)
            {
                float y = 64 - 13 * i;
                Box(31, y - 3, 6, 6, Hex(0xC0B18C));
                Line(43, y, 63, y, 3, Brown);
            }
            Line(32, 64, 34, 62, 1.7f, Jade);
            Line(34, 62, 38, 67, 1.7f, Jade);
        }

        private void DrawTools()
        {
            Ellipse(51, 15, 29, 5, Shadow);
            Line(24, 23, 65, 74, 11, Hex(0x765F43));
            Line(25, 26, 64, 75, 7, Hex(0xB18A56));
            Poly(Hex(0x738F8D), 47, 78, 61, 91, 77, 82, 83, 68, 76, 61, 69, 76, 62, 76, 55, 69);
            Poly(Hex(0x99B2AD), 47, 78, 61, 91, 74, 84, 61, 75, 55, 69);
            Line(70, 22, 35, 70, 10, Hex(0x735E45));
            Line(69, 25, 35, 70, 6, Hex(0xD1AB74));
            Poly(Hex(0x607D7B), 17, 83, 27, 91, 52, 61, 42, 51, 32, 57);
            Poly(Hex(0x9AB1AB), 16, 84, 26, 92, 48, 66, 38, 58, 29, 65);
            Line(61, 23, 71, 16, 5, Brown);
            Line(71, 16, 78, 24, 5, Brown);
            Line(78, 24, 70, 33, 5, Brown);
        }

        private void DrawWorkers()
        {
            DrawPerson(37, 20, 0.58f, false);
            DrawPerson(-2, 4, 0.84f, true);
        }

        private void DrawSettings()
        {
            // A true mesh ring keeps the centre transparent on every surface.
            Gear(52, 47, Hex(0x637976));
            Gear(49, 52, Hex(0x91A19A));
            Arc(49, 52, 17, 17, 25, 155, 3, Hex(0xB9C1AD));
        }

        private void Gear(float cx, float cy, Color fill)
        {
            const int count = 48;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                float b = (i + 1) * Mathf.PI * 2f / count;
                float ra = i % 6 == 1 || i % 6 == 2 || i % 6 == 3 ? 40 : 31;
                int j = (i + 1) % count;
                float rb = j % 6 == 1 || j % 6 == 2 || j % 6 == 3 ? 40 : 31;
                Quad(
                    new Vector2(cx + Mathf.Cos(a) * ra, cy + Mathf.Sin(a) * ra),
                    new Vector2(cx + Mathf.Cos(b) * rb, cy + Mathf.Sin(b) * rb),
                    new Vector2(cx + Mathf.Cos(b) * 15, cy + Mathf.Sin(b) * 15),
                    new Vector2(cx + Mathf.Cos(a) * 15, cy + Mathf.Sin(a) * 15), fill);
            }
        }

        private void DrawReset()
        {
            Arc(49, 48, 29, 29, 33, 171, 11, Hex(0xAD906B));
            Arc(49, 48, 29, 29, 215, 348, 11, Hex(0xAD906B));
            Arc(49, 51, 29, 29, 33, 171, 9, Brown);
            Arc(49, 51, 29, 29, 215, 348, 9, Brown);
            Poly(Brown, 66, 82, 84, 70, 65, 59);
            Poly(Brown, 31, 21, 13, 32, 32, 44);
        }

        private void DrawPlay()
        {
            Ellipse(50, 47, 39, 39, Hex(0xB69C73));
            Ellipse(50, 52, 39, 39, Brown);
            Arc(50, 52, 35, 35, 48, 137, 2.5f, Hex(0xA78861));
            Poly(Hex(0x71553F), 40, 28, 40, 72, 70, 50);
            Poly(Cream, 38, 32, 38, 74, 68, 53);
        }

        private void DrawPlus()
        {
            Ellipse(50, 46, 41, 40, Hex(0x659148));
            Ellipse(50, 51, 41, 40, Hex(0x85B85D));
            Ellipse(48, 55, 35, 33, Hex(0x93C269));
            RoundBox(43, 26, 14, 51, 4, Cream);
            RoundBox(25, 44, 50, 14, 4, Cream);
        }

        private void DrawFactory()
        {
            Ellipse(52, 14, 38, 6, Shadow);
            Box(67, 43, 13, 45, DarkJade);
            Box(67, 46, 8, 41, Teal);
            Box(65, 84, 17, 5, Brown);
            Ellipse(72, 94, 8, 4, new Color(0.77f, 0.79f, 0.68f, 0.6f));
            Poly(Hex(0xDDCFAD), 13, 18, 13, 57, 39, 72, 39, 54, 59, 67, 59, 50, 85, 50, 85, 18);
            Poly(Cream, 13, 18, 13, 57, 39, 72, 39, 54, 59, 67, 59, 50, 76, 50, 76, 18);
            Poly(DarkJade, 9, 56, 39, 76, 42, 70, 15, 51);
            Poly(DarkJade, 37, 53, 59, 70, 63, 64, 43, 49);
            Box(59, 46, 29, 6, Teal);
            Box(23, 36, 12, 14, Teal);
            Box(44, 36, 12, 14, Teal);
            Box(63, 19, 12, 20, Brown);
            Box(11, 15, 78, 5, Hex(0xB9A480));
        }

        private void DrawSprout()
        {
            Ellipse(50, 18, 31, 8, Shadow);
            Ellipse(50, 24, 31, 10, Hex(0x9C7D52));
            Ellipse(49, 27, 29, 8, Hex(0xB89361));
            Line(50, 27, 50, 67, 6, DarkJade);
            Line(48, 28, 48, 67, 2.3f, Mint);
            Poly(DarkJade, 50, 48, 39, 46, 24, 52, 17, 68, 18, 79, 35, 77, 46, 66);
            Poly(Hex(0x8BAF65), 49, 50, 34, 55, 18, 79, 35, 77, 46, 66);
            Line(27, 70, 47, 52, 2, Hex(0xA9C37D));
            Poly(DarkJade, 49, 57, 53, 77, 67, 87, 83, 88, 82, 71, 70, 59);
            Poly(Hex(0x74A65E), 49, 58, 53, 77, 67, 87, 83, 88, 66, 69);
            Line(52, 61, 74, 81, 2, Hex(0xA9C37D));
            Ellipse(33, 25, 3, 1.5f, Hex(0xD3AF7A));
            Ellipse(66, 26, 4, 1.5f, Hex(0x94764F));
        }

        private void DrawStar()
        {
            Vector2[] points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (90 + 36 * i) * Mathf.Deg2Rad;
                float radius = i % 2 == 0 ? 43 : 22;
                points[i] = new Vector2(51 + Mathf.Cos(angle) * radius, 46 + Mathf.Sin(angle) * radius);
            }
            Polygon(points, Hex(0xCC922D));
            for (int i = 0; i < points.Length; i++) points[i] += new Vector2(-1, 5);
            Polygon(points, Gold);
            Poly(Hex(0xFFD975), 50, 94, 40, 61, 9, 64, 35, 46, 50, 51);
            Poly(Hex(0xEAAA32), 50, 51, 75, 17, 71, 44, 91, 64, 63, 61);
            Poly(Hex(0xFFE9A3), 50, 80, 45, 60, 25, 62, 42, 52, 50, 55);
        }

        private static Color Hex(uint rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        private int Vertex(Vector2 point, Color fill)
        {
            int index = mesh.currentVertCount;
            Vector2 pos = drawingOffset + (partOffset + point * partScale) * drawingScale;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = new Vector3(pos.x, pos.y, 0f);
            vertex.color = fill * color;
            vertex.uv0 = Vector2.zero;
            mesh.AddVert(vertex);
            return index;
        }

        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color fill)
        {
            int start = Vertex(a, fill);
            Vertex(b, fill);
            Vertex(c, fill);
            Vertex(d, fill);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }

        private void Box(float x, float y, float width, float height, Color fill)
        {
            Quad(new Vector2(x, y), new Vector2(x + width, y),
                new Vector2(x + width, y + height), new Vector2(x, y + height), fill);
        }

        private void RoundBox(float x, float y, float width, float height, float radius, Color fill)
        {
            radius = Mathf.Min(radius, Mathf.Min(width, height) * 0.5f);
            Box(x + radius, y, width - radius * 2, height, fill);
            Box(x, y + radius, width, height - radius * 2, fill);
            Ellipse(x + radius, y + radius, radius, radius, fill);
            Ellipse(x + width - radius, y + radius, radius, radius, fill);
            Ellipse(x + radius, y + height - radius, radius, radius, fill);
            Ellipse(x + width - radius, y + height - radius, radius, radius, fill);
        }

        private void Ellipse(float cx, float cy, float rx, float ry, Color fill, float rotation = 0)
        {
            const int count = 40;
            float radians = rotation * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            int center = Vertex(new Vector2(cx, cy), fill);
            float feather = Mathf.Min(0.7f, Mathf.Min(rx, ry) * 0.35f);
            for (int i = 0; i <= count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                float x = Mathf.Cos(angle) * (rx - feather);
                float y = Mathf.Sin(angle) * (ry - feather);
                Vertex(new Vector2(cx + x * cos - y * sin, cy + x * sin + y * cos), fill);
                x = Mathf.Cos(angle) * (rx + feather);
                y = Mathf.Sin(angle) * (ry + feather);
                Color edge = fill;
                edge.a = 0;
                Vertex(new Vector2(cx + x * cos - y * sin, cy + x * sin + y * cos), edge);
                if (i == 0) continue;
                int inner = center + 1 + i * 2;
                mesh.AddTriangle(center, inner - 2, inner);
                mesh.AddTriangle(inner - 2, inner - 1, inner + 1);
                mesh.AddTriangle(inner + 1, inner, inner - 2);
            }
        }

        private void Line(float ax, float ay, float bx, float by, float thickness, Color fill)
        {
            Vector2 a = new Vector2(ax, ay);
            Vector2 b = new Vector2(bx, by);
            Vector2 direction = (b - a).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;
            Quad(a + normal, b + normal, b - normal, a - normal, fill);
            Ellipse(ax, ay, thickness * 0.5f, thickness * 0.5f, fill);
            Ellipse(bx, by, thickness * 0.5f, thickness * 0.5f, fill);
        }

        private void Arc(float cx, float cy, float rx, float ry, float from, float to, float thickness, Color fill)
        {
            int steps = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(to - from) / 9f));
            float half = thickness * 0.5f;
            float feather = Mathf.Min(0.6f, half * 0.35f);
            Color clear = fill;
            clear.a = 0;
            int start = mesh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(from, to, (float)i / steps) * Mathf.Deg2Rad;
                Vector2 centre = new Vector2(cx + Mathf.Cos(angle) * rx, cy + Mathf.Sin(angle) * ry);
                Vector2 normal = new Vector2(Mathf.Cos(angle) / rx, Mathf.Sin(angle) / ry).normalized;
                Vertex(centre + normal * (half + feather), clear);
                Vertex(centre + normal * (half - feather), fill);
                Vertex(centre - normal * (half - feather), fill);
                Vertex(centre - normal * (half + feather), clear);
                if (i == 0) continue;
                int current = start + i * 4;
                for (int band = 0; band < 3; band++)
                {
                    mesh.AddTriangle(current - 4 + band, current + band, current + band + 1);
                    mesh.AddTriangle(current + band + 1, current - 3 + band, current - 4 + band);
                }
            }
            float first = from * Mathf.Deg2Rad;
            float last = to * Mathf.Deg2Rad;
            Ellipse(cx + Mathf.Cos(first) * rx, cy + Mathf.Sin(first) * ry, half, half, fill);
            Ellipse(cx + Mathf.Cos(last) * rx, cy + Mathf.Sin(last) * ry, half, half, fill);
        }

        private void Poly(Color fill, params float[] coordinates)
        {
            Vector2[] points = new Vector2[coordinates.Length / 2];
            for (int i = 0; i < points.Length; i++)
                points[i] = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
            Polygon(points, fill);
        }

        // Ear clipping handles the few concave silhouettes (hair, roofs, stars).
        private void Polygon(Vector2[] points, Color fill)
        {
            if (points.Length < 3) return;
            int vertexStart = mesh.currentVertCount;
            float signedArea = 0;
            for (int i = 0; i < points.Length; i++)
            {
                Vertex(points[i], fill);
                Vector2 next = points[(i + 1) % points.Length];
                signedArea += points[i].x * next.y - next.x * points[i].y;
            }
            List<int> remaining = new List<int>(points.Length);
            for (int i = 0; i < points.Length; i++)
                remaining.Add(signedArea > 0 ? i : points.Length - 1 - i);

            int budget = points.Length * points.Length;
            while (remaining.Count > 2 && budget-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int before = remaining[(i + remaining.Count - 1) % remaining.Count];
                    int current = remaining[i];
                    int after = remaining[(i + 1) % remaining.Count];
                    Vector2 a = points[before];
                    Vector2 b = points[current];
                    Vector2 c = points[after];
                    if (Cross(b - a, c - b) <= 0.0001f) continue;
                    bool containsPoint = false;
                    for (int j = 0; j < remaining.Count; j++)
                    {
                        int index = remaining[j];
                        if (index == before || index == current || index == after) continue;
                        Vector2 p = points[index];
                        if (Cross(b - a, p - a) >= 0 && Cross(c - b, p - b) >= 0 && Cross(a - c, p - c) >= 0)
                        {
                            containsPoint = true;
                            break;
                        }
                    }
                    if (containsPoint) continue;
                    mesh.AddTriangle(vertexStart + before, vertexStart + current, vertexStart + after);
                    remaining.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }
    }
}
