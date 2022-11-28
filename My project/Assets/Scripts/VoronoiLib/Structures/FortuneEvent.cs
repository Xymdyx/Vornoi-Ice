/// credit: https://github.com/Zalgo2462/VoronoiLib zalgo2462 FortunesAlgo implementation

using System;

namespace VoronoiLib.Structures
{
    interface FortuneEvent : IComparable<FortuneEvent>
    {
        double X { get; }
        double Y { get; }
    }
}
