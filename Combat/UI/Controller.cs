using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Surface.Core;

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
            forward.Pressed = () => Tank.StartMovingForward();
            forward.Released = () => Tank.StopMovingForward();
            forward.TransformedCenter += new Vector2(0, ((this.Height / 2) - (forward.Height / 2)) * -1);

            backward = new Button(this.Game, Vector2.Zero, this);
            backward.Pressed = () => Tank.StartMovingBackward();
            backward.Released = () => Tank.StopMovingBackward();
            backward.TransformedCenter += new Vector2(0, ((this.Height / 2) - (backward.Height / 2)));

            left = new Button(this.Game, Vector2.Zero, this);
            left.TransformedCenter += new Vector2(((this.Width / 2) - left.Width / 2) * -1, 0);
            left.Pressed = Tank.StartTurningLeft;
            left.Released = Tank.StopTurningLeft;

            right = new Button(this.Game, Vector2.Zero, this);
            right.TransformedCenter += new Vector2(((this.Width / 2) - right.Width / 2), 0);
            right.Pressed = Tank.StartTurningRight;
            right.Released = Tank.StopTurningRight;


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

        public void HandleContactChanged(Contact contact)
        {
            var allbuttons = new[] { forward, backward, left, right, fire };
            var button = allbuttons.Where(b=>b.Contact != null).FirstOrDefault(b => b.Contact.Id == contact.Id);

            if (button == null)
                return;

            if (button.HitTest(contact, false))
            {
                button.Pressed();
            }
            else
            {
                button.Released();
            }
        }


        public void HandleContactAdded(Microsoft.Surface.Core.Contact contact)
        {
            var touched = this.HitTesting(contact, false);

            if (touched is Button)
            {
                var button = touched as Button;
                button.Contact = contact;
                if (button.Pressed != null)
                {
                    button.Pressed();
                }
            }
        }

        public void HandleContactReleased(Contact contact)
        {
            var touched = this.HitTesting(contact, false);

            if (touched is Button)
            {
                var button = touched as Button;
                button.Contact = null;
                if (button.Released != null)
                {
                    button.Released();
                }
            }
        }
    }
}
