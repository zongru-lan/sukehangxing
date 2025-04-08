using System;
using System.Drawing;

namespace UI.XRay.Business.Entities
{
    [Serializable]
    public class LabelRegion : IComparable<LabelRegion>
    {
        public int ToLine { get; private set; }
        public int FromLine { get; private set; }
        public int ToChannel { get; private set; }
        public int FromChannel { get; private set; }

        public LabelRegion(int fromLine, int toLine, int fromChannel, int toChannel)
        {
            FromLine = fromLine;
            FromChannel = fromChannel;
            ToChannel = toChannel;
            ToLine = toLine;
        }
        public bool IntersectWith(LabelRegion region)
        {
            var thisRect = new Rectangle(FromLine, FromChannel, ToLine - FromLine, ToChannel - FromChannel);
            var regionRect = new Rectangle(region.FromLine, region.FromChannel, region.ToLine - region.FromLine, region.ToChannel - region.FromChannel);

            var intersect = Rectangle.Intersect(thisRect, regionRect);
            return (intersect.Height > 0 && intersect.Width > 0);
        }

        public int CompareTo(LabelRegion other)
        {
            if (other == null)
            {
                return 1;
            }
            return FromChannel - other.FromChannel;
        }
    }
}
