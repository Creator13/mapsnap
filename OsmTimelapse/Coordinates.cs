namespace OsmTimelapse;

public struct Coordinates
{
    public double latitude;
    public double longitude;

    public Coordinates(string coordsXY)
    {
        var values = coordsXY.Trim().Split(';');
        latitude = double.Parse(values[0]);
        longitude = double.Parse(values[1]);
    }

    public Coordinates(double latitude, double longitude)
    {
        this.latitude = latitude;
        this.longitude = longitude;
    }

    public override string ToString()
    {
        return $"{latitude.ToString()}° {longitude.ToString()}°";
    }
}