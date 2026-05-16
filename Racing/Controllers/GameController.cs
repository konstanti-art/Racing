using System.Collections.Generic;
using System.Windows.Forms;
using Racing.Models;

namespace Racing.Controllers
{
    public class GameController
    {
        private readonly GameEngine _engine;
        private readonly Form _view;
        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        public GameController(GameEngine engine, Form view)
        {
            _engine = engine;
            _view = view;
        }

        public void HandleKeyDown(Keys key)
        {
            if (key == Keys.P) _engine.IsPaused = !_engine.IsPaused;

            if (key == Keys.R && _engine.IsGameOver)
            {
                _engine.ResetGame();
            }

            if (!_pressedKeys.Contains(key)) _pressedKeys.Add(key);
        }

        public void HandleKeyUp(Keys key) => _pressedKeys.Remove(key);

        public void ExecuteTick()
        {
            if (!_engine.IsGameOver && !_engine.IsPaused)
            {
                float dx = 0, dy = 0;

                if (_pressedKeys.Contains(Keys.Left) || _pressedKeys.Contains(Keys.A)) dx = -9;
                if (_pressedKeys.Contains(Keys.Right) || _pressedKeys.Contains(Keys.D)) dx = 9;
                if (_pressedKeys.Contains(Keys.Up) || _pressedKeys.Contains(Keys.W)) dy = -7;
                if (_pressedKeys.Contains(Keys.Down) || _pressedKeys.Contains(Keys.S)) dy = 7;

                if (dx != 0 || dy != 0) _engine.MovePlayer(dx, dy);
                if (_pressedKeys.Contains(Keys.Space)) _engine.Shoot();

                _engine.UpdateTick();
            }
            _view.Invalidate();
        }
    }
}