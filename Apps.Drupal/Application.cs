﻿using Blackbird.Applications.Sdk.Common;

namespace Apps.Drupal;

public class Application : IApplication
{
    public string Name
    {
        get => "Drupal";
        set { }
    }

    public T GetInstance<T>()
    {
        throw new NotImplementedException();
    }
}