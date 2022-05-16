
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerScienceCoursework.States;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerScienceCoursework.UI
{
    public abstract class Component
    {
        public bool Disposed = false;

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

        public abstract void Update(GameTime gameTime, GameState state);
    }
}