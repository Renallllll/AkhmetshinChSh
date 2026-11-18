using PdfSharp.Fonts;
using PdfSharp.Snippets.Font;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AkhmetshinChudoShoes
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    /// 
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Активируем бесплатную AGPL-лицензию для PdfSharp
            PdfSharp.PdfSharpLicense.SetAgplLicense();
            GlobalFontSettings.FontResolver = new FailsafeFontResolver();
        }
    }


}
