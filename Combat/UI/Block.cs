using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Combat.UI
{
    public class Block : UIElement
    {
        public Block(Game game, UIElement parent, Vector2 position, int width, int height) :
            base(game, null, null, null, new Vector2?(position), width, height, parent)
        { }

        public override void Initialize()
        {
            this.SpriteColor = Color.Orange;
            base.Initialize();
        }

        public float Rotation
        {
            get
            {
                return rotation;
            }
            set
            {
                rotation = value;
            }
        }

        protected override void LoadContent()
        {
            this.Texture = CreateRectangle((int)this.Width, (int)this.Height, Color.Beige);
            base.LoadContent();
        }

        private Texture2D CreateRectangle(int width, int height, Color color)
        {
            Texture2D rectangleTexture = new Texture2D(GraphicsDevice, width, height, 1, TextureUsage.None,
            SurfaceFormat.Color);// create the rectangle texture, ,but it will have no color! lets fix that

            var colors = new Color[(width * height)];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            rectangleTexture.SetData<Color>(colors);

            return rectangleTexture;
        }
    }
}
