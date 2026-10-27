using System;
using System.Collections.Generic;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;

namespace QuietHome.UI
{
    /// <summary>
    /// Panneau modal générique : liste navigable (haut/bas, OK, Retour).
    /// Une entrée sans action s'affiche comme du texte (diagnostic, mode d'emploi).
    /// </summary>
    public sealed class OverlayMenu : View
    {
        public sealed class Item
        {
            public string Label;
            public Action Run;
            /// <summary>Reste ouvert après OK (volume).</summary>
            public bool KeepOpen;
            public Item(string label, Action run = null, bool keepOpen = false) { Label = label; Run = run; KeepOpen = keepOpen; }
        }

        const float PanelW = 1040, LineH = 68, PadX = 60;
        const int VisibleLines = 10;
        readonly List<Item> items;
        readonly List<TextLabel> lines = new List<TextLabel>();
        readonly Palette palette;
        readonly View highlight;
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
            float panelH = 160 + shown * LineH + 48;
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
                Position = new Position(PadX, 48),
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
                { FontFamily = Palette.Font("regular"),
                    Position = new Position(PadX + 8, 160 + i * LineH),
                    Size = new Size(PanelW - 2 * PadX - 16, LineH - 8),
                    PointSize = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    Ellipsis = true,
                };
                lines.Add(line);
                panel.Add(line);
            }
            Add(panel);
            selected = initialSelection >= 0 && initialSelection < items.Count ? initialSelection : FirstActionable();
            Refresh();
        }

        int FirstActionable()
        {
            for (int i = 0; i < items.Count; i++) if (items[i].Run != null) return i;
            return 0;
        }

        void Refresh()
        {
            if (selected < scroll) scroll = selected;
            if (selected >= scroll + lines.Count) scroll = selected - lines.Count + 1;
            bool hasAction = items.Count > 0 && items[selected].Run != null;
            highlight.Opacity = hasAction ? 1f : 0f;
            highlight.PositionY = 160 + (selected - scroll) * LineH;
            for (int i = 0; i < lines.Count; i++)
            {
                int idx = scroll + i;
                var line = lines[i];
                if (idx >= items.Count) { line.Text = ""; continue; }
                var item = items[idx];
                line.Text = item.Label;
                line.TextColor = item.Run == null ? palette.Muted
                    : idx == selected ? palette.AccentText : palette.Text;
            }
        }

        /// <summary>Traite une touche ; le panneau est modal, il consomme tout.</summary>
        public void HandleKey(string key)
        {
            switch (key)
            {
                case "Up":
                    if (selected > 0) selected--;
                    break;
                case "Down":
                    if (selected < items.Count - 1) selected++;
                    break;
                case "Return":
                case "KP_Enter":
                    var item = items.Count > 0 ? items[selected] : null;
                    if (item?.Run == null) break;
                    if (item.KeepOpen) { item.Run(); break; }
                    Close();
                    item.Run();
                    return;
                case "XF86Back":
                case "Back":
                case "Escape":
                case "BackSpace":
                    Close();
                    return;
            }
            Refresh();
        }

        public void Close()
        {
            Unparent();
            Closed?.Invoke();
            Dispose();
        }
    }
}
