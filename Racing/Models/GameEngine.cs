using System;
using System.Collections.Generic;
using System.Drawing;

namespace Racing.Models
{
    public class TrafficCar
    {
        public RectangleF Rect;
        public float Speed;
        public bool IsOncoming;
        public bool IsCrashed;

        public TrafficCar(float x, float y, float speed, bool isOncoming)
        {
            Rect = new RectangleF(x, y, 65, 110);
            Speed = speed;
            IsOncoming = isOncoming;
            IsCrashed = false;
        }
    }

    public class GameEngine
    {
        private const float ROAD_SPEED = 8.0f;

        public RectangleF Car { get; private set; }
        public List<RectangleF> Barrels { get; } = new List<RectangleF>();
        public List<RectangleF> Bullets { get; } = new List<RectangleF>();
        public List<RectangleF> AmmoPickups { get; } = new List<RectangleF>();
        public List<TrafficCar> Traffic { get; } = new List<TrafficCar>();

        public int Score { get; private set; }
        public int Lives { get; private set; } = 3;
        public int Ammo { get; private set; } = 0;
        public bool IsGameOver { get; private set; }
        public bool IsPaused { get; set; }
        public int HighScore { get; private set; }

        public event Action OnHit;
        private Random rng = new Random();
        private int frameCount = 0;
        private int shootCooldown = 0;

        private readonly int[] lanes = { 80, 185, 340, 445 };

        public GameEngine()
        {
            Car = new RectangleF(270, 550, 60, 100);
        }

        public void MovePlayer(float dx, float dy)
        {
            if (IsGameOver || IsPaused) return;
            float newX = Car.X + dx;
            float newY = Car.Y + dy;

            if (newX < 50) newX = 50;
            if (newX > 490) newX = 490;
            if (newY < 50) newY = 50;
            if (newY > 650) newY = 650;

            Car = new RectangleF(newX, newY, Car.Width, Car.Height);
        }

        public void UpdateTick()
        {
            if (IsGameOver || IsPaused) return;
            frameCount++;
            if (shootCooldown > 0) shootCooldown--;

            SpawnTraffic();
            SpawnItems();
            MoveItems();
            CheckCollisions();
        }

        private void SpawnTraffic()
        {
            if (frameCount % 60 == 0)
            {
                int laneIndex = rng.Next(0, 4);
                float x = lanes[laneIndex];
                bool isOncoming = laneIndex < 2;
                float speed = isOncoming ? rng.Next(12, 16) : rng.Next(3, 6);

                Traffic.Add(new TrafficCar(x, -150, speed, isOncoming));
            }
        }

        private void MoveItems()
        {
            foreach (var t in Traffic)
            {
                t.Rect = new RectangleF(t.Rect.X, t.Rect.Y + t.Speed, t.Rect.Width, t.Rect.Height);
            }
            Traffic.RemoveAll(t => t.Rect.Y > 850);


            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                Bullets[i] = new RectangleF(Bullets[i].X, Bullets[i].Y - 15, 15, 25);
                if (Bullets[i].Y < -100) Bullets.RemoveAt(i);
            }

            for (int i = Barrels.Count - 1; i >= 0; i--)
            {
                Barrels[i] = new RectangleF(Barrels[i].X, Barrels[i].Y + ROAD_SPEED, 50, 50);
                if (Barrels[i].Y > 850) Barrels.RemoveAt(i);
            }
            for (int i = AmmoPickups.Count - 1; i >= 0; i--)
            {
                AmmoPickups[i] = new RectangleF(AmmoPickups[i].X, AmmoPickups[i].Y + ROAD_SPEED, 30, 40);
                if (AmmoPickups[i].Y > 850) AmmoPickups.RemoveAt(i);
            }
        }

        private void CheckCollisions()
        {
            // 1 ПУЛИ и БОЧКИ
            for (int i = Barrels.Count - 1; i >= 0; i--)
            {
                for (int j = Bullets.Count - 1; j >= 0; j--)
                {
                    if (i < Barrels.Count && Bullets[j].IntersectsWith(Barrels[i]))
                    {
                        Bullets.RemoveAt(j);
                        Barrels.RemoveAt(i);
                        Score += 50;
                    }
                }
            }

            // 2 ПУЛИ и ТРАФИК
            for (int i = Traffic.Count - 1; i >= 0; i--)
            {
                for (int j = Bullets.Count - 1; j >= 0; j--)
                {
                    if (i < Traffic.Count && Bullets[j].IntersectsWith(Traffic[i].Rect))
                    {
                        Bullets.RemoveAt(j);
                        Traffic.RemoveAt(i);
                        Score += 100;
                    }
                }
            }

            // 3 ТРАФИК и БОЧКИ
            foreach (var t in Traffic)
            {
                if (t.IsCrashed) continue;
                for (int i = Barrels.Count - 1; i >= 0; i--)
                {
                    if (t.Rect.IntersectsWith(Barrels[i]))
                    {
                        t.IsCrashed = true;
                        t.Speed = ROAD_SPEED;
                        Barrels.RemoveAt(i);
                        break;
                    }
                }
            }

            // 4 ТРАФИК между собой
            for (int i = 0; i < Traffic.Count; i++)
            {
                for (int j = i + 1; j < Traffic.Count; j++)
                {
                    if (Traffic[i].IsCrashed && Traffic[j].IsCrashed) continue;
                    if (Traffic[i].Rect.IntersectsWith(Traffic[j].Rect))
                    {
                        Traffic[i].IsCrashed = true;
                        Traffic[i].Speed = ROAD_SPEED;
                        Traffic[j].IsCrashed = true;
                        Traffic[j].Speed = ROAD_SPEED;
                    }
                }
            }

            // 5 ИГРОК и ТРАФИК
            for (int i = Traffic.Count - 1; i >= 0; i--)
            {
                if (Car.IntersectsWith(Traffic[i].Rect))
                {
                    Lives--;
                    Traffic.RemoveAt(i);
                    OnHit?.Invoke();
                    if (Lives <= 0) EndGame();
                }
            }

            // 6 ИГРОК и БОЧКИ
            for (int i = Barrels.Count - 1; i >= 0; i--)
            {
                if (Car.IntersectsWith(Barrels[i]))
                {
                    Lives--;
                    Barrels.RemoveAt(i);
                    OnHit?.Invoke();
                    if (Lives <= 0) EndGame();
                }
            }

            // 7 ИГРОК и ПАТРОНЫ
            for (int i = AmmoPickups.Count - 1; i >= 0; i--)
            {
                if (Car.IntersectsWith(AmmoPickups[i]))
                {
                    Ammo += 5;
                    AmmoPickups.RemoveAt(i);
                }
            }
        }

        private void EndGame()
        {
            IsGameOver = true;
            if (Score > HighScore) HighScore = Score;
        }

        private void SpawnItems()
        {
            if (frameCount % 100 == 0) Barrels.Add(new RectangleF(rng.Next(60, 480), -100, 50, 50));
            if (frameCount % 300 == 0) AmmoPickups.Add(new RectangleF(rng.Next(60, 480), -100, 30, 40));
        }

        public void Shoot()
        {
            if (IsGameOver || IsPaused || Ammo <= 0 || shootCooldown > 0) return;
            Bullets.Add(new RectangleF(Car.X + 22, Car.Y - 20, 15, 25));
            Ammo--;
            shootCooldown = 12;
        }

        public void ResetGame()
        {
            if (Score > HighScore) HighScore = Score;

            Score = 0;
            Lives = 3;
            Ammo = 0;
            IsGameOver = false;
            IsPaused = false;

            Car = new RectangleF(270, 550, 60, 100);
            Barrels.Clear();
            Bullets.Clear();
            AmmoPickups.Clear();
            Traffic.Clear();
        }
    }
}