using McMotdParser.Deserializer;
using System;
using System.Diagnostics;
using McMotdParser.Enum;
using Xunit;

namespace McMotdParser.Test.Deserializer
{
    public class SectionSignDeserializerTests
    {
        [Fact]
        public void SectionDeserializer()
        {
            // Arrange
            var sectionSignDeserializer = new SectionSignDeserializer(@"                §aHypixel Network §c[1.8-1.20]        §b§lDROPPER v1.0 §7- §6§lNEW ARCADE LOBBY");

            // Act
            List<MotdContent> contents = sectionSignDeserializer.deserialize();
            List<MotdContent> expect = new List<MotdContent>()
            {
                new MotdContent { Color = "#808080", Text = "                " },
                new MotdContent { Color = "#55FF55", Text = "Hypixel Network " },
                new MotdContent { Color = "#FF5555", Text = "[1.8-1.20]        " },
                new MotdContent { Color = "#55FFFF", Text = "DROPPER v1.0 ", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }},
                new MotdContent { Color = "#AAAAAA", Text = "- " },
                new MotdContent { Color = "#FFAA00", Text = "NEW ARCADE LOBBY", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }}
            };
            // Assert
            Assert.True(contents.SequenceEqual(expect));
        }
        [Fact]
        public void SectionDeserializerStartWithSign()
        {
            var sectionSignDeserializer = new SectionSignDeserializer(@"§aHypixel Network §c[1.8-1.20]        §b§lDROPPER v1.0 §7- §6§lNEW ARCADE LOBBY");

            // Act
            List<MotdContent> contents = sectionSignDeserializer.deserialize();

            List<MotdContent> expect = new List<MotdContent>()
            {
                new MotdContent { Color = "#55FF55", Text = "Hypixel Network " },
                new MotdContent { Color = "#FF5555", Text = "[1.8-1.20]        " },
                new MotdContent { Color = "#55FFFF", Text = "DROPPER v1.0 ", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }},
                new MotdContent { Color = "#AAAAAA", Text = "- " },
                new MotdContent { Color = "#FFAA00", Text = "NEW ARCADE LOBBY", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }}
            };
            // Assert
            Assert.True(contents.SequenceEqual(expect));
        }        
        [Fact]
        public void SectionDeserializerWithLineBreak()
        {
            var sectionSignDeserializer = new SectionSignDeserializer("§aHypixel Network §c[1.8-1.20]\r\n        §b§lDROPPER v1.0 §7- §6§lNEW ARCADE LOBBY");

            // Act
            List<MotdContent> contents = sectionSignDeserializer.deserialize();

            List<MotdContent> expect = new List<MotdContent>()
            {
                new MotdContent { Color = "#55FF55", Text = "Hypixel Network " },
                new MotdContent { Color = "#FF5555", Text = "[1.8-1.20]"},
                new MotdContent { Color = "#FF5555", Text = "        ", LineBreak = true },
                new MotdContent { Color = "#55FFFF", Text = "DROPPER v1.0 ", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }},
                new MotdContent { Color = "#AAAAAA", Text = "- " },
                new MotdContent { Color = "#FFAA00", Text = "NEW ARCADE LOBBY", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }}
            };
            // Assert
            Assert.True(contents.SequenceEqual(expect));
        }
    }
}
