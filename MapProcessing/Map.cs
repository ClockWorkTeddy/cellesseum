using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace MapProcessing
{
    internal class Map
    {
        public Dictionary<int, Creature> CreaturesHash { get; set; }
        public int Size { get; init; }
        private readonly List<Creature> deadCreatures = new List<Creature>();
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
            Debug.Print($"Plants before: {CreaturesHash.Count}");
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
            Debug.Print($"Plants after: {CreaturesHash.Count}");
        }

        private void FillArea(Creature creature)
        {
            for (int y = 0; y < creature.Size; y++)
            {
                for (int x = 0; x < creature.Size; x++)
                {
                    //Debug.Print($"{creature.GetType()} - {creature.Location.Y + y}:{creature.Location.X + x}={(creature.Location.Y + y) * Size + (creature.Location.X + x)}");
                    CurrentArea.Add((creature.Location.Y + y) * Size + (creature.Location.X + x), (int)creature.Type);
                }
            }
        }

        private void ClearDead()
        {
            deadCreatures.ForEach(dc =>
            {
                CreaturesHash.Remove(dc.Location.Y * Size + dc.Location.X);

                for (int y = 0; y < dc.Size; y++)
                {
                    for (int x = 0; x < dc.Size; x++)
                    {
                        CurrentArea.Remove((dc.Location.Y + y) * Size + dc.Location.X + x);
                    }
                }
            });

            deadCreatures.Clear();
        }

        private void MoveCreature(Creature creature)
        {
            creature.Location = new Point(creature.Location.X + creature.Speed, creature.Location.Y + creature.Speed);
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
