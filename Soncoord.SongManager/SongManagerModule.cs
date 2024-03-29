﻿using Prism.Ioc;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;
using Soncoord.Business.SongManager;
using Soncoord.Infrastructure;
using Soncoord.Infrastructure.Interfaces.Services;
using Soncoord.SongManager.ViewModels;
using Soncoord.SongManager.Views;

namespace Soncoord.SongManager
{
    public class SongManagerModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            var regionManager = containerProvider.Resolve<IRegionManager>();           
            regionManager.RegisterViewWithRegion(Regions.ShellContent, typeof(SongManager));
            regionManager.RegisterViewWithRegion(Regions.SongList, typeof(SongList));
            regionManager.RegisterViewWithRegion(Regions.SongDetail, typeof(SongDetail));
            regionManager.RegisterViewWithRegion(Regions.SongImport, typeof(SongImportExport));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<ISongsService, SongsService>();

            ViewModelLocationProvider.Register<SongDetail, SongDetailViewModel>();
            ViewModelLocationProvider.Register<SongList, SongListViewModel>();
            ViewModelLocationProvider.Register<SongImportExport, SongImportExportViewModel>();
        }
    }
}
