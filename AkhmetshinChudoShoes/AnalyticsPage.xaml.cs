using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace AkhmetshinChudoShoes
{
    public partial class AnalyticsPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        // ДОБАВЛЕНО: Храним последние правильные даты
        private DateTime _lastValidStart;
        private DateTime _lastValidEnd;
        private bool _isReverting = false; // Защита от зацикливания

        public AnalyticsPage()
        {
            InitializeComponent();

            // По умолчанию: с 1-го числа текущего месяца по сегодняшний день
            DateTime now = DateTime.Now;
            DateTime startOfMonth = new DateTime(now.Year, now.Month, 1);

            DatePickerStart.SelectedDate = startOfMonth;
            DatePickerEnd.SelectedDate = now;

            // Сохраняем в память
            _lastValidStart = startOfMonth;
            _lastValidEnd = now;

            UpdateAnalytics();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Если мы сами меняем дату (откатываем), ничего не делаем
            if (_isReverting) return;

            // Если одно из полей пустое, не рассчитываем
            if (!DatePickerStart.SelectedDate.HasValue || !DatePickerEnd.SelectedDate.HasValue)
                return;

            DateTime start = DatePickerStart.SelectedDate.Value;
            DateTime end = DatePickerEnd.SelectedDate.Value;

            // ВАЛИДАЦИЯ
            if (start > end)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);

                // ВКЛЮЧАЕМ ЗАЩИТУ И ОТКАТЫВАЕМ ДАТЫ НА ПОСЛЕДНИЕ ПРАВИЛЬНЫЕ
                _isReverting = true;
                DatePickerStart.SelectedDate = _lastValidStart;
                DatePickerEnd.SelectedDate = _lastValidEnd;
                _isReverting = false; // ВЫКЛЮЧАЕМ ЗАЩИТУ

                return; // Прерываем, данные не обновляем (остаются старые)
            }

            // Если даты корректны — запоминаем их
            _lastValidStart = start;
            _lastValidEnd = end;

            // Обновляем данные
            UpdateAnalytics();
        }

        private void UpdateAnalytics()
        {
            if (!DatePickerStart.SelectedDate.HasValue || !DatePickerEnd.SelectedDate.HasValue)
                return;

            DateTime start = DatePickerStart.SelectedDate.Value;
            DateTime end = DatePickerEnd.SelectedDate.Value;

            // Так как откат уже произошел выше, тут даты всегда правильные
            DateTime endDateInclusive = end.AddDays(1);

            // 1. Общее количество заказов
            var ordersInPeriod = _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate < endDateInclusive)
                .ToList();

            TBlockTotalOrders.Text = ordersInPeriod.Count.ToString();

            // 2. Общая сумма продаж
            var orderProductsInPeriod = _context.OrderProducts
                .Where(op => op.Orders.OrderDate >= start && op.Orders.OrderDate < endDateInclusive)
                .ToList();

            decimal totalSum = orderProductsInPeriod
                .Sum(op => op.Quantity * op.StockItems.Products.ProductPrice);

            TBlockTotalSum.Text = totalSum.ToString("F2");

            // 3. Топ-5 моделей
            var topProducts = orderProductsInPeriod
                .GroupBy(op => op.StockItems.Products)
                .Select(g => new
                {
                    ProductName = g.Key.ProductName,
                    ManufacturerName = _context.Manufacturers.FirstOrDefault(m => m.ManufacturerID == g.Key.ProductManufacturer)?.Manufacturer,
                    TotalQuantity = g.Sum(op => op.Quantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();

            TopProductsGrid.ItemsSource = topProducts;
        }

        // ЭКСПОРТ В PDF (остается без изменений)
        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            DateTime start = DatePickerStart.SelectedDate ?? DateTime.MinValue;
            DateTime end = DatePickerEnd.SelectedDate ?? DateTime.MaxValue;

            if (start > end)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                PdfDocument document = new PdfDocument();
                document.Info.Title = "Отчет по продажам";
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                XFont fontTitle = new XFont("Arial", 18, XFontStyleEx.Bold);
                XFont fontText = new XFont("Arial", 12, XFontStyleEx.Regular);
                XFont fontHeader = new XFont("Arial", 12, XFontStyleEx.Bold);

                int y = 40;
                gfx.DrawString("Отчет по продажам", fontTitle, XBrushes.Black, new XRect(0, y, page.Width, page.Height), XStringFormats.TopCenter);
                y += 40;

                gfx.DrawString($"Период: с {start:dd.MM.yyyy} по {end:dd.MM.yyyy}", fontText, XBrushes.Black, 40, y);
                y += 25;

                gfx.DrawString($"Общее количество заказов: {TBlockTotalOrders.Text}", fontText, XBrushes.Black, 40, y);
                y += 20;
                gfx.DrawString($"Общая сумма продаж: {TBlockTotalSum.Text} руб.", fontText, XBrushes.Black, 40, y);
                y += 40;

                gfx.DrawString("Топ-5 моделей по количеству проданных пар:", fontHeader, XBrushes.Black, 40, y);
                y += 25;

                gfx.DrawString("Наименование", fontHeader, XBrushes.Black, 40, y);
                gfx.DrawString("Производитель", fontHeader, XBrushes.Black, 300, y);
                gfx.DrawString("Кол-во пар", fontHeader, XBrushes.Black, 450, y);
                y += 20;

                foreach (var item in TopProductsGrid.Items)
                {
                    dynamic row = item;
                    gfx.DrawString(row.ProductName.ToString(), fontText, XBrushes.Black, 40, y);
                    gfx.DrawString(row.ManufacturerName.ToString(), fontText, XBrushes.Black, 300, y);
                    gfx.DrawString(row.TotalQuantity.ToString(), fontText, XBrushes.Black, 450, y);
                    y += 20;
                }

                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.Filter = "PDF Files|*.pdf";
                saveFileDialog.FileName = $"Отчет_{start:dd.MM.yyyy}_{end:dd.MM.yyyy}.pdf";

                if (saveFileDialog.ShowDialog() == true)
                {
                    document.Save(saveFileDialog.FileName);
                    MessageBox.Show("Отчет успешно сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}