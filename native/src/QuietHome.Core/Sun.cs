using System;

namespace QuietHome.Core
{
    /// <summary>
    /// Hauteur du soleil, pour le thème automatique : formules approchées de l'US Naval Observatory,
    /// à la minute près pour le lever et le coucher. Calcul local, sans réseau.
    /// </summary>
    public static class Sun
    {
        /// <summary>Hauteur du centre du soleil au lever et au coucher, réfraction comprise (degrés).</summary>
        public const double Horizon = -0.833;

        static readonly DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        /// <summary>Hauteur du soleil en degrés (négative la nuit), à l'instant utc, au lieu donné.</summary>
        public static double Elevation(DateTime utc, double latitude, double longitude)
        {
            double d = (utc - J2000).TotalDays;
            double g = Rad(Mod(357.529 + 0.98560028 * d));                       // anomalie moyenne
            double q = Mod(280.459 + 0.98564736 * d);                             // longitude moyenne
            double l = Rad(q + 1.915 * Math.Sin(g) + 0.020 * Math.Sin(2 * g));    // longitude écliptique
            double e = Rad(23.439 - 0.00000036 * d);                              // obliquité
            double ra = Math.Atan2(Math.Cos(e) * Math.Sin(l), Math.Cos(l));
            double dec = Math.Asin(Math.Sin(e) * Math.Sin(l));
            double sidereal = Mod(15 * (18.697374558 + 24.06570982441908 * d));   // temps sidéral de Greenwich
            double hourAngle = Rad(sidereal + longitude) - ra;
            double lat = Rad(latitude);
            return Deg(Math.Asin(Math.Sin(lat) * Math.Sin(dec) + Math.Cos(lat) * Math.Cos(dec) * Math.Cos(hourAngle)));
        }

        static double Mod(double deg)
        {
            deg %= 360;
            return deg < 0 ? deg + 360 : deg;
        }

        static double Rad(double deg) => deg * Math.PI / 180;
        static double Deg(double rad) => rad * 180 / Math.PI;
    }
}
