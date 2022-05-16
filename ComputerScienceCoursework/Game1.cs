﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ComputerScienceCoursework.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Myra.Graphics2D.UI;
using Myra;
using System;
using System.IO;
using System.Xml.Serialization;

namespace ComputerScienceCoursework
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class GameInstance : Game
    {
        FPS_Counter fpsCounter;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        private State _currentState;
        private State _nextState;
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;

        private SoundEffect ClickSound;
        private SoundEffect DestroySound;
        public bool fullscreen;
        public bool fpsCounterSetting;
        public int screenWidth;
        public int screenHeight;

        public string temporaryVar2 = "";

        public GameInstance()
        {
            if (File.Exists("settings.txt"))
            {
                string line;
                StreamReader sr = new StreamReader("settings.txt");
                line = sr.ReadLine();
                if (line == "true")
                {
                    fullscreen = true;
                }
                else
                {
                    fullscreen = false;
                }
                line = sr.ReadLine();
                string[] resolution = line.Split('*');
                screenWidth = Convert.ToInt32(resolution[0]);
                screenHeight = Convert.ToInt32(resolution[1]);
                line = sr.ReadLine();
                if (line == "true")
                {
                    fpsCounterSetting = true;
                }
                else
                {
                    fpsCounterSetting = false;
                }
                sr.Close();
            }
            else
            {
                StreamWriter sw = new StreamWriter("settings.txt");
                //Write a line of text
                sw.WriteLine("true");
                fullscreen = true;
                //Write a second line of text
                sw.WriteLine("0*0");
                sw.WriteLine("true");
                fpsCounterSetting = true;
                //Close the file
                sw.Close();
            }

            graphics = new GraphicsDeviceManager(this);
            if (fullscreen == true) //If the settings say it is fullscreen
            {
                graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;   //Sets width and height to the screensize
                graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
                graphics.IsFullScreen = true;               //Fullscreen
            }
            else
            {
                graphics.PreferredBackBufferWidth = screenWidth;   // set this value to the desired width of your window
                graphics.PreferredBackBufferHeight = screenHeight;   // set this value to the desired height of your window
                graphics.IsFullScreen = false;               //set the GraphicsDeviceManager's fullscreen property
            }
            graphics.ApplyChanges();
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
            base.Initialize();
        }
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            _currentState = new MenuState(this, GraphicsDevice, Content);
            ClickSound = Content.Load<SoundEffect>("Sounds/FX/Click");
            DestroySound = Content.Load<SoundEffect>("Sounds/FX/Poof");

            fpsCounter = new FPS_Counter(spriteBatch, Content);
            fpsCounter.LoadContent(Content);
            
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            _previousMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();
            if (!(_currentState is MenuState))
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                    Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    if (_currentState is GameState state) Task.Run(() => state.SaveGame());
                    _nextState = new MenuState(this, GraphicsDevice, Content);
                }
            }
            if (_currentMouseState.LeftButton == ButtonState.Released &&
                _previousMouseState.LeftButton == ButtonState.Pressed)
            {
                if (IsMouseInsideWindow())
                {
                    var mk_snd = true;
                    if (_currentState is GameState s) { if (!s.IsLoaded) mk_snd = false; }
                    if (mk_snd.Equals(true)) ClickSound.Play(0.2f, -0.3f, 0.0f);
                }
            }
            if (_nextState != null)
            {
                _currentState = _nextState;
                _nextState = null;
                if (_currentState is MenuState)
                {
                    //
                }
                else if (_currentState is GameState cs)
                {
                    cs.ObjectDestroyed += GameState_ObjectDestroyed;
                }
            }
            _currentState.Update(gameTime);
            _currentState.PostUpdate(gameTime);
            base.Update(gameTime);
        }
        private void GameState_ObjectDestroyed(object sender, EventArgs e)
        {
            DestroySound.Play(0.2f, -0.3f, 0.0f);
        }
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _currentState.Draw(gameTime, spriteBatch);


            base.Draw(gameTime);
            if (fpsCounterSetting == true)
            {
                fpsCounter.Draw(gameTime, spriteBatch);
            }
        }
        public void ChangeState(State state)
        {
            _nextState = state;
        }
        private bool IsMouseInsideWindow()
        {
            MouseState ms = Mouse.GetState();
            Point pos = new Point(ms.X, ms.Y);
            return GraphicsDevice.Viewport.Bounds.Contains(pos);
        }
    }
    public class FPS_Counter
    {
        private SpriteFont _font;
        private float _fps = 0;
        private float _totalTime;
        private float _displayFPS;
        public FPS_Counter(SpriteBatch batch, ContentManager content)
        {
            this._totalTime = 0f;
            this._displayFPS = 0f;
        }
        public void LoadContent(ContentManager content)
        {
            this._font = content.Load<SpriteFont>("Fonts/Font_01");
        }
        public void Draw(GameTime gameTime, SpriteBatch batch)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            _totalTime += elapsed;
            if (_totalTime >= 1000)
            {
                _displayFPS = _fps;
                _fps = 0;
                _totalTime = 0;
            }
            _fps++;
            batch.Begin();
            batch.DrawString(this._font, this._displayFPS.ToString() + " FPS", new Vector2(10, 10), Color.White);
            batch.End();
        }
    }
}