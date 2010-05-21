using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Combat.UI
{
    public class Board1 : BoardBase
    {

        public Board1(Game game)
            : base(game)
        { }

        #region IBoard Members

        public override IEnumerable<Block> GetObstacles()
        {
            var blocks = new List<Block>();
            blocks.Add(NewRectangleBlock(center - new Vector2(100, 0), false));
            blocks.Add(NewRectangleBlock(center + new Vector2(100, 0), false));
            blocks.Add(NewRectangleBlock(center - new Vector2(0, 100), true));
            blocks.Add(NewRectangleBlock(center + new Vector2(0, 100), true));

            return blocks;
            
        }

        #endregion
    }
}
