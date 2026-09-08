using UnityEngine;
using UnityEditor;
using System.IO;

public class QuickSymbolGenerator : EditorWindow
{
    [MenuItem("Tools/Generate All Sprites")]
    static void GenerateAll()
    {
        string path = "Assets/Sprites/";
        Directory.CreateDirectory(path);

        // Create each symbol
        CreateSprite("Cherry", "#FF1744");
        CreateSprite("Lemon", "#FFEA00");
        CreateSprite("Orange", "#FF9100");
        CreateSprite("Seven", "#00E676");
        CreateSprite("Star", "#FFD700");
        CreateSprite("Wild", "#D500F9");
        CreateSprite("Scatter", "#00B0FF");

        AssetDatabase.Refresh();
        Debug.Log("✅ All sprites created! Check Assets/Sprites/");
    }

    static void CreateSprite(string name, string hexColor)
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color color = GetColorFromHex(hexColor);

        // Fill with color
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();

        // Save
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes($"Assets/Sprites/Symbol_{name}.png", bytes);

        Debug.Log($"Created: {name}");
    }

    static Color GetColorFromHex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }
}