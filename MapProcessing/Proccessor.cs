using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MapProcessing
{
    public class Proccessor
    {
        public List<Dictionary<int, int>> ProcessMap(Map map)
        {
            map.Start(100);

            return map.AreaSnapShot;
        }
    }
}
