using System;
using System.Collections.Generic;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace QuietHome.UI
{
    /// <summary>
    /// Panneau modal générique : liste navigable (haut/bas, OK, Retour), avec des titres de section
    /// que le curseur saute, une valeur à droite des réglages et ◀ ▶ pour les régler sur place.
    /// Une entrée sans action s'affiche comme du texte (diagnostic, mode d'emploi).
    /// </summary>
    public sealed class OverlayMenu : View
    {
        public sealed class Item
        {
            public string Label;
            public Action Run;
            /// <summary>Reste ouvert après OK (volume, réglages basculés sur place).</summary>
            public bool KeepOpen;
            /// <summary>État affiché à droite (« Activé », « Automatique »…), mis à jour par Run ou Adjust.</summary>
            public string Value;
            /// <summary>◀ (-1) ou ▶ (+1) sur la ligne : règle sans quitter le panneau.</summary>
            public Action<int> Adjust;
            /// <summary>Titre de section : discret, jamais sélectionné.</summary>
            public bool Header;

            public Item(string label, Action run = null, bool keepOpen = false) { Label = label; Run = run; KeepOpen = keepOpen; }

            public static Item Section(string title) => new Item(title) { Header = true };
        }

        const float PanelW = 1040, LineH = 64, PadX = 60, ListTop = 150;
        const int VisibleLines = 12;
        readonly List<Item> items;
        readonly List<TextLabel> lines = new List<TextLabel>(), values = new List<TextLabel>();
        readonly Palette palette;
        readonly View highlight;
        readonly TextLabel footer;
        int selected, scroll;

        public event Action Closed;
        public int Selected => selected;

        public OverlayMenu(string title, List<Item> items, Palette palette, Size screen, int initialSelection = -1)
        {
            this.items = items;
            this.palette = palette;
            Size = screen;
            BackgroundColor = palette.Overlay;

            int shown = Math.Min(VisibleLines, Math.Max(1, items.Count));
            float panelH = ListTop + shown * LineH + 90;
            var panel = new View
            {
                Size = new Size(PanelW, panelH),
                Position = new Position((screen.Width - PanelW) / 2, (screen.Height - panelH) / 2),
                BackgroundColor = palette.Surface,
                CornerRadius = 28,
            };
            panel.Add(new TextLabel
            {
                Text = title,
                Position = new Position(PadX, 44),
                Size = new Size(PanelW - 2 * PadX, 70),
                PointSize = 28,
                TextColor = palette.Text,
                FontFamily = Palette.Font("light"),
            });

            highlight = new View
            {
                Size = new Size(PanelW - 2 * PadX + 32, LineH - 8),
                CornerRadius = (LineH - 8) / 2,
                BackgroundColor = palette.Accent,
                PositionX = PadX - 16,
            };
            panel.Add(highlight);

            for (int i = 0; i < shown; i++)
            {
                var line = new TextLabel
                {
                    FontFamily = Palette.Font("regular"),
                    Position = new Position(PadX + 8, ListTop + i * LineH),
                    Size = new Size(PanelW - 2 * PadX - 16, LineH - 8),
                    PointSize = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    Ellipsis = true,
                };
                var value = new TextLabel
                {
                    FontFamily = Palette.Font("light"),
                    Position = new Position(PanelW / 2, ListTop + i * LineH),
                    Size = new Size(PanelW / 2 - PadX - 8, LineH - 8),
                    PointSize = 17,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.End,
                    Ellipsis = true,
                };
                lines.Add(line);
                values.Add(value);
                panel.Add(line);
                panel.Add(value);
            }
            footer = new TextLabel
            {
                FontFamily = Palette.Font("regular"),
                Position = new Position(PadX, panelH - 70),
                Size = new Size(PanelW - 2 * PadX, 40),
                PointSize = 13,
                TextColor = palette.Muted,
            };
            panel.Add(footer);
            Add(panel);
            selected = initialSelection >= 0 && initialSelection < items.Count && !items[initialSelection].Header
                ? initialSelection : FirstActionable();
            Refresh();
        }

        int FirstActionable()
        {
            for (int i = 0; i < items.Count; i++) if (items[i].Run != null || items[i].Adjust != null) return i;
            for (int i = 0; i < items.Count; i++) if (!items[i].Header) return i;
            return 0;
        }

        static bool Selectable(Item item) => item.Run != null || item.Adjust != null;

        void Refresh()
        {
            // Garde le titre de section visible au-dessus de la ligne sélectionnée.
            int top = selected > 0 && items[selected - 1].Header ? selected - 1 : selected;
            if (top < scroll) scroll = top;
            if (selected >= scroll + lines.Count) scroll = selected - lines.Count + 1;
            var current = items.Count > 0 ? items[selected] : null;
            highlight.Opacity = current != null && Selectable(current) ? 1f : 0f;
            highlight.PositionY = ListTop + (selected - scroll) * LineH;
            for (int i = 0; i < lines.Count; i++)
            {
                int idx = scroll + i;
                var line = lines[i];
                var value = values[i];
                if (idx >= items.Count) { line.Text = ""; value.Text = ""; continue; }
                var item = items[idx];
                bool on = idx == selected && Selectable(item);
                if (item.Header)
                {
                    // Section : petites capitales espacées, posées au bas de la ligne.
                    line.Text = string.Join(" ", item.Label.ToUpperInvariant().ToCharArray());
                    line.PointSize = 12;
                    line.VerticalAlignment = VerticalAlignment.Bottom;
                    line.TextColor = palette.Muted;
                    value.Text = "";
                    continue;
                }
                line.Text = item.Label;
                line.PointSize = 18;
                line.VerticalAlignment = VerticalAlignment.Center;
                line.TextColor = !Selectable(item) ? palette.Muted : on ? palette.AccentText : palette.Text;
                value.Text = item.Value == null ? "" : on && item.Adjust != null ? "‹  " + item.Value + "  ›" : item.Value;
                value.TextColor = on ? palette.AccentText : palette.Muted;
            }
            footer.Text = current?.Adjust != null ? "◀ ▶ pour régler · Retour pour fermer"
                : current?.Run != null ? "OK pour choisir · Retour pour fermer" : "Retour pour fermer";
        }

        int Step(int from, int dir)
        {
            for (int i = from + dir; i >= 0 && i < items.Count; i += dir)
                if (!items[i].Header) return i;
            return from;
        }

        /// <summary>Traite une touche ; le panneau est modal, il consomme tout. Renvoie true si la sélection ou un réglage a bougé.</summary>
        public bool HandleKey(string key)
        {
            int before = selected;
            var item = items.Count > 0 ? items[selected] : null;
            switch (key)
            {
                case "Up":
                    selected = Step(selected, -1);
                    break;
                case "Down":
                    selected = Step(selected, +1);
                    break;
                case "Left":
                case "Right":
                    if (item?.Adjust == null) break;
                    item.Adjust(key == "Left" ? -1 : +1);
                    Refresh();
                    return true;
                case "Return":
                case "KP_Enter":
                    if (item == null) break;
                    if (item.Run == null) { if (item.Adjust != null) { item.Adjust(+1); Refresh(); return true; } break; }
                    if (item.KeepOpen) { item.Run(); break; }
                    Close();
                    item.Run();
                    return false;
                case "XF86Back":
                case "Back":
                case "Escape":
                case "BackSpace":
                    Close();
                    return false;
            }
            Refresh();
            return selected != before;
        }

        public void Close()
        {
            Unparent();
            Closed?.Invoke();
            Dispose();
        }
    }
}
