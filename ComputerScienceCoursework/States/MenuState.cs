using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Serialization;
using System.Diagnostics;
using ComputerScienceCoursework;
using ComputerScienceCoursework.States;
using ComputerScienceCoursework.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Myra.Graphics2D.UI;
using Myra;

namespace ComputerScienceCoursework.States
{
    public class MenuState : State
    {
        // list to hold all components in menu
        private List<Component> _components;
        // texture for mouse cursor
        private Texture2D _cursorTexture { get; set; }
        private Texture2D _backgroundTexture { get; set; }
        private int scroll_x = -50;
        private int scroll_y = -200;
        private bool fullscreen;
        private bool fpsCounter;
        public int screenWidth;
        public int screenHeight;
        GraphicsDeviceManager graphics;
        private Desktop _mainmenu;
        private bool mainmenu;
        private Desktop _pregameMenu;
        private bool pregameMenu;
        private Desktop _createGameMenu;
        private bool createGameMenu;
        private Desktop _settingsMenu;
        private bool settingsMenu;

        public enum menuState
        {
            mainmenu, pregameMenu, createGameMenu, settingsMenu, ingame
        }
        menuState state = menuState.mainmenu;

        public menuState State
        {
            get { return state; }
            set
            {
                state = value;
            }
        }

        public string temporaryVar2 = "";

        // construct state
        public MenuState(GameInstance game, GraphicsDevice graphicsDevice, ContentManager content) : base(game, graphicsDevice, content)
        {
            // variables to hold button texture and font
            var buttonTexture = _content.Load<Texture2D>("Sprites/UI/UI_Button");
            var buttonFont = _content.Load<SpriteFont>("Fonts/Font_01");
            _backgroundTexture = _content.Load<Texture2D>("Sprites/Images/world_capture");

            if (File.Exists("settings.txt"))
            {
                string line;
                StreamReader sr = new StreamReader("settings.txt");
                //Read the first line of text
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
                    fpsCounter = true;
                }
                else
                {
                    fpsCounter = false;
                }
                sr.Close();
            }

            bool temporaryVar = fullscreen;
            bool temporaryVar3 = fpsCounter;
            string temporaryVar2 = "";

            MyraEnvironment.Game = game;
            var panel = new Panel();
            var mainMenuText = new Label();
            mainMenuText.Text = "Game";
            mainMenuText.HorizontalAlignment = HorizontalAlignment.Center;
            mainMenuText.Top = 40;
            panel.Widgets.Add(mainMenuText);
            var singlePlayerButton = new TextButton();
            singlePlayerButton.Text = "Singleplayer";
            singlePlayerButton.HorizontalAlignment = HorizontalAlignment.Center;
            singlePlayerButton.Top = 100;
            singlePlayerButton.Width = 120;
            singlePlayerButton.Height = 40;
            panel.Widgets.Add(singlePlayerButton);
            singlePlayerButton.Click += (s, a) =>
            {
                state = menuState.pregameMenu;
            };
            var settingsButton = new TextButton();
            settingsButton.Text = "Settings";
            settingsButton.HorizontalAlignment = HorizontalAlignment.Center;
            settingsButton.Top = 160;
            settingsButton.Width = 120;
            settingsButton.Height = 40;
            panel.Widgets.Add(settingsButton);
            settingsButton.Click += (s, a) =>
            {
                state = menuState.settingsMenu;
            };
            var exitButton = new TextButton();
            exitButton.Text = "Exit";
            exitButton.HorizontalAlignment = HorizontalAlignment.Center;
            exitButton.Top = 220;
            exitButton.Width = 120;
            exitButton.Height = 40;
            panel.Widgets.Add(exitButton);
            exitButton.Click += (s, a) =>
            {
                game.Exit();
            };
            // Add it to the desktop
            _mainmenu = new Desktop();
            _mainmenu.Root = panel;
            var panel2 = new Panel();
            var label2 = new Label();
            label2.Text = "Singleplayer";
            label2.HorizontalAlignment = HorizontalAlignment.Center;
            label2.Top = 40;
            panel2.Widgets.Add(label2);
            var createGameButton = new TextButton();
            createGameButton.Text = "Create new game";
            createGameButton.HorizontalAlignment = HorizontalAlignment.Center;
            createGameButton.Top = 100;
            createGameButton.Width = 120;
            createGameButton.Height = 40;
            panel2.Widgets.Add(createGameButton);
            createGameButton.Click += (s, a) =>
            {
                state = menuState.createGameMenu;
            };
            var loadGameButton = new TextButton();
            loadGameButton.Text = "Load game";
            loadGameButton.HorizontalAlignment = HorizontalAlignment.Center;
            loadGameButton.Top = 160;
            loadGameButton.Width = 120;
            loadGameButton.Height = 40;
            panel2.Widgets.Add(loadGameButton);
            loadGameButton.Click += (s, a) =>
            {
                _game.ChangeState(new GameState(_game, _graphicsDevice, _content, true, 4));
            };
            var backButton = new TextButton();
            backButton.Text = "Back";
            backButton.HorizontalAlignment = HorizontalAlignment.Center;
            backButton.Top = 220;
            backButton.Width = 120;
            backButton.Height = 40;
            panel2.Widgets.Add(backButton);
            backButton.Click += (s, a) =>
            {
                state = menuState.mainmenu;
            };
            // Add it to the desktop
            _pregameMenu = new Desktop();
            _pregameMenu.Root = panel2;
            var createGamePanel = new Panel();
            var createGameLabel = new Label();
            createGameLabel.Text = "Create game";
            createGameLabel.HorizontalAlignment = HorizontalAlignment.Center;
            createGameLabel.Top = 40;
            createGamePanel.Widgets.Add(createGameLabel);


            var mapSelectionNote = new Label();
            mapSelectionNote.Text = "Select the map:";
            mapSelectionNote.Left = 100;
            mapSelectionNote.Top = 160;
            createGamePanel.Widgets.Add(mapSelectionNote);
            var mapSelectionComboBox = new ComboBox();
            mapSelectionComboBox.HorizontalAlignment = HorizontalAlignment.Center;
            mapSelectionComboBox.Top = 160;
            mapSelectionComboBox.Width = 120;
            mapSelectionComboBox.Height = 40;
            mapSelectionComboBox.Items.Add(new ListItem("Grassy plains"));
            mapSelectionComboBox.SelectedIndex = 0;
            mapSelectionComboBox.Items.Add(new ListItem("Forest"));
            mapSelectionComboBox.Items.Add(new ListItem("Lake world"));
            mapSelectionComboBox.Items.Add(new ListItem("Marshes"));
            createGamePanel.Widgets.Add(mapSelectionComboBox);

            var createGameButton2 = new TextButton();
            createGameButton2.Text = "Create Game";
            createGameButton2.HorizontalAlignment = HorizontalAlignment.Center;
            createGameButton2.Top = 220;
            createGameButton2.Width = 120;
            createGameButton2.Height = 40;
            createGamePanel.Widgets.Add(createGameButton2);
            createGameButton2.Click += (s, a) =>
            {
                int tempval =  Convert.ToInt32(mapSelectionComboBox.SelectedIndex);
                _game.ChangeState(new GameState(_game, _graphicsDevice, _content, true, tempval));
            };
            var backButtonCreate = new TextButton();
            backButtonCreate.Text = "Back";
            backButtonCreate.HorizontalAlignment = HorizontalAlignment.Center;
            backButtonCreate.Top = 280;
            backButtonCreate.Width = 120;
            backButtonCreate.Height = 40;
            createGamePanel.Widgets.Add(backButtonCreate);
            backButtonCreate.Click += (s, a) =>
            {
                state = menuState.pregameMenu;
            };
            // Add it to the desktop
            _createGameMenu = new Desktop();
            _createGameMenu.Root = createGamePanel;
            var panel3 = new Panel();
            var label3 = new Label();
            label3.Text = "Settings";
            label3.HorizontalAlignment = HorizontalAlignment.Center;
            label3.Top = 40;
            panel3.Widgets.Add(label3);
            var fullscreenButton = new TextButton();
            fullscreenButton.Text = "Fullscreen: " + temporaryVar;
            fullscreenButton.HorizontalAlignment = HorizontalAlignment.Center;
            fullscreenButton.Top = 100;
            fullscreenButton.Width = 120;
            fullscreenButton.Height = 40;
            panel3.Widgets.Add(fullscreenButton);
            fullscreenButton.Click += (s, a) =>
            {
                temporaryVar ^= true;
                fullscreenButton.Text = "Fullscreen: " + temporaryVar;
            };
            var resolutionNote = new Label();
            resolutionNote.Text = "Resolution set automatically \non fullscreen mode";
            resolutionNote.Left = 0;
            resolutionNote.Top = 160;
            panel3.Widgets.Add(resolutionNote);
            var resolutionSizeComboBox = new ComboBox();
            resolutionSizeComboBox.HorizontalAlignment = HorizontalAlignment.Center;
            resolutionSizeComboBox.Top = 160;
            resolutionSizeComboBox.Width = 120;
            resolutionSizeComboBox.Height = 40;
            resolutionSizeComboBox.Items.Add(new ListItem("Don't Change"));
            resolutionSizeComboBox.SelectedIndex = 0;
            resolutionSizeComboBox.Items.Add(new ListItem("800*600"));
            resolutionSizeComboBox.Items.Add(new ListItem("1024*768"));
            resolutionSizeComboBox.Items.Add(new ListItem("1280*720"));
            resolutionSizeComboBox.Items.Add(new ListItem("1280*1024"));
            resolutionSizeComboBox.Items.Add(new ListItem("1366*768"));
            resolutionSizeComboBox.Items.Add(new ListItem("1440*900"));
            resolutionSizeComboBox.Items.Add(new ListItem("1600*900"));
            resolutionSizeComboBox.Items.Add(new ListItem("1920*1080"));
            resolutionSizeComboBox.Items.Add(new ListItem("1920*1200"));
            panel3.Widgets.Add(resolutionSizeComboBox);

            var fpsButton = new TextButton();
            fpsButton.Text = "FPS Counter on: " + temporaryVar3;
            fpsButton.HorizontalAlignment = HorizontalAlignment.Center;
            fpsButton.Top = 220;
            fpsButton.Width = 120;
            fpsButton.Height = 40;
            panel3.Widgets.Add(fpsButton);
            fpsButton.Click += (s, a) =>
            {
                temporaryVar3 ^= true;
                fpsButton.Text = "FPS Counter on: " + temporaryVar3;
            };

            var saveSettingsButton = new TextButton();
            saveSettingsButton.Text = "Save Settings";
            saveSettingsButton.HorizontalAlignment = HorizontalAlignment.Right;
            saveSettingsButton.VerticalAlignment = VerticalAlignment.Bottom;
            saveSettingsButton.Width = 120;
            saveSettingsButton.Height = 40;
            panel3.Widgets.Add(saveSettingsButton);
            saveSettingsButton.TouchDown += (s, a) =>
            {
                Dialog dialog = new Dialog
                {
                    Title = "Do you want to save the settings"
                };
                var stackPanel = new HorizontalStackPanel
                {
                    Spacing = 8
                };
                stackPanel.Proportions.Add(new Proportion
                {
                    Type = ProportionType.Auto,
                });
                stackPanel.Proportions.Add(new Proportion
                {
                    Type = ProportionType.Fill,
                });
                var warningSettings = new Label
                {
                    Text = "Warning:After changing settings you must restart the game to see an affect"
                };
                stackPanel.Widgets.Add(warningSettings);
                dialog.Content = stackPanel;
                dialog.Closed += (t, b) => {
                    if (!dialog.Result)
                    {
                        return;
                    }
                    StreamWriter sw = new StreamWriter("settings.txt");
                    sw.WriteLine(temporaryVar.ToString().ToLower());
                    temporaryVar2 = resolutionSizeComboBox.SelectedItem.Text;
                    if (temporaryVar2 != "Don't Change")
                    {
                        sw.WriteLine(resolutionSizeComboBox.SelectedItem);
                    }
                    else
                    {
                        sw.WriteLine(screenWidth + "*" + screenHeight);
                    }
                    sw.WriteLine(temporaryVar3.ToString().ToLower());
                    sw.Close();

                };
                dialog.ShowModal(_settingsMenu);
            };
            var backButtonSettings = new TextButton();
            backButtonSettings.Text = "Back";
            backButtonSettings.HorizontalAlignment = HorizontalAlignment.Left;
            backButtonSettings.VerticalAlignment = VerticalAlignment.Bottom;
            backButtonSettings.Width = 120;
            backButtonSettings.Height = 40;
            panel3.Widgets.Add(backButtonSettings);
            backButtonSettings.Click += (s, a) =>
            {
                temporaryVar = fullscreen;
                fullscreenButton.Text = "Fullscreen: " + temporaryVar;
                temporaryVar3 = fpsCounter;
                fpsButton.Text = "FPS Counter on: " + temporaryVar3;
                state = menuState.mainmenu;
            };
            // Add it to the desktop
            _settingsMenu = new Desktop();
            _settingsMenu.Root = panel3;


            // set mouse position
            Mouse.SetPosition(_graphicsDevice.Viewport.Width / 2, _graphicsDevice.Viewport.Height / 2);
            // load (mouse) cursor content
            _cursorTexture = _content.Load<Texture2D>("Sprites/UI/UI_Cursor");
        }


        // draw state
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            spriteBatch.Draw(_backgroundTexture, new Vector2(scroll_x, scroll_y), Color.White);
            spriteBatch.End();
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            if (State == menuState.mainmenu)
            {
                _mainmenu.Render();
            }
            if (State == menuState.pregameMenu)
            {
                _pregameMenu.Render();
            }
            if (State == menuState.createGameMenu)
            {
                _createGameMenu.Render();
            }
            if (State == menuState.settingsMenu)
            {
                _settingsMenu.Render();
            }
            var msp = Mouse.GetState().Position;
            var mp = new Vector2(msp.X, msp.Y);
            spriteBatch.Draw(_cursorTexture, mp, Color.White);
            spriteBatch.End();

        }
        public override void PostUpdate(GameTime gameTime)
        {
            
        }
        public override void Update(GameTime gameTime)
        {
            // update each component
            if (State == menuState.mainmenu)
            {
                _mainmenu.Render();
            }
            if (State == menuState.pregameMenu)
            {
                _pregameMenu.Render();
            }
            if (State == menuState.createGameMenu)
            {
                _createGameMenu.Render();
            }
            if (State == menuState.settingsMenu)
            {
                _settingsMenu.Render();
            }
        }
    }
}
