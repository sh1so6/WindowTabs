using Microsoft.Extensions.DependencyInjection;
using WindowTabs.CSharp.Contracts;

namespace WindowTabs.CSharp.Services
{
    internal static class ServiceCollectionManagedGroupExtensions
    {
        public static IServiceCollection AddWindowTabsManagedGroupServices(this IServiceCollection services)
        {
            return services
                .AddManagedDesktopDragDropServices()
                .AddManagedGroupStripStateServices()
                .AddManagedGroupRegistryServices();
        }

        private static IServiceCollection AddManagedDesktopDragDropServices(this IServiceCollection services)
        {
            services.AddSingleton<ManagedDesktopInteractionState>();
            services.AddSingleton<IDragDropParent, ManagedDesktopDragDropParent>();
            services.AddSingleton<ManagedDragDropController>();
            services.AddSingleton<IDragDrop>(serviceProvider => serviceProvider.GetRequiredService<ManagedDragDropController>());
            return services;
        }

        private static IServiceCollection AddManagedGroupStripStateServices(this IServiceCollection services)
        {
            services.AddSingleton<ManagedGroupStripGroupOrderService>();
            services.AddSingleton<ManagedGroupStripLayoutService>();
            services.AddSingleton<ManagedGroupStripDragStateService>();
            services.AddSingleton<ManagedGroupStripPreviewStateService>();
            services.AddSingleton<ManagedGroupStripDragSessionStateService>();
            services.AddSingleton<ManagedGroupStripDisplayStateService>();
            services.AddSingleton<ManagedGroupStripFormStateService>();
            services.AddSingleton<ManagedGroupStripDropController>();
            services.AddSingleton<ManagedGroupStripButtonInteractionService>();
            services.AddSingleton<ManagedGroupStripButtonCollectionService>();
            services.AddSingleton<ManagedGroupStripButtonVisualService>();
            services.AddSingleton<ManagedGroupStripControlBindingService>();
            services.AddSingleton<ManagedGroupStripWindowSetService>();
            return services;
        }

        private static IServiceCollection AddManagedGroupRegistryServices(this IServiceCollection services)
        {
            services.AddSingleton<ManagedGroupStripFormFactory>();
            services.AddSingleton<ManagedGroupStripRegistrySyncService>();
            services.AddSingleton<ManagedGroupStripRegistryLifecycleService>();
            services.AddSingleton<ManagedGroupStripVisualService>();
            services.AddSingleton<ManagedGroupStripPlacementService>();
            services.AddSingleton<ManagedGroupStripPaintService>();
            services.AddSingleton<ManagedGroupStripMenuService>();
            services.AddSingleton<ManagedGroupDragDropTargetRegistrySyncService>();
            services.AddSingleton<ManagedGroupDragDropTargetRegistryLifecycleService>();
            return services;
        }
    }
}
