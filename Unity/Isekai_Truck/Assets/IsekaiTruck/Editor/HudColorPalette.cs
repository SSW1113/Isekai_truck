using UnityEngine;

namespace IsekaiTruck.Editor
{
    internal static class HudColorPalette
    {
        public static readonly Color EntryBackground = new Color32(0xFC, 0xCE, 0x7E, 0xFF);
        public static readonly Color DarkInk = new Color32(0x4C, 0x38, 0x45, 0xFF);
        public static readonly Color Cream = new Color32(0xF4, 0xE7, 0xC3, 0xFF);
        public static readonly Color SoftWhite = new Color32(0xFF, 0xFB, 0xF2, 0xFF);
        public static readonly Color ModalFace = new Color32(0xF2, 0xDC, 0xB7, 0xFF);
        public static readonly Color ModalInset = new Color32(0xFF, 0xF4, 0xD8, 0xFF);
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
        public static readonly Color Wanted = new Color32(0xE6, 0x5C, 0x45, 0xFF);
        public static readonly Color WantedDepth = new Color32(0x8E, 0x29, 0x30, 0xFF);
        public static readonly Color WantedTrack = new Color32(0x55, 0x29, 0x36, 0xFF);
        public static readonly Color WantedStar = new Color32(0xFF, 0xD3, 0x6B, 0xFF);
        public static readonly Color WantedBeaconRed = new Color32(0xF0, 0x4B, 0x5F, 0xFF);
        public static readonly Color WantedBeaconBlue = new Color32(0x61, 0xC8, 0xE6, 0xFF);

        public static bool Matches(Color actual, Color expected)
        {
            return Mathf.Approximately(actual.r, expected.r) &&
                Mathf.Approximately(actual.g, expected.g) &&
                Mathf.Approximately(actual.b, expected.b) &&
                Mathf.Approximately(actual.a, expected.a);
        }
    }
}
