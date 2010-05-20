using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Combat.UI;
using Microsoft.Xna.Framework.Graphics;

namespace Combat
{
    public class Bullet : UIElement
    {

        public Vector2 Velocity { get; set; }


        public List<Block> Obstacles { get; set; }
        
        public Bullet(Game game, Vector2 position)
            : this(game, position, game.Content.Load<Texture2D>("Ball"))
        {
            // Empty.
        }

        private Bullet(Game game, Vector2 position, Texture2D texture)
            : base(game, null, null, texture, new Vector2?(position), texture.Width, texture.Height, null)
        { 

        }

        public override void Update(GameTime gameTime)
        {
            var traveled = Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;
            this.TransformedCenter += traveled;
            this.DistanceTraveled += traveled.Length();
            CheckBounce();
            CheckDistanceTraveled();
            if (HasHitOpponent() && !(Opponent.Dying()))
            {
                Opponent.Die();
                Owner.Score++;
                Game.Components.Remove(this);
            }
            CheckObstacles();
            base.Update(gameTime);

        }

        private void CheckObstacles()
        {
            foreach (var obstacle in Obstacles)
            {
                if (ObstacleHit(obstacle))
                {
                    return;
                }
            }
        }

        private bool ObstacleHit(Block obstacle)
        {
            if (this.Intersects(obstacle))
            {
                this.ResetPositionFrom(obstacle);
                //hit top
                this.Velocity *= this.DetermineBounceDirectionFrom(obstacle);

                return true;
            }

            return false;
        }

        private void CheckDistanceTraveled()
        {
            if (this.DistanceTraveled > 1500)
            {
                this.Game.Components.Remove(this);
            }
        }

        private void CheckBounce()
        {
            int MaxX = GraphicsDevice.Viewport.Width;
            int MinX = 0;
            int MaxY = GraphicsDevice.Viewport.Height;
            int MinY = 0;

            // Check for bounce.
            if (Right > MaxX)
            {
                ReverseX();
                TransformedCenter = new Vector2(MaxX - Width / 2, TransformedCenter.Y);
            }

            else if (Left < MinX)
            {
                ReverseX();
                TransformedCenter = new Vector2(MinX + Width / 2, TransformedCenter.Y);
            }

            if (Bottom > MaxY)
            {
                ReverseY();
                TransformedCenter = new Vector2(TransformedCenter.X, MaxY - Height / 2);
            }

            else if (Top < MinY)
            {
                ReverseY();
                TransformedCenter = new Vector2(TransformedCenter.X, MinY + Height / 2);
            }
        }


        public void ReverseX()
        {
            Velocity = new Vector2(Velocity.X * -1, Velocity.Y);
        }

        public void ReverseY()
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y * -1);
        }

        public float DistanceTraveled { get; set; }

        public bool HasTraveledMaxDistance()
        {
            return DistanceTraveled > 1500;
        }



        public bool HasHitOpponent()
        {
            return this.Intersects(Opponent);
        }

        public Tank Opponent { get; set; }

        public Tank Owner { get; set; }

    }
}
