using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Core.Models;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UI.Messages;
using UI.Services.AuthService;
using UI.Services.ProductService;


namespace UI.ViewModels.Product
{
    public partial class ProductModel : ObservableObject
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Sku { get; set; }

        public string? CategoryName { get; set; }
        public string? ImagePath { get; set; }
        public int? StockQuantity { get; set; }

        [ObservableProperty]
        private int? availableStockQuantity;
        public long? SalePrice { get; set; }

        public ObservableCollection<ProductModel> PairProducts { get; set; } = new();
        public bool HasPairProducts => PairProducts != null && PairProducts.Count > 0; // de lam nut dropdown hien thi san pham kem theo
    }

    public partial class ProductViewModel : ObservableObject
    {
        private readonly ProductService _productService;
        private readonly DispatcherQueue _dispatcherQueue;

        // --- Các biến trạng thái UI (Properties) ---

        public ObservableCollection<ProductModel> Products { get; } = new();

        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private int currentPageNumber = 1;
        [ObservableProperty] private bool canGoNext;
        [ObservableProperty] private bool canGoPrevious = false;
        [ObservableProperty] private string displayRangeText = String.Empty;

        [ObservableProperty] private string? searchText = null;
        [ObservableProperty] private Guid? categoryId = null;
        [ObservableProperty] private long? minPrice = null, maxPrice = null;

        public Action? NavigateToAddProductAction { get; set; }
        public Action<Guid>? NavigateToProductDetailAction { get; set; }

        private CancellationTokenSource? _loadCts;

        [ObservableProperty] private string activeSortColumn;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SortGlyph))]
        private bool isAscending = true;

        public string SortGlyph => IsAscending ? "\xE70E" : "\xE70D";

        // --- Các biến xử lý logic GraphQL Cursor ---
        private string? currentEndCursor = null;
        private Stack<string> previousCursors = new();
        private bool pressedButton = false;

        public ProductViewModel()
        {
            _productService = App.Current.Services.GetRequiredService<ProductService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            WeakReferenceMessenger.Default.Register<CategorySelectedMessage>(this, async (r, m) =>
            {
                _debounceCts?.Cancel();
                CategoryId = m.Value;
                await LoadProductsAsync();
            });

            WeakReferenceMessenger.Default.Register<CategoryDeletedMessage>(this, async (r, m) =>
            {
                _debounceCts?.Cancel();
                if (CategoryId == m.Value)
                {
                    CategoryId = null;
                }
                await LoadProductsAsync();
            });

            ActiveSortColumn = "SKU";
            IsAscending = false;
        }

        [RelayCommand]
        public void AddProduct()
        {
            NavigateToAddProductAction?.Invoke();
        }

        [RelayCommand]
        public void GoToProductDetail(ProductModel selectedProduct)
        {
            if (selectedProduct != null)
            {
                NavigateToProductDetailAction?.Invoke(selectedProduct.Id);
            }
        }

        public async Task LoadProductsAsync(string? afterCursor = null)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var currentToken = _loadCts.Token;
            IsLoading = true;

            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            int itemsPerPage = localSettings.Values["ItemsPerPage"] as int? ?? 10;

            if (!pressedButton)
            {
                CurrentPageNumber = 1;
                afterCursor = null;

                previousCursors.Clear();
                currentEndCursor = null;
                CanGoPrevious = false;
            }
            pressedButton = false;

            try
            {
                var result = await _productService.GetProductsPaginationAsync(
                    itemsPerPage,
                    afterCursor,
                    SearchText,
                    CategoryId,
                    MinPrice,
                    MaxPrice,
                    ActiveSortColumn,
                    IsAscending
                );

                if (currentToken.IsCancellationRequested) return;

                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (currentToken.IsCancellationRequested) return;

                    Products.Clear();

                    foreach (var item in result.Products)
                    {
                        Products.Add(item);
                    }

                    currentEndCursor = result.EndCursor;
                    CanGoNext = result.HasNextPage;
                });

                if (!currentToken.IsCancellationRequested)
                {
                    UpdateDisplayRangeText(itemsPerPage, result.Products.Count);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI LẤY SẢN PHẨM] {ex.Message}");
            }
            finally
            {
                if (!currentToken.IsCancellationRequested)
                {
                    _dispatcherQueue.TryEnqueue(() => IsLoading = false);
                }
            }
        }

        public void UpdateDisplayRangeText(int itemsPerPage, int currentCount)
        {
            if (currentCount == 0)
            {
                DisplayRangeText = "Không có sản phẩm nào";
                return;
            }

            int startIndex = (CurrentPageNumber - 1) * itemsPerPage + 1;

            int endIndex = Math.Min(itemsPerPage * CurrentPageNumber, itemsPerPage * (CurrentPageNumber - 1) + currentCount);

            DisplayRangeText = $"Đang hiển thị sản phẩm từ {startIndex} - {endIndex}";
        }

        [RelayCommand]
        public async Task NextPage()
        {
            if (currentEndCursor != null)
            {
                previousCursors.Push(currentEndCursor);
            }
            pressedButton = true;
            CurrentPageNumber++;
            CanGoPrevious = CurrentPageNumber > 1;

            await LoadProductsAsync(afterCursor: currentEndCursor);
        }

        [RelayCommand]
        public async Task PreviousPage()
        {
            if (CurrentPageNumber > 1 && previousCursors.Count > 0)
            {
                pressedButton = true;
                CurrentPageNumber--;
                CanGoPrevious = CurrentPageNumber > 1;

                previousCursors.Pop();
                string? cursorToLoad = previousCursors.Count > 0 ? previousCursors.Peek() : null;
                await LoadProductsAsync(afterCursor: cursorToLoad);
            }
        }

        public async Task LoadAllProductsAsync(string? searchText = null, Guid? categoryId = null)
        {
            IsLoading = true;
            try
            {
                var allProducts = await _productService.GetProductsAsync(searchText, categoryId);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    Products.Clear();
                    foreach (var item in allProducts)
                    {
                        Products.Add(item);
                    }

                    DisplayRangeText = $"Hiển thị {allProducts.Count} sản phẩm";
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LỖI LẤY TẤT CẢ SẢN PHẨM] {ex.Message}");
            }
            finally
            {
                _dispatcherQueue.TryEnqueue(() => IsLoading = false);
            }
        }

        // --- DEBOUNCE LOGIC ---
        private CancellationTokenSource? _debounceCts;
        private readonly int _debounceDelay = 500;

        partial void OnSearchTextChanged(string? value) => DebounceLoadProducts();
        partial void OnMinPriceChanged(long? value) => DebounceLoadProducts();
        partial void OnMaxPriceChanged(long? value) => DebounceLoadProducts();

        private async void DebounceLoadProducts()
        {
            _debounceCts?.Cancel();

            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;

            try
            {
                await Task.Delay(_debounceDelay, token);

                if (!token.IsCancellationRequested)
                {
                    await LoadProductsAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        public void UnregisterMessages()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            _debounceCts?.Cancel();
            _loadCts?.Cancel();
        }

        [RelayCommand]
        private async Task SortAsync(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) return;

            if (ActiveSortColumn == columnName)
            {
                IsAscending = !IsAscending;
            }
            else
            {
                ActiveSortColumn = columnName;
                IsAscending = true;
            }

            pressedButton = false;

            await LoadProductsAsync();
        }
    }
}