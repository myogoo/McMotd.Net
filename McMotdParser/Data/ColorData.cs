using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace McMotdParser.Data
{
    partial class MotdData
    {
        internal static readonly Dictionary<char, string> BedrockColors = new Dictionary<char, string>
        {
            ['g'] = "#DDD605", ['h'] = "#E3D4D1", ['i'] = "#CECACA", ['j'] = "#443A3B",
            ['m'] = "#971607", ['n'] = "#B4684D", ['p'] = "#DEB12D", ['q'] = "#47A036",
            ['s'] = "#2CBAA8", ['t'] = "#21497B", ['u'] = "#9A5CC6", ['v'] = "#EB7114", ['w'] = "#8BB3FF"
        };

        internal static bool TryColor(string? value, out string color)
        {
            color = "#808080";
            if (value is null) return false;
            if (value.Length == 7 && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit))
            {
                color = value;
                return true;
            }
            return ColorDict.TryGetValue(value.ToLowerInvariant(), out color!);
        }

        public static Dictionary<string, string> ColorDict = new Dictionary<string, string>(){
                    { "0", "#000000" },
                    { "1", "#0000AA" },
                    { "2", "#00AA00" },
                    { "3", "#00AAAA" },
                    { "4", "#AA0000" },
                    { "5", "#AA00AA" },
                    { "6", "#FFAA00" },
                    { "7", "#AAAAAA" },
                    { "8", "#555555" },
                    { "9", "#5555FF" },
                    { "a", "#55FF55" },
                    { "b", "#55FFFF" },
                    { "c", "#FF5555" },
                    { "d", "#FF55FF" },
                    { "e", "#FFFF55" },
                    { "f", "#FFFFFF" },
                    { "black", "#000000" },
                    { "dark_blue", "#0000AA" },
                    { "dark_green", "#00AA00" },
                    { "dark_aqua", "#00AAAA" },
                    { "dark_red", "#AA0000" },
                    { "dark_purple", "#AA00AA" },
                    { "gold", "#FFAA00" },
                    { "gray", "#AAAAAA" },
                    { "dark_gray", "#555555" },
                    { "blue", "#5555FF" },
                    { "green", "#55FF55" },
                    { "aqua", "#55FFFF" },
                    { "red", "#FF5555" },
                    { "light_purple", "#FF55FF" },
                    { "yellow", "#FFFF55" },
                    { "white", "#FFFFFF" }
        };
    }
}
