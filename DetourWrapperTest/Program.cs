// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

public unsafe partial class DetourWrapper
{
    const string DllPath = @"D:\My Documents\Repos\DetourWrapper\x64\Debug\DetourWrapper.dll";
    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial void* allocDetour();

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial void freeDetour(void* detourPtr);

    [LibraryImport(DllPath), UnmanagedCallConv]
     public static partial uint load(void* detourPtr, [MarshalAs(UnmanagedType.LPStr)] string filePath);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint find_path(void* ptr, void* start, void* end, void* strPath);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint random_roam(void* ptr, void* start, void* strPath);

    [LibraryImport(DllPath), UnmanagedCallConv]
    public static partial uint check_los(void* ptr, void* start, void* end, void* range);

    // Add future functions...
    static unsafe void Main()
    {
        Vector3 startPt = new Vector3(4821.74f, 57.6562f, 17139.2f);
        Vector3 endPt = new Vector3(4339.0127f, 57.65735f, 17687.316f);
        Vector3 WallStart = new Vector3(4412.0195f, 59.477183f, 17394.703f);
        Vector3 WallTarget = new Vector3(4414.3f, 58.2188f, 17341.8f);
        Vector3 WallTarget2 = new Vector3(4409.1f, 57.6562f, 17423.6f);
        Vector3 treeStart = new Vector3(4409.3916f, 57.65735f, 17508.252f);
        Vector3 treeTarget = new Vector3(4408.38f, 57.6562f, 17493.3f);
        Vector3 rockStart = new Vector3(4455.538f, 57.65735f, 17429.459f);
        Vector3 rockTarget = new Vector3(4463.85f, 57.6562f, 17402.4f);
        Vector3 rockTarget2 = new Vector3(4473.99f, 57.6562f, 17441f);


        float range = 60.0f;

        void* detourPtr = DetourWrapper.allocDetour();
        if (detourPtr == null)
        {
            Console.WriteLine("Detour allocation failed!");
            return;
        }
        else
        {
            string filePath = @"D:\My Documents\Repos\DetourWrapperTest\DetourWrapperTest\Meshes\87.bin";
            uint result = DetourWrapper.load(detourPtr, filePath);
            if (result == 0)
            {
                Console.WriteLine("Load failed!");
                return;
            }
            else
            {
                Console.WriteLine("Load successful!");

                uint los = DetourWrapper.check_los(detourPtr, &rockStart, &rockTarget2, &range);
                if (los == 0)
                {
                    Console.WriteLine("Invalid point!");
                }

                if (los == 1)
                {
                    Console.WriteLine("Out of range!");
                }

                if (los == 2)
                {
                    Console.WriteLine("LoS Failed!");
                }

                if (los == 5)
                {
                    Console.WriteLine("LoS Success!");
                }


                Span<float> strPathArray = stackalloc float[768];                
                fixed (float* strPathPtr = strPathArray)
                    
                {
                    uint pathCount = DetourWrapper.find_path(detourPtr, &startPt, &endPt, strPathPtr);

                    if (pathCount > 0)
                    {
                        List<Vector3> pathPoints = new List<Vector3>();
                        for (int i = 0; i < pathCount * 3; i += 3)
                        {
                            Vector3 point = new Vector3(strPathArray[i], strPathArray[i + 1], strPathArray[i + 2]);
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

                Span<float> strPathArray2 = stackalloc float[768];
                fixed (float* strPathPtr = strPathArray2)

                {
                    uint pathCount = DetourWrapper.random_roam(detourPtr, &startPt, strPathPtr);

                    if (pathCount > 0)
                    {
                        Console.WriteLine("Random Roam Path found!");
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
            }

            DetourWrapper.freeDetour(detourPtr);
        }
     }
}
