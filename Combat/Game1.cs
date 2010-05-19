#region File Description
//-----------------------------------------------------------------------------
// Game1.cs
//
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Surface.Core;
using Combat.UI;

namespace Combat
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private ContactTarget contactTarget;

        private Controller player1Controller;
        private Controller player2Controller;


        private List<Bullet> bullets = new List<Bullet>();

        private Tank player1;
        private Tank player2;

        /// <summary>
        /// The target receiving all surface input for the application.
        /// </summary>
        protected ContactTarget ContactTarget
        {
            get { return contactTarget; }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            SetWindowOnSurface();
            InitializeSurfaceInput();

            var player1Position = new Vector2(graphics.GraphicsDevice.Viewport.Width - 100, graphics.GraphicsDevice.Viewport.Height / 2);
            var player2Position = new Vector2(100, graphics.GraphicsDevice.Viewport.Height / 2);

            ContactTarget.ContactAdded += new EventHandler<ContactEventArgs>(ContactTarget_ContactAdded);
            ContactTarget.ContactRemoved += new EventHandler<ContactEventArgs>(ContactTarget_ContactRemoved);
            ContactTarget.ContactChanged += new EventHandler<ContactEventArgs>(ContactTarget_ContactChanged);

            player1 = new Tank(this, player1Position);
            player1.Fired += TankFired;
            player1Controller = new Controller(this, new Vector2(graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height));
            player1Controller.Tank = player1;
            player1Controller.Height *= 2;
            player1Controller.Width *= 2;
            player1Controller.TransformedCenter -= new Vector2(player1Controller.Width * 1.5f, player1Controller.Height / 2);

            player2 = new Tank(this, player2Position);
            player2.Rotation = MathHelper.ToRadians(180);
            player2.Fired += TankFired;

            player2Controller = new Controller(this, new Vector2(0, graphics.GraphicsDevice.Viewport.Height));
            player2Controller.Tank = player2;
            player2Controller.Height *= 2;
            player2Controller.Width *= 2;
            player2Controller.TransformedCenter -= new Vector2(player1Controller.Width * -1.5f, player1Controller.Height / 2);



            Components.Add(player1);
            Components.Add(player2);
            Components.Add(player1Controller);
            Components.Add(player2Controller);

            base.Initialize();
        }

        private void TankFired(object sender, EventArgs<Tank> e)
        {
            lock (bulletLock)
            {
                var bullet = new Bullet();
                var tank = e.EventData;
                var originalPosition = new Vector2(tank.TransformedCenter.X - (ballTexture.Width), tank.TransformedCenter.Y - (ballTexture.Height / 2));


                bullet.Angle = tank.Rotation % CIRCLE;

                bullet.Velocity = new Vector2(-(float)Math.Cos(tank.Rotation),
                                             -(float)Math.Sin(tank.Rotation)) * 1000.0f;
                bullet.OriginalPosition = originalPosition + bullet.Velocity * .015f;
                bullet.CurrentPosition = bullet.OriginalPosition;

                bullets.Add(bullet);
            }
        }

        void ContactTarget_ContactChanged(object sender, ContactEventArgs e)
        {
            player1Controller.HandleContactChanged(e.Contact);
            player2Controller.HandleContactChanged(e.Contact);
        }

        void ContactTarget_ContactRemoved(object sender, ContactEventArgs e)
        {
            player1Controller.HandleContactReleased(e.Contact);
            player2Controller.HandleContactReleased(e.Contact);
        }

        void ContactTarget_ContactAdded(object sender, ContactEventArgs e)
        {
            player1Controller.HandleContactAdded(e.Contact);
            player2Controller.HandleContactAdded(e.Contact);
        }

      
        /// <summary>
        /// Moves and sizes the window to cover the input surface.
        /// </summary>
        private void SetWindowOnSurface()
        {
            System.Diagnostics.Debug.Assert(Window.Handle != System.IntPtr.Zero,
                "Window initialization must be complete before SetWindowOnSurface is called");
            if (Window.Handle == System.IntPtr.Zero)
                return;

            // We don't want to run in full-screen mode because we need
            // overlapped windows, so instead run in windowed mode
            // and resize to take up the whole surface with no border.

            // Make sure the graphics device has the correct back buffer size.
            InteractiveSurface interactiveSurface = InteractiveSurface.DefaultInteractiveSurface;
            if (interactiveSurface != null)
            {
                graphics.PreferredBackBufferWidth = interactiveSurface.Width;
                graphics.PreferredBackBufferHeight = interactiveSurface.Height;
                graphics.ApplyChanges();

                // Remove the border and position the window.
                Program.RemoveBorder(Window.Handle);
                Program.PositionWindow(Window);
            }
        }

        /// <summary>
        /// Initializes the surface input system. This should be called after any window
        /// initialization is done, and should only be called once.
        /// </summary>
        private void InitializeSurfaceInput()
        {
            System.Diagnostics.Debug.Assert(Window.Handle != System.IntPtr.Zero,
                "Window initialization must be complete before InitializeSurfaceInput is called");
            if (Window.Handle == System.IntPtr.Zero)
                return;
            System.Diagnostics.Debug.Assert(contactTarget == null,
                "Surface input already initialized");
            if (contactTarget != null)
                return;

            // Create a target for surface input.
            contactTarget = new ContactTarget(Window.Handle, EventThreadChoice.OnBackgroundThread);
            contactTarget.EnableInput();
        }


        // This is a texture we can render.
        Texture2D ballTexture;


        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            
            ballTexture = Content.Load<Texture2D>("Ball");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        // Store some information about the sprite's motion.
        //Vector2 ballSpeed = Vector2.Negate(new Vector2(100.0f, 0f));

        private KeyboardState keyState;

        private static readonly float CIRCLE = MathHelper.Pi * 2;

        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == 
                ButtonState.Pressed)
                this.Exit();
            
            keyState = Keyboard.GetState();

            // Move the sprite around.
            UpdateSprite(gameTime);
            
            base.Update(gameTime);
        }


        private object bulletLock = new object();
   
        void UpdateSprite(GameTime gameTime) 
        {
            lock (bulletLock)
            {
                // Move the sprite by speed, scaled by elapsed time.
                foreach (var bullet in bullets)
                {
                    if (bullet != null)
                    {
                        var traveled = bullet.Velocity * (float)gameTime.ElapsedGameTime.TotalSeconds;

                        bullet.CurrentPosition += traveled;

                        bullet.DistanceTraveled += traveled.Length();

                        int MaxX =
                            graphics.GraphicsDevice.Viewport.Width - (int)player1.Width;
                        int MinX = 0;
                        int MaxY =
                            graphics.GraphicsDevice.Viewport.Height - (int)player1.Height;
                        int MinY = 0;

                        // Check for bounce.
                        if (bullet.X > MaxX)
                        {
                            bullet.ReverseX();
                            bullet.X = MaxX;
                        }

                        else if (bullet.X < MinX)
                        {
                            bullet.ReverseX();
                            bullet.X = MinX;
                        }

                        if (bullet.Y > MaxY)
                        {
                            bullet.ReverseY();
                            bullet.Y = MaxY;
                        }

                        else if (bullet.Y < MinY)
                        {
                            bullet.ReverseY();
                            bullet.Y = MinY;
                        }
                    }
                }

                bullets.RemoveAll(b => b.HasTraveledMaxDistance());
            }
        }

       


        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);

            // Draw the sprite.
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend);

            foreach(var bullet in bullets)
            {
                spriteBatch.Draw(ballTexture, bullet.CurrentPosition, Color.White);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
