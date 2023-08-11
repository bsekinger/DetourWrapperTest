// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

    // Add future functions...
    static unsafe void Main()
    {
        void* detourPtr = DetourWrapper.allocDetour();
        if (detourPtr != null)
        {
            string filePath = @"D:\My Documents\Repos\DetourWrapperTest\DetourWrapperTest\Meshes\87.bin";
            uint result = DetourWrapper.load(detourPtr, filePath);
            if (result != 0)
            {
                Console.WriteLine("Load successful!");
            }
            else
            {
                Console.WriteLine("Load failed!");
            }

            DetourWrapper.freeDetour(detourPtr);
        }
        else
        {
            Console.WriteLine("Detour allocation failed!");
        }
    }
}
