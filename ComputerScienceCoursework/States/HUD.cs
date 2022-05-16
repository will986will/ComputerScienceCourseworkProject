﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerScienceCoursework.Content;
using ComputerScienceCoursework.Objects;
using ComputerScienceCoursework.States;
using ComputerScienceCoursework.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.TextureAtlases;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI.Properties;

namespace ComputerScienceCoursework.UI
{
    public class HUD : Component
    {
        public GameState State { get; set; }
        private Desktop _HUD;
        public IncomeReport CurrentIncomeReport => State.CurrentMap.GetIncomeReport;
        protected GameContent _content { get; set; }
        protected GraphicsDevice _graphicsDevice { get; set; }
        protected SpriteFont _font { get; set; }
        protected MouseState _previousMouse { get; set; }
        protected MouseState _currentMouse { get; set; }
        protected Vector2 _displaySize { get; set; }
        public Vector2 DisplaySize => _displaySize;
        protected Texture2D _texture { get; set; }
        protected Color _displayColor { get; set; } = Color.WhiteSmoke;
        protected Color[] _displayColorData { get; set; }
        protected Vector2 _buttonSize { get; set; }
        protected Vector2 _position { get; set; }
        protected Vector2 _scale { get; set; } = new Vector2(1, 1);
        protected List<Component> _components = new List<Component>();
        public Rectangle DisplayRect => new Rectangle((int)_position.X, (int)_position.Y, (int)_displaySize.X, (int)_displaySize.Y);
        public Rectangle BorderRect => new Rectangle((int)_position.X, (int)_position.Y - 5, (int)_displaySize.X, 5);
        protected Vector2 SelectionCellsSection_GridSpacePercents => new Vector2(0.9f, 0.1f);
        protected Vector2 SelectionCellsSection_Dimensions => new Vector2(_displaySize.X * 0.4f, _displaySize.Y);
        protected Rectangle SelectionCellsSection_Rectangle => new Rectangle((int)(_position.X + (_displaySize.X * 0.6)), (int)_position.Y, (int)SelectionCellsSection_Dimensions.X, (int)SelectionCellsSection_Dimensions.Y);
        protected Texture2D SelectionCellsSection_Texture { get; set; }
        protected Vector2 SelectionCell_GridDimensions => new Vector2(6, 3);
        protected Vector2 SelectionCell_Dimensions => new Vector2((SelectionCellsSection_Dimensions.X * SelectionCellsSection_GridSpacePercents.X) / SelectionCell_GridDimensions.X,
            (SelectionCellsSection_Dimensions.Y * SelectionCellsSection_GridSpacePercents.X) / SelectionCell_GridDimensions.Y);

        protected Texture2D SelectionCell_Texture { get; set; }
        protected Vector2 SelectionCell_Spacing => new Vector2((SelectionCellsSection_Dimensions.X * SelectionCellsSection_GridSpacePercents.Y) /
                                                               (SelectionCell_GridDimensions.X + 1),
            (SelectionCellsSection_Dimensions.Y * SelectionCellsSection_GridSpacePercents.Y) /
            (SelectionCell_GridDimensions.Y + 1));
        protected Vector2 SelectionCells_BuildingIndexes => new Vector2(13, 21); // Y is max + 1
        protected int SelectionCell_BlankIndex = 13;
        protected Texture2D SelectionCell_BlankTexture { get; set; }
        protected List<int> SelectionCells_BldgHouses_Indexes => new List<int>()
        {
            15, // Low House Icon
            24, // Med House Icon
            25, // Elite House Icon
            40 // High Rise House Icon
        };
        // RESIDENTIAL TEXTURES
        protected List<Texture2D> SelectionCells_BldgHouses_Textures { get; set; } = new List<Texture2D>();
        // RESIDENTIAL BUTTONS
        protected List<Button> SelectionCells_BldgHouses_Btns { get; set; } = new List<Button>();

        // RESOURCE INDEXES
        protected List<int> SelectionCells_BldgReso_Indexes => new List<int>()
        {
            14, // Townhall Icon
            16, // Farm Icon
            17, // Log Cabin Icon
            18, // Quarry Icon
            19, // Powerline Icon
            20, // Windmill Icon
            39, // Watermill
            41, // Coal Plant
        };
        // RESOURCE TEXTURES
        protected List<Texture2D> SelectionCells_BldgReso_Textures { get; set; } = new List<Texture2D>();
        // RESOURCE BUTTONS
        protected List<Button> SelectionCells_BldgReso_Btns { get; set; } = new List<Button>();

        // DECORATION INDEXES
        protected List<int> SelectionCells_BldgDeco_Indexes => new List<int>()
        {
            30 // Road Icon
        };
        // DECORATION TEXTURES
        protected List<Texture2D> SelectionCells_BldgDeco_Textures { get; set; } = new List<Texture2D>();
        // DECORATION BUTTONS
        protected List<Button> SelectionCells_BldgDeco_Btns { get; set; } = new List<Button>();

        // ALL BUILDING TEXTURES
        protected List<List<Texture2D>> SelectionCells_AllBldgs_Textures => new List<List<Texture2D>>()
        {
            SelectionCells_BldgHouses_Textures, SelectionCells_BldgReso_Textures, SelectionCells_BldgDeco_Textures
        };

        // CURRENT VIEW INDEX (WHICH BUILDING PAGE IS VIEWED)
        protected int SelectionCells_ViewIndex = 800; // 800 = Houses, 801 = Resources, 802 = Decorations

        protected List<Texture2D> SelectionCells_BuildingTextures { get; set; } = new List<Texture2D>();
        // END SELECTION CELLS SECTION

        // BEGIN INFOGRAPHIC SECTION (MIDDLE MENU)
        protected Vector2 InfographicsSection_Dimensions => new Vector2(_displaySize.X * 0.3f, _displaySize.Y);
        protected Rectangle InfographicsSection_Rectangle => new Rectangle((int)(_position.X + (_displaySize.X * 0.3)), (int)_position.Y, (int)InfographicsSection_Dimensions.X, (int)InfographicsSection_Dimensions.Y);
        protected Texture2D InfographicsSection_Texture { get; set; }
        // END INFOGRAPHIC SECTION

        // BEGIN RESOURCE BAR SECTION (DISPLAY RESOURCE VALS)
        protected Vector2 ResourceBar_Dimensions => new Vector2(InfographicsSection_Dimensions.X * 0.95f,
            InfographicsSection_Dimensions.Y * 0.3f);

        protected Vector2 ResourceBar_Position => new Vector2(InfographicsSection_Rectangle.X +
                                                              ((InfographicsSection_Rectangle.Width * 0.05f) / 2),
            (InfographicsSection_Rectangle.Y + InfographicsSection_Rectangle.Height) - (ResourceBar_Dimensions.Y + (InfographicsSection_Rectangle.Height * 0.05f)));
        protected Rectangle ResourceBar_Rectangle => new Rectangle((int)ResourceBar_Position.X, (int)ResourceBar_Position.Y, (int)ResourceBar_Dimensions.X, (int)ResourceBar_Dimensions.Y);
        protected Texture2D ResourceBar_Texture { get; set; }

        protected Vector2 ResourceCell_Dimension => new Vector2(ResourceBar_Dimensions.X / 8, ResourceBar_Dimensions.Y);
        // END RESOUCE BAR SECTION

        // BEGIN SELECT BUILDING PAGE BUTTONS SECTION (WITHIN INFOGRAPHIC SECTION)
        protected Texture2D Btn_SelectHouses_Texture { get; set; }
        protected const string Btn_SelectHouses_String = "View Residential Buildings";

        protected Texture2D Btn_SelectDeco_Texture { get; set; }
        protected const string Btn_SelectDeco_String = "View Decoration Buildings";

        protected Texture2D Btn_SelectReso_Texture { get; set; }
        protected const string Btn_SelectReso_String = "View Resource Buildings";

        protected List<Button> BtnList_SelectBldgsView { get; set; } = new List<Button>();
        // END SELECT BUILDING PAGE BUTTONS SECTION

        // BEGIN DISPLAY INFO SECTION
        protected Vector2 DisplayInfo_Dimensions => new Vector2(ResourceBar_Rectangle.Width - (Btn_SelectHouses_Texture.Width), ResourceBar_Rectangle.Height * 2);
        protected Vector2 DisplayInfo_Position => new Vector2(ResourceBar_Rectangle.X, InfographicsSection_Rectangle.Y + (InfographicsSection_Rectangle.Height * 0.05f));
        protected Texture2D DisplayInfo_Texture { get; set; }
        protected Rectangle DisplayInfo_Rectangle => new Rectangle((int)DisplayInfo_Position.X, (int)DisplayInfo_Position.Y, (int)DisplayInfo_Dimensions.X, (int)DisplayInfo_Dimensions.Y);

        protected Texture2D DeleteBuildingBtn_Texture { get; set; }
        protected Button DeleteBuildingBtn { get; set; }
        protected Vector2 DeleteBuildingBtn_Pos { get; set; }

        protected bool DisplayDeleteBldgBtn = false;
        // END DISPLAY INFO SECTION

        // PROPERTIES FOR SELECT BUILDING BUTTON TOOLTIP
        protected bool ShowBldgBtnTooltip { get; set; } = false;
        protected int ShowBldgBtnId { get; set; } = 0;
        // END PROPERTIES FOR SELECT BUILDING BUTTON TOOLTIP

        // PROPERTIERS FOR HOVER BUILDING BUTTON TOOLTIP
        private string _baseString = "WORKERS: ";
        private string _baseString_PerTurnRevenue = "-000";
        private string _baseString_Header = "RESOURCE  COST LOSS GAIN";
        protected Vector2 Tooltip_Dimensions => new Vector2(_font.MeasureString(_baseString).X, (_font.MeasureString(_baseString).Y + 10) * 8);
        protected Vector2 ExtraTooltip_Dimensions => new Vector2(_font.MeasureString(_baseString_PerTurnRevenue).X, _font.MeasureString(_baseString_PerTurnRevenue).Y + 10);
        protected Texture2D Tooltip_Texture { get; set; }
        // END PROPERTIES FOR HOVER BUILDING BUTTON TOOLTIP

        /// <summary>
        /// An int array containing the IDs for icons to represent inventory items in the HUD.
        /// In order: Money, Wood, Coal, Iron, Food, Energy, Workers
        /// </summary>
        public int[] HUDIconIDs { get; set; }

        public HUD(GraphicsDevice graphicsDevice_, GameContent content_, GameInstance game)
        {
            
            _graphicsDevice = graphicsDevice_;
            MyraEnvironment.Game = game;
            var HUDPanel = new Panel();
            HUDPanel.VerticalAlignment = Myra.Graphics2D.UI.VerticalAlignment.Bottom;
            HUDPanel.Height = (int)(graphicsDevice_.Viewport.Height * 0.25f);
            HUDPanel.Background = new SolidBrush("#4BD961FF");
            _HUD = new Desktop();
            _HUD.Root = HUDPanel;

            int height = (int)(graphicsDevice_.Viewport.Height * 0.25f);
            int width = (int)(graphicsDevice_.Viewport.Width);

            _position = new Vector2(0, graphicsDevice_.Viewport.Height - height);
            _displaySize = new Vector2(width, height);
            _buttonSize = new Vector2(30, height * 0.8f);


            _content = content_;
            _font = _content.GetFont(1);

            HUDIconIDs = new int[]
            {
                5,6,7,8,12,9,10,11
            };

            SetColorData(graphicsDevice_);

            DisplayInfo_Texture = content_.GetUiTexture(37);

            for (int i = (int)SelectionCells_BuildingIndexes.X; i < SelectionCells_BuildingIndexes.Y; i++)
            {
                SelectionCells_BuildingTextures.Add(content_.GetUiTexture(i));
            }
            SelectionCells_BuildingTextures.Add(content_.GetUiTexture(24));
            SelectionCells_BuildingTextures.Add(content_.GetUiTexture(25));
            SelectionCells_BuildingTextures.Add(content_.GetUiTexture(40));

            if (SelectionCells_BuildingTextures.Count <
                (SelectionCell_GridDimensions.X * SelectionCell_GridDimensions.Y))
            {
                var dif = (SelectionCell_GridDimensions.X * SelectionCell_GridDimensions.Y) -
                          SelectionCells_BuildingTextures.Count;
                for (int i = 0; i < dif + 1; i++)
                {
                    SelectionCells_BuildingTextures.Add(SelectionCells_BuildingTextures[0]);
                }
            }

            SelectionCell_BlankTexture = content_.GetUiTexture(13);
            foreach (var e in SelectionCells_BldgHouses_Indexes)
            {
                SelectionCells_BldgHouses_Textures.Add(content_.GetUiTexture(e));
            }
            foreach (var e in SelectionCells_BldgReso_Indexes)
            {
                SelectionCells_BldgReso_Textures.Add(content_.GetUiTexture(e));
            }
            foreach (var e in SelectionCells_BldgDeco_Indexes)
            {
                SelectionCells_BldgDeco_Textures.Add(content_.GetUiTexture(e));
            }
            foreach (var e in SelectionCells_AllBldgs_Textures)
            {
                if (e.Count < (SelectionCell_GridDimensions.X * SelectionCell_GridDimensions.Y))
                {
                    var dif = (SelectionCell_GridDimensions.X * SelectionCell_GridDimensions.Y) - e.Count;
                    for (int i = 0; i < dif + 1; i++)
                    {
                        e.Add(SelectionCell_BlankTexture);
                    }
                }
            }

            SetSelectionCellButtons();

            // get textures for buttons that swap between building selections
            Btn_SelectHouses_Texture = content_.GetUiTexture(26);
            Btn_SelectReso_Texture = content_.GetUiTexture(27);
            Btn_SelectDeco_Texture = content_.GetUiTexture(28);

            var init_btn_bldg_pos = new Vector2();
            init_btn_bldg_pos.X = (ResourceBar_Rectangle.X + ResourceBar_Rectangle.Width) - (Btn_SelectHouses_Texture.Width);
            init_btn_bldg_pos.Y = (ResourceBar_Rectangle.Y - (Btn_SelectHouses_Texture.Height * 3.5f));

            var btn_bldgs_house = new Button(Btn_SelectHouses_Texture, _font)
            {
                Position = init_btn_bldg_pos,
                ID = 800
            };
            btn_bldgs_house.Click += ViewBldgsBtn_OnClick;
            var btn_bldgs_reso = new Button(Btn_SelectReso_Texture, _font)
            {
                Position = init_btn_bldg_pos + new Vector2(0, Btn_SelectHouses_Texture.Height * 1.18f),
                ID = 801
            };
            btn_bldgs_reso.Click += ViewBldgsBtn_OnClick;
            var btn_bldgs_deco = new Button(Btn_SelectDeco_Texture, _font)
            {
                Position = init_btn_bldg_pos + new Vector2(0, ((Btn_SelectHouses_Texture.Height * 1.18f) * 2f)),
                ID = 802
            };
            btn_bldgs_deco.Click += ViewBldgsBtn_OnClick;
            _components.Add(btn_bldgs_house);
            _components.Add(btn_bldgs_reso);
            _components.Add(btn_bldgs_deco);
            BtnList_SelectBldgsView.Add(btn_bldgs_house);
            BtnList_SelectBldgsView.Add(btn_bldgs_reso);
            BtnList_SelectBldgsView.Add(btn_bldgs_deco);

            DeleteBuildingBtn_Texture = content_.GetUiTexture(23);
            DeleteBuildingBtn_Pos = new Vector2(
                (int)(DisplayInfo_Rectangle.X + DisplayInfo_Rectangle.Width - DeleteBuildingBtn_Texture.Width -
                       ((DisplayInfo_Rectangle.Width * 0.05f) / 2)),
                (int)(DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height * 0.05f)));

            DeleteBuildingBtn = new Button(content_.GetUiTexture(23), _font)
            {
                Position = DeleteBuildingBtn_Pos,
                HoverColor = Color.Red
            };
            DeleteBuildingBtn.Click += DeleteBuildingBtnOnClick;
        }

        private void DeleteBuildingBtnOnClick(object sender, EventArgs e)
        {
            State.DeleteBldgButton_Click();
        }

        public void SetColorData(GraphicsDevice graphicsDevice_)
        {
            Tooltip_Texture = new Texture2D(graphicsDevice_, (int)Tooltip_Dimensions.X, (int)Tooltip_Dimensions.Y);
            _texture = new Texture2D(graphicsDevice_, (int)_displaySize.X, (int)_displaySize.Y);
            _displayColorData = new Color[(int)_displaySize.X * (int)_displaySize.Y];
            for (int i = 0; i < _displayColorData.Length; i++)
            {
                _displayColorData[i] = _displayColor;
            }
            _texture.SetData(_displayColorData);

            SelectionCellsSection_Texture = new Texture2D(graphicsDevice_, (int)SelectionCellsSection_Dimensions.X, (int)SelectionCellsSection_Dimensions.Y);
            var scst_data =
                new Color[(int)SelectionCellsSection_Dimensions.X * (int)SelectionCellsSection_Dimensions.Y];
            for (int i = 0; i < scst_data.Length; i++)
            {
                scst_data[i] = Color.White;
            }
            SelectionCellsSection_Texture.SetData(scst_data);

            InfographicsSection_Texture = new Texture2D(graphicsDevice_, (int)InfographicsSection_Dimensions.X, (int)InfographicsSection_Dimensions.Y);
            var ist_data =
                new Color[(int)InfographicsSection_Dimensions.X * (int)InfographicsSection_Dimensions.Y];
            for (int i = 0; i < ist_data.Length; i++)
            {
                ist_data[i] = Color.White;
            }
            InfographicsSection_Texture.SetData(ist_data);

            ResourceBar_Texture = new Texture2D(graphicsDevice_, (int)ResourceBar_Dimensions.X, (int)ResourceBar_Dimensions.Y);
            var rbt_data =
                new Color[(int)ResourceBar_Dimensions.X * (int)ResourceBar_Dimensions.Y];
            for (int i = 0; i < rbt_data.Length; i++)
            {
                rbt_data[i] = Color.White;
            }
            ResourceBar_Texture.SetData(rbt_data);

            ResourceBar_Texture = _content.GetUiTexture(29);
            Console.WriteLine($"ResourceBar dimens: Width: {ResourceBar_Rectangle.Width} || Height: {ResourceBar_Rectangle.Height}");

            SelectionCell_Texture = new Texture2D(graphicsDevice_, (int)SelectionCell_Dimensions.X, (int)SelectionCell_Dimensions.Y);
            var sct_data = new Color[(int)SelectionCell_Dimensions.X * (int)SelectionCell_Dimensions.Y];
            for (int i = 0; i < sct_data.Length; i++)
            {
                sct_data[i] = Color.Beige;
            }
            SelectionCell_Texture.SetData(sct_data);

            // set color data of tooltip
            var dta = new Color[(int)Tooltip_Dimensions.X * (int)Tooltip_Dimensions.Y];
            for (int o = 0; o < dta.Length; o++)
            {
                dta[o] = Color.White;
            }
            Tooltip_Texture.SetData(dta);
        }

        public void SetSelectionCellButtons()
        {
            var initCellPos = new Vector2(SelectionCellsSection_Rectangle.X, SelectionCellsSection_Rectangle.Y) +
                              new Vector2(SelectionCell_Spacing.X, SelectionCell_Spacing.Y);

            // Residential Buildings Buttons
            var b = 0;
            for (int i = 0; i < (SelectionCell_GridDimensions.Y); i++)
            {
                var cellRowPos = initCellPos;
                cellRowPos += new Vector2(0, SelectionCell_Dimensions.Y * i) + new Vector2(0, SelectionCell_Spacing.Y * i);
                for (int j = 0; j < (SelectionCell_GridDimensions.X); j++)
                {
                    var cellColPos = cellRowPos + new Vector2(j * SelectionCell_Dimensions.X, 0) + new Vector2(j * SelectionCell_Spacing.X, 0);
                    var cellRect = new Rectangle((int)cellColPos.X, (int)cellColPos.Y, (int)SelectionCell_Dimensions.X, (int)SelectionCell_Dimensions.Y);
                    var btn = new Button(SelectionCells_BldgHouses_Textures[b], _font, cellRect)
                    {
                        Position = cellColPos,
                        HoverColor = Color.Yellow,
                        ResourceLocked = true
                    };
                    btn.ID = b + 100;
                    btn.Click += BldgBtn_OnClick;
                    SelectionCells_BldgHouses_Btns.Add(btn);
                    // increment which building is currently drawn
                    b++;
                }
            }

            // Resource Building Buttons
            b = 0;
            for (int i = 0; i < (SelectionCell_GridDimensions.Y); i++)
            {
                var cellRowPos = initCellPos;
                cellRowPos += new Vector2(0, SelectionCell_Dimensions.Y * i) + new Vector2(0, SelectionCell_Spacing.Y * i);
                for (int j = 0; j < (SelectionCell_GridDimensions.X); j++)
                {
                    var cellColPos = cellRowPos + new Vector2(j * SelectionCell_Dimensions.X, 0) + new Vector2(j * SelectionCell_Spacing.X, 0);
                    var cellRect = new Rectangle((int)cellColPos.X, (int)cellColPos.Y, (int)SelectionCell_Dimensions.X, (int)SelectionCell_Dimensions.Y);
                    var btn = new Button(SelectionCells_BldgReso_Textures[b], _font, cellRect)
                    {
                        Position = cellColPos,
                        HoverColor = Color.Yellow,
                        ResourceLocked = true
                    };
                    btn.ID = b + 200;
                    btn.Click += BldgBtn_OnClick;
                    SelectionCells_BldgReso_Btns.Add(btn);
                    // increment which building is currently drawn
                    b++;
                }
            }

            // Decoration Building Buttons
            b = 0;
            for (int i = 0; i < (SelectionCell_GridDimensions.Y); i++)
            {
                var cellRowPos = initCellPos;
                cellRowPos += new Vector2(0, SelectionCell_Dimensions.Y * i) + new Vector2(0, SelectionCell_Spacing.Y * i);
                for (int j = 0; j < (SelectionCell_GridDimensions.X); j++)
                {
                    var cellColPos = cellRowPos + new Vector2(j * SelectionCell_Dimensions.X, 0) + new Vector2(j * SelectionCell_Spacing.X, 0);
                    var cellRect = new Rectangle((int)cellColPos.X, (int)cellColPos.Y, (int)SelectionCell_Dimensions.X, (int)SelectionCell_Dimensions.Y);
                    var btn = new Button(SelectionCells_BldgDeco_Textures[b], _font, cellRect)
                    {
                        Position = cellColPos,
                        HoverColor = Color.Yellow,
                        ResourceLocked = true
                    };
                    btn.ID = b + 300;
                    btn.Click += BldgBtn_OnClick;
                    SelectionCells_BldgDeco_Btns.Add(btn);
                    // increment which building is currently drawn
                    b++;
                }
            }
        }

        private void BldgBtn_OnClick(object sender, EventArgs e)
        {
            if (sender is Button convSender)
            {
                Console.WriteLine($"Button clicked: {BuildingData.Dict_BuildingKeys[convSender.ID].Name}");
                State.SelectedObject = BuildingData.Dict_BuildingKeys[convSender.ID];
            }
        }

        private void ViewBldgsBtn_OnClick(object sender, EventArgs e)
        {
            foreach (var b in BtnList_SelectBldgsView)
            {
                b.IsSelected = false;
            }
            if (sender is Button convSender)
            {
                string name = string.Empty;
                switch (convSender.ID)
                {
                    case 800:
                        name = "View Residential Bldgs";
                        break;
                    case 801:
                        name = "View Resource Bldgs";
                        break;
                    case 802:
                        name = "View Decoration Bldgs";
                        break;
                }

                SelectionCells_ViewIndex = convSender.ID;
                convSender.IsSelected = true;
                Console.WriteLine($"Button clicked: {name}");
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            _currentMouse = Mouse.GetState();
            var mouseRectangle = new Rectangle(_currentMouse.X, _currentMouse.Y, 1, 1);

            var show_spec_resource_toolip = false;
            var show_spec_resource_tooltip_id = 0;

            _HUD.Render();
            spriteBatch.Draw(_texture, DisplayRect, Color.DarkKhaki);
            spriteBatch.Draw(_texture, BorderRect, Color.Black);

            spriteBatch.Draw(SelectionCellsSection_Texture, SelectionCellsSection_Rectangle, Color.DarkKhaki);
            spriteBatch.Draw(InfographicsSection_Texture, InfographicsSection_Rectangle, Color.DarkOliveGreen);
            spriteBatch.Draw(DisplayInfo_Texture, DisplayInfo_Rectangle, Color.White);
            spriteBatch.Draw(ResourceBar_Texture, ResourceBar_Rectangle, Color.White);

            /**
             *      DRAW RESOURCE AND STATE DATA TO HUD
             **/
            if (State is null)
                return;

            #region DRAW RESOURCES
            string game_day = $"Day: {State.GSData.Day}";
            var game_day_origin = new Vector2(0, _font.MeasureString(game_day).Y / 2);
            var game_day_position = _position + new Vector2(5, 15);
            spriteBatch.DrawString(_font, game_day, game_day_position, Color.Black, 0.0f, game_day_origin, 1.5f, SpriteEffects.None, 1.0f);

            string game_year = $"Year: {State.GSData.Year}";
            var game_year_origin = new Vector2(0, _font.MeasureString(game_year).Y / 2);
            var game_year_position = game_day_position + new Vector2(0, _font.MeasureString(game_day).Y + 5);
            spriteBatch.DrawString(_font, game_year, game_year_position, Color.Black, 0.0f, game_year_origin, 1.5f, SpriteEffects.None, 1.0f);

            var resource_vals = new int[]
            {
                State.GSData.PlayerInventory.Money,
                State.GSData.PlayerInventory.Wood,
                State.GSData.PlayerInventory.Coal,
                State.GSData.PlayerInventory.Iron,
                State.GSData.PlayerInventory.Stone,
                State.GSData.PlayerInventory.Food,
                State.GSData.PlayerInventory.Energy,
                State.GSData.PlayerInventory.Workers
            };

            var resource_string_vals = new string[]
            {
                "Money", "Wood", "Coal", "Iron", "Stone", "Food", "Energy", "Workers"
            };

            var icon_start_pos = new Vector2(ResourceBar_Rectangle.X, ResourceBar_Rectangle.Y);
            var icon_scale = new Vector2(2, 2);

            // display inventory icons in hud
            for (int i = 0; i < HUDIconIDs.Length; i++)
            {
                var icon_id = HUDIconIDs[i];
                var icon_texture = _content.GetUiTexture(icon_id);
                var icon_position = icon_start_pos + new Vector2(ResourceCell_Dimension.X * i, 0);
                icon_position += new Vector2(ResourceCell_Dimension.X * 0.4f, ResourceCell_Dimension.Y * 0.5f);
                var icon_rect = new Rectangle((int)icon_position.X, (int)icon_position.Y, (int)(ResourceCell_Dimension
                        .X * 0.4f), (int)
                    (ResourceCell_Dimension.Y * 0.4f));
                spriteBatch.Draw(icon_texture, icon_rect, Color.White);

                if (icon_rect.Contains(_currentMouse.Position))
                {
                    show_spec_resource_toolip = true;
                    show_spec_resource_tooltip_id = i;
                }

                var resource_text = $"{resource_vals[i]}";
                var resource_text_origin = new Vector2(_font.MeasureString(resource_text).X / 2, _font.MeasureString(resource_text).Y / 2);
                var resource_text_pos = icon_position + new Vector2(icon_rect.Width * 0.5f, -(icon_rect.Height * 0.6f));

                spriteBatch.DrawString(_font, resource_text, resource_text_pos, Color.Black, 0.0f, resource_text_origin, 1.0f, SpriteEffects.None, 1.0f);
            }
            #endregion

            #region DRAW DIPLAY INFO SECTION 

            if (State.CurrentlySelectedTile != null && BuildingData.ValidBuilding(State.CurrentlySelectedTile.Object))
            {
                var obj = State.CurrentlySelectedTile.Object;
                var bldg = BuildingData.Dict_BuildingFromObjectID[obj.ObjectId];

                // display bldg name
                var name_x = _font.MeasureString(bldg.Name).X;
                var name_y = _font.MeasureString(bldg.Name).Y;
                var name_o = new Vector2(0, name_y / 2);
                var name_pos = new Vector2(DisplayInfo_Rectangle.X + (DisplayInfo_Rectangle.Width * 0.05f),
                    DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height * 0.05f) + name_y);
                spriteBatch.DrawString(_font, bldg.Name, name_pos, Color.Black, 0.0f, name_o, 1.0f, SpriteEffects.None,
                    1.0f);

                // display bldg icon
                var dist = (DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height * 0.8f)) - name_pos.Y;
                var offset = (dist * 0.5) / 2;
                var size = dist - (dist * 0.1);
                var bldg_txt_pos = new Vector2((int)DisplayInfo_Rectangle.X + (int)offset, (int)name_pos.Y + (int)offset);
                var bldg_txt_rect = new Rectangle((int)bldg_txt_pos.X, (int)bldg_txt_pos.Y, (int)size, (int)size);

                var bldg_txt = _content.GetTileTexture(bldg.TextureIndex);
                var src_rect = new Rectangle(0, (int)(bldg_txt.Height - (bldg_txt.Height * 0.3f)), (int)bldg_txt.Width,
                    (int)(bldg_txt.Height * 0.3f));
                spriteBatch.Draw(_content.GetTileTexture(State.CurrentlySelectedTile.Object.TextureIndex), destinationRectangle: bldg_txt_rect, sourceRectangle: src_rect, color: Color.White);
                DeleteBuildingBtn.Draw(gameTime, spriteBatch);
            }
            else if (State.CurrentlySelectedTile != null && BuildingData.ValidResource(State.CurrentlySelectedTile.Object) && State.CurrentlySelectedTile.Object.ObjectId == 10)
            {
                var obj = State.CurrentlySelectedTile.Object;
                var bldg = BuildingData.Dict_BuildingFromObjectID[obj.ObjectId];

                // display bldg name
                var name_x = _font.MeasureString("Farmland").X;
                var name_y = _font.MeasureString("Farmland").Y;
                var name_o = new Vector2(0, name_y / 2);
                var name_pos = new Vector2(DisplayInfo_Rectangle.X + (DisplayInfo_Rectangle.Width * 0.05f),
                    DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height * 0.05f) + name_y);
                spriteBatch.DrawString(_font, "Farmland", name_pos, Color.Black, 0.0f, name_o, 1.0f, SpriteEffects.None,
                    1.0f);

                // display bldg icon
                var dist = (DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height * 0.8f)) - name_pos.Y;
                var offset = (dist * 0.5) / 2;
                var size = dist - (dist * 0.1);
                var bldg_txt_pos = new Vector2((int)DisplayInfo_Rectangle.X + (int)offset, (int)name_pos.Y + (int)offset);
                var bldg_txt_rect = new Rectangle((int)bldg_txt_pos.X, (int)bldg_txt_pos.Y, (int)size, (int)size);

                var bldg_txt = _content.GetTileTexture(bldg.TextureIndex);
                var src_rect = new Rectangle(0, (int)(bldg_txt.Height - (bldg_txt.Height * 0.3f)), (int)bldg_txt.Width,
                    (int)(bldg_txt.Height * 0.3f));
                spriteBatch.Draw(_content.GetTileTexture(State.CurrentlySelectedTile.Object.TextureIndex), destinationRectangle: bldg_txt_rect, sourceRectangle: src_rect, color: Color.White);
                DeleteBuildingBtn.Draw(gameTime, spriteBatch);
            }
            else if (State.CurrentlySelectedTile != null && BuildingData.ValidResource(State.CurrentlySelectedTile.Object) && State.CurrentlySelectedTile.Object.ObjectId == 2)
            {
                var obj = State.CurrentlySelectedTile.Object;
                var bldg = BuildingData.Dict_BuildingFromObjectID[obj.ObjectId];

                // display bldg name
                var name_x = _font.MeasureString("Forest").X;
                var name_y = _font.MeasureString("Forest").Y;
                var name_o = new Vector2(0, name_y / 2);
                var name_pos = new Vector2(DisplayInfo_Rectangle.X + (DisplayInfo_Rectangle.Width * 0.05f),
                    DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height * 0.05f) + name_y);
                spriteBatch.DrawString(_font, "Forest", name_pos, Color.Black, 0.0f, name_o, 1.0f, SpriteEffects.None,
                    1.0f);
                string cost = "Costs 10 pounds to delete";
                var str_pos = new Vector2(DisplayInfo_Rectangle.X + (DisplayInfo_Rectangle.Width / 2) + 20, DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height / 2));
                var str_x = _font.MeasureString(cost).X;
                var str_y = _font.MeasureString(cost).Y;
                var str_o = new Vector2(str_x / 2, str_y / 2);
                spriteBatch.DrawString(_font, cost, str_pos, Color.Black, 0.0f, str_o, 0.5f, SpriteEffects.None, 1.0f);

                // display bldg icon
                var dist = (DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height * 0.8f)) - name_pos.Y;
                var offset = (dist * 0.5) / 2;
                var size = dist - (dist * 0.1);
                var bldg_txt_pos = new Vector2((int)DisplayInfo_Rectangle.X + (int)offset, (int)name_pos.Y + (int)offset);
                var bldg_txt_rect = new Rectangle((int)bldg_txt_pos.X, (int)bldg_txt_pos.Y, (int)size, (int)size);

                var bldg_txt = _content.GetTileTexture(bldg.TextureIndex);
                var src_rect = new Rectangle(0, (int)(bldg_txt.Height - (bldg_txt.Height * 0.3f)), (int)bldg_txt.Width,
                    (int)(bldg_txt.Height * 0.3f));
                spriteBatch.Draw(_content.GetTileTexture(State.CurrentlySelectedTile.Object.TextureIndex), destinationRectangle: bldg_txt_rect, sourceRectangle: src_rect, color: Color.White);
                DeleteBuildingBtn.Draw(gameTime, spriteBatch);
            }
            else
            {
                string nothing = "No building is selected";
                var str_pos = new Vector2(DisplayInfo_Rectangle.X + (DisplayInfo_Rectangle.Width / 2), DisplayInfo_Rectangle.Y + (DisplayInfo_Rectangle.Height / 2));
                var str_x = _font.MeasureString(nothing).X;
                var str_y = _font.MeasureString(nothing).Y;
                var str_o = new Vector2(str_x / 2, str_y / 2);
                spriteBatch.DrawString(_font, nothing, str_pos, Color.Black, 0.0f, str_o, 1.0f, SpriteEffects.None, 1.0f);
            }
            #endregion

            var show_tooltip = false;
            Button ref_b = null;
            ShowBldgBtnTooltip = false;
            ShowBldgBtnId = 0;

            foreach (Component c in _components)
            {
                c.Draw(gameTime, spriteBatch);
                if (c is Button b)
                {
                    if (b.IsHovering)
                    {
                        if (b.ID.Equals(800) || b.ID.Equals(801) || b.ID.Equals(802))
                        {
                            ShowBldgBtnTooltip = true;
                            ShowBldgBtnId = b.ID;
                        }
                    }
                }
            }

            // switch building page view
            var list = SelectionCells_BldgHouses_Btns;
            switch (SelectionCells_ViewIndex)
            {
                case 801:
                    list = SelectionCells_BldgReso_Btns;
                    break;
                case 802:
                    list = SelectionCells_BldgDeco_Btns;
                    break;
                default:
                    list = SelectionCells_BldgHouses_Btns;
                    break;
            }

            // foreach button in building page view
            foreach (var e in list)
            {
                e.Draw(gameTime, spriteBatch);
                if (e is Button b)
                {
                    if (b.IsHovering)
                    {
                        if (b.ResourceLocked)
                        {
                            if (BuildingData.Dict_BuildingKeys.ContainsKey(b.ID))
                            {
                                show_tooltip = true;
                                ref_b = b;
                            }
                        }
                    }
                }
            }

            if (show_tooltip)
            {
                var b = ref_b;
                var bld = BuildingData.Dict_BuildingKeys[b.ID];
                var cost_vals = new Dictionary<int, int>()
                                {
                                    {0, bld.MoneyUpfront},
                                    {1, bld.WoodUpfront},
                                    {2, bld.CoalUpfront},
                                    {3, bld.IronUpfront},
                                    {4, bld.StoneUpfront },
                                    {5, bld.WorkersUpfront},
                                    {6, bld.EnergyUpfront},
                                    {7, bld.FoodUpfront}
                                };
                var per_turn_cost_vals = new Dictionary<int, int>()
                {
                    {0, bld.MoneyCost},
                    {1, bld.WoodCost},
                    {2, bld.CoalCost},
                    {3, bld.IronCost},
                    {4, bld.StoneCost},
                    {5, bld.WorkersCost},
                    {6, bld.EnergyCost},
                    {7, bld.FoodCost}
                };
                var per_turn_gain_vals = new Dictionary<int, int>()
                {
                    {0, bld.MoneyOutput},
                    {1, bld.WoodOutput},
                    {2, bld.CoalOutput},
                    {3, bld.IronOutput},
                    {4, bld.StoneOutput},
                    {5, bld.WorkersOutput},
                    {6, bld.EnergyOutput},
                    {7, bld.FoodOutput}
                };
                var str_vals = new Dictionary<int, string>()
                                {
                                    {0, "Money"},
                                    {1, "wood"},
                                    {2, "coal"},
                                    {3, "iron"},
                                    {4, "stone" },
                                    {5, "workers"},
                                    {6, "energy"},
                                    {7, "food"}
                                };
                var bool_vals = new Dictionary<int, bool>()
                                {
                                    {0, false},
                                    {1, false},
                                    {2, false},
                                    {3, false},
                                    {4, false},
                                    {5, false},
                                    {6, false},
                                    {7, false }
                                };
                foreach (var d in cost_vals)
                {
                    switch (str_vals[d.Key])
                    {
                        case "Money":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Money >= d.Value;
                            break;
                        case "wood":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Wood >= d.Value;
                            break;
                        case "coal":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Coal >= d.Value;
                            break;
                        case "iron":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Iron >= d.Value;
                            break;
                        case "stone":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Stone >= d.Value;
                            break;
                        case "workers":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Workers >= d.Value;
                            break;
                        case "energy":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Energy >= d.Value;
                            break;
                        case "food":
                            bool_vals[d.Key] = State.GSData.PlayerInventory.Food >= d.Value;
                            break;
                    }
                }

                // set dimensions of tooltip
                var tooltip_rect = new Rectangle((int)_currentMouse.X, (int)(_currentMouse.Y - Tooltip_Dimensions.Y),
                    (int)Tooltip_Dimensions.X, (int)Tooltip_Dimensions.Y);

                // re position the tooltip if it is showing outside of the window
                if ((_currentMouse.X + tooltip_rect.Width + (ExtraTooltip_Dimensions.X * 3)) > _graphicsDevice.Viewport.Width)
                {
                    tooltip_rect.X = (int)(_graphicsDevice.Viewport.Width - (tooltip_rect.Width + (ExtraTooltip_Dimensions.X * 3)));
                }
                else if ((_currentMouse.X + tooltip_rect.Width + (ExtraTooltip_Dimensions.X * 3)) < 0)
                {
                    tooltip_rect.X = 0;
                }
                if ((_currentMouse.Y - (tooltip_rect.Height + ExtraTooltip_Dimensions.Y)) < 0)
                {
                    tooltip_rect.Y = 0;
                }
                else if ((_currentMouse.Y - (tooltip_rect.Height + ExtraTooltip_Dimensions.Y)) > _graphicsDevice.Viewport.Height)
                {
                    tooltip_rect.Y = _graphicsDevice.Viewport.Height - tooltip_rect.Height;
                }

                var bldg_name = bld.Name;

                // draw tooltip
                spriteBatch.Draw(Tooltip_Texture, destinationRectangle: tooltip_rect, color: Color.LightSlateGray);

                // draw resource strings in tooltip
                var complete_str = "";
                int res_strt = 0;
                foreach (var val in str_vals)
                {
                    var res_str = $"{val.Value}: ";
                    complete_str += res_str;

                    var res_pos = new Vector2(tooltip_rect.X, ((tooltip_rect.Y + Tooltip_Dimensions.Y) - ((_font.MeasureString(_baseString).Y + 10) * res_strt) - 5));
                    var origin = new Vector2(0, _font.MeasureString(res_str).Y);
                    spriteBatch.DrawString(_font, res_str, res_pos, (bool_vals[val.Key] ? Color.Black : Color.DarkRed), 0, origin, new Vector2(1, 1), SpriteEffects.None, 1);

                    // draw extra rec for display of per turn revenue for resource
                    var xtra_dimen = new Rectangle((int)tooltip_rect.X + (int)Tooltip_Dimensions.X, (int)((tooltip_rect.Y + tooltip_rect.Height) - (ExtraTooltip_Dimensions.Y * (res_strt + 1))), (int)ExtraTooltip_Dimensions.X * 3, (int)ExtraTooltip_Dimensions.Y);
                    spriteBatch.Draw(Tooltip_Texture, destinationRectangle: xtra_dimen, color: Color.LightGray);

                    string[] rev_strs = { $"-{cost_vals[val.Key]}", $"-{per_turn_cost_vals[val.Key]}", $"+{per_turn_gain_vals[val.Key]}" };
                    for (int i = 0; i < rev_strs.Length; i++)
                    {
                        var rev_str_pos = new Vector2(xtra_dimen.X + (i * (xtra_dimen.Width / 3)), res_pos.Y);
                        var rev_str_origin = new Vector2(0, _font.MeasureString(_baseString_PerTurnRevenue).Y);

                        var friendly_string = "";
                        var offset_cnt = _baseString_PerTurnRevenue.Length - rev_strs[i].Length;
                        for (int j = 0; j < offset_cnt; j++)
                        {
                            friendly_string += " ";
                        }
                        friendly_string += rev_strs[i];

                        spriteBatch.DrawString(_font, (rev_strs[i].Equals("-0") || rev_strs[i].Equals("+0") ? "" : friendly_string), rev_str_pos, (i.Equals(0) ? (bool_vals[val.Key] ? Color.Black : Color.Red) : (i.Equals(1) ? Color.Red : Color.Green)), 0, rev_str_origin, new Vector2(1, 1), SpriteEffects.None, 1);
                    }
                    res_strt++;
                }

                var xtra_tooltip_rect = new Rectangle((int)tooltip_rect.X, (int)((tooltip_rect.Y + tooltip_rect.Height) - (Tooltip_Dimensions.Y + (ExtraTooltip_Dimensions.Y * 1.6f))),
                    (int)Tooltip_Dimensions.X + (int)(ExtraTooltip_Dimensions.X * 3), (int)(ExtraTooltip_Dimensions.Y * 1.6f));

                spriteBatch.Draw(Tooltip_Texture, destinationRectangle: xtra_tooltip_rect, color: Color.DarkSlateGray);

                var head_str_pos = new Vector2(xtra_tooltip_rect.X, xtra_tooltip_rect.Y + xtra_tooltip_rect.Height);
                var head_str_origin = new Vector2(0, _font.MeasureString(_baseString_Header).Y);

                var name_str_pos = new Vector2(xtra_tooltip_rect.X + (xtra_tooltip_rect.Width / 2), xtra_tooltip_rect.Y + (_font.MeasureString(bldg_name).Y * 0.8f));
                var name_str_origin =
                    new Vector2(_font.MeasureString(bldg_name).X / 2, _font.MeasureString(bldg_name).Y / 2);

                spriteBatch.DrawString(_font, _baseString_Header, head_str_pos, Color.White, 0f, head_str_origin, new Vector2(0.9f, 0.9f), SpriteEffects.None, 1);
                spriteBatch.DrawString(_font, bldg_name, name_str_pos, Color.White, 0f, name_str_origin, new Vector2(0.9f, 0.9f), SpriteEffects.None, 1);
            }

            if (show_spec_resource_toolip)
            {
                // build string
                var spec_res_tooltip_txt = $"{resource_string_vals[show_spec_resource_tooltip_id]}'s current amount is {resource_vals[show_spec_resource_tooltip_id]} (+{CurrentIncomeReport.TotalGain[show_spec_resource_tooltip_id]} -{CurrentIncomeReport.TotalLoss[show_spec_resource_tooltip_id]} / per turn)";

                // calculate dimensions
                var spec_res_tooltip_dimens = new Vector2(_font.MeasureString(spec_res_tooltip_txt).X, _font.MeasureString(spec_res_tooltip_txt).Y);
                var spec_res_tooltip_rect = new Rectangle((int)_currentMouse.X, (int)(_currentMouse.Y - spec_res_tooltip_dimens.Y),
                    (int)spec_res_tooltip_dimens.X, (int)spec_res_tooltip_dimens.Y);
                spec_res_tooltip_rect.X = _currentMouse.X - (spec_res_tooltip_rect.Width / 2);
                Vector2 spec_res_tooltip_txt_pos = new Vector2(spec_res_tooltip_rect.X + (spec_res_tooltip_rect.Width / 2), spec_res_tooltip_rect.Y + _font.MeasureString(spec_res_tooltip_txt).Y);

                // draw tooltip and string
                spriteBatch.Draw(Tooltip_Texture, destinationRectangle: spec_res_tooltip_rect, color: Color.LightGray);
                spriteBatch.DrawString(_font, spec_res_tooltip_txt, spec_res_tooltip_txt_pos, Color.Black, 0, new Vector2(_font.MeasureString(spec_res_tooltip_txt).X / 2, _font.MeasureString(spec_res_tooltip_txt).Y), new Vector2(0.9f, 0.9f), SpriteEffects.None, 1);
            }

            if (ShowBldgBtnTooltip && ShowBldgBtnId != 0)
            {
                var str = string.Empty;
                switch (ShowBldgBtnId)
                {
                    case 800:
                        str = Btn_SelectHouses_String;
                        break;
                    case 801:
                        str = Btn_SelectReso_String;
                        break;
                    case 802:
                        str = Btn_SelectDeco_String;
                        break;
                }
                var ref_tooltip_dimens = new Vector2(_font.MeasureString(str).X, _font.MeasureString(str).Y);
                var ref_tooltip_rect = new Rectangle((int)_currentMouse.X,
                    (int)(_currentMouse.Y - ref_tooltip_dimens.Y),
                    (int)ref_tooltip_dimens.X, (int)ref_tooltip_dimens.Y);
                ref_tooltip_rect.X = _currentMouse.X - (ref_tooltip_rect.Width / 2);
                Vector2 ref_tooltip_txt_pos = new Vector2(ref_tooltip_rect.X + (ref_tooltip_rect.Width / 2), ref_tooltip_rect.Y + _font.MeasureString(str).Y);

                spriteBatch.Draw(Tooltip_Texture, destinationRectangle: ref_tooltip_rect, color: Color.LightGray);
                spriteBatch.DrawString(_font, str, ref_tooltip_txt_pos, Color.Black, 0, new Vector2(_font.MeasureString(str).X / 2, _font.MeasureString(str).Y), new Vector2(0.9f, 0.9f), SpriteEffects.None, 1);
            }
        }

        public override void Update(GameTime gameTime, GameState state)
        {
            foreach (Component c in _components)
            {
                c.Update(gameTime, state);
            }

            var list = SelectionCells_BldgHouses_Btns;
            switch (SelectionCells_ViewIndex)
            {
                case 801:
                    list = SelectionCells_BldgReso_Btns;
                    break;
                case 802:
                    list = SelectionCells_BldgDeco_Btns;
                    break;
                default:
                    list = SelectionCells_BldgHouses_Btns;
                    break;
            }

            foreach (var e in list)
            {
                e.Update(gameTime, state);
            }

            DeleteBuildingBtn.Update(gameTime, state);

            // save gamestate
            State = state;
        }
    }
}