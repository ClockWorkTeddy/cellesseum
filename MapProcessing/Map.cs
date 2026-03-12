using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace MapProcessing
{
    public class Map
    {
        public Dictionary<Guid, Creature> CreaturesHash { get; set; }
        public int Size { get; init; }
        private readonly List<Creature> deadCreatures = new List<Creature>();
        private readonly List<Creature> eatenCreatures = new List<Creature>();
        public List<Dictionary<int, int>> AreaSnapShot = new List<Dictionary<int, int>>();
        public Dictionary<int, int> CurrentArea = new Dictionary<int, int>();
        public int Epoche = 0;
        private int fertility;
        private Dictionary<int, Plant> plantHash = new Dictionary<int, Plant>();
        private Dictionary<Guid, Grazer> grazerHash = new Dictionary<Guid, Grazer>();
        private int term = 0;
        public Map(int size, Dictionary<Guid, Creature> creaturesHash)
        {
            Size = size;
            fertility = (int)(Math.Pow(size, 2) * 0.1 / Plant.DefaultLifeSpan );
            CreaturesHash = creaturesHash;
        }

        public void Start(int term)
        {
            int grazerCount = 1;
            CreateGrazer(1);

            for (int i = 0; i < term && grazerHash.Count > 0; i++)
            {
                this.term = i;
                Next();
                SnapShotArea();
                Epoche++;
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
                    y = Math.Clamp(y%2 == 0 ? y : y -1, 0, Size - 1);
                    x = Math.Clamp(x%2 == 0 ? x : x -1, 0, Size - 1);
                } while (!IsCellFreeForGrazer(y * Size + x, Grazer.DefaultSize));

                var guid = Guid.NewGuid();
                var grazer = new Grazer(new Point(x, y), guid);
                CreaturesHash[guid] = grazer;
                grazerHash[guid] = grazer;
                Grazing(grazer);
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

        private bool IsCellFreeForGrazer(int index, int size)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (CurrentArea.ContainsKey(index + y * Size + x) && CurrentArea[index + y * Size + x] == 2)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void Next()
        {
            if (this.term < 200)
                CreatePlant();

            var newGrazers = new List<Grazer>();
            foreach (var grazer in grazerHash)
            {
                if (grazer.Value.Satiety > grazer.Value.LifeSpan / 2)
                {
                    var newLocation = GetNewPosition(grazer.Value);
                    var guid = Guid.NewGuid();
                    var newGrazer = new Grazer(newLocation, guid);
                    CreaturesHash[guid] = newGrazer;
                    newGrazers.Add(newGrazer);
                    FillArea(newGrazer);
                }
            }
            foreach (var newGrazer in newGrazers)
            {
                grazerHash[newGrazer.Id] = newGrazer;
            }
            foreach (var creature in CreaturesHash)
            {
                if (creature.Value.Speed > 0)
                {
                    MoveCreature(creature.Value);
                }
                if (creature.Value is Grazer grazer)
                {
                    grazer.Starve();
                    if (grazer.Satiety > grazer.LifeSpan / 2)
                    {
                        var newLocation = GetNewPosition(grazer);

                    }
                }
                Starve(creature);
                OldCreature(creature.Value);
            }
            ClearDead();
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

                var guid = Guid.NewGuid();
                var plant = new Plant(new Point(x, y), guid);
                CreaturesHash[guid] = plant;
                plantHash[y * Size + x] = plant;
                FillArea(plant);
            }
        }

        private void Starve(KeyValuePair<Guid, Creature> creature)
        {
            if (creature.Value is Grazer grazer)
            {
                grazer.Satiety--;
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
                CreaturesHash.Remove(dc.Id);
                if (dc is Plant plant)
                {
                    plantHash.Remove(plant.Location.Y * Size + plant.Location.X);
                }
                else
                {
                    ;
                }
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
                        var eatenPlant = plantHash[cellIndex];
                        eatenCreatures.Add(plantHash[cellIndex]);
                        grazer.Eat(eatenPlant);
                    }
                }
            }
        }

        private Point GetNewPosition(Creature creature)
        {
            var random = new Random();
            var newX = 0;
            var newY = 0;
            int index = 0;
            do
            {
                var directionX = random.Next(-1, 2);
                var directionY = random.Next(-1, 2);
                newX = creature.Location.X + directionX * creature.Speed;
                newY = creature.Location.Y + directionY * creature.Speed;
                index++;
            } while (!IsCellFreeForGrazer(newY * Size + newX, Grazer.DefaultSize) && index < 8);

            return new Point(Math.Clamp(newX, 0, Size - creature.Size), Math.Clamp(newY, 0, Size - creature.Size));
        }

        private void OldCreature(Creature creature)
        {
            creature.Age++;
            if (creature.Dead)
            {
                if (creature is Grazer grazer)
                    Debug.WriteLine($"Grazer dies in age of {grazer.Age}; Term: {this.term}");
                this.deadCreatures.Add(creature);
            }
        }

        private void SnapShotArea()
        {
            AreaSnapShot.Add(new Dictionary<int, int>(CurrentArea));
        }
    }
}
