using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
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


        // --- Các biến xử lý logic GraphQL Cursor ---
        private string? currentEndCursor = null;
        private Stack<string> previousCursors = new();

        public ProductViewModel()
        {
            _productService = App.Current.Services.GetRequiredService<ProductService>();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); // Lấy luồng UI để cập nhật giao diện an toàn
        }

        public async Task LoadProductsAsync(string? afterCursor = null, string? searchText = null, Guid? categoryId = null)
        {
            IsLoading = true;

            // Lấy cấu hình số lượng mỗi trang
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            int itemsPerPage = localSettings.Values["ItemsPerPage"] as int? ?? 10;

            try
            {
                var result = await _productService.GetProductsPaginationAsync(itemsPerPage, afterCursor, searchText, categoryId);

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

        public async Task NextPageAsync(string? searchText = null, Guid? categoryId = null)
        {
            if (currentEndCursor != null)
            {
                previousCursors.Push(currentEndCursor);
            }
            CurrentPageNumber++;
            CanGoPrevious = CurrentPageNumber > 1;

            await LoadProductsAsync(afterCursor: currentEndCursor, searchText: searchText, categoryId: categoryId);
        }

        public async Task PreviousPageAsync(string? searchText = null, Guid? categoryId = null)
        {
            if (CurrentPageNumber > 1 && previousCursors.Count > 0)
            {
                CurrentPageNumber--;
                CanGoPrevious = CurrentPageNumber > 1;

                previousCursors.Pop();
                string? cursorToLoad = previousCursors.Count > 0 ? previousCursors.Peek() : null;
                await LoadProductsAsync(afterCursor: cursorToLoad, searchText: searchText, categoryId: categoryId);
            }
        }
    }
}