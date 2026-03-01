using DragonMerge.Items;

namespace DragonMerge.Board
{
    [System.Serializable]
    public class Tile
    {
        public int X;
        public int Y;
        public MergeItem Occupant;

        public bool IsEmpty => Occupant == null;

        public Tile(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
