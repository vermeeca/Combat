using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Combat
{
    public class Bullet
    {
        public Vector2 OriginalPosition { get; set; }
        public Vector2 CurrentPosition { get; set; }
        public Vector2 Velocity { get; set; }
        public float Angle { get; set; }

        public float X
        {
            get
            {
                return CurrentPosition.X;
            }

            set { CurrentPosition = new Vector2(value, CurrentPosition.Y); }
        }

        public float Y
        {
            get
            {
                return CurrentPosition.Y;
            }
            set
            {
                CurrentPosition = new Vector2(CurrentPosition.X, value);
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
    }
}
