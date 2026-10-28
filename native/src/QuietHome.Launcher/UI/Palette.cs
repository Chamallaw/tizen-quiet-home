using QuietHome.Core;
using Tizen.NUI;

namespace QuietHome.UI
{
    /// <summary>Thème courant converti en couleurs NUI, et fabrique de dégradés.</summary>
    public sealed class Palette
    {
        public readonly ThemeSpec Spec;
        public readonly Color Surface, Text, Muted, Accent, AccentText, Overlay, CardText, Button, Marker;

        public Palette(ThemeSpec spec)
        {
            Spec = spec;
            Surface = C(spec.Surface);
            Text = C(spec.Text);
            Muted = C(spec.Muted);
            Accent = C(spec.Accent);
            AccentText = C(spec.AccentText);
            Overlay = C(spec.Overlay);
            CardText = C(Themes.CardText);
            Button = C(spec.Button ?? spec.Surface);
            Marker = C(spec.Marker ?? spec.Text);
        }

        public string Name => Spec.Name;

        /// <summary>
        /// Rapport taille de texte / point : la TV déclare 72 ppp (1 pt = 1 px), alors que la maquette est pensée
        /// pour 96 ppp. Fixé au démarrage d'après Window.Dpi, pour des lettres aussi grandes, donc aussi nettes, que prévu.
        /// </summary>
        public static float TextScale = 1f;

        public static float Pt(float points) => (float)System.Math.Round(points * TextScale);
        public PropertyMap BackgroundVisual() => Gradient(Spec.BackgroundFrom, Spec.BackgroundTo, 0);

        public static Color C(string hex)
        {
            var c = Themes.ParseColor(hex);
            return new Color(c[0], c[1], c[2], c[3]);
        }

        static Vector4 V(string hex)
        {
            var c = Themes.ParseColor(hex);
            return new Vector4(c[0], c[1], c[2], c[3]);
        }

        /// <summary>Dégradé linéaire diagonal (haut-gauche → bas-droit) avec coins arrondis.</summary>
        public static PropertyMap Gradient(string from, string to, float cornerRadius)
        {
            var stops = new PropertyArray();
            stops.PushBack(new PropertyValue(V(from)));
            stops.PushBack(new PropertyValue(V(to)));
            var offsets = new PropertyArray();
            offsets.PushBack(new PropertyValue(0f));
            offsets.PushBack(new PropertyValue(1f));

            var map = new PropertyMap();
            map.Add(Visual.Property.Type, new PropertyValue((int)Visual.Type.Gradient));
            map.Add(GradientVisualProperty.StartPosition, new PropertyValue(new Vector2(-0.5f, -0.5f)));
            map.Add(GradientVisualProperty.EndPosition, new PropertyValue(new Vector2(0.5f, 0.5f)));
            map.Add(GradientVisualProperty.StopOffset, new PropertyValue(offsets));
            map.Add(GradientVisualProperty.StopColor, new PropertyValue(stops));
            if (cornerRadius > 0) map.Add(Visual.Property.CornerRadius, new PropertyValue(cornerRadius));
            return map;
        }

        /// <summary>
        /// Famille DM Sans embarquée (res/fonts, enregistrée au démarrage) pour une graisse donnée.
        /// Si l'enregistrement échoue, DALi retombe sur la police système.
        /// </summary>
        public static string Font(string weight)
        {
            switch (weight)
            {
                case "extralight": return "DM Sans ExtraLight";
                case "light": return "DM Sans Light";
                case "medium": return "DM Sans Medium";
                default: return "DM Sans";
            }
        }

        /// <summary>Graisse légère : l'essentiel de la hiérarchie passe par la taille et la couleur.</summary>
        public static PropertyMap Weight(string weight) => new PropertyMap().Add("weight", new PropertyValue(weight));
    }
}
