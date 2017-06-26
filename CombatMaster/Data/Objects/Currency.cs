namespace CombatMaster.Data
{
    public class Currency
    {
        public long Gold { get; set; }
        public long Silver { get; set; }
        public long Copper { get; set; }

        public string Format()
        {
            return string.Format("{0}g {1}s {2}c", Gold, Silver, Copper);
        }

        public long Value
        {
            get
            {
                return (Gold * 10000) + (Silver * 100) + (Copper * 1);
            }
        }
    }
}
