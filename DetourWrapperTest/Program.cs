// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;

public unsafe partial class DetourWrapper
{
    const string DllPath = @"D:\My Documents\Repos\DetourWrapper\x64\Debug\DetourWrapper.dll";
    [LibraryImport(DllPath), UnmanagedCallConv]
    //[DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    public static partial void* allocDetour();

    [LibraryImport(DllPath), UnmanagedCallConv]
    //[DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    public static partial void freeDetour(void* detourPtr);

    [LibraryImport(DllPath), UnmanagedCallConv]
    //[DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    public static partial uint load(void* detourPtr, [MarshalAs(UnmanagedType.LPStr)] string filePath);

    [LibraryImport(DllPath), UnmanagedCallConv]
    //[DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    public static partial uint find_path(void* ptr, void* start, void* end, void* strPath);

    // Add future functions...
    static unsafe void Main()
    {
        Vector3 startPt = new Vector3(4821.74f, 57.6562f, 17139.2f);
        Vector3 endPt = new Vector3(4339.0127f, 57.65735f, 17687.316f);
        
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

                float[] strPathArray = new float[768];
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
            }

            DetourWrapper.freeDetour(detourPtr);
        }
     }
}
