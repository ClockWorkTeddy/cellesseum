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
        private int fertility = 250;
        public Map(int size)
        {
            Size = size;
            CreaturesHash = new Dictionary<int, Creature>();
        }

        public void Start(int term)
        {
            for (int i = 0; i < term; i++)
            {
                CreatePlant();
                Next();
                ClearDead();
                SnapShotArea();
            }
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
                } while (CreaturesHash.ContainsKey(y*Size+x));

                CreaturesHash[y * Size + x] = new Plant(new Point(x, y));
            }
            Debug.Print($"Plants after: {CreaturesHash.Count}");
        }

        private void ClearDead()
        {
            deadCreatures.ForEach(dc => CreaturesHash.Remove(dc.Location.Y * Size + dc.Location.X));
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
            Dictionary<int, int> area = new Dictionary<int, int>();
            foreach (var creature in CreaturesHash)
            {
                area.Add(creature.Key, (int)creature.Value.Type);
            }
            AreaSnapShot.Add(area);
        }
    }
}
