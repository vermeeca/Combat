using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Combat.UI
{

    public abstract class BoardBase
    {

        protected Vector2 center;
        protected Game game;

        public BoardBase(Game game)
        {
            this.center =  new Vector2(game.GraphicsDevice.Viewport.Width / 2, game.GraphicsDevice.Viewport.Height / 2);
            this.game = game;
        }

        protected Block NewRectangleBlock(Vector2 position, int width, int height)
        {
            var block = new Block(game, null, position, width, height);
            return block;
        }

        public abstract IEnumerable<Block> GetObstacles();
    }
}
