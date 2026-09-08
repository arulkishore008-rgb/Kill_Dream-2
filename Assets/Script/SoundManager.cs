using UnityEngine;

public static class SoundManager 
{
    public static System.Action<NoiseEvent> OnNoiseMade;

    public static void MakeNoise(Vector3 position , float radius , GameObject Source = null)
    {
        NoiseEvent Noise = new NoiseEvent(position , radius , Source);

        OnNoiseMade?.Invoke(Noise);
    }
}
