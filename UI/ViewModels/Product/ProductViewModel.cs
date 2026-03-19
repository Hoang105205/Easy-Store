using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Threading.Tasks;
using UI.Services.AuthService;
using UI.Services.ProductService;


namespace UI.ViewModels.Product
{
    public class ProductModel
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Sku { get; set; }

        public string? CategoryName { get; set; }
        public string? ImagePath { get; set; }
        public int? StockQuantity { get; set; }
        public long? SalePrice { get; set; }
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

        // --- Các biến xử lý logic GraphQL Cursor ---
        private string? currentEndCursor = null;
        private Stack<string> previousCursors = new();
        private bool pressedButton = false;

        public ProductViewModel()
        {
            _productService = App.Current.Services.GetRequiredService<ProductService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); // Lấy luồng UI để cập nhật giao diện an toàn
        }

        public async Task LoadProductsAsync(
            string? afterCursor = null, 
            string? searchText = null, 
            Guid? categoryId = null,
            long? minPrice = null,
            long? maxPrice = null)
        {
            IsLoading = true;

            // Lấy cấu hình số lượng mỗi trang
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
                var result = await _productService.GetProductsPaginationAsync(itemsPerPage, afterCursor, searchText, categoryId, minPrice, maxPrice);

                // Cập nhật UI trên Thread chính
                _dispatcherQueue.TryEnqueue(() =>
                {
                    Products.Clear();

                    foreach (var item in result.Products)
                    {
                        Products.Add(item);
                    }

                    currentEndCursor = result.EndCursor;
                    CanGoNext = result.HasNextPage;
                });

                UpdateDisplayRangeText(itemsPerPage, result.Products.Count);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LỖI LẤY SẢN PHẨM] {ex.Message}");
            }
            finally
            {
                _dispatcherQueue.TryEnqueue(() => IsLoading = false);
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

        public async Task NextPageAsync(string? searchText = null, Guid? categoryId = null, long? minPrice = null, long? maxPrice = null)
        {
            if (currentEndCursor != null)
            {
                previousCursors.Push(currentEndCursor);
            }
            pressedButton = true;
            CurrentPageNumber++;
            CanGoPrevious = CurrentPageNumber > 1;

            await LoadProductsAsync(
                afterCursor: currentEndCursor, 
                searchText: searchText, 
                categoryId: categoryId,
                minPrice: minPrice,
                maxPrice: maxPrice
            );
        }

        public async Task PreviousPageAsync(string? searchText = null, Guid? categoryId = null, long? minPrice = null, long? maxPrice = null)
        {
            if (CurrentPageNumber > 1 && previousCursors.Count > 0)
            {
                pressedButton = true;
                CurrentPageNumber--;
                CanGoPrevious = CurrentPageNumber > 1;

                previousCursors.Pop();
                string? cursorToLoad = previousCursors.Count > 0 ? previousCursors.Peek() : null;
                await LoadProductsAsync(
                    afterCursor: cursorToLoad,
                    searchText: searchText,
                    categoryId: categoryId,
                    minPrice: minPrice,
                    maxPrice: maxPrice
                );
            }
        }
    }
}