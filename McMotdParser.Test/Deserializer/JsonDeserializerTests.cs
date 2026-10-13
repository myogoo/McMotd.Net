using McMotdParser.Deserializer;
using McMotdParser.Enum;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace McMotdParser.Test.Deserializer
{
    public class JsonDeserializerTests
    {
        [Fact]
        public void VerySimpleJsonMotdDeserialize()
        {
            string raw_motd = @"{""text"":""기모찌서버""}";
            var testResult = new MotdParser().deserialize(raw_motd);
            

            MotdContents contents= new MotdContents();
            List<MotdContent> expect = new List<MotdContent>()
            {
                new MotdContent() { Color =  "#808080", Text = "기모찌서버"  }
            };

            Assert.True(testResult.SequenceEqual(expect));
        }
        
        [Fact]
        public void NestedJsonMotdDeserialize()
        {
            string raw_motd = @"{""color"":""red"",""extra"":[{""color"":""green"",""text"":""World""}],""text"":""Hello ""}";
            var testResult = new MotdParser().deserialize(raw_motd);
            
            List<MotdContent> expect = new List<MotdContent>()
            {
                new MotdContent{ Color = "#FF5555", Text = "Hello "},
                new MotdContent{ Color = "#55FF55", Text = "World"},
            };

            Assert.True(testResult.SequenceEqual(expect));
        }
        
        [Fact]
        public void ComplexJsonMotdDeserialize()
        {
            //string raw_motd = @"{""extra"":[{""Color"":""aqua"",""text"":""◆ ""},{""bold"":true,""italic"":true,""Color"":""#00ffff"",""text"":""스""},{""bold"":true,""italic"":true,""Color"":""#19e5ff"",""text"":""티""},{""bold"":true,""italic"":true,""Color"":""#33ccff"",""text"":""브""},{""bold"":true,""italic"":true,""Color"":""#4cb2ff"",""text"":""""},{""bold"":true,""italic"":true,""Color"":""#6699ff"",""text"":""갤""},{""bold"":true,""italic"":true,""Color"":""#7f7fff"",""text"":""러""},{""bold"":true,""italic"":true,""Color"":""#9966ff"",""text"":""리""},{""bold"":true,""italic"":true,""Color"":""#b24cff"",""text"":""""},{""bold"":true,""italic"":true,""Color"":""#cc32ff"",""text"":""놀""},{""bold"":true,""italic"":true,""Color"":""#e519ff"",""text"":""이""},{""bold"":true,""italic"":true,""Color"":""#ff00ff"",""text"":""터""},{""Color"":""light_purple"",""text"":"" ◆\r\n""},{""Color"":""gray"",""text"":""건축\/쉼터""}],""text"":""""}";
            string raw_motd = "{\"extra\":[{\"color\":\"aqua\",\"text\":\"◆ \"},{\"bold\":true,\"italic\":true,\"color\":\"#00ffff\",\"text\":\"스\"},{\"bold\":true,\"italic\":true,\"color\":\"#19e5ff\",\"text\":\"티\"},{\"bold\":true,\"italic\":true,\"color\":\"#33ccff\",\"text\":\"브\"},{\"bold\":true,\"italic\":true,\"color\":\"#4cb2ff\",\"text\":\"\"},{\"bold\":true,\"italic\":true,\"color\":\"#6699ff\",\"text\":\"갤\"},{\"bold\":true,\"italic\":true,\"color\":\"#7f7fff\",\"text\":\"러\"},{\"bold\":true,\"italic\":true,\"color\":\"#9966ff\",\"text\":\"리\"},{\"bold\":true,\"italic\":true,\"color\":\"#b24cff\",\"text\":\"\"},{\"bold\":true,\"italic\":true,\"color\":\"#cc32ff\",\"text\":\"놀\"},{\"bold\":true,\"italic\":true,\"color\":\"#e519ff\",\"text\":\"이\"},{\"bold\":true,\"italic\":true,\"color\":\"#ff00ff\",\"text\":\"터\"},{\"color\":\"light_purple\",\"text\":\" ◆\"},{\"color\":\"gray\",\"text\":\"건축/쉼터\"}],\"text\":\"\"}";
            
            var testResult = new MotdParser().deserialize(raw_motd);
            
            List<MotdContent> expect = new List<MotdContent>()
            {
                new MotdContent{ Color = "#55FFFF", Text = "◆ "},
                new MotdContent{ Color = "#00ffff", Text = "스", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#19e5ff", Text = "티", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#33ccff", Text = "브", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#6699ff", Text = "갤", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#7f7fff", Text = "러", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#9966ff", Text = "리", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#cc32ff", Text = "놀", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#e519ff", Text = "이", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#ff00ff", Text = "터", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold,TextFormatEnum.Italic } },
                new MotdContent{ Color = "#FF55FF", Text = " ◆" },
                new MotdContent{ Color = "#AAAAAA", Text = "건축/쉼터" },
            };

            Assert.True(testResult.SequenceEqual(expect));
        }

        [Fact]
        public void SectionSignMotdDeserializeWithEscapeCharacter()
        {
            string raw_motd = "                §aHypixel Network §c[1.8-1.20]\r\n        §b§lDROPPER v1.0 §7- §6§lNEW ARCADE LOBBY";


            var testResult = new MotdParser().deserialize(raw_motd);
            MotdContents contents = new MotdContents();

            List<MotdContent> except = new List<MotdContent>()
            {
                new MotdContent { Color = "#808080", Text = "                " },
                new MotdContent { Color = "#55FF55", Text = "Hypixel Network "},
                new MotdContent { Color = "#FF5555", Text = "[1.8-1.20]" },
                new MotdContent { Color = "#FF5555", Text = "        " , LineBreak = true },
                new MotdContent { Color = "#55FFFF", Text = "DROPPER v1.0 ", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }},
                new MotdContent { Color = "#AAAAAA", Text = "- " },
                new MotdContent { Color = "#FFAA00", Text = "NEW ARCADE LOBBY", TextFormatting = new HashSet<TextFormatEnum> { TextFormatEnum.Bold }}
            };

            contents.Contents = except;

            Assert.Equal(contents.Contents,testResult);
        }

        [Fact]
        public void SimpleJsonMotdDeserialize()
        {
            string raw_motd = @"{""color"" : ""gold"",""bold"" : true,""text"" : ""뉴인타운+RPG+반야생 스망호 1.18.2~1.20.2""}";
            
            var testResult = new MotdParser().deserialize(raw_motd);
            MotdContents contents = new MotdContents();

            List<MotdContent> except = new List<MotdContent>()
            {
                new MotdContent { Color = "#FFAA00", Text = "뉴인타운+RPG+반야생 스망호 1.18.2~1.20.2", TextFormatting = new HashSet<TextFormatEnum>() { TextFormatEnum.Bold }}
            };

            contents.Contents = except;
            
            Assert.Equal(except,testResult);
        }
    }
}



