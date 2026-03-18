using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace UI.ViewModels.Import;

public partial class ImportItemModel : ObservableObject
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private int quantity = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private double importPrice = 0;

    public double TotalPrice => Quantity * ImportPrice;
}