using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Surface.Core;

namespace Combat.UI
{
    public class Button : UIElement
    {
        public Button(Game game, Vector2 position, UIElement parent)
            : this(game, position, game.Content.Load<Texture2D>("Button"), parent)
        {
            // Empty.
        }

        private Button(Game game, Vector2 position, Texture2D texture, UIElement parent)
            : base(game, null, null, texture, new Vector2?(position), texture.Width, texture.Height, parent)
        { 
            
        }

        public Contact Contact { get; set; }

        

        public Action Pressed { get; set; }

        public Action Released { get; set; }
    }
}
