using Silk.NET.Maths;

namespace TriangleRendering;

internal class Program
{
    private static readonly int Width = 800;
    private static readonly int Height = 600;
    private static readonly Vertex[] vertices =
    [
        new Vertex(new Vector3D<double>(-0.5, -0.5, 0.0), new Vector4D<double>(1.0, 0.0, 0.0, 1.0)),
        new Vertex(new Vector3D<double>(0.5, -0.5, 0.0), new Vector4D<double>(0.0, 1.0, 0.0, 0.5)),
        new Vertex(new Vector3D<double>(0.0, 0.5, 0.0), new Vector4D<double>(0.0, 0.0, 1.0, 1.0)),

        new Vertex(new Vector3D<double>(-0.5, 0.5, 0.0), new Vector4D<double>(1.0, 0.0, 0.0, 1.0)),
        new Vertex(new Vector3D<double>(0.5, 0.5, 0.0), new Vector4D<double>(0.0, 1.0, 0.0, 1.0)),
        new Vertex(new Vector3D<double>(0.0, -0.5, 0.0), new Vector4D<double>(0.0, 0.0, 1.0, 0.5))
    ];
    private static readonly uint[] indices = [0, 1, 2, 3, 4, 5];

    private static readonly Vector3D<double> CameraPosition = new(0.0, 0.0, 3.0);
    private static readonly Vector3D<double> CameraFront = -Vector3D<double>.UnitZ;
    private static readonly Vector3D<double> CameraUp = Vector3D<double>.UnitY;
    private static readonly double Fov = 45.0f;

    private static readonly Matrix4X4<double> Model = Matrix4X4<double>.Identity;
    private static readonly Matrix4X4<double> View = Matrix4X4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
    private static readonly Matrix4X4<double> Projection = Matrix4X4.CreatePerspectiveFieldOfView(DegreesToRadians(Fov), (double)Width / Height, 0.1, 1000.0);

    private static readonly bool IsBlendingEnabled = true;

    static void Main(string[] args)
    {
        _ = args;

        Run();
    }

    private static void Run()
    {
        Vertex[] vsOutput;
        Vertex[] fsOutput;

        // Vertex shader
        {
            vsOutput = new Vertex[vertices.Length];

            Matrix4X4<double> mvp = Model * View * Projection;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector4D<double> pos = Vector4D.Transform(new Vector4D<double>(vertices[i].Pos, 1), mvp);
                Vector4D<double> color = vertices[i].Color;

                vsOutput[i] = new Vertex(new Vector3D<double>(pos.X / pos.W, pos.Y / pos.W, pos.Z / pos.W), color);
            }
        }

        // Rasterizer
        {
            fsOutput = new Vertex[Height * Width];
            Array.Fill(fsOutput, new Vertex(new Vector3D<double>(0.0, 0.0, 0.0), new Vector4D<double>(0.0, 0.0, 0.0, 1.0)));

            for (int i = 0; i < indices.Length; i += 3)
            {
                Vertex v0 = vsOutput[indices[i + 0]];
                Vertex v1 = vsOutput[indices[i + 1]];
                Vertex v2 = vsOutput[indices[i + 2]];

                Vector2D<double> p0 = new((v0.Pos.X + 1.0) * Width / 2.0f, Height - ((v0.Pos.Y + 1.0f) * Height / 2.0));
                Vector2D<double> p1 = new((v1.Pos.X + 1.0) * Width / 2.0f, Height - ((v1.Pos.Y + 1.0f) * Height / 2.0));
                Vector2D<double> p2 = new((v2.Pos.X + 1.0) * Width / 2.0f, Height - ((v2.Pos.Y + 1.0f) * Height / 2.0));

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Vector2D<double> p = new(x, y);
                        Vector4D<double> color;

                        if (IsPointInTriangle(p, p0, p1, p2))
                        {
                            color = InterpolateColor(v0.Color, v1.Color, v2.Color, p0, p1, p2, p);

                            if (IsBlendingEnabled)
                            {
                                color = color * color.W + fsOutput[y * Width + x].Color * (1.0 - color.W);
                            }
                            fsOutput[y * Width + x] = new Vertex(new Vector3D<double>(x, y, 0.0f), color);
                        }
                    }
                }
            }
        }


        using FileStream fileStream = File.Create("image.ppm");
        using StreamWriter streamWriter = new(fileStream);

        streamWriter.WriteLine("P3");
        streamWriter.WriteLine($"{Width} {Height}");
        streamWriter.WriteLine("255");

        for (int i = 0; i < fsOutput.Length; i++)
        {
            Vertex vertex = fsOutput[i];

            streamWriter.WriteLine($"{(int)(vertex.Color.X * 255)} {(int)(vertex.Color.Y * 255)} {(int)(vertex.Color.Z * 255)}");
        }
    }

    private static Vector2D<double> Cross(Vector2D<double> vector1, Vector2D<double> vector2)
    {
        return new Vector2D<double>(vector1.X * vector2.Y - vector1.Y * vector2.X);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0f;
    }

    private static bool IsPointInTriangle(Vector2D<double> point, Vector2D<double> a, Vector2D<double> b, Vector2D<double> c)
    {
        Vector2D<double> v0 = c - a;
        Vector2D<double> v1 = b - a;
        Vector2D<double> v2 = point - a;

        double dot00 = Vector2D.Dot(v0, v0);
        double dot01 = Vector2D.Dot(v0, v1);
        double dot02 = Vector2D.Dot(v0, v2);
        double dot11 = Vector2D.Dot(v1, v1);
        double dot12 = Vector2D.Dot(v1, v2);

        double invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        double u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        double v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return (u >= 0) && (v >= 0) && (u + v < 1);
    }

    private static Vector4D<double> InterpolateColor(Vector4D<double> v0, Vector4D<double> v1, Vector4D<double> v2, Vector2D<double> p0, Vector2D<double> p1, Vector2D<double> p2, Vector2D<double> p)
    {
        double area = Cross(p1 - p0, p2 - p0).Length;
        double w0 = Cross(p1 - p, p2 - p).Length / area;
        double w1 = Cross(p2 - p, p0 - p).Length / area;
        double w2 = Cross(p0 - p, p1 - p).Length / area;

        return v0 * w0 + v1 * w1 + v2 * w2;
    }
}
