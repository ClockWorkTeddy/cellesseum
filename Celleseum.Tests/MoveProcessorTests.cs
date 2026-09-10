using System.Drawing;
using MapProcessing;

namespace Celleseum.Tests;

public class MoveProcessorTests
{
    [Fact]
    public void SmartGrazerRejectsLandingAreaPartiallyOccupiedByAnotherGrazer()
    {
        var map = new Map(8, 8);
        var grazer = AddGrazer(map, new Point(2, 2));
        var blockingGrazer = AddGrazer(map, new Point(5, 2));
        blockingGrazer.Speed = 0;
        AddPlant(map, new Point(4, 3));

        new MoveProcessor(smartGrazer: true).Execute(map);

        Assert.NotEqual(new Point(4, 2), grazer.Location);
    }

    [Fact]
    public void SmartGrazerScoresNutritionAcrossEntireLandingArea()
    {
        var map = new Map(8, 8);
        var grazer = AddGrazer(map, new Point(2, 2));
        AddPlant(map, new Point(5, 3));

        new MoveProcessor(smartGrazer: true).Execute(map);

        Assert.Equal(new Point(4, 2), grazer.Location);
    }

    [Fact]
    public void MutationSmartMovementIsOnlyEnabledForSaturationSeven()
    {
        var mutationMoveProcessor = new TestableMutationMoveProcessor(smartGrazer: true);

        Assert.False(mutationMoveProcessor.ShouldUseSmartMovementFor(new Grazer(new Point(0, 0), Guid.NewGuid(), saturation: 6)));
        Assert.True(mutationMoveProcessor.ShouldUseSmartMovementFor(new Grazer(new Point(0, 0), Guid.NewGuid(), saturation: 7)));
    }

    [Fact]
    public void SimpleSmartMovementRemainsEnabledForAnyGrazerWhenUserSelectedSmartMode()
    {
        var moveProcessor = new TestableMoveProcessor(smartGrazer: true);

        Assert.True(moveProcessor.ShouldUseSmartMovementFor(new Grazer(new Point(0, 0), Guid.NewGuid(), saturation: 0)));
        Assert.True(moveProcessor.ShouldUseSmartMovementFor(new Grazer(new Point(0, 0), Guid.NewGuid(), saturation: 7)));
    }

    private sealed class TestableMoveProcessor(bool smartGrazer) : MoveProcessor(smartGrazer)
    {
        public bool ShouldUseSmartMovementFor(Creature creature) => ShouldUseSmartMovement(creature);
    }

    private sealed class TestableMutationMoveProcessor(bool smartGrazer) : MutationMoveProcessor(smartGrazer)
    {
        public bool ShouldUseSmartMovementFor(Creature creature) => ShouldUseSmartMovement(creature);
    }

    private static Grazer AddGrazer(Map map, Point location)
    {
        var grazer = new Grazer(location, Guid.NewGuid(), saturation: 0);
        map.AddGrazer(grazer.Id, grazer);
        FillArea(map, grazer);
        return grazer;
    }

    private static void AddPlant(Map map, Point location)
    {
        var plant = new Plant(location, Guid.NewGuid());
        var index = location.Y * map.Width + location.X;
        map.AddPlant(index, plant);
        map.SetCellType(index, CellType.Plant);
    }

    private static void FillArea(Map map, Grazer grazer)
    {
        for (int y = 0; y < grazer.Size; y++)
        {
            for (int x = 0; x < grazer.Size; x++)
            {
                var index = (grazer.Location.Y + y) * map.Width + grazer.Location.X + x;
                map.SetCellType(index, CellType.Grazer);
            }
        }
    }
}
