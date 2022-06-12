using System;

namespace mapsnap;

[Serializable]
public struct CartesianCoordinates
{
    public int x, y;

    public CartesianCoordinates(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public override string ToString()
    {
        return $"({x}, {y})";
    }

    public override bool Equals(object obj)
    {
        return obj is CartesianCoordinates coords && coords.x == this.x && coords.y == this.y;
    }

    public void Deconstruct(out int x, out int y)
    {
        x = this.x;
        y = this.y;
    }
    
    public static implicit operator CartesianCoordinates((int, int) tuple)
    {
        return new CartesianCoordinates(tuple.Item1, tuple.Item2);
    }

    public static implicit operator (int, int)(CartesianCoordinates coords)
    {
        return (coords.x, coords.y);
    }
}
