﻿#region File Description
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
using Microsoft.Surface;

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

        private SpriteFont scoreFont;
        private SpriteFont gameOverFont;

        private Vector2 player1OriginalPosition;
        private Vector2 player2OriginalPosition;


        //private List<Bullet> bullets = new List<Bullet>();

        private Tank player1;
        private Tank player2;



        private string gameOverMessage = "{0} wins!  Now go submit a TI Idea to celebrate.";
        private TimeSpan? gameOverMessageDisplayed = null;




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





        private IEnumerable<Block> blocks;

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

            var center = new Vector2(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2);
            Board1 board = new Board1(this);


            blocks = board.GetObstacles();
            blocks.ForEach(b => Components.Add(b));

            player1OriginalPosition = new Vector2(graphics.GraphicsDevice.Viewport.Width - 100, graphics.GraphicsDevice.Viewport.Height / 2);
            player2OriginalPosition = new Vector2(100, graphics.GraphicsDevice.Viewport.Height / 2);

            ContactTarget.ContactAdded += new EventHandler<ContactEventArgs>(ContactTarget_ContactAdded);
            ContactTarget.ContactRemoved += new EventHandler<ContactEventArgs>(ContactTarget_ContactRemoved);
            ContactTarget.ContactChanged += new EventHandler<ContactEventArgs>(ContactTarget_ContactChanged);

            player1 = new Tank(this, player1OriginalPosition);
            player1.Name = "Player1";
            player1.Fired += TankFired;
            player1.Obstacles = blocks;
            player1Controller = new Controller(this, new Vector2(graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height));
            player1Controller.Tank = player1;
            player1Controller.Height *= 2;
            player1Controller.Width *= 2;
            player1Controller.TransformedCenter -= new Vector2(player1Controller.Width * 1.5f, player1Controller.Height / 2);

            player2 = new Tank(this, player2OriginalPosition);
            player2.Name = "Player2";
            player2.Rotation = MathHelper.ToRadians(180);
            player2.Fired += TankFired;
            player2.Obstacles = blocks;

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
            ApplicationLauncher.SignalApplicationLoadComplete();
            
        }

        private void TankFired(object sender, EventArgs<Tank> e)
        {
            var tank = e.EventData;

            if (Components.Where(c=>c is Bullet).Select(c => c as Bullet).Exists(b => b.Owner == tank))
            {
                return;
            }

            var velocity = new Vector2(-(float)Math.Cos(tank.Rotation),
                                         -(float)Math.Sin(tank.Rotation)) * 500.0f;
            var bullet = new Bullet(this, tank.TransformedCenter + velocity * .015f);

            bullet.Opponent = tank == player1 ? player2 : player1;
            bullet.Owner = tank;
            bullet.Obstacles = blocks;

            bullet.Velocity = velocity;

            Components.Add(bullet);
            
        }

        

        protected override void Update(GameTime gameTime)
        {
            if (GameOver())
            {
                if (gameOverMessageDisplayed == null)
                {
                    gameOverMessageDisplayed = TimeSpan.FromSeconds(5);
                    return;
                }

                if (gameOverMessageDisplayed.Value.TotalMilliseconds > 0)
                {
                    gameOverMessageDisplayed = gameOverMessageDisplayed.Value.Subtract(gameTime.ElapsedRealTime);
                }
                if (gameOverMessageDisplayed.Value.TotalMilliseconds < 0)
                {
                    ResetGame();
                }
                
                
                return;
            }
            base.Update(gameTime);
        }

        private void ResetGame()
        {
            var bullets = Components.Where(c => c is Bullet).ToList();
            bullets.ForEach(c => Components.Remove(c));

            player1.Reset(player1OriginalPosition, 0);
            player2.Reset(player2OriginalPosition, MathHelper.ToRadians(180));

            
        }

        private bool GameOver()
        {
            return player1.Score == 5 || player2.Score == 5;
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


        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            scoreFont = Content.Load<SpriteFont>("ScoreFont");
            gameOverFont = Content.Load<SpriteFont>("GameOverFont");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }




        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin();


            spriteBatch.DrawString(scoreFont, player1.Score.ToString(), new Vector2(graphics.GraphicsDevice.Viewport.Width - 200, 0), Color.Red);
            spriteBatch.DrawString(scoreFont, player2.Score.ToString(), new Vector2(200, 0), Color.Red);


            if (GameOver())
            { 
                var winner = GetWinner();
                var message = string.Format(gameOverMessage, winner.Name);
                spriteBatch.DrawString(gameOverFont, message, new Vector2(75, graphics.GraphicsDevice.Viewport.Height - 250), Color.Red);
            }


            spriteBatch.End();
           

            base.Draw(gameTime);
        }

        private Tank GetWinner()
        {
            return player1.Score == 5 ? player1 : player2;
        }
    }
}
