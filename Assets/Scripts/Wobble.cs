using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wobble
{

    List<Wibble> Wibbles = new List<Wibble>();
    public Vector2 Offset;

    public class Wibble
    {

        Vector2 Direction;
        float Intensity;
        public static float Attentuation = 0.92f;
        public static float MinimumIntensity = 0.05f;
        public static float WaveLength = 0.05f;
        float TimeOffset;
        public Vector2 Offset;

        public Wibble(Vector2 Direction, float WibbleIntensity)
        {
            this.Intensity = WibbleIntensity;
            this.Direction = Direction;
            this.TimeOffset = Time.unscaledTime;
        }

        public bool UpdateWibble()
        {
            Intensity *= Attentuation;
            Offset = Direction * Intensity * (float)Math.Sin((Time.fixedUnscaledTime - TimeOffset) / WaveLength);
            return !(Intensity <= MinimumIntensity);
        }
    }

    public void AddWibble(Vector2 WibbleDirection, float WibbleIntensity)
    {
        if (WibbleIntensity > Wibble.MinimumIntensity)
        Wibbles.Add(new Wibble(WibbleDirection, WibbleIntensity));
    }

    public bool UpdateWobble()
    {
        if (Wibbles.Count == 0) return false;

        Offset = Vector2.zero;

        List<bool> WibblesActive = new List<bool>();
        for (int i = 0; i < Wibbles.Count; i++)
        {
            WibblesActive.Add(Wibbles[i].UpdateWibble());
            Offset += Wibbles[i].Offset;
        }

        for (int i = Wibbles.Count - 1; i >= 0; i--)
        {
            if (!WibblesActive[i]) Wibbles.RemoveAt(i);
        }

        if (Wibbles.Count == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

}
