
namespace MBaske
{
    public class StatsBuffer : TimedQueue<float>
    {
        public bool IsEnabled { get; set; } = true;

        public float Min => Values().Min();
        public float Max => Values().Max();
        public float Range => Max - Min;

        public float Avg => Values().Average();
        public float MAD => Values().MAD();

        public float RSD => Values().StdDev(true);
        public float SD => Values().StdDev();

        public float Start => First.time;
        public float End => Last.time;

        public float Current => Last.value;

        public StatsBuffer(int initCapacity, string name = "Value") : base(initCapacity, name) { }
    }
}