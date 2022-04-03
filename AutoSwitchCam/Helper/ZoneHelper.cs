using AutoSwitchCam.Model;
using System.Collections.ObjectModel;
using System.Windows;

namespace AutoSwitchCam.Helper
{
    class ZoneHelper
    {
        public static Zone GetZoneToDisplay(ObservableCollection<Zone> zones, ObservableCollection<Head> heads)
        {
            int[] zoneHeadsCount = new int[zones.Count];

            for (int indexHead = 0; indexHead < heads.Count; indexHead++)
            {
                Head head = heads[indexHead];

                for (int indexZone = 0; indexZone < zones.Count; indexZone++)
                {
                    Zone zone = zones[indexZone];

                    if (PointInZone(zone, head.X, head.Z))
                    {
                        zoneHeadsCount[indexZone]++;
                    }
                }
            }

            int maxZoneHeadsCount = 0;
            int maxIndexZone = -1;

            for (int i = 0; i < zoneHeadsCount.Length; i++)
            {
                if (zoneHeadsCount[i] > maxZoneHeadsCount)
                {
                    maxIndexZone = i;
                }
            }

            return maxIndexZone >= 0 && maxIndexZone < zones.Count
                ? zones[maxIndexZone]
                : null;
        }

        public static Point[] ZoneToPointArray(Zone zone) => PointHelper.CoordonatesToPointArray(zone.X1, zone.Z1, zone.X2, zone.Z2, zone.X3, zone.Z3, zone.X4, zone.Z4);

        public static bool PointInZone(Zone zone, double? X, double? Y) => PointHelper.PointInPolygon(ZoneToPointArray(zone), X, Y);
    }
}
