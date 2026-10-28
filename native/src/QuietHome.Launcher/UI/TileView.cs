using System.IO;
using QuietHome.Core;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace QuietHome.UI
{
    /// <summary>Gabarit d'une rangée : carte en dégradé ou bouton rond.</summary>
    public sealed class TileStyle
    {
        public float W, H;
        public bool Round;

        public static readonly TileStyle Card = new TileStyle { W = 202, H = 130 };
        public static readonly TileStyle Console = new TileStyle { W = 202, H = 104 };
        public static readonly TileStyle Button = new TileStyle { W = 96, H = 96, Round = true };

        /// <summary>Hauteur occupée, trait de focus compris.</summary>
        public float Footprint => Round ? H : H + 26;
    }

    /// <summary>
    /// Carte : dégradé aux couleurs de la marque, logo en haut à gauche, nom en bas à gauche,
    /// trait discret sous la carte au focus. Bouton rond : pictogramme teinté, inversé au focus.
    /// </summary>
    public sealed class TileView : View
    {
        /// <summary>Dossier res/ de l'app, pour les icônes embarquées (préfixe « res: »).</summary>
        public static string ResourceDir = "";

        public readonly Tile Tile;
        readonly TileStyle style;
        readonly Palette palette;
        readonly View marker, card;
        readonly ImageView glyph;
        // État affiché, pour n'animer que les tuiles qui changent ; animation en cours, interrompue si besoin.
        bool shownFocused, shownMoving, initialized;
        Animation anim;

        /// <summary>Durée et courbe communes à toutes les transitions de focus.</summary>
        public const int FocusMs = 220;
        public static AlphaFunction Ease() => new AlphaFunction(AlphaFunction.BuiltinFunctions.EaseOutSine);
        const float Lift = 8, GlyphSize = 40;

        public TileView(Tile tile, TileStyle style, Palette palette)
        {
            Tile = tile;
            this.style = style;
            this.palette = palette;
            var icon = Resolve(tile.IconPath);

            if (style.Round)
            {
                // Bouton discret : pictogramme au trait, sans pastille, en retrait ; au focus il s'éclaircit
                // et le même trait que sous les tuiles apparaît dessous.
                Size = new Size(style.W, style.H);
                glyph = new ImageView
                {
                    ResourceUrl = icon ?? "",
                    Size = new Size(GlyphSize, GlyphSize),
                    Position = new Position((float)System.Math.Round((style.W - GlyphSize) / 2), (float)System.Math.Round((style.H - GlyphSize) / 2)),
                    DesiredWidth = (int)GlyphSize, DesiredHeight = (int)GlyphSize,
                    Color = palette.Muted,   // pictogramme blanc multiplié par la couleur du thème
                };
                Add(glyph);
                marker = new View
                {
                    Size = new Size(28, 3),
                    Position = new Position((style.W - 28) / 2, style.H - 8),
                    CornerRadius = 2,
                    BackgroundColor = palette.Marker,
                    Opacity = 0,
                };
                Add(marker);
                return;
            }

            Size = new Size(style.W, style.Footprint);
            var tone = Themes.ToneFor(tile.Label);
            bool soft = palette.Spec.SoftenTiles;
            card = new View
            {
                Size = new Size(style.W, style.H),
                Background = soft ? Palette.Gradient(Themes.Soften(tone.From), Themes.Soften(tone.To), 20)
                                  : Palette.Gradient(tone.From, tone.To, 22),
                CornerRadius = soft ? 20 : 22,
                Opacity = tile.Missing ? 0.35f : 1f,
            };
            if (soft)
            {
                // Objet posé sur le bois : ombre portée douce et liseré clair.
                card.BoxShadow = new Shadow(36, new Color(0, 0, 0, 0.5f), new Vector2(0, 16));
                card.BorderlineWidth = 2f;   // trait entier : net à 1080p
                card.BorderlineColor = new Color(1f, 0.94f, 0.86f, 0.10f);
                card.Add(new View
                {
                    Size = new Size(style.W, style.H),
                    Background = Palette.Gradient("#ffebd214", "#ffebd200", 20),
                });
            }
            Add(card);

            // Marque connue : logo officiel centré, à sa couleur, sans libellé (le logo suffit).
            var brand = Themes.BrandFor(tile.Label);
            bool branded = brand != null && icon != null;
            if (branded)
            {
                bool input = tile.Kind == TileKind.Input;
                // Tuile d'appareil : variante du logo dessinée à sa taille (render_marks.py), si elle existe.
                var hdmi = input ? Resolve(System.IO.Path.ChangeExtension(tile.IconPath, null) + "-hdmi.png") : null;
                if (hdmi != null) icon = hdmi;
                var box = AspectFit(icon, style.W * (input ? 0.50f : 0.56f), style.H * (input ? 0.38f : 0.46f));
                float centerY = style.H * (input ? 0.57f : 0.5f);   // entrée : « HDMI n » occupe le haut
                card.Add(new ImageView
                {
                    ResourceUrl = icon,
                    Size = box,
                    Position = new Position((float)System.Math.Round((style.W - box.Width) / 2), (float)System.Math.Round(centerY - box.Height / 2)),
                    // Décodé à sa taille d'affichage : logo net, sans réduction par la carte graphique.
                    DesiredWidth = (int)box.Width, DesiredHeight = (int)box.Height,
                    Color = Palette.C(!palette.Spec.SoftenTiles ? brand.MarkColor
                        : brand.MarkColor == "#ffffff" ? "#f3ece2" : Themes.Soften(brand.MarkColor, 0.9, 0.95)),
                });
            }
            else if (icon != null)
            {
                card.Add(new ImageView
                {
                    ResourceUrl = icon,
                    Size = new Size(36, 36),
                    Position = new Position(18, 16),
                    DesiredWidth = 36, DesiredHeight = 36,
                    FittingMode = FittingModeType.ShrinkToFit,
                });
            }

            if (tile.Kind == TileKind.Input)
            {
                // « HDMI n » toujours en haut à droite, avec ou sans logo.
                card.Add(new TextLabel
                { FontFamily = Palette.Font("regular"),
                    Text = Subtitle(tile),
                    Position = new Position(style.W - 116, 14),
                    Size = new Size(100, 26),
                    PointSize = Palette.Pt(12),
                    TextColor = palette.CardText,
                    HorizontalAlignment = HorizontalAlignment.End,
                    Opacity = 0.7f,
                });
            }

            if (!branded) card.Add(new TextLabel
            {
                Text = tile.Label,
                Position = new Position(18, style.H - 46),
                Size = new Size(style.W - 36, 34),
                PointSize = Palette.Pt(16),
                TextColor = palette.CardText,
                VerticalAlignment = VerticalAlignment.Bottom,
                MultiLine = false,
                Ellipsis = true,
                FontFamily = Palette.Font("regular"),
            });

            marker = new View
            {
                Size = new Size(56, 3),
                Position = new Position((style.W - 56) / 2, style.H + 18),
                CornerRadius = 2,
                BackgroundColor = palette.Marker,
                Opacity = 0,
            };
            Add(marker);
        }

        /// <summary>
        /// Applique l'état de focus. Avec animate, la tuile grandit et se soulève (ou se repose) en douceur
        /// et le trait apparaît en fondu ; sans, l'état est posé directement (construction, changement de thème).
        /// </summary>
        public void SetFocused(bool focused, bool moving, bool animate = false)
        {
            if (initialized && focused == shownFocused && moving == shownMoving) return;
            animate &= initialized;
            initialized = true;
            shownFocused = focused;
            shownMoving = moving;

            anim?.Stop();
            anim = animate ? new Animation(FocusMs) { DefaultAlphaFunction = Ease() } : null;

            if (style.Round)
            {
                glyph.Color = focused ? palette.Text : palette.Muted;
                To(glyph, "Scale", focused ? new Vector3(1.12f, 1.12f, 1f) : Vector3.One);
                To(marker, "Opacity", focused ? 1f : 0f);
                anim?.Play();
                return;
            }

            To(this, "Scale", focused ? new Vector3(1.06f, 1.06f, 1f) : Vector3.One);
            To(card, "PositionY", focused ? -Lift : 0f);
            To(marker, "Opacity", focused ? 1f : 0f);
            // En déplacement, le trait s'élargit pour signaler le mode.
            float markerW = focused && moving ? 120 : 56;
            To(marker, "SizeWidth", markerW);
            To(marker, "PositionX", (style.W - markerW) / 2);
            if (palette.Spec.SoftenTiles)
                card.BorderlineColor = focused ? palette.Marker : new Color(1f, 0.94f, 0.86f, 0.10f);
            anim?.Play();
        }

        /// <summary>Anime la propriété si une animation est en cours de préparation, sinon la pose.</summary>
        void To(View view, string property, object value)
        {
            if (anim != null)
            {
                anim.AnimateTo(view, property, value);
                return;
            }
            switch (property)
            {
                case "Scale": view.Scale = (Vector3)value; break;
                case "PositionY": view.PositionY = (float)value; break;
                case "PositionX": view.PositionX = (float)value; break;
                case "Opacity": view.Opacity = (float)value; break;
                case "SizeWidth": view.SizeWidth = (float)value; break;
            }
        }

        /// <summary>« HDMI n » depuis l'identifiant connect_source (13 = HDMI 1).</summary>
        static string Subtitle(Tile tile) =>
            int.TryParse(tile.Action, out var id) && id >= 13 && id <= 16 ? "HDMI " + (id - 12) : "";

        /// <summary>Taille ajustée à la boîte en gardant les proportions du PNG (lues dans son en-tête).</summary>
        static Size AspectFit(string pngPath, float maxW, float maxH)
        {
            try
            {
                var head = new byte[24];
                using (var f = File.OpenRead(pngPath)) f.Read(head, 0, 24);
                float w = (head[16] << 24) | (head[17] << 16) | (head[18] << 8) | head[19];
                float h = (head[20] << 24) | (head[21] << 16) | (head[22] << 8) | head[23];
                if (w > 0 && h > 0)
                {
                    // Logo déjà à la bonne taille (au pixel près) : affiché tel quel, sans aucune mise à l'échelle.
                    if (w <= maxW + 1 && h <= maxH + 1 && (w >= maxW - 1 || h >= maxH - 1)) return new Size(w, h);
                    float k = System.Math.Min(maxW / w, maxH / h);
                    return new Size((float)System.Math.Round(w * k), (float)System.Math.Round(h * k));
                }
            }
            catch (System.Exception) { /* taille par défaut */ }
            return new Size(maxH, maxH);
        }

        /// <summary>Chemin utilisable de l'icône, ou null : les icônes d'autres apps peuvent être hors de portée (sandbox).</summary>
        public static string Resolve(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (path.StartsWith(Catalog.ResPrefix))
                path = System.IO.Path.Combine(ResourceDir, path.Substring(Catalog.ResPrefix.Length));
            try { return File.Exists(path) ? path : null; }
            catch (System.Exception) { return null; }
        }
    }
}
