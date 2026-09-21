using System;

namespace WadesMiBPinner
{
    public sealed class MapMarker
    {
        public MapMarker(string name, int x, int y, string icon, int facet)
        {
            Name = name;
            X = x;
            Y = y;
            Icon = icon;
            Facet = facet;
        }

        public string Name { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public string Icon { get; private set; }
        public int Facet { get; private set; }

        public string Coordinates
        {
            get { return X + ", " + Y; }
        }
    }
}

