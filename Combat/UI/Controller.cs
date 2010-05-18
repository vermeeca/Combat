using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Combat.UI
{
    public class Controller : UIElement
    {
        public Controller(Game game, Vector2 position)
            : this(game, position, game.Content.Load<Texture2D>("Controller"))
        {
            // Empty.
        }

        Button forward;
        Button left;
        Button right;
        Button backward;
        Button fire;

        public Tank Tank { get; set; }
        

        private Controller(Game game, Vector2 position, Texture2D texture)
            : base(game, null, null, texture, new Vector2?(position), texture.Width, texture.Height, null)
        {
            
            

        }

        public override void Initialize()
        {
            forward = new Button(this.Game, Vector2.Zero, this);
            forward.Pressed = () => Tank.MoveForward();
            forward.TransformedCenter += new Vector2(0, ((this.Height / 2) - (forward.Height / 2)) * -1);

            backward = new Button(this.Game, Vector2.Zero, this);
            backward.Pressed = () => Tank.MoveBackward();
            backward.TransformedCenter += new Vector2(0, ((this.Height / 2) - (backward.Height / 2)));

            left = new Button(this.Game, Vector2.Zero, this);
            left.TransformedCenter += new Vector2(((this.Width / 2) - left.Width / 2) * -1, 0);

            right = new Button(this.Game, Vector2.Zero, this);
            right.TransformedCenter += new Vector2(((this.Width / 2) - right.Width / 2), 0);


            fire = new Button(this.Game, Vector2.Zero, this);
            fire.TransformedCenter += new Vector2(((this.Width / 2) - left.Width / 2) * -1, ((this.Height / 2) - (forward.Height / 2)) * -1);

            AddChild(forward);
            AddChild(backward);
            AddChild(left);
            AddChild(right);
            AddChild(fire);

            this.Active = false;
            base.Initialize();
        }

        internal void HandleContact(Microsoft.Surface.Core.Contact contact)
        {
            var touched = this.HitTesting(contact, false);

            if (touched is Button)
            {
                var button = touched as Button;
                Console.WriteLine("here");
                button.Pressed();
            }

            
        }
    }
}
