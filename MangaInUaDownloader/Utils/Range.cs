namespace MangaInUaDownloader.Utils
{
    public readonly struct Range
    {
        private readonly int Min, Max;

        public Range(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public bool Contains(int x) => x >= Min && x <= Max;
    }
    
    public readonly struct RangeF
    {
        private readonly float Min, Max;

        public RangeF(float min, float max)
        {
            Min = min;
            Max = max;
        }
        
        public bool Contains(float x) => x >= Min && x <= Max;
    }
}