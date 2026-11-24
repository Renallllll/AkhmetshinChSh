using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrderCompositionWindow : Window
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        private Orders _currentOrder;
        private bool _isLoading = true;

        public OrderCompositionWindow(Orders selectedOrder)
        {
            InitializeComponent();
            _currentOrder = selectedOrder;

            DatePickerOrder.SelectedDate = selectedOrder.OrderDate;

            int roleId = Manager.CurrentUser != null ? Manager.CurrentUser.RoleID : 0;

            if (roleId == 1 || roleId == 2)
            {
                BtnAddItem.Visibility = Visibility.Visible;
            }

            if (roleId != 1)
            {
                DatePickerOrder.Visibility = Visibility.Hidden;
                BtnDeleteItem.Visibility = Visibility.Hidden;
                BtnSaveDate.Visibility = Visibility.Hidden;
            }

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
            }
            catch { }
        }

        private void UpdateGrid()
        {
            TBlockTitle.Text = $"Состав заказа №{_currentOrder.OrderID} от {_currentOrder.OrderDate:dd.MM.yyyy}";

            var composition = _context.OrderProducts
                .Where(op => op.OrderID == _currentOrder.OrderID)
                .ToList();

            decimal totalSum = 0;
            foreach (var item in composition)
            {
                if (item.StockItems != null && item.StockItems.Products != null)
                {
                    totalSum += item.Quantity * item.StockItems.Products.FinalPrice;
                }
            }

            CompositionGrid.ItemsSource = composition;
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
                BtnSaveDate.IsEnabled = false;
                return;
            }

            BtnSaveDate.IsEnabled = DatePickerOrder.SelectedDate.Value.Date != _currentOrder.OrderDate.Date;
        }

        private void BtnSaveDate_Click(object sender, RoutedEventArgs e)
        {
            if (!DatePickerOrder.SelectedDate.HasValue) return;

            try
            {
                _currentOrder.OrderDate = DatePickerOrder.SelectedDate.Value;
                _context.SaveChanges();

                UpdateGrid();
                BtnSaveDate.IsEnabled = false;

                MessageBox.Show("Дата заказа успешно обновлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении даты: {ex.Message}",
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
                        .Where(si => si.ProductID == prod.ProductID && si.QuantityForOrders > 0)
                        .ToList()
                        .Select(si => new
                        {
                            StockItem = si,
                            Label = $"{(si.Sizes != null ? si.Sizes.Size.ToString() : "Размер")} (в наличии: {si.QuantityForOrders})"
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
                if (stockItem.QuantityForOrders < qty)
                {
                    MessageBox.Show(
                        $"На складе недостаточно пар (доступно: {stockItem.QuantityForOrders}). Уменьшите количество.",
                        "Недостаточно остатка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    // Если такая товарная позиция уже есть в заказе — увеличиваем количество вместо дубля
                    var existingOrderProduct = _context.OrderProducts
                        .FirstOrDefault(op => op.OrderID == _currentOrder.OrderID && op.PositionID == stockItem.PositionID);

                    if (existingOrderProduct != null)
                    {
                        existingOrderProduct.Quantity += qty;
                    }
                    else
                    {
                        _context.OrderProducts.Add(new OrderProducts
                        {
                            OrderID = _currentOrder.OrderID,
                            PositionID = stockItem.PositionID,
                            Quantity = qty
                        });
                    }

                    stockItem.QuantityForOrders -= qty;
                    _context.SaveChanges();

                    UpdateGrid();
                    addDialog.DialogResult = true;
                    addDialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при добавлении позиции: {ex.Message}",
                        "Ошибка сохранения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
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

            int itemsCount = _context.OrderProducts.Count(op => op.OrderID == _currentOrder.OrderID);
            if (itemsCount <= 1)
            {
                MessageBox.Show("Нельзя удалить последнюю позицию: заказ не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Удалить выбранную позицию из заказа?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var stockItem = _context.StockItems.FirstOrDefault(si => si.PositionID == selectedItem.PositionID);
                    if (stockItem != null)
                    {
                        stockItem.QuantityForOrders += selectedItem.Quantity;
                    }

                    _context.OrderProducts.Remove(selectedItem);
                    _context.SaveChanges();

                    UpdateGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка при удалении позиции: {ex.Message}",
                        "Ошибка удаления",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
    }
}