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

        public static bool Exists<T>(this IEnumerable<T> collection, Func<T, bool> condition)
        {
            return (collection.FirstOrDefault(condition) != null);
        }

        public static Vector2 DetermineVelocityAndSetPositionFrom(this UIElement one, UIElement two)
        {
            int x = 1;
            int y = 1;

            if (one.Left < two.Right && one.Right > two.Right)//from the right
            {
                one.Left = two.Right + 1;
                x = -1;
            }
            if (one.Right > two.Left && one.Left < two.Left)//from the left
            {
                one.Left = two.Left - one.Width - 1;
                x = -1;
            }
            if (one.Bottom > two.Top && one.Top < two.Top) //from the top
            {
                one.Top = two.Top - one.Height - 1;
                y = -1;
            }
            if (one.Top < two.Bottom && one.Bottom > two.Bottom) //from the bottom
            {
                one.Top = two.Bottom + 1;
                y = -1;
            }

            //now slide
            //now dip
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
