# TurnFlow

A pure C# Framework for building flexible turn-based combat systems, designed primarily for Unity but usable in any .NET Standard 2.1 compatible environment.

## Build to Unity

1. Build the dotnet project
    ```bash
    dotnet build -c Release
    ```
2. Copy the resulting .dll and .pdb files from /bin/Release/ into your Unity project's Assets/Plugins/ folder.
3. Wait for Unity to import the new files.
4. Use TurnFlow in your Unity Scripts:
    ```csharp
    using TurnFlow;

    public class MyScript : MonoBehaviour
    {
        void Start()
        {
            var class1 = new Class1();
            int result = class1.add(2, 3);
            Debug.Log("Result: " + result);
        }
    }
    ```