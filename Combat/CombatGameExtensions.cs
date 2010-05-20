using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Combat.UI;

namespace Combat
{
    public static class CombatGameExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        public static bool Intersects(this Bullet bullet, Tank tank)
        {
            var bulletRectangle = bullet.DrawingRectangle;
            var tankRectangle = new Rectangle((int)tank.Left, (int)tank.Top, (int)tank.Width, (int)tank.Height);
            return bulletRectangle.Intersects(tankRectangle);
           
        }
    }
}
