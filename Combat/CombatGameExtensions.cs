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

        public static void ResetPositionFrom(this UIElement one, UIElement two)
        {
            //ugly
            if (one.Left < two.Right && one.Right > two.Right)
            {
                one.Left = two.Right;
            }
            if (one.Right > two.Left && one.Left < two.Left)
            {
                one.Left = two.Left - one.Width;
            }
            if (one.Bottom > two.Top && one.Top < two.Top)
            {
                one.Top = two.Top - one.Height;
            }

        }

        public static Vector2 DetermineBounceDirectionFrom(this UIElement one, UIElement two)
        {
            var rectOne = new Rectangle((int)one.Left, (int)one.Top, (int)one.Width, (int)one.Height);
            //var rectTwo = new Rectangle((int)two.Left, (int)two.Top, (int)two.Width, (int)two.Height);
            int y = 1;
            int x = 1;

            if(one.TopDistanceFrom(two) > one.LeftDistanceFrom(two))
            {
                x = -1;
            }
            else
            {
                y = -1;
            }

            return new Vector2(x, y);


        }



        public static float TopDistanceFrom(this UIElement one, UIElement two)
        {
            return Math.Abs(one.Top - two.Top);
        }

        public static float BottomDistanceFrom(this UIElement one, UIElement two)
        {
            return Math.Abs(one.Bottom - two.Bottom);
        }

        public static float RightDistanceFrom(this UIElement one, UIElement two)
        {
            return Math.Abs(one.Right - two.Right);
        }

        public static float LeftDistanceFrom(this UIElement one, UIElement two)
        {
            return Math.Abs(one.Left - two.Left);
        }

        public static bool Intersects(this UIElement one, UIElement two)
        {
            var rectOne = new Rectangle((int)one.Left, (int)one.Top, (int)one.Width, (int)one.Height);
            var rectTwo = new Rectangle((int)two.Left, (int)two.Top, (int)two.Width, (int)two.Height);
            return rectOne.Intersects(rectTwo);
           
        }
    }
}
