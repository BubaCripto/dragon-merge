using System.Collections.Generic;
using DragonMerge.Items;

namespace DragonMerge.Logic
{
    public static class MatchDetector
    {
        public static List<List<MergeItem>> FindMatches(MergeItem[,] grid, int width, int height)
        {
            var matches = new List<List<MergeItem>>();

            for (int y = 0; y < height; y++)
            {
                int run = 1;
                for (int x = 1; x <= width; x++)
                {
                    bool same = x < width && IsMatchable(grid[x - 1, y], grid[x, y]);
                    if (same)
                    {
                        run++;
                        continue;
                    }

                    if (run >= 3)
                    {
                        var group = new List<MergeItem>();
                        for (int i = x - run; i < x; i++) group.Add(grid[i, y]);
                        matches.Add(group);
                    }

                    run = 1;
                }
            }

            for (int x = 0; x < width; x++)
            {
                int run = 1;
                for (int y = 1; y <= height; y++)
                {
                    bool same = y < height && IsMatchable(grid[x, y - 1], grid[x, y]);
                    if (same)
                    {
                        run++;
                        continue;
                    }

                    if (run >= 3)
                    {
                        var group = new List<MergeItem>();
                        for (int i = y - run; i < y; i++) group.Add(grid[x, i]);
                        matches.Add(group);
                    }

                    run = 1;
                }
            }

            return matches;
        }

        private static bool IsMatchable(MergeItem a, MergeItem b)
        {
            return a != null && b != null && a.Tier == b.Tier && a.Color == b.Color;
        }
    }
}
