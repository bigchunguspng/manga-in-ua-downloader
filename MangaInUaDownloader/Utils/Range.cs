namespace MangaInUaDownloader.Utils
{
    public struct Range
    {
        public readonly int Min, Max;

        public Range(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }
    
    public struct RangeF
    {
        public readonly float Min, Max;

        public RangeF(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }
}