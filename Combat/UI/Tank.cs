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

        public int Score { get; set; }

        public EventHandler<EventArgs<Tank>> Fired;

        public IEnumerable<Block> Obstacles { get; set; }

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

        public void Reset(Vector2 position, float rotation)
        {
            this.TransformedCenter = position;
            this.Rotation = rotation;
            this.Score = 0;
            this.deathTimeout = null;
        }

        public override void Update(GameTime gameTime)
        {
            try
            {

                var lastPosition = this.TransformedCenter;

                if (movingForward)
                {
                    MoveForward();
                }
                if (movingBackward)
                {
                    MoveBackward();
                }

                if (this.CollidingWithAnObstacleOrWall())
                {
                    this.TransformedCenter = lastPosition;
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
            }
            catch (Exception ex)
            {
                Utility.Log(ex.ToString());
            }

            base.Update(gameTime);
        }

        private bool CollidingWithAnObstacleOrWall()
        {
            return CollidingWithWall() || Obstacles.Exists(o => this.Intersects(o));
            
        }

        private bool CollidingWithWall()
        {
            return this.Left < 0 || this.Right > this.Game.GraphicsDevice.Viewport.Width
                || this.Top < 0 || this.Bottom > this.Game.GraphicsDevice.Viewport.Height;
        }

        public bool Dying()
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
