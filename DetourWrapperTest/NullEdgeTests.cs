using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

// Regression test for the 10/01 server crash: Detour's closestPointOnDetailEdges read a null edge on polys whose
// detail triangles carry no boundary flag. Run: dotnet run -- nulledge <DetourWrapper.dll> [diff <outfile>]
public static unsafe class NullEdgeTests
{
    // Same native signatures as ReturnHome/Server/Native/Detour.cs and DetourWrapper/DllExport.cpp
    static delegate* unmanaged<void*> allocDetour;
    static delegate* unmanaged<void*, byte*, uint> load;
    static delegate* unmanaged<void*, float*, float*, ushort, ushort, float*, uint> findSmoothPath;
    static delegate* unmanaged<void*, float*, float*, float*, ushort, ushort, uint> checkLos;
    static delegate* unmanaged<void*, float*, int, ushort, ushort, float*, uint> randomPoint;
    static delegate* unmanaged<void*, float*, ushort, ushort, uint> getPolyFlags;

    const ushort IncludeWalk = 0x0001;
    const ushort ExcludeLiquids = 0x003c;
    const int SmoothPathFloats = 6144;

    // World 0 polys with no boundary-flagged detail edge, from a census of all 251 meshes (vertices x, y, z)
    static readonly (int Zone, Vector3[] Verts)[] Traps =
    {
        (119, new[] { new Vector3(17583.4f, 55.51f, 20021.35f), new Vector3(17581.301f, 55.11f, 20019.949f), new Vector3(17568.0f, 54.81f, 20022.4f) }),
        (121, new[] { new Vector3(20248.682f, 45.43f, 21038.801f), new Vector3(20249.73f, 45.93f, 21041.951f), new Vector3(20249.381f, 48.33f, 21032.5f) }),
        (138, new[] { new Vector3(18685.381f, 54.12f, 24038.461f), new Vector3(18676.631f, 55.52f, 24044.762f), new Vector3(18679.43f, 55.32f, 24045.811f) }),
        (145, new[] { new Vector3(17482.771f, 84.16f, 26950.15f), new Vector3(17482.07f, 84.46f, 26947.0f), new Vector3(17479.271f, 84.86f, 26946.65f) }),
        (82, new[] { new Vector3(21324.4f, 85.77f, 14655.55f), new Vector3(21322.65f, 85.87f, 14661.149f), new Vector3(21323.0f, 87.07f, 14663.6f) }),
        (92, new[] { new Vector3(14123.2f, 54.63f, 17882.299f), new Vector3(14125.65f, 55.53f, 17883.0f), new Vector3(14134.4f, 58.03f, 17881.6f) }),
    };

    // The exact query in the 10/01 crash dump (roam point request, radius 30)
    static readonly Vector3 DumpCenter = new Vector3(17566.289f, 54.758f, 20022.711f);

    public static bool TryRun(string[] args)
    {
        // args[0] is the program path when they come from Environment.GetCommandLineArgs()
        int i = Array.IndexOf(args, "nulledge");
        if (i < 0)
        {
            return false;
        }

        if (i + 1 >= args.Length)
        {
            Console.WriteLine("usage: nulledge <DetourWrapper.dll> [diff <outfile>]");
            Environment.ExitCode = 2;
            return true;
        }

        string dll = Path.GetFullPath(args[i + 1]);
        string meshDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Meshes"));
        Bind(dll);
        Console.WriteLine($"DLL {dll}");
        Console.WriteLine($"Meshes {meshDir}");

        if (i + 3 < args.Length && args[i + 2] == "diff")
        {
            Diff(meshDir, args[i + 3]);
        }
        else
        {
            Traps_NoCrash(meshDir);
        }

        return true;
    }

    static void Bind(string dll)
    {
        IntPtr lib = NativeLibrary.Load(dll);
        allocDetour = (delegate* unmanaged<void*>)NativeLibrary.GetExport(lib, "allocDetour");
        load = (delegate* unmanaged<void*, byte*, uint>)NativeLibrary.GetExport(lib, "load");
        findSmoothPath = (delegate* unmanaged<void*, float*, float*, ushort, ushort, float*, uint>)NativeLibrary.GetExport(lib, "find_smoothPath");
        checkLos = (delegate* unmanaged<void*, float*, float*, float*, ushort, ushort, uint>)NativeLibrary.GetExport(lib, "check_los");
        randomPoint = (delegate* unmanaged<void*, float*, int, ushort, ushort, float*, uint>)NativeLibrary.GetExport(lib, "random_point");
        getPolyFlags = (delegate* unmanaged<void*, float*, ushort, ushort, uint>)NativeLibrary.GetExport(lib, "getPolyFlags");
    }

    static void* LoadMesh(string meshDir, int zone)
    {
        string path = Path.Combine(meshDir, $"0_{zone}.bin");
        void* detour = allocDetour();
        byte[] bytes = Encoding.ASCII.GetBytes(path + "\0");
        fixed (byte* p = bytes)
        {
            if (load(detour, p) == 0)
            {
                throw new InvalidOperationException($"load failed: {path}");
            }
        }

        return detour;
    }

    static Vector3 Centroid(Vector3[] v)
    {
        return (v[0] + v[1] + v[2]) / 3f;
    }

    // Unit 2D normal of edge a-b pointing away from the poly centroid
    static Vector3 Outward(Vector3 a, Vector3 b, Vector3 centroid)
    {
        Vector3 n = Vector3.Normalize(new Vector3(-(b.Z - a.Z), 0f, b.X - a.X));
        Vector3 mid = (a + b) / 2f;
        return Vector3.Dot(n, mid - centroid) < 0f ? -n : n;
    }

    // Every export at points 0.25 to 2.5 u outside each edge, then many random_point calls near each edge
    static void Traps_NoCrash(string meshDir)
    {
        float* rnd = stackalloc float[3];
        float* path = (float*)NativeMemory.Alloc(SmoothPathFloats * sizeof(float));
        float[] offsets = { 0.25f, 0.5f, 1f, 1.5f, 2f, 2.5f };
        long total = 0;

        foreach ((int zone, Vector3[] verts) in Traps)
        {
            void* detour = LoadMesh(meshDir, zone);
            Vector3 centroid = Centroid(verts);
            long calls = 0;

            if (zone == 119)
            {
                Vector3 c = DumpCenter;
                uint found = randomPoint(detour, (float*)&c, 30, IncludeWalk, ExcludeLiquids, rnd);
                calls++;
                Console.WriteLine($"  dump query random_point({c.X}, {c.Y}, {c.Z}, r 30) -> {found}");
            }

            for (int e = 0; e < 3; e++)
            {
                Vector3 a = verts[e];
                Vector3 b = verts[(e + 1) % 3];
                Vector3 n = Outward(a, b, centroid);

                for (int s = 1; s <= 9; s++)
                {
                    float t = s / 10f;
                    Vector3 onEdge = Vector3.Lerp(a, b, t);
                    foreach (float d in offsets)
                    {
                        Vector3 p = onEdge + n * d;
                        Vector3 end = p + n * 5f;
                        Vector3 target = centroid;
                        float range = 0f;
                        getPolyFlags(detour, (float*)&p, 0xffff, 0);
                        randomPoint(detour, (float*)&p, 30, IncludeWalk, ExcludeLiquids, rnd);
                        checkLos(detour, (float*)&p, (float*)&end, &range, IncludeWalk, ExcludeLiquids);
                        findSmoothPath(detour, (float*)&p, (float*)&target, IncludeWalk, ExcludeLiquids, path);
                        calls += 4;
                    }
                }

                // findRandomPointAroundCircle can land on the poly from up to the radius away
                Vector3 c2 = (a + b) / 2f + n * 4.5f;
                for (int k = 0; k < 50000; k++)
                {
                    randomPoint(detour, (float*)&c2, 30, IncludeWalk, ExcludeLiquids, rnd);
                }

                calls += 50000;
            }

            total += calls;
            Console.WriteLine($"  zone {zone}: {calls} queries, no crash");
        }

        NativeMemory.Free(path);
        Console.WriteLine($"PASS: {total} queries on the 6 trap polys, no access violation");
    }

    // Deterministic queries away from the traps; old and new DLL output must match byte for byte
    static void Diff(string meshDir, string outFile)
    {
        float* path = (float*)NativeMemory.Alloc(SmoothPathFloats * sizeof(float));
        StringBuilder sb = new StringBuilder();
        CultureInfo inv = CultureInfo.InvariantCulture;
        int lines = 0;

        foreach ((int zone, Vector3[] verts) in Traps)
        {
            void* detour = LoadMesh(meshDir, zone);
            Vector3 centroid = Centroid(verts);
            Random rng = new Random(zone);

            for (int k = 0; k < 3000; k++)
            {
                Vector3 p = new Vector3(centroid.X + (float)(rng.NextDouble() * 1000 - 500), centroid.Y + (float)(rng.NextDouble() * 20 - 10), centroid.Z + (float)(rng.NextDouble() * 1000 - 500));
                Vector3 end = p + new Vector3((float)(rng.NextDouble() * 40 - 20), 0f, (float)(rng.NextDouble() * 40 - 20));
                Vector2 off = new Vector2(p.X - centroid.X, p.Z - centroid.Z);
                Vector2 offEnd = new Vector2(end.X - centroid.X, end.Z - centroid.Z);
                if (off.Length() < 60f || offEnd.Length() < 60f)
                {
                    continue;
                }

                float range = 0f;
                uint flags = getPolyFlags(detour, (float*)&p, 0xffff, 0);
                uint los = checkLos(detour, (float*)&p, (float*)&end, &range, IncludeWalk, ExcludeLiquids);
                uint count = findSmoothPath(detour, (float*)&p, (float*)&end, IncludeWalk, ExcludeLiquids, path);
                sb.Append(zone).Append(' ').Append(k).Append(' ').Append(flags).Append(' ').Append(los).Append(' ')
                  .Append(range.ToString("R", inv)).Append(' ').Append(count);
                uint n = Math.Min(count, (uint)(SmoothPathFloats / 3));
                for (uint j = 0; j < n * 3; j++)
                {
                    sb.Append(' ').Append(path[j].ToString("R", inv));
                }

                sb.Append('\n');
                lines++;
            }
        }

        NativeMemory.Free(path);
        File.WriteAllText(outFile, sb.ToString());
        Console.WriteLine($"DIFF: wrote {lines} query results to {outFile}");
    }
}
