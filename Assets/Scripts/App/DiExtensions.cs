using VContainer;

namespace App
{
  public static class DiExtensions
  {
    public static T AsInjected<T>(this T target) where T : class
    {
     AppScope.Instance.Container.Inject(target);
      return target;
    }

    public static T Resolve<T>() =>
      AppScope.Instance.Container.Resolve<T>();
  }
}