using System.Collections.Generic;
using System.Linq;

namespace ProceduralDungeon
{
    public class RoomTypeAssigner
    {
        private System.Random _rng;

        public RoomTypeAssigner(int seed)
        {
            _rng = new System.Random(seed);
        }

        public List<PRoomType> BuildRoomDeck(int roomCount, int floorNumber)
        {
            List<PRoomType> deck = new List<PRoomType>();

            if (roomCount <= 0) return deck;

            deck.Add(PRoomType.Start);

            int[] weights = new int[] { 45, 20, 20, 15 };
            PRoomType[] types = new PRoomType[] { PRoomType.Combat, PRoomType.Loot, PRoomType.Shop, PRoomType.Altar };

            int remaining = roomCount - 2; 

            for (int r = 0; r < remaining; r++)
            {
                int totalWeight = weights.Sum();
                int roll = _rng.Next(0, totalWeight);
                int cumulative = 0;

                for (int i = 0; i < weights.Length; i++)
                {
                    cumulative += weights[i];
                    if (roll < cumulative)
                    {
                        deck.Add(types[i]);
                        break;
                    }
                }
            }

            if (roomCount > 1)
            {
                PRoomType finalType = (floorNumber > 0 && floorNumber % 5 == 0) ? PRoomType.Boss : PRoomType.MiniBoss;
                deck.Add(finalType);
            }

            return deck;
        }
    }
}
