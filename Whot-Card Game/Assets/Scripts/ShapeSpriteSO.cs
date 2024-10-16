using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ShapeSprites", menuName = "ScriptableObjects/ShapeSprites", order = 1)]
public class ShapeSpriteSO : ScriptableObject
{
    public List<ShapeSprite> shapeSpriteList;

    [System.Serializable]
    public class ShapeSprite
    {
        public string shape;
        public Sprite sprite;
    }

    public Dictionary<string, Sprite> GetShapeSpriteDictionary()
    {
        Dictionary<string, Sprite> shapeSpriteDict = new Dictionary<string, Sprite>();
        foreach (ShapeSprite shapeSprite in shapeSpriteList)
        {
            shapeSpriteDict.Add(shapeSprite.shape, shapeSprite.sprite);
           // Debug.Log($"Added shape: {shapeSprite.shape} to dictionary");
        }
        return shapeSpriteDict;
    }
}
