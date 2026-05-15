namespace MapProcessing
{
    public class Proccessor
    {
        public List<AreaData> ProcessMap(Map map, int term = 1000)
        {
            return ProcessMapFrames(map, term).ToList();
        }

        public IEnumerable<AreaData> ProcessMapFrames(Map map, int term = 1000)
        {
            return map.GenerateFrames(term);
        }
    }
}
