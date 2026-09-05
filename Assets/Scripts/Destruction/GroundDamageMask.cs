using UnityEngine;

namespace Destruction
{
  /// <summary>
  /// Owns a runtime damage mask for ground renderers and paints it in level world space.
  /// The mask uses RGB for the damage color and alpha for damage intensity when multicolor mode is enabled;
  /// otherwise it writes a grayscale intensity mask for GroundEnvShader.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class GroundDamageMask : MonoBehaviour
  {
    private static readonly int DamageMaskId = Shader.PropertyToID("_DamageMask");
    private static readonly int DamageMaskStId = Shader.PropertyToID("_DamageMask_ST");

    public static GroundDamageMask Instance { get; private set; }

    [Tooltip("Single renderer that uses GroundEnvShader and receives the generated damage mask.")]
    [SerializeField] private Renderer _groundRenderer;

    [Tooltip("Width and height of the generated square damage mask in pixels.")]
    [SerializeField, Min(1)] private int _textureResolution = 1024;

    [Tooltip("Single ground mesh whose world-space bounds define the active damage area.")]
    [SerializeField] private MeshFilter _boundsMesh;

    [Tooltip("Stores stamp RGB in the mask. The current GroundEnvShader samples only the red channel, so leave this disabled for a grayscale intensity mask.")]
    [SerializeField] private bool _useMultiColorMode;

    private Texture2D _damageMask;
    private Color[] _pixels;
    private MaterialPropertyBlock _propertyBlock;
    private Bounds _levelWorldBounds;

    private bool _isDuplicate;

    public Texture2D DamageMask => _damageMask;
    public int TextureResolution => _textureResolution;
    public Bounds LevelWorldBounds => _levelWorldBounds;

    private void Awake()
    {
      if (Instance != null && Instance != this)
      {
        _isDuplicate = true;
        Destroy(gameObject);
        return;
      }

      Instance = this;
    }

    private void OnEnable()
    {
      if (_isDuplicate)
        return;

      Initialize();
    }

    private void OnDisable()
    {
      ReleaseTexture();
    }

    private void OnDestroy()
    {
      if (Instance == this)
        Instance = null;
    }

    /// <summary>
    /// Paints a circular damage area centered at a world position.
    /// Smoothness is in the range 0..1: zero gives a hard edge, one fades from the center to the outer radius.
    /// Returns false when the center is outside the active bounds or the brush has no effect.
    /// </summary>
    public bool ApplyCircleDamage(
      Vector3 worldPosition,
      float radius,
      Color color,
      float intensity,
      float smoothness = 0f)
    {
      if (radius <= 0f || !TryGetWorldPoint(worldPosition, out var center))
        return false;

      return Paint(
        BrushShape.Circle,
        center,
        new Vector2(radius, radius),
        color,
        intensity,
        smoothness);
    }

    /// <summary>
    /// Paints an axis-aligned square damage area centered at a world position.
    /// Size is the square's side length in world units.
    /// Returns false when the center is outside the active bounds or the brush has no effect.
    /// </summary>
    public bool ApplySquareDamage(Vector3 worldPosition, float size, Color color, float intensity)
    {
      if (size <= 0f || !TryGetWorldPoint(worldPosition, out var center))
        return false;

      var halfSize = size * 0.5f;
      return Paint(BrushShape.Square, center, new Vector2(halfSize, halfSize), color, intensity, 0f);
    }

    /// <summary>
    /// Clears all painted damage from the runtime mask.
    /// </summary>
    public void ClearDamage()
    {
      if (_damageMask == null || _pixels == null)
        return;

      for (var i = 0; i < _pixels.Length; i++)
        _pixels[i] = Color.clear;

      UploadTexture();
    }

    private void Initialize()
    {
      ReleaseTexture();

      _textureResolution = Mathf.Max(1, _textureResolution);
      if (_boundsMesh == null || _boundsMesh.sharedMesh == null)
      {
        Debug.LogError($"{nameof(GroundDamageMask)} on {name} needs one assigned MeshFilter with a mesh.", this);
        return;
      }

      if (_groundRenderer == null)
      {
        Debug.LogError($"{nameof(GroundDamageMask)} on {name} needs one assigned ground Renderer.", this);
        return;
      }

      _levelWorldBounds = CalculateWorldBounds(_boundsMesh);
      var worldSize = _levelWorldBounds.size;
      if (worldSize.x <= 0f || worldSize.z <= 0f)
      {
        Debug.LogError($"{nameof(GroundDamageMask)} on {name} needs non-zero X and Z world bounds.", this);
        return;
      }

      var pixelCount = _textureResolution * _textureResolution;
      _pixels = new Color[pixelCount];
      _damageMask = new Texture2D(
        _textureResolution,
        _textureResolution,
        TextureFormat.RGBA32,
        false,
        true)
      {
        name = $"{name}_DamageMask",
        filterMode = FilterMode.Bilinear,
        wrapMode = TextureWrapMode.Clamp,
        anisoLevel = 0,
      };

      UploadTexture();

      _propertyBlock ??= new MaterialPropertyBlock();
      AssignTextureToRenderer();
    }

    private void ReleaseTexture()
    {
      if (_damageMask != null)
      {
        if (Application.isPlaying)
          Destroy(_damageMask);
        else
          DestroyImmediate(_damageMask);
        _damageMask = null;
      }

      _pixels = null;
    }

    private bool Paint(
      BrushShape shape,
      Vector2 center,
      Vector2 halfExtents,
      Color color,
      float intensity,
      float smoothness)
    {
      if (_damageMask == null || _pixels == null)
        return false;

      var paintIntensity = Mathf.Clamp01(intensity) * Mathf.Clamp01(color.a);
      if (paintIntensity <= 0f)
        return false;

      var worldSize = _levelWorldBounds.size;
      var worldMin = _levelWorldBounds.min;
      var pixelSizeX = worldSize.x / _textureResolution;
      var pixelSizeZ = worldSize.z / _textureResolution;

      var minX = WorldToPixel(center.x - halfExtents.x, worldMin.x, worldSize.x);
      var maxX = WorldToPixel(center.x + halfExtents.x, worldMin.x, worldSize.x);
      var minY = WorldToPixel(center.y - halfExtents.y, worldMin.z, worldSize.z);
      var maxY = WorldToPixel(center.y + halfExtents.y, worldMin.z, worldSize.z);

      minX = Mathf.Clamp(minX, 0, _textureResolution - 1);
      maxX = Mathf.Clamp(maxX, 0, _textureResolution - 1);
      minY = Mathf.Clamp(minY, 0, _textureResolution - 1);
      maxY = Mathf.Clamp(maxY, 0, _textureResolution - 1);

      var paintColor = new Color(
        Mathf.Clamp01(color.r),
        Mathf.Clamp01(color.g),
        Mathf.Clamp01(color.b),
        1f);
      var changed = false;

      for (var y = minY; y <= maxY; y++)
      {
        var worldZ = worldMin.z + (y + 0.5f) * pixelSizeZ;
        var offsetZ = Mathf.Abs(worldZ - center.y);

        for (var x = minX; x <= maxX; x++)
        {
          var worldX = worldMin.x + (x + 0.5f) * pixelSizeX;
          var offsetX = Mathf.Abs(worldX - center.x);
          var coverage = GetBrushCoverage(shape, offsetX, offsetZ, halfExtents, smoothness);
          if (coverage <= 0f)
            continue;

          var contribution = paintIntensity * coverage;
          var index = y * _textureResolution + x;
          var previous = _pixels[index];
          var totalIntensity = previous.a + contribution;
          if (totalIntensity <= previous.a)
            continue;

          var inverseTotal = 1f / totalIntensity;
          var outputIntensity = Mathf.Clamp01(totalIntensity);
          var outputColor = new Color(
            (previous.r * previous.a + paintColor.r * contribution) * inverseTotal,
            (previous.g * previous.a + paintColor.g * contribution) * inverseTotal,
            (previous.b * previous.a + paintColor.b * contribution) * inverseTotal,
            outputIntensity);

          _pixels[index] = _useMultiColorMode
            ? outputColor
            : new Color(outputIntensity, outputIntensity, outputIntensity, outputIntensity);
          changed = true;
        }
      }

      if (changed)
        UploadTexture();

      return changed;
    }

    private static float GetBrushCoverage(
      BrushShape shape,
      float offsetX,
      float offsetZ,
      Vector2 halfExtents,
      float smoothness)
    {
      if (shape == BrushShape.Square)
        return offsetX <= halfExtents.x && offsetZ <= halfExtents.y ? 1f : 0f;

      var distance = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
      var radius = halfExtents.x;
      if (distance > radius)
        return 0f;

      var clampedSmoothness = Mathf.Clamp01(smoothness);
      if (clampedSmoothness <= 0f)
        return 1f;

      var fadeStart = radius * (1f - clampedSmoothness);
      var fade = Mathf.InverseLerp(radius, fadeStart, distance);
      return Mathf.SmoothStep(0f, 1f, fade);
    }

    private bool TryGetWorldPoint(Vector3 worldPosition, out Vector2 point)
    {
      var min = _levelWorldBounds.min;
      var max = _levelWorldBounds.max;
      if (worldPosition.x < min.x || worldPosition.x > max.x
        || worldPosition.z < min.z || worldPosition.z > max.z)
      {
        point = default;
        return false;
      }

      point = new Vector2(worldPosition.x, worldPosition.z);
      return true;
    }

    private int WorldToPixel(float worldCoordinate, float worldMin, float worldSize)
    {
      return Mathf.FloorToInt((worldCoordinate - worldMin) / worldSize * _textureResolution);
    }

    private void UploadTexture()
    {
      _damageMask.SetPixels(_pixels);
      _damageMask.Apply(false, false);
    }

    private static Bounds CalculateWorldBounds(MeshFilter meshFilter)
    {
      var localBounds = meshFilter.sharedMesh.bounds;
      var localToWorld = meshFilter.transform.localToWorldMatrix;
      var worldBounds = new Bounds(localToWorld.MultiplyPoint3x4(localBounds.center), Vector3.zero);

      for (var x = 0; x <= 1; x++)
      {
        for (var y = 0; y <= 1; y++)
        {
          for (var z = 0; z <= 1; z++)
          {
            var corner = new Vector3(
              x == 0 ? localBounds.min.x : localBounds.max.x,
              y == 0 ? localBounds.min.y : localBounds.max.y,
              z == 0 ? localBounds.min.z : localBounds.max.z);
            worldBounds.Encapsulate(localToWorld.MultiplyPoint3x4(corner));
          }
        }
      }

      return worldBounds;
    }

    private void AssignTextureToRenderer()
    {
      var worldSize = _levelWorldBounds.size;
      var worldMin = _levelWorldBounds.min;
      var maskSt = new Vector4(
        1f / worldSize.x,
        1f / worldSize.z,
        -worldMin.x / worldSize.x,
        -worldMin.z / worldSize.z);

      if (!UsesDamageMask(_groundRenderer))
      {
        Debug.LogWarning($"{nameof(GroundDamageMask)} renderer on {name} has no _DamageMask property.", this);
        return;
      }

      _groundRenderer.GetPropertyBlock(_propertyBlock);
      _propertyBlock.SetTexture(DamageMaskId, _damageMask);
      _propertyBlock.SetVector(DamageMaskStId, maskSt);
      _groundRenderer.SetPropertyBlock(_propertyBlock);
    }

    private static bool UsesDamageMask(Renderer renderer)
    {
      var materials = renderer.sharedMaterials;
      for (var i = 0; i < materials.Length; i++)
      {
        if (materials[i] != null && materials[i].HasProperty(DamageMaskId))
          return true;
      }

      return false;
    }

    private enum BrushShape
    {
      Circle,
      Square,
    }
  }
}
