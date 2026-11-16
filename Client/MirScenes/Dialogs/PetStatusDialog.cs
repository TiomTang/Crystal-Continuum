using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Client.MirControls;
using Client.MirGraphics;
using Client.MirObjects;
using Shared;
using SlimDX;

namespace Client.MirScenes.Dialogs
{
    // Horizontal roster pinned above the bottom HUD: the hero first, then pets.
    // Hovering or clicking a card targets that actor exactly as if it had been
    // clicked on the map, so healing and support spells cast from the card land
    // on the hero or the pet.
    public sealed class PetStatusDialog : MirControl
    {
        public const int CardWidth = 72;
        public const int CardHeight = 86;
        public const int CardGap = 4;

        private const int IconBox = 38;
        private const int IconX = 17;
        private const int IconY = 14;
        private const int NameY = 52;
        private const int BarX = 12;
        private const int BarY = 72;
        private const int BarWidth = 48;
        private const int BarHeight = 5;
        private const int MaxCards = 6;   // hero + up to 5 pets

        private readonly List<MapObject> _actors = new List<MapObject>();
        private readonly PetCard[] _cards = new PetCard[MaxCards];
        private long _nextRefresh;

        private MapObject HoverActor { get; set; }

        public PetStatusDialog()
        {
            DrawControlTexture = false;
            Visible = false;

            for (int i = 0; i < MaxCards; i++)
            {
                PetCard card = new PetCard
                {
                    Parent = this,
                    Size = new Size(CardWidth, CardHeight),
                    Visible = false,
                };
                card.MouseDown += CardMouseDown;
                _cards[i] = card;
            }
        }

        public void Clear()
        {
            _actors.Clear();
            HoverActor = null;
            Visible = false;

            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].Actor = null;
                _cards[i].Visible = false;
            }
        }

        // Returns true when the cursor sits on a card. The caller must then skip the
        // map's pixel based mouse-object scan so the actor remains the mouse target.
        public bool Process()
        {
            if (!Settings.PetStatus || MapObject.User == null)
            {
                if (Visible || _actors.Count > 0) Clear();
                return false;
            }

            if (CMain.Time >= _nextRefresh)
            {
                _nextRefresh = CMain.Time + 200;
                Rebuild();
            }

            if (!Visible || _actors.Count == 0)
            {
                HoverActor = null;
                return false;
            }

            if (!DisplayRectangle.Contains(CMain.MPoint))
            {
                HoverActor = null;
                return false;
            }

            int step = CardWidth + CardGap;
            int offset = CMain.MPoint.X - DisplayRectangle.X;
            if (offset < 0) return false;

            int slot = offset / step;
            int within = offset - (slot * step);

            if (slot < 0 || slot >= _actors.Count || within > CardWidth)
            {
                HoverActor = null;
                return false;
            }

            HoverActor = _actors[slot];
            MapObject.MouseObjectID = HoverActor.ObjectID;
            return true;
        }

        // Called from the tooltip code so hovering a card keeps the actor info visible.
        public static bool IsPetStatusControl(MirControl control)
        {
            while (control != null)
            {
                if (control is PetStatusDialog) return true;
                control = control.Parent;
            }

            return false;
        }

        private void Rebuild()
        {
            _actors.Clear();

            // Hero first so it keeps a fixed slot next to the HUD edge.
            HeroObject hero = MapObject.HeroObject;
            if (hero != null && !hero.Dead)
                _actors.Add(hero);

            List<MonsterObject> pets = new List<MonsterObject>();
            foreach (KeyValuePair<uint, MapObject> pair in MapControl.Objects)
            {
                MonsterObject pet = pair.Value as MonsterObject;
                if (pet == null) continue;
                if (pet.MasterObjectId == 0 || pet.MasterObjectId != MapObject.User.ObjectID) continue;
                if (pet.Dead) continue;

                pets.Add(pet);
            }

            pets.Sort((a, b) => a.ObjectID.CompareTo(b.ObjectID));

            int room = MaxCards - _actors.Count;
            for (int i = 0; i < pets.Count && i < room; i++)
                _actors.Add(pets[i]);

            int count = _actors.Count;
            Visible = count > 0;

            for (int i = 0; i < MaxCards; i++)
            {
                PetCard card = _cards[i];
                card.Actor = i < count ? _actors[i] : null;
                card.Visible = i < count;

                if (card.Actor != null)
                    card.RefreshName();
            }

            if (count == 0)
            {
                HoverActor = null;
                return;
            }

            int width = (CardWidth * count) + (CardGap * (count - 1));
            int bottom = Settings.ScreenHeight - 152;

            if (GameScene.Scene != null && GameScene.Scene.MainDialog != null)
                bottom = GameScene.Scene.MainDialog.Location.Y;

            if (Size.Width != width)
                Size = new Size(width, CardHeight);

            Point location = new Point(8, bottom - CardHeight - 6);
            if (Location != location)
                Location = location;

            for (int i = 0; i < count; i++)
                _cards[i].Location = new Point(i * (CardWidth + CardGap), 0);
        }

        private void CardMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            PetCard card = sender as PetCard;
            if (card == null || card.Actor == null || card.Actor.Dead) return;

            MapObject.MouseObjectID = card.Actor.ObjectID;
            MapObject.TargetObjectID = card.Actor.ObjectID;

            if (card.Actor.AI != 6)
                MapObject.MagicObjectID = card.Actor.ObjectID;
        }

        private sealed class PetCard : MirControl
        {
            private static readonly Color HoverBorder = Color.FromArgb(255, 246, 214, 132);
            private static readonly Color PetNameColour = Color.FromArgb(255, 244, 232, 206);
            private static readonly Color HeroNameColour = Color.FromArgb(255, 250, 214, 120);

            private readonly MirLabel _name;

            public MapObject Actor;

            public PetCard()
            {
                DrawControlTexture = true;
                BorderColour = HoverBorder;

                // Leave room for CJK glyphs and their outline without moving the
                // name's centre or reducing its font size.
                _name = new MirLabel
                {
                    AutoSize = false,
                    BackColour = Color.Transparent,
                    DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine,
                    Font = new Font(Settings.FontName, 9F),
                    ForeColour = PetNameColour,
                    Location = new Point(0, NameY),
                    OutLine = true,
                    OutLineColour = Color.Black,
                    Parent = this,
                    Size = new Size(CardWidth, 20),
                    Text = string.Empty,
                };
            }

            public void RefreshName()
            {
                if (Actor == null)
                {
                    _name.Text = string.Empty;
                    return;
                }

                string name = Actor.Name ?? string.Empty;
                int bracket = name.IndexOf('(');
                if (bracket > 0) name = name.Substring(0, bracket);

                _name.Text = name;
                _name.ForeColour = Actor is PlayerObject ? HeroNameColour : PetNameColour;
            }

            protected override void CreateTexture()
            {
                // Cache the procedural plate just like a label. The control lifecycle
                // releases it on device loss and recreates it on the next draw.
                DXManager.Sprite.Flush();
                var replacement = TextureUpload.Paint(Size.Width, Size.Height, DrawPlate);
                DisposeTexture();
                ControlTexture = replacement;
                TextureSize = Size;
                DXManager.ControlList.Add(this);
                TextureValid = true;
            }

            private static void DrawPlate(Bitmap image)
            {
                using Graphics target = Graphics.FromImage(image);
                const int scale = 4;
                using var bitmap = new Bitmap(CardWidth * scale, CardHeight * scale);
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    void Rectangle(int left, int top, int right, int bottom, string colour, string outline = null)
                    {
                        int x = left * scale, y = top * scale;
                        int width = (right - left + 1) * scale, height = (bottom - top + 1) * scale;
                        using var brush = new SolidBrush(ColorTranslator.FromHtml(colour));
                        graphics.FillRectangle(brush, x, y, width, height);
                        if (outline == null) return;
                        using var edge = new SolidBrush(ColorTranslator.FromHtml(outline));
                        graphics.FillRectangle(edge, x, y, width, scale);
                        graphics.FillRectangle(edge, x, y + height - scale, width, scale);
                        graphics.FillRectangle(edge, x, y, scale, height);
                        graphics.FillRectangle(edge, x + width - scale, y, scale, height);
                    }

                    void Line(string colour, params Point[] points)
                    {
                        using var pen = new Pen(ColorTranslator.FromHtml(colour), scale);
                        pen.StartCap = pen.EndCap = System.Drawing.Drawing2D.LineCap.Square;
                        PointF[] scaled = points.Select(point => new PointF((point.X + 0.5F) * scale, (point.Y + 0.5F) * scale)).ToArray();
                        graphics.DrawLines(pen, scaled);
                    }

                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        float amount = (float)y / (bitmap.Height - 1);
                        using var brush = new SolidBrush(Color.FromArgb(
                            (int)(61 - 36 * amount), (int)(46 - 25 * amount), (int)(30 - 14 * amount)));
                        graphics.FillRectangle(brush, 0, y, bitmap.Width, 1);
                    }

                    Rectangle(1, 0, 70, 85, "#17130d");
                    Rectangle(0, 1, 71, 84, "#17130d");
                    Rectangle(2, 2, 69, 83, "#6b512a", "#b89a56");
                    Rectangle(4, 4, 67, 81, "#2d2419", "#302416");
                    Rectangle(6, 6, 65, 79, "#34291c", "#6c542f");
                    Line("#d1b16e", new Point(6, 5), new Point(65, 5));
                    foreach (var corner in new[] { (X: 2, Direction: 1), (X: 69, Direction: -1) })
                    {
                        foreach (var edge in new[] { (Y: 2, Direction: 1), (Y: 83, Direction: -1) })
                        {
                            Line("#d4b775", new Point(corner.X, edge.Y + 9 * edge.Direction),
                                new Point(corner.X, edge.Y), new Point(corner.X + 8 * corner.Direction, edge.Y));
                            Line("#9e7b3e", new Point(corner.X + 3 * corner.Direction, edge.Y + 7 * edge.Direction),
                                new Point(corner.X + 3 * corner.Direction, edge.Y + 3 * edge.Direction),
                                new Point(corner.X + 7 * corner.Direction, edge.Y + 3 * edge.Direction));
                        }
                    }

                    Rectangle(13, 11, 58, 54, "#151713", "#a1884e");
                    Rectangle(14, 12, 57, 53, "#20221b", "#100f0b");
                    Line("#403e2b", new Point(15, 53), new Point(57, 53), new Point(57, 13));
                    Rectangle(BarX - 2, BarY - 1, BarX + BarWidth + 1, BarY + BarHeight + 1, "#090909", "#8c7446");
                    Rectangle(BarX - 1, BarY - 1, BarX + BarWidth, BarY + BarHeight, "#100b09");
                    Rectangle(BarX, BarY, BarX + BarWidth - 1, BarY + BarHeight - 1, "#2a1010");
                }

                target.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                target.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                target.DrawImage(bitmap, new Rectangle(0, 0, CardWidth, CardHeight));
            }

            public override void Draw()
            {
                if (!Visible) return;

                bool hovered = Actor != null && MapObject.MouseObject == Actor;
                if (Border != hovered)
                    Border = hovered;

                base.Draw();
            }

            protected internal override void DrawControl()
            {
                // The cached plate must be drawn before the live portrait and HP.
                // MirControl.Draw then adds the name and hover border on top.
                base.DrawControl();
                if (Actor == null) return;
                DrawIcon();
                DrawHealthBar();
            }

            private void DrawIcon()
            {
                MLibrary library = Actor.BodyLibrary;
                if (library == null) return;

                FrameSet frames = library.Frames ?? FrameSet.DefaultMonster;

                Frame standing;
                if (!frames.TryGetValue(MirAction.Standing, out standing))
                    standing = new Frame(0, 4, 0, 500);

                int index = standing.Start + (standing.OffSet * (byte)MirDirection.Down);

                // Player armour atlases keep the female frames at an offset, so a hero
                // portrait has to shift by it or it shows the wrong gender's poses.
                PlayerObject player = Actor as PlayerObject;
                if (player != null) index += player.ArmourOffSet;

                Size source = library.GetSize(index);
                if (source.Width <= 0 || source.Height <= 0) return;

                float scale = Math.Min((float)IconBox / source.Width, (float)IconBox / source.Height);
                Size size = new Size(Math.Max(1, (int)(source.Width * scale)), Math.Max(1, (int)(source.Height * scale)));

                Point point = new Point(
                    DisplayRectangle.X + IconX + ((IconBox - size.Width) / 2),
                    DisplayRectangle.Y + IconY + ((IconBox - size.Height) / 2));

                library.Draw(index, point, size, Color.White);
            }

            private void DrawHealthBar()
            {
                int fill = BarWidth * Math.Clamp((int)Actor.PercentHealth, 0, 100) / 100;
                if (fill <= 0) return;

                int x = DisplayRectangle.X + BarX;
                int y = DisplayRectangle.Y + BarY;

                for (int row = 0; row < BarHeight; row++)
                {
                    float amount = (float)row / (BarHeight - 1);
                    Color colour = Color.FromArgb((int)(183 - 77 * amount), (int)(62 - 36 * amount), (int)(43 - 20 * amount));
                    DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, fill, 1),
                        new Vector3(x, y + row, 0), colour);
                }
                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, fill, 1),
                    new Vector3(x, y, 0), Color.FromArgb(255, 225, 120, 87));
            }
        }
    }
}
