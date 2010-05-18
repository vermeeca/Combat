﻿using System;
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


        public Tank(Game game, Vector2 position)
            : this(game, position, game.Content.Load<Texture2D>("Tank"))
        {
            // Empty.
        }

        private Tank(Game game, Vector2 position, Texture2D texture)
            : base(game, null, null, texture, new Vector2?(position), texture.Width, texture.Height, null)
        {

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
            base.Update(gameTime);
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
