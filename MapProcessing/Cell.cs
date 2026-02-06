using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MapProcessing
{
    public enum CellType
    {
        Empty,
        Plant,
        Herbivore,
        Carnivore
    }
    public class Cell
    {
        public Cell(CellType type, Point location)
        {
            Type = type;
            Location = location;
        }

        public CellType Type { get; }
        public Point Location { get; }
    }
}
