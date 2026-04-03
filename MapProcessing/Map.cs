using System.Collections.ObjectModel;
using System.Diagnostics;
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
        private readonly Random _random = new Random();
        private readonly byte[] _types;
        private readonly byte[] _saturations;

        public Map(int size)
        {
            Size = size;
            var cellCount = size * size;
            _types = new byte[cellCount];
            _saturations = new byte[cellCount];
        }

        public List<AreaData> AreaSnapShot { get; private set; } = new List<AreaData>();
        public int Epoche { get; private set; } = 0;

        public void Start(int term)
        {
            int grazerCount = 1;
            CreateGrazer(grazerCount);
            List<int> milliseconds = new List<int>();
            for (int i = 0; i < term && grazerHash.Count > 0; i++)
            {
                var sw = Stopwatch.StartNew();
                Next();
                sw.Stop();
                SnapShotArea();
                Epoche++;
                milliseconds.Add((int)sw.ElapsedMilliseconds);
            }
            Debug.WriteLine("");
        }

        private void Next()
        {
            CreatePlants();

            var currentGrazers = grazerHash.Values.ToList();
            foreach (var grazer in currentGrazers)
            {
                grazer.Starve();
                MoveCreature(grazer);

                if (grazer.Satiety > Grazer.BreedingThreshold)
                {
                    grazer.Satiety = Grazer.DefaultSatiety;
                    var newLocation = GetNewPositionNearParent(grazer);
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
            for (int i = 0; i < quantity; i++)
            {
                var position = GetNewPositionRandomly();
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

            for (int i = 0; i < fertility; i++)
            {
                var x = 0;
                var y = 0;
                do
                {
                    x = _random.Next(0, Size);
                    y = _random.Next(0, Size);

                } while (!IsCellFreeFor(y * Size + x, Plant.DefaultSize, CellType.Plant));

                var guid = Guid.NewGuid();
                var plant = new Plant(new Point(x, y), guid);
                plantHash[y * Size + x] = plant;
                FillArea(plant);
            }
        }

        private Point GetNewPositionNearParent(Creature creature)
        {
            var newX = 0;
            var newY = 0;
            int index = 0;
            do
            {
                var directionX = _random.Next(-1, 2);
                var directionY = _random.Next(-1, 2);
                newX = creature.Location.X + directionX * creature.Speed;
                newY = creature.Location.Y + directionY * creature.Speed;
                index++;
            } while (!IsCellFreeFor(newY * Size + newX, Grazer.DefaultSize, CellType.Grazer) && index < 8);

            return new Point(Math.Clamp(newX, 0, Size - creature.Size), Math.Clamp(newY, 0, Size - creature.Size));
        }

        private Point GetNewPositionRandomly()
        {
            var x = 0;
            var y = 0;
            do
            {
                x = _random.Next(0, Size);
                y = _random.Next(0, Size);
                y = Math.Clamp(y % 2 == 0 ? y : y - 1, 0, Size - 1);
                x = Math.Clamp(x % 2 == 0 ? x : x - 1, 0, Size - 1);
            } while (!IsCellFreeFor(y * Size + x, Grazer.DefaultSize, CellType.Grazer));

            return new Point(x, y);
        }

        private bool IsCellFreeFor(int index, int size, CellType cellType)
        {
            var cellTypeVal = (byte)cellType;
            var cellCount = _types.Length;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var idx = index + y * Size + x;
                    if ((uint)idx < (uint)cellCount && _types[idx] == cellTypeVal)
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
            var cellCount = _types.Length;
            for (int y = 0; y < grazer.Size; y++)
            {
                for (int x = 0; x < grazer.Size; x++)
                {
                    var cellIndex = (grazer.Location.Y + y) * Size + grazer.Location.X + x;
                    if ((uint)cellIndex < (uint)cellCount && _types[cellIndex] == (byte)CellType.Plant)
                    {
                        var eatenPlant = plantHash[cellIndex];
                        eatenCreatures.Add(eatenPlant);
                        grazer.Eat(eatenPlant);
                    }
                }
            }
        }

        private void FillArea(Creature creature)
        {
            var saturation = (byte)(creature.Type == CellType.Plant ? Math.Clamp(creature.NutritionValue, 2, 10) : Math.Clamp(creature.Satiety / 20, 5, 10));
            var type = (byte)creature.Type;
            var baseIndex = creature.Location.Y * Size + creature.Location.X;
            for (int y = 0; y < creature.Size; y++)
            {
                var rowBase = baseIndex + y * Size;
                for (int x = 0; x < creature.Size; x++)
                {
                    _types[rowBase + x] = type;
                    _saturations[rowBase + x] = saturation;
                }
            }
        }

        private void ClearArea(Creature creature)
        {
            var baseIndex = creature.Location.Y * Size + creature.Location.X;
            for (int y = 0; y < creature.Size; y++)
            {
                var rowBase = baseIndex + y * Size;
                for (int x = 0; x < creature.Size; x++)
                {
                    _types[rowBase + x] = 0;
                    _saturations[rowBase + x] = 0;
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
            var cellCount = _types.Length;
            var types = new byte[cellCount];
            var saturations = new byte[cellCount];
            Array.Copy(_types, types, cellCount);
            Array.Copy(_saturations, saturations, cellCount);
            AreaSnapShot.Add(new AreaData
            {
                PlantCount = plantHash.Count,
                GrazerCount = grazerHash.Count,
                Types = types,
                Saturations = saturations
            });
        }
    }
}
