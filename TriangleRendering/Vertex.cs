using Silk.NET.Maths;

namespace TriangleRendering;

public struct Vertex(Vector3D<double> pos, Vector4D<double> color)
{
    public Vector3D<double> Pos = pos;

    public Vector4D<double> Color = color;
}
