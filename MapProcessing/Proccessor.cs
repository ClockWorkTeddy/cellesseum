using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace MapProcessing
{
    public class Proccessor
    {
        public List<AreaData> ProcessMap(Map map)
        {
            map.Start(500);

            return map.AreaSnapShot;
        }
    }
}
