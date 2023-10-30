using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rygg.Runes.Data.Core
{
    public class MysticRequest
    {
        public string[] Runes { get; set; } = null!;
        public string Question { get; set; } = null!;
        public SpreadTypes SpreadType { get; set; }
    }
    public enum SpreadTypes
    {
        Astrological,
        Choice,
        SimpleLove,
        CurrentRelationship,
        YesNo,
        CelticCross,
        AnswerToWhy,
        Norns,
        SevenGems,
        Advice,
        FiveCard,
        Decison,
        FourCard

    }
    public class Reading
    {
        public long Id { get; set; }
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
        public Rune[] Runes { get; set; } = null!;
        public byte[] AnnotatedImage { get; set; } = null!;
    }
    public class Rune
    {
        public Rune(string data, Point? pt = null)
        {
            var d = data.Split(' ');
            this.Name = d[0];
            if (d.Length > 1)
            {
                this.Probability = double.Parse(d[1]);
                this.X1 = int.Parse(d[2]);
                this.Y1 = int.Parse(d[3]);
                this.X2 = int.Parse(d[4]);
                this.Y2 = int.Parse(d[5]);
            }
            else if (pt != null)
            {
                this.Probability = 1;
                this.X1 = pt.Value.X;
                this.X2 = pt.Value.X;
                this.Y1 = pt.Value.Y;
                this.Y2 = pt.Value.Y;
            }
            else
                throw new InvalidDataException();
        }
        public override string ToString()
        {
            return $"{Name} {Probability} {X1} {Y1} {X2} {Y2}";
        }
        public string Name { get; set; } = null!;
        public double Probability { get; set; }
        public int X1 { get; set; }
        public int Y1 { get; set; }
        public int X2 { get; set; }
        public int Y2 { get; set; }
        public PointF Center
        {
            get
            {
                return new PointF((X1 + X2) / 2.0f, (Y1 + Y2) / 2.0f);
            }
        }
        public string RunicCharachter
        {
            get
            {
                switch (Name.ToLower().Trim())
                {
                    case "algiz": return "\u16A8";
                    case "ansuz": return "\u16A9";
                    case "berkana": return "\u16AA";
                    case "dagaz": return "\u16AB";
                    case "ehwaz": return "\u16AC";
                    case "eywas": return "\u16AD";
                    case "fehu": return "\u16AE";
                    case "gebo": return "\u16AF";
                    case "hagall": return "\u16B0";
                    case "ice": return "\u16B1";
                    case "ingwaz": return "\u16B2";
                    case "jera": return "\u16B3";
                    case "kanu": return "\u16B4";
                    case "lagu": return "\u16B5";
                    case "mannaz": return "\u16B6";
                    case "nyedis": return "\u16B7";
                    case "othala": return "\u16B8";
                    case "pertho": return "\u16B9";
                    case "raido": return "\u16BA";
                    case "sowuli": return "\u16BB";
                    case "teiwaz": return "\u16BC";
                    case "thurizas": return "\u16BD";
                    case "urus": return "\u16BE";
                    case "wunjo": return "\u16BF";
                    default: throw new NotImplementedException();
                }
            }
        }
    }
}
