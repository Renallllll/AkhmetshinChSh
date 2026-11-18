using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;

namespace AkhmetshinChudoShoes
{
    public partial class AnalyticsPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        public AnalyticsPage()
        {
            InitializeComponent();

            DateTime now = DateTime.Now;
            DatePickerStart.SelectedDate = new DateTime(now.Year, now.Month, 1);
            DatePickerEnd.SelectedDate = now;

            UpdateAnalytics();
        }

        // Пересчет данных в реальном времени
        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAnalytics();
        }

        private void UpdateAnalytics()
        {
            // Защита от вылета при инициализации
            if (DatePickerStart == null || DatePickerEnd == null || TBlockTotalOrders == null)
                return;

            // ЗАЩИТА: Если даты пустые (пользователь стер), прерываем расчет
            if (!DatePickerStart.SelectedDate.HasValue || !DatePickerEnd.SelectedDate.HasValue)
            {
                TBlockTotalOrders.Text = "-";
                TBlockTotalSum.Text = "-";
                TopProductsGrid.ItemsSource = null;
                return;
            }

            DateTime start = DatePickerStart.SelectedDate.Value;
            DateTime end = DatePickerEnd.SelectedDate.Value;

            // ВАЛИДАЦИЯ
            if (start > end)
            {
                TBlockTotalOrders.Text = "-";
                TBlockTotalSum.Text = "-";
                TopProductsGrid.ItemsSource = null;
                return;
            }

            DateTime endDateInclusive = end.AddDays(1);

            var ordersInPeriod = _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate < endDateInclusive)
                .ToList();

            TBlockTotalOrders.Text = ordersInPeriod.Count.ToString();

            var orderProductsInPeriod = _context.OrderProducts
                .Where(op => op.Orders.OrderDate >= start && op.Orders.OrderDate < endDateInclusive)
                .ToList();

            decimal totalSum = orderProductsInPeriod
                .Sum(op => op.Quantity * op.StockItems.Products.ProductPrice);

            TBlockTotalSum.Text = totalSum.ToString("F2");

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

        // ЭКСПОРТ В PDF
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
                // Создаем документ
                PdfDocument document = new PdfDocument();
                document.Info.Title = "Отчет по продажам";
                PdfPage page = document.AddPage();
                XGraphics gfx = XGraphics.FromPdfPage(page);

                // Шрифты
                XFont fontTitle = new XFont("Arial", 18, XFontStyleEx.Bold);
                XFont fontText = new XFont("Arial", 12, XFontStyleEx.Regular);
                XFont fontHeader = new XFont("Arial", 12, XFontStyleEx.Bold);

                int y = 40;
                // Заголовок
                gfx.DrawString("Отчет по продажам", fontTitle, XBrushes.Black, new XRect(0, y, page.Width, page.Height), XStringFormats.TopCenter);
                y += 40;

                // Период
                gfx.DrawString($"Период: с {start:dd.MM.yyyy} по {end:dd.MM.yyyy}", fontText, XBrushes.Black, 40, y);
                y += 25;

                // Сводные данные
                gfx.DrawString($"Общее количество заказов: {TBlockTotalOrders.Text}", fontText, XBrushes.Black, 40, y);
                y += 20;
                gfx.DrawString($"Общая сумма продаж: {TBlockTotalSum.Text} руб.", fontText, XBrushes.Black, 40, y);
                y += 40;

                // Таблица Топ-5
                gfx.DrawString("Топ-5 моделей по количеству проданных пар:", fontHeader, XBrushes.Black, 40, y);
                y += 25;

                gfx.DrawString("Наименование", fontHeader, XBrushes.Black, 40, y);
                gfx.DrawString("Производитель", fontHeader, XBrushes.Black, 300, y);
                gfx.DrawString("Кол-во пар", fontHeader, XBrushes.Black, 450, y);
                y += 20;

                // Вывод строк
                foreach (var item in TopProductsGrid.Items)
                {
                    dynamic row = item;
                    gfx.DrawString(row.ProductName.ToString(), fontText, XBrushes.Black, 40, y);
                    gfx.DrawString(row.ManufacturerName.ToString(), fontText, XBrushes.Black, 300, y);
                    gfx.DrawString(row.TotalQuantity.ToString(), fontText, XBrushes.Black, 450, y);
                    y += 20;
                }

                // Сохранение файла
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