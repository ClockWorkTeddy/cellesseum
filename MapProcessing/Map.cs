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

        private int _score;
        private int _overallPlantsCount;
        private int _overallGrazersCount;

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            var cellCount = width * height;
            _types = new byte[cellCount];
            _saturations = new byte[cellCount];
        }

        public int Epoche { get; private set; } = 0;

        public bool HasGrazers() => grazerHash.Count > 0;

        /// <summary>
        /// Get the Random instance used for all randomization in the simulation.
        /// </summary>
        public Random GetRandom() => _random;

        /// <summary>
        /// Get the list of current dead creatures (for inspection/cleanup).
        /// </summary>
        public List<Creature> GetDeadCreatures() => deadCreatures;

        /// <summary>
        /// Get the list of current eaten creatures (for inspection/cleanup).
        /// </summary>
        public List<Creature> GetEatenCreatures() => eatenCreatures;

        /// <summary>
        /// Get all plants currently on the map.
        /// </summary>
        public IReadOnlyDictionary<int, Plant> GetPlants() => new ReadOnlyDictionary<int, Plant>(plantHash);

        /// <summary>
        /// Get all grazers currently on the map.
        /// </summary>
        public IReadOnlyDictionary<Guid, Grazer> GetGrazers() => new ReadOnlyDictionary<Guid, Grazer>(grazerHash);

        /// <summary>
        /// Get the cell type (Empty, Plant, or Grazer) at the given index.
        /// </summary>
        public CellType GetCellType(int index) => (CellType)_types[index];

        /// <summary>
        /// Set the cell type at the given index.
        /// </summary>
        public void SetCellType(int index, CellType cellType) => _types[index] = (byte)cellType;

        /// <summary>
        /// Set the saturation value at the given index.
        /// </summary>
        public void SetSaturation(int index, byte saturation) => _saturations[index] = saturation;

        /// <summary>
        /// Add a plant to the map.
        /// </summary>
        public void AddPlant(int index, Plant plant)
        {
            plantHash[index] = plant;
            _overallPlantsCount++;
            _score++;
        }

        /// <summary>
        /// Remove a plant from the map.
        /// </summary>
        public void RemovePlant(int index)
        {
            plantHash.Remove(index);
        }

        /// <summary>
        /// Get a plant at the given index, or null if none exists.
        /// </summary>
        public Plant? GetPlantAt(int index)
        {
            return plantHash.TryGetValue(index, out var plant) ? plant : null;
        }

        /// <summary>
        /// Add a grazer to the map.
        /// </summary>
        public void AddGrazer(Guid id, Grazer grazer)
        {
            grazerHash[id] = grazer;
            _overallGrazersCount++;
            _score += 10 * (grazer.Saturation + 1);
        }

        /// <summary>
        /// Remove a grazer from the map.
        /// </summary>
        public void RemoveGrazer(Guid id)
        {
            grazerHash.Remove(id);
        }

        /// <summary>
        /// Increment the epoch counter (called after each frame is generated).
        /// </summary>
        public void IncrementEpoch()
        {
            Epoche++;
        }

        /// <summary>
        /// Get snapshot of current map state.
        /// </summary>
        public AreaData SnapShotArea()
        {
            return SnapShotAreaInternal();
        }

        private AreaData SnapShotAreaInternal()
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
                NormalizedScore = (int)(_score / (Width * Height * 0.0265)),
                OverallPlantsCount = _overallPlantsCount,
                OverallGrazersCount = _overallGrazersCount,
                Types = types,
                Saturations = saturations
            };
        }
    }
}
