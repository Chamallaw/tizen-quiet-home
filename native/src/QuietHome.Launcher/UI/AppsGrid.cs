using System;
using System.Collections.Generic;
using QuietHome.Core;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace QuietHome.UI
{
    /// <summary>
    /// Écran « Toutes les apps » : grille de cartes (logos officiels quand ils existent),
    /// huit par ligne comme l'accueil, défilement vertical. Les touches sont transmises par HomeScreen.
    /// </summary>
    public sealed class AppsGrid : View
    {
        const int Cols = 8;
        const float MarginX = 80, Top = 210, Gap = 20;
        static readonly float RowH = TileStyle.Card.Footprint + 34;

        readonly FocusModel focus = new FocusModel();
        readonly List<List<TileView>> views = new List<List<TileView>>();
        readonly View layer, viewport;
        readonly Palette palette;

        public AppsGrid(List<Tile> tiles, Palette palette, Size screen)
        {
            this.palette = palette;
            Size = screen;
            var c = Themes.ParseColor(palette.Spec.BackgroundTo);
            BackgroundColor = new Color(c[0], c[1], c[2], 0.94f);

            Add(new TextLabel
            {
                Text = "Toutes les apps",
                Position = new Position(MarginX, 80),
                Size = new Size(1200, 90),
                PointSize = 40,
                TextColor = palette.Text,
                FontFamily = Palette.Font("extralight"),
            });
            Add(new TextLabel
            {
                Text = tiles.Count + " applications · maintenir OK pour les favoris",
                Position = new Position(MarginX, 160),
                Size = new Size(1200, 40),
                PointSize = 14,
                TextColor = palette.Muted,
                FontFamily = Palette.Font("light"),
            });

            viewport = new View { Position = new Position(0, Top), Size = new Size(screen.Width, screen.Height - Top),
                ClippingMode = ClippingModeType.ClipChildren };
            Add(viewport);
            layer = new View { Size = new Size(screen.Width, 20000) };
            viewport.Add(layer);

            var rows = new List<Row>();
            float stride = TileStyle.Card.W + Gap;
            for (int i = 0; i < tiles.Count; i += Cols)
            {
                var row = new Row { Id = "g" + i / Cols };
                var line = new List<TileView>();
                for (int j = i; j < Math.Min(tiles.Count, i + Cols); j++)
                {
                    row.Tiles.Add(tiles[j]);
                    var tv = new TileView(tiles[j], TileStyle.Card, palette)
                        { Position = new Position(MarginX + (j - i) * stride, 24 + (i / Cols) * RowH) };
                    layer.Add(tv);
                    line.Add(tv);
                }
                rows.Add(row);
                views.Add(line);
            }
            focus.SetRows(rows);
            Apply(false);
        }

        public Tile Current => focus.Current;

        /// <summary>Déplace la sélection ; false si elle est déjà au bord.</summary>
        public bool Move(Direction dir)
        {
            if (!focus.Move(dir)) return false;
            Apply(true);
            return true;
        }

        void Apply(bool animate)
        {
            for (int r = 0; r < views.Count; r++)
                for (int c = 0; c < views[r].Count; c++)
                    views[r][c].SetFocused(r == focus.RowIndex && c == focus.Column(r), false, animate);
            // Garde deux lignes de contexte au-dessus de la ligne active.
            float y = -Math.Max(0, focus.RowIndex - 1) * RowH;
            if (!animate) { layer.PositionY = y; return; }
            var anim = new Animation(TileView.FocusMs) { DefaultAlphaFunction = TileView.Ease() };
            anim.AnimateTo(layer, "PositionY", y);
            anim.Play();
        }

        public void Close()
        {
            Unparent();
            Dispose();
        }
    }
}
