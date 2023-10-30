using System.Drawing;
using System.Linq.Expressions;
using Rygg.Runes.Data.Core;
namespace Rygg.Runes.Spreads
{
   
    public enum SpreadResult
    {
        Fits,
        GeometryMismatch,
        TooManyRunes,
        NotEnoughRunes
    }
    public static class SpreadFactory
    {
        public static Spread Create(SpreadTypes spreadType)
        {
            return spreadType switch
            {
                SpreadTypes.Advice => new AdviceSpread(),
                SpreadTypes.AnswerToWhy => new AnswerToWhySpread(),
                SpreadTypes.Astrological => new AstrologicalSpread(),
                SpreadTypes.CelticCross => new CelticCrossSpread(),
                SpreadTypes.Choice => new ChoiceSpread(),
                SpreadTypes.FourCard => new FourCardSpread(),
                SpreadTypes.CurrentRelationship => new CurrentRelationshipSpread(),
                SpreadTypes.Decison => new DecisonSpread(),
                SpreadTypes.FiveCard => new FiveCardSpread(),
                SpreadTypes.Norns => new NornsSpread(),
                SpreadTypes.SevenGems => new SevenGemsSpread(),
                SpreadTypes.SimpleLove => new SimpleLoveSpread(),
                SpreadTypes.YesNo => new YesNoSpread(),
                _ => throw new NotImplementedException(),
            };
        }
    }
    public abstract class Spread
    {
        public abstract SpreadTypes Type { get; }
        public abstract int RuneCount { get; }
        public abstract string Name { get; }
        public virtual SpreadResult Validate(Rune[] runes, out Rune?[,] matrix)
        {
            matrix = GenerateMatrix(runes, out SpreadResult result);
            return result;
        }
        public abstract bool[,] ValidMatrix { get; }
        protected virtual SpreadResult ValidateCount(ref Rune[] runes)
        {
            runes = runes.Where(r => r.Probability >= 0.5d).ToArray();
            if (runes.Length > RuneCount)
                return SpreadResult.TooManyRunes;
            else if (runes.Length < RuneCount)
                return SpreadResult.NotEnoughRunes;
            return SpreadResult.Fits;
        }
        protected virtual IEnumerable<IGrouping<double, Rune>> GroupRunes(Rune[] runes, int closeness = 75)
        {
            return runes.OrderBy(r => r.Probability).Take(RuneCount).GroupBy(r => Math.Round(r.Center.Y / closeness) * closeness).Where(g => g.Key > 10);
        }
        protected Rune?[,] GenerateMatrix(Rune[] runes, out SpreadResult result)
        {
            var validMatrix = this.ValidMatrix;
            var rowCount = validMatrix.GetLength(0);
            var columnCount = validMatrix.GetLength(1);
            result = ValidateCount(ref runes);
            int closeNess = 75;
            IEnumerable<IGrouping<double, Rune>> groupedRunes;
            do
            {
                groupedRunes = GroupRunes(runes, closeNess);
                closeNess += 25;
            }
            while (groupedRunes.Count() > rowCount);
            
            Rune[,] matrix = new Rune[rowCount, columnCount];
           
            int row = 0;
            int runeCt = 0;
            foreach(var g in groupedRunes.OrderBy(r => r.Key))
            {
                int i = 0;
                foreach (var rune in g.OrderBy(r => r.Center.X))
                {
                    while (i < columnCount && !validMatrix[row, i])
                        i++;
                    if (i >= columnCount)
                        break;
                    matrix[row, i] = rune;
                    runeCt++;
                    i++;
                }
                row++;
                if (rowCount == row)
                    break;
            }
            if(result == SpreadResult.Fits)
            {
                int j = 0;
                while(j < rowCount)
                {
                    int i = 0;
                    while(i < columnCount)
                    {
                        if ((validMatrix[j, i] && matrix[j, i] == null) || (!validMatrix[j, i] && matrix[j, i] != null))
                        {
                            result = SpreadResult.GeometryMismatch;
                        }    
                        i++;
                    }
                    j++;
                }
            }
            return matrix;
        }

    }
   
    public class AstrologicalSpread : Spread
    { 
        public override int RuneCount => 12;
        public override bool[,] ValidMatrix => new bool[2, 6]
        {
            {true, true, true, true, true, true },
            {true, true, true, true, true, true }
        };
        public override string Name => "Astrological";

        public override SpreadTypes Type => SpreadTypes.Astrological;
    }
    public class ChoiceSpread : Spread
    {
        public override int RuneCount => 5;
        public override SpreadTypes Type => SpreadTypes.Choice;
        public override string Name => "Choice";
        public override bool[,] ValidMatrix => new bool[4, 3]
        {
            {true, false, true },
            {false, true, false },
            {false, true, false },
            {false, true, false }
        };
        
    }
    public class SimpleLoveSpread : Spread
    {
        public override int RuneCount => 5;
        public override SpreadTypes Type => SpreadTypes.SimpleLove;
        public override string Name => "Simple Love";
        public override bool[,] ValidMatrix => new bool[3, 3]
        {
            { true, false, true },
            { false, true, false },
            { true, false, true}
        };
       
    }
    public class CurrentRelationshipSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.CurrentRelationship;
        public override int RuneCount => 5;
        public override bool[,] ValidMatrix => new bool[2, 5]
        {
            {false, false, true, false, false },
            {true, true, false, true, true }
        };
        public override string Name => "Current Relationship";

        
    }
    public class YesNoSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.YesNo;
        public override bool[,] ValidMatrix => new bool[1, 1] { { true } };
        public override int RuneCount => 1;

        public override string Name => "Yes/No";

    }
    public class CelticCrossSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.CelticCross;
        public override bool[,] ValidMatrix => new bool[5, 4]
        {
            {false, false, false, true },
            {false, true, false, true },
            {true, true ,true, true },
            {false, true, false, true },
            {false, false,false, true }
        };
        public override int RuneCount => 10;

        public override string Name => "Celtic Cross";

       
    }
    public class AnswerToWhySpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.AnswerToWhy;
        public override bool[,] ValidMatrix => new bool[6, 5]
        {
            {false, false, true, false, false },
            {false, true, false, true, false },
            {true, false, true, false, true },
            {false, false, true, false, false },
            {false, false, true, false, false },
            {false, false, true, false, false }
        };
        public override int RuneCount => 9;

        public override string Name => "Answer to Why";

       
    }
    public class NornsSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.Norns;
        public override int RuneCount => 3;
        public override bool[,] ValidMatrix => new bool[1, 3]
        {
            {true, true, true }
        };
        public override string Name => "Norns";

        
    }
    public class AdviceSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.Advice;
        public override bool[,] ValidMatrix => new bool[4, 3]
        {
            {false, true, false },
            {true, false, true },
            {false, true, false },
            {false, true, false }
        };
        public override int RuneCount => 5;

        public override string Name => "Advice";
    }
    public class SevenGemsSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.SevenGems;
        public override bool[,] ValidMatrix => new bool[5, 3]
        {
            {false, true, false },
            {true, false, true },
            {false, true, false },
            {true, false, true },
            {false, true, false }
        };
        public override int RuneCount => 7;

        public override string Name => "Seven Gems";
    }
    public class FiveCardSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.FiveCard;
        public override bool[,] ValidMatrix => new bool[3, 3]
        {
            {false, true, false },
            {true, true, true },
            {false, true, false }
        };
        public override int RuneCount => 5;

        public override string Name => "Five Cards";
    }
    public class DecisonSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.Decison;
        public override bool[,] ValidMatrix => new bool[3, 5]
        {
            {false, false, true, false, false },
            {false, true, false, true, false },
            {true, false, false, false, true }
        };
        public override int RuneCount => 5;

        public override string Name => "Decision or Problem";
    }
    public class FourCardSpread : Spread
    {
        public override SpreadTypes Type => SpreadTypes.FourCard;
        public override bool[,] ValidMatrix => new bool[3, 3]
        {
            {false, true, false},
            {true, false, true },
            {false, true, false}
        };
        public override int RuneCount => 4;

        public override string Name => "Four Card";
    }
}
