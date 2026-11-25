using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrderCompositionWindow : Window
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        private Orders _currentOrder;
        private List<OrderProducts> _workingItems = new List<OrderProducts>();
        private List<int> _removedPositionIds = new List<int>();
        private Dictionary<int, int> _stockAdjustments = new Dictionary<int, int>();
        private bool _isLoading = true;
        private bool _isSaved = false;

        public OrderCompositionWindow(Orders selectedOrder)
        {
            InitializeComponent();
            _currentOrder = selectedOrder;

            DatePickerOrder.SelectedDate = selectedOrder.OrderDate;

            // Кнопка добавления позиции скрыта для всех ролей, включая администратора и менеджера
            BtnAddItem.Visibility = Visibility.Collapsed;

            int roleId = Manager.CurrentUser != null ? Manager.CurrentUser.RoleID : 0;

            if (roleId != 1)
            {
                DatePickerOrder.Visibility = Visibility.Hidden;
                BtnDeleteItem.Visibility = Visibility.Hidden;
                BtnSaveChanges.Visibility = Visibility.Hidden;
            }
            else
            {
                BtnSaveChanges.Visibility = Visibility.Visible;
            }

            _workingItems = _context.OrderProducts
                .Where(op => op.OrderID == _currentOrder.OrderID)
                .ToList();

            UpdateGrid();
            _isLoading = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                if (_currentOrder != null)
                {
                    _context.Entry(_currentOrder).Reload();
                }

                if (!_isSaved)
                {
                    foreach (var entry in _context.ChangeTracker.Entries().ToList())
                    {
                        if (entry.Entity is OrderProducts || entry.Entity is StockItems || entry.Entity is Orders)
                        {
                            entry.Reload();
                        }
                    }
                }
            }
            catch { }
        }

        private int GetEffectiveStock(StockItems stockItem)
        {
            int delta = _stockAdjustments.ContainsKey(stockItem.PositionID) ? _stockAdjustments[stockItem.PositionID] : 0;
            return stockItem.QuantityForOrders + delta;
        }

        private void AddStockDelta(int positionId, int delta)
        {
            if (!_stockAdjustments.ContainsKey(positionId))
                _stockAdjustments[positionId] = 0;

            _stockAdjustments[positionId] += delta;
        }

        private void UpdateGrid()
        {
            DateTime displayDate = (Manager.CurrentUser != null && Manager.CurrentUser.RoleID == 1 && DatePickerOrder.SelectedDate.HasValue)
                ? DatePickerOrder.SelectedDate.Value
                : _currentOrder.OrderDate;

            TBlockTitle.Text = $"Состав заказа №{_currentOrder.OrderID} от {displayDate:dd.MM.yyyy}";

            decimal totalSum = 0;
            foreach (var item in _workingItems)
            {
                if (item.StockItems != null && item.StockItems.Products != null)
                {
                    totalSum += item.Quantity * item.StockItems.Products.FinalPrice;
                }
            }

            CompositionGrid.ItemsSource = null;
            CompositionGrid.ItemsSource = _workingItems;
            TBlockTotalSum.Text = $"{totalSum} руб.";
        }

        private void DatePickerOrder_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !DatePickerOrder.SelectedDate.HasValue) return;

            if (DatePickerOrder.SelectedDate.Value.Date > DateTime.Now.Date)
            {
                MessageBox.Show(
                    "Дата заказа не может быть в будущем. Выберите сегодняшнюю или более раннюю дату.",
                    "Ошибка даты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                _isLoading = true;
                DatePickerOrder.SelectedDate = _currentOrder.OrderDate;
                _isLoading = false;
                return;
            }

            UpdateGrid();
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            var allProducts = _context.Products.ToList();
            if (allProducts.Count == 0)
            {
                MessageBox.Show("В каталоге нет товаров для добавления.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Window addDialog = new Window
            {
                Title = "Добавление позиции в заказ",
                Width = 440,
                Height = 460,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            StackPanel root = new StackPanel { Margin = new Thickness(12) };

            root.Children.Add(new TextBlock { Text = "Поиск по наименованию:", Margin = new Thickness(0, 0, 0, 4) });
            TextBox tbSearch = new TextBox { Height = 26, Margin = new Thickness(0, 0, 0, 8) };
            root.Children.Add(tbSearch);

            root.Children.Add(new TextBlock { Text = "Товар:", Margin = new Thickness(0, 0, 0, 4) });
            ComboBox cbProducts = new ComboBox { Height = 28, DisplayMemberPath = "ProductName", Margin = new Thickness(0, 0, 0, 8) };
            root.Children.Add(cbProducts);

            root.Children.Add(new TextBlock { Text = "Доступный размер и остаток:", Margin = new Thickness(0, 0, 0, 4) });
            ComboBox cbSizes = new ComboBox { Height = 28, Margin = new Thickness(0, 0, 0, 8) };
            root.Children.Add(cbSizes);

            root.Children.Add(new TextBlock { Text = "Количество пар:", Margin = new Thickness(0, 0, 0, 4) });
            TextBox tbQty = new TextBox { Text = "1", Height = 28, Margin = new Thickness(0, 0, 0, 14) };
            root.Children.Add(tbQty);

            Action refreshProducts = () =>
            {
                string query = (tbSearch.Text ?? string.Empty).Trim().ToLower();
                var filtered = string.IsNullOrEmpty(query)
                    ? allProducts
                    : allProducts.Where(p => p.ProductName != null && p.ProductName.ToLower().Contains(query)).ToList();

                cbProducts.ItemsSource = filtered;
                cbProducts.SelectedIndex = filtered.Count > 0 ? 0 : -1;
            };

            cbProducts.SelectionChanged += (s, args) =>
            {
                var prod = cbProducts.SelectedItem as Products;
                if (prod == null)
                {
                    cbSizes.ItemsSource = null;
                    return;
                }

                var sizes = _context.StockItems
                    .Where(si => si.ProductID == prod.ProductID)
                    .ToList()
                    .Select(si => new { StockItem = si, Available = GetEffectiveStock(si) })
                    .Where(x => x.Available > 0)
                    .Select(x => new
                    {
                        x.StockItem,
                        Label = $"{(x.StockItem.Sizes != null ? x.StockItem.Sizes.Size.ToString() : "Размер")} (в наличии: {x.Available})"
                    })
                    .ToList();

                cbSizes.DisplayMemberPath = "Label";
                cbSizes.ItemsSource = sizes;
                cbSizes.SelectedIndex = sizes.Count > 0 ? 0 : -1;
            };

            tbSearch.TextChanged += (s, args) => refreshProducts();
            refreshProducts();

            StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            Button btnConfirm = new Button { Content = "Добавить", MinWidth = 110, Height = 32, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            Button btnCancel = new Button { Content = "Отмена", MinWidth = 100, Height = 32, IsCancel = true };

            btnConfirm.Click += (s, args) =>
            {
                dynamic selectedSizeEntry = cbSizes.SelectedItem;
                if (selectedSizeEntry == null)
                {
                    MessageBox.Show(
                        "Выберите размер из списка доступных на складе.",
                        "Не выбран размер",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(tbQty.Text, out int qty) || qty <= 0)
                {
                    MessageBox.Show(
                        "Введите корректное количество пар (целое число больше 0).",
                        "Некорректное количество",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                StockItems stockItem = selectedSizeEntry.StockItem;
                int available = GetEffectiveStock(stockItem);
                if (available < qty)
                {
                    MessageBox.Show(
                        $"На складе недостаточно пар (доступно: {available}). Уменьшите количество.",
                        "Недостаточно остатка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                _removedPositionIds.Remove(stockItem.PositionID);

                var existingOrderProduct = _workingItems.FirstOrDefault(op => op.PositionID == stockItem.PositionID);
                if (existingOrderProduct != null)
                {
                    existingOrderProduct.Quantity += qty;
                }
                else
                {
                    _workingItems.Add(new OrderProducts
                    {
                        OrderID = _currentOrder.OrderID,
                        PositionID = stockItem.PositionID,
                        StockItems = stockItem,
                        Quantity = qty
                    });
                }

                AddStockDelta(stockItem.PositionID, -qty);
                UpdateGrid();
                addDialog.Close();
            };

            btnCancel.Click += (s, args) => addDialog.Close();

            buttons.Children.Add(btnConfirm);
            buttons.Children.Add(btnCancel);
            root.Children.Add(buttons);

            addDialog.Content = root;
            addDialog.ShowDialog();
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CompositionGrid.SelectedItem as OrderProducts;
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите товарную позицию для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_workingItems.Count <= 1)
            {
                MessageBox.Show("Нельзя удалить последнюю позицию: заказ не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Удалить выбранную позицию из заказа?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _workingItems.Remove(selectedItem);
                if (!_removedPositionIds.Contains(selectedItem.PositionID))
                {
                    _removedPositionIds.Add(selectedItem.PositionID);
                }

                AddStockDelta(selectedItem.PositionID, selectedItem.Quantity);
                UpdateGrid();
            }
        }

        private void BtnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_workingItems.Count == 0)
            {
                MessageBox.Show("Заказ не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (DatePickerOrder.SelectedDate.HasValue && DatePickerOrder.SelectedDate.Value.Date > DateTime.Now.Date)
            {
                MessageBox.Show("Дата заказа не может быть в будущем.", "Ошибка даты", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (DatePickerOrder.SelectedDate.HasValue)
                {
                    _currentOrder.OrderDate = DatePickerOrder.SelectedDate.Value;
                }

                foreach (int posId in _removedPositionIds)
                {
                    var dbEntity = _context.OrderProducts.FirstOrDefault(op =>
                        op.OrderID == _currentOrder.OrderID && op.PositionID == posId);

                    if (dbEntity != null && _context.Entry(dbEntity).State != EntityState.Detached)
                    {
                        _context.OrderProducts.Remove(dbEntity);
                    }
                }

                foreach (var item in _workingItems)
                {
                    var existingInDb = _context.OrderProducts.FirstOrDefault(op =>
                        op.OrderID == _currentOrder.OrderID && op.PositionID == item.PositionID);

                    if (existingInDb != null)
                    {
                        existingInDb.Quantity = item.Quantity;
                    }
                    else
                    {
                        _context.OrderProducts.Add(new OrderProducts
                        {
                            OrderID = _currentOrder.OrderID,
                            PositionID = item.PositionID,
                            Quantity = item.Quantity
                        });
                    }
                }

                foreach (var pair in _stockAdjustments)
                {
                    if (pair.Value == 0) continue;
                    var stockItem = _context.StockItems.FirstOrDefault(si => si.PositionID == pair.Key);
                    if (stockItem != null)
                    {
                        stockItem.QuantityForOrders += pair.Value;
                    }
                }

                _context.SaveChanges();
                _removedPositionIds.Clear();
                _stockAdjustments.Clear();
                _isSaved = true;

                _workingItems = _context.OrderProducts
                    .Where(op => op.OrderID == _currentOrder.OrderID)
                    .ToList();

                UpdateGrid();
                MessageBox.Show("Изменения состава заказа успешно сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении изменений: {ex.Message}",
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}