using System.Collections.Generic;
using UnityEngine;

/// TEMPORARY diagnostic - captures the final audio mix off the AudioListener. Delete when done.
/// Armed mode waits for the first sound above a threshold, then records a fixed window.
public class AudioCaptureProbe : MonoBehaviour
{
  public static readonly List<float> Buffer = new List<float>();
  public static bool Capturing;
  public static bool Armed;
  public static bool Triggered;
  public static bool Done;
  public static int Channels;
  public static int MaxFrames = 48000;   // 1 s at 48 kHz
  public static float Threshold = 0.02f;

  private void OnAudioFilterRead(float[] data, int channels)
  {
    Channels = channels;

    if (Armed && !Triggered)
    {
      for (var i = 0; i < data.Length; i++)
      {
        if (Mathf.Abs(data[i]) <= Threshold)
          continue;

        Triggered = true;
        Capturing = true;
        break;
      }
    }

    if (!Capturing)
      return;

    lock (Buffer)
    {
      for (var i = 0; i < data.Length; i++)
        Buffer.Add(data[i]);

      if (Buffer.Count < MaxFrames * channels)
        return;

      Capturing = false;
      Armed = false;
      Done = true;
    }
  }
}
