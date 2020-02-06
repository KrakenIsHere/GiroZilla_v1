using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GiroZilla;
using PyroSquidUniLib.FileSystem;
using Microsoft.Win32;
using Serilog;

namespace GiroZilla.Views
{
    public partial class Help
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<Help>();

        private const double Offset = 1139d;

        private bool _isNewPage;
        private bool _isSelectedByCode;
        private bool _isManualSelect;
        private bool _isLaunch = true;

        private int _page = 1;

        public Help()
        {
            InitializeComponent();
            PopulateTreeView();

            ViewerPDF.PDFScrollViewer.ScrollChanged += PDFScrollViewer_ScrollChanged;
        }

        private async void ExpandFirstTreeViewItemAtLaunch(bool isLaunch)
        {
            try
            {
                switch (isLaunch)
                {
                    case true:
                        {
                            var isFirst = false;
                            foreach (CustomTreeViewItemParent item in ManualTreeView.Items)
                            {
                                //Must be an IF
                                if (!isFirst && ManualTreeView.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem tvi)
                                {
                                    tvi.IsSelected = true;
                                    tvi.IsExpanded = true;
                                    tvi.UpdateLayout();
                                    isFirst = true;
                                }
                            }
                            break;
                        }
                }
                _isLaunch = false;
                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        private async void PopulateTreeView()
        {
            var parents = new List<CustomTreeViewItemParent>();

            // Indholdsfortegnelse
            var p1 = new CustomTreeViewItemParent { Header = "Indholdsfortegnelse", Page = 1 };
            p1.Children.Add(new CustomTreeViewItemChild { Header = "Fortsat", Page = 2 });
            parents.Add(p1);

            // Side Menu
            var p2 = new CustomTreeViewItemParent { Header = "Side Menu", Page = 3 };
            p2.Children.Add(new CustomTreeViewItemChild { Header = "Sider", Page = 3 });
            parents.Add(p2);

            // Kunder
            var p3 = new CustomTreeViewItemParent { Header = "Kunder", Page = 4 };
            p3.Children.Add(new CustomTreeViewItemChild { Header = "Tilføj en ny kunde", Page = 4 });
            p3.Children.Add(new CustomTreeViewItemChild { Header = "Tilføj en ny Fejning/Service", Page = 5 });
            p3.Children.Add(new CustomTreeViewItemChild { Header = "Tilføj produkter til fejning", Page = 6 });
            p3.Children.Add(new CustomTreeViewItemChild { Header = "Fjern produkter fra fejning", Page = 6 });
            p3.Children.Add(new CustomTreeViewItemChild { Header = "Fjern kunde", Page = 7 });
            p3.Children.Add(new CustomTreeViewItemChild { Header = "Kunde søgning", Page = 7 });
            p3.Children.Add(new CustomTreeViewItemChild { Header = "Genindlæs tabel", Page = 7 });
            parents.Add(p3);

            // Faktura
            var p4 = new CustomTreeViewItemParent { Header = "Faktura", Page = 8 };
            p4.Children.Add(new CustomTreeViewItemChild { Header = "Print Girokort/Faktura", Page = 8 });
            p4.Children.Add(new CustomTreeViewItemChild { Header = "Markér fejning som betalt", Page = 9 });
            p4.Children.Add(new CustomTreeViewItemChild { Header = "Slet Fejning/Service", Page = 10 });
            p4.Children.Add(new CustomTreeViewItemChild { Header = "Fejning/Service Søgning", Page = 10 });
            p4.Children.Add(new CustomTreeViewItemChild { Header = "Genindlæs tabel", Page = 10 });
            parents.Add(p4);

            // Ruteplan
            var p5 = new CustomTreeViewItemParent { Header = "Ruteplan", Page = 11 };
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Tilføj en ny Rute", Page = 11 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Tilføj kunder til eksisterende rute", Page = 12 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Fjern kunde fra eksisterende rute", Page = 13 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Print rute", Page = 14 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Slet rute", Page = 15 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Print Månedsskema", Page = 15 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Tilføj Kommune / Område", Page = 16 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Slet Kommune / Område", Page = 16 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Rute Søgning", Page = 17 });
            p5.Children.Add(new CustomTreeViewItemChild { Header = "Genindlæs tabel", Page = 17 });
            parents.Add(p5);

            // Produkter
            var p6 = new CustomTreeViewItemParent { Header = "Produkter", Page = 18 };
            p6.Children.Add(new CustomTreeViewItemChild { Header = "Tilføj produkter", Page = 18 });
            p6.Children.Add(new CustomTreeViewItemChild { Header = "Fjern produkter", Page = 19 });
            p6.Children.Add(new CustomTreeViewItemChild { Header = "Produkt Søgning", Page = 20 });
            p6.Children.Add(new CustomTreeViewItemChild { Header = "Genindlæs tabel", Page = 20 });
            parents.Add(p6);

            // Indstillinger
            var p7 = new CustomTreeViewItemParent { Header = "Indstillinger", Page = 21 };
            p7.Children.Add(new CustomTreeViewItemChild { Header = "Database", Page = 22 });
            p7.Children.Add(new CustomTreeViewItemChild { Header = "Ændre database indstillinger", Page = 22 });
            p7.Children.Add(new CustomTreeViewItemChild { Header = "Test database indstillinger", Page = 23 });
            p7.Children.Add(new CustomTreeViewItemChild { Header = "Licens", Page = 23 });
            p7.Children.Add(new CustomTreeViewItemChild { Header = "Girokort & Faktura", Page = 24 });
            p7.Children.Add(new CustomTreeViewItemChild { Header = "Ændringer og gem", Page = 24 });
            parents.Add(p7);

            ManualTreeView.ItemsSource = parents;

            await Task.FromResult(true);
        }

        private async void ManualTreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                switch (!_isSelectedByCode)
                {
                    case true:
                        {
                            _isManualSelect = true;

                            var tree = sender as TreeView;

                            switch (tree?.SelectedItem)
                            {
                                case CustomTreeViewItemParent parent:
                                    {
                                        _page = parent.Page;
                                        break;
                                    }
                                case CustomTreeViewItemChild child:
                                    {
                                        _page = child.Page;
                                        break;
                                    }
                            }

                            ViewerPDF.PDFScrollViewer.ScrollToVerticalOffset(Offset * (_page - 1));
                            break;
                        }
                }
                _isSelectedByCode = false;


                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        private async void PDFScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            try
            {
                ExpandFirstTreeViewItemAtLaunch(_isLaunch);

                switch (!_isManualSelect)
                {
                    case true:
                        {
                            var tree = ManualTreeView;

                            switch (ViewerPDF.PDFScrollViewer.VerticalOffset > Offset * _page)
                            {
                                case true:
                                    {
                                        _page++;
                                        _isNewPage = true;
                                        break;
                                    }
                                default:
                                    {
                                        switch (ViewerPDF.PDFScrollViewer.VerticalOffset < Offset * (_page - 1))
                                        {
                                            case true:
                                                {
                                                    _page--;
                                                    _isNewPage = true;
                                                    _isSelectedByCode = true;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                            }

                            switch (_isNewPage)
                            {
                                case true:
                                    {
                                        foreach (CustomTreeViewItemParent item in tree.Items)
                                        {
                                            var tvi = tree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

                                            switch (item.Page == _page)
                                            {
                                                case true:
                                                    {
                                                        switch (tvi != null)
                                                        {
                                                            case true:
                                                                {
                                                                    tvi.IsExpanded = true;
                                                                    tvi.IsSelected = true;
                                                                    tvi.UpdateLayout();
                                                                    break;
                                                                }
                                                        }
                                                        break;
                                                    }
                                            }

                                            switch (item.Page > _page)
                                            {
                                                case true:
                                                    {
                                                        switch (tvi != null)
                                                        {
                                                            case true:
                                                                {
                                                                    tvi.IsExpanded = false;
                                                                    tvi.UpdateLayout();
                                                                    break;
                                                                }
                                                        }
                                                        break;
                                                    }
                                            }

                                            switch (item.Page < _page)
                                            {
                                                case true:
                                                    {
                                                        var foundItem = false;
                                                        foreach (CustomTreeViewItemChild subItem in tvi.Items)
                                                        {
                                                            //Must be an IF
                                                            if (!(tvi.ItemContainerGenerator.ContainerFromItem(subItem) is TreeViewItem tvi2) 
                                                                || subItem.Page != _page 
                                                                || foundItem) continue;

                                                            tvi2.IsSelected = true;
                                                            foundItem = true;
                                                        }
                                                        break;
                                                    }
                                            }
                                        }
                                        break;
                                    }
                            }
                            break;
                        }
                }
                _isManualSelect = false;

                await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }

        private async void DownloadManual_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                const string filePath = "Assets/Manual/GiroZilla Manual (DA-DK).pdf";

                var sfd = new SaveFileDialog
                {
                    InitialDirectory = DefaultDirectories.CurrentUserDesktop,
                    Filter = "Portable Document Format (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = "GiroZilla Manual (DA-DK).pdf"
                };

                switch (sfd.ShowDialog() == true)
                {
                    case true:
                        {
                            File.Copy(filePath, sfd.FileName);
                            break;
                        }
                }
                await Task.FromResult(true);
                Log.Information("Successfully downloaded manual PDF");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected Error");
            }
        }
    }

    public class CustomTreeViewItemParent
    {
        public CustomTreeViewItemParent()
        {
            Children = new ObservableCollection<CustomTreeViewItemChild>();
        }

        public string Header { get; set; }

        public int Page { get; set; }

        public ObservableCollection<CustomTreeViewItemChild> Children { get; set; }
    }

    public class CustomTreeViewItemChild
    {
        public string Header { get; set; }
        public int Page { get; set; }
    }
}
