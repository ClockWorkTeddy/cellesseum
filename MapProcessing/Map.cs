using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;

namespace MapProcessing
{
    public class Map
    {
        public int Width { get; init; }
        public int Height { get; init; }
        private readonly List<Creature> deadCreatures = new List<Creature>();
        private readonly List<Creature> eatenCreatures = new List<Creature>();
        private readonly Dictionary<int, Plant> plantHash = new Dictionary<int, Plant>();
        private readonly Dictionary<Guid, Grazer> grazerHash = new Dictionary<Guid, Grazer>();
        private readonly Random _random = new Random();
        private readonly byte[] _types;
        private readonly byte[] _saturations;

        private long _profileCreatePlantsTicks;
        private long _profileMoveAndBreedTicks;
        private long _profileClearDeadTicks;
        private long _profileAgingTicks;
        private long _profileSnapshotTicks;
        private long _profileTotalTicks;
        private int _profileSampleCount;
        private int _score;
        private int _term;

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            var cellCount = width * height;
            _types = new byte[cellCount];
            _saturations = new byte[cellCount];
        }

        public List<AreaData> AreaSnapShot { get; private set; } = new List<AreaData>();
        public int Epoche { get; private set; } = 0;

        public IEnumerable<AreaData> GenerateFrames(int term)
        {
            _term = term;
            const int initialGrazerCount = 1;
            CreateGrazer(initialGrazerCount);

            for (int i = 0; i < term && grazerHash.Count > 0; i++)
            {
                var totalStart = Stopwatch.GetTimestamp();

                Next();

                var snapshotStart = Stopwatch.GetTimestamp();
                var frame = SnapShotArea();
                _profileSnapshotTicks += Stopwatch.GetTimestamp() - snapshotStart;

                Epoche++;

                var totalTicks = Stopwatch.GetTimestamp() - totalStart;
                _profileTotalTicks += totalTicks;
                RecordProfileSample();

                yield return frame;
            }

            Debug.WriteLine("");
        }

        public void Start(int term)
        {
            AreaSnapShot = GenerateFrames(term).ToList();
        }

        private void Next()
        {
            var phaseStart = Stopwatch.GetTimestamp();
            CreatePlants();
            _profileCreatePlantsTicks += Stopwatch.GetTimestamp() - phaseStart;

            phaseStart = Stopwatch.GetTimestamp();
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
            _profileMoveAndBreedTicks += Stopwatch.GetTimestamp() - phaseStart;

            phaseStart = Stopwatch.GetTimestamp();
            ClearDead();
            _profileClearDeadTicks += Stopwatch.GetTimestamp() - phaseStart;

            phaseStart = Stopwatch.GetTimestamp();
            foreach (var plant in plantHash)
            {
                OldCreature(plant.Value);
            }
            foreach(var grazer in grazerHash)
            {
                OldCreatureWithoutSaturation(grazer.Value);
            }
            _profileAgingTicks += Stopwatch.GetTimestamp() - phaseStart;

            phaseStart = Stopwatch.GetTimestamp();
            ClearDead();
            _profileClearDeadTicks += Stopwatch.GetTimestamp() - phaseStart;
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
            _score += 10;
        }

        private void CreatePlants()
        {
            var amplifier = 0.025;
            var fertility = (int)(amplifier * (Width * Height - grazerHash.Count * Math.Pow(Grazer.DefaultSize, 2) - plantHash.Count * Math.Pow(Plant.DefaultSize, 2)));

            for (int i = 0; i < fertility; i++)
            {
                var x = 0;
                var y = 0;
                do
                {
                    x = _random.Next(0, Width);
                    y = _random.Next(0, Height);

                } while (!IsCellFreeFor(y * Width + x, Plant.DefaultSize, CellType.Plant));

                var guid = Guid.NewGuid();
                var plant = new Plant(new Point(x, y), guid);
                plantHash[y * Width + x] = plant;
                FillArea(plant);
                _score++;
            }
        }

        private Point GetNewPositionNearParent(Creature creature)
        {
            var maxX = Width - creature.Size;
            var maxY = Height - creature.Size;
            var chosen = creature.Location;
            var freeCount = 0;

            for (int directionY = -1; directionY <= 1; directionY++)
            {
                for (int directionX = -1; directionX <= 1; directionX++)
                {
                    if (directionX == 0 && directionY == 0)
                    {
                        continue;
                    }

                    var newX = creature.Location.X + directionX * creature.Speed;
                    var newY = creature.Location.Y + directionY * creature.Speed;

                    if ((uint)newX > (uint)maxX || (uint)newY > (uint)maxY)
                    {
                        continue;
                    }

                    if (!IsCellFreeFor(newY * Width + newX, Grazer.DefaultSize, CellType.Grazer))
                    {
                        continue;
                    }

                    freeCount++;
                    if (_random.Next(freeCount) == 0)
                    {
                        chosen = new Point(newX, newY);
                    }
                }
            }

            return chosen;
        }

        private Point GetNewPositionRandomly()
        {
            var x = 0;
            var y = 0;
            do
            {
                x = _random.Next(0, Width);
                y = _random.Next(0, Height);
                y = Math.Clamp(y % 2 == 0 ? y : y - 1, 0, Height - 1);
                x = Math.Clamp(x % 2 == 0 ? x : x - 1, 0, Width - 1);
            } while (!IsCellFreeFor(y * Width + x, Grazer.DefaultSize, CellType.Grazer));

            return new Point(x, y);
        }

        private bool IsCellFreeFor(int index, int size, CellType cellType)
        {
            return !((uint)index < (uint)_types.Length && _types[index] == (byte)cellType);
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
                    var cellIndex = (grazer.Location.Y + y) * Width + grazer.Location.X + x;
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
            var saturation = (byte)Math.Clamp(creature.NutritionValue, 2, 10);
            var type = (byte)creature.Type;
            var baseIndex = creature.Location.Y * Width + creature.Location.X;
            for (int y = 0; y < creature.Size; y++)
            {
                var rowBase = baseIndex + y * Width;
                for (int x = 0; x < creature.Size; x++)
                {
                    _types[rowBase + x] = type;
                    _saturations[rowBase + x] = saturation;
                }
            }
        }

        private void ClearArea(Creature creature)
        {
            var baseIndex = creature.Location.Y * Width + creature.Location.X;
            for (int y = 0; y < creature.Size; y++)
            {
                var rowBase = baseIndex + y * Width;
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
                    plantHash.Remove(plant.Location.Y * Width + plant.Location.X);
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
                plantHash.Remove(ec.Location.Y * Width + ec.Location.X);
            });
            eatenCreatures.Clear();
        }

        private void OldCreature(Creature creature)
        {
            creature.Age++;

            var baseIndex = creature.Location.Y * Width + creature.Location.X;
            _saturations[baseIndex] = (byte)creature.NutritionValue;
            if (creature.Dead)
            {
                this.deadCreatures.Add(creature);
            }
        }

        private void OldCreatureWithoutSaturation(Creature creature)
        {
            creature.Age++;

            if (creature.Dead)
            {
                this.deadCreatures.Add(creature);
            }
        }


        private void RecordProfileSample()
        {
            _profileSampleCount++;
            if (_profileSampleCount % 100 != 0)
            {
                return;
            }

            var windowSize = 100d;
            Debug.WriteLine($"Epoch {Epoche}: avg total {TicksToMilliseconds(_profileTotalTicks / windowSize):F3} ms | plants {TicksToMilliseconds(_profileCreatePlantsTicks / windowSize):F3} ms | move+breed {TicksToMilliseconds(_profileMoveAndBreedTicks / windowSize):F3} ms | clearDead {TicksToMilliseconds(_profileClearDeadTicks / windowSize):F3} ms | aging {TicksToMilliseconds(_profileAgingTicks / windowSize):F3} ms | snapshot {TicksToMilliseconds(_profileSnapshotTicks / windowSize):F3} ms");

            _profileCreatePlantsTicks = 0;
            _profileMoveAndBreedTicks = 0;
            _profileClearDeadTicks = 0;
            _profileAgingTicks = 0;
            _profileSnapshotTicks = 0;
            _profileTotalTicks = 0;
        }

        private static double TicksToMilliseconds(double ticks)
        {
            return ticks * 1000d / Stopwatch.Frequency;
        }

        private AreaData SnapShotArea()
        {
            var cellCount = _types.Length;
            var types = new byte[cellCount];
            var saturations = new byte[cellCount];
            Array.Copy(_types, types, cellCount);
            Array.Copy(_saturations, saturations, cellCount);
            return new AreaData
            {
                PlantCount = plantHash.Count,
                GrazerCount = grazerHash.Count,
                NormalizedScore = (int)(_score / Math.Pow(_term, 2) * 10000),
                Types = types,
                Saturations = saturations
            };
        }
    }
}
