// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using DetourWrapper; // Include the namespace from the DLL

class Program
{
    unsafe static void Main(string[] args)
    {
        string filePath = "D:\\My Documents\\Repos\\DetourWrapperTest\\DetourWrapperTest\\Meshes\\87.bin";
        void* ptr = null;

        // Call the LoadMesh function from the C++/CLI assembly
        ptr = MeshLoader.LoadMesh(filePath);

        if (ptr != null)
        {
            // Call the FreeMesh functiom from the C++/CLI assembly
            MeshLoader.FreeMesh(ptr);
        }
    }
}
