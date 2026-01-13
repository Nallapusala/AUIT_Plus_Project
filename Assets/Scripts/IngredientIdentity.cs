using UnityEngine;

public class IngredientIdentity : MonoBehaviour
{
    public enum IngredientType
    {
        Milk,
        Eggs,
        Flour,
        Sugar
    }

    public IngredientType type;
}