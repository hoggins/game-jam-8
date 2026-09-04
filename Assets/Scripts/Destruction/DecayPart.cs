using UnityEngine;

namespace Destruction
{
  public class DecayPart : MonoBehaviour
  {
    [SerializeField] private PartDecaySettings settings = new();

    public PartDecaySettings Settings => settings;

    public void Configure(PartDecaySettings computed) => settings = computed;
  }
}
