using UnityEngine;

namespace IsekaiTruck.Editor
{
    internal static class HudColorPalette
    {
        public static readonly Color EntryBackground = new Color32(0xFC, 0xCE, 0x7E, 0xFF);
        public static readonly Color SidePanel = new Color32(0xA7, 0x8C, 0x9B, 0xFF);
        public static readonly Color Level = new Color32(0xFC, 0x7E, 0xC6, 0xFF);
        public static readonly Color LevelDepth = new Color32(0xA8, 0x50, 0x85, 0xFF);
        public static readonly Color LevelTrack = new Color32(0x70, 0x4C, 0x61, 0xFF);
        public static readonly Color LevelFill = new Color32(0xFF, 0xD2, 0xEB, 0xFF);
        public static readonly Color Upgrade = new Color32(0xFC, 0xCE, 0x7E, 0xFF);
        public static readonly Color UpgradeDepth = new Color32(0x9A, 0x70, 0x2F, 0xFF);
        public static readonly Color Soul = new Color32(0x64, 0x7B, 0x7D, 0xFF);
        public static readonly Color SoulDepth = new Color32(0x41, 0x53, 0x55, 0xFF);
        public static readonly Color Speed = new Color32(0x7E, 0xF1, 0xFC, 0xFF);
        public static readonly Color SpeedDepth = new Color32(0x37, 0xA6, 0xB0, 0xFF);

        public static bool Matches(Color actual, Color expected)
        {
            return Mathf.Approximately(actual.r, expected.r) &&
                Mathf.Approximately(actual.g, expected.g) &&
                Mathf.Approximately(actual.b, expected.b) &&
                Mathf.Approximately(actual.a, expected.a);
        }
    }
}
