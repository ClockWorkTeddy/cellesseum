using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace MapProcessing
{
    internal class Map
    {
        public List<Creature> Creatures { get; set; }
        public int Size { get; init; }
        private readonly List<Creature> deadCreatures = new List<Creature>();
        public List<Dictionary<int, int>> AreaSnapShot = new List<Dictionary<int, int>>();
        private int fertility = 250;
        public Map(int size)
        {
            Size = size;
            Creatures = new List<Creature>();
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
            foreach (var creature in Creatures)
            {
                if (creature.Speed > 0)
                {
                    MoveCreature(creature);
                }
                OldCreature(creature);
            }
        }

        private void CreatePlant()
        {
            Random random = new Random();

            for (int i = 0; i <= fertility; i++)
            {
                var x = 0;
                var y = 0;
                do
                {
                    x = random.Next(0, Size);
                    y = random.Next(0, Size);
                } while (Creatures.Exists(c => c.Location == new Point(x, y)));

                Creatures.Add(new Plant(new Point(x, y)));
            }
        }

        private void ClearDead()
        {
            Creatures.RemoveAll(c => deadCreatures.Contains(c));
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
            foreach (var creature in Creatures)
            {
                var index = creature.Location.Y * Size + creature.Location.X;
                area.Add(index, (int)creature.Type);
            }
            AreaSnapShot.Add(area);
        }
    }
}
