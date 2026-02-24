using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace MapProcessing
{
    internal class Map
    {
        public Dictionary<int, Creature> CreaturesHash { get; set; }
        public int Size { get; init; }
        private readonly List<Creature> deadCreatures = new List<Creature>();
        private readonly List<Creature> eatenCreatures = new List<Creature>();
        public List<Dictionary<int, int>> AreaSnapShot = new List<Dictionary<int, int>>();
        public Dictionary<int, int> CurrentArea = new Dictionary<int, int>();
        private int fertility;
        public Map(int size)
        {
            Size = size;
            fertility = (int)(Math.Pow(size, 2) * 0.1 / Plant.DefaultLifeSpan );
            CreaturesHash = new Dictionary<int, Creature>();
        }

        public void Start(int term)
        {
            int grazerCount = 1;
            CreateGrazer(1);

            for (int i = 0; i < term; i++)
            {
                CreatePlant();
                Next();
                ClearDead();
                SnapShotArea();
            }
        }

        private void CreateGrazer(int quantity)
        {
            Random random = new Random();

            for (int i = 0; i < quantity; i++)
            {
                var x = 0;
                var y = 0;
                do
                {
                    x = random.Next(0, Size);
                    y = random.Next(0, Size);
                } while (!IsCellFree(y * Size + x, Grazer.DefaultSize));

                var grazer = new Grazer(new Point(x, y));
                CreaturesHash[y * Size + x] = grazer;
                FillArea(grazer);
            }

        }

        private bool IsCellFree(int index, int size)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (CurrentArea.ContainsKey(index + y * Size + x))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void Next()
        {
            foreach (var creature in CreaturesHash)
            {
                if (creature.Value.Speed > 0)
                {
                    MoveCreature(creature.Value);
                }
                OldCreature(creature.Value);
            }
        }

        private void CreatePlant()
        {
            Random random = new Random();

            for (int i = 0; i < fertility; i++)
            {
                var x = 0;
                var y = 0;
                do
                {
                    x = random.Next(0, Size);
                    y = random.Next(0, Size);
                            
                } while (!IsCellFree(y * Size + x, Plant.DefaultSize));

                var plant = new Plant(new Point(x, y));

                CreaturesHash[y * Size + x] = plant;
                FillArea(plant);
            }
        }

        private void FillArea(Creature creature)
        {
            for (int y = 0; y < creature.Size; y++)
            {
                for (int x = 0; x < creature.Size; x++)
                {
                    CurrentArea[(creature.Location.Y + y) * Size + (creature.Location.X + x)] = (int)creature.Type;
                }
            }
        }

        private void ClearArea(Creature creature)
        {
            for (int y = 0; y < creature.Size; y++)
            {
                for (int x = 0; x < creature.Size; x++)
                {
                    CurrentArea.Remove((creature.Location.Y + y) * Size + creature.Location.X + x);
                }
            }
        }

        private void ClearDead()
        {
            deadCreatures.ForEach(dc =>
            {
                CreaturesHash.Remove(dc.Location.Y * Size + dc.Location.X);
                ClearArea(dc);
            });

            deadCreatures.Clear();
        }

        private void MoveCreature(Creature creature)
        {
            ClearArea(creature);
            creature.Location = GetNewPosition(creature);
            if (creature is Grazer grazer)
            {
                Grazing(grazer);
            }
            FillArea(creature);
        }

        private void Grazing(Grazer grazer)
        {
            for (int y = 0; y < grazer.Size; y++)
            {
                for (int x = 0; x < grazer.Size; x++)
                {
                    var cellIndex = (grazer.Location.Y + y) * Size + grazer.Location.X + x;
                    if (CurrentArea.ContainsKey(cellIndex) && CurrentArea[cellIndex] == (int)CellType.Plant)
                    {
                        var eatenPlant = CreaturesHash[cellIndex];
                        eatenCreatures.Add(CreaturesHash[cellIndex]);
                        grazer.Eat(eatenPlant);
                    }
                }
            }
        }

        private Point GetNewPosition(Creature creature)
        {
            var random = new Random();
            var directionX = random.Next(-1, 2);
            var directionY = random.Next(-1, 2);
            var newX = creature.Location.X + directionX * creature.Speed;
            var newY = creature.Location.Y + directionY * creature.Speed;

            return new Point(Math.Clamp(newX, 0, Size - creature.Size), Math.Clamp(newY, 0, Size - creature.Size));
        }

        private void OldCreature(Creature creature)
        {
            creature.Age++;
            if (creature.Dead)
            {
                this.deadCreatures.Add(creature);
            }
        }

        private void SnapShotArea()
        {
            AreaSnapShot.Add(new Dictionary<int, int>(CurrentArea));
        }
    }
}
