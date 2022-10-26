using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml.Serialization;

using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Reflection;

namespace AnalysisWpf
{

    public partial class MainWindow : Window
    {
        private string m_sysname = string.Empty;

        private List<string> m_nodeNames = new List<string>();

        private List<KeyValuePair<string, XDocument>>  dataList = new List<KeyValuePair<string, XDocument>>();

        public MainWindow()
        {
            //position of submenu
            var menuDropAlignmentField = typeof(SystemParameters).GetField("_menuDropAlignment", BindingFlags.NonPublic | BindingFlags.Static);
            Action setAlignmentValue = () => {
                if (SystemParameters.MenuDropAlignment && menuDropAlignmentField != null) menuDropAlignmentField.SetValue(null, false);
            };
            setAlignmentValue();

            SystemParameters.StaticPropertyChanged += (sender, e) => { setAlignmentValue(); };

            InitializeComponent();

            tabControl1.Items.Clear();
        }
        

        private XDocument LoadDocument(string documentName)
        {
            try
            {
                XDocument doc = XDocument.Load(documentName);
                return doc;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
                return null;
            }
        }

        private void AddTab(string name)
        {
            TabItem tp = new TabItem
            {
                Name = name,
                Header = name,
                Height = Double.NaN
            };
            tabControl1.Items.Add(tp);
        }
        private List<string> CreateTabs(XDocument doc)
        {

            List<string> nodeNames = new List<string>();
            if (tabControl1.Items.Count==0)
            { 
                
                foreach (var name in doc.DescendantNodes().OfType<XElement>()
                    .Select(x => x.Name).Distinct())
                {
                    if (name != "server")
                    {
                        TabItem tb = tabControl1.Items.OfType<TabItem>().FirstOrDefault(n => n.Name == name);
                        if (tb==null)
                            nodeNames.Add(name.ToString());
                        else
                        {
                            //just skip duplicate tab.
                        }
                    }
                }
            
                tabControl1.Height = Double.NaN;
                //create controls on form and add to grid
                foreach (string nodename in nodeNames)
                {
                    AddTab(nodename);
                }
            }
            else
            {
                //Tab control already populated. Find missing missing elements
                foreach (var name in doc.DescendantNodes().OfType<XElement>()
                    .Select(x => x.Name).Distinct())
                {
                    if (name != "server")
                    {
                        var tabName = m_nodeNames.Find(x => x == name);

                        if (string.IsNullOrEmpty(tabName))
                        {
                            //Tab name not found
                            string nodename = name.ToString();
                            m_nodeNames.Add(nodename);
                            AddTab(nodename);


                        }
                    }
                }

            }
            return nodeNames;
        }

        private void AddToList(string systemName, XDocument xdoc)
        {
            dataList.Add(new KeyValuePair<string, XDocument>(systemName, xdoc));
        }


        //Idea is to check tags and tag names, and add to dataList
        private void AnalyseDocument(XDocument xdoc)
        {
            //find system name and add it to the list of systems
            // add it also to dataList
            if (xdoc == null)
                return;
            string nodename = "systemname";
            XElement node = (from c in xdoc.Descendants(nodename) select c).FirstOrDefault();
            string systemname = node.Value.ToString().Substring(1);
            string tab = "#!#@!@Servername: ";
            int length = tab.Length;
            string listName = systemname.Remove(0, length);
            lstSystems.Items.Add(listName);
            AddToList(listName, xdoc);
        }

        private void ClearTabData()
        {
            //foreach (string nodename in m_nodeNames)
            foreach (TabItem tabItem in tabControl1.Items)
            {
                tabItem.Content = null;
            }
        }

        private void FillTabs (XDocument xdoc)
        {
            //Now fill in grid views
            List<string> newList = BuildNodeList(xdoc);

            foreach (string nodename in newList)
            {
                XElement node = (from c in xdoc.Descendants(nodename) select c).FirstOrDefault();
                TabItem tb = tabControl1.Items.OfType<TabItem>().FirstOrDefault(n => n.Name == nodename);

              
                if (tb != null)
                {
                    if (nodename == "systemname")
                    {
                        //add to listbox
                        m_sysname = node.Value.ToString().Substring(1);
                    }

                    if (tb.Content == null)
                    {
                        // add GridView
                        ScrollViewer sv = new ScrollViewer
                        {
                            Height = Double.NaN
                        };
                        Grid grid = new Grid
                        {
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            Height = Double.NaN
                        };

                        // add columns
                        ColumnDefinition gridCol1 = new ColumnDefinition
                        {
                            Width = new GridLength(1020)
                        };

                        grid.ColumnDefinitions.Add(gridCol1);

                        RowDefinition gridRow1 = new RowDefinition();
                        grid.RowDefinitions.Add(gridRow1);

                        //TextBox tb1 = new TextBox
                        //{
                        //    Text = m_sysname
                        //};

                        //Grid.SetRow(tb1, 0);
                        //Grid.SetColumn(tb1, 0);

                        //grid.Children.Add(tb1);

                        TextBox tb2 = new TextBox
                        {
                            Height = Double.NaN,
                            MaxLength = 100,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            IsReadOnly = true
                        };
                        

                        string nodeValue = node.Value.ToString();
                        if (!string.IsNullOrEmpty(nodeValue))
                        {
                            string content = node.Value.ToString().Substring(1);
                            tb2.AppendText(content);
                        }
                        else
                        {
                            tb2.AppendText("");
                        }

                        Grid.SetRow(tb2, 0);
                        Grid.SetColumn(tb2, 1);

                        grid.Children.Add(tb2);
                        sv.Content = grid;
                        tb.Content = sv;
                    }
                    else
                    {
                        //something already in TabItem
                    }
                }
                else
                {
                    //tab item not found

                }
            }
        }

        private List<string> BuildNodeList (XDocument doc)
        {
            List<string> newNodes = new List<string>();

            if (doc != null)
            {
                foreach (var name in doc.DescendantNodes().OfType<XElement>()
                    .Select(x => x.Name).Distinct())
                {
                    if (name != "server")
                        newNodes.Add(name.ToString());
                }

            }
            return newNodes;

        }

        private void LstSystems_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = (ListBox)sender;
            var system = (string)item.SelectedItem;
            var doc = dataList.FirstOrDefault(x => x.Key == system);
            ClearTabData();
            FillTabs(doc.Value);
        }

        private void SelectItem(int id)
        {
            lstSystems.SelectedIndex=id;
            var system = (string)lstSystems.SelectedItem;
            var doc = dataList.FirstOrDefault(x => x.Key == system);
            ClearTabData();
            FillTabs(doc.Value);
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new CommonOpenFileDialog();
            dlg.Title = "Select files to load  ...";
            dlg.IsFolderPicker = false;
            dlg.ShowHiddenItems = true;
            dlg.InitialDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            dlg.AddToMostRecentlyUsedList = false;
            dlg.AllowNonFileSystemItems = false;
            dlg.DefaultDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            dlg.EnsureFileExists = true;
            dlg.EnsurePathExists = true;
            dlg.EnsureReadOnly = false;
            dlg.EnsureValidNames = true;
            dlg.Multiselect = true;
            dlg.ShowPlacesList = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {

                var fileNames = dlg.FileNames;


                foreach(var file in fileNames)
                {
                    XDocument xdoc = LoadDocument(file);
                    if (xdoc == null)
                        return;

                    AnalyseDocument(xdoc);
                    m_nodeNames = CreateTabs(xdoc);
                }
                if (lstSystems.Items.Count > 0)
                    SelectItem(0);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            m_nodeNames.Clear();
            dataList.Clear();
            tabControl1.Items.Clear();
            lstSystems.Items.Clear();

        }
    }
}
