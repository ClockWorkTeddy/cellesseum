using System.Collections.ObjectModel;
using System.Drawing;

namespace MapProcessing
{
    public class Map
    {
        public int Size { get; init; }
        private readonly List<Creature> deadCreatures = new List<Creature>();
        private readonly List<Creature> eatenCreatures = new List<Creature>();
        private readonly Dictionary<int, Plant> plantHash = new Dictionary<int, Plant>();
        private readonly Dictionary<Guid, Grazer> grazerHash = new Dictionary<Guid, Grazer>();

        public Map(int size)
        {
            Size = size;
        }

        public List<AreaData> AreaSnapShot { get; private set; } = new List<AreaData>();
        public AreaData CurrentAreaData { get; private set; } = new AreaData();
        public int Epoche { get; private set; } = 0;

        public void Start(int term)
        {
            int grazerCount = 1;
            CreateGrazer(grazerCount);

            for (int i = 0; i < term && grazerHash.Count > 0; i++)
            {
                Next();
                SnapShotArea();
                Epoche++;
            }
        }

        private void Next()
        {
            CreatePlants();

            for (int i = 0; i < grazerHash.Count; i++)
            {
                var grazer = grazerHash.ElementAt(i);
                grazer.Value.Starve();
                MoveCreature(grazer.Value);

                if (grazer.Value.Satiety > Grazer.BreedingThreshold)
                {
                    grazer.Value.Satiety = Grazer.DefaultSatiety;
                    var newLocation = GetNewPositionNearParent(grazer.Value);
                    PlaceGrazer(newLocation);
                }
            }

            ClearDead();

            foreach (var plant in plantHash)
            {
                OldCreature(plant.Value);
            }
            foreach(var grazer in grazerHash)
            {
                OldCreature(grazer.Value);
            }

            ClearDead();
        }

        private void CreateGrazer(int quantity)
        {
            Random random = new Random();
            for (int i = 0; i < quantity; i++)
            {
                var position = GetNewPositionRandomly(random);
                PlaceGrazer(position);
            }
        }

        private void PlaceGrazer(Point position)
        {
            var guid = Guid.NewGuid();
            var grazer = new Grazer(position, guid);
            grazerHash[guid] = grazer;
            Grazing(grazer);
            FillArea(grazer);
        }

        private void CreatePlants()
        {
            var amplifier = 0.025;
            var fertility = (int)(amplifier * (Math.Pow(Size, 2) - grazerHash.Count * Math.Pow(Grazer.DefaultSize, 2) - plantHash.Count * Math.Pow(Plant.DefaultSize, 2)));

            Random random = new Random();

            for (int i = 0; i < fertility; i++)
            {
                var x = 0;
                var y = 0;
                do
                {
                    x = random.Next(0, Size);
                    y = random.Next(0, Size);

                } while (!IsCellFreeFor(y * Size + x, Plant.DefaultSize, CellType.Plant));

                var guid = Guid.NewGuid();
                var plant = new Plant(new Point(x, y), guid);
                plantHash[y * Size + x] = plant;
                FillArea(plant);
            }
        }

        private Point GetNewPositionNearParent(Creature creature)
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
            } while (!IsCellFreeFor(newY * Size + newX, Grazer.DefaultSize, CellType.Grazer) && index < 8);

            return new Point(Math.Clamp(newX, 0, Size - creature.Size), Math.Clamp(newY, 0, Size - creature.Size));
        }

        private Point GetNewPositionRandomly(Random random)
        {
            var x = 0;
            var y = 0;
            do
            {
                x = random.Next(0, Size);
                y = random.Next(0, Size);
                y = Math.Clamp(y % 2 == 0 ? y : y - 1, 0, Size - 1);
                x = Math.Clamp(x % 2 == 0 ? x : x - 1, 0, Size - 1);
            } while (!IsCellFreeFor(y * Size + x, Grazer.DefaultSize, CellType.Grazer));

            return new Point(x, y);
        }

        private bool IsCellFreeFor(int index, int size, CellType cellType)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (CurrentAreaData.CurrentArea.ContainsKey(index + y * Size + x) && CurrentAreaData.CurrentArea[index + y * Size + x].X == (int)cellType)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void MoveCreature(Creature creature)
        {
            ClearArea(creature);

            creature.Location = GetNewPositionNearParent(creature);
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
                    if (CurrentAreaData.CurrentArea.ContainsKey(cellIndex) && CurrentAreaData.CurrentArea[cellIndex].X == (int)CellType.Plant)
                    {
                        var eatenPlant = plantHash[cellIndex];
                        eatenCreatures.Add(plantHash[cellIndex]);
                        grazer.Eat(eatenPlant);
                    }
                }
            }
        }

        private void FillArea(Creature creature)
        {
            for (int y = 0; y < creature.Size; y++)
            {
                for (int x = 0; x < creature.Size; x++)
                {
                    var saturation = creature.Type == CellType.Plant ? Math.Clamp(creature.NutritionValue, 2, 10) : 0;
                    CurrentAreaData.CurrentArea[(creature.Location.Y + y) * Size + (creature.Location.X + x)] = new Point((int)creature.Type, saturation);
                }
            }
        }

        private void ClearArea(Creature creature)
        {
            for (int y = 0; y < creature.Size; y++)
            {
                for (int x = 0; x < creature.Size; x++)
                {
                    CurrentAreaData.CurrentArea.Remove((creature.Location.Y + y) * Size + creature.Location.X + x);
                }
            }
        }

        private void ClearDead()
        {
            deadCreatures.ForEach(dc =>
            {
                if (dc is Plant plant)
                {
                    plantHash.Remove(plant.Location.Y * Size + plant.Location.X);
                }
                else if (dc is Grazer)
                {
                    grazerHash.Remove(dc.Id);
                }
                ClearArea(dc);
            });

            deadCreatures.Clear();

            eatenCreatures.ForEach(ec =>
            {
                plantHash.Remove(ec.Location.Y * Size + ec.Location.X);
            });
            eatenCreatures.Clear();
        }

        private void OldCreature(Creature creature)
        {
            creature.Age++;
            FillArea(creature);

            if (creature.Dead)
            {
                this.deadCreatures.Add(creature);
            }
        }

        private void SnapShotArea()
        {
            AreaSnapShot.Add(new AreaData
            {
                PlantCount = plantHash.Count,
                GrazerCount = grazerHash.Count,
                CurrentArea = new Dictionary<int, Point>(CurrentAreaData.CurrentArea)
            });
        }
    }
}
