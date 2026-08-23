using UnityEngine;

namespace IsekaiTruck.Editor
{
    internal static class HudColorPalette
    {
        public static readonly Color EntryBackground = new Color32(0xFC, 0xCE, 0x7E, 0xFF);
        public static readonly Color SidePanel = new Color32(0xAA, 0x96, 0xA1, 0xFF);
        public static readonly Color Level = new Color32(0xE9, 0x82, 0xB8, 0xFF);
        public static readonly Color LevelDepth = new Color32(0x9E, 0x63, 0x82, 0xFF);
        public static readonly Color LevelTrack = new Color32(0x75, 0x59, 0x68, 0xFF);
        public static readonly Color LevelFill = new Color32(0xF5, 0xC5, 0xDC, 0xFF);
        public static readonly Color Upgrade = new Color32(0xED, 0xC6, 0x7F, 0xFF);
        public static readonly Color UpgradeDepth = new Color32(0x98, 0x79, 0x48, 0xFF);
        public static readonly Color Soul = new Color32(0x70, 0x85, 0x87, 0xFF);
        public static readonly Color SoulDepth = new Color32(0x50, 0x62, 0x64, 0xFF);
        public static readonly Color Speed = new Color32(0x87, 0xDF, 0xE6, 0xFF);
        public static readonly Color SpeedDepth = new Color32(0x4E, 0xA7, 0xAE, 0xFF);

        public static bool Matches(Color actual, Color expected)
        {
            return Mathf.Approximately(actual.r, expected.r) &&
                Mathf.Approximately(actual.g, expected.g) &&
                Mathf.Approximately(actual.b, expected.b) &&
                Mathf.Approximately(actual.a, expected.a);
        }
    }
}
