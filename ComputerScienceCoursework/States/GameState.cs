﻿using ComputerScienceCoursework.Content;
using ComputerScienceCoursework.Objects;
using ComputerScienceCoursework.UI;
using Comora;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Component = ComputerScienceCoursework.UI.Component;

namespace ComputerScienceCoursework.States
{
    public class GameStateData
    {
        // time data
        public int Day { get; set; } = 1;
        public int Year { get { return Day / 365; } }
        public float DayTime { get; set; } = 0f;
        // player inventory data
        public Inventory PlayerInventory { get; set; } = new Inventory();
        // map data
        public List<TileData> TileData { get; set; } = new List<TileData>();
    }

    public class GameState : State
    {
        #region PROPS

        private bool _debug { get; set; } = false;
        private Random _rndGen = new Random(); //used for deciding the random texture for a residential building

        private GraphicsDevice _graphicsDevice { get; set; }


        // if the game is currently saving
        protected bool IsSaving = false;

        // how much of the load pool is remaining (gets subtracted from with every milestone)
        private int _remainingLoad = 100;

        public int LoadProgress
        {
            get { return 100 - _remainingLoad; }
        }

        // is the game loaded?
        public bool IsLoaded
        {
            get { return LoadProgress.Equals(100); }
        }

        // is the game currently loading?
        public bool IsLoading = false;

        public string LoadingText { get; set; } = "Loading Game...";

        public Rectangle LoadingBar { get; set; }

        public Texture2D LoadingTexture { get; set; }
        public Texture2D LoadingCellTexture { get; set; }

        public Texture2D LoadingScreen_MapCellTexture { get; set; }
        public Texture2D LoadingScreen_MapCellHighlightedTexture { get; set; }
        public Vector2 LoadingScreen_CurrentCell { get; set; }
        public List<Vector2> LoadingScreen_HighlightedCells { get; set; } = new List<Vector2>();


        #region GAME CONTENT
        // gamecontent manager - holds all sprites, effects, sounds
        private GameContent _gameContent { get; set; }

        // texture for mouse cursor
        private Texture2D _cursorTexture { get; set; }

        // font for writing text
        private SpriteFont _font { get; set; }
        #endregion

        #region MAP AND CAMERA
        // current map rendering
        private Map _currentMap { get; set; }

        public Map CurrentMap => _currentMap;

        // game camera
        private Camera _camera { get; set; }
        #endregion

        #region MOUSE & KEYBOARD STATES
        // previous mouse state (before current)
        private MouseState _previousMouseState { get; set; }
        #endregion

        #region EXTRA PROPERTIES
        // first render?
        private bool _firstTake { get; set; } = true;
        #endregion

        #region GAME STATE DATA

        public GameStateData GSData { get; set; }

        private const float _timeCycleDelay = 8; // seconds
        private float _remainingDelay = _timeCycleDelay;

        // some extra building information
        private Vector2 _townHallIndex = Vector2.Zero;

        public Tile CurrentlySelectedTile { get; set; }

        public Tile CurrentlyPressedTile { get; set; } = null;

        public Tile RoadStartTile { get; set; } = null;

        public int RoadPlacementCount { get; set; } = 0;

        public EventHandler ObjectDestroyed;

        #endregion

        #region COMPONENTS
        public List<Component> Components { get; set; } = new List<Component>();

        public HUD GameHUD { get; set; }

        public TileObject SelectedObject { get; set; } = new TileObject();

        public Tile CurrentlyHoveredTile { get; set; }

        public Button DeleteBldgButton { get; set; }
        private const float _deleteButtonDelay = 10; // seconds
        private Tile DeleteBldgQueue { get; set; }
        #endregion

        #endregion

        #region MAP PROPS
        private int _mapBounds = 75;

        private List<TileData> _tileData { get; set; }
        private string mapGen = "";
        #endregion
        public GameState(GameInstance game, GraphicsDevice graphicsDevice, ContentManager content, bool newgame, Int32 mapNo) : base(game, graphicsDevice, content)
        {
            SharedConstruction(graphicsDevice, content);

            if (newgame is true)
            {
                Console.WriteLine($"Starting new game...");
                Task.Run(() => CreateGame());
            }

            if (mapNo == 0) mapGen = "grassy_plains";
            else if (mapNo == 1) mapGen = "forest";
            else if (mapNo == 2) mapGen = "lake_world";
            else if (mapNo == 3) mapGen = "marshes";
            else if (mapNo == 4) mapGen = "GAMEDATA";
            // generate gamestate data for new game || load gamestate data from previous game
            InitGameStateData();
        }

        // a method that can be called from both constructors to run general startup logic
        public void SharedConstruction(GraphicsDevice graphicsDevice, ContentManager content)
        {
            // create new gamecontent instance
            _gameContent = new GameContent(content);

            // save graphics device
            _graphicsDevice = graphicsDevice;

            LoadLoadingScreen();

            if (_remainingLoad < 100) _remainingLoad = 100;

            // load (mouse) cursor content
            _cursorTexture = _gameContent.GetUiTexture(4);

            // create camera instance and set its position to mid map
            _camera = new Camera(graphicsDevice);

            // load the hud in a separate thread
            Task.Run(() => LoadHUD());
        }

        public void LoadLoadingScreen()
        {
            var loading_bar_dimensions = new Vector2(_graphicsDevice.Viewport.Width / 2, _graphicsDevice.Viewport.Height / 8);
            var loading_bar_location =
                new Vector2(_graphicsDevice.Viewport.Width / 4, (_graphicsDevice.Viewport.Height / 4) * 2.5f);

            LoadingBar = new Rectangle((int)loading_bar_location.X, (int)loading_bar_location.Y, (int)loading_bar_dimensions.X, (int)loading_bar_dimensions.Y);

            LoadingTexture = new Texture2D(_graphicsDevice, 1, 1);
            LoadingTexture.SetData(new[] { Color.DarkSlateGray });

            LoadingCellTexture = new Texture2D(_graphicsDevice, 1, 1);
            LoadingCellTexture.SetData(new[] { Color.LightCyan });

            LoadingScreen_MapCellTexture = new Texture2D(_graphicsDevice, 1, 1);
            LoadingScreen_MapCellTexture.SetData(new[] { Color.WhiteSmoke });
            LoadingScreen_MapCellHighlightedTexture = new Texture2D(_graphicsDevice, 1, 1);
            LoadingScreen_MapCellHighlightedTexture.SetData(new[] { Color.Red });
        }

        public async void LoadHUD()
        {
            GameHUD = new HUD(_graphicsDevice, _gameContent, _game);
            Components.Add(GameHUD);

            var welcome_txt =
                "After the first day processess, you will have some money to begin constructing \n" +
                "some buildings. Start with some houses to gain workers, so you can then construct \n" +
                "buildings that provide resources. Good luck, and make sure to monitor your \n" +
                "resource incomes so that they don't fall negative! You can monitor per-resource \n" +
                "gains / per day by hovering over a resource in the HUD's middle resource bar menu.";

            var welcome_dialog_window = new DialogWindow(welcome_txt, new Vector2(100, 100), _graphicsDevice,
                _gameContent.GetFont(1), _gameContent);

            Components.Add(welcome_dialog_window);
        }

        public void InitGameStateData()
        {
            GSData = new GameStateData();
            GSData.PlayerInventory = new Inventory();
        }

        public async void CreateGame()
        {
            LoadingText = $"Loading map...";

            await GenerateMap();

            LoadingText = $"Wrapping things up...";

            _remainingLoad -= 100;
        }

        private void PlayerInventoryOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine($"{sender.GetType().Name} has changed a property in the inventory");
        }

        public async Task<bool> GenerateMap()
        {
            // array to hold tiles
            
            Tile[,] tileArr_ = new Tile[_mapBounds, _mapBounds];
            string data = null;

            try
            {
                LoadingText = $"Reading save files...";
                // try to read map data from data_map.json file
                using (var streamReader = new System.IO.StreamReader($""+mapGen+".json"))
                {
                    data = streamReader.ReadToEnd();
                    streamReader.Close();
                }
                // if the data read isn't null or empty, load the map | else, throw exception
                if (string.IsNullOrEmpty(data).Equals(true))
                {
                    LoadingText = $"Map data corrupted... one moment...";
                    throw new NotSupportedException("Error Reading Map Data: Data is empty.");
                }
                else
                {
                    Console.WriteLine($"Loading map...");

                    // deserialize data to gamestate data object
                    GameStateData loaded_GSData = JsonConvert.DeserializeObject<GameStateData>(data);
                    // set gamestate data
                    GSData = loaded_GSData;
                    GSData.PlayerInventory = loaded_GSData.PlayerInventory;
                    GSData.TileData = loaded_GSData.TileData;

                    // get tiledata from gamestate data to List of _tileData
                    List<TileData> tdList_ = loaded_GSData.TileData;

                    LoadingText = $"Putting things back together...";
                    // for each _tileData loaded
                    foreach (TileData t in tdList_)
                    {
                        // get x and y index
                        var x = (int)t.TileIndex.X;
                        var y = (int)t.TileIndex.Y;

                        // create new tile and pass gamecontent instance and _tileData
                        tileArr_[x, y] = new Tile(_gameContent, _graphicsDevice, t);
                        tileArr_[x, y].Click += Tile_OnClick;
                        tileArr_[x, y].Pressed += Tile_OnPressed;
                        tileArr_[x, y].Pressing += Tile_CurrentlyPressed;

                        // set the camera's position if this tile is a townhall
                        if (tileArr_[x, y].Object.TypeId.Equals(2) && tileArr_[x, y].Object.ObjectId.Equals(10))
                            _camera.Position = tileArr_[x, y].Position + new Vector2(0, 100);
                    }

                    Console.WriteLine("Map restored - tile count: " + tileArr_.Length);
                }

                LoadingText = $"Looping through map data completed...";
                // create new map instance from loaded data
                _currentMap = new Map(tileArr_, _mapBounds, _mapBounds, 34, 100, _gameContent);

                GSData.PlayerInventory.PropertyChanged += PlayerInventoryOnPropertyChanged;

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error Loading Map Data: {e.Message}.");
                return false;
            }

        }

        // update
        public override void Update(GameTime gameTime)
        {
            if (IsLoaded)
            {
                // game state is loaded

                // get current keyboard state
                var keyboardState = Keyboard.GetState();
                var mouseState = Mouse.GetState();

                // handle current state input (keyboard / mouse)
                HandleInput(gameTime, keyboardState, mouseState);
                _previousMouseState = Mouse.GetState();
            }
            else
            {
                // game state is still loading
            }
        }

        // post update (called after update)
        public override void PostUpdate(GameTime gameTime)
        {
            CurrentlyHoveredTile = null;

            if (IsLoaded)
            {
                // game state is loaded

                // get keyboard state
                var keyboardState = Keyboard.GetState();

                // update map and camera
                try
                {
                    _currentMap.Update(gameTime, keyboardState, _camera, this);
                }
                catch (Exception e)
                {
                    //Console.WriteLine("Error drawing map: " + e.Message);
                }

                // if currently hovering a tile (do logic for setting radius highlight for currently selected building, before placing)
                if (!(CurrentlyHoveredTile is null))
                {

                    // if an object (bld) has been selected from the build menu
                    if (SelectedObject.ObjectId > 0 && RoadStartTile is null)
                    {
                        var sel_obj = SelectedObject;
                        // ensure the object selected is a building
                        if (sel_obj.TypeId.Equals(2))
                        {
                            // get the respective building and it's range
                            var bldg = (Building)SelectedObject;
                            var rng = bldg.Range;

                            // for each tile within the buildings range
                            for (int x = ((int)CurrentlyHoveredTile.TileIndex.X - rng); x < (CurrentlyHoveredTile.TileIndex.X + (rng + 1)); x++)
                            {
                                for (int y = ((int)CurrentlyHoveredTile.TileIndex.Y - rng); y < (CurrentlyHoveredTile.TileIndex.Y + (rng + 1)); y++)
                                {
                                    // set the tile as glowing (part of the building's *potential* radius, if placed at the currently hovered tile)
                                    try
                                    {
                                        _currentMap.Tiles[x, y].IsGlowing = true;
                                    }
                                    catch (Exception exc)
                                    {
                                        Console.WriteLine($"Couldn't activate tile: {new Vector2(x, y)} | {exc.Message}");
                                    }
                                }
                            }
                        }
                    }
                    // else, if a road tile is selected and a "road start" tile has been selected by clicking on a tile but not letting go and dragging
                    // do some road drawing logic
                    else if (!(RoadStartTile is null) && !(CurrentlyPressedTile is null) && SelectedObject.ObjectId.Equals(Building.Road().ObjectId) && CurrentlyHoveredTile.TileIndex.Equals(CurrentlyPressedTile.TileIndex))
                    {
                        RoadPlacementCount = 0;

                        // get the x and y offsets from the currently pressed tile and the starting pressed tile
                        var x_offset = CurrentlyPressedTile.TileIndex.X - RoadStartTile.TileIndex.X;
                        var y_offset = CurrentlyPressedTile.TileIndex.Y - RoadStartTile.TileIndex.Y;

                        // calculate the highlighted tiles for drawing
                        if (x_offset >= 0)
                        {
                            for (int x = ((int)RoadStartTile.TileIndex.X);
                                x < ((int)RoadStartTile.TileIndex.X + x_offset + 1);
                                x++)
                            {
                                _currentMap.Tiles[x, (int)RoadStartTile.TileIndex.Y].IsPreviewingRoad =
                                    _currentMap.Tiles[x, (int)RoadStartTile.TileIndex.Y].Object.ObjectId <= 0 &&
                                    _currentMap.Tiles[x, (int)RoadStartTile.TileIndex.Y].IsVisible;
                                if (x.Equals((int)(RoadStartTile.TileIndex.X + x_offset)))
                                {
                                    if (y_offset >= 0)
                                    {
                                        for (int y = (int)(RoadStartTile.TileIndex.Y);
                                            y < (int)(RoadStartTile.TileIndex.Y + y_offset + 1);
                                            y++)
                                        {
                                            _currentMap.Tiles[x, y].IsPreviewingRoad =
                                                _currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                _currentMap.Tiles[x, y].IsVisible;
                                        }
                                    }
                                    else if (y_offset < 0)
                                    {
                                        for (int y = (int)(RoadStartTile.TileIndex.Y);
                                            y > (int)(RoadStartTile.TileIndex.Y + y_offset - 1);
                                            y--)
                                        {
                                            _currentMap.Tiles[x, y].IsPreviewingRoad =
                                                _currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                _currentMap.Tiles[x, y].IsVisible;
                                        }
                                    }
                                }
                            }
                        }
                        else if (x_offset < 0)
                        {
                            for (int x = ((int)RoadStartTile.TileIndex.X);
                                x > ((int)RoadStartTile.TileIndex.X + x_offset - 1);
                                x--)
                            {
                                _currentMap.Tiles[x, (int)RoadStartTile.TileIndex.Y].IsPreviewingRoad =
                                    _currentMap.Tiles[x, (int)RoadStartTile.TileIndex.Y].Object.ObjectId <= 0 &&
                                    _currentMap.Tiles[x, (int)RoadStartTile.TileIndex.Y].IsVisible;
                                if (x.Equals((int)(RoadStartTile.TileIndex.X + x_offset)))
                                {
                                    if (y_offset >= 0)
                                    {
                                        for (int y = (int)(RoadStartTile.TileIndex.Y);
                                            y < (int)(RoadStartTile.TileIndex.Y + y_offset + 1);
                                            y++)
                                        {
                                            _currentMap.Tiles[x, y].IsPreviewingRoad =
                                                _currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                _currentMap.Tiles[x, y].IsVisible;
                                        }
                                    }
                                    else if (y_offset < 0)
                                    {
                                        for (int y = (int)(RoadStartTile.TileIndex.Y);
                                            y > (int)(RoadStartTile.TileIndex.Y + y_offset - 1);
                                            y--)
                                        {
                                            _currentMap.Tiles[x, y].IsPreviewingRoad =
                                                _currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                _currentMap.Tiles[x, y].IsVisible;
                                        }
                                    }
                                }
                            }
                        }

                        RoadPlacementCount = _currentMap.Tiles.OfType<Tile>().ToList()
                            .FindAll(i => i.IsPreviewingRoad == true).Count();
                    }
                    else
                    {
                        RoadStartTile = null;
                    }
                }

                // also update ui components
                foreach (var c in Components)
                {
                    c.Update(gameTime, this);
                }

                // update the camera (comora)
                _camera.Update(gameTime);

                // update timer (day cycle)
                var timer = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _remainingDelay -= timer;
                // if timer elapsed (upd gsd)
                if (_remainingDelay <= 0)
                {
                    // update gamestate data and reset timer
                    Task.Run(() => UpdateGameState(gameTime));
                    _remainingDelay = _timeCycleDelay;
                }
            }
            else
            {
                // game state is still loading
            }
        }

        /// <summary>
        /// Asynchronous Method ran on Update when the daycycle timer/interval is surpassed
        /// Buildings are processed, the day is advanced, and the game is saved (every 10 days)
        /// </summary>
        /// <param name="gameTime"></param>
        public async void UpdateGameState(GameTime gameTime)
        {
            // cycle finished
            Console.WriteLine($"Advancing cycle to day: {GSData.Day}");

            // process all building in game
            ProcessBuildings();

            // advance day
            GSData.Day += 1;
            var day = GSData.Day;

            // if day is a multiple of 10, save the game
            if ((day % 10).Equals(0)) SaveGame();
        }

        public void ProcessBuildings()
        {
            // hold variables to calculate total amount of workers
            var total_workers = 0;

            // reset specific vals
            GSData.PlayerInventory.Workers = 0;
            GSData.PlayerInventory.Energy = 0;
            GSData.PlayerInventory.Food = 0;

            foreach (Tile t in _currentMap.Tiles)
            {
                t.IsVisible = false;
                t.TileData.IsVisible = false;
            }

            foreach (var t in _currentMap.Tiles)
            {
                // if the object ID is greater than 0 (is an object)
                if (t.Object.ObjectId > 0)
                {
                    if (t.Object.TypeId.Equals(1))
                    {
                        // resouce tile
                    }
                    else if (t.Object.TypeId.Equals(2))
                    {
                        // building tile

                        // if a resource is linked to this building's objectid, add that resource's output to this building's income
                        if (BuildingData.Dict_BuildingResourceLinkKeys.ContainsKey(t.Object.ObjectId))
                        {
                            // get the building default data from dict
                            Building obj = BuildingData.Dict_BuildingFromObjectID[t.Object.ObjectId];

                            // reset inventory values to default (before adding calculated resources from proxim)
                            t.Object.MoneyOutput = obj.MoneyOutput;
                            t.Object.WoodOutput = obj.WoodOutput;
                            t.Object.CoalOutput = obj.CoalOutput;
                            t.Object.IronOutput = obj.IronOutput;
                            t.Object.StoneOutput = obj.StoneOutput;
                            t.Object.WorkersOutput = obj.WorkersOutput;
                            t.Object.EnergyOutput = obj.EnergyOutput;
                            t.Object.FoodOutput = obj.FoodOutput;

                            // loop thru all tiles within range for resources to add to output
                            for (int x = ((int)t.TileIndex.X - obj.Range); x < (t.TileIndex.X + (obj.Range + 1)); x++)
                            {
                                for (int y = ((int)t.TileIndex.Y - obj.Range); y < (t.TileIndex.Y + (obj.Range + 1)); y++)
                                {
                                    if (!(_currentMap.Tiles[x, y].Object.ObjectId.Equals(t.Object.ObjectId) &&
                                          _currentMap.Tiles[x, y].Object.TypeId.Equals(t.Object.TypeId)))
                                    {
                                        // for each linked resource to this building
                                        foreach (var k in BuildingData.Dict_BuildingResourceLinkKeys[t.Object.ObjectId])
                                        {
                                            // if the current tile is the linked resource
                                            if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(k) && _currentMap.Tiles[x, y].Object.TypeId.Equals(1))
                                            {
                                                Console.WriteLine("Adding " + BuildingData.Dic_ResourceCollectionKeys[k][0]);
                                                // add the resource output to this buildings output
                                                switch (BuildingData.Dic_ResourceCollectionKeys[k][0])
                                                {
                                                    case "Wood":
                                                        Console.WriteLine(
                                                            $"Adding {(int)BuildingData.Dic_ResourceCollectionKeys[k][1]} wood to the building");
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object.WoodOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object.WoodOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        break;
                                                    case "Food":
                                                        Console.WriteLine(
                                                            $"Adding {(int)BuildingData.Dic_ResourceCollectionKeys[k][1]} Food to the building");
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object.FoodOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object.FoodOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        break;
                                                    case "Stone":
                                                        Console.WriteLine(
                                                            $"Adding {(int)BuildingData.Dic_ResourceCollectionKeys[k][1]} stone to the building");
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object.StoneOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object.StoneOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        break;
                                                    case "Coal":
                                                        Console.WriteLine(
                                                            $"Adding {(int)BuildingData.Dic_ResourceCollectionKeys[k][1]} coal to the building");
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object.CoalOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object.CoalOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        break;
                                                    case "Iron":
                                                        Console.WriteLine(
                                                            $"Adding {(int)BuildingData.Dic_ResourceCollectionKeys[k][1]} iron to the building");
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object.IronOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object.IronOutput += (int)BuildingData.Dic_ResourceCollectionKeys[k][1];
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        var make_light = false;
                        var light_rng = 0;
                        if (t.Object.ObjectId == Building.PowerLine().ObjectId)
                        {
                            make_light = true;
                            light_rng = Building.PowerLine().Range;
                        }
                        else if (t.Object.ObjectId == Building.TownHall().ObjectId)
                        {
                            make_light = true;
                            light_rng = Building.TownHall().Range;
                        }

                        if (make_light.Equals(true))
                        {
                            for (int x = ((int)t.TileIndex.X - light_rng); x < (t.TileIndex.X + (light_rng + 1)); x++)
                            {
                                for (int y = ((int)t.TileIndex.Y  - light_rng); y < (t.TileIndex.Y + (light_rng + 1)); y++)
                                {
                                    _currentMap.Tiles[x, y].IsVisible = true;
                                    _currentMap.Tiles[x, y].TileData.IsVisible = true;
                                }
                            }
                        }


                        GSData.PlayerInventory.Money -= t.Object.MoneyCost; GSData.PlayerInventory.Money += t.Object.MoneyOutput;
                        GSData.PlayerInventory.Wood -= t.Object.WoodCost; GSData.PlayerInventory.Wood += +t.Object.WoodOutput;
                        GSData.PlayerInventory.Coal -= t.Object.CoalCost; GSData.PlayerInventory.Coal += t.Object.CoalOutput;
                        GSData.PlayerInventory.Iron -= t.Object.IronCost; GSData.PlayerInventory.Iron += t.Object.IronOutput;
                        GSData.PlayerInventory.Stone -= t.Object.StoneCost; GSData.PlayerInventory.Stone += t.Object.StoneOutput;
                        GSData.PlayerInventory.Workers -= t.Object.WorkersCost;
                        GSData.PlayerInventory.Workers += t.Object.WorkersOutput;
                        GSData.PlayerInventory.Energy -= t.Object.EnergyCost; GSData.PlayerInventory.Energy += t.Object.EnergyOutput;
                        GSData.PlayerInventory.Food += t.Object.FoodOutput;

                        total_workers += t.Object.WorkersOutput;
                    }
                }
            }

            // add default incomes to inventory
            GSData.PlayerInventory.Workers += 20;
            total_workers += 20;
            GSData.PlayerInventory.Energy += 30;
            GSData.PlayerInventory.Food += 60;
            // subtract from food the amount of food per worker in total buildings
            Console.WriteLine($"Calc Food: {total_workers} * 2 = {2 * total_workers}");
            Console.WriteLine($"Food: {GSData.PlayerInventory.Food} - {2 * total_workers} = ");
            GSData.PlayerInventory.Food -= (2 * total_workers);
            Console.WriteLine($"{GSData.PlayerInventory.Food}");
        }

        public async void SaveGame()
        {
            if (IsSaving.Equals(true)) return;

            // create list to hold tile data
            GSData.TileData = new List<TileData>();

            // for each tile in current map,
            foreach (Tile t in _currentMap.Tiles)
            {
                // add its tile data to list
                GSData.TileData.Add(t.GetTileData());
            }

            try
            {
                // delete previous backups
                System.IO.File.Delete("GAMEDATA_BACKUP.json");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            // backup old map data first
            try
            {
                // change filename to backup format
                System.IO.File.Move($"GAMEDATA.json", "GAMEDATA_BACKUP.json");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error backing up previous map data: " + e.Message);
            }

            IsSaving = true;
            // get current data_map file
            using (var streamWriter = new System.IO.StreamWriter($"GAMEDATA.json"))
            {
                // overwrite data with list of _tileData
                streamWriter.WriteLine(JsonConvert.SerializeObject(GSData, Formatting.Indented));
                streamWriter.Close();
            }

            IsSaving = false;
            Console.WriteLine("Finished Saving Map.");
        }

        private void Tile_CurrentlyPressed(object sender, EventArgs e)
        {
            CurrentlyPressedTile = (Tile)sender;
        }

        private void Tile_OnPressed(object sender, EventArgs e)
        {
            CurrentlyPressedTile = (Tile)sender;
            var sel_obj = SelectedObject;
            if (sel_obj.ObjectId.Equals(Building.Road().ObjectId))
            {
                if (CurrentlyPressedTile.Object.ObjectId <= 0) RoadStartTile = (Tile)sender;
            }
        }

        private void Tile_OnClick(object sender, EventArgs e)
        {

            // get mouse data
            var msp = Mouse.GetState().Position;
            var mp = new Vector2(msp.X, msp.Y);
            var mr = new Rectangle(msp.X, msp.Y, 1, 1);
            if (mr.Intersects(GameHUD.DisplayRect)) return;

            // get selected object and clear it
            var sel_obj = SelectedObject;
            SelectedObject = new TileObject();

            // get the tile clicked on
            Tile t = (Tile)sender;
            Console.WriteLine($"Tile clicked: {t.TileIndex}");

            CurrentlySelectedTile = t;

            // reset building delete queue
            DeleteBldgQueue = null;

            // reset road starting tile
            var rst = RoadStartTile;
            RoadStartTile = null;

            // reset currently pressed tile
            var cpt = CurrentlyPressedTile;
            CurrentlyPressedTile = null;

            // try to place/construct a building
            try
            {
                // is the tile visibile?
                if (t.IsVisible.Equals(true))
                {
                    // does the tile have a valid objectid,
                    // does it's typeid match that of a building,
                    // and does the clicked tile not already have an object in it?
                    if (sel_obj.ObjectId > 0 && sel_obj.TypeId.Equals(2) && t.Object.ObjectId <= 0)
                    {
                        // get a correctly casted version of the selected obj
                        var obj = (Building)sel_obj;

                        // check for adjacent road if object is not a road\
                        if (BuildingData.Dict_BuildingFromObjectID[obj.ObjectId].RequiresRoad)
                        {
                            var f = t.GetNearbyRoads();
                            var adj_road_cnt = f.Count(b => b);
                            if (adj_road_cnt < 1)
                                throw new Exception("No Adjacent Road"); // deny
                        }

                        // does the building's objectid have a matching resource objectid linked to it?
                        // if so, only one building of this type can be within it's range.
                        if (BuildingData.Dict_BuildingResourceLinkKeys.ContainsKey(obj.ObjectId))
                        {
                            // for each tile within the buildings range (from the clicked tile's position)
                            for (int x = ((int)t.TileIndex.X - obj.Range);
                                x < (t.TileIndex.X + (obj.Range + 1));
                                x++)
                            {
                                for (int y = ((int)t.TileIndex.Y - obj.Range);
                                    y < (t.TileIndex.Y + (obj.Range + 1));
                                    y++)
                                {
                                    // if there is already of a building of the selected building's type within the range
                                    if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(obj.ObjectId) && _currentMap.Tiles[x, y].Object.TypeId.Equals(2))
                                        throw new Exception(
                                            "There is already a building of that type collecting resources in this area."); // deny

                                    // add logic for checking for available resources within the range HERE, not below
                                }
                            }
                        }
                        else if (obj.ObjectId.Equals(Building.Watermill().ObjectId))
                        {
                            // look for adjacent water tiles
                            var nearby_water = false;
                            for (int x = ((int)t.TileIndex.X - 1);
                                x < (t.TileIndex.X + (2));
                                x++)
                            {
                                for (int y = ((int)t.TileIndex.Y - 1);
                                    y < (t.TileIndex.Y + (2));
                                    y++)
                                {
                                    // if there is already of a building of the selected building's type within the range
                                    if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(Resource.Water().Object.ObjectId)
                                        && _currentMap.Tiles[x, y].Object.TypeId.Equals(Resource.Water().Object.TypeId)
                                        && _currentMap.Tiles[x, y].TerrainId.Equals(Resource.Water().TerrainId))

                                        nearby_water = true;
                                }
                            }

                            if (nearby_water != true)
                                throw new Exception(
                                    "This building requires an adjacent water tile to construct."); // deny
                        }

                        // check balance to see if player can afford building
                        bool canBuild = true;

                        int total_Money_for_roads = 0;

                        if (rst != null && obj.ObjectId.Equals(Building.Road().ObjectId))
                        {
                            var ref_obj = Building.Road();
                            var i = RoadPlacementCount;

                            Console.WriteLine($"Placing {i} roads...");

                            for (int j = 0; j < i; j++)
                            {
                                total_Money_for_roads += 5;
                            }

                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Money >= total_Money_for_roads;
                            if (canBuild.Equals(false)) throw new Exception("Can't afford to place!");
                        }
                        else
                        {
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Money >= obj.MoneyUpfront;
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Wood >= obj.WoodUpfront;
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Coal >= obj.CoalUpfront;
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Iron >= obj.IronUpfront;
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Stone >= obj.StoneUpfront;
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Workers >= obj.WorkersUpfront;
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Energy >= obj.EnergyUpfront;
                            if (canBuild.Equals(true)) canBuild = GSData.PlayerInventory.Food >= obj.FoodUpfront;
                            if (canBuild.Equals(false)) throw new Exception("Can't afford to place!");
                        }

                        // do random skin math
                        var applied_txt_index = obj.TextureIndex;

                        var is_residence = false;
                        // apply random skin for residential buildings
                        if (obj.ObjectId.Equals(Residence.LowHouse().ObjectId))
                        {
                            is_residence = true;
                            applied_txt_index = Enumerable.Range(0, 1)
                                .Select(r => new int[] { new int[] { 11, 18, 19 }[_rndGen.Next(3)], new int[] { 11, 18, 19 }[_rndGen.Next(3)], new int[] { 11, 18, 19 }[_rndGen.Next(3)] }[_rndGen.Next(3)]).First();
                            Console.WriteLine($"Applying random texture to low house: id{applied_txt_index}");
                        }
                        else if (obj.ObjectId.Equals(Residence.MedHouse().ObjectId))
                        {
                            is_residence = true;
                            applied_txt_index = Enumerable.Range(0, 1)
                                .Select(r => new int[] { new int[] { 20, 21, 22 }[_rndGen.Next(3)], new int[] { 20, 21, 22 }[_rndGen.Next(3)], new int[] { 20, 21, 22 }[_rndGen.Next(3)] }[_rndGen.Next(3)]).First();
                            Console.WriteLine($"Applying random texture to med house: id{applied_txt_index}");
                        }
                        else if (obj.ObjectId.Equals(Residence.EliteHouse().ObjectId))
                        {
                            is_residence = true;
                            applied_txt_index = Enumerable.Range(0, 1)
                                .Select(r => new int[] { new int[] { 23, 24, 25 }[_rndGen.Next(3)], new int[] { 23, 24, 25 }[_rndGen.Next(3)], new int[] { 23, 24, 25 }[_rndGen.Next(3)] }[_rndGen.Next(3)]).First();
                            Console.WriteLine($"Applying random texture to elite house: id{applied_txt_index}");
                        }
                        else if (obj.ObjectId.Equals(Residence.HighRiseHouse().ObjectId))
                        {
                            is_residence = true;
                            applied_txt_index = Enumerable.Range(0, 1)
                                .Select(r => new int[] { new int[] { 39, 40, 41 }[_rndGen.Next(3)], new int[] { 39, 40, 41 }[_rndGen.Next(3)], new int[] { 39, 40, 41 }[_rndGen.Next(3)] }[_rndGen.Next(3)]).First();
                            Console.WriteLine($"Applying random texture to High Rise house: id{applied_txt_index}");
                        }
                        else if (obj.ObjectId.Equals(Building.Road().ObjectId))
                        {
                            applied_txt_index = _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y]
                                .DecideTextureID_NearbyRoadsFactor();
                        }

                        // if residence, do apply-resident logic
                        if (is_residence)
                        {
                            Residence res_obj = (Residence)sel_obj;
                            //foreach (var res in res_obj.Residents)
                            //{
                                // generate name for resident
                                //res.Name = Resident.RandomNameList.OrderBy(x => _rndGen.Next()).FirstOrDefault();
                            //}

                            // apply residence to tile object
                            _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object = res_obj;
                            _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object = res_obj;
                        }
                        else
                        {
                            // apply building to tile object
                            _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object = obj;
                            _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object = obj;
                        }

                        // apply texture data to object 
                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].ObjectTexture = _gameContent.GetTileTexture(applied_txt_index);
                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object.TextureIndex =
                            applied_txt_index;
                        _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object.TextureIndex =
                            applied_txt_index;

                        // apply roads logic
                        if (rst != null && obj.ObjectId.Equals(Building.Road().ObjectId))
                        {

                            // get the x and y offsets from the currently pressed tile and the starting pressed tile
                            var x_offset = cpt.TileIndex.X - rst.TileIndex.X;
                            var y_offset = cpt.TileIndex.Y - rst.TileIndex.Y;

                            // calculate the highlighted tiles for drawing
                            if (x_offset >= 0)
                            {
                                for (int x = ((int)rst.TileIndex.X);
                                    x < ((int)rst.TileIndex.X + x_offset + 1);
                                    x++)
                                {
                                    _currentMap.Tiles[x, (int)rst.TileIndex.Y].IsPreviewingRoad = false;
                                    if (_currentMap.Tiles[x, (int)rst.TileIndex.Y].Object.ObjectId <= 0 &&
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].IsVisible)
                                    {
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].Object = obj;
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].TileData.Object = obj;
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].ObjectTexture = _gameContent.GetTileTexture(applied_txt_index);
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].Object.TextureIndex =
                                            applied_txt_index;
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].TileData.Object.TextureIndex =
                                            applied_txt_index;
                                    }
                                    if (x.Equals((int)(rst.TileIndex.X + x_offset)))
                                    {
                                        if (y_offset >= 0)
                                        {
                                            for (int y = (int)(rst.TileIndex.Y);
                                                y < (int)(rst.TileIndex.Y + y_offset + 1);
                                                y++)
                                            {
                                                _currentMap.Tiles[x, y].IsPreviewingRoad = false;
                                                if (_currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                    _currentMap.Tiles[x, y].IsVisible)
                                                {
                                                    _currentMap.Tiles[x, y].Object = obj;
                                                    _currentMap.Tiles[x, y].TileData.Object = obj;
                                                    _currentMap.Tiles[x, y].ObjectTexture = _gameContent.GetTileTexture(applied_txt_index);
                                                    _currentMap.Tiles[x, y].Object.TextureIndex =
                                                        applied_txt_index;
                                                    _currentMap.Tiles[x, y].TileData.Object.TextureIndex =
                                                        applied_txt_index;
                                                }
                                            }
                                        }
                                        else if (y_offset < 0)
                                        {
                                            for (int y = (int)(rst.TileIndex.Y);
                                                y > (int)(rst.TileIndex.Y + y_offset - 1);
                                                y--)
                                            {
                                                _currentMap.Tiles[x, y].IsPreviewingRoad = false;
                                                if (_currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                    _currentMap.Tiles[x, y].IsVisible)
                                                {
                                                    _currentMap.Tiles[x, y].Object = obj;
                                                    _currentMap.Tiles[x, y].TileData.Object = obj;
                                                    _currentMap.Tiles[x, y].ObjectTexture = _gameContent.GetTileTexture(applied_txt_index);
                                                    _currentMap.Tiles[x, y].Object.TextureIndex =
                                                        applied_txt_index;
                                                    _currentMap.Tiles[x, y].TileData.Object.TextureIndex =
                                                        applied_txt_index;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (x_offset < 0)
                            {
                                for (int x = ((int)rst.TileIndex.X);
                                    x > ((int)rst.TileIndex.X + x_offset - 1);
                                    x--)
                                {
                                    _currentMap.Tiles[x, (int)rst.TileIndex.Y].IsPreviewingRoad = false;
                                    if (_currentMap.Tiles[x, (int)rst.TileIndex.Y].Object.ObjectId <= 0 &&
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].IsVisible)
                                    {
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].Object = obj;
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].TileData.Object = obj;
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].ObjectTexture = _gameContent.GetTileTexture(applied_txt_index);
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].Object.TextureIndex =
                                            applied_txt_index;
                                        _currentMap.Tiles[x, (int)rst.TileIndex.Y].TileData.Object.TextureIndex =
                                            applied_txt_index;
                                    }
                                    if (x.Equals((int)(rst.TileIndex.X + x_offset)))
                                    {
                                        if (y_offset >= 0)
                                        {
                                            for (int y = (int)(rst.TileIndex.Y);
                                                y < (int)(rst.TileIndex.Y + y_offset + 1);
                                                y++)
                                            {
                                                _currentMap.Tiles[x, y].IsPreviewingRoad = false;
                                                if (_currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                    _currentMap.Tiles[x, y].IsVisible)
                                                {
                                                    _currentMap.Tiles[x, y].Object = obj;
                                                    _currentMap.Tiles[x, y].TileData.Object = obj;
                                                    _currentMap.Tiles[x, y].ObjectTexture = _gameContent.GetTileTexture(applied_txt_index);
                                                    _currentMap.Tiles[x, y].Object.TextureIndex =
                                                        applied_txt_index;
                                                    _currentMap.Tiles[x, y].TileData.Object.TextureIndex =
                                                        applied_txt_index;
                                                }
                                            }
                                        }
                                        else if (y_offset < 0)
                                        {
                                            for (int y = (int)(rst.TileIndex.Y);
                                                y > (int)(rst.TileIndex.Y + y_offset - 1);
                                                y--)
                                            {
                                                _currentMap.Tiles[x, y].IsPreviewingRoad = false;
                                                if (_currentMap.Tiles[x, y].Object.ObjectId <= 0 &&
                                                    _currentMap.Tiles[x, y].IsVisible)
                                                {
                                                    _currentMap.Tiles[x, y].Object = obj;
                                                    _currentMap.Tiles[x, y].TileData.Object = obj;
                                                    _currentMap.Tiles[x, y].ObjectTexture = _gameContent.GetTileTexture(applied_txt_index);
                                                    _currentMap.Tiles[x, y].Object.TextureIndex =
                                                        applied_txt_index;
                                                    _currentMap.Tiles[x, y].TileData.Object.TextureIndex =
                                                        applied_txt_index;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // take away values from inventory
                        if (obj.ObjectId.Equals(Building.Road().ObjectId))
                        {
                            GSData.PlayerInventory.Money -= total_Money_for_roads;
                        }
                        else
                        {
                            GSData.PlayerInventory.Money -= obj.MoneyUpfront;
                            GSData.PlayerInventory.Wood -= obj.WoodUpfront;
                            GSData.PlayerInventory.Coal -= obj.CoalUpfront;
                            GSData.PlayerInventory.Iron -= obj.IronUpfront;
                            GSData.PlayerInventory.Stone -= obj.StoneUpfront;
                            GSData.PlayerInventory.Workers -= obj.WorkersUpfront;
                            GSData.PlayerInventory.Energy -= obj.EnergyUpfront;
                            GSData.PlayerInventory.Food -= obj.FoodUpfront;
                        }

                        // light up area around powerline
                        if (obj.ObjectId.Equals(Building.PowerLine().ObjectId))
                        {
                            // now that building is built, loop back through tiles in range
                            for (int x = ((int)t.TileIndex.X - obj.Range); x < (t.TileIndex.X + (obj.Range + 1)); x++)
                            {
                                for (int y = ((int)t.TileIndex.Y - obj.Range); y < (t.TileIndex.Y + (obj.Range + 1)); y++)
                                {
                                    // set tiles in range to be visible
                                    try
                                    {
                                        _currentMap.Tiles[x, y].IsVisible = true;
                                        _currentMap.Tiles[x, y].TileData.IsVisible = true;
                                    }
                                    catch (Exception exc)
                                    {
                                        Console.WriteLine($"Couldn't activate tile: {new Vector2(x, y)} | {exc.Message}");
                                    }
                                }
                            }
                        }

                        // if building is a farm
                        if (obj.ObjectId.Equals(Building.Farm().ObjectId))
                        {
                            var farmland_placed = 0;
                            // is farm, apply crops around farm within limited range
                            for (int x = ((int)t.TileIndex.X - 1); x < (t.TileIndex.X + 2); x++)
                            {
                                for (int y = ((int)t.TileIndex.Y - 1); y < (t.TileIndex.Y + 2); y++)
                                {
                                    // tile is same tile of farm
                                    if (new Vector2(x, y).Equals(t.TileIndex)) continue;

                                    // the tile is already an object, don't apply farmland
                                    if (_currentMap.Tiles[x, y].Object.ObjectId > 0) continue;

                                    // if the tile is visible (part of player's territory
                                    if (_currentMap.Tiles[x, y].IsVisible)
                                    {
                                        try
                                        {
                                            var farmobj = Resource.Farmland();
                                            _currentMap.Tiles[x, y].Object = farmobj;
                                            _currentMap.Tiles[x, y].ObjectTexture =
                                                _gameContent.GetTileTexture(farmobj.TextureIndex);
                                            _currentMap.Tiles[x, y].TileData.Object = farmobj;
                                            farmland_placed++;
                                        }
                                        catch (Exception exc)
                                        {
                                            Console.WriteLine(
                                                $"Couldn't activate tile: {new Vector2(x, y)} | {exc.Message}");
                                        }
                                    }
                                }
                            }

                            // calculate foodoutput gain based one farmland placed
                            var foodoutput = 5 * farmland_placed;
                            _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].TileData.Object.FoodOutput += foodoutput;
                            _currentMap.Tiles[(int)t.TileIndex.X, (int)t.TileIndex.Y].Object.FoodOutput += foodoutput;
                        }

                        GSData.TileData = new List<TileData>();

                        // update current gamestate data of tiles
                        // for each tile in current map,
                        foreach (Tile cmt in _currentMap.Tiles)
                        {
                            // add its tile data to list
                            if (cmt.Object != null)
                            {
                                if (cmt.Object.ObjectId == Building.Road().ObjectId)
                                {
                                    cmt.ObjectTexture = cmt.DecideTexture_NearbyRoadsFactor();
                                    cmt.Object.TextureIndex = cmt.DecideTextureID_NearbyRoadsFactor();
                                    cmt.TileData.Object.TextureIndex = cmt.Object.TextureIndex;
                                }
                            }
                            GSData.TileData.Add(cmt.GetTileData());

                        }
                    }
                    else if (sel_obj.ObjectId.Equals(0) && t.Object.ObjectId >= 0 & t.Object.TypeId.Equals(2) && RoadStartTile is null)
                    {

                        if (t.Object.ObjectId == Building.Road().ObjectId)
                        {
                            Console.WriteLine("Nearby roads for road: ");
                            var nrby = t.GetNearbyRoads();
                            Console.WriteLine($"Left    : {nrby[0].ToString()}");
                            Console.WriteLine($"Right   : {nrby[1].ToString()}");
                            Console.WriteLine($"Up      : {nrby[2].ToString()}");
                            Console.WriteLine($"Down    : {nrby[3].ToString()}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine(
                        $"Tile {new Vector2(t.TileIndex.X, t.TileIndex.Y)} is outside of the active area.");
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"ERROR ON TILE PLACE: {exception.Message}");
                SelectedObject = sel_obj;
                //GSData.PlayerInventory = prev_inv;
            }
        }

        public void DeleteBldgButton_Click()
        {
            Console.WriteLine("Deleting building..");

            if (CurrentlySelectedTile is null) return;
            else if (BuildingData.ValidResource(CurrentlySelectedTile.Object) && CurrentlySelectedTile.Object.ObjectId == 2)
            {
                GSData.PlayerInventory.Money -= 10;
            }
            // delete a building
            foreach (Tile t in _currentMap.Tiles)
            {
                if (t.TileIndex != CurrentlySelectedTile.TileIndex) continue;
                t.Object = new TileObject();
                t.TileData.Object = new TileObject();
                t.ObjectDestroyed = true;

                ObjectDestroyed?.Invoke(this, new EventArgs());

                GSData.PlayerInventory.Money += (t.Object.MoneyOutput / 2);
                GSData.PlayerInventory.Wood += (t.Object.WoodCost / 2);
                GSData.PlayerInventory.Coal += (t.Object.CoalCost / 2);
                GSData.PlayerInventory.Iron += (t.Object.IronCost / 2);
                GSData.PlayerInventory.Stone += (t.Object.StoneCost / 2);
            }

            CurrentlySelectedTile = null;
        }

        public void HandleInput(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        {
            // if first render
            if (_firstTake.Equals(true))
            {
                _firstTake = false;
            }

            // if shift is held down, set shift multiplier - else, set to 1
            float shift = (keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift))
                ? 3.5f
                : 1f;

            // if esc is held down, go back to main menu
            if (keyboardState.IsKeyDown(Keys.Escape))
            {
                Console.WriteLine("ESCAPE clicked...");
            }

            if (mouseState.RightButton == ButtonState.Released && _previousMouseState.RightButton == ButtonState.Pressed)
            {
                SelectedObject = new TileObject();
            }

            // if WASD, move camera accordingly and mutiply by shift multiplier

            if (keyboardState.IsKeyDown(Keys.A))
            {
                _camera.Position += new Vector2(-2, 0) * shift;
            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                _camera.Position += new Vector2(2, 0) * shift;
            }
            if (keyboardState.IsKeyDown(Keys.W))
            {
                _camera.Position += new Vector2(0, -2) * shift;
            }
            if (keyboardState.IsKeyDown(Keys.S))
            {
                _camera.Position += new Vector2(0, 2) * shift;
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            var msp = Mouse.GetState().Position;
            var mp = new Vector2(msp.X, msp.Y);

            if (IsLoaded)
            {
                // game state is loaded

                // TWO SPRITE BATCHES:
                // First batch is for the game itself, the map, npcs, all that live shit
                // Second batch is for UI and HUD rendering - separate from camera matrixes and all that ingame shit

                spriteBatch.Begin(_camera, samplerState: SamplerState.PointClamp);

                // draw game here
                try
                {
                    _currentMap.Draw(gameTime, spriteBatch);
                }
                catch (Exception e)
                {
                    //Console.WriteLine("Error drawing map:" + e.Message);
                }

                spriteBatch.End();

                //---------------------------------------------------------

                spriteBatch.Begin();
                // draw UI / HUD here 
                foreach (var c in Components)
                {
                    c.Draw(gameTime, spriteBatch);
                }

                if (SelectedObject.ObjectId > 0)
                {

                    var txt = _gameContent.GetTileTexture(SelectedObject.TextureIndex);
                    var pos = mp - new Vector2((txt.Width * 2) / 2, (txt.Height * 2) - ((txt.Height * 2) * 0.25f));
                    var rct = new Rectangle((int)pos.X, (int)pos.Y, txt.Width * 2, txt.Height * 2);
                    spriteBatch.Draw(txt, destinationRectangle: rct, color: Color.White);

                    if (!(CurrentlyHoveredTile is null))
                    {
                        var t = CurrentlyHoveredTile;
                        var obj = (Building)SelectedObject;

                        if (BuildingData.Dict_BuildingResourceLinkKeys.ContainsKey(SelectedObject.ObjectId))
                        {
                            var count = 0;
                            var resource_ids = BuildingData.Dict_BuildingResourceLinkKeys[SelectedObject.ObjectId];

                            // create dictionary to hold counts for each resource
                            var counts = new Dictionary<int, int>();

                            foreach (var r in resource_ids)
                            {
                                counts.Add(r, 0);
                            }

                            // Try to display nearby resources when a bldg is selected and hovering over tile but not yet placed
                            try
                            {
                                // check for adjacent roads
                                var f = t.GetNearbyRoads();
                                var adj_road_cnt = f.Count(b => b);
                                if (adj_road_cnt < 1) throw new Exception("No Adjacent Road");

                                // if farmland, look for empty space
                                if (obj.ObjectId.Equals(Building.Farm().ObjectId))
                                {
                                    for (int x = ((int)t.TileIndex.X - 1); x < (t.TileIndex.X + 2); x++)
                                    {
                                        for (int y = ((int)t.TileIndex.Y - 1); y < (t.TileIndex.Y + 2); y++)
                                        {
                                            // for each resource linked to this bldg
                                            foreach (var r in resource_ids)
                                            {
                                                if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(obj.ObjectId) && _currentMap.Tiles[x, y].Object.TypeId.Equals(2))
                                                    throw new Exception(
                                                        "There is already a building of that type collecting resources in this area.");

                                                // if the tile matches the resource for the bldg
                                                if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(0) & !(_currentMap.Tiles[x, y].TerrainId.Equals(Resource.Water().TerrainId)))
                                                {
                                                    counts[r] = counts[r] + 1;
                                                }
                                            }
                                        }
                                    }
                                }
                                // else, look for respective resource
                                else
                                {
                                    // for each tile within the building's range
                                    for (int x = ((int)t.TileIndex.X - obj.Range);
                                        x < (t.TileIndex.X + (obj.Range + 1));
                                        x++)
                                    {
                                        for (int y = ((int)t.TileIndex.Y - obj.Range);
                                            y < (t.TileIndex.Y + (obj.Range + 1));
                                            y++)
                                        {
                                            // for each resource linked to this bldg
                                            foreach (var r in resource_ids)
                                            {
                                                if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(obj.ObjectId) && _currentMap.Tiles[x, y].Object.TypeId.Equals(2))
                                                    throw new Exception(
                                                        "There is already a building of that type collecting resources in this area.");

                                                // if the tile matches the resource for the bldg
                                                if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(r))
                                                {
                                                    counts[r] = counts[r] + 1;
                                                }
                                            }
                                        }
                                    }
                                }

                                int indent = 0;

                                foreach (var r in resource_ids)
                                {
                                    var res_str = $"{BuildingData.Dic_ResourceNameKeys[r]}: {(counts[r] * (int)BuildingData.Dic_ResourceCollectionKeys[r][1])}";
                                    var str_x = ((_gameContent.GetFont(1).MeasureString(res_str).X * 1.3f) / 2);
                                    var str_y = ((_gameContent.GetFont(1).MeasureString(res_str).Y * 1.3f) / 2);

                                    spriteBatch.DrawString(_gameContent.GetFont(1), res_str, mp + new Vector2(0, -50 + (15 * indent)), Color.Black, 0.0f, new Vector2(str_x, str_y), 1.3f, SpriteEffects.None, 1.0f);
                                    indent++;
                                }
                            }
                            catch (Exception e)
                            {
                                var print_str = $"Error: {e.Message}";
                                var str_x = ((_gameContent.GetFont(1).MeasureString(print_str).X) / 2);
                                var str_y = ((_gameContent.GetFont(1).MeasureString(print_str).Y) / 2);

                                spriteBatch.DrawString(_gameContent.GetFont(1), print_str, mp + new Vector2(0, -50), Color.Black, 0.0f, new Vector2(str_x, str_y), 1.0f, SpriteEffects.None, 1.0f);
                            }
                        }
                        else if (SelectedObject.ObjectId.Equals(Building.Watermill().ObjectId))
                        {
                            try
                            {
                                // check for adjacent roads
                                var f = t.GetNearbyRoads();
                                var adj_road_cnt = f.Count(b => b);
                                if (adj_road_cnt < 1) throw new Exception("No Adjacent Road");

                                // look for adjacent water tiles
                                var nearby_water = false;
                                for (int x = ((int)t.TileIndex.X - 1);
                                    x < (t.TileIndex.X + (2));
                                    x++)
                                {
                                    for (int y = ((int)t.TileIndex.Y - 1);
                                        y < (t.TileIndex.Y + (2));
                                        y++)
                                    {
                                        // if there is already of a building of the selected building's type within the range
                                        if (_currentMap.Tiles[x, y].Object.ObjectId.Equals(Resource.Water().Object.ObjectId)
                                            && _currentMap.Tiles[x, y].Object.TypeId.Equals(Resource.Water().Object.TypeId)
                                            && _currentMap.Tiles[x, y].TerrainId.Equals(Resource.Water().TerrainId))

                                            nearby_water = true;
                                    }
                                }

                                if (nearby_water != true)
                                    throw new Exception(
                                        "This building requires an adjacent water tile to construct."); // deny

                                var print_str = $"Nearby water available!";
                                var str_x = ((_gameContent.GetFont(1).MeasureString(print_str).X) / 2);
                                var str_y = ((_gameContent.GetFont(1).MeasureString(print_str).Y) / 2);

                                spriteBatch.DrawString(_gameContent.GetFont(1), print_str, mp + new Vector2(0, -50), Color.Black, 0.0f, new Vector2(str_x, str_y), 1.0f, SpriteEffects.None, 1.0f);

                            }
                            catch (Exception e)
                            {
                                var print_str = $"Error: {e.Message}";
                                var str_x = ((_gameContent.GetFont(1).MeasureString(print_str).X) / 2);
                                var str_y = ((_gameContent.GetFont(1).MeasureString(print_str).Y) / 2);

                                spriteBatch.DrawString(_gameContent.GetFont(1), print_str, mp + new Vector2(0, -50), Color.Black, 0.0f, new Vector2(str_x, str_y), 1.0f, SpriteEffects.None, 1.0f);
                            }
                        }
                        else if (BuildingData.Dict_BuildingFromObjectID[SelectedObject.ObjectId].RequiresRoad)
                        {
                            // check for adjacent road if object is not a road
                            var f = t.GetNearbyRoads();
                            var adj_road_cnt = f.Count(b => b);
                            if (adj_road_cnt < 1)
                            {
                                var print_str = $"Error: No Adjacent Road";
                                var str_x = ((_gameContent.GetFont(1).MeasureString(print_str).X) / 2);
                                var str_y = ((_gameContent.GetFont(1).MeasureString(print_str).Y) / 2);

                                spriteBatch.DrawString(_gameContent.GetFont(1), print_str, mp + new Vector2(0, -50), Color.Black, 0.0f, new Vector2(str_x, str_y), 1.0f, SpriteEffects.None, 1.0f);
                            }
                        }
                    }
                }

                // draw cursor over UI elements !!
                spriteBatch.Draw(_cursorTexture, mp, Color.White);

                spriteBatch.End();
            }
            else
            {
                // most of whats drawn in here is strictly UI so only one spritebatch should be needed
                spriteBatch.Begin(samplerState: SamplerState.PointClamp);

                // draw loading bar and text

                var scale = new Vector2
                {
                    X = _graphicsDevice.Viewport.Width,
                    Y = _graphicsDevice.Viewport.Height
                };

                var dimensions = new Vector2
                {
                    X = scale.X / 2,
                    Y = scale.Y / 2
                };

                var x = (_gameContent.GetFont(1).MeasureString(LoadingText).X / 2);
                var y = (_gameContent.GetFont(1).MeasureString(LoadingText).Y / 2);

                spriteBatch.DrawString(_gameContent.GetFont(1), LoadingText, dimensions, Color.White, 0.0f, new Vector2(x, y), 1.0f, SpriteEffects.None, 1.0f);

                spriteBatch.Draw(LoadingTexture, destinationRectangle: LoadingBar, color: Color.White);

                var cells = LoadProgress / 10;

                for (int i = 0; i < cells + 1; i++)
                {
                    var cellRectangle = new Rectangle(LoadingBar.X + (i * (LoadingBar.Width / 10)), LoadingBar.Y, LoadingBar.Width / 10, LoadingBar.Height);
                    spriteBatch.Draw(LoadingCellTexture, destinationRectangle: cellRectangle, color: Color.White);
                }

                spriteBatch.Draw(_cursorTexture, mp, Color.White);

                // draw loading screen here?

                spriteBatch.End();
            }
        }
    }
}
//1717