using System;
using System.Drawing;
using System.Windows.Forms;
using Racing.Models;
using Racing.Controllers;

namespace Racing.Views
{
    public partial class Form1 : Form
    {
        private const int OFFSET_X = 250; // Ширина левой панели
        private GameEngine _model;
        private GameController _controller;
        private Timer _gameTimer;
        private int _roadY = 0;
        private Point _lastMouse;

        private Image imgCar;
        private Image imgCarOncoming;
        private Image imgCrush;
        private Image imgCrushOncoming;
        private Image imgRoad;
        private Image imgBullet;
        private Image imgBarrel;

        public Form1()
        {
            // Настройка окна
            this.Size = new Size(850, 800);
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;

            // Предварительная загрузка
            PrepareGraphics();

            // Инициализация логики
            _model = new GameEngine();
            _controller = new GameController(_model, this);
            _model.OnHit += ShakeWindow;

            // Основной игровой цикл
            _gameTimer = new Timer { Interval = 20 };
            _gameTimer.Tick += (s, e) => _controller.ExecuteTick();
            _gameTimer.Start();

            // Обработка ввода
            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) Application.Exit();
                _controller.HandleKeyDown(e.KeyCode);
            };
            this.KeyUp += (s, e) => _controller.HandleKeyUp(e.KeyCode);

            // Перетаскивание окна мышкой
            this.MouseDown += (s, e) => _lastMouse = e.Location;
            this.MouseMove += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    this.Left += e.X - _lastMouse.X;
                    this.Top += e.Y - _lastMouse.Y;
                }
            };
        }

        private void PrepareGraphics()
        {
            try
            {
                imgRoad = Racing.Properties.Resources.road;
                imgBullet = Racing.Properties.Resources.bullet_img;
                imgBarrel = Racing.Properties.Resources.barrel;

                // Кэшируем обычную машину и встречную
                imgCar = Racing.Properties.Resources.car;
                imgCarOncoming = (Image)imgCar.Clone();
                imgCarOncoming.RotateFlip(RotateFlipType.Rotate180FlipNone);

                // Кэшируем разбитую машину и встречную разбитую
                imgCrush = Racing.Properties.Resources.CrushCar;
                imgCrushOncoming = (Image)imgCrush.Clone();
                imgCrushOncoming.RotateFlip(RotateFlipType.Rotate180FlipNone);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки ресурсов: " + ex.Message);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;

            // Анимация дороги
            if (!_model.IsPaused && !_model.IsGameOver)
                _roadY = (_roadY + 8) % 800;

            // Дорога
            g.DrawImage(imgRoad, OFFSET_X, _roadY, 600, 800);
            g.DrawImage(imgRoad, OFFSET_X, _roadY - 800, 600, 800);

            // Патроны
            foreach (var p in _model.AmmoPickups)
                g.DrawImage(imgBullet, p.X + OFFSET_X, p.Y, p.Width, p.Height);

            // Бочки
            foreach (var bar in _model.Barrels)
                g.DrawImage(imgBarrel, bar.X + OFFSET_X, bar.Y, bar.Width, bar.Height);

            // Трафик
            foreach (var t in _model.Traffic)
            {
                Image target;
                if (t.IsCrashed)
                    target = t.IsOncoming ? imgCrushOncoming : imgCrush;
                else
                    target = t.IsOncoming ? imgCarOncoming : imgCar;

                g.DrawImage(target, t.Rect.X + OFFSET_X, t.Rect.Y, t.Rect.Width, t.Rect.Height);
            }

            // Пули игрока
            foreach (var b in _model.Bullets)
                g.DrawImage(imgBullet, b.X + OFFSET_X, b.Y, b.Width, b.Height);

            // Машина игрока
            g.DrawImage(imgCar, _model.Car.X + OFFSET_X, _model.Car.Y, _model.Car.Width, _model.Car.Height);

            DrawLeftSidebar(g);

            // Пауза / Конец игры
            if (_model.IsPaused) DrawOverlay(g, "ПАУЗА", "Нажмите 'P', чтобы продолжить");
            if (_model.IsGameOver) DrawOverlay(g, "ИГРА ОКОНЧЕНА", $"Ваш счет: {_model.Score}\nРекорд: {_model.HighScore}\n\nНажмите 'R' для ПЕРЕЗАПУСКА");
        }

        private void DrawLeftSidebar(Graphics g)
        {
            // Фон панели
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 30, 30)), 0, 0, OFFSET_X, 800);
            g.DrawLine(new Pen(Color.Gold, 3), OFFSET_X, 0, OFFSET_X, 800);

            Font titleFont = new Font("Segoe UI", 20, FontStyle.Bold);
            Font statFont = new Font("Segoe UI", 16, FontStyle.Bold);
            Font labelFont = new Font("Segoe UI", 10);

            // Рекорд
            g.DrawString("РЕКОРД", labelFont, Brushes.Gray, 25, 30);
            g.DrawString($"{_model.HighScore}", statFont, Brushes.Gold, 25, 50);

            // Текущий счет
            g.DrawString("СЧЕТ", labelFont, Brushes.Gray, 25, 120);
            g.DrawString($"{_model.Score}", titleFont, Brushes.White, 25, 140);

            // Жизни
            g.DrawString("ЖИЗНИ", labelFont, Brushes.Gray, 25, 230);
            string hearts = new string('❤', Math.Max(0, _model.Lives));
            g.DrawString(hearts, statFont, Brushes.Tomato, 25, 250);

            // Патроны
            g.DrawString("ПАТРОНЫ", labelFont, Brushes.Gray, 25, 320);
            g.DrawString($"{_model.Ammo}", statFont, Brushes.SpringGreen, 25, 340);

            // Подсказки управления
            int hintY = 580;
            g.DrawString("УПРАВЛЕНИЕ", labelFont, Brushes.DarkGray, 25, hintY);
            Font hintFont = new Font("Segoe UI", 9);
            string controlText = "WASD / Стрелки - Движение\n" +
                                 "SPACE - Стрелять\n" +
                                 "P - Пауза\n" +
                                 "R - Рестарт (после смерти)\n" +
                                 "ESC - Выход";
            g.DrawString(controlText, hintFont, Brushes.LightGray, 25, hintY + 25);
        }

        private void DrawOverlay(Graphics g, string title, string subtitle)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(190, 0, 0, 0)), OFFSET_X, 0, 600, 800);

            using (Font fTitle = new Font("Arial", 35, FontStyle.Bold))
            using (Font fSub = new Font("Arial", 14))
            {
                g.DrawString(title, fTitle, Brushes.White, OFFSET_X + 120, 300);
                g.DrawString(subtitle, fSub, Brushes.LightGray, OFFSET_X + 100, 380);
            }
        }

        private void ShakeWindow()
        {
            Point origin = this.Location;
            Random rnd = new Random();
            for (int i = 0; i < 6; i++)
            {
                this.Location = new Point(origin.X + rnd.Next(-10, 11), origin.Y + rnd.Next(-10, 11));
                System.Threading.Thread.Sleep(15);
            }
            this.Location = origin;
        }
    }
}