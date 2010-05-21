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
            blocks.Add(NewRectangleBlock(center - new Vector2(75, 0), 50, 25));
            blocks.Add(NewRectangleBlock(center + new Vector2(75, 0), 50, 25));
            blocks.Add(NewRectangleBlock(center - new Vector2(0, 75), 25, 50));
            blocks.Add(NewRectangleBlock(center + new Vector2(0, 75), 25, 50));


            blocks.Add(NewRectangleBlock(center - new Vector2(200, 0), 25, 100));
            blocks.Add(NewRectangleBlock(center - new Vector2(225, 37.5f), 25, 25));
            blocks.Add(NewRectangleBlock(center - new Vector2(225, -37.5f), 25, 25));

            blocks.Add(NewRectangleBlock(center + new Vector2(200, 0), 25, 100));
            blocks.Add(NewRectangleBlock(center + new Vector2(225, 37.5f), 25, 25));
            blocks.Add(NewRectangleBlock(center + new Vector2(225, -37.5f), 25, 25));

            return blocks;
            
        }

        #endregion
    }
}
