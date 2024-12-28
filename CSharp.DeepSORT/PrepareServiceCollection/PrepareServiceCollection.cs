using CSharp.DeepSORT.ViewModels;
using DeepSORT.Application.DetectorUseCase;
using DeepSORT.Application.WebCameraUseCase;
using DeepSORT.Domain.Models.Detector;
using DeepSORT.Domain.Models.WebCamera;
using DeepSORT.Infrastructure.Factory;
using Microsoft.Extensions.DependencyInjection;

namespace CSharp.DeepSORT;
static public class PrepareServiceCollection
{
    private static void RegisterViewModel(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<MainWindowViewModel>();
    }

    private static void RegisterRepository(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IDetectorFactory, DetectorFactory>();
        serviceCollection.AddSingleton<IWebCameraFactory, WebCameraFactory>();
    }

    private static void RegisterService(IServiceCollection serviceCollection)
    {
    }

    private static void RegisterUseCase(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<DetectorUseCase>();
        serviceCollection.AddSingleton<WebCameraUseCase>();
    }

    public static IServiceProvider Initialize()
    {
        // 変数宣言
        IServiceCollection serviceCollection = new ServiceCollection();
        IServiceProvider serviceProvider;

        // 登録
        RegisterRepository(serviceCollection);
        RegisterService(serviceCollection);
        RegisterUseCase(serviceCollection);
        RegisterViewModel(serviceCollection);

        serviceProvider = serviceCollection.BuildServiceProvider();

        return serviceProvider;
    }
}
