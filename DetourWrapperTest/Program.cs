// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

public unsafe partial class DetourWrapper
{
    const string DllPath = @"C:\Users\bseki\source\repos\bsekinger\DetourWrapper\x64\Debug\DetourWrapper.dll";
    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial void* allocDetour();

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial void freeDetour(void* detourPtr);

    [LibraryImport(DllPath), UnmanagedCallConv]
     public static partial uint load(void* detourPtr, [MarshalAs(UnmanagedType.LPStr)] string filePath);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint find_path(void* ptr, void* start, void* end, uint includeFlags, uint excludeFlags, void* strPath);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint find_smoothPath(void* ptr, void* start, void* end, uint includeFlags, uint excludeFlags, void* smoothPath);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint check_los(void* ptr, void* start, void* end, void* range);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint random_point(void* ptr, void* center, float radius, uint includeFlags, uint excludeFlags, void* rndPoint);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint is_water(void* ptr, void* pos, uint includeFlags, uint excludeFlags);

    public enum PolyFlags : uint
    {
        Walk = 0x01,
        Swim = 0x02,
        Water = 0x04,
        Mud = 0x08,
        Lava = 0x10,
        All = 0xffff,
    }

    // Add future functions...
    static unsafe void Main()
    {
        if (NullEdgeTests.TryRun(Environment.GetCommandLineArgs()))
        {
            return;
        }

        Vector3 startPt = new Vector3(25118, 51.27f, 14158);
        Vector3 endPt = new Vector3(25120, 51.27f, 14328);
        //Vector3 WallStart = new Vector3(4412.0195f, 59.477183f, 17394.703f);
        //Vector3 WallTarget = new Vector3(4414.3f, 58.2188f, 17341.8f);
        //Vector3 WallTarget2 = new Vector3(4409.1f, 57.6562f, 17423.6f);
        //Vector3 treeStart = new Vector3(4409.3916f, 57.65735f, 17508.252f);
        //Vector3 treeTarget = new Vector3(4408.38f, 57.6562f, 17493.3f);
        //Vector3 rockStart = new Vector3(4455.538f, 57.65735f, 17429.459f);
        //Vector3 rockTarget = new Vector3(4463.85f, 57.6562f, 17402.4f);
        //Vector3 rockTarget2 = new Vector3(4473.99f, 57.6562f, 17441f);
        Vector3 centerPoint = new Vector3(25118, 51.27f, 14158);
        float radius = 40.0f;
        Vector3 rndPoint;


        //float range = 100.0f;

        // 1) Allocate and load
        void* detourPtr = DetourWrapper.allocDetour();
        if (detourPtr == null)
        {
            Console.WriteLine("Detour allocation failed!");
            return;
        }
        else
        {
            string filePath = @"C:\Users\bseki\source\repos\DetourWrapperTest\DetourWrapperTest\Meshes\84.bin";
            uint result = DetourWrapper.load(detourPtr, filePath);
            if (result == 0)
            {
                Console.WriteLine("Load failed!");
                return;
            }
            else
            {
                Console.WriteLine("Load successful!");

                // 2) Define a helper to query & print flags
                void QueryAndPrint(Vector3 position)
                {
                    // include all flags, exclude none
                    uint rawFlags = DetourWrapper.is_water(detourPtr, &position, (uint)PolyFlags.All, 0);

                    if (rawFlags == UInt32.MaxValue)
                    {
                        Console.WriteLine($"Point {position} is outside the nav-mesh.");
                        return;
                    }

                    // Cast the low 16 bits into our enum
                    PolyFlags flags = (PolyFlags)(ushort)rawFlags;
                    Console.WriteLine(
                        $"Point {position} returned flags = 0x{rawFlags:X}. " +
                        $"Interpreted as: {flags}"
                    );
                }

                QueryAndPrint(new Vector3(25118, 51.27f, 14158));
                QueryAndPrint(new Vector3(25120, 51.27f, 14328));
                QueryAndPrint(new Vector3(25126, 51.27f, 14407));
                QueryAndPrint(new Vector3(25162, 51.27f, 14542));
                QueryAndPrint(new Vector3(25202, 51.27f, 14625));
                QueryAndPrint(new Vector3(25279, 51.27f, 14704));
                QueryAndPrint(new Vector3(25309, 51.27f, 14719));
                QueryAndPrint(new Vector3(25491, 51.27f, 14778));
                QueryAndPrint(new Vector3(25551, 51.27f, 14822));
                QueryAndPrint(new Vector3(25603, 51.27f, 14988));
                QueryAndPrint(new Vector3(25604, 51.27f, 15011));
                QueryAndPrint(new Vector3(25669, 51.27f, 15136));

                QueryAndPrint(new Vector3(25117, 37.65f, 14333)); // Maps shows almost dead center of the river. Should be water, but is maerked as walk. Why??

                //    uint los = DetourWrapper.check_los(detourPtr, &startPt, &endPt, &range);
                //    if (los == 0)
                //    {
                //        Console.WriteLine("Invalid point!");
                //    }

                //    if (los == 1)
                //    {
                //        Console.WriteLine("Out of range!");
                //    }

                //    if (los == 2)
                //    {
                //        Console.WriteLine("LoS Failed!");
                //    }

                //    if (los == 5)
                //    {
                //        Console.WriteLine("LoS Success!");
                //    }

                //    Span<float> strPathArray = stackalloc float[768];
                //    fixed (float* strPathPtr = strPathArray)

                //    {
                //        uint pathCount = DetourWrapper.find_path(detourPtr, &startPt, &endPt, strPathPtr);

                //        if (pathCount > 0)
                //        {
                //            Console.WriteLine($"find_path succeeded. Path points found: {pathCount}");
                //            List<Vector3> pathPoints = new List<Vector3>();
                //            for (int i = 0; i < pathCount * 3; i += 3)
                //            {
                //                Vector3 point = new Vector3(strPathArray[i], strPathArray[i + 1], strPathArray[i + 2]);
                //                pathPoints.Add(point);
                //            }

                //            foreach (Vector3 point in pathPoints)
                //            {
                //                Console.WriteLine("Point: X=" + point.X + " Y=" + point.Y + " Z=" + point.Z);
                //            }
                //        }
                //        else
                //        {
                //            Console.WriteLine("No path found.");
                //        }
                //    }


                Span<float> strPathArray2 = stackalloc float[6144];
                fixed (float* smoothPathPtr = strPathArray2)

                {
                    uint pathCount = DetourWrapper.find_smoothPath(detourPtr, &startPt, &endPt, 4, 25, smoothPathPtr);

                    if (pathCount > 0)
                    {
                        Console.WriteLine($"refine_path succeeded. points found: {pathCount}");
                        List<Vector3> pathPoints = new List<Vector3>();
                        for (int i = 0; i < pathCount * 3; i += 3)
                        {
                            Vector3 point = new Vector3(strPathArray2[i], strPathArray2[i + 1], strPathArray2[i + 2]);
                            pathPoints.Add(point);
                        }

                        foreach (Vector3 point in pathPoints)
                        {
                            Console.WriteLine("Point: X=" + point.X + " Y=" + point.Y + " Z=" + point.Z);
                        }
                    }
                    else
                    {
                        Console.WriteLine("No path found.");
                    }
                }

                // Assuming DetourWrapper and other relevant objects are already defined
                List<Vector3> randomPoints = new List<Vector3>(); // Create a list to store 10 random points

                for (int i = 0; i < 10; i++) // Loop to generate 10 random points
                {
                    Span<float> floats = stackalloc float[3];
                    fixed (float* rndPointPtr = floats)
                    {
                        uint randomPointResult = DetourWrapper.random_point(detourPtr, &centerPoint, radius, 4, 25, rndPointPtr);
                        if (randomPointResult == 0)
                        {
                            Console.WriteLine("Invalid point!");
                            // Consider retry logic if you want exactly 10 valid points
                        }
                        else
                        {
                            Vector3 randomPoint = new Vector3(floats[0], floats[1], floats[2]);
                            randomPoints.Add(randomPoint); // Add the valid random point to the list
                            Console.WriteLine($"Random point {i + 1}: X={randomPoint.X}, Y={randomPoint.Y}, Z={randomPoint.Z}");
                        }
                    }
                }

            }

            DetourWrapper.freeDetour(detourPtr);
        }
     }
}
