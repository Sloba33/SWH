using UnityEngine;

namespace TetraCreations.Attributes
{
    public static class ColorExtensions
    {
        // Convert the TitleColor enum to an actual Color32
        public static Color32 ToColor(this TitleColor color)
        {
            switch (color)
            {
                case TitleColor.Aqua: return new Color32(127, 219, 255, 255);
                case TitleColor.Beige: return new Color32(245, 245, 220, 255);
                case TitleColor.Black: return new Color32(0, 0, 0, 255);
                case TitleColor.Blue: return new Color32(31, 133, 221, 255);
                case TitleColor.BlueVariant: return new Color32(67, 110, 238, 255);
                case TitleColor.DarkBlue: return new Color32(41, 41, 225, 255);
                case TitleColor.Bright: return new Color32(196, 196, 196, 255);
                case TitleColor.Brown: return new Color32(148, 96, 59, 255);
                case TitleColor.Cyan: return new Color32(0, 255, 255, 255);
                case TitleColor.DarkGray: return new Color32(36, 36, 36, 255);
                case TitleColor.Fuchsia: return new Color32(240, 18, 190, 255);
                case TitleColor.Gray: return new Color32(88, 88, 88, 255);
                case TitleColor.Green: return new Color32(98, 200, 79, 255);
                case TitleColor.Indigo: return new Color32(75, 0, 130, 255);
                case TitleColor.LightGray: return new Color32(128, 128, 128, 255);
                case TitleColor.Lime: return new Color32(1, 255, 112, 255);
                case TitleColor.Navy: return new Color32(15, 35, 86, 255);
                case TitleColor.Olive: return new Color32(61, 153, 112, 255);
                case TitleColor.DarkOlive: return new Color32(47, 79, 79, 255);
                case TitleColor.Orange: return new Color32(255, 128, 0, 255);
                case TitleColor.OrangeVariant: return new Color32(255, 135, 62, 255);
                case TitleColor.Pink: return new Color32(255, 152, 203, 255);
                case TitleColor.Red: return new Color32(234, 42, 42, 255);
                case TitleColor.LightRed: return new Color32(217, 71, 71, 255);
                case TitleColor.RedVariant: return new Color32(232, 10, 10, 255);
                case TitleColor.DarkRed: return new Color32(144, 20, 39, 255);
                case TitleColor.Tan: return new Color32(210, 180, 140, 255);
                case TitleColor.Teal: return new Color32(27, 126, 126, 255);
                case TitleColor.Violet: return new Color32(181, 93, 237, 255);
                case TitleColor.White: return new Color32(255, 255, 255, 255);
                case TitleColor.Yellow: return new Color32(255, 211, 0, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }
    }
}