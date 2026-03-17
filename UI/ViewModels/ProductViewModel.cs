using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UI.Services.ProductService;


namespace UI.ViewModels
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

    public class ProductViewModel : INotifyPropertyChanged
    {
        private readonly ProductService _productService;
        private readonly DispatcherQueue _dispatcherQueue;

        // --- Các biến trạng thái UI (Properties) ---

        public ObservableCollection<ProductModel> Products { get; } = new();

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private int _currentPageNumber = 1;
        public int CurrentPageNumber
        {
            get => _currentPageNumber;
            set { _currentPageNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanGoPrevious)); }
        }

        private bool _hasNextPage;
        public bool CanGoNext
        {
            get => _hasNextPage;
            set { _hasNextPage = value; OnPropertyChanged(); }
        }

        public bool CanGoPrevious => CurrentPageNumber > 1;

        // --- Các biến xử lý logic GraphQL Cursor ---
        private string? _currentEndCursor = null;
        private Stack<string> _previousCursors = new();

        public ProductViewModel()
        {
            _productService = new ProductService();
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread(); // Lấy luồng UI để cập nhật giao diện an toàn
        }

        public async Task LoadProductsAsync(string? afterCursor = null, string? searchText = null, Guid? categoryId = null)
        {
            IsLoading = true;
            Debug.WriteLine($"searchText= {searchText}, categoryId= {categoryId}");

            // Lấy cấu hình số lượng mỗi trang
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            int itemsPerPage = localSettings.Values["ItemsPerPage"] as int? ?? 10;

            try
            {
                var result = await _productService.GetProductsAsync(itemsPerPage, afterCursor, searchText, categoryId);

                // Cập nhật UI trên Thread chính
                _dispatcherQueue.TryEnqueue(() =>
                {
                    if (afterCursor == null)
                    {
                        Products.Clear();
                    }

                    foreach (var item in result.Products)
                    {
                        Products.Add(item);
                    }

                    _currentEndCursor = result.EndCursor;
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

        public async Task NextPageAsync()
        {
            if (_currentEndCursor != null)
            {
                _previousCursors.Push(_currentEndCursor);
            }
            CurrentPageNumber++;
            await LoadProductsAsync(afterCursor: _currentEndCursor);
        }

        public async Task PreviousPageAsync()
        {
            if (CurrentPageNumber > 1 && _previousCursors.Count > 0)
            {
                CurrentPageNumber--;
                _previousCursors.Pop();
                string? cursorToLoad = _previousCursors.Count > 0 ? _previousCursors.Peek() : null;
                await LoadProductsAsync(afterCursor: cursorToLoad);
            }
        }

        // --- Implementation của INotifyPropertyChanged giúp UI tự động cập nhật khi biến thay đổi ---
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}