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


        private bool movingForward = false;
        private bool movingBackward = false;
        private bool turningRight = false;
        private bool turningLeft = false;

        public EventHandler<EventArgs<Tank>> Fired;


        public Tank(Game game, Vector2 position)
            : this(game, position, game.Content.Load<Texture2D>("Tank"))
        {
            // Empty.
        }

        private Tank(Game game, Vector2 position, Texture2D texture)
            : base(game, null, null, texture, new Vector2?(position), texture.Width, texture.Height, null)
        {

        }

        private TimeSpan? deathTimeout;

        public void Die()
        {
            deathTimeout = TimeSpan.FromSeconds(2);
        }


        public void Fire()
        {
            if (Fired != null)
            {
                Fired(this, new EventArgs<Tank>(this));
            }
        }

        public void StartMovingForward()
        {
            movingForward = true;
        }

        public void StopMovingForward()
        {
            movingForward = false;
        }

        public void StartMovingBackward()
        {
            movingBackward = true;
        }

        public void StopMovingBackward()
        {
            movingBackward = false;
        }

        public void StartTurningRight()
        {
            turningRight = true;
        }

        public void StopTurningRight()
        {
            turningRight = false;
        }

        public void StartTurningLeft()
        {
            turningLeft = true;
        }

        public void StopTurningLeft()
        {
            turningLeft = false;
        }

        public override void Update(GameTime gameTime)
        {
            if (movingForward)
            {
                MoveForward();
            }
            if (movingBackward)
            {
                MoveBackward();
            }
            if (turningRight)
            {
                Rotation += .035f;
            }
            if (turningLeft)
            {
                Rotation -= .035f;
            }

            if (Dying())
            {
                Rotation += .5f;
                deathTimeout = deathTimeout.Value.Subtract(gameTime.ElapsedRealTime);
            }

            base.Update(gameTime);
        }

        private bool Dying()
        {
            return deathTimeout != null && deathTimeout.Value.TotalMilliseconds > 0;
        }

        private void MoveForward()
        {
            this.TransformedCenter += new Vector2(-(float)Math.Cos(Rotation),
                                             -(float)Math.Sin(Rotation)) * 2f;
        }

        public float Rotation
        {
            get { return rotation; }
            set 
            { 
                rotation = value;
            }
        }

        private void MoveBackward()
        {
            TransformedCenter += new Vector2((float)Math.Cos(Rotation),
                                              (float)Math.Sin(Rotation)) * 2f;
        }

        
    }
}
