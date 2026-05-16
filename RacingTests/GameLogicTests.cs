using Microsoft.VisualStudio.TestTools.UnitTesting;
using Racing.Models;
using System.Drawing;
using System.Linq;

namespace RacingTests
{
    [TestClass]
    public class GameEngineTests
    {
        private GameEngine _engine;

        [TestInitialize]
        public void Setup()
        {
            _engine = new GameEngine();
        }

        // 1 движения игрока
        [TestMethod]
        public void MovePlayer_UpdatesPosition_WithinBoundaries()
        {
            float initialX = _engine.Car.X;

            _engine.MovePlayer(10, 0);
            Assert.AreEqual(initialX + 10, _engine.Car.X);

            _engine.MovePlayer(1000, 0);
            Assert.AreEqual(490, _engine.Car.X);
        }

        // 2 стрельбы и расхода патронов
        [TestMethod]
        public void Shoot_ConsumesAmmo_AndCreatesBullet()
        {
            _engine.AmmoPickups.Add(new RectangleF(_engine.Car.X, _engine.Car.Y, 20, 20));
            _engine.UpdateTick();

            int ammoBefore = _engine.Ammo;
            _engine.Shoot();

            Assert.AreEqual(ammoBefore - 1, _engine.Ammo);
            Assert.AreEqual(1, _engine.Bullets.Count);
        }

        // 3 Пуля уничтожает бочку
        [TestMethod]
        public void Collision_BulletHitsBarrel_IncreasesScore()
        {
            _engine.Barrels.Add(new RectangleF(100, 100, 50, 50));
            _engine.Bullets.Add(new RectangleF(105, 105, 10, 10));

            _engine.UpdateTick();

            Assert.AreEqual(50, _engine.Score);
            Assert.AreEqual(0, _engine.Barrels.Count);
        }

        // 4 Трафик врезается в бочку
        [TestMethod]
        public void Collision_TrafficHitsBarrel_CrashesTraffic()
        {
            var tCar = new TrafficCar(100, 100, 15, true);
            _engine.Traffic.Add(tCar);
            _engine.Barrels.Add(new RectangleF(105, 105, 50, 50));

            _engine.UpdateTick();

            Assert.IsTrue(tCar.IsCrashed, "Машина должна получить статус Crashed");
            Assert.AreEqual(8.0f, tCar.Speed, "Скорость разбитой машины должна стать равной скорости дороги");
            Assert.AreEqual(0, _engine.Barrels.Count, "Бочка должна исчезнуть");
        }

        // 5 Трафик врезается в трафик
        [TestMethod]
        public void Collision_TrafficHitsTraffic_BothCrash()
        {
            var car1 = new TrafficCar(100, 100, 5, false);
            var car2 = new TrafficCar(105, 105, 15, true);
            _engine.Traffic.Add(car1);
            _engine.Traffic.Add(car2);

            _engine.UpdateTick();

            Assert.IsTrue(car1.IsCrashed);
            Assert.IsTrue(car2.IsCrashed);
        }

        // 6 Рекод
        [TestMethod]
        public void GameOver_UpdatesHighScore()
        {
            for (int i = 0; i < 10; i++)
            {
                _engine.Barrels.Add(new RectangleF(100, 100, 50, 50));
                _engine.Bullets.Add(new RectangleF(105, 105, 10, 10));
                _engine.UpdateTick();
            }

            _engine.MovePlayer(0, 0);
            for (int i = 0; i < 3; i++)
            {
                _engine.Barrels.Add(new RectangleF(_engine.Car.X, _engine.Car.Y, 50, 50));
                _engine.UpdateTick();
            }

            Assert.IsTrue(_engine.IsGameOver);
            Assert.AreEqual(500, _engine.HighScore);
        }

        // 7 Перезапуск
        [TestMethod]
        public void ResetGame_ClearsEverything_ExceptHighScore()
        {
            _engine.MovePlayer(50, 50);
            _engine.Barrels.Add(new RectangleF(10, 10, 10, 10));

            _engine.ResetGame();

            Assert.AreEqual(0, _engine.Score);
            Assert.AreEqual(3, _engine.Lives);
            Assert.AreEqual(0, _engine.Barrels.Count);
            Assert.IsFalse(_engine.IsGameOver);
        }
    }
}