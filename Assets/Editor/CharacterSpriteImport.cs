#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// anything dropped in Sprites/Characters or Sprites/Rooms gets pixel art settings automatically
public class CharacterSpriteImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        bool isCharacter = assetPath.StartsWith("Assets/Sprites/Characters/");
        bool isRoom = assetPath.StartsWith("Assets/Sprites/Rooms/");
        if (!isCharacter && !isRoom) return;

        var imp = (TextureImporter)assetImporter;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 64; // same as the tiles, 64px = 1 tile
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.maxTextureSize = 2048;
    }
}
#endif
