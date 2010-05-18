using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using CoreInteractionFramework;
using Microsoft.Xna.Framework.Graphics;

namespace Combat.UI
{
    public class Tank : UIElement
    {
       
        public Tank(Game game, Vector2 position)
            : this(game, position, game.Content.Load<Texture2D>("Tank"))
        {
            // Empty.
        }

        private Tank(Game game, Vector2 position, Texture2D texture)
            : base(game, null, null, texture, new Vector2?(position), texture.Width, texture.Height, null)
        { }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

    }
}
