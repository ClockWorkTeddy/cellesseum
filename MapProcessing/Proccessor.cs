using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MapProcessing
{
    public class Proccessor
    {
        public List<Dictionary<int, int>> ProcessMap()
        {
            var map = new Map(10);
            map.Start(10);

            return map.AreaSnapShot;
        }
    }
}
