﻿using CelSerEngine.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CelSerEngine;

public class ViewModelLocator
{
    public MainViewModel MainViewModel => App.Current.Services.GetRequiredService<MainViewModel>();
    public SelectProcessViewModel SelectProcessViewModel => App.Current.Services.GetRequiredService<SelectProcessViewModel>();
}
