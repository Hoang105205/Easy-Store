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

    public double TotalPrice
    {
        get
        {
            if (HasError) return 0;

            if (double.IsNaN(Quantity) || double.IsNaN(ImportPrice))
            {
                return 0;
            }

            if (Quantity < 0 || ImportPrice < 0)
            {
                return 0;
            }

            return Quantity * ImportPrice;
        }
    }

    [ObservableProperty]
    private string sku = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(TotalPrice))]
    private bool hasError;

    public bool IsValid => !HasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;
}