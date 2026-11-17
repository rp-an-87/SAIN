using System;
using UnityEngine;

namespace SAIN.Helpers;

public static class MathHelpers
{
    public static float ClampObject(object value, float min, float max)
    {
        if (value != null)
        {
            Type type = value.GetType();
            if (type == typeof(float))
            {
                return Mathf.Clamp((float)value, min, max);
            }
            else if (type == typeof(int))
            {
                // Fork: an int boxed as object cannot be unboxed as float ((float)value threw InvalidCastException and broke the editor).
                return Mathf.Clamp((int)value, Mathf.RoundToInt(min), Mathf.RoundToInt(max));
            }
            else
            {
                Logger.LogError($"{type}");
            }
        }
        else
        {
            Logger.LogError($"Null!?");
        }
        return default;
    }
}
